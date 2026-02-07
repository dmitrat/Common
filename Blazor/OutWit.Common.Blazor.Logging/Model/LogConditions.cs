using OutWit.Common.MVVM.Model;
using OutWit.Common.NewRelic.Model;
using OutWit.Common.Values;

namespace OutWit.Common.Blazor.Logging.Model
{
    /// <summary>
    /// Encapsulates all query conditions for log search including date range,
    /// time filters, severity levels, sources, and pagination.
    /// </summary>
    public sealed class LogConditions
    {
        #region Constants

        private const int DEFAULT_PAGE_SIZE = 100;

        private static readonly int[] VALID_PAGE_SIZES = { 100, 200, 300, 400, 500 };

        #endregion

        #region Constructors

        public LogConditions()
        {
            SelectedDate = DateOnly.FromDateTime(DateTime.Now);
            PageSize = DEFAULT_PAGE_SIZE;
            ResetLevels();
        }

        #endregion

        #region Levels

        /// <summary>
        /// Resets severity levels to the default set (Critical, Error, Warning, Information).
        /// </summary>
        public void ResetLevels()
        {
            Levels = new Dictionary<NewRelicLogSeverity, SelectableValue<NewRelicLogSeverity>>
            {
                { NewRelicLogSeverity.Critical, NewRelicLogSeverity.Critical },
                { NewRelicLogSeverity.Error, NewRelicLogSeverity.Error },
                { NewRelicLogSeverity.Warning, NewRelicLogSeverity.Warning },
                { NewRelicLogSeverity.Information, NewRelicLogSeverity.Information }
            };
        }

        /// <summary>
        /// Checks whether the specified severity level is currently active.
        /// </summary>
        public bool IsActiveLevel(NewRelicLogSeverity level)
        {
            return Levels.TryGetValue(level, out var selectable)
                   && selectable.IsSelected;
        }

        /// <summary>
        /// Toggles the active state of the specified severity level.
        /// </summary>
        public void ToggleActiveLevel(NewRelicLogSeverity level)
        {
            if (Levels.TryGetValue(level, out var selectable))
                selectable.ToggleSelection();
        }

        private IReadOnlyList<NewRelicLogFilter> BuildLevelsFilters()
        {
            NewRelicLogSeverity[] selectedLevels = Levels
                .Values
                .Where(value => value.IsSelected)
                .SelectMany(value => GetLevelsForGroup(value.Value))
                .ToArray();

            if (selectedLevels.Length > 0)
                return [NewRelicLogFilters.LevelIn(selectedLevels)];

            return [];
        }

        private static IEnumerable<NewRelicLogSeverity> GetLevelsForGroup(NewRelicLogSeverity group)
        {
            return group switch
            {
                _ when group.Is(NewRelicLogSeverity.Error) => new[] { NewRelicLogSeverity.Error, NewRelicLogSeverity.Critical, NewRelicLogSeverity.Fatal },
                _ when group.Is(NewRelicLogSeverity.Warning) => new[] { NewRelicLogSeverity.Warning },
                _ when group.Is(NewRelicLogSeverity.Information) => new[] { NewRelicLogSeverity.Information },
                _ when group.Is(NewRelicLogSeverity.Debug) => new[] { NewRelicLogSeverity.Debug, NewRelicLogSeverity.Trace },
                _ => new[] { group }
            };
        }

        #endregion

        #region Sources

        /// <summary>
        /// Replaces all sources with the specified collection.
        /// </summary>
        public void ResetSources(IReadOnlyCollection<string> sources)
        {
            Sources = new Dictionary<string, SelectableValue<string>>();
            foreach (string source in sources)
                Sources.Add(source, source);
        }

        /// <summary>
        /// Merges the specified sources into the existing set, adding new and removing stale entries.
        /// </summary>
        public void MergeSources(IReadOnlyCollection<string> sources)
        {
            HashSet<string> existingSources = Sources.Values.Select(value => value.Value).ToHashSet();

            HashSet<string> sourcesToRemove = existingSources
                .Where(s => !sources.Contains(s))
                .ToHashSet();

            IReadOnlyList<string> sourcesToAdd = sources
                .Where(s => !existingSources.Contains(s))
                .ToList();

            foreach (string source in sourcesToRemove)
                Sources.Remove(source);

            foreach (string source in sourcesToAdd)
                Sources.Add(source, source);
        }

        /// <summary>
        /// Selects the specified sources and deselects all others.
        /// </summary>
        public void SelectSources(IEnumerable<string?>? selectedSources)
        {
            if (selectedSources == null)
                return;

            DeselectAllSources();

            foreach (var source in selectedSources)
            {
                if (string.IsNullOrEmpty(source))
                    continue;

                if (Sources.TryGetValue(source, out var selectableSource))
                    selectableSource.IsSelected = true;
            }
        }

