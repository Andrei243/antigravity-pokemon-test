using System;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.StageShow;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 42 · PS1 and PS2: the props' table, the trunk, a dressing, the stage show's records in a save, and a show played
/// (<see cref="ShowRun"/>) from its seed and presses. The props' sculpts and anchors are <c>ShowPropModelTests</c>'.
/// </summary>
public class StageShowTests
{
    // ------------------------------------------------------------------ PS1: props, the trunk, dressings

    [Fact]
    public void EveryPieceHasASlotAMoodATrickTwoColoursAndAName()
    {
        Assert.InRange(ShowPropDatabase.All.Count, 36, 48);
        Assert.Equal(ShowPropDatabase.All.Count, ShowPropDatabase.All.Select(p => p.Id).Distinct().Count());
        foreach (var p in ShowPropDatabase.All)
        {
            Assert.True(Enum.IsDefined(p.Slot) && Enum.IsDefined(p.Mood) && Enum.IsDefined(p.Trick), p.Id);
            Assert.Equal(2, p.Colors.Count);
            foreach (var c in p.Colors) ShowPropDatabase.ColourOf(c);
            Assert.True(Text.Has(p.NameKey), p.NameKey);
        }
        // Every slot and every mood has pieces
        foreach (var slot in Enum.GetValues<PropSlot>()) Assert.NotEmpty(ShowPropDatabase.Of(slot));
        foreach (var mood in Enum.GetValues<PropMood>()) Assert.Contains(ShowPropDatabase.All, p => p.Mood == mood);
    }

    [Fact]
    public void ADressingRefusesASecondPieceInASlotAndAFifthPiece()
    {
        var d = new Dressing();
        Assert.True(d.Wear(ShowPropDatabase.Get("top_hat_black")!));
        Assert.Equal(DressRefusal.SlotTaken, d.WhyNot(ShowPropDatabase.Get("tiara")!));
        Assert.False(d.Wear(ShowPropDatabase.Get("tiara")!));
        Assert.True(d.Wear(ShowPropDatabase.Get("round_glasses")!));
        Assert.True(d.Wear(ShowPropDatabase.Get("scarf_yellow")!));
        Assert.True(d.Wear(ShowPropDatabase.Get("baton")!));
        Assert.Equal(DressRefusal.Full, d.WhyNot(ShowPropDatabase.Get("tail_bell")!));
        Assert.Equal(4, d.Count);
        // A slot the body lacks is refused
        Assert.Equal(DressRefusal.NoSuchSlot, new Dressing().WhyNot(ShowPropDatabase.Get("baton")!, new[] { PropSlot.Head }));
    }

    [Fact]
    public void TheTrunkHoldsNineOfAPieceAndNothingOutsideTheTable()
    {
        var t = new PropTrunk();
        Assert.Equal(9, t.Add("bell_collar", 12));
        Assert.Equal(0, t.Add("bell_collar"));
        Assert.Equal(0, t.Add("no_such_piece"));
        Assert.Equal(1, t.Kinds);
    }

    [Fact]
    public void TheRecordsRoundTripASaveAndAnOlderSaveReadsEmpty()
    {
        var r = new ShowRecords();
        Assert.Null(r.ToSave());
        Assert.True(r.GiveTrunk());
        Assert.False(r.GiveTrunk());
        var mon = new Pokemon(PokemonDatabase.Get("Pikachu")!, 20, new Random(4)) { Id = Guid.NewGuid() };
        r.Keep(mon, Dressing.Of(new[] { "top_hat_black", "scarf_yellow", "tiara" }));
        r.Record("sunny_side_revue", 40, true);
        r.Owe("fan_4", "tiara");

        var json = System.Text.Json.JsonSerializer.Serialize(r.ToSave());
        var back = new ShowRecords();
        back.Load(System.Text.Json.JsonSerializer.Deserialize<StageShowSave>(json));
        Assert.True(back.TrunkGiven);
        Assert.Equal(ShowRecords.FirstHandful.Length, back.Trunk.Kinds);
        // The tiara was never in the trunk, so the dressing kept is the hat and the scarf
        Assert.Equal(new[] { "top_hat_black", "scarf_yellow" }, back.DressingOf(mon).Ids());
        Assert.Equal(1, back.Played);
        Assert.Equal(1, back.Won);
        Assert.Equal(40, back.BestOf("sunny_side_revue"));
        Assert.Equal("tiara", back.WaitingWith("fan_4"));

        var empty = new ShowRecords();
        empty.Load(null);
        Assert.Null(empty.ToSave());
        Assert.Null(new SaveData().StageShow);
    }

