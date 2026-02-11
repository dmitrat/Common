using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Json
{
    public static class SettingsBuilderJsonExtensions
    {
        /// <summary>
        /// Configures JSON format using the conventional defaults path:
        /// <c>{AppContext.BaseDirectory}/Resources/settings.json</c>.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseJson(this SettingsBuilder builder)
        {
            return builder.UseJson(SettingsPathResolver.GetDefaultsPath(".json"));
        }

        /// <summary>
        /// Configures JSON format: registers the defaults file as read-only Default provider
        /// and sets a factory for auto-creating User/Global scope providers during Build().
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="defaultsPath">Path to the JSON file containing default settings.</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseJson(this SettingsBuilder builder, string defaultsPath)
        {
            builder.AddProvider(SettingsScope.Default,
                new JsonSettingsProvider(defaultsPath, isReadOnly: true));

            builder.SetScopeProviderFactory(".json",
                path => new JsonSettingsProvider(path, isReadOnly: false));

            return builder;
        }

        /// <summary>
        /// Adds a JSON file as a settings provider for the specified scope.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <param name="filePath">Absolute or relative path to the JSON file.</param>
        /// <param name="scope">The scope this provider serves (Default, Global, or User).</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder UseJsonFile(
            this SettingsBuilder builder, string filePath, SettingsScope scope)
        {
            var isReadOnly = scope == SettingsScope.Default;
            var provider = new JsonSettingsProvider(filePath, isReadOnly);

            return builder.AddProvider(scope, provider);
        }
    }
}
