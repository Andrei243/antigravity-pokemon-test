// Screenshot harness: runs the game in a hidden 1920x1080 window, drives it into known states and saves PNGs of
// the virtual screen, so graphics changes can be checked without playing.
//
//   dotnet run --project tools/ShotHarness -- <output dir> [all|field|lineup|battle|flow|menus|look|title|times|sheets] [before dir]
//
// It reaches into GameEngine's private fields by reflection (currentMap, player, currentState, battle, ...), so
// renaming those fields means updating this file. The output directory becomes the working directory, which keeps
// the game from loading or writing savegame.json there.

using System.Numerics;
using System.Reflection;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
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

Directory.CreateDirectory(outDir);
Environment.CurrentDirectory = outDir;

Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
Raylib.InitWindow(1920, 1080, "shots");

var engine = new GameEngine();
engine.Initialize();

// Muted, and by day whatever the real time is (the "times" mode sets the other times of day)
engine.Settings.Muted = true;
engine.Settings.TimeOfDay = TimeOfDay.Day;
engine.ApplySettings(window: false);

// The game opens on its title screen; everything but the "title" mode wants a game in progress
// (in Sinnoh, which the shots below show)
engine.NewGameRegion = "Sinnoh";
engine.StartNewGame();
typeof(GameEngine).GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(engine, GameState.Overworld);

// ---------------------------------------------------------------- helpers

const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
var T = typeof(GameEngine);
object Get(string field) => T.GetField(field, Private)!.GetValue(engine)!;
void Set(string field, object value) => T.GetField(field, Private)!.SetValue(engine, value);

