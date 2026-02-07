using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Blazor.Logging.Model
{
    /// <summary>
    /// Result of editing a log filter in a dialog.
    /// </summary>
    public sealed class LogFilterEditResult : ModelBase
    {
        /// <summary>
        /// Initializes a new empty instance.
        /// </summary>
        public LogFilterEditResult()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified title and search text.
        /// </summary>
        public LogFilterEditResult(string title, string? searchText)
        {
            Title = title;
            SearchText = searchText;
        }

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not LogFilterEditResult result)
                return false;

            return Title.Is(result.Title)
                && SearchText.Is(result.SearchText)
                && IsExclusion.Is(result.IsExclusion);
        }

        public override LogFilterEditResult Clone()
        {
            return new LogFilterEditResult
            {
                SearchText = SearchText,
                Title = Title,
                IsExclusion = IsExclusion
            };
        }

        #region Properties

        public string Title { get; set; } = "";

        public string? SearchText { get; set; }

        public bool IsExclusion { get; set; }

        #endregion
    }
}
