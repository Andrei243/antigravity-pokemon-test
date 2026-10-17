using System;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The region record (plan 18 · W1): what the engine read by name for Sinnoh now comes from
/// <c>Data/regions/&lt;id&gt;.json</c>, region by region, and the introduction, the briefcase and a new game's
/// beginning follow the region they are handed. It begins and loads whole games, which put the modern rules in force
/// for the whole process (plan 06 · R19), so it runs alone and puts Platinum's back as it ends.
/// </summary>
[Collection("Shared clocks and dice")]
public class RegionRecordTests
{
    private const float Frame = 1f / 60f;

    private static Region Of(string id) => RegionDatabase.Get(id)!;

    private static readonly string[] FilesRegions = { RegionDatabase.Kanto, RegionDatabase.Sinnoh };

    [Fact]
    public void EveryRegionWithAFileHasWhatAnIntroductionAndABriefcaseNeed()
    {
        foreach (string id in FilesRegions)
        {
            var region = Of(id);
            Assert.NotNull(region.File);
            Assert.Equal(id, region.File!.Id);
            Assert.Equal(3, region.Starters.Count);
            Assert.All(region.Starters, s => Assert.NotNull(PokemonDatabase.Get(s)));
            Assert.Equal(region.Starters.Count, region.Starters.Distinct().Count());
            var professor = region.Professor!;
            Assert.NotEmpty(professor.Name);
            Assert.True(CharacterStyles.Default.Has(professor.Look), $"{id}'s professor is drawn as {professor.Look}, which characters.json lacks");
            Assert.NotNull(PokemonDatabase.Get(professor.Species));
            Assert.True(CharacterStyles.Default.Has(region.Heroes.Boy) && CharacterStyles.Default.Has(region.Heroes.Girl));
            Assert.Equal(8, region.BadgeNames.Count);
            Assert.Equal(8, region.BadgeNames.Distinct().Count());
            Assert.NotEmpty(region.File.Dex);
            Assert.NotEmpty(region.File.NationalUnlock);
            Assert.NotEmpty(region.Folder);
            // Every line of the introduction is written
            foreach (var property in typeof(RegionIntroLines).GetProperties())
                Assert.False(string.IsNullOrWhiteSpace((string)property.GetValue(professor.Lines)!), $"{id}'s {property.Name}");
        }
        // The record of a region without a file is empty, not missing
        Assert.Null(Of(RegionDatabase.Johto).File);
        Assert.Empty(Of(RegionDatabase.Johto).Starters);
        Assert.False(Of(RegionDatabase.Johto).HasStory);
    }

    [Fact]
    public void SinnohsRecordIsWhatTheEngineKnewByName()
    {
        var sinnoh = Of(RegionDatabase.Sinnoh);
        Assert.True(sinnoh.HasStory);
        Assert.Equal(new[] { "Turtwig", "Chimchar", "Piplup" }, sinnoh.Starters);
        Assert.Equal(new[] { "Coal", "Forest", "Cobble", "Fen", "Relic", "Mine", "Icicle", "Beacon" }, sinnoh.BadgeNames);
        Assert.Equal("Barry", sinnoh.Rival);
        Assert.Equal("Barry", PlayerIdentity.DefaultRivalName);
        Assert.Equal("Sinnoh", sinnoh.OverworldMap);
        Assert.Equal("sinnoh", sinnoh.Folder);
        Assert.Equal(("PLAYER", "DAWN"), (sinnoh.Heroes.Boy, sinnoh.Heroes.Girl));
        Assert.Equal(StoryState.Starters, sinnoh.Starters);
        // The rival takes the next one round
        Assert.Equal("Chimchar", sinnoh.RivalStarterFor("Turtwig"));
        Assert.Equal("Turtwig", sinnoh.RivalStarterFor("Piplup"));
        Assert.Equal("Chimchar", StoryState.RivalStarterFor("Turtwig"));
        // The twenty places Fly lands, as plan 02 · S2 wrote them
        Assert.Equal(20, SpawnLocations.All.Count);
        Assert.Equal(Enumerable.Range(1, 20), SpawnLocations.Of(sinnoh).Select(s => s.Id));
        Assert.Equal(("twinleaf_town", 116, 886, "PlayerHouse"), (SpawnLocations.Get(1)!.Area, SpawnLocations.Get(1)!.X, SpawnLocations.Get(1)!.Y, SpawnLocations.Get(1)!.Room));
        Assert.False(SpawnLocations.Get(15)!.UnlockedOnArrival);
        Assert.Equal("FLAG_FIRST_ARRIVAL_OUTSIDE_VICTORY_ROAD", SpawnLocations.Get(15)!.ArrivalFlag);
        Assert.Equal("FLAG_FIRST_ARRIVAL_ETERNA_CITY", SpawnLocations.Get(9)!.ArrivalFlag);
        // Its catalogue is the one the world's props are looked up in; a region with no world has an empty one
        Assert.Same(WorldModels.All, WorldModels.Catalogue(sinnoh.Models));
        Assert.Empty(WorldModels.Catalogue(Of(RegionDatabase.Kanto).Models));
        Assert.Equal("sinnoh", sinnoh.Models);
        // A map that no world has is asked for, not made up
        Assert.Null(MapDatabase.TryGet("a_map_nobody_made"));
        Assert.NotNull(MapDatabase.TryGet("Sinnoh"));
    }

