using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#if NETCOREAPP3_1
using System.Runtime.Intrinsics.X86;
#endif

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
            for (int i = 0; i < x2._extra.Length; i++)
                extra[i] |= x2._extra[i];
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
                Array.Copy(x1._extra, extraBuffer, extraLength);
                var extraLengthTrimmed = 0;
                for (int i = 0; i < extraLength; i++)
                {
                    var x = x1._extra[i] & x2._extra[i];
                    if (x != 0) extraLengthTrimmed = i + 1;
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
                extraBuffer.AsSpan().Fill(ulong.MaxValue);
                for (int i = 0; i < sets.Count; i++)
                {
                    var x = sets[i]._extra;
                    for (int j = 0; j < extraLength; j++) extraBuffer[j] &= x[j];
                }

                var extraLengthTrimmed = extraLength;
                while (extraLengthTrimmed > 0 && extraBuffer[extraLengthTrimmed - 1] == 0) extraLengthTrimmed--;

                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, extraLengthTrimmed).ToArray();
                return new BitSet(data, extra);
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(extraBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong AndNot(ulong x1, ulong x2)
        {
#if NETCOREAPP3_1
            if (Bmi1.X64.IsSupported)
                // In the BMI instruction, it is the
                // first parameter that gets negated.
                return Bmi1.X64.AndNot(x2, x1);
#endif
            return x1 & ~ x2;
        }

        /// <summary>
        /// Returns a <see cref="BitSet"/> whose elements are
        /// present in this one but not <paramref name="x"/>.
        /// </summary>
        /// <param name="x">The bit set whose elements will be removed.</param>
        /// <returns>The difference of this bit set and <paramref name="x"/></returns>
        public BitSet Difference(in BitSet x)
        {
            var data = AndNot(_data, x._data);
            // This function cannot make the vector any larger.
            var extraBuffer = ArrayPool<ulong>.Shared.Rent(_extra.Length);
            try
            {
                Array.Copy(_extra, extraBuffer, _extra.Length);

                var extraLength = Math.Min(_extra.Length, x._extra.Length);
                var extraLengthTrimmed = 0;
                for (int i = 0; i < extraLength; i++)
                {
                    var cell = AndNot(_extra[i], x._extra[i]);
                    if (cell != 0) extraLengthTrimmed = i + 1;
                    extraBuffer[i] = cell;
                }

                // If the parameter is less than this object, there are non-zero
                // cells at the end, which means that we cannot trim the extra.
                if (x._extra.Length < _extra.Length) extraLengthTrimmed = _extra.Length;
                var extra = new ReadOnlySpan<ulong>(extraBuffer, 0, extraLengthTrimmed).ToArray();
                return new BitSet(data, extra);
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(extraBuffer);
            }
        }

        /// <summary>
        /// An alias for <see cref="Union"/>.
        /// </summary>
        /// <seealso cref="Union"/>
        public static BitSet operator |(in BitSet x1, in BitSet x2) => Union(in x1, in x2);

        /// <summary>
        /// An alias for <see cref="Intersect"/>.
        /// </summary>
        /// <seealso cref="Intersect"/>
        public static BitSet operator &(in BitSet x1, in BitSet x2) => Intersect(in x1, in x2);

        /// <summary>
        /// An alias for <see cref="Difference"/>.
        /// </summary>
        /// <seealso cref="Difference"/>
        public static BitSet operator -(in BitSet x1, in BitSet x2) => x1.Difference(in x2);
    }
}
