using Microsoft.CodeAnalysis;
using OutWit.Common.Licensing.Keys;

namespace OutWit.Common.Licensing.Generator.Tests
{
    [TestFixture]
    public class LicenseKeyRingGeneratorTests : GeneratorTestBase
    {
        #region Constants

        private const string PEM = "-----BEGIN PUBLIC KEY-----\\nMFkwEwYHKoZIzj0CAQYIKoZI\\n-----END PUBLIC KEY-----";

        private const string PRODUCTION = """
            {
              // The ring the service exported. Comments are allowed here and are
              // not allowed by the runtime reader, which is the point.
              "product": "WitSweep",
              "keys": [
                {
                  "kid": "witsweep-2026",
                  "alg": "ES256",
                  "publicKeyPem": "$PEM",
                  "products": [ "WitSweep" ],
                  "policy": "Commercial",
                }
              ]
            }
            """;

        private const string DEVELOPMENT = """
            {
              "product": "WitSweep",
              "keys": [
                { "kid": "witsweep-dev", "alg": "ES256", "publicKeyPem": "$PEM", "products": [ "WitSweep" ], "policy": "TrialOnly" }
              ]
            }
            """;

        #endregion

        #region Fields

        private LicenseKeyRingGenerator m_generator = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_generator = new LicenseKeyRingGenerator();
        }

        #endregion

        #region Emission Tests

        /// <summary>
        /// The whole reason the ring is re-emitted rather than copied: the file
        /// may carry comments and trailing commas, and the runtime reader accepts
        /// neither. A verbatim copy would compile happily and then parse to an
        /// empty ring at startup — a product that trusts nothing and can only say
        /// "unknown key id".
        /// </summary>
        [Test]
        public void RingBecomesAConstantTheRuntimeCanReadTest()
        {
            var output = RunClean(m_generator, Files(("witsweep.keyring.json", Ring(PRODUCTION))));

            var constant = Constant(output, "WitSweepKeyRing", "RING");

            Assert.That(constant, Is.Not.Null);

            var ring = LicenseKeyRing.FromJson(constant);

            Assert.That(ring.Keys, Has.Count.EqualTo(1));
            Assert.That(ring.Find("witsweep-2026"), Is.Not.Null);
            Assert.That(ring.Find("witsweep-2026")!.Algorithm, Is.EqualTo(LicenseAlgorithm.ES256));
            Assert.That(ring.Find("witsweep-2026")!.CoversProduct("WitSweep"), Is.True);
        }

        /// <summary>A PEM carries newlines, and a constant must not lose them.</summary>
        [Test]
        public void PemSurvivesTheConstantTest()
        {
            var output = RunClean(m_generator, Files(("witsweep.keyring.json", Ring(PRODUCTION))));

            var ring = LicenseKeyRing.FromJson(Constant(output, "WitSweepKeyRing", "RING"));

            Assert.That(ring.Find("witsweep-2026")!.PublicKeyPem, Does.Contain("\n"));
            Assert.That(ring.Find("witsweep-2026")!.PublicKeyPem, Does.StartWith("-----BEGIN PUBLIC KEY-----"));
        }

        /// <summary>Generated output that changes between builds is output nobody can review in a diff.</summary>
        [Test]
        public void ConstantDoesNotDependOnMemberOrderTest()
        {
            var reordered = """
                {
                  "keys": [
                    {
                      "products": [ "WitSweep" ],
                      "policy": "Commercial",
                      "publicKeyPem": "$PEM",
                      "alg": "ES256",
                      "kid": "witsweep-2026"
                    }
                  ],
                  "product": "WitSweep"
                }
                """;

            var first = RunClean(m_generator, Files(("witsweep.keyring.json", Ring(PRODUCTION))));
            var second = RunClean(new LicenseKeyRingGenerator(), Files(("witsweep.keyring.json", Ring(reordered))));

            Assert.That(Constant(second, "WitSweepKeyRing", "RING"),
                Is.EqualTo(Constant(first, "WitSweepKeyRing", "RING")));
        }

        #endregion

        #region Development Ring Tests

        /// <summary>
        /// A development licence has to be worthless against a shipped build, and
        /// nothing at runtime enforces that — the compiler does, by leaving the
        /// development keys out of a build with no DEBUG symbol.
        /// </summary>
        [Test]
        public void DevelopmentRingIsAbsentFromAReleaseBuildTest()
        {
            var files = Files(
                ("witsweep.keyring.json", Ring(PRODUCTION)),
                ("witsweep.dev.keyring.json", Ring(DEVELOPMENT)));

            var release = RunClean(m_generator, files);

            Assert.That(Constant(release, "WitSweepKeyRing", "RING_DEVELOPMENT"), Is.Null);
            Assert.That(LicenseKeyRing.FromJson(Constant(release, "WitSweepKeyRing", "RING")).Find("witsweep-dev"),
                Is.Null);
        }

        [Test]
        public void DevelopmentRingIsPresentInADebugBuildTest()
        {
            var files = Files(
                ("witsweep.keyring.json", Ring(PRODUCTION)),
                ("witsweep.dev.keyring.json", Ring(DEVELOPMENT)));

            var debug = RunClean(new LicenseKeyRingGenerator(), files, debug: true);

            var development = LicenseKeyRing.FromJson(Constant(debug, "WitSweepKeyRing", "RING_DEVELOPMENT"));

            Assert.That(development.Find("witsweep-dev"), Is.Not.Null);
        }

