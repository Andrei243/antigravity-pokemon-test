using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>One species of our own as the user approved it (plan 03 · D14): a row of <c>Overrides/species-past-hundred.json</c>.</summary>
public sealed class SpeciesDesign
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The species it evolves from: the last of its line, or alone.</summary>
    public string From { get; set; } = string.Empty;

    public List<PokemonType> Types { get; set; } = new();

    /// <summary>The category word, without "Pokémon" (<c>Gnawer</c>).</summary>
    public string Category { get; set; } = string.Empty;

    public float Height { get; set; }
    public float Weight { get; set; }
    public StatSpread Stats { get; set; } = new();

    /// <summary>As many as the species it comes from has, in their slots' order.</summary>
    public List<string> Abilities { get; set; } = new();

    public string? HiddenAbility { get; set; }

    /// <summary>The row put on <see cref="From"/>'s evolutions, its target this species.</summary>
    public EvolutionData Evolution { get; set; } = new();

    public int Total => Stats.HP + Stats.Attack + Stats.Defense + Stats.SpAttack + Stats.SpDefense + Stats.Speed;
}

/// <summary>
/// Plan 03 · D14: the species of our own that evolve past level 100, batch by batch as the user approves them
/// (<c>docs/plans/D14-proposals-kanto.md</c>, <c>D14-proposals-johto.md</c>, <c>D14-proposals-hoenn.md</c>, <c>D14-proposals-sinnoh.md</c>). Each design is a row of the hand-written
/// table <c>Overrides/species-past-hundred.json</c>; this makes it a whole species of <c>species.json</c> by the
/// proposals' rules and puts its evolution on the species it comes from. No species is code of its own: a later batch
/// is rows of the table.
/// <list type="bullet">
/// <item>Its number is <see cref="PokemonDatabase.OursFrom"/> + its forebear's, its generation 0, and it is in no
/// regional Pokédex (<c>dexNumbers</c> empty).</item>
/// <item>Its types, stats, abilities, category, height and weight are the design's.</item>
/// <item>From its forebear: legendary or mythical, whether its females look different, growth rate, gender share, egg
/// groups, hatch cycles, friendship, colour and shape, what an Egg of it is (the forebear's own Egg), its moves by
/// level to 100 (its newest game's list too, without the moves its forebear learned on evolving), its machines, tutor
/// moves and Egg moves.</item>
/// <item>Effort: the forebear's, and one more point in the stat that rose most. Catch rate 45 (3 for a legendary's or a
/// mythical's). Base experience: the forebear's and a fifth.</item>
/// <item>Its moves past 100 are <see cref="MovesPastHundred"/>'s rule, by its own types and stats, as any species'.</item>
/// </list>
/// </summary>
public static class SpeciesPastHundred
{
    public const string FileName = "species-past-hundred.json";

    /// <summary>The table in the repository.</summary>
    public static string PathIn(string repo) => Path.Combine(repo, "tools", "DataImporter", "Overrides", FileName);

