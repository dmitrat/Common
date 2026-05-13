using Microsoft.AspNetCore.Components;
using MudBlazor;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Blazor.Logging.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the log table component.
    /// Handles row selection and display of log entries.
    /// </summary>
    public class LogsTableViewModel : ViewModelBase
    {
        #region Event Handlers

        protected Task OnRowClickedAsync(TableRowClickEventArgs<LogEntry> args)
        {
            if (Busy || args.Item == null)
                return Task.CompletedTask;

            SelectedEntry = args.Item;
            return SelectedEntryChanged.InvokeAsync(args.Item);
        }

        protected async Task OnRowDoubleClickedAsync(TableRowClickEventArgs<LogEntry> args)
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

        public string GetRowCss(LogEntry entry, int index)
        {
            var cssClass = entry.Level switch
            {
                var _ when entry.Level == LogSeverity.Critical => "log-row-fatal",
                var _ when entry.Level == LogSeverity.Fatal => "log-row-fatal",
                var _ when entry.Level == LogSeverity.Error => "log-row-error",
                var _ when entry.Level == LogSeverity.Warning => "log-row-warn",
                var _ when entry.Level == LogSeverity.Information => "log-row-info",
                var _ when entry.Level == LogSeverity.Debug => "log-row-debug",
                var _ when entry.Level == LogSeverity.Trace => "log-row-trace",
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
        public IReadOnlyList<LogEntry> Entries { get; set; } = Array.Empty<LogEntry>();

        [Parameter]
        public LogEntry? SelectedEntry { get; set; }

        [Parameter]
        public EventCallback<LogEntry?> SelectedEntryChanged { get; set; }

        [Parameter]
        public Func<string?, string>? HighlightFunc { get; set; }

        [Parameter]
        public EventCallback<LogEntry> RowDoubleClick { get; set; }

        [Parameter]
        public new bool Busy { get; set; }

        [CascadingParameter]
        protected MudTable<LogEntry> Table { get; set; } = null!;

        #endregion
    }
}
