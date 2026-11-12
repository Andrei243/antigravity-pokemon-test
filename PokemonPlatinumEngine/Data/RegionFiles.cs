using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Data;

/// <summary>The boy and the girl a region's game is played as, as <c>CharacterModels</c> knows them.</summary>
public sealed class RegionHeroes
{
    public string Boy { get; set; } = "PLAYER";
    public string Girl { get; set; } = "DAWN";
}

/// <summary>The rival's name by the player's look, where the rival is the region's other hero (plan 21 · H2).</summary>
public sealed class RegionRivals
{
    /// <summary>The rival of a player who is the boy (Hoenn's May).</summary>
    public string? Boy { get; set; }

    /// <summary>The rival of a player who is the girl (Hoenn's Brendan).</summary>
    public string? Girl { get; set; }
}

/// <summary>The professor of a region's introduction: who, how he is drawn, the Pokémon he shows and his lines.</summary>
public sealed class RegionProfessor
{
    public string Name { get; set; } = "";

    /// <summary>The character type <c>CharacterStudio</c> draws (plan 11 · C5, C6 give each professor a look of his own).</summary>
    public string Look { get; set; } = "ROWAN";

    /// <summary>The Pokémon he shows the player.</summary>
    public string Species { get; set; } = "";

    public RegionIntroLines Lines { get; set; } = new();
}

/// <summary>
/// The professor's lines in the introduction, in their order; <c>{name}</c> is the player's and <c>{rival}</c> the
/// rival's. Hand-written per region, in the game's own words (plan 18 · W1).
/// </summary>
public sealed class RegionIntroLines
{
    public string Hello { get; set; } = "";
    public string Welcome { get; set; } = "";
    public string MyName { get; set; } = "";
    public string World { get; set; } = "";
    public string OneOfThem { get; set; } = "";
    public string LiveAlongside { get; set; } = "";
    public string Study { get; set; } = "";
    public string Enough { get; set; } = "";
    public string BoyOrGirl { get; set; } = "";
    public string AskName { get; set; } = "";
    public string NextDoor { get; set; } = "";
    public string NeverStill { get; set; } = "";
    public string FineName { get; set; } = "";
    public string OfCourse { get; set; } = "";
    public string Story { get; set; } = "";
    public string OffYouGo { get; set; } = "";
}

/// <summary>One of the places a region's trainer flies to and comes back to (<see cref="SpawnLocation"/>'s shape, as the file writes it).</summary>
public sealed class RegionSpawn
{
    public int Id { get; set; }
    public string Area { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public string Room { get; set; } = "";
    public bool UnlockedOnArrival { get; set; } = true;
    public string? Flag { get; set; }

    /// <summary>
    /// The map the place lies on where it is not the region's overworld (plan 21 · H8, part c: Sootopolis City, inside its
    /// crater on a map of its own); left out for the overworld.
    /// </summary>
    public string? Map { get; set; }
}

/// <summary>
/// What <c>Data/regions/&lt;id&gt;.json</c> holds (plan 18 · W1): the content of a region the engine used to read by
/// name. The chain (the order, the links, the maps) stays code in <see cref="RegionDatabase"/>.
/// </summary>
public sealed class RegionFile
{
    public string Id { get; set; } = "";

    /// <summary>The folder of the region's world and of its music (<c>world/sinnoh</c>, <c>music/sinnoh</c>).</summary>
    public string Folder { get; set; } = "";

    /// <summary>The catalogue of models its world's props are looked up in (<see cref="WorldModels.Catalogue"/>).</summary>
    public string Models { get; set; } = "";

    /// <summary>The map Fly, Teleport and the way out noted for Dig and an Escape Rope are on: the region's overworld.</summary>
    public string Map { get; set; } = "";

    /// <summary>The regional Pokédex it plays by, a PokeAPI identifier (<c>extended-sinnoh</c>); plan 18 · W3 reads it.</summary>
    public string Dex { get; set; } = "";

    /// <summary>
    /// The regional Pokédexes it plays by when it has several (plan 23 · Z4: Kalos's Central, Coastal and Mountain ones), in
    /// the order the Pokédex lists them; empty for a region with one, which is <see cref="Dex"/> alone. <see cref="Dex"/>
    /// is the first of them, the one the player's own region shows first.
    /// </summary>
    public List<string>? Dexes { get; set; }

    /// <summary>Which rule opens the National Pokédex (<c>HallOfFameAndSeenAll</c>, <c>Caught60</c>); plan 18 · W3 reads it.</summary>
    public string NationalUnlock { get; set; } = "";

