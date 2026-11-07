using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Peak training, TR3 (plan 36; <c>docs/mechanics/rulings.md</c>, "Peak training and the training tools"): the head coach's
/// <c>train peak</c> on <see cref="HyperTraining"/>'s rule, refused under level 50, on an IV of 31 and on a stat at its peak,
/// a Bottle Cap taken once for one stat and a Gold Bottle Cap for all six, and the PEAK the judge and SKILLS read.
/// </summary>
[Collection("Shared clocks and dice")]
public class PeakTrainingTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Hall = GameEngine.SunyshoreTrainingHall;

    private static ItemData Cap => ItemDatabase.Get(HyperTraining.BottleCap)!;
    private static ItemData GoldCap => ItemDatabase.Get(HyperTraining.GoldBottleCap)!;

    /// <summary>A Bidoof of the level given whose IVs are all ten (Meagre on the judge's words), Speed's the one asked.</summary>
    private static Pokemon Bidoof(int level = 50, int speedIv = 10)
    {
        var p = new Pokemon(PokemonDatabase.Get("Bidoof")!, level)
        {
            IvHP = 10, IvAttack = 10, IvDefense = 10, IvSpAttack = 10, IvSpDefense = 10, IvSpeed = speedIv
        };
        p.ReckonStats();
        return p;
    }

    private static StoryState Champion()
    {
        var story = new StoryState();
        story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        return story;
    }

    /// <summary>The coach spoken to by a Champion: PEAK TRAINING, the first of the team, and then the answers given.</summary>
    private static HeadlessScriptHost Coach(Pokemon trainee, Inventory bag, params int[] answers)
    {
        var map = MapDatabase.Get(Hall);
        var party = new Party();
        party.Add(trainee);
        var host = new HeadlessScriptHost(Champion(), party, bag) { Map = map };
        host.Answers.Enqueue(1);
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("TrainingCoach", Hall)!, map.NPCs.Single(n => n.Key == "head_coach"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static Inventory Caps(int bottle, int gold)
    {
        var bag = new Inventory();
        if (bottle > 0) bag.AddItem(Cap, bottle);
        if (gold > 0) bag.AddItem(GoldCap, gold);
        return bag;
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    /// <summary><c>train peak check</c> and the reason it gives, on a Pokémon and a bag of our own.</summary>
    private static int Check(Pokemon? p, Inventory bag)
    {
        var party = new Party();
        if (p != null) party.Add(p);
        var host = new HeadlessScriptHost(Champion(), party, bag);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(ScriptParser.Parse("t", "script T\n  choosepokemon\n  train peak check\n  end")[0], null);
        runner.RunToEnd();
        return runner.Result;
    }

    [Fact]
    public void PeakTrainingIsRefusedUnderLevelFiftyOnAnIvOfThirtyOneAndOnAStatAtItsPeak()
    {
        // Under level 50: the check says so, and the rule trains nothing
        var young = Bidoof(level: 49);
        Assert.Equal((int)ScriptRunner.PeakReadiness.TooLow, Check(young, Caps(1, 1)));
        Assert.False(HyperTraining.Train(Cap, young, StatType.Speed));
        Assert.Equal(0, young.HyperTrained);

        // A stat whose IV is 31 is no choice: the question offers only the other five
        var quick = Bidoof(speedIv: 31);
        Assert.Equal((int)ScriptRunner.PeakReadiness.Ready, Check(quick, Caps(1, 0)));
        Assert.False(HyperTraining.CanTrain(quick, StatType.Speed));
        var host = Coach(quick, Caps(1, 0), 5);
        var question = host.Asked.Last(a => a.Question.Contains("peak"));
        Assert.Equal("NEVER MIND", question.Answer);
        Assert.Equal(0, quick.HyperTrained);

        // A stat brought to its peak is no choice either, and with all six there the check says so
        var peaked = Bidoof();
        Assert.True(HyperTraining.Train(GoldCap, peaked));
        Assert.Equal(HyperTraining.All, peaked.HyperTrained);
        Assert.Equal((int)ScriptRunner.PeakReadiness.AtItsPeak, Check(peaked, Caps(1, 1)));
        var bag = Caps(1, 1);
        Coach(peaked, bag);
        Assert.Equal(1, bag.GetQuantity(Cap));
        Assert.Equal(1, bag.GetQuantity(GoldCap));

        // No cap at all, and nobody chosen
        Assert.Equal((int)ScriptRunner.PeakReadiness.NoCap, Check(Bidoof(), new Inventory()));
        Assert.Equal((int)ScriptRunner.PeakReadiness.Nobody, Check(null, Caps(1, 0)));
        var egg = Bidoof();
        egg.IsEgg = true;
        Assert.Equal((int)ScriptRunner.PeakReadiness.Nobody, Check(egg, Caps(1, 0)));
    }

    [Fact]
    public void ABottleCapBringsTheStatChosenToItsPeakAndIsTakenOnce()
    {
        var bidoof = Bidoof();
        int speed = bidoof.Speed;
        var bag = Caps(2, 0);
        // The question lists the six stats with their judge's word, then NEVER MIND: SPEED is the sixth
        var host = Coach(bidoof, bag, 5);
        var question = host.Asked.Last(a => a.Question.Contains("peak"));
        Assert.Equal(Text.Of("training.peak.row", ("stat", Words.Stat(StatType.Speed)), ("word", Text.Of("boxTeams.judge.grade.Meagre"))), question.Answer);
        Assert.True(bidoof.IsHyperTrained(StatType.Speed));
        Assert.Equal(HyperTraining.Bit(StatType.Speed), bidoof.HyperTrained);
        // Its IV stays its own; its stat grows as if it were 31
        Assert.Equal(10, bidoof.IvSpeed);
        Assert.True(bidoof.Speed > speed);
        Assert.Equal(1, bag.GetQuantity(Cap));
        Assert.Contains("is at its peak now", Said(host));

        // NEVER MIND takes nothing
        var other = Bidoof();
        var kept = Caps(1, 0);
        Coach(other, kept, 6);
        Assert.Equal(0, other.HyperTrained);
        Assert.Equal(1, kept.GetQuantity(Cap));
    }

    [Fact]
    public void AGoldBottleCapBringsAllSixToTheirPeak()
    {
        var bidoof = Bidoof(speedIv: 31);
        var bag = Caps(0, 1);
        // With a Gold Bottle Cap alone the question is ALL SIX and NEVER MIND
        var host = Coach(bidoof, bag, 0);
        Assert.StartsWith("ALL SIX", host.Asked.Last(a => a.Question.Contains("peak")).Answer);
        // Every stat it could be trained in is (Speed's IV was 31 already, so its bit stays clear)
        Assert.Equal(HyperTraining.All & ~HyperTraining.Bit(StatType.Speed), bidoof.HyperTrained);
        Assert.Equal(0, bag.GetQuantity(GoldCap));
        Assert.Contains("Every one of its stats", Said(host));
    }

    [Fact]
    public void BeforeAHallOfFameTheCoachOffersNoPeakTraining()
    {
        var map = MapDatabase.Get(Hall);
        var party = new Party();
        party.Add(Bidoof());
        var host = new HeadlessScriptHost(null, party, Caps(1, 1)) { Map = map };
        host.Answers.Enqueue(3);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("TrainingCoach", Hall)!, map.NPCs.Single(n => n.Key == "head_coach"));
        runner.RunToEnd();
        Assert.DoesNotContain(host.Asked, a => a.Answer == "PEAK TRAINING");
        Assert.Equal(0, party.Members[0].HyperTrained);
    }

    [Fact]
    public void ThePeakSurvivesASaveAndTheJudgeAndSkillsReadIt()
    {
        var bidoof = Bidoof();
        Assert.True(HyperTraining.Train(Cap, bidoof, StatType.Speed));
        var back = SavedPokemonData.FromPokemon(bidoof).ToPokemon();
        Assert.True(back.IsHyperTrained(StatType.Speed));
        Assert.False(back.IsHyperTrained(StatType.Attack));
        Assert.Equal(bidoof.Speed, back.Speed);
        Assert.Equal("PEAK", Text.Of("summary.peak"));
    }
}
