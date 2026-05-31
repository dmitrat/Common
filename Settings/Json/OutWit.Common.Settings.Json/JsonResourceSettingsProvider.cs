using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OutWit.Common.Settings.Json
{
    /// <summary>
    /// Read-only JSON settings provider that sources its document from an embedded assembly
    /// resource instead of a file. Intended for the <c>Default</c> scope so an application's
    /// immutable default settings ship inside the binary rather than as a loose file on disk
    /// (which, for example, breaks macOS app-bundle code signing). Parsing, group enumeration
    /// and group-info reading are inherited verbatim from <see cref="JsonSettingsProvider"/>,
    /// so merge behaviour is identical to a file-based defaults provider.
    /// </summary>
    public sealed class JsonResourceSettingsProvider : JsonSettingsProvider
    {
        #region Fields

        private readonly string m_json;

        #endregion

        #region Constructors

        /// <summary>
        /// Loads the embedded resource <paramref name="resourceName"/> from <paramref name="assembly"/>.
        /// When no resource matches the name exactly, the first resource whose manifest name ends with
        /// <paramref name="resourceName"/> is used (tolerates the root-namespace prefix, e.g.
        /// passing <c>"settings.json"</c> to match <c>"MyApp.settings.json"</c>).
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The embedded resource name (full manifest name or a suffix).</param>
        public JsonResourceSettingsProvider(Assembly assembly, string resourceName)
            : base(isReadOnly: true)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (string.IsNullOrEmpty(resourceName))
                throw new ArgumentNullException(nameof(resourceName));

            var names = assembly.GetManifestResourceNames();
            var name = names.Contains(resourceName)
                ? resourceName
                : names.FirstOrDefault(n => n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            if (name == null)
                throw new InvalidOperationException(
                    $"Embedded resource '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.");

            using var stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Could not open embedded resource '{name}'.");
            using var reader = new StreamReader(stream);
            m_json = reader.ReadToEnd();
        }

        #endregion

        #region Source

        /// <inheritdoc />
        protected override string? ReadRawJson() => m_json;

        #endregion
    }
}
