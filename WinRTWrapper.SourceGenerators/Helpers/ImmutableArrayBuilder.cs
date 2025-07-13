// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WinRTWrapper.SourceGenerators.Helpers
{
    /// <summary>
    /// A helper type to build sequences of values with pooled buffers.
    /// </summary>
    /// <typeparam name="T">The type of items to create sequences for.</typeparam>
    internal ref struct ImmutableArrayBuilder<T> : IList<T>
    {
        /// <summary>
        /// The rented <see cref="Writer"/> instance to use.
        /// </summary>
        private Writer? writer;

        /// <summary>
        /// Creates a <see cref="ImmutableArrayBuilder{T}"/> value with a pooled underlying data writer.
        /// </summary>
        /// <returns>A <see cref="ImmutableArrayBuilder{T}"/> instance to write data to.</returns>
        public static ImmutableArrayBuilder<T> Rent() => new(new Writer());

        /// <summary>
        /// Creates a new <see cref="ImmutableArrayBuilder{T}"/> object with the specified parameters.
        /// </summary>
        /// <param name="writer">The target data writer to use.</param>
        private ImmutableArrayBuilder(Writer writer) => this.writer = writer;

        /// <inheritdoc cref="ImmutableArray{T}.Builder.Count"/>
        [MemberNotNull(nameof(writer))]
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => writer!.Count;
        }

        /// <summary>
        /// Gets the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [UnscopedRef]
        [MemberNotNull(nameof(writer))]
        public readonly ReadOnlySpan<T> WrittenSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => writer!.WrittenSpan;
        }

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly T this[int index]
        {
            get => writer![index];
            set => writer![index] = value;
        }

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        readonly bool ICollection<T>.IsReadOnly => ((ICollection<T>)writer!).IsReadOnly;

        /// <inheritdoc cref="ImmutableArray{T}.Builder.Add(T)"/>
        [MemberNotNull(nameof(writer))]
        public readonly void Add(T item) => writer!.Add(item);

        /// <summary>
        /// Adds the specified items to the end of the array.
        /// </summary>
        /// <param name="items">The items to add at the end of the array.</param>
        [MemberNotNull(nameof(writer))]
        public readonly void AddRange(scoped ReadOnlySpan<T> items) => writer!.AddRange(items);

        /// <summary>
        /// Adds the specified items to the end of the array.
        /// </summary>
        /// <param name="collection">The items to add at the end of the array.</param>
        [MemberNotNull(nameof(writer))]
        public readonly void AddRange(IEnumerable<T> collection) => writer!.AddRange(collection);

        /// <inheritdoc cref="ImmutableArray{T}.Builder.ToImmutable"/>
        [MemberNotNull(nameof(writer))]
        public readonly ImmutableArray<T> ToImmutable()
        {
            T[] array = writer!.WrittenSpan.ToArray();

            return Unsafe.As<T[], ImmutableArray<T>>(ref array);
        }

        /// <inheritdoc cref="ImmutableArray{T}.Builder.ToArray"/>
        [MemberNotNull(nameof(writer))]
        public readonly T[] ToArray() => writer!.WrittenSpan.ToArray();

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public override readonly string ToString() => writer!.WrittenSpan.ToString();

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            Writer? writer = this.writer;

            this.writer = null;

            writer?.Dispose();
        }

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly int IndexOf(T item) => writer!.IndexOf(item);

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly void Insert(int index, T item) => writer!.Insert(index, item);

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly void RemoveAt(int index) => writer!.RemoveAt(index);

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly void Clear() => writer!.Clear();

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly bool Contains(T item) => writer!.Contains(item);

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly void CopyTo(T[] array, int arrayIndex) => writer!.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly bool Remove(T item) => writer!.Remove(item);
        
        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        public readonly IEnumerator<T> GetEnumerator() => writer!.GetEnumerator();

        /// <inheritdoc/>
        [MemberNotNull(nameof(writer))]
        readonly IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)writer!).GetEnumerator();

        /// <summary>
        /// Gets the underlying <see cref="IEnumerable{T}"/> instance containing the written items.
        /// </summary>
        /// <returns>The underlying <see cref="IEnumerable{T}"/> instance containing the written items.</returns>
        public readonly IEnumerable<T> GetEnumerable() => writer!;

        /// <summary>
        /// A class handling the actual buffer writing.
        /// </summary>
        private sealed class Writer : IList<T>, IDisposable
        {
            /// <summary>
            /// The underlying <typeparamref name="T"/> array.
            /// </summary>
            private T[]? array;

            /// <summary>
            /// The starting offset within <see cref="array"/>.
            /// </summary>
            private int index;

            /// <summary>
            /// Creates a new <see cref="Writer"/> instance with the specified parameters.
            /// </summary>
            public Writer()
            {
                array = ArrayPool<T>.Shared.Rent(typeof(T) == typeof(char) ? 1024 : 8);
                index = 0;
            }

            /// <inheritdoc/>
            [MemberNotNull(nameof(array))]
            public T this[int index]
            {
                get => array![index];
                set => array![index] = value;
            }

            /// <inheritdoc cref="ImmutableArrayBuilder{T}.Count"/>
            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => index;
            }

            /// <inheritdoc cref="ImmutableArrayBuilder{T}.WrittenSpan"/>
            public ReadOnlySpan<T> WrittenSpan
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(array, 0, index);
            }

            /// <inheritdoc/>
            [MemberNotNull(nameof(array))]
            bool ICollection<T>.IsReadOnly => ((ICollection<T>)array!).IsReadOnly;

            /// <inheritdoc cref="ImmutableArrayBuilder{T}.Add"/>
            [MemberNotNull(nameof(array))]
            public void Add(T value)
            {
                EnsureCapacity(1);

                array[index++] = value;
            }

            /// <inheritdoc cref="ImmutableArrayBuilder{T}.AddRange(ReadOnlySpan{T})"/>
            [MemberNotNull(nameof(array))]
            public void AddRange(ReadOnlySpan<T> items)
            {
                EnsureCapacity(items.Length);

                items.CopyTo(array.AsSpan(index));

                index += items.Length;
            }

            /// <inheritdoc cref="ImmutableArrayBuilder{T}.AddRange(IEnumerable{T})"/>
            public void AddRange(IEnumerable<T> collection)
            {
                if (collection is ICollection<T> c)
                {
                    int count = c.Count;
                    if (count > 0)
                    {
                        EnsureCapacity(count);

                        c.CopyTo(array, index);

                        index += count;
                    }
                }
                else
                {
                    using IEnumerator<T> en = collection.GetEnumerator();
                    while (en.MoveNext())
                    {
                        Add(en.Current);
                    }
                }
            }

            /// <inheritdoc/>
            [MemberNotNull(nameof(array))]
            public void Clear()
            {
                ArrayPool<T>.Shared.Return(array, clearArray: typeof(T) != typeof(char));
                array = ArrayPool<T>.Shared.Rent(typeof(T) == typeof(char) ? 1024 : 8);
                index = 0;
            }

            /// <inheritdoc/>
            [MemberNotNull(nameof(array))]
            public bool Contains(T item) => ((ICollection<T>)array!).Contains(item);

            /// <inheritdoc/>
            [MemberNotNull(nameof(array))]
            public void CopyTo(T[] array, int arrayIndex) => this.array!.CopyTo(array, arrayIndex);

            /// <inheritdoc/>
            public void Dispose()
            {
                T[]? array = this.array;

                this.array = null!;

                if (array != null)
                {
                    ArrayPool<T>.Shared.Return(array, clearArray: typeof(T) != typeof(char));
                }
            }

            /// <inheritdoc/>
            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < index; i++)
                {
                    yield return array![i];
                }
            }

            /// <inheritdoc/>
            public int IndexOf(T item) => Array.IndexOf(array, item);

            /// <inheritdoc/>
            [MemberNotNull(nameof(array))]
            public void Insert(int index, T item)
            {
                // Note that insertions at the end are legal.
                if (index > this.index)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be greater than the current size of the builder.");
                }
                if (this.index == array!.Length) { ResizeBuffer(1); }
                if (index < this.index)
                {
                    Array.Copy(array, index, array, index + 1, this.index - index);
                }
                array[index] = item;
                this.index++;
            }

            /// <inheritdoc/>
            public bool Remove(T item)
            {
                int index = IndexOf(item);
                if (index >= 0)
                {
                    RemoveAt(index);
                    return true;
                }
                return false;
            }

            /// <inheritdoc/>
            [MemberNotNull(nameof(array))]
            public void RemoveAt(int index)
            {
                if (index >= this.index)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "Index must be less than the current size of the builder.");
                }
                this.index--;
                if (index < this.index)
                {
                    Array.Copy(array, index + 1, array, index, this.index - index);
                }
                array![this.index] = default!;
            }

            /// <summary>
            /// Ensures that <see cref="array"/> has enough free space to contain a given number of new items.
            /// </summary>
            /// <param name="requestedSize">The minimum number of items to ensure space for in <see cref="array"/>.</param>
            [MemberNotNull(nameof(array))]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureCapacity(int requestedSize)
            {
                if (requestedSize > array!.Length - index)
                {
                    ResizeBuffer(requestedSize);
                }
            }

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Resizes <see cref="array"/> to ensure it can fit the specified number of new items.
            /// </summary>
            /// <param name="sizeHint">The minimum number of items to ensure space for in <see cref="array"/>.</param>
            [MemberNotNull(nameof(array))]
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void ResizeBuffer(int sizeHint)
            {
                int minimumSize = index + sizeHint;

                T[] oldArray = array!;
                T[] newArray = ArrayPool<T>.Shared.Rent(minimumSize);

                Array.Copy(oldArray, newArray, index);

                array = newArray;

                ArrayPool<T>.Shared.Return(oldArray, clearArray: typeof(T) != typeof(char));
            }
        }
    }
}