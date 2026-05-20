using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Platform.Tests.Directories
{
    [TestFixture]
    public sealed class StandardDirectoryProviderTests
    {
        #region Tests

        [Test]
        public void ConstructorWithoutValidBaseSegmentsThrowsArgumentExceptionTest()
        {
            Assert.That(
                () => _ = new StandardDirectoryProvider(string.Empty, " "),
                Throws.ArgumentException);
        }

        [Test]
        public void SemanticDirectoryMethodsAppendConfiguredApplicationScopeTest()
        {
            var provider = new StandardDirectoryProvider("OutWit", "PlatformTests");

            var userData = provider.GetUserDataDirectory();
            var sharedData = provider.GetSharedDataDirectory();
            var cache = provider.GetCacheDirectory();
            var logs = provider.GetLogsDirectory();
            var config = provider.GetConfigDirectory();
            var temp = provider.GetTempDirectory();

            Assert.Multiple(() =>
            {
                Assert.That(userData, Does.EndWith(Path.Combine("OutWit", "PlatformTests")));
                Assert.That(sharedData, Does.EndWith(Path.Combine("OutWit", "PlatformTests")));
                Assert.That(cache, Does.EndWith(Path.Combine("OutWit", "PlatformTests")));
                Assert.That(logs, Does.EndWith(Path.Combine("OutWit", "PlatformTests")));
                Assert.That(config, Does.EndWith(Path.Combine("OutWit", "PlatformTests")));
                Assert.That(temp, Does.EndWith(Path.Combine("OutWit", "PlatformTests")));
            });
        }

        [Test]
        public void SemanticDirectoryMethodsAppendAdditionalSegmentsTest()
        {
            var provider = new StandardDirectoryProvider("OutWit", "PlatformTests");

            var logs = provider.GetLogsDirectory("Client", "Current");
            var cache = provider.GetCacheDirectory("Controllers");

            Assert.Multiple(() =>
            {
                Assert.That(logs, Does.EndWith(Path.Combine("OutWit", "PlatformTests", "Client", "Current")));
                Assert.That(cache, Does.EndWith(Path.Combine("OutWit", "PlatformTests", "Controllers")));
            });
        }

        [Test]
        public void CurrentRuntimeUserDataAndTempDirectoriesUseExpectedRootsTest()
        {
            var provider = new StandardDirectoryProvider("OutWit", "PlatformTests");
            var userData = provider.GetUserDataDirectory();
            var temp = provider.GetTempDirectory();

            Assert.Multiple(() =>
            {
                Assert.That(userData, Does.StartWith(GetExpectedUserDataRoot()));
                Assert.That(temp, Does.StartWith(Path.GetTempPath()));
            });
        }

        #endregion

        #region Functions

        private static string GetExpectedUserDataRoot()
        {
            if (OperatingSystem.IsWindows())
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (OperatingSystem.IsLinux())
            {
                var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (!string.IsNullOrWhiteSpace(xdgDataHome))
                    return xdgDataHome;

                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local",
                    "share");
            }

            if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support");
            }

            if (OperatingSystem.IsAndroid())
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        #endregion
    }
}
