using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Files
{
    /// <summary>
    /// Opens files the way <see cref="FileStream"/> does for each
    /// <see cref="FileMode"/> and <see cref="FileAccess"/>.
    /// </summary>
    internal sealed class FatFileOpener
    {
        #region Fields

        private readonly FatVolumeCore m_core;

        private readonly FatNamespace m_names;

        private readonly FatNamespaceEditor m_editor;

        #endregion

        #region Constructors

        public FatFileOpener(FatVolumeCore core, FatNamespace names, FatNamespaceEditor editor)
        {
            m_core = core;
            m_names = names;
            m_editor = editor;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Opens or creates a file.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The mode or access is not a defined value.</exception>
        /// <exception cref="ArgumentException">The mode and access do not go together, or the path is not valid.</exception>
        /// <exception cref="NotSupportedException">The file would be written and the device is read-only.</exception>
        /// <exception cref="FatException">As for <see cref="FatVolume.OpenAsync"/>.</exception>
        public async ValueTask<FatFileStream> OpenAsync(string path, FileMode mode, FileAccess access, CancellationToken cancellationToken)
        {
            Validate(mode, access);
            m_core.ThrowIfDisposed();

            bool isWriter = (access & FileAccess.Write) != 0;
            if (isWriter)
                m_core.ThrowIfReadOnly();

            var (parent, name) = await m_names.RequireParentAsync(path, cancellationToken).ConfigureAwait(false);
            var directory = m_names.Open(parent);
            var item = await directory.FindAsync(name, cancellationToken).ConfigureAwait(false);

            if (item != null)
                return await OpenExistingAsync(directory, item, mode, access, cancellationToken).ConfigureAwait(false);

            if (mode is FileMode.Open or FileMode.Truncate)
                throw new FatException(FatErrorKind.NotFound, $"The file {FatPath.Combine(parent.Entry.Path, name)} does not exist.");

            m_core.ThrowIfReadOnly();
            item = await m_editor.CreateFileAsync(directory, name, cancellationToken).ConfigureAwait(false);
            if (!isWriter)
                await m_core.FlushAsync(cancellationToken).ConfigureAwait(false);

            var stream = new FatFileStream(m_core, directory, item, access, mode == FileMode.Append);
            if (isWriter)
                stream.MarkModified();
            return stream;
        }

        private async ValueTask<FatFileStream> OpenExistingAsync(FatDirectory directory, DirectoryItem item, FileMode mode, FileAccess access,
            CancellationToken cancellationToken)
        {
            var entry = item.Entry;
            if (entry.IsDirectory)
                throw new FatException(FatErrorKind.NotAFile, $"{entry.Path} is a directory, not a file.");
            if (mode == FileMode.CreateNew)
                throw new FatException(FatErrorKind.AlreadyExists, $"{entry.Path} already exists.");
            if ((access & FileAccess.Write) != 0 && (entry.Attributes & FatAttributes.ReadOnly) != 0)
                throw new FatException(FatErrorKind.AccessDenied, $"{entry.Path} is read-only.");

            var stream = new FatFileStream(m_core, directory, item, access, mode == FileMode.Append);
            if (mode is not (FileMode.Create or FileMode.Truncate))
                return stream;

            try
            {
                await stream.SetLengthAsync(0, cancellationToken).ConfigureAwait(false);
                return stream;
            }
            catch
            {
                await CloseQuietlyAsync(stream).ConfigureAwait(false);
                throw;
            }
        }

        /// <remarks>
        /// The failure that made the stream useless is the one reported.
        /// </remarks>
        private static async ValueTask CloseQuietlyAsync(FatFileStream stream)
        {
            try
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Reported by the caller instead.
            }
        }

        private static void Validate(FileMode mode, FileAccess access)
        {
            if (mode is < FileMode.CreateNew or > FileMode.Append)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "The mode is not a defined value.");
            if (access is < FileAccess.Read or > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException(nameof(access), access, "The access is not a defined value.");

            if (access == FileAccess.Read && mode is FileMode.CreateNew or FileMode.Create or FileMode.Truncate or FileMode.Append)
                throw new ArgumentException($"The mode {mode} writes and cannot go with read-only access.", nameof(access));
            if (mode == FileMode.Append && access != FileAccess.Write)
                throw new ArgumentException("A file opened to append can only be written.", nameof(access));
        }

        #endregion
    }
}
