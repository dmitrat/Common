using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Serialization;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class SettingsValueTests
    {
        #region Constructor Tests

        [Test]
        public void ConstructorSetsPropertiesCorrectlyTest()
        {
            var serializer = new SettingsSerializerString();
            var value = new SettingsValue<string>("UserName", "String", "", false,
                SettingsScope.User, serializer, "admin", "admin");

            Assert.That(value.Key, Is.EqualTo("UserName"));
            Assert.That(value.Name, Is.EqualTo("UserName"));
            Assert.That(value.ValueKind, Is.EqualTo("String"));
            Assert.That(value.Tag, Is.EqualTo(""));
            Assert.That(value.Hidden, Is.False);
            Assert.That(value.Scope, Is.EqualTo(SettingsScope.User));
            Assert.That(value.DefaultValue, Is.EqualTo("admin"));
            Assert.That(value.Value, Is.EqualTo("admin"));
        }

        #endregion

        #region DefaultValue/Value Tests

        [Test]
        public void SetDefaultValueAndValueTest()
        {
            var serializer = new SettingsSerializerString();
            var value = new SettingsValue<string>("UserName", "String", "", false,
                SettingsScope.User, serializer, "admin", "john");

            Assert.That(value.DefaultValue, Is.EqualTo("admin"));
            Assert.That(value.Value, Is.EqualTo("john"));
            Assert.That(value.IsDefault, Is.False);
        }

        [Test]
        public void IsDefaultTrueWhenValuesEqualTest()
        {
            var serializer = new SettingsSerializerInteger();
            var value = new SettingsValue<int>("MaxRetries", "Integer", "", false,
                SettingsScope.User, serializer, 3, 3);

            Assert.That(value.IsDefault, Is.True);
        }

        [Test]
        public void IsDefaultFalseWhenValuesDifferTest()
        {
            var serializer = new SettingsSerializerInteger();
            var value = new SettingsValue<int>("MaxRetries", "Integer", "", false,
                SettingsScope.User, serializer, 3, 5);

            Assert.That(value.IsDefault, Is.False);
        }

        [Test]
        public void IsDefaultUpdatesWhenValueChangesTest()
        {
            var serializer = new SettingsSerializerString();
            var value = new SettingsValue<string>("Name", "String", "", false,
                SettingsScope.User, serializer, "admin", "admin");

            Assert.That(value.IsDefault, Is.True);

            ISettingsValue iv = value;
            iv.Value = "john";
            Assert.That(value.IsDefault, Is.False);

            iv.Value = "admin";
            Assert.That(value.IsDefault, Is.True);
        }

        #endregion

        #region Is Tests

        [Test]
        public void IsReturnsTrueForEqualValuesTest()
        {
            var serializer = new SettingsSerializerString();

            var v1 = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.User, serializer, "a", "b");
            var v2 = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.User, serializer, "a", "b");

            Assert.That(v1.Is(v2), Is.True);
        }

        [Test]
        public void IsReturnsFalseForDifferentValuesTest()
        {
            var serializer = new SettingsSerializerString();

            var v1 = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.User, serializer, "a", "b");
            var v2 = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.User, serializer, "a", "c");

            Assert.That(v1.Is(v2), Is.False);
        }

        #endregion

        #region Clone Tests

        [Test]
        public void CloneCreatesEqualCopyTest()
        {
            var serializer = new SettingsSerializerString();
            var value = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.User, serializer, "default", "user");
            value.Name = "Display Name";

            var clone = (SettingsValue<string>)value.Clone();

            Assert.That(clone.Key, Is.EqualTo("Key"));
            Assert.That(clone.Name, Is.EqualTo("Display Name"));
            Assert.That(clone.DefaultValue, Is.EqualTo("default"));
            Assert.That(clone.Value, Is.EqualTo("user"));
            Assert.That(clone.Scope, Is.EqualTo(SettingsScope.User));
            Assert.That(value.Is(clone), Is.True);
        }

        #endregion

        #region PropertyChanged Tests

        [Test]
        public void PropertyChangedFiresOnValueChangeTest()
        {
            var serializer = new SettingsSerializerString();
            var value = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.User, serializer, "", "");

            var changedProps = new List<string>();
            value.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

            value.Value = "test";

            Assert.That(changedProps, Does.Contain("Value"));
        }

        #endregion

        #region Scope Tests

        [Test]
        public void ScopeIsPreservedTest()
        {
            var serializer = new SettingsSerializerString();

            var userValue = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.User, serializer, "a", "b");
            var globalValue = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.Global, serializer, "a", "b");
            var defaultValue = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.Default, serializer, "a", "a");

            Assert.That(userValue.Scope, Is.EqualTo(SettingsScope.User));
            Assert.That(globalValue.Scope, Is.EqualTo(SettingsScope.Global));
            Assert.That(defaultValue.Scope, Is.EqualTo(SettingsScope.Default));
        }

        [Test]
        public void IsReturnsFalseForDifferentScopesTest()
        {
            var serializer = new SettingsSerializerString();

            var v1 = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.User, serializer, "a", "b");
            var v2 = new SettingsValue<string>("Key", "String", "", false,
                SettingsScope.Global, serializer, "a", "b");

            Assert.That(v1.Is(v2), Is.False);
        }

        #endregion
    }
}
