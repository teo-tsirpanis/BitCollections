// Copyright (c) 2020 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace BitCollections
{
    public partial struct BitSet
    {
        /// <summary>
        /// Returns a <see cref="BitSet"/> whose elements
        /// exist in either of the two given bit sets.
        /// </summary>
        /// <param name="x1">The first bit set.</param>
        /// <param name="x2">The second bit set.</param>
        /// <returns>The union of <paramref name="x1"/> and <paramref name="x2"/>.</returns>
        public static BitSet Union(in BitSet x1, in BitSet x2)
        {
            var data = x1._data | x2._data;
            var extra = NewArray(Math.Max(x1._extra.Length, x2._extra.Length));
            Array.Copy(x1._extra, extra, x1._extra.Length);
            BitAlgorithms.Or(new Span<ulong>(extra, 0, x2._extra.Length), new ReadOnlySpan<ulong>(x2._extra));
            return new BitSet(data, extra);
        }

        /// <summary>
        /// Returns a <see cref="BitSet"/> whose elements
        /// exist in both of the two given bit sets.
        /// </summary>
        /// <param name="x1">The first bit set.</param>
        /// <param name="x2">The second bit set.</param>
        /// <returns>The intersection of <paramref name="x1"/> and <paramref name="x2"/>.</returns>
        public static BitSet Intersect(in BitSet x1, in BitSet x2)
        {
            var data = x1._data & x2._data;
            var extraLength = Math.Min(x1._extra.Length, x2._extra.Length);
            if (extraLength == 0) return new BitSet(data, _emptyArray);

            var extraBuffer = ArrayPool<ulong>.Shared.Rent(extraLength);
            try
            {
                extraBuffer.AsSpan(0, extraLength).Fill(ulong.MaxValue);
                Array.Copy(x1._extra, extraBuffer, extraLength);
                BitAlgorithms.And(new Span<ulong>(extraBuffer, 0, extraLength),
                    new ReadOnlySpan<ulong>(x2._extra, 0, extraLength));

                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, extraLength);
                extra = BitAlgorithms.TrimTrailingZeroes(extra);
                return new BitSet(data, extra.ToArray());
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(extraBuffer);
            }
        }

        /// <summary>
        /// Returns a <see cref="BitSet"/> whose elements
        /// exist in either bit set of the given sequence.
        /// </summary>
        /// <param name="xs">The sequence of bit sets to unite.</param>
        /// <returns>The union of the bit sets in <paramref name="xs"/>.</returns>
        public static BitSet UnionMany(IEnumerable<BitSet> xs)
        {
            var sets = xs.ToList();
            ulong data = 0;
            int extraLength = 0;
            for (int i = 0; i < sets.Count; i++)
            {
                var x = sets[i];
                data |= x._data;
                if (x._extra.Length > extraLength) extraLength = x._extra.Length;
            }

            var extra = NewArray(extraLength);
            for (int i = 0; i < sets.Count; i++)
            {
                var x = sets[i]._extra;
                BitAlgorithms.Or(new Span<ulong>(extra, 0, x.Length), new ReadOnlySpan<ulong>(x));
            }

            return new BitSet(data, extra);
        }

        /// <summary>
        /// Returns a <see cref="BitSet"/> whose elements
        /// exist in all bit sets of the given sequence.
        /// </summary>
        /// <param name="xs">The sequence of bit sets to intersect.</param>
        /// <returns>The intersection of the bit sets in <paramref name="xs"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="xs"/> is empty.</exception>
        public static BitSet IntersectMany(IEnumerable<BitSet> xs)
        {
            var sets = xs.ToList();
            if (sets.Count == 0)
                throw new ArgumentException("Cannot find the intersection of an empty set.", nameof(xs));
            ulong data = ulong.MaxValue;
            int extraLength = int.MaxValue;

            for (int i = 0; i < sets.Count; i++)
            {
                var x = sets[i];
                data &= x._data;
                if (x._extra.Length < extraLength) extraLength = x._extra.Length;
            }

            var extraBuffer = ArrayPool<ulong>.Shared.Rent(extraLength);
            try
            {
                extraBuffer.AsSpan(0, extraLength).Fill(ulong.MaxValue);
                for (int i = 0; i < sets.Count; i++)
                {
                    var x = sets[i]._extra;
                    BitAlgorithms.And(new Span<ulong>(extraBuffer, 0, extraLength),
                        new ReadOnlySpan<ulong>(x, 0, extraLength));
                }

                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, extraLength);
                extra = BitAlgorithms.TrimTrailingZeroes(extra);
                return new BitSet(data, extra.ToArray());
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(extraBuffer);
            }
        }

        /// <summary>
        /// Returns a <see cref="BitSet"/> whose elements are
        /// present in this one but not <paramref name="x"/>.
        /// </summary>
        /// <param name="x">The bit set whose elements will be removed.</param>
        /// <returns>The difference of this bit set and <paramref name="x"/></returns>
        public BitSet Difference(in BitSet x)
        {
            var data = BitAlgorithms.AndNotSingle(_data, x._data);
            // This function cannot make the vector any larger.
            var extraBuffer = ArrayPool<ulong>.Shared.Rent(_extra.Length);
            try
            {
                Array.Copy(_extra, extraBuffer, _extra.Length);

                var extraLength = Math.Min(_extra.Length, x._extra.Length);
                _ = BitAlgorithms.AndNot(extraBuffer.AsSpan(0, extraLength), x._extra.AsSpan(0, extraLength));
                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, _extra.Length);
                extra = BitAlgorithms.TrimTrailingZeroes(extra);
                return new BitSet(data, extra.ToArray());
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(extraBuffer);
            }
        }

        /// <summary>
        /// Returns a <see cref="BitSet"/> whose elements
        /// exist only one of the two given bit sets.
        /// </summary>
        /// <param name="x1">The first bit set.</param>
        /// <param name="x2">The second bit set.</param>
        /// <returns>The symmetric difference of <paramref name="x1"/>
        /// and <paramref name="x2"/>.</returns>
        public static BitSet SymmetricDifference(in BitSet x1, in BitSet x2)
        {
            var data = x1._data ^ x2._data;
            var extraLength = Math.Max(x1._extra.Length, x2._extra.Length);
            var extraBuffer = ArrayPool<ulong>.Shared.Rent(extraLength);
            try
            {
                extraBuffer.AsSpan(0, extraLength).Fill(0);
                Array.Copy(x1._extra, extraBuffer, x1._extra.Length);

                _ = BitAlgorithms.Xor(extraBuffer.AsSpan(0, x2._extra.Length), x2._extra.AsSpan());
                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, extraLength);
                extra = BitAlgorithms.TrimTrailingZeroes(extra);
                return new BitSet(data, extra.ToArray());
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(extraBuffer);
            }
        }
    }
}
