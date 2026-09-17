using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Files;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Entry sets of an exFAT directory.
    /// </summary>
    /// <remarks>
    /// A set is always written whole, with its checksum, and its name hash is made with the
    /// volume's up-case table. A removed set keeps its bytes with the in-use bit of each entry
    /// cleared, as the specification and Linux do.
    /// </remarks>
    internal sealed class FatDirectoryWriterExFat : FatDirectoryWriter
    {
        #region Fields

        private readonly FatDirectoryExFat m_directory;

        #endregion

        #region Constructors

        public FatDirectoryWriterExFat(FatVolumeCore core, FatDirectoryExFat directory)
            : base(core, directory)
        {
            m_directory = directory;
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        /// <remarks>The template is a set, whole or its file and stream entries alone; its name is replaced.</remarks>
        public override async ValueTask<DirectoryItem> AddAsync(string name, byte[] template, DirectoryItem? ignored,
            CancellationToken cancellationToken)
        {
            ShortNameBuilder.Validate(name);

            var upcase = await Core.ExFat!.GetUpcaseTableAsync(cancellationToken).ConfigureAwait(false);
            await foreach (var item in Directory.EnumerateAsync(cancellationToken).ConfigureAwait(false))
            {
                if (ignored != null && item.Key == ignored.Key)
                    continue;
                if (upcase.NamesEqual(item.Entry.Name, name))
                    throw new FatException(FatErrorKind.AlreadyExists, $"{item.Entry.Path} already exists.");
            }

            var set = ExFatEntryEncoder.Rename(template, name, upcase.HashOf(name));
            long start = await PlaceAsync(set, cancellationToken).ConfigureAwait(false);
            return m_directory.Describe(set, start);
        }

        /// <inheritdoc />
        public override byte[] NewTemplate(FatAttributes attributes, uint firstCluster, long dataLength, bool isContiguous)
        {
            return ExFatEntryEncoder.Create(attributes, Core.Clock.GetLocalNow(), firstCluster, dataLength, isContiguous);
        }

        /// <inheritdoc />
        public override ValueTask<byte[]> ReadTemplateAsync(DirectoryItem item, CancellationToken cancellationToken)
        {
            return ReadSlotsAsync(item.FirstSlot, (int)(item.LastSlot - item.FirstSlot + 1), cancellationToken);
        }

        /// <inheritdoc />
        public override async ValueTask<DirectoryItem> UpdateAsync(DirectoryItem item, FatFileState state, bool isWrite,
            CancellationToken cancellationToken)
        {
            var set = await ReadTemplateAsync(item, cancellationToken).ConfigureAwait(false);
            ExFatEntryEncoder.SetStream(set, state.FirstCluster, state.Length, state.ValidLength, state.IsContiguous);
            if (isWrite)
            {
                ExFatEntryEncoder.SetModified(set, Core.Clock.GetLocalNow());
                ExFatEntryEncoder.AddAttributes(set, FatAttributes.Archive);
            }

            ExFatEntryEncoder.Seal(set);
            await WriteSlotsAsync(item.FirstSlot, set, cancellationToken).ConfigureAwait(false);
            return m_directory.Describe(set, item.FirstSlot);
        }

        /// <inheritdoc />
        /// <remarks>exFAT directories have no <c>.</c> and <c>..</c> entries; a zeroed cluster is an empty directory.</remarks>
        public override ValueTask InitializeDirectoryAsync(uint cluster, DirectoryItem parent, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        /// <remarks>Nothing in an exFAT directory names its parent.</remarks>
        public override ValueTask SetParentAsync(DirectoryItem parent, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public override async ValueTask<DirectoryItem> SetAttributesAsync(DirectoryItem item, FatAttributes attributes,
            CancellationToken cancellationToken)
        {
            var set = await ReadTemplateAsync(item, cancellationToken).ConfigureAwait(false);
            ExFatEntryEncoder.SetAttributes(set, attributes);
            ExFatEntryEncoder.Seal(set);
            await WriteSlotsAsync(item.FirstSlot, set, cancellationToken).ConfigureAwait(false);
            return m_directory.Describe(set, item.FirstSlot);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The label entry stays when the label is removed, with no characters, as
        /// mkfs.exfat writes it: exfatlabel cannot set a label on a root without one.
        /// </remarks>
        public override async ValueTask SetLabelAsync(string? label, CancellationToken cancellationToken)
        {
            long slot = await FindSlotAsync(found => found.Span[ExFatEntry.TYPE] == ExFatEntry.VOLUME_LABEL, cancellationToken).ConfigureAwait(false);
            if (slot < 0 && label == null)
                return;

            var entry = new byte[ExFatEntry.SIZE];
            ExFatEntryEncoder.WriteLabel(entry, label);
            if (slot < 0)
                await PlaceAsync(entry, cancellationToken).ConfigureAwait(false);
            else
                await WriteSlotsAsync(slot, entry, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override void MarkFree(Span<byte> slot)
        {
            slot[ExFatEntry.TYPE] &= unchecked((byte)~ExFatEntry.IN_USE);
        }

        #endregion
    }
}
