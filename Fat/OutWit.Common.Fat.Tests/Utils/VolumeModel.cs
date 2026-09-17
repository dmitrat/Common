using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// What a volume should hold: files with their contents and directories, by path,
    /// matched without regard to case as FAT matches names.
    /// </summary>
    internal sealed class VolumeModel
    {
        #region Fields

        private readonly Dictionary<string, string> m_directories = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, (string Path, byte[] Data)> m_files = new(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Functions

        /// <summary>
        /// Takes what a volume holds as the starting point.
        /// </summary>
        public static async Task<VolumeModel> ReadAsync(FatVolume volume)
        {
            var model = new VolumeModel();
            await foreach (var entry in volume.EnumerateAsync(recursive: true))
            {
                if (entry.IsDirectory)
                    model.m_directories[entry.Path] = entry.Path;
                else
                    model.m_files[entry.Path] = (entry.Path, await volume.ReadAllBytesAsync(entry.Path));
            }

            return model;
        }

        public bool IsFile(string path)
        {
            return m_files.ContainsKey(path);
        }

        public bool IsDirectory(string path)
        {
            return path == "/" || m_directories.ContainsKey(path);
        }

        public bool Exists(string path)
        {
            return IsFile(path) || IsDirectory(path);
        }

        public byte[] Read(string path)
        {
            return m_files[path].Data;
        }

        /// <summary>
        /// Sets a file's contents; an existing file keeps the case of its name.
        /// </summary>
        public void Write(string path, byte[] data)
        {
            string display = m_files.TryGetValue(path, out var existing) ? existing.Path : Canonical(path);
            m_files[display] = (display, data);
        }

        public void CreateDirectory(string path)
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string current = string.Empty;
            foreach (string part in parts)
            {
                current += "/" + part;
                if (m_directories.TryGetValue(current, out var existing))
                    current = existing;
                else
                    m_directories[current] = current;
            }
        }

        /// <summary>
        /// Removes a file, or a directory with everything below it.
        /// </summary>
        public void Delete(string path)
        {
            if (m_files.Remove(path))
                return;

            m_directories.Remove(path);
            foreach (string below in Below(path).ToList())
            {
                m_files.Remove(below);
                m_directories.Remove(below);
            }
        }

        /// <summary>
        /// Moves a file or a directory with everything below it, replacing a file at the destination.
        /// </summary>
        public void Move(string source, string destination)
        {
            string display = Canonical(destination);

            if (m_files.Remove(source, out var file))
            {
                m_files.Remove(destination);
                m_files[display] = (display, file.Data);
                return;
            }

            var moved = Below(source).ToList();
            string from = m_directories[source];
            m_directories.Remove(source);
            m_directories[display] = display;
            foreach (string below in moved)
            {
                string renamed = display + below[from.Length..];
                if (m_files.Remove(below, out var child))
                    m_files[renamed] = (renamed, child.Data);
                else if (m_directories.Remove(below))
                    m_directories[renamed] = renamed;
            }
        }

        public IReadOnlyList<string> Files()
        {
            return m_files.Values.Select(file => file.Path).OrderBy(path => path, StringComparer.Ordinal).ToList();
        }

        public IReadOnlyList<string> Directories()
        {
            return m_directories.Values.OrderBy(path => path, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Fails unless the volume lists exactly the model's entries, with the model's contents.
        /// </summary>
        public async Task AssertMatchesAsync(FatVolume volume)
        {
            var files = new List<string>();
            var directories = new List<string>();
            await foreach (var entry in volume.EnumerateAsync(recursive: true))
                (entry.IsDirectory ? directories : files).Add(entry.Path);

            Assert.That(directories.Order(StringComparer.Ordinal), Is.EqualTo(Directories()), "directories");
            Assert.That(files.Order(StringComparer.Ordinal), Is.EqualTo(Files()), "files");

            foreach (string path in files)
            {
                var contents = await volume.ReadAllBytesAsync(path);
                Assert.That(contents.Length, Is.EqualTo(Read(path).Length), path);
                Assert.That(contents.AsSpan().SequenceEqual(Read(path)), Is.True, path);
            }
        }

        public static string Parent(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash <= 0 ? "/" : path[..slash];
        }

        public static string Name(string path)
        {
            return path[(path.LastIndexOf('/') + 1)..];
        }

        /// <summary>
        /// The path with its directory spelled as the model has it.
        /// </summary>
        private string Canonical(string path)
        {
            string parent = Parent(path);
            return (parent == "/" ? string.Empty : m_directories[parent]) + "/" + Name(path);
        }

        private IEnumerable<string> Below(string directory)
        {
            string prefix = directory + "/";
            return m_files.Keys.Concat(m_directories.Keys)
                .Where(path => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
