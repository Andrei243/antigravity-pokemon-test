using System;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The landmarks of the post-game's places (plan 01 · M10, part b; style guide, "Stark Mountain", "The Battle
/// Frontier" and "Snowpoint Temple"): Stark Mountain's lava and its smoking mouth, the Battle Frontier's booths, its
/// model of itself and its statues, and the giant in Snowpoint Temple's hall. No GPU calls.
/// </summary>
internal static partial class Landmarks
{
    // ------------------------------------------------------------------ Stark Mountain

    private static readonly Color LavaHot = Rgb(255, 224, 118), LavaBright = Rgb(250, 158, 50), LavaMid = Rgb(222, 94, 36), LavaDeep = Rgb(150, 44, 30);
    private static readonly Color Crust = Rgb(60, 46, 50), CrustLight = Rgb(90, 70, 70), Scorched = Rgb(76, 64, 70), ScorchedDark = Rgb(52, 42, 48);
    private static readonly Tone Basalt = Tone.Of(104, 94, 100, 130, 118, 120, 70, 62, 72);
    private static readonly Color Smoke = Rgb(120, 112, 118), SmokeLight = Rgb(158, 150, 152), SmokeDark = Rgb(84, 76, 86);

    /// <summary>How far the lava's surface stands above the rock it lies in, in texels.</summary>
    public const int LavaLift = 2;

    /// <summary>
    /// A pool of lava as large as its tiles: one sheet a hair above the rock, which the builder has levelled under
    /// it (<see cref="Data.WorldMapBuilder"/>), with a scorched rim. Its crust drifts in four frames.
    /// </summary>
    private static void Lava(KitBuilder kit, int w, int d)
    {
        var top = kit.Frames($"lava.{w}x{d}", w, d, MovingFrames, 2f, PaintLava);
        kit.Box(0, w, 0, d, 0, LavaLift, top: top);
    }

