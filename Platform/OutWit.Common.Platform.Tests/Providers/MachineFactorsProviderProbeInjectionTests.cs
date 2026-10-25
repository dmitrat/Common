using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Platform;
using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.MachineIdentity;
using OutWit.Common.Platform.Models.SystemInfo;
using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Platform.Tests.Providers
{
    /// <summary>
    /// MachineFactorsProvider must (a) surface the probe's machine identity as a
    /// factor, (b) omit factors it cannot read rather than emitting them blank,
    /// and (c) survive a probe that throws.
    /// </summary>
    [TestFixture]
    public sealed class MachineFactorsProviderProbeInjectionTests
    {
        #region Machine Id Tests

        [Test]
        public async Task ProbeMachineIdentityIsReturnedRawTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "raw-stable-identity" });

            var factors = await provider.CollectAsync();

            var machineId = factors.FirstOrDefault(factor => factor.Key == MachineFactorKeys.MACHINE_ID);

            Assert.Multiple(() =>
            {
                Assert.That(machineId, Is.Not.Null);
                Assert.That(machineId!.Value, Is.EqualTo("raw-stable-identity"),
                    "Hashing belongs to the consumer, not to this provider.");
            });
        }

        [Test]
        public async Task MissingMachineIdentityIsOmittedNotBlankTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = null });

            var factors = await provider.CollectAsync();

            Assert.That(factors.Any(factor => factor.Key == MachineFactorKeys.MACHINE_ID), Is.False,
                "Two machines that both failed to read an identity must not look alike because of it.");
        }

        [Test]
        public async Task WhitespaceMachineIdentityIsOmittedTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "   " });

            var factors = await provider.CollectAsync();

            Assert.That(factors.Any(factor => factor.Key == MachineFactorKeys.MACHINE_ID), Is.False);
        }

        [Test]
        public async Task MachineIdentityIsTrimmedTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "  padded-id\n" });

            var factors = await provider.CollectAsync();

            Assert.That(factors.Single(factor => factor.Key == MachineFactorKeys.MACHINE_ID).Value,
                Is.EqualTo("padded-id"));
        }

        [Test]
        public async Task TheFactorIsTheHardwareIdentityWhereItDiffersTest()
        {
            // macOS: the OS identity names the kernel build, the factor binds
            // to the firmware's platform UUID instead.
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "kernel-build", HardwareIdentity = "platform-uuid" });

            var factors = await provider.CollectAsync();

            Assert.That(factors.Single(factor => factor.Key == MachineFactorKeys.MACHINE_ID).Value, Is.EqualTo("platform-uuid"));
        }

        [Test]
        public async Task OnAMacTheFactorIsNotTheKernelBuildTest()
        {
            Assume.That(OperatingSystem.IsMacOS(), Is.True, "the real macOS probe");
            var kernel = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sysctl", "-n kern.uuid")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            })!;
            var kernelUuid = (await kernel.StandardOutput.ReadToEndAsync()).Trim();

            var factor = (await new MachineFactorsProvider().CollectAsync()).Single(item => item.Key == MachineFactorKeys.MACHINE_ID);

            Assert.Multiple(() =>
            {
                Assert.That(Guid.TryParse(factor.Value, out _), Is.True, $"a platform UUID, read {factor.Value}");
                Assert.That(factor.Value, Is.Not.EqualTo(kernelUuid).IgnoreCase, "kern.uuid is the kernel build's UUID");
            });
        }

        [Test]
        public async Task ThrowingProbeDoesNotLoseOtherFactorsTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { Throws = true });

            var factors = await provider.CollectAsync();

            Assert.Multiple(() =>
            {
                Assert.That(factors.Any(factor => factor.Key == MachineFactorKeys.MACHINE_ID), Is.False);
                Assert.That(factors.Any(factor => factor.Key == MachineFactorKeys.MACHINE_NAME), Is.True,
                    "One unreadable factor must not take the rest of the collection with it.");
            });
        }

        #endregion

        #region Collection Tests

        [Test]
        public async Task MachineNameIsAlwaysCollectedTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "id" });

            var factors = await provider.CollectAsync();

            var name = factors.FirstOrDefault(factor => factor.Key == MachineFactorKeys.MACHINE_NAME);

            Assert.Multiple(() =>
            {
                Assert.That(name, Is.Not.Null);
                Assert.That(name!.Value, Is.EqualTo(Environment.MachineName));
            });
        }

        [Test]
        public async Task NoFactorIsEverReturnedBlankTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "id" });

            var factors = await provider.CollectAsync();

            Assert.That(factors.All(factor => !string.IsNullOrWhiteSpace(factor.Value)), Is.True);
        }

        [Test]
        public async Task KeysAreUniqueTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "id" });

            var factors = await provider.CollectAsync();

            Assert.That(factors.Select(factor => factor.Key).Distinct().Count(), Is.EqualTo(factors.Count));
        }

        [Test]
        public async Task OnlyWellKnownKeysAreReturnedTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "id" });

            var known = new[]
            {
                MachineFactorKeys.MACHINE_ID,
                MachineFactorKeys.PRIMARY_MAC,
                MachineFactorKeys.MACHINE_NAME
            };

            var factors = await provider.CollectAsync();

            Assert.That(factors.All(factor => known.Contains(factor.Key)), Is.True);
        }

        [Test]
        public async Task RepeatedCollectionIsStableTest()
        {
            var provider = new MachineFactorsProvider(new FakeProbe { RawIdentity = "id" });

            var first = await provider.CollectAsync();
            var second = await provider.CollectAsync();

            Assert.That(first.Select(factor => $"{factor.Key}={factor.Value}"),
                Is.EqualTo(second.Select(factor => $"{factor.Key}={factor.Value}")),
                "A factor set that varies between calls cannot anchor an identity.");
        }

        #endregion

        #region Fakes

        private sealed class FakeProbe : IPlatformProbe
        {
            public PlatformKind Kind => PlatformKind.Unknown;
            public string? RawIdentity { get; set; }
            public string? HardwareIdentity { get; set; }
            public bool Throws { get; set; }

            public string GetCpuModelName() => string.Empty;
            public IReadOnlyList<SystemGpuInfo> GetGpus() => Array.Empty<SystemGpuInfo>();
            public SystemStorageType GetStorageType(string rootPath) => SystemStorageType.Unknown;
            public double GetCpuLoadPercent() => 0.0;
            public long GetAvailableRamMb() => 0;
            public bool IsUserActive() => true;

            public string? GetRawMachineIdentity()
            {
                if (Throws)
                    throw new InvalidOperationException("probe failure");

                return RawIdentity;
            }

            public string? GetRawHardwareIdentity()
            {
                return HardwareIdentity ?? GetRawMachineIdentity();
            }

            public void Dispose() { }
        }

        #endregion
    }
}
