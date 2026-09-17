using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// A device held in memory, for tests and for images loaded whole.
    /// </summary>
    /// <remarks>
    /// Storage is sparse: memory is taken in 1 MiB chunks, and only for chunks that
    /// have held something other than zeros. A freshly formatted volume of several
    /// gigabytes costs little more than its metadata.
    /// </remarks>
    public sealed class BlockDeviceMemory : BlockDeviceBase
    {
        #region Constants

        /// <summary>
        /// The size of one storage chunk; a multiple of every valid sector size.
        /// </summary>
        public const int CHUNK_SIZE = 1 << 20;

        #endregion

        #region Fields

        private readonly Dictionary<long, byte[]> m_chunks;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a writable device filled with zeros.
        /// </summary>
        /// <param name="sectorCount">The number of sectors.</param>
        /// <param name="sectorSize">The sector size in bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException">The sector size or count is invalid.</exception>
        public BlockDeviceMemory(long sectorCount, int sectorSize = DEFAULT_SECTOR_SIZE)
            : this(new Dictionary<long, byte[]>(), sectorCount, sectorSize, false)
        {
        }

        private BlockDeviceMemory(Dictionary<long, byte[]> chunks, long sectorCount, int sectorSize, bool isReadOnly)
            : base(sectorSize, sectorCount, isReadOnly)
        {
            m_chunks = chunks;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Loads an image from a stream that is read to its end.
        /// </summary>
        /// <param name="source">The image; it need not be seekable, so a decompressing stream will do.</param>
        /// <param name="sectorSize">The sector size in bytes.</param>
        /// <param name="isReadOnly">Whether the loaded device refuses writes.</param>
        /// <param name="cancellationToken">Cancels the load.</param>
        /// <returns>The loaded device.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The sector size is invalid.</exception>
        /// <exception cref="InvalidDataException">The image is not a whole number of sectors.</exception>
        public static async Task<BlockDeviceMemory> LoadAsync(Stream source, int sectorSize = DEFAULT_SECTOR_SIZE,
            bool isReadOnly = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            CheckSectorSize(sectorSize);

            var chunks = new Dictionary<long, byte[]>();
            long length = 0;
            var buffer = new byte[CHUNK_SIZE];

            while (true)
            {
                int read = await source.ReadAtLeastAsync(buffer, CHUNK_SIZE, false, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;

                if (buffer.AsSpan(0, read).IndexOfAnyExcept((byte)0) >= 0)
                {
                    buffer.AsSpan(read).Clear();
                    chunks[length / CHUNK_SIZE] = buffer;
                    buffer = new byte[CHUNK_SIZE];
                }

                length += read;
                if (read < CHUNK_SIZE)
                    break;
            }

            if (length % sectorSize != 0)
                throw new InvalidDataException($"The image is {length} bytes long, which is not a whole number of {sectorSize}-byte sectors.");

            return new BlockDeviceMemory(chunks, length / sectorSize, sectorSize, isReadOnly);
        }

        /// <summary>
        /// Writes the whole image, zeros included, to a stream.
        /// </summary>
        /// <param name="target">Receives the image.</param>
        /// <param name="cancellationToken">Cancels the save.</param>
        /// <returns>A task that completes when the image is written.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The device is disposed.</exception>
        public async Task SaveAsync(Stream target, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(target);
            ThrowIfDisposed();

            long length = SectorCount * SectorSize;
            var zeros = new byte[CHUNK_SIZE];

            for (long index = 0; index * CHUNK_SIZE < length; index++)
            {
                int count = (int)Math.Min(CHUNK_SIZE, length - index * CHUNK_SIZE);
                byte[] chunk = m_chunks.TryGetValue(index, out var stored) ? stored : zeros;
                await target.WriteAsync(chunk.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Core

        /// <inheritdoc />
        protected override ValueTask ReadSectorsAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Span<byte> target = buffer.Span;
            long position = sector * SectorSize;

            while (!target.IsEmpty)
            {
                int offset = (int)(position % CHUNK_SIZE);
                int count = Math.Min(target.Length, CHUNK_SIZE - offset);

                if (m_chunks.TryGetValue(position / CHUNK_SIZE, out var chunk))
                    chunk.AsSpan(offset, count).CopyTo(target);
                else
                    target[..count].Clear();

                target = target[count..];
                position += count;
            }

            return default;
        }

        /// <inheritdoc />
        protected override ValueTask WriteSectorsAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            ReadOnlySpan<byte> source = buffer.Span;
            long position = sector * SectorSize;

            while (!source.IsEmpty)
            {
                int offset = (int)(position % CHUNK_SIZE);
                int count = Math.Min(source.Length, CHUNK_SIZE - offset);
                long index = position / CHUNK_SIZE;
                ReadOnlySpan<byte> part = source[..count];

                if (!m_chunks.TryGetValue(index, out var chunk) && part.IndexOfAnyExcept((byte)0) >= 0)
                {
                    chunk = new byte[CHUNK_SIZE];
                    m_chunks[index] = chunk;
                }

                // A chunk that was never written already reads as zeros.
                if (chunk != null)
                    part.CopyTo(chunk.AsSpan(offset));

                source = source[count..];
                position += count;
            }

            return default;
        }

        /// <inheritdoc />
        protected override ValueTask DisposeCoreAsync()
        {
            m_chunks.Clear();
            return default;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The memory actually taken by the image, in bytes.
        /// </summary>
        public long AllocatedBytes => (long)m_chunks.Count * CHUNK_SIZE;

        #endregion
    }
}
