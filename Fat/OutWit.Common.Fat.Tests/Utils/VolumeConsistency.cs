using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Checks the allocation of a volume without Linux: every taken cluster belongs to one
    /// owner, every file's clusters are as many as the file needs, and nothing is lost. On
    /// exFAT the bitmap must mark exactly the clusters that have an owner.
    /// </summary>
    internal static class VolumeConsistency
    {
        #region Functions

        public static async Task AssertAsync(FatVolume volume)
        {
            var owners = new Dictionary<uint, string>();
            var core = volume.Core;
            if (volume.Info.Kind is FatKind.Fat32 or FatKind.ExFat)
                Claim(await volume.GetClusterRunsAsync("/", CancellationToken.None), "/", owners);

            if (core.ExFat is { } exFat)
            {
                var bitmap = await exFat.GetBitmapAsync(CancellationToken.None);
                var upcase = await exFat.GetUpcaseEntryAsync(CancellationToken.None);
                Claim(await new ClusterChain(core.Table, bitmap.FirstCluster).ReadAllAsync(CancellationToken.None), "(bitmap)", owners);
                Claim(await new ClusterChain(core.Table, upcase.FirstCluster).ReadAllAsync(CancellationToken.None), "(up-case table)", owners);
            }

            await foreach (var entry in volume.EnumerateAsync(recursive: true))
                await ClaimAsync(volume, entry, owners);

            long free = await volume.CountFreeClustersAsync();
            Assert.That(owners.Count + free, Is.EqualTo(volume.Info.ClusterCount), "clusters that belong to no entry");

            if (core.Allocator is ClusterAllocatorExFat { Bitmap: { } used })
            {
                for (uint cluster = FatTable.FIRST_CLUSTER; cluster <= core.Table.LastCluster; cluster++)
                {
                    if (await used.IsUsedAsync(cluster, CancellationToken.None) != owners.ContainsKey(cluster))
                        Assert.Fail($"The bitmap marks cluster {cluster} {(owners.ContainsKey(cluster) ? "free, but " + owners[cluster] + " holds it" : "used, and nothing holds it")}.");
                }
            }
        }

        private static async Task ClaimAsync(FatVolume volume, FatDirectoryEntry entry, Dictionary<uint, string> owners)
        {
            var runs = await volume.GetClusterRunsAsync(entry.Path, CancellationToken.None);
            Claim(runs, entry.Path, owners);

            long clusters = runs.Sum(run => run.Count);
            int clusterSize = volume.Info.ClusterSize;
            if (!entry.IsDirectory)
            {
                Assert.That(clusters, Is.EqualTo((entry.Length + clusterSize - 1) / clusterSize), $"the clusters of {entry.Path}");
                return;
            }

            if (volume.Info.Kind != FatKind.ExFat)
            {
                Assert.That(clusters, Is.GreaterThan(0), $"{entry.Path} has no clusters");
                return;
            }

            var item = (await volume.GetItemAsync(entry.Path, CancellationToken.None))!;
            Assert.That(item.DataLength % clusterSize, Is.Zero, $"the size of {entry.Path}");
            Assert.That(clusters, Is.EqualTo(item.DataLength / clusterSize), $"the clusters of {entry.Path}");
            Assert.That(item.ValidLength, Is.EqualTo(item.DataLength), $"the valid length of {entry.Path}");
        }

        private static void Claim(IEnumerable<ClusterRun> runs, string owner, Dictionary<uint, string> owners)
        {
            foreach (var run in runs)
            {
                for (long index = run.FirstIndex; index < run.EndIndex; index++)
                {
                    uint cluster = run.ClusterAt(index);
                    if (!owners.TryAdd(cluster, owner))
                        Assert.Fail($"Cluster {cluster} belongs to both {owners[cluster]} and {owner}.");
                }
            }
        }

        #endregion
    }
}
