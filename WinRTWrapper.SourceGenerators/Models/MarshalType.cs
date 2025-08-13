using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace WinRTWrapper.SourceGenerators.Models
{
    /// <summary>
    /// Represents a function that converts a string representation of a managed type to a string representation of a wrapper type, and vice versa.
    /// </summary>
    /// <param name="expression">The input string to be converted.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate ExpressionSyntax MarshalConversionFunction(ExpressionSyntax expression);

    /// <summary>
    /// Represents a function that converts a string representation of a managed type to a string representation of a wrapper type, and vice versa, with generic type arguments.
    /// </summary>
    /// <param name="expression">The input string to be converted.</param>
    /// <param name="typeArgs">The generic type arguments to be used in the conversion.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate ExpressionSyntax MarshalGenericConversionFunction(ExpressionSyntax expression, params ImmutableArray<ITypeSymbol> typeArgs);

    /// <summary>
    /// Represents a function that converts a string representation of a managed type to a string representation of a wrapper type, and vice versa, with additional arguments.
    /// </summary>
    /// <param name="expression">The input function that takes an array of strings and returns a string representation of the wrapper type.</param>
    /// <param name="args">The additional arguments to be used in the conversion.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate ExpressionSyntax MarshalConversionInWithArgsFunction(Func<ImmutableArray<IParameterSymbol>, ExpressionSyntax> expression, params ImmutableArray<IParameterSymbol> args);

    /// <summary>
    /// Represents a function that converts a string representation of a wrapper type to a string representation of a managed type, with additional arguments.
    /// </summary>
    /// <param name="expression">The input string to be converted.</param>
    /// <param name="args">The additional arguments to be used in the conversion.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate ExpressionSyntax MarshalConversionOutWithArgsFunction(ExpressionSyntax expression, params ImmutableArray<IParameterSymbol> args);

    /// <summary>
    /// Represents a function that converts a string representation of a managed type to a string representation of a wrapper type, and vice versa, with additional arguments and generic type arguments.
    /// </summary>
    /// <param name="expression">The input function that takes an array of strings and returns a string representation of the wrapper type.</param>
    /// <param name="args">The additional arguments to be used in the conversion.</param>
    /// <param name="typeArgs">The generic type arguments to be used in the conversion.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate ExpressionSyntax MarshalGenericConversionInWithArgsFunction(Func<ImmutableArray<IParameterSymbol>, ExpressionSyntax> expression, ImmutableArray<IParameterSymbol> args, params ImmutableArray<ITypeSymbol> typeArgs);

    /// <summary>
    /// Represents a function that converts a string representation of a wrapper type to a string representation of a managed type, with additional arguments and generic type arguments.
    /// </summary>
    /// <param name="expression">The input string to be converted.</param>
    /// <param name="args">The additional arguments to be used in the conversion.</param>
    /// <param name="typeArgs">The generic type arguments to be used in the conversion.</param>
    /// <returns>The converted string representation of the wrapper type.</returns>
    internal delegate ExpressionSyntax MarshalGenericConversionOutWithArgsFunction(ExpressionSyntax expression, ImmutableArray<IParameterSymbol> args, params ImmutableArray<ITypeSymbol> typeArgs);

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
    internal record MarshalType(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalConversionFunction ConvertToWrapper, MarshalConversionFunction ConvertToManaged) : IMarshalType
    {
        /// <inheritdoc/>
        public virtual string ManagedTypeName => ManagedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        /// <inheritdoc/>
        public virtual string WrapperTypeName => WrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        /// <inheritdoc/>
        public bool HasConversion { get; init; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarshalType"/> record with specified managed and wrapper types, and default conversion functions that return the input unchanged.
        /// </summary>
        public MarshalType(ITypeSymbol managedType, ITypeSymbol wrapperType)
            : this(managedType, wrapperType, FallbackConvert, FallbackConvert) => HasConversion = false;

        /// <summary>
        /// Provides a fallback mechanism for converting the input string.
        /// </summary>
        /// <param name="expression">The input string to be processed.</param>
        /// <returns>The original input string without any modifications.</returns>
        private static ExpressionSyntax FallbackConvert(ExpressionSyntax expression) => expression;

        /// <inheritdoc/>
        ExpressionSyntax IMarshalType.ConvertToWrapper(ExpressionSyntax expression) => ConvertToWrapper(expression);

        /// <inheritdoc/>
        ExpressionSyntax IMarshalType.ConvertToManaged(ExpressionSyntax expression) => ConvertToManaged(expression);
    }

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types, including conversion functions for
    /// </summary>
    /// <param name="ManagedType">The type representing the managed data structure.</param>
    /// <param name="WrapperType">The type representing the wrapper data structure used for marshaling.</param>
    /// <param name="ConvertToWrapper">The function that converts a managed type representation to its wrapper type equivalent.</param>
    /// <param name="ConvertToManaged">The function that converts a wrapper type representation back to its managed type equivalent.</param>
    /// <param name="TypeArguments">The generic arguments used in the type.</param>
    internal record MarshalGenericType(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalGenericConversionFunction ConvertToWrapper, MarshalGenericConversionFunction ConvertToManaged, ImmutableArray<ITypeSymbol> TypeArguments) : IMarshalGenericType
    {
        /// <inheritdoc/>
        public string ManagedTypeName => $"{ManagedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None))}<{string.Join(", ", TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>";

        /// <inheritdoc/>
        public string WrapperTypeName => $"{WrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None))}<{string.Join(", ", TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>";

        /// <inheritdoc/>
        public bool HasConversion { get; init; } = true;

        /// <inheritdoc/>
        ExpressionSyntax IMarshalType.ConvertToWrapper(ExpressionSyntax expression) => ConvertToWrapper(expression, TypeArguments);

        /// <inheritdoc/>
        ExpressionSyntax IMarshalType.ConvertToManaged(ExpressionSyntax expression) => ConvertToManaged(expression, TypeArguments);

        /// <inheritdoc/>
        IMarshalGenericType IMarshalGenericType.WithTypeArguments(ImmutableArray<ITypeSymbol> arguments) => this with { TypeArguments = arguments };
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
    internal sealed record MarshalTypeWithArgs(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalConversionFunction ConvertToWrapper, MarshalConversionFunction ConvertToManaged, MarshalConversionInWithArgsFunction ConvertToWrapperWithArgs, MarshalConversionOutWithArgsFunction ConvertToManagedWithArgs, params ImmutableArray<ITypeSymbol> Arguments)
        : MarshalType(ManagedType, WrapperType, ConvertToWrapper, ConvertToManaged), IMarshalTypeWithArgs
    {
        /// <inheritdoc/>
        ExpressionSyntax IMarshalTypeWithArgs.ConvertToWrapperWithArgs(Func<ImmutableArray<IParameterSymbol>, ExpressionSyntax> expression, params ImmutableArray<IParameterSymbol> args) =>
            ConvertToWrapperWithArgs(expression, args);

        /// <inheritdoc/>
        ExpressionSyntax IMarshalTypeWithArgs.ConvertToManagedWithArgs(ExpressionSyntax expression, params ImmutableArray<IParameterSymbol> args) =>
            ConvertToManagedWithArgs(expression, args);
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
    /// <param name="TypeArguments">The generic arguments used in the type.</param>
    /// <param name="Arguments">The additional arguments used in the type.</param>
    internal sealed record MarshalGenericTypeWithArgs(ITypeSymbol ManagedType, ITypeSymbol WrapperType, MarshalGenericConversionFunction ConvertToWrapper, MarshalGenericConversionFunction ConvertToManaged, MarshalGenericConversionInWithArgsFunction ConvertToWrapperWithArgs, MarshalGenericConversionOutWithArgsFunction ConvertToManagedWithArgs, ImmutableArray<ITypeSymbol> TypeArguments, params ImmutableArray<ITypeSymbol> Arguments) : MarshalGenericType(ManagedType, WrapperType, ConvertToWrapper, ConvertToManaged, TypeArguments), IMarshalGenericTypeWithArgs
    {
        /// <inheritdoc/>
        ExpressionSyntax IMarshalTypeWithArgs.ConvertToWrapperWithArgs(Func<ImmutableArray<IParameterSymbol>, ExpressionSyntax> expression, params ImmutableArray<IParameterSymbol> args) =>
            ConvertToWrapperWithArgs(expression, args, TypeArguments);

        /// <inheritdoc/>
        ExpressionSyntax IMarshalTypeWithArgs.ConvertToManagedWithArgs(ExpressionSyntax expression, params ImmutableArray<IParameterSymbol> args) =>
            ConvertToManagedWithArgs(expression, args, TypeArguments);
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

        /// <summary>
        /// A function that converts a managed type representation to its wrapper type equivalent.
        /// </summary>
        /// <param name="expression">The expression representing the managed type to be converted.</param>
        /// <returns>The expression representing the converted wrapper type.</returns>
        ExpressionSyntax ConvertToWrapper(ExpressionSyntax expression);

        /// <summary>
        /// A function that converts a wrapper type representation back to its managed type equivalent.
        /// </summary>
        /// <param name="expression">The expression representing the wrapper type to be converted.</param>
        /// <returns>The expression representing the converted managed type.</returns>
        ExpressionSyntax ConvertToManaged(ExpressionSyntax expression);
    }

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types that includes generic arguments.
    /// </summary>
    internal interface IMarshalGenericType : IMarshalType
    {
        /// <summary>
        /// Gets or sets the generic arguments used in the type.
        /// </summary>
        ImmutableArray<ITypeSymbol> TypeArguments { get; }

        /// <summary>
        /// Creates a new instance of the type with the specified generic type arguments.
        /// </summary>
        /// <param name="arguments">The generic type arguments to be used in the new instance.</param>
        /// <returns>The new instance of the type with the specified generic type arguments.</returns>
        IMarshalGenericType WithTypeArguments(ImmutableArray<ITypeSymbol> arguments);
    }

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types, including conversion functions for
    /// </summary>
    internal interface IMarshalTypeWithArgs : IMarshalType
    {
        /// <summary>
        /// Gets the additional arguments used in the type.
        /// </summary>
        ImmutableArray<ITypeSymbol> Arguments { get; }

        /// <summary>
        /// A function that converts a managed type representation to its wrapper type equivalent, with additional arguments.
        /// </summary>
        ExpressionSyntax ConvertToWrapperWithArgs(Func<ImmutableArray<IParameterSymbol>, ExpressionSyntax> expression, params ImmutableArray<IParameterSymbol> args);

        /// <summary>
        /// A function that converts a wrapper type representation back to its managed type equivalent, with additional arguments.
        /// </summary>
        ExpressionSyntax ConvertToManagedWithArgs(ExpressionSyntax expression, params ImmutableArray<IParameterSymbol> args);
    }

    /// <summary>
    /// Represents a type used for marshaling between managed and wrapper types that includes generic arguments and additional arguments.
    /// </summary>
    internal interface IMarshalGenericTypeWithArgs : IMarshalTypeWithArgs, IMarshalGenericType;
}
