using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Unova's landmarks of its own (plan 22 · U7; style guide, "Props", "Mistralton's planes" and "Chargestone's crystals"):
/// the small aeroplanes of Mistralton City's apron and runway, and the charged crystals that light Chargestone Cave.
/// No GPU calls.
/// </summary>
internal static partial class Landmarks
{
    private static readonly Tone PlaneBody = Tone.Of(244, 246, 250, 255, 255, 255, 186, 196, 214);
    private static readonly Tone PlaneStripe = Tone.Of(238, 134, 50, 252, 176, 92, 176, 92, 30);
    private static readonly Tone PlaneWing = Tone.Of(226, 232, 242, 248, 250, 255, 166, 176, 198);
    private static readonly Color CrystalLight = Rgb(206, 244, 255), CrystalBase = Rgb(112, 202, 238), CrystalDark = Rgb(58, 128, 200);
    private static readonly Color CrystalSpark = Rgb(255, 250, 164);

    /// <summary>
    /// A small aeroplane with its nose along the longer side of its tiles: a white fuselage with an orange stripe and a
    /// row of windows, a wing across it a third of the way back from the nose, a tail plane and a fin at the tail and
    /// a propeller disc at the nose. Every face is painted at its size; the plane stands on its wheels two texels up.
    /// </summary>
    private static void Airplane(KitBuilder kit, int w, int d)
    {
        bool eastWest = w >= d;
        int length = (eastWest ? w : d) - 8, beam = 14, mid = (eastWest ? d : w) / 2;
        int y0 = 3, y1 = 19;
        int along0 = 4;
        var side = kit.Face($"plane.side.{length}", length, y1 - y0, c => PaintPlaneSide(c));
        var top = kit.Face($"plane.top.{length}x{beam}", eastWest ? length : beam, eastWest ? beam : length, c => PaintPlaneTop(c, eastWest));
        var end = kit.Face("plane.end", beam, y1 - y0, c => { Pix.Raised(c, 0, 0, c.Width, c.Height, PlaneBody); c.Rect(0, c.Height - 5, c.Width, 3, PlaneStripe.Base); });
        if (eastWest)
            kit.Box(along0, along0 + length, mid - beam / 2f, mid + beam / 2f, y0, y1, top, side, end, end, side);
        else
            kit.Box(mid - beam / 2f, mid + beam / 2f, along0, along0 + length, y0, y1, top, end, side, side, end);

        // The wing, a third of the way back; the tail plane and the fin at the tail
        int tail = along0 + length - 14;
        if (eastWest)
        {
            kit.Block("plane.wing", PlaneWing, along0 + length / 3f, along0 + length / 3f + 18, 4, d - 4, y0 + 9, y0 + 12);
            kit.Block("plane.tailplane", PlaneWing, tail, tail + 10, mid - 12, mid + 12, y1 - 7, y1 - 5);
            kit.Block("plane.fin", PlaneStripe, tail, tail + 10, mid - 1.5f, mid + 1.5f, y1, y1 + 14);
            kit.Block("plane.prop", Iron, along0 - 3, along0, mid - 1, mid + 1, y0 + 4, y0 + 12);
        }
        else
        {
            kit.Block("plane.wing", PlaneWing, 4, w - 4, along0 + length / 3f, along0 + length / 3f + 18, y0 + 9, y0 + 12);
            kit.Block("plane.tailplane", PlaneWing, mid - 12, mid + 12, tail, tail + 10, y1 - 7, y1 - 5);
            kit.Block("plane.fin", PlaneStripe, mid - 1.5f, mid + 1.5f, tail, tail + 10, y1, y1 + 14);
            kit.Block("plane.prop", Iron, mid - 1, mid + 1, along0 - 3, along0, y0 + 4, y0 + 12);
        }
    }