    /// <summary>Whether a new game here begins with the region's story (Sinnoh's: the player's room, with nothing).</summary>
    public bool HasStory { get; set; }

    /// <summary>
    /// Whether the region's places change with the seasons (plan 22 · U2: Unova's, whose wild Pokémon and some of whose
    /// chunks go by the month, <see cref="AreaSeason"/>). No other region has seasons: plan 22 · U3 hands the season to the look and the sign, and U4
    /// to the chunks, the wild Pokémon and Deerling's coats.
    /// </summary>
    public bool HasSeasons { get; set; }

    /// <summary>
    /// Whether the region's people get about on roller skates (plan 23 · Z1: Kalos's): once the skates are had
    /// (<see cref="Overworld.SkateRules.Flag"/>) the run button puts them on (<see cref="Overworld.TravelMode.Skating"/>).
    /// </summary>
    public bool Skates { get; set; }

    /// <summary>
    /// How the region's challenge is taken (plan 23 · Z6): <c>"trials"</c> for Alola's island challenge, whose
    /// <see cref="Badges"/> are the grand trials' stamps (the Trainer Card says STAMPS); left out for Gyms and badges.
    /// </summary>
    public string? Challenge { get; set; }

    /// <summary>The region's badges are an island challenge's stamps (<see cref="Challenge"/> is <c>trials</c>).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Stamps => string.Equals(Challenge, "trials", StringComparison.OrdinalIgnoreCase);

    /// <summary>The name the introduction offers for the rival; null where the region's rival is its other hero.</summary>
    public string? Rival { get; set; }

    /// <summary>
    /// Where the rival is the hero the player isn't (Hoenn's May and Brendan, plan 21 · H2): the rival's name for a
    /// player who is the boy and for one who is the girl. Null where <see cref="Rival"/> names a friend.
    /// </summary>
    public RegionRivals? Rivals { get; set; }

    public RegionHeroes Heroes { get; set; } = new();
    public List<string> Starters { get; set; } = new();

    /// <summary>The names of the region's badges, in the order of its Trainer Card (none to eight; plan 18 · W2 reads them).</summary>
    public List<string> Badges { get; set; } = new();

    /// <summary>The badge each field move asks for here, by the move's name (plan 18 · W2 reads them).</summary>
    public Dictionary<string, string> Gates { get; set; } = new();

    public RegionProfessor Professor { get; set; } = new();
    public List<RegionSpawn> Spawns { get; set; } = new();

    /// <summary>
    /// The region's levels for a team that crossed into it (plan 18 · W4): pairs of a level of the region's own game and
    /// the level it is met at, <c>[[2, 62], [63, 104]]</c> (<see cref="LevelMapping"/>). None for Sinnoh, whose levels
    /// are Platinum's and where every game begins.
    /// </summary>
    public List<int[]>? Levels { get; set; }

    /// <summary>
    /// The region's pasture (plan 35 · BN5): the map of its copy of the meadow every PC leads to, one of its own
    /// <see cref="Maps"/>. None for a region without one, whose PCs offer none.
    /// </summary>
    public string? Pasture { get; set; }

    /// <summary>
    /// The level cap by Badges held (plan 13 · V3; plan 18 · W4b-1), in the region's own game's levels: index 0 the cap
    /// with none, the last the cap with every Badge (the Champion's). <c>LevelCaps.TableOf</c> reads it and maps it by
    /// <see cref="Levels"/> where the region is crossed into. None where the region has no table yet.
    /// </summary>
    public List<int>? Caps { get; set; }

    /// <summary>
    /// How the player moves here (plan 30 · FM1): four ways on the grid when left out, as every region before Kalos.
    /// <see cref="Region.Movement"/> reads it, never this field.
    /// </summary>
    public RegionMovement? Movement { get; set; }

    /// <summary>
    /// The manner its battles are filmed in (plan 45 · BC1; style guide, "The battle's manners"): a name of
    /// <c>Graphics.CameraManners</c>' table (<c>"sinnoh"</c>, <c>"galar"</c>...), Sinnoh's when left out. The options'
    /// BATTLE CAMERA row decides whether it is used; a name the table lacks is refused by the pack check and the tests.
    /// </summary>
    public string? BattleCamera { get; set; }

    /// <summary>The body kit the region's people are drawn in (plan 30 · FM4 builds the grown one); chibi when left out.</summary>
    public CharacterKit Kit { get; set; } = CharacterKit.Chibi;

