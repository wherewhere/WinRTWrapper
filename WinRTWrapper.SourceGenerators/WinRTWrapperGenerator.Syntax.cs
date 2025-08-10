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
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)):
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
                        if (!symbol.GetMembers().OfType<IMethodSymbol>().Any(x=>x is {Name : "ConvertToWrapper", Parameters.Length:1 } && x.Parameters[0].Type.Equals(managedType, SymbolEqualityComparer.Default)))
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
        private static BaseMethodDeclarationSyntax? AddMethod(SymbolWrapper<IMethodSymbol> source, ImmutableArray<MarshalType> marshals, ref bool? needConstructor)
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
                    MarshalType returnType = GetWrapperType(Enumerable.Unwrap(wrapper?.GetReturnTypeAttributes(), method.GetReturnTypeAttributes()), marshals, method.ReturnType, wrapper?.ReturnType, VarianceKind.Out);
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
                    if (returnType is MarshalTypeWithArgs { Arguments: { Length: > 0 } arguments } returnWithArgs && CheckArgs(arguments, method.Parameters))
                    {
                        IEnumerable<(MarshalType marshal, string name)> parameters = GetParameters(source);
                        IEnumerable<(MarshalType marshal, string name)> GetParameters(SymbolWrapper<IMethodSymbol> source)
                        {
                            (IMethodSymbol? wrapper, IMethodSymbol target) = source;
                            if (wrapper == null)
                            {
                                return method.Parameters[..^arguments.Length].Select(x => (GetWrapperType(x.GetAttributes(), marshals, x.Type, null, VarianceKind.In), x.Name));
                            }
                            else
                            {
                                IEnumerable<(MarshalType marshal, string name)> GetParameters(IMethodSymbol wrapper, IMethodSymbol target)
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
                                SyntaxFactory.SingletonList(
                                    SyntaxFactory.ParseStatement($"{(method.ReturnsVoid ? string.Empty : "return ")}{returnWithArgs.ConvertToWrapperWithArgs(args => $"{target.GetMemberTarget(method)}.{method.Name}({string.Join(", ", parameters.Select(x => x.marshal.ConvertToManaged(x.name)).Concat(args.Select(x => x.Name)))})", [.. method.Parameters[^arguments.Length..]])};"))),
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
                                SyntaxFactory.List<StatementSyntax>([
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
                                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.ThisExpression())))))])),
                            expressionBody: default)
                            .WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"/// <inheritdoc cref=\"{method.GetConstructedFromDocumentationCommentId()}\"/>")));
                    }
                    else
                    {
                        IEnumerable<(MarshalType marshal, string name)> parameters = GetParameters(source);
                        IEnumerable<(MarshalType marshal, string name)> GetParameters(SymbolWrapper<IMethodSymbol> source)
                        {
                            (IMethodSymbol? wrapper, IMethodSymbol target) = source;
                            if (wrapper == null)
                            {
                                return method.Parameters.Select(x => (GetWrapperType(x.GetAttributes(), marshals, x.Type, null, VarianceKind.In), x.Name));
                            }
                            else
                            {
                                IEnumerable<(MarshalType marshal, string name)> GetParameters(IMethodSymbol wrapper, IMethodSymbol target)
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
                                    SyntaxFactory.ParseStatement($"{(method.ReturnsVoid ? string.Empty : "return ")}{returnType.ConvertToWrapper($"{target.GetMemberTarget(method)}.{method.Name}({string.Join(", ", parameters.Select(x => x.marshal.ConvertToManaged(x.name)))})")};"))),
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
        /// Adds a <paramref name="property"/> to the generated wrapper class.
        /// </summary>
        /// <param name="source">The property symbol to add.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the property code to.</param>
        /// <returns>The updated <see cref="StringBuilder"/> with the property code added.</returns>
        private static MemberDeclarationSyntax? AddProperty(SymbolWrapper<IPropertySymbol> source, ImmutableArray<MarshalType> marshals)
        {
            StringBuilder builder = new();
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
                        MarshalType returnType = GetWrapperType(Enumerable.Unwrap(wrapper?.GetAttributes(), property.GetAttributes()), marshals, property.Type, wrapper?.Type, VarianceKind.Out);
                        ImmutableArray<(MarshalType marshal, string name)> parameters = [.. property.Parameters.Select(x => (GetWrapperType(x.GetAttributes(), marshals, x.Type, null, VarianceKind.In), x.Name))];
                        _ = builder.AppendLine(handler:
                            $$"""
                                    /// <inheritdoc cref="{{property.GetConstructedFromDocumentationCommentId()}}"/>
                                    {{source.GetMemberModify()}}{{returnType.WrapperTypeName}} this[{{string.Join(" ", parameters.Select(x => $"{x.marshal.WrapperTypeName} {x.name}"))}}]
                                    {
                                        get
                                        {
                                            return this.target[{{string.Join(", ", parameters.Select(x => x.marshal.ConvertToManaged(x.name)))}}];
                                        }
                            """);
                        if (!property.IsReadOnly)
                        {
                            _ = builder.AppendLine(handler:
                                $$"""
                                            set
                                            {
                                                this.target[{{string.Join(", ", parameters.Select(x => x.marshal.ConvertToManaged(x.name)))}}] = {{returnType.ConvertToManaged("value")}};
                                            }
                                """);
                        }
                        _ = builder.AppendLine(
                            """
                                    }

                            """);
                        break;
                    }
                    return SyntaxFactory.ParseMemberDeclaration(builder.ToString());
                default:
                    VarianceKind variance = property switch
                    {
                        { IsReadOnly: true } => VarianceKind.Out,
                        { IsWriteOnly: true } => VarianceKind.In,
                        _ => VarianceKind.None
                    };
                    MarshalType marshal = GetWrapperType(Enumerable.Unwrap(wrapper?.GetAttributes(), property.GetAttributes()), marshals, property.Type, wrapper?.Type, variance);
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <inheritdoc cref="{{property.GetConstructedFromDocumentationCommentId()}}"/>
                                {{source.GetMemberModify()}}{{marshal.WrapperTypeName}} {{property.Name}}
                                {
                                    get
                                    {
                                        return {{marshal.ConvertToWrapper($"{target.GetMemberTarget(property)}.{property.Name}")}};
                                    }
                        """);
                    if (!property.IsReadOnly)
                    {
                        _ = builder.AppendLine(handler:
                            $$"""
                                        set
                                        {
                                            {{target.GetMemberTarget(property)}}.{{property.Name}} = {{marshal.ConvertToManaged("value")}};
                                        }
                            """);
                    }
                    _ = builder.AppendLine(
                        """
                                }

                        """);
                    break;
            }
            return SyntaxFactory.ParseMemberDeclaration(builder.ToString());
        }

        /// <summary>
        /// Adds an <paramref name="event"/> to the generated wrapper class.
        /// </summary>
        /// <param name="source">The event symbol to add.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the event code to.</param>
        /// <returns>The updated <see cref="StringBuilder"/> with the event code added.</returns>
        private static MemberDeclarationSyntax? AddEvent(SymbolWrapper<IEventSymbol> source, GenerationOptions options)
        {
            StringBuilder builder = new();
            (INamedTypeSymbol symbol, INamedTypeSymbol target, IEventSymbol? wrapper, IEventSymbol @event) = source;
            IMethodSymbol invoke = @event.Type.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == "Invoke");
            MarshalType marshal = GetWrapperType(Enumerable.Unwrap(wrapper?.GetAttributes(), @event.GetAttributes()), options.Marshals, @event.Type, wrapper?.Type, VarianceKind.None);
            switch ((options, marshal))
            {
                case ({ IsWinMDObject: true }, { HasConversion: true }):
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <summary>
                                /// The singleton flag for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event registration.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}bool _is_{{@event.Name}}_EventRegistered = false;
                                /// <summary>
                                /// The event registration token table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventTable = new global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetConstructedFromDocumentationCommentId()}}"/>
                                {{@event.GetMemberModify()}}event {{marshal.WrapperTypeName}} {{@event.Name}}
                                {
                                    add
                                    {
                                        if (!_is_{{@event.Name}}_EventRegistered)
                                        {
                                            {{target.GetMemberTarget(@event)}}.{{@event.Name}} += delegate ({{string.Join(", ", invoke.Parameters.Select(x => x.ToDisplayString()))}}) 
                                            {
                                                {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} @event = _{{@event.Name}}_EventTable.InvocationList;
                                                if (@event != null)
                                                {
                                                    {{(invoke.ReturnsVoid ? string.Empty : "return ")}}@event.Invoke({{string.Join(", ", invoke.Parameters.Select(x => x.Name))}});
                                                }
                                                return{{(invoke.ReturnsVoid ? string.Empty : $" default({invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})")}};
                                            };
                                            _is_{{@event.Name}}_EventRegistered = true;
                                        }
                                        return _{{@event.Name}}_EventTable.AddEventHandler({{marshal.ConvertToManaged("value")}});
                                    }
                                    remove
                                    {
                                        _{{@event.Name}}_EventTable.RemoveEventHandler(value);
                                    }
                                }

                        """);
                    break;
                case ({ IsWinMDObject: false }, { HasConversion: true }):
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <summary>
                                /// The event weak table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<{{marshal.WrapperTypeName}}, {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventWeakTable = new global::System.Runtime.CompilerServices.ConditionalWeakTable<{{marshal.WrapperTypeName}}, {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetConstructedFromDocumentationCommentId()}}"/>
                                {{@event.GetMemberModify()}}event {{marshal.WrapperTypeName}} {{@event.Name}}
                                {
                                    add
                                    {
                                        {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} handle = {{marshal.ConvertToManaged("value")}};
                                        {{target.GetMemberTarget(@event)}}.{{@event.Name}} += handle;
                                        _{{@event.Name}}_EventWeakTable.Add(value, handle);
                                    }
                                    remove
                                    {
                                        {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} handle;
                                        if (_{{@event.Name}}_EventWeakTable.TryGetValue(value, out handle))
                                        {
                                            {{target.GetMemberTarget(@event)}}.{{@event.Name}} -= handle;
                                            _{{@event.Name}}_EventWeakTable.Remove(value);
                                        }
                                    }
                                }

                        """);
                    break;
                case ({ IsWinMDObject: true }, { HasConversion: false }):
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <summary>
                                /// The singleton flag for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event registration.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}bool _is_{{@event.Name}}_EventRegistered = false;
                                /// <summary>
                                /// The event registration token table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventTable = new global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetConstructedFromDocumentationCommentId()}}"/>
                                {{@event.GetMemberModify()}}event {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{@event.Name}}
                                {
                                    add
                                    {
                                        if (!_is_{{@event.Name}}_EventRegistered)
                                        {
                                            {{target.GetMemberTarget(@event)}}.{{@event.Name}} += delegate ({{string.Join(", ", invoke.Parameters.Select(x => x.ToDisplayString()))}}) 
                                            {
                                                {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} @event = _{{@event.Name}}_EventTable.InvocationList;
                                                if (@event != null)
                                                {
                                                    {{(invoke.ReturnsVoid ? string.Empty : "return ")}}@event.Invoke({{string.Join(", ", invoke.Parameters.Select(x => x.Name))}});
                                                }
                                                return{{(invoke.ReturnsVoid ? string.Empty : $" default({invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})")}};
                                            };
                                            _is_{{@event.Name}}_EventRegistered = true;
                                        }
                                        return _{{@event.Name}}_EventTable.AddEventHandler(value);
                                    }
                                    remove
                                    {
                                        _{{@event.Name}}_EventTable.RemoveEventHandler(value);
                                    }
                                }

                        """);
                    break;
                case ({ IsWinMDObject: false }, { HasConversion: false }):
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <inheritdoc cref="{{@event.GetConstructedFromDocumentationCommentId()}}"/>
                                {{@event.GetMemberModify()}}event {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{@event.Name}}
                                {
                                    add
                                    {
                                        {{target.GetMemberTarget(@event)}}.{{@event.Name}} += value;
                                    }
                                    remove
                                    {
                                        {{target.GetMemberTarget(@event)}}.{{@event.Name}} -= value;
                                    }
                                }

                        """);
                    break;
            }
            return SyntaxFactory.ParseMemberDeclaration(builder.ToString());
        }

        /// <summary>
        /// Gets the wrapper type for a given <paramref name="original"/> type based on its attributes.
        /// </summary>
        /// <param name="attributes">The attributes of the type.</param>
        /// <param name="original">The original type symbol.</param>
        /// <returns>The wrapper type and its marshaller if applicable.</returns>
        private static MarshalType GetWrapperType(IEnumerable<AttributeData> attributes, ImmutableArray<MarshalType> marshals, ITypeSymbol original, ITypeSymbol? expect = null, VarianceKind variance = VarianceKind.None)
        {
            static bool GetWrapperType(IEnumerable<AttributeData> attributes, string name, ITypeSymbol original, ITypeSymbol? expect, VarianceKind variance, [NotNullWhen(true)] out MarshalType? marshal)
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
                            inner => $"({wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){marshaller.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.ConvertToWrapper(({managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({inner}))",
                            inner => $"({managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){marshaller.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.ConvertToManaged(({wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({inner}))");
                        return true;
                    }
                }
                marshal = default;
                return false;
            }

            static bool GetWrapperTypeByExpect(ITypeSymbol original, ITypeSymbol expect, VarianceKind variance, [NotNullWhen(true)] out MarshalType? marshal)
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
                        inner => $"({wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expect.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.ConvertToWrapper(({managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({inner}))",
                        inner => $"({managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expect.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.ConvertToManaged(({wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({inner}))");
                    return true;
                }
                marshal = default;
                return false;
            }

            static bool GetMarshalType(ImmutableArray<MarshalType> marshals, ITypeSymbol original, ITypeSymbol? expect, VarianceKind variance, [NotNullWhen(true)] out MarshalType? marshal)
            {
                if (marshals.FirstOrDefault(x => CheckSuitable(x.ManagedType, x.WrapperType, original, expect, variance)) is MarshalType marshier)
                {
                    if (marshier is IMarshalGenericType generic && original is INamedTypeSymbol symbol)
                    {
                        generic.GenericArguments = symbol.TypeArguments;
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

            return GetWrapperType(attributes, nameof(WinRTWrapperMarshalUsingAttribute), original, expect, variance, out MarshalType? marshal) ? marshal
                : GetWrapperType(original.GetAttributes(), nameof(WinRTWrapperMarshallingAttribute), original, expect, variance, out marshal) ? marshal
                : expect != null && GetWrapperTypeByExpect(original, expect, variance, out marshal) ? marshal
                : GetMarshalType(marshals, original, expect, variance, out marshal) ? marshal
                : new MarshalType(original, original);
        }
    }
}
