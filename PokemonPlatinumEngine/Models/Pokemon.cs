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
    /// <summary>Where it must level up (<c>Mt. Coronet</c>, <c>Moss Rock</c>).</summary>
    public string? Location { get; set; }
    /// <summary>Beauty or affection needed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Value { get; set; }
    /// <summary>Plain words for <see cref="EvolutionMethod.Other"/> and anything else the fields can't hold.</summary>
    public string? Note { get; set; }
}

/// <summary>Points a species gives in each stat when it is defeated.</summary>
public class StatSpread
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int HP { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Attack { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Defense { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int SpAttack { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int SpDefense { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Speed { get; set; }
}

public class PokemonSpecies
{
    public int DexNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Pokémon"; // e.g. "Tiny Leaf Pokémon"
    /// <summary>The generation that introduced the species.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Generation { get; set; }
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
    public float Height { get; set; } = 0.5f; // meters
    public float Weight { get; set; } = 10.0f; // kg
    /// <summary>Body colour and shape, as the Pokédex sorts them (for the generated models of plan 03 · D5).</summary>
    public string? Color { get; set; }
    public string? Shape { get; set; }
    public string DexEntry { get; set; } = string.Empty;
    public List<LearnableMove> Learnset { get; set; } = new();
    public List<EvolutionData>? Evolutions { get; set; }

    /// <summary>The abilities this species can have: one or two, in the order the game picks from.</summary>
    public List<string> Abilities { get; set; } = new();

    /// <summary>Its hidden ability from Generation 5 on; Platinum has none, so new Pokémon never get it yet.</summary>
    public string? HiddenAbility { get; set; }

    /// <summary>The evolution the engine runs on level-up: a plain level with no other condition.</summary>
    [JsonIgnore]
    public EvolutionData? LevelEvolution => Evolutions?.FirstOrDefault(e => e.Method == EvolutionMethod.Level && e.Level > 0);

    [JsonIgnore]
    public bool IsGenderless => GenderRatio < 0;
}

public class Pokemon
{
    public PokemonSpecies Species { get; private set; }
    public string Nickname { get; set; }
    public int Level { get; private set; }
    public Gender Gender { get; set; }
    public Nature Nature { get; set; }
    public bool IsShiny { get; set; }

    /// <summary>The name of its ability (one of its species' abilities), or null if the species has none listed yet.</summary>
    public string? AbilityName { get; set; }
    public Ability? Ability => AbilityDatabase.Get(AbilityName);

    /// <summary>The item it holds (null = nothing).</summary>
    public ItemData? HeldItem { get; set; }

    // Stats
    public int CurrentHP { get; set; }
    public int MaxHP { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public int SpAttack { get; private set; }
    public int SpDefense { get; private set; }
    public int Speed { get; private set; }

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
    public Dictionary<StatType, int> StatStages { get; } = new();

    // Progression
    public int CurrentExp { get; set; }
    public int ExpForCurrentLevel => GetExpForLevel(Level, Species.GrowthRate);
    public int ExpForNextLevel => GetExpForLevel(Level + 1, Species.GrowthRate);
    public float ExpProgressRatio
    {
        get
        {
            if (Level >= 100) return 1.0f;
            int baseExp = ExpForCurrentLevel;
            int nextExp = ExpForNextLevel;
            int span = nextExp - baseExp;
            if (span <= 0) return 0f;
            return Math.Clamp((float)(CurrentExp - baseExp) / span, 0f, 1f);
        }
    }

    public List<Move> Moves { get; } = new();

    public string DisplayName => string.IsNullOrWhiteSpace(Nickname) ? Species.Name : Nickname;
    public bool IsFainted => CurrentHP <= 0 || Status == StatusCondition.Faint;

    public Pokemon(PokemonSpecies species, int level, Random? rng = null)
    {
        rng ??= new Random();
        Species = species;
        Nickname = species.Name;
        Level = Math.Clamp(level, 1, 100);
        Gender = RollGender(species, rng);
        Nature = (Nature)rng.Next(Enum.GetValues<Nature>().Length);
        IsShiny = rng.Next(8192) == 0;
        AbilityName = AbilityDatabase.PickFor(species, rng);

        IvHP = rng.Next(32);
        IvAttack = rng.Next(32);
        IvDefense = rng.Next(32);
        IvSpAttack = rng.Next(32);
        IvSpDefense = rng.Next(32);
        IvSpeed = rng.Next(32);

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
        Nickname = species.Name;
        Level = Math.Clamp(level, 1, 100);
        Gender = gender;
        Nature = nature;
        IsShiny = isShiny;
        AbilityName = AbilityDatabase.ForSpecies(species).FirstOrDefault();

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

    public void CalculateStats() => RecalculateStats();

    public void RecalculateStats()
    {
        MaxHP = CalculateHP(Species.BaseHP, IvHP, EvHP, Level);
        Attack = CalculateOtherStat(Species.BaseAttack, IvAttack, EvAttack, Level, GetNatureMultiplier(Nature, StatType.Attack));
        Defense = CalculateOtherStat(Species.BaseDefense, IvDefense, EvDefense, Level, GetNatureMultiplier(Nature, StatType.Defense));
        SpAttack = CalculateOtherStat(Species.BaseSpAttack, IvSpAttack, EvSpAttack, Level, GetNatureMultiplier(Nature, StatType.SpAttack));
        SpDefense = CalculateOtherStat(Species.BaseSpDefense, IvSpDefense, EvSpDefense, Level, GetNatureMultiplier(Nature, StatType.SpDefense));
        Speed = CalculateOtherStat(Species.BaseSpeed, IvSpeed, EvSpeed, Level, GetNatureMultiplier(Nature, StatType.Speed));
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

    public static int GetExpForLevel(int level, GrowthRate growthRate)
    {
        if (level <= 1) return 0;
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

    public void PopulateMovesForLevel()
    {
        Moves.Clear();
        // The last four it would have learned by now, each move once
        var availableMoves = Species.Learnset
            .Where(m => m.Level <= Level)
            .OrderByDescending(m => m.Level)
            .DistinctBy(m => m.MoveName)
            .Take(4)
            .Reverse();

        foreach (var m in availableMoves)
        {
            Moves.Add(MoveDatabase.Create(m.MoveName));
        }

        if (Moves.Count == 0)
        {
            Moves.Add(MoveDatabase.Create("Tackle"));
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

    public bool GainExp(int expGained, out List<string> newMovesLearned, out bool evolved, out string oldName)
    {
        newMovesLearned = new List<string>();
        evolved = false;
        oldName = Species.Name;
        if (Level >= 100) return false;

        CurrentExp += expGained;
        bool leveledUp = false;

        while (Level < 100 && CurrentExp >= ExpForNextLevel)
        {
            Level++;
            leveledUp = true;
            int oldMaxHP = MaxHP;
            RecalculateStats();
            CurrentHP += (MaxHP - oldMaxHP); // Keep HP difference

            // Check for new moves
            var movesToLearn = Species.Learnset.Where(m => m.Level == Level);
            foreach (var m in movesToLearn)
            {
                if (Moves.Count < 4)
                {
                    Moves.Add(MoveDatabase.Create(m.MoveName));
                    newMovesLearned.Add(m.MoveName);
                }
                else
                {
                    newMovesLearned.Add(m.MoveName);
                }
            }

            // Check for evolution
            if (Species.LevelEvolution is { } evolution && Level >= evolution.Level)
            {
                var nextSpecies = PokemonDatabase.Get(evolution.TargetSpecies);
                if (nextSpecies != null)
                {
                    oldName = Species.Name;
                    int abilitySlot = Math.Max(0, AbilityDatabase.ForSpecies(Species).ToList().IndexOf(AbilityName ?? ""));
                    Species = nextSpecies;
                    var abilities = AbilityDatabase.ForSpecies(Species);
                    if (abilities.Count > 0) AbilityName = abilities[Math.Min(abilitySlot, abilities.Count - 1)];
                    if (string.IsNullOrWhiteSpace(Nickname) || Nickname == oldName)
                    {
                        Nickname = Species.Name;
                    }
                    RecalculateStats();
                    evolved = true;
                }
            }
        }

        return leveledUp;
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
}

