using System;
using OutWit.Common.Abstract;

namespace OutWit.Common.Geometry
{
    public class Point3<TX, TY, TZ> : ModelBase
    {
        #region Constructors

        public Point3()
        {
            X = default;
            Y = default;
            Z = default;
        }

        public Point3(TX x, TY y, TZ z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"[{X}, {Y}, {Z}]";
        }

        public virtual bool IsEmpty()
        {
            return X.Equals(default) && Y.Equals(default) && Z.Equals(default);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Point3<TX, TY, TZ> point))
                return false;

            return X.Equals(point.X) &&
                   Y.Equals(point.Y) &&
                   Z.Equals(point.Z);
        }

        public override ModelBase Clone()
        {
            return new Point3<TX, TY, TZ>(X, Y, Z);
        } 

        #endregion

        #region Properties

        public TX X { get; internal set; }

        public TY Y { get; internal set; }

        public TZ Z { get; internal set; }

        #endregion
    }
}
