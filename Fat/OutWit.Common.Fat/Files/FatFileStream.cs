using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Files
{
    /// <summary>
    /// The contents of a file on a FAT volume.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A read follows the file's cluster chain only as far as it reads, and reads each
    /// contiguous stretch of clusters in one request to the device. A chain that ends
    /// before the file does is reported as <see cref="FatErrorKind.Corrupt"/> when the read
    /// reaches the gap.
    /// </para>
    /// <para>
    /// A write takes clusters as the file grows, next to the ones it has where it can.
    /// The directory entry — length, first cluster, time of the last write — and the
    /// allocation table reach the device on <see cref="FlushAsync(CancellationToken)"/>
    /// and on disposal: clusters a file grows by are linked before the entry counts on
    /// them, and clusters it shrinks by are released after the entry stops counting on
    /// them, so a crash between the two leaves at worst clusters that belong to no one.
    /// That holds on devices that write in the order they are asked to;
    /// <see cref="Devices.BlockDeviceCached"/> keeps it only when created to write through.
    /// </para>
    /// <para>
    /// A read or write that fails leaves the position where it was. The stream is
    /// asynchronous at heart; the synchronous members block on it. Like the volume, it is
    /// not safe for concurrent use — neither with itself nor with other streams or calls
    /// on the same volume.
    /// </para>
    /// </remarks>
    public sealed class FatFileStream : Stream
    {
        #region Fields

        private readonly FatVolumeCore m_core;

        private readonly FatFileHandle m_handle;

        private readonly FileAccess m_access;

        private readonly long m_appendStart;

        private long m_position;

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <param name="core">The volume.</param>
        /// <param name="directory">The directory holding the file.</param>
        /// <param name="item">The file.</param>
        /// <param name="access">What the stream may do.</param>
        /// <param name="isAppend">Whether writes may only go past the current end.</param>
        /// <exception cref="FatException">The file is open in a conflicting way, or its first cluster is not on the volume.</exception>
        internal FatFileStream(FatVolumeCore core, FatDirectory directory, DirectoryItem item, FileAccess access, bool isAppend)
        {
            var handle = new FatFileHandle(core, directory, item);
            bool isWriter = (access & FileAccess.Write) != 0;
            core.OpenFiles.Open(item, isWriter);

            m_core = core;
            m_handle = handle;
            m_access = access;
            m_appendStart = isAppend ? handle.Length : -1;
            m_position = isAppend ? handle.Length : 0;

            if (isWriter)
                core.OpenFiles.Attach(item, this);
        }

        #endregion

        #region Read

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfClosed();
            if (!IsReader)
                throw new NotSupportedException("The file is not open for reading.");

            int read = await m_handle.ReadAsync(m_position, buffer, cancellationToken).ConfigureAwait(false);
            m_position += read;
            return read;
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        /// <remarks>Blocks on the asynchronous read.</remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        /// <remarks>Blocks on the asynchronous read.</remarks>
        public override int Read(Span<byte> buffer)
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int read = Read(rented, 0, buffer.Length);
                rented.AsSpan(0, read).CopyTo(buffer);
                return read;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }

        /// <summary>
        /// Follows the chain to the last cluster the file needs, reading only the table.
        /// </summary>
        /// <exception cref="FatException">The chain is corrupt or ends before the file does.</exception>
        internal ValueTask CheckChainAsync(CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            return m_handle.CheckChainAsync(cancellationToken);
        }

        #endregion

        #region Write

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">The file is not open for writing.</exception>
        /// <exception cref="FatException">The volume is full, or the file would pass 4 GiB.</exception>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfNotWriter();
            if (buffer.IsEmpty)
                return;

            await m_handle.WriteAsync(m_position, buffer, cancellationToken).ConfigureAwait(false);
            m_position += buffer.Length;
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        /// <remarks>Blocks on the asynchronous write.</remarks>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        /// <remarks>Blocks on the asynchronous write.</remarks>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(rented);
                Write(rented, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }

        /// <summary>
        /// Makes the file longer, with zeros, or shorter. A position past the new end moves
        /// to it.
        /// </summary>
        /// <param name="value">The new length.</param>
        /// <param name="cancellationToken">Cancels the change.</param>
        /// <returns>A task that completes when the length is set.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
        /// <exception cref="NotSupportedException">The file is not open for writing.</exception>
        /// <exception cref="IOException">The file is open to append and the length would cut into what it held.</exception>
        /// <exception cref="FatException">The volume is full, or the file would pass 4 GiB.</exception>
        public async ValueTask SetLengthAsync(long value, CancellationToken cancellationToken = default)
        {
            ThrowIfNotWriter();
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (value < m_appendStart)
                throw new IOException("A file open to append cannot be cut shorter than it was when opened.");

            await m_handle.SetLengthAsync(value, cancellationToken).ConfigureAwait(false);
            m_position = Math.Min(m_position, value);
        }

        /// <inheritdoc />
        /// <remarks>Blocks on <see cref="SetLengthAsync"/>.</remarks>
        public override void SetLength(long value)
        {
            SetLengthAsync(value).AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Writes the directory entry and the allocation table, and flushes the device.
        /// Does nothing when the file has not changed since the last flush.
        /// </summary>
        /// <param name="cancellationToken">Cancels the flush.</param>
        /// <returns>A task that completes when everything is written.</returns>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            return m_handle.CommitAsync(cancellationToken).AsTask();
        }

        /// <inheritdoc />
        /// <remarks>Blocks on <see cref="FlushAsync(CancellationToken)"/>.</remarks>
        public override void Flush()
        {
            FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Makes the next flush write the entry, as for a file just created.
        /// </summary>
        internal void MarkModified()
        {
            m_handle.MarkModified();
        }

        #endregion

        #region Position

        /// <inheritdoc />
        /// <exception cref="IOException">
        /// The position would be before the start, or, for a file open to append, before the
        /// end it had when opened.
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfClosed();

            long position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => m_position + offset,
                SeekOrigin.End => m_handle.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
            };

            if (position < 0)
                throw new IOException("An attempt was made to move the position before the beginning of the stream.");
            if (position < m_appendStart)
                throw new IOException("A file open to append cannot be written before the end it had when opened.");

            m_position = position;
            return position;
        }

        #endregion

        #region Dispose

        /// <inheritdoc />
        /// <remarks>Flushes a file open for writing first, blocking on it.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !m_disposed)
            {
                try
                {
                    if (IsWriter && !m_core.IsDisposed)
                        m_handle.CommitAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
                }
                finally
                {
                    Release();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Flushes a file open for writing, and closes the stream.
        /// </summary>
        /// <remarks>The stream is closed even when the flush fails; the failure is reported.</remarks>
        /// <returns>A task that completes when the stream is closed.</returns>
        public override async ValueTask DisposeAsync()
        {
            if (m_disposed)
                return;

            try
            {
                if (IsWriter && !m_core.IsDisposed)
                    await m_handle.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                Release();
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region Tools

        private void Release()
        {
            m_disposed = true;
            m_core.OpenFiles.Close(m_handle.Item, IsWriter);
        }

        private void ThrowIfNotWriter()
        {
            ThrowIfClosed();
            if (!IsWriter)
                throw new NotSupportedException("The file is not open for writing.");
        }

        private void ThrowIfClosed()
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);
            m_core.ThrowIfDisposed();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The file, as of when it was opened or last flushed.
        /// </summary>
        public FatDirectoryEntry Entry => m_handle.Item.Entry;

        /// <inheritdoc />
        public override bool CanRead => !m_disposed && IsReader;

        /// <inheritdoc />
        public override bool CanSeek => !m_disposed;

        /// <inheritdoc />
        public override bool CanWrite => !m_disposed && IsWriter;

        /// <inheritdoc />
        public override long Length
        {
            get
            {
                ThrowIfClosed();
                return m_handle.Length;
            }
        }

        /// <inheritdoc />
        public override long Position
        {
            get
            {
                ThrowIfClosed();
                return m_position;
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                Seek(value, SeekOrigin.Begin);
            }
        }

        private bool IsReader => (m_access & FileAccess.Read) != 0;

        private bool IsWriter => (m_access & FileAccess.Write) != 0;

        #endregion
    }
}
