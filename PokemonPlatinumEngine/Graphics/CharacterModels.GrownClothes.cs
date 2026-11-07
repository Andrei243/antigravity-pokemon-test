using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What covers what on a grown body (plan 30 · FM4, part b; style guide, "Grown characters", "Clothes"): whether a
/// garment is worn over the body, its sleeves, a coat or a robe over it, and where the legs are bare.
/// </summary>
internal readonly record struct GrownDress(bool Clothed, bool Sleeves, bool LongSleeves, bool Coat, bool BareThighs, bool BareShins)
{
    /// <summary>
    /// A look's dress: a top in the skin's own colour is a bare chest and a swimsuit the body's own, with nothing over
    /// them; under a skirt, shorts or a swimsuit the legs are bare (shorts painted on the thighs), under a coat or a robe
    /// without a skirt they are in trousers.
    /// </summary>
    public static GrownDress Of(CharacterStyle s)
    {
        int dr = s.Top.R - s.Skin.R, dg = s.Top.G - s.Skin.G, db = s.Top.B - s.Skin.B;
        bool clothed = !s.Swimsuit && dr * dr + dg * dg + db * db > 8 * 8;
        bool coat = clothed && (s.Coat || s.Robe);
        bool bareThighs = s.Swimsuit || (s.Skirt && !s.Robe);
        bool bareShins = bareThighs || s.Shorts;
        return new GrownDress(clothed, clothed, clothed && !s.ShortSleeves, coat, bareThighs, bareShins);
    }

    /// <summary>Whether anything is worn over the body: a garment, or a skirt over bare legs.</summary>
    public bool Wears(CharacterStyle s) => Clothed || (s.Skirt && !s.Swimsuit);
}

internal static partial class CharacterModels
{
    /// <summary>How far a grown garment stands off the body (style guide, "Grown characters", "Clothes"), over the body's slimming under it.</summary>
    public const float GrownClothesOff = 0.0065f;

    /// <summary>How slim a grown arm is under a sleeve, and a grown torso under a garment.</summary>
    public const float GrownUnderSleeve = 0.85f, GrownUnderGarment = 0.95f;

    /// <summary>Where a grown garment's hem is (style guide, "Grown characters", "Clothes"): a shirt's, a jacket's, a coat's, a robe's.</summary>
    internal static float GrownHemOf(CharacterStyle s, GrownFrame f)
    {
        float sz = f.Size;
        if (s.Robe) return f.Ankle.Y + 0.05f * sz;
        if (s.Coat) return f.Knee.Y - 0.05f * sz;
        return s.Jacket ? f.HipY - 0.035f * sz : f.HipY + 0.05f * sz;
    }

    /// <summary>Where a grown skirt's hem is: just above the knee.</summary>
    internal static float GrownSkirtHem(GrownFrame f) => f.Knee.Y + 0.055f * f.Size;

