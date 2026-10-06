using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokédex (style guide, "Menu screens"): its list, the three pages of an entry, the search panel and the diploma.</summary>
internal static partial class ModernUi
{
    /// <summary>The pages of an entry as tabs: INFO in Blue, AREA in Green, SIZE in Gold.</summary>
    private static readonly (string Label, UiIcon Icon, Color Color)[] DexPages =
    {
        ("INFO", UiIcon.Dex, Blue), ("AREA", UiIcon.Pin, Green), ("SIZE", UiIcon.Size, Gold)
    };

    /// <summary>The player's height on the size page, in metres.</summary>
    public const float PlayerHeight = 1.4f;

    private static readonly Color Sea = new(196, 222, 244, 255);
    private static readonly Color Cream = new(252, 246, 228, 255);
    private static readonly Color DeepGold = new(176, 120, 30, 255);

    /// <summary>The tint that turns a sprite into a dark silhouette: black, a little see-through over the panel.</summary>
    private static readonly Color Silhouette = new(0, 0, 0, 214);

    public static void DrawPokedex(int sw, int sh, PokedexScreen dex, Pokedex pokedex, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("POKÉDEX");
        switch (dex.Focus)
        {
            case PokedexFocus.Entry:
                Hints(sw - Margin, 44, ("Left / Right", "Page"), ("Up / Down", "Species"), ("Esc", "Back"));
                break;
            case PokedexFocus.Search:
                Hints(sw - Margin, 44, ("Left / Right", "Change"), ("Enter", "Search"), ("Esc", "Close"));
                break;
            case PokedexFocus.Diploma:
                Hints(sw - Margin, 44, ("Z", "Close"));
                break;
            default:
                Hints(sw - Margin, 44, ("Z", "Entry"), ("Left / Right", "Jump 10"), ("Enter", "Search"), ("Esc", dex.Results != null ? "All" : "Back"));
                break;
        }

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        DexList(new Rectangle(Margin - slide, ContentTop, 760, ContentBottom - ContentTop), dex, pokedex);

        var detail = new Rectangle(Margin + 760 + Gutter + slide, ContentTop, sw - Margin * 2 - 760 - Gutter, ContentBottom - ContentTop);
        Panel(detail, 34);
        if (dex.Selected is { } entry)
        {
            if (!pokedex.IsSeen(entry.Species.DexNumber)) DexUnseen(detail, entry);
            else
            {
                var page = dex.Focus == PokedexFocus.Entry ? dex.Page : PokedexPage.Info;
                Tabs(new Rectangle(detail.X + 32, detail.Y + 28, detail.Width - 64, 64), DexPages, Array.IndexOf(PokedexScreen.Pages, page));
                bool owned = pokedex.IsCaught(entry.Species.DexNumber);
                switch (page)
                {
                    case PokedexPage.Area: DexArea(detail, entry.Species); break;
                    case PokedexPage.Size: DexSize(detail, entry.Species, owned, dex.Portrait); break;
                    default: DexInfo(detail, entry, owned); break;
                }
            }
        }

        if (dex.Focus == PokedexFocus.Search) DexSearch(sw, sh, dex);
        else if (dex.Focus == PokedexFocus.Diploma) Diploma(sw, sh, dex.ShownDiploma);
    }

    // ------------------------------------------------------------------ the list

