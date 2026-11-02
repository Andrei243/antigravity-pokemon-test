using System.Globalization;
using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace RoomEditor;

/// <summary>What stands on a tile of a room that the editor can pick out, place and take away.</summary>
public enum ThingKind { Person, Sign, Warp, Trigger, TileScript, HiddenItem, Prop }

/// <summary>A field of something placed, as the editor shows it and lets it be typed: its label and its value as text.</summary>
public sealed record EditorField(string Label, string Value);

/// <summary>
/// A room being edited (plan 16 · T9): a <see cref="MapFile"/>, changed by painting tiles and by placing and taking
/// away what stands on them, every change one step that <see cref="Undo"/> takes back to the file as it was. It is
/// the editor's model and knows nothing of a window: the window draws it and turns the mouse and the keys into its
/// methods, and the tests drive the same methods.
/// <para>
/// Its text is what the game writes of the map it makes (<see cref="MapFile.FromMap"/> of <see cref="MapFile.ToMap"/>),
/// so a room the editor saves loads in the game as it reads, and reads back as it is written, which the content
/// check holds every room to. A tile's behaviour follows from its look on a hand-made map, so the editor paints looks.
/// </para>
/// </summary>
public sealed class EditorDocument
{
    /// <summary>The file as it stands.</summary>
    public MapFile File { get; private set; }

    private readonly List<string> undo = new(), redo = new();
    private int strokes;
    private string? strokeStart;
    private string saved;

    public EditorDocument(MapFile file)
    {
        File = Copy(file);
        saved = Text;
    }

    /// <summary>A room's file as it is on disk.</summary>
    public static EditorDocument Open(string text) => new(GameDataFiles.Deserialize<MapFile>(text));

    /// <summary>
    /// A new room: <paramref name="floor"/> inside a ring of <paramref name="wall"/>, the ring blocked, the back wall
    /// two rows deep as a room's is, and the given look.
    /// </summary>
    public static EditorDocument New(string name, int width, int height, InteriorStyle interior = InteriorStyle.House,
        TileType floor = TileType.Floor, TileType wall = TileType.Wall)
    {
        if (width < 3 || height < 4) throw new ArgumentException("a room is at least 3 tiles wide and 4 deep");
        var file = new MapFile { Name = name, DisplayName = name, Interior = interior, Width = width, Height = height };
        for (int y = 0; y < height; y++)
        {
            var ground = new StringBuilder();
            var solid = new StringBuilder();
            for (int x = 0; x < width; x++)
            {
                bool edge = x == 0 || x == width - 1 || y <= 1 || y == height - 1;
                ground.Append(TileCodes.CodeOf(edge ? wall : floor));
                solid.Append(edge ? '#' : '.');
            }
            file.Ground.Add(ground.ToString());
            file.Solid.Add(solid.ToString());
        }
        return new EditorDocument(file);
    }

    /// <summary>The file's text, as the game would write it.</summary>
    public string Text => GameDataFiles.Serialize(File);

    /// <summary>Whether something has changed since it was opened or last saved.</summary>
    public bool Dirty => Text != saved;

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public int Width => File.Width;
    public int Height => File.Height;
    public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    // ------------------------------------------------------------------ steps

    /// <summary>
    /// Begins a stroke: every change until <see cref="EndStroke"/> is one step (a drag of the mouse painting a run of
    /// tiles is undone at once).
    /// </summary>
    public void BeginStroke()
    {
        if (strokes++ == 0) strokeStart = Text;
    }

    public void EndStroke()
    {
        if (strokes == 0 || --strokes > 0) return;
        if (strokeStart != null && strokeStart != Text)
        {
            undo.Add(strokeStart);
            redo.Clear();
        }
        strokeStart = null;
    }

    private void Change(Action<MapFile> change)
    {
        string before = Text;
        change(File);
        if (strokes > 0 || before == Text) return;
        undo.Add(before);
        redo.Clear();
    }

