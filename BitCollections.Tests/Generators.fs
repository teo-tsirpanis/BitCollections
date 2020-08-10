namespace BitCollections.Tests

open BitCollections
open FsCheck

[<AbstractClass; Sealed>]
type Generators private() =
    static let bitArrayNeoGen (size: int) = gen {
        let ban = BitArrayNeo(size)
        for i = 0 to size - 1 do
            let! bit = Arb.generate
            ban.[i] <- bit
        return ban
    }
    static let bitSetGen size = gen {
        let! data = Arb.generate
        let! extraSize = Gen.choose(0, size)
        let! extra = Arb.generate |> Gen.filter((<>) 0UL) |> Gen.arrayOfLength extraSize
        return BitSet(data, extra)
    }

    static member BitSet() = bitSetGen |> Gen.sized |> Arb.fromGen
    static member BitArrayNeo() = bitArrayNeoGen |> Gen.sized |> Arb.fromGen
    static member BitArrayNeoGen size = bitArrayNeoGen size
