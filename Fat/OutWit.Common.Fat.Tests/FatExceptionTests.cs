namespace OutWit.Common.Fat.Tests
{
    [TestFixture]
    public class FatExceptionTests
    {
        #region Construction Tests

        [Test]
        public void KindAndMessageAreKeptTest()
        {
            var error = new FatException(FatErrorKind.Corrupt, "broken");

            Assert.That(error.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Is.EqualTo("broken"));
            Assert.That(error, Is.InstanceOf<IOException>());
        }

        [Test]
        public void InnerExceptionIsKeptTest()
        {
            var cause = new EndOfStreamException();

            var error = new FatException(FatErrorKind.NotRecognized, "gone", cause);

            Assert.That(error.Kind, Is.EqualTo(FatErrorKind.NotRecognized));
            Assert.That(error.InnerException, Is.SameAs(cause));
        }

        #endregion
    }
}
