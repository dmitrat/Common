using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OutWit.Common.Blazor.Logging.Model;
using OutWit.Common.MVVM.Blazor.ViewModels;

namespace OutWit.Common.Blazor.Logging.ViewModels.Dialogs
{
    /// <summary>
    /// ViewModel for the edit log filter dialog.
    /// </summary>
    public class DialogEditLogFilterViewModel : ViewModelBase
    {
        #region Event Handlers

        protected override void OnInitialized()
        {
            if (ExistingFilter != null)
            {
                Title = ExistingFilter.Title;
                SearchText = ExistingFilter.FullTextSearch ?? string.Empty;
                IsExclusion = ExistingFilter.IsExclusion;
            }
        }

        #endregion

        #region Functions

        protected void Submit()
        {
            var effectiveTitle = !string.IsNullOrWhiteSpace(Title)
                ? Title
                : (SearchText ?? "Unnamed Filter");

            var result = new LogFilterEditResult(effectiveTitle, SearchText)
            {
                IsExclusion = ForceExclusion || IsExclusion
            };

            MudDialog.Close(DialogResult.Ok(result));
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Shift")
                ForceExclusion = true;
        }

        protected void OnKeyUp(KeyboardEventArgs e)
        {
            if (e.Key == "Shift")
                ForceExclusion = false;
        }

        #endregion

        #region Properties

        [SupplyParameterFromForm]
        public string? Title { get; set; }

        [SupplyParameterFromForm]
        public string? SearchText { get; set; }

        [SupplyParameterFromForm]
        public bool IsExclusion { get; set; }

        public bool IsNew => ExistingFilter == null;

        private bool ForceExclusion { get; set; }

        #endregion

        #region Parameters

        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; } = null!;

        [Parameter]
        public LogFilterNode? ExistingFilter { get; set; }

        #endregion
    }
}