    private static void DexList(Rectangle list, PokedexScreen dex, Pokedex pokedex)
    {
        Panel(list, 34);
        var mode = dex.Mode;
        float x = list.X + 44 + Tag(list.X + 44, list.Y + 50, mode == PokedexMode.Sinnoh ? "SINNOH" : "NATIONAL", mode == PokedexMode.Sinnoh ? Blue : Red, 40) + 36;
        if (dex.Results is { } query)
        {
            x = Field("FOUND", dex.Rows.Count.ToString(CultureInfo.InvariantCulture), x, list.Y + 28, 40, 56);
            Field("ORDER", PokedexSearch.NameOf(query.Order), x, list.Y + 28, 40);
        }
        else
        {
            x = Field("SEEN", pokedex.SeenIn(mode).ToString(CultureInfo.InvariantCulture), x, list.Y + 28, 40, 56);
            Field("CAUGHT", pokedex.CaughtIn(mode).ToString(CultureInfo.InvariantCulture), x, list.Y + 28, 40);
        }
        UiShapes.Fill(new Rectangle(list.X + 36, list.Y + 114, list.Width - 72, 3), 1.5f, Rule);

        const float top = 132;
        var rows = dex.Rows;
        if (rows.Count == 0)
        {
            EmptyNote(new Rectangle(list.X, list.Y + top, list.Width, list.Height - top), "Nothing found.");
            return;
        }
        var order = dex.Results?.Order ?? PokedexOrder.Number;
        for (int row = 0; row < PokedexScreen.VisibleRows && dex.FirstRow + row < rows.Count; row++)
        {
            int index = dex.FirstRow + row;
            var (number, s) = rows[index];
            var r = RowRect(list, row, top, scrolls: true);
            bool selected = index == dex.SelectedIndex;
            bool seen = pokedex.IsSeen(s.DexNumber), caught = pokedex.IsCaught(s.DexNumber);
            var ink = ListRow(r, selected, seen ? s.Name : "— — —", seen, nameX: 232);
            UiFonts.DrawCentered($"No. {number:D3}", r.X + 100, r.Y + r.Height / 2f, 24, selected ? Color.White : Muted, UiWeight.Black);
            if (seen) RowIcon(r, PixelArtGenerator.GetPokemonIcon(s.Name), 1f, selected, hop: Hop(selected));
            else UiShapes.Circle(new Vector2(r.X + 52, r.Y + r.Height / 2f), 8, Rule);
            if (PokedexSearch.SizeLabel(order, s) is { } size) RowValue(r, size, ink, 26);
            else if (caught)
            {
                var c = new Vector2(r.X + r.Width - 44, r.Y + r.Height / 2f);
                UiShapes.Circle(c, 18, selected ? Color.White : Red);
                UiIcons.Draw(UiIcon.Ball, c, 9, selected ? Red : Color.White, selected ? Color.White : Red);
            }
        }
        ScrollBar(list, dex.FirstRow, PokedexScreen.VisibleRows, rows.Count, top);
    }

    // ------------------------------------------------------------------ the pages of an entry

    /// <summary>A species not yet seen: a question mark on the disc and dashes for its name.</summary>
    private static void DexUnseen(Rectangle detail, PokedexEntry entry)
    {
        var disc = new Vector2(detail.X + 44 + 204, detail.Y + 44 + 204);
        BallDisc(disc, 204);
        float x = detail.X + 44 + 408 + 44;
        UiFonts.DrawCentered($"No. {entry.Number:D3}", x, detail.Y + 78, 30, Muted, UiWeight.Black);
        float qw = UiFonts.Measure("?", 220, UiWeight.Black);
        UiFonts.DrawCentered("?", disc.X - qw / 2f, disc.Y, 220, Rule, UiWeight.Black);
        UiFonts.DrawCentered("— — —", x, detail.Y + 138, 56, Muted, UiWeight.Black);
        UiShapes.Fill(new Rectangle(detail.X + 44, detail.Y + 496, detail.Width - 88, 3), 1.5f, Rule);
        UiFonts.Draw("Not seen yet.", detail.X + 44, detail.Y + 526, 30, Muted);
    }

