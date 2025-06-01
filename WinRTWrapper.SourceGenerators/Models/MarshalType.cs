using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace WinRTWrapper.SourceGenerators.Models
{
    /// <summary>
    /// Represents a function that converts a string representation of a managed type to a string representation of a wrapper type, and vice versa.
    /// </summary>
    /// <param name="inner">The input string to be converted.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate string MarshalConversionFunction(string inner);

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types, including conversion functions for
    /// transforming data between the two.
    /// </summary>
    /// <param name="ManagedType">The type representing the managed data structure.</param>
    /// <param name="WrapperType">The type representing the wrapper data structure used for marshaling.</param>
    /// <param name="ConvertToWrapper">A function that converts a managed type representation to its wrapper type  equivalent. The input is a string
    /// representation of the managed type, and the output is a string representation of the wrapper type.</param>
    /// <param name="ConvertToManaged">A function that converts a wrapper type representation back to its managed  type equivalent. The input is a
    /// string representation of the wrapper type, and the output is a string representation of the managed type.</param>
    internal record MarshalType(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalConversionFunction ConvertToWrapper, MarshalConversionFunction ConvertToManaged)
    {
        /// <summary>
        /// Gets the fully qualified name of the managed type.
        /// </summary>
        public virtual string ManagedTypeName => ManagedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        /// <summary>
        /// Gets the fully qualified name of the wrapper type.
        /// </summary>
        public virtual string WrapperTypeName => WrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        /// <summary>
        /// Indicates whether the <see cref="ConvertToWrapper"/> and <see cref="ConvertToManaged"/> functions are defined and can be used for conversion.
        /// </summary>
        public bool HasConversion { get; init; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarshalType"/> record with specified managed and wrapper types, and default conversion functions that return the input unchanged.
        /// </summary>
        public MarshalType(ITypeSymbol managedType, ITypeSymbol wrapperType)
            : this(managedType, wrapperType, FallbackConvert, FallbackConvert) => HasConversion = false;

        /// <summary>
        /// Provides a fallback mechanism for converting the input string.
        /// </summary>
        /// <param name="input">The input string to be processed.</param>
        /// <returns>The original input string without any modifications.</returns>
        private static string FallbackConvert(string input) => input;
    }

    internal record MarshalGenericType(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalConversionFunction ConvertToWrapper, MarshalConversionFunction ConvertToManaged, ImmutableArray<ITypeSymbol> GenericArguments) : MarshalType(ManagedType, WrapperType, ConvertToWrapper, ConvertToManaged)
    {
        public ImmutableArray<ITypeSymbol> GenericArguments { get; set; } = GenericArguments;

        /// <summary>
        /// Gets the fully qualified name of the managed type with generic arguments.
        /// </summary>
        public override string ManagedTypeName => $"{ManagedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None))}<{string.Join(", ", GenericArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>";

        /// <summary>
        /// Gets the fully qualified name of the wrapper type with generic arguments.
        /// </summary>
        public override string WrapperTypeName => $"{WrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None))}<{string.Join(", ", GenericArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>";
    }
}
