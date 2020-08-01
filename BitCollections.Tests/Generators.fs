namespace BitCollections.Tests

open BitCollections
open FsCheck

[<AbstractClass; Sealed>]
type Generators private() =
    static let bitSetArb =
        Arb.generate
        |> Gen.listOf
        |> Gen.map (List.distinct >> List.map (fun (NonNegativeInt x) -> x) >> BitSet)
        |> Arb.fromGen

    static member BitSet() = bitSetArb
