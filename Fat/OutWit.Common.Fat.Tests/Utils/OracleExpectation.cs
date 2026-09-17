using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// What a volume holds according to the library, in the form <c>check_image.py</c> reads.
    /// </summary>
    internal sealed class OracleExpectation
    {
        #region Constants

        public static readonly JsonSerializerOptions OPTIONS = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        #endregion

        #region Functions

        /// <summary>
        /// Lists the volume through the library: every entry, every file's hash, and every
        /// chain; every alias on FAT, and on exFAT every entry's contiguity and valid length.
        /// </summary>
        public static async Task<OracleExpectation> FromVolumeAsync(FatVolume volume, long partitionStart = 0)
        {
            bool isExFat = volume.Info.Kind == FatKind.ExFat;
            var entries = new List<OracleEntry>();
            await foreach (var entry in volume.EnumerateAsync(recursive: true))
            {
                var item = new OracleEntry
                {
                    Path = entry.Path,
                    Type = entry.IsDirectory ? "directory" : "file",
                    ShortName = isExFat ? null : entry.ShortName,
                    Attributes = (int)(entry.Attributes & DirectorySlot.SETTABLE_ATTRIBUTES)
                };

                var runs = await volume.GetClusterRunsAsync(entry.Path, CancellationToken.None);
                var clusters = runs.Count > 0 ? runs.Select(run => new[] { run.FirstCluster, run.LastCluster }).ToList() : null;
                if (!entry.IsDirectory)
                {
                    var contents = await volume.ReadAllBytesAsync(entry.Path);
                    item.Size = contents.Length;
                    item.Sha256 = contents.Length > 0 ? Convert.ToHexStringLower(SHA256.HashData(contents)) : null;
                    item.Clusters = clusters;
                }

                if (isExFat)
                {
                    var found = (await volume.GetItemAsync(entry.Path, CancellationToken.None))!;
                    item.Clusters = clusters;
                    item.Contiguous = found.IsContiguous;
                    item.ValidDataLength = entry.IsDirectory ? null : found.ValidLength;
                }

                entries.Add(item);
            }

            var info = volume.Info;
            string? label = await volume.GetLabelAsync();
            return new OracleExpectation
            {
                Kind = info.Kind.ToString().ToLowerInvariant(),
                SectorSize = info.SectorSize,
                PartitionStart = partitionStart,
                Entries = entries,
                Volume = new OracleVolume
                {
                    Label = label,
                    BootLabel = isExFat ? null : label ?? "NO NAME",
                    VolumeSerial = info.VolumeSerial,
                    SectorsPerCluster = info.SectorsPerCluster,
                    FatSectors = info.FatSectors,
                    ClusterHeapSector = info.ClusterHeapSector,
                    ClusterCount = info.ClusterCount,
                    RootCluster = info.RootCluster,
                    RootDirectoryEntries = info.RootDirectoryEntries,
                    FreeClusters = await volume.CountFreeClustersAsync()
                }
            };
        }

        #endregion

        #region Properties

        public string Kind { get; init; } = string.Empty;

        public int SectorSize { get; init; }

        public long PartitionStart { get; init; }

        public List<OracleEntry> Entries { get; init; } = new();

        public OracleVolume? Volume { get; init; }

        #endregion
    }
}
