[<FsCheck.Xunit.Properties(Arbitrary = [|typeof<BitCollections.Tests.Generators>|], MaxTest = 500, EndSize = 10_000)>]
module BitCollections.Tests.BitSetTests

open BitCollections
open FsCheck
open FsCheck.Xunit
open global.Xunit

[<Property>]
let ``A BitSet can be reliably round-tripped into an integer sequence`` x =
    let xs = Array.ofSeq x
    let x' = BitSet xs
    Assert.Equal<BitSet>(x, x')
    let xs' = seq x'
    Assert.Equal(xs, xs')

[<Property>]
let ``The union of two BitSets works`` x1 x2 =
    let indirect =
        Set.union (set x1) (set x2)
        |> BitSet
    let direct = BitSet.Union(&x1, &x2)
    Assert.Equal<BitSet>(indirect, direct)

[<Property>]
let ``The union of many BitSets works`` xs =
    let indirect =
        xs
        |> Array.map set
        |> Set.unionMany
        |> BitSet
    let direct = BitSet.UnionMany xs
    Assert.Equal<BitSet>(indirect, direct)

[<Property>]
let ``The difference of two BitSets works`` (x1: BitSet) x2 =
    let indirect =
        Set.difference (set x1) (set x2)
        |> BitSet
    let direct = x1.Difference(&x2)
    Assert.Equal<BitSet>(indirect, direct)

[<Property>]
let ``The intersection of two BitSets works`` x1 x2 =
    let indirect =
        Set.intersect (set x1) (set x2)
        |> BitSet
    let direct = BitSet.Intersect(&x1, &x2)
    Assert.Equal<BitSet>(indirect, direct)

[<Property>]
let ``The intersection of many BitSets works`` (NonEmptyArray xs) =
    let indirect =
        xs
        |> Array.map set
        |> Set.intersectMany
        |> BitSet
    let direct = BitSet.IntersectMany xs
    Assert.Equal<BitSet>(indirect, direct)
