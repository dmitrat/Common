using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// A window onto a consecutive range of another device's sectors — one partition of
    /// a disk, seen as a device of its own.
    /// </summary>
    /// <remarks>
    /// The window does not own the disk: disposing it leaves the disk open, so several
    /// partitions of one disk can be used and disposed independently. A flush is passed
    /// on to the disk.
    /// </remarks>
    public sealed class BlockDevicePartition : BlockDeviceBase
    {
        #region Fields

        private readonly IBlockDevice m_disk;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a window onto part of a disk.
        /// </summary>
        /// <param name="disk">The device the window looks into.</param>
        /// <param name="firstSector">The disk sector that becomes sector zero of the window.</param>
        /// <param name="sectorCount">The number of sectors in the window.</param>
        /// <exception cref="ArgumentNullException"><paramref name="disk"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The range does not lie within the disk.</exception>
        public BlockDevicePartition(IBlockDevice disk, long firstSector, long sectorCount)
            : base(Require(disk).SectorSize, CheckExtent(disk, firstSector, sectorCount), disk.IsReadOnly)
        {
            m_disk = disk;
            FirstSector = firstSector;
        }

        #endregion

        #region Functions

        private static IBlockDevice Require(IBlockDevice disk)
        {
            ArgumentNullException.ThrowIfNull(disk);
            return disk;
        }

        private static long CheckExtent(IBlockDevice disk, long firstSector, long sectorCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(firstSector);
            ArgumentOutOfRangeException.ThrowIfNegative(sectorCount);

            if (firstSector > disk.SectorCount - sectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorCount), sectorCount,
                    $"Sectors {firstSector} to {firstSector + sectorCount - 1} lie outside the disk of {disk.SectorCount} sectors.");

            return sectorCount;
        }

        #endregion

        #region Core

        /// <inheritdoc />
        protected override ValueTask ReadSectorsAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return m_disk.ReadAsync(FirstSector + sector, buffer, cancellationToken);
        }

        /// <inheritdoc />
        protected override ValueTask WriteSectorsAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return m_disk.WriteAsync(FirstSector + sector, buffer, cancellationToken);
        }

        /// <inheritdoc />
        protected override ValueTask FlushSectorsAsync(CancellationToken cancellationToken)
        {
            return m_disk.FlushAsync(cancellationToken);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The device the window looks into.
        /// </summary>
        public IBlockDevice Disk => m_disk;

        /// <summary>
        /// The disk sector that is sector zero of the window.
        /// </summary>
        public long FirstSector { get; }

        #endregion
    }
}
