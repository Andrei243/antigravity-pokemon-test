using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The Mega Dynamax forms with a sculpt of their own, first half (plan 06 · R20c, wave 60), after the user's reference
// pictures (docs/art/reference/gemini/megadynamax): the thirty-nine forms from Abomasnow to Houndoom, the Megas of the
// data and the Megas of our own among them. Each is built on its Mega's own builder, as the species of our own are
// built on their forebears': the Mega is taken whole (renamed) and what the picture adds is laid over it, which is
// always the same three things (the Key Stone's emblem glowing on the body, the crimson and violet clouds of Max
// energy about the feet and over the shoulders) and then what is its own (crystals, flames, a weapon, a flower's
// petals). Everything is kept in one nested class so that the second half's file (PokemonModels.MegaMax2.cs) can
// define helpers of the same names; a Pokémon in one of these forms asks for it by its name (Pokemon.ModelName) once
// PokemonModels.HasModel says it has one.
internal static partial class PokemonModels
{
    private static class MegaMax1
    {
        /// <summary>The forms built here, by the names the data gives them.</summary>
        public static readonly string[] Names =
        {
            "Abomasnow-Mega-Mmax", "Absol-Mega-Mmax", "Aerodactyl-Mega-Mmax", "Aggron-Mega-Mmax", "Alakazam-Mega-Mmax",
            "Altaria-Mega-Mmax", "Ampharos-Mega-Mmax", "Audino-Mega-Mmax", "Banette-Mega-Mmax", "Baxcalibur-Mega-Mmax",
            "Beedrill-Mega-Mmax", "Blastoise-Mega-Mmax", "Blaziken-Mega-Mmax", "Camerupt-Mega-Mmax", "Chandelure-Mega-Mmax",
            "Charizard-Mega-X-Mmax", "Charizard-Mega-Y-Mmax", "Chesnaught-Mega-Mmax", "Cinderace-Mega-Mmax", "Decidueye-Mega-Mmax",
            "Delphox-Mega-Mmax", "Dragalge-Mega-Mmax", "Dragonite-Mega-Mmax", "Emboar-Mega-Mmax", "Empoleon-Mega-Mmax",
            "Excadrill-Mega-Mmax", "Feraligatr-Mega-Mmax", "Froslass-Mega-Mmax", "Gallade-Mega-Mmax", "Garchomp-Mega-Mmax",
            "Gardevoir-Mega-Mmax", "Gengar-Mega-Mmax", "Glalie-Mega-Mmax", "Glimmora-Mega-Mmax", "Golisopod-Mega-Mmax",
            "Greninja-Mega-Mmax", "Gyarados-Mega-Mmax", "Heracross-Mega-Mmax", "Houndoom-Mega-Mmax"
        };

        /// <summary>The sculpt of a Mega Dynamax form by its name in capitals, null for any other.</summary>
        public static PokeBuilder? Make(string form)
        {
            var b = Sculpt(form);
            return b == null ? null : Done(b);
        }

        private static PokeBuilder? Sculpt(string form) => form switch
        {
            "ABOMASNOW-MEGA-MMAX" => Abomasnow(),
            "ABSOL-MEGA-MMAX" => Absol(),
            "AERODACTYL-MEGA-MMAX" => Aerodactyl(),
            "AGGRON-MEGA-MMAX" => Aggron(),
            "ALAKAZAM-MEGA-MMAX" => Alakazam(),
            "ALTARIA-MEGA-MMAX" => Altaria(),
            "AMPHAROS-MEGA-MMAX" => Ampharos(),
            "AUDINO-MEGA-MMAX" => Audino(),
            "BANETTE-MEGA-MMAX" => Banette(),
            "BAXCALIBUR-MEGA-MMAX" => Baxcalibur(),
            "BEEDRILL-MEGA-MMAX" => Beedrill(),
            "BLASTOISE-MEGA-MMAX" => Blastoise(),
            "BLAZIKEN-MEGA-MMAX" => Blaziken(),
            "CAMERUPT-MEGA-MMAX" => Camerupt(),
            "CHANDELURE-MEGA-MMAX" => Chandelure(),
            "CHARIZARD-MEGA-X-MMAX" => CharizardX(),
            "CHARIZARD-MEGA-Y-MMAX" => CharizardY(),
            "CHESNAUGHT-MEGA-MMAX" => Chesnaught(),
            "CINDERACE-MEGA-MMAX" => Cinderace(),
            "DECIDUEYE-MEGA-MMAX" => Decidueye(),
            "DELPHOX-MEGA-MMAX" => Delphox(),
            "DRAGALGE-MEGA-MMAX" => Dragalge(),
            "DRAGONITE-MEGA-MMAX" => Dragonite(),
            "EMBOAR-MEGA-MMAX" => Emboar(),
            "EMPOLEON-MEGA-MMAX" => Empoleon(),
            "EXCADRILL-MEGA-MMAX" => Excadrill(),
            "FERALIGATR-MEGA-MMAX" => Feraligatr(),
            "FROSLASS-MEGA-MMAX" => Froslass(),
            "GALLADE-MEGA-MMAX" => Gallade(),
            "GARCHOMP-MEGA-MMAX" => Garchomp(),
            "GARDEVOIR-MEGA-MMAX" => Gardevoir(),
            "GENGAR-MEGA-MMAX" => Gengar(),
            "GLALIE-MEGA-MMAX" => Glalie(),
            "GLIMMORA-MEGA-MMAX" => Glimmora(),
            "GOLISOPOD-MEGA-MMAX" => Golisopod(),
            "GRENINJA-MEGA-MMAX" => Greninja(),
            "GYARADOS-MEGA-MMAX" => Gyarados(),
            "HERACROSS-MEGA-MMAX" => Heracross(),
            "HOUNDOOM-MEGA-MMAX" => Houndoom(),
            _ => null
        };

        // ------------------------------------------------------------------ the colours of Max energy

        private static readonly Color Crimson = Rgb(172, 34, 62);
        private static readonly Color DeepCrimson = Rgb(112, 22, 48);
        private static readonly Color Violet = Rgb(108, 62, 160);
        private static readonly Color DeepViolet = Rgb(66, 36, 110);
        private static readonly Color Cyan = Rgb(78, 226, 236);
        private static readonly Color Ice = Rgb(158, 230, 248);
        private static readonly Color DeepIce = Rgb(84, 172, 222);
        private static readonly Color Amethyst = Rgb(150, 96, 224);
        private static readonly Color Amber = Rgb(248, 176, 54);
        private static readonly Color Gold = Rgb(244, 200, 80);
        private static readonly Color Orange = Rgb(246, 120, 44);
        private static readonly Color Yolk = Rgb(255, 226, 120);

        /// <summary>A Mega taken whole as the start of its Mega Dynamax form: renamed, as big in the picture as it was.</summary>
        private static PokeBuilder Mm(PokeBuilder mega, string form)
        {
            var b = Become(mega, form, mega.Model.Fill);
            Rebase(b);
            return b;
        }

        /// <summary>
        /// What is added moves with the part of the Mega it grows from (a crystal on a forearm with the arm), and nothing set
        /// near the face takes the head's weight from the eyes, whose marks need it. Done as the sculpt is made, after every
        /// shape is in.
        /// </summary>
        private static PokeBuilder Done(PokeBuilder b)
        {
            if (BaseCounts.TryGetValue(b, out var baseCount))
            {
                var shapes = b.Sdf.Shapes;
                for (int i = (int)baseCount; i < shapes.Count; i++) shapes[i].Bone = BoneNear(b, shapes[i].A);
                b.Sdf.Changed();
            }
            return b;
        }

        // ------------------------------------------------------------------ finding the surface

        /// <summary>How many of a build's shapes are its Mega's own: what is added after is never taken for the body when something is set on it.</summary>
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PokeBuilder, object> BaseCounts = new();

        /// <summary>Takes the shapes so far as the body (after a part of the Mega was taken away).</summary>
        private static void Rebase(PokeBuilder b)
        {
            BaseCounts.Remove(b);
            BaseCounts.Add(b, b.Sdf.Shapes.Count);
        }

        /// <summary>The signed distance to the Mega's own surface, never to anything added since.</summary>
        private static float Skin(PokeBuilder b, Vector3 p)
        {
            b.Sdf.Prepare();
            int n = BaseCounts.TryGetValue(b, out var count) ? (int)count : b.Sdf.Shapes.Count;
            var shapes = b.Sdf.Shapes;
            float d = float.MaxValue;
            for (int i = 0; i < n && i < shapes.Count; i++)
            {
                var s = shapes[i];
                float box = s.BoundsDistance(p);
                switch (s.Op)
                {
                    case SdfOp.Add:
                    {
                        if (d < float.MaxValue && box > 0f && box > d + s.Blend) continue;
                        float di = s.Distance(p);
                        d = s.Blend > 0f ? SdfModel.SmoothMin(d, di, s.Blend) : Math.Min(d, di);
                        break;
                    }
                    case SdfOp.Cut:
                    {
                        if (box > 0f && box > Math.Max(0f, -d) + s.Blend) continue;
                        float di = s.Distance(p);
                        d = s.Blend > 0f ? SdfModel.SmoothMax(d, -di, s.Blend) : Math.Max(d, -di);
                        break;
                    }
                }
            }
            return d;
        }

        /// <summary>The bone of the Mega's own shape nearest to <paramref name="p"/>.</summary>
        private static int BoneNear(PokeBuilder b, Vector3 p)
        {
            b.Sdf.Prepare();
            int n = BaseCounts.TryGetValue(b, out var count) ? (int)count : b.Sdf.Shapes.Count;
            var shapes = b.Sdf.Shapes;
            int best = Body;
            float bestD = float.MaxValue;
            for (int i = 0; i < n && i < shapes.Count; i++)
            {
                var s = shapes[i];
                if (s.Op != SdfOp.Add) continue;
                float d = s.Distance(p);
                if (d < bestD) { bestD = d; best = s.Bone; }
            }
            return best;
        }

        /// <summary>The first point of the surface met walking from <paramref name="from"/> along <paramref name="dir"/>, or null if it is never met.</summary>
        private static Vector3? Hit(PokeBuilder b, Vector3 from, Vector3 dir, float reach)
        {
            var d = Vector3.Normalize(dir);
            const float Step = 0.006f;
            for (float t = 0f; t <= reach; t += Step)
            {
                var p = from + d * t;
                if (Skin(b, p) <= 0f) return p;
            }
            return null;
        }

        /// <summary>The nearest point of the body to <paramref name="p"/>, sunk a little into it so that what is set there is joined to it.</summary>
        private static Vector3 Snap(PokeBuilder b, Vector3 p, float sink = 0.012f)
        {
            for (int i = 0; i < 8; i++)
            {
                float d = Skin(b, p);
                if (MathF.Abs(d) < 0.002f) break;
                p -= Normal(b, p) * d;
            }
            return p - Normal(b, p) * sink;
        }

        /// <summary>A short strut from <paramref name="p"/> to the body's surface, for a thing that would otherwise stand clear of it.</summary>
        private static void Tether(PokeBuilder b, int bone, Vector3 p, float r, Color color)
        {
            var s = Snap(b, p);
            b.Limb(bone, p, s, r, r, color, null, 0.008f);
        }

        /// <summary>The front of the body at height <paramref name="y"/> and sideways <paramref name="x"/>.</summary>
        private static Vector3 Front(PokeBuilder b, float x, float y)
        {
            var (lo, hi) = b.Sdf.Bounds();
            return Hit(b, V(x, y, hi.Z + 0.05f), V(0, 0, -1), hi.Z - lo.Z + 0.1f) ?? V(x, y, 0.1f);
        }

        /// <summary>The back of the body at height <paramref name="y"/> and sideways <paramref name="x"/>.</summary>
        private static Vector3 Back(PokeBuilder b, float x, float y)
        {
            var (lo, hi) = b.Sdf.Bounds();
            return Hit(b, V(x, y, lo.Z - 0.05f), V(0, 0, 1), hi.Z - lo.Z + 0.1f) ?? V(x, y, -0.1f);
        }

