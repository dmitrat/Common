using NUnit.Framework;
using OutWit.Common.Tests.Mock;
using System;
using System.Globalization;
using System.Threading;

namespace OutWit.Common.Tests
{

    [TestFixture]
    public class ModelBaseTests
    {
        private CultureInfo _originalCulture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }

        [OneTimeTearDown]
        public void TearDownFixture()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
        }

        [Test]
        public void ToString_WithAllAttributeVariations_FormatsStringCorrectly()
        {
            // Arrange
            var model = new ProductTestModel
            {
                ProductId = 101,
                ProductName = "Test Widget",
                UnitPrice = 99.95m,
                ProductCode = 255, // 0xFF
                StockCount = 50 // This property should be ignored
            };

            // Act
            var result = model.ToString();

            // Assert
            var expected = "ID: 101, ProductName: Test Widget, Price: 99.95, ProductCode: 000000FF";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ToString_WhenNoPropertiesHaveAttribute_ReturnsBaseToString()
        {
            // Arrange
            var model = new ModelWithoutAttributes { Id = 1, Name = "Test" };
            var expected = model.GetType().ToString();

            // Act
            var result = model.ToString();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ToString_OnObjectWithNullPropertyValue_RendersNullString()
        {
            // Arrange
            var model = new ProductTestModel
            {
                ProductId = 102,
                ProductName = null,
                UnitPrice = 10.5m,
                ProductCode = 1
            };

            // Act
            var result = model.ToString();

            // Assert
            var expected = "ID: 102, ProductName: null, Price: 10.50, ProductCode: 00000001";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ToString_OnMultipleCalls_ReturnsSameResultAndUsesCache()
        {
            // Arrange
            var model = new ProductTestModel
            {
                ProductId = 999,
                ProductName = "Cached Item",
                UnitPrice = 1.23m,
                ProductCode = 10
            };

            // Act
            var result1 = model.ToString();
            var result2 = model.ToString();

            // Assert
            Assert.That(result1, Is.Not.Empty);
            Assert.That(result1, Is.EqualTo(result2), "Result should be consistent on multiple calls.");
        }
    }
}
