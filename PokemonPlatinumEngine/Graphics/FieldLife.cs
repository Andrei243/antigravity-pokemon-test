using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>One piece of the field's small effects to draw: a cell of the life atlas, tinted, lying on the ground or standing up.</summary>
internal readonly record struct LifeQuad(Vector3 At, float Width, float Height, int Cell, Color Tint, int Turn = 0);

/// <summary>A block of weather drawn over the picture, in layout units (1920 by 1080).</summary>
internal readonly record struct WeatherBlock(float X, float Y, float Width, float Height, Color Color);

/// <summary>
/// A layer of mist drawn over the picture: the haze texture with each of its texels <paramref name="Scale"/>
/// layout units wide, moved by an offset in layout units, in a tint whose alpha is how thick it lies.
/// </summary>
internal readonly record struct HazeLayer(float Scale, float OffsetX, float OffsetY, Color Tint);

/// <summary>
/// The small things that move in the field (style guide, "Life"): footprints, dust, leaves thrown up by tall
/// grass, rings on water, rain landing, and the doors that open. It is told what happens (a step, a landing, a
/// door) and says what there is to draw at this moment; everything is a function of its own clock, so tests
/// run it without a window.
/// </summary>
internal sealed class FieldLife
{
    public const int Cell = 16, Columns = 8;

    // Cells of the atlas
    public const int Print = 0, Puff = 1, Leaf = 4, Ring = 6, Drop = 9, Fleck = 10;

    public const float PrintStays = 3f, PrintFades = 3f, PuffTime = 0.36f, LeafTime = 0.5f, RingTime = 0.7f, DropTime = 0.3f, FleckTime = 0.3f;
    public const float DoorOpens = 0.2f, DoorLingers = 0.4f;

    /// <summary>How often rain lands: this many times a second, a few drops each time (<see cref="Weathers.LandsABeat"/>).</summary>
    public const float RainBeats = 30f;

    /// <summary>The stretch of ground rain is seen landing on, in tiles, round the middle of the view.</summary>
    public const float RainWidth = 24f, RainDepth = 16f;

    private enum Kind { Print, Puff, Leaf, Ring, Drop, Fleck }

    private readonly record struct Piece(Kind Kind, Vector3 At, double Born, Color Tint, int Turn, float Seed, float Size);

    private readonly List<Piece> pieces = new();
    private int steps;
    private long rainBeat;

    /// <summary>
    /// Seconds since the game began walking the field: the clock everything here runs by. It is kept to double
    /// precision, so what moves by it is as smooth after a day's play as in the first minute.
    /// </summary>
    public double Now { get; private set; }

    public int Count => pieces.Count;

    public void Advance(float dt)
    {
        Now += dt;
        pieces.RemoveAll(p => Now - p.Born > LifeOf(p.Kind));
        if (door is { } d && d.ShutAt is { } shut && Now >= shut + DoorOpens) door = null;
    }

    /// <summary>Forgets everything: on going to another map.</summary>
    public void Clear()
    {
        pieces.Clear();
        door = null;
    }

    private static float LifeOf(Kind kind) => kind switch
    {
        Kind.Print => PrintStays + PrintFades,
        Kind.Puff => PuffTime,
        Kind.Leaf => LeafTime,
        Kind.Ring => RingTime,
        Kind.Fleck => FleckTime,
        _ => DropTime
    };

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color RingTint = Rgb(210, 236, 255), DropTint = Rgb(190, 222, 250), FleckTint = Rgb(236, 242, 255);
    private static readonly Color LeafLight = Rgb(168, 226, 112), LeafMid = Rgb(110, 196, 92);

    /// <summary>The colour of the dust a kind of ground gives off.</summary>
    public static Color DustOf(TileType ground) => ground switch
    {
        TileType.Path => Rgb(214, 196, 150),
        TileType.Sand => Rgb(232, 218, 170),
        TileType.Dirt => Rgb(170, 134, 98),
        TileType.Snow => Rgb(214, 226, 244),
        _ => Rgb(200, 196, 190)
    };

    private static bool RaisesDust(TileType ground) =>
        ground is TileType.Path or TileType.Sand or TileType.Dirt or TileType.Snow or TileType.CaveFloor or TileType.Rock;

