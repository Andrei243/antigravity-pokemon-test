using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 06 · R14a: the day's events, berries and the lottery. Each number is the original's, worked out in a comment
/// from its code: <c>src/unk_020559DC.c</c> (the days and the minutes), <c>src/berry_patches.c</c> and
/// <c>berry_patch_manager.c</c> (the patches), <c>include/data/berry_init.h</c> (what a new game finds),
/// <c>src/scrcmd_jubilife_lottery.c</c> and <c>system_vars.c</c> (the lottery), <c>src/script_manager.c</c>
/// (the hidden items that come back) and the berry table in each berry's item file.
/// </summary>
public class DailyEventsTests
{
    /// <summary>A generator that gives the numbers it was handed, in turn (each kept under the bound asked), then <paramref name="then"/>.</summary>
    private sealed class Rolls(int then, params int[] rolls) : Random
    {
        private readonly Queue<int> left = new(rolls);
        public override int Next(int maxValue) => Math.Min(left.Count > 0 ? left.Dequeue() : then, Math.Max(0, maxValue - 1));
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
        public override int Next() => Next(int.MaxValue);
        public override long NextInt64(long maxValue) => Next((int)Math.Min(int.MaxValue, maxValue));
    }

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    /// <summary>Patches with nothing in them, so a test plants what it means to.</summary>
    private static BerryPatches Empty()
    {
        var patches = new BerryPatches();
        foreach (var p in patches.Patches) p.Clear();
        return patches;
    }

    // ------------------------------------------------------------------ the berry table

    [Fact]
    public void EveryBerryOfPlatinumsCanBePlantedWithItsTablesNumbers()
    {
        var berries = ItemDatabase.GetAll().Where(i => i.Pocket == ItemPocket.Berries && i.Id is > 0 and < 1000).ToList();
        Assert.Equal(64, berries.Count);
        Assert.All(berries, b => Assert.True(BerryPatches.CanPlant(b), b.Name));
        // cheri_berry.json: size 20, soft, a yield of 1, stages of 3 hours, 15 moisture an hour, spicy 10, smooth 25
        var cheri = Item("Cheri Berry").Berry!;
        Assert.Equal((20, 2, 1, 3, 15, 10, 0, 25), (cheri.Size, cheri.Firmness, cheri.Yield, cheri.StageHours, cheri.Drain, cheri.Spicy, cheri.Sweet, cheri.Smoothness));
        // enigma_berry.json: a day a stage, 7 an hour
        Assert.Equal((24, 7), (Item("Enigma Berry").Berry!.StageHours, Item("Enigma Berry").Berry!.Drain));
        Assert.False(BerryPatches.CanPlant(Item("Potion")));
    }

    // ------------------------------------------------------------------ what a new game finds

    [Fact]
    public void ANewGameFindsTheOriginalsFirstCropWaitingToBeSeen()
    {
        var patches = BerryPatches.NewGame();
        // sBerryInitTable: patch 0 an Oran Berry with one berry, patch 6 two Razz Berries; each in fruit for four of
        // its stages (Oran's 4 hours: 4 × 60 × 4 = 960 minutes), its soil moist, its yield rating 3, not growing
        var oran = patches[0]!;
        Assert.Equal(("Oran Berry", BerryStage.Fruit, 960, 1, 100, 3, false), (oran.Berry, oran.Stage, oran.StageMinutes, oran.Yield, oran.Moisture, oran.YieldRating, oran.Growing));
        Assert.Equal(("Razz Berry", 2), (patches[6]!.Berry, patches[6]!.Yield));
        Assert.Equal(119, BerryPatches.FirstCrop.Length);
        // The original's loop reads past its 119 rows; here those patches start empty
        Assert.All(Enumerable.Range(119, 9), i => Assert.Equal(BerryStage.None, patches.StageOf(i)));

        // Unseen, the minutes pass it by; seen, it grows
        patches.MinutesPass(10_000);
        Assert.Equal((BerryStage.Fruit, 960), (oran.Stage, oran.StageMinutes));
        patches.See(0);
        patches.MinutesPass(60);
        Assert.Equal(900, oran.StageMinutes);
    }

