using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using OutWit.Common.Utils;

namespace OutWit.Common.Settings.Utils
{
    public static class Extensions
    {
        #region Constants

        private const string CONFIGURATION_FILE_EXTENSION = ".config";

        #endregion

        #region Functions

        public static string CheckConfigurationPath(this string me)
        {
            if (Path.GetExtension(me)?.ToLower() != CONFIGURATION_FILE_EXTENSION)
                me = $"{me}{CONFIGURATION_FILE_EXTENSION}";

            return me;
        }

        public static bool ConfigurationExists(this string me)
        {
            return File.Exists(me.CheckConfigurationPath());
        }

        public static string LocalAssemblyPath(this Assembly me)
        {
            return me.Location;
        }

        public static string RoamingAssemblyPath(this Assembly me, int maxDepth, string configurationFolder)
        {
            var assemblyName = me.GetName().Name;

            int depth = 0;
            var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foreach (var part in assemblyName.Split('.'))
            {
                if(depth == maxDepth)
                    break;

                baseFolder = baseFolder.AppendPath(part);
                depth++;
            }

            if (!string.IsNullOrEmpty(configurationFolder))
                baseFolder = baseFolder.AppendPath(configurationFolder);

            return me.ConfigurationFilePath(baseFolder);
        }

        public static string ConfigurationFilePath(this Assembly me, string baseFolder)
        {
            if (!baseFolder.CheckFolder())
                return null;

            return baseFolder.AppendPath(Path.GetFileName(me.Location));
        }

        #endregion
    }
}
