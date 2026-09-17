namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// One sector held by <see cref="BlockDeviceCached"/>.
    /// </summary>
    internal sealed class BlockDeviceCachedEntry
    {
        #region Constructors

        public BlockDeviceCachedEntry(long sector, byte[] data)
        {
            Sector = sector;
            Data = data;
        }

        #endregion

        #region Properties

        public long Sector { get; }

        public byte[] Data { get; }

        public bool IsDirty { get; set; }

        #endregion
    }
}
