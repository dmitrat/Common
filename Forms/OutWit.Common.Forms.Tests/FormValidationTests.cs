using System.Linq;
using MemoryPack;
using NUnit.Framework;
using OutWit.Common.Forms.Model;

namespace OutWit.Common.Forms.Tests
{
    /// <summary>
    /// What the authority says back, and what a renderer is entitled to conclude from it.
    /// </summary>
    [TestFixture]
    public class FormValidationTests
    {
        #region Tests

        /// <summary>Nothing to say is the usual answer, and it is a good one.</summary>
        [Test]
        public void SilenceIsGoodTest()
        {
            Assert.That(FormValidation.Good().IsGood, Is.True);
        }

        /// <summary>
        /// A warning does not stop anything. It is the authority saying "that is allowed and
        /// probably not what you meant", which the person standing in front of the patient is
        /// entitled to overrule; an error is a configuration that will not be accepted.
        /// </summary>
        [Test]
        public void AWarningDoesNotStopAnythingAndAnErrorDoesTest()
        {
            var warned = FormValidation.Of(new FormIssue("Rate", "Forms.Unusual", FormSeverity.Warning));
            var refused = FormValidation.Of(new FormIssue("Rate", "Forms.TooFast", FormSeverity.Error));

            Assert.Multiple(() =>
            {
                Assert.That(warned.IsGood, Is.True);
                Assert.That(refused.IsGood, Is.False);
            });
        }

        [Test]
        public void IssuesAreFoundByTheFieldTheyAreAboutTest()
        {
            var validation = FormValidation.Of(
                new FormIssue("Rate", "Forms.TooFast"),
                new FormIssue("Diary", "Forms.NeedsDiary"),
                new FormIssue(null, "Forms.WholeThing"));

            Assert.Multiple(() =>
            {
                Assert.That(validation.For("Rate").Single().TextKey, Is.EqualTo("Forms.TooFast"));
                Assert.That(validation.For("Nothing"), Is.Empty);
            });
        }

        /// <summary>
        /// A message travels as a key and its arguments, never as a sentence: a validator that
        /// answers with text has decided what language the operator reads, from the wrong side of
        /// the wire.
        /// </summary>
        [Test]
        public void AMessageTravelsAsAKeyAndItsArgumentsTest()
        {
            var issue = new FormIssue("OnTime", "Forms.OverCycle", FormSeverity.Error, "30", "20");

            var back = MemoryPackSerializer.Deserialize<FormValidation>(
                MemoryPackSerializer.Serialize(FormValidation.Of(issue)))!;

            Assert.Multiple(() =>
            {
                Assert.That(back.Issues.Single().TextKey, Is.EqualTo("Forms.OverCycle"));
                Assert.That(back.Issues.Single().Arguments, Is.EqualTo(new[] { "30", "20" }));
            });
        }

        #endregion
    }
}
