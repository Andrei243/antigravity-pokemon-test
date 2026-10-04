using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>How one scene gives way to the next.</summary>
public enum TransitionKind
{
    /// <summary>A plain fade through the dark: doors, menus, the end of a battle.</summary>
    Fade,
    /// <summary>Into a battle with a wild Pokémon: two flashes, a pinwheel closing, an iris opening.</summary>
    Wild,
    /// <summary>A wild Pokémon of a higher level than the player's first: the picture breaks into shards.</summary>
    WildStrong,
    /// <summary>Into a battle with a trainer: two flashes, shutters from both sides.</summary>
    Trainer,
    /// <summary>A trainer whose first Pokémon is of a higher level: shutters from the sides and from above and below.</summary>
    TrainerStrong,
    /// <summary>Into a battle with a Gym Leader: two flashes, diamonds growing from a grid.</summary>
    Leader
}

/// <summary>
/// The shapes that close over one scene and open on the next (style guide, "Into battle"). What is covered at
/// any moment is a function of the kind, the phase and the point (<see cref="Covers"/>), which is what tests
/// check; <see cref="Draw"/> draws the same shapes.
/// </summary>
public static class SceneTransition
{
    /// <summary>The dark a scene closes to: never pure black.</summary>
    public static readonly Color Dark = new(14, 14, 22, 255);

    public const float FadeSeconds = 0.4f, FlashSeconds = 0.25f, CloseSeconds = 0.9f, OpenSeconds = 0.5f;
    private const int Blades = 8, Bars = 12, Uprights = 16, Columns = 8, Rows = 5;

    // The shards: a grid of points pulled out of line, two triangles to each of its squares
    private const int ShardColumns = 10, ShardRows = 6;
    private const float ShardGrows = 0.3f;

    /// <summary>
    /// The way into a battle, as Platinum picks it: by who is met, and by whether the first Pokémon they send
    /// out is of a higher level than the player's first.
    /// </summary>
    public static TransitionKind ForBattle(bool trainer, bool leader, int theirLevel, int ownLevel)
    {
        if (leader) return TransitionKind.Leader;
        bool stronger = theirLevel > ownLevel;
        if (trainer) return stronger ? TransitionKind.TrainerStrong : TransitionKind.Trainer;
        return stronger ? TransitionKind.WildStrong : TransitionKind.Wild;
    }

    /// <summary>How long a kind takes to close over the scene it leaves.</summary>
    public static float OutSeconds(TransitionKind kind) => kind == TransitionKind.Fade ? FadeSeconds : CloseSeconds;

    /// <summary>How long it takes to open on the scene it brings.</summary>
    public static float InSeconds(TransitionKind kind) => kind == TransitionKind.Fade ? FadeSeconds : OpenSeconds;

    /// <summary>
    /// How white the screen is <paramref name="seconds"/> into closing: two flashes in the first quarter of a
    /// second, each lit for half of its eighth. Never more than three quarters white.
    /// </summary>
    public static float Flash(TransitionKind kind, float seconds)
    {
        if (kind == TransitionKind.Fade || seconds < 0f || seconds >= FlashSeconds) return 0f;
        float beat = seconds / (FlashSeconds / 2f);
        return beat - MathF.Floor(beat) < 0.5f ? 0.75f : 0f;
    }

