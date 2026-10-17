using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>A Safari Game as a save keeps it: the balls and the steps left, and how many Pokémon it has caught.</summary>
public sealed record SafariSave(int Balls, int Steps, int Caught = 0);

public class SaveData
{
    /// <summary>
    /// The file's own check (plan 12 · Q1): FNV-1a of the file as written, this field read as <c>null</c>
    /// (<see cref="SaveManager.Seal"/>). It is the first thing in the file, and a file without one (every save from
    /// before) is taken on trust.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyOrder(-1)]
    public string? Checksum { get; set; }

    /// <summary>The <see cref="WorldVersion"/> from which Sinnoh's overworld is one map made from the imported world (plan 01 · M2).</summary>
    public const int ImportedWorld = 1;

    /// <summary>The <see cref="WorldVersion"/> from which Jubilife City is part of that map and no longer one of its own (plan 01 · M5).</summary>
    public const int ImportedJubilife = 2;

    /// <summary>
    /// The <see cref="WorldVersion"/> from which the rooms of the first chapters are laid out on the original's plans
    /// (plan 01 · M11, the last rooms: the Pokémon Centers' ground floors, the Poké Marts, the Trainers' School, the
    /// Pokétch Company and Jubilife TV's lobby). A save from before stood
    /// on the tiles of the hand-made rooms: <see cref="PlaceOn"/> moves it.
    /// </summary>
    public const int RebuiltRooms = 3;

    /// <summary>
    /// The <see cref="WorldVersion"/> from which the last two hand-made places are on the original's plans too (plan
    /// 01 · M11, the last step: the player's house on both floors and Professor Rowan's lab).
    /// </summary>
    public const int RebuiltHome = 4;

    /// <summary>The <see cref="WorldVersion"/> of saves made today.</summary>
    public const int CurrentWorld = RebuiltHome;

    public string PlayerName { get; set; } = "Lucas";

    /// <summary>Which of the two characters the player is. Saves from before the choice existed are the boy.</summary>
    public PlayerLook Look { get; set; }

    /// <summary>The rival's name, given in the introduction (plan 02 · S4). Older saves call him by his own.</summary>
    public string RivalName { get; set; } = PlayerIdentity.DefaultRivalName;

    /// <summary>
    /// What the player wears (plan 11 · C9): a garment of <c>clothes.json</c> in each slot or the look's own. Null in
    /// older saves and where nothing was ever changed: the look's own clothes.
    /// </summary>
    public Outfit? Outfit { get; set; }

    /// <summary>The garments bought at a boutique, by id. Null in older saves: nothing bought.</summary>
    public List<string>? Wardrobe { get; set; }

    /// <summary>The number on the Trainer Card, drawn when the game began. 0 in older saves: one is drawn on loading.</summary>
    public int TrainerId { get; set; }

    /// <summary>The day the adventure began; null in saves from before it was recorded.</summary>
    public DateTime? Started { get; set; }

    /// <summary>
    /// Whose rules the game was saved under: Modern for every save written since plan 06 · R19, when every game came to
    /// be played by them. A save that says Platinum (or nothing, from before the choice existed) was begun under
    /// Platinum's rules and is brought over to the modern ones as it loads; this is read for nothing else.
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public RulesPreset Rules { get; set; }

    public int Money { get; set; } = 3000;
    public float PlayTimeSeconds { get; set; } = 0;
    /// <summary>
    /// The badges as a save from before plan 18 · W2 kept them: Sinnoh's eight as one mask, for whatever region the save
    /// stood in. Read for nothing but <see cref="RegionalBadges"/>, and never written again (nought is left out).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int Badges { get; set; } = 0;

    /// <summary>Each region's badges, a mask by the region's id (plan 18 · W2; <c>StoryState.RegionBadges</c>).</summary>
    public Dictionary<string, int> RegionBadges { get; set; } = new();

    public string CurrentMapName { get; set; } = "Sinnoh";
    public int PlayerGridX { get; set; } = 116;
    public int PlayerGridY { get; set; } = 886;
    public Direction PlayerFacing { get; set; } = Direction.Down;

    /// <summary>How the player was getting about: a save made while surfing loads out on the water.</summary>
    public PokemonPlatinumEngine.Overworld.TravelMode Travel { get; set; }

    /// <summary>
    /// The height the player stood at, which says which level they were on where a tile has two (a bridge and
    /// the water under it). Null in older saves: the player stands on the ground.
    /// </summary>
    public float? PlayerHeight { get; set; }

    /// <summary>
    /// Where Dig and an Escape Rope lead out of the caves: outside the way the player last went into them (plan 02
    /// · S2). Null in older saves, and outside the caves.
    /// </summary>
    public MapSpot? Exit { get; set; }

    /// <summary>
    /// The cells of the region's map the player last passed through, which the Town Map marks (plan 01 · M12;
    /// <see cref="Models.MapTrail.ToSave"/>). Null before any, and in older saves.
    /// </summary>
    public int[]? MapTrail { get; set; }

    /// <summary>The day the clock was last looked at, for Pokérus's days (plan 06 · R10). Null in older saves.</summary>
    public DateTime? LastDay { get; set; }

    /// <summary>The key item kept on the item button (the original's registered item), by name; null when none is.</summary>
    public string? RegisteredItem { get; set; }

    /// <summary>
    /// Whoever travels with the player (plan 02 · S6): the trainer of Platinum's data who battles beside them, and
    /// the person of the map who walks behind, by their key. Null when nobody does, and in older saves.
    /// </summary>
    public string? Partner { get; set; }
    public string? PartnerPerson { get; set; }

