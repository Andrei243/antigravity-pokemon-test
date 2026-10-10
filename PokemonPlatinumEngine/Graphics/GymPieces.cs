using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What moves in a Gym's puzzle (plan 01 · M9; style guide, "Gyms"): pieces built once with the field art kit, each
/// at its own origin, and drawn every frame where the puzzle has them: the Eterna Gym's two clock hands turning on
/// their pin, the water in its fountains falling. The room round them is a scene like any other room's.
/// </summary>
internal sealed class GymPieces
{
    private sealed record Piece(Mesh Mesh, Material Main, Material Depth);

    private readonly FieldShaders shaders;
    private Map? builtFor;
    private Texture2D art;
    private readonly List<Piece> owned = new();

    // The Eterna Gym's pieces
    private Piece? minuteHand, hourHand, waterPool, stillParts;

    // The Pastoria Gym's: the water over the pool, a raft of its floating floor, a button of each colour up and down
    private Piece? poolWater, raft;
    private readonly List<(int X, int Y)> rafts = new();
    private readonly Dictionary<(PastoriaWater.Button Colour, bool Down), Piece> buttons = new();

    // The Hearthome Gym's: the doors and pads (still), and a sign for each door, shown where its clue lies
    private readonly Dictionary<HearthomeDoors.Sign, Piece> clues = new();

    public GymPieces(FieldShaders shaders) => this.shaders = shaders;

    /// <summary>The hands' and the water's lift above the floor, in texels of art.</summary>
    private const int HandTop = 2, TipTop = 9;

    /// <summary>Draws the pieces of the map's puzzle into the shadow map.</summary>
    public void DrawDepth(Map map)
    {
        if (!Ready(map)) return;
        foreach (var (piece, transform) in Placed(map)) Raylib.DrawMesh(piece.Mesh, piece.Depth, Matrix4x4.Transpose(transform));
    }

    /// <summary>Draws the pieces of the map's puzzle, lit as the room is.</summary>
    public void Draw(Map map)
    {
        if (!Ready(map)) return;
        foreach (var (piece, transform) in Placed(map))
        {
            Raylib.DrawMesh(piece.Mesh, piece.Main, Matrix4x4.Transpose(transform));
            FrameProfiler.Count(piece.Mesh.TriangleCount);
        }
    }

    private IEnumerable<(Piece Piece, Matrix4x4 Transform)> Placed(Map map)
    {
        if (map.Name.StartsWith(HearthomeDoors.Entrance, StringComparison.Ordinal))
        {
            if (stillParts != null) yield return (stillParts, Matrix4x4.Identity);
            // The sign of the door that leads on, standing where it was put this time
            if (map.Puzzle is HearthomeDoors { Correct: { } sign } doors && clues.TryGetValue(sign, out var clue))
                yield return (clue, Matrix4x4.CreateTranslation(doors.Clue.X, 0f, doors.Clue.Y));
            yield break;
        }
        if (map.Puzzle is PastoriaWater pool)
        {
            // The water a hair under its level, so a walkway at the same height shows over it; the rafts on it
            float level = pool.Level - map.GroundLevel;
            if (poolWater != null) yield return (poolWater, Matrix4x4.CreateTranslation(0f, level - 0.06f, 0f));
            if (raft != null)
                foreach (var (x, y) in rafts) yield return (raft, Matrix4x4.CreateTranslation(x, level, y));
            foreach (var (x, y, colour) in PastoriaWater.Buttons)
                if (buttons.TryGetValue((colour, colour == pool.Pressed), out var button))
                    yield return (button, Matrix4x4.CreateTranslation(x, Relief.At(map, x + 0.5f, y + 0.5f), y));
            yield break;
        }
        if (map.Puzzle is EternaClock clock)
        {
            var pin = new Vector3(EternaClock.CenterX + 0.5f, 0f, EternaClock.CenterZ + 0.5f);
            if (stillParts != null) yield return (stillParts, Matrix4x4.Identity);
            // A hand is built pointing north from its pin and turned clockwise as seen from above
            Matrix4x4 Turned(float degrees) => Matrix4x4.CreateRotationY(-degrees * MathF.PI / 180f) * Matrix4x4.CreateTranslation(pin);
            if (hourHand != null) yield return (hourHand, Turned(clock.HourAngle));
            if (minuteHand != null) yield return (minuteHand, Turned(clock.MinuteAngle) * Matrix4x4.CreateTranslation(0f, 0.01f, 0f));
            // The water stands at the kerb's height when full and sinks out of sight under the floor as it drains
            if (waterPool != null)
            {
                float full = 6f / GroundBaker.ArtTile * MapScene.VerticalScaleOf(map);
                foreach (var (x, level) in new[] { (1, clock.LeftWater), (EternaClock.RightFountainX - 2, clock.RightWater) })
                    if (level > 0f) yield return (waterPool, Matrix4x4.CreateTranslation(x, -0.08f + (full + 0.08f) * level, EternaClock.FountainZ));
            }
        }
    }

