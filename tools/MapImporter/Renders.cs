using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter;

/// <summary>
/// Top-down pictures of the imported data, one colour per kind of tile. They are for checking the import by eye:
/// nothing here is what the game will draw.
/// </summary>
public sealed class Renders
{
    private readonly DecompMaps decomp;
    private readonly Dictionary<int, float[]> heights = new();

    /// <summary>When set, tiles are painted by what they look like instead of by what they do.</summary>
    public Func<int, TerrainCover[]>? CoverOf { get; init; }

    public Renders(DecompMaps decomp) => this.decomp = decomp;

    // ---------------------------------------------------------------- colours

    public enum Setting { Outdoors, Cave, Indoors }

    public static Setting SettingOf(MapHeader? header) => header?.MapType switch
    {
        "CAVE" or "UNDERGROUND" => Setting.Cave,
        "INDOORS" or "POKECENTER" => Setting.Indoors,
        _ => Setting.Outdoors
    };

    private static readonly Rgb Unknown = Rgb.Of(255, 0, 255);
    public static readonly Rgb Nothing = Rgb.Of(20, 22, 34);
    private static readonly Rgb Building = Rgb.Of(196, 104, 84);
    private static readonly Rgb Ink = Rgb.Of(24, 22, 40);

    /// <summary>The groups the key is drawn from: a name, a colour and the behaviours that share it.</summary>
    public static readonly (string Name, Rgb Colour, Func<byte, bool> Covers)[] Groups =
    {
        ("TALL GRASS", Rgb.Of(72, 168, 76), b => b == 0x02),
        ("VERY TALL GRASS", Rgb.Of(36, 128, 58), b => b == 0x03),
        ("CAVE FLOOR", Rgb.Of(176, 156, 130), b => b == 0x08),
        ("MOUNTAIN FLOOR", Rgb.Of(198, 184, 156), b => b == 0x0C),
        ("OLD CHATEAU FLOOR", Rgb.Of(150, 132, 150), b => b == 0x0B),
        ("RIVER", Rgb.Of(92, 156, 224), b => b == 0x10),
        ("WATERFALL", Rgb.Of(176, 214, 246), b => b == 0x13),
        ("SEA", Rgb.Of(60, 120, 204), b => b == 0x15),
        ("PUDDLE", Rgb.Of(140, 184, 224), b => b is 0x16 or 0x1D),
        ("SHALLOW WATER", Rgb.Of(118, 178, 228), b => b == 0x17),
        ("ICE", Rgb.Of(186, 232, 240), b => b == 0x20),
        ("SAND", Rgb.Of(234, 216, 152), b => b == 0x21),
        ("SHINY FLOOR", Rgb.Of(206, 222, 238), b => b == 0x2C),
        ("RAILING", Rgb.Of(150, 128, 98), b => b is >= 0x30 and <= 0x37 or 0x49 or 0x4A),
        ("LEDGE", Rgb.Of(228, 150, 58), b => b is >= 0x38 and <= 0x3B or >= 0x5A and <= 0x5D),
        ("SLIDE", Rgb.Of(150, 222, 232), b => b is >= 0x40 and <= 0x43),
        ("ROCK CLIMB", Rgb.Of(128, 96, 72), b => b is 0x4B or 0x4C),
        ("GYM FLOOR", Rgb.Of(120, 200, 190), b => b is >= 0x56 and <= 0x59),
        ("DOOR OR WARP", Rgb.Of(224, 48, 60), b => b is >= 0x5E and <= 0x6F and not 0x61 and not 0x66 and not 0x68),
        ("BRIDGE", Rgb.Of(172, 122, 80), b => b is >= 0x70 and <= 0x7D),
        ("FURNITURE", Rgb.Of(152, 112, 172), b => b is >= 0x80 and <= 0x8F or >= 0xE0 and <= 0xEC),
        ("BERRY PATCH", Rgb.Of(232, 130, 172), b => b == 0xA0),
        ("SHALLOW SNOW", Rgb.Of(240, 244, 248), b => b is 0xA8 or 0xA9),
        ("DEEP SNOW", Rgb.Of(206, 220, 242), b => b is >= 0xA1 and <= 0xA3),
        ("MUD", Rgb.Of(140, 110, 80), b => b is 0xA4 or 0xA5),
        ("MARSH GRASS", Rgb.Of(112, 128, 76), b => b is 0xA6 or 0xA7),
        ("BIKE", Rgb.Of(240, 200, 60), b => b is >= 0xD7 and <= 0xDB),
    };

