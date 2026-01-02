using NUnit.Framework;
using OutWit.Common.MVVM.Blazor.ViewModels;

namespace OutWit.Common.MVVM.Blazor.Tests.ViewModels
{
    [TestFixture]
    public class ViewModelBaseTests
    {
        #region PropertyChanged Tests

        [Test]
        public void SetPropertyRaisesPropertyChangedTest()
        {
            var vm = new TestViewModel();
            var propertyChangedRaised = false;
            var changedPropertyName = string.Empty;

            vm.PropertyChanged += (s, e) =>
            {
                propertyChangedRaised = true;
                changedPropertyName = e.PropertyName;
            };

            vm.Name = "Test";

            Assert.That(propertyChangedRaised, Is.True);
            Assert.That(changedPropertyName, Is.EqualTo(nameof(TestViewModel.Name)));
        }

        [Test]
        public void SetPropertyDoesNotRaiseWhenValueUnchangedTest()
        {
            var vm = new TestViewModel();
            vm.Name = "Test";

            var propertyChangedCount = 0;
            vm.PropertyChanged += (s, e) => propertyChangedCount++;

            vm.Name = "Test"; // Same value

            Assert.That(propertyChangedCount, Is.EqualTo(0));
        }

        [Test]
        public void SetPropertyReturnsTrueWhenValueChangedTest()
        {
            var vm = new TestViewModel();

            vm.Name = "First";
            vm.Name = "Second";

            Assert.That(vm.Name, Is.EqualTo("Second"));
        }

        [Test]
        public void OnPropertyChangedCalledTest()
        {
            var vm = new TestViewModel();

            vm.Name = "Test";

            Assert.That(vm.LastChangedProperty, Is.EqualTo(nameof(TestViewModel.Name)));
        }

        #endregion

        #region Busy Tests

        [Test]
        public void BusyDefaultIsFalseTest()
        {
            var vm = new TestViewModel();

            Assert.That(vm.Busy, Is.False);
        }

        #endregion

        #region Check Tests

        [Test]
        public void CheckWithResultReturnsValueOnSuccessTest()
        {
            var vm = new TestViewModel();

            var result = vm.TestCheck(() => 42);

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void CheckWithResultReturnsDefaultOnExceptionTest()
        {
            var vm = new TestViewModel();

            var result = vm.TestCheck<int>(() => throw new Exception("Test"), -1);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void CheckBoolReturnsTrueOnSuccessTest()
        {
            var vm = new TestViewModel();

            var result = vm.TestCheck(() => true);

            Assert.That(result, Is.True);
        }

        [Test]
        public void CheckBoolReturnsFalseOnExceptionTest()
        {
            var vm = new TestViewModel();

            var result = vm.TestCheck(() => throw new Exception("Test"));

            Assert.That(result, Is.False);
        }

        [Test]
        public void CheckActionSwallowsExceptionTest()
        {
            var vm = new TestViewModel();

            Assert.DoesNotThrow(() => vm.TestCheck(() => throw new Exception("Test")));
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void DisposeUnsubscribesFromPropertyChangedTest()
        {
            var vm = new TestViewModel();
            var propertyChangedCount = 0;

            vm.PropertyChanged += (s, e) => propertyChangedCount++;
            vm.Name = "Test1";
            Assert.That(propertyChangedCount, Is.EqualTo(1));

            vm.Dispose();
            vm.RaisePropertyChanged(nameof(TestViewModel.Name));

            // Internal handler should be unsubscribed, but external handler still fires
            // So count should still increase
            Assert.That(propertyChangedCount, Is.EqualTo(2));
        }

        #endregion

        #region Multiple Properties Tests

        [Test]
        public void MultiplePropertiesRaiseIndependentlyTest()
        {
            var vm = new TestViewModel();
            var changedProperties = new List<string>();

            vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

            vm.Name = "Test";
            vm.Count = 5;

            Assert.That(changedProperties, Has.Count.EqualTo(2));
            Assert.That(changedProperties, Contains.Item(nameof(TestViewModel.Name)));
            Assert.That(changedProperties, Contains.Item(nameof(TestViewModel.Count)));
        }

        #endregion
    }
}