    /// <summary>A side of a plane's fuselage: white plates, an orange stripe along it, a row of round windows above it.</summary>
    public static void PaintPlaneSide(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        Pix.Raised(c, 0, 0, w, h, PlaneBody);
        c.Rect(0, h - 7, w, 3, PlaneStripe.Base);
        c.HLine(0, h - 7, w, PlaneStripe.Light);
        for (int x = 14; x + 6 < w - 10; x += 9) Pix.Disc(c, x, 3, 5, Glass);
        // The cockpit's glass near the nose, the end the plane's nose is at
        c.Rect(2, 2, 7, 5, Glass);
    }

    /// <summary>The top of a plane's fuselage: white with the orange stripe down its middle.</summary>
    public static void PaintPlaneTop(PixelCanvas c, bool eastWest)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, PlaneBody);
        if (eastWest) c.Rect(0, c.Height / 2 - 1, c.Width, 2, PlaneStripe.Base);
        else c.Rect(c.Width / 2 - 1, 0, 2, c.Height, PlaneStripe.Base);
    }

    /// <summary>
    /// A cluster of charged crystals standing on the cave floor, 72 by 83 texels (a 40 by 46 drawing at 1.8 times, for the three tiles of the original's model): three shards of pale cyan leaning from one
    /// foot, light on their left faces and dark on their right, with a spark of yellow in the tallest. Its glow is a pool
    /// on the floor and a halo about the tallest shard, drawn with the lamps' light.
    /// </summary>
    private static void ChargeStone(KitBuilder kit, int w, int d, MeshBuilder lamplight)
    {
        const float k = 1.8f;
        kit.Sprite(kit.Face("chargestone2", (int)(40 * k), (int)(46 * k), c => PaintChargeStone(c, k)), w / 2f, d / 2f + 8);
        var foot = kit.Origin + new Vector3(w / 64f, 0.04f, d / 64f + 0.15f);
        const float pool = 1.6f, halo = 0.7f;
        var poolLevel = new Color(110, 120, 130, 255);
        var haloLevel = new Color(120, 130, 140, 255);
        lamplight.Quad(foot + new Vector3(-pool, 0, pool), foot + new Vector3(pool, 0, pool), foot + new Vector3(pool, 0, -pool), foot + new Vector3(-pool, 0, -pool),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), poolLevel, Vector3.UnitY);
        var heart = kit.At(w / 2f, 30 * k, d / 2f + 9);
        float up = halo * kit.VS;
        lamplight.Quad(heart + new Vector3(-halo, -up, 0), heart + new Vector3(halo, -up, 0), heart + new Vector3(halo, up, 0), heart + new Vector3(-halo, up, 0),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), haloLevel, Vector3.UnitZ);
    }

    /// <summary>Three shards from one foot, the tallest in the middle; each is a triangle on a narrow base, its left half light and its right half dark.</summary>
    public static void PaintChargeStone(PixelCanvas c, float k = 1f)
    {
        void Shard(float cx, float baseY, float apexX, float apexY, float half)
        {
            c.Tri(cx - half, baseY, cx, baseY, apexX, apexY, CrystalLight);
            c.Tri(cx, baseY, cx + half, baseY, apexX, apexY, CrystalBase);
            c.Tri(cx + half * 0.3f, baseY, cx + half, baseY, apexX, apexY, CrystalDark);
        }
        Shard(12f * k, 44f * k, 7f * k, 22f * k, 6f * k);
        Shard(28f * k, 44f * k, 31f * k, 25f * k, 7f * k);
        Shard(20f * k, 44f * k, 20f * k, 3f * k, 8f * k);
        c.Rect((int)(15 * k), (int)(38 * k), (int)(12 * k), (int)(6 * k), Rgb(96, 98, 110));
        c.Dot(20f * k, 24f * k, CrystalSpark);
        c.Dot(19f * k, 25f * k, CrystalSpark);
        c.Dot(20f * k, 26f * k, CrystalLight);
        Pix.Outline(c);
    }
}
