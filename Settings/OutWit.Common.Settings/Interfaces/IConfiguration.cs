using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace OutWit.Common.Settings.Interfaces
{
    public interface IConfiguration
    {
        void Save();
        void CopyTo(string filePath);

        ConfigurationSection this[string key] { get; }

        string ConfigurationPath { get; }
    }
}
