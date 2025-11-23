using System;
using System.Collections.Generic;
using System.Text;
using OutWit.Common.Tests.Mock;
using OutWit.Common.Values;

namespace OutWit.Common.Tests
{
    [TestFixture]
    public class StringEnumTests
    {
        // --- Conversion & Basic Info Tests ---

        [Test]
        public void IsTest()
        {
            Assert.That(ColorEnum.Red.Is(ColorEnum.Red), Is.True);
            Assert.That(ColorEnum.Red.Is(ColorEnum.Green), Is.False);
        }

        [Test]
        public void ToStringReturnsUnderlyingStringValue()
        {
            // Arrange
            var color = ColorEnum.Red;

            // Act
            var result = color.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("RED"));
        }

        [Test]
        public void ImplicitConversionToStringReturnsValue()
        {
            // Arrange
            ColorEnum color = ColorEnum.Green;

            // Act
            string result = color; // Implicit cast

            // Assert
            Assert.That(result, Is.EqualTo("GREEN"));
        }

        [Test]
        public void ImplicitConversionFromNullReturnsNull()
        {
            // Arrange
            ColorEnum? color = null;

            // Act
            string? result = color;

            // Assert
            Assert.That(result, Is.Null);
        }

        // --- Parsing Logic Tests ---

        [Test]
        public void ParseExistingValueReturnsCorrectInstance()
        {
            // Act
            var result = ColorEnum.Parse("RED");

            // Assert
            Assert.That(result, Is.EqualTo(ColorEnum.Red));
            Assert.That(ReferenceEquals(result, ColorEnum.Red), Is.True, "Should return the exact static instance");
        }

        [Test]
        public void ParseNonExistingValueThrowsInvalidOperationException()
        {
            // Arrange
            var invalidValue = "PURPLE";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => ColorEnum.Parse(invalidValue));
            Assert.That(ex!.Message, Does.Contain("not a valid ColorEnum"));
        }

        [Test]
        public void ParseNullOrEmptyThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ColorEnum.Parse(null!));
            Assert.Throws<ArgumentNullException>(() => ColorEnum.Parse(""));
            Assert.Throws<ArgumentNullException>(() => ColorEnum.Parse("   "));
        }

        [Test]
        public void TryParseExistingValueReturnsTrueAndInstance()
        {
            // Act
            bool success = ColorEnum.TryParse("BLUE", out var result);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(ColorEnum.Blue));
        }

        [Test]
        public void TryParseNonExistingValueReturnsFalseAndNull()
        {
            // Act
            bool success = ColorEnum.TryParse("YELLOW", out var result);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetAllReturnsAllStaticInstances()
        {
            // Act
            var allColors = ColorEnum.GetAll();

            // Assert
            Assert.That(allColors, Has.Count.EqualTo(3));
            Assert.That(allColors, Does.Contain((object?)ColorEnum.Red));
            Assert.That(allColors, Does.Contain((object?)ColorEnum.Green));
            Assert.That(allColors, Does.Contain((object?)ColorEnum.Blue));
        }

        // --- Equality & Comparison Tests ---

        [Test]
        public void EqualsReturnsTrueForSameInstance()
        {
            // Arrange
            var color1 = ColorEnum.Red;
            var color2 = ColorEnum.Red;

            // Assert
            Assert.That(color1.Equals(color2), Is.True);
        }

        [Test]
        public void EqualsReturnsTrueForDifferentReferencesWithSameValue()
        {
            // Note: Since the constructor is private, we can't easily create a "fake" duplicate 
            // from outside without Reflection. However, Parse guarantees returning the singleton.
            // This test ensures that if logic changes to allow new instances, value comparison still holds.

            // Arrange
            var color1 = ColorEnum.Red;
            var color2 = ColorEnum.Parse("RED");

            // Assert
            Assert.That(color1.Equals(color2), Is.True);
        }

        [Test]
        public void EqualsReturnsFalseForDifferentValues()
        {
            // Arrange
            var color1 = ColorEnum.Red;
            var color2 = ColorEnum.Blue;

            // Assert
            Assert.That(color1.Equals(color2), Is.False);
        }

        [Test]
        public void EqualsReturnsFalseForNull()
        {
            // Arrange
            var color = ColorEnum.Red;

            // Assert
            Assert.That(color.Equals(null), Is.False);
        }

        [Test]
        public void EqualityOperatorReturnsTrueForSameValues()
        {
            // Arrange
            var color1 = ColorEnum.Green;
            var color2 = ColorEnum.Parse("GREEN");

            // Assert
            Assert.That(color1 == color2, Is.True);
        }

        [Test]
        public void EqualityOperatorReturnsFalseForDifferentValues()
        {
            // Arrange
            var color1 = ColorEnum.Green;
            var color2 = ColorEnum.Red;

            // Assert
            Assert.That(color1 == color2, Is.False);
        }

        [Test]
        public void EqualityOperatorHandlesNulls()
        {
            // Arrange
            ColorEnum? null1 = null;
            ColorEnum? null2 = null;
            ColorEnum? notNull = ColorEnum.Red;

            // Assert
            Assert.That(null1 == null2, Is.True, "null == null should be true");
            Assert.That(null1 == notNull, Is.False, "null == instance should be false");
            Assert.That(notNull == null1, Is.False, "instance == null should be false");
        }

        [Test]
        public void InequalityOperatorWorksCorrectly()
        {
            // Arrange
            var color1 = ColorEnum.Red;
            var color2 = ColorEnum.Blue;

            // Assert
            Assert.That(color1 != color2, Is.True);
            Assert.That(color1 != ColorEnum.Red, Is.False);
        }

        [Test]
        public void GetHashCodeReturnsSameValueForSameUnderlyingString()
        {
            // Arrange
            var color1 = ColorEnum.Red;
            var color2 = ColorEnum.Parse("RED"); // Should be same instance/value

            // Act & Assert
            Assert.That(color1.GetHashCode(), Is.EqualTo(color2.GetHashCode()));
        }

        [Test]
        public void GetHashCodeReturnsDifferentValuesForDifferentStrings()
        {
            // Arrange
            var color1 = ColorEnum.Red;
            var color2 = ColorEnum.Blue;

            // Act & Assert
            Assert.That(color1.GetHashCode(), Is.Not.EqualTo(color2.GetHashCode()));
        }
    }

}
