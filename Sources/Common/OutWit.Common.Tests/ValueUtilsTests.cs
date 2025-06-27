using NUnit.Framework;
using OutWit.Common.Values; 
using OutWit.Common.Abstract; 
using System;
using System.Collections.Generic;
using OutWit.Common.Tests.Mock;

namespace OutWit.Common.Tests
{


    [TestFixture]
    public class ValueUtilsTests
    {
        private readonly ModelBase _modelA1 = new TestModel { Id = 1, Name = "A", Value = 10.0 };
        private readonly ModelBase _modelA1_Clone = new TestModel { Id = 1, Name = "A", Value = 10.0 };
        private readonly ModelBase _modelB2 = new TestModel { Id = 2, Name = "B", Value = 20.0 };

        #region Is (Generic Comparison) Tests

        [Test]
        public void Is_Generic_ForEqualIComparableValues_ShouldReturnTrue()
        {
            Assert.That(5.Is(5), Is.True);
            Assert.That("hello".Is("hello"), Is.True);
            Assert.That(Guid.Empty.Is(Guid.Empty), Is.True);
        }

        [Test]
        public void Is_Generic_ForUnequalIComparableValues_ShouldReturnFalse()
        {
            Assert.That(5.Is(10), Is.False);
            Assert.That("hello".Is("world"), Is.False);
        }

        [Test]
        public void Is_ForNullableTypes_ShouldHandleNullsAndValues()
        {
            int? a = 10;
            int? b = 10;
            int? c = 20;
            int? d = null;
            int? e = null;

            Assert.That(a.Is(b), Is.True, "Two identical non-null values should be equal.");
            Assert.That(a.Is(c), Is.False, "Two different non-null values should not be equal.");
            Assert.That(a.Is(d), Is.False, "A non-null value and a null should not be equal.");
            Assert.That(d.Is(e), Is.True, "Two null values should be equal.");
        }

        [Test]
        public void Is_ForDateTime_ShouldCompareCorrectly()
        {
            var date1 = new DateTime(2025, 6, 6, 12, 0, 0, DateTimeKind.Utc);
            var date2 = new DateTime(2025, 6, 6, 12, 0, 0, DateTimeKind.Utc);
            var date3 = new DateTime(2025, 6, 6, 12, 0, 1, DateTimeKind.Utc);
            var date4_local = new DateTime(2025, 6, 6, 12, 0, 0, DateTimeKind.Local);

            Assert.That(date1.Is(date2), Is.True, "Identical DateTimes should be equal.");
            Assert.That(date1.Is(date3), Is.False, "Different DateTimes should not be equal.");
            Assert.That(date1.Is(date4_local), Is.False, "DateTimes with different Kind should not be equal.");
        }

        [Test]
        public void Is_ForTimeSpan_ShouldCompareCorrectly()
        {
            Assert.That(TimeSpan.FromHours(1).Is(TimeSpan.FromMinutes(60)), Is.True, "Equivalent TimeSpans should be equal.");
            Assert.That(TimeSpan.FromSeconds(1).Is(TimeSpan.FromSeconds(2)), Is.False, "Different TimeSpans should not be equal.");
        }

        
        #endregion

        #region Is (Floating-Point Comparison) Tests

        [Test]
        public void Is_ForFloatingPoint_WhenWithinTolerance_ShouldReturnTrue()
        {
            Assert.That(10.0d.Is(10.00000001d), Is.True);
            Assert.That(10.0f.Is(10.00000001f), Is.True);
            Assert.That(10.0m.Is(10.00000001m), Is.True);
        }

        [Test]
        public void Is_ForFloatingPoint_WhenOutsideTolerance_ShouldReturnFalse()
        {
            Assert.That(10.0d.Is(10.1d), Is.False);
            Assert.That(10.0f.Is(10.1f), Is.False);
            Assert.That(10.0m.Is(10.1m), Is.False);
        }

        #endregion

