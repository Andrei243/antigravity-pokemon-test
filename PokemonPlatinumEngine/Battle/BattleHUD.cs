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
    public BattleMenuState MenuState { get; set; } = BattleMenuState.Main;
    public int MainMenuIndex { get; set; } = 0;
    public int MoveMenuIndex { get; set; } = 0;
    public int SwitchMenuIndex { get; set; } = 0;
    public int BagMenuIndex { get; set; } = 0;

    public void Draw(
        int screenWidth,
        int screenHeight,
        Pokemon playerPokemon,
        Pokemon enemyPokemon,
        Party playerParty,
        Party? enemyTrainerParty,
        bool isTrainerBattle,
        string battleMessage,
        BattleVFX vfx,
        float playerSpriteOffset,
        float enemySpriteOffset,
        bool playerDamageFlash,
        bool enemyDamageFlash,
        Inventory inventory)
    {
        // 1. Battlefield Sky & Ground Gradient
        int panelHeight = 260;
        int panelY = screenHeight - panelHeight;

        Raylib.DrawRectangleGradientV(0, 0, screenWidth, panelY - 180, new Color(168, 212, 248, 255), new Color(224, 240, 255, 255));
        Raylib.DrawRectangleGradientV(0, panelY - 180, screenWidth, 180, new Color(120, 200, 104, 255), new Color(88, 168, 80, 255));

        // 2. Battle Platforms (Full HD 2.0x / 2.2x scale)
        var enemyPlatform = PixelArtGenerator.GetBattlePlatformTexture(isPlayer: false);
        var playerPlatform = PixelArtGenerator.GetBattlePlatformTexture(isPlayer: true);

        Raylib.DrawTextureEx(enemyPlatform, new Vector2(1150, 270), 0f, 2.0f, Color.White);
        Raylib.DrawTextureEx(playerPlatform, new Vector2(150, 530), 0f, 2.2f, Color.White);

        // 3. Enemy Pokémon Sprite (2.8x scale)
        if (!enemyPokemon.IsFainted)
        {
            var enemyTex = PixelArtGenerator.GetPokemonSprite(enemyPokemon.Species.Name, isBack: false);
            int ex = 1270 + (int)enemySpriteOffset;
            int ey = 110;
            Color tint = enemyDamageFlash ? Color.Red : Color.White;
            Raylib.DrawTextureEx(enemyTex, new Vector2(ex, ey), 0f, 2.8f, tint);
        }

        // 4. Player Pokémon Back Sprite (3.2x scale)
        if (!playerPokemon.IsFainted)
        {
            var playerTex = PixelArtGenerator.GetPokemonSprite(playerPokemon.Species.Name, isBack: true);
            int px = 280 + (int)playerSpriteOffset;
            int py = 310;
            Color tint = playerDamageFlash ? Color.Red : Color.White;
            Raylib.DrawTextureEx(playerTex, new Vector2(px, py), 0f, 3.2f, tint);
        }

        // 5. Enemy HP Box (Top Left: x = 80, y = 60)
        DrawEnemyHPBox(80, 60, enemyPokemon, enemyTrainerParty);

        // 6. Player HP Box (Bottom Right: x = 1160, y = 490)
        DrawPlayerHPBox(1160, 490, playerPokemon);

        // 7. Visual FX layer
        vfx.Draw();

        // 8. Bottom Battle Control Panel (y = panelY, height = 260)
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

    private static void DrawEnemyHPBox(int x, int y, Pokemon pokemon, Party? trainerParty)
    {
        int w = 540, h = 130;
        RenderHelper.DrawPlatinumPanel(x, y, w, h, Palette.UiPanelBg);

        RenderHelper.DrawTextWithShadow(pokemon.DisplayName, x + 24, y + 16, 26, Palette.TextDark);
        string genderStr = pokemon.Gender == Gender.Male ? "♂" : pokemon.Gender == Gender.Female ? "♀" : "";
        Color genderColor = pokemon.Gender == Gender.Male ? new Color(56, 120, 240, 255) : new Color(240, 88, 136, 255);
        Raylib.DrawText(genderStr, x + 310, y + 16, 24, genderColor);

        RenderHelper.DrawTextWithShadow($"Lv.{pokemon.Level}", x + 360, y + 16, 24, Palette.TextDark);
        RenderHelper.DrawHPBar(x + 24, y + 58, w - 48, 24, pokemon.CurrentHP, pokemon.MaxHP);
        RenderHelper.DrawStatusBadge(x + w - 90, y + 16, pokemon.Status);

        if (trainerParty != null)
        {
            RenderHelper.DrawPartyBallStatus(x + 24, y + 92, trainerParty);
        }
    }

    private static void DrawPlayerHPBox(int x, int y, Pokemon pokemon)
    {
        int w = 640, h = 155;
        RenderHelper.DrawPlatinumPanel(x, y, w, h, Palette.UiPanelBg);

        RenderHelper.DrawTextWithShadow(pokemon.DisplayName, x + 24, y + 16, 28, Palette.TextDark);
        string genderStr = pokemon.Gender == Gender.Male ? "♂" : pokemon.Gender == Gender.Female ? "♀" : "";
        Color genderColor = pokemon.Gender == Gender.Male ? new Color(56, 120, 240, 255) : new Color(240, 88, 136, 255);
        Raylib.DrawText(genderStr, x + 360, y + 18, 24, genderColor);

        RenderHelper.DrawTextWithShadow($"Lv.{pokemon.Level}", x + 420, y + 18, 26, Palette.TextDark);
        RenderHelper.DrawHPBar(x + 24, y + 58, w - 48, 24, pokemon.CurrentHP, pokemon.MaxHP);

        string hpText = $"{pokemon.CurrentHP}/{pokemon.MaxHP}";
        int hpTextWidth = Raylib.MeasureText(hpText, 22);
        RenderHelper.DrawTextWithShadow(hpText, x + w - hpTextWidth - 28, y + 92, 22, Palette.TextDark);

        RenderHelper.DrawStatusBadge(x + 24, y + 92, pokemon.Status);
        RenderHelper.DrawExpBar(x + 130, y + 130, w - 160, 10, pokemon.ExpProgressRatio);
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