    /// <summary>
    /// What a grown body wears (style guide, "Grown characters", "Clothes"), a layer of its own over the body as the chibi
    /// kit's is, meshed apart so that every hem is an edge with its own outline: the sleeves first, each cut flat at its
    /// hem; the garment's body over the torso, cut at its hem (a coat's or a robe's tails on the coat's tail bone); a
    /// skirt on the skirt's bone; then what lies on the garment (stripes, a jacket's band and zip, a coat's opening or
    /// buttons, pockets, overalls, an apron, a vest, the team's mark, a bag's straps and the bag) and the collar or the
    /// scarf. A cut only carves what came before it.
    /// </summary>
    private static void GrownClothes(SdfModel w, CharacterStyle s, GrownFrame f, GrownDress dress)
    {
        const SurfaceMaterial Cloth = SurfaceMaterial.Cloth;
        const float Off = GrownClothesOff;
        float sz = f.Size, g = f.Girth, R = f.HeadR;
        var top = s.Top;
        var bottom = s.Bottom;
        var lining = Shade(top, 0.62f);
        var band = Shade(top, 0.86f);
        float cell = GrownMeshCell;

        if (dress.Clothed)
        {
            // ---- the sleeves, each cut flat at its hem: the cut face and a strip a cell wide above it a shade darker
            foreach (float side in new[] { 1f, -1f })
            {
                int upperB = side > 0f ? HumanBones.UpperArmL : HumanBones.UpperArmR;
                int foreB = side > 0f ? HumanBones.ForeArmL : HumanBones.ForeArmR;
                var sh = Side(f.Shoulder, side);
                var el = Side(f.Elbow, side);
                var wr = Side(f.Wrist, side);
                w.Ellipsoid(sh + new Vector3(side * 0.006f * g, -0.03f * sz, 0f), new Vector3(0.046f * g + Off, 0.07f * sz + Off, 0.045f * g + Off), top, upperB, 0.025f * sz, Cloth, AlignY(el - sh));
                w.Capsule(sh, el, 0.044f * g + Off, 0.031f * g + Off, top, upperB, 0.02f * sz, Cloth);
                Vector3 hem, along;
                int hemBone;
                if (dress.LongSleeves)
                {
                    w.Sphere(el, 0.032f * g + Off, top, foreB, 0.015f * sz, Cloth);
                    w.Capsule(el, wr, 0.032f * g + Off, 0.025f * g + Off, top, foreB, 0.015f * sz, Cloth);
                    along = Vector3.Normalize(wr - el);
                    hem = wr - along * (0.004f * sz);
                    hemBone = foreB;
                }
                else
                {
                    along = Vector3.Normalize(el - sh);
                    hem = Vector3.Lerp(sh, el, 0.62f);
                    hemBone = upperB;
                }
                var turn = AlignY(along);
                w.Box(hem + along * 0.1f, new Vector3(0.06f, 0.1f, 0.06f), 0f, top, hemBone, 0f, rotation: turn, op: SdfOp.Cut);
                if (s.Jacket)
                    w.Paint(w.Box(hem - along * (2.4f * cell), new Vector3(0.06f, 1.4f * cell, 0.06f), 0f, band, hemBone, GrownPaintEdge, rotation: turn), false, hemBone);
                w.Paint(w.Box(hem, new Vector3(0.06f, 0.9f * cell, 0.06f), 0f, lining, hemBone, GrownPaintEdge, rotation: turn), false, hemBone);
            }

            // ---- the garment's body over the torso, a little bigger than the body under it all round
            float hemY = GrownHemOf(s, f);
            GrownGarmentBody(w, f, top, Off, overTheThighs: hemY < f.HipY + 0.01f * sz);
            int[] hemBones;
            if (dress.Coat)
            {
                // A coat's tails on the coat's tail bone: over the tops of the thighs at the hips, straight past the hands
                // and flaring below them to just below the knee; a robe's to the ankles
                float flare = s.Robe ? 1.75f : 1.6f;
                float hips = Math.Max(f.PelvisW * 0.98f, f.ThighOuter + 0.004f * sz) + Off;
                var mid = new Vector3(0, f.HipY - 0.45f * (f.HipY - hemY), -0.008f * sz);
                var upperTail = w.Capsule(new Vector3(0, f.HipY + 0.03f * sz, -0.004f * sz), mid, hips, hips * 1.04f, top, HumanBones.CoatTail, 0.03f * sz, Cloth);
                var lowerTail = w.Capsule(mid, new Vector3(0, hemY + 0.01f * sz, -0.012f * sz), hips * 1.04f, f.PelvisW * flare + Off, top, HumanBones.CoatTail, 0.04f * sz, Cloth);
                upperTail.Stretch = new Vector3(1f, 1f, 0.62f);
                lowerTail.Stretch = new Vector3(1f, 1f, 0.74f);
                w.Box(new Vector3(0, hemY - 0.3f, 0), new Vector3(0.6f, 0.3f, 0.6f), 0f, top, HumanBones.CoatTail, 0f, op: SdfOp.Cut);
                hemBones = new[] { HumanBones.CoatTail, HumanBones.Hips };
            }
            else
            {
                // A shirt ends over the top of the trousers (so shorts and a skirt show whole), a jacket just below the
                // hips; the cut keeps clear of the hands at the sides
                w.Box(new Vector3(0, hemY - 0.3f, 0), new Vector3(0.21f * sz, 0.3f, 0.21f * sz), 0f, top, HumanBones.Hips, 0f, op: SdfOp.Cut);
                hemBones = new[] { HumanBones.Hips, HumanBones.Spine };
                if (s.Jacket)
                    w.Paint(w.Box(new Vector3(0, hemY + 2.6f * cell, 0), new Vector3(0.25f * sz, 1.6f * cell, 0.25f * sz), 0f, band, HumanBones.Hips, GrownPaintEdge), false, hemBones);
            }
            w.Paint(w.Box(new Vector3(0, hemY, 0), new Vector3(0.6f, 0.9f * cell, 0.6f), 0f, lining, HumanBones.Hips, GrownPaintEdge), false, hemBones);
            if (s.Skirt && !dress.Coat) GrownSkirt(w, s, f, Off);
            GrownGarmentFront(w, s, f, dress, hemY, lining);
        }
        else if (s.Skirt && !s.Swimsuit)
            GrownSkirt(w, s, f, Off);
    }

