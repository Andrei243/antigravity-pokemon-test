using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The vector interface (prototyped in plan 04 · G1, grown into the UI kit in G3): crisp anti-aliased panels
/// with soft shadows, Nunito, and Platinum's colour language (red FIGHT, gold BAG, green POKÉMON, blue RUN; light
/// panels framed in slate). Pokémon appear as 2D pixel sprites, as in the main games. See docs/art/style-guide.md.
/// </summary>
internal static class ModernUi
{
    // Design tokens
    public static readonly Color Ink = new(36, 44, 68, 255);
    public static readonly Color Muted = new(110, 120, 148, 255);
    public static readonly Color Frame = new(52, 64, 96, 255);
    public static readonly Color PanelTop = new(255, 255, 255, 255);
    public static readonly Color PanelBottom = new(234, 240, 248, 255);
    public static readonly Color ShadowColor = new(14, 22, 46, 80);
    public static readonly Color Red = new(232, 72, 76, 255);
    public static readonly Color Gold = new(240, 176, 56, 255);
    public static readonly Color Green = new(56, 182, 104, 255);
    public static readonly Color Blue = new(70, 136, 232, 255);
    public static readonly Color Track = new(62, 72, 98, 255);

    private static Color Lighter(Color c, float t) => PixelCanvas.Mix(c, Color.White, t);
    private static Color Darker(Color c, float t) => PixelCanvas.Mix(c, new Color(20, 16, 40, 255), t);

    public static void Panel(Rectangle r, float radius, float skew = 0f, float border = 4f, Color? frame = null)
    {
        UiShapes.Shadow(r, radius, 22f, new Vector2(0, 8), ShadowColor, skew);
        UiShapes.Shape(r, radius, PanelTop, PanelBottom, frame ?? Frame, border, skew);
    }

