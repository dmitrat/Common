using System;
using System.Collections.Generic;
using System.Linq;

namespace OutWit.Common.Licensing.Storage
{
    /// <summary>
    /// Reads licences from an environment variable — the Docker and compose
    /// path, where there is no installer to drop a file and no admin screen to
    /// paste into before the container first starts.
    /// <para>
    /// The default name follows the ecosystem's <c>Section__Key</c> convention,
    /// so it binds the same way every other setting in a compose file does.
    /// Several licences may be supplied at once, separated by semicolons or
    /// newlines: a renewal staged beside a live licence is the normal case, and
    /// a single-slot variable would force the swap to happen exactly at expiry.
    /// </para>
    /// <para>
    /// <b>Read-only by construction.</b> Whoever set the variable owns it, so
    /// <see cref="Save"/> refuses rather than pretending: a store that silently
    /// dropped an installed licence would produce a panel reporting success over
    /// a licence that vanished on the next restart. Pair it with a writable
    /// store through <see cref="LicenseStoreComposite"/> when the product also
    /// needs to accept a paste.
    /// </para>
    /// </summary>
    public sealed class LicenseStoreEnvironment : ILicenseStore
    {
        #region Constants

        /// <summary>The variable read when none is named.</summary>
        public const string DEFAULT_VARIABLE = "Licensing__License";

        private static readonly char[] SEPARATORS = { ';', '\r', '\n' };

        #endregion

        #region Fields

        private readonly string m_variable;

        #endregion

        #region Constructors

        public LicenseStoreEnvironment()
            : this(DEFAULT_VARIABLE)
        {
        }

        public LicenseStoreEnvironment(string variable)
        {
            m_variable = string.IsNullOrWhiteSpace(variable) ? DEFAULT_VARIABLE : variable;
        }

        #endregion

        #region ILicenseStore

        public IReadOnlyList<string> ReadTokens()
        {
            string? value;

            try
            {
                value = Environment.GetEnvironmentVariable(m_variable);
            }
            catch (System.Security.SecurityException)
            {
                // A host that refuses to hand out its environment is a
                // deployment fact, not a licensing failure.
                return Array.Empty<string>();
            }

            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value!
                .Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim())
                .Where(token => token.Length > 0)
                .ToList();
        }

        public void Save(string token)
        {
            throw new NotSupportedException(
                $"Licences supplied through '{m_variable}' are owned by whoever set the variable and cannot be " +
                "installed by the product. Combine this store with a writable one through LicenseStoreComposite.");
        }

        public bool Remove(string licenseId)
        {
            return false;
        }

        /// <summary>
        /// Nothing observed. The sidecar records a first run and a clock
        /// high-water mark, neither of which an environment variable can hold.
        /// </summary>
        public LicenseStoreState ReadState()
        {
            return new LicenseStoreState();
        }

        public void WriteState(LicenseStoreState state)
        {
        }

        #endregion

        #region Properties

        /// <summary>The variable this store reads.</summary>
        public string Variable => m_variable;

        #endregion
    }
}
