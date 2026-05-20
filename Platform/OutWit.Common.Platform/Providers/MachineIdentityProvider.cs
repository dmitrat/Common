using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Platform.Interfaces;
using OutWit.Common.Platform.Internal;

namespace OutWit.Common.Platform.Providers
{
    /// <summary>
    /// Derives a stable hashed machine identity. The raw OS-level identity is
    /// fetched via a per-OS <c>IPlatformProbe</c>; if the OS does not expose
    /// one, a per-app fallback file is created under the user data directory
    /// supplied by an <see cref="IStandardDirectoryProvider"/>.
    /// </summary>
    public sealed class MachineIdentityProvider : IMachineIdentityProvider
    {
        #region Constants

        private const string FALLBACK_FILE = "machine-id";

        #endregion

        #region Fields

        private readonly IStandardDirectoryProvider m_directoryProvider;
        private readonly IPlatformProbe m_probe;

        #endregion

        #region Constructors

        public MachineIdentityProvider(IStandardDirectoryProvider directoryProvider)
            : this(directoryProvider, PlatformProbeFactory.ForCurrentPlatform())
        {
        }

        internal MachineIdentityProvider(IStandardDirectoryProvider directoryProvider, IPlatformProbe probe)
        {
            m_directoryProvider = directoryProvider;
            m_probe = probe;
        }

        #endregion

        #region IMachineIdentityProvider

        public Task<string> GetMachineIdentityAsync()
        {
            return Task.Run(() =>
            {
                var raw = m_probe.GetRawMachineIdentity();
                if (string.IsNullOrWhiteSpace(raw))
                    raw = GetOrCreateFallbackIdentity();

                return HashWithSha256(raw!);
            });
        }

        #endregion

        #region Tools

        private string GetOrCreateFallbackIdentity()
        {
            try
            {
                var directory = m_directoryProvider.GetUserDataDirectory();
                Directory.CreateDirectory(directory);

                var filePath = Path.Combine(directory, FALLBACK_FILE);
                if (File.Exists(filePath))
                {
                    var existing = File.ReadAllText(filePath).Trim();
                    if (!string.IsNullOrWhiteSpace(existing))
                        return existing;
                }

                var newIdentity = Guid.NewGuid().ToString("N");
                File.WriteAllText(filePath, newIdentity);
                return newIdentity;
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        private static string HashWithSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        #endregion
    }
}
