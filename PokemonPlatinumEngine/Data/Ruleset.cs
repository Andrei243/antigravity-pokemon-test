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

    /// <summary>What paralysis multiplies Speed by.</summary>
    public float ParalysisSpeed { get; private init; } = 0.25f;

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

    private Ruleset(RulesPreset preset) => Preset = preset;

    public static Ruleset Platinum { get; } = new(RulesPreset.Platinum);

    public static Ruleset Modern { get; } = new(RulesPreset.Modern)
    {
        CriticalOdds = new[] { 24, 8, 2, 1, 1 },
        CriticalMultiplier = 1.5f,
        BurnDamageDivisor = 16,
        ParalysisSpeed = 0.5f,
        ElectricTypesCantBeParalyzed = true,
        SleepLengths = 3,
        ConfusionSelfHitOdds = 3,
        SteelResistsGhostAndDark = false,
        ModernMoveValues = true
    };

    public static Ruleset Of(RulesPreset preset) => preset == RulesPreset.Modern ? Modern : Platinum;

    /// <summary>
    /// The rules of the game in progress: what a battle uses when it isn't handed rules of its own, and what the
    /// moves' values follow. Static, like <see cref="Core.PlayerIdentity"/>, and for the same reason tests must
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
    }
}