    /// <summary>
    /// Someone has just stepped onto a tile, walking <paramref name="facing"/>: a print in sand or snow, dust
    /// behind a run, leaves out of tall grass, a ring on the water behind a swimmer, a splash in a puddle.
    /// </summary>
    public void Footstep(Map map, int x, int y, Direction facing, bool running, TravelMode mode)
    {
        if (!map.InBounds(x, y) || map.IsIndoors) return;
        steps++;
        var (dx, dz) = FieldMovement.Delta(facing);
        float cx = x + 0.5f, cz = y + 0.5f, ground = Relief.At(map, cx, cz);
        var type = map.GetGroundTile(x, y);
        var behaviour = map.BehaviourAt(x, y);

        if (mode == TravelMode.Surfing)
        {
            // On the water left behind, where the last stroke was
            float bx = cx - dx, bz = cz - dz;
            pieces.Add(new Piece(Kind.Ring, new Vector3(bx, Relief.At(map, bx, bz) + 0.02f, bz), Now, RingTint, 0, 0f, 1f));
            return;
        }

        // Platinum's three kinds of standing water: a puddle splashes, water ankle deep ripples, a still puddle
        // only mirrors (which is not drawn yet)
        if (behaviour == TileBehavior.Puddle)
        {
            Splash(map, x, y, 0.8f, 2);
            return;
        }
        if (behaviour == TileBehavior.ShallowWater)
        {
            pieces.Add(new Piece(Kind.Ring, new Vector3(cx, ground + 0.02f, cz), Now, RingTint, 0, 0f, 0.8f));
            return;
        }
        if (behaviour == TileBehavior.StillPuddle) return;

        if (map.IsTallGrass(x, y))
        {
            // Four bits in two greens, two to each side: the inner pair fly high, the outer pair low and wide
            float[] sides = { -0.55f, -0.2f, 0.2f, 0.55f };
            for (int i = 0; i < sides.Length; i++)
                pieces.Add(new Piece(Kind.Leaf, new Vector3(cx, ground, cz + 0.2f), Now, i % 2 == 0 ? LeafLight : LeafMid, 0,
                    sides[i] + 0.04f * (steps % 3 - 1), MathF.Abs(sides[i]) < 0.3f ? 1f : 0.6f));
            return;
        }

        // Left foot and right foot in turn, each a little to its own side of the line walked
        float side = steps % 2 == 0 ? 0.11f : -0.11f;
        if (TileBehaviors.KeepsFootprints(behaviour))
        {
            var tint = behaviour == TileBehavior.Sand ? Rgb(196, 170, 118) : Rgb(140, 160, 206);
            int turn = facing switch { Direction.Up => 0, Direction.Right => 1, Direction.Down => 2, _ => 3 };
            pieces.Add(new Piece(Kind.Print, new Vector3(cx - dz * side, ground + 0.012f, cz + dx * side), Now, tint, turn, 0f, 1f));
        }

        if (running && RaisesDust(type))
            pieces.Add(new Piece(Kind.Puff, new Vector3(cx - dx * 0.45f - dz * side, ground, cz - dz * 0.45f + dx * side + 0.15f), Now, DustOf(type), 0, 0f, 1f));
    }

    /// <summary>Someone has landed from a hop over a ledge: dust to both sides of their feet (none off water).</summary>
    public void Landing(Map map, int x, int y)
    {
        if (!map.InBounds(x, y) || map.IsIndoors || GroundBaker.IsWaterAt(map, x, y)) return;
        float cx = x + 0.5f, cz = y + 0.5f, ground = Relief.At(map, cx, cz);
        var tint = DustOf(map.GetGroundTile(x, y));
        foreach (float side in new[] { -0.3f, 0.3f })
            pieces.Add(new Piece(Kind.Puff, new Vector3(cx + side, ground, cz + 0.25f), Now, tint, 0, side, 1.25f));
    }

    /// <summary>
    /// Water is thrown up on a tile: a ring, and drops to both sides. A puddle stepped in gives a small ring and
    /// two drops; water someone rides out onto gives a wide ring and four drops that fly higher.
    /// </summary>
    public void Splash(Map map, int x, int y, float size = 1.7f, int drops = 4)
    {
        if (!map.InBounds(x, y) || map.IsIndoors) return;
        float cx = x + 0.5f, cz = y + 0.5f, ground = Relief.At(map, cx, cz);
        pieces.Add(new Piece(Kind.Ring, new Vector3(cx, ground + 0.02f, cz), Now, RingTint, 0, 0f, size));
        float[] sides = drops > 2 ? new[] { -0.62f, -0.36f, 0.36f, 0.62f } : new[] { -0.18f, 0.2f };
        foreach (float side in sides)
            pieces.Add(new Piece(Kind.Drop, new Vector3(cx + side, ground, cz + 0.1f), Now, DropTint, 0, side * 0.5f, size));
    }

