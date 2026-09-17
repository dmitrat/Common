namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// An allocation bitmap or up-case table entry, as the root directory holds it.
    /// </summary>
    /// <param name="Type">The entry type.</param>
    /// <param name="Flags">The bitmap's flags: bit 0 names the table it belongs to. Zero for the up-case table.</param>
    /// <param name="FirstCluster">Where its data starts.</param>
    /// <param name="DataLength">How long its data is, in bytes.</param>
    /// <param name="Checksum">The up-case table's checksum. Zero for the bitmap.</param>
    /// <param name="Slot">Where the entry lies in the root.</param>
    internal readonly record struct ExFatRootEntry(byte Type, byte Flags, uint FirstCluster, ulong DataLength, uint Checksum, long Slot);
}
