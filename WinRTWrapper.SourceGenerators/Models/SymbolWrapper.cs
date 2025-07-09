using Microsoft.CodeAnalysis;
using WinRTWrapper.SourceGenerators.Extensions;

namespace WinRTWrapper.SourceGenerators.Models
{
    /// <summary>
    /// Represents a wrapper for a symbol that contains a target symbol and an optional wrapper symbol.
    /// </summary>
    /// <typeparam name="T">The type of the symbol being wrapped, which must implement <see cref="ISymbol"/>.</typeparam>
    /// <param name="Wrapper">The optional wrapper symbol that contains the target symbol.</param>
    /// <param name="Target">The target symbol that is being wrapped.</param>
    internal sealed record SymbolWrapper<T>(T? Wrapper, T Target) : ISymbolWrapper where T : ISymbol
    {
        /// <summary>
        /// Gets the wrapped symbol of the wrapper.
        /// </summary>
        public INamedTypeSymbol WrapperSymbol { get; } = Wrapper?.ContainingType!;

        /// <summary>
        /// Gets the target symbol of the wrapper.
        /// </summary>
        public INamedTypeSymbol TargetSymbol => Target.ContainingType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolWrapper{T}"/> class with the specified wrapper and target symbols.
        /// </summary>
        /// <param name="symbol">The symbol that contains the target symbol.</param>
        /// <param name="target">The target symbol that is being wrapped.</param>
        public SymbolWrapper(INamedTypeSymbol symbol, T target) : this(Wrapper: default, target) => WrapperSymbol = symbol;

        /// <inheritdoc/>
        ISymbol? ISymbolWrapper.Wrapper => Wrapper;

        /// <inheritdoc/>
        ISymbol ISymbolWrapper.Target => Target;

        /// <summary>
        /// Deconstructs the <see cref="SymbolWrapper{T}"/> into its constituent parts.
        /// </summary>
        /// <param name="wrapperSymbol">The symbol of the wrapper.</param>
        /// <param name="targetSymbol">The symbol of the target.</param>
        /// <param name="wrapper">The wrapped symbol, if any.</param>
        /// <param name="target">The target symbol being wrapped.</param>
        public void Deconstruct(out INamedTypeSymbol wrapperSymbol, out INamedTypeSymbol targetSymbol, out T? wrapper, out T target)
        {
            wrapperSymbol = WrapperSymbol;
            targetSymbol = TargetSymbol;
            wrapper = Wrapper;
            target = Target;
        }

        /// <summary>
        /// Gets the member modifier string for the wrapped symbol.
        /// </summary>
        /// <returns>The member modifier string, which includes accessibility, static, and partial definition modifiers.</returns>
        public string GetMemberModify()
        {
            return (Wrapper, Target) switch
            {
                (IMethodSymbol wrapper, _) => $"{wrapper.DeclaredAccessibility.FormatToString()} {(wrapper.IsStatic ? "static " : string.Empty)}{(wrapper.IsPartialDefinition ? "partial " : string.Empty)}",
                (null, T target) => $"public {(target.IsStatic ? "static " : string.Empty)}",
                _ => string.Empty
            };
        }
    }

    /// <summary>
    /// Factory class for creating instances of <see cref="ISymbolWrapper"/> based on the type of symbol.
    /// </summary>
    internal static class SymbolWrapper
    {
        /// <summary>
        /// Creates an instance of <see cref="ISymbolWrapper"/> based on the type of the provided symbol.
        /// </summary>
        /// <typeparam name="T">The type of the symbol being wrapped, which must implement <see cref="ISymbol"/>.</typeparam>
        /// <param name="symbol">The symbol that contains the target symbol.</param>
        /// <param name="target">The target symbol that is being wrapped.</param>
        /// <returns>The created <see cref="ISymbolWrapper"/> instance.</returns>
        public static ISymbolWrapper Create<T>(INamedTypeSymbol symbol, T target) where T : ISymbol => target switch
        {
            IMethodSymbol method => new SymbolWrapper<IMethodSymbol>(symbol, method),
            IPropertySymbol property => new SymbolWrapper<IPropertySymbol>(symbol, property),
            IEventSymbol @event => new SymbolWrapper<IEventSymbol>(symbol, @event),
            _ => new SymbolWrapper<T>(symbol, target),
        };

        /// <summary>
        /// Creates an instance of <see cref="ISymbolWrapper"/> based on the provided wrapper and target symbols.
        /// </summary>
        /// <typeparam name="T">The type of the symbol being wrapped, which must implement <see cref="ISymbol"/>.</typeparam>
        /// <param name="wrapper">The symbol that contains the target symbol.</param>
        /// <param name="target">The target symbol that is being wrapped.</param>
        /// <returns>The created <see cref="ISymbolWrapper"/> instance.</returns>
        public static ISymbolWrapper Create<T>(T wrapper, T target) where T : ISymbol => (wrapper, target) switch
        {
            (IMethodSymbol w, IMethodSymbol t) => new SymbolWrapper<IMethodSymbol>(w, t),
            (IPropertySymbol w, IPropertySymbol t) => new SymbolWrapper<IPropertySymbol>(w, t),
            (IEventSymbol w, IEventSymbol t) => new SymbolWrapper<IEventSymbol>(w, t),
            _ => new SymbolWrapper<T>(wrapper, target),
        };
    }

    /// <summary>
    /// Represents a wrapper for a symbol that contains a target symbol and an optional wrapper symbol.
    /// </summary>
    internal interface ISymbolWrapper
    {
        /// <summary>
        /// Gets the wrapped symbol of the wrapper.
        /// </summary>
        ISymbol? Wrapper { get; }

        /// <summary>
        /// Gets the target symbol of the wrapper.
        /// </summary>
        ISymbol Target { get; }
    }
}
