using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Logging.Query.Model
{
    [MemoryPackable]
    public partial class LogPage : ModelBase
    {
        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not LogPage page)
                return false;

            return Offset.Is(page.Offset)
                   && PageSize.Is(page.PageSize)
                   && HasMore == page.HasMore
                   && Items.Is(page.Items);
        }

        public override LogPage Clone()
        {
            return new LogPage
            {
                Offset = Offset,
                PageSize = PageSize,
                HasMore = HasMore,
                Items = Items.Select(entry => entry.Clone()).ToArray()
            };
        }

        #endregion

        #region Properties

        /// <summary>The offset from which the page was retrieved.</summary>
        [MemoryPackOrder(0)]
        public int Offset { get; init; }

        /// <summary>Page size (LIMIT).</summary>
        [MemoryPackOrder(1)]
        public int PageSize { get; init; }

        /// <summary>Indicates whether a next page is likely to exist.</summary>
        [MemoryPackOrder(2)]
        public bool HasMore { get; init; }

        /// <summary>The collection of log entries in this page.</summary>
        [MemoryPackOrder(3)]
        public LogEntry[] Items { get; init; } = [];

        #endregion
    }
}
