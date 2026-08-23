using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Navigation.Modules.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Modules.Model
{
    /// <summary>
    /// What <c>services.AddUiModules(o => ...)</c> hands the application: where to look for
    /// module folders, and which modules are compiled in.
    /// </summary>
    public sealed class UiModulesOptions
    {
        #region Constants

        /// <summary>
        /// The folder next to the application that is scanned by default.
        /// </summary>
        public const string DEFAULT_FOLDER = "@Modules";

        #endregion

        #region Fields

        private readonly List<IUiModule> m_modules = new();

        #endregion

        #region Functions

        /// <summary>
        /// Adds a module that is compiled into the application rather than loaded from a folder.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>These options.</returns>
        public UiModulesOptions AddModule(IUiModule module)
        {
            m_modules.Add(module ?? throw new ArgumentNullException(nameof(module)));
            return this;
        }

        /// <summary>
        /// Adds a module that is compiled into the application rather than loaded from a folder.
        /// </summary>
        /// <typeparam name="TModule">The module type.</typeparam>
        /// <returns>These options.</returns>
        public UiModulesOptions AddModule<TModule>()
            where TModule : IUiModule, new()
        {
            return AddModule(new TModule());
        }

        #endregion

        #region Properties

        /// <summary>
        /// The module folder, absolute or relative to the application base directory.
        /// Created when missing.
        /// </summary>
        public string Folder { get; set; } = DEFAULT_FOLDER;

        /// <summary>
        /// Whether <see cref="Folder"/> is scanned at all. False for applications whose
        /// modules are all compiled in.
        /// </summary>
        public bool ScanFolder { get; set; } = true;

        /// <summary>
        /// The sub-folder mask the loader looks for; null means the loader's default (<c>*.module</c>).
        /// </summary>
        public string? FolderPattern { get; set; }

        /// <summary>
        /// Logger for the loader and for module failures. Optional: at registration time
        /// there is no container to resolve one from.
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Modules compiled into the application. Initialized after the folder modules.
        /// </summary>
        public IReadOnlyList<IUiModule> Modules => m_modules;

        #endregion
    }
}
