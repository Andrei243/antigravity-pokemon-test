using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>The level-up panel's two pages, in the order they are shown.</summary>
public enum LevelUpPage
{
    /// <summary>What the level added to each stat (+n).</summary>
    Gains,
    /// <summary>The six stats as they now stand.</summary>
    Stats
}

/// <summary>
/// The level-up panel (plan 08 · P5; style guide, "Battle panels", "A level"): a Pokémon's six stats before a level
/// and after it, shown as two pages, the gains and then the new values, each waiting for a press. It is the
/// original's level-up summary (<c>SEQ_GET_EXP_LEVEL_UP_SUMMARY_PRINT_DIFF</c> to <c>_PRINT_TRUE_WAIT</c> of
/// <c>BattleScript_GetExpTask</c>), which follows the line that tells of the level and comes before the moves the
/// level brings. A battle shows one over its message box (<c>BattleHUD.LevelUp</c>) and the bag one over the team's
/// cards for a Rare Candy (<c>BagScreen.LevelUp</c>); both are painted by <c>ModernUi.LevelUp</c>.
/// <para>
/// It holds numbers, never reads them off the Pokémon as it is drawn: in a battle the rules are a turn ahead of the
/// screen, and the two spreads are the log's own (<see cref="Battle.Sim.LevelRose"/>). No drawing and no keys of its
/// own: whoever shows it calls <see cref="Press"/> for the button and <see cref="Advance"/> for the clock.
/// </para>
/// </summary>
public sealed class LevelUpPanel
{
    /// <summary>How long the panel takes to slide into place.</summary>
    public const float AppearTime = 0.18f;

    /// <summary>
    /// The six stats in the order the panel lists them, which is the original's (its <c>statParams</c>: maximum HP,
    /// Attack, Defense, Sp. Atk, Sp. Def, Speed), not the order effort is counted in.
    /// </summary>
    public static readonly IReadOnlyList<StatType> Order = new[]
    {
        StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed
    };

    public LevelUpPanel(Pokemon pokemon, int level, StatSpread before, StatSpread after)
    {
        Pokemon = pokemon;
        Level = level;
        Before = before;
        After = after;
    }

    /// <summary>Whose level it is: its name, gender and icon head the panel.</summary>
    public Pokemon Pokemon { get; }

    /// <summary>The level it reached.</summary>
    public int Level { get; }

    /// <summary>Its stats before the level, and at it.</summary>
    public StatSpread Before { get; }
    public StatSpread After { get; }

    /// <summary>The page on show.</summary>
    public LevelUpPage Page { get; private set; } = LevelUpPage.Gains;

    /// <summary>True once both pages have been read: whoever shows the panel takes it down and goes on.</summary>
    public bool Closed { get; private set; }

    /// <summary>Seconds since it opened, for its slide into place.</summary>
    public float Age { get; private set; }

    /// <summary>0 as it opens to 1 once it stands in place.</summary>
    public float Appear => Math.Clamp(Age / AppearTime, 0f, 1f);

    /// <summary>What the level added to a stat: the first page's number.</summary>
    public int Gain(StatType stat) => After.Of(stat) - Before.Of(stat);

    /// <summary>The stat as it now stands: the second page's number.</summary>
    public int Value(StatType stat) => After.Of(stat);

    /// <summary>
    /// The press it waits for (the original's A, B, X, Y or a touch): from the gains to the new values, and from
    /// those it closes. True once it has closed.
    /// </summary>
    public bool Press()
    {
        if (Page == LevelUpPage.Gains) Page = LevelUpPage.Stats;
        else Closed = true;
        return Closed;
    }

    public void Advance(float dt) => Age += dt;
}