    /// <summary>
    /// The sound the region's music plays in (plan 31 · MU2): <c>"modern"</c>, or <c>"ds"</c> and left out for the DS
    /// sound every region before Kalos has. A song's own header <c>sound</c> wins over it (<see cref="Audio.Song.PlaysIn"/>).
    /// </summary>
    public string? Sound { get; set; }

    /// <summary>
    /// <see cref="Sound"/> read: the modern sound, the DS one, or null when the file names none. Anything but
    /// <c>modern</c> and <c>ds</c> is refused, so a misspelling can't leave a region in the wrong sound unnoticed.
    /// </summary>
    public Audio.SynthSound? MusicSound => Sound?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "modern" => Audio.SynthSound.Modern,
        "ds" => Audio.SynthSound.Ds,
        _ => throw new FormatException($"region {Id}: 'sound' is 'modern' or 'ds', not '{Sound}'")
    };

    /// <summary>
    /// What the region's outdoors are walked on (plan 32 · OW1): <c>"open"</c>, the open world's engine (Galar's and
    /// Paldea's, once their files are written), or <c>"grid"</c> and left out for the tile grid every region before them
    /// has. Its rooms stay grid maps either way (plan 32 · decision 6). <see cref="Region.Field"/> reads it, never this.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// <see cref="Field"/> read: the open world or the grid, the grid when the file names none. Anything but <c>open</c>
    /// and <c>grid</c> is refused, so a misspelling can't leave a region on the wrong field unnoticed.
    /// </summary>
    public FieldKind FieldKind => Field?.Trim().ToLowerInvariant() switch
    {
        null or "" or "grid" => FieldKind.Grid,
        "open" => FieldKind.Open,
        _ => throw new FormatException($"region {Id}: 'field' is 'grid' or 'open', not '{Field}'")
    };

    // What a region a pack adds says of its place in the chain (plan 16 · T14, the last part; docs/packs.md, "A region
    // of your own"). The game's own regions are code in RegionDatabase and their files leave these out; a pack's region
    // has no code, so its file says them all. RegionDatabase reads them only for a region no game region is called.

    /// <summary>A pack's region: its name as the game shows it (the id when left out).</summary>
    public string? Name { get; set; }

    /// <summary>A pack's region: the generation it stands for, nought when left out (no generation of the games').</summary>
    public int? Generation { get; set; }

    /// <summary>A pack's region: where a new game begun there (<c>--region</c>) wakes.</summary>
    public RegionSpot? Start { get; set; }

    /// <summary>A pack's region: where a Champion who crosses in lands; the start when left out.</summary>
    public RegionSpot? Arrival { get; set; }

    /// <summary>A pack's region: every map that belongs to it, its overworld and its rooms.</summary>
    public List<string>? Maps { get; set; }

    /// <summary>
    /// A pack's region: the region it joins the chain after, which must be the last region with maps (the game's or an
    /// earlier pack's). Left out, the region is no part of the chain: nothing sails there, though its maps are its own.
    /// </summary>
    public string? After { get; set; }

    /// <summary>A pack's region: the way to it from <see cref="After"/>, with where it leaves from and who takes the player.</summary>
    public RegionLinkFile? Link { get; set; }
}

/// <summary>A place a pack's region file names: a map, a tile and the way the player faces.</summary>
public sealed class RegionSpot
{
    public string Map { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Facing { get; set; } = Direction.Down;

    public MapSpot ToSpot() => new(Map, X, Y, Facing);
}

/// <summary>
/// The way to a pack's region from the region it joins after (plan 16 · T14): how it goes, the attendant's word for
/// it, the map of the region before where it leaves from, and the attendant who stands there, laid onto that map by
/// the game as the maps load (a person of a room's own shape, <see cref="MapFile.NpcRecord"/>, with
/// <c>isTransportAttendant</c>), so a pack never copies a file of the game's region to put someone on it.
/// </summary>
public sealed class RegionLinkFile
{
    public Transport Transport { get; set; } = Transport.Boat;
    public string? Vessel { get; set; }
    public string? DepartureMap { get; set; }
    public MapFile.NpcRecord? Attendant { get; set; }
}

/// <summary>Which ways the player may step (plan 30): the grid's four, eight on the grid (stage 1), or free (stage 2).</summary>
public enum MovementDirections { Four, Eight, Free }

/// <summary>How fast the player goes (plan 30): Platinum's paces by button, or the stick's tilt (stage 2 only).</summary>
public enum MovementSpeed { Paces, Analog }

/// <summary>A region's own way of travelling beyond on foot, surfing, cycling and diving (plan 30, "The setting").</summary>
public enum RegionMode { Skates, Ride, RotomBike, Mount }

/// <summary>
/// Which field engine a region's outdoors are walked on (plan 32): the tile grid of every region from Sinnoh to Alola,
/// or the open world in metres of Galar and Paldea.
/// </summary>
public enum FieldKind { Grid, Open }

/// <summary>The body kit people are drawn in (plan 30, decision 12): the chibi kit, or the grown one from Kalos on.</summary>
public enum CharacterKit { Chibi, Grown }

/// <summary>
/// A region's (or a place's) word on movement (plan 30 · FM1, <c>movement</c> in a region file or a <c>world.json</c>
/// map entry). Every field left out is the four-way grid's: a record with nothing in it says what no record says.
/// GPU-free.
/// </summary>
public sealed record RegionMovement
{
    /// <summary>The four-way grid of <c>player_move.c</c>, at Platinum's paces, with no mode of the region's own.</summary>
    public static readonly RegionMovement Grid = new();

