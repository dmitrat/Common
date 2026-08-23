using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using OutWit.Common.MVVM.Navigation.ViewModels;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Dialogs
{
    /// <summary>
    /// The view a progress dialog gets when the application has not registered one of its
    /// own. Built in code rather than XAML so the package ships no resources; it carries the
    /// <c>navigation-progress-dialog</c> class, and an application that wants more than a
    /// restyle registers its own view for <see cref="ProgressDialogViewModel"/>.
    /// </summary>
    public class ProgressDialogView : UserControl
    {
        #region Constants

        public const string CLASS_NAME = "navigation-progress-dialog";

        #endregion

        #region Constructors

        public ProgressDialogView()
        {
            Classes.Add(CLASS_NAME);
            Width = 380;

            Content = BuildContent();
        }

        #endregion

        #region Initialization

        private static Control BuildContent()
        {
            var title = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeight.SemiBold
            };
            title.Bind(TextBlock.TextProperty, new Binding(nameof(ProgressDialogViewModel.Title)));

            var status = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.75
            };
            status.Bind(TextBlock.TextProperty, new Binding(nameof(ProgressDialogViewModel.Status)));
            status.Bind(IsVisibleProperty, new Binding(nameof(ProgressDialogViewModel.Status)) { Converter = StringNotEmptyConverter.INSTANCE });

            var bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 1
            };
            bar.Bind(RangeBase.ValueProperty, new Binding(nameof(ProgressDialogViewModel.Progress)));
            bar.Bind(ProgressBar.IsIndeterminateProperty, new Binding(nameof(ProgressDialogViewModel.IsIndeterminate)));

            var cancel = new Button
            {
                Content = "Cancel",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            cancel.Bind(Button.CommandProperty, new Binding(nameof(ProgressDialogViewModel.CancelCommand)));
            cancel.Bind(IsVisibleProperty, new Binding(nameof(ProgressDialogViewModel.IsCancellable)));

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(title);
            panel.Children.Add(status);
            panel.Children.Add(bar);
            panel.Children.Add(cancel);

            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Child = panel
            };
        }

        #endregion
    }
}
