using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// A table of 32-bit entries, of which the low 28 bits count; the high four are
    /// reserved and ignored on reading.
    /// </summary>
    internal sealed class FatTableFat32 : FatTable
    {
        #region Constants

        private const int ENTRY_SIZE = sizeof(uint);

        private const uint MASK = 0x0FFFFFFF;

        #endregion

        #region Constructors

        public FatTableFat32(IBlockDevice device, FatVolumeInfo info)
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
            return BinaryPrimitives.ReadUInt32LittleEndian(sector.AsSpan((int)(offset % Info.SectorSize))) & MASK;
        }

        /// <inheritdoc />
        /// <remarks>The four reserved high bits are kept as they are.</remarks>
        protected override async ValueTask WriteValueAsync(uint cluster, uint value, CancellationToken cancellationToken)
        {
            long offset = (long)cluster * ENTRY_SIZE;
            byte[] sector = await GetSectorAsync(offset, cancellationToken).ConfigureAwait(false);
            var slot = sector.AsSpan((int)(offset % Info.SectorSize));
            uint kept = BinaryPrimitives.ReadUInt32LittleEndian(slot) & ~MASK;
            BinaryPrimitives.WriteUInt32LittleEndian(slot, kept | (value & MASK));
            MarkDirty(offset);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override uint EndOfChainMark => 0x0FFFFFFF;

        /// <inheritdoc />
        protected override uint EndOfChain => 0x0FFFFFF8;

        /// <inheritdoc />
        protected override uint BadCluster => 0x0FFFFFF7;

        #endregion
    }
}