    // ------------------------------------------------------------------ growing

    [Fact]
    public void APlantingStartsWetAndAtItsBestRating()
    {
        var patches = Empty();
        patches.Plant(3, "Cheri Berry");
        // BerryPatches_PlantInPatch: 3 hours × 60 = 180 minutes in its first stage, moisture 100, rating 5, growing
        var p = patches[3]!;
        Assert.Equal((BerryStage.Planted, 180, 100, 5, 0, true), (p.Stage, p.StageMinutes, p.Moisture, p.YieldRating, p.Replants, p.Growing));
        Assert.True(patches.HasBerry(3));
        Assert.False(patches.IsEmpty(3));
    }

    [Fact]
    public void APlantWateredEveryStageBearsItsWholeCrop()
    {
        var patches = Empty();
        patches.Plant(0, "Cheri Berry");
        // Watered before each of its four growing stages, it never dries out: in fruit its yield is the berry's 1 ×
        // the rating's 5 (AdvancePatchGrowth), and it stays in fruit for four stages of 180 minutes
        for (int stage = 0; stage < 4; stage++)
        {
            patches.Water(0);
            patches.MinutesPass(180);
        }
        var p = patches[0]!;
        Assert.Equal((BerryStage.Fruit, 5, 5, 720), (p.Stage, p.Yield, p.YieldRating, p.StageMinutes));
    }

    [Fact]
    public void ADryingPlantLosesARatingPointForEachHourBoneDry()
    {
        var patches = Empty();
        patches.Plant(0, "Cheri Berry");
        // 720 minutes at once (DrainPatchMoisture per stage, 15 an hour): planted → sprouted, 100 − 45 = 55; → growing,
        // 55 − 45 = 10; → blooming: 10 lasts one hour of three ((10 + 14) / 15), so two hours dry cost 2 points (3
        // left); → fruit: three hours dry cost the last 3. In fruit the crop is 1 × 0 = 0, raised to the least, 2.
        patches.MinutesPass(720);
        var p = patches[0]!;
        Assert.Equal((BerryStage.Fruit, 0, 0, 2, 720), (p.Stage, p.Moisture, p.YieldRating, p.Yield, p.StageMinutes));
        Assert.Equal(SoilMoisture.VeryDry, patches.MoistureOf(0));
    }

    [Fact]
    public void TheMinutesShortOfAnHourAreKeptTowardTheNext()
    {
        var patches = Empty();
        patches.Plant(0, "Cheri Berry");
        // moistureMinutesRemaining: 30 and 30 make the first hour, and 15 is drunk then
        patches.MinutesPass(30);
        Assert.Equal((100, 30), (patches[0]!.Moisture, patches[0]!.MoistureMinutes));
        patches.MinutesPass(30);
        Assert.Equal((85, 0), (patches[0]!.Moisture, patches[0]!.MoistureMinutes));
        // 85 is moist; 50 and below dry; nothing very dry
        Assert.Equal(SoilMoisture.Moist, patches.MoistureOf(0));
        patches[0]!.Moisture = 50;
        Assert.Equal(SoilMoisture.Dry, patches.MoistureOf(0));
    }

