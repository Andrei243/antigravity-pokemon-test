// Screenshot harness: runs the game in a hidden 1920x1080 window, drives it into known states and saves PNGs of
// the virtual screen, so graphics changes can be checked without playing.
//
//   dotnet run --project tools/ShotHarness -- <output dir> [all|field|lineup|battle|doubles|demo|arenas|flow|menus|evolution|look|title|intro|terrain|buildings|lab|life|world|times|sheets|pokemon] [before dir]
//   dotnet run --project tools/ShotHarness -- <output dir> area <key>      one shot of an area of the imported world, by its key (twinleaf_town)
//   dotnet run --project tools/ShotHarness -- <output dir> cities [key ...]   the buildings of every town of Sinnoh, from the importer's last full run
//   dotnet run --project tools/ShotHarness -- <output dir> dex [--back] [species ...]   boards of every species' 3D model, sixty to a page (--back: from behind)
//   dotnet run --project tools/ShotHarness -- <output dir> export [species ...]   species' models as .glb files, to edit and drop into overrides/models
//   dotnet run --project tools/ShotHarness -- <output dir> profile            where a frame goes: the heavy scenes timed, then taken apart pass by pass
//   dotnet run --project tools/ShotHarness -- <dir> diff <other dir>          two runs' shots compared pixel by pixel
//   dotnet run --project tools/ShotHarness -- <dir> contact [prefix]          every shot of a run on sheets of twenty
//   dotnet run --project tools/ShotHarness -- <dir> crop <shot> <x> <y> <width> <height> <scale> [other dir ...]   a rectangle of a shot, enlarged
//
// Two runs draw the same pictures (the game's chance is seeded and its clock counted in frames), so `diff` shows
// exactly what a change did. It reaches into GameEngine's private fields by reflection (currentMap, player, currentState, battle, ...), so
// renaming those fields means updating this file. The output directory becomes the working directory, which keeps
// the game from loading or writing savegame.json there.

using System.Numerics;
using System.Reflection;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

string startDir = Environment.CurrentDirectory;
string outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "shots");
string mode = args.Length > 1 ? args[1] : "all";
bool Run(string section) => mode == "all" || mode == section;

if (mode != "crop")
{
    Directory.CreateDirectory(outDir);
    Environment.CurrentDirectory = outDir;
}

// ---------------------------------------------------------------- every shot at a glance
//
//   dotnet run --project tools/ShotHarness -- <dir> contact [name prefix]
//
// Puts the shots of a folder on sheets of twenty, four to a row with each one's name under it
// (`contact_01`, `contact_02`, ...): for looking through a whole run when nothing in particular is being looked
// for. A prefix keeps only the shots whose names begin with it.
if (mode == "contact")
{
    Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
    // (Only for the default font the names are written in)
    Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
    Raylib.InitWindow(320, 200, "contact");
    string prefix = args.Length > 2 ? args[2] : "";
    var names = Directory.GetFiles(outDir, "*.png").Select(Path.GetFileNameWithoutExtension)
        .Where(n => !n.StartsWith("compare_") && !n.StartsWith("diff_") && !n.StartsWith("crop_") && !n.StartsWith("contact_") && n.StartsWith(prefix))
        .OrderBy(n => n, StringComparer.Ordinal).ToList();
    foreach (string old in Directory.GetFiles(outDir, "contact_*.png")) File.Delete(old);
    const int columns = 4, rows = 5, thumbW = 480, thumbH = 270, label = 22, gap = 6;
    for (int first = 0, sheetNumber = 1; first < names.Count; first += columns * rows, sheetNumber++)
    {
        var sheet = Raylib.GenImageColor(columns * (thumbW + gap) + gap, rows * (thumbH + label + gap) + gap, new Color(24, 26, 34, 255));
        for (int i = 0; i < columns * rows && first + i < names.Count; i++)
        {
            var img = Raylib.LoadImage(Path.Combine(outDir, names[first + i] + ".png"));
            // A crop keeps its shape: it is fitted into the cell, not stretched over it
            float fit = Math.Min((float)thumbW / img.Width, (float)thumbH / img.Height);
            int w = Math.Max(1, (int)(img.Width * fit)), h = Math.Max(1, (int)(img.Height * fit));
            Raylib.ImageResize(ref img, w, h);
            int x = gap + i % columns * (thumbW + gap), y = gap + i / columns * (thumbH + label + gap);
            Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, w, h), new Rectangle(x + (thumbW - w) / 2, y + (thumbH - h) / 2, w, h), Color.White);
            Raylib.ImageDrawText(ref sheet, names[first + i], x + 2, y + thumbH + 2, 20, Color.White);
            Raylib.UnloadImage(img);
        }
        Raylib.ExportImage(sheet, Path.Combine(outDir, $"contact_{sheetNumber:00}.png"));
        Raylib.UnloadImage(sheet);
        Console.WriteLine($"contact_{sheetNumber:00}: {names[first]} to {names[Math.Min(names.Count, first + columns * rows) - 1]}");
    }
    Raylib.CloseWindow();
    return;
}

// ---------------------------------------------------------------- a closer look at a shot
//
//   dotnet run --project tools/ShotHarness -- <dir> crop <shot> <x> <y> <width> <height> <scale> [other dir ...]
//
// Cuts a rectangle (in the shot's own pixels) out of a saved shot and enlarges it without smoothing, as
// `crop_<shot>_<x>_<y>` in the first folder: for judging an edge or a texture pixel by pixel. The same shot of
// the other folders is cut alike and put beside it, in the order the folders are given.
if (mode == "crop")
{
    Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
    if (args.Length < 8) { Console.WriteLine("usage: <dir> crop <shot> <x> <y> <width> <height> <scale> [other dir ...]"); return; }
    string[] folders = new[] { args[0] }.Concat(args.Skip(8)).Select(f => Path.GetFullPath(f, startDir)).ToArray();
    string shot = args[2];
    int cx = int.Parse(args[3]), cy = int.Parse(args[4]), cw = int.Parse(args[5]), ch = int.Parse(args[6]);
    int zoom = int.Parse(args[7]);
    var sheet = Raylib.GenImageColor(folders.Length * (cw * zoom + 10) + 10, ch * zoom + 20, new Color(24, 26, 34, 255));
    for (int i = 0; i < folders.Length; i++)
    {
        var img = Raylib.LoadImage(Path.Combine(folders[i], shot + ".png"));
        Raylib.ImageCrop(ref img, new Rectangle(cx, cy, cw, ch));
        Raylib.ImageResizeNN(ref img, cw * zoom, ch * zoom);
        int x = 10 + i * (cw * zoom + 10);
        Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, cw * zoom, ch * zoom), new Rectangle(x, 10, cw * zoom, ch * zoom), Color.White);
        Raylib.UnloadImage(img);
    }
    string cropPath = Path.Combine(folders[0], $"crop_{shot}_{cx}_{cy}.png");
    Raylib.ExportImage(sheet, cropPath);
    Console.WriteLine("wrote " + cropPath);
    return;
}

// ---------------------------------------------------------------- comparing two runs
//
//   dotnet run --project tools/ShotHarness -- <dir> diff <other dir>
//
// Compares the shots two runs saved under the same names, pixel by pixel, without starting the game. The harness
// seeds the game's chance and counts its own clock, so two runs of the same code draw the same pictures and any
// shot listed here was changed by the code. It prints the shots that differ, the most changed first, and writes a
// board for each (`diff_<name>`: the other run's shot, this one's, and their difference made eight times stronger).
if (mode == "diff")
{
    if (args.Length < 3) { Console.WriteLine("usage: <dir> diff <other dir>"); return; }
    string otherDir = Path.GetFullPath(args[2], startDir);
    static string NameOf(string path) => Path.GetFileNameWithoutExtension(path);
    bool Compared(string name) => !name.StartsWith("compare_") && !name.StartsWith("diff_") && !name.StartsWith("crop_") && !name.StartsWith("contact_");
    var mine = Directory.GetFiles(outDir, "*.png").Select(NameOf).Where(Compared).ToHashSet();
    var theirs = Directory.GetFiles(otherDir, "*.png").Select(NameOf).Where(Compared).ToHashSet();
    foreach (string old in Directory.GetFiles(outDir, "diff_*.png")) File.Delete(old);

    const int tolerance = 6;
    var changed = new List<(string Name, double Share, double Mean, int Max)>();
    int same = 0;
    foreach (string name in mine.Intersect(theirs).OrderBy(n => n, StringComparer.Ordinal))
    {
        var a = Raylib.LoadImage(Path.Combine(otherDir, name + ".png"));
        var b = Raylib.LoadImage(Path.Combine(outDir, name + ".png"));
        Raylib.ImageFormat(ref a, PixelFormat.UncompressedR8G8B8A8);
        Raylib.ImageFormat(ref b, PixelFormat.UncompressedR8G8B8A8);
        if (a.Width != b.Width || a.Height != b.Height)
        {
            changed.Add((name, 1.0, 255.0, 255));
            Raylib.UnloadImage(a); Raylib.UnloadImage(b);
            continue;
        }
        long over = 0, sum = 0;
        int max = 0, count = a.Width * a.Height;
        var delta = Raylib.GenImageColor(a.Width, a.Height, Color.Black);
        Raylib.ImageFormat(ref delta, PixelFormat.UncompressedR8G8B8A8);
        unsafe
        {
            byte* pa = (byte*)a.Data, pb = (byte*)b.Data, pd = (byte*)delta.Data;
            for (int i = 0; i < count; i++)
            {
                int worst = 0;
                for (int c = 0; c < 3; c++)
                {
                    int d = Math.Abs(pa[i * 4 + c] - pb[i * 4 + c]);
                    sum += d;
                    if (d > worst) worst = d;
                    pd[i * 4 + c] = (byte)Math.Min(255, d * 8);
                }
                if (worst > tolerance) over++;
                if (worst > max) max = worst;
            }
        }
        if (over == 0) same++;
        else
        {
            changed.Add((name, (double)over / count, sum / (count * 3.0), max));
            var board = Raylib.GenImageColor(960 * 3 + 40, 540 + 76, new Color(24, 26, 34, 255));
            int col = 0;
            foreach (var (img, label) in new[] { (a, "OTHER"), (b, "THIS"), (delta, "DIFFERENCE x8") })
            {
                var small = Raylib.ImageCopy(img);
                Raylib.ImageResize(ref small, 960, 540);
                int x = 10 + col * 970;
                Raylib.ImageDraw(ref board, small, new Rectangle(0, 0, 960, 540), new Rectangle(x, 66, 960, 540), Color.White);
                Raylib.ImageDrawText(ref board, label, x + 4, 18, 40, Color.White);
                Raylib.UnloadImage(small);
                col++;
            }
            Raylib.ExportImage(board, Path.Combine(outDir, "diff_" + name + ".png"));
            Raylib.UnloadImage(board);
        }
        Raylib.UnloadImage(delta); Raylib.UnloadImage(a); Raylib.UnloadImage(b);
    }

    foreach (var c in changed.OrderByDescending(c => c.Share))
        Console.WriteLine($"{c.Name}: {c.Share * 100:F2}% of its pixels differ (mean {c.Mean:F2}, most {c.Max})");
    Console.WriteLine($"{same + changed.Count} shots compared: {same} the same, {changed.Count} differ.");
    var onlyMine = mine.Except(theirs).OrderBy(n => n, StringComparer.Ordinal).ToList();
    var onlyTheirs = theirs.Except(mine).OrderBy(n => n, StringComparer.Ordinal).ToList();
    if (onlyMine.Count > 0) Console.WriteLine($"only here ({onlyMine.Count}): {string.Join(", ", onlyMine)}");
    if (onlyTheirs.Count > 0) Console.WriteLine($"only in the other ({onlyTheirs.Count}): {string.Join(", ", onlyTheirs)}");
    return;
}

Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
// SHOTS_WINDOW=3840x2160 makes the hidden window that size: the game then draws as it does full screen on a 4K
// display (FXAA on the high preset, the screen shown one to one), which is what the timings should be read at
var windowSize = (Environment.GetEnvironmentVariable("SHOTS_WINDOW") ?? "1920x1080").Split('x');
Raylib.InitWindow(int.Parse(windowSize[0]), int.Parse(windowSize[1]), "shots");

// Two runs draw the same pictures: the game's chance is seeded, and the clock its small motions keep time by is
// the harness's own count of frames
Dice.Seed(20261004);
long tick = 0;
FrameClock.Fixed = 0;

var startClock = System.Diagnostics.Stopwatch.StartNew();
var engine = new GameEngine();
engine.Initialize();
double startMs = startClock.Elapsed.TotalMilliseconds;

// Muted, and by day whatever the real time is (the "times" mode sets the other times of day)
engine.Settings.Muted = true;
engine.Settings.TimeOfDay = TimeOfDay.Day;
engine.ApplySettings(window: false);

