using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace WinRTWrapper.SourceGenerators.Models
{
    /// <summary>
    /// Represents a wrapper type that encapsulates metadata about a named type, its target type, generated member, and
    /// implemented interfaces.
    /// </summary>
    /// <param name="Symbol">The symbol representing the wrapper type.</param>
    /// <param name="Target">The target type that this wrapper is intended to wrap.</param>
    /// <param name="Member">The type of members to generate in the WinRT wrapper.</param>
    /// <param name="Interfaces">The interfaces that the wrapper type implements.</param>
    internal sealed record WrapperType(INamedTypeSymbol Symbol, INamedTypeSymbol Target, GenerateMember Member, ImmutableArray<INamedTypeSymbol> Interfaces);
}
