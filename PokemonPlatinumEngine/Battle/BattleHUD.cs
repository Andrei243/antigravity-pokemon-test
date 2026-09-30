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
        // The vector interface (plan 04 · G1 prototype) draws the states it covers; G3 rebuilds the rest
        if (ArtLook.VectorUi && UI.ModernUi.DrawBattle(this, screenWidth, screenHeight, playerPokemon, enemyTrainerParty, battleMessage, vfx, anim)) return;

        int panelHeight = 260;
        int panelY = screenHeight - panelHeight;

        // 1. HP boxes slide in once their Pokémon is out and disappear when it leaves the field
        float enemySlide = BoxSlide(anim, anim.Enemy);
        if (enemySlide >= 0f) DrawEnemyHPBox((int)(80 - 700 * enemySlide), 60, anim.Enemy, enemyTrainerParty);

        float playerSlide = BoxSlide(anim, anim.Player);
        if (playerSlide >= 0f) DrawPlayerHPBox((int)(1160 + 800 * playerSlide), 490, anim.Player);

        // 2. Visual FX layer
        vfx.Draw();

        // 3. Bottom Battle Control Panel (y = panelY, height = 260)
        RenderHelper.DrawPlatinumPanel(0, panelY, screenWidth, panelHeight, Palette.UiBackground);

        switch (MenuState)
        {
            case BattleMenuState.Message:
                DrawMessagePanel(24, panelY + 16, screenWidth - 48, panelHeight - 32, battleMessage);
                break;
            case BattleMenuState.Main:
                DrawMainMenu(screenWidth, panelY, playerPokemon.DisplayName);
                break;
            case BattleMenuState.Moves:
                DrawMoveSelection(screenWidth, panelY, playerPokemon);
                break;
            case BattleMenuState.SwitchPokemon:
                DrawSwitchSelection(screenWidth, panelY, playerParty, playerPokemon);
                break;
            case BattleMenuState.SelectBagItem:
                DrawBagSelection(screenWidth, panelY, inventory);
                break;
        }
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

    private static void DrawEnemyHPBox(int x, int y, CombatantView view, Party? trainerParty)
    {
        var pokemon = view.Shown!;
        int w = 540, h = 130;
        RenderHelper.DrawPlatinumPanel(x, y, w, h, Palette.UiPanelBg);

        RenderHelper.DrawTextWithShadow(pokemon.DisplayName, x + 24, y + 16, 26, Palette.TextDark);
        RenderHelper.DrawGenderSymbol(x + 316, y + 18, 24, pokemon.Gender);

        RenderHelper.DrawTextWithShadow($"Lv.{pokemon.Level}", x + 360, y + 16, 24, Palette.TextDark);
        RenderHelper.DrawHPBar(x + 24, y + 58, w - 48, 24, (int)MathF.Ceiling(view.DisplayedHp), pokemon.MaxHP);
        RenderHelper.DrawStatusBadge(x + w - 90, y + 16, pokemon.Status);

        if (trainerParty != null)
        {
            RenderHelper.DrawPartyBallStatus(x + 24, y + 92, trainerParty);
        }
    }

    private static void DrawPlayerHPBox(int x, int y, CombatantView view)
    {
        var pokemon = view.Shown!;
        int w = 640, h = 155;
        RenderHelper.DrawPlatinumPanel(x, y, w, h, Palette.UiPanelBg);

        RenderHelper.DrawTextWithShadow(pokemon.DisplayName, x + 24, y + 16, 28, Palette.TextDark);
        RenderHelper.DrawGenderSymbol(x + 366, y + 20, 24, pokemon.Gender);

        // HP counts down with the bar as it drains
        int hp = (int)MathF.Ceiling(view.DisplayedHp);
        RenderHelper.DrawTextWithShadow($"Lv.{pokemon.Level}", x + 420, y + 18, 26, Palette.TextDark);
        RenderHelper.DrawHPBar(x + 24, y + 58, w - 48, 24, hp, pokemon.MaxHP);

        string hpText = $"{hp}/{pokemon.MaxHP}";
        int hpTextWidth = RenderHelper.MeasureText(hpText, 22);
        RenderHelper.DrawTextWithShadow(hpText, x + w - hpTextWidth - 28, y + 92, 22, Palette.TextDark);

        RenderHelper.DrawStatusBadge(x + 24, y + 92, pokemon.Status);
        RenderHelper.DrawExpBar(x + 130, y + 130, w - 160, 10, view.DisplayedExp);
    }

    private void DrawMainMenu(int screenWidth, int panelY, string activePkmnName)
    {
        int promptWidth = 620;
        int panelHeight = 260;
        RenderHelper.DrawPlatinumPanel(24, panelY + 16, promptWidth, panelHeight - 32, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow("What will", 50, panelY + 54, 32, Palette.TextDark);
        RenderHelper.DrawTextWithShadow($"{activePkmnName} do?", 50, panelY + 114, 32, Palette.UiAccent);

        int startX = promptWidth + 48;
        int btnWidth = (screenWidth - startX - 36) / 2;
        int btnHeight = (panelHeight - 32 - 16) / 2;

        string[] buttons = { "FIGHT", "BAG", "POKÉMON", "RUN" };
        Color[] btnColors = { new Color(240, 88, 88, 255), new Color(240, 184, 56, 255), new Color(72, 192, 120, 255), new Color(72, 144, 240, 255) };

        for (int i = 0; i < 4; i++)
        {
            int col = i % 2;
            int row = i / 2;
            int bx = startX + col * (btnWidth + 16);
            int by = panelY + 16 + row * (btnHeight + 16);

            bool isSelected = MainMenuIndex == i;
            Color fill = isSelected ? Color.White : Palette.UiPanelBg;

            RenderHelper.DrawPlatinumPanel(bx, by, btnWidth, btnHeight, fill);
            Raylib.DrawRectangle(bx + 10, by + 10, 14, btnHeight - 20, btnColors[i]);
            RenderHelper.DrawTextWithShadow(buttons[i], bx + 42, by + (btnHeight - 28) / 2, 28, isSelected ? Palette.UiAccent : Palette.TextDark);
        }
    }

    private void DrawMoveSelection(int screenWidth, int panelY, Pokemon pokemon)
    {
        int moveBoxWidth = 560;
        int moveBoxHeight = 104;
        int startX = 24;

        for (int i = 0; i < 4; i++)
        {
            int col = i % 2;
            int row = i / 2;
            int mx = startX + col * (moveBoxWidth + 16);
            int my = panelY + 16 + row * (moveBoxHeight + 16);

            bool isSelected = MoveMenuIndex == i;
            Color fill = isSelected ? Color.White : Palette.UiPanelBg;

            RenderHelper.DrawPlatinumPanel(mx, my, moveBoxWidth, moveBoxHeight, fill);

            if (i < pokemon.Moves.Count)
            {
                var move = pokemon.Moves[i];
                RenderHelper.DrawTextWithShadow(move.Name, mx + 20, my + 16, 24, isSelected ? Palette.UiAccent : Palette.TextDark);
                RenderHelper.DrawTypeBadge(mx + 20, my + 54, move.Type, 90, 28);
                RenderHelper.DrawTextWithShadow($"PP  {move.CurrentPP}/{move.MaxPP}", mx + 130, my + 58, 20, Palette.TextDark);
            }
            else
            {
                RenderHelper.DrawTextWithShadow("---", mx + 30, my + 36, 26, Color.Gray);
            }
        }

        int infoX = startX + (moveBoxWidth + 16) * 2;
        int infoWidth = screenWidth - infoX - 24;
        int infoHeight = 228;
        RenderHelper.DrawPlatinumPanel(infoX, panelY + 16, infoWidth, infoHeight, Palette.UiPanelBg);

        if (MoveMenuIndex < pokemon.Moves.Count)
        {
            var move = pokemon.Moves[MoveMenuIndex];
            RenderHelper.DrawTextWithShadow($"TYPE: {move.Type}", infoX + 28, panelY + 34, 22, Palette.TextDark);
            RenderHelper.DrawTextWithShadow($"CATEGORY: {move.Category}", infoX + 28, panelY + 74, 22, Palette.TextDark);
            string pwr = move.Power > 0 ? $"{move.Power}" : "---";
            string acc = move.Accuracy > 0 ? $"{move.Accuracy}%" : "---";
            RenderHelper.DrawTextWithShadow($"POWER: {pwr}   ACCURACY: {acc}", infoX + 28, panelY + 114, 22, Palette.TextDark);
            RenderHelper.DrawTextWithShadow(move.Data.Description, infoX + 28, panelY + 152, 18, Color.DarkGray);
        }

        RenderHelper.DrawTextWithShadow("X / Esc: Back", infoX + 28, panelY + infoHeight - 16, 16, Palette.UiAccent);
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

    private static void DrawMessagePanel(int x, int y, int width, int height, string message)
    {
        RenderHelper.DrawPlatinumPanel(x, y, width, height, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow(message, x + 36, y + (height - 30) / 2, 28, Palette.TextDark);

        if ((int)(Raylib.GetTime() * 4) % 2 == 0)
        {
            int triX = x + width - 40;
            int triY = y + height - 36;
            Raylib.DrawTriangle(
                new Vector2(triX, triY),
                new Vector2(triX + 18, triY),
                new Vector2(triX + 9, triY + 16),
                Palette.UiAccent);
        }
    }
}
