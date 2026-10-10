using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Sinnoh's berry patches (plan 06 · R14a): the growing of <c>src/berry_patches.c</c>, the patches a new game finds,
/// the soil the world places, and <c>common.BerryPatch</c>, the script that reads, mulches, plants, waters and picks.
/// </summary>
public class BerryPatchesTests
{
    // A Cheri Berry's growing (the item table): a stage of 3 hours, 15 of water lost an hour, a yield of 1 a point.
    private const int CheriStage = 180;

    private static BerryPatches Planted(string berry = "Cheri Berry", Mulch mulch = Mulch.None)
    {
        var patches = new BerryPatches();
        if (mulch != Mulch.None) patches.LayMulch(0, mulch);
        patches.Plant(0, berry);
        return patches;
    }

    // ---------------------------------------------------------------- growing

    [Fact]
    public void ABerryGrowsAStageAtATimeByItsOwnHours()
    {
        var patches = Planted();
        Assert.Equal((BerryStage.Planted, CheriStage), (patches[0].Stage, patches[0].StageMinutes));

        patches.MinutesPass(CheriStage - 1);
        Assert.Equal(BerryStage.Planted, patches[0].Stage);
        patches.MinutesPass(1);
        Assert.Equal((BerryStage.Sprouted, CheriStage), (patches[0].Stage, patches[0].StageMinutes));

        // Many stages at once go stage by stage: Growing and Blooming, then fruit for four stages' time
        patches.MinutesPass(3 * CheriStage);
        Assert.Equal((BerryStage.Fruit, 4 * CheriStage), (patches[0].Stage, patches[0].StageMinutes));
    }

    [Theory]
    [InlineData(Mulch.None, 180)]
    [InlineData(Mulch.Growth, 135)] // CalcMinutesRemainingInStage: three quarters
    [InlineData(Mulch.Damp, 270)]   // half as long again
    [InlineData(Mulch.Stable, 180)]
    [InlineData(Mulch.Gooey, 180)]
    public void MulchChangesHowLongAStageLasts(Mulch mulch, int minutes) =>
        Assert.Equal(minutes, BerryPatches.StageMinutesOf("Cheri Berry", mulch));

    [Theory]
    [InlineData(Mulch.None, 15)]
    [InlineData(Mulch.Damp, 7)]    // CalcMoistureDrainRate: halved, rounded down
    [InlineData(Mulch.Growth, 22)] // half as much again
    public void MulchChangesHowFastTheSoilDries(Mulch mulch, int rate) =>
        Assert.Equal(rate, BerryPatches.DrainOf("Cheri Berry", mulch));

    [Fact]
    public void StableMulchKeepsTheFruitOnLonger()
    {
        var patches = Planted(mulch: Mulch.Stable);
        patches.MinutesPass(4 * CheriStage);
        // GetHarvestTimeWithMulch: 4 stages, and half as many again
        Assert.Equal((BerryStage.Fruit, 6 * CheriStage), (patches[0].Stage, patches[0].StageMinutes));
    }

    [Fact]
    public void WateredEveryStageThePlantGivesItsBestYield()
    {
        var patches = Planted();
        for (int stage = 0; stage < 4; stage++)
        {
            patches.MinutesPass(CheriStage);
            patches.Water(0);
        }
        // Rated 5 throughout: max(2, base yield 1 × 5)
        Assert.Equal((BerryStage.Fruit, 5, 5), (patches[0].Stage, patches[0].YieldRating, patches[0].Yield));
    }

    [Fact]
    public void EveryHourTheSoilIsDryCostsAPointOfTheRating()
    {
        var patches = Planted();
        // Stage 1: 3 hours at 15 an hour, 100 to 55. Stage 2: to 10. Stage 3: the water lasts (10 + 14) / 15 = 1 hour,
        // the other 2 are dry: rating 3. Stage 4: 3 hours dry, rating 0. Fruit: max(2, 1 × 0) = 2.
        patches.MinutesPass(CheriStage);
        Assert.Equal((55, 5), (patches[0].Moisture, patches[0].YieldRating));
        patches.MinutesPass(CheriStage);
        Assert.Equal((10, 5), (patches[0].Moisture, patches[0].YieldRating));
        patches.MinutesPass(CheriStage);
        Assert.Equal((0, 3), (patches[0].Moisture, patches[0].YieldRating));
        Assert.Equal(SoilMoisture.VeryDry, patches.MoistureOf(0));
        patches.MinutesPass(CheriStage);
        Assert.Equal((BerryStage.Fruit, 0, 2), (patches[0].Stage, patches[0].YieldRating, patches[0].Yield));
    }

