﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides a handler used by the language compiler to process interpolated strings into <see cref="string"/> instances.
    /// </summary>
    [InterpolatedStringHandler]
    internal readonly ref struct DefaultInterpolatedStringHandler
    {
        /// <summary>
        /// Expected average length of formatted data used for an individual interpolation expression result.
        /// </summary>
        /// <remarks>
        /// This is inherited from string.Format, and could be changed based on further data.
        /// string.Format actually uses `format.Length + args.Length * 8`, but format.Length
        /// includes the format items themselves, e.g. "{0}", and since it's rare to have double-digit
        /// numbers of items, we bump the 8 up to 11 to account for the three extra characters in "{d}",
        /// since the compiler-provided base length won't include the equivalent character count.
        /// </remarks>
        private const int GuessedLengthPerHole = 11;
        /// <summary>
        /// Minimum size array to rent from the pool.
        /// </summary>
        /// <remarks>Same as stack-allocation size used today by string.Format.</remarks>
        private const int MinimumArrayPoolLength = 256;

        /// <summary>
        /// The associated <see cref="StringBuilder"/> to which to append.
        /// </summary>
        private readonly StringBuilder _stringBuilder;

        /// <summary>
        /// Optional provider to pass to IFormattable.ToString or ISpanFormattable.TryFormat calls.
        /// </summary>
        private readonly IFormatProvider? _provider;

        /// <summary>
        /// Whether <see cref="_provider"/> provides an ICustomFormatter.
        /// </summary>
        /// <remarks>
        /// Custom formatters are very rare.  We want to support them, but it's ok if we make them more expensive
        /// in order to make them as pay-for-play as possible.  So, we avoid adding another reference type field
        /// to reduce the size of the handler and to reduce required zero'ing, by only storing whether the provider
        /// provides a formatter, rather than actually storing the formatter.  This in turn means, if there is a
        /// formatter, we pay for the extra interface call on each AppendFormatted that needs it.
        /// </remarks>
        private readonly bool _hasCustomFormatter;

        /// <summary>
        /// Creates a handler used to translate an interpolated string into a <see cref="string"/>.
        /// </summary>
        public DefaultInterpolatedStringHandler()
        {
            _stringBuilder = new StringBuilder();
            _hasCustomFormatter = false;
        }

        /// <summary>
        /// Creates a handler used to translate an interpolated string into a <see cref="string"/>.
        /// </summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _stringBuilder = new StringBuilder(GetDefaultLength(literalLength, formattedCount));
            _hasCustomFormatter = false;
        }

        /// <summary>
        /// Creates a handler used to translate an interpolated string into a <see cref="string"/>.
        /// </summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount, IFormatProvider? provider)
        {
            _stringBuilder = new StringBuilder(GetDefaultLength(literalLength, formattedCount));
            _provider = provider;
            _hasCustomFormatter = provider != null && HasCustomFormatter(provider);
        }

        /// <summary>Derives a default length with which to seed the handler.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        [MethodImpl((MethodImplOptions)0x100)] // becomes a constant when inputs are constant
        internal static int GetDefaultLength(int literalLength, int formattedCount) =>
            Math.Max(MinimumArrayPoolLength, literalLength + (formattedCount * GuessedLengthPerHole));

        /// <summary>
        /// Gets the built <see cref="string"/>.
        /// </summary>
        /// <returns>The built string.</returns>
        public override string ToString() => _stringBuilder.ToString();

        /// <summary>
        /// Gets the built <see cref="string"/> and clears the handler.
        /// </summary>
        /// <returns>The built string.</returns>
        /// <remarks>
        /// This releases any resources used by the handler. The method should be invoked only
        /// once and as the last thing performed on the handler. Subsequent use is erroneous, ill-defined,
        /// and may destabilize the process, as may using any other copies of the handler after ToStringAndClear
        /// is called on any one of them.
        /// </remarks>
        public string ToStringAndClear()
        {
            string result = _stringBuilder.ToString();
            _ = _stringBuilder.Clear();
            return result;
        }

        /// <summary>
        /// Clears the handler.
        /// </summary>
        /// <remarks>
        /// This releases any resources used by the handler. The method should be invoked only
        /// once and as the last thing performed on the handler. Subsequent use is erroneous, ill-defined,
        /// and may destabilize the process, as may using any other copies of the handler after <see cref="Clear"/>
        /// is called on any one of them.
        /// </remarks>
        [MethodImpl((MethodImplOptions)0x100)]
        public void Clear() => _ = _stringBuilder.Clear();

        /// <summary>
        /// Gets a span of the characters appended to the handler.
        /// </summary>
        public ReadOnlySpan<char> Text
        {
            get
            {
                char[] result = new char[_stringBuilder.Length];
                _stringBuilder.CopyTo(0, result, 0, _stringBuilder.Length);
                return result;
            }
        }

        /// <summary>
        /// Writes the specified string to the handler.
        /// </summary>
        /// <param name="value">The string to write.</param>
        [MethodImpl((MethodImplOptions)0x100)]
        public void AppendLiteral(string value) => _ = _stringBuilder.Append(value);

        #region AppendFormatted
        #region AppendFormatted T
        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value)
        {
            // If there's a custom formatter, always use it.
            if (_hasCustomFormatter)
            {
                AppendCustomFormatter(value, format: null);
                return;
            }

            string? s = value switch
            {
                IFormattable formattable => formattable.ToString(format: null, _provider), // constrained call avoiding boxing for value types
                _ => value?.ToString(),
            };

            if (s != null)
            {
                AppendLiteral(s);
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, string? format)
        {
            // If there's a custom formatter, always use it.
            if (_hasCustomFormatter)
            {
                AppendCustomFormatter(value, format);
                return;
            }

            string? s = value switch
            {
                IFormattable formattable => formattable.ToString(format, _provider), // constrained call avoiding boxing for value types
                _ => value?.ToString(),
            };

            if (s != null)
            {
                AppendLiteral(s);
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, int alignment)
        {
            int startingPos = _stringBuilder.Length;
            AppendFormatted(value);
            if (alignment != 0)
            {
                AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, int alignment, string? format)
        {
            int startingPos = _stringBuilder.Length;
            AppendFormatted(value, format);
            if (alignment != 0)
            {
                AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
            }
        }
        #endregion

        #region AppendFormatted string
        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted(string? value)
        {
            if (_hasCustomFormatter)
            {
                AppendCustomFormatter(value, format: null);
                return;
            }

            if (value != null)
            {
                AppendLiteral(value);
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) =>
            // Format is meaningless for strings and doesn't make sense for someone to specify.  We have the overload
            // simply to disambiguate between ROS<char> and object, just in case someone does specify a format, as
            // string is implicitly convertible to both. Just delegate to the T-based implementation.
            AppendFormatted<string?>(value, alignment, format);
        #endregion

        #region AppendFormatted ReadOnlySpan<char>
        /// <summary>
        /// Writes the specified character span to the handler.
        /// </summary>
        /// <param name="value">The span to write.</param>
        public void AppendFormatted(params ReadOnlySpan<char> value) => _ = _stringBuilder.Append(value.ToArray());

        /// <summary>
        /// Writes the specified string of chars to the handler.
        /// </summary>
        /// <param name="value">The span to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="__">The format string.</param>
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? __ = null)
        {
            bool leftAlign = false;
            if (alignment < 0)
            {
                leftAlign = true;
                alignment = -alignment;
            }

            int paddingRequired = alignment - value.Length;
            if (paddingRequired <= 0)
            {
                // The value is as large or larger than the required amount of padding,
                // so just write the value.
                AppendFormatted(value);
                return;
            }

            // Write the value along with the appropriate padding.
            if (leftAlign)
            {
                AppendFormatted(value);
                int _pos = _stringBuilder.Length;
                _ = _stringBuilder.Insert(_pos, " ", paddingRequired);
            }
            else
            {
                int _pos = _stringBuilder.Length;
                _ = _stringBuilder.Insert(_pos, " ", paddingRequired);
                AppendFormatted(value);
            }
        }
        #endregion

        #region AppendFormatted object
        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) =>
            // This overload is expected to be used rarely, only if either a) something strongly typed as object is
            // formatted with both an alignment and a format, or b) the compiler is unable to target type to T. It
            // exists purely to help make cases from (b) compile. Just delegate to the T-based implementation.
            AppendFormatted<object?>(value, alignment, format);
        #endregion
        #endregion

        /// <summary>
        /// Gets whether the provider provides a custom formatter.
        /// </summary>
        [MethodImpl((MethodImplOptions)0x100)] // only used in a few hot path call sites
        internal static bool HasCustomFormatter(IFormatProvider provider) =>
            provider != null &&
                provider.GetType() != typeof(CultureInfo) && // optimization to avoid GetFormat in the majority case
                provider.GetFormat(typeof(ICustomFormatter)) != null;

        /// <summary>
        /// Formats the value using the custom formatter from the provider.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendCustomFormatter<T>(T value, string? format)
        {
            ICustomFormatter? formatter = (ICustomFormatter?)_provider?.GetFormat(typeof(ICustomFormatter));
            if (formatter != null && formatter.Format(format, value, _provider) is string customFormatted)
            {
                AppendLiteral(customFormatted);
            }
        }

        /// <summary>
        /// Handles adding any padding required for aligning a formatted value in an interpolation expression.
        /// </summary>
        /// <param name="startingPos">The position at which the written value started.</param>
        /// <param name="alignment">Non-zero minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        private void AppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
        {
            int _pos = _stringBuilder.Length;
            int charsWritten = _pos - startingPos;

            bool leftAlign = false;
            if (alignment < 0)
            {
                leftAlign = true;
                alignment = -alignment;
            }

            int paddingNeeded = alignment - charsWritten;
            if (paddingNeeded > 0)
            {
                _ = leftAlign
                    ? _stringBuilder.Insert(_pos, " ", paddingNeeded)
                    : _stringBuilder.Insert(startingPos, " ", paddingNeeded);
            }
        }
    }
}