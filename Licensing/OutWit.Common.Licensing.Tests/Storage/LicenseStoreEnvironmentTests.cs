using System;
using OutWit.Common.Licensing.Storage;

namespace OutWit.Common.Licensing.Tests.Storage
{
    /// <summary>
    /// The Docker delivery path, where there is no installer to drop a file and
    /// no admin screen to paste into before the container first starts.
    /// </summary>
    [TestFixture]
    public sealed class LicenseStoreEnvironmentTests
    {
        private const string VARIABLE = "OutWit_Licensing_Tests__License";

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(VARIABLE, null);
        }

        #region Read Tests

        [Test]
        public void UnsetVariableReadsAsNothingInstalledTest()
        {
            var store = new LicenseStoreEnvironment(VARIABLE);

            Assert.That(store.ReadTokens(), Is.Empty);
        }

        [Test]
        public void SingleTokenIsReadTest()
        {
            Environment.SetEnvironmentVariable(VARIABLE, "  header.payload.signature  ");

            var store = new LicenseStoreEnvironment(VARIABLE);

            Assert.That(store.ReadTokens(), Is.EqualTo(new[] { "header.payload.signature" }));
        }

        [Test]
        public void SeveralTokensAreReadTest()
        {
            // A renewal staged beside a live licence is the normal case; a
            // single-slot variable would force the swap to happen exactly at
            // expiry, which is how "renewed on the right day, still had an
            // outage" happens.
            Environment.SetEnvironmentVariable(VARIABLE, "one.two.three;four.five.six\nseven.eight.nine");

            var store = new LicenseStoreEnvironment(VARIABLE);

            Assert.That(store.ReadTokens(), Has.Count.EqualTo(3));
        }

        [Test]
        public void BlankVariableReadsAsNothingInstalledTest()
        {
            Environment.SetEnvironmentVariable(VARIABLE, "   ");

            var store = new LicenseStoreEnvironment(VARIABLE);

            Assert.That(store.ReadTokens(), Is.Empty);
        }

        [Test]
        public void DefaultVariableFollowsTheEcosystemConventionTest()
        {
            Assert.That(new LicenseStoreEnvironment().Variable, Is.EqualTo("Licensing__License"));
        }

        #endregion

        #region Write Tests

        [Test]
        public void SavingRefusesRatherThanPretendingTest()
        {
            // A store that silently dropped an installed licence would produce a
            // panel reporting success over a licence that vanished on the next
            // restart.
            var store = new LicenseStoreEnvironment(VARIABLE);

            Assert.Throws<NotSupportedException>(() => store.Save("header.payload.signature"));
        }

        [Test]
        public void RemovingIsAlwaysFalseTest()
        {
            var store = new LicenseStoreEnvironment(VARIABLE);

            Assert.That(store.Remove("anything"), Is.False);
        }

        [Test]
        public void StateIsNeverObservedTest()
        {
            var store = new LicenseStoreEnvironment(VARIABLE);

            store.WriteState(new LicenseStoreState { FirstRunUtc = DateTime.UtcNow });

            Assert.That(store.ReadState().FirstRunUtc, Is.EqualTo(default(DateTime)));
        }

        #endregion
    }
}
