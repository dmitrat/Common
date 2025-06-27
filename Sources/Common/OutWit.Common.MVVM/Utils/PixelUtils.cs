using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OutWit.Common.MVVM.Utils
{
    public static class PixelUtils
    {
        #region Constants

        private const double PIXEL_FACTOR = 1.0;
        private const double INCH_FACTOR = 96.0;
        private const double CENTIMETER_FACTOR = 37.7952755905512;
        private const double MILLIMETER_FACTOR = 3.77952755905512;
        private const double POINT_FACTOR = 1.3333333333333333; 

        #endregion

        public static double DeviceUnitToInch(this double me)
        {
            return me / INCH_FACTOR;
        }
        public static double InchToDeviceUnit(this double me)
        {
            return me * INCH_FACTOR;
        }


        public static double DeviceUnitToCentimeter(this double me)
        {
            return me / CENTIMETER_FACTOR;
        }
        public static double CentimeterToDeviceUnit(this double me)
        {
            return me * CENTIMETER_FACTOR;
        }

        public static double DeviceUnitToMillimeter(this double me)
        {
            return me / MILLIMETER_FACTOR;
        }
        public static double MillimeterToDeviceUnit(this double me)
        {
            return me * MILLIMETER_FACTOR;
        }

        public static double DeviceUnitToPoint(this double me)
        {
            return me / POINT_FACTOR;
        }
        public static double PointToDeviceUnit(this double me)
        {
            return me * POINT_FACTOR;
        }

        public static ushort DeviceUnitToPixel(this double me)
        {
            return (ushort)Math.Round(me);
        }
        public static double PixelToDeviceUnit(this ushort me)
        {
            return me;
        }

        public static double Distance(this Point me, Point point)
        {
            var dx = me.X - point.X;
            var dy = me.Y - point.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Distance(this Point me, Point start, Point end)
        {
            return Math.Abs((end.X - start.X) * (start.Y - me.Y) - (start.X - me.X) * (end.Y - start.Y)) / start.Distance(end);
        }
    }
}
