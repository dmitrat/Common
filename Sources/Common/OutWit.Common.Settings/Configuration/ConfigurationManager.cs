using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Utils;
using OutWit.Common.Utils;

namespace OutWit.Common.Settings.Configuration
{
    public class ConfigurationManager : IConfigurationManager
    {
        #region Constructors

        public ConfigurationManager(Assembly assembly, bool useUserConfiguration, int maxDepth = int.MaxValue, string configurationFolder = null)
        {
            InitDefaults(assembly, maxDepth, configurationFolder);
            OpenDefaultConfiguration();

            if (useUserConfiguration)
                OpenUserConfiguration();
        }

        #endregion

        #region Initialization

        private void InitDefaults(Assembly assembly, int maxDepth, string configurationFolder)
        {
            LocalAssemblyPath = assembly.LocalAssemblyPath();
            RoamingAssemblyPath = assembly.RoamingAssemblyPath(maxDepth, configurationFolder);
        }

        #endregion

        #region Functions

        public void Save()
        {
            try
            {
                Default.Save();
            }
            catch (Exception e)
            {
                
            }

            try
            {
                User?.Save();
            }
            catch (Exception e)
            {

            }
        }

        public void ResetUserConfiguration()
        {
            Default.CopyTo(RoamingAssemblyPath);

            OpenDefaultConfiguration();
            OpenUserConfiguration();
        }

        public void MergeUserConfiguration()
        {
            //TODO:Implement!!!
        }

        private void OpenDefaultConfiguration()
        {
            Default = new Configuration(LocalAssemblyPath);
        }

        private void OpenUserConfiguration()
        {
            if (!RoamingAssemblyPath.ConfigurationExists())
                ResetUserConfiguration();

            User = new Configuration(RoamingAssemblyPath); 
        }


        #endregion

        #region Properties

        public string RoamingAssemblyPath { get; private set; }
        public string LocalAssemblyPath { get; private set; }

        public IConfiguration Default { get; private set; }
        public IConfiguration User { get; private set; }

        #endregion
    }
}
