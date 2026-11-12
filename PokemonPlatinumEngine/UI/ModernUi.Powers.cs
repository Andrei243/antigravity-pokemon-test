using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Power Watch's panel and the powers' chips over the field (plan 36 · TR8; style guide, "The Power Watch").</summary>
internal static partial class ModernUi
{
    /// <summary>Each kind's colour: its icon's disc on the panel and its chip over the field.</summary>
    internal static Color PowerColour(PowerKind kind) => kind switch
    {
        PowerKind.Exp => new Color(70, 136, 232, 255),
        PowerKind.Effort => new Color(232, 112, 64, 255),
        PowerKind.Money => new Color(214, 160, 40, 255),
        PowerKind.Bargain => new Color(56, 168, 120, 255),
        PowerKind.Catch => new Color(226, 72, 84, 255),
        PowerKind.Encounter => new Color(96, 176, 64, 255),
        PowerKind.Stealth => new Color(110, 100, 168, 255),
        PowerKind.Hatch => new Color(230, 130, 170, 255),
        PowerKind.TypeEncounter => new Color(60, 160, 176, 255),
        _ => new Color(150, 120, 90, 255)
    };

    /// <summary>A kind's short mark, three or four letters (<c>powerWatch.short.*</c>).</summary>
    internal static string PowerShort(PowerKind kind)
    {
        string key = kind.ToString();
        key = char.ToLowerInvariant(key[0]) + key[1..];
        return Text.Of($"powerWatch.short.{key}");
    }

    /// <summary>A kind's icon: a disc of its colour with its short mark in white.</summary>
    internal static void PowerIcon(Vector2 at, float radius, PowerKind kind, byte alpha = 255)
    {
        var colour = PowerColour(kind) with { A = alpha };
        UiShapes.Circle(at, radius, colour);
        UiShapes.Ring(at, radius, 3, Darker(colour, 0.2f) with { A = alpha });
        string mark = PowerShort(kind);
        float size = radius * (mark.Length > 3 ? 0.62f : 0.74f);
        UiFonts.DrawCentered(mark, at.X - UiFonts.Measure(mark, size, UiWeight.Black) / 2f, at.Y, size, Color.White with { A = alpha }, UiWeight.Black);
    }

    /// <summary>Minutes and seconds of play left, as "9:42".</summary>
    internal static string PowerTime(float seconds)
    {
        int s = Math.Max(0, (int)MathF.Ceiling(seconds));
        return $"{s / 60}:{s % 60:D2}";
    }

