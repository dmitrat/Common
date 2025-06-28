using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutWit.Common.Rest.Adapters
{
    public class QueryBuilderAdapterEnum: QueryBuilderAdapterBase<Enum>
    {
        #region Constructors

        public QueryBuilderAdapterEnum()
        {
            Format = "";
        }

        #endregion

        #region Functions

        public override string? Convert(Enum value)
        {
            return $"{value}";
        }

        public override string? Convert(Enum value, string format)
        {
            return Convert(value);
        }

        #endregion
    }
}