    // ------------------------------------------------------------------ PS2: a show

    private static StageShowData Show(string id = "sunny_side_revue") => StageShowDatabase.Get(id)!;

    private static Performer Player(params string[] props) => new() { Species = "Pikachu", Dressing = Dressing.Of(props) };

    [Fact]
    public void EveryShowHasThreeCuesForEachOfTheFourAndItsSongsTempo()
    {
        Assert.Equal(2, StageShowDatabase.All.Count);
        foreach (var show in StageShowDatabase.All)
        {
            Assert.Equal(3, show.Cast.Count);
            for (int p = 0; p < 4; p++) Assert.Equal(3, show.Cues.Count(c => c.Performer == p));
            foreach (var c in show.Cues) Assert.InRange(c.Bar + c.Length, 1, show.Bars);
            foreach (var m in show.Cast)
            {
                Assert.NotNull(PokemonDatabase.Get(m.Species));
                Assert.All(m.Props, id => Assert.NotNull(ShowPropDatabase.Get(id)));
            }
            Assert.True(Text.Has(show.NameKey), show.NameKey);
            var song = MusicLibrary.Get(show.Song);
            Assert.NotNull(song);
            Assert.Equal(song!.Tempo, show.Tempo);
        }
    }

    [Fact]
    public void AShowIsAPureFunctionOfItsSeedAndPresses()
    {
        foreach (var show in StageShowDatabase.All)
        {
            var a = new ShowRun(show, Player("top_hat_black", "scarf_yellow"), 77);
            var b = new ShowRun(show, Player("top_hat_black", "scarf_yellow"), 77);
            a.PlayOut();
            b.PlayOut();
            Assert.Equal(a.Eyes, b.Eyes);
            Assert.Equal(a.Tricks, b.Tricks);
            Assert.Equal(a.Places(), b.Places());
            Assert.True(a.Over);
            Assert.Equal(a.TotalFrames, a.Frame);
        }
    }

    [Fact]
    public void TheEyesAlwaysSumToAHundred()
    {
        var run = new ShowRun(Show("hour_of_lanterns"), Player("pointed_hat"), 5);
        while (!run.Over)
        {
            run.Step(run.Frame % 7 == 0 || ShowRun.Autopilot(run));
            Assert.Equal(100, run.Eyes.Sum());
            Assert.All(run.Eyes, e => Assert.InRange(e, 0, 100));
        }
    }

    [Fact]
    public void ATrickOffTheBeatEarnsNothingAndARepeatedOneLess()
    {
        var show = Show();
        // Off the beat: the press half a beat after each beat of the player's cues
        var off = new ShowRun(show, Player("top_hat_black"), 1);
        off.PlayOut(r => r.CueAt(r.BeatAt(r.Frame))?.Performer == 0 && r.Frame == (int)Math.Round((r.Beat + 0.5) * r.FramesPerBeat));
        Assert.All(off.Tricks.Where(t => t.Performer == 0), t => { Assert.False(t.OnBeat); Assert.Equal(0, t.Drew); });

        // One piece only: every trick after the first repeats it, and draws half
        var run = new ShowRun(show, Player("top_hat_black"), 1);
        run.PlayOut();
        var mine = run.Tricks.Where(t => t.Performer == 0 && t.OnBeat).ToList();
        Assert.True(mine.Count >= 6);
        Assert.Equal(ShowRun.TrickEyes, mine[0].Drew);
        Assert.All(mine.Skip(1), t => Assert.True(t.Drew <= (ShowRun.TrickEyes + 1) / 2));
    }

