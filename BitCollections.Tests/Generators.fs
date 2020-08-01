namespace BitCollections.Tests

open BitCollections
open FsCheck

[<AbstractClass; Sealed>]
type Generators private() =
    static let bitSetGen size = gen {
        let! data = Arb.generate
        let! extraSize = Gen.choose(0, size)
        let! extra = Arb.generate |> Gen.filter((<>) 0UL) |> Gen.arrayOfLength extraSize
        return BitSet(data, extra)
    }

    static member BitSet() = bitSetGen |> Gen.sized |> Arb.fromGen
