using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Geometry
{
    public class Point3Int : Point3<int, int, int>
    {
        #region Constructors

        public Point3Int() : 
            base(0, 0, 0)
        {
        }

        public Point3Int(int x, int y, int z) :
            base(x, y, z)
        {
        }

        #endregion

        #region Functions

        public override bool IsEmpty()
        {
            return X.Is(0) && Y.Is(0) && Y.Is(0);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Point3Int point))
                return false;

            return X.Is(point.X) &&
                   Y.Is(point.Y) &&
                   Z.Is(point.Z);
        }

        public override ModelBase Clone()
        {
            return new Point3Int(X, Y, Z);
        } 

        #endregion
    }
}