    /// <summary>
    /// Rain (or hail) is landing round a spot (in tiles): thirty times a second <paramref name="dropsABeat"/>
    /// drops come down on the ground in view of it, a fleck where one lands on open ground, and on water one
    /// in three leaves a ring. Where each lands follows from its number alone, so the rain is the same rain
    /// however the frames fall.
    /// </summary>
    public void Rainfall(Map map, float x, float z, int dropsABeat)
    {
        if (map.IsIndoors || dropsABeat <= 0) return;
        long beat = (long)(Now * RainBeats);
        // After a pause (a battle, a menu) only the last few beats are made up
        if (beat - rainBeat > 4) rainBeat = beat - 4;
        const float texel = 1f / 32f;
        while (rainBeat < beat)
        {
            rainBeat++;
            for (int i = 0; i < dropsABeat; i++)
            {
                int number = (int)rainBeat;
                float rx = MathF.Floor((x + (GroundBaker.Rand01(number, i, 921) - 0.5f) * RainWidth) * 32f) * texel;
                float rz = MathF.Floor((z + (GroundBaker.Rand01(number, i, 922) - 0.5f) * RainDepth) * 32f) * texel;
                int tx = (int)MathF.Floor(rx), tz = (int)MathF.Floor(rz);
                if (!map.InBounds(tx, tz)) continue;
                double born = rainBeat / (double)RainBeats;
                if (GroundBaker.IsWaterAt(map, tx, tz))
                {
                    // One drop in three leaves a ring: water under rain is busy, but still water
                    if (i % 3 == 0) pieces.Add(new Piece(Kind.Ring, new Vector3(rx, Relief.At(map, rx, rz) + 0.02f, rz), born, RingTint, 0, 0f, 0.55f));
                }
                else if (!map.IsSolid(tx, tz))
                    pieces.Add(new Piece(Kind.Fleck, new Vector3(rx, Relief.At(map, rx, rz) + 0.014f, rz), born, FleckTint, 0, 0f, 1f));
            }
        }
    }

    /// <summary>Alpha in the two or three flat steps things fade by.</summary>
    private static byte Fade(float left)
    {
        // left: 1 when fading starts, 0 when gone
        return left > 0.66f ? (byte)255 : left > 0.33f ? (byte)170 : (byte)90;
    }

    /// <summary>
    /// What there is to draw now: <paramref name="flat"/> lies on the ground (prints, rings, flecks),
    /// <paramref name="upright"/> stands up facing the eye (dust, leaves, drops). Sizes are in tiles; an upright
    /// thing's height is in screen rows of a tile, to be stretched like every upright thing in the field.
    /// </summary>
    public void Quads(List<LifeQuad> flat, List<LifeQuad> upright)
    {
        const float texel = 1f / 32f;
        foreach (var p in pieces)
        {
            float age = (float)(Now - p.Born);
            if (age < 0f) continue;
            switch (p.Kind)
            {
                case Kind.Print:
                {
                    // It stays, then goes in two steps
                    byte alpha = age < PrintStays ? (byte)220 : age < PrintStays + PrintFades / 2f ? (byte)150 : (byte)80;
                    flat.Add(new LifeQuad(p.At, 0.5f, 0.5f, Print, p.Tint with { A = alpha }, p.Turn));
                    break;
                }
                case Kind.Ring:
                {
                    int frame = Math.Min(2, (int)(age / RingTime * 3f));
                    float size = (8 + frame * 3) * 2 * texel * p.Size;
                    flat.Add(new LifeQuad(p.At, size, size * 0.7f, Ring + frame, p.Tint with { A = Fade(1f - age / RingTime) }));
                    break;
                }
                case Kind.Fleck:
                {
                    int frame = age < FleckTime / 2f ? 0 : 1;
                    flat.Add(new LifeQuad(p.At, 0.5f, 0.5f, Fleck + frame, p.Tint with { A = frame == 0 ? (byte)230 : (byte)150 }));
                    break;
                }
                case Kind.Puff:
                {
                    int frame = Math.Min(2, (int)(age / PuffTime * 3f));
                    float rise = MathF.Floor(age / PuffTime * 3f) * texel;
                    upright.Add(new LifeQuad(p.At + new Vector3(0, rise, 0), 0.5f * p.Size, 0.5f * p.Size, Puff + frame, p.Tint with { A = frame == 2 ? (byte)150 : (byte)230 }));
                    break;
                }
                case Kind.Leaf:
                {
                    // Up eighteen texels and back, flying out to its side, in whole texels; it turns over at the top
                    float a = age / LeafTime;
                    float up = MathF.Round(4f * a * (1f - a) * 18f * p.Size) * texel + 0.25f;
                    float along = MathF.Round(p.Seed * a * 40f) * texel + p.Seed * 0.3f;
                    // Those to one side start flat and those to the other turned, and each turns over as it tops its arc
                    int frame = ((a < 0.5f ? 0 : 1) + (p.Seed > 0f ? 1 : 0)) % 2;
                    upright.Add(new LifeQuad(p.At + new Vector3(along, up, 0), 0.5f, 0.5f, Leaf + frame, p.Tint with { A = a > 0.8f ? (byte)170 : (byte)255 }));
                    break;
                }
                default:
                {
                    float a = age / DropTime;
                    float up = (MathF.Round(4f * a * (1f - a) * 8f * p.Size) + 3f) * texel;
                    upright.Add(new LifeQuad(p.At + new Vector3(p.Seed * a, up, 0), 0.5f, 0.5f, Drop, p.Tint));
                    break;
                }
            }
        }
    }

