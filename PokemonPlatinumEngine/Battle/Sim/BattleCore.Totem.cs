using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// A totem's aura (plan 23 · Z6; <c>docs/mechanics/rulings.md</c>, "Totems and their aura"): the stat stages a trial's
/// totem takes as the battle opens (<see cref="Stats"/>, each by <see cref="Stages"/>), and, where it isn't its own line,
/// the species its first call for help brings (<see cref="Ally"/>). Plain values, so the Vs. Recorder's file keeps it.
/// </summary>
public sealed record TotemAura(IReadOnlyList<StatType> Stats, int Stages = 1, string? Ally = null)
{
    /// <summary>The words a script names an aura's stats by (<c>wildbattle "Gumshoos" 12 totem defense</c>): <c>all</c> is the five.</summary>
    public static readonly string[] StatWords = { "attack", "defense", "spattack", "spdefense", "speed", "accuracy", "evasion", "all" };

    /// <summary>The stats a word of <see cref="StatWords"/> stands for.</summary>
    public static IEnumerable<StatType> StatsOf(string word) => word switch
    {
        "attack" => new[] { StatType.Attack },
        "defense" => new[] { StatType.Defense },
        "spattack" => new[] { StatType.SpAttack },
        "spdefense" => new[] { StatType.SpDefense },
        "speed" => new[] { StatType.Speed },
        "accuracy" => new[] { StatType.Accuracy },
        "evasion" => new[] { StatType.Evasion },
        "all" => new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed },
        _ => Array.Empty<StatType>()
    };
}

/// <summary>
/// Totem battles (plan 23 · Z6; Sun and Moon's trials): a wild battle handed a <see cref="BattleConditions.TotemAura"/>.
/// The totem is the first wild Pokémon. As the battle opens it is wrapped in its aura (a line, a
/// <see cref="TotemAuraRaised"/> for the screen, and each stat raised through <see cref="ChangeStat(Battler, StatType, int, Battler?, bool)"/>,
/// so every rule that answers a stage's rise answers this one); fought as an SOS battle, its first action is a call for
/// help that is always answered, by its aura's <see cref="TotemAura.Ally"/> where it names one; nobody may throw a ball
/// at it or its allies; and the battle is won once the totem falls, whoever answered its calls still standing.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>The totem's aura, or null in any other battle.</summary>
    public TotemAura? Totem => Conditions.TotemAura;

    /// <summary>The totem as the rules have it, once its aura is raised.</summary>
    private Pokemon? totem;

    /// <summary>Whether the totem has made its first call, which is always answered.</summary>
    private bool totemCalled;

    /// <summary>What a ball thrown in a totem's battle meets: a line of our own.</summary>
    public static string TotemRefusal => Text.Of("battle.totemCantBeCaught");

    /// <summary>The totem has fallen: the battle is won.</summary>
    private bool TotemFell => totem is { IsFainted: true };

    /// <summary>The aura as the battle opens: a line for the totem, then each stat it raises by its stages.</summary>
    private void RaiseTheTotemsAura()
    {
        if (Totem is not { } aura || IsTrainerBattle) return;
        var first = EnemySlots.FirstOrDefault(b => b.IsActive);
        if (first?.Pokemon == null) return;
        totem = first.Pokemon;
        Say(Text.Of("battle.totemAura", ("pokemon", first.Name))).With(new TotemAuraRaised(first.Place));
        foreach (var stat in aura.Stats.Distinct()) ChangeStat(first, stat, aura.Stages, null, false, By.Other);
    }

    /// <summary>Whether the caller is the totem making its first call, which is made and answered without a roll.</summary>
    private bool TotemsFirstCall(Battler caller) => Totem != null && !totemCalled && caller.Pokemon != null && ReferenceEquals(caller.Pokemon, totem);

    /// <summary>The ally a totem's first call brings where its aura names one; null for its own line, as any caller's.</summary>
    private PokemonSpecies? TotemsAlly() => Totem?.Ally is { } name ? PokemonDatabase.Get(name) : null;
}
