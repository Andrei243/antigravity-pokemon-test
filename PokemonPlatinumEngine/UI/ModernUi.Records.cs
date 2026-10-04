using System;
using System.Globalization;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The screens that keep the player's records: the Pokédex, the Trainer Card and saving.</summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ Pokédex

    /// <summary>
    /// The Pokédex: the species in number order on the left under the SEEN and CAUGHT counts, the chosen one
    /// on the right with what is known of it.
    /// </summary>
    public static void DrawPokedex(int sw, int sh, PokedexScreen dex, Pokedex pokedex, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("POKÉDEX");
        Hints(sw - Margin, 44, ("Left / Right", "Jump 10"), ("Esc", "Back"));

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        var species = dex.Species;

        // ---- The list
        var list = new Rectangle(Margin - slide, ContentTop, 760, ContentBottom - ContentTop);
        Panel(list, 34);
        float fx = Field("SEEN", pokedex.SeenCount.ToString(), list.X + 48, list.Y + 28, 40, 72);
        Field("CAUGHT", pokedex.CaughtCount.ToString(), fx, list.Y + 28, 40);
        UiShapes.Fill(new Rectangle(list.X + 36, list.Y + 114, list.Width - 72, 3), 1.5f, Rule);

        const float top = 132;
        for (int row = 0; row < PokedexScreen.VisibleRows && dex.FirstRow + row < species.Count; row++)
        {
            int index = dex.FirstRow + row;
            var s = species[index];
            var r = RowRect(list, row, top, scrolls: true);
            bool selected = index == dex.SelectedIndex;
            bool seen = pokedex.IsSeen(s.DexNumber), caught = pokedex.IsCaught(s.DexNumber);
            var ink = ListRow(r, selected, seen ? s.Name : "— — —", seen, nameX: 232);
            UiFonts.DrawCentered($"No. {s.DexNumber:D3}", r.X + 100, r.Y + r.Height / 2f, 24, selected ? Color.White : Muted, UiWeight.Black);
            if (seen) RowIcon(r, PixelArtGenerator.GetPokemonIcon(s.Name), 1f, selected, hop: Hop(selected));
            else UiShapes.Circle(new Vector2(r.X + 52, r.Y + r.Height / 2f), 8, Rule);
            if (caught)
            {
                var c = new Vector2(r.X + r.Width - 44, r.Y + r.Height / 2f);
                UiShapes.Circle(c, 18, selected ? Color.White : Red);
                UiIcons.Draw(UiIcon.Ball, c, 9, selected ? Red : Color.White, selected ? Color.White : Red);
            }
            _ = ink;
        }
        ScrollBar(list, dex.FirstRow, PokedexScreen.VisibleRows, species.Count, top);

        // ---- The chosen species
        var detail = new Rectangle(Margin + 760 + Gutter + slide, ContentTop, sw - Margin * 2 - 760 - Gutter, ContentBottom - ContentTop);
        Panel(detail, 34);
        if (dex.SelectedIndex >= species.Count) return;
        var chosen = species[dex.SelectedIndex];
        bool known = pokedex.IsSeen(chosen.DexNumber), owned = pokedex.IsCaught(chosen.DexNumber);

        var disc = new Vector2(detail.X + 44 + 204, detail.Y + 44 + 204);
        BallDisc(disc, 204);
        float x = detail.X + 44 + 408 + 44;
        UiFonts.DrawCentered($"No. {chosen.DexNumber:D3}", x, detail.Y + 78, 30, Muted, UiWeight.Black);
        if (!known)
        {
            float qw = UiFonts.Measure("?", 220, UiWeight.Black);
            UiFonts.DrawCentered("?", disc.X - qw / 2f, disc.Y, 220, Rule, UiWeight.Black);
            UiFonts.DrawCentered("— — —", x, detail.Y + 138, 56, Muted, UiWeight.Black);
            UiShapes.Fill(new Rectangle(detail.X + 44, detail.Y + 496, detail.Width - 88, 3), 1.5f, Rule);
            UiFonts.Draw("Not seen yet.", detail.X + 44, detail.Y + 526, 30, Muted);
            return;
        }

        PixelArt(PixelArtGenerator.GetPokemonSprite(chosen.Name, isBack: false), disc, 3, Hop(true) * 3);
        float size = 56, room = detail.X + detail.Width - 44 - x;
        float nameW = UiFonts.Measure(chosen.Name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(chosen.Name, x, detail.Y + 138, size, Ink, UiWeight.Black);
        UiFonts.DrawCentered(owned ? $"{chosen.Category} Pokémon" : "????? Pokémon", x, detail.Y + 192, 28, Muted, UiWeight.ExtraBold);
        float px = x;
        px += TypePill(px, detail.Y + 228, chosen.PrimaryType, 42) + 10;
        if (chosen.SecondaryType.HasValue) TypePill(px, detail.Y + 228, chosen.SecondaryType.Value, 42);

        string height = owned ? chosen.Height.ToString("0.0", CultureInfo.InvariantCulture) + " m" : "?????";
        string weight = owned ? chosen.Weight.ToString("0.0", CultureInfo.InvariantCulture) + " kg" : "?????";
        float hx = Field("HEIGHT", height, x, detail.Y + 320, 38, 64);
        Field("WEIGHT", weight, hx, detail.Y + 320, 38);

        UiShapes.Fill(new Rectangle(detail.X + 44, detail.Y + 496, detail.Width - 88, 3), 1.5f, Rule);
        if (owned) DrawWrapped(chosen.DexEntry, detail.X + 44, detail.Y + 526, detail.Width - 88, 30, Ink, 42);
        else UiFonts.Draw("Catch one to record what is known about it.", detail.X + 44, detail.Y + 526, 30, Muted);
    }

    // ------------------------------------------------------------------ Trainer Card

    /// <summary>
    /// The Trainer Card: one wide card that rises into place, a Blue band across its top with the name and ID
    /// number, the player's records on the left, their field sprite on the right and the badges along the bottom.
    /// </summary>
    public static void DrawTrainerCard(int sw, int sh, TrainerCardInfo info, Texture2D? portrait, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("TRAINER CARD");
        Hints(sw - Margin, 44, ("Esc", "Back"));

        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var card = new Rectangle(200, 150 + rise, sw - 400, 840);
        Panel(card, 44);

        var band = new Rectangle(card.X + 18, card.Y + 18, card.Width - 36, 108);
        UiShapes.Shape(band, 30, Lighter(Blue, 0.16f), Darker(Blue, 0.1f), Darker(Blue, 0.32f), 3);
        UiFonts.DrawCentered(info.Name, band.X + 44, band.Y + band.Height / 2f, 52, Color.White, UiWeight.Black);
        string id = $"ID No. {TrainerCardScreen.FormatId(info.TrainerId)}";
        UiFonts.DrawCentered(id, band.X + band.Width - 44 - UiFonts.Measure(id, 32, UiWeight.Black), band.Y + band.Height / 2f, 32, Color.White, UiWeight.Black);

        // ---- Records
        float x = card.X + 72, y = card.Y + 168;
        Label("MONEY", x, y);
        Money(x, y + 24 + 40 * 0.56f, info.Money, 40, Ink);
        Field("TIME PLAYED", TitleScreen.FormatPlayTime(info.PlayTimeSeconds), x + 440, y);
        Field("POKÉMON CAUGHT", info.Caught.ToString(), x, y + 124);
        Field("POKÉMON SEEN", info.Seen.ToString(), x + 440, y + 124);
        Field("ADVENTURE STARTED", info.Started is { } day ? day.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) : "—", x, y + 248);

        // ---- The player's own field sprite, on a pale plate
        var plate = new Rectangle(card.X + card.Width - 72 - 340, card.Y + 152, 340, 400);
        UiShapes.Shape(plate, 30, Disc, Disc, Rule, 4);
        UiShapes.Fill(new Rectangle(plate.X + 28, plate.Y + plate.Height - 64, plate.Width - 56, 16), 8, Rule);
        if (portrait is { } sprite)
        {
            const int scale = 6;
            float w = sprite.Width * scale, h = sprite.Height * scale;
            Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
                new Rectangle(MathF.Round(plate.X + (plate.Width - w) / 2f), MathF.Round(plate.Y + plate.Height - 40 - h), w, h), Vector2.Zero, 0f, Color.White);
        }

        // ---- Badges
        float by = card.Y + 590;
        UiShapes.Fill(new Rectangle(card.X + 72, by, card.Width - 144, 3), 1.5f, Rule);
        Label($"BADGES  {TitleScreen.CountBadges(info.Badges)} / 8", card.X + 72, by + 24);
        float step = (card.Width - 144 - 88) / 7f;
        for (int b = 0; b < 8; b++)
        {
            bool earned = (info.Badges & (1 << b)) != 0;
            var c = new Vector2(card.X + 72 + 44 + b * step, by + 116);
            Badge(c, 44, b, earned);
            string name = BadgeNames[b];
            UiFonts.DrawCentered(name, c.X - UiFonts.Measure(name, 22, UiWeight.Black) / 2f, c.Y + 76, 22, earned ? Ink : Muted, UiWeight.Black);
        }
    }

    // ------------------------------------------------------------------ saving

    /// <summary>
    /// Saving: the save as it will be, the question under it with SAVE and CANCEL, and once it is done, a line
    /// saying so. <paramref name="shown"/> slides the panel up into place.
    /// </summary>
    public static void DrawSave(int sw, int sh, SaveData save, int answer, bool saved, float shown)
    {
        Dim(sw, sh, (int)(150 * shown));
        var r = new Rectangle(sw / 2f - 540, 140 + (1f - shown) * 60f, 1080, 780);
        Panel(r, 36);
        SaveSummary(new Rectangle(r.X, r.Y, r.Width, SaveSummaryHeight), save, "SAVE");
        UiShapes.Fill(new Rectangle(r.X + 48, r.Y + SaveSummaryHeight + 12, r.Width - 96, 3), 1.5f, Rule);

        float cy = r.Y + SaveSummaryHeight + 82;
        if (saved)
        {
            float tw = Tag(r.X + 56, cy + 34, "SAVED", Green, 52);
            UiFonts.DrawCentered($"{save.PlayerName} saved the game.", r.X + 56 + tw + 28, cy + 60, 40, Ink, UiWeight.ExtraBold);
            return;
        }

        UiFonts.DrawCentered("Save the game?", r.X + 56, cy, 44, Ink, UiWeight.Black);
        const float gap = 32;
        float bw = (r.Width - 112 - gap) / 2f;
        Button(new Rectangle(r.X + 56, r.Y + r.Height - 56 - 84, bw, 84), 42, Green, SaveScreen.Answers[0], 32, answer == 0);
        Button(new Rectangle(r.X + 56 + bw + gap, r.Y + r.Height - 56 - 84, bw, 84), 42, Blue, SaveScreen.Answers[1], 32, answer == 1);
    }
}
