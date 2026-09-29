using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public class StarterSelectScreen
{
    public int SelectedIndex { get; set; } = 0;
    public bool IsActive { get; set; } = false;
    public bool ConfirmingSelection { get; set; } = false;

    private readonly string[] starters = { "Turtwig", "Chimchar", "Piplup" };

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        ConfirmingSelection = false;
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
    }

    public Pokemon? Update()
    {
        if (!IsActive) return null;

        if (ConfirmingSelection)
        {
            if (InputManager.IsActionPressed(GameAction.Confirm))
            {
                string chosenSpecies = starters[SelectedIndex];
                var species = PokemonDatabase.Get(chosenSpecies)!;
                var pokemon = new Pokemon(species, 5);
                AudioManager.PlayBGM("Victory");
                Close();
                return pokemon;
            }
            else if (InputManager.IsActionPressed(GameAction.Cancel))
            {
                ConfirmingSelection = false;
                AudioManager.PlaySound("cancel");
            }
            return null;
        }

        if (InputManager.IsActionPressed(GameAction.Left))
        {
            SelectedIndex = (SelectedIndex - 1 + starters.Length) % starters.Length;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Right))
        {
            SelectedIndex = (SelectedIndex + 1) % starters.Length;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            ConfirmingSelection = true;
            AudioManager.PlaySound("select");
        }

        return null;
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;

        // Background Gradient
        Raylib.DrawRectangleGradientV(0, 0, screenWidth, screenHeight, new Color(24, 48, 96, 255), new Color(60, 120, 190, 255));

        // Header Title
        RenderHelper.DrawPlatinumPanel(40, 24, screenWidth - 80, 68, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow("PROFESSOR ROWAN'S STARTER BRIEFCASE", 64, 40, 32, Palette.UiAccent);

        // 3 Starter Pokeball Bases
        int slotWidth = 460;
        int slotHeight = 440;
        int spacing = 60;
        int totalW = 3 * slotWidth + 2 * spacing;
        int startX = (screenWidth - totalW) / 2;
        int startY = 120;

        for (int i = 0; i < 3; i++)
        {
            int sx = startX + i * (slotWidth + spacing);
            bool isSelected = SelectedIndex == i;

            Color fill = isSelected ? Color.White : Palette.UiPanelBg;
            RenderHelper.DrawPlatinumPanel(sx, startY, slotWidth, slotHeight, fill);

            var sp = PokemonDatabase.Get(starters[i])!;

            // Sprite preview
            var sprite = PixelArtGenerator.GetPokemonSprite(sp.Name, isBack: false);
            float bob = isSelected ? MathF.Sin((float)Raylib.GetTime() * 6f) * 10f : 0f;
            float spriteScale = 2.6f;
            float px = sx + (slotWidth - sprite.Width * spriteScale) / 2f;
            float py = startY + 36 + bob;
            Raylib.DrawTextureEx(sprite, new Vector2(px, py), 0f, spriteScale, Color.White);

            // Name & Type
            RenderHelper.DrawTextWithShadow(sp.Name, sx + (slotWidth - RenderHelper.MeasureText(sp.Name, 30)) / 2, startY + 330, 30, isSelected ? Palette.UiAccent : Palette.TextDark);
            RenderHelper.DrawTypeBadge(sx + (slotWidth - 140) / 2, startY + 376, sp.PrimaryType, 140, 36);
        }

        // Details Panel (Bottom)
        var current = PokemonDatabase.Get(starters[SelectedIndex])!;
        int detY = startY + slotHeight + 36;
        int detHeight = screenHeight - detY - 36;
        RenderHelper.DrawPlatinumPanel(40, detY, screenWidth - 80, detHeight, Palette.UiPanelBg);

        if (ConfirmingSelection)
        {
            RenderHelper.DrawTextWithShadow($"Do you choose {current.Name} as your partner Pokémon?", 68, detY + 32, 34, Palette.UiAccent);
            RenderHelper.DrawTextWithShadow("Press Z / Space to Confirm   |   X / Esc to Change Selection", 68, detY + 96, 26, Palette.TextDark);
        }
        else
        {
            RenderHelper.DrawTextWithShadow($"No.{current.DexNumber:D3} {current.Name}  -  The {current.Category} Pokémon", 68, detY + 28, 30, Palette.UiAccent);
            RenderHelper.DrawTextWithShadow(current.DexEntry, 68, detY + 76, 24, Palette.TextDark);
            RenderHelper.DrawTextWithShadow($"Base Stats: HP {current.BaseHP}  |  Attack {current.BaseAttack}  |  Defense {current.BaseDefense}  |  Sp. Atk {current.BaseSpAttack}  |  Sp. Def {current.BaseSpDefense}  |  Speed {current.BaseSpeed}", 68, detY + 130, 22, Palette.TextDark);
            RenderHelper.DrawTextWithShadow("Left / Right: Select Pokémon   |   Z / Space: Choose Partner", screenWidth - 720, detY + detHeight - 44, 22, Color.Gray);
        }
    }
}
