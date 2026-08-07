using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Licensing.Crypto;

namespace OutWit.Common.Licensing.Storage
{
    /// <summary>
    /// An in-memory store, for tests, samples and hosts that manage licence
    /// persistence themselves (an operator pasting a token into a container's
    /// environment, for instance).
    /// </summary>
    public sealed class LicenseStoreMemory : ILicenseStore
    {
        #region Fields

        private readonly Dictionary<string, string> m_tokens = new();

        private LicenseStoreState m_state = new();

        #endregion

        #region Constructors

        public LicenseStoreMemory(params string[] tokens)
        {
            foreach (var token in tokens.Where(token => !string.IsNullOrWhiteSpace(token)))
                Save(token);
        }

        #endregion

        #region ILicenseStore

        public IReadOnlyList<string> ReadTokens()
        {
            return m_tokens.Values.ToList();
        }

        public void Save(string token)
        {
            var parsed = LicenseToken.Parse(token);
            if (parsed == null)
                throw new ArgumentException("The value is not a licence token.", nameof(token));

            m_tokens[parsed.Payload.Id] = parsed.Raw;
        }

        public bool Remove(string licenseId)
        {
            return m_tokens.Remove(licenseId);
        }

        public LicenseStoreState ReadState()
        {
            return m_state.Clone();
        }

        public void WriteState(LicenseStoreState state)
        {
            m_state = state.Clone();
        }

        #endregion
    }
}
