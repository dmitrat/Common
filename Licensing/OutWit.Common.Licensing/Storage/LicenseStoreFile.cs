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
    /// </summary>
    public sealed class LicenseStoreFile : ILicenseStore
    {
        #region Constants

        private const string LICENSE_EXTENSION = ".lic";
        private const string STATE_FILE = "licensing.state.json";

        #endregion

        #region Fields

        private static readonly JsonSerializerOptions JSON_OPTIONS = new() { WriteIndented = true };

        private readonly string m_directory;

        #endregion

        #region Constructors

        public LicenseStoreFile(string directory)
        {
            m_directory = directory;
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

            Directory.CreateDirectory(m_directory);

            // Named by licence id, so re-installing the same document overwrites
            // itself instead of accumulating copies that all validate.
            var fileName = SanitizeFileName(parsed.Payload.Id) + LICENSE_EXTENSION;

            File.WriteAllText(Path.Combine(m_directory, fileName), parsed.Raw);
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
                Directory.CreateDirectory(m_directory);

                var path = StatePath();
                var temporary = path + ".tmp";

                // Written through a temporary file: a process killed mid-write
                // would otherwise leave a truncated sidecar, and the clock guard
                // would silently lose its high-water mark.
                File.WriteAllText(temporary, JsonSerializer.Serialize(state, JSON_OPTIONS));

                if (File.Exists(path))
                    File.Delete(path);

                File.Move(temporary, path);

                ProtectFile(path);
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

        private static void ProtectFile(string path)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                // Permission hardening is a nicety, not a precondition.
            }
        }

        #endregion
    }
}
