using NUnit.Framework;
using OutWit.Common.Forms.Model;
using OutWit.Common.Forms.Utils;

namespace OutWit.Common.Forms.Tests
{
    /// <summary>
    /// The one piece of logic a form is allowed to carry, and therefore the piece that has to be
    /// right: whether a control is live.
    /// </summary>
    [TestFixture]
    public class FormConditionTests
    {
        #region Tools

        private static FormValues Values(params (string Key, string Value)[] values)
        {
            var result = new FormValues();

            foreach (var (key, value) in values)
                result.Set(key, value);

            return result;
        }

        #endregion

        #region Tests

        /// <summary>
        /// No condition means always. An absent <c>EnabledWhen</c> that disabled its field would
        /// make every simple form dead on arrival.
        /// </summary>
        [Test]
        public void NoConditionIsAlwaysMetTest()
        {
            Assert.That(((FormCondition?)null).IsMet(new FormValues()), Is.True);
        }

        [Test]
        public void EqualsComparesTheFieldItNamesTest()
        {
            var condition = FormCondition.On("Mode", FormOperator.Equals, "Holter");

            Assert.Multiple(() =>
            {
                Assert.That(condition.IsMet(Values(("Mode", "Holter"))), Is.True);
                Assert.That(condition.IsMet(Values(("Mode", "Event"))), Is.False);
            });
        }

        /// <remarks>
        /// The two sides of a wire write enumeration members with whatever case their language
        /// prefers, and a group that silently stopped opening over a capital letter would be very
        /// hard to see.
        /// </remarks>
        [Test]
        public void CaseDoesNotDecideWhetherAGroupOpensTest()
        {
            var condition = FormCondition.On("Mode", FormOperator.Equals, "holter");

            Assert.That(condition.IsMet(Values(("Mode", "Holter"))), Is.True);
        }

        [Test]
        public void BooleansAreReadAsBooleansTest()
        {
            var on = FormCondition.On("Diary", FormOperator.IsTrue);
            var off = FormCondition.On("Diary", FormOperator.IsFalse);

            Assert.Multiple(() =>
            {
                Assert.That(on.IsMet(Values(("Diary", "true"))), Is.True);
                Assert.That(on.IsMet(Values(("Diary", "false"))), Is.False);
                Assert.That(off.IsMet(Values(("Diary", "false"))), Is.True);

                // Nothing at all is not true, which is what an empty form should read as.
                Assert.That(on.IsMet(new FormValues()), Is.False);
                Assert.That(off.IsMet(new FormValues()), Is.True);
            });
        }

        [Test]
        public void OneOfSeveralIsInTest()
        {
            var condition = FormCondition.On("Cable", FormOperator.In, "Cable3Lead", "Cable5Lead");

            Assert.Multiple(() =>
            {
                Assert.That(condition.IsMet(Values(("Cable", "Cable5Lead"))), Is.True);
                Assert.That(condition.IsMet(Values(("Cable", "Cable10Lead"))), Is.False);
            });
        }

        [Test]
        public void NumbersCompareAsNumbersTest()
        {
            var condition = FormCondition.On("Rate", FormOperator.GreaterThan, "250");

            Assert.Multiple(() =>
            {
                Assert.That(condition.IsMet(Values(("Rate", "1000"))), Is.True,
                    "1000 is more than 250, which it would not be as text");
                Assert.That(condition.IsMet(Values(("Rate", "125"))), Is.False);
            });
        }

        /// <remarks>
        /// A comparison that cannot be answered is false. It disables a control for a reason nobody
        /// can see, which is bad — and the alternative, treating unanswerable as true, enables a
        /// control that the authority will then reject, which is worse.
        /// </remarks>
        [Test]
        public void AComparisonThatCannotBeAnsweredIsNotMetTest()
        {
            var condition = FormCondition.On("Rate", FormOperator.GreaterThan, "250");

            Assert.Multiple(() =>
            {
                Assert.That(condition.IsMet(Values(("Rate", "quite fast"))), Is.False);
                Assert.That(condition.IsMet(new FormValues()), Is.False);
            });
        }

