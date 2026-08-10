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
        #region Constants

        private static readonly string[] OPERATORS = { ">=", "<=", "!=", ">", "<", "=" };

        #endregion

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
            return Parse(range, out _);
        }

        /// <summary>
        /// Parses a range and reports the clauses it could not use.
        /// <para>
        /// The verifier fails open on purpose, so <b>the issuing form is the
        /// only place a typo can be caught</b>. Catching it there means knowing
        /// exactly what the verifier will silently drop, which is why this
        /// reports it rather than leaving the issuing side to re-implement the
        /// same parser and drift away from it.
        /// </para>
        /// </summary>
        /// <param name="range">The range as typed.</param>
        /// <param name="rejected">Tokens that carry no meaning and were dropped.</param>
        public static LicenseVersionRange Parse(string? range, out IReadOnlyList<string> rejected)
        {
            var dropped = new List<string>();

            if (string.IsNullOrWhiteSpace(range))
            {
                rejected = dropped;
                return new LicenseVersionRange(Array.Empty<Clause>());
            }

            var clauses = new List<Clause>();

            foreach (var token in range!.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var clause = Clause.Parse(token);

                if (clause == null)
                    dropped.Add(token);
                else
                    clauses.Add(clause);
            }

            rejected = dropped;

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

        /// <summary>
        /// What this range covers, in words, for an operator to read before
        /// signing. A range is a compact expression with an expensive failure
        /// mode; the sentence is what makes <c>"&gt;=1.5.0 &lt;2.x"</c> —
        /// which quietly parses as <c>"&gt;=1.5.0"</c> — visible as the
        /// open-ended grant it actually is.
        /// </summary>
        public string Describe()
        {
            if (m_clauses.Count == 0)
                return "Every version, including major versions that do not exist yet.";

            return string.Join(", ", m_clauses.Select(clause => clause.Describe())) + ".";
        }

        public override string ToString()
        {
            return m_clauses.Count == 0
                ? "(any version)"
                : string.Join(" ", m_clauses.Select(clause => clause.Text));
        }

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

        #region Properties

        /// <summary>True when the range imposes nothing.</summary>
        public bool IsUnbounded => m_clauses.Count == 0;

        /// <summary>
        /// True when some clause caps the version from above.
        /// <para>
        /// Distinct from <see cref="IsUnbounded"/>, and the distinction is the
        /// whole point: <c>"&gt;=1.5.0"</c> has a clause, so it is not
        /// unbounded, and it still covers every major version ever to be
        /// written. Paired with an unlimited term that is the one grant in this
        /// system that cannot be taken back in either dimension.
        /// </para>
        /// </summary>
        public bool HasUpperBound => m_clauses.Any(clause => clause.IsUpperBound);

        /// <summary>The clauses that were understood, as they were written.</summary>
        public IReadOnlyList<string> Clauses => m_clauses.Select(clause => clause.Text).ToList();

        #endregion

        #region Clause

        private sealed class Clause
        {
            private readonly string m_operator;
            private readonly Version m_version;
            private readonly string m_versionText;

            private Clause(string op, Version version, string versionText, string text)
            {
                m_operator = op;
                m_version = version;
                m_versionText = versionText;
                Text = text;
            }

            public static Clause? Parse(string token)
            {
                foreach (var op in OPERATORS)
                {
                    if (!token.StartsWith(op, StringComparison.Ordinal))
                        continue;

                    var text = token.Substring(op.Length);

                    return Version.TryParse(text, out var parsed)
                        ? new Clause(op, Normalize(parsed), text, token)
                        : null;
                }

                return Version.TryParse(token, out var exact)
                    ? new Clause("=", Normalize(exact), token, token)
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

            /// <summary>
            /// Described from the text as typed rather than from the padded
            /// value, so a range written <c>"&lt;2.0.0"</c> does not read back
            /// as <c>"below 2.0.0.0"</c> and leave an operator wondering what
            /// the tool did to it.
            /// </summary>
            public string Describe()
            {
                return m_operator switch
                {
                    ">=" => $"{m_versionText} or later",
                    "<=" => $"{m_versionText} or earlier",
                    ">" => $"after {m_versionText}",
                    "<" => $"below {m_versionText}",
                    "!=" => $"except {m_versionText}",
                    _ => $"exactly {m_versionText}"
                };
            }

            /// <summary>
            /// Whether this clause stops the range going up forever. An exact
            /// match counts: it is the tightest ceiling there is.
            /// </summary>
            public bool IsUpperBound => m_operator is "<" or "<=" or "=";

            /// <summary>The clause exactly as it was written.</summary>
            public string Text { get; }
        }

        #endregion
    }
}
