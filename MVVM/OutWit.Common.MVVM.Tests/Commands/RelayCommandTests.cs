using NUnit.Framework;
using OutWit.Common.MVVM.Commands;

namespace OutWit.Common.MVVM.Tests.Commands
{
    [TestFixture]
    public class RelayCommandTests
    {
        #region Execute Tests

        [Test]
        public void ExecuteInvokesActionTest()
        {
            var executed = false;
            var command = new RelayCommand(_ => executed = true);

            command.Execute(null);

            Assert.That(executed, Is.True);
        }

        [Test]
        public void ExecutePassesParameterTest()
        {
            object? passedParameter = null;
            var command = new RelayCommand(param => passedParameter = param);
            var testParameter = new object();

            command.Execute(testParameter);

            Assert.That(passedParameter, Is.EqualTo(testParameter));
        }

        #endregion

        #region CanExecute Tests

        [Test]
        public void CanExecuteReturnsTrueWhenNoPredicateTest()
        {
            var command = new RelayCommand(_ => { });

            var canExecute = command.CanExecute(null);

            Assert.That(canExecute, Is.True);
        }

        [Test]
        public void CanExecuteReturnsPredicateResultTest()
        {
            var command = new RelayCommand(_ => { }, _ => false);

            var canExecute = command.CanExecute(null);

            Assert.That(canExecute, Is.False);
        }

        [Test]
        public void CanExecutePassesParameterToPredicateTest()
        {
            object? passedParameter = null;
            var command = new RelayCommand(
                _ => { },
                param =>
                {
                    passedParameter = param;
                    return true;
                });
            var testParameter = new object();

            command.CanExecute(testParameter);

            Assert.That(passedParameter, Is.EqualTo(testParameter));
        }

        #endregion

        #region RaiseCanExecuteChanged Tests

        [Test]
        public void RaiseCanExecuteChangedRaisesEventTest()
        {
            var command = new RelayCommand(_ => { });
            var eventRaised = false;

            command.CanExecuteChanged += (_, _) => eventRaised = true;
            command.RaiseCanExecuteChanged();

            Assert.That(eventRaised, Is.True);
        }

        #endregion
    }
}