    // ------------------------------------------------------------------ doors

    private readonly record struct Door(Map Map, int X, int Y, double OpenedAt, double? ShutAt, bool ShutHeard = false);

    private Door? door;

    /// <summary>A door starts to open (if it isn't open already): someone is stepping up to it. True if it has just begun to.</summary>
    public bool OpenDoor(Map map, int x, int y)
    {
        if (door is { } d && d.Map == map && d.X == x && d.Y == y && d.ShutAt == null) return false;
        door = new Door(map, x, y, Now, null);
        return true;
    }

    /// <summary>The door that has just begun to shut behind someone who came out of it, once: the moment to hear it.</summary>
    public (Map Map, int X, int Y)? TakeShutting()
    {
        if (door is not { ShutAt: { } shut } d || d.ShutHeard || Now < shut) return null;
        door = d with { ShutHeard = true };
        return (d.Map, d.X, d.Y);
    }

    /// <summary>Someone has just come out of a door: it stands open behind them, and shuts in a moment.</summary>
    public void LeaveDoor(Map map, int x, int y) => door = new Door(map, x, y, Now - DoorOpens, Now + DoorLingers);

    /// <summary>
    /// How far a door stands open, in the frames its art has: 0 shut, 1 ajar, 2 open. Only one door moves at a
    /// time: the one someone is going through.
    /// </summary>
    public int DoorFrame(Map map, int x, int y)
    {
        if (door is not { } d || d.Map != map || d.X != x || d.Y != y) return 0;
        if (d.ShutAt is { } shut && Now >= shut) return Now - shut < DoorOpens / 2f ? 1 : 0;
        return Now - d.OpenedAt < DoorOpens / 2f ? 1 : 2;
    }

    /// <summary>The door that is open or moving now, if there is one.</summary>
    public (Map Map, int X, int Y)? MovingDoor => door is { } d ? (d.Map, d.X, d.Y) : null;
}

/// <summary>The weather drawn over the picture: a function of the kind and the time alone.</summary>
internal static class WeatherFx
{
    private static float Hash(int i, int salt) => GroundBaker.Rand01(i, salt, 911);

    /// <summary>A size or a place in whole blocks of three layout units: the size of one texel of the field at 1080p.</summary>
    private static float Snap(float v) => MathF.Floor(v / 3f) * 3f;

    /// <summary>How many pieces a kind of weather keeps in the air.</summary>
    public static int CountOf(FieldWeather weather) => weather switch
    {
        FieldWeather.Rain => 220,
        FieldWeather.HeavyRain or FieldWeather.Thunderstorm => 380,
        FieldWeather.Snow => 140,
        FieldWeather.HeavySnow => 320,
        FieldWeather.Blizzard => 460,
        FieldWeather.Hail => 180,
        FieldWeather.Sandstorm => 240,
        FieldWeather.Ash => 110,
        _ => 0
    };

    /// <summary>
    /// How white lightning makes the picture at a moment (0 to 1). A storm strikes once in each eight seconds,
    /// one to six seconds in, so three to thirteen seconds apart: two flashes within a quarter of a second,
    /// the second the fainter, and a short glow after it.
    /// </summary>
    public static float Lightning(FieldWeather weather, double time)
    {
        if (weather != FieldWeather.Thunderstorm || time < 0.0) return 0f;
        const double window = 8.0;
        long n = (long)(time / window);
        float since = (float)(time - n * window) - (1f + GroundBaker.Rand01((int)n, 7, 915) * 5f);
        if (since < 0f) return 0f;
        if (since < 0.07f) return 0.8f;
        if (since < 0.13f) return 0f;
        if (since < 0.22f) return 0.5f;
        return since < 0.4f ? 0.25f * (1f - (since - 0.22f) / 0.18f) : 0f;
    }

