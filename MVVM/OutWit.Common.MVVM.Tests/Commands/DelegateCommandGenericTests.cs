using NUnit.Framework;
using OutWit.Common.MVVM.Commands;

namespace OutWit.Common.MVVM.Tests.Commands
{
    [TestFixture]
    public class DelegateCommandGenericTests
    {
        #region Execute Tests

        [Test]
        public void ExecuteInvokesActionWithTypedParameterTest()
        {
            string? passedValue = null;
            var command = new DelegateCommand<string>(value => passedValue = value);

            command.Execute("test");

            Assert.That(passedValue, Is.EqualTo("test"));
        }

        [Test]
        public void ExecuteHandlesNullParameterTest()
        {
            string? passedValue = "initial";
            var command = new DelegateCommand<string?>(value => passedValue = value);

            command.Execute(null);

            Assert.That(passedValue, Is.Null);
        }

        #endregion

        #region CanExecute Tests

        [Test]
        public void CanExecuteReturnsTrueWhenNoPredicateTest()
        {
            var command = new DelegateCommand<string>(_ => { });

            var canExecute = command.CanExecute("test");

            Assert.That(canExecute, Is.True);
        }

        [Test]
        public void CanExecuteReturnsPredicateResultTest()
        {
            var command = new DelegateCommand<string>(_ => { }, value => value?.Length > 3);

            Assert.That(command.CanExecute("hi"), Is.False);
            Assert.That(command.CanExecute("hello"), Is.True);
        }

        [Test]
        public void CanExecuteHandlesInvalidTypeTest()
        {
            var command = new DelegateCommand<string>(_ => { });

            var canExecute = command.CanExecute(123);

            Assert.That(canExecute, Is.True);
        }

        #endregion

        #region RaiseCanExecuteChanged Tests

        [Test]
        public void RaiseCanExecuteChangedRaisesEventTest()
        {
            var command = new DelegateCommand<string>(_ => { });
            var eventRaised = false;

            command.CanExecuteChanged += (_, _) => eventRaised = true;
            command.RaiseCanExecuteChanged();

            Assert.That(eventRaised, Is.True);
        }

        #endregion
    }
}
