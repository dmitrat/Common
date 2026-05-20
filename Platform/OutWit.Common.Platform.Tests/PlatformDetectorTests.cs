using OutWit.Common.Platform;

namespace OutWit.Common.Platform.Tests
{
    [TestFixture]
    public sealed class PlatformDetectorTests
    {
        #region Tests

        [Test]
        public void GetCurrentPlatformReturnsExpectedCurrentRuntimePlatformTest()
        {
            var platform = PlatformDetector.GetCurrentPlatform();

            if (OperatingSystem.IsAndroid())
            {
                Assert.That(platform, Is.EqualTo(PlatformKind.Android));
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                Assert.That(platform, Is.EqualTo(PlatformKind.Windows));
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                Assert.That(platform, Is.EqualTo(PlatformKind.Linux));
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                Assert.That(platform, Is.EqualTo(PlatformKind.MacOS));
                return;
            }

            Assert.That(platform, Is.EqualTo(PlatformKind.Unknown));
        }

        #endregion
    }
}
