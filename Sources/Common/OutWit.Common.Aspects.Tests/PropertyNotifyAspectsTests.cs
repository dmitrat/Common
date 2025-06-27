using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using OutWit.Common.Aspects.Tests.Mock;

namespace OutWit.Common.Aspects.Tests
{
    [TestFixture]
    public class PropertyNotifyAspectsTests
    {
        #region [Notify] Aspect Tests (for classes that already implement INotifyPropertyChanged)

        [Test]
        public void Notify_OnPropertySet_InvokesEventOnInheritedClass()
        {
            // This test verifies the main scenario, including inheritance.
            // Arrange
            var viewModel = new Augmenting_InheritedViewModel();
            var receivedEvents = new List<string>();
            // Subscribe to the event that is implemented in the base class
            (viewModel as INotifyPropertyChanged).PropertyChanged += (sender, args) =>
            {
                receivedEvents.Add(args.PropertyName);
            };

            // Act
            viewModel.SourceProperty = "New Value";

            // Assert
            Assert.That(receivedEvents.Count, Is.EqualTo(2), "There should be two events: for SourceProperty and CalculatedProperty");
            Assert.That(receivedEvents, Does.Contain("SourceProperty"));
            Assert.That(receivedEvents, Does.Contain("CalculatedProperty"));
        }

        [Test]
        public void Notify_WithNotifyAlso_RaisesBothEvents()
        {
            // This test explicitly focuses on NotifyAlso
            // Arrange
            var viewModel = new Augmenting_InheritedViewModel();
            var receivedEvents = new List<string>();
            (viewModel as INotifyPropertyChanged).PropertyChanged += (sender, args) =>
            {
                receivedEvents.Add(args.PropertyName);
            };

            // Act
            viewModel.SourceProperty = "Test";

            // Assert
            Assert.That(receivedEvents, Is.EquivalentTo(new[] { "SourceProperty", "CalculatedProperty" }));
        }

        #endregion

        #region [NotifyAuto] Aspect Tests (for POCO classes)

        [Test]
        public void NotifyAuto_OnPocoPropertySet_InjectsInterfaceAndRaisesEvent()
        {
            // Arrange
            var viewModel = new Generating_PocoViewModel();
            var receivedEvents = new List<string>();

            // Act & Assert
            // 1. Verify that the interface was added and the object can be cast to it.
            INotifyPropertyChanged notifiable = viewModel as INotifyPropertyChanged;
            Assert.That(notifiable, Is.Not.Null, "INotifyPropertyChanged should be mixed in by the aspect.");

            // 2. Subscribe to the event.
            notifiable.PropertyChanged += (sender, args) =>
            {
                receivedEvents.Add(args.PropertyName);
            };

            // 3. Change the property and check the result.
            viewModel.Value = 42;

            Assert.That(receivedEvents.Count, Is.EqualTo(2));
            Assert.That(receivedEvents, Does.Contain("Value"));
            Assert.That(receivedEvents, Does.Contain("Description"));
        }

        [Test]
        public void NotifyAuto_WithNotifyAlso_RaisesBothEvents()
        {
            // Arrange
            var viewModel = new Generating_PocoViewModel();
            var receivedEvents = new List<string>();
            (viewModel as INotifyPropertyChanged).PropertyChanged += (sender, args) =>
            {
                receivedEvents.Add(args.PropertyName);
            };

            // Act
            viewModel.Value = 100;

            // Assert
            Assert.That(receivedEvents, Is.EquivalentTo(new[] { "Value", "Description" }));
        }

        #endregion
    }
}