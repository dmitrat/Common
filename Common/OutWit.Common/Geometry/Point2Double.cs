using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Geometry
{
    public class Point2Double : Point2<double, double>
    {
        #region Constructors

        public Point2Double() : 
            base(0d, 0d)
        {
        }

        public Point2Double(double x, double y) :
            base(x, y)
        {
        }

        #endregion

        #region Functions

        public override bool IsEmpty()
        {
            return X.Is(0d) && Y.Is(0d);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Point2Double point))
                return false;

            return X.Is(point.X, tolerance) && 
                   Y.Is(point.Y, tolerance);
        }

        public override ModelBase Clone()
        {
            return new Point2Double(X, Y);
        } 

        #endregion
    }
}
