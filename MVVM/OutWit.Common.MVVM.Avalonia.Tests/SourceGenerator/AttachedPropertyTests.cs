using Avalonia;
using Avalonia.Controls;
using NUnit.Framework;

namespace OutWit.Common.MVVM.Avalonia.Tests.SourceGenerator
{
    [TestFixture]
    public class AttachedPropertyTests
    {
        #region AttachedProperty Field Tests

        [Test]
        public void IsHighlightedPropertyFieldExistsTest()
        {
            var field = typeof(TestAttachedProperties).GetField("IsHighlightedProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType.Name, Does.Contain("AttachedProperty"));
        }

        [Test]
        public void OpacityPropertyFieldExistsTest()
        {
            var field = typeof(TestAttachedProperties).GetField("OpacityProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType.Name, Does.Contain("AttachedProperty"));
        }

        [Test]
        public void TagPropertyFieldExistsTest()
        {
            var field = typeof(TestAttachedProperties).GetField("TagProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType.Name, Does.Contain("AttachedProperty"));
        }

        #endregion

        #region Get Method Tests

        [Test]
        public void GetIsHighlightedMethodExistsTest()
        {
            var method = typeof(TestAttachedProperties).GetMethod("GetIsHighlighted",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(bool)));
        }

        [Test]
        public void GetOpacityMethodExistsTest()
        {
            var method = typeof(TestAttachedProperties).GetMethod("GetOpacity",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(double)));
        }

        [Test]
        public void GetTagMethodExistsTest()
        {
            var method = typeof(TestAttachedProperties).GetMethod("GetTag",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(string)));
        }

        #endregion

        #region Set Method Tests

        [Test]
        public void SetIsHighlightedMethodExistsTest()
        {
            var method = typeof(TestAttachedProperties).GetMethod("SetIsHighlighted",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(void)));
        }

        [Test]
        public void SetOpacityMethodExistsTest()
        {
            var method = typeof(TestAttachedProperties).GetMethod("SetOpacity",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(void)));
        }

        [Test]
        public void SetTagMethodExistsTest()
        {
            var method = typeof(TestAttachedProperties).GetMethod("SetTag",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(void)));
        }

        #endregion

        #region Functional Tests

        [Test]
        public void AttachedPropertyGetSetWorksTest()
        {
            var button = new Button();

            TestAttachedProperties.SetIsHighlighted(button, true);
            var result = TestAttachedProperties.GetIsHighlighted(button);

            Assert.That(result, Is.True);
        }

        [Test]
        public void AttachedPropertyDefaultValueTest()
        {
            var button = new Button();

            var result = TestAttachedProperties.GetIsHighlighted(button);

            Assert.That(result, Is.False);
        }

        [Test]
        public void AttachedPropertyOpacityWorksTest()
        {
            var button = new Button();

            TestAttachedProperties.SetOpacity(button, 0.5);
            var result = TestAttachedProperties.GetOpacity(button);

            Assert.That(result, Is.EqualTo(0.5));
        }

        [Test]
        public void AttachedPropertyTagWorksTest()
        {
            var button = new Button();

            TestAttachedProperties.SetTag(button, "TestTag");
            var result = TestAttachedProperties.GetTag(button);

            Assert.That(result, Is.EqualTo("TestTag"));
        }

        #endregion
    }
}
