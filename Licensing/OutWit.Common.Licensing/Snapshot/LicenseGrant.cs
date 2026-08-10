using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Snapshot
{
    /// <summary>
    /// One line of "what this licence actually gives you": a declared feature
    /// with a yes or no, or a declared limit with the number in force.
    /// <para>
    /// Built from the product's <b>declared vocabulary</b> rather than from the
    /// licence, so a capability the customer paid for and did not get shows up
    /// as a "no" beside its own description instead of being absent from the
    /// screen entirely.
    /// </para>
    /// </summary>
    public sealed class LicenseGrant : ModelBase
    {
        #region Constants

        /// <summary>What an unset limit reads as. An absent limit is unlimited.</summary>
        public const string UNLIMITED = "unlimited";

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseGrant other)
                return false;

            return Key.Is(other.Key)
                   && Description.Is(other.Description)
                   && Kind.Is(other.Kind)
                   && IsGranted.Is(other.IsGranted)
                   && Value.Is(other.Value);
        }

        public override LicenseGrant Clone()
        {
            return new LicenseGrant
            {
                Key = Key,
                Description = Description,
                Kind = Kind,
                IsGranted = IsGranted,
                Value = Value
            };
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return Kind == LicenseGrantKind.Feature
                ? $"{Key}: {(IsGranted ? "yes" : "no")}"
                : $"{Key} = {DisplayValue}";
        }

        #endregion

        #region Properties

        /// <summary>The declared key, exactly as the product spells it.</summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>What the product said this key means, for a panel to show beside it.</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>Whether this is a capability or a cap.</summary>
        public LicenseGrantKind Kind { get; init; } = LicenseGrantKind.Feature;

        /// <summary>For a feature, whether it is granted. Always true for a limit.</summary>
        public bool IsGranted { get; init; }

        /// <summary>For a limit, the cap in force. <see cref="long.MaxValue"/> means unlimited.</summary>
        public long Value { get; init; }

        /// <summary>True when a limit is uncapped.</summary>
        public bool IsUnlimited => Kind == LicenseGrantKind.Limit && Value == long.MaxValue;

        /// <summary>The value a panel prints, with the unlimited case spelled out.</summary>
        public string DisplayValue => Kind == LicenseGrantKind.Feature
            ? (IsGranted ? "yes" : "no")
            : (IsUnlimited ? UNLIMITED : Value.ToString());

        #endregion
    }

    /// <summary>Which of the two kinds of grant a <see cref="LicenseGrant"/> carries.</summary>
    public enum LicenseGrantKind
    {
        /// <summary>A capability, granted or not.</summary>
        Feature = 0,

        /// <summary>A numeric cap.</summary>
        Limit = 1
    }
}
