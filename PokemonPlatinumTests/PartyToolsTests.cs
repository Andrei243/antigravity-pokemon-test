using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q19, remembering and renaming from the party menu: the moves a Pokémon could remember, the menu's two
/// entries and what the row decides, the choice of a move to forget, and the keyboard for a Pokémon's name.
/// </summary>
[Collection("Shared clocks and dice")]
public class PartyToolsTests
{
    private static Pokemon Turtwig(int level, params string[] moves) => Scenario.Mon("Turtwig", level, moves);

    private static (PartyScreen Screen, Party Team) Menu(Pokemon p, bool tools = true)
    {
        var team = new Party();
        team.Add(p);
        var screen = new PartyScreen { PartyTools = tools };
        screen.Open();
        return (screen, team);
    }

    /// <summary>Opens the Pokémon's menu and chooses the entry of that kind.</summary>
    private static void Choose(PartyScreen screen, Party team, PartyActionKind kind)
    {
        screen.Confirm(team);
        int at = screen.Actions!.ToList().FindIndex(a => a.Kind == kind);
        Assert.True(at >= 0, kind + " is not in the menu");
        screen.MoveAction(at);
        screen.Confirm(team);
    }

    [Fact]
    public void TheListIsTheLevelUpMovesAtOrBelowItsLevelThatItDoesNotKnowInLevelOrder()
    {
        var p = Turtwig(30, "Tackle");
        var list = Relearning.MovesFor(p);
        Assert.NotEmpty(list);
        Assert.DoesNotContain("Tackle", list);
        Assert.Equal(list.Count, list.Distinct().Count());
        var levels = list.Select(m => p.Learnset.First(l => l.MoveName == m).Level).ToList();
        Assert.All(levels, l => Assert.True(l <= 30));
        Assert.Equal(levels.OrderBy(l => l), levels);
        // Everything it could have learned by now, bar the one it knows, is there
        var expected = p.Learnset.Where(l => l.Level <= 30 && l.MoveName != "Tackle").Select(l => l.MoveName).Distinct();
        Assert.Equal(expected.OrderBy(m => m), list.OrderBy(m => m));
    }

    [Fact]
    public void ALowerLevelHasFewerAndAnEggNone()
    {
        Assert.True(Relearning.MovesFor(Turtwig(8)).Count <= Relearning.MovesFor(Turtwig(30)).Count + 2);
        var egg = Turtwig(5);
        egg.IsEgg = true;
        Assert.Empty(Relearning.MovesFor(egg));
    }

    [Fact]
    public void AMoveIsTaughtForNothingIntoAFreePlace()
    {
        var p = Turtwig(30, "Tackle");
        string move = Relearning.MovesFor(p)[0];
        Assert.True(Relearning.Teach(p, move));
        Assert.True(p.Knows(move));
        Assert.DoesNotContain(move, Relearning.MovesFor(p));
        Assert.False(Relearning.Teach(p, "Splash"), "a move off its list can't be remembered");
    }

    [Fact]
    public void WithTheRowOffTheMenuIsToday()
    {
        var (screen, team) = Menu(Turtwig(30, "Tackle"), tools: false);
        screen.Confirm(team);
        Assert.DoesNotContain(screen.Actions!, a => a.Kind is PartyActionKind.Remember or PartyActionKind.Rename);
        Assert.Equal(PartyActionKind.Summary, screen.Actions![0].Kind);
        Assert.Equal(PartyActionKind.Cancel, screen.Actions![^1].Kind);
    }

    [Fact]
    public void TheRowFollowsTheRulesAndCanBeChosenEitherWay()
    {
        Assert.Equal(RulesDefault.Rules, new GameSettings().PartyTools);
        Assert.False(FieldHelp.On(RulesDefault.Rules, RulesPreset.Platinum));
        Assert.True(FieldHelp.On(RulesDefault.Rules, RulesPreset.Modern));
        Assert.True(FieldHelp.On(RulesDefault.On, RulesPreset.Platinum));
        Assert.False(FieldHelp.On(RulesDefault.Off, RulesPreset.Modern));
        var s = new GameSettings();
        OptionsScreen.Change(s, OptionRow.PartyTools, 1);
        Assert.Equal(RulesDefault.On, s.PartyTools);
        OptionsScreen.Change(s, OptionRow.PartyTools, 1);
        Assert.Equal(RulesDefault.Off, s.PartyTools);
        OptionsScreen.Change(s, OptionRow.PartyTools, 1);
        Assert.Equal(RulesDefault.Rules, s.PartyTools);
    }

    [Fact]
    public void RememberAndRenameSitBetweenTheFieldMovesAndSwitch()
    {
        var (screen, team) = Menu(Turtwig(30, "Tackle"));
        screen.Confirm(team);
        var kinds = screen.Actions!.Select(a => a.Kind).ToList();
        Assert.Equal(new[] { PartyActionKind.Summary, PartyActionKind.Remember, PartyActionKind.Rename, PartyActionKind.Switch, PartyActionKind.Cancel },
            kinds.Where(k => k != PartyActionKind.FieldMove));
        Assert.True(kinds.IndexOf(PartyActionKind.Remember) < kinds.IndexOf(PartyActionKind.Switch));
    }

    [Fact]
    public void ATradedPokemonHasNoRenameAndAnEggHasNeither()
    {
        var traded = Turtwig(30, "Tackle");
        traded.OriginalTrainer = new TrainerMark("Someone", 1234, PlayerLook.Boy);
        var (screen, team) = Menu(traded);
        screen.Confirm(team);
        Assert.Contains(screen.Actions!, a => a.Kind == PartyActionKind.Remember);
        Assert.DoesNotContain(screen.Actions!, a => a.Kind == PartyActionKind.Rename);

        var egg = Turtwig(5);
        egg.IsEgg = true;
        var (eggScreen, eggTeam) = Menu(egg);
        eggScreen.Confirm(eggTeam);
        Assert.DoesNotContain(eggScreen.Actions!, a => a.Kind is PartyActionKind.Remember or PartyActionKind.Rename);
    }

