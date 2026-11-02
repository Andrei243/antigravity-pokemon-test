// The room and overlay editor (plan 16 · T9). See tools/RoomEditor/README.md.
//
//   dotnet run --project tools/RoomEditor -- <Room>                          edit Data/maps/<Room>.json of the repository
//   dotnet run --project tools/RoomEditor -- --new <Room> <width> <height>   a new room (floor in a ring of wall), saved on Ctrl+S
//   ... --area <key>                                                         and an area's overlay on the second tab (twinleaf_town)
//   ... --data <dir>                                                         another folder laid out like Data/
//   ... --shot <out dir>                                                     each tab drawn once in a hidden window (editor_*.png), then out
//   dotnet run --project tools/RoomEditor -- --roundtrip                     no window: every room and overlay opened and written back, what differs told (exit 1)
//   dotnet run --project tools/RoomEditor -- --check <file>                  the same of one room's or overlay's file

using System.Globalization;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using RoomEditor;

string? data = null, shot = null, area = null, name = null, check = null;
bool roundTrip = false;
(string Name, int W, int H)? fresh = null;
for (int i = 0; i < args.Length; i++)
{
    string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
    try
    {
        switch (args[i])
        {
            case "--data": data = Value(); break;
            case "--shot": shot = Value(); break;
            case "--area": area = Value(); break;
            case "--check": check = Value(); break;
            case "--roundtrip": roundTrip = true; break;
            case "--new": fresh = (Value(), int.Parse(Value(), CultureInfo.InvariantCulture), int.Parse(Value(), CultureInfo.InvariantCulture)); break;
            default: name = args[i]; break;
        }
    }
    catch (Exception e) when (e is ArgumentException or FormatException)
    {
        Console.Error.WriteLine(e.Message);
        return 2;
    }
}

// The repository's data folder, where a room is written to be committed; the copy beside the program otherwise
data ??= DevMode.FindRepositoryData(AppContext.BaseDirectory) ?? GameDataFiles.Directory;
if (!Directory.Exists(data))
{
    Console.Error.WriteLine($"No data folder at {data}");
    return 2;
}

// No window: every room and overlay (or one file) opened and written back as the editor does, and what differs told
if (roundTrip || check != null)
{
    var (files, found) = check != null ? (1, RoundTrip.One(data, check)) : RoundTrip.All(data);
    foreach (string line in found) Console.WriteLine(line);
    Console.WriteLine($"{files} file{(files == 1 ? "" : "s")} opened and written back: {found.Count} difference{(found.Count == 1 ? "" : "s")}");
    return found.Count == 0 ? 0 : 1;
}

EditorDocument room;
if (fresh is { } f) room = EditorDocument.New(f.Name, f.W, f.H);
// An area alone: its overlay and its view, beside an empty room nothing saves unless it is drawn
else if (name == null && area != null) room = EditorDocument.New("Untitled", 10, 8);
else if (name != null)
{
    string path = Path.Combine(data, MapDatabase.Folder, name + ".json");
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"No room at {path}: give a room's name, or --new <Room> <width> <height>");
        return 2;
    }
    room = EditorDocument.Open(File.ReadAllText(path));
}
else
{
    Console.Error.WriteLine("Name a room (PokemonCenter), or --new <Room> <width> <height>; --area <key> adds an overlay, --shot <dir> draws the tabs.");
    return 2;
}

OverlayDocument? overlay = null;
string? overlayPath = null;
if (area != null)
{
    var world = EditorAreas.Find(data, area);
    if (world == null)
    {
        Console.Error.WriteLine($"No region of {data} has the area '{area}'");
        return 2;
    }
    overlayPath = world.Value.OverlayPath;
    overlay = new OverlayDocument(area, world.Value.Area, File.Exists(overlayPath) ? File.ReadAllText(overlayPath) : null);
}

var window = new EditorWindow(room, data, overlay, overlayPath);
if (shot != null) window.Shots(shot);
else window.Run();
return 0;
