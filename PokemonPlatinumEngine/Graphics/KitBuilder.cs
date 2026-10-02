using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Builds field geometry on the pixel grid: boxes and upright cards measured in texels (1/32 of a tile across,
/// 1/32 of a screen row upward), each visible face showing a piece of art painted for it at exactly that size
/// in the map's <see cref="ArtSheet"/>. Texture coordinates are kept in texels until <see cref="Finish"/>.
/// No GPU calls, so tests can build with it.
/// </summary>
internal sealed class KitBuilder
{
    /// <summary>One texel in world units.</summary>
    public const float Texel = 1f / GroundBaker.ArtTile;

    /// <summary>
    /// South faces are lit as if they leaned back a little: they are the face of every building and piece of
    /// furniture, and a true vertical wall would sit in half shade under the high sun.
    /// </summary>
    public static readonly Vector3 FrontNormal = Vector3.Normalize(new Vector3(0f, 0.3f, 1f));

    /// <summary>Upright sprites take light like the ground they stand on.</summary>
    public static readonly Vector3 CardNormal = Vector3.Normalize(new Vector3(0f, 0.8f, 0.6f));

    public ArtSheet Sheet { get; }

    /// <summary>World height of one screen row (see <see cref="MapScene.VS"/>).</summary>
    public float VS { get; }

    /// <summary>Geometry that is lit and casts shadows.</summary>
    public MeshBuilder Solid { get; } = new();

    /// <summary>Things lying on the ground (rugs, mats): they take shadows and cast none.</summary>
    public MeshBuilder Flat { get; } = new();

    /// <summary>Where texel (0, 0, 0) is in the world: x and z in tiles.</summary>
    public Vector3 Origin { get; set; }

    public KitBuilder(ArtSheet sheet, float vs)
    {
        Sheet = sheet;
        VS = vs;
    }

    public Art Face(string key, int width, int height, Action<PixelCanvas> paint) => Sheet.Paint(key, width, height, paint);

    /// <summary>A point given in texels from the origin: x east, y up (in screen rows), z south.</summary>
    public Vector3 At(float x, float y, float z) => Origin + new Vector3(x * Texel, y * Texel * VS, z * Texel);

    /// <summary>A quad showing <paramref name="art"/>, its corners named as seen from its front.</summary>
    public void Quad(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topRight, Vector3 topLeft, Art art, Vector3 normal, MeshBuilder? into = null)
    {
        (into ?? Solid).Quad(bottomLeft, bottomRight, topRight, topLeft,
            new(art.X, art.Y + art.Height), new(art.X + art.Width, art.Y + art.Height), new(art.X + art.Width, art.Y), new(art.X, art.Y),
            Color.White, normal);
    }

    /// <summary>A triangle of <paramref name="art"/>: each corner names its place in the art in texels from the top left.</summary>
    public void Tri(Vector3 a, Vector3 b, Vector3 c, Art art, Vector2 ta, Vector2 tb, Vector2 tc, Vector3 normal, MeshBuilder? into = null)
    {
        var o = new Vector2(art.X, art.Y);
        (into ?? Solid).Tri(a, b, c, o + ta, o + tb, o + tc, normal, normal, normal, Color.White, Color.White, Color.White);
    }

    /// <summary>
    /// A box from (x0, y0, z0) to (x1, y1, z1) in texels. Each face given is drawn with its art as seen from
    /// outside: the top's lower edge is its south side, the west face's right edge and the east face's left edge
    /// are their south ends.
    /// </summary>
    public void Box(float x0, float x1, float z0, float z1, float y0, float y1,
        Art? top = null, Art? south = null, Art? west = null, Art? east = null, Art? north = null)
    {
        if (top is { } t) Quad(At(x0, y1, z1), At(x1, y1, z1), At(x1, y1, z0), At(x0, y1, z0), t, Vector3.UnitY);
        if (south is { } s) Quad(At(x0, y0, z1), At(x1, y0, z1), At(x1, y1, z1), At(x0, y1, z1), s, FrontNormal);
        if (west is { } w) Quad(At(x0, y0, z0), At(x0, y0, z1), At(x0, y1, z1), At(x0, y1, z0), w, -Vector3.UnitX);
        if (east is { } e) Quad(At(x1, y0, z1), At(x1, y0, z0), At(x1, y1, z0), At(x1, y1, z1), e, Vector3.UnitX);
        if (north is { } n) Quad(At(x1, y0, z0), At(x0, y0, z0), At(x0, y1, z0), At(x1, y1, z0), n, -Vector3.UnitZ);
    }

    /// <summary>
    /// A box in one material, each face a bevelled rectangle: the top with a light line where it meets the front
    /// and the left side, the upright faces light at the top and left and dark at the foot and right.
    /// </summary>
    public void Block(string material, Tone tone, float x0, float x1, float z0, float z1, float y0, float y1, bool sides = true)
    {
        int w = Size(x1 - x0), d = Size(z1 - z0), h = Size(y1 - y0);
        var top = Face($"{material}.top.{w}x{d}", w, d, c =>
        {
            c.Rect(0, 0, w, d, tone.Base);
            c.HLine(0, d - 1, w, tone.Light);
            c.VLine(0, 0, d, tone.Light);
        });
        var front = Face($"{material}.front.{w}x{h}", w, h, c => Pix.Raised(c, 0, 0, w, h, tone));
        Art? side = sides ? Face($"{material}.side.{d}x{h}", d, h, c => Pix.Raised(c, 0, 0, d, h, tone)) : null;
        Box(x0, x1, z0, z1, y0, y1, top, front, side, side);
    }

    private static int Size(float texels) => Math.Max(1, (int)MathF.Round(texels));

    /// <summary>An upright card facing the camera at depth <paramref name="z"/>: a sprite standing in the scene.</summary>
    public void Card(float x0, float x1, float z, float y0, float y1, Art art) =>
        Quad(At(x0, y0, z), At(x1, y0, z), At(x1, y1, z), At(x0, y1, z), art, CardNormal);

    /// <summary>A sprite at its own size, centred on <paramref name="centerX"/> and standing on <paramref name="y0"/>.</summary>
    public void Sprite(Art art, float centerX, float z, float y0 = 0f)
    {
        float x0 = MathF.Round(centerX - art.Width / 2f);
        Card(x0, x0 + art.Width, z, y0, y0 + art.Height, art);
    }

    /// <summary>Art lying flat just above the ground (<paramref name="lift"/> in world units).</summary>
    public void Decal(float x0, float x1, float z0, float z1, float lift, Art art)
    {
        Vector3 L(float x, float z) => Origin + new Vector3(x * Texel, lift, z * Texel);
        Quad(L(x0, z1), L(x1, z1), L(x1, z0), L(x0, z0), art, Vector3.UnitY, Flat);
    }

    /// <summary>Turns the texel coordinates into texture coordinates, once the sheet's size is final.</summary>
    public void Finish()
    {
        float su = 1f / ArtSheet.Width, sv = 1f / Sheet.Height;
        Solid.ScaleUVs(su, sv);
        Flat.ScaleUVs(su, sv);
    }
}
