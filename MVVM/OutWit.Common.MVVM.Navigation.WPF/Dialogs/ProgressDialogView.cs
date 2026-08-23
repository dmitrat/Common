using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using OutWit.Common.MVVM.Navigation.ViewModels;

namespace OutWit.Common.MVVM.Navigation.WPF.Dialogs
{
    /// <summary>
    /// The view a progress dialog gets when the application has not registered one of its
    /// own. Built in code rather than XAML so the package ships no resource dictionary; an
    /// application that wants more than a restyle registers its own view for
    /// <see cref="ProgressDialogViewModel"/>.
    /// </summary>
    public class ProgressDialogView : UserControl
    {
        #region Constructors

        public ProgressDialogView()
        {
            Width = 380;
            Content = BuildContent();
        }

        #endregion

        #region Initialization

        private static UIElement BuildContent()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock { FontSize = 16, FontWeight = FontWeights.SemiBold };
            title.SetBinding(TextBlock.TextProperty, new Binding(nameof(ProgressDialogViewModel.Title)));
            panel.Children.Add(title);

            var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Opacity = 0.75, Margin = new Thickness(0, 12, 0, 0) };
            status.SetBinding(TextBlock.TextProperty, new Binding(nameof(ProgressDialogViewModel.Status)));
            status.SetBinding(UIElement.VisibilityProperty,
                new Binding(nameof(ProgressDialogViewModel.Status)) { Converter = new StringNotEmptyToVisibilityConverter() });
            panel.Children.Add(status);

            var bar = new ProgressBar { Minimum = 0, Maximum = 1, Height = 6, Margin = new Thickness(0, 12, 0, 0) };
            bar.SetBinding(RangeBase.ValueProperty, new Binding(nameof(ProgressDialogViewModel.Progress)));
            bar.SetBinding(ProgressBar.IsIndeterminateProperty, new Binding(nameof(ProgressDialogViewModel.IsIndeterminate)));
            panel.Children.Add(bar);

            var cancel = new Button
            {
                Content = "Cancel",
                Padding = new Thickness(12, 4, 12, 4),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            cancel.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(ProgressDialogViewModel.CancelCommand)));
            cancel.SetBinding(UIElement.VisibilityProperty,
                new Binding(nameof(ProgressDialogViewModel.IsCancellable)) { Converter = new BooleanToVisibilityConverter() });
            panel.Children.Add(cancel);

            return panel;
        }

        #endregion

        #region Classes

        /// <summary>
        /// Hides an empty status line rather than leaving a gap where one would be.
        /// </summary>
        private sealed class StringNotEmptyToVisibilityConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        #endregion
    }
}
