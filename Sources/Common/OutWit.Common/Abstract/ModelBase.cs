using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace OutWit.Common.Abstract
{
    [DataContract]
    public abstract class ModelBase : ICloneable
    {
        #region Constants

        public const double DEFAULT_TOLERANCE = 0.0000001;

        #endregion

        #region Functions
        
        public abstract bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE);
        public abstract ModelBase Clone();

        #endregion

        #region ICloneable

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion
    }
}