    private static Rgb OpenGround(Setting setting) => setting switch
    {
        Setting.Cave => Rgb.Of(168, 150, 128),
        Setting.Indoors => Rgb.Of(218, 206, 182),
        _ => Rgb.Of(196, 214, 150)
    };

    private static Rgb BlockedGround(Setting setting) => setting switch
    {
        Setting.Cave => Rgb.Of(70, 62, 58),
        Setting.Indoors => Rgb.Of(84, 78, 92),
        _ => Rgb.Of(58, 110, 64)
    };

    public static Rgb Colour(byte behaviour, bool solid, Setting setting)
    {
        if (behaviour == 0) return solid ? BlockedGround(setting) : OpenGround(setting);
        foreach (var (_, colour, covers) in Groups)
            if (covers(behaviour)) return solid && behaviour is not (>= 0x38 and <= 0x3B) ? colour.Darker(0.35f) : colour;
        return Unknown;
    }

    /// <summary>The colour of each kind of ground on the map-like pictures.</summary>
    public static Rgb CoverColour(TerrainCover cover, bool solid) => cover switch
    {
        TerrainCover.Grass => Rgb.Of(142, 202, 112),
        TerrainCover.Flowers => Rgb.Of(240, 168, 190),
        TerrainCover.TallGrass => Rgb.Of(66, 156, 74),
        TerrainCover.Path => Rgb.Of(228, 208, 154),
        TerrainCover.Paving => Rgb.Of(198, 198, 204),
        TerrainCover.Sand => Rgb.Of(240, 224, 164),
        TerrainCover.Rock => Rgb.Of(180, 164, 140),
        TerrainCover.CaveFloor => Rgb.Of(152, 136, 118),
        TerrainCover.Snow => Rgb.Of(244, 246, 250),
        TerrainCover.Ice => Rgb.Of(186, 232, 240),
        TerrainCover.Marsh => Rgb.Of(120, 132, 84),
        TerrainCover.Water => Rgb.Of(72, 132, 212),
        TerrainCover.Bridge => Rgb.Of(172, 122, 80),
        TerrainCover.Steps => Rgb.Of(214, 196, 170),
        TerrainCover.Tree => Rgb.Of(44, 104, 60),
        TerrainCover.Cliff => Rgb.Of(126, 106, 86),
        TerrainCover.Boulder => Rgb.Of(112, 112, 122),
        TerrainCover.Fence => Rgb.Of(150, 120, 90),
        TerrainCover.Building => Building,
        _ => solid ? Rgb.Of(150, 0, 150) : Rgb.Of(255, 60, 255)
    };

    // Behaviours worth seeing on the map-like pictures too: ledges, doors and other ways through, rock climbs
    private static bool Marked(byte b) => b is >= 0x38 and <= 0x3B or >= 0x5A and <= 0x6F or 0x4B or 0x4C;

    // ---------------------------------------------------------------- chunks

    /// <summary>One chunk, <paramref name="scale"/> pixels per tile, with the footprints of its props.</summary>
    public Picture Chunk(int landId, int scale, Setting setting, bool props = true)
    {
        var picture = new Picture(LandData.Tiles * scale, LandData.Tiles * scale);
        DrawChunk(picture, 0, 0, landId, scale, setting, props);
        return picture;
    }

    private void DrawChunk(Picture picture, int left, int top, int landId, int scale, Setting setting, bool props)
    {
        var land = decomp.Land(landId);
        var look = CoverOf?.Invoke(landId);
        for (int z = 0; z < LandData.Tiles; z++)
            for (int x = 0; x < LandData.Tiles; x++)
            {
                byte b = land.Behaviour(x, z);
                bool solid = land.Solid(x, z);
                var colour = look == null || Marked(b) ? Colour(b, solid, setting) : CoverColour(look[z * LandData.Tiles + x], solid);
                picture.Fill(left + x * scale, top + z * scale, scale, scale, colour);
            }

        if (!props) return;
        foreach (var prop in land.Props)
        {
            if (decomp.PropModel(prop.ModelId) is not { } model) continue;
            float x0 = prop.TileX + model.BoxMin.X / LandData.TileUnits, z0 = prop.TileZ + model.BoxMin.Z / LandData.TileUnits;
            float w = model.BoxSize.X / LandData.TileUnits, d = model.BoxSize.Z / LandData.TileUnits;
            if (w > LandData.Tiles - 1 && d > LandData.Tiles - 1) continue;   // a sheet of water or a floor the size of the chunk

            int px = left + (int)MathF.Round(x0 * scale), pz = top + (int)MathF.Round(z0 * scale);
            int pw = Math.Max(1, (int)MathF.Round(w * scale)), pd = Math.Max(1, (int)MathF.Round(d * scale));
            if (Cover.IsBuilding(model))
            {
                if (look == null) picture.Fill(px, pz, pw, pd, Building, 0.8f);
                picture.Frame(px, pz, pw, pd, Ink);
                if (scale >= 8 && Picture.TextWidth(model.Name) + 2 <= pw) picture.Text(px + 2, pz + 2, model.Name, Rgb.Of(255, 255, 255), 1, Ink);
            }
            else if (scale >= 4 && look == null)
                picture.Frame(px, pz, pw, pd, Building.Darker(0.25f));
        }
    }

