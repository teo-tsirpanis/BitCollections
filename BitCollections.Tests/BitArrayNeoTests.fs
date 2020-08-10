// Copyright (c) 2020 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

[<FsCheck.Xunit.Properties(Arbitrary = [|typeof<BitCollections.Tests.Generators>|], MaxTest = 500, EndSize = 1_000)>]
module BitCollections.Tests.BitArrayNeoTests

open BitCollections
open FsCheck
open FsCheck.Experimental
open FsCheck.Xunit
open global.Xunit

let bitSetEquivelanceMachine (PositiveInt bitCapacity) =
    let universe = BitSet.Universe bitCapacity
    let _or (ban': BitArrayNeo) = {
        new Operation<BitArrayNeo,BitSet>() with
            member _.Run bs =
                let bs' = ban'.ToBitSet()
                BitSet.Union(&bs, &bs')
            member _.Check (ban, bs) =
                let banOld = BitArrayNeo ban
                Assert.NotEqual(ban.Or ban', banOld.Equals ban)
                ban.Equals bs |> Prop.ofTestable
            override _.ToString() = sprintf "or %O" ban'}
    let _and (ban': BitArrayNeo) = {
        new Operation<BitArrayNeo,BitSet>() with
            member _.Run bs =
                let bs' = ban'.ToBitSet()
                BitSet.Intersect(&bs, &bs')
            member _.Check (ban, bs) =
                let banOld = BitArrayNeo ban
                Assert.NotEqual(ban.And ban', banOld.Equals ban)
                ban.Equals bs |> Prop.ofTestable
            override _.ToString() = sprintf "and %O" ban'}
    let _xor (ban': BitArrayNeo) = {
        new Operation<BitArrayNeo,BitSet>() with
            member _.Run bs =
                let bs' = ban'.ToBitSet()
                BitSet.SymmetricDifference(&bs, &bs')
            member _.Check (ban, bs) =
                let banOld = BitArrayNeo ban
                Assert.NotEqual(ban.Xor ban', banOld.Equals ban)
                ban.Equals bs |> Prop.ofTestable
            override _.ToString() = sprintf "xor %O" ban'}
    let _not = {
        new Operation<BitArrayNeo,BitSet>() with
            member _.Run bs =
                universe.Difference &bs
            member _.Check (ban, bs) =
                ban.Not()
                ban.Equals bs |> Prop.ofTestable
            override _.ToString() = "not"
    }
    let flip idx = {
        new Operation<BitArrayNeo,BitSet>() with
            member _.Run bs =
                bs.Set(idx, not bs.[idx])
            member _.Check (ban, bs) =
                Assert.True(ban.Set(idx, not ban.[idx]))
                ban.Equals bs |> Prop.ofTestable
    }
    let create ban = {
        new Setup<BitArrayNeo,BitSet>() with
            member _.Actual() = ban
            member _.Model() = ban.ToBitSet()
    }
    let generator = Generators.BitArrayNeoGen bitCapacity
    {new Machine<BitArrayNeo,BitSet>(15) with
        member _.Setup = generator |> Gen.map create |> Arb.fromGen
        member _.Next _ = Gen.oneof [
            Gen.map _or generator
            Gen.map _and generator
            Gen.map _xor generator
            Gen.constant _not
            Gen.choose(0, bitCapacity - 1) |> Gen.map flip
        ]}

[<Property>]
let ``A BitArrayNeo can be reliably round-tripped into a BitSet`` (bs: BitSet) =
    let ban = BitArrayNeo bs
    let bs' = ban.ToBitSet()
    let ban' = BitArrayNeo bs'
    Assert.Equal<BitSet>(bs, bs')
    Assert.Equal<BitArrayNeo>(ban, ban')

[<Property>]
let ``A BitArrayNeo works as an IEnumerable`` (bs: BitSet) =
    let ban = BitArrayNeo bs
    // Xunit has problems with comparing
    // sequences of a different type.
    Assert.Equal<_ seq>(Seq.readonly bs, Seq.readonly ban)

[<Property>]
let ``Operations on BitSets and BitArrayNeoes match in their behavior`` bitCapacity =
    bitCapacity |> bitSetEquivelanceMachine |> StateMachine.toProperty

[<Property>]
let ``Equality checks between BitSets and BitArrayNeoes work`` (ban: BitArrayNeo) =
    let bs = ban.ToBitSet()
    Assert.True(ban.Equals bs)
    Assert.True(bs.Equals ban)
