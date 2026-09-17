using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// A device backed by an image file.
    /// </summary>
    /// <remarks>
    /// I/O is positional, through <see cref="RandomAccess"/>, so there is no shared
    /// file position to keep in step. Bytes past the last whole sector are not part
    /// of the device and are left as they are.
    /// </remarks>
    public sealed class BlockDeviceFile : BlockDeviceBase
    {
        #region Fields

        private readonly FileStream m_stream;

        private readonly SafeFileHandle m_handle;

        #endregion

        #region Constructors

        private BlockDeviceFile(FileStream stream, long sectorCount, int sectorSize, bool isReadOnly)
            : base(sectorSize, sectorCount, isReadOnly)
        {
            m_stream = stream;
            m_handle = stream.SafeFileHandle;
            Path = stream.Name;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Opens an existing image file.
        /// </summary>
        /// <param name="path">The image file.</param>
        /// <param name="sectorSize">The sector size in bytes.</param>
        /// <param name="isReadOnly">Whether to open the file for reading only; other readers are then allowed.</param>
        /// <returns>The device.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The sector size is invalid.</exception>
        /// <exception cref="IOException">The file cannot be opened.</exception>
        public static BlockDeviceFile Open(string path, int sectorSize = DEFAULT_SECTOR_SIZE, bool isReadOnly = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            CheckSectorSize(sectorSize);

            var stream = OpenStream(path, FileMode.Open, isReadOnly);
            try
            {
                return new BlockDeviceFile(stream, stream.Length / sectorSize, sectorSize, isReadOnly);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates an image file of the given size, replacing any file already there.
        /// </summary>
        /// <param name="path">The image file.</param>
        /// <param name="sectorCount">The number of sectors.</param>
        /// <param name="sectorSize">The sector size in bytes.</param>
        /// <returns>The writable device; its sectors read as zeros.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The sector size or count is invalid.</exception>
        /// <exception cref="IOException">The file cannot be created.</exception>
        public static BlockDeviceFile Create(string path, long sectorCount, int sectorSize = DEFAULT_SECTOR_SIZE)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            CheckGeometry(sectorSize, sectorCount);

            var stream = OpenStream(path, FileMode.Create, false);
            try
            {
                stream.SetLength(sectorCount * sectorSize);
                return new BlockDeviceFile(stream, sectorCount, sectorSize, false);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        private static FileStream OpenStream(string path, FileMode mode, bool isReadOnly)
        {
            return new FileStream(path, new FileStreamOptions
            {
                Mode = mode,
                Access = isReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
                Share = isReadOnly ? FileShare.Read : FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.RandomAccess,
                BufferSize = 0
            });
        }

        #endregion

        #region Core

        /// <inheritdoc />
        protected override async ValueTask ReadSectorsAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            long offset = sector * SectorSize;
            while (!buffer.IsEmpty)
            {
                int read = await RandomAccess.ReadAsync(m_handle, buffer, offset, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException($"The image file ended at byte {offset}, inside sector {offset / SectorSize}.");

                buffer = buffer[read..];
                offset += read;
            }
        }

        /// <inheritdoc />
        protected override ValueTask WriteSectorsAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return RandomAccess.WriteAsync(m_handle, buffer, sector * SectorSize, cancellationToken);
        }

        /// <summary>
        /// Flushes the file to disk. The operating system call is synchronous.
        /// </summary>
        /// <param name="cancellationToken">Not observed once the flush has started.</param>
        /// <returns>A completed task.</returns>
        protected override ValueTask FlushSectorsAsync(CancellationToken cancellationToken)
        {
            if (!IsReadOnly)
                m_stream.Flush(flushToDisk: true);
            return default;
        }

        /// <inheritdoc />
        protected override ValueTask DisposeCoreAsync()
        {
            return m_stream.DisposeAsync();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The full path of the image file.
        /// </summary>
        public string Path { get; }

        #endregion
    }
}