    public void Undo()
    {
        if (undo.Count == 0) return;
        redo.Add(Text);
        File = GameDataFiles.Deserialize<MapFile>(undo[^1]);
        undo.RemoveAt(undo.Count - 1);
    }

    public void Redo()
    {
        if (redo.Count == 0) return;
        undo.Add(Text);
        File = GameDataFiles.Deserialize<MapFile>(redo[^1]);
        redo.RemoveAt(redo.Count - 1);
    }

    private static MapFile Copy(MapFile file) => GameDataFiles.Deserialize<MapFile>(GameDataFiles.Serialize(file));

    // ------------------------------------------------------------------ tiles

    public TileType GroundAt(int x, int y) => TileCodes.Parse(File.Ground[y][x], File.Name);
    public bool SolidAt(int x, int y) => File.Solid[y][x] == '#';
    public TileType? OverheadAt(int x, int y) =>
        File.Overhead is { } rows && rows[y][x] != TileCodes.None ? TileCodes.Parse(rows[y][x], File.Name) : null;

    /// <summary>Paints a tile's look. Walls, water and trees block where they are painted, and floor opens a tile that only they had blocked.</summary>
    public void Paint(int x, int y, TileType type)
    {
        if (!Inside(x, y)) return;
        Change(f =>
        {
            bool wasBlocking = Blocks(GroundAt(x, y));
            SetChar(f.Ground, x, y, TileCodes.CodeOf(type));
            if (Blocks(type)) SetChar(f.Solid, x, y, '#');
            else if (wasBlocking && !PropOver(x, y)) SetChar(f.Solid, x, y, '.');
        });
    }

    /// <summary>Paints a rectangle of tiles as one step.</summary>
    public void Fill(int x, int y, int width, int depth, TileType type)
    {
        BeginStroke();
        for (int j = y; j < y + depth; j++)
            for (int i = x; i < x + width; i++)
                Paint(i, j, type);
        EndStroke();
    }

    /// <summary>The looks a tile is blocked by when painted: what nobody walks on in a room.</summary>
    public static bool Blocks(TileType type) => type is TileType.Wall or TileType.Water or TileType.Tree or TileType.TreeTrunk
        or TileType.CaveWall or TileType.Rock or TileType.Void or TileType.PC;

    public void SetSolid(int x, int y, bool solid)
    {
        if (!Inside(x, y)) return;
        Change(f => SetChar(f.Solid, x, y, solid ? '#' : '.'));
    }

    /// <summary>Paints the overhead layer, or clears it where <paramref name="type"/> is null; the layer is left out of the file when nothing is on it.</summary>
    public void SetOverhead(int x, int y, TileType? type)
    {
        if (!Inside(x, y)) return;
        Change(f =>
        {
            f.Overhead ??= Enumerable.Range(0, f.Height).Select(_ => new string(TileCodes.None, f.Width)).ToList();
            SetChar(f.Overhead, x, y, type is { } t ? TileCodes.CodeOf(t) : TileCodes.None);
            if (f.Overhead.All(row => row.All(c => c == TileCodes.None))) f.Overhead = null;
        });
    }

    private static void SetChar(List<string> rows, int x, int y, char c)
    {
        var row = rows[y].ToCharArray();
        row[x] = c;
        rows[y] = new string(row);
    }

