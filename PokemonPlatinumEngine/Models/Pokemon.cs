using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

public class LearnableMove
{
    /// <summary>The level it is learned at; 0 means when the Pokémon evolves into this species (Generation 7 on).</summary>
    public int Level { get; set; }
    public string MoveName { get; set; } = string.Empty;
}

/// <summary>One way a species evolves. Only the fields its <see cref="Method"/> uses are set.</summary>
public class EvolutionData
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public EvolutionMethod Method { get; set; } = EvolutionMethod.Level;
    public string TargetSpecies { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Level { get; set; }
    /// <summary>The item used or held.</summary>
    public string? Item { get; set; }
    /// <summary>The move it must know.</summary>
    public string? Move { get; set; }
    /// <summary>The move type it must know, or the type a party member must have.</summary>
    public PokemonType? Type { get; set; }
    /// <summary>The species that must be in the party, or traded for.</summary>
    public string? Species { get; set; }
    /// <summary>The place a map must have for it to level up there (<c>Magnetic Field</c>, <c>Moss Rock</c>, <c>Ice Rock</c>, <c>Stone Arch</c>).</summary>
    public string? Location { get; set; }
    /// <summary>Beauty or affection needed, or how many: steps, uses of a move, foes knocked out, items in the bag.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Value { get; set; }
    /// <summary>High friendship is needed on top of the method's own condition (Eevee into Sylveon).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool NeedsFriendship { get; set; }
    /// <summary>Plain words for <see cref="EvolutionMethod.Other"/> and anything else the fields can't hold.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// The form it must be in (Galarian Meowth into Perrserker, Alolan Vulpix into Alolan Ninetales); null for the
    /// species' own form. A Pokémon in a form that has evolutions of its own evolves only by those.
    /// </summary>
    public string? FromForm { get; set; }

    /// <summary>The form it becomes (Burmy's sandy cloak makes a sandy Wormadam); null for the species' own.</summary>
    public string? TargetForm { get; set; }

    /// <summary>The region it has to evolve in, for regional forms that are born that way (Pikachu into Alolan Raichu in Alola).</summary>
    public string? Region { get; set; }
}

