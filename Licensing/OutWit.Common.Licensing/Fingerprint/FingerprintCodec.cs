using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Fingerprint
{
    /// <summary>
    /// Renders a host's binding factors as a short code a person can read over
    /// the phone, and checks one that was typed back.
    /// <para>
    /// A SHA-256 hex digest is 64 characters — unusable on a support call and
    /// error-prone in an e-mail. This produces sixteen characters in four
    /// groups, in <b>Crockford Base32</b>: the alphabet omits <c>I</c>,
    /// <c>L</c>, <c>O</c> and <c>U</c>, and decoding folds the confusable pairs
    /// (<c>0</c>/<c>O</c>, <c>1</c>/<c>I</c>/<c>L</c>) together, so the most
    /// common transcription slips correct themselves. The final character is a
    /// check symbol, which catches the rest <b>before</b> a wrong fingerprint
    /// becomes a wrong licence and a second support cycle.
    /// </para>
    /// </summary>
    public static class FingerprintCodec
    {
        #region Constants

        private const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        /// <summary>Crockford's check alphabet — the base 32 symbols plus five more, giving modulo 37.</summary>
        private const string CHECK_ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ*~$=U";

        private const int IDENTITY_BYTES = 9;   // 72 bits — collision-free far past any realistic customer base
        private const int DATA_CHARS = 15;      // 15 × 5 bits covers 72 with room to spare
        private const int GROUP_SIZE = 4;

        #endregion

        #region Functions

        /// <summary>
        /// Builds the display code for a set of host factors, e.g.
        /// <c>WSW-K3M9-7TQZ-B2XF-R8VN</c>.
        /// </summary>
        /// <param name="prefix">Short product marker, e.g. <c>WSW</c>. Optional.</param>
        /// <param name="factors">The host's hashed factors.</param>
        public static string Encode(string? prefix, IReadOnlyList<LicenseFactor>? factors)
        {
            var body = EncodeIdentity(ComputeIdentity(factors));

            return string.IsNullOrWhiteSpace(prefix)
                ? body
                : $"{prefix!.Trim().ToUpperInvariant()}-{body}";
        }

        /// <summary>
        /// True when <paramref name="code"/> is well-formed and its check symbol
        /// agrees — so a mistyped code is caught at the point of entry rather
        /// than by issuing a licence nobody can use.
        /// </summary>
        public static bool IsWellFormed(string? code)
        {
            var normalized = NormalizeBody(code);

            if (normalized == null || normalized.Length != DATA_CHARS + 1)
                return false;

            var data = normalized.Substring(0, DATA_CHARS);
            var check = normalized[DATA_CHARS];

            return DecodeValue(data) is { } value && CheckSymbol(value) == check;
        }

        /// <summary>
        /// Strips formatting and folds Crockford's confusable characters, so two
        /// spellings of the same code compare equal. Returns <c>null</c> when the
        /// input contains something that is not a Crockford symbol at all.
        /// </summary>
        public static string? NormalizeBody(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var builder = new StringBuilder();

            foreach (var raw in code!.ToUpperInvariant())
            {
                if (raw is '-' or ' ')
                    continue;

                var symbol = raw switch
                {
                    'O' => '0',
                    'I' => '1',
                    'L' => '1',
                    _ => raw
                };

                // A product prefix is not part of the code; anything else that is
                // not a symbol makes the whole input unusable.
                if (CHECK_ALPHABET.IndexOf(symbol) < 0)
                    return null;

                builder.Append(symbol);
            }

            var normalized = builder.ToString();

            // Drop a leading product prefix if one was included.
            if (normalized.Length > DATA_CHARS + 1)
                normalized = normalized.Substring(normalized.Length - (DATA_CHARS + 1));

            return normalized;
        }

        #endregion

        #region Tools

        /// <summary>
        /// Reduces the factor set to a stable 72-bit identity.
        /// <para>
        /// Factors are sorted before hashing, so the code does not depend on the
        /// order a provider happened to return them in — a fingerprint that
        /// changed between two runs on the same machine would be worse than
        /// useless.
        /// </para>
        /// </summary>
        private static byte[] ComputeIdentity(IReadOnlyList<LicenseFactor>? factors)
        {
            var canonical = factors == null || factors.Count == 0
                ? string.Empty
                : string.Join("\n", factors
                    .Select(factor => $"{factor.Key}={factor.Hash}")
                    .OrderBy(entry => entry, StringComparer.Ordinal));

            using var sha = SHA256.Create();
            var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));

            return digest.Take(IDENTITY_BYTES).ToArray();
        }

        private static string EncodeIdentity(byte[] identity)
        {
            // Reversed by hand rather than with Reverse(): on some target
            // frameworks that binds to MemoryExtensions.Reverse, which sorts the
            // array in place and returns void. The trailing zero byte keeps the
            // BigInteger positive, since it reads little-endian two's complement.
            var littleEndian = new byte[identity.Length + 1];
            for (var index = 0; index < identity.Length; index++)
                littleEndian[index] = identity[identity.Length - 1 - index];

            var value = new BigInteger(littleEndian);

            var chars = new char[DATA_CHARS];
            var remaining = value;

            for (var index = DATA_CHARS - 1; index >= 0; index--)
            {
                chars[index] = ALPHABET[(int)(remaining % 32)];
                remaining /= 32;
            }

            var body = new string(chars) + CheckSymbol(value);

            return string.Join("-", Enumerable
                .Range(0, body.Length / GROUP_SIZE)
                .Select(group => body.Substring(group * GROUP_SIZE, GROUP_SIZE)));
        }

        private static BigInteger? DecodeValue(string data)
        {
            BigInteger value = 0;

            foreach (var symbol in data)
            {
                var digit = ALPHABET.IndexOf(symbol);
                if (digit < 0)
                    return null;

                value = value * 32 + digit;
            }

            return value;
        }

        private static char CheckSymbol(BigInteger value)
        {
            return CHECK_ALPHABET[(int)(value % 37)];
        }

        #endregion
    }
}
