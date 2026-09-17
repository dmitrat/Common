using System.Text.Json.Serialization;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// The volume as a whole, according to the library, under the names the manifest and
    /// <c>check_image.py</c> use.
    /// </summary>
    internal sealed class OracleVolume
    {
        #region Properties

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Label { get; init; }

        /// <summary>
        /// The label a FAT boot sector keeps, "NO NAME" for none; <c>null</c> on exFAT.
        /// </summary>
        public string? BootLabel { get; init; }

        public uint VolumeSerial { get; init; }

        public int SectorsPerCluster { get; init; }

        public long FatSectors { get; init; }

        public long ClusterHeapSector { get; init; }

        public uint ClusterCount { get; init; }

        public uint? RootCluster { get; init; }

        public int? RootDirectoryEntries { get; init; }

        public long FreeClusters { get; init; }

        #endregion
    }
}
