using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Models.StageShow;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>A prop's meshes (GPU-free): its body and its outline shell, in the prop's own unit space.</summary>
internal sealed class PropMesh
{
    public required string Id { get; init; }
    public required SdfMesh Body { get; init; }
    public required SdfMesh Shell { get; init; }

    // On the GPU (ShowPropModels.Upload)
    internal SkinnedModel? BodyModel, ShellModel;
}

/// <summary>
/// The stage show's props sculpted with the SDF kit (plan 42 · PS1; style guide, "Pokémon (version 2, G7)", "Props
/// worn"): every <c>shape</c> of <c>Data/show-props.json</c> at a unit size, in the prop's two colours (body and trim),
/// meshed through <see cref="SdfCache"/> (keyed by the model, so kept between runs). Our own designs.
/// <para>
/// A prop's space: one unit is the wearer's head's half width (<see cref="PropAnchor.Size"/>); the origin is where the
/// piece meets the body (the anchor), +Y off the surface, +Z the wearer's front. A neck piece that goes round the neck
/// (<see cref="WrapsNeck"/>) is sculpted round a ring of radius 1 about the origin, its X and Z scaled to the seam's own
/// width and depth as it is worn (<see cref="PropAnchors.Placement"/>). One bone (bone 0); every part at least two cells
/// thick.
/// </para>
/// </summary>
internal static class ShowPropModels
{
    /// <summary>The cell every prop is meshed with, in its units.</summary>
    public const float Cell = 0.025f;

    /// <summary>How far out the outline shell is meshed, in a prop's units.</summary>
    public const float ShellIso = 0.035f;

    /// <summary>Every shape the sculptor knows, in the table's order of first use.</summary>
    public static IReadOnlyList<string> Shapes { get; } = new[]
    {
        "top_hat", "paper_crown", "flower_crown", "pointed_hat", "beret", "head_bow", "tiara",
        "round_glasses", "star_glasses", "masquerade_mask", "monocle",
        "bow_tie", "scarf", "ruff", "pearl_string", "bell_collar",
        "short_cape", "paper_wings", "back_bow", "lantern",
        "folding_fan", "baton", "hand_bell", "star_wand", "parasol", "tambourine",
        "tail_ribbon", "tail_bell",
    };

    /// <summary>Whether a shape goes round the neck (sculpted round a ring of radius 1, fitted to the seam as it is worn).</summary>
    public static bool WrapsNeck(string shape) => shape is "scarf" or "ruff" or "pearl_string" or "bell_collar";

    public static bool Knows(string shape) => Array.IndexOf((string[])Shapes, shape) >= 0;

    private static readonly ConcurrentDictionary<string, Lazy<PropMesh>> Meshes = new(StringComparer.Ordinal);

    /// <summary>A prop's meshes, sculpted and meshed the first time (from the cache after the first run). GPU-free.</summary>
    public static PropMesh Mesh(ShowProp prop) =>
        Meshes.GetOrAdd(prop.Id, _ => new Lazy<PropMesh>(() =>
        {
            var sdf = Sculpt(prop);
            return new PropMesh { Id = prop.Id, Body = SdfCache.Get(sdf, Cell), Shell = SdfCache.Get(sdf, Cell * 1.25f, ShellIso) };
        })).Value;

    /// <summary>
    /// A prop sculpted (no meshing). Named by the prop's id, not its shape: <see cref="SdfCache"/> keeps one mesh to a
    /// name, and two colourways of one shape under one name would put each other's file out every run.
    /// </summary>
    public static SdfModel Sculpt(ShowProp prop)
    {
        var (br, bg, bb) = ShowPropDatabase.ColourOf(prop.Colors.Count > 0 ? prop.Colors[0] : "#FFFFFF");
        var (tr, tg, tb) = ShowPropDatabase.ColourOf(prop.Colors.Count > 1 ? prop.Colors[1] : prop.Colors.Count > 0 ? prop.Colors[0] : "#FFFFFF");
        var sdf = new SdfModel("prop " + prop.Id) { BoneCount = 1, WeightBlend = 0.03f, ColorSharpness = 0.88f };
        var s = new Sculptor(sdf, new Color(br, bg, bb, (byte)255), new Color(tr, tg, tb, (byte)255));
        s.Shape(prop.Shape, prop.Slot);
        if (sdf.Shapes.Count == 0) throw new ArgumentException($"no sculpt for the prop shape '{prop.Shape}'");
        return sdf;
    }

