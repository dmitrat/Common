using System.Collections.ObjectModel;
using System.Windows.Input;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// A module's contribution to a zone: a navigation bar entry, a menu item, a toolbar
    /// button. Identity (<see cref="Zone"/>, <see cref="Key"/>) and placement are fixed
    /// at creation; presentation state (<see cref="Header"/>, <see cref="IsEnabled"/>, …)
    /// belongs to the module and notifies. <see cref="IsSelected"/> and
    /// <see cref="Command"/> are maintained by the contribution registry.
    /// </summary>
    public class ContributionItem : NotifyPropertyChangedBase
    {
        #region Fields

        private readonly ObservableCollection<ContributionItem> m_children = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an item. <see cref="Zone"/> and <see cref="Key"/> are required.
        /// </summary>
        public ContributionItem()
        {
            Children = new ReadOnlyObservableCollection<ContributionItem>(m_children);
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Zone}/{Key}" + (RouteKey != null ? $" -> {RouteKey}" : string.Empty);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The zone the item belongs to.
        /// </summary>
        public required string Zone { get; init; }

        /// <summary>
        /// The key, unique within the zone. A second Add with the same key replaces the item.
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        /// Key of the parent item within the same zone, for nested menus. A module can
        /// place an item under another module's item without owning that object; the
        /// zone builds the tree, in whichever order the items arrive.
        /// </summary>
        public string? ParentKey { get; init; }

        /// <summary>
        /// Sort order within the parent. Ties keep insertion order.
        /// </summary>
        public double Order { get; init; }

        /// <summary>
        /// Route to navigate to when the item is activated. When set, the registry
        /// supplies <see cref="Command"/>.
        /// </summary>
        public string? RouteKey { get; init; }

        /// <summary>
        /// Parameters for the navigation, and the parameters the item is selected for.
        /// </summary>
        public NavigationParameters? Parameters { get; init; }

        /// <summary>
        /// Outlet to navigate in; null means the route's default outlet.
        /// </summary>
        public string? Outlet { get; init; }

        /// <summary>
        /// Display text, already localized. The module updates it on language change.
        /// </summary>
        [Notify]
        public string? Header { get; set; }

        /// <summary>
        /// Icon name or resource key, interpreted by the application's templates.
        /// </summary>
        [Notify]
        public string? Icon { get; set; }

        /// <summary>
        /// Tooltip text, already localized.
        /// </summary>
        [Notify]
        public string? ToolTip { get; set; }

        /// <summary>
        /// Keyboard gesture such as "Ctrl+S", parsed by the platform.
        /// </summary>
        [Notify]
        public string? Gesture { get; set; }

        /// <summary>
        /// Whether the item is shown.
        /// </summary>
        [Notify]
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Whether the item can be activated. Mirrored into <see cref="Command"/>'s CanExecute.
        /// </summary>
        [Notify]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Toggle state owned by the module ("Show grid ✓"). Not touched by navigation.
        /// </summary>
        [Notify]
        public bool IsChecked { get; set; }

        /// <summary>
        /// True while the item's route and parameters are what its outlet shows.
        /// Maintained by the zone; for highlighting the navigation bar.
        /// </summary>
        [Notify]
        public bool IsSelected { get; internal set; }

        /// <summary>
        /// Navigation command, supplied by the registry when <see cref="RouteKey"/> is set.
        /// </summary>
        [Notify]
        public ICommand? Command { get; internal set; }

        /// <summary>
        /// A view model for a non-standard contribution (a widget rather than an item).
        /// The zone's markup shows it through a ContentControl and the view locator.
        /// </summary>
        public object? Content { get; init; }

        /// <summary>
        /// Opaque data for the application's templates and guards.
        /// </summary>
        public object? Metadata { get; init; }

        /// <summary>
        /// Child items, sorted by <see cref="Order"/>. Populated by the zone from <see cref="ParentKey"/>.
        /// </summary>
        public ReadOnlyObservableCollection<ContributionItem> Children { get; }

        internal ObservableCollection<ContributionItem> ChildrenInternal => m_children;

        internal long Sequence { get; set; }

        #endregion
    }
}
