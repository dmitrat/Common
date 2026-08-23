using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Utils;
using OutWit.Common.MVVM.Navigation.WPF.Controls;
using OutWit.Common.MVVM.Navigation.WPF.Utils;

namespace OutWit.Common.MVVM.Navigation.WPF.Services
{
    /// <summary>
    /// Maps view models to views: the <see cref="IViewRegistry"/> first (the only path under
    /// trimming or AOT), then a naming convention searched in the view model's own assembly.
    /// Serves as the core's <see cref="IViewFactory"/> and as a <see cref="DataTemplateSelector"/>
    /// whose templates hold a <see cref="ViewPresenter"/> — so DI-built views work inside any
    /// ContentControl or ItemsControl: <c>ContentTemplateSelector="{StaticResource OutWit.Navigation.ViewLocator}"</c>.
    /// Created from DI so that views may take dependencies.
    /// </summary>
    public sealed class ViewLocator : DataTemplateSelector, IViewFactory
    {
        #region Constants

        /// <summary>
        /// The application resource key <c>UseWpfViewLocator()</c> stores the locator under.
        /// </summary>
        public const string RESOURCE_KEY = "OutWit.Navigation.ViewLocator";

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>?> m_conventionCache = new();
        private readonly ConcurrentDictionary<Type, DataTemplate> m_templates = new();

        private readonly IServiceProvider m_provider;
        private readonly IViewRegistry m_registry;
        private readonly ViewNamingConvention m_convention;
        private readonly ILogger<ViewLocator>? m_logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the locator. Resolved from DI by <c>services.AddWpfNavigation()</c>.
        /// </summary>
        /// <param name="provider">The service provider views are created from.</param>
        /// <param name="registry">The explicit view registry.</param>
        /// <param name="options">What AddWpfNavigation collected; null means defaults.</param>
        /// <param name="logger">Optional logger.</param>
        public ViewLocator(IServiceProvider provider,
                           IViewRegistry registry,
                           WpfNavigationOptions? options = null,
                           ILogger<ViewLocator>? logger = null)
        {
            m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
            m_registry = registry ?? throw new ArgumentNullException(nameof(registry));
            m_convention = options?.ViewConvention ?? ViewNamingConvention.ViewModelsToViews;
            m_logger = logger;
        }

        #endregion

        #region DataTemplateSelector

        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            if (item == null || !CanBuild(item.GetType()))
                return null;

            return m_templates.GetOrAdd(item.GetType(), CreateTemplate);
        }

        #endregion

        #region IViewFactory

        public bool CanBuild(Type viewModelType)
        {
            return viewModelType != null && Resolve(viewModelType) != null;
        }

        public object Build(object viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            var factory = Resolve(viewModel.GetType())
                          ?? throw new InvalidOperationException(
                              $"No view is known for {viewModel.GetType().FullName}. Register one in IViewRegistry or follow the {m_convention} naming convention.");

            var view = factory(m_provider);

            if (view is FrameworkElement element)
                element.DataContext = viewModel;

            return view;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Finds the view type the naming convention maps a view model type to, or null.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <returns>The view type, or null.</returns>
        public Type? FindViewTypeByConvention(Type viewModelType)
        {
            if (viewModelType == null)
                throw new ArgumentNullException(nameof(viewModelType));

            return ViewNaming.FindViewType(viewModelType, m_convention, type => typeof(FrameworkElement).IsAssignableFrom(type));
        }

        /// <summary>
        /// The locator <c>UseWpfViewLocator()</c> put into the application resources, or null.
        /// </summary>
        /// <returns>The locator.</returns>
        public static IViewFactory? FindInApplication()
        {
            return Application.Current?.TryFindResource(RESOURCE_KEY) as IViewFactory;
        }

        private Func<IServiceProvider, object>? Resolve(Type viewModelType)
        {
            if (m_registry.TryGetFactory(viewModelType, out var registered))
                return registered;

            return m_conventionCache.GetOrAdd(viewModelType, ResolveByConvention);
        }

        private Func<IServiceProvider, object>? ResolveByConvention(Type viewModelType)
        {
            var viewType = FindViewTypeByConvention(viewModelType);
            if (viewType == null)
            {
                m_logger?.LogDebug("No view found by convention for {ViewModel}", viewModelType.FullName);
                return null;
            }

            return provider => ActivatorUtilities.CreateInstance(provider, viewType);
        }

        private DataTemplate CreateTemplate(Type viewModelType)
        {
            var presenter = new FrameworkElementFactory(typeof(ViewPresenter));
            presenter.SetValue(ViewPresenter.ViewFactoryProperty, this);
            presenter.SetBinding(ViewPresenter.ViewModelProperty, new Binding());

            var template = new DataTemplate(viewModelType) { VisualTree = presenter };
            template.Seal();

            return template;
        }

        #endregion
    }
}
