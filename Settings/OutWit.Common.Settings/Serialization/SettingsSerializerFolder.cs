using System;

namespace OutWit.Common.Settings.Serialization
{
    public sealed class SettingsSerializerFolder : SettingsSerializerString
    {
        #region Functions

        public override string Parse(string value, string tag)
        {
            return Environment.ExpandEnvironmentVariables(value);
        }

        public override string Format(string value)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            var result = value;

            if (!string.IsNullOrEmpty(appData))
                result = result.Replace(appData, "%APPDATA%");

            if (!string.IsNullOrEmpty(programData))
                result = result.Replace(programData, "%PROGRAMDATA%");

            return result;
        }

        #endregion

        #region Properties

        public override string ValueKind => "Folder";

        #endregion
    }
}
