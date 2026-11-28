using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 15 (plan 46 · CP12a): the first half of the Tale Forms, "<Species>-Tale", what a Pokémon bound to a tale
// wakes into while its trainer tells it (Data/tales.json's "forms"; docs/plans/decisions/CP-story-chapters.md, section 5,
// tales 1 to 6 and the first two of tale 7). Each is its species' own sculpt taken whole, built by the very call its
// species' row makes, and lit by its tale: the Sânziene's midsummer gold, Făt-Frumos' white heat and golden hooves, the
// Spiriduș' hearth fire, the Ielele's violet ring of light, Baba Dochia's frost, the Uriași' standing stones and Muma
// Pădurii's moss and foxfire. What is laid on the body is set on the species' own surface and takes the bone of the
// nearest of its shapes (as plan 06 · R20c's Mega Dynamax sculpts do), so nothing added takes a head's weight from its
// eyes. The designs are ours and the user's to approve.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames15() => Tales15.Names;

    private static PokeBuilder? CreateCarpathia15(string species) => Tales15.Make(species);

    private static class Tales15
    {
        public static readonly string[] Names =
        {
            "Sanziana-Tale", "Buburoza-Tale", "Fierander-Tale", "Hutulmare-Tale", "Spiriduke-Tale", "Harciogre-Tale",
            "Buciumbill-Tale", "Privigale-Tale", "Ielele-Tale", "Babadochia-Tale", "Capraneagra-Tale", "Zimbrunt-Tale",
            "Ursabrun-Tale", "Hategant-Tale", "Codrulynx-Tale", "Hribarde-Tale"
        };

        /// <summary>The sculpt of a Tale Form by its name in capitals, null for any other.</summary>
        public static PokeBuilder? Make(string form)
        {
            var b = form switch
            {
                "SANZIANA-TALE" => SanzianaTale(),
                "BUBUROZA-TALE" => BuburozaTale(),
                "FIERANDER-TALE" => FieranderTale(),
                "HUTULMARE-TALE" => HutulmareTale(),
                "SPIRIDUKE-TALE" => SpiridukeTale(),
                "HARCIOGRE-TALE" => HarciogreTale(),
                "BUCIUMBILL-TALE" => BuciumbillTale(),
                "PRIVIGALE-TALE" => PrivigaleTale(),
                "IELELE-TALE" => IeleleTale(),
                "BABADOCHIA-TALE" => BabadochiaTale(),
                "CAPRANEAGRA-TALE" => CapraneagraTale(),
                "ZIMBRUNT-TALE" => ZimbruntTale(),
                "URSABRUN-TALE" => UrsabrunTale(),
                "HATEGANT-TALE" => HategantTale(),
                "CODRULYNX-TALE" => CodrulynxTale(),
                "HRIBARDE-TALE" => HribardeTale(),
                _ => null
            };
            return b == null ? null : Done(b);
        }

        // ------------------------------------------------------------------ the tales' colours

        private static readonly Color Bedstraw = Rgb(255, 222, 92);
        private static readonly Color MidsummerGold = Rgb(250, 206, 84);
        private static readonly Color WhiteHot = Rgb(255, 240, 200);
        private static readonly Color RedHot = Rgb(232, 98, 44);
        private static readonly Color HeroGold = Rgb(236, 190, 82);
        private static readonly Color Ember = Rgb(236, 118, 46);
        private static readonly Color SpriteRed = Rgb(206, 52, 40);
        private static readonly Color Coin = Rgb(242, 198, 78);
        private static readonly Color NightViolet = Rgb(104, 82, 168);
        private static readonly Color DanceLight = Rgb(216, 196, 255);
        private static readonly Color Moonlight = Rgb(232, 236, 255);
        private static readonly Color Frost = Rgb(226, 240, 252);
        private static readonly Color Ice = Rgb(190, 226, 248);
        private static readonly Color DeepIce = Rgb(150, 196, 232);
        private static readonly Color Granite = Rgb(148, 142, 134);
        private static readonly Color DarkGranite = Rgb(110, 104, 98);
        private static readonly Color Lichen = Rgb(150, 160, 112);
        private static readonly Color DeepMoss = Rgb(64, 104, 50);
        private static readonly Color Foxfire = Rgb(156, 246, 150);

        // ------------------------------------------------------------------ taking the species whole

        /// <summary>A species' sculpt taken whole as the start of its Tale Form: renamed, as big in the picture as it was.</summary>
        private static PokeBuilder Take(PokeBuilder species, string form)
        {
            var b = Become(species, form, species.Model.Fill);
            BaseCounts.Remove(b);
            BaseCounts.Add(b, b.Sdf.Shapes.Count);
            return b;
        }

        /// <summary>
        /// What was added moves with the part of the species it grows from (a stone on a hump with the body, a flower of a
        /// crown with the head), and nothing set near the face takes the head's weight from the eyes.
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

        /// <summary>Where a bone's joint was sculpted against where the finished build has it: what a lift or a grounding moved everything by.</summary>
        private static Vector3 Moved(PokeBuilder b, Vector3 headSculptedAt) => Shifted(b, "head", headSculptedAt);

        // ------------------------------------------------------------------ finding the surface (as MegaMax1's)

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PokeBuilder, object> BaseCounts = new();

        /// <summary>The signed distance to the species' own surface, never to anything added since.</summary>
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

        /// <summary>The bone of the species' own shape nearest to <paramref name="p"/>.</summary>
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

        /// <summary>The first point of the surface met walking from <paramref name="from"/> along <paramref name="dir"/>, or null.</summary>
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

        /// <summary>Which way the surface faces at a point on it.</summary>
        private static Vector3 Normal(PokeBuilder b, Vector3 p)
        {
            const float e = 0.008f;
            var g = V(Skin(b, p + V(e, 0, 0)) - Skin(b, p - V(e, 0, 0)),
                Skin(b, p + V(0, e, 0)) - Skin(b, p - V(0, e, 0)),
                Skin(b, p + V(0, 0, e)) - Skin(b, p - V(0, 0, e)));
            return g.LengthSquared() < 1e-12f ? V(0, 1, 0) : Vector3.Normalize(g);
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

        /// <summary>The top of the body over the place (<paramref name="x"/>, <paramref name="z"/>), sunk a little.</summary>
        private static Vector3 Top(PokeBuilder b, float x, float z, float sink = 0.01f)
        {
            var (lo, hi) = b.Sdf.Bounds();
            var at = Hit(b, V(x, hi.Y + 0.05f, z), V(0, -1, 0), hi.Y - lo.Y + 0.1f) ?? V(x, 0.5f, z);
            return at - V(0, sink, 0);
        }

        /// <summary>The underside of the body under the place (<paramref name="x"/>, <paramref name="z"/>), looking up from <paramref name="below"/>.</summary>
        private static Vector3 Under(PokeBuilder b, float x, float z, float below)
        {
            var (_, hi) = b.Sdf.Bounds();
            var at = Hit(b, V(x, below, z), V(0, 1, 0), hi.Y - below) ?? V(x, below, z);
            return at + V(0, 0.008f, 0);
        }

        // ------------------------------------------------------------------ what the tales lay on a body

        /// <summary>Changes colours on every shape and decal (C14Swap's, for the whole body).</summary>
        private static void Swap(PokeBuilder b, params (Color from, Color to)[] map) => C14Swap(b, null, map);

        /// <summary>Changes colours on the shapes and decals of these bones alone.</summary>
        private static void SwapOn(PokeBuilder b, string[] bones, params (Color from, Color to)[] map) => C14Swap(b, C14On(b, bones), map);

        /// <summary>A rock of the giants or a slab of frost laid on the surface at <paramref name="near"/>, turned along the surface.</summary>
        private static void Stone(PokeBuilder b, Vector3 near, Vector3 half, Color color, float yaw, SurfaceMaterial mat)
        {
            var at = Snap(b, near, half.Y * 0.45f);
            var n = Normal(b, at);
            var rot = Euler(n) + V(0, yaw, 0);
            b.Box(Body, at + n * half.Y * 0.3f, half, MathF.Min(half.X, half.Y) * 0.45f, color, rot, mat, 0.012f);
        }

        /// <summary>An icicle hanging from the underside found over (<paramref name="x"/>, <paramref name="z"/>).</summary>
        private static void Icicle(PokeBuilder b, float x, float z, float below, float length, float r)
        {
            var root = Under(b, x, z, below);
            b.Spike(Body, root + V(0, 0.01f, 0), root - V(0, length, 0), r, Ice, mat: Shell, blend: 0.006f);
        }

        /// <summary>A small mushroom of foxfire standing on the surface at <paramref name="near"/>.</summary>
        private static void Glowcap(PokeBuilder b, Vector3 near, float size)
        {
            var at = Snap(b, near, 0.006f);
            var n = Normal(b, at);
            var up = Vector3.Normalize(n + V(0, 1.2f, 0));
            b.Limb(Body, at - up * 0.008f, at + up * size * 1.6f, size * 0.32f, size * 0.26f, Rgb(214, 236, 200), Leaf, 0.006f);
            b.Ell(Body, at + up * size * 1.7f, V(size, size * 0.5f, size), Foxfire, Euler(up), Glow, 0.006f);
        }

        /// <summary>A ring of little flowers lying round a crown, each joined to the ring and the ring sunk into the head.</summary>
        private static void Wreath(PokeBuilder b, int bone, Vector3 center, float r, int count, float flower, Color a, Color c)
        {
            b.Torus(bone, center, r, flower * 0.45f, Rgb(118, 150, 66), mat: Leaf, blend: 0.006f);
            for (int i = 0; i < count; i++)
            {
                float t = i * MathF.Tau / count;
                var at = center + V(MathF.Sin(t) * r, 0.004f, MathF.Cos(t) * r);
                b.Ell(bone, at, V(flower, flower * 0.8f, flower), i % 2 == 0 ? a : c, mat: Glow, blend: 0.004f);
            }
        }

        // ------------------------------------------------------------------ 1 · the Sânziene's midsummer

        /// <summary>Sanziana in the midsummer night: her crown of bedstraw lit and grown into a wreath of sprigs, her hair and wings gone to gold.</summary>
        private static PokeBuilder SanzianaTale()
        {
            var b = Take(Sanziana(), "Sanziana-Tale");
            var d = Moved(b, V(0, 0.78f, 0));
            Swap(b, (Rgb(236, 238, 230), Rgb(252, 240, 188)), (Rgb(240, 200, 96), Rgb(252, 208, 76)), (Rgb(230, 182, 52), Bedstraw));
            C14Material(b, Bedstraw, Glow);
            // The wreath of sânziene: sprigs of bedstraw standing up round the crown, a bloom of light at each tip
            int head = BoneOf(b, "head");
            var c = V(0, 0.87f, 0.01f) + d;
            for (int i = 0; i < 9; i++)
            {
                float a = (i + 0.5f) * MathF.Tau / 9f;
                var root = Snap(b, c + V(MathF.Sin(a) * 0.066f, 0.06f, MathF.Cos(a) * 0.062f), 0.006f);
                var tip = root + V(MathF.Sin(a) * 0.02f, 0.06f + 0.012f * (i % 3), MathF.Cos(a) * 0.02f);
                b.Spike(head, root, tip, 0.009f, Rgb(124, 160, 70), mat: Leaf, blend: 0.004f);
                b.Ell(head, tip, V(0.016f, 0.014f, 0.016f), i % 2 == 0 ? Bedstraw : MidsummerGold, mat: Glow, blend: 0.004f);
            }
            return Lift(b);
        }

        /// <summary>Buburoza at midsummer: its wing cases gone from red rose to bedstraw gold, its spots lit, its wings warm gold, a wreath of little blooms on its head.</summary>
        private static PokeBuilder BuburozaTale()
        {
            var b = Take(Cp1Buburoza(), "Buburoza-Tale");
            var d = Moved(b, V(0, 0.4f, 0.02f));
            Swap(b, (Rgb(206, 72, 56), Rgb(238, 184, 64)), (Rgb(170, 50, 44), Rgb(208, 132, 44)), (Rgb(232, 206, 120), WhiteHot), (Rgb(246, 196, 214), Rgb(255, 232, 158)));
            int head = BoneOf(b, "head");
            var hc = V(0, 0.48f, 0.04f) + d;
            Wreath(b, head, hc + V(0, 0.058f, -0.012f), 0.074f, 10, 0.014f, Bedstraw, Rgb(255, 250, 230));
            return Lift(b);
        }

        // ------------------------------------------------------------------ 2 · Făt-Frumos' youth without age

        /// <summary>Fierander forging the hero's sword: its cracks gone white-hot, its iron glowing red, its eyes white fire.</summary>
        private static PokeBuilder FieranderTale()
        {
            var b = Take(Cp1Hearth(true), "Fierander-Tale");
            Swap(b, (Rgb(250, 140, 40), WhiteHot), (Rgb(200, 90, 30), Rgb(255, 214, 140)), (Rgb(132, 136, 144), RedHot), (Rgb(96, 98, 106), Rgb(196, 70, 36)),
                (Rgb(172, 176, 184), WhiteHot));
            C14Material(b, RedHot, Glow);
            C14Material(b, WhiteHot, Glow);
            // Sparks struck off the anvil, set into the plates of its back
            float k = 1.25f;
            foreach (var (x, y) in new[] { (-0.09f, 0.64f), (0.08f, 0.56f), (-0.07f, 0.47f), (0.09f, 0.42f), (0f, 0.36f) })
            {
                var at = Snap(b, V(x, y, -0.2f) * k, 0.004f);
                b.Ell(Body, at, V(0.016f, 0.016f, 0.016f) * k, WhiteHot, mat: Glow, blend: 0.006f);
            }
            return b;
        }

        /// <summary>Hutulmare as the hero's steed fed on embers: a mane and tail of gold fire, golden hooves, a white star on its brow.</summary>
        private static PokeBuilder HutulmareTale()
        {
            var b = Take(Hutulmare(), "Hutulmare-Tale");
            SwapOn(b, new[] { "head", "tail" }, (Rgb(38, 34, 38), HeroGold));
            C14Material(b, HeroGold, Glow);
            Swap(b, (Rgb(152, 98, 60), Rgb(186, 134, 78)), (Rgb(70, 62, 62), Coin));
            C14Material(b, Coin, Metal);
            // The white star on its brow
            int head = BoneOf(b, "head");
            var c = V(0, 0.87f, 0.41f);
            b.PaintEll(head, c + V(0, 0.07f, 0.045f), V(0.032f, 0.03f, 0.03f), Rgb(250, 246, 236), soft: 0.006f);
            return b;
        }

        // ------------------------------------------------------------------ 3 · the Spiriduș' house sprite

        /// <summary>Spiriduke with the hearth's fire in it: its smoke turned to flame, its waistcoat ember red, its lamp blazing, its gold bright.</summary>
        private static PokeBuilder SpiridukeTale()
        {
            var b = Take(Spiriduke(), "Spiriduke-Tale");
            var d = Moved(b, V(0, 0.55f, 0));
            Swap(b, (Rgb(150, 142, 142), Ember), (Rgb(150, 44, 46), SpriteRed), (Rgb(222, 178, 82), Coin));
            C14Material(b, Ember, Glow);
            int arm = BoneOf(b, "armR");
            Flame(b, arm, V(0.355f, 0.49f, 0.16f) + d, 0.15f);
            // Embers rising off the curl of fire
            foreach (var p in new[] { V(0.04f, 0.2f, 0.0f), V(-0.04f, 0.14f, -0.03f), V(0.05f, 0.09f, -0.06f) })
            {
                var at = Snap(b, p + d, 0.004f);
                b.Ell(Body, at, V(0.016f, 0.016f, 0.016f), Rgb(255, 210, 110), mat: Glow, blend: 0.006f);
            }
            return Lift(b);
        }

        /// <summary>Harciogre keeping the sprite's hoard: its sack gone red and full of gold coins, and the sprite's red cap on its head.</summary>
        private static PokeBuilder HarciogreTale()
        {
            var b = Take(Cp1Hamster(true), "Harciogre-Tale");
            float k = 1.5f;
            Swap(b, (Rgb(178, 150, 104), Rgb(168, 52, 42)), (Rgb(214, 188, 120), Coin));
            C14Material(b, Coin, Metal);
            // Coins spilling over the sack's neck
            var bc = V(0, 0.22f, 0) * k;
            var sack = bc + V(-0.05f, 0.08f, -0.2f) * k;
            var neck = sack + V(-0.05f, 0.21f, 0) * k;
            foreach (var (dx, dz, tilt) in new[] { (0.02f, 0.02f, 20f), (-0.03f, -0.01f, -25f), (0.0f, -0.03f, 40f), (0.05f, -0.02f, -10f) })
                b.Ell(Body, neck + V(dx, 0.008f, dz) * k, V(0.022f, 0.006f, 0.022f) * k, Coin, V(tilt, 0, tilt * 0.5f), Metal, 0.004f);
            // The sprite's red cap, flopping back
            int head = BoneOf(b, "head");
            var hc = V(0, 0.46f, 0.03f) * k;
            var crown = Snap(b, hc + V(0, 0.11f, -0.03f) * k, 0.03f);
            b.Ell(head, crown, V(0.085f, 0.05f, 0.08f) * k, SpriteRed, mat: SurfaceMaterial.Cloth, blend: 0.012f);
            b.Tube(head, new[] { crown + V(0, 0.03f, -0.01f) * k, crown + V(0.02f, 0.09f, -0.06f) * k, crown + V(0.05f, 0.08f, -0.12f) * k, crown + V(0.06f, 0.04f, -0.13f) * k },
                0.05f * k, 0.018f * k, SpriteRed, SurfaceMaterial.Cloth, 0.012f);
            b.Ell(head, crown + V(0.06f, 0.035f, -0.135f) * k, V(0.02f, 0.02f, 0.02f) * k, Coin, mat: Metal, blend: 0.006f);
            return b;
        }

        // ------------------------------------------------------------------ 4 · the Ielele's night dance

        /// <summary>Buciumbill's horn ringing in a ring of light at the night dance, its mantle and crest gone violet.</summary>
        private static PokeBuilder BuciumbillTale()
        {
            var b = Take(Cp1Pelican(true), "Buciumbill-Tale");
            Swap(b, (Rgb(132, 150, 168), NightViolet), (Rgb(132, 146, 158), Rgb(186, 172, 232)), (Rgb(54, 120, 110), Rgb(70, 58, 128)), (Rgb(64, 104, 84), Rgb(96, 74, 156)));
            float k = 1.3f;
            int head = BoneOf(b, "head");
            var hc = V(0, 0.7f, 0.07f) * k;
            var hr = V(0.075f, 0.07f, 0.08f) * k;
            var root2 = hc + V(0, -0.015f, hr.Z * 0.8f);
            var mouth = root2 + V(0, -0.28f, 0.42f) * k;
            var dir = Vector3.Normalize(mouth - root2);
            var bell = mouth - dir * 0.02f * k;
            // The ring of light round the horn's bell, held to it by four spokes
            var ring = bell - dir * 0.01f;
            float rr = 0.12f * k;
            b.Torus(head, ring, rr, 0.014f * k, DanceLight, Euler(dir), mat: Glow, blend: 0.004f);
            var (u, w) = Tangents(dir);
            foreach (var t in new[] { 0f, 0.25f, 0.5f, 0.75f })
            {
                var o = u * MathF.Cos(t * MathF.Tau) + w * MathF.Sin(t * MathF.Tau);
                b.Limb(head, ring + o * 0.07f * k, ring + o * rr, 0.009f * k, 0.009f * k, DanceLight, Glow, 0.004f);
            }
            return b;
        }

        private static (Vector3 u, Vector3 w) Tangents(Vector3 n)
        {
            var u = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) > 0.9f ? V(1, 0, 0) : V(0, 1, 0)));
            return (u, Vector3.Cross(n, u));
        }

        /// <summary>Privigale singing at the night dance: its russet gone violet and plum, its silver feathers moonlight that glows.</summary>
        private static PokeBuilder PrivigaleTale()
        {
            var b = Take(Cp1Privigale(), "Privigale-Tale");
            Swap(b, (Rgb(170, 104, 60), Rgb(106, 80, 132)), (Rgb(176, 86, 46), Rgb(136, 86, 186)), (Rgb(92, 52, 34), Rgb(54, 42, 86)), (Rgb(212, 220, 230), Moonlight),
                (Rgb(140, 96, 64), Rgb(196, 186, 240)));
            C14Material(b, Moonlight, Glow);
            return Lift(b);
        }

        /// <summary>The Ielele at their night dance: a ring of violet light through their joined hands, the grass they tread glowing violet, veils and hair of moonlit lilac.</summary>
        private static PokeBuilder IeleleTale()
        {
            var b = Take(C13Ielele(), "Ielele-Tale");
            var d = Moved(b, V(0, 0.52f, 0.2f));
            Swap(b, (Rgb(150, 236, 176), Rgb(196, 152, 255)), (Rgb(242, 242, 252), Rgb(240, 232, 255)), (Rgb(222, 224, 246), Rgb(214, 202, 246)),
                (Rgb(176, 228, 224), Rgb(186, 160, 240)), (Rgb(200, 190, 240), Rgb(232, 206, 255)));
            // The ring of light their hands hold up between them
            b.Torus(Body, V(0, 0.43f, 0) + d, 0.124f, 0.011f, DanceLight, mat: Glow, blend: 0.004f);
            return Lift(b);
        }

        // ------------------------------------------------------------------ 5 · Baba Dochia's cold returning

        /// <summary>Babadochia as the cold comes back: her coats frosted blue, icicles on every hem, a snow crystal of light on her crook.</summary>
        private static PokeBuilder BabadochiaTale()
        {
            var b = Take(C11Dochia(true), "Babadochia-Tale");
            Swap(b, (Rgb(246, 244, 236), Frost), (Rgb(232, 224, 200), Rgb(206, 222, 238)), (Rgb(196, 198, 204), Rgb(160, 186, 220)));
            float k = 2f;
            var bc = V(0, 0.46f, -0.02f);
            var br = V(0.3f, 0.38f, 0.27f);
            // Icicles from the second hem as well as the first
            for (int t = 1; t < 3; t++)
            {
                float y = 0.14f + t * 0.15f;
                float dy = (y - bc.Y) / br.Y, ring = MathF.Sqrt(MathF.Max(0.15f, 1f - dy * dy));
                for (int i = 0; i < 11; i++)
                {
                    if ((i + t) % 2 == 1) continue;
                    float a = (i + t * 0.5f) * MathF.Tau / 11f;
                    var at = bc + V(MathF.Sin(a) * br.X * ring, y - bc.Y, MathF.Cos(a) * br.Z * ring);
                    b.Spike(Body, at - V(0, 0.01f * k, 0), at - V(0, 0.045f * k, 0), 0.01f * k, Ice, mat: Shell, blend: 0.004f);
                }
            }
            // A crystal of snow, six arms of light, on the crook's top
            var hand = bc + V((br.X + 0.03f * k), -0.02f * k, 0.09f * k);
            var star = V(hand.X + 0.02f * k - 0.03f * k, 1.13f, hand.Z);
            int arm = BoneOf(b, "armR");
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                b.Spike(arm, star, star + V(MathF.Sin(a) * 0.055f, MathF.Cos(a) * 0.055f, 0), 0.011f, Ice, mat: Glow, blend: 0.004f);
            }
            b.Ell(arm, star, V(0.02f, 0.02f, 0.02f), Frost, mat: Glow, blend: 0.004f);
            return b;
        }

        /// <summary>Capraneagra in the returning cold: horns and stone plates of ice, frost on its ridge, icicles at its beard and belly.</summary>
        private static PokeBuilder CapraneagraTale()
        {
            var b = Take(C3Capraneagra(), "Capraneagra-Tale");
            Swap(b, (Rgb(90, 98, 112), Ice), (Rgb(82, 92, 106), DeepIce), (Rgb(146, 140, 128), Rgb(206, 232, 250)));
            // Frost on its ridge and shoulders
            for (int i = 0; i < 6; i++)
            {
                float z = 0.16f - 0.06f * i;
                b.PaintEll(Body, V(0, 0.66f - 0.004f * i, z), V(0.06f, 0.04f, 0.04f), Frost, soft: 0.012f);
            }
            // Icicles under its chin and along its belly
            Icicle(b, 0f, 0.37f, 0.55f, 0.06f, 0.012f);
            Icicle(b, 0.02f, 0.34f, 0.55f, 0.045f, 0.01f);
            foreach (var (x, z) in new[] { (0.05f, 0.08f), (-0.05f, 0.0f), (0.04f, -0.08f), (-0.04f, -0.14f) })
                Icicle(b, x, z, 0.3f, 0.05f, 0.012f);
            return b;
        }

        // ------------------------------------------------------------------ 6 · the Uriași, the giants

        /// <summary>Zimbrunt as a giant's beast: boulders risen on its hump, horns and hooves of granite, the star between its horns lit amber.</summary>
        private static PokeBuilder ZimbruntTale()
        {
            var b = Take(C4Zimbrunt(), "Zimbrunt-Tale");
            Swap(b, (Rgb(40, 38, 42), Granite), (Rgb(48, 44, 46), DarkGranite), (Rgb(244, 234, 206), Rgb(252, 194, 92)));
            foreach (var (x, z, sx, sy, yaw) in new[] { (0f, 0.22f, 0.11f, 0.07f, 10f), (-0.16f, 0.12f, 0.08f, 0.055f, -20f), (0.15f, 0.1f, 0.085f, 0.05f, 25f),
                         (0.06f, 0.36f, 0.07f, 0.05f, 40f), (-0.07f, 0.0f, 0.075f, 0.05f, -35f) })
                Stone(b, Top(b, x, z), V(sx, sy, sx * 0.85f), (int)yaw % 2 == 0 ? Granite : DarkGranite, yaw, Shell);
            return b;
        }

        /// <summary>Ursabrun as a giant's bear: slabs of granite on its shoulders and hump, its grizzled mane gone to stone, its claws granite.</summary>
        private static PokeBuilder UrsabrunTale()
        {
            var b = Take(C3Ursabrun(), "Ursabrun-Tale");
            Swap(b, (Rgb(186, 186, 190), Granite), (Rgb(150, 150, 156), DarkGranite));
            foreach (var (x, z, sx, sy, yaw) in new[] { (0f, 0.12f, 0.09f, 0.055f, 0f), (0.12f, 0.08f, 0.07f, 0.05f, 30f), (-0.12f, 0.06f, 0.07f, 0.05f, -30f),
                         (0f, -0.12f, 0.08f, 0.05f, 15f) })
                Stone(b, Top(b, x, z), V(sx, sy, sx * 0.8f), Granite, yaw, Shell);
            return b;
        }

        /// <summary>Hategant as a hill of the giants: a menhir and two smaller standing stones risen out of its mound, its scutes gone to granite.</summary>
        private static PokeBuilder HategantTale()
        {
            var b = Take(Carp8Sauropod(1), "Hategant-Tale");
            Swap(b, (Rgb(150, 144, 128), Granite), (Rgb(84, 128, 52), DeepMoss));
            var top = Top(b, 0f, -0.04f, 0.04f);
            b.Box(Body, top + V(0, 0.12f, 0), V(0.05f, 0.15f, 0.04f), 0.02f, DarkGranite, V(0, 15f, 4f), Shell, 0.02f);
            foreach (var (x, z, h, yaw) in new[] { (0.09f, -0.15f, 0.08f, -20f), (-0.09f, 0.1f, 0.07f, 30f) })
            {
                var at = Top(b, x, z, 0.03f);
                b.Box(Body, at + V(0, h * 0.7f, 0), V(0.035f, h, 0.03f), 0.014f, Granite, V(0, yaw, 6f), Shell, 0.015f);
            }
            return b;
        }

        // ------------------------------------------------------------------ 7 · Muma Pădurii's forest closing

        /// <summary>Codrulynx cloaked in the old forest's moss: a mantle of moss down its back and flanks, lichen grown long at its chin, foxfire on its shoulders.</summary>
        private static PokeBuilder CodrulynxTale()
        {
            var b = Take(Cp1Lynx(2), "Codrulynx-Tale");
            Swap(b, (Rgb(170, 186, 140), Lichen));
            var bc = V(0, 0.54f, -0.05f);
            var br = V(0.19f, 0.17f, 0.35f);
            // The mantle of moss, draped from the spine
            b.Ell(Body, bc + V(0, br.Y * 0.42f, -0.02f), V(br.X * 1.06f, br.Y * 0.6f, br.Z * 0.92f), DeepMoss, mat: Leaf, blend: 0.02f);
            FurTufts(b, Body, bc + V(0, br.Y * 0.3f, -0.02f), V(br.X * 1.08f, br.Y * 0.72f, br.Z * 0.9f), 26, 0.07f, 0.024f, DeepMoss, 0.9f, -0.35f, 0.75f);
            // Foxfire caps on its shoulders and haunch
            foreach (var p in new[] { V(0.14f, 0.7f, 0.18f), V(-0.15f, 0.69f, 0.14f), V(0.15f, 0.66f, -0.3f) })
                Glowcap(b, p, 0.02f);
            return b;
        }

        /// <summary>Hribarde as the forest closes: foxfire caps on its brim, its cracks glowing green, moss grown over its stem.</summary>
        private static PokeBuilder HribardeTale()
        {
            var b = Take(Hribarde(), "Hribarde-Tale");
            var d = Moved(b, V(0, 0.64f, 0));
            Swap(b, (Rgb(96, 50, 30), Foxfire));
            var cc = V(0, 0.83f, 0) + d;
            var cr = V(0.5f, 0.2f, 0.44f);
            foreach (var (x, z, s) in new[] { (-0.12f, 0.2f, 0.028f), (0.2f, 0.18f, 0.024f), (-0.32f, -0.02f, 0.026f), (0.0f, -0.32f, 0.022f), (0.3f, -0.18f, 0.024f) })
            {
                var dir = V(x / cr.X, 0.65f, z / cr.Z);
                Glowcap(b, Out(cc, cr, default, dir), s);
            }
            var bc = V(0, 0.38f, 0) + d;
            var br = V(0.25f, 0.3f, 0.22f);
            foreach (var dir in new[] { V(0.8f, -0.2f, -0.4f), V(-0.85f, -0.1f, -0.3f), V(0.2f, -0.6f, -0.9f) })
                b.Ell(Body, Out(bc, br, default, dir), V(0.06f, 0.05f, 0.06f), DeepMoss, mat: Leaf, blend: 0.02f);
            return b;
        }
    }
}
