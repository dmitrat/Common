using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Messenger;

namespace OutWit.Common.Messenger.Tests
{
    [TestFixture]
    public class MessengerTransportExtensionsTests
    {
        #region Fields

        private CapturingTransport m_transport = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_transport = new CapturingTransport();
        }

        #endregion

        #region RenderText Tests

        [Test]
        public void RenderTextWithoutIconOrTitleReturnsTextTest()
        {
            var message = new MessengerMessage { Target = "c", Text = "body" };

            Assert.That(message.RenderText(), Is.EqualTo("body"));
        }

        [Test]
        public void RenderTextPrependsIconTest()
        {
            var message = new MessengerMessage { Target = "c", Text = "body", Icon = "⚠️" };

            Assert.That(message.RenderText(), Is.EqualTo("⚠️ body"));
        }

        [Test]
        public void RenderTextPlacesTitleAboveBodyTest()
        {
            var message = new MessengerMessage { Target = "c", Text = "body", Title = "Heads up" };

            Assert.That(message.RenderText(), Is.EqualTo("Heads up\n\nbody"));
        }

        [Test]
        public void RenderTextCombinesIconAndTitleTest()
        {
            var message = new MessengerMessage { Target = "c", Text = "body", Title = "Heads up", Icon = "❌" };

            Assert.That(message.RenderText(), Is.EqualTo("❌ Heads up\n\nbody"));
        }

        #endregion

        #region MessageEmoji Tests

        [Test]
        public void EmojiForSeverityMapsCorrectlyTest()
        {
            Assert.That(MessageEmoji.For(MessageSeverity.Info), Is.EqualTo("ℹ️"));
            Assert.That(MessageEmoji.For(MessageSeverity.Success), Is.EqualTo("✅"));
            Assert.That(MessageEmoji.For(MessageSeverity.Warning), Is.EqualTo("⚠️"));
            Assert.That(MessageEmoji.For(MessageSeverity.Error), Is.EqualTo("❌"));
            Assert.That(MessageEmoji.For(MessageSeverity.None), Is.Null);
        }

        #endregion

        #region Send Overload Tests

        [Test]
        public async Task SendErrorUsesErrorEmojiTest()
        {
            await m_transport.SendErrorAsync("chat-1", "Something broke");

            Assert.That(m_transport.Last!.Icon, Is.EqualTo(MessageEmoji.Error));
            Assert.That(m_transport.Last.Target, Is.EqualTo("chat-1"));
            Assert.That(m_transport.Last.Text, Is.EqualTo("Something broke"));
        }

        [Test]
        public async Task SendWarningUsesWarningEmojiTest()
        {
            await m_transport.SendWarningAsync("chat-1", "Careful");

            Assert.That(m_transport.Last!.Icon, Is.EqualTo(MessageEmoji.Warning));
        }

        [Test]
        public async Task SendInfoUsesInfoEmojiTest()
        {
            await m_transport.SendInfoAsync("chat-1", "FYI");

            Assert.That(m_transport.Last!.Icon, Is.EqualTo(MessageEmoji.Info));
        }

        [Test]
        public async Task SendSuccessUsesSuccessEmojiTest()
        {
            await m_transport.SendSuccessAsync("chat-1", "Done");

            Assert.That(m_transport.Last!.Icon, Is.EqualTo(MessageEmoji.Success));
        }

        [Test]
        public async Task SendWithSeverityPicksMatchingEmojiTest()
        {
            await m_transport.SendAsync("chat-1", "msg", MessageSeverity.Warning);

            Assert.That(m_transport.Last!.Icon, Is.EqualTo(MessageEmoji.Warning));
        }

        [Test]
        public async Task SendWithExplicitIconPassesItThroughTest()
        {
            await m_transport.SendAsync("chat-1", "buy", icon: "🟢");

            Assert.That(m_transport.Last!.Icon, Is.EqualTo("🟢"));
        }

        [Test]
        public async Task SendPassesFormatAndSilentTest()
        {
            await m_transport.SendErrorAsync("chat-1", "x", MessageFormat.Markdown, silent: true);

            Assert.That(m_transport.Last!.Format, Is.EqualTo(MessageFormat.Markdown));
            Assert.That(m_transport.Last.SilentNotification, Is.True);
        }

        #endregion

        #region Stub

        private sealed class CapturingTransport : IMessengerTransport
        {
            public MessengerMessage? Last { get; private set; }

            public Task<MessageSendResult> SendAsync(MessengerMessage message, CancellationToken ct = default)
            {
                Last = message;
                return Task.FromResult(MessageSendResult.Success("ok"));
            }
        }

        #endregion
    }
}
