using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Skipping a scene already seen (plan 12 · Q20): the two lines that mark a scene and what the library finds wrong
/// with them, when a scene counts as seen, what the runner does in a hurry and where the hurry stops, which scenes
/// the options let the player skip, the hold, what the save and the machine keep, and what the engine ends at once.
/// Last, every scene the game's scripts mark is played on every way through it and played again by someone who holds
/// the button the whole time, and the two leave the same.
/// </summary>
public class SceneSkipTests
{
    private static (ScriptRunner Runner, HeadlessScriptHost Host) Ready(string source, string common = "", NPC? subject = null, Action<HeadlessScriptHost>? before = null)
    {
        var library = ScriptLibrary.FromSources(("test", source), ("common", common));
        var host = new HeadlessScriptHost();
        before?.Invoke(host);
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), subject);
        return (runner, host);
    }

    private static List<string> Said(HeadlessScriptHost host) => host.Transcript.Select(t => t.Text).ToList();
    private static List<string> Unread(HeadlessScriptHost host) => host.Unread.Select(t => t.Text).ToList();

    private static NPC Trainer(string id = "tester") => MapFile.BuildNpc(new MapFile.NpcRecord
    {
        Id = id, Name = "Tess", NpcType = "Lass", X = 3, Y = 3,
        Dialog = new List<string> { "I like shorts." },
        Trainer = new MapFile.TrainerRecord
        {
            Id = id, Name = "Tess", TrainerClass = "Lass", PrizeMoney = 120,
            Party = new List<MapFile.PartyMember> { new() { Species = "Bidoof", Level = 3 } },
            DialogueBefore = "Let's battle!", DialogueAfter = "You're strong."
        }
    }, "Test");

    // ------------------------------------------------------------------ the two lines

    [Fact]
    public void AScriptMarksASceneWithItsTwoLines()
    {
        var script = ScriptParser.Parse("town", """
            script Arrival
              if flag FLAG_DONE end
              scene "The rival's welcome"
              say "Hello!"
              endscene
              end
            """)[0];

        Assert.Equal(new[] { Op.If, Op.Scene, Op.Say, Op.EndScene, Op.End }, script.Code.Select(i => i.Op));
        Assert.Equal("The rival's welcome", script.Code[1].Name);
        // A scene's key is the file it is written in and its name
        Assert.Equal("town.The rival's welcome", SceneSkip.Key(script.File, script.Code[1].Name));
        // They were added at the end of the language, one after the other: nothing before them moved
        // (plan 14 · B14's master came after them)
        var ops = Enum.GetValues<Op>();
        Assert.Equal(Array.IndexOf(ops, Op.Scene) + 1, Array.IndexOf(ops, Op.EndScene));
        Assert.True(Array.IndexOf(ops, Op.Scene) > Array.IndexOf(ops, Op.Letterbox));
    }

    [Theory]
    [InlineData("script A\n scene", "the scene's name")]
    [InlineData("script A\n scene \"\"", "no name for a scene")]
    [InlineData("script A\n scene \"a.b\"", "no name for a scene")]
    [InlineData("script A\n scene \" leading\"", "no name for a scene")]
    [InlineData("script A\n scene \"A name that goes on for far longer than forty characters\"", "no name for a scene")]
    [InlineData("script A\n scene \"One\" \"Two\"", "more than the line needs")]
    [InlineData("script A\n endscene now", "more than the line needs")]
    [InlineData("script A\n if flag FLAG_A scene \"One\"", "can't guard a scene")]
    [InlineData("script A\n scene \"One\"\n if flag FLAG_A endscene", "can't guard a scene")]
    public void WhatIsWrittenWrongOfASceneIsToldWithItsLine(string source, string telling)
    {
        var wrong = Assert.Throws<ScriptException>(() => ScriptParser.Parse("town", source));
        Assert.Contains(telling, wrong.Message);
        Assert.StartsWith($"town.txt({source.Split('\n').Length})", wrong.Message);
    }

    [Fact]
    public void TheLibraryNamesWhatIsWrongWithTheScenes()
    {
        List<string> Problems(params (string File, string Text)[] sources) => ScriptLibrary.FromSources(sources).Problems();

        // A scene well made: one endscene below it, or one to each way out
        Assert.Empty(Problems(("town", """
            script A
              scene "One"
              ask "Well?"
              if no goto Other
              endscene
              end
            label Other
              say "Oh."
              endscene
              end
            script B
              scene "Two"
              call Helper
              endscene
            script Helper
              say "Helping."
              return
            """)));

        // Left open
        var open = Problems(("town", "script A\n scene \"One\"\n say \"x\"\n end"));
        Assert.Contains("town.txt(2): scene 'One' is left open", Assert.Single(open));

        // An endscene with no scene above it
        var stray = Problems(("town", "script A\n say \"x\"\n endscene\n scene \"One\"\n endscene"));
        Assert.Contains("town.txt(3): an endscene with no scene above it", Assert.Single(stray));

        // A scene inside a scene, written out and through a call
        var inside = Problems(("town", "script A\n scene \"One\"\n scene \"Two\"\n endscene"));
        Assert.Contains("town.txt(3): scene 'Two' begins inside scene 'One'", Assert.Single(inside));
        var called = Problems(("town", "script A\n scene \"One\"\n call B\n endscene\nscript B\n call C\n return\nscript C\n scene \"Three\"\n endscene"));
        Assert.Contains("town.txt(3): scene 'One' calls 'B', which begins a scene of its own", Assert.Single(called));

        // Two scenes of one name in one place's file, but not in two files
        var twice = Problems(("town", "script A\n scene \"One\"\n endscene\nscript B\n scene \"One\"\n endscene"));
        Assert.Contains("town.txt(5): town.txt has two scenes called 'One'", Assert.Single(twice));
        Assert.Empty(Problems(("town", "script A\n scene \"One\"\n endscene"), ("city", "script A\n scene \"One\"\n endscene")));
    }

    // ------------------------------------------------------------------ seen

    [Fact]
    public void ASceneIsSeenAtItsEndsceneAndNotBefore()
    {
        var (runner, host) = Ready("""
            script S
              say "Before."
              scene "One"
              say "In it."
              setflag FLAG_IN
              endscene
              say "After."
            """);
        host.Pauses = true;

        runner.Update(0f);
        Assert.Null(runner.Scene);
        host.Busy = false;
        runner.Update(0f);
        Assert.Equal("test.One", runner.Scene);
        Assert.Empty(host.Story.ScenesSeen);
        host.Busy = false;
        runner.Update(0f);
        // Past its endscene: the story has it, and so has the machine
        Assert.Null(runner.Scene);
        Assert.True(host.Story.HasSeenScene("test.One"));
        Assert.Contains("test.One", host.MachineScenes);
        Assert.Equal(new[] { "scene test.One", "endscene" }, host.Log);
        host.Busy = false;
        runner.Update(0f);
        Assert.False(runner.IsRunning);
        Assert.Equal(new[] { "Before.", "In it.", "After." }, Said(host));
    }

    [Theory]
    // The script ends on a way out
    [InlineData("script S\n scene \"One\"\n if not flag FLAG_X end\n endscene")]
    // Its battle is lost
    [InlineData("script S\n scene \"One\"\n battle self\n endscene")]
    // The script that began it was called, and returns
    [InlineData("script S\n call Inner\n say \"Back.\"\nscript Inner\n scene \"One\"\n return")]
    // The script runs off its end
    [InlineData("script S\n scene \"One\"\n say \"x\"")]
    public void ASceneThatEndsWithoutItsEndsceneIsLeftUnseen(string source)
    {
        var (runner, host) = Ready(source, subject: Trainer(), before: h => h.Fight = _ => BattleOutcome.Lost);
        runner.RunToEnd();

        Assert.Null(runner.Scene);
        Assert.Empty(host.Story.ScenesSeen);
        Assert.Empty(host.MachineScenes);
        Assert.Contains("scene over, unseen", host.Log);
    }

    [Fact]
    public void ASceneLeftOpenByAScriptThatGoesWrongOrIsStoppedIsOver()
    {
        var (runner, host) = Ready("script S\n scene \"One\"\n battle nobody\n endscene");
        Assert.Throws<ScriptException>(() => runner.RunToEnd());
        Assert.Equal("test.One", runner.Scene);
        // The game stops a script that went wrong, and the scene with it
        runner.Abort();
        Assert.Null(runner.Scene);
        Assert.Empty(host.Story.ScenesSeen);

        // And a script started while another's scene was open leaves none of that scene
        (runner, host) = Ready("script S\n scene \"One\"\n say \"x\"\n endscene\nscript T\n say \"y\"");
        host.Pauses = true;
        runner.Update(0f);
        Assert.Equal("test.One", runner.Scene);
        runner.Start(ScriptLibrary.FromSources(("test", "script T\n say \"y\"")).All.First());
        Assert.Null(runner.Scene);
    }

    [Fact]
    public void ASceneBegunAgainWhileItIsOpenGoesOn()
    {
        // A question asked until it hears yes jumps back over the scene's first line
        var (runner, host) = Ready("""
            script S
            label Again
              scene "One"
              ask "Ready?"
              if no goto Again
              endscene
            """);
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(0);
        runner.RunToEnd();

        Assert.Equal(new[] { "scene test.One", "endscene" }, host.Log);
        Assert.True(host.Story.HasSeenScene("test.One"));
    }

    // ------------------------------------------------------------------ the hurry

    private const string Scene = """
        script S
          say "Before the scene."
          scene "One"
          speaker "Rival"
          say "A long speech." "And more of it."
          walk rival right 2 down 1
          emote rival exclaim
          wait 5
          camera pan 4 4 3
          letterbox on
          effect burst 2 2
          effect beam 3 3 hold
          sound "door_open"
          fanfare item
          music "common/rival"
          fade out
          setflag FLAG_HAPPENED
          setvar VAR_STATE 3
          give "Potion" 2
          givepokemon "Starly" 4
          givemoney 250
          hide other
          place player 5 5 up
          anim pc on
          shake rival
          endscene
          say "After the scene."
        """;

    private static Map Room()
    {
        var map = new Map(10, 10) { Name = "Room" };
        map.NPCs.Add(new NPC { Key = "rival", Name = "Rival", GridX = 2, GridY = 2, Facing = Direction.Down });
        map.NPCs.Add(new NPC { Key = "other", Name = "Other", GridX = 7, GridY = 7 });
        return map;
    }

    [Fact]
    public void InAHurryNothingIsReadOrWaitedForAndEverythingStillHappens()
    {
        var (runner, host) = Ready(Scene, before: h => h.Map = Room());
        host.Pauses = true;
        var rival = host.Map!.FindPerson("rival", null)!;

        // Played as far as the scene's first lines, which wait to be read
        runner.Update(0f);
        host.Busy = false;
        runner.Update(0f);
        Assert.Equal(new[] { "Before the scene.", "A long speech.", "And more of it." }, Said(host));
        Assert.True(host.Busy);
        Assert.False(host.Skipping);

        // The button is held: the game puts the text away, and the runner hurries
        host.Busy = false;
        Assert.True(runner.Hurry());
        Assert.True(runner.Hurrying && host.Skipping);
        runner.Update(0f);

        // It stopped at the scene's end, and the line after it is said at the scene's own pace
        Assert.False(runner.Hurrying || host.Skipping);
        Assert.Null(runner.Scene);
        Assert.Equal("After the scene.", Said(host)[^1]);
        Assert.Equal(1, host.Skips);

        // Everything a line does to the story, the bag, the team and the map happened
        Assert.True(host.Story.Has("FLAG_HAPPENED"));
        Assert.Equal(3, host.Story.Var("VAR_STATE"));
        Assert.Equal(2, host.Bag.GetQuantity(ItemDatabase.Get("Potion")!));
        Assert.Equal("Starly", Assert.Single(host.Party.Members).Species.Name);
        Assert.Equal(3250, host.Money);
        Assert.DoesNotContain(host.Map.NPCs, n => n.Key == "other");
        Assert.Equal(((5, 5), Direction.Up), (host.PlayerTile, host.PlayerFacing));
        // The walk ended where it would have, facing as its last step leaves them
        Assert.Equal((4, 3, Direction.Down), (rival.GridX, rival.GridY, rival.Facing));
        Assert.Equal((0f, 0f, 0f), (rival.StepOffsetX, rival.StepOffsetY, rival.WalkBlend));

        // What was only shown or heard was dropped: the item's lines unread, the bubble, the sounds, what passes
        Assert.Equal(new[] { "{player} received 2 × Potion!", "{player} put them away in the Medicine pocket." }.Select(l => l.Replace("{player}", host.PlayerName)), Unread(host));
        Assert.Equal(new[] { "emote Rival Exclaim", "effect Burst 2 2", "sound door_open", "fanfare FanfareItem", "fanfare FanfareItem", "shake Rival 8" }, host.Dropped);
        // What a scene leaves on the screen was left: the camera where it was sent, the bars in, the black screen,
        // the effect held, the song, the PC on
        Assert.Equal(("4,4", true, true, "common/rival", true), (host.CameraOn, host.Bars, host.ScreenBlack, host.Song, host.PcOn));
        Assert.Equal(new[] { "Beam 3 3" }, host.HeldEffects);
        // And the scene counts as seen
        Assert.True(host.Story.HasSeenScene("test.One"));
    }

    [Theory]
    [InlineData("ask \"Well?\"", "Well?", null)]
    [InlineData("choose \"Which?\" \"This\" \"That\"", "Which?", null)]
    [InlineData("battle self", null, "battle tester Won")]
    [InlineData("wildbattle \"Starly\" 3", null, "wildbattle Starly 3 Won")]
    [InlineData("catchinglesson \"Bidoof\" 2", null, "catchinglesson Bidoof 2 Caught")]
    [InlineData("starter", null, "open Starter")]
    [InlineData("shop", null, "open Shop")]
    [InlineData("pc", null, "open Pc")]
    [InlineData("wardrobe", null, "open Wardrobe")]
    [InlineData("choosepokemon", null, "open ChoosePokemon")]
    [InlineData("halloffame show", null, "open Ceremony")]
    [InlineData("credits", null, "open Credits")]
    public void TheHurryStopsBeforeWhatWaitsForThePlayer(string line, string? asked, string? logged)
    {
        var (runner, host) = Ready($"script S\n scene \"One\"\n say \"First.\"\n setflag FLAG_BEFORE\n {line}\n setflag FLAG_AFTER\n say \"Last.\"\n endscene", subject: Trainer());
        host.Pauses = true;
        runner.Update(0f);
        host.Busy = false;
        Assert.True(runner.Hurry());
        runner.Update(0f);

        // The hurry is over, and what waits for the player was begun at the scene's own pace: the host is busy with it
        Assert.False(runner.Hurrying || host.Skipping);
        Assert.Empty(host.Problems);
        Assert.True(host.Story.Has("FLAG_BEFORE"));
        Assert.False(host.Story.Has("FLAG_AFTER"));
        Assert.Equal("test.One", runner.Scene);
        if (asked != null) Assert.Equal(asked, Assert.Single(host.Asked).Question);
        if (logged != null) Assert.Contains(logged, host.Log);
        Assert.True(host.Busy);

        // Answered, the scene goes on at its own pace: its last line is read, not dropped
        host.Busy = false;
        runner.Update(0f);
        Assert.True(host.Story.Has("FLAG_AFTER"));
        Assert.Equal("Last.", Said(host)[^1]);
        Assert.Empty(host.Unread);
    }

    [Fact]
    public void HoldingAgainSkipsOnAfterTheAnswer()
    {
        // Someone who holds the button the whole time: the hurry stops for each question and battle, and goes on after it
        var (runner, host) = Ready("""
            script S
              scene "One"
              say "One."
              ask "Ready?"
              if no end
              say "Two."
              battle self
              say "Three."
              endscene
              say "Four."
            """, subject: Trainer());
        runner.Impatient = true;
        runner.RunToEnd();

        Assert.Empty(host.Problems);
        Assert.Equal(new[] { "One.", "Two.", "Three." }, Unread(host));
        Assert.Equal(new[] { "Ready?", "Four." }, Said(host));
        Assert.Equal(3, host.Skips);
        Assert.Contains("battle tester Won", host.Log);
        Assert.True(host.Story.HasSeenScene("test.One"));
    }

    [Fact]
    public void OutsideASceneThereIsNoHurry()
    {
        var (runner, host) = Ready("script S\n say \"One.\"\n say \"Two.\"\n scene \"S\"\n endscene");
        host.Pauses = true;
        Assert.False(runner.Hurry());
        runner.Update(0f);
        Assert.False(runner.Hurry());
        Assert.False(host.Skipping);
        Assert.Equal(0, host.Skips);
        // ... and once the script is over there is none either
        host.Busy = false;
        runner.Update(0f);
        Assert.False(runner.IsRunning);
        Assert.False(runner.Hurry());
    }

    [Fact]
    public void AHostThatIsBusyAfterAllEndsTheHurry()
    {
        var (runner, host) = Ready("script S\n scene \"One\"\n say \"One.\"\n setflag FLAG_A\n say \"Two.\"\n endscene");
        host.Pauses = true;
        runner.Update(0f);
        // The host has something to see through that no hurry ends (it stays busy): the hurry is over at once
        Assert.True(runner.Hurry());
        runner.Update(0f);
        Assert.False(runner.Hurrying || host.Skipping);
        Assert.False(host.Story.Has("FLAG_A"));
    }

    [Fact]
    public void EveryCommandSaysWhatItDoesInAHurry()
    {
        var all = ScriptParser.Parse("test", ScriptTests.EveryCommand)[0];
        var told = new HashSet<Op>();
        foreach (var line in all.Everything())
        {
            // A command the runner doesn't know in a hurry throws here: name it in ScriptRunner.StopsAHurry
            ScriptRunner.StopsAHurry(line);
            told.Add(line.Op);
        }
        Assert.DoesNotContain(Enum.GetValues<Op>(), op => !told.Contains(op));

        // What waits for the player, or can't be ended at once, stops a hurry; what is said, shown, heard or only
        // changes the story is hurried through
        var lines = ScriptParser.Parse("test", """
            script Stops
              ask "Well?"
              choose "Which?" "This" "That"
              battle self
              wildbattle "Starly" 2
              catchinglesson "Bidoof" 2
              sweetscent
              honeytree battle
              starter
              shop
              pc
              pc halloffame
              wardrobe
              travel
              choosepokemon
              choosepokemon "Bidoof"
              master self
              chooseitem berries
              halloffame show
              credits
              save
              hatch
              poffin cook
              supercontest enter
              supercontest photo
              seal capsules
              seal shop
              underground mine
              underground down
              usemove "Cut"
              surf
              climb
              fly
              teleport
              escape
              flowerclock
              pressbutton blue
              gearbutton normal
              platformlift
              fossil choose
              gamecorner slots 3
              villa catalogue
              palpark start
              frontier records tower
              tower battle
              factory rent
              hall board
              castle self
              arcade board
              tutor learn
              amity walk
            script Runs
              say "a"
              text "b"
              sayown
              trainerline before
              speaker none
              goto Here
            label Here
              call Stops
              setflag FLAG_A
              setvar VAR_A 3
              give "Potion"
              find own
              take "Potion"
              givepokemon "Starly" 4
              givebadge coal
              heal
              face self player
              walk self up 2
              move self up 2
              waitmoves
              emote self exclaim
              act self wave
              mood self happy
              show self
              hide self
              place self 3 4
              warp "Room" 3 4
              fade out
              wait 2
              camera pan 3 4
              camera release
              letterbox on
              slowmo 0.5
              effect burst 3 4
              anim heal
              anim halloffame
              shake self
              flicker self
              music "common/rival"
              fanfare item
              sound "select"
              cry "Shinx"
              halloffame
              honeytree status
              poffin check
              supercontest check
              seal give "Heart Seal A"
              underground give trap "Bubble Trap"
              underground flags
              platformlift check
              defeat "lass_caroline"
              partner off
              poketch on
              seen "Dialga"
              nationaldex
              random 3
              team first
              ribbon count
              daycare state
              berry status
              lottery draw
              scene "One"
              endscene
              return
              end
            """);
        Assert.DoesNotContain(lines[0].Code, line => !ScriptRunner.StopsAHurry(line));
        Assert.DoesNotContain(lines[1].Code, ScriptRunner.StopsAHurry);
    }

    [Fact]
    public void NothingHurriedThroughWaitsForThePlayer()
    {
        // Every command of the language, each alone in a scene, played by someone who holds the button the whole
        // time on a host that notes whatever reached it in a hurry and should not have (a question, a battle, a
        // screen, a save, a puzzle): a command that opens one and doesn't stop a hurry is told here
        var all = ScriptParser.Parse("test", ScriptTests.EveryCommand)[0];
        string[] text = ScriptTests.EveryCommand.Replace("\r\n", "\n").Split('\n');
        int tried = 0, reached = 0;
        foreach (var line in all.Code.Where(i => i.Op is not (Op.Scene or Op.EndScene or Op.Goto or Op.Return or Op.End or Op.Call)))
        {
            string written = text[line.Line - 1].Trim();
            if (written.StartsWith("if ", StringComparison.Ordinal)) continue;
            var library = ScriptLibrary.FromSources(("test", $"script S\n scene \"One\"\n {written}\n endscene\nscript Other\n end"), ("common", ""));
            var map = Room();
            foreach (string who in new[] { "twin", "helper", "rival", "cheryl", "opponent" })
                map.NPCs.Add(MapFile.BuildNpc(new MapFile.NpcRecord { Id = who, Name = who, NpcType = "Lass", X = 1, Y = 1, Trainer = new MapFile.TrainerRecord { Id = who, Name = who, TrainerClass = "Lass", Party = new List<MapFile.PartyMember> { new() { Species = "Bidoof", Level = 3 } } } }, "Test"));
            var host = new HeadlessScriptHost { Map = map };
            host.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5, new Random(0)));
            var runner = new ScriptRunner(library, host) { Impatient = true };
            runner.Start(library.All.First(s => s.Name == "S"), Trainer());
            tried++;
            try
            {
                runner.RunToEnd();
                reached++;
            }
            catch (Exception e) when (e is not Xunit.Sdk.XunitException)
            {
                // (A line that needs more of the game than this room has stops there: what it reached first still counts)
            }
            Assert.True(!host.Problems.Any(p => p.Contains("a hurry reached")), $"'{written}': {string.Join("; ", host.Problems)}");
        }
        Assert.True(tried > 300 && reached > 150, $"only {reached} of {tried} commands ran to their end here");
    }

    // ------------------------------------------------------------------ what may be skipped

    [Fact]
    public void TheRowDecidesWhatMayBeSkipped()
    {
        var story = new StoryState();
        var machine = new GameSettings();
        bool May(SkipScenes row) => SceneSkip.Allowed(row, "route_201.The professor", story, machine.ScenesSeen);

        // Off, nothing; Any, everything; Seen, nothing yet
        Assert.False(May(SkipScenes.Off));
        Assert.True(May(SkipScenes.Any));
        Assert.False(May(SkipScenes.Seen));

        // Once this game has played it to its end
        story.SeeScene("route_201.The professor");
        Assert.True(May(SkipScenes.Seen));
        Assert.False(May(SkipScenes.Off));
        Assert.False(SceneSkip.Allowed(SkipScenes.Seen, "route_201.Off to the lake", story, machine.ScenesSeen));

        // ... or this machine has, in another game
        var second = new StoryState();
        Assert.False(SceneSkip.Allowed(SkipScenes.Seen, "route_201.The professor", second, machine.ScenesSeen));
        machine.SeeScene("route_201.The professor");
        Assert.True(SceneSkip.Allowed(SkipScenes.Seen, "route_201.The professor", second, machine.ScenesSeen));

        // Seeing a scene is no change to who stands where: the maps are not looked at again for it
        int revision = story.Revision;
        Assert.True(story.SeeScene("route_201.Off to the lake"));
        Assert.False(story.SeeScene("route_201.Off to the lake"));
        Assert.Equal(revision, story.Revision);
        // A new game has seen nothing
        story.Clear();
        Assert.Empty(story.ScenesSeen);
    }

    [Fact]
    public void ASceneSkippedCountsAsSeen()
    {
        var (runner, host) = Ready("script S\n scene \"One\"\n say \"x\"\n endscene");
        runner.Impatient = true;
        runner.RunToEnd();
        Assert.Equal(new[] { "x" }, Unread(host));
        Assert.True(host.Story.HasSeenScene("test.One"));
        Assert.Contains("test.One", host.MachineScenes);
    }

    // ------------------------------------------------------------------ the hold

    [Fact]
    public void TheHoldTakesASecondAndStartsOverWhenLetGo()
    {
        Assert.Equal(1f, SkipHold.Seconds);
        var hold = new SkipHold();
        const float frame = 1f / 60f;

        // Nothing is shown until the button is down, and from then on its ring fills evenly
        Assert.False(hold.Update(false, true, frame));
        Assert.False(hold.Showing);
        for (int i = 0; i < 30; i++) Assert.False(hold.Update(true, true, frame));
        Assert.True(hold.Showing);
        Assert.InRange(hold.Fill, 0.49f, 0.51f);

        // Let go early, it starts again from nothing
        Assert.False(hold.Update(false, true, frame));
        Assert.False(hold.Showing);
        Assert.Equal(0f, hold.Fill);

        // Held for the whole second it does its work, once
        int fired = 0, frames = 0;
        for (; frames < 200 && fired == 0; frames++)
            if (hold.Update(true, true, frame)) fired++;
        Assert.InRange(frames, 59, 62);
        // Still down, it counts for nothing more until it has been let go
        for (int i = 0; i < 200; i++) Assert.False(hold.Update(true, true, frame));
        Assert.False(hold.Showing);
        hold.Update(false, true, frame);
        for (int i = 0; i < 59; i++) Assert.False(hold.Update(true, true, frame));
        Assert.True(hold.Showing);

        // A frame that took far too long is no hold by itself
        hold.Reset();
        Assert.False(hold.Update(true, true, 5f));
        Assert.InRange(hold.Fill, 0.05f, 0.15f);
    }

    [Fact]
    public void WhereNothingMayBeSkippedTheHoldDoesNothing()
    {
        // The options' row off (or a scene not seen yet, or a question on the screen): the engine says "not allowed"
        var hold = new SkipHold();
        for (int i = 0; i < 300; i++) Assert.False(hold.Update(true, false, 1f / 60f));
        Assert.False(hold.Showing);
        Assert.Equal(0f, hold.Fill);
        // A hold counted while it was allowed is gone the moment it isn't
        for (int i = 0; i < 30; i++) hold.Update(true, true, 1f / 60f);
        Assert.True(hold.Showing);
        hold.Update(true, false, 1f / 60f);
        Assert.False(hold.Showing);
        Assert.False(SceneSkip.Allowed(SkipScenes.Off, "test.One", new StoryState(), new GameSettings().ScenesSeen));
    }

    // ------------------------------------------------------------------ what is kept

    private sealed class Folder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pokemon-scenes-" + Guid.NewGuid().ToString("N"));
        public Folder() => Directory.CreateDirectory(Path);
        public string File(string name) => System.IO.Path.Combine(Path, name);
        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { }
        }
    }

    [Fact]
    public void TheSaveKeepsTheScenesSeenAndAnOlderSaveHasNone()
    {
        using var folder = new Folder();
        var story = new StoryState();
        story.SeeScene("route_201.The professor");
        story.SeeScene("RowanLab.The Pokedex");
        var save = new SaveData { PlayerName = "Lucas", CurrentMapName = "Sinnoh", ScenesSeen = story.ScenesSeen.Order(StringComparer.Ordinal).ToList(), StoryVersion = StoryState.CurrentVersion };

        Assert.True(SaveManager.SaveGame(save, folder.File(SaveManager.FileName)));
        var (outcome, loaded) = SaveManager.Load(folder.File(SaveManager.FileName));
        Assert.Equal(SaveLoad.Ok, outcome);
        var back = new StoryState();
        back.Restore(loaded!.ToStory());
        back.RestoreScenes(loaded.ScenesSeen);
        Assert.Equal(new[] { "RowanLab.The Pokedex", "route_201.The professor" }, back.ScenesSeen.Order(StringComparer.Ordinal));

        // A save from before scenes were kept reads as having seen none, and needs nothing done to it: the story's
        // version is what it was
        var old = JsonSerializer.Deserialize<SaveData>("""{ "PlayerName": "Lucas", "CurrentMapName": "Sinnoh", "StoryVersion": 18, "StoryFlags": [ "FLAG_HAS_POKEDEX" ] }""")!;
        Assert.Empty(old.ScenesSeen);
        back.Restore(old.ToStory());
        back.RestoreScenes(old.ScenesSeen);
        Assert.Empty(back.ScenesSeen);
        // (Restoring the story alone forgets them too: the save's own list is what counts)
        back.SeeScene("x.Y");
        back.Restore(old.ToStory());
        Assert.Empty(back.ScenesSeen);
    }

    [Fact]
    public void TheMachineKeepsTheScenesSeenWithTheOptions()
    {
        using var folder = new Folder();
        string path = folder.File(GameSettings.FileName);
        var settings = GameSettings.Load(path);
        Assert.Equal(SkipScenes.Off, settings.SkipScenes);
        Assert.Empty(settings.ScenesSeen);

        // Seen once: worth writing; seen again: not
        Assert.True(settings.SeeScene("route_201.The professor"));
        Assert.False(settings.SeeScene("route_201.The professor"));
        Assert.True(settings.SeeScene("RowanLab.The Pokedex"));
        Assert.False(settings.SeeScene(""));
        Assert.Equal(new[] { "RowanLab.The Pokedex", "route_201.The professor" }, settings.ScenesSeen);

        // What a switch set for this run alone never reaches the file with them
        settings.SetForThisRun(nameof(GameSettings.Quality), GraphicsQuality.Low);
        settings.SkipScenes = SkipScenes.Seen;
        settings.Save();
        var again = GameSettings.Load(path);
        Assert.Equal(new[] { "RowanLab.The Pokedex", "route_201.The professor" }, again.ScenesSeen);
        Assert.Equal((SkipScenes.Seen, GraphicsQuality.High), (again.SkipScenes, again.Quality));
        Assert.False(again.SeeScene("RowanLab.The Pokedex"));

        // A file from before the row, or one whose list was written by hand in any order
        File.WriteAllText(path, """{ "Quality": "Medium", "ScenesSeen": [ "b.Two", "a.One" ] }""");
        var byHand = GameSettings.Load(path);
        Assert.Equal(SkipScenes.Off, byHand.SkipScenes);
        Assert.False(byHand.SeeScene("a.One"));
        Assert.False(byHand.SeeScene("b.Two"));
        File.WriteAllText(path, """{ "Quality": "Medium" }""");
        Assert.Empty(GameSettings.Load(path).ScenesSeen);

        // The log and a crash report are told how many, not which
        Assert.Contains("\"ScenesSeen\": 2", settings.Describe());
        Assert.DoesNotContain("route_201", settings.Describe());
    }

    [Fact]
    public void TheOptionsRowIsOffUnlessChosenAndGoesRoundItsThreeValues()
    {
        var settings = new GameSettings();
        Assert.Equal(SkipScenes.Off, settings.SkipScenes);
        var seen = new List<SkipScenes>();
        for (int i = 0; i < 4; i++)
        {
            OptionsScreen.Change(settings, OptionRow.SkipScenes, 1);
            seen.Add(settings.SkipScenes);
        }
        Assert.Equal(new[] { SkipScenes.Seen, SkipScenes.Any, SkipScenes.Off, SkipScenes.Seen }, seen);
        OptionsScreen.Change(settings, OptionRow.SkipScenes, -1);
        Assert.Equal(SkipScenes.Off, settings.SkipScenes);
        // A new row goes in before HELP, and the reset (plan 12 · Q3) is the last
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(OptionRow.ResetDefaults, rows[^1]);
        Assert.True(Array.IndexOf(rows, OptionRow.SkipScenes) < Array.IndexOf(rows, OptionRow.Help));
    }

    // ------------------------------------------------------------------ what the engine ends at once

    [Fact]
    public void TheNumbersAreTheStyleGuides()
    {
        // Style guide, "Field menus and notices", "A scene skipped": a hold of one second; the pill 56 high, 64 from
        // the right edge, its middle in the middle of a cinema bar; a ring 36 across and 5 wide; 0.2 s to close and 0.3 s to open
        Assert.Equal(1f, SkipHold.Seconds);
        Assert.Equal((64f, 56f, 18f, 5f), (ModernUi.SkipRingMargin, ModernUi.SkipPillHeight, ModernUi.SkipRingRadius, ModernUi.SkipRingWidth));
        Assert.Equal(CameraRig.BarHeight / 2f, ModernUi.SkipRingMiddle);
        Assert.Equal((0.2f, 0.3f), (GameEngine.SkipCloseSeconds, GameEngine.SkipOpenSeconds));
    }

    [Fact]
    public void ACamerasMovesAreOverAtOnce()
    {
        var rig = new CameraRig();
        var player = new System.Numerics.Vector2(10.5f, 10.5f);
        rig.Look(player);
        rig.Pan(20.5f, 14.5f, 3f);
        rig.ZoomTo(2f, 4f);
        rig.Letterbox(true, 0.4f);
        rig.SlowMotion(0.25f, 30f);
        rig.Advance(0.1f);
        Assert.True(rig.Moving);

        rig.Settle();
        Assert.False(rig.Moving);
        Assert.Equal(new System.Numerics.Vector2(20.5f, 14.5f), rig.Look(player));
        Assert.Equal((2f, CameraRig.BarHeight, 1f), (rig.Zoom, rig.Bars, rig.Pace));

        // Released and settled, it is as if nothing had been told
        rig.Release(2f);
        rig.Settle();
        Assert.True(rig.AtRest);
        Assert.Equal(player, rig.Look(player));

        // A path is at its last spot; and a rig nobody told anything is left as it is
        rig.Path(new[] { new System.Numerics.Vector2(12.5f, 10.5f), new System.Numerics.Vector2(12.5f, 4.5f) }, 2f);
        rig.Settle();
        Assert.Equal(new System.Numerics.Vector2(12.5f, 4.5f), rig.Look(player));
        var untold = new CameraRig();
        untold.Settle();
        Assert.True(untold.AtRest);
        Assert.Equal(player, untold.Look(player));
    }

    [Fact]
    public void ASetPieceIsAtItsEndAtOnceAndUnheard()
    {
        var stage = new SetPieceStage();
        var cues = new List<(SetPieceCue Cue, int? X)>();

        // The Hall of Fame's machine keeps its balls, unlit; a healing machine's tray is empty again
        stage.Play(SetPiece.HallOfFame, 4, 10.0, (5, 2));
        stage.Settle(10.0);
        Assert.Equal((4, false), stage.Machine(SetPiece.HallOfFame, 10.0));
        stage.Play(SetPiece.Heal, 3, 10.0, (5, 2));
        stage.Settle(10.0);
        Assert.Equal((0, false), stage.Machine(SetPiece.Heal, 10.0));
        Assert.False(stage.MachineAtWork(10.0));

        // A PC switched on is on, one switched off is dark
        stage.Play(SetPiece.PcOn, 0, 10.0, (7, 3));
        stage.Settle(10.0);
        Assert.Equal((7, 3, SetPieces.PcLit), stage.Screen(10.0));
        stage.Play(SetPiece.PcOff, 0, 10.0);
        stage.Settle(10.0);
        Assert.Null(stage.Screen(10.0));

        // A ride is over and nobody is shaken or unseen
        var someone = new NPC();
        stage.Play(SetPiece.ElevatorUp, 4, 10.0, (2, 2));
        stage.Shake(someone, 8, 10.0);
        stage.Flicker(null, 6, 10.0);
        stage.Settle(10.0);
        Assert.Equal((0, 0, 0), (stage.Indicator(10.0), stage.Shudder(10.0), stage.OffsetOf(someone, 10.05)));
        Assert.True(stage.Seen(null, 10.1));
        Assert.False(stage.AnyoneMoved);

        // Nothing of it is heard, then or later
        stage.TakeCues(60.0, cues);
        Assert.Empty(cues);
    }

    [Fact]
    public void AnEffectThatPassesIsGoneAndOneThatIsHeldStays()
    {
        var map = new Map(8, 8);
        var life = new FieldLife();
        life.Effect(map, SceneEffect.Burst, 2, 2, 0.6f, Direction.Right, hold: false);
        life.Effect(map, SceneEffect.Rift, 3, 3, 3f, Direction.Right, hold: true);
        life.Effect(map, SceneEffect.Beam, 4, 4, 1.5f, Direction.Right, hold: true);
        life.StopEffect(4, 4);
        Assert.Equal(3, life.EffectCount);

        life.EndPassingEffects();
        // The burst and the beam that was stopped are gone; the rift a scene holds is still lit
        Assert.Equal(1, life.EffectCount);
        life.StopEffect(3, 3);
        life.EndPassingEffects();
        Assert.Equal(0, life.EffectCount);
    }

    [Fact]
    public void AScriptsFadeIsWhereItWasGoing()
    {
        var fade = new ScreenFade();
        fade.To(true, 0.4f);
        fade.Update(0.1f);
        Assert.True(fade.IsMoving);
        fade.Finish();
        Assert.Equal((1f, false), (fade.Level, fade.IsMoving));
        fade.To(false, 0.4f);
        fade.Finish();
        Assert.Equal(0f, fade.Level);
        // One that was never told anything stays as it is
        var still = new ScreenFade();
        still.Finish();
        Assert.Equal(0f, still.Level);
    }

    [Fact]
    public void NothingIsHeardWhileHushed()
    {
        var heard = AudioManager.Listen();
        try
        {
            AudioManager.PlaySound("select");
            AudioManager.Hushed = true;
            AudioManager.PlaySound("bump");
            AudioManager.PlayCry(PokemonDatabase.Get("Starly")!);
            AudioManager.PlayFanfare(PokemonPlatinumEngine.Audio.MusicRole.FanfareItem);
            AudioManager.Hushed = false;
            AudioManager.PlaySound("cancel");
            Assert.Equal(new[] { "select", "cancel" }, heard);
        }
        finally
        {
            AudioManager.Hushed = false;
            AudioManager.StopListening();
        }
    }

    [Fact]
    public void SomeoneCutShortOnTheirWalkStandsWhereTheStepsEnd()
    {
        // The host in a hurry puts whoever walks at the end of their walk with the game's own walk; a walk played a
        // frame at a time ends on the same tile, facing the same way, standing still
        var steps = new[]
        {
            new ScriptStep(Direction.Right), new ScriptStep(Direction.Right), new ScriptStep(Direction.Down, ScriptStepKind.Back),
            new ScriptStep(Direction.Left, ScriptStepKind.OnSpot), new ScriptStep(Direction.Up)
        };
        var played = new NPC { GridX = 3, GridY = 3, Facing = Direction.Down };
        var walk = new NpcWalk(played, steps, fast: false);
        for (int i = 0; i < 600 && !walk.IsDone; i++) walk.Update(1f / 60f);
        Assert.True(walk.IsDone);

        var skipped = new NPC { GridX = 3, GridY = 3, Facing = Direction.Down };
        new NpcWalk(skipped, steps, fast: false).Finish();
        Assert.Equal((played.GridX, played.GridY, played.Facing), (skipped.GridX, skipped.GridY, skipped.Facing));
        Assert.Equal((5, 3, Direction.Up), (skipped.GridX, skipped.GridY, skipped.Facing));
        Assert.Equal((0f, 0f, 0f), (skipped.StepOffsetX, skipped.StepOffsetY, skipped.WalkBlend));
    }

    [Fact]
    public void AHurryLetsItsWaitsPassForWhoeverWalksAndHasTheWalksArriveWhereTheSceneWaits()
    {
        // The field's host walks people a frame at a time, and where the player's own walk ends can hang on who stood
        // where when: someone sent ahead before a pause must be as far ahead in a hurry, and a walk the scene waits
        // for must be over before the next line turns anyone. The runner tells the host both, in the scene's order.
        // (Found by the harness's `skips` mode: a hurry that only dropped its waits left the player facing the way of
        // their last step, where the scene played turns them afterwards.)
        var (runner, host) = Ready("""
            script Lead
              scene "The way round"
              move rival right 2
              wait 0.5
              walk player right 1
              emote rival exclaim 0.75
              camera pan 4 4 0.4
              move rival down 1
              waitmoves
              face rival up
              endscene
              wait 2
              walk player left 1
            """, before: h => h.Map = Room());
        var rival = host.Map!.FindPerson("rival", null)!;
        runner.Impatient = true;
        runner.Update(0f);

        // The pause, the bubble's seconds and the camera's move passed for the walkers; the walk and the waitmoves
        // had them arrive
        Assert.Equal(new[] { 0.5f, 0.75f, 0.4f }, host.Elapsed);
        Assert.Equal(2, host.Arrivals);
        Assert.Equal((4, 3, Direction.Up), (rival.GridX, rival.GridY, rival.Facing));
        // After the scene nothing is hurried: the pause is waited, and the host is told of no more time or arrivals
        Assert.False(runner.Hurrying);
        Assert.True(runner.IsRunning);
        runner.Update(2.5f);
        Assert.False(runner.IsRunning);
        Assert.Equal(3, host.Elapsed.Count);
        Assert.Equal(2, host.Arrivals);
    }

    // ------------------------------------------------------------------ the game's own scenes

    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    // A walk of this class's own: playing scenes walks people about and takes them off the maps
    private static readonly ScriptWalk Walk = new();

    /// <summary>The opening's scenes (plan 02 · S4), which Q20 wrapped first: each must be marked.</summary>
    public static readonly string[] OpeningScenes =
    {
        "PlayerHouse2F.The television's special", "PlayerHouse2F.The rival bursts in", "PlayerHouse.The rival has been",
        "twinleaf_town.The rival runs out", "RivalHouse2F.The rival's room",
        "route_201.The professor", "route_201.The briefcase", "PlayerHouse.The Running Shoes", "route_201.Off to the lake",
        "lake_verity_low_water.The man in grey", "sandgem_town.The assistant", "RowanLab.The Pokedex", "sandgem_town.The tour",
        "PlayerHouse.The parcel", "route_202.The catching lesson",
    };

    [Fact]
    public void TheGamesScriptsMarkTheirScenes()
    {
        var scenes = Scripts.Scenes;
        Assert.DoesNotContain(OpeningScenes, scene => !scenes.Contains(scene));
        // None is marked in what every place shares: a scene is some place's own
        Assert.DoesNotContain(scenes, scene => scene.StartsWith(ScriptLibrary.Common + ".", StringComparison.Ordinal));
        // And the library finds nothing wrong with any (a scene left open, one inside another, two of a name)
        Assert.DoesNotContain(Scripts.Problems(), problem => problem.Contains("scene", StringComparison.Ordinal));
    }

    [Fact]
    public void EverySceneOfTheGamePlayedAndSkippedLeavesTheSame()
    {
        int scripts = 0, ways = 0, skips = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scene in Walk.PlayEveryScene(seen))
        {
            scripts++;
            string name = $"{scene.Script.FullName}, started by {scene.What}, given {scene.Given}";
            Assert.True(scene.Played.Count == scene.Skipped.Count, $"{name}: {scene.Played.Count} ways through it played, {scene.Skipped.Count} skipped");
            for (int k = 0; k < scene.Played.Count; k++)
            {
                var (a, b) = (scene.Played[k], scene.Skipped[k]);
                string way = $"{name}, way {(a.Answers.Count == 0 ? "-" : string.Join(".", a.Answers))}";
                ways++;
                skips += b.Host.Skips;

                // Nothing a hurry reached waited for the player, and nobody walked anywhere a played scene doesn't walk them
                Assert.True(a.Host.Problems.SequenceEqual(b.Host.Problems), $"{way}: skipped, {string.Join("; ", b.Host.Problems.Except(a.Host.Problems))}");
                // The same questions, with the same answers, and the same battles and screens in the same order
                Assert.True(a.Answers.SequenceEqual(b.Answers), $"{way}: the skipped way took the answers {string.Join(".", b.Answers)}");
                Assert.True(a.Host.Asked.SequenceEqual(b.Host.Asked), $"{way}: asked {string.Join(" | ", a.Host.Asked)} when played and {string.Join(" | ", b.Host.Asked)} when skipped");
                Assert.Equal(WhatWaited(a.Host), WhatWaited(b.Host));
                // The same story: flags, variables, trainers beaten, items taken, badges, starters; the same scenes seen
                Assert.True(JsonSerializer.Serialize(a.Host.Story.Snapshot()) == JsonSerializer.Serialize(b.Host.Story.Snapshot()),
                    $"{way}: the story differs:\n  played : {string.Join(", ", Golden.GoldenScriptTests.Changes(a.Before, a.Host.Story.Snapshot()))}\n  skipped: {string.Join(", ", Golden.GoldenScriptTests.Changes(b.Before, b.Host.Story.Snapshot()))}");
                Assert.Equal(a.Host.Story.ScenesSeen.Order(StringComparer.Ordinal), b.Host.Story.ScenesSeen.Order(StringComparer.Ordinal));
                // The same bag, team, money, map, tiles and facings, who is on the map, and what is left on the screen
                Assert.True(a.Left == b.Left, $"{way}: left\n{a.Left}when played and\n{b.Left}when skipped");
            }
        }

        // Every scene the scripts mark was played through to its end on some way, and so was skipped to it
        Assert.True(scripts >= OpeningScenes.Length && skips > 0 && Environment.GetEnvironmentVariable("SCENES_TOLD") != "1",
            $"{scripts} plays of scripts with scenes, {ways} ways through them and {skips} hurries; {seen.Count} of {Scripts.Scenes.Count} scenes played to their end");
        var never = Scripts.Scenes.Where(scene => !seen.Contains(scene) && !BeyondTheWalk.Contains(scene)).Order(StringComparer.Ordinal).ToList();
        Assert.True(never.Count == 0, $"no way through its script plays these scenes to their endscene: {string.Join(", ", never)}");
        // ... and a scene the walk has come to reach has no business on the list of those it can't
        Assert.DoesNotContain(BeyondTheWalk, seen.Contains);
    }

    /// <summary>
    /// Scenes the walk of every script can't play to their end, each with why: it begins every script with a new
    /// game's story given one or two of the things the script asks about, and plays a script only as something of a
    /// map starts it. Whatever is named here is played both ways by its own chapter's test instead, or not at all.
    /// </summary>
    private static readonly HashSet<string> BeyondTheWalk = new(StringComparer.Ordinal)
    {
    };

    /// <summary>What waited for the player on a way through a script, in order: the battles and the screens the host's log names.</summary>
    private static List<string> WhatWaited(HeadlessScriptHost host) => host.Log.Where(entry =>
        entry.StartsWith("battle ", StringComparison.Ordinal) || entry.StartsWith("wildbattle ", StringComparison.Ordinal)
        || entry.StartsWith("catchinglesson ", StringComparison.Ordinal) || entry.StartsWith("open ", StringComparison.Ordinal)
        || entry.StartsWith("save", StringComparison.Ordinal) || entry.StartsWith("warp ", StringComparison.Ordinal)
        || entry.StartsWith("scene ", StringComparison.Ordinal) || entry == "endscene").ToList();
}
