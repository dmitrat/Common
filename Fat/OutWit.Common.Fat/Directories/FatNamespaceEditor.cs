using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Formatting;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Changes to the tree of a mounted volume: creating, deleting and moving entries,
    /// setting their attributes, and setting the volume label.
    /// </summary>
    /// <remarks>
    /// Each operation that stands alone ends with a flush, so the volume is consistent
    /// after it returns. The order within an operation keeps a crash from losing data:
    /// a moved entry is written in its new place before the old one is removed, and a
    /// deleted entry is removed before its clusters are released, so the worst a crash
    /// leaves is clusters that belong to no one.
    /// </remarks>
    internal sealed class FatNamespaceEditor
    {
        #region Fields

        private readonly FatVolumeCore m_core;

        private readonly FatNamespace m_names;

        #endregion

        #region Constructors

        public FatNamespaceEditor(FatVolumeCore core, FatNamespace names)
        {
            m_core = core;
            m_names = names;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Adds an empty file to a directory. Does not flush.
        /// </summary>
        public async ValueTask<DirectoryItem> CreateFileAsync(FatDirectory directory, string name, CancellationToken cancellationToken)
        {
            await m_core.BeginChangeAsync(cancellationToken).ConfigureAwait(false);
            var writer = FatDirectoryWriter.Open(m_core, directory);
            return await writer.AddAsync(name, writer.NewTemplate(FatAttributes.Archive, 0, 0, false), null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a directory and any missing directories above it.
        /// </summary>
        /// <returns>The directory, new or not.</returns>
        /// <exception cref="FatException">A component is a file.</exception>
        public async ValueTask<FatDirectoryEntry> CreateDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            var components = FatPath.Split(path);
            var current = m_names.Root;

            for (int i = 0; i < components.Count; i++)
            {
                var directory = m_names.Open(current);
                var found = await directory.FindAsync(components[i], cancellationToken).ConfigureAwait(false);
                if (found != null)
                {
                    if (!found.Entry.IsDirectory)
                        throw i == components.Count - 1
                            ? new FatException(FatErrorKind.AlreadyExists, $"{found.Entry.Path} already exists as a file.")
                            : new FatException(FatErrorKind.NotADirectory, $"{found.Entry.Path} is a file, not a directory.");

                    current = found;
                    continue;
                }

                current = await AddDirectoryAsync(current, directory, components[i], cancellationToken).ConfigureAwait(false);
            }

            await m_core.FlushAsync(cancellationToken).ConfigureAwait(false);
            return current.Entry;
        }

        /// <summary>
        /// Deletes a file or a directory.
        /// </summary>
        /// <remarks>
        /// A tree is deleted entry by entry, so a deletion that stops — at an entry that is
        /// open or read-only, or when cancelled — leaves what it deleted by then deleted,
        /// and writes that much out before it reports why it stopped.
        /// </remarks>
        /// <exception cref="ArgumentException">The path is the root.</exception>
        /// <exception cref="FatException">
        /// Nothing is there; the entry, or one below it, is read-only or open; or the
        /// directory has entries and <paramref name="recursive"/> is not set.
        /// </exception>
        public async ValueTask DeleteAsync(string path, bool recursive, CancellationToken cancellationToken)
        {
            var (parent, name) = await m_names.RequireParentAsync(path, cancellationToken).ConfigureAwait(false);
            var directory = m_names.Open(parent);
            var item = await directory.FindAsync(name, cancellationToken).ConfigureAwait(false)
                       ?? throw new FatException(FatErrorKind.NotFound, $"Nothing exists at {path}.");

            await m_core.BeginChangeAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await DeleteItemAsync(directory, item, recursive, new HashSet<uint> { m_names.Root.Entry.FirstCluster }, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await FlushDeletedAsync().ConfigureAwait(false);
                throw;
            }

            await m_core.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes out what a deletion that stopped had released, so that the clusters of
        /// entries already removed are not left in memory only; best effort, since the
        /// failure that stopped the deletion is the one to report.
        /// </summary>
        private async ValueTask FlushDeletedAsync()
        {
            try
            {
                await m_core.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The releases stay in memory until the next flush; FatChecker reports them if it never comes.
            }
        }

        /// <summary>
        /// Renames or moves a file or a directory.
        /// </summary>
        /// <returns>The entry in its new place.</returns>
        /// <exception cref="ArgumentException">A path is the root, or a directory would move into itself.</exception>
        /// <exception cref="FatException">
        /// The source is missing or open; the destination's directory is missing; or the
        /// destination exists and may not be replaced.
        /// </exception>
        public async ValueTask<FatDirectoryEntry> MoveAsync(string source, string destination, bool overwrite, CancellationToken cancellationToken)
        {
            var (sourceParent, sourceName) = await m_names.RequireParentAsync(source, cancellationToken).ConfigureAwait(false);
            var (targetParent, targetName) = await m_names.RequireParentAsync(destination, cancellationToken).ConfigureAwait(false);

            var sourceDirectory = m_names.Open(sourceParent);
            var item = await sourceDirectory.FindAsync(sourceName, cancellationToken).ConfigureAwait(false)
                       ?? throw new FatException(FatErrorKind.NotFound, $"Nothing exists at {source}.");
            bool isDirectory = item.Entry.IsDirectory;
            if (!isDirectory)
                m_core.OpenFiles.ThrowIfOpen(item);

            var targetComponents = FatPath.Split(destination);
            var targetParentComponents = targetComponents.Take(targetComponents.Count - 1).ToList();
            if (isDirectory && await m_names.PassesThroughAsync(targetParentComponents, item, cancellationToken).ConfigureAwait(false))
                throw new ArgumentException($"{item.Entry.Path} cannot move into itself.", nameof(destination));

            bool isSameDirectory = targetParent.Key == sourceParent.Key;
            var targetDirectory = isSameDirectory ? sourceDirectory : m_names.Open(targetParent);

            var existing = await targetDirectory.FindAsync(targetName, cancellationToken).ConfigureAwait(false);
            if (existing != null && existing.Key == item.Key)
            {
                if (existing.Entry.Name == targetName)
                    return item.Entry;
                existing = null;
            }

            await m_core.BeginChangeAsync(cancellationToken).ConfigureAwait(false);
            var sourceWriter = FatDirectoryWriter.Open(m_core, sourceDirectory);
            var targetWriter = isSameDirectory ? sourceWriter : FatDirectoryWriter.Open(m_core, targetDirectory);
            var template = await sourceWriter.ReadTemplateAsync(item, cancellationToken).ConfigureAwait(false);

            List<ClusterRun>? replaced = null;
            byte[]? replacedSlots = null;
            if (existing != null)
            {
                if (!overwrite || isDirectory || existing.Entry.IsDirectory)
                    throw new FatException(FatErrorKind.AlreadyExists, $"{existing.Entry.Path} already exists.");
                CheckDeletable(existing);
                replaced = await ReadChainAsync(existing, cancellationToken).ConfigureAwait(false);
                replacedSlots = await targetWriter.RemoveAsync(existing, cancellationToken).ConfigureAwait(false);
            }

            DirectoryItem moved;
            try
            {
                moved = await targetWriter.AddAsync(targetName, template, isSameDirectory ? item : null, cancellationToken).ConfigureAwait(false);
            }
            catch when (existing != null && replacedSlots != null)
            {
                await RestoreQuietlyAsync(targetWriter, existing, replacedSlots).ConfigureAwait(false);
                throw;
            }

            await sourceWriter.RemoveAsync(item, cancellationToken).ConfigureAwait(false);

            if (isDirectory && !isSameDirectory)
                await FatDirectoryWriter.Open(m_core, m_names.Open(moved)).SetParentAsync(targetParent, cancellationToken).ConfigureAwait(false);

            if (replaced != null)
                await m_core.Allocator.ReleaseAsync(replaced, cancellationToken).ConfigureAwait(false);

            await m_core.FlushAsync(cancellationToken).ConfigureAwait(false);
            return moved.Entry;
        }

        /// <summary>
        /// Sets the attributes a caller may set on an entry.
        /// </summary>
        /// <returns>The entry as it now reads.</returns>
        /// <exception cref="ArgumentException">The path is the root, or an attribute is not one a caller may set.</exception>
        /// <exception cref="FatException">Nothing is there.</exception>
        public async ValueTask<FatDirectoryEntry> SetAttributesAsync(string path, FatAttributes attributes, CancellationToken cancellationToken)
        {
            if ((attributes & ~DirectorySlot.SETTABLE_ATTRIBUTES) != 0)
                throw new ArgumentException($"Only {DirectorySlot.SETTABLE_ATTRIBUTES} can be set; {attributes} was given.", nameof(attributes));

            var (parent, name) = await m_names.RequireParentAsync(path, cancellationToken).ConfigureAwait(false);
            var directory = m_names.Open(parent);
            var item = await directory.FindAsync(name, cancellationToken).ConfigureAwait(false)
                       ?? throw new FatException(FatErrorKind.NotFound, $"Nothing exists at {path}.");
            var kept = item.Entry.Attributes & ~DirectorySlot.SETTABLE_ATTRIBUTES;
            if (item.Entry.Attributes == (kept | attributes))
                return item.Entry;

            await m_core.BeginChangeAsync(cancellationToken).ConfigureAwait(false);
            var changed = await FatDirectoryWriter.Open(m_core, directory).SetAttributesAsync(item, attributes, cancellationToken).ConfigureAwait(false);
            await m_core.FlushAsync(cancellationToken).ConfigureAwait(false);
            return changed.Entry;
        }

        /// <summary>
        /// Sets or removes the volume label: in the root and, on FAT12/16/32, in the boot
        /// sector and its backup, as Windows and fatlabel keep it.
        /// </summary>
        /// <returns>The label as stored, or <c>null</c> when there is none now.</returns>
        /// <exception cref="ArgumentException">The label is too long or holds a character the format does not allow.</exception>
        /// <exception cref="FatException">The root has no room for a label entry.</exception>
        public async ValueTask<string?> SetLabelAsync(string? label, CancellationToken cancellationToken)
        {
            string? normalized = FatFormatPlan.NormalizeLabel(m_core.Info.Kind, label);
            await m_core.BeginChangeAsync(cancellationToken).ConfigureAwait(false);
            await FatDirectoryWriter.Open(m_core, m_names.Open(m_names.Root)).SetLabelAsync(normalized, cancellationToken).ConfigureAwait(false);
            if (m_core.ExFat == null)
            {
                var bytes = FatFormatPlan.LabelBytes(normalized);
                await SetBootLabelAsync(0, bytes, cancellationToken).ConfigureAwait(false);
                if (m_core.Info.BackupBootSector is { } backup)
                    await SetBootLabelAsync(backup, bytes, cancellationToken).ConfigureAwait(false);
            }

            await m_core.FlushAsync(cancellationToken).ConfigureAwait(false);
            return normalized;
        }

        private async ValueTask SetBootLabelAsync(long sector, byte[] label, CancellationToken cancellationToken)
        {
            var bytes = new byte[m_core.Info.SectorSize];
            await m_core.Device.ReadAsync(sector, bytes, cancellationToken).ConfigureAwait(false);
            if (FatBootSectorWriter.SetLabel(bytes, m_core.Info.Kind, label))
                await m_core.Device.WriteAsync(sector, bytes, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a directory with one zeroed cluster: on exFAT a cluster its entry counts as
        /// its size and no table chain describes, as Linux makes one.
        /// </summary>
        /// <remarks>
        /// The cluster is marked used on the device before the entry that counts on it is
        /// written. It is given back when the entry could not be added for a reason found
        /// before anything was written — a taken name, no room — and otherwise stays taken,
        /// since part of the entry may be on the device already.
        /// </remarks>
        private async ValueTask<DirectoryItem> AddDirectoryAsync(DirectoryItem parent, FatDirectory directory, string name, CancellationToken cancellationToken)
        {
            ShortNameBuilder.Validate(name);
            await m_core.BeginChangeAsync(cancellationToken).ConfigureAwait(false);

            bool isExFat = m_core.ExFat != null;
            var runs = await m_core.Allocator.AllocateAsync(0, 1, 0, !isExFat, cancellationToken).ConfigureAwait(false);
            uint cluster = runs[0].FirstCluster;
            bool isAdding = false;
            try
            {
                await m_core.ZeroClustersAsync(runs[0], cancellationToken).ConfigureAwait(false);
                var writer = FatDirectoryWriter.Open(m_core, directory);
                await writer.InitializeDirectoryAsync(cluster, parent, cancellationToken).ConfigureAwait(false);
                await m_core.Allocator.FlushAsync(cancellationToken).ConfigureAwait(false);
                var template = writer.NewTemplate(FatAttributes.Directory, cluster, isExFat ? m_core.ClusterSize : 0, isExFat);
                isAdding = true;
                return await writer.AddAsync(name, template, null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception error) when (!isAdding || error is FatException or ArgumentException)
            {
                await m_core.Allocator.ReleaseAsync(runs, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        /// <remarks>
        /// Best effort: the failure that made the move give up is the one reported.
        /// </remarks>
        private static async ValueTask RestoreQuietlyAsync(FatDirectoryWriter writer, DirectoryItem item, byte[] slots)
        {
            try
            {
                await writer.RestoreAsync(item, slots, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The entry stays removed and its clusters lost; FatChecker reports them.
            }
        }

        private async ValueTask DeleteItemAsync(FatDirectory parent, DirectoryItem item, bool recursive, HashSet<uint> ancestors,
            CancellationToken cancellationToken)
        {
            CheckDeletable(item);

            if (item.Entry.IsDirectory)
            {
                if (item.Entry.FirstCluster != 0 && !ancestors.Add(item.Entry.FirstCluster))
                    throw new FatException(FatErrorKind.Corrupt, $"The directory {item.Entry.Path} is one of its own ancestors.");

                var directory = m_names.Open(item);
                var children = new List<DirectoryItem>();
                await foreach (var child in directory.EnumerateAsync(cancellationToken).ConfigureAwait(false))
                    children.Add(child);

                if (children.Count > 0 && !recursive)
                    throw new FatException(FatErrorKind.NotEmpty, $"The directory {item.Entry.Path} is not empty.");

                foreach (var child in children)
                    await DeleteItemAsync(directory, child, recursive, ancestors, cancellationToken).ConfigureAwait(false);

                if (item.Entry.FirstCluster != 0)
                    ancestors.Remove(item.Entry.FirstCluster);
            }

            var runs = await ReadChainAsync(item, cancellationToken).ConfigureAwait(false);
            await FatDirectoryWriter.Open(m_core, parent).RemoveAsync(item, cancellationToken).ConfigureAwait(false);
            await m_core.Allocator.ReleaseAsync(runs, cancellationToken).ConfigureAwait(false);
        }

        private void CheckDeletable(DirectoryItem item)
        {
            if ((item.Entry.Attributes & FatAttributes.ReadOnly) != 0)
                throw new FatException(FatErrorKind.AccessDenied, $"{item.Entry.Path} is read-only.");
            if (!item.Entry.IsDirectory)
                m_core.OpenFiles.ThrowIfOpen(item);
        }

        private async ValueTask<List<ClusterRun>> ReadChainAsync(DirectoryItem item, CancellationToken cancellationToken)
        {
            if (item.Entry.FirstCluster == 0)
                return new List<ClusterRun>();

            var runs = await m_core.OpenChain(item).ReadAllAsync(cancellationToken).ConfigureAwait(false);
            return new List<ClusterRun>(runs);
        }

        #endregion
    }
}
