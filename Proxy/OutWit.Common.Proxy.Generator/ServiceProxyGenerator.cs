using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var targets = context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 },
                    static (ctx, _) =>
                    {
                        var ids = (InterfaceDeclarationSyntax)ctx.Node;
                        var symbol = ctx.SemanticModel.GetDeclaredSymbol(ids);
                        return symbol as INamedTypeSymbol;
                    })
                .Where(static s => s is not null)
                .Select(static (iface, _) =>
                {
                    var attr = iface!.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name == nameof(ProxyTargetAttribute));

                    if (attr is null)
                        return null;

                    var nameArgument = attr.ConstructorArguments.FirstOrDefault().Value as string;
                    var className = string.IsNullOrWhiteSpace(nameArgument) ? $"{iface.Name}Proxy" : nameArgument;

                    return new { Symbol = iface, ClassName = className };
                })
                .Where(static x => x is not null);

            context.RegisterSourceOutput(targets, static (spc, item) =>
            {
                if (item is not null)
                    GenerateProxy(spc, item.ClassName!, item.Symbol);
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
