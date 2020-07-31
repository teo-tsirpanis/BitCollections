using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace BitArrayNeo
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
        public BitSet Union(in BitSet x1, in BitSet x2)
        {
            var data = x1._data | x2._data;
            var extra = NewArray(Math.Max(x1._extra.Length, x2._extra.Length));
            Array.Copy(x1._extra, extra, x1._extra.Length);
            for (int i = 0; i < x2._extra.Length; i++)
                extra[i] &= x2._extra[i];
            return new BitSet(data, extra);
        }

        /// <summary>
        /// Returns a <see cref="BitSet"/> whose elements
        /// exist in both of the two given bit sets.
        /// </summary>
        /// <param name="x1">The first bit set.</param>
        /// <param name="x2">The second bit set.</param>
        /// <returns>The intersection of <paramref name="x1"/> and <paramref name="x2"/>.</returns>
        public BitSet Intersect(in BitSet x1, in BitSet x2)
        {
            var data = x1._data & x2._data;
            var extraLength = Math.Min(x1._extra.Length, x2._extra.Length);
            var extraBuffer = ArrayPool<ulong>.Shared.Rent(extraLength);
            try
            {
                Array.Copy(x1._extra, extraBuffer, x1._extra.Length);
                int extraLengthTrimmed = 0;
                for (int i = 0; i < extraLength; i++)
                {
                    var x = x1._extra[i] & x2._extra[i];
                    if (x != 0) extraLengthTrimmed++;
                    extraBuffer[i] = x;
                }

                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, extraLengthTrimmed).ToArray();
                return new BitSet(data, extra);
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
        public BitSet UnionMany(IEnumerable<BitSet> xs)
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
                for (int j = 0; j < x.Length; j++) extra[j] |= x[j];
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
        public BitSet IntersectMany(IEnumerable<BitSet> xs)
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
                extraBuffer.AsSpan().Fill(ulong.MaxValue);
                for (int i = 0; i < sets.Count; i++)
                {
                    var x = sets[i]._extra;
                    for (int j = 0; j < extraLength; j++) extraBuffer[j] &= x[j];
                }

                var extraLengthTrimmed = extraLength;
                while (extraLengthTrimmed >= 0 && extraBuffer[extraLengthTrimmed - 1] == 0) extraLengthTrimmed--;

                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, extraLengthTrimmed).ToArray();
                return new BitSet(data, extra);
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(extraBuffer);
            }
        }
    }
}
