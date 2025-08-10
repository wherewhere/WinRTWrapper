using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using WinRTWrapper.SourceGenerators.Extensions;
using WinRTWrapper.SourceGenerators.Models;
using Enumerable = WinRTWrapper.SourceGenerators.Extensions.Enumerable;

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
            List<MemberDeclarationSyntax> members = [];
            members.AddRange(InitBuilder((symbol, target), !options.IsWinMDObject && !options.IsWinRTComponent));
            bool? needConstructor = null;
            List<ISymbolWrapper> wrappers = [.. GetMembers(source, options.Marshals)];
            for (int i = wrappers.Count - 1; i >= 0;)
            {
                if (wrappers[i] is { Wrapper: IMethodSymbol method })
                {
                    List<SymbolWrapper<IMethodSymbol>> temp = [.. wrappers.OfType<SymbolWrapper<IMethodSymbol>>().Where(x => method.Equals(x.Wrapper, SymbolEqualityComparer.Default) == true)];
                    if (temp.Count > 1)
                    {
                        temp.Remove(temp.OrderByDescending(x => x.Target.Parameters.Length).FirstOrDefault());
                        int count = wrappers.RemoveAll(x => temp.Contains(x));
                        i = i - count + 1;
                        continue;
                    }
                }
                i--;
            }
            foreach (ISymbolWrapper member in wrappers)
            {
                if (options.IsCSWinRT && member.Wrapper is not { ContainingType.TypeKind: not TypeKind.Interface })
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "WRAPPER001",
                            "Missing member definition in wrapper class",
                            "The member '{0}' is not defined in the wrapper class '{1}' which C#/WinRT source generator needs it.",
                            "Wrapper",
                            DiagnosticSeverity.Warning,
                            true,
                            "C#/WinRT source generator needs member definition to generate something important. It is not necessary on built-in WinRT platform.",
                            "https://learn.microsoft.com/windows/apps/develop/platform/csharp-winrt/authoring"),
                        symbol.Locations.FirstOrDefault(),
                        member.Target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        new Dictionary<string, string?>()
                        {
                            { "Name", member.Target.Name }
                        }.ToImmutableDictionary(),
                        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                }
                switch (member)
                {
                    case SymbolWrapper<IMethodSymbol> method:
                        if (AddMethod(method, options.Marshals, ref needConstructor) is MemberDeclarationSyntax methodDeclaration)
                        {
                            members.Add(methodDeclaration);
                        }
                        break;
                    case SymbolWrapper<IPropertySymbol> property:
                        if (AddProperty(property, options.Marshals) is MemberDeclarationSyntax propertyDeclaration)
                        {
                            members.Add(propertyDeclaration);
                        }
                        break;
                    case SymbolWrapper<IEventSymbol> @event:
                        if (AddEvent(@event, options) is MemberDeclarationSyntax eventDeclaration)
                        {
                            members.Add(eventDeclaration);
                        }
                        break;
                }
            }

            if (needConstructor == true)
            {
                members.Add(SyntaxFactory.ConstructorDeclaration(symbol.Name)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword)))
                    .WithBody(SyntaxFactory.Block())
                    .WithLeadingTrivia(SyntaxFactory.TriviaList(
                        SyntaxFactory.Comment("///<summary>"),
                        SyntaxFactory.Comment($"/// Initializes a new instance of the <see cref=\"{symbol.GetDocumentationCommentId()}\"/> class."),
                        SyntaxFactory.Comment("///</summary>"))));
            }

            CompilationUnitSyntax compilationUnitSyntax =
                SyntaxFactory.CompilationUnit()
                    .AddMembers(
                        SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(symbol.ContainingNamespace.ToDisplayString()))
                            .WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment("// <auto-generated/>"),
                                    SyntaxFactory.Trivia(SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword), true)),
                                    SyntaxFactory.Comment(" ")))
                            .AddMembers(
                                SyntaxFactory.ClassDeclaration(symbol.Name)
                                    .AddModifiers(
                                        SyntaxFactory.Token(symbol.DeclaredAccessibility switch { Accessibility.Private => SyntaxKind.InternalKeyword, Accessibility.Public => SyntaxKind.PublicKeyword, _ => SyntaxKind.InternalKeyword }),
                                        SyntaxFactory.Token(symbol.IsStatic ? SyntaxKind.StaticKeyword : SyntaxKind.SealedKeyword),
                                        SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                                    .AddMembers([.. members])))
                    .NormalizeWhitespace();

            context.AddSource($"{symbol.Name}.g.cs", compilationUnitSyntax.GetText(Encoding.UTF8));
        }

        private static IEnumerable<ISymbolWrapper> GetMembers(WrapperType source, ImmutableArray<MarshalType> marshals)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target, GenerateMember member, ImmutableArray<INamedTypeSymbol> interfaces) = source;
            switch (member)
            {
                case SourceGenerators.GenerateMember.All:
                    static IEnumerable<ISymbolWrapper> GetISymbolWrappers(INamedTypeSymbol symbol, INamedTypeSymbol target, ImmutableArray<MarshalType> marshals)
                    {
                        foreach (ISymbol item in target.GetAllMembers())
                        {
                            if (item.DeclaredAccessibility == Accessibility.Public)
                            {
                                if (symbol.GetMembers().FirstOrDefault(x => IsSameMember(x, item, marshals)) is ISymbol wrapper)
                                {
                                    if (IsPartialMember(wrapper))
                                    {
                                        yield return SymbolWrapper.Create(wrapper, item);
                                    }
                                    continue;
                                }
                                yield return SymbolWrapper.Create(symbol, item);
                            }
                        }
                    }
                    return GetISymbolWrappers(symbol, target, marshals);
                case SourceGenerators.GenerateMember.Defined:
                    return from wrapper in symbol.GetMembers().Where(IsPartialMember)
                           from traget in target.GetAllMembers().Where(x => x.DeclaredAccessibility == Accessibility.Public)
                           where IsSameMember(wrapper, traget, marshals)
                           select SymbolWrapper.Create(wrapper, traget);
                case SourceGenerators.GenerateMember.Interface:
                    return from wrapper in (interfaces.Length > 0 ? interfaces : symbol.Interfaces).SelectMany(x => x.GetMembers())
                           from traget in target.GetAllMembers().Where(x => x.DeclaredAccessibility == Accessibility.Public)
                           where IsSameMember(wrapper, traget, marshals)
                           select SymbolWrapper.Create(wrapper, traget);
                case SourceGenerators.GenerateMember.Defined | SourceGenerators.GenerateMember.Interface:
                    return from wrapper in symbol.GetMembers().Where(IsPartialMember).Concat((interfaces.Length > 0 ? interfaces : symbol.Interfaces).SelectMany(x => x.GetMembers()))
                           from traget in target.GetAllMembers().Where(x => x.DeclaredAccessibility == Accessibility.Public)
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
                    IPropertySymbol property => property.IsPartialDefinition,
                    IEventSymbol @event => @event.IsPartialDefinition,
                    _ => false
                };
            }

            static bool IsSameMember(ISymbol wrapper, ISymbol target, ImmutableArray<MarshalType> marshals)
            {
                switch (wrapper, target)
                {
                    case (IMethodSymbol w, IMethodSymbol t):
                        if (w.IsStatic == t.IsStatic && w.MethodKind == t.MethodKind && w.Name == t.Name)
                        {
                            if (w.Parameters.Length == t.Parameters.Length)
                            {
                                if (!w.ReturnType.Equals(t.ReturnType, SymbolEqualityComparer.Default))
                                {
                                    if (!IsWrapperType(Enumerable.Unwrap(w.GetReturnTypeAttributes(), t.GetReturnTypeAttributes()), marshals, t.ReturnType, w.ReturnType, VarianceKind.Out))
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
                                        if (!IsWrapperType(Enumerable.Unwrap(wrapperParam.GetAttributes(), targetParam.GetAttributes()), marshals, targetParam.Type, wrapperParam.Type, VarianceKind.In))
                                        {
                                            return false;
                                        }
                                    }
                                }
                                return true;
                            }
                            else if (w.Parameters.Length < t.Parameters.Length)
                            {
                                if (!w.ReturnType.Equals(t.ReturnType, SymbolEqualityComparer.Default))
                                {
                                    MarshalType returnType = GetWrapperType(Enumerable.Unwrap(w.GetReturnTypeAttributes(), t.GetReturnTypeAttributes()), marshals, t.ReturnType, w.ReturnType, VarianceKind.Out);
                                    if (returnType is MarshalTypeWithArgs { Arguments: { Length: > 0 } arguments } returnWithArgs)
                                    {
                                        ImmutableArray<IParameterSymbol> parameters = t.Parameters;
                                        if (parameters.Length >= arguments.Length)
                                        {
                                            for (int i = 1; i <= arguments.Length; i++)
                                            {
                                                ITypeSymbol argument = arguments[^i];
                                                IParameterSymbol parameter = parameters[^i];
                                                if (!parameter.Type.IsSubclassOf(argument))
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
                                                    if (!IsWrapperType(Enumerable.Unwrap(wrapperParam.GetAttributes(), targetParam.GetAttributes()), marshals, targetParam.Type, wrapperParam.Type, VarianceKind.In))
                                                    {
                                                        return false;
                                                    }
                                                }
                                            }
                                            return true;
                                        }
                                        return false;
                                    }
                                }
                        }
                        }
                        return false;
                    case (IPropertySymbol w, IPropertySymbol t):
                        if (w.IsStatic == t.IsStatic && w.Name == t.Name)
                        {
                            if (!w.Type.Equals(t.Type, SymbolEqualityComparer.Default))
                            {
                                VarianceKind variance = t switch
                                {
                                    { IsReadOnly: true } => VarianceKind.Out,
                                    { IsWriteOnly: true } => VarianceKind.In,
                                    _ => VarianceKind.None
                                };
                                if (!IsWrapperType(Enumerable.Unwrap(w.GetAttributes(), t.GetAttributes()), marshals, t.Type, w.Type, variance))
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
                                if (!IsWrapperType(Enumerable.Unwrap(w.GetAttributes(), t.GetAttributes()), marshals, t.Type, w.Type, VarianceKind.None))
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

            static bool IsWrapperType(IEnumerable<AttributeData> attributes, ImmutableArray<MarshalType> marshals, ITypeSymbol original, ITypeSymbol? expect = null, VarianceKind variance = VarianceKind.None)
            {
                static bool IsWrapperType(IEnumerable<AttributeData> attributes, string name, ITypeSymbol original, ITypeSymbol? expect, VarianceKind variance = VarianceKind.None)
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
                            && CheckSuitable(managed, wrapper, original, expect, variance))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                static bool IsWrapperTypeByExpect(ITypeSymbol original, ITypeSymbol expect, VarianceKind variance = VarianceKind.None)
                {
                    if (expect.GetAttributes().FirstOrDefault(x =>
                        x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                        && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                        is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managed }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapper }] }
                        && CheckSuitable(managed, wrapper, original, expect, variance))
                    {
                        return true;
                    }
                    return false;
                }

                static bool IsMarshalType(ImmutableArray<MarshalType> marshals, ITypeSymbol original, ITypeSymbol? expect, VarianceKind variance = VarianceKind.None)
                {
                    if (marshals.FirstOrDefault(x => CheckSuitable(x.ManagedType, x.WrapperType, original, expect, variance)) is MarshalType marshier)
                    {
                        return true;
                    }
                    return false;
                }

                static bool CheckSuitable(ITypeSymbol managed, ITypeSymbol wrapper, ITypeSymbol original, ITypeSymbol? expect, VarianceKind variance = VarianceKind.None)
                {
                    if (wrapper is INamedTypeSymbol { IsGenericType: true })
                    {
                        if (original is not INamedTypeSymbol { IsGenericType: true } && expect is not INamedTypeSymbol { IsGenericType: true })
                        {
                            return false;
                        }
                    }
                    return original.IsSuitable(managed, variance) && expect?.IsSuitable(wrapper, variance.Negative()) != false;
                }

                return IsWrapperType(attributes, nameof(WinRTWrapperMarshalUsingAttribute), original, expect, variance)
                    || IsWrapperType(original.GetAttributes(), nameof(WinRTWrapperMarshallingAttribute), original, expect, variance)
                    || expect != null && IsWrapperTypeByExpect(original, expect, variance)
                    || IsMarshalType(marshals, original, expect, variance);
            }
        }
    }
}
