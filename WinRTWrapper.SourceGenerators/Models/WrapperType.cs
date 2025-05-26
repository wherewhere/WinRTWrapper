using Microsoft.CodeAnalysis;

namespace WinRTWrapper.SourceGenerators.Models
{
    internal sealed record WrapperType(INamedTypeSymbol Symbol, INamedTypeSymbol Target);
}
