using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Storage;

namespace OutWit.Common.Licensing.Tests.Storage
{
    /// <summary>
    /// Several delivery paths at once — compose sets a variable, an installer
    /// drops a file, an operator pastes into the admin screen — and the product
    /// should not have to know which the customer used.
    /// </summary>
    [TestFixture]
    public sealed class LicenseStoreCompositeTests
    {
        private static readonly DateTime NOW = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        private const string VARIABLE = "OutWit_Licensing_Composite_Tests__License";

        private LicenseTestContext m_context = null!;

        [SetUp]
        public void SetUp()
        {
            m_context = new LicenseTestContext();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(VARIABLE, null);
        }

        #region Read Tests

        [Test]
        public void TokensFromEverySourceAreReadTest()
        {
            var primary = new LicenseStoreMemory(Issue(expires: NOW.AddYears(1)));
            Environment.SetEnvironmentVariable(VARIABLE, Issue(expires: NOW.AddYears(2)));

            var composite = new LicenseStoreComposite(primary, new LicenseStoreEnvironment(VARIABLE));

            Assert.That(composite.ReadTokens(), Has.Count.EqualTo(2));
        }

        [Test]
        public void TheSameLicenceThroughTwoPathsIsOneLicenceTest()
        {
            // Counting it twice would make a superseded document look like it
            // was still installed.
            var token = Issue(expires: NOW.AddYears(1));

            var primary = new LicenseStoreMemory(token);
            Environment.SetEnvironmentVariable(VARIABLE, token);

            var composite = new LicenseStoreComposite(primary, new LicenseStoreEnvironment(VARIABLE));

            Assert.That(composite.ReadTokens(), Has.Count.EqualTo(1));
        }

        [Test]
        public void OneUnreachableSourceDoesNotBlindTheOthersTest()
        {
            // A mount that is not there yet is a normal container start, and the
            // licence pasted into the admin screen is still perfectly good.
            var primary = new LicenseStoreMemory(Issue(expires: NOW.AddYears(1)));

            var composite = new LicenseStoreComposite(primary, new LicenseStoreThrowing());

            Assert.That(composite.ReadTokens(), Has.Count.EqualTo(1));
        }

        #endregion

        #region Write Tests

        [Test]
        public void InstallsGoToThePrimaryTest()
        {
            var primary = new LicenseStoreMemory();
            var composite = new LicenseStoreComposite(primary, new LicenseStoreEnvironment(VARIABLE));

            composite.Save(Issue(expires: NOW.AddYears(1)));

            Assert.That(primary.ReadTokens(), Has.Count.EqualTo(1),
                "Writing must never be attempted against a read-only source.");
        }

        [Test]
        public void TheSidecarBelongsToThePrimaryTest()
        {
            var primary = new LicenseStoreMemory();
            var composite = new LicenseStoreComposite(primary, new LicenseStoreEnvironment(VARIABLE));

            composite.WriteState(new LicenseStoreState { FirstRunUtc = NOW });

            Assert.That(primary.ReadState().FirstRunUtc, Is.EqualTo(NOW));
        }

        [Test]
        public void ALicenceTheProductDidNotPutThereCannotBeRemovedTest()
        {
            // The honest answer: the product did not install it and cannot take
            // it away. Reporting success would leave a panel claiming a licence
            // was uninstalled that comes straight back on the next reload.
            var payload = LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: NOW.AddYears(1));
            Environment.SetEnvironmentVariable(VARIABLE, m_context.Issue(payload));

            var composite = new LicenseStoreComposite(new LicenseStoreMemory(), new LicenseStoreEnvironment(VARIABLE));

            Assert.Multiple(() =>
            {
                Assert.That(composite.Remove(payload.Id), Is.False);
                Assert.That(composite.ReadTokens(), Has.Count.EqualTo(1));
            });
        }

        #endregion

        #region Service Tests

        [Test]
        public async Task ServiceHonoursALicenceDeliveredByTheEnvironmentTest()
        {
            Environment.SetEnvironmentVariable(VARIABLE, Issue(expires: NOW.AddYears(1)));

            var options = new LicensingOptions()
                .ForProduct(LicenseTestContext.PRODUCT, new Version(1, 5, 0))
                .WithKeyRing(m_context.Ring())
                .WithStore(new LicenseStoreComposite(new LicenseStoreMemory(), new LicenseStoreEnvironment(VARIABLE)))
                .WithBinding(new LicenseBindingProviderNone())
                .WithClock(() => NOW);

            using var service = new LicenseService(options);
            await service.ReloadAsync();

            Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Licensed));
        }

        [Test]
        public async Task BestOfBothSourcesWinsTest()
        {
            // Selection does not care which door a licence came through: the
            // runtime picks whichever is currently best, exactly as it already
            // does within one directory.
            Environment.SetEnvironmentVariable(VARIABLE, Issue(expires: NOW.AddDays(10)));

            var pasted = LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: NOW.AddYears(3));

            var options = new LicensingOptions()
                .ForProduct(LicenseTestContext.PRODUCT, new Version(1, 5, 0))
                .WithKeyRing(m_context.Ring())
                .WithStore(new LicenseStoreComposite(
                    new LicenseStoreMemory(m_context.Issue(pasted)),
                    new LicenseStoreEnvironment(VARIABLE)))
                .WithBinding(new LicenseBindingProviderNone())
                .WithClock(() => NOW);

            using var service = new LicenseService(options);
            await service.ReloadAsync();

            Assert.That(service.State.Payload!.Id, Is.EqualTo(pasted.Id));
        }

        #endregion

        #region Tools

        private string Issue(DateTime expires)
        {
            return m_context.Issue(LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: expires));
        }

        /// <summary>A source that is not there — a volume yet to be mounted.</summary>
        private sealed class LicenseStoreThrowing : ILicenseStore
        {
            public IReadOnlyList<string> ReadTokens() => throw new UnauthorizedAccessException();

            public void Save(string token) => throw new UnauthorizedAccessException();

            public bool Remove(string licenseId) => throw new UnauthorizedAccessException();

            public LicenseStoreState ReadState() => throw new UnauthorizedAccessException();

            public void WriteState(LicenseStoreState state) => throw new UnauthorizedAccessException();
        }

        #endregion
    }
}
