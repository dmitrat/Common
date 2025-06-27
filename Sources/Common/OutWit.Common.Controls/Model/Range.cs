using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Controls.Model
{
    public class Range<TValue> : ModelBase, IRange<TValue>
        where TValue : struct, IComparable<TValue>
    {
        #region Constructors

        private Range()
        {
        }

        public Range(TValue from, TValue to)
        {
            From = from;
            To = to;
        }

        #endregion

        #region Functions

        public void Reset(TValue from, TValue to)
        {
            From = from;
            To = to;
        }

        public override string ToString()
        {
            return $"[{From}, {To}]";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is Range<TValue> range))
                return false;

            return range.From.CompareTo(From) == 0 &&
                   range.To.CompareTo(To) == 0;
        }

        public override ModelBase Clone()
        {
            return new Range<TValue>(From, To);
        }

        #endregion

        #region Properties

        [Notify]
        public TValue From { get; private set; }

        [Notify]
        public TValue To { get; private set; }

        #endregion

    }
}
