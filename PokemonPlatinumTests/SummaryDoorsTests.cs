using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>The summary's move pages and the other doors into it (plan 08 · P4): the PC and the battle's switching cards.</summary>
[Collection("Shared clocks and dice")]
public class SummaryDoorsTests
{
    private static Pokemon WithMoves(string species, params string[] moves) => Of(Gender.Male, species, 20, moves);

    private static void Frame(params GameAction[] held)
    {
        InputManager.Feed(ActionFrame.None.Next(held));
    }

    // ------------------------------------------------------------------ the move pages

    [Theory]
    [InlineData(4)]
    [InlineData(3)]
    [InlineData(1)]
    public void TheMoveCursorGoesRoundHoweverManyMovesThereAre(int count)
    {
        var all = new[] { "Tackle", "Growl", "Scratch", "Ember" };
        var p = WithMoves("Chimchar", all.Take(count).ToArray());
        var view = new SummaryView();
        view.Open();
        Assert.False(view.LookAtMoves(p), "The first page has no moves to look at");
        while (view.Page != SummaryPage.BattleMoves) view.Turn(1);
        Assert.True(view.LookAtMoves(p));
        Assert.Equal(0, view.MoveSelection);
        for (int i = 1; i < count; i++)
        {
            view.MoveMove(1, p.Moves.Count);
            Assert.Equal(i, view.MoveSelection);
        }
        view.MoveMove(1, p.Moves.Count);
        Assert.Equal(0, view.MoveSelection);
        view.MoveMove(-1, p.Moves.Count);
        Assert.Equal(count - 1, view.MoveSelection);
        view.LeaveMoves();
        Assert.Equal(-1, view.MoveSelection);
    }

    [Fact]
    public void BothMovePagesTakeTheCursorAndTurningPutsItAway()
    {
        var p = WithMoves("Chimchar", "Scratch", "Ember");
        var view = new SummaryView();
        view.Open();
        while (view.Page != SummaryPage.BattleMoves) view.Turn(1);
        Assert.True(view.LookAtMoves(p));
        view.Turn(1);
        Assert.Equal(SummaryPage.ContestMoves, view.Page);
        Assert.Equal(-1, view.MoveSelection);
        Assert.True(view.LookAtMoves(p));
        // An Egg has no moves to look at
        var egg = WithMoves("Chimchar", "Scratch");
        egg.IsEgg = true;
        view.LeaveMoves();
        Assert.False(view.LookAtMoves(egg));
    }

    [Fact]
    public void EveryMoveHasItsTextOnTheMovePages()
    {
        // The battle page shows the move's own description, the contest page the effect's words in both languages
        foreach (string tag in new[] { "en", "ro" })
        {
            var keys = Text.KeysOf(tag);
            foreach (var effect in System.Enum.GetValues<PokemonPlatinumEngine.Models.SuperContests.ContestEffect>())
                Assert.Contains($"summary.contest.effect.{effect}", keys);
        }
        var scratch = MoveDatabase.Get("Scratch")!;
        Assert.False(string.IsNullOrEmpty(scratch.Description));
        Assert.NotNull(scratch.Contest);
    }

    [Fact]
    public void TheKeysDoTheSameInEveryDoor()
    {
        var p = WithMoves("Chimchar", "Scratch", "Ember");
        var view = new SummaryView { CanMark = false };
        view.Open();
        try
        {
            // Left and right turn the page; up and down are handed back as a step to another Pokémon
            Frame(GameAction.Right);
            var result = view.Press(p, 1, 0);
            Assert.Equal(default, result);
            Assert.Equal(SummaryPage.Memo, view.Page);
            Assert.Equal(new SummaryView.Result(false, 1), view.Press(p, 0, 1));

            // Confirm on the first page puts the summary away where the marks may not be set
            view.Open();
            Frame(GameAction.Confirm);
            Assert.True(view.Press(p, 0, 0).Close);
            // ... and cancel does anywhere
            view.Turn(1);
            Frame(GameAction.Cancel);
            Assert.True(view.Press(p, 0, 0).Close);

            // On a move page confirm brings the cursor up, and confirm puts it away without closing
            while (view.Page != SummaryPage.BattleMoves) view.Turn(1);
            Frame(GameAction.Confirm);
            Assert.False(view.Press(p, 0, 0).Close);
            Assert.Equal(0, view.MoveSelection);
            Frame();
            view.Press(p, 0, 1);
            Assert.Equal(1, view.MoveSelection);
            Frame(GameAction.Confirm);
            Assert.False(view.Press(p, 0, 0).Close);
            Assert.Equal(-1, view.MoveSelection);
        }
        finally { InputManager.Feed(null); }
    }