// The game opens on its title screen; everything but the "title" mode wants a game in progress
// (in Sinnoh, which the shots below show)
engine.NewGameRegion = "Sinnoh";
engine.StartNewGame();
typeof(GameEngine).GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(engine, GameState.Overworld);
// The harness moves between maps without arriving anywhere, so the location sign would sit in every field shot
((LocationSign)typeof(GameEngine).GetField("locationSign", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(engine)!).Hide();

// ---------------------------------------------------------------- helpers

const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
var T = typeof(GameEngine);
object Get(string field) => T.GetField(field, Private)!.GetValue(engine)!;
void Set(string field, object value) => T.GetField(field, Private)!.SetValue(engine, value);

void Frames(int n)
{
    for (int i = 0; i < n; i++)
    {
        FrameClock.Fixed = ++tick / 60.0;
        engine.Update(1f / 60f);
        engine.Draw();
    }
}

// Advances the game by some seconds, drawing only every sixth frame: the battle's effects and camera are functions
// of time, so the frames in between only cost time under software rendering
void Skip(double seconds)
{
    int n = Math.Max(1, (int)Math.Round(seconds * 60.0));
    for (int i = 0; i < n; i++)
    {
        FrameClock.Fixed = ++tick / 60.0;
        engine.Update(1f / 60f);
        if (i % 6 == 5 || i == n - 1) engine.Draw();
    }
}

Image Capture()
{
    engine.Draw();
    var rt = (RenderTexture2D)Get("virtualScreen");
    var img = Raylib.LoadImageFromTexture(rt.Texture);
    Raylib.ImageFlipVertical(ref img);
    return img;
}

void Save(Image img, string name)
{
    Raylib.ExportImage(img, Path.Combine(outDir, name + ".png"));
    Raylib.UnloadImage(img);
    Console.WriteLine("wrote " + name);
}

// The game renders at 3840x2160. Shots are saved at half size (quick to write and review) unless SHOTS_4K=1;
// coordinates everywhere in this file are layout units (1920x1080).
bool native4K = Environment.GetEnvironmentVariable("SHOTS_4K") == "1";
const int S = GameEngine.RenderScale;

// SHOTS_FILTER=text saves only the shots whose name contains it, for quick looks at one thing. It also skips the
// timings unless SHOTS_TIMING=1 (a filter that matches nothing then gives a run of timings alone)
string filter = Environment.GetEnvironmentVariable("SHOTS_FILTER") ?? "";
bool Wanted(string name) => filter.Length == 0 || name.Contains(filter);

void Shot(string name)
{
    if (!Wanted(name)) return;
    var img = Capture();
    if (!native4K) Raylib.ImageResize(ref img, GameEngine.VirtualWidth, GameEngine.VirtualHeight);
    Save(img, name);
}

// A region of the screen at native resolution, enlarged with nearest-neighbour filtering for close inspection
// (scale is relative to layout units, so 2 means the native 4K pixels)
void ShotCrop(string name, int x, int y, int w, int h, int scale)
{
    if (!Wanted(name)) return;
    var img = Capture();
    Raylib.ImageCrop(ref img, new Rectangle(x * S, y * S, w * S, h * S));
    if (scale > S) Raylib.ImageResizeNN(ref img, w * scale, h * scale);
    Save(img, name);
}

// The five hand-made maps of the first towns and routes gave way to the imported world (plan 01 · M2). The shots
// that stood on them now stand at the same kind of place on the map of Sinnoh, or by the lake: each old spot has
// its new one here, and an old map's name alone stands for a typical spot of the area that replaced it.
var movedSpots = new Dictionary<(string, int, int), (string Map, int X, int Y)>
{
    [("TwinleafTown", 11, 8)] = ("Sinnoh", 112, 880), [("TwinleafTown", 6, 9)] = ("Sinnoh", 116, 886), [("TwinleafTown", 11, 1)] = ("Sinnoh", 112, 866),
    [("TwinleafTown", 9, 14)] = ("Sinnoh", 111, 890), [("TwinleafTown", 17, 9)] = ("Sinnoh", 105, 876), [("TwinleafTown", 12, 7)] = ("Sinnoh", 116, 876),
    [("TwinleafTown", 3, 3)] = ("Sinnoh", 103, 868), [("TwinleafTown", 11, 9)] = ("Sinnoh", 112, 881), [("TwinleafTown", 12, 10)] = ("Sinnoh", 112, 882),
    [("Route201", 14, 10)] = ("Sinnoh", 112, 857), [("Route201", 24, 8)] = ("Sinnoh", 115, 854), [("Route201", 22, 11)] = ("Sinnoh", 110, 850),
    [("Route201", 24, 9)] = ("Sinnoh", 166, 815), [("Route201", 24, 14)] = ("Sinnoh", 120, 854), [("Route201", 22, 15)] = ("Sinnoh", 120, 854),
    [("Route201", 22, 10)] = ("Sinnoh", 110, 850), [("Route201", 32, 3)] = ("Sinnoh", 124, 850), [("Route201", 12, 10)] = ("Sinnoh", 130, 854),
    [("SandgemTown", 14, 8)] = ("Sinnoh", 178, 845), [("SandgemTown", 8, 19)] = ("Sinnoh", 168, 844), [("SandgemTown", 6, 8)] = ("Sinnoh", 177, 843),
    [("SandgemTown", 22, 8)] = ("Sinnoh", 187, 843), [("SandgemTown", 7, 19)] = ("Sinnoh", 168, 843), [("SandgemTown", 12, 10)] = ("Sinnoh", 178, 846),
    [("LakeVerity", 14, 11)] = ("LakeVerity", 44, 46), [("LakeVerity", 14, 10)] = ("LakeVerity", 44, 46), [("LakeVerity", 24, 5)] = ("LakeVerity", 51, 40),
    [("Route202", 14, 10)] = ("Sinnoh", 174, 815), [("Route202", 15, 2)] = ("Sinnoh", 174, 802)
};
var movedMaps = new Dictionary<string, (string Map, int X, int Y)>
{
    ["TwinleafTown"] = ("Sinnoh", 112, 880), ["Route201"] = ("Sinnoh", 115, 854), ["LakeVerity"] = ("LakeVerity", 44, 46),
    ["SandgemTown"] = ("Sinnoh", 178, 845), ["Route202"] = ("Sinnoh", 174, 815)
};

(string Map, int X, int Y) Place(string map, int x, int y) =>
    movedSpots.TryGetValue((map, x, y), out var spot) ? spot : movedMaps.TryGetValue(map, out var typical) ? typical : (map, x, y);

// Puts the player on a tile of a map as it is today
void At(string map, int x, int y, Direction facing)
{
    Set("currentMap", MapDatabase.Get(map));
    ((Player)Get("player")).SetPosition(x, y, facing);
    Set("currentState", GameState.Overworld);
    Frames(2);
}

// Like At, for the shots written when the first towns were hand-made maps of their own
void GoTo(string map, int x, int y, Direction facing)
{
    (map, x, y) = Place(map, x, y);
    At(map, x, y, facing);
}

// The open tile of an area of the imported world nearest the middle of its open ground
(Map Map, int X, int Y) AreaSpot(string key)
{
    foreach (var name in MapDatabase.MapNames)
    {
        var map = MapDatabase.Get(name);
        if (map.AreaBounds(key) is not { } b) continue;
        var area = map.FindArea(key);
        var open = new List<(int X, int Y)>();
        for (int y = b.Y; y < b.Y + b.Height; y++)
            for (int x = b.X; x < b.X + b.Width; x++)
                if (map.AreaAt(x, y) == area && map.IsWalkable(x, y)) open.Add((x, y));
        if (open.Count == 0) throw new InvalidOperationException($"The area {key} has no open ground");
        double cx = open.Average(t => t.X), cy = open.Average(t => t.Y);
        var (sx, sy) = open.OrderBy(t => (t.X - cx) * (t.X - cx) + (t.Y - cy) * (t.Y - cy)).First();
        return (map, sx, sy);
    }
    throw new ArgumentException($"No map has an area called {key}");
}

void SetPlayerAnim(float walk, float blend, bool running = false)
{
    var p = (Player)Get("player");
    var PT = typeof(Player);
    PT.GetField("<WalkCycle>k__BackingField", Private)!.SetValue(p, walk);
    PT.GetField("<WalkBlend>k__BackingField", Private)!.SetValue(p, blend);
    PT.GetField("<IsRunning>k__BackingField", Private)!.SetValue(p, running);
}

void Timing(string label, int frames = 300)
{
    if (filter.Length > 0 && Environment.GetEnvironmentVariable("SHOTS_TIMING") != "1") return;
    var sw = System.Diagnostics.Stopwatch.StartNew();
    Frames(frames);
    Console.WriteLine($"{label}: {sw.Elapsed.TotalMilliseconds / frames:F2} ms/frame");
}

// A scene's frame time, then the same frames with the profiler on: each pass's share of the frame, and the meshes
// and triangles it draws. `frozen` draws one moment over and over (a move's effect at its height, a camera's
// close-up), which the game's own clock would move on from.
void Profile(string label, int frames = 240, bool frozen = false)
{
    // SHOTS_ONLY=battle,route measures only the scenes whose name has one of those words in it
    var only = (Environment.GetEnvironmentVariable("SHOTS_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
    if (only.Length > 0 && !only.Any(word => label.Contains(word, StringComparison.OrdinalIgnoreCase))) return;
    void One() { if (!frozen) { FrameClock.Fixed = ++tick / 60.0; engine.Update(1f / 60f); } engine.Draw(); }
    for (int i = 0; i < 20; i++) One();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    for (int i = 0; i < frames; i++) One();
    double plain = sw.Elapsed.TotalMilliseconds / frames;
    FrameProfiler.Enabled = true;
    for (int i = 0; i < 5; i++) One();
    FrameProfiler.Reset();
    for (int i = 0; i < frames / 2; i++) One();
    FrameProfiler.Enabled = false;
    Console.WriteLine($"{label}: {plain:F2} ms/frame");
    Console.WriteLine($"    {FrameProfiler.Report()}");
}

// Test map with every character type in a row, for close-ups of the 3D models
Map BuildLineup()
{
    var m = new Map(18, 12) { Name = "Lineup", DisplayName = "Lineup" };
    for (int x = 0; x < 18; x++) { m.SetGroundTile(x, 0, TileType.Tree, true); m.SetGroundTile(x, 11, TileType.Tree, true); }
    for (int y = 0; y < 12; y++) { m.SetGroundTile(0, y, TileType.Tree, true); m.SetGroundTile(17, y, TileType.Tree, true); }
    for (int x = 1; x < 17; x++) m.SetGroundTile(x, 6, TileType.Path);
    string[] types = { "Rival", "Rowan", "Nurse", "Mom", "Lady", "Clerk", "Youngster", "Lass", "StarterBriefcase", "Rift" };
    for (int i = 0; i < types.Length; i++)
        m.NPCs.Add(new NPC { Name = types[i] + "_d", NpcType = types[i], GridX = 3 + i, GridY = 4, Facing = Direction.Down });
    Direction[] dirs = { Direction.Left, Direction.Up, Direction.Right, Direction.Down };
    for (int i = 0; i < 8; i++)
        m.NPCs.Add(new NPC { Name = types[i] + "_s", NpcType = types[i], GridX = 3 + i, GridY = 7, Facing = dirs[i % 4] });
    return m;
}

// The same characters inside the field's focus band, around the player at (10, 7)
Map BuildFocusLineup()
{
    var m = new Map(22, 14) { Name = "LineupFocus", DisplayName = "Lineup" };
    for (int x = 0; x < 22; x++) { m.SetGroundTile(x, 0, TileType.Tree, true); m.SetGroundTile(x, 13, TileType.Tree, true); }
    for (int y = 0; y < 14; y++) { m.SetGroundTile(0, y, TileType.Tree, true); m.SetGroundTile(21, y, TileType.Tree, true); }
    for (int x = 1; x < 21; x++) m.SetGroundTile(x, 9, TileType.Path);
    string[] front = { "Rival", "Rowan", "Nurse", "Mom", "Lady", "Clerk", "Youngster", "Lass", "Clown", "Looker", "Gentleman", "StarterBriefcase", "Rift" };
    for (int i = 0, x = 3; i < front.Length; i++, x++)
    {
        if (x == 10) x++;   // the player's place
        m.NPCs.Add(new NPC { Name = front[i] + "_f", NpcType = front[i], GridX = x, GridY = 7, Facing = Direction.Down });
    }
    string[] turned = { "Player", "Rival", "Rowan", "Nurse", "Mom", "Lady", "Clerk", "Youngster", "Lass", "Clown", "Looker", "Gentleman" };
    Direction[] dirs = { Direction.Left, Direction.Up, Direction.Right };
    for (int i = 0; i < turned.Length; i++)
        m.NPCs.Add(new NPC { Name = turned[i] + "_t", NpcType = turned[i], GridX = 4 + i, GridY = 9, Facing = dirs[i % 3] });
    return m;
}

var party = (Party)Get("playerParty");
party.Add(new Pokemon(PokemonDatabase.Get("Chimchar")!, 7));
party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 6));
var inventory = (Inventory)Get("playerInventory");
var pokedex = (Pokedex)Get("playerPokedex");

void Confirm(BattleEngine b)
{
    Console.WriteLine("  msg: " + b.CurrentMessage);
    b.ConfirmMessage();
    Frames(1);
}

// A Pokémon of a species, or of a species' form by the form's name (Meowth-Galar, Charizard-Mega-X)
Pokemon Meet(string name, int level)
{
    if (PokemonDatabase.Get(name) is { } species) return new Pokemon(species, level);
    var p = new Pokemon(PokemonDatabase.SpeciesOfForm(name) ?? throw new ArgumentException($"No species or form is called {name}"), level);
    p.ChangeForm(name);
    return p;
}

BattleEngine StartBattle(string foe, int level, Trainer trainer = null, string map = "Route201", Random chance = null)
{
    // On the map of Sinnoh the stage depends on where the battle starts: the area's trees, water within sight
    var (name, x, y) = Place(map, -1, -1);
    Set("currentMap", MapDatabase.Get(name));
    if (x >= 0) ((Player)Get("player")).SetPosition(x, y, Direction.Up);
    if (trainer != null && trainer.Party.Count == 0) trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
    var enemy = trainer?.Party.Members[0] ?? Meet(foe, level);
    // (A wild battle can be given its own chance, with the rolls a shot depends on fixed)
    var b = chance == null
        ? new BattleEngine(party, enemy, inventory, pokedex, trainer)
        : new BattleEngine(new BattleSetup { PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy }, Random = chance });
    ((BattleRenderer)Get("battleRenderer")).SetArena((Map)Get("currentMap"), x, y);
    Set("battle", b);
    Set("currentState", GameState.Battle);
    return b;
}

// Waits out the camera sweep and sends the player's Pokémon out, ending on the main battle menu once the camera
// has eased back to the overview
void ToMainMenu(BattleEngine b)
{
    Skip(130 / 60.0);
    Confirm(b);
    Skip(50 / 60.0);
    Confirm(b);
    Skip(0.8);
}

// ---------------------------------------------------------------- overworld

if (Run("field"))
{
    GoTo("TwinleafTown", 11, 8, Direction.Down); Shot("01_twinleaf");
    GoTo("TwinleafTown", 6, 9, Direction.Up); Shot("02_twinleaf_house");
    GoTo("TwinleafTown", 11, 1, Direction.Up); Shot("02b_twinleaf_north");
    GoTo("TwinleafTown", 9, 14, Direction.Left); Shot("02c_twinleaf_pond");
    GoTo("PlayerHouse", 4, 6, Direction.Up); Shot("03_player_house");
    GoTo("RivalHouse", 5, 7, Direction.Up); Shot("03b_rival_house");
    GoTo("TwinleafTown", 17, 9, Direction.Up); Shot("03c_twinleaf_rival_door");
    GoTo("Route201", 14, 10, Direction.Left); Shot("04_route201");
    GoTo("Route201", 24, 8, Direction.Up); Shot("04b_route201_grass");
    GoTo("Route201", 22, 11, Direction.Down); Shot("04c_route201_ledge");
    GoTo("SandgemTown", 14, 8, Direction.Up); Shot("05_sandgem");
    GoTo("SandgemTown", 8, 19, Direction.Up); Shot("05b_sandgem_lab");
    GoTo("PokemonCenter", 5, 6, Direction.Up); Shot("06_pokecenter");
    GoTo("PokeMart", 4, 5, Direction.Up); Shot("06b_mart");
    GoTo("RowanLab", 5, 7, Direction.Up); Shot("06c_lab");
    GoTo("LakeVerity", 14, 11, Direction.Up); Shot("07_lake");
    GoTo("Route202", 14, 10, Direction.Up); Shot("07b_route202");
    GoTo("Route202", 15, 2, Direction.Up); Shot("07c_route202_north");
    GoTo("JubilifeCity", 20, 30, Direction.Up); Shot("08_jubilife_south");
    GoTo("JubilifeCity", 19, 18, Direction.Up); Shot("08b_jubilife_crossroads");
    GoTo("JubilifeCity", 7, 9, Direction.Up); Shot("08c_jubilife_school");
    GoTo("JubilifeCity", 30, 10, Direction.Up); Shot("08d_jubilife_poketch_tv");
    GoTo("JubilifeCity", 28, 29, Direction.Up); Shot("08e_jubilife_center_mart");
    GoTo("JubilifeCity", 9, 29, Direction.Up); Shot("08f_jubilife_terminal");
    GoTo("TrainersSchool", 6, 9, Direction.Up); Shot("09_trainers_school");
    GoTo("PoketchCompany", 5, 7, Direction.Up); Shot("09b_poketch_company");
    GoTo("JubilifePokemonCenter", 5, 6, Direction.Up); Shot("09c_jubilife_center");
    GoTo("PalletTown", 9, 9, Direction.Down); Shot("10_pallet");
    GoTo("PalletTown", 9, 17, Direction.Down); Shot("10b_pallet_pier");
    GoTo("PalletPlayerHouse", 4, 6, Direction.Up); Shot("10c_pallet_house");

    // A trainer spotting the player
    GoTo("Route201", 24, 9, Direction.Up);
    var tristan = MapDatabase.Get("Sinnoh").NPCs.First(n => n.IsTrainer);
    tristan.HasSpottedPlayer = true;
    tristan.ExclamationTimer = 5f;
    Frames(1); Shot("04d_route201_spotted");
    tristan.HasSpottedPlayer = false;
    tristan.ExclamationTimer = 0f;

    // Dialogue box
    GoTo("TwinleafTown", 12, 7, Direction.Up);
    ((DialogueManager)Get("dialogue")).ShowDialogue("Barry", new List<string> { "Barry: Hey, Lucas! You're finally ready! Professor Rowan is waiting at Lake Verity!" });
    Set("currentState", GameState.Dialogue);
    Frames(180); Shot("08_dialogue");
    Set("dialogue", new DialogueManager());

    // With the folder of an earlier run as the third argument, before/after boards of the outdoor shots too
    if (args.Length > 2)
        Boards(args[2], new[] { "01_twinleaf", "02_twinleaf_house", "02b_twinleaf_north", "02c_twinleaf_pond", "04_route201", "04b_route201_grass", "04c_route201_ledge", "05_sandgem", "05b_sandgem_lab", "07_lake", "07b_route202", "07c_route202_north" });

    // Frame timing (no vsync in the hidden window)
    foreach (var m in new[] { "TwinleafTown", "Route201", "SandgemTown", "PokemonCenter" })
    {
        GoTo(m, m == "PokemonCenter" ? 5 : 12, m == "PokemonCenter" ? 6 : 10, Direction.Down);
        Timing(m);
    }
}

// ---------------------------------------------------------------- 3D characters up close

if (Run("lineup"))
{
    Set("currentMap", BuildLineup());
    ((Player)Get("player")).SetPosition(9, 9, Direction.Down);
    Set("currentState", GameState.Overworld);
    Frames(2);
    Shot("30_lineup");
    ShotCrop("31_lineup_front", 380, 60, 940, 205, 2);
    ShotCrop("32_lineup_sides", 380, 280, 740, 195, 2);
    foreach (var (walk, label) in new[] { (0.25f, "a"), (0.75f, "b") })
    {
        SetPlayerAnim(walk, 1f);
        ShotCrop("33_player_walk_" + label, 870, 420, 180, 200, 4);
    }
    SetPlayerAnim(0.25f, 1f, running: true);
    ShotCrop("34_player_run", 870, 420, 180, 200, 4);
    foreach (var d in new[] { Direction.Left, Direction.Up, Direction.Right })
    {
        ((Player)Get("player")).SetPosition(9, 9, d);
        Frames(1);
        ShotCrop("35_player_" + d, 870, 420, 180, 200, 4);
    }
    ((Player)Get("player")).SetPosition(9, 9, Direction.Down);
    SetPlayerAnim(0f, 0f);

    // Every character in focus: a row facing the camera with the player in the middle, and a row turned away
    Set("currentMap", BuildFocusLineup());
    ((Player)Get("player")).SetPosition(10, 7, Direction.Down);
    Frames(2);
    Shot("36_lineup_focus");
    ShotCrop("36b_lineup_focus_front", 300, 440, 1320, 175, 2);
    ShotCrop("36c_lineup_focus_turned", 300, 615, 1320, 185, 2);

    // Look-dev: the 3D models as battles show them (front, three-quarter, side, back), and the field's sprites
    var asm = typeof(GameEngine).Assembly;
    var context = Get("renderContext");
    var poseType = asm.GetType("PokemonPlatinumEngine.Graphics.CharacterPose")!;
    var exprType = asm.GetType("PokemonPlatinumEngine.Graphics.Expression")!;
    var animType = asm.GetType("PokemonPlatinumEngine.Graphics.SpriteAnim")!;
    var turntable = asm.GetType("PokemonPlatinumEngine.Graphics.CharacterStudio")!.GetMethod("Turntable", BindingFlags.Static | BindingFlags.Public)!;
    var sheet = asm.GetType("PokemonPlatinumEngine.Graphics.CharacterSprites")!.GetMethod("Sheet", BindingFlags.Static | BindingFlags.NonPublic)!;
    object Pose(string expression = "Neutral", bool blink = false)
    {
        object pose = Activator.CreateInstance(poseType)!;
        poseType.GetField("Time")!.SetValue(pose, 0.4f);
        poseType.GetField("Blink")!.SetValue(pose, blink);
        poseType.GetField("Expression")!.SetValue(pose, Enum.Parse(exprType, expression));
        return pose;
    }
    string[] everyone = { "Player", "Dawn", "Rival", "Rowan", "Nurse", "Mom", "Lady", "Clerk", "Youngster", "Lass", "Clown", "Looker", "Gentleman", "StarterBriefcase", "Rift" };
    foreach (var type in everyone)
    {
        if (!Wanted("37_turntable_" + type.ToLowerInvariant())) continue;
        var img = (Image)turntable.Invoke(null, new object[] { context, type, new[] { 0f, 0.65f, MathF.PI / 2f, MathF.PI }, 300, 420, Pose(), 1.7f, 0.7f })!;
        Save(img, "37_turntable_" + type.ToLowerInvariant());
    }

    // Sprite sheets: each facing a row (down, right, up, left), each frame of the strip a column, at 4x
    Image Strip(string type, string anim, int frames, bool blink = false, string expression = "Neutral")
    {
        var rows = Raylib.GenImageColor(40 * frames * 4, 58 * 4 * 4, new Color(206, 218, 232, 255));
        for (int facing = 0; facing < 4; facing++)
        {
            var row = (Image)sheet.Invoke(null, new object[] { context, type, facing, Enum.Parse(animType, anim), frames, 4, blink, Enum.Parse(exprType, expression) })!;
            Raylib.ImageDraw(ref rows, row, new Rectangle(0, 0, row.Width, row.Height), new Rectangle(0, facing * 58 * 4, row.Width, row.Height), Color.White);
            Raylib.UnloadImage(row);
        }
        return rows;
    }
    foreach (var (type, anim, frames) in new[]
    {
        ("Player", "Walk", 8), ("Player", "Run", 8), ("Player", "Idle", 2), ("Player", "Hop", 3), ("Player", "Wave", 6), ("Player", "Surprised", 6),
        ("Player", "Cheer", 6), ("Player", "Nod", 6), ("Rival", "Walk", 8), ("Lass", "Walk", 8), ("Rowan", "Walk", 8), ("Nurse", "Idle", 2)
    })
    {
        string name = $"38_sheet_{type.ToLowerInvariant()}_{anim.ToLowerInvariant()}";
        if (Wanted(name)) Save(Strip(type, anim, frames), name);
    }

    // Faces: every expression in 3D (front view) and as pixel faces on the standing sprite, eyes open and shut
    foreach (var type in new[] { "Player", "Lass", "Rowan" })
    {
        string name = "39_faces_" + type.ToLowerInvariant();
        if (!Wanted(name)) continue;
        var faces = Raylib.GenImageColor(6 * 240, 340 + 58 * 4, new Color(206, 218, 232, 255));
        int col = 0;
        foreach (var (expression, blink) in new[] { ("Neutral", false), ("Neutral", true), ("Happy", false), ("Surprised", false), ("Sad", false), ("Angry", false) })
        {
            // A close-up of the head (the adults' heads sit a little higher)
            float headY = type == "Player" || type == "Lass" ? 0.86f : 0.97f;
            var view = (Image)turntable.Invoke(null, new object[] { context, type, new[] { 0f }, 240, 340, Pose(expression, blink), 0.75f, headY })!;
            Raylib.ImageDraw(ref faces, view, new Rectangle(0, 0, 240, 340), new Rectangle(col * 240, 0, 240, 340), Color.White);
            Raylib.UnloadImage(view);
            var sprite = (Image)sheet.Invoke(null, new object[] { context, type, 0, Enum.Parse(animType, "Idle"), 1, 4, blink, Enum.Parse(exprType, expression) })!;
            Raylib.ImageDraw(ref faces, sprite, new Rectangle(0, 0, sprite.Width, sprite.Height), new Rectangle(col * 240 + 40, 340, sprite.Width, sprite.Height), Color.White);
            Raylib.UnloadImage(sprite);
            col++;
        }
        Save(faces, name);
    }
}

// ---------------------------------------------------------------- battles

