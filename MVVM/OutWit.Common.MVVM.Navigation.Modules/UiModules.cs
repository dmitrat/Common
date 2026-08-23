using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Navigation.Modules.Interfaces;
using OutWit.Common.MVVM.Navigation.Modules.Model;
using OutWit.Common.Plugins;
using OutWit.Common.Plugins.Abstractions.Attributes;

namespace OutWit.Common.MVVM.Navigation.Modules
{
    /// <summary>
    /// The UI-module axis: loads modules from a folder and/or takes compiled-in ones, runs
    /// their two phases, and remembers which of them failed. Same shape as the other OutWit
    /// plugin axes: <c>RegisterServices</c> before the container is built,
    /// <c>InitializeAsync</c> after. Registered as a singleton by <c>AddUiModules</c>.
    /// </summary>
    public sealed class UiModules : IDisposable
    {
        #region Constants

        private const string PHASE_INITIALIZE = "Initialize";
        private const string PHASE_ON_INITIALIZED = "OnInitialized";

        #endregion

        #region Fields

        private readonly List<IUiModule> m_modules = new();
        private readonly Dictionary<IUiModule, string> m_names = new();
        private readonly HashSet<IUiModule> m_broken = new();
        private readonly List<UiModuleFailure> m_failures = new();

        private readonly UiModulesOptions m_options;
        private readonly ILogger? m_logger;

        private WitPluginLoader<IUiModule>? m_loader;
        private bool m_registered;
        private bool m_initialized;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the axis from its options. Nothing is loaded until <see cref="RegisterServices"/>.
        /// </summary>
        /// <param name="options">Folder, compiled-in modules, logger.</param>
        public UiModules(UiModulesOptions? options = null)
        {
            m_options = options ?? new UiModulesOptions();
            m_logger = m_options.Logger;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Phase one. Loads the folder modules, then lets every module — folder ones first,
        /// compiled-in ones after — register its services. Call before the container is built.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <exception cref="AggregateException">The folder could not be scanned: a bad manifest, a missing or circular dependency. Configuration errors fail fast.</exception>
        public void RegisterServices(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (m_registered)
                throw new InvalidOperationException("RegisterServices has already been called.");

            m_registered = true;

            if (m_options.ScanFolder)
                LoadFolder();

            foreach (var module in m_options.Modules)
                Add(module, module.GetType().Name);

            foreach (var module in m_modules)
            {
                try
                {
                    module.Initialize(services);
                }
                catch (Exception e)
                {
                    Fail(module, PHASE_INITIALIZE, e);
                }
            }

            services.AddSingleton(this);
        }

        /// <summary>
        /// Phase two. Gives every module that survived phase one the built container. Runs
        /// inline on the calling thread — call it on the UI thread, the registries marshal
        /// there anyway. Safe to call once.
        /// </summary>
        /// <param name="serviceProvider">The built container.</param>
        public Task InitializeAsync(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (!m_registered)
                throw new InvalidOperationException("RegisterServices must run before InitializeAsync.");

            if (m_initialized)
                return Task.CompletedTask;

            m_initialized = true;

            foreach (var module in m_modules)
            {
                if (m_broken.Contains(module))
                    continue;

                try
                {
                    module.OnInitialized(serviceProvider);
                }
                catch (Exception e)
                {
                    Fail(module, PHASE_ON_INITIALIZED, e);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// The name a module is known by: its manifest name, or its type name when compiled in.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>The name.</returns>
        public string NameOf(IUiModule module)
        {
            return m_names.TryGetValue(module, out var name) ? name : module.GetType().Name;
        }

        private void LoadFolder()
        {
            var folder = Path.IsPathRooted(m_options.Folder)
                ? m_options.Folder
                : Path.Combine(AppContext.BaseDirectory, m_options.Folder);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            m_loader = new WitPluginLoader<IUiModule>(folder, useIsolatedContexts: false, m_logger, m_options.FolderPattern);
            m_loader.Load();

            foreach (var module in m_loader.Plugins)
                Add(module, module.GetType().GetCustomAttribute<WitPluginManifestAttribute>()?.Name ?? module.GetType().Name);

            m_logger?.LogInformation("UI modules: {Count} loaded from {Folder}", m_loader.Plugins.Count, folder);
        }

        private void Add(IUiModule module, string name)
        {
            m_modules.Add(module);
            m_names[module] = name;
        }

        private void Fail(IUiModule module, string phase, Exception error)
        {
            m_broken.Add(module);
            m_failures.Add(new UiModuleFailure(NameOf(module), phase, error));
            m_logger?.LogError(error, "UI module {Module} failed in {Phase}", NameOf(module), phase);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            m_loader?.Dispose();
            m_loader = null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Every module, folder ones first in load order, then compiled-in ones in registration order.
        /// </summary>
        public IReadOnlyList<IUiModule> Modules => m_modules;

        /// <summary>
        /// The names of <see cref="Modules"/>, in the same order.
        /// </summary>
        public IReadOnlyList<string> Names => m_modules.Select(NameOf).ToArray();

        /// <summary>
        /// Modules that threw in one of their phases.
        /// </summary>
        public IReadOnlyList<UiModuleFailure> Failures => m_failures;

        #endregion
    }
}
