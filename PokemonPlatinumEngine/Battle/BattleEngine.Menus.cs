using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The player's choices: one for each of their Pokémon on the field, then the rules play the turn. Every choice is
// a method that takes no input, so tests and the screenshot harness can drive battles; HandleMenuInput maps keys
// onto them. What a menu refuses (no PP, a Pokémon already out) it refuses here, before the rules are asked.
public partial class BattleEngine
{
    /// <summary>The items offered by the battle's BAG menu, in slot order.</summary>
    public static IReadOnlyList<string> BagItems { get; } = new[] { "Poké Ball", "Great Ball", "Potion", "Super Potion" };

    private int menuSlot;
    private List<int> asked = new();
    private readonly Dictionary<int, BattleChoice> choices = new();
    private Move? targetingMove;
    private List<Battler> targetChoices = new();

    /// <summary>A fainted Pokémon is being replaced (the switch menu can't be closed).</summary>
    private bool replacing;

    /// <summary>The row of the commands' stack (BAG, POKÉMON, RUN) the cursor was on last: where a step sideways from FIGHT goes.</summary>
    private int stackRow = 1;

    /// <summary>The move each of the player's Pokémon chose last in this battle: where its move menu opens.</summary>
    private readonly Dictionary<Pokemon, int> lastMove = new();

    /// <summary>The move whose target is being chosen.</summary>
    public Move? TargetingMove => targetingMove;

    /// <summary>The Pokémon a move can be aimed at while choosing a target, foes first.</summary>
    public IReadOnlyList<Battler> TargetChoices => targetChoices;

    /// <summary>
    /// The four places as the target menu lays them out, two to a row: the foes on top as the player sees them, the
    /// player's own under them. A place a single battle doesn't have is null.
    /// </summary>
    public IReadOnlyList<Battler?> TargetLayout => new[]
    {
        EnemySlots.ElementAtOrDefault(1), EnemySlots[0],
        PlayerSlots[0], PlayerSlots.ElementAtOrDefault(1)
    };