    /// <summary>
    /// What falls through the air at a moment, in blocks over a picture of the given size. The time is taken
    /// to double precision and each piece's place wrapped round its own span before it is rounded, so the
    /// weather moves as smoothly late in a long session as at its start.
    /// </summary>
    public static void Build(FieldWeather weather, double time, float width, float height, List<WeatherBlock> into)
    {
        // Where something that started at `start` and moves `speed` a second is now, on a line `span` long that wraps
        float Along(float start, float speed, float span) => (float)((start + time * speed) % span);

        int count = CountOf(weather);
        switch (weather)
        {
            case FieldWeather.Rain or FieldWeather.HeavyRain or FieldWeather.Thunderstorm:
            {
                // A streak is three blocks (four in heavy rain), each a block further east than the one above:
                // it falls along its own slant
                bool heavy = weather != FieldWeather.Rain;
                for (int i = 0; i < count; i++)
                {
                    float speed = (heavy ? 1800f : 1500f) + Hash(i, 1) * 500f, span = height + 100f;
                    float y = Along(Hash(i, 2) * span, speed, span) - 50f;
                    float x = Hash(i, 3) * (width + 400f) - 400f + y / 3f;
                    var streak = new Color(176, 200, 236, Hash(i, 4) > 0.75f ? 210 : 150);
                    float sx = Snap(x), sy = Snap(y);
                    into.Add(new WeatherBlock(sx, sy, 3f, 9f, streak));
                    into.Add(new WeatherBlock(sx + 3f, sy + 9f, 3f, 9f, streak));
                    into.Add(new WeatherBlock(sx + 6f, sy + 18f, 3f, heavy ? 9f : 6f, streak));
                    if (heavy) into.Add(new WeatherBlock(sx + 9f, sy + 27f, 3f, 6f, streak));
                }
                break;
            }
            case FieldWeather.Hail:
            {
                // Pellets falling fast and steep, each a block with a paler block above it
                for (int i = 0; i < count; i++)
                {
                    float speed = 1100f + Hash(i, 1) * 400f, span = height + 60f;
                    float y = Along(Hash(i, 2) * span, speed, span) - 30f;
                    float x = Hash(i, 3) * (width + 200f) - 200f + y / 6f;
                    float sx = Snap(x), sy = Snap(y);
                    into.Add(new WeatherBlock(sx, sy, 6f, 6f, new Color(224, 238, 255, 235)));
                    into.Add(new WeatherBlock(sx, sy - 6f, 3f, 6f, new Color(224, 238, 255, 120)));
                }
                break;
            }
            case FieldWeather.Snow or FieldWeather.Ash:
            {
                bool ash = weather == FieldWeather.Ash;
                for (int i = 0; i < count; i++)
                {
                    float fall = (ash ? 60f : 130f) + Hash(i, 1) * (ash ? 70f : 90f);
                    float span = height + 40f;
                    float y = Along(Hash(i, 2) * span, fall, span) - 20f;
                    // Each sways to its own beat, four blocks to either side
                    float x = Hash(i, 3) * width + MathF.Round((float)Math.Sin(time * (0.7f + Hash(i, 5)) + i) * 4f) * 3f;
                    bool big = Hash(i, 6) > 0.6f;
                    var color = ash
                        ? (big ? new Color(190, 186, 186, 210) : new Color(150, 146, 150, 225))
                        : new Color(255, 255, 255, big ? 240 : 200);
                    float size = big ? 6f : 3f;
                    into.Add(new WeatherBlock(Snap(x), Snap(y), size, size, color));
                }
                break;
            }
            case FieldWeather.HeavySnow or FieldWeather.Blizzard:
            {
                bool blizzard = weather == FieldWeather.Blizzard;
                for (int i = 0; i < count; i++)
                {
                    float fall = (blizzard ? 300f : 380f) + Hash(i, 1) * 260f, drift = (blizzard ? 1300f : 560f) + Hash(i, 4) * (blizzard ? 500f : 200f);
                    float span = height + 40f, wide = width + 400f;
                    float y = Along(Hash(i, 2) * span, fall, span) - 20f;
                    float x = Along(Hash(i, 3) * wide, drift, wide) - 200f;
                    float pick = Hash(i, 6), size = pick > 0.78f ? 9f : pick > 0.35f ? 6f : 3f;
                    float sx = Snap(x), sy = Snap(y);
                    into.Add(new WeatherBlock(sx, sy, size, size, new Color(255, 255, 255, size > 3f ? 240 : 200)));
                    // The tail it leaves, up the wind: longer and flatter the harder it blows
                    if (size > 3f) into.Add(new WeatherBlock(sx - (blizzard ? 9f : 3f), sy - 3f, blizzard ? 9f : 3f, 3f, new Color(255, 255, 255, 150)));
                }
                break;
            }
            case FieldWeather.Sandstorm:
            {
                for (int i = 0; i < count; i++)
                {
                    float fall = 100f + Hash(i, 1) * 100f, drift = 1700f + Hash(i, 4) * 600f;
                    float span = height + 40f, wide = width + 400f;
                    float y = Along(Hash(i, 2) * span, fall, span) - 20f;
                    float x = Along(Hash(i, 3) * wide, drift, wide) - 200f;
                    bool big = Hash(i, 6) > 0.5f;
                    into.Add(new WeatherBlock(Snap(x), Snap(y), big ? 24f : 12f, 3f, big ? new Color(226, 198, 140, 200) : new Color(240, 220, 170, 160)));
                }
                break;
            }
        }
    }

