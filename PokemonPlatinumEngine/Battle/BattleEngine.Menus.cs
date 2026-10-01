using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The player's choices: one action per Pokémon on the field, then the turn runs. Every choice is a method that
// takes no input, so tests and the screenshot harness can drive battles; HandleMenuInput maps keys onto them.
public partial class BattleEngine
{
    /// <summary>The items offered by the battle's BAG menu, in slot order.</summary>
    public static IReadOnlyList<string> BagItems { get; } = new[] { "Poké Ball", "Great Ball", "Potion", "Super Potion" };

    private int menuSlot;
    private readonly Dictionary<int, BattleAction> choices = new();
    private Move? targetingMove;
    private List<Battler> targetChoices = new();

    /// <summary>A fainted Pokémon is being replaced (the switch menu can't be closed); runs once it is.</summary>
    private Action? afterReplacement;

    /// <summary>The move whose target is being chosen.</summary>
    public Move? TargetingMove => targetingMove;

    /// <summary>The Pokémon a move can be aimed at while choosing a target, foes first.</summary>
    public IReadOnlyList<Battler> TargetChoices => targetChoices;

    /// <summary>The switch menu is open because a fainted Pokémon has to be replaced.</summary>
    public bool IsChoosingReplacement => afterReplacement != null;

    /// <summary>Opens the menus for the first of the player's Pokémon that can act.</summary>
    private void StartTurn()
    {
        if (Result != BattleResult.None) return;

        choices.Clear();
        targetingMove = null;
        foreach (var foe in EnemySlots.Where(b => b.IsActive))
            foreach (var mine in PlayerSlots.Where(b => b.IsActive)) foe.FoughtAgainst.Add(mine.Pokemon!);

        menuSlot = PlayerSlots.FirstOrDefault(b => b.IsActive)?.Slot ?? 0;
        HUD.MenuState = BattleMenuState.Main;
    }

    /// <summary>Stores the action for the Pokémon whose menu is open and moves on to the next one, or starts the turn.</summary>
    private void Commit(BattleAction action)
    {
        action.User = MenuBattler;
        action.Actor = MenuBattler.Pokemon;
        action.IsPlayer = true;
        choices[menuSlot] = action;

        var next = PlayerSlots.FirstOrDefault(b => b.Slot > menuSlot && b.IsActive && !choices.ContainsKey(b.Slot));
        if (next != null)
        {
            menuSlot = next.Slot;
            HUD.MenuState = BattleMenuState.Main;
            HUD.MainMenuIndex = 0;
            return;
        }

        ExecuteTurn(PlayerSlots.Where(b => choices.ContainsKey(b.Slot)).Select(b => choices[b.Slot]).ToList());
    }

