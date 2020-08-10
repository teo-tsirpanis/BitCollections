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
        /// An enumerator object over a <see cref="BitSet"/>'s indices of active bits.
        /// </summary>
        /// <seealso cref="BitSet.GetEnumerator"/>
        public struct BitCollectionEnumerator : IEnumerator<int>
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

            /// <inheritdoc/>
            public bool MoveNext()
            {
                while (true)
                {
                    if (_currentField == 0)
                    {
                        if (_currentFieldIndex == _extra.Length - 1)
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

            /// <summary>Not supported.</summary>
            void IEnumerator.Reset() => throw new NotSupportedException();

            /// <inheritdoc/>
            public readonly int Current => _nextItem;

            /// <inheritdoc/>
            object IEnumerator.Current => _nextItem;

            /// <summary>
            /// This implementation of <see cref="IDisposable"/> does nothing.
            /// </summary>
            void IDisposable.Dispose()
            {
            }
        }

    public partial struct BitSet
    {
        /// <returns>A <see cref="BitCollectionEnumerator"/> over this <see cref="BitSet"/>.</returns>
        public BitCollectionEnumerator GetEnumerator() => new BitCollectionEnumerator(_data, _extra, 0);

        /// <inheritdoc/>
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
