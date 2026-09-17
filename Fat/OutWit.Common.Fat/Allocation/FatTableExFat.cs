using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// exFAT's table: 32-bit entries, all of whose bits count.
    /// </summary>
    /// <remarks>
    /// On exFAT the table only links clusters; whether a cluster is free is the allocation
    /// bitmap's business, and the entry of a cluster that is free, or that belongs to a file
    /// whose clusters are contiguous, means nothing. A chain is followed only where its
    /// entry says the table describes it. Values from 0xFFFFFFF8 up end a chain, as Linux
    /// reads them.
    /// </remarks>
    internal sealed class FatTableExFat : FatTable
    {
        #region Constants

        private const int ENTRY_SIZE = sizeof(uint);

        #endregion

        #region Constructors

        public FatTableExFat(IBlockDevice device, FatVolumeInfo info)
            : base(device, info)
        {
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        protected override async ValueTask<uint> ReadValueAsync(uint cluster, CancellationToken cancellationToken)
        {
            long offset = (long)cluster * ENTRY_SIZE;
            byte[] sector = await GetSectorAsync(offset, cancellationToken).ConfigureAwait(false);
            return BinaryPrimitives.ReadUInt32LittleEndian(sector.AsSpan((int)(offset % Info.SectorSize)));
        }

        /// <inheritdoc />
        protected override async ValueTask WriteValueAsync(uint cluster, uint value, CancellationToken cancellationToken)
        {
            long offset = (long)cluster * ENTRY_SIZE;
            byte[] sector = await GetSectorAsync(offset, cancellationToken).ConfigureAwait(false);
            BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan((int)(offset % Info.SectorSize)), value);
            MarkDirty(offset);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override uint EndOfChainMark => 0xFFFFFFFF;

        /// <inheritdoc />
        protected override uint EndOfChain => 0xFFFFFFF8;

        /// <inheritdoc />
        protected override uint BadCluster => 0xFFFFFFF7;

        #endregion
    }
}
