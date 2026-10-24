using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W4, part b: the crossing's rules. Under the user's option B the team crosses whole; a Pokémon belongs to
/// the region it was met in and a traded one to the receiver's; each region has its own rival's name; and the
/// headless host plays the attendant's travel through a crossing, as the game does.
/// </summary>
[Collection("Shared clocks and dice")]
public class CrossingTests
{
    private const float Frame = 1f / 60f;

    private static Pokemon Mon(string species, int level = 30) => new(PokemonDatabase.Get(species)!, level);

    [Fact]
    public void TheWholeTeamCrossesAndNothingRestsInTheBoxes()
    {
        var party = new Party();
        foreach (var species in new[] { "Infernape", "Staraptor", "Luxray", "Garchomp" }) party.Add(Mon(species, 60));
        party.Members[0].Region = RegionDatabase.Sinnoh;
        party.Members[1].Region = RegionDatabase.Kanto;
        var boxes = new PcBoxes();
        var before = party.Members.ToList();
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        var story = new StoryState();

        Assert.All(before, p => Assert.True(Crossing.CanTake(p, kanto, story)));
        Assert.Empty(Crossing.Depart(party, boxes, kanto, story));
        Assert.Equal(before, party.Members);
        Assert.Equal(0, boxes.Count);
    }

    [Fact]
    public void APokemonBelongsToTheRegionItWasMetInAndATradedOneToTheReceivers()
    {
        var met = Mon("Bidoof");
        var older = Mon("Starly");
        older.Region = RegionDatabase.Sinnoh;
        Assert.Equal(1, Crossing.Belong(new[] { met, older }, RegionDatabase.Kanto));
        Assert.Equal(RegionDatabase.Kanto, met.Region);
        Assert.Equal(RegionDatabase.Sinnoh, older.Region);

        // Wherever a traded one comes from, it joins the region of whoever received it
        var traded = Mon("Machoke");
        traded.Region = RegionDatabase.Johto;
        Crossing.Join(traded, RegionDatabase.Kanto);
        Assert.Equal(RegionDatabase.Kanto, traded.Region);

        // and an older save's Pokémon, which has none, belongs to the save's own region
        Assert.True(Crossing.BelongsTo(Mon("Shinx"), RegionDatabase.Sinnoh, RegionDatabase.Sinnoh));
        Assert.False(Crossing.BelongsTo(met, RegionDatabase.Sinnoh, RegionDatabase.Sinnoh));
    }