    // ---------------------------------------------------------------- matrices

    public sealed class Options
    {
        public int Scale = 4;
        public bool Props = true;
        public bool Labels = true;
        public bool Events = true;
        public bool AreaBorders = true;
        public bool Key = true;
    }

    /// <summary>The header each cell of a matrix belongs to: its own where the matrix says, the one given otherwise.</summary>
    public MapHeader? HeaderAt(Matrix matrix, int x, int y, MapHeader? fallback) =>
        matrix.HeaderAt(x, y) is { } id && decomp.Headers.TryGetValue(id, out var header) ? header : fallback;

    public Picture World(Matrix matrix, Options options, MapHeader? owner = null)
    {
        int s = options.Scale, cell = LandData.Tiles * s;
        int keyHeight = options.Key ? 2 + (KeyEntries().Count + 3) / 4 * 9 * KeyScale(s) : 0;
        var picture = new Picture(matrix.Width * cell, matrix.Height * cell + keyHeight, Nothing);

        for (int y = 0; y < matrix.Height; y++)
            for (int x = 0; x < matrix.Width; x++)
                if (matrix.LandAt(x, y) is var land and not Matrix.NoLand)
                    DrawChunk(picture, x * cell, y * cell, land, s, SettingOf(HeaderAt(matrix, x, y, owner)), options.Props);

        if (options.AreaBorders && matrix.Headers != null)
        {
            var line = Rgb.Of(255, 255, 255);
            for (int y = 0; y < matrix.Height; y++)
                for (int x = 0; x < matrix.Width; x++)
                {
                    if (x + 1 < matrix.Width && matrix.HeaderAt(x, y) != matrix.HeaderAt(x + 1, y))
                        picture.Fill((x + 1) * cell, y * cell, 1, cell, line, 0.45f);
                    if (y + 1 < matrix.Height && matrix.HeaderAt(x, y) != matrix.HeaderAt(x, y + 1))
                        picture.Fill(x * cell, (y + 1) * cell, cell, 1, line, 0.45f);
                }
        }

        if (options.Events) DrawEvents(picture, HeadersOn(matrix, owner), s, 0, 0);
        if (options.Labels && matrix.Headers != null) DrawLabels(picture, matrix, s);
        if (options.Key) DrawKey(picture, 4, matrix.Height * cell + 2, KeyScale(s));
        return picture;
    }

    private static int KeyScale(int scale) => scale >= 4 ? 2 : 1;

    /// <summary>The areas a matrix holds: the ones it names, or the one that loads it.</summary>
    public IEnumerable<MapHeader> HeadersOn(Matrix matrix, MapHeader? owner) => matrix.Headers != null
        ? matrix.Headers.Distinct().Select(id => decomp.Headers.GetValueOrDefault(id)).OfType<MapHeader>()
        : owner != null ? new[] { owner } : Enumerable.Empty<MapHeader>();

    /// <summary>Warps, signs, people and triggers of every area on a matrix, as small marks.</summary>
    private void DrawEvents(Picture picture, IEnumerable<MapHeader> headers, int s, int left, int top)
    {
        foreach (var header in headers)
        {
            var e = decomp.Events(header.Events);
            foreach (var t in e.Triggers) picture.Frame(left + t.X * s, top + t.Z * s, Math.Max(1, t.Width) * s, Math.Max(1, t.Length) * s, Rgb.Of(60, 230, 230));
            foreach (var sign in e.Signs) Mark(picture, left + sign.X * s, top + sign.Z * s, s, Rgb.Of(250, 224, 60));
            foreach (var o in e.Objects) Mark(picture, left + o.X * s, top + o.Z * s, s, o.TrainerType == "TRAINER_TYPE_NONE" ? Rgb.Of(60, 90, 230) : Rgb.Of(150, 60, 220));
            foreach (var w in e.Warps) Mark(picture, left + w.X * s, top + w.Z * s, s, Rgb.Of(255, 255, 255), Rgb.Of(224, 30, 40));
        }
    }