    /// <summary>The keys that answer a menu: the A button, and Enter, which reads a battle's messages on as well.</summary>
    private static bool ConfirmPressed =>
        InputManager.IsActionPressed(GameAction.Confirm) || Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Enter);

    /// <summary>The switch menu is open because a fainted Pokémon has to be replaced.</summary>
    public bool IsChoosingReplacement => replacing;

    /// <summary>Opens the menus for the first of the player's Pokémon that can act.</summary>
    private void BeginChoosing()
    {
        choices.Clear();
        targetingMove = null;
        // Only the Pokémon the rules ask about: one in the middle of a move has nothing to choose
        asked = core.Request is ActionRequest request ? request.Places.Where(p => p.Side == BattleSide.Player).Select(p => p.Slot).ToList() : new List<int>();
        menuSlot = asked.Count > 0 ? asked[0] : 0;
        HUD.MenuState = BattleMenuState.Main;
    }

    /// <summary>Stores the choice for the Pokémon whose menu is open and moves on to the next one, or hands the turn to the rules.</summary>
    private void Commit(BattleChoice choice)
    {
        choices[menuSlot] = choice;

        // Running is for the whole side: nobody else is asked
        if (choice.Kind == ChoiceKind.Run)
        {
            foreach (var other in PlayerSlots.Where(b => asked.Contains(b.Slot) && !choices.ContainsKey(b.Slot)))
                choices[other.Slot] = BattleChoice.Run(other.Place);
        }

        var next = PlayerSlots.FirstOrDefault(b => b.Slot > menuSlot && asked.Contains(b.Slot) && !choices.ContainsKey(b.Slot));
        if (next != null)
        {
            menuSlot = next.Slot;
            HUD.MenuState = BattleMenuState.Main;
            HUD.MainMenuIndex = 0;
            return;
        }

        Submit(PlayerSlots.Where(b => choices.ContainsKey(b.Slot)).Select(b => choices[b.Slot]).ToList());
    }

    /// <summary>Answers what the rules asked, and shows what they made of it.</summary>
    private void Submit(IReadOnlyList<BattleChoice> answer)
    {
        // While lines are still being shown the screen is behind the rules, and what it holds must never be
        // copied over theirs (a choice made early by a tool would otherwise undo a turn's worth of state)
        if (!playingTheLog) SyncToCore();
        core.Submit(answer);
        Play(core.TakeLog());
        Pump();
    }

    /// <summary>Goes back to the previous Pokémon's choice (double battles); returns false when there is none.</summary>
    public bool CancelChoice()
    {
        var previous = PlayerSlots.LastOrDefault(b => b.Slot < menuSlot && choices.ContainsKey(b.Slot));
        if (previous == null) return false;

        // An item set aside for that Pokémon goes back in the bag
        if (choices[previous.Slot] is { Kind: ChoiceKind.Item, Item: { } item } && ItemDatabase.Get(item) is { } data) PlayerInventory.AddItem(data, 1);
        choices.Remove(previous.Slot);
        menuSlot = previous.Slot;
        HUD.MenuState = BattleMenuState.Main;
        return true;
    }

    /// <summary>Acts on a choice from the main battle menu: 0 FIGHT, 1 BAG, 2 POKÉMON, 3 RUN.</summary>
    public void SelectMainMenuOption(int index)
    {
        switch (index)
        {
            case 0:
                if (core.UsableMoves(InCore(MenuBattler)).Count == 0)
                {
                    // Nothing left to use: it struggles
                    var place = MenuBattler.Place;
                    QueueMessage($"{MenuBattler.Name} has no moves left!", () => Commit(BattleChoice.Fight(place, -1)));
                    Pump();
                    return;
                }
                HUD.MenuState = BattleMenuState.Moves;
                // The cursor waits on the move this Pokémon chose last
                HUD.MoveMenuIndex = Math.Clamp(lastMove.GetValueOrDefault(MenuBattler.Pokemon!), 0, Math.Max(0, MenuBattler.Pokemon!.Moves.Count - 1));
                break;
            case 1:
                HUD.MenuState = BattleMenuState.SelectBagItem;
                HUD.BagMenuIndex = 0;
                break;
            case 2:
                HUD.MenuState = BattleMenuState.SwitchPokemon;
                HUD.SwitchMenuIndex = 0;
                break;
            case 3:
                AttemptRun();
                break;
        }
    }

    /// <summary>Uses the move in the given slot, as chosen from the FIGHT menu.</summary>
    public void SelectMove(int index)
    {
        var user = MenuBattler;
        if (index < 0 || index >= user.Pokemon!.Moves.Count) return;

        var move = user.Pokemon.Moves[index];

        // The rules say what can't be picked, and why: no PP, a Choice item, a Taunt, an Encore, a Disable…
        var mine = InCore(user);
        if (core.WhyNotMove(mine, mine.Pokemon!.Moves[index]) is { } refusal)
        {
            Refuse(refusal, BattleMenuState.Moves);
            return;
        }

        AudioManager.PlaySound("select");
        lastMove[user.Pokemon] = index;

        // In a double battle a single-target move asks where to aim it
        if (IsDouble && move.Target == MoveTarget.Selected)
        {
            // In the order the target menu lays them out: the foes left to right, then the partner
            targetChoices = EnemySlots.Reverse().Where(b => b.IsActive).Concat(PlayerSlots.Where(b => b.IsActive && b != user)).ToList();
            if (targetChoices.Count > 1)
            {
                targetingMove = move;
                HUD.TargetMenuIndex = 0;
                HUD.MenuState = BattleMenuState.SelectTarget;
                return;
            }
        }

        Commit(BattleChoice.Fight(user.Place, index));
    }

    /// <summary>Aims the move being chosen at <see cref="TargetChoices"/>[index].</summary>
    public void SelectTarget(int index)
    {
        if (targetingMove == null || index < 0 || index >= targetChoices.Count) return;
        int move = MenuBattler.Pokemon!.Moves.IndexOf(targetingMove);
        targetingMove = null;
        AudioManager.PlaySound("select");
        Commit(BattleChoice.Fight(MenuBattler.Place, move, targetChoices[index].Place));
    }

    /// <summary>Sends in the party's Pokémon at <paramref name="partyIndex"/>: a switch for this turn, or a fainted Pokémon's replacement.</summary>
    public void SelectSwitch(int partyIndex)
    {
        if (partyIndex < 0 || partyIndex >= PlayerParty.Count) return;
        var chosen = PlayerParty.Members[partyIndex];

        string? refusal = null;
        if (PlayerSlots.Any(b => b.Pokemon == chosen && !chosen.IsFainted)) refusal = $"{chosen.DisplayName} is already in battle!";
        else if (chosen.IsFainted) refusal = $"{chosen.DisplayName} has no energy left to battle!";
        else if (choices.Values.Any(a => a.Kind == ChoiceKind.Switch && a.SwitchTo == partyIndex)) refusal = $"{chosen.DisplayName} is already going in!";

        // Something may be holding the Pokémon on the field (a fainted one's place is free to fill)
        if (refusal == null && !replacing)
        {
            InCore(MenuBattler);
            refusal = core.WhyNot(BattleChoice.Switch(MenuBattler.Place, partyIndex));
        }

        if (refusal != null)
        {
            Refuse(refusal, BattleMenuState.SwitchPokemon);
            return;
        }

        AudioManager.PlaySound("select");
        var place = MenuBattler.Place;
        if (replacing)
        {
            replacing = false;
            Submit(new[] { BattleChoice.Switch(place, partyIndex) });
            return;
        }

        Commit(BattleChoice.Switch(place, partyIndex));
    }

    /// <summary>Uses the item in the given slot, as chosen from the BAG menu.</summary>
    public void SelectBagItem(int index)
    {
        if (index < 0 || index >= BagItems.Count) return;

        string itemName = BagItems[index];
        var itemData = ItemDatabase.Get(itemName);
        if (itemData == null) return;

        string? refusal = null;
        if (PlayerInventory.GetQuantity(itemData) <= 0) refusal = $"You don't have any {itemName}s left!";
        else if (itemData.Pocket == ItemPocket.PokeBalls && IsTrainerBattle) refusal = "The Trainer blocked the Ball! Don't be a thief!";
        else if (itemData.Pocket == ItemPocket.PokeBalls && EnemySlots.Count(b => b.IsActive) > 1) refusal = "There are two Pokémon out! The Ball can't be aimed!";
        else if (itemData.EffectType == ItemEffectType.HealHP && PlayerPokemon.CurrentHP >= PlayerPokemon.MaxHP) refusal = "It won't have any effect!";
        else
        {
            InCore(MenuBattler);
            refusal = core.WhyNot(BattleChoice.UseItem(MenuBattler.Place, itemData.Name));
        }

        if (refusal != null)
        {
            Refuse(refusal, BattleMenuState.SelectBagItem);
            return;
        }

        PlayerInventory.RemoveItem(itemData, 1);
        AudioManager.PlaySound("select");
        Commit(BattleChoice.UseItem(MenuBattler.Place, itemData.Name));
    }

    /// <summary>
    /// Uses an item for the Pokémon whose menu is open without going through the bag, which offers only a few and
    /// counts them: for tools that stage a battle (the harness throwing a ball of each kind).
    /// </summary>
    public void UseItem(ItemData item) => Commit(BattleChoice.UseItem(MenuBattler.Place, item.Name));

    private void AttemptRun()
    {
        if (IsTrainerBattle)
        {
            QueueMessage("No! There's no running from a Trainer battle!", () => HUD.MenuState = BattleMenuState.Main);
            Pump();
            return;
        }

        // Held on the field: the try isn't made, and the turn isn't spent
        InCore(MenuBattler);
        if (core.WhyNot(BattleChoice.Run(MenuBattler.Place)) is { } held)
        {
            Refuse(held, BattleMenuState.Main);
            return;
        }
        Commit(BattleChoice.Run(MenuBattler.Place));
    }

    /// <summary>Says why a choice can't be made and goes back to the menu it was made in.</summary>
    private void Refuse(string why, BattleMenuState backTo)
    {
        AudioManager.PlaySound("cancel");
        QueueMessage(why, () => HUD.MenuState = backTo);
        Pump();
    }

    /// <summary>
    /// Moves the cursor of the menu that is open one step, as the arrow keys do, and follows the way that menu is
    /// drawn. With both a step sideways and one up or down, a menu that isn't a plain grid takes the one up or down.
    /// </summary>
    public void MoveCursor(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;

        int before, after;
        switch (HUD.MenuState)
        {
            case BattleMenuState.Main:
                before = HUD.MainMenuIndex;
                after = HUD.MainMenuIndex = CommandStep(before, dx, dy);
                break;
            case BattleMenuState.Moves:
                // Two to a row; a Pokémon with fewer than four moves has no cursor on the empty places
                before = HUD.MoveMenuIndex;
                after = HUD.MoveMenuIndex = UI.Kit.UiNav.Grid(before, MenuBattler.Pokemon!.Moves.Count, 2, dx, dy);
                break;
            case BattleMenuState.SelectTarget:
                before = HUD.TargetMenuIndex;
                after = HUD.TargetMenuIndex = TargetStep(before, dx, dy);
                break;
            case BattleMenuState.SwitchPokemon:
                // The team is laid out three to a row
                before = HUD.SwitchMenuIndex;
                after = HUD.SwitchMenuIndex = UI.Kit.UiNav.Grid(before, PlayerParty.Count, 3, dx, dy);
                break;
            case BattleMenuState.SelectBagItem:
                before = HUD.BagMenuIndex;
                after = HUD.BagMenuIndex = UI.Kit.UiNav.Grid(before, BagItems.Count, 2, dx, dy);
                break;
            default:
                return;
        }
        if (after != before) AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// A step among the commands, which are drawn as FIGHT (0), tall on the left, with BAG, POKÉMON and RUN (1 to 3)
    /// stacked beside it. Up and down go through the four in that order and round again. Left and right cross
    /// between FIGHT and the stack, and come back into the stack on the row the cursor stood on last.
    /// </summary>
    private int CommandStep(int index, int dx, int dy)
    {
        if (index != 0) stackRow = index;
        if (dy != 0) return UI.Kit.UiNav.Wrap(index, Math.Sign(dy), 4);
        return index == 0 ? stackRow : 0;
    }

    /// <summary>
    /// A step among the targets, over the four cards as they lie (<see cref="TargetLayout"/>). A card that can't
    /// be aimed at (the Pokémon using the move, an empty place) is never stopped on: a step up or down that would
    /// land on one takes the card beside it, and a step sideways stays where it is.
    /// </summary>
    private int TargetStep(int index, int dx, int dy)
    {
        if (index < 0 || index >= targetChoices.Count) return 0;
        var layout = TargetLayout;
        int at = -1;
        for (int i = 0; i < layout.Count; i++) if (layout[i] == targetChoices[index]) at = i;
        if (at < 0) return index;

        int column = at % 2, row = at / 2;
        if (dy != 0) row = 1 - row;
        else column = 1 - column;

        var tries = dy != 0 ? new[] { row * 2 + column, row * 2 + 1 - column } : new[] { row * 2 + column };
        foreach (int card in tries)
        {
            int choice = layout[card] is { } there ? targetChoices.IndexOf(there) : -1;
            if (choice >= 0) return choice;
        }
        return index;
    }

    private void HandleMenuInput()
    {
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        bool confirm = ConfirmPressed, cancel = InputManager.IsActionPressed(GameAction.Cancel);

        switch (HUD.MenuState)
        {
            case BattleMenuState.Main:
                if (cancel && CancelChoice())
                {
                    AudioManager.PlaySound("cancel");
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm)
                {
                    AudioManager.PlaySound("select");
                    SelectMainMenuOption(HUD.MainMenuIndex);
                }
                break;

            case BattleMenuState.Moves:
                if (cancel)
                {
                    AudioManager.PlaySound("cancel");
                    HUD.MenuState = BattleMenuState.Main;
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm) SelectMove(HUD.MoveMenuIndex);
                break;

            case BattleMenuState.SelectTarget:
                if (cancel)
                {
                    AudioManager.PlaySound("cancel");
                    targetingMove = null;
                    HUD.MenuState = BattleMenuState.Moves;
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm) SelectTarget(HUD.TargetMenuIndex);
                break;

            case BattleMenuState.SwitchPokemon:
                if (cancel)
                {
                    if (!replacing)
                    {
                        AudioManager.PlaySound("cancel");
                        HUD.MenuState = BattleMenuState.Main;
                    }
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm) SelectSwitch(HUD.SwitchMenuIndex);
                break;

            case BattleMenuState.SelectBagItem:
                if (cancel)
                {
                    AudioManager.PlaySound("cancel");
                    HUD.MenuState = BattleMenuState.Main;
                    return;
                }
                MoveCursor(dx, dy);
                if (confirm) SelectBagItem(HUD.BagMenuIndex);
                break;
        }
    }
}
