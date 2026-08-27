using System;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Tests.Model
{
    [TestFixture]
    public class NavigationGroupTests
    {
        #region Construction Tests

        [Test]
        public void DefaultAloneIsTheWholeGroupTest()
        {
            var group = new NavigationGroup("section", "general");

            Assert.That(group.RouteKeys, Is.EqualTo(new[] { "general" }));
            Assert.That(group.Outlet, Is.EqualTo(NavigationOutlets.MAIN));
            Assert.That(group.Metadata, Is.Null);
        }

        [Test]
        public void DefaultNotListedGoesFirstTest()
        {
            var group = new NavigationGroup("section", "general", new[] { "diary", "notes" });

            Assert.That(group.RouteKeys, Is.EqualTo(new[] { "general", "diary", "notes" }));
        }

        [Test]
        public void DuplicateMembersCollapseTest()
        {
            var group = new NavigationGroup("section", "general", new[] { "general", "diary", "diary" });

            Assert.That(group.RouteKeys, Is.EqualTo(new[] { "general", "diary" }));
        }

        [Test]
        public void EmptyKeysAreRefusedTest()
        {
            Assert.Throws<ArgumentException>(() => new NavigationGroup("", "general"));
            Assert.Throws<ArgumentException>(() => new NavigationGroup("section", ""));
            Assert.Throws<ArgumentException>(() => new NavigationGroup("section", "general", new[] { "" }));
            Assert.Throws<ArgumentException>(() => new NavigationGroup("section", "general", outlet: ""));
        }

        [Test]
        public void AGroupCannotContainItselfTest()
        {
            Assert.Throws<ArgumentException>(() => new NavigationGroup("section", "section"));
            Assert.Throws<ArgumentException>(() => new NavigationGroup("section", "general", new[] { "section" }));
        }

        [Test]
        public void ContainsChecksMembershipTest()
        {
            var group = new NavigationGroup("section", "general", new[] { "diary" });

            Assert.That(group.Contains("general"), Is.True);
            Assert.That(group.Contains("diary"), Is.True);
            Assert.That(group.Contains("section"), Is.False);
            Assert.That(group.Contains("other"), Is.False);
        }

        #endregion

        #region ModelBase Tests

        [Test]
        public void IsComparesEveryFieldTest()
        {
            var group = new NavigationGroup("section", "general", new[] { "general", "diary" }, "Side", "meta");

            Assert.That(group.Is(new NavigationGroup("section", "general", new[] { "general", "diary" }, "Side", "meta")), Is.True);
            Assert.That(group.Is(new NavigationGroup("other", "general", new[] { "general", "diary" }, "Side", "meta")), Is.False);
            Assert.That(group.Is(new NavigationGroup("section", "diary", new[] { "general", "diary" }, "Side", "meta")), Is.False);
            Assert.That(group.Is(new NavigationGroup("section", "general", new[] { "general" }, "Side", "meta")), Is.False);
            Assert.That(group.Is(new NavigationGroup("section", "general", new[] { "general", "diary" }, "Main", "meta")), Is.False);
            Assert.That(group.Is(new NavigationGroup("section", "general", new[] { "general", "diary" }, "Side", "other")), Is.False);
        }

        [Test]
        public void CloneIsEqualAndIndependentTest()
        {
            var group = new NavigationGroup("section", "general", new[] { "general", "diary" }, "Side", "meta");

            var clone = group.Clone();

            Assert.That(clone, Is.Not.SameAs(group));
            Assert.That(group.Is(clone), Is.True);
        }

        #endregion
    }
}
