# BitCollections

BitCollections is a library that provides efficient collections storing bit values. It provides two types:

* `BitSet`: An immutable struct, allocation-free when bit indices less than 64 are stored.

* `BitArrayNeo`: A mutable class, has a simpler API than `System.Collections.BitArray`, its modification methods return whether the collection changed.

These two types can be converted to each other. Both implement `IEnumerable<int>` for their active bit indices.

## Why not use BitCollections?

BitCollections are not yet SIMD-accelerated, but they store the bits in 64-bit integers (in contrast with `BitArray`'s 32-bits).
