using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace OutWit.Common.Controls.Utils
{
    public static class ScreenUtils
    {
        public static double DeviceWidth(this Window me)
        {
            try
            {
                return GetScreen(me).Bounds.Width;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public static double DeviceHeight(this Window me)
        {
            try
            {
                return GetScreen(me).Bounds.Height;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public static double AreaWidth(this Window me)
        {
            try
            {
                return GetScreen(me).WorkingArea.Width;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public static double AreaHeight(this Window me)
        {
            try
            {
                return GetScreen(me).WorkingArea.Height;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public static bool IsPrimary(this Window me)
        {
            try
            {
                return GetScreen(me).Primary;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static bool HasSecondaryScreen(this Window me)
        {
            try
            {
                return Screen.AllScreens.Length >= 2;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static Rectangle? SecondaryScreenBounds(this Window me)
        {
            try
            {
                var currentScreen = GetScreen(me);
                var secondaryScreen = Screen.AllScreens.FirstOrDefault(x => !x.Bounds.Equals(currentScreen.Bounds));

                return secondaryScreen?.WorkingArea;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static Screen GetScreen(Window window)
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(window);
            return Screen.FromHandle(windowInteropHelper.Handle);
        }
    }
}
