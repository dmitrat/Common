using Avalonia;
using NUnit.Framework;

namespace OutWit.Common.MVVM.Avalonia.Tests.SourceGenerator
{
    [TestFixture]
    public class DirectPropertyTests
    {
        #region DirectProperty Field Tests

        [Test]
        public void CounterPropertyFieldExistsTest()
        {
            var field = typeof(TestDirectPropertyControl).GetField("CounterProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType.Name, Does.Contain("DirectProperty"));
        }

        [Test]
        public void LabelPropertyFieldExistsTest()
        {
            var field = typeof(TestDirectPropertyControl).GetField("LabelProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType.Name, Does.Contain("DirectProperty"));
        }

        [Test]
        public void IsActivePropertyFieldExistsTest()
        {
            var field = typeof(TestDirectPropertyControl).GetField("IsActiveProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType.Name, Does.Contain("DirectProperty"));
        }

        #endregion

        #region Backing Field Tests

        [Test]
        public void CounterBackingFieldExistsTest()
        {
            var field = typeof(TestDirectPropertyControl).GetField("m_counter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void LabelBackingFieldExistsTest()
        {
            var field = typeof(TestDirectPropertyControl).GetField("m_label",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void IsActiveBackingFieldExistsTest()
        {
            var field = typeof(TestDirectPropertyControl).GetField("m_isActive",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(bool)));
        }

        #endregion

        #region Property Getter Tests

        [Test]
        public void CounterPropertyGetterReturnsDefaultValueTest()
        {
            var control = new TestDirectPropertyControl();

            var value = control.Counter;

            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void LabelPropertyGetterReturnsDefaultValueTest()
        {
            var control = new TestDirectPropertyControl();

            var value = control.Label;

            Assert.That(value, Is.EqualTo(""));
        }

        [Test]
        public void IsActivePropertyGetterReturnsDefaultValueTest()
        {
            var control = new TestDirectPropertyControl();

            var value = control.IsActive;

            Assert.That(value, Is.False);
        }

        #endregion

        #region Property Setter Tests

        [Test]
        public void CounterPropertySetterUpdatesValueTest()
        {
            var control = new TestDirectPropertyControl();

            control.Counter = 100;

            Assert.That(control.Counter, Is.EqualTo(100));
        }

        [Test]
        public void LabelPropertySetterUpdatesValueTest()
        {
            var control = new TestDirectPropertyControl();

            control.Label = "TestLabel";

            Assert.That(control.Label, Is.EqualTo("TestLabel"));
        }

        [Test]
        public void IsActivePropertySetterUpdatesValueTest()
        {
            var control = new TestDirectPropertyControl();

            control.IsActive = true;

            Assert.That(control.IsActive, Is.True);
        }

        #endregion

        #region GetValue SetValue Tests

        [Test]
        public void CounterPropertyUsesGetValueTest()
        {
            var control = new TestDirectPropertyControl();
            var counterProperty = (AvaloniaProperty)typeof(TestDirectPropertyControl)
                .GetField("CounterProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                .GetValue(null)!;

            control.SetValue(counterProperty, 50);

            Assert.That(control.Counter, Is.EqualTo(50));
        }

        [Test]
        public void CounterPropertyUsesSetValueTest()
        {
            var control = new TestDirectPropertyControl();
            var counterProperty = (AvaloniaProperty)typeof(TestDirectPropertyControl)
                .GetField("CounterProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                .GetValue(null)!;

            control.Counter = 75;

            Assert.That(control.GetValue(counterProperty), Is.EqualTo(75));
        }

        #endregion
    }
}
