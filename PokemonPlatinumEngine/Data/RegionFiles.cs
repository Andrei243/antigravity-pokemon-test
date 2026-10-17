using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Data;

/// <summary>The boy and the girl a region's game is played as, as <c>CharacterModels</c> knows them.</summary>
public sealed class RegionHeroes
{
    public string Boy { get; set; } = "PLAYER";
    public string Girl { get; set; } = "DAWN";
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

    /// <summary>Which rule opens the National Pokédex (<c>HallOfFameAndSeenAll</c>, <c>Caught60</c>); plan 18 · W3 reads it.</summary>
    public string NationalUnlock { get; set; } = "";

    /// <summary>Whether a new game here begins with the region's story (Sinnoh's: the player's room, with nothing).</summary>
    public bool HasStory { get; set; }

    /// <summary>The name the introduction offers for the rival; null where the region's rival is its other hero.</summary>
    public string? Rival { get; set; }

    public RegionHeroes Heroes { get; set; } = new();
    public List<string> Starters { get; set; } = new();

    /// <summary>The names of the region's badges, in the order of its Trainer Card (none to eight; plan 18 · W2 reads them).</summary>
    public List<string> Badges { get; set; } = new();

    /// <summary>The badge each field move asks for here, by the move's name (plan 18 · W2 reads them).</summary>
    public Dictionary<string, string> Gates { get; set; } = new();

    public RegionProfessor Professor { get; set; } = new();
    public List<RegionSpawn> Spawns { get; set; } = new();
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
