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
    private static (string Label, UiIcon Icon, Color Color)[] DexPages => new[]
    {
        (Text.Of("pokedex.page.Info"), UiIcon.Dex, Blue), (Text.Of("pokedex.page.Area"), UiIcon.Pin, Green), (Text.Of("pokedex.page.Size"), UiIcon.Size, Gold)
    };

    /// <summary>The player's height on the size page, in metres.</summary>
    public const float PlayerHeight = 1.4f;

    /// <summary>A Pokédex number as the list and the entries write it: No. 001.</summary>
    public static string DexNumber(int number) => Text.Of("summary.number", ("number", number.ToString("D3", CultureInfo.InvariantCulture)));

    /// <summary>A height in metres, to a tenth: 0.4 m.</summary>
    public static string Metres(float metres) => Text.Of("pokedex.metres", ("n", Text.Decimal(metres, 1)));

    /// <summary>A weight in kilograms, to a tenth: 10.2 kg.</summary>
    public static string Kilograms(float kilograms) => Text.Of("pokedex.kilograms", ("n", Text.Decimal(kilograms, 1)));

    /// <summary>The colour of a Pokédex's tag (plan 18 · W3): Sinnoh's Blue, Kanto's Green, the National one's Red, and Gold for a Pokédex whose region has no colour of its own yet.</summary>
    public static Color DexColour(string dex) => dex switch { Pokedex.Sinnoh => Blue, "kanto" => Green, Pokedex.National => Red, _ => Gold };

    private static readonly Color Cream = new(252, 246, 228, 255);
    private static readonly Color DeepGold = new(176, 120, 30, 255);

    /// <summary>The tint that turns a sprite into a dark silhouette: black, a little see-through over the panel.</summary>
    private static readonly Color Silhouette = new(0, 0, 0, 214);

    public static void DrawPokedex(int sw, int sh, PokedexScreen dex, Pokedex pokedex, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("pokedex.title"));
        switch (dex.Focus)
        {
            case PokedexFocus.Entry:
                Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.page"), Hint("shared.keys.upDown", "shared.hints.species"), Hint("shared.keys.esc", "shared.hints.back"));
                break;
            case PokedexFocus.Search:
                Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.change"), Hint("shared.keys.enter", "shared.hints.search"), Hint("shared.keys.esc", "shared.hints.close"));
                break;
            case PokedexFocus.Diploma:
                Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.close"));
                break;
            default:
                Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.entry"), Hint("shared.keys.leftRight", "shared.hints.jump"), Hint("shared.keys.enter", "shared.hints.search"),
                    Hint("shared.keys.esc", dex.Results != null ? "shared.hints.all" : "shared.hints.back"));
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
                    case PokedexPage.Area: DexArea(detail, entry.Species, dex.AreaRegion); break;
                    case PokedexPage.Size: DexSize(detail, entry.Species, owned, dex.Portrait); break;
                    default: DexInfo(detail, entry, owned, dex.EntryShown(entry.Species)); break;
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
        string modeKey = Pokedex.KeyOf(mode);
        float x = list.X + 44 + Tag(list.X + 44, list.Y + 50, Text.Of($"pokedex.mode.{modeKey}"), DexColour(mode), 40) + 36;
        if (dex.Results is { } query)
        {
            x = Field(Text.Of("pokedex.found"), dex.Rows.Count.ToString(CultureInfo.InvariantCulture), x, list.Y + 28, 40, 56);
            Field(Text.Of("pokedex.order"), PokedexSearch.NameOf(query.Order), x, list.Y + 28, 40);
        }
        else
        {
            x = Field(Text.Of("pokedex.seen"), pokedex.SeenIn(mode).ToString(CultureInfo.InvariantCulture), x, list.Y + 28, 40, 56);
            Field(Text.Of("pokedex.caught"), pokedex.CaughtIn(mode).ToString(CultureInfo.InvariantCulture), x, list.Y + 28, 40);
        }
        UiShapes.Fill(new Rectangle(list.X + 36, list.Y + 114, list.Width - 72, 3), 1.5f, Rule);

        const float top = 132;
        var rows = dex.Rows;
        if (rows.Count == 0)
        {
            EmptyNote(new Rectangle(list.X, list.Y + top, list.Width, list.Height - top), Text.Of("pokedex.nothingFound"));
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
            var ink = ListRow(r, selected, seen ? s.DisplayName : "— — —", seen, nameX: 232);
            UiFonts.DrawCentered(DexNumber(number), r.X + 100, r.Y + r.Height / 2f, 24, selected ? Color.White : Muted, UiWeight.Black);
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
        UiFonts.DrawCentered(DexNumber(entry.Number), x, detail.Y + 78, 30, Muted, UiWeight.Black);
        float qw = UiFonts.Measure("?", 220, UiWeight.Black);
        UiFonts.DrawCentered("?", disc.X - qw / 2f, disc.Y, 220, Rule, UiWeight.Black);
        UiFonts.DrawCentered("— — —", x, detail.Y + 138, 56, Muted, UiWeight.Black);
        UiShapes.Fill(new Rectangle(detail.X + 44, detail.Y + 496, detail.Width - 88, 3), 1.5f, Rule);
        UiFonts.Draw(Text.Of("pokedex.notSeen"), detail.X + 44, detail.Y + 526, 30, Muted);
    }

    /// <summary>
    /// INFO: the sprite on a disc, number, name, category, types, height, weight, habitat and the written entry
    /// (<see cref="DexEntries"/>; the <paramref name="which"/>th of its entries, the generated sentence where the file
    /// has none), with a dot for each entry at the rule's right end, the one shown lit.
    /// </summary>
    private static void DexInfo(Rectangle detail, PokedexEntry entry, bool owned, int which = 0)
    {
        var s = entry.Species;
        var disc = new Vector2(detail.X + 44 + 180, detail.Y + 120 + 180);
        BallDisc(disc, 180);
        PixelArt(PixelArtGenerator.GetPokemonSprite(s.Name, isBack: false), disc, 3, Hop(true) * 3);

        float x = detail.X + 44 + 360 + 44;
        UiFonts.DrawCentered(DexNumber(entry.Number), x, detail.Y + 160, 30, Muted, UiWeight.Black);
        float size = 56, room = detail.X + detail.Width - 44 - x;
        float nameW = UiFonts.Measure(s.DisplayName, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(s.DisplayName, x, detail.Y + 218, size, Ink, UiWeight.Black);
        UiFonts.DrawCentered(owned ? Text.Of("pokedex.kind", ("category", s.Category)) : Text.Of("pokedex.unknownKind"), x, detail.Y + 270, 28, Muted, UiWeight.ExtraBold);
        float px = x;
        px += TypePill(px, detail.Y + 304, s.PrimaryType, 42) + 10;
        if (s.SecondaryType.HasValue) TypePill(px, detail.Y + 304, s.SecondaryType.Value, 42);

        // Height, weight and the kind of place it lives in, three captions over values; the habitat's value is made
        // smaller where it wouldn't fit before the panel's edge
        string height = owned ? Metres(s.Height) : "?????";
        string weight = owned ? Kilograms(s.Weight) : "?????";
        var habitat = DexEntries.HabitatOf(s.Name);
        float hx = Field(Text.Of("pokedex.height"), height, x, detail.Y + 384, 34, 44);
        hx = Field(Text.Of("pokedex.weight"), weight, hx, detail.Y + 384, 34, habitat.HasValue ? 44 : 64);
        if (habitat is { } kind)
        {
            string word = owned ? DexEntries.Word(kind) : "?????";
            float right = detail.X + detail.Width - 44, wordSize = 34;
            float wordW = UiFonts.Measure(word, wordSize, UiWeight.Black);
            if (hx + wordW > right) wordSize = MathF.Max(22, MathF.Floor(wordSize * (right - hx) / wordW));
            Field(Text.Of("pokedex.habitat"), word, hx, detail.Y + 384, wordSize);
        }

        // The rule under them, ending in a dot for each written entry where there is more than one, the one shown Blue
        int count = owned ? DexEntries.CountFor(s) : 1;
        float dots = count > 1 ? (count - 1) * 26 + 16 + 20 : 0, last = detail.X + detail.Width - 44 - 8;
        UiShapes.Fill(new Rectangle(detail.X + 44, detail.Y + 512, detail.Width - 88 - dots, 3), 1.5f, Rule);
        for (int i = 0; i < count && count > 1; i++)
            UiShapes.Circle(new Vector2(last - (count - 1 - i) * 26, detail.Y + 513.5f), 8, i == which % count ? Blue : Rule);
        if (owned) DrawWrapped(DexEntries.EntryFor(s, which), detail.X + 44, detail.Y + 540, detail.Width - 88, 30, Ink, 42);
        else UiFonts.Draw(Text.Of("pokedex.catchToRecord"), detail.X + 44, detail.Y + 540, 30, Muted);
    }

    /// <summary>
    /// AREA: the map of Sinnoh (<see cref="RegionMap"/>, the Town Map's own picture, smaller), with the places the
    /// species lives lit, and beside it those places and how the species is met in each.
    /// </summary>
    private static void DexArea(Rectangle detail, PokemonSpecies s, Region? region)
    {
        var habitats = region == null ? null : Habitats.Of(region);
        var places = habitats?.Of(s.Name) ?? Array.Empty<Habitat>();
        var map = new Rectangle(detail.X + 44, detail.Y + 124, 600, 500);
        RegionMap(map, 20, new HashSet<(int, int)>(places.SelectMany(p => p.Cells)), of: region);

        // The places, in the region's order, with how the species is met there
        float lx = map.X + map.Width + 28, room = detail.X + detail.Width - 44 - lx, ly = map.Y;
        Label(Text.Of("pokedex.whereItLives"), lx, ly);
        ly += 40;
        if (places.Count == 0)
        {
            UiFonts.Draw(Text.Of("pokedex.areaUnknown"), lx, ly, 30, Muted, UiWeight.ExtraBold);
            return;
        }
        float bottom = detail.Y + detail.Height - 44;
        for (int i = 0; i < places.Count; i++)
        {
            var lines = Wrap(WaysOf(places[i]), room, 22);
            float need = 34 + lines.Count * 28 + 14;
            if (ly + need > bottom)
            {
                UiFonts.Draw(Text.Of("pokedex.more", ("count", places.Count - i)), lx, ly, 24, Muted, UiWeight.ExtraBold);
                break;
            }
            UiFonts.Draw(places[i].Name, lx, ly, 28, Ink, UiWeight.Black);
            for (int l = 0; l < lines.Count; l++) UiFonts.Draw(lines[l], lx, ly + 36 + l * 28, 22, Muted, UiWeight.Bold);
            ly += need;
        }

        // Under the map, how many places in all
        string summary = Text.Of("pokedex.livesIn", ("name", s.DisplayName), ("n", places.Count));
        UiFonts.Draw(summary, map.X, map.Y + map.Height + 28, 28, Ink, UiWeight.ExtraBold);
        float kx = map.X;
        float ky = map.Y + map.Height + 92;
        UiShapes.Shape(new Rectangle(kx, ky, 26, 26), 5, Selection, Selection, Color.White, 2.5f);
        UiFonts.DrawCentered(Text.Of("pokedex.mapKey"), map.X + 38, ky + 13, 22, Muted, UiWeight.ExtraBold);
    }

    /// <summary>A place's name from its key: <c>eterna_city</c> is Eterna City.</summary>
    public static string PlaceName(string key) => string.Join(' ', key.Split('_').Select(w =>
        w == "pokemon" ? "Pokémon" : w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));

    /// <summary>How a species is met in a place, in words, a way of its own (the Hallowed Tower) last.</summary>
    public static string WaysOf(Habitat place)
    {
        string ways = WaysOf(place.Ways);
        if (!place.Ways.HasFlag(HabitatWays.Special) || place.How == null) return ways;
        return ways.Length == 0 ? place.How : ways + " · " + place.How;
    }

    /// <summary>How a species is met in a place, in words: "Grass in the morning and at night · Surfing · Old and Good Rod".</summary>
    public static string WaysOf(HabitatWays ways)
    {
        var parts = new List<string>();
        var times = new List<string>();
        if (ways.HasFlag(HabitatWays.Morning)) times.Add(Text.Of("pokedex.way.morning"));
        if (ways.HasFlag(HabitatWays.Day)) times.Add(Text.Of("pokedex.way.day"));
        if (ways.HasFlag(HabitatWays.Night)) times.Add(Text.Of("pokedex.way.night"));
        if (times.Count == 3) parts.Add(Text.Of("pokedex.way.allDay"));
        else if (times.Count > 0) parts.Add(Text.Of("pokedex.way.grass", ("times", string.Join(", ", times))));
        if (ways.HasFlag(HabitatWays.Surfing)) parts.Add(Text.Of("pokedex.way.surfing"));
        var rods = new List<string>();
        if (ways.HasFlag(HabitatWays.OldRod)) rods.Add(Text.Of("pokedex.way.Old"));
        if (ways.HasFlag(HabitatWays.GoodRod)) rods.Add(Text.Of("pokedex.way.Good"));
        if (ways.HasFlag(HabitatWays.SuperRod)) rods.Add(Text.Of("pokedex.way.Super"));
        if (rods.Count > 0) parts.Add(Text.Of("pokedex.way.rods", ("rods", string.Join(", ", rods)), ("n", rods.Count)));
        return string.Join(" · ", parts);
    }

    /// <summary>SIZE: the species and the player as two dark silhouettes on one line, at their real heights.</summary>
    private static void DexSize(Rectangle detail, PokemonSpecies s, bool owned, Texture2D? portrait)
    {
        if (!owned)
        {
            EmptyNote(new Rectangle(detail.X, detail.Y + 92, detail.Width, detail.Height - 92), Text.Of("pokedex.catchToCompare"));
            return;
        }
        float hx = Field(Text.Of("pokedex.height"), Metres(s.Height), detail.X + 44, detail.Y + 124, 38, 64);
        Field(Text.Of("pokedex.weight"), Kilograms(s.Weight), hx, detail.Y + 124, 38);

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
            string text = Metres(metres);
            float tw = UiFonts.Measure(text, 24, UiWeight.ExtraBold);
            UiFonts.DrawCentered(text, centerX - tw / 2f, floor + 82, 24, Muted, UiWeight.ExtraBold);
        }

        if (portrait is { } player) Stand(player, PlayerHeight, detail.X + detail.Width * 0.3f, PlayerIdentity.Name);
        Stand(PixelArtGenerator.GetPokemonSprite(s.Name, isBack: false), s.Height, detail.X + detail.Width * 0.68f, s.DisplayName);
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
        UiFonts.Draw(Text.Of("pokedex.search.title"), panel.X + 56, panel.Y + 36, 44, Ink, UiWeight.Black);

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
                    var button = buttons[b];
                    string label = Text.Of($"pokedex.search.button.{button}");
                    var color = button switch { PokedexSearchButton.Search => Green, PokedexSearchButton.Reset => Blue, _ => Gold };
                    Button(new Rectangle(panel.X + 56 + b * (bw + gap), y + 12, bw, 80), 40, color, label, 30, i == dex.SearchIndex && b == dex.ButtonIndex);
                }
                continue;
            }
            string name = Text.Of($"pokedex.search.row.{row}");
            ValueRow(new Rectangle(panel.X + 40, y, panel.Width - 80, height), name, dex.ValueOf(row), i == dex.SearchIndex, 440);
            y += pitch;
        }
    }

    // ------------------------------------------------------------------ the diploma

    /// <summary>The diploma for a complete Pokédex: a cream certificate with a gold double rule and a seal.</summary>
    public static void Diploma(int sw, int sh, string mode)
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

        Centred(Text.Of("pokedex.diploma.title"), r.Y + 118, 84, DeepGold, UiWeight.Black);
        string modeKey = Pokedex.KeyOf(mode);
        Centred(Text.Of($"pokedex.diploma.{modeKey}"), r.Y + 196, 34, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(r.X + r.Width / 2f - 160, r.Y + 234, 320, 3), 1.5f, Gold);
        Centred(PlayerIdentity.Name, r.Y + 312, 64, Ink, UiWeight.Black);
        string line = Text.Of($"pokedex.diploma.line.{modeKey}", ("count", Pokedex.Entries(mode).Count));
        var lines = Wrap(line, 900, 32, UiWeight.ExtraBold);
        for (int i = 0; i < lines.Count; i++) Centred(lines[i], r.Y + 400 + i * 46, 32, Ink, UiWeight.ExtraBold);

        string day = Words.Date(GameClock.Today);
        UiFonts.Draw(day, r.X + 92, r.Y + r.Height - 120, 28, Muted, UiWeight.ExtraBold);

        // The seal: a gold medallion with the Pokédex's sign
        var seal = new Vector2(r.X + r.Width - 170, r.Y + r.Height - 160);
        UiShapes.Circle(seal, 74, DeepGold);
        UiShapes.Circle(seal, 64, Gold);
        UiShapes.Ring(seal, 52, 3, Lighter(Gold, 0.45f));
        UiIcons.Draw(UiIcon.Dex, seal, 26, Color.White, Gold);
    }
}
