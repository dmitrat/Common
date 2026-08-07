using System;
using System.Security.Cryptography;
using System.Text;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Binding
{
    /// <summary>
    /// Turns a raw factor value into the hash a licence records.
    /// </summary>
    public static class FactorHasher
    {
        #region Functions

        /// <summary>
        /// Hashes <paramref name="value"/> to lower-case hex SHA-256, after
        /// normalising it.
        /// <para>
        /// Normalisation is trim + invariant lower-case, and it is not
        /// cosmetic: Windows host names are case-insensitive, and a machine
        /// whose name is re-typed in a different case has not become a different
        /// machine. Without it, that would silently invalidate a licence.
        /// </para>
        /// <para>
        /// The hash is deliberately unsalted. A salt would prevent correlating
        /// the same machine across products, but it would also make a
        /// fingerprint product-specific — so one utility could no longer produce
        /// a code that works for every product — and the threat it defends
        /// against (recovering a host name or MAC from a licence its own owner
        /// holds) is not one that matters here.
        /// </para>
        /// </summary>
        public static string Hash(string? value)
        {
            var normalized = Normalize(value);

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));

            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));

            return builder.ToString();
        }

        /// <summary>Builds a recorded factor from a raw key/value pair.</summary>
        public static LicenseFactor ToFactor(string key, string? value)
        {
            return new LicenseFactor
            {
                Key = key,
                Hash = Hash(value)
            };
        }

        /// <summary>The normalisation applied before hashing. Exposed so callers can reproduce it.</summary>
        public static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        #endregion
    }
}
