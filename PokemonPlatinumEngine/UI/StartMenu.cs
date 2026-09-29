using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.UI;

public class StartMenu
{
    public int SelectedIndex { get; set; } = 0;
    public bool IsActive { get; set; } = false;

    private readonly string[] menuItems =
    {
        "POKÉDEX",
        "POKÉMON",
        "BAG",
        "TRAINER",
        "SAVE",
        "OPTIONS",
        "EXIT"
    };

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("cancel");
    }

    public string? Update()
    {
        if (!IsActive) return null;

        if (InputManager.IsActionPressed(GameAction.Up))
        {
            SelectedIndex = (SelectedIndex - 1 + menuItems.Length) % menuItems.Length;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Down))
        {
            SelectedIndex = (SelectedIndex + 1) % menuItems.Length;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu))
        {
            Close();
            return null;
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            AudioManager.PlaySound("select");
            string item = menuItems[SelectedIndex];
            if (item == "EXIT")
            {
                Close();
                return null;
            }
            return item;
        }

        return null;
    }

    public void Draw(int screenWidth)
    {
        if (!IsActive) return;

        int menuWidth = 320;
        int itemHeight = 58;
        int menuHeight = menuItems.Length * itemHeight + 36;
        int menuX = screenWidth - menuWidth - 40;
        int menuY = 40;

        RenderHelper.DrawPlatinumPanel(menuX, menuY, menuWidth, menuHeight, Palette.UiPanelBg);

        for (int i = 0; i < menuItems.Length; i++)
        {
            int iy = menuY + 18 + i * itemHeight;
            bool isSelected = SelectedIndex == i;

            if (isSelected)
            {
                Raylib.DrawRectangle(menuX + 14, iy, menuWidth - 28, itemHeight - 8, Palette.UiAccent);
                RenderHelper.DrawTextWithShadow(menuItems[i], menuX + 48, iy + 10, 24, Color.White);

                Raylib.DrawTriangle(
                    new System.Numerics.Vector2(menuX + 24, iy + 10),
                    new System.Numerics.Vector2(menuX + 24, iy + 34),
                    new System.Numerics.Vector2(menuX + 38, iy + 22),
                    Color.White);
            }
            else
            {
                RenderHelper.DrawTextWithShadow(menuItems[i], menuX + 38, iy + 10, 24, Palette.TextDark);
            }
        }
    }
}
