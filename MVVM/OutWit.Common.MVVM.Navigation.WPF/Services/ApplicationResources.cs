using System;
using System.Windows;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.WPF.Interfaces;

namespace OutWit.Common.MVVM.Navigation.WPF.Services
{
    /// <summary>
    /// Default <see cref="IApplicationResources"/> over <c>Application.Current.Resources</c>.
    /// Every change is applied on the UI thread.
    /// </summary>
    public sealed class ApplicationResources : IApplicationResources
    {
        #region Fields

        private readonly IDispatcher m_dispatcher;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the service. Resolved from DI by <c>services.AddWpfNavigation()</c>.
        /// </summary>
        /// <param name="dispatcher">The UI-thread dispatcher.</param>
        public ApplicationResources(IDispatcher dispatcher)
        {
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        #endregion

        #region IApplicationResources

        public void AddResources(Uri source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            m_dispatcher.Invoke(() => Require().Resources.MergedDictionaries.Add(new ResourceDictionary { Source = source }));
        }

        public void AddResources(ResourceDictionary resources)
        {
            if (resources == null)
                throw new ArgumentNullException(nameof(resources));

            m_dispatcher.Invoke(() => Require().Resources.MergedDictionaries.Add(resources));
        }

        #endregion

        #region Tools

        private static Application Require()
        {
            return Application.Current
                   ?? throw new InvalidOperationException("Application.Current is null: resources can be added only after the application has started.");
        }

        #endregion
    }
}
