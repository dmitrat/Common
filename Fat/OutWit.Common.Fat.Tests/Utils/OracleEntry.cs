namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// One entry of an <see cref="OracleExpectation"/>.
    /// </summary>
    internal sealed class OracleEntry
    {
        #region Properties

        public string Path { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public long? Size { get; set; }

        public string? Sha256 { get; set; }

        public string? ShortName { get; init; }

        public List<uint[]>? Clusters { get; set; }

        public bool? Contiguous { get; set; }

        public long? ValidDataLength { get; set; }

        /// <summary>
        /// The read-only, hidden, system and archive bits.
        /// </summary>
        public int? Attributes { get; set; }

        #endregion
    }
}
