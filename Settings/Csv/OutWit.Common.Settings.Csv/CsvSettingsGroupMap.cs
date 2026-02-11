using CsvHelper.Configuration;

namespace OutWit.Common.Settings.Csv
{
    internal sealed class CsvSettingsGroupMap : ClassMap<CsvSettingsGroupRecord>
    {
        public CsvSettingsGroupMap()
        {
            Map(m => m.Group).Index(0).Name("Group");
            Map(m => m.DisplayName).Index(1).Name("DisplayName");
            Map(m => m.Priority).Index(2).Name("Priority");
        }
    }
}
