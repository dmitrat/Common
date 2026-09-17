namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// A file or directory as the kernel lists it. On FAT12/16/32 the 8.3 alias comes from
    /// mdir and the cluster runs of non-empty files from mshowfat; both are absent for
    /// names outside the BMP, which mtools cannot handle. On exFAT, dump.exfat gives the
    /// cluster runs of everything that has clusters, directories included, whether they are
    /// contiguous without a table chain, the valid data length and the name hash.
    /// </summary>
    internal sealed class ReferenceEntry
    {
        #region Constants

        public const string EMPTY_SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        #endregion

        #region Functions

        public override string ToString()
        {
            return Path;
        }

        #endregion

        #region Properties

        public string Path { get; init; } = "";

        public string Type { get; init; } = "";

        public long? Size { get; init; }

        public string? Sha256 { get; init; }

        public string? ShortName { get; init; }

        public DateTime? Modified { get; init; }

        public List<long[]>? Clusters { get; init; }

        public bool? Contiguous { get; init; }

        public long? ValidDataLength { get; init; }

        public int? NameHash { get; init; }

        public bool IsDirectory => Type == "directory";

        public string Name => Path[(Path.LastIndexOf('/') + 1)..];

        public string ContentSha256 => Sha256 ?? EMPTY_SHA256;

        #endregion
    }
}
