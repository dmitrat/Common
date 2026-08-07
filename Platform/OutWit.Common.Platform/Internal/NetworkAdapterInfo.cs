using System.Net.NetworkInformation;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// The subset of a network adapter's description that primary-adapter
    /// selection needs.
    /// <para>
    /// It exists so the selection rules can be a pure function over plain data:
    /// <see cref="NetworkInterface"/> cannot be constructed in a test, so
    /// reading and choosing are split — <see cref="NetworkAdapterReader"/> does
    /// the reading, and the rules stay verifiable against fabricated input.
    /// </para>
    /// </summary>
    internal sealed class NetworkAdapterInfo
    {
        #region Constructors

        public NetworkAdapterInfo(string name, string description, NetworkInterfaceType type, string physicalAddress)
        {
            Name = name;
            Description = description;
            Type = type;
            PhysicalAddress = physicalAddress;
        }

        #endregion

        #region Properties

        /// <summary>Adapter name as the OS reports it.</summary>
        public string Name { get; }

        /// <summary>Adapter description — usually the driver/product string.</summary>
        public string Description { get; }

        /// <summary>Adapter type as classified by the OS.</summary>
        public NetworkInterfaceType Type { get; }

        /// <summary>
        /// Physical address, normalised to <c>AA:BB:CC:DD:EE:FF</c>, or empty
        /// when the adapter has none.
        /// </summary>
        public string PhysicalAddress { get; }

        #endregion
    }
}