    /// <summary>
    /// A grown garment's body (style guide, "Grown characters", "Clothes"): the torso's own shapes, the pelvis, the
    /// waist, the rib cage, the top of the chest, the slopes up the neck to the shoulders and a curved figure's bust,
    /// each <paramref name="off"/> bigger, so the garment rises to a round neck at the neck's foot.
    /// </summary>
    private static void GrownGarmentBody(SdfModel w, GrownFrame f, Color top, float off, bool overTheThighs)
    {
        const SurfaceMaterial Cloth = SurfaceMaterial.Cloth;
        float sz = f.Size, g = f.Girth;
        var o = new Vector3(off);
        w.Ellipsoid(new Vector3(0, f.HipY + 0.045f * sz, -0.008f * sz), new Vector3(f.PelvisW, 0.1f * sz, f.PelvisD) + o, top, HumanBones.Hips, 0f, Cloth);
        // Over the tops of the thighs, which reach out past the pelvis at the hip joints, where a jacket's hem and a coat's
        // tails go down past them (without it the thighs stood out of a jacket's sides like a leotard's)
        if (overTheThighs)
            w.Ellipsoid(new Vector3(0, f.HipY + 0.005f * sz, -0.006f * sz), new Vector3(f.ThighOuter + 0.004f * sz, 0.1f * sz, f.PelvisD) + o, top, HumanBones.Hips, 0.03f * sz, Cloth);
        w.Ellipsoid(new Vector3(0, f.WaistY, 0.004f * sz), new Vector3(f.WaistW, (f.ChestJointY - f.HipY) * 0.62f, f.WaistD) + o, top, HumanBones.Spine, 0.04f * sz, Cloth);
        w.Ellipsoid(new Vector3(0, f.ChestMidY, 0f), new Vector3(f.ChestW, (f.ShoulderY - f.WaistY) * 0.5f, f.ChestD) + o, top, HumanBones.Chest, 0.04f * sz, Cloth);
        w.Ellipsoid(new Vector3(0, f.ShoulderY - 0.035f * sz, -0.008f * sz + f.Forward * 0.25f), new Vector3(f.Shoulder.X * 0.92f, 0.055f * sz, f.ChestD * 0.85f) + o,
            top, HumanBones.Chest, 0.03f * sz, Cloth);
        foreach (var (side, clavicle) in new[] { (1f, HumanBones.ClavicleL), (-1f, HumanBones.ClavicleR) })
            w.Capsule(new Vector3(side * 0.028f * sz, f.NeckFootY + 0.045f * sz, -0.035f * sz + f.Forward * 0.25f),
                new Vector3(side * (f.Shoulder.X - 0.035f * sz), f.ShoulderY + 0.022f * sz, -0.015f * sz + f.Forward * 0.25f),
                0.036f * g + off, 0.028f * g + off, top, clavicle, 0.03f * sz, Cloth);
        if (f.Curved)
            foreach (float side in new[] { 1f, -1f })
                w.Ellipsoid(new Vector3(side * f.ChestW * 0.42f, f.ChestMidY + 0.02f * sz, f.ChestD * 0.66f), new Vector3(0.046f, 0.044f, 0.04f) * sz + o,
                    top, HumanBones.Chest, 0.025f * sz, Cloth);
        if (f.Forward > 0f)
            w.Ellipsoid(new Vector3(0, f.ChestMidY + 0.06f * sz, -f.ChestD * 0.55f), new Vector3(f.ChestW * 0.8f, 0.09f * sz, f.ChestD * 0.6f) + o, top, HumanBones.Chest, 0.04f * sz, Cloth);
    }