    private static void Mark(Picture picture, int x, int y, int s, Rgb colour, Rgb? rim = null)
    {
        if (s < 4)
        {
            picture.Fill(x, y, s, s, rim ?? colour);
            return;
        }
        int inset = s >= 8 ? 2 : 1;
        picture.Fill(x, y, s, s, rim ?? Ink);
        picture.Fill(x + inset, y + inset, s - inset * 2, s - inset * 2, colour);
    }

    private void DrawLabels(Picture picture, Matrix matrix, int s)
    {
        int cell = LandData.Tiles * s, textScale = s >= 4 ? 2 : 1;
        var cells = new Dictionary<string, List<(int X, int Y)>>();
        for (int y = 0; y < matrix.Height; y++)
            for (int x = 0; x < matrix.Width; x++)
            {
                if (matrix.LandAt(x, y) == Matrix.NoLand || matrix.HeaderAt(x, y) is not { } id) continue;
                if (!cells.TryGetValue(id, out var list)) cells[id] = list = new();
                list.Add((x, y));
            }

        foreach (var (id, list) in cells)
        {
            if (!decomp.Headers.TryGetValue(id, out var header) || header.Index < 3) continue;
            string name = Name(header).ToUpperInvariant();
            // On the chunk of the area nearest the middle of all of them, so the name always sits on its own area
            float cx = (float)list.Average(c => c.X), cy = (float)list.Average(c => c.Y);
            var (hx, hy) = list.OrderBy(c => (c.X - cx) * (c.X - cx) + (c.Y - cy) * (c.Y - cy)).First();
            int x = hx * cell + cell / 2 - Picture.TextWidth(name, textScale) / 2;
            int y = hy * cell + cell / 2 - Picture.TextHeight * textScale / 2;
            picture.Fill(x - 2, y - 2, Picture.TextWidth(name, textScale) + 4, Picture.TextHeight * textScale + 4, Ink, 0.6f);
            picture.Text(x, y, name, Rgb.Of(255, 255, 255), textScale);
        }
    }

    /// <summary>The name a player sees for an area ("Route 201"), or its key when it has none of its own.</summary>
    public string Name(MapHeader header) =>
        decomp.LocationNames.TryGetValue(header.Label, out var name) && name != "Mystery Zone" && name.Length > 0 ? name : header.Key.Replace('_', ' ');

    private List<(string, Rgb)> KeyEntries()
    {
        var entries = new List<(string, Rgb)>();
        if (CoverOf != null)
        {
            entries.AddRange(TerrainCoverCodes.All.Where(c => c != TerrainCover.Unknown).Select(c => (Spaced(c.ToString()), CoverColour(c, false))));
            entries.Add(("NOT KNOWN YET", CoverColour(TerrainCover.Unknown, false)));
            entries.Add(("LEDGE", Colour(0x3B, true, Setting.Outdoors)));
            entries.Add(("DOOR OR WARP", Colour(0x69, false, Setting.Outdoors)));
        }
        else
        {
            entries.Add(("OPEN GROUND", OpenGround(Setting.Outdoors)));
            entries.Add(("BLOCKED", BlockedGround(Setting.Outdoors)));
            entries.Add(("BUILDING OR PROP", Building));
            entries.AddRange(Groups.Select(g => (g.Name, g.Colour)));
        }
        entries.Add(("WARP EVENT", Rgb.Of(224, 30, 40)));
        entries.Add(("PERSON", Rgb.Of(60, 90, 230)));
        entries.Add(("TRAINER", Rgb.Of(150, 60, 220)));
        entries.Add(("SIGN", Rgb.Of(250, 224, 60)));
        return entries;
    }

    private static string Spaced(string name) =>
        string.Concat(name.Select((ch, i) => i > 0 && char.IsUpper(ch) ? " " + ch : ch.ToString())).ToUpperInvariant();

