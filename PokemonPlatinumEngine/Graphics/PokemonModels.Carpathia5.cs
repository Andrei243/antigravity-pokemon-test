using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 5 (plan 46 · CP4 to CP8): Babelith to Muscarita (20 species), sculpted from nothing after the briefs
// of docs/plans/decisions/CP-dex-2-mountains.md and CP-dex-2b-mountains-more.md and the user's reference pictures
// (docs/art/reference/gemini/carpathia): the plateau's weathered stones, the shepherd's bell and the village belfry, the
// cimbalom, and the beech and oak woods' boars, foxes, squirrels, grouse, roe deer, acorns, oaks and fly agarics. Each
// line is one builder by stage, so its palette and its motifs carry over as it grows. The sound rings and drifting
// spores of the pictures are left out: a loose piece is no part of a sculpt.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames5() => new[]
    {
        "Babelith", "Babelossus", "Clopotel", "Clopotitan", "Tambalet", "Tambalon", "Mistretel", "Mistretor", "Vulpita", "Vulpoi",
        "Veverel", "Veverar", "Cocomunte", "Capriorel", "Caprioara", "Ghindel", "Stejarel", "Stejaron", "Palariuta", "Muscarita"
    };

    private static PokeBuilder? CreateCarpathia5(string species) => species switch
    {
        "BABELITH" => C5Babelith(),
        "BABELOSSUS" => C5Babelossus(),
        "CLOPOTEL" => C5Clopotel(),
        "CLOPOTITAN" => C5Clopotitan(),
        "TAMBALET" => C5Cimbalom(false),
        "TAMBALON" => C5Cimbalom(true),
        "MISTRETEL" => C5Boar(false),
        "MISTRETOR" => C5Boar(true),
        "VULPITA" => C5Fox(false),
        "VULPOI" => C5Fox(true),
        "VEVEREL" => C5Squirrel(false),
        "VEVERAR" => C5Squirrel(true),
        "COCOMUNTE" => C5Cocomunte(),
        "CAPRIOREL" => C5Roe(false),
        "CAPRIOARA" => C5Roe(true),
        "GHINDEL" => C5Ghindel(),
        "STEJAREL" => C5Stejarel(),
        "STEJARON" => C5Stejaron(),
        "PALARIUTA" => C5Palariuta(),
        "MUSCARITA" => C5Muscarita(),
        _ => null
    };

    private static readonly Color C5Sandstone = Rgb(176, 170, 152);
    private static readonly Color C5SandPale = Rgb(214, 204, 182);
    private static readonly Color C5Rust = Rgb(186, 120, 96);
    private static readonly Color C5Lichen = Rgb(232, 174, 50);
    private static readonly Color C5Bronze = Rgb(170, 128, 62);
    private static readonly Color C5Strap = Rgb(84, 60, 44);
    private static readonly Color C5Timber = Rgb(92, 72, 52);
    private static readonly Color C5Iron = Rgb(110, 112, 118);
    private static readonly Color C5Agaric = Rgb(204, 52, 48);
    private static readonly Color C5Gill = Rgb(234, 222, 196);
    private static readonly Color C5Moss = Rgb(120, 150, 80);
    private static readonly Color C5OakLeaf = Rgb(98, 164, 62);
    private static readonly Color C5Bark = Rgb(170, 128, 70);

    /// <summary>Lichen: yellow-orange blots painted over stone at the points given.</summary>
    private static void C5LichenOn(PokeBuilder b, int bone, float size, params Vector3[] at)
    {
        foreach (var p in at) b.PaintEll(bone, p, V(size, size * 0.8f, size), C5Lichen, soft: 0.008f);
    }

    /// <summary>A bronze bell from its crown down to its mouth, hollow below, with a clapper on a bone of its own hanging in it.</summary>
    private static void C5Bell(PokeBuilder b, int bone, int clapper, Vector3 mouth, float h, float r, Color bronze)
    {
        b.Limb(bone, mouth + V(0, h * 0.05f, 0), mouth + V(0, h, 0), r, r * 0.68f, bronze, Metal, 0.03f * r);
        b.Ell(bone, mouth + V(0, h * 0.06f, 0), V(r * 1.12f, h * 0.16f, r * 1.12f), bronze, mat: Metal, blend: 0.04f * r);
        b.Ell(bone, mouth + V(0, h * 1.02f, 0), V(r * 0.7f, h * 0.14f, r * 0.7f), bronze, mat: Metal, blend: 0.05f * r);
        b.Cut(bone, mouth + V(0, -h * 0.06f, 0), V(r * 0.88f, h * 0.55f, r * 0.88f));
        // The clapper on its rod, hanging from the solid crown into the hollow (made after the cut, so it is whole)
        b.Limb(clapper, mouth + V(0, h * 0.62f, 0), mouth + V(0, h * 0.05f, 0), r * 0.09f, r * 0.08f, bronze, Metal, 0.004f);
        b.Ell(clapper, mouth + V(0, -h * 0.02f, 0), V(r * 0.26f, r * 0.26f, r * 0.26f), bronze, mat: Metal, blend: 0.01f);
    }

    /// <summary>A strap's bow: two loops and two tails either side of a knot at <paramref name="knot"/>, in the xy plane.</summary>
    private static void C5Bow(PokeBuilder b, int bone, Vector3 knot, float s, Color strap)
    {
        b.Ell(bone, knot, V(0.035f * s, 0.032f * s, 0.03f * s), strap, mat: SurfaceMaterial.Leather, blend: 0.01f * s);
        PokeBuilder.Both(x =>
        {
            b.Ell(bone, knot + V(0.075f * s * x, 0.03f * s, 0), V(0.07f * s, 0.032f * s, 0.022f * s), strap, V(0, 0, -25f * x), SurfaceMaterial.Leather, 0.012f * s);
            b.Spike(bone, knot + V(0.02f * s * x, -0.01f * s, 0), knot + V(0.08f * s * x, -0.07f * s, 0.005f * s), 0.024f * s, strap, 0.4f, SurfaceMaterial.Leather);
        });
    }

    // ------------------------------------------------------------------ Babelith line

    /// <summary>Babelith: a mushroom of banded sandstone, wider at its cap than at its narrow neck, a stern old face worn into the cap and two stubby stone arms.</summary>
    private static PokeBuilder C5Babelith()
    {
        var b = new PokeBuilder("Babelith", 0.78f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Shell };
        // The neck, flaring to its foot
        b.Limb(Body, V(0, 0.04f, 0), V(0, 0.52f, 0), 0.2f, 0.14f, C5Sandstone, Shell, 0.03f);
        b.Ell(Body, V(0, 0.06f, 0), V(0.25f, 0.07f, 0.22f), C5Sandstone, mat: Shell, blend: 0.05f);
        foreach (var (y, ring) in new[] { (0.13f, 0.19f), (0.24f, 0.176f), (0.34f, 0.163f), (0.44f, 0.151f) })
        {
            b.PaintTorus(Body, V(0, y, 0), ring, 0.012f, C5Rust);
            b.PaintTorus(Body, V(0, y + 0.03f, 0), ring - 0.004f, 0.008f, C5SandPale);
        }
        // The cap, its rim and the bands of rock across it
        var c = V(0, 0.68f, 0);
        var r = V(0.42f, 0.25f, 0.36f);
        b.Ell(Body, c, r, C5Sandstone, mat: Shell);
        b.Ell(Body, V(0, 0.6f, 0), V(0.45f, 0.1f, 0.39f), C5Sandstone, mat: Shell, blend: 0.05f);
        foreach (var (y, ring) in new[] { (0.6f, 0.45f), (0.67f, 0.425f), (0.74f, 0.405f) })
            b.PaintTorus(Body, V(0, y, 0), ring, 0.013f, C5Rust, sx: 1f, sz: 0.87f);
        C5LichenOn(b, Body, 0.045f, V(0.15f, 0.9f, 0.1f), V(-0.2f, 0.86f, -0.05f), V(0.05f, 0.92f, -0.15f), V(0.32f, 0.74f, 0.12f),
            V(-0.34f, 0.7f, 0.12f), V(-0.1f, 0.88f, 0.22f), V(0.12f, 0.06f, 0.2f), V(-0.18f, 0.06f, 0.12f));
        // The stern face: heavy brows, a broad nose and a downturned mouth
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.12f * s, 0.7f);
            b.Ell(Body, at + V(0.005f * s, 0.06f, -0.01f), V(0.085f, 0.03f, 0.05f), C5Sandstone, V(0, 0, 12f * s), Shell, 0.015f);
            b.Eye(Body, at, Outward(c, r, at), 0.032f, Rgb(110, 104, 92), glare: true);
        });
        var nose = On(c, r, 0, 0.64f);
        b.Ell(Body, nose, V(0.045f, 0.06f, 0.04f), C5Sandstone, mat: Shell, blend: 0.015f);
        b.Mark(Body, On(c, r, 0, 0.56f), Outward(c, r, On(c, r, 0, 0.56f)), 0.07f, 0.02f, Rgb(92, 80, 70), MarkShape.Smile, 180f);
        // Two stubby stone arms hanging from under the cap
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.3f * s, 0.53f, 0.04f));
            b.Limb(arm, V(0.3f * s, 0.54f, 0.04f), V(0.34f * s, 0.3f, 0.08f), 0.075f, 0.065f, C5Sandstone, Shell);
            b.Ell(arm, V(0.35f * s, 0.25f, 0.09f), V(0.07f, 0.065f, 0.065f), C5Sandstone, mat: Shell);
            Digits(b, arm, V(0.35f * s, 0.21f, 0.12f), V(0, -0.6f, 0.8f), V(0.45f, 0, 0), 0.05f, 0.02f, C5Sandstone, Shell);
            b.PaintTorus(arm, V(0.32f * s, 0.43f, 0.06f), 0.075f, 0.01f, C5Rust);
            b.PaintTorus(arm, V(0.335f * s, 0.35f, 0.07f), 0.07f, 0.01f, C5Rust);
        });
        b.Ground();
        return b;
    }

    /// <summary>Babelossus: the plateau's great stone face on a body of stacked sandstone, grass and flowers on its shoulder ledges, violet signs glowing on its brow.</summary>
    private static PokeBuilder C5Babelossus()
    {
        var b = new PokeBuilder("Babelossus", 0.96f, BodyPlan.Biped, V(0, 0.95f, 0)) { Coat = Shell };
        var grass = Rgb(110, 160, 70);
        var violet = Rgb(178, 96, 236);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.17f * s, 0.62f, 0));
            b.Limb(leg, V(0.17f * s, 0.66f, 0), V(0.2f * s, 0.14f, 0.02f), 0.13f, 0.12f, C5Sandstone, Shell);
            b.Box(leg, V(0.2f * s, 0.07f, 0.06f), V(0.12f, 0.07f, 0.16f), 0.04f, C5Sandstone, mat: Shell, blend: 0.02f);
            b.Ell(leg, V(0.2f * s, 0.4f, 0.1f), V(0.1f, 0.09f, 0.05f), C5Sandstone, mat: Shell, blend: 0.02f);
            foreach (float y in new[] { 0.22f, 0.32f, 0.52f })
                b.PaintTorus(leg, V(0.19f * s, y, 0.01f), 0.13f, 0.012f, C5Rust);
        });
        // The body: stacked blocks of sandstone, banded
        b.Box(Body, V(0, 0.86f, 0), V(0.27f, 0.2f, 0.19f), 0.08f, C5Sandstone, mat: Shell, blend: 0.03f);
        b.Box(Body, V(0, 1.12f, -0.01f), V(0.34f, 0.15f, 0.23f), 0.07f, C5Sandstone, mat: Shell, blend: 0.03f);
        foreach (float y in new[] { 0.76f, 0.88f, 1.02f, 1.14f })
            b.PaintEll(Body, V(0, y, 0), V(0.4f, 0.012f, 0.3f), C5Rust, soft: 0.006f);
        // The shoulder ledges, grassed over and flowering
        PokeBuilder.Both(s =>
        {
            b.Box(Body, V(0.36f * s, 1.3f, 0), V(0.21f, 0.06f, 0.23f), 0.03f, C5Sandstone, V(0, 0, -4f * s), Shell, 0.03f);
            b.PaintEll(Body, V(0.36f * s, 1.37f, 0), V(0.22f, 0.03f, 0.24f), grass, soft: 0.01f);
            foreach (var (x, z) in new[] { (0.26f, 0.12f), (0.4f, -0.08f), (0.48f, 0.1f), (0.3f, -0.14f), (0.5f, -0.02f) })
                b.Spike(Body, V(x * s, 1.34f, z), V((x + 0.02f) * s, 1.43f, z + 0.01f), 0.022f, grass, 0.5f, Leaf);
            foreach (var (x, z, col) in new[] { (0.33f, 0.02f, Rgb(246, 210, 70)), (0.45f, 0.14f, violet), (0.42f, -0.15f, White) })
                b.Ell(Body, V(x * s, 1.365f, z), V(0.022f, 0.016f, 0.022f), col, mat: Leaf, blend: 0.006f);
        });
        // Great arms of stacked stone, fists of blocks
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.44f * s, 1.22f, 0));
            b.Limb(arm, V(0.44f * s, 1.22f, 0), V(0.52f * s, 0.9f, 0.04f), 0.12f, 0.11f, C5Sandstone, Shell);
            b.Limb(arm, V(0.52f * s, 0.9f, 0.04f), V(0.55f * s, 0.6f, 0.1f), 0.13f, 0.12f, C5Sandstone, Shell);
            b.Box(arm, V(0.56f * s, 0.48f, 0.12f), V(0.11f, 0.1f, 0.11f), 0.05f, C5Sandstone, mat: Shell, blend: 0.03f);
            Digits(b, arm, V(0.56f * s, 0.42f, 0.2f), V(0, -0.7f, 0.6f), V(0.4f, 0, 0), 0.07f, 0.035f, C5Sandstone, Shell);
            foreach (var (x, y, z) in new[] { (0.48f, 1.06f, 0.02f), (0.53f, 0.8f, 0.06f), (0.542f, 0.68f, 0.08f) })
                b.PaintTorus(arm, V(x * s, y, z), 0.12f, 0.012f, C5Rust);
            C5LichenOn(b, arm, 0.04f, V(0.6f * s, 0.75f, 0.14f), V(0.5f * s, 1.0f, 0.1f));
        });
        // The head: a solemn great face, its eyes shut, signs glowing on its brow
        int head = b.Head(V(0, 1.36f, 0.04f));
        var c = V(0, 1.56f, 0.06f);
        var r = V(0.19f, 0.22f, 0.17f);
        b.Limb(head, V(0, 1.3f, 0.02f), V(0, 1.48f, 0.05f), 0.15f, 0.15f, C5Sandstone, Shell);
        b.Ell(head, c, r, C5Sandstone, mat: Shell);
        b.Ell(head, c + V(0, 0.05f, -0.06f), V(0.17f, 0.2f, 0.15f), C5Sandstone, mat: Shell, blend: 0.04f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.07f * s, 1.58f);
            b.Ell(head, at + V(0, 0.045f, -0.01f), V(0.075f, 0.025f, 0.04f), C5Sandstone, V(0, 0, 8f * s), Shell, 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.03f, Rgb(120, 112, 100), closed: true);
        });
        var nose = On(c, r, 0, 1.53f);
        b.Ell(head, nose, V(0.04f, 0.07f, 0.045f), C5Sandstone, mat: Shell, blend: 0.015f);
        var mouth = On(c, r, 0, 1.44f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.06f, 0.018f, Rgb(96, 84, 74), MarkShape.Smile, 180f);
        foreach (var (x, shape) in new[] { (-0.1f, MarkShape.Zigzag), (0f, MarkShape.Diamond), (0.1f, MarkShape.Triangle) })
        {
            var at = On(c, r, x, 1.69f);
            b.Mark(head, at, Outward(c, r, at), 0.028f, 0.022f, violet, shape);
        }
        b.PaintEll(head, c + V(0, -0.02f, 0), V(0.2f, 0.012f, 0.18f), C5Rust, soft: 0.006f);
        C5LichenOn(b, head, 0.035f, c + V(0.12f, 0.12f, 0.05f), c + V(-0.15f, -0.05f, 0.06f));
        C5LichenOn(b, Body, 0.05f, V(0.2f, 1.1f, 0.22f), V(-0.12f, 0.9f, 0.18f), V(0.15f, 0.8f, 0.18f));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Clopotel line

    /// <summary>Clopotel: a small dented bronze shepherd's bell, a leather strap tied in a bow on its crown, two eyes on its shoulder and a little round clapper for a tongue.</summary>
    private static PokeBuilder C5Clopotel()
    {
        var b = new PokeBuilder("Clopotel", 0.45f, BodyPlan.Floating, V(0, 0.36f, 0)) { Coat = Metal }.Hover();
        int clapper = b.Part("clapper", Body, V(0, 0.4f, 0), PokeRole.Tail);
        C5Bell(b, Body, clapper, V(0, 0.18f, 0), 0.3f, 0.17f, C5Bronze);
        // Patina and dents painted on, a crack at the mouth
        foreach (var (x, y, z) in new[] { (0.09f, 0.36f, 0.1f), (-0.12f, 0.27f, 0.08f), (0.05f, 0.42f, -0.1f), (-0.06f, 0.3f, -0.14f) })
            b.PaintEll(Body, V(x, y, z), V(0.03f, 0.025f, 0.03f), Rgb(130, 96, 46), soft: 0.008f);
        b.PaintEll(Body, V(-0.08f, 0.22f, 0.15f), V(0.006f, 0.04f, 0.02f), Rgb(70, 52, 30), V(0, 0, 20f), 0.004f);
        // The loop on its crown and the strap tied in a bow there
        b.Torus(Body, V(0, 0.52f, 0), 0.04f, 0.012f, C5Bronze, V(90f, 0, 0), mat: Metal, blend: 0.006f);
        int bow = b.Part("bow", Body, V(0, 0.56f, 0), PokeRole.Leaf);
        b.Limb(bow, V(0, 0.5f, 0), V(0, 0.56f, 0.0f), 0.018f, 0.02f, C5Strap, SurfaceMaterial.Leather, 0.006f);
        C5Bow(b, bow, V(0, 0.58f, 0.0f), 1f, C5Strap);
        // Two eyes on the bell's shoulder
        PokeBuilder.Both(s => b.Eye(Body, V(0.048f * s, 0.38f, 0.128f), V(0.35f * s, 0.25f, 1f), 0.026f, Rgb(84, 60, 36)));
        // Raised by less than Lift's gap: the hollow carved under the bell leaves its lowest point higher than its shapes' reach
        b.Ground(0.07f);
        return b;
    }

    /// <summary>Clopotitan: a mountain village's wooden belfry walking on timber legs, a shingled skirt and a needle roof with the strap's bow round it, a great bell for a heart and two eyes glowing in its dark.</summary>
    private static PokeBuilder C5Clopotitan()
    {
        var b = new PokeBuilder("Clopotitan", 0.95f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = SurfaceMaterial.Leather };
        var shingle = Rgb(84, 70, 58);
        var shingleDark = Rgb(64, 54, 46);
        var moss = Rgb(104, 124, 66);
        // The needle roof before anything else, its round foot cut off flat at the eaves: a spike's base is a ball as
        // wide as the roof, which would otherwise fill the belfry and bury the eyes on its back wall
        b.Spike(Body, V(0, 1.2f, 0), V(0, 1.72f, 0), 0.32f, shingle, mat: SurfaceMaterial.Leather, blend: 0.02f);
        b.CutBox(Body, V(0, 0.7f, 0), V(0.5f, 0.5f, 0.5f), Quaternion.Identity);
        // The bell next, so its hollow is carved out of it alone: it hangs in the belfry as a heart
        int clapper = b.Part("clapper", Body, V(0, 1.0f, 0), PokeRole.Tail);
        C5Bell(b, Body, clapper, V(0, 0.8f, 0.02f), 0.26f, 0.14f, C5Bronze);
        b.Limb(Body, V(0, 1.18f, 0), V(0, 1.04f, 0.01f), 0.03f, 0.03f, C5Iron, Metal, 0.006f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.15f * s, 0.42f, 0));
            b.Box(leg, V(0.15f * s, 0.22f, 0.01f), V(0.09f, 0.22f, 0.09f), 0.025f, C5Timber, mat: SurfaceMaterial.Leather, blend: 0.01f);
            b.Box(leg, V(0.15f * s, 0.035f, 0.04f), V(0.11f, 0.035f, 0.13f), 0.02f, C5Timber, mat: SurfaceMaterial.Leather, blend: 0.01f);
            b.PaintEll(leg, V(0.15f * s, 0.16f, 0.01f), V(0.11f, 0.02f, 0.11f), C5Iron, soft: 0.004f);
        });
        // The shingled skirt of the lower storey, banded in iron at its corners
        b.Box(Body, V(0, 0.57f, 0), V(0.3f, 0.15f, 0.27f), 0.03f, shingle, mat: SurfaceMaterial.Leather, blend: 0.02f);
        b.Box(Body, V(0, 0.45f, 0), V(0.34f, 0.04f, 0.31f), 0.02f, shingle, mat: SurfaceMaterial.Leather, blend: 0.03f);
        foreach (float y in new[] { 0.5f, 0.58f, 0.66f })
            b.PaintEll(Body, V(0, y, 0), V(0.4f, 0.008f, 0.36f), shingleDark, soft: 0.004f);
        // The belfry floor, four posts, the back wall in shadow and the eaves
        b.Box(Body, V(0, 0.72f, 0), V(0.3f, 0.025f, 0.27f), 0.01f, C5Timber, mat: SurfaceMaterial.Leather, blend: 0.01f);
        foreach (var (x, z) in new[] { (0.26f, 0.23f), (-0.26f, 0.23f), (0.26f, -0.23f), (-0.26f, -0.23f) })
        {
            b.Limb(Body, V(x, 0.72f, z), V(x, 1.18f, z), 0.035f, 0.035f, C5Timber, SurfaceMaterial.Leather, 0.01f);
            b.PaintEll(Body, V(x, 0.78f, z), V(0.045f, 0.03f, 0.045f), C5Iron, soft: 0.004f);
        }
        b.Box(Body, V(0, 0.96f, -0.2f), V(0.26f, 0.24f, 0.035f), 0.01f, Rgb(40, 32, 30), mat: SurfaceMaterial.Leather, blend: 0.01f);
        PokeBuilder.Both(s => b.Box(Body, V(0.24f * s, 0.96f, 0), V(0.02f, 0.24f, 0.21f), 0.01f, C5Timber, mat: SurfaceMaterial.Leather, blend: 0.01f));
        PokeBuilder.Both(s => b.Cut(Body, V(0.24f * s, 0.98f, 0.0f), V(0.05f, 0.13f, 0.12f)));
        PokeBuilder.Both(s => b.Eye(Body, V(0.14f * s, 1.02f, -0.165f), V(0, 0, 1f), 0.03f, Rgb(250, 200, 60), glare: true));
        // The roof: the eaves, a steep shingled needle and the bow of the strap round it
        b.Box(Body, V(0, 1.2f, 0), V(0.36f, 0.03f, 0.33f), 0.015f, shingle, mat: SurfaceMaterial.Leather, blend: 0.01f);
        foreach (float y in new[] { 1.28f, 1.36f, 1.44f })
            b.PaintTorus(Body, V(0, y, 0), 0.3f * (1.72f - y) / 0.52f, 0.008f, shingleDark);
        int bow = b.Part("bow", Body, V(0, 1.42f, 0.05f), PokeRole.Leaf);
        b.Torus(bow, V(0, 1.4f, 0), 0.165f, 0.018f, C5Strap, mat: SurfaceMaterial.Leather, blend: 0.01f);
        C5Bow(b, bow, V(0, 1.42f, 0.17f), 1.4f, C5Strap);
        // Timber arms, bound in iron, moss on them, blocky hands
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.3f * s, 1.0f, 0));
            b.Box(arm, V(0.36f * s, 0.86f, 0.02f), V(0.06f, 0.17f, 0.06f), 0.015f, C5Timber, V(0, 0, 12f * s), SurfaceMaterial.Leather, 0.02f);
            b.Box(arm, V(0.42f * s, 0.56f, 0.06f), V(0.08f, 0.17f, 0.08f), 0.02f, C5Timber, mat: SurfaceMaterial.Leather, blend: 0.02f);
            foreach (float y in new[] { 0.48f, 0.64f })
                b.PaintEll(arm, V(0.42f * s, y, 0.06f), V(0.1f, 0.018f, 0.1f), C5Iron, soft: 0.004f);
            b.PaintEll(arm, V(0.44f * s, 0.56f, 0.12f), V(0.04f, 0.08f, 0.03f), moss, soft: 0.01f);
            Digits(b, arm, V(0.42f * s, 0.4f, 0.1f), V(0, -0.7f, 0.6f), V(0.45f, 0, 0), 0.07f, 0.028f, C5Timber, SurfaceMaterial.Leather);
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Tambalet line

    /// <summary>
    /// Tambalet and Tambalon: tortoises whose shell is a cimbalom's top, polished wood with silver strings across it,
    /// two long feelers looping up and over with soft hammer-heads; the grown one's a concert cimbalom of carved dark
    /// wood with gold scrolls, a carved crest on its head and its eyes shut, lost in the music.
    /// </summary>
    private static PokeBuilder C5Cimbalom(bool big)
    {
        float k = big ? 1.5f : 1f;
        var b = new PokeBuilder(big ? "Tambalon" : "Tambalet", big ? 0.9f : 0.55f, BodyPlan.Quadruped, V(0, 0.2f * k, 0)) { Coat = Scales };
        var skin = Rgb(214, 150, 92);
        var belly = Rgb(236, 196, 132);
        var wood = big ? Rgb(110, 62, 42) : Rgb(150, 70, 50);
        var woodEdge = big ? Rgb(82, 46, 32) : Rgb(120, 54, 40);
        var silver = Rgb(222, 224, 232);
        var gold = Rgb(222, 180, 82);
        var teal = Rgb(72, 150, 140);
        var hammer = Rgb(222, 206, 186);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                float x = 0.14f * s * k, zz = z * k;
                int leg = b.Leg(s, V(x, 0.16f * k, zz), front);
                b.Limb(leg, V(x, 0.17f * k, zz), V(x * 1.15f, 0.05f * k, zz + 0.02f * k), 0.055f * k, 0.05f * k, skin);
                b.Ell(leg, V(x * 1.15f, 0.03f * k, zz + 0.03f * k), V(0.055f * k, 0.03f * k, 0.065f * k), skin);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Ell(leg, V(x * 1.15f + t * 0.025f * k, 0.02f * k, zz + 0.085f * k), V(0.014f * k, 0.016f * k, 0.014f * k), belly, mat: Shell, blend: 0.006f);
                b.Mark(leg, V(x * 1.12f + 0.05f * s * k, 0.1f * k, zz + 0.01f * k), V(s, 0, 0.4f), 0.03f * k, 0.014f * k, front ? teal : gold, MarkShape.Zigzag);
            });
        // The body under the shell, its pale belly showing
        b.Ell(Body, V(0, 0.2f * k, 0), V(0.2f * k, 0.1f * k, 0.22f * k), skin);
        b.PaintEll(Body, V(0, 0.12f * k, 0.02f * k), V(0.18f * k, 0.04f * k, 0.2f * k), belly);
        // The shell: the instrument's box, its strings and bridges
        var top = 0.33f * k;
        b.Box(Body, V(0, 0.28f * k, -0.01f * k), V(0.25f * k, 0.05f * k, 0.22f * k), 0.025f * k, wood, V(0, 0, 0), Shell, 0.02f * k);
        b.PaintEll(Body, V(0, 0.27f * k, 0.22f * k), V(0.26f * k, 0.03f * k, 0.02f * k), woodEdge, soft: 0.006f);
        if (big)
        {
            b.Box(Body, V(0, 0.36f * k, -0.03f * k), V(0.22f * k, 0.03f * k, 0.18f * k), 0.015f * k, wood, mat: Shell, blend: 0.01f);
            top = 0.39f * k;
            foreach (var (x, z) in new[] { (-0.17f, 0.2f), (0.17f, 0.2f), (0.24f, 0.05f), (-0.24f, 0.05f) })
                b.Mark(Body, V(x * k, 0.28f * k, z * k), V(x, 0, z), 0.03f * k, 0.022f * k, gold, MarkShape.Wave);
            b.Mark(Body, V(0, 0.28f * k, -0.23f * k), V(0, 0, -1f), 0.025f * k, 0.022f * k, teal, MarkShape.Diamond);
        }
        float zs = big ? 0.17f : 0.19f;
        for (int i = 0; i < (big ? 11 : 8); i++)
        {
            float x = -0.2f + 0.4f * i / (big ? 10f : 7f);
            b.PaintEll(Body, V(x * k * (big ? 0.95f : 1f), top, -0.02f * k), V(0.0035f * k, 0.01f * k, zs * k), silver, soft: 0.003f);
        }
        foreach (float z in big ? new[] { -0.08f, 0.06f } : new[] { -0.06f, 0.08f })
            b.Box(Body, V(0, top + 0.01f * k, z * k), V(0.21f * k, 0.012f * k, 0.012f * k), 0.006f * k, gold, mat: Shell, blend: 0.006f);
        // Tuning pegs along the shell's side edges
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 5; i++)
                b.Limb(Body, V(0.235f * s * k, 0.32f * k, (-0.16f + 0.08f * i) * k), V(0.235f * s * k, 0.355f * k, (-0.16f + 0.08f * i) * k),
                    0.012f * k, 0.01f * k, silver, Metal, 0.004f);
        });
        int tail = b.Tail(V(0, 0.2f * k, -0.2f * k));
        b.Spike(tail, V(0, 0.19f * k, -0.18f * k), V(0, 0.15f * k, -0.3f * k), 0.04f * k, skin);
        // The neck stretched out under the shell's edge, the head with its swirls
        int head = b.Head(V(0, 0.24f * k, 0.2f * k));
        b.Limb(head, V(0, 0.22f * k, 0.14f * k), V(0, 0.3f * k, 0.3f * k), 0.065f * k, 0.06f * k, skin);
        var c = V(0, 0.34f * k, 0.36f * k);
        var r = V(0.09f * k, 0.08f * k, 0.09f * k);
        b.Ell(head, c, r, skin);
        b.Ell(head, c + V(0, -0.03f * k, 0.06f * k), V(0.06f * k, 0.045f * k, 0.05f * k), skin, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s * k, 0.36f * k);
            b.Eye(head, at, Outward(c, r, at), 0.024f * k, Rgb(70, 50, 34), closed: big);
            var cheek = On(c, r, 0.07f * s * k, 0.32f * k);
            b.Mark(head, cheek, Outward(c, r, cheek), 0.014f * k, 0.014f * k, Rgb(176, 104, 60), MarkShape.Ring);
        });
        var smile = c + V(0, -0.045f * k, 0.105f * k);
        b.Mark(head, smile, V(0, -0.3f, 1f), 0.03f * k, 0.012f * k, Rgb(110, 64, 40), MarkShape.Smile);
        b.Mark(head, c + V(0, 0.06f * k, 0.05f * k), V(0, 0.7f, 0.7f), 0.03f * k, 0.03f * k, Rgb(176, 104, 60), MarkShape.Ring);
        if (big)
        {
            // A carved wooden crest standing on its head
            b.Box(head, c + V(0, 0.1f * k, -0.02f * k), V(0.05f * k, 0.06f * k, 0.02f * k), 0.012f * k, woodEdge, mat: Shell, blend: 0.015f);
            PokeBuilder.Both(s => b.Ell(head, c + V(0.05f * s * k, 0.13f * k, -0.02f * k), V(0.025f * k, 0.025f * k, 0.015f * k), woodEdge, mat: Shell, blend: 0.01f));
            b.Mark(head, c + V(0, 0.11f * k, 0.0f), V(0, 0, 1f), 0.03f * k, 0.035f * k, gold, MarkShape.Wave);
        }
        // The two feelers: up from the brow, over in a loop and down to soft hammers
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s * k, 0.06f * k, -0.02f * k));
            var root = c + V(0.035f * s * k, 0.06f * k, -0.02f * k);
            var loop = s < 0
                ? new[] { root, root + V(-0.02f * k, 0.18f * k, -0.05f * k), root + V(0.03f * k, 0.3f * k, -0.14f * k), root + V(0.06f * k, 0.24f * k, -0.26f * k), root + V(0.1f * k, 0.12f * k, -0.3f * k) }
                : new[] { root, root + V(0.04f * k, 0.2f * k, -0.04f * k), root + V(0.1f * k, 0.26f * k, 0.06f * k), root + V(0.16f * k, 0.16f * k, 0.14f * k), root + V(0.2f * k, 0.06f * k, 0.14f * k) };
            var path = Smooth(3, loop);
            b.Tube(ear, path, 0.014f * k, 0.012f * k, skin, blend: 0.006f);
            var end = path[^1];
            var dir = Vector3.Normalize(path[^1] - path[^3]);
            b.Ell(ear, end + dir * 0.03f * k, V(0.03f * k, 0.04f * k, 0.03f * k), hammer, Euler(dir), SurfaceMaterial.Cloth, 0.008f);
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Mistretel line

    /// <summary>
    /// Mistretel and Mistretor: a chubby striped boar piglet snuffling nose-down with tiny tusk buds and a curled tail;
    /// the grown boar dark and grizzled, a crest of black bristles down its spine, two great curved tusks, its legs
    /// caked in mud, head lowered to charge.
    /// </summary>
    private static PokeBuilder C5Boar(bool big)
    {
        float k = big ? 1.75f : 1f;
        var b = new PokeBuilder(big ? "Mistretor" : "Mistretel", big ? 0.9f : 0.55f, BodyPlan.Quadruped, V(0, 0.22f * k, 0)) { Coat = Fur };
        var coat = big ? Rgb(88, 82, 72) : Rgb(150, 112, 78);
        var stripe = big ? Rgb(196, 182, 150) : Rgb(226, 206, 160);
        var snout = big ? Rgb(110, 80, 74) : Rgb(196, 140, 118);
        var hoof = Rgb(70, 56, 48);
        var mud = Rgb(132, 116, 84);
        var tusk = Rgb(240, 232, 210);
        var bristle = Rgb(46, 44, 42);
        foreach (var (z, front) in new[] { (0.13f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                float x = 0.09f * s * k, zz = z * k;
                int leg = b.Leg(s, V(x, 0.16f * k, zz), front);
                b.Limb(leg, V(x, 0.2f * k, zz), V(x, 0.05f * k, zz + 0.01f * k), 0.045f * k, 0.03f * k, coat);
                b.Ell(leg, V(x, 0.028f * k, zz + 0.02f * k), V(0.03f * k, 0.028f * k, 0.035f * k), hoof, mat: Shell, blend: 0.01f);
                if (big) b.PaintEll(leg, V(x, 0.06f * k, zz + 0.01f * k), V(0.05f * k, 0.06f * k, 0.05f * k), mud, soft: 0.012f);
            });
        // The barrel of a body, striped along its back
        var bc = V(0, 0.24f * k, -0.01f * k);
        var br = V(0.15f * k, 0.13f * k, 0.22f * k);
        b.Ell(Body, bc, br, coat);
        b.Ell(Body, V(0, 0.26f * k, 0.1f * k), V(0.14f * k, 0.13f * k, 0.12f * k), coat, blend: 0.04f);
        foreach (float x in big ? new[] { -0.06f, 0.06f } : new[] { -0.1f, -0.04f, 0.04f, 0.1f })
            b.PaintEll(Body, V(x * k, 0.33f * k, -0.02f * k), V(0.016f * k, 0.05f * k, 0.18f * k), stripe, V(0, 0, x * 260f), 0.008f);
        if (big)
        {
            // The crest of bristles standing up along its spine
            for (int i = 0; i < 9; i++)
            {
                float z = 0.15f - i * 0.04f;
                float h = 0.1f + 0.05f * MathF.Sin(i * 0.4f);
                var root = V(0, 0.34f * k + 0.02f * k * MathF.Cos(i * 0.3f), z * k);
                b.Spike(Body, root, root + V(0, h * k, -0.04f * k), 0.028f * k, bristle, 0.5f);
            }
        }
        // The head lowered, an upturned snout and the tusks
        int head = b.Head(V(0, 0.26f * k, 0.18f * k));
        var c = V(0, 0.23f * k, 0.24f * k);
        var r = V(0.1f * k, 0.09f * k, 0.1f * k);
        b.Ell(head, c, r, coat);
        b.Limb(head, c + V(0, -0.01f * k, 0.04f * k), c + V(0, -0.05f * k, 0.15f * k), 0.06f * k, 0.045f * k, coat);
        var disc = c + V(0, -0.055f * k, 0.165f * k);
        b.Ell(head, disc, V(0.045f * k, 0.035f * k, 0.018f * k), snout, V(-20f, 0, 0), blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, disc + V(0.016f * s * k, 0.002f * k, 0.016f * k), V(0, 0.3f, 1f), 0.007f * k, 0.01f * k, Rgb(70, 40, 36));
            if (big)
                b.Tube(head, Smooth(3, c + V(0.04f * s * k, -0.05f * k, 0.1f * k), c + V(0.1f * s * k, -0.06f * k, 0.13f * k), c + V(0.13f * s * k, 0.0f, 0.13f * k), c + V(0.12f * s * k, 0.06f * k, 0.1f * k)),
                    0.016f * k, 0.004f * k, tusk, Shell, 0.004f);
            else
                b.Spike(head, c + V(0.035f * s * k, -0.05f * k, 0.1f * k), c + V(0.055f * s * k, -0.025f * k, 0.12f * k), 0.01f, tusk, mat: Shell);
            var at = On(c, r, 0.055f * s * k, c.Y + 0.02f * k);
            b.Eye(head, at, Outward(c, r, at), (big ? 0.016f : 0.018f) * k, Rgb(40, 30, 26), glare: big);
            int ear = b.Ear(head, s, c + V(0.06f * s * k, 0.06f * k, -0.03f * k));
            CatEar(b, ear, c + V(0.06f * s * k, 0.06f * k, -0.03f * k), c + V(0.11f * s * k, 0.13f * k, -0.05f * k), 0.035f * k, coat, Rgb(110, 80, 64));
        });
        if (big)
            foreach (var (x, y, z) in new[] { (0f, 0.1f, 0.0f), (0.03f, 0.09f, -0.04f), (-0.03f, 0.09f, -0.04f) })
                b.Spike(head, c + V(x * k, (y - 0.03f) * k, z * k), c + V(x * k, (y + 0.05f) * k, (z - 0.05f) * k), 0.022f * k, bristle, 0.5f);
        // The tail: a curl on the piglet, a tufted whip on the boar
        int tail = b.Tail(V(0, 0.28f * k, -0.22f * k));
        if (big)
        {
            var tp = Smooth(3, V(0, 0.28f, -0.22f) * k, V(0, 0.34f, -0.3f) * k, V(0, 0.42f, -0.3f) * k, V(0, 0.44f, -0.24f) * k);
            b.Tube(tail, tp, 0.018f * k, 0.012f * k, coat);
            b.Spike(tail, tp[^1], tp[^1] + V(0, -0.02f, 0.06f) * k, 0.03f * k, bristle, 0.5f);
        }
        else
        {
            var curl = Spiral(V(0, 0.33f, -0.27f) * k, V(0, 0, -1f), V(0, 1f, 0), 0.035f * k, 0.015f * k, MathF.PI, MathF.PI + 1.6f * MathF.Tau, 16);
            b.Tube(tail, new[] { V(0, 0.28f, -0.21f) * k }.Concat(curl).ToArray(), 0.014f * k, 0.008f * k, coat);
        }
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Vulpita line

    /// <summary>
    /// Vulpita and Vulpoi: a sly red fox sitting with one forepaw raised, huge ears, a white ruff of cheeks and chest,
    /// black stockings, its great bushy tail curled round beside it, white-tipped on the cub, an ember at the trickster's tip.
    /// </summary>
    private static PokeBuilder C5Fox(bool big)
    {
        float k = big ? 1.55f : 1f;
        var b = new PokeBuilder(big ? "Vulpoi" : "Vulpita", big ? 0.8f : 0.55f, BodyPlan.Quadruped, V(0, 0.2f * k, 0)) { Coat = Fur };
        var red = big ? Rgb(150, 40, 40) : Rgb(140, 36, 40);
        var dark = Rgb(40, 30, 32);
        var white = Rgb(244, 240, 240);
        // The haunches folded under, black-stockinged hind feet in front of them
        PokeBuilder.Both(s =>
        {
            int hind = b.Leg(s, V(0.08f * s * k, 0.1f * k, -0.06f * k), front: false);
            b.Ell(hind, V(0.08f * s * k, 0.1f * k, -0.05f * k), V(0.07f * k, 0.09f * k, 0.1f * k), red);
            b.Ell(hind, V(0.09f * s * k, 0.025f * k, 0.06f * k), V(0.035f * k, 0.025f * k, 0.065f * k), dark);
            b.Limb(hind, V(0.09f * s * k, 0.06f * k, -0.03f * k), V(0.09f * s * k, 0.03f * k, 0.04f * k), 0.03f * k, 0.028f * k, dark);
        });
        // Forelegs: the left standing, the right raised and curled
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s * k, 0.22f * k, 0.05f * k));
            if (s < 0)
            {
                b.Limb(leg, V(-0.05f * k, 0.22f * k, 0.05f * k), V(-0.055f * k, 0.04f * k, 0.1f * k), 0.026f * k, 0.022f * k, dark);
                b.Ell(leg, V(-0.055f * k, 0.022f * k, 0.12f * k), V(0.03f * k, 0.022f * k, 0.04f * k), dark);
            }
            else
            {
                b.Limb(leg, V(0.05f * k, 0.22f * k, 0.05f * k), V(0.06f * k, 0.18f * k, 0.15f * k), 0.026f * k, 0.022f * k, dark);
                b.Limb(leg, V(0.06f * k, 0.18f * k, 0.15f * k), V(0.055f * k, 0.25f * k, 0.15f * k), 0.022f * k, 0.022f * k, dark);
                b.Ell(leg, V(0.055f * k, 0.265f * k, 0.15f * k), V(0.03f * k, 0.03f * k, 0.028f * k), dark);
            }
        });
        // The body sitting up, a white chest
        b.Ell(Body, V(0, 0.2f * k, -0.01f * k), V(0.09f * k, 0.15f * k, 0.1f * k), red, V(-15f, 0, 0));
        b.PaintEll(Body, V(0, 0.22f * k, 0.07f * k), V(0.05f * k, 0.1f * k, 0.05f * k), white);
        // The head: huge ears, a white ruff, a narrow sly look and a smirk
        int head = b.Head(V(0, 0.32f * k, 0.02f * k));
        var c = V(0, 0.4f * k, 0.03f * k);
        var r = V(0.08f * k, 0.07f * k, 0.075f * k);
        b.Limb(head, V(0, 0.3f * k, 0.01f * k), c, 0.05f * k, 0.05f * k, red);
        b.Ell(head, c, r, red);
        b.Limb(head, c + V(0, -0.02f * k, 0.04f * k), c + V(0, -0.03f * k, 0.11f * k), 0.035f * k, 0.014f * k, red);
        b.Ell(head, c + V(0, -0.028f * k, 0.12f * k), V(0.012f * k, 0.01f * k, 0.01f * k), dark, blend: 0.004f);
        b.PaintEll(head, c + V(0, -0.04f * k, 0.07f * k), V(0.045f * k, 0.02f * k, 0.06f * k), white);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
            {
                var root = c + V(0.05f * s * k, (-0.02f - 0.012f * i) * k, 0.02f * k);
                b.Spike(head, root, root + V((0.06f - 0.01f * i) * s * k, (-0.015f - 0.01f * i) * k, 0.0f), 0.025f * k, white, 0.5f);
            }
            b.PaintEll(head, c + V(0.05f * s * k, -0.03f * k, 0.03f * k), V(0.035f * k, 0.025f * k, 0.035f * k), white);
            var at = On(c, r, 0.035f * s * k, c.Y + 0.01f * k);
            b.Eye(head, at, Outward(c, r, at), 0.018f * k, Rgb(214, 150, 60), glare: true);
            int ear = b.Ear(head, s, c + V(0.045f * s * k, 0.05f * k, -0.01f * k));
            var root2 = c + V(0.045f * s * k, 0.05f * k, -0.01f * k);
            var tip = root2 + V(0.07f * s * k, 0.17f * k, -0.02f * k);
            CatEar(b, ear, root2, tip, 0.045f * k, red, Rgb(70, 30, 34));
            b.PaintEll(ear, Vector3.Lerp(root2, tip, 0.88f), V(0.03f * k, 0.035f * k, 0.03f * k), dark);
        });
        b.Mark(head, c + V(0, -0.045f * k, 0.095f * k), V(0, -0.2f, 1f), 0.022f * k, 0.01f * k, dark, MarkShape.Smile, 12f);
        // The tail: a great curl round its right side
        int tail = b.Tail(V(0, 0.12f * k, -0.07f * k));
        var tp = Smooth(3, V(0, 0.12f, -0.07f) * k, V(0.08f, 0.1f, -0.2f) * k, V(0.18f, 0.12f, -0.16f) * k, V(0.22f, 0.12f, -0.04f) * k, V(0.2f, 0.1f, 0.06f) * k);
        b.Tube(tail, tp, 0.05f * k, 0.075f * k, red, blend: 0.02f);
        b.PaintEll(tail, V(0.19f, 0.13f, -0.14f) * k, V(0.06f * k, 0.08f * k, 0.05f * k), Rgb(90, 30, 34), soft: 0.015f);
        if (big)
        {
            b.PaintEll(tail, tp[^1], V(0.08f * k, 0.08f * k, 0.08f * k), dark, soft: 0.02f);
            Flame(b, tail, tp[^1] + V(0.01f, 0.03f, 0.02f) * k, 0.5f * k);
        }
        else b.PaintEll(tail, tp[^1], V(0.08f * k, 0.09f * k, 0.08f * k), white, soft: 0.012f);
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Veverel line

    /// <summary>
    /// Veverel and Veverar: a red squirrel on its hind legs, long tufted ears, a cream belly and puffed cheeks, a
    /// hazelnut held in both paws; the grown one bigger, its tail a plume of fir and golden beech leaves and a hollow
    /// in its chest fur like a tree's knot, stuffed with nuts.
    /// </summary>
    private static PokeBuilder C5Squirrel(bool big)
    {
        float k = big ? 1.9f : 1f;
        var b = new PokeBuilder(big ? "Veverar" : "Veverel", big ? 0.75f : 0.45f, BodyPlan.Biped, V(0, 0.18f * k, 0)) { Coat = Fur };
        var red = Rgb(212, 96, 52);
        var deep = Rgb(150, 58, 40);
        var cream = Rgb(246, 222, 172);
        var nut = Rgb(150, 100, 56);
        var fir = Rgb(52, 98, 60);
        var beech = Rgb(220, 172, 70);
        // Hind legs folded, long feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s * k, 0.1f * k, -0.02f * k));
            b.Ell(leg, V(0.07f * s * k, 0.09f * k, -0.02f * k), V(0.05f * k, 0.07f * k, 0.07f * k), red);
            b.Ell(leg, V(0.075f * s * k, 0.018f * k, 0.04f * k), V(0.03f * k, 0.018f * k, 0.065f * k), red);
            Digits(b, leg, V(0.075f * s * k, 0.012f * k, 0.09f * k), V(0, 0, 1f), V(0.4f, 0, 0), 0.025f * k, 0.007f * k, deep);
        });
        // The body, a cream belly
        b.Ell(Body, V(0, 0.18f * k, 0), V(0.075f * k, 0.11f * k, 0.07f * k), red);
        b.PaintEll(Body, V(0, 0.16f * k, 0.05f * k), V(0.05f * k, 0.09f * k, 0.04f * k), cream);
        if (big)
        {
            // The hollow in its chest, nuts and acorns crammed in it
            b.Cut(Body, V(0, 0.22f * k, 0.072f * k), V(0.022f * k, 0.035f * k, 0.016f * k));
            b.PaintEll(Body, V(0, 0.22f * k, 0.06f * k), V(0.032f * k, 0.045f * k, 0.02f * k), deep, soft: 0.006f);
            foreach (var (x, y) in new[] { (-0.01f, 0.21f), (0.01f, 0.225f), (0f, 0.24f), (0.012f, 0.205f), (-0.012f, 0.232f) })
                b.Ell(Body, V(x * k, y * k, 0.062f * k), V(0.008f * k, 0.008f * k, 0.008f * k), nut, mat: Shell, blend: 0.003f);
        }
        // Forepaws: holding the hazelnut to its mouth, or held up guarding its store
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s * k, 0.24f * k, 0.03f * k));
            var hand = big ? V(0.055f * s * k, 0.16f * k, 0.08f * k) : V(0.03f * s * k, 0.25f * k, 0.09f * k);
            b.Limb(arm, V(0.05f * s * k, 0.25f * k, 0.03f * k), hand, 0.018f * k, 0.014f * k, red);
            b.Ell(arm, hand + V(0, 0, 0.01f * k), V(0.016f * k, 0.014f * k, 0.014f * k), red);
        });
        if (!big)
        {
            b.Ell(Body, V(0, 0.255f * k, 0.105f * k), V(0.035f * k, 0.032f * k, 0.03f * k), nut, mat: Shell, blend: 0.006f);
            b.PaintEll(Body, V(0, 0.27f * k, 0.1f * k), V(0.036f * k, 0.014f * k, 0.035f * k), Rgb(110, 72, 40), soft: 0.004f);
        }
        // The head: puffed cream cheeks, big eyes, tufted ears
        int head = b.Head(V(0, 0.29f * k, 0.02f * k));
        var c = V(0, 0.34f * k, 0.03f * k);
        var r = V(0.065f * k, 0.06f * k, 0.06f * k);
        b.Ell(head, c, r, red);
        PokeBuilder.Both(s => b.Ell(head, c + V(0.045f * s * k, -0.025f * k, 0.03f * k), V((big ? 0.03f : 0.038f) * k, 0.03f * k, 0.03f * k), cream, blend: 0.015f));
        b.Ell(head, c + V(0, -0.015f * k, 0.06f * k), V(0.012f * k, 0.01f * k, 0.008f * k), deep, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s * k, c.Y + 0.012f * k);
            b.Eye(head, at, Outward(c, r, at), 0.02f * k, Rgb(60, 36, 24));
            int ear = b.Ear(head, s, c + V(0.03f * s * k, 0.045f * k, -0.01f * k));
            var root = c + V(0.03f * s * k, 0.045f * k, -0.01f * k);
            var tip = root + V(0.02f * s * k, 0.12f * k, -0.01f * k);
            CatEar(b, ear, root, tip, 0.022f * k, red, Rgb(220, 140, 110));
            var tuft = Vector3.Lerp(root, tip, 0.8f);
            foreach (var d in new[] { V(0, 1f, 0), V(0.4f * s, 0.9f, 0), V(-0.3f * s, 0.95f, 0) })
                b.Spike(ear, tuft, tip + d * 0.04f * k, 0.009f * k, Rgb(70, 40, 30));
        });
        // The tail: a plume rising behind and curling over at its top
        int tail = b.Tail(V(0, 0.1f * k, -0.06f * k));
        var tp = Smooth(3, V(0, 0.1f, -0.04f) * k, V(0, 0.12f, -0.16f) * k, V(0, 0.26f, -0.2f) * k, V(0, 0.4f, -0.16f) * k, V(0, 0.44f, -0.08f) * k, V(0, 0.4f, -0.04f) * k);
        if (big)
        {
            b.Tube(tail, tp, 0.04f * k, 0.035f * k, deep, blend: 0.015f);
            for (int i = 2; i < tp.Length - 1; i += 2)
            {
                var p = tp[i];
                var along = Vector3.Normalize(tp[i + 1] - tp[i - 1]);
                foreach (var side in new[] { V(1f, 0, 0), V(-1f, 0, 0), V(0, 0, -1f), V(0.7f, 0, -0.7f), V(-0.7f, 0, -0.7f) })
                {
                    var outv = Vector3.Normalize(side - along * Vector3.Dot(side, along));
                    b.Spike(tail, p + outv * 0.02f * k, p + outv * 0.085f * k + along * 0.02f * k, 0.012f * k, fir, 0.5f, Leaf);
                }
                if (i % 4 == 2)
                    Petal(b, tail, p + V(0.025f, 0, -0.01f) * k, p + V(0.07f, 0.02f, -0.03f) * k, 0.018f * k, beech, Leaf);
                else
                    Petal(b, tail, p + V(-0.025f, 0, -0.01f) * k, p + V(-0.07f, 0.03f, -0.02f) * k, 0.018f * k, beech, Leaf);
            }
        }
        else
        {
            b.Tube(tail, tp, 0.05f * k, 0.055f * k, red, blend: 0.02f);
            b.PaintEll(tail, V(0, 0.3f, -0.2f) * k, V(0.06f * k, 0.06f * k, 0.03f * k), deep, soft: 0.02f);
        }
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Cocomunte

    /// <summary>Cocomunte: a capercaillie cock in full display: slate plumage, a green sheen on its breast, a red brow, its broad tail fanned and its beak open in song, wings drooped.</summary>
    private static PokeBuilder C5Cocomunte()
    {
        var b = new PokeBuilder("Cocomunte", 0.78f, BodyPlan.Bird, V(0, 0.38f, 0)) { Coat = Fur };
        var slate = Rgb(66, 74, 90);
        var dark = Rgb(42, 46, 58);
        var green = Rgb(40, 150, 96);
        var red = Rgb(226, 60, 50);
        var beak = Rgb(196, 196, 176);
        var foot = Rgb(70, 70, 76);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.26f, -0.02f));
            b.Limb(leg, V(0.06f * s, 0.28f, -0.02f), V(0.07f * s, 0.06f, 0.0f), 0.032f, 0.022f, dark);
            Digits(b, leg, V(0.07f * s, 0.03f, 0.01f), V(0, -0.2f, 1f), V(0.55f, 0, 0), 0.07f, 0.011f, foot, Shell);
            b.Limb(leg, V(0.07f * s, 0.03f, 0.01f), V(0.07f * s, 0.015f, -0.05f), 0.01f, 0.008f, foot, Shell);
        });
        // The body, chest thrown out
        b.Ell(Body, V(0, 0.38f, -0.02f), V(0.13f, 0.15f, 0.17f), slate, V(-20f, 0, 0));
        b.Ell(Body, V(0, 0.44f, 0.07f), V(0.11f, 0.13f, 0.1f), slate);
        b.PaintEll(Body, V(0, 0.44f, 0.15f), V(0.08f, 0.1f, 0.06f), green);
        // The fanned tail standing up behind
        int tail = b.Tail(V(0, 0.4f, -0.16f));
        for (int i = 0; i < 9; i++)
        {
            float a = (i - 4f) * 0.3f;
            var dir = V(MathF.Sin(a), MathF.Cos(a) * 0.95f, -0.3f);
            var root = V(0, 0.4f, -0.16f);
            Blade(b, tail, root, root + dir * 0.36f, 0.06f, dark, V(0, 0.25f, -1f), 0.25f);
            b.PaintEll(tail, root + dir * 0.32f + V(0, 0, -0.005f), V(0.03f, 0.02f, 0.03f), Rgb(208, 214, 226), Euler(dir), 0.006f);
        }
        b.Ell(tail, V(0, 0.42f, -0.17f), V(0.07f, 0.07f, 0.04f), dark, blend: 0.02f);
        // Drooped wings, their tips trailing
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.11f * s, 0.46f, 0.0f));
            b.Ell(wing, V(0.13f * s, 0.32f, -0.04f), V(0.04f, 0.17f, 0.12f), slate, V(-15f, 0, 10f * s));
            b.Spike(wing, V(0.14f * s, 0.2f, -0.06f), V(0.16f * s, 0.06f, -0.12f), 0.06f, slate, 0.35f);
            b.PaintEll(wing, V(0.16f * s, 0.32f, -0.02f), V(0.02f, 0.06f, 0.06f), dark, soft: 0.01f);
        });
        // The head raised, beak open in song, the red brow over each eye
        int head = b.Head(V(0, 0.55f, 0.08f));
        var c = V(0, 0.64f, 0.1f);
        var r = V(0.065f, 0.07f, 0.07f);
        b.Limb(head, V(0, 0.52f, 0.07f), c, 0.065f, 0.055f, slate);
        b.Ell(head, c, r, slate);
        b.Spike(head, c + V(0, 0.005f, 0.05f), c + V(0, 0.04f, 0.13f), 0.028f, beak, 0.7f, Shell);
        b.Spike(head, c + V(0, -0.02f, 0.05f), c + V(0, -0.04f, 0.11f), 0.022f, beak, 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(60, 40, 30));
            b.Ell(head, at + V(0.008f * s, 0.022f, -0.01f), V(0.022f, 0.012f, 0.028f), red, V(0, 0, -15f * s), blend: 0.008f);
        });
        foreach (var (y, z) in new[] { (-0.04f, 0.02f), (-0.05f, -0.01f) })
            b.Spike(head, c + V(0, y, z), c + V(0, y - 0.04f, z + 0.02f), 0.018f, dark, 0.5f);
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Capriorel line

    /// <summary>
    /// Capriorel and Caprioara: a roe fawn dotted with cream spots on long wobbly legs, huge ears and a sprout between
    /// them; the doe slender, her ears and neck wreathed in bluebells, anemones and fern, a white rump and one foreleg lifted.
    /// </summary>
    private static PokeBuilder C5Roe(bool big)
    {
        float k = big ? 1.5f : 1f;
        var b = new PokeBuilder(big ? "Caprioara" : "Capriorel", big ? 0.85f : 0.6f, BodyPlan.Quadruped, V(0, 0.36f * k, 0)) { Coat = Fur };
        var coat = big ? Rgb(176, 92, 58) : Rgb(170, 94, 56);
        var cream = Rgb(240, 214, 160);
        var white = Rgb(246, 240, 232);
        var hoof = Rgb(58, 44, 40);
        var leaf = Rgb(98, 150, 72);
        var bluebell = Rgb(110, 140, 220);
        var anemone = Rgb(250, 224, 226);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.13f, false) })
            PokeBuilder.Both(s =>
            {
                float x = 0.045f * s * k, zz = z * 0.92f * k;
                bool lifted = big && front && s > 0;
                int leg = b.Leg(s, V(x, 0.32f * k, zz), front);
                var knee = lifted ? V(x, 0.2f * k, zz + 0.08f * k) : V(x, 0.18f * k, zz + (front ? 0.0f : -0.02f) * k);
                var foot = lifted ? V(x, 0.14f * k, zz + 0.02f * k) : V(x, 0.03f * k, zz);
                b.Limb(leg, V(x, 0.34f * k, zz), knee, 0.03f * k, 0.02f * k, coat);
                b.Limb(leg, knee, foot, 0.02f * k, 0.016f * k, coat);
                b.Ell(leg, foot + V(0, -0.012f * k, 0.008f * k), V(0.018f * k, 0.018f * k, 0.022f * k), hoof, mat: Shell, blend: 0.006f);
            });
        b.Ell(Body, V(0, 0.36f * k, 0), V(0.08f * k, 0.08f * k, 0.17f * k), coat);
        b.PaintEll(Body, V(0, 0.3f * k, 0.01f * k), V(0.06f * k, 0.03f * k, 0.13f * k), cream);
        if (big) b.PaintEll(Body, V(0, 0.38f * k, -0.16f * k), V(0.06f * k, 0.06f * k, 0.03f * k), white);
        else
            foreach (var (x, y, z) in new[] { (0.5f, 0.7f, 0.45f), (-0.5f, 0.7f, 0.35f), (0.6f, 0.55f, -0.1f), (-0.6f, 0.6f, -0.2f), (0.4f, 0.8f, -0.55f),
                (-0.3f, 0.9f, 0.0f), (0.25f, 0.95f, 0.15f), (-0.45f, 0.6f, -0.6f), (0.8f, 0.2f, 0.25f), (-0.8f, 0.2f, 0.1f) })
                b.PaintEll(Body, Carp8At(V(0, 0.36f * k, 0), V(0.08f * k, 0.08f * k, 0.17f * k), V(x, y, z)), V(0.012f * k, 0.01f * k, 0.014f * k), cream, soft: 0.004f);
        int tail = b.Tail(V(0, 0.4f * k, -0.16f * k));
        b.Spike(tail, V(0, 0.4f * k, -0.15f * k), V(0, 0.44f * k, -0.22f * k), 0.025f * k, coat);
        // The neck up to the head, the big ears
        int head = b.Head(V(0, 0.42f * k, 0.13f * k));
        var c = V(0, 0.6f * k, 0.18f * k);
        var r = V(0.05f * k, 0.05f * k, 0.055f * k);
        b.Limb(head, V(0, 0.38f * k, 0.12f * k), c + V(0, -0.02f * k, -0.01f * k), 0.04f * k, 0.03f * k, coat);
        b.PaintEll(head, V(0, 0.48f * k, 0.17f * k), V(0.03f * k, 0.06f * k, 0.03f * k), cream);
        b.Ell(head, c, r, coat);
        b.Limb(head, c + V(0, -0.005f * k, 0.03f * k), c + V(0, -0.02f * k, 0.09f * k), 0.03f * k, 0.018f * k, coat);
        b.Ell(head, c + V(0, -0.02f * k, 0.1f * k), V(0.014f * k, 0.011f * k, 0.01f * k), hoof, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s * k, c.Y + 0.008f * k);
            b.Eye(head, at, Outward(c, r, at), 0.016f * k, Rgb(56, 36, 28));
            int ear = b.Ear(head, s, c + V(0.03f * s * k, 0.035f * k, -0.02f * k));
            var root = c + V(0.03f * s * k, 0.035f * k, -0.02f * k);
            var tip = root + V(0.09f * s * k, 0.07f * k, -0.01f * k);
            b.Ell(ear, (root + tip) / 2f, V(0.03f * k, Vector3.Distance(root, tip) * 0.55f, 0.012f * k), coat, Euler(tip - root));
            b.PaintEll(ear, (root + tip) / 2f + V(0, 0, 0.008f * k), V(0.02f * k, Vector3.Distance(root, tip) * 0.42f, 0.01f * k), Rgb(236, 196, 176), Euler(tip - root));
            if (big)
            {
                // Bluebells and an anemone caught at the ear's root
                b.Ell(ear, root + V(0.015f * s * k, 0.0f, 0.012f * k), V(0.014f * k, 0.012f * k, 0.01f * k), anemone, mat: Leaf, blend: 0.004f);
                b.Ell(ear, root + V(0.03f * s * k, -0.012f * k, 0.004f * k), V(0.008f * k, 0.011f * k, 0.008f * k), bluebell, mat: Leaf, blend: 0.004f);
                b.Spike(ear, root + V(0.01f * s * k, 0.005f * k, -0.005f * k), root + V(0.03f * s * k, 0.05f * k, -0.01f * k), 0.008f * k, leaf, 0.5f, Leaf);
            }
        });
        if (big)
        {
            // A garland round the neck
            foreach (var (a, col) in new[] { (-1.2f, anemone), (-0.6f, bluebell), (0f, anemone), (0.6f, bluebell), (1.2f, anemone), (1.8f, leaf), (-1.8f, leaf) })
            {
                var p = V(0, 0.45f * k, 0.15f * k) + V(MathF.Sin(a) * 0.035f * k, 0, MathF.Cos(a) * 0.03f * k);
                b.Ell(head, p, V(0.012f * k, 0.01f * k, 0.012f * k), col, mat: Leaf, blend: 0.004f);
            }
            b.Spike(head, c + V(0, 0.04f * k, -0.01f * k), c + V(0, 0.1f * k, -0.03f * k), 0.008f * k, leaf, 0.5f, Leaf);
        }
        else
        {
            // A tiny sprout between its ears
            b.Limb(head, c + V(0, 0.04f * k, -0.01f * k), c + V(0, 0.08f * k, -0.01f * k), 0.006f * k, 0.005f * k, leaf, Leaf, 0.004f);
            PokeBuilder.Both(s => Petal(b, head, c + V(0, 0.078f * k, -0.01f * k), c + V(0.03f * s * k, 0.1f * k, -0.01f * k), 0.01f * k, leaf, Leaf));
        }
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Ghindel line

    /// <summary>Ghindel: a tiny glossy acorn with a rough brown cap tipped over one eye, two green leaves sprouting from its top and two leaf-bud nubs at its sides.</summary>
    private static PokeBuilder C5Ghindel()
    {
        var b = new PokeBuilder("Ghindel", 0.4f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Shell };
        var nut = Rgb(214, 158, 72);
        var cap = Rgb(126, 92, 64);
        var c = V(0, 0.16f, 0);
        var r = V(0.1f, 0.12f, 0.1f);
        b.Ell(Body, c, r, nut, mat: Shell);
        b.Spike(Body, V(0, 0.08f, 0), V(0, 0.0f, 0.01f), 0.07f, nut, mat: Shell, blend: 0.03f);
        b.PaintEll(Body, V(-0.04f, 0.2f, 0.08f), V(0.015f, 0.04f, 0.01f), Rgb(240, 204, 130), V(0, 0, 15f), 0.008f);
        // The cap, tipped down over its right eye, and the leaves on it
        int top = b.Part("cap", Body, V(0, 0.24f, 0), PokeRole.Leaf);
        b.Ell(top, V(0.01f, 0.25f, 0.0f), V(0.12f, 0.06f, 0.115f), cap, V(0, 0, -12f), Shell, 0.01f);
        foreach (var (x, z) in new[] { (0.05f, 0.06f), (-0.05f, 0.05f), (0.08f, -0.03f), (-0.07f, -0.05f), (0f, -0.08f), (0.02f, 0.02f) })
            b.PaintEll(top, V(x, 0.29f, z), V(0.02f, 0.012f, 0.02f), Rgb(92, 66, 48), soft: 0.005f);
        b.Limb(top, V(0.01f, 0.3f, 0), V(0.015f, 0.34f, 0), 0.008f, 0.007f, Rgb(110, 150, 70), Leaf, 0.004f);
        Petal(b, top, V(0.015f, 0.335f, 0), V(-0.04f, 0.39f, 0.01f), 0.022f, C5OakLeaf, Leaf);
        Petal(b, top, V(0.015f, 0.335f, 0), V(0.075f, 0.37f, 0.0f), 0.022f, C5OakLeaf, Leaf);
        // Two leaf-bud nubs for arms
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.12f, 0));
            b.Limb(arm, V(0.08f * s, 0.12f, 0), V(0.12f * s, 0.11f, 0.01f), 0.015f, 0.012f, nut, Shell);
            Petal(b, arm, V(0.115f * s, 0.11f, 0.01f), V(0.15f * s, 0.12f, 0.01f), 0.012f, Rgb(176, 134, 80), Leaf);
        });
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.155f);
            b.Eye(Body, at, Outward(c, r, at), 0.017f, Rgb(50, 34, 26));
        });
        b.Mark(Body, On(c, r, 0, 0.12f), V(0, -0.2f, 1f), 0.009f, 0.006f, Rgb(160, 70, 60), MarkShape.Disc);
        b.Ground();
        return b;
    }

    /// <summary>An oak leaf of several lobes from <paramref name="from"/> to <paramref name="to"/>: a petal with two smaller ones either side of it.</summary>
    private static void C5OakLeafAt(PokeBuilder b, int bone, Vector3 from, Vector3 to, float width, Color color)
    {
        Petal(b, bone, from, to, width, color, Leaf);
        var d = to - from;
        var side = Vector3.Normalize(Vector3.Cross(d, V(0, 1f, 0)) + V(0.001f, 0, 0));
        foreach (float t in new[] { 0.35f, 0.65f })
            PokeBuilder.Both(s => Petal(b, bone, from + d * t, from + d * t + side * s * width * 1.4f + d * 0.12f, width * 0.6f, color, Leaf));
    }

    /// <summary>Stejarel: an oak sapling: a slim ridged trunk, a bark face with a determined look, an acorn-cap band under a crown of lobed leaves, two branch arms (an acorn dangling from one) and stumpy roots for feet.</summary>
    private static PokeBuilder C5Stejarel()
    {
        var b = new PokeBuilder("Stejarel", 0.72f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Leaf };
        var face = Rgb(222, 178, 104);
        var dark = Rgb(120, 86, 48);
        var cap = Rgb(110, 80, 58);
        // Roots bunched for feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.12f, 0));
            foreach (var (dx, dz) in new[] { (0.07f, 0.06f), (0.09f, -0.03f), (0.03f, 0.09f) })
                b.Limb(leg, V(0.04f * s, 0.12f, 0), V((0.04f + dx) * s, 0.015f, dz), 0.035f, 0.016f, C5Bark, SurfaceMaterial.Leather);
        });
        // The trunk
        b.Limb(Body, V(0, 0.08f, 0), V(0, 0.56f, 0), 0.085f, 0.065f, C5Bark, SurfaceMaterial.Leather, 0.03f);
        foreach (float x in new[] { -0.05f, -0.02f, 0.02f, 0.05f })
            b.PaintEll(Body, V(x, 0.2f, 0.07f), V(0.004f, 0.1f, 0.03f), dark, soft: 0.004f);
        // The face on its bark, a determined look
        var fc = V(0, 0.44f, 0.04f);
        var fr = V(0.065f, 0.09f, 0.04f);
        b.PaintEll(Body, fc + V(0, 0, 0.03f), V(0.05f, 0.075f, 0.04f), face, soft: 0.006f);
        PokeBuilder.Both(s => b.Eye(Body, V(0.026f * s, 0.46f, 0.077f), V(0.25f * s, 0.05f, 1f), 0.017f, Rgb(50, 34, 24), glare: true));
        b.Mark(Body, V(0, 0.415f, 0.078f), V(0, 0, 1f), 0.014f, 0.006f, Rgb(80, 50, 30), MarkShape.Smile);
        // The acorn-cap band and the crown above it
        b.Torus(Body, V(0, 0.54f, 0), 0.07f, 0.03f, cap, mat: Shell, blend: 0.01f);
        foreach (var a in new[] { 0f, 1.2f, 2.4f, 3.6f, 4.8f })
            b.PaintEll(Body, V(MathF.Sin(a) * 0.09f, 0.56f, MathF.Cos(a) * 0.09f), V(0.018f, 0.012f, 0.018f), Rgb(84, 60, 44), soft: 0.005f);
        int crown = b.Part("crown", Body, V(0, 0.6f, 0), PokeRole.Leaf);
        foreach (var (dx, dz) in new[] { (0.03f, 0.0f), (-0.03f, 0.01f), (0f, -0.03f) })
            b.Limb(crown, V(dx * 0.5f, 0.54f, dz * 0.5f), V(dx * 2.5f, 0.66f, dz * 2.5f), 0.02f, 0.014f, C5Bark, SurfaceMaterial.Leather, 0.008f);
        b.Ell(crown, V(0, 0.72f, -0.01f), V(0.15f, 0.08f, 0.13f), Rgb(80, 140, 52), mat: Leaf, blend: 0.03f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var from = V(MathF.Sin(a) * 0.08f, 0.71f + 0.02f * (i % 2), MathF.Cos(a) * 0.07f - 0.01f);
            C5OakLeafAt(b, crown, from, from + V(MathF.Sin(a) * 0.12f, 0.06f + 0.03f * (i % 3), MathF.Cos(a) * 0.1f), 0.035f, C5OakLeaf);
        }
        // Branch arms with leaves; an acorn dangles from the right
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.36f, 0));
            var elbow = V(0.16f * s, 0.38f, 0.02f);
            var hand = V(0.24f * s, 0.44f, 0.02f);
            b.Limb(arm, V(0.05f * s, 0.34f, 0), elbow, 0.024f, 0.018f, C5Bark, SurfaceMaterial.Leather);
            b.Limb(arm, elbow, hand, 0.018f, 0.012f, C5Bark, SurfaceMaterial.Leather);
            b.Limb(arm, elbow, V(0.22f * s, 0.32f, 0.03f), 0.014f, 0.01f, C5Bark, SurfaceMaterial.Leather);
            Petal(b, arm, hand, hand + V(0.04f * s, 0.05f, 0), 0.02f, C5OakLeaf, Leaf);
            Petal(b, arm, V(0.22f * s, 0.32f, 0.03f), V(0.27f * s, 0.3f, 0.03f), 0.018f, C5OakLeaf, Leaf);
            if (s > 0)
            {
                b.Limb(arm, V(0.2f * s, 0.41f, 0.02f), V(0.2f * s, 0.35f, 0.03f), 0.005f, 0.005f, dark, SurfaceMaterial.Leather, 0.003f);
                b.Ell(arm, V(0.2f * s, 0.33f, 0.03f), V(0.022f, 0.028f, 0.022f), Rgb(214, 158, 72), mat: Shell, blend: 0.006f);
                b.Ell(arm, V(0.2f * s, 0.355f, 0.03f), V(0.026f, 0.014f, 0.026f), cap, mat: Shell, blend: 0.004f);
            }
        });
        b.Ground();
        return b;
    }

    /// <summary>Stejaron: the ancient oak giant: bark like riveted iron plates, acorns set along its limbs like rivets, a wide dark crown over a bearded bark face with dull amber eyes, its trunk legs rooted deep.</summary>
    private static PokeBuilder C5Stejaron()
    {
        var b = new PokeBuilder("Stejaron", 0.97f, BodyPlan.Biped, V(0, 0.9f, 0)) { Coat = SurfaceMaterial.Leather };
        var bark = Rgb(92, 74, 60);
        var barkDark = Rgb(66, 54, 46);
        var steel = Rgb(150, 154, 162);
        var crownLeaf = Rgb(58, 108, 60);
        var acorn = Rgb(186, 136, 70);
        var acornCap = Rgb(104, 78, 56);
        void Acorn(int bone, Vector3 at, Vector3 outward)
        {
            var o = Vector3.Normalize(outward);
            b.Ell(bone, at + o * 0.012f, V(0.022f, 0.028f, 0.022f), acorn, Euler(o), Shell, 0.006f);
            b.Ell(bone, at + o * 0.03f, V(0.025f, 0.014f, 0.025f), acornCap, Euler(o), Shell, 0.004f);
        }
        void Rivets(int bone, Vector3 a, Vector3 b2, Vector3 outward, int n)
        {
            for (int i = 0; i < n; i++)
                b.Ell(bone, Vector3.Lerp(a, b2, (i + 0.5f) / n) + Vector3.Normalize(outward) * 0.004f, V(0.011f, 0.011f, 0.011f), Rgb(190, 192, 198), mat: Metal, blend: 0.003f);
        }
        // Trunk legs spreading into roots, steel greaves on the shins
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.17f * s, 0.6f, 0));
            b.Limb(leg, V(0.16f * s, 0.62f, 0), V(0.2f * s, 0.12f, 0.02f), 0.13f, 0.12f, bark, SurfaceMaterial.Leather);
            foreach (var (dx, dz) in new[] { (0.12f, 0.1f), (0.14f, -0.08f), (0.02f, 0.16f), (-0.04f, 0.13f), (0.06f, -0.14f) })
                b.Limb(leg, V(0.2f * s, 0.16f, 0.02f), V((0.2f + dx) * s, 0.02f, dz), 0.07f, 0.025f, bark, SurfaceMaterial.Leather);
            b.Box(leg, V(0.2f * s, 0.32f, 0.11f), V(0.09f, 0.11f, 0.03f), 0.02f, steel, V(-6f, 0, 0), Metal, 0.01f);
            Rivets(leg, V(0.13f * s, 0.42f, 0.14f), V(0.27f * s, 0.42f, 0.14f), V(0, 0, 1f), 4);
            Acorn(leg, V(0.31f * s, 0.46f, 0.02f), V(s, 0.2f, 0.2f));
        });
        // The torso of bark, a steel breastplate and a belt of plates
        b.Ell(Body, V(0, 0.92f, 0), V(0.32f, 0.36f, 0.24f), bark);
        b.Ell(Body, V(0, 1.12f, -0.02f), V(0.38f, 0.2f, 0.26f), bark, blend: 0.05f);
        foreach (float x in new[] { -0.15f, -0.05f, 0.05f, 0.15f })
            b.PaintEll(Body, V(x, 0.86f, 0.2f), V(0.006f, 0.14f, 0.05f), barkDark, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            b.Box(Body, V(0.13f * s, 1.12f, 0.22f), V(0.13f, 0.09f, 0.03f), 0.02f, steel, V(-15f, 10f * s, 0), Metal, 0.012f);
            Rivets(Body, V(0.03f * s, 1.2f, 0.25f), V(0.24f * s, 1.2f, 0.21f), V(0, 0.3f, 1f), 4);
            b.Box(Body, V(0.13f * s, 0.68f, 0.21f), V(0.12f, 0.05f, 0.03f), 0.015f, steel, V(5f, 12f * s, 0), Metal, 0.012f);
            Rivets(Body, V(0.04f * s, 0.7f, 0.25f), V(0.22f * s, 0.7f, 0.22f), V(0, 0, 1f), 3);
            Acorn(Body, V(0.32f * s, 1.02f, 0.1f), V(s, 0.1f, 0.3f));
            Acorn(Body, V(0.3f * s, 0.85f, 0.12f), V(s, 0, 0.4f));
        });
        // Long arms of branch, steel bands, clawed root hands, a leafy twig at each shoulder
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.36f * s, 1.14f, 0));
            var elbow = V(0.5f * s, 0.82f, 0.04f);
            var wrist = V(0.56f * s, 0.5f, 0.1f);
            b.Limb(arm, V(0.36f * s, 1.16f, 0), elbow, 0.12f, 0.1f, bark, SurfaceMaterial.Leather);
            b.Limb(arm, elbow, wrist, 0.11f, 0.1f, bark, SurfaceMaterial.Leather);
            b.Box(arm, V(0.53f * s, 0.66f, 0.08f), V(0.11f, 0.05f, 0.11f), 0.02f, steel, mat: Metal, blend: 0.01f);
            Rivets(arm, V(0.44f * s, 0.66f, 0.19f), V(0.62f * s, 0.66f, 0.19f), V(0, 0, 1f), 3);
            foreach (var (t, y) in new[] { (0.3f, 0.02f), (0.7f, -0.02f) })
                Acorn(arm, Vector3.Lerp(V(0.36f * s, 1.16f, 0), elbow, t) + V(0.1f * s, y, 0), V(s, 0.2f, 0.1f));
            Acorn(arm, Vector3.Lerp(elbow, wrist, 0.5f) + V(0.1f * s, 0, 0), V(s, 0, 0.2f));
            foreach (var (dx, dz) in new[] { (0.04f, 0.1f), (-0.04f, 0.12f), (0.0f, 0.14f), (0.07f, 0.06f) })
                b.Tube(arm, Smooth(2, wrist, wrist + V(dx * s, -0.12f, dz), wrist + V(dx * 1.3f * s, -0.24f, dz + 0.04f)), 0.035f, 0.012f, bark, SurfaceMaterial.Leather);
            var twig = V(0.42f * s, 1.26f, -0.06f);
            b.Limb(arm, V(0.38f * s, 1.18f, -0.04f), twig + V(0.14f * s, 0.22f, -0.02f), 0.035f, 0.016f, bark, SurfaceMaterial.Leather);
            C5OakLeafAt(b, arm, twig + V(0.13f * s, 0.2f, -0.02f), twig + V(0.25f * s, 0.3f, -0.02f), 0.05f, crownLeaf);
        });
        // The head: a bearded bark face under an acorn-cap of hair, dull amber eyes
        int head = b.Head(V(0, 1.24f, 0.08f));
        var c = V(0, 1.36f, 0.12f);
        var r = V(0.14f, 0.15f, 0.13f);
        b.Ell(head, c, r, bark);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.055f * s, 1.39f);
            b.Ell(head, at + V(0.004f * s, 0.03f, -0.01f), V(0.06f, 0.02f, 0.035f), barkDark, V(0, 0, -14f * s), blend: 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.024f, Rgb(232, 160, 50), glare: true);
        });
        b.Ell(head, On(c, r, 0, 1.34f), V(0.03f, 0.05f, 0.035f), bark, blend: 0.012f);
        for (int i = -2; i <= 2; i++)
            b.Spike(head, c + V(i * 0.04f, -0.08f, 0.09f), c + V(i * 0.05f, -0.24f, 0.08f), 0.035f, barkDark, 0.5f, SurfaceMaterial.Leather);
        b.Ell(head, c + V(0, 0.12f, -0.02f), V(0.16f, 0.09f, 0.15f), Rgb(96, 72, 54), mat: Shell, blend: 0.02f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            b.PaintEll(head, c + V(MathF.Sin(a) * 0.13f, 0.15f, MathF.Cos(a) * 0.12f - 0.02f), V(0.025f, 0.018f, 0.025f), Rgb(72, 54, 42), soft: 0.006f);
        }
        // The crown spreading over its head and shoulders
        int crown = b.Part("crown", Body, V(0, 1.4f, -0.1f), PokeRole.Leaf);
        PokeBuilder.Both(s => b.Limb(crown, V(0.2f * s, 1.2f, -0.1f), V(0.36f * s, 1.5f, -0.16f), 0.06f, 0.035f, bark, SurfaceMaterial.Leather));
        b.Limb(crown, V(0, 1.2f, -0.12f), V(0, 1.55f, -0.18f), 0.07f, 0.04f, bark, SurfaceMaterial.Leather);
        b.Ell(crown, V(0, 1.6f, -0.18f), V(0.48f, 0.14f, 0.3f), crownLeaf, mat: Leaf, blend: 0.04f);
        PokeBuilder.Both(s => b.Ell(crown, V(0.4f * s, 1.52f, -0.16f), V(0.2f, 0.12f, 0.22f), crownLeaf, mat: Leaf, blend: 0.04f));
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f;
            var from = V(MathF.Sin(a) * 0.4f, 1.58f + 0.03f * (i % 3), MathF.Cos(a) * 0.24f - 0.18f);
            C5OakLeafAt(b, crown, from, from + V(MathF.Sin(a) * 0.16f, 0.06f, MathF.Cos(a) * 0.1f), 0.055f, i % 2 == 0 ? crownLeaf : Rgb(72, 124, 66));
        }
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Palariuta line

    /// <summary>The fly agaric's cap: a scarlet dome flaring over its rim, white spots on it and pale gills under it.</summary>
    private static void C5AgaricCap(PokeBuilder b, int bone, Vector3 c, Vector3 r, Vector3 tilt, int spots)
    {
        b.Ell(bone, c, r, C5Agaric, tilt, Shell);
        b.Cut(bone, c + V(0, -r.Y * 0.95f, 0), V(r.X * 0.86f, r.Y * 0.55f, r.Z * 0.86f), tilt);
        b.PaintEll(bone, c + V(0, -r.Y * 0.45f, 0), V(r.X * 0.95f, r.Y * 0.32f, r.Z * 0.95f), C5Gill, tilt, 0.01f);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < spots; i++)
        {
            float y = 1f - 0.8f * (i + 0.5f) / spots, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            float sz = (i % 3 == 0 ? 0.13f : 0.09f) * r.X;
            b.PaintEll(bone, c + n * r, V(sz, sz * 0.7f, sz), Rgb(246, 238, 226), tilt, 0.004f);
        }
    }

    /// <summary>Palariuta: a small fly agaric half sunk in moss, a round red cap dotted white over a short pale stalk, two bright eyes peering out of the shadow under its rim.</summary>
    private static PokeBuilder C5Palariuta()
    {
        var b = new PokeBuilder("Palariuta", 0.45f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Leaf };
        var stalk = Rgb(240, 226, 196);
        var shade = Rgb(52, 34, 36);
        // The cap first, so the hollow under it is carved out of it alone
        int cap = b.Head(V(0, 0.26f, 0));
        C5AgaricCap(b, cap, V(0, 0.32f, 0), V(0.18f, 0.12f, 0.17f), default, 11);
        b.Limb(cap, V(0, 0.22f, 0), V(0, 0.3f, 0), 0.07f, 0.06f, stalk, Leaf);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.05f, 0));
            b.Ell(leg, V(0.055f * s, 0.025f, 0.02f), V(0.04f, 0.025f, 0.045f), stalk, mat: Leaf);
        });
        var sc = V(0, 0.15f, 0);
        var sr = V(0.1f, 0.13f, 0.09f);
        b.Ell(Body, sc, sr, stalk, mat: Leaf);
        b.PaintEll(Body, V(0, 0.205f, 0.07f), V(0.09f, 0.035f, 0.05f), shade, soft: 0.01f);
        PokeBuilder.Both(s => b.Eye(Body, On(sc, sr, 0.035f * s, 0.19f), V(0.3f * s, 0.1f, 1f), 0.02f, Rgb(232, 150, 40)));
        // The moss round its foot
        b.Torus(Body, V(0, 0.06f, 0), 0.1f, 0.03f, C5Moss, mat: Leaf, blend: 0.015f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            b.Ell(Body, V(MathF.Sin(a) * 0.12f, 0.07f + 0.01f * (i % 2), MathF.Cos(a) * 0.11f), V(0.025f, 0.022f, 0.025f), C5Moss, mat: Leaf, blend: 0.012f);
        }
        b.Ground();
        return b;
    }

    /// <summary>Muscarita: a tall fly agaric dancing: a wide white-spotted scarlet cap tilted like a hat over a dark-masked face, a frilled ring collar of moss, a slim white body in a frilled skirt, one arm raised, moss at its wrists and ankles.</summary>
    private static PokeBuilder C5Muscarita()
    {
        var b = new PokeBuilder("Muscarita", 0.78f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Leaf };
        var white = Rgb(244, 240, 232);
        var skirt = Rgb(240, 216, 214);
        var frill = Rgb(226, 222, 170);
        var mask = Rgb(48, 30, 36);
        // The cap first, wide and tilted like a dancer's hat, so the hollow under it is carved out of it alone
        int head = b.Head(V(0, 0.66f, 0));
        var c = V(0, 0.74f, 0.01f);
        var r = V(0.055f, 0.065f, 0.055f);
        C5AgaricCap(b, head, c + V(0.02f, 0.1f, -0.01f), V(0.28f, 0.09f, 0.24f), V(0, 0, -10f), 14);
        // Legs crossed in a dance step, moss boots
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.36f, 0));
            var knee = V(0.03f * s, 0.2f, 0.02f);
            var ankle = V(-0.01f * s, 0.06f, 0.01f + 0.03f * s);
            b.Limb(leg, V(0.04f * s, 0.38f, 0), knee, 0.03f, 0.024f, white);
            b.Limb(leg, knee, ankle, 0.024f, 0.018f, white);
            b.Ell(leg, ankle + V(0, -0.025f, 0.02f), V(0.025f, 0.035f, 0.04f), C5Moss, V(-30f, 0, 0), Leaf, 0.01f);
            b.Torus(leg, ankle + V(0, 0.01f, 0), 0.024f, 0.012f, C5Moss, mat: Leaf, blend: 0.006f);
        });
        // The body and its frilled skirt
        b.Ell(Body, V(0, 0.52f, 0), V(0.065f, 0.12f, 0.055f), white);
        b.Limb(Body, V(0, 0.47f, 0), V(0, 0.42f, 0), 0.06f, 0.1f, skirt, SurfaceMaterial.Cloth, 0.02f);
        b.Ell(Body, V(0, 0.38f, 0), V(0.13f, 0.035f, 0.12f), skirt, mat: SurfaceMaterial.Cloth, blend: 0.03f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            b.Ell(Body, V(MathF.Sin(a) * 0.13f, 0.35f + 0.012f * (i % 2), MathF.Cos(a) * 0.12f), V(0.035f, 0.02f, 0.035f), skirt, mat: SurfaceMaterial.Cloth, blend: 0.012f);
        }
        // The frilled ring collar, moss over cream
        b.Torus(Body, V(0, 0.62f, 0), 0.07f, 0.025f, frill, mat: SurfaceMaterial.Cloth, blend: 0.01f);
        b.Torus(Body, V(0, 0.65f, 0), 0.055f, 0.02f, C5Moss, mat: Leaf, blend: 0.01f);
        // Slim arms: one raised in the dance, one held out low
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.6f, 0));
            var elbow = s > 0 ? V(0.16f * s, 0.7f, 0.02f) : V(0.14f * s, 0.52f, 0.04f);
            var hand = s > 0 ? V(0.24f * s, 0.8f, 0.04f) : V(0.24f * s, 0.46f, 0.08f);
            b.Limb(arm, V(0.05f * s, 0.6f, 0), elbow, 0.02f, 0.017f, white);
            b.Limb(arm, elbow, hand, 0.017f, 0.014f, white);
            b.Ell(arm, hand + Vector3.Normalize(hand - elbow) * 0.02f, V(0.018f, 0.025f, 0.012f), white, Euler(hand - elbow));
            b.Torus(arm, Vector3.Lerp(elbow, hand, 0.85f), 0.018f, 0.01f, C5Moss, Euler(hand - elbow), mat: Leaf, blend: 0.005f);
        });
        // The head: a dark mask band with orange eyes, a sweet small smile
        b.Limb(head, V(0, 0.64f, 0), c, 0.03f, 0.03f, white);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, 0.015f, 0.03f), V(0.06f, 0.024f, 0.04f), mask, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.024f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(236, 150, 40), glare: true);
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.03f), V(0, -0.2f, 1f), 0.012f, 0.006f, Rgb(120, 60, 70), MarkShape.Smile);
        // The stalk's top holding the cap on
        b.Limb(head, c + V(0, 0.04f, -0.01f), c + V(0.01f, 0.09f, -0.01f), 0.035f, 0.03f, white);
        b.Ground();
        return b;
    }
}
