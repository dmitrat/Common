using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OutWit.Common.Proxy.Attributes;
using OutWit.Common.Proxy.Generator.Generators;
using OutWit.Common.Proxy.Generator.Utils;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Common.Proxy.Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class ServiceProxyGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilationProvider = context.CompilationProvider;

            context.RegisterSourceOutput(compilationProvider, (spc, compilation) =>
            {
                var proxyTargetAttribute = compilation.GetTypeByMetadataName(typeof(ProxyTargetAttribute).FullName!);
                if (proxyTargetAttribute is null)
                    return;

                var allInterfaces = GetAllInterfaces(compilation.GlobalNamespace);

                foreach (var iface in allInterfaces)
                {
                    var attr = iface.GetAttributes().FirstOrDefault(a =>
                        SymbolEqualityComparer.Default.Equals(a.AttributeClass, proxyTargetAttribute));

                    if (attr is null)
                        continue;

                    var nameArgument = attr.ConstructorArguments.FirstOrDefault().Value as string;
                    var className = string.IsNullOrWhiteSpace(nameArgument) ? $"{iface.Name}Proxy" : nameArgument;

                    GenerateProxy(spc, className!, iface);
                }
            });
        }

        private static IEnumerable<INamedTypeSymbol> GetAllInterfaces(INamespaceSymbol root)
        {
            foreach (var namespaceOrType in root.GetMembers())
            {
                if (namespaceOrType is INamespaceSymbol @namespace)
                {
                    foreach (var nested in GetAllInterfaces(@namespace))
                        yield return nested;
                }
                else if (namespaceOrType is INamedTypeSymbol type && type.TypeKind == TypeKind.Interface)
                {
                    yield return type;
                }
            }
        }

        private static void GenerateProxy(SourceProductionContext context, string className, INamedTypeSymbol interfaceSymbol)
        {
            var namespaceName = interfaceSymbol.ContainingNamespace.ToDisplayString();

            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine($"namespace {namespaceName}");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine($"    public class {className} : {interfaceSymbol.ToDisplayString()}");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine($"        private readonly OutWit.Common.Proxy.Interfaces.{nameof(IProxyInterceptor)} _interceptor;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"        public {className}(OutWit.Common.Proxy.Interfaces.{nameof(IProxyInterceptor)} interceptor)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            _interceptor = interceptor;");
            sourceBuilder.AppendLine("        }");

            foreach (ISymbol member in interfaceSymbol.GetAllMembers())
            {
                if (member is IMethodSymbol method)
                    method.Generate(sourceBuilder);
                
                else if (member is IPropertySymbol property)
                    property.Generate(sourceBuilder);

                else if (member is IEventSymbol eventSymbol)
                    eventSymbol.Generate(sourceBuilder);
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");


            context.AddSource($"{className}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

    }
}
