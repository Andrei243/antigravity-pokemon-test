using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 17 · N1: the string table. Every key the engine asks for is in English's table and every entry of it is
/// asked for somewhere; the screens moved onto the table say nothing in quotes of their own; forms follow the
/// plural rule and the player's look; and the pseudo-language brackets every string, which is what shows a string
/// that slipped past. Changing the language is the whole process's, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class TextTests
{
    /// <summary>
    /// The files whose words are all the table's: every file of <c>UI/</c> and <c>Core/</c> but those
    /// <see cref="NotOnTheTable"/> names, and the files of the rules outside them that hand a screen its words
    /// (<see cref="AlsoOnTheTable"/>). A word written in quotes in one of them fails
    /// <see cref="TheScreensOnTheTableSayNothingInQuotesOfTheirOwn"/>.
    /// </summary>
    private static IEnumerable<string> OnTheTable =>
        new[] { "UI", "Core" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(Engine, folder), "*.cs", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(Engine, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(name => !NotOnTheTable.ContainsKey(name))
            .Concat(AlsoOnTheTable)
            .OrderBy(name => name, StringComparer.Ordinal);

    /// <summary>The files of the rules outside <c>UI/</c> and <c>Core/</c> whose words a screen shows (plan 17 · N1d, N1e).</summary>
    private static readonly string[] AlsoOnTheTable =
    {
        "Models/PokedexSearch.cs", "Models/PoketchApps/ColorChangerApp.cs", "Models/Decorating.cs", "Models/Frontier/FrontierBoard.cs",
        "Models/Poffins.cs", "Models/SuperContests/ContestRules.cs", "Models/Ribbons.cs", "Models/Underground/SecretBaseRules.cs",
        "Overworld/FieldMoveRules.cs", "Overworld/Fishing.cs", "Overworld/Bicycle.cs", "Models/ItemUse.cs", "Models/MoveTeaching.cs",
        "Models/EncounterAids.cs", "Models/Journal.cs",
        // Plan 08 · P8: the programmes a television shows
        "Story/Broadcasts.cs",
        // Plan 17 · N2: the battle's words that are no line of its log (which TheBattleSaysNothingInQuotesOfItsOwn holds)
        "Battle/BattleText.cs", "Battle/BattleInfo.cs", "Battle/BattleBag.cs", "Battle/MoveHint.cs",
        "Battle/BattleEngine.cs", "Battle/BattleEngine.Menus.cs", "Battle/BattleEngine.Learning.cs", "Battle/BattleHUD.cs", "Models/Trainer.cs"
    };

    /// <summary>
    /// The files of <c>UI/</c> and <c>Core/</c> that are not on the table, each with why: what speaks to a terminal or
    /// a report and never to the screen, which stays in English. (Plan 17 · N2 put the battle's, the evolution
    /// scene's and the introduction's on the table, which were the rest of this list.)
    /// </summary>
    private static readonly Dictionary<string, string> NotOnTheTable = new()
    {
        ["Core/CommandLine.cs"] = "the switches and --help, said to the terminal",
        ["Core/SavePaths.cs"] = "--where, said to the terminal",
        ["Core/MigrationOut.cs"] = "the extensions of the files it writes, not words on a screen",
        ["Core/Diagnostics/Log.cs"] = "the log, for whoever reads log.txt",
        ["Core/Diagnostics/CrashReport.cs"] = "a crash's report, for whoever reads it",
        ["Core/Diagnostics/Manifest.cs"] = "--verify, said to the terminal",
        ["Core/Diagnostics/GameVersion.cs"] = "the build's version as the log and --help write it",
        ["Core/Autoplay/AutoplayReport.cs"] = "the playthrough bot's play.md, for whoever reads the run (plan 16 · T7)",
        ["Core/Autoplay/Autoplayer.cs"] = "the playthrough bot's findings and the goals it names, for play.md (plan 16 · T7)",
        ["Core/Autoplay/Policy.cs"] = "the keys the bot remembers its goals by, no words (plan 16 · T7)",
        ["Core/DevCommands.cs"] = "the developer's commands and what they print, for the console and the harness's scenarios (plan 16 · T17)",
        ["Core/ShotScenario.cs"] = "a scenario's verbs and the mistakes in its lines, said to whoever wrote it (plan 16 · T17)",
        ["Core/StateSnapshot.cs"] = "the names of a snapshot's fields, which a scenario expects and state.json keeps (plan 16 · T17)",
        ["Core/Autoplay/Autoplayer.Itinerary.cs"] = "why a leg of the bot's way was given up, for play.md (plan 16 · T13)",
        ["Core/Autoplay/Itinerary.cs"] = "a leg of the bot's way as play.md names it (plan 16 · T13)",
        ["Core/Autoplay/Itineraries.cs"] = "the names of maps, places, scripts and people the bot's way goes by, no words (plan 16 · T13)",
        ["Core/DevMode.cs"] = "where developer mode reads the data from, for the log (plan 16 · T6)",
        ["Core/Reload.cs"] = "the folders and kinds of file the live reload watches, for the developer (plan 16 · T6)",
        ["Core/GameEngine.Dev.cs"] = "what the developer's console, reload, trace and frame time say, for the developer alone (plan 16 · T6)",
        ["UI/DevConsole.cs"] = "the developer's console, which says what its commands answer in English (plan 16 · T6)",
        ["Core/InputRecording.cs"] = "why a recording is refused, for whoever plays it back with a tool (plan 16 · T19)",
        ["Core/Movie.cs"] = "where a replay parted, for the log and the tool playing it (plan 16 · T19)",
        ["Core/StateHash.cs"] = "the names of the hash's groups, which a parting names (plan 16 · T19)",
        ["Core/Diagnostics/ReportFolder.cs"] = "a report's report.txt, for whoever is handed the folder (plan 16 · T20)"
    };

    /// <summary>What may stay in quotes in a file on the table: names of the data, and the logo, which is a picture (plan 17).</summary>
    private static readonly Dictionary<string, string[]> NotWords = new()
    {
        ["UI/TitleScreen.cs"] = new[] { "Sinnoh", "Giratina", "PLATINUM", "Pokémon", "A  FAN  REMAKE" },
        ["UI/StartMenu.cs"] = new[] { "Trainer" },
        // The address of a translated line, which is a key and no word (plan 17 · N4)
        ["Core/Text.Lines.cs"] = new[] { "script:{script}#{index}.{k}" },
        // The text box's markup problems, which ScriptLibrary.Problems and the content check tell whoever writes a line, never the screen (plan 09 · L3)
        ["UI/Kit/RichText.cs"] = new[]
        {
            "a '{' is never closed", "{{{body}}} has no text to colour", "{{{body}}} needs a good number after it",
            "{{{body}}} needs a sound's name after it", "{{{body}}}: a break takes no argument"
        },
        // The items' own names and the uses the item table gives them, which the bag goes by
        ["UI/BagScreen.cs"] = new[]
        {
            "Gracidea", "Bicycle", "OldRod", "GoodRod", "SuperRod", "EscapeRope", "Journal", "VsSeeker", "PokeRadar", "Honey",
            "PoffinCase", "ExplorerKit", "TownMap", "Seal Case", "Fashion Case", "Flute", "Shaymin-Sky", "Itemfinder"
        },
        // A place's name made from its key says Pokémon with its accent
        ["UI/ModernUi.Pokedex.cs"] = new[] { "Pokémon" },
        // The search's groups of first letters, and the shapes as species.json names them, which it searches by
        ["Models/PokedexSearch.cs"] = new[]
        {
            "ABC", "DEF", "GHI", "JKL", "MNO", "PQR", "STU", "VWX", "YZ",
            "Ball", "Squiggle", "Fish", "Arms", "Blob", "Upright", "Legs", "Quadruped", "Wings", "Tentacles", "Heads", "Humanoid", "Bug Wings", "Armor"
        },
        ["UI/StarterSelectScreen.cs"] = new[] { "Turtwig", "Chimchar", "Piplup" },
        // The words a place's name ends in, which tell a city from a town and a route's colour from a lake's
        ["UI/ModernUi.TownMap.cs"] = new[] { "City" },
        ["UI/ModernUi.Field.cs"] = new[] { "Route", "Lake", "Town", "City" },
        ["UI/ModernUi.Ceremony.cs"] = new[] { "PLATINUM", "Pokémon", "A  FAN  REMAKE" },
        // The bonus game's own Pokémon, whose cry it plays
        ["UI/SlotMachineScreen.cs"] = new[] { "Clefairy" },
        // The treasures' ids in the Underground's table, which a treasure's colour in the wall goes by
        ["UI/ModernUi.Underground.cs"] = new[]
        {
            "_FOSSIL", "RARE_BONE", "OLD_AMBER", "RED_SHARD", "FIRE_STONE", "FLAME", "HEAT", "DRACO", "BLUE_SHARD", "WATER_STONE", "SPLASH", "DAMP", "SKY",
            "YELLOW_SHARD", "THUNDER", "ZAP", "REVIVE", "STAR", "SUN", "GREEN_SHARD", "LEAF", "MEADOW", "INSECT", "HEART", "MIND", "ICY", "ICICLE", "MOON",
            "LIGHT_CLAY", "SPOOKY", "TOXIC", "DREAD", "ODD_KEYSTONE", "EARTH", "FIST", "SMOOTH", "STONE_PLATE"
        },
        // A row's picture by its kind; the hikers' kinds of stock and the Digger Drill's id, which the lists go by
        ["UI/UndergroundScreen.cs"] = new[] { "sphere:", "treasure:", "Traps", "Treasures", "TRAP_DIGGER_DRILL" },
        // The same Pokémon, whose sprite the slot machine's display shows in the bonus game
        ["UI/ModernUi.Pastimes.cs"] = new[] { "Clefairy" },
        // A mistake of the code's own, never seen by a player
        ["UI/PoffinCookingScreen.cs"] = new[] { "{berry.Name} is no berry." },
        // The words of the accessories' names in contests.json, which tell how an accessory is drawn
        ["UI/ModernUi.SuperContest.cs"] = new[]
        {
            "White", "Yellow", "Pink", "Brown", "Black", "Orange", "Blue", "Green", "Purple", "Red", "Gold", "Glass", "Silk", "Snow",
            "Glitter", "Shiny", "Fire", "Leaf", "Tree", "Mushroom", "Scale", "Pebble", "Boulder", "Coin", "Candy", "Dewdrop", "Claw", "Hown",
            "Stump", "Sparks", "Determination", "Spoon", "Smoke", "Poison", "Eerie", "Spring", "Seashell", "Note", "Confetti", "Parasol",
            "Umbrella", "Spotlight", "Cape", "Mike", "Surfboard", "Carpet", "Pipe", "Bed", "Mirror", "Board", "Lace", "Top Hat", "Heroic",
            "Professor", "Award", "Cube", "Turtwig", "Chimchar", "Piplup", "Flag", "Crown", "Tiara", "Comet", "Specs", "Fluff", "Mirror Ball",
            "Feather", "Moustache", "Beard", "Barrette", "Headband", "Flower", "Crystal", "Powder", "Horn", "Extract", "Balloon",
            "Hat", "Headdress", "Veil", "Mask"
        },
        // The backdrops' names in contests.json, which choose how each is painted
        ["UI/ModernUi.Ribbons.cs"] = new[]
        {
            "Dress Up", "Ranch", "City at Night", "Snowy Town", "Fiery", "Outer Space", "Cumulus Cloud", "Desert", "Flower Patch",
            "Future Room", "Open Sea", "Total Darkness", "Tatami Room", "Gingerbread Room", "Seafloor", "Underground", "Sky", "Theater"
        },
        // A mistake of the code's own, never seen by a player
        ["Models/Poffins.cs"] = new[] { "A Poffin has five flavours." },
        // The scarves the condition's score goes by, the accessories a first win puts in the Fashion Case (items.json's and
        // contests.json's names) and the start of a ribbon's constant
        ["Models/SuperContests/ContestRules.cs"] = new[]
        {
            "Red Scarf", "Blue Scarf", "Pink Scarf", "Green Scarf", "Yellow Scarf", "RIBBON_",
            "Red Barrette", "Red Balloons", "Top Hat", "Gold Pedestal", "Blue Barrette", "Blue Balloons", "Silk Veil", "Glass Stage",
            "Pink Barrette", "Pink Balloon", "Lace Headdress", "Flower Stage", "Green Barrette", "Green Balloons", "Professor Hat", "Cube Stage",
            "Yellow Barrette", "Yellow Balloon", "Heroic Headband", "Award Podium"
        },
        // The parts of the ribbons' constants that tell a contest's ribbon, its region and its rank
        ["Models/Ribbons.cs"] = new[] { "RIBBON_", "RIBBON_HOENN_", "SUPER", "GREAT", "HYPER", "ULTRA", "MASTER" },
        // The parts of the goods' and traps' ids a tool goes by, the records underground.json says a good shows, the
        // hikers' kinds of stock, and the other diggers' names, which stand for other players' and are kept in the save
        ["Models/Underground/SecretBaseRules.cs"] = new[]
        {
            "_TOOL", "TRAP_", "UG_GOOD_", "TRAP_DIGGER_DRILL", "FossilsDug", "SpheresDug", "TrapHits", "TrapsHit", "TimesBaseMoved",
            "FlagsObtained", "FlagsRegistered", "TotalScore", "Traps", "Goods", "Treasures",
            "Bram", "Odette", "Corwin", "Mirela", "Tavish", "Ilse", "Rurik", "Nadia", "Per", "Zofia", "Emrys", "Linnea", "Dov", "Sabela",
            "Kito", "Marit", "Anselm", "Yara", "Wim", "Tindra", "Oskar", "Beatrix", "Jarek", "Solveig"
        },
        // The field moves' names as the move data has them, which the party's moves are matched by
        ["Overworld/FieldMoveRules.cs"] = new[]
        {
            "Cut", "Fly", "Surf", "Strength", "Defog", "Rock Smash", "Waterfall", "Rock Climb", "Flash", "Teleport", "Dig", "Sweet Scent",
            "Milk Drink", "Soft-Boiled", "Whirlpool", "Dive", "Secret Power"
        },
        // The rods' uses in the field as the item table gives them
        ["Overworld/Fishing.cs"] = new[] { "OldRod", "GoodRod", "SuperRod" },
        // The item table's field uses and use parameters, which an item is used by
        ["Models/ItemUse.cs"] = new[]
        {
            "Healing", "Berry", "friendshipLow", "friendshipMed", "friendshipHigh", "healSleep", "healPoison", "healBurn", "healFreeze",
            "healParalysis", "hpRestored", "levelUp", "ppUp", "ppMax", "ppRestored", "restorePPAllMoves", "reviveAll"
        },
        // What an HM's name begins with, and a mistake of the code's own, never seen by a player
        ["Models/MoveTeaching.cs"] = new[] { "HM", "A Pokémon with four moves forgets one to learn another" },
        // The Repels' and the flutes' own names, which their use goes by
        ["Models/EncounterAids.cs"] = new[] { "Repel", "Super Repel", "Max Repel", "Black Flute", "White Flute" },
        // The vowels English says "an" before, which choose between the Journal's two lines of a catch
        ["Models/Journal.cs"] = new[] { "AEIOU" },
        // The looks the start makes ready, the items and Pokémon a game begins with or is owed, the items' uses the
        // bag goes by, the trainer classes the Journal tells of, the region and the rooms the game asks about
        ["Core/GameEngine.cs"] = new[]
        {
            "PLAYER", "DAWN", "ROWAN", "Poké Ball", "Great Ball", "Ultra Ball", "Master Ball", "Journal", "Potion", "Revive", "Turtwig",
            "Glitter Powder", "PoffinCase", "TownMap", "Leader", "Elite Four", "Champion", "Sinnoh", "PlayerHouse",
            "Soot Sack", // the item that gathers Route 113's ash (plan 21 · H4), by its name in the data
            "slot {s.Number} {s.Outcome}" // how the slots read, for the log's line after it
        },
        // The region, the starter and its move an old save was given, and the save's field of its checksum
        ["Core/SaveManager.cs"] = new[] { "Lucas", "Sinnoh", "Turtwig", "Tackle", "Poké Ball", "Checksum", "Kanto", "PalletTown_PlayersHouse_1F" },
        // English as languages.json names it and its script, the language every other falls back to
        ["Core/Language.cs"] = new[] { "English", "Latin" },
        // The folders the typeface is read from
        ["UI/Kit/UiFonts.cs"] = new[] { "Assets", "Fonts" },
        ["UI/Kit/TextMetrics.cs"] = new[] { "Assets", "Fonts" },
        // The keyboard's rows of letters, which plan 17 · N5 gives each language its own of
        ["UI/Kit/NameEntry.cs"] = new[] { "ABCDEFGHIJ", "KLMNOPQRST", "UVWXYZ.-' ", "uvwxyz.-' " },
        // The bands' trainers by class and name as trainers.json has them, and the mark's lettering, a picture as the title's logo is
        ["UI/SceneTransition.Bands.cs"] = new[]
        {
            "Leader", "Elite Four", "Champion", "Roark", "Gardenia", "Wake", "Maylene", "Fantina", "Candice", "Byron", "Volkner",
            "Aaron", "Bertha", "Flint", "Lucian", "Cynthia", "VS",
            // The Champions of the Mega Max band's own table (plan 25 · YD6)
            "Steven", "Iris",
            // Kanto's (plan 19 · K8): its Leaders and the Elite Four by name, and the Champion who is the rival
            "Brock", "Misty", "Lt. Surge", "Erika", "Koga", "Sabrina", "Blaine", "Giovanni", "Lorelei", "Bruno", "Agatha", "Lance", "{rival}",
            // Hoenn's (plan 21 · H9, part b): its Leaders, the Elite Four and Wallace by name
            "Roxanne", "Brawly", "Wattson", "Flannery", "Norman", "Winona", "Tate & Liza", "Juan", "Sidney", "Phoebe", "Glacia",
            "Drake", "Wallace"
        },
        // The region and the rooms the Journal goes by, the Gyms' towns it keeps in the save, the items' uses the bag goes by, the move Surf and the bicycles
        ["Core/GameEngine.Scripts.cs"] = new[]
        {
            "Sinnoh", "PlayerHouse", "RowanLab", "OreburghGym", "Oreburgh", "EternaGym", "Eterna", "HearthomeGym", "Hearthome", "VeilstoneGym",
            "Veilstone", "SunyshoreGym", "Sunyshore", "Journal", "TownMap", "PoffinCase", "VsSeeker", "ExplorerKit", "PokeRadar", "Honey",
            "Bicycle", "EscapeRope", "Surf",
            // Emerald's two bicycles by their items' names, and the Itemfinder's use (plan 21 · H3, part b)
            "Mach Bike", "Acro Bike", "Itemfinder"
        },
        // The map, the scripts the game itself starts there, the region, and the base's props by their art keys
        ["Core/GameEngine.Underground.cs"] = new[]
        {
            "Underground", "Wall", "BasePC", "DiggerPC", "GoUp", "Sinnoh", "pc:{SecretBaseRules.FlagFor(flags)}", "good:{good.Good}"
        },
        // The maps and the script of Pal Park, the place a Pokémon of the park was met, the Villa's furniture by its
        // names in the table, the start of its flags and its scripts' names, and the Game Corner's rooms and its case
        ["Core/GameEngine.Pastimes.cs"] = new[]
        {
            "PalPark", "PalParkLobby", "CaughtAll", "Pal Park", "Table", "Big Sofa", "Small Sofa", "Bed", "Night Table", "TV", "Audio System",
            "Bookshelf", "Rack", "Houseplant", "PC Desk", "Music Box", "Pokémon Bust", "Silver Pokémon Bust", "Piano", "Guest Set", "Wall Clock",
            "Masterpiece", "Tea Set", "Look", "FLAG_VILLA_FURNITURE_", "Chandelier", "VeilstoneGameCorner", "VeilstonePrizeExchange", "Coin Case"
        },
        // The lobby's map, a person of the room by name, and the look a trainer with none takes
        ["Core/GameEngine.Frontier.cs"] = new[] { "BattleTower", "opponent_2", "Trainer" },
        ["Core/GameEngine.Capsules.cs"] = new[] { "Fashion Case" },
        // The key item the Apricorn Box is, and Kurt's id in the phone book (plan 20 · J6)
        ["Core/GameEngine.Apricorns.cs"] = new[] { "Apricorn Box", "kurt" },
        // The start of the Master Ranks' flags, and the region whose story is asked about
        ["Core/GameEngine.SuperContests.cs"] = new[] { "FLAG_CONTEST_MASTER_", "Sinnoh" },
        // The lobbies' maps
        ["Core/GameEngine.Hall.cs"] = new[] { "BattleHall" },
        ["Core/GameEngine.Factory.cs"] = new[] { "BattleFactory" },
        ["Core/GameEngine.Castle.cs"] = new[] { "BattleCastle" },
        ["Core/GameEngine.Arcade.cs"] = new[] { "BattleArcade" },
        // The Battle Pyramid's floor's own scripts by name (plan 14 · B10)
        ["Core/GameEngine.Pyramid.cs"] = new[] { "Wild", "Trainer", "HiddenItem" },
        ["Core/GameEngine.Encounters.cs"] = new[] { "Poké Radar" },
        // The characters' own names, which the save keeps as the player's and the rival's, and the looks by their names
        ["Core/PlayerIdentity.cs"] = new[] { "Barry", "Dawn", "Lucas", "DAWN", "PLAYER", "Player", "Assistant", "Rival:" },
        // What the tests hear of a cry, never on screen
        ["Core/AudioManager.cs"] = new[] { "cry {name}", "cry {name} {mode}", "cry {name} shaped {shape.Clamped().Key}", "music {song.Id}{(night ? ", ")}{(lowHp ? ", ")}{(giant ? ", "bed {b}", "fanfare {role}" },
        // Plan 17 · N2. The Pokémon the professor shows, the looks he and the rival are drawn as, and the ball's art
        ["UI/IntroScreen.cs"] = new[] { "Buneary", "RIVAL", "ROWAN", "Poké Ball" },
        // The keyboard's case key, which shows the letters it turns to (plan 17 · N5 gives each language its keyboard)
        ["UI/ModernUi.Intro.cs"] = new[] { "abc", "ABC" },
        // The ball whose art the foe's tray and the caught mark draw, and the class the boss box names (plan 25 · YD6)
        ["UI/ModernUi.Battle.cs"] = new[] { "Poké Ball", "Champion" },
        // The Pokémon, the ball and the trainer the catching lesson is played with, the stages by their names, and the
        // species, abilities and forms the field's weather turns
        ["Core/GameEngine.Battles.cs"] = new[]
        {
            "Piplup", "Poké Ball", "Plain", "Water", "City", "Forest", "Mountain", "Snow", "Shinx", "Castform", "Forecast", "Castform-Sunny",
            "Castform-Rainy", "Castform-Snowy", "Cherrim", "Cherrim-Sunshine", "Arceus", "Multitype", "Arceus-"
        },
        // The class a trainer with none is, and the end of a Master Trainer's class, which names its species
        ["Models/Trainer.cs"] = new[] { "Trainer", " Master" },
        // The balls the Great Marsh's battle throws, and the move whose effect the screen shows as a change of shape
        ["Battle/BattleEngine.Menus.cs"] = new[] { "Safari Ball" },
        ["Battle/BattleEngine.cs"] = new[] { "Transform" },
        // What a crash report says of the screen the game was on, for whoever reads the report
        ["Core/GameEngine.Diagnostics.cs"] = new[] { "{flow.ClosingFrom} (the close button's question was up)", "Transition ({flow.Before} to {flow.After})" },
        // The options' rows the first start's page asks for by name, some not built yet (plan 16 · T24)
        ["UI/FirstStartScreen.cs"] = new[] { "Language", "Captions", "Quality" }
    };

    private static readonly Regex Literal = new(@"(\$?)""((?:[^""\\\n]|\\.)*)""", RegexOptions.Compiled);

    private static string Engine => Path.Combine(Repository.Root, "PokemonPlatinumEngine");

    // A source file's lines with the comments left out
    private static IEnumerable<string> CodeOf(string path) =>
        File.ReadLines(path).Where(line => !line.TrimStart().StartsWith("//")).Select(line =>
        {
            int comment = line.IndexOf(" // ", StringComparison.Ordinal);
            return comment >= 0 && line[..comment].Count(c => c == '"') % 2 == 0 ? line[..comment] : line;
        });

    private static IEnumerable<(string Text, bool Interpolated)> LiteralsOf(string path) =>
        CodeOf(path).SelectMany(line => Literal.Matches(line)).Select(m => (m.Groups[2].Value, m.Groups[1].Value == "$"));

    // A line that writes to the log or throws says nothing on screen: the log and a mistake of the code's are read in English
    private static readonly Regex NotForTheScreen = new(@"\bLog\.(Info|Warn|Error|Say|Phase)\(|Exception\(", RegexOptions.Compiled);

    private static IEnumerable<(string Text, bool Interpolated)> LiteralsOnScreen(string path) =>
        CodeOf(path).Where(line => !NotForTheScreen.IsMatch(line))
            .SelectMany(line => Literal.Matches(line)).Select(m => (m.Groups[2].Value, m.Groups[1].Value == "$"));

    private static HashSet<string> Sections => Text.EnglishKeys.Select(k => k[..k.IndexOf('.')]).ToHashSet();

    private static bool LooksLikeAKey(string text, HashSet<string> sections) =>
        Regex.IsMatch(text, @"^[a-z][A-Za-z]*(\.[A-Za-z0-9{}.?\s]+)+$") && sections.Contains(text[..text.IndexOf('.')]);

    // A key as a pattern: whatever an interpolation puts in is one segment of letters and digits
    private static Regex PatternOf(string key) =>
        new("^" + string.Concat(Regex.Split(key, @"(\{[^}]*\})").Select(part => part.StartsWith('{') ? "[A-Za-z0-9]+" : Regex.Escape(part))) + "$");

    /// <summary>Every key the engine's code asks for, as written: a literal, or an interpolation of one.</summary>
    private static List<(string Key, string File)> KeysAskedFor()
    {
        var sections = Sections;
        var found = new List<(string, string)>();
        foreach (var file in Directory.EnumerateFiles(Engine, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            foreach (var (text, _) in LiteralsOf(file))
                if (LooksLikeAKey(text, sections)) found.Add((text, Path.GetRelativePath(Engine, file)));
        }
        return found;
    }

    [Fact]
    public void EveryKeyTheEngineAsksForIsInEnglishsTable()
    {
        var keys = Text.EnglishKeys;
        var asked = KeysAskedFor();
        Assert.NotEmpty(asked);
        var missing = asked.Where(a => a.Key.Contains('{') ? !keys.Any(PatternOf(a.Key).IsMatch) : !keys.Contains(a.Key))
            .Select(a => $"{a.File}: {a.Key}").ToList();
        Assert.True(missing.Count == 0, "Keys with no English:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void EveryEntryOfEnglishsTableIsAskedForSomewhere()
    {
        var asked = KeysAskedFor().Select(a => a.Key).Distinct().ToList();
        var plain = asked.Where(k => !k.Contains('{')).ToHashSet();
        var patterns = asked.Where(k => k.Contains('{')).Select(PatternOf).ToList();
        var unused = Text.EnglishKeys.Where(k => !plain.Contains(k) && !patterns.Any(p => p.IsMatch(k))).OrderBy(k => k).ToList();
        Assert.True(unused.Count == 0, "Entries nothing asks for:\n" + string.Join("\n", unused));
    }

    [Fact]
    public void TheScreensOnTheTableSayNothingInQuotesOfTheirOwn()
    {
        var sections = Sections;
        var said = new List<string>();
        foreach (var name in OnTheTable)
        {
            var allowed = NotWords.GetValueOrDefault(name, Array.Empty<string>());
            foreach (var (text, _) in LiteralsOnScreen(Path.Combine(Engine, name)))
            {
                string bare = Regex.Replace(text, @"\{[^}]*\}", "");
                if (!Regex.IsMatch(bare, @"\p{L}{2,}")) continue;            // no word in it: a format, a number
                if (LooksLikeAKey(text, sections)) continue;                 // the table's key
                if (Regex.IsMatch(text, @"^[a-z_]+$")) continue;             // a sound's or a song's name
                if (Regex.IsMatch(text, @"^[X.]+$")) continue;               // a picture in blocks (the Pokétch's)
                if (Regex.IsMatch(text, @"^[A-Z0-9]+(_[A-Z0-9]+)+$")) continue; // a constant's name (RIBBON_RED), which the data goes by
                if (Regex.IsMatch(text, @"^[A-Za-z][a-z0-9]*([A-Z0-9][A-Za-z0-9]*)+$")) continue; // a name of the code's or the data's (TwinleafTown, halfSize)
                if (Regex.IsMatch(text, @"^[a-z_]+\.[A-Z]\w*$")) continue;    // a script's name (common.UseFly)
                if (Regex.IsMatch(bare, @"^\S*\.[a-z]+$")) continue;          // a file's name (settings.json, .bak)
                if (allowed.Contains(text)) continue;
                said.Add($"{name}: \"{text}\"");
            }
        }
        Assert.True(said.Count == 0, "Words in quotes on screens that are on the table:\n" + string.Join("\n", said));
    }

    [Fact]
    public void TheEnglishIsWhatTheScreensAlwaysSaid()
    {
        Assert.Equal("OPTIONS", Text.Of("options.title"));
        Assert.Equal("Slot 2 can't be read", TitleNotice.ForSlot(2, SaveLoad.Damaged)!.Heading);
        Assert.Equal("It will be saved in slot 3. The save you have is kept as it is.", TitleScreen.NewGameBlurb(3, 1));
        Assert.Equal("It will be saved in slot 3. The saves you have are kept as they are.", TitleScreen.NewGameBlurb(3, 2));
        Assert.Equal("It will be saved in slot 3. The saves you have are kept as they are.", TitleScreen.NewGameBlurb(3, 0));
        Assert.Equal(new[] { "KEEP PLAYING", "SAVE AND QUIT", "QUIT" }, StartMenu.QuitAnswers);
        Assert.Equal("1280 × 720", Text.Of("options.windowSize.value", ("width", 1280), ("height", 720)));
        // N1b: the bag, the shop, the party and the names of the game's enums
        Assert.Equal(new[] { "BUY", "SELL", "SEE YA!" }, ShopScreen.Choices);
        Assert.Equal("Sold a Potion for 150.", Text.Of("shop.sold", ("n", 1), ("item", "Potion"), ("money", 150)));
        Assert.Equal("Sold 3 × Potion for 450.", Text.Of("shop.sold", ("n", 3), ("item", "Potion"), ("money", 450)));
        Assert.Equal("Choose 3 Pokémon to enter. (2 to go)", Text.Of("party.prompt.enter", ("count", 3), ("left", 2)));
        Assert.Equal("PSN", Words.Status(StatusCondition.Toxic));
        Assert.Equal("", Words.Status(StatusCondition.None));
        Assert.Equal("Sp. Atk", Words.Stat(StatType.SpAttack));
        Assert.Equal("Quirky", Words.Nature(Nature.Quirky));
        Assert.Equal("9 March 2026", Words.Date(new DateTime(2026, 3, 9)));
        Assert.Equal(new DateTime(2026, 3, 9).ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture), Words.Date(new DateTime(2026, 3, 9)));
        Assert.All(Enum.GetValues<PokemonType>(), t => Assert.Equal(t.ToString(), Words.Type(t)));
        Assert.All(Enum.GetValues<Nature>(), n => Assert.Equal(n.ToString(), Words.Nature(n)));
        // N1c: the Pokédex, the records, the PC, the starters and saving
        Assert.Equal(new[] { "Number", "A to Z", "Heaviest" }, new[] { PokedexOrder.Number, PokedexOrder.Alphabet, PokedexOrder.Heaviest }.Select(PokedexSearch.NameOf));
        Assert.All(PokedexSearch.Shapes, shape => Assert.Equal(shape, PokedexSearch.NameOfShape(shape)));
        Assert.Equal("Any", PokedexSearch.NameOfGroup(-1));
        Assert.Equal("Grass: morning, night · Surfing · Old, Good Rods",
            ModernUi.WaysOf(HabitatWays.Morning | HabitatWays.Night | HabitatWays.Surfing | HabitatWays.OldRod | HabitatWays.GoodRod));
        Assert.Equal("Grass, all day · Super Rod", ModernUi.WaysOf(HabitatWays.Morning | HabitatWays.Day | HabitatWays.Night | HabitatWays.SuperRod));
        Assert.Equal("No. 007", ModernUi.DexNumber(7));
        Assert.Equal("10.5 kg", ModernUi.Kilograms(10.5f));
        Assert.Equal("Bidoof lives in one place.", Text.Of("pokedex.livesIn", ("name", "Bidoof"), ("n", 1)));
        Assert.Equal(Enumerable.Range(0, PcBoxes.WallpaperNames.Count).Select(i => PcBoxes.WallpaperNames[i]), Enumerable.Range(0, PcBoxes.WallpaperNames.Count).Select(i => Text.Of($"boxes.wallpaper.{i}")));
        Assert.Equal("Monday", Words.Weekday(new DateTime(2026, 10, 12)));
        Assert.Equal("9 Mar 2026", Words.ShortDate(new DateTime(2026, 3, 9)));
        Assert.Equal(new[] { "SAVE", "CANCEL" }, SaveScreen.Answers);
        Assert.Equal("SLOT 2", ModernUi.SlotLabel(2));
        // N1d1: the Town Map, the Pokétch, help, the wardrobe and the Hall of Fame's scene
        Assert.Equal("October", Words.Month(new DateTime(2026, 10, 12)));
        Assert.Equal("GREEN", ColorChangerApp.Name(0));
        Assert.Equal("FAI", HelpScreen.ShortName(PokemonType.Fairy));
        var help = new HelpScreen();
        help.Open();
        Assert.Equal(("Super effective", "A Fire move against a Grass Pokémon does twice the damage."), help.Describe());
        Assert.Equal("Your Own Bottoms", ModernUi.GarmentName(ClothingSlot.Bottoms, null));
        Assert.Equal(new[] { "BUY", "NO" }, WardrobeScreen.BuyChoices);
        Assert.Equal("Met at Wayward Cave, at Lv. 62.", Text.Of("ceremony.metAt", ("place", "Wayward Cave"), ("level", 62)));
        // N1d3: the Underground and its mining game
        Assert.Equal("The wall rings hollow in one place!", Text.Of("digging.ringsHollow", ("n", 1)));
        Assert.Equal("The wall rings hollow in 3 places!", Text.Of("digging.ringsHollow", ("n", 3)));
        Assert.Equal("Red 4", Text.Of("belowGround.price.Red", ("size", 4)));
        Assert.Equal(new[] { "HIS WARES", "YOUR TREASURES" }, new[] { UndergroundScreen.TabName(UndergroundTab.Wares), UndergroundScreen.TabName(UndergroundTab.YourTreasures) });
        Assert.Equal("PUT IN BAG", UndergroundScreen.ActionName(UndergroundAction.PutInBag));
        Assert.Equal(new[] { "DECORATE", "PUT AWAY", "MOVE", "DONE" }, DecoratingSession.MenuRows);
        Assert.Equal("OUT: 3 OF 10", Text.Of("decorating.countOf", ("out", 3), ("limit", 10)));
        // N1d2: the contest condition, Poffins, Ball Capsules and Seals, the Fashion Case, the ribbons and the Super Contest
        Assert.Equal(new[] { "GIVE", "TRASH", "CANCEL" }, PoffinCaseScreen.ActionNames);
        Assert.Equal("Bidoof gobbled up the Poffin happily!", PoffinCaseScreen.EatenLine("Bidoof", PoffinTaste.Liked));
        Assert.Equal(new[] { "ACCESSORIES", "BACKDROPS" }, FashionCaseScreen.TabNames);
        Assert.Equal("ATTACH", SealCaseScreen.Label(CapsuleAction.Attach));
        Assert.Equal(new[] { "1 heart", "0 hearts", "3 hearts" }, new[] { 1, 0, 3 }.Select(n => Text.Of("contest.hearts", ("n", n))));
        Assert.Equal("It has won 2 ribbons.", Text.Of("ribbonPage.won", ("n", 2)));
        Assert.Equal("1 time", Text.Of("cooking.times", ("n", 1)));
        // N1e: the lines the contest's, the ribbons', the Poffins' and the secret base's rules hand their screens
        Assert.Equal(new[] { "Mild Poffin", "Sour-Bitter Poffin", "Dry Poffin" }, new[] { PoffinType.Mild, PoffinType.SourBitter, PoffinType.Dry }.Select(Poffins.NameOf));
        Assert.Equal("SPICY", Poffins.NameOf(Flavor.Spicy));
        Assert.Equal("+2 if performed first.", ContestRules.Describe(ContestEffect.FirstPerformanceAdvantage));
        Assert.Equal("", ContestRules.Describe(ContestEffect.None));
        Assert.Equal("For winning a Smart Super Contest at the Ultra Rank.", RibbonRules.Describe("RIBBON_SMART_ULTRA"));
        Assert.Equal("For winning Hoenn's Coolness Contest at the Hyper Rank.", RibbonRules.Describe("RIBBON_HOENN_COOL_HYPER"));
        Assert.Equal("an event", RibbonRules.WaitsFor("RIBBON_CLASSIC"));
        Assert.Equal("The big boulder was taken away. One more can go.", Text.Of("baseRules.boulderTakenMore", ("n", 1)));
        Assert.Equal("The big boulder was taken away. 4 more can go.", Text.Of("baseRules.boulderTakenMore", ("n", 4)));
        // N1e1: what the field's rules hand the screen
        Assert.Equal("Not while someone is travelling with you.", FieldMoveRules.Why(FieldMoveError.Partner));
        Assert.Equal("The Bicycle can't be ridden here.", BicycleRules.Why(BicycleCheck.NotHere));
        Assert.Equal("It won't have any effect.", ItemUseResult.Nothing.Message);
        Assert.Equal("Caught an Eevee on Route 201.", Text.Of("journal.line.caughtAn", ("species", "Eevee"), ("at", " on Route 201")));
        Assert.Equal("{player} found 3 × Potion!", Text.Of("found.found", ("n", 3), ("item", "Potion")));
        Assert.Equal("{player} put it away in the Medicine pocket.", Text.Of("found.putAway", ("n", 1), ("pocket", ScriptRunner.PocketName(ItemPocket.Medicine))));
        Assert.Equal("Confirm, talk, read", InputManager.Bindings[(int)GameAction.Confirm].Name);
        // N1e, part 3: the engine's own notices and the Underground's tunnels
        Assert.Equal("The battery isn't charged enough. 12 steps to go!", Text.Of("engine.vsSeeker.charging", ("steps", 12)));
        Assert.Equal("You dug up the Prism Sphere: size 9. It had grown by 2!",
            Text.Of("tunnels.dugUpGrown", ("sphere", "Prism Sphere"), ("size", 9), ("grew", 2)));
        Assert.Equal("It can't be set in a wall.", Text.Of("tunnels.refused.set.wall"));
        // A placeholder the call names nothing for is left for whoever fills it later
        Assert.Equal("Hello {player}", Text.Fill("Hello {player}"));
    }

    // ------------------------------------------------------------------ plan 17 · N2: the battle's lines and the introduction

    /// <summary>
    /// The battle's rules, whose words are the log's lines (plan 17 · N2): every file of <c>Battle/</c> but the trainer
    /// AI's (which says nothing), those <see cref="AlsoOnTheTable"/> holds as screens, and the two that describe for a
    /// test where the screen and the rules part; and the abilities' table, which gives some of them their lines.
    /// </summary>
    private static IEnumerable<string> TheBattlesRules =>
        Directory.EnumerateFiles(Path.Combine(Engine, "Battle"), "*.cs", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Engine, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(name => !name.StartsWith("Battle/Sim/Ai/", StringComparison.Ordinal) && !AlsoOnTheTable.Contains(name)
                && name is not ("Battle/ConditionLook.cs" or "Battle/FieldLook.cs"))
            .Append("Data/AbilityEffectTable.cs")
            .OrderBy(name => name, StringComparer.Ordinal);

    /// <summary>What may stay in quotes among the battle's rules though it has a word in lower case: the data's own words.</summary>
    private static readonly Dictionary<string, string[]> BattleNotWords = new()
    {
        // Struggle's description, which is the data's like every other move's (plan 17 · N3)
        ["Battle/Sim/BattleCore.Moves.cs"] = new[] { "Used only when no other move has PP left. It also hurts the user." },
        // An ability's name, which the shapes of the later games go by (plan 06 · R19b)
        ["Battle/Sim/BattleCore.Shapes.cs"] = new[] { "Zero to Hero" },
        // The abilities Simple Beam and Entrainment can't replace or hand on, by name (plan 06 · R24)
        ["Battle/Sim/BattleCore.Effects.Later1.cs"] = new[] { "Zero to Hero", "Power of Alchemy" },
        // The later abilities' names, which the formula and the core go by (plan 06 · R27)
        ["Battle/DamageCalculator.Abilities.cs"] = new[] { "Tablets of Ruin", "Vessel of Ruin", "Sword of Ruin", "Beads of Ruin" },
        ["Battle/Sim/BattleCore.Abilities.cs"] = new[] { "Good as Gold" },
        // The rest of them (plan 06 · R27b): Showdown's lists by name, and Receiver's kin
        ["Battle/Effects/LaterAbilityEffects2.cs"] = new[] { "Zero to Hero", "Power of Alchemy" },
        ["Battle/Sim/BattleCore.Abilities2.cs"] = new[] { "Power of Alchemy" }
    };

    [Fact]
    public void TheBattleSaysNothingInQuotesOfItsOwn()
    {
        // A line of the log has words in lower case; the names of the data the rules go by (Griseous Orb, Sleep Talk) don't
        var sections = Sections;
        var said = new List<string>();
        foreach (var name in TheBattlesRules)
        {
            var allowed = BattleNotWords.GetValueOrDefault(name, Array.Empty<string>());
            foreach (var (text, _) in LiteralsOnScreen(Path.Combine(Engine, name)))
            {
                string bare = Regex.Replace(text, @"\{[^}]*\}", "");
                if (LooksLikeAKey(text, sections)) continue;
                if (Regex.IsMatch(text, @"^[a-z][A-Za-z0-9_]*$")) continue;   // a sound's name, an argument's
                if (!Regex.IsMatch(bare, @"(?<!\p{L})\p{Ll}{2,}")) continue;
                if (allowed.Contains(text)) continue;
                said.Add($"{name}: \"{text}\"");
            }
        }
        Assert.NotEmpty(TheBattlesRules);
        Assert.True(said.Count == 0, "Lines in quotes among the battle's rules:\n" + string.Join("\n", said));
    }

    /// <summary>What stands in for each placeholder of a battle's line when it is measured: long names of Platinum's kind.</summary>
    private static string StandIn(string placeholder) => placeholder switch
    {
        "player" or "rival" => "Barbara",
        "trainer" or "trainer2" => "Pokémon Breeder Lorenzo",
        "move" or "newMove" or "bindingMove" or "forgotten" => "Extreme Speed",
        "item" => "Lagging Tail",
        "ability" => "Compound Eyes",
        "team" => "The foe's team",
        "stat" => "evasiveness",
        "condition" => "paralysis",
        "flavour" => "bitter",
        "what" => "Defense and Sp. Def",
        "money" => "65535",
        "exp" => "99999",
        "level" => "100",
        "n" or "count" or "hits" or "magnitude" or "stockpile" or "taken" or "amount" => "10",
        "species" => "Kangaskhan",
        "pick" => "Electric",
        "some" => "Foe Kangaskhan",
        "last" => "Foe Kangaskhan",
        "class" => "Pokémon Breeder",
        "name" => "Lorenzo",
        _ => "Foe Kangaskhan"
    };

    [Fact]
    public void EveryEnglishBattleLineFitsTheMessageBoxsTwoLines()
    {
        // The battle's box shows two lines of a message (style guide, "Battle panels"); this is the floor a translation is held to
        var keys = Text.EnglishKeys.Where(k => k.StartsWith("battle.", StringComparison.Ordinal)).OrderBy(k => k).ToList();
        Assert.True(keys.Count > 300, $"{keys.Count} lines");
        var tooLong = new List<string>();
        foreach (var key in keys)
            foreach (long n in new long[] { 1, 10 })
            {
                string line = Text.Of(key, ("n", n));
                foreach (Match hole in Regex.Matches(line, @"\{([A-Za-z0-9]+)\}"))
                    line = line.Replace(hole.Value, StandIn(hole.Groups[1].Value));
                var rows = TextMetrics.Wrap(line, ModernUi.MessageBoxWidth - ModernUi.MessageMargin, ModernUi.MessageSize, UiWeight.ExtraBold);
                if (rows.Count > 2) tooLong.Add($"{key}: {line}");
            }
        Assert.True(tooLong.Count == 0, "Lines that run past the box's two lines:\n" + string.Join("\n", tooLong.Distinct()));
    }

    /// <summary>Plays a battle on the rules alone to its end: the player's lead uses its first move, and whoever faints is replaced.</summary>
    private static BattleCore PlayOut(BattleCore core)
    {
        for (int guard = 0; guard < 200 && core.Request != null; guard++)
        {
            switch (core.Request)
            {
                case ActionRequest asked:
                    core.Submit(asked.Places.Select(place => BattleChoice.Fight(place, 0)).ToList());
                    break;
                case ReplacementRequest replace:
                    var party = replace.Place.Side == BattleSide.Player ? core.PlayerParty : core.Trainers[0].Party;
                    int next = Enumerable.Range(0, party.Count).First(i => !party.Members[i].IsFainted && core.SlotsOf(replace.Place.Side).All(b => b.Pokemon != party.Members[i]));
                    core.Submit(BattleChoice.Switch(replace.Place, next));
                    break;
                case SwitchOfferRequest offer:
                    core.Submit(BattleChoice.Stay(offer.Place));
                    break;
            }
        }
        Assert.Null(core.Request);
        return core;
    }

    private static List<string> LinesOf(BattleCore core) => core.Log.OfType<Said>().Select(s => s.Text).ToList();

    [Fact]
    public void ABattleUnderThePseudoLanguageSaysEveryLineInBrackets()
    {
        try
        {
            Text.Use(Text.Pseudo);
            // A wild battle and a trainer's, played to their ends: every line the log says comes from the table
            var wild = PlayOut(CoreScenario.Wild(Scenario.Mon("Luxray", 40, "Spark", "Bite"), Scenario.Mon("Bidoof", 12, "Tackle"), new BattleRandom(17)));
            var trainer = PlayOut(CoreScenario.Against(
                new[] { Scenario.Mon("Infernape", 45, "Flamethrower", "Close Combat") },
                new[] { Scenario.Mon("Roserade", 30, "Toxic", "Magical Leaf"), Scenario.Mon("Budew", 20, "Growth") }, new BattleRandom(18)));
            foreach (var core in new[] { wild, trainer })
            {
                var lines = LinesOf(core);
                Assert.True(lines.Count > 4, string.Join("\n", lines));
                Assert.All(lines, line => Assert.Matches(@"^\[.*\]$", line));
            }
            Assert.Contains(LinesOf(trainer), line => line.Contains("[[Ace Trainer] Vera]", StringComparison.Ordinal));
            // Plan 17 · N3: the Pokémon and the moves a line names are the language's own names, in brackets as every string is
            Assert.Contains(LinesOf(wild), line => line.Contains("[Luxray]", StringComparison.Ordinal) && line.Contains("[Spark]", StringComparison.Ordinal));
            Assert.Contains(LinesOf(wild), line => line.Contains("Bidoof]", StringComparison.Ordinal));
        }
        finally
        {
            Text.Use(Text.English);
        }
    }

    [Fact]
    public void ARecordedBattleReplaysTheSameInAnotherLanguage()
    {
        // The record is the seed and the answers alone, so a battle played in English replays event for event in
        // Romanian, which says every line in its own words (plan 17 · N6), and to the same end
        // in the pseudo-language, every line its own in brackets
        static BattleCore Battle(BattleRandom rolls)
        {
            var party = new Party();
            party.Add(Scenario.Mon("Staravia", 30, "Wing Attack", "Quick Attack"));
            return new BattleCore(new CoreSetup
            {
                PlayerParty = party, WildPokemon = new List<Pokemon> { Scenario.Mon("Geodude", 28, "Rock Throw", "Tackle") }, Random = rolls,
                Rules = Ruleset.Platinum, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
            });
        }
        var first = Battle(new BattleRandom(20261015));
        first.Start();
        var played = PlayOut(first);
        var record = played.Record!;
        Assert.NotEmpty(record.Answers);
        try
        {
            Text.Use("ro");
            Assert.Equal("ro", Text.Current);
            var again = Battle(new BattleRandom(record.Seed));
            again.Replay(record);
            // Romanian has its own table (plan 17 · N6): the same events to the same end, every line said in Romanian
            Assert.Equal(played.Result, again.Result);
            Assert.Equal(played.Log.Select(e => e.GetType()), again.Log.Select(e => e.GetType()));
            Assert.Equal(LinesOf(played).Count, LinesOf(again).Count);
            Assert.NotEqual(LinesOf(played), LinesOf(again));

            Text.Use(Text.Pseudo);
            var bracketed = Battle(new BattleRandom(record.Seed));
            bracketed.Replay(record);
            Assert.Equal(played.Result, bracketed.Result);
            Assert.All(LinesOf(bracketed), line => Assert.Matches(@"^\[.*\]$", line));
            Assert.Equal(LinesOf(played), LinesOf(bracketed).Select(line => line.Replace("[", "").Replace("]", "")));
        }
        finally
        {
            Text.Use(Text.English);
        }
    }

    [Fact]
    public void TheIntroductionUnderThePseudoLanguageSaysEveryLineInBrackets()
    {
        try
        {
            Text.Use(Text.Pseudo);
            var intro = new IntroScreen();
            intro.Open();
            var said = new List<string>();
            const float frame = 1f / 60f;
            for (int guard = 0; guard < 20000 && intro.Phase != IntroPhase.Done; guard++)
            {
                if (intro.Talking)
                {
                    if (intro.LineComplete)
                    {
                        said.Add(intro.SpokenText);
                        if (intro.Speaker.Length > 0) Assert.Matches(@"^\[.*\]$", intro.Speaker);
                    }
                    intro.PressConfirm();
                }
                else if (intro.Phase is IntroPhase.ChooseLook or IntroPhase.ConfirmLook or IntroPhase.ConfirmName or IntroPhase.ConfirmRival)
                    intro.PressConfirm();
                else if (intro.Phase is IntroPhase.EnterName or IntroPhase.EnterRival)
                {
                    intro.PressStart();
                    intro.PressConfirm();
                }
                intro.Advance(frame);
            }
            Assert.Equal(IntroPhase.Done, intro.Phase);
            Assert.True(said.Count >= 12, string.Join("\n", said));
            Assert.All(said, line => Assert.Matches(@"^\[.*\]$", line));
            Assert.Contains(said, line => line == "[Lucas! That's a fine name.]");
        }
        finally
        {
            Text.Use(Text.English);
        }
    }

    [Fact]
    public void AKeyWithNoEntryShowsItselfInBrackets()
    {
        Assert.Equal("[no.such.key]", Text.Of("no.such.key"));
        Assert.False(Text.Has("no.such.key"));
    }

    [Fact]
    public void TheLanguagesFollowTheirOwnPluralRules()
    {
        var english = Language.Of("en")!;
        Assert.Equal(new[] { "other", "one", "other", "other" }, new long[] { 0, 1, 2, 21 }.Select(english.FormFor));
        var french = new Language { Tag = "fr", Plural = PluralRule.French };
        Assert.Equal(new[] { "one", "one", "other" }, new long[] { 0, 1, 2 }.Select(french.FormFor));
        var none = new Language { Tag = "ja", Plural = PluralRule.None };
        Assert.Equal("other", none.FormFor(1));
        Assert.Equal("1.5", Text.Decimal(1.5, 1));
    }

    [Fact]
    public void OnlyLanguagesThePlayerCanReadAreOffered()
    {
        Assert.Contains(Language.All, l => l.Tag == Text.English);
        Assert.Contains(Language.All, l => l.Tag == Text.Pseudo && l.Pseudo);
        Assert.DoesNotContain(Language.Offered, l => l.Pseudo);
        Assert.All(Language.Offered, l => Assert.Contains(l.Script, Language.DrawnScripts));

        // The options' row goes round what is offered and never lands on the pseudo-language
        var settings = new GameSettings();
        Assert.Equal(Text.English, settings.Language);
        for (int i = 0; i < Language.All.Count + 1; i++)
        {
            OptionsScreen.Change(settings, OptionRow.Language, 1);
            Assert.Contains(Language.Offered, l => l.Tag == settings.Language);
        }
        settings.Language = Text.Pseudo;
        OptionsScreen.Change(settings, OptionRow.Language, 1);
        Assert.NotEqual(Text.Pseudo, settings.Language);
    }

    [Fact]
    public void ThePseudoLanguageBracketsEveryString()
    {
        try
        {
            Text.Use(Text.Pseudo);
            Assert.Equal(Text.Pseudo, Text.Current);
            Assert.Equal("[OPTIONS]", Text.Of("options.title"));
            Assert.Equal("[Delete slot 2?]", TitleScreen.DeleteHeading(2));
            Assert.All(StartMenu.QuitAnswers, a => Assert.Matches(@"^\[.+\]$", a));

            // A tag nobody knows is English
            Text.Use("qq");
            Assert.Equal(Text.English, Text.Current);
            Assert.Equal("OPTIONS", Text.Of("options.title"));
        }
        finally
        {
            Text.Use(Text.English);
        }
    }

    [Fact]
    public void AnEntryOfTheBoyAndTheGirlFollowsThePlayersLook()
    {
        // The forms' reader is the same for every entry: one of the table's own is held, and a look's form is read from the look
        Assert.Equal("Your save is intact.", Text.Of("title.notice.crash.intact", ("n", 1)));
        Assert.Equal("Your saves are intact.", Text.Of("title.notice.crash.intact", ("n", 3)));
        Assert.Equal("girl", Text.FormOfLook(PlayerLook.Girl));
        Assert.Equal("boy", Text.FormOfLook(PlayerLook.Boy));
    }
}
