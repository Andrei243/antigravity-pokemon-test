using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What can be done with the item under the cursor.</summary>
public enum BagAction { Use, Give, Cancel }

/// <summary>
/// The bag: Platinum's eight pockets as tabs, the pocket's items as a list and the chosen item beside it. The A
/// button opens what can be done with an item (USE, GIVE, CANCEL); using or giving goes on to the party to
/// pick a Pokémon. Its logic takes no input (<see cref="MovePocket"/>, <see cref="MoveCursor"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it. Up or down kept down runs on
/// through a pocket's list (<see cref="HeldKey"/>).
/// </summary>
public class BagScreen
{
    private const float AppearTime = 0.3f, ChoiceAppearTime = 0.35f;

    /// <summary>How many rows of a pocket show at once.</summary>
    public const int VisibleRows = 8;

    /// <summary>Platinum's pockets, in its order.</summary>
    public static readonly ItemPocket[] Pockets =
    {
        ItemPocket.Items, ItemPocket.Medicine, ItemPocket.PokeBalls, ItemPocket.TMsAndHMs,
        ItemPocket.Berries, ItemPocket.Mail, ItemPocket.BattleItems, ItemPocket.KeyItems
    };

    // Each pocket remembers where its cursor was, as in the games
    private readonly int[] cursors = new int[Pockets.Length], firsts = new int[Pockets.Length];
    private readonly HeldKey upDown = new();
    private readonly CursorTick tick = new();
    private float openAge;

    public ItemPocket CurrentPocket { get; set; } = ItemPocket.Items;
    public bool IsActive { get; set; } = false;

    private int PocketIndex => Math.Max(0, Array.IndexOf(Pockets, CurrentPocket));

    public int SelectedIndex
    {
        get => cursors[PocketIndex];
        set => cursors[PocketIndex] = value;
    }

    /// <summary>The first row of the pocket's list that shows: it follows the cursor (<see cref="UiNav.Window"/>).</summary>
    public int FirstRow => firsts[PocketIndex];

    /// <summary>What can be done with the chosen item, while its menu is up (null otherwise), and the cursor in it.</summary>
    public IReadOnlyList<BagAction>? Actions { get; private set; }
    public int ActionIndex { get; private set; }

    // An item that goes to one Pokémon of the player's choosing waits here while they pick
    private ItemData? choosingFor;
    private bool giving;
    private float choiceAge;
    private EvolutionRequest? request;

    /// <summary>The Pokémon the cursor is on while an item waits for its target.</summary>
    public int TargetIndex { get; private set; }

    /// <summary>The item waiting for the player to pick a Pokémon (null: the bag itself is showing).</summary>
    public ItemData? ChoosingFor => choosingFor;

    /// <summary>Whether the waiting item is to be given to hold rather than used.</summary>
    public bool Giving => giving;

    /// <summary>Hands over the evolution an item has just set off, once.</summary>
    public EvolutionRequest? TakeEvolution()
    {
        var taken = request;
        request = null;
        return taken;
    }

    public void Open()
    {
        IsActive = true;
        CurrentPocket = ItemPocket.Items;
        Array.Clear(cursors);
        Array.Clear(firsts);
        Actions = null;
        choosingFor = null;
        openAge = 0f;
        upDown.Release();
    }

    public void Close()
    {
        IsActive = false;
        Actions = null;
        choosingFor = null;
    }

    // ---------------------------------------------------------------- moving about

    /// <summary>To the next or the previous pocket, wrapping round; each keeps its own cursor.</summary>
    public void MovePocket(int step)
    {
        if (step == 0 || Actions != null) return;
        CurrentPocket = Pockets[UiNav.Wrap(PocketIndex, step, Pockets.Length)];
        AudioManager.PlaySound("page");
    }

    /// <summary>
    /// Up or down the pocket's list (or the item's actions while they are up), wrapping round. In the list a step
    /// that comes from a key kept down (<paramref name="held"/>) stops at either end instead.
    /// </summary>
    public void MoveCursor(int step, int count, bool held = false)
    {
        if (step == 0) return;
        if (Actions != null)
        {
            ActionIndex = UiNav.Wrap(ActionIndex, step, Actions.Count);
            AudioManager.PlaySound("cursor");
            return;
        }
        if (count <= 0) return;
        int from = Math.Min(SelectedIndex, count - 1);
        int next = held ? Math.Clamp(from + step, 0, count - 1) : UiNav.Wrap(from, step, count);
        if (held && next == from) return;
        SelectedIndex = next;
        Follow(count);
        if (tick.Sounds(held)) AudioManager.PlaySound("cursor");
    }

