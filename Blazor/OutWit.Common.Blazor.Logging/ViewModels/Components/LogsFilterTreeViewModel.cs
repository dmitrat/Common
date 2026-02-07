using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OutWit.Common.Aspects;
using OutWit.Common.Blazor.Logging.Model;
using OutWit.Common.Blazor.Logging.Views.Dialogs;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Common.Utils;
using OutWit.Common.Values;

namespace OutWit.Common.Blazor.Logging.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the log filter tree component.
    /// Manages filter hierarchy and selection.
    /// </summary>
    public class LogsFilterTreeViewModel : ViewModelBase
    {
        #region Initialization

        protected override Task OnInitializedAsync()
        {
            InitializeFilters();
            return base.OnInitializedAsync();
        }

        private void InitializeFilters()
        {
            var root = new LogFilterNode("All logs (day)");
            RootFilter = root;
            RootFilters = [root];
            SelectedFilter ??= root;
        }

        #endregion

        #region Event Handlers

        protected override void OnPropertyChanged(string? propertyName)
        {
            if (propertyName.Is(nameof(SelectedFilter)))
            {
                TreeViewKey = Guid.NewGuid();
                StateHasChanged();
            }
        }

        protected async Task OnFilterChangedAsync(LogFilterNode? node)
        {
            if (Busy || node == null || node == SelectedFilter)
                return;

            await UpdateSelectedFilterAsync(node);
        }

        protected async Task OnDuplicateClickedAsync(LogFilterNode node)
        {
            if (Busy) return;

            var copy = node.CloneShallow();
            copy.Title += " (copy)";

            if (node.Parent is null)
                RootFilters = [.. RootFilters, copy];
            else
                node.Parent.AddChild(copy);

            await UpdateSelectedFilterAsync(copy);
        }

        protected async Task OnDeleteClickedAsync(LogFilterNode node)
        {
            if (Busy) return;

            if (node.Parent is null && RootFilters.Count == 1)
            {
                ResetNode(node);
                await UpdateSelectedFilterAsync(node);
                return;
            }

            var newSelected = RemoveNode(node);
            await UpdateSelectedFilterAsync(newSelected);
        }

        protected async Task OnDisabledToggledAsync(LogFilterNode node, bool isDisabled)
        {
            if (Busy) return;

            node.IsDisabled = isDisabled;
            await FiltersChanged.InvokeAsync();
        }

        #endregion

        #region Dialog Actions

        public async Task EditFilterDialogAsync(LogFilterNode node)
        {
            if (Busy) return;

            var data = await ShowFilterDialogAsync("Edit Filter", node);
            if (data == null) return;

            ApplyFilterData(node, data);
            await UpdateSelectedFilterAsync(node);
        }

        public async Task AddChildFilterDialogAsync(LogFilterNode parent)
        {
            if (Busy) return;

            var data = await ShowFilterDialogAsync("Add Filter", null);
            if (data == null) return;

            var newFilter = CreateFilterNode(data);
            parent.AddChild(newFilter);

            await UpdateSelectedFilterAsync(newFilter);
        }

        #endregion

        #region Helper Methods

        private static TreeItemData<LogFilterNode> CreateTreeItem(LogFilterNode node)
        {
            return new TreeItemData<LogFilterNode>
            {
                Value = node,
                Expanded = true,
                Children = node.Children?.Count > 0
                    ? node.Children.Select(CreateTreeItem).ToList()
                    : []
            };
        }

        private async Task<LogFilterEditResult?> ShowFilterDialogAsync(string title, LogFilterNode? existingFilter)
        {
            var parameters = new DialogParameters
            {
                { nameof(DialogEditLogFilter.ExistingFilter), existingFilter }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            var dialog = await DialogService.ShowAsync<DialogEditLogFilter>(title, parameters, options);
            var result = await dialog.Result;

            return result is { Canceled: false, Data: LogFilterEditResult data } ? data : null;
        }

        private async Task UpdateSelectedFilterAsync(LogFilterNode newFilter)
        {
            SelectedFilter = newFilter;
            await SelectedFilterChanged.InvokeAsync(newFilter);
            await FiltersChanged.InvokeAsync();
        }

        private static LogFilterNode CreateFilterNode(LogFilterEditResult data)
        {
            return new LogFilterNode
            {
                Title = data.Title,
                FullTextSearch = data.SearchText,
                IsExclusion = data.IsExclusion
            };
        }

        private static void ApplyFilterData(LogFilterNode node, LogFilterEditResult data)
        {
            node.Title = data.Title;
            node.FullTextSearch = data.SearchText;
            node.IsExclusion = data.IsExclusion;
        }

        private static void ResetNode(LogFilterNode node)
        {
            node.Title = "All logs (day)";
            node.FullTextSearch = null;
            node.Filters.Clear();
            node.Children.Clear();
            node.IsDisabled = false;
            node.CurrentOffset = 0;
        }

        private LogFilterNode RemoveNode(LogFilterNode node)
        {
            if (node.Parent is null)
            {
                RootFilters = RootFilters.Where(f => f != node).ToList();
                return RootFilters.FirstOrDefault() ?? RootFilter;
            }

            node.Parent.Children.Remove(node);
            return node.Parent;
        }

        #endregion

        #region Injected Dependencies

        [Inject]
        public IDialogService DialogService { get; set; } = null!;

        #endregion

        #region Parameters

        [Parameter]
        [Notify]
        public LogFilterNode? SelectedFilter { get; set; }

        [Parameter]
        public EventCallback<LogFilterNode> SelectedFilterChanged { get; set; }

        [Parameter]
        public new bool Busy { get; set; }

        [Parameter]
        public EventCallback FiltersChanged { get; set; }

        #endregion

        #region Properties

        public LogFilterNode RootFilter { get; private set; } = null!;

        public List<LogFilterNode> RootFilters { get; private set; } = [];

        protected IReadOnlyCollection<TreeItemData<LogFilterNode>> TreeItems =>
            RootFilters.Select(CreateTreeItem).ToList();

        protected Guid TreeViewKey { get; private set; } = Guid.NewGuid();

        #endregion
    }
}
