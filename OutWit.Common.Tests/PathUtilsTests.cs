using NUnit.Framework;
using OutWit.Common.Utils; // Ваш неймспейс
using System;
using System.IO;
using System.Reflection;

namespace OutWit.Common.Tests
{
    [TestFixture]
    public class PathUtilsTests
    {
        private string _testDirectory = null!;

        [SetUp]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "PathUtilsTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #region AppendPath Tests

        [Test]
        public void AppendPath_ShouldCombinePathsCorrectly()
        {
            var part1 = @"C:\Users\Test";
            var part2 = "Documents";
            var result = part1.AppendPath(part2);
            Assert.That(result, Is.EqualTo(@"C:\Users\Test\Documents"));
        }

        #endregion

        #region DirectoryName Tests

        [Test]
        public void DirectoryName_ForFullPath_ShouldReturnDirectory()
        {
            var path = @"C:\Users\Test folder\test file.txt";
            var result = path.DirectoryName();
            Assert.That(result, Is.EqualTo(@"C:\Users\Test folder"));
        }

        #endregion

        #region IsFullPath Tests

        [Test]
        [TestCase(@"C:\folder\file.txt", true)]
        [TestCase(@"\\server\share", true)]
        [TestCase(@"relative\path.txt", false)]
        [TestCase(null, false)]
        public void IsFullPath_ShouldCorrectlyIdentifyPaths(string? path, bool expected)
        {
            var result = path.IsFullPath();
            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion

        #region Application Path Helper Tests (Updated Region)

        [TestCase(Environment.SpecialFolder.ApplicationData)]
        [TestCase(Environment.SpecialFolder.CommonApplicationData)]
        public void ApplicationPathHelpers_ShouldConstructCorrectPath(Environment.SpecialFolder specialFolder)
        {
            // Arrange
            var assembly = Assembly.GetExecutingAssembly();
            var expectedBasePath = Environment.GetFolderPath(specialFolder);
            string resultPath;

            // Act
            if (specialFolder == Environment.SpecialFolder.ApplicationData)
            {
                resultPath = assembly.ApplicationDataPath(1, "logs");
            }
            else
            {
                resultPath = assembly.ProgramDataPath(1, "logs");
            }

            // Assert
            Assert.That(resultPath, Does.StartWith(expectedBasePath));
            var assemblyNamePart = assembly.GetName().Name!.Split('.').First();
            var expectedEnding = Path.Combine(assemblyNamePart, "logs");
            Assert.That(resultPath, Does.EndWith(expectedEnding));
        }

        #endregion

        #region GetFullPath Tests

        [Test]
        public void GetFullPath_ForRelativePath_ShouldResolveAgainstAssemblyLocation()
        {
            var relativeDir = "SubFolder";
            var expectedFullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, relativeDir);
            Directory.CreateDirectory(expectedFullPath);

            var result = relativeDir.GetFullPath();

            Assert.That(result, Is.EqualTo(expectedFullPath));
        }

        [Test]
        public void GetFullPath_ForNonExistentPath_ShouldReturnEmptyString()
        {
            var relativeDir = "NonExistentFolder";
            var result = relativeDir.GetFullPath();
            Assert.That(result, Is.EqualTo(""));
        }

        #endregion

        #region CheckFolder Tests

        [Test]
        public void CheckFolder_ForNonExistentPath_ShouldCreateDirectoryAndReturnTrue()
        {
            var newFolderPath = Path.Combine(_testDirectory, "NewFolder");
            var result = newFolderPath.CheckFolder();
            Assert.That(result, Is.True);
            Assert.That(Directory.Exists(newFolderPath), Is.True);
        }

        [Test]
        public void CheckFolder_ForExistingPath_ShouldReturnTrue()
        {
            var existingFolderPath = Path.Combine(_testDirectory, "ExistingFolder");
            Directory.CreateDirectory(existingFolderPath);
            var result = existingFolderPath.CheckFolder();
            Assert.That(result, Is.True);
        }

        #endregion

        #region CopyFile & CopyFolder Tests

        [Test]
        public void CopyFile_WithDeleteSource_ShouldMoveFile()
        {
            var sourceFile = Path.Combine(_testDirectory, "source.txt");
            var destFile = Path.Combine(_testDirectory, "dest.txt");
            File.WriteAllText(sourceFile, "test");

            var result = PathUtils.CopyFile(sourceFile, destFile, true);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(destFile), Is.True);
            Assert.That(File.Exists(sourceFile), Is.False);
        }

        [Test]
        public void CopyFolder_ShallowCopy_ShouldCopyFilesButNotFolders()
        {
            var sourceDir = Path.Combine(_testDirectory, "Source");
            var destDir = Path.Combine(_testDirectory, "Destination");
            Directory.CreateDirectory(sourceDir);

            File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content");
            Directory.CreateDirectory(Path.Combine(sourceDir, "SubFolder"));

            var result = PathUtils.CopyFolder(sourceDir, destDir, false);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(Path.Combine(destDir, "file1.txt")), Is.True, "File should be copied.");
            Assert.That(Directory.Exists(Path.Combine(destDir, "SubFolder")), Is.False, "Subfolder should NOT be copied (shallow copy).");
        }

        #endregion

        #region Path/Url Conversion Tests

        [Test]
        public void PathToUrl_And_UrlToPath_ShouldPerformConversion()
        {
            var basePath = @"C:\MyWebApp";
            var fullPath = @"C:\MyWebApp\images\logo.png";

            var url = fullPath.PathToUrl(basePath);
            var convertedPath = url.UrlToPath(basePath);

            Assert.That(url, Is.EqualTo("/images/logo.png"));
            Assert.That(convertedPath, Is.EqualTo(fullPath));
        }

        #endregion
    }
}
