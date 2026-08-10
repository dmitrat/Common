using System;
using System.Collections.Generic;
using System.Linq;

namespace OutWit.Common.Licensing.Storage
{
    /// <summary>
    /// Reads licences from several places at once and writes to exactly one.
    /// <para>
    /// A deployment routinely has more than one delivery path — an environment
    /// variable set by compose, a file dropped by an installer, and a paste from
    /// the admin screen — and which of them a given customer used is not
    /// something the product should have to know. Every source is read; the
    /// runtime then picks whichever licence is currently best, exactly as it
    /// already does within one directory.
    /// </para>
    /// <para>
    /// Writes go to <see cref="Primary"/> alone, and so does the sidecar. The
    /// alternative — asking every store whether it can be written to — would
    /// have meant a new member on <see cref="ILicenseStore"/> to express
    /// something the composition already knows.
    /// </para>
    /// </summary>
    public sealed class LicenseStoreComposite : ILicenseStore
    {
        #region Fields

        private readonly IReadOnlyList<ILicenseStore> m_sources;

        #endregion

        #region Constructors

        /// <param name="primary">Where installs, uninstalls and the sidecar go.</param>
        /// <param name="others">Additional read-only sources.</param>
        public LicenseStoreComposite(ILicenseStore primary, params ILicenseStore[] others)
        {
            Primary = primary ?? throw new ArgumentNullException(nameof(primary));

            var sources = new List<ILicenseStore> { primary };
            sources.AddRange((others ?? Array.Empty<ILicenseStore>()).Where(store => store != null));

            m_sources = sources;
        }

        #endregion

        #region ILicenseStore

        /// <summary>
        /// Every token from every source, in source order, with duplicates
        /// dropped — the same licence arriving through both a file drop and an
        /// environment variable is one licence, not two, and counting it twice
        /// would make a superseded document look like it was still installed.
        /// </summary>
        public IReadOnlyList<string> ReadTokens()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var tokens = new List<string>();

            foreach (var source in m_sources)
            {
                foreach (var token in ReadFrom(source))
                {
                    if (seen.Add(token))
                        tokens.Add(token);
                }
            }

            return tokens;
        }

        public void Save(string token)
        {
            Primary.Save(token);
        }

        /// <summary>
        /// Removes from the primary store only. A licence supplied by the
        /// environment or by a mounted file survives, and goes on being reported
        /// — which is the honest answer, since the product did not put it there
        /// and cannot take it away.
        /// </summary>
        public bool Remove(string licenseId)
        {
            return Primary.Remove(licenseId);
        }

        public LicenseStoreState ReadState()
        {
            return Primary.ReadState();
        }

        public void WriteState(LicenseStoreState state)
        {
            Primary.WriteState(state);
        }

        #endregion

        #region Tools

        /// <summary>
        /// One unreachable source must not blind the product to the others: a
        /// mount that is not there yet is a normal container start, and the
        /// licence pasted into the admin screen is still perfectly good.
        /// </summary>
        private static IReadOnlyList<string> ReadFrom(ILicenseStore store)
        {
            try
            {
                return store.ReadTokens();
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        #endregion

        #region Properties

        /// <summary>The store that receives installs, uninstalls and the sidecar.</summary>
        public ILicenseStore Primary { get; }

        /// <summary>Every source read, primary first.</summary>
        public IReadOnlyList<ILicenseStore> Sources => m_sources;

        #endregion
    }
}
