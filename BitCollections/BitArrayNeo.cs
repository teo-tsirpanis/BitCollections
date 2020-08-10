// Copyright (c) 2020 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace BitCollections
{
    /// <summary>
    /// A fixed-length and mutable array of bits.
    /// </summary>
    /// <remarks>Its difference from <see cref="System.Collections.BitArray"/>
    /// is that the <see cref="BitArrayNeo"/>'s mutating methods returns whether
    /// the collection's content changed. Other members can be requested by opening
    /// a GitHub issue.</remarks>
    [PublicAPI, DebuggerTypeProxy(typeof(BitCollectionDebugView))]
    public partial class BitArrayNeo : IEquatable<BitArrayNeo>, IEquatable<BitSet>, ICloneable, IEnumerable<int>
    {
        private readonly ulong[] _data;
        private readonly int _bitCapacity;

        /// <summary>
        /// Creates a <see cref="BitArrayNeo"/> that can
        /// hold at most <paramref name="bitCapacity"/> bits.
        /// </summary>
        /// <param name="bitCapacity">The amount of bits this bit array
        /// can hold. The lowest is zero and the highest is (<paramref name="bitCapacity"/> - 1).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bitCapacity"/> is negative.</exception>
        public BitArrayNeo(int bitCapacity)
        {
            if (bitCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(bitCapacity));
            _bitCapacity = bitCapacity;
            _data = new ulong[bitCapacity == 0 ? 0 : bitCapacity / 64 + 1];
        }

        /// <summary>
        /// Creates a <see cref="BitArrayNeo"/> with the same
        /// capacity and content of another <see cref="BitArrayNeo"/>.
        /// </summary>
        /// <param name="x">The bit array to clone.</param>
        public BitArrayNeo(BitArrayNeo x)
        {
            _bitCapacity = x._bitCapacity;
            _data = x._data.AsSpan().ToArray();
        }

        /// <summary>
        /// Creates a <see cref="BitArrayNeo"/> from an existing <see cref="BitSet"/>.
        /// </summary>
        /// <param name="bs">The bit set whose data to be copied.</param>
        public BitArrayNeo(BitSet bs)
        {
            _data = new ulong[bs.Extra.Length + 1];
            _data[0] = bs.Data;
            bs.Extra.CopyTo(_data.AsSpan(1));
            _bitCapacity = _data.Length * 64;
        }

        private static void ThrowNegativeValue(int x) =>
            throw new ArgumentOutOfRangeException(nameof(x), x, "BitArrayNeoes cannot store negative values.");

        private static void ThrowDifferentCapacity([InvokerParameterName] string paramName) =>
            throw new ArgumentException("The BitArrayNeoes' capacities differ.", paramName);

        /// <summary>
        /// Gets or sets individual bits in a <see cref="BitArrayNeo"/>.
        /// </summary>
        /// <param name="x">The bit index to work with.</param>
        /// <exception cref="ArgumentOutOfRangeException">A bit with a negative or
        /// too large index was attempted to be set. The getter does not throw exceptions,
        /// returning <see langword="false"/> instead on invalid input.</exception>
        public bool this[int x]
        {
            get
            {
                if (x < 0) return false;
                var idx = x / 64;
                return idx < _data.Length && (_data[idx] & 1ul << x) != 0;
            }

            set
            {
                if (x < 0) ThrowNegativeValue(x);
                if (x >= _bitCapacity)
                    throw new ArgumentOutOfRangeException(nameof(x), x,
                        "Cannot store a value greater than the capacity of a BitArrayNeo.");
                var idx = x / 64;
                var mask = 1ul << x;
                if (value)
                    _data[idx] |= mask;
                else
                    _data[idx] &= ~mask;
            }
        }

        /// <summary>
        /// Sets the <paramref name="x"/>th bit of this
        /// <see cref="BitArrayNeo"/> to <paramref name="value"/>.
        /// </summary>
        /// <param name="x">The index of the bit to change.</param>
        /// <param name="value">The new value of that bit.</param>
        /// <returns>Whether the bit actually changed, i.e. whether the
        /// bit hadn't already been set to <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="x"/> is negative or too large.</exception>
        public bool Set(int x, bool value)
        {
            if (x < 0) ThrowNegativeValue(x);
            if (this[x] == value)
                return false;
            this[x] = value;
            return true;
        }

        /// <summary>
        /// Performs a bitwise OR between the bits of this
        /// <see cref="BitArrayNeo"/> and <paramref name="x"/>,
        /// modifying the former.
        /// </summary>
        /// <param name="x">The other bit array.</param>
        /// <returns>Whether the content of this bit array actually changed.</returns>
        /// <exception cref="ArgumentException">The bit arrays have different <see cref="BitCapacity"/>ies.</exception>
        public bool Or(BitArrayNeo x)
        {
            if (_bitCapacity != x._bitCapacity)
                ThrowDifferentCapacity(nameof(x));
            return BitAlgorithms.Or(_data.AsSpan(), x._data.AsSpan());
        }

        /// <summary>
        /// Performs a bitwise AND between the bits of this
        /// <see cref="BitArrayNeo"/> and <paramref name="x"/>,
        /// modifying the former.
        /// </summary>
        /// <param name="x">The other bit array.</param>
        /// <returns>Whether the content of this bit array actually changed.</returns>
        /// <exception cref="ArgumentException">The bit arrays have different <see cref="BitCapacity"/>ies.</exception>
        public bool And(BitArrayNeo x)
        {
            if (_bitCapacity != x._bitCapacity)
                ThrowDifferentCapacity(nameof(x));
            return BitAlgorithms.And(_data.AsSpan(), x._data.AsSpan());
        }

        /// <summary>
        /// Performs a bitwise XOR between the bits of this
        /// <see cref="BitArrayNeo"/> and <paramref name="x"/>,
        /// modifying the former.
        /// </summary>
        /// <param name="x">The other bit array.</param>
        /// <returns>Whether the content of this bit array actually changed.</returns>
        /// <exception cref="ArgumentException">The bit arrays have different <see cref="BitCapacity"/>ies.</exception>
        public bool Xor(BitArrayNeo x)
        {
            if (_bitCapacity != x._bitCapacity)
                ThrowDifferentCapacity(nameof(x));
            return BitAlgorithms.Xor(_data.AsSpan(), x._data.AsSpan());
        }

        /// <summary>
        /// Inverts all the bits of this <see cref="BitArrayNeo"/>.
        /// </summary>
        public void Not()
        {
            if (_data.Length == 0) return;
            BitAlgorithms.Not(_data.AsSpan());
            // The unused bits at the last cell are reset to zero.
            // This allows easy comparison and conversion to BitSet
            // and they cannot be changed from any other place.
            _data[_data.Length - 1] &= (1ul << _bitCapacity) - 1;
        }

        /// <summary>
        /// Converts this <see cref="BitArrayNeo"/> to a <see cref="BitSet"/>.
        /// </summary>
        public BitSet ToBitSet()
        {
            if (_data.Length == 0) return BitSet.Empty;
            ReadOnlySpan<ulong> extra = _data.AsSpan(1);
            extra = BitAlgorithms.TrimTrailingZeroes(extra);
            return new BitSet(_data[0], extra.ToArray());
        }

        /// <summary>
        /// The amount of bits this bit array can hold. The lowest
        /// is zero and the highest is (<see cref="BitCapacity"/> - 1).
        /// </summary>
        public int BitCapacity => _bitCapacity;

        /// <summary>
        /// A read-only view of the data of a <see cref="BitArrayNeo"/>.
        /// </summary>
        public ReadOnlySpan<ulong> AsSpan() => new ReadOnlySpan<ulong>(_data);

        /// <inheritdoc/>
        public bool Equals(BitArrayNeo? other)
        {
            if (ReferenceEquals(this, other)) return true;
            return other != null && _bitCapacity == other._bitCapacity &&
                   _data.AsSpan().SequenceEqual(other._data.AsSpan());
        }

        /// <inheritdoc/>
        public bool Equals(BitSet other)
        {
            if (_data.Length == 0) return other.IsEmpty;
            ReadOnlySpan<ulong> extra = _data.AsSpan(1);
            extra = BitAlgorithms.TrimTrailingZeroes(extra);
            return _data[0] == other.Data && extra.SequenceEqual(other.Extra);
        }

        /// <inheritdoc/>
        public override string ToString() =>
            _data.Length == 0 ? string.Empty : BitAlgorithms.FormatBitArray(_data[0], _data.AsSpan(1));

        /// <inheritdoc/>
        object ICloneable.Clone() => new BitArrayNeo(this);
    }
}
