using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Files;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// An exFAT directory: entry sets, and in the root the label, the allocation bitmap and
    /// the up-case table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A directory other than the root is as long as its stream extension says, at most
    /// <see cref="MAX_BYTES"/>, and may lie in contiguous clusters the table does not
    /// describe. The root is as long as its chain. There are no <c>.</c> and <c>..</c>
    /// entries. A directory that grows records its new size in its parent, and keeps its
    /// clusters without a table chain while the new ones follow the old.
    /// </para>
    /// <para>
    /// A broken entry set stops a listing when the listing reaches it, after the entries
    /// before it. A lookup passes over it, and reports it only when the name is not found
    /// elsewhere, since the broken set may be the one looked for. The root's critical
    /// entries are found whatever the sets around them hold.
    /// </para>
    /// <para>
    /// Names are compared with the volume's up-case table, which is read the first time two
    /// names are compared.
    /// </para>
    /// </remarks>
    internal sealed class FatDirectoryExFat : FatDirectory
    {
        #region Constants

        public const long MAX_BYTES = 256L * 1024 * 1024;

        #endregion

        #region Constructors

        public FatDirectoryExFat(FatVolumeCore core, DirectoryItem item)
            : base(core, item)
        {
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        public override async IAsyncEnumerable<DirectoryItem> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var parser = NewParser();
            await foreach (var chunk in ReadChunksAsync(cancellationToken).ConfigureAwait(false))
            {
                for (int offset = 0; offset + ExFatEntry.SIZE <= chunk.Length; offset += ExFatEntry.SIZE)
                {
                    var kind = ParseSlot(parser, chunk, offset, out var item, out _);
                    if (parser.TakeProblem() is { } problem)
                        throw problem;
                    if (kind == DirectorySlotKind.End)
                        yield break;
                    if (item != null)
                        yield return item;
                }
            }

            parser.Finish();
            if (parser.TakeProblem() is { } cut)
                throw cut;
        }

        /// <inheritdoc />
        /// <exception cref="FatException">The name is not found and the directory holds a broken set.</exception>
        public override async ValueTask<DirectoryItem?> FindAsync(string name, CancellationToken cancellationToken)
        {
            var parser = NewParser();
            ExFatUpcaseTable? upcase = null;
            FatException? problem = null;

            await foreach (var chunk in ReadChunksAsync(cancellationToken).ConfigureAwait(false))
            {
                for (int offset = 0; offset + ExFatEntry.SIZE <= chunk.Length; offset += ExFatEntry.SIZE)
                {
                    var kind = ParseSlot(parser, chunk, offset, out var item, out _);
                    problem ??= Drain(parser);
                    if (kind == DirectorySlotKind.End)
                        return problem != null ? throw problem : null;
                    if (item == null)
                        continue;

                    upcase ??= await Core.ExFat!.GetUpcaseTableAsync(cancellationToken).ConfigureAwait(false);
                    if (upcase.NamesEqual(item.Entry.Name, name))
                        return item;
                }
            }

            parser.Finish();
            problem ??= Drain(parser);
            return problem != null ? throw problem : null;
        }

        /// <inheritdoc />
        public override async ValueTask<string?> ReadLabelAsync(CancellationToken cancellationToken)
        {
            var (label, _) = await ReadRootEntriesAsync(cancellationToken).ConfigureAwait(false);
            return label;
        }

        /// <summary>
        /// Reads what the root holds besides files: the label, and every allocation bitmap
        /// and up-case table entry, in order. Broken sets do not matter here.
        /// </summary>
        public async ValueTask<(string? Label, List<ExFatRootEntry> Critical)> ReadRootEntriesAsync(CancellationToken cancellationToken)
        {
            var parser = NewParser();
            string? label = null;
            var critical = new List<ExFatRootEntry>();

            await foreach (var chunk in ReadChunksAsync(cancellationToken).ConfigureAwait(false))
            {
                for (int offset = 0; offset + ExFatEntry.SIZE <= chunk.Length; offset += ExFatEntry.SIZE)
                {
                    var kind = ParseSlot(parser, chunk, offset, out _, out string? found);
                    Drain(parser);
                    if (kind == DirectorySlotKind.End)
                        return (label, critical);
                    if (kind == DirectorySlotKind.Label)
                        label ??= found;
                    else if (kind == DirectorySlotKind.Critical)
                        critical.Add(parser.Critical);
                }
            }

            return (label, critical);
        }

        /// <summary>
        /// The entry a set just written at <paramref name="start"/> describes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The set does not describe an entry.</exception>
        public DirectoryItem Describe(byte[] set, long start)
        {
            var parser = new ExFatEntrySetParser(Entry.Path, Cluster, Item, start);
            DirectoryItem? item = null;
            for (int offset = 0; offset < set.Length; offset += ExFatEntry.SIZE)
                parser.Parse(set.AsSpan(offset, ExFatEntry.SIZE), out item, out _);

            if (parser.TakeProblem() is { } problem)
                throw new InvalidOperationException("The set written is not valid: " + problem.Message, problem);
            return item ?? throw new InvalidOperationException("The set written does not describe an entry.");
        }

        /// <inheritdoc />
        /// <remarks>
        /// The root grows by a table chain. Any other directory grows by clusters that follow
        /// its own when they are free, and otherwise gets a table chain through all of them.
        /// New clusters are zeroed before they are linked or counted, and the new size is
        /// recorded in the parent once the bitmap and the table are written.
        /// </remarks>
        public override async ValueTask GrowAsync(long slots, CancellationToken cancellationToken)
        {
            if (Item.IsRoot)
            {
                await base.GrowAsync(slots, cancellationToken).ConfigureAwait(false);
                return;
            }

            var parent = Item.Parent ?? throw new InvalidOperationException($"The directory {Entry.Path} was not found through its parent.");
            long capacity = await GetCapacityAsync(cancellationToken).ConfigureAwait(false);
            long clusters = (slots * DirectorySlot.SIZE + Core.ClusterSize - 1) / Core.ClusterSize;
            if ((capacity + clusters * Core.ClusterSize / DirectorySlot.SIZE) * DirectorySlot.SIZE > MAX_BYTES)
                throw new FatException(FatErrorKind.NoSpace, $"The directory {Entry.Path} is at its limit of {MAX_BYTES / DirectorySlot.SIZE} entries.");

            var allocator = Core.Allocator;
            bool isContiguous;
            ClusterChain chain;
            List<ClusterRun> runs;
            if (Entry.FirstCluster == 0)
            {
                runs = await TakeZeroedAsync(0, clusters, 0, cancellationToken).ConfigureAwait(false);
                isContiguous = runs.Count == 1;
                if (!isContiguous)
                    await LinkOrGiveBackAsync(runs, runs, 0, cancellationToken).ConfigureAwait(false);
                chain = ClusterChain.FromRuns(Core.Table, runs);
            }
            else
            {
                chain = GetChain();
                var existing = await chain.ReadAllAsync(cancellationToken).ConfigureAwait(false);
                uint last = chain.LastCluster;
                runs = await TakeZeroedAsync(last, clusters, chain.KnownCount, cancellationToken).ConfigureAwait(false);
                isContiguous = Item.IsContiguous && runs.Count == 1 && runs[0].FirstCluster == last + 1;
                if (Item.IsContiguous && !isContiguous)
                    await LinkOrGiveBackAsync(existing.Concat(runs).ToList(), runs, 0, cancellationToken).ConfigureAwait(false);
                else if (!Item.IsContiguous)
                    await LinkOrGiveBackAsync(runs, runs, last, cancellationToken).ConfigureAwait(false);
                chain.Append(runs);
            }

            await allocator.FlushAsync(cancellationToken).ConfigureAwait(false);

            long size = chain.KnownCount * Core.ClusterSize;
            var state = new FatFileState(Entry.FirstCluster != 0 ? Entry.FirstCluster : runs[0].FirstCluster, size, size, isContiguous);
            var writer = FatDirectoryWriter.Open(Core, Core.OpenDirectory(parent));
            Replace(await writer.UpdateAsync(Item, state, false, cancellationToken).ConfigureAwait(false), chain);
        }

        /// <inheritdoc />
        protected override bool IsFreeSlot(byte first)
        {
            return !ExFatEntry.IsInUse(first);
        }

        private ExFatEntrySetParser NewParser()
        {
            return new ExFatEntrySetParser(Entry.Path, Cluster, Item);
        }

        private static DirectorySlotKind ParseSlot(ExFatEntrySetParser parser, ReadOnlyMemory<byte> chunk, int offset,
            out DirectoryItem? item, out string? label)
        {
            return parser.Parse(chunk.Span.Slice(offset, ExFatEntry.SIZE), out item, out label);
        }

        /// <summary>
        /// Takes every problem the parser holds and gives the first.
        /// </summary>
        private static FatException? Drain(ExFatEntrySetParser parser)
        {
            var first = parser.TakeProblem();
            while (parser.TakeProblem() != null)
            {
            }

            return first;
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        protected override long MaxBytes => MAX_BYTES;

        /// <inheritdoc />
        protected override long? RecordedBytes => Item.IsRoot ? null : Item.DataLength;

        #endregion
    }
}
