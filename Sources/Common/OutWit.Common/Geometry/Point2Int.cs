using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Geometry
{
    public class Point2Int : Point2<int, int>
    {
        #region Constructors

        public Point2Int() : 
            base(0, 0)
        {
        }

        public Point2Int(int x, int y) :
            base(x, y)
        {
        }

        #endregion

        #region Functions

        public override bool IsEmpty()
        {
            return X.Is(0) && Y.Is(0);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Point2Int point))
                return false;

            return X.Is(point.X) && 
                   Y.Is(point.Y);
        }

        public override ModelBase Clone()
        {
            return new Point2Int(X, Y);
        } 

        #endregion
    }
}
