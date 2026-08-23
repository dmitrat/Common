using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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

        private CancellationTokenSource? m_transition;
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
                Show(null);
                return;
            }

            if (Content is FrameworkElement current && ReferenceEquals(current.DataContext, viewModel))
                return;

            var factory = ViewFactory ?? ViewLocator.FindInApplication();

            if (factory == null)
            {
                // no locator in sight: hand the view model to the ContentPresenter and let the
                // application's DataTemplates do what they can — no view caching on this path
                Show(viewModel);
                return;
            }

            if (KeepViews && m_views.TryGetValue(viewModel, out var cached))
            {
                Show(cached);
                return;
            }

            var view = ViewPresenter.BuildView(factory, viewModel);

            if (KeepViews && Outlet?.Route?.Mode == NavigationRouteMode.Cached)
                m_views.AddOrUpdate(viewModel, view);

            Show(view);
        }

        /// <summary>
        /// Puts the view up, fading through it when a duration is set. The fade is on this
        /// control rather than between two presenters: a cached view is a single element that
        /// cannot be in two places at once, and the second presenter is exactly what would
        /// try to put it there.
        /// </summary>
        private void Show(object? view)
        {
            m_transition?.Cancel();
            m_transition = null;

            if (TransitionDuration <= TimeSpan.Zero || !IsVisible)
            {
                ClearFade();
                Content = view;

                return;
            }

            var cancellation = new CancellationTokenSource();
            m_transition = cancellation;

            _ = FadeAsync(view, cancellation);
        }

        private async Task FadeAsync(object? view, CancellationTokenSource cancellation)
        {
            var half = TimeSpan.FromMilliseconds(TransitionDuration.TotalMilliseconds / 2);

            try
            {
                if (Content != null)
                    await FadeAsync(1, 0, half, cancellation.Token);

                if (cancellation.IsCancellationRequested)
                    return;

                Content = view;

                await FadeAsync(0, 1, half, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // a newer navigation took over mid-fade; it owns the opacity now
            }
            finally
            {
                if (ReferenceEquals(m_transition, cancellation))
                {
                    m_transition = null;
                    ClearFade();
                }

                cancellation.Dispose();
            }
        }

        private async Task FadeAsync(double from, double to, TimeSpan duration, CancellationToken cancellation)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var animation = new DoubleAnimation(from, to, new Duration(duration))
            {
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.Completed += (_, _) => completion.TrySetResult(true);

            using (cancellation.Register(() => completion.TrySetCanceled()))
            {
                BeginAnimation(OpacityProperty, animation);

                await completion.Task;
            }
        }

        /// <summary>
        /// Releases the animation's hold on Opacity. Without this the property stays where the
        /// animation left it and every later assignment is silently ignored.
        /// </summary>
        private void ClearFade()
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = 1;
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

        /// <summary>
        /// How long to fade from one screen to the next. Zero — the default — swaps them
        /// outright. Fast navigation is safe: a fade that is overtaken hands the opacity to
        /// whichever navigation arrived last.
        /// </summary>
        [StyledProperty]
        public TimeSpan TransitionDuration { get; set; }

        #endregion
    }
}
