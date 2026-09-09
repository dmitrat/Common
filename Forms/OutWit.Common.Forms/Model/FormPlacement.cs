namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// Where one thing ended up: which row, which column, and how many columns wide.
    /// </summary>
    /// <remarks>
    /// Worked out, not declared — a form says how many columns it wants and how much of them each
    /// thing takes, and this is what that comes to once the renderer has said how many columns it
    /// can afford. It travels nowhere: it is not part of the schema, is not serialised, and is
    /// recomputed by whoever draws.
    /// </remarks>
    /// <typeparam name="T">
    /// What is being placed. Fields within a group, or groups within the form: the arithmetic is
    /// the same and doing it twice is how two levels of one form come to disagree.
    /// </typeparam>
    /// <param name="Item">The thing being placed.</param>
    /// <param name="Row">Its row, counted from zero.</param>
    /// <param name="Column">Its first column, counted from zero.</param>
    /// <param name="Span">How many columns it takes, at least one.</param>
    public readonly record struct FormPlacement<T>(T Item, int Row, int Column, int Span);
}
