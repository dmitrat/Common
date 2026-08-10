using System;
using System.IO;
using OutWit.Common.Licensing.Binding;

namespace OutWit.Common.Licensing.Tests.Binding
{
    /// <summary>
    /// The identity of an installation, which is the anchor the whole server
    /// binding rests on. Every rule below is a failure mode if broken, and
    /// three of them are silent.
    /// </summary>
    [TestFixture]
    public sealed class LicenseInstallIdTests
    {
        private string m_directory = null!;

        [SetUp]
        public void SetUp()
        {
            m_directory = Path.Combine(Path.GetTempPath(), "owl-install-id-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_directory))
                Directory.Delete(m_directory, recursive: true);
        }

        #region Configuration Tests

        [Test]
        public void ConfigurationWinsOverTheFileTest()
        {
            // The installer writes it into .env, which is what makes the identity
            // available before the host starts, identical across replicas, and
            // knowable before first start.
            LicenseInstallId.FromFile(m_directory);

            Assert.That(LicenseInstallId.Resolve("from-the-installer", m_directory), Is.EqualTo("from-the-installer"));
        }

        [Test]
        public void ConfigurationIsTrimmedTest()
        {
            Assert.That(LicenseInstallId.Resolve("  padded  ", m_directory), Is.EqualTo("padded"));
        }

        [Test]
        public void BlankConfigurationFallsBackToTheFileTest()
        {
            var generated = LicenseInstallId.Resolve("   ", m_directory);

            Assert.Multiple(() =>
            {
                Assert.That(generated, Is.Not.Empty);
                Assert.That(File.Exists(Path.Combine(m_directory, LicenseInstallId.FILE_NAME)), Is.True);
            });
        }

        #endregion

        #region Persistence Tests

        [Test]
        public void GeneratedOnceAndNeverAgainTest()
        {
            // Regenerating would make every restart a new installation, and the
            // licence would die on the second start of a perfectly good server.
            var first = LicenseInstallId.FromFile(m_directory);
            var second = LicenseInstallId.FromFile(m_directory);
            var third = LicenseInstallId.Resolve(null, m_directory);

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.Not.Empty);
                Assert.That(second, Is.EqualTo(first));
                Assert.That(third, Is.EqualTo(first));
            });
        }

        [Test]
        public void SurvivesTheProcessThatWroteItTest()
        {
            // The container-recreation case, in the only form a unit test can
            // reach: nothing in memory, the directory as the volume left it.
            var first = LicenseInstallId.FromFile(m_directory);

            var reread = File.ReadAllText(Path.Combine(m_directory, LicenseInstallId.FILE_NAME)).Trim();

            Assert.That(reread, Is.EqualTo(first));
        }

        [Test]
        public void TwoDeploymentsAreTwoIdentitiesTest()
        {
            var other = Path.Combine(Path.GetTempPath(), "owl-install-id-" + Guid.NewGuid().ToString("N"));

            try
            {
                Assert.That(LicenseInstallId.FromFile(other), Is.Not.EqualTo(LicenseInstallId.FromFile(m_directory)));
            }
            finally
            {
                if (Directory.Exists(other))
                    Directory.Delete(other, recursive: true);
            }
        }

        [Test]
        public void NoTemporaryFileIsLeftBehindTest()
        {
            LicenseInstallId.FromFile(m_directory);

            Assert.That(Directory.GetFiles(m_directory, "*.tmp"), Is.Empty);
        }

        #endregion

        #region Shape Tests

        [Test]
        public void IsOneHundredAndTwentyEightBitsOfHexTest()
        {
            var generated = LicenseInstallId.Generate();

            Assert.Multiple(() =>
            {
                Assert.That(generated, Has.Length.EqualTo(LicenseInstallId.BYTE_COUNT * 2));
                Assert.That(generated, Does.Match("^[0-9a-f]+$"), "Lower-case hex, so it survives a copy and paste.");
            });
        }

        [Test]
        public void EveryGeneratedValueIsDifferentTest()
        {
            // Unguessable is the whole property: a second deployment cannot
            // invent the first one's identity, it would have to steal it.
            var values = new System.Collections.Generic.HashSet<string>();

            for (var index = 0; index < 200; index++)
                values.Add(LicenseInstallId.Generate());

            Assert.That(values, Has.Count.EqualTo(200));
        }

        #endregion

        #region Failure Tests

        [Test]
        public void AnUnwritableDirectoryYieldsNothingRatherThanSomethingNewTest()
        {
            // An absent factor fails the licence visibly. One that shifted on
            // every start would fail it mysteriously, which is far worse.
            var unwritable = Path.Combine(m_directory, "file-where-a-directory-should-be");

            Directory.CreateDirectory(m_directory);
            File.WriteAllText(unwritable, "not a directory");

            Assert.That(LicenseInstallId.FromFile(unwritable), Is.Empty);
        }

        #endregion
    }
}