    [Fact]
    public void MulchChangesTheStagesTheDrainAndTheCrop()
    {
        // CalcMinutesRemainingInStage: Growth Mulch three quarters (180 × 3 / 4 = 135), Damp half as long again (270)
        Assert.Equal(135, BerryPatches.StageMinutesOf("Cheri Berry", Mulch.Growth));
        Assert.Equal(270, BerryPatches.StageMinutesOf("Cheri Berry", Mulch.Damp));
        // CalcMoistureDrainRate: Damp half (15 / 2 = 7), Growth half as much again (15 + 7 = 22)
        Assert.Equal(7, BerryPatches.DrainOf("Cheri Berry", Mulch.Damp));
        Assert.Equal(22, BerryPatches.DrainOf("Cheri Berry", Mulch.Growth));
        // Stable: six stages in fruit; Gooey: fifteen seedings; the whole life 1 + (3 + 4) × 10 = 71 stages, with
        // Stable 1 + (3 + 6) × 10 = 91, with Gooey 1 + 7 × 15 = 106
        Assert.Equal((4, 6, 10, 15), (BerryPatches.HarvestStagesOf(Mulch.None), BerryPatches.HarvestStagesOf(Mulch.Stable), BerryPatches.ReplantsOf(Mulch.None), BerryPatches.ReplantsOf(Mulch.Gooey)));
        Assert.Equal((71, 91, 106), (BerryPatches.LifetimeStages(Mulch.None), BerryPatches.LifetimeStages(Mulch.Stable), BerryPatches.LifetimeStages(Mulch.Gooey)));

        var patches = Empty();
        Assert.True(patches.CanMulch(1));
        patches.LayMulch(1, Mulch.Growth);
        Assert.False(patches.CanMulch(1));
        Assert.True(patches.IsEmpty(1));
        patches.Plant(1, "Cheri Berry");
        Assert.Equal(135, patches[1]!.StageMinutes);
        // Picking empties the patch, mulch and all (ZeroBerryPatch)
        patches.MinutesPass(135 * 4);
        Assert.Equal(BerryStage.Fruit, patches.StageOf(1));
        Assert.Equal(("Cheri Berry", 2), patches.Pick(1));
        Assert.Equal((BerryStage.None, Mulch.None), (patches.StageOf(1), patches[1]!.Mulch));
        Assert.Equal((null, 0), patches.Pick(1));
    }

    [Fact]
    public void APlantSeedsItselfAgainTenTimesAndThenIsGone()
    {
        var patches = Empty();
        patches.Plant(0, "Cheri Berry");
        var p = patches[0]!;
        // Its four stages, then its four in fruit: out of fruit it drops its berries and sprouts again, rating whole
        patches.MinutesPass(180 * 4);
        Assert.Equal(BerryStage.Fruit, p.Stage);
        patches.MinutesPass(180 * 4);
        Assert.Equal((BerryStage.Sprouted, 0, 5, 1), (p.Stage, p.Yield, p.YieldRating, p.Replants));
        // Each seeding is three stages to grow and four in fruit; the tenth leaves nothing
        for (int i = 1; i < 10; i++) patches.MinutesPass(180 * 7);
        Assert.Equal(BerryStage.None, p.Stage);
        Assert.Null(p.Berry);

        // A whole life's minutes at once take it in one go (BerryPatches_ElapseMinutes' first test): 180 × 71
        patches.Plant(0, "Cheri Berry");
        patches.MinutesPass(180 * 71);
        Assert.Equal(BerryStage.None, p.Stage);
    }

    [Fact]
    public void PatchesSavedComeBackTheSame()
    {
        var patches = BerryPatches.NewGame();
        patches.Plant(119, "Sitrus Berry");
        patches.LayMulch(118, Mulch.Damp);
        var save = new SaveData { Berries = patches };
        var loaded = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!.Berries!;
        Assert.Equal(BerryPatches.Count, loaded.Patches.Count);
        Assert.Equal(("Sitrus Berry", BerryStage.Planted, 480), (loaded[119]!.Berry, loaded[119]!.Stage, loaded[119]!.StageMinutes));
        Assert.Equal(Mulch.Damp, loaded[118]!.Mulch);
        Assert.Equal(("Oran Berry", BerryStage.Fruit), (loaded[0]!.Berry, loaded[0]!.Stage));
        // A save from before has none, and the game gives it a new game's
        Assert.Null(JsonSerializer.Deserialize<SaveData>("{}")!.Berries);
    }

    // ------------------------------------------------------------------ the berry plants' look

