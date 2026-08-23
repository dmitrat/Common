using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using OutWit.Common.MVVM.Attributes;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Controls
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

        private readonly ConditionalWeakTable<object, Control> m_views = new();

        private INavigationOutlet? m_subscribed;

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

            if (Content is Control current && ReferenceEquals(current.DataContext, viewModel))
                return;

            var factory = ViewFactory ?? FindFactory();

            if (factory == null)
            {
                // no locator in sight: hand the view model to the ContentPresenter and let
                // Application.DataTemplates do what they can — no view caching on this path
                Content = viewModel;
                return;
            }

            if (KeepViews && m_views.TryGetValue(viewModel, out var cached))
            {
                Content = cached;
                return;
            }

            var view = BuildView(factory, viewModel);

            if (KeepViews && Outlet?.Route?.Mode == NavigationRouteMode.Cached)
                m_views.AddOrUpdate(viewModel, view);

            Content = view;
        }

        private static Control BuildView(IViewFactory factory, object viewModel)
        {
            try
            {
                return factory.Build(viewModel) as Control
                       ?? new TextBlock { Text = $"View for {viewModel.GetType().FullName} is not a Control" };
            }
            catch (Exception e)
            {
                return new TextBlock { Text = $"View not found: {viewModel.GetType().FullName} ({e.Message})" };
            }
        }

        private static IViewFactory? FindFactory()
        {
            return Application.Current?.DataTemplates.OfType<IViewFactory>().FirstOrDefault();
        }

        #endregion

        #region Control

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            Subscribe(Outlet);
            Refresh();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            Subscribe(null);
        }

        #endregion

        #region Event Handlers

        private static void OnOutletChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is not NavigationOutlet control)
                return;

            control.Subscribe(e.NewValue as INavigationOutlet);
            control.Refresh();
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
        /// The factory views are built with. When null, the <see cref="ViewLocator"/> found in
        /// <c>Application.DataTemplates</c> is used; when there is none either, the view model
        /// is handed to the ContentPresenter and views are not cached.
        /// </summary>
        [StyledProperty]
        public IViewFactory? ViewFactory { get; set; }

        #endregion
    }
}