    /// <summary>Goes back to the previous Pokémon's choice (double battles); returns false when there is none.</summary>
    public bool CancelChoice()
    {
        var previous = PlayerSlots.LastOrDefault(b => b.Slot < menuSlot && choices.ContainsKey(b.Slot));
        if (previous == null) return false;

        // An item set aside for that Pokémon goes back in the bag
        if (choices[previous.Slot] is { Type: ActionType.UseItem, Item: { } item }) PlayerInventory.AddItem(item, 1);
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
                if (UsableMoves(MenuBattler).Count == 0)
                {
                    // Nothing left to use: it struggles
                    QueueMessage($"{MenuBattler.Name} has no moves left!", () => Commit(new BattleAction { Type = ActionType.Fight, Move = new Move(StruggleData) }));
                    AdvanceEventQueue();
                    return;
                }
                HUD.MenuState = BattleMenuState.Moves;
                HUD.MoveMenuIndex = 0;
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

    /// <summary>The moves a Pokémon can pick: any with PP left, or only its locked move while a Choice item holds it.</summary>
    private static List<Move> UsableMoves(Battler b)
    {
        var moves = b.Pokemon!.Moves.Where(m => m.CurrentPP > 0).ToList();
        if (b.ChoiceLock != null && moves.Contains(b.ChoiceLock)) return new List<Move> { b.ChoiceLock };
        return moves;
    }

    /// <summary>Uses the move in the given slot, as chosen from the FIGHT menu.</summary>
    public void SelectMove(int index)
    {
        var user = MenuBattler;
        if (index < 0 || index >= user.Pokemon!.Moves.Count) return;

        var move = user.Pokemon.Moves[index];
        if (move.CurrentPP <= 0)
        {
            AudioManager.PlaySound("cancel");
            QueueMessage("There's no PP left for this move!", () => HUD.MenuState = BattleMenuState.Moves);
            AdvanceEventQueue();
            return;
        }
        if (user.ChoiceLock != null && user.ChoiceLock != move && user.ChoiceLock.CurrentPP > 0)
        {
            AudioManager.PlaySound("cancel");
            QueueMessage($"{user.Name} can only use {user.ChoiceLock.Name}!", () => HUD.MenuState = BattleMenuState.Moves);
            AdvanceEventQueue();
            return;
        }

        AudioManager.PlaySound("select");

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

        Commit(new BattleAction { Type = ActionType.Fight, Move = move });
    }

    /// <summary>Aims the move being chosen at <see cref="TargetChoices"/>[index].</summary>
    public void SelectTarget(int index)
    {
        if (targetingMove == null || index < 0 || index >= targetChoices.Count) return;
        var move = targetingMove;
        targetingMove = null;
        AudioManager.PlaySound("select");
        Commit(new BattleAction { Type = ActionType.Fight, Move = move, Target = targetChoices[index] });
    }

    /// <summary>Sends in the party's Pokémon at <paramref name="partyIndex"/>: a switch for this turn, or a fainted Pokémon's replacement.</summary>
    public void SelectSwitch(int partyIndex)
    {
        if (partyIndex < 0 || partyIndex >= PlayerParty.Count) return;
        var chosen = PlayerParty.Members[partyIndex];

        string? refusal = null;
        if (PlayerSlots.Any(b => b.Pokemon == chosen && !chosen.IsFainted)) refusal = $"{chosen.DisplayName} is already in battle!";
        else if (chosen.IsFainted) refusal = $"{chosen.DisplayName} has no energy left to battle!";
        else if (choices.Values.Any(a => a.Type == ActionType.Switch && a.SwitchToIndex == partyIndex)) refusal = $"{chosen.DisplayName} is already going in!";

        if (refusal != null)
        {
            AudioManager.PlaySound("cancel");
            QueueMessage(refusal, () => HUD.MenuState = BattleMenuState.SwitchPokemon);
            AdvanceEventQueue();
            return;
        }

        AudioManager.PlaySound("select");
        if (afterReplacement != null)
        {
            var then = afterReplacement;
            afterReplacement = null;
            var place = MenuBattler;
            QueueMessage($"Go! {chosen.DisplayName}!", () => then(), onShow: () =>
            {
                SendIn(place, chosen);
                Anim.SendOut(BattleSide.Player, chosen, place.Slot);
            });
            AdvanceEventQueue();
            return;
        }

        Commit(new BattleAction { Type = ActionType.Switch, SwitchToIndex = partyIndex });
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

        if (refusal != null)
        {
            AudioManager.PlaySound("cancel");
            QueueMessage(refusal, () => HUD.MenuState = BattleMenuState.SelectBagItem);
            AdvanceEventQueue();
            return;
        }

        PlayerInventory.RemoveItem(itemData, 1);
        AudioManager.PlaySound("select");
        Commit(new BattleAction { Type = ActionType.UseItem, Item = itemData });
    }

    private void AttemptRun()
    {
        if (IsTrainerBattle)
        {
            QueueMessage("No! There's no running from a Trainer battle!", () => HUD.MenuState = BattleMenuState.Main);
        }
        else
        {
            QueueMessage("Got away safely!", () => Result = BattleResult.PlayerRan);
        }

        // Show the message right away; queued messages otherwise wait for the next turn
        AdvanceEventQueue();
    }

    private void HandleMenuInput()
    {
        bool up = InputManager.IsActionPressed(GameAction.Up), down = InputManager.IsActionPressed(GameAction.Down);
        bool left = InputManager.IsActionPressed(GameAction.Left), right = InputManager.IsActionPressed(GameAction.Right);
        bool confirm = InputManager.IsActionPressed(GameAction.Confirm), cancel = InputManager.IsActionPressed(GameAction.Cancel);

        // The 2×2 menus (commands, moves, bag)
        int Grid(int index)
        {
            if (up || down) index = (index + 2) % 4;
            if (left || right) index = index % 2 == 0 ? index + 1 : index - 1;
            if (up || down || left || right) AudioManager.PlaySound("cursor");
            return index;
        }

        switch (HUD.MenuState)
        {
            case BattleMenuState.Main:
                if (cancel && CancelChoice())
                {
                    AudioManager.PlaySound("cancel");
                    return;
                }
                HUD.MainMenuIndex = Grid(HUD.MainMenuIndex);
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
                HUD.MoveMenuIndex = Grid(HUD.MoveMenuIndex);
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
                if (left || right || up || down)
                {
                    int step = right || down ? 1 : -1;
                    HUD.TargetMenuIndex = (HUD.TargetMenuIndex + step + targetChoices.Count) % Math.Max(1, targetChoices.Count);
                    AudioManager.PlaySound("cursor");
                }
                if (confirm) SelectTarget(HUD.TargetMenuIndex);
                break;

            case BattleMenuState.SwitchPokemon:
                if (cancel)
                {
                    if (afterReplacement == null)
                    {
                        AudioManager.PlaySound("cancel");
                        HUD.MenuState = BattleMenuState.Main;
                    }
                    return;
                }
                // The team is laid out three to a row
                int switchDx = (right ? 1 : 0) - (left ? 1 : 0);
                int switchDy = (down ? 1 : 0) - (up ? 1 : 0);
                if (switchDx != 0 || switchDy != 0)
                {
                    int next = UI.Kit.UiNav.Grid(HUD.SwitchMenuIndex, PlayerParty.Count, 3, switchDx, switchDy);
                    if (next != HUD.SwitchMenuIndex) AudioManager.PlaySound("cursor");
                    HUD.SwitchMenuIndex = next;
                }
                if (confirm) SelectSwitch(HUD.SwitchMenuIndex);
                break;

            case BattleMenuState.SelectBagItem:
                if (cancel)
                {
                    AudioManager.PlaySound("cancel");
                    HUD.MenuState = BattleMenuState.Main;
                    return;
                }
                HUD.BagMenuIndex = Grid(HUD.BagMenuIndex);
                if (confirm) SelectBagItem(HUD.BagMenuIndex);
                break;
        }
    }
}
