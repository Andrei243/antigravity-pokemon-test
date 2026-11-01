using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The trainer tips (plan 12 · Q23; <see cref="Models.Tips"/>; style guide, "Field menus and notices", "A tip"): raised by a
/// battle's lines (<see cref="BattleEngine.TakeTips"/>), by the field's own steps (a ledge's hop, an evolution) and by a
/// script's <c>tip</c>, each shown once, at the next quiet moment: in the field with nothing under way, in a battle only as
/// its main menu opens, never while the log plays. While a tip is up nothing under it is updated, and A or B puts it away.
/// </summary>
public partial class GameEngine
{
    /// <summary>How long a tip is up before a press can put it away, so a press meant for the line before it isn't taken.</summary>
    public const float TipSettleSeconds = 0.25f;

    /// <summary>How long the card takes to rise into place.</summary>
    public const float TipRiseSeconds = 0.2f;

    /// <summary>Whether the options' TIPS row holds for the rules in force (by the rules: the modern rules show them).</summary>
    internal bool TipsOn => Settings.Tips.Holds(Ruleset.Current.Preset);

    /// <summary>A tip on screen now.</summary>
    public bool TipShown => flow.ShownTip != null;

    // A battle shows one tip each time its main menu opens: the next waits for the menu after
    private bool tipRested;

    /// <summary>
    /// Whether this engine shows tips at all: true for the game in its window, false for a game with no window
    /// (<see cref="InitializeHeadless"/>), whose tool or test presses nothing for them, unless it sets this again.
    /// </summary>
    public bool ShowsTips { get; set; } = true;

    /// <summary>Raises a tip, while the row holds: it waits for the next quiet moment, unless this game has shown it.</summary>
    internal void RaiseTip(string id)
    {
        if (TipsOn && ShowsTips) flow.Tips.Raise(id);
    }

    /// <summary>
    /// A frame in the field or a battle with a tip in mind: the tip up takes the frame (true) and A or B puts it away;
    /// otherwise a waiting tip comes up if the moment is quiet. False leaves the frame to the field or the battle.
    /// </summary>
    private bool UpdateTip(float dt)
    {
        if (flow.Battle is { } fight && flow.State == GameState.Battle)
        {
            foreach (string id in fight.TakeTips()) RaiseTip(id);
            if (fight.HUD.MenuState != BattleMenuState.Main) tipRested = false;
        }
        if (flow.ShownTip != null)
        {
            flow.ShownTipAge += dt;
            if (flow.ShownTipAge >= TipSettleSeconds
                && (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel)))
                PutTipAway();
            return true;
        }
        if (flow.Tips.Waiting.Count == 0 || !QuietForATip()) return false;
        flow.ShownTip = flow.Tips.TakeNext();
        flow.ShownTipAge = 0f;
        if (flow.State == GameState.Battle) tipRested = true;
        AudioManager.PlaySound("select");
        return true;
    }

    /// <summary>The tip up put away (A or B, or a tool).</summary>
    internal void PutTipAway()
    {
        if (flow.ShownTip == null) return;
        flow.ShownTip = null;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// A moment a tip may come up: the field the player's (where the game could be saved, the start menu shut, not mid-step),
    /// or a battle at its main menu with nothing being shown.
    /// </summary>
    private bool QuietForATip()
    {
        if (flow.State == GameState.Battle)
            return flow.Battle is { IsBattleOver: false, Watching: false } b && b.HUD.MenuState == BattleMenuState.Main && b.AtItsMenu && !tipRested;
        return CanSaveNow && !flow.StartMenu.IsActive && !flow.Player.IsMoving && !flow.Player.IsSliding && !flow.Dialogue.IsActive;
    }

    /// <summary>The tip up, over the field or the battle.</summary>
    private void DrawTip()
    {
        if (flow.ShownTip is not { } tip) return;
        float appear = TipRiseSeconds <= 0 ? 1f : System.Math.Clamp(flow.ShownTipAge / TipRiseSeconds, 0f, 1f);
        ModernUi.TipCard(VirtualWidth, VirtualHeight, tip.Title, tip.Body, appear);
    }

    private sealed partial class FieldHost
    {
        public void Tip(string id) => game.RaiseTip(id);
    }
}
