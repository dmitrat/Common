using System.Linq;
using System.Text;

namespace OutWit.Common.Licensing.Generator
{
    /// <summary>
    /// Turns the keys a licence uses into the identifiers C# will accept.
    /// <para>
    /// The keys themselves are never touched — they are the contract with the
    /// issuing side and a licence granting <c>format.nas</c> means exactly that
    /// string. Only the names of the constants are derived.
    /// </para>
    /// </summary>
    internal static class Naming
    {
        #region Functions

        /// <summary>
        /// A C# identifier from a key: <c>format.nas</c> becomes
        /// <c>FormatNas</c>, <c>maxVariants</c> stays <c>MaxVariants</c>.
        /// Separators are dropped and each part is capitalised; casing already
        /// inside a part is left alone, so <c>maxNodes</c> does not become
        /// <c>Maxnodes</c>.
        /// </summary>
        public static string Identifier(string value)
        {
            var builder = new StringBuilder();
            var capitalise = true;

            foreach (var character in value ?? string.Empty)
            {
                if (!char.IsLetterOrDigit(character))
                {
                    capitalise = true;
                    continue;
                }

                builder.Append(capitalise ? char.ToUpperInvariant(character) : character);
                capitalise = false;
            }

            var identifier = builder.ToString();

            if (identifier.Length == 0)
                return "Key";

            // A key that begins with a digit is legal in a licence and not in
            // C#, so it gets a prefix rather than a build error.
            return char.IsDigit(identifier[0]) ? "_" + identifier : identifier;
        }

        /// <summary>The name of the generated class for a product: <c>test-client</c> becomes <c>TestClientLicense</c>.</summary>
        public static string ClassName(string product)
        {
            return Identifier(product) + "License";
        }

        /// <summary>The name of the generated ring class: <c>WitSweep</c> becomes <c>WitSweepKeyRing</c>.</summary>
        public static string RingClassName(string product)
        {
            return Identifier(product) + "KeyRing";
        }

        /// <summary>Escapes a string for a C# literal.</summary>
        public static string Literal(string? value)
        {
            return "\"" + (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") + "\"";
        }

        /// <summary>A doc-comment-safe rendering of arbitrary text.</summary>
        public static string Comment(string? value)
        {
            var text = (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            return string.Concat(text.Where(character => character != '\r' && character != '\n'));
        }

        #endregion
    }
}
