using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Binding;

namespace OutWit.Common.Licensing.Tests.Binding
{
    /// <summary>
    /// The deployment binding: three factors at 3-of-3, and the normalisation
    /// that stops an operator's punctuation from being a different deployment.
    /// </summary>
    [TestFixture]
    public sealed class LicenseBindingProviderTenantTests
    {
        #region Deployment Tests

        [Test]
        public async Task ADeploymentContributesThreeFactorsTest()
        {
            var provider = LicenseBindingProviderTenant.ForDeployment(
                "b3f1c7a94e0d", "https://acme.example", "https://auth.acme.example");

            var factors = await provider.CollectAsync();

            Assert.Multiple(() =>
            {
                Assert.That(provider.Kind, Is.EqualTo(LicenseBindingKind.Tenant));
                Assert.That(factors.Select(factor => factor.Key), Is.EquivalentTo(new[]
                {
                    LicenseBindingProviderTenant.FACTOR_INSTALL_ID,
                    LicenseBindingProviderTenant.FACTOR_PUBLIC_BASE_URL,
                    LicenseBindingProviderTenant.FACTOR_ISSUER
                }));
            });
        }

        [Test]
        public async Task AServiceWithNoIdentityAuthorityContributesTwoTest()
        {
            // A blank value is not contributed rather than hashed as empty: an
            // absent factor is something the threshold can account for, an empty
            // one would make two different services look alike.
            var factors = await LicenseBindingProviderTenant
                .ForDeployment("b3f1c7a94e0d", "https://acme.example")
                .CollectAsync();

            Assert.That(factors, Has.Count.EqualTo(2));
        }

        #endregion

        #region Normalisation Tests

        [Test]
        public async Task ATrailingSlashIsNotADifferentDeploymentTest()
        {
            // An operator who pastes the URL with a slash into one config and
            // without into another has not moved anything. Left alone these hash
            // differently and the licence dies for a reason nobody can see.
            var withSlash = await LicenseBindingProviderTenant
                .ForDeployment("id", "https://acme.example/").CollectAsync();

            var withoutSlash = await LicenseBindingProviderTenant
                .ForDeployment("id", "https://acme.example").CollectAsync();

            Assert.That(Hash(withSlash, LicenseBindingProviderTenant.FACTOR_PUBLIC_BASE_URL),
                Is.EqualTo(Hash(withoutSlash, LicenseBindingProviderTenant.FACTOR_PUBLIC_BASE_URL)));
        }

        [Test]
        public async Task CaseIsNotADifferentDeploymentTest()
        {
            var upper = await LicenseBindingProviderTenant
                .ForDeployment("id", "https://ACME.example").CollectAsync();

            var lower = await LicenseBindingProviderTenant
                .ForDeployment("id", "https://acme.example").CollectAsync();

            Assert.That(Hash(upper, LicenseBindingProviderTenant.FACTOR_PUBLIC_BASE_URL),
                Is.EqualTo(Hash(lower, LicenseBindingProviderTenant.FACTOR_PUBLIC_BASE_URL)));
        }

        [Test]
        public async Task ADifferentAddressIsADifferentDeploymentTest()
        {
            // The whole point of the factor: a clone worth having is reachable
            // somewhere else, and that shows.
            var here = await LicenseBindingProviderTenant
                .ForDeployment("id", "https://acme.example").CollectAsync();

            var there = await LicenseBindingProviderTenant
                .ForDeployment("id", "https://acme-staging.example").CollectAsync();

            Assert.That(Hash(here, LicenseBindingProviderTenant.FACTOR_PUBLIC_BASE_URL),
                Is.Not.EqualTo(Hash(there, LicenseBindingProviderTenant.FACTOR_PUBLIC_BASE_URL)));
        }

        [TestCase("https://acme.example///", "https://acme.example")]
        [TestCase("  https://acme.example/  ", "https://acme.example")]
        [TestCase(null, null)]
        [TestCase("", "")]
        public void UrlsAreTrimmedToWhatBothSidesWillAgreeOnTest(string? input, string? expected)
        {
            Assert.That(LicenseBindingProviderTenant.NormalizeUrl(input), Is.EqualTo(expected));
        }

        #endregion

        #region Tools

        private static string Hash(IReadOnlyList<Abstract.LicenseFactor> factors, string key)
        {
            return factors.Single(factor => factor.Key == key).Hash;
        }

        #endregion
    }
}
