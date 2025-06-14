﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using WinRTWrapper.SourceGenerators.Models;

namespace WinRTWrapper.SourceGenerators
{
    [Generator(LanguageNames.CSharp)]
    public sealed partial class WinRTWrapperGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// The namespace name for the WinRT wrapper generator attributes.
        /// </summary>
        private const string namespaceName = $"{nameof(WinRTWrapper)}.{nameof(Microsoft.CodeAnalysis)}";

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(EmitPostGeneratedType);

            // Prepare all the generation options in a single incremental model
            IncrementalValueProvider<GenerationOptions> generationOptions =
                context.AnalyzerConfigOptionsProvider
                .Combine(context.CompilationProvider)
                .Select(GetGenerationOptions);

            IncrementalValuesProvider<WrapperType?> wrapperTypes =
                context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    $"{namespaceName}.{nameof(GenerateWinRTWrapperAttribute)}",
                    static (x, _) => x is ClassDeclarationSyntax,
                    GetWrapperType)
                .Where(static x => x is not null);

            context.RegisterSourceOutput(wrapperTypes.Combine(generationOptions), EmitGeneratedType);
        }

        /// <summary>
        /// Gets the <see cref="WrapperType"/> from the given <see cref="GeneratorAttributeSyntaxContext"/>.
        /// </summary>
        /// <param name="context">The context containing the attribute syntax and symbol information.</param>
        /// <param name="token">The cancellation token for the operation.</param>
        /// <returns>The <see cref="WrapperType"/> if the attribute is valid, otherwise null.</returns>
        private static WrapperType? GetWrapperType(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (context is { TargetSymbol: INamedTypeSymbol symbol, Attributes: [{ ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol target }, ..] args }] })
            {
                ImmutableArray<INamedTypeSymbol> interfaces = args is [_, .., { Kind: TypedConstantKind.Array, Values: ImmutableArray<TypedConstant> values }] ? [.. values.SelectMany<TypedConstant, INamedTypeSymbol>(x => x is { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol target } ? [target] : [])] : [];
                GenerateMember member = args is [_, { Kind: TypedConstantKind.Enum, Value: int value }, ..] ? (GenerateMember)value : interfaces.Length > 0 ? SourceGenerators.GenerateMember.Interface : SourceGenerators.GenerateMember.All;
                return new WrapperType(symbol, target, member, interfaces);
            }
            return null;
        }

        /// <summary>
        /// Emits the source for a given <see cref="GeneratedType"/> object.
        /// </summary>
        /// <param name="context">The input <see cref="SourceProductionContext"/> instance to use to emit code.</param>
        /// <param name="info">The tuple containing the <see cref="WrapperType"/> and <see cref="GenerationOptions"/>.</param>
        private static void EmitGeneratedType(SourceProductionContext context, (WrapperType?, GenerationOptions) info)
        {
            (WrapperType? source, GenerationOptions options) = info;
            (INamedTypeSymbol symbol, INamedTypeSymbol target, _, _) = source!;
            StringBuilder builder = InitBuilder((symbol, target));
            bool? needConstructor = null;
            foreach (ISymbol member in GetMembers(source))
            {
                switch (member)
                {
                    case IMethodSymbol method:
                        _ = AddMethod((symbol, target), method, builder, options.Marshals, ref needConstructor);
                        break;
                    case IPropertySymbol property:
                        _ = AddProperty((symbol, target), property, builder, options.Marshals);
                        break;
                    case IEventSymbol @event:
                        _ = AddEvent((symbol, target), @event, builder, options);
                        break;
                }
            }

            if (needConstructor == true)
            {
                _ = builder.Append(handler:
                    $$"""
                            /// Initializes a new instance of the <see cref="{{symbol.GetDocumentationCommentId()}}"/> class.
                            internal {{symbol.Name}}() { }
                    """);
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

        private static IEnumerable<ISymbol> GetMembers(WrapperType source)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target, GenerateMember member, ImmutableArray<INamedTypeSymbol> interfaces) = source;
            switch (member)
            {
                case SourceGenerators.GenerateMember.None:
                    yield break;
                case SourceGenerators.GenerateMember.All:
                    {
                        foreach (ISymbol item in target.GetMembers())
                        {
                            if (item.DeclaredAccessibility == Accessibility.Public)
                            {
                                yield return item;
                            }
                        }

                        break;
                    }
                default:
                    List<ISymbol> members = [..(member switch
                    {
                        SourceGenerators.GenerateMember.Defined => symbol.GetMembers(),
                        SourceGenerators.GenerateMember.Interface => (interfaces.Length > 0 ? interfaces : symbol.Interfaces).SelectMany(x => x.GetMembers()),
                        SourceGenerators.GenerateMember.Defined | SourceGenerators.GenerateMember.Interface => (interfaces.Length > 0 ? interfaces : symbol.Interfaces).SelectMany(x => x.GetMembers()).Concat(symbol.GetMembers()),
                        _ => []
                    }).Where(x => x.DeclaredAccessibility == Accessibility.Public)];
                    foreach (ISymbol item in target.GetMembers())
                    {
                        if (item.DeclaredAccessibility == Accessibility.Public)
                        {
                            if (members.Exists(x => x.IsStatic == item.IsStatic && x.Kind == item.Kind && x.Name == item.Name))
                            {
                                yield return item;
                            }
                        }
                    }
                    break;
            }
        }
    }
}
