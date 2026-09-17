using System.Text;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class ShortNameTests
    {
        #region Decoding Tests

        [TestCase("README  TXT", "README.TXT")]
        [TestCase("MAKEFILE   ", "MAKEFILE")]
        [TestCase("A          ", "A")]
        [TestCase("LONGFI~1TXT", "LONGFI~1.TXT")]
        [TestCase("X       C  ", "X.C")]
        public void StoredNameGetsItsDotBackTest(string raw, string expected)
        {
            Assert.That(ShortName.Decode(Encoding.ASCII.GetBytes(raw)), Is.EqualTo(expected));
        }

        [TestCase(0x00, "README.TXT")]
        [TestCase(0x08, "readme.TXT")]
        [TestCase(0x10, "README.txt")]
        [TestCase(0x18, "readme.txt")]
        [TestCase(0xE7, "README.TXT")]
        public void CaseFlagsLowerTheirPartTest(int flags, string expected)
        {
            var raw = Encoding.ASCII.GetBytes("README  TXT");

            Assert.That(ShortName.Decode(raw, (byte)flags), Is.EqualTo(expected));
            Assert.That(ShortName.Decode(raw), Is.EqualTo("README.TXT"));
        }

        [Test]
        public void LeadingFiveStandsForE5Test()
        {
            var raw = Encoding.ASCII.GetBytes("\u0005BC     TXT");

            Assert.That(ShortName.Decode(raw), Is.EqualTo("\u03C3BC.TXT"));
        }

        [TestCase("A\u0005C     TXT", "A_C.TXT")]
        [TestCase("A/B     TXT", "A_B.TXT")]
        [TestCase("A\\B     TXT", "A_B.TXT")]
        [TestCase("NAME    T\u0001T", "NAME.T_T")]
        public void SeparatorsAndControlCharactersBecomeUnderscoresTest(string raw, string expected)
        {
            Assert.That(ShortName.Decode(Encoding.ASCII.GetBytes(raw)), Is.EqualTo(expected));
        }

        [Test]
        public void HighBytesUseCodePage437Test()
        {
            var raw = Encoding.ASCII.GetBytes("XNICXDX TXT");
            raw[0] = 0x9A;
            raw[4] = 0x99;
            raw[6] = 0x90;

            Assert.That(ShortName.Decode(raw), Is.EqualTo("\u00DCNIC\u00D6D\u00C9.TXT"));
        }

        #endregion

        #region Checksum Tests

        [TestCase("README  TXT")]
        [TestCase("LONGFI~1TXT")]
        [TestCase("ABCD~1  TXT")]
        [TestCase("\u00FF\u00FF\u00FF\u00FF\u00FF\u00FF\u00FF\u00FF\u00FF\u00FF\u00FF")]
        public void ChecksumFollowsTheSpecificationTest(string raw)
        {
            var name = raw.Select(c => (byte)c).ToArray();

            Assert.That(ShortName.Checksum(name), Is.EqualTo(DirectorySlotBuilder.Checksum(name)));
        }

        #endregion

        #region Dot Entry Tests

        [TestCase(".          ", true)]
        [TestCase("..         ", true)]
        [TestCase("...        ", false)]
        [TestCase(".A         ", false)]
        [TestCase("A.         ", false)]
        [TestCase("           ", false)]
        public void DotEntriesAreRecognisedTest(string raw, bool isDot)
        {
            Assert.That(ShortName.IsDotEntry(Encoding.ASCII.GetBytes(raw)), Is.EqualTo(isDot));
        }

        #endregion
    }
}
