using System;
using System.ComponentModel.Composition.Hosting;
using System.IO;

namespace OutWit.Common.MEF
{
    public class DirectoriesModuleCatalog : AggregateCatalog
    {
        public DirectoriesModuleCatalog(string modulePath, string filter = "*", SearchOption option = SearchOption.TopDirectoryOnly)
        {
            Catalogs.Add(new DirectoryCatalog(modulePath));
            foreach (var path in Directory.GetDirectories(modulePath, filter, option))
                Catalogs.Add(new DirectoryCatalog(path));
        }
    }
}
