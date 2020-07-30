using System;
using System.Collections;
using System.Collections.Generic;

namespace BitArrayNeo
{
    public partial struct BitSet
    {
        public struct Enumerator : IEnumerator<int>
        {
            private readonly BitSet _bitSet;
            private int _nextItem;
            private ulong _currentField;
            private int _currentFieldIndex;

            public Enumerator(in BitSet bs)
            {
                _bitSet = bs;
                _nextItem = -1;
                _currentField = bs._data;
                _currentFieldIndex = -1;
            }

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

            public void Reset()
            {
                this = new Enumerator(in _bitSet);
            }

            public readonly int Current => _nextItem;

            object IEnumerator.Current => _nextItem;

            void IDisposable.Dispose()
            {
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(in this);

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
