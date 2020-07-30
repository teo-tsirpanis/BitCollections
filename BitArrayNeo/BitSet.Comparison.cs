using System;

namespace BitArrayNeo
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

        public bool Equals(BitSet other) => AreEqual(in this, in other);

        public override bool Equals(object? obj) => obj is BitSet x && Equals(x);

        public static bool operator ==(in BitSet x1, in BitSet x2) => AreEqual(in x1, in x2);

        public static bool operator !=(in BitSet x1, in BitSet x2) => !(x1 == x2);

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

        public int CompareTo(BitSet other) => Compare(in this, in other);

        int IComparable.CompareTo(object? obj) =>
            obj != null ? CompareTo((BitSet) obj) : throw new ArgumentNullException(nameof(obj));
    }
}
