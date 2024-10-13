using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Configuration
{
    public class ResourcesMerged : IResources
    {
        #region Constructors

        public ResourcesMerged(IEnumerable<IResources> resources)
        {
            Inner = resources;
        }

        #endregion

        #region Functions

        private string GetResource(string key)
        {
            foreach (var resources in Inner)
            {
                if (resources.HasStringFor(key))
                    return resources[key];
            }

            return key;
        }

        private string GetResource(string key, CultureInfo culture)
        {
            foreach (var resources in Inner)
            {
                if(resources.HasStringFor(key, culture))
                    return resources[key, culture];
            }

            return key;
        }

        #endregion

        #region IResources

        public void ResetCulture(string cultureName)
        {
            foreach (var resources in Inner)
                resources.ResetCulture(cultureName);
        }

        public bool HasStringFor(string key)
        {
            return Inner.Any(resources => resources.HasStringFor(key));
        }

        public bool HasStringFor(string key, CultureInfo culture)
        {
            return Inner.Any(resources => resources.HasStringFor(key, culture));
        }

        #endregion

        #region Properties

        public string this[string key] => GetResource(key);

        public string this[string key, CultureInfo culture] => GetResource(key, culture);

        private IEnumerable<IResources> Inner { get; }

        #endregion
    }
}
