using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The script language (plan 02 · S1): how a file is read, and what the runner does with each line. Scripts run
/// here in a host with no screen, where every message is confirmed as it comes, so a test plays one to its end
/// and looks at what was said and what changed.
/// </summary>
public class ScriptTests
{
    /// <summary>A file of scripts called "test" and a common one, with the first script of "test" ready to run.</summary>
    private static (ScriptRunner Runner, HeadlessScriptHost Host) Ready(string source, string common = "", NPC? subject = null, IReadOnlyList<string>? own = null)
    {
        var library = ScriptLibrary.FromSources(("test", source), ("common", common));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), subject, own);
        return (runner, host);
    }

    private static HeadlessScriptHost Run(string source, Action<HeadlessScriptHost>? before = null, NPC? subject = null, string common = "")
    {
        var (runner, host) = Ready(source, common, subject);
        before?.Invoke(host);
        runner.RunToEnd();
        return host;
    }

    private static List<string> Said(HeadlessScriptHost host) => host.Transcript.Select(t => t.Text).ToList();

    private static NPC Trainer(string id = "tester", string name = "Tess", int prize = 120) => MapFile.BuildNpc(new MapFile.NpcRecord
    {
        Id = id,
        Name = name,
        NpcType = "Lass",
        X = 3,
        Y = 3,
        Dialog = new List<string> { "I like shorts." },
        Trainer = new MapFile.TrainerRecord
        {
            Id = id,
            Name = name,
            TrainerClass = "Lass",
            Party = new List<MapFile.PartyMember> { new() { Species = "Bidoof", Level = 3 } },
            PrizeMoney = prize,
            DialogueBefore = "Let's battle!",
            DialogueAfter = "You're strong."
        }
    }, "Test");

    // ------------------------------------------------------------------ reading a file

    [Fact]
    public void AFileIsScriptsOfLinesWithLabelsAndRemarks()
    {
        var scripts = ScriptParser.Parse("town", """
            # A remark before anything
            script Greeter      # and one after a line
              say "Hello!" "A \"quoted\" word, and a # that is no remark."
              if flag FLAG_MET goto Again
              setflag FLAG_MET
              end
            label Again
              say "You again."

            script Other
              end
            """);

        Assert.Equal(new[] { "Greeter", "Other" }, scripts.Select(s => s.Name));
        var greeter = scripts[0];
        Assert.Equal("town.Greeter", greeter.FullName);
        Assert.Equal(new[] { Op.Say, Op.If, Op.SetFlag, Op.End, Op.Say }, greeter.Code.Select(i => i.Op));
        Assert.Equal(new[] { "Hello!", "A \"quoted\" word, and a # that is no remark." }, greeter.Code[0].Lines);
        // The label is the place of the line after it, and the jump knows it
        Assert.Equal(4, greeter.Labels["Again"]);
        Assert.Equal(4, greeter.Code[1].Then!.Target);
        Assert.Equal(3, greeter.Code[0].Line);
    }

    /// <summary>One line for every command the language has: a new one without a line here has no reader, or no test.</summary>
    private const string EveryCommand = """
        script All
          say "a"
          text "b"
          sayown
          trainerline before
          speaker "Someone"
          speaker none
          speaker self
          ask "Well?"
          choose "Which?" "This" "That" "Neither"
          if flag FLAG_A goto Here
          goto Here
        label Here
          call Other
          return
          end
          setflag FLAG_A
          clearflag FLAG_A
          setvar VAR_A 3
          addvar VAR_A -1
          give "Potion" 2
          find "Potion"
          find own
          setflag own
          additem "Potion"
          take "Potion"
          givepokemon "Starly" 4
          givebadge coal
          givemoney 100
          takemoney 50
          heal
          greetings clear
          battle self canlose
          battle self and twin with "cheryl_eterna_forest" canlose
          battle self with helper
          battle self first
          battle self as "rival_route_201_piplup" first canlose
          wildbattle "Starly" 2
          wildbattle "Giratina" 47 nofleeing
          catchinglesson "Bidoof" 2
          face self player
          face player up
          walk player up 2 left fast
          move self down
          waitmoves
          emote self exclaim 0.5
          show self
          hide self
          place self 3 4 down
          warp "PlayerHouse" 4 5 up
          fade out 0.2
          fade in
          wait 0.5
          camera pan 3 4 0.5
          camera release
          camera shake
          usemove "Cut"
          surf
          climb
          fly
          teleport
          escape
          sweetscent
          poketch on
          poketchapp PartyStatus
          safari start
          safari end
          turnback
          defeat "lass_caroline"
          flowerclock
          pressbutton blue
          gearbutton reverse
          partner cheryl "cheryl_eterna_forest"
          partner off
          choosepokemon
          trade kazza
          halloffame
          pc halloffame
          music "sinnoh/jubilife"
          music area
          music stop
          fanfare item
          sound "select"
          cry "Giratina-Origin"
          cry own
          starter
          shop
          wardrobe "jubilife"
          pc
          travel
          honeytree status
          honeytree slather
          honeytree battle
          swarms on
          trophygarden
          roamer start "Mesprit"
          survivepoison VAR_A
          lottery draw
          lottery boxed
          chooseitem berries
          berry status
          poffin check
          poffin cook
          poffin room
          poffin give 60 30 30 30 30 40
        script Other
          end
        """;

    [Fact]
    public void EveryCommandHasItsLine()
    {
        var all = ScriptParser.Parse("test", EveryCommand)[0];
        var read = all.Everything().Select(i => i.Op).ToHashSet();
        Assert.DoesNotContain(Enum.GetValues<Op>(), op => !read.Contains(op));
    }

    [Fact]
    public void EveryQuestionAnIfCanAskIsRead()
    {
        var script = ScriptParser.Parse("test", """
            script Asks
              if flag FLAG_A end
              if not flag FLAG_A end
              if var VAR_A == 2 end
              if var VAR_A >= VAR_B end
              if var PLAYER_X < 10 end
              if badge forest end
              if badges >= 2 end
              if item "Potion" end
              if item "Potion" >= 3 end
              if party < 6 end
              if knows "Cut" end
              if has "Starly" end
              if yes end
              if no end
              if won end
              if lost end
              if result != 1 end
              if defeated self end
              if defeated "roark" end
              if rematch self end
              if taken "item_potion" end
              if starter "Piplup" end
              if money >= 500 end
              if facing left end
              if boy end
              if girl end
              if poketch end
              if poketchapp MemoPad end
              if pokerus end
              if safari end
              if partner end
              if time morning end
              if weekday friday end
            """)[0];

        var asked = script.Code.Select(i => i.Condition!.Query).ToHashSet();
        Assert.DoesNotContain(Enum.GetValues<Query>(), q => !asked.Contains(q));
        Assert.True(script.Code[1].Condition!.Negated);
        Assert.Equal((Compare.GreaterOrEqual, "VAR_B"), (script.Code[3].Condition!.Compare, script.Code[3].Condition!.Other));
        Assert.Equal((Compare.GreaterOrEqual, 1), (script.Code[7].Condition!.Compare, script.Code[7].Condition!.Number));
        Assert.Equal((Compare.GreaterOrEqual, 3), (script.Code[8].Condition!.Compare, script.Code[8].Condition!.Number));
    }

    [Theory]
    [InlineData("say \"x\"", "outside any script")]
    [InlineData("script A\n dance", "no command 'dance'")]
    [InlineData("script A\n say \"unclosed", "closing quote")]
    [InlineData("script A\n say hello", "not in quotes")]
    [InlineData("script A\n goto Nowhere", "no label 'Nowhere'")]
    [InlineData("script A\n end\nscript A", "two scripts called 'A'")]
    [InlineData("script A\nlabel L\nlabel L", "two labels called 'L'")]
    [InlineData("script A\n setvar VAR_A many", "not a number")]
    [InlineData("script A\n setflag met_rival", "not a flag's name")]
    [InlineData("script A\n setvar RESULT 1", "kept by the game")]
    [InlineData("script A\n if flag FLAG_A", "nothing to do")]
    [InlineData("script A\n if flag FLAG_A if flag FLAG_B end", "can't guard another")]
    [InlineData("script A\n if weather rain end", "nothing an 'if' can ask")]
    [InlineData("script A\n walk player", "needs steps")]
    [InlineData("script A\n walk up 2", "a direction where a person belongs")]
    [InlineData("script A\n end now", "more than the line needs")]
    [InlineData("script A\n emote self none", "no bubble")]
    [InlineData("script A\n givepokemon \"Starly\" 101", "1 to 100")]
    [InlineData("script A\n givebadge marsh", "not a badge")]
    [InlineData("script A\n choose \"Which?\" \"Only one\"", "at least two answers")]
    [InlineData("script A\n fade sideways", "'out' or 'in'")]
    [InlineData("script A\n wait soon", "not a time")]
    [InlineData("script A\n hide player", "never hidden")]
    [InlineData("script A\n battle player", "can't be battled")]
    [InlineData("script A\n give \"Potion\" 0", "one at a time at least")]
    public void WhatIsWrittenWrongIsToldWithItsLine(string source, string telling)
    {
        var wrong = Assert.Throws<ScriptException>(() => ScriptParser.Parse("town", source));
        Assert.Contains(telling, wrong.Message);
        Assert.StartsWith($"town.txt({source.Split('\n').Length})", wrong.Message);
    }

    // ------------------------------------------------------------------ what is said

    [Fact]
    public void LinesOneAfterAnotherAreOneTalk()
    {
        var host = Run("""
            script S
              say "One."
              say "Two." "Three."
              setflag FLAG_BETWEEN
              say "Four."
            """);

        Assert.Equal(new[] { "One.", "Two.", "Three.", "Four." }, Said(host));
        // The box opened twice: the flag was set between the two talks
        Assert.Equal(2, host.Talks);
    }

    [Fact]
    public void WhoSpeaksIsWhoeverTheScriptBelongsToUntilItSaysOtherwise()
    {
        var host = Run("""
            script S
              say "Mine."
              speaker "Rowan"
              say "His."
              text "Nobody's."
              speaker none
              say "Nobody's either."
              speaker self
              say "Mine again."
            """, subject: Trainer(name: "Tess"));

        Assert.Equal(new string?[] { "Tess", "Rowan", null, null, "Tess" }, host.Transcript.Select(t => t.Speaker));
    }

    [Fact]
    public void APersonsOwnLinesAndATrainersAreSaidByName()
    {
        var host = Run("""
            script S
              sayown
              trainerline before
              trainerline after
            """, subject: Trainer());

        Assert.Equal(new[] { "I like shorts.", "Let's battle!", "You're strong." }, Said(host));
    }

    [Fact]
    public void TextIsFilledInAsItIsSaid()
    {
        var host = Run("""
            script S
              setvar VAR_COUNT 7
              additem "Great Ball" 2
              say "{player}, meet {assistant}." "{var:VAR_COUNT} of them, and {var:MONEY} in your purse."
              say "{lead} looks well. That {item} is yours, says {self}."
            """, h =>
        {
            h.PlayerName = "Ana";
            h.PlayerLook = PlayerLook.Girl;
            h.Money = 250;
            h.Party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 5) { Nickname = "Pip" });
        }, Trainer(name: "Tess"));

        Assert.Equal(new[]
        {
            "Ana, meet Lucas.", "7 of them, and 250 in your purse.", "Pip looks well. That Great Ball is yours, says Tess."
        }, Said(host));
    }

    // ------------------------------------------------------------------ where a script goes

    [Fact]
    public void AFlagDecidesWhichWayAScriptGoes()
    {
        const string source = """
            script S
              if flag FLAG_MET goto Again
              say "Pleased to meet you."
              setflag FLAG_MET
              end
            label Again
              say "Hello again."
            """;

        var first = Run(source);
        Assert.Equal(new[] { "Pleased to meet you." }, Said(first));
        Assert.True(first.Story.Has("FLAG_MET"));

        var second = Run(source, h => h.Story.Set("FLAG_MET"));
        Assert.Equal(new[] { "Hello again." }, Said(second));
    }

    [Fact]
    public void AQuestionIsAnsweredYesOrNo()
    {
        const string source = """
            script S
              ask "Ready?"
              if yes say "Good."
              if no say "Later, then."
              if not yes setflag FLAG_SAID_NO
            """;

        var yes = Run(source, h => h.Answers.Enqueue(0));
        Assert.Equal(new[] { "Ready?", "Good." }, Said(yes));
        Assert.Equal(("Ready?", "Yes"), yes.Asked.Single());

        var no = Run(source, h => h.Answers.Enqueue(1));
        Assert.Equal(new[] { "Ready?", "Later, then." }, Said(no));
        Assert.True(no.Story.Has("FLAG_SAID_NO"));
    }

    [Fact]
    public void AMenuGivesThePlaceOfTheAnswerPicked()
    {
        const string source = """
            script S
              choose "Which one?" "Red" "Green" "Never mind"
              if result == 0 say "Red it is."
              if result == 1 say "Green it is."
              if result == 2 say "As you like."
              setvar VAR_PICKED 10
              addvar VAR_PICKED 1
            """;

        var green = Run(source, h => h.Answers.Enqueue(1));
        Assert.Equal("Green it is.", Said(green).Last());
        Assert.Equal(11, green.Story.Var("VAR_PICKED"));
        Assert.Equal("As you like.", Said(Run(source, h => h.Answers.Enqueue(2))).Last());
    }

    [Fact]
    public void BackingOutOfAMenuPicksItsLastAnswerAndOutOfAQuestionNo()
    {
        var library = ScriptLibrary.FromSources(("test", "script S\n ask \"Sure?\"\n choose \"Which?\" \"A\" \"B\" \"Leave\""));
        var cancels = new List<int>();
        var host = new Asking(cancels);
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.Single());
        runner.RunToEnd();

        Assert.Equal(new[] { 1, 2 }, cancels);
    }

    [Fact]
    public void AScriptCallsAnotherAndComesBack()
    {
        var host = Run("""
            script S
              say "Before."
              call Local
              call Shared
              say "After."
              call Stops
              say "Never."
            script Local
              say "Mine."
              return
              say "Skipped."
            script Stops
              say "The end."
              end
            """, common: """
            script Local
              say "The common one, which the place's own hides."
            script Shared
              say "Shared."
            """);

        Assert.Equal(new[] { "Before.", "Mine.", "Shared.", "After.", "The end." }, Said(host));
    }

    [Fact]
    public void VariablesAreComparedWithNumbersAndWithOneAnother()
    {
        var host = Run("""
            script S
              setvar VAR_A 3
              setvar VAR_B 5
              if var VAR_A < VAR_B say "A is less."
              if var VAR_A >= 3 say "A is three or more."
              if var VAR_NEVER_SET == 0 say "Unset is nought."
              if var PLAYER_X == 12 say "Twelve across."
              if var PARTY_COUNT == 0 say "Nobody yet."
              if var BADGE_COUNT > 0 say "Not said."
            """, h => h.PlayerTile = (12, 4));

        Assert.Equal(new[] { "A is less.", "A is three or more.", "Unset is nought.", "Twelve across.", "Nobody yet." }, Said(host));
    }

    [Fact]
    public void AScriptThatGoesRoundInCirclesIsStopped()
    {
        var (spinning, _) = Ready("script S\nlabel Again\n goto Again");
        var caught = Assert.Throws<ScriptException>(() => spinning.RunToEnd());
        Assert.Contains("round in circles", caught.Message);

        // One that pauses each time round never ends either, and is told so
        var (pausing, _) = Ready("script S\nlabel Again\n wait 1\n goto Again");
        Assert.Contains("never ends", Assert.Throws<ScriptException>(() => pausing.RunToEnd()).Message);
    }

    // ------------------------------------------------------------------ what a script waits for

    [Fact]
    public void AScriptWaitsForThePlayerAndGoesOnWhenTheyAreDone()
    {
        var (runner, host) = Ready("""
            script S
              setflag FLAG_ONE
              say "Read me."
              setflag FLAG_TWO
              walk player up
              setflag FLAG_THREE
              wait 2
              setflag FLAG_FOUR
            """);

        // Text on the screen: nothing past it happens until it is gone
        host.Pauses = true;
        runner.Update(0.016f);
        Assert.True(host.Busy);
        Assert.True(host.Story.Has("FLAG_ONE"));
        Assert.False(host.Story.Has("FLAG_TWO"));
        runner.Update(5f);
        Assert.False(host.Story.Has("FLAG_TWO"));

        // A walk: the line after it waits until nobody is walking
        host.Busy = false;
        runner.Update(0.016f);
        Assert.True(host.Walking);
        Assert.True(host.Story.Has("FLAG_TWO"));
        Assert.False(host.Story.Has("FLAG_THREE"));

        // A pause is counted in the time that passes
        host.Walking = false;
        runner.Update(0.016f);
        Assert.True(host.Story.Has("FLAG_THREE"));
        runner.Update(1.5f);
        Assert.False(host.Story.Has("FLAG_FOUR"));
        Assert.True(runner.IsRunning);
        runner.Update(0.6f);
        Assert.True(host.Story.Has("FLAG_FOUR"));
        Assert.False(runner.IsRunning);
    }

    [Fact]
    public void AWalkSetGoingIsOnlyWaitedForWhenAsked()
    {
        var (runner, host) = Ready("""
            script S
              move player up 3
              setflag FLAG_MEANWHILE
              waitmoves
              setflag FLAG_AFTER
            """);
        host.Pauses = true;
        runner.Update(0.016f);

        Assert.True(host.Walking);
        Assert.True(host.Story.Has("FLAG_MEANWHILE"));
        Assert.False(host.Story.Has("FLAG_AFTER"));
        host.Walking = false;
        runner.Update(0.016f);
        Assert.True(host.Story.Has("FLAG_AFTER"));
    }

    // ------------------------------------------------------------------ giving and taking

    [Fact]
    public void AnItemGivenGoesInTheBagWithItsFanfareAndItsWords()
    {
        var host = Run("""
            script S
              give "Potion"
              give "Poké Ball" 5
            """, h => h.PlayerName = "Ana");

        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Potion")!));
        Assert.Equal(5, host.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.Equal(2, host.Log.Count(l => l == $"fanfare {MusicRole.FanfareItem}"));
        Assert.Equal(new[]
        {
            "Ana received the Potion!", "Ana put it away in the Medicine pocket.",
            "Ana received 5 × Poké Ball!", "Ana put them away in the Poké Balls pocket."
        }, Said(host));
    }

    [Fact]
    public void AnItemFoundIsToldInOtherWordsAndATmSaysWhatItHolds()
    {
        var host = Run("""
            script S
              find "Rare Candy"
              find "Poké Ball" 3
              find "TM70"
            """, h => h.PlayerName = "Ana");

        Assert.Equal(new[]
        {
            "Ana found the Rare Candy!", "Ana put it away in the Medicine pocket.",
            "Ana found 3 × Poké Ball!", "Ana put them away in the Poké Balls pocket.",
            "Ana found the TM70!", "TM70 holds the move Flash.", "Ana put it away in the TMs & HMs pocket."
        }, Said(host));
        // A TM is received to its own fanfare
        Assert.Equal(2, host.Log.Count(l => l == $"fanfare {MusicRole.FanfareItem}"));
        Assert.Equal(1, host.Log.Count(l => l == $"fanfare {MusicRole.FanfareTM}"));
        Assert.Equal(3, host.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
    }

    [Fact]
    public void AScriptsOwnItemAndFlagAreWhatItWasStartedWith()
    {
        // A ball's: what the ball holds, and the flag that hides it
        var ball = new NPC { NpcType = NPC.ItemBallType, Name = "Great Ball", Item = "Great Ball", ItemCount = 2, HiddenBy = "FLAG_OBTAINED_TEST_GREAT_BALL" };
        var library = ScriptLibrary.FromSources(("test", "script S\n setflag own\n find own\nscript Undo\n clearflag own\n additem own"));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("test.S")!, ball);
        runner.RunToEnd();

        Assert.True(host.Story.Has("FLAG_OBTAINED_TEST_GREAT_BALL"));
        Assert.Equal(2, host.Bag.GetQuantity(ItemDatabase.Get("Great Ball")!));
        Assert.Equal($"{host.PlayerName} found 2 × Great Ball!", host.Transcript[0].Text);

        // Given outright: a hidden item has nobody, only an item and a flag
        runner.Start(library.Find("test.Undo")!, item: ("Stardust", 1), flag: "FLAG_OBTAINED_TEST_GREAT_BALL");
        runner.RunToEnd();
        Assert.False(host.Story.Has("FLAG_OBTAINED_TEST_GREAT_BALL"));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Stardust")!));

        // A script nothing of the kind started has neither, and says so
        runner.Start(library.Find("test.S")!, Trainer());
        Assert.Contains("no flag of its own", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        var finding = ScriptLibrary.FromSources(("test", "script S\n find own"));
        var alone = new ScriptRunner(finding, new HeadlessScriptHost());
        alone.Start(finding.All.Single());
        Assert.Equal("test.txt(2): this script has no item of its own: no item ball and no hidden item started it.", Assert.Throws<ScriptException>(() => alone.RunToEnd()).Message);
    }

    [Fact]
    public void APokemonOfTheMapCriesAsItself()
    {
        // The mine's Machop: its cry, then its lines (plan 10 · F1)
        var machop = new NPC { NpcType = NPC.PokemonType, Name = "Machop", Species = "Machop", DialogLines = { "Maaa... CHOP!" } };
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Assert.Equal(FieldScripts.Pokemon, FieldScripts.For(machop));
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Pokemon)!, machop);
        runner.RunToEnd();
        Assert.Contains("cry Machop", host.Log);
        Assert.Equal("Maaa... CHOP!", Assert.Single(host.Transcript).Text);

        // A form cries as its form; a script that belongs to nobody has no cry of its own
        var origin = new NPC { NpcType = NPC.PokemonType, Name = "Giratina", Species = "Giratina-Origin" };
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Pokemon)!, origin);
        runner.RunToEnd();
        Assert.Contains("cry Giratina-Origin", host.Log);
        var crying = ScriptLibrary.FromSources(("test", "script S\n cry own"));
        var alone = new ScriptRunner(crying, new HeadlessScriptHost());
        alone.Start(crying.All.Single(), Trainer());
        Assert.Contains("no cry of its own", Assert.Throws<ScriptException>(() => alone.RunToEnd()).Message);
    }

    [Fact]
    public void ItemsAreTakenAllOrNotAtAll()
    {
        var host = Run("""
            script S
              additem "Coupon 1" 2
              if item "Coupon 1" >= 2 say "Two coupons."
              take "Coupon 1" 3
              if result == 0 say "Not three, so none are taken."
              take "Coupon 1" 2
              if result == 1 say "Two taken."
              if not item "Coupon 1" say "None left."
            """);

        Assert.Equal(new[] { "Two coupons.", "Not three, so none are taken.", "Two taken.", "None left." }, Said(host));
        Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get("Coupon 1")!));
        // Putting an item in the bag without a word plays nothing
        Assert.DoesNotContain(host.Log, l => l.StartsWith("fanfare"));
    }

    [Fact]
    public void APokemonGivenJoinsTheTeamOrGoesToThePcWhenItIsFull()
    {
        var host = Run("""
            script S
              givepokemon "Starly" 4
              if result == 1 say "On the team."
              givepokemon "Bidoof" 3
              if result == 2 say "Sent to the PC."
              if has "Starly" say "A Starly is with you."
              if party == 6 say "Six."
            """, h =>
        {
            for (int i = 0; i < 5; i++) h.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 3));
        });

        Assert.Equal(new[] { "On the team.", "Sent to the PC.", "A Starly is with you.", "Six." }, Said(host));
        Assert.Equal(("Starly", 4), (host.Party.Members[5].Species.Name, host.Party.Members[5].Level));
        Assert.Equal("Bidoof", host.Box.Single().Species.Name);
    }

    [Fact]
    public void BadgesAndMoneyAreGivenAndAskedAbout()
    {
        var host = Run("""
            script S
              if not badge coal say "No Coal Badge yet."
              givebadge coal
              givebadge forest
              if badge coal say "The Coal Badge!"
              if badges >= 2 say "Two badges."
              givemoney 500
              takemoney 2000
              if result == 0 say "Can't pay that."
              takemoney 300
              if result == 1 say "Paid."
              if money == 1200 say "1200 left."
            """, h => h.Money = 1000);

        Assert.Equal(new[] { "No Coal Badge yet.", "The Coal Badge!", "Two badges.", "Can't pay that.", "Paid.", "1200 left." }, Said(host));
        Assert.Equal(0b11, host.Story.BadgeMask);
    }

    [Fact]
    public void TheTeamIsHealed()
    {
        var host = Run("script S\n heal", h =>
        {
            var hurt = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5);
            hurt.CurrentHP = 1;
            h.Party.Add(hurt);
        });

        Assert.Equal(host.Party.Members[0].MaxHP, host.Party.Members[0].CurrentHP);
    }

    // ------------------------------------------------------------------ battles

    private const string BattleScript = """
        script S
          if defeated self goto Beaten
          trainerline before
          battle self
          setflag FLAG_AFTER_THE_BATTLE
          say "Well fought."
          end
        label Beaten
          trainerline after
        """;

    [Fact]
    public void ABattleWonGoesOnAndTheTrainerIsBeatenFromThenOn()
    {
        var trainer = Trainer(prize: 120);
        var host = Run(BattleScript, h => h.Money = 1000, trainer);

        Assert.Equal(new[] { "Let's battle!", "Well fought." }, Said(host));
        Assert.True(trainer.HasBattled);
        Assert.True(host.Story.HasDefeated("tester"));
        Assert.Equal(1120, host.Money);

        // Spoken to again, they have only their last word
        var (again, second) = Ready(BattleScript, subject: trainer);
        again.RunToEnd();
        Assert.Equal(new[] { "You're strong." }, Said(second));
    }

    [Fact]
    public void ABattleLostEndsTheScriptThere()
    {
        var trainer = Trainer();
        var (runner, host) = Ready(BattleScript, subject: trainer);
        host.Fight = _ => BattleOutcome.Lost;
        runner.RunToEnd();

        Assert.Equal(new[] { "Let's battle!" }, Said(host));
        Assert.False(host.Story.Has("FLAG_AFTER_THE_BATTLE"));
        Assert.True(runner.EndedInDefeat);
        Assert.False(trainer.HasBattled);
    }

    /// <summary>
    /// A battle with no Pokémon of the player's able to fight is never started: a line says why and the script ends
    /// there, the trainer still unbeaten and whatever came after the battle still to come. A game whose first Pokémon
    /// went missing battled anyway, and every battle ended the game.
    /// </summary>
    [Fact]
    public void NoBattleIsStartedWithoutAPokemonToFightIt()
    {
        var trainer = Trainer();
        var (runner, host) = Ready(BattleScript, subject: trainer);
        host.NeedsPokemon = true;
        runner.RunToEnd();

        Assert.Equal(new[] { "Let's battle!", $"{host.PlayerName} has no Pokémon that can battle!" }, Said(host));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle"));
        Assert.False(host.Story.Has("FLAG_AFTER_THE_BATTLE"));
        Assert.False(host.Story.HasDefeated("tester"));
        Assert.False(runner.EndedInDefeat);

        // A wild Pokémon put in the player's way waits too, and is met once the player has a Pokémon
        const string wild = "script S\n wildbattle \"Starly\" 2\n setflag FLAG_MET";
        var alone = Run(wild, h => h.NeedsPokemon = true);
        Assert.DoesNotContain(alone.Log, l => l.StartsWith("wildbattle"));
        Assert.False(alone.Story.Has("FLAG_MET"));
        var withOne = Run(wild, h =>
        {
            h.NeedsPokemon = true;
            h.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        });
        Assert.Contains(withOne.Log, l => l.StartsWith("wildbattle Starly 2"));
        Assert.True(withOne.Story.Has("FLAG_MET"));
    }

    [Fact]
    public void ABattleThatMayBeLostGoesOnEitherWay()
    {
        const string source = """
            script S
              battle self canlose
              if won say "You won."
              if lost say "You lost, and the story goes on."
              if result == 0 setflag FLAG_LOST
            """;

        var (runner, host) = Ready(source, subject: Trainer());
        host.Fight = _ => BattleOutcome.Lost;
        runner.RunToEnd();

        Assert.Equal(new[] { "You lost, and the story goes on." }, Said(host));
        Assert.True(host.Story.Has("FLAG_LOST"));
        Assert.False(runner.EndedInDefeat);
        Assert.Equal(new[] { "You won." }, Said(Run(source, subject: Trainer())));
    }

    [Theory]
    [InlineData(BattleOutcome.Won, 1)]
    [InlineData(BattleOutcome.Fled, 2)]
    [InlineData(BattleOutcome.Caught, 3)]
    public void AWildPokemonPutInThePlayersWayTellsHowItEnded(BattleOutcome outcome, int result)
    {
        var host = Run("script S\n wildbattle \"Starly\" 2\n setvar VAR_ENDED 0\n if result == 1 setvar VAR_ENDED 1\n if result == 2 setvar VAR_ENDED 2\n if result == 3 setvar VAR_ENDED 3",
            h => h.Fight = _ => outcome);

        Assert.Equal(result, host.Story.Var("VAR_ENDED"));
        Assert.Contains($"wildbattle Starly 2 {outcome}", host.Log);
        // One that was caught is the player's
        Assert.Equal(outcome == BattleOutcome.Caught ? 1 : 0, host.Party.Count);
    }

    [Fact]
    public void TwoTrainersBattledAtOnceAreBothBeatenAndAPartnerByIdIsPlatinums()
    {
        var first = Trainer("grunt_a", "Ann", prize: 100);
        var second = Trainer("grunt_b", "Bob", prize: 200);
        var (runner, host) = Ready("""
            script S
              battle grunt_a and grunt_b with "cheryl_eterna_forest"
            """);
        host.Map = Room(first, second);
        host.Money = 0;
        runner.RunToEnd();

        Assert.Contains("battle grunt_a and grunt_b with cheryl_eterna_forest Won", host.Log);
        Assert.True(host.Story.HasDefeated("grunt_a"));
        Assert.True(host.Story.HasDefeated("grunt_b"));
        Assert.Equal(300, host.Money);
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void TheCatchingLessonIsTheAssistantsAndTellsOfACatch()
    {
        var host = Run("script S\n catchinglesson \"Bidoof\" 2\n if result == 3 setflag FLAG_SHOWN");

        Assert.True(host.Story.Has("FLAG_SHOWN"));
        Assert.Contains("catchinglesson Bidoof 2 Caught", host.Log);
        // What the assistant caught isn't the player's
        Assert.Equal(0, host.Party.Count);
    }

    [Fact]
    public void AWildBattleThatCantBeFledSaysSo()
    {
        var host = Run("script S\n wildbattle \"Giratina\" 47 nofleeing", h => h.Fight = _ => BattleOutcome.Won);
        Assert.Contains("wildbattle Giratina 47 nofleeing Won", host.Log);
    }

    // ------------------------------------------------------------------ people and the field

    private static Map Room(params NPC[] people)
    {
        var map = new Map(12, 12) { Name = "Room" };
        foreach (var npc in people) map.NPCs.Add(npc);
        return map;
    }

    private static NPC Person(string key, int x, int y, string? name = null) =>
        new() { Key = key, Name = name ?? key, GridX = x, GridY = y };

    [Fact]
    public void PeopleWalkAndTurnWhereAScriptSendsThem()
    {
        var rival = Person("rival", 5, 5, "Barry");
        var (runner, host) = Ready("""
            script S
              walk rival up 2 right 3
              face rival player
              walk player down 2
              face player Barry
              place rival 1 1 left
            """);
        host.Map = Room(rival);
        host.PlayerTile = (8, 6);
        runner.RunToEnd();

        // Up two and right three from 5,5 is 8,3; the player went from 8,6 to 8,8 and turned back to look up at him
        Assert.Equal((8, 8), host.PlayerTile);
        Assert.Equal(Direction.Up, host.PlayerFacing);
        Assert.Equal((1, 1, Direction.Left), (rival.GridX, rival.GridY, rival.Facing));
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void SomeoneTurnsToLookAtSomeoneElse()
    {
        Assert.Equal(Direction.Left, ScriptRunner.Toward((5, 5), (2, 5)));
        Assert.Equal(Direction.Right, ScriptRunner.Toward((5, 5), (9, 6)));
        Assert.Equal(Direction.Up, ScriptRunner.Toward((5, 5), (5, 1)));
        Assert.Equal(Direction.Down, ScriptRunner.Toward((5, 5), (6, 9)));
        Assert.Null(ScriptRunner.Toward((5, 5), (5, 5)));
    }

    [Fact]
    public void AWalkIntoAWallIsAProblemTheHostNotes()
    {
        var (runner, host) = Ready("script S\n walk player right 2\n walk player up 9");
        host.Map = Room();
        host.Map.SetSolid(5, 4, true);
        host.PlayerTile = (4, 4);
        runner.RunToEnd();

        Assert.Equal(new[] { "the player walks into something solid at 5,4", "the player walks off the map at 6,-1" },
            host.Problems.Take(2));
    }

    [Fact]
    public void SomeoneAScriptHidesIsOffTheMapAndCanStillBeNamed()
    {
        var guard = Person("guard", 4, 4);
        var library = ScriptLibrary.FromSources(("test", "script Hide\n hide guard\nscript Show\n show guard"));
        var host = new HeadlessScriptHost { Map = Room(guard) };
        var runner = new ScriptRunner(library, host);

        runner.Start(library.Find("test.Hide")!);
        runner.RunToEnd();
        Assert.Empty(host.Map!.NPCs);
        Assert.Same(guard, host.Map.Absent.Single());

        // Off the map, a script still finds them by name: that is how they are brought back
        runner.Start(library.Find("test.Show")!);
        runner.RunToEnd();
        Assert.Same(guard, host.Map.NPCs.Single());
        Assert.Empty(host.Map.Absent);
    }

    [Fact]
    public void ANameNobodyHasIsToldWithTheLine()
    {
        var (runner, host) = Ready("script S\n say \"Hm.\"\n face nobody up");
        host.Map = Room();
        var wrong = Assert.Throws<ScriptException>(() => runner.RunToEnd());
        Assert.Equal("test.txt(3): nobody on this map is called 'nobody'.", wrong.Message);

        // And "self" where nobody started the script
        var (alone, _) = Ready("script S\n face self up");
        Assert.Contains("'self' is nobody here", Assert.Throws<ScriptException>(() => alone.RunToEnd()).Message);
    }

    [Fact]
    public void TheRestIsHandedToTheHostAsWritten()
    {
        var host = Run("""
            script S
              emote player exclaim
              camera pan 10 12
              camera shake
              camera release
              fade out
              warp "PlayerHouse" 4 5 up
              fade in
              music "sinnoh/jubilife"
              music area
              music stop
              fanfare heal
              sound "select"
              shop
              pc
            """);

        Assert.Equal(new[]
        {
            "emote player Exclaim", "camera Pan 10 12", "camera Shake", "camera Release", "fade out", "warp PlayerHouse 4 5",
            "fade in", "music sinnoh/jubilife", "music area", "music stop", "fanfare FanfareHeal", "sound select", "open Shop", "open Pc"
        }, host.Log);
        Assert.Equal(((4, 5), Direction.Up), (host.PlayerTile, host.PlayerFacing));
    }

    [Fact]
    public void AScriptThatMayShowNothingIsStoppedWhenItTries()
    {
        var library = ScriptLibrary.FromSources(("test", "script S\n setflag FLAG_FINE\n say \"Not fine.\""));
        var host = new HeadlessScriptHost { ShowsNothing = true };
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.Single());

        Assert.Contains("say something", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        Assert.True(host.Story.Has("FLAG_FINE"));
    }

    /// <summary>A host that only notes which answer backing out of each question would give.</summary>
    private sealed class Asking : IScriptHost
    {
        private readonly List<int> cancels;
        private readonly HeadlessScriptHost inner = new();

        public Asking(List<int> cancels) => this.cancels = cancels;

        public void Ask(string? speaker, string question, IReadOnlyList<string> answers, int cancel) => cancels.Add(cancel);

        public StoryState Story => inner.Story;
        public Party Party => inner.Party;
        public Inventory Bag => inner.Bag;
        public Poketch Poketch => inner.Poketch;
        public SafariGame Safari => inner.Safari;
        public int SeenInSinnoh => inner.SeenInSinnoh;
        public SpecialEncounters Encounters => inner.Encounters;
        public BerryPatches Berries => inner.Berries;
        public string? ChosenItem => inner.ChosenItem;
        public PoffinCase Poffins => inner.Poffins;
        public int? HoneyTreeFaced => inner.HoneyTreeFaced;
        public uint TrainerNumber => inner.TrainerNumber;
        public TimeOfDay TimeOfDay => inner.TimeOfDay;
        public DayOfWeek Weekday => inner.Weekday;
        public IEnumerable<Pokemon> Boxed => inner.Boxed;
        public Random Chance => inner.Chance;
        public int Money { get => inner.Money; set => inner.Money = value; }
        public string PlayerName => inner.PlayerName;
        public PlayerLook PlayerLook => inner.PlayerLook;
        public NPC? FindNpc(string name, string? place) => null;
        public (int X, int Y) TileOf(NPC? who) => (0, 0);
        public Direction FacingOf(NPC? who) => Direction.Down;
        public void Face(NPC? who, Direction direction) { }
        public void Place(NPC? who, int x, int y, Direction? facing) { }
        public void SetVisible(NPC who, bool visible) { }
        public void Walk(NPC? who, IReadOnlyList<Direction> steps, bool fast) { }
        public bool Walking => false;
        public void Emote(NPC? who, EmoteBubble bubble, float seconds) { }
        public void Camera(CameraMove move, int x, int y, float seconds) { }
        public bool Busy => false;
        public void Say(string? speaker, IReadOnlyList<string> lines) { }
        public int Answer => 0;
        public void Battle(NPC trainer, NPC? second, Trainer? partner, bool mayLose, bool first) { }
        public void WildBattle(Pokemon wild, BattleKind kind, bool cannotFlee) { }
        public BattleOutcome Outcome => BattleOutcome.Won;
        public void Open(ScriptScreen screen, NPC? subject, string? counter = null) { }
        public bool GivePokemon(Pokemon pokemon) => true;
        public void Warp(string map, int x, int y, Direction? facing) { }
        public void Fade(bool toBlack, float seconds) { }
        public void UseMove(FieldMove move, Pokemon user, NPC? subject) { }
        public bool Surf() => true;
        public bool Climb() => true;
        public bool Fly() => true;
        public bool Teleport() => true;
        public bool Escape() => true;
        public void Turnback() { }
        public void Defeat(string trainerId) { }
        public void TurnClock(int from, int to) { }
        public void PressButton(PastoriaWater.Button button) { }
        public void PressGearButton(SunyshoreGears.Button kind) { }
        public void TravelWith(NPC? who, string? trainerId) { }
        public string? Partner => null;
        public bool Trade(string trade, int slot) => false;
        public void EnterHallOfFame() { }
        public void Note(JournalEvent line) { }
        public bool SweetScent() => false;
        public void Music(string? song) { }
        public void Fanfare(MusicRole role) { }
        public void Sound(string name) { }
        public void Cry(string species) { }
    }
}
