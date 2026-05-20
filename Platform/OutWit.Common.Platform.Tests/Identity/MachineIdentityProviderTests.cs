using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Platform.Tests.Identity
{
    [TestFixture]
    public sealed class MachineIdentityProviderTests
    {
        #region Tests

        [Test]
        public async Task GetMachineIdentityAsyncReturnsStable64CharacterHexValueTest()
        {
            var provider = new MachineIdentityProvider(new StandardDirectoryProvider("OutWit", "PlatformTests", Guid.NewGuid().ToString("N")));

            var first = await provider.GetMachineIdentityAsync();
            var second = await provider.GetMachineIdentityAsync();

            Assert.Multiple(() =>
            {
                Assert.That(first, Has.Length.EqualTo(64));
                Assert.That(second, Has.Length.EqualTo(64));
                Assert.That(first, Is.EqualTo(second));
                Assert.That(first.All(Uri.IsHexDigit), Is.True);
            });
        }

        #endregion
    }
}
