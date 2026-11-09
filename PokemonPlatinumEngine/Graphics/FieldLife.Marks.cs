using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>What the story has laid on a tile for the field to show (plan 21 · part r): how much salt is left, a cave's mouth opened, deep water laid.</summary>
internal enum StoryMarkKind { SaltHeap, SaltLittle, CaveMouth, DeepWater }

/// <summary>One mark the story has laid on a tile.</summary>
internal readonly record struct StoryMark(int X, int Y, StoryMarkKind Kind);

/// <summary>
/// Marks the ground carries while something is on it or the story has changed it (plan 21 · part r; style guide, "Life",
/// "Planks that give" and "Hoenn's last things"). A plank of Hoenn that someone stands on is pressed down, as Emerald's
/// Fortree bridge swaps the plank's metatile: the player's plank, and the plank under a Kecleon nobody can see, which is
/// how the bridge on Route 120 shows that something stands there until it is gone. Shoal Cave's piles of salt are heaps of
/// white crystals, flattened once the day's salt is taken; under the abnormal weather a cave's mouth opens in the rock
/// and deep water darkens the sea, where Emerald lays their metatiles by script. Each mark is a block of two cells by two
/// of the life atlas, 32 texels to the tile like everything in the field. GPU-free.
/// </summary>
internal sealed partial class FieldLife
{
    /// <summary>The atlas's blocks of the marks, each the top-left cell of two by two: a plank pressed down, a whole heap of salt, a little salt left, a cave's mouth, deep water.</summary>
    public const int PressedPlank = 16, SaltHeap = 18, SaltLittle = 20, CaveMouth = 22, DeepWater = 32;

    /// <summary>The rows of cells the atlas has: two of life's pieces and four of the marks' blocks.</summary>
    public const int AtlasRows = 6;

    /// <summary>How far a lying mark is lifted over what it lies on, so it never fights that face.</summary>
    public const float MarkLift = 0.01f, DeckLift = 0.1f;

    private static readonly Color PlankShadow = new(64, 42, 28, 170), SaltTint = new(246, 244, 238, 255), MouthTint = new(255, 255, 255, 255),
        DeepTint = new(14, 30, 70, 150);

    private readonly List<StoryMark> marks = new();
    private string? marksMap;

