using System.Text;
using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class CodePage437Tests
    {
        #region Table Tests

        [Test]
        public void UpperHalfMatchesDotNetCodePageTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var reference = Encoding.GetEncoding(437);

            using (Assert.EnterMultipleScope())
            {
                for (int value = 0x80; value <= 0xFF; value++)
                    Assert.That(CodePage437.Decode((byte)value), Is.EqualTo(reference.GetString(new[] { (byte)value })[0]), $"0x{value:X2}");
            }
        }

        [Test]
        public void LowerHalfIsAsciiTest()
        {
            var all = Enumerable.Range(0x20, 0x60).Select(v => (byte)v).ToArray();

            Assert.That(CodePage437.Decode(all), Is.EqualTo(Encoding.ASCII.GetString(all)));
        }

        #endregion
    }
}
