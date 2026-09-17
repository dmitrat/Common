using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// A small exFAT volume in memory with no boot region: one-sector clusters, the table
    /// at sector 1, the root at cluster 2. Directories and root entries are written slot by
    /// slot, so broken ones can be made as easily as sound ones.
    /// </summary>
    internal sealed class ExFatTestVolume
    {
        #region Constants

        public const uint ROOT = 2;

        public const uint BITMAP = 3;

        public const uint UPCASE = 4;

        /// <summary>Upper case for 'a' to 'z'.</summary>
        public const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        #endregion

        #region Constructors

        public ExFatTestVolume(uint clusters = 200, int fatSectors = 4, int fatCount = 1, int activeFat = 0)
        {
            Image = new FatTableImage(FatKind.ExFat, clusters, fatSectors, fatCount, isMirrored: false, activeFat: activeFat);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Writes the table, as copy <paramref name="copy"/>, and the clusters given so far.
        /// </summary>
        public async Task<BlockDeviceProbe> CreateAsync(int copy = 0)
        {
            var device = await Image.CreateDeviceAsync(copy);
            foreach (var (cluster, data) in Clusters)
                await device.WriteAsync(Image.ClusterSector(cluster), Pad(data));
            return new BlockDeviceProbe(device);
        }

        /// <summary>
        /// Puts data in clusters from <paramref name="first"/> on, padded with zeros to whole
        /// clusters; the table is not touched.
        /// </summary>
        public ExFatTestVolume Put(uint first, byte[] data)
        {
            Clusters.Add((first, data));
            return this;
        }

        /// <summary>
        /// A root holding a bitmap and an up-case table, then <paramref name="entries"/>.
        /// </summary>
        public ExFatTestVolume WithRoot(byte[] bitmap, string upcase = LETTERS, params byte[][] entries)
        {
            var table = ExFatSetBuilder.UpcaseTable(upcase);
            Image.Chain(ROOT).Chain(BITMAP).Chain(UPCASE);
            Put(BITMAP, bitmap).Put(UPCASE, table);
            return Put(ROOT, ExFatSetBuilder.Join(new[]
            {
                ExFatSetBuilder.Bitmap(BITMAP, (ulong)bitmap.Length),
                ExFatSetBuilder.Upcase(UPCASE, table)
            }.Concat(entries).ToArray()));
        }

        public FatVolumeCore Core(IBlockDevice device)
        {
            return new FatVolumeCore(device, Image.Info);
        }

        public static FatNamespace Names(FatVolumeCore core)
        {
            return new FatNamespace(core, new FatDirectoryEntry { Attributes = FatAttributes.Directory, FirstCluster = ROOT });
        }

        /// <summary>
        /// A directory entry as a parent would describe it.
        /// </summary>
        public static DirectoryItem Directory(uint firstCluster, long dataLength, bool isContiguous)
        {
            var entry = new FatDirectoryEntry { Path = "/dir", Name = "dir", Attributes = FatAttributes.Directory, FirstCluster = firstCluster };
            return new DirectoryItem(entry, ROOT, 10, 12) { IsContiguous = isContiguous, DataLength = dataLength, ValidLength = dataLength };
        }

        /// <summary>
        /// Slots that are free but not the end: a directory full of them is read to its end.
        /// </summary>
        public static byte[] FreeSlots(int count)
        {
            return Enumerable.Repeat(ExFatSetBuilder.Entry(0x05), count).SelectMany(slot => slot).ToArray();
        }

        private static byte[] Pad(byte[] data)
        {
            var padded = new byte[(data.Length + FatTableImage.SECTOR_SIZE - 1) / FatTableImage.SECTOR_SIZE * FatTableImage.SECTOR_SIZE];
            data.CopyTo(padded, 0);
            return padded;
        }

        #endregion

        #region Properties

        public FatTableImage Image { get; }

        public List<(uint Cluster, byte[] Data)> Clusters { get; } = new();

        #endregion
    }
}
