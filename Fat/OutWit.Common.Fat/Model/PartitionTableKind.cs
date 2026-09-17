namespace OutWit.Common.Fat.Model
{
    /// <summary>
    /// How a disk is divided.
    /// </summary>
    public enum PartitionTableKind
    {
        /// <summary>
        /// No partition table: the disk is one volume, or holds nothing recognisable.
        /// </summary>
        None,

        /// <summary>
        /// A master boot record with up to four primary partitions.
        /// </summary>
        Mbr
    }
}
