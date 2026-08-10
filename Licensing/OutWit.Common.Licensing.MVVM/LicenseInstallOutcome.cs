using OutWit.Common.Abstract;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Validation;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.MVVM
{
    /// <summary>
    /// What happened to a pasted licence.
    /// <para>
    /// Distinct from <see cref="LicenseValidationResult"/> because that type
    /// carries a decoded payload and is produced by the runtime — it cannot
    /// cross a wire, and a panel one WitRPC round trip away from the licence
    /// needs an answer that can. The verdict, the sentence and the state that
    /// followed are all a panel ever showed of it.
    /// </para>
    /// </summary>
    public sealed class LicenseInstallOutcome : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseInstallOutcome other)
                return false;

            return Status.Is(other.Status)
                   && Message.Is(other.Message)
                   && IsAccepted.Is(other.IsAccepted);
        }

        public override LicenseInstallOutcome Clone()
        {
            return new LicenseInstallOutcome
            {
                Status = Status,
                Message = Message,
                IsAccepted = IsAccepted,
                Snapshot = Snapshot?.Clone()
            };
        }

        #endregion

        #region Functions

        /// <summary>The outcome of a licence the runtime kept.</summary>
        public static LicenseInstallOutcome Accepted(LicenseStatus status, string message, LicenseSnapshot? snapshot)
        {
            return new LicenseInstallOutcome
            {
                Status = status,
                Message = message,
                IsAccepted = true,
                Snapshot = snapshot
            };
        }

        /// <summary>The outcome of a licence the runtime refused, with the reason it gave.</summary>
        public static LicenseInstallOutcome Rejected(LicenseStatus status, string message)
        {
            return new LicenseInstallOutcome
            {
                Status = status,
                Message = message,
                IsAccepted = false
            };
        }

        public override string ToString()
        {
            return $"{Status}: {Message}";
        }

        #endregion

        #region Properties

        /// <summary>The verdict on the pasted document.</summary>
        public LicenseStatus Status { get; init; } = LicenseStatus.Malformed;

        /// <summary>A sentence the person who pasted it can act on.</summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Whether the licence was kept. True for a valid document and for one
        /// staged ahead of its start date — that is how a renewal is installed
        /// before it is needed — and false for everything else, so a pasted
        /// mistake never displaces a working licence.
        /// </summary>
        public bool IsAccepted { get; init; }

        /// <summary>The state that followed, when the licence was kept.</summary>
        public LicenseSnapshot? Snapshot { get; init; }

        #endregion
    }
}
