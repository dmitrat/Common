using System;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Common.Licensing.Tests
{
    /// <summary>
    /// The fingerprint prefix is what a customer reads first on a support call,
    /// so it has to actually distinguish one product from another.
    /// </summary>
    [TestFixture]
    public sealed class LicensingOptionsTests
    {
        #region Fingerprint Prefix Tests

        [TestCase("WitSweep", "WSW")]
        [TestCase("WitCloud", "WCL")]
        [TestCase("WitLicense", "WLI")]
        [TestCase("OutWit.Cloud", "OWC")]
        [TestCase("Sweep", "SWE")]
        [TestCase("AB", "AB")]
        [TestCase("", "")]
        public void PrefixIsDerivedFromCapitalisedPartsTest(string product, string expected)
        {
            var options = new LicensingOptions().ForProduct(product);

            Assert.That(options.FingerprintPrefix, Is.EqualTo(expected));
        }

        [Test]
        public void ProductsInOneFamilyGetDistinctPrefixesTest()
        {
            // Taking the first three letters would give every product in the
            // family "WIT", which is the same as having no marker at all.
            var sweep = new LicensingOptions().ForProduct("WitSweep").FingerprintPrefix;
            var cloud = new LicensingOptions().ForProduct("WitCloud").FingerprintPrefix;
            var license = new LicensingOptions().ForProduct("WitLicense").FingerprintPrefix;

            Assert.That(new[] { sweep, cloud, license }, Is.Unique);
        }

        [Test]
        public void ExplicitPrefixWinsTest()
        {
            var options = new LicensingOptions().ForProduct("WitSweep", new Version(1, 0), "SWP");

            Assert.That(options.FingerprintPrefix, Is.EqualTo("SWP"));
        }

        #endregion

        #region Guard Tests

        [Test]
        public void ProductIsRequiredTest()
        {
            Assert.That(
                () => new ServiceCollection().AddLicensing(_ => { }),
                Throws.InvalidOperationException,
                "Registering licensing without naming a product would silently validate every licence against an empty product key.");
        }

        #endregion
    }
}
