using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Csv
{
    public static class SettingsBuilderCsvExtensions
    {
        /// <summary>
        /// Configures CSV format using the conventional defaults path:
        /// <c>{AppContext.BaseDirectory}/Resources/settings.csv</c>.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseCsv(this SettingsBuilder builder)
        {
            return builder.UseCsv(SettingsPathResolver.GetDefaultsPath(".csv"));
        }

        /// <summary>
        /// Configures CSV format: registers the defaults file as read-only Default provider
        /// and sets a factory for auto-creating User/Global scope providers during Build().
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="defaultsPath">Path to the CSV file containing default settings.</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseCsv(this SettingsBuilder builder, string defaultsPath)
        {
            builder.AddProvider(SettingsScope.Default,
                new CsvSettingsProvider(defaultsPath, isReadOnly: true));

            builder.SetScopeProviderFactory(".csv",
                path => new CsvSettingsProvider(path, isReadOnly: false));

            return builder;
        }

        /// <summary>
        /// Adds a CSV file as a settings provider for the specified scope.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="filePath">Absolute or relative path to the CSV file.</param>
        /// <param name="scope">The scope this provider serves (Default, Global, or User).</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseCsvFile(
            this SettingsBuilder builder, string filePath, SettingsScope scope)
        {
            var isReadOnly = scope == SettingsScope.Default;
            var provider = new CsvSettingsProvider(filePath, isReadOnly);

            return builder.AddProvider(scope, provider);
        }
    }
}
