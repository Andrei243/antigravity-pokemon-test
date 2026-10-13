using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The game's states without the renderers (plan 16 · T11, <see cref="GameFlow"/>): a transition's closing, its midpoint
/// and its opening, what the picture holds through it, the close button's question and where the menus go back to.
/// No window.
/// </summary>
public class GameFlowTests
{
    [Fact]
    public void ATransitionClosesRunsItsMidpointOnceAndOpensOnTheNextState()
    {
        var flow = new GameFlow { State = GameState.Overworld };
        int midpoints = 0;
        flow.StartTransition(GameState.Battle, () => midpoints++, TransitionKind.Fade);

        Assert.Equal(GameState.Transition, flow.State);
        Assert.True(flow.Closing);
        Assert.Equal(GameState.Overworld, flow.Scene);

        Assert.Equal(TransitionStep.Closing, flow.Advance(SceneTransition.FadeSeconds / 2));
        Assert.Equal(0, midpoints);

        Assert.Equal(TransitionStep.Shut, flow.Advance(SceneTransition.FadeSeconds));
        Assert.Equal(1, midpoints);
        Assert.False(flow.Closing);
        Assert.Equal(0f, flow.Clock);
        Assert.Equal(GameState.Battle, flow.Scene);
        Assert.Equal(GameState.Transition, flow.State);

        Assert.Equal(TransitionStep.Opening, flow.Advance(SceneTransition.FadeSeconds / 2));
        Assert.Equal(TransitionStep.Opened, flow.Advance(SceneTransition.FadeSeconds));
        Assert.Equal(GameState.Battle, flow.State);
        Assert.Equal(1, midpoints);
        Assert.Equal(TransitionStep.None, flow.Advance(1f));
    }

    [Fact]
    public void WhatIsLetGoAtTheShutRunsBeforeTheMidpoint()
    {
        var flow = new GameFlow { State = GameState.Overworld };
        var order = new List<string>();
        flow.StartTransition(GameState.Overworld, () => order.Add("midpoint"));
        flow.Advance(SceneTransition.FadeSeconds, () => order.Add("shut"));
        Assert.Equal(new[] { "shut", "midpoint" }, order);
    }

    [Fact]
    public void AWayIntoABattleTakesItsOwnTime()
    {
        var flow = new GameFlow { State = GameState.Overworld };
        flow.StartTransition(GameState.Battle, null, TransitionKind.LeaderBand);
        float closing = SceneTransition.OutSeconds(TransitionKind.LeaderBand);
        Assert.True(closing > SceneTransition.FadeSeconds);
        Assert.Equal(TransitionStep.Closing, flow.Advance(closing - 0.01f));
        Assert.Equal(TransitionStep.Shut, flow.Advance(0.02f));
        Assert.Equal(TransitionKind.LeaderBand, flow.Kind);
    }

    [Fact]
    public void OpeningOnAStateFromTheDarkHasNoMidpoint()
    {
        var flow = new GameFlow { State = GameState.Title };
        flow.OpenOn(GameState.Overworld);
        Assert.False(flow.Closing);
        Assert.Equal(GameState.Overworld, flow.Scene);
        flow.After = GameState.Journal;
        Assert.Equal(TransitionStep.Opened, flow.Advance(SceneTransition.FadeSeconds));
        Assert.Equal(GameState.Journal, flow.State);
    }

    [Fact]
    public void TheCloseButtonsQuestionShowsTheStateUnderItAndGivesItBack()
    {
        var flow = new GameFlow { State = GameState.BagMenu };
        flow.AskToClose();
        Assert.Equal(GameState.WindowClosing, flow.State);
        Assert.Equal(GameState.BagMenu, flow.Shown);
        Assert.Equal(GameState.BagMenu, flow.Scene);
        flow.KeepPlaying();
        Assert.Equal(GameState.BagMenu, flow.State);
    }

    [Fact]
    public void TheQuestionOverATransitionShowsTheTransitionsScene()
    {
        var flow = new GameFlow { State = GameState.Overworld };
        flow.StartTransition(GameState.Battle);
        flow.AskToClose();
        Assert.Equal(GameState.Transition, flow.Shown);
        Assert.Equal(GameState.Overworld, flow.Scene);
    }

    [Fact]
    public void TheMenusGoBackToTheFieldUnlessToldOtherwise()
    {
        var flow = new GameFlow();
        Assert.Equal(GameState.Title, flow.State);
        Assert.Equal(GameState.Overworld, flow.OptionsReturn);
        Assert.Equal(GameState.Overworld, flow.EvolutionReturn);
        Assert.Empty(flow.Evolutions);
    }

    [Fact]
    public void ANewFlowHoldsAnEmptyTeamBagAndStoryAndTheStartingMoney()
    {
        var flow = new GameFlow();
        Assert.Equal(0, flow.Party.Count);
        Assert.Equal(3000, flow.Money);
        Assert.Null(flow.Battle);
        Assert.Equal(0, flow.Story.Revision);
    }