    [Fact]
    public void APlantIsColouredByItsStrongestFlavour()
    {
        Assert.Equal(BerryArt.Flavour.Spicy, BerryArt.FlavourOf(Item("Cheri Berry").Berry));
        Assert.Equal(BerryArt.Flavour.Sweet, BerryArt.FlavourOf(Item("Pecha Berry").Berry));
        Assert.Equal(BerryArt.Flavour.Bitter, BerryArt.FlavourOf(Item("Rawst Berry").Berry));
        // Level flavours go to the first in the contests' order: Sitrus is dry, sweet, bitter and sour alike
        Assert.Equal(BerryArt.Flavour.Dry, BerryArt.FlavourOf(Item("Sitrus Berry").Berry));
        // Nothing growing has no card; every stage has one of the card's size, its fruit in the flavour's colour
        Assert.Null(BerryArt.Paint(BerryStage.None, BerryArt.Flavour.Spicy, SoilMoisture.Moist));
        foreach (var stage in Enum.GetValues<BerryStage>().Skip(1))
            foreach (var moisture in Enum.GetValues<SoilMoisture>())
            {
                var art = BerryArt.Paint(stage, BerryArt.Flavour.Sour, moisture)!;
                Assert.Equal((BerryArt.Width, BerryArt.Height), (art.Width, art.Height));
                // The soil at its foot is as wet as the patch
                var soil = BerryArt.SoilOf(moisture).Base;
                Assert.Contains(Enumerable.Range(0, art.Width).Select(x => art.Get(x, BerryArt.Height - 2)), c => c.R == soil.R && c.G == soil.G && c.B == soil.B);
            }
        var fruit = BerryArt.Paint(BerryStage.Fruit, BerryArt.Flavour.Dry, SoilMoisture.Moist)!;
        var blue = BerryArt.ColoursOf(BerryArt.Flavour.Dry).Base;
        Assert.Contains(Enumerable.Range(0, fruit.Width * fruit.Height).Select(i => fruit.Get(i % fruit.Width, i / fruit.Width)), c => c.R == blue.R && c.G == blue.G && c.B == blue.B);
    }

    // ------------------------------------------------------------------ the lottery

    [Theory]
    // CheckTrainerIdForMatch: from the last digit up, stopping at the first that differs
    [InlineData(12345, 12345, 5)]
    [InlineData(12345, 33345, 3)]
    [InlineData(12345, 12346, 0)]
    // 10005 against 00005: the noughts match too, up to the 1
    [InlineData(10005, 5, 4)]
    // Short numbers match their leading noughts too: 00042 against 00042
    [InlineData(42, 42, 5)]
    // Only the low sixteen bits of an ID are read
    [InlineData(1, 65537, 5)]
    public void ALotteryNumberMatchesFromTheLastDigitUp(int ticket, int id, int digits) =>
        Assert.Equal(digits, Lottery.Matching(ticket, id));

    [Fact]
    public void TheDaysNumberIsTheHighHalfOfTheGroupsNumberTakenOnce()
    {
        // SystemVars_SynchronizeJubilifeLotteryTrainerID: 0 × 1,103,515,245 + 12,345 = 12,345, whose high half is 0;
        // 1 × 1,103,515,245 + 12,345 = 1,103,527,590, whose high half is 1,103,527,590 / 65,536 = 16,838
        Assert.Equal(0, Lottery.NumberOf(0));
        Assert.Equal(16838, Lottery.NumberOf(1));
        // Wrapped at 32 bits: 0xFFFFFFFF × 1,103,515,245 + 12,345 = −1,103,515,245 + 12,345 = 3,191,464,396 (mod 2³²),
        // whose high half is 48,697
        Assert.Equal(48697, Lottery.NumberOf(0xFFFFFFFFu));
        // RandomizeJubilifeLottery keeps the second of its two draws (both halves written to the low variable)
        Assert.Equal(777, Lottery.Drawn(new Rolls(0, 123, 777)));
    }

