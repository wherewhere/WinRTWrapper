using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using WinRTWrapper.SourceGenerators.Extensions;
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
            foreach (ISymbolWrapper member in GetMembers(source, options.Marshals))
            {
                switch (member)
                {
                    case SymbolWrapper<IMethodSymbol> method:
                        _ = AddMethod(method, builder, options.Marshals, ref needConstructor);
                        break;
                    case SymbolWrapper<IPropertySymbol> property:
                        _ = AddProperty(property, builder, options.Marshals);
                        break;
                    case SymbolWrapper<IEventSymbol> @event:
                        _ = AddEvent(@event, builder, options);
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

        private static IEnumerable<ISymbolWrapper> GetMembers(WrapperType source, ImmutableArray<MarshalType> marshals)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target, GenerateMember member, ImmutableArray<INamedTypeSymbol> interfaces) = source;
            switch (member)
            {
                case SourceGenerators.GenerateMember.All:
                    static IEnumerable<ISymbolWrapper> GetISymbolWrappers(INamedTypeSymbol symbol, INamedTypeSymbol target, ImmutableArray<MarshalType> marshals)
                    {
                        foreach (ISymbol item in target.GetMembers())
                        {
                            if (item.DeclaredAccessibility == Accessibility.Public)
                            {
                                if (symbol.GetMembers().FirstOrDefault(x => IsSameMember(x, item, marshals)) is ISymbol wrapper)
                                {
                                    if (IsPartialMember(wrapper))
                                    {
                                        yield return SymbolWrapper.Create(wrapper, item);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                yield return SymbolWrapper.Create(symbol, item);
                            }
                        }
                    }
                    return GetISymbolWrappers(symbol, target, marshals);
                case SourceGenerators.GenerateMember.Defined:
                    return from wrapper in symbol.GetMembers().Where(IsPartialMember)
                           from traget in target.GetMembers().Where(x => x.DeclaredAccessibility == Accessibility.Public)
                           where IsSameMember(wrapper, traget, marshals)
                           select SymbolWrapper.Create(wrapper, traget);
                case SourceGenerators.GenerateMember.Interface:
                    return from wrapper in (interfaces.Length > 0 ? interfaces : symbol.Interfaces).SelectMany(x => x.GetMembers())
                           from traget in target.GetMembers().Where(x => x.DeclaredAccessibility == Accessibility.Public)
                           where IsSameMember(wrapper, traget, marshals)
                           select SymbolWrapper.Create(wrapper, traget);
                case SourceGenerators.GenerateMember.Defined | SourceGenerators.GenerateMember.Interface:
                    return from wrapper in symbol.GetMembers().Where(IsPartialMember).Concat((interfaces.Length > 0 ? interfaces : symbol.Interfaces).SelectMany(x => x.GetMembers()))
                           from traget in target.GetMembers().Where(x => x.DeclaredAccessibility == Accessibility.Public)
                           where IsSameMember(wrapper, traget, marshals)
                           select SymbolWrapper.Create(wrapper, traget);
                case SourceGenerators.GenerateMember.None:
                default:
                    return [];
            }

            static bool IsPartialMember(ISymbol member)
            {
                return member switch
                {
                    IMethodSymbol method => method.IsPartialDefinition,
                    IPropertySymbol property => property.GetMethod?.IsPartialDefinition == true || property.SetMethod?.IsPartialDefinition == true,
                    IEventSymbol @event => @event.AddMethod?.IsPartialDefinition == true || @event.RemoveMethod?.IsPartialDefinition == true,
                    _ => false
                };
            }

            static bool IsSameMember(ISymbol wrapper, ISymbol target, ImmutableArray<MarshalType> marshals)
            {
                switch (wrapper, target)
                {
                    case (IMethodSymbol w, IMethodSymbol t):
                        if (w.IsStatic == t.IsStatic && w.MethodKind == t.MethodKind && w.Name == t.Name && w.Parameters.Length == t.Parameters.Length)
                        {
                            if (!w.ReturnType.Equals(t.ReturnType, SymbolEqualityComparer.Default))
                            {
                                if (!IsWrapperType([.. w.GetReturnTypeAttributes(), .. t.GetReturnTypeAttributes()], marshals, t.ReturnType, w.ReturnType))
                                {
                                    return false;
                                }
                            }
                            for (int i = 0; i < w.Parameters.Length; i++)
                            {
                                IParameterSymbol wrapperParam = w.Parameters[i];
                                IParameterSymbol targetParam = t.Parameters[i];
                                if (!wrapperParam.Type.Equals(targetParam.Type, SymbolEqualityComparer.Default))
                                {
                                    if (!IsWrapperType([.. wrapperParam.GetAttributes(), .. targetParam.GetAttributes()], marshals, targetParam.Type, wrapperParam.Type))
                                    {
                                        return false;
                                    }
                                }
                            }
                            return true;
                        }
                        return false;
                    case (IPropertySymbol w, IPropertySymbol t):
                        if (w.IsStatic == t.IsStatic && w.Name == t.Name)
                        {
                            if (!w.Type.Equals(t.Type, SymbolEqualityComparer.Default))
                            {
                                if (!IsWrapperType([.. w.GetAttributes(), .. t.GetAttributes()], marshals, t.Type, w.Type))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        return false;
                    case (IEventSymbol w, IEventSymbol t):
                        if (w.IsStatic == t.IsStatic && w.Name == t.Name)
                        {
                            if (!w.Type.Equals(t.Type, SymbolEqualityComparer.Default))
                            {
                                if (!IsWrapperType([.. w.GetAttributes(), .. t.GetAttributes()], marshals, t.Type, w.Type))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }

            static bool IsWrapperType(IEnumerable<AttributeData> attributes, ImmutableArray<MarshalType> marshals, ITypeSymbol original, ITypeSymbol? expect = null)
            {
                static bool IsWrapperType(IEnumerable<AttributeData> attributes, string name, ITypeSymbol original, ITypeSymbol? expect)
                {
                    if (attributes.FirstOrDefault(x =>
                        x.AttributeClass?.Name == name
                        && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                        is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol marshaller }] })
                    {
                        if (marshaller.GetAttributes().FirstOrDefault(x =>
                            x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                            && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                            is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managed }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapper }] }
                            && original.IsSubclassOf(managed) && expect?.IsSubclassOf(wrapper) != false)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                static bool IsWrapper(ITypeSymbol original, ITypeSymbol expect)
                {
                    if (expect.GetAttributes().FirstOrDefault(x =>
                        x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                        && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                        is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managed }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapper }] }
                        && original.IsSubclassOf(managed) && expect.IsSubclassOf(wrapper) != false)
                    {
                        return true;
                    }
                    return false;
                }

                static bool IsMarshalType(ImmutableArray<MarshalType> marshals, ITypeSymbol original, ITypeSymbol? expect)
                {
                    if (marshals.FirstOrDefault(x => original.IsSubclassOf(x.ManagedType) && expect?.IsSubclassOf(x.WrapperType) != false) is MarshalType marshier)
                    {
                        if (marshier is MarshalGenericType generic && original is INamedTypeSymbol symbol)
                        {
                            generic.GenericArguments = symbol.TypeArguments;
                        }
                        return true;
                    }
                    return false;
                }

                return IsWrapperType(attributes, nameof(WinRTWrapperMarshalUsingAttribute), original, expect)
                    || IsWrapperType(original.GetAttributes(), nameof(WinRTWrapperMarshallingAttribute), original, expect)
                    || expect != null && IsWrapper(original, expect)
                    || IsMarshalType(marshals, original, expect);
            }
        }
    }
}
