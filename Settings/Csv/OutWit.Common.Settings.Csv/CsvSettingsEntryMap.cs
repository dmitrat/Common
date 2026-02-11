using CsvHelper.Configuration;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Csv
{
    internal sealed class CsvSettingsEntryMap : ClassMap<SettingsEntry>
    {
        public CsvSettingsEntryMap()
        {
            Map(m => m.Group).Index(0).Name("Group");
            Map(m => m.Key).Index(1).Name("Key");
            Map(m => m.Value).Index(2).Name("Value");
            Map(m => m.ValueKind).Index(3).Name("ValueKind");
            Map(m => m.Tag).Index(4).Name("Tag");
            Map(m => m.Hidden).Index(5).Name("Hidden");
        }
    }
}