if (Run("battle"))
{
    // Wild battle: camera sweep, send-out, attack, hit, faint
    var b = StartBattle("Shinx", 5);
    Frames(1); Shot("40_wild_intro_start");
    Skip(50 / 60.0); Shot("41_wild_intro_mid");
    Skip(80 / 60.0); Shot("42_wild_appeared");
    Confirm(b); Skip(10 / 60.0); Shot("43_go_sendout");
    Skip(60 / 60.0); Confirm(b); Skip(2 / 60.0); Shot("44_main_menu");
    b.HUD.MenuState = BattleMenuState.Moves; Frames(1); Shot("44b_moves");
    b.HUD.MenuState = BattleMenuState.Main;

    // Whoever is faster attacks first: lunge, then the hit lands and the HP bar drains
    b.SelectMove(0);
    Console.WriteLine("  msg: " + b.CurrentMessage);
    Skip(8 / 60.0); Shot("45_attack_lunge");
    Skip(14 / 60.0); Shot("46_attack_hit");
    Skip(30 / 60.0); Shot("47_hp_drain");
    for (int guard = 0; guard < 12 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++)
    {
        Confirm(b);
        Skip(30 / 60.0);
    }
    Shot("49_after_turn");

    // Knock the foe out
    b.EnemyPokemon.CurrentHP = 1;
    b.SelectMove(0);
    Skip(40 / 60.0);
    for (int guard = 0; guard < 6 && !b.CurrentMessage.Contains("fainted"); guard++) { Confirm(b); Skip(20 / 60.0); }
    Skip(18 / 60.0); Shot("50_faint_mid");
    Skip(40 / 60.0); Shot("50b_faint_done");

    // Capture with a Master Ball (always caught) and with a Poké Ball that shakes twice and breaks open: the
    // harness's dice are seeded, so the shake checks are fixed here or the ball would do the same thing every run
    // by accident
    foreach (var (ball, tag) in new[] { ("Master Ball", "caught"), ("Poké Ball", "free") })
    {
        b = StartBattle("Starly", 4, chance: tag == "free"
            ? new PokemonPlatinumEngine.Battle.Sim.BattleRandom(4).Force(PokemonPlatinumEngine.Battle.Sim.RollKind.CatchShake, 0, 0, 65535)
            : null);
        ToMainMenu(b);
        b.UseItem(ItemDatabase.Get(ball)!);
        Confirm(b);
        // The player runs in and throws (0.6 s), the ball flies (0.8 s), opens (0.55 s), drops (0.3 s), then wobbles
        Skip(0.45); Shot($"50c_{tag}_ball_throw");
        Skip(0.55); Shot($"51_{tag}_ball_flight");
        Skip(0.6); Shot($"52_{tag}_ball_open");
        Skip(0.55); Shot($"53_{tag}_ball_ground");
        Skip(0.3); Shot($"54_{tag}_ball_wobble");
        Skip(2.6); Shot($"55_{tag}_result");
        Console.WriteLine("  msg: " + b.CurrentMessage);
    }

    // Trainer battle: both trainers on their platforms, then they step aside as the Pokémon come out
    var trainer = MapDatabase.Get("Sinnoh").NPCs.First(n => n.IsTrainer).TrainerData!;
    b = StartBattle("", 0, trainer);
    Frames(1); Shot("60_trainer_intro_start");
    Skip(130 / 60.0); Shot("61_trainer_wants");
    Confirm(b); Skip(14 / 60.0); Shot("62_trainer_sendout");
    Skip(60 / 60.0); Shot("63_trainer_gone");
    Confirm(b); Skip(14 / 60.0); Shot("64_player_sendout");
    Skip(60 / 60.0); Confirm(b); Skip(2 / 60.0); Shot("65_trainer_main");

    // Forest styles and a range of sizes
    int k = 0;
    foreach (var (foe, map) in new[] { ("Bidoof", "TwinleafTown"), ("Gible", "Route201"), ("Riolu", "LakeVerity"), ("Giratina", "Route201"), ("Starly", "Route202") })
    {
        b = StartBattle(foe, 5, null, map);
        ToMainMenu(b);
        Shot($"7{k++}_field_{foe}");
    }

    // Another lead, for its back sprite
    party.Swap(0, 1);
    b = StartBattle("Turtwig", 5);
    ToMainMenu(b);
    Shot("76_field_second_lead");
    party.Swap(0, 1);

    StartBattle("Luxray", 30);
    Timing("battle");
}

// ---------------------------------------------------------------- double battles

if (Run("doubles"))
{
    BattleEngine StartDouble(Trainer[] trainers, Pokemon[] wild)
    {
        Set("currentMap", MapDatabase.Get("Sinnoh"));
        var d = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
            Trainers = trainers.ToList(), WildPokemon = wild.ToList(), Random = new Random(5)
        });
        ((BattleRenderer)Get("battleRenderer")).SetArena((Map)Get("currentMap"));
        Set("battle", d);
        Set("currentState", GameState.Battle);
        return d;
    }

    // Twins: one trainer sending two at once
    party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 8));
    var twins = new Trainer { Name = "Liv & Liz", TrainerClass = "Lass", DoubleBattle = true };
    twins.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 6));
    twins.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 6));
    twins.Party.Add(new Pokemon(PokemonDatabase.Get("Gible")!, 6));
    var d = StartDouble(new[] { twins }, Array.Empty<Pokemon>());
    Skip(130 / 60.0); Shot("90_double_intro");
    Confirm(d); Skip(70 / 60.0); Shot("91_double_foes_out");
    Confirm(d); Skip(70 / 60.0); Shot("92_double_mine_out");
    for (int guard = 0; guard < 6 && d.HUD.MenuState == BattleMenuState.Message; guard++) { Confirm(d); Skip(20 / 60.0); }
    Skip(2 / 60.0); Shot("93_double_main_first");
    d.SelectMainMenuOption(0); Skip(2 / 60.0); Shot("94_double_moves");
    int single = d.PlayerPokemon.Moves.FindIndex(m => m.Target == MoveTarget.Selected && m.Category != MoveCategory.Status);
    d.HUD.MoveMenuIndex = Math.Max(0, single);
    d.SelectMove(Math.Max(0, single)); Skip(2 / 60.0); Shot("95_double_target");
    d.HUD.TargetMenuIndex = 1; Skip(2 / 60.0); Shot("95b_double_target_right");
    d.SelectTarget(1); Skip(2 / 60.0); Shot("96_double_main_second");
    d.SelectMove(0);
    if (d.HUD.MenuState == BattleMenuState.SelectTarget) d.SelectTarget(0);
    Skip(8 / 60.0); Shot("97_double_attack");
    for (int guard = 0; guard < 20 && d.HUD.MenuState == BattleMenuState.Message && !d.IsBattleOver; guard++) { Confirm(d); Skip(25 / 60.0); }
    Skip(0.8); Shot("98_double_after_turn");

    // A fainted Pokémon of the player's has to be replaced
    d.PlayerSlots[1].Pokemon!.CurrentHP = 1;
    d.PlayerSlots[1].Pokemon!.Status = StatusCondition.Burn;
    d.SelectMove(0); if (d.HUD.MenuState == BattleMenuState.SelectTarget) d.SelectTarget(0);
    if (d.HUD.MenuState == BattleMenuState.Main) { d.SelectMove(0); if (d.HUD.MenuState == BattleMenuState.SelectTarget) d.SelectTarget(0); }
    for (int guard = 0; guard < 25 && d.HUD.MenuState == BattleMenuState.Message && !d.IsBattleOver; guard++) { Confirm(d); Skip(25 / 60.0); }
    Shot("99_double_replace");

    // Two trainers together, and two wild Pokémon
    var a = new Trainer { Name = "Ana", TrainerClass = "Youngster" };
    a.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 6));
    var c = new Trainer { Name = "Cal", TrainerClass = "Lass" };
    c.Party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 6));
    party.HealAll();
    d = StartDouble(new[] { a, c }, Array.Empty<Pokemon>());
    Skip(130 / 60.0); Shot("9a_two_trainers");
    d = StartDouble(Array.Empty<Trainer>(), new[] { new Pokemon(PokemonDatabase.Get("Bidoof")!, 4), new Pokemon(PokemonDatabase.Get("Gible")!, 4) });
    Skip(130 / 60.0); Shot("9b_wild_pair");
    Confirm(d); Skip(70 / 60.0); Confirm(d); Skip(2 / 60.0); Shot("9c_wild_pair_main");
    Timing("double battle");
}


// ---------------------------------------------------------------- scripted demo battle (plan 04 · G8)

if (Run("demo"))
{
    var renderer = (BattleRenderer)Get("battleRenderer");
    // Barry's Torterra is far stronger than the lead, so the show can go on: it takes every move without fainting,
    // and plays harmless moves itself (one attack, to show the foe's side of a move)
    var demoParty = new Party();
    var lead = new Pokemon(PokemonDatabase.Get("Infernape")!, 60, new Random(3));
    demoParty.Add(lead);
    var rival = new Trainer { Name = "Barry", TrainerClass = "Rival" };
    var torterra = new Pokemon(PokemonDatabase.Get("Torterra")!, 90, new Random(4));
    rival.Party.Add(torterra);

    Set("currentMap", MapDatabase.Get("Sinnoh"));
    renderer.SetArena(BattleArena.Grass);
    var b = new BattleEngine(new BattleSetup
    {
        PlayerParty = demoParty, Inventory = inventory, Pokedex = pokedex, Trainers = new List<Trainer> { rival }, Random = new Random(11)
    });
    Set("battle", b);
    Set("currentState", GameState.Battle);

    // The opening: the sweep in, both trainers on their platforms, then each throws out their Pokémon
    Skip(0.05); Shot("d01_intro_sweep");
    Skip(1.0); Shot("d02_intro_trainers");
    Skip(1.0);
    Confirm(b);
    Skip(0.1); Shot("d03_foe_throw");
    Skip(0.22); Shot("d04_foe_ball_open");
    Skip(0.45); Shot("d05_foe_out");
    Skip(0.6);
    Confirm(b);
    Skip(0.1); Shot("d06_player_throw");
    Skip(0.25); Shot("d07_player_ball_open");
    Skip(0.5); Shot("d08_player_out");
    for (int guard = 0; guard < 6 && b.HUD.MenuState == BattleMenuState.Message; guard++) { Confirm(b); Skip(0.3); }
    Skip(0.6); Shot("d09_menu_overview");

    // Moves of every kind, through the real battle flow: the attacker's shot as it winds up, the cut to the
    // target as the effect arrives, the impact, and what follows. Both sides are healed between turns.
    int turn = 0;
    void Use(string move, string tag)
    {
        // Let the last turn's effects finish first
        Skip(1.0);
        lead.Moves.Clear();
        lead.Moves.Add(new Move(MoveDatabase.Get(move)!));
        torterra.Moves.Clear();
        torterra.Moves.Add(new Move(MoveDatabase.Get(turn++ switch { 0 => "Razor Leaf", 1 => "Withdraw", _ => "Splash" })!));
        lead.CurrentHP = lead.MaxHP;
        b.EnemyPokemon.CurrentHP = b.EnemyPokemon.MaxHP;
        lead.ResetStatStages();
        b.EnemyPokemon.ResetStatStages();
        // The lead always moves first, so each move's shots come before the foe's
        b.EnemyPokemon.StatStages[StatType.Speed] = -6;
        b.SelectMove(0);
        Console.WriteLine("  msg: " + b.CurrentMessage);
        Skip(0.12); Shot($"{tag}_a_windup");
        Skip(0.16); Shot($"{tag}_b_cut");
        Skip(0.12); Shot($"{tag}_c_impact");
        Skip(0.3); Shot($"{tag}_d_after");
        for (int guard = 0; guard < 12 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++)
        {
            Confirm(b);
            if (turn <= 2 && b.CurrentMessage.StartsWith("Foe") && b.CurrentMessage.Contains(" used "))
            {
                // The foe's attack, and its status move on itself
                Skip(0.12); Shot($"{tag}_e_foe_windup");
                Skip(0.36); Shot($"{tag}_f_foe_impact");
            }
            Skip(0.35);
        }
    }
    Use("Flamethrower", "d10_flamethrower");
    Use("Thunderbolt", "d11_thunderbolt");
    Use("Surf", "d12_surf");
    Use("Ice Beam", "d13_ice_beam");
    Use("Shadow Ball", "d14_shadow_ball");
    Use("Razor Leaf", "d15_razor_leaf");
    Use("Close Combat", "d16_close_combat");
    Use("Earthquake", "d17_earthquake");
    Use("Swords Dance", "d18_swords_dance");
    Use("Charm", "d19_charm");
    Use("Psychic", "d20_psychic");
    Use("Dragon Pulse", "d21_dragon_pulse");

    // A critical hit punches in (cued by hand, so the dice don't decide, with the messages it would show)
    var message = typeof(BattleEngine).GetField("currentMessage", Private)!;
    b.HUD.MenuState = BattleMenuState.Message;
    message.SetValue(b, $"{lead.DisplayName} used Slash!");
    b.Anim.Attack(BattleSide.Player, 0, MoveCategory.Physical);
    b.Anim.Cue(new EffectCue { Move = "Slash", Type = PokemonType.Normal, Category = MoveCategory.Physical, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Critical = true });
    Skip(0.36); b.Anim.Hit(BattleSide.Enemy, 0, 1.8f);
    Skip(0.12); Shot("d22_critical_punch_in");
    message.SetValue(b, "A critical hit!");
    Skip(0.3); Shot("d23_critical_after");
    b.HUD.MenuState = BattleMenuState.Main;
    Skip(1.0);

    // The foe faints
    lead.Moves.Clear();
    lead.Moves.Add(new Move(MoveDatabase.Get("Flare Blitz")!));
    b.EnemyPokemon.CurrentHP = 1;
    b.SelectMove(0);
    for (int guard = 0; guard < 6 && !b.CurrentMessage.Contains("fainted"); guard++) { Confirm(b); Skip(0.3); }
    Skip(0.5); Shot("d24_faint");
    Skip(0.5); Shot("d25_faint_sink");

    // A wild Pokémon, a thrown Poké Ball: the run-in, the throw, the flight, the red light, the wobbles, the click
    Set("currentMap", MapDatabase.Get("Sinnoh"));
    renderer.SetArena(BattleArena.Grass);
    var wild = new Pokemon(PokemonDatabase.Get("Starly")!, 3) { CurrentHP = 1, Status = StatusCondition.Sleep };
    inventory.AddItem(ItemDatabase.Get("Poké Ball")!, 1);
    var c = new BattleEngine(party, wild, inventory, pokedex, null, new List<Pokemon>());
    Set("battle", c);
    Skip(2.2); Confirm(c); Skip(1.2); Confirm(c); Skip(0.6);
    c.UseItem(ItemDatabase.Get("Poké Ball")!);
    Confirm(c);
    Skip(0.2); Shot("d30_ball_run_in");
    Skip(0.25); Shot("d31_ball_throw");
    Skip(0.45); Shot("d32_ball_flight");
    Skip(0.65); Shot("d33_ball_open_red_light");
    Skip(0.55); Shot("d34_ball_drop");
    Skip(0.35); Shot("d35_ball_wobble");
    Skip(2.0); Shot("d36_ball_click");
    Skip(0.8); Shot("d37_gotcha");
    Console.WriteLine("  msg: " + c.CurrentMessage);
}

// ---------------------------------------------------------------- arenas (plan 04 · G8)

if (Run("arenas"))
{
    var renderer = (BattleRenderer)Get("battleRenderer");
    void Arena(string name, BattleArena kind, PokemonType? theme = null, TreeStyle trees = TreeStyle.Round, bool lakeside = false, TimeOfDay time = TimeOfDay.Day)
    {
        if (!Wanted(name)) return;
        engine.Settings.TimeOfDay = time;
        engine.ApplySettings(window: false);
        Set("currentMap", MapDatabase.Get("Sinnoh"));
        renderer.SetArena(kind, theme, trees, lakeside);
        var b = new BattleEngine(party, new Pokemon(PokemonDatabase.Get("Shinx")!, 5, new Random(2)), inventory, pokedex);
        Set("battle", b);
        Set("currentState", GameState.Battle);
        Skip(2.2); Confirm(b); Skip(1.2); Confirm(b); Skip(0.8);
        Shot(name);
    }
    Arena("a01_grass", BattleArena.Grass);
    Arena("a02_grass_pines_lake", BattleArena.Grass, null, TreeStyle.Pine, true);
    Arena("a03_forest", BattleArena.Forest);
    Arena("a04_forest_night", BattleArena.Forest, time: TimeOfDay.Night);
    Arena("a05_cave", BattleArena.Cave);
    Arena("a06_water", BattleArena.Water);
    Arena("a07_water_twilight", BattleArena.Water, time: TimeOfDay.Twilight);
    Arena("a08_snow", BattleArena.Snow);
    Arena("a09_sand", BattleArena.Sand);
    Arena("a10_indoors", BattleArena.Indoors);
    Arena("a11_indoors_night", BattleArena.Indoors, time: TimeOfDay.Night);
    int g = 20;
    foreach (var type in new[] { PokemonType.Rock, PokemonType.Grass, PokemonType.Fighting, PokemonType.Water, PokemonType.Ghost, PokemonType.Steel, PokemonType.Ice, PokemonType.Electric })
        Arena($"a{g++}_gym_{type.ToString().ToLowerInvariant()}", BattleArena.Gym, type);
    int l = 30;
    foreach (var type in new PokemonType?[] { PokemonType.Bug, PokemonType.Ground, PokemonType.Fire, PokemonType.Psychic, null })
        Arena($"a{l++}_league_{type?.ToString().ToLowerInvariant() ?? "champion"}", BattleArena.League, type);
    engine.Settings.TimeOfDay = TimeOfDay.Day;
    engine.ApplySettings(window: false);
}

// ---------------------------------------------------------------- the real encounter flow

if (Run("flow"))
{
    // Grass encounter -> fade -> battle -> run -> fade back to the field (in grass no trainer is watching)
    GoTo("Route201", 24, 14, Direction.Up);
    T.GetMethod("StartWildBattle", Private)!
        .Invoke(engine, new object[] { new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3 } });
    Frames(12); Shot("80_flow_fade_out");
    Frames(36); Shot("81_flow_fade_in");
    Frames(60); Shot("82_flow_battle");
    var fb = (BattleEngine)Get("battle");
    Confirm(fb); Frames(40);
    Confirm(fb); Frames(2);
    fb.SelectMainMenuOption(3); Frames(2); Shot("83_flow_run");
    Confirm(fb);
    Frames(14); Shot("84_flow_leave");
    Frames(60); Shot("85_flow_back");
    Console.WriteLine("state: " + Get("currentState"));
}

// ---------------------------------------------------------------- menus

