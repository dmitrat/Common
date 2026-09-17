using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Files;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Utils
{
    /// <summary>
    /// Shortcuts over <see cref="FatVolume"/>.
    /// </summary>
    public static class FatVolumeExtensions
    {
        #region Functions

        /// <summary>
        /// Tells whether anything exists at a path.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">Cancels the lookup.</param>
        /// <returns><c>true</c> when a file or directory is there.</returns>
        /// <exception cref="ArgumentException">The path climbs above the root.</exception>
        /// <exception cref="FatException">A directory on the way is corrupt.</exception>
        public static async ValueTask<bool> ExistsAsync(this FatVolume volume, string path, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(volume);
            return await volume.GetEntryAsync(path, cancellationToken).ConfigureAwait(false) != null;
        }

        /// <summary>
        /// Opens an existing file for reading.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="path">The file.</param>
        /// <param name="cancellationToken">Cancels the lookup.</param>
        /// <returns>A stream over the file's contents.</returns>
        /// <exception cref="ArgumentException">The path is the root or climbs above it.</exception>
        /// <exception cref="FatException">
        /// The path does not name a file, the file is open for writing, or its chain is corrupt.
        /// </exception>
        public static ValueTask<FatFileStream> OpenReadAsync(this FatVolume volume, string path, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(volume);
            return volume.OpenAsync(path, FileMode.Open, FileAccess.Read, cancellationToken);
        }

        /// <summary>
        /// Creates a file, or empties the one that is there, and opens it for reading and writing.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="path">The file.</param>
        /// <param name="cancellationToken">Cancels the creation.</param>
        /// <returns>A stream over the empty file.</returns>
        /// <exception cref="ArgumentException">The path is the root or climbs above it, or the name is not valid.</exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="FatException">As for <see cref="FatVolume.OpenAsync"/> with <see cref="FileMode.Create"/>.</exception>
        public static ValueTask<FatFileStream> CreateAsync(this FatVolume volume, string path, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(volume);
            return volume.OpenAsync(path, FileMode.Create, FileAccess.ReadWrite, cancellationToken);
        }

        /// <summary>
        /// Reads a whole file.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="path">The file.</param>
        /// <param name="cancellationToken">Cancels the read.</param>
        /// <returns>The file's contents.</returns>
        /// <exception cref="ArgumentException">The path is the root or climbs above it.</exception>
        /// <exception cref="IOException">The file is larger than an array can hold.</exception>
        /// <exception cref="FatException">
        /// As for <see cref="OpenReadAsync"/>; also when the chain is shorter than the file,
        /// which is checked before any memory is taken for the contents.
        /// </exception>
        public static async ValueTask<byte[]> ReadAllBytesAsync(this FatVolume volume, string path, CancellationToken cancellationToken = default)
        {
            await using var stream = await volume.OpenReadAsync(path, cancellationToken).ConfigureAwait(false);
            if (stream.Length > Array.MaxLength)
                throw new IOException($"{stream.Entry.Path} is {stream.Length} bytes, more than an array can hold; read it as a stream.");

            await stream.CheckChainAsync(cancellationToken).ConfigureAwait(false);
            var contents = new byte[stream.Length];
            await stream.ReadExactlyAsync(contents, cancellationToken).ConfigureAwait(false);
            return contents;
        }

        /// <summary>
        /// Creates a file, or replaces the contents of the one that is there, and writes it whole.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="path">The file.</param>
        /// <param name="contents">What the file is to hold.</param>
        /// <param name="cancellationToken">Cancels the write; the file may be left partly written.</param>
        /// <returns>The file as written.</returns>
        /// <exception cref="ArgumentException">The path is the root or climbs above it, or the name is not valid.</exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="FatException">
        /// As for <see cref="FatVolume.OpenAsync"/> with <see cref="FileMode.Create"/>; also
        /// when the volume is full or the contents pass 4 GiB.
        /// </exception>
        public static async ValueTask<FatDirectoryEntry> WriteAllBytesAsync(this FatVolume volume, string path, ReadOnlyMemory<byte> contents,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(volume);
            await using var stream = await volume.OpenAsync(path, FileMode.Create, FileAccess.Write, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(contents, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return stream.Entry;
        }

        #endregion
    }
}