    /// <summary>The steps the Repel has left (plan 06 · R11); a save from before has none.</summary>
    public int RepelSteps { get; set; }

    /// <summary>The Pokétch: whether the player has it, its apps and the pedometer's count. Null in older saves.</summary>
    public PoketchSave? Poketch { get; set; }

    /// <summary>A Safari Game under way in the Great Marsh (plan 01 · M7): its balls and steps left. Null when none is.</summary>
    public SafariSave? Safari { get; set; }

    /// <summary>
    /// What the game remembers of its wild Pokémon (plan 06 · R13): the day's numbers, swarms, the Trophy Garden, the
    /// honey trees, the Poké Radar's battery, the roamers. Null in older saves, which are given a new one.
    /// </summary>
    public SpecialEncounters? Encounters { get; set; }

    /// <summary>The berry patches (plan 06 · R14a). Null in older saves, which are given the new game's.</summary>
    public BerryPatches? Berries { get; set; }

    /// <summary>The Poffin Case's Poffins in their places (plan 06 · R14c). Null when it is empty, and in older saves.</summary>
    public List<Poffin>? Poffins { get; set; }

    /// <summary>
    /// The Fashion Case's accessories, each kind's name and how many (plan 06 · R17a). Null in saves from before it:
    /// such a save that has been through the Contest Hall's first scene is given the Glitter Powder that scene gives.
    /// </summary>
    public Dictionary<string, int>? FashionCase { get; set; }

    /// <summary>The Fashion Case's backdrops, by their names in the order they came (plan 06 · R17b). Null with none, and in older saves.</summary>
    public List<string>? Backdrops { get; set; }

    /// <summary>The Seal Case: its loose Seals and its twelve Ball Capsules (plan 06 · R17b). Null while it holds no Seal, and in older saves.</summary>
    public SavedSealCase? SealCase { get; set; }

    /// <summary>The Super Contests entered and won, and the winners' photographs (plan 06 · R17b). Null before any, and in older saves.</summary>
    public Models.SuperContests.ContestRecords? ContestRecords { get; set; }

    /// <summary>
    /// The Underground (plan 06 · R16a): its bag and PC, what lies buried, the base, the records and the day's number.
    /// Null in saves from before it, which are given a new one.
    /// </summary>
    public Models.Underground.UndergroundState? Underground { get; set; }

    /// <summary>
    /// Fossils, the Game Corner, the Villa and Pal Park (plan 06 · R16b): the Coin Case's coins, the six Pokémon that
    /// wait in Pal Park, and four of the original's game records. Null in saves from before them.
    /// </summary>
    public Models.PastimesSave? Pastimes { get; set; }

    /// <summary>
    /// The Battle Frontier (plan 06 · R18a): every facility's streaks, the Battle Points, and the Battle Tower's own
    /// part, a challenge put down between two battles among it. Null in saves from before it, which are given a new one.
    /// </summary>
    public Models.Frontier.FrontierRecords? Frontier { get; set; }

    /// <summary>Solaceon Town's Day Care (plan 06 · R15). Null while it is empty with no cycle under way, and in older saves.</summary>
    public DayCareSave? DayCare { get; set; }

    /// <summary>
    /// Which layout of the world the position refers to. Saves from before the import (0, also what a file
    /// without the field reads as) stood on hand-made maps that no longer exist; <see cref="Place"/> moves them.
    /// </summary>
    public int WorldVersion { get; set; }

    /// <summary>
    /// The <see cref="SaveVersion"/> from which every Pokémon of the player's has its own id (plan 07 · O2,
    /// <see cref="SavedPokemonData.Id"/>): a save from before is given them as it loads (<see cref="SaveMigration"/>).
    /// </summary>
    public const int PokemonIdsGiven = 1;

    /// <summary>The <see cref="SaveVersion"/> of saves made today.</summary>
    public const int CurrentSave = PokemonIdsGiven;

    /// <summary>
    /// The version of the save's own records, beside <see cref="WorldVersion"/> (where its player stands) and
    /// <see cref="StoryVersion"/> (what its story knows): its Pokémon and what is kept of them (plan 07 · O2). 0, what a
    /// file without the field reads as, is every save from before; <see cref="SaveMigration.Upgrade"/> brings one up to
    /// <see cref="CurrentSave"/> as it loads, a step for each version. A later session that changes what a save keeps
    /// raises it and adds its step there.
    /// </summary>
    public int SaveVersion { get; set; }

    /// <summary>
    /// How many ids this game has given its Pokémon (plan 07 · O2): the count the next is made from
    /// (<see cref="Models.PokemonIds"/>). 0 in a save from before, whose ids are counted from nought as it loads.
    /// </summary>
    public uint PokemonIdsIssued { get; set; }

    /// <summary>The link battles' record (plan 07 · O2): wins, losses and draws by friend code. Null while empty, and in older saves.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public LinkBattleRecord? LinkBattles { get; set; }

