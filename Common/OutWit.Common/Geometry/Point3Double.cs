using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Geometry
{
    public class Point3Double : Point3<double, double, double>
    {
        #region Constructors

        public Point3Double() : 
            base(0d, 0d, 0d)
        {
        }

        public Point3Double(double x, double y, double z) :
            base(x, y, z)
        {
        }

        #endregion

        #region Functions

        public override bool IsEmpty()
        {
            return X.Is(0d) && Y.Is(0d) && Y.Is(0d);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Point3Double point))
                return false;

            return X.Is(point.X, tolerance) &&
                   Y.Is(point.Y, tolerance) &&
                   Z.Is(point.Z, tolerance);
        }

        public override ModelBase Clone()
        {
            return new Point3Double(X, Y, Z);
        } 

        #endregion
    }
}
