using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using WinRTWrapper.SourceGenerators.Helpers;
using WinRTWrapper.SourceGenerators.Models;

namespace WinRTWrapper.SourceGenerators.Extensions
{
    internal static class SymbolExtensions
    {
        /// <summary>
        /// Adds the appropriate accessibility modifier token(s) to the given <see cref="SyntaxTokenList"/>.
        /// </summary>
        /// <param name="list">The <see cref="SyntaxTokenList"/> to which the accessibility modifier(s) will be added.</param>
        /// <param name="accessibility">The <see cref="Accessibility"/> value representing the desired accessibility level.</param>
        /// <returns>The updated <see cref="SyntaxTokenList"/> with the added accessibility modifier token(s).</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="accessibility"/> value is not recognized.</exception>
        public static SyntaxTokenList AddAccessibility(this in SyntaxTokenList list, Accessibility accessibility) => accessibility switch
        {
            Accessibility.NotApplicable => list.Add(SyntaxFactory.Token(SyntaxKind.None)),
            Accessibility.Private => list.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
            Accessibility.ProtectedAndInternal or Accessibility.ProtectedAndFriend => list.AddRange([SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)]),
            Accessibility.Protected => list.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)),
            Accessibility.Internal or Accessibility.Friend => list.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword)),
            Accessibility.ProtectedOrInternal or Accessibility.ProtectedOrFriend => list.AddRange([SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.InternalKeyword)]),
            Accessibility.Public => list.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null),
        };

        /// <summary>
        /// Adds the appropriate ref kind token(s) to the given <see cref="SyntaxTokenList"/>.
        /// </summary>
        /// <param name="list">The <see cref="SyntaxTokenList"/> to which the ref kind token(s) will be added.</param>
        /// <param name="refKind">The <see cref="RefKind"/> value representing the desired ref kind.</param>
        /// <returns>The updated <see cref="SyntaxTokenList"/> with the added ref kind token(s).</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static SyntaxTokenList AddRefKind(this in SyntaxTokenList list, RefKind refKind) => refKind switch
        {
            RefKind.None => list,
            RefKind.Ref => list.Add(SyntaxFactory.Token(SyntaxKind.RefKeyword)),
            RefKind.Out => list.Add(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
            RefKind.In => list.Add(SyntaxFactory.Token(SyntaxKind.InKeyword)),
            RefKind.RefReadOnlyParameter => list.AddRange([SyntaxFactory.Token(SyntaxKind.RefKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)]),
            _ => throw new ArgumentOutOfRangeException(nameof(refKind), refKind, null),
        };

        /// <summary>
        /// Gets the member modifier string for the wrapped symbol.
        /// </summary>
        /// <param name="wrapper">The symbol wrapper containing the method symbol.</param>
        /// <returns>The member modifier string, which includes accessibility, static, and partial definition modifiers.</returns>
        public static SyntaxTokenList GetParameterModify<T>(this T parameter) where T : IParameterSymbol
        {
            SyntaxTokenList list = SyntaxFactory.TokenList();
            if (parameter.IsThis) { list = list.Add(SyntaxFactory.Token(SyntaxKind.ThisKeyword)); }
            if (parameter.IsParams) { list = list.Add(SyntaxFactory.Token(SyntaxKind.ParamsKeyword)); }
            if (parameter.ScopedKind == ScopedKind.ScopedRef) { list = list.Add(SyntaxFactory.Token(SyntaxKind.ScopedKeyword)); }
            list = list.AddRefKind(parameter.RefKind);
            return list;
        }

        /// <summary>
        /// Gets the target <typeparamref name="T"/> member's name based on whether it is static or instance.
        /// </summary>
        /// <typeparam name="T">The type of the member.</typeparam>
        /// <param name="target">The target type symbol.</param>
        /// <param name="member">The member symbol.</param>
        /// <returns>The member's target name.</returns>
        public static string GetMemberTarget<T>(this INamedTypeSymbol target, T member) where T : ISymbol =>
            member.IsStatic ? target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : $"this.{nameof(target)}";

        /// <summary>
        /// Determines whether the specified type is a subclass of, or implements, the specified base type.
        /// </summary>
        /// <typeparam name="T">The type to check. Must implement <see cref="ITypeSymbol"/>.</typeparam>
        /// <typeparam name="TBase">The base type to compare against. Must implement <see cref="ITypeSymbol"/>.</typeparam>
        /// <param name="type">The type to evaluate.</param>
        /// <param name="baseType">The base type to check for inheritance or implementation.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is equal to <paramref name="baseType"/>, implements
        /// <paramref name="baseType"/>, or is a subclass of <paramref name="baseType"/>; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool IsSubclassOf<T, TBase>(this T type, TBase baseType)
            where T : ITypeSymbol
            where TBase : ITypeSymbol
        {
            return type.IsSameType(baseType)
                || baseType.TypeKind == TypeKind.Interface && type.AllInterfaces.Any(x => x.IsSameType(baseType))
                || (type.BaseType?.IsSubclassOf(baseType) ?? false);
        }

        /// <summary>
        /// Determines whether the specified type is the same as the specified base type.
        /// </summary>
        /// <typeparam name="TLeft">The type of the left symbol, which must implement <see cref="ITypeSymbol"/>.</typeparam>
        /// <typeparam name="TRight">The type of the right symbol, which must implement <see cref="ITypeSymbol"/>.</typeparam>
        /// <param name="left">The left symbol to compare.</param>
        /// <param name="right">The right symbol to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are the same type; otherwise, <see langword="false"/>.</returns>"
        public static bool IsSameType<TLeft, TRight>(this TLeft left, TRight right)
            where TLeft : ITypeSymbol
            where TRight : ITypeSymbol => (left, right) switch
            {
                (INamedTypeSymbol { IsGenericType: true } leftNamed, INamedTypeSymbol { IsGenericType: true } rightNamed) when leftNamed.TypeArguments.All(x => x is ITypeParameterSymbol) || rightNamed.TypeArguments.All(x => x is ITypeParameterSymbol) => rightNamed.ConstructedFrom.Equals(leftNamed.ConstructedFrom, SymbolEqualityComparer.Default),
                _ => left.Equals(right, SymbolEqualityComparer.Default),
            };

        /// <summary>
        /// Determines whether the specified type is a subclass of, or implements, the specified base type.
        /// </summary>
        /// <typeparam name="T">The type to check. Must implement <see cref="ITypeSymbol"/>.</typeparam>
        /// <param name="type">The type to evaluate.</param>
        /// <param name="baseTypeNameSpace">The namespace of the base type to check for inheritance or implementation.</param>
        /// <param name="baseTypeName">The name of the base type to check for inheritance or implementation.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is equal to base type, implements
        /// base type, or is a subclass of base type; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool IsSubclassOf<T>(this T type, string baseTypeNameSpace, string baseTypeName)
            where T : ITypeSymbol
        {
            static bool IsEquals<SubT>(SubT type, string baseTypeNameSpace, string baseTypeName) where SubT : ITypeSymbol =>
                type.Name == baseTypeName && type.ContainingNamespace.ToDisplayString() == baseTypeNameSpace;
            return IsEquals(type, baseTypeNameSpace, baseTypeName)
                || type.AllInterfaces.Any(x => IsEquals(x, baseTypeNameSpace, baseTypeName))
                || (type.BaseType?.IsSubclassOf(baseTypeNameSpace, baseTypeName) ?? false);
        }

        /// <summary>
        /// Determines whether the specified type is suitable for the target type based on the specified variance kind.
        /// </summary>
        /// <typeparam name="TSource">The type to check. Must implement <see cref="ITypeSymbol"/>.</typeparam>
        /// <typeparam name="TTarget">The target type to compare against. Must implement <see cref="ITypeSymbol"/>.</typeparam>
        /// <param name="type">The type to evaluate.</param>
        /// <param name="target">The target type to check for suitability.</param>
        /// <param name="variance">The variance kind to use for the comparison. Defaults to <see cref="VarianceKind.None"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is suitable for <paramref name="target"/> based on the specified variance; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid <paramref name="variance"/> kind is specified.</exception>
        public static bool IsSuitable<TSource, TTarget>(this TSource type, TTarget target, VarianceKind variance = VarianceKind.None)
            where TSource : ITypeSymbol
            where TTarget : ITypeSymbol => variance switch
            {
                VarianceKind.None => type.IsSameType(target),
                VarianceKind.Out => type.IsSubclassOf(target),
                VarianceKind.In => target.IsSubclassOf(type),
                _ => throw new ArgumentOutOfRangeException(nameof(variance), variance, "Invalid variance kind specified."),
            };

        /// <summary>
        /// Get all the members of this symbol.
        /// </summary>
        /// <returns>An <see cref="ImmutableArray{ISymbol}"/> containing all the members of this symbol. If this symbol has no members,
        /// returns an empty <see cref="ImmutableArray{ISymbol}"/>. Never returns Null.</returns>
        public static ImmutableArray<ISymbol> GetAllMembers<T>(this T symbol) where T : ITypeSymbol
        {
            ImmutableArrayBuilder<ISymbol> builder = ImmutableArrayBuilder<ISymbol>.Rent();
            builder.AddRange(symbol.GetMembers());
            ITypeSymbol type = symbol;
            if (type.BaseType is { TypeKind: TypeKind.Class, SpecialType: SpecialType.None } baseType)
            {
                type = baseType;
                List<ISymbol> temp = [];
                foreach (ISymbol member in type.GetMembers())
                {
                    if (!builder.GetEnumerable().Any(x => x.IsSameMember(member)))
                    {
                        temp.Add(member);
                    }
                }
                builder.AddRange(temp);
            }
            return builder.ToImmutable();
        }

        /// <summary>
        /// Determines whether two symbols are the same member.
        /// </summary>
        /// <typeparam name="TLeft">The type of the left symbol, which must implement <see cref="ISymbol"/>.</typeparam>
        /// <typeparam name="TRight">The type of the right symbol, which must implement <see cref="ISymbol"/>.</typeparam>
        /// <param name="left">The left symbol to compare.</param>
        /// <param name="right">The right symbol to compare.</param>
        /// <returns>True if the two symbols represent the same member; otherwise, false.</returns>
        public static bool IsSameMember<TLeft, TRight>(this TLeft left, TRight right)
            where TLeft : ISymbol
            where TRight : ISymbol => (left, right) switch
            {
                (IMethodSymbol l, IMethodSymbol r) => l.Name == r.Name && l.Parameters.Length == r.Parameters.Length && l.TypeParameters.Length == r.TypeParameters.Length && l.Parameters.Select(x => x.Type).SequenceEqual(r.Parameters.Select(x => x.Type), SymbolEqualityComparer.Default),
                (IPropertySymbol { IsIndexer: true } l, IPropertySymbol { IsIndexer: true } r) => l.Name == r.Name && l.Parameters.Length == r.Parameters.Length && l.Parameters.Select(x => x.Type).SequenceEqual(r.Parameters.Select(x => x.Type), SymbolEqualityComparer.Default),
                (IPropertySymbol l, IPropertySymbol r) => l.Name == r.Name && l.Type.Equals(r.Type, SymbolEqualityComparer.Default),
                (IEventSymbol l, IEventSymbol r) => l.Name == r.Name && l.Type.Equals(r.Type, SymbolEqualityComparer.Default),
                _ => left.Equals(right, SymbolEqualityComparer.Default),
            };

        /// <summary>
        /// Retrieves the documentation comment ID for a symbol, accounting for its constructed form.
        /// </summary>
        /// <typeparam name="T">The type of the symbol, which must implement <see cref="ISymbol"/>.</typeparam>
        /// <param name="symbol">The symbol for which to retrieve the documentation comment ID. This can represent a type, method, or other
        /// code element.</param>
        /// <returns>A string containing the documentation comment ID for the symbol, or <see langword="null"/> if the symbol
        /// does not have a documentation comment ID.</returns>
        public static string? GetConstructedFromDocumentationCommentId<T>(this T symbol) where T : ISymbol
        {
            switch (symbol)
            {
                case INamedTypeSymbol namedTypeSymbol:
                    return namedTypeSymbol.ConstructedFrom.GetDocumentationCommentId();
                case { ContainingType: { IsGenericType: true } container }:
                    if (container.ConstructedFrom.GetMembers(symbol.Name).FirstOrDefault(
                        symbol is IMethodSymbol method
                        ? x => x is IMethodSymbol m && m.MethodKind == method.MethodKind && m.Parameters.Length == method.Parameters.Length && m.TypeParameters.Length == method.TypeParameters.Length
                        : x => x.Kind == symbol.Kind) is ISymbol member)
                    {
                        return member.GetDocumentationCommentId();
                    }
                    goto default;
                case IMethodSymbol methodSymbol:
                    return methodSymbol.ConstructedFrom.GetDocumentationCommentId();
                default:
                    return symbol.GetDocumentationCommentId();
            }
        }

        /// <summary>
        /// Returns the negative variance of the specified <see cref="VarianceKind"/>.
        /// </summary>
        /// <param name="variance">The <see cref="VarianceKind"/> to negate.</param>
        /// <returns>The negative variance of the specified <see cref="VarianceKind"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid <paramref name="variance"/> kind is specified.</exception>
        public static VarianceKind Negative(this VarianceKind variance) => variance switch
        {
            VarianceKind.None => VarianceKind.None,
            VarianceKind.Out => VarianceKind.In,
            VarianceKind.In => VarianceKind.Out,
            _ => throw new ArgumentOutOfRangeException(nameof(variance), variance, "Invalid variance kind specified."),
        };

        /// <summary>
        /// Find the first child element matching a given predicate, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The root element.</param>
        /// <param name="predicate">The predicate to use to match the child nodes.</param>
        /// <returns>The child that was found, or <see langword="null"/>.</returns>
        private static T? FindChild<T>(this SyntaxNode element, Func<T, bool>? predicate = null)
            where T : SyntaxNode
        {
            foreach (SyntaxNode child in element.ChildNodes())
            {
                if (child is T result && predicate?.Invoke(result) != false)
                {
                    return result;
                }

                T? descendant = FindChild(child, predicate);

                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        /// <summary>
        /// Find the first child (or self) element matching a given predicate, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The root element.</param>
        /// <param name="predicate">The predicate to use to match the child nodes.</param>
        /// <returns>The child (or self) that was found, or <see langword="null"/>.</returns>
        public static T? FindChildOrSelf<T>(this SyntaxNode element, Func<T, bool>? predicate = null)
            where T : SyntaxNode => element is T result && predicate?.Invoke(result) != false ? result : FindChild(element, predicate);
    }
}
