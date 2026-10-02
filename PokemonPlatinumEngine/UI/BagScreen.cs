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

public class BagScreen
{
    private const float ChoiceAppearTime = 0.35f;

    public ItemPocket CurrentPocket { get; set; } = ItemPocket.Items;
    public int SelectedIndex { get; set; } = 0;
    public bool IsActive { get; set; } = false;

    // An item that goes to one Pokémon of the player's choosing waits here while they pick
    private ItemData? choosingFor;
    private float choiceAge;
    private EvolutionRequest? request;

    /// <summary>The Pokémon the cursor is on while an item waits for its target.</summary>
    public int TargetIndex { get; private set; }

    /// <summary>The item waiting for the player to pick a Pokémon (null: the bag itself is showing).</summary>
    public ItemData? ChoosingFor => choosingFor;

    /// <summary>Hands over the evolution an item has just set off, once.</summary>
    public EvolutionRequest? TakeEvolution()
    {
        var taken = request;
        request = null;
        return taken;
    }

    private readonly ItemPocket[] pockets =
    {
        ItemPocket.Items,
        ItemPocket.Medicine,
        ItemPocket.PokeBalls,
        ItemPocket.TMsAndHMs,
        ItemPocket.Berries,
        ItemPocket.KeyItems
    };

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        CurrentPocket = ItemPocket.Items;
        choosingFor = null;
    }

    public void Close()
    {
        IsActive = false;
        choosingFor = null;
    }

    /// <param name="context">What evolutions need to know (the hour, the map); just the party and the bag when left out.</param>
    public void Update(Inventory inventory, Party party, Action<string> onNotification, EvolutionContext? context = null, float dt = 1f / 60f)
    {
        if (!IsActive) return;

        if (choosingFor != null)
        {
            choiceAge += dt;
            int dx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
            int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
            if (dx != 0 || dy != 0) MoveTarget(dx, dy, party.Count);
            else if (InputManager.IsActionPressed(GameAction.Cancel)) CancelTarget();
            else if (InputManager.IsActionPressed(GameAction.Confirm))
                UseOnTarget(inventory, party, onNotification, context ?? new EvolutionContext { Party = party, Bag = inventory });
            return;
        }

        var items = inventory.GetPocketItems(CurrentPocket);

        if (InputManager.IsActionPressed(GameAction.Left))
        {
            int pIdx = Array.IndexOf(pockets, CurrentPocket);
            CurrentPocket = pockets[(pIdx - 1 + pockets.Length) % pockets.Length];
            SelectedIndex = 0;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Right))
        {
            int pIdx = Array.IndexOf(pockets, CurrentPocket);
            CurrentPocket = pockets[(pIdx + 1) % pockets.Length];
            SelectedIndex = 0;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Up))
        {
            if (items.Count > 0)
            {
                SelectedIndex = (SelectedIndex - 1 + items.Count) % items.Count;
                AudioManager.PlaySound("cursor");
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Down))
        {
            if (items.Count > 0)
            {
                SelectedIndex = (SelectedIndex + 1) % items.Count;
                AudioManager.PlaySound("cursor");
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            if (items.Count > 0 && SelectedIndex < items.Count)
            {
                var itemStack = items[SelectedIndex];
                UseItem(itemStack, inventory, party, onNotification);
            }
        }
    }

    // ---------------------------------------------------------------- items used on a Pokémon of the player's choosing

    /// <summary>Items the player aims at one Pokémon: Rare Candies, what evolves a Pokémon, and anything given to hold.</summary>
    public static bool NeedsTarget(ItemData item) =>
        item.EffectType == ItemEffectType.LevelUp || Evolution.IsUsedToEvolve(item) || IsGivenToHold(item);

    private static bool IsGivenToHold(ItemData item) =>
        PokemonPlatinumEngine.Battle.Effects.HeldItemEffects.IsHoldable(item) || Evolution.IsHeldForEvolution(item);

    public void BeginTargetChoice(ItemData item)
    {
        choosingFor = item;
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
    /// Uses the waiting item on the Pokémon under the cursor. An evolution it sets off is left for
    /// <see cref="TakeEvolution"/>: one from a Rare Candy's level can be stopped, one from a stone can't.
    /// </summary>
    public void UseOnTarget(Inventory inventory, Party party, Action<string> onNotification, EvolutionContext context)
    {
        if (choosingFor == null || TargetIndex >= party.Count) return;
        var item = choosingFor;
        var target = party.Members[TargetIndex];

        if (item.EffectType == ItemEffectType.LevelUp)
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
        else
        {
            GiveToHold(item, inventory, target, onNotification);
        }
        choosingFor = null;
    }

    private void UseItem(ItemStack stack, Inventory inventory, Party party, Action<string> onNotification)
    {
        var item = stack.Data;
        if (NeedsTarget(item))
        {
            if (party.Count > 0) BeginTargetChoice(item);
        }
        else if (item.EffectType == ItemEffectType.HealHP)
        {
            var lead = party.FirstUsable;
            if (lead != null && lead.CurrentHP < lead.MaxHP)
            {
                lead.CurrentHP = Math.Min(lead.MaxHP, lead.CurrentHP + item.EffectValue);
                inventory.RemoveItem(item, 1);
                AudioManager.PlaySound("heal");
                onNotification($"{item.Name} restored {lead.DisplayName}'s HP!");
            }
            else
            {
                onNotification("It won't have any effect.");
            }
        }
        else if (item.EffectType == ItemEffectType.Revive)
        {
            var fainted = party.Members.FirstOrDefault(p => p.IsFainted);
            if (fainted != null)
            {
                fainted.Revive(fainted.MaxHP / 2);
                inventory.RemoveItem(item, 1);
                AudioManager.PlaySound("heal");
                onNotification($"{item.Name} revived {fainted.DisplayName}!");
            }
            else
            {
                onNotification("It won't have any effect.");
            }
        }
        else if (item.EffectType == ItemEffectType.FullRestore)
        {
            var lead = party.FirstUsable;
            if (lead != null && (lead.CurrentHP < lead.MaxHP || lead.Status != StatusCondition.None))
            {
                lead.HealFull();
                inventory.RemoveItem(item, 1);
                AudioManager.PlaySound("heal");
                onNotification($"{item.Name} restored {lead.DisplayName}'s HP and condition!");
            }
            else
            {
                onNotification("It won't have any effect.");
            }
        }
        else
        {
            onNotification($"You used the {item.Name}!");
        }
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

    public void Draw(int screenWidth, int screenHeight, Inventory inventory, Party party, EvolutionContext? context = null)
    {
        if (!IsActive) return;

        if (choosingFor != null)
        {
            // An item that evolves Pokémon says who it would work on, as the games do
            var item = choosingFor;
            var probe = context ?? new EvolutionContext { Party = party, Bag = inventory };
            probe.Item = item;
            bool evolves = Evolution.IsUsedToEvolve(item);
            string prompt = item.EffectType == ItemEffectType.LevelUp || evolves
                ? $"Use the {item.Name} on which Pokémon?"
                : $"Give the {item.Name} to which Pokémon?";
            ModernUi.DrawPartyChoice(screenWidth, screenHeight, party, TargetIndex, prompt,
                p => evolves ? Evolution.Find(p, EvolutionTrigger.UseItem, probe) != null : null,
                Math.Clamp(choiceAge / ChoiceAppearTime, 0f, 1f));
            return;
        }

        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, Palette.UiBackground);

        // Header / Pocket Tabs
        int headerHeight = 64;
        RenderHelper.DrawPlatinumPanel(24, 20, screenWidth - 48, headerHeight, Palette.UiPanelBg);
        int tabWidth = (screenWidth - 56) / pockets.Length;

        for (int i = 0; i < pockets.Length; i++)
        {
            var p = pockets[i];
            int tx = 28 + i * tabWidth;
            bool isCurrent = p == CurrentPocket;

            if (isCurrent)
            {
                Raylib.DrawRectangle(tx, 26, tabWidth - 6, headerHeight - 12, Palette.UiAccent);
                RenderHelper.DrawTextWithShadow(p.ToString().ToUpperInvariant(), tx + 16, 38, 22, Color.White);
            }
            else
            {
                RenderHelper.DrawTextWithShadow(p.ToString().ToUpperInvariant(), tx + 16, 38, 22, Palette.TextDark);
            }
        }

        // Items List Panel (Left)
        int listWidth = 840;
        int listHeight = screenHeight - 116;
        RenderHelper.DrawPlatinumPanel(24, 96, listWidth, listHeight, Palette.UiPanelBg);

        var items = inventory.GetPocketItems(CurrentPocket);
        if (items.Count == 0)
        {
            RenderHelper.DrawTextWithShadow("No items in this pocket.", 60, 140, 24, Color.Gray);
        }
        else
        {
            int maxVisible = 14;
            int scrollOffset = Math.Max(0, Math.Min(SelectedIndex - maxVisible / 2, Math.Max(0, items.Count - maxVisible)));

            for (int i = 0; i < maxVisible && (i + scrollOffset) < items.Count; i++)
            {
                int itemIdx = i + scrollOffset;
                var it = items[itemIdx];
                int iy = 112 + i * 58;
                bool isSelected = SelectedIndex == itemIdx;

                if (isSelected)
                {
                    Raylib.DrawRectangle(38, iy, listWidth - 28, 50, Palette.UiAccent);
                    RenderHelper.DrawTextWithShadow(it.Name, 60, iy + 12, 24, Color.White);
                    RenderHelper.DrawTextWithShadow($"x{it.Quantity}", listWidth - 90, iy + 12, 24, Color.White);
                }
                else
                {
                    RenderHelper.DrawTextWithShadow(it.Name, 52, iy + 12, 24, Palette.TextDark);
                    RenderHelper.DrawTextWithShadow($"x{it.Quantity}", listWidth - 96, iy + 12, 24, Palette.TextDark);
                }
            }
        }

        // Details Panel (Right)
        int descX = 24 + listWidth + 24;
        int descWidth = screenWidth - descX - 24;
        RenderHelper.DrawPlatinumPanel(descX, 96, descWidth, listHeight, Palette.UiPanelBg);

        if (items.Count > 0 && SelectedIndex < items.Count)
        {
            var selItem = items[SelectedIndex].Data;
            RenderHelper.DrawTextWithShadow(selItem.Name, descX + 36, 128, 36, Palette.UiAccent);
            RenderHelper.DrawTextWithShadow($"Category: {selItem.Pocket}", descX + 36, 178, 22, Palette.TextDark);

            RenderHelper.DrawPlatinumPanel(descX + 32, 224, descWidth - 64, 420, Palette.UiBackground);
            RenderHelper.DrawTextWithShadow(selItem.Description, descX + 54, 256, 26, Palette.TextDark);

            RenderHelper.DrawTextWithShadow("Press Z / Space to Use   |   X / Esc: Back", descX + 36, 96 + listHeight - 48, 22, Color.Gray);
        }
    }
}
