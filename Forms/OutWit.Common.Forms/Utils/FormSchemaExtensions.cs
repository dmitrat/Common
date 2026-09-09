using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Forms.Model;

namespace OutWit.Common.Forms.Utils
{
    /// <summary>
    /// Walking a schema, which is a tree.
    /// </summary>
    public static class FormSchemaExtensions
    {
        #region Functions

        /// <summary>Every field of the form, whatever it is nested in.</summary>
        public static IEnumerable<FormField> Fields(this FormSchema me)
        {
            return me.Groups.SelectMany(group => group.Fields());
        }

        /// <summary>Every field of this group and of the groups under it.</summary>
        public static IEnumerable<FormField> Fields(this FormGroup me)
        {
            foreach (var field in me.Fields)
                yield return field;

            foreach (var field in me.Groups.SelectMany(group => group.Fields()))
                yield return field;
        }

        /// <summary>Every group of the form, the nested ones included.</summary>
        public static IEnumerable<FormGroup> AllGroups(this FormSchema me)
        {
            return me.Groups.SelectMany(group => group.AllGroups());
        }

        public static IEnumerable<FormGroup> AllGroups(this FormGroup me)
        {
            yield return me;

            foreach (var group in me.Groups.SelectMany(one => one.AllGroups()))
                yield return group;
        }

        /// <summary>
        /// The field with this key, or nothing. Where the key is declared once and mirrored
        /// elsewhere, the original — whichever comes first in the tree — since it is the one that
        /// carries the kind, the options and the default.
        /// </summary>
        public static FormField? Field(this FormSchema me, string key)
        {
            return me.Fields()
                .Where(field => field.Key == key)
                .OrderBy(field => field.IsMirror)
                .FirstOrDefault();
        }

        /// <summary>
        /// The field a group is switched by, where it has one.
        /// </summary>
        public static FormField? Switch(this FormGroup me)
        {
            return me.SwitchKey == null
                ? null
                : me.Fields.FirstOrDefault(field => field.Key == me.SwitchKey);
        }

        /// <summary>
        /// The fields of a group that are drawn among its contents — which is all of them except
        /// the one drawn as the group's own switch.
        /// </summary>
        public static IEnumerable<FormField> Contents(this FormGroup me)
        {
            return me.SwitchKey == null
                ? me.Fields
                : me.Fields.Where(field => field.Key != me.SwitchKey);
        }

        /// <summary>
        /// Keys declared by two fields, which is the one structural mistake a renderer cannot
        /// survive: two fields under one key means the second silently overwrites the first, both
        /// in the values and in every condition that looks at it. A mirror is not a second field:
        /// it is the same one drawn again, and is counted with its original.
        /// </summary>
        /// <remarks>
        /// Offered rather than enforced. A schema arrives from somewhere else, frequently from
        /// another process, and refusing to draw it is a worse answer than drawing it and saying
        /// what is wrong with it.
        /// </remarks>
        public static IReadOnlyList<string> DuplicateKeys(this FormSchema me)
        {
            return me.Fields()
                .Where(field => !field.IsMirror)
                .GroupBy(field => field.Key)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
        }

        /// <summary>
        /// Keys spoken of that no field declares: a condition on a field nobody has, a mirror of
        /// a field nobody has, or a group switched by a field it does not hold.
        /// </summary>
        /// <remarks>
        /// Each of them fails silently otherwise. A condition on a missing field is always false —
        /// nothing can equal a value that is never there — so whatever it guards never opens; a
        /// group whose switch is not among its own fields is drawn with no switch at all. Both
        /// look exactly like a bug in the renderer.
        /// </remarks>
        public static IReadOnlyList<string> UnknownKeys(this FormSchema me)
        {
            var known = me.Fields().Where(field => !field.IsMirror).Select(field => field.Key).ToHashSet();

            var mirrored = me.Fields().Where(field => field.IsMirror).Select(field => field.Key);

            // A switch is a field of its own group, which is where Switch() looks; one declared
            // anywhere else is as missing as one declared nowhere.
            var switched = me.AllGroups()
                .Where(group => group.SwitchKey != null && group.Switch() == null)
                .Select(group => group.SwitchKey!);

            var looked = me.AllGroups()
                .SelectMany(group => new[] { group.EnabledWhen, group.VisibleWhen, group.ActiveWhen })
                .Concat(me.Fields().SelectMany(field => new[] { field.EnabledWhen, field.VisibleWhen }))
                .Concat(me.Fields().SelectMany(field => field.Options).Select(option => option.VisibleWhen))
                .SelectMany(Keys)
                .Concat(mirrored)
                .Where(key => !known.Contains(key))
                .Concat(switched);

            return looked.Distinct().ToList();
        }

        #endregion

        #region Tools

        private static IEnumerable<string> Keys(FormCondition? condition)
        {
            if (condition == null)
                yield break;

            if (condition.Key != null)
                yield return condition.Key;

            foreach (var key in condition.Conditions.SelectMany(Keys))
                yield return key;
        }

        #endregion
    }
}
