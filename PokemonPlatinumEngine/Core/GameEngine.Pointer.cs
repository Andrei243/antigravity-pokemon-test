using System.Numerics;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

// The pointer (plan 12 · Q6, Q7; style guide, "Shapes and components", "The pointer"). The mouse's frame is read in
// the layout's units (InputManager.ReadPointer, through the Viewport Draw keeps) and turned into the screens' own
// moves: a hover brings the cursor of whatever is open onto what lies under the pointer (its HitTest, from the
// rectangles it draws with), a click presses the Confirm action on it, a right click the Cancel action (in the field,
// the menu), the wheel the arrows up and down. So nothing a screen does is done twice: a click is the A button, and
// the keyboard owns the screen while the mouse is still.
public partial class GameEngine
{
    private readonly UiPointer pointer = new();
    private PointerFrame? pointerSet;
    private bool clickHeld;

    /// <summary>Where the picture stands in the window, as the last frame drawn fitted it (plan 12 · Q6).</summary>
    public Viewport Viewport { get; private set; } = Viewport.Identity;

    /// <summary>The pointer: where it stands and whether its arrow is shown.</summary>
    public UiPointer Pointer => pointer;

    /// <summary>A tool's mouse for the next frame, in place of the window's (the harness's shots point with it).</summary>
    internal void SetPointer(PointerFrame frame) => pointerSet = frame;

    private static uint Bit(GameAction action) => 1u << (int)action;

    /// <summary>
    /// One frame of the mouse: the arrow shown or hidden, the cursor of what is open brought under the pointer, and
    /// the frame's actions given the presses the mouse stands for.
    /// </summary>
    private void KeepThePointer(float dt)
    {
        var frame = InputManager.Pointer;
        if (pointerSet is { } set)
        {
            frame = set;
            pointerSet = null;
        }
        var actions = InputManager.Fed ?? ActionFrame.None;
        pointer.Update(frame, actions.Pressed != 0, dt);
        if (!frame.Held) clickHeld = false;
        uint pressed = 0;
        if (frame.Any)
        {
            var target = PointableNow();
            bool onThePicture = frame.Inside || Viewport.OnThePicture(frame.At);
            int? hit = onThePicture ? target?.HitTest(frame.At) : null;
            if (hit is int over && over != UiPointer.Anywhere && (frame.Moved || frame.Click)) target!.PointAt(over);
            if (frame.Click && hit is int clicked && !target!.Press(clicked))
            {
                pressed |= Bit(GameAction.Confirm);
                clickHeld = true;
            }
            if (frame.RightClick) pressed |= Bit(RightClickAction());
            if (frame.Wheel > 0f) pressed |= Bit(GameAction.Up);
            else if (frame.Wheel < 0f) pressed |= Bit(GameAction.Down);
        }
        // The button kept down after a click is the A button kept down (a dangerous answer's hold, plan 12 · Q9)
        uint down = clickHeld ? Bit(GameAction.Confirm) : 0u;
        if ((pressed | down) != 0) InputManager.Feed(new ActionFrame(actions.Down | down | pressed, actions.Pressed | pressed) { MoveX = actions.MoveX, MoveY = actions.MoveY });
    }

    internal sealed partial class Driver
    {
        /// <summary>Hands the next frame a mouse of the tool's own (plan 12 · Q6): moved to a place, clicked or not; the game's own pointer reads it.</summary>
        public void Point(PointerFrame frame) => game.SetPointer(frame);

        /// <summary>The pointer as the game has it (where it stands, whether its arrow shows), and what it can be used on now.</summary>
        public UiPointer Pointer => game.pointer;
        public IPointable? Pointable => game.PointableNow();
    }

    /// <summary>A right click: the menu, in the field with nothing under way; the B button anywhere else.</summary>
    private GameAction RightClickAction() =>
        flow.State == GameState.Overworld && !flow.StartMenu.IsActive && !ScriptRunning && !flow.PoketchView.InHand
            ? GameAction.Menu
            : GameAction.Cancel;

    /// <summary>What the pointer can be used on now: the screen or the part of it that is open; null for nothing (the field).</summary>
    internal IPointable? PointableNow()
    {
        int sw = VirtualWidth, sh = VirtualHeight;
        if (closePrompt.IsOpen)
            return new Pointable(at => UiPointer.Hit(ModernUi.PromptButtons(sw, sh, closePrompt.Answers.Count), at),
                i => { if (i != closePrompt.Index) closePrompt.Move(i - closePrompt.Index); });
        switch (flow.State)
        {
            case GameState.Title:
                return new Pointable(flow.TitleScreen.HitTest, flow.TitleScreen.PointAt);
            case GameState.Intro:
                return new Pointable(flow.IntroScreen.HitTest, flow.IntroScreen.PointAt);
            case GameState.Overworld when flow.StartMenu.IsActive:
                return new Pointable(flow.StartMenu.HitTest, flow.StartMenu.PointAt);
            case GameState.Dialogue when flow.Choice.IsOpen:
                return new Pointable(at => ModernUi.HitRow(ModernUi.ChoiceRows(sw, sh, flow.Choice.Options, flow.Choice.Cursor), at), flow.Choice.PointAt);
            case GameState.Dialogue:
                return Pointable.ReadsOn;
            case GameState.Battle when retryPrompt.IsOpen:
                return new Pointable(at => UiPointer.Hit(ModernUi.PromptButtons(sw, sh, 2), at),
                    i => { if (i != retryPrompt.Index) retryPrompt.Move(i - retryPrompt.Index); });
            case GameState.Battle:
                return flow.Battle;
            case GameState.PartyMenu:
                return new Pointable(at => flow.PartyScreen.HitTest(at, flow.Party), i => flow.PartyScreen.PointAt(i, flow.Party));
            case GameState.BagMenu:
                return new Pointable(at => flow.BagScreen.HitTest(at, flow.Bag, flow.Party), i => flow.BagScreen.PointAt(i, flow.Bag, flow.Party), flow.BagScreen.Press);
            case GameState.Shop:
                return new Pointable(flow.ShopScreen.HitTest, flow.ShopScreen.PointAt);
            case GameState.PokedexMenu:
                return new Pointable(flow.PokedexScreen.HitTest, flow.PokedexScreen.PointAt);
            case GameState.PCStorage:
                return new Pointable(at => flow.PCScreen.HitTest(at, flow.Party.Count), i => flow.PCScreen.PointAt(i, flow.Party.Count),
                    i => flow.PCScreen.Press(i, flow.Party.Count));
            case GameState.Options:
                return new Pointable(flow.OptionsScreen.HitTest, flow.OptionsScreen.PointAt);
            case GameState.SaveMenu:
                return new Pointable(flow.SaveScreen.HitTest, flow.SaveScreen.PointAt);
            case GameState.FlyMap:
                return new Pointable(flow.TownMapScreen.HitTest, flow.TownMapScreen.PointAt);
            case GameState.Evolution:
                return new Pointable(flow.EvolutionScreen.HitTest, flow.EvolutionScreen.PointAt);
            default:
                return null;
        }
    }
}
