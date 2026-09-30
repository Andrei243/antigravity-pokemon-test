using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public enum BattleMenuState
{
    Main,
    Moves,
    SwitchPokemon,
    SelectBagItem,
    Message
}

public class BattleHUD
{
    // Battlefield layout in virtual-screen pixels. The battle renderer projects these from its 3D camera every
    // frame; the defaults match the settled camera.

    /// <summary>Where each Pokémon's feet touch its platform.</summary>
    public static Vector2 EnemyFeet { get; set; } = new(1430, 400);
    public static Vector2 PlayerFeet { get; set; } = new(470, 800);

    /// <summary>Rough body centres, used as targets for move effects and Poké Ball throws.</summary>
    public static Vector2 EnemyCenter { get; set; } = new(1430, 226);
    public static Vector2 PlayerCenter { get; set; } = new(470, 568);

    private const float BoxSlideTime = 0.35f;

    public BattleMenuState MenuState { get; set; } = BattleMenuState.Main;
    public int MainMenuIndex { get; set; } = 0;
    public int MoveMenuIndex { get; set; } = 0;
    public int SwitchMenuIndex { get; set; } = 0;
    public int BagMenuIndex { get; set; } = 0;

    /// <summary>Draws the HP boxes, move effects and the bottom panel over the battle field.</summary>
    /// <param name="playerPokemon">The Pokémon the menus act for.</param>
    /// <param name="anim">What the field shows: the HP boxes follow the Pokémon on the platforms and their draining bars.</param>
    public void Draw(
        int screenWidth,
        int screenHeight,
        Pokemon playerPokemon,
        Party playerParty,
        Party? enemyTrainerParty,
        string battleMessage,
        BattleVFX vfx,
        BattleAnimator anim,
        Inventory inventory)
    {
        // The vector HUD draws the HP boxes and effects, and the panels it has been rebuilt for
        if (UI.ModernUi.DrawBattle(this, screenWidth, screenHeight, playerPokemon, enemyTrainerParty, battleMessage, vfx, anim)) return;

        // Switching and the bag still use the older panels until G3 rebuilds them
        int panelHeight = 260;
        int panelY = screenHeight - panelHeight;
        RenderHelper.DrawPlatinumPanel(0, panelY, screenWidth, panelHeight, Palette.UiBackground);
        if (MenuState == BattleMenuState.SwitchPokemon) DrawSwitchSelection(screenWidth, panelY, playerParty, playerPokemon);
        else DrawBagSelection(screenWidth, panelY, inventory);
    }

    /// <summary>How far a side's HP box is slid off screen: 0 = in place, 1 = fully out, -1 = hidden.</summary>
    internal static float BoxSlide(BattleAnimator anim, CombatantView view)
    {
        if (view.Shown == null || !view.Present) return -1f;

        float t = 1f;
        if (view.SendOutAge >= 0f) t = view.SendOutAge / BoxSlideTime;
        else if (view == anim.Enemy && anim.Time < 1.2f + BoxSlideTime) t = (anim.Time - 1.2f) / BoxSlideTime; // wild Pokémon, after the camera sweep

        t = Math.Clamp(t, 0f, 1f);
        return (1f - t) * (1f - t) * (1f - t);
    }

    private void DrawSwitchSelection(int screenWidth, int panelY, Party party, Pokemon activePokemon)
    {
        int panelHeight = 260;
        RenderHelper.DrawPlatinumPanel(24, panelY + 12, screenWidth - 48, panelHeight - 24, Palette.UiPanelBg);
        string prompt = activePokemon.IsFainted
            ? "Your Pokémon fainted! Choose a Pokémon to send out:"
            : "Choose a Pokémon to switch in: (Press X / Esc to Cancel)";
        RenderHelper.DrawTextWithShadow(prompt, 40, panelY + 22, 20, Palette.TextDark);

        int slotWidth = (screenWidth - 48 - 48) / 3;
        int slotHeight = 88;

        for (int i = 0; i < party.Count; i++)
        {
            var pkmn = party.Members[i];
            int col = i % 3;
            int row = i / 3;
            int sx = 36 + col * (slotWidth + 16);
            int sy = panelY + 56 + row * (slotHeight + 10);

            bool isSelected = SwitchMenuIndex == i;
            Color fill = isSelected ? Color.White : (pkmn.IsFainted ? new Color(210, 210, 210, 255) : Palette.UiBackground);

            RenderHelper.DrawPlatinumPanel(sx, sy, slotWidth, slotHeight, fill);

            var icon = PixelArtGenerator.GetPokemonIcon(pkmn.Species.Name);
            Raylib.DrawTexture(icon, sx + 12, sy + 18, Color.White);

            RenderHelper.DrawTextWithShadow(pkmn.DisplayName, sx + 72, sy + 14, 22, isSelected ? Palette.UiAccent : Palette.TextDark);
            RenderHelper.DrawTextWithShadow($"Lv.{pkmn.Level}  HP: {pkmn.CurrentHP}/{pkmn.MaxHP}", sx + 72, sy + 48, 18, pkmn.IsFainted ? Color.Red : Palette.TextDark);
        }
    }

    private void DrawBagSelection(int screenWidth, int panelY, Inventory inventory)
    {
        int panelHeight = 260;
        RenderHelper.DrawPlatinumPanel(24, panelY + 12, screenWidth - 48, panelHeight - 24, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow("Choose an item to use: (Press X / Esc to Cancel)", 40, panelY + 24, 20, Palette.TextDark);

        string[] bagItems = { "Poké Ball", "Great Ball", "Potion", "Super Potion" };
        int itemWidth = (screenWidth - 48 - 64) / 2;

        for (int i = 0; i < bagItems.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            int bx = 40 + col * (itemWidth + 24);
            int by = panelY + 68 + row * 78;

            bool isSelected = BagMenuIndex == i;
            var itData = ItemDatabase.Get(bagItems[i]);
            int qty = itData != null ? inventory.GetQuantity(itData) : 0;

            RenderHelper.DrawPlatinumPanel(bx, by, itemWidth, 68, isSelected ? Color.White : Palette.UiBackground);
            RenderHelper.DrawTextWithShadow(bagItems[i], bx + 24, by + 18, 24, isSelected ? Palette.UiAccent : (qty > 0 ? Palette.TextDark : Color.Gray));
            RenderHelper.DrawTextWithShadow($"x{qty}", bx + itemWidth - 80, by + 18, 24, isSelected ? Palette.UiAccent : (qty > 0 ? Palette.TextDark : Color.Gray));
        }
    }
}
