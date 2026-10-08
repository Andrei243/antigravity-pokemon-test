// Screenshot harness: runs the game in a hidden 1920x1080 window, drives it into known states and saves PNGs of
// the virtual screen, so graphics changes can be checked without playing.
//
//   dotnet run --project tools/ShotHarness -- <output dir> [all|field|lineup|battle|doubles|demo|arenas|flow|menus|evolution|look|title|intro|terrain|buildings|lab|life|world|times|sheets|pokemon] [before dir]
//   dotnet run --project tools/ShotHarness -- <output dir> area <key>      one shot of an area of the imported world, by its key (twinleaf_town)
//   dotnet run --project tools/ShotHarness -- <output dir> cities [key ...]   the buildings of every town of Sinnoh, from the importer's last full run
//   dotnet run --project tools/ShotHarness -- <output dir> dex [--back] [species ...]   boards of every species' 3D model, sixty to a page (--back: from behind)
//   dotnet run --project tools/ShotHarness -- <output dir> export [species ...]   species' models as .glb files, to edit and drop into overrides/models
//   dotnet run --project tools/ShotHarness -- <output dir> profile            where a frame goes: the heavy scenes timed, then taken apart pass by pass
//   dotnet run --project tools/ShotHarness -- <output dir> opening            the first chapter's scenes, played by its own scripts
//   dotnet run --project tools/ShotHarness -- <output dir> encounters         wild Pokémon beyond the tables: the Poké Radar, a honey tree, poison, swarms, a roamer, Feebas
//   dotnet run --project tools/ShotHarness -- <output dir> eterna             the third chapter's second half: Eterna City, Team Galactic's building, the Bicycle
//   dotnet run --project tools/ShotHarness -- <output dir> distortion         the Distortion World's floors and the north's last landmarks (the end of `world`)
//   dotnet run --project tools/ShotHarness -- <dir> diff <other dir>          two runs' shots compared pixel by pixel
//   dotnet run --project tools/ShotHarness -- <dir> contact [prefix]          every shot of a run on sheets of twenty
//   dotnet run --project tools/ShotHarness -- <dir> crop <shot> <x> <y> <width> <height> <scale> [other dir ...]   a rectangle of a shot, enlarged
//
// Two runs draw the same pictures (the game's chance is seeded and its clock counted in frames), so `diff` shows
// exactly what a change did. This file keeps the arguments, the window, the seeds and the order of `all`; Harness.cs
// holds the game and the helpers, and Modes/ a file for each mode. The harness reaches the engine through its Driver
// (GameEngine.Drive) and its internal classes directly (InternalsVisibleTo), never by reflection. The output directory
// becomes the working directory, which keeps the game from loading or writing savegame.json there.

string startDir = Environment.CurrentDirectory;
string outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "shots");
string mode = args.Length > 1 ? args[1] : "all";

if (mode != "crop")
{
    Directory.CreateDirectory(outDir);
    Environment.CurrentDirectory = outDir;
}

// The tools that read the shots of earlier runs start no game
if (mode == "contact") { Compare.Contact(args, outDir); return; }
if (mode == "crop") { Compare.Crop(args, startDir); return; }
if (mode == "diff") { Compare.Diff(args, outDir, startDir); return; }

Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
// SHOTS_WINDOW=3840x2160 makes the hidden window that size: the game then draws as it does full screen on a 4K
// display (FXAA on the high preset, the screen shown one to one), which is what the timings should be read at
var windowSize = (Environment.GetEnvironmentVariable("SHOTS_WINDOW") ?? "1920x1080").Split('x');
Raylib.InitWindow(int.Parse(windowSize[0]), int.Parse(windowSize[1]), "shots");

// Two runs draw the same pictures: the game's chance is seeded, the clock its small motions keep time by is the
// harness's own count of frames, and the day is fixed (the weather calendar and Pokérus's days follow the date): the
// first of June, when it rains on Route 212's south and Route 213 is clear
Dice.Seed(20261004);
FrameClock.Fixed = 0;
GameClock.FixedDate = new DateTime(2026, 6, 1);

var h = new Harness(args, mode, outDir, startDir);

// The modes, in the order `all` runs them (those that test `mode ==` are not part of `all`)
if (h.Run("field")) h.FieldMode();
if (h.Run("lineup")) h.LineupMode();
if (h.Run("battle")) h.BattleMode();
if (h.Run("doubles")) h.DoublesMode();
if (h.Run("demo")) h.DemoMode();
if (h.Run("arenas")) h.ArenasMode();
if (h.Run("flow")) h.FlowMode();
if (h.Run("menus")) h.MenusMode();
if (h.Run("evolution")) h.EvolutionMode();
if (h.Run("look")) h.LookMode();
if (h.Run("intro")) h.IntroMode();
if (h.Run("title")) h.TitleMode();
if (h.Run("terrain")) h.TerrainMode();
if (h.Run("buildings")) h.BuildingsMode();
if (mode == "rooms") h.RoomsMode();
if (mode == "gyms") h.GymsMode();
if (mode == "area") h.AreaMode();
if (mode == "cities") h.CitiesMode();
if (h.Run("life")) h.LifeMode();
if (h.Run("lab")) h.LabMode();
if (h.Run("world")) h.WorldMode();
if (mode == "distortion") h.DistortionMode();
if (mode == "profile") h.ProfileMode();
if (h.Run("times")) h.TimesMode();
if (h.Run("pokemon")) h.PokemonMode();
if (mode == "dex") h.DexMode();
if (mode == "versus") h.VersusMode();
if (mode == "conditions") h.ConditionsMode();
if (h.Run("sheets")) h.SheetsMode();
if (h.Run("story")) h.StoryMode();
if (mode == "fieldmoves") h.FieldMovesMode();
if (mode == "encounters") h.EncountersMode();
if (mode == "jubilife") h.JubilifeMode();
if (mode == "windworks") h.WindworksMode();
if (mode == "eterna") h.EternaMode();
if (mode == "opening") h.OpeningMode();
if (mode == "boutique") h.BoutiqueMode();
if (mode == "export") h.ExportMode();

h.Close();
Raylib.CloseWindow();
