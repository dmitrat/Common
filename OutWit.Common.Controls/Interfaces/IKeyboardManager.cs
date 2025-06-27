using System;
using System.Windows.Input;

namespace OutWit.Common.Controls.Interfaces
{
    public interface IKeyboardManager
    {
        public event KeyboardEventHandler KeyDown;
        public event KeyboardEventHandler KeyUp;

        public event KeyPressedEventHandler EnterKeyPressed;
        public event KeyPressedEventHandler EscapeKeyPressed;
        public event KeyPressedEventHandler F5KeyPressed;
        public event KeyPressedEventHandler PageUpKeyPressed;
        public event KeyPressedEventHandler PageDownKeyPressed;
        public event KeyPressedEventHandler LeftArrowKeyPressed;
        public event KeyPressedEventHandler RightArrowKeyPressed;
        public event KeyPressedEventHandler UpArrowKeyPressed;
        public event KeyPressedEventHandler DownArrowKeyPressed;
        public event KeyPressedEventHandler SaveCombinationPressed;
        public event KeyPressedEventHandler UndoCombinationPressed;

    }

    public delegate void KeyPressedEventHandler();
    public delegate void KeyboardEventHandler(KeyEventArgs args);
}
