using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// The damaged volumes the checker's tests and the fsck comparison share: what each
    /// damage is, what the checker must report for it, and whether fsck sees it too.
    /// </summary>
    internal static class FatCheckerCases
    {
        #region Constants

        private const long MIB = 1L << 20;

        /// <summary>
        /// Kind, damage, the problem kinds the checker reports — none for changes that are no
        /// damage — and whether <c>fsck -n</c> says anything. fsck.exfat 1.3 says nothing of
        /// a lost cluster, a dirty volume, a percentage in use past 100, a backup boot region
        /// that differs, an up-case table's chain longer than it needs, or a directory whose
        /// valid length is not its length; fsck.vfat 4.2 prints a broken long name and a
        /// backup boot sector that differs, but exits with 0 for them.
        /// </summary>
        public static readonly object[] DAMAGE = Build();

        #endregion

        #region Functions

        /// <summary>
        /// A volume formatted in memory, mounted for writing.
        /// </summary>
        public static async Task<(BlockDeviceMemory Disk, FatVolume Volume)> FormatAsync(FatKind kind)
        {
            var (bytes, clusterSize) = kind switch
            {
                FatKind.Fat12 => (4 * MIB, 0),
                FatKind.Fat16 => (32 * MIB, 0),
                FatKind.Fat32 => (64 * MIB, 512),
                _ => (16 * MIB, 0)
            };
            var disk = new BlockDeviceMemory(bytes / 512, 512);
            await FatFormatter.FormatAsync(disk, new FatFormatOptions { Kind = kind, ClusterSize = clusterSize == 0 ? null : clusterSize });
            return (disk, await FatVolume.MountAsync(disk, TestVolumes.Options(null)));
        }

        /// <summary>
        /// A formatted volume, filled by <see cref="FatDamage.PopulateAsync"/> and damaged;
        /// nothing is mounted on it any more.
        /// </summary>
        public static async Task<BlockDeviceMemory> DamagedAsync(FatKind kind, string damage)
        {
            var (disk, volume) = await FormatAsync(kind);
            await using (volume)
                await FatDamage.PopulateAsync(volume);

            await using (var clean = await FatVolume.MountAsync(disk, TestVolumes.Options(null)))
            {
                var report = await FatChecker.CheckAsync(clean);
                Assert.That(report.IsClean, Is.True, "Before the damage: " + string.Join("; ", report.Problems.Select(problem => problem.Message)));
            }

            await using (var damaged = await FatVolume.MountAsync(disk, TestVolumes.Options(null)))
                await FatDamage.ApplyAsync(damaged, damage);
            return disk;
        }

        private static object[] Build()
        {
            var cases = new List<object>();
            foreach (var kind in new[] { FatKind.Fat12, FatKind.Fat16, FatKind.Fat32 })
            {
                bool isFat32 = kind == FatKind.Fat32;
                Add(cases, kind, "lost-cluster", true, isFat32
                    ? new[] { FatProblemKind.LostClusters, FatProblemKind.FreeCount }
                    : new[] { FatProblemKind.LostClusters });
                Add(cases, kind, "cross-link", true, FatProblemKind.CrossLinked, FatProblemKind.LengthMismatch, FatProblemKind.LostClusters);
                Add(cases, kind, "broken-chain", true, isFat32
                    ? new[] { FatProblemKind.BrokenChain, FatProblemKind.FreeCount }
                    : new[] { FatProblemKind.BrokenChain });
                Add(cases, kind, "longer-size", true, FatProblemKind.LengthMismatch);
                Add(cases, kind, "directory-loop", true, FatProblemKind.DirectoryLoop, FatProblemKind.LostClusters);
                Add(cases, kind, "dirty", true, FatProblemKind.VolumeDirty);
                Add(cases, kind, "long-name-checksum", true, FatProblemKind.BrokenEntry);
                Add(cases, kind, "dot-dot", true, FatProblemKind.DotEntries);
                Add(cases, kind, "tables-differ", true, FatProblemKind.TablesDiffer);
                Add(cases, kind, "bad-cluster", false);
                Add(cases, kind, "table-tail", false);
            }

            Add(cases, FatKind.Fat32, "fsinfo-count", true, FatProblemKind.FreeCount);
            Add(cases, FatKind.Fat32, "fat32-backup", true, FatProblemKind.BootRegion);

            Add(cases, FatKind.ExFat, "lost-cluster", false, FatProblemKind.LostClusters);
            Add(cases, FatKind.ExFat, "cross-link", true, FatProblemKind.CrossLinked, FatProblemKind.LostClusters);
            Add(cases, FatKind.ExFat, "broken-chain", true, FatProblemKind.BrokenChain);
            Add(cases, FatKind.ExFat, "longer-size", true, FatProblemKind.LengthMismatch);
            Add(cases, FatKind.ExFat, "directory-loop", true, FatProblemKind.DirectoryLoop, FatProblemKind.LostClusters);
            Add(cases, FatKind.ExFat, "dirty", false, FatProblemKind.VolumeDirty);
            Add(cases, FatKind.ExFat, "bitmap-cleared", true, FatProblemKind.BitmapMismatch);
            Add(cases, FatKind.ExFat, "set-checksum", true, FatProblemKind.BrokenEntry, FatProblemKind.LostClusters);
            Add(cases, FatKind.ExFat, "name-hash", true, FatProblemKind.WrongNameHash);
            Add(cases, FatKind.ExFat, "directory-size", true, FatProblemKind.LengthMismatch, FatProblemKind.LostClusters);
            Add(cases, FatKind.ExFat, "percent", false, FatProblemKind.PercentInUse);
            Add(cases, FatKind.ExFat, "upcase-checksum", true, FatProblemKind.Metadata);
            Add(cases, FatKind.ExFat, "exfat-backup", false, FatProblemKind.BootRegion);
            Add(cases, FatKind.ExFat, "bad-cluster", false);
            Add(cases, FatKind.ExFat, "clusterless-directory", false);
            Add(cases, FatKind.ExFat, "empty-file-cluster", true, FatProblemKind.LengthMismatch, FatProblemKind.LostClusters);
            Add(cases, FatKind.ExFat, "upcase-chain-long", false, FatProblemKind.Metadata);
            Add(cases, FatKind.ExFat, "directory-valid-length", false, FatProblemKind.LengthMismatch);
            return cases.ToArray();
        }

        private static void Add(List<object> cases, FatKind kind, string damage, bool isSeenByFsck, params FatProblemKind[] expected)
        {
            cases.Add(new TestCaseData(kind, damage, expected, isSeenByFsck).SetArgDisplayNames(kind.ToString(), damage));
        }

        #endregion
    }
}
