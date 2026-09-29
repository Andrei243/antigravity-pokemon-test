using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public class PartyScreen
{
    public int SelectedIndex { get; set; } = 0;
    public int? SwapSourceIndex { get; set; } = null;
    public bool ShowSummary { get; set; } = false;
    public bool IsActive { get; set; } = false;

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        SwapSourceIndex = null;
        ShowSummary = false;
    }

    public void Close()
    {
        IsActive = false;
        SwapSourceIndex = null;
        ShowSummary = false;
    }

    public void Update(Party party)
    {
        if (!IsActive) return;

        if (ShowSummary)
        {
            if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm))
            {
                ShowSummary = false;
                AudioManager.PlaySound("cancel");
            }
            return;
        }

        if (InputManager.IsActionPressed(GameAction.Up))
        {
            SelectedIndex = (SelectedIndex - 1 + party.Count) % party.Count;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Down))
        {
            SelectedIndex = (SelectedIndex + 1) % party.Count;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            if (SwapSourceIndex.HasValue)
            {
                SwapSourceIndex = null;
                AudioManager.PlaySound("cancel");
            }
            else
            {
                Close();
                AudioManager.PlaySound("cancel");
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            if (SwapSourceIndex.HasValue)
            {
                party.Swap(SwapSourceIndex.Value, SelectedIndex);
                SwapSourceIndex = null;
                AudioManager.PlaySound("select");
            }
            else
            {
                ShowSummary = true;
                AudioManager.PlaySound("select");
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Run))
        {
            SwapSourceIndex = SelectedIndex;
            AudioManager.PlaySound("select");
        }
    }

    public void Draw(int screenWidth, int screenHeight, Party party)
    {
        if (!IsActive) return;

        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, Palette.UiBackground);

        // Header
        RenderHelper.DrawPlatinumPanel(30, 24, screenWidth - 60, 56, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow("POKÉMON PARTY", 54, 36, 26, Palette.UiAccent);
        RenderHelper.DrawTextWithShadow("Z: Summary   |   Shift / X: Move Slot   |   Esc: Back", screenWidth - 520, 40, 18, Palette.TextDark);

        if (ShowSummary && SelectedIndex < party.Count)
        {
            DrawPokemonSummary(screenWidth, screenHeight, party.Members[SelectedIndex]);
            return;
        }

        int startY = 100;
        int slotHeight = 136;

        for (int i = 0; i < party.Count; i++)
        {
            var pkmn = party.Members[i];
            int sy = startY + i * (slotHeight + 16);

            bool isSelected = SelectedIndex == i;
            bool isSwapping = SwapSourceIndex == i;

            Color bgColor = isSwapping ? new Color(255, 230, 200, 255) : isSelected ? Color.White : Palette.UiPanelBg;
            RenderHelper.DrawPlatinumPanel(40, sy, screenWidth - 80, slotHeight, bgColor);

            // Icon (1.8x scale)
            var icon = PixelArtGenerator.GetPokemonIcon(pkmn.Species.Name);
            Raylib.DrawTextureEx(icon, new Vector2(64, sy + 24), 0f, 1.8f, Color.White);

            // Name & Level
            RenderHelper.DrawTextWithShadow(pkmn.DisplayName, 190, sy + 34, 28, isSelected ? Palette.UiAccent : Palette.TextDark);
            RenderHelper.DrawTextWithShadow($"Lv.{pkmn.Level}", 480, sy + 38, 24, Palette.TextDark);

            // Type Badge
            RenderHelper.DrawTypeBadge(620, sy + 34, pkmn.Species.PrimaryType, 110, 34);

            // HP Bar & Text
            RenderHelper.DrawHPBar(780, sy + 36, 560, 28, pkmn.CurrentHP, pkmn.MaxHP);
            string hpStr = $"{pkmn.CurrentHP} / {pkmn.MaxHP}";
            RenderHelper.DrawTextWithShadow(hpStr, 1370, sy + 38, 24, Palette.TextDark);

            // Status
            RenderHelper.DrawStatusBadge(1560, sy + 36, pkmn.Status);

            if (i == 0)
            {
                Raylib.DrawRectangle(screenWidth - 190, sy + 34, 80, 32, Palette.UiAccentSecondary);
                RenderHelper.DrawTextWithShadow("LEAD", screenWidth - 176, sy + 40, 18, Color.White);
            }
        }
    }

    private static void DrawPokemonSummary(int screenWidth, int screenHeight, Pokemon pkmn)
    {
        int panelW = screenWidth - 80;
        int panelH = screenHeight - 120;
        int px = 40, py = 90;

        RenderHelper.DrawPlatinumPanel(px, py, panelW, panelH, Palette.UiPanelBg);

        // Big HD Sprite Preview (3.2x scale)
        var sprite = PixelArtGenerator.GetPokemonSprite(pkmn.Species.Name, isBack: false);
        Raylib.DrawTextureEx(sprite, new Vector2(px + 60, py + 60), 0f, 3.2f, Color.White);

        // Basic Info
        int tx = px + 520;
        RenderHelper.DrawTextWithShadow($"No. {pkmn.Species.DexNumber:D3}   {pkmn.DisplayName}", tx, py + 40, 34, Palette.UiAccent);
        RenderHelper.DrawTextWithShadow($"The {pkmn.Species.Category} Pokémon", tx, py + 86, 22, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"Nature: {pkmn.Nature}   |   Level: {pkmn.Level}", tx, py + 120, 22, Palette.TextDark);

        // Types
        RenderHelper.DrawTypeBadge(tx, py + 160, pkmn.Species.PrimaryType, 110, 34);
        if (pkmn.Species.SecondaryType.HasValue)
        {
            RenderHelper.DrawTypeBadge(tx + 130, py + 160, pkmn.Species.SecondaryType.Value, 110, 34);
        }

        // Stats Box
        int statsY = py + 220;
        int statsW = panelW - 560;
        RenderHelper.DrawPlatinumPanel(tx, statsY, statsW, 180, Palette.UiBackground);
        RenderHelper.DrawTextWithShadow($"HP:        {pkmn.CurrentHP} / {pkmn.MaxHP}", tx + 32, statsY + 24, 24, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"ATTACK:    {pkmn.Attack}", tx + 32, statsY + 74, 24, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"DEFENSE:   {pkmn.Defense}", tx + 32, statsY + 124, 24, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"SP. ATK:   {pkmn.SpAttack}", tx + 460, statsY + 24, 24, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"SP. DEF:   {pkmn.SpDefense}", tx + 460, statsY + 74, 24, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"SPEED:     {pkmn.Speed}", tx + 460, statsY + 124, 24, Palette.TextDark);

        // Moves Box
        int movesY = py + 430;
        RenderHelper.DrawPlatinumPanel(px + 40, movesY, panelW - 80, 240, Palette.UiBackground);
        RenderHelper.DrawTextWithShadow("KNOWN MOVES:", px + 64, movesY + 20, 24, Palette.UiAccent);

        for (int m = 0; m < pkmn.Moves.Count; m++)
        {
            var move = pkmn.Moves[m];
            int col = m % 2;
            int row = m / 2;
            int mx = px + 64 + col * 860;
            int my = movesY + 68 + row * 76;

            RenderHelper.DrawTypeBadge(mx, my, move.Type, 96, 30);
            RenderHelper.DrawTextWithShadow(move.Name, mx + 114, my + 2, 24, Palette.TextDark);
            RenderHelper.DrawTextWithShadow($"PP {move.CurrentPP}/{move.MaxPP}", mx + 420, my + 4, 22, Palette.TextDark);
        }

        RenderHelper.DrawTextWithShadow("Press Z, Space, or Esc to return", px + panelW - 380, py + panelH - 36, 20, Color.Gray);
    }
}
