using System.Collections.Generic;
using OutWit.Common.Licensing.Generator.Json;

namespace OutWit.Common.Licensing.Generator
{
    /// <summary>
    /// One product's licensing vocabulary, as read from its <c>*.product.json</c>.
    /// <para>
    /// The single source of truth for three things that must agree and today
    /// are checked by nobody: the keys the product's code asks about, the keys
    /// it declares to the runtime, and the keys an operator types into the
    /// registry catalogue. The third is a separate system; the first two stop
    /// being able to drift the moment they come from here.
    /// </para>
    /// </summary>
    internal sealed class ProductDescriptor
    {
        #region Constructors

        private ProductDescriptor(string product, List<VocabularyEntry> features, List<VocabularyEntry> limits)
        {
            Product = product;
            Features = features;
            Limits = limits;
        }

        #endregion

        #region Functions

        /// <summary>Reads a descriptor, or returns null with a reason a human can act on.</summary>
        public static ProductDescriptor? Read(string text, out string? error)
        {
            var root = JsonReader.Parse(text, out error);

            if (root == null)
                return null;

            if (root.Kind != JsonNodeKind.Object)
            {
                error = "the descriptor must be a JSON object";
                return null;
            }

            var product = root.TextOf("product");

            if (string.IsNullOrWhiteSpace(product))
            {
                error = "'product' is missing — a vocabulary with no product cannot be matched to a licence";
                return null;
            }

            var features = ReadEntries(root.Member("features"), withDefault: false, out error);
            if (error != null)
                return null;

            var limits = ReadEntries(root.Member("limits"), withDefault: true, out error);
            if (error != null)
                return null;

            return new ProductDescriptor(product!.Trim(), features, limits);
        }

        #endregion

        #region Tools

        private static List<VocabularyEntry> ReadEntries(JsonNode? node, bool withDefault, out string? error)
        {
            error = null;

            var entries = new List<VocabularyEntry>();

            if (node == null || node.Kind == JsonNodeKind.Null)
                return entries;

            if (node.Kind != JsonNodeKind.Array || node.Items == null)
            {
                error = "'features' and 'limits' must be arrays";
                return entries;
            }

            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var item in node.Items)
            {
                if (item.Kind != JsonNodeKind.Object)
                {
                    error = "every entry must be an object with a 'key'";
                    return entries;
                }

                var key = item.TextOf("key");

                if (string.IsNullOrWhiteSpace(key))
                {
                    error = "an entry has no 'key'";
                    return entries;
                }

                key = key!.Trim();

                // Case-insensitive, because the runtime matches keys that way —
                // two entries differing only in case would generate two members
                // that mean one thing, and the product would look like it
                // supports both.
                if (!seen.Add(key))
                {
                    error = $"'{key}' appears twice";
                    return entries;
                }

                entries.Add(new VocabularyEntry(
                    key,
                    item.TextOf("name") ?? item.TextOf("description") ?? string.Empty,
                    withDefault ? item.NumberOf("default") : null));
            }

            return entries;
        }

        #endregion

        #region Properties

        public string Product { get; }

        public List<VocabularyEntry> Features { get; }

        public List<VocabularyEntry> Limits { get; }

        #endregion
    }

    /// <summary>One declared key: what it is called, what it means, and what it is when unset.</summary>
    internal sealed class VocabularyEntry
    {
        public VocabularyEntry(string key, string name, long? @default)
        {
            Key = key;
            Name = name;
            Default = @default;
        }

        public string Key { get; }

        public string Name { get; }

        /// <summary>Only limits have one. An absent limit is unlimited unless the product says otherwise.</summary>
        public long? Default { get; }
    }
}
