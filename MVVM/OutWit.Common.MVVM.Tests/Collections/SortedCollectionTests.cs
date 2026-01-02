using NUnit.Framework;
using OutWit.Common.MVVM.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OutWit.Common.MVVM.Tests.Collections
{
    [TestFixture]
    public class SortedCollectionTests
    {
        #region Test Data

        private class TestItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void ConstructorWithKeyGetterCreatesEmptyCollectionTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);

            Assert.That(collection.Count, Is.EqualTo(0));
        }

        [Test]
        public void ConstructorWithNullKeyGetterThrowsExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SortedCollection<int, TestItem>(null!));
        }

        [Test]
        public void ConstructorWithValuesPopulatesCollectionTest()
        {
            var items = new[] { new TestItem { Id = 1 }, new TestItem { Id = 2 } };

            var collection = new SortedCollection<int, TestItem>(x => x.Id, items);

            Assert.That(collection.Count, Is.EqualTo(2));
        }

        #endregion

        #region Add Tests

        [Test]
        public void AddSingleItemIncreasesCountTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);

            collection.Add(new TestItem { Id = 1 });

            Assert.That(collection.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddSingleItemRaisesItemsAddedEventTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            var eventRaised = false;
            IReadOnlyCollection<TestItem>? addedItems = null;

            collection.ItemsAdded += (_, items) =>
            {
                eventRaised = true;
                addedItems = items;
            };

            var item = new TestItem { Id = 1 };
            collection.Add(item);

            Assert.That(eventRaised, Is.True);
            Assert.That(addedItems, Is.Not.Null);
            Assert.That(addedItems!.Count, Is.EqualTo(1));
            Assert.That(addedItems.First(), Is.EqualTo(item));
        }

        [Test]
        public void AddNullItemThrowsExceptionTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);

            Assert.Throws<ArgumentNullException>(() => collection.Add((TestItem)null!));
        }

        [Test]
        public void AddDuplicateKeyThrowsExceptionTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            collection.Add(new TestItem { Id = 1 });

            Assert.Throws<ArgumentException>(() => collection.Add(new TestItem { Id = 1 }));
        }

        [Test]
        public void AddMultipleItemsIncreasesCountTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            var items = new[] { new TestItem { Id = 1 }, new TestItem { Id = 2 } };

            collection.Add(items);

            Assert.That(collection.Count, Is.EqualTo(2));
        }

        [Test]
        public void AddEmptyCollectionDoesNotChangeCountTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);

            collection.Add(Array.Empty<TestItem>());

            Assert.That(collection.Count, Is.EqualTo(0));
        }

        #endregion

        #region Remove Tests

        [Test]
        public void RemoveByKeyDecreasesCountTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            collection.Add(new TestItem { Id = 1 });

            collection.Remove(1);

            Assert.That(collection.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveByKeyReturnsRemovedItemTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            var item = new TestItem { Id = 1, Name = "Test" };
            collection.Add(item);

            var removed = collection.Remove(1);

            Assert.That(removed, Is.EqualTo(item));
        }

        [Test]
        public void RemoveNonExistentKeyReturnsDefaultTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);

            var removed = collection.Remove(999);

            Assert.That(removed, Is.Null);
        }

        [Test]
        public void RemoveAtIndexDecreasesCountTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            collection.Add(new TestItem { Id = 1 });

            collection.RemoveAt(0);

            Assert.That(collection.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveAtInvalidIndexThrowsExceptionTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);

            Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(0));
        }

        [Test]
        public void RemoveMultipleItemsDecreasesCountTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            var item1 = new TestItem { Id = 1 };
            var item2 = new TestItem { Id = 2 };
            collection.Add(new[] { item1, item2, new TestItem { Id = 3 } });

            collection.Remove(new[] { item1, item2 });

            Assert.That(collection.Count, Is.EqualTo(1));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void ClearRemovesAllItemsTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            collection.Add(new[] { new TestItem { Id = 1 }, new TestItem { Id = 2 } });

            collection.Clear();

            Assert.That(collection.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClearRaisesCollectionClearEventTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            collection.Add(new TestItem { Id = 1 });
            var eventRaised = false;

            collection.CollectionClear += (_, _) => eventRaised = true;
            collection.Clear();

            Assert.That(eventRaised, Is.True);
        }

        #endregion

        #region Contains Tests

        [Test]
        public void ContainsKeyReturnsTrueForExistingKeyTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            collection.Add(new TestItem { Id = 1 });

            var contains = collection.Contains(1);

            Assert.That(contains, Is.True);
        }

        [Test]
        public void ContainsKeyReturnsFalseForNonExistingKeyTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);

            var contains = collection.Contains(1);

            Assert.That(contains, Is.False);
        }

        [Test]
        public void ContainsValueReturnsTrueForExistingValueTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            var item = new TestItem { Id = 1 };
            collection.Add(item);

            var contains = collection.Contains(item);

            Assert.That(contains, Is.True);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void ResetReplacesAllItemsTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            collection.Add(new TestItem { Id = 1 });

            collection.Reset(new[] { new TestItem { Id = 2 }, new TestItem { Id = 3 } });

            Assert.That(collection.Count, Is.EqualTo(2));
            Assert.That(collection.Contains(1), Is.False);
            Assert.That(collection.Contains(2), Is.True);
        }

        [Test]
        public void ResetRaisesCollectionResetEventTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            var eventRaised = false;

            collection.CollectionReset += (_, _) => eventRaised = true;
            collection.Reset(new[] { new TestItem { Id = 1 } });

            Assert.That(eventRaised, Is.True);
        }

        #endregion

        #region Sorting Tests

        [Test]
        public void ItemsAreSortedByKeyTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            collection.Add(new TestItem { Id = 3 });
            collection.Add(new TestItem { Id = 1 });
            collection.Add(new TestItem { Id = 2 });

            var values = collection.Values.ToList();

            Assert.That(values[0].Id, Is.EqualTo(1));
            Assert.That(values[1].Id, Is.EqualTo(2));
            Assert.That(values[2].Id, Is.EqualTo(3));
        }

        #endregion

        #region PropertyChanged Tests

        [Test]
        public void CountChangeRaisesPropertyChangedEventTest()
        {
            var collection = new SortedCollection<int, TestItem>(x => x.Id);
            var eventRaised = false;
            string? changedProperty = null;

            collection.PropertyChanged += (_, e) =>
            {
                eventRaised = true;
                changedProperty = e.PropertyName;
            };

            collection.Add(new TestItem { Id = 1 });

            Assert.That(eventRaised, Is.True);
            Assert.That(changedProperty, Is.EqualTo(nameof(collection.Count)));
        }

        #endregion
    }
}
