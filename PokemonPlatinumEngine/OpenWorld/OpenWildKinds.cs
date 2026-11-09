using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// How a wild Pokémon of an open map takes the player (plan 32 · OW7; the originals' three temperaments and a fourth
/// that pays no heed, ours by species' data): what it does once it has seen or smelt the player.
/// </summary>
public enum OpenTemperament
{
    /// <summary>Pays the player no heed: it wanders its zone and is met only when walked into.</summary>
    Calm,

    /// <summary>Runs from the player once it has seen or smelt them.</summary>
    Shy,

    /// <summary>Comes over to look, and stands a few metres off watching for as long as the player is near.</summary>
    Curious,

    /// <summary>Charges the player once it has seen or smelt them, and meets them by running into them.</summary>
    Aggressive
}

/// <summary>
/// A wild Pokémon's senses and ways (plan 32 · OW7), in millimetres and 65,536ths of a turn: how far it sees ahead
/// (<see cref="Sight"/>, to either side by <see cref="HalfAngle"/>, by the cone and the line a trainer's eyes use,
/// <see cref="OpenSight"/>), how near it smells the player whatever lies between (<see cref="Scent"/>, twice as far
/// for a player who runs), and, for a curious one, how near it comes (<see cref="Keep"/>). GPU-free.
/// </summary>
public sealed record OpenWildKind(OpenTemperament Temperament, int Sight, int HalfAngle, int Scent, int Keep)
{
    /// <summary>One that senses nothing and pays no heed.</summary>
    public static readonly OpenWildKind Heedless = new(OpenTemperament.Calm, 0, 0, 0, 0);

    /// <summary>Whether it notices the player at all.</summary>
    public bool Notices => Temperament != OpenTemperament.Calm && (Sight > 0 || Scent > 0);
}

/// <summary>
/// <c>Data/open-wild.json</c> (plan 32 · OW7; ours; <c>docs/data-files.md</c>, "Open worlds"): each temperament's senses,
/// and which species has which. A species named has its own; any other takes the first of its types that has a row,
/// and the default otherwise. Read strictly, as every file a person writes is.
/// </summary>
public sealed class OpenWildFile
{
    /// <summary>Each temperament's senses: a row for every one of the four.</summary>
    [DataRequired] public Dictionary<string, OpenWildKindFile> Kinds { get; set; } = new();

    /// <summary>A species' (or a form's) temperament, by its name in the data.</summary>
    public Dictionary<string, string> Species { get; set; } = new();

    /// <summary>A type's temperament, for a species not named: its first type with a row decides.</summary>
    public Dictionary<string, string> Types { get; set; } = new();

    /// <summary>What a species neither named nor of a type with a row takes.</summary>
    public string Default { get; set; } = nameof(OpenTemperament.Calm);
}

/// <summary>A temperament's senses, in metres and degrees as a person writes them.</summary>
public sealed class OpenWildKindFile
{
    /// <summary>How far it sees ahead, in metres (nought for none).</summary>
    public double Sight { get; set; }

    /// <summary>How wide its sight is, in degrees all told.</summary>
    public double Angle { get; set; }

    /// <summary>How near it smells the player whatever lies between, in metres (nought for none).</summary>
    public double Scent { get; set; }

    /// <summary>How near a curious one comes before it stands and watches, in metres.</summary>
    public double Keep { get; set; }
}

/// <summary>The temperaments of every species as <c>open-wild.json</c> gives them (plan 32 · OW7). GPU-free.</summary>
public sealed class OpenWildKinds
{
    /// <summary>The file's name under the data folder.</summary>
    public const string FileName = "open-wild.json";

    /// <summary>The longest sight or scent a temperament may have, in metres: a wild Pokémon nearer the player than this is all that is asked whether it senses them.</summary>
    public const double Farthest = 30;

    private static readonly Lazy<OpenWildKinds> game = new(() => From(GameDataFiles.Load<OpenWildFile>(FileName, strict: true), FileName));

    /// <summary>The game's own, from the data folder.</summary>
    public static OpenWildKinds Game => game.Value;

    private readonly Dictionary<OpenTemperament, OpenWildKind> kinds;
    private readonly Dictionary<string, OpenTemperament> species;
    private readonly Dictionary<PokemonType, OpenTemperament> types;
    private readonly OpenTemperament fallback;

