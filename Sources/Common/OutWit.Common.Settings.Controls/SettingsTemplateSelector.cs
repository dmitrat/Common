using System;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Controls
{
    public class SettingsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SettingsValueBoolean)
                return SettingsValueBooleanTemplate;

            if (item is SettingsValueFolder)
                return SettingsValueFolderTemplate;

            if (item is SettingsValueEnum)
                return SettingsValueEnumTemplate;

            if (item is SettingsValueStringList)
                return SettingsValueStringListTemplate;

            if (item is SettingsValueDouble)
                return SettingsValueDoubleTemplate;

            if (item is SettingsValueInteger)
                return SettingsValueIntegerTemplate;

            if (item is SettingsValuePath)
                return SettingsValuePathTemplate;

            if (item is SettingsValueString)
                return SettingsValueStringTemplate;

            return base.SelectTemplate(item, container);
        }

        public DataTemplate SettingsValueBooleanTemplate { get; set; } =
            DataTemplateUtils.Create<SettingsValueBoolean, SettingsValueBooleanControl>();

        public DataTemplate SettingsValueFolderTemplate { get; set; }=
            DataTemplateUtils.Create<SettingsValueFolder, SettingsValueFolderControl>();

        public DataTemplate SettingsValueEnumTemplate { get; set; }=
            DataTemplateUtils.Create<SettingsValueEnum, SettingsValueEnumControl>();

        public DataTemplate SettingsValueStringListTemplate { get; set; }=
            DataTemplateUtils.Create<SettingsValueStringList, SettingsValueStringListControl>();

        public DataTemplate SettingsValueDoubleTemplate { get; set; }=
            DataTemplateUtils.Create<SettingsValueDouble, SettingsValueDoubleControl>();

        public DataTemplate SettingsValueIntegerTemplate { get; set; }=
            DataTemplateUtils.Create<SettingsValueInteger, SettingsValueIntegerControl>();

        public DataTemplate SettingsValuePathTemplate { get; set; }=
            DataTemplateUtils.Create<SettingsValuePath, SettingsValuePathControl>();

        public DataTemplate SettingsValueStringTemplate { get; set; } =
            DataTemplateUtils.Create<SettingsValueString, SettingsValueStringControl>();
    }
}
