using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Headbutt trees (plan 38 · EX4; <c>docs/mechanics/rulings.md</c>, "Headbutt trees"): HeartGold's trees and tables in
/// Johto's areas, our rule of which table a tree gives, the field move's check and the Pokédex's area page.
/// </summary>
public class HeadbuttTests
{
    private static readonly Lazy<List<Map>> JohtoMaps =
        new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Johto).BuildMaps().ToList());

    [Fact]
    public void EveryTreeOfJohtosAreasStandsOnAHeadbuttTile()
    {
        // HeartGold's list names a few tiles beside a tree too (two of Route 35's lie in its grass), which nobody faces
        // as a tree: every tree has a tile of the behaviour, and all but a handful of the tiles are
        // (and a tree or two of HeartGold's Kanto routes stands wholly off it, shaken by nobody)
        int trees = 0, tiles = 0, off = 0, bare = 0;
        foreach (var map in JohtoMaps.Value)
            foreach (var area in map.Areas.Where(a => a.Headbutt != null).Distinct())
            {
                trees += area.Headbutt!.TreeCount;
                Assert.Equal(6, area.Headbutt.Common.Count);
                Assert.Equal(HeadbuttTrees.SlotWeights, area.Headbutt.Common.Select(e => e.Weight));
                bare += area.Headbutt.Tiles.GroupBy(t => t.Value).Count(g => g.All(t => map.BehaviourAt(t.Key.X, t.Key.Y) != TileBehavior.HeadbuttTree));
                tiles += area.Headbutt.Tiles.Count;
                off += area.Headbutt.Tiles.Keys.Count(t => map.BehaviourAt(t.X, t.Y) != TileBehavior.HeadbuttTree);
            }
        Assert.True(trees > 400, $"only {trees} trees");
        Assert.True(tiles > trees);
        Assert.True(off * 50 < tiles, $"{off} of {tiles} tiles are no Headbutt tree");
        Assert.True(bare * 100 < trees, $"{bare} of {trees} trees stand on no Headbutt tile");
        // Johto's own routes' trees each have one
        foreach (var key in new[] { "route_29", "route_30", "route_31", "route_33", "route_42", "route_43", "new_bark" })
        {
            var area = JohtoMaps.Value.Single(m => m.Name == "Johto").Areas.Single(a => a.Key == key);
            Assert.All(area.Headbutt!.Tiles.GroupBy(t => t.Value), g => Assert.Contains(g, t => JohtoMaps.Value.Single(m => m.Name == "Johto").BehaviourAt(t.Key.X, t.Key.Y) == TileBehavior.HeadbuttTree));
        }
        // Route 29's and Ilex Forest's, with HeartGold's species
        var johto = JohtoMaps.Value.Single(m => m.Name == "Johto");
        var route = johto.Areas.Single(a => a.Key == "route_29").Headbutt!;
        Assert.Contains(route.Common, e => e.SpeciesName == "Exeggcute");
        Assert.Contains(route.Rare, e => e.SpeciesName == "Spinarak");
        var ilex = JohtoMaps.Value.Single(m => m.Name == "IlexForest");
        Assert.NotNull(ilex.Areas.Single(a => a.Key == "ilex_forest").Headbutt);
    }

    [Fact]
    public void ATreesTableFollowsFromTheTrainerNumberWithNoDraw()
    {
        // Two in five plain trees are rich for a player, by the last digit of the shown number plus the tree's number
        Assert.Equal(HeadbuttTreeKind.Rich, HeadbuttTrees.KindOf(0, false, 10));
        Assert.Equal(HeadbuttTreeKind.Rich, HeadbuttTrees.KindOf(1, false, 10));
        Assert.Equal(HeadbuttTreeKind.Plain, HeadbuttTrees.KindOf(2, false, 10));
        Assert.Equal(HeadbuttTreeKind.Plain, HeadbuttTrees.KindOf(0, false, 12));
        Assert.Equal(HeadbuttTreeKind.Rich, HeadbuttTrees.KindOf(3, false, 12));
        // The digit is of the 16 bits on the Trainer Card
        Assert.Equal(HeadbuttTrees.KindOf(4, false, 7), HeadbuttTrees.KindOf(4, false, 0x10007));
        Assert.Equal(HeadbuttTreeKind.Secret, HeadbuttTrees.KindOf(4, true, 7));
        for (int tree = 0; tree < 50; tree++)
            Assert.Equal(2 * 10, Enumerable.Range(0, 50).Count(t => HeadbuttTrees.KindOf(t, false, (uint)tree) == HeadbuttTreeKind.Rich));

        // Shaken a thousand times, the same tree gives one table to one player and the other to another
        var johto = JohtoMaps.Value.Single(m => m.Name == "Johto");
        var trees = johto.Areas.Single(a => a.Key == "route_29").Headbutt!;
        var common = trees.Common.Select(e => e.SpeciesName).ToHashSet();
        var rare = trees.Rare.Select(e => e.SpeciesName).ToHashSet();
        int Falls(uint number, HashSet<string> table)
        {
            var random = new Random(4);
            var met = Enumerable.Range(0, 1000).Select(_ => HeadbuttTrees.Shake(trees, 0, false, number, random)).OfType<WildEncounterEntry>().ToList();
            Assert.All(met, m => Assert.Contains(m.SpeciesName, table));
            return met.Count;
        }
        Assert.InRange(Falls(10, rare), 740, 860);   // rich: four times in five
        Assert.InRange(Falls(12, common), 440, 560); // plain: half the time
        Assert.All(trees.Rare.Concat(trees.Common), e => Assert.True(e.MaxLevel > e.MinLevel));
    }

    [Fact]
    public void ATreeNoAreaListsDropsNothingAndDrawsNothing()
    {
        var johto = JohtoMaps.Value.Single(m => m.Name == "Johto");
        // Route 29's woods: 06 tiles that are no listed tree (row 395 west of the first tree)
        Assert.Equal(TileBehavior.HeadbuttTree, johto.BehaviourAt(587, 395));
        Assert.Null(johto.HeadbuttTreeAt(587, 395));
        var random = new Random(1);
        int before = random.Next();
        random = new Random(1);
        Assert.Null(johto.ShakeHeadbuttTree(587, 395, 12, random: random));
        Assert.Equal(before, random.Next());
        Assert.NotNull(johto.HeadbuttTreeAt(588, 396));
    }

    [Fact]
    public void HeadbuttIsAFieldMoveOfJohtosAloneAndAsksNoBadge()
    {
        var story = new StoryState();
        var tree = new FieldSpot { HeadbuttTree = true, Region = RegionDatabase.Johto };
        Assert.Null(FieldMoveRules.BadgeFor(FieldMove.Headbutt, RegionDatabase.Johto));
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Headbutt, tree, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.Headbutt, tree with { HeadbuttTree = false }, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.Headbutt, tree with { Surfing = true }, story));
        Assert.Equal("common.UseHeadbutt", FieldScripts.FromMenu(FieldMove.Headbutt));
        var knower = new Pokemon(PokemonDatabase.Get("Slowpoke")!, 20);
        knower.Moves.Clear();
        knower.Moves.Add(new Move(MoveDatabase.Get("Headbutt")!));
        Assert.Contains(PartyScreen.ActionsFor(knower, RegionDatabase.Johto), a => a.Move == FieldMove.Headbutt);
        Assert.DoesNotContain(PartyScreen.ActionsFor(knower, RegionDatabase.Sinnoh), a => a.Move == FieldMove.Headbutt);
        Assert.DoesNotContain(PartyScreen.ActionsFor(knower, RegionDatabase.Hoenn), a => a.Move == FieldMove.Headbutt);
        // On Route 29, facing the first tree from the south
        var johto = JohtoMaps.Value.Single(m => m.Name == "Johto");
        var spot = FieldMoveRules.SpotOf(johto, 588, 398, Direction.Up, new Walker(TravelMode.OnFoot, johto.HeightAt(588, 398)));
        Assert.True(spot.HeadbuttTree);
    }

    [Fact]
    public void TheTreesSpeciesAreOnJohtosAreaPage()
    {
        var habitats = Habitats.Of(RegionDatabase.Get(RegionDatabase.Johto)!)!;
        var places = habitats.Of("Pineco");
        Assert.Contains(places, p => p.Ways.HasFlag(HabitatWays.Headbutt));
        Assert.Contains(habitats.Of("Exeggcute"), p => p.Name == "Route 29" && p.Ways.HasFlag(HabitatWays.Headbutt));
    }
}
