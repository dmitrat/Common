using System.Configuration;
using System.IO;
using OutWit.Common.Exceptions;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Utils;

namespace OutWit.Common.Settings.Configuration
{
    public class Configuration : IConfiguration
    {
        #region Constructors

        public Configuration(string configurationPath)
        {
            ConfigurationPath = configurationPath.CheckConfigurationPath();

            if(!File.Exists(ConfigurationPath))
                throw new ExceptionOf<IConfiguration>($"Can not find configuration file: {ConfigurationPath}");

            LoadConfiguration();
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Inner.Sections.Count} sections, location: {ConfigurationPath}";
        }

        public void Save()
        {
            Inner.Save();
        }

        public void CopyTo(string configurationPath)
        {
            Inner.SaveAs(configurationPath.CheckConfigurationPath());

            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            Inner = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(
                new ExeConfigurationFileMap { ExeConfigFilename = ConfigurationPath },
                ConfigurationUserLevel.None);
        }

        #endregion

        #region Properties

        public ConfigurationSection this[string key] => Inner?.Sections[key];

        public string ConfigurationPath { get; }

        private System.Configuration.Configuration Inner { get; set; }

        #endregion
    }
}