    private void DrawKey(Picture picture, int x, int y, int scale)
    {
        var entries = KeyEntries();
        int column = Math.Max(120 * scale, (picture.Width - x * 2) / 4), row = 9 * scale;
        for (int i = 0; i < entries.Count; i++)
        {
            int px = x + i % 4 * column, py = y + i / 4 * row;
            picture.Fill(px, py, 7 * scale, 7 * scale, entries[i].Item2);
            picture.Text(px + 9 * scale, py + scale, entries[i].Item1, Rgb.Of(230, 232, 240), scale);
        }
    }

    // ---------------------------------------------------------------- heights

    /// <summary>The ground's height at the middle of every tile of a chunk, in tiles; the topmost where plates overlap.</summary>
    public float[] TileHeights(int landId)
    {
        if (!heights.TryGetValue(landId, out var result))
        {
            var plates = decomp.Land(landId).Heights;
            result = new float[LandData.Tiles * LandData.Tiles];
            float half = LandData.Tiles * LandData.TileUnits / 2f;
            for (int z = 0; z < LandData.Tiles; z++)
                for (int x = 0; x < LandData.Tiles; x++)
                {
                    var at = plates.HeightsAt((x + 0.5f) * LandData.TileUnits - half, (z + 0.5f) * LandData.TileUnits - half);
                    result[z * LandData.Tiles + x] = at.Count > 0 ? at[^1] / LandData.TileUnits : float.NaN;
                }
            heights[landId] = result;
        }
        return result;
    }

