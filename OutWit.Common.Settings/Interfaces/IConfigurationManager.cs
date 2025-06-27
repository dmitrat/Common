using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace OutWit.Common.Settings.Interfaces
{
    public interface IConfigurationManager
    {
        void Save();

        void ResetUserConfiguration();
        void MergeUserConfiguration();

        IConfiguration Default { get; }
        IConfiguration User { get; }

        string LocalAssemblyPath { get; }
        string RoamingAssemblyPath { get; }
    }
}
