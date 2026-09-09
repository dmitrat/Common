using System.Linq;
using NUnit.Framework;
using OutWit.Common.Forms.Model;
using OutWit.Common.Forms.Utils;

namespace OutWit.Common.Forms.Tests
{
    /// <summary>
    /// Where the fields go.
    /// </summary>
    /// <remarks>
    /// A form says how many columns it wants and how much of them each field takes; this is the
    /// arithmetic that turns those two numbers into placements. It lives here rather than in each
    /// renderer because two renderers that afford the same number of columns must lay the same
    /// form out the same way — otherwise the browser and the desktop are two forms.
    /// </remarks>
    [TestFixture]
    public class FormLayoutTests
    {
        #region Tools

        private static FormField Field(string key, int span = 1)
        {
            return new FormField(key, $"Forms.{key}", FormFieldKind.Text) { Span = span };
        }

        private static FormGroup Group(int columns, params FormField[] fields)
        {
            var group = new FormGroup("Group", "Forms.Group") { Columns = columns };

            foreach (var field in fields)
                group.Fields.Add(field);

            return group;
        }

        private static (int Row, int Column, int Span) At(
            System.Collections.Generic.IReadOnlyList<FormPlacement<FormField>> placements, string key)
        {
            var placement = placements.First(one => one.Item.Key == key);

            return (placement.Row, placement.Column, placement.Span);
        }

        #endregion

        #region Tests

        /// <summary>
        /// A form that says nothing about columns is a field under a field, which is what every
        /// form was before there was anything to say.
        /// </summary>
        [Test]
        public void AFormThatSaysNothingIsOneColumnTest()
        {
            var group = Group(0, Field("A"), Field("B"));

            var placements = group.Placements();

            Assert.Multiple(() =>
            {
                Assert.That(group.ColumnCount(), Is.EqualTo(1));
                Assert.That(At(placements, "A").Row, Is.EqualTo(0));
                Assert.That(At(placements, "B").Row, Is.EqualTo(1));
            });
        }

        /// <summary>Left to right, then down.</summary>
        [Test]
        public void FieldsFillARowBeforeStartingTheNextTest()
        {
            var placements = Group(2, Field("A"), Field("B"), Field("C")).Placements();

            Assert.Multiple(() =>
            {
                Assert.That(At(placements, "A"), Is.EqualTo((0, 0, 1)));
                Assert.That(At(placements, "B"), Is.EqualTo((0, 1, 1)));
                Assert.That(At(placements, "C"), Is.EqualTo((1, 0, 1)));
            });
        }

        /// <summary>
        /// A field that asks for room gets it, and what follows starts underneath.
        /// </summary>
        [Test]
        public void AFieldThatAsksForRoomTakesItTest()
        {
            var placements = Group(3, Field("A", span: 2), Field("B"), Field("C")).Placements();

            Assert.Multiple(() =>
            {
                Assert.That(At(placements, "A"), Is.EqualTo((0, 0, 2)));
                Assert.That(At(placements, "B"), Is.EqualTo((0, 2, 1)));
                Assert.That(At(placements, "C"), Is.EqualTo((1, 0, 1)));
            });
        }

        /// <summary>
        /// A field that does not fit in what is left of a row starts the next one rather than being
        /// broken across two — and the gap it leaves is left empty. Pulling a later, narrower field
        /// forward to fill it would put the questions in an order nobody wrote, and the order a
        /// form asks things in is part of what it means.
        /// </summary>
        [Test]
        public void AFieldThatDoesNotFitStartsTheNextRowTest()
        {
            var placements = Group(3, Field("A"), Field("B", span: 3), Field("C")).Placements();

            Assert.Multiple(() =>
            {
                Assert.That(At(placements, "A"), Is.EqualTo((0, 0, 1)));
                Assert.That(At(placements, "B"), Is.EqualTo((1, 0, 3)),
                    "it did not fit beside A, and it was not cut in half either");
                Assert.That(At(placements, "C"), Is.EqualTo((2, 0, 1)),
                    "and C stays after B, rather than being pulled up into the gap");
            });
        }

        /// <summary>
        /// A field cannot ask for more than the group has. Three columns of two is both of them,
        /// not a third column brought into existence for one field.
        /// </summary>
        [Test]
        public void AFieldCannotAskForMoreThanThereIsTest()
        {
            var placements = Group(2, Field("A", span: 3), Field("B")).Placements();

            Assert.Multiple(() =>
            {
                Assert.That(At(placements, "A"), Is.EqualTo((0, 0, 2)));
                Assert.That(At(placements, "B"), Is.EqualTo((1, 0, 1)));
                Assert.That(placements.RowCount(), Is.EqualTo(2));
            });
        }