if (Run("menus"))
{
    // Start menu: sliding in, open, and the prompt behind QUIT GAME
    GoTo("TwinleafTown", 11, 8, Direction.Down);
    var startMenu = (StartMenu)Get("startMenu");
    startMenu.Open();
    Frames(4); Shot("09a_startmenu_sliding");
    Frames(30); Shot("09_startmenu");
    startMenu.Move(-1);
    Frames(2); Shot("09b_startmenu_quit");
    startMenu.Confirm();
    Frames(30); Shot("09c_quit_prompt");
    startMenu.Cancel();
    startMenu.Hide();

    // The sign on arriving somewhere, and a notice
    var sign = (LocationSign)Get("locationSign");
    sign.Show("Twinleaf Town");
    engine.ShowNotification("Game saved.");
    Frames(40); Shot("10_sign_and_notice");
    ShotCrop("10b_sign_native", 30, 20, 700, 150, 2);
    GoTo("Route201", 14, 10, Direction.Left);
    sign.Show("Route 201");
    Frames(40); Shot("10c_sign_route");
    sign.Hide();
    Frames(200);

    // Pokémon menu and summary, with a poisoned and a fainted Pokémon to show the status pills
    party.Members[1].Status = StatusCondition.Poison;
    party.Members[1].CurrentHP = party.Members[1].MaxHP / 3;
    party.Members[2].CurrentHP = 0;
    var partyScreen = (PartyScreen)Get("partyScreen");
    Set("currentState", GameState.PartyMenu);
    partyScreen.Open();
    Frames(6); Shot("20a_party_opening");
    Frames(40); Shot("20_party");
    partyScreen.ShowSummary = true;
    Frames(30); Shot("20b_summary");
    partyScreen.MoveCursor(0, 1, party.Count);
    Frames(30); Shot("20c_summary_second");
    partyScreen.Close();

    Set("currentState", GameState.StarterSelect);
    var starters = (StarterSelectScreen)Get("starterSelectScreen");
    starters.Open();
    Frames(8); Shot("21a_starter_opening");
    starters.Move(1);
    Frames(40); Shot("21_starter");
    starters.Confirm();
    Frames(30); Shot("21b_starter_asking");
    starters.Cancel();
    starters.Close();
    foreach (var (item, count) in new[]
             {
                 ("Potion", 5), ("Super Potion", 2), ("Antidote", 3), ("Revive", 1), ("Rare Candy", 2), ("Fire Stone", 1), ("Poké Ball", 10),
                 ("Great Ball", 3), ("Oran Berry", 4), ("Escape Rope", 2), ("Old Rod", 1), ("Town Map", 1), ("TM01", 1), ("Repel", 3)
             })
        if (ItemDatabase.Get(item) is { } data) inventory.AddItem(data, count);
    Set("currentState", GameState.BagMenu);
    var bag = (BagScreen)Get("bagScreen");
    bag.Open();
    Frames(6); Shot("22a_bag_opening");
    bag.MovePocket(1);
    bag.MoveCursor(1, 4);
    Frames(30); Shot("22_bag");
    bag.Confirm(inventory, party, engine.ShowNotification);
    Frames(4); Shot("22b_bag_actions");
    bag.Confirm(inventory, party, engine.ShowNotification);
    Frames(30); Shot("22c_bag_use_on");
    bag.CancelTarget();
    bag.MovePocket(2);
    Frames(4); Shot("22d_bag_tm");
    bag.MovePocket(4);
    Frames(4); Shot("22e_bag_key_items");
    bag.MovePocket(-7);
    Frames(4); Shot("22f_bag_items");
    bag.Close();

    // The Trainer Card, opened as the start menu opens it (so it has the player's portrait), with two badges won
    Set("currentState", GameState.Overworld);
    Set("badgesMask", 0b11);
    var handle = T.GetMethod("HandleStartMenuChoice", Private) ?? T.GetMethod("HandleStartMenu", Private);
    handle!.Invoke(engine, new object[] { StartMenuChoice.Trainer });
    Frames(40); Shot("28_trainer_card");
    ((TrainerCardScreen)Get("trainerCardScreen")).Close();
    Set("badgesMask", 0);

    // Saving: the question over the field, and the moment after
    Set("currentState", GameState.Overworld);
    handle.Invoke(engine, new object[] { StartMenuChoice.Save });
    var saving = (SaveScreen)Get("saveScreen");
    Frames(30); Shot("31_save_asking");
    saving.Confirm();
    Frames(12); Shot("31b_saved");
    Frames(120);
    Console.WriteLine($"after saving: state {Get("currentState")}, a save was written: {File.Exists("savegame.json")}");
    File.Delete("savegame.json");
    Set("currentState", GameState.Overworld);
    Set("currentState", GameState.Shop);
    var shop = (ShopScreen)Get("shopScreen");
    shop.Open("Sandgem Poké Mart");
    Frames(30); shop.Move(0, 1, 3000); shop.Move(0, 1, 3000);
    Frames(2); Shot("29_shop");
    shop.Confirm(inventory, 3000, engine.ShowNotification);
    shop.Move(1, 0, 3000); shop.Move(1, 0, 3000);
    Frames(30); Shot("29b_shop_how_many");
    shop.Cancel();
    shop.Close();
    var boxed = (List<Pokemon>)Get("pcBoxStorage");
    foreach (var name in new[] { "Starly", "Bidoof", "Shinx", "Budew", "Kricketot", "Staravia", "Luxio", "Riolu", "Gible", "Prinplup" })
        boxed.Add(new Pokemon(PokemonDatabase.Get(name)!, 4 + boxed.Count * 3));
    Set("currentState", GameState.PCStorage);
    var pc = (PCScreen)Get("pcScreen");
    pc.Open();
    Frames(30); Shot("30_pc");
    pc.Move(1, 0, party.Count); pc.Move(1, 0, party.Count); pc.Move(0, 1, party.Count);
    Frames(4); Shot("30b_pc_in_the_box");
    pc.Move(0, -1, party.Count); pc.Move(0, -1, party.Count);
    Frames(4); Shot("30c_pc_box_name");
    pc.Close();
    boxed.Clear();

    // The battle's panels for switching and for the bag
    var mb = StartBattle("Shinx", 5);
    ToMainMenu(mb);
    mb.HUD.MenuState = BattleMenuState.SwitchPokemon;
    mb.HUD.SwitchMenuIndex = 1;
    Frames(2); Shot("23_battle_switch");
    mb.HUD.MenuState = BattleMenuState.SelectBagItem;
    mb.HUD.BagMenuIndex = 2;
    Frames(2); Shot("24_battle_bag");
    ShotCrop("24b_battle_bag_native", 40, 820, 1140, 230, 2);
    mb.HUD.MenuState = BattleMenuState.Main;

    // A trainer battle, for the row of balls under the foe's box and a long message
    var menuTrainer = MapDatabase.Get("Sinnoh").NPCs.First(n => n.IsTrainer).TrainerData!;
    mb = StartBattle("", 0, menuTrainer);
    Frames(131); Confirm(mb); Frames(74); Confirm(mb); Frames(74); Confirm(mb); Frames(2);
    Shot("25_trainer_hud");

    // The Pokédex: the Sinnoh list and an entry's three pages, the search and its results, the National Pokédex
    // and a diploma; then a battle against a species from a later generation
    var dexScreen = (PokedexScreen)Get("pokedexScreen");
    pokedex.RegisterCaught(387);
    pokedex.RegisterSeen(906);
    pokedex.RegisterSeen(396);
    pokedex.RegisterSeen(399);
    foreach (string caught in new[] { "Onix", "Bidoof", "Shinx", "Psyduck", "Machop" }) pokedex.RegisterCaught(PokemonDatabase.Get(caught)!.DexNumber);
    void Pick(string name) => dexScreen.SelectedIndex = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == name);
    // Menu sprites are baked the first time a menu asks for them: bake the ones these shots show beforehand
    var dexSprites = typeof(GameEngine).Assembly.GetType("PokemonPlatinumEngine.Graphics.PokemonSprites")!;
    foreach (int seen in pokedex.SeenSpecies) dexSprites.GetMethod("Request")!.Invoke(null, new object[] { PokemonDatabase.GetByDex(seen)!.Name });
    dexSprites.GetMethod("Flush")!.Invoke(null, new[] { Get("renderContext") });
    void Page(PokedexPage page) { while (dexScreen.Page != page) dexScreen.Sideways(1); }
    Set("currentState", GameState.PokedexMenu);
    dexScreen.Open(pokedex, ((WorldRenderer)Get("world")).Portrait(PlayerIdentity.Character));
    Frames(30); Shot("26_pokedex_turtwig");
    Pick("Starly");
    Frames(2); Shot("26c_pokedex_seen_only");
    Pick("Bibarel");
    Frames(2); Shot("26d_pokedex_unseen");
    Pick("Starly");
    dexScreen.Confirm();
    Page(PokedexPage.Area);
    Frames(2); Shot("26e_pokedex_area_starly");
    Pick("Psyduck");
    Frames(2); Shot("26f_pokedex_area_psyduck");
    Pick("Turtwig");
    Frames(2); Shot("26g_pokedex_area_unknown");
    Page(PokedexPage.Size);
    Frames(2); Shot("26h_pokedex_size_turtwig");
    Pick("Onix");
    Frames(2); Shot("26i_pokedex_size_onix");
    dexScreen.Cancel();
    dexScreen.OpenSearch();
    for (int i = 0; i < 2; i++) dexScreen.Sideways(1);   // the heaviest first
    Frames(2); Shot("26j_pokedex_search");
    dexScreen.Search();
    Frames(2); Shot("26k_pokedex_results_heaviest");
    dexScreen.Cancel();
    pokedex.UnlockNational();
    dexScreen.OpenSearch();
    dexScreen.Sideways(1);
    dexScreen.Search();
    Pick("Sprigatito");
    Frames(2); Shot("26b_pokedex_later_generation");
    var complete = new Pokedex();
    foreach (var e in Pokedex.Entries(PokedexMode.Sinnoh)) complete.RegisterSeen(e.Species.DexNumber);
    dexScreen.Open(complete);
    Frames(20); Shot("26l_pokedex_diploma");
    dexScreen.Close();
    var later = StartBattle("Sprigatito", 5);
    ToMainMenu(later);
    Frames(2); Shot("27_battle_later_generation");

    if (args.Length > 2)
        Boards(args[2], new[]
        {
            "09_startmenu", "20b_summary", "23_battle_switch", "24_battle_bag", "21_starter", "22_bag", "26_pokedex_turtwig",
            "28_trainer_card", "29_shop", "30_pc"
        });

    party.Members[1].Status = StatusCondition.None;
    party.HealAll();
    Set("currentState", GameState.Overworld);
}

// ---------------------------------------------------------------- evolution

if (Run("evolution"))
{
    var scene = (EvolutionScreen)Get("evolutionScreen");
    var before = party.Members.ToList();
    EvolutionContext Now() => new() { Party = party, Bag = inventory };
    Pokemon Fresh(string species, int level) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);
    void Until(Func<bool> done, int limit = 1500) { for (int i = 0; i < limit && !done(); i++) Frames(1); }
    void Dismiss() { for (int i = 0; i < 8 && scene.IsActive; i++) { scene.PressConfirm(); Frames(2); } }

    // A level-up evolution from the first message to the last: Turtwig into Grotle
    var turtwig = Fresh("Turtwig", 18);
    Set("currentState", GameState.Evolution);
    scene.Begin(turtwig, Evolution.Find(turtwig, EvolutionTrigger.LevelUp, Now())!, Now(), cancellable: true);
    Frames(40); Shot("e01_notice");
    Until(() => scene.Phase == EvolutionPhase.Gather); Frames(60); Shot("e02_gather");
    Until(() => scene.Phase == EvolutionPhase.Morph);
    Until(() => scene.Look().OldScale > 0.8f); Shot("e03_morph_old");
    Until(() => scene.Look().NewScale > 0.4f); Shot("e04_morph_new");
    Timing("evolution scene", 60);
    Until(() => scene.Look().NewScale > 0.8f || scene.Phase != EvolutionPhase.Morph); Shot("e05_morph_late");
    Until(() => scene.Phase == EvolutionPhase.Burst); Frames(10); Shot("e06_burst");
    Until(() => scene.Phase == EvolutionPhase.Reveal); Frames(22); Shot("e07_reveal");
    Until(() => scene.Phase == EvolutionPhase.Congratulate); Frames(70); Shot("e08_congratulations");
    Dismiss();
    Console.WriteLine($"evolved into {turtwig.Species.Name}; scene active: {scene.IsActive}");

    // Four moves and a fifth on offer: Chimchar into Monferno, which learns Mach Punch at that level
    var chimchar = Fresh("Chimchar", 14);
    Frames(60);
    Set("currentState", GameState.Evolution);
    scene.Begin(chimchar, Evolution.Find(chimchar, EvolutionTrigger.LevelUp, Now())!, Now(), cancellable: true);
    Until(() => scene.Phase == EvolutionPhase.Congratulate);
    scene.PressConfirm();
    Frames(40); Shot("e09_move_choice");
    scene.MoveCursor(-1);
    Frames(4); Shot("e09b_move_choice_keep");
    scene.MoveCursor(3);
    scene.PressConfirm();
    Frames(50); Shot("e10_move_learned");
    Dismiss();
    Console.WriteLine($"{chimchar.Species.Name} knows {string.Join(", ", chimchar.Moves.Select(m => m.Name))}");

    // Stopped with B
    var starly = Fresh("Starly", 14);
    Frames(60);
    Set("currentState", GameState.Evolution);
    scene.Begin(starly, Evolution.Find(starly, EvolutionTrigger.LevelUp, Now())!, Now(), cancellable: true);
    Until(() => scene.Phase == EvolutionPhase.Morph); Frames(50);
    scene.PressCancel();
    Frames(50); Shot("e11_stopped");
    Dismiss();
    Console.WriteLine($"still a {starly.Species.Name}");

    // A stone from the bag: who it works on, then the scene through the game's own states and back to the bag
    var eevee = Fresh("Eevee", 12);
    party.Add(eevee);
    inventory.AddItem(ItemDatabase.Get("Fire Stone")!, 1);
    var bag = (BagScreen)Get("bagScreen");
    Set("currentState", GameState.BagMenu);
    bag.Open();
    bag.BeginTargetChoice(ItemDatabase.Get("Fire Stone")!);
    Frames(40); Shot("e12_bag_choice");
    bag.MoveTarget(1, 0, party.Count);
    bag.MoveTarget(0, 1, party.Count);
    Frames(4); Shot("e12b_bag_choice_able");
    bag.UseOnTarget(inventory, party, engine.ShowNotification, Now());
    Frames(12); Shot("e13_fade_to_scene");
    Until(() => scene.Phase == EvolutionPhase.Morph && (GameState)Get("currentState") == GameState.Evolution);
    Until(() => scene.Look().NewScale > 0.5f); Shot("e14_stone_morph");
    Until(() => scene.Phase == EvolutionPhase.Congratulate); Frames(60); Shot("e15_stone_evolved");
    Dismiss();
    Frames(70);
    Console.WriteLine($"after the stone: {eevee.Species.Name}, state {Get("currentState")}, stones left {inventory.GetQuantity(ItemDatabase.Get("Fire Stone")!)}");
    bag.Close();

    // A trade: the Pokémon given leaves, the one received arrives and evolves, and the field comes back
    GoTo("TwinleafTown", 11, 8, Direction.Down);
    var kadabra = Fresh("Kadabra", 30);
    engine.ReceiveTradedPokemon(kadabra, eevee);
    Until(() => (GameState)Get("currentState") == GameState.Evolution);
    Until(() => scene.Phase == EvolutionPhase.Congratulate); Frames(60); Shot("e16_trade_evolved");
    Dismiss();
    Frames(70);
    Console.WriteLine($"after the trade: {string.Join(", ", party.Members.Select(m => m.Species.Name))}, state {Get("currentState")}, " +
                      $"Alakazam caught: {pokedex.IsCaught(PokemonDatabase.Get("Alakazam")!.DexNumber)}");

    // A battle won: the Pokémon that grew in it evolves once it is over, before the field comes back
    var grower = Fresh("Starly", 13);
    grower.CurrentExp = grower.ExpForNextLevel - 1;
    party.Members[0] = grower;
    GoTo("Route201", 24, 14, Direction.Up);
    T.GetMethod("StartWildBattle", Private)!
        .Invoke(engine, new object[] { new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 2, MaxLevel = 2 } });
    Frames(60);
    var won = (BattleEngine)Get("battle");
    for (int i = 0; i < 4000 && (GameState)Get("currentState") != GameState.Evolution; i++)
    {
        if ((GameState)Get("currentState") == GameState.Battle && !won.IsBattleOver)
        {
            if (won.IsWaitingForConfirm) won.ConfirmMessage();
            else if (won.HUD.MenuState != BattleMenuState.Message) won.SelectMove(0);
        }
        Frames(1);
    }
    Console.WriteLine($"after the battle: {grower.Species.Name} Lv {grower.Level}, state {Get("currentState")}");
    Frames(30); Shot("e17_after_battle");
    Until(() => scene.Phase == EvolutionPhase.Congratulate);
    Dismiss();
    Frames(70);
    Console.WriteLine($"after its evolution: {grower.Species.Name}, state {Get("currentState")}");

    party.Clear();
    foreach (var member in before) party.Add(member);
    Set("currentState", GameState.Overworld);
}

// ---------------------------------------------------------------- style guide reference frames (docs/art/style-guide.md)

// Boards: the same frame from an earlier run next to this run's, at half size with a label over each
void Boards(string beforeDir, string[] frames)
{
    foreach (var frame in frames)
    {
        string before = Path.Combine(Path.GetFullPath(beforeDir, startDir), frame + ".png");
        string after = Path.Combine(outDir, frame + ".png");
        if (!File.Exists(before) || !File.Exists(after)) continue;
        var board = Raylib.GenImageColor(1920 + 30, 540 + 76, new Color(24, 26, 34, 255));
        int col = 0;
        foreach (var (path, label) in new[] { (before, "BEFORE"), (after, "AFTER") })
        {
            var img = Raylib.LoadImage(path);
            Raylib.ImageResize(ref img, 960, 540);
            int x = 10 + col * 970;
            Raylib.ImageDraw(ref board, img, new Rectangle(0, 0, 960, 540), new Rectangle(x, 66, 960, 540), Color.White);
            Raylib.ImageDrawText(ref board, label, x + 4, 18, 40, Color.White);
            Raylib.UnloadImage(img);
            col++;
        }
        Save(board, "compare_" + frame);
    }
}

if (Run("look"))
{
    // The reference frames by day. Pass the folder of an earlier run as the third argument to also get
    // before/after boards (compare_*.png).
    foreach (var name in new[] { "Starly", "Shinx", "Bidoof" })
        if (party.Count < 6) party.Add(new Pokemon(PokemonDatabase.Get(name)!, 4 + party.Count));
    party.Members[1].CurrentHP = party.Members[1].MaxHP / 3;
    party.Members[3].CurrentHP = party.Members[3].MaxHP / 7;

    GoTo("TwinleafTown", 11, 8, Direction.Down); Frames(2); Shot("look_1_twinleaf");
    ShotCrop("look_1b_twinleaf_native", 720, 300, 480, 270, 2);

    GoTo("TwinleafTown", 12, 7, Direction.Up);
    ((DialogueManager)Get("dialogue")).ShowDialogue("Barry", new List<string> { "Barry: Hey, Lucas! You're finally ready! Professor Rowan is waiting at Lake Verity!" });
    Set("currentState", GameState.Dialogue);
    Frames(180); Shot("look_2_dialogue");
    Set("dialogue", new DialogueManager());

    var pb = StartBattle("Shinx", 5);
    ToMainMenu(pb);
    Shot("look_3_battle");
    ShotCrop("look_3c_hud_native", 1200, 640, 480, 270, 2);
    pb.HUD.MenuState = BattleMenuState.Moves; Frames(1); Shot("look_3b_moves");
    pb.HUD.MenuState = BattleMenuState.Main;

    Set("currentState", GameState.PartyMenu);
    ((PartyScreen)Get("partyScreen")).Open();
    Frames(40); Shot("look_4_party");
    ((PartyScreen)Get("partyScreen")).Close();

    GoTo("Route201", 24, 8, Direction.Up); Frames(2); Shot("look_5_route201");
    GoTo("PlayerHouse", 4, 6, Direction.Up); Frames(2); Shot("look_6_house");
    GoTo("LakeVerity", 14, 11, Direction.Up); Frames(2); Shot("look_7_lake");

    Set("currentState", GameState.Options);
    ((OptionsScreen)Get("optionsScreen")).Open();
    Frames(1); Shot("look_8_options");
    Set("currentState", GameState.Overworld);

    GoTo("TwinleafTown", 11, 8, Direction.Down);
    Timing("field");
    StartBattle("Luxray", 30);
    ToMainMenu((BattleEngine)Get("battle"));
    Timing("battle");

    if (args.Length > 2)
        Boards(args[2], new[] { "look_1_twinleaf", "look_2_dialogue", "look_3_battle", "look_3b_moves", "look_4_party", "look_5_route201", "look_6_house", "look_7_lake" });
}

// ---------------------------------------------------------------- the new-game introduction (plan 04 · G10)