    [Fact]
    public void ADressingOfTheShowsMoodBeatsOneOfNoneWithTheSamePresses()
    {
        var show = Show(); // bright
        var moody = new ShowRun(show, Player("paper_crown_gold", "scarf_yellow", "hand_bell", "tail_bell"), 9);
        var plain = new ShowRun(show, Player(), 9);
        moody.PlayOut();
        plain.PlayOut();
        Assert.True(moody.Eyes[0] > plain.Eyes[0]);
        Assert.True(moody.ScoreOf(0) > plain.ScoreOf(0));
    }

    [Fact]
    public void AutomaticCountsAsGenerous()
    {
        var auto = new ShowRun(Show(), Player(), 3, TimingAssist.Automatic);
        var generous = new ShowRun(Show(), Player(), 3, TimingAssist.Generous);
        Assert.Equal(TimingAssist.Generous, auto.Assist);
        Assert.Equal(generous.WindowFrames, auto.WindowFrames);
        Assert.True(auto.WindowFrames > new ShowRun(Show(), Player(), 3).WindowFrames);
        // Automatic presses nothing by itself
        auto.PlayOut(_ => false);
        Assert.DoesNotContain(auto.Tricks, t => t.Performer == 0);
    }

    [Fact]
    public void AShowsFrameIsBounded()
    {
        var run = new ShowRun(Show("hour_of_lanterns"), Player("pointed_hat", "masquerade_mask"), 11);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        run.PlayOut();
        // Some two and a half thousand frames, each a handful of comparisons
        Assert.InRange(run.TotalFrames, 1500, 4000);
        Assert.True(watch.ElapsedMilliseconds < 500, $"{watch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void AFansGiftWaitsOnceAShowAndIsTakenOnce()
    {
        var records = new ShowRecords();
        records.GiveTrunk();
        var run = new ShowRun(Show(), Player("paper_crown_gold"), 2);
        run.PlayOut();
        var (fan, prop) = ShowFans.Finish(records, run);
        Assert.Contains(fan, ShowFans.Keys);
        Assert.Equal(prop, records.WaitingWith(fan));
        Assert.Equal(prop, records.TakeFrom(fan));
        Assert.Null(records.TakeFrom(fan));
        // The next show's fan is the next of the three
        var next = ShowFans.GiftAfter(2, 10);
        Assert.NotEqual(fan, next.Fan);
    }

    // ------------------------------------------------------------------ the scripts, headless

    private static HeadlessScriptHost Play(string script, ShowRecords records, Action<HeadlessScriptHost>? ready = null)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Pikachu")!, 20, new Random(1)));
        var host = new HeadlessScriptHost(new StoryState(), party) { StageShow = records };
        ready?.Invoke(host);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, "musical_theater")!, new NPC { Name = "Owner", DialogLines = new() { "Hello!" } });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheOwnerGivesTheTrunkOnceAndTheDeskOpensTheRoomAndTheStage()
    {
        var records = new ShowRecords();
        Play("Desk", records, h => h.Answers.Enqueue(1));
        Assert.Equal(0, records.Played);
        Play("Owner", records);
        Assert.True(records.TrunkGiven);
        int kinds = records.Trunk.Kinds;
        Play("Owner", records);
        Assert.Equal(kinds, records.Trunk.Kinds);
        var host = Play("Desk", records, h => h.Answers.Enqueue(0));
        Assert.Contains(host.Log, l => l.Contains("DressingRoom"));
        host = Play("Desk", records, h => h.Answers.Enqueue(1));
        Assert.Contains(host.Log, l => l.Contains("StageShow"));
        Assert.Equal(1, records.Played);
        // A fan waits after the show, gives once, then talks as before
        string fan = records.Fans.Keys.Single();
        string script = "Fan" + fan["fan_".Length..];
        host = Play(script, records);
        Assert.Contains(host.Transcript, t => t.Text.Contains("received a Prop"));
        host = Play(script, records);
        Assert.DoesNotContain(host.Transcript, t => t.Text.Contains("received a Prop"));
    }
}
