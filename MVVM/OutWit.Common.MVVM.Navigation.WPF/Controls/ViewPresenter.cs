using System;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Attributes;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.WPF.Services;

namespace OutWit.Common.MVVM.Navigation.WPF.Controls
{
    /// <summary>
    /// Shows the view of any view model: nested content, a zone widget, the inside of a
    /// dialog. Gives WPF what Avalonia gets from <c>Application.DataTemplates</c> —
    /// DI-built views through the <see cref="IViewFactory"/>. Also what the
    /// <see cref="ViewLocator"/> template selector puts inside its templates.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;n:ViewPresenter ViewModel="{Binding Widget}" /&gt;
    /// </code>
    /// </example>
    public partial class ViewPresenter : ContentControl
    {
        #region Functions

        private void Refresh()
        {
            var viewModel = ViewModel;

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
                Content = viewModel;
                return;
            }

            Content = BuildView(factory, viewModel);
        }

        internal static FrameworkElement BuildView(IViewFactory factory, object viewModel)
        {
            try
            {
                return factory.Build(viewModel) as FrameworkElement
                       ?? new TextBlock { Text = $"View for {viewModel.GetType().FullName} is not a FrameworkElement" };
            }
            catch (Exception e)
            {
                return new TextBlock { Text = $"View not found: {viewModel.GetType().FullName} ({e.Message})" };
            }
        }

        #endregion

        #region Event Handlers

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ViewPresenter)?.Refresh();
        }

        private static void OnViewFactoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ViewPresenter)?.Refresh();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The view model to show.
        /// </summary>
        [StyledProperty]
        public object? ViewModel { get; set; }

        /// <summary>
        /// The factory views are built with. When null, the <see cref="ViewLocator"/> registered
        /// in the application resources by <c>UseWpfViewLocator()</c> is used; when there is none
        /// either, the view model is handed to the ContentPresenter as is.
        /// </summary>
        [StyledProperty]
        public IViewFactory? ViewFactory { get; set; }

        #endregion
    }
}