    /// <summary>The marks the story has laid on the map now shown: told by the engine whenever the story or the map changes.</summary>
    public void SetMarks(Map map, IEnumerable<StoryMark> laid)
    {
        marks.Clear();
        deepWater.Clear();
        var deep = new HashSet<(int, int)>();
        foreach (var mark in laid)
            if (mark.Kind == StoryMarkKind.DeepWater) deep.Add((mark.X, mark.Y));
            else marks.Add(mark);
        // Deep water is one patch to each run of its tiles, so it has no seams between them
        var seen = new HashSet<(int, int)>();
        foreach (var start in deep)
        {
            if (!seen.Add(start)) continue;
            int x0 = start.Item1, x1 = start.Item1, y0 = start.Item2, y1 = start.Item2;
            var run = new Stack<(int X, int Y)>();
            run.Push(start);
            while (run.Count > 0)
            {
                var (x, y) = run.Pop();
                x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
                foreach (var next in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                    if (deep.Contains(next) && seen.Add(next)) run.Push(next);
            }
            deepWater.Add((x0, y0, x1 - x0 + 1, y1 - y0 + 1));
        }
        marksMap = map.Name;
    }

    private readonly List<(int X, int Y, int Width, int Depth)> deepWater = new();

    public int MarkCount => marks.Count + deepWater.Count;

    /// <summary>
    /// The marks there are to draw now: <paramref name="flat"/> the pressed planks and the deep water, lying down, and
    /// <paramref name="upright"/> the piles of salt and the caves' mouths, standing up. <paramref name="people"/> is who may
    /// be in sight, whose unseen ones press their planks. A mark is a tile across.
    /// </summary>
    public void MarkQuads(Map map, IReadOnlyList<NPC> people, List<LifeQuad> flat, List<LifeQuad> upright)
    {
        if (plankMap is { } m && string.Equals(m, map.Name, StringComparison.Ordinal) && IsPlank(map, plankX, plankY))
            flat.Add(Pressed(map, plankX, plankY, map.DeckAt(plankX, plankY)));
        foreach (var npc in people)
            if (npc.IsUnseen && UnderAnUnseenFoot(map, npc.GridX, npc.GridY))
                flat.Add(Pressed(map, npc.GridX, npc.GridY, npc.Level ?? map.DeckAt(npc.GridX, npc.GridY)));

        if (!string.Equals(marksMap, map.Name, StringComparison.Ordinal)) return;
        foreach (var (x, y, kind) in marks)
        {
            float ground = Relief.At(map, x + 0.5f, y + 0.5f);
            switch (kind)
            {
                case StoryMarkKind.SaltHeap:
                case StoryMarkKind.SaltLittle:
                    upright.Add(new LifeQuad(new Vector3(x + 0.5f, ground, y + 0.6f), 1f, 1f, kind == StoryMarkKind.SaltHeap ? SaltHeap : SaltLittle, SaltTint));
                    break;
                case StoryMarkKind.CaveMouth:
                    // Stood at the tile's foot, against the rock it opens in
                    upright.Add(new LifeQuad(new Vector3(x + 0.5f, ground, y + 0.98f), 1f, 1f, CaveMouth, MouthTint));
                    break;
            }
        }
        foreach (var (x, y, w, d) in deepWater)
        {
            float cx = x + w / 2f, cz = y + d / 2f;
            flat.Add(new LifeQuad(new Vector3(cx, Relief.At(map, cx, cz) + MarkLift, cz), w, d, DeepWater, DeepTint));
        }
    }

    /// <summary>Whether something unseen standing on a tile presses a plank down: one of Hoenn's plank bridges, or any deck of Hoenn's over water or a gorge (Route 120's bridge by Fortree).</summary>
    public static bool UnderAnUnseenFoot(Map map, int x, int y) =>
        IsPlank(map, x, y) || map.InBounds(x, y) && map.DeckAt(x, y) != null && InHoenn(map.Name);

    private static LifeQuad Pressed(Map map, int x, int y, float? standing)
    {
        float height = standing is { } s ? Relief.Under(map, x + 0.5f, y + 0.5f, s) : Relief.At(map, x + 0.5f, y + 0.5f);
        // A deck's boards stand a little over the height it is walked at: the mark lies on them
        return new LifeQuad(new Vector3(x + 0.5f, height + DeckLift, y + 0.5f), 1f, 1f, PressedPlank, PlankShadow);
    }

    /// <summary>Whether a cell of the atlas is the top-left of one of the marks' blocks, drawn two cells by two and lit as the ground is (not as water thrown up).</summary>
    public static bool IsMark(int cell) => cell >= PressedPlank && cell is not (ShiverLeft or ShiverRight);

    /// <summary>Whether a cell is water thrown up (rings, drops, flecks, bubbles), which catches the sky and dims only half as far as the ground does.</summary>
    public static bool IsWet(int cell) => cell >= Ring && cell < PressedPlank;
}

internal static partial class LifeArt
{
    private const int Block = FieldLife.Cell * 2;

    private static (int X, int Y) BlockAt(int cell) => (FieldLife.Cell * (cell % FieldLife.Columns), FieldLife.Cell * (cell / FieldLife.Columns));

