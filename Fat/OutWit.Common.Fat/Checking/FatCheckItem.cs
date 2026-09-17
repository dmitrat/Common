using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// The checks of one entry: its clusters, its length, its name hash and, for a FAT
    /// directory, its dot entries.
    /// </summary>
    internal static class FatCheckItem
    {
        #region Constants

        private const long MAX_EXFAT_DIRECTORY_BYTES = 256L * 1024 * 1024;

        #endregion

        #region Functions

        /// <summary>
        /// Gives an entry its clusters, and checks they are as many as it needs.
        /// </summary>
        /// <param name="context">The check.</param>
        /// <param name="item">The entry.</param>
        /// <param name="parent">The owner of the directory it lies in.</param>
        /// <param name="cancellationToken">Cancels the check.</param>
        /// <returns>
        /// The entry's owner, and whether it is a directory worth entering: its clusters all
        /// found, of the right number, and no one else's.
        /// </returns>
        public static async ValueTask<(int Owner, bool IsWhole)> ClaimAsync(FatCheckContext context, DirectoryItem item, int parent,
            CancellationToken cancellationToken)
        {
            var core = context.Core;
            var entry = item.Entry;
            int owner = context.Owners.Add(entry.Name, parent);
            bool isExFat = core.ExFat != null;
            long length = isExFat ? item.DataLength : entry.Length;
            if (entry.FirstCluster == 0)
            {
                if (entry.IsDirectory && !isExFat)
                    context.Report(FatProblemKind.BrokenEntry, entry.Path, null, $"The directory {entry.Path} has no clusters.");
                else if (length > 0)
                    context.Report(FatProblemKind.LengthMismatch, entry.Path, null, $"{entry.Path} records {length} bytes and no cluster.");
                return (owner, false);
            }

            bool isEmpty = length == 0 && (isExFat || !entry.IsDirectory);
            if (isEmpty)
            {
                context.Report(FatProblemKind.LengthMismatch, entry.Path, entry.FirstCluster,
                    $"{entry.Path} records no bytes, yet starts at cluster {entry.FirstCluster}.");
                if (item.IsContiguous)
                    return (owner, false);
            }

            long needed = core.ClustersFor(length);
            var walk = item.IsContiguous
                ? FatCheckChain.Run(context, entry.FirstCluster, needed, owner)
                : await FatCheckChain.WalkAsync(context, entry.FirstCluster, owner, cancellationToken).ConfigureAwait(false);
            context.ReportShared(walk, entry.Path);
            if (walk.Problem != null)
            {
                context.Report(FatProblemKind.BrokenChain, entry.Path, entry.FirstCluster, $"The chain of {entry.Path} {walk.Problem}.");
                return (owner, false);
            }

            long clusters = item.IsContiguous ? needed : walk.Count;
            return (owner, !isEmpty && HasRightLength(context, item, clusters) && walk.IsWhole);
        }

        /// <summary>
        /// Checks the name hash an exFAT entry records.
        /// </summary>
        public static void CheckHash(FatCheckContext context, DirectoryItem item, ExFatUpcaseTable upcase)
        {
            ushort hash = upcase.HashOf(item.StoredName ?? item.Entry.Name);
            if (hash != item.NameHash)
                context.Report(FatProblemKind.WrongNameHash, item.Entry.Path, null,
                    $"{item.Entry.Path} records the name hash 0x{item.NameHash:X4}; its name hashes to 0x{hash:X4}.");
        }

        /// <summary>
        /// Checks that a FAT directory starts with '.', naming itself, and '..', naming the
        /// directory it lies in: zero for the root, or on FAT32 the root's cluster as well.
        /// </summary>
        public static async ValueTask CheckDotEntriesAsync(FatCheckContext context, DirectoryItem directory, DirectoryItem parent,
            CancellationToken cancellationToken)
        {
            var core = context.Core;
            var entry = directory.Entry;
            var slots = new byte[2 * DirectorySlot.SIZE];
            await core.Device.ReadBytesAsync(core.ClusterOffset(entry.FirstCluster), slots, cancellationToken).ConfigureAwait(false);

            uint parentCluster = parent.IsRoot ? 0 : parent.Entry.FirstCluster;
            uint dotDot = ReadDot(slots.AsSpan(DirectorySlot.SIZE), 2, core.HasHighCluster) ?? uint.MaxValue;
            bool isParent = dotDot == parentCluster || parent.IsRoot && dotDot == core.Info.RootCluster;
            if (ReadDot(slots, 1, core.HasHighCluster) != entry.FirstCluster || !isParent)
                context.Report(FatProblemKind.DotEntries, entry.Path, entry.FirstCluster,
                    $"The directory {entry.Path} does not start with '.' at cluster {entry.FirstCluster} and '..' at cluster {parentCluster}.");
        }

        /// <summary>
        /// Whether a length suits the clusters an entry holds.
        /// </summary>
        private static bool HasRightLength(FatCheckContext context, DirectoryItem item, long clusters)
        {
            var core = context.Core;
            var entry = item.Entry;
            if (core.ExFat == null && entry.IsDirectory)
                return true;

            long length = core.ExFat == null ? entry.Length : item.DataLength;
            if (entry.IsDirectory && (length % core.ClusterSize != 0 || item.ValidLength != length || length > MAX_EXFAT_DIRECTORY_BYTES))
            {
                context.Report(FatProblemKind.LengthMismatch, entry.Path, entry.FirstCluster,
                    $"The directory {entry.Path} records {length} bytes, {item.ValidLength} of them valid; a directory is whole clusters, all valid, at most {MAX_EXFAT_DIRECTORY_BYTES}.");
                return false;
            }

            long needed = core.ClustersFor(length);
            if (clusters == needed)
                return true;

            context.Report(FatProblemKind.LengthMismatch, entry.Path, entry.FirstCluster,
                $"{entry.Path} records {length} bytes, which need {needed} clusters; its chain has {clusters}.");
            return false;
        }

        /// <summary>
        /// The cluster a dot entry names, or <c>null</c> when the slot is not that dot entry.
        /// </summary>
        private static uint? ReadDot(ReadOnlySpan<byte> slot, int dots, bool hasHighCluster)
        {
            var name = slot[..DirectorySlot.NAME_LENGTH];
            if (!ShortName.IsDotEntry(name) || (name[1] == '.' ? 2 : 1) != dots)
                return null;
            if ((slot[DirectorySlot.ATTRIBUTES] & (byte)FatAttributes.Directory) == 0)
                return null;

            uint low = BinaryPrimitives.ReadUInt16LittleEndian(slot[DirectorySlot.FIRST_CLUSTER_LOW..]);
            uint high = hasHighCluster ? BinaryPrimitives.ReadUInt16LittleEndian(slot[DirectorySlot.FIRST_CLUSTER_HIGH..]) : 0u;
            return high << 16 | low;
        }

        #endregion
    }
}
