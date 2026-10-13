using PokemonPlatinumEngine.Core;
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
}
