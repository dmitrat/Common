using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Geometry
{
    public class Point2Float : Point2<float, float>
    {
        #region Constructors

        public Point2Float() : 
            base(0f, 0f)
        {
        }

        public Point2Float(float x, float y) :
            base(x, y)
        {
        }

        #endregion

        #region Functions

        public override bool IsEmpty()
        {
            return X.Is(0f) && Y.Is(0f);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Point2Float point))
                return false;

            return X.Is(point.X, tolerance) && 
                   Y.Is(point.Y, tolerance);
        }

        public override ModelBase Clone()
        {
            return new Point2Float(X, Y);
        } 

        #endregion
    }
}
