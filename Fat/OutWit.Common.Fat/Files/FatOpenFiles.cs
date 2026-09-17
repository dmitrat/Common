using System;
using System.Collections.Generic;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Files
{
    /// <summary>
    /// The files a volume has open, so that a file being written is not read, opened
    /// again, moved or deleted at the same time.
    /// </summary>
    /// <remarks>
    /// Many readers or one writer, like <c>FileShare.Read</c> for readers and
    /// <c>FileShare.None</c> for a writer.
    /// </remarks>
    internal sealed class FatOpenFiles
    {
        #region Fields

        private readonly Dictionary<(uint Directory, long Slot), int> m_readers = new();

        private readonly Dictionary<(uint Directory, long Slot), FatFileStream> m_writers = new();

        #endregion

        #region Functions

        /// <summary>
        /// Registers an open file.
        /// </summary>
        /// <exception cref="FatException"><see cref="FatErrorKind.InUse"/>: the file is open in a conflicting way.</exception>
        public void Open(DirectoryItem item, bool isWriter)
        {
            var key = item.Key;
            if (m_writers.ContainsKey(key) || (isWriter && m_readers.ContainsKey(key)))
                throw new FatException(FatErrorKind.InUse, $"{item.Entry.Path} is already open.");

            if (!isWriter)
                m_readers[key] = m_readers.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        /// <summary>
        /// Registers the stream of a writer opened with <see cref="Open"/>.
        /// </summary>
        public void Attach(DirectoryItem item, FatFileStream stream)
        {
            m_writers[item.Key] = stream;
        }

        /// <summary>
        /// Removes an open file.
        /// </summary>
        public void Close(DirectoryItem item, bool isWriter)
        {
            var key = item.Key;
            if (isWriter)
            {
                m_writers.Remove(key);
                return;
            }

            if (m_readers.TryGetValue(key, out int count))
            {
                if (count > 1)
                    m_readers[key] = count - 1;
                else
                    m_readers.Remove(key);
            }
        }

        /// <summary>
        /// Throws when a file is open at all.
        /// </summary>
        /// <exception cref="FatException"><see cref="FatErrorKind.InUse"/>: the file is open.</exception>
        public void ThrowIfOpen(DirectoryItem item)
        {
            if (m_readers.ContainsKey(item.Key) || m_writers.ContainsKey(item.Key))
                throw new FatException(FatErrorKind.InUse, $"{item.Entry.Path} is open.");
        }

        /// <summary>
        /// A snapshot of the open writers.
        /// </summary>
        public IReadOnlyCollection<FatFileStream> GetWriters()
        {
            return new List<FatFileStream>(m_writers.Values);
        }

        #endregion
    }
}
