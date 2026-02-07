using Microsoft.AspNetCore.Components;
using MudBlazor;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Common.NewRelic.Model;

namespace OutWit.Common.Blazor.Logging.ViewModels.Dialogs
{
    /// <summary>
    /// ViewModel for the log statistics dialog.
    /// </summary>
    public class DialogLogsStatisticsViewModel : ViewModelBase
    {
        #region Functions

        public void OnClose()
        {
            MudDialog?.Close(DialogResult.Ok(true));
        }

        public Color GetFreeTierColor()
        {
            if (Consumption == null) return Color.Default;

            return Consumption.FreeTierUsagePercent switch
            {
                >= 90 => Color.Error,
                >= 75 => Color.Warning,
                _ => Color.Success
            };
        }

        public string GetFreeTierBackgroundColor()
        {
            if (Consumption == null) return string.Empty;

            return Consumption.FreeTierUsagePercent switch
            {
                >= 90 => "background: rgba(var(--mud-palette-error-rgb), 0.1);",
                >= 75 => "background: rgba(var(--mud-palette-warning-rgb), 0.1);",
                _ => "background: rgba(var(--mud-palette-success-rgb), 0.1);"
            };
        }

        #endregion

        #region Properties

        [CascadingParameter]
        public IMudDialogInstance? MudDialog { get; set; }

        [Parameter]
        public DateTime From { get; set; }

        [Parameter]
        public DateTime To { get; set; }

        [Parameter]
        public NewRelicLogStatistics? Statistics { get; set; }

        [Parameter]
        public NewRelicDataConsumption? Consumption { get; set; }

        #endregion
    }
}
