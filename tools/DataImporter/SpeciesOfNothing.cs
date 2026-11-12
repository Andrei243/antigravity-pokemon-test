using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// One species made from nothing as the user approved it (plan 46 · CP1): a row of
/// <c>Overrides/species-carpathia.json</c>, keyed by its name. Every value a species has is written here, since there is
/// no forebear to take one from; a value left out is a mistake the importer names (<see cref="SpeciesOfNothing.Missing"/>).
/// The lists that may be empty (<see cref="TutorMoves"/>, <see cref="EggMoves"/>, <see cref="Evolutions"/>) are still written, as <c>[]</c>.
/// </summary>
public sealed class NothingDesign
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Its number in the Carpathian Pokédex, from 1: its National number is <see cref="PokemonDatabase.CarpathiaFrom"/> − 1 + this.</summary>
    public int? Number { get; set; }

    /// <summary>The category word, without "Pokémon" (<c>Lantern</c>).</summary>
    public string? Category { get; set; }

    public List<PokemonType>? Types { get; set; }
    public StatSpread? Stats { get; set; }

    /// <summary>One or two, in the order the game picks from.</summary>
    public List<string>? Abilities { get; set; }

    /// <summary>Its hidden ability; written as null for none.</summary>
    public string? HiddenAbility { get; set; }

    public int? CatchRate { get; set; }
    public GrowthRate? GrowthRate { get; set; }
    public List<string>? EggGroups { get; set; }

    /// <summary>The effort a foe of it gives.</summary>
    public StatSpread? Effort { get; set; }

    public int? BaseExperience { get; set; }

    /// <summary>Metres and kilograms.</summary>
    public float? Height { get; set; }
    public float? Weight { get; set; }

    /// <summary>The Pokédex's colour and shape words (<c>red</c>, <c>quadruped</c>), as the games' species have them.</summary>
    public string? Color { get; set; }
    public string? Shape { get; set; }

    /// <summary>Eighths that are female: 0 always male, 8 always female, −1 genderless.</summary>
    public int? GenderRatio { get; set; }

    public int? HatchCycles { get; set; }
    public int? Friendship { get; set; }

    /// <summary>Its moves by level, a level 0 meaning as it evolves into this species.</summary>
    public List<LearnableMove>? Learnset { get; set; }

    /// <summary>The machines it learns from, by the machine's name (<c>TM06</c>).</summary>
    public List<string>? TmMoves { get; set; }

    public List<string>? TutorMoves { get; set; }
    public List<string>? EggMoves { get; set; }

    /// <summary>What it evolves into, each a species of the games or of this table.</summary>
    public List<EvolutionData>? Evolutions { get; set; }

    /// <summary>The species an Egg it lays is; null for itself.</summary>
    public string? Offspring { get; set; }

    public bool Legendary { get; set; }
    public bool Mythical { get; set; }
    public bool GenderDifferences { get; set; }
}

/// <summary>
/// Plan 46 · CP1: species made from nothing, Carpathia's, with no forebear in any game. Each is a row of the
/// hand-written table <c>Overrides/species-carpathia.json</c> (<see cref="NothingDesign"/>), which this makes a whole
/// species of <c>species.json</c>, after the games' and D14's (<see cref="SpeciesPastHundred"/>), in the Carpathian
/// Pokédex's order.
/// <list type="bullet">
/// <item>Its number is <see cref="PokemonDatabase.CarpathiaFrom"/> − 1 + its number in the Carpathian Pokédex
/// (<see cref="Pokedex.Carpathia"/>, which it carries in <c>dexNumbers</c>); its generation 0.</item>
/// <item>Every value is the row's; nothing is taken from another species, and a row that leaves one out stops the
/// import naming it.</item>
/// <item>Its moves, abilities and evolutions' targets must be the data's (the games' or the table's own).</item>
/// <item>Its moves past 100 are <see cref="MovesPastHundred"/>'s rule, by its own types and stats, as any species'.</item>
/// </list>
/// The table is empty until the user approves a batch (plan 46 · CP3); the tests hold a placeholder of their own to it.
/// </summary>
public static class SpeciesOfNothing
{
    public const string FileName = "species-carpathia.json";

