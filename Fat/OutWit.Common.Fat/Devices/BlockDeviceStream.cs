using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// A device over any seekable stream.
    /// </summary>
    /// <remarks>
    /// The device size is fixed when it is created: the stream length rounded down to
    /// whole sectors. The stream's position is moved by every call, so nothing else
    /// should use the stream while the device does.
    /// </remarks>
    public sealed class BlockDeviceStream : BlockDeviceBase
    {
        #region Fields

        private readonly Stream m_stream;

        private readonly bool m_leaveOpen;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a device over a stream.
        /// </summary>
        /// <param name="stream">A readable, seekable stream.</param>
        /// <param name="sectorSize">The sector size in bytes.</param>
        /// <param name="isReadOnly">Whether to refuse writes; a stream that cannot write is read-only regardless.</param>
        /// <param name="leaveOpen">Whether the stream stays open when the device is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The stream cannot read or seek.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The sector size is invalid.</exception>
        public BlockDeviceStream(Stream stream, int sectorSize = DEFAULT_SECTOR_SIZE, bool isReadOnly = false, bool leaveOpen = false)
            : base(sectorSize, CountSectors(stream, sectorSize), isReadOnly || !stream.CanWrite)
        {
            m_stream = stream;
            m_leaveOpen = leaveOpen;
        }

        #endregion

        #region Functions

        private static long CountSectors(Stream stream, int sectorSize)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanRead || !stream.CanSeek)
                throw new ArgumentException("The stream must be readable and seekable.", nameof(stream));

            CheckSectorSize(sectorSize);
            return stream.Length / sectorSize;
        }

        #endregion

        #region Core

        /// <inheritdoc />
        protected override async ValueTask ReadSectorsAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            m_stream.Position = sector * SectorSize;
            await m_stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async ValueTask WriteSectorsAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            m_stream.Position = sector * SectorSize;
            await m_stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Flushes the stream. A <see cref="FileStream"/> is flushed to disk, synchronously;
        /// any other stream only as far as its own flush goes.
        /// </summary>
        /// <param name="cancellationToken">Cancels the flush.</param>
        /// <returns>A task that completes when the stream is flushed.</returns>
        protected override async ValueTask FlushSectorsAsync(CancellationToken cancellationToken)
        {
            if (IsReadOnly)
                return;

            if (m_stream is FileStream file)
                file.Flush(flushToDisk: true);
            else
                await m_stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override ValueTask DisposeCoreAsync()
        {
            return m_leaveOpen ? default : m_stream.DisposeAsync();
        }

        #endregion
    }
}
