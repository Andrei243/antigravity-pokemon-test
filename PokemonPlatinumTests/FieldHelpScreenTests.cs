using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q18, the screens: the party menu's strip of granted moves, the move naming a helper as its user, Fly with a
/// flier outside the team, and the Pokétch's Hidden Moves app, which is there only while the row is on.
/// </summary>
[Collection("Shared clocks and dice")]
public class FieldHelpScreenTests
{
    private static Party Team(int count)
    {
        var party = new Party();
        for (int i = 0; i < count; i++) party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 20));
        return party;
    }

    [Fact]
    public void TheCursorGoesDownFromTheLastRowOntoTheStripAndBackUp()
    {
        var screen = new PartyScreen();
        screen.Open();
        screen.Helpers = new[] { FieldMove.Cut, FieldMove.Surf };
        screen.MoveCursor(0, 1, 6);
        Assert.False(screen.OnStrip);
        screen.SelectedIndex = 4;
        screen.MoveCursor(0, 1, 6);
        Assert.True(screen.OnStrip);
        screen.MoveCursor(1, 0, 6);
        Assert.Equal(1, screen.StripIndex);
        screen.MoveCursor(0, -1, 6);
        Assert.False(screen.OnStrip);
        Assert.Equal(4, screen.SelectedIndex);
    }

    [Fact]
    public void WithNoHelpersThereIsNoStripAndTheCursorWrapsAsItDid()
    {
        var screen = new PartyScreen();
        screen.Open();
        screen.SelectedIndex = 4;
        screen.MoveCursor(0, 1, 6);
        Assert.False(screen.OnStrip);
        Assert.Equal(0, screen.SelectedIndex);
    }

    [Fact]
    public void AMoveChosenOnTheStripNamesNoPokemonOfTheTeamAsItsUser()
    {
        var team = Team(3);
        var screen = new PartyScreen();
        screen.Open();
        screen.Helpers = new[] { FieldMove.Cut, FieldMove.Fly };
        screen.SelectedIndex = 2;
        screen.MoveCursor(0, 1, team.Count);
        screen.MoveCursor(1, 0, team.Count);
        screen.Confirm(team);
        Assert.Equal((FieldMove.Fly, PartyScreen.HelperUser), screen.TakeFieldMove());
        Assert.Null(screen.TakeFieldMove());
        // B comes off the strip before it closes the menu
        screen.Cancel();
        Assert.False(screen.OnStrip);
        Assert.True(screen.IsActive);
    }

    [Fact]
    public void ATeamMembersOwnMoveStillNamesItsPlace()
    {
        var team = Team(1);
        team.Members[0].Moves.Clear();
        team.Members[0].Moves.Add(new Move(MoveDatabase.Get("Cut")!));
        var screen = new PartyScreen();
        screen.Open();
        screen.Confirm(team);
        int cut = screen.Actions!.ToList().FindIndex(a => a.Kind == PartyActionKind.FieldMove);
        for (int i = 0; i < cut; i++) screen.MoveAction(1);
        screen.Confirm(team);
        Assert.Equal((FieldMove.Cut, 0), screen.TakeFieldMove());
    }

    [Fact]
    public void FlyRemembersAFlierThatIsNoneOfTheTeam()
    {
        var flier = FieldHelp.HelperFor(FieldMove.Fly);
        var screen = new TownMapScreen();
        screen.Open(new TownMapState(RegionMap.Sinnoh!, new StoryState(), RegionMap.CellOf(305, 540)), TownMapMode.Fly, flier);
        Assert.Same(flier, screen.Flier);
        Assert.Equal("Staravia", screen.Flier!.Species.Name);
        Assert.DoesNotContain(flier, Team(3).Members);
    }

    [Fact]
    public void TheHiddenMovesAppIsOnThePoketchOnlyWhileTheRowSaysSoAndIsNeverSaved()
    {
        var poketch = new Poketch { Enabled = true };
        poketch.Register(PoketchApp.DigitalWatch);
        Assert.Equal(new[] { PoketchApp.DigitalWatch }, poketch.Shown);
        poketch.ShowsHiddenMoves = true;
        Assert.Equal(new[] { PoketchApp.DigitalWatch, PoketchApp.HiddenMoves }, poketch.Shown);
        poketch.Next();
        Assert.IsType<HiddenMovesApp>(poketch.State);
        // It is no app anyone was given, so a save holds none
        Assert.DoesNotContain(PoketchApp.HiddenMoves, poketch.Apps);
        var again = new Poketch();
        again.Load(poketch.Save());
        Assert.Equal(new[] { PoketchApp.DigitalWatch }, again.Shown);
        poketch.ShowsHiddenMoves = false;
        Assert.Equal(new[] { PoketchApp.DigitalWatch }, poketch.Shown);
    }

    [Fact]
    public void TheHiddenMovesAppListsWhatIsGranted()
    {
        var poketch = new Poketch { Enabled = true, ShowsHiddenMoves = true };
        var context = new PoketchContext { Poketch = poketch, Granted = new[] { FieldMove.Cut, FieldMove.Surf } };
        var app = Assert.IsType<HiddenMovesApp>(poketch.State);
        Assert.Equal(new[] { FieldMove.Cut, FieldMove.Surf }, app.Moves(context));
        Assert.Empty(app.Buttons(context));
    }
}
