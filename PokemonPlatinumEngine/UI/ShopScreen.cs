using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public class ShopScreen
{
    public int SelectedIndex { get; set; } = 0;
    public bool IsActive { get; set; } = false;

    private readonly List<ItemData> shopStock = new();

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        shopStock.Clear();
        shopStock.Add(ItemDatabase.Get("Poké Ball")!);
        shopStock.Add(ItemDatabase.Get("Great Ball")!);
        shopStock.Add(ItemDatabase.Get("Potion")!);
        shopStock.Add(ItemDatabase.Get("Super Potion")!);
        shopStock.Add(ItemDatabase.Get("Antidote")!);
        shopStock.Add(ItemDatabase.Get("Revive")!);
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
    }

    public void Update(Inventory playerInventory, ref int playerMoney, Action<string> onNotification)
    {
        if (!IsActive) return;

        if (InputManager.IsActionPressed(GameAction.Up))
        {
            SelectedIndex = (SelectedIndex - 1 + shopStock.Count) % shopStock.Count;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Down))
        {
            SelectedIndex = (SelectedIndex + 1) % shopStock.Count;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            var item = shopStock[SelectedIndex];
            if (playerMoney >= item.Price)
            {
                playerMoney -= item.Price;
                playerInventory.AddItem(item, 1);
                AudioManager.PlaySound("select");
                onNotification($"Purchased 1 {item.Name} for ${item.Price}!");
            }
            else
            {
                onNotification("You don't have enough money!");
            }
        }
    }

    public void Draw(int screenWidth, int screenHeight, int playerMoney)
    {
        if (!IsActive) return;

        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, Palette.UiBackground);

        // Header
        int headerHeight = 64;
        RenderHelper.DrawPlatinumPanel(24, 20, screenWidth - 48, headerHeight, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow("SANDGEM TOWN POKÉ MART", 52, 34, 28, Palette.UiAccent);
        RenderHelper.DrawTextWithShadow($"MONEY: ${playerMoney:N0}", screenWidth - 360, 36, 26, Palette.TextDark);

        // Catalog List (Left)
        int listWidth = 840;
        int listHeight = screenHeight - 116;
        RenderHelper.DrawPlatinumPanel(24, 96, listWidth, listHeight, Palette.UiPanelBg);

        for (int i = 0; i < shopStock.Count; i++)
        {
            var it = shopStock[i];
            int iy = 112 + i * 68;
            bool isSelected = SelectedIndex == i;

            if (isSelected)
            {
                Raylib.DrawRectangle(38, iy, listWidth - 28, 58, Palette.UiAccent);
                RenderHelper.DrawTextWithShadow(it.Name, 60, iy + 14, 26, Color.White);
                RenderHelper.DrawTextWithShadow($"${it.Price:N0}", listWidth - 120, iy + 14, 26, Color.White);
            }
            else
            {
                RenderHelper.DrawTextWithShadow(it.Name, 52, iy + 14, 26, Palette.TextDark);
                RenderHelper.DrawTextWithShadow($"${it.Price:N0}", listWidth - 126, iy + 14, 26, Palette.TextDark);
            }
        }

        // Details Panel (Right)
        int descX = 24 + listWidth + 24;
        int descWidth = screenWidth - descX - 24;
        RenderHelper.DrawPlatinumPanel(descX, 96, descWidth, listHeight, Palette.UiPanelBg);

        if (SelectedIndex < shopStock.Count)
        {
            var sel = shopStock[SelectedIndex];
            RenderHelper.DrawTextWithShadow(sel.Name, descX + 36, 128, 36, Palette.UiAccent);
            RenderHelper.DrawTextWithShadow($"Price: ${sel.Price:N0}", descX + 36, 180, 26, Palette.TextDark);

            RenderHelper.DrawPlatinumPanel(descX + 32, 230, descWidth - 64, 420, Palette.UiBackground);
            RenderHelper.DrawTextWithShadow(sel.Description, descX + 56, 264, 26, Palette.TextDark);

            RenderHelper.DrawTextWithShadow("Press Z / Space to Buy 1   |   X / Esc: Exit", descX + 36, 96 + listHeight - 48, 22, Color.Gray);
        }
    }
}