    /// <summary>How far the shapes have closed, 0 to 1, <paramref name="seconds"/> into closing or opening.</summary>
    public static float Closed(TransitionKind kind, bool closing, float seconds)
    {
        if (kind == TransitionKind.Fade) return Math.Clamp(closing ? seconds / FadeSeconds : 1f - seconds / FadeSeconds, 0f, 1f);
        if (!closing) return Math.Clamp(1f - seconds / OpenSeconds, 0f, 1f);
        // The shapes wait for the flashes, then close with an ease
        float t = Math.Clamp((seconds - FlashSeconds) / (CloseSeconds - FlashSeconds), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static bool OpensAsIris(TransitionKind kind) => kind is TransitionKind.Wild or TransitionKind.WildStrong;

    private static float Noise(int a, int b, int salt)
    {
        uint h = (uint)(a * 374761393 + b * 668265263 + salt * 2147483647);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFF) / 65536f;
    }

    /// <summary>A point of the shards' grid: on the screen's edge it stays put, inside it is pulled up to a third of a square out of line.</summary>
    private static Vector2 ShardPoint(int col, int row, float width, float height)
    {
        float cw = width / ShardColumns, ch = height / ShardRows;
        float x = col * cw, y = row * ch;
        if (col > 0 && col < ShardColumns) x += (Noise(col, row, 1) - 0.5f) * cw * 0.66f;
        if (row > 0 && row < ShardRows) y += (Noise(col, row, 2) - 0.5f) * ch * 0.66f;
        return new Vector2(x, y);
    }

    /// <summary>One of the two shards of a square of the grid, and how far it has grown from its middle (0 to 1).</summary>
    private static (Vector2 A, Vector2 B, Vector2 C, float Grown) Shard(int col, int row, int half, float closed, float width, float height)
    {
        var nw = ShardPoint(col, row, width, height);
        var ne = ShardPoint(col + 1, row, width, height);
        var sw = ShardPoint(col, row + 1, width, height);
        var se = ShardPoint(col + 1, row + 1, width, height);
        // Squares are cut along one diagonal or the other in turn
        bool falling = (col + row) % 2 == 0;
        var (a, b, c) = falling
            ? (half == 0 ? (nw, sw, se) : (nw, se, ne))
            : (half == 0 ? (nw, sw, ne) : (ne, sw, se));

        // The nearer the middle of the screen, the sooner a shard starts; none starts in step with its neighbour
        var centre = (a + b + c) / 3f;
        var middle = new Vector2(width / 2f, height / 2f);
        float far = Math.Clamp((centre - middle).Length() / middle.Length(), 0f, 1f);
        float start = (1f - ShardGrows) * Math.Clamp(0.8f * far + 0.2f * Noise(col * 2 + half, row, 3), 0f, 1f);
        float grown = Math.Clamp((closed - start) / ShardGrows, 0f, 1f);
        return (centre + (a - centre) * grown, centre + (b - centre) * grown, centre + (c - centre) * grown, grown);
    }

    private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.X - b.X) * (a.Y - b.Y) - (a.X - b.X) * (p.Y - b.Y);
        float d2 = (p.X - c.X) * (b.Y - c.Y) - (b.X - c.X) * (p.Y - c.Y);
        float d3 = (p.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (p.Y - a.Y);
        bool negative = d1 < 0f || d2 < 0f || d3 < 0f, positive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(negative && positive);
    }

    /// <summary>Whether a point of a screen of the given size is under the dark, the shapes being <paramref name="closed"/> this far.</summary>
    public static bool Covers(TransitionKind kind, bool closing, float closed, float x, float y, float width, float height)
    {
        if (closed <= 0f) return false;
        if (closed >= 1f) return true;
        float cx = width / 2f, cy = height / 2f;
        if (!closing && OpensAsIris(kind))
        {
            // An iris: open from the middle outward
            float reach = MathF.Sqrt(cx * cx + cy * cy);
            float dx = x - cx, dy = y - cy;
            return MathF.Sqrt(dx * dx + dy * dy) > (1f - closed) * reach;
        }

        switch (kind)
        {
            case TransitionKind.Wild:
            {
                // Eight blades turning a quarter of a turn as they widen
                float slice = MathF.Tau / Blades;
                float angle = MathF.Atan2(y - cy, x - cx) - closed * MathF.PI / 2f;
                float within = ((angle % slice) + slice) % slice;
                return within < closed * slice;
            }
            case TransitionKind.WildStrong:
            {
                var p = new Vector2(x, y);
                for (int row = 0; row < ShardRows; row++)
                    for (int col = 0; col < ShardColumns; col++)
                        for (int half = 0; half < 2; half++)
                        {
                            var (a, b, c, grown) = Shard(col, row, half, closed, width, height);
                            if (grown > 0f && Inside(p, a, b, c)) return true;
                        }
                return false;
            }
            case TransitionKind.Trainer or TransitionKind.TrainerStrong:
            {
                int bar = Math.Clamp((int)(y / (height / Bars)), 0, Bars - 1);
                if (bar % 2 == 0 ? x < closed * width : x > (1f - closed) * width) return true;
                if (kind == TransitionKind.Trainer) return false;
                int upright = Math.Clamp((int)(x / (width / Uprights)), 0, Uprights - 1);
                return upright % 2 == 0 ? y < closed * height : y > (1f - closed) * height;
            }
            case TransitionKind.Leader:
            {
                float cw = width / Columns, ch = height / Rows;
                float dx = MathF.Abs(x % cw - cw / 2f) / (cw / 2f), dy = MathF.Abs(y % ch - ch / 2f) / (ch / 2f);
                return dx + dy < closed * 2.05f;
            }
            default:
                return false;
        }
    }

