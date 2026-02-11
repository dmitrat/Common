using System.IO;
using System.Reflection;
using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsPathResolverTests
    {
        #region BuildRelativePath Tests

        [Test]
        public void BuildRelativePathDepthOneTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;
            var result = SettingsPathResolver.BuildRelativePath(assembly, 1);

            var name = assembly.GetName().Name!;
            var parts = name.Split('.');
            var expected = Path.Combine(parts[0], string.Join(".", parts, 1, parts.Length - 1));

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildRelativePathDepthTwoTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;
            var result = SettingsPathResolver.BuildRelativePath(assembly, 2);

            var name = assembly.GetName().Name!;
            var parts = name.Split('.');
            var expected = Path.Combine(parts[0], parts[1], string.Join(".", parts, 2, parts.Length - 2));

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildRelativePathDepthZeroReturnsFullNameTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;
            var result = SettingsPathResolver.BuildRelativePath(assembly, 0);

            Assert.That(result, Is.EqualTo(assembly.GetName().Name));
        }

        [Test]
        public void BuildRelativePathDepthExceedingPartsReturnsFullNameTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;
            var name = assembly.GetName().Name!;
            var partsCount = name.Split('.').Length;

            var result = SettingsPathResolver.BuildRelativePath(assembly, partsCount);

            Assert.That(result, Is.EqualTo(name));
        }

        #endregion

        #region GetUserDataPath Tests

        [Test]
        public void GetUserDataPathStartsWithAppDataTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;
            var result = SettingsPathResolver.GetUserDataPath(assembly, 1);

            var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            Assert.That(result, Does.StartWith(appData));
        }

        #endregion

        #region GetGlobalDataPath Tests

        [Test]
        public void GetGlobalDataPathStartsWithProgramDataTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;
            var result = SettingsPathResolver.GetGlobalDataPath(assembly, 1);

            var programData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            Assert.That(result, Does.StartWith(programData));
        }

        #endregion

        #region GetDefaultsPath Tests

        [Test]
        public void GetDefaultsPathReturnsConventionalPathTest()
        {
            var result = SettingsPathResolver.GetDefaultsPath(".json");

            var expected = Path.Combine(System.AppContext.BaseDirectory, "Resources", "settings.json");
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetDefaultsPathWorksWithDifferentExtensionsTest()
        {
            var json = SettingsPathResolver.GetDefaultsPath(".json");
            var csv = SettingsPathResolver.GetDefaultsPath(".csv");
            var db = SettingsPathResolver.GetDefaultsPath(".db");

            Assert.That(json, Does.EndWith("settings.json"));
            Assert.That(csv, Does.EndWith("settings.csv"));
            Assert.That(db, Does.EndWith("settings.db"));
        }

        #endregion

        #region GetScopeDataPath Tests

        [Test]
        public void GetScopeDataPathUserReturnsAppDataTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;
            var result = SettingsPathResolver.GetScopeDataPath(SettingsScope.User, assembly, 1);

            var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            Assert.That(result, Does.StartWith(appData));
        }

        [Test]
        public void GetScopeDataPathGlobalReturnsProgramDataTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;
            var result = SettingsPathResolver.GetScopeDataPath(SettingsScope.Global, assembly, 1);

            var programData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            Assert.That(result, Does.StartWith(programData));
        }

        [Test]
        public void GetScopeDataPathDefaultThrowsTest()
        {
            var assembly = typeof(SettingsPathResolverTests).Assembly;

            Assert.Throws<System.ArgumentException>(() =>
                SettingsPathResolver.GetScopeDataPath(SettingsScope.Default, assembly, 1));
        }

        #endregion
    }
}
