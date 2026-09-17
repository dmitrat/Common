using System.Buffers.Binary;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Writes master boot records slot by slot, from the specification's offsets.
    /// </summary>
    internal static class MbrBuilder
    {
        #region Functions

        public static byte[] Build(uint diskSignature, params MbrSlot[] slots)
        {
            return BuildForSectorSize(512, diskSignature, slots);
        }

        /// <summary>
        /// A table in a sector of the given size; the slots count in sectors of that size.
        /// </summary>
        public static byte[] BuildForSectorSize(int sectorSize, uint diskSignature, params MbrSlot[] slots)
        {
            var sector = new byte[sectorSize];
            BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(440), diskSignature);

            foreach (var slot in slots)
            {
                var entry = sector.AsSpan(446 + slot.Index * 16, 16);
                entry[0] = slot.Status;
                entry[4] = slot.Type;
                BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], slot.FirstSector);
                BinaryPrimitives.WriteUInt32LittleEndian(entry[12..], slot.SectorCount);
            }

            sector[510] = 0x55;
            sector[511] = 0xAA;
            return sector;
        }

        #endregion
    }
}
