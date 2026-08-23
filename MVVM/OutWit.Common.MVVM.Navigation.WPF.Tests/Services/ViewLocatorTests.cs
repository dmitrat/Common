using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.WPF.Controls;
using OutWit.Common.MVVM.Navigation.WPF.Services;
using OutWit.Common.MVVM.Navigation.WPF.Tests.Mock;
using OutWit.Common.MVVM.Navigation.WPF.Tests.Mock.ViewModels;
using OutWit.Common.MVVM.Navigation.WPF.Tests.Mock.Views;

namespace OutWit.Common.MVVM.Navigation.WPF.Tests.Services
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ViewLocatorTests
    {
        #region Convention Tests

        [Test]
        public void ViewModelsToViewsConventionFindsViewTest()
        {
            using var provider = WpfTestHost.Build();
            var locator = provider.GetRequiredService<ViewLocator>();

            Assert.That(locator.FindViewTypeByConvention(typeof(SampleViewModel)), Is.EqualTo(typeof(SampleView)));
            Assert.That(locator.CanBuild(typeof(SampleViewModel)), Is.True);
            Assert.That(locator.CanBuild(typeof(OrphanViewModel)), Is.False);
        }

        [Test]
        public void NoConventionUsesRegistryOnlyTest()
        {
            using var provider = WpfTestHost.Build(configureWpf: o => o.ViewConvention = ViewNamingConvention.None);
            var locator = provider.GetRequiredService<ViewLocator>();

            Assert.That(locator.CanBuild(typeof(SampleViewModel)), Is.False);

            provider.GetRequiredService<IViewRegistry>().Register<SampleViewModel, SampleView>();

            Assert.That(locator.CanBuild(typeof(SampleViewModel)), Is.True);
        }

        #endregion

        #region Build Tests

        [Test]
        public void BuildSetsDataContextAndResolvesDependenciesTest()
        {
            using var provider = WpfTestHost.Build();
            var locator = provider.GetRequiredService<ViewLocator>();
            var viewModel = new InjectedViewModel();

            var view = (InjectedView)locator.Build(viewModel);

            Assert.That(view.DataContext, Is.SameAs(viewModel));
            Assert.That(view.Dependency, Is.SameAs(provider.GetRequiredService<ViewDependency>()));
        }

        [Test]
        public void BuildThrowsForUnknownViewModelTest()
        {
            using var provider = WpfTestHost.Build();

            Assert.That(() => provider.GetRequiredService<ViewLocator>().Build(new OrphanViewModel()), Throws.InvalidOperationException);
        }

        [Test]
        public void RegistryWinsOverConventionTest()
        {
            using var provider = WpfTestHost.Build(nav => nav.AddView<SampleViewModel, TransientSampleView>());

            var view = provider.GetRequiredService<ViewLocator>().Build(new SampleViewModel());

            Assert.That(view, Is.InstanceOf<TransientSampleView>());
        }

        #endregion

        #region Template Selector Tests

        [Test]
        public void SelectTemplateReturnsTemplateForKnownViewModelOnlyTest()
        {
            using var provider = WpfTestHost.Build();
            var locator = provider.GetRequiredService<ViewLocator>();
            var container = new ContentControl();

            var known = locator.SelectTemplate(new SampleViewModel(), container);
            var unknown = locator.SelectTemplate(new OrphanViewModel(), container);
            var again = locator.SelectTemplate(new SampleViewModel(), container);

            Assert.That(known, Is.Not.Null);
            Assert.That(known!.DataType, Is.EqualTo(typeof(SampleViewModel)));
            Assert.That(unknown, Is.Null);
            Assert.That(again, Is.SameAs(known));
            Assert.That(locator.SelectTemplate(null, container), Is.Null);
        }

        [Test]
        public void TemplateBuildsTheViewThroughViewPresenterTest()
        {
            using var provider = WpfTestHost.Build();
            var locator = provider.GetRequiredService<ViewLocator>();
            var viewModel = new SampleViewModel();
            var content = new ContentControl { Content = viewModel, ContentTemplateSelector = locator };
            var window = new Window { Content = content, Width = 100, Height = 100, ShowInTaskbar = false, WindowStyle = WindowStyle.None };

            window.Show();
            WpfTestHost.DoEvents();

            try
            {
                var presenter = FindChild<ViewPresenter>(content);

                Assert.That(presenter, Is.Not.Null);
                Assert.That(presenter!.ViewModel, Is.SameAs(viewModel));
                Assert.That(presenter.Content, Is.InstanceOf<SampleView>());
                Assert.That(((SampleView)presenter.Content!).DataContext, Is.SameAs(viewModel));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ViewLocatorIsTheViewFactoryTest()
        {
            using var provider = WpfTestHost.Build();

            Assert.That(provider.GetRequiredService<IViewFactory>(), Is.SameAs(provider.GetRequiredService<ViewLocator>()));
        }

        #endregion

        #region Tools

        private static TChild? FindChild<TChild>(DependencyObject parent)
            where TChild : DependencyObject
        {
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is TChild typed)
                    return typed;

                var nested = FindChild<TChild>(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        #endregion
    }
}
