using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Files;
using OutWit.Common.Fat.Formatting;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Adds, removes and changes the entries of one directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// New entries take the first run of free slots that fits them — slots the format marks
    /// free, or the slots from the end marker on — and the directory grows by zeroed clusters
    /// when none fits. Removing an entry marks its slots free; nothing is moved, so the place
    /// of every other entry stays valid while the volume is mounted.
    /// </para>
    /// <para>
    /// An entry is described to the writer by a template in the format's own bytes: a short
    /// entry on FAT12/16/32, a file and a stream entry on exFAT. A name is put on it when it
    /// is added, so a moved entry keeps its data, attributes and times.
    /// </para>
    /// </remarks>
    internal abstract class FatDirectoryWriter
    {
        #region Constructors

        protected FatDirectoryWriter(FatVolumeCore core, FatDirectory directory)
        {
            Core = core;
            Directory = directory;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Opens a writer in the format of the directory.
        /// </summary>
        public static FatDirectoryWriter Open(FatVolumeCore core, FatDirectory directory)
        {
            return directory is FatDirectoryExFat exFat
                ? new FatDirectoryWriterExFat(core, exFat)
                : new FatDirectoryWriterVfat(core, directory);
        }

        /// <summary>
        /// Adds an entry.
        /// </summary>
        /// <param name="name">The name; validated here.</param>
        /// <param name="template">What the entry records besides its name, from <see cref="NewTemplate"/> or <see cref="ReadTemplateAsync"/>.</param>
        /// <param name="ignored">An entry that does not count as taking the name, such as the one being renamed.</param>
        /// <param name="cancellationToken">Cancels the addition.</param>
        /// <returns>The new entry and its place.</returns>
        /// <exception cref="ArgumentException">The name is not valid.</exception>
        /// <exception cref="FatException">The name is taken, or there is no room.</exception>
        public abstract ValueTask<DirectoryItem> AddAsync(string name, byte[] template, DirectoryItem? ignored, CancellationToken cancellationToken);

        /// <summary>
        /// A template for a new entry, its times now.
        /// </summary>
        /// <param name="attributes">Its attributes.</param>
        /// <param name="firstCluster">Its first cluster, or zero.</param>
        /// <param name="dataLength">The length its entry records; zero for a directory on FAT12/16/32.</param>
        /// <param name="isContiguous">Whether its clusters follow each other without a table chain; exFAT only.</param>
        public abstract byte[] NewTemplate(FatAttributes attributes, uint firstCluster, long dataLength, bool isContiguous);

        /// <summary>
        /// The template of an existing entry, to add it somewhere else.
        /// </summary>
        public abstract ValueTask<byte[]> ReadTemplateAsync(DirectoryItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Records where an entry's data lies and how long it is.
        /// </summary>
        /// <param name="item">The entry.</param>
        /// <param name="state">Its data.</param>
        /// <param name="isWrite">Whether the change is a write, which sets the modification time and the archive bit.</param>
        /// <param name="cancellationToken">Cancels the change.</param>
        /// <returns>The entry as it now reads.</returns>
        public abstract ValueTask<DirectoryItem> UpdateAsync(DirectoryItem item, FatFileState state, bool isWrite, CancellationToken cancellationToken);

        /// <summary>
        /// Prepares the first cluster of a new directory, zeroed already, before its entry is added.
        /// </summary>
        public abstract ValueTask InitializeDirectoryAsync(uint cluster, DirectoryItem parent, CancellationToken cancellationToken);

        /// <summary>
        /// Tells this directory, just moved, where its parent now is.
        /// </summary>
        public abstract ValueTask SetParentAsync(DirectoryItem parent, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the attributes a caller may set on an entry, keeping those that tell what it is.
        /// </summary>
        /// <param name="item">The entry.</param>
        /// <param name="attributes">Read-only, hidden, system and archive; other bits are ignored.</param>
        /// <param name="cancellationToken">Cancels the change.</param>
        /// <returns>The entry as it now reads.</returns>
        public abstract ValueTask<DirectoryItem> SetAttributesAsync(DirectoryItem item, FatAttributes attributes, CancellationToken cancellationToken);

        /// <summary>
        /// Sets or removes the volume label this directory, the root, records.
        /// </summary>
        /// <param name="label">The label as the format stores it, from <see cref="Formatting.FatFormatPlan.NormalizeLabel"/>; <c>null</c> for none.</param>
        /// <param name="cancellationToken">Cancels the change.</param>
        /// <exception cref="FatException">The label needs a new entry and the root has no room.</exception>
        public abstract ValueTask SetLabelAsync(string? label, CancellationToken cancellationToken);

        /// <summary>
        /// Marks an entry's slots free.
        /// </summary>
        /// <returns>The slots as they were, for <see cref="RestoreAsync"/>.</returns>
        public async ValueTask<byte[]> RemoveAsync(DirectoryItem item, CancellationToken cancellationToken)
        {
            int count = (int)(item.LastSlot - item.FirstSlot + 1);
            var slots = await ReadSlotsAsync(item.FirstSlot, count, cancellationToken).ConfigureAwait(false);
            var removed = (byte[])slots.Clone();
            for (int i = 0; i < count; i++)
                MarkFree(removed.AsSpan(i * DirectorySlot.SIZE, DirectorySlot.SIZE));
            await WriteSlotsAsync(item.FirstSlot, removed, cancellationToken).ConfigureAwait(false);
            return slots;
        }

        /// <summary>
        /// Puts back the slots of an entry removed a moment ago.
        /// </summary>
        public ValueTask RestoreAsync(DirectoryItem item, byte[] slots, CancellationToken cancellationToken)
        {
            return WriteSlotsAsync(item.FirstSlot, slots, cancellationToken);
        }

        /// <summary>
        /// Marks one slot of a removed entry free.
        /// </summary>
        protected abstract void MarkFree(Span<byte> slot);

        /// <summary>
        /// Writes new slots where they fit, growing the directory when nothing does.
        /// </summary>
        /// <returns>The first slot written.</returns>
        protected async ValueTask<long> PlaceAsync(byte[] slots, CancellationToken cancellationToken)
        {
            int count = slots.Length / DirectorySlot.SIZE;
            var free = await Directory.FindFreeAsync(count, cancellationToken).ConfigureAwait(false);
            if (free.Available < count)
                await Directory.GrowAsync(count - free.Available, cancellationToken).ConfigureAwait(false);

            await WriteSlotsAsync(free.Start, slots, cancellationToken).ConfigureAwait(false);
            if (free.EndMarker >= 0)
                await KeepEndMarkerAsync(free.Start + count, cancellationToken).ConfigureAwait(false);
            return free.Start;
        }

        /// <summary>
        /// The first slot before the end marker that <paramref name="isWanted"/> picks, or -1.
        /// </summary>
        protected async ValueTask<long> FindSlotAsync(Func<ReadOnlyMemory<byte>, bool> isWanted, CancellationToken cancellationToken)
        {
            long index = 0;
            await foreach (var chunk in Directory.ReadSlotsAsync(cancellationToken).ConfigureAwait(false))
            {
                for (int offset = 0; offset + DirectorySlot.SIZE <= chunk.Length; offset += DirectorySlot.SIZE, index++)
                {
                    var slot = chunk.Slice(offset, DirectorySlot.SIZE);
                    if (slot.Span[0] == DirectorySlot.END_OF_DIRECTORY)
                        return -1;
                    if (isWanted(slot))
                        return index;
                }
            }

            return -1;
        }

        protected async ValueTask<byte[]> ReadSlotsAsync(long start, int count, CancellationToken cancellationToken)
        {
            var slots = new byte[count * DirectorySlot.SIZE];
            await ForEachStretchAsync(start, count, (offset, index, length) =>
                Core.Device.ReadBytesAsync(offset, slots.AsMemory(index * DirectorySlot.SIZE, length * DirectorySlot.SIZE), cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return slots;
        }

        protected ValueTask WriteSlotsAsync(long start, byte[] slots, CancellationToken cancellationToken)
        {
            return ForEachStretchAsync(start, slots.Length / DirectorySlot.SIZE, (offset, index, length) =>
                Core.Device.WriteBytesAsync(offset, slots.AsMemory(index * DirectorySlot.SIZE, length * DirectorySlot.SIZE), cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Makes sure the slot after new entries placed past the end marker is an end
        /// marker too, in case the free tail held something other than zeros.
        /// </summary>
        private async ValueTask KeepEndMarkerAsync(long slot, CancellationToken cancellationToken)
        {
            long capacity = await Directory.GetCapacityAsync(cancellationToken).ConfigureAwait(false);
            if (slot >= capacity)
                return;

            var next = await ReadSlotsAsync(slot, 1, cancellationToken).ConfigureAwait(false);
            if (next[0] == DirectorySlot.END_OF_DIRECTORY)
                return;

            next[0] = DirectorySlot.END_OF_DIRECTORY;
            await WriteSlotsAsync(slot, next, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Calls <paramref name="action"/> once for each stretch of slots that lie next to
        /// each other on the device.
        /// </summary>
        private async ValueTask ForEachStretchAsync(long start, int count, Func<long, int, int, ValueTask> action, CancellationToken cancellationToken)
        {
            int index = 0;
            while (index < count)
            {
                long offset = await Directory.GetSlotOffsetAsync(start + index, cancellationToken).ConfigureAwait(false);
                int length = 1;
                while (index + length < count
                       && await Directory.GetSlotOffsetAsync(start + index + length, cancellationToken).ConfigureAwait(false)
                       == offset + length * DirectorySlot.SIZE)
                    length++;

                await action(offset, index, length).ConfigureAwait(false);
                index += length;
            }
        }

        #endregion

        #region Properties

        protected FatVolumeCore Core { get; }

        protected FatDirectory Directory { get; }

        #endregion
    }
}
