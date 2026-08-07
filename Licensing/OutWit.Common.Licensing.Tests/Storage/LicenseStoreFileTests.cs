using System;
using System.IO;
using System.Linq;
using OutWit.Common.Licensing.Storage;

namespace OutWit.Common.Licensing.Tests.Storage
{
    /// <summary>
    /// The file store touches real disk, where the interesting failures live:
    /// a directory that does not exist yet, a re-installed duplicate, a corrupt
    /// sidecar.
    /// </summary>
    [TestFixture]
    public sealed class LicenseStoreFileTests
    {
        private string m_directory = null!;
        private LicenseTestContext m_context = null!;

        [SetUp]
        public void SetUp()
        {
            m_directory = Path.Combine(Path.GetTempPath(), $"outwit-licensing-tests-{Guid.NewGuid():N}");
            m_context = new LicenseTestContext();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(m_directory))
                    Directory.Delete(m_directory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        #region Token Tests

        [Test]
        public void MissingDirectoryReadsAsEmptyTest()
        {
            var store = new LicenseStoreFile(m_directory);

            Assert.Multiple(() =>
            {
                Assert.That(store.ReadTokens(), Is.Empty);
                Assert.That(store.ReadState().FirstRunUtc, Is.EqualTo(default(DateTime)));
            });
        }

        [Test]
        public void SavedTokenIsReadBackVerbatimTest()
        {
            var store = new LicenseStoreFile(m_directory);
            var token = m_context.Issue(LicenseTestContext.Payload());

            store.Save(token);

            Assert.That(store.ReadTokens().Single(), Is.EqualTo(token));
        }

        [Test]
        public void ReinstallingTheSameLicenceDoesNotDuplicateItTest()
        {
            var store = new LicenseStoreFile(m_directory);
            var token = m_context.Issue(LicenseTestContext.Payload());

            store.Save(token);
            store.Save(token);

            Assert.That(store.ReadTokens(), Has.Count.EqualTo(1),
                "Two copies of one licence would both validate and inflate every count taken from the store.");
        }

        [Test]
        public void SeveralLicencesCoexistTest()
        {
            var store = new LicenseStoreFile(m_directory);

            store.Save(m_context.Issue(LicenseTestContext.Payload()));
            store.Save(m_context.Issue(LicenseTestContext.Payload()));

            Assert.That(store.ReadTokens(), Has.Count.EqualTo(2));
        }

        [Test]
        public void RemoveDeletesByLicenceIdTest()
        {
            var store = new LicenseStoreFile(m_directory);
            var payload = LicenseTestContext.Payload();

            store.Save(m_context.Issue(payload));

            Assert.Multiple(() =>
            {
                Assert.That(store.Remove(payload.Id), Is.True);
                Assert.That(store.ReadTokens(), Is.Empty);
                Assert.That(store.Remove(payload.Id), Is.False);
            });
        }

        [Test]
        public void SavingSomethingThatIsNotATokenThrowsTest()
        {
            var store = new LicenseStoreFile(m_directory);

            Assert.That(() => store.Save("not-a-licence"), Throws.ArgumentException);
        }

        [Test]
        public void UnreadableFilesAreSkippedNotFatalTest()
        {
            var store = new LicenseStoreFile(m_directory);
            store.Save(m_context.Issue(LicenseTestContext.Payload()));

            File.WriteAllText(Path.Combine(m_directory, "notes.txt"), "not a licence");
            File.WriteAllText(Path.Combine(m_directory, "junk.lic"), "garbage");

            // Junk is returned and left for the validator to reject — the store
            // does not judge content, but it must not crash on it either.
            Assert.That(store.ReadTokens(), Has.Count.EqualTo(2));
        }

        #endregion

        #region State Tests

        [Test]
        public void StateSurvivesARoundTripTest()
        {
            var store = new LicenseStoreFile(m_directory);
            var written = new LicenseStoreState
            {
                FirstRunUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                HighWaterMarkUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            store.WriteState(written);

            Assert.That(store.ReadState().Is(written), Is.True);
        }

        [Test]
        public void CorruptStateDegradesToFreshRatherThanThrowingTest()
        {
            var store = new LicenseStoreFile(m_directory);
            store.WriteState(new LicenseStoreState { FirstRunUtc = DateTime.UtcNow });

            File.WriteAllText(Path.Combine(m_directory, "licensing.state.json"), "{ not json");

            Assert.That(store.ReadState().FirstRunUtc, Is.EqualTo(default(DateTime)),
                "An unreadable sidecar must not stop the product from starting.");
        }

        [Test]
        public void StateWriteLeavesNoTemporaryFileBehindTest()
        {
            var store = new LicenseStoreFile(m_directory);

            store.WriteState(new LicenseStoreState { FirstRunUtc = DateTime.UtcNow });
            store.WriteState(new LicenseStoreState { FirstRunUtc = DateTime.UtcNow.AddDays(1) });

            Assert.That(Directory.EnumerateFiles(m_directory, "*.tmp"), Is.Empty);
        }

        #endregion
    }
}