// The professor's welcome step by step: fading in, each beat of his talk, the Pokémon coming out of its ball,
// the choice of who to be, the name keyboard, the send-off. Run by itself (not as part of `all`) it also lets
// the introduction end and shows the game it starts: the field, the Trainer Card and a battle as the girl.
if (Run("intro"))
{
    var intro = (IntroScreen)Get("introScreen");
    Set("currentState", GameState.Intro);
    intro.Open();
    // Presses the A button through whatever is being said until the introduction reaches a phase
    void Until(IntroPhase phase, int limit = 2000)
    {
        for (int guard = 0; guard < limit && intro.Phase != phase; guard++)
        {
            if (intro.Talking && intro.LineComplete) intro.PressConfirm();
            Frames(1);
        }
    }
    // Waits for the line being written to be all there
    void Line() { for (int guard = 0; guard < 600 && intro.Talking && !intro.LineComplete; guard++) Frames(1); Frames(2); }

    Frames(36); Shot("i01_fading_in");
    Until(IntroPhase.Greeting); Frames(40); Shot("i02_hello");
    Line(); intro.PressConfirm(); Line(); intro.PressConfirm(); Line(); Shot("i03_professor_rowan");
    Until(IntroPhase.World); Line(); Shot("i04_the_world");
    Until(IntroPhase.BallOpens); Frames(32); Shot("i05_ball");
    Frames(14); Shot("i06_flash");
    Frames(14); Shot("i07_pokemon_appears");
    Until(IntroPhase.Alongside); Frames(14); Shot("i08_pokemon_hops");
    Line(); Frames(60); Shot("i09_alongside");
    Timing("introduction");
    Until(IntroPhase.BallCloses); Frames(14); Shot("i10_pokemon_returns");
    Until(IntroPhase.AboutYou); Line(); Shot("i11_about_you");
    Until(IntroPhase.ChooseLook); Frames(40); Shot("i12_boy_or_girl");
    intro.Move(1, 0); Frames(40); Shot("i13_the_girl");
    intro.PressConfirm(); Frames(30); Shot("i14_so_you_are_a_girl");
    intro.PressConfirm();
    Until(IntroPhase.AskName); Line(); Shot("i15_your_name");
    Until(IntroPhase.EnterName); Frames(30); Shot("i16_keyboard");
    // "Maya", through the keyboard's own cursor: M is the third key of the second row
    intro.Move(0, 1); intro.Move(1, 0); intro.Move(1, 0); intro.PressConfirm();
    foreach (char c in "aya") intro.Entry!.Type(c);
    Frames(20); Shot("i17_keyboard_name");
    ShotCrop("i17b_keyboard_native", 640, 180, 1200, 760, 2);
    intro.PressStart(); Frames(4); Shot("i18_keyboard_ok");
    intro.PressConfirm(); Frames(30); Shot("i19_so_you_are_maya");
    intro.PressConfirm();
    Until(IntroPhase.Farewell); Line(); Shot("i20_farewell");
    Until(IntroPhase.SendOff); Frames(48); Shot("i21_send_off");
    Frames(40); Shot("i22_shrinking");
    Frames(30); Shot("i23_nearly_gone");

    if (mode == "intro")
    {
        // Let it end: the game begins as Maya, the girl
        for (int guard = 0; guard < 400 && (GameState)Get("currentState") == GameState.Intro; guard++) Frames(1);
        Frames(60); Shot("i30_the_game_begins");
        Frames(120);
        var card = T.GetMethod("HandleStartMenuChoice", Private)!;
        card.Invoke(engine, new object[] { StartMenuChoice.Trainer });
        Frames(40); Shot("i31_her_trainer_card");
        ((TrainerCardScreen)Get("trainerCardScreen")).Close();
        Set("currentState", GameState.Overworld);
        var her = StartBattle("Starly", 3);
        Frames(150); Shot("i32_her_battle");
        Set("currentState", GameState.Overworld);
        // Sandgem's assistant is the one the player isn't: Lucas, with his own lines
        Set("currentMap", MapDatabase.Get("Sinnoh"));
        var helper = MapDatabase.Get("Sinnoh").NPCs.First(n => n.NpcType == "Assistant");
        ((Player)Get("player")).SetPosition(helper.GridX, helper.GridY + 1, Direction.Up);
        Frames(20);
        ((DialogueManager)Get("dialogue")).ShowDialogue(helper.Name, helper.DialogLines);
        Set("currentState", GameState.Dialogue);
        Frames(90); Shot("i33_the_assistant");
    }
    else
    {
        intro.Close();
        Set("currentState", GameState.Overworld);
    }
}

// ---------------------------------------------------------------- title screen

if (Run("title"))
{
    const float Dt = 1f / 60f;
    TitleScreen NewTitle(SaveData? save)
    {
        var title = new TitleScreen(save);
        Set("titleScreen", title);
        Set("currentState", GameState.Title);
        return title;
    }
    void Seconds(float s) => Frames((int)MathF.Round(s / Dt));

    // The opening from the start, with no saved game: notice, three fly-over shots, Giratina, the title, the menu
    var opening = NewTitle(null);
    Seconds(1.6f); Shot("title_1_notice");
    Seconds(TitleScreen.NoticeTime - 1.6f + 1.9f); Shot("title_2_journey_twinleaf");
    Seconds(TitleScreen.SegmentTime); Shot("title_3_journey_route201");
    Seconds(TitleScreen.SegmentTime); Shot("title_4_journey_sandgem");
    Seconds(TitleScreen.SegmentTime - 1.9f + 1.0f); Shot("title_5_reveal_silhouette");
    Seconds(0.9f); Shot("title_6_reveal_lit");
    Seconds(2.2f); Shot("title_7_idle");
    Timing("title");
    opening.PressConfirm();
    Seconds(1f); Shot("title_8_menu_no_save");

    // With a saved game: three badges, twelve and a half hours, a party of five
    var save = new SaveData
    {
        PlayerName = "Lucas",
        CurrentMapName = "SandgemTown",
        PlayTimeSeconds = 12 * 3600 + 34 * 60 + 20,
        Badges = 0b0000_0111,
        Money = 12480,
        CaughtSpecies = Enumerable.Range(387, 14).ToList(),
        Party = new[] { ("Grotle", 24), ("Staravia", 22), ("Luxio", 23), ("Bibarel", 20), ("Riolu", 18) }
            .Select(p => SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get(p.Item1)!, p.Item2))).ToList()
    };
    var saved = NewTitle(save);
    saved.PressConfirm();
    Seconds(1.2f);
    saved.PressConfirm();
    Seconds(1f); Shot("title_9_menu_continue");
    ShotCrop("title_9b_continue_native", 1016, 250, 824, 452, 2);
    saved.Move(1);
    Seconds(0.2f); Shot("title_10_menu_new_game");
    saved.PressConfirm();
    Seconds(0.2f); Shot("title_11_confirm_new_game");

    // "Yes" leads to the last question: whose rules. Platinum's are offered; the other answer has its own words
    saved.Move(1);
    saved.PressConfirm();
    Seconds(0.2f); Shot("title_12_rules_platinum");
    saved.Move(1);
    Seconds(0.2f); Shot("title_13_rules_modern");

    // A game played by the modern rules says so on its panel
    save.Rules = RulesPreset.Modern;
    var modern = NewTitle(save);
    modern.PressConfirm();
    Seconds(1.2f);
    modern.PressConfirm();
    Seconds(1f); Shot("title_14_continue_modern_rules");

    Set("currentState", GameState.Overworld);
}

// ---------------------------------------------------------------- terrain and nature (plan 04 · G4)

if (Run("terrain"))
{
    // Water: the pond, the lake from its shore and from its east bank, with native-resolution close-ups
    GoTo("TwinleafTown", 9, 14, Direction.Left); Frames(2); Shot("t1_pond");
    ShotCrop("t1b_pond_native", 330, 500, 560, 400, 2);
    GoTo("LakeVerity", 14, 10, Direction.Up); Frames(2); Shot("t2_lake");
    GoTo("LakeVerity", 24, 5, Direction.Left); Frames(2); Shot("t2b_lake_east");
    ShotCrop("t2c_lake_native", 560, 300, 560, 400, 2);

    // Tall grass with someone standing in it, the ledge, a boulder
    GoTo("Route201", 22, 15, Direction.Down); Frames(2); Shot("t3_in_grass");
    ShotCrop("t3b_grass_native", 680, 340, 560, 400, 2);
    GoTo("Route201", 22, 10, Direction.Down); Frames(2); Shot("t4_ledge");
    ShotCrop("t4b_ledge_native", 560, 560, 560, 300, 2);

    // Trees: Twinleaf's pines and Route 201's round trees
    GoTo("TwinleafTown", 3, 3, Direction.Left); Frames(2); Shot("t5_pines");
    ShotCrop("t5b_pines_native", 0, 300, 560, 400, 2);
    GoTo("Route201", 32, 3, Direction.Right); Frames(2); Shot("t6_round_trees");
    ShotCrop("t6b_round_native", 1300, 200, 560, 400, 2);
    GoTo("TwinleafTown", 6, 9, Direction.Up); Frames(2); Shot("t7_flowers");
    ShotCrop("t7b_flowers_native", 620, 300, 560, 400, 2);

    // The other ground kinds, on a sample map: sand round a pool, dirt, snow and a cave floor
    var sample = new Map(26, 16) { Name = "TerrainSample", DisplayName = "Terrain sample" };
    for (int x = 0; x < 26; x++) { sample.SetGroundTile(x, 0, TileType.Tree, true); sample.SetGroundTile(x, 15, TileType.Tree, true); }
    for (int y = 0; y < 16; y++) { sample.SetGroundTile(0, y, TileType.Tree, true); sample.SetGroundTile(25, y, TileType.Tree, true); }
    void Patch(int x0, int y0, int w, int h, TileType t) { for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) sample.SetGroundTile(x, y, t); }
    Patch(2, 2, 10, 6, TileType.Sand);
    Patch(5, 4, 4, 2, TileType.Water);
    Patch(14, 2, 9, 5, TileType.Dirt);
    Patch(2, 9, 10, 5, TileType.Snow);
    Patch(14, 9, 9, 5, TileType.CaveFloor);
    Patch(12, 2, 1, 12, TileType.Path);
    Set("currentMap", sample);
    ((Player)Get("player")).SetPosition(12, 6, Direction.Down);
    Set("currentState", GameState.Overworld);
    Frames(2); Shot("t8_ground_kinds_north");
    ((Player)Get("player")).SetPosition(12, 10, Direction.Down);
    Frames(2); Shot("t8b_ground_kinds_south");
    ShotCrop("t8c_ground_native", 100, 300, 760, 420, 2);

    // Water after dark
    engine.Settings.TimeOfDay = TimeOfDay.Night;
    engine.ApplySettings(window: false);
    GoTo("LakeVerity", 14, 10, Direction.Up); Frames(2); Shot("t9_lake_night");
    engine.Settings.TimeOfDay = TimeOfDay.Day;
    engine.ApplySettings(window: false);

    GoTo("Route201", 22, 10, Direction.Down);
    Timing("route 201");
    GoTo("LakeVerity", 14, 10, Direction.Up);
    Timing("lake verity");

    // Battles: boulders round the meadow, and a lake behind the opponent on maps that have one
    var lakeBattle = StartBattle("Bidoof", 4, null, "LakeVerity");
    ToMainMenu(lakeBattle);
    Shot("t10_battle_lakeside");
    Timing("battle by the lake");
    var routeBattle = StartBattle("Starly", 4, null, "Route201");
    ToMainMenu(routeBattle);
    Shot("t11_battle_route");
    Set("currentState", GameState.Overworld);

    if (args.Length > 2) Boards(args[2], new[] { "t1_pond", "t2_lake", "t2b_lake_east", "t3_in_grass", "t4_ledge", "t5_pines", "t6_round_trees", "t7_flowers", "t9_lake_night", "t10_battle_lakeside", "t11_battle_route" });
}

// ---------------------------------------------------------------- buildings, props and rooms (plan 04 · G5)

if (Run("buildings"))
{
    // Every building from the street, with native-resolution close-ups of one of each family
    (string Name, string Map, int X, int Y)[] streets =
    {
        ("b01_twinleaf_house", "TwinleafTown", 6, 9), ("b02_twinleaf_rival", "TwinleafTown", 17, 9), ("b03_twinleaf_town", "TwinleafTown", 11, 9),
        ("b04_sandgem_center", "SandgemTown", 6, 8), ("b05_sandgem_mart", "SandgemTown", 22, 8), ("b06_sandgem_lab", "SandgemTown", 7, 19),
        ("b07_jubilife_school", "JubilifeCity", 7, 9), ("b08_jubilife_poketch_tv", "JubilifeCity", 30, 9),
        ("b09_jubilife_center_mart", "JubilifeCity", 28, 28), ("b10_jubilife_terminal", "JubilifeCity", 9, 28),
        ("b11_jubilife_crossroads", "JubilifeCity", 19, 17), ("b12_pallet", "PalletTown", 5, 8)
    };
    foreach (var (name, map, x, y) in streets)
    {
        GoTo(map, x, y, Direction.Up); Frames(2); Shot(name);
        if (name is "b01_twinleaf_house" or "b04_sandgem_center" or "b07_jubilife_school" or "b10_jubilife_terminal" or "b12_pallet")
            ShotCrop(name + "_native", 640, 150, 640, 360, 2);
    }

    // The same streets after dark: windows, lamps and the light they throw
    engine.Settings.TimeOfDay = TimeOfDay.Night;
    engine.ApplySettings(window: false);
    GoTo("TwinleafTown", 11, 9, Direction.Up); Frames(2); Shot("b20_twinleaf_night");
    GoTo("SandgemTown", 14, 8, Direction.Up); Frames(2); Shot("b21_sandgem_night");
    GoTo("JubilifeCity", 28, 28, Direction.Up); Frames(2); Shot("b22_jubilife_night");
    GoTo("PlayerHouse", 4, 6, Direction.Up); Frames(2); Shot("b23_house_night");
    engine.Settings.TimeOfDay = TimeOfDay.LateNight;
    engine.ApplySettings(window: false);
    GoTo("TwinleafTown", 11, 9, Direction.Up); Frames(2); Shot("b24_twinleaf_late_night");
    engine.Settings.TimeOfDay = TimeOfDay.Twilight;
    engine.ApplySettings(window: false);
    GoTo("JubilifeCity", 19, 17, Direction.Up); Frames(2); Shot("b25_jubilife_twilight");
    GoTo("PokemonCenter", 5, 6, Direction.Up); Frames(2); Shot("b26_center_twilight");
    engine.Settings.TimeOfDay = TimeOfDay.Day;
    engine.ApplySettings(window: false);

    // Every room
    (string Name, string Map, int X, int Y)[] rooms =
    {
        ("b30_player_house", "PlayerHouse", 4, 6), ("b31_rival_house", "RivalHouse", 5, 7), ("b32_pallet_house", "PalletPlayerHouse", 4, 6),
        ("b33_pokemon_center", "PokemonCenter", 5, 6), ("b34_mart", "PokeMart", 4, 5), ("b35_lab", "RowanLab", 5, 7),
        ("b36_school", "TrainersSchool", 6, 9), ("b37_poketch", "PoketchCompany", 5, 7)
    };
    foreach (var (name, map, x, y) in rooms)
    {
        GoTo(map, x, y, Direction.Up); Frames(2); Shot(name);
        if (name is "b30_player_house" or "b33_pokemon_center" or "b34_mart" or "b35_lab")
        {
            ShotCrop(name + "_native_left", 400, 60, 640, 360, 2);
            ShotCrop(name + "_native_right", 900, 60, 640, 360, 2);
        }
    }

    GoTo("TwinleafTown", 11, 9, Direction.Up); Timing("twinleaf");
    GoTo("JubilifeCity", 28, 28, Direction.Up); Timing("jubilife");
    GoTo("JubilifeCity", 19, 17, Direction.Up); Timing("jubilife crossroads");
    GoTo("PokemonCenter", 5, 6, Direction.Up); Timing("pokemon center");
    GoTo("TrainersSchool", 6, 9, Direction.Up); Timing("trainers school");

    if (args.Length > 2)
        Boards(args[2], streets.Select(s => s.Name).Concat(rooms.Select(r => r.Name))
            .Concat(new[] { "b20_twinleaf_night", "b21_sandgem_night", "b22_jubilife_night", "b23_house_night", "b24_twinleaf_late_night", "b25_jubilife_twilight", "b26_center_twilight" }).ToArray());
}

// ---------------------------------------------------------------- the imported world (plan 01 · M2)

if (mode == "area")
{
    string key = args.Length > 2 ? args[2] : "twinleaf_town";
    var (areaMap, ax, ay) = AreaSpot(key);
    At(areaMap.Name, ax, ay, Direction.Down); Frames(2); Shot("area_" + key);
    Console.WriteLine($"{key}: {areaMap.Name} ({ax}, {ay})");
}

// ---------------------------------------------------------------- every town of Sinnoh (plan 01 · M4)

// The whole of Sinnoh as the importer last wrote it (tools/MapImporter/out/world: run the importer first), with
// every area open, to look at the buildings and landmarks of towns the game hasn't reached yet. For each town:
// a shot before each of its different buildings and the things that stand about, and a sheet of them all.
if (mode == "cities")
{
    string? repo = null;
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null && repo == null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) repo = dir.FullName;
    string import = Path.Combine(repo ?? startDir, "tools", "MapImporter", "out", "world");
    if (!Directory.Exists(Path.Combine(import, "chunks")))
    {
        Console.WriteLine($"No import at {import}: run `dotnet run --project tools/MapImporter` first.");
        return;
    }

    var index = new WorldIndexFile { Region = "Sinnoh" };
    index.Maps.Add(new WorldMapEntry { Name = "SinnohWhole", Matrix = 0, Trees = TreeStyle.Pine });
    foreach (string file in Directory.GetFiles(Path.Combine(import, "areas"), "*.json")) index.Areas.Add(Path.GetFileNameWithoutExtension(file));
    var built = System.Diagnostics.Stopwatch.StartNew();
    var whole = WorldMapBuilder.Build(World.Open(import, index), index.Maps[0]);
    var all = MapStructures.BuildingsOf(whole);
    Console.WriteLine($"all of Sinnoh: {all.Count} blocks of buildings, {whole.Props.Count} props, made in {built.Elapsed.TotalSeconds:F1} s");

    string[] towns = args.Length > 2 ? args[2..] : new[]
    {
        "twinleaf_town", "sandgem_town", "jubilife_city", "oreburgh_city", "floaroma_town", "valley_windworks_outside", "eterna_city",
        "hearthome_city", "solaceon_town", "veilstone_city", "pastoria_city", "celestic_town", "canalave_city", "snowpoint_city",
        "sunyshore_city", "pokemon_league", "route_209", "route_212_north", "route_213", "valor_lakefront", "route_221",
        "fuego_ironworks_outside", "fight_area", "survival_area", "resort_area"
    };
    var walker = (Player)Get("player");
    void Look(string name, int x, int y)
    {
        Set("currentMap", whole);
        walker.SetPosition(Math.Clamp(x, 0, whole.Width - 1), Math.Clamp(y, 0, whole.Height - 1), Direction.Up);
        Set("currentState", GameState.Overworld);
        Frames(3);
        Shot(name);
    }

    foreach (string town in towns)
    {
        if (whole.FindArea(town) == null) { Console.WriteLine($"{town}: no such area on the overworld"); continue; }
        bool Here(int x, int y) => whole.AreaAt(x, y)?.Key == town;
        var shots = new List<string>();

        // One of each model, the town's own buildings before its houses and blocks of flats
        var seen = new HashSet<string>();
        foreach (var b in all.Where(b => !b.Annex && Here(b.X0, b.Y0))
                     .OrderBy(b => b.Kind is BuildingKind.House or BuildingKind.Apartments ? 1 : 0).ThenBy(b => b.Y0).ThenBy(b => b.X0))
        {
            if (!seen.Add(b.Model + (b.Kind == BuildingKind.Gym ? "" : "")) || shots.Count >= 12) continue;
            int x = b.Doors.Count > 0 ? b.Doors[0].X : (b.X0 + b.X1) / 2;
            // Far enough back to see a tall block whole
            int back = 3 + (b.Porch.Count > 0 ? 1 : 0) + (int)MathF.Min(4f, MathF.Max(0f, b.Height - 6f) / 2.5f);
            string name = $"c_{town}_{shots.Count + 1}_{b.Model}";
            Look(name, x, b.Y1 + back);
            shots.Add(name);
            // A tall block's top is out of the frame from the street: look at it from over its roof as well
            if (b.Height > 8f && shots.Count < 12)
            {
                name = $"c_{town}_{shots.Count + 1}_{b.Model}_top";
                Look(name, (b.X0 + b.X1) / 2, b.Y0 + 1);
                shots.Add(name);
            }
        }
        var things = new HashSet<PropType>();
        foreach (var prop in whole.Props.Where(p => p.Model.Length > 0 && Here(p.X, p.Y)).OrderBy(p => p.Y).ThenBy(p => p.X))
        {
            if (!things.Add(prop.Type) || shots.Count >= 15) continue;
            string name = $"c_{town}_{shots.Count + 1}_{prop.Model}";
            Look(name, prop.X + prop.Width / 2, prop.Y + prop.Depth + 2);
            shots.Add(name);
        }
        if (shots.Count == 0) { Console.WriteLine($"{town}: nothing stands there"); continue; }

        // The sheet: three shots to a row at a third of their size
        const int cellW = 640, cellH = 360, gap = 6;
        int rows = (shots.Count + 2) / 3;
        var sheet = Raylib.GenImageColor(3 * cellW + 4 * gap, rows * (cellH + gap) + gap, new Color(24, 26, 34, 255));
        for (int i = 0; i < shots.Count; i++)
        {
            string path = Path.Combine(outDir, shots[i] + ".png");
            if (!File.Exists(path)) continue;
            var img = Raylib.LoadImage(path);
            Raylib.ImageResize(ref img, cellW, cellH);
            Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, cellW, cellH),
                new Rectangle(gap + i % 3 * (cellW + gap), gap + i / 3 * (cellH + gap), cellW, cellH), Color.White);
            Raylib.UnloadImage(img);
        }
        Save(sheet, "city_" + town);
    }

    // Jubilife City is the densest place there is: its middle by day and after dark
    Look("c_jubilife_middle", 150, 768);
    engine.Settings.TimeOfDay = TimeOfDay.Night;
    engine.ApplySettings(window: false);
    Look("c_jubilife_night", 150, 768);
    engine.Settings.TimeOfDay = TimeOfDay.Day;
    engine.ApplySettings(window: false);
    walker.SetPosition(150, 768, Direction.Up);
    Timing("jubilife city (imported)");
}

