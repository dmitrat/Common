using System;
using System.Buffers.Binary;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// The BIOS parameter block of a FAT12, FAT16 or FAT32 boot sector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parsing is in two steps. <see cref="TryParse"/> decides whether the sector is a
    /// FAT boot sector at all and returns <c>null</c> when it is not.
    /// <see cref="Describe"/> then checks the numbers against each other and against the
    /// device, and throws <see cref="FatException"/> when a FAT boot sector is broken.
    /// </para>
    /// <para>
    /// The width of the table is decided between the two readers that matter: the Linux
    /// driver, which is the test oracle, and FatFs, which the recorders run.
    /// </para>
    /// <list type="bullet">
    /// <item>A 16-bit table size of zero means the FAT32 layout whatever the cluster count,
    /// as in Linux. FatFs classifies by count alone and refuses a FAT32 layout with fewer
    /// than 65526 clusters; such volumes exist, and reading them costs nothing.</item>
    /// <item>Otherwise up to 4084 clusters is FAT12 and from 4086 FAT16, where both agree.
    /// At exactly 4085 Linux reads FAT16 and FatFs FAT12, so the table decides: FAT16 if
    /// it can hold 16-bit entries for all of them, FAT12 if not.</item>
    /// <item>FAT16 goes up to 65525 clusters, the most its entries can number, as in
    /// FatFs; Linux stops at 65524.</item>
    /// <item>As in Linux, a data area larger than the table can address is not an error:
    /// the clusters past the table cannot be used and are left out of the count.</item>
    /// </list>
    /// <para>
    /// An FSInfo sector of zero is read as sector 1, as Linux does; its signatures are
    /// checked when it is read, not here.
    /// </para>
    /// </remarks>
    internal sealed class FatBootSector
    {
        #region Constants

        public const uint MAX_UNDISPUTED_FAT12_CLUSTERS = 4084;

        public const uint DISPUTED_CLUSTERS = 4085;

        public const uint MAX_FAT16_CLUSTERS = 65525;

        public const uint MAX_FAT32_CLUSTERS = 0x0FFFFFF5;

        public const int DIRECTORY_ENTRY_SIZE = 32;

        private const int OFFSET_BYTES_PER_SECTOR = 11;

        private const int OFFSET_SECTORS_PER_CLUSTER = 13;

        private const int OFFSET_RESERVED_SECTORS = 14;

        private const int OFFSET_FAT_COUNT = 16;

        private const int OFFSET_ROOT_ENTRIES = 17;

        private const int OFFSET_TOTAL_SECTORS_16 = 19;

        private const int OFFSET_MEDIA = 21;

        private const int OFFSET_FAT_SECTORS_16 = 22;

        private const int OFFSET_HIDDEN_SECTORS = 28;

        private const int OFFSET_TOTAL_SECTORS_32 = 32;

        private const int OFFSET_EXTENDED_SIGNATURE_16 = 38;

        private const int OFFSET_FAT_SECTORS_32 = 36;

        private const int OFFSET_EXTENDED_FLAGS = 40;

        private const int OFFSET_VERSION = 42;

        private const int OFFSET_ROOT_CLUSTER = 44;

        private const int OFFSET_FS_INFO = 48;

        private const int OFFSET_BACKUP_BOOT = 50;

        private const int OFFSET_EXTENDED_SIGNATURE_32 = 66;

        private const byte EXTENDED_SIGNATURE = 0x29;

        private const byte EXTENDED_SIGNATURE_SERIAL_ONLY = 0x28;

        private const ushort MIRRORING_DISABLED = 0x0080;

        private const ushort ACTIVE_FAT_MASK = 0x000F;

        private const ushort NO_SECTOR = 0xFFFF;

        private const int IMPLIED_FS_INFO_SECTOR = 1;

        private const int RESERVED_ENTRIES = 2;

        private const byte MEDIA_REMOVABLE = 0xF0;

        private const byte MEDIA_LOWEST = 0xF8;

        #endregion

        #region Constructors

        private FatBootSector()
        {
        }

        #endregion

        #region Functions

        /// <summary>
        /// Reads the parameter block, or returns <c>null</c> when the sector is not a FAT
        /// boot sector: no boot signature, no jump instruction, or parameters no FAT
        /// volume can have.
        /// </summary>
        public static FatBootSector? TryParse(ReadOnlySpan<byte> sector)
        {
            if (!BootSignature.IsPresent(sector) || sector[0] is not (0xEB or 0xE9 or 0xE8))
                return null;

            var boot = new FatBootSector
            {
                BytesPerSector = ReadUInt16(sector, OFFSET_BYTES_PER_SECTOR),
                SectorsPerCluster = sector[OFFSET_SECTORS_PER_CLUSTER],
                ReservedSectors = ReadUInt16(sector, OFFSET_RESERVED_SECTORS),
                FatCount = sector[OFFSET_FAT_COUNT],
                RootEntries = ReadUInt16(sector, OFFSET_ROOT_ENTRIES),
                TotalSectors = ReadUInt16(sector, OFFSET_TOTAL_SECTORS_16),
                Media = sector[OFFSET_MEDIA],
                FatSectors = ReadUInt16(sector, OFFSET_FAT_SECTORS_16),
                HiddenSectors = ReadUInt32(sector, OFFSET_HIDDEN_SECTORS)
            };

            if (boot.TotalSectors == 0)
                boot.TotalSectors = ReadUInt32(sector, OFFSET_TOTAL_SECTORS_32);

            if (!boot.IsPlausible())
                return null;

            boot.IsFat32Layout = boot.FatSectors == 0;
            int signatureOffset = OFFSET_EXTENDED_SIGNATURE_16;

            if (boot.IsFat32Layout)
            {
                boot.FatSectors = ReadUInt32(sector, OFFSET_FAT_SECTORS_32);
                boot.ExtendedFlags = ReadUInt16(sector, OFFSET_EXTENDED_FLAGS);
                boot.Version = ReadUInt16(sector, OFFSET_VERSION);
                boot.RootCluster = ReadUInt32(sector, OFFSET_ROOT_CLUSTER);
                boot.FsInfoSector = ReadUInt16(sector, OFFSET_FS_INFO);
                boot.BackupBootSector = ReadUInt16(sector, OFFSET_BACKUP_BOOT);
                signatureOffset = OFFSET_EXTENDED_SIGNATURE_32;
            }

            if (sector[signatureOffset] is EXTENDED_SIGNATURE or EXTENDED_SIGNATURE_SERIAL_ONLY)
                boot.VolumeSerial = ReadUInt32(sector, signatureOffset + 1);

            return boot;
        }

        /// <summary>
        /// Checks the parameters and works out the layout they describe.
        /// </summary>
        /// <param name="deviceSectorSize">The sector size of the device holding the volume.</param>
        /// <param name="deviceSectorCount">The sectors available to the volume.</param>
        /// <returns>The layout.</returns>
        /// <exception cref="FatException">The parameters are inconsistent or unsupported.</exception>
        public FatVolumeInfo Describe(int deviceSectorSize, long deviceSectorCount)
        {
            if (BytesPerSector != deviceSectorSize)
                throw new FatException(FatErrorKind.SectorSizeMismatch,
                    $"The volume has {BytesPerSector}-byte sectors but its device has {deviceSectorSize}-byte sectors.");

            if (FatSectors == 0)
                throw Corrupt("The boot sector gives no size for the allocation table.");
            if (IsFat32Layout && RootEntries != 0)
                throw Corrupt("A FAT32 boot sector declares a fixed root directory.");
            if (IsFat32Layout && Version != 0)
                throw new FatException(FatErrorKind.Unsupported, $"FAT32 version {Version >> 8}.{Version & 0xFF} is not supported.");
            if (!IsFat32Layout && RootEntries == 0)
                throw Corrupt("A FAT12 or FAT16 boot sector declares no root directory.");

            long rootSectors = ((long)RootEntries * DIRECTORY_ENTRY_SIZE + BytesPerSector - 1) / BytesPerSector;
            long rootStart = ReservedSectors + (long)FatCount * FatSectors;
            long dataStart = rootStart + rootSectors;
            if (dataStart >= TotalSectors)
                throw Corrupt($"The data area would start at sector {dataStart}, past the end of a volume of {TotalSectors} sectors.");

            long clusters = (TotalSectors - dataStart) / SectorsPerCluster;
            if (clusters == 0)
                throw Corrupt("The data area is smaller than one cluster.");

            var kind = ClassifyKind(clusters);
            clusters = Math.Min(clusters, AddressableClusters(kind));
            CheckClusterLimit(kind, clusters);

            if (TotalSectors > deviceSectorCount)
                throw Corrupt($"The volume declares {TotalSectors} sectors but only {deviceSectorCount} are available to it.");

            if (!IsFat32Layout)
            {
                return Build(kind, clusters, dataStart, activeFat: 0, isMirrored: true,
                    rootSector: rootStart, rootEntries: RootEntries, rootCluster: null, fsInfo: null, backup: null);
            }

            if (RootCluster < 2 || RootCluster > clusters + 1)
                throw Corrupt($"The root directory starts at cluster {RootCluster}, outside clusters 2 to {clusters + 1}.");

            bool isMirrored = (ExtendedFlags & MIRRORING_DISABLED) == 0;
            int activeFat = isMirrored ? 0 : ExtendedFlags & ACTIVE_FAT_MASK;
            if (activeFat >= FatCount)
                throw Corrupt($"Allocation table {activeFat} is marked active but the volume has {FatCount}.");

            int? fsInfo = FsInfoSector == 0
                ? (IMPLIED_FS_INFO_SECTOR < ReservedSectors ? IMPLIED_FS_INFO_SECTOR : null)
                : ReservedSector(FsInfoSector, "FSInfo");

            return Build(kind, clusters, dataStart, activeFat, isMirrored, rootSector: null, rootEntries: null,
                RootCluster, fsInfo, ReservedSector(BackupBootSector, "backup boot"));
        }

        private bool IsPlausible()
        {
            return BlockDeviceBase.IsValidSectorSize(BytesPerSector)
                   && SectorsPerCluster != 0 && (SectorsPerCluster & (SectorsPerCluster - 1)) == 0
                   && ReservedSectors != 0
                   && FatCount != 0
                   && (Media == MEDIA_REMOVABLE || Media >= MEDIA_LOWEST)
                   && TotalSectors != 0;
        }

        private FatKind ClassifyKind(long clusters)
        {
            if (IsFat32Layout)
                return FatKind.Fat32;

            if (clusters <= MAX_UNDISPUTED_FAT12_CLUSTERS)
                return FatKind.Fat12;

            if (clusters == DISPUTED_CLUSTERS)
                return FatSectors * BytesPerSector * 8 / 16 - RESERVED_ENTRIES >= clusters ? FatKind.Fat16 : FatKind.Fat12;

            return FatKind.Fat16;
        }

        private long AddressableClusters(FatKind kind)
        {
            int entryBits = kind switch { FatKind.Fat12 => 12, FatKind.Fat16 => 16, _ => 32 };
            return FatSectors * BytesPerSector * 8 / entryBits - RESERVED_ENTRIES;
        }

        private static void CheckClusterLimit(FatKind kind, long clusters)
        {
            long limit = kind switch
            {
                FatKind.Fat12 => DISPUTED_CLUSTERS,
                FatKind.Fat16 => MAX_FAT16_CLUSTERS,
                _ => MAX_FAT32_CLUSTERS
            };

            if (clusters > limit)
                throw Corrupt($"{clusters} clusters are more than {kind} can address; the most is {limit}.");
        }

        private int? ReservedSector(int sector, string name)
        {
            if (sector is 0 or NO_SECTOR)
                return null;

            return sector < ReservedSectors
                ? sector
                : throw Corrupt($"The {name} sector {sector} lies outside the {ReservedSectors} reserved sectors.");
        }

        private FatVolumeInfo Build(FatKind kind, long clusters, long dataStart, int activeFat, bool isMirrored,
            long? rootSector, int? rootEntries, uint? rootCluster, int? fsInfo, int? backup)
        {
            return new FatVolumeInfo
            {
                Kind = kind,
                SectorSize = BytesPerSector,
                SectorsPerCluster = SectorsPerCluster,
                TotalSectors = TotalSectors,
                FatOffset = ReservedSectors,
                FatCount = FatCount,
                FatSectors = FatSectors,
                ActiveFat = activeFat,
                IsFatMirrored = isMirrored,
                RootDirectorySector = rootSector,
                RootDirectoryEntries = rootEntries,
                RootCluster = rootCluster,
                ClusterHeapSector = dataStart,
                ClusterCount = (uint)clusters,
                VolumeSerial = VolumeSerial,
                FsInfoSector = fsInfo,
                BackupBootSector = backup
            };
        }

        private static FatException Corrupt(string message)
        {
            return new FatException(FatErrorKind.Corrupt, message);
        }

        private static ushort ReadUInt16(ReadOnlySpan<byte> sector, int offset)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(sector[offset..]);
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> sector, int offset)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(sector[offset..]);
        }

        #endregion

        #region Properties

        public int BytesPerSector { get; private init; }

        public int SectorsPerCluster { get; private init; }

        public int ReservedSectors { get; private init; }

        public int FatCount { get; private init; }

        public int RootEntries { get; private init; }

        public long TotalSectors { get; private set; }

        public byte Media { get; private init; }

        public long FatSectors { get; private set; }

        public bool IsFat32Layout { get; private set; }

        public ushort ExtendedFlags { get; private set; }

        public ushort Version { get; private set; }

        public uint RootCluster { get; private set; }

        public int FsInfoSector { get; private set; }

        public int BackupBootSector { get; private set; }

        public uint VolumeSerial { get; private set; }

        public uint HiddenSectors { get; private init; }

        #endregion
    }
}
