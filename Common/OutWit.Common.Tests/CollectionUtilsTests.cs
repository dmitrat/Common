using NUnit.Framework;
using OutWit.Common.Collections; 
using OutWit.Common.Abstract; 
using System.Collections;
using System.Collections.ObjectModel;
using OutWit.Common.Tests.Mock;

namespace OutWit.Common.Tests 
{

    [TestFixture]
    public class CollectionUtilsTests
    {
        #region Test Data

        private readonly TestModel _modelA1 = new() { Id = 1, Name = "A", Value = 10.0 };
        private readonly TestModel _modelA1_Clone = new() { Id = 1, Name = "A", Value = 10.0 };
        private readonly TestModel _modelA1_Almost = new() { Id = 1, Name = "A", Value = 10.00000001 };
        private readonly TestModel _modelB2 = new() { Id = 2, Name = "B", Value = 20.0 };

        #endregion

        #region ForEach Tests

        [Test]
        public void ForEach_ShouldIterateAllItems_WithCorrectIndex()
        {
            // Arrange
            var source = new List<string> { "a", "b", "c" };
            var results = new List<(string item, int index)>();

            // Act
            source.ForEach((item, index) => results.Add((item, index)));

            // Assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0], Is.EqualTo(("a", 0)));
            Assert.That(results[1], Is.EqualTo(("b", 1)));
            Assert.That(results[2], Is.EqualTo(("c", 2)));
        }

        #endregion

        #region Is (Equality) Tests

        [Test]
        public void Is_IEnumerable_WhenSequencesAreEqual_ShouldReturnTrue()
        {
            var list1 = new List<int> { 1, 2, 3 };
            var list2 = new List<int> { 1, 2, 3 };
            Assert.That(list1.Is(list2), Is.True);
        }

        [Test]
        public void Is_IDictionary_WhenDictionariesHaveDifferentElementOrder_ShouldReturnTrue()
        {
            var dict1 = new Dictionary<int, string> { { 1, "A" }, { 2, "B" } };
            var dict2 = new Dictionary<int, string> { { 2, "B" }, { 1, "A" } };
            Assert.That(dict1.Is(dict2), Is.True);
        }

        [Test]
        public void Is_IEnumerable_WithModelBase_ShouldUseModelsIsMethodForComparison()
        {
            var list1 = new List<ModelBase> { _modelA1, _modelB2 };
            var list2 = new List<ModelBase> { _modelA1_Clone, _modelB2 };
            var list3 = new List<ModelBase> { _modelA1_Almost, _modelB2 };

            Assert.That(list1.Is(list2), Is.True, "Clones should be considered equal.");
            Assert.That(list1.Is(list3), Is.True, "Objects within tolerance should be considered equal.");
        }

        [Test]
        public void Check_ForNonGenericIDictionary_ShouldCompareCorrectly()
        {
            // Arrange
            var ht1 = new Hashtable { { 1, "A" }, { "key", 123 } };
            var ht2 = new Hashtable { { "key", 123 }, { 1, "A" } }; // Same data, different order
            var ht3 = new Hashtable { { 1, "B" }, { "key", 123 } }; // Different data

            // Act & Assert
            Assert.That(CollectionUtils.Check(ht1, ht2), Is.True, "Hashtables with same content should be equal.");
            Assert.That(CollectionUtils.Check(ht1, ht3), Is.False, "Hashtables with different content should not be equal.");
            Assert.That(CollectionUtils.Check(ht1, null), Is.False);
        }

        #endregion

        #region Split Tests

        [Test]
        public void Split_WhenArrayDividesEvenly_ShouldCreateCorrectChunks()
        {
            // Arrange
            var source = Enumerable.Range(0, 10).ToArray(); // [0, 1, .. 9]
            const int chunksCount = 5;

            // Act
            var result = source.Split(chunksCount);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(chunksCount));
            Assert.That(result.ElementAt(0), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.ElementAt(4), Is.EqualTo(new[] { 8, 9 }));
        }

        [Test]
        public void Split_WhenArrayDoesNotDivideEvenly_ShouldCreateCorrectChunks()
        {
            // Arrange
            var source = Enumerable.Range(0, 10).ToArray(); // 10 elements
            const int chunksCount = 3; // Expected sizes: 4, 4, 2

            // Act
            var result = source.Split(chunksCount);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(chunksCount));
            Assert.That(result.ElementAt(0).Length, Is.EqualTo(4), "First chunk size should be 4");
            Assert.That(result.ElementAt(1).Length, Is.EqualTo(4), "Second chunk size should be 4");
            Assert.That(result.ElementAt(2).Length, Is.EqualTo(2), "Third chunk size should be 2");
            Assert.That(result.ElementAt(2), Is.EqualTo(new[] { 8, 9 }));
        }

        #endregion

        #region TryGetValue Tests

        [Test]
        public void TryGetValue_ForIReadOnlyDictionary_WhenKeyDoesNotExist_ReturnsDefault()
        {
            // Arrange
            IReadOnlyDictionary<string, int> dict = new Dictionary<string, int>();

            // Act
            var value = dict.TryGetValue("test", -1);

            // Assert
            Assert.That(value, Is.EqualTo(-1));
        }

        #endregion

        #region AddOrUpdate Tests

        [Test]
        public void AddOrUpdate_WhenKeyDoesNotExist_ShouldAddNewItem()
        {
            // Arrange
            var dict = new Dictionary<int, string>();

            // Act
            var wasAdded = dict.AddOrUpdate(1, "A");

            // Assert
            Assert.That(wasAdded, Is.True);
            Assert.That(dict.ContainsKey(1), Is.True);
            Assert.That(dict[1], Is.EqualTo("A"));
        }

        [Test]
        public void AddOrUpdate_WhenKeyExists_ShouldUpdateItem()
        {
            // Arrange
            var dict = new Dictionary<int, string> { { 1, "A" } };

            // Act
            var wasAdded = dict.AddOrUpdate(1, "B");

            // Assert
            Assert.That(wasAdded, Is.False);
            Assert.That(dict[1], Is.EqualTo("B"));
        }

        #endregion

        #region FindClosest Tests

        [Test]
        public void FindClosest_WhenValueIsInMiddle_ShouldFindClosestItem()
        {
            // Arrange
            var source = new List<TestModel>
        {
            new() { Id = 1, Value = 10 },
            new() { Id = 2, Value = 20 },
            new() { Id = 3, Value = 30 }
        };

            // Act
            var result = source.FindClosest(22, model => (int)model.Value);

            // Assert
            Assert.That(result.Id, Is.EqualTo(2));
        }

        #endregion

        #region ToObservable Tests

        [Test]
        public void ToObservable_FromNull_ReturnsEmptyCollection()
        {
            // Arrange
            List<int>? source = null;

            // Act
            var result = source.ToObservable();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ToObservable_FromExistingCollection_CopiesAllItems()
        {
            // Arrange
            var source = new List<string> { "a", "b" };

            // Act
            var result = source.ToObservable();

            // Assert
            Assert.That(result, Is.TypeOf<ObservableCollection<string>>());
            Assert.That(result, Is.EqualTo(source));
        }

        #endregion
    }
}