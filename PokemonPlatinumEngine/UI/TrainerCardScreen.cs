using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.UI;

public class TrainerCardScreen
{
    public bool IsActive { get; set; } = false;

    public void Open()
    {
        IsActive = true;
    }

    public void Close()
    {
        IsActive = false;
    }

    public void Update()
    {
        if (!IsActive) return;

        if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
    }

    public void Draw(int screenWidth, int screenHeight, SaveData saveData)
    {
        if (!IsActive) return;

        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(30, 36, 48, 255));

        int cardWidth = 1160;
        int cardHeight = 720;
        int cx = (screenWidth - cardWidth) / 2;
        int cy = (screenHeight - cardHeight) / 2;

        RenderHelper.DrawPlatinumPanel(cx, cy, cardWidth, cardHeight, Palette.UiBackground);

        // Header Strip
        Raylib.DrawRectangle(cx + 12, cy + 12, cardWidth - 24, 60, Palette.UiAccent);
        RenderHelper.DrawTextWithShadow("TRAINER CARD", cx + 36, cy + 24, 30, Color.White);
        RenderHelper.DrawTextWithShadow("IDNo. 24391", cx + cardWidth - 200, cy + 26, 24, Color.White);

        // Player Info
        int infoX = cx + 56;
        int infoY = cy + 104;
        RenderHelper.DrawTextWithShadow($"NAME: {saveData.PlayerName}", infoX, infoY, 28, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"MONEY: ${saveData.Money:N0}", infoX, infoY + 48, 26, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"POKÉDEX: {saveData.CaughtSpecies.Count} CAUGHT", infoX, infoY + 96, 26, Palette.TextDark);

        int mins = (int)(saveData.PlayTimeSeconds / 60);
        int secs = (int)(saveData.PlayTimeSeconds % 60);
        RenderHelper.DrawTextWithShadow($"TIME: {mins:D2}:{secs:D2}", infoX, infoY + 144, 26, Palette.TextDark);

        // Lucas Character Sprite Preview on card
        var playerTex = PixelArtGenerator.GetNpcSprite("TRAINER", Data.Direction.Down);
        Raylib.DrawTextureEx(playerTex, new System.Numerics.Vector2(cx + cardWidth - 240, infoY + 10), 0f, 3.5f, Color.White);

        // Sinnoh Badges Section
        int badgeY = cy + 330;
        int badgePanelHeight = 330;
        RenderHelper.DrawPlatinumPanel(cx + 24, badgeY, cardWidth - 48, badgePanelHeight, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow("SINNOH GYM BADGES", cx + 48, badgeY + 24, 26, Palette.UiAccent);

        string[] badgeNames = { "Coal", "Forest", "Cobble", "Fen", "Relic", "Mine", "Icicle", "Beacon" };
        Color[] badgeColors = {
            Color.Brown, Color.Green, Color.Orange, Color.Blue,
            Color.Purple, Color.DarkGray, Color.SkyBlue, Color.Gold
        };

        int badgeSpacing = (cardWidth - 140) / 8;

        for (int b = 0; b < 8; b++)
        {
            int bx = cx + 56 + b * badgeSpacing;
            int by = badgeY + 96;
            bool hasBadge = (saveData.Badges & (1 << b)) != 0;

            if (hasBadge)
            {
                Raylib.DrawCircle(bx + 40, by + 40, 36, Color.Black);
                Raylib.DrawCircle(bx + 40, by + 40, 32, badgeColors[b]);
                Raylib.DrawCircle(bx + 40, by + 40, 12, Color.White);
            }
            else
            {
                Raylib.DrawCircleLines(bx + 40, by + 40, 34, Color.Gray);
            }

            RenderHelper.DrawTextWithShadow(badgeNames[b], bx + 16, by + 90, 20, hasBadge ? Palette.TextDark : Color.Gray);
        }

        RenderHelper.DrawTextWithShadow("Press Z, Space, or X to return", cx + cardWidth - 360, cy + cardHeight + 16, 20, Color.LightGray);
    }
}
