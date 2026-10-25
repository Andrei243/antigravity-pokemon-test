using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Lilycove's Contest Lobby and Hall (plan 21 · H11), played through with no screen: the receptionist's questions and
/// what a win leaves (the ribbon, the Luxury Ball, the painter and her museum), who may enter a rank, the winners' wall,
/// the Berry Blender's three drums, the rooms themselves and the data the contest asks.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennContestTests
{
    private const string Lobby = "lilycove_city_contest_lobby";

    private static Pokemon Mon(string species = "Bidoof") => new(PokemonDatabase.Get(species)!, 20, Gender.Male, Nature.Hardy, false);

    private static HeadlessScriptHost Run(string name, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, Lobby)!, new NPC());
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static HeadlessScriptHost WithAnswers(HeadlessScriptHost host, params int[] answers)
    {
        foreach (int a in answers) host.Answers.Enqueue(a);
        return host;
    }

    // ---------------------------------------------------------------- the receptionist

    [Fact]
    public void TheReceptionistGivesTheCaseOnceAndEntersAPokemonInTheNormalRank()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        // ENTER, the Normal Rank, the Cool Contest; the first of the team
        WithAnswers(host, 0, 0, 0);
        Run("ContestReceptionist", host);

        Assert.True(host.Story.Has("FLAG_RECEIVED_POKEBLOCK_CASE"));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Pokéblock Case")!));
        Assert.Contains("appeals Normal Cool Bidoof 0", host.Log);
        Assert.Contains("RIBBON_HOENN_COOL", host.Party.Members[0].Ribbons);
        Assert.Contains(host.Transcript, l => l.Text.Contains("has been entered"));
        Assert.Equal(1, host.HoennContests.TotalWon);
        Assert.Single(host.HoennContests.Hall);

        // The second time there is no case to give, and a Pokémon that has won returns to the same rank with a question
        var again = new HeadlessScriptHost(host.Story, host.Party, host.Bag);
        WithAnswers(again, 0, 0, 0, 0);
        Run("ContestReceptionist", again);
        Assert.Equal(1, again.Bag.GetQuantity(ItemDatabase.Get("Pokéblock Case")!));
        Assert.Contains(again.Transcript, l => l.Text.Contains("already won this rank"));
    }

    [Fact]
    public void ARankIsOpenOnlyToAPokemonWithTheRibbonOfTheOneBelow()
    {
        var p = Mon();
        Assert.Equal(AppealEntry.Equal, HoennContestRules.Check(p, ContestStat.Smart, AppealRank.Normal));
        Assert.Equal(AppealEntry.TooLow, HoennContestRules.Check(p, ContestStat.Smart, AppealRank.Super));
        p.Ribbons.Add(HoennContestRules.RibbonOf(ContestStat.Smart, AppealRank.Normal));
        Assert.Equal(AppealEntry.Equal, HoennContestRules.Check(p, ContestStat.Smart, AppealRank.Super));
        Assert.Equal(AppealEntry.Above, HoennContestRules.Check(p, ContestStat.Smart, AppealRank.Normal));
        Assert.Equal(AppealEntry.TooLow, HoennContestRules.Check(p, ContestStat.Cute, AppealRank.Super));
        p.CurrentHP = 0;
        Assert.Equal(AppealEntry.Fainted, HoennContestRules.Check(p, ContestStat.Smart, AppealRank.Normal));
        p.IsEgg = true;
        Assert.Equal(AppealEntry.Egg, HoennContestRules.Check(p, ContestStat.Smart, AppealRank.Normal));
    }

    [Fact]
    public void ThePokemonTooLowForTheRankIsTurnedAwayAndNothingIsHeld()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        host.Story.Set("FLAG_RECEIVED_POKEBLOCK_CASE");
        // ENTER, the Master Rank, the Cool Contest, the first Pokémon: turned away; yes, cancel the entry
        WithAnswers(host, 0, 3, 0, 0);
        Run("ContestReceptionist", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("hasn't won the rank under"));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("appeals "));
        Assert.Empty(host.Party.Members[0].Ribbons);
    }

    [Fact]
    public void AMasterRankWinOfEightHundredPointsBringsThePainterAndHerMuseum()
    {
        var host = new HeadlessScriptHost { AppealPoints = 820, Map = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().Single(m => m.Name == "LilycoveCity_ContestLobby") };
        var p = Mon();
        foreach (var rank in new[] { AppealRank.Normal, AppealRank.Super, AppealRank.Hyper, AppealRank.Master })
            p.Ribbons.Add(HoennContestRules.RibbonOf(ContestStat.Cool, rank));
        host.Party.Add(p);
        host.Story.Set("FLAG_RECEIVED_POKEBLOCK_CASE");
        // ENTER, the Master Rank, Cool; "enter anyway?" yes; later "take the painting to the museum?" yes
        WithAnswers(host, 0, 3, 0, 0, 0);
        Run("ContestReceptionist", host);

        Assert.Contains("appeals Master Cool Bidoof 0", host.Log);
        Assert.True(host.Story.Has("FLAG_HOENN_CONTEST_MASTER_COOL"));
        // A rank won again at the Master Rank: a Luxury Ball
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Luxury Ball")!));
        // The painting hangs in the museum, and the Artist Ribbon goes with it
        Assert.True(host.Story.Has("FLAG_COOL_PAINTING_MADE"));
        Assert.NotNull(host.HoennContests.Museum[(int)ContestStat.Cool]);
        Assert.Contains(HoennContestRules.ArtistRibbon, p.Ribbons);

        // Under 800 points there is no painter
        var humble = new HeadlessScriptHost { AppealPoints = 799 };
        var q = Mon();
        foreach (var rank in new[] { AppealRank.Normal, AppealRank.Super, AppealRank.Hyper, AppealRank.Master })
            q.Ribbons.Add(HoennContestRules.RibbonOf(ContestStat.Cute, rank));
        humble.Party.Add(q);
        humble.Story.Set("FLAG_RECEIVED_POKEBLOCK_CASE");
        WithAnswers(humble, 0, 3, 2, 0);
        Run("ContestReceptionist", humble);
        Assert.False(humble.Story.Has("FLAG_CUTE_PAINTING_MADE"));
        Assert.Equal(1, humble.Bag.GetQuantity(ItemDatabase.Get("Luxury Ball")!));
    }

    [Fact]
    public void ALostContestLeavesNoRibbonAndNoPrize()
    {
        var host = new HeadlessScriptHost { AppealPlacement = 2 };
        host.Party.Add(Mon());
        host.Story.Set("FLAG_RECEIVED_POKEBLOCK_CASE");
        WithAnswers(host, 0, 0, 3);
        Run("ContestReceptionist", host);
        Assert.Empty(host.Party.Members[0].Ribbons);
        Assert.Equal(0, host.HoennContests.TotalWon);
        Assert.Equal(1, host.HoennContests.TotalEntered);
        Assert.Empty(host.HoennContests.Hall);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Thank you for taking part"));
    }

    [Fact]
    public void TheInformationEndsTheTalkAndExitLeavesWithAWord()
    {
        var info = new HeadlessScriptHost();
        info.Story.Set("FLAG_RECEIVED_POKEBLOCK_CASE");
        WithAnswers(info, 1, 2);
        Run("ContestReceptionist", info);
        Assert.Contains(info.Transcript, l => l.Text.Contains("four ranks"));

        var exit = new HeadlessScriptHost();
        exit.Story.Set("FLAG_RECEIVED_POKEBLOCK_CASE");
        WithAnswers(exit, 2);
        Run("ContestReceptionist", exit);
        Assert.Contains(exit.Transcript, l => l.Text.Contains("on our stage"));
    }

    // ---------------------------------------------------------------- the wall

    [Fact]
    public void TheWinnersWallKeepsTheLastSixNewestFirst()
    {
        var host = new HeadlessScriptHost();
        for (int i = 0; i < 7; i++)
        {
            var p = Mon();
            p.Nickname = $"Mon{i}";
            var outcome = HoennContestRules.Conclude(ContestStat.Cool, AppealRank.Normal, p, 0, 300);
            HoennContestRules.Record(outcome, p, $"Trainer{i}", host.HoennContests);
        }
        Assert.Equal(HoennContestRecords.HallPaintings, host.HoennContests.Hall.Count);
        Run("Winner1", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Mon6") && l.Text.Contains("Trainer6"));
        Run("Winner6", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Mon1"));

        var empty = new HeadlessScriptHost();
        Run("Winner3", empty);
        Assert.Contains(empty.Transcript, l => l.Text.Contains("An empty frame"));
    }

    // ---------------------------------------------------------------- the Berry Blender

    [Fact]
    public void ABlenderDrumNeedsTheCaseAndAnEmptySlotInIt()
    {
        var noCase = new HeadlessScriptHost();
        WithAnswers(noCase, 0);
        Run("BlenderOne", noCase);
        Assert.Contains(noCase.Transcript, l => l.Text.Contains("need a Pokéblock Case"));
        Assert.DoesNotContain(noCase.Log, l => l.StartsWith("pokeblock"));

        var host = new HeadlessScriptHost();
        host.Bag.AddItem(ItemDatabase.Get("Pokéblock Case")!);
        WithAnswers(host, 0);
        Run("BlenderThree", host);
        Assert.Contains("pokeblock blend 3", host.Log);

        var full = new HeadlessScriptHost();
        full.Bag.AddItem(ItemDatabase.Get("Pokéblock Case")!);
        var block = new Pokeblock(PokeblockColor.Red, 9, 0, 0, 0, 0, 24);
        for (int i = 0; i < PokeblockCase.Capacity; i++) full.PokeblockCase.Add(block);
        WithAnswers(full, 0);
        Run("BlenderTwo", full);
        Assert.Contains(full.Transcript, l => l.Text.Contains("Case is full"));
        Assert.DoesNotContain(full.Log, l => l.StartsWith("pokeblock"));

        var declined = new HeadlessScriptHost();
        WithAnswers(declined, 1);
        Run("BlenderOne", declined);
        Assert.Contains(declined.Transcript, l => l.Text.Contains("all right"));
    }

    // ---------------------------------------------------------------- the rooms and the data

    [Fact]
    public void TheLobbyAndTheHallAreRoomsOfHoennWithAWayBetweenThem()
    {
        var hoenn = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn);
        var maps = hoenn.BuildMaps().ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        var lobby = maps["LilycoveCity_ContestLobby"];
        var hall = maps["LilycoveCity_ContestHall"];
        Assert.Contains(lobby.Warps, w => w.TargetMap.Equals("LilycoveCity_ContestHall", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(hall.Warps, w => w.TargetMap.Equals("LilycoveCity_ContestLobby", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(maps.Values.Where(m => m != lobby), m => m.Warps.Any(w => w.TargetMap.Equals("LilycoveCity_ContestLobby", StringComparison.OrdinalIgnoreCase) && !w.Shut));
        // The receptionist and the three drums are people of the lobby
        Assert.Contains(lobby.Everyone, n => n.Script == "ContestReceptionist");
        Assert.Equal(3, lobby.Everyone.Count(n => n.Script is "BlenderOne" or "BlenderTwo" or "BlenderThree"));
    }

    [Fact]
    public void ThePokeblockCaseIsAKeyItemTheBagOpensAndTheBlenderIsInTheRegionsScripts()
    {
        var item = ItemDatabase.Get("Pokéblock Case");
        Assert.NotNull(item);
        Assert.Equal("Pokéblock Case", item!.Name);
        Assert.False(string.IsNullOrEmpty(LocalNames.Item("Pokéblock Case")));
        Assert.Empty(ScriptLibrary.Default.Problems().Where(p => p.Contains(Lobby)));
    }
}
