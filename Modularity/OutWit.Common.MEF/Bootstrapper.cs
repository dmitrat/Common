using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;
using OutWit.Common.MEF.Interfaces;
using OutWit.Common.Utils;

namespace OutWit.Common.MEF
{
    public class Bootstrapper<TModule> : IEnumerable<TModule>
        where TModule : IModule
    {
        #region Fields

        private readonly List<TModule> m_modules = new ();

        #endregion

        #region Constants

        private const string DEFAULT_MODULE_FILTER = "*.module";

        #endregion

        #region Constructors

        public Bootstrapper(string modulePath, bool isAbsolute = false, string filter = DEFAULT_MODULE_FILTER,
            SearchOption option = SearchOption.TopDirectoryOnly)
        {
            ModulePath = modulePath;
            IsAbsolute = isAbsolute;
            Filter = filter;
            Option = option;
        }

        #endregion

        #region Functions

        public Bootstrapper<TModule> Run()
        {
            var catalog = CreateCatalog();
            var container = new CompositionContainer(catalog);

            foreach (var lazyModule in container.GetExports<TModule>())
            {
                var module = lazyModule.Value;

                m_modules.Add(module);
            }
            
            return this;
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

        #region IEnumerable

        public IEnumerator<TModule> GetEnumerator()
        {
            return m_modules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
        #region Properties

        public IReadOnlyCollection<TModule> Modules => m_modules;

        private string Filter { get; }
        private SearchOption Option { get; }
        private string ModulePath { get; }
        private bool IsAbsolute { get; }

        #endregion

    }
}
