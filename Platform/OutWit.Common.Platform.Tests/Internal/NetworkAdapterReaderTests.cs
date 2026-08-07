using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using OutWit.Common.Platform.Internal;

namespace OutWit.Common.Platform.Tests.Internal
{
    /// <summary>
    /// Primary-adapter selection decides how stable the <c>primary-mac</c>
    /// factor is, so the rules are exercised against fabricated adapter sets
    /// rather than whatever the test host happens to have installed.
    /// </summary>
    [TestFixture]
    public sealed class NetworkAdapterReaderTests
    {
        #region Address Formatting Tests

        [Test]
        public void FormatAddressProducesColonSeparatedUpperHexTest()
        {
            var formatted = NetworkAdapterReader.FormatAddress(new byte[] { 0xAA, 0x0B, 0xCC, 0xDD, 0xEE, 0x0F });

            Assert.That(formatted, Is.EqualTo("AA:0B:CC:DD:EE:0F"));
        }

        [Test]
        public void FormatAddressReturnsEmptyForMissingBytesTest()
        {
            Assert.Multiple(() =>
            {
                Assert.That(NetworkAdapterReader.FormatAddress(null), Is.Empty);
                Assert.That(NetworkAdapterReader.FormatAddress(Array.Empty<byte>()), Is.Empty);
            });
        }

        #endregion

        #region Selection Tests

        [Test]
        public void SelectPrimaryPicksTheOnlyPhysicalAdapterTest()
        {
            var selected = NetworkAdapterReader.SelectPrimary(new[]
            {
                Adapter("eth0", "Intel I219-V", NetworkInterfaceType.Ethernet, "AA:BB:CC:DD:EE:01")
            });

            Assert.That(selected?.PhysicalAddress, Is.EqualTo("AA:BB:CC:DD:EE:01"));
        }

        [Test]
        public void SelectPrimarySkipsVirtualAdaptersByDescriptionTest()
        {
            var selected = NetworkAdapterReader.SelectPrimary(new[]
            {
                Adapter("Ethernet 2", "Hyper-V Virtual Ethernet Adapter", NetworkInterfaceType.Ethernet, "00:15:5D:00:00:01"),
                Adapter("Ethernet 3", "VMware Virtual Ethernet Adapter for VMnet8", NetworkInterfaceType.Ethernet, "00:50:56:C0:00:08"),
                Adapter("Ethernet", "Realtek PCIe GbE Family Controller", NetworkInterfaceType.Ethernet, "FF:EE:DD:CC:BB:AA")
            });

            Assert.That(selected?.PhysicalAddress, Is.EqualTo("FF:EE:DD:CC:BB:AA"),
                "A hypervisor adapter must never win over the physical one, however it enumerates.");
        }

        [Test]
        public void SelectPrimarySkipsVirtualAdaptersByNameTest()
        {
            var selected = NetworkAdapterReader.SelectPrimary(new[]
            {
                Adapter("docker0", "", NetworkInterfaceType.Ethernet, "02:42:AC:11:00:01"),
                Adapter("enp3s0", "", NetworkInterfaceType.Ethernet, "B4:2E:99:11:22:33")
            });

            Assert.That(selected?.PhysicalAddress, Is.EqualTo("B4:2E:99:11:22:33"));
        }

        [Test]
        public void SelectPrimarySkipsLoopbackAndTunnelTypesTest()
        {
            var selected = NetworkAdapterReader.SelectPrimary(new[]
            {
                Adapter("lo", "Software Loopback", NetworkInterfaceType.Loopback, "AA:AA:AA:AA:AA:AA"),
                Adapter("teredo", "Microsoft Teredo", NetworkInterfaceType.Tunnel, "BB:BB:BB:BB:BB:BB"),
                Adapter("eth0", "Intel I219-V", NetworkInterfaceType.Ethernet, "CC:CC:CC:CC:CC:CC")
            });

            Assert.That(selected?.PhysicalAddress, Is.EqualTo("CC:CC:CC:CC:CC:CC"));
        }

        [Test]
        public void SelectPrimarySkipsAdaptersWithoutUsableAddressTest()
        {
            var selected = NetworkAdapterReader.SelectPrimary(new[]
            {
                Adapter("ppp0", "WAN Adapter", NetworkInterfaceType.Ppp, ""),
                Adapter("eth1", "Intel I219-V", NetworkInterfaceType.Ethernet, "00:00:00:00:00:00"),
                Adapter("eth0", "Intel I219-V", NetworkInterfaceType.Ethernet, "11:22:33:44:55:66")
            });

            Assert.That(selected?.PhysicalAddress, Is.EqualTo("11:22:33:44:55:66"),
                "An all-zero address is not an identity.");
        }

        [Test]
        public void SelectPrimaryPrefersWiredOverWirelessTest()
        {
            var selected = NetworkAdapterReader.SelectPrimary(new[]
            {
                Adapter("wlan0", "Intel Wi-Fi 6 AX201", NetworkInterfaceType.Wireless80211, "00:11:22:33:44:55"),
                Adapter("eth0", "Intel I219-V", NetworkInterfaceType.Ethernet, "FF:FF:FF:FF:FF:FF")
            });

            Assert.That(selected?.PhysicalAddress, Is.EqualTo("FF:FF:FF:FF:FF:FF"),
                "Wired wins even when its address sorts last — wireless addresses may be randomised per network.");
        }

        [Test]
        public void SelectPrimaryFallsBackToWirelessWhenNoWiredAdapterExistsTest()
        {
            var selected = NetworkAdapterReader.SelectPrimary(new[]
            {
                Adapter("wlan0", "Intel Wi-Fi 6 AX201", NetworkInterfaceType.Wireless80211, "00:11:22:33:44:55")
            });

            Assert.That(selected?.PhysicalAddress, Is.EqualTo("00:11:22:33:44:55"));
        }

        [Test]
        public void SelectPrimaryIsIndependentOfEnumerationOrderTest()
        {
            var first = Adapter("eth0", "Intel I219-V", NetworkInterfaceType.Ethernet, "11:11:11:11:11:11");
            var second = Adapter("eth1", "Intel I210", NetworkInterfaceType.Ethernet, "22:22:22:22:22:22");

            var forward = NetworkAdapterReader.SelectPrimary(new[] { first, second });
            var reversed = NetworkAdapterReader.SelectPrimary(new[] { second, first });

            Assert.That(forward?.PhysicalAddress, Is.EqualTo(reversed?.PhysicalAddress),
                "Adapter enumeration order changes between boots; the chosen factor must not.");
        }

        [Test]
        public void SelectPrimaryReturnsNullWhenNothingQualifiesTest()
        {
            var selected = NetworkAdapterReader.SelectPrimary(new[]
            {
                Adapter("lo", "Software Loopback", NetworkInterfaceType.Loopback, "AA:AA:AA:AA:AA:AA"),
                Adapter("docker0", "", NetworkInterfaceType.Ethernet, "02:42:AC:11:00:01")
            });

            Assert.That(selected, Is.Null,
                "An absent factor is safer than a wrong one baked into an identity.");
        }

        [Test]
        public void SelectPrimaryReturnsNullForEmptyInputTest()
        {
            Assert.That(NetworkAdapterReader.SelectPrimary(new List<NetworkAdapterInfo>()), Is.Null);
        }

        #endregion

        #region Tools

        private static NetworkAdapterInfo Adapter(string name, string description, NetworkInterfaceType type, string address)
        {
            return new NetworkAdapterInfo(name, description, type, address);
        }

        #endregion
    }
}
