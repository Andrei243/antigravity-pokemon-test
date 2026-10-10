using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Pixel art for the things of the Gyms' puzzles that stand in the way like people (plan 01 · M9; style guide,
/// "Gyms"), each drawn as a card as the field's obstacles are: the Veilstone Gym's punching bags and stacks of tyres,
/// the Hearthome Gym's bollards and the Snowpoint Gym's snowballs. Outlined like the field's other sprites. Also the
/// Sunyshore Gym's art (its gears, walkways, buttons and doorway, and the face its floors stand on). No GPU calls.
/// </summary>
internal static class GymArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>A punching bag of red leather hanging from its chain, banded at top and foot, 24 by 44.</summary>
    public static void PaintPunchingBag(PixelCanvas c)
    {
        var leather = Tone.Of(206, 62, 58, 234, 104, 92, 150, 38, 48);
        var band = Tone.Of(250, 246, 238, 255, 255, 255, 196, 192, 196);
        var chain = Rgb(150, 154, 170);
        // The chain runs up out of the top of the card
        for (int y = 0; y < 8; y++) c.Set(12, y, y % 2 == 0 ? chain : Rgb(96, 100, 118));
        c.Rect(9, 7, 7, 2, Rgb(80, 84, 100));
        for (int y = 9; y < 42; y++)
        {
            // A bag rounded at its top and foot, a little fuller in the middle
            int half = y < 12 ? 6 + (y - 9) : y > 38 ? 9 - (y - 38) : 9;
            for (int x = 12 - half; x < 12 + half; x++)
            {
                float u = (x + 0.5f - 12f) / half;
                var tone = y is >= 13 and <= 15 || y is >= 34 and <= 36 ? band : leather;
                c.Set(x, y, u < -0.45f ? tone.Light : u > 0.5f ? tone.Dark : tone.Base);
            }
        }
        // Stitches down the middle and a glint on the leather
        for (int y = 17; y < 33; y += 3) c.Set(12, y, leather.Dark);
        c.Rect(6, 18, 2, 8, Rgb(246, 156, 140));
        Pix.Outline(c);
    }

    /// <summary>Three tyres stacked one on another, 30 by 30.</summary>
    public static void PaintTireStack(PixelCanvas c)
    {
        var rubber = Tone.Of(58, 58, 72, 96, 98, 116, 34, 34, 46);
        for (int tyre = 0; tyre < 3; tyre++)
        {
            int top = 20 - tyre * 8;
            for (int y = top; y < top + 9; y++)
                for (int x = 2; x < 28; x++)
                {
                    float u = (x + 0.5f - 15f) / 13f, v = (y + 0.5f - (top + 4.5f)) / 4.5f;
                    if (u * u + v * v * 0.35f > 1f) continue;
                    var col = v < -0.5f ? rubber.Light : v > 0.5f ? rubber.Dark : rubber.Base;
                    // The tread: a dark notch every four texels across its face
                    if (MathF.Abs(v) < 0.5f && x % 4 == 0) col = rubber.Dark;
                    c.Set(x, y, col);
                }
        }
        // The hole of the top tyre, seen from above
        for (int x = 10; x < 20; x++) c.Set(x, 5, Rgb(24, 22, 32));
        for (int x = 8; x < 22; x++) c.Set(x, 6, Rgb(24, 22, 32));
        Pix.Outline(c);
    }

    /// <summary>
    /// A great ball of packed snow, 30 by 30: smooth and round, lit from the upper left (`204,230,255`, then `186,210,244`),
    /// shaded blue toward the lower right (`148,182,232`) and deepest where it sits on the ice (`110,148,218`), with a few
    /// sparkles on its lit side and a soft blue outline. Drawn as a card of the scenery (<see cref="GymPieces"/>), lit as
    /// the ice it stands on, which brings it to about the brightness of the field's snow beside it, below white.
    /// </summary>
    public static void PaintSnowball(PixelCanvas c)
    {
        var light = Rgb(204, 230, 255);
        var snow = Rgb(186, 210, 244);
        var shade = Rgb(148, 182, 232);
        var deep = Rgb(110, 148, 218);
        const float cx = 15f, cy = 15f, r = 13.5f;
        for (int y = 1; y < 30; y++)
            for (int x = 1; x < 29; x++)
            {
                float u = (x + 0.5f - cx) / r, v = (y + 0.5f - cy) / r;
                // Round, its foot flattened where it sits on the ice
                if (u * u + v * v > 1f || y > 28) continue;
                float lit = u * 0.7f + v * 0.9f;
                var col = lit < -0.55f ? light : v > 0.8f || lit > 0.7f ? deep : lit > 0.15f ? shade : snow;
                c.Set(x, y, col);
            }
        // Sparkles on the lit side
        c.Set(9, 9, Rgb(226, 242, 255));
        c.Set(10, 8, Rgb(226, 242, 255));
        c.Set(13, 6, Rgb(226, 242, 255));
        Pix.Outline(c, 0.3f);
    }

    /// <summary>A bollard of dark polished stone with a gilded cap, 16 by 28.</summary>
    public static void PaintBollard(PixelCanvas c)
    {
        var stone = Tone.Of(84, 70, 108, 124, 106, 150, 54, 44, 72);
        var gold = Tone.Of(222, 182, 82, 250, 222, 132, 160, 120, 52);
        for (int y = 6; y < 27; y++)
            for (int x = 3; x < 13; x++)
            {
                float u = (x + 0.5f - 8f) / 5f;
                c.Set(x, y, u < -0.4f ? stone.Light : u > 0.5f ? stone.Dark : stone.Base);
            }
        for (int y = 1; y < 7; y++)
        {
            int half = y < 3 ? 2 + y : 6;
            for (int x = 8 - half; x < 8 + half; x++) c.Set(x, y, x < 7 ? gold.Light : x > 9 ? gold.Dark : gold.Base);
        }
        c.Rect(2, 25, 12, 2, stone.Dark);
        Pix.Outline(c);
    }

    // ------------------------------------------------------------------ the Sunyshore Gym (plan 01 · M9, part 2c)

    private static readonly Color Black = Rgb(40, 40, 48), Hazard = Rgb(236, 196, 60);

    /// <summary>The gold of the original's gears and the steel blue of its "alt" gears (style guide, "Gyms").</summary>
    public static Tone GearTone(bool alt) => alt ? Tone.Of(110, 150, 214, 164, 198, 244, 66, 96, 160) : Tone.Of(228, 184, 58, 250, 220, 118, 164, 122, 36);

    /// <summary>The pale steel of the walkways.</summary>
    public static readonly Tone Deck = Tone.Of(186, 194, 210, 214, 220, 232, 128, 136, 156);

    /// <summary>The dark steel of a gear's boss and an axle.</summary>
    public static readonly Tone Boss = Tone.Of(60, 66, 86, 104, 112, 136, 38, 42, 58);

    /// <summary>
    /// The face the Sunyshore Gym's floors stand on (style guide, "Gyms"), 32 by <see cref="NatureArt.FaceCap"/> +
    /// <see cref="NatureArt.FaceBody"/>: a pale steel edge and a band of hazard stripes in the cap, then panels of dark
    /// steel sixteen rows each, lit along their tops, with a seam and two rivets.
    /// </summary>
    public static PixelCanvas PowerFace()
    {
        var c = new PixelCanvas(32, NatureArt.FaceCap + NatureArt.FaceBody);
        var edge = Rgb(206, 212, 226);
        var steel = Rgb(74, 82, 104);
        var lit = Rgb(100, 108, 132);
        var shade = Rgb(48, 54, 72);
        var rivet = Rgb(140, 148, 168);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < 32; x++)
            {
                Color col;
                if (y == 0) col = edge;
                else if (y < 7) col = (x + y) % 8 < 4 ? Hazard : Black;
                else if (y == 7) col = shade;
                else
                {
                    // A panel every sixteen rows from the cap's foot down, so the body repeats whole
                    int row = (y - NatureArt.FaceCap + 64) % 16;
                    col = row == 0 ? lit : row == 15 ? shade : x == 31 ? shade : x == 0 ? lit : steel;
                    if (row == 3 && x is 3 or 28) col = rivet;
                }
                c.SetRaw(x, y, col);
            }
        return c;
    }

    /// <summary>
    /// A flat gear seen from above, as wide as the canvas: a toothed wheel of its tone, sixteen teeth round a rim, a ring
    /// of shade inside it, six slots between rim and boss, and the face lit toward the upper left; clear outside the teeth.
    /// </summary>
    public static void PaintGearFace(PixelCanvas c, Tone tone)
    {
        float r = c.Width / 2f, root = r - 6f;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r, d = MathF.Sqrt(dx * dx + dy * dy);
                float a = MathF.Atan2(dy, dx);
                // Sixteen teeth, a little narrower at their tips than at their roots
                float tooth = MathF.Cos(a * 16f);
                float edge = tooth > 0.2f ? r - 0.5f : root;
                if (d > edge) continue;
                Color col;
                if (d > root - 1.5f) col = d > root ? tone.Base : tone.Dark;
                else if (d > root - 5f) col = tone.Light;
                else if (d > root - 7f) col = tone.Dark;
                else col = dx + dy < -r * 0.3f ? tone.Light : tone.Base;
                float spoke = MathF.Abs(MathF.Sin(a * 3f));
                if (d < root - 9f && d > 14f && spoke > 0.55f) col = PixelCanvas.Shadow(tone.Dark, 0.35f);
                c.SetRaw(x, y, col);
            }
    }

    /// <summary>
    /// A walkway's deck seen from above, running along the canvas's height (north and south) or its width: pale steel in
    /// plates eight texels long with a dark seam, raised dots as on a tread plate, and a yellow line along each side.
    /// </summary>
    public static void PaintDeck(PixelCanvas c, bool alongHeight)
    {
        int across = alongHeight ? c.Width : c.Height;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                int a = alongHeight ? x : y, along = alongHeight ? y : x;
                Color col;
                if (a < 2 || a >= across - 2) col = a == 0 || a == across - 1 ? Deck.Dark : Hazard;
                else if (along % 8 == 7) col = Deck.Dark;
                else if (along % 8 == 0) col = Deck.Light;
                else col = (a + along * 3) % 6 == 0 ? Deck.Light : Deck.Base;
                c.SetRaw(x, y, col);
            }
    }

    /// <summary>The side of a walkway or a bar: a band of hazard stripes along its top edge over dark steel.</summary>
    public static void PaintDeckSide(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                c.SetRaw(x, y, y == 0 ? Deck.Light : y == c.Height - 1 ? Boss.Dark : y < 3 ? ((x + y) % 8 < 4 ? Hazard : Black) : Boss.Base);
    }

    /// <summary>Hazard stripes over the whole face, yellow and black on the slant, eight texels to a pair, with a dark rim.</summary>
    public static void PaintHazard(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                c.SetRaw(x, y, x == 0 || y == 0 || x == c.Width - 1 || y == c.Height - 1 ? Boss.Dark : (x + y) % 8 < 4 ? Hazard : Black);
    }

    /// <summary>The boss on a gear's hub, seen from above: a dark steel disc with a light rim and four bolts.</summary>
    public static void PaintBoss(PixelCanvas c)
    {
        float r = c.Width / 2f;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r, d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > r - 0.3f) continue;
                var col = d > r - 2f ? Boss.Light : dx + dy < -r * 0.4f ? Boss.Light : Boss.Base;
                bool bolt = MathF.Abs(MathF.Abs(dx) - r * 0.5f) < 1f && MathF.Abs(dy) < 1f || MathF.Abs(MathF.Abs(dy) - r * 0.5f) < 1f && MathF.Abs(dx) < 1f;
                c.SetRaw(x, y, bolt ? Boss.Dark : col);
            }
    }

    /// <summary>
    /// A button on a gear's hub, seen from above: a steel rim and a cap of yellow with a black arrow round it, turning
    /// the way the gear under it turns (<paramref name="sense"/> 1 counter-clockwise, -1 clockwise), or two arrows for a
    /// half turn (<paramref name="half"/>); clear outside the rim.
    /// </summary>
    public static void PaintButton(PixelCanvas c, int sense, bool half)
    {
        var rim = Tone.Of(176, 184, 196, 214, 220, 228, 120, 128, 144);
        var cap = Tone.Of(248, 214, 72, 255, 236, 140, 196, 160, 40);
        float r = c.Width / 2f;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r, d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > r - 0.3f) continue;
                var col = d > r - 3f ? (dx + dy < 0 ? rim.Light : rim.Dark) : d > r - 4f ? cap.Dark : dx + dy < -r * 0.5f ? cap.Light : cap.Base;
                c.SetRaw(x, y, col);
            }
        // The arrow: a ring two texels thick round the middle, its head at the end it turns to
        float ring = r * 0.5f;
        int arrows = half ? 2 : 1;
        for (int k = 0; k < arrows; k++)
        {
            float start = k * MathF.PI + 0.3f;
            float span = half ? MathF.PI * 0.55f : MathF.PI * 1.35f;
            // Seen from above with north up, the canvas's y runs south: counter-clockwise is a falling angle here
            for (float t = 0f; t <= span; t += 0.02f)
            {
                float a = start - sense * t;
                for (float w = -1f; w <= 1f; w += 0.5f)
                {
                    int px = (int)(r + MathF.Cos(a) * (ring + w)), py = (int)(r + MathF.Sin(a) * (ring + w));
                    if (px >= 0 && py >= 0 && px < c.Width && py < c.Height) c.SetRaw(px, py, Black);
                }
            }
            // The head: a triangle across the ring at its end, pointing on along it
            float end = start - sense * span;
            var tip = new Vector2(r + MathF.Cos(end - sense * 0.5f) * ring, r + MathF.Sin(end - sense * 0.5f) * ring);
            var outer = new Vector2(r + MathF.Cos(end) * (ring + 4f), r + MathF.Sin(end) * (ring + 4f));
            var inner = new Vector2(r + MathF.Cos(end) * (ring - 4f), r + MathF.Sin(end) * (ring - 4f));
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                    if (Inside(new Vector2(x + 0.5f, y + 0.5f), tip, outer, inner)) c.SetRaw(x, y, Black);
        }
    }

    private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        static float Cross(Vector2 o, Vector2 u, Vector2 v) => (u.X - o.X) * (v.Y - o.Y) - (u.Y - o.Y) * (v.X - o.X);
        float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
        bool negative = d1 < 0 || d2 < 0 || d3 < 0, positive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(negative && positive);
    }

    /// <summary>A steel door on to the next room, 28 by 50: two leaves meeting in the middle in a yellow frame, a stripe across them.</summary>
    public static void PaintPowerDoor(PixelCanvas c)
    {
        var steel = Tone.Of(120, 128, 150, 156, 164, 184, 82, 88, 108);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                bool frame = x < 3 || x >= c.Width - 3 || y < 3;
                Color col = frame ? (x == 0 || y == 0 ? Hazard : PixelCanvas.Shadow(Hazard, 0.18f))
                    : x == c.Width / 2 || x == c.Width / 2 - 1 ? steel.Dark
                    : y is >= 22 and < 26 ? ((x + y) % 6 < 3 ? Hazard : Black)
                    : x == 3 || y == 3 ? steel.Light : steel.Base;
                c.SetRaw(x, y, col);
            }
    }
}