    /// <summary>Sinnoh's introduction, word for word, kept here so that moving the lines to a file changed none of them.</summary>
    [Fact]
    public void SinnohsIntroductionSaysWhatItAlwaysSaid()
    {
        var intro = new IntroScreen();
        intro.Open();
        Assert.Equal(Of(RegionDatabase.Sinnoh), intro.Region);
        Assert.Equal(("Prof. Rowan", "ROWAN", "Buneary"), (intro.Professor, intro.ProfessorLook, intro.ShownSpecies));
        Assert.Equal("Barry", intro.RivalName);

        var said = Of(RegionDatabase.Sinnoh).Professor!.Lines;
        Assert.Equal("Hello there! I'm glad you could come.", said.Hello);
        Assert.Equal("Welcome to the world of Pokémon!", said.Welcome);
        Assert.Equal("My name is Rowan. People call me the Pokémon Professor.", said.MyName);
        Assert.Equal("This world is home to the creatures we call Pokémon. They live in the tall grass, in caves, in the sea and in the sky.", said.World);
        Assert.Equal("This is one of them. Friendly, isn't it?", said.OneOfThem);
        Assert.Equal("We people live alongside Pokémon. Some of us keep them as friends and some battle together with them.", said.LiveAlongside);
        Assert.Equal("As for me, I study them. There is still so much about them that nobody knows.", said.Study);
        Assert.Equal("But that's enough about me. Tell me about yourself.", said.Enough);
        Assert.Equal("Are you a boy, or are you a girl?", said.BoyOrGirl);
        Assert.Equal("And what is your name?", said.AskName);
        Assert.Equal("And this is the boy from next door. You two have been friends for as long as either of you can remember.", said.NextDoor);
        Assert.Equal("He's never still for a moment, that one. Now, what was his name again?", said.NeverStill);
        Assert.Equal("{name}! That's a fine name.", said.FineName);
        Assert.Equal("{rival}, of course! How could I forget?", said.OfCourse);
        Assert.Equal("{name}, your very own story is about to begin. You will meet many Pokémon and many people on your way.", said.Story);
        Assert.Equal("Now, off you go. The world of Pokémon is waiting!", said.OffYouGo);
    }

    [Fact]
    public void AKantoGamesIntroductionSaysOakAndShowsKantosStarters()
    {
        var kanto = Of(RegionDatabase.Kanto);
        var intro = new IntroScreen();
        intro.Open(region: kanto);
        Assert.Equal(("Prof. Oak", "Blue"), (intro.Professor, intro.RivalName));
        Assert.Contains("Oak", kanto.Professor!.Lines.MyName);

        // Greeted by the professor under his own name, whatever the player does next
        For(intro, IntroPhase.Greeting);
        Assert.Equal("", intro.Speaker);
        for (int guard = 0; guard < 8 && intro.Speaker == ""; guard++) intro.PressConfirm();
        Assert.Equal("Prof. Oak", intro.Speaker);
        intro.PressConfirm();
        Assert.Contains("Oak", intro.SpokenText);

        // The briefcase offers Kanto's three
        var screen = new StarterSelectScreen();
        Assert.Equal(new[] { "Turtwig", "Chimchar", "Piplup" }, screen.Starters);
        screen.Open(kanto);
        Assert.Equal(new[] { "Bulbasaur", "Charmander", "Squirtle" }, screen.Starters);
        screen.Move(-1);
        Assert.Equal(2, screen.SelectedIndex);
        screen.Confirm();
        screen.Move(1);
        var given = screen.Confirm();
        Assert.Equal("Squirtle", given!.Species.Name);
    }

    [Fact]
    public void ARegionWhoseRivalIsTheOtherHeroNamesNoRival()
    {
        var withRival = RegionOnIts(rival: "Gary");
        var without = RegionOnIts(rival: null);
        Assert.True(Name(withRival).AskedForARival);
        Assert.Equal(IntroPhase.AskRival, Name(withRival).Phase);
        Assert.False(Name(without).AskedForARival);
        Assert.Equal(IntroPhase.Farewell, Name(without).Phase);
        Assert.DoesNotContain("next door", Name(without).SpokenAfter);
    }