    [Fact]
    public void ThePokemonsRegionIsSavedAndAnOlderSavesIsLeftOut()
    {
        var p = Mon("Turtwig");
        p.Region = RegionDatabase.Kanto;
        Assert.Equal(RegionDatabase.Kanto, SavedPokemonData.FromPokemon(p).ToPokemon().Region);

        var save = new SaveData { CurrentMapName = "Sinnoh", Party = new() { SavedPokemonData.FromPokemon(Mon("Chimchar")) } };
        string json = SaveManager.Write(save);
        Assert.DoesNotContain("\"region\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Null(SaveManager.Read(json)!.Party[0].ToPokemon().Region);
        // A copy for a battle keeps it, as it keeps the id
        var copy = Mon("Piplup");
        copy.CopyStateFrom(p);
        Assert.Equal(RegionDatabase.Kanto, copy.Region);
    }

    [Fact]
    public void EachRegionHasItsOwnRivalAndAnOlderSavesNameIsItsOwnRegions()
    {
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        try
        {
            PlayerIdentity.Set("Lucas", PlayerLook.Boy);
            // An older save named one rival, in the region it began in
            PlayerIdentity.RestoreRivals(null, "Kit", RegionDatabase.Sinnoh);
            PlayerIdentity.InRegion(sinnoh);
            Assert.Equal("Kit", PlayerIdentity.RivalName);
            Assert.Equal("Kit!", PlayerIdentity.Fill("{rival}!"));

            // In Kanto {rival} names Kanto's rival, the region's own until a namerival names him
            PlayerIdentity.InRegion(kanto);
            Assert.Equal("Blue", PlayerIdentity.Fill("{rival}"));
            Assert.False(PlayerIdentity.HasNamedRival(RegionDatabase.Kanto));
            PlayerIdentity.SetRival("Gary", kanto.Rival, RegionDatabase.Kanto);
            Assert.Equal("Gary", PlayerIdentity.Fill("{rival}"));
            // and Sinnoh's keeps his own
            Assert.Equal("Kit", PlayerIdentity.RivalOf(sinnoh));
            Assert.Equal(new[] { "Kanto", "Sinnoh" }, PlayerIdentity.RivalNames.Keys.OrderBy(k => k));

            // A region whose rival is the other hero names them by the player's look
            PlayerIdentity.InRegion(hoenn);
            Assert.Equal("May", PlayerIdentity.RivalName);

            // A newer save's names are each their region's
            PlayerIdentity.RestoreRivals(new Dictionary<string, string> { ["Kanto"] = "Gary", ["Sinnoh"] = "Kit" }, "Kit", RegionDatabase.Sinnoh);
            PlayerIdentity.InRegion(kanto);
            Assert.Equal("Gary", PlayerIdentity.RivalName);
        }
        finally
        {
            PlayerIdentity.Set(null, PlayerLook.Boy);
            PlayerIdentity.SetRival(null);
        }
    }

    [Fact]
    public void TheRivalsKeyboardTakesTheNameGivenOrTheRegionsOwn()
    {
        var screen = new RivalNamingScreen();
        screen.Open(RegionDatabase.Kanto, "Blue", null);
        Assert.True(screen.IsActive);
        Assert.Null(screen.Outcome);
        screen.ToDone();
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal("Blue", screen.Outcome);

        screen.Open(RegionDatabase.Kanto, "Blue", "Gary");
        screen.Cancel();
        screen.ToDone();
        screen.Confirm();
        Assert.Equal("Gar", screen.Outcome);
    }

    // The map of Kanto with its captain on Vermilion City's quay, a host that plays the attendant there
    private static (HeadlessScriptHost Host, ScriptRunner Runner, NPC Captain) AtVermilionsQuay()
    {
        var kanto = MapDatabase.Get("Kanto");
        var captain = kanto.Everyone.First(n => n.IsTransportAttendant);
        var host = new HeadlessScriptHost { Map = kanto, PlayerTile = (captain.GridX, captain.GridY + 1), MapNamed = MapDatabase.Get };
        foreach (var species in new[] { "Venusaur", "Pidgeot", "Raichu" }) host.Party.Add(Mon(species, 60));
        host.Party.Members[0].Region = RegionDatabase.Sinnoh;
        return (host, new ScriptRunner(ScriptLibrary.Default, host), captain);
    }

    [Fact]
    public void TheHeadlessHostCrossesWithTheFlagSetAndNotBefore()
    {
        // Before Kanto's Hall of Fame the captain says who may come aboard, and nobody sails
        var (before, runner, captain) = AtVermilionsQuay();
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, captain);
        runner.RunToEnd();
        Assert.Empty(before.Crossings);
        Assert.Equal("Kanto", before.Map!.Name);
        Assert.Contains(before.Transcript, l => l.Text.Contains("Hall of Fame"));

        // After it, the ship sails to Johto with the whole team, money and bag as they were
        var (host, sail, sailor) = AtVermilionsQuay();
        host.Story.CompleteRegion(RegionDatabase.Kanto);
        host.Bag.AddItem(ItemDatabase.Get("Potion")!, 3);
        var team = host.Party.Members.ToList();
        int money = host.Money;
        sail.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, sailor);
        sail.RunToEnd();

