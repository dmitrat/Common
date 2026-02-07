using Microsoft.AspNetCore.Components;
using MudBlazor;
using OutWit.Common.Blazor.Logging.Model;
using OutWit.Common.Blazor.Logging.Utils;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Common.NewRelic.Model;

namespace OutWit.Common.Blazor.Logging.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the log toolbar component.
    /// Handles date/time selection, severity toggles, source filtering, and pagination.
    /// </summary>
    public class LogsToolbarViewModel : ViewModelBase
    {
        #region Functions

        protected string GetMultiSelectionText(List<string>? selectedValues)
        {
            if (selectedValues == null || selectedValues.Count == 0)
                return "None selected";

            if (selectedValues.Count == AvailableSources.Count)
                return "All sources selected";

            return $"{selectedValues.Count} selected";
        }

        #endregion

        #region Event Handlers

        public Task OnToggleLevelAsync(NewRelicLogSeverity mainLevel)
        {
            if (IsLoading || Conditions == null || !Conditions.Levels.ContainsKey(mainLevel))
                return Task.CompletedTask;

            Conditions.Offset = 0;
            Conditions.ToggleActiveLevel(mainLevel);

            return ConditionsChanged.InvokeAsync();
        }

        protected async Task OnDateChangedAsync(DateTime? value)
        {
            if (!value.HasValue || IsLoading || Conditions == null)
                return;

            Conditions.Offset = 0;
            Conditions.SelectedDate = DateOnly.FromDateTime(value.Value);

            await ConditionsChanged.InvokeAsync(true);
        }

        protected async Task OnMinTimeChangedAsync(TimeSpan? value)
        {
            if (IsLoading || Conditions == null)
                return;

            Conditions.Offset = 0;
            Conditions.MinTime = value.AsTimeOnly();

            await ConditionsChanged.InvokeAsync();
        }

        protected async Task OnMaxTimeChangedAsync(TimeSpan? value)
        {
            if (IsLoading || Conditions == null)
                return;

            Conditions.Offset = 0;
            Conditions.MaxTime = value.AsTimeOnly();

            await ConditionsChanged.InvokeAsync();
        }

        protected async Task OnSelectedSourcesChangedAsync(IEnumerable<string?>? values)
        {
            if (IsLoading || Conditions == null || values == null)
                return;

            Conditions.SelectSources(values);

            await ConditionsChanged.InvokeAsync();
        }

        protected async Task OnSelectedPageSizeChangedAsync(IEnumerable<int>? values)
        {
            if (IsLoading || values == null || Conditions == null)
                return;

            IReadOnlyList<int> selection = values.ToList();
            if (selection.Count != 1)
                return;

            Conditions.Offset = 0;
            Conditions.PageSize = selection.Single();

            await ConditionsChanged.InvokeAsync(true);
        }

        protected Task OnReloadClickedAsync()
        {
            if (Busy)
                return Task.CompletedTask;

            return ConditionsChanged.InvokeAsync();
        }

        protected Task OnPrevClickedAsync()
        {
            if (IsLoading || !CanGoPrev || Conditions == null)
                return Task.CompletedTask;

            Conditions?.SetPreviousPage();

            return ConditionsChanged.InvokeAsync(true);
        }

        protected Task OnNextClickedAsync()
        {
            if (IsLoading || !CanGoNext || Conditions == null)
                return Task.CompletedTask;

            Conditions?.SetNextPage();

            return ConditionsChanged.InvokeAsync(true);
        }

        protected Task OnShowStatisticsClickedAsync()
        {
            if (IsLoading || Conditions == null)
                return Task.CompletedTask;

            return StatisticsRequested.InvokeAsync();
        }

        #endregion

        #region Parameters

        [Parameter]
        public LogConditions? Conditions { get; set; }

        [Parameter]
        public bool CurrentPageHasMore { get; set; }

        [Parameter]
        public bool IsLoading { get; set; }

        [Parameter]
        public EventCallback<bool> ConditionsChanged { get; set; }

        [Parameter]
        public EventCallback StatisticsRequested { get; set; }

        #endregion

        #region Properties

        protected DateTime? SelectedDateTime => Conditions?.SelectedDate.ToDateTime(TimeOnly.MinValue);
        protected TimeSpan? SelectedMinTime => Conditions?.MinTime.AsTimeSpan();
        protected TimeSpan? SelectedMaxTime => Conditions?.MaxTime.AsTimeSpan();

        protected bool ShowError => Conditions?.IsActiveLevel(NewRelicLogSeverity.Error) ?? false;
        protected bool ShowWarning => Conditions?.IsActiveLevel(NewRelicLogSeverity.Warning) ?? false;
        protected bool ShowInfo => Conditions?.IsActiveLevel(NewRelicLogSeverity.Information) ?? false;
        protected bool ShowDebug => Conditions?.IsActiveLevel(NewRelicLogSeverity.Debug) ?? false;

        protected IReadOnlyCollection<string> AvailableSources => Conditions?.GetAvailableSources() ?? [];
        protected IReadOnlyCollection<string> SelectedSources => Conditions?.GetSelectedSources() ?? [];

        protected IReadOnlyCollection<int> AvailablePageSizes => Conditions?.GetAvailablePageSizes() ?? [];
        protected IReadOnlyCollection<int> SelectedPageSize => Conditions?.GetSelectedPageSizes() ?? [];

        protected int CurrentPageNumber => Conditions?.GetCurrentPageNumber() ?? 1;

        protected bool CanGoNext => CurrentPageHasMore;
        protected bool CanGoPrev => Conditions?.Offset > 0;

        #endregion
    }
}
