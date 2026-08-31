namespace OutWit.Common.Settings.Serialization
{
    /// <summary>
    /// A display hint, not protection.
    /// </summary>
    /// <remarks>
    /// This serializer is <see cref="SettingsSerializerString"/> with a different
    /// <see cref="ValueKind"/> label: it tells a settings UI to render the field with dots.
    /// It does not encrypt, it does not restrict who may read the file — the value sits in
    /// the settings store in plaintext, and therefore in every backup, support bundle and
    /// version-control checkout that ever sees it. Anything genuinely secret — a token, a
    /// key, a passphrase — belongs in <c>OutWit.Shared.Secrets</c>: declare the setting with
    /// <see cref="SettingsSerializerSecret"/> (ValueKind "Secret"), whose file footprint is
    /// a store reference and a non-secret hint, never the value. A secret that has already
    /// lived in a settings file should be treated as disclosed: migrate it to the secret
    /// store, then rotate it.
    /// </remarks>
    public sealed class SettingsSerializerPassword : SettingsSerializerString
    {
        #region Properties

        public override string ValueKind => "Password";

        #endregion
    }
}
