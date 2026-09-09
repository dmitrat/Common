using System.Linq;
using NUnit.Framework;
using OutWit.Common.Forms.MessagePack;
using OutWit.Common.Forms.Model;
using OutWit.Common.Forms.Utils;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.NUnit;

namespace OutWit.Common.Forms.Tests
{
    /// <summary>
    /// The same form, carried by a system that speaks MemoryPack and by one that speaks
    /// MessagePack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the fixture that decides whether the package is general or private. WitRPC speaks
    /// MemoryPack unasked; a product built on MessagePack — as the Norav one is, where every model
    /// carries <c>[MessagePackObject]</c> — has to be able to carry these types too, and without
    /// either side editing the model.
    /// </para>
    /// <para>
    /// What it takes is one call at that product's startup. Nothing here is a dependency of the
    /// library itself.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class FormWireTests
    {
        #region Setup

        /// <summary>
        /// What a MessagePack-standardised product does once.
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            FormsMessagePack.Register();
        }

        #endregion

        #region Tools

        /// <summary>
        /// A form with everything awkward in it: tabs, a nested group, a group switch, a two-level
        /// condition and an option that comes and goes.
        /// </summary>
        private static FormSchema Schema()
        {
            var mode = new FormField("Mode", "Forms.Mode", FormFieldKind.Choice)
            {
                DefaultValue = "Holter",
                Options =
                {
                    new FormOption("Holter", "Forms.Mode.Holter"),
                    new FormOption("Event", "Forms.Mode.Event")
                    {
                        VisibleWhen = FormCondition.On("Licensed", FormOperator.IsTrue)
                    }
                }
            };

            var limit = new FormField("Brady.Limit", "Forms.Brady.Limit", FormFieldKind.Number)
            {
                Minimum = 30,
                Maximum = 120,
                Step = 5,
                UnitKey = "Forms.Bpm",
                Presentation = FormPresentation.Inline,
                Span = 2,
                EnabledWhen = FormCondition.All(
                    FormCondition.On("Mode", FormOperator.Equals, "Event"),
                    FormCondition.On("Brady", FormOperator.IsTrue))
            };

            // The three kinds a patient's card needs and a recorder's settings do not: a date
            // with no time, a measurement whose unit belongs to the workstation, and a list of
            // free values with somewhere to take suggestions from.
            var born = new FormField("Born", "Forms.Born", FormFieldKind.Date);

            var weight = new FormField("Weight", "Forms.Weight", FormFieldKind.Quantity)
            {
                QuantityKey = "Weight",
                Maximum = 305
            };

            var drugs = new FormField("Drugs", "Forms.Drugs", FormFieldKind.Tokens)
            {
                SuggestionsKey = "Medications"
            };

            var brady = new FormField("Brady", "Forms.Brady", FormFieldKind.Boolean)
            {
                DefaultValue = "false"
            };

            var licensed = new FormField("Licensed", "Forms.Licensed", FormFieldKind.Boolean)
            {
                DefaultValue = "true",
                IsReadOnly = true
            };

            var detection = new FormGroup("Detection", "Forms.Detection")
            {
                Columns = 3,
                Span = 2,
                SwitchKey = "Brady",
                Fields = { brady, limit }
            };

            var tabs = new FormGroup("Tabs", "", FormGroupKind.Tabs)
            {
                Groups =
                {
                    new FormGroup("Tab.Recording", "Forms.Recording", FormGroupKind.Tab)
                    {
                        Fields = { mode, licensed, born, weight, drugs }
                    },
                    new FormGroup("Tab.Detection", "Forms.DetectionTab", FormGroupKind.Tab)
                    {
                        Groups = { detection }
                    }
                }
            };

            return new FormSchema("Patch.EventRecorder", "Forms.Title")
            {
                Version = "1",
                Groups = { tabs }
            };
        }

        #endregion

        #region MemoryPack

        /// <summary>The native wire carries the whole form, conditions and all.</summary>
        [Test]
        public void MemoryPackCarriesTheWholeFormTest()
        {
            var schema = Schema();

            var clone = schema.MemoryPackClone();

            Assert.Multiple(() =>
            {
                Assert.That(clone, Is.Not.Null);
                Assert.That(clone, Is.Not.SameAs(schema));
                Assert.That(clone, Was.EqualTo(schema));
            });
        }

        [Test]
        public void MemoryPackCarriesTheAnswersBackTest()
        {
            var values = FormValues.Of(Schema()).Set("Mode", "Event").Set("Brady", "true");

            Assert.That(values.MemoryPackClone(), Was.EqualTo(values));
        }

        #endregion

        #region MessagePack

