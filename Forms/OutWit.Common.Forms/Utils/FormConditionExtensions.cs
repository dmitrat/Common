using System;
using System.Globalization;
using System.Linq;
using OutWit.Common.Forms.Model;

namespace OutWit.Common.Forms.Utils
{
    /// <summary>
    /// Answering a condition against what is currently in the form.
    /// </summary>
    /// <remarks>
    /// Everything here runs where the form is drawn, on every change, without asking anybody. That
    /// is the reason conditions are in the schema at all — see "The line it draws" in the
    /// package README.
    /// </remarks>
    public static class FormConditionExtensions
    {
        #region Functions

        /// <summary>
        /// Whether the condition holds. A condition that is not there holds: an absent
        /// <c>EnabledWhen</c> means "always".
        /// </summary>
        public static bool IsMet(this FormCondition? me, FormValues values)
        {
            if (me == null)
                return true;

            var own = me.Key == null ? (bool?)null : Compare(me, values.Text(me.Key));

            if (me.Conditions.Count == 0)
                return own ?? true;

            var children = me.Junction == FormJunction.Or
                ? me.Conditions.Any(condition => condition.IsMet(values))
                : me.Conditions.All(condition => condition.IsMet(values));

            if (own == null)
                return children;

            // A condition with both a comparison of its own and children joins them the same way it
            // joins the children — otherwise the same shape would mean two things depending on how
            // it was built.
            return me.Junction == FormJunction.Or ? own.Value || children : own.Value && children;
        }

        /// <summary>Whether this field is on the screen and can be changed right now.</summary>
        public static bool IsEnabled(this FormField me, FormValues values)
        {
            return !me.IsReadOnly && me.EnabledWhen.IsMet(values);
        }

        /// <summary>Whether this field is on the screen at all right now.</summary>
        public static bool IsVisible(this FormField me, FormValues values)
        {
            return me.VisibleWhen.IsMet(values);
        }

        /// <summary>Whether this group's contents can be changed right now.</summary>
        public static bool IsEnabled(this FormGroup me, FormValues values)
        {
            return me.EnabledWhen.IsMet(values);
        }

        /// <summary>Whether this group is on the screen at all right now.</summary>
        public static bool IsVisible(this FormGroup me, FormValues values)
        {
            return me.VisibleWhen.IsMet(values);
        }

        /// <summary>
        /// Whether an entry of a list is marked as on: what <see cref="FormGroup.ActiveWhen"/>
        /// says; its switch, where there is no condition; always, where there is neither.
        /// </summary>
        /// <remarks>
        /// Here rather than in each renderer for the same reason the conditions are: it is a rule
        /// about the form, it is answered on every change, and two renderers that read it
        /// differently show the same entry as on in one window and off in the next.
        /// </remarks>
        public static bool IsActive(this FormGroup me, FormValues values)
        {
            if (me.ActiveWhen != null)
                return me.ActiveWhen.IsMet(values);

            var @switch = me.Switch();

            return @switch == null || IsTrue(values.Text(@switch.Key));
        }

        /// <summary>Whether this option is offered at all right now.</summary>
        public static bool IsVisible(this FormOption me, FormValues values)
        {
            return me.VisibleWhen.IsMet(values);
        }

        #endregion

        #region Tools

        private static bool Compare(FormCondition condition, string? value)
        {
            switch (condition.Operator)
            {
                case FormOperator.None:
                    return true;

                case FormOperator.IsSet:
                    return !string.IsNullOrEmpty(value);

                case FormOperator.IsNotSet:
                    return string.IsNullOrEmpty(value);

                case FormOperator.IsTrue:
                    return IsTrue(value);

                case FormOperator.IsFalse:
                    return !IsTrue(value);

                case FormOperator.Equals:
                    return condition.Values.Count > 0 && Same(value, condition.Values[0]);

                case FormOperator.NotEquals:
                    return condition.Values.Count > 0 && !Same(value, condition.Values[0]);

                case FormOperator.In:
                    return condition.Values.Any(one => Same(value, one));

                case FormOperator.NotIn:
                    return !condition.Values.Any(one => Same(value, one));

                case FormOperator.GreaterThan:
                    return Numbers(value, condition, out var left, out var right) && left > right;

                case FormOperator.LessThan:
                    return Numbers(value, condition, out var less, out var limit) && less < limit;

                default:
                    return true;
            }
        }

        /// <remarks>
        /// Case-insensitive, because the two sides of a wire write enumeration members and booleans
        /// with whatever case their language prefers, and a form that silently stopped enabling a
        /// group over a capital letter would be very hard to see.
        /// </remarks>
        private static bool Same(string? value, string other)
        {
            return string.Equals(value ?? "", other ?? "", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrue(string? value)
        {
            return bool.TryParse(value, out var result) && result;
        }

        /// <remarks>
        /// A comparison against something that is not a number is not true and not false; it is a
        /// condition that cannot be answered, and answering "no" quietly disables a control for
        /// reasons nobody can see. So it is false here and the mistake shows up as a group that
        /// never opens, which is at least findable.
        /// </remarks>
        private static bool Numbers(string? value, FormCondition condition, out double left, out double right)
        {
            right = 0;

            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out left) &&
                   condition.Values.Count > 0 &&
                   double.TryParse(condition.Values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out right);
        }

        #endregion
    }
}
