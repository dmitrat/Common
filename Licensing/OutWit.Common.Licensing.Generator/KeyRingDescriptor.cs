using System;
using System.Collections.Generic;
using OutWit.Common.Licensing.Generator.Json;

namespace OutWit.Common.Licensing.Generator
{
    /// <summary>
    /// A key ring exported by the issuing service, read far enough to be checked
    /// and re-emitted.
    /// <para>
    /// The checks matter more than the parsing. Every way a ring can be wrong
    /// currently fails <b>closed and silently</b>: the runtime drops a key with
    /// no <c>kid</c>, lets the last of two duplicate <c>kid</c>s win, and treats
    /// a key that names no products as covering none. Each of those turns into
    /// "licence invalid" at a customer site with nothing to say which side is
    /// wrong. At build time they are one line each.
    /// </para>
    /// </summary>
    internal sealed class KeyRingDescriptor
    {
        #region Constants

        /// <summary>
        /// <c>alg</c> and <c>policy</c> are enums at the far end, and a value
        /// outside the set does not merely spoil its own key: the runtime reader
        /// throws on it and hands back an <b>empty ring</b>. One mistyped word
        /// and the product trusts nothing at all.
        /// <para>
        /// Checked against a hard-coded list because an analyzer cannot reference
        /// the library it generates for. The cost is that a value added to the
        /// library needs a line here; the alternative is a whole ring lost to a
        /// spelling.
        /// </para>
        /// </summary>
        private static readonly string[] ALGORITHMS = { "None", "ES256", "ES384", "ES512", "RS256", "PS256" };

        private static readonly string[] POLICIES = { "Commercial", "TrialOnly" };

        #endregion

        #region Constructors

        private KeyRingDescriptor(string product, string json, List<KeyRingEntry> keys)
        {
            Product = product;
            Json = json;
            Keys = keys;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Reads a ring, or returns null with a reason. <paramref name="warnings"/>
        /// carries what is suspicious but still buildable.
        /// </summary>
        public static KeyRingDescriptor? Read(string text, out string? error, out List<string> warnings)
        {
            warnings = new List<string>();

            var root = JsonReader.Parse(text, out error);

            if (root == null)
                return null;

            if (root.Kind != JsonNodeKind.Object)
            {
                error = "the ring must be a JSON object";
                return null;
            }

            var product = (root.TextOf("product") ?? string.Empty).Trim();

            if (product.Length == 0)
            {
                error = "'product' is missing — it names the class the ring is generated into";
                return null;
            }

            var keys = ReadKeys(root, product, out error, warnings);

            if (keys == null)
                return null;

            return new KeyRingDescriptor(product, JsonWriter.Write(root), keys);
        }

        #endregion

        #region Tools

        private static List<KeyRingEntry>? ReadKeys(JsonNode root, string product, out string? error,
            List<string> warnings)
        {
            error = null;

            var node = root.Member("keys");

            if (node == null || node.Kind != JsonNodeKind.Array || node.Items == null)
            {
                error = "'keys' is missing or is not an array";
                return null;
            }

            if (node.Items.Count == 0)
            {
                error = "'keys' is empty — a ring that trusts nothing rejects every licence";
                return null;
            }

            var keys = new List<KeyRingEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var covered = false;

            foreach (var item in node.Items)
            {
                if (item.Kind != JsonNodeKind.Object)
                {
                    error = "'keys' must hold objects";
                    return null;
                }

                var keyId = (item.TextOf("kid") ?? string.Empty).Trim();

                if (keyId.Length == 0)
                {
                    // The runtime drops such a key without a word, which is the
                    // worst possible outcome: a ring one key short of working.
                    error = "a key has no 'kid' and would be dropped silently at runtime";
                    return null;
                }

                if (!seen.Add(keyId))
                {
                    // The runtime lets the last one win rather than throwing, so
                    // a duplicated kid is a packaging mistake that never reports
                    // itself. Here it can.
                    error = $"'{keyId}' appears twice — the second would silently replace the first";
                    return null;
                }

                var algorithm = (item.TextOf("alg") ?? string.Empty).Trim();

                if (algorithm.Length == 0)
                {
                    error = $"'{keyId}' has no 'alg'";
                    return null;
                }

                if (!Known(ALGORITHMS, algorithm))
                {
                    error = Unknown(keyId, "alg", algorithm, ALGORITHMS);
                    return null;
                }

                if (string.Equals(algorithm, "None", StringComparison.OrdinalIgnoreCase))
                    warnings.Add($"'{keyId}' has algorithm None and can verify nothing");

                if ((item.TextOf("publicKeyPem") ?? string.Empty).Trim().Length == 0)
                {
                    error = $"'{keyId}' has no 'publicKeyPem'";
                    return null;
                }

                var policy = (item.TextOf("policy") ?? "Commercial").Trim();

                if (!Known(POLICIES, policy))
                {
                    error = Unknown(keyId, "policy", policy, POLICIES);
                    return null;
                }

                var products = ReadProducts(item);

                if (products.Count == 0)
                    warnings.Add($"'{keyId}' names no products and therefore covers none");

                foreach (var name in products)
                {
                    if (string.Equals(name, product, StringComparison.OrdinalIgnoreCase))
                        covered = true;
                }

                keys.Add(new KeyRingEntry(keyId, algorithm, policy));
            }

            if (!covered)
                warnings.Add($"no key in this ring covers '{product}', so every licence for it would be refused");

            return keys;
        }

        private static bool Known(string[] allowed, string value)
        {
            foreach (var candidate in allowed)
            {
                if (string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string Unknown(string keyId, string field, string value, string[] allowed)
        {
            return $"'{keyId}' has {field} '{value}', which is not one of {string.Join(", ", allowed)} — " +
                   "the runtime would fail to read the ring and trust no key at all";
        }

        private static List<string> ReadProducts(JsonNode key)
        {
            var products = new List<string>();
            var node = key.Member("products");

            if (node?.Kind != JsonNodeKind.Array || node.Items == null)
                return products;

            foreach (var item in node.Items)
            {
                if (item.Kind == JsonNodeKind.String && !string.IsNullOrWhiteSpace(item.Text))
                    products.Add(item.Text!.Trim());
            }

            return products;
        }

        #endregion

        #region Properties

        /// <summary>What the ring was exported for. Names the generated class.</summary>
        public string Product { get; }

        /// <summary>The ring, minified and strict — the text that becomes the constant.</summary>
        public string Json { get; }

        /// <summary>What the ring trusts, for the generated documentation.</summary>
        public List<KeyRingEntry> Keys { get; }

        #endregion
    }

    /// <summary>One trusted key, reduced to what a reader of the generated file needs.</summary>
    internal sealed class KeyRingEntry
    {
        #region Constructors

        public KeyRingEntry(string keyId, string algorithm, string policy)
        {
            KeyId = keyId;
            Algorithm = algorithm;
            Policy = policy;
        }

        #endregion

        #region Properties

        public string KeyId { get; }

        public string Algorithm { get; }

        public string Policy { get; }

        #endregion
    }
}