    /// <summary>
    /// A grown skirt (style guide, "Grown characters", "Clothes") on the skirt's bone so it swings: from the waist,
    /// under the shirt's hem, flaring a little to just above the knee, cut flat at its hem with its darker strip.
    /// </summary>
    private static void GrownSkirt(SdfModel w, CharacterStyle s, GrownFrame f, float off)
    {
        float sz = f.Size;
        float hem = GrownSkirtHem(f);
        var bottom = s.Bottom;
        var skirt = w.Capsule(new Vector3(0, f.WaistY - 0.01f * sz, 0.002f * sz), new Vector3(0, hem + 0.012f * sz, -0.006f * sz),
            Math.Max(f.WaistW, f.PelvisW * 0.92f) + off, Math.Max(f.PelvisW * 1.5f, f.ThighOuter * 1.25f) + off, bottom, HumanBones.Skirt, 0.02f * sz, SurfaceMaterial.Cloth);
        skirt.Stretch = new Vector3(1f, 1f, 0.8f);
        // Over the tops of the thighs at the hip joints, where the cone is still narrow
        w.Ellipsoid(new Vector3(0, f.HipY + 0.005f * sz, -0.004f * sz), new Vector3(f.ThighOuter + 0.004f * sz + off, 0.08f * sz, f.PelvisD * 1.02f + off), bottom, HumanBones.Skirt,
            0.03f * sz, SurfaceMaterial.Cloth);
        w.Box(new Vector3(0, hem - 0.3f, 0), new Vector3(0.6f, 0.3f, 0.6f), 0f, bottom, HumanBones.Skirt, 0f, op: SdfOp.Cut);
        w.Paint(w.Box(new Vector3(0, hem, 0), new Vector3(0.6f, 0.9f * GrownMeshCell, 0.6f), 0f, Shade(bottom, 0.62f), HumanBones.Skirt, GrownPaintEdge), false, HumanBones.Skirt);
    }

