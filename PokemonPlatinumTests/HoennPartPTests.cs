using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · H12, part p (the look at Hoenn's last places): the Battle Frontier island's halls and the Trainer Hill
/// built as halls and a tower rather than cottages, the Trainer Hill's trainers and Hoenn's triathletes in looks of their
/// own rather than the boy's, the Mechadolls, Birth Island's triangle and the Abandoned Ship's wreck.
/// </summary>
public class HoennPartPTests
{
    private static readonly Lazy<List<Map>> Maps = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().ToList());

    private static Map MapNamed(string name) => Maps.Value.Single(m => m.Name == name);

    [Theory]
    [InlineData("battle_frontier_battle_tower_lobby", BuildingKind.Tower)]
    [InlineData("battle_frontier_battle_dome_lobby", BuildingKind.Hall)]
    [InlineData("battle_frontier_battle_palace_lobby", BuildingKind.Hall)]
    [InlineData("battle_frontier_battle_arena_lobby", BuildingKind.Hall)]
    [InlineData("battle_frontier_battle_factory_lobby", BuildingKind.Hall)]
    [InlineData("battle_frontier_battle_pike_lobby", BuildingKind.Hall)]
    [InlineData("battle_frontier_battle_pyramid_lobby", BuildingKind.Hall)]
    [InlineData("battle_frontier_lounge_1", BuildingKind.House)]
    public void TheFrontierIslandsShutDoorsAreHallsAndATower(string area, BuildingKind kind) =>
        Assert.Equal(kind, WorldMapBuilder.KindOfArea(area));

    [Fact]
    public void TheTrainerHillTheRankingHallAndTheExchangeCornerAreNoCottages()
    {
        Assert.Equal(BuildingKind.Tower, MapStructures.KindOfDoor("TrainerHillLobby"));
        Assert.Equal(BuildingKind.Hall, MapStructures.KindOfDoor("BattleFrontier_RankingHall"));
        Assert.Equal(BuildingKind.Shop, MapStructures.KindOfDoor("BattleFrontier_ExchangeServiceCorner"));
    }

    [Fact]
    public void TheIslandsHallsAndTheHillStandAsThoseKindsInTheWorld()
    {
        var island = MapNamed("HoennBattleFrontier");
        // The Battle Tower's door (72, 14) and the Battle Dome's (19, 17)
        // (a shut door is its building's all the same, though it lists no way through)
        static bool At(BuildingInfo b, int x, int y) => x >= b.X0 && x <= b.X1 && y >= b.Y0 && y <= b.Y1;
        Assert.Contains(island.PlacedBuildings, b => b.Kind == BuildingKind.Tower && At(b, 72, 14));
        Assert.Contains(island.PlacedBuildings, b => b.Kind == BuildingKind.Hall && At(b, 19, 17));
        var hoenn = MapNamed("Hoenn");
        var near = hoenn.PlacedBuildings.Where(b => b.X1 >= 224 && b.X0 <= 238 && b.Y1 >= 106 && b.Y0 <= 116).ToList();
        Assert.True(near.Any(b => b.Kind == BuildingKind.Tower),
            string.Join("; ", near.Select(b => $"{b.Kind} {b.X0},{b.Y0}-{b.X1},{b.Y1} doors {string.Join(",", b.Doors)}")));
    }

    [Fact]
    public void EveryTrainerHillTrainerStandsInALookOfItsOwnNotTheBoys()
    {
        var styles = CharacterStyles.Default;
        // What a look nobody named stands as: the boy
        string boy = styles.CharacterFor("no look of anyone's");
        foreach (var trainer in Enumerable.Range(0, TrainerHillDatabase.Modes.Count).SelectMany(TrainerHillDatabase.TrainersOf))
        {
            string look = styles.CharacterFor(trainer.Look);
            Assert.True(styles.Has(look), $"{trainer.Class} {trainer.Name} stands as {look}, which is no look");
            Assert.NotEqual(boy, look);
        }
    }

    [Theory]
    [InlineData("hex_maniac", "psychic")]
    [InlineData("cycling_triathlete_m", "cyclist_m")]
    [InlineData("running_triathlete_f", "jogger")]
    [InlineData("swimming_triathlete_f", "swimmer_f")]
    [InlineData("gameboy_kid", "kid_with_nds")]
    public void HoennsTrainerLooksStandAsTheNearestOfOurs(string gba, string ours) =>
        Assert.Equal(ours, CharacterStyles.Default.CharacterFor(gba));

    [Fact]
    public void TheMechadollsWearTheirOwnLook()
    {
        Assert.True(CharacterStyles.Default.Has("mechadoll"));
        var puzzle = MapNamed("Route110_TrickHousePuzzle5");
        var dolls = puzzle.Everyone.Where(n => n.Key?.StartsWith("mechadoll_") == true).ToList();
        Assert.Equal(5, dolls.Count);
        Assert.All(dolls, d => Assert.Equal("mechadoll", d.NpcType));
    }

    [Fact]
    public void BirthIslandsTriangleIsACrystalCardOfItsOwn()
    {
        var island = MapNamed("BirthIsland_Exterior");
        var triangle = Assert.Single(island.Everyone, n => n.Key == "birth_island_exterior_rock");
        Assert.Equal(PropType.DeoxysTriangle, triangle.GymThing);
        var card = ThingCards.Paint(PropType.DeoxysTriangle);
        Assert.Equal((HoennArt.TriangleWidth, HoennArt.TriangleHeight), (card.Width, card.Height));
        // Its point stands high in the card, its foot low, and its corners are clear
        Assert.True(card.IsOpaque(15, 4) && card.IsOpaque(15, 28));
        Assert.False(card.IsOpaque(1, 1) || card.IsOpaque(28, 1));
    }

    [Fact]
    public void TheAbandonedShipLiesInRoute108AsAWreck()
    {
        var hoenn = MapNamed("Hoenn");
        Assert.Contains(hoenn.Props, p => p.Type == PropType.Wreck && p.Covers(169, 364));
        Assert.DoesNotContain(hoenn.Props, p => p.Type == PropType.Boat && p.Covers(169, 364));
    }
}
