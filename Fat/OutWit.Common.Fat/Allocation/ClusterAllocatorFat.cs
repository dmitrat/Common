using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// Clusters on FAT12, FAT16 and FAT32, where a free cluster is one whose table entry is zero.
    /// </summary>
    /// <remarks>
    /// On FAT32 the next-free hint and the free count come from FSInfo and go back there on
    /// flush. A sector without FSInfo's three signatures is neither read nor written.
    /// </remarks>
    internal sealed class ClusterAllocatorFat : ClusterAllocator
    {
        #region Fields

        private readonly IBlockDevice m_device;

        private bool m_isLoaded;

        private bool m_hasFsInfo;

        #endregion

        #region Constructors

        public ClusterAllocatorFat(IBlockDevice device, FatTable table)
            : base(table)
        {
            m_device = device;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Writes the table and, on FAT32, FSInfo.
        /// </summary>
        public override async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            await Table.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (!m_hasFsInfo || !IsChanged)
                return;

            var info = Table.Info;
            var sector = new byte[info.SectorSize];
            long fsInfoSector = info.FsInfoSector.GetValueOrDefault();
            await m_device.ReadAsync(fsInfoSector, sector, cancellationToken).ConfigureAwait(false);
            uint freeCount = FreeCount is >= 0 and < FatFsInfo.UNKNOWN ? (uint)FreeCount.Value : FatFsInfo.UNKNOWN;
            FatFsInfo.Write(sector, freeCount, NextFree);
            await m_device.WriteAsync(fsInfoSector, sector, cancellationToken).ConfigureAwait(false);
            IsChanged = false;
        }

        /// <inheritdoc />
        protected override async ValueTask LoadAsync(CancellationToken cancellationToken)
        {
            if (m_isLoaded)
                return;

            var info = Table.Info;
            if (info.Kind == FatKind.Fat32 && info.FsInfoSector is { } fsInfoSector)
            {
                var sector = new byte[info.SectorSize];
                await m_device.ReadAsync(fsInfoSector, sector, cancellationToken).ConfigureAwait(false);
                if (FatFsInfo.IsValid(sector))
                {
                    m_hasFsInfo = true;
                    uint freeCount = FatFsInfo.ReadFreeCount(sector);
                    uint nextFree = FatFsInfo.ReadNextFree(sector);
                    FreeCount = freeCount <= info.ClusterCount ? freeCount : null;
                    NextFree = Table.IsCluster(nextFree) ? nextFree : FatTable.FIRST_CLUSTER;
                }
            }

            m_isLoaded = true;
        }

        /// <inheritdoc />
        protected override async ValueTask<uint?> FindFreeAsync(uint start, CancellationToken cancellationToken)
        {
            uint cluster = start;
            for (long examined = 0; examined < Table.Info.ClusterCount; examined++)
            {
                var entry = await Table.GetAsync(cluster, cancellationToken).ConfigureAwait(false);
                if (entry.Kind == FatTableEntryKind.Free)
                    return cluster;
                cluster = Next(cluster);
            }

            return null;
        }

        /// <inheritdoc />
        /// <remarks>The table is the free map, so a taken cluster always ends a chain at first.</remarks>
        protected override ValueTask TakeAsync(uint cluster, bool isChained, CancellationToken cancellationToken)
        {
            return Table.SetAsync(cluster, Table.EndOfChainMark, cancellationToken);
        }

        /// <inheritdoc />
        protected override ValueTask FreeAsync(uint cluster, CancellationToken cancellationToken)
        {
            return Table.SetAsync(cluster, 0, cancellationToken);
        }

        /// <inheritdoc />
        protected override async ValueTask<long> CountAsync(CancellationToken cancellationToken)
        {
            long free = 0;
            for (uint cluster = FatTable.FIRST_CLUSTER; cluster <= Table.LastCluster; cluster++)
            {
                var entry = await Table.GetAsync(cluster, cancellationToken).ConfigureAwait(false);
                if (entry.Kind == FatTableEntryKind.Free)
                    free++;
            }

            return free;
        }

        #endregion
    }
}
