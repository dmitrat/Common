using System.Linq;
using OutWit.Common.Settings.Collections;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Serialization;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsCollectionTests
    {
        #region Constructor Tests

        [Test]
        public void ConstructorSetsGroupAndDisplayNameTest()
        {
            var collection = new SettingsCollection("General", "General Settings", 1);

            Assert.That(collection.Group, Is.EqualTo("General"));
            Assert.That(collection.DisplayName, Is.EqualTo("General Settings"));
            Assert.That(collection.Priority, Is.EqualTo(1));
        }

        [Test]
        public void ConstructorDefaultsDisplayNameToGroupTest()
        {
            var collection = new SettingsCollection("Advanced");

            Assert.That(collection.DisplayName, Is.EqualTo("Advanced"));
            Assert.That(collection.Priority, Is.EqualTo(0));
        }

        #endregion

        #region Add/Access Tests

        [Test]
        public void AddAndAccessByKeyTest()
        {
            var collection = new SettingsCollection("General");
            var serializer = new SettingsSerializerString();
            var value = new SettingsValue<string>("UserName", "String", "", false,
                SettingsScope.User, serializer, "", "");

            collection.Add(value);

            Assert.That(collection.ContainsKey("UserName"), Is.True);
            Assert.That(collection["UserName"], Is.SameAs(value));
            Assert.That(collection.Count, Is.EqualTo(1));
        }

        [Test]
        public void EnumerationReturnsAllValuesTest()
        {
            var collection = new SettingsCollection("General");
            var serializer = new SettingsSerializerString();

            collection.Add(new SettingsValue<string>("A", "String", "", false,
                SettingsScope.User, serializer, "", ""));
            collection.Add(new SettingsValue<string>("B", "String", "", false,
                SettingsScope.User, serializer, "", ""));
            collection.Add(new SettingsValue<string>("C", "String", "", false,
                SettingsScope.User, serializer, "", ""));

            var keys = collection.Select(v => v.Key).ToList();

            Assert.That(keys, Does.Contain("A"));
            Assert.That(keys, Does.Contain("B"));
            Assert.That(keys, Does.Contain("C"));
            Assert.That(collection.Count, Is.EqualTo(3));
        }

        #endregion

        #region Is Tests

        [Test]
        public void IsReturnsTrueForSameGroupTest()
        {
            var c1 = new SettingsCollection("General");
            var c2 = new SettingsCollection("General");

            Assert.That(c1.Is(c2), Is.True);
        }

        [Test]
        public void IsReturnsFalseForDifferentGroupTest()
        {
            var c1 = new SettingsCollection("General");
            var c2 = new SettingsCollection("Advanced");

            Assert.That(c1.Is(c2), Is.False);
        }

        #endregion
    }
}
