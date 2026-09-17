namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Applies random operations to a volume and to a <see cref="VolumeModel"/> alike. A
    /// full volume is expected; any other failure is not.
    /// </summary>
    internal sealed class RandomOperations
    {
        #region Constants

        private static readonly string[] NAMES =
        {
            "A.TXT", "b.txt", "DATA.BIN", "Long name.dat", "LONG NAME.DAT", "log file 1.txt", "log file 2.txt",
            "файл.bin", "Ünïcödé.txt", "x.tar.gz", "entry-000.dat", "entry-001.dat", "UPPER", "lower", "Mixed.Case"
        };

        #endregion

        #region Fields

        private readonly FatVolume m_volume;

        private readonly VolumeModel m_model;

        private readonly System.Random m_random;

        private readonly int m_maxBytes;

        #endregion

        #region Constructors

        public RandomOperations(FatVolume volume, VolumeModel model, int seed, int maxBytes)
        {
            m_volume = volume;
            m_model = model;
            m_random = new System.Random(seed);
            m_maxBytes = maxBytes;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Runs operations, checking the volume against the model every <paramref name="checkEvery"/>.
        /// </summary>
        public async Task RunAsync(int count, int checkEvery)
        {
            for (int step = 1; step <= count; step++)
            {
                int choice = m_random.Next(100);
                string operation = choice switch
                {
                    < 25 => await WriteAsync(),
                    < 35 => await AppendAsync(),
                    < 45 => await ResizeAsync(),
                    < 55 => await CreateDirectoryAsync(),
                    < 70 => await DeleteAsync(),
                    < 88 => await MoveAsync(),
                    _ => await InterleaveAsync()
                };
                Log.Add($"{step}: {operation}");

                if (step % checkEvery == 0)
                    await m_model.AssertMatchesAsync(m_volume);
            }
        }

        private async Task<string> WriteAsync()
        {
            string path = NewPath();
            if (m_model.IsDirectory(path))
                return $"skip write {path}";

            var data = Data();
            try
            {
                await m_volume.WriteAllBytesAsync(path, data);
                m_model.Write(path, data);
            }
            catch (FatException error) when (error.Kind == FatErrorKind.NoSpace)
            {
                NoSpace++;
                if (await m_volume.ExistsAsync(path))
                    m_model.Write(path, Array.Empty<byte>());
            }

            return $"write {path} {data.Length}";
        }

        private async Task<string> AppendAsync()
        {
            if (PickFile() is not { } path)
                return "skip append";

            var data = Data();
            try
            {
                await using var stream = await m_volume.OpenAsync(path, FileMode.Append, FileAccess.Write);
                await stream.WriteAsync(data);
                m_model.Write(path, m_model.Read(path).Concat(data).ToArray());
            }
            catch (FatException error) when (error.Kind == FatErrorKind.NoSpace)
            {
                NoSpace++;
            }

            return $"append {path} {data.Length}";
        }

        private async Task<string> ResizeAsync()
        {
            if (PickFile() is not { } path)
                return "skip resize";

            int length = m_random.Next(m_maxBytes);
            try
            {
                await using var stream = await m_volume.OpenAsync(path, FileMode.Open, FileAccess.ReadWrite);
                await stream.SetLengthAsync(length);
                var resized = new byte[length];
                var old = m_model.Read(path);
                old.AsSpan(0, Math.Min(length, old.Length)).CopyTo(resized);
                m_model.Write(path, resized);
            }
            catch (FatException error) when (error.Kind == FatErrorKind.NoSpace)
            {
                NoSpace++;
            }

            return $"resize {path} {length}";
        }

        private async Task<string> CreateDirectoryAsync()
        {
            string path = NewPath();
            if (m_random.Next(3) == 0)
                path += "/" + NAMES[m_random.Next(NAMES.Length)];
            if (m_model.IsFile(path) || PassesThroughFile(path))
                return $"skip mkdir {path}";

            try
            {
                await m_volume.CreateDirectoryAsync(path);
                m_model.CreateDirectory(path);
            }
            catch (FatException error) when (error.Kind == FatErrorKind.NoSpace)
            {
                NoSpace++;
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i <= parts.Length; i++)
                {
                    string prefix = "/" + string.Join('/', parts.Take(i));
                    if (!m_model.IsDirectory(prefix) && await m_volume.ExistsAsync(prefix))
                        m_model.CreateDirectory(prefix);
                }
            }

            return $"mkdir {path}";
        }

        private async Task<string> DeleteAsync()
        {
            var candidates = m_model.Files().Concat(m_model.Directories()).ToList();
            if (candidates.Count == 0)
                return "skip delete";

            string path = candidates[m_random.Next(candidates.Count)];
            await m_volume.DeleteAsync(path, recursive: true);
            m_model.Delete(path);
            return $"delete {path}";
        }

        private async Task<string> MoveAsync()
        {
            var candidates = m_model.Files().Concat(m_model.Directories()).ToList();
            if (candidates.Count == 0)
                return "skip move";

            string source = candidates[m_random.Next(candidates.Count)];
            string destination = m_random.Next(4) == 0
                ? VolumeModel.Parent(source).TrimEnd('/') + "/" + VolumeModel.Name(source).ToUpperInvariant()
                : NewPath();
            bool sourceIsDirectory = m_model.IsDirectory(source);
            bool same = string.Equals(source, destination, StringComparison.OrdinalIgnoreCase);

            if (sourceIsDirectory && (destination + "/").StartsWith(source + "/", StringComparison.OrdinalIgnoreCase) && !same)
                return $"skip move {source} into itself";
            if (!same && (m_model.IsDirectory(destination) || (m_model.IsFile(destination) && sourceIsDirectory)))
                return $"skip move {source} over {destination}";

            try
            {
                await m_volume.MoveAsync(source, destination, overwrite: true);
                m_model.Move(source, destination);
            }
            catch (FatException error) when (error.Kind == FatErrorKind.NoSpace)
            {
                NoSpace++;
            }

            return $"move {source} {destination}";
        }

        private async Task<string> InterleaveAsync()
        {
            string directory = PickDirectory();
            string first = $"{directory.TrimEnd('/')}/woven {m_random.Next(1000)} a.bin";
            string second = $"{directory.TrimEnd('/')}/woven {m_random.Next(1000)} b.bin";
            if (m_model.Exists(first) || m_model.Exists(second) || string.Equals(first, second, StringComparison.OrdinalIgnoreCase))
                return "skip interleave";

            var written = new[] { new List<byte>(), new List<byte>() };
            var streams = new List<OutWit.Common.Fat.Files.FatFileStream>();
            try
            {
                foreach (string path in new[] { first, second })
                {
                    streams.Add(await m_volume.OpenAsync(path, FileMode.CreateNew, FileAccess.Write));
                    m_model.Write(path, Array.Empty<byte>());
                }

                int chunk = Math.Max(1, m_maxBytes / 8);
                for (int round = 0; round < 6; round++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var data = Data(chunk);
                        await streams[i].WriteAsync(data);
                        written[i].AddRange(data);
                    }
                }
            }
            catch (FatException error) when (error.Kind == FatErrorKind.NoSpace)
            {
                NoSpace++;
            }
            finally
            {
                foreach (var stream in streams)
                    await stream.DisposeAsync();
            }

            if (streams.Count > 0)
                m_model.Write(first, written[0].ToArray());
            if (streams.Count > 1)
                m_model.Write(second, written[1].ToArray());
            return $"interleave {first} {second}";
        }

        private string NewPath()
        {
            return PickDirectory().TrimEnd('/') + "/" + NAMES[m_random.Next(NAMES.Length)];
        }

        private string PickDirectory()
        {
            var directories = m_model.Directories();
            int index = m_random.Next(directories.Count + 2);
            return index < directories.Count ? directories[index] : "/";
        }

        private string? PickFile()
        {
            var files = m_model.Files();
            return files.Count == 0 ? null : files[m_random.Next(files.Count)];
        }

        private bool PassesThroughFile(string path)
        {
            for (string parent = VolumeModel.Parent(path); parent != "/"; parent = VolumeModel.Parent(parent))
            {
                if (m_model.IsFile(parent))
                    return true;
            }

            return false;
        }

        private byte[] Data(int? size = null)
        {
            var data = new byte[size ?? m_random.Next(m_maxBytes)];
            m_random.NextBytes(data);
            return data;
        }

        #endregion

        #region Properties

        public int NoSpace { get; private set; }

        public List<string> Log { get; } = new();

        #endregion
    }
}
