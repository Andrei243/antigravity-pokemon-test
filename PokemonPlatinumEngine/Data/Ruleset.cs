using System.Collections.Generic;

namespace PokemonPlatinumEngine.Data;

/// <summary>Whose rules an adventure is played by. Chosen when it begins, kept in its save and never changed after.</summary>
public enum RulesPreset
{
    /// <summary>Platinum's own rules: the default, and what the story is balanced for.</summary>
    Platinum,
    /// <summary>The newest games' rules, wherever they changed something Platinum has.</summary>
    Modern
}

/// <summary>
/// Every number and rule that differs between the generations (plan 06, decision 1). Code that runs such a rule
/// asks the rules for it instead of knowing the number itself, so the two presets can't drift apart in a dozen
/// places. <c>docs/mechanics/rulings.md</c> lists what each preset says, including the differences that wait for
/// the session that writes their rule: add those here when their code is written, never as a constant beside it.
/// </summary>
public sealed class Ruleset
{
    public RulesPreset Preset { get; }

    /// <summary>The name the game shows for the preset.</summary>
    public string Name => Preset == RulesPreset.Modern ? "Modern rules" : "Platinum rules";

    /// <summary>A critical hit is one in this many, by critical-hit stage (0 to 4).</summary>
    public IReadOnlyList<int> CriticalOdds { get; private init; } = new[] { 16, 8, 4, 3, 2 };

    /// <summary>What a critical hit multiplies the damage by. Sniper raises it by half again.</summary>
    public float CriticalMultiplier { get; private init; } = 2f;

    /// <summary>A burn takes one part in this many of the Pokémon's HP at the end of each turn.</summary>
    public int BurnDamageDivisor { get; private init; } = 8;

    /// <summary>Paralysis divides Speed by this.</summary>
    public int ParalysisSpeedDivisor { get; private init; } = 4;

    /// <summary>What paralysis multiplies Speed by.</summary>
    public float ParalysisSpeed => 1f / ParalysisSpeedDivisor;

    /// <summary>Electric types can't be paralysed (from Generation 6).</summary>
    public bool ElectricTypesCantBeParalyzed { get; private init; }

    /// <summary>How many lengths a sleep can have, each as likely as the others.</summary>
    public int SleepLengths { get; private init; } = 4;

    /// <summary>A confused Pokémon hurts itself one time in this many.</summary>
    public int ConfusionSelfHitOdds { get; private init; } = 2;

    /// <summary>Steel resists Ghost and Dark, as it did until Generation 6.</summary>
    public bool SteelResistsGhostAndDark { get; private init; } = true;

    /// <summary>
    /// Moves have the power, accuracy and PP of the newest games (Tackle is 40 and never weaker for missing)
    /// instead of Platinum's (35, 95% accurate): <see cref="Models.MoveData.Modern"/>.
    /// </summary>
    public bool ModernMoveValues { get; private init; }

    /// <summary>
    /// Species and their forms have the types and base stats of the newest games (Clefairy is a Fairy type, Pikachu's
    /// Defense is 40) instead of Platinum's: <see cref="Models.PokemonSpecies.Modern"/>.
    /// </summary>
    public bool ModernSpeciesValues { get; private init; }

    // ---- Conditions and the field (plan 06 · R3). A count is of turn ends still to come unless it says otherwise.

    /// <summary>
    /// A sleep's counter starts at this plus one of <see cref="SleepLengths"/>. It drops by one each time the
    /// sleeper's turn comes, and the turn it reaches nothing the Pokémon wakes and moves.
    /// </summary>
    public int SleepCounterBase => 2;

    /// <summary>Weather brought by an ability (Drizzle, Sand Stream…) lasts this many turns; 0 is for as long as the battle.</summary>
    public int AbilityWeatherTurns { get; private init; }

    /// <summary>How long a Taunt lasts, from the shortest to the longest (each as likely).</summary>
    public (int Min, int Max) TauntTurns { get; private init; } = (3, 5);

