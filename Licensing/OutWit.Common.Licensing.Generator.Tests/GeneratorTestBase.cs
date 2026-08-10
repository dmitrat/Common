using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OutWit.Common.Licensing.Keys;

namespace OutWit.Common.Licensing.Generator.Tests
{
    /// <summary>
    /// Runs a generator over files rather than over source, and compiles what
    /// comes out against the real licensing library.
    /// <para>
    /// Both halves matter. A generator that emits the right text into a compilation
    /// that never builds is a generator that fails the day the library renames a
    /// method, and the failure lands in a consumer's build rather than here.
    /// </para>
    /// </summary>
    public abstract class GeneratorTestBase
    {
        #region Constants

        /// <summary>
        /// What the generators fall back to when the host supplies no
        /// <c>RootNamespace</c>, which is the case for a bare driver.
        /// </summary>
        protected const string NAMESPACE = "Licensing";

        #endregion

        #region Functions

        /// <summary>
        /// Runs <paramref name="generator"/> over the named files and returns the
        /// compilation it produced, plus its diagnostics.
        /// </summary>
        protected static Compilation Run(IIncrementalGenerator generator,
            IReadOnlyDictionary<string, string> files, out ImmutableArray<Diagnostic> diagnostics,
            bool debug = false)
        {
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                Array.Empty<SyntaxTree>(),
                References(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // The build a customer receives has no DEBUG symbol, and that is the
            // whole mechanism keeping a development key out of it — so which side
            // of #if DEBUG a test stands on is part of what is being tested.
            var parseOptions = new CSharpParseOptions(preprocessorSymbols: debug ? new[] { "DEBUG" } : null);

            var driver = CSharpGeneratorDriver.Create(
                generators: new[] { generator.AsSourceGenerator() },
                additionalTexts: files.Select(file => (AdditionalText)new TextFile(file.Key, file.Value)),
                parseOptions: parseOptions);

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out diagnostics);

            return output;
        }

        /// <summary>Runs, and insists the result both reports nothing and builds.</summary>
        protected static Compilation RunClean(IIncrementalGenerator generator,
            IReadOnlyDictionary<string, string> files, bool debug = false)
        {
            var output = Run(generator, files, out var diagnostics, debug);

            Assert.That(diagnostics, Is.Empty, () => Describe(diagnostics));

            AssertCompiles(output);

            return output;
        }

        /// <summary>The generated text, concatenated — enough to assert on shape.</summary>
        protected static string Sources(Compilation compilation)
        {
            return string.Join("\n", compilation.SyntaxTrees.Select(tree => tree.ToString()));
        }

        /// <summary>
        /// The value of a constant in the generated code, decoded by the compiler
        /// rather than by string surgery on the emitted text.
        /// </summary>
        protected static string? Constant(Compilation compilation, string typeName, string memberName)
        {
            var type = compilation.GetTypeByMetadataName($"{NAMESPACE}.{typeName}");

            Assert.That(type, Is.Not.Null, $"'{typeName}' was not generated");

            var field = type!.GetMembers(memberName).OfType<IFieldSymbol>().FirstOrDefault();

            return field?.ConstantValue as string;
        }

        /// <summary>Fails with the compiler's own words when the generated code does not build.</summary>
        protected static void AssertCompiles(Compilation compilation)
        {
            var errors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.That(errors, Is.Empty, () => Describe(errors));
        }

        #endregion

        #region Tools

        private static IEnumerable<MetadataReference> References()
        {
            var assemblies = new HashSet<Assembly>();

            Collect(typeof(LicenseKeyRing).Assembly, assemblies);
            Collect(typeof(object).Assembly, assemblies);

            return assemblies
                .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
        }

        private static void Collect(Assembly assembly, ISet<Assembly> collected)
        {
            if (assembly.IsDynamic || !collected.Add(assembly))
                return;

            foreach (var name in assembly.GetReferencedAssemblies())
            {
                try
                {
                    Collect(Assembly.Load(name), collected);
                }
                catch
                {
                    // Platform-specific references that will not load here are
                    // also not ones the generated code can use.
                }
            }
        }

        private static string Describe(IEnumerable<Diagnostic> diagnostics)
        {
            var builder = new StringBuilder();

            foreach (var diagnostic in diagnostics)
                builder.AppendLine(diagnostic.ToString());

            return builder.ToString();
        }

        #endregion

        #region Text File

        /// <summary>An <see cref="AdditionalText"/> that lives in the test rather than on disk.</summary>
        private sealed class TextFile : AdditionalText
        {
            private readonly SourceText m_text;

            public TextFile(string path, string text)
            {
                Path = path;
                m_text = SourceText.From(text, Encoding.UTF8);
            }

            public override SourceText GetText(CancellationToken cancellationToken = default)
            {
                return m_text;
            }

            public override string Path { get; }
        }

        #endregion
    }
}
