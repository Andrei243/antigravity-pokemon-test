using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Battle;

// The pointer in battle (plan 12 · Q7; style guide, "Shapes and components", "The pointer"): what lies under a point
// of the menu that is open, from the rectangles ModernUi draws the menus with, and the cursor brought onto it. A click
// is the menu's own Confirm (the engine presses it), so a whole battle is fought through the same choices as the keys.
public partial class BattleEngine : IPointable
{
    /// <summary>
    /// What lies under a point of the menu that is open (plan 12 · Q7): a command, a move card, a target card that can
    /// be aimed at (a greyed one is nothing), a card of the team, a pocket, an item, Yes or No; anywhere while a line is
    /// read or the information panel is up, where a click reads on; null for nothing.
    /// </summary>
    public int? PointAt(Vector2 at) => HitTest(at);

    /// <inheritdoc cref="PointAt(Vector2)"/>
    public int? HitTest(Vector2 at)
    {
        if (AutoRunning || Watching) return null;
        switch (HUD.MenuState)
        {
            case BattleMenuState.Main:
            {
                if (AutoOffered && UiPointer.Inside(ModernUi.AutoRect, at)) return AutoIndex;
                var (_, labels) = ModernUi.CommandLabels(this);
                if (UiPointer.Inside(ModernUi.CommandRect(0), at)) return 0;
                for (int i = 0; i < 3; i++)
                    if (labels[i].Length > 0 && UiPointer.Inside(ModernUi.CommandRect(i + 1), at)) return i + 1;
                return null;
            }
            case BattleMenuState.Moves:
                for (int i = 0; i < Fighter.Moves.Count && i < 4; i++)
                    if (UiPointer.Inside(ModernUi.MoveCardRect(i), at)) return i;
                return null;
            case BattleMenuState.SelectTarget:
            {
                var layout = TargetLayout;
                for (int i = 0; i < layout.Count; i++)
                    if (layout[i] is { } place && UiPointer.Inside(ModernUi.TargetCard(i, TargetColumns), at))
                        return targetChoices.IndexOf(place) is >= 0 and var choice ? choice : null;
                return null;
            }
            case BattleMenuState.SwitchPokemon or BattleMenuState.SelectBagTarget:
                for (int i = 0, count = HUD.MenuState == BattleMenuState.SwitchPokemon ? SwitchParty.Count : PlayerParty.Count; i < count; i++)
                    if (UiPointer.Inside(ModernUi.TeamCardRect(i), at)) return i;
                return null;
            case BattleMenuState.SelectBagPocket:
                for (int i = 0; i <= BattleBag.Pockets.Length; i++)
                    if (UiPointer.Inside(ModernUi.PocketButtonRect(i), at)) return i;
                return null;
            case BattleMenuState.SelectBagItem:
            {
                int first = HUD.BagFirstRow * BagColumns, listed = BagListed.Count;
                for (int slot = 0; slot < BagRows * BagColumns && first + slot < listed; slot++)
                    if (UiPointer.Inside(ModernUi.BagCardRect(slot), at)) return first + slot;
                return null;
            }
            case BattleMenuState.LearnMove when LearnStep == LearnStep.Choose:
                return Learner is { } learner
                    ? ModernUi.HitMoveChoice(new Raylib_cs.Rectangle(GameEngine.VirtualWidth - 64 - 820, 64, 820, 716), learner.Moves.Count, at)
                    : null;
            case BattleMenuState.LearnMove:
                return ModernUi.HitRow(ModernUi.ChoiceRows(GameEngine.VirtualWidth + 48,
                    GameEngine.VirtualHeight + 40 + (int)(DialogueBox.HeightNow - DialogueBox.Height),
                    new[] { Text.Of("battleMenu.yes"), Text.Of("battleMenu.no") }, LearnCursor), at);
            case BattleMenuState.Info or BattleMenuState.Message:
                return UiPointer.Anywhere;
            default:
                return null;
        }
    }

    /// <summary>Brings the open menu's cursor onto what the pointer is over, with the cursor's sound when it moves.</summary>
    void IPointable.PointAt(int index) => PointAtIndex(index);

    /// <summary>Brings the open menu's cursor onto an index <see cref="HitTest"/> gave.</summary>
    public void PointAtIndex(int index)
    {
        if (index == UiPointer.Anywhere) return;
        int before;
        switch (HUD.MenuState)
        {
            case BattleMenuState.Main:
                before = HUD.MainMenuIndex;
                HUD.MainMenuIndex = index;
                if (index is >= 1 and <= 3) stackRow = index;
                break;
            case BattleMenuState.Moves:
                before = HUD.MoveMenuIndex;
                HUD.MoveMenuIndex = index;
                break;
            case BattleMenuState.SelectTarget:
                before = HUD.TargetMenuIndex;
                HUD.TargetMenuIndex = index;
                break;
            case BattleMenuState.SwitchPokemon:
                before = HUD.SwitchMenuIndex;
                HUD.SwitchMenuIndex = index;
                break;
            case BattleMenuState.SelectBagTarget:
                before = HUD.BagTargetIndex;
                HUD.BagTargetIndex = index;
                break;
            case BattleMenuState.SelectBagPocket:
                before = HUD.BagPocketIndex;
                HUD.BagPocketIndex = index;
                break;
            case BattleMenuState.SelectBagItem:
                before = HUD.BagMenuIndex;
                HUD.BagMenuIndex = index;
                break;
            case BattleMenuState.LearnMove:
                before = LearnCursor;
                LearnCursor = index;
                break;
            default:
                return;
        }
        if (before != index) AudioManager.PlaySound("cursor");
    }
}
