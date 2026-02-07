using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using OutWit.Common.NewRelic.Nrql;

namespace OutWit.Common.NewRelic.Tests.Nrql
{

#if DEBUG

    [TestFixture]
    public class NrqlStringUtilsTests
    {
        [Test]
        public void ToNrqlTimestampFormatsCorrectlyTest()
        {
            // Arrange
            var dto = new DateTime(2025, 1, 15, 08, 30, 0);
            var expected = "'2025-01-15 08:30:00 +0000'";

            // Act
            var result = NrqlStringUtils.ToNrqlTimestamp(dto);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ToNrqlLiteralHandlesBooleansTest()
        {
            Assert.That(NrqlStringUtils.ToNrqlLiteralFromString("true"), Is.EqualTo("true"));
            Assert.That(NrqlStringUtils.ToNrqlLiteralFromString("false"), Is.EqualTo("false"));
        }

        [Test]
        public void ToNrqlLiteralHandlesNumbersTest()
        {
            Assert.That(NrqlStringUtils.ToNrqlLiteralFromString("123"), Is.EqualTo("123"));
            Assert.That(NrqlStringUtils.ToNrqlLiteralFromString("123.456"), Is.EqualTo("123.456"));
        }

        [Test]
        public void ToNrqlLiteralHandlesDateTimeOffsetTest()
        {
            var dtString = "2025-01-01T12:00:00Z";
            var expected = "'2025-01-01 12:00:00 +0000'";
            Assert.That(NrqlStringUtils.ToNrqlLiteralFromString(dtString), Is.EqualTo(expected));
        }

        [Test]
        public void ToNrqlLiteralHandlesStringsTest()
        {
            var str = "Hello World";
            var expected = "'Hello World'";
            Assert.That(NrqlStringUtils.ToNrqlLiteralFromString(str), Is.EqualTo(expected));
        }

        [Test]
        public void EscapeSingleQuotedHandlesSpecialCharactersTest()
        {
            Assert.That(NrqlStringUtils.EscapeSingleQuoted("simple"), Is.EqualTo("simple"));
            Assert.That(NrqlStringUtils.EscapeSingleQuoted("it's"), Is.EqualTo("it\\'s"));
            Assert.That(NrqlStringUtils.EscapeSingleQuoted("c:\\path"), Is.EqualTo("c:\\\\path"));
            Assert.That(NrqlStringUtils.EscapeSingleQuoted("c:\\it's"), Is.EqualTo("c:\\\\it\\'s"));
        }

        [Test]
        public void EqualsAnyIgnoreCasePerformsCorrectComparisonTest()
        {
            Assert.That("test".EqualsAnyIgnoreCase("TEST", "prod"), Is.True);
            Assert.That("Test".EqualsAnyIgnoreCase("dev", "prod"), Is.False);
            Assert.That(((string)null).EqualsAnyIgnoreCase("test"), Is.False);
            Assert.That("test".EqualsAnyIgnoreCase(), Is.False);
        }

        [Test]
        public void JsonElementAsStringHandlesAllValueKindsTest()
        {
            using (var doc = JsonDocument.Parse("{\"n\":null, \"t\":true, \"f\":false, \"s\":\"text\", \"num\":123}"))
            {
                var root = doc.RootElement;
                Assert.That(root.GetProperty("n").AsString(), Is.Null);
                Assert.That(root.GetProperty("t").AsString(), Is.EqualTo("true"));
                Assert.That(root.GetProperty("f").AsString(), Is.EqualTo("false"));
                Assert.That(root.GetProperty("s").AsString(), Is.EqualTo("text"));
                Assert.That(root.GetProperty("num").AsString(), Is.EqualTo("123"));
            }
        }
    }

#endif
}
