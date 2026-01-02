using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using OutWit.Common.MVVM.WPF.Generator.Generators;
using System.Linq;
using System.Text;

namespace OutWit.Common.MVVM.WPF.Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class WpfDependencyPropertyGenerator : IIncrementalGenerator
    {
        #region Constants

        private const string STYLED_PROPERTY_ATTRIBUTE_NAME = "OutWit.Common.MVVM.Attributes.StyledPropertyAttribute";
        private const string ATTACHED_PROPERTY_ATTRIBUTE_NAME = "OutWit.Common.MVVM.Attributes.AttachedPropertyAttribute";

        #endregion

        #region IIncrementalGenerator

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidateProperties = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsPropertyWithAttribute(node),
                    transform: static (ctx, _) => GetPropertyInfo(ctx))
                .Where(static m => m != null);

            var combined = candidateProperties.Combine(context.CompilationProvider);

            context.RegisterSourceOutput(combined, static (spc, source) =>
            {
                var (propertyInfo, compilation) = source;

                if (propertyInfo == null)
                    return;

                GenerateProperty(spc, propertyInfo.Value, compilation);
            });
        }

        #endregion

        #region Functions

        private static bool IsPropertyWithAttribute(SyntaxNode node)
        {
            return node is PropertyDeclarationSyntax property &&
                   property.AttributeLists.Count > 0;
        }

        private static PropertyInfo? GetPropertyInfo(GeneratorSyntaxContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;
            if (propertySymbol == null)
                return null;

            var attributes = propertySymbol.GetAttributes();

            var styledAttr = attributes.FirstOrDefault(a => 
                a.AttributeClass?.ToDisplayString() == STYLED_PROPERTY_ATTRIBUTE_NAME);

            var attachedAttr = attributes.FirstOrDefault(a => 
                a.AttributeClass?.ToDisplayString() == ATTACHED_PROPERTY_ATTRIBUTE_NAME);

            if (styledAttr == null && attachedAttr == null)
                return null;

            var containingType = propertySymbol.ContainingType;
            if (containingType == null)
                return null;

            return new PropertyInfo
            {
                PropertySymbol = propertySymbol,
                ContainingType = containingType,
                AttributeData = attachedAttr ?? styledAttr!,
                IsAttached = attachedAttr != null
            };
        }

        private static void GenerateProperty(
            SourceProductionContext context,
            PropertyInfo propertyInfo,
            Compilation compilation)
        {
            var propertySymbol = propertyInfo.PropertySymbol;
            var containingType = propertyInfo.ContainingType;

            var generator = new DependencyPropertyGenerator(
                propertySymbol,
                containingType,
                propertyInfo.AttributeData,
                propertyInfo.IsAttached);

            var source = generator.Generate();

            var fileName = $"{containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.{propertySymbol.Name}.g.cs"
                .Replace('<', '{')
                .Replace('>', '}');

            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
        }

        #endregion

        #region Nested Types

        private struct PropertyInfo
        {
            public IPropertySymbol PropertySymbol { get; set; }
            public INamedTypeSymbol ContainingType { get; set; }
            public AttributeData AttributeData { get; set; }
            public bool IsAttached { get; set; }
        }

        #endregion
    }
}
