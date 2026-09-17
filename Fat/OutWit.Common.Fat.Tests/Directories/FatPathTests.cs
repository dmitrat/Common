using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class FatPathTests
    {
        #region Split Tests

        [TestCase("/", new string[0])]
        [TestCase("", new string[0])]
        [TestCase("a/b", new[] { "a", "b" })]
        [TestCase("/a/b/", new[] { "a", "b" })]
        [TestCase("\\a\\b", new[] { "a", "b" })]
        [TestCase("//a///b", new[] { "a", "b" })]
        [TestCase("/a/./b", new[] { "a", "b" })]
        [TestCase("/a/c/../b", new[] { "a", "b" })]
        [TestCase("/a/..", new string[0])]
        [TestCase("/Long Name.txt", new[] { "Long Name.txt" })]
        public void PathIsSplitIntoComponentsTest(string path, string[] expected)
        {
            Assert.That(FatPath.Split(path), Is.EqualTo(expected));
        }

        [TestCase("..")]
        [TestCase("/a/../..")]
        public void ClimbingAboveRootIsRejectedTest(string path)
        {
            Assert.Throws<ArgumentException>(() => FatPath.Split(path));
        }

        [Test]
        public void NullPathIsRejectedTest()
        {
            Assert.Throws<ArgumentNullException>(() => FatPath.Split(null!));
        }

        #endregion

        #region Join Tests

        [Test]
        public void EntryPathIsCombinedTest()
        {
            Assert.That(FatPath.Combine("/", "a"), Is.EqualTo("/a"));
            Assert.That(FatPath.Combine("/a", "b c"), Is.EqualTo("/a/b c"));
        }

        [Test]
        public void ComponentsAreJoinedTest()
        {
            Assert.That(FatPath.Join(Array.Empty<string>()), Is.EqualTo("/"));
            Assert.That(FatPath.Join(new[] { "a", "b" }), Is.EqualTo("/a/b"));
        }

        #endregion
    }
}
