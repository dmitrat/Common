namespace OutWit.Common.Platform.Models.MachineIdentity
{
    /// <summary>
    /// Well-known <see cref="MachineFactor.Key"/> values.
    /// <para>
    /// Keys are strings rather than an enum on purpose: consumers combine
    /// machine factors with factors this library knows nothing about (a tenant
    /// slug, an installation id), and the whole set has to travel through one
    /// uniform shape.
    /// </para>
    /// </summary>
    public static class MachineFactorKeys
    {
        #region Constants

        /// <summary>
        /// The OS-level machine identity — Windows registry <c>MachineGuid</c>,
        /// <c>/etc/machine-id</c>, macOS <c>kern.uuid</c>, Android
        /// <c>ro.serialno</c>. The strongest available factor. Survives hardware
        /// changes; changes when the OS is reinstalled.
        /// </summary>
        public const string MACHINE_ID = "machine-id";

        /// <summary>
        /// Physical address of the primary physical network adapter, formatted
        /// <c>AA:BB:CC:DD:EE:FF</c>. Independent of <see cref="MACHINE_ID"/>:
        /// survives an OS reinstall, changes when the adapter is replaced.
        /// <para>
        /// Wireless-only hosts may report an unstable value where the OS
        /// randomises MAC addresses per network; wired adapters are preferred
        /// for exactly that reason, and threshold matching absorbs the rest.
        /// </para>
        /// </summary>
        public const string PRIMARY_MAC = "primary-mac";

        /// <summary>
        /// The host name. The weakest of the three — trivially changed by an
        /// administrator — but free to read and independent of the other two,
        /// so it earns its place as a corroborating signal.
        /// </summary>
        public const string MACHINE_NAME = "machine-name";

        #endregion
    }
}
