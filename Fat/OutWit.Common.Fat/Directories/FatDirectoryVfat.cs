using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// A FAT12, FAT16 or FAT32 directory: short entries with long names in front of them.
    /// </summary>
    /// <remarks>
    /// A directory holds at most <see cref="MAX_SLOTS"/> slots, as Windows and FatFs limit
    /// it; a chain that goes on past that without an end marker is corrupt.
    /// </remarks>
    internal sealed class FatDirectoryVfat : FatDirectory
    {
        #region Constants

        public const int MAX_SLOTS = 65536;

        public const int MAX_BYTES = MAX_SLOTS * DirectorySlot.SIZE;

        #endregion

        #region Constructors

        public FatDirectoryVfat(FatVolumeCore core, DirectoryItem item)
            : base(core, item)
        {
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        public override async IAsyncEnumerable<DirectoryItem> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var parser = new DirectoryParser(Entry.Path, Core.HasHighCluster, Cluster);
            var found = new List<DirectoryItem>();

            await foreach (var chunk in ReadChunksAsync(cancellationToken).ConfigureAwait(false))
            {
                found.Clear();
                bool isEnd = Parse(parser, chunk, found, null);
                foreach (var item in found)
                    yield return item;

                if (isEnd)
                    yield break;
            }
        }

        /// <inheritdoc />
        /// <remarks>By long or short name.</remarks>
        public override async ValueTask<DirectoryItem?> FindAsync(string name, CancellationToken cancellationToken)
        {
            await foreach (var item in EnumerateAsync(cancellationToken).ConfigureAwait(false))
            {
                if (string.Equals(item.Entry.Name, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.Entry.ShortName, name, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        /// <inheritdoc />
        public override async ValueTask<string?> ReadLabelAsync(CancellationToken cancellationToken)
        {
            var parser = new DirectoryParser(Entry.Path, false);
            var labels = new List<string>();

            await foreach (var chunk in ReadChunksAsync(cancellationToken).ConfigureAwait(false))
            {
                bool isEnd = Parse(parser, chunk, null, labels);
                if (labels.Count > 0)
                    return labels[0];
                if (isEnd)
                    break;
            }

            return null;
        }

        /// <inheritdoc />
        protected override bool IsFreeSlot(byte first)
        {
            return first == DirectorySlot.DELETED;
        }

        private static bool Parse(DirectoryParser parser, ReadOnlyMemory<byte> chunk, List<DirectoryItem>? items, List<string>? labels)
        {
            var span = chunk.Span;
            for (int offset = 0; offset + DirectorySlot.SIZE <= span.Length; offset += DirectorySlot.SIZE)
            {
                switch (parser.Parse(span.Slice(offset, DirectorySlot.SIZE), out var item, out var label))
                {
                    case DirectorySlotKind.End:
                        return true;
                    case DirectorySlotKind.Entry when item != null:
                        items?.Add(item);
                        break;
                    case DirectorySlotKind.Label when label != null:
                        labels?.Add(label);
                        break;
                }
            }

            return false;
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        protected override long MaxBytes => MAX_BYTES;

        #endregion
    }
}
