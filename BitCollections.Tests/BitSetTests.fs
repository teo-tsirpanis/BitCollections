[<FsCheck.Xunit.Properties(Arbitrary = [|typeof<BitCollections.Tests.Generators>|], MaxTest = 500, EndSize = 1_000)>]
module BitCollections.Tests.BitSetTests

open System
open BitCollections
open FsCheck
open FsCheck.Xunit
open global.Xunit

let (|||) x1 x2 = BitSet.Union(&x1, &x2)
let (&&&) x1 x2 = BitSet.Intersect(&x1, &x2)
let (-) (x1: BitSet) x2 = x1.Difference(&x2)

[<Property>]
let ``A BitSet can be reliably round-tripped into an integer sequence`` x =
    let xs = Array.ofSeq x
    let x' = BitSet xs
    Assert.Equal<BitSet>(x, x')
    let xs' = seq x'
    Assert.Equal(xs, xs')

[<Property>]
let ``A BitSet can reliably store and retrieve values`` (bs: BitSet) idx =
    if idx < 0 then
        Assert.False(bs.[idx])
        Assert.Throws<ArgumentOutOfRangeException>(Action(fun () -> bs.Set(idx, false) |> ignore)) |> ignore
    else
        let existingVal = bs.[idx]
        Assert.Equal<BitSet>(bs, bs.Set(idx, existingVal))
        let idxFlipped = bs.Set(idx, not existingVal)
        Assert.NotEqual<BitSet>(bs, idxFlipped)
        Assert.NotEqual(existingVal, idxFlipped.[idx])

[<Property>]
let ``The union of two BitSets is commutative`` (x1: BitSet) x2 =
    Assert.Equal<BitSet>(x1 ||| x2, x2 ||| x1)

[<Property>]
let ``The union of two BitSets is associative`` x1 x2 x3 =
    let direct = BitSet.UnionMany[x1; x2; x3]
    Assert.Equal<BitSet>(direct, (x1 ||| x2) ||| x3)
    Assert.Equal<BitSet>(direct, x1 ||| (x2 ||| x3))

[<Property>]
let ``The union of two BitSets has the empty one as the identity element`` x1 =
    Assert.Equal<BitSet>(x1, x1 ||| BitSet.Empty)

[<Property>]
let ``The intersection of two BitSets is commutative`` (x1: BitSet) x2 =
    Assert.Equal<BitSet>(x1 &&& x2, x2 &&& x1)

[<Property>]
let ``The intersection of two BitSets is associative`` x1 x2 x3 =
    let direct = BitSet.IntersectMany[x1; x2; x3]
    Assert.Equal<BitSet>(direct, (x1 &&& x2) &&& x3)
    Assert.Equal<BitSet>(direct, x1 &&& (x2 &&& x3))

[<Property>]
let ``The intersection of two BitSets has the empty one as the anihilating element`` x1 =
    Assert.True((x1 &&& BitSet.Empty).IsEmpty)

[<Fact>]
let ``The intersection of no BitSets raises an exception``() =
    Assert.Throws<ArgumentException>(Action(fun () -> BitSet.IntersectMany Seq.empty |> ignore))

[<Property>]
let ``The difference of two BitSets has the empty one as the identity element`` x =
    Assert.Equal<BitSet>(x, x - BitSet.Empty)

[<Property>]
let ``The difference a BitSet with itself results in an empty BitSet`` x =
    Assert.Equal<BitSet>(BitSet.Empty, x - x)

[<Property>]
let ``Singleton returns a BitSet with only one element`` idx =
    if idx < 0 then
        Assert.Throws<ArgumentOutOfRangeException>(Action(fun () -> BitSet.Singleton idx |> ignore)) |> ignore
    else
        Assert.Single(BitSet.Singleton idx) |> ignore

[<Property>]
let ``Unsetting the only bit of a BitSet correctly trims the extra array`` (NonNegativeInt x) =
    let bs = BitSet.Singleton x
    bs.Set(x, false) |> ignore
