![Licensed under the MIT License](https://img.shields.io/github/license/teo-tsirpanis/BitCollections.svg)
[![NuGet](https://img.shields.io/nuget/v/BitCollections.svg)](https://nuget.org/packages/BitCollections)

# BitCollections

BitCollections is a library that provides efficient collections storing bit values. It provides two types:

* `BitSet`: An immutable struct, allocation-free when small bit indices are stored.

* `BitArrayNeo`: A mutable class, has a similar API with `System.Collections.BitArray`, its modification methods return whether the collection changed.

These two types can be converted and checked for equality against each other. Both implement `IEnumerable<int>`, returning the indices of their active bits.

## Missing features

`BitArrayNeo` is not a drop-in replacement for `BitArray`. In particular, it is missing features like:

* The `LeftShift`/`RightShift` methods
* Some additional constructors (such as accepting an array of bytes or booleans)
* SIMD acceleration

`BitSet` is missing some set-relational operators (such as `IsSubsetOf`) that might come in handy. They will be added in a future release.
