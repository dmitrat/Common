using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Avalonia.Services;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock.ViewModels;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock.Views;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Services
{
    [TestFixture]
    public class ViewLocatorTests
    {
        #region Convention Tests

        [Test]
        public void ViewModelsToViewsConventionFindsViewInViewsNamespaceTest()
        {
            using var provider = AvaloniaTestHost.Build();
            var locator = provider.GetRequiredService<ViewLocator>();

            Assert.That(locator.FindViewTypeByConvention(typeof(SampleViewModel)), Is.EqualTo(typeof(SampleView)));
            Assert.That(locator.CanBuild(typeof(SampleViewModel)), Is.True);
        }

        [Test]
        public void ViewModelsToViewsConventionFallsBackToSameNamespaceTest()
        {
            using var provider = AvaloniaTestHost.Build();
            var locator = provider.GetRequiredService<ViewLocator>();

            Assert.That(locator.FindViewTypeByConvention(typeof(SameNamespaceViewModel)), Is.EqualTo(typeof(SameNamespaceView)));
        }

        [Test]
        public void SameNamespaceConventionDoesNotLookIntoViewsNamespaceTest()
        {
            using var provider = AvaloniaTestHost.Build(configureAvalonia: o => o.ViewConvention = ViewNamingConvention.SameNamespace);
            var locator = provider.GetRequiredService<ViewLocator>();

            Assert.That(locator.FindViewTypeByConvention(typeof(SampleViewModel)), Is.Null);
            Assert.That(locator.FindViewTypeByConvention(typeof(SameNamespaceViewModel)), Is.EqualTo(typeof(SameNamespaceView)));
        }

        [Test]
        public void NoConventionUsesRegistryOnlyTest()
        {
            using var provider = AvaloniaTestHost.Build(configureAvalonia: o => o.ViewConvention = ViewNamingConvention.None);
            var locator = provider.GetRequiredService<ViewLocator>();

            Assert.That(locator.CanBuild(typeof(SampleViewModel)), Is.False);

            provider.GetRequiredService<IViewRegistry>().Register<SampleViewModel, SampleView>();

            Assert.That(locator.CanBuild(typeof(SampleViewModel)), Is.True);
        }

        [Test]
        public void UnknownViewModelIsNotMatchedTest()
        {
            using var provider = AvaloniaTestHost.Build();
            var locator = provider.GetRequiredService<ViewLocator>();

            Assert.That(locator.CanBuild(typeof(OrphanViewModel)), Is.False);
            Assert.That(((IDataTemplate)locator).Match(new OrphanViewModel()), Is.False);
            Assert.That(((IDataTemplate)locator).Match(null), Is.False);
            Assert.That(((IDataTemplate)locator).Match(new SampleViewModel()), Is.True);
        }

        #endregion

        #region Registry Tests

        [Test]
        public void RegistryWinsOverConventionTest()
        {
            using var provider = AvaloniaTestHost.Build(nav => nav.AddView<SampleViewModel, TransientSampleView>());
            var locator = provider.GetRequiredService<ViewLocator>();

            var view = locator.Build(new SampleViewModel());

            Assert.That(view, Is.InstanceOf<TransientSampleView>());
        }

        [Test]
        public void RegistryFactoryIsUsedTest()
        {
            using var provider = AvaloniaTestHost.Build();
            var marker = new TextBlock();
            provider.GetRequiredService<IViewRegistry>().Register(typeof(OrphanViewModel), _ => marker);

            var view = provider.GetRequiredService<ViewLocator>().Build(new OrphanViewModel());

            Assert.That(view, Is.SameAs(marker));
        }

        #endregion

        #region Build Tests

        [Test]
        public void BuildSetsDataContextTest()
        {
            using var provider = AvaloniaTestHost.Build();
            var viewModel = new SampleViewModel();

            var view = (SampleView)provider.GetRequiredService<ViewLocator>().Build(viewModel);

            Assert.That(view.DataContext, Is.SameAs(viewModel));
        }

        [Test]
        public void BuildResolvesViewDependenciesFromContainerTest()
        {
            using var provider = AvaloniaTestHost.Build();

            var view = (InjectedView)provider.GetRequiredService<ViewLocator>().Build(new InjectedViewModel());

            Assert.That(view.Dependency, Is.SameAs(provider.GetRequiredService<ViewDependency>()));
        }

        [Test]
        public void BuildThrowsForUnknownViewModelTest()
        {
            using var provider = AvaloniaTestHost.Build();

            Assert.That(() => provider.GetRequiredService<ViewLocator>().Build(new OrphanViewModel()), Throws.InvalidOperationException);
        }

        [Test]
        public void DataTemplateBuildReturnsPlaceholderForUnknownViewModelTest()
        {
            using var provider = AvaloniaTestHost.Build();
            IDataTemplate template = provider.GetRequiredService<ViewLocator>();

            var control = template.Build(new OrphanViewModel());

            Assert.That(control, Is.InstanceOf<TextBlock>());
            Assert.That(((TextBlock)control!).Text, Does.Contain(nameof(OrphanViewModel)));
            Assert.That(template.Build(null), Is.Null);
        }

        [Test]
        public void ViewLocatorIsTheViewFactoryTest()
        {
            using var provider = AvaloniaTestHost.Build();

            Assert.That(provider.GetRequiredService<IViewFactory>(), Is.SameAs(provider.GetRequiredService<ViewLocator>()));
        }

        #endregion
    }
}
