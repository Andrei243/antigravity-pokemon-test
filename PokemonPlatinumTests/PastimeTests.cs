using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Fossils, the Game Corner, the Villa and Pal Park (plan 06 · R16b). The numbers are the decompilation's
/// (<c>overlay101</c>, <c>scrcmd_fossil.c</c>, <c>scrcmd_game_corner_prize.c</c>, <c>villa_furniture.c</c>,
/// <c>system_vars.c</c>, <c>scripts_villa.s</c>, <c>scripts_pal_park_lobby.s</c>).
/// </summary>
[Collection("MapDatabase")]
public class PastimeTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static HeadlessScriptHost Run(string name, string place, HeadlessScriptHost host, Map? map = null, string? who = null)
    {
        if (map != null) host.Map = map;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, place)!, who == null ? null : host.Map!.Everyone.First(n => n.Key == who));
        runner.RunToEnd();
        return host;
    }

    private static bool Said(HeadlessScriptHost host, string part) => host.Transcript.Any(l => l.Text.Contains(part));

    // ------------------------------------------------------------------ the slot machine

    private static void Frames(SlotMachine m, int frames, SlotKeys keys = SlotKeys.None)
    {
        for (int i = 0; i < frames; i++) m.Update(i == 0 ? keys : SlotKeys.None);
    }

    /// <summary>Stops the three reels in an order, a few frames apart, and lets them settle and the machine judge.</summary>
    private static void Stop(SlotMachine m, int[] order, int gap = 5)
    {
        Frames(m, 12);
        foreach (int reel in order)
        {
            Frames(m, 1, reel == 0 ? SlotKeys.Stop1 : reel == 1 ? SlotKeys.Stop2 : SlotKeys.Stop3);
            Frames(m, gap);
        }
        // Slips of up to a whole turn, two frames a symbol, and the settling
        for (int i = 0; i < 80 && !(m.Stopped(0) && m.Stopped(1) && m.Stopped(2)); i++) m.Update(SlotKeys.None);
        Frames(m, 3);
    }

    [Fact]
    public void TheReelsAndThePaysAreTheOriginals()
    {
        Assert.All(SlotMachine.Reels, reel => Assert.Equal(21, reel.Length));
        // Unk_ov101_021D9688: the left reel has two red sevens and a blue, the middle one three red and two blue
        Assert.Equal(new[] { 2, 3, 2 }, SlotMachine.Reels.Select(r => r.Count(s => s == (int)SlotSymbol.RedSeven)).ToArray());
        Assert.Equal(new[] { 3, 2, 3 }, SlotMachine.Reels.Select(r => r.Count(s => s == (int)SlotSymbol.BlueSeven)).ToArray());
        Assert.Equal(new[] { 100, 100, 0, 2, 10, 15 }, SlotMachine.Pays);
        Assert.Equal(15, SlotMachine.BonusPays[(int)SlotSymbol.Replay]);
        Assert.Equal(new[] { 50, 70, 90, 50, 70, 90 }, SlotMachine.Stamina);
    }

    [Fact]
    public void AGameCostsThreeCoinsAndNothingDrawnNeverPays()
    {
        // Whatever the moment each reel is stopped at, a game whose draw came to nothing leaves every line clear
        for (int seed = 0; seed < 60; seed++)
        {
            var m = new SlotMachine(100, 0, new Random(seed));
            Frames(m, 1);
            Assert.True(m.WaitsForBet);
            Frames(m, 1, SlotKeys.Bet);
            Assert.Equal(97, m.Coins);
            m.Rig(SlotMachine.Draw.None);
            Frames(m, seed % 7);
            Stop(m, SlotMachine.Orders[seed % 6], gap: 1 + seed % 9);
            Assert.Equal(SlotSymbol.None, m.Lined);
            Frames(m, 30);
            Assert.Equal(97, m.Coins);
        }
    }

    [Fact]
    public void ADrawnSymbolStoppedLeftToRightLinesUp()
    {
        // The draw marks the Ball and the Replay "wide": stopped in order the reels slip as far as it takes
        foreach (var (draw, symbol, pays) in new[] { (SlotMachine.Draw.Ball, SlotSymbol.Ball, 10), (SlotMachine.Draw.Replay, SlotSymbol.Replay, 0) })
            for (int seed = 0; seed < 40; seed++)
            {
                var m = new SlotMachine(100, 0, new Random(seed));
                m.Rig(draw);
                Frames(m, seed % 5);
                Stop(m, SlotMachine.Orders[0]);
                Assert.Equal(symbol, m.Lined);
                Assert.NotEqual(0, m.Lines);
                // ov101_021D597C: every line pays the symbol's coins
                int lines = System.Numerics.BitOperations.PopCount((uint)m.Lines);
                Frames(m, 400);
                Assert.Equal(100 + pays * lines, m.Coins);
            }
    }

    [Fact]
    public void ACherryOnTheLeftReelPaysByItself()
    {
        // A cherry isn't one of the draws that slip all the way round: within four symbols it is brought into the window,
        // and further off the game comes to nothing
        int cherries = 0;
        for (int seed = 0; seed < 40; seed++)
        {
            var m = new SlotMachine(100, 0, new Random(seed));
            m.Rig(SlotMachine.Draw.Cherry);
            Stop(m, SlotMachine.Orders[0]);
            if (m.Lined == SlotSymbol.None)
            {
                Frames(m, 60);
                Assert.Equal(100, m.Coins);
                continue;
            }
            cherries++;
            Assert.Equal(SlotSymbol.Cherry, m.Lined);
            // In the middle row one line (2 coins), in the top or the bottom row its row and a diagonal (4)
            bool middle = m.Shows(0, 2) == SlotSymbol.Cherry;
            Frames(m, 200);
            Assert.Equal(100 + (middle ? 2 : 4), m.Coins);
        }
        Assert.InRange(cherries, 20, 40);
    }

    [Fact]
    public void AReplayPullsTheLeverAgainForNothing()
    {
        var m = new SlotMachine(100, 0, new Random(3));
        Frames(m, 1);
        Frames(m, 1, SlotKeys.Bet);
        m.Rig(SlotMachine.Draw.Replay);
        Stop(m, SlotMachine.Orders[0]);
        Assert.Equal(SlotSymbol.Replay, m.Lined);
        // The reels turn again without a bet (ov101_021D1F54 goes straight to the pull)
        for (int i = 0; i < 120 && !m.Spinning; i++) m.Update(SlotKeys.None);
        Assert.True(m.Spinning);
        Assert.Equal(97, m.Coins);
    }

    [Fact]
    public void ThreeSevensBeginTheBonusGame()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var m = new SlotMachine(100, 2, new Random(seed));
            m.RigBonus(seed % 6);
            m.Rig(seed % 6 <= 2 ? SlotMachine.Draw.RedSevens : SlotMachine.Draw.BlueSevens);
            Assert.Equal(seed % 6 <= 2 ? SlotSymbol.RedSeven : SlotSymbol.BlueSeven, m.Awaited);
            // Sevens are four symbols apart at most on no reel, so they are lined up left to right or not at all;
            // stopped at once on every seed they come within reach on some
            Stop(m, SlotMachine.Orders[0]);
            if (m.Lined == SlotSymbol.None) continue;
            Assert.Contains(m.Lined, new[] { SlotSymbol.RedSeven, SlotSymbol.BlueSeven });
            int lines = System.Numerics.BitOperations.PopCount((uint)m.Lines);
            for (int i = 0; i < 2000 && !m.InBonus; i++) m.Update(SlotKeys.Held);
            Assert.True(m.InBonus);
            Assert.Equal(100 + 100 * lines, m.Coins);
            Assert.Equal(1, m.Rounds);
            for (int i = 0; i < 10 && !m.WaitsForBet; i++) m.Update(SlotKeys.None);
            Assert.Equal(SlotMachine.BonusGames, m.GamesLeft);
            return;
        }
        Assert.Fail("no seed lined the sevens up");
    }

    [Fact]
    public void InTheBonusGameTheReelsPayOnlyStoppedAsClefairyPoints()
    {
        for (int pointed = 0; pointed < 6; pointed++)
        {
            // As pointed: a replay on the middle line, fifteen coins
            var m = new SlotMachine(100, 0, new Random(pointed));
            m.Rig(SlotMachine.Draw.Replay, bonusGame: true, pointed: pointed);
            Stop(m, SlotMachine.Orders[pointed]);
            Assert.Equal(SlotSymbol.Replay, m.Lined);
            Assert.Equal(1, m.Lines);
            Assert.All(new[] { 0, 1, 2 }, reel => Assert.Equal(SlotSymbol.Replay, m.Shows(reel, 2)));
            Frames(m, 200);
            Assert.Equal(115, m.Coins);

            // Any other order: the middle line stays clear
            var wrong = new SlotMachine(100, 0, new Random(pointed));
            wrong.Rig(SlotMachine.Draw.Replay, bonusGame: true, pointed: pointed);
            Stop(wrong, SlotMachine.Orders[(pointed + 3) % 6]);
            Assert.Equal(SlotSymbol.None, wrong.Lined);
        }
    }

    [Fact]
    public void ABonusRoundIsFifteenGamesAtACoinEach()
    {
        var m = new SlotMachine(500, 0, new Random(11));
        m.RigBonus(2);
        m.Rig(SlotMachine.Draw.Replay, bonusGame: true, pointed: 0);
        int games = 0, before = m.Coins;
        // Play the round out, always as pointed
        for (int guard = 0; guard < 40 && m.InBonus && m.Rounds == 0; guard++)
        {
            if (m.Spinning)
            {
                Stop(m, SlotMachine.Orders[m.PointedOrder]);
                games++;
            }
            for (int i = 0; i < 400 && !m.Spinning && m.InBonus && m.Rounds == 0; i++) m.Update(SlotKeys.Bet | SlotKeys.Confirm);
        }
        Assert.Equal(SlotMachine.BonusGames, games);
        // The first game was rigged without its bet; fourteen bets of one coin, fifteen wins of fifteen
        Assert.True(m.Coins >= before + 15 * 15 - 15);
    }

    [Fact]
    public void TheMachinesMoodsAreDealtAnewEachDay()
    {
        // sub_0203E484: one machine of the best mood and one of the worst among twelve, whatever the day
        foreach (uint day in new uint[] { 0, 1, 77, 123456789 })
        {
            var moods = Enumerable.Range(0, 12).Select(machine => SlotMachine.MoodOf(machine, day)).OrderBy(mood => mood).ToArray();
            Assert.Equal(new[] { 0, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 5 }, moods);
            Assert.Equal(SlotMachine.MoodOf(4, day), SlotMachine.MoodOf(4, day));
        }
        Assert.NotEqual(Enumerable.Range(0, 12).Select(i => SlotMachine.MoodOf(i, 1)), Enumerable.Range(0, 12).Select(i => SlotMachine.MoodOf(i, 2)));
    }

    [Fact]
    public void AMachineSaysWhyItStops()
    {
        var empty = new SlotMachine(0, 0, new Random(1));
        Frames(empty, 2);
        Assert.Equal(SlotNotice.NoCoins, empty.Notice);
        Frames(empty, 2, SlotKeys.Confirm);
        Assert.True(empty.Finished);

        var few = new SlotMachine(2, 0, new Random(1));
        Frames(few, 2);
        Assert.Equal(SlotNotice.TooFewCoins, few.Notice);

        // A full case is told, and the game goes on
        var full = new SlotMachine(CoinCase.Most, 0, new Random(1));
        Frames(full, 2);
        Assert.Equal(SlotNotice.CaseFull, full.Notice);
        Frames(full, 2, SlotKeys.Confirm);
        Assert.True(full.WaitsForBet);

        // Leaving before the lever is pulled gives the bet back
        var leaving = new SlotMachine(10, 0, new Random(1));
        Frames(leaving, 1);
        Frames(leaving, 1, SlotKeys.Bet);
        Frames(leaving, 2, SlotKeys.Quit);
        Assert.True(leaving.Finished);
        Assert.Equal(10, leaving.Coins);
    }

    [Fact]
    public void AMachinePlayedAtRandomPaysBackAsItsDrawsAllow()
    {
        // Thousands of games stopped left to right at whatever moment: the coins never go wrong, and the machine pays
        // out something (about a quarter of the games draw a prize)
        var m = new SlotMachine(20000, 5, new Random(42));
        var hands = new Random(7);
        int won = 0, games = 0;
        for (int frame = 0; frame < 200000 && games < 1500 && !m.Finished; frame++)
        {
            var keys = SlotKeys.None;
            if (m.WaitsForBet || m.WaitsForLever) keys = SlotKeys.Bet;
            else if (m.Spinning && hands.Next(4) == 0)
            {
                int next = m.InBonus ? SlotMachine.Orders[m.PointedOrder][m.Presses] : m.Presses;
                keys = next == 0 ? SlotKeys.Stop1 : next == 1 ? SlotKeys.Stop2 : SlotKeys.Stop3;
                if (m.Presses == 2) games++;
            }
            int before = m.Payout;
            m.Update(keys | SlotKeys.Held);
            if (m.Payout > before) won += m.Payout - before;
            Assert.InRange(m.Coins, 0, CoinCase.Most);
        }
        Assert.True(games >= 1500, $"only {games} games were played");
        Assert.True(won > 0);
    }

    // ------------------------------------------------------------------ the Coin Case and the counters

    [Fact]
    public void TheCoinCaseHoldsFiftyThousand()
    {
        var coins = new CoinCase();
        Assert.True(coins.Add(49990));
        Assert.False(coins.CanAdd(50));
        Assert.True(coins.CanAdd(10));
        // Coins_Add fills a case that isn't full, and adds nothing to one that is
        Assert.True(coins.Add(50));
        Assert.Equal(50000, coins.Coins);
        Assert.False(coins.Add(1));
        Assert.False(coins.Take(50001));
        Assert.True(coins.Take(50000));
        Assert.Equal(0, coins.Coins);
    }

    [Fact]
    public void TheCounterSellsCoinsAndTheExchangeHasPlatinumsPrizes()
    {
        Assert.Equal(19, GameCornerPrizes.All.Length);
        Assert.All(GameCornerPrizes.All, p => Assert.NotNull(ItemDatabase.Get(p.Item)));
        Assert.Equal(("Silk Scarf", 1000), GameCornerPrizes.All[0]);
        Assert.Equal(("TM68", 20000), GameCornerPrizes.All[^1]);
        Assert.Equal(15, GameCornerPrizes.All.Count(p => p.Item.StartsWith("TM")));

        // Fifty Coins for 1,000, with a Coin Case
        var none = Run("CoinsClerk", "VeilstoneGameCorner", new HeadlessScriptHost());
        Assert.True(Said(none, "no Coin Case"));
        var host = new HeadlessScriptHost { Money = 3000 };
        host.Bag.AddItem(ItemDatabase.Get("Coin Case")!);
        host.Answers.Enqueue(0);
        Run("CoinsClerk", "VeilstoneGameCorner", host);
        Assert.Equal(50, host.Pastimes.Coins.Coins);
        Assert.Equal(2000, host.Money);
        // Five hundred are 10,000, which 2,000 doesn't buy
        host.Answers.Enqueue(1);
        Run("CoinsClerk", "VeilstoneGameCorner", host);
        Assert.Equal(50, host.Pastimes.Coins.Coins);
        Assert.Equal(2000, host.Money);

        // Ten bonus rounds in a row earn TM64, once
        host.Story.SetVar(GameCornerPrizes.StreakVariable, 10);
        Run("CoinsClerk", "VeilstoneGameCorner", host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("TM64")!));
        Assert.True(host.Story.Has("FLAG_RECEIVED_GAME_CORNER_TM64"));

        // The exchange: the fifth prize is TM90 for 2,000 Coins
        host.Pastimes.Coins.Set(2500);
        host.Answers.Enqueue(4);
        host.Answers.Enqueue(0);
        Run("Receptionist", "VeilstonePrizeExchange", host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("TM90")!));
        Assert.Equal(500, host.Pastimes.Coins.Coins);
        // And with too few Coins nothing changes hands
        host.Answers.Enqueue(18);
        host.Answers.Enqueue(0);
        Run("Receptionist", "VeilstonePrizeExchange", host);
        Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get("TM68")!));
        Assert.Equal(500, host.Pastimes.Coins.Coins);
    }

    [Fact]
    public void TheHallsPlayersGiveCoinsOnceAndAMachineNeedsACoinCase()
    {
        var host = new HeadlessScriptHost();
        Run("Machine3", "VeilstoneGameCorner", host);
        Assert.DoesNotContain(host.Log, line => line.StartsWith("pastime slots"));
        host.Bag.AddItem(ItemDatabase.Get("Coin Case")!);
        host.SlotsLeave = 480;
        Run("Machine3", "VeilstoneGameCorner", host);
        Assert.Contains("pastime slots 3", host.Log);
        Assert.Equal(480, host.Pastimes.Coins.Coins);

        host.Pastimes.Coins.Set(0);
        Run("OldMan", "VeilstoneGameCorner", host);
        Run("OldMan", "VeilstoneGameCorner", host);
        Run("Farmer", "VeilstoneGameCorner", host);
        Run("Farmer", "VeilstoneGameCorner", host);
        Assert.Equal(70, host.Pastimes.Coins.Coins);

        // The twelve machines of the original's events are the twelve the room's signs start
        var room = Room("VeilstoneGameCorner");
        var machines = room.SignScripts.Values.Where(s => s.StartsWith("Machine")).ToList();
        Assert.Equal(12, machines.Distinct().Count());
        Assert.Equal("Machine0", room.SignScripts[(12, 6)]);
        Assert.Equal("Machine11", room.SignScripts[(12, 9)]);
        Assert.Equal(20, room.Props.Count(p => p.Type == PropType.SlotMachine));
    }

    [Fact]
    public void TheClownGivesTheCoinCaseOnALuckyToss()
    {
        // GetRandom 2: a toss, whatever hand is said
        int won = 0;
        for (int seed = 0; seed < 20; seed++)
        {
            var host = new HeadlessScriptHost { Rng = new Random(seed) };
            host.Answers.Enqueue(seed % 2);
            Run("Clown", "VeilstoneSoutheastHouse", host, Room("VeilstoneSoutheastHouse"), "clown");
            bool has = host.Bag.GetQuantity(ItemDatabase.Get("Coin Case")!) == 1;
            Assert.Equal(has, host.Story.Has("FLAG_RECEIVED_VEILSTONE_CITY_SOUTHEAST_HOUSE_COIN_CASE"));
            if (has) won++;
        }
        Assert.InRange(won, 3, 17);
    }

    // ------------------------------------------------------------------ fossils

    [Fact]
    public void TheSevenFossilsAreTheSevenSpecies()
    {
        Assert.Equal(new[] { "Aerodactyl", "Omanyte", "Kabuto", "Lileep", "Anorith", "Shieldon", "Cranidos" }, Fossils.All.Select(f => f.Species).ToArray());
        Assert.All(Fossils.All, f =>
        {
            Assert.NotNull(ItemDatabase.Get(f.Item));
            Assert.NotNull(PokemonDatabase.Get(f.Species));
        });
    }

    [Fact]
    public void TheMuseumRevivesAFossilOnceThePlayerHasLeftAndComeBack()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 20));
        host.Bag.AddItem(ItemDatabase.Get("Skull Fossil")!, 2);

        // Nothing for whoever has never dug
        Run("FossilResearcher", "OreburghMiningMuseum", host);
        Assert.Equal(2, Fossils.Count(host.Bag));
        host.Story.Set("FLAG_RECEIVED_EXPLORER_KIT");

        // One kind: no question which. The Fossil goes, the species is remembered, and the machine is at work
        host.Answers.Enqueue(0);
        Run("FossilResearcher", "OreburghMiningMuseum", host);
        Assert.Equal(1, Fossils.Count(host.Bag));
        Assert.Equal(PokemonDatabase.Get("Cranidos")!.DexNumber, host.Story.Var(Fossils.RevivedVariable));
        Assert.True(host.Story.Has("FLAG_MAP_LOCAL_MUSEUM_REVIVING"));
        Run("FossilResearcher", "OreburghMiningMuseum", host);
        Assert.Equal(1, host.Party.Count);

        // Out and in again: the local flag is gone, and the Pokémon is handed over at level 20
        host.Story.ClearLocal();
        Run("FossilResearcher", "OreburghMiningMuseum", host);
        Assert.Equal(2, host.Party.Count);
        Assert.Equal("Cranidos", host.Party.Members[1].Species.Name);
        Assert.Equal(20, host.Party.Members[1].Level);
        Assert.Equal(0, host.Story.Var(Fossils.RevivedVariable));
        Assert.Equal(1, host.Pastimes.FossilsRevived);
    }

    [Fact]
    public void WithSeveralKindsTheScientistAsksWhichAndAFullTeamWaits()
    {
        var host = new HeadlessScriptHost();
        host.Story.Set("FLAG_RECEIVED_EXPLORER_KIT");
        for (int i = 0; i < 6; i++) host.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 5));
        host.Bag.AddItem(ItemDatabase.Get("Old Amber")!);
        host.Bag.AddItem(ItemDatabase.Get("Root Fossil")!);
        host.Bag.AddItem(ItemDatabase.Get("Armor Fossil")!);
        // Yes, and the second of the three held (in the table's order: Old Amber, Root, Armor)
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(1);
        Run("FossilResearcher", "OreburghMiningMuseum", host);
        Assert.Contains(host.Asked, q => q.Question.Contains("Which Fossil"));
        Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get("Root Fossil")!));
        Assert.Equal(PokemonDatabase.Get("Lileep")!.DexNumber, host.Story.Var(Fossils.RevivedVariable));

        // A full team is told to make room, and the Pokémon goes on waiting
        host.Story.ClearLocal();
        Run("FossilResearcher", "OreburghMiningMuseum", host);
        Assert.Equal(6, host.Party.Count);
        Assert.NotEqual(0, host.Story.Var(Fossils.RevivedVariable));
        Assert.True(Said(host, "team is full"));
    }

    // ------------------------------------------------------------------ the Villa

    [Fact]
    public void TheCatalogueHasTwentyPiecesInTwentyThreePlaces()
    {
        Assert.Equal(20, VillaRules.Furniture.Count);
        Assert.Equal(23, VillaRules.Furniture.Sum(f => f.Places.Length));
        Assert.Equal(new[] { 0, 120000, 90000, 187000, 58000, 220000, 160000, 150000, 127000, 120000, 168000, 25300, 150000, 150000, 146700, 208000, 52000, 140000, 108000, 120000 },
            VillaRules.Furniture.Select(f => f.Price).ToArray());
        Assert.Equal(4, VillaRules.Find("Houseplant")!.Places.Length);
        Assert.Equal(20, VillaRules.Furniture.Select(f => f.Flag).Distinct().Count());

        // Every piece stands on the room's floor, no two on one tile, and none where a visitor or the door's mat is
        var room = Room(VillaRules.Room);
        var story = new StoryState();
        foreach (var piece in VillaRules.Furniture) story.Set(piece.Flag);
        var blocked = VillaRules.Blocked(story).ToList();
        Assert.Equal(blocked.Count, blocked.Distinct().Count());
        Assert.All(blocked, t => Assert.True(room.IsWalkable(t.X, t.Y), $"furniture at {t} stands in a wall"));
        Assert.All(room.Everyone, who => Assert.DoesNotContain((who.GridX, who.GridY), blocked));
        Assert.DoesNotContain((11, 11), blocked);
        Assert.DoesNotContain((10, 6), blocked);
        // Each piece that stands on the floor has its words
        foreach (var (file, script) in GameEngine.PastimeScripts) Assert.NotNull(ScriptLibrary.Default.Find(script, file));
    }

    [Fact]
    public void EveryPieceOfTheVillasFurnitureIsBuiltOnItsOwnTiles()
    {
        var room = Room("ResortAreaVilla");
        const float slack = 4 / 32f;
        foreach (var piece in VillaRules.Furniture)
        {
            // The chandelier is owned and counted and never drawn: it hangs out of the camera's sight
            if (piece.Name == "Chandelier") continue;
            foreach (var place in piece.Places)
            {
                var prop = new Prop { Type = GameEngine.VillaProp(piece), X = place.X, Y = place.Y, Width = place.Width, Depth = place.Depth };
                Assert.True(prop.Type != PropType.Rug, $"the {piece.Name} has no prop of its own");
                var kit = new PokemonPlatinumEngine.Graphics.KitBuilder(new PokemonPlatinumEngine.Graphics.ArtSheet(), 1.624f);
                PokemonPlatinumEngine.Graphics.PropModels.Build(kit, prop, room);
                Assert.True(kit.Solid.VertexCount + kit.Flat.VertexCount > 0, $"the {piece.Name} builds nothing");
                var (min, max) = (kit.Solid.VertexCount > 0 ? kit.Solid : kit.Flat).Bounds();
                Assert.True(min.X >= place.X - slack && max.X <= place.X + place.Width + slack, $"the {piece.Name} is wider than its place ({min.X} to {max.X})");
                // What stands on the floor keeps to the tiles the original's table blocks for it
                if (place.Blocks)
                    Assert.True(min.Z >= place.Y - slack && max.Z <= place.Y + place.Depth + slack, $"the {piece.Name} is deeper than its place ({min.Z} to {max.Z})");
            }
        }
    }

    [Fact]
    public void NinePiecesAreListedOnlyOnceSomethingHasBeenDone()
    {
        var story = new StoryState();
        var none = new VillaRecords();
        Assert.Equal(11, VillaRules.Catalogue(story, none).Count);
        // The Music Box comes with the Rack, the Tea Set with the Guest Set
        story.Set("FLAG_VILLA_FURNITURE_RACK");
        Assert.Contains(VillaRules.Catalogue(story, none), f => f.Name == "Music Box");
        Assert.DoesNotContain(VillaRules.Catalogue(story, none), f => f.Name == "Tea Set");
        story.Set("FLAG_VILLA_FURNITURE_GUEST_SET");
        Assert.Contains(VillaRules.Catalogue(story, none), f => f.Name == "Tea Set");
        // ScrCmd_CheckMetFurnitureRequirements
        bool Lists(string name, VillaRecords records) => VillaRules.Catalogue(new StoryState(), records).Any(f => f.Name == name);
        Assert.False(Lists("Piano", none with { HallOfFameEntries = 9 }));
        Assert.True(Lists("Piano", none with { HallOfFameEntries = 10 }));
        Assert.False(Lists("Guest Set", none with { BattlegroundBattles = 49 }));
        Assert.True(Lists("Guest Set", none with { BattlegroundBattles = 50 }));
        Assert.True(Lists("Wall Clock", none with { BerriesPlanted = 50 }));
        Assert.True(Lists("Masterpiece", none with { EggsHatched = 30 }));
        Assert.False(Lists("Chandelier", none with { Steps = 299999 }));
        Assert.True(Lists("Chandelier", none with { Steps = 300000 }));
        Assert.True(Lists("Pokémon Bust", none with { FrontierEverywhere = true }));
        Assert.True(Lists("Silver Pokémon Bust", none with { FrontierPrint = true }));
        Assert.Equal(20, VillaRules.Catalogue(story, new VillaRecords(true, true, 10, 50, 50, 30, 300000)).Count);
    }

    private sealed class Rolls : Random
    {
        private readonly Queue<int> rolls;
        public Rolls(params int[] rolls) { this.rolls = new Queue<int>(rolls); }
        public override int Next(int max) => rolls.Dequeue() % max;
    }

    [Fact]
    public void TheMoreFurnitureTheOftenerSomeoneCalls()
    {
        // CalcVillaVisitorIndex: nobody when the roll is over 25, 75 or 90; otherwise one of 4, 12 or 15
        Assert.Equal(VillaRules.NoVisitor, VillaRules.DrawVisitor(0, new Rolls(26)));
        Assert.Equal(3, VillaRules.DrawVisitor(7, new Rolls(25, 7)));
        Assert.Equal(VillaRules.NoVisitor, VillaRules.DrawVisitor(8, new Rolls(76)));
        Assert.Equal(11, VillaRules.DrawVisitor(8, new Rolls(75, 11)));
        Assert.Equal(VillaRules.NoVisitor, VillaRules.DrawVisitor(12, new Rolls(91)));
        Assert.Equal(14, VillaRules.DrawVisitor(20, new Rolls(90, 14)));
        Assert.Equal(15, VillaRules.Visitors.Length);

        // SystemVars_UpdateVillaVisitor: a new day draws afresh, but not while the player is at the Villa
        var story = new StoryState();
        story.Set(VillaRules.InsideFlag);
        story.SetVar(VillaRules.VisitorVariable, 9);
        VillaRules.NewDay(story, VillaRules.Area, new Rolls(0, 2, 4));
        Assert.Equal(9, story.Var(VillaRules.VisitorVariable));
        VillaRules.NewDay(story, "jubilife_city", new Rolls(0, 2, 4));
        Assert.Equal(2, story.Var(VillaRules.VisitorVariable));
        Assert.Equal(4, story.Var(VillaRules.MessageVariable));
        Assert.False(story.Has(VillaRules.InsideFlag));
        // The day's events draw it too
        var encounters = SpecialEncounters.NewGame(new Random(1));
        DailyEvents.DaysPass(1, story, encounters, "jubilife_city", new Random(5));
        Assert.InRange(story.Var(VillaRules.MessageVariable), 0, 4);
    }

    [Fact]
    public void TheVillaIsGivenFurnishedFromItsCatalogueAndVisited()
    {
        var room = Room(VillaRules.Room);
        var host = new HeadlessScriptHost { Money = 500000 };
        host.Map = room;
        host.Place(null, 11, 11, Direction.Up);

        // The first time in: the boy offers the free table; it is delivered, he leaves for good, and the rival drops in
        host.Answers.Enqueue(0);
        Run("OnEnter", VillaRules.Room, host);
        Assert.Equal(2, host.Story.Var(VillaRules.StateVariable));
        Assert.Equal(1, host.Story.Var(VillaRules.AreaStateVariable));
        Assert.True(host.Story.Has("FLAG_VILLA_FURNITURE_TABLE"));
        Assert.True(host.Story.Has("FLAG_HIDE_VILLA_SCHOOL_KID_M"));
        Assert.True(host.Story.Has("FLAG_HIDE_VILLA_RIVAL"));
        Assert.Equal(500000, host.Money);
        Assert.Contains("pastime furnish 0", host.Log);
        Assert.Equal(VillaRules.NoVisitor, host.Story.Var(VillaRules.VisitorVariable));

        // The catalogue: the Big Sofa is the second row, 120,000
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(0);
        Run("OrderForm", VillaRules.Room, host);
        Assert.True(host.Story.Has("FLAG_VILLA_FURNITURE_BIG_SOFA"));
        Assert.Equal(380000, host.Money);
        // Bought already: nothing more is paid
        host.Answers.Enqueue(1);
        Run("OrderForm", VillaRules.Room, host);
        Assert.Equal(380000, host.Money);
        Assert.True(Said(host, "sold out"));
        // Too dear: the TV is 220,000
        host.Money = 1000;
        host.Answers.Enqueue(5);
        host.Answers.Enqueue(0);
        Run("OrderForm", VillaRules.Room, host);
        Assert.False(host.Story.Has("FLAG_VILLA_FURNITURE_TV"));
        // The small sofa brings the assistant, who stays
        host.Money = 90000;
        host.Answers.Enqueue(2);
        host.Answers.Enqueue(0);
        Run("OrderForm", VillaRules.Room, host);
        Assert.Equal(0, host.Money);
        Assert.Equal(1, host.Story.Var(VillaRules.VisitorVariable));
        Assert.False(host.Story.Has("FLAG_HIDE_VILLA_COUNTERPART"));

        // Coming in on another day: the day's visitor is shown, everyone else sent home
        host.Story.Unset(VillaRules.OutsideFlag);
        host.Story.Unset(VillaRules.InsideFlag);
        host.Story.SetVar(VillaRules.VisitorVariable, 13);
        Run("OnEnter", VillaRules.Room, host);
        Assert.False(host.Story.Has("FLAG_HIDE_VILLA_FLINT"));
        Assert.False(host.Story.Has("FLAG_HIDE_VILLA_VOLKNER"));
        Assert.True(host.Story.Has("FLAG_HIDE_VILLA_COUNTERPART"));
        // The professor waits outside until he is asked in
        host.Story.SetVar(VillaRules.VisitorVariable, 2);
        Run("OnEnter", VillaRules.Room, host);
        Assert.True(host.Story.Has("FLAG_HIDE_VILLA_PROF_ROWAN"));
        host.Story.Set(VillaRules.InsideFlag);
        Run("OnEnter", VillaRules.Room, host);
        Assert.False(host.Story.Has("FLAG_HIDE_VILLA_PROF_ROWAN"));
        // Fantina walks in once the player has stepped up to the table
        host.Story.Unset(VillaRules.InsideFlag);
        host.Story.SetVar(VillaRules.VisitorVariable, 6);
        Run("OnEnter", VillaRules.Room, host);
        Assert.True(host.Story.Has("FLAG_HIDE_VILLA_FANTINA"));
        Assert.Equal(6, host.Story.Var("VAR_MAP_LOCAL_VILLA_CALLER"));
        var trigger = Assert.Single(room.Triggers, t => t.Value == 6);
        Run(trigger.Script, VillaRules.Room, host);
        Assert.False(host.Story.Has("FLAG_HIDE_VILLA_FANTINA"));
        Assert.True(host.Story.Has(VillaRules.OutsideFlag));

        // Each visitor has five things to say, and Maylene and Candice keep the fifth for the Tea Set
        foreach (string who in new[] { "Rival", "Counterpart", "ProfRowan", "Mom", "Roark", "Gardenia", "Fantina", "Maylene", "CrasherWake", "Byron", "Candice", "Volkner", "Cynthia" })
        {
            var said = new HashSet<string>();
            for (int message = 0; message < 5; message++)
            {
                var talk = new HeadlessScriptHost { Map = Room(VillaRules.Room) };
                talk.Story.SetVar(VillaRules.MessageVariable, message);
                said.Add(Run(who, VillaRules.Room, talk).Transcript[0].Text);
            }
            Assert.Equal(5, said.Count);
        }
        var pair = new HeadlessScriptHost { Map = Room(VillaRules.Room) };
        pair.Story.SetVar(VillaRules.VisitorVariable, 14);
        pair.Story.SetVar(VillaRules.MessageVariable, 4);
        string without = Run("Maylene", VillaRules.Room, pair).Transcript[0].Text;
        pair.Story.Set("FLAG_VILLA_FURNITURE_TEA_SET");
        pair.Transcript.Clear();
        Assert.NotEqual(without, Run("Candice", VillaRules.Room, pair).Transcript[0].Text);
    }

    // ------------------------------------------------------------------ Pal Park

    private static List<Pokemon> Six() => new[] { "Bulbasaur", "Charmander", "Squirtle", "Pikachu", "Magikarp", "Geodude" }
        .Select(name => new Pokemon(PokemonDatabase.Get(name)!, 30)).ToList();

    [Fact]
    public void TheParkHoldsSixUntilTheyAreCaughtAndStored()
    {
        var park = new PalParkState();
        Assert.False(park.Begin(new Random(1)));
        Assert.False(park.Stock(Six().Take(5)));
        Assert.True(park.Stock(Six()));
        // CANNOT_MIGRATE_ALREADY_STOCKED: no more until those six have been caught and stored
        Assert.False(park.Stock(Six()));
        Assert.True(park.Begin(new Random(1)));
        park.Tick(5);
        Assert.Equal(0, park.Seconds);
        park.StartClock();
        park.Tick(100.5f);
        park.Stop();
        // CatchingShow_End: two points for every second under a thousand
        Assert.Equal((1000 - 100) * 2, park.Show!.TimePoints);
        park.Abandon();
        Assert.True(park.Stocked);

        var six = park.TakeSix("Pal Park", new DateTime(2026, 6, 1));
        Assert.Equal(6, six.Count);
        Assert.All(six, p => Assert.Equal("Park Ball", p.Ball));
        Assert.False(park.Stocked);

        // The save keeps the six that wait
        var pastimes = new Pastimes();
        pastimes.PalPark.Stock(Six());
        pastimes.Coins.Set(321);
        pastimes.Steps = 12345;
        var loaded = new Pastimes();
        loaded.Load(GameDataFiles.Deserialize<PastimesSave>(GameDataFiles.Serialize(pastimes.ToSave())));
        Assert.Equal(321, loaded.Coins.Coins);
        Assert.Equal(12345, loaded.Steps);
        Assert.Equal(new[] { "Bulbasaur", "Charmander", "Squirtle", "Pikachu", "Magikarp", "Geodude" }, loaded.PalPark.Waiting.Select(p => p.Species.Name).ToArray());
    }

    [Fact]
    public void TheShowsPrizeIsABerryByItsScore()
    {
        // PalParkLobby_Worker_SetPrize: CallIfLt 3000, 3300, 3500, 10000 in turn, and the level that gives sets the
        // score it checks to 0x2710, so no later one gives as well
        for (int seed = 0; seed < 20; seed++)
        {
            Assert.Contains(PalParkState.Prize(0, new Random(seed)), PalParkState.Level1);
            Assert.Contains(PalParkState.Prize(2999, new Random(seed)), PalParkState.Level1);
            Assert.Contains(PalParkState.Prize(3000, new Random(seed)), PalParkState.Level2);
            Assert.Contains(PalParkState.Prize(3299, new Random(seed)), PalParkState.Level2);
            Assert.Contains(PalParkState.Prize(3300, new Random(seed)), PalParkState.Level3);
            Assert.Contains(PalParkState.Prize(3499, new Random(seed)), PalParkState.Level3);
            Assert.Contains(PalParkState.Prize(3500, new Random(seed)), PalParkState.Level4);
            Assert.Contains(PalParkState.Prize(9999, new Random(seed)), PalParkState.Level4);
        }
        Assert.Equal("", PalParkState.Prize(10000, new Random(1)));
        Assert.Equal(8 + 5 + 7 + 6, PalParkState.Level1.Concat(PalParkState.Level2).Concat(PalParkState.Level3).Concat(PalParkState.Level4).Distinct().Count());
        Assert.All(PalParkState.Level1.Concat(PalParkState.Level2).Concat(PalParkState.Level3).Concat(PalParkState.Level4), berry => Assert.NotNull(ItemDatabase.Get(berry)));
    }

    [Fact]
    public void TheLobbyHoldsAShowForSixBroughtOver()
    {
        var lobby = Room("PalParkLobby");
        // Nobody has brought any: the attendant says so, and nothing begins
        var none = new HeadlessScriptHost { Map = lobby };
        none.Story.Set("FLAG_TALKED_TO_PAL_PARK_LOBBY_RECEPTIONIST");
        none.Answers.Enqueue(0);
        Run("Worker", "PalParkLobby", none, who: "worker");
        Assert.True(Said(none, "no Pokémon of yours"));
        Assert.Null(none.Pastimes.PalPark.Show);

        // Six waiting: the show begins and the player is sent into the park
        var host = new HeadlessScriptHost { Map = Room("PalParkLobby") };
        host.Place(null, 7, 12, Direction.Up);
        host.Pastimes.PalPark.Stock(Six());
        host.Story.Set("FLAG_TALKED_TO_PAL_PARK_LOBBY_RECEPTIONIST");
        host.Answers.Enqueue(0);
        Run("Worker", "PalParkLobby", host, who: "worker");
        Assert.NotNull(host.Pastimes.PalPark.Show);
        Assert.Contains(host.Log, line => line.Contains("PalPark") && line.Contains("24") && line.Contains("47"));

        // The park: the countdown starts the clock
        Run("OnEnter", "pal_park", host);
        Assert.True(host.Pastimes.PalPark.Running);
        Assert.Equal(PalParkState.Finished, host.Story.Var(PalParkState.StateVariable));

        // All six caught in order, forty seconds in: back in the lobby the score is told, the record kept, a berry given,
        // and the six sent to the boxes
        var show = host.Pastimes.PalPark.Show!;
        host.Pastimes.PalPark.Tick(40);
        for (int i = 0; i < CatchingShow.Size; i++)
        {
            int met = -1;
            for (int step = 0; step < 100000 && met < 0; step++) met = show.Step(CatchingShow.AreaOf(show.Pokemon.First(p => !show.IsCaught(show.Pokemon.ToList().IndexOf(p))).Species));
            show.CaughtCurrent();
        }
        Assert.Equal(6, show.Caught);
        Run(GameEngine.ParkCaughtAllScript, "pal_park", host);
        host.Map = Room("PalParkLobby");
        host.Place(null, 7, 7, Direction.Down);
        host.Answers.Enqueue(0);
        int berries = host.Bag.AllItems.Sum(s => s.Quantity);
        Run("OnEnter", "PalParkLobby", host);
        Assert.Equal(show.CatchingPoints + show.TypePoints + (1000 - 40) * 2, host.Story.Var(PalParkState.RecordVariable));
        Assert.Equal(berries + 1, host.Bag.AllItems.Sum(s => s.Quantity));
        Assert.Equal(6, host.Boxes.Count);
        Assert.False(host.Pastimes.PalPark.Stocked);
        Assert.Equal(PalParkState.Idle, host.Story.Var(PalParkState.StateVariable));

        // Giving up at the gate keeps the six waiting
        var quitter = new HeadlessScriptHost { Map = Room("PalParkLobby") };
        quitter.Pastimes.PalPark.Stock(Six());
        quitter.Pastimes.PalPark.Begin(new Random(2));
        quitter.Answers.Enqueue(0);
        Run("RetireAtGate", "pal_park", quitter);
        Assert.Equal(PalParkState.Retired, quitter.Story.Var(PalParkState.StateVariable));
        quitter.Place(null, 7, 7, Direction.Down);
        Run("OnEnter", "PalParkLobby", quitter);
        Assert.Null(quitter.Pastimes.PalPark.Show);
        Assert.True(quitter.Pastimes.PalPark.Stocked);
        Assert.Empty(quitter.Boxes);

        // A park with no show in it sends whoever is there back to the lobby
        var stray = new HeadlessScriptHost();
        Run("OnEnter", "pal_park", stray);
        Assert.Contains(stray.Log, line => line.Contains("PalParkLobby"));
    }

    [Fact]
    public void TheParkHasGrassAndWaterInItsCorners()
    {
        var park = MapDatabase.Fresh()("PalPark");
        Assert.Equal("PalPark", park.Name);
        Assert.Equal(64, park.Width);
        // The four corners of the field are tall grass (areas 1 to 4), and there is water for the four water areas'
        // Pokémon in some of them (GetEncounterArea)
        var grass = new HashSet<int>();
        var water = new HashSet<int>();
        for (int y = 0; y < park.Height; y++)
            for (int x = 0; x < park.Width; x++)
            {
                if (park.IsTallGrass(x, y)) grass.Add(CatchingShow.AreaAt(x, y, true, false));
                if (park.IsDeepWater(x, y)) water.Add(CatchingShow.AreaAt(x, y, false, true));
            }
        // Three corners have tall grass; the south-east is beach and sea, and there is a pond besides
        Assert.Equal(new[] { 1, 2, 3 }, grass.OrderBy(a => a).ToArray());
        Assert.Contains(8, water);
        Assert.True(water.Count >= 2);
        // Every species the original could bring over keeps to a corner the park has
        var corners = grass.Concat(water).ToHashSet();
        var homeless = PokemonDatabase.GetAll().Where(s => s.PalPark != null && !corners.Contains(CatchingShow.AreaOf(s))).Select(s => s.Name).ToList();
        Assert.True(homeless.Count == 0, "no corner for: " + string.Join(", ", homeless.Take(12)));
        // The show begins on the tile the original warps to, before the gate
        Assert.True(park.IsWalkable(24, 47));
        Assert.Contains(park.Triggers, t => t.Script == "RetireAtGate");
        Assert.Contains(park.Everyone, n => n.Key == "worker");
    }

    // ------------------------------------------------------------------ the rooms and their doors

    [Theory]
    [InlineData("oreburgh_city", "OreburghMiningMuseum", 305, 747)]
    [InlineData("veilstone_city", "VeilstoneGameCorner", 706, 623)]
    [InlineData("veilstone_city", "VeilstonePrizeExchange", 716, 623)]
    [InlineData("veilstone_city", "VeilstoneSoutheastHouse", 696, 623)]
    [InlineData("resort_area", "ResortAreaVilla", 822, 469)]
    [InlineData("route_221", "PalParkLobby", 306, 909)]
    public void EachRoomIsBehindItsDoorAndLeadsBackOutBeforeIt(string area, string room, int doorX, int doorY)
    {
        var maps = MapDatabase.Fresh();
        var sinnoh = maps("Sinnoh");
        var door = sinnoh.GetWarpAt(doorX, doorY);
        Assert.NotNull(door);
        Assert.Equal(room, door!.TargetMap);
        Assert.Equal(area, sinnoh.AreaAt(doorX, doorY)!.Key);
        var inside = maps(room);
        Assert.True(inside.IsWalkable(door.TargetX, door.TargetY));
        var back = Assert.Single(inside.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal((doorX, doorY + 1), (back.TargetX, back.TargetY));
        // Everyone stands on floor, and no two on one tile but those who are never there together
        Assert.All(inside.Everyone.Where(n => !n.IsPCTerminal), n => Assert.False(inside.IsSolid(n.GridX, n.GridY), $"{n.Key} stands in something at {n.GridX},{n.GridY}"));
    }

    [Fact]
    public void TheMuseumIsOnTheOriginalsPlan()
    {
        var museum = Room("OreburghMiningMuseum");
        Assert.Equal((1, 3), museum.RoomCorner());
        // The door is in the west half of the front wall, under the original's mat; the east wing runs on four rows further
        Assert.Equal(TileType.Door, museum.GetGroundTile(5, 14));
        Assert.True(museum.IsWalkable(5, 13));
        Assert.True(museum.IsWalkable(20, 17));
        Assert.False(museum.IsWalkable(5, 16));
        Assert.Equal(15, museum.Props.Count(p => p.Type == PropType.DisplayCase));
        // The exhibits are read where the original's signs are
        Assert.Contains("lamps", museum.Signboards[(19, 7)]);
        Assert.Contains("How coal is made, 3", museum.Signboards[(15, 3)]);
    }
}
