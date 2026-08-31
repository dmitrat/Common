using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Settings.Values
{
    /// <summary>
    /// A settings value that is a <b>reference to a secret</b>, never the secret itself.
    /// The settings file carries only the secret-store key, a set flag, and a short
    /// non-secret hint (the tail of the value, for "which key is this" in a UI); the secret
    /// lives in an operating-system secret store behind <c>OutWit.Shared.Secrets</c>.
    /// The store glue — reveal, set, clear — ships in <c>OutWit.Shared.Secrets.Settings</c>,
    /// so this type and its serializer stay free of any store dependency.
    /// </summary>
    [MemoryPackable]
    public sealed partial class SecretValue : ModelBase
    {
        #region Constants

        /// <summary>
        /// A hint is only produced for secrets at least this long, so a short secret's
        /// hint can never be the secret itself.
        /// </summary>
        public const int MIN_LENGTH_FOR_HINT = 8;

        /// <summary>
        /// The hint is the last this-many characters of the secret.
        /// </summary>
        public const int HINT_LENGTH = 4;

        #endregion

        #region Functions

        /// <summary>
        /// Value comparison.
        /// </summary>
        /// <param name="modelBase">The model to compare with.</param>
        /// <param name="tolerance">Unused for this model.</param>
        /// <returns>True when all fields match.</returns>
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SecretValue other)
                return false;

            return StoreKey.Is(other.StoreKey)
                   && IsSet.Is(other.IsSet)
                   && Hint.Is(other.Hint);
        }

        /// <summary>
        /// Creates a copy.
        /// </summary>
        /// <returns>A new <see cref="SecretValue"/> with the same values.</returns>
        public override ModelBase Clone()
        {
            return new SecretValue
            {
                StoreKey = StoreKey,
                IsSet = IsSet,
                Hint = Hint
            };
        }

        /// <summary>
        /// The display hint for a secret: its last <see cref="HINT_LENGTH"/> characters when
        /// it is at least <see cref="MIN_LENGTH_FOR_HINT"/> long, otherwise empty. The hint
        /// is not a secret — it exists so a UI can say "wit_sk_••••SzCo" and an engineer can
        /// tell which key is set without ever seeing it.
        /// </summary>
        /// <param name="secret">The secret text.</param>
        /// <returns>The hint, possibly empty.</returns>
        public static string MakeHint(string secret)
        {
            if (string.IsNullOrEmpty(secret) || secret.Length < MIN_LENGTH_FOR_HINT)
                return "";

            return secret.Substring(secret.Length - HINT_LENGTH);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The key the secret lives under in the secret store, e.g. "WitSweep/ApiKey".
        /// </summary>
        [ToString]
        public string StoreKey { get; set; } = "";

        /// <summary>
        /// Whether a secret has been stored. A display state, not a guarantee — the store is
        /// the truth, and this self-heals on the next set or reveal.
        /// </summary>
        [ToString]
        public bool IsSet { get; set; }

        /// <summary>
        /// The non-secret display hint — see <see cref="MakeHint"/>. Never the secret.
        /// </summary>
        [ToString]
        public string Hint { get; set; } = "";

        #endregion
    }
}