    /// <summary>INFO: the sprite on a disc, number, name, category, types, height, weight and the entry.</summary>
    private static void DexInfo(Rectangle detail, PokedexEntry entry, bool owned)
    {
        var s = entry.Species;
        var disc = new Vector2(detail.X + 44 + 180, detail.Y + 120 + 180);
        BallDisc(disc, 180);
        PixelArt(PixelArtGenerator.GetPokemonSprite(s.Name, isBack: false), disc, 3, Hop(true) * 3);

        float x = detail.X + 44 + 360 + 44;
        UiFonts.DrawCentered($"No. {entry.Number:D3}", x, detail.Y + 160, 30, Muted, UiWeight.Black);
        float size = 56, room = detail.X + detail.Width - 44 - x;
        float nameW = UiFonts.Measure(s.Name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(s.Name, x, detail.Y + 218, size, Ink, UiWeight.Black);
        UiFonts.DrawCentered(owned ? $"{s.Category} Pokémon" : "????? Pokémon", x, detail.Y + 270, 28, Muted, UiWeight.ExtraBold);
        float px = x;
        px += TypePill(px, detail.Y + 304, s.PrimaryType, 42) + 10;
        if (s.SecondaryType.HasValue) TypePill(px, detail.Y + 304, s.SecondaryType.Value, 42);

        string height = owned ? s.Height.ToString("0.0", CultureInfo.InvariantCulture) + " m" : "?????";
        string weight = owned ? s.Weight.ToString("0.0", CultureInfo.InvariantCulture) + " kg" : "?????";
        float hx = Field("HEIGHT", height, x, detail.Y + 384, 38, 64);
        Field("WEIGHT", weight, hx, detail.Y + 384, 38);

        UiShapes.Fill(new Rectangle(detail.X + 44, detail.Y + 512, detail.Width - 88, 3), 1.5f, Rule);
        if (owned) DrawWrapped(s.DexEntry, detail.X + 44, detail.Y + 540, detail.Width - 88, 30, Ink, 42);
        else UiFonts.Draw("Catch one to record what is known about it.", detail.X + 44, detail.Y + 540, 30, Muted);
    }

    /// <summary>
    /// AREA: Sinnoh's overworld, one square per chunk, with the places the species lives lit, and beside it those
    /// places and how the species is met in each.
    /// </summary>
    private static void DexArea(Rectangle detail, PokemonSpecies s)
    {
        var habitats = Habitats.Sinnoh;
        var places = habitats?.Of(s.Name) ?? Array.Empty<Habitat>();
        var map = new Rectangle(detail.X + 44, detail.Y + 124, 600, 500);
        RegionMap(map, 20, new HashSet<(int, int)>(places.SelectMany(p => p.Cells)));

        // The places, in the region's order, with how the species is met there
        float lx = map.X + map.Width + 28, room = detail.X + detail.Width - 44 - lx, ly = map.Y;
        Label("WHERE IT LIVES", lx, ly);
        ly += 40;
        if (places.Count == 0)
        {
            UiFonts.Draw("Area unknown.", lx, ly, 30, Muted, UiWeight.ExtraBold);
            return;
        }
        float bottom = detail.Y + detail.Height - 44;
        for (int i = 0; i < places.Count; i++)
        {
            var lines = Wrap(WaysOf(places[i].Ways), room, 22);
            float need = 34 + lines.Count * 28 + 14;
            if (ly + need > bottom)
            {
                UiFonts.Draw($"and {places.Count - i} more", lx, ly, 24, Muted, UiWeight.ExtraBold);
                break;
            }
            UiFonts.Draw(places[i].Name, lx, ly, 28, Ink, UiWeight.Black);
            for (int l = 0; l < lines.Count; l++) UiFonts.Draw(lines[l], lx, ly + 36 + l * 28, 22, Muted, UiWeight.Bold);
            ly += need;
        }

        // Under the map, how many places in all
        string summary = places.Count == 1 ? $"{s.Name} lives in one place." : $"{s.Name} lives in {places.Count} places.";
        UiFonts.Draw(summary, map.X, map.Y + map.Height + 28, 28, Ink, UiWeight.ExtraBold);
        float kx = map.X;
        float ky = map.Y + map.Height + 92;
        UiShapes.Fill(new Rectangle(kx, ky, 26, 26), 5, Selection);
        kx += 38 + UiFonts.Measure("Where it lives", 22, UiWeight.ExtraBold);
        UiFonts.DrawCentered("Where it lives", map.X + 38, ky + 13, 22, Muted, UiWeight.ExtraBold);
        UiShapes.Shape(new Rectangle(kx + 30, ky, 26, 26), 5, Color.White, Color.White, Rule, 2);
        UiFonts.DrawCentered("Town", kx + 68, ky + 13, 22, Muted, UiWeight.ExtraBold);
    }

    /// <summary>
    /// Sinnoh's overworld, one square per chunk (water a pale blue, land the Disc colour, towns white), in a
    /// rectangle, its empty north left out; the chunks <paramref name="lit"/> breathing in the Selection colour and
    /// the one at <paramref name="ring"/> ringed in white. Used by the Pokédex's area page and by Fly.
    /// </summary>
    public static void RegionMap(Rectangle map, float cell, IReadOnlySet<(int X, int Y)> lit, (int X, int Y)? ring = null)
    {
        UiShapes.Fill(map, 18, Sea);
        var habitats = Habitats.Sinnoh;
        if (habitats == null) return;
        // The map's rows from the first with any ground, so the empty north doesn't take room
        int first = Enumerable.Range(0, habitats.Height).FirstOrDefault(y => Enumerable.Range(0, habitats.Width).Any(x => habitats.LookAt(x, y) != ' '));
        int rows = Math.Min(habitats.Height - first, (int)(map.Height / cell));
        float ox = map.X + (map.Width - habitats.Width * cell) / 2f, oy = map.Y + (map.Height - rows * cell) / 2f;
        float pulse = 0.72f + 0.28f * (0.5f + 0.5f * MathF.Sin((float)FrameClock.Now * 3.2f));
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < habitats.Width; x++)
            {
                char look = habitats.LookAt(x, first + y);
                var r = new Rectangle(ox + x * cell, oy + y * cell, cell, cell);
                if (look == '.') Raylib.DrawRectangleRec(r, Disc);
                else if (look == 'T') Raylib.DrawRectangleRec(r, Color.White);
                if (lit.Contains((x, first + y)))
                    UiShapes.Fill(new Rectangle(r.X + 2, r.Y + 2, cell - 4, cell - 4), 4, Selection with { A = (byte)(255 * pulse) });
            }
        if (ring is var (rx, ry) && ry >= first && ry < first + rows)
        {
            var c = new Vector2(ox + (rx + 0.5f) * cell, oy + (ry - first + 0.5f) * cell);
            UiShapes.Ring(c, cell * 0.62f, 4f, Color.White);
            UiShapes.Ring(c, cell * 0.62f + 3f, 2f, Ink);
        }
    }

