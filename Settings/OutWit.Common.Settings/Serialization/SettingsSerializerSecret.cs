using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Serialization
{
    /// <summary>
    /// The serializer for <see cref="SecretValue"/> — a pure reference parser. The stored
    /// representation is "{StoreKey}|{0 or 1}|{Hint}", and the secret itself never passes
    /// through this class, the settings file, or the settings pipeline at all: it lives in
    /// an operating-system secret store, reached through the extensions in
    /// <c>OutWit.Shared.Secrets.Settings</c>. Contrast with
    /// <see cref="SettingsSerializerPassword"/>, which is a display hint over a plaintext
    /// string.
    /// </summary>
    public sealed class SettingsSerializerSecret : SettingsSerializerBase<SecretValue>
    {
        #region Constants

        private const char SEPARATOR = '|';

        private const string SET_FLAG = "1";

        #endregion

        #region Functions

        /// <summary>
        /// Parses a stored reference. Tolerant: a bare store key — the natural form for a
        /// default-settings template — reads as "not set, no hint".
        /// </summary>
        /// <param name="value">The stored string, e.g. "WitSweep/ApiKey|1|SzCo".</param>
        /// <param name="tag">Unused.</param>
        /// <returns>The parsed reference.</returns>
        public override SecretValue Parse(string value, string tag)
        {
            string[] parts = (value ?? "").Split(SEPARATOR);

            return new SecretValue
            {
                StoreKey = parts.Length > 0 ? parts[0] : "",
                IsSet = parts.Length > 1 && parts[1] == SET_FLAG,
                Hint = parts.Length > 2 ? parts[2] : ""
            };
        }

        /// <summary>
        /// Formats a reference for storage.
        /// </summary>
        /// <param name="value">The reference.</param>
        /// <returns>"{StoreKey}|{0 or 1}|{Hint}".</returns>
        public override string Format(SecretValue value)
        {
            return $"{value.StoreKey}{SEPARATOR}{(value.IsSet ? SET_FLAG : "0")}{SEPARATOR}{value.Hint}";
        }

        /// <summary>
        /// Value comparison via <see cref="SecretValue.Is"/>.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>True when equal.</returns>
        public override bool AreEqual(SecretValue a, SecretValue b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a == null || b == null)
                return false;

            return a.Is(b);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override string ValueKind => "Secret";

        #endregion
    }
}
