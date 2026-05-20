using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OutWit.Common.Platform;
using OutWit.Common.Platform.Interfaces;
using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.SystemInfo;
using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Platform.Tests.Providers
{
    /// <summary>
    /// MachineIdentityProvider must (a) hash whatever raw identity the probe
    /// returns, and (b) fall back to a persisted file under the user data
    /// directory when the probe returns null/empty.
    /// </summary>
    [TestFixture]
    public sealed class MachineIdentityProviderProbeInjectionTests
    {
        private string m_tempDir = null!;
        private StandardDirectoryProvider m_directoryProvider = null!;

        [SetUp]
        public void SetUp()
        {
            m_tempDir = Path.Combine(Path.GetTempPath(), $"outwit-platform-id-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(m_tempDir);
            // StandardDirectoryProvider needs at least one segment.
            m_directoryProvider = new StandardDirectoryProvider("OutWit", "Platform", "Tests");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(m_tempDir))
                    Directory.Delete(m_tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        [Test]
        public async Task ProbeRawIdentityIsHashedTest()
        {
            var probe = new FakeProbe { RawIdentity = "raw-stable-identity" };
            var provider = new MachineIdentityProvider(m_directoryProvider, probe);

            var hashed = await provider.GetMachineIdentityAsync();

            Assert.Multiple(() =>
            {
                Assert.That(hashed, Is.Not.Null);
                Assert.That(hashed, Has.Length.EqualTo(64), "SHA-256 hex digest must be 64 hex chars.");
                Assert.That(hashed, Does.Match("^[0-9a-f]+$"));
                Assert.That(hashed, Is.Not.EqualTo("raw-stable-identity"));
            });
        }

        [Test]
        public async Task SameRawIdentityProducesSameHashAcrossCallsTest()
        {
            var probe = new FakeProbe { RawIdentity = "stable" };
            var provider = new MachineIdentityProvider(m_directoryProvider, probe);

            var first = await provider.GetMachineIdentityAsync();
            var second = await provider.GetMachineIdentityAsync();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public async Task EmptyProbeRawIdentityFallsBackToPersistedFileTest()
        {
            // The fake provider returns null → MachineIdentityProvider creates
            // a machine-id file under the user data dir and uses it as the
            // raw identity. Two calls must reuse the same file (same hash).
            var probe = new FakeProbe { RawIdentity = null };
            var provider = new MachineIdentityProvider(m_directoryProvider, probe);

            var first = await provider.GetMachineIdentityAsync();
            var second = await provider.GetMachineIdentityAsync();

            Assert.That(first, Is.EqualTo(second));
        }

        #region Fakes

        private sealed class FakeProbe : IPlatformProbe
        {
            public PlatformKind Kind => PlatformKind.Unknown;
            public string? RawIdentity { get; set; }

            public string GetCpuModelName() => string.Empty;
            public IReadOnlyList<SystemGpuInfo> GetGpus() => Array.Empty<SystemGpuInfo>();
            public SystemStorageType GetStorageType(string rootPath) => SystemStorageType.Unknown;
            public double GetCpuLoadPercent() => 0.0;
            public long GetAvailableRamMb() => 0;
            public bool IsUserActive() => true;
            public string? GetRawMachineIdentity() => RawIdentity;

            public void Dispose() { }
        }

        #endregion
    }
}
