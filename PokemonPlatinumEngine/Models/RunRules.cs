using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>How hard the game is on the trainers (plan 13 · V2 and V3 give each its numbers; V1 only records the choice).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Difficulty { Easy, Normal, Challenge }

/// <summary>
/// The ways to play (plan 13 · V1): choices made once, at NEW GAME, that change how a run is played and never move
/// afterwards. Normal, with nothing on, is Platinum's game exactly: <see cref="IsNormal"/>. The record is plain data,
/// kept whole in <c>SaveData.Run</c> (null in a save from before it, which reads as Normal) and handed to whatever
/// enforces a rule (plan 13 · V2 to V9); V1 enforces none of them, it only asks and remembers.
/// </summary>
public sealed record RunRules
{
    public Difficulty Difficulty { get; init; } = Difficulty.Normal;

    /// <summary>EXP stops at the next leader's level by badge (V3).</summary>
    public bool LevelCap { get; init; }

    /// <summary>Trainers are levelled round the lead's level (V3).</summary>
    public bool ScaleTrainers { get; init; }

    /// <summary>A Pokémon that faints is dead, one wild Pokémon is caught in each area (V4).</summary>
    public bool Nuzlocke { get; init; }

    /// <summary>With a Nuzlocke: a species already caught, or evolved from one, may be passed over without using the area's chance.</summary>
    public bool DupesClause { get; init; }

    /// <summary>With a Nuzlocke: a shiny may be caught whatever the area's chance.</summary>
    public bool ShinyClause { get; init; }

    /// <summary>With a Nuzlocke: a gift counts as the area's catch.</summary>
    public bool GiftsCount { get; init; }

    /// <summary>A type every Pokémon of the run must have (V4); null for none.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PokemonType? Monotype { get; init; }

    /// <summary>How many Pokémon the party holds: 6, or fewer (1 for a solo run).</summary>
    public int PartyLimit { get; init; } = Party.MaxSize;

    public bool NoBattleItems { get; init; }
    public bool NoCenter { get; init; }
    public bool NoCatching { get; init; }
    public bool NoRunning { get; init; }

    /// <summary>Battle Style is Set and cannot be changed (needs plan 08 · P1).</summary>
    public bool SetStyle { get; init; }

    /// <summary>
    /// The shiny odds as 1 in this many: 0 for the rules' own (what Normal keeps, so nothing moves), else 4096, 1024 or 512
    /// (V5 reads it).
    /// </summary>
    public int ShinyDenominator { get; init; }

    public static RunRules Normal { get; } = new();

    /// <summary>The usual Nuzlocke: the two common clauses, nothing else.</summary>
    public static RunRules NuzlockePreset { get; } = new() { Nuzlocke = true, DupesClause = true, ShinyClause = true };

    /// <summary>A Nuzlocke with everything that makes a run hard: the Challenge trainers, the level cap, no items in battle, Set.</summary>
    public static RunRules Hardcore { get; } = NuzlockePreset with
    {
        Difficulty = Difficulty.Challenge, LevelCap = true, NoBattleItems = true, SetStyle = true
    };

    /// <summary>The presets the panel's first row offers, in its order; a run that matches none is Custom.</summary>
    public static IReadOnlyList<(string Key, RunRules Rules)> Presets { get; } = new[]
    {
        ("normal", Normal), ("nuzlocke", NuzlockePreset), ("hardcore", Hardcore)
    };

    /// <summary>Nothing is on: this is Platinum's game as it always was.</summary>
    public bool IsNormal => this == Normal;

    /// <summary>The key of the preset this run is, or "custom".</summary>
    public string PresetKey
    {
        get
        {
            foreach (var (key, rules) in Presets)
                if (this == rules) return key;
            return "custom";
        }
    }

    /// <summary>
    /// The shiny odds as 1 in this many (plan 13 · V5): the run's own when it chose them, else the rules' (8,192 by
    /// Platinum's, 4,096 by the modern ones), so a run with nothing chosen rolls exactly as the game always did.
    /// </summary>
    public int ShinyOdds(int rulesOdds) => ShinyDenominator > 0 ? ShinyDenominator : rulesOdds;

    /// <summary>The rules a script can ask after with <c>if rule …</c> (plan 13 · V5), by the word it uses.</summary>
    public static readonly string[] RuleNames =
    {
        "nuzlocke", "monotype", "solo", "noitems", "nocenter", "nocatching", "norunning", "set", "shinyodds", "challenge", "easy", "levelcap"
    };

    /// <summary>Whether the run has a rule on, by its script word (<see cref="RuleNames"/>); false for a word it doesn't know.</summary>
    public bool Has(string rule) => rule.ToLowerInvariant() switch
    {
        "nuzlocke" => Nuzlocke,
        "monotype" => Monotype != null,
        "solo" => PartyLimit < Party.MaxSize,
        "noitems" => NoBattleItems,
        "nocenter" => NoCenter,
        "nocatching" => NoCatching,
        "norunning" => NoRunning,
        "set" => SetStyle,
        "shinyodds" => ShinyDenominator > 0,
        "challenge" => Difficulty == Difficulty.Challenge,
        "easy" => Difficulty == Difficulty.Easy,
        "levelcap" => LevelCap,
        _ => false
    };

    /// <summary>
    /// Whether a Pokémon may be carried in the team under the run's Monotype (plan 13 · V5): one of the type, or of the
    /// line of the player's starter, which is exempt (it is given before any type could be chosen for). Anything under
    /// no Monotype.
    /// </summary>
    public bool MayCarry(Pokemon pokemon, string? starter)
    {
        if (Monotype is not { } type || pokemon.HasType(type)) return true;
        return starter != null && PokemonDatabase.Get(starter) is { } start && Models.Nuzlocke.LineOf(pokemon.Species) == Models.Nuzlocke.LineOf(start);
    }

    /// <summary>What a battle is told of the run's restrictions (the core's <c>Restrictions</c>): null when none touches a battle, which changes nothing.</summary>
    public RunTerms? BattleTerms() =>
        NoCatching || NoRunning || Monotype != null || NoBattleItems ? new RunTerms(NoCatching, NoRunning, Monotype, NoBattleItems) : null;

    /// <summary>The ShinyDenominator choices, in the order the panel steps through them.</summary>
    public static readonly int[] ShinyChoices = { 0, 4096, 1024, 512 };

    /// <summary>Which party limit a Pokémon count is held to: 1 to 6.</summary>
    public static int ClampLimit(int n) => Math.Clamp(n, 1, Party.MaxSize);

    /// <summary>
    /// The run in force: set by the engine as a game begins or loads (as <c>Ruleset.Use</c> is), read by the one rule that
    /// has no caller to hand it to (V5's shiny roll). Tests never set it.
    /// </summary>
    public static RunRules Current { get; set; } = Normal;
}

/// <summary>
/// What a battle is told of the run's restrictions (plan 13 · V5; <see cref="Battle.Sim.CoreSetup.Restrictions"/>): no ball
/// at all, no running, a type every Pokémon caught must have, and no item from the bag but a ball. A battle refuses these
/// for the player's side only; null (a run with none of them) changes nothing. A recorded battle doesn't keep them: they
/// only refuse choices.
/// </summary>
public sealed record RunTerms(bool NoCatching = false, bool NoRunning = false, PokemonType? Monotype = null, bool NoItems = false);
