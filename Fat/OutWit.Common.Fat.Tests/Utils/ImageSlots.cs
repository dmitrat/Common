using System.Text;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Finds and changes directory slots in an image, for tests that damage a volume.
    /// </summary>
    internal static class ImageSlots
    {
        #region Functions

        /// <summary>
        /// Finds the short entry with the given 11 on-disk characters within a range of sectors.
        /// </summary>
        /// <returns>The byte offset of the slot on the device.</returns>
        public static async Task<long> FindAsync(IBlockDevice device, long firstSector, int sectorCount, string name)
        {
            var wanted = Encoding.ASCII.GetBytes(name);
            var buffer = new byte[sectorCount * device.SectorSize];
            await device.ReadAsync(firstSector, buffer);

            for (int offset = 0; offset < buffer.Length; offset += DirectorySlotBuilder.SLOT)
            {
                if (buffer.AsSpan(offset, 11).SequenceEqual(wanted))
                    return firstSector * device.SectorSize + offset;
            }

            throw new InvalidOperationException($"No slot named '{name}' in sectors {firstSector} to {firstSector + sectorCount - 1}.");
        }

        /// <summary>
        /// Overwrites bytes of the device.
        /// </summary>
        public static async Task WriteAsync(IBlockDevice device, long offset, byte[] data)
        {
            long sector = offset / device.SectorSize;
            int within = (int)(offset % device.SectorSize);
            int sectors = (within + data.Length + device.SectorSize - 1) / device.SectorSize;
            var buffer = new byte[sectors * device.SectorSize];

            await device.ReadAsync(sector, buffer);
            data.CopyTo(buffer, within);
            await device.WriteAsync(sector, buffer);
        }

        /// <summary>
        /// The device sector a cluster of a volume starts at.
        /// </summary>
        public static long ClusterSector(FatVolumeInfo info, long volumeSector, uint cluster)
        {
            return volumeSector + info.ClusterHeapSector + (cluster - 2L) * info.SectorsPerCluster;
        }

        #endregion
    }
}