    private static Region RegionOnIts(string? rival) => new()
    {
        Id = "Test", Name = "Test", Generation = 99,
        File = new RegionFile
        {
            Id = "Test", Rival = rival, Starters = { "Turtwig", "Chimchar", "Piplup" },
            Professor = new RegionProfessor { Name = "Prof. Test", Species = "Buneary", Lines = Of(RegionDatabase.Sinnoh).Professor!.Lines }
        }
    };

    private static (bool AskedForARival, IntroPhase Phase, string SpokenAfter) Name(Region region)
    {
        var intro = new IntroScreen();
        intro.Open(region: region);
        For(intro, IntroPhase.ChooseLook);
        intro.PressConfirm();
        intro.PressConfirm();
        For(intro, IntroPhase.EnterName);
        intro.PressStart();
        intro.PressConfirm();
        Assert.Equal(IntroPhase.ConfirmName, intro.Phase);
        intro.PressConfirm();
        var phase = intro.Phase;
        string spoken = "";
        for (int guard = 0; guard < 4000 && intro.Phase != IntroPhase.SendOff; guard++)
        {
            if (intro.Phase == IntroPhase.EnterRival) break;
            if (intro.Talking) { intro.PressConfirm(); spoken += intro.SpokenText + "|"; }
            intro.Advance(Frame);
        }
        return (intro.NamesARival, phase, spoken);
    }

    private static void For(IntroScreen intro, IntroPhase until)
    {
        for (int guard = 0; guard < 4000 && intro.Phase != until; guard++)
        {
            if (intro.Talking) intro.PressConfirm();
            intro.Advance(Frame);
        }
        Assert.Equal(until, intro.Phase);
    }

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    [Fact]
    public void TheKantoStandInBeginsWithItsFirstStarterAndTheSinnohGameWithNothing()
    {
        string folder = Path.Combine(Path.GetTempPath(), "region-record-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            var kanto = Headless();
            kanto.NewGameRegion = "Kanto";
            kanto.StartNewGame();
            Assert.Equal(1, kanto.Drive.Party.Count);
            Assert.Equal("Bulbasaur", kanto.Drive.Party.Members[0].Species.Name);
            Assert.Equal("Kanto", kanto.Here.Id);
            Assert.Equal("Blue", PlayerIdentity.RivalName);
            Assert.True(kanto.Drive.Bag.GetQuantity(ItemDatabase.Get("Potion")!) > 0);
            kanto.Close();

            var sinnoh = Headless();
            sinnoh.NewGameRegion = "Sinnoh";
            sinnoh.StartNewGame();
            Assert.Equal(0, sinnoh.Drive.Party.Count);
            Assert.Equal("Sinnoh", sinnoh.Here.Id);
            Assert.Equal("Barry", PlayerIdentity.RivalName);

            // A game saved in the opening has no Pokémon, and loads with none (it was a Turtwig before plan 02 · S4)
            Assert.True(sinnoh.Drive.Save());
            int slot = sinnoh.Drive.SaveSlot;
            var again = Headless();
            again.ContinueGame(slot);
            Assert.Equal(0, again.Drive.Party.Count);
            Assert.Equal("Sinnoh", again.Here.Id);
            again.Close();
            sinnoh.Close();
        }
        finally
        {
            SavePaths.Current = paths;
            PlayerIdentity.SetRival(null);
            // The games put the modern rules in force; the rest of the run reads Platinum's baseline (as HeadlessGameTests)
            Ruleset.Use(RulesPreset.Platinum);
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }

    [Fact]
    public void ANewGameWithNoRegionNamedBeginsInSinnohsBedroomWithRowansIntroduction()
    {
        // Plan 18 · W10: nothing on the command line, nothing asked at NEW GAME
        string folder = Path.Combine(Path.GetTempPath(), "region-record-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            var engine = Headless();
            Assert.Null(engine.NewGameRegion);
            engine.StartNewGame();
            Assert.Equal("Sinnoh", engine.Here.Id);
            Assert.Equal("PlayerHouse2F", engine.Drive.Map.Name);
            Assert.Equal(0, engine.Drive.Party.Count);
            Assert.Equal("Barry", PlayerIdentity.RivalName);
            engine.Close();
        }
        finally
        {
            SavePaths.Current = paths;
            PlayerIdentity.SetRival(null);
            Ruleset.Use(RulesPreset.Platinum);
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }
}
