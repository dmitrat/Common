using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// A table of 12-bit entries, packed three bytes to two entries.
    /// </summary>
    /// <remarks>
    /// Cluster <c>n</c> starts at byte <c>n + n / 2</c>: an even cluster takes the low 12
    /// bits of the two bytes there, an odd one the high 12. The two bytes may lie in
    /// different sectors.
    /// </remarks>
    internal sealed class FatTableFat12 : FatTable
    {
        #region Constants

        private const uint MASK = 0x0FFF;

        #endregion

        #region Constructors

        public FatTableFat12(IBlockDevice device, FatVolumeInfo info)
            : base(device, info)
        {
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        protected override async ValueTask<uint> ReadValueAsync(uint cluster, CancellationToken cancellationToken)
        {
            long offset = cluster + cluster / 2;
            int sectorSize = Info.SectorSize;

            byte[] first = await GetSectorAsync(offset, cancellationToken).ConfigureAwait(false);
            byte low = first[offset % sectorSize];
            byte[] second = await GetSectorAsync(offset + 1, cancellationToken).ConfigureAwait(false);
            byte high = second[(offset + 1) % sectorSize];

            uint pair = (uint)(low | high << 8);
            return (cluster & 1) == 0 ? pair & MASK : pair >> 4;
        }

        /// <inheritdoc />
        /// <remarks>The neighbouring entry's nibble in the shared byte is kept.</remarks>
        protected override async ValueTask WriteValueAsync(uint cluster, uint value, CancellationToken cancellationToken)
        {
            long offset = cluster + cluster / 2;
            int sectorSize = Info.SectorSize;
            value &= MASK;

            byte[] first = await GetSectorAsync(offset, cancellationToken).ConfigureAwait(false);
            byte[] second = await GetSectorAsync(offset + 1, cancellationToken).ConfigureAwait(false);
            int low = (int)(offset % sectorSize);
            int high = (int)((offset + 1) % sectorSize);

            if ((cluster & 1) == 0)
            {
                first[low] = (byte)value;
                second[high] = (byte)((second[high] & 0xF0) | (int)(value >> 8));
            }
            else
            {
                first[low] = (byte)((first[low] & 0x0F) | (int)((value << 4) & 0xF0));
                second[high] = (byte)(value >> 4);
            }

            MarkDirty(offset);
            MarkDirty(offset + 1);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override uint EndOfChainMark => 0x0FFF;

        /// <inheritdoc />
        protected override uint EndOfChain => 0x0FF8;

        /// <inheritdoc />
        protected override uint BadCluster => 0x0FF7;

        #endregion
    }
}