        /// <summary>The top of the body over the place (<paramref name="x"/>, <paramref name="z"/>).</summary>
        private static Vector3 Top(PokeBuilder b, float x, float z)
        {
            var (lo, hi) = b.Sdf.Bounds();
            return Hit(b, V(x, hi.Y + 0.05f, z), V(0, -1, 0), hi.Y - lo.Y + 0.1f) ?? V(x, 0.5f, z);
        }

        /// <summary>The side of the body (<paramref name="s"/> is -1 or 1) at height <paramref name="y"/> and depth <paramref name="z"/>.</summary>
        private static Vector3 Side(PokeBuilder b, float s, float y, float z)
        {
            var (lo, hi) = b.Sdf.Bounds();
            return Hit(b, V(s * (hi.X + 0.05f), y, z), V(-s, 0, 0), hi.X - lo.X + 0.1f) ?? V(s * 0.1f, y, z);
        }

        /// <summary>Which way the surface faces at a point on it (the gradient of the distance).</summary>
        private static Vector3 Normal(PokeBuilder b, Vector3 p)
        {
            const float e = 0.008f;
            var g = V(Skin(b, p + V(e, 0, 0)) - Skin(b, p - V(e, 0, 0)),
                Skin(b, p + V(0, e, 0)) - Skin(b, p - V(0, e, 0)),
                Skin(b, p + V(0, 0, e)) - Skin(b, p - V(0, 0, e)));
            return g.LengthSquared() < 1e-12f ? V(0, 0, 1) : Vector3.Normalize(g);
        }

        /// <summary>The end of a bone's shapes that scores highest (a hand, a tail's tip, an ear's point).</summary>
        private static Vector3 Tip(PokeBuilder b, string bone, Func<Vector3, float> score)
        {
            int i = b.Model.Skeleton.Find(bone);
            var best = b.Model.Skeleton.Count > 0 && i >= 0 ? b.Model.Skeleton[i].Joint : V(0, 0.3f, 0);
            float top = float.MinValue;
            foreach (var s in b.Sdf.Shapes)
            {
                if (s.Bone != i || s.Op != SdfOp.Add) continue;
                foreach (var p in s.Shape == SdfShape.RoundCone ? new[] { s.A, s.B } : new[] { s.A })
                {
                    float v = score(p);
                    if (v > top) { top = v; best = p; }
                }
            }
            return best;
        }

        // ------------------------------------------------------------------ what every one of them carries

        /// <summary>
        /// The Key Stone's emblem glowing on the surface at (<paramref name="x"/>, <paramref name="y"/>) facing out: a ring of light with a four-pointed star in it.
        /// </summary>
        private static void Emblem(PokeBuilder b, int bone, float x, float y, float r, Color ring, Color star, bool decal = true)
        {
            // The place within a hand's breadth of where it is wanted that is flattest (a decal needs a smooth surface under it)
            var best = Front(b, x, y);
            float bestScore = float.MaxValue;
            for (int k = -5; k <= 5; k++)
            {
                var at = Front(b, x, y + k * 0.012f);
                float score = RimSpread(b, at, Normal(b, at), r) + MathF.Abs(k) * 0.0012f;
                if (score < bestScore) { bestScore = score; best = at; }
            }
            EmblemAt(b, bone, best, r, ring, star, decal);
        }

