using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using WinRTWrapper.SourceGenerators.Extensions;
using WinRTWrapper.SourceGenerators.Models;
using Enumerable = WinRTWrapper.SourceGenerators.Extensions.Enumerable;

namespace WinRTWrapper.SourceGenerators
{
    public partial class WinRTWrapperGenerator
    {
        /// <summary>
        /// Gets the <see cref="MemberDeclarationSyntax"/> for initializing a wrapper class based on the provided <paramref name="source"/> type.
        /// </summary>
        /// <param name="source">The source wrapper type.</param>
        /// <param name="isPublic">Indicates whether the generated member should be public.</param>
        /// <returns>The <see cref="MemberDeclarationSyntax"/> representing the initialization of the wrapper class.</returns>
        private static IEnumerable<MemberDeclarationSyntax> InitBuilder((INamedTypeSymbol, INamedTypeSymbol) source, bool isPublic = false)
        {
            StringBuilder builder = new();
            (INamedTypeSymbol symbol, INamedTypeSymbol target) = source;
            if (!target.IsStatic)
            {
                yield return SyntaxFactory.FieldDeclaration(
                    default,
                    target is { IsReferenceType: true } or { IsReadOnly: true } ?
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)) :
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("target")))))
                    .WithLeadingTrivia(
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.Comment("/// <summary>"),
                            SyntaxFactory.Comment($"/// The target <see cref=\"{target.GetConstructedFromDocumentationCommentId()}\"/> object of the wrapper."),
                            SyntaxFactory.Comment("/// </summary>")));

                yield return SyntaxFactory.ConstructorDeclaration(
                    default,
                    SyntaxFactory.TokenList(isPublic ? SyntaxFactory.Token(SyntaxKind.PublicKeyword) : SyntaxFactory.Token(SyntaxKind.InternalKeyword)),
                    SyntaxFactory.Identifier(symbol.Name),
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Parameter(
                                default,
                                target.IsValueType ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword)) : default,
                                SyntaxFactory.IdentifierName(target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                SyntaxFactory.Identifier("target"),
                                default))),
                    default,
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<StatementSyntax>(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("this.target"),
                                    SyntaxFactory.IdentifierName("target"))))))
                    .WithLeadingTrivia(
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.Comment("/// <summary>"),
                            SyntaxFactory.Comment($"/// Initializes a new instance of the <see cref=\"{symbol.GetConstructedFromDocumentationCommentId()}\"/> class with the specified target <see cref=\"{target.GetConstructedFromDocumentationCommentId()}\"/> object."),
                            SyntaxFactory.Comment("/// </summary>"),
                            SyntaxFactory.Comment($"/// <param name=\"target\">The target <see cref=\"{target.GetConstructedFromDocumentationCommentId()}\"/> object.</param>")));

                foreach (AttributeData attribute in symbol.GetAttributes().Where(x =>
                    x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                    && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName))
                {
                    if (attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managedType }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapperType }])
                    {
                        if (!symbol.GetMembers().OfType<IMethodSymbol>().Any(x => x is { Name: "ConvertToWrapper", Parameters.Length: 1 } && x.Parameters[0].Type.Equals(managedType, SymbolEqualityComparer.Default)))
                        {
                            yield return SyntaxFactory.MethodDeclaration(
                                default,
                                SyntaxFactory.TokenList(
                                    isPublic ? SyntaxFactory.Token(SyntaxKind.PublicKeyword) : SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                                SyntaxFactory.IdentifierName(wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                default,
                                SyntaxFactory.Identifier("ConvertToWrapper"),
                                default,
                                SyntaxFactory.ParameterList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Parameter(
                                            default,
                                            managedType.IsValueType ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword)) : default,
                                            SyntaxFactory.IdentifierName(managedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                            SyntaxFactory.Identifier("managed"),
                                            default))),
                                default,
                                SyntaxFactory.Block(
                                    SyntaxFactory.SingletonList<StatementSyntax>(
                                        SyntaxFactory.ReturnStatement(
                                            SyntaxFactory.CastExpression(
                                                SyntaxFactory.IdentifierName(wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                SyntaxFactory.ObjectCreationExpression(
                                                    SyntaxFactory.IdentifierName(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("managed")))),
                                                    default))))),
                                expressionBody: default)
                                .WithLeadingTrivia(
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.Comment("/// <summary>"),
                                        SyntaxFactory.Comment($"/// Converts a managed type <see cref=\"{managedType.GetConstructedFromDocumentationCommentId()}\"/> to a wrapper type <see cref=\"{wrapperType.GetConstructedFromDocumentationCommentId()}\"/>."),
                                        SyntaxFactory.Comment("/// </summary>"),
                                        SyntaxFactory.Comment("/// <param name=\"managed\">The managed type to convert.</param>"),
                                        SyntaxFactory.Comment("/// <returns>The converted wrapper type.</returns>")));
                        }
                        if (!symbol.GetMembers().OfType<IMethodSymbol>().Any(x => x is { Name: "ConvertToManaged", Parameters.Length: 1 } && x.Parameters[0].Type.Equals(wrapperType, SymbolEqualityComparer.Default)))
                        {
                            yield return SyntaxFactory.MethodDeclaration(
                                default,
                                SyntaxFactory.TokenList(
                                    isPublic ? SyntaxFactory.Token(SyntaxKind.PublicKeyword) : SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                                SyntaxFactory.IdentifierName(managedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                default,
                                SyntaxFactory.Identifier("ConvertToManaged"),
                                default,
                                SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Parameter(SyntaxFactory.Identifier("wrapper")).WithType(SyntaxFactory.IdentifierName(wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))),
                                default,
                                SyntaxFactory.Block(
                                    SyntaxFactory.SingletonList<StatementSyntax>(
                                        SyntaxFactory.ReturnStatement(
                                            SyntaxFactory.CastExpression(
                                                SyntaxFactory.IdentifierName(managedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ParenthesizedExpression(
                                                        SyntaxFactory.CastExpression(
                                                            SyntaxFactory.IdentifierName(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                            SyntaxFactory.IdentifierName("wrapper").WithLeadingTrivia(SyntaxFactory.Space))),
                                                    SyntaxFactory.IdentifierName("target")))))),
                                expressionBody: default)
                                .WithLeadingTrivia(
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.Comment("/// <summary>"),
                                        SyntaxFactory.Comment($"/// Converts a wrapper type <see cref=\"{wrapperType.GetConstructedFromDocumentationCommentId()}\"/> to a managed type <see cref=\"{managedType.GetConstructedFromDocumentationCommentId()}\"/>."),
                                        SyntaxFactory.Comment("/// </summary>"),
                                        SyntaxFactory.Comment("/// <param name=\"wrapper\">The wrapper type to convert.</param>"),
                                        SyntaxFactory.Comment("/// <returns>The converted managed type.</returns>")));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="BaseMethodDeclarationSyntax?"/> for a given <paramref name="source"/> method symbol.
        /// </summary>
        /// <param name="source">The method symbol to process.</param>
        /// <returns>The <see cref="BaseMethodDeclarationSyntax?"/> representing the method.</returns>
        private static BaseMethodDeclarationSyntax? AddMethod(SymbolWrapper<IMethodSymbol> source, ImmutableArray<IMarshalType> marshals, ref bool? needConstructor)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target, IMethodSymbol? wrapper, IMethodSymbol method) = source;
            switch (method)
            {
                case { MethodKind: MethodKind.Constructor }:
                    if (method.Parameters.Length == 0)
                    {
                        needConstructor = false;
                    }
                    else
                    {
                        needConstructor ??= true;
                    }
                    return SyntaxFactory.ConstructorDeclaration(
                        default,
                        SyntaxFactory.TokenList(source.GetMemberModify()),
                        SyntaxFactory.Identifier(symbol.Name),
                        SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(method.Parameters.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.Name)).WithType(SyntaxFactory.IdentifierName(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                        SyntaxFactory.ConstructorInitializer(
                            SyntaxKind.ThisConstructorInitializer,
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        default,
                                        default,
                                        SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.IdentifierName(target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(method.Parameters.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name))))),
                                            default))))),
                        SyntaxFactory.Block())
                        .WithLeadingTrivia(
                            SyntaxFactory.TriviaList(
                                SyntaxFactory.Comment($"/// <inheritdoc cref=\"{method.GetConstructedFromDocumentationCommentId()}\"/>")));
                case { MethodKind: MethodKind.Ordinary }:
                    IMarshalType returnType = GetWrapperType(Enumerable.Unwrap(wrapper?.GetReturnTypeAttributes(), method.GetReturnTypeAttributes()), marshals, method.ReturnType, wrapper?.ReturnType, VarianceKind.Out);
                    static bool CheckArgs(ImmutableArray<ITypeSymbol> arguments, ImmutableArray<IParameterSymbol> parameters)
                    {
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
                            return true;
                        }
                        return false;
                    }
                    if (returnType is IMarshalTypeWithArgs { Arguments: { Length: > 0 } arguments } returnWithArgs && CheckArgs(arguments, method.Parameters))
                    {
                        IEnumerable<(IMarshalType marshal, string name)> parameters = GetParameters(source);
                        IEnumerable<(IMarshalType marshal, string name)> GetParameters(SymbolWrapper<IMethodSymbol> source)
                        {
                            (IMethodSymbol? wrapper, IMethodSymbol target) = source;
                            if (wrapper == null)
                            {
                                return method.Parameters[..^arguments.Length].Select(x => (GetWrapperType(x.GetAttributes(), marshals, x.Type, null, VarianceKind.In), x.Name));
                            }
                            else
                            {
                                IEnumerable<(IMarshalType marshal, string name)> GetParameters(IMethodSymbol wrapper, IMethodSymbol target)
                                {
                                    for (int i = 0; i < wrapper.Parameters.Length; i++)
                                    {
                                        IParameterSymbol wrapperParam = wrapper.Parameters[i];
                                        IParameterSymbol targetParam = target.Parameters[i];
                                        yield return (GetWrapperType(Enumerable.Unwrap(wrapperParam.GetAttributes(), targetParam.GetAttributes()), marshals, targetParam.Type, wrapperParam.Type, VarianceKind.In), wrapperParam.Name);
                                    }
                                }
                                return GetParameters(wrapper, target);
                            }
                        }
                        ExpressionSyntax result = returnWithArgs.ConvertToWrapperWithArgs(
                            args => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(target.GetMemberTarget(method)),
                                    SyntaxFactory.IdentifierName(method.Name)),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        parameters.Select(x =>
                                            SyntaxFactory.Argument(
                                                x.marshal.ConvertToManaged(SyntaxFactory.IdentifierName(x.name))))
                                            .Concat(args.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name))))))),
                            [.. method.Parameters[^arguments.Length..]]);
                        return SyntaxFactory.MethodDeclaration(
                            default,
                            SyntaxFactory.TokenList(source.GetMemberModify()),
                            SyntaxFactory.IdentifierName(returnType.WrapperTypeName),
                            default,
                            SyntaxFactory.Identifier(method.Name),
                            default,
                            SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.name)).WithType(SyntaxFactory.IdentifierName(x.marshal.WrapperTypeName))))),
                            default,
                            SyntaxFactory.Block(
                                SyntaxFactory.SingletonList<StatementSyntax>(
                                    method.ReturnsVoid ?
                                        SyntaxFactory.ExpressionStatement(result) :
                                        SyntaxFactory.ReturnStatement(result))),
                            expressionBody: default)
                            .WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"/// <inheritdoc cref=\"{method.GetConstructedFromDocumentationCommentId()}\"/>")));
                    }
                    else if (method is { IsStatic: false, Parameters.Length: 0, Name: nameof(IDisposable.Dispose), ReturnType.SpecialType: SpecialType.System_Void } && symbol.AllInterfaces.Any(x => x.Name == nameof(System.IDisposable) && x.ContainingNamespace.ToDisplayString() == nameof(System)))
                    {
                        return SyntaxFactory.MethodDeclaration(
                            default,
                            SyntaxFactory.TokenList(source.GetMemberModify()),
                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                            default,
                            SyntaxFactory.Identifier(nameof(IDisposable.Dispose)),
                            default,
                            SyntaxFactory.ParameterList(),
                            default,
                            SyntaxFactory.Block(
                                SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(target.GetMemberTarget(method)),
                                            SyntaxFactory.IdentifierName(nameof(IDisposable.Dispose))))),
                                SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("global::System.GC"),
                                            SyntaxFactory.IdentifierName(nameof(GC.SuppressFinalize))),
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.ThisExpression())))))),
                            expressionBody: default)
                            .WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"/// <inheritdoc cref=\"{method.GetConstructedFromDocumentationCommentId()}\"/>")));
                    }
                    else
                    {
                        IEnumerable<(IMarshalType marshal, string name)> parameters = GetParameters(source);
                        IEnumerable<(IMarshalType marshal, string name)> GetParameters(SymbolWrapper<IMethodSymbol> source)
                        {
                            (IMethodSymbol? wrapper, IMethodSymbol target) = source;
                            if (wrapper == null)
                            {
                                return method.Parameters.Select(x => (GetWrapperType(x.GetAttributes(), marshals, x.Type, null, VarianceKind.In), x.Name));
                            }
                            else
                            {
                                IEnumerable<(IMarshalType marshal, string name)> GetParameters(IMethodSymbol wrapper, IMethodSymbol target)
                                {
                                    for (int i = 0; i < wrapper.Parameters.Length; i++)
                                    {
                                        IParameterSymbol wrapperParam = wrapper.Parameters[i];
                                        IParameterSymbol targetParam = target.Parameters[i];
                                        yield return (GetWrapperType(Enumerable.Unwrap(wrapperParam.GetAttributes(), targetParam.GetAttributes()), marshals, targetParam.Type, wrapperParam.Type, VarianceKind.In), wrapperParam.Name);
                                    }
                                }
                                return GetParameters(wrapper, target);
                            }
                        }
                        ExpressionSyntax result = returnType.ConvertToWrapper(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(target.GetMemberTarget(method)),
                                    SyntaxFactory.IdentifierName(method.Name)),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        parameters.Select(x =>
                                            SyntaxFactory.Argument(
                                                x.marshal.ConvertToManaged(SyntaxFactory.IdentifierName(x.name))))))));
                        return SyntaxFactory.MethodDeclaration(
                            default,
                            SyntaxFactory.TokenList(source.GetMemberModify()),
                            SyntaxFactory.IdentifierName(returnType.WrapperTypeName),
                            default,
                            SyntaxFactory.Identifier(method.Name),
                            default,
                            SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.name)).WithType(SyntaxFactory.IdentifierName(x.marshal.WrapperTypeName))))),
                            default,
                            SyntaxFactory.Block(
                                SyntaxFactory.SingletonList<StatementSyntax>(
                                    method.ReturnsVoid ?
                                        SyntaxFactory.ExpressionStatement(result) :
                                        SyntaxFactory.ReturnStatement(result))),
                            expressionBody: default)
                            .WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"/// <inheritdoc cref=\"{method.GetConstructedFromDocumentationCommentId()}\"/>")));
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="BasePropertyDeclarationSyntax?"/> for a given <paramref name="source"/> property symbol.
        /// </summary>
        /// <param name="source">The property symbol to process.</param>
        /// <returns>The <see cref="BasePropertyDeclarationSyntax?"/> representing the property, or null if the property is write-only or an indexer that does not meet the criteria.</returns>
        private static BasePropertyDeclarationSyntax? AddProperty(SymbolWrapper<IPropertySymbol> source, ImmutableArray<IMarshalType> marshals)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target, IPropertySymbol? wrapper, IPropertySymbol property) = source;
            switch (property)
            {
                case { IsWriteOnly: true }:
                    return null;
                case { IsIndexer: true }:
                    if (property is { IsStatic: false, Parameters.Length: 1 } && symbol.AllInterfaces.Any(x =>
                        x.IsGenericType
                        && x.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
                        && ((x.TypeArguments.Length == 1
                            && property.Parameters[0].Type.SpecialType == SpecialType.System_Int32
                            && (x.MetadataName, property.IsReadOnly) is ("IList`1", false) or ("IReadOnlyList`1", true)
                            && x.TypeArguments[0].Equals(property.Type, SymbolEqualityComparer.Default))
                        || (x.TypeArguments.Length == 2
                            && (x.MetadataName, property.IsReadOnly) is ("IDictionary`2", false) or ("IReadOnlyDictionary`2", true)
                            && x.TypeArguments[0].Equals(property.Parameters[0].Type, SymbolEqualityComparer.Default)
                            && x.TypeArguments[1].Equals(property.Type, SymbolEqualityComparer.Default)))))
                    {
                        IMarshalType returnType = GetWrapperType(Enumerable.Unwrap(wrapper?.GetAttributes(), property.GetAttributes()), marshals, property.Type, wrapper?.Type, VarianceKind.Out);
                        ImmutableArray<(IMarshalType marshal, string name)> parameters = [.. property.Parameters.Select(x => (GetWrapperType(x.GetAttributes(), marshals, x.Type, null, VarianceKind.In), x.Name))];
                        SyntaxList<AccessorDeclarationSyntax> list =
                            SyntaxFactory.SingletonList(
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.SingletonList<StatementSyntax>(
                                            SyntaxFactory.ReturnStatement(
                                                SyntaxFactory.ElementAccessExpression(
                                                    SyntaxFactory.IdentifierName(target.GetMemberTarget(property)),
                                                    SyntaxFactory.BracketedArgumentList(
                                                        SyntaxFactory.SeparatedList(
                                                            parameters.Select(x =>
                                                                SyntaxFactory.Argument(
                                                                    x.marshal.ConvertToManaged(SyntaxFactory.IdentifierName(x.name))))))))))));
                        if (!property.IsReadOnly)
                        {
                            list = list.Add(
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.SetAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.SingletonList<StatementSyntax>(
                                            SyntaxFactory.ExpressionStatement(
                                                SyntaxFactory.AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    SyntaxFactory.ElementAccessExpression(
                                                        SyntaxFactory.IdentifierName(target.GetMemberTarget(property)),
                                                        SyntaxFactory.BracketedArgumentList(
                                                            SyntaxFactory.SeparatedList(
                                                                parameters.Select(x =>
                                                                    SyntaxFactory.Argument(
                                                                        x.marshal.ConvertToManaged(SyntaxFactory.IdentifierName(x.name))))))),
                                                    SyntaxFactory.IdentifierName("value")))))));
                        }
                        return SyntaxFactory.IndexerDeclaration(
                            default,
                            SyntaxFactory.TokenList(source.GetMemberModify()),
                            SyntaxFactory.IdentifierName(returnType.WrapperTypeName),
                            default,
                            SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(parameters.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.name)).WithType(SyntaxFactory.IdentifierName(x.marshal.WrapperTypeName))))),
                            SyntaxFactory.AccessorList(list))
                            .WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"/// <inheritdoc cref=\"{property.GetConstructedFromDocumentationCommentId()}\"/>")));
                    }
                    return null;
                default:
                    VarianceKind variance = property switch
                    {
                        { IsReadOnly: true } => VarianceKind.Out,
                        { IsWriteOnly: true } => VarianceKind.In,
                        _ => VarianceKind.None
                    };
                    IMarshalType marshal = GetWrapperType(Enumerable.Unwrap(wrapper?.GetAttributes(), property.GetAttributes()), marshals, property.Type, wrapper?.Type, variance);
                    SyntaxList<AccessorDeclarationSyntax> _list =
                        SyntaxFactory.SingletonList(
                             SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration,
                                SyntaxFactory.Block(
                                    SyntaxFactory.SingletonList<StatementSyntax>(
                                        SyntaxFactory.ReturnStatement(
                                            marshal.ConvertToWrapper(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName(target.GetMemberTarget(property)),
                                                    SyntaxFactory.IdentifierName(property.Name))))))));
                    if (!property.IsReadOnly)
                    {
                        _list = _list.Add(
                            SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.SetAccessorDeclaration,
                                SyntaxFactory.Block(
                                    SyntaxFactory.SingletonList<StatementSyntax>(
                                        SyntaxFactory.ExpressionStatement(
                                            SyntaxFactory.AssignmentExpression(
                                                SyntaxKind.SimpleAssignmentExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName(target.GetMemberTarget(property)),
                                                    SyntaxFactory.IdentifierName(property.Name)),
                                                marshal.ConvertToManaged(SyntaxFactory.IdentifierName("value"))))))));
                    }
                    return SyntaxFactory.PropertyDeclaration(
                        default,
                        SyntaxFactory.TokenList(source.GetMemberModify()),
                        SyntaxFactory.IdentifierName(marshal.WrapperTypeName),
                        default,
                        SyntaxFactory.Identifier(property.Name),
                        SyntaxFactory.AccessorList(_list))
                        .WithLeadingTrivia(
                            SyntaxFactory.TriviaList(
                                SyntaxFactory.Comment($"/// <inheritdoc cref=\"{property.GetConstructedFromDocumentationCommentId()}\"/>")));
            }
        }

        /// <summary>
        /// Gets the <see cref="MemberDeclarationSyntax"/> for a given <paramref name="source"/> event symbol.
        /// </summary>
        /// <param name="source">The event symbol to process.</param>
        /// <returns>The <see cref="MemberDeclarationSyntax"/> representing the event, or an empty enumerable if the event is not applicable.</returns>
        private static IEnumerable<MemberDeclarationSyntax> AddEvent(SymbolWrapper<IEventSymbol> source, ImmutableArray<IMarshalType> marshals)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target, IEventSymbol? wrapper, IEventSymbol @event) = source;
            IMethodSymbol invoke = @event.Type.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == "Invoke");
            IMarshalType marshal = GetWrapperType(Enumerable.Unwrap(wrapper?.GetAttributes(), @event.GetAttributes()), marshals, @event.Type, wrapper?.Type, VarianceKind.None);
            switch (@event)
            {
                case { IsWindowsRuntimeEvent: true }:
                    yield return SyntaxFactory.FieldDeclaration(
                        default,
                        @event.IsStatic ?
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword)) :
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName("bool"),
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier($"_is_{@event.Name}_EventRegistered"),
                                    default,
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))))))
                        .WithLeadingTrivia(
                            SyntaxFactory.TriviaList(
                                SyntaxFactory.Comment("/// <summary>"),
                                SyntaxFactory.Comment($"/// The singleton flag for the <see cref=\"{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{@event.Name}\"/> event registration."),
                                SyntaxFactory.Comment("/// </summary>")));

                    yield return SyntaxFactory.FieldDeclaration(
                        default,
                        @event.IsStatic ?
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)) :
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.GenericName(
                                SyntaxFactory.Identifier("global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable"),
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                        SyntaxFactory.IdentifierName(@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))),
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier($"_{@event.Name}_EventTable"),
                                    default,
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.GenericName(
                                                SyntaxFactory.Identifier("global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable"),
                                                SyntaxFactory.TypeArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                        SyntaxFactory.IdentifierName(@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))),
                                            SyntaxFactory.ArgumentList(),
                                            default))))))
                        .WithLeadingTrivia(
                            SyntaxFactory.TriviaList(
                                SyntaxFactory.Comment("/// <summary>"),
                                SyntaxFactory.Comment($"/// The event registration token table for the <see cref=\"{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{@event.Name}\"/> event."),
                                SyntaxFactory.Comment("/// </summary>")));

                    yield return SyntaxFactory.EventDeclaration(
                        default,
                        SyntaxFactory.TokenList(source.GetMemberModify()),
                        SyntaxFactory.IdentifierName(marshal.WrapperTypeName),
                        default,
                        SyntaxFactory.Identifier(@event.Name),
                        SyntaxFactory.AccessorList(
                            SyntaxFactory.List([
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.AddAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.IfStatement(
                                            SyntaxFactory.PrefixUnaryExpression(
                                                SyntaxKind.LogicalNotExpression,
                                                SyntaxFactory.IdentifierName($"_is_{@event.Name}_EventRegistered")),
                                            SyntaxFactory.Block(
                                                SyntaxFactory.ExpressionStatement(
                                                    SyntaxFactory.AssignmentExpression(
                                                        SyntaxKind.AddAssignmentExpression,
                                                        SyntaxFactory.MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            SyntaxFactory.IdentifierName(target.GetMemberTarget(@event)),
                                                            SyntaxFactory.IdentifierName(@event.Name)),
                                                        SyntaxFactory.AnonymousMethodExpression(
                                                            SyntaxFactory.ParameterList(
                                                                SyntaxFactory.SeparatedList(invoke.Parameters.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.Name)).WithType(SyntaxFactory.IdentifierName(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                                                            SyntaxFactory.Block(
                                                                SyntaxFactory.LocalDeclarationStatement(
                                                                    SyntaxFactory.VariableDeclaration(
                                                                        SyntaxFactory.IdentifierName(@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                                        SyntaxFactory.SingletonSeparatedList(
                                                                            SyntaxFactory.VariableDeclarator(
                                                                                SyntaxFactory.Identifier("@event"),
                                                                                default,
                                                                                SyntaxFactory.EqualsValueClause(
                                                                                    SyntaxFactory.MemberAccessExpression(
                                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                                        SyntaxFactory.IdentifierName($"_{@event.Name}_EventTable"),
                                                                                        SyntaxFactory.IdentifierName("InvocationList"))))))),
                                                                SyntaxFactory.IfStatement(
                                                                    SyntaxFactory.BinaryExpression(
                                                                        SyntaxKind.NotEqualsExpression,
                                                                        SyntaxFactory.IdentifierName("@event"),
                                                                        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                                                    SyntaxFactory.Block(
                                                                        SyntaxFactory.SingletonList<StatementSyntax>(
                                                                            invoke.ReturnsVoid ?
                                                                                SyntaxFactory.ExpressionStatement(
                                                                                    SyntaxFactory.InvocationExpression(
                                                                                        SyntaxFactory.MemberAccessExpression(
                                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                                            SyntaxFactory.IdentifierName("@event"),
                                                                                            SyntaxFactory.IdentifierName("Invoke")),
                                                                                        SyntaxFactory.ArgumentList(
                                                                                            SyntaxFactory.SeparatedList(invoke.Parameters.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name))))))) :
                                                                                SyntaxFactory.ReturnStatement(
                                                                                    SyntaxFactory.InvocationExpression(
                                                                                        SyntaxFactory.MemberAccessExpression(
                                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                                            SyntaxFactory.IdentifierName("@event"),
                                                                                            SyntaxFactory.IdentifierName("Invoke")),
                                                                                        SyntaxFactory.ArgumentList(
                                                                                            SyntaxFactory.SeparatedList(invoke.Parameters.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name)))))))))),
                                                                SyntaxFactory.ReturnStatement(
                                                                    invoke.ReturnsVoid ? null : SyntaxFactory.DefaultExpression(
                                                                        SyntaxFactory.IdentifierName(invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))))),
                                                SyntaxFactory.ExpressionStatement(
                                                    SyntaxFactory.AssignmentExpression(
                                                        SyntaxKind.SimpleAssignmentExpression,
                                                        SyntaxFactory.IdentifierName($"_is_{@event.Name}_EventRegistered"),
                                                        SyntaxFactory.Token(SyntaxKind.EqualsToken),
                                                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))))),
                                        SyntaxFactory.ReturnStatement(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName($"_{@event.Name}_EventTable"),
                                                    SyntaxFactory.IdentifierName("AddEventHandler")),
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            marshal.ConvertToManaged(SyntaxFactory.IdentifierName("value"))))))))),
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.RemoveAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.SingletonList(
                                            SyntaxFactory.ExpressionStatement(
                                                SyntaxFactory.InvocationExpression(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName($"_{@event.Name}_EventTable"),
                                                        SyntaxFactory.IdentifierName("RemoveEventHandler")),
                                                    SyntaxFactory.ArgumentList(
                                                        SyntaxFactory.SingletonSeparatedList(
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("value")))))))))])))
                        .WithLeadingTrivia(
                            SyntaxFactory.TriviaList(
                                SyntaxFactory.Comment($"/// <inheritdoc cref=\"{@event.GetConstructedFromDocumentationCommentId()}\"/>")));
                    break;
                default:
                    switch (marshal)
                    {
                        case { HasConversion: true }:
                            yield return SyntaxFactory.FieldDeclaration(
                                default,
                                @event.IsStatic ?
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)) :
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                                SyntaxFactory.VariableDeclaration(
                                    SyntaxFactory.GenericName(
                                        SyntaxFactory.Identifier("global::System.Runtime.CompilerServices.ConditionalWeakTable"),
                                        SyntaxFactory.TypeArgumentList(
                                            SyntaxFactory.SeparatedList<TypeSyntax>([
                                                SyntaxFactory.IdentifierName(marshal.WrapperTypeName),
                                                SyntaxFactory.IdentifierName(@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))]))),
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(
                                            SyntaxFactory.Identifier($"_{@event.Name}_EventWeakTable"),
                                            default,
                                            SyntaxFactory.EqualsValueClause(
                                                SyntaxFactory.ObjectCreationExpression(
                                                    SyntaxFactory.GenericName(
                                                        SyntaxFactory.Identifier("global::System.Runtime.CompilerServices.ConditionalWeakTable"),
                                                        SyntaxFactory.TypeArgumentList(
                                                            SyntaxFactory.SeparatedList<TypeSyntax>([
                                                                SyntaxFactory.IdentifierName(marshal.WrapperTypeName),
                                                                SyntaxFactory.IdentifierName(@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))]))),
                                                    SyntaxFactory.ArgumentList(),
                                                    default))))))
                                .WithLeadingTrivia(
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.Comment("/// <summary>"),
                                        SyntaxFactory.Comment($"/// The event weak table for the <see cref=\"{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{@event.Name}\"/> event."),
                                        SyntaxFactory.Comment("/// </summary>")));

                            yield return SyntaxFactory.EventDeclaration(
                                default,
                                SyntaxFactory.TokenList(source.GetMemberModify()),
                                SyntaxFactory.IdentifierName(marshal.WrapperTypeName),
                                default,
                                SyntaxFactory.Identifier(@event.Name),
                                SyntaxFactory.AccessorList(
                                    SyntaxFactory.List([
                                        SyntaxFactory.AccessorDeclaration(
                                            SyntaxKind.AddAccessorDeclaration,
                                            SyntaxFactory.Block(
                                                SyntaxFactory.LocalDeclarationStatement(
                                                    SyntaxFactory.VariableDeclaration(
                                                        SyntaxFactory.IdentifierName(@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                        SyntaxFactory.SingletonSeparatedList(
                                                            SyntaxFactory.VariableDeclarator(
                                                                SyntaxFactory.Identifier("handle"),
                                                                default,
                                                                SyntaxFactory.EqualsValueClause(
                                                                    marshal.ConvertToManaged(SyntaxFactory.IdentifierName("value"))))))),
                                                SyntaxFactory.ExpressionStatement(
                                                    SyntaxFactory.AssignmentExpression(
                                                        SyntaxKind.AddAssignmentExpression,
                                                        SyntaxFactory.MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            SyntaxFactory.IdentifierName(target.GetMemberTarget(@event)),
                                                            SyntaxFactory.IdentifierName(@event.Name)),
                                                        SyntaxFactory.IdentifierName("handle"))),
                                                SyntaxFactory.ExpressionStatement(
                                                    SyntaxFactory.InvocationExpression(
                                                        SyntaxFactory.MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            SyntaxFactory.IdentifierName($"_{@event.Name}_EventWeakTable"),
                                                            SyntaxFactory.IdentifierName("Add")),
                                                        SyntaxFactory.ArgumentList(
                                                            SyntaxFactory.SeparatedList([
                                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value")),
                                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("handle"))])))))),
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.RemoveAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.LocalDeclarationStatement(
                                            SyntaxFactory.VariableDeclaration(
                                                SyntaxFactory.IdentifierName(@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("handle"))))),
                                        SyntaxFactory.IfStatement(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName($"_{@event.Name}_EventWeakTable"),
                                                    SyntaxFactory.IdentifierName("TryGetValue")),
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SeparatedList([
                                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value")),
                                                        SyntaxFactory.Argument(
                                                            default,
                                                            SyntaxFactory.Token(SyntaxKind.OutKeyword),
                                                            SyntaxFactory.IdentifierName("handle"))]))),
                                            SyntaxFactory.Block(
                                                SyntaxFactory.ExpressionStatement(
                                                    SyntaxFactory.AssignmentExpression(
                                                        SyntaxKind.SubtractAssignmentExpression,
                                                        SyntaxFactory.MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            SyntaxFactory.IdentifierName(target.GetMemberTarget(@event)),
                                                            SyntaxFactory.IdentifierName(@event.Name)),
                                                        SyntaxFactory.IdentifierName("handle"))),
                                                SyntaxFactory.ExpressionStatement(
                                                    SyntaxFactory.InvocationExpression(
                                                        SyntaxFactory.MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            SyntaxFactory.IdentifierName($"_{@event.Name}_EventWeakTable"),
                                                            SyntaxFactory.IdentifierName("Remove")),
                                                        SyntaxFactory.ArgumentList(
                                                            SyntaxFactory.SingletonSeparatedList(
                                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value"))))))))))])))
                                .WithLeadingTrivia(
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.Comment($"/// <inheritdoc cref=\"{@event.GetConstructedFromDocumentationCommentId()}\"/>")));
                            break;
                        default:
                            yield return SyntaxFactory.EventDeclaration(
                                default,
                                SyntaxFactory.TokenList(source.GetMemberModify()),
                                SyntaxFactory.IdentifierName(@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                default,
                                SyntaxFactory.Identifier(@event.Name),
                                SyntaxFactory.AccessorList(
                                    SyntaxFactory.List([
                                        SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.AddAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.SingletonList<StatementSyntax>(
                                            SyntaxFactory.ExpressionStatement(
                                                SyntaxFactory.AssignmentExpression(
                                                    SyntaxKind.AddAssignmentExpression,
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName(target.GetMemberTarget(@event)),
                                                        SyntaxFactory.IdentifierName(@event.Name)),
                                                    SyntaxFactory.IdentifierName("value")))))),
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.RemoveAccessorDeclaration,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.SingletonList<StatementSyntax>(
                                            SyntaxFactory.ExpressionStatement(
                                                SyntaxFactory.AssignmentExpression(
                                                    SyntaxKind.SubtractAssignmentExpression,
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName(target.GetMemberTarget(@event)),
                                                        SyntaxFactory.IdentifierName(@event.Name)),
                                                    SyntaxFactory.IdentifierName("value"))))))])))
                                .WithLeadingTrivia(
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.Comment($"/// <inheritdoc cref=\"{@event.GetConstructedFromDocumentationCommentId()}\"/>")));
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets the wrapper type for a given <paramref name="original"/> type based on its attributes.
        /// </summary>
        /// <param name="attributes">The attributes of the type.</param>
        /// <param name="original">The original type symbol.</param>
        /// <returns>The wrapper type and its marshaller if applicable.</returns>
        private static IMarshalType GetWrapperType(IEnumerable<AttributeData> attributes, ImmutableArray<IMarshalType> marshals, ITypeSymbol original, ITypeSymbol? expect = null, VarianceKind variance = VarianceKind.None)
        {
            static bool GetWrapperType(IEnumerable<AttributeData> attributes, string name, ITypeSymbol original, ITypeSymbol? expect, VarianceKind variance, [NotNullWhen(true)] out IMarshalType? marshal)
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
                        marshal = new MarshalType(
                            managed,
                            wrapper,
                            expression => SyntaxFactory.CastExpression(
                                SyntaxFactory.IdentifierName(wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(marshaller.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                        SyntaxFactory.IdentifierName("ConvertToWrapper")),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.CastExpression(
                                                    SyntaxFactory.IdentifierName(managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                    SyntaxFactory.ParenthesizedExpression(expression))))))),
                            expression => SyntaxFactory.CastExpression(
                                SyntaxFactory.IdentifierName(managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(marshaller.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                        SyntaxFactory.IdentifierName("ConvertToManaged")),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.CastExpression(
                                                    SyntaxFactory.IdentifierName(wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                    SyntaxFactory.ParenthesizedExpression(expression))))))));
                        return true;
                    }
                }
                marshal = default;
                return false;
            }

            static bool GetWrapperTypeByExpect(ITypeSymbol original, ITypeSymbol expect, VarianceKind variance, [NotNullWhen(true)] out IMarshalType? marshal)
            {
                if (expect.GetAttributes().FirstOrDefault(x =>
                    x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                    && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                    is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managed }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapper }] }
                    && CheckSuitable(managed, wrapper, original, expect, variance))
                {
                    marshal = new MarshalType(
                        managed,
                        wrapper,
                        expression => SyntaxFactory.CastExpression(
                            SyntaxFactory.IdentifierName(wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(expect.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                    SyntaxFactory.IdentifierName("ConvertToWrapper")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.CastExpression(
                                                SyntaxFactory.IdentifierName(managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                SyntaxFactory.ParenthesizedExpression(expression))))))),
                        expression => SyntaxFactory.CastExpression(
                            SyntaxFactory.IdentifierName(managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(expect.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                    SyntaxFactory.IdentifierName("ConvertToManaged")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.CastExpression(
                                                SyntaxFactory.IdentifierName(wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                                SyntaxFactory.ParenthesizedExpression(expression))))))));
                    return true;
                }
                marshal = default;
                return false;
            }

            static bool GetMarshalType(ImmutableArray<IMarshalType> marshals, ITypeSymbol original, ITypeSymbol? expect, VarianceKind variance, [NotNullWhen(true)] out IMarshalType? marshal)
            {
                if (marshals.FirstOrDefault(x => CheckSuitable(x.ManagedType, x.WrapperType, original, expect, variance)) is IMarshalType marshier)
                {
                    if (marshier is IMarshalGenericType generic && original is INamedTypeSymbol symbol)
                    {
                        marshier = generic.WithTypeArguments(symbol.TypeArguments);
                    }
                    marshal = marshier;
                    return true;
                }
                marshal = default;
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

            return GetWrapperType(attributes, nameof(WinRTWrapperMarshalUsingAttribute), original, expect, variance, out IMarshalType? marshal) ? marshal
                : GetWrapperType(original.GetAttributes(), nameof(WinRTWrapperMarshallingAttribute), original, expect, variance, out marshal) ? marshal
                : expect != null && GetWrapperTypeByExpect(original, expect, variance, out marshal) ? marshal
                : GetMarshalType(marshals, original, expect, variance, out marshal) ? marshal
                : new MarshalType(original, original);
        }
    }
}