    /// <summary>Keeps the cursor inside the pocket and the list's window on the cursor.</summary>
    internal void Follow(int count)
    {
        SelectedIndex = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));
        firsts[PocketIndex] = UiNav.Window(firsts[PocketIndex], SelectedIndex, count, VisibleRows);
    }

    // ---------------------------------------------------------------- what an item can do

    /// <summary>Items used on one Pokémon: medicine, Rare Candies and what evolves a Pokémon.</summary>
    public static bool CanUse(ItemData item) =>
        FieldItems.IsMedicine(item) || item.EffectType == ItemEffectType.LevelUp || Evolution.IsUsedToEvolve(item);

    /// <summary>As in Platinum, a Pokémon can hold anything but a Key Item or a TM.</summary>
    public static bool CanGive(ItemData item) => item.Pocket is not (ItemPocket.KeyItems or ItemPocket.TMsAndHMs);

    /// <summary>Items the player aims at one Pokémon, to use or to give.</summary>
    public static bool NeedsTarget(ItemData item) => CanUse(item) || CanGive(item);

    /// <summary>What the A button offers for an item, CANCEL last.</summary>
    public static List<BagAction> ActionsFor(ItemData item)
    {
        var actions = new List<BagAction>();
        if (CanUse(item)) actions.Add(BagAction.Use);
        if (CanGive(item)) actions.Add(BagAction.Give);
        actions.Add(BagAction.Cancel);
        return actions;
    }

    /// <summary>
    /// The A button: on an item, opens what can be done with it; on one of those, does it. Using and giving
    /// both go on to the party to pick a Pokémon.
    /// </summary>
    public void Confirm(Inventory inventory, Party party, Action<string> onNotification)
    {
        var items = inventory.GetPocketItems(CurrentPocket);
        if (items.Count == 0) return;
        Follow(items.Count);
        var item = items[SelectedIndex].Data;

        if (Actions == null)
        {
            var actions = ActionsFor(item);
            if (actions.Count == 1)
            {
                onNotification("It can't be used here.");
                AudioManager.PlaySound("error");
                return;
            }
            Actions = actions;
            ActionIndex = 0;
            AudioManager.PlaySound("select");
            return;
        }

        var action = Actions[Math.Clamp(ActionIndex, 0, Actions.Count - 1)];
        Actions = null;
        if (action == BagAction.Cancel)
        {
            AudioManager.PlaySound("cancel");
            return;
        }
        if (party.Count == 0)
        {
            onNotification("There is no Pokémon to give it to.");
            return;
        }
        BeginTargetChoice(item, give: action == BagAction.Give);
    }

    /// <summary>The B button: out of the item's actions, then out of the bag.</summary>
    public void Cancel()
    {
        if (Actions != null) Actions = null;
        else Close();
        AudioManager.PlaySound("cancel");
    }

    /// <param name="context">What evolutions need to know (the hour, the map); just the party and the bag when left out.</param>
    public void Update(Inventory inventory, Party party, Action<string> onNotification, EvolutionContext? context = null, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        tick.Update(dt);

        // Up or down kept down runs on through a pocket's list; an item's actions and the party take presses only
        bool list = choosingFor == null && Actions == null;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down),
            list ? InputManager.Axis(GameAction.Up, GameAction.Down, held: true) : 0);

        if (choosingFor != null)
        {
            choiceAge += dt;
            if (dx != 0 || dy != 0) MoveTarget(dx, dy, party.Count);
            else if (InputManager.IsActionPressed(GameAction.Cancel)) CancelTarget();
            else if (InputManager.IsActionPressed(GameAction.Confirm))
                UseOnTarget(inventory, party, onNotification, context ?? new EvolutionContext { Party = party, Bag = inventory });
            return;
        }

        if (dx != 0) MovePocket(dx);
        else if (dy != 0)
        {
            int count = inventory.GetPocketItems(CurrentPocket).Count;
            for (int i = 0; i < Math.Abs(dy); i++) MoveCursor(Math.Sign(dy), count, upDown.Repeating);
        }

        // A button pressed while the list runs on still counts: it acts on the item the cursor has come to
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm(inventory, party, onNotification);
    }

    // ---------------------------------------------------------------- items used on a Pokémon of the player's choosing

    /// <summary>Goes on to the party with an item: to use it if it can be used, to give it otherwise.</summary>
    public void BeginTargetChoice(ItemData item) => BeginTargetChoice(item, give: !CanUse(item));

    public void BeginTargetChoice(ItemData item, bool give)
    {
        choosingFor = item;
        giving = give;
        TargetIndex = 0;
        choiceAge = 0f;
        AudioManager.PlaySound("select");
    }

    public void MoveTarget(int dx, int dy, int count)
    {
        int next = UiNav.Grid(TargetIndex, count, 2, dx, dy);
        if (next == TargetIndex) return;
        TargetIndex = next;
        AudioManager.PlaySound("cursor");
    }

    public void CancelTarget()
    {
        choosingFor = null;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>
    /// Uses the waiting item on the Pokémon under the cursor, or gives it to hold. An evolution it sets off is
    /// left for <see cref="TakeEvolution"/>: one from a Rare Candy's level can be stopped, one from a stone can't.
    /// Something that would have no effect stays in the bag and the choice stays open.
    /// </summary>
    public void UseOnTarget(Inventory inventory, Party party, Action<string> onNotification, EvolutionContext context)
    {
        if (choosingFor == null || TargetIndex >= party.Count) return;
        var item = choosingFor;
        var target = party.Members[TargetIndex];

        if (giving)
        {
            GiveToHold(item, inventory, target, onNotification);
        }
        else if (item.EffectType == ItemEffectType.LevelUp)
        {
            if (target.Level >= 100)
            {
                onNotification("It won't have any effect.");
                return;
            }
            // A Rare Candy also brings a fainted Pokémon round, with the hit points the level gave it
            bool wasFainted = target.IsFainted;
            target.GainExp(target.ExpForNextLevel - target.CurrentExp, out _);
            if (wasFainted) target.Revive(target.CurrentHP);
            inventory.RemoveItem(item, 1);
            AudioManager.PlayFanfare(MusicRole.FanfareLevelUp);
            onNotification($"{target.DisplayName} grew to Lv. {target.Level}!");
            context.Item = null;
            if (Evolution.Find(target, EvolutionTrigger.LevelUp, context) is { } grown)
                request = new EvolutionRequest(target, grown, Cancellable: true);
        }
        else if (Evolution.IsUsedToEvolve(item))
        {
            context.Item = item;
            var evolution = Evolution.Find(target, EvolutionTrigger.UseItem, context);
            if (evolution == null)
            {
                onNotification("It won't have any effect.");
                return;
            }
            inventory.RemoveItem(item, 1);
            request = new EvolutionRequest(target, evolution, Cancellable: false);
        }
        else if (FieldItems.IsMedicine(item))
        {
            if (FieldItems.Use(item, target) is not { } done)
            {
                onNotification("It won't have any effect.");
                return;
            }
            inventory.RemoveItem(item, 1);
            AudioManager.PlaySound("heal");
            onNotification(done);
            // While there is more of it, the party stays up for the next Pokémon, as in the games
            if (inventory.GetQuantity(item) > 0) return;
        }
        else
        {
            GiveToHold(item, inventory, target, onNotification);
        }
        choosingFor = null;
    }

    /// <summary>Gives an item to a Pokémon to hold; whatever it held goes back in the bag.</summary>
    public static void GiveToHold(ItemData item, Inventory inventory, Pokemon holder, Action<string> onNotification)
    {
        inventory.RemoveItem(item, 1);
        var previous = holder.HeldItem;
        holder.HeldItem = item;
        AudioManager.PlaySound("select");
        if (previous != null)
        {
            inventory.AddItem(previous, 1);
            onNotification($"{holder.DisplayName} swapped its {previous.Name} for the {item.Name}.");
        }
        else onNotification($"{holder.DisplayName} was given the {item.Name} to hold.");
    }

    /// <summary>Whether the waiting item would do anything for a Pokémon: ABLE or NOT ABLE on its card (null: nothing to say).</summary>
    public bool? WouldWorkOn(Pokemon p, EvolutionContext probe)
    {
        if (choosingFor is not { } item || giving) return null;
        if (item.EffectType == ItemEffectType.LevelUp) return p.Level < 100;
        if (Evolution.IsUsedToEvolve(item))
        {
            probe.Item = item;
            return Evolution.Find(p, EvolutionTrigger.UseItem, probe) != null;
        }
        return FieldItems.IsMedicine(item) ? FieldItems.WouldHelp(item, p) : null;
    }

    public void Draw(int screenWidth, int screenHeight, Inventory inventory, Party party, EvolutionContext? context = null)
    {
        if (!IsActive) return;

        if (choosingFor != null)
        {
            // An item used on a Pokémon says who it would work on, as the games do
            var item = choosingFor;
            var probe = context ?? new EvolutionContext { Party = party, Bag = inventory };
            string prompt = giving ? $"Give the {item.Name} to which Pokémon?" : $"Use the {item.Name} on which Pokémon?";
            ModernUi.DrawPartyChoice(screenWidth, screenHeight, party, TargetIndex, prompt, p => WouldWorkOn(p, probe),
                Math.Clamp(choiceAge / ChoiceAppearTime, 0f, 1f), giving ? "Give" : "Use");
            return;
        }

        Follow(inventory.GetPocketItems(CurrentPocket).Count);
        ModernUi.DrawBag(screenWidth, screenHeight, this, inventory, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