// ---------------------------------------------------------------- terrain lab

// A map made here to show every terrain feature (plan 01 · M3) in one place, since the areas open so far are
// nearly flat: a mountain with stairs, a rock face to climb and a river falling off it, a gorge with a bridge
// over the river and a boardwalk across it lower down, a sheet of ice, snow of every depth, a marsh, a beach,
// and a lawn fenced by ledges that face south, west and east.
Map BuildTerrainLab()
{
    const int W = 48, H = 34;
    var m = new Map(W, H) { Name = "TerrainLab", DisplayName = "Terrain Lab", Trees = TreeStyle.Pine };
    void Fill(int x0, int y0, int x1, int y1, TileType type, bool solid = false)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                m.SetGroundTile(x, y, type, solid);
    }
    void Raise(int x0, int y0, int x1, int y1, float height, float slopeX = 0f, float slopeZ = 0f)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                m.SetHeight(x, y, height, slopeX, slopeZ);
    }
    void Does(int x0, int y0, int x1, int y1, TileBehavior behaviour)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                m.SetBehaviour(x, y, behaviour);
    }

    Raise(0, 0, W - 1, H - 1, 0f);
    Fill(0, 0, W - 1, 0, TileType.Tree, true);
    Fill(0, H - 1, W - 1, H - 1, TileType.Tree, true);
    Fill(0, 0, 0, H - 1, TileType.Tree, true);
    Fill(W - 1, 0, W - 1, H - 1, TileType.Tree, true);

    // The mountain: a plateau three tiles up across the north, lawn and pines on top, bare rock along its rim
    Raise(1, 1, W - 2, 10, 3f);
    Fill(1, 9, W - 2, 10, TileType.Rock);
    Fill(3, 2, 5, 3, TileType.Tree, true);
    Fill(12, 2, 13, 2, TileType.Tree, true);
    Fill(28, 2, 31, 3, TileType.Tree, true);
    Fill(36, 4, 42, 7, TileType.TallGrass);
    Fill(16, 5, 19, 6, TileType.FlowerGrass);

    // Notches cut into the rim, with a spur of rock either side of each: stairs, a face to climb, and the river's fall
    void Notch(int x0, int x1)
    {
        Fill(x0 - 1, 11, x0 - 1, 13, TileType.Rock, true);
        Fill(x1 + 1, 11, x1 + 1, 13, TileType.Rock, true);
        Raise(x0 - 1, 11, x0 - 1, 13, 3f);
        Raise(x1 + 1, 11, x1 + 1, 13, 3f);
        for (int y = 11; y <= 13; y++) Raise(x0, y, x1, y, 2.5f - (y - 11), 0f, -1f);
    }
    Notch(8, 9);
    Fill(8, 11, 9, 13, TileType.Stairs);
    Notch(14, 15);
    Fill(14, 11, 15, 13, TileType.Rock, true);
    Does(14, 11, 15, 13, TileBehavior.RockClimbNorthSouth);

    // The river: along the plateau, down the fall, and on south through the lowland, half a tile under its banks
    Fill(22, 1, 24, 10, TileType.Water);
    Raise(22, 1, 24, 10, 2.5f);
    Notch(22, 24);
    Fill(22, 11, 24, 13, TileType.Water);
    Does(22, 11, 24, 13, TileBehavior.Waterfall);
    for (int y = 11; y <= 13; y++) Raise(22, y, 24, y, 2f - (y - 11), 0f, -1f);
    Fill(22, 14, 24, H - 2, TileType.Water);
    Raise(22, 14, 24, H - 2, -0.5f);

    // The gorge: a terrace of rock two tiles up on each bank, stairs up to each from the far side, a bridge between
    Fill(16, 16, 21, 19, TileType.Rock);
    Raise(16, 16, 21, 19, 2f);
    Fill(25, 16, 30, 19, TileType.Rock);
    Raise(25, 16, 30, 19, 2f);
    Fill(22, 17, 24, 18, TileType.Planks);
    Does(22, 17, 24, 18, TileBehavior.BridgeOverWater);
    for (int x = 22; x <= 24; x++)
        for (int y = 17; y <= 18; y++)
            m.SetDeck(x, y, 2f);
    Fill(14, 17, 15, 18, TileType.Stairs);
    Raise(14, 17, 14, 18, 0.5f, 1f);
    Raise(15, 17, 15, 18, 1.5f, 1f);
    Fill(31, 17, 32, 18, TileType.Stairs);
    Raise(31, 17, 31, 18, 1.5f, -1f);
    Raise(32, 17, 32, 18, 0.5f, -1f);
    m.NPCs.Add(new NPC { Name = "Hiker", NpcType = "Gentleman", GridX = 18, GridY = 17, Facing = Direction.Down });

    // A boardwalk across the river lower down, level with its banks
    Fill(21, 24, 25, 25, TileType.Planks);
    Does(22, 24, 24, 25, TileBehavior.BridgeOverWater);
    for (int x = 22; x <= 24; x++)
        for (int y = 24; y <= 25; y++)
            m.SetDeck(x, y, 0f);
    Fill(10, 20, 21, 20, TileType.Path);
    Fill(25, 20, 34, 20, TileType.Path);

    // A sheet of ice with two rocks on it
    Fill(3, 22, 10, 29, TileType.Ice);
    m.AddProp(PropType.Boulder, 6, 24);
    m.AddProp(PropType.Boulder, 8, 27);

    // Snow, deeper band by band toward the east
    Fill(12, 22, 19, 29, TileType.Snow);
    Does(14, 22, 15, 29, TileBehavior.DeepSnow);
    Does(16, 22, 17, 29, TileBehavior.DeeperSnow);
    Does(18, 22, 19, 29, TileBehavior.DeepestSnow);

    // A marsh, its grass and a patch of deep mud
    Fill(27, 22, 33, 28, TileType.Marsh);
    Fill(28, 23, 29, 25, TileType.TallGrass);
    Does(28, 23, 29, 25, TileBehavior.MarshGrass);
    Does(31, 25, 32, 27, TileBehavior.DeepMud);

    // A beach running down into a bay, with two puddles on it
    Fill(35, 24, 46, 32, TileType.Sand);
    Fill(37, 28, 46, 32, TileType.Water);
    Raise(37, 28, 46, 32, -0.5f);
    m.SetBehaviour(36, 25, TileBehavior.Puddle);
    m.SetBehaviour(41, 25, TileBehavior.Puddle);
    m.AddProp(PropType.Boulder, 43, 30);

    // A lawn with tall grass, fenced by ledges: hopped out of southward, westward and eastward, entered from the north
    Fill(38, 14, 42, 17, TileType.TallGrass);
    Fill(37, 14, 37, 17, TileType.LedgeLeft);
    Fill(43, 14, 43, 17, TileType.LedgeRight);
    Fill(38, 18, 42, 18, TileType.LedgeDown);
    m.Signboards[(35, 15)] = "Terrain Lab\nEvery kind of ground in one place.";
    m.SetGroundTile(35, 15, TileType.Signpost, isSolid: true);

    // The obstacles that field moves clear, in a row along the path
    m.AddProp(PropType.CutTree, 28, 20);
    m.AddProp(PropType.CrackedRock, 30, 20);
    m.AddProp(PropType.StrengthBoulder, 32, 20);
    return m;
}

// ---------------------------------------------------------------- life (plan 04 · G9)

// What moves in the field: prints in sand and snow, dust behind a run and under a hop, leaves out of tall grass,
// rings behind a swimmer, a door opening and shutting, the bubbles over heads, every weather, the camera sent
// to look elsewhere, and the three ways into a battle caught as they close and open.
if (Run("life"))
{
    var walker = (Player)Get("player");
    var sinnoh = MapDatabase.Get("Sinnoh");
    void Put(int x, int y, Direction facing, TravelMode travel = TravelMode.OnFoot)
    {
        Set("currentMap", sinnoh);
        walker.SetPosition(x, y, facing);
        walker.SetMode(travel);
        Set("currentState", GameState.Overworld);
        engine.Steering = (null, false);
        Frames(3);
    }
    // Walks the player with the game's own steps, so each one leaves what a step leaves
    void Walk(Direction way, int tiles, bool run = false)
    {
        int fromX = walker.GridX, fromY = walker.GridY;
        engine.Steering = (way, run);
        for (int guard = 0; guard < 900 && (Math.Abs(walker.GridX - fromX) + Math.Abs(walker.GridY - fromY) < tiles || walker.IsMoving); guard++) Frames(1);
        engine.Steering = (null, false);
    }
    (int X, int Y) Nearest(int x, int y, Func<int, int, bool> wanted)
    {
        for (int reach = 0; reach < 24; reach++)
            for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == reach && sinnoh.InBounds(x + dx, y + dy) && wanted(x + dx, y + dy)) return (x + dx, y + dy);
        throw new InvalidOperationException($"Nothing of the kind near ({x}, {y})");
    }

    // Prints along Route 219's beach, and in the snow before the rival's door
    var (sandX, sandY) = Nearest(184, 853, (x, y) => Enumerable.Range(0, 6).All(i => sinnoh.BehaviourAt(x + i, y) == TileBehavior.Sand && sinnoh.IsWalkable(x + i, y)));
    Put(sandX, sandY, Direction.Right); Walk(Direction.Right, 5); Shot("l01_prints_in_sand");
    ShotCrop("l01b_prints_native", 560, 380, 640, 360, 2);
    var (snowX, snowY) = Nearest(105, 877, (x, y) => Enumerable.Range(0, 4).All(i => TileBehaviors.KeepsFootprints(sinnoh.BehaviourAt(x + i, y)) && sinnoh.BehaviourAt(x + i, y) != TileBehavior.Sand && sinnoh.IsWalkable(x + i, y)));
    Put(snowX, snowY, Direction.Right); Walk(Direction.Right, 3); Shot("l02_prints_in_snow");

    // Dust behind a run up Twinleaf's road, caught two frames after a step
    Put(112, 884, Direction.Up); Walk(Direction.Up, 4, run: true); Frames(2); Shot("l03_dust_running");
    ShotCrop("l03b_dust_native", 640, 400, 640, 360, 2);

    // Leaves out of the tall grass of Route 201
    var (grassX, grassY) = Nearest(110, 850, (x, y) => sinnoh.IsTallGrass(x, y) && sinnoh.IsTallGrass(x + 1, y) && sinnoh.IsTallGrass(x + 2, y));
    Put(grassX, grassY, Direction.Right);
    engine.Steering = (Direction.Right, false);
    for (int guard = 0; guard < 60 && walker.GridX == grassX; guard++) Frames(1);
    engine.Steering = (null, false);
    Frames(5);
    if ((GameState)Get("currentState") == GameState.Overworld) { Shot("l04_leaves_from_grass"); ShotCrop("l04b_leaves_native", 640, 380, 640, 360, 2); }
    Set("currentState", GameState.Overworld);

    // A hop over a ledge of Route 201 onto bare ground: the dust where it lands
    Frames(40);
    var (ledgeX, ledgeY) = Nearest(120, 852, (x, y) => sinnoh.GetGroundTile(x, y) == TileType.LedgeDown && sinnoh.IsWalkable(x, y - 1) && sinnoh.IsWalkable(x, y + 1) && !sinnoh.IsTallGrass(x, y + 1));
    Put(ledgeX, ledgeY - 1, Direction.Down); Walk(Direction.Down, 2); Frames(2); Shot("l05_dust_under_a_hop");
    ShotCrop("l05b_hop_native", 640, 380, 640, 360, 2);

    // Rings behind a swimmer on Twinleaf's pond
    var (pondX, pondY) = Nearest(111, 892, (x, y) => Enumerable.Range(0, 4).All(i => sinnoh.IsDeepWater(x + i, y) && !sinnoh.IsSolid(x + i, y)));
    Put(pondX, pondY, Direction.Right, TravelMode.Surfing); Walk(Direction.Right, 3); Frames(4); Shot("l06_rings_behind_a_swimmer");
    ShotCrop("l06b_rings_native", 560, 380, 640, 360, 2);
    walker.SetMode(TravelMode.OnFoot);

    // Riding out onto the pond from its bank: the splash where the Pokémon lands
    var swimmer = ((Party)Get("playerParty")).Members[0];
    var surf = new Move(MoveDatabase.Get("Surf")!);
    swimmer.Moves.Add(surf);
    var (bankX, bankY) = Nearest(111, 892, (x, y) => sinnoh.IsWalkable(x, y) && sinnoh.IsDeepWater(x + 1, y) && sinnoh.IsDeepWater(x + 2, y) && !sinnoh.IsSolid(x + 1, y));
    Put(bankX, bankY, Direction.Right);
    T.GetMethod("TryStartSurf", Private)!.Invoke(engine, null);
    for (int guard = 0; guard < 120 && (walker.Mode != TravelMode.Surfing || walker.IsMoving); guard++) Frames(1);
    Frames(5); Shot("l06c_splash_riding_out"); ShotCrop("l06d_splash_native", 640, 380, 640, 360, 2);
    swimmer.Moves.Remove(surf);
    walker.SetMode(TravelMode.OnFoot);

    // A door: ajar as the step toward it begins, open as it ends, then from inside out again and shut behind
    void ThroughDoor(string name, int warpX, int warpY)
    {
        Put(warpX, warpY + 2, Direction.Up); Walk(Direction.Up, 1);
        engine.Steering = (Direction.Up, false);
        Frames(3); ShotCrop($"{name}_1_ajar", 640, 240, 640, 360, 2);
        Frames(6); ShotCrop($"{name}_2_open", 640, 240, 640, 360, 2);
        for (int guard = 0; guard < 120 && ((Map)Get("currentMap")).Name == "Sinnoh"; guard++) Frames(1);
        engine.Steering = (null, false);
        Frames(40);
        Console.WriteLine($"{name}: through the door into " + ((Map)Get("currentMap")).Name);
        engine.Steering = (Direction.Down, false);
        for (int guard = 0; guard < 400 && ((Map)Get("currentMap")).Name != "Sinnoh"; guard++) Frames(1);
        engine.Steering = (null, false);
        Frames(26); ShotCrop($"{name}_3_open_behind", 640, 240, 640, 360, 2);
        Frames(60); ShotCrop($"{name}_4_shut_again", 640, 240, 640, 360, 2);
    }
    ThroughDoor("l07_house_door", 116, 885);
    // The glass doors of Sandgem's Pokémon Center slide apart
    var center = MapStructures.BuildingsOf(sinnoh).First(b => b.Kind == BuildingKind.PokemonCenter && !b.Annex
        && b.Doors.Exists(d => sinnoh.GetWarpAt(d.X, b.Y1) != null || sinnoh.GetWarpAt(d.X, b.Y1 + 1) != null));
    int centerDoor = center.Doors[0].X;
    ThroughDoor("l08_center_door", centerDoor, sinnoh.GetWarpAt(centerDoor, center.Y1) != null ? center.Y1 : center.Y1 + 1);
    // The player's door after dark: the room's light in the doorway
    engine.Settings.TimeOfDay = TimeOfDay.Night;
    engine.ApplySettings(window: false);
    ThroughDoor("l09_house_door_at_night", 116, 885);
    engine.Settings.TimeOfDay = TimeOfDay.Day;
    engine.ApplySettings(window: false);

    // Every bubble, over a row of people and the player
    Set("currentMap", BuildFocusLineup());
    walker.SetPosition(10, 7, Direction.Down);
    Frames(3);
    var row = ((Map)Get("currentMap")).NPCs;
    var bubbles = new[] { EmoteBubble.Exclaim, EmoteBubble.Question, EmoteBubble.Dots, EmoteBubble.Note, EmoteBubble.Heart, EmoteBubble.Sleep, EmoteBubble.Sweat };
    for (int i = 0; i < bubbles.Length; i++) engine.ShowEmote(row[i].Name, bubbles[i], 5f);
    engine.ShowEmote(null, EmoteBubble.Exclaim, 5f);
    Frames(1); ShotCrop("l10_bubble_popping", 560, 240, 800, 360, 2);
    Frames(12); Shot("l11_bubbles"); ShotCrop("l11b_bubbles_native", 420, 250, 1000, 300, 2);
    engine.ShowEmote(null, EmoteBubble.None, 0f);

    // Every weather over Twinleaf Town, and rain after dark
    Put(112, 880, Direction.Down);
    var town = sinnoh.AreaAt(112, 880)!;
    foreach (var (name, kind) in new[]
             {
                 ("l12_rain", FieldWeather.Rain), ("l13_snow", FieldWeather.Snow), ("l14_heavy_snow", FieldWeather.HeavySnow),
                 ("l15_fog", FieldWeather.Fog), ("l16_sandstorm", FieldWeather.Sandstorm), ("l17_ash", FieldWeather.Ash),
                 ("l17b_cloudy", FieldWeather.Cloudy), ("l17c_heavy_rain", FieldWeather.HeavyRain), ("l17d_hail", FieldWeather.Hail),
                 ("l17e_blizzard", FieldWeather.Blizzard)
             })
    {
        town.Weather = kind;
        Frames(20); Shot(name);
    }
    // A thunderstorm, caught in the first flash of its lightning (the harness's frames are a sixtieth of a second)
    town.Weather = FieldWeather.Thunderstorm;
    for (int guard = 0; guard < 720 && !engine.LightningNow; guard++) Frames(1);
    Shot("l17f_thunderstorm_lightning");
    Frames(30); Shot("l17g_thunderstorm");
    town.Weather = FieldWeather.Rain;
    engine.Settings.TimeOfDay = TimeOfDay.Night;
    engine.ApplySettings(window: false);
    Frames(4); Shot("l18_rain_at_night");
    engine.Settings.TimeOfDay = TimeOfDay.Day;
    engine.ApplySettings(window: false);
    // Rain on the pond, seen from its north bank: rings where it lands on water, flecks on the ground
    var (shoreX, shoreY) = Nearest(111, 889, (x, y) => sinnoh.IsWalkable(x, y) && sinnoh.IsDeepWater(x, y + 1) && sinnoh.IsDeepWater(x, y + 2) && sinnoh.IsDeepWater(x + 1, y + 1));
    Put(shoreX, shoreY, Direction.Down);
    Frames(40); Shot("l19_rain_on_the_pond"); ShotCrop("l19b_rain_native", 640, 420, 640, 360, 2);
    Put(112, 880, Direction.Down);
    Timing("rain");
    town.Weather = FieldWeather.HeavySnow;
    Timing("heavy snow");
    town.Weather = FieldWeather.Fog;
    Timing("fog");
    town.Weather = FieldWeather.Clear;

    // The camera sent to look at the rival's house, half way and there, and back
    Put(112, 880, Direction.Down);
    engine.PanCamera(105, 876, 1f);
    Frames(30); Shot("l20_camera_half_way");
    Frames(40); Shot("l21_camera_there");
    engine.ReleaseCamera(0.5f);
    Frames(40);

    // Into battle: each way of closing caught in its flash, half closed and nearly shut, and the opening on the battle
    var start = T.GetMethod("StartTransition", Private)!;
    foreach (var (name, kind) in new[]
             {
                 ("wild", TransitionKind.Wild), ("wild_strong", TransitionKind.WildStrong), ("trainer", TransitionKind.Trainer),
                 ("trainer_strong", TransitionKind.TrainerStrong), ("leader", TransitionKind.Leader)
             })
    {
        Put(112, 880, Direction.Down);
        start.Invoke(engine, new object?[] { GameState.Overworld, null, kind });
        Frames(2); Shot($"l30_{name}_1_flash");
        Frames(36); Shot($"l30_{name}_2_closing");
        Frames(10); Shot($"l30_{name}_3_nearly_shut");
        Frames(18); Shot($"l30_{name}_4_opening");
        Frames(40);
    }
    // And the real thing: a wild Pokémon in the grass, from the flash to the battle's first frame
    Put(grassX, grassY, Direction.Right);
    T.GetMethod("StartWildBattle", Private)!.Invoke(engine, new object[] { new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3, Weight = 1 } });
    Frames(40); Shot("l31_into_battle_closing");
    Frames(26); Shot("l32_into_battle_opening");
    Frames(40); Shot("l33_battle_begins");
    Set("currentState", GameState.Overworld);

    // A waterfall of the terrain lab, twice an eighth of a second apart: the sheet has moved four texels down
    Set("currentMap", BuildTerrainLab());
    walker.SetPosition(20, 15, Direction.Up);
    walker.SetMode(TravelMode.OnFoot);
    Frames(4); ShotCrop("l40_waterfall_a", 760, 100, 640, 360, 2);
    Frames(8); ShotCrop("l40_waterfall_b", 760, 100, 640, 360, 2);

    // A fountain playing and a turbine turning, on a lawn made for them: their four frames, a sixth of a second apart
    var yard = new Map(22, 14) { Name = "Yard", DisplayName = "Yard" };
    for (int x = 0; x < 22; x++) { yard.SetGroundTile(x, 0, TileType.Tree, true); yard.SetGroundTile(x, 13, TileType.Tree, true); }
    for (int y = 0; y < 14; y++) { yard.SetGroundTile(0, y, TileType.Tree, true); yard.SetGroundTile(21, y, TileType.Tree, true); }
    yard.Props.Add(new Prop { Type = PropType.Fountain, X = 5, Y = 5, Width = 4, Depth = 3 });
    yard.Props.Add(new Prop { Type = PropType.WindTurbine, X = 13, Y = 6, Width = 2, Depth = 2, Height = 5f });
    Set("currentMap", yard);
    walker.SetPosition(10, 8, Direction.Up);
    Frames(5);
    for (int frame = 0; frame < 4; frame++)
    {
        Shot($"l41_fountain_and_turbine_{frame}");
        Frames(10);
    }

    Put(112, 880, Direction.Down);
    engine.Steering = null;
    Timing("twinleaf with life");
}

