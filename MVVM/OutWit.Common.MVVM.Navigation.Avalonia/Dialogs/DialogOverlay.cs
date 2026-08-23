using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Dialogs
{
    /// <summary>
    /// The backdrop <see cref="DialogHostOverlay"/> puts into the overlay layer: a dimmed
    /// surface with the dialog view centred on it. A click on the backdrop outside the view,
    /// or Escape, raises <see cref="DismissRequested"/>. Styled as a ContentControl; target
    /// the <c>navigation-dialog-overlay</c> class to restyle it.
    /// </summary>
    public sealed class DialogOverlay : ContentControl
    {
        #region Constants

        public const string CLASS_NAME = "navigation-dialog-overlay";

        #endregion

        #region Events

        /// <summary>
        /// The user asked to close the dialog from the UI.
        /// </summary>
        public event Action? DismissRequested;

        #endregion

        #region Constructors

        public DialogOverlay()
        {
            Classes.Add(CLASS_NAME);
            Background = new SolidColorBrush(Colors.Black, 0.4);
            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
            Focusable = true;
        }

        #endregion

        #region Control

        protected override Type StyleKeyOverride => typeof(ContentControl);

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.Source is Visual source && Content is Visual content && (ReferenceEquals(source, content) || content.IsVisualAncestorOf(source)))
                return;

            e.Handled = true;
            DismissRequested?.Invoke();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key != Key.Escape || e.Handled)
                return;

            e.Handled = true;
            DismissRequested?.Invoke();
        }

        #endregion
    }
}
