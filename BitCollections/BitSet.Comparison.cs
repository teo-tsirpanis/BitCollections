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

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is BitSet x && Equals(x);

        /// <summary>
        /// The implementation of the equality operator.
        /// </summary>
        public static bool operator ==(in BitSet x1, in BitSet x2) => AreEqual(in x1, in x2);

        /// <summary>
        /// The implementation of the inequality operator.
        /// </summary>
        public static bool operator !=(in BitSet x1, in BitSet x2) => !(x1 == x2);

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