        #region Is (Special Cases) Tests

        [Test]
        public void Is_ForEnums_ShouldCompareCorrectly()
        {
            Assert.That(TestEnumOne.A.Is(TestEnumOne.A), Is.True, "Same enum members should be equal.");
            Assert.That(TestEnumOne.A.Is(TestEnumOne.B), Is.False, "Different enum members should not be equal.");

            // Using the central Check method to compare different enum types
            Assert.That(ValueUtils.Check(TestEnumOne.A, TestEnumTwo.A), Is.False, "Members of different enum types should not be equal.");
        }

        [Test]
        public void Is_ForTypes_ShouldCompareByAssemblyQualifiedName()
        {
            Assert.That(typeof(string).Is(typeof(string)), Is.True);
            Assert.That(typeof(string).Is(typeof(int)), Is.False);
        }


        #endregion

        #region Check (Dispatcher) Tests

        [Test]
        public void Check_WithModelBase_ShouldUseModelIsLogic()
        {
            Assert.That(_modelA1.Check(_modelA1_Clone), Is.True);
            Assert.That(_modelA1.Check(_modelB2), Is.False);
        }

        [Test]
        public void Check_WithIComparable_ShouldUseGenericIsLogic()
        {
            // This test verifies the `if (me is IComparable...` branch
            object a = 123;
            object b = 123;
            object c = 456;
            Assert.That(a.Check(b), Is.True);
            Assert.That(a.Check(c), Is.False);
        }

        [Test]
        public void Check_WithICollection_ShouldUseCollectionUtilsIs()
        {
            var list1 = new List<int> { 1, 2, 3 };
            var list2 = new List<int> { 1, 2, 3 };
            var list3 = new List<int> { 1, 2, 4 };

            // This test implicitly depends on CollectionUtils.Is
            Assert.That(list1.Check(list2), Is.True);
            Assert.That(list1.Check(list3), Is.False);
        }

        [Test]
        public void Check_WithFallbackToEquals_ShouldWorkCorrectly()
        {
            // This tests the final fallback to `object.Equals`
            var poco1 = new NonComparable { Id = 1, Name = "Test" };
            var poco2 = new NonComparable { Id = 1, Name = "Test" };
            var poco3 = new NonComparable { Id = 2, Name = "Test" };

            Assert.That(poco1.Check(poco2), Is.True);
            Assert.That(poco1.Check(poco3), Is.False);
        }

        [Test]
        public void Check_WithMismatchedTypes_ShouldReturnFalse()
        {
            // Test comparison between completely different types
            Assert.That("hello".Check(123), Is.False);
            Assert.That(new List<int>().Check(new Dictionary<int, int>()), Is.False);
        }

        #endregion

        #region Min/Max Tests

        [Test]
        public void MinMax_ForParams_ShouldReturnCorrectValue()
        {
            Assert.That(ValueUtils.Max(5, 1, 10, 3), Is.EqualTo(10));
            Assert.That(ValueUtils.Min(5, 1, 10, 3), Is.EqualTo(1));
        }

        [Test]
        public void MinMax_ForNullableValues_ShouldHandleNullsAndValues()
        {
            int? a = 10;
            int? b = 20;
            int? c = null;

            // Min
            Assert.That(ValueUtils.Min(a, b), Is.EqualTo(10));
            Assert.That(ValueUtils.Min(a, c), Is.EqualTo(10));
            Assert.That(ValueUtils.Min(c, b), Is.EqualTo(20));
            Assert.That(ValueUtils.Min(c, c), Is.Null);

            // Max
            Assert.That(ValueUtils.Max(a, b), Is.EqualTo(20));
            Assert.That(ValueUtils.Max(a, c), Is.EqualTo(10));
            Assert.That(ValueUtils.Max(c, b), Is.EqualTo(20));
            Assert.That(ValueUtils.Max(c, c), Is.Null);
        }

        #endregion
    }
}
