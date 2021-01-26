// Copyright (c) 2020 Theodore Tsirpanis
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace BitCollections
{
    /// <summary>
    /// An immutable set of bit values.
    /// </summary>
    [PublicAPI, DebuggerTypeProxy(typeof(BitCollectionDebugView))]
    public readonly partial struct BitSet : IEquatable<BitSet>, IComparable<BitSet>,
        IComparable, IEnumerable<int>
    {
        // We don't allocate for the first 64 bits.
        private readonly ulong _data;

        // The bits after the first 64. Zeroes at
        // the end of the array must be trimmed.
        private readonly ulong[] _extra;

        private static readonly ulong[] _emptyArray =
#if !NET45
            Array.Empty<ulong>();
#else
            new ulong[0];
#endif
        private static readonly BitSet _empty = new BitSet(0, NewArray(0));

        internal BitSet(ulong data, ulong[] extra)
        {
            Debug.Assert(extra.Length == 0 || extra[extra.Length - 1] != 0,
                "The bit set's extra array has zeroes at the end.");
            _data = data;
            _extra = extra;
        }

        internal ulong Data => _data;
        internal ReadOnlySpan<ulong> Extra => new ReadOnlySpan<ulong>(_extra);

        private static ulong[] NewArray(int length)
        {
            return length == 0 ? _emptyArray : new ulong[length];
        }

        private static void ThrowNegativeValue([InvokerParameterName] string paramName, int x) =>
            throw new ArgumentOutOfRangeException(paramName, x, "BitSets cannot store negative values");

        /// <summary>
        /// Creates a <see cref="BitSet"/> with only a single element.
        /// </summary>
        /// <param name="x">The only element to be included.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="x"/> is negative.</exception>
        /// <seealso cref="Set"/>
        public static BitSet Singleton(int x)
        {
            if (x < 0) ThrowNegativeValue(nameof(x), x);
            if (x < 64)
                return new BitSet(1ul << x, _emptyArray);

            var idx = x / 64;
            var extra = NewArray(idx);
            extra[idx - 1] = 1ul << x;
            return new BitSet(0, extra);
        }

        /// <summary>
        /// Creates a <see cref="BitSet"/> that contains all numbers
        /// between 0 and <paramref name="count"/> minus one, inclusive.
        /// </summary>
        /// <param name="count">The cardinality of the returned bit set.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is negative.</exception>
        public static BitSet Universe(int count)
        {
            if (count < 0) ThrowNegativeValue(nameof(count), count);
            if (count <= 64)
                return new BitSet(BitAlgorithms.GetFirstBitsOn(count), _emptyArray);

            // We want to find the position of the last
            // bit, not the position of the bit count.
            var fieldCount = Math.DivRem(count - 1, 64, out var remainingBits);
            // The reduced count is now brought back.
            remainingBits++;
            var extra = NewArray(fieldCount);
            extra.AsSpan().Fill(ulong.MaxValue);
            extra[extra.Length - 1] = BitAlgorithms.GetFirstBitsOn(remainingBits);

            return new BitSet(ulong.MaxValue, extra);
        }

        /// <summary>
        /// Creates a <see cref="BitSet"/> containing the given elements.
        /// </summary>
        /// <remarks>Duplicate values are ignored.</remarks>
        /// <param name="numbers">The numbers to include.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// One of the <paramref name="numbers"/> is negative.</exception>
        public BitSet(IEnumerable<int> numbers)
        {
            if (numbers is BitSet bs)
            {
                this = bs;
                return;
            }

            // We use ToList to avoid resizing the resulting
            // array to precisely fit the elements.
            var xs = numbers.ToList();
            xs.Sort();
            if (xs.Count == 0)
            {
                this = _empty;
                return;
            }

            _data = 0;
            _extra = NewArray(xs[xs.Count - 1] / 64);
            foreach (var i in xs)
            {
                if (i < 0) ThrowNegativeValue(nameof(numbers), i);
                if (i < 64)
                    _data |= 1ul << i;
                else
                {
                    var idx = i / 64 - 1;
                    _extra[idx] |= 1ul << i;
                }
            }
        }

        /// <summary>
        /// An empty <see cref="BitSet"/>.
        /// </summary>
        public static ref readonly BitSet Empty => ref _empty;

        /// <summary>
        /// Whether this <see cref="BitSet"/> is empty.
        /// </summary>
        public bool IsEmpty => _data == 0 && (_extra == null! || _extra.Length == 0);

        /// <summary>
        /// Checks whether the bit with the given index is present.
        /// </summary>
        /// <remarks>If <paramref name="x"/> is negative, an exception
        /// will not be thrown; the result will simply be <see langword="false"/>.</remarks>
        /// <param name="x">The bit index to check.</param>
        public bool this[int x]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (x < 0) return false;
                if (x < 64) return (_data & (1ul << x)) != 0;
                var idx = x / 64 - 1;
                return idx < _extra.Length && (_extra[idx] & (1ul << x)) != 0;
            }
        }

        /// <summary>
        /// Changes a single bit of a <see cref="BitSet"/>.
        /// </summary>
        /// <param name="i">The bit index to check</param>
        /// <param name="val">The bit's new value.</param>
        /// <returns>A <see cref="BitSet"/> whose <paramref name="i"/>th
        /// bit set to <paramref name="val"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="i"/> is negative.</exception>
        public BitSet Set(int i, bool val)
        {
            if (i < 0) ThrowNegativeValue(nameof(i), i);
            if (this[i] == val)
                return this;
            var mask = 1ul << i;
            if (i < 64)
                return new BitSet(val ? _data | mask : _data & ~mask, _extra);
            var idx = i / 64;
            var extraLength = Math.Max(_extra.Length, idx);
            var extraBuffer = ArrayPool<ulong>.Shared.Rent(extraLength);
            try
            {
                Array.Copy(_extra, extraBuffer, _extra.Length);
                ref var cell = ref extraBuffer[idx - 1];
                cell = val ? cell | mask : cell & ~mask;

                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, extraLength);
                extra = BitAlgorithms.TrimTrailingZeroes(extra);
                return new BitSet(_data, extra.ToArray());
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(extraBuffer);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => BitAlgorithms.FormatBitArray(_data, Extra);
    }
}