void Frames(int n)
{
    for (int i = 0; i < n; i++)
    {
        engine.Update(1f / 60f);
        engine.Draw();
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

void Shot(string name)
{
    var img = Capture();
    if (!native4K) Raylib.ImageResize(ref img, GameEngine.VirtualWidth, GameEngine.VirtualHeight);
    Save(img, name);
}

// A region of the screen at native resolution, enlarged with nearest-neighbour filtering for close inspection
// (scale is relative to layout units, so 2 means the native 4K pixels)
void ShotCrop(string name, int x, int y, int w, int h, int scale)
{
    var img = Capture();
    Raylib.ImageCrop(ref img, new Rectangle(x * S, y * S, w * S, h * S));
    if (scale > S) Raylib.ImageResizeNN(ref img, w * scale, h * scale);
    Save(img, name);
}

void GoTo(string map, int x, int y, Direction facing)
{
    Set("currentMap", MapDatabase.Get(map));
    ((Player)Get("player")).SetPosition(x, y, facing);
    Set("currentState", GameState.Overworld);
    Frames(2);
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
    var sw = System.Diagnostics.Stopwatch.StartNew();
    Frames(frames);
    Console.WriteLine($"{label}: {sw.Elapsed.TotalMilliseconds / frames:F2} ms/frame");
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

BattleEngine StartBattle(string foe, int level, Trainer trainer = null, string map = "Route201")
{
    Set("currentMap", MapDatabase.Get(map));
    if (trainer != null && trainer.Party.Count == 0) trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
    var enemy = trainer?.Party.Members[0] ?? new Pokemon(PokemonDatabase.Get(foe)!, level);
    var b = new BattleEngine(party, enemy, inventory, pokedex, trainer);
    ((BattleRenderer)Get("battleRenderer")).Trees = ((Map)Get("currentMap")).Trees;
    Set("battle", b);
    Set("currentState", GameState.Battle);
    return b;
}

// Waits out the camera sweep and sends the player's Pokémon out, ending on the main battle menu
void ToMainMenu(BattleEngine b)
{
    Frames(130);
    Confirm(b);
    Frames(50);
    Confirm(b);
    Frames(2);
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
    var tristan = MapDatabase.Get("Route201").NPCs.First(n => n.IsTrainer);
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
}

// ---------------------------------------------------------------- battles

if (Run("battle"))
{
    // Wild battle: camera sweep, send-out, attack, hit, faint
    var b = StartBattle("Shinx", 5);
    Frames(1); Shot("40_wild_intro_start");
    Frames(50); Shot("41_wild_intro_mid");
    Frames(80); Shot("42_wild_appeared");
    Confirm(b); Frames(10); Shot("43_go_sendout");
    Frames(60); Confirm(b); Frames(2); Shot("44_main_menu");
    b.HUD.MenuState = BattleMenuState.Moves; Frames(1); Shot("44b_moves");
    b.HUD.MenuState = BattleMenuState.Main;

    // Whoever is faster attacks first: lunge, then the hit lands and the HP bar drains
    b.SelectMove(0);
    Console.WriteLine("  msg: " + b.CurrentMessage);
    Frames(8); Shot("45_attack_lunge");
    Frames(14); Shot("46_attack_hit");
    Frames(30); Shot("47_hp_drain");
    for (int guard = 0; guard < 12 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++)
    {
        Confirm(b);
        Frames(30);
    }
    Shot("49_after_turn");

    // Knock the foe out
    b.EnemyPokemon.CurrentHP = 1;
    b.SelectMove(0);
    Frames(40);
    for (int guard = 0; guard < 6 && !b.CurrentMessage.Contains("fainted"); guard++) { Confirm(b); Frames(20); }
    Frames(18); Shot("50_faint_mid");
    Frames(40); Shot("50b_faint_done");

    // Capture with a Master Ball (always caught) and a Poké Ball on a healthy foe (usually breaks free)
    foreach (var (ball, tag) in new[] { ("Master Ball", "caught"), ("Poké Ball", "free") })
    {
        b = StartBattle("Starly", 4);
        ToMainMenu(b);
        typeof(BattleEngine).GetMethod("ExecuteTurn", Private)!
            .Invoke(b, new object[] { new BattleAction { Type = ActionType.UseItem, IsPlayer = true, Item = ItemDatabase.Get(ball) } });
        Confirm(b);
        Frames(24); Shot($"51_{tag}_ball_flight");
        Frames(30); Shot($"52_{tag}_ball_open");
        Frames(40); Shot($"53_{tag}_ball_ground");
        Frames(30); Shot($"54_{tag}_ball_wobble");
        Frames(120); Shot($"55_{tag}_result");
        Console.WriteLine("  msg: " + b.CurrentMessage);
    }

    // Trainer battle: both trainers on their platforms, then they step aside as the Pokémon come out
    var trainer = MapDatabase.Get("Route201").NPCs.First(n => n.IsTrainer).TrainerData!;
    b = StartBattle("", 0, trainer);
    Frames(1); Shot("60_trainer_intro_start");
    Frames(130); Shot("61_trainer_wants");
    Confirm(b); Frames(14); Shot("62_trainer_sendout");
    Frames(60); Shot("63_trainer_gone");
    Confirm(b); Frames(14); Shot("64_player_sendout");
    Frames(60); Confirm(b); Frames(2); Shot("65_trainer_main");

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
    ((StarterSelectScreen)Get("starterSelectScreen")).Open();
    Frames(1); Shot("21_starter");
    Set("currentState", GameState.BagMenu);
    ((BagScreen)Get("bagScreen")).Open();
    Frames(1); Shot("22_bag");

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
    var menuTrainer = MapDatabase.Get("Route201").NPCs.First(n => n.IsTrainer).TrainerData!;
    mb = StartBattle("", 0, menuTrainer);
    Frames(131); Confirm(mb); Frames(74); Confirm(mb); Frames(74); Confirm(mb); Frames(2);
    Shot("25_trainer_hud");

    if (args.Length > 2) Boards(args[2], new[] { "09_startmenu", "20b_summary", "23_battle_switch", "24_battle_bag" });

    party.Members[1].Status = StatusCondition.None;
    party.HealAll();
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

    Set("currentState", GameState.Overworld);
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

// ---------------------------------------------------------------- contact sheets

if (Run("sheets"))
{
    // Every species: front sprite, back sprite and menu icon, as baked from the 3D models
    var species = PokemonDatabase.GetAll().Select(s => s.Name).ToArray();
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
}

engine.Close();
Raylib.CloseWindow();
