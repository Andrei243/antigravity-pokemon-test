// Screenshot harness: runs the game in a hidden 1920x1080 window, drives it into known states and saves PNGs of
// the virtual screen, so graphics changes can be checked without playing.
//
//   dotnet run --project tools/ShotHarness -- <output dir> [all|field|lineup|battle|flow|menus|look|sheets]
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
AudioManager.ToggleMute();

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

void Shot(string name) => Save(Capture(), name);

// A region of the screen, enlarged with nearest-neighbour filtering for close inspection
void ShotCrop(string name, int x, int y, int w, int h, int scale)
{
    var img = Capture();
    Raylib.ImageCrop(ref img, new Rectangle(x, y, w, h));
    Raylib.ImageResizeNN(ref img, w * scale, h * scale);
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

void Timing(string label, int frames = 120)
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
    // Grass encounter -> fade -> battle -> run -> fade back to the field
    GoTo("Route201", 24, 8, Direction.Up);
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
    GoTo("TwinleafTown", 11, 8, Direction.Down);
    ((StartMenu)Get("startMenu")).Open();
    Frames(1); Shot("09_startmenu");
    ((StartMenu)Get("startMenu")).Close();

    Set("currentState", GameState.PartyMenu);
    ((PartyScreen)Get("partyScreen")).Open();
    Frames(1); Shot("20_party");
    Set("currentState", GameState.StarterSelect);
    ((StarterSelectScreen)Get("starterSelectScreen")).Open();
    Frames(1); Shot("21_starter");
    Set("currentState", GameState.BagMenu);
    ((BagScreen)Get("bagScreen")).Open();
    Frames(1); Shot("22_bag");
    Set("currentState", GameState.Overworld);
}

// ---------------------------------------------------------------- art direction (plan 04 · G1, docs/art/style-guide.md)

if (Run("look"))
{
    // The style guide's reference frames in the pre-overhaul look and in the chosen "Sinnoh Diorama" look, then
    // side-by-side boards. Pass "current" or "diorama" as the third argument to render only one look.
    string only = args.Length > 2 ? args[2] : "";
    foreach (var name in new[] { "Starly", "Shinx", "Bidoof" })
        if (party.Count < 6) party.Add(new Pokemon(PokemonDatabase.Get(name)!, 4 + party.Count));
    party.Members[1].CurrentHP = party.Members[1].MaxHP / 3;
    party.Members[3].CurrentHP = party.Members[3].MaxHP / 7;

    foreach (var look in new[] { ArtDirection.Current, ArtDirection.Diorama })
    {
        string tag = look.ToString().ToLowerInvariant();
        if (only != "" && only != tag) continue;
        ArtLook.Set(look);

        GoTo("TwinleafTown", 11, 8, Direction.Down); Frames(2); Shot($"look_{tag}_1_twinleaf");

        GoTo("TwinleafTown", 12, 7, Direction.Up);
        ((DialogueManager)Get("dialogue")).ShowDialogue("Barry", new List<string> { "Barry: Hey, Lucas! You're finally ready! Professor Rowan is waiting at Lake Verity!" });
        Set("currentState", GameState.Dialogue);
        Frames(180); Shot($"look_{tag}_2_dialogue");
        Set("dialogue", new DialogueManager());

        var pb = StartBattle("Shinx", 5);
        ToMainMenu(pb);
        Shot($"look_{tag}_3_battle");
        pb.HUD.MenuState = BattleMenuState.Moves; Frames(1); Shot($"look_{tag}_3b_moves");
        pb.HUD.MenuState = BattleMenuState.Main;

        Set("currentState", GameState.PartyMenu);
        ((PartyScreen)Get("partyScreen")).Open();
        Frames(1); Shot($"look_{tag}_4_party");
        ((PartyScreen)Get("partyScreen")).Close();

        GoTo("Route201", 24, 8, Direction.Up); Frames(2); Shot($"look_{tag}_5_route201");
        GoTo("PlayerHouse", 4, 6, Direction.Up); Frames(2); Shot($"look_{tag}_6_house");

        GoTo("TwinleafTown", 11, 8, Direction.Down);
        Timing($"{tag} field");
        StartBattle("Luxray", 30);
        ToMainMenu((BattleEngine)Get("battle"));
        Timing($"{tag} battle");
    }
    ArtLook.Set(ArtDirection.Diorama);

    if (only == "")
    {
        foreach (var frame in new[] { "1_twinleaf", "2_dialogue", "3_battle", "3b_moves", "4_party", "5_route201", "6_house" })
        {
            var board = Raylib.GenImageColor(1920 + 30, 540 + 76, new Color(24, 26, 34, 255));
            int col = 0;
            foreach (var (tag, label) in new[] { ("current", "BEFORE"), ("diorama", "AFTER  (SINNOH DIORAMA)") })
            {
                var img = Raylib.LoadImage(Path.Combine(outDir, $"look_{tag}_{frame}.png"));
                Raylib.ImageResize(ref img, 960, 540);
                int x = 10 + col * 970;
                Raylib.ImageDraw(ref board, img, new Rectangle(0, 0, 960, 540), new Rectangle(x, 66, 960, 540), Color.White);
                Raylib.ImageDrawText(ref board, label, x + 4, 18, 40, Color.White);
                Raylib.UnloadImage(img);
                col++;
            }
            Save(board, $"compare_{frame}");
        }
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