    [Fact]
    public void MinutesShortOfAnHourAreKeptForTheNext()
    {
        var patches = Planted();
        patches.MinutesPass(30);
        Assert.Equal(100, patches[0].Moisture);
        patches.MinutesPass(30);
        Assert.Equal(85, patches[0].Moisture);
    }

    [Fact]
    public void FruitLeftOnThePlantFallsAndGrowsAgainUntilItsReplantsRunOut()
    {
        var patches = Planted();
        patches.MinutesPass(4 * CheriStage);
        patches.MinutesPass(4 * CheriStage);
        Assert.Equal((BerryStage.Sprouted, 1, 0, 5), (patches[0].Stage, patches[0].Replants, patches[0].Yield, patches[0].YieldRating));
        Assert.Equal("Cheri Berry", patches[0].Berry);

        // 1 + (3 + 4) × 10 stages is all a patch can live: longer leaves the soil bare
        var old = Planted();
        old.MinutesPass(71 * CheriStage);
        Assert.Equal((BerryStage.None, null), (old[0].Stage, old[0].Berry));
    }

    [Fact]
    public void PickingLeavesBareSoilWithNoMulch()
    {
        var patches = Planted(mulch: Mulch.Growth);
        patches.MinutesPass(4 * 135);
        var (berry, count) = patches.Pick(0);
        Assert.Equal(("Cheri Berry", 2), (berry, count)); // dried out under Growth Mulch: rated 0, so 2
        Assert.Equal((BerryStage.None, Mulch.None, (string?)null), (patches[0].Stage, patches[0].Mulch, patches[0].Berry));
    }

    [Fact]
    public void TheClockCountsWholeMinutesAndNeverBackwards()
    {
        var patches = Planted();
        var start = new DateTime(2026, 10, 10, 12, 0, 0);
        patches.ClockTo(start);
        Assert.Equal(CheriStage, patches[0].StageMinutes);
        patches.ClockTo(start.AddMinutes(90.5));
        Assert.Equal(CheriStage - 90, patches[0].StageMinutes);
        Assert.Equal(start.AddMinutes(90), patches.Clock);
        patches.ClockTo(start.AddHours(-5));
        Assert.Equal(CheriStage - 90, patches[0].StageMinutes);
    }

    // ---------------------------------------------------------------- a new game

    [Fact]
    public void ANewGameFindsEveryPatchOfTheTableInFruitAndWaitingToBeSeen()
    {
        var patches = BerryPatches.NewGame();
        Assert.Equal(118, BerryPatches.NewGameBerries.Length);
        for (int i = 0; i < BerryPatches.NewGameBerries.Length; i++)
        {
            var (berry, yield) = BerryPatches.NewGameBerries[i];
            Assert.Equal((berry, BerryStage.Fruit, yield, 3, false), (patches[i].Berry, patches[i].Stage, patches[i].Yield, patches[i].YieldRating, patches[i].Growing));
            Assert.NotNull(ItemDatabase.Get(berry)?.Berry);
        }
        Assert.All(patches.Patches.Skip(118), p => Assert.Equal(BerryStage.None, p.Stage));

        // Unseen, nothing grows; once seen, the fruit falls after its four stages (an Oran Berry's are 4 hours)
        patches.MinutesPass(10_000);
        Assert.Equal(BerryStage.Fruit, patches[0].Stage);
        patches.Seen(0);
        patches.MinutesPass(4 * 4 * 60);
        Assert.Equal(BerryStage.Sprouted, patches[0].Stage);
    }

    [Fact]
    public void EveryBerryThatGrowsHasItsNumbersAndAColour()
    {
        var berries = ItemDatabase.GetAll().Where(i => i.Berry != null).ToList();
        Assert.True(berries.Count >= 64, $"{berries.Count} berries grow");
        Assert.All(berries, b => Assert.True(b.Berry!.StageDuration > 0 && b.Berry.BaseYield > 0, b.Name));
        Assert.Equal(BerryArt.ColourOf("Cheri Berry"), BerryArt.ColourOf("Figy Berry")); // both spicy
        Assert.NotEqual(BerryArt.ColourOf("Cheri Berry"), BerryArt.ColourOf("Chesto Berry"));
        foreach (var stage in Enum.GetValues<BerryStage>())
            Assert.Equal((BerryArt.Width, BerryArt.Height), (BerryArt.Paint(stage, "Oran Berry", true).Width, BerryArt.Paint(stage, "Oran Berry", true).Height));
    }

