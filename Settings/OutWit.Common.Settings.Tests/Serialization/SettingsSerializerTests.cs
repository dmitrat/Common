using System;
using System.Collections.Generic;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Serialization;
using OutWit.Common.Settings.Tests.Utils;

namespace OutWit.Common.Settings.Tests.Serialization
{
    [TestFixture]
    public class SettingsSerializerTests
    {
        #region String Tests

        [Test]
        public void StringParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerString();

            Assert.That(serializer.ValueKind, Is.EqualTo("String"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(string)));

            Assert.That(serializer.Parse("hello", ""), Is.EqualTo("hello"));
            Assert.That(serializer.Format("hello"), Is.EqualTo("hello"));
            Assert.That(serializer.Parse("", ""), Is.EqualTo(""));
        }

        [Test]
        public void StringAreEqualTest()
        {
            var serializer = new SettingsSerializerString();

            Assert.That(serializer.AreEqual("abc", "abc"), Is.True);
            Assert.That(serializer.AreEqual("abc", "def"), Is.False);
            Assert.That(serializer.AreEqual("", ""), Is.True);
            Assert.That(serializer.AreEqual("abc", "ABC"), Is.False);
        }

        [Test]
        public void StringPreservesSpecialCharactersTest()
        {
            var serializer = new SettingsSerializerString();

            Assert.That(serializer.Parse("hello world", ""), Is.EqualTo("hello world"));
            Assert.That(serializer.Parse("path\\to\\file", ""), Is.EqualTo("path\\to\\file"));
            Assert.That(serializer.Parse("строка юникод", ""), Is.EqualTo("строка юникод"));
        }

        #endregion

        #region Integer Tests

        [Test]
        public void IntegerParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerInteger();

            Assert.That(serializer.ValueKind, Is.EqualTo("Integer"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(int)));

