using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// A table of 16-bit entries.
    /// </summary>
    internal sealed class FatTableFat16 : FatTable
    {
        #region Constants

        private const int ENTRY_SIZE = sizeof(ushort);

        #endregion

        #region Constructors

        public FatTableFat16(IBlockDevice device, FatVolumeInfo info)
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
            return BinaryPrimitives.ReadUInt16LittleEndian(sector.AsSpan((int)(offset % Info.SectorSize)));
        }

        /// <inheritdoc />
        protected override async ValueTask WriteValueAsync(uint cluster, uint value, CancellationToken cancellationToken)
        {
            long offset = (long)cluster * ENTRY_SIZE;
            byte[] sector = await GetSectorAsync(offset, cancellationToken).ConfigureAwait(false);
            BinaryPrimitives.WriteUInt16LittleEndian(sector.AsSpan((int)(offset % Info.SectorSize)), (ushort)value);
            MarkDirty(offset);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override uint EndOfChainMark => 0xFFFF;

        /// <inheritdoc />
        protected override uint EndOfChain => 0xFFF8;

        /// <inheritdoc />
        protected override uint BadCluster => 0xFFF7;

        #endregion
    }
}
