using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// Paths on a mounted volume: finding what they name and listing directories.
    /// </summary>
    internal sealed class FatNamespace
    {
        #region Fields

        private readonly FatVolumeCore m_core;

        #endregion

        #region Constructors

        public FatNamespace(FatVolumeCore core, FatDirectoryEntry root)
        {
            m_core = core;
            Root = new DirectoryItem(root, 0, -1, -1);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Opens a directory.
        /// </summary>
        public FatDirectory Open(DirectoryItem directory)
        {
            return m_core.OpenDirectory(directory);
        }

        /// <summary>
        /// Finds what a path names.
        /// </summary>
        /// <returns>The entry, or <c>null</c> when nothing can be there, including a path through a file.</returns>
        /// <exception cref="ArgumentException">The path climbs above the root.</exception>
        public async ValueTask<DirectoryItem?> FindAsync(IReadOnlyList<string> components, CancellationToken cancellationToken)
        {
            var current = Root;
            foreach (string name in components)
            {
                if (!current.Entry.IsDirectory)
                    return null;

                var found = await Open(current).FindAsync(name, cancellationToken).ConfigureAwait(false);
                if (found == null)
                    return null;
                current = found;
            }

            return current;
        }

        /// <summary>
        /// Finds what a path names, and throws when it is missing or of the wrong kind.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">The kind wanted, or <c>null</c> for either.</param>
        /// <param name="cancellationToken">Cancels the lookup.</param>
        /// <exception cref="FatException">Nothing is there, or it is of the other kind.</exception>
        public async ValueTask<DirectoryItem> RequireAsync(string path, bool? isDirectory, CancellationToken cancellationToken)
        {
            var item = await FindAsync(FatPath.Split(path), cancellationToken).ConfigureAwait(false)
                       ?? throw new FatException(FatErrorKind.NotFound, $"Nothing exists at {path}.");

            if (isDirectory is { } wanted && item.Entry.IsDirectory != wanted)
                throw wanted
                    ? new FatException(FatErrorKind.NotADirectory, $"{item.Entry.Path} is a file, not a directory.")
                    : new FatException(FatErrorKind.NotAFile, $"{item.Entry.Path} is a directory, not a file.");

            return item;
        }

        /// <summary>
        /// Finds the directory a path's last component would be in.
        /// </summary>
        /// <returns>The directory and the last component.</returns>
        /// <exception cref="ArgumentException">The path is the root, or climbs above it.</exception>
        /// <exception cref="FatException">The directory is missing, or is a file.</exception>
        public async ValueTask<(DirectoryItem Parent, string Name)> RequireParentAsync(string path, CancellationToken cancellationToken)
        {
            var components = FatPath.Split(path);
            if (components.Count == 0)
                throw new ArgumentException("The path names the root, which has no name of its own.", nameof(path));

            var parentComponents = components.Take(components.Count - 1).ToList();
            var parent = await FindAsync(parentComponents, cancellationToken).ConfigureAwait(false)
                         ?? throw new FatException(FatErrorKind.NotFound, $"The directory {FatPath.Join(parentComponents)} does not exist.");
            if (!parent.Entry.IsDirectory)
                throw new FatException(FatErrorKind.NotADirectory, $"{parent.Entry.Path} is a file, not a directory.");

            return (parent, components[^1]);
        }

        /// <summary>
        /// Tells whether a directory is one of the directories a path runs through, the
        /// last one included.
        /// </summary>
        /// <remarks>
        /// Directories are told apart by their entry's place, not their first cluster, which
        /// an exFAT directory of size zero does not have.
        /// </remarks>
        public async ValueTask<bool> PassesThroughAsync(IReadOnlyList<string> components, DirectoryItem directory, CancellationToken cancellationToken)
        {
            var current = Root;
            foreach (string name in components)
            {
                var found = await Open(current).FindAsync(name, cancellationToken).ConfigureAwait(false);
                if (found == null || !found.Entry.IsDirectory)
                    return false;
                if (found.Key == directory.Key)
                    return true;
                current = found;
            }

            return false;
        }

        /// <summary>
        /// Lists a directory, and its subdirectories when asked.
        /// </summary>
        /// <exception cref="FatException">
        /// The path does not name a directory, a directory is corrupt, or a directory is its
        /// own ancestor.
        /// </exception>
        public async IAsyncEnumerable<FatDirectoryEntry> EnumerateAsync(string path, bool recursive,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var directory = await RequireAsync(path, isDirectory: true, cancellationToken).ConfigureAwait(false);

            var pending = new Stack<(IAsyncEnumerator<DirectoryItem> Listing, uint Cluster)>();
            pending.Push((Open(directory).EnumerateAsync(cancellationToken).GetAsyncEnumerator(cancellationToken), directory.Entry.FirstCluster));
            try
            {
                while (pending.Count > 0)
                {
                    var listing = pending.Peek().Listing;
                    if (!await listing.MoveNextAsync().ConfigureAwait(false))
                    {
                        await pending.Pop().Listing.DisposeAsync().ConfigureAwait(false);
                        continue;
                    }

                    var item = listing.Current;
                    yield return item.Entry;

                    if (!recursive || !item.Entry.IsDirectory)
                        continue;

                    uint cluster = item.Entry.FirstCluster;
                    if (cluster != 0 && (cluster == Root.Entry.FirstCluster || pending.Any(open => open.Cluster == cluster)))
                        throw new FatException(FatErrorKind.Corrupt,
                            $"The directory {item.Entry.Path} starts at cluster {cluster}, which is one of its own ancestors.");

                    pending.Push((Open(item).EnumerateAsync(cancellationToken).GetAsyncEnumerator(cancellationToken), cluster));
                }
            }
            finally
            {
                while (pending.Count > 0)
                    await pending.Pop().Listing.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The root directory, which lies in no directory.
        /// </summary>
        public DirectoryItem Root { get; }

        #endregion
    }
}