    // Where a save made on one of the old hand-made maps wakes up in the imported world, and the version from
    // which that map is gone: a name can come back as a map of the world (the lake), so the version decides
    private static readonly Dictionary<string, (int Until, MapSpot Spot)> OldMaps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TwinleafTown"] = (ImportedWorld, new MapSpot("Sinnoh", 116, 886)),
        ["Route201"] = (ImportedWorld, new MapSpot("Sinnoh", 112, 858)),
        ["LakeVerity"] = (ImportedWorld, new MapSpot("LakeVerity", 46, 53, Direction.Up)),
        ["SandgemTown"] = (ImportedWorld, new MapSpot("Sinnoh", 177, 843)),
        ["Route202"] = (ImportedWorld, new MapSpot("Sinnoh", 170, 829)),
        // Outside the door of Jubilife's Pokémon Center
        ["JubilifeCity"] = (ImportedJubilife, new MapSpot("Sinnoh", 180, 777))
    };

    /// <summary>Where the save's player stands on today's maps (a room's own tiles are <see cref="PlaceOn"/>'s to check).</summary>
    public MapSpot Place() =>
        OldMaps.TryGetValue(CurrentMapName, out var moved) && WorldVersion < moved.Until
            ? moved.Spot
            : new MapSpot(CurrentMapName, PlayerGridX, PlayerGridY, PlayerFacing);

    // The rooms laid out again on the original's plans, by the version from which each is so: a save from before
    // stood on the old room's tiles, which mean nothing on the new plan (a tile behind the counter, a wall)
    private static readonly (int From, string[] Rooms)[] RebuiltSince =
    {
        (RebuiltRooms, new[]
        {
            "PokemonCenter", "JubilifePokemonCenter", "OreburghPokemonCenter", "FloaromaPokemonCenter", "EternaPokemonCenter",
            "HearthomePokemonCenter", "SolaceonPokemonCenter", "VeilstonePokemonCenter", "PastoriaPokemonCenter", "CanalavePokemonCenter",
            "CelesticPokemonCenter", "SnowpointPokemonCenter", "SunyshorePokemonCenter", "PokemonLeagueSouthPokemonCenter",
            "FightAreaPokemonCenter", "SurvivalAreaPokemonCenter", "ResortAreaPokemonCenter",
            "PokeMart", "JubilifePokeMart", "OreburghPokeMart", "FloaromaPokeMart", "EternaPokeMart", "HearthomePokeMart", "SolaceonPokeMart",
            "PastoriaPokeMart", "CanalavePokeMart", "SnowpointPokeMart", "SunyshorePokeMart", "FightAreaPokeMart", "SurvivalAreaPokeMart",
            "TrainersSchool", "PoketchCompany", "JubilifeTV1F",
            "RivalHouse", "RivalHouse2F",
        }),
        (RebuiltHome, new[] { "PlayerHouse", "PlayerHouse2F", "RowanLab" }),
    };

    /// <summary>Whether a room has been laid out again since a save of this <see cref="WorldVersion"/> was made.</summary>
    public static bool RebuiltAfter(string room, int worldVersion) =>
        RebuiltSince.Any(set => worldVersion < set.From && set.Rooms.Contains(room, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Where the save's player stands on today's maps, rooms included (plan 01 · M11, the last rooms). One rule for
    /// every room: a save made in a room that has been laid out again since (<see cref="RebuiltAfter"/>), or standing
    /// on a tile of a room where nobody can stand, wakes on the tile the room is come into by (<see cref="ArrivalIn"/>:
    /// inside its door, or beside the stairs that lead up to it), looking the way one comes in. Outdoors and in a
    /// room that is as it was, the save's own tile stands. The room a region's game begins in is come into by the
    /// tile it begins on: a game saved in the player's bedroom before the first step wakes there, where the scene
    /// that the first step starts finds it.
    /// </summary>
    public MapSpot PlaceOn(Func<string, Map?> maps)
    {
        var spot = Place();
        if (maps(spot.Map) is not { IsIndoors: true, IsStreamed: false } room
            || !string.Equals(room.Name, spot.Map, StringComparison.OrdinalIgnoreCase)) return spot;
        bool standable = room.InBounds(spot.X, spot.Y) && !room.IsSolid(spot.X, spot.Y);
        if (standable && !RebuiltAfter(room.Name, WorldVersion)) return spot;
        if (RegionDatabase.RegionOfMap(room.Name)?.Start is { } start && string.Equals(start.Map, room.Name, StringComparison.OrdinalIgnoreCase)
            && room.InBounds(start.X, start.Y) && !room.IsSolid(start.X, start.Y)) return start;
        return ArrivalIn(room, maps) ?? spot;
    }

    /// <summary>
    /// The tile a room is come into by: where the way back in from wherever its first way out leads puts whoever takes
    /// it (the mat inside a door, the landing of a flight of stairs). Null for a room no way leads back into.
    /// </summary>
    public static MapSpot? ArrivalIn(Map room, Func<string, Map?> maps)
    {
        foreach (var way in room.Warps)
        {
            if (maps(way.TargetMap) is not { } beyond || ReferenceEquals(beyond, room)) continue;
            // The way back in: of those that lead here, the one nearest to where this way comes out
            Warp? back = null;
            int nearest = int.MaxValue;
            foreach (var other in beyond.Warps)
            {
                if (!string.Equals(other.TargetMap, room.Name, StringComparison.OrdinalIgnoreCase)) continue;
                int away = Math.Abs(other.SourceX - way.TargetX) + Math.Abs(other.SourceY - way.TargetY);
                if (away < nearest) (back, nearest) = (other, away);
            }
            if (back != null && room.InBounds(back.TargetX, back.TargetY) && !room.IsSolid(back.TargetX, back.TargetY))
                return new MapSpot(room.Name, back.TargetX, back.TargetY, back.TargetFacing);
        }
        return null;
    }

    public List<SavedPokemonData> Party { get; set; } = new();
    /// <summary>The PC as saves before plan 06 · R12 kept it: one list, laid out thirty to a box as it loads.</summary>
    public List<SavedPokemonData> BoxStorage { get; set; } = new();

    /// <summary>The Trainer Card's score (plan 06 · R12).</summary>
    public int TrainerScore { get; set; }

    /// <summary>The Hall of Fame (plan 06 · R12): how many teams have entered, when the first did, and the last thirty.</summary>
    public int HallOfFameTotal { get; set; }
    public DateTime? HallOfFameDebut { get; set; }
    public List<SavedHallOfFameEntry> HallOfFame { get; set; } = new();

    /// <summary>The Journal's pages, newest first (plan 06 · R12).</summary>
    public List<JournalPage> Journal { get; set; } = new();

    /// <summary>The PC's boxes, each Pokémon in its place (plan 06 · R12); null in a save from before.</summary>
    public SavedBoxes? Boxes { get; set; }
    public List<SavedItemData> Inventory { get; set; } = new();
    public List<int> SeenSpecies { get; set; } = new();
    public List<int> CaughtSpecies { get; set; } = new();

    /// <summary>Whether the Pokédex has been upgraded to the National Pokédex (false in older saves).</summary>
    public bool NationalPokedex { get; set; }

    /// <summary>The Pokédexes whose diploma the player has been given: "Sinnoh", "National" (the first two Pokédexes) or the identifier of another (plan 18 · W3).</summary>
    public List<string> Diplomas { get; set; } = new();

    /// <summary>The regional Pokédexes of the regions the player has entered, by identifier (plan 18 · W3); empty in a save from before, which has Sinnoh's only.</summary>
    public List<string> DexesEntered { get; set; } = new();

    /// <summary>Ids of the trainers already beaten; they don't challenge again.</summary>
    public List<string> DefeatedTrainers { get; set; } = new();

    /// <summary>Story flags set so far, including each region's Hall of Fame (see <see cref="Story.StoryState"/>).</summary>
    public List<string> StoryFlags { get; set; } = new();

    // ---- The rest of what the story remembers (plan 02 · S1). A save from before it has none of these, and
    // StoryVersion 0 says so: Story.StoryMigration brings such a save up to date as it is loaded.

    /// <summary>How much of the story the save knows about: <see cref="Story.StoryState.CurrentVersion"/> when it was written.</summary>
    public int StoryVersion { get; set; }

    /// <summary>The story's variables that aren't nought, by name.</summary>
    public Dictionary<string, int> StoryVariables { get; set; } = new();

    /// <summary>The items picked up off the ground and the hidden ones found, by their ids.</summary>
    public List<string> TakenItems { get; set; } = new();

    /// <summary>The people spoken to since the Hallowed Tower last stirred (plan 08 · P12; <c>StoryState.Greeted</c>).</summary>
    public List<string> GreetedPeople { get; set; } = new();

    /// <summary>
    /// The scenes this game has played to their end (plan 12 · Q20; <c>StoryState.ScenesSeen</c>), each by its key:
    /// what may be skipped under the options' <c>Seen</c>. A save from before they were kept has none, and nothing
    /// brings it up to date: its scenes are simply not seen yet.
    /// </summary>
    public List<string> ScenesSeen { get; set; } = new();

    /// <summary>The species taken from the professor's briefcase, and the one the rival took; null until then.</summary>
    public string? PlayerStarter { get; set; }
    public string? RivalStarter { get; set; }

    /// <summary>The story as the save has it.</summary>
    public Story.StorySnapshot ToStory() =>
        new(StoryFlags, StoryVariables, DefeatedTrainers, TakenItems, RegionalBadges(), PlayerStarter, RivalStarter);

    /// <summary>The region the save was made in: that of the map its player stands on today, Sinnoh's for a map no region lists.</summary>
    public string RegionId => RegionDatabase.RegionOfMap(Place().Map)?.Id ?? RegionDatabase.Sinnoh;

    /// <summary>
    /// Each region's badges as the save has them (plan 18 · W2, story version 19). A save from before kept one mask, its
    /// eight bits Sinnoh's order, and it goes to the region the save was made in (<see cref="RegionId"/>, of
    /// <see cref="Place"/>). It is carried here, as the story is read and before <c>StoryMigration.Upgrade</c>, and not
    /// as a step of its own at version 19, because the older steps ask the badges (the Coal Badge for HM06, the Icicle
    /// Badge for Snowpoint City's people, the Beacon Badge for Sunyshore's).
    /// </summary>
    public Dictionary<string, int> RegionalBadges()
    {
        if (RegionBadges.Count > 0 || Badges == 0) return new Dictionary<string, int>(RegionBadges, StringComparer.OrdinalIgnoreCase);
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { [RegionId] = Badges };
    }

    /// <summary>The badges of the region the save was made in, and how many that region has: what its summary shows.</summary>
    public (int Mask, int Of) BadgesHere()
    {
        string region = RegionId;
        int mask = RegionalBadges().FirstOrDefault(b => string.Equals(b.Key, region, StringComparison.OrdinalIgnoreCase)).Value;
        return (mask, RegionDatabase.Get(region)?.BadgeNames.Count ?? 0);
    }
}

/// <summary>
/// The Day Care as a save keeps it (plan 06 · R15): its places, each the Pokémon left and the steps walked since; the
/// Egg found (its personality, nought for none) and the steps of the egg cycle under way.
/// </summary>
public sealed class DayCareSave
{
    public List<SavedDayCarePlace?> Places { get; set; } = new();
    public uint Offspring { get; set; }
    public int StepCounter { get; set; }

    /// <summary>The Day Care as a save; null while there is nothing to keep.</summary>
    public static DayCareSave? From(DayCare dayCare)
    {
        if (dayCare.Count == 0 && dayCare.Offspring == 0 && dayCare.StepCounter == 0) return null;
        var save = new DayCareSave { Offspring = dayCare.Offspring, StepCounter = dayCare.StepCounter };
        for (int i = 0; i < DayCare.Places; i++)
            save.Places.Add(dayCare[i] is { } p ? new SavedDayCarePlace { Pokemon = SavedPokemonData.FromPokemon(p), Steps = dayCare.StepsOf(i) } : null);
        return save;
    }

    /// <summary>Puts a saved Day Care back; null empties it.</summary>
    public static void Restore(DayCareSave? save, DayCare dayCare)
    {
        if (save == null)
        {
            dayCare.Clear();
            return;
        }
        dayCare.Restore(save.Places.Select(p => (p?.Pokemon?.ToPokemon(), p?.Steps ?? 0)).ToList(), save.Offspring, save.StepCounter);
    }
}

public sealed class SavedDayCarePlace
{
    public SavedPokemonData? Pokemon { get; set; }
    public int Steps { get; set; }
}

public class SavedPokemonData
{
    public string SpeciesName { get; set; } = "Turtwig";

    /// <summary>
    /// Its own id (plan 07 · O2, <see cref="Pokemon.Id"/>). Null, left out, for one that has none (a copy the Battle
    /// Frontier keeps while a challenge is put down) and in saves from before, whose Pokémon are given theirs as they
    /// load (<see cref="SaveMigration"/>).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Guid? Id { get; set; }

    /// <summary>
    /// Its species' number in the National Pokédex (plan 03 · D13), kept beside the name: a species is found by its
    /// name, and by this if the data no longer has the name (a name respelt by a later import: an apostrophe, an
    /// accent, a gender sign). 0, left out, in saves from before it was kept.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int DexNumber { get; set; }

    /// <summary>Still an Egg (plan 06 · R15): its friendship is the egg cycles it has left. Left out for a Pokémon.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsEgg { get; set; }

    /// <summary>The form it is in (plan 03 · D11); null for its species' own, and in saves from before forms.</summary>
    public string? Form { get; set; }
    public string Nickname { get; set; } = "";
    public int Level { get; set; } = 5;
    public Gender Gender { get; set; } = Gender.Male;
    public Nature Nature { get; set; } = Nature.Hardy;
    public bool IsShiny { get; set; } = false;
    public int CurrentHP { get; set; } = 20;
    public int Status { get; set; } = 0;
    public int IvHP { get; set; } = 15;
    public int IvAttack { get; set; } = 15;
    public int IvDefense { get; set; } = 15;
    public int IvSpAttack { get; set; } = 15;
    public int IvSpDefense { get; set; } = 15;
    public int IvSpeed { get; set; } = 15;

    /// <summary>Its effort values (plan 06 · R10); 0 in saves from before they were gained.</summary>
    public int EvHP { get; set; }
    public int EvAttack { get; set; }
    public int EvDefense { get; set; }
    public int EvSpAttack { get; set; }
    public int EvSpDefense { get; set; }
    public int EvSpeed { get; set; }
    public int CurrentExp { get; set; } = 0;

    /// <summary>Null in saves from before abilities: the species' first ability is used.</summary>
    public string? Ability { get; set; }
    public string? HeldItem { get; set; }

    /// <summary>Null in saves from before friendship: the species' base friendship is used.</summary>
    public int? Friendship { get; set; }

    /// <summary>Its contest condition (plan 06 · R14c; Beauty was kept before it): 0 in saves from before Poffins.</summary>
    public int Cool { get; set; }
    public int Beauty { get; set; }
    public int Cute { get; set; }
    public int Smart { get; set; }
    public int Tough { get; set; }
    public int Sheen { get; set; }

    /// <summary>Null in saves from before personality values: a new one is rolled.</summary>
    public uint? Personality { get; set; }
    public string? Ball { get; set; }

    /// <summary>The trainer who first had it, when that wasn't the player (plan 06 · R10); left out for the player's own.</summary>
    public TrainerMark? OriginalTrainer { get; set; }

    /// <summary>Its Pokérus byte (<see cref="PokerusRules"/>); 0, left out, for one that never had it.</summary>
    public int Pokerus { get; set; }

    /// <summary>The PC's marks on it (plan 06 · R12), where it was met and on what day at what level, and its language when foreign.</summary>
    public int Marks { get; set; }
    public string? MetLocation { get; set; }
    public int MetLevel { get; set; }
    public DateTime? MetDate { get; set; }
    public string? Language { get; set; }

    /// <summary>Steps, move uses and knock-outs counted toward an evolution; left out when there are none.</summary>
    public Dictionary<string, int>? EvolutionProgress { get; set; }

    /// <summary>The ribbons it has won (plan 06 · R17a), by the original's names; left out for one with none.</summary>
    public List<string>? Ribbons { get; set; }

    /// <summary>The Ball Capsule on its ball (plan 06 · R17b): its number among the case's twelve and its Seals; left out for one with none.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int CapsuleId { get; set; }
    public List<string>? Capsule { get; set; }

    /// <summary>Its Dynamax Level and the Gigantamax Factor (plan 06 · R22); left out for one with neither.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int DynamaxLevel { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool GigantamaxFactor { get; set; }

    /// <summary>Its Tera Type where one was given to it (plan 06 · R23: a type's name or "Stellar"); left out for one that has its first type.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? TeraType { get; set; }
    public List<SavedMoveData> Moves { get; set; } = new();

    public static SavedPokemonData FromPokemon(Pokemon p)
    {
        var saved = new SavedPokemonData
        {
            Id = p.Id != Guid.Empty ? p.Id : null,
            SpeciesName = p.Species.Name,
            DexNumber = p.Species.DexNumber,
            IsEgg = p.IsEgg,
            Form = p.Form,
            Nickname = p.Nickname,
            Level = p.Level,
            Gender = p.Gender,
            Nature = p.Nature,
            IsShiny = p.IsShiny,
            CurrentHP = p.CurrentHP,
            Status = (int)p.Status,
            IvHP = p.IvHP,
            IvAttack = p.IvAttack,
            IvDefense = p.IvDefense,
            IvSpAttack = p.IvSpAttack,
            IvSpDefense = p.IvSpDefense,
            IvSpeed = p.IvSpeed,
            EvHP = p.EvHP,
            EvAttack = p.EvAttack,
            EvDefense = p.EvDefense,
            EvSpAttack = p.EvSpAttack,
            EvSpDefense = p.EvSpDefense,
            EvSpeed = p.EvSpeed,
            CurrentExp = p.CurrentExp,
            Ability = p.AbilityName,
            HeldItem = p.HeldItem?.Name,
            Friendship = p.Friendship,
            Cool = p.Cool,
            Beauty = p.Beauty,
            Cute = p.Cute,
            Smart = p.Smart,
            Tough = p.Tough,
            Sheen = p.Sheen,
            Personality = p.Personality,
            Ball = p.Ball,
            OriginalTrainer = p.OriginalTrainer,
            Pokerus = p.Pokerus,
            Marks = p.Marks,
            MetLocation = p.MetLocation,
            MetLevel = p.MetLevel,
            MetDate = p.MetDate,
            Language = p.Language,
            EvolutionProgress = p.EvolutionProgress.Count > 0 ? new Dictionary<string, int>(p.EvolutionProgress) : null,
            Ribbons = p.Ribbons.Count > 0 ? p.Ribbons.ToList() : null,
            CapsuleId = p.CapsuleId,
            Capsule = p.CapsuleId != 0 ? (p.Capsule ?? BallCapsule.Empty).ToSave() : null,
            DynamaxLevel = p.DynamaxLevel,
            GigantamaxFactor = p.GigantamaxFactor,
            TeraType = p.HasOwnTeraType ? p.TeraType.Name : null
        };

        foreach (var m in p.Moves)
        {
            saved.Moves.Add(new SavedMoveData { MoveName = m.Name, CurrentPP = m.CurrentPP, PPUps = m.PPUps });
        }
        return saved;
    }

    public Pokemon ToPokemon(Ruleset? rules = null)
    {
        var sp = PokemonDatabase.Get(SpeciesName) ?? PokemonDatabase.GetByDex(DexNumber) ?? PokemonDatabase.Get("Turtwig")!;
        // Effort past the most a stat may hold under the rules in force is cut to it: a game brought over from
        // Platinum's rules (255) to the modern ones (252), plan 06 · R19
        int cap = (rules ?? Ruleset.Current).EvStatCap;
        var p = new Pokemon(sp, Level, Gender, Nature, IsShiny)
        {
            // One that was never given a name of its own (older saves wrote the species' name in) has none: it is called what its species is called, in whatever language
            Nickname = Nickname == SpeciesName || Nickname == sp.Name ? "" : Nickname,
            CurrentHP = Math.Min(CurrentHP, 999),
            Status = (StatusCondition)Status,
            IvHP = IvHP,
            IvAttack = IvAttack,
            IvDefense = IvDefense,
            IvSpAttack = IvSpAttack,
            IvSpDefense = IvSpDefense,
            IvSpeed = IvSpeed,
            EvHP = Math.Min(EvHP, cap),
            EvAttack = Math.Min(EvAttack, cap),
            EvDefense = Math.Min(EvDefense, cap),
            EvSpAttack = Math.Min(EvSpAttack, cap),
            EvSpDefense = Math.Min(EvSpDefense, cap),
            EvSpeed = Math.Min(EvSpeed, cap),
            CurrentExp = CurrentExp,
            HeldItem = HeldItem != null ? ItemDatabase.Get(HeldItem) : null,
            Cool = Cool,
            Beauty = Beauty,
            Cute = Cute,
            Smart = Smart,
            Tough = Tough,
            Sheen = Sheen,
            Ball = Ball,
            OriginalTrainer = OriginalTrainer,
            Pokerus = Pokerus,
            Marks = Marks,
            MetLocation = MetLocation,
            MetLevel = MetLevel,
            MetDate = MetDate,
            Language = Language,
            DynamaxLevel = Math.Clamp(DynamaxLevel, 0, 10),
            GigantamaxFactor = GigantamaxFactor
        };
        p.IsEgg = IsEgg;
        if (Models.TeraType.Parse(TeraType) is { } tera) p.TeraType = tera;
        p.Id = Id ?? Guid.Empty;
        if (Form != null) p.RestoreForm(Form);
        if (Ability != null) p.AbilityName = Ability;
        if (Friendship is { } friendship) p.Friendship = friendship;
        if (Personality is { } personality) p.Personality = personality;
        foreach (var (key, count) in EvolutionProgress ?? new()) p.EvolutionProgress[key] = count;
        if (Ribbons != null) p.Ribbons.UnionWith(Ribbons);
        if (CapsuleId is > 0 and <= SealCase.Capsules)
        {
            p.CapsuleId = CapsuleId;
            p.Capsule = BallCapsule.Parse(Capsule);
        }
        p.RecalculateStats();
        p.CurrentHP = Math.Clamp(CurrentHP, 0, p.MaxHP);
        if (p.CurrentHP <= 0)
        {
            p.Status = StatusCondition.Faint;
        }

        if (Moves.Count > 0)
        {
            p.Moves.Clear();
            foreach (var sm in Moves)
            {
                var move = MoveDatabase.Create(sm.MoveName);
                move.PPUps = Math.Clamp(sm.PPUps, 0, 3);
                // A move whose modern PP is fewer (a game brought over from Platinum's rules) keeps no more than its most
                move.CurrentPP = Math.Clamp(sm.CurrentPP, 0, move.MaxPP);
                p.Moves.Add(move);
            }
        }
        return p;
    }
}

public class SavedMoveData
{
    public string MoveName { get; set; } = "Tackle";
    public int CurrentPP { get; set; } = 35;

    /// <summary>The PP Ups it has taken (plan 06 · R11); a save from before has none.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int PPUps { get; set; }
}

public class SavedItemData
{
    public string ItemName { get; set; } = "Poké Ball";
    public int Quantity { get; set; } = 5;
}

/// <summary>
/// What became of a save as it was read (plan 12 · Q1): there was none, it was whole, it was damaged and the one
/// before it was taken from the backup, or it and its backup were both damaged and nothing could be loaded.
/// </summary>
public enum SaveLoad { Ok, None, Recovered, Damaged }

/// <summary>
/// How the game writes and reads a save so that nothing but the save being written can be lost (plan 12 · Q1). A
/// save is written to a temporary file beside it (<c>save1.json.tmp</c>) and moved over the save, which is a rename
/// on every platform, after the old file, if it reads whole, has been copied to its backup (<c>save1.bak</c>); it
/// carries its own <see cref="SaveData.Checksum"/>. Reading it says what happened (<see cref="SaveLoad"/>): a file
/// that is cut short, won't parse or fails its checksum gives way to the backup. Which file a save is has nothing to
/// do with it: every method here takes its path, and the game's own are <see cref="SavePaths"/>' (plan 12 · Q2: a
/// slot's file in the folder <see cref="SavePaths.Current"/> names, through <see cref="SaveSlots"/>). No drawing, no input.
/// </summary>
public static class SaveManager
{
    /// <summary>
    /// The one save an install had before there were slots (plan 12 · Q2), which the first start afterwards makes
    /// slot 1 (<see cref="SaveSlots.AdoptOldSave"/>). The game writes no file of this name any more.
    /// </summary>
    public const string FileName = SavePaths.OldSaveName;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>The backup kept beside a save: <c>save1.json</c>'s is <c>save1.bak</c>.</summary>
    public static string BackupOf(string path) => Path.ChangeExtension(path, ".bak");

    /// <summary>The file a save is written to before it takes the save's place: <c>save1.json.tmp</c>.</summary>
    public static string TempOf(string path) => path + ".tmp";

    /// <summary>
    /// A save as the game writes it to a slot's file, its checksum in it (the harness's <c>saves</c> mode writes
    /// its own the same way: plan 24 · X3). The data's own <see cref="SaveData.Checksum"/> is left as it was.
    /// </summary>
    public static string Write(SaveData data)
    {
        string? kept = data.Checksum;
        data.Checksum = null;
        try { return Seal(JsonSerializer.Serialize(data, Options)); }
        finally { data.Checksum = kept; }
    }

    /// <summary>
    /// A save read as the game reads a slot's file, with no check of its checksum (the tests read the saves of
    /// every version the same way: plan 24 · X3). Throws on a file that isn't JSON.
    /// </summary>
    public static SaveData? Read(string json) => JsonSerializer.Deserialize<SaveData>(json);

    // ------------------------------------------------------------------ the checksum

    private const ulong FnvOffset = 14695981039346656037UL, FnvPrime = 1099511628211UL;

    /// <summary>
    /// FNV-1a, 64 bits, of the file as it is with its checksum read as <c>null</c>. Carriage returns are passed over,
    /// so a file whose line endings a checkout has changed still checks.
    /// </summary>
    private static string ChecksumOf(byte[] utf8, int start, int length)
    {
        ulong hash = FnvOffset;
        hash = Fnv(utf8.AsSpan(0, start), hash);
        hash = Fnv("null"u8, hash);
        hash = Fnv(utf8.AsSpan(start + length), hash);
        return hash.ToString("x16");
    }

    private static ulong Fnv(ReadOnlySpan<byte> bytes, ulong hash)
    {
        foreach (byte b in bytes)
        {
            if (b == (byte)'\r') continue;
            hash ^= b;
            hash *= FnvPrime;
        }
        return hash;
    }

    /// <summary>
    /// Where the value of the file's top-level <c>Checksum</c> stands in its bytes, and what it is (null for
    /// <c>null</c>). False when the file has none; throws on a file that isn't JSON.
    /// </summary>
    private static bool FindChecksum(byte[] utf8, out int start, out int length, out string? value)
    {
        start = length = 0;
        value = null;
        var reader = new Utf8JsonReader(utf8);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            bool mine = reader.ValueTextEquals("Checksum");
            reader.Read();
            if (!mine)
            {
                reader.Skip();
                continue;
            }
            start = (int)reader.TokenStartIndex;
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    length = 4;
                    return true;
                case JsonTokenType.String:
                    // The quotes are the value's too
                    if (utf8[start] != (byte)'"') start--;
                    length = reader.ValueSpan.Length + 2;
                    value = reader.GetString() ?? "";
                    return true;
                default:
                    // Something else where the checksum should be: the file was changed by hand
                    length = reader.ValueSpan.Length;
                    value = "";
                    return true;
            }
        }
        return false;
    }

    /// <summary>Writes a file's checksum into it, where it says <c>null</c>.</summary>
    public static string Seal(string json)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        if (!FindChecksum(utf8, out int start, out int length, out _)) return json;
        string sum = ChecksumOf(utf8, start, length);
        var sealedBytes = new byte[utf8.Length - length + sum.Length + 2];
        utf8.AsSpan(0, start).CopyTo(sealedBytes);
        sealedBytes[start] = (byte)'"';
        Encoding.ASCII.GetBytes(sum).CopyTo(sealedBytes, start + 1);
        sealedBytes[start + 1 + sum.Length] = (byte)'"';
        utf8.AsSpan(start + length).CopyTo(sealedBytes.AsSpan(start + sum.Length + 2));
        return Encoding.UTF8.GetString(sealedBytes);
    }

    /// <summary>
    /// Whether a file is whole: it is JSON, and its checksum, where it has one, is the file's. A file without one (a
    /// save from before) is whole when it parses.
    /// </summary>
    public static bool Intact(string json)
    {
        try
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(json);
            if (!FindChecksum(utf8, out int start, out int length, out string? value))
            {
                using var _ = JsonDocument.Parse(utf8);
                return true;
            }
            if (value != null && !string.Equals(value, ChecksumOf(utf8, start, length), StringComparison.OrdinalIgnoreCase)) return false;
            using var whole = JsonDocument.Parse(utf8);
            return whole.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>A file's save if it is whole and reads as one; null otherwise.</summary>
    private static SaveData? ReadWhole(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return Intact(json) ? Read(json) : null;
        }
        catch (Exception)
        {
            // Whatever is wrong with it, a file that can't be read is damaged: it must never stop the game
            return null;
        }
    }

    // ------------------------------------------------------------------ writing and reading the file

    /// <summary>
    /// Writes a save so that a crash at any moment loses nothing but this save: to the temporary file first, flushed
    /// to the disk, then the old save, if it is whole, copied to the backup, then the temporary file moved over the
    /// save. A damaged save is never copied over a good backup.
    /// </summary>
    public static bool SaveGame(SaveData data, string path)
    {
        string temp = TempOf(path);
        try
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(Write(data));
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(utf8);
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(path) && Intact(File.ReadAllText(path))) File.Copy(path, BackupOf(path), overwrite: true);
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads a save and says what happened: <see cref="SaveLoad.None"/> with no file at all, <see cref="SaveLoad.Ok"/>
    /// with the save whole, <see cref="SaveLoad.Recovered"/> with the save gone or damaged and the backup whole (its
    /// game is the one returned), <see cref="SaveLoad.Damaged"/> with nothing whole to load.
    /// </summary>
    public static (SaveLoad Outcome, SaveData? Save) Load(string path)
    {
        string backup = BackupOf(path);
        bool hasSave = File.Exists(path), hasBackup = File.Exists(backup);
        if (!hasSave && !hasBackup) return (SaveLoad.None, null);
        if (hasSave && ReadWhole(path) is { } save) return (SaveLoad.Ok, save);
        if (hasBackup && ReadWhole(backup) is { } before) return (SaveLoad.Recovered, before);
        return (SaveLoad.Damaged, null);
    }
}

