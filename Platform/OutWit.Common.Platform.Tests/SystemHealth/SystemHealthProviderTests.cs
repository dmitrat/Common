using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Platform.Tests.SystemHealth
{
    [TestFixture]
    public sealed class SystemHealthProviderTests
    {
        #region Tests

        [Test]
        public async Task CollectAsyncReturnsReusableSystemHealthSnapshotTest()
        {
            using var provider = new SystemHealthProvider();

            var snapshot = await provider.CollectAsync();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.TimestampUtc, Is.Not.EqualTo(default(DateTime)));
                Assert.That(snapshot.CpuLoadPercent, Is.GreaterThanOrEqualTo(0));
                Assert.That(snapshot.AvailableRamMb, Is.GreaterThanOrEqualTo(0));
                Assert.That(snapshot.IsUserActive, Is.True.Or.False);
            });
        }

        #endregion
    }
}
