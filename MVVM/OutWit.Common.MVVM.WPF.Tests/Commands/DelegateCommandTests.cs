using NUnit.Framework;
using OutWit.Common.MVVM.WPF.Commands;
using System.Threading;

namespace OutWit.Common.MVVM.WPF.Tests.Commands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DelegateCommandTests
    {
        #region Execute Tests

        [Test]
        public void ExecuteInvokesActionTest()
        {
            var executed = false;
            var command = new DelegateCommand(_ => executed = true);

            command.Execute(null);

            Assert.That(executed, Is.True);
        }

        [Test]
        public void ExecutePassesParameterTest()
        {
            object? passedParameter = null;
            var command = new DelegateCommand(param => passedParameter = param);
            var testParameter = new object();

            command.Execute(testParameter);

            Assert.That(passedParameter, Is.EqualTo(testParameter));
        }

        #endregion

        #region CanExecute Tests

        [Test]
        public void CanExecuteReturnsTrueWhenNoPredicateTest()
        {
            var command = new DelegateCommand(_ => { });

            var canExecute = command.CanExecute(null);

            Assert.That(canExecute, Is.True);
        }

        [Test]
        public void CanExecuteReturnsPredicateResultTest()
        {
            var command = new DelegateCommand(_ => { }, _ => false);

            var canExecute = command.CanExecute(null);

            Assert.That(canExecute, Is.False);
        }

        [Test]
        public void CanExecutePassesParameterToPredicateTest()
        {
            object? passedParameter = null;
            var command = new DelegateCommand(
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

        #region CanExecuteChanged Tests

        [Test]
        public void CanExecuteChangedCanBeSubscribedTest()
        {
            var command = new DelegateCommand(_ => { });
            var eventRaised = false;

            command.CanExecuteChanged += (_, _) => eventRaised = true;

            Assert.That(eventRaised, Is.False);
        }

        #endregion
    }
}
