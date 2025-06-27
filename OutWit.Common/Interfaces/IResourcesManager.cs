using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Interfaces
{
    public interface IResourcesManager : IEnumerable<IResources>
    {
        void AddResourceDictionary(IResources resources);

        void ResetCulture(string cultureName);
    }
}
