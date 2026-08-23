using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;
using OutWit.Common.MVVM.Navigation.Utils;

namespace OutWit.Common.MVVM.Navigation.Tests.Utils
{
    [TestFixture]
    public class NavigationValidationTests
    {
        #region Route Tests

        [Test]
        public void ValidateReportsRouteWithoutViewTest()
        {
            using var provider = NavigationTestHost.Build(nav => nav.AddRoute<PlainViewModel>("plain"));

            var problems = provider.ValidateNavigation();

            Assert.That(problems, Has.Count.EqualTo(1));
            Assert.That(problems[0], Does.Contain("plain").And.Contain("no view"));
        }

        [Test]
        public void ValidatePassesWhenViewIsRegisteredTest()
        {
            using var provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<PlainViewModel>("plain");
                nav.AddView<PlainViewModel, FakeView>();
            });

            Assert.That(provider.ValidateNavigation(), Is.Empty);
        }

        [Test]
        public void ValidateUsesViewFactoryWhenRegisteredTest()
        {
            using var provider = NavigationTestHost.Build(
                nav => nav.AddRoute<PlainViewModel>("plain"),
                services => services.AddSingleton<IViewFactory>(new FakeViewFactory(typeof(PlainViewModel))));

            Assert.That(provider.ValidateNavigation(), Is.Empty);
        }

        [Test]
        public void ValidateReportsUndeclaredOutletTest()
        {
            using var provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<PlainViewModel>("plain", outlet: "Ghost");
                nav.AddView<PlainViewModel, FakeView>();
            });

            var problems = provider.ValidateNavigation();

            Assert.That(problems, Has.Count.EqualTo(1));
            Assert.That(problems[0], Does.Contain("Ghost"));
        }

        [Test]
        public void ValidateThrowsWhenAskedTest()
        {
            using var provider = NavigationTestHost.Build(nav => nav.AddRoute<PlainViewModel>("plain"));

            Assert.That(() => provider.ValidateNavigation(throwOnProblems: true), Throws.InvalidOperationException);
        }

        [Test]
        public void ValidateReportsNotifyWithoutNotifyPropertyChangedTest()
        {
            using var provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<SilentViewModel>("silent");
                nav.AddView<SilentViewModel, FakeView>();
            });

            var problems = provider.ValidateNavigation();

            Assert.That(problems, Has.Count.EqualTo(1));
            Assert.That(problems[0], Does.Contain(nameof(SilentViewModel.Caption)).And.Contain("INotifyPropertyChanged"));
        }

        [Test]
        public void ValidateAcceptsNotifyOnANotifyingBaseTest()
        {
            using var provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<TalkativeViewModel>("talkative");
                nav.AddView<TalkativeViewModel, FakeView>();
            });

            Assert.That(provider.ValidateNavigation(), Is.Empty);
        }

        #endregion

        #region Contribution Tests

        [Test]
        public void ValidateReportsContributionWithUnknownRouteOrOutletTest()
        {
            using var provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<PlainViewModel>("plain");
                nav.AddView<PlainViewModel, FakeView>();
            });

            var contributions = provider.GetRequiredService<IContributionRegistry>();
            contributions.Add(new ContributionItem { Zone = "Bar", Key = "ok", RouteKey = "plain" });
            contributions.Add(new ContributionItem { Zone = "Bar", Key = "bad-route", RouteKey = "missing" });
            contributions.Add(new ContributionItem { Zone = "Bar", Key = "bad-outlet", RouteKey = "plain", Outlet = "Ghost" });
            contributions.Add(new ContributionItem { Zone = "Bar", Key = "nested-bad", ParentKey = "ok", RouteKey = "missing-too" });

            var problems = provider.ValidateNavigation();

            Assert.That(problems, Has.Count.EqualTo(3));
            Assert.That(problems.Count(problem => problem.Contains("bad-route")), Is.EqualTo(1));
            Assert.That(problems.Count(problem => problem.Contains("bad-outlet")), Is.EqualTo(1));
            Assert.That(problems.Count(problem => problem.Contains("nested-bad")), Is.EqualTo(1));
        }

        #endregion

        #region Classes

        /// <summary>
        /// [Notify] on a class that does not implement INotifyPropertyChanged: the aspect has
        /// nothing to raise on, so the property binds once and never updates — silently.
        /// </summary>
        private sealed class SilentViewModel
        {
            [OutWit.Common.Aspects.Notify]
            public string? Caption { get; set; }
        }

        private sealed class TalkativeViewModel : OutWit.Common.Abstract.NotifyPropertyChangedBase
        {
            [OutWit.Common.Aspects.Notify]
            public string? Caption { get; set; }
        }

        #endregion
    }
}
