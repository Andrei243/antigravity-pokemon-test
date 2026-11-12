using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Crossing berries, CR5 (plan 39): two touching soils of a pair note the cross on both, and soils apart, a pair of no
/// cross or a planting without the kit note nothing; a harvest of a noted patch rolls once and gives one of the cross;
/// a patch with no cross draws nothing; each of the fifteen crosses comes from berries the world gives; the note and the
/// crosses found are saved; Johto's Berry Pots are planted, watered, picked and crossed from the bag, given once by the
/// florist to a player with the kit, and an older save is given their patches.
/// </summary>
public class BerryCrossingTests
{
    /// <summary>A generator that answers with the numbers it is given, in order, and counts what is drawn.</summary>
    private sealed class Scripted(params int[] rolls) : Random
    {
        private readonly Queue<int> left = new(rolls);
        public int Drawn { get; private set; }
        public override int Next(int maxValue) { Drawn++; return left.Count > 0 ? Math.Min(left.Dequeue(), Math.Max(0, maxValue - 1)) : 0; }
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
        public override int Next() { Drawn++; return 0; }
        public override double NextDouble() { Drawn++; return 0; }
    }

    private static NPC Soil(int patch, int x, int y) => new() { Name = "Soft soil", NpcType = NPC.BerrySoilType, BerryPatch = patch, GridX = x, GridY = y };

    private static HeadlessScriptHost WithKit(HeadlessScriptHost host)
    {
        host.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        return host;
    }

