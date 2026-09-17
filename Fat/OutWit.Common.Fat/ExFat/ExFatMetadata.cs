using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// What an exFAT volume keeps besides files: the up-case table and the allocation bitmap,
    /// found in the root the first time they are needed, and the boot sector's dirty flag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The root's entries are followed through the table, as the specification describes
    /// them and as <c>dump.exfat</c> reads them. A volume with two tables (TexFAT) has a
    /// bitmap for each; the one of the active table counts.
    /// </para>
    /// <para>
    /// The first change marks the volume dirty in its main boot sector, and says that the
    /// percentage in use is not known; <see cref="MarkCleanAsync"/> clears the flag again —
    /// unless the volume was dirty when it was found, which is for a checker to settle — and
    /// records the percentage when the free clusters have been counted. Both fields lie
    /// outside the boot region's checksum.
    /// </para>
    /// </remarks>
    internal sealed class ExFatMetadata
    {
        #region Constants

        private const byte BITMAP_OF_SECOND_TABLE = 0x01;

        private const int MAX_UPCASE_BYTES = 0x10000 * sizeof(char);

        private const int VOLUME_FLAGS = 106;

        private const int PERCENT_IN_USE = 112;

        private const ushort VOLUME_DIRTY = 0x0002;

        private const byte PERCENT_UNKNOWN = 0xFF;

        #endregion

        #region Fields

        private readonly FatVolumeCore m_core;

        private ExFatRootEntry? m_bitmap;

        private ExFatRootEntry? m_upcaseEntry;

        private ExFatUpcaseTable? m_upcase;

        private bool m_isMarked;

        private bool m_wasDirty;

        #endregion

        #region Constructors

        public ExFatMetadata(FatVolumeCore core)
        {
            m_core = core;
        }

        #endregion

        #region Functions

        /// <summary>
        /// The volume's up-case table.
        /// </summary>
        /// <exception cref="FatException">The root has no up-case table, or it is corrupt.</exception>
        public async ValueTask<ExFatUpcaseTable> GetUpcaseTableAsync(CancellationToken cancellationToken)
        {
            if (m_upcase != null)
                return m_upcase;

            var entry = await GetUpcaseEntryAsync(cancellationToken).ConfigureAwait(false);
            if (entry.DataLength is 0 or > MAX_UPCASE_BYTES)
                throw new FatException(FatErrorKind.Corrupt, $"The up-case table records {entry.DataLength} bytes.");

            var data = new byte[entry.DataLength];
            await ReadAsync(entry, data, cancellationToken).ConfigureAwait(false);
            uint checksum = ExFatChecksum.OfUpcaseTable(data);
            if (checksum != entry.Checksum)
                throw new FatException(FatErrorKind.Corrupt,
                    $"The up-case table sums to 0x{checksum:X8}; its entry records 0x{entry.Checksum:X8}.");

            return m_upcase = ExFatUpcaseTable.Parse(data);
        }

        /// <summary>
        /// Where the up-case table lies.
        /// </summary>
        /// <exception cref="FatException">The root has no up-case table or no suitable bitmap.</exception>
        public async ValueTask<ExFatRootEntry> GetUpcaseEntryAsync(CancellationToken cancellationToken)
        {
            await FindAsync(cancellationToken).ConfigureAwait(false);
            return m_upcaseEntry!.Value;
        }

        /// <summary>
        /// Where the allocation bitmap of the active table lies.
        /// </summary>
        /// <exception cref="FatException">The root has no such bitmap, or it is too short for the volume.</exception>
        public async ValueTask<ExFatRootEntry> GetBitmapAsync(CancellationToken cancellationToken)
        {
            await FindAsync(cancellationToken).ConfigureAwait(false);
            return m_bitmap!.Value;
        }

        /// <summary>
        /// Marks the volume dirty before its first change, and the device flushed so the mark
        /// is there before the change is.
        /// </summary>
        public async ValueTask MarkDirtyAsync(CancellationToken cancellationToken)
        {
            if (m_isMarked)
                return;

            var sector = await ReadBootSectorAsync(cancellationToken).ConfigureAwait(false);
            ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(sector.AsSpan(VOLUME_FLAGS));
            m_wasDirty = (flags & VOLUME_DIRTY) != 0;
            BinaryPrimitives.WriteUInt16LittleEndian(sector.AsSpan(VOLUME_FLAGS), (ushort)(flags | VOLUME_DIRTY));
            sector[PERCENT_IN_USE] = PERCENT_UNKNOWN;
            await m_core.Device.WriteAsync(0, sector, cancellationToken).ConfigureAwait(false);
            await m_core.Device.FlushAsync(cancellationToken).ConfigureAwait(false);
            m_isMarked = true;
        }

        /// <summary>
        /// Clears the dirty flag this volume set, once everything is written.
        /// </summary>
        /// <param name="freeClusters">The free clusters, when counted, for the percentage in use.</param>
        /// <param name="cancellationToken">Cancels the change.</param>
        public async ValueTask MarkCleanAsync(long? freeClusters, CancellationToken cancellationToken)
        {
            if (!m_isMarked)
                return;

            var sector = await ReadBootSectorAsync(cancellationToken).ConfigureAwait(false);
            if (!m_wasDirty)
            {
                ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(sector.AsSpan(VOLUME_FLAGS));
                BinaryPrimitives.WriteUInt16LittleEndian(sector.AsSpan(VOLUME_FLAGS), (ushort)(flags & ~VOLUME_DIRTY));
            }

            long clusters = m_core.Info.ClusterCount;
            sector[PERCENT_IN_USE] = freeClusters is { } free ? (byte)((clusters - free) * 100 / clusters) : PERCENT_UNKNOWN;
            await m_core.Device.WriteAsync(0, sector, cancellationToken).ConfigureAwait(false);
            await m_core.Device.FlushAsync(cancellationToken).ConfigureAwait(false);
            m_isMarked = false;
        }

        private async ValueTask<byte[]> ReadBootSectorAsync(CancellationToken cancellationToken)
        {
            var sector = new byte[m_core.Info.SectorSize];
            await m_core.Device.ReadAsync(0, sector, cancellationToken).ConfigureAwait(false);
            return sector;
        }

        private async ValueTask FindAsync(CancellationToken cancellationToken)
        {
            if (m_bitmap != null && m_upcaseEntry != null)
                return;

            var info = m_core.Info;
            var root = new DirectoryItem(new FatDirectoryEntry
            {
                Path = FatPath.ROOT,
                Attributes = FatAttributes.Directory,
                FirstCluster = info.RootCluster.GetValueOrDefault()
            }, 0, -1, -1);
            var (_, critical) = await new FatDirectoryExFat(m_core, root).ReadRootEntriesAsync(cancellationToken).ConfigureAwait(false);

            byte wantedFlags = info.ActiveFat == 1 ? BITMAP_OF_SECOND_TABLE : (byte)0;
            var bitmaps = critical.Where(entry => entry.Type == ExFatEntry.ALLOCATION_BITMAP).ToList();
            var bitmap = bitmaps.FirstOrDefault(entry => (entry.Flags & BITMAP_OF_SECOND_TABLE) == wantedFlags);
            if (bitmap.Type == 0)
                throw new FatException(FatErrorKind.Corrupt, $"The root has no allocation bitmap for table {info.ActiveFat}.");
            if (bitmap.DataLength < ((ulong)info.ClusterCount + 7) / 8)
                throw new FatException(FatErrorKind.Corrupt,
                    $"The allocation bitmap records {bitmap.DataLength} bytes; {info.ClusterCount} clusters need {((ulong)info.ClusterCount + 7) / 8}.");

            var upcase = critical.FirstOrDefault(entry => entry.Type == ExFatEntry.UPCASE_TABLE);
            if (upcase.Type == 0)
                throw new FatException(FatErrorKind.Corrupt, "The root has no up-case table.");

            m_bitmap = bitmap;
            m_upcaseEntry = upcase;
        }

        /// <summary>
        /// Reads a root entry's data through its chain.
        /// </summary>
        private async ValueTask ReadAsync(ExFatRootEntry entry, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var chain = new ClusterChain(m_core.Table, entry.FirstCluster);
            int clusterSize = m_core.ClusterSize;
            int done = 0;
            while (done < buffer.Length)
            {
                long index = done / clusterSize;
                long ahead = ((long)buffer.Length - done + clusterSize - 1) / clusterSize;
                var run = await chain.FindAsync(index, ahead, cancellationToken).ConfigureAwait(false)
                          ?? throw new FatException(FatErrorKind.Corrupt,
                              $"The chain from cluster {entry.FirstCluster} ends after {chain.KnownCount} clusters; its entry records {entry.DataLength} bytes.");

                int length = (int)Math.Min(buffer.Length - done, (run.EndIndex - index) * clusterSize);
                await m_core.Device.ReadBytesAsync(m_core.ClusterOffset(run.ClusterAt(index)), buffer.Slice(done, length), cancellationToken).ConfigureAwait(false);
                done += length;
            }
        }

        #endregion
    }
}
