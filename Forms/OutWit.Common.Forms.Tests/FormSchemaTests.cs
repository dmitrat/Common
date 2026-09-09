using System.Linq;
using MemoryPack;
using NUnit.Framework;
using OutWit.Common.Forms.Model;
using OutWit.Common.Forms.Utils;

namespace OutWit.Common.Forms.Tests
{
    /// <summary>
    /// The schema as a whole: what it can be asked, and that it survives the wire.
    /// </summary>
    [TestFixture]
    public class FormSchemaTests
    {
        #region Tools

        /// <summary>
        /// A form with everything awkward in it: tabs, a nested group, a group switch, a condition
        /// and an option that comes and goes.
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
                }
            };

            var diary = new FormField("Diary", "Forms.Diary", FormFieldKind.Boolean)
            {
                DefaultValue = "false"
            };

            var kind = new FormField("Diary.Kind", "Forms.Diary.Kind", FormFieldKind.Choice)
            {
                EnabledWhen = FormCondition.On("Diary", FormOperator.IsTrue),
                Options =
                {
                    new FormOption("Buttons", "Forms.Diary.Buttons"),
                    new FormOption("Voice", "Forms.Diary.Voice")
                    {
                        VisibleWhen = FormCondition.On("Mode", FormOperator.Equals, "Event")
                    }
                }
            };

            var recording = new FormGroup("Recording", "Forms.Recording")
            {
                Fields = { mode }
            };

            var diaryGroup = new FormGroup("Diary", "Forms.Diary")
            {
                SwitchKey = "Diary",
                Fields = { diary, kind }
            };

            var tabs = new FormGroup("Tabs", "", FormGroupKind.Tabs)
            {
                Groups =
                {
                    new FormGroup("Tab.Basic", "Forms.Basic", FormGroupKind.Tab) { Groups = { recording } },
                    new FormGroup("Tab.Diary", "Forms.DiaryTab", FormGroupKind.Tab) { Groups = { diaryGroup } }
                }
            };

            return new FormSchema("Test.Form", "Forms.Title") { Groups = { tabs } };
        }

        #endregion

        #region Tests

        /// <summary>
        /// Every field, however deep. A renderer that could only see the top level would draw a
        /// third of a form and nobody would know which third.
        /// </summary>
        [Test]
        public void EveryFieldIsFoundHoweverDeepItIsTest()
        {
            var keys = Schema().Fields().Select(field => field.Key).ToList();

            Assert.That(keys, Is.EquivalentTo(new[] { "Mode", "Diary", "Diary.Kind" }));
        }

        [Test]
        public void AFieldIsFoundByItsKeyTest()
        {
            Assert.That(Schema().Field("Diary.Kind")?.Kind, Is.EqualTo(FormFieldKind.Choice));
        }

        /// <summary>
        /// Values start complete, at the schema's defaults, and say that is where they came from —
        /// so nothing downstream has to guess what an absent value meant.
        /// </summary>
        [Test]
        public void ValuesStartAtTheDefaultsAndSaySoTest()
        {
            var values = FormValues.Of(Schema());

            Assert.Multiple(() =>
            {
                Assert.That(values.Text("Mode"), Is.EqualTo("Holter"));
                Assert.That(values.Get("Mode")!.State, Is.EqualTo(FormValueState.Default));

                // A field with no default is not "empty": it is a thing nobody has answered.
                Assert.That(values.Get("Diary.Kind")!.State, Is.EqualTo(FormValueState.NotAvailable));
            });
        }

        /// <summary>
        /// The switch of a group is not drawn among its contents: it is the group's own on and off.
        /// </summary>
        [Test]
        public void AGroupSwitchIsNotOneOfItsFieldsTest()
        {
            var group = Schema().AllGroups().First(one => one.Key == "Diary");

            Assert.Multiple(() =>
            {
                Assert.That(group.Switch()?.Key, Is.EqualTo("Diary"));
                Assert.That(group.Contents().Select(field => field.Key), Is.EquivalentTo(new[] { "Diary.Kind" }));
            });
        }

        /// <summary>
        /// An option can come and go with another field's value, which is how a device offers a
        /// choice only in some configurations.
        /// </summary>
        [Test]
        public void AnOptionCanDependOnAnotherFieldTest()
        {
            var voice = Schema().Field("Diary.Kind")!.Options.First(option => option.Value == "Voice");

            var holter = new FormValues().Set("Mode", "Holter");
            var evented = new FormValues().Set("Mode", "Event");

            Assert.Multiple(() =>
            {
                Assert.That(voice.IsVisible(holter), Is.False);
                Assert.That(voice.IsVisible(evented), Is.True);
            });
        }

        /// <summary>
        /// The one structural mistake a renderer cannot survive: two fields under one key. The
        /// second would silently overwrite the first, in the values and in every condition.
        /// </summary>
        [Test]
        public void TwoFieldsUnderOneKeyAreReportedTest()
        {
            var schema = Schema();

            schema.Groups[0].Groups[0].Fields.Add(new FormField("Mode", "Forms.Mode", FormFieldKind.Text));

            Assert.That(schema.DuplicateKeys(), Is.EquivalentTo(new[] { "Mode" }));
        }

        /// <summary>
        /// A mirror is the same field drawn again, not a second field: it is not a duplicate, it
        /// takes no default of its own, and one with no original is a key nobody declared.
        /// </summary>
        [Test]
        public void AMirrorIsTheSameFieldAgainTest()
        {
            var schema = Schema();

            schema.Groups[0].Groups[0].Fields.Add(new FormField("Mode", "Forms.Mode", FormFieldKind.Choice)
            {
                IsMirror = true
            });

            Assert.Multiple(() =>
            {
                Assert.That(schema.DuplicateKeys(), Is.Empty, "a mirror is not a second field");
                Assert.That(schema.UnknownKeys(), Is.Empty, "it has an original");
                Assert.That(FormValues.Of(schema).Text("Mode"), Is.EqualTo("Holter"),
                    "the original's default stands, wherever the mirror comes in the tree");
            });

            schema.Groups[0].Groups[0].Fields.Add(new FormField("Ghost", "Forms.Ghost", FormFieldKind.Text)
            {
                IsMirror = true
            });

            Assert.That(schema.UnknownKeys(), Is.EquivalentTo(new[] { "Ghost" }), "a mirror of nothing");
        }

        /// <summary>
        /// A mirror drawn before its original is still not the field: asking for the key gives the
        /// original, wherever it comes in the tree, because that is the one with the options and
        /// the default. A renderer handed the mirror would draw a choice with nothing to choose.
        /// </summary>
        [Test]
        public void AMirrorBeforeItsOriginalIsStillNotTheFieldTest()
        {
            var schema = Schema();

            // The tab's own fields come before those of the sections inside it, so this mirror is
            // met first.
            schema.Groups[0].Groups[0].Fields.Add(new FormField("Mode", "Forms.Mode", FormFieldKind.Choice)
            {
                IsMirror = true
            });

            var field = schema.Field("Mode")!;

            Assert.Multiple(() =>
            {
                Assert.That(field.IsMirror, Is.False);
                Assert.That(field.Options, Has.Count.EqualTo(2));
                Assert.That(field.DefaultValue, Is.EqualTo("Holter"));
            });
        }

        /// <summary>
        /// A group switched by a field it does not hold is drawn with no switch at all — the same
        /// silence as a condition on a field nobody declared, and reported the same way.
        /// </summary>
        [Test]
        public void AGroupSwitchedByAFieldItDoesNotHoldIsReportedTest()
        {
            var schema = Schema();

            schema.AllGroups().First(group => group.Key == "Recording").SwitchKey = "Diary";

            Assert.That(schema.UnknownKeys(), Is.EquivalentTo(new[] { "Diary" }),
                "declared elsewhere on the form, which is as missing as declared nowhere");
        }

        /// <summary>
        /// And a condition that watches a field nobody declared: it can never be true, so whatever
        /// it guards never opens — which looks exactly like a bug in the renderer.
        /// </summary>
        [Test]
        public void AConditionOnAFieldNobodyDeclaredIsReportedTest()
        {
            var schema = Schema();

            schema.Field("Mode")!.VisibleWhen = FormCondition.On("Ghost", FormOperator.IsTrue);

            Assert.That(schema.UnknownKeys(), Is.EquivalentTo(new[] { "Ghost" }));
        }

        /// <summary>
        /// And the condition that marks an entry of a list as on is a condition like the others:
        /// one on a field nobody declared is reported, not silently never met.
        /// </summary>
        [Test]
        public void AnEntrysStateConditionIsCheckedTooTest()
        {
            var schema = Schema();

            schema.Groups.Add(new FormGroup("Episodes", "Forms.Episodes", FormGroupKind.List)
            {
                Groups =
                {
                    new FormGroup("Brady", "Forms.Brady", FormGroupKind.Entry)
                    {
                        ActiveWhen = FormCondition.On("Ghost", FormOperator.IsTrue)
                    }
                }
            });

            Assert.That(schema.UnknownKeys(), Is.EquivalentTo(new[] { "Ghost" }));
        }

        /// <summary>
        /// An entry's summary and its state condition travel with it — across a copy, and across
        /// the wire. A field that quietly stays behind on either is a list of entries with no
        /// second line.
        /// </summary>
        [Test]
        public void AnEntryKeepsItsSummaryAndStateAcrossACopyAndTheWireTest()
        {
            var entry = new FormGroup("Brady", "Forms.Brady", FormGroupKind.Entry)
            {
                SwitchKey = "Brady.On",
                SummaryKey = "Forms.BradySummary",
                ActiveWhen = FormCondition.On("Brady.On", FormOperator.IsTrue),
                Fields = { new FormField("Brady.On", "Forms.Detect", FormFieldKind.Boolean) }
            };

            var copy = entry.Clone();
            var back = MemoryPackSerializer.Deserialize<FormGroup>(MemoryPackSerializer.Serialize(entry))!;

            Assert.Multiple(() =>
            {
                Assert.That(copy.Is(entry), Is.True);
                Assert.That(back.Is(entry), Is.True);
                Assert.That(back.SummaryKey, Is.EqualTo("Forms.BradySummary"));
                Assert.That(back.ActiveWhen?.Key, Is.EqualTo("Brady.On"));

                copy.ActiveWhen!.Key = "Something else";
                Assert.That(entry.ActiveWhen!.Key, Is.EqualTo("Brady.On"), "the original is untouched");
                Assert.That(copy.Is(entry), Is.False, "and the difference is seen");
            });
        }

        /// <summary>
        /// It crosses the wire whole. This is the point of the package: the description is data, so
        /// the thing that declares a form and the thing that draws it need not be the same process,
        /// the same machine or the same language.
        /// </summary>
        [Test]
        public void ASchemaSurvivesTheWireTest()
        {
            var schema = Schema();

            var bytes = MemoryPackSerializer.Serialize(schema);
            var back = MemoryPackSerializer.Deserialize<FormSchema>(bytes)!;

            Assert.Multiple(() =>
            {
                Assert.That(back.Is(schema), Is.True, "field for field");
                Assert.That(back.Field("Diary.Kind")!.EnabledWhen!.Key, Is.EqualTo("Diary"),
                    "conditions included, which are the part that would be missed");
            });
        }

        [Test]
        public void ValuesSurviveTheWireTest()
        {
            var values = FormValues.Of(Schema()).Set("Mode", "Event").Set("Diary", "true");

            var back = MemoryPackSerializer.Deserialize<FormValues>(MemoryPackSerializer.Serialize(values))!;

            Assert.Multiple(() =>
            {
                Assert.That(back.Is(values), Is.True);
                Assert.That(back.Get("Mode")!.State, Is.EqualTo(FormValueState.Set),
                    "and where each value came from travels with it");
            });
        }

        #endregion
    }
}