    private static void Play(HeadlessScriptHost host, string script, NPC? subject, bool strict = true)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script)!, subject);
        runner.RunToEnd();
        if (strict) Assert.Empty(host.Problems);
    }

    /// <summary>Plants a berry in a soil of a map from the bag, as the soil's own script does.</summary>
    private static void PlantIn(HeadlessScriptHost host, NPC soil, string berry)
    {
        host.Bag.AddItem(ItemDatabase.Get(berry)!, 1);
        host.PlayerFacing = Direction.Up;
        host.ItemChoice = berry;
        host.Answers.Clear();
        host.Answers.Enqueue(0);
        Play(host, FieldScripts.BerryPatch, soil);
    }

    private static (HeadlessScriptHost Host, NPC A, NPC B, NPC Far) Garden(bool kit = true)
    {
        var map = new Map(10, 10);
        var a = Soil(0, 3, 3);
        var b = Soil(1, 4, 4);
        var far = Soil(2, 8, 8);
        map.Add(a);
        map.Add(b);
        map.Add(far);
        var host = new HeadlessScriptHost { Berries = new BerryPatches(), Map = map };
        return (kit ? WithKit(host) : host, a, b, far);
    }

    // ---------------------------------------------------------------- the table

    [Fact]
    public void TheTableNamesOnlyFlavoursAndBerriesThatAreThereAndEachCrossOnce()
    {
        Assert.Empty(BerryCrossing.Problems());
        Assert.Equal(15, BerryCrossing.Table.Rows.Count);
        Assert.Equal("Spicy", BerryCrossing.Strongest("Cheri Berry"));
        Assert.Null(BerryCrossing.Strongest("Liechi Berry"));
        Assert.Equal("Liechi Berry", BerryCrossing.CrossOf("Cheri Berry", "Figy Berry"));
        Assert.Null(BerryCrossing.CrossOf("Cheri Berry", "Cheri Berry"));
        Assert.Null(BerryCrossing.CrossOf("Cheri Berry", "Pecha Berry"));
        Assert.Equal("Lansat Berry", BerryCrossing.CrossOf("Pecha Berry", "Liechi Berry"));
    }

    [Fact]
    public void EveryCrossComesFromBerriesTheWorldGives()
    {
        // A fixed point: what the world gives, then whatever two berries had cross into, until nothing more comes
        // (seeded only from a way other than crossing itself, which ItemSources counts once the kit is given)
        var sources = ItemSources.Everywhere;
        var had = sources.Items.Where(i => ItemDatabase.Get(i)?.Berry != null && sources.Of(i).Any(w => w.Kind != ItemSourceKind.Crossed)).ToHashSet();
        Assert.All(BerryCrossing.Table.Rows, r => Assert.True(sources.Has(r.Makes, ItemSourceKind.Crossed)));
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (string a in had.ToList())
                foreach (string b in had.ToList())
                    if (BerryCrossing.CrossOf(a, b) is { } cross && had.Add(cross)) grew = true;
        }
        foreach (var row in BerryCrossing.Table.Rows) Assert.Contains(row.Makes, had);
    }

    // ---------------------------------------------------------------- noting a cross

    [Fact]
    public void TwoTouchingSoilsOfAPairNoteTheCrossOnBoth()
    {
        var (host, a, b, _) = Garden();
        PlantIn(host, a, "Cheri Berry");
        Assert.Null(host.Berries[0].Cross);
        PlantIn(host, b, "Figy Berry");
        Assert.Equal("Liechi Berry", host.Berries[0].Cross);
        Assert.Equal("Liechi Berry", host.Berries[1].Cross);
    }

    [Fact]
    public void SoilsApartAPairOfNoCrossOrNoKitNoteNothing()
    {
        var (host, a, _, far) = Garden();
        PlantIn(host, a, "Cheri Berry");
        PlantIn(host, far, "Figy Berry");
        Assert.All(host.Berries.Patches, p => Assert.Null(p.Cross));

        var (other, c, d, _) = Garden();
        PlantIn(other, c, "Cheri Berry");
        PlantIn(other, d, "Pecha Berry");
        Assert.All(other.Berries.Patches, p => Assert.Null(p.Cross));

        var (bare, e, f, _) = Garden(kit: false);
        PlantIn(bare, e, "Cheri Berry");
        PlantIn(bare, f, "Figy Berry");
        Assert.All(bare.Berries.Patches, p => Assert.Null(p.Cross));
    }

    // ---------------------------------------------------------------- the harvest

    private static HeadlessScriptHost Harvest(string? cross, int yield, Random rng)
    {
        var patches = new BerryPatches();
        patches.Plant(0, "Cheri Berry");
        patches.MinutesPass(4 * 180);
        patches[0].Yield = yield;
        patches[0].Cross = cross;
        var host = WithKit(new HeadlessScriptHost { Berries = patches, Rng = rng });
        host.PlayerFacing = Direction.Up;
        Play(host, FieldScripts.BerryPatch, Soil(0, 1, 1));
        return host;
    }

    [Fact]
    public void AHarvestOfANotedPatchRollsOnceAndGivesOneOfTheCross()
    {
        var rng = new Scripted(0);
        var host = Harvest("Liechi Berry", 3, rng);
        Assert.Equal(1, rng.Drawn);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Liechi Berry")!));
        Assert.Equal(2, host.Bag.GetQuantity(ItemDatabase.Get("Cheri Berry")!));
        Assert.Contains("Liechi Berry", host.Crafting!.Crosses);
        Assert.Contains(host.Transcript, t => t.Text.Contains("crossed into a Liechi Berry"));
        Assert.Null(host.Berries[0].Cross);

        // Over its odds the roll gives none of the cross, and the fruit is all the patch's own
        var missed = Harvest("Liechi Berry", 3, new Scripted(99));
        Assert.Equal(0, missed.Bag.GetQuantity(ItemDatabase.Get("Liechi Berry")!));
        Assert.Equal(3, missed.Bag.GetQuantity(ItemDatabase.Get("Cheri Berry")!));
    }

    [Fact]
    public void APatchWithNoCrossDrawsNothing()
    {
        var rng = new Scripted();
        var host = Harvest(null, 3, rng);
        Assert.Equal(0, rng.Drawn);
        Assert.Equal(3, host.Bag.GetQuantity(ItemDatabase.Get("Cheri Berry")!));
    }

    // ---------------------------------------------------------------- the save

    [Fact]
    public void TheCrossAndTheCrossesFoundAreSavedAndAnOlderSaveHasNone()
    {
        var patches = new BerryPatches();
        patches.Plant(0, "Cheri Berry");
        patches[0].Cross = "Liechi Berry";
        string json = System.Text.Json.JsonSerializer.Serialize(patches, GameDataFiles.Json);
        var back = System.Text.Json.JsonSerializer.Deserialize<BerryPatches>(json, GameDataFiles.Json)!;
        Assert.Equal("Liechi Berry", back[0].Cross);
        Assert.DoesNotContain("\"cross\":null", json.Replace(" ", ""));

        var state = new CraftingState();
        Assert.Null(state.ToSave());
        state.FoundCross("Liechi Berry");
        var saved = state.ToSave();
        Assert.NotNull(saved);
        var loaded = new CraftingState();
        loaded.Load(saved);
        Assert.Equal(new[] { "Liechi Berry" }, loaded.Crosses);
    }

    // ---------------------------------------------------------------- Johto's Berry Pots

    [Fact]
    public void ThePotsArePlantedWateredPickedAndCrossFromTheBag()
    {
        var host = WithKit(new HeadlessScriptHost { Berries = new BerryPatches() });
        int first = BerryPatches.PotFirst;
        host.Bag.AddItem(ItemDatabase.Get("Cheri Berry")!, 1);
        host.Bag.AddItem(ItemDatabase.Get("Figy Berry")!, 1);
        // The first pot: plant a Cheri Berry
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        host.ItemChoice = "Cheri Berry";
        Play(host, FieldScripts.BerryPots, null);
        Assert.Equal("Cheri Berry", host.Berries[first].Berry);
        // The second pot, beside it: a Figy Berry, which crosses
        host.Answers.Clear();
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(0);
        host.ItemChoice = "Figy Berry";
        Play(host, FieldScripts.BerryPots, null);
        Assert.Equal("Figy Berry", host.Berries[first + 1].Berry);
        Assert.Equal("Liechi Berry", host.Berries[first].Cross);
        Assert.Equal("Liechi Berry", host.Berries[first + 1].Cross);
        // Watered
        host.Berries[first].Moisture = 10;
        host.Answers.Clear();
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Play(host, FieldScripts.BerryPots, null);
        Assert.Equal(100, host.Berries[first].Moisture);
        // Grown and picked
        host.Berries.MinutesPass(4 * 180);
        host.Answers.Clear();
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Play(host, FieldScripts.BerryPots, null);
        Assert.Equal(BerryStage.None, host.Berries[first].Stage);
        Assert.True(host.Bag.GetQuantity(ItemDatabase.Get("Cheri Berry")!) + host.Bag.GetQuantity(ItemDatabase.Get("Liechi Berry")!) >= 2);
    }

    private static NPC Florist(HeadlessScriptHost host)
    {
        var florist = new NPC { Name = "Florist", NpcType = "Lady", Key = "t25r0601_gswoman1", ScriptFile = "goldenrod_flower_shop" };
        var map = new Map(8, 8);
        map.Add(florist);
        host.Map = map;
        return florist;
    }

    [Fact]
    public void TheFloristGivesThePotsOnceToAPlayerWithTheKit()
    {
        var story = new StoryState();
        story.Set("FLAG_UNK_0B5");
        var without = new HeadlessScriptHost(story);
        without.Answers.Enqueue(4);
        Play(without, "goldenrod_flower_shop.Florist", Florist(without));
        Assert.Equal(0, without.Bag.GetQuantity(ItemDatabase.Get(GameEngine.BerryPotsItem)!));

        var with = WithKit(new HeadlessScriptHost(story));
        with.Answers.Enqueue(4);
        Play(with, "goldenrod_flower_shop.Florist", Florist(with));
        Assert.Equal(1, with.Bag.GetQuantity(ItemDatabase.Get(GameEngine.BerryPotsItem)!));
        with.Answers.Enqueue(4);
        Play(with, "goldenrod_flower_shop.Florist", Florist(with));
        Assert.Equal(1, with.Bag.GetQuantity(ItemDatabase.Get(GameEngine.BerryPotsItem)!));
        Assert.True(BagScreen.UsedInField(ItemDatabase.Get(GameEngine.BerryPotsItem)!));
    }

    [Fact]
    public void AnOlderSaveIsGivenThePotsPatches()
    {
        var old = new BerryPatches { Patches = Enumerable.Range(0, BerryPatches.PotFirst).Select(_ => new BerryPatch()).ToList() };
        Assert.Equal(BerryPatches.Count, old.Grown().Patches.Count);
        Assert.All(old.Patches.Skip(BerryPatches.PotFirst), p => Assert.Null(p.Berry));
    }
}
