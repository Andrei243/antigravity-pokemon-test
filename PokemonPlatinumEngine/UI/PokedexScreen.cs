using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public class PokedexScreen
{
    public int SelectedIndex { get; set; } = 0;
    public bool IsActive { get; set; } = false;

    private readonly List<PokemonSpecies> allSpecies = new();

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        allSpecies.Clear();
        allSpecies.AddRange(PokemonDatabase.GetAll().OrderBy(s => s.DexNumber));
    }

    public void Close()
    {
        IsActive = false;
    }

    public void Update()
    {
        if (!IsActive) return;

        if (InputManager.IsActionPressed(GameAction.Up))
        {
            if (allSpecies.Count > 0)
            {
                SelectedIndex = (SelectedIndex - 1 + allSpecies.Count) % allSpecies.Count;
                AudioManager.PlaySound("cursor");
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Down))
        {
            if (allSpecies.Count > 0)
            {
                SelectedIndex = (SelectedIndex + 1) % allSpecies.Count;
                AudioManager.PlaySound("cursor");
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
    }

    public void Draw(int screenWidth, int screenHeight, Pokedex pokedex)
    {
        if (!IsActive) return;

        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, Palette.UiBackground);

        // Header
        int headerHeight = 64;
        RenderHelper.DrawPlatinumPanel(24, 20, screenWidth - 48, headerHeight, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow("SINNOH POKÉDEX", 52, 34, 28, Palette.UiAccent);
        RenderHelper.DrawTextWithShadow($"SEEN: {pokedex.SeenCount}    CAUGHT: {pokedex.CaughtCount}", screenWidth - 460, 36, 24, Palette.TextDark);

        // Species List (Left)
        int listWidth = 740;
        int listHeight = screenHeight - 116;
        RenderHelper.DrawPlatinumPanel(24, 96, listWidth, listHeight, Palette.UiPanelBg);

        int maxVisible = 14;
        int scrollOffset = Math.Max(0, Math.Min(SelectedIndex - maxVisible / 2, Math.Max(0, allSpecies.Count - maxVisible)));

        for (int i = 0; i < maxVisible && (i + scrollOffset) < allSpecies.Count; i++)
        {
            int idx = i + scrollOffset;
            var sp = allSpecies[idx];
            int sy = 112 + i * 58;

            bool isSelected = SelectedIndex == idx;
            bool isSeen = pokedex.IsSeen(sp.DexNumber);
            bool isCaught = pokedex.IsCaught(sp.DexNumber);

            if (isSelected)
            {
                Raylib.DrawRectangle(38, sy, listWidth - 28, 50, Palette.UiAccent);
            }

            if (isCaught)
            {
                Raylib.DrawCircle(62, sy + 25, 10, Color.Red);
                Raylib.DrawCircle(62, sy + 25, 5, Color.White);
            }
            else if (isSeen)
            {
                Raylib.DrawCircleLines(62, sy + 25, 10, Color.DarkGray);
            }

            string numStr = $"No.{sp.DexNumber:D3}";
            string nameStr = isSeen ? sp.Name : "-----";
            RenderHelper.DrawTextWithShadow($"{numStr}   {nameStr}", 88, sy + 12, 24, isSelected ? Color.White : Palette.TextDark);
        }

        // Details Panel (Right)
        int detX = 24 + listWidth + 24;
        int detWidth = screenWidth - detX - 24;
        RenderHelper.DrawPlatinumPanel(detX, 96, detWidth, listHeight, Palette.UiPanelBg);

        if (SelectedIndex < allSpecies.Count)
        {
            var sel = allSpecies[SelectedIndex];
            bool isSeen = pokedex.IsSeen(sel.DexNumber);

            if (isSeen)
            {
                // Pedestal panel for Pokemon Sprite
                RenderHelper.DrawPlatinumPanel(detX + 36, 120, 300, 300, Palette.UiBackground);
                var sprite = PixelArtGenerator.GetPokemonSprite(sel.Name, isBack: false);
                float spriteScale = 3.0f;
                float sx = detX + 36 + (300 - sprite.Width * spriteScale) / 2f;
                float sy = 120 + (300 - sprite.Height * spriteScale) / 2f;
                Raylib.DrawTextureEx(sprite, new Vector2(sx, sy), 0f, spriteScale, Color.White);

                // Info to the right of sprite
                int tx = detX + 368;
                RenderHelper.DrawTextWithShadow($"No.{sel.DexNumber:D3}  {sel.Name}", tx, 134, 36, Palette.UiAccent);
                RenderHelper.DrawTextWithShadow($"The {sel.Category} Pokémon", tx, 186, 24, Palette.TextDark);
                RenderHelper.DrawTextWithShadow($"Height: {sel.Height:F1} m   |   Weight: {sel.Weight:F1} kg", tx, 226, 22, Palette.TextDark);

                RenderHelper.DrawTypeBadge(tx, 276, sel.PrimaryType, 110, 34);
                if (sel.SecondaryType.HasValue)
                {
                    RenderHelper.DrawTypeBadge(tx + 124, 276, sel.SecondaryType.Value, 110, 34);
                }

                // Dex Entry Box
                RenderHelper.DrawPlatinumPanel(detX + 36, 450, detWidth - 72, 380, Palette.UiBackground);
                RenderHelper.DrawTextWithShadow("POKÉDEX LORE & DATA", detX + 60, 474, 24, Palette.UiAccent);
                RenderHelper.DrawWrappedText(sel.DexEntry, detX + 60, 520, detWidth - 120, 26, Palette.TextDark, 10);

                RenderHelper.DrawTextWithShadow("Press Z / Space / X / Esc to close", detX + 36, 96 + listHeight - 48, 22, Color.Gray);
            }
            else
            {
                RenderHelper.DrawPlatinumPanel(detX + 36, 120, detWidth - 72, 400, Palette.UiBackground);
                RenderHelper.DrawTextWithShadow("Pokémon not yet encountered.", detX + 70, 240, 32, Color.Gray);
                RenderHelper.DrawTextWithShadow("Explore the tall grass, waterways, and battle trainers to register data.", detX + 70, 300, 24, Color.Gray);
                RenderHelper.DrawTextWithShadow("Press Z / Space / X / Esc to close", detX + 36, 96 + listHeight - 48, 22, Color.Gray);
            }
        }
    }
}