    /// <summary>
    /// Fly (style guide, "Fly"): the map of Sinnoh on the left with the town under the cursor lit and the player's
    /// chunk ringed, and the towns Fly reaches as a list on the right.
    /// </summary>
    public static void DrawFly(int sw, int sh, IReadOnlyList<SpawnLocation> towns, int cursor, int top, (int X, int Y)? here, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("FLY");
        Hints(sw - 64, 44, ("Z", "Fly"), ("Esc", "Back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;

        var panel = new Rectangle(64 - slide, 132, 1100, 900);
        Panel(panel, 34);
        var lit = new HashSet<(int, int)>();
        if (cursor < towns.Count) lit.Add((towns[cursor].X / 32, towns[cursor].Y / 32));
        RegionMap(new Rectangle(panel.X + 28, panel.Y + 28, panel.Width - 56, panel.Height - 56), 28, lit,
            here is var (hx, hy) ? (hx / 32, hy / 32) : null);

        var list = new Rectangle(1196 + slide, 132, 660, 900);
        Panel(list, 34);
        if (towns.Count == 0)
        {
            UiFonts.DrawCentered("Nowhere to fly to yet.", list.X + 44, list.Y + 60, 32, Muted, UiWeight.ExtraBold);
            return;
        }
        const int visible = FlyScreen.VisibleRows;
        for (int i = top; i < Math.Min(towns.Count, top + visible); i++)
        {
            var r = new Rectangle(list.X + 20, list.Y + 20 + (i - top) * 92, list.Width - 40, 84);
            ListRow(r, i == cursor, PlaceName(towns[i].Area), nameX: 40);
        }
        ScrollBar(list, top, visible, towns.Count);
    }

    /// <summary>A place's name from its key: <c>eterna_city</c> is Eterna City.</summary>
    public static string PlaceName(string key) => string.Join(' ', key.Split('_').Select(w =>
        w == "pokemon" ? "Pokémon" : w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));

    /// <summary>How a species is met in a place, in words: "Grass in the morning and at night · Surfing · Old and Good Rod".</summary>
    public static string WaysOf(HabitatWays ways)
    {
        var parts = new List<string>();
        var times = new List<string>();
        if (ways.HasFlag(HabitatWays.Morning)) times.Add("morning");
        if (ways.HasFlag(HabitatWays.Day)) times.Add("day");
        if (ways.HasFlag(HabitatWays.Night)) times.Add("night");
        if (times.Count == 3) parts.Add("Grass, all day");
        else if (times.Count > 0) parts.Add("Grass: " + string.Join(", ", times));
        if (ways.HasFlag(HabitatWays.Surfing)) parts.Add("Surfing");
        var rods = new List<string>();
        if (ways.HasFlag(HabitatWays.OldRod)) rods.Add("Old");
        if (ways.HasFlag(HabitatWays.GoodRod)) rods.Add("Good");
        if (ways.HasFlag(HabitatWays.SuperRod)) rods.Add("Super");
        if (rods.Count > 0) parts.Add(string.Join(", ", rods) + (rods.Count == 1 ? " Rod" : " Rods"));
        return string.Join(" · ", parts);
    }

    /// <summary>SIZE: the species and the player as two dark silhouettes on one line, at their real heights.</summary>
    private static void DexSize(Rectangle detail, PokemonSpecies s, bool owned, Texture2D? portrait)
    {
        if (!owned)
        {
            EmptyNote(new Rectangle(detail.X, detail.Y + 92, detail.Width, detail.Height - 92), "Catch one to compare its size.");
            return;
        }
        float hx = Field("HEIGHT", s.Height.ToString("0.0", CultureInfo.InvariantCulture) + " m", detail.X + 44, detail.Y + 124, 38, 64);
        Field("WEIGHT", s.Weight.ToString("0.0", CultureInfo.InvariantCulture) + " kg", hx, detail.Y + 124, 38);

        float floor = detail.Y + detail.Height - 150;
        UiShapes.Fill(new Rectangle(detail.X + 60, floor, detail.Width - 120, 4), 2, Rule);
        float tallest = MathF.Max(PlayerHeight, MathF.Max(0.1f, s.Height));
        float perMetre = 520f / tallest;

        void Stand(Texture2D art, float metres, float centerX, string name)
        {
            var bounds = TextureBounds.Of(art);
            float scale = metres * perMetre / bounds.Height;
            var dest = new Rectangle(centerX - (bounds.X + bounds.Width / 2f) * scale, floor - (bounds.Y + bounds.Height) * scale, art.Width * scale, art.Height * scale);
            // Every texel's colour multiplied to nothing and its opacity kept: a flat silhouette of the sprite's own shape
            Raylib.DrawTexturePro(art, new Rectangle(0, 0, art.Width, art.Height), dest, Vector2.Zero, 0f, Silhouette);
            float w = UiFonts.Measure(name, 28, UiWeight.Black);
            UiFonts.DrawCentered(name, centerX - w / 2f, floor + 44, 28, Ink, UiWeight.Black);
            string text = metres.ToString("0.0", CultureInfo.InvariantCulture) + " m";
            float tw = UiFonts.Measure(text, 24, UiWeight.ExtraBold);
            UiFonts.DrawCentered(text, centerX - tw / 2f, floor + 82, 24, Muted, UiWeight.ExtraBold);
        }

        if (portrait is { } player) Stand(player, PlayerHeight, detail.X + detail.Width * 0.3f, PlayerIdentity.Name);
        Stand(PixelArtGenerator.GetPokemonSprite(s.Name, isBack: false), s.Height, detail.X + detail.Width * 0.68f, s.Name);
    }

    // ------------------------------------------------------------------ the search panel

    private static void DexSearch(int sw, int sh, PokedexScreen dex)
    {
        Dim(sw, sh, 150);
        var rows = dex.SearchRows;
        const float pitch = 96, height = 84;
        float panelH = 116 + (rows.Count - 1) * pitch + 92 + 48;
        var panel = new Rectangle(sw / 2f - 620, sh / 2f - panelH / 2f + 20, 1240, panelH);
        Panel(panel, 36);
        UiFonts.Draw("Search the Pokédex", panel.X + 56, panel.Y + 36, 44, Ink, UiWeight.Black);

        float y = panel.Y + 116;
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == PokedexSearchRow.Buttons)
            {
                var buttons = dex.Buttons;
                const float gap = 28;
                float bw = (panel.Width - 112 - gap * (buttons.Count - 1)) / buttons.Count;
                for (int b = 0; b < buttons.Count; b++)
                {
                    var (label, color) = buttons[b] switch
                    {
                        PokedexSearchButton.Search => ("SEARCH", Green),
                        PokedexSearchButton.Reset => ("RESET", Blue),
                        _ => ("DIPLOMA", Gold)
                    };
                    Button(new Rectangle(panel.X + 56 + b * (bw + gap), y + 12, bw, 80), 40, color, label, 30, i == dex.SearchIndex && b == dex.ButtonIndex);
                }
                continue;
            }
            string name = row switch
            {
                PokedexSearchRow.Mode => "Pokédex",
                PokedexSearchRow.Order => "Order",
                PokedexSearchRow.Name => "Name",
                PokedexSearchRow.Type1 => "Type",
                PokedexSearchRow.Type2 => "Second type",
                _ => "Shape"
            };
            ValueRow(new Rectangle(panel.X + 40, y, panel.Width - 80, height), name, dex.ValueOf(row), i == dex.SearchIndex, 440);
            y += pitch;
        }
    }

