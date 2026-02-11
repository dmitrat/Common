using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsEntryTests
    {
        #region Constructor Tests

        [Test]
        public void DefaultConstructorInitializesEmptyStringsTest()
        {
            var entry = new SettingsEntry();

            Assert.That(entry.Group, Is.EqualTo(""));
            Assert.That(entry.Key, Is.EqualTo(""));
            Assert.That(entry.Value, Is.EqualTo(""));
            Assert.That(entry.ValueKind, Is.EqualTo(""));
            Assert.That(entry.Tag, Is.EqualTo(""));
            Assert.That(entry.Hidden, Is.False);
        }

        #endregion

        #region Is Tests

        [Test]
        public void IsReturnsTrueForEqualEntriesTest()
        {
            var e1 = new SettingsEntry
            {
                Group = "General",
                Key = "Name",
                Value = "admin",
                ValueKind = "String",
                Tag = "",
                Hidden = false
            };

            var e2 = new SettingsEntry
            {
                Group = "General",
                Key = "Name",
                Value = "admin",
                ValueKind = "String",
                Tag = "",
                Hidden = false
            };

            Assert.That(e1.Is(e2), Is.True);
        }

        [Test]
        public void IsReturnsFalseForDifferentEntriesTest()
        {
            var e1 = new SettingsEntry { Key = "Name", Value = "admin", ValueKind = "String" };
            var e2 = new SettingsEntry { Key = "Name", Value = "john", ValueKind = "String" };

            Assert.That(e1.Is(e2), Is.False);
        }

        #endregion

        #region Clone Tests

        [Test]
        public void CloneCreatesEqualCopyTest()
        {
            var entry = new SettingsEntry
            {
                Group = "General",
                Key = "Name",
                Value = "admin",
                ValueKind = "String",
                Tag = "tag",
                Hidden = true
            };

            var clone = (SettingsEntry)entry.Clone();

            Assert.That(entry.Is(clone), Is.True);
            Assert.That(clone, Is.Not.SameAs(entry));
        }

        #endregion
    }
}
