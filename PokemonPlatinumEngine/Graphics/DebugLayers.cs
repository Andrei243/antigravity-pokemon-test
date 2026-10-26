using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The field's rules a developer can draw over the picture (plan 16 · T18), one flag each; several are on at once.
/// Never a setting: the console's <c>layers</c>, a scenario's line of the same name and the harness's
/// <c>SHOTS_LAYERS</c> turn them on.
/// </summary>
[Flags]
internal enum DebugLayer
{
    None = 0,
    /// <summary>The tiles that block of themselves (<see cref="Map.IsSolid"/>).</summary>
    Solid = 1,
    /// <summary>Each tile's behaviour (<see cref="Map.BehaviourAt"/>): its number, and its name where it first shows.</summary>
    Behaviour = 2,
    /// <summary>The walked height (<see cref="Map.HeightAt"/>), with the drawn height beside it where the two differ.</summary>
    Height = 4,
    /// <summary>The step triggers, with the variable and value each waits for.</summary>
    Triggers = 8,
    /// <summary>The warps, with where each leads, or that it is locked or shut.</summary>
    Warps = 16,
    /// <summary>Each waiting trainer's sight, as <see cref="TrainerApproach.CanSee"/> walks it.</summary>
    Sight = 32,
    /// <summary>The borders where the area changes, with each area's key.</summary>
    Areas = 64,
    /// <summary>The tiles a step may meet a wild Pokémon on, land and water told apart.</summary>
    Encounters = 128,
    /// <summary>The chunks' borders of a streamed map, with what each chunk's scene has come to.</summary>
    Chunks = 256,
    All = Solid | Behaviour | Height | Triggers | Warps | Sight | Areas | Encounters | Chunks
}

/// <summary>What a chunk's scene has come to this frame, for the <see cref="DebugLayer.Chunks"/> layer.</summary>
internal enum ChunkState
{
    /// <summary>Nothing of it is asked for yet.</summary>
    None,
    /// <summary>Baking on another thread, or being uploaded a few steps a frame.</summary>
    Preparing,
    /// <summary>Drawn.</summary>
    Ready,
    /// <summary>Came into view before it was ready: the frame stopped for it.</summary>
    Waited
}

/// <summary>A tile tinted by a layer, at the height it is drawn at (<see cref="DebugLayers.LiftOf"/>).</summary>
internal readonly record struct LayerQuad(int X, int Y, float Lift, Color Colour, DebugLayer Layer);

/// <summary>A line along the tiles' edges, from (X0, Z0) to (X1, Z1) in tiles, drawn on the ground.</summary>
internal readonly record struct LayerLine(float X0, float Z0, float X1, float Z1, Color Colour, DebugLayer Layer);

/// <summary>Words at a point of the ground (in tiles; the middle of a tile is x + 0.5), drawn in the 2D pass.</summary>
internal readonly record struct LayerLabel(float X, float Z, float Lift, string Text, Color Colour, DebugLayer Layer);

/// <summary>What the layers on draw over a window of the map.</summary>
internal sealed class LayerPicture
{
    public List<LayerQuad> Quads { get; } = new();
    public List<LayerLine> Lines { get; } = new();
    public List<LayerLabel> Labels { get; } = new();
    public bool IsEmpty => Quads.Count == 0 && Lines.Count == 0 && Labels.Count == 0;
}

/// <summary>
/// The field's rules drawn as layers (plan 16 · T18), GPU-free: what each layer on shows over a window of the map, as
/// tinted tiles, lines on the ground and labels. Built for a window round the camera alone, never for the whole map of
/// Sinnoh. <see cref="WorldRenderer"/> draws it; tests read it.
/// </summary>
internal static class DebugLayers
{
    /// <summary>The widest window built, in tiles: what a camera zoomed out sees, and no more.</summary>
    public const int MostTiles = 96;

    public static readonly Color SolidColour = new(230, 40, 40, 110);
    public static readonly Color TriggerColour = new(255, 226, 40, 120);
    public static readonly Color WarpColour = new(228, 52, 200, 140);
    public static readonly Color LockedColour = new(120, 20, 90, 160);
    public static readonly Color SightColour = new(255, 140, 20, 120);
    public static readonly Color AreaColour = new(255, 255, 255, 230);
    public static readonly Color LandColour = new(60, 200, 70, 100);
    public static readonly Color WaterColour = new(40, 120, 240, 110);
    public static readonly Color LabelColour = new(255, 255, 255, 255);

