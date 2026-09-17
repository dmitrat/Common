namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Where new entries can go in a directory.
    /// </summary>
    /// <param name="Start">The first slot of the free run.</param>
    /// <param name="Available">How many free slots follow from there within the directory's clusters.</param>
    /// <param name="EndMarker">The slot holding the end marker, or -1 when the run was found before it or there is none.</param>
    internal readonly record struct FreeSlotRun(long Start, long Available, long EndMarker);
}
