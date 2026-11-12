using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The workshop's berry garden, CR8 (plan 39): a map of ours behind Sunyshore City's workshop, two rows of twelve soils
/// numbered after Johto's pots; they grow as any soil does, cross along their row with the kit in the bag, and come to an
/// older save bare, every patch it had keeping its number. Its keeper sells the mulches and shows the crosses.
/// </summary>
public class BerryGardenTests
{
    private const string GardenMap = "SunyshoreGarden";

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static List<NPC> Soils(Map map) => map.NPCs.Where(n => n.IsBerrySoil).OrderBy(n => n.BerryPatch).ToList();

    private static HeadlessScriptHost Host(Map map, bool kit = true)
    {
        var host = new HeadlessScriptHost { Berries = new BerryPatches(), Map = map };
        if (kit) host.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        return host;
    }

    private static void Play(HeadlessScriptHost host, string script, NPC? subject)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, GardenMap)!, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }

    private static void PlantIn(HeadlessScriptHost host, NPC soil, string berry)
    {
        host.Bag.AddItem(ItemDatabase.Get(berry)!, 1);
        host.PlayerFacing = Direction.Up;
        host.ItemChoice = berry;
        host.Answers.Clear();
        host.Answers.Enqueue(0);
        Play(host, FieldScripts.BerryPatch, soil);
    }

    [Fact]
    public void TheGardenHasTwoRowsOfTwelveSoilsNumberedAfterThePots()
    {
        Assert.Equal(BerryPatches.PotFirst + BerryPatches.PotCount, BerryPatches.GardenFirst);
        Assert.Equal(BerryPatches.GardenFirst + BerryPatches.GardenCount, BerryPatches.Count);
        var map = Room(GardenMap);
        var soils = Soils(map);
        Assert.Equal(24, soils.Count);
        Assert.Equal(Enumerable.Range(BerryPatches.GardenFirst, BerryPatches.GardenCount), soils.Select(s => s.BerryPatch!.Value));
        Assert.Equal(2, soils.Select(s => s.GridY).Distinct().Count());
        Assert.All(soils.GroupBy(s => s.GridY), row => Assert.Equal(12, row.Count()));
        // Each is tended from the tile south of it, which is open ground
        Assert.All(soils, s => Assert.True(map.IsWalkable(s.GridX, s.GridY + 1), $"nothing to stand on below the soil at {s.GridX},{s.GridY}"));
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(GardenMap)!.Id);
    }

    [Fact]
    public void TheWorkshopsBackDoorAndTheGardensGateLeadToEachOther()
    {
        var workshop = Room("SunyshoreWorkshop");
        var garden = Room(GardenMap);
        var into = workshop.Warps.Single(w => w.TargetMap == GardenMap);
        var back = garden.Warps.Single(w => w.TargetMap == "SunyshoreWorkshop");
        Assert.True(garden.IsWalkable(into.TargetX, into.TargetY));
        Assert.True(workshop.IsWalkable(back.TargetX, back.TargetY));
        // The way out of the one is a step from where the other puts the player
        Assert.Equal((back.SourceX, back.SourceY), (into.TargetX, into.TargetY + 1));
        Assert.Equal((into.SourceX, into.SourceY), (back.TargetX, back.TargetY - 1));
        // The street's door stays the room's first way out, so a save in the room still wakes inside it
        Assert.Equal("Sinnoh", workshop.Warps[0].TargetMap);
    }

    [Fact]
    public void TheGardensSoilsGrowAndCrossAlongTheirRowOnly()
    {
        var map = Room(GardenMap);
        var host = Host(map);
        var soils = Soils(map);
        var first = soils[0];
        var beside = soils.Single(s => s.GridY == first.GridY && s.GridX == first.GridX + 1);
        var otherRow = soils.Single(s => s.GridY != first.GridY && s.GridX == first.GridX);
        PlantIn(host, first, "Cheri Berry");
        PlantIn(host, otherRow, "Figy Berry");
        Assert.Null(host.Berries[first.BerryPatch!.Value].Cross);
        PlantIn(host, beside, "Figy Berry");
        Assert.Equal("Liechi Berry", host.Berries[first.BerryPatch!.Value].Cross);
        Assert.Equal("Liechi Berry", host.Berries[beside.BerryPatch!.Value].Cross);
        Assert.Null(host.Berries[otherRow.BerryPatch!.Value].Cross);

        // Grown and picked with the roll fixed under its odds: one of the cross among the fruit
        host.Berries.MinutesPass(4 * 180);
        Assert.Equal(BerryStage.Fruit, host.Berries[first.BerryPatch!.Value].Stage);
        var fixedRoll = new FixedRandom(0);
        host.Rng = fixedRoll;
        host.PlayerFacing = Direction.Up;
        host.Answers.Clear();
        host.Answers.Enqueue(0);
        Play(host, FieldScripts.BerryPatch, first);
        Assert.Equal(BerryStage.None, host.Berries[first.BerryPatch!.Value].Stage);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Liechi Berry")!));
    }

    [Fact]
    public void WithoutTheKitTheGardensSoilsGrowAndNoteNoCross()
    {
        var map = Room(GardenMap);
        var host = Host(map, kit: false);
        var soils = Soils(map);
        PlantIn(host, soils[0], "Cheri Berry");
        PlantIn(host, soils[1], "Figy Berry");
        Assert.Equal("Cheri Berry", host.Berries[soils[0].BerryPatch!.Value].Berry);
        Assert.All(host.Berries.Patches, p => Assert.Null(p.Cross));
    }

    [Fact]
    public void AnOlderSaveIsGivenTheGardenBareAndKeepsItsPots()
    {
        var old = new BerryPatches { Patches = Enumerable.Range(0, BerryPatches.GardenFirst).Select(_ => new BerryPatch()).ToList() };
        old.Plant(BerryPatches.PotFirst + 1, "Oran Berry");
        old.Grown();
        Assert.Equal(BerryPatches.Count, old.Patches.Count);
        Assert.Equal("Oran Berry", old[BerryPatches.PotFirst + 1].Berry);
        Assert.All(old.Patches.Skip(BerryPatches.GardenFirst), p => Assert.Null(p.Berry));

        // and through the save's own reading, as a save of today's game round trips
        string json = System.Text.Json.JsonSerializer.Serialize(old, GameDataFiles.Json);
        var back = System.Text.Json.JsonSerializer.Deserialize<BerryPatches>(json, GameDataFiles.Json)!;
        Assert.Equal(BerryPatches.Count, back.Patches.Count);
    }

    [Fact]
    public void TheKeeperSellsMulchAndShowsTheCrossesOrSaysWhatTheyWant()
    {
        Assert.Contains("Gooey Mulch", MartDatabase.Stock("workshop_garden", 0).Select(i => i.Name));
        var map = Room(GardenMap);
        var keeper = map.NPCs.Single(n => n.Key == "garden_keeper");

        var shopping = Host(map);
        shopping.Answers.Enqueue(0);
        Play(shopping, "GardenKeeper", keeper);
        Assert.Contains(shopping.Log, l => l.Contains("shop") && l.Contains("workshop_garden"));

        var crosses = Host(map);
        crosses.Answers.Enqueue(1);
        Play(crosses, "GardenKeeper", keeper);
        Assert.Contains(crosses.Log, l => l.Contains("Crafting") && l.Contains("crosses"));

        var without = Host(map, kit: false);
        without.Answers.Enqueue(1);
        Play(without, "GardenKeeper", keeper);
        Assert.DoesNotContain(without.Log, l => l.Contains("Crafting"));
        Assert.Contains(without.Transcript, t => t.Text.Contains("craftswoman"));
    }

    /// <summary>A generator that always answers the same number.</summary>
    private sealed class FixedRandom(int value) : Random
    {
        public override int Next(int maxValue) => Math.Min(value, Math.Max(0, maxValue - 1));
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
        public override int Next() => value;
        public override double NextDouble() => 0;
    }
}