    public MovementDirections Directions { get; init; } = MovementDirections.Four;
    public MovementSpeed Speed { get; init; } = MovementSpeed.Paces;

    /// <summary>The region's own modes of travel; null where the record says nothing of them (a place's record then keeps the region's).</summary>
    public List<RegionMode>? Modes { get; init; }

    /// <summary>Whether the record lists a mode.</summary>
    public bool Offers(RegionMode mode) => Modes?.Contains(mode) == true;

    /// <summary>Whether this is the grid's own word: four ways, the paces, no mode.</summary>
    public bool IsGrid => Directions == MovementDirections.Four && Speed == MovementSpeed.Paces && (Modes == null || Modes.Count == 0);

    /// <summary>
    /// This record with a place's laid over it (a map entry's <c>movement</c>): the place's directions and speed where it
    /// says them, its modes where it lists any (an empty list bars them all). A place's record from JSON always carries
    /// its directions and speed, so a place says the whole of them.
    /// </summary>
    public RegionMovement Under(RegionMovement? place) => place == null ? this : this with
    {
        Directions = place.Directions,
        Speed = place.Speed,
        Modes = place.Modes ?? Modes
    };

    /// <summary>
    /// What the options' MOVEMENT row makes of this (plan 30, decision 6): <c>Four ways</c> takes the grid's four
    /// directions and its paces and keeps the region's modes; <c>As the region</c> keeps the record.
    /// </summary>
    public RegionMovement AsChosen(Core.MovementChoice choice) => choice == Core.MovementChoice.FourWays
        ? this with { Directions = MovementDirections.Four, Speed = MovementSpeed.Paces }
        : this;
}

/// <summary>The kinds of rule that open the National Pokédex (plan 18 · W3).</summary>
public enum NationalUnlockRule
{
    /// <summary>The region's Hall of Fame, and every species of its regional Pokédex seen (Platinum's rule).</summary>
    HallOfFameAndSeenAll,

    /// <summary>A number of species caught (Kanto's: sixty).</summary>
    Caught,

    /// <summary>The region's Hall of Fame alone.</summary>
    HallOfFame
}

/// <summary>
/// A region's rule for the National Pokédex as its file writes it: <c>HallOfFameAndSeenAll</c>, <c>HallOfFame</c> or
/// <c>Caught60</c> (the rule's name with its number after it). An empty or unknown text is the Hall of Fame alone.
/// </summary>
public readonly record struct NationalUnlock(NationalUnlockRule Rule, int Count)
{
    public static NationalUnlock Parse(string? text)
    {
        text = text?.Trim() ?? "";
        if (text.Equals("HallOfFameAndSeenAll", StringComparison.OrdinalIgnoreCase)) return new(NationalUnlockRule.HallOfFameAndSeenAll, 0);
        if (text.StartsWith("Caught", StringComparison.OrdinalIgnoreCase) && int.TryParse(text.AsSpan(6), out int count) && count > 0)
            return new(NationalUnlockRule.Caught, count);
        return new(NationalUnlockRule.HallOfFame, 0);
    }
}

/// <summary>Reads the region files.</summary>
public static class RegionFiles
{
    /// <summary>The file of a region, by its id; null when the region has none (Johto and the rest, until their plans).</summary>
    public static RegionFile? Load(string id)
    {
        string relative = $"regions/{id.ToLowerInvariant()}.json";
        return System.IO.File.Exists(GameDataFiles.PathOf(relative)) ? GameDataFiles.Load<RegionFile>(relative) : null;
    }
}
