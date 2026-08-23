using System;
using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Avalonia.Utils;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Utils;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Services
{
    /// <summary>
    /// Maps view models to views: the <see cref="IViewRegistry"/> first (the only path under
    /// trimming or AOT), then a naming convention searched in the view model's own assembly.
    /// Serves as an <see cref="IDataTemplate"/> for <c>Application.DataTemplates</c> and as the
    /// core's <see cref="IViewFactory"/>. Created from DI so that views may take dependencies.
    /// </summary>
    public sealed class ViewLocator : IDataTemplate, IViewFactory
    {
        #region Fields

        private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>?> m_conventionCache = new();

        private readonly IServiceProvider m_provider;
        private readonly IViewRegistry m_registry;
        private readonly ViewNamingConvention m_convention;
        private readonly ILogger<ViewLocator>? m_logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the locator. Resolved from DI by <c>services.AddAvaloniaNavigation()</c>.
        /// </summary>
        /// <param name="provider">The service provider views are created from.</param>
        /// <param name="registry">The explicit view registry.</param>
        /// <param name="options">What AddAvaloniaNavigation collected; null means defaults.</param>
        /// <param name="logger">Optional logger.</param>
        public ViewLocator(IServiceProvider provider,
                           IViewRegistry registry,
                           AvaloniaNavigationOptions? options = null,
                           ILogger<ViewLocator>? logger = null)
        {
            m_provider = provider ?? throw new ArgumentNullException(nameof(provider));
            m_registry = registry ?? throw new ArgumentNullException(nameof(registry));
            m_convention = options?.ViewConvention ?? ViewNamingConvention.ViewModelsToViews;
            m_logger = logger;
        }

        #endregion

        #region IDataTemplate

        Control? ITemplate<object?, Control?>.Build(object? param)
        {
            if (param == null)
                return null;

            try
            {
                return Build(param) as Control
                       ?? Placeholder($"View for {param.GetType().FullName} is not a Control");
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Building a view for {ViewModel} failed", param.GetType().FullName);
                return Placeholder($"View not found: {param.GetType().FullName}");
            }
        }

        public bool Match(object? data)
        {
            return data != null && CanBuild(data.GetType());
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

            if (view is StyledElement styled)
                styled.DataContext = viewModel;

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

            return ViewNaming.FindViewType(viewModelType, m_convention, type => typeof(Control).IsAssignableFrom(type));
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
                return null;

            return provider => ActivatorUtilities.CreateInstance(provider, viewType);
        }

        private static Control Placeholder(string text)
        {
            return new TextBlock { Text = text };
        }

        #endregion
    }
}
