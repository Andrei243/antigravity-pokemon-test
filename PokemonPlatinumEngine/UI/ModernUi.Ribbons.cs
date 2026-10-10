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
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Ribbons and the Fashion Case on screen (plan 06 · R17b; style guide, "Ribbons" and "The Fashion Case"): each of
/// the eighty ribbons as a sign of our own (a rosette, a bow, a medal, a pennant, by what it is given for, in a colour
/// of its own), the summary's page of them, the case's accessories and backdrops, and a contest winner's photograph.
/// </summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ a ribbon

    private static readonly Color[] DailyColors =
    {
        new(250, 206, 70, 255), new(244, 140, 62, 255), new(98, 140, 220, 255), new(150, 156, 176, 255),
        new(110, 196, 130, 255), new(160, 124, 220, 255), new(244, 140, 180, 255)
    };

    private static readonly string[] DailyOrder = { "RIBBON_ALERT", "RIBBON_SHOCK", "RIBBON_DOWNCAST", "RIBBON_CARELESS", "RIBBON_RELAX", "RIBBON_SNOOZE", "RIBBON_SMILE" };

    /// <summary>A ribbon's own colour.</summary>
    public static Color RibbonColor(RibbonRecord ribbon)
    {
        string id = ribbon.Id;
        if (RibbonRules.ContestOf(id) is var (quality, _)) return QualityColor(quality);
        return RibbonRules.Kind(id) switch
        {
            RibbonKind.Champion => id == RibbonRules.SinnohChamp ? new Color(70, 136, 232, 255) : new Color(226, 80, 76, 255),
            RibbonKind.Effort => new Color(92, 186, 108, 255),
            RibbonKind.Tower => id switch
            {
                "RIBBON_ABILITY" => new Color(90, 150, 236, 255),
                "RIBBON_GREAT_ABILITY" => new Color(226, 80, 76, 255),
                "RIBBON_DOUBLE_ABILITY" => new Color(92, 186, 108, 255),
                "RIBBON_MULTI_ABILITY" => new Color(240, 176, 56, 255),
                "RIBBON_PAIR_ABILITY" => new Color(236, 110, 170, 255),
                "RIBBON_WORLD_ABILITY" => new Color(160, 112, 214, 255),
                "RIBBON_HOENN_VICTORY" => new Color(226, 80, 76, 255),
                _ => new Color(90, 150, 236, 255)
            },
            RibbonKind.Daily => DailyColors[Math.Max(0, Array.IndexOf(DailyOrder, id))],
            RibbonKind.Syndicate => id == RibbonRules.Gorgeous ? new Color(236, 110, 170, 255) : id == RibbonRules.Royal ? new Color(140, 96, 214, 255) : new Color(226, 80, 110, 255),
            RibbonKind.Footprint => new Color(190, 140, 96, 255),
            RibbonKind.HoennContest => new Color(214, 150, 96, 255),
            _ => id switch
            {
                "RIBBON_RED" => new Color(226, 80, 76, 255),
                "RIBBON_GREEN" => new Color(92, 186, 108, 255),
                "RIBBON_BLUE" => new Color(90, 150, 236, 255),
                "RIBBON_HOENN_MARINE" => new Color(70, 150, 220, 255),
                "RIBBON_HOENN_LAND" => new Color(120, 180, 96, 255),
                "RIBBON_HOENN_SKY" => new Color(140, 200, 240, 255),
                "RIBBON_FESTIVAL" or "RIBBON_CARNIVAL" => new Color(244, 150, 90, 255),
                "RIBBON_RECORD" or "RIBBON_HISTORY" or "RIBBON_LEGEND" => new Color(120, 124, 156, 255),
                _ => new Color(150, 120, 200, 255)
            }
        };
    }

    // A bow: two loops, a knot and two tails
    private static void Bow(Vector2 c, float r, Color color)
    {
        Color dark = Darker(color, 0.28f), light = Lighter(color, 0.3f);
        UiShapes.Triangle(c + new Vector2(-r * 0.12f, r * 0.1f), c + new Vector2(-r * 0.62f, r * 1.02f), c + new Vector2(-r * 0.02f, r * 0.8f), dark);
        UiShapes.Triangle(c + new Vector2(r * 0.12f, r * 0.1f), c + new Vector2(r * 0.62f, r * 1.02f), c + new Vector2(r * 0.02f, r * 0.8f), dark);
        UiShapes.Triangle(c, c + new Vector2(-r, -r * 0.62f), c + new Vector2(-r, r * 0.5f), dark, r * 0.12f);
        UiShapes.Triangle(c, c + new Vector2(r, -r * 0.62f), c + new Vector2(r, r * 0.5f), dark, r * 0.12f);
        UiShapes.Triangle(c, c + new Vector2(-r * 0.84f, -r * 0.44f), c + new Vector2(-r * 0.84f, r * 0.34f), color, r * 0.1f);
        UiShapes.Triangle(c, c + new Vector2(r * 0.84f, -r * 0.44f), c + new Vector2(r * 0.84f, r * 0.34f), color, r * 0.1f);
        UiShapes.Circle(c, r * 0.3f, dark);
        UiShapes.Circle(c, r * 0.2f, light);
    }

    // A medal hanging from a folded band
    private static void Medal(Vector2 c, float r, Color band, Color metal)
    {
        UiShapes.Triangle(c + new Vector2(-r * 0.62f, -r), c + new Vector2(-r * 0.06f, -r), c + new Vector2(r * 0.2f, r * 0.05f), Darker(band, 0.2f));
        UiShapes.Triangle(c + new Vector2(r * 0.62f, -r), c + new Vector2(r * 0.06f, -r), c + new Vector2(-r * 0.2f, r * 0.05f), band);
        var m = c + new Vector2(0, r * 0.34f);
        UiShapes.Circle(m, r * 0.66f, Darker(metal, 0.35f));
        UiShapes.Circle(m, r * 0.54f, metal);
        UiShapes.Circle(m + new Vector2(-r * 0.16f, -r * 0.16f), r * 0.14f, Lighter(metal, 0.6f));
    }

    // A pennant: a band with a swallow tail
    private static void Pennant(Vector2 c, float r, Color color)
    {
        Color dark = Darker(color, 0.28f);
        var body = new Rectangle(c.X - r * 0.5f, c.Y - r * 0.95f, r, r * 1.5f);
        UiShapes.Fill(new Rectangle(body.X - r * 0.08f, body.Y - r * 0.06f, body.Width + r * 0.16f, body.Height + r * 0.1f), r * 0.1f, dark);
        UiShapes.Fill(body, r * 0.06f, color);
        UiShapes.Triangle(new Vector2(body.X - r * 0.08f, body.Y + body.Height), new Vector2(c.X, body.Y + body.Height), new Vector2(body.X - r * 0.08f, c.Y + r), dark);
        UiShapes.Triangle(new Vector2(body.X + body.Width + r * 0.08f, body.Y + body.Height), new Vector2(c.X, body.Y + body.Height), new Vector2(body.X + body.Width + r * 0.08f, c.Y + r), dark);
        UiShapes.Fill(new Rectangle(body.X, body.Y + r * 0.22f, body.Width, r * 0.16f), 0, Lighter(color, 0.45f));
    }

    // So many gold pips under a ribbon: the rank it was won at
    private static void Pips(Vector2 c, float r, int count)
    {
        float gap = r * 0.36f, x = c.X - gap * (count - 1) / 2f;
        for (int i = 0; i < count; i++)
        {
            UiShapes.Circle(new Vector2(x + i * gap, c.Y), r * 0.17f, Darker(Gold, 0.4f));
            UiShapes.Circle(new Vector2(x + i * gap, c.Y), r * 0.12f, Gold);
        }
    }

    /// <summary>
    /// A ribbon's sign, about 2 × <paramref name="r"/> across: a rosette for a Super Contest's and the Syndicate's, a
    /// bow for Hoenn's contests and Julia's seven, a medal for a Champion's, the Battle Tower's and Dr. Footstep's, a
    /// pennant for the Effort Ribbon and the ones only an event gave; pips for a contest's rank.
    /// </summary>
    public static void RibbonIcon(Vector2 c, float r, RibbonRecord ribbon)
    {
        var color = RibbonColor(ribbon);
        var kind = RibbonRules.Kind(ribbon.Id);
        int rank = RibbonRules.ContestOf(ribbon.Id)?.Rank ?? -1;
        switch (kind)
        {
            case RibbonKind.Contest:
                Rosette(c - new Vector2(0, r * 0.3f), r * 0.74f, color);
                Pips(c + new Vector2(0, r * 1.04f), r, rank + 1);
                break;
            case RibbonKind.HoennContest:
                Bow(c - new Vector2(0, r * 0.26f), r * 0.84f, color);
                if (rank >= 0) Pips(c + new Vector2(0, r * 1.04f), r, rank + 1);
                else Star(c - new Vector2(0, r * 0.2f), r * 0.2f, Gold);
                break;
            case RibbonKind.Syndicate:
                Rosette(c - new Vector2(0, r * 0.1f), r * 0.92f, color);
                if (ribbon.Id != RibbonRules.Gorgeous) UiShapes.Ring(c - new Vector2(0, r * 0.1f), r * 0.98f, r * 0.08f, Gold);
                if (ribbon.Id == RibbonRules.GorgeousRoyal) Star(c - new Vector2(0, r * 1.02f), r * 0.26f, Gold);
                break;
            case RibbonKind.Champion:
                Medal(c, r * 0.96f, color, Gold);
                Star(c + new Vector2(0, r * 0.33f), r * 0.3f, Color.White);
                break;
            case RibbonKind.Tower:
                Medal(c, r * 0.96f, color, new Color(200, 210, 226, 255));
                UiShapes.Triangle(c + new Vector2(0, r * 0.08f), c + new Vector2(-r * 0.24f, r * 0.52f), c + new Vector2(r * 0.24f, r * 0.52f), Darker(color, 0.1f));
                break;
            case RibbonKind.Footprint:
                Medal(c, r * 0.96f, color, new Color(226, 198, 150, 255));
                var paw = c + new Vector2(0, r * 0.42f);
                UiShapes.Circle(paw, r * 0.2f, Darker(color, 0.3f));
                for (int i = -1; i <= 1; i++) UiShapes.Circle(paw + new Vector2(i * r * 0.22f, -r * 0.28f + Math.Abs(i) * r * 0.06f), r * 0.1f, Darker(color, 0.3f));
                break;
            case RibbonKind.Daily:
                Bow(c - new Vector2(0, r * 0.1f), r * 0.94f, color);
                break;
            default:
                Pennant(c, r * 0.96f, color);
                if (kind == RibbonKind.Effort) Star(c - new Vector2(0, r * 0.06f), r * 0.26f, Gold);
                break;
        }
    }

    /// <summary>
    /// The summary's page of ribbons: how many, then the Pokémon's own in the original's order, six to a row; with a
    /// cursor on one, its name and what it is for underneath.
    /// </summary>
    public static void RibbonsPanel(Rectangle r, Pokemon p, int cursor)
    {
        Panel(r, 34);
        var ribbons = RibbonRules.Of(p);
        Label("RIBBONS", r.X + 44, r.Y + 30);
        string count = ribbons.Count.ToString();
        Tag(r.X + r.Width - 44 - UiFonts.Measure(count, 24, UiWeight.Black) - 34, r.Y + 26, count, ribbons.Count > 0 ? Frame : Muted, 40);

        const float foot = 196, top = 84, pad = 36, gap = 14;
        var grid = new Rectangle(r.X + pad, r.Y + top, r.Width - pad * 2, r.Height - top - foot - 24);
        if (ribbons.Count == 0)
        {
            EmptyNote(new Rectangle(r.X, r.Y, r.Width, r.Height - foot), "No ribbons yet.");
        }
        int columns = PartyScreen.RibbonColumns, rows = PartyScreen.RibbonRows;
        float cw = (grid.Width - gap * (columns - 1)) / columns, ch = (grid.Height - gap * (rows - 1)) / rows;
        int firstRow = Math.Clamp((Math.Max(cursor, 0) / columns) - (rows - 1), 0, Math.Max(0, (ribbons.Count - 1) / columns - (rows - 1)));
        for (int i = firstRow * columns; i < ribbons.Count && i < (firstRow + rows) * columns; i++)
        {
            var cell = new Rectangle(grid.X + (i % columns) * (cw + gap), grid.Y + (i / columns - firstRow) * (ch + gap), cw, ch);
            bool selected = i == cursor;
            if (selected) Card(cell, 24, true);
            else UiShapes.Shape(cell, 24, new Color(248, 250, 253, 255), new Color(236, 241, 248, 255), Rule, 3);
            RibbonIcon(new Vector2(cell.X + cell.Width / 2f, cell.Y + cell.Height / 2f - 4), MathF.Min(cell.Width, cell.Height) * 0.3f, ribbons[i]);
        }
        if (ribbons.Count > rows * columns)
        {
            string more = $"{firstRow + 1} / {(ribbons.Count - 1) / columns - (rows - 1) + 1}";
            UiFonts.DrawCentered(more, r.X + r.Width - 44 - UiFonts.Measure(more, 20, UiWeight.Black), grid.Y + grid.Height + 12, 20, Muted, UiWeight.Black);
        }

        // ---- The ribbon looked at
        var box = new Rectangle(r.X + 28, r.Y + r.Height - foot - 4, r.Width - 56, foot - 24);
        UiShapes.Fill(box, 26, Disc);
        if (cursor >= 0 && cursor < ribbons.Count)
        {
            var chosen = ribbons[cursor];
            RibbonIcon(new Vector2(box.X + 86, box.Y + box.Height / 2f), 52, chosen);
            UiFonts.Draw(chosen.Name, box.X + 172, box.Y + 26, 40, Ink, UiWeight.Black);
            DrawWrapped(RibbonRules.Describe(chosen.Id), box.X + 172, box.Y + 80, box.Width - 172 - 36, 28, Muted, 36, UiWeight.ExtraBold);
        }
        else
        {
            string note = ribbons.Count == 0 ? "Ribbons are won at contests, and given for deeds of all kinds."
                : ribbons.Count == 1 ? "It has won a ribbon." : $"It has won {ribbons.Count} ribbons.";
            UiFonts.DrawCentered(note, box.X + 44, box.Y + box.Height / 2f, 32, Muted, UiWeight.ExtraBold);
        }
    }

    // ------------------------------------------------------------------ backdrops

    private static Color C(int r, int g, int b, int a = 255) => new(r, g, b, a);

    /// <summary>
    /// A backdrop as a picture of our own: a sky or a wall in two tones and a few plain shapes, by its name's scene.
    /// </summary>
    public static void BackdropArt(Rectangle r, int backdrop)
    {
        float w = r.Width, h = r.Height, x = r.X, y = r.Y;
        // Its small things are sized for a picture seven hundred wide; a swatch has them as much smaller
        float small = w / 700f;
        float Rnd(int i, int salt) => SoftCanvas.Rand(backdrop * 97 + 5, i, salt);
        void Sky(Color top, Color bottom) => UiShapes.Shape(r, 0, top, bottom);
        void Ground(float from, Color top, Color bottom) => UiShapes.Shape(new Rectangle(x, y + h * from, w, h * (1f - from)), 0, top, bottom);
        Raylib.BeginScissorMode((int)(x * GameEngine.RenderScale), (int)(y * GameEngine.RenderScale), (int)(w * GameEngine.RenderScale), (int)(h * GameEngine.RenderScale));
        switch (SuperContestDatabase.BackdropName(backdrop))
        {
            case "Dress Up":
                Sky(C(255, 236, 240), C(250, 214, 226));
                for (int i = 0; i < 9; i += 2) UiShapes.Fill(new Rectangle(x + i * w / 9f, y, w / 9f, h), 0, C(246, 170, 196, 110));
                Ground(0.8f, C(214, 170, 130), C(186, 140, 104));
                break;
            case "Ranch":
                Sky(C(140, 204, 250), C(214, 238, 252));
                UiShapes.Circle(new Vector2(x + w * 0.3f, y + h * 1.25f), h * 0.62f, C(120, 196, 104));
                UiShapes.Circle(new Vector2(x + w * 0.8f, y + h * 1.3f), h * 0.7f, C(98, 178, 92));
                for (int i = 0; i < 8; i++) UiShapes.Fill(new Rectangle(x + w * (0.04f + i * 0.13f), y + h * 0.7f, w * 0.02f, h * 0.14f), 2, C(226, 214, 190));
                UiShapes.Fill(new Rectangle(x, y + h * 0.74f, w, h * 0.022f), 0, C(226, 214, 190));
                break;
            case "City at Night":
                Sky(C(26, 30, 70), C(64, 60, 120));
                for (int i = 0; i < 9; i++)
                {
                    float bw = w * (0.08f + 0.05f * Rnd(i, 1)), bh = h * (0.3f + 0.45f * Rnd(i, 2));
                    var b = new Rectangle(x + i * w / 8.4f - 10, y + h - bh, bw, bh);
                    UiShapes.Fill(b, 0, C(20, 22, 50));
                    for (int k = 0; k < 6; k++)
                        if (Rnd(i * 7 + k, 3) > 0.4f) UiShapes.Fill(new Rectangle(b.X + bw * (0.2f + 0.45f * (k % 2)), b.Y + bh * (0.1f + 0.14f * (k / 2)) + 8, bw * 0.18f, bh * 0.06f + 6), 0, C(250, 226, 130));
                }
                break;
            case "Snowy Town":
                Sky(C(170, 200, 236), C(226, 238, 250));
                UiShapes.Circle(new Vector2(x + w * 0.25f, y + h * 1.2f), h * 0.6f, C(240, 246, 252));
                UiShapes.Circle(new Vector2(x + w * 0.78f, y + h * 1.26f), h * 0.66f, C(226, 236, 248));
                for (int i = 0; i < 40; i++) UiShapes.Circle(new Vector2(x + w * Rnd(i, 1), y + h * Rnd(i, 2)), (3 + 4 * Rnd(i, 3)) * small, C(255, 255, 255, 220));
                break;
            case "Fiery":
                Sky(C(120, 20, 24), C(250, 150, 50));
                for (int i = 0; i < 9; i++)
                {
                    float fx = x + w * (i + 0.5f) / 9f, fh = h * (0.3f + 0.4f * Rnd(i, 1));
                    UiShapes.Triangle(new Vector2(fx - w * 0.08f, y + h), new Vector2(fx + w * 0.08f, y + h), new Vector2(fx + w * 0.03f * (Rnd(i, 2) - 0.5f), y + h - fh), C(255, 206, 80, 210), 8);
                }
                break;
            case "Outer Space":
                Sky(C(10, 8, 30), C(50, 24, 80));
                for (int i = 0; i < 60; i++) UiShapes.Circle(new Vector2(x + w * Rnd(i, 1), y + h * Rnd(i, 2)), (1.5f + 2.5f * Rnd(i, 3)) * small, C(255, 255, 255, 230));
                UiShapes.Circle(new Vector2(x + w * 0.76f, y + h * 0.3f), h * 0.16f, C(236, 170, 110));
                UiShapes.Turned(new Vector2(x + w * 0.76f, y + h * 0.3f), h * 0.27f, h * 0.02f, h * 0.02f, -20f, C(250, 220, 170));
                break;
            case "Cumulus Cloud":
                Sky(C(96, 170, 240), C(190, 226, 252));
                for (int i = 0; i < 7; i++) UiShapes.Circle(new Vector2(x + w * (0.08f + i * 0.14f), y + h * (0.78f + 0.08f * Rnd(i, 1))), h * (0.2f + 0.08f * Rnd(i, 2)), C(255, 255, 255));
                Ground(0.9f, C(255, 255, 255), C(236, 244, 252));
                break;
            case "Desert":
                Sky(C(250, 196, 110), C(252, 228, 170));
                UiShapes.Circle(new Vector2(x + w * 0.78f, y + h * 0.24f), h * 0.1f, C(255, 246, 210));
                UiShapes.Circle(new Vector2(x + w * 0.22f, y + h * 1.34f), h * 0.7f, C(232, 180, 110));
                UiShapes.Circle(new Vector2(x + w * 0.74f, y + h * 1.42f), h * 0.76f, C(214, 160, 96));
                break;
            case "Flower Patch":
                Sky(C(150, 214, 250), C(214, 240, 226));
                Ground(0.56f, C(120, 196, 110), C(86, 164, 92));
                for (int i = 0; i < 46; i++)
                {
                    var at = new Vector2(x + w * Rnd(i, 1), y + h * (0.6f + 0.38f * Rnd(i, 2)));
                    UiShapes.Circle(at, 7 * small, i % 3 == 0 ? C(250, 150, 180) : i % 3 == 1 ? C(252, 226, 110) : C(250, 250, 252));
                    UiShapes.Circle(at, 3 * small, C(250, 196, 70));
                }
                break;
            case "Future Room":
                Sky(C(40, 70, 96), C(24, 40, 64));
                for (int i = 1; i < 8; i++) UiShapes.Fill(new Rectangle(x + i * w / 8f, y, 3, h), 0, C(90, 220, 226, 90));
                for (int i = 1; i < 6; i++) UiShapes.Fill(new Rectangle(x, y + i * h / 6f, w, 3), 0, C(90, 220, 226, 90));
                Ground(0.82f, C(60, 96, 120), C(40, 64, 90));
                break;
            case "Open Sea":
                Sky(C(130, 200, 250), C(214, 240, 252));
                UiShapes.Circle(new Vector2(x + w * 0.5f, y + h * 0.5f), h * 0.12f, C(255, 250, 214));
                Ground(0.5f, C(60, 150, 220), C(30, 96, 180));
                for (int i = 0; i < 16; i++) UiShapes.Fill(new Rectangle(x + w * Rnd(i, 1), y + h * (0.55f + 0.42f * Rnd(i, 2)), w * 0.08f, 4), 2, C(255, 255, 255, 150));
                break;
            case "Total Darkness":
                Sky(C(10, 10, 16), C(22, 20, 34));
                UiShapes.Circle(new Vector2(x + w * 0.5f, y + h * 0.9f), h * 0.4f, C(60, 56, 90, 60));
                break;
            case "Tatami Room":
                Sky(C(236, 220, 180), C(214, 196, 150));
                for (int i = 0; i < 4; i++) UiShapes.Fill(new Rectangle(x + i * w / 4f, y, 6, h * 0.62f), 0, C(120, 90, 60));
                UiShapes.Fill(new Rectangle(x, y + h * 0.6f, w, 8), 0, C(120, 90, 60));
                Ground(0.62f, C(196, 204, 130), C(170, 180, 110));
                for (int i = 1; i < 4; i++) UiShapes.Fill(new Rectangle(x + i * w / 4f, y + h * 0.62f, 4, h * 0.38f), 0, C(110, 120, 70));
                break;
            case "Gingerbread Room":
                Sky(C(196, 130, 80), C(160, 100, 60));
                for (int i = 0; i < 9; i++) UiShapes.Circle(new Vector2(x + w * (i + 0.5f) / 9f, y + 4), w / 15f, C(255, 250, 240));
                for (int i = 0; i < 12; i++) UiShapes.Circle(new Vector2(x + w * Rnd(i, 1), y + h * (0.3f + 0.4f * Rnd(i, 2))), 12 * small, i % 2 == 0 ? C(240, 90, 110) : C(120, 200, 140));
                Ground(0.82f, C(250, 240, 226), C(232, 214, 196));
                break;
            case "Seafloor":
                Sky(C(30, 110, 180), C(14, 50, 110));
                Ground(0.84f, C(226, 206, 150), C(196, 172, 120));
                for (int i = 0; i < 5; i++) UiShapes.Fill(new Rectangle(x + w * (0.1f + i * 0.2f), y + h * (0.5f + 0.1f * Rnd(i, 1)), 14 * small, h * 0.4f), 7 * small, C(60, 170, 120));
                for (int i = 0; i < 16; i++) UiShapes.Ring(new Vector2(x + w * Rnd(i, 2), y + h * 0.8f * Rnd(i, 3)), (6 + 8 * Rnd(i, 4)) * small, 3 * small, C(200, 236, 255, 170));
                break;
            case "Underground":
                Sky(C(96, 66, 48), C(60, 40, 30));
                for (int i = 0; i < 12; i++) UiShapes.Fill(new Rectangle(x + w * Rnd(i, 1), y + h * Rnd(i, 2), (50 + 60 * Rnd(i, 3)) * small, (30 + 30 * Rnd(i, 4)) * small), 14 * small, C(76, 52, 38));
                UiShapes.Circle(new Vector2(x + w * 0.5f, y + h * 0.36f), h * 0.3f, C(255, 214, 120, 46));
                Ground(0.84f, C(120, 86, 60), C(90, 62, 44));
                break;
            case "Sky":
                Sky(C(70, 140, 236), C(170, 214, 250));
                for (int i = 0; i < 6; i++) UiShapes.Fill(new Rectangle(x + w * Rnd(i, 1) - 60, y + h * (0.12f + 0.7f * Rnd(i, 2)), w * (0.2f + 0.2f * Rnd(i, 3)), 16 * small), 8 * small, C(255, 255, 255, 170));
                break;
            default:
                // Theater, and any backdrop without a scene of its own: curtains, a stage and a light
                Sky(C(90, 24, 40), C(60, 16, 30));
                UiShapes.Triangle(new Vector2(x + w * 0.5f, y - h * 0.1f), new Vector2(x + w * 0.24f, y + h * 0.86f), new Vector2(x + w * 0.76f, y + h * 0.86f), C(255, 240, 190, 60));
                for (int i = 0; i < 4; i++)
                {
                    UiShapes.Fill(new Rectangle(x + i * w * 0.045f, y, w * 0.03f, h), 0, C(170, 40, 60));
                    UiShapes.Fill(new Rectangle(x + w - (i + 1) * w * 0.045f + w * 0.015f, y, w * 0.03f, h), 0, C(170, 40, 60));
                }
                Ground(0.84f, C(196, 150, 100), C(150, 106, 70));
                break;
        }
        Raylib.EndScissorMode();
    }

    // ------------------------------------------------------------------ the Fashion Case

    private static readonly (string Label, UiIcon Icon, Color Color)[] FashionTabs =
    {
        ("ACCESSORIES", UiIcon.Hat, new Color(236, 110, 170, 255)),
        ("BACKDROPS", UiIcon.Pin, new Color(70, 136, 232, 255))
    };

    public static void DrawFashionCase(int sw, int sh, FashionCaseScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle("FASHION CASE");
        Hints(sw - Margin, 44, ("C", "Accessories / Backdrops"), ("Esc", "Back"));
        Tabs(new Rectangle(Margin, ContentTop, 760, 76), FashionTabs, screen.Tab);
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24, listW = 1100;
        var list = new Rectangle(Margin - slide, top, listW, ContentBottom - top);
        var detail = new Rectangle(Margin + listW + Gutter + slide, ContentTop, sw - Margin * 2 - listW - Gutter, ContentBottom - ContentTop);
        Panel(list, 34);
        Panel(detail, 34);
        var fashion = screen.Case;

        if (screen.Tab == 0)
        {
            var held = screen.Accessories;
            if (held.Count == 0) EmptyNote(list, "No Accessories yet.");
            const float pad = 28, gap = 14, cellH = 118;
            float cell = (list.Width - pad * 2 - gap * (FashionCaseScreen.Columns - 1)) / FashionCaseScreen.Columns;
            int rows = (int)((list.Height - pad * 2 + gap) / (cellH + gap));
            int firstRow = Math.Max(0, screen.Cursor / FashionCaseScreen.Columns - rows + 1);
            for (int i = firstRow * FashionCaseScreen.Columns; i < held.Count && i < (firstRow + rows) * FashionCaseScreen.Columns; i++)
            {
                var r = new Rectangle(list.X + pad + (i % FashionCaseScreen.Columns) * (cell + gap), list.Y + pad + (i / FashionCaseScreen.Columns - firstRow) * (cellH + gap), cell, cellH);
                Card(r, 22, i == screen.Cursor);
                AccessoryIcon(new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f - 8), 32, held[i].Accessory);
                string n = $"×{held[i].Count}";
                UiFonts.DrawCentered(n, r.X + r.Width - 14 - UiFonts.Measure(n, 22, UiWeight.Black), r.Y + r.Height - 22, 22, Ink, UiWeight.Black);
            }

            Label("ACCESSORY", detail.X + 44, detail.Y + 30);
            string total = $"{fashion.Total} IN ALL";
            Tag(detail.X + detail.Width - 44 - UiFonts.Measure(total, 24, UiWeight.Black) - 34, detail.Y + 26, total, Frame, 40);
            if (screen.Cursor < held.Count)
            {
                var (accessory, count) = held[screen.Cursor];
                var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 230);
                UiShapes.Circle(disc, 130, Disc);
                UiShapes.Ring(disc, 130, 5, Rule);
                AccessoryIcon(disc, 84, accessory);
                string name = SuperContestDatabase.AccessoryName(accessory);
                float size = 44, nw = UiFonts.Measure(name, size, UiWeight.Black);
                if (nw > detail.Width - 88) size = MathF.Floor(size * (detail.Width - 88) / nw);
                UiFonts.DrawCentered(name, detail.X + (detail.Width - UiFonts.Measure(name, size, UiWeight.Black)) / 2f, detail.Y + 410, size, Ink, UiWeight.Black);
                string has = $"×{count}  of {FashionCase.Limit(accessory)}";
                UiFonts.DrawCentered(has, detail.X + (detail.Width - UiFonts.Measure(has, 28, UiWeight.ExtraBold)) / 2f, detail.Y + 458, 28, Muted, UiWeight.ExtraBold);
            }
        }
        else
        {
            var backdrops = fashion.Backdrops;
            if (backdrops.Count == 0) EmptyNote(list, "No Backdrops yet.");
            bool scrolls = backdrops.Count > FashionCaseScreen.VisibleRows;
            for (int row = 0; row < FashionCaseScreen.VisibleRows && screen.First + row < backdrops.Count; row++)
            {
                int index = screen.First + row;
                var r = RowRect(list, row, 26, scrolls);
                bool selected = index == screen.Cursor;
                ListRow(r, selected, SuperContestDatabase.BackdropName(backdrops[index]), nameX: 148);
                var swatch = new Rectangle(r.X + 20, r.Y + 8, 104, r.Height - 16);
                BackdropArt(swatch, backdrops[index]);
                UiShapes.Shape(swatch, 0, default, default, selected ? Color.White : Frame, 3);
            }
            ScrollBar(list, screen.First, FashionCaseScreen.VisibleRows, backdrops.Count, 26);

            Label("BACKDROP", detail.X + 44, detail.Y + 30);
            string total = $"{backdrops.Count} / {SuperContestDatabase.Backdrops.Count}";
            Tag(detail.X + detail.Width - 44 - UiFonts.Measure(total, 24, UiWeight.Black) - 34, detail.Y + 26, total, Frame, 40);
            if (screen.Cursor < backdrops.Count)
            {
                var picture = new Rectangle(detail.X + 44, detail.Y + 96, detail.Width - 88, (detail.Width - 88) * 0.72f);
                BackdropArt(picture, backdrops[screen.Cursor]);
                UiShapes.Shape(picture, 0, default, default, Frame, 4);
                string name = SuperContestDatabase.BackdropName(backdrops[screen.Cursor]);
                UiFonts.DrawCentered(name, detail.X + (detail.Width - UiFonts.Measure(name, 40, UiWeight.Black)) / 2f, picture.Y + picture.Height + 44, 40, Ink, UiWeight.Black);
            }
        }

        // ---- The contests entered with what the case holds
        var records = screen.Records;
        float fy = detail.Y + detail.Height - 232;
        UiShapes.Fill(new Rectangle(detail.X + 44, fy, detail.Width - 88, 3), 1.5f, Rule);
        Label("SUPER CONTESTS", detail.X + 44, fy + 22);
        float third = (detail.Width - 88) / 3f;
        Field("ENTERED", records.Entered.ToString(), detail.X + 44, fy + 62, 44, 50);
        Field("WON", records.Won.ToString(), detail.X + 44 + third, fy + 62, 44, 50);
        Field("RIBBONS", records.Ribbons.ToString(), detail.X + 44 + third * 2, fy + 62, 44, 50);
    }

    // ------------------------------------------------------------------ a winner's photograph

    /// <summary>
    /// The photograph in one of the Contest Hall's frames: a print with the Pokémon as it won on the theatre's stage,
    /// the accessories it wore round it, and under it who, when and at what.
    /// </summary>
    public static void DrawContestPhoto(int sw, int sh, ContestPhoto photo, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle("CONTEST PHOTO");
        Hints(sw - Margin, 44, ("Z", "Close"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 50f;
        var print = new Rectangle(sw / 2f - 520, ContentTop + 6 + rise, 1040, ContentBottom - ContentTop - 12);
        UiShapes.Shadow(print, 14, 34, new Vector2(0, 12), new Color(6, 14, 34, 120));
        UiShapes.Fill(print, 14, new Color(250, 250, 246, 255));
        var picture = new Rectangle(print.X + 36, print.Y + 36, print.Width - 72, 640);
        BackdropArt(picture, SuperContestDatabase.Backdrop("Theater"));
        // A wash of the contest's own colour over the light
        UiShapes.Fill(picture, 0, QualityColor(photo.Category) with { A = 36 });

        var c = new Vector2(picture.X + picture.Width / 2f, picture.Y + picture.Height * 0.62f);
        string model = photo.Form ?? photo.Species;
        var sprite = PixelArtGenerator.GetPokemonSprite(model, false);
        const int scale = 4;
        var worn = photo.Accessories.Select(SuperContestDatabase.Accessory).Where(a => a >= 0).ToList();
        void Ring(bool behind)
        {
            for (int i = 0; i < worn.Count; i++)
            {
                float a = -MathF.PI / 2f + (i % 10) * MathF.PI * 2f / 10f + (i / 10) * 0.31f;
                if ((MathF.Sin(a) < -0.3f) != behind) continue;
                AccessoryIcon(c + new Vector2(MathF.Cos(a) * 220f, MathF.Sin(a) * 180f - 40f) * (1f + 0.2f * (i / 10)), 40, worn[i]);
            }
        }
        Ring(true);
        PixelArt(sprite, c - new Vector2(0, sprite.Height * scale * 0.28f), scale);
        Ring(false);
        UiShapes.Shape(picture, 0, default, default, new Color(60, 56, 70, 255), 4);
        Rosette(new Vector2(picture.X + picture.Width - 86, picture.Y + 86), 46, QualityColor(photo.Category));

        float ty = picture.Y + picture.Height + 30;
        UiFonts.Draw(photo.Name, print.X + 44, ty, 52, Ink, UiWeight.Black);
        string won = $"{photo.Category} Contest, {photo.Rank} Rank";
        UiFonts.Draw(won, print.X + 44, ty + 62, 32, Muted, UiWeight.ExtraBold);
        string by = $"{PlayerIdentity.Fill(photo.Trainer)}  ·  {photo.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}";
        UiFonts.Draw(by, print.X + print.Width - 44 - UiFonts.Measure(by, 28, UiWeight.ExtraBold), ty + 66, 28, Muted, UiWeight.ExtraBold);
    }
}
