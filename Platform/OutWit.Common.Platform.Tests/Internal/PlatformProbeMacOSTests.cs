using OutWit.Common.Platform.Internal;

namespace OutWit.Common.Platform.Tests.Internal
{
    /// <summary>
    /// The macOS hardware identity is the firmware's platform UUID, read from
    /// <c>ioreg</c>'s listing of the platform expert device; the parsing runs
    /// on every OS, the real reading in the provider tests on a Mac.
    /// </summary>
    [TestFixture]
    public sealed class PlatformProbeMacOSTests
    {
        // The shape `ioreg -rd1 -c IOPlatformExpertDevice` prints (trimmed).
        private const string IOREG_LISTING =
            "+-o J616sAP  <class IOPlatformExpertDevice, id 0x100000214, registered, matched, active, busy 0 (63 ms), retain 39>\n" +
            "    {\n" +
            "      \"IOPolledInterface\" = \"AppleARMWatchdogTimerHibernateHandler is not serializable\"\n" +
            "      \"model\" = <\"Mac16,8\">\n" +
            "      \"IOPlatformSerialNumber\" = \"XXXXXXXXXX\"\n" +
            "      \"IOPlatformUUID\" = \"02BC3BC3-3D2E-555E-999B-57BE42F766AD\"\n" +
            "      \"platform-name\" = <\"t6050\">\n" +
            "    }\n";

        [Test]
        public void ThePlatformUuidIsReadFromTheListingTest()
        {
            Assert.That(PlatformProbeMacOS.ParsePlatformUuid(IOREG_LISTING), Is.EqualTo("02BC3BC3-3D2E-555E-999B-57BE42F766AD"));
        }

        [Test]
        public void AListingWithoutAPlatformUuidReadsAsNoneTest()
        {
            Assert.Multiple(() =>
            {
                Assert.That(PlatformProbeMacOS.ParsePlatformUuid(""), Is.Null);
                Assert.That(PlatformProbeMacOS.ParsePlatformUuid("\"IOPlatformSerialNumber\" = \"X\"\n"), Is.Null);
                Assert.That(PlatformProbeMacOS.ParsePlatformUuid("\"IOPlatformUUID\" = \"   \"\n"), Is.Null);
            });
        }
    }
}
