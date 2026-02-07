using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Common.NewRelic.Model;

namespace OutWit.Common.Blazor.Logging.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the log details panel.
    /// Displays detailed information about a selected log entry.
    /// </summary>
    public class LogsDetailsViewModel : ViewModelBase
    {
        #region Functions

        public async Task CopyMessageAsync()
        {
            if (Entry == null || string.IsNullOrWhiteSpace(Entry.Message))
                return;

            try
            {
                await Js.InvokeVoidAsync("navigator.clipboard.writeText", Entry.Message);
                Snackbar.Add("Log message copied.", Severity.Success);
            }
            catch
            {
                Snackbar.Add("Copy failed.", Severity.Error);
            }
        }

        #endregion

        #region Injected

        [Inject]
        public IJSRuntime Js { get; set; } = null!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = null!;

        #endregion

        #region Properties

        [Parameter]
        public NewRelicLogEntry? Entry { get; set; }

        public Color SeverityColor
        {
            get
            {
                return Entry?.Level switch
                {
                    var _ when Entry?.Level == NewRelicLogSeverity.Critical => Color.Error,
                    var _ when Entry?.Level == NewRelicLogSeverity.Fatal => Color.Error,
                    var _ when Entry?.Level == NewRelicLogSeverity.Error => Color.Error,
                    var _ when Entry?.Level == NewRelicLogSeverity.Warning => Color.Warning,
                    var _ when Entry?.Level == NewRelicLogSeverity.Information => Color.Info,
                    _ => Color.Default
                };
            }
        }

        public IEnumerable<KeyValuePair<string, string>> ContextProperties
        {
            get
            {
                if (Entry == null) yield break;

                if (!string.IsNullOrWhiteSpace(Entry.SourceContext))
                    yield return new KeyValuePair<string, string>("Source", Entry.SourceContext);

                if (!string.IsNullOrWhiteSpace(Entry.Host))
                    yield return new KeyValuePair<string, string>("Host", Entry.Host);

                if (!string.IsNullOrWhiteSpace(Entry.Environment))
                    yield return new KeyValuePair<string, string>("Env", Entry.Environment);

                if (!string.IsNullOrWhiteSpace(Entry.TraceId))
                    yield return new KeyValuePair<string, string>("TraceId", Entry.TraceId);

                if (!string.IsNullOrWhiteSpace(Entry.SpanId))
                    yield return new KeyValuePair<string, string>("SpanId", Entry.SpanId);
            }
        }

        [Parameter]
        public Func<string?, string>? HighlightFunc { get; set; }

        #endregion
    }
}
