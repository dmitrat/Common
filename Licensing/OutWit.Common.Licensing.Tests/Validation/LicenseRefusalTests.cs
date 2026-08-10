using System;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.Tests.Validation
{
    /// <summary>
    /// What a refusal says. Validation reports a reason rather than a boolean so
    /// that most licence questions become a thirty-second answer — which only
    /// works if the reason names the right axis.
    /// </summary>
    [TestFixture]
    public sealed class LicenseRefusalTests
    {
        #region Binding Tests

        [Test]
        public void ADeploymentBindingSaysDeploymentTest()
        {
            // It used to say "machine" whatever the binding was. Told that about
            // a container, a server operator goes and looks at hardware — and
            // the answer is a URL or an installation id in a config file.
            var described = Refusal(LicenseBindingKind.Tenant).Describe();

            Assert.Multiple(() =>
            {
                Assert.That(described, Does.Contain("a different deployment"));
                Assert.That(described, Does.Not.Contain("machine"));
            });
        }

        [Test]
        public void AMachineBindingStillSaysMachineTest()
        {
            var described = Refusal(LicenseBindingKind.Machine).Describe();

            Assert.That(described, Does.Contain("a different machine"));
        }

        [Test]
        public void TheCustomerIsNotMistakenForTheOwnerOfTheOtherMachineTest()
        {
            // "issued for a different machine to ACME GmbH" reads as though the
            // machine were theirs.
            var described = Refusal(LicenseBindingKind.Machine).Describe();

            Assert.Multiple(() =>
            {
                Assert.That(described, Does.Contain("ACME GmbH"));
                Assert.That(described, Does.Not.Contain("machine to ACME"));
            });
        }

        [Test]
        public void AnAnonymousLicenceStillReadsAsASentenceTest()
        {
            var described = LicenseValidationResult
                .Failure(LicenseStatus.BindingMismatch, Payload(LicenseBindingKind.Tenant, customer: null))
                .Describe();

            Assert.That(described, Is.EqualTo("This licence was issued for a different deployment."));
        }

        #endregion

        #region Tools

        private static LicenseValidationResult Refusal(LicenseBindingKind kind)
        {
            return LicenseValidationResult.Failure(LicenseStatus.BindingMismatch, Payload(kind));
        }

        private static LicensePayload Payload(LicenseBindingKind kind, string? customer = "ACME GmbH")
        {
            return new LicensePayload
            {
                Id = "licence-1",
                Product = "TestProduct",
                Binding = new LicenseBinding { Kind = kind, Threshold = 3 },
                Customer = customer == null ? null : new LicenseCustomer { Id = "acme", Name = customer }
            };
        }

        #endregion
    }
}
