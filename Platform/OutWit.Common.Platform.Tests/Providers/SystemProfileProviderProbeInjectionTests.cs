using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OutWit.Common.Platform;
using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.SystemInfo;
using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Platform.Tests.Providers
{
    /// <summary>
    /// Strategy-pattern smoke: a fake probe drives the SystemProfileProvider
    /// from a unit-test host that may not be on Windows/Linux/macOS/Android.
    /// Proves that per-OS code paths can be exercised independently of the
    /// actual current platform.
    /// </summary>
    [TestFixture]
    public sealed class SystemProfileProviderProbeInjectionTests
    {
        [Test]
        public async Task ProbeValuesFlowIntoSystemProfileTest()
        {
            var probe = new FakeProbe
            {
                Kind = PlatformKind.Linux,
                CpuModelName = "Intel(R) Core(TM) i9-13900K",
                Gpus = new[]
                {
                    new SystemGpuInfo
                    {
                        ModelName = "NVIDIA RTX 4090",
                        VRamMb = 24576,
                        GpuType = SystemGpuType.Discrete,
                        SupportedFeatures = SystemGpuFeatures.Cuda | SystemGpuFeatures.Vulkan
                    }
                },
                StorageType = SystemStorageType.NVMe
            };

            using var provider = new SystemHealthProviderTestHandle(probe);
            var profile = await new SystemProfileProvider(probe).CollectAsync();

            Assert.Multiple(() =>
            {
                Assert.That(profile.Os.Platform, Is.EqualTo(PlatformKind.Linux));
                Assert.That(profile.Cpu.ModelName, Is.EqualTo("Intel(R) Core(TM) i9-13900K"));
                Assert.That(profile.Cpu.LogicalCoreCount, Is.GreaterThan(0));
                Assert.That(profile.Cpu.Architecture, Is.EqualTo(RuntimeInformation.ProcessArchitecture));
                Assert.That(profile.Gpus, Has.Count.EqualTo(1));
                Assert.That(profile.Gpus[0].ModelName, Is.EqualTo("NVIDIA RTX 4090"));
                Assert.That(profile.TempStorage.StorageType, Is.EqualTo(SystemStorageType.NVMe));
            });
        }

        [Test]
        public async Task UnknownProbeProducesWellFormedEmptyProfileTest()
        {
            // The Null probe is the safety net for unknown hosts.
            using var probe = new PlatformProbeNull();
            var profile = await new SystemProfileProvider(probe).CollectAsync();

            Assert.Multiple(() =>
            {
                Assert.That(profile.Os.Platform, Is.EqualTo(PlatformKind.Unknown));
                Assert.That(profile.Cpu.ModelName, Is.Empty);
                Assert.That(profile.Gpus, Is.Empty);
                Assert.That(profile.TempStorage.StorageType, Is.EqualTo(SystemStorageType.Unknown));
            });
        }

        #region Fakes

        private sealed class FakeProbe : IPlatformProbe
        {
            public PlatformKind Kind { get; set; } = PlatformKind.Unknown;
            public string CpuModelName { get; set; } = string.Empty;
            public IReadOnlyList<SystemGpuInfo> Gpus { get; set; } = System.Array.Empty<SystemGpuInfo>();
            public SystemStorageType StorageType { get; set; } = SystemStorageType.Unknown;
            public double CpuLoad { get; set; }
            public long AvailableRamMb { get; set; }
            public bool UserActive { get; set; } = true;
            public string? RawMachineIdentity { get; set; }

            public string GetCpuModelName() => CpuModelName;
            public IReadOnlyList<SystemGpuInfo> GetGpus() => Gpus;
            public SystemStorageType GetStorageType(string rootPath) => StorageType;
            public double GetCpuLoadPercent() => CpuLoad;
            public long GetAvailableRamMb() => AvailableRamMb;
            public bool IsUserActive() => UserActive;
            public string? GetRawMachineIdentity() => RawMachineIdentity;

            public void Dispose() { }
        }

        /// <summary>
        /// Holds the probe alive so the using statement in the test reads naturally;
        /// SystemProfileProvider does not own the probe but a real consumer would.
        /// </summary>
        private sealed class SystemHealthProviderTestHandle : System.IDisposable
        {
            private readonly IPlatformProbe m_probe;
            public SystemHealthProviderTestHandle(IPlatformProbe probe) { m_probe = probe; }
            public void Dispose() => m_probe.Dispose();
        }

        #endregion
    }
}
