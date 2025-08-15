using NUnit.Framework.Legacy;
using OutWit.Common.Reflection.Tests.Mock;

namespace OutWit.Common.Reflection.Tests
{
    [TestFixture]
    public class EventUtilsTests
    {
        [SetUp]
        public void Setup()
        {
            // Reset the captured state before each test for the universal handler
            UniversalHandlerHost.Reset();
        }

        [Test]
        public void GetAllEventsFindsAllEventsInHierarchyTest()
        {
            // Arrange
            var type = typeof(DerivedEventSource);
            var expectedEventNames = new[] { "BaseEvent", "DerivedEvent", "InterfaceEvent" };

            // Act
            var events = type.GetAllEvents().ToList();
            var eventNames = events.Select(e => e.Name).ToList();

            // Assert
            Assert.That(events, Is.Not.Null);
            // Using CollectionAssert to ensure all expected events are found, regardless of order.
            CollectionAssert.AreEquivalent(expectedEventNames, eventNames);
        }

        [Test]
        public void GetAllEventsReturnsEmptyForTypeWithNoEventsTest()
        {
            // Arrange
            var type = typeof(ClassWithNoEvents);

            // Act
            var events = type.GetAllEvents().ToList();

            // Assert
            Assert.That(events, Is.Not.Null);
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void GetAllMethodsFindsAllMethodsInHierarchyTest()
        {
            // Arrange
            var type = typeof(DerivedMethodSource);
            var expectedMethodNames = new[] { "BaseMethod", "DerivedMethod", "InterfaceMethod" };

            // Act
            var methods = type.GetAllMethods().ToList();
            var methodNames = methods.Select(m => m.Name).ToList();

            // Assert
            // Check that our specific methods are present
            foreach (var name in expectedMethodNames)
            {
                Assert.That(methodNames, Does.Contain(name));
            }

            // Also check that methods from System.Object are included
            Assert.That(methodNames, Does.Contain(nameof(ToString)));
            Assert.That(methodNames, Does.Contain(nameof(GetHashCode)));
            Assert.That(methodNames, Does.Contain(nameof(Equals)));
        }

        [Test]
        public void CreateUniversalHandlerSubscribesAndCapturesCorrectDataTest()
        {
            // Arrange
            var publisher = new TestPublisher();
            var eventInfo = publisher.GetType().GetEvent(nameof(TestPublisher.CustomEvent))!;
            const int testNumber = 42;
            const string testString = "Hello, World!";

            // Act
            // 1. Create the dynamic delegate pointing to our universal handler
            Delegate dynamicHandler = eventInfo.CreateUniversalHandler(publisher, UniversalHandlerHost.Handle);

            // 2. Subscribe to the event using the new delegate
            eventInfo.AddEventHandler(publisher, dynamicHandler);

            // 3. Trigger the event
            publisher.RaiseCustomEvent(testNumber, testString);

            // Assert
            Assert.That(UniversalHandlerHost.CapturedSender, Is.SameAs(publisher));
            Assert.That(UniversalHandlerHost.CapturedEventName, Is.EqualTo(nameof(TestPublisher.CustomEvent)));
            Assert.That(UniversalHandlerHost.CapturedParameters, Is.Not.Null);
            Assert.That(UniversalHandlerHost.CapturedParameters, Has.Length.EqualTo(2));
            Assert.That(UniversalHandlerHost.CapturedParameters![0], Is.EqualTo(testNumber));
            Assert.That(UniversalHandlerHost.CapturedParameters![1], Is.EqualTo(testString));
        }

        [Test]
        public void CreateUniversalHandlerWorksWithStandardEventHandlerTest()
        {
            // Arrange
            var publisher = new TestPublisher();
            var eventInfo = publisher.GetType().GetEvent(nameof(TestPublisher.StandardEvent))!;

            // Act
            Delegate dynamicHandler = eventInfo.CreateUniversalHandler(publisher, UniversalHandlerHost.Handle);
            eventInfo.AddEventHandler(publisher, dynamicHandler);
            publisher.RaiseStandardEvent();

            // Assert
            Assert.That(UniversalHandlerHost.CapturedSender, Is.SameAs(publisher));
            Assert.That(UniversalHandlerHost.CapturedEventName, Is.EqualTo(nameof(TestPublisher.StandardEvent)));
            Assert.That(UniversalHandlerHost.CapturedParameters, Is.Not.Null);
            Assert.That(UniversalHandlerHost.CapturedParameters, Has.Length.EqualTo(2));
            Assert.That(UniversalHandlerHost.CapturedParameters![0], Is.SameAs(publisher)); // First arg is sender
            Assert.That(UniversalHandlerHost.CapturedParameters![1], Is.SameAs(EventArgs.Empty)); // Second is EventArgs
        }

        [Test]
        public void CreateUniversalHandlerWorksWithParameterlessEventTest()
        {
            // Arrange
            var publisher = new TestPublisher();
            var eventInfo = publisher.GetType().GetEvent(nameof(TestPublisher.SimpleEvent))!;

            // Act
            Delegate dynamicHandler = eventInfo.CreateUniversalHandler(publisher, UniversalHandlerHost.Handle);
            eventInfo.AddEventHandler(publisher, dynamicHandler);
            publisher.RaiseSimpleEvent();

            // Assert
            Assert.That(UniversalHandlerHost.CapturedSender, Is.SameAs(publisher));
            Assert.That(UniversalHandlerHost.CapturedEventName, Is.EqualTo(nameof(TestPublisher.SimpleEvent)));
            Assert.That(UniversalHandlerHost.CapturedParameters, Is.Not.Null);
            Assert.That(UniversalHandlerHost.CapturedParameters, Is.Empty);
        }

        [Test]
        public void CreateUniversalHandlerThrowsExceptionForNonStaticDelegateTest()
        {
            // Arrange
            var publisher = new TestPublisher();
            var eventInfo = publisher.GetType().GetEvent(nameof(TestPublisher.CustomEvent))!;
            var nonStaticHandler = new NonStaticHandler();
            UniversalEventHandler<TestPublisher> handlerDelegate = nonStaticHandler.HandleInstance;

            // Act & Assert
            var ex = Assert.Throws<Exception>(() =>
            {
                eventInfo.CreateUniversalHandler(publisher, handlerDelegate);
            });

            Assert.That(ex.Message, Is.EqualTo("Universal event handler delegate must be static"));
        }
    }
}