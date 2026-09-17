using System.Collections.Generic;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// Which file, directory or structure each cluster belongs to, as the check finds them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One number a cluster, kept in pieces that are made only where something is found, so
    /// a large volume costs four bytes for each cluster in use rather than for each it has.
    /// </para>
    /// <para>
    /// An owner is its name and the owner it lies in, not its path: a deep tree would
    /// otherwise keep a path as long as its depth for every entry. A path is put together
    /// only for a message.
    /// </para>
    /// </remarks>
    internal sealed class FatCheckOwners
    {
        #region Constants

        private const int CHUNK_BITS = 16;

        private const int CHUNK_SIZE = 1 << CHUNK_BITS;

        /// <summary>
        /// The owner of the root, which every path starts from.
        /// </summary>
        public const int ROOT = 1;

        #endregion

        #region Fields

        private readonly int[]?[] m_chunks;

        private readonly List<(int Parent, string Name)> m_owners = new() { (0, FatPath.ROOT) };

        #endregion

        #region Constructors

        public FatCheckOwners(uint clusterCount)
        {
            m_chunks = new int[]?[((long)clusterCount + CHUNK_SIZE - 1) >> CHUNK_BITS];
        }

        #endregion

        #region Functions

        /// <summary>
        /// Adds an owner.
        /// </summary>
        /// <param name="name">Its name; for something outside the tree, what it is.</param>
        /// <param name="parent">The directory it lies in, or zero for something outside the tree.</param>
        /// <returns>The owner's number, never zero.</returns>
        public int Add(string name, int parent)
        {
            m_owners.Add((parent, name));
            return m_owners.Count;
        }

        /// <summary>
        /// Gives a cluster to an owner, unless it has one.
        /// </summary>
        /// <returns>The owner it had: zero for none, and then it is the new owner's now.</returns>
        public int Claim(uint cluster, int owner)
        {
            uint index = cluster - FatTable.FIRST_CLUSTER;
            var chunk = m_chunks[index >> CHUNK_BITS] ??= new int[CHUNK_SIZE];
            ref int slot = ref chunk[index & (CHUNK_SIZE - 1)];
            if (slot != 0)
                return slot;

            slot = owner;
            Used++;
            return 0;
        }

        /// <summary>
        /// Whether a cluster belongs to anything.
        /// </summary>
        public bool IsOwned(uint cluster)
        {
            uint index = cluster - FatTable.FIRST_CLUSTER;
            return m_chunks[index >> CHUNK_BITS] is { } chunk && chunk[index & (CHUNK_SIZE - 1)] != 0;
        }

        /// <summary>
        /// What an owner is called in a message: its path, or what it is.
        /// </summary>
        public string Describe(int owner)
        {
            var names = new Stack<string>();
            var (parent, name) = m_owners[owner - 1];
            while (parent != 0)
            {
                names.Push(name);
                (parent, name) = m_owners[parent - 1];
            }

            if (name != FatPath.ROOT)
                return name;
            return names.Count == 0 ? FatPath.ROOT : FatPath.ROOT + string.Join('/', names);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The clusters that belong to something.
        /// </summary>
        public long Used { get; private set; }

        #endregion
    }
}
