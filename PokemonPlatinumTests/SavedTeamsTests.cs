using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Saved teams and the judge (plan 12 · Q27; style guide, "PC boxes", "Teams" and "The judge"): a team kept by its members'
/// ids, swapped in from the PC in one step, kept through a save, a load and a sort; a member gone skipped; a swap that
/// can't go through whole refused whole; and the judge's six words.
/// </summary>
public class SavedTeamsTests
{
    private static uint count;

    private static Pokemon Mon(string species, int level = 10)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, new Random(level));
        p.Id = PokemonIds.Make(77, ++count);
        return p;
    }

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    private static PCScreen ToolsOn(PcBoxes pc, List<SavedTeam> teams)
    {
        var screen = new PCScreen { Tools = true, Teams = teams };
        screen.Open(pc);
        return screen;
    }

    private static string[] Names(Party party) => party.Members.Select(p => p.Species.Name).ToArray();

    [Fact]
    public void ATeamSwappedInComesInItsOrderAndTheTeamThatWasThereTakesTheirPlaces()
    {
        var pc = new PcBoxes();
        var turtwig = Mon("Turtwig"); var starly = Mon("Starly");
        var shinx = Mon("Shinx"); var bidoof = Mon("Bidoof"); var budew = Mon("Budew");
        var party = PartyOf(turtwig, starly);
        pc.PutIn(2, 7, shinx);
        pc.PutIn(0, 3, bidoof);
        pc.PutIn(0, 4, budew);
        shinx.CurrentHP = 1;
        var team = new SavedTeam { Name = "B", Members = { shinx.Id, starly.Id, bidoof.Id } };

        var swap = SavedTeams.SwapIn(team, party, pc);

        Assert.True(swap.Done);
        Assert.Equal(new[] { "Shinx", "Starly", "Bidoof" }, Names(party));
        // healed as anything withdrawn is
        Assert.Equal(shinx.MaxHP, shinx.CurrentHP);
        // Turtwig, the one who left, took the first place a member left
        Assert.Same(turtwig, pc[2, 7]);
        Assert.Null(pc[0, 3]);
        Assert.Same(budew, pc[0, 4]);
    }

    [Fact]
    public void TwoNamedTeamsSwapInAndOutAfterASaveALoadAndASort()
    {
        var pc = new PcBoxes();
        var a = new[] { Mon("Turtwig", 12), Mon("Starly", 9), Mon("Bidoof", 7) };
        var b = new[] { Mon("Shinx", 20), Mon("Budew", 15), Mon("Kricketot", 6), Mon("Abra", 11) };
        var party = PartyOf(a);
        foreach (var p in b) pc.Store(p);
        var teams = new List<SavedTeam>();
        var screen = ToolsOn(pc, teams);
        screen.KeepTeam(party, "Road", _ => { });
        teams.Add(new SavedTeam { Name = "Gym", Members = b.Select(p => p.Id).ToList() });

        // A save and a load: the boxes, the party and the teams as a file has them
        var save = new SaveData
        {
            Party = party.Members.Select(SavedPokemonData.FromPokemon).ToList(),
            Boxes = SavedBoxes.From(pc),
            Teams = teams.Select(t => t.Copy()).ToList()
        };
        var read = SaveManager.Read(SaveManager.Write(save))!;
        var pc2 = read.Boxes!.ToBoxes();
        var party2 = new Party();
        foreach (var s in read.Party) party2.Add(s.ToPokemon());
        var teams2 = read.Teams!;
        Assert.Equal(new[] { "Road", "Gym" }, teams2.Select(t => t.Name));

        // and a sort of every box, which moves the stored but not their ids
        BoxQuery.SortAll(pc2, BoxSort.Level);

        var screen2 = ToolsOn(pc2, teams2);
        screen2.TeamIndex = 1;
        screen2.SwapIn(party2, pc2, _ => { });
        Assert.Equal(new[] { "Shinx", "Budew", "Kricketot", "Abra" }, Names(party2));
        Assert.Equal(3, pc2.Count);
        screen2.TeamIndex = 0;
        screen2.SwapIn(party2, pc2, _ => { });
        Assert.Equal(new[] { "Turtwig", "Starly", "Bidoof" }, Names(party2));
        Assert.Equal(4, pc2.Count);
        Assert.Equal(7, pc2.Count + party2.Count);
    }

    [Fact]
    public void AMemberReleasedIsSkippedWithALineAndTheRestCome()
    {
        var pc = new PcBoxes();
        var lead = Mon("Turtwig");
        var party = PartyOf(lead);
        var shinx = Mon("Shinx"); var budew = Mon("Budew"); var abra = Mon("Abra");
        foreach (var p in new[] { shinx, budew, abra }) pc.Store(p);
        var teams = new List<SavedTeam> { new() { Name = "Mixed", Members = { shinx.Id, budew.Id, abra.Id } } };
        Assert.Equal(ReleaseOutcome.Released, pc.Release(budew, party));

        var said = new List<string>();
        var screen = ToolsOn(pc, teams);
        screen.SwapIn(party, pc, said.Add);

        Assert.Equal(new[] { "Shinx", "Abra" }, Names(party));
        Assert.Contains(lead, pc.All);
        Assert.Single(said);
        Assert.Contains("1", said[0]);
        // and the panel shows the gap: Budew is found nowhere
        Assert.Equal((-1, (BoxPlace?)null), SavedTeams.Find(budew.Id, party, pc));
    }

    [Fact]
    public void ASwapThatCannotGoThroughWholeChangesNothing()
    {
        // Someone leaving holds Mail
        var pc = new PcBoxes();
        var lead = Mon("Turtwig");
        lead.HeldItem = ItemDatabase.GetAll().First(i => i.Pocket == ItemPocket.Mail);
        var party = PartyOf(lead);
        var shinx = Mon("Shinx");
        pc.Store(shinx);
        var team = new SavedTeam { Name = "S", Members = { shinx.Id } };
        var swap = SavedTeams.SwapIn(team, party, pc);
        Assert.False(swap.Done);
        Assert.Equal("boxTeams.swap.mail", swap.Refusal);
        Assert.Equal(new[] { "Turtwig" }, Names(party));
        Assert.Same(shinx, pc[0, 0]);

        // None of the team stored any more
        var gone = new SavedTeam { Name = "G", Members = { Guid.NewGuid() } };
        Assert.Equal("boxTeams.swap.noneHere", SavedTeams.SwapIn(gone, party, pc).Refusal);

        // No room: every box full, the member's place the one that comes free, and a team of six leaving for one coming
        var small = new PcBoxes();
        var six = PartyOf(Mon("Starly"), Mon("Bidoof"), Mon("Budew"), Mon("Kricketot"), Mon("Abra"), Mon("Shinx"));
        var coming = Mon("Piplup");
        small.PutIn(0, 0, coming);
        var zubat = Mon("Zubat");
        for (int s = 1; s < small.Capacity; s++) small.PutIn(s / PcBoxes.BoxSize, s % PcBoxes.BoxSize, zubat.Clone());
        var lone = new SavedTeam { Name = "P", Members = { coming.Id } };
        var refused = SavedTeams.SwapIn(lone, six, small);
        Assert.Equal("boxTeams.swap.noRoom", refused.Refusal);
        Assert.Equal(6, six.Count);
        Assert.Same(coming, small[0, 0]);
    }

    [Fact]
    public void AMonotypeRunKeepsOutWhomItWouldAndALimitIsHeld()
    {
        var pc = new PcBoxes();
        var party = PartyOf(Mon("Turtwig"));
        var shinx = Mon("Shinx"); var budew = Mon("Budew");
        pc.Store(shinx); pc.Store(budew);
        var team = new SavedTeam { Name = "R", Members = { shinx.Id, budew.Id } };
        var swap = SavedTeams.SwapIn(team, party, pc, p => p.HasType(PokemonType.Grass) ? null : "no", limit: 6);
        Assert.True(swap.Done);
        Assert.Equal(new[] { "Budew" }, Names(party));
    }

    [Fact]
    public void SaveThisTeamKeepsThePartyAndGivesIdsFirst()
    {
        var pc = new PcBoxes();
        var fresh = new Pokemon(PokemonDatabase.Get("Starly")!, 5, new Random(3));
        var party = PartyOf(fresh);
        var teams = new List<SavedTeam>();
        var screen = ToolsOn(pc, teams);
        uint next = 0;
        screen.BeforeKeeping = () => PokemonIds.Give(party.Members, 9, ref next);
        screen.Zone = StorageZone.BoxName;
        screen.Confirm(party, pc, _ => { });
        screen.MenuIndex = screen.BoxMenu!.ToList().IndexOf(PcBoxAction.Teams);
        screen.Confirm(party, pc, _ => { });
        Assert.True(screen.TeamsOpen);
        Assert.True(screen.OnSaveRow);
        screen.Confirm(party, pc, _ => { });
        Assert.NotNull(screen.TeamNaming);
        screen.FinishTeamNaming(party, "Wings", _ => { });
        Assert.Single(teams);
        Assert.Equal("Wings", teams[0].Name);
        Assert.Equal(new[] { fresh.Id }, teams[0].Members);
        Assert.NotEqual(Guid.Empty, fresh.Id);

        // B closes the panel; the box's menu has TEAMS only with the tools
        Assert.True(screen.Cancel(party, pc));
        Assert.False(screen.TeamsOpen);
        var off = new PCScreen { Tools = false };
        Assert.DoesNotContain(PcBoxAction.Teams, off.BoxActions);
        Assert.DoesNotContain(PcAction.Judge, off.ActionsFor(StorageZone.Box));
        Assert.DoesNotContain(PcAction.Judge, off.ActionsFor(StorageZone.Party));
    }

    [Fact]
    public void ATeamIsDeletedFromItsMenuAndEightAreTheMost()
    {
        var pc = new PcBoxes();
        var party = PartyOf(Mon("Turtwig"));
        var teams = Enumerable.Range(1, SavedTeams.Most).Select(i => new SavedTeam { Name = $"T{i}", Members = { party.Members[0].Id } }).ToList();
        var screen = ToolsOn(pc, teams);
        screen.OpenTeams();
        // No SAVE THIS TEAM row with eight kept
        Assert.Equal(SavedTeams.Most, screen.TeamRows);
        screen.TeamIndex = 2;
        screen.Confirm(party, pc, _ => { });
        Assert.Equal(PcTeamAction.SwapIn, screen.TeamMenu![0]);
        screen.MenuIndex = 1;
        screen.Confirm(party, pc, _ => { });
        Assert.Equal(SavedTeams.Most - 1, teams.Count);
        Assert.DoesNotContain(teams, t => t.Name == "T3");
        Assert.Equal(SavedTeams.Most, screen.TeamRows);
    }

    [Theory]
    [InlineData(0, IvGrade.Nothing)]
    [InlineData(1, IvGrade.Meagre)]
    [InlineData(10, IvGrade.Meagre)]
    [InlineData(11, IvGrade.Fair)]
    [InlineData(20, IvGrade.Fair)]
    [InlineData(21, IvGrade.Good)]
    [InlineData(25, IvGrade.Good)]
    [InlineData(26, IvGrade.Splendid)]
    [InlineData(30, IvGrade.Splendid)]
    [InlineData(31, IvGrade.Flawless)]
    public void TheJudgeHasSixWords(int iv, IvGrade grade) => Assert.Equal(grade, IvJudge.Grade(iv));

    [Fact]
    public void TheJudgeNamesTheBestStatOrSaysAllAlike()
    {
        var p = Mon("Shinx");
        p.IvHP = p.IvAttack = p.IvDefense = p.IvSpAttack = p.IvSpDefense = p.IvSpeed = 15;
        Assert.Null(IvJudge.Best(p));
        p.IvSpeed = 31;
        p.IvAttack = 28;
        Assert.Equal(StatType.Speed, IvJudge.Best(p));
        Assert.Equal(new[] { IvGrade.Fair, IvGrade.Splendid, IvGrade.Fair, IvGrade.Fair, IvGrade.Fair, IvGrade.Flawless }, IvJudge.Of(p));
        // Every word has its English and its Romanian
        foreach (var g in Enum.GetValues<IvGrade>()) Assert.Contains($"boxTeams.judge.grade.{g}", Text.EnglishKeys);
    }

    [Fact]
    public void JudgeTurnsTheDetailColumnUntilB()
    {
        var pc = new PcBoxes();
        var party = PartyOf(Mon("Turtwig"), Mon("Starly"));
        var screen = ToolsOn(pc, new List<SavedTeam>());
        screen.Confirm(party, pc, _ => { });
        screen.MenuIndex = screen.Menu!.ToList().IndexOf(PcAction.Judge);
        screen.Confirm(party, pc, _ => { });
        Assert.True(screen.Judging);
        Assert.False(screen.Busy);
        // the cursor moves with the page up
        screen.Move(0, 1, party.Count);
        Assert.Equal(1, screen.PartyIndex);
        Assert.True(screen.Judging);
        Assert.True(screen.Cancel(party, pc));
        Assert.False(screen.Judging);
    }
}