    /// <summary>
    /// What lies on a grown garment (style guide, "Grown characters", "Clothes"): stripes; down the front a jacket's zip,
    /// a buttoned coat's placket and three buttons, an open coat's shirt between its fronts to the waist and the line
    /// where they meet below, a robe's sash; pocket flaps; overalls' bib and straps; an apron's bib, waist band and
    /// swinging panel; a vest; the team's mark; a bag's straps and the bag on its bone; the collar, or the scarf over it.
    /// </summary>
    private static void GrownGarmentFront(SdfModel w, CharacterStyle s, GrownFrame f, GrownDress dress, float hemY, Color lining)
    {
        const SurfaceMaterial Cloth = SurfaceMaterial.Cloth;
        const float Off = GrownClothesOff;
        float sz = f.Size, g = f.Girth;
        var top = s.Top;
        float cell = GrownMeshCell, edge = GrownPaintEdge;
        // A probe that finds no clothes ahead of it stops at the body's middle, never far behind it
        Vector3 Front(float x, float y) => SurfaceAlong(w, new Vector3(x, y, 1f), -Vector3.UnitZ) is var p && p.Z < 0f ? p with { Z = 0f } : p;
        float frontTop = f.ShoulderY - 0.02f * sz;
        float frontZ = f.ChestD + 0.05f;
        var seam = new[] { HumanBones.Spine, HumanBones.Hips, HumanBones.Chest, HumanBones.CoatTail };
        void WaistBand(float y, float half, Color colour) =>
            w.Paint(w.Box(new Vector3(0, y, 0), new Vector3(0.4f, half, 0.4f), 0f, colour, HumanBones.Spine, edge), false, HumanBones.Spine, HumanBones.Hips, HumanBones.Skirt, HumanBones.CoatTail);

        if (s.Stripes)
        {
            // Light stripes on a coloured shirt, coloured ones on a white shirt, across the chest and the waist
            var stripe = top.R + top.G + top.B > 690 ? s.Accent : PixelCanvas.Light1(top, 0.55f);
            foreach (float y in new[] { f.ChestMidY - 0.01f * sz, f.WaistY - 0.005f * sz })
                w.Paint(w.Box(new Vector3(0, y, 0), new Vector3(0.4f, 0.016f * sz, 0.4f), 0f, stripe, HumanBones.Chest, edge), false, HumanBones.Chest, HumanBones.Spine);
        }

        if (s.Robe)
            WaistBand(f.WaistY - 0.01f * sz, 0.022f * sz, s.Accent);
        else if (s.Jacket || dress.Coat)
        {
            float from = dress.Coat && !s.Buttoned ? f.WaistY - 0.02f * sz : frontTop;
            w.Paint(w.Box(new Vector3(0, (from + hemY) / 2f, frontZ), new Vector3(0.75f * cell, (from - hemY) / 2f, 0.06f), 0f, Shade(top, dress.Coat ? 0.72f : 0.6f), HumanBones.Spine, edge),
                false, seam);
            // An open coat's fronts part in a V from the collar to the waist, the shirt between them
            if (dress.Coat && !s.Buttoned)
            {
                // (a cone drawn deep through the front, so the coat's surface lies inside it however it curves)
                var opening = w.Capsule(new Vector3(0, frontTop + 0.01f * sz, f.ChestD), new Vector3(0, from, f.ChestD), 0.045f * sz, 0.006f * sz, s.Accent, HumanBones.Spine, edge);
                opening.Stretch = new Vector3(1f, 1f, 2.5f);
                w.Paint(opening, false, HumanBones.Spine, HumanBones.Chest);
            }
        }
        if (dress.Coat && s.Buttoned)
        {
            foreach (float y in new[] { f.ChestMidY + 0.01f * sz, f.WaistY + 0.01f * sz, f.HipY + 0.06f * sz })
                w.Sphere(Front(-0.018f * sz, y) + new Vector3(0, 0, -0.002f), Math.Max(GrownLeastRadius, 0.0095f * sz), s.Accent, HumanBones.Spine, 0f, SurfaceMaterial.Plastic);
        }
        else if (s.Jacket)
            w.Box(Front(0, frontTop - 0.01f * sz) + new Vector3(0, 0, 0.001f), new Vector3(0.009f, 0.011f, 0.0045f) * sz, 0.003f * sz, new Color(206, 210, 220, 255),
                HumanBones.Chest, 0f, SurfaceMaterial.Metal);

        // Two pocket flaps: on a jacket over the hips, on a coat lower down its tails
        if (s.Jacket || (dress.Coat && !s.Robe))
        {
            float pocketY = dress.Coat ? f.HipY - 0.06f * sz : f.HipY + 0.005f * sz;
            int pocketBone = dress.Coat ? HumanBones.CoatTail : HumanBones.Hips;
            foreach (float side in new[] { 1f, -1f })
                w.Box(Front(side * f.PelvisW * 0.55f, pocketY) + new Vector3(0, 0, -0.002f), new Vector3(0.024f * sz, Math.Max(GrownLeastRadius, 0.007f * sz), 0.006f * sz),
                    0.0025f * sz, Shade(top, 0.84f), pocketBone, 0f, Cloth);
        }

        // A strap over the shoulder that follows the clothes' surface round it, a little off it, in short pieces, from low
        // on the front to the back
        Vector3[] Strap(float x, float from, float to, Color colour, float radius, SurfaceMaterial material)
        {
            var middle = new Vector3(x, f.ChestMidY, 0f);
            const int Pieces = 12;
            var points = new Vector3[Pieces + 1];
            for (int i = 0; i <= Pieces; i++)
            {
                float a = from + (to - from) * i / Pieces;
                var d = new Vector3(0, MathF.Sin(a), MathF.Cos(a));
                points[i] = SurfaceAlong(w, middle + d * 0.4f, -d) + d * (radius * 0.8f);
            }
            for (int i = 0; i < Pieces; i++)
                w.Capsule(points[i], points[i + 1], radius, radius, colour, HumanBones.Chest, 0.003f * sz, material);
            return points;
        }

        float bibLow = f.WaistY - 0.02f * sz, bibHigh = f.ChestMidY + 0.04f * sz;
        if (s.Overalls)
        {
            w.Paint(w.Box(new Vector3(0, (bibLow + bibHigh) / 2f - 0.02f * sz, frontZ), new Vector3(f.ChestW * 0.5f, (bibHigh - bibLow) / 2f + 0.02f * sz, 0.06f), 0f, s.Bottom,
                HumanBones.Spine, edge), false, HumanBones.Spine, HumanBones.Chest, HumanBones.Hips);
            foreach (float side in new[] { 1f, -1f })
                Strap(side * f.ChestW * 0.42f, -0.35f, MathF.PI + 0.6f, s.Bottom, Math.Max(GrownLeastRadius, 0.0085f * sz), Cloth);
        }
        if (s.Apron is Color apron)
        {
            w.Paint(w.Box(new Vector3(0, (bibLow + bibHigh) / 2f, frontZ), new Vector3(f.ChestW * 0.55f, (bibHigh - bibLow) / 2f, 0.06f), 0f, apron, HumanBones.Spine, edge),
                false, HumanBones.Spine, HumanBones.Chest);
            // The panel from the waist to the knees lies along the front of whatever is under it, tilted to it
            float panelHem = f.Knee.Y + 0.02f * sz;
            var high = Front(0, f.WaistY - 0.01f * sz);
            var low = s.Skirt || dress.Coat ? Front(0, panelHem + 0.02f * sz) : high with { Y = panelHem + 0.02f * sz };
            float tilt = MathF.Atan2(high.Z - low.Z, high.Y - low.Y);
            var mid = (high + low) / 2f + new Vector3(0, 0, 0.012f * sz);
            float half = Vector3.Distance(high, low) / 2f + 0.008f * sz;
            w.Box(mid, new Vector3(f.PelvisW * 0.95f, half, Math.Max(GrownLeastRadius, 0.007f * sz)), 0.004f * sz, apron, HumanBones.Skirt, 0f, Cloth, Pitch(tilt));
            WaistBand(f.WaistY, 0.011f * sz, apron);
        }
        if (s.Vest is Color vest)
        {
            float low = f.WaistY - 0.07f * sz, high = f.ShoulderY + 0.05f * sz;
            w.Paint(w.Box(new Vector3(0, (low + high) / 2f, 0), new Vector3(0.4f, (high - low) / 2f, 0.4f), 0f, vest, HumanBones.Chest, edge), false,
                HumanBones.Spine, HumanBones.Chest, HumanBones.ClavicleL, HumanBones.ClavicleR);
            w.Paint(w.Box(new Vector3(0, (low + high) / 2f, frontZ), new Vector3(0.02f * sz, (high - low) / 2f, 0.06f), 0f, top, HumanBones.Chest, edge),
                false, HumanBones.Spine, HumanBones.Chest);
        }
        if (s.GalacticMark)
        {
            // Team Galactic's mark in the middle of the chest: the lozenge in its yellow round its dark core
            var at = new Vector3(0, f.ChestMidY + 0.01f * sz, f.ChestD + Off + 0.02f);
            var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4f);
            w.Paint(w.Box(at, new Vector3(0.02f * sz, 0.02f * sz, 0.05f), 0f, GalacticMark.Base, HumanBones.Chest, rotation: turn), false, HumanBones.Chest);
            w.Paint(w.Box(at, new Vector3(0.0095f * sz, 0.0095f * sz, 0.052f), 0f, GalacticMark.Ink, HumanBones.Chest, rotation: turn), false, HumanBones.Chest);
        }