    /// <summary>
    /// The layers of mist a kind of weather lays over the picture at a moment, the nearest last. A layer's
    /// offset wraps where its cloud repeats, so it stays small however long the mist has drifted.
    /// </summary>
    public static void Haze(FieldWeather weather, double time, List<HazeLayer> into)
    {
        // A layer whose texels are `scale` units wide, begun `lead` units along, drifting east and down at these speeds
        void Layer(float scale, float lead, float east, float down, Color tint)
        {
            double repeat = LifeArt.HazeSize * (double)scale;
            into.Add(new HazeLayer(scale, (float)((lead + time * east) % repeat), (float)(time * down % repeat), tint));
        }

        switch (weather)
        {
            case FieldWeather.Fog:
                Layer(26f, 400f, 7f, 3f, new Color(232, 236, 244, 100));
                Layer(15f, 0f, 16f, 0f, new Color(232, 236, 244, 130));
                break;
            case FieldWeather.Sandstorm:
                Layer(22f, 300f, 170f, 0f, new Color(226, 198, 140, 90));
                Layer(12f, 0f, 300f, 20f, new Color(226, 198, 140, 120));
                break;
            case FieldWeather.HeavySnow:
                Layer(18f, 0f, 220f, 90f, new Color(255, 255, 255, 70));
                break;
            case FieldWeather.Blizzard:
                Layer(24f, 200f, 380f, 60f, new Color(255, 255, 255, 110));
                Layer(14f, 0f, 620f, 110f, new Color(255, 255, 255, 130));
                break;
        }
    }
}

