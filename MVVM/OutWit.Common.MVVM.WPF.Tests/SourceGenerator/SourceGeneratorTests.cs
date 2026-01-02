using NUnit.Framework;
using System.Windows;

namespace OutWit.Common.MVVM.WPF.Tests.SourceGenerator
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class SourceGeneratorTests
    {
        #region DependencyProperty Field Tests

        [Test]
        public void TextPropertyFieldExistsTest()
        {
            var field = typeof(TestControl).GetField("TextProperty", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
        }

        [Test]
        public void NumberPropertyFieldExistsTest()
        {
            var field = typeof(TestControl).GetField("NumberProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
        }

        [Test]
        public void IsCheckedPropertyFieldExistsTest()
        {
            var field = typeof(TestControl).GetField("IsCheckedProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
        }

        #endregion

        #region Property Getter Tests

        [Test]
        public void TextPropertyGetterReturnsDefaultValueTest()
        {
            var control = new TestControl();

            var value = control.Text;

            Assert.That(value, Is.EqualTo("Default"));
        }

        [Test]
        public void NumberPropertyGetterReturnsDefaultValueTest()
        {
            var control = new TestControl();

            var value = control.Number;

            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void IsCheckedPropertyGetterReturnsDefaultValueTest()
        {
            var control = new TestControl();

            var value = control.IsChecked;

            Assert.That(value, Is.False);
        }

        #endregion

        #region Property Setter Tests

        [Test]
        public void TextPropertySetterUpdatesValueTest()
        {
            var control = new TestControl();

            control.Text = "NewValue";

            Assert.That(control.Text, Is.EqualTo("NewValue"));
        }

        [Test]
        public void NumberPropertySetterUpdatesValueTest()
        {
            var control = new TestControl();

            control.Number = 100;

            Assert.That(control.Number, Is.EqualTo(100));
        }

        [Test]
        public void IsCheckedPropertySetterUpdatesValueTest()
        {
            var control = new TestControl();

            control.IsChecked = true;

            Assert.That(control.IsChecked, Is.True);
        }

        #endregion

        #region GetValue SetValue Tests

        [Test]
        public void TextPropertyUsesGetValueTest()
        {
            var control = new TestControl();
            var textProperty = (DependencyProperty)typeof(TestControl)
                .GetField("TextProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                .GetValue(null)!;

            control.SetValue(textProperty, "DirectValue");

            Assert.That(control.Text, Is.EqualTo("DirectValue"));
        }

        [Test]
        public void TextPropertyUsesSetValueTest()
        {
            var control = new TestControl();
            var textProperty = (DependencyProperty)typeof(TestControl)
                .GetField("TextProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                .GetValue(null)!;

            control.Text = "PropertyValue";

            Assert.That(control.GetValue(textProperty), Is.EqualTo("PropertyValue"));
        }

        #endregion

        #region Metadata Tests

        [Test]
        public void NumberPropertyHasAffectsMeasureMetadataTest()
        {
            var numberProperty = (DependencyProperty)typeof(TestControl)
                .GetField("NumberProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                .GetValue(null)!;

            var metadata = numberProperty.GetMetadata(typeof(TestControl)) as FrameworkPropertyMetadata;

            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata!.AffectsMeasure, Is.True);
        }

        [Test]
        public void IsCheckedPropertyHasBindsTwoWayByDefaultMetadataTest()
        {
            var isCheckedProperty = (DependencyProperty)typeof(TestControl)
                .GetField("IsCheckedProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                .GetValue(null)!;

            var metadata = isCheckedProperty.GetMetadata(typeof(TestControl)) as FrameworkPropertyMetadata;

            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata!.BindsTwoWayByDefault, Is.True);
        }

        #endregion
    }
}
