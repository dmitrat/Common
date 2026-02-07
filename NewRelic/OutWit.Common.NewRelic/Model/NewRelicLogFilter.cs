using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Common.Collections;

namespace OutWit.Common.NewRelic.Model
{
    [MemoryPackable]
    public partial class NewRelicLogFilter : ModelBase
    {
        #region Factory helpers

        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.Equals"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter Eq(string attribute, string value)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.Equals,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.NotEquals"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter NotEq(string attribute, string value)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.NotEquals,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.Contains"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="value">The substring to search for.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter Contains(string attribute, string value)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.Contains,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.NotContains"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="value">The substring that must not be present.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter NotContains(string attribute, string value)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.NotContains,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.In"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="values">The set of values to match against.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter In(string attribute, params string[] values)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.In,
                Values = values
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.GreaterOrEqual"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter GreaterOrEqual(string attribute, string value)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.GreaterOrEqual,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.GreaterThan"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter GreaterThan(string attribute, string value)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.GreaterThan,
                Values = [value]
            };
        }


        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.LessOrEqual"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter LessOrEqual(string attribute, string value)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.LessOrEqual,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="NewRelicLogFilterOperator.LessThan"/> operator.
        /// </summary>
        /// <param name="attribute">The log attribute name.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter LessThan(string attribute, string value)
        {
            return new NewRelicLogFilter
            {
                Attribute = attribute,
                Operator = NewRelicLogFilterOperator.LessThan,
                Values = [value]
            };
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not NewRelicLogFilter filter)
                return false;

            return Attribute.Is(filter.Attribute)
                   && Operator.Is(filter.Operator)
                   && Values.Is(filter.Values);
        }

        public override NewRelicLogFilter Clone()
        {
            return new NewRelicLogFilter
            {
                Attribute = Attribute,
                Operator = Operator,
                Values = Values.ToArray()
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Log attribute name, e.g. "level", "service.name".
        /// </summary>
        [MemoryPackOrder(0)]
        public string Attribute { get; set; } = string.Empty;

        /// <summary>
        /// Comparison operator.
        /// </summary>
        [MemoryPackOrder(1)]
        public NewRelicLogFilterOperator Operator { get; set; }

        /// <summary>
        /// Filter values.
        /// For non-IN operators the first value is used.
        /// For IN operator all values are used.
        /// </summary>
        [MemoryPackOrder(2)]
        public string[] Values { get; set; } = [];

        #endregion
    }
}