    [Fact]
    public void TheWorldPlacesSoilForThePatchesOfItsOpenAreas()
    {
        var world = World.LoadAll().Single(w => w.Index.Region == "Sinnoh");
        var soil = world.BuildMaps().SelectMany(m => m.Everyone).Where(n => n.IsBerrySoil).ToList();
        Assert.True(soil.Count >= 80, $"{soil.Count} patches of soil");
        Assert.All(soil, s => Assert.InRange(s.BerryPatch!.Value, 0, BerryPatches.NewGameBerries.Length - 1));
        Assert.Equal(soil.Count, soil.Select(s => s.BerryPatch).Distinct().Count());
        Assert.All(soil, s => Assert.Equal(FieldScripts.BerryPatch, FieldScripts.For(s)));

        var map = world.BuildMaps().First(m => m.Everyone.Any(n => n.IsBerrySoil));
        GameEngine.ShowBerries(map, BerryPatches.NewGame());
        Assert.All(map.NPCs.Where(n => n.IsBerrySoil), s => Assert.Equal(BerryStage.Fruit, s.BerryLook.Stage));
    }

    // ---------------------------------------------------------------- the script

    private static HeadlessScriptHost Talk(HeadlessScriptHost host, Direction facing = Direction.Up)
    {
        host.PlayerFacing = facing;
        var soil = new NPC { Name = "Soft soil", NpcType = NPC.BerrySoilType, BerryPatch = 0 };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.BerryPatch)!, soil);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    [Fact]
    public void FruitIsPickedIntoTheBag()
    {
        var host = Talk(new HeadlessScriptHost { Berries = BerryPatches.NewGame() });
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Oran Berry")!));
        Assert.Contains("picked 1 × Oran Berry", Said(host));
        Assert.Equal(BerryStage.None, host.Berries[0].Stage);
    }

    [Fact]
    public void FruitCanBeLeftOnThePlant()
    {
        var host = new HeadlessScriptHost { Berries = BerryPatches.NewGame() };
        host.Answers.Enqueue(1);
        Talk(host);
        Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get("Oran Berry")!));
        Assert.Equal(BerryStage.Fruit, host.Berries[0].Stage);
    }

    [Fact]
    public void BareSoilTakesMulchAndThenABerryChosenFromTheBag()
    {
        var host = new HeadlessScriptHost { Berries = new BerryPatches() };
        host.Bag.AddItem(ItemDatabase.Get("Damp Mulch")!, 2);
        host.Bag.AddItem(ItemDatabase.Get("Pecha Berry")!, 3);
        host.Answers.Enqueue(0); // spread mulch
        host.Answers.Enqueue(0); // plant a berry: yes
        host.ItemChoice = null;  // the first that fits each time
        Talk(host);

        Assert.Equal((BerryStage.Planted, "Pecha Berry", Mulch.Damp), (host.Berries[0].Stage, host.Berries[0].Berry, host.Berries[0].Mulch));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Damp Mulch")!));
        Assert.Equal(2, host.Bag.GetQuantity(ItemDatabase.Get("Pecha Berry")!));
        Assert.Contains("open ChooseItem mulch Damp Mulch", host.Log);
        Assert.Contains("open ChooseItem berries Pecha Berry", host.Log);
        Assert.Contains("pressed one Pecha Berry", Said(host));
    }

    [Fact]
    public void SoilIsOnlyWorkedFacingItFromBelow()
    {
        var host = new HeadlessScriptHost { Berries = new BerryPatches() };
        host.Bag.AddItem(ItemDatabase.Get("Pecha Berry")!, 1);
        Talk(host, Direction.Left);
        Assert.Empty(host.Asked);
        Assert.Equal(BerryStage.None, host.Berries[0].Stage);
    }

    [Fact]
    public void AGrowingPlantIsWateredWithTheSprayduck()
    {
        var patches = Planted();
        patches.MinutesPass(CheriStage + 60);
        Assert.Equal(40, patches[0].Moisture);

        var without = Talk(new HeadlessScriptHost { Berries = patches });
        Assert.Empty(without.Asked);
        Assert.Contains("little Cheri Berry plant", Said(without));

        var host = new HeadlessScriptHost { Berries = patches };
        host.Bag.AddItem(ItemDatabase.Get("Sprayduck")!, 1);
        Talk(host);
        Assert.Equal(100, patches[0].Moisture);
    }

    [Fact]
    public void ABackedOutChoiceLeavesTheSoilAsItWas()
    {
        var host = new HeadlessScriptHost { Berries = new BerryPatches(), ItemChoice = "" };
        host.Bag.AddItem(ItemDatabase.Get("Pecha Berry")!, 1);
        Talk(host);
        Assert.Equal(BerryStage.None, host.Berries[0].Stage);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Pecha Berry")!));
    }
}
