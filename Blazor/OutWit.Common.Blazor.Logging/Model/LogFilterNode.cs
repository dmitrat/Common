using OutWit.Common.Abstract;
using OutWit.Common.NewRelic.Model;
using OutWit.Common.Values;
using OutWit.Common.Collections;

namespace OutWit.Common.Blazor.Logging.Model
{
    /// <summary>
    /// Represents a node in the log filter tree.
    /// Supports hierarchical filtering with parent-child relationships.
    /// </summary>
    public class LogFilterNode : ModelBase
    {
        #region Constructors

        /// <summary>
        /// Creates a new filter node with a default title.
        /// </summary>
        public LogFilterNode()
        {
            Title = "Filter";
        }

        /// <summary>
        /// Creates a new filter node with a specific title.
        /// </summary>
        public LogFilterNode(string title)
        {
            Title = title;
        }

        #endregion

        #region ModelBase

        /// <summary>
        /// Value-based comparison of two filter nodes (including children).
        /// Parent reference is intentionally ignored to avoid recursion loops.
        /// </summary>
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LogFilterNode node)
                return false;

            return Title.Is(node.Title) &&
                   FullTextSearch.Is(node.FullTextSearch) &&
                   IsDisabled.Is(node.IsDisabled) &&
                   CurrentOffset.Is(node.CurrentOffset) &&
                   LastHasMore.Is(node.LastHasMore) &&
                   Filters.Is(node.Filters) &&
                   IsExclusion.Is(node.IsExclusion) &&
                   Children.Is(node.Children);
        }

        /// <summary>
        /// Creates a deep clone of this node, including all children.
        /// Parent of the clone is always null.
        /// </summary>
        public override LogFilterNode Clone()
        {
            return CloneDeep();
        }

        #endregion

        #region Debug helpers

        public override string ToString()
        {
            return $"{Title} (disabled: {IsDisabled}, filters: {Filters.Count}, children: {Children.Count})";
        }

        #endregion

        #region Tree helpers

        /// <summary>
        /// Adds the specified child node to this node and updates Parent.
        /// </summary>
        public void AddChild(LogFilterNode child)
        {
            if (child is null)
                throw new ArgumentNullException(nameof(child));

            if (child.Parent != null)
                child.Parent.Children.Remove(child);

            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Enumerates this node and all its ancestors up to the root.
        /// </summary>
        public IEnumerable<LogFilterNode> AncestorsAndSelf()
        {
            for (var node = this; node != null; node = node.Parent)
                yield return node;
        }

        /// <summary>
        /// Enumerates this node and all its descendants in depth-first order.
        /// </summary>
        public IEnumerable<LogFilterNode> SelfAndDescendants()
        {
            yield return this;

            foreach (var child in Children)
            {
                foreach (var descendant in child.SelfAndDescendants())
                    yield return descendant;
            }
        }

        /// <summary>
        /// Creates a shallow copy of this node (no children).
        /// </summary>
        public LogFilterNode CloneShallow()
        {
            var clone = new LogFilterNode
            {
                Title = Title,
                FullTextSearch = FullTextSearch,
                IsDisabled = IsDisabled,
                CurrentOffset = CurrentOffset,
                LastHasMore = LastHasMore,
                IsExclusion = IsExclusion
            };

            if (Filters.Count > 0)
                clone.Filters.AddRange(Filters);

            return clone;
        }

        /// <summary>
        /// Creates a deep copy of this node and all its children.
        /// </summary>
        public LogFilterNode CloneDeep()
        {
            var clone = CloneShallow();

            foreach (var child in Children)
            {
                var childClone = child.CloneDeep();
                clone.AddChild(childClone);
            }

            return clone;
        }

        #endregion

        #region Highlight helpers

        /// <summary>
        /// Returns terms that should be highlighted in the UI.
        /// </summary>
        public IEnumerable<string> GetHighlightTerms()
        {
            if (!string.IsNullOrWhiteSpace(FullTextSearch))
                yield return FullTextSearch!;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Human-readable title shown in the filter tree.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Full-text search string applied to the log message.
        /// </summary>
        public string? FullTextSearch { get; set; }

        /// <summary>
        /// Structured filters for NewRelic (field-based conditions).
        /// </summary>
        public List<NewRelicLogFilter> Filters { get; } = new();

        /// <summary>
        /// Indicates whether this node is temporarily disabled.
        /// </summary>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Reference to the parent node. Null for root nodes.
        /// </summary>
        public LogFilterNode? Parent { get; private set; }

        /// <summary>
        /// Collection of child filters.
        /// </summary>
        public List<LogFilterNode> Children { get; } = new();

        /// <summary>
        /// Last known offset for pagination.
        /// </summary>
        public int CurrentOffset { get; set; }

        /// <summary>
        /// Indicates whether the last page had more data.
        /// </summary>
        public bool LastHasMore { get; set; }

        /// <summary>
        /// If true, creates a "NOT LIKE" filter instead of "LIKE".
        /// </summary>
        public bool IsExclusion { get; set; }

        /// <summary>
        /// True if the node has at least one child.
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        #endregion
    }
}