    /// <summary>A matrix shaded by height: low ground dark, high ground light, water and gaps kept apart.</summary>
    public Picture Heights(Matrix matrix, int scale)
    {
        int cell = LandData.Tiles * scale;
        var picture = new Picture(matrix.Width * cell, matrix.Height * cell, Nothing);
        var all = new List<float>();
        for (int i = 0; i < matrix.Land.Length; i++)
            if (matrix.Land[i] != Matrix.NoLand)
                all.AddRange(TileHeights(matrix.Land[i]).Where(h => !float.IsNaN(h)).Select(h => h + (matrix.Altitudes?[i] ?? 0) * 0.5f));
        if (all.Count == 0) return picture;
        float min = all.Min(), max = Math.Max(min + 1f, all.Max());

        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                int landId = matrix.LandAt(cx, cy);
                if (landId == Matrix.NoLand) continue;
                var land = decomp.Land(landId);
                var tiles = TileHeights(landId);
                for (int z = 0; z < LandData.Tiles; z++)
                    for (int x = 0; x < LandData.Tiles; x++)
                    {
                        float h = tiles[z * LandData.Tiles + x];
                        Rgb colour;
                        if (float.IsNaN(h)) colour = Rgb.Of(120, 30, 60);
                        else
                        {
                            float t = (h + matrix.AltitudeAt(cx, cy) * 0.5f - min) / (max - min);
                            colour = Ramp(t);
                            byte b = land.Behaviour(x, z);
                            if (b is 0x10 or 0x13 or 0x15) colour = Rgb.Of(60, 120, 204).Mix(colour, 0.35f);
                            else if (land.Solid(x, z)) colour = colour.Darker(0.18f);
                        }
                        picture.Fill((cx * LandData.Tiles + x) * scale, (cy * LandData.Tiles + z) * scale, scale, scale, colour);
                    }
            }
        return picture;
    }

    private static Rgb Ramp(float t)
    {
        (float At, Rgb Colour)[] stops =
        {
            (0f, Rgb.Of(40, 92, 70)), (0.2f, Rgb.Of(110, 160, 90)), (0.45f, Rgb.Of(214, 204, 130)),
            (0.7f, Rgb.Of(176, 124, 88)), (1f, Rgb.Of(250, 250, 250))
        };
        t = Math.Clamp(t, 0f, 1f);
        for (int i = 1; i < stops.Length; i++)
            if (t <= stops[i].At)
                return stops[i - 1].Colour.Mix(stops[i].Colour, (t - stops[i - 1].At) / (stops[i].At - stops[i - 1].At));
        return stops[^1].Colour;
    }

    // ---------------------------------------------------------------- one area, close up

    /// <summary>The chunks of one area of a matrix, with its events, large enough to count tiles.</summary>
    public Picture Area(Matrix matrix, MapHeader header, int scale, out (int X, int Y, int Width, int Height) tiles)
    {
        var cells = new List<(int X, int Y)>();
        for (int y = 0; y < matrix.Height; y++)
            for (int x = 0; x < matrix.Width; x++)
                if (matrix.LandAt(x, y) != Matrix.NoLand && (matrix.Headers == null || matrix.HeaderAt(x, y) == header.Id))
                    cells.Add((x, y));
        if (cells.Count == 0) throw new InvalidOperationException($"{header.Id} has no chunk on matrix {matrix.Id}");

        int x0 = cells.Min(c => c.X), y0 = cells.Min(c => c.Y), x1 = cells.Max(c => c.X), y1 = cells.Max(c => c.Y);
        int cell = LandData.Tiles * scale;
        var picture = new Picture((x1 - x0 + 1) * cell, (y1 - y0 + 1) * cell, Nothing);
        foreach (var (x, y) in cells)
            DrawChunk(picture, (x - x0) * cell, (y - y0) * cell, matrix.LandAt(x, y), scale, SettingOf(header), props: true);
        DrawEvents(picture, new[] { header }, scale, -x0 * cell, -y0 * cell);
        tiles = (x0 * LandData.Tiles, y0 * LandData.Tiles, (x1 - x0 + 1) * LandData.Tiles, (y1 - y0 + 1) * LandData.Tiles);
        return picture;
    }

    // ---------------------------------------------------------------- the hand-made maps, for comparison

    /// <summary>One of the game's current hand-made maps in the same colours.</summary>
    public static Picture HandMade(Map map, int scale)
    {
        var picture = new Picture(map.Width * scale, map.Height * scale, Nothing);
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var tile = map.GetGroundTile(x, y);
                bool solid = map.IsSolid(x, y);
                Rgb colour = tile switch
                {
                    TileType.Grass or TileType.FlowerGrass => solid ? BlockedGround(Setting.Outdoors) : OpenGround(Setting.Outdoors),
                    TileType.Path => Rgb.Of(222, 200, 150),
                    TileType.TallGrass => Colour(0x02, false, Setting.Outdoors),
                    TileType.Water => Colour(0x15, false, Setting.Outdoors),
                    TileType.LedgeDown => Colour(0x3B, true, Setting.Outdoors),
                    TileType.Tree or TileType.TreeTrunk => BlockedGround(Setting.Outdoors),
                    TileType.RoofRed or TileType.RoofBlue or TileType.RoofGreen or TileType.Wall => Building,
                    TileType.Door => Colour(0x69, false, Setting.Outdoors),
                    TileType.Signpost => Rgb.Of(250, 224, 60),
                    TileType.Sand => Colour(0x21, false, Setting.Outdoors),
                    TileType.Snow => Colour(0xA8, false, Setting.Outdoors),
                    TileType.CaveFloor => Colour(0x08, false, Setting.Outdoors),
                    _ => solid ? BlockedGround(Setting.Indoors) : OpenGround(Setting.Indoors)
                };
                picture.Fill(x * scale, y * scale, scale, scale, colour);
            }

        foreach (var b in MapStructures.FindBuildings(map))
            picture.Frame(b.X0 * scale, b.Y0 * scale, b.Width * scale, b.Depth * scale, Ink);
        foreach (var prop in map.Props)
            picture.Frame(prop.X * scale, prop.Y * scale, prop.Width * scale, prop.Depth * scale, Building.Darker(0.25f));
        foreach (var npc in map.NPCs) Mark(picture, npc.GridX * scale, npc.GridY * scale, scale, npc.IsTrainer ? Rgb.Of(150, 60, 220) : Rgb.Of(60, 90, 230));
        foreach (var warp in map.Warps) Mark(picture, warp.SourceX * scale, warp.SourceY * scale, scale, Rgb.Of(255, 255, 255), Rgb.Of(224, 30, 40));
        return picture;
    }

    /// <summary>Two pictures side by side under their titles.</summary>
    public static Picture Board(params (string Title, Picture Picture)[] panels)
    {
        const int gap = 16, title = 20;
        // A panel is as wide as its picture or its title, whichever is wider
        int Width((string Title, Picture Picture) p) => Math.Max(p.Picture.Width, Picture.TextWidth(p.Title, 2));
        var board = new Picture(panels.Sum(Width) + gap * (panels.Length + 1), panels.Max(p => p.Picture.Height) + title + gap * 2, Rgb.Of(34, 36, 52));
        int x = gap;
        foreach (var panel in panels)
        {
            board.Text(x, gap, panel.Title, Rgb.Of(240, 240, 246), 2);
            board.Blit(panel.Picture, x, gap + title);
            x += Width(panel) + gap;
        }
        return board;
    }
}
