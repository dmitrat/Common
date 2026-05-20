using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Platform.Tests.SystemInfo
{
    [TestFixture]
    public sealed class SystemProfileProviderTests
    {
        #region Tests

        [Test]
        public async Task CollectAsyncReturnsCurrentRuntimeSystemProfileTest()
        {
            var provider = new SystemProfileProvider();

            var profile = await provider.CollectAsync();

            Assert.Multiple(() =>
            {
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.Os.Platform, Is.EqualTo(PlatformDetector.GetCurrentPlatform()));
                Assert.That(profile.Cpu.LogicalCoreCount, Is.GreaterThan(0));
                Assert.That(profile.Memory.TotalRamMb, Is.GreaterThanOrEqualTo(0));
                Assert.That(profile.Gpus, Is.Not.Null);
                Assert.That(profile.TempStorage.AvailableSpaceMb, Is.GreaterThanOrEqualTo(0));
            });
        }

        #endregion
    }
}