    /// <summary>Draws the transition over a screen of the given size, <paramref name="seconds"/> into its closing or opening half.</summary>
    public static void Draw(TransitionKind kind, bool closing, float seconds, int width, int height)
    {
        float closed = Closed(kind, closing, seconds);
        if (kind == TransitionKind.Fade)
        {
            Raylib.DrawRectangle(0, 0, width, height, new Color(0, 0, 0, (int)(closed * 255)));
            return;
        }

        if (closing && Flash(kind, seconds) is > 0f and var white)
            Raylib.DrawRectangle(0, 0, width, height, new Color(255, 255, 255, (int)(white * 255)));
        if (closed <= 0f) return;
        if (closed >= 1f)
        {
            Raylib.DrawRectangle(0, 0, width, height, Dark);
            return;
        }

        var middle = new Vector2(width / 2f, height / 2f);
        float reach = middle.Length() + 4f;
        if (!closing && OpensAsIris(kind))
        {
            Raylib.DrawRing(middle, (1f - closed) * reach, reach + 8f, 0f, 360f, 64, Dark);
            return;
        }

        switch (kind)
        {
            case TransitionKind.Wild:
            {
                float slice = 360f / Blades, turned = closed * 90f;
                for (int i = 0; i < Blades; i++)
                    Raylib.DrawCircleSector(middle, reach, turned + i * slice, turned + i * slice + closed * slice, 12, Dark);
                break;
            }
            case TransitionKind.WildStrong:
            {
                for (int row = 0; row < ShardRows; row++)
                    for (int col = 0; col < ShardColumns; col++)
                        for (int half = 0; half < 2; half++)
                        {
                            var (a, b, c, grown) = Shard(col, row, half, closed, width, height);
                            if (grown <= 0f) continue;
                            // The order raylib wants: counter-clockwise as the screen shows it
                            float turn = (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
                            if (turn < 0f) Raylib.DrawTriangle(a, b, c, Dark);
                            else Raylib.DrawTriangle(a, c, b, Dark);
                        }
                break;
            }
            case TransitionKind.Trainer or TransitionKind.TrainerStrong:
            {
                float bar = height / (float)Bars, w = closed * width;
                for (int i = 0; i < Bars; i++)
                    Raylib.DrawRectangleRec(new Rectangle(i % 2 == 0 ? 0f : width - w, i * bar, w, bar + 1f), Dark);
                if (kind == TransitionKind.Trainer) break;
                float upright = width / (float)Uprights, h = closed * height;
                for (int i = 0; i < Uprights; i++)
                    Raylib.DrawRectangleRec(new Rectangle(i * upright, i % 2 == 0 ? 0f : height - h, upright + 1f, h), Dark);
                break;
            }
            case TransitionKind.Leader:
            {
                float cw = width / (float)Columns, ch = height / (float)Rows;
                for (int row = 0; row < Rows; row++)
                    for (int col = 0; col < Columns; col++)
                    {
                        // A diamond as wide and tall as the same share of its cell
                        float rx = closed * 2.05f * cw / 2f, ry = closed * 2.05f * ch / 2f;
                        var c = new Vector2((col + 0.5f) * cw, (row + 0.5f) * ch);
                        Raylib.DrawTriangle(new Vector2(c.X, c.Y - ry), new Vector2(c.X - rx, c.Y), new Vector2(c.X + rx, c.Y), Dark);
                        Raylib.DrawTriangle(new Vector2(c.X - rx, c.Y), new Vector2(c.X, c.Y + ry), new Vector2(c.X + rx, c.Y), Dark);
                    }
                break;
            }
        }
    }
}
