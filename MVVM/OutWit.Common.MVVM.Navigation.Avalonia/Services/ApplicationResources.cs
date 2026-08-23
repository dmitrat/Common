using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Services
{
    /// <summary>
    /// Default <see cref="IApplicationResources"/> over <c>Application.Current</c>. Every
    /// change is applied on the UI thread.
    /// </summary>
    public sealed class ApplicationResources : IApplicationResources
    {
        #region Fields

        private readonly IDispatcher m_dispatcher;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the service. Resolved from DI by <c>services.AddAvaloniaNavigation()</c>.
        /// </summary>
        /// <param name="dispatcher">The UI-thread dispatcher.</param>
        public ApplicationResources(IDispatcher dispatcher)
        {
            m_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        #endregion

        #region IApplicationResources

        public void AddStyles(Uri source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            AddStyle(new StyleInclude(source) { Source = source });
        }

        public void AddStyle(IStyle style)
        {
            if (style == null)
                throw new ArgumentNullException(nameof(style));

            m_dispatcher.Invoke(() => Require().Styles.Add(style));
        }

        public void AddResources(Uri source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            AddResources(new ResourceInclude(source) { Source = source });
        }

        public void AddResources(IResourceProvider resources)
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
                   ?? throw new InvalidOperationException("Application.Current is null: resources can be added only after the application has initialized.");
        }

        #endregion
    }
}
