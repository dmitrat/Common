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

        public static string GetTypeString(this ITypeSymbol me)
        {
            var dd = me as INamedTypeSymbol;
            if (dd == null)
                return "";

            var result = $"{me.ContainingNamespace}.{me.MetadataName}";

            if (dd.TypeArguments.Length >0 )
                result = $"{result}[{string.Join(",", dd.TypeArguments.Select(x => $"[{x.GetTypeString()}]"))}]";


            return $"{result}, {me.ContainingAssembly}";

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
                return $"\"{me.GetTypeString()}\"";

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
