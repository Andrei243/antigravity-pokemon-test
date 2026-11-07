using System;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The training hall, TR2 (plan 36): Sinnoh's hall behind Sunyshore City's alley and the way in and out, the coach's
/// script on its ways through, a drill played through the screen giving the Pokémon its effort, the reset, the drills'
/// prizes, the records through a save, and <c>train open</c> before and after a Hall of Fame.
/// </summary>
[Collection("Shared clocks and dice")]
public class TrainingHallTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Hall = GameEngine.SunyshoreTrainingHall;

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    private static Pokemon Bidoof() => new(PokemonDatabase.Get("Bidoof")!, 50);

    /// <summary>A seed whose throw the autopilot plays to gold at the given skill.</summary>
    private static int GoldSeed(StatType stat, float knack = 0.9f)
    {
        int seed = 1;
        while (TrainingDrill.Create(stat, seed).PlayItself(knack) != DrillGrade.Gold) seed++;
        return seed;
    }

    // ---------------------------------------------------------------- the hall and its way in

    [Fact]
    public void TheHallsWayInIsOpenFromTheStartAndLeadsInAndOut()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        var way = sinnoh.Warps.Single(w => w.TargetMap == Hall);
        Assert.Null(way.OpenedBy);
        Assert.Equal((879, 741), (way.SourceX, way.SourceY));
        sinnoh.ApplyDoors(_ => false);
        Assert.False(sinnoh.IsSolid(way.SourceX, way.SourceY));
        // The alley's tile below it, and a step up from it into the way in
        Assert.True(sinnoh.IsWalkable(way.SourceX, way.SourceY + 1));
        var step = FieldMovement.Step(sinnoh, way.SourceX, way.SourceY + 1, Direction.Up, new Walker(Height: sinnoh.HeightAt(way.SourceX, way.SourceY + 1)));
        Assert.NotEqual(StepKind.Blocked, step.Kind);
        // Sunyshore City's unused door 7 stays locked: the hall stands for it
        Assert.True(sinnoh.IsSolid(832, 751));
        Assert.DoesNotContain(sinnoh.Warps, w => w.SourceX == 832 && w.SourceY == 751 && w.TargetMap == Hall);

        var room = MapDatabase.Get(Hall);
        var back = room.Warps.Single();
        Assert.Equal("Sinnoh", back.TargetMap);
        Assert.Equal((way.SourceX, way.SourceY + 1), (back.TargetX, back.TargetY));
        Assert.True(room.IsWalkable(way.TargetX, way.TargetY));
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(Hall)!.Id);
        // Its billboard is read from the alley's other column
        Assert.True(sinnoh.IsWalkable(880, 741));
        Assert.True(sinnoh.TileScripts.ContainsKey((880, 740)));
    }

    [Fact]
    public void AWalkFromSunyshoreCityReachesTheHallAndItsCoach()
    {
        var walked = WorldWalk.From(MapDatabase.Get, new[] { new MapSpot("Sinnoh", 860, 786, Direction.Down) });
        var room = walked.Keys.SingleOrDefault(m => m.Name == Hall);
        Assert.NotNull(room);
        var coach = room!.NPCs.Single(n => n.Key == "head_coach");
        // The coach is spoken to across the counter, from the tile two below him
        Assert.Contains((coach.GridX, coach.GridY + 2), walked[room]);
    }

    [Fact]
    public void TheHallHasItsTargetsMatsCounterAndPeople()
    {
        var room = MapDatabase.Get(Hall);
        Assert.Equal(3, room.Props.Count(p => p.Type == PropType.DrillTarget));
        Assert.Contains(room.Props, p => p.Type == PropType.TrainingMat);
        Assert.Contains(room.Props, p => p.Type == PropType.Counter);
        Assert.False(new Prop { Type = PropType.TrainingMat }.IsSolid);
        Assert.False(new Prop { Type = PropType.DrillTarget }.IsSolid);
        // and since plan 36 · TR9 the tutor of the later games' moves between them behind the counter
        Assert.Equal(new[] { "TrainingCoach", "TrainingClerk", "TrainingTutor" }, room.NPCs.Select(n => n.Script).ToArray());
        // The mat is walked on
        var mat = room.Props.Single(p => p.Type == PropType.TrainingMat);
        Assert.True(room.IsWalkable(mat.X + 1, mat.Y + 1));
    }

    // ---------------------------------------------------------------- the command and the coach

    private static (HeadlessScriptHost Host, ScriptRunner Runner) Coach(Pokemon? trainee, params int[] answers)
    {
        var map = MapDatabase.Get(Hall);
        var party = new Party();
        if (trainee != null) party.Add(trainee);
        var host = new HeadlessScriptHost(null, party) { Map = map };
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("TrainingCoach", Hall)!, map.NPCs.Single(n => n.Key == "head_coach"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return (host, runner);
    }

    [Fact]
    public void TheCoachRunsADrillOnThePokemonChosenAndHandsOverItsFirstGoldsVitamin()
    {
        var bidoof = Bidoof();
        var (host, _) = Coach(bidoof, 0);
        Assert.Contains(host.Log, l => l.StartsWith("open Training Bidoof Attack Gold +24"));
        Assert.Equal(24, bidoof.EvAttack);
        Assert.Contains("Gold!", Said(host));
        // A stat's first gold earns its vitamin, handed over at once
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Protein")!));
        Assert.Equal(1, host.Training!.Golds);
        Assert.Empty(host.Training.Owed(hallOfFame: true));
    }

    [Fact]
    public void ADrillLeftOrOnAPokemonThatCantTrainGivesNothing()
    {
        // The screen left before a drill
        var bidoof = Bidoof();
        var map = MapDatabase.Get(Hall);
        var party = new Party();
        party.Add(bidoof);
        var host = new HeadlessScriptHost(null, party) { Map = map, TrainingGrade = null };
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("TrainingCoach", Hall)!, map.NPCs.Single(n => n.Key == "head_coach"));
        runner.RunToEnd();
        Assert.Equal(0, bidoof.EvAttack);
        Assert.Contains("No rush", Said(host));

        // An Egg isn't opened on at all
        var egg = Bidoof();
        egg.IsEgg = true;
        var eggHost = new HeadlessScriptHost(null, new Party()) { };
        eggHost.Party.Add(egg);
        var r = new ScriptRunner(Scripts, eggHost);
        r.Start(ScriptParser.Parse("t", "script T\n  choosepokemon\n  train drill\n  end")[0], null);
        r.RunToEnd();
        Assert.Equal(0, r.Result);
        Assert.DoesNotContain(eggHost.Log, l => l.StartsWith("open Training"));
    }

    [Fact]
    public void TheResetTakesOneStatsEffortOrAllOfItToNought()
    {
        var bidoof = Bidoof();
        bidoof.EvAttack = 100;
        bidoof.EvSpeed = 60;
        bidoof.ReckonStats();
        int attack = bidoof.Attack;
        // RESET EFFORT, yes, ONE STAT'S, SPEED
        var (host, _) = Coach(bidoof, 1, 0, 1, 5);
        Assert.Equal(0, bidoof.EvSpeed);
        Assert.Equal(100, bidoof.EvAttack);
        Assert.Contains("clean slate", Said(host));
        // ...then all of it, and the stats reckoned again at once
        Coach(bidoof, 1, 0, 0);
        Assert.Equal(0, EffortRules.Total(bidoof));
        Assert.True(bidoof.Attack < attack);
        // Nothing left to take is said so
        var (empty, _) = Coach(bidoof, 1, 0, 0);
        Assert.Contains("nothing there to wipe", Said(empty));
    }

    [Fact]
    public void TrainOpenIsNoughtBeforeAHallOfFameAndOneAfter()
    {
        var story = new StoryState();
        int Open()
        {
            var host = new HeadlessScriptHost(story);
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(ScriptParser.Parse("t", "script T\n  train open\n  end")[0], null);
            runner.RunToEnd();
            return runner.Result;
        }
        Assert.Equal(0, Open());
        story.Set(RegionDatabase.Get(RegionDatabase.Kanto)!.StoryCompleteFlag);
        Assert.Equal(1, Open());
    }

    [Fact]
    public void TheClerkSaysHerShelfWaitsForAChampion()
    {
        var map = MapDatabase.Get(Hall);
        var host = new HeadlessScriptHost { Map = map };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("TrainingClerk", Hall)!, map.NPCs.Single(n => n.Key == "clerk"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains("only stocks it for Champions", Said(host));
    }

    // ---------------------------------------------------------------- the prizes and the records

    [Fact]
    public void TheDrillsPrizesAreEachStatsFirstGoldAndEveryTenthGoldsCapOnceAHallOfFameIsEntered()
    {
        var records = new TrainingRecords();
        records.Record(StatType.Attack, 130, DrillGrade.Gold);
        records.Record(StatType.Speed, 50, DrillGrade.Silver);
        Assert.Equal(new[] { "Protein" }, records.Owed(hallOfFame: false));
        for (int i = 0; i < 9; i++) records.Record(StatType.Defense, 80, DrillGrade.Gold);
        Assert.Equal(10, records.Golds);
        // A Bottle Cap only for a Champion
        Assert.Equal(new[] { "Protein", "Iron" }, records.Owed(hallOfFame: false));
        Assert.Equal(new[] { "Protein", "Iron", "Bottle Cap" }, records.Owed(hallOfFame: true));
        Assert.Equal("Protein", records.Hand(hallOfFame: true));
        Assert.Equal("Iron", records.Hand(hallOfFame: true));
        Assert.Equal("Bottle Cap", records.Hand(hallOfFame: true));
        Assert.Null(records.Hand(hallOfFame: true));
        // Fifty golds: five caps in all and a Gold Bottle Cap
        for (int i = 0; i < 40; i++) records.Record(StatType.HP, 80, DrillGrade.Gold);
        var owed = records.Owed(hallOfFame: true);
        Assert.Equal(4, owed.Count(o => o == "Bottle Cap"));
        Assert.Single(owed, o => o == "Gold Bottle Cap");
        Assert.Contains("HP Up", owed);
        // Every prize is an item of the data
        foreach (var p in TrainingData.Prizes.FirstGold) Assert.NotNull(ItemDatabase.Get(p.Item));
        foreach (var p in TrainingData.Prizes.EveryGolds) Assert.NotNull(ItemDatabase.Get(p.Item));
        Assert.Equal(DrillKinds.Stats.OrderBy(s => s), TrainingData.Prizes.FirstGold.Select(p => p.Stat).OrderBy(s => s));
    }

    [Fact]
    public void ABestIsKeptAndANewOneSaidSo()
    {
        var records = new TrainingRecords();
        Assert.True(records.Record(StatType.Attack, 60, DrillGrade.Bronze));
        Assert.False(records.Record(StatType.Attack, 50, DrillGrade.Bronze));
        Assert.True(records.Record(StatType.Attack, 90, DrillGrade.Silver));
        Assert.Equal(90, records.Best(StatType.Attack));
        Assert.Equal(3, records.Drills);
        Assert.False(records.HasGold(StatType.Attack));
    }

    [Fact]
    public void TheRecordsGoThroughASaveAndALoadAndAGameWithNoneWritesNone()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        try
        {
            Assert.Null(engine.Drive.BuildSave().Training);
            var records = engine.Drive.Training;
            records.Record(StatType.Attack, 130, DrillGrade.Gold);
            records.Record(StatType.Speed, 70, DrillGrade.Silver);
            records.Hand(hallOfFame: false);
            var save = engine.Drive.BuildSave();
            string json = JsonSerializer.Serialize(save.Training);
            var back = new TrainingRecords();
            back.Load(JsonSerializer.Deserialize<TrainingSave>(json));
            Assert.Equal(130, back.Best(StatType.Attack));
            Assert.Equal(70, back.Best(StatType.Speed));
            Assert.Equal(2, back.Drills);
            Assert.Equal(1, back.Golds);
            Assert.True(back.HasGold(StatType.Attack));
            // The vitamin handed over stays handed over
            Assert.Empty(back.Owed(hallOfFame: false));
            back.Load(null);
            Assert.True(back.IsEmpty);
        }
        finally
        {
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    // ---------------------------------------------------------------- the screen

    [Fact]
    public void ADrillPlayedThroughTheScreenGivesThePokemonItsEffortAndKeepsTheRecord()
    {
        var bidoof = Bidoof();
        int attack = bidoof.Attack;
        var records = new TrainingRecords();
        var screen = new TrainingScreen();
        screen.Open(bidoof, records, new Random(3));
        Assert.Equal(TrainingPhase.Stats, screen.Phase);
        Assert.Equal(StatType.Attack, screen.Stat);
        screen.NextSeed = GoldSeed(StatType.Attack);
        screen.Confirm();
        Assert.Equal(TrainingPhase.Briefing, screen.Phase);
        Assert.Equal(DrillKind.Throw, screen.Drill!.Kind);
        screen.Confirm();
        Assert.Equal(TrainingPhase.Playing, screen.Phase);
        // Nothing moves during the count
        screen.Advance(1f);
        Assert.Equal(0, screen.Drill.Frames);
        screen.Play(TrainingDrill.Length, d => d.Autopilot(0.9f));
        Assert.Equal(TrainingPhase.Grade, screen.Phase);
        Assert.Equal(3, screen.Outcome);
        Assert.Equal(24, screen.Gained);
        Assert.Equal(24, bidoof.EvAttack);
        Assert.Equal(0, screen.EffortBefore[0]);
        Assert.Equal(24, screen.EffortAfter[0]);
        Assert.True(bidoof.Attack >= attack);
        Assert.Equal(bidoof.Attack, screen.ValueAfter[0]);
        Assert.True(screen.NewBest);
        Assert.True(records.HasGold(StatType.Attack));
        screen.Confirm();
        Assert.Equal(TrainingPhase.Effort, screen.Phase);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal(3, screen.Outcome);
    }

    [Fact]
    public void AStatTheDrillsRefuseIsToldWhyAndTheScreenStaysOnTheChoice()
    {
        var bidoof = Bidoof();
        bidoof.EvSpeed = Ruleset.Current.EvStatCap;
        var screen = new TrainingScreen();
        screen.Open(bidoof, new TrainingRecords(), new Random(3));
        while (screen.Stat != StatType.Speed) screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(TrainingPhase.Stats, screen.Phase);
        Assert.NotNull(screen.Refusal);
        screen.Move(0, 1);
        Assert.Null(screen.Refusal);
        // Backing out of the choice leaves with nothing
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Equal(0, screen.Outcome);
    }

    [Fact]
    public void TheScreensClockPlaysWholeFramesAfterTheCountAndTheSameSeedGivesTheSameDrill()
    {
        int Played(int seed)
        {
            var screen = new TrainingScreen();
            screen.Open(Bidoof(), new TrainingRecords(), new Random(1));
            screen.NextSeed = seed;
            screen.Confirm();
            screen.Confirm();
            for (int i = 0; i < 60 * 40 && screen.Phase == TrainingPhase.Playing; i++)
            {
                screen.Input = screen.Drill!.Autopilot();
                screen.Advance(1f / 60f);
            }
            Assert.Equal(TrainingPhase.Grade, screen.Phase);
            Assert.Equal(TrainingDrill.Length, screen.Drill!.Frames);
            return screen.Drill.Score;
        }
        Assert.Equal(Played(7), Played(7));
    }
}
