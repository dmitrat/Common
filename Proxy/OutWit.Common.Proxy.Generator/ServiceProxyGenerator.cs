using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OutWit.Common.Proxy.Attributes;
using OutWit.Common.Proxy.Generator.Generators;
using OutWit.Common.Proxy.Generator.Utils;
using OutWit.Common.Proxy.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace OutWit.Common.Proxy.Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class ServiceProxyGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var proxyAttrSymbolProvider =
                context.CompilationProvider.Select((compilation, _) =>
                    compilation.GetTypeByMetadataName(
                        "OutWit.Common.Proxy.Attributes.ProxyTargetAttribute"));

            var targets = context.CompilationProvider
                .Combine(proxyAttrSymbolProvider)
                .Select(static (pair, _) =>
                {
                    var (compilation, proxyAttr) = pair;

                    if (proxyAttr is null)
                        return ImmutableArray<(INamedTypeSymbol Symbol, string ClassName)>.Empty;

                    var results = ImmutableArray.CreateBuilder<(INamedTypeSymbol Symbol, string ClassName)>();

                    void WalkNamespace(INamespaceSymbol ns)
                    {
                        foreach (var type in ns.GetTypeMembers())
                            VisitType(type);

                        foreach (var child in ns.GetNamespaceMembers())
                            WalkNamespace(child);
                    }

                    void VisitType(INamedTypeSymbol type)
                    {
                        if (type.TypeKind == TypeKind.Interface)
                        {
                            var attr = type.GetAttributes()
                                .FirstOrDefault(a =>
                                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, proxyAttr) ||
                                    a.AttributeClass?.Name == nameof(ProxyTargetAttribute));

                            if (attr is not null)
                            {
                                string? nameArgument = null;

                                if (attr.ConstructorArguments.Length > 0)
                                {
                                    var arg = attr.ConstructorArguments[0];
                                    if (!arg.IsNull &&
                                        arg.Value is string s &&
                                        !string.IsNullOrWhiteSpace(s))
                                    {
                                        nameArgument = s;
                                    }
                                }

                                var className = string.IsNullOrEmpty(nameArgument)
                                    ? $"{type.Name}Proxy"
                                    : nameArgument!;

                                results.Add((type, className));
                            }
                        }

                        foreach (var nested in type.GetTypeMembers())
                            VisitType(nested);
                    }

                    WalkNamespace(compilation.GlobalNamespace);
                    return results.ToImmutable();
                });

            context.RegisterSourceOutput(targets, static (spc, interfaces) =>
            {
                foreach (var (symbol, className) in interfaces)
                {
                    GenerateProxy(spc, className, symbol);
                }
            });
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