    private bool Ready(Map map)
    {
        bool hearthome = map.Name.StartsWith(HearthomeDoors.Entrance, StringComparison.Ordinal) && map.IsIndoors;
        if (map.Puzzle == null && !hearthome) return false;
        if (builtFor == map) return true;
        Unload();
        builtFor = map;
        if (map.Puzzle is EternaClock) BuildEterna(map);
        else if (map.Puzzle is PastoriaWater) BuildPastoria(map);
        else if (hearthome) BuildHearthome(map);
        return true;
    }

    // ------------------------------------------------------------------ the Hearthome Gym

    private static readonly Tone DoorWood = Tone.Of(96, 62, 110, 134, 96, 150, 62, 38, 78);
    private static readonly Tone Gilt = Tone.Of(232, 196, 96, 252, 232, 150, 170, 130, 60);

    /// <summary>
    /// The Hearthome Gym's rooms (style guide, "Gyms"): a door of dark wood in the back wall at each way on, its sign
    /// in gilt over it; the pads that lead between the entrance and Fantina's room, a ring of light on the floor; and
    /// a sign of each kind standing a little above the floor, the one of the door that leads on shown where its clue lies.
    /// </summary>
    private void BuildHearthome(Map map)
    {
        const int T = GroundBaker.ArtTile;
        var sheet = new ArtSheet();
        float vs = MapScene.VerticalScaleOf(map);
        var still = new KitBuilder(sheet, vs);
        var back = map.RoomCorner().Back;
        var doors = map.Puzzle is HearthomeDoors puzzle ? puzzle.Doors.Select(d => ((HearthomeDoors.Sign?)d.Sign, d.X, d.Y)).ToList()
            : map.Warps.Where(w => w.SourceY == back - 1).Select(w => ((HearthomeDoors.Sign?)null, w.SourceX, w.SourceY)).ToList();
        foreach (var (sign, x, _) in doors)
        {
            still.Origin = new Vector3(x, 0f, back);
            var door = still.Face($"door.{sign}", 28, 50, c => PaintDoor(c, sign));
            still.Card(2, 30, 0.6f, 0, 50, door);
        }
        // A pad that is a warp: a ring of pale light on the floor
        foreach (var warp in map.Warps.Where(w => w.SourceY >= back && w.SourceY < map.Height - 1))
        {
            still.Origin = new Vector3(warp.SourceX, 0f, warp.SourceY);
            var pad = still.Face("pad", 28, 28, PaintPad);
            still.Decal(2, 30, 2, 30, 0.012f, pad);
        }
        var clueKits = new Dictionary<HearthomeDoors.Sign, KitBuilder>();
        if (map.Puzzle is HearthomeDoors)
            foreach (var sign in Enum.GetValues<HearthomeDoors.Sign>())
            {
                var kit = new KitBuilder(sheet, vs);
                var art = kit.Face($"clue.{sign}", 22, 22, c => PaintSign(c, sign, 22, glow: true));
                kit.Card(5, 27, 16, 10, 32, art);
                clueKits[sign] = kit;
            }
        art = Upload(sheet);
        stillParts = Make(still);
        foreach (var (sign, kit) in clueKits)
            if (Make(kit) is { } piece) clues[sign] = piece;
    }