        // The bag's straps over the shoulders (laid before the collar and the scarf go on), the bag on the back
        if (s.Bag is Color bag)
        {
            foreach (float side in new[] { 1f, -1f })
            {
                var points = Strap(side * f.ChestW * 0.62f, -0.55f, MathF.PI + 0.45f, Shade(bag, 0.78f), Math.Max(GrownLeastRadius, 0.0095f * sz), SurfaceMaterial.Leather);
                w.Box(points[1] + new Vector3(0, 0, 0.003f), new Vector3(0.012f, 0.009f, 0.004f) * sz, 0.002f * sz, new Color(206, 210, 220, 255), HumanBones.Chest, 0f, SurfaceMaterial.Metal);
            }
            float bz = -f.ChestD - Off - 0.04f * sz;
            w.Box(new Vector3(0, f.ChestMidY - 0.03f * sz, bz), new Vector3(0.085f, 0.1f, 0.045f) * sz, 0.035f * sz, bag, HumanBones.Bag, 0f, SurfaceMaterial.Leather);
            w.Box(new Vector3(0, f.ChestMidY + 0.04f * sz, bz - 0.01f * sz), new Vector3(0.082f, 0.035f, 0.045f) * sz, 0.025f * sz, Shade(bag, 0.86f), HumanBones.Bag, 0.005f * sz,
                SurfaceMaterial.Leather);
            w.Box(new Vector3(0, f.ChestMidY + 0.005f * sz, bz - 0.054f * sz), new Vector3(0.018f, 0.013f, 0.006f) * sz, 0.005f * sz, new Color(236, 230, 214, 255), HumanBones.Bag, 0f,
                SurfaceMaterial.Metal);
        }

