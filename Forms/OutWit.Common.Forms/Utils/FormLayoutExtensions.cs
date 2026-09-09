using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Forms.Model;

namespace OutWit.Common.Forms.Utils
{
    /// <summary>
    /// Where a form's parts go: the arithmetic every renderer would otherwise write for itself,
    /// and get subtly differently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A form says how many columns its groups flow into, how many columns each group's fields flow
    /// into, and how many of them each thing takes. It does not say how wide anything is, where the
    /// labels go, or what happens when there is no room: those belong to whoever is drawing, and a
    /// form that tried to settle them would be settling them for a window it has never seen.
    /// </para>
    /// <para>
    /// So this is placement and nothing else — which column, which row, how wide — worked out from
    /// the number of columns the renderer has decided it can afford. Two renderers that afford the
    /// same number lay the form out the same way, which is the point.
    /// </para>
    /// </remarks>
    public static class FormLayoutExtensions
    {
        #region Functions

        /// <summary>
        /// How many columns this group asks for, as a number that can be used: at least one.
        /// </summary>
        public static int ColumnCount(this FormGroup me)
        {
            return Math.Max(1, me.Columns);
        }

        /// <summary>
        /// How the room is shared between a group's columns: one number per column, all of them
        /// above zero. Equal shares where the group said nothing, or said something unusable.
        /// </summary>
        public static IReadOnlyList<double> Weights(this FormGroup me)
        {
            return Shares(me.ColumnWeights, me.ColumnCount());
        }

        /// <remarks>
        /// A list that does not have one number per column, or that has one of them at zero, is
        /// ignored whole rather than patched: half a proportion is not a proportion, and guessing
        /// the rest would put widths on the screen that nobody chose.
        /// </remarks>
        private static IReadOnlyList<double> Shares(IReadOnlyList<double> weights, int columns)
        {
            return weights.Count == columns && weights.All(weight => weight > 0)
                ? weights
                : Enumerable.Repeat(1.0, columns).ToList();
        }

        /// <summary>How many columns the form asks its groups to flow into.</summary>
        public static int ColumnCount(this FormSchema me)
        {
            return Math.Max(1, me.Columns);
        }

        /// <summary>
        /// How the room is shared between the form's own columns: one number per column, all of
        /// them above zero. Equal shares where the form said nothing, or said something unusable.
        /// </summary>
        public static IReadOnlyList<double> Weights(this FormSchema me)
        {
            return Shares(me.ColumnWeights, me.ColumnCount());
        }

        /// <summary>
        /// How many columns this field takes, within a group of the given width. At least one, and
        /// never more than there are: a field asking for three columns of two takes both.
        /// </summary>
        public static int SpanIn(this FormField me, int columns)
        {
            return Math.Clamp(me.Span, 1, Math.Max(1, columns));
        }

        /// <summary>How many columns this group takes, within a form of the given width.</summary>
        public static int SpanIn(this FormGroup me, int columns)
        {
            return Math.Clamp(me.Span, 1, Math.Max(1, columns));
        }

        /// <summary>
        /// Lays things out across a given number of columns, in the order they were declared.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Left to right, top to bottom, and a thing that does not fit in what is left of a row
        /// starts the next one rather than being broken across two. The gap it leaves behind is
        /// left empty: shuffling a later, narrower thing forward to fill it would put the form's
        /// parts in an order nobody wrote, and the order a form is read in is part of what it
        /// means.
        /// </para>
        /// <para>
        /// <paramref name="columns"/> is what the renderer has decided it can afford, which is not
        /// always what was asked for — one column is always a correct answer, and is what a narrow
        /// surface should pass.
        /// </para>
        /// </remarks>
        /// <param name="items">What to place, already filtered to what is being drawn.</param>
        /// <param name="span">How many columns one of them takes, given how many there are.</param>
        /// <param name="columns">How many columns there are.</param>
        public static IReadOnlyList<FormPlacement<T>> Placements<T>(this IEnumerable<T> items,
            Func<T, int, int> span, int columns)
        {
            var width = Math.Max(1, columns);

            var placements = new List<FormPlacement<T>>();

            var row = 0;
            var column = 0;

            foreach (var item in items)
            {
                var taken = Math.Clamp(span(item, width), 1, width);

                if (column + taken > width)
                {
                    row++;
                    column = 0;
                }

                placements.Add(new FormPlacement<T>(item, row, column, taken));

                column += taken;

                if (column < width)
                    continue;

                row++;
                column = 0;
            }

            return placements;
        }

        /// <summary>Lays fields out across a given number of columns.</summary>
        public static IReadOnlyList<FormPlacement<FormField>> Placements(
            this IEnumerable<FormField> me, int columns)
        {
            return me.Placements((field, width) => field.SpanIn(width), columns);
        }

        /// <summary>
        /// Lays out the fields a group draws among its contents — all of them except the one drawn
        /// as the group's own switch — across as many columns as it asks for.
        /// </summary>
        public static IReadOnlyList<FormPlacement<FormField>> Placements(this FormGroup me)
        {
            return me.Contents().Placements(me.ColumnCount());
        }

        /// <summary>Lays groups out across a given number of columns.</summary>
        public static IReadOnlyList<FormPlacement<FormGroup>> Placements(
            this IEnumerable<FormGroup> me, int columns)
        {
            return me.Placements((group, width) => group.SpanIn(width), columns);
        }

        /// <summary>
        /// Lays out the form's own groups across as many columns as it asks for, the way it asked
        /// for them to be filled.
        /// </summary>
        public static IReadOnlyList<FormPlacement<FormGroup>> Placements(this FormSchema me)
        {
            return me.Flow == FormFlow.Columns
                ? me.Groups.Stacked(me.ColumnCount())
                : me.Groups.Placements(me.ColumnCount());
        }

        /// <summary>
        /// Fills each column to the bottom before beginning the next, in the order things were
        /// declared.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The columns are as even as counting allows: with five things in two columns the first
        /// gets three. What they actually come out to on the screen depends on how tall each one
        /// is, which is not something a description can know — but three short sections and two
        /// tall ones are still closer to level than any row-wise filling of the same five.
        /// </para>
        /// <para>
        /// Everything takes one column here, whatever its span: a thing wider than the column it
        /// is being stacked in has nothing to be stacked in.
        /// </para>
        /// </remarks>
        public static IReadOnlyList<FormPlacement<T>> Stacked<T>(this IEnumerable<T> me, int columns)
        {
            var items = me.ToList();

            var width = Math.Max(1, columns);
            var rows = (int)Math.Ceiling(items.Count / (double)width);

            var placements = new List<FormPlacement<T>>(items.Count);

            for (var index = 0; index < items.Count; index++)
                placements.Add(new FormPlacement<T>(items[index],
                    Row: rows == 0 ? 0 : index % rows,
                    Column: rows == 0 ? 0 : index / rows,
                    Span: 1));

            return placements;
        }

        /// <summary>How many rows a set of placements comes to.</summary>
        public static int RowCount<T>(this IReadOnlyList<FormPlacement<T>> me)
        {
            return me.Count == 0 ? 0 : me.Max(one => one.Row) + 1;
        }

        #endregion
    }
}