    /// <summary>
    /// Lava seen from above, <paramref name="frame"/> of four: plates of dark crust with a paler edge to the upper
    /// left, the cracks between them glowing from deep red at a plate's edge to yellow in the middle of a wide
    /// crack, and a ragged rim of scorched rock all round. The glow is marked as light, so it burns after dark, and
    /// by day wherever the place's light says so. From frame to frame the heat travels along the cracks and a few
    /// bubbles rise and burst; the plates themselves stay where they are.
    /// </summary>
    public static void PaintLava(PixelCanvas c, int frame = 0)
    {
        const int cell = 18;
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // The rim: scorched rock, its inner edge ragged by a few texels
                int edge = Math.Min(Math.Min(x, w - 1 - x), Math.Min(y, h - 1 - y));
                int ragged = 4 + (int)(GroundBaker.Rand01(x / 3, y / 3, 61) * 5f);
                if (edge < ragged)
                {
                    // (flecks two texels long: nothing in the field's art is a texel by itself)
                    c.SetRaw(x, y, edge == ragged - 1 ? ScorchedDark : (x / 2 + y * 3) % 7 == 0 ? ScorchedDark : Scorched);
                    continue;
                }
                // The plates: each texel belongs to the nearest of the points scattered one to a cell
                float best = float.MaxValue, second = float.MaxValue;
                int plate = 0;
                for (int cy = y / cell - 1; cy <= y / cell + 1; cy++)
                    for (int cx = x / cell - 1; cx <= x / cell + 1; cx++)
                    {
                        float px = (cx + 0.2f + 0.6f * GroundBaker.Rand01(cx, cy, 62)) * cell, py = (cy + 0.2f + 0.6f * GroundBaker.Rand01(cx, cy, 63)) * cell;
                        float dist = MathF.Abs(x - px) + MathF.Abs(y - py) * 1.2f;
                        if (dist < best) { second = best; best = dist; plate = cx * 31 + cy * 17; }
                        else if (dist < second) second = dist;
                    }
                float crack = second - best;
                // How hot this stretch of crack is now: the heat moves on a plate every frame
                int heat = ((plate % 4 + 4) % 4 + frame) % MovingFrames;
                if (crack < 1.6f)
                    c.SetRaw(x, y, (heat == 0 ? LavaHot : heat == 1 ? LavaBright : LavaBright) with { A = ArtSheet.PublicLight });
                else if (crack < 3.4f)
                    c.SetRaw(x, y, (heat == 0 ? LavaBright : LavaMid) with { A = ArtSheet.PublicLight });
                else if (crack < 5f)
                    c.SetRaw(x, y, LavaDeep);
                else
                    c.SetRaw(x, y, crack < 6.5f && best > second * 0.5f ? CrustLight : (x / 2 * 5 + y * 3) % 11 == 0 ? CrustLight : Crust);
            }
        // Bubbles: a ring of light that opens over two frames, each in its own place and its own time
        for (int i = 0; i < w * h / 2600; i++)
        {
            int bx = 10 + (int)(GroundBaker.Rand01(i, 7, 64) * (w - 20)), by = 10 + (int)(GroundBaker.Rand01(i, 9, 65) * (h - 20));
            int phase = (i + frame) % MovingFrames;
            if (phase >= 2) continue;
            var glow = LavaHot with { A = ArtSheet.PublicLight };
            if (phase == 0) { c.SetRaw(bx, by, glow); c.SetRaw(bx + 1, by, glow); }
            else
            {
                c.SetRaw(bx - 1, by, glow); c.SetRaw(bx + 1, by, glow); c.SetRaw(bx, by - 1, glow); c.SetRaw(bx, by + 1, glow);
            }
        }
    }

    /// <summary>
    /// The mouth of Stark Mountain: three tiers of basalt stepping in and up from the plateau the world blocks under
    /// its model's box, the uppermost holding the crater, whose throat glows, and a column of smoke rising from it
    /// in four frames. It stands east of the way into the mountain and north of the row that way lies on, so the
    /// door at the cliff's foot stays in plain sight.
    /// </summary>
    private static void Crater(KitBuilder kit, Prop prop, int w, int d)
    {
        int x0 = Math.Min(96, w / 5), x1 = w - 16, z0 = 24, z1 = Math.Max(z0 + 96, d - 44);
        int bw = x1 - x0, bd = z1 - z0;
        int rows = Rows(kit, MathF.Min(prop.Height, 9f), 120, 210);
        int first = rows * 2 / 5, second = rows * 3 / 4;
        CraterTier(kit, x0, x1, z0, z1, 0, first, crater: false);
        CraterTier(kit, x0 + bw / 7, x1 - bw / 6, z0 + bd / 10, z1 - bd / 5, first, second, crater: false);
        int tx0 = x0 + bw * 2 / 7, tx1 = x1 - bw / 3, tz0 = z0 + bd / 6, tz1 = z1 - bd * 2 / 5;
        CraterTier(kit, tx0, tx1, tz0, tz1, second, rows, crater: true);
        // The smoke stands over the throat, a little north of the rim's middle so it rises behind the near rim
        float cx = (tx0 + tx1) / 2f, cz = (tz0 + tz1) / 2f - 4;
        kit.Sprite(kit.Frames("crater.smoke", 110, 150, MovingFrames, 2f, PaintSmoke), cx, cz, rows - 6);
    }

    private static void CraterTier(KitBuilder kit, int x0, int x1, int z0, int z1, int y0, int y1, bool crater)
    {
        int bw = x1 - x0, bd = z1 - z0, h = y1 - y0;
        var front = kit.Face($"crater.face.{bw}x{h}", bw, h, PaintBasaltFace);
        var side = kit.Face($"crater.face.{bd}x{h}", bd, h, PaintBasaltFace);
        var top = kit.Face($"crater.{(crater ? "rim" : "top")}.{bw}x{bd}", bw, bd, c => PaintBasaltTop(c, crater));
        kit.Box(x0, x1, z0, z1, y0, y1, top, front, side, side, front);
    }

    /// <summary>A face of the volcano's rock: the relief's rock face, darkened to basalt, with a few seams that glow faintly.</summary>
    public static void PaintBasaltFace(PixelCanvas c)
    {
        var rock = NatureArt.RockFace();
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                var col = rock.Get(x % rock.Width, y % rock.Height);
                c.SetRaw(x, y, PixelCanvas.Mix(col, Basalt.Dark, 0.5f));
            }
        // Seams of heat: short, steep, a few to a face, the same every time
        for (int i = 0; i < c.Width / 60; i++)
        {
            int sx = 12 + (int)(GroundBaker.Rand01(i, c.Width, 66) * (c.Width - 24)), sy = 6 + (int)(GroundBaker.Rand01(i, c.Height, 67) * Math.Max(1, c.Height - 26));
            for (int k = 0; k < 14 && sy + k < c.Height - 2; k++)
                c.SetRaw(sx + (k / 4 % 2), sy + k, (k % 5 == 0 ? LavaBright : LavaMid) with { A = ArtSheet.PublicLight });
        }
    }

    /// <summary>
    /// The top of a tier of the volcano: mottled basalt with a paler edge; or the crater itself, a ring of rock
    /// round a throat that glows from red at its lip to yellow in its middle.
    /// </summary>
    public static void PaintBasaltTop(PixelCanvas c, bool crater)
    {
        int w = c.Width, h = c.Height;
        Pix.Raised(c, 0, 0, w, h, Basalt);
        for (int y = 2; y < h - 2; y++)
            for (int x = 2; x < w - 2; x++)
            {
                float r = GroundBaker.Rand01(x / 2, y / 2, 68);
                if (r < 0.1f) c.SetRaw(x, y, Basalt.Dark);
                else if (r > 0.94f) c.SetRaw(x, y, Basalt.Light);
            }
        if (!crater) return;
        float cx = w / 2f, cy = h / 2f, rx = w * 0.36f, ry = h * 0.36f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                float r = MathF.Sqrt(u * u + v * v);
                if (r > 1.16f) continue;
                if (r > 1f) c.SetRaw(x, y, Basalt.Dark);                       // the lip's shade
                else if (r > 0.86f) c.SetRaw(x, y, Crust);
                else if (r > 0.66f) c.SetRaw(x, y, LavaDeep with { A = ArtSheet.PublicLight });
                else if (r > 0.42f) c.SetRaw(x, y, LavaMid with { A = ArtSheet.PublicLight });
                else if (r > 0.2f) c.SetRaw(x, y, LavaBright with { A = ArtSheet.PublicLight });
                else c.SetRaw(x, y, LavaHot with { A = ArtSheet.PublicLight });
            }
    }

    /// <summary>
    /// The smoke over the crater, <paramref name="frame"/> of four: a column of grey puffs, small and lit from below
    /// at the throat, larger and paler as they rise and drift east, each a quarter of the way to the next one's
    /// place in every frame, so the column climbs without end.
    /// </summary>
    public static void PaintSmoke(PixelCanvas c, int frame = 0)
    {
        int w = c.Width, h = c.Height;
        const int puffs = 5;
        for (int i = puffs; i >= 0; i--)
        {
            float t = (i + frame / (float)MovingFrames) / puffs;          // 0 at the throat, 1 at the top
            if (t > 1.02f) continue;
            float cy = h - 12 - t * (h - 30f);
            float cx = w * 0.4f + t * w * 0.2f + MathF.Sin(i * 2.1f) * 4f;
            float rx = 9f + t * 26f, ry = 7f + t * 15f;
            var light = t < 0.2f ? Rgb(196, 150, 120) : SmokeLight;
            Lump(c, cx, cy, rx, ry, light, Smoke, SmokeDark);
            Lump(c, cx - rx * 0.5f, cy + ry * 0.3f, rx * 0.5f, ry * 0.6f, light, Smoke, SmokeDark);
            Lump(c, cx + rx * 0.55f, cy + ry * 0.2f, rx * 0.45f, ry * 0.55f, light, Smoke, SmokeDark);
        }
        Pix.Outline(c, 0.35f);
    }

    // ------------------------------------------------------------------ the Battle Frontier

    private static readonly Tone AwningBlue = Tone.Of(58, 108, 214, 110, 156, 240, 40, 76, 160);
    private static readonly Tone AwningWhite = Tone.Of(238, 240, 244, 252, 252, 255, 190, 196, 212);
    private static readonly Tone Gold = Tone.Of(226, 184, 78, 248, 220, 130, 170, 128, 52);
    private static readonly Tone BronzeWarm = Tone.Of(170, 126, 78, 208, 168, 110, 118, 84, 60);

    /// <summary>How high a booth's counter and its back wall stand, in texels.</summary>
    public const int BoothCounter = 20, BoothBack = 50;

    /// <summary>
    /// A booth of the Exchange Service Corner, three tiles each way: a back wall of shelves under a striped awning,
    /// and a counter along its other three sides, waist high, so whoever serves in it is seen standing inside. The
    /// side customers are served across (the tile the world marks as a counter) carries the price board.
    /// </summary>
    private static void Booth(KitBuilder kit, Map map, Prop prop, int w, int d)
    {
        bool servesEast = map.BehaviourAt(prop.X + prop.Width - 1, prop.Y + prop.Depth / 2) == TileBehavior.Counter;
        int x0 = 4, x1 = w - 4, zBack0 = 6, zBack1 = 22, z1 = d - 4;
        var back = kit.Face($"booth.back.{x1 - x0}", x1 - x0, BoothBack, PaintBoothBack);
        var backSide = kit.Face("booth.backside", zBack1 - zBack0, BoothBack, c => Pix.Raised(c, 0, 0, c.Width, c.Height, PaleStone));
        kit.Box(x0, x1, zBack0, zBack1, 0, BoothBack, south: back, west: backSide, east: backSide);
        // The awning: a sheet over the back wall, reaching a little way out over whoever stands behind the counter
        var awning = kit.Face($"booth.awning.{w}x38", w, 38, c => PaintAwning(c, top: true));
        var valance = kit.Face($"booth.valance.{w}", w, 7, c => PaintAwning(c, top: false));
        var valanceSide = kit.Face("booth.valance.38", 38, 7, c => PaintAwning(c, top: false));
        kit.Box(0, w, 2, 40, BoothBack, BoothBack + 7, awning, valance, valanceSide, valanceSide);

        // The counters: the serving side, the far side and the front
        int thick = 16;
        Art Top(int cw, int cd) => kit.Face($"booth.counter.top.{cw}x{cd}", cw, cd, c => PaintCounter(c, top: true, board: false));
        Art Front(int cw, bool board) => kit.Face($"booth.counter.{cw}.{(board ? "board" : "plain")}", cw, BoothCounter, c => PaintCounter(c, top: false, board));
        int sideDepth = z1 - zBack1;
        kit.Box(x0, x0 + thick, zBack1, z1, 0, BoothCounter, Top(thick, sideDepth), Front(thick, false), Front(sideDepth, !servesEast), Front(sideDepth, false));
        kit.Box(x1 - thick, x1, zBack1, z1, 0, BoothCounter, Top(thick, sideDepth), Front(thick, false), Front(sideDepth, false), Front(sideDepth, servesEast));
        kit.Box(x0 + thick, x1 - thick, z1 - thick, z1, 0, BoothCounter, Top(x1 - x0 - 2 * thick, thick), Front(x1 - x0 - 2 * thick, false));
    }

    /// <summary>The back of a booth: pale boards with three shelves of prizes, boxes and bottles in a few bright colours.</summary>
    public static void PaintBoothBack(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        Pix.Raised(c, 0, 0, w, h, PaleStone);
        Color[] prizes = { Rgb(214, 82, 66), Rgb(58, 108, 214), Rgb(226, 184, 78), Rgb(92, 170, 96), Rgb(150, 98, 170) };
        for (int shelf = 0; shelf < 3; shelf++)
        {
            int y = 8 + shelf * 13;
            c.Rect(4, y + 9, w - 8, 2, DarkWood.Base);
            c.HLine(4, y + 9, w - 8, DarkWood.Light);
            for (int x = 7, i = shelf; x + 7 < w - 6; x += 9, i++)
            {
                var col = prizes[(i * 3 + shelf) % prizes.Length];
                int tall = 5 + (i * 5 + shelf * 3) % 4;
                c.Rect(x, y + 9 - tall, 6, tall, col);
                c.VLine(x, y + 9 - tall, tall, PixelCanvas.Mix(col, Color.White, 0.35f));
                c.VLine(x + 5, y + 9 - tall, tall, PixelCanvas.Shadow(col, 0.3f));
            }
        }
    }

    /// <summary>A booth's awning in broad stripes of blue and white: its top, or the scalloped strip that hangs from its edge.</summary>
    public static void PaintAwning(PixelCanvas c, bool top)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var tone = x / 12 % 2 == 0 ? AwningBlue : AwningWhite;
                if (top) c.SetRaw(x, y, y < 2 ? tone.Light : y >= h - 3 ? tone.Dark : tone.Base);
                else if (y >= h - 3 && (x % 12 is 0 or 11 || y == h - 1 && x % 12 is 1 or 10)) continue;   // the scallops
                else c.SetRaw(x, y, y == 0 ? tone.Dark : tone.Base);
            }
    }

    /// <summary>A booth's counter: a top of polished wood, or a front of pale panels, with the price board on the side that serves.</summary>
    public static void PaintCounter(PixelCanvas c, bool top, bool board)
    {
        int w = c.Width, h = c.Height;
        if (top)
        {
            Pix.Raised(c, 0, 0, w, h, Wood);
            for (int y = 3; y < h - 2; y += 5) c.HLine(2, y, w - 4, Wood.Dark);
            return;
        }
        Pix.Raised(c, 0, 0, w, h, PaleStone);
        c.Rect(0, 0, w, 3, Wood.Base);
        c.HLine(0, 0, w, Wood.Light);
        c.HLine(0, 3, w, PaleStone.Dark);
        for (int x = 15; x < w - 4; x += 16) c.VLine(x, 5, h - 7, PaleStone.Dark);
        if (!board || w < 30) return;
        // The board: a dark panel with rows of pale marks, a gold star at its head
        int bx = w / 2 - 12;
        c.Rect(bx, 6, 24, h - 9, Rgb(52, 60, 84));
        Pix.Border(c, bx, 6, 24, h - 9, Gold.Dark);
        for (int y = 9; y < h - 5; y += 3) c.HLine(bx + 3, y, 14 + y % 5, Rgb(214, 220, 232));
        c.Rect(bx + 19, 8, 2, 2, Gold.Light);
    }

    /// <summary>
    /// The model of the Battle Frontier in the middle of its plaza: a table of pale stone as large as its tiles, the
    /// grounds laid out on it in miniature, and the little tower standing up from its north end.
    /// </summary>
    private static void ScaleModel(KitBuilder kit, int w, int d)
    {
        int x0 = 6, x1 = w - 6, z0 = 8, z1 = d - 6, table = 16;
        var top = kit.Face($"scalemodel.top.{x1 - x0}x{z1 - z0}", x1 - x0, z1 - z0, PaintMiniature);
        var front = kit.Face($"scalemodel.front.{x1 - x0}", x1 - x0, table, c => Pix.Raised(c, 0, 0, c.Width, c.Height, PaleStone));
        var side = kit.Face($"scalemodel.side.{z1 - z0}", z1 - z0, table, c => Pix.Raised(c, 0, 0, c.Width, c.Height, PaleStone));
        kit.Box(x0, x1, z0, z1, 0, table, top, front, side, side);
        float cx = w / 2f;
        kit.Block("scalemodel.tower", PaleStone, cx - 7, cx + 7, z0 + 10, z0 + 22, table, table + 22);
        kit.Block("scalemodel.cap", AwningBlue, cx - 5, cx + 5, z0 + 12, z0 + 20, table + 22, table + 28);
    }

    /// <summary>
    /// The Battle Frontier from above, small: lawns, the cross of its paved ways, its pools, and the four halls as
    /// blocks of their own colours round the plaza (the tower stands on it as a block of its own).
    /// </summary>
    public static void PaintMiniature(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        var lawn = Rgb(110, 184, 96); var lawnDark = Rgb(78, 150, 84);
        var path = Rgb(206, 200, 190); var pathDark = Rgb(160, 154, 156);
        Pix.Raised(c, 0, 0, w, h, PaleStone);
        c.Rect(4, 4, w - 8, h - 8, lawn);
        for (int i = 0; i < w * h / 90; i++)
            c.SetRaw(5 + (i * 37 + 3) % (w - 10), 5 + (i * 23 + 11) % (h - 10), lawnDark);
        // The ways: north to south and east to west, meeting in the plaza
        c.Rect(w / 2 - 4, 4, 8, h - 8, path);
        c.Rect(4, h / 2 - 4, w - 8, 8, path);
        c.Rect(w / 2 - 12, h / 2 - 12, 24, 24, path);
        Pix.Border(c, w / 2 - 12, h / 2 - 12, 24, 24, pathDark);
        // Pools either side of the way in
        foreach (int px in new[] { w / 2 - 30, w / 2 + 14 })
        {
            c.Rect(px, h - 30, 16, 12, Water);
            c.HLine(px, h - 30, 16, WaterDark);
            c.HLine(px + 3, h - 24, 6, WaterLight);
        }
        // The four halls, each a little block with a lit edge and a shadow: castle, hall, factory, arcade
        void Hall(int x, int y, Color col)
        {
            c.Rect(x, y, 14, 12, col);
            c.HLine(x, y, 14, PixelCanvas.Mix(col, Color.White, 0.4f));
            c.VLine(x, y, 12, PixelCanvas.Mix(col, Color.White, 0.25f));
            c.Rect(x + 1, y + 12, 14, 2, lawnDark);
        }
        Hall(10, h / 2 - 24, Rgb(214, 170, 72));
        Hall(w - 24, h / 2 - 24, Rgb(214, 120, 62));
        Hall(14, h - 32, Rgb(138, 110, 170));
        Hall(w - 28, h - 32, Rgb(214, 92, 150));
        // Where the tower stands
        c.Rect(w / 2 - 9, 8, 18, 16, pathDark);
    }

    /// <summary>
    /// A bronze of a trainer on a plinth, two tiles each way: one arm thrown up, a Poké Ball in the hand, the other
    /// on the hip. The plinth carries a plate.
    /// </summary>
    private static void TrainerStatue(KitBuilder kit, int w, int d)
    {
        float cx = w / 2f, cz = d / 2f + 6;
        float half = MathF.Min(24, w / 2f - 4);
        kit.Block("trainerstatue.plinth", Stone, cx - half, cx + half, cz - 18, cz + 14, 0, 10);
        kit.Block("trainerstatue.plinth2", PaleStone, cx - half + 6, cx + half - 6, cz - 12, cz + 8, 10, 24);
        kit.Sprite(kit.Face("trainerstatue.plate", 16, 7, c => { Pix.Raised(c, 0, 0, c.Width, c.Height, Gold); c.HLine(3, 3, 10, Gold.Dark); }), cx, cz + 8.5f, 13);
        kit.Sprite(kit.Face("trainerstatue.figure", 34, 54, PaintTrainerStatue), cx, cz - 2, 24);
    }

    /// <summary>The figure of the Battle Frontier's statues, our own: a trainer in a cap, legs apart, holding a ball overhead.</summary>
    public static void PaintTrainerStatue(PixelCanvas c)
    {
        var b = BronzeWarm;
        void Part(float cx, float cy, float rx, float ry) => Lump(c, cx, cy, rx, ry, b.Light, b.Base, b.Dark);
        Part(12, 47, 4, 7);          // legs
        Part(21, 47, 4, 7);
        Part(16.5f, 34, 8, 9);       // the coat
        Part(16.5f, 21, 5.5f, 5.5f); // head
        c.Rect(11, 16, 12, 3, b.Dark);          // the cap's brim
        c.Rect(12, 13, 10, 3, b.Base);
        c.HLine(12, 13, 10, b.Light);
        Part(24, 22, 2.5f, 8);       // the arm thrown up
        Part(25, 9, 5, 5);           // the ball
        c.HLine(21, 9, 9, b.Dark);
        c.Rect(24, 8, 2, 2, b.Light);
        Part(8, 33, 2.5f, 6);        // the other arm, on the hip
        Pix.Outline(c);
    }

    // ------------------------------------------------------------------ Snowpoint Temple

    private static readonly Tone ColdStone = Tone.Of(150, 160, 178, 186, 196, 212, 104, 112, 138);

    /// <summary>
    /// The giant in Snowpoint Temple's hall: a plinth across its tiles and on it a figure of carved stone, taller
    /// than a door, rimed with ice along its shoulders.
    /// </summary>
    private static void Colossus(KitBuilder kit, int w, int d)
    {
        float cx = w / 2f, cz = d / 2f + 6;
        float half = MathF.Min(42, w / 2f - 4);
        kit.Block("colossus.plinth", ColdStone, cx - half, cx + half, cz - 20, cz + 16, 0, 12);
        kit.Sprite(kit.Face("colossus.figure", 72, 92, PaintColossus), cx, cz - 2, 12);
    }

    /// <summary>
    /// A giant of stone, our own: a barrel of a body on short legs, shoulders wider than the plinth, arms that hang
    /// to the knee, no neck, and across its face a row of seven hollows. Bands are cut round its wrists and ankles,
    /// and ice lies along its shoulders and its head.
    /// </summary>
    public static void PaintColossus(PixelCanvas c)
    {
        var s = ColdStone;
        void Part(float cx, float cy, float rx, float ry) => Lump(c, cx, cy, rx, ry, s.Light, s.Base, s.Dark);
        Part(26, 80, 9, 11);          // legs
        Part(46, 80, 9, 11);
        Part(36, 52, 22, 26);         // the body
        Part(10, 44, 9, 12);          // shoulders
        Part(62, 44, 9, 12);
        Part(8, 66, 7, 16);           // arms to the knee
        Part(64, 66, 7, 16);
        Part(36, 24, 13, 10);         // the head, sunk between the shoulders
        // The hollows of its face, and the bands
        for (int i = 0; i < 7; i++)
        {
            int ex = 24 + i * 4, ey = 40 + (i is 0 or 6 ? 2 : i is 1 or 5 ? 1 : 0);
            c.Rect(ex, ey, 2, 2, s.Deep);
        }
        foreach (int bx in new[] { 3, 59 }) c.Rect(bx, 72, 11, 2, s.Dark);
        foreach (int bx in new[] { 19, 39 }) c.Rect(bx, 84, 15, 2, s.Dark);
        c.Rect(28, 56, 16, 2, s.Dark);
        c.Rect(34, 58, 4, 8, s.Dark);
        // Rime: the paler blue of the temple's ice along what faces up
        var rime = Rgb(206, 228, 244);
        for (int x = 0; x < c.Width; x++)
            for (int y = 0; y < 46; y++)
                if (c.Get(x, y).A > 0 && c.Get(x, y - 1).A == 0)
                {
                    c.SetRaw(x, y, rime);
                    if (x % 3 != 0) c.SetRaw(x, y + 1, rime);
                    break;
                }
        Pix.Outline(c);
    }
}
