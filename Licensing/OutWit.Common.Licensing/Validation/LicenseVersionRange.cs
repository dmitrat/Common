using System;
using System.Collections.Generic;
using System.Linq;

namespace OutWit.Common.Licensing.Validation
{
    /// <summary>
    /// The <c>appVer</c> range: which product versions a licence covers.
    /// <para>
    /// Syntax is a space-separated list of clauses, all of which must hold —
    /// <c>"&gt;=1.5.0 &lt;2.0.0"</c>. An empty range covers every version.
    /// </para>
    /// <para>
    /// This is the safety valve that makes an unlimited term reasonable:
    /// perpetual for the version that was bought, a new document for a major
    /// upgrade. Without it, "unlimited" would mean "every future version, free,
    /// forever".
    /// </para>
    /// </summary>
    public sealed class LicenseVersionRange
    {
        #region Fields

        private readonly IReadOnlyList<Clause> m_clauses;

        #endregion

        #region Constructors

        private LicenseVersionRange(IReadOnlyList<Clause> clauses)
        {
            m_clauses = clauses;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Parses a range. Unparseable input yields a range that matches
        /// everything: a licence must never be refused because of a typo the
        /// customer cannot see or fix — that is an issuing error, and it belongs
        /// in the issuing tool's validation, not in a dead product.
        /// </summary>
        public static LicenseVersionRange Parse(string? range)
        {
            if (string.IsNullOrWhiteSpace(range))
                return new LicenseVersionRange(Array.Empty<Clause>());

            var clauses = new List<Clause>();

            foreach (var token in range!.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var clause = Clause.Parse(token);
                if (clause != null)
                    clauses.Add(clause);
            }

            return new LicenseVersionRange(clauses);
        }

        /// <summary>
        /// True when <paramref name="version"/> satisfies every clause. A null
        /// version matches an empty range only — a product that does not know
        /// its own version cannot claim to be inside a bounded one.
        /// </summary>
        public bool Matches(Version? version)
        {
            if (m_clauses.Count == 0)
                return true;

            if (version == null)
                return false;

            var normalized = Normalize(version);

            return m_clauses.All(clause => clause.Matches(normalized));
        }

        /// <summary>True when the range imposes nothing.</summary>
        public bool IsUnbounded => m_clauses.Count == 0;

        #endregion

        #region Tools

        /// <summary>
        /// Pads a version to four components.
        /// <para>
        /// <see cref="Version"/> leaves unspecified components at -1, so
        /// <c>1.5</c> compares as <i>less than</i> <c>1.5.0</c>. Left alone,
        /// that would make a product versioned <c>1.5</c> fail a
        /// <c>"&gt;=1.5.0"</c> licence — an invisible, entirely wrong refusal.
        /// </para>
        /// </summary>
        private static Version Normalize(Version version)
        {
            return new Version(
                Math.Max(0, version.Major),
                Math.Max(0, version.Minor),
                Math.Max(0, version.Build),
                Math.Max(0, version.Revision));
        }

        #endregion

        #region Clause

        private sealed class Clause
        {
            private readonly string m_operator;
            private readonly Version m_version;

            private Clause(string op, Version version)
            {
                m_operator = op;
                m_version = version;
            }

            public static Clause? Parse(string token)
            {
                foreach (var op in new[] { ">=", "<=", "!=", ">", "<", "=" })
                {
                    if (!token.StartsWith(op, StringComparison.Ordinal))
                        continue;

                    return Version.TryParse(token.Substring(op.Length), out var parsed)
                        ? new Clause(op, Normalize(parsed))
                        : null;
                }

                return Version.TryParse(token, out var exact)
                    ? new Clause("=", Normalize(exact))
                    : null;
            }

            public bool Matches(Version version)
            {
                var comparison = version.CompareTo(m_version);

                return m_operator switch
                {
                    ">=" => comparison >= 0,
                    "<=" => comparison <= 0,
                    ">" => comparison > 0,
                    "<" => comparison < 0,
                    "!=" => comparison != 0,
                    _ => comparison == 0
                };
            }
        }

        #endregion
    }
}
