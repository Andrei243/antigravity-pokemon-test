using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · H12, part n (rulings, "Mauville's slot machines, the two basements and the quiz and contest ladies"):
/// Emerald's own slot machine at Mauville (<see cref="ReelMachine"/>: its tapes, the bet, the lines, the machine running
/// the reels on to keep them clear or to line its bias up, a jackpot, Reel Time and a seeded sitting played twice the
/// same), the two basements opened and played by <see cref="HeadlessScriptHost"/>, and the Quiz and Contest Ladies.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennPartNTests
{
    private static readonly Lazy<List<Map>> Maps = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().ToList());

    private static Map Room(string name) => Maps.Value.Single(m => m.Name == name);

    /// <summary>A generator that always draws the same number, so the machine's draws are known.</summary>
    private sealed class Always : Random
    {
        private readonly int value;
        public Always(int value) => this.value = value;
        public override int Next(int maxValue) => Math.Min(value, maxValue - 1);
        public override int Next() => value;
        public override int Next(int minValue, int maxValue) => Math.Clamp(value, minValue, maxValue - 1);
    }

    // Plays one spin: the coins in, then each reel stopped after so many frames, then the spin's end read out
    private static ReelMachine Spin(ReelMachine m, int coins, int[] waits)
    {
        // A replay spins again on the same coins, with no bet
        Until(m, () => m.Betting || m.AwaitingStop || m.Finished);
        if (m.Finished) return m;
        if (m.Betting)
        {
            for (int i = 0; i < coins; i++) m.Update(ReelKeys.Bet);
            if (coins < ReelMachine.MaxBet) m.Update(ReelKeys.Confirm);
        }
        for (int reel = 0; reel < ReelMachine.Reels; reel++)
        {
            Until(m, () => m.AwaitingStop);
            for (int f = 0; f < waits[reel]; f++) m.Update(ReelKeys.None);
            m.Update(ReelKeys.Confirm);
            int r = reel;
            Until(m, () => !m.IsMoving(r));
        }
        Until(m, () => m.Paying || m.Lost || m.Finished);
        return m;
    }

    private static void Until(ReelMachine m, Func<bool> done, int most = 20000)
    {
        for (int i = 0; i < most && !done(); i++) m.Update(ReelKeys.None);
        Assert.True(done(), "the machine never came to it");
    }

    [Fact]
    public void TheTapesAreEmeraldsAndEveryMachineHasALuckEachDay()
    {
        Assert.All(ReelMachine.Tapes, t => Assert.Equal(ReelMachine.SymbolsPerReel, t.Length));
        // Two sevens of each colour on the left reel, one cherry-free third reel but for its last symbol
        Assert.Equal(2, ReelMachine.Tapes[0].Count(s => s == ReelSymbol.RedSeven));
        Assert.Equal(1, ReelMachine.Tapes[2].Count(s => s == ReelSymbol.Cherry));
        // GetSlotMachineId: the twelve machines' lucks on a day are Emerald's table turned by the day's number
        var day = Enumerable.Range(0, 12).Select(i => ReelMachine.LuckOf(i, 0)).ToArray();
        Assert.Equal(new[] { 0, 1, 2, 2, 1, 3, 3, 5, 2, 4, 4, 3 }, day);
        Assert.Equal(Enumerable.Range(0, 6).ToArray(), Enumerable.Range(0, 12).Select(i => ReelMachine.LuckOf(i, 3)).Distinct().OrderBy(x => x));
    }

    [Fact]
    public void ALineIsThreeOfASymbolTheMixedSevensOrACherryOnTheLeft()
    {
        Assert.Equal(ReelMachine.Match.RedSeven, ReelMachine.MatchOf(ReelSymbol.RedSeven, ReelSymbol.RedSeven, ReelSymbol.RedSeven));
        Assert.Equal(ReelMachine.Match.MixedSeven, ReelMachine.MatchOf(ReelSymbol.BlueSeven, ReelSymbol.BlueSeven, ReelSymbol.RedSeven));
        Assert.Equal(ReelMachine.Match.None, ReelMachine.MatchOf(ReelSymbol.RedSeven, ReelSymbol.BlueSeven, ReelSymbol.RedSeven));
        Assert.Equal(ReelMachine.Match.Cherry, ReelMachine.MatchOf(ReelSymbol.Cherry, ReelSymbol.Lotad, ReelSymbol.Power));
        Assert.Equal(ReelMachine.Match.Azurill, ReelMachine.MatchOf(ReelSymbol.Azurill, ReelSymbol.Azurill, ReelSymbol.Azurill));
        Assert.Equal(new[] { 2, 4, 0, 6, 12, 3, 90, 300, 300 }, ReelMachine.Payouts);
    }

    [Fact]
    public void ABetTakesItsCoinsAndLeavingGivesBackWhatWasBetWithNoSpin()
    {
        var m = new ReelMachine(10, 3, new Always(0xFFFF));
        Until(m, () => m.Betting);
        m.Update(ReelKeys.Bet);
        m.Update(ReelKeys.Bet);
        Assert.Equal((8, 2), (m.Coins, m.Bet));
        m.Update(ReelKeys.Cancel);
        Assert.True(m.AskingToQuit);
        m.Update(ReelKeys.Confirm);
        Assert.True(m.Finished);
        Assert.Equal(10, m.Coins);
        // Two coins are too few for three at once
        var poor = new ReelMachine(2, 3, new Always(0xFFFF));
        Until(poor, () => poor.Betting);
        poor.Update(ReelKeys.MaxBet);
        Assert.Equal(ReelNotice.NeedThree, poor.Notice);
        poor.Update(ReelKeys.Confirm);
        Assert.True(poor.Betting);
        Assert.Equal(2, poor.Coins);
    }

    [Fact]
    public void WithNoBiasTheReelsRunOnSoTheMiddleLineNeverPays()
    {
        // A draw of 0xFFFF misses every bias, so the machine keeps the lit line clear wherever the reels are stopped
        for (int a = 0; a < 21; a += 3)
            for (int b = 0; b < 21; b += 4)
            {
                var m = new ReelMachine(100, 2, new Always(0xFFFF));
                Spin(m, 1, new[] { a * 3, b * 3, (a + b) * 2 });
                Assert.Equal(ReelMachine.Bias.None, m.MachineBias);
                Assert.Equal(0, m.Matches);
                Assert.Equal(99, m.Coins);
            }
    }

    [Fact]
    public void ABiasTowardTheSevensLinesThemUpWhereItCanAndAJackpotPaysThreeHundred()
    {
        // A draw of nought gives the special bias's first try: the straight sevens
        int jackpots = 0;
        for (int wait = 0; wait < 60; wait += 5)
        {
            var m = new ReelMachine(100, 5, new Always(0));
            Spin(m, 3, new[] { wait, wait, wait });
            if (m.Has(ReelMachine.Match.RedSeven) || m.Has(ReelMachine.Match.BlueSeven))
            {
                jackpots++;
                Until(m, () => m.Betting);
                Assert.True(m.Coins >= 97 + 300);
                Assert.Equal(m.Has(ReelMachine.Match.BlueSeven), m.LuckyGame);
                // The sevens' bias goes with the jackpot
                Assert.Equal(ReelMachine.Bias.None, m.MachineBias & ReelMachine.Bias.Sevens);
            }
        }
        Assert.True(jackpots > 0, "no stop lined the sevens up");
    }

    [Fact]
    public void ReelTimeDrawsItsSpinsAndTheReelsStopWhereTheyAreToldWhileTheyLast()
    {
        // Emerald's tables: with the gauge full the draw for none comes up five times in 256
        var random = new Random(11);
        ReelMachine? won = null;
        for (int tries = 0; tries < 40 && won == null; tries++)
        {
            var m = new ReelMachine(200, 5, random);
            m.RigBolts(ReelMachine.MostBolts);
            Until(m, () => m.Betting);
            m.Rig(ReelMachine.Bias.ReelTime);
            m.Update(ReelKeys.MaxBet);
            Until(m, () => m.InReelTime);
            Until(m, () => !m.InReelTime);
            Assert.Equal(m.ReelTimeDrawn, m.ReelTimeSpinsLeft);
            if (m.ReelTimeSpinsLeft > 0) won = m;
        }
        Assert.NotNull(won);
        // The gauge is spent on a Reel Time won
        Assert.Equal(0, won!.Bolts);
        int left = won.ReelTimeSpinsLeft;
        // A stop in Reel Time is where the reel is: the symbol under the middle line as the button was pressed
        for (int reel = 0; reel < ReelMachine.Reels; reel++)
        {
            Until(won, () => won.AwaitingStop);
            won.Update(ReelKeys.Confirm);
            int r = reel;
            Until(won, () => !won.IsMoving(r));
        }
        Until(won, () => won.Paying || won.Lost);
        Assert.Equal(left - 1, won.ReelTimeSpinsLeft);
    }

    [Fact]
    public void ASeededSittingPlaysTheSameTwice()
    {
        static (int, int, int, long) Play()
        {
            var m = new ReelMachine(50, 4, new Random(77));
            for (int spin = 0; spin < 8 && !m.Finished; spin++) Spin(m, 3, new[] { 7 + spin, 3 * spin, 11 });
            Until(m, () => m.Betting || m.Finished);
            return (m.Coins, m.Bolts, m.NetCoinLoss, m.Frame);
        }
        Assert.Equal(Play(), Play());
    }

    [Fact]
    public void TheScreenRunsTheMachineAtSixtyFramesASecondAndClosesWhenItEnds()
    {
        var screen = new ReelMachineScreen();
        screen.Open(4, 0, 2, new Always(0xFFFF));
        screen.Frames(5);
        // With no coins the machine says so, and the notice closes the sitting
        Assert.Equal(ReelNotice.NoCoins, screen.Machine!.Notice);
        screen.Press(ReelKeys.Confirm);
        screen.Frames(2);
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void MauvillesTwelveMachinesAreEmeraldsOwn()
    {
        var corner = Room("MauvilleCity_GameCorner");
        var host = new HeadlessScriptHost { Map = corner, Region = RegionDatabase.Hoenn };
        host.Bag.AddItem(ItemDatabase.Get("Coin Case")!, 1);
        for (int machine = 0; machine < 12; machine++)
        {
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find($"Machine{machine}", "mauville_city_game_corner")!, null);
            runner.RunToEnd();
            Assert.Contains($"pastime reels {machine}", host.Log);
        }
        Assert.DoesNotContain(host.Log, l => l.StartsWith("pastime slots"));
    }

    // ------------------------------------------------------------------ the basements

    private static HeadlessScriptHost Champion(Map map)
    {
        var host = new HeadlessScriptHost { Map = map, Region = RegionDatabase.Hoenn, MapNamed = name => Maps.Value.FirstOrDefault(m => m.Name == name) };
        host.Story.Set("HoennHallOfFame");
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 60, new Random(3)));
        return host;
    }

    private static HeadlessScriptHost Run(HeadlessScriptHost host, string script, string file, NPC? self = null, params int[] answers)
    {
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        self ??= host.Map!.Everyone.FirstOrDefault(n => n.Script == script);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, self);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        return host;
    }

    [Fact]
    public void TheBasementsAreOpenRoomsOfTheWorld()
    {
        foreach (var name in new[] { "MossdeepCity_GameCorner_B1F", "SootopolisCity_MysteryEventsHouse_B1F" })
        {
            Assert.Contains(name, RegionDatabase.Get(RegionDatabase.Hoenn)!.Maps);
            Assert.NotNull(Room(name));
        }
        // The house's stairs down are shut, as Emerald's layout shuts them: only the old man's question leads down
        var house = Room("SootopolisCity_MysteryEventsHouse_1F");
        Assert.DoesNotContain(house.Warps, w => !w.Shut && w.TargetMap == "SootopolisCity_MysteryEventsHouse_B1F");
    }

    [Fact]
    public void BeforeTheHallOfFameNobodyVisitsTheOldMan()
    {
        var host = new HeadlessScriptHost { Map = Room("SootopolisCity_MysteryEventsHouse_1F"), Region = RegionDatabase.Hoenn };
        Run(host, "OldMan", "sootopolis_city_mystery_events_house_1f");
        Assert.Empty(host.Asked);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("warp "));
    }

    [Theory]
    [InlineData(0, "FLAG_HOENN_MYSTERY_VISITOR_ILSE", "hoenn_mystery_visitor_ilse")]
    [InlineData(1, "FLAG_HOENN_MYSTERY_VISITOR_RONAN", "hoenn_mystery_visitor_ronan")]
    [InlineData(2, "FLAG_HOENN_MYSTERY_VISITOR_WREN", "hoenn_mystery_visitor_wren")]
    public void TheDaysVisitorIsBattledInTheBasementOnceADay(int roll, string shown, string team)
    {
        var house = Room("SootopolisCity_MysteryEventsHouse_1F");
        var host = Champion(house);
        host.Rolls.Enqueue(roll);
        Run(host, "OldMan", "sootopolis_city_mystery_events_house_1f", answers: 0);
        Assert.True(host.Story.Has(shown));
        Assert.Equal(roll, host.Story.Var("VAR_HOENN_MYSTERY_VISITOR"));
        Assert.Contains(host.Log, l => l.StartsWith("warp SootopolisCity_MysteryEventsHouse_B1F 3 2"));
        Assert.NotNull(TrainerDatabase.Get(team));

        // Downstairs the one shown is the day's visitor, and the battle with them takes the player back up to the old man
        var basement = host.Map!;
        Assert.Equal("SootopolisCity_MysteryEventsHouse_B1F", basement.Name);
        var visitor = Assert.Single(basement.NPCs, n => n.Script == "Visitor");
        var fought = new List<NPC?>();
        host.Fight = n => { fought.Add(n); return BattleOutcome.Lost; };
        Run(host, "Visitor", "sootopolis_city_mystery_events_house_b1f", visitor);
        Assert.Single(fought);
        Assert.Contains(host.Log, l => l.Contains(team));
        Assert.True(host.Story.Has("FLAG_DAILY_HOENN_MYSTERY_VISITOR"));
        Assert.Contains(host.Log, l => l.StartsWith("warp SootopolisCity_MysteryEventsHouse_1F 5 3"));
        Assert.Equal(0, host.Story.Var("VAR_SOOTOPOLIS_MYSTERY_EVENTS_STATE"));
        Assert.Contains(host.Transcript, t => t.Speaker == "Old Man" && t.Text.Contains("too bad"));

        // And not again that day
        int before = host.Asked.Count;
        Run(host, "OldMan", "sootopolis_city_mystery_events_house_1f");
        Assert.Equal(before, host.Asked.Count);
    }

    [Fact]
    public void MossdeepsManShowsThePlayerDownToThePractisingGirl()
    {
        var corner = Room("MossdeepCity_GameCorner_1F");
        var host = new HeadlessScriptHost { Map = corner, Region = RegionDatabase.Hoenn };
        Run(host, "Attendant", "mossdeep_city_game_corner_1f", answers: 0);
        Assert.Contains(host.Log, l => l.StartsWith("warp MossdeepCity_GameCorner_B1F 3 2"));
        var basement = Room("MossdeepCity_GameCorner_B1F");
        Assert.Contains(basement.NPCs, n => n.Name == "Girl");
        // The stairs up lead into the hall before the counter
        var up = Assert.Single(basement.Warps);
        Assert.Equal(("MossdeepCity_GameCorner_1F", 5, 7), (up.TargetMap, up.TargetX, up.TargetY));
    }

    // ------------------------------------------------------------------ the ladies

    private static HeadlessScriptHost Center()
    {
        var map = Room("LilycoveCity_PokemonCenter_1F");
        return new HeadlessScriptHost { Map = map, Region = RegionDatabase.Hoenn };
    }

    [Fact]
    public void TheQuizLadysPrizesAreEmeraldsAndEachIsAnItem()
    {
        Assert.Equal(16, QuizLady.Prizes.Length);
        Assert.All(QuizLady.Prizes, p => Assert.NotNull(ItemDatabase.Get(p)));
        Assert.InRange(QuizLady.Pick(0xFFFF), 0, 15);
    }

    [Theory]
    [InlineData(0, 0, "Glitter Mail")]
    [InlineData(10, 1, "TM15")]
    [InlineData(13, 2, "Rare Candy")]
    public void ARightAnswerWinsTheQuestionsPrizeAndAWrongOneNothing(int roll, int answer, string prize)
    {
        var host = Center();
        host.Rolls.Enqueue(roll);
        Run(host, "QuizLady", "lilycove_city_pokemon_center_1f", answers: new[] { 0, answer });
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(prize)!));
        Assert.True(host.Story.Has("FLAG_DAILY_HOENN_QUIZ_LADY"));
        Assert.Equal(0, host.Story.Var(QuizLady.QuestionVariable));

        var wrong = Center();
        wrong.Rolls.Enqueue(roll);
        Run(wrong, "QuizLady", "lilycove_city_pokemon_center_1f", answers: new[] { 0, (answer + 1) % 3 });
        Assert.Equal(0, wrong.Bag.GetQuantity(ItemDatabase.Get(prize)!));
        // Nothing more that day
        int asked = wrong.Asked.Count;
        Run(wrong, "QuizLady", "lilycove_city_pokemon_center_1f");
        Assert.Equal(asked, wrong.Asked.Count);
    }

    [Fact]
    public void EveryQuestionHasThreeAnswersAndAPrize()
    {
        for (int q = 0; q < 16; q++)
        {
            var host = Center();
            host.Rolls.Enqueue(q);
            Run(host, "QuizLady", "lilycove_city_pokemon_center_1f", answers: new[] { 0, 0 });
            Assert.Equal(2, host.Asked.Count);
        }
    }

    [Fact]
    public void TheContestLadysPokemonTakesAPokeblockOfItsConditionsFlavourAsAGoodOne()
    {
        Assert.Equal((true, 30), ContestLady.Judge(0, new Pokeblock(PokeblockColor.Red, 30, 0, 0, 0, 0, 20)));
        Assert.Equal((false, 0), ContestLady.Judge(1, new Pokeblock(PokeblockColor.Red, 30, 0, 0, 0, 0, 20)));
        Assert.True(ContestLady.Ready(5, 0));
        Assert.True(ContestLady.Ready(1, 5));
        Assert.False(ContestLady.Ready(4, 4));

        // Smart: a Kecleon, shown beside her, wants bitter
        var host = Center();
        host.Rolls.Enqueue(3);
        host.Bag.AddItem(ItemDatabase.Get("Pokéblock Case")!, 1);
        host.PokeblockCase.Add(new Pokeblock(PokeblockColor.Green, 0, 0, 0, 25, 0, 10));
        host.PokeblockCase.Add(new Pokeblock(PokeblockColor.Red, 18, 0, 0, 0, 0, 10));
        Run(host, "ContestLady", "lilycove_city_pokemon_center_1f", answers: new[] { 0, 0 });
        Assert.Equal(4, host.Story.Var(ContestLady.CategoryVariable));
        Assert.True(host.Story.Has("FLAG_HOENN_CONTEST_LADY_SMART"));
        Assert.Equal((1, 0, 25), (host.Story.Var(ContestLady.GoodVariable), host.Story.Var(ContestLady.OtherVariable), host.Story.Var(ContestLady.SheenVariable)));
        Assert.Equal(1, host.PokeblockCase.Count);
        Assert.True(host.Story.Has("FLAG_DAILY_HOENN_CONTEST_LADY"));
        host.Map!.ApplyPresence(host.Story.Has);
        Assert.Contains(host.Map.NPCs, n => n.Name == "Kecleon");

        // The fifth good one makes it ready, and the next talk draws another contest
        host.Story.Unset("FLAG_DAILY_HOENN_CONTEST_LADY");
        host.Story.SetVar(ContestLady.GoodVariable, 4);
        host.PokeblockCase.Add(new Pokeblock(PokeblockColor.Green, 0, 0, 0, 12, 0, 10));
        Run(host, "ContestLady", "lilycove_city_pokemon_center_1f", answers: new[] { 0, 0 });
        Assert.Equal((0, 0), (host.Story.Var(ContestLady.CategoryVariable), host.Story.Var(ContestLady.GoodVariable)));
    }

    [Fact]
    public void WithoutPokeblocksTheContestLadyAsksForNone()
    {
        var host = Center();
        host.Rolls.Enqueue(0);
        Run(host, "ContestLady", "lilycove_city_pokemon_center_1f");
        Assert.Empty(host.Asked);
        Assert.False(host.Story.Has("FLAG_DAILY_HOENN_CONTEST_LADY"));
    }
}
