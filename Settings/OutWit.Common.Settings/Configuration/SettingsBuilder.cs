using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MemoryPack;
using MemoryPack.Formatters;
using Microsoft.Extensions.Logging;
using OutWit.Common.Settings.Aspects;
using OutWit.Common.Settings.Formatters;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Serialization;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Configuration
{
    public sealed class SettingsBuilder
    {
        #region Static Fields

        private static readonly object s_memoryPackLock = new();
        private static readonly HashSet<Type> s_registeredValueTypes = new();

        #endregion

        #region Fields

        private readonly List<(SettingsScope Scope, ISettingsProvider Provider)> m_providers = new();
        private readonly List<ISettingsSerializer> m_serializers = new();
        private readonly List<SettingsGroupInfo> m_configuredGroups = new();
        private readonly List<Type> m_containerTypes = new();

        private int m_depth = 1;
        private string? m_fileNameOverride;
        private string? m_scopeFileExtension;
        private Func<string, ISettingsProvider>? m_scopeProviderFactory;
        private Func<SettingsScope, ISettingsProvider>? m_scopeAwareProviderFactory;
        private ILogger? m_logger;

        #endregion

        #region Functions

        /// <summary>
        /// Registers a storage provider for the specified scope.
        /// </summary>
        public SettingsBuilder AddProvider(SettingsScope scope, ISettingsProvider provider)
        {
            m_providers.Add((scope, provider));
            return this;
        }

        /// <summary>
        /// Registers a custom serializer. Overrides built-in serializers with the same ValueKind.
        /// </summary>
        public SettingsBuilder AddSerializer(ISettingsSerializer serializer)
        {
            m_serializers.Add(serializer);
            return this;
        }

        /// <summary>
        /// Registers a settings container type. The manager will use <see cref="Aspects.SettingAttribute"/>
        /// from this type's properties to determine the scope of each setting.
        /// Entries in defaults without a matching property are ignored during Load().
        /// </summary>
        /// <typeparam name="T">The container type inheriting from <see cref="SettingsContainer"/>.</typeparam>
        public SettingsBuilder RegisterContainer<T>() where T : SettingsContainer
        {
            m_containerTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Configures group metadata (priority, display name) at build time.
        /// These overrides are applied after loading metadata from providers.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <param name="priority">The display priority (lower values appear first).</param>
        /// <param name="displayName">Optional display name override.</param>
        public SettingsBuilder ConfigureGroup(string group, int priority = 0, string displayName = "")
        {
            m_configuredGroups.Add(new SettingsGroupInfo
            {
                Group = group,
                Priority = priority,
                DisplayName = displayName
            });
            return this;
        }

        /// <summary>
        /// Sets the path depth for assembly name splitting in <see cref="SettingsPathResolver"/>.
        /// Default is 1. Example with depth=2: "OutWit/Settings/Example.Module.Json".
        /// </summary>
        /// <param name="depth">Number of leading name segments that become separate folders.</param>
        public SettingsBuilder WithDepth(int depth)
        {
            m_depth = depth;
            return this;
        }

        /// <summary>
        /// Overrides the auto-generated file name for scope providers.
        /// By default, the assembly name is used (e.g. "OutWit.Settings.Example.Module.Json").
        /// </summary>
        /// <param name="fileName">The file name without extension.</param>
        public SettingsBuilder WithFileName(string fileName)
        {
            m_fileNameOverride = fileName;
            return this;
        }

        /// <summary>
        /// Sets an optional logger for diagnostic messages.
        /// Logs lifecycle events (load, merge, delete) and errors without exposing setting values.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public SettingsBuilder WithLogger(ILogger logger)
        {
            m_logger = logger;
            return this;
        }

        /// <summary>
        /// Registers a factory for creating scope providers from file paths.
        /// Called by format-specific extensions (UseJson, UseCsv, UseDatabase).
        /// </summary>
        /// <param name="extension">File extension including dot (e.g. ".json").</param>
        /// <param name="factory">Factory that creates a writable provider from a file path.</param>
        public SettingsBuilder SetScopeProviderFactory(string extension, Func<string, ISettingsProvider> factory)
        {
            m_scopeFileExtension = extension;
            m_scopeProviderFactory = factory;
            return this;
        }

        /// <summary>
        /// Registers a scope-aware factory for creating providers without file paths.
        /// Used by shared-database extensions (UseSharedDatabase) where all scopes
        /// live in the same database and path-based routing is not applicable.
        /// </summary>
        /// <param name="factory">Factory that creates a writable provider for the given scope.</param>
        public SettingsBuilder SetScopeProviderFactory(Func<SettingsScope, ISettingsProvider> factory)
        {
            m_scopeAwareProviderFactory = factory;
            return this;
        }

        /// <summary>
        /// Registers MemoryPack Union formatter for <see cref="ISettingsValue"/>
        /// using the specified custom serializers (in addition to built-in ones).
        /// Registrations are accumulative — types from all prior <see cref="Build"/>
        /// and <see cref="RegisterMemoryPack"/> calls are preserved.
        /// Use on client side where no <see cref="SettingsManager"/> is needed.
        /// </summary>
        /// <param name="configure">Optional configuration to add custom serializers.</param>
        public static void RegisterMemoryPack(Action<SettingsBuilder>? configure = null)
        {
            var builder = new SettingsBuilder();
            configure?.Invoke(builder);
            builder.RegisterMemoryPackInternal();
        }

        /// <summary>
        /// Builds and returns a configured SettingsManager.
        /// Built-in serializers are registered automatically.
        /// When a scope provider factory is registered, User/Global scope providers
        /// are auto-created based on <see cref="SettingAttribute"/> scopes.
        /// MemoryPack formatter registration is accumulative — custom serializer types
        /// from all <see cref="Build"/> calls are preserved across invocations.
        /// </summary>
        public SettingsManager Build()
        {
            var manager = new SettingsManager(m_logger);

            foreach (var serializer in CreateBuiltInSerializers())
                manager.AddSerializer(serializer);

            foreach (var serializer in m_serializers)
                manager.AddSerializer(serializer);

            foreach (var (scope, provider) in m_providers)
                manager.AddProvider(scope, provider);

            foreach (var groupInfo in m_configuredGroups)
                manager.AddGroupOverride(groupInfo);

            foreach (var type in m_containerTypes)
                manager.RegisterContainerType(type);

            AutoCreateScopeProviders(manager);

            RegisterMemoryPackInternal();

            return manager;
        }

        #endregion

        #region Tools

        private void AutoCreateScopeProviders(SettingsManager manager)
        {
            if (m_scopeProviderFactory == null && m_scopeAwareProviderFactory == null)
                return;

            if (m_containerTypes.Count == 0)
                return;

            var explicitScopes = m_providers.Select(p => p.Scope).ToHashSet();
            var requiredScopes = ComputeRequiredScopes();
            var assembly = m_containerTypes[0].Assembly;

            foreach (var scope in new[] { SettingsScope.User, SettingsScope.Global })
            {
                if (explicitScopes.Contains(scope))
                    continue;

                if (!requiredScopes.Contains(scope))
                    continue;

                if (m_scopeAwareProviderFactory != null)
                {
                    manager.AddProvider(scope, m_scopeAwareProviderFactory(scope));
                    m_logger?.LogDebug("Auto-created {Scope} provider (shared database)", scope);
                }
                else if (m_scopeProviderFactory != null)
                {
                    var dir = SettingsPathResolver.GetScopeDataPath(scope, assembly, m_depth);
                    var name = m_fileNameOverride ?? assembly.GetName().Name ?? "Settings";
                    var path = Path.Combine(dir, $"{name}{m_scopeFileExtension}");

                    manager.AddProvider(scope, m_scopeProviderFactory(path));
                    m_logger?.LogDebug("Auto-created {Scope} provider: {Path}", scope, path);
                }
            }
        }

        private HashSet<SettingsScope> ComputeRequiredScopes()
        {
            var scopes = new HashSet<SettingsScope>();

            foreach (var type in m_containerTypes)
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attr = prop.GetCustomAttribute<SettingAttribute>();
                    if (attr != null)
                        scopes.Add(attr.Scope);
                }
            }

            return scopes;
        }

        private void RegisterMemoryPackInternal()
        {
            lock (s_memoryPackLock)
            {
                foreach (var serializer in CreateBuiltInSerializers())
                    s_registeredValueTypes.Add(serializer.ValueType);

                foreach (var serializer in m_serializers)
                    s_registeredValueTypes.Add(serializer.ValueType);

                var unionMembers = s_registeredValueTypes
                    .Select(vt => typeof(SettingsValue<>).MakeGenericType(vt))
                    .OrderBy(t => t.FullName, StringComparer.Ordinal)
                    .Select((t, i) => ((ushort)i, t))
                    .ToArray();

                MemoryPackFormatterProvider.Register(
                    new DynamicUnionFormatter<ISettingsValue>(unionMembers));

                MemoryPackFormatterProvider.Register(new SettingsObjectFormatter());
            }
        }

        /// <summary>
        /// Resets the accumulated MemoryPack type registrations.
        /// Intended for test isolation only.
        /// </summary>
        public static void ResetMemoryPackRegistrations()
        {
            lock (s_memoryPackLock)
            {
                s_registeredValueTypes.Clear();
            }
        }

        /// <summary>
        /// Returns the number of value types currently registered for MemoryPack serialization.
        /// Intended for test verification only.
        /// </summary>
        public static int MemoryPackRegistrationCount
        {
            get
            {
                lock (s_memoryPackLock)
                {
                    return s_registeredValueTypes.Count;
                }
            }
        }

        private static List<ISettingsSerializer> CreateBuiltInSerializers()
        {
            return new List<ISettingsSerializer>
            {
                new SettingsSerializerString(),
                new SettingsSerializerInteger(),
                new SettingsSerializerLong(),
                new SettingsSerializerDouble(),
                new SettingsSerializerDecimal(),
                new SettingsSerializerBoolean(),
                new SettingsSerializerDateTime(),
                new SettingsSerializerTimeSpan(),
                new SettingsSerializerGuid(),
                new SettingsSerializerEnum(),
                new SettingsSerializerEnumList(),
                new SettingsSerializerStringList(),
                new SettingsSerializerIntegerList(),
                new SettingsSerializerDoubleList(),
                new SettingsSerializerUrl(),
                new SettingsSerializerServiceUrl(),
                new SettingsSerializerPath(),
                new SettingsSerializerFolder(),
                new SettingsSerializerLanguage(),
                new SettingsSerializerPassword(),
            };
        }

        #endregion
    }
}
