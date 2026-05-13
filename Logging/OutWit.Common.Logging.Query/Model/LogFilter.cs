using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Logging.Query.Model
{
    [MemoryPackable]
    public partial class LogFilter : ModelBase
    {
        #region Factory helpers

        /// <summary>
        /// Creates a filter with the <see cref="LogFilterOperator.Equals"/> operator.
        /// </summary>
        public static LogFilter Eq(string attribute, string value)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.Equals,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="LogFilterOperator.NotEquals"/> operator.
        /// </summary>
        public static LogFilter NotEq(string attribute, string value)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.NotEquals,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="LogFilterOperator.Contains"/> operator.
        /// </summary>
        public static LogFilter Contains(string attribute, string value)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.Contains,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="LogFilterOperator.NotContains"/> operator.
        /// </summary>
        public static LogFilter NotContains(string attribute, string value)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.NotContains,
                Values = [value]
            };
        }

        /// <summary>
        /// Creates a filter with the <see cref="LogFilterOperator.In"/> operator.
        /// </summary>
        public static LogFilter In(string attribute, params string[] values)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.In,
                Values = values
            };
        }

        public static LogFilter GreaterOrEqual(string attribute, string value)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.GreaterOrEqual,
                Values = [value]
            };
        }

        public static LogFilter GreaterThan(string attribute, string value)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.GreaterThan,
                Values = [value]
            };
        }

        public static LogFilter LessOrEqual(string attribute, string value)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.LessOrEqual,
                Values = [value]
            };
        }

        public static LogFilter LessThan(string attribute, string value)
        {
            return new LogFilter
            {
                Attribute = attribute,
                Operator = LogFilterOperator.LessThan,
                Values = [value]
            };
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not LogFilter filter)
                return false;

            return Attribute.Is(filter.Attribute)
                   && Operator.Is(filter.Operator)
                   && Values.Is(filter.Values);
        }

        public override LogFilter Clone()
        {
            return new LogFilter
            {
                Attribute = Attribute,
                Operator = Operator,
                Values = Values.ToArray()
            };
        }

        #endregion

        #region Properties

        /// <summary>Log attribute name, e.g. "level", "service.name".</summary>
        [MemoryPackOrder(0)]
        public string Attribute { get; set; } = string.Empty;

        /// <summary>Comparison operator.</summary>
        [MemoryPackOrder(1)]
        public LogFilterOperator Operator { get; set; }

        /// <summary>
        /// Filter values. For non-IN operators the first value is used.
        /// For the IN operator all values are used.
        /// </summary>
        [MemoryPackOrder(2)]
        public string[] Values { get; set; } = [];

        #endregion
    }
}
