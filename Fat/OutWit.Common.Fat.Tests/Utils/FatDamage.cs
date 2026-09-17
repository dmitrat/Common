using System.Buffers.Binary;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Damage done on purpose to a volume <see cref="PopulateAsync"/> filled, one structure
    /// at a time, straight on the device — for the checker's tests and for comparing its
    /// findings with fsck's.
    /// </summary>
    internal static class FatDamage
    {
        #region Constants

        public const string FRAGMENTED = "/fragmented.bin";

        public const string FIRST = "/first.bin";

        public const string SECOND = "/second.bin";

        public const string LONG_NAME = "/A long file name.txt";

        public const string DIRECTORY = "/dir";

        public const string INNER = "/dir/inner";

        public const string EMPTY = "/empty";

        public const string ZERO = "/zero.bin";

        private const int FAT_STATE_FAT16 = 37;

        private const int FAT_STATE_FAT32 = 65;

        private const int EXFAT_VOLUME_FLAGS = 106;

        private const int EXFAT_PERCENT_IN_USE = 112;

        private const int EXFAT_SERIAL = 100;

        private const int FS_INFO_FREE_COUNT = 488;

        private const int FAT32_BACKUP_BOOT = 6;

        #endregion

        #region Functions

        /// <summary>
        /// Files and directories for the damage to fall on: a file whose chain is not
        /// contiguous, two plain files, a long name, a directory within a directory, an empty
        /// directory and an empty file.
        /// </summary>
        public static async Task PopulateAsync(FatVolume volume)
        {
            int clusterSize = volume.Info.ClusterSize;
            await volume.WriteAllBytesAsync(FRAGMENTED, FatVolumeOracleTests.Pattern("frg", clusterSize));
            await volume.WriteAllBytesAsync(FIRST, FatVolumeOracleTests.Pattern("fst", 3 * clusterSize + 1));
            await volume.WriteAllBytesAsync(SECOND, FatVolumeOracleTests.Pattern("snd", 2 * clusterSize));
            await using (var stream = await volume.OpenAsync(FRAGMENTED, FileMode.Append, FileAccess.Write))
                await stream.WriteAsync(FatVolumeOracleTests.Pattern("frg2", 2 * clusterSize));
            await volume.WriteAllBytesAsync(LONG_NAME, FatVolumeOracleTests.Pattern("lfn", 100));
            await volume.CreateDirectoryAsync(INNER);
            await volume.WriteAllBytesAsync(INNER + "/deep.txt", FatVolumeOracleTests.Pattern("dp", 10));
            await volume.WriteAllBytesAsync(DIRECTORY + "/other.bin", FatVolumeOracleTests.Pattern("oth", clusterSize + 5));
            await volume.CreateDirectoryAsync(EMPTY);
            await volume.WriteAllBytesAsync(ZERO, Array.Empty<byte>());
            await volume.FlushAsync();
        }

        /// <summary>
        /// The damage one test case does.
        /// </summary>
        public static Task ApplyAsync(FatVolume volume, string damage)
        {
            bool isExFat = volume.Info.Kind == FatKind.ExFat;
            return damage switch
            {
                "lost-cluster" => LoseClusterAsync(volume),
                "cross-link" => SetFirstClusterAsync(volume, SECOND, FIRST),
                "broken-chain" => BreakChainAsync(volume),
                "longer-size" => GrowLengthAsync(volume, FRAGMENTED, volume.Info.ClusterSize),
                "directory-loop" => SetFirstClusterAsync(volume, INNER, DIRECTORY),
                "dirty" => PatchAsync(volume, isExFat ? EXFAT_VOLUME_FLAGS : StateOffset(volume), value => (byte)(value | (isExFat ? 0x02 : 0x01))),
                "bitmap-cleared" => ClearBitmapAsync(volume),
                "set-checksum" => PatchSlotAsync(volume, SECOND, 0, ExFatEntry.SET_CHECKSUM, value => (byte)(value ^ 0x55)),
                "name-hash" => PatchSetAsync(volume, SECOND, (set, _) => set[ExFatEntry.SIZE + ExFatEntry.NAME_HASH] ^= 0x34),
                "directory-size" => GrowLengthAsync(volume, DIRECTORY, 1),
                "percent" => PatchAsync(volume, EXFAT_PERCENT_IN_USE, _ => 150),
                "upcase-checksum" => BreakUpcaseChecksumAsync(volume),
                "exfat-backup" => PatchAsync(volume, 12L * volume.Info.SectorSize + EXFAT_SERIAL, value => (byte)(value ^ 0xFF)),
                "long-name-checksum" => PatchSlotAsync(volume, LONG_NAME, 0, DirectorySlot.LONG_CHECKSUM, value => (byte)(value ^ 0xFF)),
                "dot-dot" => BreakDotDotAsync(volume),
                "tables-differ" => BreakSecondTableAsync(volume),
                "fsinfo-count" => PatchAsync(volume, volume.Info.FsInfoSector!.Value * (long)volume.Info.SectorSize + FS_INFO_FREE_COUNT, value => (byte)(value - 1)),
                "bad-cluster" => MarkBadAsync(volume),
                "table-tail" => BreakTableTailAsync(volume),
                "clusterless-directory" => DropDirectoryClusterAsync(volume),
                "empty-file-cluster" => GiveEmptyFileAClusterAsync(volume),
                "upcase-chain-long" => LengthenUpcaseChainAsync(volume),
                "directory-valid-length" => ChangeEntryAsync(volume, EMPTY, (set, _) =>
                    BinaryPrimitives.WriteUInt64LittleEndian(set.AsSpan(ExFatEntry.SIZE + ExFatEntry.VALID_DATA_LENGTH), 0)),
                "fat32-backup" => PatchAsync(volume, FAT32_BACKUP_BOOT * (long)volume.Info.SectorSize + 3, value => (byte)(value ^ 0x20)),
                _ => throw new ArgumentOutOfRangeException(nameof(damage), damage, "No such damage.")
            };
        }

        private static async Task LoseClusterAsync(FatVolume volume)
        {
            var core = volume.Core;
            uint cluster = core.Table.LastCluster;
            if (core.ExFat != null)
            {
                var bitmap = new ExFatBitmap(core, await core.ExFat.GetBitmapAsync(CancellationToken.None));
                Assert.That(await bitmap.IsUsedAsync(cluster, CancellationToken.None), Is.False);
                await bitmap.SetAsync(cluster, true, CancellationToken.None);
                await bitmap.FlushAsync(CancellationToken.None);
                return;
            }

            Assert.That((await core.Table.GetAsync(cluster, CancellationToken.None)).Kind, Is.EqualTo(FatTableEntryKind.Free));
            await core.Table.SetAsync(cluster, core.Table.EndOfChainMark, CancellationToken.None);
            await core.Table.FlushAsync(CancellationToken.None);
        }

        /// <summary>
        /// Marks a free cluster bad, as a surface scan does: in the table, one fewer free in
        /// FSInfo, and on exFAT in the bitmap too, so that nothing takes it.
        /// </summary>
        private static async Task MarkBadAsync(FatVolume volume)
        {
            var core = volume.Core;
            uint cluster = core.Table.LastCluster - 1;
            uint bad = volume.Info.Kind switch { FatKind.Fat12 => 0xFF7, FatKind.Fat16 => 0xFFF7, FatKind.Fat32 => 0x0FFFFFF7, _ => 0xFFFFFFF7 };
            Assert.That((await core.Table.GetAsync(cluster, CancellationToken.None)).Kind, Is.EqualTo(FatTableEntryKind.Free));
            await core.Table.SetAsync(cluster, bad, CancellationToken.None);
            await core.Table.FlushAsync(CancellationToken.None);
            if (volume.Info.FsInfoSector is { } fsInfo)
            {
                var count = new byte[sizeof(uint)];
                long offset = fsInfo * (long)volume.Info.SectorSize + FS_INFO_FREE_COUNT;
                await core.Device.ReadBytesAsync(offset, count);
                BinaryPrimitives.WriteUInt32LittleEndian(count, BinaryPrimitives.ReadUInt32LittleEndian(count) - 1);
                await core.Device.WriteBytesAsync(offset, count);
            }

            if (core.ExFat == null)
                return;

            var bitmap = new ExFatBitmap(core, await core.ExFat.GetBitmapAsync(CancellationToken.None));
            await bitmap.SetAsync(cluster, true, CancellationToken.None);
            await bitmap.FlushAsync(CancellationToken.None);
        }

        /// <summary>
        /// Changes the last byte of the second table copy, past the entries of the last cluster.
        /// </summary>
        private static async Task BreakTableTailAsync(FatVolume volume)
        {
            var info = volume.Info;
            int bits = info.Kind switch { FatKind.Fat12 => 12, FatKind.Fat16 => 16, _ => 32 };
            long usedBytes = ((info.ClusterCount + 2L) * bits + 7) / 8;
            Assert.That(usedBytes, Is.LessThan(info.FatSectors * info.SectorSize), "The table must have a tail.");
            await PatchAsync(volume, (info.FatOffset + 2 * info.FatSectors) * info.SectorSize - 1, value => (byte)(value ^ 0x5A));
        }

        /// <summary>
        /// Makes the empty directory one of no clusters and no length, and frees its cluster.
        /// </summary>
        private static async Task DropDirectoryClusterAsync(FatVolume volume)
        {
            var core = volume.Core;
            uint cluster = (await volume.GetItemAsync(EMPTY, CancellationToken.None))!.Entry.FirstCluster;
            await PatchSetAsync(volume, EMPTY, (set, _) =>
            {
                var stream = set.AsSpan(ExFatEntry.SIZE, ExFatEntry.SIZE);
                stream[ExFatEntry.SECONDARY_FLAGS] = ExFatEntry.ALLOCATION_POSSIBLE;
                BinaryPrimitives.WriteUInt32LittleEndian(stream[ExFatEntry.FIRST_CLUSTER..], 0);
                BinaryPrimitives.WriteUInt64LittleEndian(stream[ExFatEntry.DATA_LENGTH..], 0);
                BinaryPrimitives.WriteUInt64LittleEndian(stream[ExFatEntry.VALID_DATA_LENGTH..], 0);
            });
            var bitmap = new ExFatBitmap(core, await core.ExFat!.GetBitmapAsync(CancellationToken.None));
            await bitmap.SetAsync(cluster, false, CancellationToken.None);
            await bitmap.FlushAsync(CancellationToken.None);
        }

        /// <summary>
        /// Gives the empty file a first cluster, marked used, as if its length had been cut
        /// to zero and its clusters kept.
        /// </summary>
        private static async Task GiveEmptyFileAClusterAsync(FatVolume volume)
        {
            var core = volume.Core;
            uint cluster = core.Table.LastCluster;
            await PatchSetAsync(volume, ZERO, (set, _) =>
            {
                var stream = set.AsSpan(ExFatEntry.SIZE, ExFatEntry.SIZE);
                stream[ExFatEntry.SECONDARY_FLAGS] = ExFatEntry.ALLOCATION_POSSIBLE | ExFatEntry.NO_FAT_CHAIN;
                BinaryPrimitives.WriteUInt32LittleEndian(stream[ExFatEntry.FIRST_CLUSTER..], cluster);
            });
            var bitmap = new ExFatBitmap(core, await core.ExFat!.GetBitmapAsync(CancellationToken.None));
            await bitmap.SetAsync(cluster, true, CancellationToken.None);
            await bitmap.FlushAsync(CancellationToken.None);
        }

        /// <summary>
        /// Adds a cluster to the up-case table's chain, marked used, which its length does not
        /// need.
        /// </summary>
        private static async Task LengthenUpcaseChainAsync(FatVolume volume)
        {
            var core = volume.Core;
            var entry = await core.ExFat!.GetUpcaseEntryAsync(CancellationToken.None);
            var runs = await new ClusterChain(core.Table, entry.FirstCluster).ReadAllAsync(CancellationToken.None);
            uint extra = core.Table.LastCluster;
            await core.Table.SetAsync(runs[^1].LastCluster, extra, CancellationToken.None);
            await core.Table.SetAsync(extra, core.Table.EndOfChainMark, CancellationToken.None);
            await core.Table.FlushAsync(CancellationToken.None);
            var bitmap = new ExFatBitmap(core, await core.ExFat.GetBitmapAsync(CancellationToken.None));
            await bitmap.SetAsync(extra, true, CancellationToken.None);
            await bitmap.FlushAsync(CancellationToken.None);
        }

        private static async Task ClearBitmapAsync(FatVolume volume)
        {
            var core = volume.Core;
            var bitmap = new ExFatBitmap(core, await core.ExFat!.GetBitmapAsync(CancellationToken.None));
            uint cluster = (await volume.GetItemAsync(FIRST, CancellationToken.None))!.Entry.FirstCluster;
            await bitmap.SetAsync(cluster, false, CancellationToken.None);
            await bitmap.FlushAsync(CancellationToken.None);
        }

        private static async Task BreakChainAsync(FatVolume volume)
        {
            var runs = await volume.GetClusterRunsAsync(FRAGMENTED, CancellationToken.None);
            Assert.That(runs, Has.Count.GreaterThan(1), "The file must not be contiguous.");
            await volume.Core.Table.SetAsync(runs[^1].LastCluster, 0, CancellationToken.None);
            await volume.Core.Table.FlushAsync(CancellationToken.None);
        }

        private static async Task BreakSecondTableAsync(FatVolume volume)
        {
            var info = volume.Info;
            uint cluster = (await volume.GetItemAsync(FIRST, CancellationToken.None))!.Entry.FirstCluster;
            int bits = info.Kind switch { FatKind.Fat12 => 12, FatKind.Fat16 => 16, _ => 32 };
            long offset = (info.FatOffset + info.FatSectors) * info.SectorSize + (long)cluster * bits / 8;
            await PatchAsync(volume, offset, value => (byte)(value ^ 0x01));
        }

        private static async Task BreakDotDotAsync(FatVolume volume)
        {
            var inner = (await volume.GetItemAsync(INNER, CancellationToken.None))!.Entry.FirstCluster;
            long offset = volume.Core.ClusterOffset(inner) + DirectorySlot.SIZE + DirectorySlot.FIRST_CLUSTER_LOW;
            var bytes = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)inner);
            await volume.Core.Device.WriteBytesAsync(offset, bytes);
        }

        private static async Task BreakUpcaseChecksumAsync(FatVolume volume)
        {
            var entry = await volume.Core.ExFat!.GetUpcaseEntryAsync(CancellationToken.None);
            var root = volume.Core.OpenDirectory(volume.Names.Root);
            long offset = await root.GetSlotOffsetAsync(entry.Slot, CancellationToken.None);
            await PatchAsync(volume, offset + ExFatEntry.UPCASE_CHECKSUM, value => (byte)(value ^ 0x01));
        }

        private static async Task SetFirstClusterAsync(FatVolume volume, string path, string takenFrom)
        {
            uint cluster = (await volume.GetItemAsync(takenFrom, CancellationToken.None))!.Entry.FirstCluster;
            await ChangeEntryAsync(volume, path, (entry, isExFat) =>
            {
                if (isExFat)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(ExFatEntry.SIZE + ExFatEntry.FIRST_CLUSTER), cluster);
                    return;
                }

                BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(DirectorySlot.FIRST_CLUSTER_LOW), (ushort)cluster);
                BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(DirectorySlot.FIRST_CLUSTER_HIGH), (ushort)(cluster >> 16));
            });
        }

        private static Task GrowLengthAsync(FatVolume volume, string path, int bytes)
        {
            return ChangeEntryAsync(volume, path, (entry, isExFat) =>
            {
                var span = entry.AsSpan(isExFat ? ExFatEntry.SIZE + ExFatEntry.DATA_LENGTH : DirectorySlot.FILE_SIZE);
                if (isExFat)
                    BinaryPrimitives.WriteUInt64LittleEndian(span, BinaryPrimitives.ReadUInt64LittleEndian(span) + (ulong)bytes);
                else
                    BinaryPrimitives.WriteUInt32LittleEndian(span, BinaryPrimitives.ReadUInt32LittleEndian(span) + (uint)bytes);
            });
        }

        /// <summary>
        /// Changes the short entry of a FAT file, or the whole entry set of an exFAT one with
        /// its checksum made right again.
        /// </summary>
        private static Task ChangeEntryAsync(FatVolume volume, string path, Action<byte[], bool> change)
        {
            if (volume.Info.Kind == FatKind.ExFat)
                return PatchSetAsync(volume, path, (set, _) => change(set, true));

            return PatchSetAsync(volume, path, (set, item) =>
            {
                int offset = (int)(item.LastSlot - item.FirstSlot) * DirectorySlot.SIZE;
                var entry = set.AsSpan(offset, DirectorySlot.SIZE).ToArray();
                change(entry, false);
                entry.CopyTo(set, offset);
            });
        }

        private static async Task PatchSetAsync(FatVolume volume, string path, Action<byte[], DirectoryItem> change)
        {
            var item = (await volume.GetItemAsync(path, CancellationToken.None))!;
            var (parent, _) = await volume.Names.RequireParentAsync(path, CancellationToken.None);
            var directory = volume.Core.OpenDirectory(parent);
            int count = (int)(item.LastSlot - item.FirstSlot + 1);
            var offsets = new long[count];
            var set = new byte[count * DirectorySlot.SIZE];
            for (int i = 0; i < count; i++)
            {
                offsets[i] = await directory.GetSlotOffsetAsync(item.FirstSlot + i, CancellationToken.None);
                await volume.Core.Device.ReadBytesAsync(offsets[i], set.AsMemory(i * DirectorySlot.SIZE, DirectorySlot.SIZE));
            }

            change(set, item);
            if (volume.Info.Kind == FatKind.ExFat)
                BinaryPrimitives.WriteUInt16LittleEndian(set.AsSpan(ExFatEntry.SET_CHECKSUM), ExFatChecksum.OfEntrySet(set));

            for (int i = 0; i < count; i++)
                await volume.Core.Device.WriteBytesAsync(offsets[i], set.AsMemory(i * DirectorySlot.SIZE, DirectorySlot.SIZE));
        }

        private static async Task PatchSlotAsync(FatVolume volume, string path, int slot, int offset, Func<byte, byte> change)
        {
            var item = (await volume.GetItemAsync(path, CancellationToken.None))!;
            var (parent, _) = await volume.Names.RequireParentAsync(path, CancellationToken.None);
            long start = await volume.Core.OpenDirectory(parent).GetSlotOffsetAsync(item.FirstSlot + slot, CancellationToken.None);
            await PatchAsync(volume, start + offset, change);
        }

        private static async Task PatchAsync(FatVolume volume, long offset, Func<byte, byte> change)
        {
            var value = new byte[1];
            await volume.Core.Device.ReadBytesAsync(offset, value);
            value[0] = change(value[0]);
            await volume.Core.Device.WriteBytesAsync(offset, value);
        }

        private static int StateOffset(FatVolume volume)
        {
            return volume.Info.Kind == FatKind.Fat32 ? FAT_STATE_FAT32 : FAT_STATE_FAT16;
        }

        #endregion
    }
}
