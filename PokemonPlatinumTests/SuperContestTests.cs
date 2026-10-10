using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The Super Contests (plan 06 · R17a): the moves' contest data, the tables the contests are held by, who may enter,
/// the Visual Competition's hearts, the dance and how a step is graded, the acting and every effect, the results, what
/// a win leaves, the Fashion Case, a whole contest through its screen, and the Contest Hall's receptionists. Every
/// number is worked out from the original's code (<c>src/contest.c</c>, <c>src/unk_02094EDC.c</c>,
/// <c>src/overlay017</c>) in a comment beside it.
/// </summary>
[Collection("MapDatabase")]
public class SuperContestTests
{
    /// <summary>A generator whose draws are the numbers queued, in order, each taken modulo what is asked; nought once they run out.</summary>
    private sealed class Draws : Random
    {
        public readonly Queue<int> Queue = new();
        public Draws Then(params int[] values)
        {
            foreach (int v in values) Queue.Enqueue(v);
            return this;
        }
        public override int Next(int maxValue) => Queue.Count > 0 ? Queue.Dequeue() % maxValue : 0;
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
    }

    private static Pokemon Mon(string species, params string[] moves)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, 20, Gender.Male, Nature.Hardy, false);
        if (moves.Length > 0) Give(p, moves);
        return p;
    }

    private static void Give(Pokemon p, params string[] moves)
    {
        p.Moves.Clear();
        foreach (string move in moves) p.Moves.Add(MoveDatabase.Create(move));
    }

    private static MoveData M(string name) => MoveDatabase.Get(name);

    // An opponent of a test's own: a Bidoof with these moves and this way of thinking
    private static ContestOpponent Rival(string name, int mind, params string[] moves) => new()
    {
        Name = name, Nickname = name + "'s", Species = "Bidoof", Moves = moves.ToList(), Mind = mind, Fame = 1
    };

    // A contest of the test's own four: the player's Pokémon and three Bidoof, the generator's draws the test's to queue
    private static (SuperContest Contest, Draws Chance) Stage(ContestStat category = ContestStat.Cool, ContestRank rank = ContestRank.Normal,
        ContestMode mode = ContestMode.Official, Pokemon? mine = null, int mind = 0)
    {
        var chance = new Draws();
        var contest = new SuperContest(mine ?? Mon("Turtwig", "Cut", "Pound", "Water Gun", "Gust"), "Lucas", false, category, rank, mode, chance,
            new[] { Rival("Ann", mind, "Cut", "Pound"), Rival("Bob", mind, "Cut", "Pound"), Rival("Cyd", mind, "Cut", "Pound") });
        return (contest, chance);
    }

    private static ActingChoice[] Acts(params (string Move, int Judge)[] choices) =>
        choices.Select(c => new ActingChoice(M(c.Move), c.Judge)).ToArray();

    // ---------------------------------------------------------------- the data

    [Fact]
    public void EveryMoveOfPlatinumHasItsQualityAndEffectOnStage()
    {
        // res/moves/pound/data.json: "contest": { "effect": "CONTEST_EFFECT_BASIC", "type": "CONTEST_TYPE_TOUGH" }
        var pound = M("Pound").Contest!;
        Assert.Equal((ContestStat.Tough, ContestEffect.Basic, 3), (pound.Type, pound.Effect, pound.Hearts));
        Assert.Equal((ContestStat.Cool, ContestEffect.ConsecutiveUse), (M("Double Kick").Contest!.Type, M("Double Kick").Contest!.Effect));
        Assert.Equal(ContestEffect.TwoHeartsWhenVoltageUp, M("Zap Cannon").Contest!.Effect);

        var platinum = MoveDatabase.GetAll().Where(m => m.Id is >= 1 and <= 467).ToList();
        Assert.Equal(467, platinum.Count);
        Assert.All(platinum, m => Assert.NotNull(m.Contest));
        Assert.All(platinum, m => Assert.NotEqual(ContestEffect.None, m.Contest!.Effect));
        Assert.All(MoveDatabase.GetAll().Where(m => m.Id > 467), m => Assert.Null(m.Contest));
    }

    [Fact]
    public void AnEffectIsWorthWhatTheOriginalsTableSays()
    {
        // Unk_020F568C's unk_04: 0x1E for a plain act, 0x14 for most, 0xA for three, nothing for those that reckon their own
        Assert.Equal(30, ContestRules.Appeal(ContestEffect.Basic));
        Assert.Equal(20, ContestRules.Appeal(ContestEffect.FirstNextTurn));
        Assert.Equal(20, ContestRules.Appeal(ContestEffect.MaxVoltageAdvantage));
        Assert.Equal(10, ContestRules.Appeal(ContestEffect.UniqueJudge));
        Assert.Equal(10, ContestRules.Appeal(ContestEffect.TwoVoltageInARowAdvantage));
        Assert.Equal(10, ContestRules.Appeal(ContestEffect.PityPoints));
        foreach (var none in new[] { ContestEffect.DoubledJudge, ContestEffect.Voltage, ContestEffect.AllSameJudge, ContestEffect.DoubleNextTurn,
                     ContestEffect.StealVoltage, ContestEffect.LowVoltageAdvantage, ContestEffect.HighScoreLaterTurn })
            Assert.Equal(0, ContestRules.Appeal(none));
        Assert.All(Enum.GetValues<ContestEffect>().Where(e => e != ContestEffect.None), e => Assert.NotEqual("", ContestRules.Describe(e)));
    }

    [Fact]
    public void AMovesQualityRaisesTheVoltageOfItsOwnContestAndLowersTheTwoItClashesWith()
    {
        // Unk_ov17_022539C8, the Cool Contest's row: { 10, 0, -10, -10, 0 }
        Assert.Equal(new[] { 10, 0, -10, -10, 0 }, Enum.GetValues<ContestStat>().Select(m => ContestRules.Voltage(ContestStat.Cool, m)));
        // The Tough Contest's: { 0, -10, -10, 0, 10 }
        Assert.Equal(new[] { 0, -10, -10, 0, 10 }, Enum.GetValues<ContestStat>().Select(m => ContestRules.Voltage(ContestStat.Tough, m)));
        foreach (var contest in Enum.GetValues<ContestStat>())
            Assert.Equal(10, ContestRules.Voltage(contest, contest));
    }

    [Fact]
    public void TheTablesAreTheArchives()
    {
        // contest_data.narc: 4608 bytes of contestants at 48 each, 96 of judges at 8, 8064 of dressings at 84, twelve
        // themes of a hundred accessories
        Assert.Equal(96, SuperContestDatabase.Opponents.Count);
        Assert.Equal(12, SuperContestDatabase.Judges.Count);
        Assert.Equal(96, SuperContestDatabase.Dressings.Count);
        Assert.Equal(12, SuperContestDatabase.Themes.Count);
        Assert.Equal(100, SuperContestDatabase.Accessories.Count);
        Assert.All(SuperContestDatabase.Themes, t => Assert.Equal(100, t.Points.Length));
        Assert.Equal("Colored Parasol", SuperContestDatabase.AccessoryName(SuperContestDatabase.FirstUniqueAccessory));
        Assert.Equal("Confetti", SuperContestDatabase.AccessoryName(SuperContestDatabase.FirstUniqueAccessory - 1));

        // The first contestant's 48 bytes: Luis and his Pikachu Sparky, level in the Cool and Tough Contests of the Normal Rank
        var luis = SuperContestDatabase.Opponents[0];
        Assert.Equal(("Luis", "Sparky", "Pikachu", ContestRank.Normal), (luis.Name, luis.Nickname, luis.Species, luis.Rank));
        Assert.Equal(new[] { "Thunder Shock", "Quick Attack", "Slam", "Thunderbolt" }, luis.Moves);
        Assert.Equal((3, 1, 3, 1, 2, 30), (luis.Cool, luis.Beauty, luis.Cute, luis.Smart, luis.Tough, luis.Sheen));
        Assert.Equal(new[] { ContestStat.Cool, ContestStat.Tough }, luis.Contests);

        // Everything the tables name exists
        Assert.All(SuperContestDatabase.Opponents, o => Assert.NotNull(PokemonDatabase.Get(o.Species)));
        Assert.All(SuperContestDatabase.Opponents, o => Assert.All(o.Moves, m => Assert.NotNull(MoveDatabase.Get(m).Contest)));
        Assert.All(SuperContestDatabase.Opponents, o => Assert.InRange(o.Moves.Count, 2, 4));
        Assert.All(SuperContestDatabase.Opponents, o => Assert.Equal(12, o.Dressings.Count));
        Assert.All(SuperContestDatabase.Dressings, d => Assert.All(d.Ids, id => Assert.InRange(id, 0, 99)));
        Assert.All(SuperContestDatabase.Judges, j => Assert.Contains(j.Name, new[] { "Dexter", "Jordan", "Keira" }));
    }

    // ---------------------------------------------------------------- who may enter

    [Fact]
    public void ARankAsksForTheRibbonsOfTheRanksUnderIt()
    {
        // CheckContestEligibility: not an Egg, not fainted, contestRank <= the quality's ribbons, two moves at least
        var p = Mon("Turtwig", "Tackle", "Withdraw");
        Assert.Null(ContestRules.WhyNot(p, ContestStat.Cute, ContestRank.Normal));
        Assert.Equal("rank", ContestRules.WhyNot(p, ContestStat.Cute, ContestRank.Great));
        p.Ribbons.Add(ContestRules.Ribbon(ContestStat.Cute, ContestRank.Normal));
        Assert.Null(ContestRules.WhyNot(p, ContestStat.Cute, ContestRank.Great));
        // Another quality's ribbon is no help
        Assert.Equal("rank", ContestRules.WhyNot(p, ContestStat.Cool, ContestRank.Great));
        Assert.Equal("rank", ContestRules.WhyNot(p, ContestStat.Cute, ContestRank.Ultra));

        Give(p, "Tackle");
        Assert.Equal("moves", ContestRules.WhyNot(p, ContestStat.Cute, ContestRank.Normal));
        Give(p, "Tackle", "Withdraw");
        p.CurrentHP = 0;
        Assert.Equal("fainted", ContestRules.WhyNot(p, ContestStat.Cute, ContestRank.Normal));
        p.CurrentHP = 1;
        p.IsEgg = true;
        Assert.Equal("egg", ContestRules.WhyNot(p, ContestStat.Cute, ContestRank.Normal));
    }

    [Fact]
    public void TheRibbonsAreNamedAsTheOriginalNamesThem()
    {
        Assert.Equal("RIBBON_COOL", ContestRules.Ribbon(ContestStat.Cool, ContestRank.Normal));
        Assert.Equal("RIBBON_SMART_ULTRA", ContestRules.Ribbon(ContestStat.Smart, ContestRank.Ultra));
        Assert.Equal("Tough Ribbon Master", ContestRules.RibbonName(ContestStat.Tough, ContestRank.Master));

        // CalcMonContestFame: one, and one more for each ribbon in a row from the first
        var p = Mon("Turtwig");
        Assert.Equal(1, ContestRules.Fame(p, ContestStat.Cool));
        p.Ribbons.Add("RIBBON_COOL");
        p.Ribbons.Add("RIBBON_COOL_GREAT");
        p.Ribbons.Add("RIBBON_COOL_MASTER");
        Assert.Equal(3, ContestRules.Fame(p, ContestStat.Cool));
        Assert.Equal(3, ContestRules.RibbonsOf(p, ContestStat.Cool));
        Assert.Equal(1, ContestRules.Fame(p, ContestStat.Beauty));
    }

    // ---------------------------------------------------------------- the draws

    [Fact]
    public void TheThemeIsDrawnFromThoseOfTheRank()
    {
        // sub_02095A74: { 2, 3, 4 }, then { 0, 1, 5 } from the Great Rank, then { 6, 7, 8 } from the Ultra Rank; any of twelve at the Master Rank
        Assert.Equal(new[] { 2, 3, 4, 2 }, new[] { 0, 1, 2, 3 }.Select(d => SuperContest.ChooseTheme(ContestRank.Normal, () => d)));
        Assert.Equal(new[] { 2, 3, 4, 0, 1, 5 }, Enumerable.Range(0, 6).Select(d => SuperContest.ChooseTheme(ContestRank.Great, () => d)));
        Assert.Equal(8, SuperContest.ChooseTheme(ContestRank.Ultra, () => 8));
        Assert.Equal(11, SuperContest.ChooseTheme(ContestRank.Master, () => 11 + 12));
        Assert.Equal("The Created", SuperContestDatabase.Themes[2].Name);
    }

    [Fact]
    public void TheJudgeInTheMiddleIsDrawnAndTheOthersSitAtTheSides()
    {
        // sub_020954F0 at the Normal Rank: Jordan and Keira have 1 for every quality, Dexter 2, so he is the only one to draw
        var judges = SuperContest.ChooseJudges(ContestStat.Cool, ContestRank.Normal, () => 7);
        Assert.Equal(new[] { "Jordan", "Dexter", "Keira" }, judges.Select(j => j.Name));
        foreach (var rank in Enum.GetValues<ContestRank>())
            foreach (var quality in Enum.GetValues<ContestStat>())
                Assert.Equal(3, SuperContest.ChooseJudges(quality, rank, () => 3).Distinct().Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThreeOpponentsOfTheRankWhoEnterTheContestAreDrawn(bool afterGame)
    {
        var chance = new Random(11);
        foreach (var rank in Enum.GetValues<ContestRank>())
            foreach (var quality in Enum.GetValues<ContestStat>())
                for (int n = 0; n < 12; n++)
                {
                    var chosen = SuperContest.ChooseOpponents(quality, rank, practice: false, afterGame, () => chance.Next(0x10000));
                    Assert.Equal(3, chosen.Distinct().Count());
                    Assert.All(chosen, o => Assert.Equal(rank, o.Rank));
                    Assert.All(chosen, o => Assert.Contains(quality, o.Contests));
                    Assert.All(chosen, o => Assert.False(o.Practice || o.SingleRound));
                    // sub_02094F04: those of before the game's end (1) are gone after it, those of after it (2, 3) unknown before; at most one guest
                    Assert.All(chosen, o => Assert.True(afterGame ? o.Season != 1 : o.Season is 0 or 1));
                    Assert.InRange(chosen.Count(o => o.Season == 3), 0, 1);
                }
        // A practice round's opponents are the practice rounds' own
        foreach (var quality in Enum.GetValues<ContestStat>())
            Assert.All(SuperContest.ChooseOpponents(quality, ContestRank.Normal, practice: true, afterGame, () => chance.Next(0x10000)), o => Assert.True(o.Practice));
    }

    [Fact]
    public void AnOpponentsPokemonIsTheTables()
    {
        // sub_02095380: level 10, the moves, the nickname and the condition from the table; never shiny
        var luis = SuperContestDatabase.Opponents[0];
        var sparky = SuperContest.PokemonOf(luis, new Random(3));
        Assert.Equal(("Pikachu", 10, "Sparky", false), (sparky.Species.Name, sparky.Level, sparky.DisplayName, sparky.IsShiny));
        Assert.Equal(luis.Moves, sparky.Moves.Select(m => m.Data.Name));
        Assert.Equal((3, 1, 3, 1, 2, 30), (sparky.Cool, sparky.Beauty, sparky.Cute, sparky.Smart, sparky.Tough, sparky.Sheen));
        Assert.Equal("Luis", sparky.OriginalTrainer!.Name);
        // sub_02074128: a man's Pokémon is male where its species has both (25 past the species' share of females)
        Assert.Equal(luis.Gender == 0 ? Gender.Male : Gender.Female, sparky.Gender);
    }

    // ---------------------------------------------------------------- the Visual Competition

    [Fact]
    public void TheConditionIsScoredForTheContestsQualityItsNeighboursAndTheSheen()
    {
        // ov17_0223F374, a Cool Contest: cool + (tough + beauty + sheen) / 2 = 100 + (40 + 30 + 51) / 2 = 160
        int[] condition = { 100, 30, 7, 9, 40 };
        Assert.Equal(160, ContestRules.ConditionScore(ContestStat.Cool, condition, 51, null));
        // The Red Scarf is the Cool Contest's: 160 × 110 / 100 = 176; the Blue and Yellow its neighbours': 160 × 105 / 100 = 168
        Assert.Equal(176, ContestRules.ConditionScore(ContestStat.Cool, condition, 51, "Red Scarf"));
        Assert.Equal(168, ContestRules.ConditionScore(ContestStat.Cool, condition, 51, "Blue Scarf"));
        Assert.Equal(168, ContestRules.ConditionScore(ContestStat.Cool, condition, 51, "Yellow Scarf"));
        Assert.Equal(160, ContestRules.ConditionScore(ContestStat.Cool, condition, 51, "Pink Scarf"));
        // A Smart Contest: smart + (cute + tough + sheen) / 2 = 9 + (7 + 40 + 51) / 2 = 58, and the Green Scarf's 63
        Assert.Equal(58, ContestRules.ConditionScore(ContestStat.Smart, condition, 51, null));
        Assert.Equal(63, ContestRules.ConditionScore(ContestStat.Smart, condition, 51, "Green Scarf"));
    }

    [Fact]
    public void ScoresBecomeHeartsByTheRanksSteps()
    {
        // sub_02095928: a heart for each step reached. Normal { 10 … 80 }, Master { 320, 360 … 600 }
        Assert.Equal(new[] { 0, 1, 1, 8, 8 }, new[] { 9, 10, 19, 80, 500 }.Select(s => ContestRules.ConditionHearts(ContestRank.Normal, s)));
        Assert.Equal(new[] { 0, 1, 2, 8 }, new[] { 319, 320, 399, 600 }.Select(s => ContestRules.ConditionHearts(ContestRank.Master, s)));
        Assert.Equal(new[] { 0, 1, 5 }, new[] { 89, 90, 170 }.Select(s => ContestRules.ConditionHearts(ContestRank.Great, s)));
        // sub_0209598C: nothing for nothing, then 1 up to the first step, 2 to the second, 3 to the third, 4 past it. Normal { 3, 5, 8 }
        Assert.Equal(new[] { 0, 1, 1, 2, 2, 3, 3, 4 }, new[] { 0, 1, 3, 4, 5, 6, 8, 9 }.Select(s => ContestRules.DressHearts(ContestRank.Normal, s)));
        Assert.Equal(new[] { 1, 2, 3, 4 }, new[] { 10, 20, 30, 31 }.Select(s => ContestRules.DressHearts(ContestRank.Master, s)));
        // ov22_02257580: five accessories at the Normal Rank, twenty at the Master Rank
        Assert.Equal(new[] { 5, 10, 15, 20 }, Enum.GetValues<ContestRank>().Select(ContestRules.AccessoryLimit));
    }

    [Fact]
    public void TheDressingUpScoresWhatItsAccessoriesAreWorthForTheTheme()
    {
        var mine = Mon("Turtwig", "Cut", "Pound");
        (mine.Cool, mine.Beauty, mine.Tough, mine.Sheen) = (20, 10, 4, 6);
        // Every draw nought: the first of the Normal Rank's themes, "The Created"
        var chance = new Draws();
        var contest = new SuperContest(mine, "Lucas", false, ContestStat.Cool, ContestRank.Normal, ContestMode.Official, false, chance);
        Assert.Equal(2, contest.Theme);
        var theme = SuperContestDatabase.Themes[2];
        int glitter = SuperContestDatabase.Accessory("Glitter Powder"), fluff = SuperContestDatabase.Accessory("White Fluff");
        // The theme's file: 2 for the Glitter Powder, nothing for a White Fluff
        Assert.Equal((2, 0), (theme.PointsFor(glitter), theme.PointsFor(fluff)));

        contest.JudgeVisual(new[] { glitter, glitter, fluff });
        // 20 + (4 + 10 + 6) / 2 = 30: three hearts at the Normal Rank; 4 points of dressing-up: past 3, within 5, two hearts
        Assert.Equal((30, 3, 4, 2, 5), (contest.Player.ConditionScore, contest.Player.ConditionHearts, contest.Player.DressScore, contest.Player.DressHearts, contest.Player.VisualHearts));

        // The opponents wear the dressings their rows keep for the theme, and are scored the same way
        foreach (var rival in contest.Contestants.Skip(1))
        {
            var dressing = SuperContestDatabase.Dressings[rival.Source!.Dressings[2]];
            Assert.Equal(dressing.Ids, rival.Accessories);
            Assert.Equal(dressing.Ids.Sum(theme.PointsFor), rival.DressScore);
            Assert.Equal(ContestRules.DressHearts(ContestRank.Normal, rival.DressScore), rival.DressHearts);
        }
        // No more than the rank allows
        contest.JudgeVisual(Enumerable.Repeat(glitter, 9));
        Assert.Equal(5, contest.Player.Accessories.Count);
    }

    // ---------------------------------------------------------------- the dance

    [Fact]
    public void EachContestHasItsMusic()
    {
        // Unk_ov17_02253084 and Unk_ov17_0225312C: 120 beats a minute and three steps at the Normal Rank, four at the Great Rank
        var normal = DanceSong.For(ContestRank.Normal, ContestStat.Cute);
        // unk_18 = 60 × 30 × 10000 / 120 = 150000; a bar of 16 beats: 240 frames
        Assert.Equal((120, 3, 150000, 240, 32), (normal.Tempo, normal.Steps, normal.Beat, normal.BarFrames, normal.Slots));
        Assert.Equal(4, DanceSong.For(ContestRank.Great, ContestStat.Cool).Steps);
        // The Ultra Rank's Cool Contest: 100 beats a minute in three time, 12 beats of 18 frames; its Beauty Contest: 60, 12 beats of 30 frames
        Assert.Equal((100, 216, 24), (DanceSong.For(ContestRank.Ultra, ContestStat.Cool).Tempo, DanceSong.For(ContestRank.Ultra, ContestStat.Cool).BarFrames, DanceSong.For(ContestRank.Ultra, ContestStat.Cool).Slots));
        Assert.Equal((60, 360), (DanceSong.For(ContestRank.Master, ContestStat.Beauty).Tempo, DanceSong.For(ContestRank.Master, ContestStat.Beauty).BarFrames));
        Assert.Equal(150, DanceSong.For(ContestRank.Ultra, ContestStat.Cute).Tempo);
        // The windows: two frames for an excellent step and three for a good one at the lower ranks, one and two at most of the upper
        Assert.Equal((2, 3), (normal.Excellent, normal.Good));
        Assert.Equal((1, 2), (DanceSong.For(ContestRank.Master, ContestStat.Tough).Excellent, DanceSong.For(ContestRank.Master, ContestStat.Tough).Good));
    }

    [Fact]
    public void AStepIsMeasuredAgainstTheNearestHalfBeat()
    {
        var dance = new SuperContestDance(Stage().Contest);
        // ov17_0224DDE4 at 120 beats a minute: half-beats every 7.5 frames, rounded: 0, 8, 15, 23, 30…
        Assert.Equal((0, 2), dance.Nearest(15));
        Assert.Equal((2, 2), dance.Nearest(17));
        Assert.Equal((3, 2), dance.Nearest(18));
        // Frame 19 is four from 15 and four from 23: the one before wins
        Assert.Equal((4, 2), dance.Nearest(19));
        Assert.Equal((3, 3), dance.Nearest(20));
        Assert.Equal((0, 4), dance.Nearest(30));
        Assert.Equal((0, 16), dance.Nearest(120));
    }

    // Lets the dance run with nobody at the player's buttons until something holds
    private static void Run(SuperContestDance dance, Func<bool> until)
    {
        for (int i = 0; i < 20000 && !until(); i++) dance.Advance();
        Assert.True(until());
    }

    [Fact]
    public void TheLeadsStepsOnTheBeatScoreTwoAndNearItOne()
    {
        var (contest, chance) = Stage();
        chance.Then(5, 6, 7);
        var dance = new SuperContestDance(contest);
        // The fourth entry leads first and the player last (unk_05 = { 3, 2, 1, 0 }, turned a place after each lead)
        Assert.Equal(3, dance.Lead);
        Run(dance, () => dance.Rotation == 3);
        Assert.Equal((0, 0, 0), (dance.Lead, dance.Bar, dance.Frame));
        Assert.Equal(0, dance.Score[0]);

        // On the second beat's frame (30): nothing off, two points, and the step is the others' to repeat
        Run(dance, () => dance.Frame == 29);
        Assert.True(dance.Press(DanceStep.Jump));
        dance.Advance();
        var mark = Assert.Single(dance.Danced, m => m.Contestant == 0);
        Assert.Equal((DanceStep.Jump, 0, 2, 4, 30), (mark.Step, mark.Grade, mark.Points, mark.Slot, mark.Frame));
        Assert.Equal((4, DanceStep.Jump), dance.Led.Single());
        // The next is read a beat less two frames later (15 - 2 = 13): not before frame 43
        Run(dance, () => dance.Frame == 40);
        Assert.False(dance.Press(DanceStep.Left));
        Run(dance, () => dance.Frame == 47);
        // Frame 48 is three from the beat at 45: good, one point
        Assert.True(dance.Press(DanceStep.Left));
        dance.Advance();
        Assert.Equal((2, 1), (dance.Danced.Single(m => m.Contestant == 0).Grade, dance.Danced.Single(m => m.Contestant == 0).Points));
        // Frame 64 is four from 60 and from 68: a miss, and still a step taken
        Run(dance, () => dance.Frame == 63);
        Assert.True(dance.Press(DanceStep.Right));
        dance.Advance();
        Assert.True(dance.Danced.Single(m => m.Contestant == 0).Missed);
        Assert.Equal(3, dance.Score[0]);
        // Three steps to a bar at the Normal Rank
        Run(dance, () => dance.Frame == 80);
        Assert.False(dance.Press(DanceStep.Jump));
        // The lead's half ends a quarter of a beat (three frames) before the middle: a step may still fall on frame 116, not on 117
        Run(dance, () => dance.Bar == 1 && dance.Frame == 115);
        Assert.True(dance.CanPress);
        dance.Advance();
        Assert.False(dance.CanPress);
    }

    [Fact]
    public void TheOthersRepeatTheLeadsStepsHalfABarLater()
    {
        var (contest, chance) = Stage();
        chance.Then(21, 22, 23);
        var dance = new SuperContestDance(contest);
        // The first half: the lead dances its three steps
        Run(dance, () => dance.Frame == dance.Song.BarFrames / 2);
        Assert.Equal(3, dance.Led.Count);
        Assert.False(dance.LeadsNow);
        var led = dance.Led.ToList();

        // The first step again, on its half-beat's own frame half a bar on: excellent
        int Frame(int slot) => (int)(((slot + 16) * 75000L + 5000) / 10000);
        Run(dance, () => dance.Frame == Frame(led[0].Slot) - 1);
        Assert.True(dance.Press(led[0].Step));
        dance.Advance();
        var right = dance.Danced.Single(m => m.Contestant == 0);
        Assert.Equal((0, 2, false), (right.Grade, right.Points, right.WrongStep));

        // The second with another step: on the beat, and worth nothing
        Run(dance, () => dance.Frame == Frame(led[1].Slot) - 1);
        var other = led[1].Step == DanceStep.Jump ? DanceStep.Left : DanceStep.Jump;
        Assert.True(dance.Press(other));
        dance.Advance();
        var wrong = dance.Danced.Single(m => m.Contestant == 0);
        Assert.Equal((0, true, true), (wrong.Points, wrong.WrongStep, wrong.Missed));
        Assert.Equal(2, dance.Score[0]);

        // A step where the lead made none is a miss
        int free = Enumerable.Range(17, 14).First(s => led.All(l => Math.Abs(l.Slot + 16 - s) > 1) && Frame(s - 16) > dance.Frame + 14);
        Run(dance, () => dance.Frame == Frame(free - 16) - 1);
        Assert.True(dance.Press(DanceStep.Right));
        dance.Advance();
        Assert.True(dance.Danced.Single(m => m.Contestant == 0).Missed);
    }

    [Fact]
    public void ADanceIsTheSameForTheSameDrawsAndEndsAfterFourLeads()
    {
        int[] Scores(int seed)
        {
            var mine = Mon("Turtwig", "Cut", "Pound");
            var contest = new SuperContest(mine, "Lucas", false, ContestStat.Cute, ContestRank.Great, ContestMode.Official, false, new Random(seed));
            var dance = new SuperContestDance(contest);
            Run(dance, () => dance.IsOver);
            Assert.Equal(dance.Score, contest.Contestants.Select(c => c.DanceScore));
            return dance.Score.ToArray();
        }
        int[] once = Scores(5), again = Scores(5);
        Assert.Equal(once, again);
        // The player took no step; an opponent has four leads' two bars of four steps of two points at most
        Assert.Equal(0, once[0]);
        Assert.All(once.Skip(1), s => Assert.InRange(s, 1, 4 * 2 * 4 * 2));
    }

    // ---------------------------------------------------------------- the acting

    [Fact]
    public void ATurnOfPlainActsIsTheMovesHeartsAndTheJudges()
    {
        var (contest, _) = Stage();
        var acting = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        // A Cool Contest. Cut (Cool) raises judge 0 to 10; Pound (Tough) leaves judge 1 alone; Water Gun (Cute) and Gust
        // (Smart) would lower theirs: judge 2 is at nothing, and Gust takes judge 0 back to nothing
        var log = acting.Perform(Acts(("Cut", 0), ("Pound", 1), ("Water Gun", 2), ("Gust", 0)));
        // 30 for a plain act; judge 0 was chosen twice (20 each), judges 1 and 2 once (30)
        Assert.Equal(new[] { 50, 60, 60, 50 }, acting.Total);
        Assert.Equal(new[] { 0, 0, 0 }, acting.Voltage);
        Assert.Equal(new[] { 10, 0, 0, -10 }, log.Where(e => e.Step == ActingStep.Voltage).Select(e => e.Change));
        Assert.True(log.Single(e => e.Step == ActingStep.Perform && e.Contestant == 3).Flag);
        Assert.False(log.Single(e => e.Step == ActingStep.Perform && e.Contestant == 0).Flag);
        // ov17_02243B0C: the lowest goes first next turn, and of two level the one who performed later
        Assert.Equal(new[] { 3, 0, 2, 1 }, acting.Order);
        Assert.Equal(1, acting.Turn);
    }

    [Fact]
    public void AMoveMayNotBeShownTwiceRunningUnlessItsEffectLetsIt()
    {
        var (contest, _) = Stage(mine: Mon("Turtwig", "Cut", "Double Kick", "Pound"));
        var acting = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        ActingChoice[] With(string mine, string theirs) => Acts((mine, 0), (theirs, 1), (theirs, 1), (theirs, 1));
        acting.Perform(With("Cut", "Cut"));
        // ov17_02243A98
        Assert.False(acting.CanUse(0, M("Cut")));
        Assert.True(acting.CanUse(0, M("Double Kick")));
        Assert.Throws<InvalidOperationException>(() => acting.Perform(With("Cut", "Pound")));

        // Double Kick's effect lets it be shown again (ov17_0224687C)…
        acting.Perform(With("Double Kick", "Pound"));
        Assert.True(acting.CanUse(0, M("Double Kick")));
        Assert.True(acting.CanUse(0, M("Cut")));
        // …once: a repeat marks nothing
        acting.Perform(With("Double Kick", "Cut"));
        Assert.False(acting.CanUse(0, M("Double Kick")));
    }

    [Fact]
    public void AFilledVoltagePaysFiveHeartsAndEightWithTheHeadJudge()
    {
        var (contest, _) = Stage();
        var acting = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        acting.Voltage[0] = 40;
        acting.Voltage[1] = 40;
        // ov17_022463C4: 10 × 5 at the sides, 10 × 8 for the judge in the middle (unk_10E)
        var log = acting.Perform(Acts(("Cut", 0), ("Cut", 1), ("Pound", 2), ("Pound", 2)));
        var filled = log.Where(e => e.Step == ActingStep.Voltage && e.Points > 0).ToList();
        Assert.Equal(new[] { (0, 50), (1, 80) }, filled.Select(e => (e.Contestant, e.Points)));
        // 30 + 50 + the judge's 30, and 30 + 80 + 30
        Assert.Equal(new[] { 110, 140 }, acting.Total.Take(2));
        // ov17_02246018: a voltage that was filled starts again
        Assert.Equal(new[] { 0, 0, 0 }, acting.Voltage);
    }

    [Fact]
    public void TheEffectsOfWhereAnActFallsInTheTurn()
    {
        var (contest, _) = Stage();
        // Thunderbolt (first, +2), Mega Kick (last, +2), Reversal (double if last), Trump Card (more the later)
        var acting = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        acting.Perform(Acts(("Thunderbolt", 0), ("Mega Kick", 0), ("Trump Card", 0), ("Reversal", 0)));
        // All four for one judge: nothing from the judge. Cool moves all, the voltage 10, 20, 30, 40
        // Thunderbolt first: 20 + 20. Mega Kick second: 20. Trump Card third: 0 + 30. Reversal last: 20 + 20.
        Assert.Equal(new[] { 40, 20, 30, 40 }, acting.Total);
        Assert.Equal(40, acting.Voltage[0]);

        var later = new SuperContestActing(contest, new[] { 3, 2, 1, 0 });
        later.Perform(Acts(("Thunderbolt", 0), ("Mega Kick", 1), ("Trump Card", 2), ("Reversal", 0)));
        // Reversal first: 20 and the judge's 20. Trump Card second: 20 + 30. Mega Kick third: 20 + 30. Thunderbolt last: 20 + 20.
        Assert.Equal(new[] { 40, 50, 50, 40 }, later.Total);
        // Mega Kick, last for once: 20 + 20 + the judge's 30
        var last = new SuperContestActing(contest, new[] { 0, 2, 3, 1 });
        last.Perform(Acts(("Cut", 0), ("Mega Kick", 1), ("Cut", 0), ("Cut", 0)));
        Assert.Equal(70, last.Total[1]);
    }

    [Fact]
    public void TheEffectsOfTheJudgeChosen()
    {
        var (contest, _) = Stage();
        // Razor Wind: +3 with the judge to itself (ov17_02246834): 10 + 30 + the judge's 30; shared: 10 + 20
        var alone = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        alone.Perform(Acts(("Razor Wind", 2), ("Pound", 1), ("Pound", 1), ("Razor Wind", 1)));
        Assert.Equal(70, alone.Total[0]);
        Assert.Equal(10 + 10, alone.Total[3]);

        // Guillotine: +15 when all four chose one judge (ov17_02246918), who then gives nobody anything
        var all = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        var log = all.Perform(Acts(("Guillotine", 1), ("Pound", 1), ("Pound", 1), ("Pound", 1)));
        Assert.Equal(new[] { 150, 30, 30, 30 }, all.Total);
        Assert.Equal(0, log.Single(e => e.Step == ActingStep.Judge).Points);
        var not = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        not.Perform(Acts(("Guillotine", 1), ("Pound", 1), ("Pound", 1), ("Pound", 0)));
        Assert.Equal(10, not.Total[0]);
    }

    [Fact]
    public void TheEffectsThatReadTheVoltage()
    {
        var (contest, _) = Stage();
        // Roost adds the judge's voltage as it stood (ov17_022468D4): 30, and then raises it; the judge's 30 on top
        var roost = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        roost.Voltage[0] = 30;
        roost.Perform(Acts(("Roost", 0), ("Pound", 1), ("Pound", 1), ("Pound", 1)));
        Assert.Equal((60, 40), (roost.Total[0], roost.Voltage[0]));

        // Detect scores more the lower the voltage (ov17_02246BC0): 40, 30, 20, 10, nothing
        foreach (var (voltage, worth) in new[] { (0, 40), (10, 30), (20, 20), (30, 10), (40, 0) })
        {
            var detect = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
            detect.Voltage[2] = voltage;
            detect.Perform(Acts(("Detect", 2), ("Pound", 1), ("Pound", 1), ("Pound", 1)));
            // A Cool move at 40 fills the voltage: five hearts more
            Assert.Equal(worth + 30 + (voltage == 40 ? 50 : 0), detect.Total[0]);
        }

        // Zap Cannon: +2 when it raises the voltage (ov17_022467CC), which a Cool move does here
        var zap = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        zap.Perform(Acts(("Zap Cannon", 0), ("Pound", 1), ("Pound", 1), ("Pound", 1)));
        Assert.Equal(20 + 20 + 30, zap.Total[0]);

        // Copycat takes the bonus the act before earned by filling a voltage (ov17_02246A50); Hyper Beam gets +3 after one (ov17_02246E08)
        var steal = new SuperContestActing(contest, new[] { 1, 0, 3, 2 });
        steal.Voltage[0] = 40;
        steal.Perform(Acts(("Copycat", 2), ("Cut", 0), ("Pound", 1), ("Pound", 1)));
        Assert.Equal(0 + 50 + 30, steal.Total[0]);
        var after = new SuperContestActing(contest, new[] { 1, 0, 3, 2 });
        after.Voltage[0] = 40;
        after.Perform(Acts(("Hyper Beam", 2), ("Cut", 0), ("Pound", 1), ("Pound", 1)));
        Assert.Equal(20 + 30 + 30, after.Total[0]);
        // With no voltage filled before it, neither gets anything
        var none = new SuperContestActing(contest, new[] { 1, 0, 3, 2 });
        none.Perform(Acts(("Copycat", 2), ("Cut", 0), ("Pound", 1), ("Pound", 1)));
        Assert.Equal(30, none.Total[0]);
    }

    [Fact]
    public void TheEffectsThatHoldTheVoltage()
    {
        var (contest, _) = Stage();
        // Barrier: no voltage rises for the rest of the turn, its own act's included (ov17_022469FC)
        var barrier = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        barrier.Perform(Acts(("Barrier", 0), ("Cut", 1), ("Cut", 2), ("Cut", 2)));
        Assert.Equal(new[] { 0, 0, 0 }, barrier.Voltage);

        // Leer: none falls (ov17_02246C88). Gust would take judge 0 down from 20
        var leer = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        leer.Voltage[0] = 20;
        leer.Perform(Acts(("Leer", 1), ("Gust", 0), ("Pound", 2), ("Pound", 2)));
        Assert.Equal(20, leer.Voltage[0]);
        var without = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        without.Voltage[0] = 20;
        without.Perform(Acts(("Pound", 1), ("Gust", 0), ("Pound", 2), ("Pound", 2)));
        Assert.Equal(10, without.Voltage[0]);

        // Screech lowers every judge's voltage that isn't nothing by one (ov17_02246960), then its own quality's due at
        // its judge, who is at nothing already
        var screech = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        (screech.Voltage[0], screech.Voltage[2]) = (20, 30);
        screech.Perform(Acts(("Screech", 1), ("Pound", 1), ("Pound", 1), ("Pound", 1)));
        Assert.Equal(new[] { 10, 0, 20 }, screech.Voltage);

        // Drain Punch in a Beauty Contest: +3 when the act before raised the voltage too (ov17_02246CDC)
        var (beauty, _) = Stage(ContestStat.Beauty);
        var twice = new SuperContestActing(beauty, new[] { 1, 0, 2, 3 });
        twice.Perform(Acts(("Drain Punch", 0), ("Ember", 1), ("Pound", 2), ("Pound", 2)));
        Assert.Equal(10 + 30 + 30, twice.Total[0]);
        var once = new SuperContestActing(beauty, new[] { 1, 0, 2, 3 });
        once.Perform(Acts(("Drain Punch", 0), ("Cut", 1), ("Pound", 2), ("Pound", 2)));
        Assert.Equal(10 + 30, once.Total[0]);
    }

    [Fact]
    public void AScoreDoubledByTheTurnBefore()
    {
        var (contest, _) = Stage(mine: Mon("Turtwig", "Swords Dance", "Cut"));
        var acting = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        // Swords Dance: nothing now but the judge's 30, and the next turn's score doubled (ov17_022469DC)
        acting.Perform(Acts(("Swords Dance", 0), ("Pound", 1), ("Pound", 1), ("Pound", 1)));
        Assert.Equal(30, acting.Total[0]);
        Assert.True(acting.IsDoubled(0));
        // ov17_02246228: the act's 30 again, before the voltage and the judge: 60 + the judge's 30
        var log = acting.Perform(Acts(("Cut", 0), ("Cut", 1), ("Cut", 1), ("Cut", 1)));
        Assert.Equal(30, log.Single(e => e.Step == ActingStep.Doubled).Points);
        Assert.Equal(30 + 90, acting.Total[0]);
        Assert.False(acting.IsDoubled(0));
    }

    [Fact]
    public void TheLeastOfTheTurnIsMadeUpTo()
    {
        var (contest, _) = Stage();
        // Pay Day (ov17_02246E68): 10 and the judge's 20 is the least beside three of 30 + 20 or 30 + 30: +3
        var least = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        least.Perform(Acts(("Pay Day", 0), ("Pound", 0), ("Pound", 1), ("Pound", 2)));
        Assert.Equal(10 + 20 + 30, least.Total[0]);
        // Alone with its judge beside three who share one: 10 + 30 against 30 + 10. Level counts as least
        var level = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        level.Perform(Acts(("Pay Day", 0), ("Pound", 1), ("Pound", 1), ("Pound", 1)));
        Assert.Equal(40 + 30, level.Total[0]);
        // Not the least: nothing more
        var (cute, _) = Stage(ContestStat.Cute);
        var more = new SuperContestActing(cute, new[] { 0, 1, 2, 3 });
        more.Perform(Acts(("Pay Day", 0), ("Razor Wind", 1), ("Razor Wind", 1), ("Razor Wind", 1)));
        Assert.Equal(10 + 30, more.Total[0]);
    }

    [Fact]
    public void TheNextTurnsOrderCanBeAskedFor()
    {
        var (contest, _) = Stage();
        // Quick Attack goes first next turn, Vital Throw last (ov17_02246578, ov17_02246668), whatever they scored
        var acting = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        acting.Perform(Acts(("Pound", 0), ("Vital Throw", 1), ("Quick Attack", 2), ("Pound", 0)));
        Assert.Equal(2, acting.Order[0]);
        Assert.Equal(1, acting.Order[3]);

        // Two ask to go first: the later one does, and the earlier is second
        var two = new SuperContestActing(contest, new[] { 0, 1, 2, 3 });
        var log = two.Perform(Acts(("Quick Attack", 0), ("Pound", 1), ("Pound", 2), ("Quick Attack", 0)));
        Assert.Equal(new[] { 3, 0 }, two.Order.Take(2));
        Assert.Equal((1, 0), (log.Last(e => e.Step == ActingStep.Effect).Variant, log.Last(e => e.Step == ActingStep.Effect).Other));

        // Metronome shuffles it (ov17_02246AB0): four draws, each a place among those left. 2, 0, 1, 0: the third of four
        // goes first, then the first of three left, then the second of two, then the last
        var (shuffled, chance) = Stage();
        var metronome = new SuperContestActing(shuffled, new[] { 0, 1, 2, 3 });
        chance.Then(2, 0, 1, 0);
        metronome.Perform(Acts(("Metronome", 0), ("Pound", 1), ("Pound", 2), ("Pound", 2)));
        Assert.Equal(new[] { 2, 0, 3, 1 }, metronome.Order);
    }

    [Fact]
    public void AnOpponentChoosesByTheTable()
    {
        // Ann performs first; her way of thinking is the first. She has Cut (Cool, plain), Pound (Tough, plain) and
        // Thunderbolt (Cool, +2 first). With every voltage and every score at nothing, the rows of place 1 that say yes:
        // has a move of the contest's quality (15): +15 to those; has a plain act (3): +30 to those; has a
        // first-performance move (7): +30 to it. Cut 45, Pound 30, Thunderbolt 45.
        var chance = new Draws();
        var contest = new SuperContest(Mon("Turtwig", "Cut", "Pound"), "Lucas", false, ContestStat.Cool, ContestRank.Normal, ContestMode.Official, chance,
            new[] { Rival("Ann", 0, "Cut", "Pound", "Thunderbolt"), Rival("Bob", 0, "Pound", "Cut"), Rival("Cyd", 0, "Pound", "Cut") });
        var acting = new SuperContestActing(contest, new[] { 1, 0, 2, 3 });
        // Four draws for the moves' ties, three for the judges', then the draw that keeps her from the player's judge
        // (under 230 in 256 at the Normal Rank): Thunderbolt's 9 beats Cut's 1; no judge is preferred, the player's is
        // out, and judge 0's 5 beats judge 1's 2
        chance.Then(1, 0, 9, 0, 5, 2, 7, 0);
        var choices = acting.Choose(new ActingChoice(M("Cut"), 2));
        Assert.Equal(("Thunderbolt", 0), (choices[1].Move.Name, choices[1].Judge));
        Assert.Equal(("Cut", 2), (choices[0].Move.Name, choices[0].Judge));
        // The same draws with a draw of 255 for the last: she doesn't shy, and judge 2's 7 wins
        var again = new SuperContestActing(contest, new[] { 1, 0, 2, 3 });
        chance.Queue.Clear();
        chance.Then(1, 0, 9, 0, 5, 2, 7, 255);
        Assert.Equal(2, again.Choose(new ActingChoice(M("Cut"), 2))[1].Judge);
    }

    [Fact]
    public void TheOpponentsAlwaysChooseSomethingTheyMayShow()
    {
        var chance = new Random(4);
        foreach (var rank in Enum.GetValues<ContestRank>())
            foreach (var quality in Enum.GetValues<ContestStat>())
            {
                var contest = new SuperContest(Mon("Turtwig", "Cut", "Pound"), "Lucas", false, quality, rank, ContestMode.Official, true, chance);
                var acting = new SuperContestActing(contest);
                for (int turn = 0; turn < SuperContestActing.Turns; turn++)
                {
                    var mine = contest.Player.Pokemon.Moves.Select(m => m.Data).First(m => acting.CanUse(0, m));
                    var choices = acting.Choose(new ActingChoice(mine, turn % 3));
                    for (int c = 1; c < 4; c++) Assert.True(acting.CanUse(c, choices[c].Move));
                    acting.Perform(choices);
                }
                Assert.True(acting.IsOver);
                Assert.Equal(acting.Total, contest.Contestants.Select(c => c.ActingScore));
            }
    }

    // ---------------------------------------------------------------- the results

    private static SuperContest Scored(Draws chance, int[] hearts, int[] dance, int[] acting)
    {
        var contest = new SuperContest(Mon("Turtwig", "Cut", "Pound"), "Lucas", false, ContestStat.Cool, ContestRank.Normal, ContestMode.Official, chance,
            new[] { Rival("Ann", 0, "Cut", "Pound"), Rival("Bob", 0, "Cut", "Pound"), Rival("Cyd", 0, "Cut", "Pound") });
        for (int c = 0; c < 4; c++)
        {
            contest.Contestants[c].ConditionHearts = hearts[c];
            contest.Contestants[c].DanceScore = dance[c];
            contest.Contestants[c].ActingScore = acting[c];
        }
        return contest;
    }

    [Fact]
    public void TheThreeRoundsCountAThirdEach()
    {
        var contest = Scored(new Draws(), new[] { 6, 3, 0, 9 }, new[] { 30, 48, 12, 0 }, new[] { 200, 100, 250, 50 });
        // ov17_02251930, each round worth 3333: the best score divides it (3333 / 9 = 370, / 48 = 69, / 250 = 13) and each
        // score takes that many hundredths, rounded: 6 × 370 = 2220 → 22; 30 × 69 = 2070 → 21; 200 × 13 = 2600 → 26
        Assert.Equal(new[] { 22, 11, 0, 33 }, contest.Shares(0));
        Assert.Equal(new[] { 21, 33, 8, 0 }, contest.Shares(1));
        Assert.Equal(new[] { 26, 13, 33, 7 }, contest.Shares(2));
        // ov17_0223EEB8: the acting's first order is the looks and the dance together, best first: 44, 43, 33, 8
        Assert.Equal(new[] { 1, 0, 3, 2 }, contest.ActingOrder());

        contest.Finish();
        // ov17_02251A1C: a round's bar is 64 dots at its best, and each share a whole percent of the best share:
        // 100 × 22 / 33 = 66 → 64 × 66 / 100 = 42; 100 × 21 / 33 = 63 → 40; 100 × 26 / 33 = 78 → 49
        Assert.Equal(new[] { 42, 40, 49 }, contest.Player.Bars);
        Assert.Equal(new[] { 131, 109, 79, 77 }, contest.Contestants.Select(c => c.Total));
        Assert.Equal(new[] { 0, 1, 2, 3 }, contest.Contestants.Select(c => c.Placement));
        Assert.Same(contest.Player, contest.Winner);
    }

    [Fact]
    public void LevelContestantsArePlacedByADrawEach()
    {
        var chance = new Draws();
        var contest = Scored(chance, new[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, 0 });
        // Nobody scored anything (the original divides by nought here; nobody gets anything). The four draws 5, 9, 1, 7
        // place them: the highest first
        chance.Then(5, 9, 1, 7);
        contest.Finish();
        Assert.Equal(new[] { 2, 0, 3, 1 }, contest.Contestants.Select(c => c.Placement));
    }

    [Fact]
    public void APracticeRoundOnlyCountsWhoDidBetter()
    {
        var (contest, _) = Stage(mode: ContestMode.PracticeActing);
        Assert.True(contest.IsPractice);
        Assert.False(contest.HasVisual || contest.HasDance);
        // Contest_Init: a round of acting held alone starts with the player last
        Assert.Equal(new[] { 3, 2, 1, 0 }, contest.ActingOrder());
        int[] scores = { 90, 120, 90, 60 };
        for (int c = 0; c < 4; c++) contest.Contestants[c].ActingScore = scores[c];
        // FieldTask_RunPracticeCompetition: one did better
        contest.Finish();
        Assert.Equal(1, contest.Player.Placement);
    }

    // ---------------------------------------------------------------- what a contest leaves

    [Fact]
    public void AWinGivesTheRibbonTheAccessoryAndFriendship()
    {
        var p = Mon("Turtwig", "Cut", "Pound");
        p.Friendship = 70;
        var fashion = new FashionCase();
        // Contest_EndContest, Contest_CalcFirstTimeVictoryAccessoryReward: the Cute Contest's Normal Rank gives the Pink Barrette
        var won = ContestAwards.Conclude(p, ContestStat.Cute, ContestRank.Normal, ContestMode.Official, 0, fashion);
        Assert.Equal((true, "Cute Ribbon", "Pink Barrette"), (won.Won, won.NewRibbon, won.Prize));
        Assert.Contains("RIBBON_CUTE", p.Ribbons);
        Assert.Equal(1, fashion.Count(SuperContestDatabase.Accessory("Pink Barrette")));
        // FRIENDSHIP_EVENT_CONTEST_WIN: +3 under 100
        Assert.Equal(73, p.Friendship);

        // Won again: the ribbon is worn already and the barrette, one of a kind, doesn't fit twice
        var again = ContestAwards.Conclude(p, ContestStat.Cute, ContestRank.Normal, ContestMode.Official, 0, fashion);
        Assert.Equal((true, null, null), (again.Won, again.NewRibbon, again.Prize));
        // Second place and a practice round leave nothing
        var second = ContestAwards.Conclude(p, ContestStat.Cool, ContestRank.Normal, ContestMode.Official, 1, fashion);
        var practice = ContestAwards.Conclude(p, ContestStat.Cool, ContestRank.Normal, ContestMode.PracticeDance, 0, fashion);
        Assert.False(second.Won || practice.Won);
        Assert.DoesNotContain("RIBBON_COOL", p.Ribbons);

        Assert.Equal("Gold Pedestal", ContestRules.Prize(ContestStat.Cool, ContestRank.Master));
        Assert.Equal("Heroic Headband", ContestRules.Prize(ContestStat.Tough, ContestRank.Ultra));
        foreach (var quality in Enum.GetValues<ContestStat>())
            foreach (var rank in Enum.GetValues<ContestRank>())
                Assert.True(SuperContestDatabase.Accessory(ContestRules.Prize(quality, rank)) >= SuperContestDatabase.FirstUniqueAccessory);
    }

    [Fact]
    public void TheFashionCaseKeepsNineOfAKindAndOneOfTheRest()
    {
        var fashion = new FashionCase();
        Assert.True(fashion.IsEmpty);
        int fluff = SuperContestDatabase.Accessory("White Fluff"), parasol = SuperContestDatabase.Accessory("Colored Parasol");
        // FashionCase_CanFitAccessoryCount
        Assert.True(fashion.Add(fluff, 9));
        Assert.False(fashion.Add(fluff));
        Assert.True(fashion.Add(parasol));
        Assert.False(fashion.Add(parasol));
        Assert.False(fashion.Add("No Such Thing"));
        Assert.Equal(new[] { (fluff, 9), (parasol, 1) }, fashion.Held);
        Assert.Equal(10, fashion.Total);

        // Saved by name, read back, and what a save doesn't know is left out
        var save = new SaveData { FashionCase = fashion.ToSave() };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        read.FashionCase!["Mystery Hat"] = 3;
        var back = new FashionCase();
        back.Load(read.FashionCase);
        Assert.Equal(fashion.Held, back.Held);
        back.Load(null);
        Assert.True(back.IsEmpty);
    }

    [Fact]
    public void RibbonsAreSavedAndCopied()
    {
        var p = Mon("Turtwig");
        p.Ribbons.Add("RIBBON_SMART");
        p.Ribbons.Add("RIBBON_SMART_GREAT");
        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal(new[] { "RIBBON_SMART", "RIBBON_SMART_GREAT" }, back.Ribbons);
        var copy = Mon("Turtwig");
        copy.CopyStateFrom(p);
        Assert.Equal(p.Ribbons, copy.Ribbons);
        Assert.NotSame(p.Ribbons, p.Clone().Ribbons);
        // One with none writes nothing
        Assert.Null(SavedPokemonData.FromPokemon(Mon("Turtwig")).Ribbons);
    }

    // ---------------------------------------------------------------- the screen

    // Plays a contest through its screen as a player would: an accessory put on, the dance left to itself or stepped on
    // the beat, the first move that may be shown to each judge in turn
    private static SuperContestScreen Play(ContestMode mode, FashionCase fashion, Pokemon mine, Random chance, bool dances = false)
    {
        var screen = new SuperContestScreen();
        screen.Begin(mine, "Lucas", false, ContestStat.Cool, ContestRank.Normal, mode, false, fashion, chance);
        Assert.Equal(SuperContestPhase.Lineup, screen.Phase);
        for (int i = 0; i < 400000 && screen.IsActive; i++)
        {
            switch (screen.Phase)
            {
                case SuperContestPhase.Dressing:
                    Assert.Equal(5, screen.Limit);
                    if (screen.InCase.Count > 0 && screen.Worn.Count == 0)
                    {
                        screen.PressConfirm();
                        Assert.Single(screen.Worn);
                    }
                    screen.PressStart();
                    break;
                case SuperContestPhase.DanceLead when screen.Dance != null:
                    screen.Advance(1f / 30f);
                    break;
                case SuperContestPhase.Dance:
                    // On the beat when leading: the frame before a whole beat of 15 frames
                    if (dances && screen.Dance!.Lead == 0 && screen.Dance.Frame % 15 == 14) screen.PressStep(DanceStep.Jump);
                    screen.Advance(1f / 30f);
                    break;
                case SuperContestPhase.ActingMove:
                {
                    var acting = screen.Acting!;
                    while (!acting.CanUse(0, mine.Moves[screen.MoveCursor].Data)) screen.Move(0, 1);
                    screen.PressConfirm();
                    Assert.Equal(SuperContestPhase.ActingJudge, screen.Phase);
                    for (int j = 0; j < acting.Turn % 3; j++) screen.Move(1, 0);
                    screen.PressConfirm();
                    break;
                }
                default:
                    screen.PressConfirm();
                    break;
            }
        }
        Assert.False(screen.IsActive);
        Assert.Equal(SuperContestPhase.Done, screen.Phase);
        return screen;
    }

    [Fact]
    public void AWholeContestPlaysThroughItsScreen()
    {
        var fashion = new FashionCase();
        fashion.Add("Glitter Powder");
        var mine = Mon("Turtwig", "Cut", "Double Kick", "Thunderbolt", "Pound");
        (mine.Cool, mine.Tough, mine.Beauty, mine.Sheen) = (255, 255, 255, 255);
        var screen = Play(ContestMode.Official, fashion, mine, new Random(9), dances: true);
        var contest = screen.Contest!;
        Assert.True(contest.IsOver);
        Assert.Equal(new[] { 0, 1, 2, 3 }, contest.Contestants.Select(c => c.Placement).OrderBy(p => p));
        // The looks: 255 + (255 + 255 + 255) / 2 = 637, all eight hearts; the one Glitter Powder is worn
        Assert.Equal(8, contest.Player.ConditionHearts);
        Assert.Equal(new[] { SuperContestDatabase.Accessory("Glitter Powder") }, contest.Player.Accessories);
        // The dance was danced and the acting acted
        Assert.True(screen.Dance!.IsOver && screen.Acting!.IsOver);
        Assert.True(contest.Player.DanceScore > 0);
        Assert.True(contest.Player.ActingScore >= 4 * 30);
        var outcome = screen.Outcome!;
        Assert.Equal(contest.Player.Placement, outcome.Placement);
        Assert.Equal(outcome.Won, mine.Ribbons.Contains("RIBBON_COOL"));
        Assert.Equal(outcome.Won ? "Red Barrette" : null, outcome.Prize);
    }

    [Theory]
    [InlineData(ContestMode.PracticeVisual)]
    [InlineData(ContestMode.PracticeDance)]
    [InlineData(ContestMode.PracticeActing)]
    public void APracticeRoundPlaysThroughItsScreenAndLeavesNothing(ContestMode mode)
    {
        var fashion = new FashionCase();
        var mine = Mon("Turtwig", "Cut", "Pound");
        var screen = Play(mode, fashion, mine, new Random(2));
        // Only the round asked for was held
        Assert.Equal(mode == ContestMode.PracticeDance, screen.Dance is { IsOver: true });
        Assert.Equal(mode == ContestMode.PracticeActing, screen.Acting is { IsOver: true });
        Assert.Equal(mode == ContestMode.PracticeVisual, screen.Dance == null && screen.Acting == null);
        Assert.InRange(screen.Outcome!.Placement, 0, 3);
        Assert.False(screen.Outcome.Won);
        Assert.Empty(mine.Ribbons);
        Assert.True(fashion.IsEmpty);
    }

    [Fact]
    public void TheDressingUpEndsWhenItsSixtySecondsAreUp()
    {
        var fashion = new FashionCase();
        fashion.Add("White Fluff", 9);
        fashion.Add("Glitter Powder");
        var screen = new SuperContestScreen();
        screen.Begin(Mon("Turtwig", "Cut", "Pound"), "Lucas", false, ContestStat.Cool, ContestRank.Normal, ContestMode.PracticeVisual, false, fashion, new Random(1));
        while (screen.Phase != SuperContestPhase.Dressing) screen.PressConfirm();
        Assert.Equal(60f, screen.TimeLeft);
        Assert.Equal(new[] { 9, 1 }, screen.InCase.Select(h => h.Count));

        // Five at the Normal Rank, and no more; what is worn leaves the case's count
        for (int i = 0; i < 7; i++) screen.PressConfirm();
        Assert.Equal(5, screen.Worn.Count);
        Assert.Equal(4, screen.InCase[0].Count);
        // The last comes off again, and the other kind goes on in its place
        screen.PressCancel();
        screen.Move(1, 0);
        screen.PressConfirm();
        screen.PressConfirm();
        Assert.Equal(5, screen.Worn.Count);
        Assert.Equal(SuperContestDatabase.Accessory("Glitter Powder"), screen.Worn[^1]);
        Assert.Equal(0, screen.InCase[1].Count);
        // The case is not emptied by dressing up
        Assert.Equal(10, fashion.Total);

        screen.Advance(59f);
        Assert.Equal(SuperContestPhase.Dressing, screen.Phase);
        screen.Advance(1.5f);
        Assert.Equal(SuperContestPhase.Looks, screen.Phase);
        Assert.Equal(5, screen.Contest!.Player.Accessories.Count);
    }

    // ---------------------------------------------------------------- the Contest Hall's scripts

    private static HeadlessScriptHost Run(string name, HeadlessScriptHost host, NPC? subject = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, "ContestHallLobby")!, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheCommandsCheckEnterAndTellWhatAWinGave()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script Check
              supercontest mode official
              supercontest rank great
              supercontest category smart
              choosepokemon
              supercontest check
              end
            script Enter
              supercontest enter
              end
            script Ribbon
              supercontest ribbon
              end
            script Prize
              supercontest prize
              text "{contestant} won the {ribbon} and the {accessory}."
              end
            script Give
              giveaccessory "Sparks" 2
              end
            script Crown
              giveaccessory "Crown"
              end
            """));
        (HeadlessScriptHost Host, ScriptRunner Runner) Game(Action<HeadlessScriptHost> set)
        {
            var host = new HeadlessScriptHost();
            host.Party.Add(Mon("Turtwig", "Cut", "Pound"));
            host.Party.Add(Mon("Bidoof", "Tackle"));
            set(host);
            return (host, new ScriptRunner(library, host));
        }
        int Do(ScriptRunner runner, string script)
        {
            runner.Start(library.Find(script, "test")!, null);
            runner.RunToEnd();
            return runner.Result;
        }
        int Check(Action<HeadlessScriptHost> set) => Do(Game(set).Runner, "Check");

        // The Great Rank asks for the Smart Ribbon (3); nobody chosen (1); one move only (4); fainted (2)
        Assert.Equal(3, Check(_ => { }));
        Assert.Equal(1, Check(h => h.PokemonChoice = 255));
        Assert.Equal(4, Check(h => { h.PokemonChoice = 1; h.Party.Members[1].Ribbons.Add("RIBBON_SMART"); }));
        Assert.Equal(2, Check(h => { h.Party.Members[0].CurrentHP = 0; h.Party.Members[0].Ribbons.Add("RIBBON_SMART"); }));

        var (won, winner) = Game(h => h.Party.Members[0].Ribbons.Add("RIBBON_SMART"));
        Assert.Equal((0, 0, 1, 1), (Do(winner, "Check"), Do(winner, "Enter"), Do(winner, "Ribbon"), Do(winner, "Prize")));
        Assert.Contains("supercontest Great Smart Official Turtwig 0", won.Log);
        Assert.Contains(won.Transcript, l => l.Text == "Turtwig won the Smart Ribbon Great and the Green Balloons.");
        Assert.Contains("RIBBON_SMART_GREAT", won.Party.Members[0].Ribbons);

        var (_, loser) = Game(h => { h.Party.Members[0].Ribbons.Add("RIBBON_SMART"); h.ContestPlacement = 2; });
        Assert.Equal((0, 2, 0, 0), (Do(loser, "Check"), Do(loser, "Enter"), Do(loser, "Ribbon"), Do(loser, "Prize")));

        // Common_GiveAccessory: into the case with a line, when it fits; a second Crown doesn't
        var (given, giver) = Game(_ => { });
        Assert.Equal((1, 1, 0), (Do(giver, "Give"), Do(giver, "Crown"), Do(giver, "Crown")));
        Assert.Equal((2, 1), (given.FashionCase.Count(SuperContestDatabase.Accessory("Sparks")), given.FashionCase.Count(SuperContestDatabase.Accessory("Crown"))));
        Assert.Contains(given.Transcript, l => l.Text.Contains("received 2 × Sparks"));
        Assert.Equal(1, given.Transcript.Count(l => l.Text.Contains("the Crown")));
    }

    [Fact]
    public void AnAccessoryThatDoesNotExistIsAProblemOfTheScript()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script Give
              giveaccessory "Glitter Poweder"
              end
            """));
        Assert.Contains(library.Problems(), p => p.Contains("Glitter Poweder"));
        Assert.DoesNotContain(ScriptLibrary.Default.Problems(), p => p.Contains("accessory"));
    }

    [Fact]
    public void TheReceptionistEntersAPokemonAndHandsOverThePrize()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Turtwig", "Cut", "Pound"));
        // ENTER, the Normal Rank, the Cute Contest; the first of the team
        foreach (int answer in new[] { 0, 0, 2 }) host.Answers.Enqueue(answer);
        Run("ReceptionistOfficial", host);
        Assert.Contains("supercontest Normal Cute Official Turtwig 0", host.Log);
        Assert.Contains("RIBBON_CUTE", host.Party.Members[0].Ribbons);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Turtwig has been entered"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("received the Pink Barrette"));
        Assert.Equal(1, host.FashionCase.Count(SuperContestDatabase.Accessory("Pink Barrette")));

        // Second place: thanks, and no prize
        var second = new HeadlessScriptHost { ContestPlacement = 1 };
        second.Party.Add(Mon("Turtwig", "Cut", "Pound"));
        foreach (int answer in new[] { 0, 0, 0 }) second.Answers.Enqueue(answer);
        Run("ReceptionistOfficial", second);
        Assert.DoesNotContain(second.Transcript, l => l.Text.Contains("prize"));
        Assert.Contains(second.Transcript, l => l.Text.Contains("Thank you for taking part"));
        Assert.Empty(second.Party.Members[0].Ribbons);

        // Backing out of the team's screen, and saying yes to cancelling
        var out1 = new HeadlessScriptHost { PokemonChoice = 255 };
        out1.Party.Add(Mon("Turtwig", "Cut", "Pound"));
        foreach (int answer in new[] { 0, 1, 4, 0 }) out1.Answers.Enqueue(answer);
        Run("ReceptionistOfficial", out1);
        Assert.DoesNotContain(out1.Log, l => l.StartsWith("supercontest"));

        // INFO, the ranks: told, and the talk ends; EXIT leaves with a word
        var info = new HeadlessScriptHost();
        foreach (int answer in new[] { 1, 2 }) info.Answers.Enqueue(answer);
        Run("ReceptionistOfficial", info);
        Assert.Contains(info.Transcript, l => l.Text.Contains("four ranks"));
        Assert.Contains(info.Transcript, l => l.Text.Contains("hear more"));
        var exit = new HeadlessScriptHost();
        exit.Answers.Enqueue(2);
        Assert.Contains(Run("ReceptionistOfficial", exit).Transcript, l => l.Text.Contains("on our stage"));
    }

    [Fact]
    public void ThePracticeCounterHoldsOneRoundAndAsksAgain()
    {
        var host = new HeadlessScriptHost { ContestPlacement = 3 };
        host.Party.Add(Mon("Turtwig", "Cut", "Pound"));
        // PRACTICE, the dance (which asks no quality); once more, then no more
        foreach (int answer in new[] { 0, 1, 1, 0 }) host.Answers.Enqueue(answer);
        Run("ReceptionistPractice", host);
        Assert.Equal(2, host.Log.Count(l => l == "supercontest Normal Cool PracticeDance Turtwig 3"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("getting used to"));
        Assert.Empty(host.Party.Members[0].Ribbons);

        // The acting, of the Smart Contest; a good round, and no more
        var acting = new HeadlessScriptHost();
        acting.Party.Add(Mon("Turtwig", "Cut", "Pound"));
        foreach (int answer in new[] { 0, 2, 3, 0 }) acting.Answers.Enqueue(answer);
        Run("ReceptionistPractice", acting);
        Assert.Contains("supercontest Normal Smart PracticeActing Turtwig 0", acting.Log);
        Assert.Contains(acting.Transcript, l => l.Text.Contains("went nicely"));

        Assert.Contains(Run("ReceptionistLink", new HeadlessScriptHost()).Transcript, l => l.Text.Contains("Link Contests"));
    }

    [Fact]
    public void KeiraGivesTheGlitterPowderInTheLobbysFirstScene()
    {
        var map = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "ContestHallLobby.json")).ToMap();
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (16, 13), PlayerFacing = Direction.Up };
        Run("OnEnter", host);
        // Common_GiveAccessory ACCESSORY_GLITTER_POWDER, 1
        Assert.Equal(1, host.FashionCase.Count(SuperContestDatabase.Accessory("Glitter Powder")));
        Assert.Contains(host.Transcript, l => l.Text.Contains("received the Glitter Powder"));
        Assert.True(host.Story.Has(GameEngine.ContestHallVisitedFlag));
        // The three counters each have their receptionist's script
        foreach (string who in new[] { "receptionist_official", "receptionist_practice", "receptionist_link" })
            Assert.NotNull(ScriptLibrary.Default.Find(map.Everyone.Single(n => n.Key == who).Script!, "ContestHallLobby"));
    }
}
