using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Platform;
using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.SystemInfo;
using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Platform.Tests.Providers
{
    /// <summary>
    /// SystemHealthProvider must (a) read its values straight from the probe,
    /// and (b) dispose the probe when disposed. The second guarantee matters
    /// because the Windows probe holds PerformanceCounter handles.
    /// </summary>
    [TestFixture]
    public sealed class SystemHealthProviderProbeInjectionTests
    {
        [Test]
        public async Task ProbeValuesFlowIntoHealthSnapshotTest()
        {
            using var probe = new RecordingProbe
            {
                CpuLoad = 42.5,
                AvailableRamMb = 8192,
                UserActive = false
            };

            var snapshot = await new SystemHealthProvider(probe).CollectAsync();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.CpuLoadPercent, Is.EqualTo(42.5));
                Assert.That(snapshot.AvailableRamMb, Is.EqualTo(8192L));
                Assert.That(snapshot.IsUserActive, Is.False);
                Assert.That(snapshot.TimestampUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [Test]
        public async Task ProviderDisposeForwardsToProbeTest()
        {
            var probe = new RecordingProbe();
            var provider = new SystemHealthProvider(probe);

            _ = await provider.CollectAsync();
            provider.Dispose();

            Assert.That(probe.DisposeCallCount, Is.EqualTo(1));
        }

        [Test]
        public void DoubleProviderDisposeDoesNotDoubleDisposeProbeTest()
        {
            var probe = new RecordingProbe();
            var provider = new SystemHealthProvider(probe);

            provider.Dispose();
            provider.Dispose();

            Assert.That(probe.DisposeCallCount, Is.EqualTo(1));
        }

        #region Fakes

        private sealed class RecordingProbe : IPlatformProbe
        {
            public PlatformKind Kind => PlatformKind.Unknown;
            public double CpuLoad { get; set; }
            public long AvailableRamMb { get; set; }
            public bool UserActive { get; set; } = true;
            public int DisposeCallCount { get; private set; }

            public string GetCpuModelName() => string.Empty;
            public IReadOnlyList<SystemGpuInfo> GetGpus() => Array.Empty<SystemGpuInfo>();
            public SystemStorageType GetStorageType(string rootPath) => SystemStorageType.Unknown;
            public double GetCpuLoadPercent() => CpuLoad;
            public long GetAvailableRamMb() => AvailableRamMb;
            public bool IsUserActive() => UserActive;
            public string? GetRawMachineIdentity() => null;

            public void Dispose() => DisposeCallCount++;
        }

        #endregion
    }
}
