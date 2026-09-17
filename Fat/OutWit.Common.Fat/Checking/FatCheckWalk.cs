namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// How far a chain or a run was followed, and why it stopped short.
    /// </summary>
    /// <param name="Count">The clusters given to its owner.</param>
    /// <param name="Problem">What broke it, as the end of a sentence; <c>null</c> when nothing did.</param>
    /// <param name="SharedCluster">The cluster found to belong to another owner, where the walk stopped; zero when none.</param>
    /// <param name="SharedWith">That other owner; zero when none.</param>
    internal readonly record struct FatCheckWalk(long Count, string? Problem, uint SharedCluster, int SharedWith)
    {
        #region Properties

        /// <summary>
        /// Whether the walk reached the end without a fault or another owner's cluster.
        /// </summary>
        public bool IsWhole => Problem == null && SharedWith == 0;

        #endregion
    }
}