if (Run("lab"))
{
    var lab = BuildTerrainLab();
    var hiker = (Player)Get("player");
    void Stand(string name, int x, int y, Direction facing, TravelMode travel = TravelMode.OnFoot, float? height = null)
    {
        Set("currentMap", lab);
        hiker.SetPosition(x, y, facing);
        hiker.SetMode(travel);
        if (height is { } h) hiker.SetHeight(h);
        Set("currentState", GameState.Overworld);
        Frames(3);
        Shot(name);
    }

    Stand("lab01_mountain_stairs", 9, 15, Direction.Up);
    ShotCrop("lab01b_stairs_native", 600, 180, 640, 360, 2);
    Stand("lab02_plateau", 19, 7, Direction.Down);
    Stand("lab03_waterfall", 20, 15, Direction.Up);
    ShotCrop("lab03b_waterfall_native", 760, 100, 640, 360, 2);
    Stand("lab04_rock_climb", 13, 15, Direction.Up);
    Stand("lab05_bridge", 23, 17, Direction.Right, height: 2f);
    ShotCrop("lab05b_bridge_native", 640, 280, 640, 360, 2);
    Stand("lab06_under_the_bridge", 23, 21, Direction.Up, TravelMode.Surfing);
    Stand("lab07_gorge_stairs", 13, 18, Direction.Right);
    Stand("lab08_boardwalk", 23, 24, Direction.Right, height: 0f);
    Stand("lab09_ice", 6, 26, Direction.Down);
    Stand("lab10_snow", 15, 25, Direction.Right);
    ShotCrop("lab10b_snow_native", 640, 300, 640, 360, 2);
    Stand("lab11_marsh", 30, 24, Direction.Down);
    Stand("lab12_beach", 40, 26, Direction.Down);
    Stand("lab13_surfing", 40, 30, Direction.Left, TravelMode.Surfing);
    ShotCrop("lab13b_surfing_native", 640, 300, 640, 360, 2);
    Stand("lab14_ledges", 40, 16, Direction.Down);
    ShotCrop("lab14b_ledges_native", 640, 260, 640, 360, 2);
    Stand("lab15_obstacles", 29, 22, Direction.Up);
    ShotCrop("lab15b_obstacles_native", 640, 260, 640, 360, 2);

    engine.Settings.TimeOfDay = TimeOfDay.Night;
    engine.ApplySettings(window: false);
    Stand("lab20_mountain_night", 9, 15, Direction.Up);
    engine.Settings.TimeOfDay = TimeOfDay.Day;
    engine.ApplySettings(window: false);

    hiker.SetPosition(20, 15, Direction.Up);
    hiker.SetMode(TravelMode.OnFoot);
    Timing("terrain lab");
    hiker.SetMode(TravelMode.OnFoot);
}

if (Run("world"))
{
    // The first towns and routes as the imported world has them, with the places where one area runs into the next
    (string Name, string Map, int X, int Y, Direction Facing)[] places =
    {
        ("w01_twinleaf", "Sinnoh", 112, 880, Direction.Down), ("w02_twinleaf_home", "Sinnoh", 116, 886, Direction.Up),
        ("w03_twinleaf_pond", "Sinnoh", 111, 890, Direction.Down), ("w04_twinleaf_to_route201", "Sinnoh", 112, 864, Direction.Up),
        ("w05_route201_briefcase", "Sinnoh", 110, 855, Direction.Right), ("w06_route201_grass", "Sinnoh", 127, 853, Direction.Right),
        ("w07_route201_to_sandgem", "Sinnoh", 160, 845, Direction.Right), ("w08_sandgem", "Sinnoh", 178, 845, Direction.Up),
        ("w09_sandgem_lab", "Sinnoh", 168, 844, Direction.Up), ("w10_sandgem_beach", "Sinnoh", 184, 858, Direction.Down),
        ("w11_route219", "Sinnoh", 183, 868, Direction.Down), ("w12_sandgem_to_route202", "Sinnoh", 186, 832, Direction.Up),
        ("w13_route202", "Sinnoh", 174, 815, Direction.Up), ("w14_route202_north", "Sinnoh", 174, 802, Direction.Up),
        ("w15_lakefront", "Sinnoh", 81, 846, Direction.Up), ("w16_lake", "LakeVerity", 44, 46, Direction.Up),
        ("w17_lake_east", "LakeVerity", 51, 40, Direction.Left)
    };
    foreach (var (name, map, x, y, facing) in places)
    {
        At(map, x, y, facing); Frames(2); Shot(name);
        if (name is "w01_twinleaf" or "w08_sandgem") ShotCrop(name + "_native", 640, 300, 640, 360, 2);
    }

    engine.Settings.TimeOfDay = TimeOfDay.Night;
    engine.ApplySettings(window: false);
    At("Sinnoh", 112, 880, Direction.Down); Frames(2); Shot("w20_twinleaf_night");
    At("Sinnoh", 178, 845, Direction.Up); Frames(2); Shot("w21_sandgem_night");
    engine.Settings.TimeOfDay = TimeOfDay.Day;
    engine.ApplySettings(window: false);

    if (args.Length > 2)
        Boards(args[2], new[] { "w01_twinleaf", "w02_twinleaf_home", "w06_route201_grass", "w08_sandgem", "w09_sandgem_lab", "w13_route202", "w16_lake", "w20_twinleaf_night" });

    // The walk the plan asks for: from the player's door in Twinleaf Town to the top of Route 202, tile by tile
    // along the shortest way, every frame timed. A chunk that has to be waited for shows up as one long frame.
    if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        var path = WalkingPath(sinnoh, (116, 886), (174, 801));
        var renderer = (WorldRenderer)Get("world");
        At("Sinnoh", 116, 886, Direction.Up); Frames(30);
        renderer.ResetStreamingStats();
        double total = 0, worst = 0;
        (int X, int Y) worstAt = default;
        int frames = 0, mostChunks = 0;
        var watch = new System.Diagnostics.Stopwatch();
        foreach (var (x, y) in path)
        {
            ((Player)Get("player")).SetPosition(x, y, Direction.Up);
            // Eight tiles a second, the pace of running, with each frame held to a sixtieth of a second as the
            // screen's refresh holds it: the chunks ahead get the time to bake that they get in play
            for (int f = 0; f < 8; f++)
            {
                watch.Restart();
                FrameClock.Fixed = ++tick / 60.0;
                engine.Update(1f / 60f);
                engine.Draw();
                double ms = watch.Elapsed.TotalMilliseconds;
                total += ms; frames++;
                if (ms > worst) { worst = ms; worstAt = (x, y); }
                while (watch.Elapsed.TotalMilliseconds < 1000.0 / 60.0) Thread.Sleep(1);
            }
            mostChunks = Math.Max(mostChunks, renderer.LoadedChunks);
        }
        Console.WriteLine($"walk from Twinleaf Town to Route 202: {path.Count} tiles, {total / frames:F2} ms/frame, worst frame {worst:F1} ms at ({worstAt.X}, {worstAt.Y}), at most {mostChunks} chunks loaded");
        Console.WriteLine($"  streaming: {renderer.Streaming}");
    }

    foreach (var (label, map, x, y) in new[] { ("twinleaf", "Sinnoh", 112, 881), ("route 201", "Sinnoh", 127, 853), ("sandgem", "Sinnoh", 178, 845), ("route 202", "Sinnoh", 174, 815), ("lake verity", "LakeVerity", 44, 46) })
    {
        At(map, x, y, Direction.Up);
        Timing(label);
    }
}

// The shortest way on foot between two tiles (ledges are walked round, as they must be going north)
List<(int X, int Y)> WalkingPath(Map map, (int X, int Y) from, (int X, int Y) to)
{
    var came = new Dictionary<(int, int), (int, int)> { [from] = from };
    var queue = new Queue<(int X, int Y)>();
    queue.Enqueue(from);
    while (queue.Count > 0 && !came.ContainsKey(to))
    {
        var (x, y) = queue.Dequeue();
        foreach (var next in new[] { (x, y - 1), (x + 1, y), (x - 1, y), (x, y + 1) })
        {
            if (came.ContainsKey(next) || !(map.IsWalkable(next.Item1, next.Item2) || next == to)) continue;
            came[next] = (x, y);
            queue.Enqueue(next);
        }
    }
    if (!came.ContainsKey(to)) throw new InvalidOperationException($"No way on foot from {from} to {to}");
    var path = new List<(int X, int Y)>();
    for (var at = to; at != from; at = came[at]) path.Add(at);
    path.Reverse();
    return path;
}

// ---------------------------------------------------------------- where the time goes (plan 04 · G11)

// Not part of `all`: the heaviest scenes of the game, each timed and then taken apart by the profiler. Run it by
// itself, with nothing else running, and with SHOTS_WINDOW=3840x2160 for the game as it is full screen.
if (mode == "profile")
{
    Console.WriteLine($"window {Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}, preset {engine.Settings.Quality}, profiler waits for the GPU: {FrameProfiler.WaitsForGpu}");
    Console.WriteLine($"start-up: {startMs:F0} ms to initialise the engine");
    // Arriving somewhere new: its chunks baked and uploaded behind the fade
    var fieldRenderer = (WorldRenderer)Get("world");
    fieldRenderer.ResetStreamingStats();
    var arrival = System.Diagnostics.Stopwatch.StartNew();
    Set("currentMap", MapDatabase.Get("Sinnoh"));
    ((Player)Get("player")).SetPosition(178, 845, Direction.Down);
    Set("currentState", GameState.Overworld);
    Frames(1);
    double firstFrame = arrival.Elapsed.TotalMilliseconds;
    Frames(1);
    Console.WriteLine($"arriving in a town not seen yet: {firstFrame:F0} ms to its first frame, {arrival.Elapsed.TotalMilliseconds - firstFrame:F0} ms for the next");
    Console.WriteLine($"  streaming: {fieldRenderer.Streaming}");
    var presets = (Environment.GetEnvironmentVariable("SHOTS_PRESETS") ?? "High").Split(',');
    foreach (string preset in presets)
    {
        engine.Settings.Quality = Enum.Parse<GraphicsQuality>(preset);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        Console.WriteLine($"--- {preset}");

        At("Sinnoh", 112, 880, Direction.Down); Frames(40); Profile("twinleaf town");
        At("Sinnoh", 115, 854, Direction.Up); Frames(40); Profile("route 201");
        At("Sinnoh", 110, 850, Direction.Up); Frames(40); Profile("route 201, in the trees");
        At("Sinnoh", 178, 845, Direction.Down); Frames(40); Profile("sandgem town");
        At("Sinnoh", 174, 815, Direction.Up); Frames(40); Profile("route 202");
        At("LakeVerity", 44, 46, Direction.Up); Frames(40); Profile("lake verity");
        At("JubilifeCity", 20, 30, Direction.Up); Frames(40); Profile("jubilife city");
        At("PokemonCenter", 5, 6, Direction.Up); Frames(40); Profile("pokemon center");

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Sinnoh", 112, 880, Direction.Down); Frames(40); Profile("twinleaf town at night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        var town = MapDatabase.Get("Sinnoh").AreaAt(112, 880);
        town.Weather = FieldWeather.HeavyRain;
        At("Sinnoh", 112, 880, Direction.Down); Frames(40); Profile("twinleaf town in heavy rain");
        town.Weather = FieldWeather.Fog;
        Frames(40); Profile("twinleaf town in fog");
        town.Weather = FieldWeather.Clear;

        // A wild battle at its menu, then the scripted battle's heaviest moments, each held still
        party.HealAll();
        var wild = StartBattle("Shinx", 5);
        ToMainMenu(wild);
        Profile("battle, at the menu");

        var show = new Party();
        var lead = new Pokemon(PokemonDatabase.Get("Infernape")!, 60, new Random(3));
        show.Add(lead);
        var rival = new Trainer { Name = "Barry", TrainerClass = "Rival" };
        var torterra = new Pokemon(PokemonDatabase.Get("Torterra")!, 90, new Random(4));
        rival.Party.Add(torterra);
        Set("currentMap", MapDatabase.Get("Sinnoh"));
        ((BattleRenderer)Get("battleRenderer")).SetArena(BattleArena.Grass);
        var b = new BattleEngine(new BattleSetup
        {
            PlayerParty = show, Inventory = inventory, Pokedex = pokedex, Trainers = new List<Trainer> { rival }, Random = new Random(11)
        });
        Set("battle", b);
        Set("currentState", GameState.Battle);
        Skip(1.0); Profile("trainer battle, both trainers in the sweep", 120, frozen: true);
        Skip(1.05); b.ConfirmMessage(); Frames(1);
        Skip(1.4); b.ConfirmMessage(); Frames(1);
        Skip(0.85);
        for (int guard = 0; guard < 6 && b.HUD.MenuState == BattleMenuState.Message; guard++) { b.ConfirmMessage(); Frames(1); Skip(0.3); }
        Skip(0.6);
        foreach (string move in new[] { "Flamethrower", "Surf", "Earthquake", "Close Combat" })
        {
            Skip(1.0);
            lead.Moves.Clear();
            lead.Moves.Add(new Move(MoveDatabase.Get(move)!));
            torterra.Moves.Clear();
            torterra.Moves.Add(new Move(MoveDatabase.Get("Splash")!));
            lead.CurrentHP = lead.MaxHP;
            b.EnemyPokemon.CurrentHP = b.EnemyPokemon.MaxHP;
            b.EnemyPokemon.StatStages[StatType.Speed] = -6;
            b.SelectMove(0);
            Skip(0.12); Profile($"{move}, winding up", 120, frozen: true);
            Skip(0.16); Profile($"{move}, arriving", 120, frozen: true);
            Skip(0.12); Profile($"{move}, landing", 120, frozen: true);
            for (int guard = 0; guard < 12 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++) { b.ConfirmMessage(); Frames(1); Skip(0.35); }
        }

        // Two trainers and four Pokémon
        party.HealAll();
        var one = new Trainer { Name = "Ana", TrainerClass = "Youngster" };
        one.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 6));
        var two = new Trainer { Name = "Cal", TrainerClass = "Lass" };
        two.Party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 6));
        var d = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
            Trainers = new List<Trainer> { one, two }, WildPokemon = new List<Pokemon>(), Random = new Random(5)
        });
        ((BattleRenderer)Get("battleRenderer")).SetArena((Map)Get("currentMap"));
        Set("battle", d);
        Set("currentState", GameState.Battle);
        Skip(130 / 60.0); Profile("double battle, two trainers in the sweep", 120, frozen: true);
        d.ConfirmMessage(); Frames(1); Skip(70 / 60.0);
        d.ConfirmMessage(); Frames(1); Skip(70 / 60.0);
        for (int guard = 0; guard < 6 && d.HUD.MenuState == BattleMenuState.Message; guard++) { d.ConfirmMessage(); Frames(1); Skip(20 / 60.0); }
        Skip(0.8);
        Profile("double battle, at the menu");
        Set("currentState", GameState.Overworld);
    }
    engine.Settings.Quality = GraphicsQuality.High;
    engine.ApplySettings(window: false);
}

// ---------------------------------------------------------------- times of day

if (Run("times"))
{
    // Twinleaf, Route 201 and a battle at each of Platinum's five times of day, then each quality preset
    foreach (var time in new[] { TimeOfDay.Morning, TimeOfDay.Day, TimeOfDay.Twilight, TimeOfDay.Night, TimeOfDay.LateNight })
    {
        engine.Settings.TimeOfDay = time;
        engine.ApplySettings(window: false);
        string tag = time.ToString().ToLowerInvariant();
        GoTo("TwinleafTown", 11, 8, Direction.Down); Frames(2); Shot($"time_{tag}_twinleaf");
        GoTo("SandgemTown", 14, 8, Direction.Up); Frames(2); Shot($"time_{tag}_sandgem");
        var tb = StartBattle("Shinx", 5);
        ToMainMenu(tb);
        Shot($"time_{tag}_battle");
    }
    engine.Settings.TimeOfDay = TimeOfDay.Day;

    foreach (var quality in new[] { GraphicsQuality.Low, GraphicsQuality.Medium, GraphicsQuality.High })
    {
        engine.Settings.Quality = quality;
        engine.ApplySettings(window: false);
        string tag = quality.ToString().ToLowerInvariant();
        GoTo("TwinleafTown", 11, 8, Direction.Down); Frames(2); Shot($"quality_{tag}_twinleaf");
        Timing($"{tag} field");
        var qb = StartBattle("Shinx", 5);
        ToMainMenu(qb);
        Shot($"quality_{tag}_battle");
        Timing($"{tag} battle");
    }
}

