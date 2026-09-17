namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// A partition table slot as sfdisk reports it.
    /// </summary>
    internal sealed class ReferencePartition
    {
        #region Properties

        public int Index { get; init; }

        public byte Type { get; init; }

        public bool Active { get; init; }

        public long FirstSector { get; init; }

        public long SectorCount { get; init; }

        #endregion
    }
}
