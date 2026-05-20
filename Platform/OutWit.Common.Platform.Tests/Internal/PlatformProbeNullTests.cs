using OutWit.Common.Platform;
using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Tests.Internal
{
    /// <summary>
    /// The null probe must produce empty/default answers without throwing —
    /// the contract that lets the public providers stay safe on an
    /// <see cref="PlatformKind.Unknown"/> host.
    /// </summary>
    [TestFixture]
    public sealed class PlatformProbeNullTests
    {
        [Test]
        public void KindIsUnknownTest()
        {
            using var probe = new PlatformProbeNull();

            Assert.That(probe.Kind, Is.EqualTo(PlatformKind.Unknown));
        }

        [Test]
        public void AllProbeMethodsReturnSafeDefaultsTest()
        {
            using var probe = new PlatformProbeNull();

            Assert.Multiple(() =>
            {
                Assert.That(probe.GetCpuModelName(), Is.Empty);
                Assert.That(probe.GetGpus(), Is.Empty);
                Assert.That(probe.GetStorageType("/"), Is.EqualTo(SystemStorageType.Unknown));
                Assert.That(probe.GetCpuLoadPercent(), Is.EqualTo(0.0));
                Assert.That(probe.GetAvailableRamMb(), Is.EqualTo(0L));
                Assert.That(probe.IsUserActive(), Is.True);
                Assert.That(probe.GetRawMachineIdentity(), Is.Null);
            });
        }

        [Test]
        public void DisposeIsIdempotentTest()
        {
            var probe = new PlatformProbeNull();

            probe.Dispose();
            Assert.DoesNotThrow(() => probe.Dispose());
        }
    }
}