    [Fact]
    public void TheMoneyIsChangedWhereItIsKept()
    {
        var flow = new GameFlow();
        Spend(ref flow.Money, 500);
        Assert.Equal(2500, flow.Money);

        static void Spend(ref int money, int price) => money -= price;
    }

    [Fact]
    public void AnActionPressedIsDownForItsFirstFrameOnly()
    {
        var first = ActionFrame.Press(GameAction.Up);
        Assert.True(first.IsPressed(GameAction.Up));
        Assert.True(first.IsDown(GameAction.Up));
        var held = first.Next(GameAction.Up, GameAction.Run);
        Assert.False(held.IsPressed(GameAction.Up));
        Assert.True(held.IsDown(GameAction.Up));
        Assert.True(held.IsPressed(GameAction.Run));
        var let = held.Next();
        Assert.False(let.IsDown(GameAction.Up));
        Assert.False(let.IsPressed(GameAction.Up));
    }

    [Fact]
    public void AScreenOfTheFlowIsSteeredByTheActionsItIsHanded()
    {
        var flow = new GameFlow();
        try
        {
            flow.StartMenu.Open();
            flow.Act(ActionFrame.Press(GameAction.Down));
            Assert.Equal(StartMenuChoice.None, flow.StartMenu.Update());
            Assert.Equal(1, flow.StartMenu.SelectedIndex);

            flow.Act(ActionFrame.Press(GameAction.Confirm));
            Assert.Equal(StartMenuChoice.Pokemon, flow.StartMenu.Update());

            // Run is a held action, pressed for as long as it is down
            flow.Act(ActionFrame.Press(GameAction.Run).Next(GameAction.Run));
            Assert.True(InputManager.IsActionPressed(GameAction.Run));
            flow.Act(ActionFrame.None);
            Assert.Equal(0, InputManager.Axis(GameAction.Left, GameAction.Right, held: true));
        }
        finally
        {
            flow.Act(null);
        }
        Assert.Null(InputManager.Fed);
    }

    [Fact]
    public void AModelWantedIsAskedOfWhoeverAnswers()
    {
        var flow = new GameFlow();
        flow.WantModel("Turtwig");
        var asked = new List<string>();
        flow.ModelWanted += asked.Add;
        flow.WantModel("Turtwig");
        flow.WantModel("Charizard-Mega-X");
        Assert.Equal(new[] { "Turtwig", "Charizard-Mega-X" }, asked);
    }

    [Fact]
    public void TheScreensThatRenderFiguresAreMadeAndSteeredWithNoWindow()
    {
        var flow = new GameFlow();
        try
        {
            flow.IntroScreen.Open();
            flow.Act(ActionFrame.None);
            for (int i = 0; i < 30; i++) flow.IntroScreen.Update(1f / 30f);
            Assert.True(flow.IntroScreen.Appearance().Backdrop >= 0f);
            Assert.False(flow.TitleScreen.HasSave);
            flow.TitleScreen = TitleScreen.WithSlots(new[] { new TitleSlot(2, null) });
            Assert.False(flow.TitleScreen.HasSave);
            Assert.NotNull(flow.EvolutionScreen);
            Assert.NotNull(flow.WardrobeScreen);
        }
        finally
        {
            flow.Act(null);
        }
    }

    [Fact]
    public void AGamesRecordsAndItsOtherScreensAreTheFlowsWithNoWindow()
    {
        // Plan 16 · T11g: what a save keeps beside the field's objects, and the screens of the game's other corners,
        // are made with the flow and need no window
        var flow = new GameFlow();
        Assert.Equal(1, flow.SaveSlot);
        Assert.Empty(flow.HallOfFame.Entries);
        Assert.Equal(0, flow.DayCare.Count);
        Assert.Null(flow.Along);
        Assert.Null(flow.Tower);
        Assert.Null(flow.Arcade);
        Assert.NotNull(flow.Berries);
        Assert.NotNull(flow.Ceremony);
        Assert.NotNull(flow.Credits);
        Assert.NotNull(flow.HatchScreen);
        Assert.NotNull(flow.DecorationScreen);
        Assert.NotNull(flow.Choice);

        flow.SaveSlot = 3;
        flow.TrainerScore += 5;
        Assert.Equal(3, flow.SaveSlot);
        Assert.Equal(5, flow.TrainerScore);
    }

    [Fact]
    public void TheFieldsRunningStateStartsAtRestInTheFlow()
    {
        // Plan 16 · T11h: what the field has going from frame to frame is the flow's, and a new flow has nothing going
        var flow = new GameFlow();
        Assert.Null(flow.PlayerWalk);
        Assert.Empty(flow.NpcWalks);
        Assert.Null(flow.TrainerApproach);
        Assert.Null(flow.Fishing);
        Assert.Null(flow.Trap);
        Assert.Equal(-1, flow.PresenceRevision);
        Assert.True(flow.TrainersLookOnArrival);
        Assert.Equal(BattleOutcome.None, flow.ScriptOutcome);
        Assert.Empty(flow.EnteredSlots);

        flow.ScriptAnswer = 2;
        flow.ClockTicks++;
        Assert.Equal(2, flow.ScriptAnswer);
        Assert.Equal(1, flow.ClockTicks);
    }
}
