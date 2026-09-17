using System;
using System.Buffers.Binary;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// The main boot sector of an exFAT volume.
    /// </summary>
    /// <remarks>
    /// As with <see cref="FatBootSector"/>, <see cref="TryParse"/> only decides whether
    /// the sector is exFAT, and <see cref="Describe"/> validates. Validation includes the
    /// boot region checksum, which the specification requires before anything else on
    /// the volume is trusted. A bad main region is reported, not silently replaced by
    /// the backup at sector 12.
    /// </remarks>
    internal sealed class ExFatBootSector
    {
        #region Constants

        public const int BOOT_REGION_SECTORS = 12;

        public const int BACKUP_BOOT_SECTOR = 12;

        public const uint MAX_CLUSTERS = 0xFFFFFFF5;

        private const int OFFSET_NAME = 3;

        private const int OFFSET_MUST_BE_ZERO = 11;

        private const int MUST_BE_ZERO_LENGTH = 53;

        private const int OFFSET_VOLUME_LENGTH = 72;

        private const int OFFSET_FAT_OFFSET = 80;

        private const int OFFSET_FAT_LENGTH = 84;

        private const int OFFSET_CLUSTER_HEAP = 88;

        private const int OFFSET_CLUSTER_COUNT = 92;

        private const int OFFSET_ROOT_CLUSTER = 96;

        private const int OFFSET_SERIAL = 100;

        private const int OFFSET_REVISION = 104;

        private const int OFFSET_VOLUME_FLAGS = 106;

        private const int OFFSET_SECTOR_SHIFT = 108;

        private const int OFFSET_CLUSTER_SHIFT = 109;

        private const int OFFSET_FAT_COUNT = 110;

        private const int MIN_SECTOR_SHIFT = 9;

        private const int MAX_SECTOR_SHIFT = 12;

        private const int MAX_CLUSTER_SHIFT = 25;

        private const int MIN_FAT_OFFSET = 24;

        private const int MIN_VOLUME_BYTES = 1 << 20;

        private const int SUPPORTED_MAJOR_REVISION = 1;

        private const ushort ACTIVE_FAT_FLAG = 0x0001;

        private static ReadOnlySpan<byte> JUMP_BOOT => new byte[] { 0xEB, 0x76, 0x90 };

        private static ReadOnlySpan<byte> FILE_SYSTEM_NAME => "EXFAT   "u8;

        #endregion

        #region Constructors

        private ExFatBootSector()
        {
        }

        #endregion

        #region Functions

        /// <summary>
        /// Reads the boot sector, or returns <c>null</c> when the sector is not exFAT's:
        /// no boot signature, a different jump or name, or a non-zero legacy BPB area.
        /// </summary>
        public static ExFatBootSector? TryParse(ReadOnlySpan<byte> sector)
        {
            if (!BootSignature.IsPresent(sector)
                || !sector[..JUMP_BOOT.Length].SequenceEqual(JUMP_BOOT)
                || !sector.Slice(OFFSET_NAME, FILE_SYSTEM_NAME.Length).SequenceEqual(FILE_SYSTEM_NAME)
                || sector.Slice(OFFSET_MUST_BE_ZERO, MUST_BE_ZERO_LENGTH).IndexOfAnyExcept((byte)0) >= 0)
                return null;

            return new ExFatBootSector
            {
                VolumeLength = BinaryPrimitives.ReadUInt64LittleEndian(sector[OFFSET_VOLUME_LENGTH..]),
                FatOffset = BinaryPrimitives.ReadUInt32LittleEndian(sector[OFFSET_FAT_OFFSET..]),
                FatLength = BinaryPrimitives.ReadUInt32LittleEndian(sector[OFFSET_FAT_LENGTH..]),
                ClusterHeapOffset = BinaryPrimitives.ReadUInt32LittleEndian(sector[OFFSET_CLUSTER_HEAP..]),
                ClusterCount = BinaryPrimitives.ReadUInt32LittleEndian(sector[OFFSET_CLUSTER_COUNT..]),
                RootCluster = BinaryPrimitives.ReadUInt32LittleEndian(sector[OFFSET_ROOT_CLUSTER..]),
                VolumeSerial = BinaryPrimitives.ReadUInt32LittleEndian(sector[OFFSET_SERIAL..]),
                Revision = BinaryPrimitives.ReadUInt16LittleEndian(sector[OFFSET_REVISION..]),
                VolumeFlags = BinaryPrimitives.ReadUInt16LittleEndian(sector[OFFSET_VOLUME_FLAGS..]),
                SectorShift = sector[OFFSET_SECTOR_SHIFT],
                ClusterShift = sector[OFFSET_CLUSTER_SHIFT],
                FatCount = sector[OFFSET_FAT_COUNT]
            };
        }

        /// <summary>
        /// Checks that the volume's sector size is valid and matches the device's, before
        /// the rest of the boot region is read in the device's sectors.
        /// </summary>
        /// <exception cref="FatException">The size is invalid or differs from the device's.</exception>
        public void CheckSectorSize(int deviceSectorSize)
        {
            if (SectorShift is < MIN_SECTOR_SHIFT or > MAX_SECTOR_SHIFT)
                throw Corrupt($"The sector size shift {SectorShift} is outside {MIN_SECTOR_SHIFT} to {MAX_SECTOR_SHIFT}.");

            if (BytesPerSector != deviceSectorSize)
                throw new FatException(FatErrorKind.SectorSizeMismatch,
                    $"The volume has {BytesPerSector}-byte sectors but its device has {deviceSectorSize}-byte sectors.");
        }

        /// <summary>
        /// Checks the parameters and the boot region checksum and describes the layout.
        /// </summary>
        /// <param name="region">Sectors 0 to 11 of the volume.</param>
        /// <param name="deviceSectorCount">The sectors available to the volume.</param>
        /// <returns>The layout.</returns>
        /// <exception cref="FatException">The parameters or the checksum are wrong, or the revision is unsupported.</exception>
        public FatVolumeInfo Describe(ReadOnlySpan<byte> region, long deviceSectorCount)
        {
            CheckSectorSize(region.Length / BOOT_REGION_SECTORS);

            if (Revision >> 8 != SUPPORTED_MAJOR_REVISION)
                throw new FatException(FatErrorKind.Unsupported, $"exFAT revision {Revision >> 8}.{Revision & 0xFF:D2} is not supported.");
            if (ClusterShift > MAX_CLUSTER_SHIFT - SectorShift)
                throw Corrupt($"Clusters of 2^{SectorShift + ClusterShift} bytes exceed the 32 MiB limit.");
            if (FatCount is not (1 or 2))
                throw Corrupt($"The volume declares {FatCount} allocation tables; exFAT has one or two.");
            if (VolumeLength < (ulong)(MIN_VOLUME_BYTES / BytesPerSector))
                throw Corrupt($"A volume of {VolumeLength} sectors is below the 1 MiB minimum.");
            if (VolumeLength > (ulong)deviceSectorCount)
                throw Corrupt($"The volume declares {VolumeLength} sectors but only {deviceSectorCount} are available to it.");
            if (FatOffset < MIN_FAT_OFFSET)
                throw Corrupt($"The allocation table starts at sector {FatOffset}, inside the boot regions.");
            if ((ulong)FatOffset + (ulong)FatLength * (ulong)FatCount > ClusterHeapOffset)
                throw Corrupt("The allocation tables overlap the cluster heap.");
            if (ClusterHeapOffset >= VolumeLength)
                throw Corrupt($"The cluster heap starts at sector {ClusterHeapOffset}, past the end of the volume.");

            ulong heapClusters = (VolumeLength - ClusterHeapOffset) >> ClusterShift;
            if (ClusterCount == 0 || ClusterCount > heapClusters || ClusterCount > MAX_CLUSTERS)
                throw Corrupt($"The volume declares {ClusterCount} clusters; its heap has room for {heapClusters}.");
            if ((ulong)FatLength * (ulong)BytesPerSector < ((ulong)ClusterCount + 2) * sizeof(uint))
                throw Corrupt($"An allocation table of {FatLength} sectors cannot hold {ClusterCount} clusters.");
            if (RootCluster < 2 || RootCluster > (ulong)ClusterCount + 1)
                throw Corrupt($"The root directory starts at cluster {RootCluster}, outside clusters 2 to {(ulong)ClusterCount + 1}.");

            int activeFat = (VolumeFlags & ACTIVE_FAT_FLAG) != 0 ? 1 : 0;
            if (activeFat >= FatCount)
                throw Corrupt($"Allocation table {activeFat} is marked active but the volume has {FatCount}.");
            if (!ExFatBootChecksum.Matches(region, BytesPerSector))
                throw Corrupt("The main boot region checksum does not match.");

            return new FatVolumeInfo
            {
                Kind = FatKind.ExFat,
                SectorSize = BytesPerSector,
                SectorsPerCluster = 1 << ClusterShift,
                TotalSectors = (long)VolumeLength,
                FatOffset = FatOffset,
                FatCount = FatCount,
                FatSectors = FatLength,
                ActiveFat = activeFat,
                IsFatMirrored = false,
                RootCluster = RootCluster,
                ClusterHeapSector = ClusterHeapOffset,
                ClusterCount = ClusterCount,
                VolumeSerial = VolumeSerial,
                BackupBootSector = BACKUP_BOOT_SECTOR
            };
        }

        private static FatException Corrupt(string message)
        {
            return new FatException(FatErrorKind.Corrupt, message);
        }

        #endregion

        #region Properties

        public ulong VolumeLength { get; private init; }

        public uint FatOffset { get; private init; }

        public uint FatLength { get; private init; }

        public uint ClusterHeapOffset { get; private init; }

        public uint ClusterCount { get; private init; }

        public uint RootCluster { get; private init; }

        public uint VolumeSerial { get; private init; }

        public ushort Revision { get; private init; }

        public ushort VolumeFlags { get; private init; }

        public int SectorShift { get; private init; }

        public int ClusterShift { get; private init; }

        public int FatCount { get; private init; }

        public int BytesPerSector => 1 << SectorShift;

        #endregion
    }
}
