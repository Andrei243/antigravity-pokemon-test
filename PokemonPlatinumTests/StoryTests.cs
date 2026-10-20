using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// What the story remembers and how the field starts its scripts (plan 02 · S1): the state and its save, people a
/// flag keeps off the map, triggers, the question box, scripted walks, and the game's own script files, each of
/// which is played to its end here on every way through it.
/// </summary>
public class StoryTests
{
    // ------------------------------------------------------------------ what the story remembers

    [Fact]
    public void TheStoryRemembersFlagsVariablesTrainersItemsAndBadges()
    {
        var story = new StoryState();
        Assert.False(story.Has("FLAG_MET_ROWAN"));
        Assert.Equal(0, story.Var("VAR_NEVER_SET"));

        story.Set("FLAG_MET_ROWAN");
        story.SetVar("VAR_JUBILIFE_CITY_STATE", 2);
        story.AddVar("VAR_JUBILIFE_CITY_STATE", 1);
        story.Defeat("route_202_tristan");
        story.Take("item_potion");
        story.GiveBadge("Sinnoh", "Coal");
        story.GiveBadge("Sinnoh", "Relic");

        Assert.True(story.Has("FLAG_MET_ROWAN"));
        Assert.Equal(3, story.Var("VAR_JUBILIFE_CITY_STATE"));
        Assert.True(story.HasDefeated("route_202_tristan"));
        Assert.True(story.HasTaken("item_potion"));
        Assert.True(story.HasBadge("Sinnoh", "Coal") && story.HasBadge("Sinnoh", "Relic") && !story.HasBadge("Sinnoh", "Forest"));
        Assert.Equal(2, story.BadgeCount("Sinnoh"));
        // The bits are the Trainer Card's: Coal is the first, Relic the fifth
        Assert.Equal(0b10001, story.BadgeMask("Sinnoh"));

        story.Unset("FLAG_MET_ROWAN");
        story.SetVar("VAR_JUBILIFE_CITY_STATE", 0);
        Assert.False(story.Has("FLAG_MET_ROWAN"));
        Assert.Empty(story.Variables);
    }

    [Fact]
    public void TheBadgesAreInTheOrderTheTrainerCardShowsThem()
    {
        // Sinnoh's file lists them (plan 18 · W2), and the card names each by its name (plan 17 · N1c), in English the name itself
        var sinnoh = RegionDatabase.Get("Sinnoh")!.BadgeNames;
        Assert.Equal(new[] { "Coal", "Forest", "Cobble", "Fen", "Relic", "Mine", "Icicle", "Beacon" }, sinnoh);
        Assert.Equal(sinnoh, sinnoh.Select(PokemonPlatinumEngine.Core.Words.Badge));
        // So are Kanto's, which its file lists too
        var kanto = RegionDatabase.Get("Kanto")!.BadgeNames;
        Assert.Equal(kanto, kanto.Select(PokemonPlatinumEngine.Core.Words.Badge));
    }

    [Fact]
    public void OnlyAChangeCountsAsOne()
    {
        var story = new StoryState();
        int before = story.Revision;
        story.Unset("FLAG_NEVER_SET");
        story.SetVar("VAR_NOUGHT", 0);
        Assert.Equal(before, story.Revision);

        story.Set("FLAG_A");
        int once = story.Revision;
        story.Set("FLAG_A");
        story.GiveBadge("Sinnoh", "Coal");
        int twice = story.Revision;
        story.GiveBadge("Sinnoh", "Coal");
        Assert.True(once > before && twice > once);
        Assert.Equal(twice, story.Revision);
    }

    [Theory]
    [InlineData("Turtwig", "Chimchar")]
    [InlineData("Chimchar", "Piplup")]
    [InlineData("Piplup", "Turtwig")]
    public void TheRivalTakesTheStarterThatBeatsThePlayers(string mine, string theirs)
    {
        var story = new StoryState();
        story.ChooseStarter(mine);
        Assert.Equal((mine, theirs), (story.PlayerStarter, story.RivalStarter));

        // Platinum's own reason: his is of the type mine is weak to
        var weakTo = PokemonDatabase.Get(theirs)!.PrimaryType;
        Assert.True(TypeChart.GetEffectiveness(weakTo, PokemonDatabase.Get(mine)!.PrimaryType) > 1f);
    }

    // ------------------------------------------------------------------ the save