/// <summary>The PC's boxes in a save (plan 06 · R12): the box the PC was left on, the wallpapers unlocked, and each box's name, wallpaper and Pokémon by place.</summary>
public class SavedBoxes
{
    public int CurrentBox { get; set; }
    public int UnlockedWallpapers { get; set; }
    public List<SavedBox> Boxes { get; set; } = new();

    public static SavedBoxes From(PcBoxes pc) => new()
    {
        CurrentBox = pc.CurrentBox,
        UnlockedWallpapers = pc.UnlockedWallpapers,
        Boxes = pc.Boxes.Select(b => new SavedBox
        {
            Name = b.Name,
            Wallpaper = b.Wallpaper,
            Pokemon = b.Slots.Select((p, slot) => (p, slot)).Where(t => t.p != null)
                .Select(t => new SavedBoxedPokemon { Slot = t.slot, Pokemon = SavedPokemonData.FromPokemon(t.p!) }).ToList()
        }).ToList()
    };

    public PcBoxes ToBoxes()
    {
        // As many boxes as the save had: Platinum's eighteen, or the National Pokédex's (plan 03 · D13)
        var pc = new PcBoxes(Boxes.Count) { UnlockedWallpapers = UnlockedWallpapers };
        pc.CurrentBox = Math.Clamp(CurrentBox, 0, pc.Boxes.Count - 1);
        for (int i = 0; i < Math.Min(Boxes.Count, pc.Boxes.Count); i++)
        {
            var saved = Boxes[i];
            pc.Rename(i, saved.Name);
            if (pc.HasWallpaper(saved.Wallpaper)) pc.Boxes[i].Wallpaper = saved.Wallpaper;
            foreach (var boxed in saved.Pokemon)
                if (boxed.Slot is >= 0 and < PcBoxes.BoxSize && pc[i, boxed.Slot] == null)
                    pc.Boxes[i].Slots[boxed.Slot] = boxed.Pokemon.ToPokemon();
        }
        return pc;
    }
}

public class SavedBox
{
    public string Name { get; set; } = "";
    public int Wallpaper { get; set; }
    public List<SavedBoxedPokemon> Pokemon { get; set; } = new();
}

public class SavedBoxedPokemon
{
    public int Slot { get; set; }
    public SavedPokemonData Pokemon { get; set; } = new();
}

/// <summary>A team of the Hall of Fame in a save (plan 06 · R12).</summary>
public class SavedHallOfFameEntry
{
    public DateTime Date { get; set; }
    public List<HallOfFameMember> Team { get; set; } = new();

    public static SavedHallOfFameEntry From(HallOfFameEntry entry) => new() { Date = entry.Date, Team = entry.Team.ToList() };
    public HallOfFameEntry ToEntry() => new(Date, Team);
}