    private OpenWildKinds(Dictionary<OpenTemperament, OpenWildKind> kinds, Dictionary<string, OpenTemperament> species,
        Dictionary<PokemonType, OpenTemperament> types, OpenTemperament fallback)
    {
        this.kinds = kinds;
        this.species = species;
        this.types = types;
        this.fallback = fallback;
    }

    /// <summary>
    /// The kinds a file gives, held to what JSON can't say (a row for each temperament, no other name, sight and scent
    /// within <see cref="Farthest"/>, an angle within a turn); a finding names the file and the path and stops the read.
    /// Species and types are taken as written: the content check names one the data hasn't.
    /// </summary>
    public static OpenWildKinds From(OpenWildFile file, string name)
    {
        OpenTemperament Temperament(string text, string path) =>
            Enum.TryParse<OpenTemperament>(text, ignoreCase: true, out var t) && Enum.IsDefined(t) && !int.TryParse(text, out _)
                ? t : throw new System.IO.InvalidDataException($"{name}: {path}: '{text}' is no temperament (Calm, Shy, Curious or Aggressive)");
        var kinds = new Dictionary<OpenTemperament, OpenWildKind>();
        foreach (var (key, row) in file.Kinds)
        {
            var t = Temperament(key, $"$.kinds['{key}']");
            if (row.Sight < 0 || row.Sight > Farthest || row.Scent < 0 || row.Scent > Farthest)
                throw new System.IO.InvalidDataException($"{name}: $.kinds['{key}']: sight and scent are from nought to {Farthest} m");
            if (row.Angle < 0 || row.Angle > 360) throw new System.IO.InvalidDataException($"{name}: $.kinds['{key}'].angle: from nought to 360 degrees");
            if (row.Keep < 0 || row.Keep > Farthest) throw new System.IO.InvalidDataException($"{name}: $.kinds['{key}'].keep: from nought to {Farthest} m");
            kinds[t] = new OpenWildKind(t, OpenPos.Millimetres(row.Sight),
                (int)Math.Round(row.Angle / 2 * OpenYaw.Turn / 360.0, MidpointRounding.AwayFromZero), OpenPos.Millimetres(row.Scent), OpenPos.Millimetres(row.Keep));
        }
        foreach (var t in Enum.GetValues<OpenTemperament>())
            if (!kinds.ContainsKey(t)) throw new System.IO.InvalidDataException($"{name}: $.kinds: has no row for {t}");
        var species = file.Species.ToDictionary(p => p.Key, p => Temperament(p.Value, $"$.species['{p.Key}']"), StringComparer.OrdinalIgnoreCase);
        var types = new Dictionary<PokemonType, OpenTemperament>();
        foreach (var (key, value) in file.Types)
        {
            if (!Enum.TryParse<PokemonType>(key, ignoreCase: true, out var type) || int.TryParse(key, out _))
                throw new System.IO.InvalidDataException($"{name}: $.types['{key}']: no such type");
            types[type] = Temperament(value, $"$.types['{key}']");
        }
        return new OpenWildKinds(kinds, species, types, Temperament(file.Default, "$.default"));
    }

    /// <summary>A temperament's senses.</summary>
    public OpenWildKind Of(OpenTemperament temperament) => kinds[temperament];

    /// <summary>
    /// A species' (or a form's) temperament: its own where named, its form's species' where that is named, else the
    /// first of its types with a row, else the default.
    /// </summary>
    public OpenWildKind For(string name)
    {
        if (species.TryGetValue(name, out var own)) return kinds[own];
        var data = PokemonDatabase.Get(name) ?? PokemonDatabase.SpeciesOfForm(name);
        if (data != null)
        {
            if (species.TryGetValue(data.Name, out var ofSpecies)) return kinds[ofSpecies];
            if (types.TryGetValue(data.PrimaryType, out var first)) return kinds[first];
            if (data.SecondaryType is { } second && types.TryGetValue(second, out var other)) return kinds[other];
        }
        return kinds[fallback];
    }

    /// <summary>The species the file names (for the content check).</summary>
    public IEnumerable<string> NamedSpecies => species.Keys;
}
