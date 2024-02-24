// Copyright (c) 2020 Theodore Tsirpanis
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BitCollections
{
    internal sealed class BitCollectionDebugView
    {
        public BitCollectionDebugView(IEnumerable<int> xs) => Items = xs.ToArray();

        public BitCollectionDebugView(BitSet bs) : this((IEnumerable<int>) bs)
        {
        }

        public BitCollectionDebugView(BitArrayNeo ban) : this((IEnumerable<int>) ban)
        {
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public int[] Items { get; }
    }
}
