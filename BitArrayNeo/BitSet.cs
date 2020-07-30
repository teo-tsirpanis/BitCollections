using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitArrayNeo
{
    [PublicAPI]
    public readonly partial struct BitSet : IEquatable<BitSet>, IComparable<BitSet>, IComparable, IEnumerable<int>
    {
        private readonly ulong _data;
        private readonly ulong[] _extra;

        private static readonly ulong[] _emptyArray =
#if !NET45
            Array.Empty<ulong>();
#else
            new ulong[0];
#endif
        private static readonly BitSet _empty = new BitSet(0, NewArray(0));

        private BitSet(ulong data, ulong[] extra)
        {
            _data = data;
            _extra = extra;
        }

        private static ulong[] NewArray(int length)
        {
            return length == 0 ? _emptyArray : new ulong[length];
        }

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

        public BitSet(IEnumerable<int> numbers)
        {
            if (numbers is BitSet bs)
            {
                this = bs;
                return;
            }

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

        public static ref readonly BitSet Empty => ref _empty;

        public bool IsEmpty => _data == 0 && (_extra == null || _extra.Length == 0);

        public bool this[int x]
        {
            get
            {
                if (x < 0) return false;
                if (x < 64) return (_data & (1ul << x)) != 0;
                var idx = Math.DivRem(x, 64, out var ofs);
                return idx < _extra.Length && (_extra[idx - 1] & (1ul << ofs)) != 0;
            }
        }

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
