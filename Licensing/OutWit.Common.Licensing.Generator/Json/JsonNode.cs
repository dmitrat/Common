using System.Collections.Generic;

namespace OutWit.Common.Licensing.Generator.Json
{
    /// <summary>
    /// A parsed JSON value, in the smallest shape a descriptor needs.
    /// <para>
    /// Hand-rolled rather than taken from a package because this is an analyzer:
    /// its dependencies have to be shipped inside the analyzer folder, and a
    /// serialiser is a large thing to carry there for a file with three keys in
    /// it.
    /// </para>
    /// </summary>
    internal sealed class JsonNode
    {
        #region Constructors

        private JsonNode(JsonNodeKind kind)
        {
            Kind = kind;
        }

        #endregion

        #region Functions

        public static JsonNode Object(Dictionary<string, JsonNode> members)
        {
            return new JsonNode(JsonNodeKind.Object) { Members = members };
        }

        public static JsonNode Array(List<JsonNode> items)
        {
            return new JsonNode(JsonNodeKind.Array) { Items = items };
        }

        public static JsonNode String(string value)
        {
            return new JsonNode(JsonNodeKind.String) { Text = value };
        }

        public static JsonNode Number(double value)
        {
            return new JsonNode(JsonNodeKind.Number) { Value = value };
        }

        public static JsonNode Boolean(bool value)
        {
            return new JsonNode(JsonNodeKind.Boolean) { Value = value ? 1 : 0 };
        }

        public static JsonNode Null()
        {
            return new JsonNode(JsonNodeKind.Null);
        }

        /// <summary>The named member, or null when absent or when this is not an object.</summary>
        public JsonNode? Member(string name)
        {
            return Members != null && Members.TryGetValue(name, out var member) ? member : null;
        }

        /// <summary>The text of a string member, or null.</summary>
        public string? TextOf(string name)
        {
            var member = Member(name);

            return member?.Kind == JsonNodeKind.String ? member.Text : null;
        }

        /// <summary>The numeric value of a member, or null when absent or not a number.</summary>
        public long? NumberOf(string name)
        {
            var member = Member(name);

            return member?.Kind == JsonNodeKind.Number ? (long)member.Value : (long?)null;
        }

        #endregion

        #region Properties

        public JsonNodeKind Kind { get; }

        public Dictionary<string, JsonNode>? Members { get; private set; }

        public List<JsonNode>? Items { get; private set; }

        public string? Text { get; private set; }

        public double Value { get; private set; }

        #endregion
    }

    /// <summary>What a <see cref="JsonNode"/> holds.</summary>
    internal enum JsonNodeKind
    {
        Object,
        Array,
        String,
        Number,
        Boolean,
        Null
    }
}
