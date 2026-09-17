using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// Validation and disposal shared by the devices in this library.
    /// </summary>
    /// <remarks>
    /// Arguments are checked here, once, and synchronously; a derived device sees only
    /// requests that are non-empty, whole sectors and inside the device, and writes only
    /// when it is writable.
    /// </remarks>
    public abstract class BlockDeviceBase : IBlockDevice
    {
        #region Constants

        /// <summary>
        /// The sector size assumed when none is given.
        /// </summary>
        public const int DEFAULT_SECTOR_SIZE = 512;

        /// <summary>
        /// The smallest sector size FAT and exFAT allow.
        /// </summary>
        public const int MIN_SECTOR_SIZE = 512;

        /// <summary>
        /// The largest sector size FAT and exFAT allow.
        /// </summary>
        public const int MAX_SECTOR_SIZE = 4096;

        #endregion

        #region Fields

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initialises the geometry of the device.
        /// </summary>
        /// <param name="sectorSize">The sector size in bytes.</param>
        /// <param name="sectorCount">The number of sectors.</param>
        /// <param name="isReadOnly">Whether writes are refused.</param>
        /// <exception cref="ArgumentOutOfRangeException">The sector size or count is invalid.</exception>
        protected BlockDeviceBase(int sectorSize, long sectorCount, bool isReadOnly)
        {
            CheckGeometry(sectorSize, sectorCount);

            SectorSize = sectorSize;
            SectorCount = sectorCount;
            IsReadOnly = isReadOnly;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Tells whether a sector size is one FAT and exFAT can use.
        /// </summary>
        /// <param name="sectorSize">The size to check.</param>
        /// <returns><c>true</c> for a power of two from 512 to 4096.</returns>
        public static bool IsValidSectorSize(int sectorSize)
        {
            return sectorSize is >= MIN_SECTOR_SIZE and <= MAX_SECTOR_SIZE && (sectorSize & (sectorSize - 1)) == 0;
        }

        /// <summary>
        /// Throws unless a sector size is one FAT and exFAT can use.
        /// </summary>
        /// <param name="sectorSize">The size to check.</param>
        /// <exception cref="ArgumentOutOfRangeException">The size is not a power of two from 512 to 4096.</exception>
        protected static void CheckSectorSize(int sectorSize)
        {
            if (!IsValidSectorSize(sectorSize))
                throw new ArgumentOutOfRangeException(nameof(sectorSize), sectorSize,
                    $"The sector size must be a power of two from {MIN_SECTOR_SIZE} to {MAX_SECTOR_SIZE}.");
        }

        /// <summary>
        /// Throws unless a device of this geometry can exist: a valid sector size, and a
        /// sector count that is not negative and whose bytes a <see cref="long"/> can address.
        /// </summary>
        /// <param name="sectorSize">The sector size in bytes.</param>
        /// <param name="sectorCount">The number of sectors.</param>
        /// <exception cref="ArgumentOutOfRangeException">The size or the count is invalid.</exception>
        protected static void CheckGeometry(int sectorSize, long sectorCount)
        {
            CheckSectorSize(sectorSize);
            if (sectorCount < 0 || sectorCount > long.MaxValue / sectorSize)
                throw new ArgumentOutOfRangeException(nameof(sectorCount), sectorCount,
                    $"The sector count must be from 0 to {long.MaxValue / sectorSize} for {sectorSize}-byte sectors.");
        }

        /// <summary>
        /// Throws when the device has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The device is disposed.</exception>
        protected void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);
        }

        private void CheckRange(long sector, int length)
        {
            if (length % SectorSize != 0)
                throw new ArgumentException($"The buffer length {length} is not a multiple of the sector size {SectorSize}.", "buffer");

            long count = length / SectorSize;
            if (sector < 0 || sector > SectorCount - count)
                throw new ArgumentOutOfRangeException(nameof(sector), sector,
                    $"Sectors {sector} to {sector + count - 1} lie outside the device of {SectorCount} sectors.");
        }

        #endregion

        #region Core

        /// <summary>
        /// Reads a validated, non-empty range of sectors.
        /// </summary>
        /// <param name="sector">The first sector.</param>
        /// <param name="buffer">Receives the sectors.</param>
        /// <param name="cancellationToken">Cancels the read.</param>
        /// <returns>A task that completes when the buffer is filled.</returns>
        protected abstract ValueTask ReadSectorsAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken);

        /// <summary>
        /// Writes a validated, non-empty range of sectors to a writable device.
        /// </summary>
        /// <param name="sector">The first sector.</param>
        /// <param name="buffer">The sectors to write.</param>
        /// <param name="cancellationToken">Cancels the write.</param>
        /// <returns>A task that completes when the sectors are accepted.</returns>
        protected abstract ValueTask WriteSectorsAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

        /// <summary>
        /// Commits written data. Does nothing unless overridden.
        /// </summary>
        /// <param name="cancellationToken">Cancels the flush.</param>
        /// <returns>A task that completes when the data is durable.</returns>
        protected virtual ValueTask FlushSectorsAsync(CancellationToken cancellationToken)
        {
            return default;
        }

        /// <summary>
        /// Releases what the device owns. Does nothing unless overridden.
        /// </summary>
        /// <remarks>
        /// Called again by a later <see cref="DisposeAsync"/> if it throws: the device counts
        /// as disposed only once this completes, so an override that cannot finish — a
        /// final flush that fails — should throw before releasing anything.
        /// </remarks>
        /// <returns>A task that completes when the resources are released.</returns>
        protected virtual ValueTask DisposeCoreAsync()
        {
            return default;
        }

        #endregion

        #region IBlockDevice

        /// <inheritdoc />
        public ValueTask ReadAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            CheckRange(sector, buffer.Length);

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            return buffer.IsEmpty ? default : ReadSectorsAsync(sector, buffer, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (IsReadOnly)
                throw new NotSupportedException("The device is read-only.");
            CheckRange(sector, buffer.Length);

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            return buffer.IsEmpty ? default : WriteSectorsAsync(sector, buffer, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            return FlushSectorsAsync(cancellationToken);
        }

        #endregion

        #region IAsyncDisposable

        /// <summary>
        /// Releases the device.
        /// </summary>
        /// <remarks>
        /// When releasing fails the exception propagates and the device stays open, so
        /// nothing it still holds is lost and disposal can be retried.
        /// </remarks>
        /// <returns>A task that completes when the device is released.</returns>
        public async ValueTask DisposeAsync()
        {
            if (m_disposed)
                return;

            await DisposeCoreAsync().ConfigureAwait(false);
            m_disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public int SectorSize { get; }

        /// <inheritdoc />
        public long SectorCount { get; }

        /// <inheritdoc />
        public bool IsReadOnly { get; }

        /// <summary>
        /// Whether the device has been disposed.
        /// </summary>
        public bool IsDisposed => m_disposed;

        #endregion
    }
}
