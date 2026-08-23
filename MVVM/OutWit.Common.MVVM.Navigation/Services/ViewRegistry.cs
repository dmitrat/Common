using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Default <see cref="IViewRegistry"/>: a locked dictionary of view factories.
    /// </summary>
    public sealed class ViewRegistry : IViewRegistry
    {
        #region Fields

        private readonly object m_sync = new();
        private readonly Dictionary<Type, Func<IServiceProvider, object>> m_factories = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the registry, pre-loaded with the views AddNavigation collected.
        /// </summary>
        /// <param name="options">What AddNavigation collected; null means none.</param>
        public ViewRegistry(NavigationOptions? options = null)
        {
            if (options == null)
                return;

            foreach (var pair in options.Views)
                Register(pair.Key, pair.Value);
        }

        #endregion

        #region IViewRegistry

        public void Register<TViewModel, TView>()
            where TViewModel : class
            where TView : class
        {
            Register(typeof(TViewModel), typeof(TView));
        }

        public void Register(Type viewModelType, Type viewType)
        {
            if (viewType == null)
                throw new ArgumentNullException(nameof(viewType));

            Register(viewModelType, provider => ActivatorUtilities.CreateInstance(provider, viewType));
        }

        public void Register(Type viewModelType, Func<IServiceProvider, object> factory)
        {
            if (viewModelType == null)
                throw new ArgumentNullException(nameof(viewModelType));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (m_sync)
                m_factories[viewModelType] = factory;
        }

        public bool Contains(Type viewModelType)
        {
            if (viewModelType == null)
                return false;

            lock (m_sync)
                return m_factories.ContainsKey(viewModelType);
        }

        public bool TryGetFactory(Type viewModelType, [NotNullWhen(true)] out Func<IServiceProvider, object>? factory)
        {
            if (viewModelType == null)
            {
                factory = null;
                return false;
            }

            lock (m_sync)
                return m_factories.TryGetValue(viewModelType, out factory);
        }

        #endregion

        #region Properties

        public IReadOnlyCollection<Type> ViewModelTypes
        {
            get
            {
                lock (m_sync)
                    return m_factories.Keys.ToArray();
            }
        }

        #endregion
    }
}