        /// <summary>
        /// A product whose only ring is the development one trusts nothing in
        /// Release. That is the right outcome and it is still said out loud,
        /// because it does not look like an outcome anybody chose.
        /// </summary>
        [Test]
        public void DevelopmentOnlyRingLeavesReleaseTrustingNothingTest()
        {
            var output = Run(m_generator, Files(("witsweep.dev.keyring.json", Ring(DEVELOPMENT))),
                out var diagnostics);

            AssertCompiles(output);

            Assert.That(Ids(diagnostics), Does.Contain("OWL003"));
            Assert.That(LicenseKeyRing.FromJson(Constant(output, "WitSweepKeyRing", "RING")).Keys, Is.Empty);
        }

        #endregion

        #region Refusal Tests

        /// <summary>
        /// The runtime drops a key with no <c>kid</c> without a word, which leaves
        /// a ring one key short of working and a customer with "licence invalid".
        /// </summary>
        [Test]
        public void KeyWithoutKeyIdIsAnErrorTest()
        {
            var ring = """
                { "product": "WitSweep", "keys": [ { "alg": "ES256", "publicKeyPem": "x", "products": [ "WitSweep" ] } ] }
                """;

            Run(m_generator, Files(("witsweep.keyring.json", ring)), out var diagnostics);

            Assert.That(Ids(diagnostics), Does.Contain("OWL002"));
        }

        /// <summary>The runtime lets the second of two identical kids win, silently.</summary>
        [Test]
        public void DuplicateKeyIdIsAnErrorTest()
        {
            var ring = """
                {
                  "product": "WitSweep",
                  "keys": [
                    { "kid": "same", "alg": "ES256", "publicKeyPem": "x", "products": [ "WitSweep" ] },
                    { "kid": "SAME", "alg": "ES256", "publicKeyPem": "y", "products": [ "WitSweep" ] }
                  ]
                }
                """;

            Run(m_generator, Files(("witsweep.keyring.json", ring)), out var diagnostics);

            Assert.That(Ids(diagnostics), Does.Contain("OWL002"));
        }

        [Test]
        public void EmptyRingIsAnErrorTest()
        {
            Run(m_generator, Files(("witsweep.keyring.json", """{ "product": "WitSweep", "keys": [] }""")),
                out var diagnostics);

            Assert.That(Ids(diagnostics), Does.Contain("OWL002"));
        }

        [Test]
        public void UnparseableRingIsAnErrorTest()
        {
            Run(m_generator, Files(("witsweep.keyring.json", "{ \"product\": ")), out var diagnostics);

            Assert.That(Ids(diagnostics), Does.Contain("OWL002"));
        }

        /// <summary>
        /// <c>alg</c> and <c>policy</c> are enums at the far end, and an unknown
        /// value does not spoil only its own key — the runtime reader throws and
        /// returns an <b>empty</b> ring, so one mistyped word costs every key in
        /// the file. Written as a test because it is what caught it: a plausible
        /// "Development" policy silently emptied a ring in this very fixture.
        /// </summary>
        [TestCase("\"alg\": \"ES257\"", "\"policy\": \"Commercial\"")]
        [TestCase("\"alg\": \"ES256\"", "\"policy\": \"Development\"")]
        public void UnknownEnumValueIsAnErrorTest(string algorithm, string policy)
        {
            var ring = $$"""
                { "product": "WitSweep", "keys": [ { "kid": "k", {{algorithm}}, {{policy}}, "publicKeyPem": "x", "products": [ "WitSweep" ] } ] }
                """;

            Run(m_generator, Files(("witsweep.keyring.json", ring)), out var diagnostics);

            Assert.That(Ids(diagnostics), Does.Contain("OWL002"));
        }

        /// <summary>
        /// A key that names no product covers none, and a ring that covers not
        /// even its own product refuses every licence written for it.
        /// </summary>
        [Test]
        public void RingThatCoversNothingIsWarnedAboutTest()
        {
            var ring = """
                { "product": "WitSweep", "keys": [ { "kid": "k", "alg": "ES256", "publicKeyPem": "x", "products": [ "WitCloud" ] } ] }
                """;

            Run(m_generator, Files(("witsweep.keyring.json", ring)), out var diagnostics);

            Assert.That(Ids(diagnostics), Does.Contain("OWL003"));
        }

        /// <summary>Which ring a product trusts cannot be decided by file ordering.</summary>
        [Test]
        public void TwoRingsForOneProductIsAnErrorTest()
        {
            var files = Files(
                ("witsweep.keyring.json", Ring(PRODUCTION)),
                ("old.keyring.json", Ring(PRODUCTION).Replace("witsweep-2026", "witsweep-2025")));

            Run(m_generator, files, out var diagnostics);

            Assert.That(Ids(diagnostics), Does.Contain("OWL004"));
        }

        #endregion

        #region Tools

        private static string Ring(string template)
        {
            return template.Replace("$PEM", PEM);
        }

        private static Dictionary<string, string> Files(params (string Name, string Text)[] files)
        {
            return files.ToDictionary(file => file.Name, file => file.Text);
        }

        private static IEnumerable<string> Ids(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.Select(diagnostic => diagnostic.Id).ToList();
        }

        #endregion
    }
}