            Assert.That(serializer.Parse("42", ""), Is.EqualTo(42));
            Assert.That(serializer.Parse("-100", ""), Is.EqualTo(-100));
            Assert.That(serializer.Parse("0", ""), Is.EqualTo(0));
            Assert.That(serializer.Format(42), Is.EqualTo("42"));
            Assert.That(serializer.Format(-100), Is.EqualTo("-100"));
        }

        [Test]
        public void IntegerBoundaryValuesTest()
        {
            var serializer = new SettingsSerializerInteger();

            Assert.That(serializer.Parse(int.MaxValue.ToString(), ""), Is.EqualTo(int.MaxValue));
            Assert.That(serializer.Parse(int.MinValue.ToString(), ""), Is.EqualTo(int.MinValue));
            Assert.That(serializer.Format(int.MaxValue), Is.EqualTo("2147483647"));
            Assert.That(serializer.Format(int.MinValue), Is.EqualTo("-2147483648"));
        }

        [Test]
        public void IntegerAreEqualTest()
        {
            var serializer = new SettingsSerializerInteger();

            Assert.That(serializer.AreEqual(42, 42), Is.True);
            Assert.That(serializer.AreEqual(42, 43), Is.False);
            Assert.That(serializer.AreEqual(0, 0), Is.True);
            Assert.That(serializer.AreEqual(-1, -1), Is.True);
        }

        [Test]
        public void IntegerParseInvalidThrowsTest()
        {
            var serializer = new SettingsSerializerInteger();

            Assert.Throws<FormatException>(() => serializer.Parse("abc", ""));
            Assert.Throws<FormatException>(() => serializer.Parse("3.14", ""));
            Assert.Throws<OverflowException>(() => serializer.Parse("9999999999999", ""));
        }

        #endregion

        #region Long Tests

        [Test]
        public void LongParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerLong();

            Assert.That(serializer.ValueKind, Is.EqualTo("Long"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(long)));

            Assert.That(serializer.Parse("42", ""), Is.EqualTo(42L));
            Assert.That(serializer.Parse("-100", ""), Is.EqualTo(-100L));
            Assert.That(serializer.Parse("0", ""), Is.EqualTo(0L));
            Assert.That(serializer.Format(42L), Is.EqualTo("42"));
        }

        [Test]
        public void LongBoundaryValuesTest()
        {
            var serializer = new SettingsSerializerLong();

            Assert.That(serializer.Parse("9223372036854775807", ""), Is.EqualTo(long.MaxValue));
            Assert.That(serializer.Parse("-9223372036854775808", ""), Is.EqualTo(long.MinValue));
            Assert.That(serializer.Format(long.MaxValue), Is.EqualTo("9223372036854775807"));
            Assert.That(serializer.Format(long.MinValue), Is.EqualTo("-9223372036854775808"));
        }

        [Test]
        public void LongAreEqualTest()
        {
            var serializer = new SettingsSerializerLong();

            Assert.That(serializer.AreEqual(42L, 42L), Is.True);
            Assert.That(serializer.AreEqual(42L, 43L), Is.False);
            Assert.That(serializer.AreEqual(long.MaxValue, long.MaxValue), Is.True);
        }

        [Test]
        public void LongParseInvalidThrowsTest()
        {
            var serializer = new SettingsSerializerLong();

            Assert.Throws<FormatException>(() => serializer.Parse("abc", ""));
            Assert.Throws<FormatException>(() => serializer.Parse("3.14", ""));
        }

        #endregion

        #region Double Tests

        [Test]
        public void DoubleParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerDouble();

            Assert.That(serializer.ValueKind, Is.EqualTo("Double"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(double)));

            Assert.That(serializer.Parse("3.14", ""), Is.EqualTo(3.14));
            Assert.That(serializer.Parse("-2.5", ""), Is.EqualTo(-2.5));
            Assert.That(serializer.Parse("0", ""), Is.EqualTo(0.0));
            Assert.That(serializer.Format(3.14), Is.EqualTo("3.14"));
            Assert.That(serializer.Format(-2.5), Is.EqualTo("-2.5"));
        }

        [Test]
        public void DoubleInvariantCultureTest()
        {
            var serializer = new SettingsSerializerDouble();

            var formatted = serializer.Format(1234.56);
            Assert.That(formatted, Is.EqualTo("1234.56"));

            var parsed = serializer.Parse("1234.56", "");
            Assert.That(parsed, Is.EqualTo(1234.56));
        }

        [Test]
        public void DoubleAreEqualWithToleranceTest()
        {
            var serializer = new SettingsSerializerDouble();

            Assert.That(serializer.AreEqual(1.0, 1.0), Is.True);
            Assert.That(serializer.AreEqual(1.0, 1.0 + 1e-11), Is.True);
            Assert.That(serializer.AreEqual(1.0, 1.0 + 1e-9), Is.False);
            Assert.That(serializer.AreEqual(1.0, 2.0), Is.False);
            Assert.That(serializer.AreEqual(0.0, 0.0), Is.True);
            Assert.That(serializer.AreEqual(-1.5, -1.5), Is.True);
        }

        [Test]
        public void DoubleParseInvalidThrowsTest()
        {
            var serializer = new SettingsSerializerDouble();

            Assert.Throws<FormatException>(() => serializer.Parse("abc", ""));
            Assert.Throws<FormatException>(() => serializer.Parse("", ""));
        }

        #endregion

        #region Decimal Tests

        [Test]
        public void DecimalParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerDecimal();

            Assert.That(serializer.ValueKind, Is.EqualTo("Decimal"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(decimal)));

            Assert.That(serializer.Parse("123.456", ""), Is.EqualTo(123.456m));
            Assert.That(serializer.Parse("-99.99", ""), Is.EqualTo(-99.99m));
            Assert.That(serializer.Parse("0", ""), Is.EqualTo(0m));
            Assert.That(serializer.Format(123.456m), Is.EqualTo("123.456"));
            Assert.That(serializer.Format(0m), Is.EqualTo("0"));
        }

        [Test]
        public void DecimalHighPrecisionTest()
        {
            var serializer = new SettingsSerializerDecimal();

            var value = 0.123456789012345678901234567m;
            var formatted = serializer.Format(value);
            var parsed = serializer.Parse(formatted, "");

            Assert.That(parsed, Is.EqualTo(value));
        }

        [Test]
        public void DecimalAreEqualTest()
        {
            var serializer = new SettingsSerializerDecimal();

            Assert.That(serializer.AreEqual(1.0m, 1.0m), Is.True);
            Assert.That(serializer.AreEqual(1.0m, 1.1m), Is.False);
            Assert.That(serializer.AreEqual(0m, 0m), Is.True);
            Assert.That(serializer.AreEqual(decimal.MaxValue, decimal.MaxValue), Is.True);
        }

        [Test]
        public void DecimalParseInvalidThrowsTest()
        {
            var serializer = new SettingsSerializerDecimal();

            Assert.Throws<FormatException>(() => serializer.Parse("abc", ""));
            Assert.Throws<FormatException>(() => serializer.Parse("", ""));
        }

        #endregion

        #region Boolean Tests

        [Test]
        public void BooleanParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerBoolean();

            Assert.That(serializer.ValueKind, Is.EqualTo("Boolean"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(bool)));

            Assert.That(serializer.Parse("True", ""), Is.True);
            Assert.That(serializer.Parse("False", ""), Is.False);
            Assert.That(serializer.Format(true), Is.EqualTo("True"));
            Assert.That(serializer.Format(false), Is.EqualTo("False"));
        }

        [Test]
        public void BooleanAreEqualTest()
        {
            var serializer = new SettingsSerializerBoolean();

            Assert.That(serializer.AreEqual(true, true), Is.True);
            Assert.That(serializer.AreEqual(false, false), Is.True);
            Assert.That(serializer.AreEqual(true, false), Is.False);
            Assert.That(serializer.AreEqual(false, true), Is.False);
        }

        [Test]
        public void BooleanParseInvalidThrowsTest()
        {
            var serializer = new SettingsSerializerBoolean();

            Assert.Throws<FormatException>(() => serializer.Parse("abc", ""));
            Assert.Throws<FormatException>(() => serializer.Parse("1", ""));
            Assert.Throws<FormatException>(() => serializer.Parse("", ""));
        }

        #endregion

        #region DateTime Tests

        [Test]
        public void DateTimeParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerDateTime();

            Assert.That(serializer.ValueKind, Is.EqualTo("DateTime"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(DateTime)));

            var dt = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
            var formatted = serializer.Format(dt);
            var parsed = serializer.Parse(formatted, "");

            Assert.That(parsed, Is.EqualTo(dt));
            Assert.That(parsed.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void DateTimePreservesLocalKindTest()
        {
            var serializer = new SettingsSerializerDateTime();

            var local = new DateTime(2025, 3, 20, 14, 0, 0, DateTimeKind.Local);
            var formatted = serializer.Format(local);
            var parsed = serializer.Parse(formatted, "");

            Assert.That(parsed, Is.EqualTo(local));
            Assert.That(parsed.Kind, Is.EqualTo(DateTimeKind.Local));
        }

        [Test]
        public void DateTimeFormatUsesRoundTripFormatTest()
        {
            var serializer = new SettingsSerializerDateTime();
            var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var formatted = serializer.Format(dt);
            Assert.That(formatted, Does.Contain("2025-01-01"));
            Assert.That(formatted, Does.EndWith("Z"));
        }

        [Test]
        public void DateTimeAreEqualTest()
        {
            var serializer = new SettingsSerializerDateTime();
            var dt = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);

            Assert.That(serializer.AreEqual(dt, dt), Is.True);
            Assert.That(serializer.AreEqual(dt, dt.AddSeconds(1)), Is.False);
            Assert.That(serializer.AreEqual(DateTime.MinValue, DateTime.MinValue), Is.True);
        }

        [Test]
        public void DateTimeParseInvalidThrowsTest()
        {
            var serializer = new SettingsSerializerDateTime();

            Assert.Throws<FormatException>(() => serializer.Parse("not-a-date", ""));
            Assert.Throws<FormatException>(() => serializer.Parse("", ""));
        }

        #endregion

        #region TimeSpan Tests

        [Test]
        public void TimeSpanParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerTimeSpan();

            Assert.That(serializer.ValueKind, Is.EqualTo("TimeSpan"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(TimeSpan)));

            var span = TimeSpan.FromHours(1.5);
            var formatted = serializer.Format(span);
            var parsed = serializer.Parse(formatted, "");

            Assert.That(parsed, Is.EqualTo(span));
        }

        [Test]
        public void TimeSpanVariousValuesTest()
        {
            var serializer = new SettingsSerializerTimeSpan();

            var zero = TimeSpan.Zero;
            Assert.That(serializer.Parse(serializer.Format(zero), ""), Is.EqualTo(zero));

            var negative = TimeSpan.FromMinutes(-30);
            Assert.That(serializer.Parse(serializer.Format(negative), ""), Is.EqualTo(negative));

            var days = TimeSpan.FromDays(7);
            Assert.That(serializer.Parse(serializer.Format(days), ""), Is.EqualTo(days));

            var complex = new TimeSpan(1, 2, 3, 4, 500);
            Assert.That(serializer.Parse(serializer.Format(complex), ""), Is.EqualTo(complex));
        }

        [Test]
        public void TimeSpanAreEqualTest()
        {
            var serializer = new SettingsSerializerTimeSpan();

            var span = TimeSpan.FromHours(1.5);
            Assert.That(serializer.AreEqual(span, span), Is.True);
            Assert.That(serializer.AreEqual(span, TimeSpan.Zero), Is.False);
            Assert.That(serializer.AreEqual(TimeSpan.Zero, TimeSpan.Zero), Is.True);
        }

        [Test]
        public void TimeSpanFormatUsesConstantFormatTest()
        {
            var serializer = new SettingsSerializerTimeSpan();

            var span = new TimeSpan(1, 2, 3, 4, 500);
            var formatted = serializer.Format(span);

            Assert.That(formatted, Is.EqualTo("1.02:03:04.5000000"));
        }

        [Test]
        public void TimeSpanParseInvalidThrowsTest()
        {
            var serializer = new SettingsSerializerTimeSpan();

            Assert.Throws<FormatException>(() => serializer.Parse("not-a-timespan", ""));
        }

        #endregion

        #region Guid Tests

        [Test]
        public void GuidParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerGuid();

            Assert.That(serializer.ValueKind, Is.EqualTo("Guid"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(Guid)));

            var guid = Guid.NewGuid();
            var formatted = serializer.Format(guid);
            var parsed = serializer.Parse(formatted, "");

            Assert.That(parsed, Is.EqualTo(guid));
        }

        [Test]
        public void GuidFormatUsesDashFormatTest()
        {
            var serializer = new SettingsSerializerGuid();
            var guid = Guid.Empty;

            Assert.That(serializer.Format(guid), Is.EqualTo("00000000-0000-0000-0000-000000000000"));
        }

        [Test]
        public void GuidParsesVariousFormatsTest()
        {
            var serializer = new SettingsSerializerGuid();
            var expected = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            Assert.That(serializer.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890", ""), Is.EqualTo(expected));
            Assert.That(serializer.Parse("{a1b2c3d4-e5f6-7890-abcd-ef1234567890}", ""), Is.EqualTo(expected));
            Assert.That(serializer.Parse("a1b2c3d4e5f67890abcdef1234567890", ""), Is.EqualTo(expected));
        }

        [Test]
        public void GuidAreEqualTest()
        {
            var serializer = new SettingsSerializerGuid();
            var guid = Guid.NewGuid();

            Assert.That(serializer.AreEqual(guid, guid), Is.True);
            Assert.That(serializer.AreEqual(guid, Guid.NewGuid()), Is.False);
            Assert.That(serializer.AreEqual(Guid.Empty, Guid.Empty), Is.True);
        }

        [Test]
        public void GuidParseInvalidThrowsTest()
        {
            var serializer = new SettingsSerializerGuid();

            Assert.Throws<FormatException>(() => serializer.Parse("not-a-guid", ""));
            Assert.Throws<FormatException>(() => serializer.Parse("", ""));
        }

        #endregion

        #region Enum Tests

        [Test]
        public void EnumParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerEnum();
            var tag = typeof(TestEnum).AssemblyQualifiedName!;

            Assert.That(serializer.ValueKind, Is.EqualTo("Enum"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(object)));

            var parsed = serializer.Parse("Beta", tag);
            Assert.That(parsed, Is.EqualTo(TestEnum.Beta));

            var formatted = serializer.Format(TestEnum.Beta);
            Assert.That(formatted, Is.EqualTo("Beta"));
        }

        [Test]
        public void EnumParsesAllValuesTest()
        {
            var serializer = new SettingsSerializerEnum();
            var tag = typeof(TestEnum).AssemblyQualifiedName!;

            Assert.That(serializer.Parse("None", tag), Is.EqualTo(TestEnum.None));
            Assert.That(serializer.Parse("Alpha", tag), Is.EqualTo(TestEnum.Alpha));
            Assert.That(serializer.Parse("Beta", tag), Is.EqualTo(TestEnum.Beta));
            Assert.That(serializer.Parse("Gamma", tag), Is.EqualTo(TestEnum.Gamma));
        }

        [Test]
        public void EnumAreEqualTest()
        {
            var serializer = new SettingsSerializerEnum();

            Assert.That(serializer.AreEqual(TestEnum.Alpha, TestEnum.Alpha), Is.True);
            Assert.That(serializer.AreEqual(TestEnum.Alpha, TestEnum.Beta), Is.False);
            Assert.That(serializer.AreEqual(TestEnum.None, TestEnum.None), Is.True);
        }

        [Test]
        public void EnumParseInvalidTypeThrowsTest()
        {
            var serializer = new SettingsSerializerEnum();

            Assert.Throws<ArgumentException>(() => serializer.Parse("Value", "NonExistent.Type, Foo"));
        }

        [Test]
        public void EnumParseNonEnumTypeThrowsTest()
        {
            var serializer = new SettingsSerializerEnum();
            var tag = typeof(int).AssemblyQualifiedName!;

            Assert.Throws<ArgumentException>(() => serializer.Parse("42", tag));
        }

        [Test]
        public void EnumParseInvalidValueThrowsTest()
        {
            var serializer = new SettingsSerializerEnum();
            var tag = typeof(TestEnum).AssemblyQualifiedName!;

            Assert.Throws<ArgumentException>(() => serializer.Parse("NonExistentValue", tag));
        }

        #endregion

        #region EnumList Tests

        [Test]
        public void EnumListParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerEnumList();
            var tag = typeof(TestEnum).AssemblyQualifiedName!;

            Assert.That(serializer.ValueKind, Is.EqualTo("EnumList"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(IReadOnlyList<object>)));

            var parsed = serializer.Parse("Alpha, Beta, Gamma", tag);
            Assert.That(parsed, Has.Count.EqualTo(3));
            Assert.That(parsed[0], Is.EqualTo(TestEnum.Alpha));
            Assert.That(parsed[1], Is.EqualTo(TestEnum.Beta));
            Assert.That(parsed[2], Is.EqualTo(TestEnum.Gamma));

            var formatted = serializer.Format(new List<object> { TestEnum.Alpha, TestEnum.Beta });
            Assert.That(formatted, Is.EqualTo("Alpha, Beta"));
        }

        [Test]
        public void EnumListParseSingleElementTest()
        {
            var serializer = new SettingsSerializerEnumList();
            var tag = typeof(TestEnum).AssemblyQualifiedName!;

            var parsed = serializer.Parse("Beta", tag);
            Assert.That(parsed, Has.Count.EqualTo(1));
            Assert.That(parsed[0], Is.EqualTo(TestEnum.Beta));
        }

        [Test]
        public void EnumListParseEmptyReturnsEmptyListTest()
        {
            var serializer = new SettingsSerializerEnumList();
            var tag = typeof(TestEnum).AssemblyQualifiedName!;

            Assert.That(serializer.Parse("", tag), Is.Empty);
            Assert.That(serializer.Parse(null!, tag), Is.Empty);
        }

        [Test]
        public void EnumListAreEqualTest()
        {
            var serializer = new SettingsSerializerEnumList();

            Assert.That(serializer.AreEqual(
                new List<object> { TestEnum.Alpha, TestEnum.Beta },
                new List<object> { TestEnum.Alpha, TestEnum.Beta }), Is.True);

            Assert.That(serializer.AreEqual(
                new List<object> { TestEnum.Alpha },
                new List<object> { TestEnum.Beta }), Is.False);
        }

        [Test]
        public void EnumListAreEqualNullAndDifferentLengthTest()
        {
            var serializer = new SettingsSerializerEnumList();
            var list = new List<object> { TestEnum.Alpha };

            Assert.That(serializer.AreEqual(null!, null!), Is.True);
            Assert.That(serializer.AreEqual(list, null!), Is.False);
            Assert.That(serializer.AreEqual(null!, list), Is.False);

            Assert.That(serializer.AreEqual(
                new List<object> { TestEnum.Alpha },
                new List<object> { TestEnum.Alpha, TestEnum.Beta }), Is.False);
        }

        [Test]
        public void EnumListAreEqualSameReferenceTest()
        {
            var serializer = new SettingsSerializerEnumList();
            var list = new List<object> { TestEnum.Alpha, TestEnum.Beta };

            Assert.That(serializer.AreEqual(list, list), Is.True);
        }

        [Test]
        public void EnumListAreEqualDifferentOrderReturnsFalseTest()
        {
            var serializer = new SettingsSerializerEnumList();

            Assert.That(serializer.AreEqual(
                new List<object> { TestEnum.Alpha, TestEnum.Beta },
                new List<object> { TestEnum.Beta, TestEnum.Alpha }), Is.False);
        }

        [Test]
        public void EnumListParseInvalidTypeThrowsTest()
        {
            var serializer = new SettingsSerializerEnumList();

            Assert.Throws<ArgumentException>(() => serializer.Parse("Alpha", "NonExistent.Type, Foo"));
        }

        [Test]
        public void EnumListParseInvalidValueThrowsTest()
        {
            var serializer = new SettingsSerializerEnumList();
            var tag = typeof(TestEnum).AssemblyQualifiedName!;

            Assert.Throws<ArgumentException>(() => serializer.Parse("Alpha, Invalid, Beta", tag));
        }

        #endregion

        #region StringList Tests

        [Test]
        public void StringListParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerStringList();

            Assert.That(serializer.ValueKind, Is.EqualTo("StringList"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(IReadOnlyList<string>)));

            var parsed = serializer.Parse("a, b, c", "");
            Assert.That(parsed, Is.EqualTo(new List<string> { "a", "b", "c" }));

            var formatted = serializer.Format(new List<string> { "a", "b", "c" });
            Assert.That(formatted, Is.EqualTo("a, b, c"));
        }

        [Test]
        public void StringListParseSingleElementTest()
        {
            var serializer = new SettingsSerializerStringList();

            var parsed = serializer.Parse("single", "");
            Assert.That(parsed, Has.Count.EqualTo(1));
            Assert.That(parsed[0], Is.EqualTo("single"));
        }

        [Test]
        public void StringListParseTrimsWhitespaceTest()
        {
            var serializer = new SettingsSerializerStringList();

            var parsed = serializer.Parse("  a  ,  b  ,  c  ", "");
            Assert.That(parsed, Is.EqualTo(new List<string> { "a", "b", "c" }));
        }

        [Test]
        public void StringListParseEmptyReturnsEmptyListTest()
        {
            var serializer = new SettingsSerializerStringList();

            Assert.That(serializer.Parse("", ""), Is.Empty);
            Assert.That(serializer.Parse(null!, ""), Is.Empty);
        }

        [Test]
        public void StringListAreEqualTest()
        {
            var serializer = new SettingsSerializerStringList();

            Assert.That(serializer.AreEqual(
                new List<string> { "a", "b" },
                new List<string> { "a", "b" }), Is.True);

            Assert.That(serializer.AreEqual(
                new List<string> { "a" },
                new List<string> { "b" }), Is.False);
        }

        [Test]
        public void StringListAreEqualNullAndDifferentLengthTest()
        {
            var serializer = new SettingsSerializerStringList();
            var list = new List<string> { "a" };

            Assert.That(serializer.AreEqual(null!, null!), Is.True);
            Assert.That(serializer.AreEqual(list, null!), Is.False);
            Assert.That(serializer.AreEqual(null!, list), Is.False);

            Assert.That(serializer.AreEqual(
                new List<string> { "a" },
                new List<string> { "a", "b" }), Is.False);
        }

        [Test]
        public void StringListAreEqualSameReferenceTest()
        {
            var serializer = new SettingsSerializerStringList();
            var list = new List<string> { "a", "b" };

            Assert.That(serializer.AreEqual(list, list), Is.True);
        }

        #endregion

        #region IntegerList Tests

        [Test]
        public void IntegerListParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerIntegerList();

            Assert.That(serializer.ValueKind, Is.EqualTo("IntegerList"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(IReadOnlyList<int>)));

            var parsed = serializer.Parse("1, 2, 3", "");
            Assert.That(parsed, Is.EqualTo(new List<int> { 1, 2, 3 }));

            var formatted = serializer.Format(new List<int> { 1, 2, 3 });
            Assert.That(formatted, Is.EqualTo("1, 2, 3"));
        }

        [Test]
        public void IntegerListParseSingleElementTest()
        {
            var serializer = new SettingsSerializerIntegerList();

            var parsed = serializer.Parse("42", "");
            Assert.That(parsed, Has.Count.EqualTo(1));
            Assert.That(parsed[0], Is.EqualTo(42));
        }

        [Test]
        public void IntegerListParseTrimsWhitespaceTest()
        {
            var serializer = new SettingsSerializerIntegerList();

            var parsed = serializer.Parse("  1  ,  2  ,  3  ", "");
            Assert.That(parsed, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }

        [Test]
        public void IntegerListParseEmptyReturnsEmptyListTest()
        {
            var serializer = new SettingsSerializerIntegerList();

            Assert.That(serializer.Parse("", ""), Is.Empty);
            Assert.That(serializer.Parse(null!, ""), Is.Empty);
        }

        [Test]
        public void IntegerListAreEqualTest()
        {
            var serializer = new SettingsSerializerIntegerList();

            Assert.That(serializer.AreEqual(
                new List<int> { 1, 2 },
                new List<int> { 1, 2 }), Is.True);

            Assert.That(serializer.AreEqual(
                new List<int> { 1 },
                new List<int> { 2 }), Is.False);
        }

        [Test]
        public void IntegerListAreEqualNullAndDifferentLengthTest()
        {
            var serializer = new SettingsSerializerIntegerList();
            var list = new List<int> { 1 };

            Assert.That(serializer.AreEqual(null!, null!), Is.True);
            Assert.That(serializer.AreEqual(list, null!), Is.False);
            Assert.That(serializer.AreEqual(null!, list), Is.False);

            Assert.That(serializer.AreEqual(
                new List<int> { 1 },
                new List<int> { 1, 2 }), Is.False);
        }

        [Test]
        public void IntegerListAreEqualSameReferenceTest()
        {
            var serializer = new SettingsSerializerIntegerList();
            var list = new List<int> { 1, 2, 3 };

            Assert.That(serializer.AreEqual(list, list), Is.True);
        }

        [Test]
        public void IntegerListParseInvalidElementThrowsTest()
        {
            var serializer = new SettingsSerializerIntegerList();

            Assert.Throws<FormatException>(() => serializer.Parse("1, abc, 3", ""));
        }

        #endregion

        #region DoubleList Tests

        [Test]
        public void DoubleListParseFormatRoundTripTest()
        {
            var serializer = new SettingsSerializerDoubleList();

            Assert.That(serializer.ValueKind, Is.EqualTo("DoubleList"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(IReadOnlyList<double>)));

            var parsed = serializer.Parse("1.5, 2.5, 3.5", "");
            Assert.That(parsed, Is.EqualTo(new List<double> { 1.5, 2.5, 3.5 }));

            var formatted = serializer.Format(new List<double> { 1.5, 2.5, 3.5 });
            Assert.That(formatted, Is.EqualTo("1.5, 2.5, 3.5"));
        }

        [Test]
        public void DoubleListParseSingleElementTest()
        {
            var serializer = new SettingsSerializerDoubleList();

            var parsed = serializer.Parse("3.14", "");
            Assert.That(parsed, Has.Count.EqualTo(1));
            Assert.That(parsed[0], Is.EqualTo(3.14));
        }

        [Test]
        public void DoubleListParseEmptyReturnsEmptyListTest()
        {
            var serializer = new SettingsSerializerDoubleList();

            Assert.That(serializer.Parse("", ""), Is.Empty);
            Assert.That(serializer.Parse(null!, ""), Is.Empty);
        }

        [Test]
        public void DoubleListAreEqualTest()
        {
            var serializer = new SettingsSerializerDoubleList();

            Assert.That(serializer.AreEqual(
                new List<double> { 1.0, 2.0 },
                new List<double> { 1.0, 2.0 }), Is.True);

            Assert.That(serializer.AreEqual(
                new List<double> { 1.0 },
                new List<double> { 2.0 }), Is.False);
        }

        [Test]
        public void DoubleListAreEqualWithToleranceTest()
        {
            var serializer = new SettingsSerializerDoubleList();

            Assert.That(serializer.AreEqual(
                new List<double> { 1.0, 2.0 },
                new List<double> { 1.0 + 1e-11, 2.0 + 1e-11 }), Is.True);

            Assert.That(serializer.AreEqual(
                new List<double> { 1.0 },
                new List<double> { 1.0 + 1e-9 }), Is.False);
        }

        [Test]
        public void DoubleListAreEqualNullAndDifferentLengthTest()
        {
            var serializer = new SettingsSerializerDoubleList();
            var list = new List<double> { 1.0 };

            Assert.That(serializer.AreEqual(null!, null!), Is.True);
            Assert.That(serializer.AreEqual(list, null!), Is.False);
            Assert.That(serializer.AreEqual(null!, list), Is.False);

            Assert.That(serializer.AreEqual(
                new List<double> { 1.0 },
                new List<double> { 1.0, 2.0 }), Is.False);
        }

        [Test]
        public void DoubleListAreEqualSameReferenceTest()
        {
            var serializer = new SettingsSerializerDoubleList();
            var list = new List<double> { 1.0, 2.0 };

            Assert.That(serializer.AreEqual(list, list), Is.True);
        }

        [Test]
        public void DoubleListParseInvalidElementThrowsTest()
        {
            var serializer = new SettingsSerializerDoubleList();

            Assert.Throws<FormatException>(() => serializer.Parse("1.0, abc, 3.0", ""));
        }

        #endregion

        #region Alias Tests

        [Test]
        public void UrlSerializerHasCorrectValueKindAndTypeTest()
        {
            var serializer = new SettingsSerializerUrl();

            Assert.That(serializer.ValueKind, Is.EqualTo("Url"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(string)));
            Assert.That(serializer.Parse("https://example.com", ""), Is.EqualTo("https://example.com"));
            Assert.That(serializer.Format("https://example.com"), Is.EqualTo("https://example.com"));
        }

        [Test]
        public void ServiceUrlSerializerHasCorrectValueKindAndTypeTest()
        {
            var serializer = new SettingsSerializerServiceUrl();

            Assert.That(serializer.ValueKind, Is.EqualTo("ServiceUrl"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(string)));
            Assert.That(serializer.Parse("http://api.local:8080", ""), Is.EqualTo("http://api.local:8080"));
            Assert.That(serializer.Format("http://api.local:8080"), Is.EqualTo("http://api.local:8080"));
        }

        [Test]
        public void PathSerializerHasCorrectValueKindAndTypeTest()
        {
            var serializer = new SettingsSerializerPath();

            Assert.That(serializer.ValueKind, Is.EqualTo("Path"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(string)));
            Assert.That(serializer.Parse("C:\\file.txt", ""), Is.EqualTo("C:\\file.txt"));
            Assert.That(serializer.Format("C:\\file.txt"), Is.EqualTo("C:\\file.txt"));
        }

        [Test]
        public void LanguageSerializerHasCorrectValueKindAndTypeTest()
        {
            var serializer = new SettingsSerializerLanguage();

            Assert.That(serializer.ValueKind, Is.EqualTo("Language"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(string)));
            Assert.That(serializer.Parse("en-US", ""), Is.EqualTo("en-US"));
            Assert.That(serializer.Format("en-US"), Is.EqualTo("en-US"));
        }

        [Test]
        public void PasswordSerializerHasCorrectValueKindAndTypeTest()
        {
            var serializer = new SettingsSerializerPassword();

            Assert.That(serializer.ValueKind, Is.EqualTo("Password"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(string)));
            Assert.That(serializer.Parse("secret123", ""), Is.EqualTo("secret123"));
            Assert.That(serializer.Format("secret123"), Is.EqualTo("secret123"));
        }

        [Test]
        public void FolderSerializerHasCorrectValueKindAndTypeTest()
        {
            var serializer = new SettingsSerializerFolder();

            Assert.That(serializer.ValueKind, Is.EqualTo("Folder"));
            Assert.That(serializer.ValueType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void FolderSerializerExpandsEnvironmentVariablesTest()
        {
            var serializer = new SettingsSerializerFolder();

            var parsed = serializer.Parse("%APPDATA%\\MyApp", "");
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            Assert.That(parsed, Is.EqualTo($"{appData}\\MyApp"));
        }

        [Test]
        public void FolderSerializerCollapsesAppDataOnFormatTest()
        {
            var serializer = new SettingsSerializerFolder();
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var formatted = serializer.Format($"{appData}\\MyApp");

            Assert.That(formatted, Is.EqualTo("%APPDATA%\\MyApp"));
        }

        [Test]
        public void FolderSerializerCollapsesProgramDataOnFormatTest()
        {
            var serializer = new SettingsSerializerFolder();
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            if (!string.IsNullOrEmpty(programData))
            {
                var formatted = serializer.Format($"{programData}\\MyApp");
                Assert.That(formatted, Is.EqualTo("%PROGRAMDATA%\\MyApp"));
            }
        }

        [Test]
        public void FolderSerializerPassesThroughRegularPathTest()
        {
            var serializer = new SettingsSerializerFolder();

            Assert.That(serializer.Format("C:\\SomePath\\MyApp"), Is.EqualTo("C:\\SomePath\\MyApp"));
        }

        #endregion

        #region Interface Boxing Tests

        [Test]
        public void InterfaceBoxingIntegerTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerInteger();

            var deserialized = serializer.Deserialize("42", "");
            Assert.That(deserialized, Is.EqualTo(42));

            var serialized = serializer.Serialize(42);
            Assert.That(serialized, Is.EqualTo("42"));

            Assert.That(serializer.AreEqual(42, 42), Is.True);
            Assert.That(serializer.AreEqual(42, 43), Is.False);
        }

        [Test]
        public void InterfaceBoxingBooleanTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerBoolean();

            Assert.That(serializer.Deserialize("True", ""), Is.EqualTo(true));
            Assert.That(serializer.Serialize(false), Is.EqualTo("False"));
            Assert.That(serializer.AreEqual(true, true), Is.True);
            Assert.That(serializer.AreEqual(true, false), Is.False);
        }

        [Test]
        public void InterfaceBoxingDoubleTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerDouble();

            Assert.That(serializer.Deserialize("3.14", ""), Is.EqualTo(3.14));
            Assert.That(serializer.Serialize(3.14), Is.EqualTo("3.14"));
            Assert.That(serializer.AreEqual(1.0, 1.0), Is.True);
        }

        [Test]
        public void InterfaceBoxingDateTimeTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerDateTime();
            var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var formatted = serializer.Serialize(dt);
            var parsed = serializer.Deserialize(formatted, "");
            Assert.That(parsed, Is.EqualTo(dt));
            Assert.That(serializer.AreEqual(dt, dt), Is.True);
        }

        [Test]
        public void InterfaceBoxingGuidTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerGuid();
            var guid = Guid.NewGuid();

            var formatted = serializer.Serialize(guid);
            var parsed = serializer.Deserialize(formatted, "");
            Assert.That(parsed, Is.EqualTo(guid));
        }

        [Test]
        public void InterfaceBoxingStringListTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerStringList();
            var list = (IReadOnlyList<string>)new List<string> { "a", "b" };

            var formatted = serializer.Serialize(list);
            Assert.That(formatted, Is.EqualTo("a, b"));

            var parsed = serializer.Deserialize("x, y", "");
            Assert.That(parsed, Is.EqualTo(new List<string> { "x", "y" }));
        }

        [Test]
        public void InterfaceBoxingAreEqualMismatchedTypesReturnsFalseTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerInteger();

            Assert.That(serializer.AreEqual(42, "42"), Is.False);
            Assert.That(serializer.AreEqual("hello", 42), Is.False);
        }

        [Test]
        public void InterfaceBoxingAreEqualSameReferenceReturnsTrueTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerStringList();
            var list = (object)new List<string> { "a" };

            Assert.That(serializer.AreEqual(list, list), Is.True);
        }

        [Test]
        public void InterfaceSerializeWrongTypeReturnsEmptyTest()
        {
            ISettingsSerializer serializer = new SettingsSerializerInteger();

            Assert.That(serializer.Serialize("not an int"), Is.EqualTo(""));
        }

        #endregion
    }
}
