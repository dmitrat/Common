using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Files;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// A mounted FAT12, FAT16, FAT32 or exFAT volume.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mounting reads the boot sector — exFAT's boot region — and nothing else.
    /// Directories, the allocation table and file data are read when an operation needs
    /// them; put the device behind a <see cref="BlockDeviceCached"/> when it is slow.
    /// </para>
    /// <para>
    /// Paths use '/' or '\' between components and are rooted either way. Names match
    /// without regard to case: on FAT12/16/32 by long name or by 8.3 alias, on exFAT by
    /// the volume's up-case table. New names get an 8.3 alias the way Linux makes them,
    /// and a long name only when the alias cannot hold them.
    /// </para>
    /// <para>
    /// On exFAT the first change marks the volume dirty; <see cref="FlushAsync"/> and
    /// disposal clear the mark once everything is written.
    /// </para>
    /// <para>
    /// Creating, deleting and moving flush before they return. Writes to a file reach the
    /// directory and the allocation table when its stream is flushed or disposed. A file
    /// may be open for reading any number of times, or for writing once. The volume is not
    /// safe for concurrent use.
    /// </para>
    /// </remarks>
    public sealed class FatVolume : IAsyncDisposable
    {
        #region Fields

        private readonly FatVolumeCore m_core;

        private readonly FatNamespace m_names;

        private readonly FatNamespaceEditor m_editor;

        private readonly FatFileOpener m_opener;

        private readonly IAsyncDisposable[] m_owned;

        private bool m_isReleased;

        #endregion

        #region Constructors

        private FatVolume(IBlockDevice device, FatVolumeInfo info, FatVolumeOptions options, IAsyncDisposable[] owned)
        {
            m_core = new FatVolumeCore(device, info, options.Clock);
            m_owned = owned;
            Root = new FatDirectoryEntry
            {
                Path = FatPath.ROOT,
                Attributes = FatAttributes.Directory,
                FirstCluster = info.RootCluster ?? 0
            };
            m_names = new FatNamespace(m_core, Root);
            m_editor = new FatNamespaceEditor(m_core, m_names);
            m_opener = new FatFileOpener(m_core, m_names, m_editor);
        }

        #endregion

        #region Mount

        /// <summary>
        /// Mounts the volume that starts at sector zero of a device.
        /// </summary>
        /// <param name="device">A partition, or a disk without a partition table.</param>
        /// <param name="options">How to mount it; <see cref="FatVolumeOptions.DEFAULT"/> when <c>null</c>.</param>
        /// <param name="cancellationToken">Cancels the mount.</param>
        /// <returns>The mounted volume.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <exception cref="FatException">
        /// The device holds no FAT or exFAT volume, or one that is broken or of another
        /// sector size.
        /// </exception>
        public static async ValueTask<FatVolume> MountAsync(IBlockDevice device, FatVolumeOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(device);
            options ??= FatVolumeOptions.DEFAULT;

            var (volumeDevice, info, owned) = await FatVolumeMounter.FindVolumeAsync(device, options.LeaveOpen, cancellationToken).ConfigureAwait(false);
            return new FatVolume(volumeDevice, info, options, owned);
        }

        /// <summary>
        /// Mounts the first FAT12, FAT16, FAT32 or exFAT volume on a disk, partitioned or not.
        /// </summary>
        /// <param name="disk">The disk.</param>
        /// <param name="options">How to mount it; <see cref="FatVolumeOptions.DEFAULT"/> when <c>null</c>.</param>
        /// <param name="cancellationToken">Cancels the mount.</param>
        /// <returns>The mounted volume.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="disk"/> is <c>null</c>.</exception>
        /// <exception cref="FatException">
        /// The disk holds no usable FAT volume; the message names the partitions that were
        /// rejected. Also as for <see cref="FatDetector.DetectDiskAsync"/>.
        /// </exception>
        public static async ValueTask<FatVolume> MountDiskAsync(IBlockDevice disk, FatVolumeOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(disk);
            options ??= FatVolumeOptions.DEFAULT;

            var (device, info, owned) = await FatVolumeMounter.FindOnDiskAsync(disk, options.LeaveOpen, cancellationToken).ConfigureAwait(false);
            return new FatVolume(device, info, options, owned);
        }

        #endregion

        #region Read

        /// <summary>
        /// Reads the volume label from the root directory.
        /// </summary>
        /// <param name="cancellationToken">Cancels the read.</param>
        /// <returns>The label, or <c>null</c> when the volume has none.</returns>
        /// <exception cref="FatException">The root directory is corrupt.</exception>
        public ValueTask<string?> GetLabelAsync(CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfDisposed();
            return m_names.Open(m_names.Root).ReadLabelAsync(cancellationToken);
        }

        /// <summary>
        /// Finds the entry at a path.
        /// </summary>
        /// <param name="path">The path; "/" is the root.</param>
        /// <param name="cancellationToken">Cancels the lookup.</param>
        /// <returns>The entry, or <c>null</c> when nothing exists there.</returns>
        /// <exception cref="ArgumentException">The path climbs above the root.</exception>
        /// <exception cref="FatException">A directory on the way is corrupt.</exception>
        public async ValueTask<FatDirectoryEntry?> GetEntryAsync(string path, CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfDisposed();
            var item = await m_names.FindAsync(FatPath.Split(path), cancellationToken).ConfigureAwait(false);
            return item?.Entry;
        }

        /// <summary>
        /// Lists a directory.
        /// </summary>
        /// <param name="path">The directory; "/" is the root.</param>
        /// <param name="recursive">
        /// Whether to list subdirectories too, each right after its own entry. A directory
        /// that turns out to be its own ancestor is reported as corrupt.
        /// </param>
        /// <param name="cancellationToken">Cancels the listing.</param>
        /// <returns>The entries, in directory order, without <c>.</c>, <c>..</c> and the volume label.</returns>
        /// <exception cref="ArgumentException">The path climbs above the root.</exception>
        /// <exception cref="FatException">The path does not name a directory, or a directory is corrupt.</exception>
        public IAsyncEnumerable<FatDirectoryEntry> EnumerateAsync(string path = FatPath.ROOT, bool recursive = false,
            CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfDisposed();
            return m_names.EnumerateAsync(path, recursive, cancellationToken);
        }

        /// <summary>
        /// Opens a file, creating it when the mode says so, as <see cref="FileStream"/> does.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="mode">
        /// Whether the file must exist, must not, or is created when missing; whether it is
        /// cut to nothing; whether writes go only past its end.
        /// </param>
        /// <param name="access">Whether the stream reads, writes or both.</param>
        /// <param name="cancellationToken">Cancels the open.</param>
        /// <returns>A stream over the file's contents, at its start, or at its end to append.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The mode or access is not a defined value.</exception>
        /// <exception cref="ArgumentException">
        /// The mode writes and the access does not; the mode appends and the access reads;
        /// the path is the root or climbs above it; or a new file's name is not valid.
        /// </exception>
        /// <exception cref="NotSupportedException">The file would be written and the device is read-only.</exception>
        /// <exception cref="FatException">
        /// The file is missing, or exists when it must not; the path names a directory, or
        /// runs through a file; the file is read-only and would be written, or open in a
        /// conflicting way; the directory is full; or the file's chain is corrupt.
        /// </exception>
        public ValueTask<FatFileStream> OpenAsync(string path, FileMode mode, FileAccess access,
            CancellationToken cancellationToken = default)
        {
            return m_opener.OpenAsync(path, mode, access, cancellationToken);
        }

        #endregion

        #region Write

        /// <summary>
        /// Creates a directory, and any missing directories above it.
        /// </summary>
        /// <param name="path">The directory.</param>
        /// <param name="cancellationToken">Cancels the creation.</param>
        /// <returns>The directory, new or already there.</returns>
        /// <exception cref="ArgumentException">A name is not valid, or the path climbs above the root.</exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="FatException">
        /// The path names a file or runs through one, a directory on the way is full, or the
        /// volume is.
        /// </exception>
        public ValueTask<FatDirectoryEntry> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfReadOnly();
            return m_editor.CreateDirectoryAsync(path, cancellationToken);
        }

        /// <summary>
        /// Deletes a file or a directory.
        /// </summary>
        /// <param name="path">The file or directory.</param>
        /// <param name="recursive">
        /// Whether a directory that has entries is deleted with them, entry by entry: when
        /// one of them cannot be deleted, what was deleted before it stays deleted.
        /// </param>
        /// <param name="cancellationToken">Cancels the deletion; what was deleted by then stays deleted.</param>
        /// <returns>A task that completes when the entry is gone.</returns>
        /// <exception cref="ArgumentException">The path is the root, or climbs above it.</exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="FatException">
        /// Nothing is there; the entry, or one below it, is read-only or open; or the
        /// directory has entries and <paramref name="recursive"/> is not set.
        /// </exception>
        public ValueTask DeleteAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfReadOnly();
            return m_editor.DeleteAsync(path, recursive, cancellationToken);
        }

        /// <summary>
        /// Renames or moves a file or a directory. The entry keeps its data, attributes and
        /// times.
        /// </summary>
        /// <param name="source">What to move.</param>
        /// <param name="destination">Its new path; the directory it names must exist.</param>
        /// <param name="overwrite">Whether a file at the destination is replaced; a directory never is.</param>
        /// <param name="cancellationToken">Cancels the move.</param>
        /// <returns>The entry in its new place.</returns>
        /// <exception cref="ArgumentException">
        /// A path is the root or climbs above it, the new name is not valid, or a directory
        /// would move into itself.
        /// </exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="FatException">
        /// The source is missing or an open file; the destination's directory is missing or
        /// full; or something is at the destination and may not be replaced.
        /// </exception>
        public ValueTask<FatDirectoryEntry> MoveAsync(string source, string destination, bool overwrite = false,
            CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfReadOnly();
            return m_editor.MoveAsync(source, destination, overwrite, cancellationToken);
        }

        /// <summary>
        /// Writes what open files, the allocation table and the bitmap hold back, flushes the
        /// device, and on exFAT clears the dirty mark.
        /// </summary>
        /// <param name="cancellationToken">Cancels the flush.</param>
        /// <returns>A task that completes when everything is written.</returns>
        /// <exception cref="FatException">A file's chain turned out corrupt.</exception>
        public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfDisposed();
            if (m_core.IsReadOnly)
                return;

            foreach (var writer in m_core.OpenFiles.GetWriters())
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            await m_core.FlushAsync(cancellationToken).ConfigureAwait(false);
            await m_core.EndChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Counts the free clusters by reading the whole allocation table, or on exFAT the
        /// whole allocation bitmap — on a slow device, a long operation.
        /// </summary>
        /// <param name="cancellationToken">Cancels the count.</param>
        /// <returns>The number of free clusters; multiply by <see cref="FatVolumeInfo.ClusterSize"/> for bytes.</returns>
        /// <exception cref="FatException">The table or the bitmap cannot be read.</exception>
        public ValueTask<long> CountFreeClustersAsync(CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfDisposed();
            return m_core.CountFreeClustersAsync(cancellationToken);
        }

        /// <summary>
        /// Sets the attributes of a file or a directory. Its times stay as they are.
        /// </summary>
        /// <param name="path">The file or directory.</param>
        /// <param name="attributes">
        /// Any of <see cref="FatAttributes.ReadOnly"/>, <see cref="FatAttributes.Hidden"/>,
        /// <see cref="FatAttributes.System"/> and <see cref="FatAttributes.Archive"/>; the
        /// ones left out are cleared.
        /// </param>
        /// <param name="cancellationToken">Cancels the change.</param>
        /// <returns>The entry with its new attributes.</returns>
        /// <exception cref="ArgumentException">
        /// The path is the root or climbs above it, or <paramref name="attributes"/> holds
        /// <see cref="FatAttributes.Directory"/> or <see cref="FatAttributes.VolumeLabel"/>,
        /// which tell what an entry is.
        /// </exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="FatException">Nothing is there, or its directory is corrupt.</exception>
        public ValueTask<FatDirectoryEntry> SetAttributesAsync(string path, FatAttributes attributes, CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfReadOnly();
            return m_editor.SetAttributesAsync(path, attributes, cancellationToken);
        }

        /// <summary>
        /// Sets or removes the volume label. On FAT12/16/32 the boot sector's copy of the
        /// label changes too, as Windows and <c>fatlabel</c> keep it.
        /// </summary>
        /// <param name="label">
        /// The label, or <c>null</c> or empty to remove it. On FAT12/16/32 up to 11 ASCII
        /// characters a short name may hold, or spaces, stored in upper case without the
        /// spaces at its end, so that one of spaces alone removes the label too; on exFAT
        /// up to 11 UTF-16 units without control characters, stored as given.
        /// </param>
        /// <param name="cancellationToken">Cancels the change.</param>
        /// <returns>The label as stored, or <c>null</c> when the volume has none now.</returns>
        /// <exception cref="ArgumentException">The label is too long or holds a character the format does not allow.</exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="FatException">The root is full, or corrupt.</exception>
        public ValueTask<string?> SetLabelAsync(string? label, CancellationToken cancellationToken = default)
        {
            m_core.ThrowIfReadOnly();
            return m_editor.SetLabelAsync(label, cancellationToken);
        }

        #endregion

        #region IAsyncDisposable

        /// <summary>
        /// Flushes, closes the files open for writing, and releases whatever device the
        /// volume owns.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the flush fails, nothing is closed: the volume stays usable and disposal can
        /// be retried. After a successful flush the volume refuses work, files open for
        /// reading included.
        /// </para>
        /// <para>
        /// Every owned device is released even when one of them fails; disposal can then be
        /// retried to release the rest, and devices already released ignore the second call.
        /// </para>
        /// </remarks>
        /// <returns>A task that completes when everything is released.</returns>
        /// <exception cref="FatException">A file's chain turned out corrupt while flushing.</exception>
        /// <exception cref="AggregateException">More than one device failed to release.</exception>
        public async ValueTask DisposeAsync()
        {
            if (m_isReleased)
                return;

            if (!m_core.IsDisposed)
            {
                await FlushAsync(CancellationToken.None).ConfigureAwait(false);
                foreach (var writer in m_core.OpenFiles.GetWriters())
                    await writer.DisposeAsync().ConfigureAwait(false);
                m_core.IsDisposed = true;
            }

            List<Exception>? failures = null;
            foreach (var owned in m_owned)
            {
                try
                {
                    await owned.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception failure)
                {
                    (failures ??= new List<Exception>()).Add(failure);
                }
            }

            if (failures is { Count: 1 })
                ExceptionDispatchInfo.Throw(failures[0]);
            if (failures != null)
                throw new AggregateException("Releasing the volume's devices failed.", failures);

            m_isReleased = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The layout of the volume.
        /// </summary>
        public FatVolumeInfo Info => m_core.Info;

        /// <summary>
        /// The device the volume is on.
        /// </summary>
        public IBlockDevice Device => m_core.Device;

        /// <summary>
        /// Whether the volume can only be read, because its device can.
        /// </summary>
        public bool IsReadOnly => m_core.IsReadOnly;

        /// <summary>
        /// The root directory.
        /// </summary>
        public FatDirectoryEntry Root { get; }

        /// <summary>
        /// The parts of the volume below this front.
        /// </summary>
        internal FatVolumeCore Core => m_core;

        /// <summary>
        /// The paths of the volume.
        /// </summary>
        internal FatNamespace Names => m_names;

        #endregion
    }
}
