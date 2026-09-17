using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// Clusters on exFAT, where the allocation bitmap says which are free and the table only
    /// links the clusters of chains.
    /// </summary>
    /// <remarks>
    /// The bitmap is found in the root the first time a cluster is taken, released or
    /// counted. A released cluster keeps whatever its table entry held, as Linux leaves it:
    /// nothing reads the entry of a free cluster.
    /// </remarks>
    internal sealed class ClusterAllocatorExFat : ClusterAllocator
    {
        #region Fields

        private readonly FatVolumeCore m_core;

        private ExFatBitmap? m_bitmap;

        #endregion

        #region Constructors

        public ClusterAllocatorExFat(FatVolumeCore core)
            : base(core.Table)
        {
            m_core = core;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Writes the table and the bitmap.
        /// </summary>
        public override async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            await Table.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (m_bitmap != null)
                await m_bitmap.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async ValueTask LoadAsync(CancellationToken cancellationToken)
        {
            if (m_bitmap != null)
                return;

            var entry = await m_core.ExFat!.GetBitmapAsync(cancellationToken).ConfigureAwait(false);
            m_bitmap = new ExFatBitmap(m_core, entry);
        }

        /// <inheritdoc />
        protected override ValueTask<uint?> FindFreeAsync(uint start, CancellationToken cancellationToken)
        {
            return m_bitmap!.FindFreeAsync(start, cancellationToken);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The chain is ended first: nothing reads the table entry of a free cluster, so a
        /// failure there takes nothing, while a bitmap bit set before a table write that
        /// fails would be a cluster nothing owns.
        /// </remarks>
        protected override async ValueTask TakeAsync(uint cluster, bool isChained, CancellationToken cancellationToken)
        {
            if (isChained)
                await EndChainAsync(cluster, cancellationToken).ConfigureAwait(false);
            await m_bitmap!.SetAsync(cluster, true, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override ValueTask FreeAsync(uint cluster, CancellationToken cancellationToken)
        {
            return m_bitmap!.SetAsync(cluster, false, cancellationToken);
        }

        /// <inheritdoc />
        protected override ValueTask<long> CountAsync(CancellationToken cancellationToken)
        {
            return m_bitmap!.CountFreeAsync(cancellationToken);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The bitmap, once it has been found.
        /// </summary>
        public ExFatBitmap? Bitmap => m_bitmap;

        #endregion
    }
}
