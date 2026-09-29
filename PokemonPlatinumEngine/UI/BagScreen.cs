using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public class BagScreen
{
    public ItemPocket CurrentPocket { get; set; } = ItemPocket.Items;
    public int SelectedIndex { get; set; } = 0;
    public bool IsActive { get; set; } = false;

    private readonly ItemPocket[] pockets =
    {
        ItemPocket.Items,
        ItemPocket.Medicine,
        ItemPocket.PokeBalls,
        ItemPocket.TMsAndHMs,
        ItemPocket.KeyItems
    };

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        CurrentPocket = ItemPocket.Items;
    }

    public void Close()
    {
        IsActive = false;
    }

    public void Update(Inventory inventory, Party party, Action<string> onNotification)
    {
        if (!IsActive) return;

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

    private void UseItem(ItemStack stack, Inventory inventory, Party party, Action<string> onNotification)
    {
        var item = stack.Data;
        if (item.EffectType == ItemEffectType.HealHP)
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
        else if (item.EffectType == ItemEffectType.LevelUp)
        {
            var lead = party.FirstUsable;
            if (lead != null && lead.Level < 100)
            {
                lead.GainExp(lead.ExpForNextLevel - lead.CurrentExp, out var moves, out bool evolved, out string oldName);
                inventory.RemoveItem(item, 1);
                AudioManager.PlaySound("levelup");
                onNotification($"{lead.DisplayName} grew to Lv. {lead.Level}!");
            }
        }
        else
        {
            onNotification($"You used the {item.Name}!");
        }
    }

    public void Draw(int screenWidth, int screenHeight, Inventory inventory)
    {
        if (!IsActive) return;

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
