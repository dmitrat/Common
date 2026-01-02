using NUnit.Framework;
using OutWit.Common.MVVM.Blazor.Collections;

namespace OutWit.Common.MVVM.Blazor.Tests.Collections
{
    [TestFixture]
    public class SafeObservableCollectionTests
    {
        #region Add Tests

        [Test]
        public void AddItemRaisesCollectionChangedTest()
        {
            var collection = new SafeObservableCollection<string>();
            var collectionChangedRaised = false;

            collection.CollectionChanged += (s, e) => collectionChangedRaised = true;
            collection.Add("Test");

            Assert.That(collectionChangedRaised, Is.True);
        }

        [Test]
        public void AddItemIncreasesCountTest()
        {
            var collection = new SafeObservableCollection<int>();

            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            Assert.That(collection.Count, Is.EqualTo(3));
        }

        #endregion

        #region Remove Tests

        [Test]
        public void RemoveItemRaisesCollectionChangedTest()
        {
            var collection = new SafeObservableCollection<string> { "Test" };
            var collectionChangedRaised = false;

            collection.CollectionChanged += (s, e) => collectionChangedRaised = true;
            collection.Remove("Test");

            Assert.That(collectionChangedRaised, Is.True);
        }

        [Test]
        public void RemoveItemDecreasesCountTest()
        {
            var collection = new SafeObservableCollection<int> { 1, 2, 3 };

            collection.Remove(2);

            Assert.That(collection.Count, Is.EqualTo(2));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void ClearRaisesCollectionChangedTest()
        {
            var collection = new SafeObservableCollection<string> { "A", "B", "C" };
            var collectionChangedRaised = false;

            collection.CollectionChanged += (s, e) => collectionChangedRaised = true;
            collection.Clear();

            Assert.That(collectionChangedRaised, Is.True);
        }

        [Test]
        public void ClearSetsCountToZeroTest()
        {
            var collection = new SafeObservableCollection<int> { 1, 2, 3 };

            collection.Clear();

            Assert.That(collection.Count, Is.EqualTo(0));
        }

        #endregion

        #region StateHasChanged Callback Tests

        [Test]
        public void AddCallsStateHasChangedCallbackTest()
        {
            var stateHasChangedCalled = false;
            var collection = new SafeObservableCollection<string>(() => stateHasChangedCalled = true);

            collection.Add("Test");

            Assert.That(stateHasChangedCalled, Is.True);
        }

        [Test]
        public void RemoveCallsStateHasChangedCallbackTest()
        {
            var stateHasChangedCallCount = 0;
            var collection = new SafeObservableCollection<string>(() => stateHasChangedCallCount++) { "Test" };

            stateHasChangedCallCount = 0; // Reset after initial add
            collection.Remove("Test");

            Assert.That(stateHasChangedCallCount, Is.EqualTo(1));
        }

        [Test]
        public void ClearCallsStateHasChangedCallbackTest()
        {
            var stateHasChangedCallCount = 0;
            var collection = new SafeObservableCollection<string>(() => stateHasChangedCallCount++)
            {
                "A", "B", "C"
            };

            stateHasChangedCallCount = 0;
            collection.Clear();

            Assert.That(stateHasChangedCallCount, Is.EqualTo(1));
        }

        #endregion
    }
}