        // A collar round the neck's foot (the accent colour's; the garment's own under a scarf), and a scarf wrapped over
        // it with its tail hanging in front
        // Round the neck's own axis, lower in front than behind
        float neck = 0.055f * g;
        var collar = new Vector3(0, f.NeckFootY - 0.004f * sz, -0.016f * sz + f.Forward * 0.3f);
        var lean = Pitch(0.28f);
        w.Torus(collar, neck + 0.012f * sz, Math.Max(GrownLeastRadius, 0.011f * sz), s.Scarf ? top : s.Accent, HumanBones.Chest, 0.006f * sz, Cloth, lean);
        if (s.Scarf)
        {
            w.Torus(collar + new Vector3(0, 0.008f * sz, 0.002f * sz), neck + 0.016f * sz, 0.016f * sz, s.Accent, HumanBones.Chest, 0f, Cloth, lean);
            var knot = Front(f.ChestW * 0.3f, f.NeckFootY - 0.03f * sz);
            w.Capsule(knot + new Vector3(0, 0, 0.008f * sz), Front(f.ChestW * 0.38f, f.ChestMidY - 0.02f * sz) + new Vector3(0, 0, 0.012f * sz), 0.016f * sz, 0.013f * sz,
                s.Accent, HumanBones.Chest, 0.003f * sz, Cloth);
        }
    }

    /// <summary>
    /// What a grown person holds (style guide, "Grown characters", "What is held"), a mesh of its own with its own outline
    /// (<see cref="CharacterRig.Held"/>: a parasol's canopy alone is a third of a body's vertices, more than the body's or
    /// the clothes' mesh has room for), on the bone it moves with, at a grown body's size: a rod or a net in the right
    /// hand, a parasol, a pickaxe or a camera over the right shoulder, a guitar slung across the front, a float ring round
    /// the waist. The right hand is the character's right.
    /// </summary>
    private static void GrownHeld(SdfModel m, CharacterStyle s, GrownFrame f)
    {
        const SurfaceMaterial Wood = SurfaceMaterial.Leather, Metal = SurfaceMaterial.Metal, Cloth = SurfaceMaterial.Cloth;
        float sz = f.Size;
        var el = Side(f.Elbow, -1f);
        var wr = Side(f.Wrist, -1f);
        var palm = wr + Vector3.Normalize(wr - el) * (0.45f * f.HandLength);
        float least = GrownLeastRadius;
        switch (s.Holds)
        {
            case HeldProp.Rod:
            {
                // Out ahead and up from the grip, a reel near the hand
                var tip = palm + new Vector3(-0.06f, 0.55f, 0.85f) * sz;
                m.Capsule(palm - (tip - palm) * 0.06f, tip, 0.011f * sz, least, s.PropColor ?? new Color(84, 66, 54, 255), HumanBones.HandR, 0.003f * sz, Wood);
                m.Sphere(palm + Vector3.Normalize(tip - palm) * (0.07f * sz) + new Vector3(0, -0.012f, 0) * sz, 0.017f * sz, new Color(176, 180, 190, 255), HumanBones.HandR, 0.003f * sz, Metal);
                break;
            }
            case HeldProp.Net:
            {
                // A pole up and ahead from the hand, a hoop at its end and a pale bag hanging from it
                var tip = palm + new Vector3(-0.03f, 0.62f, 0.48f) * sz;
                var hoop = tip + new Vector3(0, 0.06f, 0.09f) * sz;
                m.Capsule(palm, tip, 0.01f * sz, Math.Max(least, 0.009f * sz), s.PropColor ?? new Color(176, 136, 84, 255), HumanBones.HandR, 0.003f * sz, Wood);
                m.Torus(hoop, 0.1f * sz, least, new Color(176, 180, 190, 255), HumanBones.HandR, 0.003f * sz, Metal, Pitch(0.5f));
                m.Ellipsoid(hoop + new Vector3(0, -0.085f, 0.025f) * sz, new Vector3(0.082f, 0.105f, 0.082f) * sz, new Color(238, 238, 228, 255), HumanBones.HandR, 0.01f * sz, Cloth);
                break;
            }
            case HeldProp.Parasol:
            {
                // Over the right shoulder: the shaft from the hand up behind the head, the canopy a shallow dome with a
                // scalloped rim in the accent colour
                var canopy = new Vector3(-0.15f * sz, f.Crown + 0.08f * sz, -0.24f * sz);
                var tilt = Quaternion.Concatenate(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.32f), Pitch(-0.5f));
                m.Capsule(palm, canopy, Math.Max(least, 0.01f * sz), Math.Max(least, 0.01f * sz), new Color(112, 84, 64, 255), HumanBones.HandR, 0.003f * sz, Wood);
                m.Ellipsoid(canopy, new Vector3(0.4f, 0.075f, 0.4f) * sz, s.PropColor ?? s.Top, HumanBones.Chest, 0f, Cloth, tilt);
                m.Torus(canopy + Vector3.Transform(new Vector3(0, -0.037f, 0) * sz, tilt), 0.38f * sz, 0.016f * sz, s.Accent, HumanBones.Chest, 0f, Cloth, tilt);
                break;
            }
            case HeldProp.Pickaxe:
            {
                // Over the right shoulder: the handle from the hand up and back, the head across its top
                var low = palm;
                var head = new Vector3(-f.Shoulder.X - 0.03f * sz, f.ShoulderY + 0.2f * sz, -0.07f * sz);
                m.Capsule(low, head, 0.013f * sz, 0.012f * sz, s.PropColor ?? new Color(150, 108, 66, 255), HumanBones.HandR, 0.003f * sz, Wood);
                m.Capsule(head + new Vector3(0, -0.03f, 0.19f) * sz, head + new Vector3(0, 0.03f, -0.21f) * sz, 0.02f * sz, Math.Max(least, 0.009f * sz), new Color(150, 154, 166, 255),
                    HumanBones.HandR, 0.004f * sz, Metal);
                break;
            }
            case HeldProp.Guitar:
            {
                // Slung across the front: the body at the hips, the neck up to the left shoulder, a dark sound hole
                var colour = s.PropColor ?? new Color(196, 120, 58, 255);
                var along = Vector3.Normalize(new Vector3(0.5f, 0.86f, 0f));
                var turn = AlignY(along);
                var body = new Vector3(-0.03f * sz, f.HipY + 0.02f * sz, f.PelvisD + 0.055f * sz);
                m.Ellipsoid(body, new Vector3(0.12f, 0.105f, 0.032f) * sz, colour, HumanBones.Spine, 0.015f * sz, Wood, turn);
                m.Ellipsoid(body + along * (0.14f * sz), new Vector3(0.09f, 0.075f, 0.03f) * sz, colour, HumanBones.Spine, 0.025f * sz, Wood, turn);
                m.Capsule(body + along * (0.2f * sz), body + along * (0.5f * sz), 0.014f * sz, 0.012f * sz, new Color(84, 56, 40, 255), HumanBones.Spine, 0.004f * sz, Wood);
                m.Paint(m.Sphere(body + along * (0.065f * sz) + new Vector3(0, 0, 0.032f * sz), 0.026f * sz, new Color(52, 36, 28, 255), HumanBones.Spine, GrownPaintEdge), false, HumanBones.Spine);
                break;
            }
            case HeldProp.Camera:
            {
                // A television camera on the right shoulder: a box half a head long, a handle on top, a lens pointing ahead
                var colour = s.PropColor ?? new Color(58, 60, 70, 255);
                var at = new Vector3(-f.Shoulder.X - 0.02f * sz, f.ShoulderY + 0.07f * sz, 0.02f * sz);
                m.Box(at, new Vector3(0.05f, 0.055f, 0.12f) * sz, 0.012f * sz, colour, HumanBones.ClavicleR, 0.003f * sz, SurfaceMaterial.Plastic);
                m.Capsule(at + new Vector3(0, 0.06f, -0.045f) * sz, at + new Vector3(0, 0.06f, 0.045f) * sz, 0.01f * sz, 0.01f * sz, colour, HumanBones.ClavicleR, 0.003f * sz, SurfaceMaterial.Plastic);
                m.Cylinder(at + new Vector3(0, 0.005f, 0.12f) * sz, 0.035f * sz, 0.026f * sz, 0.006f * sz, new Color(28, 30, 36, 255), HumanBones.ClavicleR, 0.003f * sz, SurfaceMaterial.Plastic,
                    Pitch(MathF.PI / 2f));
                break;
            }
            case HeldProp.FloatRing:
            {
                // Round the waist, striped white four times round
                var at = new Vector3(0, f.HipY + 0.06f * sz, 0.002f * sz);
                float ring = f.PelvisW * 1.08f + 0.04f * sz;
                m.Torus(at, ring, 0.042f * sz, s.PropColor ?? new Color(240, 96, 80, 255), HumanBones.Hips, 0f, SurfaceMaterial.Plastic);
                for (int i = 0; i < 4; i++)
                {
                    float a = (i + 0.5f) * MathF.PI / 2f;
                    var stripe = at + new Vector3(MathF.Sin(a), 0, MathF.Cos(a)) * ring;
                    m.Paint(m.Box(stripe, new Vector3(0.026f, 0.06f, 0.026f) * sz, 0f, new Color(248, 248, 244, 255), HumanBones.Hips, GrownPaintEdge,
                        rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitY, a)), false, HumanBones.Hips);
                }
                break;
            }
        }
    }
}
