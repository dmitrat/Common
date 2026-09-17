using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// The file allocation table, read and written one entry at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing is read at mount. An entry is fetched when it is asked for, together with
    /// the rest of its sector, and a few recently used table sectors are kept, so walking
    /// a chain costs one request per table sector rather than one per cluster.
    /// </para>
    /// <para>
    /// A changed sector stays in memory until <see cref="FlushAsync"/>, or until it has to
    /// make room, and is then written to every copy of the table when the copies are
    /// mirrored, or to the active copy alone when they are not. Reads come from the first
    /// copy, or from the active one.
    /// </para>
    /// </remarks>
    internal abstract class FatTable
    {
        #region Constants

        public const uint FIRST_CLUSTER = 2;

        private const int WINDOW_SECTORS = 8;

        #endregion

        #region Fields

        private readonly IBlockDevice m_device;

        private readonly List<FatTableSector> m_window = new(WINDOW_SECTORS);

        #endregion

        #region Constructors

        protected FatTable(IBlockDevice device, FatVolumeInfo info)
        {
            m_device = device;
            Info = info;
            FirstSector = info.FatOffset + (info.IsFatMirrored ? 0 : info.ActiveFat) * info.FatSectors;
            LastCluster = info.ClusterCount + FIRST_CLUSTER - 1;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Creates the table of the width the volume uses.
        /// </summary>
        /// <exception cref="ArgumentException">The kind is not one of the four.</exception>
        public static FatTable Create(IBlockDevice device, FatVolumeInfo info)
        {
            return info.Kind switch
            {
                FatKind.Fat12 => new FatTableFat12(device, info),
                FatKind.Fat16 => new FatTableFat16(device, info),
                FatKind.Fat32 => new FatTableFat32(device, info),
                FatKind.ExFat => new FatTableExFat(device, info),
                _ => throw new ArgumentException($"{info.Kind} is not a FAT variant.", nameof(info))
            };
        }

        /// <summary>
        /// Reads the entry of a cluster.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The cluster is not on the volume.</exception>
        public async ValueTask<FatTableEntry> GetAsync(uint cluster, CancellationToken cancellationToken)
        {
            CheckCluster(cluster);

            uint value = await ReadValueAsync(cluster, cancellationToken).ConfigureAwait(false);
            return new FatTableEntry(Classify(value), value);
        }

        /// <summary>
        /// Changes the entry of a cluster. The change reaches the device on the next flush.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The cluster is not on the volume.</exception>
        public ValueTask SetAsync(uint cluster, uint value, CancellationToken cancellationToken)
        {
            CheckCluster(cluster);
            return WriteValueAsync(cluster, value, cancellationToken);
        }

        /// <summary>
        /// Writes every changed sector to the table copies.
        /// </summary>
        public async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            foreach (var held in m_window)
            {
                if (held.IsDirty)
                    await WriteSectorAsync(held, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Tells whether a cluster number lies on the volume.
        /// </summary>
        public bool IsCluster(uint cluster)
        {
            return cluster >= FIRST_CLUSTER && cluster <= LastCluster;
        }

        /// <summary>
        /// Reads the raw value of a cluster's entry.
        /// </summary>
        protected abstract ValueTask<uint> ReadValueAsync(uint cluster, CancellationToken cancellationToken);

        /// <summary>
        /// Stores the raw value of a cluster's entry through <see cref="GetSectorAsync"/> and
        /// <see cref="MarkDirty"/>.
        /// </summary>
        protected abstract ValueTask WriteValueAsync(uint cluster, uint value, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the table sector that holds a byte offset into the table, reading it
        /// when it is not held.
        /// </summary>
        protected async ValueTask<byte[]> GetSectorAsync(long offset, CancellationToken cancellationToken)
        {
            return (await HoldAsync(offset / Info.SectorSize, cancellationToken).ConfigureAwait(false)).Data;
        }

        /// <summary>
        /// Marks the held sector that holds a byte offset into the table as changed.
        /// </summary>
        protected void MarkDirty(long offset)
        {
            long sector = offset / Info.SectorSize;
            foreach (var held in m_window)
            {
                if (held.Sector == sector)
                {
                    held.IsDirty = true;
                    return;
                }
            }

            throw new InvalidOperationException($"Table sector {sector} is not held.");
        }

        private async ValueTask<FatTableSector> HoldAsync(long sector, CancellationToken cancellationToken)
        {
            for (int i = 0; i < m_window.Count; i++)
            {
                var held = m_window[i];
                if (held.Sector != sector)
                    continue;

                m_window.RemoveAt(i);
                m_window.Insert(0, held);
                return held;
            }

            FatTableSector entry;
            if (m_window.Count < WINDOW_SECTORS)
            {
                entry = new FatTableSector(sector, new byte[Info.SectorSize]);
            }
            else
            {
                entry = m_window[^1];
                if (entry.IsDirty)
                    await WriteSectorAsync(entry, cancellationToken).ConfigureAwait(false);
                m_window.RemoveAt(m_window.Count - 1);
                entry.Sector = sector;
            }

            await m_device.ReadAsync(FirstSector + sector, entry.Data, cancellationToken).ConfigureAwait(false);
            m_window.Insert(0, entry);
            return entry;
        }

        private async ValueTask WriteSectorAsync(FatTableSector held, CancellationToken cancellationToken)
        {
            if (Info.IsFatMirrored)
            {
                for (int copy = 0; copy < Info.FatCount; copy++)
                    await m_device.WriteAsync(Info.FatOffset + copy * Info.FatSectors + held.Sector, held.Data, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await m_device.WriteAsync(FirstSector + held.Sector, held.Data, cancellationToken).ConfigureAwait(false);
            }

            held.IsDirty = false;
        }

        private void CheckCluster(uint cluster)
        {
            if (cluster < FIRST_CLUSTER || cluster > LastCluster)
                throw new ArgumentOutOfRangeException(nameof(cluster), cluster, $"Clusters run from {FIRST_CLUSTER} to {LastCluster}.");
        }

        private FatTableEntryKind Classify(uint value)
        {
            if (value == 0)
                return FatTableEntryKind.Free;
            if (value >= EndOfChain)
                return FatTableEntryKind.End;
            if (value == BadCluster)
                return FatTableEntryKind.Bad;

            return IsCluster(value) ? FatTableEntryKind.Link : FatTableEntryKind.Invalid;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The layout of the volume.
        /// </summary>
        public FatVolumeInfo Info { get; }

        /// <summary>
        /// The first sector of the copy that is read.
        /// </summary>
        public long FirstSector { get; }

        /// <summary>
        /// The highest cluster number on the volume.
        /// </summary>
        public uint LastCluster { get; }

        /// <summary>
        /// The value that ends a chain when written.
        /// </summary>
        public abstract uint EndOfChainMark { get; }

        /// <summary>
        /// Whether any sector has changes not yet written.
        /// </summary>
        public bool IsDirty => m_window.Exists(held => held.IsDirty);

        /// <summary>
        /// The smallest value that ends a chain.
        /// </summary>
        protected abstract uint EndOfChain { get; }

        /// <summary>
        /// The value that marks a bad cluster.
        /// </summary>
        protected abstract uint BadCluster { get; }

        #endregion
    }
}
