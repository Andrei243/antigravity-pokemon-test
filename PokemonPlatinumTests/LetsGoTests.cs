using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumTests.Golden;

namespace PokemonPlatinumTests;

/// <summary>
/// Let's Go and the raids on Team Star's gates (plan 34 · PD5): a lead sent at a wild Pokémon fights it headless as AUTO
/// would, its record replays the battle to the same log, and the lead keeps the EXP; and a raid's clock, the field's own
/// ticks, ends a raid on a base's gate that hasn't felled its goal.
/// </summary>
public class LetsGoTests
{
    private static Pokemon Mon(string species, int level, int seed) => new(PokemonDatabase.Get(species)!, level, new Random(seed));

    private static LetsGoTerms Terms(uint seed) => new() { Seed = seed, Rules = Ruleset.Modern, PlayerName = "Test" };

    [Fact]
    public void ALetsGoBattleReplaysFromItsRecordAndGivesItsExp()
    {
        var lead = Mon("Fuecoco", 20, 1);
        var wild = Mon("Lechonk", 6, 2);
        int expBefore = lead.CurrentExp;
        var battle = LetsGoBattle.Fight(lead, wild, Terms(0x5EED_1234));

        Assert.Equal(LetsGoEnd.Felled, battle.End);
        Assert.True(battle.Exp > 0, "the lead was given no EXP");
        // The EXP is the team's own Pokémon's, as the log says
        Assert.Equal(expBefore + battle.Exp, lead.CurrentExp);
        Assert.True(wild.IsFainted);
        // Never a ball, never the bag: nothing of the log is a capture or an item
        Assert.DoesNotContain(battle.Log, e => e is Caught);

        // The record: both sides as they began, the seed and the answers, fought again to the same log
        var replayed = battle.File.Replay();
        Assert.Equal(BattleText.Written(battle.Core), BattleText.Written(replayed));
        Assert.Equal(battle.Core.Result, replayed.Result);
        // and the record has the answers of every turn (the last, cut short by the faint, among them), each a move chosen
        // from outside as AUTO chooses
        Assert.Equal(battle.Turns + 1, battle.File.Answers.Count);
        Assert.All(battle.File.Answers, turn => Assert.All(turn, c => Assert.Equal(ChoiceKind.Fight, c.ToChoice().Kind)));
    }

    [Fact]
    public void TheSameSeedFightsTheSameBattleAndAnotherMayNot()
    {
        string Fought(uint seed)
        {
            var battle = LetsGoBattle.Fight(Mon("Fuecoco", 20, 1), Mon("Lechonk", 6, 2), Terms(seed));
            return string.Join("\n", BattleText.Written(battle.Core));
        }
        Assert.Equal(Fought(7), Fought(7));
    }

    [Fact]
    public void AHigherLevelOrAShinyIsNoLetsGoBattleAndALeadThatCantFightIsNone()
    {
        var lead = Mon("Fuecoco", 10, 1);
        Assert.True(LetsGoBattle.MayFight(lead, Mon("Lechonk", 10, 2)));
        Assert.False(LetsGoBattle.MayFight(lead, Mon("Lechonk", 11, 2)));
        var shiny = Mon("Lechonk", 5, 2);
        shiny.IsShiny = true;
        Assert.False(LetsGoBattle.MayFight(lead, shiny));
        lead.CurrentHP = 0;
        Assert.False(LetsGoBattle.MayFight(lead, Mon("Lechonk", 5, 2)));
    }

    [Fact]
    public void ALeadOutmatchedFaintsAndTheWildPokemonStays()
    {
        // A lead of the lowest level against one far stronger (a raid sends whoever comes next, whatever the level)
        var lead = Mon("Sprigatito", 2, 3);
        var wild = Mon("Makuhita", 40, 4);
        var battle = LetsGoBattle.Fight(lead, wild, Terms(99));
        Assert.Equal(LetsGoEnd.Fainted, battle.End);
        Assert.True(lead.IsFainted);
        Assert.False(wild.IsFainted);
        Assert.Equal(0, battle.Exp);
    }

