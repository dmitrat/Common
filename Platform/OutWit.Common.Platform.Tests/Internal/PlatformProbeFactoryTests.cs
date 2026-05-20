using OutWit.Common.Platform;
using OutWit.Common.Platform.Internal;

namespace OutWit.Common.Platform.Tests.Internal
{
    /// <summary>
    /// Coverage for the per-OS probe selection. Each <see cref="PlatformKind"/>
    /// must map to a fresh probe that reports the matching <c>Kind</c>, and
    /// <c>ForCurrentPlatform</c> must agree with <c>PlatformDetector</c>.
    /// </summary>
    [TestFixture]
    public sealed class PlatformProbeFactoryTests
    {
        [TestCase(PlatformKind.Windows)]
        [TestCase(PlatformKind.Linux)]
        [TestCase(PlatformKind.MacOS)]
        [TestCase(PlatformKind.Android)]
        [TestCase(PlatformKind.Unknown)]
        public void ForPlatformReturnsProbeWithMatchingKindTest(PlatformKind kind)
        {
            using var probe = PlatformProbeFactory.ForPlatform(kind);

            Assert.That(probe.Kind, Is.EqualTo(kind));
        }

        [Test]
        public void ForPlatformReturnsFreshInstanceOnEachCallTest()
        {
            using var a = PlatformProbeFactory.ForPlatform(PlatformKind.Linux);
            using var b = PlatformProbeFactory.ForPlatform(PlatformKind.Linux);

            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void ForCurrentPlatformAgreesWithPlatformDetectorTest()
        {
            using var probe = PlatformProbeFactory.ForCurrentPlatform();

            Assert.That(probe.Kind, Is.EqualTo(PlatformDetector.GetCurrentPlatform()));
        }
    }
}
