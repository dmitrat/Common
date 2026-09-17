using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// A directory still to be read, by its name, with its owner; or, with no directory,
    /// the mark that the walk has left the one starting at <see cref="ExitCluster"/>.
    /// </summary>
    internal sealed record FatCheckVisit(DirectoryItem? Directory, int Owner, bool IsChainReported, uint ExitCluster = 0);
}