    // ------------------------------------------------------------------ the PC's door

    private static (PCScreen Screen, Party Party, PcBoxes Pc) PcWith(params Pokemon[] team)
    {
        var party = new Party();
        foreach (var p in team) party.Add(p);
        var pc = new PcBoxes();
        var screen = new PCScreen();
        screen.Open(pc);
        return (screen, party, pc);
    }

    [Fact]
    public void TheBoxMenuOffersTheSummaryAndItOpensOnTheFirstPage()
    {
        var (screen, party, pc) = PcWith(WithMoves("Chimchar", "Scratch"), WithMoves("Starly", "Tackle"));
        screen.Confirm(party, pc, _ => { });
        Assert.Contains(PcAction.Summary, screen.Menu!);
        screen.Choose(PcAction.Summary, party, pc, _ => { });
        Assert.True(screen.Summarizing);
        Assert.True(screen.Busy);
        Assert.Equal(SummaryPage.Info, screen.View.Page);
        // Cancel backs out of the summary first, and then of nothing
        Assert.True(screen.Cancel(party, pc));
        Assert.False(screen.Summarizing);
        Assert.False(screen.Busy);
    }

    [Fact]
    public void UpAndDownGoThroughTheTeamAndTheBoxInTheSummary()
    {
        var (screen, party, pc) = PcWith(WithMoves("Chimchar", "Scratch"), WithMoves("Starly", "Tackle"), WithMoves("Bidoof", "Tackle"));
        screen.Confirm(party, pc, _ => { });
        screen.Choose(PcAction.Summary, party, pc, _ => { });
        screen.StepSummary(party, pc, 1);
        Assert.Equal(1, screen.PartyIndex);
        Assert.Equal((1, 3), screen.SummaryPlace(party, pc));
        screen.StepSummary(party, pc, -1);
        screen.StepSummary(party, pc, -1);
        Assert.Equal(2, screen.PartyIndex);
        screen.Cancel(party, pc);

        // In the box only places with a Pokémon are stopped on, round the whole box
        pc.PutIn(0, 3, WithMoves("Shinx", "Tackle"));
        pc.PutIn(0, 9, WithMoves("Luxio", "Spark"));
        screen.Zone = StorageZone.Box;
        screen.Box = 0;
        screen.Cell = 3;
        screen.Confirm(party, pc, _ => { });
        screen.Choose(PcAction.Summary, party, pc, _ => { });
        Assert.Equal((0, 2), screen.SummaryPlace(party, pc));
        screen.StepSummary(party, pc, 1);
        Assert.Equal(9, screen.Cell);
        screen.StepSummary(party, pc, 1);
        Assert.Equal(3, screen.Cell);
    }

    // ------------------------------------------------------------------ the battle's door

    private static BattleEngine InBattleWithATeamOfTwo()
    {
        var party = new Party();
        party.Add(Of(Gender.Male, "Garchomp", 50, "Dragon Claw"));
        party.Add(Of(Gender.Male, "Luxray", 50, "Spark"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Of(Gender.Male, "Snorlax", 50) }, Random = Calm(), Rules = Ruleset.Platinum
        });
        Settle(battle);
        battle.SelectMainMenuOption(2);
        return battle;
    }

    [Fact]
    public void TheSwitchingCardsOpenTheSummaryOfThePokemonUnderTheCursorAndComeBack()
    {
        var battle = InBattleWithATeamOfTwo();
        Assert.Equal(BattleMenuState.SwitchPokemon, battle.HUD.MenuState);
        battle.HUD.SwitchMenuIndex = 1;
        try
        {
            Frame(GameAction.Item);
            battle.Update(1f / 60f);
            Assert.Equal(BattleMenuState.Summary, battle.HUD.MenuState);
            Assert.Equal(1, battle.SummaryIndex);
            Assert.Equal(SummaryPage.Info, battle.Summary.Page);

            // Up and down go to the other, and the cards' cursor goes with it
            Frame(GameAction.Up);
            battle.Update(1f / 60f);
            Assert.Equal(0, battle.SummaryIndex);
            Assert.Equal(0, battle.HUD.SwitchMenuIndex);

            // No marks to set from a battle: confirm on the first page puts the summary away
            Frame(GameAction.Confirm);
            battle.Update(1f / 60f);
            Assert.Equal(BattleMenuState.SwitchPokemon, battle.HUD.MenuState);
        }
        finally { InputManager.Feed(null); }
        // Nothing was switched
        Assert.Equal("Garchomp", battle.PlayerPokemon.Species.Name);
    }
}
