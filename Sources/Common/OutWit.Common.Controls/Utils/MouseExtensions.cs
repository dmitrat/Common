using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace OutWit.Common.Controls.Utils
{
    public static class MouseExtensions
    {
        public static bool NoButtonPressed(this MouseEventArgs me, ModifierKeys? modifier = null)
        {
            return me.LeftButton == MouseButtonState.Released &&
                   me.MiddleButton == MouseButtonState.Released &&
                   me.RightButton == MouseButtonState.Released &&
                   (modifier == null || Keyboard.Modifiers == modifier);
        }

        public static bool LeftButtonPressed(this MouseEventArgs me, ModifierKeys? modifier = null)
        {
            return me.LeftButton == MouseButtonState.Pressed &&
                   me.MiddleButton == MouseButtonState.Released &&
                   me.RightButton == MouseButtonState.Released &&
                   (modifier == null || Keyboard.Modifiers == modifier);
        }

        public static bool MiddleButtonPressed(this MouseEventArgs me, ModifierKeys? modifier = null)
        {
            return me.LeftButton == MouseButtonState.Released &&
                   me.MiddleButton == MouseButtonState.Pressed &&
                   me.RightButton == MouseButtonState.Released &&
                   (modifier == null || Keyboard.Modifiers == modifier);

        }

        public static bool RightButtonPressed(this MouseEventArgs me, ModifierKeys? modifier = null)
        {
            return me.LeftButton == MouseButtonState.Released &&
                   me.MiddleButton == MouseButtonState.Released &&
                   me.RightButton == MouseButtonState.Pressed &&
                   (modifier == null || Keyboard.Modifiers == modifier);
            
        }

        public static bool LeftButtonBecomePressed(this MouseButtonEventArgs me, ModifierKeys? modifier = null)
        {
            return me.ChangedButton == MouseButton.Left &&
                   me.ButtonState == MouseButtonState.Pressed &&
                   (modifier == null || Keyboard.Modifiers == modifier);
        }

        public static bool LeftButtonBecomeReleased(this MouseButtonEventArgs me, ModifierKeys? modifier = null)
        {
            return me.ChangedButton == MouseButton.Left &&
                   me.ButtonState == MouseButtonState.Released &&
                   (modifier == null || Keyboard.Modifiers == modifier);
        }

        public static bool RightButtonBecomePressed(this MouseButtonEventArgs me, ModifierKeys? modifier = null)
        {
            return me.ChangedButton == MouseButton.Right &&
                   me.ButtonState == MouseButtonState.Pressed &&
                   (modifier == null || Keyboard.Modifiers == modifier);
        }

        public static bool RightButtonBecomeReleased(this MouseButtonEventArgs me, ModifierKeys? modifier = null)
        {
            return me.ChangedButton == MouseButton.Right &&
                   me.ButtonState == MouseButtonState.Released &&
                   (modifier == null || Keyboard.Modifiers == modifier);
        }

        public static bool MiddleButtonBecomePressed(this MouseButtonEventArgs me, ModifierKeys? modifier = null)
        {
            return me.ChangedButton == MouseButton.Middle &&
                   me.ButtonState == MouseButtonState.Pressed &&
                   (modifier == null || Keyboard.Modifiers == modifier);
        }

        public static bool MiddleButtonBecomeReleased(this MouseButtonEventArgs me, ModifierKeys? modifier = null)
        {
            return me.ChangedButton == MouseButton.Middle &&
                   me.ButtonState == MouseButtonState.Released &&
                   (modifier == null || Keyboard.Modifiers == modifier);
        }
    }
}
