using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The crafting kit on screen (plan 39 · CR1; style guide, "Menu screens (G10)", "The kit").</summary>
internal static partial class ModernUi
{
    private static (string Label, UiIcon Icon, Color Color)[] CraftingTabs => new[]
    {
        (Text.Of("crafting.tab.materials"), UiIcon.Pouch, Gold)
    };

    /// <summary>The colour a material is drawn in: its type's for a Pokémon's, wood for the field's, green for the pantry's.</summary>
    internal static Color MaterialColour(MaterialData material) => material.Kind switch
    {
        MaterialKind.Pokemon when material.Type is { } type => Palette.GetTypeColor(type.ToString()),
        MaterialKind.Pantry => new Color(110, 168, 88, 255),
        _ => new Color(176, 132, 84, 255)
    };

    public static void DrawCrafting(int sw, int sh, CraftingScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("crafting.title"));
        var keys = new System.Collections.Generic.List<HintItem> { Hint("shared.keys.upDown", "shared.hints.move") };
        keys.Add(Hint("shared.keys.esc", "shared.hints.back"));
        Hints(sw - Margin, 44, keys.ToArray());
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), CraftingTabs, (int)screen.Tab);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24;
        const float listW = 860;
        var list = new Rectangle(Margin - slide, top, listW, ContentBottom - top);
        var detail = new Rectangle(Margin + listW + Gutter + slide, top, sw - Margin * 2 - listW - Gutter, ContentBottom - top);
        Panel(list, 34);
        Panel(detail, 34);

        // The materials, a row each: its sign on a disc in its colour, its name, and how many the pouch holds
        var entries = screen.Entries;
        bool scrolls = entries.Count > CraftingScreen.VisibleRows;
        for (int row = 0; row < CraftingScreen.VisibleRows && screen.First + row < entries.Count; row++)
        {
            int index = screen.First + row;
            var material = entries[index];
            var r = RowRect(list, row, 24, scrolls);
            bool selected = index == screen.Cursor;
            int count = screen.Pouch.Count(material.Id);
            var ink = ListRow(r, selected, MaterialTable.NameOf(material.Id), enabled: count > 0, nameX: 112, size: 32);
            MaterialSign(new Vector2(r.X + 56, r.Y + r.Height / 2f), 28, material, count > 0);
            RowValue(r, $"×{count}", ink);
        }
        ScrollBar(list, screen.First, CraftingScreen.VisibleRows, entries.Count, 24);

        // The one under the cursor: large, what it is and where it comes from
        if (screen.Selected is not { } chosen) return;
        float x = detail.X + 44, room = detail.Width - 88;
        int held = screen.Pouch.Count(chosen.Id);
        Label(MaterialTable.KindName(chosen.Kind), x, detail.Y + 30);
        string kinds = Text.Of("crafting.kinds", ("n", screen.Pouch.Kinds));
        Tag(detail.X + detail.Width - 44 - UiFonts.Measure(kinds, 24, UiWeight.Black) - 34, detail.Y + 26, kinds, Frame, 40);
        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 210);
        UiShapes.Circle(disc, 120, Disc);
        UiShapes.Ring(disc, 120, 5, Rule);
        MaterialSign(disc, 78, chosen, held > 0);
        string name = MaterialTable.NameOf(chosen.Id);
        float size = 44, nameW = UiFonts.Measure(name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(name, detail.X + (detail.Width - Math.Min(nameW, room)) / 2f, detail.Y + 380, size, Ink, UiWeight.Black);
        string has = held > 0 ? Text.Of("crafting.has", ("count", held), ("limit", MaterialTable.Limit)) : Text.Of("crafting.none");
        UiFonts.DrawCentered(has, detail.X + (detail.Width - UiFonts.Measure(has, 28, UiWeight.ExtraBold)) / 2f, detail.Y + 432, 28, Muted, UiWeight.ExtraBold);
        string worth = Text.Of("crafting.worth", ("money", chosen.Worth));
        UiFonts.DrawCentered(worth, detail.X + (detail.Width - UiFonts.Measure(worth, 26, UiWeight.ExtraBold)) / 2f, detail.Y + 476, 26, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, detail.Y + 520, room, 3), 1.5f, Rule);
        DrawWrapped(MaterialTable.DescriptionOf(chosen.Id), x, detail.Y + 548, room, 28, Ink, 36);
        DrawWrapped(MaterialSource(chosen), x, detail.Y + 548 + 36 * 3 + 12, room, 26, Muted, 34, UiWeight.ExtraBold);
    }

    /// <summary>Where a material comes from, in a sentence: a type's leavings, or the field's, or the pantry's.</summary>
    internal static string MaterialSource(MaterialData material) => material switch
    {
        { Kind: MaterialKind.Pokemon, Type: { } type, Rare: false } => Text.Of("crafting.leftBy", ("type", Words.Type(type))),
        { Kind: MaterialKind.Pokemon, Type: { } type } => Text.Of("crafting.leftByRare", ("type", Words.Type(type))),
        { Kind: MaterialKind.Pantry } => Text.Of("crafting.pantryGoods"),
        _ => Text.Of("crafting.fieldFinds")
    };

    /// <summary>
    /// A material's sign (style guide, "The kit"): a lump in its colour with a highlight and a rim, a rare one with a
    /// glint star over it; dimmed when none is held. It is drawn with shapes, one picture for every material.
    /// </summary>
    private static void MaterialSign(Vector2 at, float radius, MaterialData material, bool held)
    {
        var body = MaterialColour(material);
        if (!held) body = new Color((int)(body.R * 0.45f + 120), (int)(body.G * 0.45f + 120), (int)(body.B * 0.45f + 120), 255);
        var rim = new Color((int)(body.R * 0.55f), (int)(body.G * 0.55f), (int)(body.B * 0.55f), 255);
        UiShapes.Circle(at + new Vector2(0, radius * 0.06f), radius * 0.78f + 3, rim);
        UiShapes.Circle(at, radius * 0.78f, body);
        UiShapes.Circle(at + new Vector2(-radius * 0.28f, -radius * 0.3f), radius * 0.2f, new Color(255, 255, 255, 150));
        if (!material.Rare) return;
        // The glint of a rare one: two crossed slivers at its upper right
        var star = at + new Vector2(radius * 0.52f, -radius * 0.52f);
        float s = radius * 0.34f;
        var glint = held ? Color.White : new Color(255, 255, 255, 150);
        UiShapes.Fill(new Rectangle(star.X - s, star.Y - s * 0.12f, s * 2, s * 0.24f), s * 0.1f, glint);
        UiShapes.Fill(new Rectangle(star.X - s * 0.12f, star.Y - s, s * 0.24f, s * 2), s * 0.1f, glint);
    }
}
