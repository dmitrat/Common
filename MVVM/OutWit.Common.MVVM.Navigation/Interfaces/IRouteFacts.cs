namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// What a zone needs to know about routes to keep its selection right: where an item's
    /// key lands by default, and whether the page an outlet shows belongs to the group an item
    /// points at. A slice of <see cref="IRouteRegistry"/>, so the zone does not carry all of it.
    /// </summary>
    internal interface IRouteFacts
    {
        #region Functions

        /// <summary>
        /// The default outlet of a route or a group; null when the key is neither.
        /// </summary>
        string? DefaultOutletOf(string key);

        /// <summary>
        /// True when the key names a group and the route is one of its members.
        /// </summary>
        bool GroupContains(string groupKey, string routeKey);

        #endregion
    }
}
