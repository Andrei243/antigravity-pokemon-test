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
//   dotnet run --project tools/ShotHarness -- <output dir> berries            berry patches in each stage, a patch picked, and the Lottery Corner
//   dotnet run --project tools/ShotHarness -- <output dir> poffins            the Poffin House's pot at each stage, the Poffin Case, a Poffin fed, the condition
//   dotnet run --project tools/ShotHarness -- <output dir> daycare            the Day Care and its couple, an Egg found, on the team and hatching, Cynthia's Egg
//   dotnet run --project tools/ShotHarness -- <output dir> contests           a Super Contest from the counter to the ribbon: the dressing-up, the dance, the acting, the results; a practice round
//   dotnet run --project tools/ShotHarness -- <output dir> capsules           Seals and ribbons: the Seal Case's house, the market's stall, the Ball Capsules, their burst in battle, the ribbons' page, the Fashion Case
//   dotnet run --project tools/ShotHarness -- <output dir> eterna             the third chapter's second half: Eterna City, Team Galactic's building, the Bicycle
//   dotnet run --project tools/ShotHarness -- <output dir> veilstone          the fifth chapter: Veilstone's warehouse and HM02, Pastoria's bomb, the chase to Valor Lakefront
//   dotnet run --project tools/ShotHarness -- <output dir> celestic           the sixth chapter: the Psyduck and the Old Charm, Celestic's ruins, Cyrus and HM03, the sea, the rival in Canalave
//   dotnet run --project tools/ShotHarness -- <output dir> distortion         the Distortion World's floors and the north's last landmarks (the end of `world`)
//   dotnet run --project tools/ShotHarness -- <output dir> league [room ...]  the Pokémon League's rooms: the guard, the lifts, the Elite Four, Cynthia, the Hall of Fame
//   dotnet run --project tools/ShotHarness -- <output dir> lakes              the seventh chapter: Riley and Iron Island, Canalave's library and the explosion, Saturn at Lake Valor, Mars at Lake Verity, the way north
//   dotnet run --project tools/ShotHarness -- <output dir> battlezone [Map,x,y[,night] ...]   the Battle Zone's towns and routes and Route 224 (the end of `world`); given places, a shot of each instead
//   dotnet run --project tools/ShotHarness -- <output dir> snowpoint          the eighth chapter's first half: the rival at Acuity Lakefront, Snowpoint City, Maylene, Candice's Badge, Rock Climb, Jupiter at Lake Acuity
//   dotnet run --project tools/ShotHarness -- <output dir> headquarters       the eighth chapter's second half: the Storage Key, the warehouse's door, Team Galactic's headquarters floor by floor, Cyrus, Saturn and the lakes' Pokémon set free
//   dotnet run --project tools/ShotHarness -- <output dir> underground        the Underground: the way down, a wall dug through, every trap, a Sphere buried, the hikers, the secret base and its decorating, a flag brought home
//   dotnet run --project tools/ShotHarness -- <output dir> pillar             the ninth chapter: the climb through Mt. Coronet, Spear Pillar, the torn pillar, the Distortion World floor by floor, Giratina, Sendoff Spring
//   dotnet run --project tools/ShotHarness -- <output dir> pastimes [part ...]  fossils, the Game Corner, the Villa and Pal Park (museum, corner, villa, park): a Fossil revived, a slot machine through a win and the bonus game, the Villa furnished and visited, a Catching Show
//   dotnet run --project tools/ShotHarness -- <output dir> sunyshore          the tenth chapter: the professor's lab, the lakes' three caverns, Flint in Sunyshore City, the Vista Lighthouse and its lift, Volkner, the Beacon Badge, the rival and Jasmine on the beach
//   dotnet run --project tools/ShotHarness -- <output dir> tower              the Battle Tower: its lobby and counters, a team entered and turned away, the lift, a streak of seven, the Tower Tycoon, a partner out of the Battle Salon, the Exchange Service Corner
//   dotnet run --project tools/ShotHarness -- <output dir> factory            the Battle Factory: its lobby and counter, the staff's hint, the six rentals and a summary, the three chosen, a battle, a trade, the round's Battle Points, a challenge put down, the Factory Head
//   dotnet run --project tools/ShotHarness -- <output dir> champion           the eleventh chapter: Victory Road, the rival before the League's door, the Hall of Fame's scene, the team's entry, the credits, home
//   dotnet run --project tools/ShotHarness -- <output dir> interiors [town ...] [work] [workb] [workc]   the rooms of plan 01 · M11 a town at a time: a shot of each from its way in, a sheet per town, and a few of them at work
//   dotnet run --project tools/ShotHarness -- <output dir> postgame           after the Hall of Fame, part a: the professor's rating, Professor Oak and the National Pokédex, the Poké Radar's lesson, Pal Park's lobby, the ship, the Fight Area's tag battle, Route 227, Marley, Lucian
//   dotnet run --project tools/ShotHarness -- <output dir> hall               the Battle Hall: its lobby and counter, the record keeper, one Pokémon entered, the stage, the board of types, a battle, the round's Battle Points, a challenge put down, two of a kind, the Hall Matron
//   dotnet run --project tools/ShotHarness -- <output dir> castle             the Battle Castle: its lobby and counter, three entered, the valet's room and his two counters, the throne room, a battle and its Castle Points, the round's Battle Points, a challenge put down, a battle passed, the Castle Valet
//   dotnet run --project tools/ShotHarness -- <output dir> legends            after the Hall of Fame, part b: Stark Mountain with Mars, Jupiter, Buck and Charon's arrest, Heatran, a rematch at the League, Snowpoint Temple and Regigigas, the sailor's son, Fullmoon Island, Cresselia and the Lunar Wing
//   dotnet run --project tools/ShotHarness -- <dir> diff <other dir>          two runs' shots compared pixel by pixel (exits 1 if any differ)
//   dotnet run --project tools/ShotHarness -- <dir> ab <before> <after> <before again>   three runs of `profile` compared (tools/dev/ab.sh)
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

