using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Settings.Interfaces
{
    public interface ISettingsManager : IEnumerable<SettingsCollection>
    {
        event SettingsManagerEventHandler SettingsUpdated;

        void AddCollection(SettingsCollection collection);
        void AddConfiguration(IConfigurationManager configuration);

        void ResetUserConfiguration();
        void MergeUserConfiguration();

        void Rebuild();
        void Reset();

        void Update();

        IReadOnlyList<SettingsCollection> Collections { get; }
    }

    public delegate void SettingsManagerEventHandler();
}
