using System;
using OutWit.Common.Abstract;

namespace OutWit.Common.Geometry
{
    public class Point2<TX, TY> : ModelBase
    {
        #region Constructors

        public Point2()
        {
            X = default;
            Y = default;
        }

        public Point2(TX x, TY y)
        {
            X = x;
            Y = y;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"[{X}, {Y}]";
        }

        public virtual bool IsEmpty()
        {
            return X.Equals(default) && Y.Equals(default);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Point2<TX, TY> point))
                return false;

            return X.Equals(point.X) && 
                   Y.Equals(point.Y);
        }

        public override ModelBase Clone()
        {
            return new Point2<TX, TY>(X, Y);
        } 

        #endregion

        #region Properties

        public TX X { get; internal set; }

        public TY Y { get; internal set; }

        #endregion
    }
}