    // ------------------------------------------------------------------ the diploma

    /// <summary>The diploma for a complete Pokédex: a cream certificate with a gold double rule and a seal.</summary>
    public static void Diploma(int sw, int sh, PokedexMode mode)
    {
        Dim(sw, sh, 170);
        var r = new Rectangle(sw / 2f - 600, sh / 2f - 400, 1200, 800);
        UiShapes.Shadow(r, 20, 30, new Vector2(0, 10), ShadowColor);
        UiShapes.Shape(r, 20, Cream, Darker(Cream, 0.04f), Gold, 8);
        UiShapes.Shape(new Rectangle(r.X + 26, r.Y + 26, r.Width - 52, r.Height - 52), 12, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), DeepGold, 3);

        void Centred(string text, float y, float size, Color color, UiWeight weight)
        {
            float w = UiFonts.Measure(text, size, weight);
            UiFonts.DrawCentered(text, r.X + (r.Width - w) / 2f, y, size, color, weight);
        }

        Centred("DIPLOMA", r.Y + 118, 84, DeepGold, UiWeight.Black);
        Centred(mode == PokedexMode.Sinnoh ? "SINNOH POKÉDEX" : "NATIONAL POKÉDEX", r.Y + 196, 34, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(r.X + r.Width / 2f - 160, r.Y + 234, 320, 3), 1.5f, Gold);
        Centred(PlayerIdentity.Name, r.Y + 312, 64, Ink, UiWeight.Black);
        string line = mode == PokedexMode.Sinnoh
            ? $"has seen every one of the {Pokedex.Entries(PokedexMode.Sinnoh).Count} Pokémon of the Sinnoh Pokédex. A full record of the region's wild life, kept with care."
            : $"has caught every Pokémon of the National Pokédex that a trainer can meet. The work of a true Pokémon Master.";
        var lines = Wrap(line, 900, 32, UiWeight.ExtraBold);
        for (int i = 0; i < lines.Count; i++) Centred(lines[i], r.Y + 400 + i * 46, 32, Ink, UiWeight.ExtraBold);

        string day = DateTime.Now.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        UiFonts.Draw(day, r.X + 92, r.Y + r.Height - 120, 28, Muted, UiWeight.ExtraBold);

        // The seal: a gold medallion with the Pokédex's sign
        var seal = new Vector2(r.X + r.Width - 170, r.Y + r.Height - 160);
        UiShapes.Circle(seal, 74, DeepGold);
        UiShapes.Circle(seal, 64, Gold);
        UiShapes.Ring(seal, 52, 3, Lighter(Gold, 0.45f));
        UiIcons.Draw(UiIcon.Dex, seal, 26, Color.White, Gold);
    }
}
