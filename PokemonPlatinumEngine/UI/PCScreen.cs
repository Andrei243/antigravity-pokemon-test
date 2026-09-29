using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

public class PCScreen
{
    public int ModeIndex { get; set; } = 0; // 0 = Party, 1 = Box
    public int PartyIndex { get; set; } = 0;
    public int BoxIndex { get; set; } = 0;
    public bool IsActive { get; set; } = false;

    public void Open()
    {
        IsActive = true;
        ModeIndex = 0;
        PartyIndex = 0;
        BoxIndex = 0;
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
    }

    public void Update(Party party, List<Pokemon> boxStorage, Action<string> onNotification)
    {
        if (!IsActive) return;

        if (InputManager.IsActionPressed(GameAction.Left) || InputManager.IsActionPressed(GameAction.Right))
        {
            ModeIndex = 1 - ModeIndex;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Up))
        {
            if (ModeIndex == 0)
            {
                PartyIndex = (PartyIndex - 1 + party.Count) % Math.Max(1, party.Count);
            }
            else
            {
                if (boxStorage.Count > 0)
                {
                    BoxIndex = (BoxIndex - 1 + boxStorage.Count) % boxStorage.Count;
                }
            }
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Down))
        {
            if (ModeIndex == 0)
            {
                PartyIndex = (PartyIndex + 1) % Math.Max(1, party.Count);
            }
            else
            {
                if (boxStorage.Count > 0)
                {
                    BoxIndex = (BoxIndex + 1) % boxStorage.Count;
                }
            }
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            if (ModeIndex == 0)
            {
                // Deposit
                if (party.Count <= 1)
                {
                    onNotification("You can't deposit your last Pokémon!");
                }
                else if (PartyIndex < party.Count)
                {
                    var pkmn = party.Members[PartyIndex];
                    party.RemoveAt(PartyIndex);
                    boxStorage.Add(pkmn);
                    PartyIndex = Math.Min(PartyIndex, party.Count - 1);
                    AudioManager.PlaySound("select");
                    onNotification($"Deposited {pkmn.DisplayName} into Box 1.");
                }
            }
            else
            {
                // Withdraw
                if (party.Count >= 6)
                {
                    onNotification("Your party is already full (6 Pokémon)!");
                }
                else if (boxStorage.Count > 0 && BoxIndex < boxStorage.Count)
                {
                    var pkmn = boxStorage[BoxIndex];
                    boxStorage.RemoveAt(BoxIndex);
                    party.Add(pkmn);
                    BoxIndex = Math.Max(0, Math.Min(BoxIndex, boxStorage.Count - 1));
                    AudioManager.PlaySound("select");
                    onNotification($"Withdrew {pkmn.DisplayName} to your party.");
                }
            }
        }
    }

    public void Draw(int screenWidth, int screenHeight, Party party, List<Pokemon> boxStorage)
    {
        if (!IsActive) return;

        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, Palette.UiBackground);

        // Header
        RenderHelper.DrawPlatinumPanel(16, 16, screenWidth - 32, 44, Palette.UiPanelBg);
        RenderHelper.DrawTextWithShadow("BEBE'S POKÉMON STORAGE SYSTEM", 32, 26, 20, Palette.UiAccent);
        RenderHelper.DrawTextWithShadow("Left/Right: Switch Panel | Z: Deposit / Withdraw | X: Exit", screenWidth - 440, 28, 14, Palette.TextDark);

        int colWidth = (screenWidth - 48) / 2;
        int listHeight = screenHeight - 88;

        // Party Column (Left)
        Color partyColFill = ModeIndex == 0 ? Color.White : Palette.UiPanelBg;
        RenderHelper.DrawPlatinumPanel(16, 70, colWidth, listHeight, partyColFill);
        RenderHelper.DrawTextWithShadow($"YOUR PARTY ({party.Count}/6)", 32, 84, 18, Palette.UiAccent);

        for (int i = 0; i < party.Count; i++)
        {
            var pkmn = party.Members[i];
            int iy = 120 + i * 62;
            bool isSel = ModeIndex == 0 && PartyIndex == i;

            RenderHelper.DrawPlatinumPanel(26, iy, colWidth - 20, 54, isSel ? Palette.UiAccent : Palette.UiBackground);
            var icon = PixelArtGenerator.GetPokemonIcon(pkmn.Species.Name);
            Raylib.DrawTexture(icon, 34, iy + 6, Color.White);

            RenderHelper.DrawTextWithShadow(pkmn.DisplayName, 88, iy + 8, 16, isSel ? Color.White : Palette.TextDark);
            RenderHelper.DrawTextWithShadow($"Lv.{pkmn.Level}  HP:{pkmn.CurrentHP}/{pkmn.MaxHP}", 88, iy + 30, 14, isSel ? Color.White : Palette.TextDark);
        }

        // Box 1 Column (Right)
        int boxX = 24 + colWidth;
        Color boxColFill = ModeIndex == 1 ? Color.White : Palette.UiPanelBg;
        RenderHelper.DrawPlatinumPanel(boxX, 70, colWidth, listHeight, boxColFill);
        RenderHelper.DrawTextWithShadow($"STORAGE BOX 1 ({boxStorage.Count} Pokémon)", boxX + 16, 84, 18, Palette.UiAccent);

        if (boxStorage.Count == 0)
        {
            RenderHelper.DrawTextWithShadow("Box is empty.", boxX + 32, 140, 16, Color.Gray);
        }
        else
        {
            int maxBoxVis = 6;
            int offset = Math.Max(0, BoxIndex - maxBoxVis / 2);

            for (int i = 0; i < maxBoxVis && (i + offset) < boxStorage.Count; i++)
            {
                int bIdx = i + offset;
                var pkmn = boxStorage[bIdx];
                int iy = 120 + i * 62;
                bool isSel = ModeIndex == 1 && BoxIndex == bIdx;

                RenderHelper.DrawPlatinumPanel(boxX + 12, iy, colWidth - 24, 54, isSel ? Palette.UiAccent : Palette.UiBackground);
                var icon = PixelArtGenerator.GetPokemonIcon(pkmn.Species.Name);
                Raylib.DrawTexture(icon, boxX + 20, iy + 6, Color.White);

                RenderHelper.DrawTextWithShadow(pkmn.DisplayName, boxX + 74, iy + 8, 16, isSel ? Color.White : Palette.TextDark);
                RenderHelper.DrawTextWithShadow($"Lv.{pkmn.Level}", boxX + 74, iy + 30, 14, isSel ? Color.White : Palette.TextDark);
            }
        }
    }
}