        /// <summary>
        /// A renderer with no room lays the same form out in one column, and nothing about the form
        /// has to change for it: spans collapse, and the order is what it always was.
        /// </summary>
        [Test]
        public void ANarrowRendererGetsOneColumnOfTheSameFormTest()
        {
            var group = Group(3, Field("A", span: 2), Field("B"), Field("C"));

            var placements = group.Contents().Placements(columns: 1);

            Assert.Multiple(() =>
            {
                Assert.That(At(placements, "A"), Is.EqualTo((0, 0, 1)));
                Assert.That(At(placements, "B"), Is.EqualTo((1, 0, 1)));
                Assert.That(At(placements, "C"), Is.EqualTo((2, 0, 1)));
            });
        }

        /// <summary>
        /// The field a group is switched by is not laid out among its contents — it is the group's
        /// own on and off, and a column left for it would be a hole where the switch is drawn.
        /// </summary>
        [Test]
        public void TheGroupsOwnSwitchIsNotPlacedAmongItsFieldsTest()
        {
            var group = Group(2, Field("On"), Field("A"), Field("B"));

            group.SwitchKey = "On";

            var placements = group.Placements();

            Assert.Multiple(() =>
            {
                Assert.That(placements.Select(one => one.Item.Key), Is.EquivalentTo(new[] { "A", "B" }));
                Assert.That(At(placements, "A"), Is.EqualTo((0, 0, 1)));
                Assert.That(At(placements, "B"), Is.EqualTo((0, 1, 1)));
            });
        }

        /// <summary>
        /// The same arithmetic one level up: a form of four short sections drawn in two columns is
        /// wider than it is tall, and drawn in one is taller than the screen. Which it should be is
        /// something the form knows.
        /// </summary>
        [Test]
        public void SectionsFlowIntoTheFormsColumnsTest()
        {
            var schema = new FormSchema("Test", "Forms.Title")
            {
                Columns = 2,
                Groups =
                {
                    new FormGroup("A", "Forms.A"),
                    new FormGroup("B", "Forms.B"),
                    new FormGroup("Wide", "Forms.Wide") { Span = 2 },
                    new FormGroup("C", "Forms.C")
                }
            };

            var placements = schema.Placements();

            (int, int, int) At(string key)
            {
                var one = placements.First(placement => placement.Item.Key == key);

                return (one.Row, one.Column, one.Span);
            }

            Assert.Multiple(() =>
            {
                Assert.That(At("A"), Is.EqualTo((0, 0, 1)));
                Assert.That(At("B"), Is.EqualTo((0, 1, 1)));
                Assert.That(At("Wide"), Is.EqualTo((1, 0, 2)));
                Assert.That(At("C"), Is.EqualTo((2, 0, 1)));
            });
        }

        /// <summary>
        /// Filled downwards, the first column is finished before the second is begun. Four sections
        /// in two columns are two and two — which is what puts a short section under another short
        /// one instead of beside a tall one.
        /// </summary>
        [Test]
        public void FilledDownwardsTheFirstColumnIsFinishedFirstTest()
        {
            var schema = new FormSchema("Test", "Forms.Title")
            {
                Columns = 2,
                Flow = FormFlow.Columns,
                Groups =
                {
                    new FormGroup("A", "Forms.A"),
                    new FormGroup("B", "Forms.B"),
                    new FormGroup("C", "Forms.C"),
                    new FormGroup("D", "Forms.D")
                }
            };

            var placements = schema.Placements();

            (int, int) At(string key)
            {
                var one = placements.First(placement => placement.Item.Key == key);

                return (one.Row, one.Column);
            }

            Assert.Multiple(() =>
            {
                Assert.That(At("A"), Is.EqualTo((0, 0)));
                Assert.That(At("B"), Is.EqualTo((1, 0)));
                Assert.That(At("C"), Is.EqualTo((0, 1)));
                Assert.That(At("D"), Is.EqualTo((1, 1)));
            });
        }

        /// <summary>
        /// An odd one out goes at the bottom of the first column: five in two columns is three and
        /// two, which is as even as counting allows.
        /// </summary>
        [Test]
        public void TheOddOneOutGoesAtTheBottomOfTheFirstColumnTest()
        {
            var placements = new[] { "A", "B", "C", "D", "E" }.Stacked(columns: 2);

            Assert.Multiple(() =>
            {
                Assert.That(placements[2], Is.EqualTo(new FormPlacement<string>("C", 2, 0, 1)));
                Assert.That(placements[3], Is.EqualTo(new FormPlacement<string>("D", 0, 1, 1)));
                Assert.That(placements.RowCount(), Is.EqualTo(3));
            });
        }

        /// <summary>An empty group is no rows, not one empty one.</summary>
        [Test]
        public void AnEmptyGroupIsNoRowsTest()
        {
            Assert.That(Group(2).Placements().RowCount(), Is.EqualTo(0));
        }

        #endregion
    }
}
