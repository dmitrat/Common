namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// One sector of the allocation table held by <see cref="FatTable"/>.
    /// </summary>
    internal sealed class FatTableSector
    {
        #region Constructors

        public FatTableSector(long sector, byte[] data)
        {
            Sector = sector;
            Data = data;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The sector's position within one copy of the table.
        /// </summary>
        public long Sector { get; set; }

        public byte[] Data { get; }

        public bool IsDirty { get; set; }

        #endregion
    }
}
