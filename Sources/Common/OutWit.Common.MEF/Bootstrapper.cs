using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;
using System.Text;
using OutWit.Common.MEF.Interfaces;
using OutWit.Common.Services.Interfaces;
using OutWit.Common.Utils;

namespace OutWit.Common.MEF
{
    public class Bootstrapper<TModule>
        where TModule : IModule
    {
        #region Fields

        private readonly List<TModule> m_modules = new List<TModule>();

        #endregion

        #region Constants

        private const string DEFAULT_MODULE_FILTER = "*.module";

        #endregion

        #region Constructors

        public Bootstrapper(IServiceContainer container, string modulePath,
            bool isAbsolute = false,
            string filter = DEFAULT_MODULE_FILTER,
            SearchOption option = SearchOption.TopDirectoryOnly)
        {
            Container = container;

            ModulePath = modulePath;
            IsAbsolute = isAbsolute;
            Filter = filter;
            Option = option;

        }

        #endregion

        #region Functions

        public void Run()
        {
            var catalog = CreateCatalog();
            var container = new CompositionContainer(catalog);

            foreach (var moduleContainer in container.GetExports<TModule>())
            {
                var module = moduleContainer.Value;

                module.Initialize(Container);

                m_modules.Add(module);
            }
        }

        private DirectoriesModuleCatalog CreateCatalog()
        {
            var modulePath = IsAbsolute
                ? ModulePath
                : Assembly.GetExecutingAssembly().AssemblyDirectory().AppendPath(ModulePath);

            if (!Directory.Exists(modulePath))
                Directory.CreateDirectory(modulePath);

            return new DirectoriesModuleCatalog(modulePath, Filter, Option);
        }

        #endregion

        #region Properties

        public IReadOnlyCollection<TModule> Modules => m_modules;
        private IServiceContainer Container { get; }

        private string Filter { get; }
        private SearchOption Option { get; }
        private string ModulePath { get; }
        private bool IsAbsolute { get; }

        #endregion
    }
}