    /// <summary>The colour of a chunk's border and label by what its scene has come to.</summary>
    public static Color ChunkColour(ChunkState state) => state switch
    {
        ChunkState.Ready => new Color(60, 220, 90, 230),
        ChunkState.Preparing => new Color(250, 210, 40, 230),
        ChunkState.Waited => new Color(240, 40, 40, 230),
        _ => new Color(150, 150, 150, 230)
    };

    /// <summary>The names the layers are turned on by, in the order <c>layers</c> lists them.</summary>
    public static IReadOnlyList<(string Name, DebugLayer Layer)> Names { get; } = new[]
    {
        ("solid", DebugLayer.Solid), ("behaviour", DebugLayer.Behaviour), ("height", DebugLayer.Height),
        ("triggers", DebugLayer.Triggers), ("warps", DebugLayer.Warps), ("sight", DebugLayer.Sight),
        ("areas", DebugLayer.Areas), ("encounters", DebugLayer.Encounters), ("chunks", DebugLayer.Chunks)
    };

    /// <summary>
    /// The layers a list of names turns on (<c>solid,sight</c>, spaces or commas between; <c>all</c>; <c>off</c> or
    /// <c>none</c> for none), or null with the first name that is no layer's.
    /// </summary>
    public static DebugLayer? Parse(string names, out string? unknown)
    {
        unknown = null;
        var layers = DebugLayer.None;
        foreach (var raw in names.Split(new[] { ',', ' ', '+' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string name = raw.Trim().ToLowerInvariant();
            if (name is "off" or "none") continue;
            if (name == "all") { layers |= DebugLayer.All; continue; }
            // (both spellings of the one that differs, and a plural or a singular of each)
            if (name == "behavior" || name == "behaviours" || name == "behaviors") name = "behaviour";
            var found = Names.FirstOrDefault(n => n.Name == name || n.Name == name + "s" || n.Name.TrimEnd('s') == name);
            if (found.Name == null) { unknown = raw; return null; }
            layers |= found.Layer;
        }
        return layers;
    }

    /// <summary>The console's and the scenarios' <c>layers</c> (plan 16 · T6, T17): why its words are no good, or null.</summary>
    internal static string? CheckWords(string[] words) =>
        Parse(string.Join(",", words), out var unknown) == null
            ? $"'{unknown}' is no layer ({string.Join(", ", Names.Select(n => n.Name))}, all or off)" : null;

    /// <summary>The console's and the scenarios' <c>layers</c>: the layers named are the ones drawn from now on.</summary>
    internal static Core.DevResult Command(Core.GameEngine engine, string[] words)
    {
        var layers = Parse(string.Join(",", words), out _)!.Value;
        engine.Drive.World.Layers = layers;
        return Core.DevResult.Done($"layers: {Describe(layers)}");
    }

    /// <summary>The names of the layers on, as <see cref="Parse"/> reads them back; "off" for none.</summary>
    public static string Describe(DebugLayer layers) =>
        layers == DebugLayer.None ? "off" : string.Join(",", Names.Where(n => (layers & n.Layer) != 0).Select(n => n.Name));

    /// <summary>A window cut to the map and to <see cref="MostTiles"/> round its middle.</summary>
    public static TileWindow Clamp(Map map, TileWindow window)
    {
        int width = Math.Min(window.Width, MostTiles), height = Math.Min(window.Height, MostTiles);
        int x = window.X + (window.Width - width) / 2, y = window.Y + (window.Height - height) / 2;
        int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
        int x1 = Math.Min(map.Width, x + width), y1 = Math.Min(map.Height, y + height);
        return new TileWindow(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
    }

    /// <summary>The height a tile's tint is drawn at: the deck's where there is one, the ground's drawn height otherwise.</summary>
    public static float LiftOf(Map map, int x, int y)
    {
        float cx = x + 0.5f, cz = y + 0.5f;
        return map.DeckAt(x, y) is { } deck ? Relief.Under(map, cx, cz, deck) : Relief.At(map, cx, cz);
    }

    /// <summary>
    /// What the layers on show over a window of a map: never anything outside the window (cut to the map and to
    /// <see cref="MostTiles"/>). <paramref name="chunkState"/> says what each chunk's scene has come to (by the chunk's
    /// column and row); without it every chunk of a streamed map is told as nothing asked for.
    /// </summary>
    public static LayerPicture Build(Map map, TileWindow window, DebugLayer layers, Func<int, int, ChunkState>? chunkState = null)
    {
        var picture = new LayerPicture();
        if (layers == DebugLayer.None) return picture;
        window = Clamp(map, window);
        if (window.Width == 0 || window.Height == 0) return picture;

        if ((layers & DebugLayer.Solid) != 0) Solid(map, window, picture);
        if ((layers & DebugLayer.Encounters) != 0) Encounters(map, window, picture);
        if ((layers & DebugLayer.Behaviour) != 0) Behaviour(map, window, picture);
        if ((layers & DebugLayer.Height) != 0) Height(map, window, picture);
        if ((layers & DebugLayer.Sight) != 0) Sight(map, window, picture);
        if ((layers & DebugLayer.Triggers) != 0) Triggers(map, window, picture);
        if ((layers & DebugLayer.Warps) != 0) Warps(map, window, picture);
        if ((layers & DebugLayer.Areas) != 0) Areas(map, window, picture);
        if ((layers & DebugLayer.Chunks) != 0) Chunks(map, window, picture, chunkState);
        return picture;
    }

    private static void Solid(Map map, TileWindow w, LayerPicture picture)
    {
        for (int y = w.Y; y < w.Bottom; y++)
            for (int x = w.X; x < w.Right; x++)
                if (map.IsSolid(x, y)) picture.Quads.Add(new LayerQuad(x, y, LiftOf(map, x, y), SolidColour, DebugLayer.Solid));
    }

    private static void Encounters(Map map, TileWindow w, LayerPicture picture)
    {
        for (int y = w.Y; y < w.Bottom; y++)
            for (int x = w.X; x < w.Right; x++)
            {
                var behaviour = map.BehaviourAt(x, y);
                if (!TileBehaviors.HasEncounters(behaviour)) continue;
                picture.Quads.Add(new LayerQuad(x, y, LiftOf(map, x, y), TileBehaviors.IsSurfable(behaviour) ? WaterColour : LandColour, DebugLayer.Encounters));
            }
    }

    // A behaviour's tint: its number spread round the hue circle, so neighbours of the table are told apart
    private static Color BehaviourColour(byte value)
    {
        float hue = (value * 0.618034f) % 1f * 6f;
        // (hue to red, green and blue at a saturation of three quarters, worked out here so no window is needed)
        float Channel(float n)
        {
            float k = (n + hue) % 6f;
            return 0.95f * (1f - 0.75f * Math.Clamp(Math.Min(k, 4f - k), 0f, 1f));
        }
        return new Color((byte)(Channel(5f) * 255f), (byte)(Channel(3f) * 255f), (byte)(Channel(1f) * 255f), (byte)110);
    }

    private static void Behaviour(Map map, TileWindow w, LayerPicture picture)
    {
        var named = new HashSet<TileBehavior>();
        for (int y = w.Y; y < w.Bottom; y++)
            for (int x = w.X; x < w.Right; x++)
            {
                var behaviour = map.BehaviourAt(x, y);
                byte value = (byte)behaviour;
                if (value == 0) continue;
                float lift = LiftOf(map, x, y);
                picture.Quads.Add(new LayerQuad(x, y, lift, BehaviourColour(value), DebugLayer.Behaviour));
                // Its number on every tile, and its name beside the first tile of it the window shows
                string text = "0x" + value.ToString("X2", CultureInfo.InvariantCulture);
                if (named.Add(behaviour)) text += " " + behaviour;
                picture.Labels.Add(new LayerLabel(x + 0.5f, y + 0.5f, lift, text, LabelColour, DebugLayer.Behaviour));
            }
    }

    // The walked height's tint: lower is blue, higher red, over four tiles of climb from the map's ground
    private static Color HeightColour(float height, float ground)
    {
        float t = Math.Clamp((height - ground) / 4f, 0f, 1f);
        return new Color((byte)(40 + 200 * t), (byte)(90 + 60 * (1f - Math.Abs(t - 0.5f) * 2f)), (byte)(230 - 190 * t), (byte)100);
    }

    private static void Height(Map map, TileWindow w, LayerPicture picture)
    {
        for (int y = w.Y; y < w.Bottom; y++)
            for (int x = w.X; x < w.Right; x++)
            {
                float walked = map.HeightAt(x, y);
                float drawn = LiftOf(map, x, y);
                picture.Quads.Add(new LayerQuad(x, y, drawn, HeightColour(walked, map.GroundLevel), DebugLayer.Height));
                // The walked height where there is any, and the drawn one beside it where the two differ
                float walkedDrawn = walked - map.GroundLevel;
                bool differs = MathF.Abs(walkedDrawn - drawn) > 0.01f;
                if (walked == 0f && !differs) continue;
                string text = Number(walked) + (differs ? " / " + Number(drawn) : "");
                picture.Labels.Add(new LayerLabel(x + 0.5f, y + 0.5f, drawn, text, LabelColour, DebugLayer.Height));
            }
    }

    private static string Number(float value) => value.ToString(value == MathF.Round(value) ? "0" : "0.##", CultureInfo.InvariantCulture);

    /// <summary>The tiles a trainer looks over, as <see cref="TrainerApproach.CanSee"/> answers them, within a window.</summary>
    public static IEnumerable<(int X, int Y)> SightOf(Map map, NPC npc, TileWindow w)
    {
        int range = npc.TrainerData?.SightRange ?? 0;
        var (sx, sy) = npc.Facing switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            _ => (1, 0)
        };
        for (int d = 1; d <= range; d++)
        {
            int tx = npc.GridX + sx * d, ty = npc.GridY + sy * d;
            if (!map.InBounds(tx, ty)) yield break;
            if (!TrainerApproach.CanSee(map, npc, tx, ty)) yield break;
            if (w.Contains(tx, ty)) yield return (tx, ty);
        }
    }

    private static void Sight(Map map, TileWindow w, LayerPicture picture)
    {
        // Only the people a trainer's eyes can reach the window from are asked
        int cx = w.X + w.Width / 2, cy = w.Y + w.Height / 2, reach = Math.Max(w.Width, w.Height) / 2 + TrainerApproach.SightReach;
        foreach (var npc in map.NPCs.Near(cx, cy, reach))
        {
            if (!npc.IsTrainer || npc.HasBattled || npc.TrainerData == null) continue;
            foreach (var (x, y) in SightOf(map, npc, w))
                picture.Quads.Add(new LayerQuad(x, y, LiftOf(map, x, y), SightColour, DebugLayer.Sight));
            if (w.Contains(npc.GridX, npc.GridY))
                picture.Labels.Add(new LayerLabel(npc.GridX + 0.5f, npc.GridY + 0.5f, LiftOf(map, npc.GridX, npc.GridY),
                    $"{npc.TrainerData.Id} sight {npc.TrainerData.SightRange}", SightColour with { A = 255 }, DebugLayer.Sight));
        }
    }

    /// <summary>A trigger's words, as the content check's plan names it: its script, and the variable and value it waits for.</summary>
    public static string TriggerLabel(StepTrigger trigger) =>
        $"trigger {trigger.Script}" + (trigger.Variable != null ? $" while {trigger.Variable} = {trigger.Value}" : "");

    private static void Triggers(Map map, TileWindow w, LayerPicture picture)
    {
        foreach (var trigger in map.Triggers)
        {
            bool any = false;
            for (int y = Math.Max(w.Y, trigger.Y); y < Math.Min(w.Bottom, trigger.Y + trigger.Depth); y++)
                for (int x = Math.Max(w.X, trigger.X); x < Math.Min(w.Right, trigger.X + trigger.Width); x++)
                {
                    if (!any)
                        picture.Labels.Add(new LayerLabel(x + 0.5f, y + 0.5f, LiftOf(map, x, y), TriggerLabel(trigger), TriggerColour with { A = 255 }, DebugLayer.Triggers));
                    any = true;
                    picture.Quads.Add(new LayerQuad(x, y, LiftOf(map, x, y), TriggerColour, DebugLayer.Triggers));
                }
        }
    }

    /// <summary>A warp's words, as the content check's plan names it: where it leads, or that the story has it shut or locked.</summary>
    public static string WarpLabel(Map map, Warp warp)
    {
        if (warp.Shut) return $"shut ({(warp.Until != null ? "until " + warp.Until : "from " + warp.From)})";
        string where = $"-> {warp.TargetMap} {warp.TargetX},{warp.TargetY}";
        if (warp.OpenedBy != null && map.InBounds(warp.SourceX, warp.SourceY) && map.IsSolid(warp.SourceX, warp.SourceY))
            return $"locked until {warp.OpenedBy} {where}";
        return where + (warp.CyclistsOnly ? " (cyclists only)" : "");
    }

    private static void Warps(Map map, TileWindow w, LayerPicture picture)
    {
        foreach (var warp in map.Warps)
        {
            if (!w.Contains(warp.SourceX, warp.SourceY)) continue;
            float lift = LiftOf(map, warp.SourceX, warp.SourceY);
            bool closed = warp.Shut || warp.OpenedBy != null && map.IsSolid(warp.SourceX, warp.SourceY);
            picture.Quads.Add(new LayerQuad(warp.SourceX, warp.SourceY, lift, closed ? LockedColour : WarpColour, DebugLayer.Warps));
            picture.Labels.Add(new LayerLabel(warp.SourceX + 0.5f, warp.SourceY + 0.5f, lift, WarpLabel(map, warp), WarpColour with { A = 255 }, DebugLayer.Warps));
        }
    }

    private static void Areas(Map map, TileWindow w, LayerPicture picture)
    {
        var named = new HashSet<MapArea>();
        for (int y = w.Y; y < w.Bottom; y++)
            for (int x = w.X; x < w.Right; x++)
            {
                var area = map.AreaAt(x, y);
                if (area == null) continue;
                float lift = LiftOf(map, x, y);
                // A border on the tile's east side and its south side where the area changes there (within the window)
                if (x + 1 < w.Right && map.AreaAt(x + 1, y) != area)
                    picture.Lines.Add(new LayerLine(x + 1, y, x + 1, y + 1, AreaColour, DebugLayer.Areas));
                if (y + 1 < w.Bottom && map.AreaAt(x, y + 1) != area)
                    picture.Lines.Add(new LayerLine(x, y + 1, x + 1, y + 1, AreaColour, DebugLayer.Areas));
                if (named.Add(area))
                    picture.Labels.Add(new LayerLabel(x + 0.5f, y + 0.5f, lift, area.Key, AreaColour, DebugLayer.Areas));
            }
    }

    private static void Chunks(Map map, TileWindow w, LayerPicture picture, Func<int, int, ChunkState>? chunkState)
    {
        if (!map.IsStreamed) return;
        int size = Map.ChunkTiles;
        int cx0 = w.X / size, cx1 = (w.Right - 1) / size, cy0 = w.Y / size, cy1 = (w.Bottom - 1) / size;
        for (int cy = cy0; cy <= cy1; cy++)
            for (int cx = cx0; cx <= cx1; cx++)
            {
                var state = chunkState?.Invoke(cx, cy) ?? ChunkState.None;
                var colour = ChunkColour(state);
                // The chunk's part of the window: its borders where they fall inside it, and its label at its corner there
                int x0 = Math.Max(w.X, cx * size), x1 = Math.Min(w.Right, (cx + 1) * size);
                int y0 = Math.Max(w.Y, cy * size), y1 = Math.Min(w.Bottom, (cy + 1) * size);
                if (cx * size >= w.X) picture.Lines.Add(new LayerLine(cx * size, y0, cx * size, y1, colour, DebugLayer.Chunks));
                if (cy * size >= w.Y) picture.Lines.Add(new LayerLine(x0, cy * size, x1, cy * size, colour, DebugLayer.Chunks));
                picture.Labels.Add(new LayerLabel(x0 + 0.5f, y0 + 0.5f, LiftOf(map, x0, y0),
                    $"chunk {cx},{cy} {state.ToString().ToLowerInvariant()}", colour, DebugLayer.Chunks));
            }
    }
}
