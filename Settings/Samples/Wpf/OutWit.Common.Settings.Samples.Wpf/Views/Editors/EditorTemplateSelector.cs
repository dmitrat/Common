using System.Windows;
using System.Windows.Controls;
using OutWit.Common.Settings.Samples.Wpf.ViewModels;

namespace OutWit.Common.Settings.Samples.Wpf.Views.Editors
{
    /// <summary>
    /// Selects a DataTemplate based on the <see cref="SettingsValueViewModel.ValueKind"/>.
    /// </summary>
    public sealed class EditorTemplateSelector : DataTemplateSelector
    {
        #region Properties

        public DataTemplate? StringTemplate { get; set; }
        public DataTemplate? IntegerTemplate { get; set; }
        public DataTemplate? LongTemplate { get; set; }
        public DataTemplate? DoubleTemplate { get; set; }
        public DataTemplate? DecimalTemplate { get; set; }
        public DataTemplate? BooleanTemplate { get; set; }
        public DataTemplate? EnumTemplate { get; set; }
        public DataTemplate? PasswordTemplate { get; set; }
        public DataTemplate? FolderTemplate { get; set; }
        public DataTemplate? PathTemplate { get; set; }
        public DataTemplate? UrlTemplate { get; set; }
        public DataTemplate? ServiceUrlTemplate { get; set; }
        public DataTemplate? TimeSpanTemplate { get; set; }
        public DataTemplate? LanguageTemplate { get; set; }
        public DataTemplate? BoundedIntTemplate { get; set; }
        public DataTemplate? ColorRgbTemplate { get; set; }

        #endregion

        #region Functions

        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            if (item is not SettingsValueViewModel vm)
                return base.SelectTemplate(item, container);

            return vm.ValueKind switch
            {
                "String" => StringTemplate,
                "Integer" => IntegerTemplate,
                "Long" => LongTemplate,
                "Double" => DoubleTemplate,
                "Decimal" => DecimalTemplate,
                "Boolean" => BooleanTemplate,
                "Enum" => EnumTemplate,
                "Password" => PasswordTemplate,
                "Folder" => FolderTemplate,
                "Path" => PathTemplate,
                "Url" => UrlTemplate,
                "ServiceUrl" => ServiceUrlTemplate,
                "TimeSpan" => TimeSpanTemplate,
                "Language" => LanguageTemplate,
                "BoundedInt" => BoundedIntTemplate,
                "ColorRgb" => ColorRgbTemplate,
                _ => StringTemplate
            };
        }

        #endregion
    }
}