    /// <summary>
    /// The marks' blocks, 32 texels square, in whites and greys to be tinted (but the cave's mouth, in its own colours): a
    /// plank pressed down (its north edge in deep shade where it has dropped from the plank beyond, its sides opened into
    /// dark gaps, a little shade over the whole), a heap of salt crystals (a mound lit on its left, shaded on its right,
    /// glints of crystal), the little left of one (a low scatter of crystals on a damp ring), a cave's mouth (a dark arch
    /// in a rim of rock) and deep water (a soft-edged patch, mottled).
    /// </summary>
    internal static void PaintMarks(PixelCanvas c)
    {
        var white = new Color(255, 255, 255, 255);
        var shade = new Color(198, 200, 212, 255);
        var deep = new Color(150, 152, 168, 255);

        // A plank pressed down: shade deepest along its north edge, the gaps either side, a faint shade across it
        var (px, py) = BlockAt(FieldLife.PressedPlank);
        for (int y = 0; y < Block; y++)
            for (int x = 0; x < Block; x++)
            {
                int alpha = y < 3 ? 255 : y < 6 ? 170 : x < 2 || x >= Block - 2 ? 190 : y >= Block - 2 ? 50 : 64;
                c.SetRaw(px + x, py + y, new Color(255, 255, 255, alpha));
            }

        // A heap of salt: a mound twenty texels high on a base twenty-six wide, standing on the block's foot
        var (sx, sy) = BlockAt(FieldLife.SaltHeap);
        for (int y = 9; y < Block - 2; y++)
        {
            float t = (y - 9) / (float)(Block - 12);
            int half = (int)MathF.Round(3 + MathF.Sqrt(t) * 10);
            for (int x = 16 - half; x < 16 + half; x++)
            {
                Color colour = x >= 16 + half / 3 ? shade : white;
                if (x >= 16 + half - 2 || y >= Block - 4) colour = deep;
                c.SetRaw(sx + x, sy + y, colour);
            }
        }
        foreach (var (gx, gy, lit) in new[] { (12, 14, false), (10, 20, false), (19, 18, true), (14, 11, true), (8, 25, false), (21, 24, false), (15, 22, true) })
            c.SetRaw(sx + gx, sy + gy, lit ? white : deep);
        Rim(c, sx, sy, Block, new Color(112, 114, 132, 255));

        // The little left: a damp ring on the floor and a low scatter of crystals on it
        var (lx, ly) = BlockAt(FieldLife.SaltLittle);
        var damp = new Color(120, 122, 136, 150);
        for (int y = 25; y < 30; y++)
            for (int x = 4; x < 28; x++)
            {
                float u = (x + 0.5f - 16f) / 12f, v = (y + 0.5f - 27.5f) / 2.6f;
                if (u * u + v * v <= 1f) c.SetRaw(lx + x, ly + y, damp);
            }
        c.Rect(lx + 10, ly + 24, 12, 3, shade);
        c.Rect(lx + 12, ly + 22, 7, 2, white);
        c.Rect(lx + 18, ly + 23, 3, 1, shade);
        foreach (var (gx, gy) in new[] { (7, 27), (24, 26), (21, 28), (9, 25), (14, 21) }) c.SetRaw(lx + gx, ly + gy, white);
        Rim(c, lx, ly, Block, new Color(112, 114, 132, 255));

        // A cave's mouth: a dark arch in a rim of rough rock, its floor lit a little at the threshold
        var (cx, cy) = BlockAt(FieldLife.CaveMouth);
        Color rock = new(122, 110, 100, 255), rockLit = new(156, 144, 130, 255), rockDark = new(84, 74, 70, 255), hole = new(18, 14, 22, 255), sill = new(60, 50, 52, 255);
        for (int y = 2; y < Block; y++)
            for (int x = 0; x < Block; x++)
            {
                float u = (x + 0.5f - 16f) / 15f, v = (y + 0.5f - Block) / 29f;
                float d = u * u + v * v;
                if (d > 1f) continue;
                float iu = (x + 0.5f - 16f) / 10f, iv = (y + 0.5f - Block) / 22f;
                float inner = iu * iu + iv * iv;
                Color colour = inner <= 1f ? (y >= Block - 3 ? sill : hole)
                    : x < 16 && inner < 1.8f ? rockLit : (x * 7 + y * 3) % 11 == 0 ? rockDark : x >= 22 ? rockDark : rock;
                c.SetRaw(cx + x, cy + y, colour);
            }
        Rim(c, cx, cy, Block, new Color(54, 46, 44, 255));

        // Deep water: a soft-edged patch, its middle the deepest, mottled with paler flecks
        var (wx, wy) = BlockAt(FieldLife.DeepWater);
        for (int y = 0; y < Block; y++)
            for (int x = 0; x < Block; x++)
            {
                int edge = Math.Min(Math.Min(x, Block - 1 - x), Math.Min(y, Block - 1 - y));
                int alpha = edge >= 6 ? 255 : edge >= 3 ? 190 : edge >= 1 ? 110 : 60;
                bool fleck = (x * 13 + y * 7) % 23 == 0 && edge >= 4;
                c.SetRaw(wx + x, wy + y, fleck ? new Color(150, 170, 210, alpha) : new Color(255, 255, 255, alpha));
            }
    }
}
