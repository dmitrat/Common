using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Platform.Models.MachineIdentity
{
    /// <summary>
    /// One named, independently-observable property of the current machine.
    /// <para>
    /// Where <see cref="Interfaces.IMachineIdentityProvider"/> collapses the
    /// machine into a single hash, factors keep the individual observations
    /// apart so a consumer can tolerate partial change: hardware drifts one
    /// component at a time, and an all-or-nothing identity turns a replaced
    /// network card into a support ticket.
    /// </para>
    /// <para>
    /// Values are returned <b>raw</b>, not hashed — hashing, storage and any
    /// matching policy belong to the consumer, and a diagnostics screen wants
    /// to show a human the actual value.
    /// </para>
    /// </summary>
    public sealed class MachineFactor : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not MachineFactor other)
                return false;

            return Key.Is(other.Key)
                   && Value.Is(other.Value);
        }

        public override MachineFactor Clone()
        {
            return new MachineFactor
            {
                Key = Key,
                Value = Value
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Stable identifier of what was observed — one of the
        /// <see cref="MachineFactorKeys"/> constants.
        /// </summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>
        /// The observed value, raw and unhashed. Never null; a factor that
        /// could not be read is omitted from the collection instead.
        /// </summary>
        public string Value { get; init; } = string.Empty;

        #endregion
    }
}