    private static StarRaid FlatRaid(string key) => new(StarBases.Get(key)!, OpenPos.FromMetres(0, 12, 0), (_, _, _) => 12.0);

    [Fact]
    public void ABasesClockEndsARaidOnTheGate()
    {
        var raid = FlatRaid("giacomo");
        Assert.All(raid.Standing, foe => Assert.NotNull(foe));
        Assert.Equal(StarRaid.Seconds, raid.SecondsLeft);
        // A few felled, not the goal
        for (int i = 0; i < 4; i++) raid.Fell(raid.Nearest(raid.Middle)!);
        Assert.False(raid.Over);
        // The clock runs to a second before its end: the raid goes on
        raid.Run(StarRaid.Seconds * OpenClock.TicksPerSecond - OpenClock.TicksPerSecond);
        Assert.False(raid.Over);
        Assert.Equal(1, raid.SecondsLeft);
        // and to its end: lost on the gate, and nothing more is felled or filled
        raid.Run(OpenClock.TicksPerSecond);
        Assert.True(raid.Lost);
        Assert.False(raid.Won);
        int felled = raid.Felled;
        raid.Fell(raid.Standing.First(f => f != null)!);
        Assert.Equal(felled, raid.Felled);
        Assert.Equal(StarBases.Get("giacomo")!.LostScript, "GiacomoRaidLost");
    }

    [Fact]
    public void ARaidFelledToItsGoalBeforeTheClockIsWonAndItsSpotsFillAgain()
    {
        var raid = FlatRaid("eri");
        while (!raid.Over)
        {
            var foe = raid.Nearest(raid.Middle);
            if (foe == null)
            {
                raid.Tick();
                continue;
            }
            Assert.Contains(foe.Species, raid.Base.Species);
            Assert.InRange(foe.Level, raid.Base.MinLevel, raid.Base.MaxLevel);
            raid.Fell(foe);
        }
        Assert.True(raid.Won);
        Assert.Equal(StarRaid.Goal, raid.Felled);
        Assert.True(raid.TicksLeft > 0);
        // The same base's raid stands the same Pokémon in the same places every time
        Assert.Equal(FlatRaid("eri").Standing.Select(f => (f!.Species, f.Level, f.Pos)), FlatRaid("eri").Standing.Select(f => (f!.Species, f.Level, f.Pos)));
    }

    [Fact]
    public void TheThreeLeadsAreSentInTurn()
    {
        var party = new Party();
        party.Add(Mon("Fuecoco", 20, 1));
        party.Add(Mon("Quaxly", 20, 2));
        var fainted = Mon("Sprigatito", 20, 3);
        fainted.CurrentHP = 0;
        party.Add(fainted);
        party.Add(Mon("Lechonk", 20, 4));
        party.Add(Mon("Pawmi", 20, 5));
        var raid = FlatRaid("mela");
        // The first three able: the first, the second and the fourth (the third has fainted), in turn
        Assert.Equal(new[] { 0, 1, 3, 0, 1 }, Enumerable.Range(0, 5).Select(_ => raid.NextLead(party)));
    }

    [Fact]
    public void EveryBasesSpeciesAndBossAreTheGames()
    {
        Assert.Equal(new[] { "giacomo", "mela", "atticus", "ortega", "eri" }, StarBases.All.Select(b => b.Key));
        foreach (var b in StarBases.All)
        {
            Assert.All(b.Species, s => Assert.NotNull(PokemonDatabase.Get(s)));
            var boss = TrainerDatabase.Get(b.Boss, RegionDatabase.Paldea);
            Assert.NotNull(boss);
            // The boss's last Pokémon is the Starmobile, and the battle is fought on a base's stage
            Assert.Equal("Revavroom", boss!.Party[^1].Species);
            Assert.True(new Trainer { Format = boss.Format }.AtAStarBase);
        }
    }
}
