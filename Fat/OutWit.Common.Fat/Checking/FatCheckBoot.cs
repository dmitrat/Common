using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// The structures in front of the data: the backup boot sector or region, the dirty
    /// flag, and the copies of a mirrored table.
    /// </summary>
    /// <remarks>
    /// exFAT's backup region is compared with the main one, whose checksum was verified when
    /// the volume was found, but for the fields that may differ: the flags and the percentage
    /// in use, which the checksum leaves out as well. A backup equal to it is sound.
    /// </remarks>
    internal static class FatCheckBoot
    {
        #region Constants

        /// <summary>
        /// The part of a FAT boot sector every writer keeps in step with its backup: the
        /// parameter blocks up to the boot code, but for the dirty bit, which Linux sets in
        /// the main sector alone.
        /// </summary>
        private const int FAT_COMPARED_BYTES = 90;

        private const int VOLUME_FLAGS = 106;

        private const int PERCENT_IN_USE = 112;

        private const ushort VOLUME_DIRTY = 0x0002;

        private const int TABLE_CHUNK_SECTORS = 256;

        /// <summary>
        /// Where FAT12 and FAT16 keep the state Linux marks dirty while mounted.
        /// </summary>
        private const int FAT16_STATE = 37;

        /// <summary>
        /// Where FAT32 keeps it.
        /// </summary>
        private const int FAT32_STATE = 65;

        private const byte FAT_STATE_DIRTY = 0x01;

        #endregion

        #region Functions

        public static async ValueTask CheckAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var info = context.Core.Info;
            if (info.Kind == FatKind.ExFat)
            {
                await CheckExFatRegionAsync(context, cancellationToken).ConfigureAwait(false);
                return;
            }

            await CheckFatStateAsync(context, cancellationToken).ConfigureAwait(false);
            if (info.BackupBootSector is { } backup)
                await CheckFatBackupAsync(context, backup, cancellationToken).ConfigureAwait(false);
            if (info.IsFatMirrored && info.FatCount > 1)
                await CheckTableCopiesAsync(context, cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask CheckExFatRegionAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            int sectorSize = context.Core.Info.SectorSize;
            var regions = new byte[2 * ExFatBootSector.BOOT_REGION_SECTORS * sectorSize];
            await context.Core.Device.ReadAsync(0, regions, cancellationToken).ConfigureAwait(false);
            var main = regions.AsSpan(0, regions.Length / 2);
            var backup = regions.AsSpan(regions.Length / 2);

            ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(main[VOLUME_FLAGS..]);
            if ((flags & VOLUME_DIRTY) != 0)
                context.Report(FatProblemKind.VolumeDirty, null, null, "The volume is marked dirty: it was not closed cleanly.");

            var first = main.ToArray();
            var second = backup.ToArray();
            foreach (int offset in new[] { VOLUME_FLAGS, VOLUME_FLAGS + 1, PERCENT_IN_USE })
                first[offset] = second[offset] = 0;
            if (!first.AsSpan().SequenceEqual(second))
                context.Report(FatProblemKind.BootRegion, null, null, "The backup boot region differs from the main one.");
        }

        private static async ValueTask CheckFatStateAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var info = context.Core.Info;
            var sector = new byte[info.SectorSize];
            await context.Core.Device.ReadAsync(0, sector, cancellationToken).ConfigureAwait(false);
            byte state = sector[info.Kind == FatKind.Fat32 ? FAT32_STATE : FAT16_STATE];
            if ((state & FAT_STATE_DIRTY) != 0)
                context.Report(FatProblemKind.VolumeDirty, null, null, "The boot sector says the volume is dirty: it was not closed cleanly.");
        }

        private static async ValueTask CheckFatBackupAsync(FatCheckContext context, int backup, CancellationToken cancellationToken)
        {
            int sectorSize = context.Core.Info.SectorSize;
            var main = new byte[sectorSize];
            var copy = new byte[sectorSize];
            await context.Core.Device.ReadAsync(0, main, cancellationToken).ConfigureAwait(false);
            await context.Core.Device.ReadAsync(backup, copy, cancellationToken).ConfigureAwait(false);

            int state = context.Core.Info.Kind == FatKind.Fat32 ? FAT32_STATE : FAT16_STATE;
            main[state] &= unchecked((byte)~FAT_STATE_DIRTY);
            copy[state] &= unchecked((byte)~FAT_STATE_DIRTY);
            if (!main.AsSpan(0, FAT_COMPARED_BYTES).SequenceEqual(copy.AsSpan(0, FAT_COMPARED_BYTES)))
                context.Report(FatProblemKind.BootRegion, null, null, $"The backup boot sector {backup} differs from the main one.");
        }

        private static async ValueTask CheckTableCopiesAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var info = context.Core.Info;
            int bits = info.Kind switch { FatKind.Fat12 => 12, FatKind.Fat16 => 16, _ => 32 };
            long usedBytes = Math.Min(((info.ClusterCount + 2L) * bits + 7) / 8, info.FatSectors * info.SectorSize);
            long usedSectors = (usedBytes + info.SectorSize - 1) / info.SectorSize;
            var first = new byte[TABLE_CHUNK_SECTORS * info.SectorSize];
            var other = new byte[first.Length];

            for (int copy = 1; copy < info.FatCount; copy++)
            {
                for (long sector = 0; sector < usedSectors; sector += TABLE_CHUNK_SECTORS)
                {
                    int count = (int)Math.Min(TABLE_CHUNK_SECTORS, usedSectors - sector);
                    int length = (int)Math.Min(count * info.SectorSize, usedBytes - sector * info.SectorSize);
                    await context.Core.Device.ReadAsync(info.FatOffset + sector, first.AsMemory(0, count * info.SectorSize), cancellationToken).ConfigureAwait(false);
                    await context.Core.Device.ReadAsync(info.FatOffset + copy * info.FatSectors + sector, other.AsMemory(0, count * info.SectorSize), cancellationToken).ConfigureAwait(false);

                    int at = first.AsSpan(0, length).CommonPrefixLength(other.AsSpan(0, length));
                    if (at == length)
                        continue;

                    long cluster = (sector * info.SectorSize + at) * 8 / bits;
                    context.Report(FatProblemKind.TablesDiffer, null, cluster <= uint.MaxValue ? (uint)cluster : null,
                        $"Allocation table {copy} differs from table 0, first at the entry of cluster {cluster}.");
                    break;
                }
            }
        }

        #endregion
    }
}