    /// <summary>
    /// Makes the room another size, keeping what is at the top left; new tiles are <paramref name="fill"/>, open, and
    /// what stands outside the new size goes.
    /// </summary>
    public void Resize(int width, int height, TileType fill = TileType.Floor)
    {
        if (width < 1 || height < 1) throw new ArgumentException("a room is at least a tile");
        Change(f =>
        {
            char code = TileCodes.CodeOf(fill);
            List<string> Grow(List<string> rows, char with) => Enumerable.Range(0, height)
                .Select(y => (y < rows.Count ? rows[y] : "").PadRight(width, with)[..width]).ToList();
            f.Ground = Grow(f.Ground, code);
            f.Solid = Grow(f.Solid, Blocks(fill) ? '#' : '.');
            if (f.Overhead != null) f.Overhead = Grow(f.Overhead, TileCodes.None);
            f.Width = width;
            f.Height = height;
            bool In(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
            f.Npcs.RemoveAll(n => !In(n.X, n.Y));
            f.Signboards.RemoveAll(s => !In(s.X, s.Y));
            f.Warps.RemoveAll(w => !In(w.SourceX, w.SourceY));
            f.Props.RemoveAll(p => !In(p.X + p.Width - 1, p.Y + p.Depth - 1));
            f.Triggers?.RemoveAll(t => !In(t.X + t.Width - 1, t.Y + t.Depth - 1));
            f.TileScripts?.RemoveAll(t => !In(t.X, t.Y));
            f.HiddenItems?.RemoveAll(h => !In(h.X, h.Y));
        });
    }

    // ------------------------------------------------------------------ what stands on the tiles

    private bool PropOver(int x, int y) => File.Props.Any(p => Covers(p.X, p.Y, p.Width, p.Depth, x, y));
    private static bool Covers(int px, int py, int w, int d, int x, int y) => x >= px && y >= py && x < px + w && y < py + d;

    /// <summary>What stands on a tile, the topmost first: the order a click picks and a removal takes.</summary>
    public IEnumerable<ThingKind> ThingsAt(int x, int y)
    {
        var f = File;
        if (f.Npcs.Any(n => n.X == x && n.Y == y)) yield return ThingKind.Person;
        if (f.Signboards.Any(s => s.X == x && s.Y == y)) yield return ThingKind.Sign;
        if (f.Warps.Any(w => w.SourceX == x && w.SourceY == y)) yield return ThingKind.Warp;
        if (f.Triggers?.Any(t => Covers(t.X, t.Y, t.Width, t.Depth, x, y)) == true) yield return ThingKind.Trigger;
        if (f.TileScripts?.Any(t => t.X == x && t.Y == y) == true) yield return ThingKind.TileScript;
        if (f.HiddenItems?.Any(h => h.X == x && h.Y == y) == true) yield return ThingKind.HiddenItem;
        if (PropOver(x, y)) yield return ThingKind.Prop;
    }

    /// <summary>Furniture on a rectangle of tiles, blocking them unless <paramref name="block"/> is false (a cushion, a mat).</summary>
    public void PlaceProp(PropType type, int x, int y, int width = 1, int depth = 1, bool block = true)
    {
        if (!Inside(x, y) || !Inside(x + width - 1, y + depth - 1)) throw new ArgumentException($"the {type} reaches outside the room");
        Change(f =>
        {
            f.Props.RemoveAll(p => p.X == x && p.Y == y);
            f.Props.Add(new MapFile.PropRecord { Type = type, X = x, Y = y, Width = width, Depth = depth });
            if (block)
                for (int j = y; j < y + depth; j++)
                    for (int i = x; i < x + width; i++) SetChar(f.Solid, i, j, '#');
        });
    }

    /// <summary>A way out from a tile to a tile of another map, in place of any the tile had.</summary>
    public void PlaceWarp(int x, int y, string targetMap, int targetX, int targetY, Direction facing = Direction.Down)
    {
        if (!Inside(x, y)) throw new ArgumentException("the warp stands outside the room");
        Change(f =>
        {
            f.Warps.RemoveAll(w => w.SourceX == x && w.SourceY == y);
            f.Warps.Add(new Warp { SourceX = x, SourceY = y, TargetMap = targetMap, TargetX = targetX, TargetY = targetY, TargetFacing = facing });
        });
    }

    public void PlaceSign(int x, int y, string text, string? script = null)
    {
        if (!Inside(x, y)) throw new ArgumentException("the sign stands outside the room");
        Change(f =>
        {
            f.Signboards.RemoveAll(s => s.X == x && s.Y == y);
            f.Signboards.Add(new MapFile.SignRecord { X = x, Y = y, Text = text, Script = script });
        });
    }

    /// <summary>Someone on a tile (the record's), in place of whoever stood there.</summary>
    public void PlacePerson(MapFile.NpcRecord person)
    {
        if (!Inside(person.X, person.Y)) throw new ArgumentException($"{person.Name} stands outside the room");
        Change(f =>
        {
            f.Npcs.RemoveAll(n => n.X == person.X && n.Y == person.Y);
            f.Npcs.Add(person);
        });
    }

    public void PlaceTrigger(int x, int y, string script, int width = 1, int depth = 1, string? variable = null, int? value = null)
    {
        if (!Inside(x, y) || !Inside(x + width - 1, y + depth - 1)) throw new ArgumentException("the trigger reaches outside the room");
        Change(f =>
        {
            f.Triggers ??= new();
            f.Triggers.RemoveAll(t => t.X == x && t.Y == y);
            f.Triggers.Add(new MapFile.TriggerRecord { X = x, Y = y, Width = width, Depth = depth, Script = script, Variable = variable, Value = variable != null ? value ?? 0 : null });
        });
    }

    public void PlaceTileScript(int x, int y, string script)
    {
        if (!Inside(x, y)) throw new ArgumentException("the tile stands outside the room");
        Change(f =>
        {
            f.TileScripts ??= new();
            f.TileScripts.RemoveAll(t => t.X == x && t.Y == y);
            f.TileScripts.Add(new MapFile.TileScriptRecord { X = x, Y = y, Script = script });
        });
    }

    /// <summary>
    /// Takes away the topmost thing on a tile, or the thing of <paramref name="kind"/>; a prop taken away opens the
    /// tiles nothing else holds closed (no other prop over them, a look that doesn't block). Says what was taken.
    /// </summary>
    public ThingKind? Remove(int x, int y, ThingKind? kind = null)
    {
        ThingKind? what = kind ?? ThingsAt(x, y).Cast<ThingKind?>().FirstOrDefault();
        if (what == null || !ThingsAt(x, y).Contains(what.Value)) return null;
        Change(f =>
        {
            switch (what)
            {
                case ThingKind.Person: f.Npcs.RemoveAll(n => n.X == x && n.Y == y); break;
                case ThingKind.Sign: f.Signboards.RemoveAll(s => s.X == x && s.Y == y); break;
                case ThingKind.Warp: f.Warps.RemoveAll(w => w.SourceX == x && w.SourceY == y); break;
                case ThingKind.Trigger:
                    f.Triggers!.RemoveAll(t => Covers(t.X, t.Y, t.Width, t.Depth, x, y));
                    if (f.Triggers.Count == 0) f.Triggers = null;
                    break;
                case ThingKind.TileScript:
                    f.TileScripts!.RemoveAll(t => t.X == x && t.Y == y);
                    if (f.TileScripts.Count == 0) f.TileScripts = null;
                    break;
                case ThingKind.HiddenItem:
                    f.HiddenItems!.RemoveAll(h => h.X == x && h.Y == y);
                    if (f.HiddenItems.Count == 0) f.HiddenItems = null;
                    break;
                case ThingKind.Prop:
                    var prop = f.Props.Last(p => Covers(p.X, p.Y, p.Width, p.Depth, x, y));
                    f.Props.Remove(prop);
                    for (int j = prop.Y; j < prop.Y + prop.Depth; j++)
                        for (int i = prop.X; i < prop.X + prop.Width; i++)
                            if (!PropOver(i, j) && !Blocks(GroundAt(i, j))) SetChar(f.Solid, i, j, '.');
                    break;
            }
        });
        return what;
    }

    // ------------------------------------------------------------------ fields

    /// <summary>The room's own fields: its names, look, song and size.</summary>
    public IReadOnlyList<EditorField> RoomFields() => new EditorField[]
    {
        new("name", File.Name), new("displayName", File.DisplayName), new("interior", File.Interior.ToString()),
        new("bgmTrack", File.BgmTrack), new("arenaType", File.ArenaType?.ToString() ?? ""), new("puzzle", File.Puzzle ?? "")
    };

    public void SetRoomField(string label, string value) => Change(f =>
    {
        switch (label)
        {
            case "name": f.Name = value; break;
            case "displayName": f.DisplayName = value; break;
            case "interior": f.Interior = Enum.Parse<InteriorStyle>(value, true); break;
            case "bgmTrack": f.BgmTrack = value; break;
            case "arenaType": f.ArenaType = value.Length == 0 ? null : Enum.Parse<PokemonType>(value, true); break;
            case "puzzle": f.Puzzle = value.Length == 0 ? null : value; break;
            default: throw new ArgumentException($"a room has no field '{label}'");
        }
    });

    /// <summary>
    /// The fields of something on a tile, as text: what the editor's panel shows for it and lets be typed. A list
    /// (a person's lines) is its items joined by <c>|</c>; a field left empty is left out of the file.
    /// </summary>
    public IReadOnlyList<EditorField> Fields(int x, int y, ThingKind kind)
    {
        var f = File;
        switch (kind)
        {
            case ThingKind.Person when f.Npcs.FirstOrDefault(n => n.X == x && n.Y == y) is { } n:
                return NpcFields.Of(n);
            case ThingKind.Sign when f.Signboards.FirstOrDefault(s => s.X == x && s.Y == y) is { } s:
                return new EditorField[] { new("text", s.Text), new("script", s.Script ?? "") };
            case ThingKind.Warp when f.Warps.FirstOrDefault(w => w.SourceX == x && w.SourceY == y) is { } w:
                return new EditorField[]
                {
                    new("targetMap", w.TargetMap), new("targetX", Number(w.TargetX)), new("targetY", Number(w.TargetY)),
                    new("targetFacing", w.TargetFacing.ToString()), new("until", w.Until ?? ""), new("from", w.From ?? "")
                };
            case ThingKind.Trigger when f.Triggers?.FirstOrDefault(t => Covers(t.X, t.Y, t.Width, t.Depth, x, y)) is { } t:
                return new EditorField[]
                {
                    new("script", t.Script), new("width", Number(t.Width)), new("depth", Number(t.Depth)),
                    new("variable", t.Variable ?? ""), new("value", t.Value is { } v ? Number(v) : "")
                };
            case ThingKind.TileScript when f.TileScripts?.FirstOrDefault(t => t.X == x && t.Y == y) is { } r:
                return new EditorField[] { new("script", r.Script) };
            case ThingKind.Prop when f.Props.LastOrDefault(p => Covers(p.X, p.Y, p.Width, p.Depth, x, y)) is { } p:
                return new EditorField[] { new("type", p.Type.ToString()), new("width", Number(p.Width)), new("depth", Number(p.Depth)) };
            case ThingKind.HiddenItem when f.HiddenItems?.FirstOrDefault(h => h.X == x && h.Y == y) is { } h:
                return new EditorField[] { new("item", h.Item), new("count", h.Count is { } c ? Number(c) : ""), new("flag", h.Flag) };
        }
        return Array.Empty<EditorField>();
    }

    private static string Number(int value) => NpcFields.Number(value);
    private static string? Optional(string value) => NpcFields.Optional(value);
    private static int ParseNumber(string value) => NpcFields.ParseNumber(value);

    /// <summary>Sets one field of something on a tile, as one step; a value the field can't take throws and changes nothing.</summary>
    public void SetField(int x, int y, ThingKind kind, string label, string value)
    {
        var before = Text;
        try
        {
            Change(f => Write(f, x, y, kind, label, value));
        }
        catch (Exception e) when (e is FormatException or ArgumentException or OverflowException)
        {
            File = GameDataFiles.Deserialize<MapFile>(before);
            throw new ArgumentException($"{label}: {e.Message}", e);
        }
    }

    private static void Write(MapFile f, int x, int y, ThingKind kind, string label, string value)
    {
        string Unknown() => throw new ArgumentException($"no field '{label}'");
        switch (kind)
        {
            case ThingKind.Person:
                var n = f.Npcs.First(p => p.X == x && p.Y == y);
                if (label is "x" or "y") Unknown();
                NpcFields.Write(n, label, value);
                break;
            case ThingKind.Sign:
                var s = f.Signboards.First(p => p.X == x && p.Y == y);
                if (label == "text") s.Text = value;
                else if (label == "script") s.Script = Optional(value);
                else Unknown();
                break;
            case ThingKind.Warp:
                var w = f.Warps.First(p => p.SourceX == x && p.SourceY == y);
                switch (label)
                {
                    case "targetMap": w.TargetMap = value; break;
                    case "targetX": w.TargetX = ParseNumber(value); break;
                    case "targetY": w.TargetY = ParseNumber(value); break;
                    case "targetFacing": w.TargetFacing = Enum.Parse<Direction>(value, true); break;
                    case "until": w.Until = Optional(value); break;
                    case "from": w.From = Optional(value); break;
                    default: Unknown(); break;
                }
                break;
            case ThingKind.Trigger:
                var t = f.Triggers!.First(p => Covers(p.X, p.Y, p.Width, p.Depth, x, y));
                switch (label)
                {
                    case "script": t.Script = value; break;
                    case "width": t.Width = ParseNumber(value); break;
                    case "depth": t.Depth = ParseNumber(value); break;
                    case "variable": t.Variable = Optional(value); t.Value = t.Variable != null ? t.Value ?? 0 : null; break;
                    case "value": t.Value = value.Length == 0 ? null : ParseNumber(value); break;
                    default: Unknown(); break;
                }
                break;
            case ThingKind.TileScript:
                if (label != "script") Unknown();
                f.TileScripts!.First(p => p.X == x && p.Y == y).Script = value;
                break;
            case ThingKind.Prop:
                var prop = f.Props.Last(p => Covers(p.X, p.Y, p.Width, p.Depth, x, y));
                switch (label)
                {
                    case "type": prop.Type = Enum.Parse<PropType>(value, true); break;
                    case "width": prop.Width = ParseNumber(value); break;
                    case "depth": prop.Depth = ParseNumber(value); break;
                    default: Unknown(); break;
                }
                break;
            case ThingKind.HiddenItem:
                var h = f.HiddenItems!.First(p => p.X == x && p.Y == y);
                switch (label)
                {
                    case "item": h.Item = value; break;
                    case "count": h.Count = value.Length == 0 ? null : ParseNumber(value); break;
                    case "flag": h.Flag = value; break;
                    default: Unknown(); break;
                }
                break;
        }
    }

    // ------------------------------------------------------------------ saving

    /// <summary>
    /// The map the game makes of the file, as it makes it: what the preview draws. Throws what the game's loader
    /// would (a tile no code names, a hidden item with no flag).
    /// </summary>
    public Map ToMap() => File.ToMap();

    /// <summary>
    /// The text to save: the map the game makes of the file written back as the game writes it
    /// (<see cref="MapFile.FromMap"/>), so a room that wouldn't load is refused before it is written.
    /// </summary>
    public string SaveText()
    {
        if (File.Name.Length == 0) throw new InvalidDataException("the room has no name");
        return GameDataFiles.Serialize(MapFile.FromMap(ToMap()));
    }

    /// <summary>Writes the room into a data folder's <c>maps/</c> as <c>&lt;Name&gt;.json</c>, and returns the path.</summary>
    public string Save(string dataFolder)
    {
        string text = SaveText();
        string path = Path.Combine(dataFolder, MapDatabase.Folder, File.Name + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, text);
        File = GameDataFiles.Deserialize<MapFile>(text);
        saved = Text;
        return path;
    }
}
