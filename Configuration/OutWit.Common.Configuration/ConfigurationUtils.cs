using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using OutWit.Common.Configuration.Attributes;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Configuration
{
    public static class ConfigurationUtils
    {
        private const string DEFAULT_CONFIG_FILE_NAME = "appsettings";

        /// <summary>
        /// Creates a new <see cref="ConfigurationInfo"/> for the specified assembly.
        /// The configuration file lookup uses
        /// <c>Path.GetDirectoryName(assembly.Location)</c> as its base path.
        /// </summary>
        /// <param name="assembly">The assembly whose directory will be used as the base path for configuration files.</param>
        /// <returns>A new <see cref="ConfigurationInfo"/> instance.</returns>
        public static ConfigurationInfo For(Assembly assembly)
        {
            return new ConfigurationInfo(assembly);
        }

        /// <summary>
        /// Creates a new <see cref="ConfigurationInfo"/> for the specified
        /// <see cref="IAssemblyContext"/>. The configuration file lookup uses
        /// <see cref="IAssemblyContext.HomeDirectory"/> as its base path —
        /// useful when the producer of the context knows the assembly was
        /// staged somewhere other than
        /// <see cref="System.Reflection.Assembly.Location"/> reports
        /// (plugin loaders, embedded resource bundles, etc.).
        /// </summary>
        /// <param name="context">The assembly context whose
        /// <see cref="IAssemblyContext.HomeDirectory"/> will be used as the
        /// base path for configuration files.</param>
        /// <returns>A new <see cref="ConfigurationInfo"/> instance.</returns>
        public static ConfigurationInfo For(IAssemblyContext context)
        {
            return new ConfigurationInfo(context);
        }

        /// <summary>
        /// Sets the base file name for configuration files (without extension).
        /// </summary>
        /// <param name="me">The configuration info instance.</param>
        /// <param name="fileName">The base file name (e.g. "appsettings").</param>
        /// <returns>The same <see cref="ConfigurationInfo"/> instance for fluent chaining.</returns>
        public static ConfigurationInfo WithFileName(this ConfigurationInfo me, string fileName)
        {
            me.FileName = fileName;
            return me;
        }

        /// <summary>
        /// Sets the environment name used to load environment-specific configuration overrides.
        /// </summary>
        /// <param name="me">The configuration info instance.</param>
        /// <param name="environment">The environment name (e.g. "Development", "Production").</param>
        /// <returns>The same <see cref="ConfigurationInfo"/> instance for fluent chaining.</returns>
        public static ConfigurationInfo WithEnvironment(this ConfigurationInfo me, string environment)
        {
            me.Environment = environment;
            return me;
        }

        /// <summary>
        /// Sets the environment using a <see cref="ConfigurationEnvironment"/> enum value.
        /// </summary>
        /// <param name="me">The configuration info instance.</param>
        /// <param name="environment">The environment to use.</param>
        /// <returns>The same <see cref="ConfigurationInfo"/> instance for fluent chaining.</returns>
        public static ConfigurationInfo WithEnvironment(this ConfigurationInfo me, ConfigurationEnvironment environment)
        {
            me.Environment = $"{environment}";
            return me;
        }

        /// <summary>
        /// Makes the JSON providers watch their files and reload on change.
        /// Off by default: every reloading provider pins a
        /// <see cref="System.IO.FileSystemWatcher"/> (one inotify instance on Linux)
        /// for the lifetime of the built configuration - a configuration built per
        /// request or per unit of work would leak a watcher each time and eventually
        /// exhaust <c>fs.inotify.max_user_instances</c>. Opt in only for a configuration
        /// that lives as long as the process and must track edits.
        /// </summary>
        /// <param name="me">The configuration info instance.</param>
        /// <param name="reloadOnChange">Whether to watch the files; <c>true</c> when omitted.</param>
        /// <returns>The same <see cref="ConfigurationInfo"/> instance for fluent chaining.</returns>
        public static ConfigurationInfo WithReloadOnChange(this ConfigurationInfo me, bool reloadOnChange = true)
        {
            me.ReloadOnChange = reloadOnChange;
            return me;
        }

        /// <summary>
        /// Builds an <see cref="IConfiguration"/> instance from JSON files located in
        /// <see cref="ConfigurationInfo.BasePath"/>. The files are read once; they are
        /// watched for changes only when <see cref="ConfigurationInfo.ReloadOnChange"/>
        /// was requested (see <see cref="WithReloadOnChange"/>).
        /// </summary>
        /// <param name="me">The configuration info instance.</param>
        /// <returns>A built <see cref="IConfiguration"/> instance.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the configuration base path cannot be resolved (e.g. an assembly-based
        /// <see cref="ConfigurationInfo"/> built from an assembly whose
        /// <see cref="System.Reflection.Assembly.Location"/> has no directory component).</exception>
        public static IConfiguration Build(this ConfigurationInfo me)
        {
            if (me.BasePath is null)
                throw new DirectoryNotFoundException(
                    me.Assembly is not null
                        ? $"Cannot find directory for component {me.Assembly.FullName}."
                        : "ConfigurationInfo was constructed without a usable base path.");

            var configFileName = string.IsNullOrEmpty(me.FileName)
                ? DEFAULT_CONFIG_FILE_NAME
                : me.FileName;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(me.BasePath)
                .AddJsonFile($"{configFileName}.json", optional: true, reloadOnChange: me.ReloadOnChange);

            if (!string.IsNullOrEmpty(me.Environment))
                configBuilder.AddJsonFile($"{configFileName}.{me.Environment}.json", optional: true, reloadOnChange: me.ReloadOnChange);

            return configBuilder.Build();
        }

        /// <summary>
        /// Binds configuration sections to a new instance of <typeparamref name="TSettings"/>.
        /// Uses <see cref="ConfigSectionAttribute"/> to map properties to custom section names.
        /// </summary>
        /// <typeparam name="TSettings">The settings type with a parameterless constructor.</typeparam>
        /// <param name="me">The configuration instance.</param>
        /// <returns>A populated settings instance.</returns>
        public static TSettings BindSettings<TSettings>(this IConfiguration me)
            where TSettings : new()
        {
            var settings = new TSettings();
            var properties = typeof(TSettings).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(info => info.CanRead && info.CanWrite);

            foreach (var property in properties)
            {
                var sectionName = property.GetCustomAttribute<ConfigSectionAttribute>()?.Name
                                  ?? property.Name;

                var section = me.GetSection(sectionName);
                if (!section.Exists())
                    continue;

                var value = section.Get(property.PropertyType);
                if (value is not null)
                    property.SetValue(settings, value);
            }

            return settings;
        }
    }
}