    [Fact]
    public void TheStoryGoesThroughTheSaveFileAndComesBackTheSame()
    {
        var story = new StoryState();
        story.Set("FLAG_RECEIVED_COUPON_1");
        story.CompleteRegion(RegionDatabase.Kanto);
        story.SetVar("VAR_POKETCH_CAMPAIGN_STATE", 2);
        story.Defeat("route_202_tristan");
        story.Take("jubilife_city.item_potion");
        story.GiveBadge("Sinnoh", "Coal");
        story.ChooseStarter("Piplup");

        var told = story.Snapshot();
        var save = new SaveData
        {
            StoryFlags = told.Flags, StoryVariables = told.Variables, DefeatedTrainers = told.DefeatedTrainers, TakenItems = told.TakenItems,
            RegionBadges = told.Badges, PlayerStarter = told.PlayerStarter, RivalStarter = told.RivalStarter, StoryVersion = StoryState.CurrentVersion
        };
        var loaded = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true }))!;

        var back = new StoryState();
        back.Restore(loaded.ToStory());
        Assert.Equal(StoryState.CurrentVersion, loaded.StoryVersion);
        Assert.True(back.Has("FLAG_RECEIVED_COUPON_1") && back.IsRegionComplete(RegionDatabase.Kanto));
        Assert.Equal(2, back.Var("VAR_POKETCH_CAMPAIGN_STATE"));
        Assert.True(back.HasDefeated("route_202_tristan") && back.HasTaken("jubilife_city.item_potion") && back.HasBadge("Sinnoh", "Coal"));
        Assert.Equal(("Piplup", "Turtwig"), (back.PlayerStarter, back.RivalStarter));
        Assert.Equal(JsonSerializer.Serialize(told), JsonSerializer.Serialize(back.Snapshot()));
    }

    /// <summary>A save as the game wrote them before the story was kept: no version, no variables, no starter.</summary>
    private const string OldSave = """
        {
          "PlayerName": "Lucas",
          "Money": 4200,
          "Badges": 1,
          "CurrentMapName": "Sinnoh",
          "PlayerGridX": 180,
          "PlayerGridY": 777,
          "WorldVersion": 2,
          "Party": [ { "SpeciesName": "Starly", "Level": 9 }, { "SpeciesName": "Monferno", "Level": 16 } ],
          "DefeatedTrainers": [ "route_202_tristan" ],
          "StoryFlags": [ "KantoHallOfFame" ]
        }
        """;

    [Fact]
    public void ASaveFromBeforeTheStoryWasKeptIsBroughtUpToDate()
    {
        var save = JsonSerializer.Deserialize<SaveData>(OldSave)!;
        Assert.Equal(0, save.StoryVersion);
        Assert.Empty(save.StoryVariables);
        Assert.Null(save.PlayerStarter);

        // What a new game starts with today, which that save never had
        var scripts = ScriptLibrary.FromSources(("common", "script NewGame\n setflag FLAG_HIDE_SOMEONE_UNTIL_LATER\n setvar VAR_SOME_STATE 1"));
        var story = new StoryState();
        story.Restore(save.ToStory());
        StoryMigration.Upgrade(story, save.StoryVersion, save.Party.Select(p => p.ToPokemon()), scripts);

        // It keeps what it had
        Assert.True(story.IsRegionComplete(RegionDatabase.Kanto));
        Assert.True(story.HasBadge("Sinnoh", "Coal"));
        Assert.True(story.HasDefeated("route_202_tristan"));
        // It is given what every game begins with
        Assert.True(story.Has("FLAG_HIDE_SOMEONE_UNTIL_LATER"));
        Assert.Equal(1, story.Var("VAR_SOME_STATE"));
        // And its starter is read off its team: a Monferno was a Chimchar, so the rival took Piplup
        Assert.Equal(("Chimchar", "Piplup"), (story.PlayerStarter, story.RivalStarter));
    }

    [Fact]
    public void ASaveOfTodayIsLeftAsItIs()
    {
        var scripts = ScriptLibrary.FromSources(("common", "script NewGame\n setflag FLAG_HIDE_SOMEONE_UNTIL_LATER"));
        var story = new StoryState();
        story.ChooseStarter("Piplup");
        StoryMigration.Upgrade(story, StoryState.CurrentVersion, new[] { new Pokemon(PokemonDatabase.Get("Torterra")!, 40) }, scripts);

        Assert.False(story.Has("FLAG_HIDE_SOMEONE_UNTIL_LATER"));
        Assert.Equal("Piplup", story.PlayerStarter);
    }

    [Fact]
    public void TheStarterIsReadOffWhatItGrewInto()
    {
        Pokemon Of(string species) => new(PokemonDatabase.Get(species)!, 30);
        Assert.Equal("Turtwig", StoryMigration.StarterAmong(new[] { Of("Bidoof"), Of("Torterra") }));
        Assert.Equal("Piplup", StoryMigration.StarterAmong(new[] { Of("Prinplup") }));
        Assert.Null(StoryMigration.StarterAmong(new[] { Of("Bidoof"), Of("Starly") }));
    }

    [Fact]
    public void ANewGamesScriptMaySetThingsUpAndNothingElse()
    {
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, ScriptLibrary.FromSources(("common", "script NewGame\n setflag FLAG_A\n setvar VAR_A 4")));
        Assert.True(story.Has("FLAG_A"));
        Assert.Equal(4, story.Var("VAR_A"));

        var talks = ScriptLibrary.FromSources(("common", "script NewGame\n say \"Welcome!\""));
        Assert.Throws<ScriptException>(() => StoryMigration.BeginNewGame(new StoryState(), talks));
    }

    // ------------------------------------------------------------------ people a flag keeps off the map

    private static NPC Person(string key, int x, int y, string? hiddenBy = null, string? shownBy = null, string? place = null) =>
        new() { Key = key, Name = key, GridX = x, GridY = y, HiddenBy = hiddenBy, ShownBy = shownBy, ScriptFile = place };

    [Fact]
    public void AFlagTakesSomeoneOffTheMapAndAnotherBringsSomeoneOn()
    {
        var map = new Map(10, 10);
        var guard = Person("guard", 2, 2, hiddenBy: "FLAG_HIDE_GUARD");
        var rival = Person("rival", 4, 4, shownBy: "FLAG_RIVAL_ARRIVED");
        var local = Person("local", 6, 6);
        map.NPCs.AddRange(new[] { guard, rival, local });
        var story = new StoryState();

        map.ApplyPresence(story.Has);
        Assert.Equal(new[] { guard, local }, map.NPCs);
        Assert.Equal(new[] { rival }, map.Absent);
        // Whoever is off the map is neither met nor in the way
        Assert.Null(map.GetNpcAt(4, 4));
        Assert.True(map.IsWalkable(4, 4));
        Assert.False(map.IsWalkable(2, 2));

        story.Set("FLAG_HIDE_GUARD");
        story.Set("FLAG_RIVAL_ARRIVED");
        map.ApplyPresence(story.Has);
        Assert.Equal(new[] { rival, local }, map.NPCs);
        Assert.True(map.IsWalkable(2, 2));

        // Back again, everyone is where they were in the list: who looks first doesn't change with coming and going
        story.Unset("FLAG_HIDE_GUARD");
        map.ApplyPresence(story.Has);
        Assert.Equal(new[] { guard, rival, local }, map.NPCs);
        Assert.Empty(map.Absent);
        Assert.Equal(3, map.Everyone.Count());
    }

    [Fact]
    public void WhatAScriptDidToSomeoneHoldsUntilTheMapIsComeToAgain()
    {
        var map = new Map(10, 10);
        var guard = Person("guard", 2, 2, hiddenBy: "FLAG_HIDE_GUARD");
        map.NPCs.Add(guard);
        var story = new StoryState();

        guard.Forced = false;
        map.ApplyPresence(story.Has);
        Assert.Empty(map.NPCs);

        // A flag changing elsewhere doesn't bring them back
        story.Set("FLAG_SOMETHING_ELSE");
        map.ApplyPresence(story.Has);
        Assert.Empty(map.NPCs);

        map.ForgetForced();
        map.ApplyPresence(story.Has);
        Assert.Same(guard, map.NPCs.Single());
    }

    [Fact]
    public void SomeoneIsFoundByWhatScriptsCallThemOrByNameAndByPlace()
    {
        var map = new Map(10, 10);
        var here = Person("clown_1", 1, 1, place: "jubilife_city");
        var there = Person("clown_1", 5, 5, place: "other_town");
        var named = new NPC { Name = "Looker", GridX = 7, GridY = 7 };
        map.NPCs.AddRange(new[] { here, there, named });
        map.Absent.Add(Person("hidden_one", 8, 8, place: "jubilife_city"));

        Assert.Same(here, map.FindPerson("clown_1", "jubilife_city"));
        Assert.Same(there, map.FindPerson("clown_1", "other_town"));
        Assert.Same(named, map.FindPerson("looker", "jubilife_city"));
        Assert.Equal("hidden_one", map.FindPerson("hidden_one")!.Key);
        Assert.Null(map.FindPerson("nobody"));
        // A script's names mean the people of its own place: another town's clown is not found for it
        Assert.Null(map.FindPerson("clown_1", "third_town"));
        Assert.Same(here, map.FindPerson("clown_1"));
    }

    [Fact]
    public void AMapFileKeepsScriptsFlagsAndTriggers()
    {
        var file = new MapFile
        {
            Name = "Test", DisplayName = "Test", Width = 4, Height = 3,
            Ground = new List<string> { "....", "....", "...." },
            Solid = new List<string> { "....", "....", "...." },
            Signboards = new List<MapFile.SignRecord> { new() { X = 3, Y = 0, Text = "A notice.", Script = "Notice" } },
            Npcs = new List<MapFile.NpcRecord>
            {
                new() { Id = "guard", Name = "Guard", NpcType = "Gentleman", X = 1, Y = 1, Dialog = new List<string> { "Halt." }, Script = "Guard", HiddenBy = "FLAG_HIDE_GUARD" },
                new() { Name = "Rival", NpcType = "Rival", X = 2, Y = 1, ShownBy = "FLAG_RIVAL_ARRIVED" }
            },
            Triggers = new List<MapFile.TriggerRecord>
            {
                new() { X = 0, Y = 2, Width = 4, Script = "Stopped", Variable = "VAR_TEST_STATE", Value = 1 },
                new() { X = 0, Y = 0, Script = "Always" }
            }
        };

        var map = file.ToMap();
        var guard = map.NPCs[0];
        Assert.Equal(("guard", "Guard", "FLAG_HIDE_GUARD"), (guard.Key, guard.Script, guard.HiddenBy));
        Assert.Equal("FLAG_RIVAL_ARRIVED", map.NPCs[1].ShownBy);
        Assert.Equal("Notice", map.SignScripts[(3, 0)]);
        Assert.Equal(("Stopped", "VAR_TEST_STATE", 1, 4), (map.Triggers[0].Script, map.Triggers[0].Variable, map.Triggers[0].Value, map.Triggers[0].Width));

        // Written back, it is the same file, with someone a flag has off the map still in it
        map.ApplyPresence(_ => false);
        Assert.Single(map.Absent);
        Assert.Equal(GameDataFiles.Serialize(file), GameDataFiles.Serialize(MapFile.FromMap(map)));
    }

    // ------------------------------------------------------------------ what starts a script

    [Fact]
    public void WhatSomeoneIsDecidesTheirScriptUnlessTheyHaveTheirOwn()
    {
        Assert.Equal(FieldScripts.Nurse, FieldScripts.For(new NPC { IsHealingNurse = true }));
        Assert.Equal(FieldScripts.Clerk, FieldScripts.For(new NPC { IsPokeMartClerk = true, DialogLines = { "Welcome!" } }));
        Assert.Equal(FieldScripts.Pc, FieldScripts.For(new NPC { IsPCTerminal = true }));
        Assert.Equal(FieldScripts.Briefcase, FieldScripts.For(new NPC { IsStarterBriefcase = true }));
        Assert.Equal(FieldScripts.Attendant, FieldScripts.For(new NPC { IsTransportAttendant = true }));
        Assert.Equal(FieldScripts.Trainer, FieldScripts.For(new NPC { IsTrainer = true, TrainerData = new Trainer(), DialogLines = { "Hi." } }));
        Assert.Equal(FieldScripts.Talk, FieldScripts.For(new NPC { DialogLines = { "Hi." } }));
        Assert.Null(FieldScripts.For(new NPC()));

        // A script of their own comes before what they are
        Assert.Equal("Healer", FieldScripts.For(new NPC { IsHealingNurse = true, Script = "Healer" }));
    }

    [Fact]
    public void ATriggerFiresWhileItsVariableHasItsValue()
    {
        var map = new Map(10, 10);
        map.Triggers.Add(new StepTrigger { X = 2, Y = 5, Width = 4, Script = "First", Variable = "VAR_TOWN_STATE", Value = 0 });
        map.Triggers.Add(new StepTrigger { X = 2, Y = 5, Width = 4, Script = "Second", Variable = "VAR_TOWN_STATE", Value = 1 });
        map.Triggers.Add(new StepTrigger { X = 8, Y = 8, Script = "Always" });
        var story = new StoryState();

        Assert.Equal("First", FieldScripts.TriggerAt(map, 3, 5, story)!.Script);
        Assert.Equal("First", FieldScripts.TriggerAt(map, 5, 5, story)!.Script);
        Assert.Null(FieldScripts.TriggerAt(map, 6, 5, story));
        Assert.Null(FieldScripts.TriggerAt(map, 3, 6, story));

        // The script it starts moves the variable on, and the next one's turn comes
        story.SetVar("VAR_TOWN_STATE", 1);
        Assert.Equal("Second", FieldScripts.TriggerAt(map, 3, 5, story)!.Script);
        story.SetVar("VAR_TOWN_STATE", 2);
        Assert.Null(FieldScripts.TriggerAt(map, 3, 5, story));
        Assert.Equal("Always", FieldScripts.TriggerAt(map, 8, 8, story)!.Script);
    }

    // ------------------------------------------------------------------ the question box

    [Fact]
    public void AQuestionsAnswersAreChosenWithTheCursorOrBackedOutOf()
    {
        var box = new ChoiceBox();
        box.Open(ScriptRunner.YesNo, cancel: 1);
        Assert.True(box.IsOpen);
        Assert.Equal(0, box.Cursor);

        box.Move(1);
        Assert.Equal(1, box.Cursor);
        box.Move(1);
        Assert.Equal(0, box.Cursor);
        box.Move(-1);
        Assert.Equal(1, box.Cursor);
        box.Move(-1);
        box.Confirm();
        Assert.False(box.IsOpen);
        Assert.Equal(0, box.Take());
        Assert.Null(box.Take());

        // The B button is "No"
        box.Open(ScriptRunner.YesNo, cancel: 1);
        box.Back();
        Assert.Equal(1, box.Take());

        // A question that has to be answered can't be backed out of
        box.Open(new[] { "Turtwig", "Chimchar", "Piplup" });
        box.Back();
        Assert.True(box.IsOpen);
        Assert.Null(box.Picked);
        box.Move(2);
        box.Confirm();
        Assert.Equal(2, box.Take());
    }

    [Fact]
    public void AQuestionStaysOnTheBoxUntilItIsAnswered()
    {
        var dialogue = new DialogueManager();
        dialogue.ShowQuestion("Nurse", "Shall I see to them?");
        Assert.True(dialogue.IsQuestion);
        Assert.False(dialogue.AwaitingAnswer);

        dialogue.Type(60f);
        Assert.True(dialogue.AwaitingAnswer);
        // The button that goes on to the next line of a talk does nothing to a question
        dialogue.Advance();
        Assert.True(dialogue.IsActive);
        Assert.Equal("Shall I see to them?", dialogue.VisibleText);

        dialogue.Close();
        Assert.False(dialogue.IsActive);

        // An ordinary talk after it is ordinary again
        dialogue.ShowDialogue("Nurse", new[] { "One.", "Two." });
        dialogue.FinishLine();
        Assert.False(dialogue.AwaitingAnswer);
        dialogue.Advance();
        Assert.Equal("Two.", dialogue.CurrentLine);
    }

    // ------------------------------------------------------------------ walks a script leads

    [Fact]
    public void SomeoneSentWalkingGoesATileAtATimeAndEndsStandingStill()
    {
        var npc = new NPC { GridX = 5, GridY = 5, Facing = Direction.Down };
        var walk = new NpcWalk(npc, new[] { Direction.Up, Direction.Up, Direction.Right }, fast: false);

        // Partway through the first step they are already on its tile by the grid, and drawn between the two
        walk.Update(0.1f);
        Assert.Equal((5, 4, Direction.Up), (npc.GridX, npc.GridY, npc.Facing));
        Assert.InRange(npc.StepOffsetY, 0.01f, 0.99f);
        Assert.InRange(npc.DrawY, 4.01f, 4.99f);

        for (int frame = 0; frame < 600 && !walk.IsDone; frame++) walk.Update(1f / 60f);
        Assert.True(walk.IsDone);
        Assert.Equal((6, 3, Direction.Right), (npc.GridX, npc.GridY, npc.Facing));
        Assert.Equal((0f, 0f, 0f), (npc.StepOffsetX, npc.StepOffsetY, npc.WalkBlend));

        // Three tiles at a walk take two thirds of a second, and less in a hurry
        Assert.Equal(4.5f, NpcWalk.WalkPace);
        Assert.True(NpcWalk.RunPace > NpcWalk.WalkPace);
    }

    [Fact]
    public void AWalkCutShortPutsThemWhereItWasGoing()
    {
        var npc = new NPC { GridX = 5, GridY = 5 };
        var walk = new NpcWalk(npc, new[] { Direction.Left, Direction.Left, Direction.Down }, fast: true);
        walk.Update(0.05f);
        walk.Finish();

        Assert.True(walk.IsDone);
        Assert.Equal((3, 6, Direction.Down), (npc.GridX, npc.GridY, npc.Facing));
        Assert.Equal((0f, 0f), (npc.StepOffsetX, npc.StepOffsetY));
    }

    [Fact]
    public void ThePlayerSentWalkingTakesTheirOwnStepsAndGivesUpAtAWall()
    {
        var map = new Map(10, 10);
        var player = new Player(5, 5);
        var walk = new PlayerWalk(new[] { Direction.Right, Direction.Right, Direction.Down }, fast: false);
        int steps = 0;
        for (int frame = 0; frame < 900 && !walk.IsDone; frame++) walk.Update(1f / 60f, player, map, () => steps++);

        Assert.True(walk.IsDone);
        Assert.False(walk.Blocked);
        Assert.Equal((7, 6, Direction.Down), (player.GridX, player.GridY, player.Facing));
        Assert.Equal(3, steps);

        // Something in the way: the walk ends there rather than keep the game waiting
        map.SetSolid(7, 7, true);
        var blocked = new PlayerWalk(new[] { Direction.Down, Direction.Down }, fast: false);
        int frames = 0;
        for (; frames < 900 && !blocked.IsDone; frames++) blocked.Update(1f / 60f, player, map);
        Assert.True(blocked.IsDone && blocked.Blocked);
        Assert.Equal((7, 6), (player.GridX, player.GridY));
        Assert.InRange(frames / 60f, PlayerWalk.GiveUpAfter, PlayerWalk.GiveUpAfter + 0.5f);
    }

    [Fact]
    public void SomeoneWalkedOnTheSpotStaysOnTheirTileAndOneSteppedBackKeepsTheirFacing()
    {
        // Two steps on the spot to the left (plan 11 · C11, the original's WalkOnSpot): turned, the legs walking, the
        // tile the same
        var npc = new NPC { GridX = 5, GridY = 5, Facing = Direction.Down };
        var onSpot = new NpcWalk(npc, new[] { new ScriptStep(Direction.Left, ScriptStepKind.OnSpot), new ScriptStep(Direction.Left, ScriptStepKind.OnSpot) }, fast: false);
        onSpot.Update(0.1f);
        Assert.Equal((5, 5, Direction.Left), (npc.GridX, npc.GridY, npc.Facing));
        Assert.Equal((0f, 0f), (npc.StepOffsetX, npc.StepOffsetY));
        Assert.True(npc.WalkBlend > 0f);
        float taken = 0.1f;
        for (; taken < 5f && !onSpot.IsDone; taken += 1f / 60f) onSpot.Update(1f / 60f);
        Assert.Equal((5, 5, Direction.Left), (npc.GridX, npc.GridY, npc.Facing));
        // Two steps' time at a walk, then the rest
        Assert.InRange(taken, 2f / NpcWalk.WalkPace, 2f / NpcWalk.WalkPace + 0.2f);

        // A step to the right with the facing kept (LockDir): a step back from where they look
        var back = new NpcWalk(npc, new[] { new ScriptStep(Direction.Right, ScriptStepKind.Back) }, fast: false);
        back.Update(0.1f);
        Assert.Equal((6, 5, Direction.Left), (npc.GridX, npc.GridY, npc.Facing));
        Assert.InRange(npc.DrawX, 5.01f, 5.99f);
        back.Finish();
        Assert.Equal((6, 5, Direction.Left), (npc.GridX, npc.GridY, npc.Facing));

        // The player takes the same with their own steps: on the spot to the left, then two back up the map
        var map = new Map(10, 10);
        var player = new Player(5, 5);
        var walk = new PlayerWalk(new[]
        {
            new ScriptStep(Direction.Left, ScriptStepKind.OnSpot), new ScriptStep(Direction.Up, ScriptStepKind.Back),
            new ScriptStep(Direction.Up, ScriptStepKind.Back)
        }, fast: false);
        int steps = 0;
        for (int frame = 0; frame < 900 && !walk.IsDone; frame++)
        {
            walk.Update(1f / 60f, player, map, () => steps++);
            Assert.Equal(Direction.Left, player.Facing);
        }
        Assert.True(walk.IsDone);
        Assert.False(walk.Blocked);
        Assert.Equal((5, 3, Direction.Left), (player.GridX, player.GridY, player.Facing));
        Assert.Equal(2, steps);

        // A step back into something is given up after a moment, as a walk into a wall is
        map.SetSolid(6, 3, true);
        var blocked = new PlayerWalk(new[] { new ScriptStep(Direction.Right, ScriptStepKind.Back) }, fast: false);
        int frames = 0;
        for (; frames < 900 && !blocked.IsDone; frames++) blocked.Update(1f / 60f, player, map);
        Assert.True(blocked.IsDone && blocked.Blocked);
        Assert.Equal((5, 3, Direction.Left), (player.GridX, player.GridY, player.Facing));
        Assert.InRange(frames / 60f, PlayerWalk.GiveUpAfter, PlayerWalk.GiveUpAfter + 0.5f);
    }

    [Fact]
    public void AGesturesClockIsTheFieldsAndEndsAfterItsRounds()
    {
        // Two people given the same gesture and the same frames are at the same moment of it: the clock is the dt the
        // field hands them, as a bubble's is (plan 11 · C11), so a fixed frame clock repeats it
        var first = new NPC();
        var second = new NPC();
        first.Act(Gesture.Nod, 2);
        second.Act(Gesture.Nod, 2);
        float[] frames = { 1f / 60f, 1f / 30f, 0.05f, 1f / 144f, 0.1f, 1f / 60f };
        foreach (float dt in frames)
        {
            first.TickGesture(dt);
            second.TickGesture(dt);
        }
        Assert.Equal(first.GestureAge, second.GestureAge);
        Assert.Equal(frames.Sum(), first.GestureAge, 4);
        Assert.Equal(Gesture.Nod, first.Gesture);
        Assert.Equal(2, first.GestureTimes);

        // Each round starts the emote again, and the gesture is over after as many rounds as it was asked for
        float round = Acting.Length(Gesture.Nod);
        Assert.Equal(0.25f, Acting.RoundAge(Gesture.Nod, round + 0.25f), 4);
        var nod = new NPC();
        nod.Act(Gesture.Nod, 2);
        float played = 0f;
        for (; played < 10f && nod.Gesture != null; played += 1f / 60f) nod.TickGesture(1f / 60f);
        Assert.InRange(played, 2f * round - 0.02f, 2f * round + 0.02f);
        Assert.Equal(0f, nod.GestureAge);

        // A jump on the spot is the hop's own length; the player plays gestures by the same clock
        Assert.Equal(Acting.JumpSeconds, Acting.Length(Gesture.Jump));
        var player = new Player(3, 3);
        player.Act(Gesture.Jump);
        player.TickGesture(Acting.JumpSeconds / 2f);
        Assert.Equal(Gesture.Jump, player.Gesture);
        player.TickGesture(Acting.JumpSeconds);
        Assert.Null(player.Gesture);

        // A face is held until it is changed or the place is come to again, with any gesture under way
        var map = new Map(6, 6);
        var held = new NPC { GridX = 2, GridY = 2, Mood = Mood.Sad };
        map.NPCs.Add(held);
        held.Act(Gesture.Laugh);
        map.ForgetForced();
        Assert.Equal((Mood.Neutral, (Gesture?)null), (held.Mood, held.Gesture));
    }

    [Fact]
    public void AScriptsFadeGoesToBlackAndComesBack()
    {
        var fade = new ScreenFade();
        fade.To(black: true, 0.4f);
        Assert.True(fade.IsMoving);
        fade.Update(0.2f);
        Assert.InRange(fade.Level, 0.45f, 0.55f);
        fade.Update(0.3f);
        Assert.Equal(1f, fade.Level);
        Assert.False(fade.IsMoving);

        fade.To(black: false, 0f);
        Assert.Equal(0f, fade.Level);
    }

    [Fact]
    public void AScenesPaceIsTheStyleGuides()
    {
        // Style guide, "Field menus and notices": a fade of 0.4 s, a shake of half a second, six answers at most,
        // people walking at the trainers' pace and resting an eighth of a second after their last step
        Assert.Equal(0.4f, ScriptParser.FadeSeconds);
        Assert.Equal(0.5f, ScriptParser.ShakeSeconds);
        Assert.Equal(6, ScriptParser.MostChoices);
        Assert.Equal((4.5f, 8f), (NpcWalk.WalkPace, NpcWalk.RunPace));
        Assert.Equal(0.75f, PlayerWalk.GiveUpAfter);

        var npc = new NPC { GridX = 2, GridY = 2 };
        var walk = new NpcWalk(npc, new[] { Direction.Right }, fast: false);
        float taken = 0f;
        for (; taken < 5f && !walk.IsDone; taken += 1f / 120f) walk.Update(1f / 120f);
        // One tile at 4.5 a second, then the rest
        Assert.InRange(taken, 1f / 4.5f + 0.11f, 1f / 4.5f + 0.15f);
    }

    // ------------------------------------------------------------------ the game's own scripts

    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    // The walk that plays the game's scripts, with maps of this class's own, so that playing scripts on them (people
    // walk, are beaten, come and go) shows nowhere else (plan 24 · X3: ScriptWalk.cs)
    private static readonly ScriptWalk Walk = new();

    [Fact]
    public void TheScriptFilesAreReadAndNameNothingThatIsNotThere()
    {
        Assert.Contains(ScriptLibrary.Common, Scripts.Files);
        Assert.Empty(Scripts.Problems(
            mapExists: Walk.OwnMaps.ContainsKey,
            songExists: MusicLibrary.Exists,
            soundExists: AudioManager.SoundNames.Contains));
    }

    [Fact]
    public void EveryScriptFileBelongsToAPlace()
    {
        // (but the Pokégear's calls, which the game starts by its name, plan 20 · J3)
        foreach (string file in Scripts.Files.Where(f => f != ScriptLibrary.Common && f != PokemonPlatinumEngine.Core.GameEngine.PhoneScripts))
            Assert.True(Walk.MapOfFile(file) != null, $"scripts/{file}.txt is named for no open area and no map");
    }

    [Fact]
    public void WhateverStartsAScriptStartsOneThatExists()
    {
        Assert.NotEmpty(Walk.Starters());
        foreach (var (what, script, file, _, _) in Walk.Starters())
            Assert.True(Scripts.Find(script, file) != null, $"{what} starts '{script}', which is in neither {file}.txt nor common.txt");

        // And the common ones the field reaches for by itself
        foreach (string name in new[] { FieldScripts.Nurse, FieldScripts.Clerk, FieldScripts.Pc, FieldScripts.Briefcase, FieldScripts.Attendant, FieldScripts.Trainer, FieldScripts.Talk, FieldScripts.Sign, ScriptLibrary.NewGame })
            Assert.True(Scripts.Find(name) != null, $"there is no {name}");
    }

    [Fact]
    public void NoScriptIsLeftThatNothingStarts()
    {
        var started = Walk.Starters().Select(s => Scripts.Find(s.Script, s.File)!.FullName).ToHashSet();
        started.UnionWith(new[] { FieldScripts.Nurse, FieldScripts.Clerk, FieldScripts.Pc, FieldScripts.Briefcase, FieldScripts.Attendant, FieldScripts.Trainer, FieldScripts.Talk, FieldScripts.Sign, ScriptLibrary.NewGame, ScriptLibrary.OpeningDone });
        // Each region's own new game and Kanto's opening for an older save (plan 19 · K3), which StoryMigration runs
        started.UnionWith(StoryMigration.RegionalNewGames);
        started.Add("common.KantoOpeningDone");
        started.Add("common.JohtoOpeningDone");
        started.Add("common.HoennOpeningDone");
        // The Pokégear's numbers for an older save of Johto, and the calls, which the game starts by the contact (plan 20 · J3)
        started.Add("common.JohtoPokegear");
        started.UnionWith(Scripts.All.Where(s => s.File == PokemonPlatinumEngine.Core.GameEngine.PhoneScripts).Select(s => s.FullName));
        // The field moves': what an obstacle, the water, a waterfall and a rock face run, and what the party menu runs
        started.UnionWith(new[] { FieldScripts.CutTree, FieldScripts.Rock, FieldScripts.Boulder, FieldScripts.Water, FieldScripts.Waterfall, FieldScripts.RockFace, FieldScripts.Whirlpool });
        // Dive down from dark water and up from the sea floor, which A on the tile underfoot runs (plan 21 · H5)
        started.UnionWith(new[] { FieldScripts.Dive, FieldScripts.Surface });
        // An Escape Rope from the bag, and a gate onto the Cycling Road refusing someone on foot
        started.UnionWith(new[] { FieldScripts.EscapeRope, FieldScripts.CyclistsOnly });
        // Waking up after a lost battle, in a Pokémon Center or at home
        started.UnionWith(new[] { FieldScripts.BlackOutCenter, FieldScripts.BlackOutHome });
        // The end of a Safari Game: its last step, or its last ball (plan 01 · M7)
        started.UnionWith(new[] { FieldScripts.SafariTimeUp, FieldScripts.SafariOutOfBalls });
        // A Repel's last step (plan 06 · R11)
        started.Add(FieldScripts.RepelWoreOff);
        // Wild Pokémon (plan 06 · R13): a honey tree, Honey from the bag and poison's survivors (the assistant's sister's
        // news of swarms is called by her own script since her house was built, plan 01 · M11)
        started.UnionWith(new[] { FieldScripts.HoneyTree, FieldScripts.UseHoney, FieldScripts.PoisonSurvived });
        // Two trainers who saw the player at once (plan 02 · S6)
        started.Add(FieldScripts.TrainerPair);
        // An Egg hatching at the end of a step (plan 06 · R15)
        started.Add(FieldScripts.HatchEgg);
        // The Underground (plan 06 · R16a): a wall that holds something, a base's PC and the bag's way up, which the game starts
        started.UnionWith(PokemonPlatinumEngine.Core.GameEngine.UndergroundScripts.Select(name => Scripts.Find(name, "underground")!.FullName));
        // What looking at the Villa's furniture says, and the Catching Show's end (plan 06 · R16b)
        started.UnionWith(PokemonPlatinumEngine.Core.GameEngine.PastimeScripts.Select(s => Scripts.Find(s.Script, s.File)!.FullName));
        started.UnionWith(Enum.GetValues<FieldMove>().Select(FieldScripts.FromMenu).OfType<string>());
        foreach (var script in Scripts.All)
        {
            if (script.Name == ScriptLibrary.OnEnter) started.Add(script.FullName);
            foreach (var call in script.Everything().Where(i => i.Op == Op.Call))
                started.Add(Scripts.Find(call.Name, script.File)!.FullName);
        }

        Assert.DoesNotContain(Scripts.All.Select(s => s.FullName), name => !started.Contains(name));
    }

    [Fact]
    public void ThePeopleAPlacesScriptsNameAreThere()
    {
        foreach (var script in Scripts.All.Where(s => s.File != ScriptLibrary.Common))
        {
            var map = Walk.MapOfFile(script.File)!;
            foreach (var (who, line) in ScriptLibrary.PeopleNamed(script))
                Assert.True(map.FindPerson(who, script.File) != null, $"{script.File}.txt({line}): nobody of {map.Name} is called '{who}'");
        }
        // What every place shares can't name anyone but the player and whoever it belongs to
        Assert.Empty(Scripts.All.Where(s => s.File == ScriptLibrary.Common).SelectMany(ScriptLibrary.PeopleNamed));
    }

    /// <summary>
    /// Flags a script asks about, as the original's does, before anything sets them: a scene that isn't written yet
    /// sets each. The Hall of Fame's script (plan 02 · S14; scripts/PokemonLeagueHallOfFame.txt, Victory) asked six so,
    /// and the session that wrote a scene took its flag off this list: plan 02 · S15, part b Heatran's, Regigigas's and
    /// Charon's arrest, plan 01 · M11, part d the professor's in the Veilstone Department Store's basement, and plan 02 ·
    /// S15, part c the last three (Dialga and Palkia at Spear Pillar, Buck spoken to at the Battleground), and plan 21 ·
    /// H4, part b Team Magma beaten on Mt. Chimney. It is empty: whoever asks ahead of a scene again names the flag here,
    /// one to a line.
    /// </summary>
    internal static readonly string[] AskedAheadOfTheirScenes = { };

    [Fact]
    public void AFlagOrAVariableAskedAboutIsSetSomewhere()
    {
        // A flag nobody sets is a misspelling, unless it is a region's Hall of Fame (the League's script sets those) or
        // a town's first arrival, which the game sets as the player comes (SpawnLocations: the elder's house in Celestic
        // Town asks Canalave City's, plan 02 · S9)
        var known = Scripts.FlagsWritten.Concat(RegionDatabase.All.Select(r => r.StoryCompleteFlag))
            .Concat(SpawnLocations.All.Select(s => s.ArrivalFlag))
            // What the Underground Man's missions wait for is set by the game as each thing is done below (plan 06 · R16a)
            .Concat(PokemonPlatinumEngine.Core.GameEngine.UndergroundMissionFlags)
            // The Villa's furniture is the player's as it is paid for (plan 06 · R16b)
            .Concat(PokemonPlatinumEngine.Core.GameEngine.PastimeFlags)
            // What the Hall of Fame's script asks ahead of the scenes that set it (plan 02 · S14)
            .Concat(AskedAheadOfTheirScenes).ToHashSet();
        Assert.DoesNotContain(Scripts.FlagsRead, f => !known.Contains(f));
        // ...each of which leaves that list with the session that writes its scene: one that is set somewhere has no
        // business on it any longer
        Assert.DoesNotContain(AskedAheadOfTheirScenes, f => Scripts.FlagsWritten.Contains(f));
        // The slot machines' and the Catching Show's records and the Villa's visitor of the day are the game's to keep (plan 06 · R16b)
        // and so are those of Amity Square's walk, which `amity` and the player's steps keep (plan 06 · R16c)
        Assert.DoesNotContain(Scripts.VariablesRead, v => !Scripts.VariablesWritten.Contains(v) && !PokemonPlatinumEngine.Core.GameEngine.PastimeVariables.Contains(v)
            && !PokemonPlatinumEngine.Overworld.AmitySquare.KeptByTheGame.Contains(v)
            // and what kind of call a call of the Pokégear's phone is, which the game sets as it begins (plan 20 · J3)
            && !PokemonPlatinumEngine.Models.Pokegear.KeptByTheGame.Contains(v));
    }

    [Fact]
    public void EveryScriptOfTheGamePlaysToItsEndOnEveryWayThroughIt()
    {
        // Every script of a place as whatever starts it would start it, on the place's own map, and every common one
        // by someone who is everything at once (ScriptWalk.PlayEveryScript)
        var played = new HashSet<string>();
        foreach (var (what, script, ways) in Walk.PlayEveryScript())
        {
            if (script.File != ScriptLibrary.Common) played.Add(script.FullName);
            Assert.NotEmpty(ways);
            foreach (var way in ways)
                Assert.True(way.Host.Problems.Count == 0, $"{script.FullName}, started by {what}: {string.Join("; ", way.Host.Problems)}");
        }
        // Every script of a place that someone or something starts has been played (a clown's has two ways: yes and no)
        Assert.Superset(new HashSet<string> { "jubilife_city.Clown1", "jubilife_city.Clown2", "jubilife_city.Clown3" }, played);
    }

    // ---- items on the ground

    private static IEnumerable<(Map Map, NPC Ball)> ItemBalls() =>
        Walk.OwnMaps.Values.SelectMany(map => map.Everyone.Where(n => n.IsItemBall).Select(ball => (map, ball)));

    [Fact]
    public void ItemsLieOnTheGroundWhereTheOriginalHasThem()
    {
        var balls = ItemBalls().ToList();
        // Thirty-seven in the areas open after plan 01 · M5; more with every area opened
        Assert.True(balls.Count >= 37, $"only {balls.Count} items lie on the ground");
        foreach (var (map, ball) in balls)
        {
            string where = $"the {ball.Item} at {ball.GridX},{ball.GridY} of {map.Name}";
            Assert.True(ItemDatabase.Get(ball.Item!) != null, $"{where} is no item");
            Assert.True(ball.ItemCount >= 1);
            Assert.False(string.IsNullOrEmpty(ball.HiddenBy), $"{where} has no flag to keep it gone");
            // A ball whose place's script decides what is in it (Turnback Cave's last room) runs that script
            Assert.Equal(ball.Script ?? FieldScripts.ItemBall, FieldScripts.For(ball));
            Assert.True(map.AreaAt(ball.GridX, ball.GridY)?.Open != false, $"{where} lies outside the open areas");
        }
        // Each ball has a flag of its own: picking one up takes no other away. But for one the original shares: the
        // TM in the Ruin Maniac's cave, which lies at the end of his tunnel once he has dug it (one or the other is there)
        var shared = new[] { "FLAG_OBTAINED_RUIN_MANIAC_CAVE_OR_TUNNEL_TM28" };
        Assert.Empty(balls.GroupBy(b => b.Ball.HiddenBy).Where(g => g.Count() > 1 && !shared.Contains(g.Key)).Select(g => g.Key));

        // What the user asked to see, where Platinum has it: the Potion beside Route 202's grass and the Poké Ball of Route 203
        var overworld = Walk.OwnMaps["Sinnoh"];
        Assert.Equal("Potion", overworld.FindPerson("item_potion", "route_202")!.Item);
        Assert.Contains(balls, b => b.Ball.Item == "Poké Ball" && b.Ball.ScriptFile == "route_203");
        Assert.Contains(balls, b => b.Ball.Item == "Rare Candy");
    }

    [Fact]
    public void AnItemPickedUpIsThePlayersAndItsBallIsGoneForGood()
    {
        // A ball whose own script decides what is in it has a test of its own (TurnbackCaveTests)
        foreach (var (map, ball) in ItemBalls().Where(b => b.Ball.Script == null).ToList())
        {
            var host = new HeadlessScriptHost { Map = map };
            map.ApplyPresence(host.Story.Has);
            Assert.Contains(ball, map.NPCs);
            Assert.False(map.IsWalkable(ball.GridX, ball.GridY));

            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Scripts.Find(FieldScripts.For(ball)!, ball.ScriptFile)!, ball);
            runner.RunToEnd();

            Assert.Equal(ball.ItemCount, host.Bag.GetQuantity(ItemDatabase.Get(ball.Item!)!));
            Assert.True(host.Story.Has(ball.HiddenBy!));
            Assert.Contains($"fanfare {ScriptRunner.FanfareFor(ItemDatabase.Get(ball.Item!)!)}", host.Log);
            Assert.StartsWith($"{host.PlayerName} found ", host.Transcript[0].Text);

            // The flag is what takes it off the map: nothing stands there any more, now or after a save is loaded
            map.ApplyPresence(host.Story.Has);
            Assert.DoesNotContain(ball, map.NPCs);
            // (no ball, that is: the Lunar Wing's lay on the tile where Cresselia rests until it is come upon, plan 02 · S15, part b)
            Assert.DoesNotContain(map.NPCs, n => n.IsItemBall && (n.GridX, n.GridY) == (ball.GridX, ball.GridY));
            var loaded = new StoryState();
            loaded.Restore(host.Story.Snapshot());
            Assert.False(ball.IsPresent(loaded.Has));

            map.ApplyPresence(_ => false);
        }
    }

    [Fact]
    public void WhatIsHiddenInTheGroundIsFoundOnceByLookingAtIt()
    {
        var hidden = Walk.OwnMaps.Values.SelectMany(map => map.HiddenItems.Select(h => (Map: map, At: h.Key, Item: h.Value))).ToList();
        Assert.True(hidden.Count >= 20, $"only {hidden.Count} hidden items");
        // The original gives one flag to two places now and then, and says so in its name (Route 207's or Wayward
        // Cave's), or the two are one place shown on two maps (Mt. Coronet's north and south faces share a ledge and
        // its two Star Pieces): whichever is found first, the other is gone too
        Assert.Empty(hidden.GroupBy(h => h.Item.Flag)
            .Where(g => g.Count() > 1 && !g.Key.Contains("_OR_") && !g.All(h => h.Map.Name.StartsWith("MtCoronetOutside", StringComparison.Ordinal)))
            .Select(g => g.Key));

        foreach (var (map, at, item) in hidden)
        {
            Assert.True(map.InBounds(at.X, at.Y));
            Assert.True(ItemDatabase.Get(item.Item) != null, $"the hidden '{item.Item}' of {map.Name} is no item");
            // Platinum's flags, or FireRed's in Kanto (plan 19 · K1)
            Assert.True(item.Flag.StartsWith("FLAG_OBTAINED_HIDDEN_", StringComparison.Ordinal) || item.Flag.StartsWith("FLAG_HIDDEN_ITEM_", StringComparison.Ordinal), item.Flag);

            var host = new HeadlessScriptHost { Map = map };
            Assert.Same(item, FieldScripts.HiddenAt(map, at.X, at.Y, host.Story));
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Scripts.Find(FieldScripts.HiddenItem)!, item: (item.Item, item.Count), flag: item.Flag);
            runner.RunToEnd();

            Assert.Equal(item.Count, host.Bag.GetQuantity(ItemDatabase.Get(item.Item)!));
            Assert.Null(FieldScripts.HiddenAt(map, at.X, at.Y, host.Story));
        }
        // Nothing is hidden where nothing is
        Assert.Null(FieldScripts.HiddenAt(Walk.OwnMaps["Sinnoh"], 0, 0, new StoryState()));
    }

    [Fact]
    public void AMapFileCanLayItemsOutAndRefusesOnesThatWouldComeBack()
    {
        MapFile Laid(Action<MapFile> change)
        {
            var file = new MapFile
            {
                Name = "Test", DisplayName = "Test", Width = 4, Height = 3,
                Ground = new List<string> { "....", "....", "...." },
                Solid = new List<string> { "....", "....", "...." },
                Npcs = new List<MapFile.NpcRecord>
                {
                    new() { Name = "Rare Candy", NpcType = NPC.ItemBallType, X = 1, Y = 1, HiddenBy = "FLAG_OBTAINED_TEST_RARE_CANDY", Item = "Rare Candy" },
                    new() { Name = "Poké Ball", NpcType = NPC.ItemBallType, X = 2, Y = 1, HiddenBy = "FLAG_OBTAINED_TEST_POKE_BALL", Item = "Poké Ball", Count = 5 }
                },
                HiddenItems = new List<MapFile.HiddenItemRecord>
                {
                    new() { X = 3, Y = 2, Item = "Nugget", Flag = "FLAG_OBTAINED_HIDDEN_TEST_NUGGET" },
                    new() { X = 0, Y = 0, Item = "Stardust", Count = 2, Flag = "FLAG_OBTAINED_HIDDEN_TEST_STARDUST" }
                }
            };
            change(file);
            return file;
        }

        var file = Laid(_ => { });
        var map = file.ToMap();
        Assert.Equal(("Rare Candy", 1, true), (map.NPCs[0].Item, map.NPCs[0].ItemCount, map.NPCs[0].IsItemBall));
        Assert.Equal(5, map.NPCs[1].ItemCount);
        Assert.Equal(new HiddenItem("Stardust", 2, "FLAG_OBTAINED_HIDDEN_TEST_STARDUST"), map.HiddenItems[(0, 0)]);
        Assert.Equal(GameDataFiles.Serialize(file), GameDataFiles.Serialize(MapFile.FromMap(map)));

        // An item that nothing would keep gone, an item that isn't one, an item on someone who is no ball
        Assert.Contains("come back", Assert.Throws<InvalidDataException>(() => Laid(f => f.Npcs[0].HiddenBy = null).ToMap()).Message);
        Assert.Contains("no item", Assert.Throws<InvalidDataException>(() => Laid(f => f.Npcs[0].Item = "Rare Sweet").ToMap()).Message);
        Assert.Contains("no item ball", Assert.Throws<InvalidDataException>(() => Laid(f => f.Npcs[0].NpcType = "Lass").ToMap()).Message);
        Assert.Contains("again and again", Assert.Throws<InvalidDataException>(() => Laid(f => f.HiddenItems![0].Flag = "").ToMap()).Message);
        Assert.Contains("no item", Assert.Throws<InvalidDataException>(() => Laid(f => f.HiddenItems![0].Item = "Gold Lump").ToMap()).Message);
    }

    [Fact]
    public void AnItemsBallIsPaintedAsTheStyleGuideHasIt()
    {
        var art = new PokemonPlatinumEngine.Graphics.PixelCanvas(PokemonPlatinumEngine.Graphics.OutdoorProps.ItemBallCard, PokemonPlatinumEngine.Graphics.OutdoorProps.ItemBallCard);
        PokemonPlatinumEngine.Graphics.OutdoorProps.PaintItemBall(art);
        (int R, int G, int B) At(int x, int y) => (art.Get(x, y).R, art.Get(x, y).G, art.Get(x, y).B);

        // Red above, white below, the band between with its white button, and the lit and the shaded sides
        Assert.Equal((218, 62, 58), At(10, 4));
        Assert.Equal((240, 240, 236), At(8, 15));
        Assert.Equal((58, 52, 72), At(3, 9));
        Assert.Equal((58, 52, 72), At(16, 10));
        Assert.Equal((240, 240, 236), At(9, 9));
        Assert.Equal((244, 120, 104), At(5, 4));
        Assert.Equal((170, 42, 56), At(16, 7));
        Assert.Equal((198, 200, 214), At(15, 14));

        // Eighteen texels across and as many high, with the outline's texel all round and nothing in the corners
        int Opaque(Func<int, bool> line) => Enumerable.Range(0, 20).Count(line);
        Assert.Equal(20, Opaque(x => art.IsOpaque(x, 9)));
        Assert.Equal(20, Opaque(y => art.IsOpaque(9, y)));
        Assert.False(art.IsOpaque(0, 0) || art.IsOpaque(19, 0) || art.IsOpaque(0, 19) || art.IsOpaque(19, 19));
        // The outline is a darker shade of what it touches, never the fill itself
        Assert.NotEqual((218, 62, 58), At(10, 0));
        Assert.True(art.IsOpaque(10, 0) && art.IsOpaque(10, 19));
    }

    [Fact]
    public void ANewGameSetsThingsUpWithoutAWord()
    {
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, Scripts);
        // Nothing of the story has been written yet that hides anyone: every open area has its people (plan 02 · S4 on)
        Assert.Equal(0, story.BadgeCount("Sinnoh"));
        Assert.Null(story.PlayerStarter);
    }

    // ---- the common scripts

    private static (HeadlessScriptHost Host, ScriptRunner Runner) Play(string script, NPC? subject = null, IReadOnlyList<string>? own = null, Action<HeadlessScriptHost>? before = null, string? file = null)
    {
        var host = new HeadlessScriptHost { Map = new Map(8, 8) };
        before?.Invoke(host);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(script, file)!, subject, own);
        runner.RunToEnd();
        return (host, runner);
    }

    private static Pokemon Hurt()
    {
        var pokemon = new Pokemon(PokemonDatabase.Get("Turtwig")!, 8);
        pokemon.CurrentHP = 1;
        return pokemon;
    }

    [Fact]
    public void TheNurseAsksFirstAndHealsOnlyWhenToldTo()
    {
        var nurse = new NPC { Name = "Nurse Joy", IsHealingNurse = true };

        var (yes, _) = Play(FieldScripts.For(nurse)!, nurse, before: h => h.Party.Add(Hurt()));
        Assert.Equal(yes.Party.Members[0].MaxHP, yes.Party.Members[0].CurrentHP);
        // She turns to her machine, which lays a ball for the one Pokémon and lights it to the heal's fanfare (plan 09 · L12)
        Assert.Equal(new[] { "anim heal 1" }, yes.Log);
        Assert.Equal(Direction.Down, nurse.Facing);
        Assert.Equal("Yes", yes.Asked.Single().Answer);
        Assert.All(yes.Transcript, line => Assert.Equal("Nurse Joy", line.Speaker));

        var (no, _) = Play(FieldScripts.For(nurse)!, nurse, before: h =>
        {
            h.Party.Add(Hurt());
            h.Answers.Enqueue(1);
        });
        Assert.Equal(1, no.Party.Members[0].CurrentHP);
        Assert.Empty(no.Log);
        // Either way she sees the player off with the same words
        Assert.Equal(yes.Transcript[^1].Text, no.Transcript[^1].Text);
        Assert.True(no.Transcript.Count < yes.Transcript.Count);
    }

    [Fact]
    public void SomeoneAtHomeHealsTheTeamWithTheirOwnWords()
    {
        foreach (string house in new[] { "PlayerHouse" })
        {
            var mom = Walk.OwnMaps[house].Everyone.Single(n => n.IsHealingNurse);
            var (host, _) = Play(FieldScripts.For(mom)!, mom, before: h => h.Party.Add(Hurt()), file: house);

            Assert.Equal(mom.DialogLines.Select(l => PlayerIdentity.Fill(l, host.PlayerName, host.PlayerLook)), host.Transcript.Select(t => t.Text));
            Assert.Equal(host.Party.Members[0].MaxHP, host.Party.Members[0].CurrentHP);
            Assert.Empty(host.Asked);
        }
    }

    [Fact]
    public void TheClerkGreetsOpensTheCounterAndSeesThePlayerOff()
    {
        var clerk = Walk.OwnMaps["JubilifePokeMart"].Everyone.Single(n => n.IsPokeMartClerk && n.Mart == null);
        var (host, _) = Play(FieldScripts.For(clerk)!, clerk);

        Assert.Contains("Jubilife", host.Transcript[0].Text);
        Assert.Equal(new[] { "open Shop" }, host.Log);
        Assert.Equal(2, host.Transcript.Count);

        // The second clerk keeps the town's own counter (plan 06 · R11)
        var own = Walk.OwnMaps["JubilifePokeMart"].Everyone.Single(n => n.Mart == "jubilife");
        (host, _) = Play(FieldScripts.For(own)!, own);
        Assert.Equal(new[] { "open Shop jubilife" }, host.Log);
    }

    [Fact]
    public void ThePcTheBriefcaseAndTheAttendantOpenWhatTheyAre()
    {
        var (pc, _) = Play(FieldScripts.Pc, new NPC { Name = "PC Terminal", IsPCTerminal = true }, before: h => h.PlayerName = "Ana");
        // The screen comes on before the line and goes off as the PC is left (plan 09 · L12)
        Assert.Equal(new[] { "anim pc on", "open Pc", "anim pc off" }, pc.Log);
        Assert.Equal((null, "Ana switched the PC on."), pc.Transcript.Single());

        var (briefcase, runner) = Play(FieldScripts.Briefcase, new NPC { IsStarterBriefcase = true }, before: h => h.StarterChoice = 2);
        Assert.Equal("Piplup", briefcase.Party.Members.Single().Species.Name);
        Assert.Equal(("Piplup", "Turtwig"), (briefcase.Story.PlayerStarter, briefcase.Story.RivalStarter));
        Assert.Equal(2, runner.Result);

        // Nowhere to sail from a map with no screen: the attendant falls back on what they have to say
        var (attendant, _) = Play(FieldScripts.Attendant, new NPC { Name = "Sailor", IsTransportAttendant = true, DialogLines = { "The sea is calm today." } });
        Assert.Equal(new[] { "open Travel" }, attendant.Log);
        Assert.Equal("The sea is calm today.", attendant.Transcript.Single().Text);
    }

    [Fact]
    public void ATrainerChallengesBattlesAndHasALastWordAfterwards()
    {
        var trainer = ScriptWalk.AnyTrainer();
        var (first, _) = Play(FieldScripts.For(trainer)!, trainer, before: h => h.Money = 100);
        Assert.Equal(new[] { "Before." }, first.Transcript.Select(t => t.Text));
        Assert.Equal(new[] { "battle anyone Won" }, first.Log);
        Assert.Equal(164, first.Money);
        Assert.True(trainer.HasBattled);

        var (after, _) = Play(FieldScripts.For(trainer)!, trainer);
        Assert.Equal(new[] { "After." }, after.Transcript.Select(t => t.Text));
        Assert.Empty(after.Log);

        // One with no last word says what they would say to anyone
        trainer.TrainerData!.DialogueAfter = "";
        var (plain, _) = Play(FieldScripts.For(trainer)!, trainer);
        Assert.Equal(new[] { "Something to say." }, plain.Transcript.Select(t => t.Text));

        // A game loaded later knows them as beaten by the story alone
        var fresh = ScriptWalk.AnyTrainer();
        var (loaded, _) = Play(FieldScripts.For(fresh)!, fresh, before: h => h.Story.Defeat("anyone"));
        Assert.Equal(new[] { "After." }, loaded.Transcript.Select(t => t.Text));
    }

    [Fact]
    public void ASignIsReadUnderTheWordSign()
    {
        var (host, _) = Play(FieldScripts.Sign, own: new[] { "Jubilife City\nSinnoh's busiest city." });
        Assert.Equal(("Sign", "Jubilife City\nSinnoh's busiest city."), host.Transcript.Single());
    }

    // ---- a place's own: the Pokétch campaign's clowns

    [Theory]
    [InlineData("clown_1", "Coupon 1", "FLAG_RECEIVED_COUPON_1")]
    [InlineData("clown_2", "Coupon 2", "FLAG_RECEIVED_COUPON_2")]
    [InlineData("clown_3", "Coupon 3", "FLAG_RECEIVED_COUPON_3")]
    public void AClownGivesACouponForTheRightAnswerOnce(string who, string coupon, string flag)
    {
        var map = Walk.MapOfFile("jubilife_city")!;
        var clown = map.FindPerson(who, "jubilife_city")!;
        Assert.Equal("jubilife_city", clown.ScriptFile);
        var script = Scripts.Find(FieldScripts.For(clown)!, clown.ScriptFile)!;
        Assert.Equal("jubilife_city", script.File);
        var item = ItemDatabase.Get(coupon)!;

        // The third clown has nothing to give until the president has told the campaign (plan 02 · S5)
        if (who == "clown_3")
        {
            var early = new HeadlessScriptHost { Map = map };
            var waiting = new ScriptRunner(Scripts, early);
            waiting.Start(script, clown);
            waiting.RunToEnd();
            Assert.Empty(early.Asked);
            Assert.Equal(0, early.Bag.GetQuantity(item));
        }

        // No: nothing given, and the question is asked again next time
        var wrong = new HeadlessScriptHost { Map = map };
        wrong.Story.SetVar("VAR_POKETCH_CAMPAIGN_STATE", 2);
        wrong.Answers.Enqueue(1);
        var runner = new ScriptRunner(Scripts, wrong);
        runner.Start(script, clown);
        runner.RunToEnd();
        Assert.Equal(0, wrong.Bag.GetQuantity(item));
        Assert.False(wrong.Story.Has(flag));
        Assert.Contains("sound bump", wrong.Log);

        // Yes: the coupon, with its fanfare
        var right = new HeadlessScriptHost { Map = map };
        right.Story.SetVar("VAR_POKETCH_CAMPAIGN_STATE", 2);
        runner = new ScriptRunner(Scripts, right);
        runner.Start(script, clown);
        runner.RunToEnd();
        Assert.Equal(1, right.Bag.GetQuantity(item));
        Assert.True(right.Story.Has(flag));
        // A coupon is a key item, received to the key item's fanfare
        Assert.Contains($"fanfare {MusicRole.FanfareKeyItem}", right.Log);
        Assert.Equal("Clown", right.Transcript[0].Speaker);

        // Afterwards the clown only talks: no second coupon
        int said = right.Transcript.Count;
        runner.Start(script, clown);
        runner.RunToEnd();
        Assert.Equal(1, right.Bag.GetQuantity(item));
        Assert.Single(right.Asked);
        Assert.Equal(said + 1, right.Transcript.Count);
    }

    // ---- the world's people and the story

    [Fact]
    public void TheWorldsPeopleCarryTheirNamesForScriptsAndTheOriginalsFlags()
    {
        var overworld = Walk.OwnMaps["Sinnoh"];
        var looker = overworld.FindPerson("looker", "jubilife_city")!;
        Assert.Equal(("Looker", "jubilife_city", "FLAG_HIDE_JUBILIFE_CITY_LOOKER"), (looker.Name, looker.ScriptFile, looker.HiddenBy));

        // The flag that hides them in the original hides them here
        var story = new StoryState();
        overworld.ApplyPresence(story.Has);
        Assert.Contains(looker, overworld.NPCs);
        story.Set("FLAG_HIDE_JUBILIFE_CITY_LOOKER");
        overworld.ApplyPresence(story.Has);
        Assert.DoesNotContain(looker, overworld.NPCs);
        Assert.Null(overworld.GetNpcAt(looker.GridX, looker.GridY));
        overworld.ApplyPresence(_ => false);

        // Everyone an overlay places can be named by a script of their area
        foreach (var npc in overworld.Everyone.Where(n => n.Key != null))
            Assert.Same(npc, overworld.FindPerson(npc.Key!, npc.ScriptFile));
    }

    /// <summary>
    /// What an overlay can say of the story, tried on a copy of the world's files: no area's story is written yet,
    /// so the game's own overlays bind no trigger and script no sign.
    /// </summary>
    [Fact]
    public void AnOverlayBindsATriggerScriptsASignAndDecidesWhoAFlagHides()
    {
        string copy = Path.Combine(Path.GetTempPath(), "sinnoh-" + Guid.NewGuid().ToString("N"));
        string source = GameDataFiles.PathOf(Path.Combine(World.Folder, "sinnoh"));
        try
        {
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(copy, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }

            string path = Path.Combine(copy, "overlays", "jubilife_city.json");
            var overlay = JsonSerializer.Deserialize<WorldOverlayFile>(File.ReadAllText(path), GameDataFiles.Json)!;
            overlay.Triggers = new List<OverlayTrigger> { new() { Trigger = 0, Script = "FirstArrival" } };
            overlay.SignScripts = new Dictionary<string, string> { ["signboard_jubilife_tv"] = "TvSign" };
            overlay.People!["looker"].HiddenBy = "";
            overlay.People["counterpart"].HiddenBy = "FLAG_OUR_OWN";
            overlay.People["clown_1"].ShownBy = "FLAG_CAMPAIGN_BEGUN";
            File.WriteAllText(path, GameDataFiles.Serialize(overlay));

            var index = JsonSerializer.Deserialize<WorldIndexFile>(File.ReadAllText(Path.Combine(copy, World.IndexFile)), GameDataFiles.Json)!;
            var map = World.Open(copy, index).BuildMaps().Single(m => m.Name == "Sinnoh");

            // The trigger keeps the original's tiles, variable and value, and takes our script, looked for in the area's file
            var first = Walk.World.Area("jubilife_city")!.Triggers[0];
            var trigger = map.Triggers.Single(t => t.ScriptFile == "jubilife_city");
            Assert.Equal((first.X, first.Z, first.Width, first.Depth), (trigger.X, trigger.Y, trigger.Width, trigger.Depth));
            Assert.Equal(("FirstArrival", "jubilife_city", "VAR_JUBILIFE_CITY_STATE", 0), (trigger.Script, trigger.ScriptFile, trigger.Variable, trigger.Value));
            var story = new StoryState();
            Assert.Same(trigger, FieldScripts.TriggerAt(map, first.X + 1, first.Z, story));
            story.SetVar("VAR_JUBILIFE_CITY_STATE", 1);
            Assert.Null(FieldScripts.TriggerAt(map, first.X + 1, first.Z, story));

            // The sign has its script, and is still a sign with its text
            var sign = Walk.World.Area("jubilife_city")!.Objects.Single(o => o.Id == "signboard_jubilife_tv");
            Assert.Equal("TvSign", map.SignScripts[(sign.X, sign.Z)]);
            Assert.StartsWith("Jubilife TV", map.GetSignboardAt(sign.X, sign.Z));
            Assert.Equal("jubilife_city", map.ScriptFileAt(sign.X, sign.Z));

            // Whom a flag hides: nobody's flag ("" keeps Looker whatever the original's says), a flag of our own, a flag waited for
            Assert.Null(map.FindPerson("looker", "jubilife_city")!.HiddenBy);
            Assert.Equal("FLAG_OUR_OWN", map.FindPerson("counterpart", "jubilife_city")!.HiddenBy);
            var clown = map.FindPerson("clown_1", "jubilife_city")!;
            Assert.Equal(("FLAG_HIDE_JUBILIFE_CITY_CLOWNS_1_AND_2", "FLAG_CAMPAIGN_BEGUN"), (clown.HiddenBy, clown.ShownBy));
            map.ApplyPresence(story.Has);
            Assert.DoesNotContain(clown, map.NPCs);
            story.Set("FLAG_CAMPAIGN_BEGUN");
            map.ApplyPresence(story.Has);
            Assert.Contains(clown, map.NPCs);
        }
        finally
        {
            if (Directory.Exists(copy)) Directory.Delete(copy, recursive: true);
        }
    }

    [Fact]
    public void TheAreasKeepTheOriginalsTriggersForTheChaptersToBind()
    {
        // No area's story is written yet, so none is bound; the areas' files hold the triggers the chapters will bind
        var jubilife = Walk.World.Area("jubilife_city")!;
        Assert.Contains(jubilife.Triggers, t => t.Variable == "VAR_JUBILIFE_CITY_STATE" && t.Value == "0");
        Assert.All(Walk.World.Index.Areas.SelectMany(key => Walk.World.Area(key)!.Triggers), t => Assert.True(int.TryParse(t.Value, out _), $"a trigger waits for '{t.Value}'"));

        foreach (var map in Walk.OwnMaps.Values)
            foreach (var trigger in map.Triggers)
                Assert.True(map.InBounds(trigger.X, trigger.Y) && map.InBounds(trigger.X + trigger.Width - 1, trigger.Y + trigger.Depth - 1), $"a trigger of {map.Name} lies off it");
    }
}