        [Test]
        public void SomethingOrNothingIsIsSetTest()
        {
            var set = FormCondition.On("Id", FormOperator.IsSet);

            Assert.Multiple(() =>
            {
                Assert.That(set.IsMet(Values(("Id", "24080012"))), Is.True);
                Assert.That(set.IsMet(Values(("Id", ""))), Is.False);
                Assert.That(set.IsMet(new FormValues()), Is.False);
            });
        }

        [Test]
        public void AllAndAnyJoinTheirChildrenTest()
        {
            var all = FormCondition.All(
                FormCondition.On("Mode", FormOperator.Equals, "Holter"),
                FormCondition.On("Diary", FormOperator.IsTrue));

            var any = FormCondition.Any(
                FormCondition.On("Mode", FormOperator.Equals, "Event"),
                FormCondition.On("Diary", FormOperator.IsTrue));

            var values = Values(("Mode", "Holter"), ("Diary", "false"));

            Assert.Multiple(() =>
            {
                Assert.That(all.IsMet(values), Is.False);
                Assert.That(any.IsMet(values), Is.False);
                Assert.That(any.IsMet(Values(("Mode", "Event"), ("Diary", "false"))), Is.True);
            });
        }

        /// <summary>
        /// A condition with a comparison of its own and children joins them the way it joins the
        /// children, so that one shape does not mean two things depending on how it was built.
        /// </summary>
        [Test]
        public void AConditionWithBothJoinsThemTheSameWayTest()
        {
            var condition = FormCondition.On("Mode", FormOperator.Equals, "Holter");

            condition.Junction = FormJunction.And;
            condition.Conditions.Add(FormCondition.On("Diary", FormOperator.IsTrue));

            Assert.Multiple(() =>
            {
                Assert.That(condition.IsMet(Values(("Mode", "Holter"), ("Diary", "true"))), Is.True);
                Assert.That(condition.IsMet(Values(("Mode", "Holter"), ("Diary", "false"))), Is.False);
            });
        }

        /// <summary>
        /// A field is dead when it is read-only, whatever its condition says: the two are different
        /// statements — never editable, and not editable right now.
        /// </summary>
        [Test]
        public void AReadOnlyFieldIsNeverEnabledTest()
        {
            var field = new FormField("Serial", "Forms.Serial", FormFieldKind.Text) { IsReadOnly = true };

            Assert.That(field.IsEnabled(new FormValues()), Is.False);
        }

        /// <summary>
        /// An entry is on when its condition says so; failing a condition, when its switch is; and
        /// failing both, always. One rule, kept here, so that no renderer reads it differently.
        /// </summary>
        [Test]
        public void AnEntryIsOnByItsConditionThenItsSwitchThenAlwaysTest()
        {
            var conditioned = new FormGroup("Symptom", "Forms.Symptom", FormGroupKind.Entry)
            {
                SwitchKey = "Symptom.On",
                ActiveWhen = FormCondition.On("Symptom.Source", FormOperator.Equals, "Button"),
                Fields = { new FormField("Symptom.On", "Forms.On", FormFieldKind.Boolean) }
            };

            var switched = new FormGroup("Brady", "Forms.Brady", FormGroupKind.Entry)
            {
                SwitchKey = "Brady.On",
                Fields = { new FormField("Brady.On", "Forms.On", FormFieldKind.Boolean) }
            };

            var plain = new FormGroup("Pause", "Forms.Pause", FormGroupKind.Entry);

            Assert.Multiple(() =>
            {
                Assert.That(conditioned.IsActive(Values(("Symptom.Source", "Button"), ("Symptom.On", "false"))),
                    Is.True, "the condition decides, not the switch");
                Assert.That(conditioned.IsActive(Values(("Symptom.Source", "Timer"), ("Symptom.On", "true"))),
                    Is.False);

                Assert.That(switched.IsActive(Values(("Brady.On", "true"))), Is.True);
                Assert.That(switched.IsActive(Values(("Brady.On", "false"))), Is.False);
                Assert.That(switched.IsActive(new FormValues()), Is.False, "nothing at all is not on");

                Assert.That(plain.IsActive(new FormValues()), Is.True);
            });
        }

        #endregion
    }
}
