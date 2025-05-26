using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using WinRTWrapper.CodeAnalysis;
using WinRTWrapper.SourceGenerators.Models;

namespace WinRTWrapper.SourceGenerators
{
    [Generator(LanguageNames.CSharp)]
    public sealed partial class WinRTWrapperGenerator : IIncrementalGenerator
    {
        private const string namespaceName = $"{nameof(WinRTWrapper)}.{nameof(CodeAnalysis)}";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(EmitPostGeneratedType);

            IncrementalValuesProvider<WrapperType?> wrapperTypes =
                context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    $"{namespaceName}.{nameof(GenerateWinRTWrapperAttribute)}",
                    static (x, _) => x is ClassDeclarationSyntax,
                    GetWrapperType)
                .Where(static x => x != null);

            context.RegisterSourceOutput(wrapperTypes, EmitGeneratedType);
        }

        private static WrapperType? GetWrapperType(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (context is { TargetSymbol: INamedTypeSymbol symbol, Attributes: [{ ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol target }] }] })
            {
                return new WrapperType(symbol, target);
            }
            return null;
        }

        private static void EmitGeneratedType(SourceProductionContext context, WrapperType? source)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target) = source!;
            StringBuilder builder = new();
            if (!target.IsStatic)
            {
                _ = builder.AppendLine(
                    $$"""
                            /// <summary>
                            /// The target <see cref="{{target.GetDocumentationCommentId()}}"/> object of the wrapper.
                            /// </summary>
                            private readonly {{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} target;

                            /// <summary>
                            /// Initializes a new instance of the <see cref="{{symbol.GetDocumentationCommentId()}}"/> class with the specified target <see cref="{{target.GetDocumentationCommentId()}}"/> object.
                            /// </summary>
                            /// <param name="target">The target <see cref="{{target.GetDocumentationCommentId()}}"/> object.</param>
                            internal {{symbol.Name}}({{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} target)
                            {
                                this.target = target;
                            }

                    """);
            }
            ImmutableArray<ISymbol> targetMembers = target.GetMembers();
            foreach (ISymbol member in targetMembers)
            {
                switch (member)
                {
                    case not { CanBeReferencedByName: true, DeclaredAccessibility: Accessibility.Public }:
                        continue;
                    case { IsStatic: true }:
                        switch (member)
                        {
                            case IMethodSymbol method:
                                switch (method)
                                {
                                    case { MethodKind: MethodKind.Ordinary }:
                                        _ = builder.AppendLine(
                                            $$"""
                                                    /// <inheritdoc cref="{{method.GetDocumentationCommentId()}}"/>
                                                    public static {{method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{method.Name}}({{string.Join(" ", method.Parameters.Select(x => x.ToDisplayString()))}})
                                                    {
                                                        {{(method.ReturnsVoid ? string.Empty : "return ")}}{{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{method.Name}}({{string.Join(", ", method.Parameters.Select(x => x.Name))}});
                                                    }

                                            """);
                                        break;
                                }
                                break;
                            case IPropertySymbol property:
                                switch (property)
                                {
                                    case { IsReadOnly: true }:
                                        _ = builder.AppendLine(
                                            $$"""
                                                    /// <inheritdoc cref="{{property.GetDocumentationCommentId()}}"/>
                                                    public static {{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{property.Name}}
                                                    {
                                                        get
                                                        {
                                                            return {{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{property.Name}};
                                                        }
                                                    }

                                            """);
                                        break;
                                    case { IsWriteOnly: false }:
                                        _ = builder.AppendLine(
                                            $$"""
                                                    /// <inheritdoc cref="{{property.GetDocumentationCommentId()}}"/>
                                                    public static {{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{property.Name}}
                                                    {
                                                        get
                                                        {
                                                            return {{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{property.Name}};
                                                        }
                                                        set
                                                        {
                                                            {{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{property.Name}} = value;
                                                        }
                                                    }

                                            """);
                                        break;
                                }
                                break;
                        }
                        break;
                    default:
                        switch (member)
                        {
                            case IMethodSymbol method:
                                switch (method)
                                {
                                    case { MethodKind: MethodKind.Constructor }:
                                        _ = builder.AppendLine(
                                            $$"""
                                                    /// <inheritdoc cref="{{method.GetDocumentationCommentId()}}"/>
                                                    public {{symbol.Name}}({{string.Join(" ", method.Parameters.Select(x => x.ToDisplayString()))}})
                                                    {
                                                        this.target = new {{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}({{string.Join(", ", method.Parameters.Select(x => x.Name))}});
                                                    }

                                            """);
                                        break;
                                    case { MethodKind: MethodKind.Ordinary }:
                                        _ = builder.AppendLine(
                                            $$"""
                                                    /// <inheritdoc cref="{{method.GetDocumentationCommentId()}}"/>
                                                    public {{method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{method.Name}}({{string.Join(" ", method.Parameters.Select(x => x.ToDisplayString()))}})
                                                    {
                                                        {{(method.ReturnsVoid ? string.Empty : "return ")}}this.target.{{method.Name}}({{string.Join(", ", method.Parameters.Select(x => x.Name))}});
                                                    }

                                            """);
                                        break;
                                }
                                break;
                            case IPropertySymbol property:
                                switch (property)
                                {
                                    case { IsReadOnly: true }:
                                        _ = builder.AppendLine(
                                            $$"""
                                                    /// <inheritdoc cref="{{property.GetDocumentationCommentId()}}"/>
                                                    public {{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{property.Name}}
                                                    {
                                                        get
                                                        {
                                                            return this.target.{{property.Name}};
                                                        }
                                                    }

                                            """);
                                        break;
                                    case { IsWriteOnly: false }:
                                        _ = builder.AppendLine(
                                            $$"""
                                                    /// <inheritdoc cref="{{property.GetDocumentationCommentId()}}"/>
                                                    public {{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{property.Name}}
                                                    {
                                                        get
                                                        {
                                                            return this.target.{{property.Name}};
                                                        }
                                                        set
                                                        {
                                                            this.target.{{property.Name}} = value;
                                                        }
                                                    }

                                            """);
                                        break;
                                }
                                break;
                        }
                        break;
                }
            }
            string generatedCode =
                $$"""
                // <auto-generated/>
                #pragma warning disable
                namespace {{symbol.ContainingNamespace.ToDisplayString()}}
                {
                    /// <inheritdoc cref="{{target.GetDocumentationCommentId()}}"/>
                    public {{(symbol.IsStatic ? "static" : "sealed")}} partial class {{symbol.Name}}
                    {
                {{builder.ToString().TrimEnd()}}
                    }
                }
                """;
            context.AddSource($"{symbol.Name}.g.cs", generatedCode);
        }
    }
}
