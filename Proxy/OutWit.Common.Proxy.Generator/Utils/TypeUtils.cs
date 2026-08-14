using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace OutWit.Common.Proxy.Generator.Utils
{
    public static class TypeUtils
    {
        /// <summary>
        /// Display format safe for use inside a <c>typeof(...)</c> expression:
        /// fully qualified, keeps generic arguments, drops nullable reference
        /// annotations (which are illegal in <c>typeof</c>).
        /// </summary>
        private static readonly SymbolDisplayFormat TYPEOF_FORMAT = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// C# syntax for the generated declarations and casts. Arrays are spelled out here
        /// for two reasons: <c>ToDisplayString</c> renders a multi-dimensional array as
        /// <c>int[*,*]</c>, which is valid metadata notation but not valid C#; and C# lists
        /// array ranks outermost-first (<c>long[][,]</c>) while reflection lists them
        /// innermost-first (<c>System.Int64[,][]</c>).
        /// </summary>
        public static string GetCSharpTypeSyntax(this ITypeSymbol me)
        {
            if (me is not IArrayTypeSymbol)
                return me.ToDisplayString();

            var ranks = new StringBuilder();
            var current = me;

            while (current is IArrayTypeSymbol array)
            {
                ranks.Append($"[{new string(',', array.Rank - 1)}]");
                current = array.ElementType;
            }

            var nullable = me.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";

            return $"{current.ToDisplayString()}{ranks}{nullable}";
        }

        /// <summary>
        /// Assembly-qualified type name in the exact shape <c>Type.GetType</c> consumes
        /// on the invocation-processing side. Returns an empty string for shapes this
        /// renderer cannot express faithfully — callers must fall back to
        /// <c>typeof(...).AssemblyQualifiedName</c> rather than emit the empty string.
        /// </summary>
        public static string GetTypeString(this ITypeSymbol me)
        {
            var name = me.GetMetadataTypeName();
            if (string.IsNullOrEmpty(name))
                return "";

            var assembly = me.GetDefiningAssembly();

            return string.IsNullOrEmpty(assembly)
                ? name
                : $"{name}, {assembly}";
        }

        /// <summary>
        /// Metadata name as reflection spells it: namespace-qualified, nested types
        /// joined with '+', generic arguments as assembly-qualified bracket groups, and
        /// arrays as the element name followed by their rank brackets (innermost element
        /// first, matching <c>Type.FullName</c>). An empty string means "cannot render" —
        /// pointers, function pointers, and nested types under generic containers.
        /// </summary>
        private static string GetMetadataTypeName(this ITypeSymbol me)
        {
            switch (me)
            {
                // byte[] -> "System.Byte[]", byte[][] -> "System.Byte[][]", byte[,] -> "System.Byte[,]".
                case IArrayTypeSymbol array:
                    var element = array.ElementType.GetMetadataTypeName();
                    return string.IsNullOrEmpty(element)
                        ? ""
                        : $"{element}[{new string(',', array.Rank - 1)}]";

                // 'dynamic' is System.Object at run time; the bare name resolves without an assembly.
                case IDynamicTypeSymbol:
                    return "System.Object";

                case INamedTypeSymbol named:
                    return named.GetNamedMetadataTypeName();

                default:
                    return "";
            }
        }

        private static string GetNamedMetadataTypeName(this INamedTypeSymbol me)
        {
            var containingTypes = me.GetContainingTypeChain();

            // A nested type under a generic container lists the container's type arguments
            // too, which this renderer does not track — let the caller use typeof(...).
            if (containingTypes.Any(type => !SymbolEqualityComparer.Default.Equals(type, me) && type.TypeArguments.Length > 0))
                return "";

            var builder = new StringBuilder();

            if (me.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace)
                builder.Append($"{containingNamespace.ToDisplayString()}.");

            builder.Append(string.Join("+", containingTypes.Select(type => type.MetadataName)));

            if (me.TypeArguments.Length > 0)
                builder.Append($"[{string.Join(",", me.TypeArguments.Select(argument => $"[{argument.GetTypeString()}]"))}]");

            return builder.ToString();
        }

        /// <summary>
        /// The assembly that qualifies the name: for an array that is the element's
        /// assembly (<c>typeof(byte[]).AssemblyQualifiedName</c> names byte's assembly).
        /// </summary>
        private static string GetDefiningAssembly(this ITypeSymbol me)
        {
            if (me is IArrayTypeSymbol array)
                return array.ElementType.GetDefiningAssembly();

            return me.ContainingAssembly?.ToString() ?? "";
        }

        /// <summary>
        /// The type and its containing types, outermost first.
        /// </summary>
        private static IReadOnlyList<INamedTypeSymbol> GetContainingTypeChain(this INamedTypeSymbol me)
        {
            var chain = new List<INamedTypeSymbol>();

            for (var type = me; type != null; type = type.ContainingType)
                chain.Insert(0, type);

            return chain;
        }

        /// <summary>
        /// Produces a C# expression that evaluates to the type-name string consumed by
        /// <c>Type.GetType</c> on the invocation-processing side. Types that are fully
        /// closed at compile time keep the historical literal format; anything that
        /// mentions a method type parameter is only known at run time, so the proxy
        /// resolves it through <c>typeof(...).AssemblyQualifiedName</c>.
        /// </summary>
        public static string GetTypeStringExpression(this ITypeSymbol me)
        {
            if (!me.ContainsTypeParameter())
            {
                var literal = me.GetTypeString();

                // Never emit an empty type string: the receiving side resolves these with
                // Type.GetType, and an empty entry silently degrades into a missing method
                // (parameter) or a null result (return value) instead of a hard failure.
                if (!string.IsNullOrEmpty(literal))
                    return $"\"{literal}\"";
            }

            return $"typeof({me.ToDisplayString(TYPEOF_FORMAT)}).AssemblyQualifiedName!";
        }

        /// <summary>
        /// Recursively checks whether the type mentions a generic type parameter
        /// (directly, as a generic argument, or as an array element).
        /// </summary>
        public static bool ContainsTypeParameter(this ITypeSymbol me)
        {
            switch (me)
            {
                case ITypeParameterSymbol:
                    return true;

                case IArrayTypeSymbol array:
                    return array.ElementType.ContainsTypeParameter();

                case INamedTypeSymbol named:
                    return named.TypeArguments.Any(argument => argument.ContainsTypeParameter());

                default:
                    return false;
            }
        }

        /// <summary>
        /// Rebuilds the `where` clauses of a generic method so the generated proxy
        /// method satisfies the interface declaration.
        /// </summary>
        public static string GetConstraintsString(this ImmutableArray<ITypeParameterSymbol> me)
        {
            var builder = new StringBuilder();

            foreach (var parameter in me)
            {
                var constraints = new List<string>();

                if (parameter.HasReferenceTypeConstraint)
                    constraints.Add("class");

                if (parameter.HasUnmanagedTypeConstraint)
                    constraints.Add("unmanaged");
                else if (parameter.HasValueTypeConstraint)
                    constraints.Add("struct");

                if (parameter.HasNotNullConstraint)
                    constraints.Add("notnull");

                constraints.AddRange(parameter.ConstraintTypes.Select(type => type.ToDisplayString(TYPEOF_FORMAT)));

                if (parameter.HasConstructorConstraint)
                    constraints.Add("new()");

                if (constraints.Count > 0)
                    builder.Append($" where {parameter.Name} : {string.Join(", ", constraints)}");
            }

            return builder.ToString();
        }
    }
}
