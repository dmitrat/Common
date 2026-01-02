using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace OutWit.Common.MVVM.Blazor.Utils
{
    /// <summary>
    /// Extension methods for Blazor components.
    /// </summary>
    public static class ComponentExtensions
    {
        /// <summary>
        /// Executes an action with busy state management on a component.
        /// </summary>
        public static async Task RunWithBusyAsync(this ComponentBase component, Func<bool> getBusy, Action<bool> setBusy, Func<Task> action)
        {
            try
            {
                setBusy(true);
                await action();
            }
            finally
            {
                setBusy(false);
                InvokeStateHasChanged(component);
            }
        }

        /// <summary>
        /// Forces a UI update on the component.
        /// </summary>
        public static void ForceUpdate(this ComponentBase component)
        {
            InvokeStateHasChanged(component);
        }

        private static void InvokeStateHasChanged(ComponentBase component)
        {
            var method = typeof(ComponentBase).GetMethod("StateHasChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            method?.Invoke(component, null);
        }
    }
}
