using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BitCollections
{
    /// <summary>
    /// An immutable set of bit values.
    /// </summary>
    [PublicAPI]
    public readonly partial struct BitSet : IEquatable<BitSet>, IComparable<BitSet>, IComparable, IEnumerable<int>
    {
        // We don't allocate for the first 64 bits.
        private readonly ulong _data;
        // The bits after the first 64.
        // Zeroes at the beginning have to be trimmed.
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
            _data = data;
            _extra = extra;
        }

        private static ulong[] NewArray(int length)
        {
            return length == 0 ? _emptyArray : new ulong[length];
        }

        /// <summary>
        /// Creates a <see cref="BitSet"/> with only a single element.
        /// </summary>
        /// <param name="x">The only element to be included.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="x"/> is negative.</exception>
        /// <seealso cref="Set"/>
        public static BitSet Singleton(int x)
        {
            if (x < 0)
                throw new ArgumentOutOfRangeException(nameof(x), x, "BitSets cannot store negative values.");
            if (x < 64)
                return new BitSet(1ul << x, _emptyArray);

            var idx = Math.DivRem(x, 64, out var ofs);
            var extra = NewArray(idx);
            extra[idx - 1] = 1ul << ofs;
            return new BitSet(0, extra);
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
                if (i < 0)
                    throw new ArgumentOutOfRangeException(nameof(numbers), i, "BitSets cannot store negative values.");
                if (i < 64)
                    _data |= 1ul << i;
                else
                {
                    var idx = Math.DivRem(i, 64, out var ofs);
                    _extra[idx - 1] |= 1ul << ofs;
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
        public bool IsEmpty => _data == 0 && (_extra == null || _extra.Length == 0);

        /// <summary>
        /// Checks whether the bit with the given index is present.
        /// </summary>
        /// <remarks>If <paramref name="x"/> is negative, an exception
        /// will not be thrown; the result will simply be <see langword="false"/>.</remarks>
        /// <param name="x">The bit index to check.</param>
        public bool this[int x]
        {
            get
            {
                if (x < 0) return false;
                if (x < 64) return (_data & (1ul << x)) != 0;
                var idx = Math.DivRem(x, 64, out var ofs) - 1;
                return idx < _extra.Length && (_extra[idx] & (1ul << ofs)) != 0;
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
            if (i < 0)
                throw new ArgumentOutOfRangeException(nameof(i), i, "BitSets cannot store negative values.");
            if (this[i] == val)
                return this;
            if (i < 64)
                return new BitSet(val ? _data | (1ul << i) : _data & ~(1ul << i), _extra);
            var idx = Math.DivRem(i, 64, out var ofs);
            var extra = NewArray(Math.Max(_extra.Length, idx));
            Array.Copy(_extra, extra, _extra.Length);
            ref ulong cell = ref extra[idx - 1];
            cell = val ? cell | (1ul << ofs) : cell & ~(1ul << ofs);
            return new BitSet(_data, extra);
        }
    }
}
