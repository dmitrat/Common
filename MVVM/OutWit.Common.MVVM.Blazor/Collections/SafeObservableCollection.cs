using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.MVVM.Blazor.Collections
{
    /// <summary>
    /// Thread-safe ObservableCollection for Blazor that notifies StateHasChanged when modified.
    /// </summary>
    public class SafeObservableCollection<T> : ObservableCollection<T>
    {
        #region Fields

        private readonly ComponentBase? m_component;
        private readonly Action? m_stateHasChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a SafeObservableCollection without automatic StateHasChanged notifications.
        /// </summary>
        public SafeObservableCollection()
        {
        }

        /// <summary>
        /// Creates a SafeObservableCollection that calls StateHasChanged on the component when modified.
        /// </summary>
        public SafeObservableCollection(ComponentBase component)
        {
            m_component = component ?? throw new ArgumentNullException(nameof(component));
            m_stateHasChanged = GetStateHasChanged(component);
        }

        /// <summary>
        /// Creates a SafeObservableCollection with a custom StateHasChanged action.
        /// </summary>
        public SafeObservableCollection(Action stateHasChanged)
        {
            m_stateHasChanged = stateHasChanged ?? throw new ArgumentNullException(nameof(stateHasChanged));
        }

        #endregion

        #region ObservableCollection

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            m_stateHasChanged?.Invoke();
        }

        #endregion

        #region Functions

        private static Action GetStateHasChanged(ComponentBase component)
        {
            var method = typeof(ComponentBase).GetMethod("StateHasChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
                throw new InvalidOperationException("Could not find StateHasChanged method on ComponentBase");

            return () => method.Invoke(component, null);
        }

        #endregion
    }
}
