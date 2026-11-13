using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The looks of Unova's special tiles (plan 38 · EX11; style guide, "Life", "Unova's special tiles"), made of the field's
/// own pieces: shaking grass is plan 10 · F12's rustle (<see cref="Rustle"/> and the grass parted by
/// <see cref="WorldRenderer.Stirring"/>), rippling water is rings spreading in turn, a dust cloud is a heap of puffs of the
/// floor's dust left standing on its tile, and a bridge's shadow a dark smudge on the planks. GPU-free.
/// </summary>
internal sealed partial class FieldLife
{
    /// <summary>How often a patch stirs, in seconds of the field's clock (the DexNav's rustle's beat).</summary>
    public const float SpecialEvery = HidingEvery;

    private static readonly Color CloudTint = Rgb(204, 190, 160), ShadowTint = Rgb(40, 44, 60);

    /// <summary>A patch of water ripples: two rings one after the other, wide and slow, and no drops.</summary>
    public void WaterRipple(Map map, int x, int y)
    {
        if (!map.InBounds(x, y) || map.IsIndoors) return;
        float cx = x + 0.5f, cz = y + 0.5f, ground = Relief.At(map, cx, cz);
        for (int i = 0; i < 2; i++)
            pieces.Add(new Piece(Kind.Ring, new Vector3(cx, ground + 0.02f, cz), Now + i * 0.3, RingTint, 0, 0f, 1.5f));
    }

    /// <summary>A dust cloud stands on a tile of a cave's floor: three puffs billowing up from it and over its sides.</summary>
    public void Dust(Map map, int x, int y)
    {
        if (!map.InBounds(x, y) || map.IsIndoors) return;
        float cx = x + 0.5f, cz = y + 0.5f, ground = Relief.At(map, cx, cz);
        foreach (float side in new[] { -0.3f, 0f, 0.3f })
            pieces.Add(new Piece(Kind.Puff, new Vector3(cx + side, ground + 0.1f, cz + 0.2f), Now, CloudTint, 0, side, 1.6f));
    }

    /// <summary>A bridge's shadow passes over a tile: a dark puff low on the planks, wide.</summary>
    public void Shadow(Map map, int x, int y)
    {
        if (!map.InBounds(x, y)) return;
        float cx = x + 0.5f, cz = y + 0.5f, ground = Relief.At(map, cx, cz);
        float top = map.DeckAt(x, y) ?? ground;
        foreach (float side in new[] { -0.4f, 0.4f })
            pieces.Add(new Piece(Kind.Puff, new Vector3(cx + side, top, cz + 0.2f), Now, ShadowTint, 0, side, 1.8f));
    }
}
