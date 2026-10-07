using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The evolution scene's stage and lights, and the list a new move is weighed against.</summary>
internal static partial class ModernUi
{
    private static readonly Color EvolutionLight = new(150, 190, 255, 255);

    private static void SoftLight(Vector2 c, float w, float h, Color color, float degrees = 0f)
    {
        var glow = SceneTextures.SoftGlow;
        Raylib.DrawTexturePro(glow, new Rectangle(0, 0, glow.Width, glow.Height), new Rectangle(c.X, c.Y, w, h),
            new Vector2(w / 2f, h / 2f), degrees, color);
    }

    private static Color Lit(Color c, float alpha) => c with { A = (byte)Math.Clamp(alpha * 255f, 0f, 255f) };

    /// <summary>
    /// A dark hall with a pool of light on its floor and, behind the Pokémon at <paramref name="c"/>, a glow whose
    /// rays turn slowly. <paramref name="glow"/> (0 to 1) is how strongly the light has gathered.
    /// </summary>
    public static void EvolutionStage(int sw, int sh, Vector2 c, float time, float glow)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, new Color(8, 10, 30, 255), new Color(22, 30, 78, 255));

        Raylib.BeginBlendMode(BlendMode.Additive);
        // The floor: a wide, flat pool of light under the feet
        SoftLight(c + new Vector2(0, 300), 1500, 240, Lit(EvolutionLight, 0.22f + 0.3f * glow));
        SoftLight(c + new Vector2(0, 300), 760, 110, Lit(Color.White, 0.2f + 0.4f * glow));

        // Rays fanning out from behind the Pokémon, alternately long and short
        const int rays = 16;
        for (int i = 0; i < rays; i++)
        {
            float angle = i * 360f / rays + time * 9f;
            float length = (i % 2 == 0 ? 1500f : 1050f) * (0.55f + 0.45f * glow);
            SoftLight(c, length, 90f, Lit(EvolutionLight, 0.26f * glow), angle);
        }

        // The glow itself: wide and blue, then tight and white
        float pulse = 1f + 0.05f * MathF.Sin(time * 5f);
        SoftLight(c, 1700 * pulse, 1700 * pulse, Lit(new Color(70, 110, 255, 255), 0.5f * glow));
        SoftLight(c, 980 * pulse, 980 * pulse, Lit(EvolutionLight, 0.7f * glow));
        SoftLight(c, 520 * pulse, 520 * pulse, Lit(Color.White, 0.8f * glow));
        Raylib.EndBlendMode();
    }

    /// <summary>
    /// Motes of light round the Pokémon: <paramref name="gather"/> (0 to 1) draws them in toward it as the
    /// evolution builds, <paramref name="scatter"/> (0 to 1) throws a ring of sparks outward once it is done.
    /// </summary>
    public static void EvolutionMotes(Vector2 c, float time, float gather, float scatter)
    {
        Raylib.BeginBlendMode(BlendMode.Additive);
        if (gather > 0.01f)
        {
            for (int i = 0; i < 40; i++)
            {
                float cycle = (time * 0.75f + i * 0.618f) % 1f;
                float angle = i * 2.399f + time * 0.5f;
                float r = 560f - 520f * cycle * cycle;
                var p = c + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r * 0.82f);
                float size = 22f + (i % 4) * 9f;
                SoftLight(p, size, size, Lit(i % 3 == 0 ? Color.White : EvolutionLight, gather * MathF.Sin(cycle * MathF.PI)));
            }
        }
        if (scatter > 0f && scatter < 1f)
        {
            float reach = UiMotion.EaseOut(scatter);
            for (int i = 0; i < 30; i++)
            {
                float angle = i * 2.399f;
                float r = 150f + (430f + (i % 5) * 70f) * reach;
                var p = c + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r * 0.85f);
                float alpha = 1f - scatter;
                float size = 30f + (i % 3) * 12f;
                // A four-pointed glint: two thin streaks crossed over a dot
                SoftLight(p, size * 3.2f, size * 0.42f, Lit(Color.White, alpha));
                SoftLight(p, size * 0.42f, size * 3.2f, Lit(Color.White, alpha));
                SoftLight(p, size, size, Lit(new Color(255, 240, 190, 255), alpha));
            }
        }
        Raylib.EndBlendMode();
    }

    /// <summary>The light a Pokémon gives off while it is white (<paramref name="white"/> 0 to 1), spilling over its edges.</summary>
    public static void EvolutionAura(Vector2 c, float size, float white)
    {
        if (white <= 0.01f || size < 1f) return;
        Raylib.BeginBlendMode(BlendMode.Additive);
        SoftLight(c, size * 1.5f, size * 1.5f, Lit(EvolutionLight, 0.3f * white));
        SoftLight(c, size * 0.9f, size * 0.9f, Lit(Color.White, 0.18f * white));
        Raylib.EndBlendMode();
    }

    /// <summary>A rendered Pokémon (see <c>PokemonSprites.Render</c>) centred on <paramref name="c"/>, <paramref name="size"/> units across.</summary>
    public static void EvolutionFigure(Texture2D picture, Vector2 c, float size)
    {
        if (size < 1f) return;
        Raylib.DrawTexturePro(picture, new Rectangle(0, 0, picture.Width, -picture.Height),
            new Rectangle(c.X - size / 2f, c.Y - size / 2f, size, size), Vector2.Zero, 0f, Color.White);
    }

    /// <summary>
    /// The choice when a Pokémon that knows four moves wants a fifth: the new move on top, its four moves under
    /// it, and a last row that keeps them all. <paramref name="cursor"/> 0 to 3 is a move to forget, 4 the last row.
    /// </summary>
    public static void MoveChoice(Rectangle r, Pokemon p, string newMove, int cursor, float appear)
    {
        r.X += (1f - UiMotion.EaseOut(appear)) * 80f;
        Panel(r, 34);
        float x = r.X + 28, w = r.Width - 56;

        Label("WANTS TO LEARN", r.X + 44, r.Y + 30);
        MoveRow(new Rectangle(x, r.Y + 64, w, 86), MoveDatabase.Get(newMove), null, false, Gold);

        Label("FORGET WHICH MOVE?", r.X + 44, r.Y + 178);
        for (int i = 0; i < p.Moves.Count && i < 4; i++)
            MoveRow(new Rectangle(x, r.Y + 212 + i * 96, w, 86), p.Moves[i].Data, p.Moves[i].CurrentPP, cursor == i, Selection, p.Moves[i].MaxPP);

        var keep = new Rectangle(x, r.Y + 212 + 4 * 96 + 14, w, 78);
        Button(keep, 39, Blue, $"Don't learn {newMove}", 30, cursor == 4);
    }

    /// <summary>
    /// The moves of a Pokémon to use an item on (an Ether, a PP Up; plan 06 · R11): its four moves with their PP,
    /// the one under <paramref name="cursor"/> picked out.
    /// </summary>
    public static void MovePick(Rectangle r, Pokemon p, int cursor, string title)
    {
        Panel(r, 34);
        float x = r.X + 28, w = r.Width - 56;
        Label(title.ToUpperInvariant(), r.X + 44, r.Y + 30);
        for (int i = 0; i < p.Moves.Count && i < 4; i++)
        {
            var m = p.Moves[i];
            MoveRow(new Rectangle(x, r.Y + 72 + i * 96, w, 86), m.Data, m.CurrentPP, cursor == i, Selection, m.MaxPP);
        }
    }

    private static void MoveRow(Rectangle row, MoveData move, int? pp, bool selected, Color accent, int? maxPP = null)
    {
        if (selected) UiShapes.Shadow(row, 24, 22, Vector2.Zero, accent with { A = 170 });
        bool framed = selected || pp == null;
        UiShapes.Shape(row, 24, new Color(248, 250, 253, 255), new Color(236, 241, 248, 255), framed ? accent : Rule, framed ? 5 : 3);
        float cy = row.Y + row.Height / 2f;
        TypePill(row.X + 16, cy - 22, move.Type, 44, 150);
        UiFonts.DrawCentered(move.Name, row.X + 186, cy - 12, 30, Ink, UiWeight.Black);
        string detail = move.Category.ToString().ToUpperInvariant() + (move.Power > 0 ? $"  ·  POWER {move.Power}" : "") +
                        (move.Accuracy > 0 ? $"  ·  ACC {move.Accuracy}" : "");
        UiFonts.DrawCentered(detail, row.X + 186, cy + 22, 18, Muted, UiWeight.Black);
        int most = maxPP ?? move.MaxPP;
        string points = pp is { } left ? $"{left} / {most}" : $"PP {most}";
        UiFonts.DrawCentered(points, row.X + row.Width - 28 - UiFonts.Measure(points, 28, UiWeight.Black), cy, 28, Ink, UiWeight.Black);
    }
}