    [Fact]
    public void TheBestMatchIsTheTeamsUnlessThePCHasMoreDigits()
    {
        Pokemon Traded(int id) => new(PokemonDatabase.Get("Abra")!, 10) { OriginalTrainer = new TrainerMark("Kazza", id, PlayerLook.Boy) };
        var own = new Pokemon(PokemonDatabase.Get("Turtwig")!, 10);
        var twoDigits = Traded(11145);
        var threeDigits = Traded(33345);

        // The player's own Pokémon carry the player's number: 54321 matches nothing of 12345, 12345 all of it
        var none =Lottery.Check(12345, new[] { own }, Array.Empty<Pokemon>(), playerId: 54321);
        Assert.Equal(0, none.Digits);
        Assert.Equal(5, Lottery.Check(12345, new[] { own }, Array.Empty<Pokemon>(), playerId: 12345).Digits);

        // The PC's three beat the team's two; level, the team's wins
        var boxed = Lottery.Check(12345, new[] { own, twoDigits }, new[] { threeDigits }, playerId: 54321);
        Assert.Equal((3, true), (boxed.Digits, boxed.InBox));
        Assert.Same(threeDigits, boxed.Pokemon);
        var level = Lottery.Check(12345, new[] { threeDigits }, new[] { Traded(43345) }, playerId: 54321);
        Assert.Equal((3, false), (level.Digits, level.InBox));
        // The first of the team with the most
        var first = Traded(10045);
        Assert.Same(first, Lottery.Check(12345, new[] { first, Traded(20045) }, Array.Empty<Pokemon>(), 0).Pokemon);

        // The corner's prizes, by the digits matched
        Assert.Equal(new[] { null, "Ultra Ball", "PP Up", "Exp. Share", "Max Revive", "Master Ball" }, Enumerable.Range(0, 6).Select(Lottery.PrizeFor));
    }

    // ------------------------------------------------------------------ the day's events

    [Fact]
    public void ANewDayRunsTheDaysEventsInTheOriginalsOrder()
    {
        var story = new StoryState();
        story.Set("FLAG_DAILY_CHECKED_LUCKY_NUMBER");
        story.Set("FLAG_SOMETHING_ELSE");
        story.SetVar(DailyEvents.NewsPressDeadlineVar, 5);
        foreach (var (_, flag) in DailyEvents.IronIslandStarPieces) story.Set(flag);
        foreach (string flag in DailyEvents.FloaromaMeadowHoney) story.Set(flag);
        var encounters = new SpecialEncounters { DailyNumber = 1 };
        var party = new Party();
        var carrier = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5) { Pokerus = 0x13 };
        party.Add(carrier);

        // Two days: the draws are the level of the day (40 → 42), then Iron Island's two Star Pieces (1 and 1) and
        // Floaroma Meadow's two Honeys (0 and 5)
        DailyEvents.DaysPass(new DailyEvents.Day(story, encounters, party, "route_205_south", new Rolls(0, 40, 1, 1, 0, 5)), 2);

        Assert.False(story.Has("FLAG_DAILY_CHECKED_LUCKY_NUMBER"));
        Assert.True(story.Has("FLAG_SOMETHING_ELSE"));
        // RecordMixedRNG_AdvanceEntries twice (× 1,812,433,253 + 1), and the dailies from it
        uint daily = SpecialEncounters.Next(SpecialEncounters.Next(1));
        Assert.Equal((daily, daily, daily), (encounters.DailyNumber, encounters.MarshDaily, encounters.SwarmDaily));
        Assert.Equal(0x11, carrier.Pokerus);
        Assert.Equal(3, story.Var(DailyEvents.NewsPressDeadlineVar));
        Assert.Equal(Lottery.NumberOf(daily), story.Var(Lottery.NumberVar));
        Assert.Equal(42, story.Var(DailyEvents.DailyLevelVar));
        // The Star Piece drawn twice comes back once; the rest stay found
        Assert.Equal(new[] { true, false, true, true }, DailyEvents.IronIslandStarPieces.Select(s => story.Has(s.Flag)));
        Assert.Equal(new[] { false, true, true, true, true, false }, DailyEvents.FloaromaMeadowHoney.Select(story.Has));

