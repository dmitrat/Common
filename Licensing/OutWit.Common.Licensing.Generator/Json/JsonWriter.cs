using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OutWit.Common.Licensing.Generator.Json
{
    /// <summary>
    /// Writes a parsed document back out as strict, minified JSON.
    /// <para>
    /// The key ring is embedded as a <c>const string</c>, and that constant is
    /// read at runtime by <c>System.Text.Json</c>, which accepts neither the
    /// comments nor the trailing commas <see cref="JsonReader"/> allows. Copying
    /// the file verbatim would therefore let an annotated ring compile happily
    /// and then parse to nothing at startup — a product that trusts no keys and
    /// says only "unknown key id". Re-emitting through this writer means the
    /// constant is by construction something the runtime reader accepts.
    /// </para>
    /// <para>
    /// Members are ordered so the same file always yields the same constant:
    /// generated output that changes between builds is generated output nobody
    /// can review in a diff.
    /// </para>
    /// </summary>
    internal static class JsonWriter
    {
        #region Functions

        /// <summary>Minified, strict JSON for <paramref name="node"/>.</summary>
        public static string Write(JsonNode node)
        {
            var builder = new StringBuilder();

            Write(builder, node);

            return builder.ToString();
        }

        #endregion

        #region Tools

        private static void Write(StringBuilder builder, JsonNode node)
        {
            switch (node.Kind)
            {
                case JsonNodeKind.Object:
                    WriteObject(builder, node);
                    break;

                case JsonNodeKind.Array:
                    WriteArray(builder, node);
                    break;

                case JsonNodeKind.String:
                    WriteString(builder, node.Text ?? string.Empty);
                    break;

                case JsonNodeKind.Number:
                    builder.Append(node.Value.ToString("R", CultureInfo.InvariantCulture));
                    break;

                case JsonNodeKind.Boolean:
                    builder.Append(node.Value != 0 ? "true" : "false");
                    break;

                default:
                    builder.Append("null");
                    break;
            }
        }

        private static void WriteObject(StringBuilder builder, JsonNode node)
        {
            var members = node.Members ?? new Dictionary<string, JsonNode>();

            builder.Append('{');

            var first = true;

            // Ordinal, not the dictionary's own order: two builds of the same
            // file must produce the same byte sequence.
            foreach (var name in members.Keys.OrderBy(key => key, System.StringComparer.Ordinal))
            {
                if (!first)
                    builder.Append(',');

                WriteString(builder, name);
                builder.Append(':');
                Write(builder, members[name]);

                first = false;
            }

            builder.Append('}');
        }

        private static void WriteArray(StringBuilder builder, JsonNode node)
        {
            var items = node.Items ?? new List<JsonNode>();

            builder.Append('[');

            for (var index = 0; index < items.Count; index++)
            {
                if (index > 0)
                    builder.Append(',');

                Write(builder, items[index]);
            }

            builder.Append(']');
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');

            foreach (var character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;

                    case '\\':
                        builder.Append("\\\\");
                        break;

                    case '\b':
                        builder.Append("\\b");
                        break;

                    case '\f':
                        builder.Append("\\f");
                        break;

                    case '\n':
                        builder.Append("\\n");
                        break;

                    case '\r':
                        builder.Append("\\r");
                        break;

                    case '\t':
                        builder.Append("\\t");
                        break;

                    default:
                        if (character < ' ')
                            builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(character);
                        break;
                }
            }

            builder.Append('"');
        }

        #endregion
    }
}
