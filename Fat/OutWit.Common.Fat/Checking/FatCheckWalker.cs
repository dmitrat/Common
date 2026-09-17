using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// One walk of the tree, from the root down, for <see cref="FatCheckTree"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The walk keeps its own stack, so a deep tree cannot exhaust the thread's, and the first
    /// clusters of the directories it is in, so a directory that holds one of them is known
    /// at once.
    /// </para>
    /// <para>
    /// A directory waiting to be read keeps its name, not its path, and entries are checked a
    /// chunk at a time, so memory follows the number of directories, not the length of their
    /// paths; a directory whose path would be longer than Windows allows is not entered.
    /// </para>
    /// </remarks>
    internal sealed class FatCheckWalker
    {
        #region Constants

        /// <summary>
        /// The longest path followed: the most Windows allows.
        /// </summary>
        public const int MAX_PATH_LENGTH = 32767;

        #endregion

        #region Fields

        private readonly FatCheckContext m_context;

        private readonly ExFatUpcaseTable? m_upcase;

        private readonly Stack<Visit> m_pending = new();

        private readonly Dictionary<uint, int> m_ancestors = new();

        #endregion

        #region Constructors

        public FatCheckWalker(FatCheckContext context, ExFatUpcaseTable? upcase)
        {
            m_context = context;
            m_upcase = upcase;
        }

        #endregion

        #region Functions

        public async ValueTask RunAsync(Visit root, CancellationToken cancellationToken)
        {
            m_pending.Push(root);
            while (m_pending.TryPop(out var visit))
            {
                if (visit.Directory == null)
                {
                    m_ancestors.Remove(visit.ExitCluster);
                    continue;
                }

                string path = m_context.Owners.Describe(visit.Owner);
                if (path.Length > MAX_PATH_LENGTH)
                {
                    m_context.Report(FatProblemKind.TooDeep, null, visit.Directory.Entry.FirstCluster,
                        $"A directory at cluster {visit.Directory.Entry.FirstCluster} has a path of {path.Length} characters, longer than any system follows; it was not entered.");
                    continue;
                }

                var directory = Relocate(visit.Directory, path);
                uint cluster = directory.Entry.FirstCluster;
                if (cluster != 0)
                {
                    m_ancestors[cluster] = visit.Owner;
                    m_pending.Push(new Visit(null, visit.Owner, false, cluster));
                }

                await ReadAsync(directory, visit, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks a directory's entries a chunk at a time, and queues its subdirectories.
        /// </summary>
        private async ValueTask ReadAsync(DirectoryItem directory, Visit visit, CancellationToken cancellationToken)
        {
            var core = m_context.Core;
            string path = directory.Entry.Path;
            var opened = core.OpenDirectory(directory);
            var exFat = core.ExFat != null ? new ExFatEntrySetParser(path, opened.Cluster) : null;
            var vfat = core.ExFat == null ? new DirectoryParser(path, core.HasHighCluster, opened.Cluster) : null;
            var children = new List<DirectoryItem>();
            var directories = new List<Visit>();

            try
            {
                await foreach (var chunk in opened.ReadSlotsAsync(cancellationToken).ConfigureAwait(false))
                {
                    children.Clear();
                    bool isEnd = Parse(path, chunk.Span, exFat, vfat, children);
                    foreach (var child in children)
                        await CheckChildAsync(child, directory, visit.Owner, directories, cancellationToken).ConfigureAwait(false);
                    if (isEnd)
                        break;
                }
            }
            catch (FatException error) when (error.Kind == FatErrorKind.Corrupt)
            {
                if (!visit.IsChainReported)
                    m_context.Report(FatProblemKind.BrokenEntry, path, directory.Entry.FirstCluster, error.Message);
            }

            exFat?.Finish();
            TakeProblems(path, exFat);
            if (vfat is { BrokenLongNames: > 0 })
                m_context.Report(FatProblemKind.BrokenEntry, path, null,
                    $"{vfat.BrokenLongNames} long names in {path} are cut short, belong to no entry, or do not match the entry after them.");

            for (int i = directories.Count - 1; i >= 0; i--)
                m_pending.Push(directories[i]);
        }

        private async ValueTask CheckChildAsync(DirectoryItem child, DirectoryItem directory, int parent, List<Visit> directories,
            CancellationToken cancellationToken)
        {
            var entry = child.Entry;
            if (entry.IsDirectory)
                m_context.Directories++;
            else
                m_context.Files++;

            if (m_upcase != null)
                FatCheckItem.CheckHash(m_context, child, m_upcase);
            if (entry.IsDirectory && IsLoop(child))
                return;

            var (owner, isWhole) = await FatCheckItem.ClaimAsync(m_context, child, parent, cancellationToken).ConfigureAwait(false);
            if (!isWhole || !entry.IsDirectory)
                return;

            if (m_context.Core.ExFat == null)
                await FatCheckItem.CheckDotEntriesAsync(m_context, child, directory, cancellationToken).ConfigureAwait(false);
            directories.Add(new Visit(Relocate(child, entry.Name), owner, false));
        }

        /// <returns>Whether the directory's end was reached.</returns>
        private bool Parse(string path, ReadOnlySpan<byte> chunk, ExFatEntrySetParser? exFat, DirectoryParser? vfat, List<DirectoryItem> children)
        {
            for (int offset = 0; offset + DirectorySlot.SIZE <= chunk.Length; offset += DirectorySlot.SIZE)
            {
                var slot = chunk.Slice(offset, DirectorySlot.SIZE);
                var kind = exFat != null ? exFat.Parse(slot, out var item, out _) : vfat!.Parse(slot, out item, out _);
                TakeProblems(path, exFat);
                if (kind == DirectorySlotKind.End)
                    return true;
                if (item != null)
                    children.Add(item);
            }

            return false;
        }

        private void TakeProblems(string path, ExFatEntrySetParser? exFat)
        {
            while (exFat?.TakeProblem() is { } problem)
                m_context.Report(FatProblemKind.BrokenEntry, path, null, problem.Message);
        }

        /// <summary>
        /// Whether a directory starts where one of those it lies in starts, and reports it.
        /// </summary>
        private bool IsLoop(DirectoryItem child)
        {
            uint cluster = child.Entry.FirstCluster;
            if (cluster == 0 || !m_ancestors.TryGetValue(cluster, out int ancestor))
                return false;

            m_context.Report(FatProblemKind.DirectoryLoop, child.Entry.Path, cluster, m_context.IsListed(FatProblemKind.DirectoryLoop)
                ? $"The directory {child.Entry.Path} starts at cluster {cluster}, where {m_context.Owners.Describe(ancestor)}, which holds it, starts."
                : string.Empty);
            return true;
        }

        /// <summary>
        /// The entry with another path, and nothing that keeps the directories above it alive.
        /// </summary>
        private static DirectoryItem Relocate(DirectoryItem item, string path)
        {
            var entry = item.Entry;
            var moved = new FatDirectoryEntry
            {
                Path = path,
                Name = entry.Name,
                ShortName = entry.ShortName,
                Attributes = entry.Attributes,
                Length = entry.Length,
                FirstCluster = entry.FirstCluster,
                Created = entry.Created,
                Modified = entry.Modified,
                Accessed = entry.Accessed
            };
            return new DirectoryItem(moved, item.DirectoryCluster, item.FirstSlot, item.LastSlot)
            {
                IsContiguous = item.IsContiguous,
                DataLength = item.DataLength,
                ValidLength = item.ValidLength,
                NameHash = item.NameHash,
                StoredName = item.StoredName
            };
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// A directory still to be read, by its name, with its owner; or, with no directory,
        /// the mark that the walk has left the one starting at <see cref="ExitCluster"/>.
        /// </summary>
        public sealed record Visit(DirectoryItem? Directory, int Owner, bool IsChainReported, uint ExitCluster = 0);

        #endregion
    }
}