        // The deadline doesn't go below nought; nothing comes back in a room the player stands in, and the meadow's
        // Honey isn't drawn while the player is in the meadow
        foreach (string flag in DailyEvents.FloaromaMeadowHoney) story.Set(flag);
        story.Set(DailyEvents.IronIslandStarPieces[1].Flag);
        DailyEvents.DaysPass(new DailyEvents.Day(story, encounters, party, DailyEvents.FloaromaMeadow, new Rolls(1, 0)), 7);
        Assert.Equal(0, story.Var(DailyEvents.NewsPressDeadlineVar));
        Assert.True(DailyEvents.FloaromaMeadowHoney.All(story.Has));
        Assert.False(story.Has(DailyEvents.IronIslandStarPieces[1].Flag));
        story.Set(DailyEvents.IronIslandStarPieces[1].Flag);
        DailyEvents.DaysPass(new DailyEvents.Day(story, encounters, party, "iron_island_b2f_right_room", new Rolls(1, 0)), 1);
        Assert.True(story.Has(DailyEvents.IronIslandStarPieces[1].Flag));

        // No day, no events
        story.Set("FLAG_DAILY_CHECKED_LUCKY_NUMBER");
        DailyEvents.DaysPass(new DailyEvents.Day(story, encounters, party, null, new Rolls(0)), 0);
        Assert.True(story.Has("FLAG_DAILY_CHECKED_LUCKY_NUMBER"));
    }

    [Fact]
    public void ANewGameDrawsTheLotterysNumberAndTheLevelOfTheDay()
    {
        var story = new StoryState();
        // scripts_init_new_game.s: RandomizeJubilifeLottery (the second of two draws), then InitDailyRandomLevel (% 98 + 2)
        DailyEvents.NewGame(story, new Rolls(0, 5, 31337, 97));
        Assert.Equal((31337, 99), (story.Var(Lottery.NumberVar), story.Var(DailyEvents.DailyLevelVar)));
    }

    [Fact]
    public void TheMinutesGrowTheBerriesAndRunDownTheHoney()
    {
        var berries = Empty();
        berries.Plant(0, "Cheri Berry");
        var encounters = new SpecialEncounters();
        encounters.Trees[0].MinutesLeft = 1440;
        // The clock's first reading counts nothing; an hour later the berries and the honey count 60
        Assert.Equal(0, encounters.ClockTo(new DateTime(2026, 6, 1, 12, 0, 0)));
        int minutes = encounters.ClockTo(new DateTime(2026, 6, 1, 13, 0, 30));
        Assert.Equal(60, minutes);
        DailyEvents.MinutesPass(berries, encounters, minutes);
        Assert.Equal((120, 85), (berries[0]!.StageMinutes, berries[0]!.Moisture));
        Assert.Equal(1380, encounters.Trees[0].MinutesLeft);
        // A clock turned back counts nothing
        Assert.Equal(0, encounters.ClockTo(new DateTime(2026, 5, 1)));
    }

    [Fact]
    public void ASaveFromBeforeTheDaysEventsIsGivenTheirNumbers()
    {
        var story = new StoryState();
        StoryMigration.Upgrade(story, 6, Array.Empty<Pokemon>(), ScriptLibrary.Default, chance: new Rolls(0, 1, 4242, 10));
        Assert.Equal((4242, 12), (story.Var(Lottery.NumberVar), story.Var(DailyEvents.DailyLevelVar)));
        Assert.True(story.Has(StoryMigration.FridayDrifloonFlag));
        // A save of today's version is left as it is
        var today = new StoryState();
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), ScriptLibrary.Default, chance: new Rolls(0, 1, 4242, 10));
        Assert.Equal(0, today.Var(Lottery.NumberVar));
    }

    // ------------------------------------------------------------------ the scripts

    private static NPC Soil(int patch) => new() { Name = "Soft soil", NpcType = NPC.BerryPatchType, Patch = patch, GridX = 3, GridY = 3 };

    private static (HeadlessScriptHost Host, ScriptRunner Runner) Run(string script, NPC? subject, Action<HeadlessScriptHost> setUp, (string, int)? item = null)
    {
        var host = new HeadlessScriptHost { Berries = Empty(), PlayerTile = (3, 4), PlayerFacing = Direction.Up };
        setUp(host);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script)!, subject, item: item);
        runner.RunToEnd();
        return (host, runner);
    }

    [Fact]
    public void SoftSoilIsPlantedWithABerryChosenFromTheBag()
    {
        var (host, _) = Run(FieldScripts.BerryPatch, Soil(5), h =>
        {
            h.Bag.AddItem(Item("Cheri Berry"), 3);
            h.ItemChoice = "Cheri Berry";
        });
        // "Plant a Berry here?" yes, the bag opened on the berries, one Cheri Berry planted
        Assert.Contains(host.Log, l => l == "open ChooseItem berries");
        Assert.Equal((BerryStage.Planted, "Cheri Berry"), (host.Berries.StageOf(5), host.Berries[5]!.Berry));
        Assert.Equal(2, host.Bag.GetQuantity(Item("Cheri Berry")));
        Assert.Contains(host.Transcript, t => t.Text.Contains("planted the Cheri Berry"));

        // Faced from the side, the soil is only told of
        var (side, _) = Run(FieldScripts.BerryPatch, Soil(5), h =>
        {
            h.PlayerFacing = Direction.Left;
            h.Bag.AddItem(Item("Cheri Berry"), 3);
        });
        Assert.Equal(BerryStage.None, side.Berries.StageOf(5));
        Assert.DoesNotContain(side.Log, l => l.StartsWith("open"));
    }

    [Fact]
    public void MulchIsSpreadAndThenABerryPlantedInIt()
    {
        var (host, _) = Run(FieldScripts.BerryPatch, Soil(2), h =>
        {
            h.Bag.AddItem(Item("Damp Mulch"));
            h.Bag.AddItem(Item("Oran Berry"));
            h.Answers.Enqueue(0);           // "Spread mulch"
            h.ItemChoice = "Damp Mulch";
            h.Decide = (question, _) => 0;  // and yes to planting
        });
        // With mulch and berries, the soil's menu; the mulch spread, then a berry asked for (the test's choice is still
        // the mulch, which isn't a berry and plants nothing)
        Assert.Equal(Mulch.Damp, host.Berries[2]!.Mulch);
        Assert.Equal(0, host.Bag.GetQuantity(Item("Damp Mulch")));
        Assert.Equal(BerryStage.None, host.Berries.StageOf(2));

        // Used from the bag on the soil faced: the berry is the script's own
        var (planted, _) = Run(FieldScripts.PlantBerry, Soil(2), h => h.Bag.AddItem(Item("Oran Berry")), ("Oran Berry", 1));
        Assert.Equal(("Oran Berry", 0), (planted.Berries[2]!.Berry, planted.Bag.GetQuantity(Item("Oran Berry"))));
    }

    [Fact]
    public void FruitIsPickedIntoTheBagForAPointOnTheCard()
    {
        var (host, runner) = Run(FieldScripts.BerryPatch, Soil(0), h =>
        {
            h.Berries.Plant(0, "Razz Berry");
            for (int i = 0; i < 4; i++) { h.Berries.Water(0); h.Berries.MinutesPass(120); }
        });
        // Razz: a yield of 2 × a rating of 5 = 10 berries
        Assert.Equal(10, host.Bag.GetQuantity(Item("Razz Berry")));
        Assert.Equal(BerryStage.None, host.Berries.StageOf(0));
        Assert.Equal(TrainerScore.BerryHarvested, host.Score);
        Assert.Contains(host.Transcript, t => t.Text.Contains("picked 10 Razz Berries"));
        Assert.Equal("10 Razz Berries", runner.Fill("{berries}"));
        Assert.Equal("an Oran Berry", ScriptRunner.Berries("Oran Berry", 1));
    }

    [Fact]
    public void TheSprayduckWatersAPlantFacedFromTheSouth()
    {
        var (host, _) = Run(FieldScripts.BerryPatch, Soil(1), h =>
        {
            h.Berries.Plant(1, "Cheri Berry");
            h.Berries[1]!.Moisture = 20;
            h.Bag.AddItem(Item("Sprayduck"));
        });
        Assert.Equal(100, host.Berries[1]!.Moisture);
        // From the bag as well
        var (bag, _) = Run(FieldScripts.UseSprayduck, Soil(1), h =>
        {
            h.Berries.Plant(1, "Cheri Berry");
            h.Berries[1]!.Moisture = 0;
        });
        Assert.Equal(100, bag.Berries[1]!.Moisture);
    }

    [Fact]
    public void TheLotteryGivesItsPrizeOnceADay()
    {
        var clerk = new NPC { Name = "Lottery Clerk", GridX = 3, GridY = 3 };
        var (host, _) = Run(FieldScripts.Lottery, clerk, h =>
        {
            h.Story.SetVar(Lottery.NumberVar, 345);
            h.TrainerNumber = 54321;
            h.Party.Add(new Pokemon(PokemonDatabase.Get("Abra")!, 10) { OriginalTrainer = new TrainerMark("Kazza", 33345, PlayerLook.Boy) });
        });
        // Three digits: the second prize, an Exp. Share, and the day's flags
        Assert.Equal(1, host.Bag.GetQuantity(Item("Exp. Share")));
        Assert.True(host.Story.Has(Lottery.PrizeFlag) && host.Story.Has(Lottery.CheckedFlag));
        Assert.Contains(host.Transcript, t => t.Text.Contains("00345"));

        // The same day again: nothing more
        var again = new HeadlessScriptHost(host.Story, host.Party, host.Bag);
        var runner = new ScriptRunner(ScriptLibrary.Default, again);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Lottery)!, clerk);
        runner.RunToEnd();
        Assert.Equal(1, again.Bag.GetQuantity(Item("Exp. Share")));
        Assert.Empty(again.Asked);
    }

    [Theory]
    [InlineData(DayOfWeek.Friday, 2, false, false)]
    [InlineData(DayOfWeek.Thursday, 2, false, true)]
    [InlineData(DayOfWeek.Friday, 1, false, true)]
    [InlineData(DayOfWeek.Friday, 2, true, true)]
    public void TheWindworksDrifloonComesOnFridays(DayOfWeek day, int windworks, bool beatenToday, bool hidden)
    {
        var library = ScriptLibrary.Default;
        var host = new HeadlessScriptHost { Today = new DateTime(2026, 10, 5).AddDays(((int)day - (int)DayOfWeek.Monday + 7) % 7) };
        Assert.Equal(day, host.Today.DayOfWeek);
        host.Story.SetVar("VAR_VALLEY_WINDWORKS_STATE", windworks);
        if (beatenToday) host.Story.Set("FLAG_DAILY_WON_AGAINST_VALLEY_WINDWORKS_OUTSIDE_DRIFLOON");
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("FridayDrifloon", "valley_windworks_outside")!);
        runner.RunToEnd();
        Assert.Equal(hidden, host.Story.Has(StoryMigration.FridayDrifloonFlag));
    }

    // ------------------------------------------------------------------ the bag

    [Fact]
    public void TheBagPlantsABerryWithSoftSoilAhead()
    {
        var razz = Item("Razz Berry");
        // UseBerryFromMenu: any berry has USE with empty soil ahead; a Razz Berry heals nothing, so none elsewhere
        Assert.DoesNotContain(BagAction.Use, BagScreen.ActionsFor(razz));
        Assert.Contains(BagAction.Use, BagScreen.ActionsFor(razz, soilAhead: true));
        Assert.True(BagScreen.UsedInField(Item("Growth Mulch")) && BagScreen.UsedInField(Item("Sprayduck")));

        var bag = new Inventory();
        bag.AddItem(razz);
        var screen = new BagScreen { SoilAhead = true };
        screen.Open();
        screen.CurrentPocket = ItemPocket.Berries;
        screen.Confirm(bag, new Party(), _ => { });
        screen.Confirm(bag, new Party(), _ => { });
        Assert.False(screen.IsActive);
        Assert.Same(razz, screen.TakeFieldUse());

        // Opened for a script to choose: the item is only chosen
        screen.OpenToChoose(ItemPocket.Berries, "PLANT WHICH BERRY?");
        Assert.True(screen.Choosing);
        screen.MovePocket(1);
        Assert.Equal(ItemPocket.Berries, screen.CurrentPocket);
        screen.Confirm(bag, new Party(), _ => { });
        Assert.Equal((false, razz), (screen.IsActive, screen.Chosen));
        Assert.Equal(1, bag.GetQuantity(razz));
    }
}
