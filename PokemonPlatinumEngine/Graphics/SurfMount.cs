using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The Pokémon that carries the player across water, as the field shows it: one round, dark-blue swimmer for
/// whichever Pokémon knows Surf, as the original has one for them all. Pixel art painted here (no GPU calls in
/// <see cref="Paint"/>) and stood in the field the way a character's sprite is, under the rider.
/// </summary>
internal static class SurfMount
{
    public const int Width = 48, Height = 26;

    /// <summary>How many rows of the rider's sprite their feet are lifted to sit on its back.</summary>
    public const int Seat = 12;

    private static readonly Color Body = new(86, 128, 206, 255);
    private static readonly Color Back = new(138, 180, 240, 255);
    private static readonly Color Under = new(58, 88, 160, 255);
    private static readonly Color Eye = new(250, 250, 252, 255);
    private static readonly Color Pupil = new(40, 44, 84, 255);

    private static readonly CharacterSprites.Card?[] Cards = new CharacterSprites.Card?[4];

    /// <summary>The swimmer seen from one side: 0 coming toward the camera, 1 heading right, 2 going away, 3 heading left.</summary>
    public static PixelCanvas Paint(int facing)
    {
        var c = new PixelCanvas(Width, Height);
        if (facing is 1 or 3)
        {
            // From the side: a long round body, the head at the front with one eye, a fin on the back and a tail behind
            c.FlatTri(3, 15, 11, 9, 12, 21, Under);
            c.FlatTri(15, 8, 22, 2, 26, 8, Body);
            c.FlatEllipse(23, 16, 16, 10, Body);
            c.FlatEllipse(36, 15, 8, 8.5f, Body);
            c.FlatEllipse(23, 11.5f, 12, 4, Back);
            c.FlatEllipse(37, 11, 5, 3, Back);
            c.Rect(9, 22, 30, 3, Under);
            c.Rect(38, 14, 4, 4, Eye);
            c.Rect(40, 14, 2, 3, Pupil);
            c.HLine(39, 21, 4, Pupil);
            Pix.Outline(c);
            if (facing == 3) c.MirrorHorizontal();
            return c;
        }

        // From the front or the back: a round body low in the water, lighter along its back
        c.FlatEllipse(24, 15, 18, 11, Body);
        c.FlatEllipse(24, 10, 13, 5, Back);
        c.Rect(9, 22, 30, 3, Under);
        if (facing == 0)
        {
            // Two eyes set wide, and a small mouth
            c.Rect(12, 15, 5, 4, Eye);
            c.Rect(31, 15, 5, 4, Eye);
            c.Rect(15, 15, 2, 3, Pupil);
            c.Rect(31, 15, 2, 3, Pupil);
            c.HLine(21, 21, 6, Pupil);
        }
        else
        {
            // Going away: its tail fin shows at the waterline
            c.FlatTri(18, 18, 24, 26, 30, 18, Under);
            c.HLine(20, 18, 8, Back);
        }
        Pix.Outline(c);
        return c;
    }

    /// <summary>Stands the swimmer on the water at a point, facing the way its rider does.</summary>
    public static void Draw(RenderContext context, Vector3 at, float yaw, float vs, CharacterPass pass)
    {
        int facing = CharacterSprites.FacingIndex(yaw);
        var card = Cards[facing] ??= CharacterSprites.MakeCard(context, Paint(facing));
        CharacterSprites.DrawCard(card, at, vs, pass);
    }
}
