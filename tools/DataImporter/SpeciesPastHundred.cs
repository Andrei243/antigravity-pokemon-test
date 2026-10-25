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

    /// <summary>
    /// Whether it is legendary, where the row says so over its forebear's (plan 03 · D14, Alola's supplement: the Ultra
    /// Beasts, which the data marks as neither, made a region's legendary set); null takes the forebear's.
    /// </summary>
    public bool? Legendary { get; set; }

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
/// <item>From its forebear: legendary (unless the row says, <see cref="SpeciesDesign.Legendary"/>) or mythical, whether its females look different, growth rate, gender share, egg
/// groups, hatch cycles, friendship, colour and shape, what an Egg of it is (the forebear's own Egg), its moves by
/// level to 100 (its newest game's list too, without the moves its forebear learned on evolving), its machines, tutor
/// moves and Egg moves.</item>
/// <item>Effort: the forebear's, and one more point in the stat that rose most. Catch rate 45 (3 for a legendary's or a
/// mythical's). Base experience: the forebear's and a fifth.</item>
/// <item>Its moves past 100 are <see cref="MovesPastHundred"/>'s rule, by its own types and stats, as any species'.</item>
/// <item>A design whose evolution names a <c>fromForm</c> (the Galar supplement) grows from that regional form: its
/// number is the form's region's block + the species' (<see cref="NumberOf"/>), and its effort, base experience,
/// colour and moves by level are the form's where the form has its own.</item>
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
            var form = FormOf(design, from);
            if (design.Evolution.FromForm != null && form?.Kind != FormKind.Regional)
                throw new InvalidDataException($"{design.Name} evolves from {design.Evolution.FromForm}, which is no regional form of {design.From}");

            var s = Make(design, from, form);
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

    /// <summary>
    /// The regional form a design grows from (the Galar supplement's technical plan: its evolution's <c>fromForm</c>),
    /// or null for one that grows from the species itself.
    /// </summary>
    public static PokemonForm? FormOf(SpeciesDesign d, PokemonSpecies from) =>
        d.Evolution.FromForm is { } name ? from.Form(name) : null;

    /// <summary>The stats a form has in the game as it is played: its own where it has them, else its species'.</summary>
    public static StatSpread StatsOf(PokemonSpecies s, PokemonForm? form) =>
        form?.Modern?.BaseStats ?? form?.BaseStats ?? StatsOf(s);

    /// <summary>
    /// The block of a thousand each region's forms are numbered in (the Galar supplement's question 1): a design grown
    /// from a regional form is the block + the species' National number (the Galarian Rapidash's 5078).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> FormBlocks = new Dictionary<string, int>
    {
        ["Alola"] = 4000, ["Galar"] = 5000, ["Hisui"] = 6000, ["Paldea"] = 7000
    };

    /// <summary>A design's number: <see cref="PokemonDatabase.OursFrom"/> + its forebear's, or its form's region's block + it.</summary>
    public static int NumberOf(PokemonSpecies from, string? form)
    {
        if (form == null) return PokemonDatabase.OursFrom + from.DexNumber;
        var parts = form.Split('-');
        var region = parts.FirstOrDefault(FormBlocks.ContainsKey)
            ?? throw new InvalidDataException($"{form} is a form of no region with a block of numbers");
        return FormBlocks[region] + from.DexNumber;
    }

    private static readonly StatType[] Order = { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };

    private static PokemonSpecies Make(SpeciesDesign d, PokemonSpecies from, PokemonForm? form)
    {
        var was = StatsOf(from, form);
        var rose = Order.OrderByDescending(stat => d.Stats.Of(stat) - was.Of(stat)).First();
        var effort = Copy(form?.EvYield ?? from.EvYield ?? new StatSpread());
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

        bool legendary = d.Legendary ?? from.Legendary;
        return new PokemonSpecies
        {
            DexNumber = NumberOf(from, form?.Name),
            Name = d.Name,
            Category = d.Category,
            Generation = 0,
            Legendary = legendary,
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
            CatchRate = legendary || from.Mythical ? 3 : 45,
            BaseExpYield = (int)Math.Round((form?.BaseExpYield ?? from.BaseExpYield) * 1.2, MidpointRounding.AwayFromZero),
            GrowthRate = from.GrowthRate,
            GenderRatio = from.GenderRatio,
            EggGroups = from.EggGroups?.ToList(),
            HatchCycles = from.HatchCycles,
            BaseFriendship = from.BaseFriendship,
            Height = d.Height,
            Weight = d.Weight,
            Color = form?.Color ?? from.Color,
            Shape = from.Shape,
            Learnset = ByLevel(form?.Learnset ?? from.Learnset),
            TmMoves = from.TmMoves?.ToList(),
            TutorMoves = from.TutorMoves?.ToList(),
            EggMoves = from.EggMoves?.ToList(),
            Offspring = from.Offspring ?? from.Name,
            Abilities = d.Abilities.ToList(),
            HiddenAbility = d.HiddenAbility,
            Modern = (form?.Learnset != null ? form.Modern?.Learnset : from.Modern?.Learnset) is { } modern ? new SpeciesValues { Learnset = ByLevel(modern) } : null
        };
    }

    private static StatSpread Copy(StatSpread s) => new()
    {
        HP = s.HP, Attack = s.Attack, Defense = s.Defense, SpAttack = s.SpAttack, SpDefense = s.SpDefense, Speed = s.Speed
    };
}
