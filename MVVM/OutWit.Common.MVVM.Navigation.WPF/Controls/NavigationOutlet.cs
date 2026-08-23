using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Attributes;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.WPF.Services;

namespace OutWit.Common.MVVM.Navigation.WPF.Controls
{
    /// <summary>
    /// Hosts an <see cref="INavigationOutlet"/>. Owns the views: for Cached routes it keeps the
    /// view of each view model alive across navigations, so scroll positions, column widths
    /// and expensive controls survive the way they did in a Prism region; for Transient
    /// routes the view goes with the view model. <c>Content</c> is the view, not the view model.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;n:NavigationOutlet Outlet="{Binding Main}" /&gt;
    /// </code>
    /// </example>
    public partial class NavigationOutlet : ContentControl
    {
        #region Fields

        private readonly ConditionalWeakTable<object, FrameworkElement> m_views = new();

        private INavigationOutlet? m_subscribed;

        #endregion

        #region Constructors

        public NavigationOutlet()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #endregion

        #region Functions

        private void Subscribe(INavigationOutlet? outlet)
        {
            if (ReferenceEquals(m_subscribed, outlet))
                return;

            if (m_subscribed != null)
                m_subscribed.PropertyChanged -= OnOutletPropertyChanged;

            m_subscribed = outlet;

            if (m_subscribed != null)
                m_subscribed.PropertyChanged += OnOutletPropertyChanged;
        }

        private void Refresh()
        {
            var viewModel = Outlet?.Content;

            if (viewModel == null)
            {
                Content = null;
                return;
            }

            if (Content is FrameworkElement current && ReferenceEquals(current.DataContext, viewModel))
                return;

            var factory = ViewFactory ?? ViewLocator.FindInApplication();

            if (factory == null)
            {
                // no locator in sight: hand the view model to the ContentPresenter and let the
                // application's DataTemplates do what they can — no view caching on this path
                Content = viewModel;
                return;
            }

            if (KeepViews && m_views.TryGetValue(viewModel, out var cached))
            {
                Content = cached;
                return;
            }

            var view = ViewPresenter.BuildView(factory, viewModel);

            if (KeepViews && Outlet?.Route?.Mode == NavigationRouteMode.Cached)
                m_views.AddOrUpdate(viewModel, view);

            Content = view;
        }

        #endregion

        #region Event Handlers

        private static void OnOutletChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not NavigationOutlet control)
                return;

            control.Subscribe(e.NewValue as INavigationOutlet);
            control.Refresh();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Subscribe(Outlet);
            Refresh();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Subscribe(null);
        }

        private void OnOutletPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is null or nameof(INavigationOutlet.Content))
                Refresh();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The outlet to host — the object itself, obtained from <see cref="INavigationService"/>, not a name.
        /// </summary>
        [StyledProperty]
        public INavigationOutlet? Outlet { get; set; }

        /// <summary>
        /// Keep the view of each Cached view model alive across navigations. Default true;
        /// false rebuilds the view on every navigation.
        /// </summary>
        [StyledProperty(DefaultValue = true)]
        public bool KeepViews { get; set; }

        /// <summary>
        /// The factory views are built with. When null, the <see cref="ViewLocator"/> registered
        /// in the application resources by <c>UseWpfViewLocator()</c> is used; when there is none
        /// either, the view model is handed to the ContentPresenter and views are not cached.
        /// </summary>
        [StyledProperty]
        public IViewFactory? ViewFactory { get; set; }

        #endregion
    }
}