        Assert.Equal(new[] { RegionDatabase.Johto }, host.Crossings);
        var arrival = RegionDatabase.Get(RegionDatabase.Johto)!.ArrivalSpot!;
        Assert.Equal(arrival.Map, host.Map!.Name);
        Assert.Equal((arrival.X, arrival.Y), host.PlayerTile);
        Assert.Equal(RegionDatabase.Johto, host.Region);
        Assert.Equal(team, host.Party.Members);
        Assert.Empty(host.Boxes);
        Assert.Equal(money, host.Money);
        Assert.Equal(3, host.Bag.GetQuantity(ItemDatabase.Get("Potion")!));
        Assert.True(host.Story.IsRegionComplete(RegionDatabase.Kanto));
        // Each Pokémon kept the region it was met in, and those with none were given the one they left
        Assert.Equal(new[] { "Sinnoh", "Kanto", "Kanto" }, host.Party.Members.Select(p => p.Region));
        // and {rival} names Johto's
        Assert.Equal("Silver", host.RivalName);
    }

    [Fact]
    public void TheCrossingAsksFirstAndAChampionMayStay()
    {
        // Plan 18 · W5: once the vessel may leave, common.Crossing asks; a no keeps the player on the quay
        var (host, runner, captain) = AtVermilionsQuay();
        host.Story.CompleteRegion(RegionDatabase.Kanto);
        host.Answers.Enqueue(1);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, captain);
        runner.RunToEnd();
        Assert.Empty(host.Crossings);
        Assert.Equal("Kanto", host.Map!.Name);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("sound crossing_"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("when you're ready"));
    }

    [Fact]
    public void TheCrossingIsHeardAndToldOverTheDark()
    {
        // Plan 18 · W5: a yes, the field to black, the vessel's horn and its one line, the crossing, the dark lifted
        var (host, runner, captain) = AtVermilionsQuay();
        host.Story.CompleteRegion(RegionDatabase.Kanto);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, captain);
        runner.RunToEnd();
        var link = RegionDatabase.LinkFrom(RegionDatabase.Kanto)!;
        string voyage = RegionDatabase.VoyageLine(link);
        Assert.Contains("Johto", voyage);
        int said = host.Transcript.FindIndex(l => l.Text == voyage);
        Assert.True(said > 0, "the voyage's line was never said");
        Assert.Null(host.Transcript[said].Speaker);
        // The question came before it, and the horn with it
        Assert.Contains(host.Transcript.Take(said), l => l.Text.Contains("aboard"));
        int horn = host.Log.IndexOf("sound crossing_horn");
        int travelled = host.Log.IndexOf($"travel {RegionDatabase.Johto}");
        Assert.InRange(horn, 0, travelled);
        Assert.Equal(new[] { RegionDatabase.Johto }, host.Crossings);
        // The game crossed into Johto began elsewhere: Johto's opening may ask its rival's name
        Assert.Equal(RegionDatabase.Kanto, host.BeganIn);
    }

    [Fact]
    public void ARegionCrossedIntoKnowsIt()
    {
        // Plan 18 · W5: CROSSED_IN is 1 where the script plays in a region the game didn't begin in
        var library = ScriptLibrary.FromSources(("common", "script Ask\n  if var CROSSED_IN == 1 say \"crossed\"\n  end"));
        var crossed = new HeadlessScriptHost { Region = RegionDatabase.Kanto, BeganInRegion = RegionDatabase.Sinnoh };
        var runner = new ScriptRunner(library, crossed);
        runner.Start(library.Find("common.Ask")!);
        runner.RunToEnd();
        Assert.Contains(crossed.Transcript, l => l.Text == "crossed");

        var home = new HeadlessScriptHost { Region = RegionDatabase.Kanto };
        var again = new ScriptRunner(library, home);
        again.Start(library.Find("common.Ask")!);
        again.RunToEnd();
        Assert.Empty(home.Transcript);
    }

    [Fact]
    public void OaksLabAsksTheRivalsNameOfAChampionWhoCrossedIn()
    {
        // Plan 18 · W5: Kanto's opening names Kanto's rival with namerival for a game begun elsewhere, and only then
        var lab = MapDatabase.Get("PalletTown_ProfessorOaksLab");
        foreach (bool crossed in new[] { true, false })
        {
            var host = new HeadlessScriptHost
            {
                Map = lab, Region = RegionDatabase.Kanto, RivalNameGiven = "Gary",
                BeganInRegion = crossed ? RegionDatabase.Sinnoh : null
            };
            host.Story.SetVar("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB", 1);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("OnEnter", "pallet_town_professor_oaks_lab")!);
            runner.RunToEnd();
            Assert.Equal(crossed, host.Log.Any(l => l.StartsWith("open NameRival")));
            if (crossed) Assert.Equal("Gary", host.RivalNames[RegionDatabase.Kanto]);
        }
    }

    [Fact]
    public void TheHeadlessHostNamesARegionsRival()
    {
        var host = new HeadlessScriptHost { Region = RegionDatabase.Johto, RivalNameGiven = "Kamon" };
        var library = ScriptLibrary.FromSources(("common", "script Name\n  namerival\n  if result == 1 say \"{rival}!\"\n  end"));
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("common.Name")!);
        runner.RunToEnd();
        Assert.Equal("Kamon", host.RivalName);
        Assert.Equal("Kamon", host.RivalNames[RegionDatabase.Johto]);
        Assert.Contains("open NameRival Johto Kamon", host.Log);
        Assert.Contains(host.Transcript, l => l.Text == "Kamon!");

        var plain = new HeadlessScriptHost { Region = RegionDatabase.Kanto };
        var again = new ScriptRunner(library, plain);
        again.Start(library.Find("common.Name")!);
        again.RunToEnd();
        Assert.Equal("Blue", plain.RivalName);
    }

    [Fact]
    public void AChampionOfSinnohCrossesToKantoWithTheTeamAndSavesItsRegions()
    {
        string folder = Path.Combine(Path.GetTempPath(), "crossing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(18);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = RegionDatabase.Sinnoh;
            engine.StartNewGame();
            game.Party.Clear();
            game.Party.Add(Mon("Infernape", 62));
            game.Party.Add(Mon("Garchomp", 64));
            game.State = GameState.Overworld;
            game.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
            int money = game.Money;

            game.TravelTo(RegionDatabase.Kanto);
            for (int f = 0; f < 60 * 10 && !(game.State == GameState.Overworld && game.Map.Name == "Kanto"); f++)
            {
                InputManager.Feed(ActionFrame.None);
                engine.Update(Frame);
            }
            Assert.Equal("Kanto", engine.Here.Id);
            Assert.Equal(2, game.Party.Count);
            Assert.All(game.Party.Members, p => Assert.Equal(RegionDatabase.Sinnoh, p.Region));
            Assert.Equal(money, game.Money);
            Assert.Equal("Blue", PlayerIdentity.RivalName);

            // The save keeps the rival the introduction named as its RivalName, and no others while there are none
            var save = game.BuildSave();
            Assert.Equal("Barry", save.RivalName);
            Assert.Null(save.RivalNames);
            Assert.Equal(RegionDatabase.Sinnoh, save.Party[0].Region);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.SetRival(null);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }
}