    /// <summary>The designs, in the table's order.</summary>
    public static List<SpeciesDesign> Load(string path)
    {
        var table = JsonNode.Parse(File.ReadAllText(path), documentOptions: new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!.AsObject();
        var designs = new List<SpeciesDesign>();
        foreach (var (name, row) in table)
        {
            var design = row!.Deserialize<SpeciesDesign>(GameDataFiles.Json)!;
            design.Name = name;
            design.Evolution.TargetSpecies = name;
            designs.Add(design);
        }
        return designs;
    }

    /// <summary>
    /// Adds a species for every design, after the games' (in their numbers' order), and its evolution at the end of its
    /// forebear's; composes both one's Pokédex sentences again. Returns the species made.
    /// </summary>
    public static List<PokemonSpecies> Add(List<PokemonSpecies> species, IReadOnlyList<SpeciesDesign> designs)
    {
        var byName = species.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        var forms = species.SelectMany(s => s.Forms ?? new()).Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var made = new List<PokemonSpecies>();
        foreach (var design in designs)
        {
            if (!byName.TryGetValue(design.From, out var from)) throw new InvalidDataException($"{design.Name} evolves from {design.From}, which is no species");
            if (from.IsOurs) throw new InvalidDataException($"{design.Name} evolves from {design.From}, a species of our own");
            if (byName.ContainsKey(design.Name) || forms.Contains(design.Name)) throw new InvalidDataException($"{design.Name} is a species or form already");
            if (design.Types.Count is 0 or > 2) throw new InvalidDataException($"{design.Name} has {design.Types.Count} types");

            var s = Make(design, from);
            made.Add(s);
            byName[s.Name] = s;
            (from.Evolutions ??= new()).Add(design.Evolution);
        }
        species.AddRange(made.OrderBy(s => s.DexNumber));
        if (species.GroupBy(s => s.DexNumber).FirstOrDefault(g => g.Count() > 1) is { } twice)
            throw new InvalidDataException($"Two species are number {twice.Key}: {string.Join(", ", twice.Select(s => s.Name))}");

        // The Pokédex's sentence tells what a species evolves into and from: the forebears' and ours again
        foreach (var s in made)
        {
            s.DexEntry = DexText.Compose(s, species);
            var from = byName[designs.First(d => d.Name == s.Name).From];
            from.DexEntry = DexText.Compose(from, species);
        }
        return made;
    }

    /// <summary>The machines, tutor moves and Egg moves of each species of ours, its forebear's as the import left them (after the later games' machines).</summary>
    public static void CopyMachines(List<PokemonSpecies> species)
    {
        foreach (var s in species.Where(s => s.IsOurs))
        {
            var from = ForebearOf(s, species);
            s.TmMoves = from.TmMoves?.ToList();
            s.TutorMoves = from.TutorMoves?.ToList();
            s.EggMoves = from.EggMoves?.ToList();
        }
    }

    /// <summary>The species one of ours evolves from.</summary>
    public static PokemonSpecies ForebearOf(PokemonSpecies ours, IEnumerable<PokemonSpecies> species) =>
        species.First(o => o.Evolutions?.Any(e => e.TargetSpecies == ours.Name) == true);

    /// <summary>The stats the forebear has in the game as it is played (the newest games' where they differ).</summary>
    public static StatSpread StatsOf(PokemonSpecies s) => s.Modern?.BaseStats ?? new StatSpread
    {
        HP = s.BaseHP, Attack = s.BaseAttack, Defense = s.BaseDefense, SpAttack = s.BaseSpAttack, SpDefense = s.BaseSpDefense, Speed = s.BaseSpeed
    };

    private static readonly StatType[] Order = { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };

    private static PokemonSpecies Make(SpeciesDesign d, PokemonSpecies from)
    {
        var was = StatsOf(from);
        var rose = Order.OrderByDescending(stat => d.Stats.Of(stat) - was.Of(stat)).First();
        var effort = Copy(from.EvYield ?? new StatSpread());
        switch (rose)
        {
            case StatType.HP: effort.HP++; break;
            case StatType.Attack: effort.Attack++; break;
            case StatType.Defense: effort.Defense++; break;
            case StatType.SpAttack: effort.SpAttack++; break;
            case StatType.SpDefense: effort.SpDefense++; break;
            default: effort.Speed++; break;
        }

        static List<LearnableMove> ByLevel(IEnumerable<LearnableMove> moves) =>
            moves.Where(m => m.Level is >= 1 and <= 100).Select(m => new LearnableMove { Level = m.Level, MoveName = m.MoveName }).ToList();

        return new PokemonSpecies
        {
            DexNumber = PokemonDatabase.OursFrom + from.DexNumber,
            Name = d.Name,
            Category = d.Category,
            Generation = 0,
            Legendary = from.Legendary,
            Mythical = from.Mythical,
            GenderDifferences = from.GenderDifferences,
            PrimaryType = d.Types[0],
            SecondaryType = d.Types.Count > 1 ? d.Types[1] : null,
            BaseHP = d.Stats.HP,
            BaseAttack = d.Stats.Attack,
            BaseDefense = d.Stats.Defense,
            BaseSpAttack = d.Stats.SpAttack,
            BaseSpDefense = d.Stats.SpDefense,
            BaseSpeed = d.Stats.Speed,
            EvYield = effort,
            CatchRate = from.Legendary || from.Mythical ? 3 : 45,
            BaseExpYield = (int)Math.Round(from.BaseExpYield * 1.2, MidpointRounding.AwayFromZero),
            GrowthRate = from.GrowthRate,
            GenderRatio = from.GenderRatio,
            EggGroups = from.EggGroups?.ToList(),
            HatchCycles = from.HatchCycles,
            BaseFriendship = from.BaseFriendship,
            Height = d.Height,
            Weight = d.Weight,
            Color = from.Color,
            Shape = from.Shape,
            Learnset = ByLevel(from.Learnset),
            TmMoves = from.TmMoves?.ToList(),
            TutorMoves = from.TutorMoves?.ToList(),
            EggMoves = from.EggMoves?.ToList(),
            Offspring = from.Offspring ?? from.Name,
            Abilities = d.Abilities.ToList(),
            HiddenAbility = d.HiddenAbility,
            Modern = from.Modern?.Learnset is { } modern ? new SpeciesValues { Learnset = ByLevel(modern) } : null
        };
    }

    private static StatSpread Copy(StatSpread s) => new()
    {
        HP = s.HP, Attack = s.Attack, Defense = s.Defense, SpAttack = s.SpAttack, SpDefense = s.SpDefense, Speed = s.Speed
    };
}