    /// <summary>The table in the repository.</summary>
    public static string PathIn(string repo) => Path.Combine(repo, "tools", "DataImporter", "Overrides", FileName);

    /// <summary>The designs of a table file, in its order.</summary>
    public static List<NothingDesign> Load(string path) => Parse(File.ReadAllText(path));

    /// <summary>The designs of a table's text, in its order.</summary>
    public static List<NothingDesign> Parse(string json)
    {
        var table = JsonNode.Parse(json, documentOptions: new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!.AsObject();
        var designs = new List<NothingDesign>();
        foreach (var (name, row) in table)
        {
            var design = row!.Deserialize<NothingDesign>(GameDataFiles.Json)!;
            design.Name = name;
            designs.Add(design);
        }
        return designs;
    }

    /// <summary>The values a row leaves out, by their names in the table; empty for a whole row.</summary>
    public static List<string> Missing(NothingDesign d)
    {
        var missing = new List<string>();
        void Need(object? value, string field) { if (value == null) missing.Add(field); }
        Need(d.Number, "number");
        Need(d.Category, "category");
        Need(d.Types, "types");
        Need(d.Stats, "stats");
        Need(d.Abilities, "abilities");
        Need(d.CatchRate, "catchRate");
        Need(d.GrowthRate, "growthRate");
        Need(d.EggGroups, "eggGroups");
        Need(d.Effort, "effort");
        Need(d.BaseExperience, "baseExperience");
        Need(d.Height, "height");
        Need(d.Weight, "weight");
        Need(d.Color, "color");
        Need(d.Shape, "shape");
        Need(d.GenderRatio, "genderRatio");
        Need(d.HatchCycles, "hatchCycles");
        Need(d.Friendship, "friendship");
        Need(d.Learnset, "learnset");
        Need(d.TmMoves, "tmMoves");
        Need(d.TutorMoves, "tutorMoves");
        Need(d.EggMoves, "eggMoves");
        Need(d.Evolutions, "evolutions");
        return missing;
    }

    /// <summary>A design's National number: <see cref="PokemonDatabase.CarpathiaFrom"/> − 1 + its Carpathian number.</summary>
    public static int NumberOf(int carpathianNumber) => PokemonDatabase.CarpathiaFrom - 1 + carpathianNumber;

    /// <summary>
    /// Adds a species for every design, after every species already in the list (in their numbers' order), and composes
    /// their Pokédex sentences. The moves and abilities the designs name are checked against the names given. Returns the
    /// species made.
    /// </summary>
    public static List<PokemonSpecies> Add(List<PokemonSpecies> species, IReadOnlyList<NothingDesign> designs,
        IEnumerable<string> moves, IEnumerable<string> abilities)
    {
        var moveNames = moves.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var abilityNames = abilities.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var names = species.SelectMany(s => new[] { s.Name }.Concat((s.Forms ?? new()).Select(f => f.Name))).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ours = designs.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var made = new List<PokemonSpecies>();
        foreach (var d in designs)
        {
            if (Missing(d) is { Count: > 0 } missing) throw new InvalidDataException($"{d.Name} leaves out {string.Join(", ", missing)}");
            if (names.Contains(d.Name)) throw new InvalidDataException($"{d.Name} is a species or form already");
            if (d.Number < 1) throw new InvalidDataException($"{d.Name} is number {d.Number} of the Carpathian Pokédex, which counts from 1");
            if (d.Types!.Count is 0 or > 2 || d.Types.Distinct().Count() != d.Types.Count) throw new InvalidDataException($"{d.Name} has types {string.Join("/", d.Types)}");
            if (d.Abilities!.Count is 0 or > 2) throw new InvalidDataException($"{d.Name} has {d.Abilities.Count} abilities");
            foreach (var ability in d.Abilities.Append(d.HiddenAbility).OfType<string>())
                if (!abilityNames.Contains(ability)) throw new InvalidDataException($"{d.Name} has {ability}, which is no ability");
            foreach (var move in d.Learnset!.Select(m => m.MoveName).Concat(d.TutorMoves!).Concat(d.EggMoves!))
                if (!moveNames.Contains(move)) throw new InvalidDataException($"{d.Name} learns {move}, which is no move");
            foreach (var e in d.Evolutions!)
                if (!names.Contains(e.TargetSpecies) && !ours.Contains(e.TargetSpecies))
                    throw new InvalidDataException($"{d.Name} evolves into {e.TargetSpecies}, which is no species");
            if (d.Offspring is { } egg && !names.Contains(egg) && !ours.Contains(egg)) throw new InvalidDataException($"{d.Name} lays Eggs of {egg}, which is no species");
            if (d.CatchRate is < 1 or > 255) throw new InvalidDataException($"{d.Name} has a catch rate of {d.CatchRate}");
            if (d.GenderRatio is < -1 or > 8) throw new InvalidDataException($"{d.Name} has a gender share of {d.GenderRatio}");
            made.Add(Make(d));
        }
        species.AddRange(made.OrderBy(s => s.DexNumber));
        if (species.GroupBy(s => s.DexNumber).FirstOrDefault(g => g.Count() > 1) is { } twice)
            throw new InvalidDataException($"Two species are number {twice.Key}: {string.Join(", ", twice.Select(s => s.Name))}");
        foreach (var s in made) s.DexEntry = DexText.Compose(s, species);
        return made;
    }

    private static PokemonSpecies Make(NothingDesign d) => new()
    {
        DexNumber = NumberOf(d.Number!.Value),
        DexNumbers = new Dictionary<string, int> { [Pokedex.Carpathia] = d.Number.Value },
        Name = d.Name,
        Category = d.Category!,
        Generation = 0,
        Legendary = d.Legendary,
        Mythical = d.Mythical,
        GenderDifferences = d.GenderDifferences,
        PrimaryType = d.Types![0],
        SecondaryType = d.Types.Count > 1 ? d.Types[1] : null,
        BaseHP = d.Stats!.HP,
        BaseAttack = d.Stats.Attack,
        BaseDefense = d.Stats.Defense,
        BaseSpAttack = d.Stats.SpAttack,
        BaseSpDefense = d.Stats.SpDefense,
        BaseSpeed = d.Stats.Speed,
        EvYield = d.Effort,
        CatchRate = d.CatchRate!.Value,
        BaseExpYield = d.BaseExperience!.Value,
        GrowthRate = d.GrowthRate!.Value,
        GenderRatio = d.GenderRatio!.Value,
        EggGroups = d.EggGroups!.ToList(),
        HatchCycles = d.HatchCycles!.Value,
        BaseFriendship = d.Friendship!.Value,
        Height = d.Height!.Value,
        Weight = d.Weight!.Value,
        Color = d.Color,
        Shape = d.Shape,
        Learnset = d.Learnset!.Select(m => new LearnableMove { Level = m.Level, MoveName = m.MoveName }).ToList(),
        TmMoves = d.TmMoves!.Count > 0 ? d.TmMoves.ToList() : null,
        TutorMoves = d.TutorMoves!.Count > 0 ? d.TutorMoves.ToList() : null,
        EggMoves = d.EggMoves!.Count > 0 ? d.EggMoves.ToList() : null,
        Evolutions = d.Evolutions!.Count > 0 ? d.Evolutions.ToList() : null,
        Offspring = d.Offspring,
        Abilities = d.Abilities!.ToList(),
        HiddenAbility = d.HiddenAbility
    };
}