    public static void DrawPowers(int sw, int sh, PowersScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("powerWatch.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.move"), Hint("shared.keys.leftRight", "powerWatch.hintLevel"),
            Hint("shared.keys.z", "powerWatch.hintUse"), Hint("shared.keys.esc", "shared.hints.back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float listW = 1060;
        var list = new Rectangle(Margin - slide, ContentTop, listW, ContentBottom - ContentTop);
        var side = new Rectangle(Margin + listW + Gutter + slide, ContentTop, sw - Margin * 2 - listW - Gutter, ContentBottom - ContentTop);
        Panel(list, 34);
        Panel(side, 34);
        var powers = screen.Powers;

        // The rows: today's free power first, then each kind learned with its level and cost
        if (screen.RowCount == 0)
            DrawWrapped(Text.Of("powerWatch.noneLearned"), list.X + 44, list.Y + 40, list.Width - 88, 30, Muted, 40);
        int row = 0;
        if (screen.Free is { } free)
        {
            var r = RowRect(list, row, 24);
            var ink = ListRow(r, screen.Cursor == row, Text.Of("powerWatch.today", ("power", PowerTable.NameOf(free.Kind))), nameX: 148);
            PowerIcon(new Vector2(r.X + 70, r.Y + r.Height / 2f), 30, free.Kind);
            RowValue(r, Text.Of("powerWatch.freeLevel", ("level", free.Level)), ink);
            row++;
        }
        foreach (var kind in screen.Kinds)
        {
            var r = RowRect(list, row, 24);
            int level = screen.LevelOf(kind);
            bool can = powers.WhyNot(kind, level, screen.NoPowers) == PowerRefusal.None;
            var ink = ListRow(r, screen.Cursor == row, PowerTable.NameOf(kind), enabled: can, nameX: 148);
            PowerIcon(new Vector2(r.X + 70, r.Y + r.Height / 2f), 30, kind, can ? (byte)255 : (byte)150);
            string value = powers.Learned(kind) > 1
                ? Text.Of("powerWatch.levelOf", ("level", level), ("most", powers.Learned(kind)), ("cost", PowerTable.CostOf(level)))
                : Text.Of("powerWatch.levelCost", ("level", level), ("cost", PowerTable.CostOf(level)));
            RowValue(r, value, ink);
            row++;
        }
        if (screen.Message is { } message)
            DrawWrapped(message, list.X + 44, list.Y + list.Height - 96, list.Width - 88, 30, screen.Refused ? Red : Ink, 40, UiWeight.ExtraBold);

        // The charge as ten pips, then what is in force with its minutes
        Label(Text.Of("powerWatch.charge"), side.X + 44, side.Y + 30);
        int most = PowerTable.ChargeMost;
        float pip = (side.Width - 88 - (most - 1) * 10) / most;
        for (int i = 0; i < most; i++)
            UiShapes.Fill(new Rectangle(side.X + 44 + i * (pip + 10), side.Y + 66, pip, 34), 8, i < powers.Charge ? Gold : Rule);
        string steps = Text.Of("powerWatch.chargeSteps", ("charge", powers.Charge), ("most", most), ("steps", PowerTable.StepsPerCharge));
        UiFonts.Draw(steps, side.X + 44, side.Y + 118, 24, Muted, UiWeight.ExtraBold);

        UiShapes.Fill(new Rectangle(side.X + 44, side.Y + 172, side.Width - 88, 3), 1.5f, Rule);
        Label(Text.Of("powerWatch.inForce"), side.X + 44, side.Y + 196);
        var inForce = powers.InForce;
        if (inForce.Count == 0) UiFonts.Draw(Text.Of("powerWatch.noneInForce"), side.X + 44, side.Y + 240, 28, Muted, UiWeight.ExtraBold);
        for (int i = 0; i < inForce.Count; i++)
        {
            var p = inForce[i];
            float y = side.Y + 250 + i * 72;
            PowerIcon(new Vector2(side.X + 74, y + 4), 26, p.Kind);
            UiFonts.DrawCentered(Text.Of("powerWatch.inForceRow", ("power", PowerTable.NameOf(p.Kind)), ("level", p.Level)), side.X + 116, y + 4, 28, Ink, UiWeight.Black);
            string left = PowerTime(p.SecondsLeft);
            UiFonts.DrawCentered(left, side.X + side.Width - 44 - UiFonts.Measure(left, 28, UiWeight.Black), y + 4, 28, Ink, UiWeight.Black);
        }
    }

    /// <summary>The left of the powers' chips over the field, and the first one's top: clear of the pinned request and the
    /// location sign above, the Safari Game's count and the run timer at the right, and the autosave disc at the bottom left.</summary>
    public const float PowerChipLeft = 32, PowerChipTop = 176, PowerChipHeight = 48, PowerChipGap = 10;

    /// <summary>The powers in force as chips over the field (plan 36 · TR8): one under another at the left, each its icon and the minutes left.</summary>
    public static void PowerChips(IReadOnlyList<ActivePower> inForce)
    {
        for (int i = 0; i < inForce.Count; i++)
        {
            var p = inForce[i];
            var r = PowerChipRect(i, p);
            UiShapes.Shape(r, r.Height / 2f, new Color(255, 255, 255, 236), new Color(234, 240, 248, 236), Frame, 3);
            PowerIcon(new Vector2(r.X + r.Height / 2f, r.Y + r.Height / 2f), r.Height / 2f - 6, p.Kind);
            UiFonts.DrawCentered(PowerTime(p.SecondsLeft), r.X + r.Height + 6, r.Y + r.Height / 2f, 26, Ink, UiWeight.Black);
        }
    }

    /// <summary>A chip's width: room for the icon and "10:00".</summary>
    public const float PowerChipWidth = 156;

    /// <summary>Where a chip stands (GPU-free: tests hold them clear of the field's other corners).</summary>
    public static Rectangle PowerChipRect(int index, ActivePower power) =>
        new(PowerChipLeft, PowerChipTop + index * (PowerChipHeight + PowerChipGap), PowerChipWidth, PowerChipHeight);
}
