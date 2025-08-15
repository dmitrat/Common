using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Common.Proxy.Generator.Tests
{
    public abstract class GeneratorTestBase
    {
        protected string RunGenerator(string source, out ImmutableArray<Diagnostic> diagnostics)
        {
            // Set of assemblies to be loaded, using a HashSet to avoid duplicates.
            var loadedAssemblies = new HashSet<Assembly>();

            // Recursively load all references starting from our main project assembly.
            var rootAssembly = typeof(IProxyInterceptor).Assembly;
            LoadAllReferences(rootAssembly, loadedAssemblies);

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { CSharpSyntaxTree.ParseText(source) },
                // Create MetadataReference from the collected assemblies.
                loadedAssemblies.Select(assembly => MetadataReference.CreateFromFile(assembly.Location)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new ServiceProxyGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out diagnostics);

            // Check for any critical compilation errors, which can help debug issues.
            var compilationErrors = outputCompilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            if (compilationErrors.Any())
            {
                var errors = string.Join("\n", compilationErrors.Select(e => e.GetMessage()));
                Assert.Fail($"Compilation failed with errors: \n{errors}");
            }

            return outputCompilation.SyntaxTrees.LastOrDefault()?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Recursively loads an assembly and all of its dependencies into a set.
        /// </summary>
        /// <param name="assembly">The assembly to load.</param>
        /// <param name="loadedAssemblies">A set to store the loaded assemblies to avoid duplicates and infinite loops.</param>
        private void LoadAllReferences(Assembly assembly, ISet<Assembly> loadedAssemblies)
        {
            // Don't load dynamic assemblies and don't load the same assembly twice.
            if (assembly.IsDynamic || !loadedAssemblies.Add(assembly))
            {
                return;
            }

            foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
            {
                try
                {
                    // Load the referenced assembly and recurse.
                    var referencedAssembly = Assembly.Load(referencedAssemblyName);
                    LoadAllReferences(referencedAssembly, loadedAssemblies);
                }
                catch
                {
                    // Ignore assemblies that can't be loaded (e.g., platform-specific ones).
                }
            }
        }
    }
}