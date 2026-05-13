using System;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Logging.Query.Model
{
    [MemoryPackable]
    public partial class LogQuery : ModelBase
    {
        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not LogQuery other)
                return false;

            return From.Is(other.From)
                   && To.Is(other.To)
                   && Lookback.Is(other.Lookback)
                   && FullTextSearch.Is(other.FullTextSearch)
                   && Filters.Is(other.Filters)
                   && PageSize == other.PageSize
                   && Offset == other.Offset
                   && SortOrder == other.SortOrder;
        }

        public override LogQuery Clone()
        {
            return new LogQuery
            {
                From = From,
                To = To,
                Lookback = Lookback,
                FullTextSearch = FullTextSearch,
                Filters = Filters?.Select(filter => filter.Clone()).ToArray(),
                PageSize = PageSize,
                Offset = Offset,
                SortOrder = SortOrder
            };
        }

        #endregion

        #region Properties

        [MemoryPackOrder(0)]
        public DateTime? From { get; set; }

        [MemoryPackOrder(1)]
        public DateTime? To { get; set; }

        [MemoryPackOrder(2)]
        public TimeSpan? Lookback { get; set; }

        [MemoryPackOrder(3)]
        public string? FullTextSearch { get; set; }

        [MemoryPackOrder(4)]
        public LogFilter[]? Filters { get; set; }

        [MemoryPackOrder(5)]
        public int? PageSize { get; set; }

        [MemoryPackOrder(6)]
        public int Offset { get; set; }

        [MemoryPackOrder(7)]
        public LogSortOrder SortOrder { get; set; } = LogSortOrder.Descending;

        #endregion
    }
}
