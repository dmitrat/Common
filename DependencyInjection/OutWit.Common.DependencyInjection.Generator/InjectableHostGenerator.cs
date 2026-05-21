using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OutWit.Common.DependencyInjection.Generator
{
    /// <summary>
    /// Emits the boilerplate <see cref="System.IServiceProvider"/> constructor and
    /// matching <c>Services</c> property for every <c>partial class</c> marked
    /// <c>[InjectableHost]</c>. The emitted <c>Services</c> property is the field
    /// that <c>InjectAspect</c>'s field auto-discovery looks for, so the host
    /// class only needs to declare its <c>[Inject]</c> properties.
    /// <para>
    /// The generated constructor is always <c>public</c>, regardless of the host
    /// class's accessibility. This is intentional: the host class's own
    /// accessibility still controls whether external code can name the type to
    /// call <c>new T(sp)</c>, but the public ctor lets MS.DI's default
    /// <c>ActivatorUtilities</c> activate the type via plain
    /// <c>services.AddSingleton&lt;T&gt;()</c>. Without that, an
    /// <c>internal</c> host type would silently fail at resolve time with
    /// "A suitable constructor for type 'T' could not be located".
    /// </para>
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class InjectableHostGenerator : IIncrementalGenerator
    {
        #region Constants

        private const string ATTRIBUTE_FULL_NAME = "OutWit.Common.DependencyInjection.InjectableHostAttribute";

        #endregion

        #region Diagnostics

        private static readonly DiagnosticDescriptor MUST_BE_PARTIAL = new(
            id: "OWDI001",
            title: "[InjectableHost] requires a partial class",
            messageFormat: "Class '{0}' is marked [InjectableHost] but is not partial; the generator cannot extend it",
            category: "OutWit.DI",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MUST_NOT_HAVE_CTOR = new(
            id: "OWDI002",
            title: "[InjectableHost] class declares its own constructor",
            messageFormat: "Class '{0}' is marked [InjectableHost] but already declares a constructor; remove the explicit constructor or drop the attribute",
            category: "OutWit.DI",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        #endregion

        #region IIncrementalGenerator

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ATTRIBUTE_FULL_NAME,
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (ctx, _) => GetHostInfo(ctx))
                .Where(static info => info is not null)
                .Select(static (info, _) => info!.Value);

            context.RegisterSourceOutput(candidates, static (spc, info) =>
            {
                if (!info.IsPartial)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(MUST_BE_PARTIAL, info.Location, info.TypeName));
                    return;
                }

                if (info.HasExplicitConstructor)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(MUST_NOT_HAVE_CTOR, info.Location, info.TypeName));
                    return;
                }

                var source = Emit(info);
                spc.AddSource(info.FileName, SourceText.From(source, Encoding.UTF8));
            });
        }

        #endregion

        #region Tools

        private static HostInfo? GetHostInfo(GeneratorAttributeSyntaxContext ctx)
        {
            if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
                return null;

            var declaration = (ClassDeclarationSyntax)ctx.TargetNode;

            bool isPartial = declaration.Modifiers.Any(m => m.ValueText == "partial");
            bool hasExplicitCtor = typeSymbol.InstanceConstructors
                .Any(c => !c.IsImplicitlyDeclared);

            var ns = typeSymbol.ContainingNamespace?.IsGlobalNamespace == true
                ? null
                : typeSymbol.ContainingNamespace?.ToDisplayString();

            return new HostInfo
            {
                TypeName = typeSymbol.Name,
                Namespace = ns,
                IsPartial = isPartial,
                HasExplicitConstructor = hasExplicitCtor,
                FileName = $"{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.InjectableHost.g.cs"
                    .Replace('<', '{').Replace('>', '}'),
                Location = declaration.Identifier.GetLocation()
            };
        }

        private static string Emit(HostInfo info)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine();

            var indent = "";
            if (!string.IsNullOrEmpty(info.Namespace))
            {
                sb.Append("namespace ").Append(info.Namespace).AppendLine();
                sb.AppendLine("{");
                indent = "    ";
            }

            sb.Append(indent).Append("partial class ").Append(info.TypeName).AppendLine();
            sb.Append(indent).AppendLine("{");

            sb.Append(indent).AppendLine("    #region InjectableHost (generated)");
            sb.AppendLine();

            // Always emit a `public` constructor regardless of the host class's
            // accessibility.
            //
            // Why public on an internal class is correct:
            //   - The class's own accessibility still gates whether an external
            //     caller can name `T` to call `new T(sp)`. A public ctor on an
            //     internal class is effectively still unreachable from outside
            //     the assembly, so there is no widening of the type's contract.
            //   - Microsoft.Extensions.DependencyInjection's default activator
            //     (`ActivatorUtilities`) only considers PUBLIC constructors,
            //     even when registering a type from within its own assembly via
            //     `services.AddSingleton<T>()`. With an internal ctor the
            //     registration silently fails at resolve time with
            //     "A suitable constructor for type 'T' could not be located".
            //   - `[InjectableHost]`'s intent is "make this class DI-resolvable
            //     with zero ceremony". Matching ctor accessibility to class
            //     accessibility breaks that intent for non-public hosts.
            //
            // For nested types where the class is `protected` / `protected
            // internal` / `private protected`: the same reasoning applies —
            // the type-level accessibility still gates `new T(sp)` from
            // outside the enclosing scope, so a public ctor is the most
            // permissive choice within that scope and a no-op outside it.

            sb.Append(indent).Append("    public ").Append(info.TypeName).AppendLine("(global::System.IServiceProvider services)");
            sb.Append(indent).AppendLine("    {");
            sb.Append(indent).AppendLine("        Services = services;");
            sb.Append(indent).AppendLine("    }");
            sb.AppendLine();
            sb.Append(indent).AppendLine("    private global::System.IServiceProvider Services { get; }");
            sb.AppendLine();
            sb.Append(indent).AppendLine("    #endregion");

            sb.Append(indent).AppendLine("}");

            if (!string.IsNullOrEmpty(info.Namespace))
                sb.AppendLine("}");

            return sb.ToString();
        }

        #endregion

        #region Nested Types

        private struct HostInfo
        {
            public string TypeName { get; set; }
            public string? Namespace { get; set; }
            public bool IsPartial { get; set; }
            public bool HasExplicitConstructor { get; set; }
            public string FileName { get; set; }
            public Location Location { get; set; }
        }

        #endregion
    }
}