    private static void PaintDoor(PixelCanvas c, HearthomeDoors.Sign? sign)
    {
        // An arched door of dark wood in a gilt frame, its sign over the middle
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - w / 2f;
                bool arch = y < 12 && dx * dx + (y - 12) * (y - 12) > (w / 2f - 1) * (w / 2f - 1);
                if (arch) continue;
                bool frame = x < 2 || x >= w - 2 || (y < 12 && dx * dx + (y - 12) * (y - 12) > (w / 2f - 3) * (w / 2f - 3));
                var col = frame ? Gilt.Base : x == w / 2 ? DoorWood.Dark : (x % 6 == 2 ? DoorWood.Light : DoorWood.Base);
                c.SetRaw(x, y, col);
            }
        c.Rect(w / 2 + 3, 30, 2, 3, Gilt.Light);
        if (sign is { } s) PaintSignAt(c, s, w / 2 - 7, 12, 14);
    }

    private static void PaintPad(PixelCanvas c)
    {
        var ring = new Color(196, 170, 236, 255);
        float r = c.Width / 2f;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float d = MathF.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                if (d > r - 0.5f) continue;
                c.SetRaw(x, y, d > r - 3f ? ring : d > r - 6f ? new Color(110, 84, 150, 255) : new Color(150, 124, 196, 255));
            }
    }

    private static void PaintSign(PixelCanvas c, HearthomeDoors.Sign sign, int size, bool glow)
    {
        if (glow)
        {
            // A soft dark disc behind it, so the gilt reads against any floor
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = MathF.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                    if (d < r - 0.5f) c.SetRaw(x, y, new Color(52, 36, 74, 255));
                }
        }
        PaintSignAt(c, sign, (size - 16) / 2, (size - 16) / 2, 16);
    }

    /// <summary>One of the eight signs, in gilt, filling a square of <paramref name="n"/> texels.</summary>
    internal static void PaintSignAt(PixelCanvas c, HearthomeDoors.Sign sign, int ox, int oy, int n)
    {
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f - r) / r, v = (y + 0.5f - r) / r, d = MathF.Sqrt(u * u + v * v);
                float a = MathF.Atan2(v, u);
                bool on = sign switch
                {
                    HearthomeDoors.Sign.Circle => d < 0.9f,
                    HearthomeDoors.Sign.Square => MathF.Abs(u) < 0.78f && MathF.Abs(v) < 0.78f,
                    HearthomeDoors.Sign.Triangle => v > -0.85f && v < 0.8f && MathF.Abs(u) < (v + 0.85f) * 0.58f,
                    HearthomeDoors.Sign.Sun => d < 0.48f || (d < 0.95f && MathF.Cos(a * 8f) > 0.55f),
                    HearthomeDoors.Sign.Donut => d < 0.92f && d > 0.45f,
                    HearthomeDoors.Sign.Moon => d < 0.9f && MathF.Sqrt((u - 0.38f) * (u - 0.38f) + (v + 0.2f) * (v + 0.2f)) > 0.66f,
                    HearthomeDoors.Sign.Star => d < 0.95f * (0.55f + 0.45f * MathF.Pow(MathF.Max(0f, MathF.Cos((a + MathF.PI / 2f) * 5f / 2f)), 6f)) || d < 0.42f,
                    _ => Heart(u, v)
                };
                if (!on) continue;
                c.SetRaw(ox + x, oy + y, u + v < -0.5f ? Gilt.Light : u + v > 0.6f ? Gilt.Dark : Gilt.Base);
            }

        static bool Heart(float u, float v)
        {
            float x = u * 1.15f, y = -v * 1.15f + 0.2f;
            float q = x * x + y * y - 0.5f;
            return q * q * q - x * x * y * y * y < 0f;
        }
    }

    // ------------------------------------------------------------------ the Eterna Gym

    private static readonly Tone Wood = Tone.Of(214, 176, 120, 234, 204, 150, 160, 120, 82);
    private static readonly Color LeafEdge = new(70, 150, 82, 255), LeafEdgeLight = new(120, 196, 104, 255);
    private static readonly Tone Stone = Tone.Of(196, 192, 186, 226, 222, 216, 138, 134, 140);
    private static readonly Color Pool = new(76, 150, 222, 255), PoolLight = new(150, 206, 246, 255), PoolDark = new(50, 112, 190, 255);

    private void BuildEterna(Map map)
    {
        const int T = GroundBaker.ArtTile;
        var sheet = new ArtSheet();
        float vs = MapScene.VerticalScaleOf(map);
        var minute = new KitBuilder(sheet, vs);
        var hour = new KitBuilder(sheet, vs);
        var water = new KitBuilder(sheet, vs);
        var still = new KitBuilder(sheet, vs);

        // The minute hand reaches the ring round the face, six tiles from the pin; the hour hand stops a tile short,
        // where its raised tip is hopped over (style guide, "Gyms": a hand is a walk of boards edged with leaves)
        Hand(minute, "minute", length: 6 * T - 4, width: 22, tip: false);
        Hand(hour, "hour", length: 5 * T - 2, width: 26, tip: true);

        // The face: a round carpet of flowers in rings of colour, filling the face's tiles out to the ring path
        still.Origin = new Vector3(EternaClock.CenterX, 0f, EternaClock.CenterZ);
        const int faceSize = 11 * T;
        var carpet = still.Face("clock.face", faceSize, faceSize, PaintFace);
        still.Decal(16 - faceSize / 2, 16 + faceSize / 2, 16 - faceSize / 2, 16 + faceSize / 2, 0.006f, carpet);

        // The pin at the clock's middle: a round boss of stone, and the twelve stones round the rim that mark the hours
        var boss = still.Face("clock.boss", 30, 30, c => Disc(c, 30, Stone));
        still.Decal(1, 31, 1, 31, 0.012f, boss);
        var mark = still.Face("clock.mark", 12, 12, c => Disc(c, 12, Stone));
        for (int h = 0; h < 12; h++)
        {
            float a = h * MathF.PI / 6f, r = 5.62f * T;
            float cx = 16 + MathF.Sin(a) * r, cz = 16 - MathF.Cos(a) * r;
            still.Decal(MathF.Round(cx - 6), MathF.Round(cx + 6), MathF.Round(cz - 6), MathF.Round(cz + 6), 0.01f, mark);
        }

        // The fountains' pools: a low kerb of stone round four tiles of floor, and the water inside it
        foreach (int x0 in new[] { 1, EternaClock.RightFountainX - 2 })
        {
            still.Origin = new Vector3(x0, 0f, EternaClock.FountainZ);
            int w = 4 * T, d = T;
            still.Block("pool.kerb", Stone, 0, w, 0, 4, 0, 6);
            still.Block("pool.kerb", Stone, 0, w, d - 4, d, 0, 6);
            still.Block("pool.kerb.end", Stone, 0, 4, 4, d - 4, 0, 6);
            still.Block("pool.kerb.end", Stone, w - 4, w, 4, d - 4, 0, 6);
        }
        water.Origin = Vector3.Zero;
        var surface = water.Face("pool.water", 4 * T - 8, T - 8, c => PaintPool(c));
        water.Decal(4, 4 * T - 4, 4, T - 4, 0f, surface);

        art = Upload(sheet);
        minuteHand = Make(minute);
        hourHand = Make(hour);
        waterPool = Make(water);
        stillParts = Make(still);
    }

    /// <summary>A clock hand pointing north from its pin: a walk of boards edged with leaves, ending in a point (the hour hand's raised).</summary>
    private static void Hand(KitBuilder kit, string key, int length, int width, bool tip)
    {
        kit.Origin = Vector3.Zero;
        int half = width / 2, point = tip ? 30 : 18;
        int body = length - point - 10;
        // The shaft runs north from the pin's edge, its boards across it
        var top = kit.Face($"hand.{key}", width, body, PaintBoards);
        var side = kit.Face($"hand.{key}.side", body, HandTop, c => c.Fill(Wood.Dark));
        var end = kit.Face($"hand.{key}.end", width, HandTop, c => c.Fill(Wood.Dark));
        kit.Box(-half, half, -length + point, -10, 0, HandTop, top, end, side, side, end);

        // The point: an arrow head of leaves, raised on the hour hand so that it is hopped over
        int raise = tip ? TipTop : HandTop, hw = half + 6;
        float zb = -length + point, za = -length;
        var head = kit.Face($"hand.{key}.head", hw * 2, point, PaintHead);
        var slope = kit.Face($"hand.{key}.head.side", point + 6, raise, c => c.Fill(LeafEdge));
        var foot = kit.Face($"hand.{key}.head.foot", hw * 2, raise, c => c.Fill(LeafEdge));
        kit.Tri(kit.At(-hw, raise, zb), kit.At(hw, raise, zb), kit.At(0, raise, za), head,
            new Vector2(0, point), new Vector2(hw * 2, point), new Vector2(hw, 0), Vector3.UnitY);
        var westNormal = Vector3.Normalize(new Vector3(-point, 0f, -hw));
        var eastNormal = Vector3.Normalize(new Vector3(point, 0f, -hw));
        kit.Quad(kit.At(-hw, 0, zb), kit.At(0, 0, za), kit.At(0, raise, za), kit.At(-hw, raise, zb), slope, westNormal);
        kit.Quad(kit.At(0, 0, za), kit.At(hw, 0, zb), kit.At(hw, raise, zb), kit.At(0, raise, za), slope, eastNormal);
        kit.Quad(kit.At(-hw, 0, zb), kit.At(hw, 0, zb), kit.At(hw, raise, zb), kit.At(-hw, raise, zb), foot, KitBuilder.FrontNormal);
    }

    private static void PaintBoards(PixelCanvas c)
    {
        // Boards across the hand, four texels wide, between edges of leaves
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                var col = y % 4 == 3 ? Wood.Dark : x % 7 == 0 && (y / 4) % 3 == 1 ? Wood.Light : Wood.Base;
                if (x < 3 || x >= c.Width - 3) col = (x + y) % 3 == 0 ? LeafEdgeLight : LeafEdge;
                c.SetRaw(x, y, col);
            }
    }

    private static void PaintHead(PixelCanvas c)
    {
        // An arrow head: full width at its foot, narrowing to a point at the top; outside it is left clear (cut out)
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
        {
            int half = (int)MathF.Round((w / 2f) * (y + 1) / h);
            for (int x = w / 2 - half; x < w / 2 + half; x++)
            {
                bool rim = x <= w / 2 - half + 1 || x >= w / 2 + half - 2 || y >= h - 2;
                c.SetRaw(x, y, rim ? LeafEdge : (x + y) % 5 == 0 ? LeafEdgeLight : new Color(96, 176, 92, 255));
            }
        }
    }

    private static readonly Color[] Blooms =
    {
        new(232, 70, 84, 255), new(250, 250, 246, 255), new(252, 206, 72, 255), new(238, 132, 190, 255), new(150, 120, 230, 255)
    };

    /// <summary>
    /// The clock's face, seen from above: a disc of foliage set thick with flowers, a ring of each colour from the rim
    /// in, scalloped where the rings meet, and a pale band at the rim; clear outside the disc, so the ground shows.
    /// </summary>
    private static void PaintFace(PixelCanvas c)
    {
        float r = c.Width / 2f;
        var leaf = new Color(84, 160, 88, 255);
        var leafDark = new Color(62, 130, 76, 255);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r, d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > r - 1f) continue;
                if (d > r - 4f)
                {
                    c.SetRaw(x, y, leafDark);
                    continue;
                }
                // Rings of colour 24 texels wide from the rim inward, each edge scalloped twenty-four times round
                float a = MathF.Atan2(dy, dx);
                float scallop = 3f * MathF.Abs(MathF.Sin(a * 12f));
                int ring = (int)((r - 4f - d + scallop) / 24f);
                var bloom = Blooms[Math.Min(ring, Blooms.Length - 1)];
                // Foliage between the flowers: each flower a cross of four texels on a staggered grid of five
                int gx = (x + (y / 5) % 2 * 2) % 5, gy = y % 5;
                bool petal = (gx == 2 && gy is 1 or 3) || (gy == 2 && gx is 1 or 3);
                bool heart = gx == 2 && gy == 2;
                var col = heart ? new Color(250, 220, 90, 255) : petal ? bloom : ((x * 7 + y * 3) % 11 == 0 ? leafDark : leaf);
                c.SetRaw(x, y, col);
            }
    }

    private static void Disc(PixelCanvas c, int size, Tone tone)
    {
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r, d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > r - 0.2f) continue;
                c.SetRaw(x, y, d > r - 1.5f ? tone.Dark : dx + dy < -r * 0.4f ? tone.Light : tone.Base);
            }
    }

    private static void PaintPool(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                var col = y < 2 ? PoolDark : ((x / 3 + y * 2) % 11 == 0) ? PoolLight : Pool;
                c.SetRaw(x, y, col);
            }
    }

    // ------------------------------------------------------------------ the Pastoria Gym

    private static readonly Color Water = new(64, 136, 214, 255), WaterLight = new(132, 194, 244, 255), WaterDark = new(44, 104, 184, 255);
    private static readonly Tone Raft = Tone.Of(178, 132, 84, 210, 168, 112, 122, 86, 56);
    private static readonly Tone Rim = Tone.Of(176, 184, 196, 214, 220, 228, 120, 128, 144);

    /// <summary>The colours of a button's face, as the style guide gives them.</summary>
    private static Tone ButtonTone(PastoriaWater.Button colour) => colour switch
    {
        PastoriaWater.Button.Blue => Tone.Of(70, 120, 222, 130, 176, 250, 40, 78, 170),
        PastoriaWater.Button.Green => Tone.Of(74, 176, 92, 140, 222, 140, 44, 124, 64),
        _ => Tone.Of(236, 138, 52, 252, 190, 110, 186, 94, 30)
    };

    /// <summary>
    /// The Pastoria Gym (style guide, "Gyms"): the water over the pool, a tile of it for each tile its floor covers;
    /// a raft of planks for each tile where that floor is what carries the player; and a button of each colour, a
    /// round face in a pale rim, standing up or pressed down.
    /// </summary>
    private void BuildPastoria(Map map)
    {
        const int T = GroundBaker.ArtTile;
        var sheet = new ArtSheet();
        float vs = MapScene.VerticalScaleOf(map);
        var water = new KitBuilder(sheet, vs);
        var surface = water.Face("pool.water", T, T, PaintWater);
        for (int y = PastoriaWater.Top; y < PastoriaWater.Top + PastoriaWater.Depth; y++)
            for (int x = PastoriaWater.Left; x < PastoriaWater.Left + PastoriaWater.Width; x++)
            {
                water.Origin = new Vector3(x, 0f, y);
                water.Decal(0, T, 0, T, 0f, surface);
            }

        // A raft wherever the floating floor is what someone stands on: open floor lower than the floor can be, and
        // of none of the behaviours that keep walkers to a height or off the floor
        rafts.Clear();
        for (int y = PastoriaWater.Top; y < PastoriaWater.Top + PastoriaWater.Depth; y++)
            for (int x = PastoriaWater.Left; x < PastoriaWater.Left + PastoriaWater.Width; x++)
                if (!map.IsSolid(x, y) && map.BehaviourAt(x, y) == TileBehavior.None && map.HeightAt(x, y) < 1.5f && map.DeckAt(x, y) == null)
                    rafts.Add((x, y));
        var planks = new KitBuilder(sheet, vs);
        planks.Block("raft", Raft, 1, T - 1, 1, T - 1, -3, 2);
        var deck = planks.Face("raft.deck", T - 4, T - 4, PaintRaft);
        planks.Decal(2, T - 2, 2, T - 2, 2f * Texel * vs + 0.004f, deck);

        var kits = new Dictionary<(PastoriaWater.Button, bool), KitBuilder>();
        foreach (var colour in Enum.GetValues<PastoriaWater.Button>())
            foreach (bool down in new[] { false, true })
            {
                var kit = new KitBuilder(sheet, vs);
                kit.Block("button.rim", Rim, 4, T - 4, 4, T - 4, 0, 2);
                var tone = ButtonTone(colour);
                var face = kit.Face($"button.{colour}.{down}", 20, 20, c => Disc(c, 20, tone));
                kit.Block($"button.{colour}", tone, 8, T - 8, 8, T - 8, 0, down ? 3 : 6, sides: true);
                kit.Decal(6, T - 6, 6, T - 6, (down ? 3 : 6) * Texel * vs + 0.004f, face);
                kits[(colour, down)] = kit;
            }

        art = Upload(sheet);
        poolWater = Make(water);
        raft = Make(planks);
        foreach (var (key, kit) in kits)
            if (Make(kit) is { } piece) buttons[key] = piece;
    }

    private const float Texel = 1f / GroundBaker.ArtTile;

    private static void PaintWater(PixelCanvas c)
    {
        // Small waves in rows, a light crest over a dark trough, offset from row to row
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                int u = (x + (y / 4) * 5) % 16, v = y % 8;
                var col = v == 0 && u < 6 ? WaterLight : v == 1 && u < 6 ? WaterDark : Water;
                c.SetRaw(x, y, col);
            }
    }

    private static void PaintRaft(PixelCanvas c)
    {
        // Planks running east and west, five texels wide, with a dark seam between them and a nail at each end
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                var col = y % 5 == 4 ? Raft.Dark : (x is 2 or 25) && y % 5 == 2 ? Raft.Dark : y % 5 == 0 ? Raft.Light : Raft.Base;
                c.SetRaw(x, y, col);
            }
    }

    // ------------------------------------------------------------------ the GPU

    private Texture2D Upload(ArtSheet sheet)
    {
        var image = sheet.ToCanvas().ToImage();
        var texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        Raylib.SetTextureFilter(texture, TextureFilter.Point);
        Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
        return texture;
    }

    private Piece? Make(KitBuilder kit)
    {
        kit.Finish();
        var builder = new MeshBuilder();
        builder.Append(kit.Solid, Matrix4x4.Identity);
        builder.Append(kit.Flat, Matrix4x4.Identity);
        if (builder.VertexCount == 0) return null;
        var piece = new Piece(builder.Upload(), RenderContext.MaterialFor(shaders.World, art), RenderContext.MaterialFor(shaders.Depth, art));
        owned.Add(piece);
        return piece;
    }

    /// <summary>Lets go of the pieces built for the last map.</summary>
    public unsafe void Unload()
    {
        foreach (var piece in owned)
        {
            Raylib.UnloadMesh(piece.Mesh);
            if (piece.Main.Maps != null) Raylib.MemFree(piece.Main.Maps);
            if (piece.Depth.Maps != null) Raylib.MemFree(piece.Depth.Maps);
        }
        owned.Clear();
        if (art.Id != 0) Raylib.UnloadTexture(art);
        art = default;
        builtFor = null;
        minuteHand = hourHand = waterPool = stillParts = poolWater = raft = null;
        buttons.Clear();
        rafts.Clear();
        clues.Clear();
    }
}
