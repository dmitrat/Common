using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using OutWit.Common.Configuration.Attributes;

namespace OutWit.Common.Configuration
{
    public static class ConfigurationUtils
    {
        private const string DEFAULT_CONFIG_FILE_NAME = "appsettings";

        /// <summary>
        /// Creates a new <see cref="ConfigurationInfo"/> for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly whose directory will be used as the base path for configuration files.</param>
        /// <returns>A new <see cref="ConfigurationInfo"/> instance.</returns>
        public static ConfigurationInfo For(Assembly assembly)
        {
            return new ConfigurationInfo(assembly);
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
        /// Builds an <see cref="IConfiguration"/> instance from JSON files located in the assembly's directory.
        /// </summary>
        /// <param name="me">The configuration info instance.</param>
        /// <returns>A built <see cref="IConfiguration"/> instance.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the assembly directory cannot be found.</exception>
        public static IConfiguration Build(this ConfigurationInfo me)
        {
            var componentDirectory = Path.GetDirectoryName(me.Assembly.Location);
            if (componentDirectory is null)
                throw new DirectoryNotFoundException($"Cannot find directory for component {me.Assembly.FullName}.");
            
            var configFileName = string.IsNullOrEmpty(me.FileName) 
                ? DEFAULT_CONFIG_FILE_NAME 
                : me.FileName;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(componentDirectory)
                .AddJsonFile($"{configFileName}.json", optional: true, reloadOnChange: true);

            if (!string.IsNullOrEmpty(me.Environment))
                configBuilder.AddJsonFile($"{configFileName}.{me.Environment}.json", optional: true, reloadOnChange: true);

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
