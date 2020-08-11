// Copyright (c) 2020 Theodore Tsirpanis
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections;
using System.Collections.Generic;

namespace BitCollections
{
    /// <summary>
    /// An enumerator object over a bit collection's indices of active bits.
    /// It can work with either a <see cref="BitSet"/> or a <see cref="BitArrayNeo"/>.
    /// </summary>
    public struct BitCollectionEnumerator
    {
        private readonly ulong[] _extra;
        private int _nextItem;
        private ulong _currentField;
        private int _currentFieldIndex;

        internal BitCollectionEnumerator(ulong first, ulong[] rest, int restStartIndex)
        {
            _currentField = first;
            _extra = rest;
            _nextItem = -1;
            _currentFieldIndex = restStartIndex - 1;
        }

        /// <summary>
        /// Loads the next item of the collection.
        /// </summary>
        /// <returns>Whether such next item exists.</returns>
        public bool MoveNext()
        {
            while (true)
            {
                if (_currentField == 0)
                {
                    if (_currentFieldIndex >= _extra.Length - 1)
                        return false;
                    _currentFieldIndex++;
                    _currentField = _extra[_currentFieldIndex];
                    // We set _nextItem to one number less than
                    // the closest multiple of 64 that is bigger
                    // than _nextItem.
                    _nextItem = (_nextItem / 64 + 1) * 64 - 1;
                }
                else
                {
                    _nextItem++;
                    var isSet = _currentField % 2;
                    _currentField /= 2;
                    if (isSet == 1) return true;
                }
            }
        }

        /// <summary>
        /// The current item.
        /// </summary>
        public readonly int Current => _nextItem;
    }

    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablearray-1.enumerator?view=netcore-3.1#remarks
    internal sealed class BitCollectionEnumeratorWrapper : IEnumerator<int>
    {
        private BitCollectionEnumerator _enumerator;

        public BitCollectionEnumeratorWrapper(BitCollectionEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        void IDisposable.Dispose()
        {
        }

        public bool MoveNext() => _enumerator.MoveNext();
        public int Current => _enumerator.Current;
        void IEnumerator.Reset() => throw new NotSupportedException();
        object IEnumerator.Current => Current;
    }

    public partial struct BitSet
    {
        /// <returns>A <see cref="BitCollectionEnumerator"/> over this <see cref="BitSet"/>.</returns>
        public BitCollectionEnumerator GetEnumerator() => new BitCollectionEnumerator(_data, _extra, 0);

        /// <inheritdoc/>
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => new BitCollectionEnumeratorWrapper(GetEnumerator());

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => new BitCollectionEnumeratorWrapper(GetEnumerator());
    }

    public partial class BitArrayNeo
    {
        /// <returns>A <see cref="BitCollectionEnumerator"/> over this <see cref="BitArrayNeo"/>.</returns>
        public BitCollectionEnumerator GetEnumerator()
        {
            var first = _data.Length == 0 ? 0 : _data[0];
            return new BitCollectionEnumerator(first, _data, 1);
        }

        /// <inheritdoc/>
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => new BitCollectionEnumeratorWrapper(GetEnumerator());

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => new BitCollectionEnumeratorWrapper(GetEnumerator());
    }
}
