namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// What a group is on the screen.
    /// </summary>
    public enum FormGroupKind : byte
    {
        /// <summary>A heading and the fields under it.</summary>
        Section = 0,

        /// <summary>A strip of tabs. Its own groups are the tabs, and it has no fields of its own.</summary>
        Tabs = 1,

        /// <summary>One tab inside a <see cref="Tabs"/> group.</summary>
        Tab = 2,

        /// <summary>
        /// A list of entries with one of them open beside it. Its own groups are the entries, and
        /// it has no fields of its own. What a strip of tabs is for a handful of pages, this is for
        /// a handful of things of one kind — the episodes an event recorder watches for — where
        /// each is named, has a state, and is read down the list without being opened.
        /// </summary>
        List = 3,

        /// <summary>
        /// One entry inside a <see cref="List"/> group. Its <see cref="FormGroup.SummaryKey"/> is
        /// the line under its name, and <see cref="FormGroup.ActiveWhen"/> — or its switch — is
        /// whether it is marked as on.
        /// </summary>
        Entry = 4
    }
}
