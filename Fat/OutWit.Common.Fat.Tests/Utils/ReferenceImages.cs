using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Loads the manifest and opens reference images as in-memory devices.
    /// </summary>
    internal static class ReferenceImages
    {
        #region Constants

        public const string FIXTURES_DIRECTORY = "Fixtures";

        public const string MANIFEST_FILE = "manifest.json";

        private static readonly JsonSerializerOptions OPTIONS = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly Lazy<ReferenceManifest> MANIFEST = new(Load);

        #endregion

        #region Functions

        public static ReferenceImage Get(string name)
        {
            return Manifest.Images.Single(image => image.Name == name);
        }

        /// <summary>
        /// Decompresses an image into a sparse, read-only memory device.
        /// </summary>
        public static async Task<BlockDeviceMemory> OpenAsync(ReferenceImage image, bool isReadOnly = true)
        {
            await using var file = File.OpenRead(Path.Combine(Root, image.File));
            await using var gzip = new GZipStream(file, CompressionMode.Decompress);
            return await BlockDeviceMemory.LoadAsync(gzip, image.SectorSize, isReadOnly);
        }

        /// <summary>
        /// The parts below <see cref="FatVolume"/> for the first volume of an image.
        /// </summary>
        public static async Task<(FatVolumeCore Core, FatNamespace Names, long FirstSector)> OpenCoreAsync(IBlockDevice disk)
        {
            var location = (await FatDetector.DetectDiskAsync(disk)).Volumes[0];
            IBlockDevice device = location.Partition != null
                ? new BlockDevicePartition(disk, location.FirstSector, location.SectorCount)
                : disk;
            var core = new FatVolumeCore(device, location.Volume);
            var root = new FatDirectoryEntry { Attributes = FatAttributes.Directory, FirstCluster = location.Volume.RootCluster ?? 0 };
            return (core, new FatNamespace(core, root), location.FirstSector);
        }

        private static ReferenceManifest Load()
        {
            string path = Path.Combine(Root, MANIFEST_FILE);
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<ReferenceManifest>(stream, OPTIONS)
                   ?? throw new InvalidDataException($"{path} is empty.");
        }

        #endregion

        #region Properties

        public static string Root => Path.Combine(AppContext.BaseDirectory, FIXTURES_DIRECTORY);

        public static ReferenceManifest Manifest => MANIFEST.Value;

        public static IEnumerable<string> Names => Manifest.Images.Select(image => image.Name);

        public static IEnumerable<string> PartitionedNames => Manifest.Images.Where(image => image.Partitions.Count > 0).Select(image => image.Name);

        public static IEnumerable<string> FatNames => Manifest.Images.Where(image => image.Volumes[0].Kind != FatKind.ExFat).Select(image => image.Name);

        public static IEnumerable<string> ExFatNames => Manifest.Images.Where(image => image.Volumes[0].Kind == FatKind.ExFat).Select(image => image.Name);

        public static IEnumerable<string> WholeDiskNames => Manifest.Images.Where(image => image.Partitions.Count == 0).Select(image => image.Name);

        #endregion
    }
}