        /// <summary>
        /// Deselects all sources.
        /// </summary>
        public void DeselectAllSources()
        {
            foreach (var source in Sources.Values)
                source.IsSelected = false;
        }

        /// <summary>
        /// Returns all available source names.
        /// </summary>
        public IReadOnlyCollection<string> GetAvailableSources()
        {
            return Sources.Values.Select(value => value.Value).ToList();
        }

        /// <summary>
        /// Returns the names of currently selected sources.
        /// </summary>
        public IReadOnlyCollection<string> GetSelectedSources()
        {
            return Sources.Values
                .Where(value => value.IsSelected)
                .Select(value => value.Value)
                .ToList();
        }

        private IReadOnlyList<NewRelicLogFilter> BuildSourceFilters()
        {
            string[] selectedSources = Sources.Values
                .Where(value => value.IsSelected)
                .Select(value => value.Value)
                .ToArray();

            if (selectedSources.Length > 0)
                return [NewRelicLogFilters.SourceContextIn(selectedSources)];

            return [];
        }

        #endregion

        #region DateTime

        /// <summary>
        /// Gets the UTC start of the query range.
        /// </summary>
        public DateTime GetFrom()
        {
            return SelectedDate.ToDateTime(MinTime ?? TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        }

        /// <summary>
        /// Gets the UTC end of the query range.
        /// </summary>
        public DateTime GetTo()
        {
            return SelectedDate.ToDateTime(MaxTime ?? TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();
        }

        #endregion

        #region Pages

        /// <summary>
        /// Returns the 1-based page number.
        /// </summary>
        public int GetCurrentPageNumber()
        {
            return Offset / PageSize + 1;
        }

        /// <summary>
        /// Advances to the next page.
        /// </summary>
        public void SetNextPage()
        {
            Offset += PageSize;
        }

        /// <summary>
        /// Returns to the previous page (minimum offset is 0).
        /// </summary>
        public void SetPreviousPage()
        {
            Offset = Math.Max(0, Offset - PageSize);
        }

        /// <summary>
        /// Returns the valid page-size options.
        /// </summary>
        public IReadOnlyList<int> GetAvailablePageSizes()
        {
            return VALID_PAGE_SIZES;
        }

        /// <summary>
        /// Returns the currently selected page size as a single-element list.
        /// </summary>
        public IReadOnlyList<int> GetSelectedPageSizes()
        {
            return [PageSize];
        }

        #endregion

        #region Functions

        /// <summary>
        /// Builds a <see cref="NewRelicLogQuery"/> from the current conditions and the specified filter node.
        /// </summary>
        public NewRelicLogQuery Query(LogFilterNode? node)
        {
            List<NewRelicLogFilter> filters = Build(node);

            filters.AddRange(BuildLevelsFilters());
            filters.AddRange(BuildSourceFilters());

            return new NewRelicLogQuery
            {
                From = SelectedDate.ToDateTime(MinTime ?? TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime(),
                To = SelectedDate.ToDateTime(MaxTime ?? TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime(),
                PageSize = PageSize,
                Offset = Offset,
                Filters = filters.ToArray()
            };
        }

        private List<NewRelicLogFilter> Build(LogFilterNode? root)
        {
            List<NewRelicLogFilter> filters = new();

            if (root == null)
                return filters;

            var chain = root
                .AncestorsAndSelf()
                .Where(node => !node.IsDisabled)
                .Reverse()
                .ToList();

            filters.AddRange(chain.SelectMany(node => node.Filters));

            foreach (var node in chain)
            {
                if (string.IsNullOrWhiteSpace(node.FullTextSearch))
                    continue;

                filters.Add(node.IsExclusion
                    ? NewRelicLogFilters.MessageNotContains(node.FullTextSearch)
                    : NewRelicLogFilters.MessageContains(node.FullTextSearch));
            }

            return filters;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Currently selected date in the toolbar.
        /// </summary>
        public DateOnly SelectedDate { get; set; }

        /// <summary>
        /// Global start time filter (inclusive).
        /// </summary>
        public TimeOnly? MinTime { get; set; }

        /// <summary>
        /// Global end time filter (exclusive).
        /// </summary>
        public TimeOnly? MaxTime { get; set; }

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The set of currently active severity levels.
        /// </summary>
        public IReadOnlyDictionary<NewRelicLogSeverity, SelectableValue<NewRelicLogSeverity>> Levels { get; private set; } = null!;

        /// <summary>
        /// The set of log sources being filtered.
        /// </summary>
        public Dictionary<string, SelectableValue<string>> Sources { get; private set; } = new();

        /// <summary>
        /// Current page offset (0-based).
        /// </summary>
        public int Offset { get; set; }

        #endregion
    }
}
