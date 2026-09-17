namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// One partition table slot for <see cref="MbrBuilder"/>.
    /// </summary>
    internal sealed record MbrSlot(int Index, byte Type, uint FirstSector, uint SectorCount, byte Status = 0x00);
}
