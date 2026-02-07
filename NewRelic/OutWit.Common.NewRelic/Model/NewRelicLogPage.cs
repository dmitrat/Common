using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Common.Collections;

namespace OutWit.Common.NewRelic.Model
{
    [MemoryPackable]
    public partial class NewRelicLogPage : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if(modelBase is not NewRelicLogPage page)
                return false;

            return Offset.Is(page.Offset)
                   && PageSize.Is(page.PageSize)
                   && HasMore == page.HasMore
                   && Items.Is(page.Items);
        }

        public override NewRelicLogPage Clone()
        {
            return new NewRelicLogPage
            {
                Offset = Offset,
                PageSize = PageSize,
                HasMore = HasMore,
                Items = Items.Select(entry => entry.Clone()).ToArray()
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// The offset from which the page was retrieved.
        /// </summary>
        [MemoryPackOrder(0)]
        public int Offset { get; init; }

        /// <summary>
        /// Page size (LIMIT).
        /// </summary>
        [MemoryPackOrder(1)]
        public int PageSize { get; init; }

        /// <summary>
        /// Indicates whether a next page is likely to exist.
        /// </summary>
        [MemoryPackOrder(2)]
        public bool HasMore { get; init; }

        /// <summary>
        /// Gets the collection of log entries contained in this object.
        /// </summary>
        [MemoryPackOrder(3)]
        public NewRelicLogEntry[] Items { get; init; } = [];

        #endregion

    }
}
