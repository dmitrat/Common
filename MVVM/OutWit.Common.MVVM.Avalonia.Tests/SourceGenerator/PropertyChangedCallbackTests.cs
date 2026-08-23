using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using NUnit.Framework;

namespace OutWit.Common.MVVM.Avalonia.Tests.SourceGenerator
{
    /// <summary>
    /// The generator promises that <c>On{Property}Changed</c> is "automatically discovered".
    /// It used to discover it and then emit a comment, so the callbacks never ran and nothing
    /// said so. These tests are what makes the promise checkable.
    /// </summary>
    [TestFixture]
    public class PropertyChangedCallbackTests
    {
        #region Fields

        private TestCallbackControl m_control = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_control = new TestCallbackControl();
            m_control.Reset();
            TestCallbackAttachedProperties.Changes.Clear();
        }

        #region Styled Property Tests

        [Test]
        public void InstanceCallbackWithTypedArgumentsIsCalledTest()
        {
            m_control.Text = "first";
            m_control.Text = "second";

            Assert.That(m_control.TextChanges, Is.EqualTo(new[] { "->first", "first->second" }));
        }

        [Test]
        public void StaticCallbackReceivesTheObjectThatChangedTest()
        {
            m_control.Number = 42;

            Assert.That(m_control.NumberChanges, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public void CallbackNamedByTheAttributeIsCalledTest()
        {
            m_control.Renamed = "explicit";

            Assert.That(m_control.RenamedChanges, Is.EqualTo(new[] { "explicit" }));
        }

        [Test]
        public void SettingTheSameValueDoesNotCallBackTest()
        {
            m_control.Text = "same";
            m_control.TextChanges.Clear();

            m_control.Text = "same";

            Assert.That(m_control.TextChanges, Is.Empty);
        }

        [Test]
        public void EachControlGetsItsOwnCallbacksTest()
        {
            var other = new TestCallbackControl();

            m_control.Text = "mine";

            Assert.That(m_control.TextChanges, Has.Count.EqualTo(1));
            Assert.That(other.TextChanges, Is.Empty, "a class handler must not report another instance's change");
        }

        [Test]
        public void APropertyWithoutACallbackStillWorksTest()
        {
            Assert.That(m_control.Untouched, Is.EqualTo("quiet"));

            m_control.Untouched = "changed";

            Assert.That(m_control.Untouched, Is.EqualTo("changed"));
        }

        #endregion

        #region Direct Property Tests

        /// <summary>
        /// The deeper half of the same defect: a direct property whose generated setter only
        /// wrote the backing field raised nothing at all, so bindings out of it never updated
        /// either — and no callback could have fired even once it was subscribed.
        /// </summary>
        [Test]
        public void DirectPropertyRaisesChangeNotificationTest()
        {
            var control = new TestDirectPropertyControl();
            var seen = new List<string?>();
            ((INotifyPropertyChanged)control).PropertyChanged += (_, e) => seen.Add(e.PropertyName);

            control.IsActive = true;

            Assert.That(control.IsActive, Is.True);
            Assert.That(seen, Does.Contain(nameof(TestDirectPropertyControl.IsActive)));
        }

        [Test]
        public void DirectPropertyCallbackIsCalledTest()
        {
            m_control.IsActive = true;
            m_control.IsActive = false;

            Assert.That(m_control.ActiveChanges, Is.EqualTo(new[] { true, false }));
        }

        #endregion

        #region Attached Property Tests

        [Test]
        public void AttachedPropertyCallbackIsCalledForTheTargetTest()
        {
            var target = new Border();

            TestCallbackAttachedProperties.SetIsPinned(target, true);

            Assert.That(TestCallbackAttachedProperties.Changes, Is.EqualTo(new[] { "Border:True" }));
            Assert.That(TestCallbackAttachedProperties.GetIsPinned(target), Is.True);
        }

        #endregion
    }
}