    /// <summary>A coloured pill button: gradient body, bright top edge, white label; selected ones get a white ring and glow.</summary>
    public static void Button(Rectangle r, float radius, Color color, string label, float size, bool selected)
    {
        if (selected)
        {
            UiShapes.Shadow(r, radius, 30f, Vector2.Zero, color with { A = 150 });
            var ring = new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12);
            UiShapes.Fill(ring, radius + 6, Color.White);
        }
        else
        {
            UiShapes.Shadow(r, radius, 14f, new Vector2(0, 6), ShadowColor);
        }
        UiShapes.Shape(r, radius, Lighter(color, 0.18f), Darker(color, 0.12f), Darker(color, 0.35f), 3f);
        var shine = new Rectangle(r.X + 10, r.Y + 6, r.Width - 20, r.Height * 0.38f);
        UiShapes.Fill(shine, radius - 6, new Color(255, 255, 255, 46));
        float w = UiFonts.Measure(label, size, UiWeight.Black);
        UiFonts.DrawCentered(label, r.X + (r.Width - w) / 2f, r.Y + r.Height / 2f + 2, size, new Color(0, 0, 0, 50), UiWeight.Black);
        UiFonts.DrawCentered(label, r.X + (r.Width - w) / 2f, r.Y + r.Height / 2f, size, Color.White, UiWeight.Black);
    }

    public static void HpBar(float x, float y, float w, float h, float ratio)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);
        float tagW = h * 2.3f;
        var tag = new Rectangle(x, y, tagW, h);
        UiShapes.Shape(tag, h / 2f, new Color(255, 200, 80, 255), new Color(236, 160, 40, 255));
        float ts = h * 0.72f;
        UiFonts.DrawCentered("HP", x + (tagW - UiFonts.Measure("HP", ts, UiWeight.Black)) / 2f, y + h / 2f, ts, new Color(110, 60, 16, 255), UiWeight.Black);

        var track = new Rectangle(x + tagW + 6, y, w - tagW - 6, h);
        UiShapes.Fill(track, h / 2f, Track);
        var color = ratio > 0.5f ? new Color(70, 214, 110, 255) : ratio > 0.2f ? new Color(246, 196, 50, 255) : new Color(240, 72, 64, 255);
        float fw = (track.Width - 6) * ratio;
        if (fw > 0.5f)
        {
            var fill = new Rectangle(track.X + 3, y + 3, Math.Max(fw, h - 6), h - 6);
            UiShapes.Shape(fill, (h - 6) / 2f, Lighter(color, 0.3f), color);
        }
    }

    private static void Level(float rightX, float centerY, int level, float size)
    {
        string n = level.ToString();
        float nw = UiFonts.Measure(n, size, UiWeight.Black);
        float lw = UiFonts.Measure("Lv", size * 0.62f, UiWeight.ExtraBold);
        UiFonts.DrawCentered("Lv", rightX - nw - lw - 4, centerY + size * 0.12f, size * 0.62f, Muted, UiWeight.ExtraBold);
        UiFonts.DrawCentered(n, rightX - nw, centerY, size, Ink, UiWeight.Black);
    }

    private static void NameWithGender(Pokemon p, float x, float centerY, float size)
    {
        UiFonts.DrawCentered(p.DisplayName, x, centerY, size, Ink, UiWeight.Black);
        float w = UiFonts.Measure(p.DisplayName, size, UiWeight.Black);
        RenderHelper.DrawGenderSymbol((int)(x + w + 10), (int)(centerY - size * 0.36f), (int)(size * 0.7f), p.Gender);
    }

    // ------------------------------------------------------------------ battle

    /// <summary>
    /// Draws the HP boxes, move effects and the bottom panel. Returns false for the menus not rebuilt yet (switching
    /// and the bag), whose panels the caller draws.
    /// </summary>
    public static bool DrawBattle(BattleHUD hud, int sw, int sh, Pokemon active, Party? trainerParty, string message, BattleVFX vfx, BattleAnimator anim)
    {
        float enemySlide = BattleHUD.BoxSlide(anim, anim.Enemy);
        if (enemySlide >= 0f) EnemyBox(56 - 720 * enemySlide, 52, anim.Enemy);
        float playerSlide = BattleHUD.BoxSlide(anim, anim.Player);
        if (playerSlide >= 0f) PlayerBox(1240 + 760 * playerSlide, 650, anim.Player);

        vfx.Draw();
        if (hud.MenuState is BattleMenuState.SwitchPokemon or BattleMenuState.SelectBagItem) return false;

        switch (hud.MenuState)
        {
            case BattleMenuState.Main:
                MessageBox(new Rectangle(48, 858, 1060, 172), null, active.DisplayName);
                string[] labels = { "BAG", "POKÉMON", "RUN" };
                Color[] colors = { Gold, Green, Blue };
                Button(new Rectangle(1140, 848, 432, 190), 34, Red, "FIGHT", 60, hud.MainMenuIndex == 0);
                for (int i = 0; i < 3; i++)
                    Button(new Rectangle(1600, 848 + i * 66, 272, 58), 29, colors[i], labels[i], 28, hud.MainMenuIndex == i + 1);
                break;
            case BattleMenuState.Moves:
                MoveMenu(hud, active);
                break;
            default:
                MessageBox(new Rectangle(48, 858, 1824, 172), message, null);
                break;
        }
        return true;
    }

    private static void EnemyBox(float x, float y, CombatantView view)
    {
        var p = view.Shown!;
        var r = new Rectangle(x, y, 580, 120);
        Panel(r, 22, skew: -0.2f);
        NameWithGender(p, x + 38, y + 38, 36);
        Level(x + r.Width - 46, y + 38, p.Level, 34);
        HpBar(x + 38, y + 72, r.Width - 90, 24, view.DisplayedHp / Math.Max(1, p.MaxHP));
        if (p.Status != StatusCondition.None) RenderHelper.DrawStatusBadge((int)(x + 300), (int)(y + 24), p.Status);
    }

    private static void PlayerBox(float x, float y, CombatantView view)
    {
        var p = view.Shown!;
        var r = new Rectangle(x, y, 628, 162);
        Panel(r, 22, skew: 0.2f);
        NameWithGender(p, x + 52, y + 40, 36);
        Level(x + r.Width - 40, y + 40, p.Level, 34);
        int hp = (int)MathF.Ceiling(view.DisplayedHp);
        HpBar(x + 52, y + 74, r.Width - 96, 24, view.DisplayedHp / Math.Max(1, p.MaxHP));
        string hpText = $"{hp} / {p.MaxHP}";
        float hw = UiFonts.Measure(hpText, 30, UiWeight.Black);
        UiFonts.DrawCentered(hpText, x + r.Width - 44 - hw, y + 122, 30, Ink, UiWeight.Black);
        if (p.Status != StatusCondition.None) RenderHelper.DrawStatusBadge((int)(x + 52), (int)(y + 108), p.Status);

        // EXP runs along the bottom edge of the box
        var exp = new Rectangle(x + 40, y + r.Height - 18, 360, 8);
        UiShapes.Fill(exp, 4, Track);
        float ew = exp.Width * Math.Clamp(view.DisplayedExp, 0f, 1f);
        if (ew > 1f) UiShapes.Fill(new Rectangle(exp.X, exp.Y, Math.Max(ew, 8), 8), 4, new Color(72, 176, 250, 255));
    }

    public static void MessageBox(Rectangle r, string? message, string? prompting)
    {
        Panel(r, 28);
        float x = r.X + 52, cy = r.Y + r.Height / 2f;
        if (prompting != null)
        {
            UiFonts.DrawCentered("What will", x, cy, 40, Ink, UiWeight.ExtraBold);
            float w = UiFonts.Measure("What will ", 40, UiWeight.ExtraBold);
            UiFonts.DrawCentered(prompting, x + w, cy, 40, Red, UiWeight.Black);
            float w2 = UiFonts.Measure(prompting + " ", 40, UiWeight.Black);
            UiFonts.DrawCentered("do?", x + w + w2, cy, 40, Ink, UiWeight.ExtraBold);
            return;
        }
        UiFonts.DrawCentered(message ?? "", x, cy, 40, Ink, UiWeight.ExtraBold);
        AdvanceArrow(r.X + r.Width - 64, r.Y + r.Height - 52);
    }

    private static void AdvanceArrow(float x, float y)
    {
        float bob = MathF.Sin((float)Raylib.GetTime() * 6f) * 3f;
        // Both windings, so it shows whichever way the batch culls
        Raylib.DrawTriangle(new Vector2(x, y + bob), new Vector2(x + 28, y + bob), new Vector2(x + 14, y + 20 + bob), Red);
        Raylib.DrawTriangle(new Vector2(x, y + bob), new Vector2(x + 14, y + 20 + bob), new Vector2(x + 28, y + bob), Red);
    }

    private static void MoveMenu(BattleHUD hud, Pokemon p)
    {
        for (int i = 0; i < 4; i++)
        {
            var r = new Rectangle(48 + (i % 2) * 556, 848 + (i / 2) * 100, 536, 88);
            bool selected = hud.MoveMenuIndex == i;
            if (i >= p.Moves.Count)
            {
                UiShapes.Shape(r, 26, new Color(226, 232, 242, 220), new Color(214, 222, 236, 220), new Color(180, 190, 210, 255), 3);
                UiFonts.DrawCentered("—", r.X + 40, r.Y + r.Height / 2f, 32, Muted, UiWeight.Black);
                continue;
            }
            var move = p.Moves[i];
            var type = Palette.GetTypeColor(move.Type.ToString());
            if (selected) UiShapes.Shadow(r, 26, 26, Vector2.Zero, type with { A = 170 });
            else UiShapes.Shadow(r, 26, 14, new Vector2(0, 6), ShadowColor);
            UiShapes.Shape(r, 26, PanelTop, PanelBottom, selected ? Darker(type, 0.15f) : Frame, selected ? 5f : 3f);
            var band = new Rectangle(r.X + 10, r.Y + 10, 124, r.Height - 20);
            UiShapes.Shape(band, 18, Lighter(type, 0.1f), Darker(type, 0.1f));
            string typeName = move.Type.ToString().ToUpperInvariant();
            float tw = UiFonts.Measure(typeName, 20, UiWeight.Black);
            UiFonts.DrawCentered(typeName, band.X + (band.Width - tw) / 2f, band.Y + band.Height / 2f, 20, Color.White, UiWeight.Black);
            UiFonts.DrawCentered(move.Name, r.X + 156, r.Y + r.Height / 2f, 32, Ink, UiWeight.Black);
            string pp = $"{move.CurrentPP}/{move.MaxPP}";
            float pw = UiFonts.Measure(pp, 26, UiWeight.Black);
            UiFonts.DrawCentered(pp, r.X + r.Width - 30 - pw, r.Y + r.Height / 2f, 26, Ink, UiWeight.Black);
            UiFonts.DrawCentered("PP", r.X + r.Width - 40 - pw - UiFonts.Measure("PP", 18, UiWeight.ExtraBold), r.Y + r.Height / 2f + 3, 18, Muted, UiWeight.ExtraBold);
        }

        var info = new Rectangle(1172, 848, 700, 188);
        Panel(info, 28);
        if (hud.MoveMenuIndex < p.Moves.Count)
        {
            var move = p.Moves[hud.MoveMenuIndex];
            string cat = move.Category.ToString().ToUpperInvariant();
            UiFonts.DrawCentered(cat, info.X + 44, info.Y + 44, 24, Muted, UiWeight.Black);
            string pwr = move.Power > 0 ? move.Power.ToString() : "—";
            string acc = move.Accuracy > 0 ? move.Accuracy.ToString() : "—";
            UiFonts.DrawCentered("POWER", info.X + 260, info.Y + 44, 20, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered(pwr, info.X + 350, info.Y + 44, 30, Ink, UiWeight.Black);
            UiFonts.DrawCentered("ACCURACY", info.X + 440, info.Y + 44, 20, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered(acc, info.X + 580, info.Y + 44, 30, Ink, UiWeight.Black);
            DrawWrapped(move.Description, info.X + 44, info.Y + 92, info.Width - 88, 26, Ink, 36);
        }
    }

    public static void DrawWrapped(string text, float x, float y, float maxWidth, float size, Color color, float lineHeight, UiWeight weight = UiWeight.Bold)
    {
        string line = "";
        foreach (var word in text.Split(' '))
        {
            string test = line.Length == 0 ? word : line + " " + word;
            if (UiFonts.Measure(test, size, weight) > maxWidth && line.Length > 0)
            {
                UiFonts.Draw(line, x, y, size, color, weight);
                y += lineHeight;
                line = word;
            }
            else line = test;
        }
        if (line.Length > 0) UiFonts.Draw(line, x, y, size, color, weight);
    }

    // ------------------------------------------------------------------ party

    /// <summary>Full-screen menu backdrop: deep teal with faint diagonal stripes.</summary>
    public static void Backdrop(int sw, int sh)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, new Color(44, 112, 146, 255), new Color(24, 60, 98, 255));
        for (int i = -10; i < 40; i++)
            Raylib.DrawRectanglePro(new Rectangle(i * 90, -200, 34, 1600), Vector2.Zero, 28f, new Color(255, 255, 255, 12));
    }

    public static void DrawParty(int sw, int sh, Party party, int selected, int? swapping)
    {
        Backdrop(sw, sh);


        UiFonts.Draw("POKÉMON", 64, 36, 52, Color.White, UiWeight.Black);
        HintPill(1320, 44, "Z", "Summary");
        HintPill(1540, 44, "X", "Move");
        HintPill(1710, 44, "Esc", "Back");

        for (int i = 0; i < 6; i++)
        {
            var r = new Rectangle(64 + (i % 2) * 912, 132 + (i / 2) * 256, 880, 232);
            if (i >= party.Count)
            {
                UiShapes.Shape(r, 34, new Color(255, 255, 255, 30), new Color(255, 255, 255, 18), new Color(255, 255, 255, 60), 3);
                continue;
            }
            PartyCard(r, party.Members[i], i == selected, i == swapping, i == 0);
        }

        var prompt = new Rectangle(64, 920, 1792, 112);
        Panel(prompt, 30);
        UiFonts.DrawCentered(swapping.HasValue ? "Move to where?" : "Choose a Pokémon.", prompt.X + 52, prompt.Y + prompt.Height / 2f, 40, Ink, UiWeight.ExtraBold);
    }

    /// <summary>A key cap and what it does, for the top-right corner of menu screens.</summary>
    public static void HintPill(float x, float y, string key, string label)
    {
        float kw = Math.Max(44, UiFonts.Measure(key, 22, UiWeight.Black) + 24);
        float lw = UiFonts.Measure(label, 24, UiWeight.ExtraBold);
        var r = new Rectangle(x, y, kw + lw + 34, 52);
        UiShapes.Fill(r, 26, new Color(10, 30, 60, 90));
        var k = new Rectangle(x + 6, y + 6, kw, 40);
        UiShapes.Fill(k, 20, Color.White);
        UiFonts.DrawCentered(key, k.X + (kw - UiFonts.Measure(key, 22, UiWeight.Black)) / 2f, k.Y + 20, 22, Ink, UiWeight.Black);
        UiFonts.DrawCentered(label, k.X + kw + 12, y + 26, 24, Color.White, UiWeight.ExtraBold);
    }

    private static void PartyCard(Rectangle r, Pokemon p, bool selected, bool swapping, bool lead)
    {
        var accent = swapping ? Gold : new Color(240, 104, 70, 255);
        if (selected || swapping) UiShapes.Shadow(r, 34, 34, Vector2.Zero, accent with { A = 190 });
        else UiShapes.Shadow(r, 34, 24, new Vector2(0, 10), new Color(6, 14, 34, 110));
        UiShapes.Shape(r, 34, PanelTop, PanelBottom, selected || swapping ? accent : Frame, selected || swapping ? 6f : 4f);

        // Portrait: the Pokémon's 2D pixel sprite at a whole-number scale on a pale disc with a faint Poké Ball
        // line. Like the main games, the icons hop: the selected one quickly and high, the rest gently.
        var c = new Vector2(r.X + 136, r.Y + r.Height / 2f);
        UiShapes.Circle(c, 96, new Color(226, 234, 246, 255));
        UiShapes.Fill(new Rectangle(c.X - 96, c.Y - 3, 192, 6), 3, new Color(208, 218, 234, 255));
        UiShapes.Circle(c, 26, new Color(208, 218, 234, 255));
        UiShapes.Circle(c, 17, new Color(226, 234, 246, 255));
        var icon = PixelArtGenerator.GetPokemonIcon(p.Species.Name);
        const int scale = 4;
        float t = (float)Raylib.GetTime();
        int hop = p.IsFainted ? 0 : selected ? ((int)(t / 0.16f) % 2) * 3 * scale : ((int)(t / 0.4f) % 2) * scale;
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
            new Rectangle(MathF.Round(c.X - icon.Width * scale / 2f), MathF.Round(c.Y - icon.Height * scale / 2f - 6 - hop), icon.Width * scale, icon.Height * scale),
            Vector2.Zero, 0, Color.White);

        float x = r.X + 264;
        NameWithGender(p, x, r.Y + 58, 42);
        Level(r.X + r.Width - 44, r.Y + 58, p.Level, 40);

        // Type pills
        float tx = x;
        foreach (var type in p.Species.SecondaryType.HasValue ? new[] { p.Species.PrimaryType, p.Species.SecondaryType.Value } : new[] { p.Species.PrimaryType })
        {
            var tc = Palette.GetTypeColor(type.ToString());
            string name = type.ToString().ToUpperInvariant();
            float w = UiFonts.Measure(name, 20, UiWeight.Black) + 36;
            var pill = new Rectangle(tx, r.Y + 94, w, 34);
            UiShapes.Shape(pill, 17, Lighter(tc, 0.12f), Darker(tc, 0.1f));
            UiFonts.DrawCentered(name, pill.X + 18, pill.Y + 17, 20, Color.White, UiWeight.Black);
            tx += w + 10;
        }
        if (p.Status != StatusCondition.None) RenderHelper.DrawStatusBadge((int)(tx + 6), (int)(r.Y + 99), p.Status);

        HpBar(x, r.Y + 150, r.Width - 308, 26, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
        string hpText = $"{p.CurrentHP} / {p.MaxHP}";
        float hw = UiFonts.Measure(hpText, 30, UiWeight.Black);
        UiFonts.DrawCentered(hpText, r.X + r.Width - 44 - hw, r.Y + 200, 30, Ink, UiWeight.Black);
        if (lead) UiFonts.DrawCentered("LEAD", x, r.Y + 200, 22, Muted, UiWeight.Black);

        if (p.IsFainted) UiShapes.Fill(r, 34, new Color(40, 40, 60, 90));
    }

    // ------------------------------------------------------------------ dialogue

    public static void DrawDialogue(int sw, int sh, string speaker, string visibleText, bool complete)
    {
        var r = new Rectangle(96, sh - 262, sw - 192, 222);
        Panel(r, 32);
        if (!string.IsNullOrEmpty(speaker))
        {
            float w = UiFonts.Measure(speaker, 30, UiWeight.Black) + 56;
            var tag = new Rectangle(r.X + 44, r.Y - 30, w, 58);
            UiShapes.Shadow(tag, 29, 12, new Vector2(0, 5), ShadowColor);
            UiShapes.Shape(tag, 29, Lighter(Red, 0.12f), Darker(Red, 0.08f), Darker(Red, 0.3f), 3);
            UiFonts.DrawCentered(speaker, tag.X + 28, tag.Y + 29, 30, Color.White, UiWeight.Black);
        }
        DrawWrapped(visibleText, r.X + 60, r.Y + 58, r.Width - 170, 40, Ink, 58, UiWeight.ExtraBold);
        if (complete) AdvanceArrow(r.X + r.Width - 80, r.Y + r.Height - 60);
    }
}
