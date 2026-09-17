namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// Consecutive clusters that follow one another in a chain.
    /// </summary>
    /// <param name="FirstIndex">The position of the first of them in the chain, from zero.</param>
    /// <param name="FirstCluster">The cluster number of the first of them.</param>
    /// <param name="Count">How many there are.</param>
    internal readonly record struct ClusterRun(long FirstIndex, uint FirstCluster, long Count)
    {
        #region Functions

        /// <summary>
        /// The cluster number at a position of the chain that lies in this run.
        /// </summary>
        public uint ClusterAt(long index)
        {
            return (uint)(FirstCluster + (index - FirstIndex));
        }

        #endregion

        #region Properties

        /// <summary>
        /// The position just past the run.
        /// </summary>
        public long EndIndex => FirstIndex + Count;

        /// <summary>
        /// The cluster number of the last cluster of the run.
        /// </summary>
        public uint LastCluster => (uint)(FirstCluster + Count - 1);

        #endregion
    }
}