// ---------------------------------------------------------------- Pokémon models (plan 04 · G7)

if (Run("pokemon"))
{
    var asm = typeof(GameEngine).Assembly;
    var context = Get("renderContext");
    var studio = asm.GetType("PokemonPlatinumEngine.Graphics.PokemonStudio")!.GetMethod("Strip", BindingFlags.Static | BindingFlags.Public)!;
    var handBuilt = (string[])asm.GetType("PokemonPlatinumEngine.Graphics.PokemonModels")!.GetField("Species", BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!;
    var backdrop = new Color(206, 218, 232, 255);
    Image Studio(string species, string clip, float[] times, float[] yaws, int w, int h, float zoom = 1.5f, float lookY = 0.5f, bool blink = false) =>
        (Image)studio.Invoke(null, new object[] { context, species, clip, times, yaws, w, h, zoom, lookY, blink })!;

    // Species named after the mode (pokemon Kricketot Kricketune …): only their turntables, one under another on a
    // board, for a quick look at models being sculpted
    var named = mode == "pokemon" ? args.Skip(2).ToArray() : Array.Empty<string>();
    if (named.Length > 0)
    {
        const int Box = 300;
        var board = Raylib.GenImageColor(4 * Box, named.Length * Box, backdrop);
        for (int i = 0; i < named.Length; i++)
        {
            var row = Studio(named[i], "idle", new[] { 0.4f }, new[] { -0.5f, 0.35f, MathF.PI / 2f, MathF.PI }, Box, Box);
            Raylib.ImageDraw(ref board, row, new Rectangle(0, 0, row.Width, row.Height), new Rectangle(0, i * Box, row.Width, row.Height), Color.White);
            Raylib.UnloadImage(row);
            Raylib.ImageDrawText(ref board, named[i], 6, i * Box + 6, 20, new Color(30, 30, 40, 255));
        }
        Save(board, "91_turntables");
    }

    // Turntables: front, three-quarter, side and back, for every model and each body plan's sample
    foreach (var species in named.Length > 0 ? Array.Empty<string>() : handBuilt.Append("Generic").Concat(new[] { "sample Serpent", "sample Fish", "sample Floating" }))
    {
        string name = "91_turntable_" + species.Replace("sample ", "sample_").ToLowerInvariant();
        if (Wanted(name)) Save(Studio(species, "idle", new[] { 0.4f }, new[] { -0.5f, 0.35f, MathF.PI / 2f, MathF.PI }, 300, 300), name);
    }

    // Clips: a row per clip, a column per moment, seen from three-quarters in front
    var clips = new (string Clip, float[] Times)[]
    {
        ("idle", new[] { 0f, 0.42f, 0.85f, 1.27f, 1.7f }),
        ("physical", new[] { 0.14f, 0.26f, 0.4f, 0.48f, 0.66f }),
        ("special", new[] { 0.2f, 0.36f, 0.48f, 0.56f, 0.76f }),
        ("status", new[] { 0.1f, 0.3f, 0.45f, 0.6f, 0.86f }),
        ("hit", new[] { 0.04f, 0.12f, 0.3f, 0.55f, 0.8f }),
        ("faint", new[] { 0.15f, 0.35f, 0.55f, 0.75f, 1f }),
        ("entry", new[] { 0.3f, 0.4f, 0.55f, 0.72f, 0.9f })
    };
    foreach (var species in named.Length > 0 ? Array.Empty<string>() : new[] { "Riolu", "Chimchar", "Shinx", "Turtwig", "Starly", "Garchomp", "Giratina", "sample Serpent", "sample Fish", "sample Floating" })
    {
        string name = "92_clips_" + species.Replace("sample ", "").ToLowerInvariant();
        if (!Wanted(name)) continue;
        const int Box = 200;
        var board = Raylib.GenImageColor(5 * Box, clips.Length * Box, backdrop);
        for (int r = 0; r < clips.Length; r++)
        {
            var row = Studio(species, clips[r].Clip, clips[r].Times, new[] { -0.6f }, Box, Box, 1.7f, 0.42f);
            Raylib.ImageDraw(ref board, row, new Rectangle(0, 0, row.Width, row.Height), new Rectangle(0, r * Box, row.Width, row.Height), Color.White);
            Raylib.UnloadImage(row);
        }
        Save(board, name);
    }

    // Generated models (plan 03 · D5): the first species of each body kind, in Pokédex order, that isn't hand-built,
    // turned round
    var genomeOf = asm.GetType("PokemonPlatinumEngine.Graphics.PokemonGenomes")!.GetMethod("For", BindingFlags.Static | BindingFlags.Public)!;
    var firstOfKind = new Dictionary<string, string>();
    foreach (var sp in named.Length > 0 ? Array.Empty<string>() : PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => s.Name))
    {
        if (handBuilt.Contains(sp, StringComparer.OrdinalIgnoreCase) || genomeOf.Invoke(null, new object[] { sp }) is not { } genome) continue;
        string kind = genome.GetType().GetField("Kind")!.GetValue(genome)!.ToString()!;
        firstOfKind.TryAdd(kind, sp);
    }
    foreach (var species in firstOfKind.Values)
    {
        string name = "91_turntable_gen_" + species.ToLowerInvariant();
        if (Wanted(name)) Save(Studio(species, "idle", new[] { 0.4f }, new[] { -0.5f, 0.35f, MathF.PI / 2f, MathF.PI }, 300, 300), name);
    }

    // A model brought in from a glTF file: Riolu's own model written out, dropped into overrides/models and read back,
    // turned round and playing the clips the file carries
    if (named.Length == 0 && Wanted("imported"))
    {
        object Call(string type, string method, params object[] a) =>
            asm.GetType("PokemonPlatinumEngine.Graphics." + type)!.GetMethod(method, BindingFlags.Static | BindingFlags.Public)!.Invoke(null, a)!;
        string folder = Path.Combine(outDir, "overrides", "models");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "Riolu.glb");
        Call("GltfWriter", "Save", Call("PokemonModels", "Get", "Riolu"), file);
        void Swap()
        {
            Call("ModelOverrides", "Refresh");
            Call("PokemonModels", "ForgetSignatures");
            Call("PokemonModels", "Release", "Riolu");
        }
        Swap();
        Save(Studio("Riolu", "idle", new[] { 0.4f }, new[] { -0.5f, 0.35f, MathF.PI / 2f, MathF.PI }, 300, 300), "91_turntable_imported_riolu");
        const int Box = 200;
        var board = Raylib.GenImageColor(5 * Box, clips.Length * Box, backdrop);
        for (int r = 0; r < clips.Length; r++)
        {
            var row = Studio("Riolu", clips[r].Clip, clips[r].Times, new[] { -0.6f }, Box, Box, 1.7f, 0.42f);
            Raylib.ImageDraw(ref board, row, new Rectangle(0, 0, row.Width, row.Height), new Rectangle(0, r * Box, row.Width, row.Height), Color.White);
            Raylib.UnloadImage(row);
        }
        Save(board, "92_clips_imported_riolu");
        File.Delete(file);
        Swap();
    }

    // Eyes up close: open, blinking, squeezed by a hit, fierce in an attack
    foreach (var (species, lookY) in named.Length > 0 ? Array.Empty<(string, float)>() : new[] { ("Piplup", 0.74f), ("Riolu", 0.66f), ("Turtwig", 0.6f), ("Luxray", 0.7f), ("Starly", 0.78f), ("Gible", 0.68f), ("Chimchar", 0.75f), ("Garchomp", 0.84f) })
    {
        string name = "93_eyes_" + species.ToLowerInvariant();
        if (!Wanted(name)) continue;
        const int Box = 260;
        var board = Raylib.GenImageColor(4 * Box, Box, backdrop);
        var frames = new[] { Studio(species, "idle", new[] { 0.4f }, new[] { 0f }, Box, Box, 0.6f, lookY), Studio(species, "idle", new[] { 0.4f }, new[] { 0f }, Box, Box, 0.6f, lookY, true),
            Studio(species, "hit", new[] { 0.025f }, new[] { 0f }, Box, Box, 0.6f, lookY), Studio(species, "physical", new[] { 0.07f }, new[] { 0f }, Box, Box, 0.6f, lookY) };
        for (int i = 0; i < frames.Length; i++)
        {
            Raylib.ImageDraw(ref board, frames[i], new Rectangle(0, 0, Box, Box), new Rectangle(i * Box, 0, Box, Box), Color.White);
            Raylib.UnloadImage(frames[i]);
        }
        Save(board, name);
    }
}

// ---------------------------------------------------------------- every species' model (plan 03 · D5)

// Boards of sixty models each in Pokédex order, seen from three-quarters in front, or only the species named after
// the mode. Not part of "all": the first run meshes every species (cached afterwards in cache/models)
if (mode == "dex")
{
    var asm = typeof(GameEngine).Assembly;
    var context = Get("renderContext");
    var studio = asm.GetType("PokemonPlatinumEngine.Graphics.PokemonStudio")!.GetMethod("Strip", BindingFlags.Static | BindingFlags.Public)!;
    var release = asm.GetType("PokemonPlatinumEngine.Graphics.PokemonModels")!.GetMethod("Release", BindingFlags.Static | BindingFlags.Public)!;
    // --back: from behind, as the player's own Pokémon is seen in battle
    bool back = args.Skip(2).Contains("--back");
    var named = args.Skip(2).Where(a => a != "--back").ToArray();
    var list = named.Length > 0 ? named : PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => s.Name).ToArray();
    const int Cols = 10, Rows = 6, Box = 192;
    var watch = System.Diagnostics.Stopwatch.StartNew();
    for (int page = 0; page * Cols * Rows < list.Length; page++)
    {
        string name = back ? $"94_dex_back_{page + 1:00}" : $"94_dex_{page + 1:00}";
        if (!Wanted(name)) continue;
        var board = Raylib.GenImageColor(Cols * Box, Rows * Box, new Color(206, 218, 232, 255));
        for (int i = 0; i < Cols * Rows && page * Cols * Rows + i < list.Length; i++)
        {
            string species = list[page * Cols * Rows + i];
            var view = (Image)studio.Invoke(null, new object[] { context, species, "idle", new[] { 0.4f }, new[] { back ? 2.6f : -0.55f }, Box, Box, 1.3f, 0.5f, false })!;
            int x = i % Cols * Box, y = i / Cols * Box;
            Raylib.ImageDraw(ref board, view, new Rectangle(0, 0, Box, Box), new Rectangle(x, y, Box, Box), Color.White);
            Raylib.UnloadImage(view);
            string label = PokemonDatabase.Get(species) is { } data ? $"{data.DexNumber} {species}" : species;
            Raylib.ImageDrawText(ref board, label, x + 4, y + Box - 16, 10, new Color(30, 30, 40, 255));
            release.Invoke(null, new object[] { species });
        }
        Save(board, name);
        Console.WriteLine($"  {Math.Min(list.Length, (page + 1) * Cols * Rows)} of {list.Length} species in {watch.Elapsed.TotalSeconds:F0} s");
    }
}

// ---------------------------------------------------------------- species in battle (plan 03 · D6)

// versus <mine> <foe> [<mine> <foe> ...]: a wild battle for each pair, the first species (or form) leading the player's
// party and the second met in the wild, shot at the main menu (95_versus_<mine>_<foe>). Not part of "all"
if (mode == "versus")
{
    var names = args.Skip(2).ToArray();
    for (int i = 0; i + 1 < names.Length; i += 2)
    {
        var mine = Meet(names[i], 20);
        party.Members.Insert(0, mine);
        var vb = StartBattle(names[i + 1], 20);
        ToMainMenu(vb);
        Shot($"95_versus_{names[i].ToLowerInvariant()}_{names[i + 1].ToLowerInvariant()}");
        party.Members.Remove(mine);
        Set("currentState", GameState.Overworld);
    }
}

// ---------------------------------------------------------------- conditions and the field (plan 06 · R3)

// conditions: what a battle's passing states look like. A Pokémon that has flown up is off its platform while its
// HP box stays (96_flight_away), and is back as it strikes (96_flight_strike, 96_flight_back). Not part of "all"
if (mode == "conditions")
{
    var flyer = new Pokemon(PokemonDatabase.Get("Staravia")!, 30);
    flyer.Moves.Clear();
    flyer.Moves.Add(new Move(MoveDatabase.Get("Fly")));
    party.Members.Insert(0, flyer);
    var cb = StartBattle("Bidoof", 12);
    ToMainMenu(cb);
    cb.SelectMove(0);

    // Reads on to a line and lets what it starts play for a moment
    void ReadTo(string line)
    {
        for (int i = 0; i < 40 && cb.CurrentMessage != line; i++)
        {
            Confirm(cb);
            Skip(0.3);
        }
        Skip(0.6);
    }

    ReadTo("Staravia flew up high!");
    Shot("96_flight_away");
    ReadTo("Staravia used Fly!");
    Shot("96_flight_strike");
    Skip(1.6);
    Shot("96_flight_back");
    party.Members.Remove(flyer);
    Set("currentState", GameState.Overworld);

    // A move of several hits (plan 06 · R4): the hits land one after another and the count is told after the last
    // (96_hits_landing, 96_hits_told). The count is fixed at five, on a foe that can take them
    var pecker = new Pokemon(PokemonDatabase.Get("Staravia")!, 30);
    pecker.Moves.Clear();
    pecker.Moves.Add(new Move(MoveDatabase.Get("Fury Attack")));
    party.Members.Insert(0, pecker);
    cb = StartBattle("Bidoof", 30, chance: new BattleRandom(7).Force(RollKind.HitCount, 2, 3));
    ToMainMenu(cb);
    cb.SelectMove(0);
    ReadTo("Staravia used Fury Attack!");
    Shot("96_hits_landing");
    for (int i = 0; i < 40 && !cb.CurrentMessage.StartsWith("Hit "); i++)
    {
        Confirm(cb);
        Skip(0.3);
    }
    Skip(0.6);
    Shot("96_hits_told");
    party.Members.Remove(pecker);
    Set("currentState", GameState.Overworld);

    // Transform (plan 06 · R5): Ditto takes the foe's shape on the field as it does in the rules (96_transformed)
    var ditto = new Pokemon(PokemonDatabase.Get("Ditto")!, 30);
    ditto.Moves.Clear();
    ditto.Moves.Add(new Move(MoveDatabase.Get("Transform")));
    party.Members.Insert(0, ditto);
    cb = StartBattle("Bidoof", 12);
    ToMainMenu(cb);
    cb.SelectMove(0);
    ReadTo("Ditto transformed into Foe Bidoof!");
    Skip(1.0);
    Shot("96_transformed");
    party.Members.Remove(ditto);
    Set("currentState", GameState.Overworld);
}

// ---------------------------------------------------------------- contact sheets

if (Run("sheets"))
{
    // Every species: front sprite, back sprite and menu icon, as baked from the 3D models
    var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => s.Name).Where(PixelArtGenerator.HasOwnModel).ToArray();
    // Only the preloaded ones are baked at start-up: ask for the rest and wait for them
    var spriteType = typeof(GameEngine).Assembly.GetType("PokemonPlatinumEngine.Graphics.PokemonSprites")!;
    foreach (var sp in species.Take(24)) spriteType.GetMethod("Request")!.Invoke(null, new object[] { sp });
    spriteType.GetMethod("Flush")!.Invoke(null, new[] { Get("renderContext") });
    var sheet = Raylib.LoadRenderTexture(1920, 1080);
    Raylib.BeginTextureMode(sheet);
    Raylib.ClearBackground(new Color(200, 220, 240, 255));
    for (int i = 0; i < species.Length && i < 24; i++)
    {
        int x = i % 8 * 240, y = i / 8 * 360;
        Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonSprite(species[i], false), new Vector2(x, y), 0, 1.5f, Color.White);
        Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonSprite(species[i], true), new Vector2(x + 40, y + 180), 0, 1f, Color.White);
        Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonIcon(species[i]), new Vector2(x + 180, y + 190), 0, 1f, Color.White);
        Raylib.DrawText(species[i], x + 4, y + 330, 20, Color.Black);
    }
    Raylib.EndTextureMode();
    var img = Raylib.LoadImageFromTexture(sheet.Texture);
    Raylib.ImageFlipVertical(ref img);
    Save(img, "90_pokemon_sheet");
    Raylib.UnloadRenderTexture(sheet);

    // On its own, the mode goes on through every species in Pokédex order, sixty to a page: the front sprite with the
    // icon beside it (plan 03 · D5). The first run bakes them all (and meshes their models), later runs read the cache
    if (mode == "sheets")
    {
        var asm = typeof(GameEngine).Assembly;
        var sprites = asm.GetType("PokemonPlatinumEngine.Graphics.PokemonSprites")!;
        var models = asm.GetType("PokemonPlatinumEngine.Graphics.PokemonModels")!;
        var context = Get("renderContext");
        var everyone = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToArray();
        const int Cols = 12, Rows = 5, W = 160, H = 216;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int page = 0; page * Cols * Rows < everyone.Length; page++)
        {
            string name = $"90_pokemon_sheet_{page + 1:00}";
            if (!Wanted(name)) continue;
            var onPage = everyone.Skip(page * Cols * Rows).Take(Cols * Rows).ToArray();
            foreach (var sp in onPage) sprites.GetMethod("Request")!.Invoke(null, new object[] { sp.Name });
            sprites.GetMethod("Flush")!.Invoke(null, new[] { context });
            var target = Raylib.LoadRenderTexture(Cols * W, Rows * H);
            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(new Color(200, 220, 240, 255));
            for (int i = 0; i < onPage.Length; i++)
            {
                int x = i % Cols * W, y = i / Cols * H;
                Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonSprite(onPage[i].Name, false), new Vector2(x + 4, y + 4), 0, 1f, Color.White);
                Raylib.DrawTextureEx(PixelArtGenerator.GetPokemonIcon(onPage[i].Name), new Vector2(x + 108, y + 140), 0, 1f, Color.White);
                Raylib.DrawText($"{onPage[i].DexNumber} {onPage[i].Name}", x + 4, y + 192, 10, Color.Black);
            }
            Raylib.EndTextureMode();
            var pageImage = Raylib.LoadImageFromTexture(target.Texture);
            Raylib.ImageFlipVertical(ref pageImage);
            Save(pageImage, name);
            Raylib.UnloadRenderTexture(target);
            foreach (var sp in onPage) models.GetMethod("Release")!.Invoke(null, new object[] { sp.Name });
            Console.WriteLine($"  {page * Cols * Rows + onPage.Length} of {everyone.Length} species in {watch.Elapsed.TotalSeconds:F0} s");
        }
    }
}

// ---------------------------------------------------------------- model files

// Writes species' models as .glb files into <out dir>/models, to refine in a 3D editor and drop into overrides/models
// (the hand-built ones when no species are named)
if (mode == "export")
{
    var asm = typeof(GameEngine).Assembly;
    var models = asm.GetType("PokemonPlatinumEngine.Graphics.PokemonModels")!;
    var writer = asm.GetType("PokemonPlatinumEngine.Graphics.GltfWriter")!;
    var named = args.Skip(2).ToArray();
    var list = named.Length > 0 ? named : (string[])models.GetField("Species", BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!;
    string folder = Path.Combine(outDir, "models");
    Directory.CreateDirectory(folder);
    foreach (var species in list)
    {
        var model = models.GetMethod("Get")!.Invoke(null, new object[] { species })!;
        string file = Path.Combine(folder, species + ".glb");
        writer.GetMethod("Save")!.Invoke(null, new[] { model, file });
        Console.WriteLine($"wrote models/{species}.glb ({new FileInfo(file).Length / 1024} KB)");
    }
}

engine.Close();
Raylib.CloseWindow();
