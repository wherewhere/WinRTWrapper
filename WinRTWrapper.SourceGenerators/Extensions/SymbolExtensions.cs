using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinRTWrapper.SourceGenerators.Extensions
{
    internal static class SymbolExtensions
    {
        /// <summary>
        /// Gets the <typeparamref name="T"/> member's modification string based on whether it is static or instance.
        /// </summary>
        /// <typeparam name="T">The type of the member.</typeparam>
        /// <param name="member">The member symbol.</param>
        /// <returns>The member's modification string.</returns>
        public static string GetMemberModify<T>(this T member) where T : ISymbol =>
            $"public {(member.IsStatic ? "static " : string.Empty)}";

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
            static bool IsEquals<SubT>(SubT type, TBase baseType) where SubT : ITypeSymbol =>
                type.Equals(baseType, SymbolEqualityComparer.Default) || (type, baseType) switch
                {
                    (INamedTypeSymbol { IsGenericType: true } namedTypeSymbol, INamedTypeSymbol { IsGenericType: true }) => namedTypeSymbol.ConstructedFrom.Equals(baseType, SymbolEqualityComparer.Default),
                    _ => false,
                };
            return IsEquals(type, baseType)
                || baseType.TypeKind == TypeKind.Interface && type.AllInterfaces.Any(x => IsEquals(x, baseType))
                || (type.BaseType?.IsSubclassOf(baseType) ?? false);
        }

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
    }
}