/// <summary>One number for each stat: the points a species gives when it is defeated, or a form's base stats.</summary>
public class StatSpread
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int HP { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Attack { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Defense { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int SpAttack { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int SpDefense { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Speed { get; set; }

    /// <summary>The number of one stat (nought for accuracy and evasion, which a spread doesn't hold).</summary>
    public int Of(StatType stat) => stat switch
    {
        StatType.HP => HP,
        StatType.Attack => Attack,
        StatType.Defense => Defense,
        StatType.SpAttack => SpAttack,
        StatType.SpDefense => SpDefense,
        StatType.Speed => Speed,
        _ => 0
    };

    /// <summary>The six numbers in the spread's own order, as a battle's log is written out (HP/Attack/Defense/Sp. Atk/Sp. Def/Speed).</summary>
    public override string ToString() => $"{HP}/{Attack}/{Defense}/{SpAttack}/{SpDefense}/{Speed}";
}

/// <summary>
/// The values that differ between the rules a game is played by (plan 06 · R1): types and base stats, and since
/// plan 06 · R19 the moves learned by level (the newest game's list, where Platinum's is another).
/// </summary>
public class SpeciesValues
{
    /// <summary>One type or two.</summary>
    public List<PokemonType>? Types { get; set; }
    public StatSpread? BaseStats { get; set; }
    public List<LearnableMove>? Learnset { get; set; }
}

/// <summary>
/// One of a species' forms (plan 03 · D11): what kind it is and whatever it doesn't share with its species. A null
/// value is the species' own.
/// </summary>
public class PokemonForm
{
    /// <summary>The species' name and the form's, as Pokémon Showdown spells them: <c>Rotom-Heat</c>, <c>Charizard-Mega-X</c>, <c>Meowth-Galar</c>.</summary>
    public string Name { get; set; } = string.Empty;
    public FormKind Kind { get; set; }

    /// <summary>The form's name as the chosen language shows it (plan 17 · N3), given its species' name.</summary>
    public string DisplayName(string speciesName) => LocalNames.Form(Name, speciesName);

    /// <summary>One type or two.</summary>
    public List<PokemonType>? Types { get; set; }
    public StatSpread? BaseStats { get; set; }
    public List<string>? Abilities { get; set; }
    public string? HiddenAbility { get; set; }
    public float? Height { get; set; }
    public float? Weight { get; set; }
    public int? BaseExpYield { get; set; }
    public StatSpread? EvYield { get; set; }
    public int? CatchRate { get; set; }
    public List<LearnableMove>? Learnset { get; set; }

    /// <summary>Its Pokédex colour where it isn't its species' (Mega Charizard X is black, Alolan Vulpix white): what a generated model is painted.</summary>
    public string? Color { get; set; }

    /// <summary>
    /// The newest games' types and base stats where they differ from Platinum's (Rotom's appliances took a second
    /// type in Generation 5). <see cref="Ruleset.Use"/> swaps them in, as it does a species' own.
    /// </summary>
    public SpeciesValues? Modern { get; set; }

    // Platinum's values, kept from the first time the rules replaced them
    private SpeciesValues? platinum;

    /// <summary>The form as these rules have it, leaving this one as it is (for tests and tools).</summary>
    public PokemonForm Under(Ruleset rules)
    {
        if (Modern == null) return this;
        var copy = (PokemonForm)MemberwiseClone();
        copy.platinum = platinum ?? new SpeciesValues { Types = Types, BaseStats = BaseStats, Learnset = Learnset };
        copy.Take(rules.ModernSpeciesValues ? Modern : null);
        return copy;
    }

    internal void UseRules(Ruleset rules)
    {
        if (Modern == null) return;
        platinum ??= new SpeciesValues { Types = Types, BaseStats = BaseStats, Learnset = Learnset };
        Take(rules.ModernSpeciesValues ? Modern : null);
    }

    private void Take(SpeciesValues? values)
    {
        Types = values?.Types ?? platinum!.Types;
        BaseStats = values?.BaseStats ?? platinum!.BaseStats;
        Learnset = values?.Learnset ?? platinum!.Learnset;
    }
}

/// <summary>
/// A species in Pal Park's catching show (the original's <c>pal_park</c> block): the field or the water it hides in
/// (one of four corners each, 0 for none), how often it turns up against the others there, and the points a catch of
/// it brings.
/// </summary>
public class PalParkData
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int LandArea { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int WaterArea { get; set; }
    public int Rarity { get; set; }
    public int CatchingPoints { get; set; }
}

/// <summary>
/// What a wild Pokémon of a species may be holding (plan 06 · R13; the original's <c>held_items</c>): its common
/// item and its rare one, either missing. The same item twice is always held.
/// </summary>
public class WildItems
{
    public string? Common { get; set; }
    public string? Rare { get; set; }
}

public class PokemonSpecies
{
    public int DexNumber { get; set; }

    /// <summary>Its number in Platinum's Sinnoh Pokédex (1–210); null for the species that Pokédex leaves out.</summary>
    public int? SinnohNumber { get; set; }

    /// <summary>
    /// Its number in each main-series Pokédex, by PokeAPI's identifier (<c>national</c>, <c>kanto</c>,
    /// <c>extended-sinnoh</c>, <c>galar</c>…), from <c>pokedexes.csv</c> and <c>pokemon_dex_numbers.csv</c> (plan 18 · W3).
    /// A Pokédex that numbers a species 000 (Unova's Victini) gives it 0. <see cref="SinnohNumber"/> stays Platinum's own.
    /// </summary>
    public Dictionary<string, int> DexNumbers { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Pokémon"; // e.g. "Tiny Leaf Pokémon"

    /// <summary>
    /// Its name as the chosen language shows it (plan 17 · N3). <see cref="Name"/> is the identifier the data, the save,
    /// the scripts and the rules go by and is never drawn.
    /// </summary>
    [JsonIgnore]
    public string DisplayName => LocalNames.Species(Name);

    /// <summary>Its category as the <c>{category}</c> of the language's kind phrase: PokeAPI's whole genus in a language that has one, else the English word.</summary>
    [JsonIgnore]
    public string DisplayCategory => LocalNames.Category(Name, Category);

    /// <summary>The whole phrase the Pokédex and the summary show for its kind ("Seed Pokémon", "Pokémon Graine").</summary>
    [JsonIgnore]
    public string DisplayKind => LocalNames.Kind(Name, Category);
    /// <summary>The generation that introduced the species.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Generation { get; set; }

    /// <summary>
    /// Legendary and mythical species (PokeAPI's flags). Mythical ones were only ever given out at events, so the
    /// National Pokédex's diploma doesn't ask for them.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Legendary { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Mythical { get; set; }

    /// <summary>
    /// Whether its females look different from its males (PokeAPI's flag, plan 09 · L6): a flag only, never a picture.
    /// The female look itself is a sculpt of ours, listed in <see cref="PokemonLook.Females"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool GenderDifferences { get; set; }
    public PokemonType PrimaryType { get; set; }
    public PokemonType? SecondaryType { get; set; }
    public int BaseHP { get; set; }
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int BaseSpAttack { get; set; }
    public int BaseSpDefense { get; set; }
    public int BaseSpeed { get; set; }
    public StatSpread? EvYield { get; set; }
    public int CatchRate { get; set; } = 45; // 3 to 255
    public int BaseExpYield { get; set; } = 64;
    public GrowthRate GrowthRate { get; set; } = GrowthRate.MediumSlow;
    /// <summary>Eighths of the species that are female: 0 is always male, 8 always female, -1 genderless.</summary>
    public int GenderRatio { get; set; } = 4;
    public List<string>? EggGroups { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int HatchCycles { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int BaseFriendship { get; set; }

    /// <summary>How likely it is to run in the Great Marsh, out of 255 (Platinum's species, <c>safari_flee_rate</c>); 0 for the later ones.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int SafariFleeRate { get; set; }

    /// <summary>Where it turns up in Pal Park's catching show and what it is worth there (Platinum's species); null for the later ones.</summary>
    public PalParkData? PalPark { get; set; }

    /// <summary>What it may hold when met in the wild (plan 06 · R13, <see cref="Overworld.WildEncounterRules.HeldItem"/>); null when nothing.</summary>
    public WildItems? WildItems { get; set; }
    public float Height { get; set; } = 0.5f; // meters
    public float Weight { get; set; } = 10.0f; // kg
    /// <summary>Body colour and shape, as the Pokédex sorts them (for the generated models of plan 03 · D5).</summary>
    public string? Color { get; set; }
    public string? Shape { get; set; }
    public string DexEntry { get; set; } = string.Empty;
    public List<LearnableMove> Learnset { get; set; } = new();

    /// <summary>
    /// The TMs and HMs it can learn, by the machine's name (<c>TM06</c>, <c>HM01</c>), in their order (plan 06 · R11):
    /// Platinum's own list for its species, and for a later one each Platinum machine whose move it learns by a
    /// machine in its own games. Null for none.
    /// </summary>
    public List<string>? TmMoves { get; set; }

    /// <summary>The moves Platinum's move tutors can teach it; null for none.</summary>
    public List<string>? TutorMoves { get; set; }

    /// <summary>
    /// The species an Egg it lays is (plan 06 · R15; the original's personal data <c>offspring</c>, which
    /// <c>Pokemon_GetBaseSpeciesFromPersonalData</c> reads): the first of its line, the baby where there is one (Azurill
    /// for Marill). Null for itself.
    /// </summary>
    public string? Offspring { get; set; }

    /// <summary>
    /// The moves an Egg of this species can hatch knowing when its father knows them (plan 06 · R15; the original's
    /// <c>learnset.egg_moves</c>, and for a later species its newest game's). Null for none.
    /// </summary>
    public List<string>? EggMoves { get; set; }

    public List<EvolutionData>? Evolutions { get; set; }

    /// <summary>The abilities this species can have: one or two, in the order the game picks from.</summary>
    public List<string> Abilities { get; set; } = new();

    /// <summary>Its hidden ability from Generation 5 on; Platinum has none, so new Pokémon never get it yet.</summary>
    public string? HiddenAbility { get; set; }

    /// <summary>
    /// Its forms other than its own (plan 03 · D11): its Megas, regional forms, Rotom's appliances, Unown's letters.
    /// Null when it has none.
    /// </summary>
    public List<PokemonForm>? Forms { get; set; }

    /// <summary>
    /// The newest games' types and base stats where they differ from Platinum's (Clefairy has been a Fairy type
    /// since Generation 6, and Pikachu's Defense rose then), and the newest game's moves learned by level (plan 06 ·
    /// R19). Null when nothing differs, and for every species after
    /// Platinum. The values above are the ones in force: <see cref="Ruleset.Use"/> swaps these in for a game played
    /// by the modern rules, as it does the moves'.
    /// </summary>
    public SpeciesValues? Modern { get; set; }

    /// <summary>Its evolution at a plain level with no other condition, if it has one.</summary>
    [JsonIgnore]
    public EvolutionData? LevelEvolution => Evolutions?.FirstOrDefault(e => e.Method == EvolutionMethod.Level && e.Level > 0);

    [JsonIgnore]
    public bool IsGenderless => GenderRatio < 0;

    /// <summary>One of its forms by name, in any case; null for a name it has no form of.</summary>
    public PokemonForm? Form(string? name) =>
        name == null ? null : Forms?.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The species as one of its forms is, under the form's name: what the form has of its own in place of the
    /// species' values (for the model generator, which reads a species).
    /// </summary>
    public PokemonSpecies AsForm(PokemonForm form)
    {
        var copy = (PokemonSpecies)MemberwiseClone();
        copy.Name = form.Name;
        if (form.Types is { Count: > 0 } types)
        {
            copy.PrimaryType = types[0];
            copy.SecondaryType = types.Count > 1 ? types[1] : null;
        }
        if (form.BaseStats is { } b)
        {
            copy.BaseHP = b.HP;
            copy.BaseAttack = b.Attack;
            copy.BaseDefense = b.Defense;
            copy.BaseSpAttack = b.SpAttack;
            copy.BaseSpDefense = b.SpDefense;
            copy.BaseSpeed = b.Speed;
        }
        copy.Abilities = form.Abilities ?? Abilities;
        copy.HiddenAbility = form.HiddenAbility ?? HiddenAbility;
        copy.Height = form.Height ?? Height;
        copy.Weight = form.Weight ?? Weight;
        copy.BaseExpYield = form.BaseExpYield ?? BaseExpYield;
        copy.EvYield = form.EvYield ?? EvYield;
        copy.CatchRate = form.CatchRate ?? CatchRate;
        copy.Learnset = form.Learnset ?? Learnset;
        copy.Color = form.Color ?? Color;
        copy.Forms = null;
        return copy;
    }

    // Platinum's values, kept from the first time the rules replaced them
    private SpeciesValues? platinum;

    /// <summary>The species as these rules have it, leaving this one as it is: for tests and tools, which never change the rules of the game in progress.</summary>
    public PokemonSpecies Under(Ruleset rules)
    {
        var copy = (PokemonSpecies)MemberwiseClone();
        copy.Forms = Forms?.Select(f => f.Under(rules)).ToList();
        if (Modern == null) return copy;
        copy.platinum = platinum ?? Values();
        copy.Take(rules.ModernSpeciesValues ? Modern : null);
        return copy;
    }

    /// <summary>Gives the species and its forms the values these rules say, in place, so every Pokémon of it follows.</summary>
    internal void UseRules(Ruleset rules)
    {
        foreach (var form in Forms ?? new()) form.UseRules(rules);
        if (Modern == null) return;
        platinum ??= Values();
        Take(rules.ModernSpeciesValues ? Modern : null);
    }

    private SpeciesValues Values() => new()
    {
        Types = SecondaryType is { } second ? new() { PrimaryType, second } : new() { PrimaryType },
        BaseStats = new StatSpread { HP = BaseHP, Attack = BaseAttack, Defense = BaseDefense, SpAttack = BaseSpAttack, SpDefense = BaseSpDefense, Speed = BaseSpeed },
        Learnset = Learnset
    };

    private void Take(SpeciesValues? values)
    {
        var types = values?.Types ?? platinum!.Types!;
        PrimaryType = types[0];
        SecondaryType = types.Count > 1 ? types[1] : null;
        var stats = values?.BaseStats ?? platinum!.BaseStats!;
        BaseHP = stats.HP;
        BaseAttack = stats.Attack;
        BaseDefense = stats.Defense;
        BaseSpAttack = stats.SpAttack;
        BaseSpDefense = stats.SpDefense;
        BaseSpeed = stats.Speed;
        Learnset = values?.Learnset ?? platinum!.Learnset!;
    }
}

public class Pokemon
{
    public PokemonSpecies Species { get; internal set; }
    /// <summary>
    /// The name its trainer gave it; empty for none (plan 17 · N3), so that a Pokémon without one is called its species'
    /// name in whatever language is chosen (<see cref="DisplayName"/>). A nickname equal to the species' English name
    /// is no nickname: older saves wrote the species' name in for every Pokémon, and a save read that way gives none.
    /// </summary>
    public string Nickname { get; set; } = string.Empty;
    public int Level { get; private set; }
    public Gender Gender { get; set; }
    public Nature Nature { get; set; }

    /// <summary>
    /// The nature a mint gave its stats (plan 06 · R28b; the later games' stat nature), or null for none: its stats grow
    /// as this nature's would, while its own <see cref="Nature"/> stays what it was for everything else (the tastes of a
    /// Poffin or a berry, Synchronize, breeding).
    /// </summary>
    public Nature? MintNature { get; set; }

    /// <summary>The nature its stats are reckoned by: a mint's, or its own.</summary>
    public Nature StatNature => MintNature ?? Nature;

    /// <summary>
    /// The stats hyper training has raised (plan 06 · R28b; <see cref="HyperTraining"/>): a bit for each of
    /// <see cref="EffortRules.Order"/>'s six. A trained stat is reckoned as if its IV were 31; the IV itself stays as it
    /// was, which is what an Egg inherits and Hidden Power reads.
    /// </summary>
    public int HyperTrained { get; set; }

    /// <summary>Whether hyper training has raised a stat.</summary>
    public bool IsHyperTrained(StatType stat) => (HyperTrained & HyperTraining.Bit(stat)) != 0;

    /// <summary>The IV a stat is reckoned with: 31 once hyper trained, its own otherwise.</summary>
    public int StatIv(StatType stat, int own) => IsHyperTrained(stat) ? HyperTraining.Perfect : own;

    /// <summary>The hidden ability its form (or species) has, or null where it has none.</summary>
    public string? HiddenAbilityName => FormData?.HiddenAbility ?? Species.HiddenAbility;

    public bool IsShiny { get; set; }

    /// <summary>The name of its ability (one of its species' abilities), or null if the species has none listed yet.</summary>
    public string? AbilityName { get; set; }
    public Ability? Ability => AbilityDatabase.Get(AbilityName);

    /// <summary>The item it holds (null = nothing).</summary>
    public ItemData? HeldItem { get; set; }

    /// <summary>How much it likes its trainer, 0 to 255 (see <see cref="FriendshipRules"/>); starts at the species' base value.</summary>
    public int Friendship { get; set; }

    /// <summary>
    /// The contest condition (plan 06 · R14c; the original's <c>MON_DATA_COOL</c> to <c>MON_DATA_SHEEN</c>): five
    /// qualities and the sheen, each 0 to 255, raised by Poffins (<see cref="Poffins.Feed"/>). Feebas evolves on its
    /// beauty; a Pokémon whose sheen is full eats no more.
    /// </summary>
    public int Cool { get; set; }
    public int Beauty { get; set; }
    public int Cute { get; set; }
    public int Smart { get; set; }
    public int Tough { get; set; }
    public int Sheen { get; set; }

    /// <summary>One of the five qualities of its condition, by the original's order.</summary>
    public int ConditionOf(ContestStat stat) => stat switch
    {
        ContestStat.Cool => Cool,
        ContestStat.Beauty => Beauty,
        ContestStat.Cute => Cute,
        ContestStat.Smart => Smart,
        _ => Tough
    };

    /// <summary>A random number fixed for life, as in the games; Wurmple's evolution is read from it.</summary>
    public uint Personality { get; set; }

    /// <summary>
    /// The Pokémon's own id (plan 07 · O2; <see cref="PokemonIds"/>): what tells two Pokémon apart that are alike in
    /// everything else, which a trade's receipt and a kept team go by. Empty for one that is nobody's (a wild one, a
    /// trainer's, a rental, the Battle Frontier's copies); a Pokémon of the player's is given one by the engine as it is
    /// saved, loaded or received (<c>GameEngine.GiveIds</c>), and keeps it for life: through a battle's copies
    /// (<see cref="Clone"/>, <see cref="CopyStateFrom"/>), an evolution, a hatching and a trade. Drawn from nothing:
    /// it is the trainer's number and a count, so it moves no seeded draw.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The region it belongs to (plan 18 · W4, part b): the region it was caught, given, hatched or received by a
    /// trade in, by id (<c>Sinnoh</c>, <c>Kanto</c>). A Pokémon of the player's that has none is given the region the
    /// player stands in by the engine as it is saved, loaded or received and before a crossing
    /// (<c>GameEngine.GiveIds</c>), so it is never stamped with a region it wasn't met in; one traded joins the
    /// receiver's. Null for one that is nobody's, and in an older save, where it reads as the save's region. Under the
    /// user's option B nothing rests in the boxes for it (<see cref="Crossing"/>): it is what the Pokémon came from.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>The ball it was caught in (null for one that wasn't caught). A Luxury Ball makes friendship grow faster.</summary>
    public string? Ball { get; set; }

    /// <summary>
    /// The trainer who first had it (the original's OT: name, ID number and whether a boy or a girl; plan 06 · R10).
    /// Null for the player's own: one the player caught, hatched or was given, and every Pokémon of a save from
    /// before it was kept. Only one that came from someone else carries a mark, and it is what a Pokémon obeys
    /// by (<see cref="Obedience"/>) and gains more EXP for.
    /// </summary>
    public TrainerMark? OriginalTrainer { get; set; }

    /// <summary>
    /// Pokérus as the original keeps it, in one byte (<see cref="PokerusRules"/>): the strain in the high four
    /// bits and the days it has left in the low four. 0 is one that never had it; a strain with no days left is
    /// cured, and still doubles the EVs it gains.
    /// </summary>
    public int Pokerus { get; set; }

    /// <summary>
    /// The six marks the PC's boxes put on a Pokémon to sort it by (plan 06 · R12): a bit each for the circle,
    /// triangle, square, heart, star and diamond (<see cref="Markings"/>).
    /// </summary>
    public int Marks { get; set; }

    /// <summary>
    /// The ribbons it has won (plan 06 · R17a), each by the original's name (<c>RIBBON_COOL</c>, <c>RIBBON_CUTE_GREAT</c>):
    /// any of Platinum's eighty (<see cref="RibbonRules"/>, plan 06 · R17b), which its summary shows on a page of
    /// their own.
    /// </summary>
    public SortedSet<string> Ribbons { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The Ball Capsule on its ball (plan 06 · R17b; the original's <c>MON_DATA_BALL_CAPSULE_ID</c> and
    /// <c>MON_DATA_BALL_CAPSULE</c>): the capsule's number among the Seal Case's twelve, from 1, or 0 for none, and a
    /// copy of its Seals, which burst as the Pokémon is sent out in battle (<see cref="CapsuleRules"/>). A trainer's
    /// Pokémon carries the number 1 and one of the trainers' own capsules. A Pokémon in a box has none.
    /// </summary>
    public int CapsuleId { get; set; }
    public BallCapsule? Capsule { get; set; }

    /// <summary>
    /// Its Dynamax Level (plan 06 · R22; Showdown's <c>dynamaxLevel</c>), 0 to 10: a Dynamax raises its HP by half and
    /// a twentieth more for each level, so twice at 10. A Dynamax Candy raises it a level.
    /// </summary>
    public int DynamaxLevel { get; set; }

    /// <summary>It has the Gigantamax Factor (plan 06 · R22): a species with a Gigantamax form takes it as it Dynamaxes, and its moves of one type become its G-Max Move.</summary>
    public bool GigantamaxFactor { get; set; }

    /// <summary>
    /// The partner it has taken in (plan 06 · R29, <see cref="FormChanges"/>): a Kyurem's Zekrom or Reshiram, a Necrozma's
    /// Solgaleo or Lunala, a Calyrex's steed, kept whole inside it until the two are split again; null for one fused with
    /// nobody. Saved with it.
    /// </summary>
    public Pokemon? FusedWith { get; set; }

    /// <summary>A Dynamax raises or lowers both its HP and its most HP at once (plan 06 · R22): only a battle's rules do it, and give the most back as the Dynamax ends.</summary>
    internal void TakeHp(int max, int current)
    {
        MaxHP = max;
        CurrentHP = Math.Clamp(current, 0, max);
    }

    /// <summary>The Tera Type given to it (a Tera Shard's, a trainer's), or null for the one it has by default.</summary>
    private TeraType? teraType;

    /// <summary>
    /// Its Tera Type (plan 06 · R23; Showdown's <c>teraType</c>): the type it takes as it Terastallizes. Ogerpon's is
    /// its mask's and Terapagos's Stellar whatever is set (Showdown's <c>forceTeraType</c>); any other Pokémon's is the
    /// one given to it, or its own first type where none was (our default: no draw, so no battle's dice move for it).
    /// </summary>
    public TeraType TeraType
    {
        get => Models.TeraType.ForcedFor(this) ?? teraType ?? Models.TeraType.Of(PrimaryType);
        set => teraType = value;
    }

    /// <summary>A Tera Type was given to it (what a save keeps); without one it is its first type.</summary>
    public bool HasOwnTeraType => teraType != null;

    /// <summary>
    /// The Tera Type it has Terastallized into in the battle being fought (plan 06 · R23), through switches until the
    /// battle ends; null when it hasn't. Only a battle's rules set it, and the screen's copy from the log's event.
    /// </summary>
    internal TeraType? Terastallized { get; set; }

    /// <summary>The types a Stellar Terastallization has spent its one boost on, by name, "Stellar" among them (Showdown's <c>stellarBoostedTypes</c>).</summary>
    internal HashSet<string>? StellarBoosted { get; set; }

    /// <summary>The move it is using now has the Stellar boost (Showdown's <c>move.stellarBoosted</c>): every hit of the use takes it.</summary>
    internal bool StellarThisUse { get; set; }

    /// <summary>
    /// Where it was met (plan 06 · R12): the name of the place, as the original writes the location's name, or a
    /// name of its own for how it came ("a trade"); null for one met before this was kept.
    /// </summary>
    public string? MetLocation { get; set; }

    /// <summary>Its level when it was met (0 for one hatched, as in the original).</summary>
    public int MetLevel { get; set; }

    /// <summary>The day it was met (the game's own day, <see cref="Core.GameClock.Today"/>).</summary>
    public DateTime? MetDate { get; set; }

    /// <summary>Notes where, on what day and at what level it was met, as catching it or being given it does.</summary>
    public void Met(string? place, DateTime day)
    {
        MetLocation = place;
        MetLevel = Level;
        MetDate = day.Date;
    }

    /// <summary>
    /// The language of the game it came from, when not this one's (plan 06 · R12): a Pokémon from abroad gains 1.7
    /// times the EXP instead of 1.5. Null for one of this game's language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>What it has done toward an evolution that counts something: steps walked, uses of a move, foes knocked out
    /// (the keys are <see cref="Evolution"/>'s).</summary>
    public Dictionary<string, int> EvolutionProgress { get; private set; } = new();

    // Stats
    public int CurrentHP { get; set; }
    public int MaxHP { get; private set; }
    public int Attack { get; internal set; }
    public int Defense { get; internal set; }
    public int SpAttack { get; internal set; }
    public int SpDefense { get; internal set; }
    public int Speed { get; internal set; }

    // IVs (0 - 31)
    public int IvHP { get; set; }
    public int IvAttack { get; set; }
    public int IvDefense { get; set; }
    public int IvSpAttack { get; set; }
    public int IvSpDefense { get; set; }
    public int IvSpeed { get; set; }

    // EVs (0 - 255)
    public int EvHP { get; set; }
    public int EvAttack { get; set; }
    public int EvDefense { get; set; }
    public int EvSpAttack { get; set; }
    public int EvSpDefense { get; set; }
    public int EvSpeed { get; set; }

    // Battle Transient States
    public StatusCondition Status { get; set; } = StatusCondition.None;
    public int SleepTurns { get; set; } = 0;
    public int ToxicCounter { get; set; } = 0;
    public Dictionary<StatType, int> StatStages { get; private set; } = new();

    // Progression
    public int CurrentExp { get; set; }
    public int ExpForCurrentLevel => GetExpForLevel(Level, Species.GrowthRate);
    public int ExpForNextLevel => GetExpForLevel(Level + 1, Species.GrowthRate);
    /// <summary>
    /// The level no Pokémon grows past in the game in progress (plan 18 · W4): the rules' <see cref="Ruleset.LevelCap"/>,
    /// 100 in Platinum and <see cref="Ruleset.HighestLevel"/> by the modern rules. A battle asks its own rules instead.
    /// </summary>
    public static int Cap => Ruleset.Current.LevelCap;

    /// <summary>Whether it has reached the level cap of the game in progress: no more EXP, no Rare Candy.</summary>
    public bool AtTheCap => Level >= Cap;

    public float ExpProgressRatio
    {
        get
        {
            if (AtTheCap) return 1.0f;
            int baseExp = ExpForCurrentLevel;
            int nextExp = ExpForNextLevel;
            int span = nextExp - baseExp;
            if (span <= 0) return 0f;
            return Math.Clamp((float)(CurrentExp - baseExp) / span, 0f, 1f);
        }
    }

    public List<Move> Moves { get; private set; } = new();

    /// <summary>What it is called wherever it is named: an Egg's name, its nickname, or its species' name in the chosen language.</summary>
    public string DisplayName => IsEgg ? EggName : string.IsNullOrWhiteSpace(Nickname) ? Species.DisplayName : Nickname;

    /// <summary>What an Egg is called wherever it is named.</summary>
    public const string EggName = "Egg";

    /// <summary>
    /// Whether it is still an Egg (plan 06 · R15; the original's <c>MON_DATA_IS_EGG</c>): its species is what will
    /// hatch, and until then its <see cref="Friendship"/> counts the egg cycles left, as the original keeps them. An
    /// Egg is never sent into battle, gains nothing, takes no item or move and is named <see cref="EggName"/>; it counts
    /// as fainted for everything that asks who can fight (<see cref="IsFainted"/>). <see cref="Breeding"/> makes Eggs
    /// and hatches them.
    /// </summary>
    public bool IsEgg { get; set; }

    /// <summary>An Egg's cycles left before it hatches: its friendship, as the original keeps them (<c>Daycare_Update</c>).</summary>
    public int EggCycles
    {
        get => IsEgg ? Friendship : 0;
        set { if (IsEgg) Friendship = Math.Clamp(value, 0, 255); }
    }

    /// <summary>
    /// The form it is in (plan 03 · D11): null for its species' own, else the name of one of
    /// <see cref="PokemonSpecies.Forms"/>. Its types, base stats, abilities, size and moves follow it; change it
    /// with <see cref="ChangeForm"/>.
    /// </summary>
    public string? Form { get; internal set; }

    /// <summary>What its form has of its own; null in its species' own form.</summary>
    public PokemonForm? FormData => Species.Form(Form);

    public PokemonType PrimaryType => FormData?.Types is { Count: > 0 } types ? types[0] : Species.PrimaryType;
    public PokemonType? SecondaryType => FormData?.Types is { Count: > 0 } types ? (types.Count > 1 ? types[1] : null) : Species.SecondaryType;
    public bool HasType(PokemonType type) => PrimaryType == type || SecondaryType == type;

    /// <summary>The abilities its form can have, in the order the game picks from.</summary>
    public IReadOnlyList<string> Abilities => FormData?.Abilities ?? Species.Abilities;

    /// <summary>
    /// The moves its form learns by level, under the rules in force (the newest games' list under the modern ones,
    /// plan 06 · R19), less any move that does nothing in a battle yet (<see cref="MoveDatabase.Runs"/>): those join
    /// as R24 to R26 give them their effects.
    /// </summary>
    public IReadOnlyList<LearnableMove> Learnset
    {
        get
        {
            var all = FormData?.Learnset ?? Species.Learnset;
            return all.All(m => MoveDatabase.Runs(m.MoveName)) ? all : all.Where(m => MoveDatabase.Runs(m.MoveName)).ToList();
        }
    }

    public float Height => FormData?.Height ?? Species.Height;
    public float Weight => FormData?.Weight ?? Species.Weight;
    public int BaseExpYield => FormData?.BaseExpYield ?? Species.BaseExpYield;

    /// <summary>The effort it leaves the Pokémon that beat it: its form's, or its species'.</summary>
    public StatSpread? EvYield => FormData?.EvYield ?? Species.EvYield;
    public int CatchRate => FormData?.CatchRate ?? Species.CatchRate;

    /// <summary>The name its model and sprites are asked for by: its form's, or its species'.</summary>
    public string ModelName => Form ?? Species.Name;

    /// <summary>
    /// The name this very Pokémon's look is asked for by (plan 09 · L1, L6): its <see cref="ModelName"/>, with
    /// <c>+f</c> after it when it is female and its model has a female look, then <c>+s</c> when it is shiny
    /// (<see cref="PokemonLook"/>). Whatever shows a particular Pokémon (its model in battle, its menu sprites, its
    /// field sprite) asks by this; a cry, a size or the data go by <see cref="ModelName"/>.
    /// </summary>
    public string LookName => IsEgg ? ModelName : PokemonLook.Of(ModelName, Gender == Gender.Female, IsShiny);
    /// <summary>Down and out, or an Egg: either way it can't be sent into battle or chosen to fight.</summary>
    public bool IsFainted => IsEgg || CurrentHP <= 0 || Status == StatusCondition.Faint;

    /// <summary>
    /// A new Pokémon as the original makes one (<c>Pokemon_InitWith</c>, plan 06 · R15): its personality is drawn
    /// first and decides its nature, gender, ability and whether it is shiny (<see cref="Models.Personality"/>), against
    /// the player's number (<see cref="Core.PlayerIdentity.Number"/>: a Pokémon met, given or hatched is shiny for the
    /// trainer who has it); then its IVs, from two more draws of three each.
    /// </summary>
    /// <param name="gender">The gender it must have (a wild Pokémon met with Cute Charm at the head of the party): Cute Charm's built personality; left out, by the personality drawn.</param>
    /// <param name="nature">The nature it must have (Synchronize): personalities are drawn until one gives it; left out, any.</param>
    public Pokemon(PokemonSpecies species, int level, Random? rng = null, Gender? gender = null, Nature? nature = null)
        : this(species, level, 0u, rng ?? Core.Dice.New(), gender, nature, choose: true)
    {
    }

    /// <summary>
    /// A new Pokémon whose personality is given (a trainer's, a roamer's, an Egg's, the Poké Radar's sparkling one): its
    /// nature, gender, ability and shininess are the personality's; the IVs are drawn from <paramref name="rng"/>.
    /// </summary>
    public Pokemon(PokemonSpecies species, int level, uint personality, Random? rng = null)
        : this(species, level, personality, rng ?? Core.Dice.New(), null, null, choose: false)
    {
    }

    private Pokemon(PokemonSpecies species, int level, uint personality, Random rng, Gender? gender, Nature? nature, bool choose)
    {
        Species = species;
        // Held to the highest level any rules allow, whatever the game's own cap (plan 18 · W4): a save made with a
        // level past 100 loads as it was
        Level = Math.Clamp(level, 1, Ruleset.HighestLevel);
        Personality = choose ? Models.Personality.Choose(species, rng, gender, nature) : personality;
        // A gender the species can't help having is its own whatever the personality (Cute Charm on one of them)
        Gender = Models.Personality.GenderOf(species, Personality);
        Form = FormOfGender();
        Nature = Models.Personality.NatureOf(Personality);
        AbilityName = Models.Personality.AbilityOf(Abilities, Personality);
        // One in 8,192 by Platinum's rules, one in 4,096 by the modern ones (plan 06 · R10)
        IsShiny = Models.Personality.IsShiny(Core.PlayerIdentity.Number, Personality, Ruleset.Current.ShinyOdds);

        // The original's two draws of three IVs each: HP, Attack, Defense, then Speed, Sp. Atk, Sp. Def
        int first = rng.Next(1 << 16), second = rng.Next(1 << 16);
        IvHP = first & 31;
        IvAttack = first >> 5 & 31;
        IvDefense = first >> 10 & 31;
        IvSpeed = second & 31;
        IvSpAttack = second >> 5 & 31;
        IvSpDefense = second >> 10 & 31;
        Friendship = species.BaseFriendship;

        CurrentExp = GetExpForLevel(Level, Species.GrowthRate);
        RecalculateStats();
        CurrentHP = MaxHP;

        // Populate initial moves
        PopulateMovesForLevel();
        ResetStatStages();
    }

    public Pokemon(PokemonSpecies species, int level, Gender gender, Nature nature, bool isShiny)
    {
        Species = species;
        Level = Math.Clamp(level, 1, Ruleset.HighestLevel);
        Gender = gender;
        Form = FormOfGender();
        Nature = nature;
        IsShiny = isShiny;
        AbilityName = Abilities.FirstOrDefault();
        Personality = RollPersonality(Core.Dice.Shared);
        Friendship = species.BaseFriendship;

        CurrentExp = GetExpForLevel(Level, Species.GrowthRate);
        RecalculateStats();
        CurrentHP = MaxHP;

        PopulateMovesForLevel();
        ResetStatStages();
    }

    /// <summary>Male or female by the species' ratio, or genderless.</summary>
    public static Gender RollGender(PokemonSpecies species, Random rng)
    {
        if (species.IsGenderless) return Gender.Genderless;
        return rng.Next(8) < species.GenderRatio ? Gender.Female : Gender.Male;
    }

    private static uint RollPersonality(Random rng) => ((uint)rng.Next(1 << 16) << 16) | (uint)rng.Next(1 << 16);

    public bool Knows(string moveName) => Moves.Any(m => m.Name.Equals(moveName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Learns a move if one of its four places is free; false when it is full or knows the move already.</summary>
    public bool TryLearn(string moveName)
    {
        if (Moves.Count >= 4 || Knows(moveName)) return false;
        Moves.Add(MoveDatabase.Create(moveName));
        return true;
    }

    /// <summary>Forgets the move in a place and learns another there.</summary>
    public void ReplaceMove(int index, string moveName)
    {
        if (index >= 0 && index < Moves.Count) Moves[index] = MoveDatabase.Create(moveName);
    }

    public void CalculateStats() => RecalculateStats();

    /// <summary>Puts it at a level without anything a level brings (the Day Care's growing, <see cref="DayCare.Grow"/>).</summary>
    internal void LevelTo(int level) => Level = Math.Clamp(level, 1, Ruleset.HighestLevel);

    /// <summary>
    /// Puts a Pokémon not yet anyone's at another level, as if it had been made there (plan 18 · W4: a region's levels
    /// mapped for a team that crossed into it, <see cref="Data.LevelMapping"/>): its EXP is the level's, its stats are
    /// worked out again and its HP keeps its share. Moves chosen for it are kept; moves that are only what its old level
    /// taught it are those of the new level.
    /// </summary>
    public void Relevel(int level)
    {
        level = Math.Clamp(level, 1, Ruleset.HighestLevel);
        if (level == Level) return;
        bool byLevel = Moves.Select(m => m.Name).SequenceEqual(MovesLearnedBy(Level));
        bool whole = CurrentHP >= MaxHP;
        float share = MaxHP > 0 ? (float)CurrentHP / MaxHP : 1f;
        Level = level;
        CurrentExp = GetExpForLevel(Level, Species.GrowthRate);
        RecalculateStats();
        CurrentHP = whole ? MaxHP : CurrentHP <= 0 ? 0 : Math.Clamp((int)MathF.Round(MaxHP * share), 1, MaxHP);
        if (byLevel) PopulateMovesForLevel();
    }

    /// <summary>
    /// Works its stats out again and moves its HP by what its maximum moved (<c>Pokemon_CalcStats</c>): what the
    /// original does after a vitamin. A fainted Pokémon stays down.
    /// </summary>
    public void ReckonStats()
    {
        int oldMax = MaxHP;
        RecalculateStats();
        if (CurrentHP > 0) CurrentHP = Math.Clamp(CurrentHP + MaxHP - oldMax, 1, MaxHP);
    }

    /// <summary>
    /// Its six stats as they stand, in a spread of their own that doesn't follow the Pokémon afterwards: what a level
    /// is measured from and to (plan 08 · P5: the log's <see cref="Battle.Sim.LevelRose"/>, a Rare Candy's panel).
    /// </summary>
    public StatSpread Stats() => new() { HP = MaxHP, Attack = Attack, Defense = Defense, SpAttack = SpAttack, SpDefense = SpDefense, Speed = Speed };

    /// <summary>
    /// Takes six stats worked out elsewhere, its HP moving by what its maximum moved (a fainted one stays down). A
    /// battle's screen brings its Pokémon to the rules' numbers with it as a level is shown (plan 08 · P5): the
    /// rules' copy was given the beaten foe's effort before its level and the screen's wasn't, so the screen's own
    /// reckoning can be a point short until the two are made the same again.
    /// </summary>
    internal void TakeStats(StatSpread stats)
    {
        int oldMax = MaxHP;
        MaxHP = stats.HP;
        Attack = stats.Attack;
        Defense = stats.Defense;
        SpAttack = stats.SpAttack;
        SpDefense = stats.SpDefense;
        Speed = stats.Speed;
        if (CurrentHP > 0) CurrentHP = Math.Clamp(CurrentHP + MaxHP - oldMax, 1, MaxHP);
    }

    public void RecalculateStats()
    {
        // A form with base stats of its own (Rotom's appliances, a Mega) is reckoned from them
        var form = FormData?.BaseStats;
        int baseHP = form?.HP ?? Species.BaseHP;
        // Shedinja's one hit point is a rule of its own, not the formula's
        // A mint's nature and hyper training count here alone (plan 06 · R28b)
        var nature = StatNature;
        MaxHP = baseHP == 1 ? 1 : CalculateHP(baseHP, StatIv(StatType.HP, IvHP), EvHP, Level);
        Attack = CalculateOtherStat(form?.Attack ?? Species.BaseAttack, StatIv(StatType.Attack, IvAttack), EvAttack, Level, GetNatureMultiplier(nature, StatType.Attack));
        Defense = CalculateOtherStat(form?.Defense ?? Species.BaseDefense, StatIv(StatType.Defense, IvDefense), EvDefense, Level, GetNatureMultiplier(nature, StatType.Defense));
        SpAttack = CalculateOtherStat(form?.SpAttack ?? Species.BaseSpAttack, StatIv(StatType.SpAttack, IvSpAttack), EvSpAttack, Level, GetNatureMultiplier(nature, StatType.SpAttack));
        SpDefense = CalculateOtherStat(form?.SpDefense ?? Species.BaseSpDefense, StatIv(StatType.SpDefense, IvSpDefense), EvSpDefense, Level, GetNatureMultiplier(nature, StatType.SpDefense));
        Speed = CalculateOtherStat(form?.Speed ?? Species.BaseSpeed, StatIv(StatType.Speed, IvSpeed), EvSpeed, Level, GetNatureMultiplier(nature, StatType.Speed));
    }

    private static int CalculateHP(int baseStat, int iv, int ev, int level)
    {
        return ((2 * baseStat + iv + (ev / 4)) * level / 100) + level + 10;
    }

    private static int CalculateOtherStat(int baseStat, int iv, int ev, int level, float natureMult)
    {
        int raw = (((2 * baseStat + iv + (ev / 4)) * level / 100) + 5);
        return (int)Math.Floor(raw * natureMult);
    }

    private static float GetNatureMultiplier(Nature nature, StatType stat)
    {
        // Gen 4 Nature table
        return (nature, stat) switch
        {
            (Nature.Lonely, StatType.Attack) => 1.1f,
            (Nature.Lonely, StatType.Defense) => 0.9f,
            (Nature.Brave, StatType.Attack) => 1.1f,
            (Nature.Brave, StatType.Speed) => 0.9f,
            (Nature.Adamant, StatType.Attack) => 1.1f,
            (Nature.Adamant, StatType.SpAttack) => 0.9f,
            (Nature.Naughty, StatType.Attack) => 1.1f,
            (Nature.Naughty, StatType.SpDefense) => 0.9f,

            (Nature.Bold, StatType.Defense) => 1.1f,
            (Nature.Bold, StatType.Attack) => 0.9f,
            (Nature.Relaxed, StatType.Defense) => 1.1f,
            (Nature.Relaxed, StatType.Speed) => 0.9f,
            (Nature.Impish, StatType.Defense) => 1.1f,
            (Nature.Impish, StatType.SpAttack) => 0.9f,
            (Nature.Lax, StatType.Defense) => 1.1f,
            (Nature.Lax, StatType.SpDefense) => 0.9f,

            (Nature.Timid, StatType.Speed) => 1.1f,
            (Nature.Timid, StatType.Attack) => 0.9f,
            (Nature.Hasty, StatType.Speed) => 1.1f,
            (Nature.Hasty, StatType.Defense) => 0.9f,
            (Nature.Jolly, StatType.Speed) => 1.1f,
            (Nature.Jolly, StatType.SpAttack) => 0.9f,
            (Nature.Naive, StatType.Speed) => 1.1f,
            (Nature.Naive, StatType.SpDefense) => 0.9f,

            (Nature.Modest, StatType.SpAttack) => 1.1f,
            (Nature.Modest, StatType.Attack) => 0.9f,
            (Nature.Mild, StatType.SpAttack) => 1.1f,
            (Nature.Mild, StatType.Defense) => 0.9f,
            (Nature.Quiet, StatType.SpAttack) => 1.1f,
            (Nature.Quiet, StatType.Speed) => 0.9f,
            (Nature.Rash, StatType.SpAttack) => 1.1f,
            (Nature.Rash, StatType.SpDefense) => 0.9f,

            (Nature.Calm, StatType.SpDefense) => 1.1f,
            (Nature.Calm, StatType.Attack) => 0.9f,
            (Nature.Gentle, StatType.SpDefense) => 1.1f,
            (Nature.Gentle, StatType.Defense) => 0.9f,
            (Nature.Sassy, StatType.SpDefense) => 1.1f,
            (Nature.Sassy, StatType.Speed) => 0.9f,
            (Nature.Careful, StatType.SpDefense) => 1.1f,
            (Nature.Careful, StatType.SpAttack) => 0.9f,

            _ => 1.0f
        };
    }

    /// <summary>
    /// The EXP a level starts at (<c>Pokemon_GetExpToLevel</c>'s six curves). Past 100 (plan 18 · W4) the four cubic
    /// curves go on as they are; Erratic's last branch would fall after 100 and turn negative at 160, and Fluctuating has
    /// no meaning there, so by a rule of our own both keep the share of Medium Fast's cube they have at 100: Erratic
    /// 600,000 × (level / 100)³ and Fluctuating 1,640,000 × (level / 100)³, rounded down (<c>docs/mechanics/rulings.md</c>,
    /// "The level cap past 100").
    /// </summary>
    public static int GetExpForLevel(int level, GrowthRate growthRate)
    {
        if (level <= 1) return 0;
        if (level > 100 && growthRate is GrowthRate.Erratic or GrowthRate.Fluctuating)
        {
            long atHundred = GetExpForLevel(100, growthRate);
            return (int)(atHundred * level * level * level / 1_000_000L);
        }
        double n = level;
        return growthRate switch
        {
            GrowthRate.Fast => (int)(0.8 * Math.Pow(n, 3)),
            GrowthRate.MediumFast => (int)Math.Pow(n, 3),
            GrowthRate.MediumSlow => (int)(1.2 * Math.Pow(n, 3) - 15 * Math.Pow(n, 2) + 100 * n - 140),
            GrowthRate.Slow => (int)(1.25 * Math.Pow(n, 3)),
            GrowthRate.Erratic => level switch
            {
                < 50 => level * level * level * (100 - level) / 50,
                < 68 => level * level * level * (150 - level) / 100,
                < 98 => level * level * level * ((1911 - 10 * level) / 3) / 500,
                _ => level * level * level * (160 - level) / 100
            },
            GrowthRate.Fluctuating => level switch
            {
                < 15 => level * level * level * ((level + 1) / 3 + 24) / 50,
                < 36 => level * level * level * (level + 14) / 50,
                _ => level * level * level * (level / 2 + 32) / 50
            },
            _ => (int)Math.Pow(n, 3)
        };
    }

    /// <summary>The moves a Pokémon made at a level knows (<see cref="PopulateMovesForLevel"/>): the last four it would have learned, each once, or Tackle.</summary>
    private List<string> MovesLearnedBy(int level)
    {
        var names = Learnset
            .Where(m => m.Level <= level)
            .OrderByDescending(m => m.Level)
            .DistinctBy(m => m.MoveName)
            .Take(4)
            .Reverse()
            .Select(m => m.MoveName)
            .ToList();
        if (names.Count == 0) names.Add("Tackle");
        return names;
    }

    public void PopulateMovesForLevel()
    {
        Moves.Clear();
        // The last four it would have learned by now, each move once
        foreach (var name in MovesLearnedBy(Level))
        {
            Moves.Add(MoveDatabase.Create(name));
        }
    }

    public void ResetStatStages()
    {
        StatStages.Clear();
        StatStages[StatType.Attack] = 0;
        StatStages[StatType.Defense] = 0;
        StatStages[StatType.SpAttack] = 0;
        StatStages[StatType.SpDefense] = 0;
        StatStages[StatType.Speed] = 0;
        StatStages[StatType.Accuracy] = 0;
        StatStages[StatType.Evasion] = 0;
    }

    public float GetStatStageMultiplier(StatType stat)
    {
        if (!StatStages.TryGetValue(stat, out int stage)) stage = 0;
        stage = Math.Clamp(stage, -6, 6);

        if (stat == StatType.Accuracy || stat == StatType.Evasion)
        {
            return stage >= 0 ? (3f + stage) / 3f : 3f / (3f - stage);
        }

        return stage >= 0 ? (2f + stage) / 2f : 2f / (2f - stage);
    }

    public int GetEffectiveStat(StatType stat)
    {
        float stageMult = GetStatStageMultiplier(stat);
        int baseVal = stat switch
        {
            StatType.Attack => Attack,
            StatType.Defense => Defense,
            StatType.SpAttack => SpAttack,
            StatType.SpDefense => SpDefense,
            StatType.Speed => Speed,
            _ => 100
        };

        if (stat == StatType.Speed && Status == StatusCondition.Paralyze)
        {
            stageMult *= 0.25f; // Gen 4 paralysis drops speed to 25%
        }

        return (int)Math.Max(1, Math.Floor(baseVal * stageMult));
    }

    /// <summary>
    /// Adds EXP and levels up as far as it reaches; true if the level rose. Evolution is not part of it: the games
    /// evolve once the battle (or the item's use) is over, so ask <see cref="Evolution.Find"/> then.
    /// </summary>
    public bool GainExp(int expGained, out List<string> newMovesLearned) => GainExp(expGained, out newMovesLearned, out _);

    /// <param name="newMovesLearned">The moves of the levels reached that it learned, into a free place.</param>
    /// <param name="movesWanted">
    /// Those it couldn't, knowing four already: the game asks which move to forget for each (the original's
    /// <c>LEARNSET_ALL_SLOTS_FILLED</c>). A move it knows already is neither.
    /// </param>
    /// <param name="cap">
    /// The level it grows no further than: a battle's own rules' (<see cref="Ruleset.LevelCap"/>), or the game's
    /// (<see cref="Cap"/>) when none is given (plan 18 · W4).
    /// </param>
    public bool GainExp(int expGained, out List<string> newMovesLearned, out List<string> movesWanted, int? cap = null)
    {
        newMovesLearned = new List<string>();
        movesWanted = new List<string>();
        int top = Math.Min(cap ?? Cap, Ruleset.HighestLevel);
        if (Level >= top) return false;

        CurrentExp += expGained;
        bool leveledUp = false;

        while (Level < top && CurrentExp >= ExpForNextLevel)
        {
            Level++;
            leveledUp = true;
            FriendshipRules.Apply(this, FriendshipEvent.LevelUp);
            int oldMaxHP = MaxHP;
            RecalculateStats();
            CurrentHP += (MaxHP - oldMaxHP); // Keep HP difference

            // The moves of the new level, in the learnset's order (Pokemon_LevelUpMove): learned into a free
            // place, or wanted when there is none; one it knows already is passed over
            foreach (var m in Learnset.Where(m => m.Level == Level))
            {
                if (Knows(m.MoveName) || movesWanted.Contains(m.MoveName)) continue;
                if (Moves.Count < 4)
                {
                    Moves.Add(MoveDatabase.Create(m.MoveName));
                    newMovesLearned.Add(m.MoveName);
                }
                else
                {
                    movesWanted.Add(m.MoveName);
                }
            }
        }

        return leveledUp;
    }

    /// <summary>
    /// Becomes another species: the ability keeps its slot, a Pokémon without a nickname takes the new name, and
    /// the hit points it gains are added to the ones it has (a fainted one stays down).
    /// </summary>
    public void EvolveInto(PokemonSpecies next)
    {
        string oldName = Species.Name;
        int abilitySlot = Math.Max(0, Abilities.ToList().IndexOf(AbilityName ?? ""));
        // A hidden ability (an Ability Patch's, plan 06 · R28b) stays the hidden one of what it becomes
        bool hidden = AbilityName != null && AbilityName == HiddenAbilityName && !Abilities.Contains(AbilityName);
        int oldMaxHP = MaxHP;

        // It becomes the new species' own form (or its females'); Evolution.Evolve gives it another one where the evolution says so
        Species = next;
        Form = FormOfGender();
        var abilities = Abilities;
        if (hidden && HiddenAbilityName is { } stillHidden) AbilityName = stillHidden;
        else if (abilities.Count > 0) AbilityName = abilities[Math.Min(abilitySlot, abilities.Count - 1)];
        // A Pokémon with no nickname is called what its species is; one called by its old species' name (older saves wrote it in) has none either
        if (string.IsNullOrWhiteSpace(Nickname) || Nickname == oldName) Nickname = string.Empty;

        RecalculateStats();
        if (CurrentHP > 0) CurrentHP = Math.Clamp(CurrentHP + MaxHP - oldMaxHP, 1, MaxHP);
    }

    /// <summary>
    /// Takes another of its species' forms (null for the species' own): its ability keeps its slot in the form's
    /// list, and its hit points change with its maximum (a fainted one stays down). Whatever makes it change
    /// (Rotom's appliances, the Griseous Orb, Mega Evolution) is plan 06's; a name the species has no form of is
    /// refused.
    /// </summary>
    public void ChangeForm(string? form)
    {
        if (form != null && Species.Form(form) == null) throw new ArgumentException($"{Species.Name} has no form {form}", nameof(form));
        int abilitySlot = Math.Max(0, Abilities.ToList().IndexOf(AbilityName ?? ""));
        // A hidden ability (an Ability Patch's, plan 06 · R28b) stays the hidden one of what it becomes
        bool hidden = AbilityName != null && AbilityName == HiddenAbilityName && !Abilities.Contains(AbilityName);
        int oldMaxHP = MaxHP;

        Form = Species.Form(form)?.Name;
        var abilities = Abilities;
        if (hidden && HiddenAbilityName is { } stillHidden) AbilityName = stillHidden;
        else if (abilities.Count > 0) AbilityName = abilities[Math.Min(abilitySlot, abilities.Count - 1)];

        RecalculateStats();
        if (CurrentHP > 0) CurrentHP = Math.Clamp(CurrentHP + MaxHP - oldMaxHP, 1, MaxHP);
    }

    /// <summary>
    /// A new Pokémon met in a form of its species (plan 03 · D13): one of the species put into the form, knowing
    /// what the form itself learns by its level where the form has a learnset of its own (a regional form, a cloak
    /// of Wormadam's, a forme of Deoxys's). <see cref="ChangeForm"/> alone leaves a Pokémon the moves it had, which
    /// is right for one that changes form and is how the original makes a trainer's (its form is set after its
    /// moves): so nothing of Platinum's is made this way, and whatever brings a form with moves of its own into the
    /// game (plan 03 · D12a's sources of forms, the later regions' plans) is.
    /// </summary>
    public static Pokemon InForm(PokemonSpecies species, string form, int level, Random? rng = null)
    {
        var pokemon = new Pokemon(species, level, rng);
        pokemon.ChangeForm(form);
        if (pokemon.FormData?.Learnset != null) pokemon.PopulateMovesForLevel();
        return pokemon;
    }

    private string? FormOfGender() => FormOfGender(Species, Gender);

    /// <summary>
    /// The form a gender puts a Pokémon of this species in: a female of a species whose females are a form of their
    /// own (Meowstic and Indeedee with their own abilities, Basculegion, Pyroar's mane) is in it from the start, and
    /// anyone else in none.
    /// </summary>
    public static string? FormOfGender(PokemonSpecies species, Gender gender) =>
        gender == Gender.Female ? species.Form(species.Name + "-Female")?.Name : null;

    /// <summary>Puts it in a form as it is saved, leaving its ability as the save has it.</summary>
    internal void RestoreForm(string? form)
    {
        Form = Species.Form(form)?.Name;
        RecalculateStats();
    }

    public void HealFull()
    {
        CurrentHP = MaxHP;
        Status = StatusCondition.None;
        SleepTurns = 0;
        ToxicCounter = 0;
        ResetStatStages();
        foreach (var m in Moves)
        {
            m.RestorePP();
        }
    }

    public void Revive(int hp)
    {
        CurrentHP = Math.Clamp(hp, 1, MaxHP);
        Status = StatusCondition.None;
        SleepTurns = 0;
        ToxicCounter = 0;
        ResetStatStages();
    }

    /// <summary>
    /// A copy that shares nothing that can change: its moves, stat stages and counters are its own. A battle's
    /// rules work on copies (<see cref="Battle.Sim.BattleCore"/>), and what they did reaches the Pokémon itself as
    /// the battle is shown.
    /// </summary>
    public Pokemon Clone()
    {
        var copy = (Pokemon)MemberwiseClone();
        copy.Moves = Moves.Select(m => new Move(m.Data, m.CurrentPP, m.PPUps)).ToList();
        copy.StatStages = new Dictionary<StatType, int>(StatStages);
        copy.EvolutionProgress = new Dictionary<string, int>(EvolutionProgress);
        copy.Ribbons = new SortedSet<string>(Ribbons, StringComparer.Ordinal);
        return copy;
    }

    /// <summary>
    /// Takes on everything of another Pokémon that can change, so that the two are the same again. A move it
    /// already has in the same place stays the same object (only its PP is taken), so whoever holds on to one of
    /// its moves still holds the right one. <c>PokemonTests</c> holds this to every property the class has.
    /// </summary>
    public void CopyStateFrom(Pokemon other)
    {
        Id = other.Id;
        Region = other.Region;
        Species = other.Species;
        Form = other.Form;
        Nickname = other.Nickname;
        IsEgg = other.IsEgg;
        Level = other.Level;
        Gender = other.Gender;
        Nature = other.Nature;
        MintNature = other.MintNature;
        HyperTrained = other.HyperTrained;
        IsShiny = other.IsShiny;
        AbilityName = other.AbilityName;
        HeldItem = other.HeldItem;
        Friendship = other.Friendship;
        Cool = other.Cool;
        Beauty = other.Beauty;
        Cute = other.Cute;
        Smart = other.Smart;
        Tough = other.Tough;
        Sheen = other.Sheen;
        Personality = other.Personality;
        Ball = other.Ball;
        OriginalTrainer = other.OriginalTrainer;
        Pokerus = other.Pokerus;
        Marks = other.Marks;
        CapsuleId = other.CapsuleId;
        Capsule = other.Capsule;
        DynamaxLevel = other.DynamaxLevel;
        GigantamaxFactor = other.GigantamaxFactor;
        FusedWith = other.FusedWith;
        teraType = other.teraType;
        MetLocation = other.MetLocation;
        MetLevel = other.MetLevel;
        MetDate = other.MetDate;
        Language = other.Language;

        CurrentHP = other.CurrentHP;
        MaxHP = other.MaxHP;
        Attack = other.Attack;
        Defense = other.Defense;
        SpAttack = other.SpAttack;
        SpDefense = other.SpDefense;
        Speed = other.Speed;
        IvHP = other.IvHP;
        IvAttack = other.IvAttack;
        IvDefense = other.IvDefense;
        IvSpAttack = other.IvSpAttack;
        IvSpDefense = other.IvSpDefense;
        IvSpeed = other.IvSpeed;
        EvHP = other.EvHP;
        EvAttack = other.EvAttack;
        EvDefense = other.EvDefense;
        EvSpAttack = other.EvSpAttack;
        EvSpDefense = other.EvSpDefense;
        EvSpeed = other.EvSpeed;

        Status = other.Status;
        SleepTurns = other.SleepTurns;
        ToxicCounter = other.ToxicCounter;
        CurrentExp = other.CurrentExp;

        StatStages.Clear();
        foreach (var (stat, stage) in other.StatStages) StatStages[stat] = stage;
        EvolutionProgress.Clear();
        foreach (var (key, count) in other.EvolutionProgress) EvolutionProgress[key] = count;
        Ribbons.Clear();
        Ribbons.UnionWith(other.Ribbons);

        for (int i = 0; i < other.Moves.Count; i++)
        {
            if (i < Moves.Count && Moves[i].Data == other.Moves[i].Data) Moves[i].CurrentPP = other.Moves[i].CurrentPP;
            else if (i < Moves.Count) Moves[i] = new Move(other.Moves[i].Data, other.Moves[i].CurrentPP, other.Moves[i].PPUps);
            else Moves.Add(new Move(other.Moves[i].Data, other.Moves[i].CurrentPP, other.Moves[i].PPUps));
        }
        if (Moves.Count > other.Moves.Count) Moves.RemoveRange(other.Moves.Count, Moves.Count - other.Moves.Count);
    }
}

/// <summary>
/// Who a Pokémon first belonged to, as the original marks it (<c>MON_DATA_OT_NAME</c>, <c>OT_ID</c>, <c>OT_GENDER</c>):
/// a Pokémon is the player's own when all three are the player's (<c>BattleSystem_PokemonIsOT</c>).
/// </summary>
public sealed record TrainerMark(string Name, int Id, Core.PlayerLook Look)
{
    /// <summary>Whether this is the given trainer.</summary>
    public bool Is(string name, int id, Core.PlayerLook look) => Name == name && Id == id && Look == look;
}
