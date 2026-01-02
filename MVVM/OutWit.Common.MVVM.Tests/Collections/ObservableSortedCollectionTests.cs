using NUnit.Framework;
using OutWit.Common.MVVM.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OutWit.Common.MVVM.Tests.Collections
{
    [TestFixture]
    public class ObservableSortedCollectionTests
    {
        #region Test Data

        private class ObservableItem : INotifyPropertyChanged
        {
            private int m_id;
            private string m_name = "";

            public int Id
            {
                get => m_id;
                set
                {
                    m_id = value;
                    OnPropertyChanged();
                }
            }

            public string Name
            {
                get => m_name;
                set
                {
                    m_name = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void ConstructorSubscribesToItemsTest()
        {
            var items = new[]
            {
                new ObservableItem { Id = 1, Name = "Item1" },
                new ObservableItem { Id = 2, Name = "Item2" }
            };

            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id, items);
            var eventRaised = false;

            collection.CollectionContentChanged += (_, _) => eventRaised = true;

            items[0].Name = "Modified";

            Assert.That(eventRaised, Is.True);
        }

        #endregion

        #region Add Tests

        [Test]
        public void AddSubscribesToItemPropertyChangedTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var item = new ObservableItem { Id = 1, Name = "Test" };
            var eventRaised = false;

            collection.Add(item);
            collection.CollectionContentChanged += (_, _) => eventRaised = true;

            item.Name = "Modified";

            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void AddMultipleSubscribesToAllItemsTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var items = new[]
            {
                new ObservableItem { Id = 1, Name = "Item1" },
                new ObservableItem { Id = 2, Name = "Item2" }
            };
            var eventCount = 0;

            collection.Add(items);
            collection.CollectionContentChanged += (_, _) => eventCount++;

            items[0].Name = "Modified1";
            items[1].Name = "Modified2";

            Assert.That(eventCount, Is.EqualTo(2));
        }

        [Test]
        public void CollectionContentChangedProvidesCorrectSenderTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var item = new ObservableItem { Id = 1, Name = "Test" };
            object? eventSender = null;

            collection.Add(item);
            collection.CollectionContentChanged += (sender, _) => eventSender = sender;

            item.Name = "Modified";

            Assert.That(eventSender, Is.EqualTo(item));
        }

        [Test]
        public void CollectionContentChangedProvidesCorrectPropertyNameTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var item = new ObservableItem { Id = 1, Name = "Test" };
            string? propertyName = null;

            collection.Add(item);
            collection.CollectionContentChanged += (_, e) => propertyName = e.PropertyName;

            item.Name = "Modified";

            Assert.That(propertyName, Is.EqualTo(nameof(ObservableItem.Name)));
        }

        #endregion

        #region Remove Tests

        [Test]
        public void RemoveUnsubscribesFromItemTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var item = new ObservableItem { Id = 1, Name = "Test" };
            var eventRaised = false;

            collection.Add(item);
            collection.Remove(1);
            collection.CollectionContentChanged += (_, _) => eventRaised = true;

            item.Name = "Modified";

            Assert.That(eventRaised, Is.False);
        }

        [Test]
        public void RemoveAtUnsubscribesFromItemTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var item = new ObservableItem { Id = 1, Name = "Test" };
            var eventRaised = false;

            collection.Add(item);
            collection.RemoveAt(0);
            collection.CollectionContentChanged += (_, _) => eventRaised = true;

            item.Name = "Modified";

            Assert.That(eventRaised, Is.False);
        }

        [Test]
        public void RemoveMultipleUnsubscribesFromAllItemsTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var items = new[]
            {
                new ObservableItem { Id = 1, Name = "Item1" },
                new ObservableItem { Id = 2, Name = "Item2" }
            };
            var eventCount = 0;

            collection.Add(items);
            collection.Remove(items);
            collection.CollectionContentChanged += (_, _) => eventCount++;

            items[0].Name = "Modified1";
            items[1].Name = "Modified2";

            Assert.That(eventCount, Is.EqualTo(0));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void ClearUnsubscribesFromAllItemsTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var items = new[]
            {
                new ObservableItem { Id = 1, Name = "Item1" },
                new ObservableItem { Id = 2, Name = "Item2" }
            };
            var eventCount = 0;

            collection.Add(items);
            collection.Clear();
            collection.CollectionContentChanged += (_, _) => eventCount++;

            items[0].Name = "Modified1";
            items[1].Name = "Modified2";

            Assert.That(eventCount, Is.EqualTo(0));
        }

        #endregion

        #region Multiple Items Tests

        [Test]
        public void MultipleItemsCanRaiseEventsIndependentlyTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);
            var item1 = new ObservableItem { Id = 1, Name = "Item1" };
            var item2 = new ObservableItem { Id = 2, Name = "Item2" };
            var eventCount = 0;

            collection.Add(item1);
            collection.Add(item2);
            collection.CollectionContentChanged += (_, _) => eventCount++;

            item1.Name = "Modified1";
            item1.Id = 10;
            item2.Name = "Modified2";

            Assert.That(eventCount, Is.EqualTo(3));
        }

        #endregion

        #region Backward Compatibility Tests

        [Test]
        public void ObservableSortedCollectionWorksCorrectlyTest()
        {
            var collection = new ObservableSortedCollection<int, ObservableItem>(x => x.Id);

            var item = new ObservableItem { Id = 1, Name = "Test" };
            var eventRaised = false;

            collection.Add(item);
            collection.CollectionContentChanged += (_, _) => eventRaised = true;

            item.Name = "Modified";

            Assert.That(eventRaised, Is.True);
        }

        #endregion
    }
}