if (mode != "crop" && mode != "ab")
{
    Directory.CreateDirectory(outDir);
    Environment.CurrentDirectory = outDir;
}

// The tools that read the shots of earlier runs start no game
if (mode == "contact") { Compare.Contact(args, outDir); return; }
if (mode == "crop") { Compare.Crop(args, startDir); return; }
if (mode == "diff") { Compare.Diff(args, outDir, startDir); return; }
if (mode == "ab") { Timings.Ab(args, startDir); return; }

Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
// SHOTS_WINDOW=3840x2160 makes the hidden window that size: the game then draws as it does full screen on a 4K
// display (FXAA on the high preset, the screen shown one to one), which is what the timings should be read at
var windowSize = (Environment.GetEnvironmentVariable("SHOTS_WINDOW") ?? "1920x1080").Split('x');
Raylib.InitWindow(int.Parse(windowSize[0]), int.Parse(windowSize[1]), "shots");

// Two runs draw the same pictures: the game's chance is seeded, the clock its small motions keep time by is the
// harness's own count of frames, and the day is fixed (the weather calendar and Pokérus's days follow the date): the
// first of June, when it rains on Route 212's south and Route 213 is clear, at noon on the wall clock (the hour of
// a game whose options follow the clock, the Hall of Fame's moment)
Dice.Seed(20261004);
FrameClock.Fixed = 0;
GameClock.FixedDate = new DateTime(2026, 6, 1);
WallClock.Fixed = new DateTime(2026, 6, 1, 12, 0, 0);

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
if (mode == "berries") h.BerriesMode();
if (mode == "poffins") h.PoffinsMode();
if (mode == "poketch") h.PoketchMode();
if (mode == "jubilife") h.JubilifeMode();
if (mode == "windworks") h.WindworksMode();
if (mode == "eterna") h.EternaMode();
if (mode == "veilstone") h.VeilstoneMode();
if (mode == "celestic") h.CelesticMode();
if (mode == "opening") h.OpeningMode();
if (mode == "boutique") h.BoutiqueMode();
if (mode == "daycare") h.DayCareMode();
if (mode == "cast") h.CastMode();
if (mode == "export") h.ExportMode();
if (mode == "contests") h.SuperContestsMode();
if (mode == "capsules") h.CapsulesMode();
if (mode == "league") h.LeagueMode();
if (mode == "lakes") h.LakesMode();
if (mode == "battlezone") h.BattleZoneMode();
if (mode == "snowpoint") h.SnowpointMode();
if (mode == "headquarters") h.HeadquartersMode();
if (mode == "underground") h.UndergroundMode();
if (mode == "pillar") h.PillarMode();
if (mode == "pastimes") h.PastimesMode();
if (mode == "interiors") h.InteriorsMode();
if (mode == "sunyshore") h.SunyshoreMode();
if (mode == "tower") h.TowerMode();
if (mode == "factory") h.FactoryMode();
if (mode == "champion") h.ChampionMode();
if (mode == "postgame") h.PostgameMode();
if (mode == "hall") h.HallMode();
if (mode == "castle") h.CastleMode();
if (mode == "legends") h.LegendsMode();

h.Close();
Raylib.CloseWindow();
