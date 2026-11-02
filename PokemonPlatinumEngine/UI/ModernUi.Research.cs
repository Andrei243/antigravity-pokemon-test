using System;
using System.Globalization;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The Pokédex's TASKS page and the list's research level (plan 15 · E2; style guide, "Menu screens", the Pokédex's TASKS)
internal static partial class ModernUi
{
    /// <summary>The research's colour: the TASKS tab, the level's disc and the rungs reached.</summary>
    public static readonly Color ResearchPlum = new(120, 72, 176, 255);

    /// <summary>A task's line, what it counts in words.</summary>
    public static string TaskLine(ResearchTask task)
    {
        string kind = task.Kind.ToString();
        return task.Move is { } move
            ? Text.Of($"pokedex.tasks.kind.{kind}", ("move", Data.LocalNames.Move(move)))
            : Text.Of($"pokedex.tasks.kind.{kind}");
    }

    /// <summary>A species' research level on the list's row: a small plum disc, none at level 0.</summary>
    private static void ResearchLevelChip(Rectangle r, int level, bool selected)
    {
        if (level <= 0) return;
        var c = new Vector2(r.X + r.Width - 150, r.Y + r.Height / 2f);
        UiShapes.Circle(c, 17, selected ? Color.White : ResearchPlum);
        string n = level.ToString(CultureInfo.InvariantCulture);
        float w = UiFonts.Measure(n, 22, UiWeight.Black);
        UiFonts.DrawCentered(n, c.X - w / 2f, c.Y, 22, selected ? ResearchPlum : Color.White, UiWeight.Black);
    }

    /// <summary>TASKS: the level on a disc with the points beside it, then each task with its count and its rungs.</summary>
    private static void DexTasks(Rectangle detail, Models.PokedexEntry entry, Research research)
    {
        var s = entry.Species;
        int level = research.LevelOf(s);
        var (points, of) = research.Points(s);
        bool done = level >= Research.MaxLevel;

        var disc = new Vector2(detail.X + 44 + 64, detail.Y + 124 + 64);
        UiShapes.Circle(disc, 64, done ? Gold : ResearchPlum);
        string n = level.ToString(CultureInfo.InvariantCulture);
        float nw = UiFonts.Measure(n, 72, UiWeight.Black);
        UiFonts.DrawCentered(n, disc.X - nw / 2f, disc.Y, 72, done ? Ink : Color.White, UiWeight.Black);

        float x = disc.X + 64 + 36, right = detail.X + detail.Width - 44;
        Label(Text.Of("pokedex.tasks.level"), x, detail.Y + 136);
        UiFonts.Draw(Text.Of("pokedex.tasks.points", ("points", points), ("of", of)), x, detail.Y + 162, 34, Ink, UiWeight.Black);
        Bar(new Rectangle(x, detail.Y + 214, right - x, 12), of == 0 ? 0f : (float)points / of, done ? Gold : ResearchPlum);

        UiShapes.Fill(new Rectangle(detail.X + 44, detail.Y + 254, detail.Width - 88, 3), 1.5f, Rule);

        var tasks = ResearchTasks.For(s);
        var record = research.RecordOf(s.DexNumber);
        float y = detail.Y + 274;
        foreach (var task in tasks)
        {
            var row = new Rectangle(detail.X + 44, y, detail.Width - 88, 66);
            Card(row, 22, false);
            float cy = row.Y + row.Height / 2f;
            UiFonts.DrawCentered(TaskLine(task), row.X + 32, cy, 28, Ink, UiWeight.Bold);

            int count = record.CountOf(task.Key);
            int reached = task.PointsFor(count);
            // The rungs as pips at the row's right, plum where reached
            float pipRight = row.X + row.Width - 32;
            for (int i = 0; i < task.Rungs.Count; i++)
            {
                var pip = new Vector2(pipRight - (task.Rungs.Count - 1 - i) * 26, cy);
                UiShapes.Circle(pip, 9, i < reached ? ResearchPlum : Rule);
            }
            float pipsLeft = pipRight - (task.Rungs.Count - 1) * 26 - 9;
            string value = task.NextFor(count) is { } next
                ? Text.Of("pokedex.tasks.count", ("count", count), ("next", next))
                : count.ToString(CultureInfo.InvariantCulture);
            float vw = UiFonts.Measure(value, 26, UiWeight.Black);
            UiFonts.DrawCentered(value, pipsLeft - 28 - vw, cy, 26, task.NextFor(count) == null ? Green : Muted, UiWeight.Black);
            y += 78;
        }
    }
}
