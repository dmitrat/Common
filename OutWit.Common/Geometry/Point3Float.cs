using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Geometry
{
    public class Point3Float : Point3<float, float, float>
    {
        #region Constructors

        public Point3Float() : 
            base(0f, 0f, 0f)
        {
        }

        public Point3Float(float x, float y, float z) :
            base(x, y, z)
        {
        }

        #endregion

        #region Functions

        public override bool IsEmpty()
        {
            return X.Is(0f) && Y.Is(0f) && Y.Is(0f);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Point3Float point))
                return false;

            return X.Is(point.X, tolerance) &&
                   Y.Is(point.Y, tolerance) &&
                   Z.Is(point.Z, tolerance);
        }

        public override ModelBase Clone()
        {
            return new Point3Float(X, Y, Z);
        } 

        #endregion
    }
}