        /// <summary>
        /// And so does a MessagePack-standardised system, through its own entry points, with no
        /// attribute of that format anywhere in the model.
        /// </summary>
        [Test]
        public void MessagePackCarriesTheWholeFormTest()
        {
            var schema = Schema();

            var clone = schema.MessagePackClone();

            Assert.Multiple(() =>
            {
                Assert.That(clone, Is.Not.Null);
                Assert.That(clone, Is.Not.SameAs(schema));
                Assert.That(clone, Was.EqualTo(schema));
            });
        }

        /// <summary>
        /// And the parts a shallow copy would lose: the nesting, the condition tree, the switch
        /// that points at another field, the option that depends on one.
        /// </summary>
        [Test]
        public void MessagePackCarriesWhatIsEasyToLoseTest()
        {
            var clone = Schema().MessagePackClone();

            Assert.Multiple(() =>
            {
                Assert.That(clone.Field("Brady.Limit")!.EnabledWhen!.Conditions, Has.Count.EqualTo(2),
                    "a two-level condition");

                Assert.That(clone.AllGroups().First(group => group.Key == "Detection").SwitchKey,
                    Is.EqualTo("Brady"), "a group switch, which is a key into another field");

                Assert.That(clone.Field("Mode")!.Options.First(option => option.Value == "Event").VisibleWhen,
                    Is.Not.Null, "an option that comes and goes");

                Assert.That(clone.Field("Brady.Limit")!.Step, Is.EqualTo(5), "and the numbers");
            });
        }

        /// <summary>
        /// The filled-in form goes back the same way — the direction that matters, since it is what
        /// the authority is asked about and what is finally sent to the device.
        /// </summary>
        [Test]
        public void MessagePackCarriesTheAnswersBackTest()
        {
            var values = FormValues.Of(Schema())
                .Set("Mode", "Event")
                .Set("Brady", "true")
                .Set("Brady.Limit", "45", FormValueState.Read);

            var clone = values.MessagePackClone();

            Assert.Multiple(() =>
            {
                Assert.That(clone, Was.EqualTo(values));
                Assert.That(clone.Get("Brady.Limit")!.State, Is.EqualTo(FormValueState.Read),
                    "including where each value came from");
            });
        }

        [Test]
        public void MessagePackCarriesWhatTheAuthoritySaidTest()
        {
            var validation = FormValidation.Of(
                new FormIssue("Brady.Limit", "Forms.OverCycle", FormSeverity.Error, "30", "20"),
                new FormIssue(null, "Forms.Unusual", FormSeverity.Warning));

            Assert.That(validation.MessagePackClone(), Was.EqualTo(validation));
        }

        /// <summary>
        /// Registering the form model does not make everything else serialisable.
        /// </summary>
        /// <remarks>
        /// The reason this package exists rather than a line of <c>ContractlessStandardResolver</c>
        /// in somebody's startup. A product on MessagePack marks its own models, and a model that
        /// has lost its attribute should fail loudly — not start travelling in a different shape
        /// because a form library once loosened the rules for everybody.
        /// </remarks>
        [Test]
        public void NothingElseBecomesSerialisableTest()
        {
            // Through the product's own entry point, which is where it would happen for real. That
            // entry point reports a failure by logging it and answering null rather than by
            // throwing, so this is what "refused" looks like from the outside.
            Assert.That(new Unattributed { Value = 1 }.ToMessagePackBytes(), Is.Null,
                "a type with no attributes and no formatter of its own is still not serialisable");
        }

        /// <summary>
        /// Whichever wire brought it, the form behaves the same: the conditions are evaluated from
        /// the data by the same code, in a desktop renderer or in a browser.
        /// </summary>
        /// <remarks>
        /// This is what the package is for. A second front end draws differently and decides
        /// nothing differently — the rule about when a field is live travelled with the form.
        /// </remarks>
        [Test]
        public void TheFormBehavesTheSameAfterEitherWireTest()
        {
            var schema = Schema();

            var throughMemoryPack = schema.MemoryPackClone();
            var throughMessagePack = schema.MessagePackClone();

            var values = new FormValues().Set("Mode", "Event").Set("Brady", "true");

            Assert.Multiple(() =>
            {
                Assert.That(throughMemoryPack.Field("Brady.Limit")!.IsEnabled(values), Is.True);
                Assert.That(throughMessagePack.Field("Brady.Limit")!.IsEnabled(values), Is.True);

                values.Set("Brady", "false");

                Assert.That(throughMemoryPack.Field("Brady.Limit")!.IsEnabled(values), Is.False);
                Assert.That(throughMessagePack.Field("Brady.Limit")!.IsEnabled(values), Is.False);
            });
        }

        #endregion

        #region Classes

        /// <summary>Somebody's model that forgot its attribute.</summary>
        private class Unattributed
        {
            public int Value { get; set; }
        }

        #endregion
    }
}
