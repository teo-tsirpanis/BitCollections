using System;
using System.Collections;
using System.Collections.Generic;

namespace BitCollections
{
    public partial struct BitSet
    {
        /// <summary>
        /// An enumerator object over a <see cref="BitSet"/>'s indices of active bits.
        /// </summary>
        /// <seealso cref="BitSet.GetEnumerator"/>
        public struct Enumerator : IEnumerator<int>
        {
            private readonly BitSet _bitSet;
            private int _nextItem;
            private ulong _currentField;
            private int _currentFieldIndex;

            internal Enumerator(in BitSet bs)
            {
                _bitSet = bs;
                _nextItem = -1;
                _currentField = bs._data;
                _currentFieldIndex = -1;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                while (true)
                {
                    if (_currentField == 0)
                    {
                        if (_currentFieldIndex == _bitSet._extra.Length - 1)
                            return false;
                        _currentFieldIndex++;
                        _currentField = _bitSet._extra[_currentFieldIndex];
                        _nextItem = (_currentFieldIndex + 1) * 64 - 1;
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

            /// <inheritdoc/>
            public void Reset() => this = new Enumerator(in _bitSet);

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

        /// <returns>An <see cref="Enumerator"/> over this <see cref="BitSet"/>.</returns>
        public Enumerator GetEnumerator() => new Enumerator(in this);

        /// <inheritdoc/>
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