    [Fact]
    public void RememberTeachesTheChosenMoveAndSaysSo()
    {
        var p = Turtwig(30, "Tackle");
        var (screen, team) = Menu(p);
        var list = Relearning.MovesFor(p);
        Choose(screen, team, PartyActionKind.Remember);
        Assert.True(screen.Remembering);
        Assert.Equal(list, screen.RememberMoves);
        screen.MoveRemember(1);
        screen.Confirm(team);
        Assert.False(screen.Remembering);
        Assert.True(p.Knows(list[1]));
        Assert.Contains(LocalNames.Move(list[1]), screen.Prompt);
    }

    [Fact]
    public void ANinthPokemonWithNothingToRememberSaysSoInThePrompt()
    {
        var p = Turtwig(1, "Tackle");
        var (screen, team) = Menu(p);
        Choose(screen, team, PartyActionKind.Remember);
        Assert.False(screen.Remembering);
        Assert.Contains(p.DisplayName, screen.Prompt);
    }

    [Fact]
    public void AFifthMoveAsksWhichToForgetAndBKeepsAllFour()
    {
        var p = Turtwig(40, "Tackle", "Withdraw", "Absorb", "Growl");
        var (screen, team) = Menu(p);
        var before = p.Moves.Select(m => m.Name).ToList();
        string wanted = Relearning.MovesFor(p)[0];
        Choose(screen, team, PartyActionKind.Remember);
        screen.Confirm(team);
        Assert.Equal(wanted, screen.ForgetFor);
        Assert.True(screen.Remembering);

        // B keeps all four and goes back to the list; B again leaves it
        screen.Cancel();
        Assert.Null(screen.ForgetFor);
        Assert.True(screen.Remembering);
        Assert.Equal(before, p.Moves.Select(m => m.Name));
        screen.Cancel();
        Assert.False(screen.Remembering);
        Assert.Equal(before, p.Moves.Select(m => m.Name));

        // The last row of the choice keeps them too
        Choose(screen, team, PartyActionKind.Remember);
        screen.Confirm(team);
        screen.MoveRemember(-1);
        Assert.Equal(4, screen.ForgetCursor);
        screen.Confirm(team);
        Assert.Null(screen.ForgetFor);
        Assert.Equal(before, p.Moves.Select(m => m.Name));

        // A move chosen to forget is replaced in its place
        screen.Confirm(team);
        screen.MoveRemember(1);
        screen.Confirm(team);
        Assert.False(screen.Remembering);
        Assert.Equal(wanted, p.Moves[1].Name);
        Assert.False(p.Knows(before[1]));
        Assert.Equal(4, p.Moves.Count);
    }

    [Fact]
    public void RenameHandsTheTeamsPokemonOnceToTheGameAndTheMenuComesBack()
    {
        var p = Turtwig(30, "Tackle");
        var (screen, team) = Menu(p);
        Choose(screen, team, PartyActionKind.Rename);
        Assert.Equal(0, screen.TakeRename());
        Assert.Null(screen.TakeRename());
        Assert.True(screen.IsActive);
    }

    [Fact]
    public void ANameKeepsToTheKeyboardsRulesAtTenLettersAndSurvivesASave()
    {
        var p = Turtwig(30, "Tackle");
        var naming = new NicknameScreen();
        naming.Open(p);
        Assert.Equal(10, naming.Entry!.MaxLength);
        foreach (char c in "Shellshockers") naming.Entry.Type(c);
        Assert.Equal("Shellshock", naming.Entry.Text);
        naming.ToDone();
        naming.Confirm();
        Assert.False(naming.IsActive);
        Assert.Equal("Shellshock", naming.Nickname);
        p.Nickname = naming.Nickname!;
        Assert.Equal("Shellshock", p.DisplayName);

        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal("Shellshock", back.Nickname);
        Assert.Equal("Shellshock", back.DisplayName);
    }

    [Fact]
    public void AnEmptyNameGivesTheSpeciesBackAndBLeavesTheOldOneAlone()
    {
        var p = Turtwig(30, "Tackle");
        p.Nickname = "Shelly";
        var naming = new NicknameScreen();
        naming.Open(p);
        Assert.Equal("Shelly", naming.Entry!.Text);
        for (int i = 0; i < 6; i++) naming.Cancel();
        Assert.True(naming.IsActive, "B deletes letters, the keyboard stays");
        naming.ToDone();
        naming.Confirm();
        Assert.Equal(string.Empty, naming.Nickname);
        p.Nickname = naming.Nickname!;
        Assert.Equal(p.Species.DisplayName, p.DisplayName);

        // B on a name already empty leaves the keyboard with nothing changed
        p.Nickname = "Shelly";
        naming.Open(p);
        for (int i = 0; i < 6; i++) naming.Cancel();
        naming.Cancel();
        Assert.False(naming.IsActive);
        Assert.True(naming.Left);
        Assert.Null(naming.Nickname);
        Assert.Equal("Shelly", p.Nickname);
    }

    [Fact]
    public void ATrainersNameKeepsItsSevenLetters()
    {
        Assert.Equal(7, new NameEntry().MaxLength);
        Assert.Equal("Abcdefg", PlayerIdentity.Clean("Abcdefghij"));
        Assert.Equal("Abcdefghij", PlayerIdentity.Clean("Abcdefghijkl", 10));
    }
}
