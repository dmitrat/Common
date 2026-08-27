using System;
using System.Linq;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Services;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    [TestFixture]
    public class RouteRegistryTests
    {
        #region Fields

        private RouteRegistry m_registry = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_registry = new RouteRegistry();
        }

        #region Group Declaration Tests

        [Test]
        public void RegisterGroupPutsTheDefaultAmongTheMembersTest()
        {
            m_registry.RegisterGroup("section", "general", new[] { "diary" });

            Assert.That(m_registry.TryGetGroup("section", out var group), Is.True);
            Assert.That(group!.DefaultRouteKey, Is.EqualTo("general"));
            Assert.That(group.RouteKeys, Is.EqualTo(new[] { "general", "diary" }));
        }

        [Test]
        public void RegisterGroupKeepsTheDeclaredOrderTest()
        {
            m_registry.RegisterGroup("section", "diary", new[] { "general", "diary", "notes" });

            Assert.That(m_registry.TryGetGroup("section", out var group), Is.True);
            Assert.That(group!.RouteKeys, Is.EqualTo(new[] { "general", "diary", "notes" }));
        }

        [Test]
        public void GroupsAreListedInDeclarationOrderTest()
        {
            m_registry.RegisterGroup("b", "b1");
            m_registry.RegisterGroup("a", "a1");

            Assert.That(m_registry.Groups.Select(group => group.Key), Is.EqualTo(new[] { "b", "a" }));
        }

        [Test]
        public void OptionsGroupsArePreloadedTest()
        {
            var options = new NavigationOptions();
            options.Routes.Add(new NavigationRoute("general", typeof(PlainViewModel)));
            options.Groups.Add(new NavigationGroup("section", "general"));

            var registry = new RouteRegistry(options);

            Assert.That(registry.ContainsGroup("section"), Is.True);
            Assert.That(registry.Contains("general"), Is.True);
        }

        [Test]
        public void RedeclarationReplacesTheDeclarationAndKeepsAddedMembersTest()
        {
            m_registry.AddToGroup("section", "extra");
            m_registry.RegisterGroup("section", "general", new[] { "general", "diary" }, outlet: "Side", metadata: "meta");

            Assert.That(m_registry.Groups, Has.Count.EqualTo(1));
            Assert.That(m_registry.TryGetGroup("section", out var group), Is.True);
            Assert.That(group!.DefaultRouteKey, Is.EqualTo("general"));
            Assert.That(group.Outlet, Is.EqualTo("Side"));
            Assert.That(group.Metadata, Is.EqualTo("meta"));
            Assert.That(group.RouteKeys, Is.EqualTo(new[] { "general", "diary", "extra" }));
        }

        #endregion

        #region AddToGroup Tests

        [Test]
        public void AddToGroupDeclaresAnUnknownGroupWithTheRouteAsDefaultTest()
        {
            m_registry.AddToGroup("section", "general");

            Assert.That(m_registry.TryGetGroup("section", out var group), Is.True);
            Assert.That(group!.DefaultRouteKey, Is.EqualTo("general"));
            Assert.That(group.RouteKeys, Is.EqualTo(new[] { "general" }));
            Assert.That(group.Outlet, Is.EqualTo(NavigationOutlets.MAIN));
        }

        [Test]
        public void AddToGroupAppendsAndIsIdempotentTest()
        {
            m_registry.RegisterGroup("section", "general");

            m_registry.AddToGroup("section", "diary");
            m_registry.AddToGroup("section", "diary");
            m_registry.AddToGroup("section", "general");

            Assert.That(m_registry.TryGetGroup("section", out var group), Is.True);
            Assert.That(group!.RouteKeys, Is.EqualTo(new[] { "general", "diary" }));
        }

        #endregion

        #region Namespace Tests

        [Test]
        public void ARouteKeyCannotBecomeAGroupTest()
        {
            m_registry.Register<PlainViewModel>("x");

            Assert.Throws<InvalidOperationException>(() => m_registry.RegisterGroup("x", "a"));
            Assert.Throws<InvalidOperationException>(() => m_registry.AddToGroup("x", "a"));
        }

        [Test]
        public void AGroupKeyCannotBecomeARouteTest()
        {
            m_registry.RegisterGroup("x", "a");

            Assert.Throws<InvalidOperationException>(() => m_registry.Register<PlainViewModel>("x"));
        }

        [Test]
        public void ContainsAndContainsGroupAreNeverBothTrueTest()
        {
            m_registry.Register<PlainViewModel>("route");
            m_registry.RegisterGroup("group", "route");

            Assert.That(m_registry.Contains("route") && m_registry.ContainsGroup("route"), Is.False);
            Assert.That(m_registry.Contains("group") && m_registry.ContainsGroup("group"), Is.False);
            Assert.That(m_registry.Contains("route"), Is.True);
            Assert.That(m_registry.ContainsGroup("group"), Is.True);
        }

        #endregion

        #region Nesting Tests

        [Test]
        public void AGroupCannotListAGroupTest()
        {
            m_registry.RegisterGroup("inner", "a");

            Assert.Throws<InvalidOperationException>(() => m_registry.RegisterGroup("outer", "inner"));
            Assert.Throws<InvalidOperationException>(() => m_registry.RegisterGroup("outer", "b", new[] { "b", "inner" }));
            Assert.Throws<InvalidOperationException>(() => m_registry.AddToGroup("outer", "inner"));
        }

        [Test]
        public void AMemberCannotBecomeAGroupTest()
        {
            m_registry.RegisterGroup("outer", "inner");

            Assert.Throws<InvalidOperationException>(() => m_registry.RegisterGroup("inner", "a"));
            Assert.Throws<InvalidOperationException>(() => m_registry.AddToGroup("inner", "a"));
        }

        [Test]
        public void AGroupCannotContainItselfTest()
        {
            Assert.Throws<ArgumentException>(() => m_registry.RegisterGroup("g", "g"));
            Assert.Throws<ArgumentException>(() => m_registry.RegisterGroup("g", "a", new[] { "a", "g" }));
        }

        [Test]
        public void AFailedDeclarationLeavesNothingBehindTest()
        {
            m_registry.RegisterGroup("inner", "a");

            Assert.Throws<InvalidOperationException>(() => m_registry.RegisterGroup("outer", "inner"));

            Assert.That(m_registry.ContainsGroup("outer"), Is.False);
            Assert.That(m_registry.Groups, Has.Count.EqualTo(1));
        }

        #endregion
    }
}