        private static (Vector3 u, Vector3 w) Tangents(Vector3 n)
        {
            var u = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) > 0.9f ? V(1, 0, 0) : V(0, 1, 0)));
            return (u, Vector3.Cross(n, u));
        }

        /// <summary>How unevenly the surface lies under a ring of radius <paramref name="r"/> laid on it at <paramref name="at"/>: the spread of its distance from the ring's plane.</summary>
        private static float RimSpread(PokeBuilder b, Vector3 at, Vector3 n, float r)
        {
            var (u, w) = Tangents(n);
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f;
                float d = Skin(b, at + (u * MathF.Cos(a) + w * MathF.Sin(a)) * r);
                lo = MathF.Min(lo, d);
                hi = MathF.Max(hi, d);
            }
            return hi - lo;
        }

        /// <summary>An emblem on the surface wherever <paramref name="at"/> is, for a forehead or a flank that doesn't face front.</summary>
        private static void EmblemAt(PokeBuilder b, int bone, Vector3 at, float r, Color ring, Color star, bool decal = true)
        {
            var n = Normal(b, at);
            // The ring is sunk until its rim touches the surface (a curved chest falls away from it), thick enough not to break up in the mesh
            var (u, w) = Tangents(n);
            float gap = (Skin(b, at + u * r) + Skin(b, at - u * r) + Skin(b, at + w * r) + Skin(b, at - w * r)) / 4f;
            float sink = Math.Clamp(gap - 0.004f, -0.004f, r * 0.55f);
            var c = at - n * sink;
            // The mark lies on the surface under it, so it takes that surface's bone, whichever was asked for
            bone = BoneNear(b, at);
            b.Torus(bone, c, r, MathF.Max(r * 0.16f, 0.014f), ring, Euler(n), mat: Glow, blend: r * 0.05f);
            // A plate of dark glass in the ring ties it to the body and gives the star a smooth ground
            b.Ell(bone, c, V(r * 0.95f, 0.012f, r * 0.95f), PixelCanvas.Shadow(ring, 0.55f), Euler(n), Glow, 0.006f);
            if (decal) b.Mark(bone, at + n * 0.002f, n, r * 0.66f, r * 0.66f, star, MarkShape.Star);
            else
            {
                // Too narrow a ground for a mark to lie on: the star is two thin crossed lozenges of light on the plate
                b.Ell(bone, c + n * 0.008f, V(r * 0.7f, 0.01f, r * 0.14f), star, Euler(n), Glow, 0.004f);
                b.Ell(bone, c + n * 0.008f, V(r * 0.14f, 0.01f, r * 0.7f), star, Euler(n), Glow, 0.004f);
            }
        }

        private static Color Cloud(int i) => (i % 3) switch { 0 => Crimson, 1 => Violet, _ => DeepCrimson };

        /// <summary>Puffs of cloud strung along a path, a core of violet running through them so they hang together.</summary>
        private static void Puffs(PokeBuilder b, int bone, Vector3[] path, float size, float taper = 1f)
        {
            b.Tube(bone, path, size * 0.55f, size * 0.4f * taper, Violet, blend: 0.012f);
            for (int i = 1; i < path.Length; i++)
            {
                float t = i / (float)(path.Length - 1);
                float s = size * (1f + 0.3f * MathF.Sin(i * 1.9f)) * (1f - (1f - taper) * t);
                b.Ell(bone, path[i], V(s, s * 0.8f, s), Cloud(i), blend: 0.02f);
            }
        }

        /// <summary>
        /// The clouds of Max energy low about the feet: a wisp that leaves <paramref name="foot"/> and winds outward on the ground
        /// <paramref name="turns"/> times round, so that the whole hangs from the body.
        /// </summary>
        private static void GroundClouds(PokeBuilder b, Vector3 foot, float rx, float rz, float turns = 1.25f, float size = 0.05f, int bone = Body)
        {
            const int Steps = 18;
            foot = Snap(b, foot, 0.03f);
            float r0 = Math.Clamp(MathF.Sqrt(foot.X * foot.X / (rx * rx) + foot.Z * foot.Z / (rz * rz)), 0.25f, 0.9f);
            float a0 = MathF.Atan2(foot.Z / rz, foot.X / rx);
            var path = new Vector3[Steps + 1];
            path[0] = V(foot.X, MathF.Max(size * 0.9f, foot.Y), foot.Z);
            for (int i = 1; i <= Steps; i++)
            {
                float t = i / (float)Steps;
                float r = r0 + (1f - r0) * t;
                float a = a0 + t * turns * MathF.Tau;
                path[i] = V(MathF.Cos(a) * rx * r, size * 0.9f, MathF.Sin(a) * rz * r);
            }
            b.Tube(bone, path, size * 0.5f, size * 0.55f, Violet, blend: 0.012f);
            for (int i = 1; i <= Steps; i++)
            {
                float t = i / (float)Steps;
                float s = size * (0.9f + 0.7f * t) * (1f + 0.25f * MathF.Sin(i * 2.3f));
                b.Ell(bone, V(path[i].X, s * 0.85f, path[i].Z), V(s, s * 0.8f, s), Cloud(i), blend: 0.02f);
            }
            // A bright cyan puff among the clouds, in the cloud itself
            b.Ell(bone, V(path[Steps / 2].X, size * 1.3f, path[Steps / 2].Z), V(size * 0.8f, size * 0.55f, size * 0.8f), Cyan, mat: Glow, blend: 0.02f);
        }

        /// <summary>A curl of Max cloud that rises from the body at <paramref name="from"/> and drifts out along <paramref name="dir"/>.</summary>
        private static void Wisp(PokeBuilder b, Vector3 from, Vector3 dir, float length, float size, float lift = 0.5f)
        {
            var d = Vector3.Normalize(dir);
            from = Snap(b, from);
            var side = Vector3.Normalize(Vector3.Cross(d, V(0, 1, 0)));
            if (side.LengthSquared() < 0.01f) side = V(1, 0, 0);
            var path = new Vector3[7];
            for (int i = 0; i < path.Length; i++)
            {
                float t = i / 6f;
                path[i] = from + d * (length * t) + V(0, 1, 0) * (lift * length * MathF.Sin(t * 2.4f)) + side * (length * 0.18f * MathF.Sin(t * 4.2f));
            }
            Puffs(b, Body, path, size, 0.7f);
        }

        /// <summary>A bolt of cyan lightning between two points, in a few jags.</summary>
        private static void Bolt(PokeBuilder b, Vector3 from, Vector3 to, float jag, Color color)
        {
            var path = new Vector3[6];
            from = Snap(b, from);
            var side = Vector3.Normalize(Vector3.Cross(to - from, V(0, 1, 0)) + V(0.001f, 0, 0));
            for (int i = 0; i < path.Length; i++)
            {
                float t = i / 5f;
                path[i] = Vector3.Lerp(from, to, t) + side * (jag * (i % 2 == 0 ? 1f : -1f) * (i == 0 || i == 5 ? 0f : 1f));
            }
            b.Tube(Body, path, 0.014f, 0.012f, color, Glow, 0.004f);
        }

        /// <summary>A fan of crystals from <paramref name="at"/>, the first along <paramref name="dir"/> and the rest spread from it, each a little shorter.</summary>
        private static void Crystals(PokeBuilder b, int bone, Vector3 at, Vector3 dir, int n, float length, float radius, Color a, Color c, float spread = 0.6f, SurfaceMaterial? mat = null)
        {
            var d = Vector3.Normalize(dir);
            at = Snap(b, at, radius * 0.6f);
            var u = Vector3.Normalize(Vector3.Cross(d, MathF.Abs(d.Y) > 0.9f ? V(1, 0, 0) : V(0, 1, 0)));
            var w = Vector3.Cross(d, u);
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : i / (float)(n - 1);
                float ang = i * 2.4f;
                var dd = Vector3.Normalize(d + (u * MathF.Cos(ang) + w * MathF.Sin(ang)) * (spread * (i == 0 ? 0f : 0.35f + 0.65f * t)));
                float l = length * (i == 0 ? 1f : 0.82f - 0.3f * t) * (1f + 0.12f * MathF.Sin(i * 3.1f));
                float r = radius * (i == 0 ? 1f : 0.85f - 0.25f * t);
                b.Spike(bone, at - dd * (r * 0.5f), at + dd * l, r, i % 2 == 0 ? a : c, 0.72f, mat ?? Glow, 0.005f);
            }
        }

        /// <summary>A flat blade of crystal or glass from <paramref name="root"/> to <paramref name="tip"/>, broad across <paramref name="facing"/>'s other axes.</summary>
        private static void Glass(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float width, Color color, Vector3 facing, float thin = 0.4f) =>
            Blade(b, bone, Snap(b, root, width * 0.5f), tip, width, color, facing, MathF.Max(thin, 0.4f), Glow);

        /// <summary>A blade set on the body: its root is brought to the surface.</summary>
        private static void Vane(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float width, Color color, Vector3 facing, float thin = 0.4f, SurfaceMaterial? mat = null) =>
            Blade(b, bone, Snap(b, root, width * 0.5f), tip, width, color, facing, MathF.Max(thin, 0.3f), mat);

        /// <summary>A foot of a model, for its clouds to leave from: the lowest end of a leg's shapes.</summary>
        private static Vector3 Foot(PokeBuilder b, string bone)
        {
            var p = Tip(b, bone, v => -v.Y);
            return V(p.X, 0.05f, p.Z);
        }

        // ------------------------------------------------------------------ Abomasnow

        /// <summary>Mega Abomasnow with ridges of ice crystal climbing its back through two small pines, icicles hanging under its arms, the white frost of its mane edged in cyan.</summary>
        private static PokeBuilder Abomasnow()
        {
            var b = Mm(AbomasnowMega(), "Abomasnow-Mega-Mmax");
            var pine = Rgb(48, 100, 68);
            var snow = Rgb(238, 244, 250);
            Crystals(b, Body, V(0, 0.76f, -0.08f), V(0, 1f, -0.12f), 7, 0.42f, 0.07f, Ice, DeepIce, 1f);
            foreach (float s in new[] { -1f, 1f })
            {
                Crystals(b, Body, V(0.3f * s, 0.7f, -0.04f), V(0.5f * s, 1f, -0.12f), 4, 0.3f, 0.05f, Ice, DeepIce, 0.7f);
                // A pine standing among the crystals, its tips under snow
                b.Spike(Body, V(0.12f * s, 0.8f, -0.1f), V(0.15f * s, 1.14f, -0.12f), 0.075f, pine, 1f, Leaf, 0.02f);
                b.Spike(Body, V(0.14f * s, 1.0f, -0.11f), V(0.15f * s, 1.16f, -0.12f), 0.04f, snow, 1f, Leaf, 0.01f);
                for (int i = 0; i < 3; i++)
                    b.Spike(Body, V((0.3f + 0.05f * i) * s, 0.52f - 0.02f * i, 0.02f), V((0.31f + 0.05f * i) * s, 0.28f - 0.04f * i, 0.02f), 0.022f, Ice, 1f, Glow, 0.004f);
            }
            Emblem(b, Body, 0f, 0.5f, 0.07f, Ice, Rgb(220, 120, 240));
            GroundClouds(b, Foot(b, "legR"), 0.55f, 0.45f);
            Wisp(b, V(-0.3f, 0.8f, -0.1f), V(-1f, 0.5f, 0f), 0.35f, 0.045f);
            Wisp(b, V(0.3f, 0.8f, -0.1f), V(1f, 0.5f, 0f), 0.35f, 0.045f);
            return b;
        }

        // ------------------------------------------------------------------ Absol

        /// <summary>Mega Absol with wings of glass: a fan of long pale shards on each shoulder, the emblem glowing on its brow.</summary>
        private static PokeBuilder Absol()
        {
            var b = Mm(AbsolMega(), "Absol-Mega-Mmax");
            var glass = Rgb(196, 234, 248);
            var edge = Rgb(126, 196, 226);
            foreach (float s in new[] { -1f, 1f })
            {
                int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
                for (int i = 0; i < 8; i++)
                {
                    float a = (18f + 12f * i) * MathF.PI / 180f;
                    float l = 0.62f - 0.04f * i;
                    var root = V(0.1f * s, 0.5f, -0.05f);
                    Glass(b, wing, root, root + V(MathF.Cos(a) * s * l, MathF.Sin(a) * l, -0.06f - 0.015f * i), 0.06f, i % 2 == 0 ? glass : edge, V(0, 0, 1), 0.18f);
                }
            }
            Emblem(b, BoneOf(b, "head"), 0f, 0.66f, 0.05f, Rgb(210, 240, 255), Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.55f, 0.6f);
            Wisp(b, V(0, 0.6f, -0.2f), V(-1f, 0.6f, -0.3f), 0.35f, 0.04f);
            Wisp(b, V(0.1f, 0.62f, -0.25f), V(1f, 0.4f, -0.4f), 0.4f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Aerodactyl

        /// <summary>Mega Aerodactyl with amber crystals standing along its wing bones, down its back and tail, under its jaw and on its brow.</summary>
        private static PokeBuilder Aerodactyl()
        {
            var b = Mm(AerodactylMega(), "Aerodactyl-Mega-Mmax");
            var deep = Rgb(210, 116, 32);
            foreach (float s in new[] { -1f, 1f })
            {
                int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
                for (int i = 0; i < 6; i++)
                {
                    float x = (0.16f + 0.1f * i) * s;
                    float l = 0.2f - 0.012f * i;
                    Crystals(b, wing, V(x, 0.52f + 0.012f * i, -0.04f), V(0.05f * s, 1f, -0.1f), 1, l, 0.034f, i % 2 == 0 ? Amber : deep, deep, 0f);
                }
            }
            Crystals(b, Body, Back(b, 0f, 0.4f) + V(0, 0.02f, 0.02f), V(0, 1f, -0.5f), 6, 0.26f, 0.045f, Amber, deep, 1f);
            var tail = Tip(b, "tail", v => -v.Z);
            Crystals(b, BoneOf(b, "tail"), tail, V(0, 0.4f, -1f), 2, 0.14f, 0.035f, Amber, deep, 0.4f);
            Crystals(b, BoneOf(b, "jaw"), V(0, 0.46f, 0.2f), V(0, -0.6f, 1f), 2, 0.12f, 0.03f, Amber, deep, 0.4f);
            Crystals(b, BoneOf(b, "head"), V(0.0f, 0.56f, 0.12f), V(0, 0.6f, -0.5f), 3, 0.16f, 0.035f, Amber, deep, 0.7f);
            Emblem(b, Body, 0f, 0.3f, 0.055f, Rgb(255, 120, 180), Rgb(120, 220, 255));
            GroundClouds(b, V(0.06f, 0.2f, -0.02f), 0.55f, 0.5f, 1.3f);
            return b;
        }

        // ------------------------------------------------------------------ Aggron

        /// <summary>Mega Aggron with a wall of white crystal pillars standing up its back like a keep, lightning in the cloud at its feet.</summary>
        private static PokeBuilder Aggron()
        {
            var b = Mm(AggronMega(), "Aggron-Mega-Mmax");
            var crystal = Rgb(222, 240, 250);
            var shade = Rgb(176, 208, 232);
            var back = Back(b, 0f, 0.7f);
            foreach (var (x, h, z) in new[] { (-0.17f, 0.34f, 0.0f), (-0.09f, 0.5f, 0.03f), (0f, 0.62f, 0.05f), (0.09f, 0.46f, 0.03f), (0.17f, 0.3f, 0.0f), (-0.13f, 0.22f, -0.1f), (0.13f, 0.26f, -0.1f) })
            {
                b.Box(Body, V(x, 0.62f + h * 0.5f, back.Z + z + 0.02f), V(0.045f, h * 0.5f, 0.04f), 0.008f, x < 0 ? crystal : shade, V(0, 0, 0), Shell, 0.01f);
                b.Box(Body, V(x, 0.62f + h, back.Z + z + 0.02f), V(0.025f, 0.03f, 0.03f), 0.006f, crystal, V(0, 0, 0), Glow, 0.006f);
            }
            Emblem(b, Body, 0f, 0.56f, 0.065f, Rgb(255, 180, 80), Rgb(120, 220, 255));
            GroundClouds(b, Foot(b, "legL"), 0.6f, 0.55f);
            Bolt(b, V(-0.2f, 0.3f, 0.2f), V(-0.35f, 0.05f, 0.35f), 0.04f, Cyan);
            Bolt(b, V(0.2f, 0.3f, 0.2f), V(0.34f, 0.05f, 0.3f), 0.04f, Rgb(190, 150, 255));
            Wisp(b, V(-0.2f, 0.75f, 0.1f), V(-1f, 0.4f, 0f), 0.3f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Alakazam

        /// <summary>Mega Alakazam with its five spoons turned to crystals of five colours hanging in an arc over its head, the emblem on its brow.</summary>
        private static PokeBuilder Alakazam()
        {
            var b = Mm(AlakazamMega(), "Alakazam-Mega-Mmax");
            b.Sdf.RemoveAll(s => s.Material == SurfaceMaterial.Metal && s.A.Y > 0.9f);
            Rebase(b);
            var colours = new[] { Rgb(160, 110, 230), Rgb(244, 130, 190), Rgb(250, 170, 100), Rgb(100, 226, 240), Rgb(226, 232, 244) };
            for (int i = 0; i < 5; i++)
            {
                float a = (200f - 40f * i) * MathF.PI / 180f;
                var c = V(MathF.Cos(a) * 0.4f, 1.0f + MathF.Sin(a) * 0.3f + 0.05f, -0.02f);
                var dir = V(MathF.Cos(a) * 0.9f, MathF.Sin(a) * 0.9f + 0.3f, 0f);
                var d = Vector3.Normalize(dir);
                b.Spike(Body, c - d * 0.12f, c + d * 0.12f, 0.04f, colours[i], 0.62f, Glow, 0.005f);
                b.Spike(Body, c + d * 0.06f, c + d * 0.17f, 0.024f, colours[i], 0.62f, Glow, 0.004f);
            }
            Emblem(b, BoneOf(b, "head"), 0f, 0.74f, 0.05f, Rgb(200, 190, 255), Rgb(180, 120, 255));
            GroundClouds(b, V(0.2f, 0.22f, 0.15f), 0.55f, 0.5f);
            Wisp(b, V(-0.3f, 0.5f, 0f), V(-1f, 0.5f, 0f), 0.3f, 0.04f);
            Wisp(b, V(0.3f, 0.5f, 0f), V(1f, 0.5f, 0f), 0.3f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Altaria

        /// <summary>Mega Altaria with its cloud wings piled into great cumulus banks pink at their feet, and a storm of Max cloud and lightning beneath it.</summary>
        private static PokeBuilder Altaria()
        {
            var b = Mm(AltariaMega(), "Altaria-Mega-Mmax");
            var white = Rgb(242, 242, 252);
            var pink = Rgb(244, 190, 214);
            foreach (float s in new[] { -1f, 1f })
            {
                int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
                foreach (var (x, y, z, r) in new[] { (0.3f, 0.72f, -0.1f, 0.15f), (0.44f, 0.62f, -0.14f, 0.13f), (0.2f, 0.86f, -0.14f, 0.13f), (0.46f, 0.82f, -0.16f, 0.12f), (0.34f, 0.94f, -0.12f, 0.11f) })
                {
                    b.Ell(wing, V(x * s, y - r * 0.35f, z), V(r * 1.1f, r * 0.8f, r), pink, blend: 0.03f);
                    b.Ell(wing, V(x * s, y, z), V(r, r * 0.85f, r), white, blend: 0.03f);
                }
            }
            Emblem(b, Body, 0f, 0.6f, 0.06f, Rgb(200, 230, 255), Rgb(255, 255, 255));
            GroundClouds(b, V(0.0f, 0.16f, -0.05f), 0.6f, 0.55f, 1.4f, 0.06f);
            Bolt(b, V(-0.3f, 0.12f, 0f), V(-0.45f, 0.03f, 0.1f), 0.04f, Rgb(230, 120, 240));
            Bolt(b, V(0.3f, 0.12f, 0f), V(0.46f, 0.03f, 0.1f), 0.04f, Cyan);
            return b;
        }

        // ------------------------------------------------------------------ Ampharos

        /// <summary>Mega Ampharos with a mane of pale hair streaming back in long locks, red orbs on its brow and along its tail, a lantern lit at the tail's tip.</summary>
        private static PokeBuilder Ampharos()
        {
            var b = Mm(AmpharosMega(), "Ampharos-Mega-Mmax");
            var hair = Rgb(214, 216, 238);
            var orb = Rgb(236, 70, 74);
            int head = BoneOf(b, "head");
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 0.04f;
                Vane(b, head, V(x, 0.74f, -0.02f), V(x * 2.5f + 0.06f, 0.52f - 0.02f * i, -0.38f - 0.02f * i), 0.06f, hair, V(0, 1, 0), 0.28f, Fur);
            }
            b.Ell(head, Top(b, 0f, 0.1f) + V(0, 0.008f, 0), V(0.038f, 0.038f, 0.038f), orb, mat: Glow, blend: 0.01f);
            int tail = BoneOf(b, "tail");
            var tip = Tip(b, "tail", v => -v.Z);
            var root = V(0, 0.22f, -0.08f);
            foreach (float t in new[] { 0.3f, 0.52f, 0.74f })
                b.Ell(tail, Snap(b, Vector3.Lerp(root, tip, t) + V(0, 0.08f, 0), -0.012f), V(0.036f, 0.036f, 0.036f), orb, mat: Glow, blend: 0.01f);
            // The lantern: a little cage of silver round a yellow light, set on the tail's tip
            var lamp = Snap(b, tip + V(0, 0.06f, 0), -0.03f);
            b.Box(tail, lamp, V(0.024f, 0.034f, 0.024f), 0.006f, Rgb(214, 218, 230), V(0, 0, 0), Metal, 0.006f);
            b.Ell(tail, lamp, V(0.02f, 0.028f, 0.02f), Yolk, mat: Glow, blend: 0.006f);
            b.Spike(tail, lamp + V(0, 0.03f, 0), lamp + V(0, 0.09f, 0), 0.03f, Rgb(214, 218, 230), 1f, Metal, 0.006f);
            Emblem(b, Body, 0f, 0.56f, 0.05f, Rgb(120, 220, 255), Rgb(255, 120, 200));
            GroundClouds(b, Foot(b, "legR"), 0.5f, 0.55f);
            Wisp(b, V(-0.1f, 0.6f, 0f), V(-1f, 0.5f, 0.2f), 0.3f, 0.035f);
            return b;
        }

        // ------------------------------------------------------------------ Audino

        /// <summary>Mega Audino with the rims of its ears alight in changing colours, a great ring of light on its chest and curls of cloud about it.</summary>
        private static PokeBuilder Audino()
        {
            var b = Mm(AudinoMega(), "Audino-Mega-Mmax");
            foreach (float s in new[] { -1f, 1f })
            {
                int ear = BoneOf(b, s < 0 ? "earL" : "earR");
                var tip = Tip(b, s < 0 ? "earL" : "earR", v => MathF.Abs(v.X));
                b.Ell(ear, tip + V(0, 0, 0), V(0.05f, 0.012f, 0.03f), Rgb(120, 230, 240), V(0, 0, -20f * s), Glow, 0.006f);
                b.Ell(ear, tip + V(-0.03f * s, 0.004f, 0.01f), V(0.035f, 0.01f, 0.022f), Rgb(250, 150, 220), V(0, 0, -20f * s), Glow, 0.006f);
            }
            Emblem(b, Body, 0f, 0.27f, 0.07f, Rgb(120, 230, 240), Rgb(250, 150, 220));
            GroundClouds(b, Foot(b, "legR"), 0.3f, 0.28f, 1.3f, 0.032f);
            Wisp(b, V(-0.12f, 0.3f, 0f), V(-1f, 0.4f, 0f), 0.2f, 0.028f);
            Wisp(b, V(0.12f, 0.3f, 0f), V(1f, 0.4f, 0f), 0.2f, 0.028f);
            return b;
        }

        // ------------------------------------------------------------------ Banette

        /// <summary>Mega Banette hung from six strings of violet light, the emblem on its chest, standing in a whirlpool of Max cloud.</summary>
        private static PokeBuilder Banette()
        {
            var b = Mm(BanetteMega(), "Banette-Mega-Mmax");
            var thread = Rgb(176, 112, 255);
            var (lo, hi) = b.Sdf.Bounds();
            float up = hi.Y + 0.55f;
            foreach (var x in new[] { -0.1f, -0.04f, 0.04f, 0.1f })
            {
                var top = Top(b, x, 0.0f);
                b.Limb(BoneOf(b, "head"), top, V(x * 1.8f, up, 0f), 0.012f, 0.011f, thread, Glow, 0.003f);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                var hand = Side(b, s, 0.26f, 0f) + V(-0.012f * s, 0, 0);
                b.Limb(BoneOf(b, s < 0 ? "armL" : "armR"), hand, V(hand.X * 1.1f, up, 0f), 0.013f, 0.012f, thread, Glow, 0.003f);
            }
            Emblem(b, Body, 0f, 0.3f, 0.045f, Rgb(200, 150, 255), Rgb(255, 255, 255));
            b.Ell(Body, V(0, 0.022f, 0), V(0.3f, 0.018f, 0.28f), Cyan, mat: Glow, blend: 0.006f);
            GroundClouds(b, V(0.05f, 0.04f, 0f), 0.3f, 0.28f, 1.6f, 0.036f);
            return b;
        }

        // ------------------------------------------------------------------ Baxcalibur

        /// <summary>Mega Baxcalibur with a sword of glowing ice taller than itself standing on its back, ice armour at its shoulders and knees.</summary>
        private static PokeBuilder Baxcalibur()
        {
            var b = Mm(BaxcaliburMega(), "Baxcalibur-Mega-Mmax");
            var ice = Rgb(120, 236, 244);
            var rock = Rgb(176, 226, 240);
            Glass(b, Body, V(0.02f, 0.72f, -0.16f), V(0.2f, 1.78f, -0.68f), 0.12f, ice, V(1f, 0f, 0.4f), 0.2f);
            Glass(b, Body, V(0.0f, 0.78f, -0.14f), V(0.12f, 1.4f, -0.4f), 0.07f, rock, V(1f, 0f, 0.4f), 0.2f);
            foreach (float s in new[] { -1f, 1f })
            {
                Crystals(b, Body, V(0.19f * s, 0.84f, -0.02f), V(0.5f * s, 1f, -0.2f), 4, 0.24f, 0.045f, rock, ice, 0.6f);
                b.Box(Body, Snap(b, V(0.11f * s, 0.4f, 0.12f), 0.01f), V(0.045f, 0.045f, 0.04f), 0.01f, rock, V(0, 0, 25f), Shell, 0.008f);
            }
            Emblem(b, Body, 0f, 0.6f, 0.075f, ice, Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.6f, 0.6f);
            Wisp(b, V(-0.2f, 0.9f, -0.1f), V(-1f, 0.5f, -0.2f), 0.4f, 0.045f);
            Wisp(b, V(0.25f, 0.9f, -0.1f), V(1f, 0.6f, -0.2f), 0.4f, 0.045f);
            return b;
        }

        // ------------------------------------------------------------------ Beedrill

        /// <summary>Mega Beedrill with its stingers sheathed in long violet crystal, stained-glass wings of violet and cyan, the emblem on its thorax.</summary>
        private static PokeBuilder Beedrill()
        {
            var b = Mm(BeedrillMega(), "Beedrill-Mega-Mmax");
            var purple = Rgb(168, 96, 240);
            foreach (float s in new[] { -1f, 1f })
            {
                string arm = s < 0 ? "armL" : "armR";
                var hand = Tip(b, arm, v => v.Z - v.Y * 0.5f);
                var dir = Vector3.Normalize(hand - b.Model.Skeleton[BoneOf(b, arm)].Joint);
                b.Spike(BoneOf(b, arm), Snap(b, hand - dir * 0.2f, 0.02f), hand + dir * 0.18f, 0.05f, purple, 0.8f, Glow, 0.008f);
                string hind = s < 0 ? "hindL" : "hindR";
                var h2 = Tip(b, hind, v => v.Z - v.Y * 0.5f);
                var d2 = Vector3.Normalize(h2 - b.Model.Skeleton[BoneOf(b, hind)].Joint);
                b.Spike(BoneOf(b, hind), Snap(b, h2 - d2 * 0.15f, 0.02f), h2 + d2 * 0.14f, 0.04f, Rgb(190, 130, 250), 0.8f, Glow, 0.008f);
                int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
                Glass(b, wing, V(0.05f * s, 0.74f, -0.06f), V(0.3f * s, 1.12f, -0.22f), 0.14f, purple, V(0, 0, 1), 0.12f);
                Glass(b, wing, V(0.05f * s, 0.72f, -0.06f), V(0.36f * s, 0.9f, -0.2f), 0.08f, Cyan, V(0, 0, 1), 0.12f);
            }
            Emblem(b, Body, 0f, 0.7f, 0.04f, Rgb(200, 150, 255), Rgb(255, 255, 255));
            GroundClouds(b, V(0.04f, 0.55f, 0.02f), 0.4f, 0.4f, 1.3f);
            Bolt(b, V(-0.1f, 0.3f, 0.1f), V(-0.2f, 0.05f, 0.2f), 0.04f, Cyan);
            Bolt(b, V(0.1f, 0.3f, 0.1f), V(0.22f, 0.05f, 0.2f), 0.04f, Cyan);
            return b;
        }

        // ------------------------------------------------------------------ Blastoise

        /// <summary>Mega Blastoise with its cannons turned to glass tubes of churning water, three on its shell and one on each forearm.</summary>
        private static PokeBuilder Blastoise()
        {
            var b = Mm(BlastoiseMega(), "Blastoise-Mega-Mmax");
            var glass = Rgb(110, 232, 240);
            var foam = Rgb(200, 250, 252);
            foreach (var (a, c, r) in new[] { (V(-0.14f, 0.8f, -0.22f), V(-0.34f, 1.2f, -0.02f), 0.085f), (V(0.18f, 0.78f, -0.22f), V(0.2f, 1.24f, -0.04f), 0.075f), (V(-0.02f, 0.7f, -0.3f), V(-0.12f, 1.0f, -0.52f), 0.06f) })
            {
                var root = Snap(b, a, 0.03f);
                var end = root + (c - a);
                b.Limb(Body, root, end, r, r, glass, Glow, 0.01f);
                b.Limb(Body, Vector3.Lerp(root, end, 0.25f), Vector3.Lerp(root, end, 0.85f), r * 0.6f, r * 0.6f, foam, Glow, 0.01f);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                string arm = s < 0 ? "armL" : "armR";
                var hand = Tip(b, arm, v => v.Z);
                b.Limb(BoneOf(b, arm), hand, hand + V(0.02f * s, 0, 0.14f), 0.05f, 0.05f, glass, Glow, 0.008f);
            }
            Emblem(b, Body, 0f, 0.55f, 0.075f, Rgb(255, 200, 120), Rgb(255, 120, 200));
            GroundClouds(b, Foot(b, "legL"), 0.6f, 0.55f);
            Wisp(b, V(-0.35f, 0.7f, 0f), V(-1f, 0.5f, 0.2f), 0.35f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Blaziken

        /// <summary>Mega Blaziken with its fists wrapped in great flames of cut-crystal fire and ribbons of flame trailing from its shoulders.</summary>
        private static PokeBuilder Blaziken()
        {
            var b = Mm(BlazikenMega(), "Blaziken-Mega-Mmax");
            foreach (float s in new[] { -1f, 1f })
            {
                string arm = s < 0 ? "armL" : "armR";
                int bone = BoneOf(b, arm);
                var hand = Tip(b, arm, v => MathF.Abs(v.X) - v.Y * 0.2f);
                FlameTongue(b, bone, hand, hand + V(0.04f * s, 0.5f, -0.06f), 0.1f, Orange, Yolk);
                FlameTongue(b, bone, hand + V(0.02f * s, 0, 0), hand + V(0.17f * s, 0.34f, -0.04f), 0.07f, Rgb(250, 150, 50), Yolk);
                FlameTongue(b, bone, hand + V(-0.02f * s, 0, 0), hand + V(-0.06f * s, 0.3f, 0.04f), 0.06f, Orange, Yolk);
                var ribbon = Smooth(4, V(0.14f * s, 0.84f, -0.04f), V(0.32f * s, 1.08f, -0.14f), V(0.5f * s, 1.0f, -0.28f), V(0.62f * s, 1.14f, -0.36f));
                b.Tube(Body, ribbon, 0.03f, 0.014f, Orange, Glow, 0.006f);
            }
            Emblem(b, Body, 0f, 0.72f, 0.045f, Orange, Yolk);
            GroundClouds(b, Foot(b, "legR"), 0.55f, 0.5f);
            return b;
        }

        // ------------------------------------------------------------------ Camerupt

        /// <summary>Mega Camerupt with a volcano risen on its back, its slopes cracked with lava and studded with violet crystals, smoke from its crater, a gem on its brow.</summary>
        private static PokeBuilder Camerupt()
        {
            var b = Mm(CameruptMega(), "Camerupt-Mega-Mmax");
            var rock = Rgb(70, 62, 68);
            var lava = Rgb(250, 130, 40);
            b.Spike(Body, V(0, 0.34f, -0.04f), V(0, 1.0f, -0.04f), 0.22f, rock, 1f, Shell, 0.03f);
            b.Ell(Body, V(0, 0.98f, -0.04f), V(0.07f, 0.035f, 0.07f), lava, mat: Glow, blend: 0.012f);
            foreach (var (x, y, z, rot) in new[] { (0.0f, 0.62f, 0.12f, 0f), (0.1f, 0.5f, 0.1f, 20f), (-0.1f, 0.5f, 0.1f, -20f), (0.12f, 0.58f, -0.14f, 90f) })
                b.PaintEll(Body, V(x, y, z - 0.02f), V(0.012f, 0.1f, 0.03f), lava, V(0, 0, rot), 0.008f);
            foreach (float s in new[] { -1f, 1f })
            {
                Crystals(b, Body, V(0.14f * s, 0.66f, 0.04f), V(0.7f * s, 0.8f, 0.1f), 3, 0.14f, 0.03f, Amethyst, Rgb(190, 120, 250), 0.5f);
                var flame = Snap(b, V(0.15f * s, 0.7f, 0.06f), 0.02f);
                FlameTongue(b, Body, flame, flame + V(0.08f * s, 0.14f, 0.04f), 0.035f, Orange, Yolk);
            }
            // Smoke climbing from the crater
            Puffs(b, Body, new[] { V(0, 1.0f, -0.04f), V(0.02f, 1.12f, -0.04f), V(-0.02f, 1.24f, -0.03f), V(0.03f, 1.36f, -0.04f) }, 0.05f, 1.3f);
            Emblem(b, BoneOf(b, "head"), 0f, 0.31f, 0.035f, Orange, Yolk);
            GroundClouds(b, Foot(b, "legR"), 0.45f, 0.5f);
            return b;
        }

        // ------------------------------------------------------------------ Chandelure

        /// <summary>Mega Chandelure with a great violet flame crowned over its globe, a pendant hanging below and the emblem lit in the glass.</summary>
        private static PokeBuilder Chandelure()
        {
            var b = Mm(ChandelureMega(), "Chandelure-Mega-Mmax");
            var violet = Rgb(150, 100, 236);
            var pale = Rgb(210, 190, 255);
            // Kept within its Mega's reach sideways: a wider body would be meshed coarser and lose its small marks
            FlameTongue(b, BoneOf(b, "head"), V(0, 0.62f, 0), V(0.02f, 1.05f, 0), 0.12f, violet, pale);
            FlameTongue(b, BoneOf(b, "head"), V(0.05f, 0.62f, 0), V(0.18f, 0.92f, 0), 0.07f, violet, pale);
            FlameTongue(b, BoneOf(b, "head"), V(-0.05f, 0.62f, 0), V(-0.16f, 0.9f, 0.02f), 0.07f, violet, pale);
            b.Spike(Body, V(0, 0.2f, 0), V(0, 0.03f, 0), 0.05f, Rgb(36, 30, 44), 0.9f, Metal, 0.01f);
            // Its body is a cage of thin arms round a stem, so the emblem is set on the stem
            Emblem(b, Body, 0f, 0.62f, 0.05f, Cyan, Rgb(255, 200, 255), decal: false);
            GroundClouds(b, V(0.0f, 0.16f, 0f), 0.34f, 0.34f, 1.4f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Charizard

        /// <summary>Mega Charizard X with blue fire licking up from its horns and along the edges of its wings, the emblem in its chest.</summary>
        private static PokeBuilder CharizardX()
        {
            var b = Mm(CharizardMegaX(), "Charizard-Mega-X-Mmax");
            var blue = Rgb(70, 150, 255);
            var pale = Rgb(190, 240, 255);
            int head = BoneOf(b, "head");
            foreach (float s in new[] { -1f, 1f })
            {
                FlameTongue(b, head, V(0.05f * s, 1.0f, -0.02f), V(0.12f * s, 1.34f, -0.1f), 0.05f, blue, pale);
                FlameTongue(b, head, V(0.09f * s, 0.98f, -0.04f), V(0.28f * s, 1.2f, -0.12f), 0.04f, blue, pale);
                int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
                foreach (var (x, y) in new[] { (0.3f, 0.92f), (0.46f, 1.0f), (0.6f, 1.0f), (0.7f, 0.9f) })
                    FlameTongue(b, wing, V(x * s, y - 0.06f, -0.2f), V((x + 0.02f) * s, y + 0.1f, -0.24f), 0.03f, blue, pale);
            }
            FlameTongue(b, head, V(0, 1.0f, -0.04f), V(0, 1.28f, -0.14f), 0.045f, blue, pale);
            Emblem(b, Body, 0f, 0.55f, 0.06f, Rgb(120, 220, 255), Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.7f, 0.6f);
            Wisp(b, V(-0.4f, 0.7f, -0.1f), V(-1f, 0.5f, -0.1f), 0.4f, 0.045f);
            return b;
        }

        /// <summary>Mega Charizard Y with a sun of gold light and a cage of amber ribs on its chest, amber crystals grown along the edges of its wings.</summary>
        private static PokeBuilder CharizardY()
        {
            var b = Mm(CharizardMegaY(), "Charizard-Mega-Y-Mmax");
            var amber = Rgb(255, 176, 70);
            var sun = Rgb(255, 214, 90);
            var front = Front(b, 0f, 0.58f);
            b.Ell(Body, front + V(0, 0, 0.012f), V(0.06f, 0.06f, 0.02f), sun, mat: Glow, blend: 0.01f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f;
                var c = front + V(0, 0, 0.012f);
                b.Spike(Body, c + V(MathF.Cos(a) * 0.05f, MathF.Sin(a) * 0.05f, 0), c + V(MathF.Cos(a) * 0.15f, MathF.Sin(a) * 0.15f, 0.02f), 0.022f, amber, 0.7f, Glow, 0.004f);
            }
            b.Ell(Body, V(0, 0.4f, front.Z + 0.01f), V(0.03f, 0.03f, 0.014f), Rgb(255, 240, 220), mat: Glow, blend: 0.006f);
            foreach (float s in new[] { -1f, 1f })
            {
                int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
                for (int i = 0; i < 7; i++)
                {
                    float t = i / 6f;
                    var at = V((0.3f + 0.52f * t) * s, 1.0f + 0.1f * MathF.Sin(t * 3f) - 0.3f * t * t, -0.2f);
                    b.Spike(wing, at, at + V(0.02f * s, 0.12f, -0.02f), 0.026f, amber, 0.7f, Glow, 0.004f);
                }
            }
            Emblem(b, BoneOf(b, "head"), 0f, 0.99f, 0.035f, Rgb(255, 220, 150), Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.7f, 0.6f);
            Wisp(b, V(-0.4f, 0.7f, -0.1f), V(-1f, 0.5f, -0.1f), 0.4f, 0.045f);
            Wisp(b, V(0.4f, 0.7f, -0.1f), V(1f, 0.5f, -0.1f), 0.4f, 0.045f);
            return b;
        }

        // ------------------------------------------------------------------ Chesnaught

        /// <summary>Mega Chesnaught with a crown of violet and green crystals standing up from its shell, the emblem on its chest.</summary>
        private static PokeBuilder Chesnaught()
        {
            var b = Mm(ChesnaughtMega(), "Chesnaught-Mega-Mmax");
            var green = Rgb(96, 190, 100);
            for (int i = 0; i < 11; i++)
            {
                float a = i * MathF.Tau / 11f;
                var baseP = Top(b, MathF.Sin(a) * 0.16f, -0.1f + MathF.Cos(a) * 0.1f);
                var dir = V(MathF.Sin(a) * 0.55f, 1f, MathF.Cos(a) * 0.3f - 0.15f);
                float l = 0.3f + 0.08f * MathF.Sin(i * 1.7f);
                var d = Vector3.Normalize(dir);
                b.Spike(Body, baseP - d * 0.03f, baseP + d * l, 0.05f, i % 2 == 0 ? Amethyst : green, 0.72f, Glow, 0.006f);
            }
            Emblem(b, Body, 0f, 0.4f, 0.07f, Rgb(200, 150, 255), Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.55f, 0.5f);
            Bolt(b, V(-0.2f, 0.2f, 0.15f), V(-0.35f, 0.04f, 0.25f), 0.04f, Cyan);
            return b;
        }

        // ------------------------------------------------------------------ Cinderace

        /// <summary>Mega Cinderace with its ears streaming fire back over its shoulders, a ball of flame set with violet crystals under its foot, a white cape of fur blown out behind.</summary>
        private static PokeBuilder Cinderace()
        {
            var b = Mm(CinderaceMega(), "Cinderace-Mega-Mmax");
            int head = BoneOf(b, "head");
            FlameTongue(b, head, V(0.0f, 0.9f, -0.04f), V(0f, 1.06f, -0.26f), 0.06f, Orange, Yolk);
            FlameTongue(b, head, V(0.0f, 1.04f, -0.22f), V(0f, 1.1f, -0.46f), 0.05f, Orange, Yolk);
            FlameTongue(b, head, V(0.0f, 1.08f, -0.42f), V(0f, 1.0f, -0.62f), 0.04f, Orange, Yolk);
            // The fire ball under its right foot
            int leg = BoneOf(b, "legR");
            var ball = V(0.08f, 0.09f, 0.18f);
            b.Ell(leg, ball, V(0.09f, 0.09f, 0.09f), Orange, mat: Glow, blend: 0.012f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * MathF.Tau / 7f;
                var d = V(MathF.Cos(a), MathF.Sin(a) * 0.9f + 0.2f, MathF.Sin(a * 2f) * 0.6f);
                d = Vector3.Normalize(d);
                b.Spike(leg, ball + d * 0.05f, ball + d * 0.14f, 0.028f, Amethyst, 0.7f, Glow, 0.004f);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                var hand = Tip(b, s < 0 ? "armL" : "armR", v => MathF.Abs(v.X));
                b.Torus(BoneOf(b, s < 0 ? "armL" : "armR"), Vector3.Lerp(V(0.06f * s, 0.62f, 0), hand, 0.7f), 0.04f, 0.012f, Gold, V(0, 0, 70f * s), mat: Metal);
                Vane(b, Body, V(0.06f * s, 0.74f, -0.02f), V(0.2f * s, 0.62f, -0.44f), 0.09f, Rgb(244, 244, 250), V(0, 1, 0), 0.25f, Fur);
            }
            Emblem(b, Body, 0f, 0.6f, 0.045f, Rgb(230, 110, 190), Rgb(255, 220, 120));
            GroundClouds(b, Foot(b, "legL"), 0.45f, 0.5f);
            return b;
        }

        // ------------------------------------------------------------------ Decidueye

        /// <summary>Mega Decidueye with a bow of glowing vine drawn across its chest and a glowing arrow nocked, a quiver of arrows at its back.</summary>
        private static PokeBuilder Decidueye()
        {
            var b = Mm(DecidueyeMega(), "Decidueye-Mega-Mmax");
            var teal = Rgb(110, 244, 226);
            var vine = Rgb(96, 74, 46);
            int wing = BoneOf(b, "wingR");
            // The bow: a curve of wood on the player's left side with a bright string
            var bow = Smooth(4, V(0.3f, 0.12f, 0.2f), V(0.36f, 0.3f, 0.24f), V(0.37f, 0.48f, 0.26f), V(0.35f, 0.66f, 0.24f), V(0.28f, 0.82f, 0.2f));
            b.Tube(wing, bow, 0.022f, 0.016f, vine, Leaf, 0.006f);
            b.Limb(wing, V(0.3f, 0.12f, 0.2f), V(0.28f, 0.82f, 0.2f), 0.014f, 0.014f, teal, Glow, 0.004f);
            Tether(b, wing, bow[bow.Length / 2], 0.02f, vine);
            // The arrow: a rod of light from the string to a head of glowing barbs, held to the body where the hand draws it
            b.Limb(wing, V(0.32f, 0.5f, 0.26f), V(-0.4f, 0.54f, 0.24f), 0.016f, 0.016f, teal, Glow, 0.004f);
            Tether(b, wing, V(0.0f, 0.52f, 0.25f), 0.02f, teal);
            b.Spike(wing, V(-0.34f, 0.54f, 0.24f), V(-0.52f, 0.55f, 0.24f), 0.05f, teal, 0.5f, Glow, 0.006f);
            foreach (float s in new[] { -1f, 1f }) b.Spike(wing, V(-0.34f, 0.54f, 0.24f), V(-0.46f, 0.54f + 0.06f * s, 0.24f), 0.025f, teal, 0.5f, Glow, 0.004f);
            // The quiver: arrows pointing down and back
            for (int i = 0; i < 5; i++)
                b.Spike(Body, Snap(b, V(-0.1f + 0.04f * i, 0.4f, -0.16f), 0.02f), V(-0.14f + 0.045f * i, 0.06f, -0.34f), 0.016f, teal, 1f, Glow, 0.003f);
            Emblem(b, Body, 0f, 0.46f, 0.05f, Gold, Rgb(110, 244, 226));
            GroundClouds(b, Foot(b, "legR"), 0.5f, 0.45f);
            return b;
        }

        // ------------------------------------------------------------------ Delphox

        /// <summary>Mega Delphox holding up a staff of pale crystal crowned with a swirling ball of fire ringed in violet, the emblem at its collar.</summary>
        private static PokeBuilder Delphox()
        {
            var b = Mm(DelphoxMega(), "Delphox-Mega-Mmax");
            var crystal = Rgb(236, 220, 248);
            var hand = Snap(b, Tip(b, "armL", v => -v.X), 0.01f);
            int arm = BoneOf(b, "armL");
            // The staff is held out to the side and up; kept short so that the Mega's thin legs are not lost to a coarser mesh
            var top = V(hand.X - 0.03f, 0.88f, hand.Z + 0.02f);
            b.Limb(arm, hand + V(0, -0.05f, 0), top, 0.02f, 0.016f, crystal, Metal, 0.006f);
            var orb = top + V(0, 0.06f, 0);
            b.Ell(arm, orb, V(0.075f, 0.075f, 0.075f), Orange, mat: Glow, blend: 0.012f);
            b.Ell(arm, orb, V(0.045f, 0.045f, 0.045f), Yolk, mat: Glow, blend: 0.01f);
            b.Torus(arm, orb, 0.09f, 0.016f, Violet, V(0, 0, 25f), mat: Glow);
            b.Torus(arm, orb, 0.09f, 0.014f, Rgb(160, 90, 220), V(40f, 0, 0), mat: Glow);
            Emblem(b, Body, 0f, 0.54f, 0.045f, Rgb(190, 150, 255), Rgb(255, 120, 90));
            GroundClouds(b, V(0.17f, 0.15f, 0.0f), 0.4f, 0.38f, 1.4f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Dragalge

        /// <summary>Mega Dragalge with clusters of violet crystal grown along its spine and its fronds, the emblem at its throat, a swamp of Max cloud below.</summary>
        private static PokeBuilder Dragalge()
        {
            var b = Mm(DragalgeMega(), "Dragalge-Mega-Mmax");
            var pink = Rgb(220, 120, 240);
            foreach (var (x, y, dy) in new[] { (0f, 0.7f, 0.5f), (0.02f, 0.58f, 0.3f), (0.0f, 0.46f, 0.1f), (0.0f, 0.34f, -0.1f), (0.04f, 0.22f, -0.3f) })
                Crystals(b, Body, Back(b, x, y) + V(0, 0, 0.02f), V(0, 0.5f + dy, -1f), 4, 0.18f, 0.035f, Amethyst, pink, 0.6f);
            foreach (float s in new[] { -1f, 1f })
                Crystals(b, Body, Side(b, s, 0.46f, 0.02f), V(s, 0.4f, 0f), 3, 0.15f, 0.03f, Amethyst, pink, 0.5f);
            Emblem(b, Body, 0f, 0.56f, 0.055f, Rgb(230, 130, 240), Rgb(130, 230, 250));
            GroundClouds(b, V(0.04f, 0.14f, 0.0f), 0.55f, 0.45f, 1.5f, 0.055f);
            return b;
        }

        // ------------------------------------------------------------------ Dragonite

        /// <summary>Mega Dragonite with a second pair of feathered wings lifted over its head, ribbons of cyan light streaming back from its horns, a glass orb at its tail's tip.</summary>
        private static PokeBuilder Dragonite()
        {
            var b = Mm(DragoniteMega(), "Dragonite-Mega-Mmax");
            var cream = Rgb(248, 236, 208);
            var shade = Rgb(222, 196, 170);
            int head = BoneOf(b, "head");
            foreach (float s in new[] { -1f, 1f })
            {
                Vane(b, head, V(0.05f * s, 0.9f, -0.03f), V(0.5f * s, 1.3f, -0.1f), 0.12f, cream, V(0, 0, 1), 0.4f);
                Vane(b, head, V(0.04f * s, 0.88f, -0.03f), V(0.34f * s, 1.24f, -0.06f), 0.1f, shade, V(0, 0, 1), 0.4f);
                Vane(b, head, V(0.05f * s, 0.88f, -0.03f), V(0.58f * s, 1.08f, -0.12f), 0.09f, cream, V(0, 0, 1), 0.4f);
                var ribbon = Smooth(4, V(0.05f * s, 0.92f, -0.04f), V(0.2f * s, 1.08f, -0.2f), V(0.46f * s, 1.02f, -0.42f), V(0.72f * s, 0.92f, -0.4f));
                b.Tube(head, ribbon, 0.022f, 0.012f, Cyan, Glow, 0.005f);
            }
            Crystals(b, Body, V(0, 0.84f, -0.22f), V(0, 0.8f, -0.6f), 3, 0.2f, 0.04f, Cyan, Rgb(150, 240, 250), 0.4f);
            var tip = Tip(b, "tail", v => -v.Z);
            b.Ell(BoneOf(b, "tail"), tip + V(0, 0.01f, -0.04f), V(0.08f, 0.08f, 0.08f), Rgb(120, 200, 180), mat: Glow, blend: 0.01f);
            b.Ell(BoneOf(b, "tail"), tip + V(0.02f, 0.03f, -0.02f), V(0.04f, 0.03f, 0.03f), Rgb(250, 250, 230), mat: Glow, blend: 0.008f);
            Emblem(b, Body, 0f, 0.5f, 0.06f, Rgb(200, 250, 255), Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.6f, 0.6f);
            Wisp(b, V(0.3f, 0.5f, -0.05f), V(1f, 0.4f, -0.2f), 0.4f, 0.045f);
            return b;
        }

        // ------------------------------------------------------------------ Emboar

        /// <summary>Mega Emboar with a mane of pink crystal and flame about its neck, fire pouring from its forearms, the emblem on its belly.</summary>
        private static PokeBuilder Emboar()
        {
            var b = Mm(EmboarMega(), "Emboar-Mega-Mmax");
            var pink = Rgb(250, 140, 190);
            var hot = Rgb(255, 90, 120);
            // The mane stands round the neck and shoulders, below the ears so as not to crowd them
            for (int i = 0; i < 12; i++)
            {
                float a = i * MathF.Tau / 12f;
                var baseP = Snap(b, V(MathF.Sin(a) * 0.2f, 0.56f, 0.04f + MathF.Cos(a) * 0.15f), 0.02f);
                var d = Vector3.Normalize(V(MathF.Sin(a) * 0.9f, 0.8f, MathF.Cos(a) * 0.5f - 0.1f));
                b.Spike(Body, baseP, baseP + d * (0.2f + 0.04f * MathF.Sin(i * 2.1f)), 0.035f, i % 2 == 0 ? pink : hot, 0.72f, Glow, 0.005f);
                if (i % 3 == 0) FlameTongue(b, Body, baseP, baseP + d * 0.26f, 0.04f, Orange, Yolk);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                var hand = Tip(b, s < 0 ? "armL" : "armR", v => MathF.Abs(v.X) + v.Y * 0.3f);
                FlameTongue(b, BoneOf(b, s < 0 ? "armL" : "armR"), hand + V(0, -0.2f, 0), hand + V(0.06f * s, 0.3f, -0.06f), 0.085f, Orange, Yolk);
                FlameTongue(b, BoneOf(b, s < 0 ? "armL" : "armR"), hand + V(0.04f * s, -0.15f, 0), hand + V(0.2f * s, 0.12f, -0.05f), 0.06f, Orange, Yolk);
            }
            Emblem(b, Body, 0f, 0.36f, 0.07f, pink, Rgb(255, 170, 120));
            GroundClouds(b, Foot(b, "legR"), 0.55f, 0.5f);
            return b;
        }

        // ------------------------------------------------------------------ Empoleon

        /// <summary>Mega Empoleon crowned with a trident of cyan crystal, gold epaulets on its shoulders and the emblem on its breastplate.</summary>
        private static PokeBuilder Empoleon()
        {
            var b = Mm(EmpoleonMega(), "Empoleon-Mega-Mmax");
            var trident = Rgb(110, 238, 244);
            int head = BoneOf(b, "head");
            var crown = Top(b, 0f, 0.0f);
            b.Spike(head, crown - V(0, 0.04f, 0), crown + V(0, 0.5f, 0.02f), 0.05f, trident, 0.6f, Glow, 0.006f);
            foreach (float s in new[] { -1f, 1f })
            {
                b.Spike(head, crown + V(0.04f * s, -0.04f, 0), crown + V(0.14f * s, 0.4f, 0.02f), 0.04f, trident, 0.6f, Glow, 0.006f);
                b.Ell(Body, Snap(b, V(0.24f * s, 0.88f, 0f), -0.01f), V(0.09f, 0.04f, 0.09f), Gold, V(0, 0, -20f * s), Metal, 0.01f);
            }
            Emblem(b, Body, 0f, 0.7f, 0.07f, Rgb(180, 250, 255), Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.6f, 0.5f);
            Bolt(b, V(-0.3f, 0.3f, 0.1f), V(-0.45f, 0.04f, 0.2f), 0.04f, Cyan);
            return b;
        }

        // ------------------------------------------------------------------ Excadrill

        /// <summary>Mega Excadrill with drills of violet crystal on both hands, ringed in turning light, and the emblem on its chest set round with spikes.</summary>
        private static PokeBuilder Excadrill()
        {
            var b = Mm(ExcadrillMega(), "Excadrill-Mega-Mmax");
            var crystal = Rgb(176, 120, 246);
            foreach (float s in new[] { -1f, 1f })
            {
                string arm = s < 0 ? "armL" : "armR";
                var hand = Tip(b, arm, v => MathF.Abs(v.X));
                var dir = Vector3.Normalize(V(0.6f * s, 0f, 0.8f));
                b.Spike(BoneOf(b, arm), hand - dir * 0.05f, hand + dir * 0.34f, 0.085f, crystal, 1f, Glow, 0.01f);
                for (int i = 0; i < 4; i++)
                    b.Torus(BoneOf(b, arm), hand + dir * (0.04f + 0.07f * i), 0.082f - 0.016f * i, 0.012f, Rgb(220, 180, 255), Euler(dir), mat: Glow);
            }
            Emblem(b, Body, 0f, 0.3f, 0.06f, Rgb(200, 150, 255), Rgb(255, 255, 255));
            var at = Front(b, 0f, 0.3f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * MathF.Tau / 10f;
                b.Spike(Body, at + V(MathF.Cos(a) * 0.07f, MathF.Sin(a) * 0.07f, 0.004f), at + V(MathF.Cos(a) * 0.105f, MathF.Sin(a) * 0.105f, 0.01f), 0.014f, Rgb(210, 160, 255), 1f, Glow, 0.003f);
            }
            GroundClouds(b, Foot(b, "legR"), 0.4f, 0.4f, 1.3f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Feraligatr

        /// <summary>Mega Feraligatr with clusters of violet and orange crystal grown on its back, tail and forearms, a crest of cyan glass, grey stone blocks at its joints.</summary>
        private static PokeBuilder Feraligatr()
        {
            var b = Mm(FeraligatrMega(), "Feraligatr-Mega-Mmax");
            var orange = Rgb(248, 130, 70);
            var stone = Rgb(138, 140, 144);
            Crystals(b, Body, Back(b, 0f, 0.82f), V(0, 0.8f, -0.8f), 6, 0.36f, 0.06f, Amethyst, orange, 0.8f);
            int tail = BoneOf(b, "tail");
            Crystals(b, tail, Back(b, 0f, 0.3f), V(0, 1f, -0.4f), 4, 0.2f, 0.04f, Amethyst, orange, 0.6f);
            Crystals(b, tail, Back(b, 0f, 0.22f) + V(0, 0, 0.1f), V(0, 1f, -0.3f), 3, 0.17f, 0.035f, Amethyst, orange, 0.5f);
            Crystals(b, BoneOf(b, "head"), V(0, 0.9f, 0.04f), V(0, 0.6f, -0.9f), 4, 0.2f, 0.04f, Cyan, Rgb(150, 240, 250), 0.7f);
            foreach (float s in new[] { -1f, 1f })
            {
                Crystals(b, BoneOf(b, s < 0 ? "armL" : "armR"), Side(b, s, 0.54f, 0.04f), V(s, 0.8f, 0f), 3, 0.17f, 0.035f, Amethyst, orange, 0.5f);
                b.Box(Body, Snap(b, V(0.28f * s, 0.78f, 0f), 0.01f), V(0.06f, 0.035f, 0.05f), 0.012f, stone, V(0, 0, 25f * s), Shell, 0.008f);
                b.Box(Body, Snap(b, V(0.13f * s, 0.26f, 0.1f), 0.01f), V(0.06f, 0.07f, 0.04f), 0.012f, stone, V(0, 0, 0), Shell, 0.008f);
            }
            Emblem(b, Body, 0f, 0.62f, 0.06f, Rgb(250, 140, 220), Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.6f, 0.65f);
            Wisp(b, V(-0.3f, 0.8f, -0.05f), V(-1f, 0.5f, -0.2f), 0.35f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Froslass

        /// <summary>Mega Froslass with a crown of ice crystal, a wide red ribbon winding down round its body from the throat and a snowflake of crystal on its gown.</summary>
        private static PokeBuilder Froslass()
        {
            var b = Mm(FroslassMega(), "Froslass-Mega-Mmax");
            var ribbon = Rgb(150, 28, 56);
            int head = BoneOf(b, "head");
            var crown = Top(b, 0f, 0.0f);
            Crystals(b, head, crown - V(0, 0.03f, 0), V(0, 1f, 0), 8, 0.3f, 0.045f, Ice, Cyan, 0.7f);
            var path = new Vector3[22];
            for (int i = 0; i < path.Length; i++)
            {
                float t = i / (float)(path.Length - 1);
                float a = 0.4f + t * 1.7f * MathF.PI;
                float r = 0.12f + 0.2f * MathF.Sin(t * 2.4f) + 0.08f * t;
                path[i] = V(MathF.Sin(a) * r, 0.92f - 0.74f * t, MathF.Cos(a) * r * 0.9f);
            }
            b.Tube(Body, path, 0.04f, 0.04f, ribbon, Glow, 0.008f);
            var flake = Front(b, 0f, 0.4f);
            b.Mark(Body, flake, V(0, 0, 1), 0.07f, 0.07f, Ice, MarkShape.Star);
            Emblem(b, Body, 0f, 0.94f, 0.04f, Rgb(250, 150, 130), Rgb(255, 240, 200));
            GroundClouds(b, V(0.0f, 0.2f, 0.0f), 0.5f, 0.5f, 1.4f, 0.055f);
            return b;
        }

        // ------------------------------------------------------------------ Gallade

        /// <summary>Mega Gallade with the blades of its arms grown into long swords of glowing crystal, a cape of pale violet light behind it, the emblem on its chest.</summary>
        private static PokeBuilder Gallade()
        {
            var b = Mm(GalladeMega(), "Gallade-Mega-Mmax");
            var blade = Rgb(140, 240, 248);
            var cape = Rgb(198, 176, 244);
            foreach (float s in new[] { -1f, 1f })
            {
                string arm = s < 0 ? "armL" : "armR";
                var hand = Tip(b, arm, v => MathF.Abs(v.X));
                var dir = Vector3.Normalize(hand - b.Model.Skeleton[BoneOf(b, arm)].Joint + V(0, 0.4f * 0.3f, 0.1f));
                Glass(b, BoneOf(b, arm), hand - dir * 0.3f, hand + dir * 0.34f + V(0, 0.1f, 0.04f), 0.07f, blade, V(0, 0, 1), 0.2f);
                Vane(b, Body, V(0.04f * s, 0.8f, -0.06f), V(0.42f * s, 0.46f, -0.24f), 0.14f, cape, V(0, 0, 1), 0.4f, Glow);
            }
            Vane(b, Body, V(0, 0.8f, -0.06f), V(0, 0.28f, -0.3f), 0.14f, cape, V(0, 0, 1), 0.4f, Glow);
            Emblem(b, Body, 0f, 0.74f, 0.04f, Rgb(255, 140, 120), Rgb(255, 230, 160));
            GroundClouds(b, Foot(b, "legR"), 0.55f, 0.5f);
            return b;
        }

        // ------------------------------------------------------------------ Garchomp

        /// <summary>Mega Garchomp with scythes of violet crystal curving up from its forearms, a crystal fin up its back, the emblem on its chest.</summary>
        private static PokeBuilder Garchomp()
        {
            var b = Mm(GarchompMega(), "Garchomp-Mega-Mmax");
            var light = Rgb(200, 150, 250);
            foreach (float s in new[] { -1f, 1f })
            {
                string arm = s < 0 ? "armL" : "armR";
                var hand = Tip(b, arm, v => MathF.Abs(v.X));
                Glass(b, BoneOf(b, arm), hand - V(0, 0.02f, 0), hand + V(0.28f * s, 0.46f, 0.1f), 0.1f, Amethyst, V(1f, 0f, 0f), 0.2f);
                Glass(b, BoneOf(b, arm), hand + V(0.02f * s, 0, 0), hand + V(0.14f * s, 0.34f, 0.14f), 0.06f, light, V(1f, 0f, 0f), 0.2f);
            }
            Glass(b, Body, V(0, 0.9f, -0.1f), V(0, 1.4f, -0.3f), 0.12f, Amethyst, V(1f, 0f, 0f), 0.2f);
            Glass(b, Body, V(0, 0.84f, -0.12f), V(0, 1.2f, -0.34f), 0.08f, light, V(1f, 0f, 0f), 0.2f);
            Emblem(b, Body, 0f, 0.82f, 0.05f, Rgb(255, 200, 120), Rgb(120, 220, 255));
            GroundClouds(b, Foot(b, "legR"), 0.6f, 0.6f);
            Bolt(b, V(-0.25f, 0.25f, 0.2f), V(-0.4f, 0.04f, 0.3f), 0.04f, Cyan);
            Bolt(b, V(0.25f, 0.25f, 0.2f), V(0.4f, 0.04f, 0.3f), 0.04f, Cyan);
            return b;
        }

        // ------------------------------------------------------------------ Gardevoir

        /// <summary>Mega Gardevoir whose gown has become a dome of stained glass, ribbed with rings of light and set with a rose window, a red glow about it, the emblem at her bodice.</summary>
        private static PokeBuilder Gardevoir()
        {
            var b = Mm(GardevoirMega(), "Gardevoir-Mega-Mmax");
            var glass = Rgb(196, 150, 240);
            var pink = Rgb(246, 150, 210);
            var teal = Rgb(130, 230, 240);
            b.Ell(Body, V(0, 0.34f, -0.05f), V(0.4f, 0.38f, 0.4f), glass, mat: Glow, blend: 0.02f);
            foreach (var (y, r) in new[] { (0.1f, 0.4f), (0.24f, 0.37f), (0.38f, 0.28f), (0.5f, 0.17f) })
                b.Torus(Body, V(0, y, -0.05f), r, 0.012f, Rgb(250, 220, 250), V(0, 0, 0), mat: Glow, blend: 0.004f);
            // The rose window: rings and a cross of petals on the dome's front
            var window = V(0, 0.34f, 0.34f);
            b.Torus(Body, window, 0.13f, 0.012f, pink, V(90f, 0, 0), mat: Glow, blend: 0.004f);
            b.Torus(Body, window, 0.07f, 0.01f, teal, V(90f, 0, 0), mat: Glow, blend: 0.004f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f;
                b.Ell(Body, window + V(MathF.Cos(a) * 0.1f, MathF.Sin(a) * 0.1f, 0.0f), V(0.03f, 0.012f, 0.01f), i % 2 == 0 ? pink : teal, V(0, 0, a / Degree), Glow, 0.004f);
            }
            Emblem(b, Body, 0f, 0.8f, 0.04f, Rgb(250, 80, 90), Rgb(255, 230, 230));
            GroundClouds(b, V(0.34f, 0.2f, 0f), 0.55f, 0.55f, 1.4f, 0.05f);
            Wisp(b, V(-0.2f, 0.75f, 0f), V(-1f, 0.5f, 0f), 0.3f, 0.035f);
            return b;
        }

        // ------------------------------------------------------------------ Gengar

        /// <summary>Mega Gengar sunk to its belly in a pool of shadow with four more arms of shadow reaching out of it, claws of violet crystal, a gem on its brow, its belly flushed crimson.</summary>
        private static PokeBuilder Gengar()
        {
            var b = Mm(GengarMega(), "Gengar-Mega-Mmax");
            var shadow = Rgb(40, 26, 66);
            var claw = Rgb(176, 100, 240);
            b.PaintEll(Body, V(0, 0.3f, 0.18f), V(0.24f, 0.22f, 0.2f), Rgb(150, 36, 64), default, 0.04f);
            for (int i = 0; i < 4; i++)
            {
                float s = i % 2 == 0 ? -1f : 1f;
                float up = i < 2 ? 0.3f : 0.0f;
                var from = Snap(b, V(0.28f * s, 0.34f + up * 0.3f, -0.06f), 0.03f);
                var path = Smooth(4, from, V((0.46f + 0.04f * i) * s, 0.4f + up, 0.0f), V((0.6f + 0.02f * i) * s, 0.64f + up, 0.1f), V((0.62f + 0.02f * i) * s, 0.92f + up * 0.5f, 0.14f));
                b.Tube(Body, path, 0.05f, 0.032f, shadow, blend: 0.012f);
                var hand = path[^1];
                b.Ell(Body, hand, V(0.045f, 0.05f, 0.03f), shadow, blend: 0.01f);
                for (int k = -1; k <= 1; k++)
                    b.Spike(Body, hand + V(0.02f * k, 0.03f, 0), hand + V(0.04f * k, 0.16f, 0.01f), 0.014f, claw, 1f, Glow, 0.003f);
            }
            b.Ell(BoneOf(b, "head"), Snap(b, V(0, 0.8f, 0.18f), -0.012f), V(0.03f, 0.05f, 0.02f), Rgb(110, 222, 244), V(0, 0, 0), Glow, 0.006f);
            Emblem(b, Body, 0f, 0.34f, 0.05f, Rgb(140, 200, 255), Rgb(220, 160, 255));
            b.Ell(Body, V(0, 0.022f, 0), V(0.46f, 0.018f, 0.42f), Rgb(34, 20, 56), blend: 0.008f);
            GroundClouds(b, V(0.12f, 0.04f, 0.0f), 0.5f, 0.46f, 1.5f, 0.05f);
            return b;
        }

        // ------------------------------------------------------------------ Glalie

        /// <summary>Mega Glalie with fangs and a horn of glowing ice crystal about its great mouth, the emblem on its brow.</summary>
        private static PokeBuilder Glalie()
        {
            var b = Mm(GlalieMega(), "Glalie-Mega-Mmax");
            var ice = Rgb(130, 232, 250);
            for (int i = 0; i < 9; i++)
            {
                float a = (-80f + 20f * i) * Degree;
                var baseP = Snap(b, V(MathF.Sin(a) * 0.25f, 0.28f - 0.02f * MathF.Abs(MathF.Sin(a)), MathF.Cos(a) * 0.25f), 0.02f);
                var dir = Vector3.Normalize(V(MathF.Sin(a) * 0.4f, i % 2 == 0 ? -0.9f : 0.9f, MathF.Cos(a)));
                b.Spike(Body, baseP, baseP + dir * (0.13f + 0.03f * MathF.Sin(i * 1.3f)), 0.04f, ice, 0.7f, Glow, 0.006f);
            }
            var horn = Snap(b, V(0, 0.64f, 0.18f), 0.04f);
            b.Spike(Body, horn, horn + V(-0.02f, 0.34f, 0.08f), 0.075f, ice, 0.75f, Glow, 0.01f);
            Emblem(b, Body, 0f, 0.6f, 0.05f, ice, Rgb(255, 255, 255));
            GroundClouds(b, V(0.0f, 0.12f, 0.0f), 0.5f, 0.5f, 1.4f, 0.055f);
            Wisp(b, V(-0.3f, 0.5f, 0f), V(-1f, 0.5f, 0f), 0.3f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Glimmora

        /// <summary>Mega Glimmora with a second ring of pink and violet petals opened in the flower's heart, the emblem glowing at its centre.</summary>
        private static PokeBuilder Glimmora()
        {
            var b = Mm(GlimmoraMega(), "Glimmora-Mega-Mmax");
            var pink = Rgb(240, 110, 196);
            var violet = Rgb(146, 90, 230);
            var c = V(0, 0.62f, 0.1f);
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < 9; i++)
                {
                    float a = (i + ring * 0.5f) * MathF.Tau / 9f;
                    float r = ring == 0 ? 0.3f : 0.2f;
                    // The petals open round the face and leave it clear
                    var from = Snap(b, c + V(MathF.Cos(a) * 0.14f, MathF.Sin(a) * 0.14f, 0.0f), 0.02f);
                    var to = c + V(MathF.Cos(a) * r, MathF.Sin(a) * r, 0.1f + 0.03f * ring);
                    Petal(b, Body, from, to, 0.06f - 0.012f * ring, (i + ring) % 2 == 0 ? pink : violet, Leaf, 0.01f);
                }
            Emblem(b, Body, 0f, 0.46f, 0.05f, Rgb(255, 120, 220), Rgb(255, 255, 255));
            GroundClouds(b, V(0.0f, 0.18f, 0.04f), 0.55f, 0.5f, 1.5f, 0.055f);
            return b;
        }

        // ------------------------------------------------------------------ Golisopod

        /// <summary>Mega Golisopod with a crystal of violet rising from its brow, its great fist ringed with violet spikes, the emblem glowing on its chest.</summary>
        private static PokeBuilder Golisopod()
        {
            var b = Mm(GolisopodMega(), "Golisopod-Mega-Mmax");
            Crystals(b, BoneOf(b, "head"), Top(b, 0f, 0.07f), V(0, 1f, -0.15f), 5, 0.34f, 0.055f, Amethyst, Rgb(190, 140, 250), 0.5f);
            var hand = Tip(b, "armR", v => v.X);
            int arm = BoneOf(b, "armR");
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f;
                var d = Vector3.Normalize(V(0.5f, MathF.Sin(a), MathF.Cos(a)));
                var tipP = hand + d * 0.17f;
                tipP.Y = MathF.Max(tipP.Y, 0.06f);
                b.Spike(arm, Snap(b, hand + d * 0.05f, 0.015f), tipP, 0.034f, i % 2 == 0 ? Amethyst : Rgb(190, 140, 250), 0.8f, Glow, 0.005f);
            }
            Emblem(b, Body, 0f, 0.46f, 0.055f, Rgb(200, 150, 255), Rgb(255, 255, 255));
            GroundClouds(b, Foot(b, "legR"), 0.55f, 0.5f);
            Wisp(b, V(-0.3f, 0.5f, 0f), V(-1f, 0.5f, 0.2f), 0.3f, 0.04f);
            return b;
        }

        // ------------------------------------------------------------------ Greninja

        /// <summary>Mega Greninja with a great four-pointed shuriken of churning water standing behind it, the emblem on its chest.</summary>
        private static PokeBuilder Greninja()
        {
            var b = Mm(GreninjaMega(), "Greninja-Mega-Mmax");
            var water = Rgb(110, 236, 246);
            var foam = Rgb(200, 250, 254);
            // Standing on the back, clear of the head
            var c = Back(b, 0f, 0.5f) + V(0f, 0.08f, -0.05f);
            foreach (var (x, y) in new[] { (1f, 1f), (-1f, 1f), (-1f, -1f), (1f, -1f) })
            {
                var d = Vector3.Normalize(V(x, y, 0f));
                Blade(b, Body, c, c + d * 0.7f, 0.2f, water, V(0, 0, 1), 0.45f, Glow);
                Blade(b, Body, c, c + d * 0.5f, 0.1f, foam, V(0, 0, 1), 0.45f, Glow);
            }
            b.Ell(Body, c, V(0.16f, 0.16f, 0.06f), water, mat: Glow, blend: 0.02f);
            Emblem(b, Body, 0f, 0.5f, 0.045f, Rgb(160, 240, 255), Rgb(255, 255, 255));
            GroundClouds(b, V(0.0f, 0.16f, 0.0f), 0.5f, 0.5f, 1.4f, 0.045f);
            return b;
        }

        // ------------------------------------------------------------------ Gyarados

        /// <summary>Mega Gyarados with crystals of violet and cyan grown from its spine along the whole of its coils, standing in a whirlpool, the emblem on its brow.</summary>
        private static PokeBuilder Gyarados()
        {
            var b = Mm(GyaradosMega(), "Gyarados-Mega-Mmax");
            for (int i = 2; i <= 8; i++)
            {
                var j = b.Model.Skeleton[i].Joint;
                Crystals(b, i, j + V(0, 0.06f, 0), V(0, 1f, -0.2f), 3, 0.2f - 0.01f * (i % 3), 0.04f, Cyan, Amethyst, 0.6f);
            }
            Crystals(b, BoneOf(b, "head"), V(0, 1.0f, 0.0f), V(0, 0.8f, -0.8f), 3, 0.2f, 0.04f, Cyan, Amethyst, 0.6f);
            b.Ell(Body, V(0, 0.03f, -0.06f), V(0.5f, 0.022f, 0.5f), Rgb(96, 122, 150), blend: 0.01f);
            b.Torus(Body, V(0, 0.04f, -0.06f), 0.48f, 0.02f, Rgb(210, 230, 244), mat: Glow);
            Emblem(b, BoneOf(b, "head"), 0f, 0.99f, 0.04f, Cyan, Rgb(220, 160, 255));
            GroundClouds(b, V(0.04f, 0.1f, -0.2f), 0.55f, 0.55f, 1.5f, 0.05f);
            return b;
        }

        // ------------------------------------------------------------------ Heracross

        /// <summary>Mega Heracross with a crown of violet crystal standing over its head inside a halo of light, the emblem on its chest.</summary>
        private static PokeBuilder Heracross()
        {
            var b = Mm(HeracrossMega(), "Heracross-Mega-Mmax");
            int head = BoneOf(b, "head");
            float topY = 0.9f;
            for (int i = 0; i < 11; i++)
            {
                float a = i * MathF.Tau / 11f;
                var baseP = Top(b, MathF.Sin(a) * 0.13f, 0.02f + MathF.Cos(a) * 0.12f);
                topY = MathF.Max(topY, baseP.Y);
                var d = Vector3.Normalize(V(MathF.Sin(a) * 0.9f, 1f, MathF.Cos(a) * 0.3f));
                float l = 0.3f + 0.12f * MathF.Cos(a * 1.0f) * MathF.Cos(a) + 0.04f * MathF.Sin(i * 2.3f);
                b.Spike(head, baseP - d * 0.03f, baseP + d * l, 0.055f, i % 2 == 0 ? Amethyst : Rgb(196, 150, 250), 0.72f, Glow, 0.006f);
            }
            b.Torus(head, V(0, topY - 0.01f, 0.02f), 0.15f, 0.022f, Rgb(255, 170, 120), mat: Glow, blend: 0.006f);
            Emblem(b, Body, 0f, 0.46f, 0.06f, Rgb(255, 190, 160), Rgb(220, 170, 255));
            GroundClouds(b, Foot(b, "legR"), 0.55f, 0.5f);
            Bolt(b, V(-0.3f, 0.3f, 0.1f), V(-0.42f, 0.04f, 0.2f), 0.04f, Cyan);
            return b;
        }

        // ------------------------------------------------------------------ Houndoom

        /// <summary>Mega Houndoom with flames and violet crystals standing up along its back, white bracers on its forelegs, the emblem on its brow.</summary>
        private static PokeBuilder Houndoom()
        {
            var b = Mm(HoundoomMega(), "Houndoom-Mega-Mmax");
            var hot = Rgb(250, 140, 60);
            var purple = Rgb(176, 110, 240);
            int body = Body;
            foreach (var (x, z, h) in new[] { (0f, -0.0f, 0.34f), (-0.07f, -0.08f, 0.3f), (0.07f, -0.08f, 0.3f), (0f, -0.16f, 0.26f), (-0.05f, -0.22f, 0.22f), (0.05f, -0.22f, 0.22f) })
            {
                var baseP = Top(b, x, z);
                FlameTongue(b, body, baseP - V(0, 0.02f, 0), baseP + V(x * 1.4f, h, -0.04f), 0.05f, hot, Yolk);
                b.Spike(body, baseP - V(0, 0.02f, 0), baseP + V(x * 1.2f, h * 0.7f, -0.03f), 0.03f, purple, 0.7f, Glow, 0.005f);
            }
            foreach (float s in new[] { -1f, 1f })
                b.Torus(BoneOf(b, s < 0 ? "legL" : "legR"), V(0.08f * s, 0.12f, 0.12f), 0.04f, 0.022f, Rgb(232, 230, 238), mat: Shell);
            Emblem(b, BoneOf(b, "head"), 0f, 0.62f, 0.035f, Rgb(250, 90, 90), Rgb(255, 230, 190));
            GroundClouds(b, Foot(b, "legL"), 0.55f, 0.6f);
            Wisp(b, V(-0.1f, 0.6f, 0.0f), V(-1f, 0.6f, -0.1f), 0.3f, 0.035f);
            return b;
        }
    }
}
