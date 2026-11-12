using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>
/// A Pokémon on a delve's floor (plan 44 · SD7): the run's own copy, never the player's Pokémon, where it stands, which
/// way it faces and what the floor alone keeps of it (asleep until the team comes near, confusion, a flinch). No drawing
/// or input.
/// </summary>
public sealed class DelveMon
{
    public DelveMon(Pokemon pokemon, bool onTeam, int order)
    {
        Pokemon = pokemon;
        OnTeam = onTeam;
        Order = order;
    }

    public Pokemon Pokemon { get; }

    /// <summary>The team's (the leader, the partner, the recruits) or the floor's own.</summary>
    public bool OnTeam { get; internal set; }

    /// <summary>A number that stays the same for the whole run, which orders ties before the run's draw breaks them.</summary>
    public int Order { get; }

    public int X { get; internal set; }
    public int Y { get; internal set; }
    public Direction Facing { get; internal set; } = Direction.Down;

    /// <summary>A resident that sleeps until the team comes within <see cref="DelveTurns.WakeReach"/> or strikes it.</summary>
    public bool Dormant { get; internal set; }

    /// <summary>A resident that flees once its HP is under a quarter.</summary>
    public bool Shy { get; internal set; }

    /// <summary>A recruit, as against the leader and the partner the player chose.</summary>
    public bool Recruit { get; internal set; }

    /// <summary>The hollow's guardian, which keeps the last stairs and never asks to join (SD9).</summary>
    public bool IsGuardian { get; internal set; }

    public int ConfusedTurns { get; internal set; }
    public bool Flinched { get; internal set; }

    public bool Fainted => Pokemon.CurrentHP <= 0;
    public bool Swims => Pokemon.HasType(PokemonType.Water);
    public string Name => Pokemon.Species.Name;

    /// <summary>Its Speed as the turn's order reads it: its stage, and halved while paralysed.</summary>
    public int EffectiveSpeed
    {
        get
        {
            int speed = Battle.Sim.Formulas.Staged(Pokemon.Speed, Pokemon.StatStages.GetValueOrDefault(StatType.Speed));
            return Pokemon.Status == StatusCondition.Paralyze ? speed / 2 : speed;
        }
    }

    /// <summary>Its distance from a tile in steps of eight ways.</summary>
    public int Reach(int x, int y) => Math.Max(Math.Abs(X - x), Math.Abs(Y - y));
}
