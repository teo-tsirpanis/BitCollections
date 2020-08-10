using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

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

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden), UsedImplicitly]
        public int[] Items { get; }
    }
}
