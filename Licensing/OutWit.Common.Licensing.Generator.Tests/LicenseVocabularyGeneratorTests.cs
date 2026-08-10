using Microsoft.CodeAnalysis;

namespace OutWit.Common.Licensing.Generator.Tests
{
    [TestFixture]
    public class LicenseVocabularyGeneratorTests : GeneratorTestBase
    {
        #region Constants

        private const string DESCRIPTOR = """
            {
              // A descriptor is meant to be read and edited by people.
              "product": "WitSweep",
              "features": [
                { "key": "format.nas", "name": "Nastran decks" },
                { "key": "integration.prepomax" }
              ],
              "limits": [
                { "key": "maxVariants", "name": "Variants per sweep", "default": 64 },
                { "key": "maxNodes" }
              ]
            }
            """;

        #endregion

        #region Fields

        private LicenseVocabularyGenerator m_generator = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_generator = new LicenseVocabularyGenerator();
        }

        #endregion

        #region Emission Tests

        /// <summary>
        /// The hazard with no other mitigation: <c>HasFeature("format.nass")</c>
        /// compiles, runs, and quietly disables a capability the customer paid
        /// for. A constant makes it a build failure.
        /// </summary>
        [Test]
        public void KeysBecomeConstantsTest()
        {
            var output = RunClean(m_generator, Files(("witsweep.product.json", DESCRIPTOR)));

            Assert.That(Constant(output, "WitSweepLicense", "Product"), Is.EqualTo("WitSweep"));
            Assert.That(Constant(output, "WitSweepLicense+Features", "FormatNas"), Is.EqualTo("format.nas"));
            Assert.That(Constant(output, "WitSweepLicense+Features", "IntegrationPrepomax"),
                Is.EqualTo("integration.prepomax"));
            Assert.That(Constant(output, "WitSweepLicense+Limits", "MaxVariants"), Is.EqualTo("maxVariants"));
            Assert.That(Constant(output, "WitSweepLicense+Limits", "MaxNodes"), Is.EqualTo("maxNodes"));
        }

        /// <summary>
        /// The declaration and the constants come from one file, so what the
        /// runtime is told and what the code asks about cannot drift apart.
        /// </summary>
        [Test]
        public void DeclareCarriesNamesAndDefaultsTest()
        {
            var source = Sources(RunClean(m_generator, Files(("witsweep.product.json", DESCRIPTOR))));

            Assert.That(source, Does.Contain("public static void Declare(LicenseVocabulary vocabulary)"));
            Assert.That(source, Does.Contain(".Feature(\"format.nas\", \"Nastran decks\")"));
            Assert.That(source, Does.Contain(".Limit(\"maxVariants\", \"Variants per sweep\", 64L)"));

            // An unset limit is unlimited unless the product says otherwise, and
            // that has to survive as null rather than becoming a zero.
            Assert.That(source, Does.Contain(".Limit(\"maxNodes\", \"\", null)"));
        }

        /// <summary>A key that is not an identifier is still a legal key.</summary>
        [Test]
        public void AwkwardKeysStillProduceIdentifiersTest()
        {
            var descriptor = """
                { "product": "test-client", "features": [ { "key": "2fa" }, { "key": "sso-saml" } ] }
                """;

            var output = RunClean(m_generator, Files(("test.product.json", descriptor)));

            Assert.That(Constant(output, "TestClientLicense+Features", "_2fa"), Is.EqualTo("2fa"));
            Assert.That(Constant(output, "TestClientLicense+Features", "SsoSaml"), Is.EqualTo("sso-saml"));
        }

        #endregion

        #region Refusal Tests

        /// <summary>
        /// Falling back to an empty vocabulary would reintroduce the silent
        /// failure the generator exists to remove, so a broken descriptor stops
        /// the build.
        /// </summary>
        [Test]
        public void UnparseableDescriptorIsAnErrorTest()
        {
            Run(m_generator, Files(("witsweep.product.json", "{ \"product\": \"WitSweep\", ")), out var diagnostics);

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("OWL001"));
            Assert.That(diagnostics.First().Severity, Is.EqualTo(DiagnosticSeverity.Error));
        }

        [Test]
        public void MissingProductIsAnErrorTest()
        {
            Run(m_generator, Files(("witsweep.product.json", """{ "features": [] }""")), out var diagnostics);

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("OWL001"));
        }

        /// <summary>
        /// The runtime matches keys case-insensitively, so two entries differing
        /// only in case would generate two members that mean one thing.
        /// </summary>
        [Test]
        public void DuplicateKeyIsAnErrorTest()
        {
            var descriptor = """
                { "product": "WitSweep", "features": [ { "key": "sso" }, { "key": "SSO" } ] }
                """;

            Run(m_generator, Files(("witsweep.product.json", descriptor)), out var diagnostics);

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("OWL001"));
        }

        #endregion

        #region Tools

        private static Dictionary<string, string> Files(params (string Name, string Text)[] files)
        {
            return files.ToDictionary(file => file.Name, file => file.Text);
        }

        #endregion
    }
}
