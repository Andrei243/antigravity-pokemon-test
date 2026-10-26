using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The World Tournament's events and the other regions (plan 14 · B12; rulings, "The World Tournament"): every region's
/// section offered once its trainers' table exists and skipped when it doesn't, an event's bracket seating only
/// entrants of its kind, a type bracket played through, the formats, the mixed and the rental events, and the desk's
/// type clause and rental way played by <see cref="HeadlessScriptHost"/>.
/// </summary>
[Collection("MapDatabase")]
public class WorldTournamentEventsTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Func<int> Draws(int seed)
    {
        var random = new Random(seed);
        return () => random.Next(0x10000);
    }

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, new Random(level));

    private static Party PartyOf(params string[] species)
    {
        var party = new Party();
        foreach (var s in species) party.Add(Mon(s, 60));
        return party;
    }

    private static TournamentEventRecord Event(string id) => TournamentDatabase.Get(id)!;

    // ------------------------------------------------------------------ the file

    [Fact]
    public void EveryRegionsSectionIsOfferedNowThatItsTableExists()
    {
        var offered = TournamentDatabase.Events.Select(e => e.Id).ToList();
        foreach (string region in new[] { "sinnoh", "kanto", "johto", "hoenn", "unova" })
        {
            Assert.Contains(region + "Leaders", offered);
            Assert.Contains(region + "Champion", offered);
        }
        foreach (string id in new[] { "worldLeaders", "worldChampions", "worldBeyond", "worldDoubles", "worldTriples", "worldRotation", "worldMix", "worldRental", "typeWater", "typeFireElectric" })
            Assert.Contains(id, offered);
        // Sinnoh's two come first, so the desk's first answers are what they were
        Assert.Equal(new[] { "sinnohLeaders", "sinnohChampion" }, offered.Take(2));
        // Every event's name and note are on the text table, and what opens it is an event of the file
        foreach (var ev in TournamentDatabase.All)
        {
            Assert.DoesNotContain("[tournamentScreen", PokemonPlatinumEngine.Core.Text.Of($"tournamentScreen.event.{ev.Id}"));
            Assert.DoesNotContain("[tournamentScreen", PokemonPlatinumEngine.Core.Text.Of($"tournamentScreen.note.{ev.Id}"));
            if (ev.After != null) Assert.NotNull(TournamentDatabase.Get(ev.After));
            Tournament.FormatOf(ev);
            Tournament.TypesOf(ev);
        }
    }

    [Fact]
    public void ARegionsSectionIsReadWhenItsDataExistsAndSkippedWhenItDoesnt()
    {
        var data = GameDataFiles.Load<TournamentData>(TournamentDatabase.FileName);
        bool Exists(string id) => TrainerDatabase.Get(id) != null;

        // Everything there is: every section read
        var all = Tournament.Offered(data, Exists, TournamentDatabase.HasData).Select(e => e.Id).ToList();
        Assert.Contains("unovaLeaders", all);

        // Unova's table not landed yet: its section is passed over, and the world's events go on without its trainers
        var noUnova = Tournament.Offered(data, id => !id.StartsWith("unova_") && Exists(id), r => r != "Unova").Select(e => e.Id).ToList();
        Assert.DoesNotContain("unovaLeaders", noUnova);
        Assert.DoesNotContain("unovaChampion", noUnova);
        Assert.Contains("worldLeaders", noUnova);
        Assert.Contains("sinnohLeaders", noUnova);

        // A section whose region is there but whose trainers aren't seats nobody, so its events are skipped too
        var noHoennTrainers = Tournament.Offered(data, id => !id.StartsWith("hoenn_") && Exists(id), _ => true).Select(e => e.Id).ToList();
        Assert.DoesNotContain("hoennLeaders", noHoennTrainers);
        Assert.Contains("kantoLeaders", noHoennTrainers);

        // A made-up region with no table of its own
        data.Regions.Add(new TournamentRegionRecord
        {
            Region = "Kalos",
            Events = { new TournamentEventRecord { Id = "kalosLeaders", Kind = "leaders", Pool = { new TournamentEntrantRecord { Trainer = "kalos_leader_viola", Look = "viola" } } } }
        });
        Assert.DoesNotContain(Tournament.Offered(data, Exists, TournamentDatabase.HasData), e => e.Id == "kalosLeaders");
        Assert.False(TournamentDatabase.HasData("Kalos"));
    }

    // ------------------------------------------------------------------ seating

    [Fact]
    public void AnEventsBracketSeatsOnlyEntrantsOfItsKind()
    {
        foreach (var ev in TournamentDatabase.Events)
            for (int seed = 1; seed <= 3; seed++)
            {
                var party = PartyOf("Garchomp", "Lucario", "Staraptor");
                var t = Tournament.Begin(new FrontierRecords(), ev, party, new[] { 0, 1, 2 }, Draws(seed * 13 + ev.Id.Length));
                var seated = t.Field.Skip(1).ToList();
                Assert.Equal(Tournament.Entrants - 1, seated.Select(e => e.Record!.Id).Distinct().Count());
                var (always, pool) = TournamentDatabase.EntrantsOf(ev);
                var named = always.Concat(pool).Select(e => e.Trainer).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var types = Tournament.TypesOf(ev);
                foreach (var who in seated)
                {
                    Assert.Contains(who.Record!.Id, named);
                    Assert.True(Tournament.OfKind(ev, who.Record!), $"{ev.Id} seated {who.Record!.Id}, a {who.Record!.Class}");
                    Assert.Equal(Tournament.TeamSize, who.Three.Count);
                    Assert.All(who.Three, p => Assert.Equal(Tournament.Level, p.Level));
                    if (types.Count > 0)
                        Assert.All(who.Three, p => Assert.True(types.Any(p.HasType), $"{ev.Id}: {who.Record!.Id}'s {p.Species.Name} is none of its types"));
                    if (ev.Kind == "leaders") Assert.StartsWith("Leader", who.Record!.Class);
                }
                // The always-in are seated whenever they qualify
                foreach (var a in always.Where(a => Tournament.Qualifies(ev, TrainerDatabase.Get(a.Trainer)!)).Take(Tournament.Entrants - 1))
                    Assert.Contains(seated, s => s.Record!.Id == a.Trainer);
            }
    }

    [Fact]
    public void ARegionsLeadersEventSeatsItsOwnLeaders()
    {
        var hoenn = Tournament.Begin(new FrontierRecords(), Event("hoennLeaders"), PartyOf("Garchomp", "Lucario", "Staraptor"), new[] { 0, 1, 2 }, Draws(4));
        Assert.All(hoenn.Field.Skip(1), e => Assert.StartsWith("hoenn_", e.Record!.Id));
        var unova = Tournament.Begin(new FrontierRecords(), Event("unovaChampion"), PartyOf("Garchomp", "Lucario", "Staraptor"), new[] { 0, 1, 2 }, Draws(5));
        Assert.Contains(unova.Field, e => e.Record?.Id == "unova_champion_iris_4");
        // Teams that held two of an item give three that hold it once
        Assert.All(unova.Field.Skip(1), e => Assert.Equal(FrontierRefusal.None, FrontierRules.WhyNot(e.Three)));
    }

    // ------------------------------------------------------------------ the special events

    [Fact]
    public void ATypeBracketIsPlayedThrough()
    {
        var ev = Event("typeWater");
        var records = new FrontierRecords();
        records.Tournament.Titles["sinnohLeaders"] = records.Tournament.Titles["worldLeaders"] = 1;
        Assert.True(Tournament.IsOpen(ev, records.Tournament));
        var party = PartyOf("Empoleon", "Gyarados", "Milotic");
        Assert.All(party.Members, p => Assert.True(Tournament.FitsTheEvent(ev, p)));
        Assert.False(Tournament.FitsTheEvent(ev, Mon("Garchomp", 50)));
        var t = Tournament.Begin(records, ev, party, new[] { 0, 1, 2 }, Draws(21));
        bool won = t.PlayHeadless(records, Draws(22));
        Assert.True(t.Bracket.IsOver);
        Assert.Equal(won ? 1 : 0, records.Tournament.TitlesOf("typeWater"));
        Assert.All(t.Field.Skip(1).SelectMany(e => e.Three), p => Assert.True(p.HasType(PokemonType.Water)));
    }

    [Fact]
    public void TheFormatEventsAreFoughtInTheirFormat()
    {
        Assert.Equal(BattleFormat.Single, Tournament.FormatOf(Event("worldLeaders")));
        Assert.Equal(BattleFormat.Double, Tournament.FormatOf(Event("worldDoubles")));
        Assert.Equal(BattleFormat.Triple, Tournament.FormatOf(Event("worldTriples")));
        Assert.Equal(BattleFormat.Rotation, Tournament.FormatOf(Event("worldRotation")));
        var records = new FrontierRecords();
        var t = Tournament.Begin(records, Event("worldDoubles"), PartyOf("Garchomp", "Lucario", "Staraptor"), new[] { 0, 1, 2 }, Draws(31));
        Assert.Equal(BattleFormat.Double, t.Format);
        t.PlayHeadless(records, Draws(32));
        Assert.True(t.Bracket.IsOver);
    }

    [Fact]
    public void TheMixedEventHandsOneOverEachWayAndTouchesNoParty()
    {
        var party = PartyOf("Garchomp", "Lucario", "Staraptor");
        string before = string.Join(",", party.Members.Select(p => $"{p.Species.Name}{p.Level}{p.CurrentHP}"));
        var t = Tournament.Begin(new FrontierRecords(), Event("worldMix"), party, new[] { 0, 1, 2 }, Draws(41));
        int opponent = t.Bracket.OpponentOf(0, Tournament.Player);
        var (mine, theirs) = t.ThreesFor(Tournament.Player, opponent, 0);
        var ownSpecies = t.Field[Tournament.Player].Three.Select(p => p.Species.Name).ToList();
        var theirSpecies = t.Field[opponent].Three.Select(p => p.Species.Name).ToList();
        Assert.Equal(2, mine.Count(p => ownSpecies.Contains(p.Species.Name)));
        Assert.Single(mine, p => theirSpecies.Contains(p.Species.Name) && !ownSpecies.Contains(p.Species.Name));
        Assert.Equal(2, theirs.Count(p => theirSpecies.Contains(p.Species.Name)));
        // The two sides of a match agree, whoever asks
        var (theirsAgain, mineAgain) = t.ThreesFor(opponent, Tournament.Player, 0);
        Assert.Equal(mine.Select(p => p.Species.Name), mineAgain.Select(p => p.Species.Name));
        Assert.Equal(theirs.Select(p => p.Species.Name), theirsAgain.Select(p => p.Species.Name));
        Assert.Equal(mine.Select(p => p.Species.Name), t.TeamForBattle().Members.Select(p => p.Species.Name));
        Assert.Equal(theirs.Select(p => p.Species.Name), t.OpponentAsTrainer().Party.Members.Select(p => p.Species.Name));
        t.PlayHeadless(new FrontierRecords(), Draws(42));
        Assert.Equal(before, string.Join(",", party.Members.Select(p => $"{p.Species.Name}{p.Level}{p.CurrentHP}")));
    }

    [Fact]
    public void TheRentalEventLendsEveryoneThreeAtFifty()
    {
        var t = Tournament.Begin(new FrontierRecords(), Event("worldRental"), new Party(), Array.Empty<int>(), Draws(51));
        Assert.All(t.Field, e =>
        {
            Assert.Equal(Tournament.TeamSize, e.Three.Count);
            Assert.All(e.Three, p => Assert.Equal(Tournament.Level, p.Level));
            Assert.Equal(FrontierRefusal.None, FrontierRules.WhyNot(e.Three));
        });
        // A seed lends the same three again
        var again = Tournament.Begin(new FrontierRecords(), Event("worldRental"), new Party(), Array.Empty<int>(), Draws(51));
        Assert.Equal(t.Field[0].Three.Select(p => p.Species.Name), again.Field[0].Three.Select(p => p.Species.Name));
        Assert.True(t.PlayHeadless(new FrontierRecords(), Draws(52)) || t.Bracket.IsOver);
    }

    // ------------------------------------------------------------------ the desk

    private static int Answer(string id) => TournamentDatabase.Events.ToList().FindIndex(e => e.Id == id) + 1;

    [Fact]
    public void TheDeskRefusesThreeOfTheWrongTypeForATypeBracket()
    {
        var host = new HeadlessScriptHost { TournamentChoice = Answer("typeWater") };
        foreach (var p in PartyOf("Garchomp", "Lucario", "Staraptor").Members) host.Party.Add(p);
        host.Frontier.Tournament.Titles["sinnohLeaders"] = host.Frontier.Tournament.Titles["worldLeaders"] = 1;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleFrontierAnnex");
        runner.Start(ScriptLibrary.Default.Find("TournamentDesk", "BattleFrontierAnnex")!, host.Map.Everyone.First(n => n.Key == "tournament_receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Null(host.Tournament);
        Assert.Contains(host.Transcript, l => l.Text.Contains("event's types"));
    }

    [Fact]
    public void TheDesksRentalEventChoosesNobodyOfTheTeam()
    {
        var host = new HeadlessScriptHost { TournamentChoice = Answer("worldRental") };
        foreach (var p in PartyOf("Garchomp", "Lucario", "Staraptor").Members) host.Party.Add(p);
        host.Frontier.Tournament.Titles["sinnohLeaders"] = host.Frontier.Tournament.Titles["worldLeaders"] = 1;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleFrontierAnnex");
        runner.Start(ScriptLibrary.Default.Find("TournamentDesk", "BattleFrontierAnnex")!, host.Map.Everyone.First(n => n.Key == "tournament_receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal("worldRental", host.Tournament?.Event.Id);
        Assert.Empty(host.Tournament!.Slots);
        var party = host.Party.Members.Select(p => p.Species.Name).ToList();
        Assert.Equal(3, host.Tournament.Field[Tournament.Player].Three.Count);
        Assert.Contains(host.Transcript, l => l.Text.Contains("rental event"));
        Assert.Equal(new[] { "Garchomp", "Lucario", "Staraptor" }, party);
    }

    [Fact]
    public void TheEventsScreenScrollsThroughEveryEvent()
    {
        var screen = new TournamentScreen();
        var record = new TournamentRecord();
        screen.OpenEvents(TournamentDatabase.Events, record);
        Assert.Equal(0, screen.First);
        for (int i = 0; i < TournamentDatabase.Events.Count - 1; i++) screen.Move(0, 1);
        Assert.Equal(TournamentDatabase.Events.Count - 1, screen.Cursor);
        Assert.Equal(TournamentDatabase.Events.Count - TournamentScreen.VisibleRows, screen.First);
        Assert.InRange(screen.Cursor - screen.First, 0, TournamentScreen.VisibleRows - 1);
        screen.Move(0, 1);
        Assert.Equal((0, 0), (screen.Cursor, screen.First));
    }
}
