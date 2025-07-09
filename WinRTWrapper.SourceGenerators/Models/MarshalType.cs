using Microsoft.CodeAnalysis;
using System;
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
    /// Represents a function that converts a string representation of a managed type to a string representation of a wrapper type, and vice versa, with additional arguments.
    /// </summary>
    /// <param name="inner">The input function that takes an array of strings and returns a string representation of the wrapper type.</param>
    /// <param name="args">The additional arguments to be used in the conversion.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate string MarshalConversionInWithArgsFunction(Func<ImmutableArray<IParameterSymbol>, string> inner, params ImmutableArray<IParameterSymbol> args);

    /// <summary>
    /// Represents a function that converts a string representation of a wrapper type to a string representation of a managed type, with additional arguments.
    /// </summary>
    /// <param name="inner">The input string to be converted.</param>
    /// <param name="args">The additional arguments to be used in the conversion.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate string MarshalConversionOutWithArgsFunction(string inner, params ImmutableArray<IParameterSymbol> args);

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
    internal record MarshalType(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalConversionFunction ConvertToWrapper, MarshalConversionFunction ConvertToManaged): IMarshalType
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

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types, including conversion functions for
    /// </summary>
    /// <param name="ManagedType">The type representing the managed data structure.</param>
    /// <param name="WrapperType">The type representing the wrapper data structure used for marshaling.</param>
    /// <param name="ConvertToWrapper">The function that converts a managed type representation to its wrapper type equivalent.</param>
    /// <param name="ConvertToManaged">The function that converts a wrapper type representation back to its managed type equivalent.</param>
    /// <param name="GenericArguments">The generic arguments used in the type.</param>
    internal sealed record MarshalGenericType(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalConversionFunction ConvertToWrapper, MarshalConversionFunction ConvertToManaged, ImmutableArray<ITypeSymbol> GenericArguments) : MarshalType(ManagedType, WrapperType, ConvertToWrapper, ConvertToManaged), IMarshalGenericType
    {
        /// <summary>
        /// Gets or sets the generic arguments used in the type.
        /// </summary>
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

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types, including conversion functions for
    /// </summary>
    /// <param name="ManagedType">The type representing the managed data structure.</param>
    /// <param name="WrapperType">The type representing the wrapper data structure used for marshaling.</param>
    /// <param name="ConvertToWrapper">The function that converts a managed type representation to its wrapper type equivalent.</param>
    /// <param name="ConvertToManaged">The function that converts a wrapper type representation back to its managed type equivalent.</param>
    /// <param name="ConvertToWrapperWithArgs">The function that converts a managed type representation to its wrapper type equivalent, with additional arguments.</param>
    /// <param name="ConvertToManagedWithArgs">The function that converts a wrapper type representation back to its managed type equivalent, with additional arguments.</param>
    /// <param name="Arguments">The additional arguments used in the type.</param>
    internal record MarshalTypeWithArgs(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalConversionFunction ConvertToWrapper, MarshalConversionFunction ConvertToManaged, MarshalConversionInWithArgsFunction ConvertToWrapperWithArgs, MarshalConversionOutWithArgsFunction ConvertToManagedWithArgs, params ImmutableArray<ITypeSymbol> Arguments) : MarshalType(ManagedType, WrapperType, ConvertToWrapper, ConvertToManaged);

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types, including conversion functions for
    /// </summary>
    /// <param name="ManagedType">The type representing the managed data structure.</param>
    /// <param name="WrapperType">The type representing the wrapper data structure used for marshaling.</param>
    /// <param name="ConvertToWrapper">The function that converts a managed type representation to its wrapper type equivalent.</param>
    /// <param name="ConvertToManaged">The function that converts a wrapper type representation back to its managed type equivalent.</param>
    /// <param name="ConvertToWrapperWithArgs">The function that converts a managed type representation to its wrapper type equivalent, with additional arguments.</param>
    /// <param name="ConvertToManagedWithArgs">The function that converts a wrapper type representation back to its managed type equivalent, with additional arguments.</param>
    /// <param name="GenericArguments">The generic arguments used in the type.</param>
    /// <param name="Arguments">The additional arguments used in the type.</param>
    internal sealed record MarshalGenericTypeWithArgs(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalConversionFunction ConvertToWrapper, MarshalConversionFunction ConvertToManaged, MarshalConversionInWithArgsFunction ConvertToWrapperWithArgs, MarshalConversionOutWithArgsFunction ConvertToManagedWithArgs, ImmutableArray<ITypeSymbol> GenericArguments, params ImmutableArray<ITypeSymbol> Arguments) : MarshalTypeWithArgs(ManagedType, WrapperType, ConvertToWrapper, ConvertToManaged, ConvertToWrapperWithArgs, ConvertToManagedWithArgs, Arguments), IMarshalGenericType
    {
        /// <summary>
        /// Gets or sets the generic arguments used in the type.
        /// </summary>
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

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types, including conversion functions for
    /// </summary>
    internal interface IMarshalType
    {
        /// <summary>
        /// Gets the type representing the managed data structure.
        /// </summary>
        ITypeSymbol ManagedType { get; }

        /// <summary>
        /// Gets the type representing the wrapper data structure used for marshaling.
        /// </summary>
        ITypeSymbol WrapperType { get; }

        /// <summary>
        /// Gets a function that converts a managed type representation to its wrapper type equivalent.
        /// </summary>
        MarshalConversionFunction ConvertToWrapper { get; }

        /// <summary>
        /// Gets a function that converts a wrapper type representation back to its managed type equivalent.
        /// </summary>
        MarshalConversionFunction ConvertToManaged { get; }

        /// <summary>
        /// Gets the fully qualified name of the managed type.
        /// </summary>
        string ManagedTypeName { get; }

        /// <summary>
        /// Gets the fully qualified name of the wrapper type.
        /// </summary>
        string WrapperTypeName { get; }

        /// <summary>
        /// Indicates whether the <see cref="ConvertToWrapper"/> and <see cref="ConvertToManaged"/> functions are defined and can be used for conversion.
        /// </summary>
        bool HasConversion { get; init; }
    }

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types that includes generic arguments.
    /// </summary>
    internal interface IMarshalGenericType : IMarshalType
    {
        /// <summary>
        /// Gets or sets the generic arguments used in the type.
        /// </summary>
        ImmutableArray<ITypeSymbol> GenericArguments { get; set; }
    }
}
