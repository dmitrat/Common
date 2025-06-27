
using NUnit.Framework;
using OutWit.Common.Tests.Mock;
using OutWit.Common.Utils;
using System;

namespace OutWit.Common.Tests
{

    [TestFixture]
    public class PropertiesUtilsTests
    {
        #region NameOfProperty Tests

        [Test]
        public void NameOfProperty_ForValidProperty_ReturnsCorrectName()
        {
            // Act
            var name = ((System.Linq.Expressions.Expression<Func<PropertyTestModel, string>>)(m => m.PublicProperty)).NameOfProperty();

            // Assert
            Assert.That(name, Is.EqualTo("PublicProperty"));
        }

        [Test]
        public void NameOfProperty_ForField_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                ((System.Linq.Expressions.Expression<Func<PropertyTestModel, string>>)(m => m.m_publicField)).NameOfProperty()
            );
        }

        #endregion

        #region With (Action) Tests

        [Test]
        public void With_Action_ClonesObjectAndAppliesPublicSetter()
        {
            // Arrange
            var original = new PropertyTestModel();

            // Act
            var updated = original.With(m => m.PublicProperty = "Updated");

            // Assert
            Assert.That(updated, Is.Not.SameAs(original), "Should be a new instance (clone).");
            Assert.That(updated.PublicProperty, Is.EqualTo("Updated"), "Clone's property should be updated.");
            Assert.That(original.PublicProperty, Is.EqualTo("Initial"), "Original object's property should remain unchanged.");
        }

        #endregion

        #region With (Expression) Tests

        [Test]
        public void With_Expression_ForPublicSetter_ClonesAndSetsValue()
        {
            // Arrange
            var original = new PropertyTestModel();

            // Act
            var updated = original.With(m => m.PublicProperty, "Updated via Expression");

            // Assert
            Assert.That(updated, Is.Not.SameAs(original), "Should be a new instance.");
            Assert.That(updated.PublicProperty, Is.EqualTo("Updated via Expression"));
            Assert.That(original.PublicProperty, Is.EqualTo("Initial"));
        }

        [Test]
        public void With_Expression_ForPrivateSetter_ClonesAndSetsValue()
        {
            // Arrange
            var original = new PropertyTestModel();

            // Act
            var updated = original.With(m => m.PrivateSetterProperty, "Updated Private");

            // Assert
            Assert.That(updated, Is.Not.SameAs(original), "Should be a new instance.");
            Assert.That(updated.GetPrivateSetterValue(), Is.EqualTo("Updated Private"), "Clone's private-set property should be updated.");
            Assert.That(original.GetPrivateSetterValue(), Is.EqualTo("Initial"), "Original's private-set property should be unchanged.");
        }

        [Test]
        public void With_Expression_ForGetOnlyProperty_ThrowsInvalidOperationException()
        {
            // Arrange
            var original = new PropertyTestModel();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                original.With(m => m.GetOnlyProperty, "Attempt to write")
            );
        }

        [Test]
        public void With_Expression_ForField_ThrowsArgumentException()
        {
            // Arrange
            var original = new PropertyTestModel();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                original.With(m => m.m_publicField, "Attempt to write")
            );
        }

        #endregion
    }
}