/// <summary>The pixel art of the field's small effects and of the emote bubbles. No GPU calls.</summary>
internal static class LifeArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>A rim one texel wide, in one colour, round what is painted in a cell so far.</summary>
    private static void Rim(PixelCanvas c, int x0, int y0, int size, Color col)
    {
        var marks = new List<(int X, int Y)>();
        for (int y = y0; y < y0 + size; y++)
            for (int x = x0; x < x0 + size; x++)
            {
                if (c.IsOpaque(x, y)) continue;
                bool Painted(int px, int py) => px >= x0 && px < x0 + size && py >= y0 && py < y0 + size && c.IsOpaque(px, py);
                if (Painted(x + 1, y) || Painted(x - 1, y) || Painted(x, y + 1) || Painted(x, y - 1)) marks.Add((x, y));
            }
        foreach (var (x, y) in marks) c.SetRaw(x, y, col);
    }

    /// <summary>
    /// The atlas of effect cells, 16 texels each, eight to a row, in white and greys so each is tinted when
    /// drawn: a shoe print, three frames of a puff, a leaf flat and turned over, three frames of a ring, a drop,
    /// and the two frames of a raindrop landing.
    /// </summary>
    public static PixelCanvas Atlas()
    {
        const int cell = FieldLife.Cell;
        var c = new PixelCanvas(cell * FieldLife.Columns, cell * 2);
        var white = Color.White;
        var grey = Rgb(206, 206, 214);
        var dark = Rgb(96, 100, 96);
        (int X, int Y) Origin(int index) => (cell * (index % FieldLife.Columns), cell * (index / FieldLife.Columns));

        // A shoe print, toe to the top: the sole, rounded at both ends, and a texel apart the heel
        c.Rect(5, 3, 6, 5, white);
        c.Rect(6, 2, 4, 1, white);
        c.Rect(6, 8, 4, 1, white);
        c.Rect(6, 10, 4, 2, white);
        c.Rect(7, 12, 2, 1, white);

        // Puffs: discs of 6, 10 and 12 texels with a grey lower right
        int[] sizes = { 6, 10, 12 };
        for (int f = 0; f < 3; f++)
        {
            int ox = cell * (FieldLife.Puff + f), size = sizes[f], x0 = ox + (cell - size) / 2, y0 = cell - size - 1;
            Pix.Disc(c, x0, y0, size, grey);
            Pix.Disc(c, x0, y0, size - 2, white);
            if (f == 2)
            {
                // The last frame is breaking up: holes in the cloud
                c.SetRaw(x0 + 5, y0 + 5, default);
                c.SetRaw(x0 + 6, y0 + 5, default);
                c.SetRaw(x0 + 3, y0 + 8, default);
                c.SetRaw(x0 + 4, y0 + 8, default);
            }
        }

        // A leaf lying flat, six texels by four, lit above and shaded below, with a dark rim
        var (lx, ly) = Origin(FieldLife.Leaf);
        c.Rect(lx + 6, ly + 6, 4, 1, white);
        c.Rect(lx + 5, ly + 7, 6, 1, white);
        c.Rect(lx + 5, ly + 8, 6, 1, grey);
        c.Rect(lx + 6, ly + 9, 4, 1, grey);
        Rim(c, lx, ly, cell, dark);

        // The same leaf turned over: a blade from lower left to upper right
        var (tx, ty) = Origin(FieldLife.Leaf + 1);
        c.Rect(tx + 9, ty + 5, 2, 1, white);
        c.Rect(tx + 8, ty + 6, 3, 1, white);
        c.Rect(tx + 7, ty + 7, 3, 1, white);
        c.Rect(tx + 6, ty + 8, 3, 1, grey);
        c.Rect(tx + 5, ty + 9, 3, 1, grey);
        c.Rect(tx + 5, ty + 10, 2, 1, grey);
        Rim(c, tx, ty, cell, dark);

        // A drop
        var (dx, dy) = Origin(FieldLife.Drop);
        c.Rect(dx + 7, dy + 6, 2, 1, white);
        c.Rect(dx + 6, dy + 7, 4, 2, white);

        // Rings seen from above: ellipses of 8, 11 and 14 texels, a texel thick, the widest broken in four
        for (int f = 0; f < 3; f++)
        {
            var (ox, oy) = Origin(FieldLife.Ring + f);
            float rx = 4f + f * 1.5f, ry = rx * 0.6f;
            for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    float u = (x + 0.5f - 8f) / rx, v = (y + 0.5f - 8f) / ry;
                    float d = u * u + v * v;
                    float inner = (rx - 1.2f) / rx;
                    if (d > 1f || d < inner * inner) continue;
                    if (f == 2 && (Math.Abs(x - 8) < 2 || Math.Abs(y - 8) < 1)) continue;
                    c.SetRaw(ox + x, oy + y, white);
                }
        }

        // A raindrop landing: a spot, then four specks flying apart
        var (fx, fy) = Origin(FieldLife.Fleck);
        c.Rect(fx + 7, fy + 6, 2, 3, white);
        c.Rect(fx + 6, fy + 7, 4, 2, white);
        var (gx, gy) = Origin(FieldLife.Fleck + 1);
        c.Rect(gx + 3, gy + 8, 2, 1, white);
        c.Rect(gx + 11, gy + 8, 2, 1, white);
        c.Rect(gx + 5, gy + 6, 1, 1, white);
        c.Rect(gx + 10, gy + 6, 1, 1, white);
        c.Rect(gx + 7, gy + 5, 2, 1, white);
        c.Rect(gx + 7, gy + 10, 2, 1, grey);
        return c;
    }

    public const int HazeSize = 128;

    /// <summary>
    /// Mist: a cloud that repeats at its edges, white, its alpha the thickness of the cloud. Drawn large and
    /// smooth over the picture, in layers (<see cref="WeatherFx.Haze"/>).
    /// </summary>
    public static PixelCanvas Haze()
    {
        var c = new PixelCanvas(HazeSize, HazeSize);
        for (int y = 0; y < HazeSize; y++)
            for (int x = 0; x < HazeSize; x++)
            {
                float n = 0.5f * Lattice(x, y, 32) + 0.3f * Lattice(x, y, 16) + 0.2f * Lattice(x, y, 8);
                // Thin cloud is no cloud: wisps with clear air between them
                float a = Math.Clamp((n - 0.3f) / 0.42f, 0f, 1f);
                a = a * a * (3f - 2f * a);
                c.SetRaw(x, y, new Color(255, 255, 255, (int)(a * 255f)));
            }
        return c;
    }

    /// <summary>Smooth noise from a lattice <paramref name="cell"/> texels apart that wraps at the haze's edge.</summary>
    private static float Lattice(int x, int y, int cell)
    {
        int period = HazeSize / cell;
        float fx = x / (float)cell, fy = y / (float)cell;
        int x0 = (int)fx, y0 = (int)fy;
        float tx = fx - x0, ty = fy - y0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float At(int i, int j) => GroundBaker.Rand01(i % period, j % period, 930 + cell);
        float top = At(x0, y0) + (At(x0 + 1, y0) - At(x0, y0)) * tx;
        float bottom = At(x0, y0 + 1) + (At(x0 + 1, y0 + 1) - At(x0, y0 + 1)) * tx;
        return top + (bottom - top) * ty;
    }

    public const int BubbleWidth = 20, BubbleHeight = 22;

    /// <summary>
    /// A bubble over someone's head, 20 by 22: white with a tail, and its sign in the middle. The three frames
    /// it pops up in are the same bubble at 10, 16 and 20 texels wide.
    /// </summary>
    public static PixelCanvas Bubble(EmoteBubble kind)
    {
        var c = new PixelCanvas(BubbleWidth, BubbleHeight);
        var white = Rgb(252, 252, 250);
        var shade = Rgb(206, 210, 224);
        c.Rect(2, 1, 16, 15, white);
        c.Rect(1, 2, 18, 13, white);
        c.Rect(2, 15, 16, 1, shade);
        // The tail
        c.Rect(8, 16, 4, 2, white);
        c.Rect(9, 18, 2, 2, white);

        var red = Rgb(226, 56, 56);
        var blue = Rgb(58, 108, 214);
        var ink = Rgb(74, 70, 96);
        switch (kind)
        {
            case EmoteBubble.Exclaim:
                c.Rect(9, 3, 2, 7, red);
                c.Rect(9, 12, 2, 2, red);
                break;
            case EmoteBubble.Question:
                c.Rect(7, 3, 6, 2, blue);
                c.Rect(11, 5, 2, 3, blue);
                c.Rect(9, 7, 2, 3, blue);
                c.Rect(6, 5, 2, 2, blue);
                c.Rect(9, 12, 2, 2, blue);
                break;
            case EmoteBubble.Dots:
                foreach (int x in new[] { 4, 9, 14 }) c.Rect(x, 8, 2, 2, ink);
                break;
            case EmoteBubble.Note:
                c.Rect(11, 3, 2, 8, ink);
                c.Rect(13, 3, 2, 3, ink);
                c.Rect(7, 9, 5, 4, ink);
                break;
            case EmoteBubble.Heart:
                c.Rect(5, 4, 4, 4, red);
                c.Rect(11, 4, 4, 4, red);
                c.Rect(6, 7, 8, 3, red);
                c.Rect(8, 10, 4, 2, red);
                c.Rect(9, 12, 2, 1, red);
                c.Rect(6, 5, 1, 1, Rgb(250, 160, 160));
                break;
            case EmoteBubble.Sleep:
                // Two Zs, the second smaller
                c.Rect(4, 5, 6, 1, blue); c.Rect(7, 6, 2, 2, blue); c.Rect(5, 8, 2, 2, blue); c.Rect(4, 10, 6, 1, blue);
                c.Rect(12, 3, 4, 1, blue); c.Rect(14, 4, 1, 1, blue); c.Rect(13, 5, 1, 1, blue); c.Rect(12, 6, 4, 1, blue);
                break;
            case EmoteBubble.Sweat:
                c.Rect(9, 3, 2, 3, blue);
                c.Rect(8, 6, 4, 4, blue);
                c.Rect(7, 8, 6, 3, blue);
                c.Rect(8, 11, 4, 1, blue);
                c.Rect(9, 8, 1, 2, Rgb(170, 206, 250));
                break;
        }
        Pix.Outline(c);
        return c;
    }

    private static readonly Color Dark = Rgb(40, 34, 48);

    /// <summary>
    /// A door standing ajar (<paramref name="frame"/> 1) or open (2), the size of the shut door it covers on the
    /// wall: the dark of the room, with a wooden leaf swung in against its frame, or glass panes slid aside.
    /// </summary>
    public static PixelCanvas OpenDoor(bool glass, int width, int height, int frame)
    {
        var c = new PixelCanvas(width, height);
        if (glass)
        {
            // The steel frame stays; the two panes have slid a third, then two thirds, of the way apart
            var steel = Tone.Of(200, 206, 218, 232, 236, 242, 140, 148, 168);
            Pix.Raised(c, 0, 0, width, height, steel);
            int inner = width - 4, gap = inner * frame / 3;
            c.Rect(2 + (inner - gap) / 2, 7, Math.Max(2, gap), height - 10, Dark);
            return c;
        }

        // A wooden door: the opening, and the leaf seen edge-on against the hinge side
        var frameTone = Rgb(248, 246, 238);
        var wood = Tone.Of(150, 98, 62, 178, 124, 82, 108, 70, 50);
        c.Rect(0, 0, width, height, frameTone);
        c.Rect(2, 2, width - 4, height - 2, Dark);
        int leaf = frame == 1 ? (width - 4) / 2 : 4;
        Pix.Raised(c, 2, 2, leaf, height - 2, wood);
        // The light from outside falls a little way in
        c.Rect(2 + leaf, height - 4, width - 4 - leaf, 4, Rgb(74, 62, 76));
        return c;
    }
}
