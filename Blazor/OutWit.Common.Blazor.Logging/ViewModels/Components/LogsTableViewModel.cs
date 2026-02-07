using Microsoft.AspNetCore.Components;
using MudBlazor;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Common.NewRelic.Model;

namespace OutWit.Common.Blazor.Logging.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the log table component.
    /// Handles row selection and display of log entries.
    /// </summary>
    public class LogsTableViewModel : ViewModelBase
    {
        #region Event Handlers

        protected Task OnRowClickedAsync(TableRowClickEventArgs<NewRelicLogEntry> args)
        {
            if (Busy || args.Item == null)
                return Task.CompletedTask;

            SelectedEntry = args.Item;
            return SelectedEntryChanged.InvokeAsync(args.Item);
        }

        protected async Task OnRowDoubleClickedAsync(TableRowClickEventArgs<NewRelicLogEntry> args)
        {
            if (Busy || args.Item == null)
                return;

            SelectedEntry = args.Item;
            await SelectedEntryChanged.InvokeAsync(args.Item);
            await RowDoubleClick.InvokeAsync(args.Item);
        }

        #endregion

        #region Public Methods

        public async Task ScrollToTopAsync()
        {
            if (Entries.Count > 0)
                await Table.ScrollToItemAsync(Entries[0]);
        }

        public async Task ScrollToSelectionAsync()
        {
            if (SelectedEntry != null && Entries.Contains(SelectedEntry))
                await Table.ScrollToItemAsync(SelectedEntry);
        }

        #endregion

        #region Helper Methods

        public string GetRowCss(NewRelicLogEntry entry, int index)
        {
            var cssClass = entry.Level switch
            {
                var _ when entry.Level == NewRelicLogSeverity.Critical => "log-row-fatal",
                var _ when entry.Level == NewRelicLogSeverity.Fatal => "log-row-fatal",
                var _ when entry.Level == NewRelicLogSeverity.Error => "log-row-error",
                var _ when entry.Level == NewRelicLogSeverity.Warning => "log-row-warn",
                var _ when entry.Level == NewRelicLogSeverity.Information => "log-row-info",
                var _ when entry.Level == NewRelicLogSeverity.Debug => "log-row-debug",
                var _ when entry.Level == NewRelicLogSeverity.Trace => "log-row-trace",
                _ => "log-row-default"
            };

            if (ReferenceEquals(entry, SelectedEntry))
            {
                cssClass = string.IsNullOrEmpty(cssClass)
                    ? "mud-table-row-selected"
                    : $"{cssClass} mud-table-row-selected";
            }

            return cssClass;
        }

        protected MarkupString Highlight(string? text)
        {
            var html = HighlightFunc?.Invoke(text) ?? (text ?? string.Empty);
            return new MarkupString(html);
        }

        #endregion

        #region Parameters

        [Parameter]
        public IReadOnlyList<NewRelicLogEntry> Entries { get; set; } = Array.Empty<NewRelicLogEntry>();

        [Parameter]
        public NewRelicLogEntry? SelectedEntry { get; set; }

        [Parameter]
        public EventCallback<NewRelicLogEntry?> SelectedEntryChanged { get; set; }

        [Parameter]
        public Func<string?, string>? HighlightFunc { get; set; }

        [Parameter]
        public EventCallback<NewRelicLogEntry> RowDoubleClick { get; set; }

        [Parameter]
        public new bool Busy { get; set; }

        [CascadingParameter]
        protected MudTable<NewRelicLogEntry> Table { get; set; } = null!;

        #endregion
    }
}
