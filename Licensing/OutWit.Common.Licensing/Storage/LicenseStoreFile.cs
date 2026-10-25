using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OutWit.Common.Licensing.Crypto;

namespace OutWit.Common.Licensing.Storage
{
    /// <summary>
    /// Keeps licences as <c>*.lic</c> files in a directory, with the runtime
    /// state in a sidecar JSON beside them.
    /// <para>
    /// A licence is <b>not</b> a secret — it is signed, not encrypted, and an
    /// operator is entitled to read what they were granted — so nothing here is
    /// obfuscated. Only the sidecar is written with restrictive permissions,
    /// because that is the file whose contents a determined user would want to
    /// edit.
    /// </para>
    /// <para>
    /// A store <b>shared by all users</b> of a machine (a licence bound to the
    /// machine serves everyone who works on it) cannot keep its sidecar to its
    /// owner: on Unix the next user could not read the demo anchor or the clock
    /// guard, start a fresh state of their own and replace the first user's,
    /// and switching users would restart the demo. Shared, every file is
    /// written readable and writable by all (<c>0666</c>), the directory it
    /// creates is <c>0777</c>, and a file is replaced by renaming a fresh one
    /// over it, which the directory's permission allows whoever owns the old
    /// one. On Windows the directory's ACL decides, set by the product.
    /// </para>
    /// </summary>
    public sealed class LicenseStoreFile : ILicenseStore
    {
        #region Constants

        private const string LICENSE_EXTENSION = ".lic";
        private const string STATE_FILE = "licensing.state.json";
        private const string TEMPORARY_EXTENSION = ".tmp";

        private const UnixFileMode OWNER_ONLY = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        private const UnixFileMode EVERYONE_FILE = OWNER_ONLY
                                                   | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                                                   | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;

        private const UnixFileMode EVERYONE_DIRECTORY = EVERYONE_FILE
                                                        | UnixFileMode.UserExecute
                                                        | UnixFileMode.GroupExecute
                                                        | UnixFileMode.OtherExecute;

        #endregion

        #region Fields

        private static readonly JsonSerializerOptions JSON_OPTIONS = new() { WriteIndented = true };

        private readonly string m_directory;

        private readonly bool m_isShared;

        #endregion

        #region Constructors

        /// <summary>
        /// A store of one user (the sidecar is the owner's alone on Unix).
        /// </summary>
        /// <param name="directory">The directory that holds the files.</param>
        public LicenseStoreFile(string directory)
            : this(directory, sharedByAllUsers: false)
        {
        }

        /// <summary>
        /// A store of one user, or of every user of the machine.
        /// </summary>
        /// <param name="directory">The directory that holds the files.</param>
        /// <param name="sharedByAllUsers">True for a machine-scope store every
        /// user of the machine reads and writes.</param>
        public LicenseStoreFile(string directory, bool sharedByAllUsers)
        {
            m_directory = directory;
            m_isShared = sharedByAllUsers;
        }

        #endregion

        #region ILicenseStore

        public IReadOnlyList<string> ReadTokens()
        {
            try
            {
                if (!Directory.Exists(m_directory))
                    return Array.Empty<string>();

                return Directory
                    .EnumerateFiles(m_directory, "*" + LICENSE_EXTENSION)
                    .Select(ReadTokenFile)
                    .Where(token => !string.IsNullOrWhiteSpace(token))
                    .Select(token => token!)
                    .ToList();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        public void Save(string token)
        {
            var parsed = LicenseToken.Parse(token);
            if (parsed == null)
                throw new ArgumentException("The value is not a licence token.", nameof(token));

            EnsureDirectory();

            // Named by licence id, so re-installing the same document overwrites
            // itself instead of accumulating copies that all validate.
            var fileName = SanitizeFileName(parsed.Payload.Id) + LICENSE_EXTENSION;

            Replace(Path.Combine(m_directory, fileName), parsed.Raw, m_isShared ? EVERYONE_FILE : null);
        }

        public bool Remove(string licenseId)
        {
            var path = Path.Combine(m_directory, SanitizeFileName(licenseId) + LICENSE_EXTENSION);

            if (!File.Exists(path))
                return false;

            try
            {
                File.Delete(path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        public LicenseStoreState ReadState()
        {
            try
            {
                var path = StatePath();

                if (!File.Exists(path))
                    return new LicenseStoreState();

                return JsonSerializer.Deserialize<LicenseStoreState>(File.ReadAllText(path), JSON_OPTIONS)
                       ?? new LicenseStoreState();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                // A corrupt or unreadable sidecar must not stop the product. It
                // degrades to "nothing observed yet", which is the same position
                // a fresh installation is in.
                return new LicenseStoreState();
            }
        }

        public void WriteState(LicenseStoreState state)
        {
            try
            {
                EnsureDirectory();

                // Written through a temporary file: a process killed mid-write
                // would otherwise leave a truncated sidecar, and the clock guard
                // would silently lose its high-water mark.
                Replace(StatePath(), JsonSerializer.Serialize(state, JSON_OPTIONS), m_isShared ? EVERYONE_FILE : OWNER_ONLY);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best effort — a read-only profile must not crash the product.
            }
        }

        #endregion

        #region Tools

        private string StatePath()
        {
            return Path.Combine(m_directory, STATE_FILE);
        }

        private void EnsureDirectory()
        {
            if (Directory.Exists(m_directory))
                return;

            Directory.CreateDirectory(m_directory);

            // mkdir applies the umask, so the mode is set after the fact.
            if (m_isShared)
                SetMode(m_directory, EVERYONE_DIRECTORY);
        }

        // A fresh file renamed over the old one: atomic, and allowed to anyone
        // who may write the directory - in a shared store the old file may
        // belong to another user. The temporary name is unique, so two users
        // writing at once never collide on it.
        private static void Replace(string path, string content, UnixFileMode? mode)
        {
            var temporary = $"{path}.{Guid.NewGuid():N}{TEMPORARY_EXTENSION}";
            try
            {
                File.WriteAllText(temporary, content);
                if (mode != null)
                    SetMode(temporary, mode.Value);

                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private static string? ReadTokenFile(string path)
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static string SanitizeFileName(string value)
        {
            var safe = new string((value ?? string.Empty)
                .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character)
                .ToArray());

            return string.IsNullOrWhiteSpace(safe) ? "license" : safe;
        }

        private static void SetMode(string path, UnixFileMode mode)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(path, mode);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                // Permission handling is a nicety, not a precondition.
            }
        }

        #endregion
    }
}
