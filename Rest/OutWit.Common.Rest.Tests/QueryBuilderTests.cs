using OutWit.Common.Rest.Tests.Model;
using System.Globalization;

namespace OutWit.Common.Rest.Tests
{
    [TestFixture]
    public class QueryBuilderTest
    {
        [SetUp]
        public void Setup()
        {
            // Set up a consistent culture for tests to ensure predictable formatting
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            // Register default adapters for every test to ensure isolation
            // This is especially important if other tests modify global state like AdapterUtils
            Utils.AdapterUtils.Register<int>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Utils.AdapterUtils.Register<DateTime>("G", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
        }

        [Test]
        public async Task AsStringAsyncWithSingleParameterTest()
        {
            // Arrange
            var builder = new QueryBuilder();
            builder.AddParameter("name", "value");

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            Assert.That(result, Is.EqualTo("name=value"));
        }

        [Test]
        public async Task AsStringAsyncWithMultipleParametersTest()
        {
            // Arrange
            var builder = new QueryBuilder();
            builder.AddParameter("param1", "value1");
            builder.AddParameter("param2", 123);
            builder.AddParameter("param3", true);

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            Assert.That(result, Is.EqualTo("param1=value1&param2=123&param3=true"));
        }

        [Test]
        public async Task AsStringAsyncIgnoresNullValueTest()
        {
            // Arrange
            var builder = new QueryBuilder();
            builder.AddParameter("param1", "value1");
            builder.AddParameter("param2", (string?)null);

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            Assert.That(result, Is.EqualTo("param1=value1"));
        }

        [Test]
        public async Task AsStringAsyncWithUrlEncodingTest()
        {
            // Arrange
            var builder = new QueryBuilder();
            builder.AddParameter("query", "c# and .net");

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            // FormUrlEncodedContent uses '+' for spaces, which is valid.
            Assert.That(result, Is.EqualTo("query=c%23+and+.net"));
        }

        [Test]
        public async Task AsStringAsyncWithEnumParameterTest()
        {
            // Arrange
            var builder = new QueryBuilder();
            builder.AddParameter("enum_val", TestEnum.Option2);

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            Assert.That(result, Is.EqualTo("enum_val=Option2"));
        }

        [Test]
        public async Task AsStringAsyncWithStringArrayTest()
        {
            // Arrange
            var builder = new QueryBuilder();
            var values = new[] { "one", "two", "three" };
            builder.AddParameter("items", values);

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            Assert.That(result, Is.EqualTo("items=one%2Ctwo%2Cthree")); // Note: comma is URL-encoded
        }

        [Test]
        public async Task AsStringAsyncWithIntegerEnumerableTest()
        {
            // Arrange
            var builder = new QueryBuilder();
            var values = new List<int> { 1, 2, 3 };
            builder.AddParameter<int>("ids", values);

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            Assert.That(result, Is.EqualTo("ids=1%2C2%2C3"));
        }

        [Test]
        public async Task AsStringAsyncWithCustomFormatTest()
        {
            // Arrange
            var builder = new QueryBuilder();
            var date = new DateTime(2025, 6, 28, 17, 0, 0, DateTimeKind.Utc);
            builder.AddParameter("eventDate", date, "yyyy-MM-dd"); // Pass a custom format

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            Assert.That(result, Is.EqualTo("eventDate=2025-06-28"));
        }

        [Test]
        public async Task AsStringAsyncWithEmptyBuilderTest()
        {
            // Arrange
            var builder = new QueryBuilder();

            // Act
            var result = await builder.AsStringAsync();

            // Assert
            Assert.That(result, Is.Empty);
        }
    }
}