using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Files;
using OutWit.Common.Fat.Formatting;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Entries of a FAT12, FAT16 or FAT32 directory: a short entry, with long-name slots in
    /// front of it when its name needs them.
    /// </summary>
    internal sealed class FatDirectoryWriterVfat : FatDirectoryWriter
    {
        #region Constructors

        public FatDirectoryWriterVfat(FatVolumeCore core, FatDirectory directory)
            : base(core, directory)
        {
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        /// <remarks>The template is a short entry; its name and case flags are replaced.</remarks>
        public override async ValueTask<DirectoryItem> AddAsync(string name, byte[] template, DirectoryItem? ignored,
            CancellationToken cancellationToken)
        {
            ShortNameBuilder.Validate(name);

            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await foreach (var item in Directory.EnumerateAsync(cancellationToken).ConfigureAwait(false))
            {
                if (ignored != null && item.Key == ignored.Key)
                    continue;
                if (string.Equals(item.Entry.Name, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.Entry.ShortName, name, StringComparison.OrdinalIgnoreCase))
                    throw new FatException(FatErrorKind.AlreadyExists, $"{item.Entry.Path} already exists.");

                taken.Add(item.Entry.Name);
                taken.Add(item.Entry.ShortName);
            }

            var shortName = new byte[DirectorySlot.NAME_LENGTH];
            bool needsLongName = !ShortNameBuilder.TryFit(name, shortName, out byte caseFlags);
            if (needsLongName)
            {
                caseFlags = 0;
                ShortNameBuilder.Generate(name, taken, shortName);
            }

            int count = needsLongName ? DirectorySlotEncoder.LongSlotCount(name) + 1 : 1;
            var slots = new byte[count * DirectorySlot.SIZE];
            byte checksum = ShortName.Checksum(shortName);
            for (int i = 0; i < count - 1; i++)
                DirectorySlotEncoder.WriteLong(slots.AsSpan(i * DirectorySlot.SIZE, DirectorySlot.SIZE), name, count - 1 - i, checksum);

            var shortSlot = slots.AsSpan((count - 1) * DirectorySlot.SIZE, DirectorySlot.SIZE);
            template.AsSpan(0, DirectorySlot.SIZE).CopyTo(shortSlot);
            DirectorySlotEncoder.Rename(shortSlot, shortName, caseFlags);

            long start = await PlaceAsync(slots, cancellationToken).ConfigureAwait(false);
            return Describe(slots, start);
        }

        /// <inheritdoc />
        public override byte[] NewTemplate(FatAttributes attributes, uint firstCluster, long dataLength, bool isContiguous)
        {
            var now = Core.Now();
            var slot = new byte[DirectorySlot.SIZE];
            DirectorySlotEncoder.WriteShort(slot, new byte[DirectorySlot.NAME_LENGTH], 0, attributes, firstCluster, (uint)dataLength, now, now);
            return slot;
        }

        /// <inheritdoc />
        public override ValueTask<byte[]> ReadTemplateAsync(DirectoryItem item, CancellationToken cancellationToken)
        {
            return ReadSlotsAsync(item.LastSlot, 1, cancellationToken);
        }

        /// <inheritdoc />
        public override async ValueTask<DirectoryItem> UpdateAsync(DirectoryItem item, FatFileState state, bool isWrite,
            CancellationToken cancellationToken)
        {
            int count = (int)(item.LastSlot - item.FirstSlot + 1);
            var slots = await ReadSlotsAsync(item.FirstSlot, count, cancellationToken).ConfigureAwait(false);
            var slot = slots[^DirectorySlot.SIZE..];
            DirectorySlotEncoder.SetFirstCluster(slot, state.FirstCluster);
            DirectorySlotEncoder.SetSize(slot, (uint)state.Length);
            if (isWrite)
            {
                DirectorySlotEncoder.SetModified(slot, Core.Now());
                slot[DirectorySlot.ATTRIBUTES] |= (byte)FatAttributes.Archive;
            }

            await WriteSlotsAsync(item.LastSlot, slot, cancellationToken).ConfigureAwait(false);
            slot.CopyTo(slots, slots.Length - DirectorySlot.SIZE);
            return Describe(slots, item.FirstSlot);
        }

        /// <inheritdoc />
        /// <remarks>Writes the <c>.</c> and <c>..</c> entries.</remarks>
        public override async ValueTask InitializeDirectoryAsync(uint cluster, DirectoryItem parent, CancellationToken cancellationToken)
        {
            var now = Core.Now();
            var slots = new byte[2 * DirectorySlot.SIZE];
            DirectorySlotEncoder.WriteShort(slots.AsSpan(0, DirectorySlot.SIZE), DotName(1), 0, FatAttributes.Directory, cluster, 0, now, now);
            DirectorySlotEncoder.WriteShort(slots.AsSpan(DirectorySlot.SIZE), DotName(2), 0, FatAttributes.Directory, ParentCluster(parent), 0, now, now);
            await Core.Device.WriteBytesAsync(Core.ClusterOffset(cluster), slots, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <remarks>Points the <c>..</c> entry at the parent.</remarks>
        /// <exception cref="FatException">The directory has no <c>..</c> entry where it belongs.</exception>
        public override async ValueTask SetParentAsync(DirectoryItem parent, CancellationToken cancellationToken)
        {
            var slot = await ReadSlotsAsync(1, 1, cancellationToken).ConfigureAwait(false);
            if (!slot.AsSpan(0, DirectorySlot.NAME_LENGTH).SequenceEqual(DotName(2)))
                throw new FatException(FatErrorKind.Corrupt, $"The directory {Directory.Entry.Path} has no '..' entry in its second slot.");

            DirectorySlotEncoder.SetFirstCluster(slot, ParentCluster(parent));
            await WriteSlotsAsync(1, slot, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async ValueTask<DirectoryItem> SetAttributesAsync(DirectoryItem item, FatAttributes attributes,
            CancellationToken cancellationToken)
        {
            int count = (int)(item.LastSlot - item.FirstSlot + 1);
            var slots = await ReadSlotsAsync(item.FirstSlot, count, cancellationToken).ConfigureAwait(false);
            var slot = slots[^DirectorySlot.SIZE..];
            DirectorySlotEncoder.SetAttributes(slot, attributes);
            await WriteSlotsAsync(item.LastSlot, slot, cancellationToken).ConfigureAwait(false);
            slot.CopyTo(slots, slots.Length - DirectorySlot.SIZE);
            return Describe(slots, item.FirstSlot);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The label is a short entry with only the volume-label attribute, its times the
        /// time it was set. Removing it marks the entry deleted, as Windows and fatlabel do.
        /// </remarks>
        public override async ValueTask SetLabelAsync(string? label, CancellationToken cancellationToken)
        {
            long slot = await FindSlotAsync(IsLabel, cancellationToken).ConfigureAwait(false);
            if (label == null)
            {
                if (slot < 0)
                    return;

                var removed = await ReadSlotsAsync(slot, 1, cancellationToken).ConfigureAwait(false);
                MarkFree(removed);
                await WriteSlotsAsync(slot, removed, cancellationToken).ConfigureAwait(false);
                return;
            }

            var now = Core.Now();
            var entry = new byte[DirectorySlot.SIZE];
            DirectorySlotEncoder.WriteShort(entry, FatFormatPlan.LabelBytes(label), 0, FatAttributes.VolumeLabel, 0, 0, now, now);
            if (slot < 0)
                await PlaceAsync(entry, cancellationToken).ConfigureAwait(false);
            else
                await WriteSlotsAsync(slot, entry, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override void MarkFree(Span<byte> slot)
        {
            slot[DirectorySlot.NAME] = DirectorySlot.DELETED;
        }

        private DirectoryItem Describe(byte[] slots, long start)
        {
            var parser = new DirectoryParser(Directory.Entry.Path, Core.HasHighCluster, Directory.Cluster);
            DirectoryItem? item = null;
            for (int offset = 0; offset < slots.Length; offset += DirectorySlot.SIZE)
                parser.Parse(slots.AsSpan(offset, DirectorySlot.SIZE), out item, out _);

            if (item == null)
                throw new InvalidOperationException("The slots written do not describe an entry.");

            int count = slots.Length / DirectorySlot.SIZE;
            return new DirectoryItem(item.Entry, Directory.Cluster, start, start + count - 1);
        }

        /// <summary>
        /// Whether a slot is the volume label: a short entry, not deleted, whose attributes
        /// say label and not directory.
        /// </summary>
        private static bool IsLabel(ReadOnlyMemory<byte> slot)
        {
            var span = slot.Span;
            byte attributes = span[DirectorySlot.ATTRIBUTES];
            return span[DirectorySlot.NAME] != DirectorySlot.DELETED
                   && (attributes & DirectorySlot.LONG_NAME_MASK) != DirectorySlot.LONG_NAME_ATTRIBUTES
                   && (attributes & (byte)(FatAttributes.VolumeLabel | FatAttributes.Directory)) == (byte)FatAttributes.VolumeLabel;
        }

        /// <summary>
        /// What <c>..</c> records: zero when the parent is the root, as Linux and Windows write it.
        /// </summary>
        private static uint ParentCluster(DirectoryItem parent)
        {
            return parent.IsRoot ? 0 : parent.Entry.FirstCluster;
        }

        private static byte[] DotName(int dots)
        {
            var name = new byte[DirectorySlot.NAME_LENGTH];
            name.AsSpan().Fill((byte)' ');
            name.AsSpan(0, dots).Fill((byte)'.');
            return name;
        }

        #endregion
    }
}