    /// <summary>Encore's and Disable's counters: each lasts one turn end more than it says.</summary>
    public (int Min, int Max) EncoreTurns { get; private init; } = (3, 7);
    public (int Min, int Max) DisableTurns { get; private init; } = (3, 6);

    public int TailwindTurns { get; private init; } = 3;
    public (int Min, int Max) UproarTurns { get; private init; } = (3, 6);

    /// <summary>A binding move (Wrap, Fire Spin) holds for this many turn ends, the last of them the one that frees.</summary>
    public (int Min, int Max) BindTurns { get; private init; } = (3, 6);

    /// <summary>The same count when the binder holds a Grip Claw.</summary>
    public int GripClawBindTurns { get; private init; } = 6;

    /// <summary>Each turn end it takes one part in this many of the bound Pokémon's HP.</summary>
    public int BindDamageDivisor { get; private init; } = 16;

    /// <summary>
    /// Protect, Detect and Endure work when a number drawn from 65,536 is no greater than this, by how many times
    /// in a row they have worked already (the last entry for every time after).
    /// </summary>
    public IReadOnlyList<int> ProtectRates { get; private init; } = new[] { 0xFFFF, 0x7FFF, 0x3FFF, 0x1FFF };

    /// <summary>Stages of evasion Minimize gives.</summary>
    public int MinimizeStages { get; private init; } = 1;

    /// <summary>Powder and spore moves do nothing to Grass types (from Generation 6).</summary>
    public bool GrassTypesIgnorePowder { get; private init; }

    /// <summary>Ghost types can always switch out and run (from Generation 6).</summary>
    public bool GhostTypesCantBeTrapped { get; private init; }

    /// <summary>Toxic never misses when a Poison type uses it (from Generation 6).</summary>
    public bool PoisonTypesNeverMissToxic { get; private init; }

    private Ruleset(RulesPreset preset) => Preset = preset;

    public static Ruleset Platinum { get; } = new(RulesPreset.Platinum);

    public static Ruleset Modern { get; } = new(RulesPreset.Modern)
    {
        CriticalOdds = new[] { 24, 8, 2, 1, 1 },
        CriticalMultiplier = 1.5f,
        BurnDamageDivisor = 16,
        ParalysisSpeedDivisor = 2,
        ElectricTypesCantBeParalyzed = true,
        SleepLengths = 3,
        ConfusionSelfHitOdds = 3,
        SteelResistsGhostAndDark = false,
        ModernMoveValues = true,
        ModernSpeciesValues = true,
        AbilityWeatherTurns = 5,
        TauntTurns = (3, 3),
        EncoreTurns = (2, 2),
        DisableTurns = (4, 4),
        TailwindTurns = 4,
        UproarTurns = (3, 3),
        BindTurns = (5, 6),
        GripClawBindTurns = 8,
        BindDamageDivisor = 8,
        // A third as likely each time, down to 1 in 729
        ProtectRates = new[] { 0xFFFF, 21845, 7281, 2427, 809, 269, 89 },
        MinimizeStages = 2,
        GrassTypesIgnorePowder = true,
        GhostTypesCantBeTrapped = true,
        PoisonTypesNeverMissToxic = true
    };

    public static Ruleset Of(RulesPreset preset) => preset == RulesPreset.Modern ? Modern : Platinum;

    /// <summary>
    /// The rules of the game in progress: what a battle uses when it isn't handed rules of its own, and what the
    /// moves' and the species' values follow. Static, like <see cref="Core.PlayerIdentity"/>, and for the same reason tests must
    /// not change it (they run side by side): a test of the modern rules hands them to its battle
    /// (<see cref="Battle.BattleSetup.Rules"/>) and asks a move for its values under them
    /// (<see cref="Models.MoveData.Under"/>).
    /// </summary>
    public static Ruleset Current { get; private set; } = Platinum;

    /// <summary>
    /// Makes a preset the rules of the game in progress. The engine calls it once, as a new game begins or a save
    /// is loaded, before any of that game's Pokémon are made.
    /// </summary>
    public static void Use(RulesPreset preset)
    {
        Current = Of(preset);
        MoveDatabase.UseRules(Current);
        PokemonDatabase.UseRules(Current);
    }
}
