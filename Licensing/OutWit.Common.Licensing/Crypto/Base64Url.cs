using System;
using System.Text;

namespace OutWit.Common.Licensing.Crypto
{
    /// <summary>
    /// Unpadded base64url (RFC 4648 §5) — the encoding the licence token is
    /// built from, so a whole licence stays a single line that survives an
    /// e-mail client, a text field, an environment variable and a QR code.
    /// </summary>
    internal static class Base64Url
    {
        #region Functions

        public static string Encode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string EncodeText(string text)
        {
            return Encode(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Decodes to bytes, or returns <c>null</c> when the input is not valid
        /// base64url. Malformed input is an expected condition here — somebody
        /// pasted half a licence — so it is reported, not thrown.
        /// </summary>
        public static byte[]? Decode(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var padded = value!
                .Replace('-', '+')
                .Replace('_', '/');

            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
                case 1: return null;
            }

            try
            {
                return Convert.FromBase64String(padded);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static string? DecodeText(string? value)
        {
            var bytes = Decode(value);

            return bytes == null
                ? null
                : Encoding.UTF8.GetString(bytes);
        }

        #endregion
    }
}
