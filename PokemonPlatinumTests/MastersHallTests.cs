using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The Masters' Hall and the titles (plan 14 · B15; rulings, "Master Trainers"): the hall off the annex's lobby, its
/// people shown only after the Hall of Fame, the receptionist's script played by <see cref="HeadlessScriptHost"/> through
/// each outcome, the titles counted from the masters beaten, and the Trainer Card's page of them.
/// </summary>
[Collection("MapDatabase")]
public class MastersHallTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, new Random(level));

    private static (ScriptRunner Runner, HeadlessScriptHost Host) Hall(params Pokemon[] team)
    {
        var host = new HeadlessScriptHost();
        foreach (var p in team) host.Party.Add(p);
        host.Story.Set("SinnohHallOfFame");
        host.Map = Room("MastersHall");
        host.Map.ApplyPresence(host.Story.Has);
        host.PlayerTile = (4, 10);
        return (new ScriptRunner(ScriptLibrary.Default, host), host);
    }

    private static void Ask(ScriptRunner runner, HeadlessScriptHost host)
    {
        runner.Start(ScriptLibrary.Default.Find("Receptionist", "MastersHall")!, host.Map!.Everyone.First(n => n.Key == "masters_receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
    }

    [Fact]
    public void NobodyIsInTheHallBeforeTheHallOfFame()
    {
        var hall = Room("MastersHall");
        hall.ApplyPresence(_ => false);
        Assert.Empty(hall.NPCs);
        hall.ApplyPresence(flag => flag == "SinnohHallOfFame");
        Assert.Equal(new[] { "masters_receptionist", "master" }, hall.NPCs.Select(n => n.Key).OrderByDescending(k => k.Length));
        // The master carries no team on the map, so loading the room draws nothing
        Assert.All(hall.Everyone, n => Assert.Null(n.TrainerData));
    }

    [Fact]
    public void TheHallIsComeToByTheLobbysWestDoorAndLeftByItsOwn()
    {
        var lobby = Room("BattleFrontierAnnex");
        var into = Assert.Single(lobby.Warps, w => w.TargetMap == "MastersHall");
        Assert.Equal((0, 6), (into.SourceX, into.SourceY));
        var hall = Room("MastersHall");
        Assert.True(hall.IsWalkable(into.TargetX, into.TargetY));
        var back = Assert.Single(hall.Warps);
        Assert.Equal("BattleFrontierAnnex", back.TargetMap);
        Assert.True(lobby.IsWalkable(back.TargetX, back.TargetY));
        // The mark the player is put on and the master's way down to it are open floor
        foreach (var (x, y) in new[] { (5, 4), (5, 5), (5, 6), (5, 7), (5, 8) }) Assert.False(hall.IsSolid(x, y));
    }

    [Fact]
    public void BackingOutFightsNobody()
    {
        var (runner, host) = Hall(Mon("Bidoof", 30));
        host.PokemonChoice = 255;
        Ask(runner, host);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle ", StringComparison.Ordinal));
        Assert.Equal(0, MasterTitles.Count(host.Story));
        Assert.Equal("Please come again. The masters are always here.", host.Transcript.Last().Text);
    }

    [Fact]
    public void ATitleWonIsHeldAndIsTheLatest()
    {
        var (runner, host) = Hall(Mon("Starly", 30), Mon("Bidoof", 30));
        host.PokemonChoice = 1;
        Ask(runner, host);
        Assert.Contains("battle master_bidoof alone Bidoof noitems Won", host.Log);
        Assert.True(MasterTitles.Holds(host.Story, "Bidoof"));
        Assert.Equal(1, MasterTitles.Count(host.Story));
        Assert.Equal("Bidoof", MasterTitles.Latest(host.Story));
        Assert.Contains(host.Transcript, t => t.Text.Contains("holds the title of Bidoof Master! Titles held: 1."));
        // The master stepped up to the mark and back to his place
        var master = host.Map!.Everyone.First(n => n.Key == "master");
        Assert.Equal((5, 4), (master.GridX, master.GridY));
    }

    [Fact]
    public void AMasterLostToGivesNoTitle()
    {
        var bidoof = Mon("Bidoof", 30);
        var (runner, host) = Hall(bidoof);
        host.PokemonChoice = 0;
        host.Fight = _ => BattleOutcome.Lost;
        Ask(runner, host);
        Assert.Contains("battle master_bidoof alone Bidoof noitems Lost", host.Log);
        Assert.Equal(0, MasterTitles.Count(host.Story));
        Assert.Null(MasterTitles.Latest(host.Story));
        Assert.Contains(host.Transcript, t => t.Text.StartsWith("Not this time.", StringComparison.Ordinal));
    }

    [Fact]
    public void AMasterBeatenAgainGivesNothingNew()
    {
        var (runner, host) = Hall(Mon("Bidoof", 30), Mon("Starly", 30));
        host.PokemonChoice = 1;
        Ask(runner, host);
        host.PokemonChoice = 0;
        Ask(runner, host);
        Assert.Equal("Bidoof", MasterTitles.Latest(host.Story));
        int held = MasterTitles.Count(host.Story);
        Assert.Equal(2, held);

        // The Starly again: the hall says the title is held, the battle is fought, and the count stays where it was
        host.PokemonChoice = 1;
        Ask(runner, host);
        Assert.Contains(host.Transcript, t => t.Text.StartsWith("You already hold the title of Starly Master.", StringComparison.Ordinal));
        Assert.Equal(held, MasterTitles.Count(host.Story));
        Assert.Equal("Starly", MasterTitles.Latest(host.Story));
        Assert.Equal(new (string, int)[] { ("Starly", 396), ("Bidoof", 399) }, MasterTitles.Won(host.Story).ToArray());
    }

    [Fact]
    public void AnEggIsNoChoice()
    {
        var egg = Mon("Bidoof", 1);
        egg.IsEgg = true;
        var (runner, host) = Hall(Mon("Starly", 30), egg);
        host.PokemonChoice = 1;
        Ask(runner, host);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle ", StringComparison.Ordinal));
        Assert.Equal(0, MasterTitles.Count(host.Story));
    }

    [Fact]
    public void TitledIsReadAndThePlaceholderNamesTheMaster()
    {
        var code = ScriptParser.Parse("test", "script S\n  if titled goto T\n  if not titled goto T\nlabel T\n  end\n")[0].Everything().ToList();
        Assert.Equal(new[] { Query.Titled, Query.Titled }, code.Where(i => i.Condition != null).Select(i => i.Condition!.Query));
        Assert.Equal(TrainerDatabase.Masters.Count, MasterTitles.Total);
        Assert.True(MasterTitles.IsMaster("master_bidoof"));
        Assert.False(MasterTitles.IsMaster("youngster_tristan"));
    }

    // ------------------------------------------------------------------ the card

    [Fact]
    public void TheTitlesPageHoldsAsManyRowsAsItsPanel()
    {
        Assert.Equal(TrainerCardScreen.TitleRows, ModernUi.CardTitleRows);
    }

    [Fact]
    public void TheCardHasATitlesPageOnlyOnceOneIsWonAndItScrolls()
    {
        var card = new TrainerCardScreen();
        card.Open(regionPages: 0, hasRun: false, titles: 0);
        Assert.False(card.HasTitlesPage);
        Assert.False(card.GoTo(CardPage.Titles));
        Assert.False(card.Turn(1));

        card.Open(regionPages: 2, hasRun: true, titles: 40);
        Assert.True(card.HasTitlesPage);
        // Front, back, the run's, the titles', then the regions'
        for (int i = 0; i < 5; i++) Assert.True(card.Turn(1));
        Assert.False(card.Turn(1));
        for (int i = 0; i < 5; i++) card.Turn(-1);
        Assert.Equal(CardPage.Front, card.Page);
        card.Turn(1);
        Assert.Equal(CardPage.Back, card.Page);
        card.Turn(1);
        Assert.Equal(CardPage.Run, card.Page);
        card.Turn(1);
        Assert.Equal(CardPage.Titles, card.Page);
        card.Turn(1);
        Assert.Equal((CardPage.Region, 0), (card.Page, card.RegionShown));

        // Forty titles are fourteen rows of three: the page of nine scrolls down five rows and no further
        Assert.True(card.GoTo(CardPage.Titles));
        Assert.Equal(5, card.LastTitleRow);
        Assert.False(card.ScrollTitles(-1));
        for (int i = 0; i < 5; i++) Assert.True(card.ScrollTitles(1));
        Assert.False(card.ScrollTitles(1));
        Assert.Equal(5, card.TitleRow);

        // A handful fits the page whole and doesn't scroll
        card.Open(titles: 4);
        Assert.True(card.GoTo(CardPage.Titles));
        Assert.Equal(0, card.LastTitleRow);
        Assert.False(card.ScrollTitles(1));
    }
}
