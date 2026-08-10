using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace OutWit.Common.Licensing.Generator
{
    /// <summary>
    /// Turns an exported key ring into a <c>const string</c> the product carries.
    /// <para>
    /// Substitution is the better attack on an offline verifier: replacing the
    /// trusted public key needs no understanding of the binary, is entirely
    /// scriptable, survives the next release unchanged, and leaves behind a
    /// product that <i>genuinely validates</i> every licence its new owner cares
    /// to mint. Removal — patching the gate — needs the attacker to read code.
    /// </para>
    /// <para>
    /// An embedded resource is the worst landing place for the ring: a plain blob
    /// in the assembly manifest, visible in any decompiler's resource view,
    /// findable with <c>strings</c>, replaceable without touching an instruction,
    /// and not covered by string encryption, which transforms literals in IL. A
    /// constant is the same data in the shape the obfuscator can defend.
    /// </para>
    /// <para>
    /// The export stays the source of truth, so nobody copies PEM blocks by hand
    /// — which is the whole reason the issuing service exports a ring at all.
    /// </para>
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class LicenseKeyRingGenerator : IIncrementalGenerator
    {
        #region Constants

        private const string SUFFIX = ".keyring.json";

        private const string DEVELOPMENT_SUFFIX = ".dev" + SUFFIX;

        private static readonly DiagnosticDescriptor UNREADABLE = new(
            id: "OWL002",
            title: "The key ring could not be read",
            messageFormat: "'{0}' could not be read: {1}",
            category: "OutWit.Licensing",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Every way a ring can be malformed fails closed and silently at runtime, and reaches " +
                         "the customer as 'licence invalid' with nothing to say which side is wrong.");

        private static readonly DiagnosticDescriptor SUSPICIOUS = new(
            id: "OWL003",
            title: "The key ring will refuse more than it looks like it should",
            messageFormat: "'{0}': {1}",
            category: "OutWit.Licensing",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The ring parses, but part of it trusts nothing. Said at build time it is a line; " +
                         "found at a customer site it is a support case.");

        private static readonly DiagnosticDescriptor AMBIGUOUS = new(
            id: "OWL004",
            title: "Two key rings claim the same product",
            messageFormat: "product '{0}' is declared by more than one ring: {1}",
            category: "OutWit.Licensing",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Which ring a product trusts cannot be decided by file ordering.");

        #endregion

        #region Functions

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var rings = context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(SUFFIX, StringComparison.OrdinalIgnoreCase))
                .Select(static (file, token) => (file.Path, Text: file.GetText(token)?.ToString() ?? string.Empty))
                .Collect();

            // Collected rather than streamed: a product's production and
            // development rings are two files that must land in one class, and
            // that decision cannot be made a file at a time.
            var withNamespace = rings.Combine(context.AnalyzerConfigOptionsProvider
                .Select(static (options, _) =>
                    options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value) &&
                    !string.IsNullOrWhiteSpace(value)
                        ? value
                        : "Licensing"));

            context.RegisterSourceOutput(withNamespace, static (production, pair) =>
            {
                var (files, rootNamespace) = pair;

                var parsed = new List<RingFile>();

                foreach (var (path, text) in files)
                {
                    var name = System.IO.Path.GetFileName(path);

                    var descriptor = KeyRingDescriptor.Read(text, out var error, out var warnings);

                    if (descriptor == null)
                    {
                        production.ReportDiagnostic(Diagnostic.Create(UNREADABLE, Location.None, name, error));

                        continue;
                    }

                    foreach (var warning in warnings)
                        production.ReportDiagnostic(Diagnostic.Create(SUSPICIOUS, Location.None, name, warning));

                    parsed.Add(new RingFile(name, descriptor,
                        name.EndsWith(DEVELOPMENT_SUFFIX, StringComparison.OrdinalIgnoreCase)));
                }

                foreach (var group in parsed.GroupBy(ring => ring.Descriptor.Product, StringComparer.OrdinalIgnoreCase))
                    Emit(production, group.Key, group.ToList(), rootNamespace);
            });
        }

        #endregion

        #region Tools

        private static void Emit(SourceProductionContext production, string product, List<RingFile> rings,
            string rootNamespace)
        {
            var live = rings.Where(ring => !ring.IsDevelopment).ToList();
            var development = rings.Where(ring => ring.IsDevelopment).ToList();

            if (live.Count > 1 || development.Count > 1)
            {
                production.ReportDiagnostic(Diagnostic.Create(AMBIGUOUS, Location.None, product,
                    string.Join(", ", rings.Select(ring => ring.Name))));

                return;
            }

            if (live.Count == 0)
            {
                // A product whose only ring is the development one trusts nothing
                // in Release — which is the correct outcome, since a development
                // licence must be worthless against a shipped build. It is said
                // out loud because it does not look like an outcome anyone chose.
                production.ReportDiagnostic(Diagnostic.Create(SUSPICIOUS, Location.None,
                    development[0].Name,
                    $"this is the only ring for '{product}', so a Release build trusts no key at all"));
            }

            var className = Naming.RingClassName(product);

            production.AddSource($"{className}.g.cs", SourceText.From(
                Render(product, className, rootNamespace, live.FirstOrDefault(), development.FirstOrDefault()),
                Encoding.UTF8));
        }

        private static string Render(string product, string className, string rootNamespace,
            RingFile? live, RingFile? development)
        {
            var builder = new StringBuilder();

            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("// Generated from the exported key ring. Do not edit — export it again instead.");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using OutWit.Common.Licensing.Keys;");
            builder.AppendLine();
            builder.AppendLine($"namespace {rootNamespace}");
            builder.AppendLine("{");

            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// The public keys {Naming.Comment(product)} accepts a licence from.");
            builder.AppendLine("    /// <para>");
            builder.AppendLine("    /// Held as constants rather than as an embedded resource: a resource is a");
            builder.AppendLine("    /// blob in the assembly manifest that can be swapped without touching an");
            builder.AppendLine("    /// instruction, and swapping it yields a product that validates the");
            builder.AppendLine("    /// attacker's own licences honestly. A literal is the same data in the one");
            builder.AppendLine("    /// shape string encryption can defend.");
            builder.AppendLine("    /// </para>");

            RenderTrusted(builder, live, development);

            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public static class {className}");
            builder.AppendLine("    {");
            builder.AppendLine("        /// <summary>What this ring was exported for.</summary>");
            builder.AppendLine($"        public const string Product = {Naming.Literal(product)};");
            builder.AppendLine();
            builder.AppendLine($"        private const string RING = {Naming.Literal(live?.Descriptor.Json ?? EmptyRing(product))};");

            if (development != null)
            {
                builder.AppendLine();
                builder.AppendLine("        // Debug only, so a development licence stays worthless against a");
                builder.AppendLine("        // shipped build without any runtime check having to enforce it.");
                builder.AppendLine("#if DEBUG");
                builder.AppendLine($"        private const string RING_DEVELOPMENT = {Naming.Literal(development.Descriptor.Json)};");
                builder.AppendLine("#endif");
            }

            builder.AppendLine();
            builder.AppendLine("        /// <summary>The ring, for <c>WithKeyRing(...)</c>.</summary>");
            builder.AppendLine("        public static ILicenseKeyRing Create()");
            builder.AppendLine("        {");

            if (development == null)
            {
                builder.AppendLine("            return LicenseKeyRing.FromJson(RING);");
            }
            else
            {
                builder.AppendLine("            var keys = new List<LicenseKeyInfo>(LicenseKeyRing.FromJson(RING).Keys);");
                builder.AppendLine();
                builder.AppendLine("#if DEBUG");
                builder.AppendLine("            keys.AddRange(LicenseKeyRing.FromJson(RING_DEVELOPMENT).Keys);");
                builder.AppendLine("#endif");
                builder.AppendLine();
                builder.AppendLine("            return new LicenseKeyRing(keys);");
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        /// <summary>
        /// Lists what the ring trusts, in the doc comment. A customer's security
        /// team is entitled to see what the product verifies against, and this is
        /// tamper-resistance rather than secrecy — hiding the list would buy
        /// nothing and cost the answer to a fair question.
        /// </summary>
        private static void RenderTrusted(StringBuilder builder, RingFile? live, RingFile? development)
        {
            builder.AppendLine("    /// <para>Trusted keys:</para>");
            builder.AppendLine("    /// <list type=\"bullet\">");

            foreach (var key in live?.Descriptor.Keys ?? new List<KeyRingEntry>())
                builder.AppendLine($"    /// <item>{Naming.Comment(key.KeyId)} — {Naming.Comment(key.Algorithm)}, {Naming.Comment(key.Policy)}</item>");

            foreach (var key in development?.Descriptor.Keys ?? new List<KeyRingEntry>())
                builder.AppendLine($"    /// <item>{Naming.Comment(key.KeyId)} — {Naming.Comment(key.Algorithm)}, {Naming.Comment(key.Policy)} (Debug builds only)</item>");

            builder.AppendLine("    /// </list>");
        }

        /// <summary>
        /// The ring for a product that has no production export yet. It trusts
        /// nothing, which is the honest Release behaviour for a product whose
        /// only keys are development ones.
        /// </summary>
        private static string EmptyRing(string product)
        {
            return "{\"product\":" + JsonString(product) + ",\"keys\":[]}";
        }

        private static string JsonString(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        #endregion

        #region Ring File

        /// <summary>One ring file, and which of the two rings it is.</summary>
        private sealed class RingFile
        {
            public RingFile(string name, KeyRingDescriptor descriptor, bool isDevelopment)
            {
                Name = name;
                Descriptor = descriptor;
                IsDevelopment = isDevelopment;
            }

            public string Name { get; }

            public KeyRingDescriptor Descriptor { get; }

            public bool IsDevelopment { get; }
        }

        #endregion
    }
}
