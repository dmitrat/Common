using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OutWit.Common.Controls.Interfaces
{
    public interface IWindowManager
    {
        event WindowManagerEventHandler WindowStateChanged;
        event WindowManagerEventHandler WindowLoaded;
        event WindowManagerEventHandler WindowClosing;
        event WindowManagerEventHandler WindowTitleUpdated;

        void Maximize();
        void Minimize();
        void Restore();
        void FullScreen();
        void Close();

        void LockNavigation();
        void UnlockNavigation();

        void UpdateTitle(string title);
        void ResetTitle();

        bool IsMaximized { get; }
        bool IsFullScreen { get; }

        WindowState WindowState { get; }
        
        string WindowTitle { get; }
    }

    public delegate void WindowManagerEventHandler();
}