    // ------------------------------------------------------------------ the GPU

    /// <summary>Puts a prop on the GPU the first time it is drawn (owner "rig", as a body's). Main thread.</summary>
    public static PropMesh Upload(ShowProp prop)
    {
        var m = Mesh(prop);
        if (m.BodyModel == null)
        {
            m.BodyModel = SkinnedModel.Upload(m.Body, 1);
            m.ShellModel = SkinnedModel.Upload(m.Shell, 1);
        }
        return m;
    }

    /// <summary>Frees every prop put on the GPU (the meshes stay). Main thread; whoever leaves the stage or the dressing room calls it.</summary>
    public static void Unload()
    {
        foreach (var lazy in Meshes.Values)
        {
            if (!lazy.IsValueCreated) continue;
            var m = lazy.Value;
            m.BodyModel?.Unload();
            m.ShellModel?.Unload();
            m.BodyModel = m.ShellModel = null;
        }
    }

    private static readonly Matrix4x4[] OneBone = { Matrix4x4.Identity };

    /// <summary>
    /// Draws the pieces worn on a model posed in <see cref="PokeModel.Skin"/>, each at its slot's anchor riding the
    /// anchor's bone; a piece of a slot the body lacks is passed by. <paramref name="root"/> places the model.
    /// </summary>
    public static void DrawWorn(RenderContext context, PokeModel model, IReadOnlyList<ShowProp> props, Matrix4x4 root, CharacterPass pass)
    {
        if (props.Count == 0 || model.Skin.Length == 0) return;
        var anchors = PropAnchors.Of(model);
        foreach (var prop in props)
        {
            if (!anchors.TryGetValue(prop.Slot, out var anchor) || anchor.Bone >= model.Skin.Length) continue;
            var m = Upload(prop);
            var place = PropAnchors.Placement(anchor, prop.Shape) * model.Skin[anchor.Bone] * root;
            switch (pass)
            {
                case CharacterPass.Color:
                    m.BodyModel!.Draw(context.ToonSkinned, OneBone, place);
                    break;
                case CharacterPass.Depth:
                    m.BodyModel!.Draw(context.DepthSkinned, OneBone, place);
                    break;
                case CharacterPass.Outline:
                    context.Shaders.SetOutlineWidth(0f);
                    Rlgl.DrawRenderBatchActive();
                    Rlgl.EnableBackfaceCulling();
                    Rlgl.SetCullFace(0);
                    m.ShellModel!.Draw(context.OutlineSkinned, OneBone, place);
                    Rlgl.SetCullFace(1);
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ the sculpts (ours)

    private sealed class Sculptor
    {
        private readonly SdfModel s;
        private readonly Color body, trim;
        private const SurfaceMaterial ClothM = SurfaceMaterial.Cloth, MetalM = SurfaceMaterial.Metal, PlasticM = SurfaceMaterial.Plastic,
            LeatherM = SurfaceMaterial.Leather, GlowM = SurfaceMaterial.Glow;

        public Sculptor(SdfModel s, Color body, Color trim)
        {
            this.s = s;
            this.body = body;
            this.trim = trim;
        }

        private static Vector3 V(float x, float y, float z) => new(x, y, z);
        private static Quaternion AboutX(float a) => Quaternion.CreateFromAxisAngle(Vector3.UnitX, a);
        private static Quaternion AboutZ(float a) => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, a);
        private static Quaternion AboutY(float a) => Quaternion.CreateFromAxisAngle(Vector3.UnitY, a);

        /// <summary>A disc or ring whose axis is Z (facing the front).</summary>
        private static readonly Quaternion FacingFront = AboutX(MathF.PI / 2f);

        public void Shape(string shape, PropSlot slot)
        {
            switch (shape)
            {
                case "top_hat": TopHat(); break;
                case "paper_crown": PaperCrown(); break;
                case "flower_crown": FlowerCrown(); break;
                case "pointed_hat": PointedHat(); break;
                case "beret": Beret(); break;
                case "head_bow": Bow(V(0, 0.12f, 0), 1f, tailsBack: false); break;
                case "tiara": Tiara(); break;
                case "round_glasses": RoundGlasses(); break;
                case "star_glasses": StarGlasses(); break;
                case "masquerade_mask": Mask(); break;
                case "monocle": Monocle(); break;
                case "bow_tie": BowTie(); break;
                case "scarf": Scarf(); break;
                case "ruff": Ruff(); break;
                case "pearl_string": Pearls(); break;
                case "bell_collar": BellCollar(); break;
                case "short_cape": Cape(); break;
                case "paper_wings": Wings(); break;
                case "back_bow": Bow(V(0, 0.1f, 0), 1.15f, tailsBack: true); break;
                case "lantern": Lantern(hanging: slot == PropSlot.Hand); break;
                case "folding_fan": Fan(); break;
                case "baton": Baton(); break;
                case "hand_bell": Bell(V(0, -0.62f, 0), 1f, handle: true); break;
                case "star_wand": StarWand(); break;
                case "parasol": Parasol(); break;
                case "tambourine": Tambourine(); break;
                case "tail_ribbon": Bow(V(0, 0.05f, 0), 0.8f, tailsBack: false); break;
                case "tail_bell": TailBell(); break;
            }
        }

        // ---------------------------------------------------------- head

        private void TopHat()
        {
            s.Cylinder(V(0, -0.04f, 0), 0.78f, 0.04f, 0.03f, body, 0, mat: ClothM);
            s.Cylinder(V(0, 0.5f, 0), 0.48f, 0.52f, 0.06f, body, 0, 0.02f, ClothM);
            s.Cylinder(V(0, 0.12f, 0), 0.5f, 0.09f, 0.03f, trim, 0, mat: ClothM);
        }

        private void PaperCrown()
        {
            s.Cylinder(V(0, 0f, 0), 0.62f, 0.2f, 0.03f, body, 0, mat: MetalM);
            s.Cylinder(V(0, 0f, 0), 0.54f, 0.4f, 0.02f, body, 0, op: SdfOp.Cut);
            for (int k = 0; k < 7; k++)
            {
                float a = k * MathF.Tau / 7f;
                var at = V(MathF.Sin(a) * 0.58f, 0.18f, MathF.Cos(a) * 0.58f);
                s.Capsule(at, at + V(MathF.Sin(a) * 0.04f, 0.32f, MathF.Cos(a) * 0.04f), 0.07f, 0.035f, body, 0, 0.015f, MetalM);
                s.Sphere(at + V(MathF.Sin(a) * 0.04f, 0.34f, MathF.Cos(a) * 0.04f), 0.05f, trim, 0, mat: PlasticM);
            }
            s.Ellipsoid(V(0, 0.02f, 0.62f), V(0.1f, 0.1f, 0.04f), trim, 0, mat: PlasticM);
        }

        private void FlowerCrown()
        {
            s.Torus(V(0, -0.06f, 0), 0.6f, 0.06f, trim, 0, mat: SurfaceMaterial.Leaf);
            for (int k = 0; k < 8; k++)
            {
                float a = k * MathF.Tau / 8f + 0.2f;
                var at = V(MathF.Sin(a) * 0.6f, 0.02f, MathF.Cos(a) * 0.6f);
                s.Ellipsoid(at, V(0.15f, 0.08f, 0.15f), body, 0, 0.02f, ClothM);
                s.Sphere(at + V(0, 0.07f, 0), 0.05f, trim, 0, mat: ClothM);
            }
        }

        private void PointedHat()
        {
            s.Cylinder(V(0, -0.04f, 0), 0.82f, 0.035f, 0.03f, body, 0, mat: ClothM);
            s.Capsule(V(0, 0.05f, 0), V(0.18f, 1.3f, -0.14f), 0.48f, 0.04f, body, 0, 0.02f, ClothM);
            s.Torus(V(0, 0.12f, 0), 0.45f, 0.06f, trim, 0, mat: ClothM);
            s.Sphere(V(0.18f, 1.3f, -0.14f), 0.07f, trim, 0, mat: MetalM);
        }

        private void Beret()
        {
            s.Ellipsoid(V(0.08f, 0.1f, -0.02f), V(0.72f, 0.2f, 0.7f), body, 0, mat: ClothM, rotation: AboutZ(-0.16f));
            s.Torus(V(0, -0.02f, 0), 0.55f, 0.06f, trim, 0, mat: ClothM);
            s.Capsule(V(0.1f, 0.26f, -0.02f), V(0.1f, 0.4f, -0.02f), 0.045f, 0.035f, trim, 0, mat: ClothM);
        }

        /// <summary>A bow of two loops and a knot, its two tails falling below it or trailing back.</summary>
        private void Bow(Vector3 at, float k, bool tailsBack)
        {
            s.Ellipsoid(at + V(-0.3f, 0.02f, 0) * k, V(0.3f, 0.19f, 0.1f) * k, body, 0, 0.02f, ClothM, AboutZ(-0.3f));
            s.Ellipsoid(at + V(0.3f, 0.02f, 0) * k, V(0.3f, 0.19f, 0.1f) * k, body, 0, 0.02f, ClothM, AboutZ(0.3f));
            var end = tailsBack ? V(0.2f, -0.02f, -0.55f) : V(0.24f, -0.3f, 0.05f);
            s.Capsule(at + V(-0.06f, -0.02f, 0) * k, at + new Vector3(-end.X, end.Y, end.Z) * k, 0.06f * k, 0.045f * k, body, 0, 0.01f, ClothM);
            s.Capsule(at + V(0.06f, -0.02f, 0) * k, at + end * k, 0.06f * k, 0.045f * k, body, 0, 0.01f, ClothM);
            s.Sphere(at + V(0, 0.01f, 0.02f) * k, 0.12f * k, trim, 0, mat: ClothM);
        }

        private void Tiara()
        {
            s.Torus(V(0, -0.06f, 0.1f), 0.58f, 0.045f, body, 0, mat: MetalM);
            for (int k = -2; k <= 2; k++)
            {
                float a = k * 0.38f;
                var at = V(MathF.Sin(a) * 0.58f, -0.04f, 0.1f + MathF.Cos(a) * 0.58f);
                float h = k == 0 ? 0.36f : MathF.Abs(k) == 1 ? 0.24f : 0.15f;
                s.Capsule(at, at + V(0, h, 0.02f), 0.06f, 0.03f, body, 0, 0.015f, MetalM);
            }
            s.Sphere(V(0, 0.2f, 0.7f), 0.09f, trim, 0, mat: PlasticM);
            s.Sphere(V(-0.21f, 0.1f, 0.65f), 0.055f, trim, 0, mat: PlasticM);
            s.Sphere(V(0.21f, 0.1f, 0.65f), 0.055f, trim, 0, mat: PlasticM);
        }

        // ---------------------------------------------------------- face (origin between the eyes, +Z out of the face)

        private void RoundGlasses()
        {
            foreach (float x in new[] { -0.36f, 0.36f })
            {
                s.Torus(V(x, 0, 0.1f), 0.24f, 0.045f, body, 0, mat: PlasticM, rotation: FacingFront);
                s.Capsule(V(x * 1.66f, 0.02f, 0.08f), V(x * 2.1f, 0.04f, -0.42f), 0.035f, 0.03f, body, 0, mat: PlasticM);
            }
            s.Capsule(V(-0.13f, 0.04f, 0.12f), V(0.13f, 0.04f, 0.12f), 0.035f, 0.035f, trim, 0, mat: PlasticM);
        }

        private void StarGlasses()
        {
            foreach (float x in new[] { -0.36f, 0.36f })
            {
                var c = V(x, 0, 0.1f);
                s.Ellipsoid(c, V(0.13f, 0.13f, 0.035f), body, 0, mat: PlasticM);
                for (int k = 0; k < 5; k++)
                {
                    float a = k * MathF.Tau / 5f;
                    s.Capsule(c, c + V(MathF.Sin(a) * 0.27f, MathF.Cos(a) * 0.27f, 0), 0.09f, 0.03f, body, 0, mat: PlasticM);
                }
                s.Ellipsoid(c + V(0, 0, 0.03f), V(0.09f, 0.09f, 0.03f), trim, 0, mat: PlasticM);
            }
            s.Capsule(V(-0.12f, 0.02f, 0.12f), V(0.12f, 0.02f, 0.12f), 0.035f, 0.035f, trim, 0, mat: PlasticM);
        }

        private void Mask()
        {
            // A curved shell across the eyes, hollowed behind and cut through at the eyes
            s.Ellipsoid(V(0, 0.02f, -0.3f), V(0.8f, 0.3f, 0.42f), body, 0, mat: ClothM);
            s.Ellipsoid(V(0, 0.02f, -0.4f), V(0.72f, 0.42f, 0.42f), body, 0, op: SdfOp.Cut);
            s.Ellipsoid(V(-0.32f, 0.03f, 0), V(0.15f, 0.09f, 0.5f), body, 0, op: SdfOp.Cut);
            s.Ellipsoid(V(0.32f, 0.03f, 0), V(0.15f, 0.09f, 0.5f), body, 0, op: SdfOp.Cut);
            s.Capsule(V(0.6f, 0.18f, 0.02f), V(0.82f, 0.62f, -0.04f), 0.07f, 0.025f, trim, 0, 0.01f, ClothM);
            s.Capsule(V(0.66f, 0.16f, 0.0f), V(0.98f, 0.44f, -0.08f), 0.06f, 0.025f, trim, 0, 0.01f, ClothM);
        }

        private void Monocle()
        {
            s.Torus(V(0.36f, 0, 0.1f), 0.24f, 0.05f, body, 0, mat: MetalM, rotation: FacingFront);
            s.Capsule(V(0.42f, -0.22f, 0.09f), V(0.58f, -0.68f, -0.04f), 0.03f, 0.03f, trim, 0, mat: MetalM);
        }

        // ---------------------------------------------------------- neck

        private void BowTie()
        {
            s.Ellipsoid(V(-0.2f, 0, 0.06f), V(0.22f, 0.14f, 0.07f), body, 0, 0.02f, ClothM, AboutZ(0.12f));
            s.Ellipsoid(V(0.2f, 0, 0.06f), V(0.22f, 0.14f, 0.07f), body, 0, 0.02f, ClothM, AboutZ(-0.12f));
            s.Sphere(V(0, 0, 0.09f), 0.085f, trim, 0, mat: ClothM);
        }

        // The ring pieces: a ring of radius 1 about the origin in X and Z (fitted to the seam), Y in head units
        private void Scarf()
        {
            s.Torus(V(0, -0.02f, 0), 1.02f, 0.14f, body, 0, mat: ClothM);
            s.Capsule(V(0.25f, -0.05f, 1.02f), V(0.4f, -0.9f, 1.12f), 0.14f, 0.12f, body, 0, 0.03f, ClothM);
            s.Paint(s.Box(V(0.36f, -0.62f, 1.1f), V(0.3f, 0.07f, 0.3f), 0.02f, trim, 0, mat: ClothM));
            s.Paint(s.Box(V(0.38f, -0.8f, 1.1f), V(0.3f, 0.05f, 0.3f), 0.02f, trim, 0, mat: ClothM));
        }

        private void Ruff()
        {
            for (int k = 0; k < 16; k++)
            {
                float a = k * MathF.Tau / 16f;
                s.Ellipsoid(V(MathF.Sin(a) * 1.08f, 0, MathF.Cos(a) * 1.08f), V(0.2f, 0.16f, 0.2f), body, 0, 0.03f, ClothM);
            }
            s.Torus(V(0, 0, 0), 1.24f, 0.07f, trim, 0, 0.02f, ClothM);
        }

        private void Pearls()
        {
            for (int k = 0; k < 20; k++)
            {
                float a = k * MathF.Tau / 20f;
                float droop = 0.5f + 0.5f * MathF.Cos(a);
                s.Sphere(V(MathF.Sin(a) * 1.04f, -0.06f - 0.22f * droop, MathF.Cos(a) * 1.04f), 0.11f, k % 5 == 0 ? trim : body, 0, mat: PlasticM);
            }
        }

        private void BellCollar()
        {
            s.Torus(V(0, 0, 0), 1.03f, 0.12f, body, 0, mat: LeatherM);
            Bell(V(0, -0.36f, 1.14f), 0.55f, handle: false);
        }

        // ---------------------------------------------------------- back (origin the back's top, +Y out of the back, +Z toward the head)

        private void Cape()
        {
            s.Ellipsoid(V(0, -0.62f, -0.35f), V(0.88f, 0.7f, 0.82f), body, 0, mat: ClothM);
            s.Ellipsoid(V(0, -0.7f, -0.35f), V(0.8f, 0.7f, 0.76f), body, 0, op: SdfOp.Cut);
            s.Paint(s.Box(V(0, -0.62f, -1.1f), V(1f, 0.8f, 0.08f), 0.02f, trim, 0, mat: ClothM));
            s.Torus(V(0, -0.05f, 0.25f), 0.3f, 0.05f, trim, 0, mat: ClothM, rotation: AboutX(0.5f));
            s.Sphere(V(0, 0.02f, 0.52f), 0.1f, trim, 0, mat: MetalM);
        }

        private void Wings()
        {
            s.Sphere(V(0, 0.04f, 0), 0.09f, trim, 0, mat: ClothM);
            foreach (float side in new[] { -1f, 1f })
            {
                s.Ellipsoid(V(side * 0.52f, 0.36f, -0.06f), V(0.48f, 0.28f, 0.035f), body, 0, 0.02f, ClothM, AboutZ(side * 0.55f));
                s.Ellipsoid(V(side * 0.42f, 0.16f, -0.02f), V(0.36f, 0.18f, 0.04f), trim, 0, 0.02f, ClothM, AboutZ(side * 0.25f));
                s.Ellipsoid(V(side * 0.32f, 0.02f, 0.02f), V(0.26f, 0.12f, 0.04f), body, 0, 0.02f, ClothM, AboutZ(side * -0.05f));
            }
        }

        private void Lantern(bool hanging)
        {
            // Hanging from a hand its handle is at the origin; on a back its foot is
            var o = hanging ? V(0, -1.0f, 0) : V(0, 0.02f, 0);
            s.Ellipsoid(o + V(0, 0.45f, 0), V(0.3f, 0.36f, 0.3f), body, 0, mat: GlowM);
            for (int k = 0; k < 3; k++)
                s.Paint(s.Box(o + V(0, 0.26f + k * 0.19f, 0), V(0.4f, 0.025f, 0.4f), 0.01f, trim, 0, mat: GlowM));
            s.Cylinder(o + V(0, 0.08f, 0), 0.16f, 0.05f, 0.02f, trim, 0, mat: ClothM);
            s.Cylinder(o + V(0, 0.82f, 0), 0.14f, 0.05f, 0.02f, trim, 0, mat: ClothM);
            if (hanging)
            {
                s.Capsule(o + V(0, 0.86f, 0), V(0, 0.02f, 0), 0.03f, 0.03f, trim, 0, mat: ClothM);
                s.Capsule(V(-0.12f, 0.02f, 0), V(0.12f, 0.02f, 0), 0.04f, 0.04f, trim, 0, mat: LeatherM);
            }
        }

        // ---------------------------------------------------------- hand (origin at the hand)

        private void Fan()
        {
            var c = V(0, 0.08f, 0.08f);
            s.Cylinder(c, 0.62f, 0.03f, 0.02f, body, 0, mat: ClothM, rotation: FacingFront);
            s.Box(c + V(0, -0.4f, 0), V(0.8f, 0.4f, 0.3f), 0f, body, 0, op: SdfOp.Cut);
            foreach (float a in new[] { -1.0f, -0.5f, 0f, 0.5f, 1.0f })
                s.Paint(s.Box(c + V(MathF.Sin(a) * 0.32f, MathF.Cos(a) * 0.32f, 0), V(0.035f, 0.33f, 0.2f), 0.01f, trim, 0, rotation: AboutZ(-a)));
            s.Capsule(c, c + V(0, -0.28f, 0), 0.05f, 0.04f, trim, 0, mat: LeatherM);
            s.Sphere(c + V(0, 0, 0.035f), 0.06f, trim, 0, mat: MetalM);
        }

        private void Baton()
        {
            var tilt = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.35f);
            var a = Vector3.Transform(V(0, -0.65f, 0), tilt) + V(0, 0, 0.06f);
            var b = Vector3.Transform(V(0, 0.85f, 0), tilt) + V(0, 0, 0.06f);
            s.Capsule(a, b, 0.05f, 0.05f, body, 0, mat: PlasticM);
            s.Sphere(a, 0.09f, trim, 0, mat: MetalM);
            s.Sphere(b, 0.09f, trim, 0, mat: MetalM);
            s.Capsule(Vector3.Lerp(a, b, 0.34f), Vector3.Lerp(a, b, 0.46f), 0.065f, 0.065f, trim, 0, mat: LeatherM);
        }

        /// <summary>A bell, open at the foot with its clapper showing, and a handle of wood above it where one is held.</summary>
        private void Bell(Vector3 at, float k, bool handle)
        {
            s.Capsule(at + V(0, -0.02f, 0) * k, at + V(0, 0.34f, 0) * k, 0.3f * k, 0.16f * k, body, 0, 0.01f, MetalM);
            s.Sphere(at + V(0, -0.2f, 0) * k, 0.25f * k, body, 0, op: SdfOp.Cut);
            s.Sphere(at + V(0, -0.1f, 0) * k, 0.08f * k, trim, 0, mat: MetalM);
            if (handle)
            {
                s.Capsule(at + V(0, 0.42f, 0) * k, at + V(0, 0.92f, 0) * k, 0.07f * k, 0.06f * k, trim, 0, mat: LeatherM);
                s.Sphere(at + V(0, 0.96f, 0) * k, 0.09f * k, trim, 0, mat: LeatherM);
            }
            else s.Torus(at + V(0, 0.48f, 0) * k, 0.07f * k, 0.035f, body, 0, mat: MetalM, rotation: FacingFront);
        }

        private void StarWand()
        {
            s.Capsule(V(0, -0.45f, 0.05f), V(0, 0.62f, 0.05f), 0.045f, 0.04f, trim, 0, mat: PlasticM);
            var c = V(0, 0.82f, 0.05f);
            s.Ellipsoid(c, V(0.15f, 0.15f, 0.07f), body, 0, mat: MetalM);
            for (int k = 0; k < 5; k++)
            {
                float a = k * MathF.Tau / 5f;
                s.Capsule(c, c + V(MathF.Sin(a) * 0.32f, MathF.Cos(a) * 0.32f, 0), 0.1f, 0.03f, body, 0, mat: MetalM);
            }
        }

        private void Parasol()
        {
            s.Ellipsoid(V(0, 1.2f, 0), V(0.9f, 0.45f, 0.9f), body, 0, mat: ClothM);
            s.Ellipsoid(V(0, 1.12f, 0), V(0.86f, 0.43f, 0.86f), body, 0, op: SdfOp.Cut);
            s.Box(V(0, 0.7f, 0), V(1.1f, 0.5f, 1.1f), 0f, body, 0, op: SdfOp.Cut);
            for (int k = 0; k < 8; k++)
            {
                float a = k * MathF.Tau / 8f;
                s.Sphere(V(MathF.Sin(a) * 0.88f, 1.2f, MathF.Cos(a) * 0.88f), 0.06f, trim, 0, mat: ClothM);
            }
            s.Capsule(V(0, -0.25f, 0), V(0, 1.68f, 0), 0.035f, 0.035f, trim, 0, mat: PlasticM);
            s.Torus(V(0.1f, -0.26f, 0), 0.1f, 0.035f, trim, 0, mat: PlasticM, rotation: FacingFront);
        }

        private void Tambourine()
        {
            var c = V(0, 0.32f, 0.06f);
            s.Cylinder(c, 0.45f, 0.08f, 0.02f, body, 0, mat: PlasticM, rotation: FacingFront);
            s.Cylinder(c, 0.38f, 0.2f, 0f, body, 0, op: SdfOp.Cut, rotation: FacingFront);
            s.Cylinder(c, 0.39f, 0.025f, 0.01f, trim, 0, mat: ClothM, rotation: FacingFront);
            for (int k = 0; k < 5; k++)
            {
                float a = k * MathF.Tau / 5f + 0.3f;
                s.Cylinder(c + V(MathF.Sin(a) * 0.44f, MathF.Cos(a) * 0.44f, 0), 0.08f, 0.03f, 0.01f, trim, 0, mat: MetalM, rotation: FacingFront);
            }
        }

        // ---------------------------------------------------------- tail

        private void TailBell()
        {
            s.Torus(V(0, 0, 0), 0.24f, 0.06f, trim, 0, mat: ClothM, rotation: AboutY(0f));
            Bell(V(0, -0.5f, 0.05f), 0.6f, handle: false);
        }
    }
}
