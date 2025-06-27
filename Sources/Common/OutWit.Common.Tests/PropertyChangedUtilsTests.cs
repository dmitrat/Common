
using NUnit.Framework;
using OutWit.Common.Tests.Mock;
using OutWit.Common.Utils;
using System.ComponentModel;

namespace OutWit.Common.Tests
{

    [TestFixture]
    public class PropertyChangedUtilsTests
    {
        #region FirePropertyChanged Tests

        [Test]
        public void FirePropertyChanged_ForClassWithMethod_InvokesEvent()
        {
            // Arrange
            var model = new ViewModelWithMethod();
            string? receivedPropertyName = null;
            model.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };

            // Act
            model.FirePropertyChanged("TestProperty");

            // Assert
            Assert.That(receivedPropertyName, Is.EqualTo("TestProperty"));
        }

        [Test]
        public void FirePropertyChanged_ForClassWithFieldOnly_InvokesEvent()
        {
            // Arrange
            var model = new ViewModelWithFieldOnly();
            string? receivedPropertyName = null;
            model.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };

            // Act
            model.FirePropertyChanged("FieldTest");

            // Assert
            Assert.That(receivedPropertyName, Is.EqualTo("FieldTest"));
        }

        [Test]
        public void FirePropertyChanged_ForInheritedClass_FindsAndInvokesBaseMethod()
        {
            var model = new InheritedViewModel();
            string? receivedPropertyName = null;
            model.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };

            model.FirePropertyChanged("InheritedTest");

            // Assert
            Assert.That(receivedPropertyName, Is.EqualTo("InheritedTest"));
        }

        [Test]
        public void FirePropertyChanged_ForUnsupportedClass_DoesNothingAndDoesNotThrow()
        {
            // Arrange
            var model = new UnsupportedViewModel();
            bool wasCalled = false;
            model.PropertyChanged += (sender, args) => { wasCalled = true; };

            // Act & Assert
            Assert.DoesNotThrow(() => model.FirePropertyChanged("WontWork"));
            Assert.That(wasCalled, Is.False);
        }

        [Test]
        public void FirePropertyChanged_UsesCacheForEfficiency()
        {
            // Arrange
            var model1 = new ViewModelWithMethod();
            string? received1 = null;
            model1.PropertyChanged += (_, args) => { received1 = args.PropertyName; };

            var model2 = new ViewModelWithMethod();
            string? received2 = null;
            model2.PropertyChanged += (_, args) => { received2 = args.PropertyName; };

            // Act
            model1.FirePropertyChanged("Call1");
            model2.FirePropertyChanged("Call2");

            // Assert
            Assert.That(received1, Is.EqualTo("Call1"));
            Assert.That(received2, Is.EqualTo("Call2"));
        }

        #endregion

        #region IsProperty & NameOfProperty Tests

        [Test]
        public void IsProperty_ForMatchingProperty_ReturnsTrue()
        {
            // Arrange
            var eventArgs = new PropertyChangedEventArgs(nameof(NameOfTestModel.MyProperty));

            // Act
            var result = eventArgs.IsProperty<NameOfTestModel, string>(m => m.MyProperty);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void NameOfProperty_ForValidProperty_ReturnsCorrectName()
        {
            // Arrange
            System.Linq.Expressions.Expression<System.Func<NameOfTestModel, string>> expr = m => m.MyProperty;

            // Act
            var name = expr.NameOfProperty();

            // Assert
            Assert.That(name, Is.EqualTo("MyProperty"));
        }

        [Test]
        public void NameOfProperty_ForField_ThrowsArgumentException()
        {
            // Arrange
            System.Linq.Expressions.Expression<System.Func<NameOfTestModel, int>> expr = m => m.MyField;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => expr.NameOfProperty());
        }

        #endregion
    }
}
