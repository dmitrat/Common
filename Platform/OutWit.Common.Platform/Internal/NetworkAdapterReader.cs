using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// Reads network adapters from the BCL and picks the one that best
    /// represents the machine.
    /// <para>
    /// Unlike the rest of the probes this is OS-independent —
    /// <see cref="NetworkInterface"/> works everywhere — so it lives beside
    /// them rather than inside them, and there is one copy of the rules instead
    /// of four.
    /// </para>
    /// </summary>
    internal static class NetworkAdapterReader
    {
        #region Constants

        /// <summary>
        /// Name/description fragments that mark an adapter as virtual. A
        /// hypervisor, container runtime or VPN adds adapters whose addresses
        /// are per-installation rather than per-machine, and one of them
        /// enumerating first would make the factor drift for no physical
        /// reason.
        /// </summary>
        private static readonly string[] VIRTUAL_MARKERS =
        {
            "virtual", "vmware", "virtualbox", "vbox", "hyper-v", "hyperv",
            "docker", "wsl", "veth", "bridge", "tap", "tun", "vpn", "teredo",
            "wan miniport", "pseudo", "loopback", "bluetooth", "npcap",
            "zerotier", "tailscale", "openvpn", "wintun", "wireguard"
        };

        /// <summary>
        /// Adapter types that represent a physical wired connection. Preferred
        /// over wireless because some hosts randomise wireless addresses per
        /// network, which would make the factor drift as the laptop moves.
        /// </summary>
        private static readonly NetworkInterfaceType[] WIRED_TYPES =
        {
            NetworkInterfaceType.Ethernet,
            NetworkInterfaceType.GigabitEthernet,
            NetworkInterfaceType.FastEthernetT,
            NetworkInterfaceType.FastEthernetFx,
            NetworkInterfaceType.Ethernet3Megabit
        };

        private const string EMPTY_ADDRESS = "00:00:00:00:00:00";

        #endregion

        #region Functions

        /// <summary>
        /// Returns the primary adapter's physical address, or <c>null</c> when
        /// no adapter qualifies. Never throws.
        /// </summary>
        public static string? ReadPrimaryMacAddress()
        {
            try
            {
                return SelectPrimary(ReadAdapters())?.PhysicalAddress;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Picks the adapter that best represents the machine, or <c>null</c>
        /// when none qualifies.
        /// <para>
        /// Returning null is deliberate: a wrong-but-present value would be
        /// silently baked into an identity, while an absent factor is something
        /// a consumer can account for.
        /// </para>
        /// </summary>
        public static NetworkAdapterInfo? SelectPrimary(IEnumerable<NetworkAdapterInfo> adapters)
        {
            var candidates = adapters
                .Where(HasUsableAddress)
                .Where(adapter => !IsVirtual(adapter))
                .ToList();

            if (candidates.Count == 0)
                return null;

            var wired = candidates.Where(adapter => WIRED_TYPES.Contains(adapter.Type)).ToList();
            if (wired.Count > 0)
                candidates = wired;

            // Ordered by address rather than by name or enumeration order:
            // adapter names and ordering change between boots and driver
            // updates, the address does not.
            return candidates
                .OrderBy(adapter => adapter.PhysicalAddress, StringComparer.Ordinal)
                .First();
        }

        /// <summary>
        /// Normalises raw address bytes to <c>AA:BB:CC:DD:EE:FF</c>, or empty
        /// when there are none.
        /// </summary>
        public static string FormatAddress(byte[]? addressBytes)
        {
            if (addressBytes == null || addressBytes.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(addressBytes.Length * 3);

            foreach (var value in addressBytes)
            {
                if (builder.Length > 0)
                    builder.Append(':');

                builder.Append(value.ToString("X2"));
            }

            return builder.ToString();
        }

        #endregion

        #region Tools

        /// <summary>
        /// Reads every adapter the OS enumerates.
        /// <para>
        /// <b>Operational status is deliberately ignored.</b> What is wanted is
        /// the machine's hardware, not its connectivity — filtering on
        /// <c>OperationalStatus.Up</c> would change the answer the moment
        /// someone unplugs a cable or disables an adapter, which is exactly the
        /// instability this factor must not have.
        /// </para>
        /// <para>
        /// Windows also enumerates a filter-driver instance per adapter
        /// (<c>…-WFP Native MAC Layer LightWeight Filter-0000</c>), each
        /// repeating its parent's address. They are harmless: duplicates of an
        /// address cannot change which address is chosen.
        /// </para>
        /// </summary>
        private static IReadOnlyList<NetworkAdapterInfo> ReadAdapters()
        {
            var result = new List<NetworkAdapterInfo>();

            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    result.Add(new NetworkAdapterInfo(
                        adapter.Name ?? string.Empty,
                        adapter.Description ?? string.Empty,
                        adapter.NetworkInterfaceType,
                        FormatAddress(adapter.GetPhysicalAddress()?.GetAddressBytes())));
                }
                catch
                {
                    // A single unreadable adapter must not lose the others.
                }
            }

            return result;
        }

        private static bool HasUsableAddress(NetworkAdapterInfo adapter)
        {
            return !string.IsNullOrEmpty(adapter.PhysicalAddress)
                   && !string.Equals(adapter.PhysicalAddress, EMPTY_ADDRESS, StringComparison.Ordinal);
        }

        private static bool IsVirtual(NetworkAdapterInfo adapter)
        {
            if (adapter.Type == NetworkInterfaceType.Loopback || adapter.Type == NetworkInterfaceType.Tunnel)
                return true;

            return VIRTUAL_MARKERS.Any(marker =>
                adapter.Name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0
                || adapter.Description.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        #endregion
    }
}
