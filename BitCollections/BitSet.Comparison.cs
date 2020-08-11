// Copyright (c) 2020 Theodore Tsirpanis
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;

namespace BitCollections
{
    public partial struct BitSet
    {
        private static bool AreEqual(in BitSet x1, in BitSet x2)
        {
            return x1._data == x2._data && x1._extra.AsSpan().SequenceEqual(x2._extra.AsSpan());
        }

        private static int Compare(in BitSet x1, in BitSet x2)
        {
            var x = x1._extra.Length.CompareTo(x2._extra.Length);
            if (x != 0) return x;
            x = x1._extra.AsSpan().SequenceCompareTo(x2._extra.AsSpan());
            return x != 0 ? x : x1._data.CompareTo(x2._data);
        }

        /// <inheritdoc/>
        public bool Equals(BitSet other) => AreEqual(in this, in other);

        /// <summary>
        /// Compares this <see cref="BitArrayNeo"/>
        /// with a <see cref="BitSet"/> to see if they are equal.
        /// </summary>
        /// <param name="other">The bit set.</param>
        /// <returns>Whether they are equal.</returns>
        /// <remarks>The <see cref="BitArrayNeo.BitCapacity"/> of
        /// <paramref name="other"/> is not taken into account.</remarks>
        public bool Equals(BitArrayNeo? other) => other != null && other.Equals(this);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is BitSet x && AreEqual(in this, in x);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
#if !NET45
            HashCode h = new HashCode();
            h.Add(_data);
            h.Add(_extra.Length);
            foreach (var x in _extra) h.Add(x);
            return h.ToHashCode();
#else
            var h = 17;
            h = h * 31 + _data.GetHashCode();
            h = h * 31 + _extra.Length.GetHashCode();
            for (int i = 0; i < _extra.Length; i++)
                h = h * 31 + _extra[i].GetHashCode();
            return h;
#endif
        }

        /// <inheritdoc/>
        public int CompareTo(BitSet other) => Compare(in this, in other);

        /// <inheritdoc/>
        int IComparable.CompareTo(object? obj) =>
            obj switch
            {
                null => 1,
                BitSet bs => Compare(in this, in bs),
                _ => String.CompareOrdinal(nameof(BitSet), obj.GetType().Name)
            };
    }
}
