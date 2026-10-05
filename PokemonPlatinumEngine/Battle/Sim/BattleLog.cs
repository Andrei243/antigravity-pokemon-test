using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>A place on the field: a side and one of its slots.</summary>
public readonly record struct Place(BattleSide Side, int Slot)
{
    /// <summary>The place's number as the original counts them: the player's first, the foe's first, then the second of each.</summary>
    public int Number => Slot * 2 + (Side == BattleSide.Enemy ? 1 : 0);
}

/// <summary>
/// One thing a battle's log says happened (plan 06 · R2). The core works a whole turn out at once and writes what
/// happened here, in order; whoever shows the battle plays the log back at its own pace. It is the same split the
/// original has between its battle logic and the "controller" it sends messages to, and what lets a battle run
/// with no screen at all (tests, a server, a replay).
/// <para>
/// Pokémon in events are the core's own (<see cref="BattleCore"/> works on copies); a place is where on the field.
/// </para>
/// </summary>
public abstract record BattleEvent;

/// <summary>
/// A line of text. Whatever is attached happens as the line appears (<see cref="Shows"/>) or at the moment a move
/// lands (<see cref="OnImpact"/>); an event that stands in the log by itself happens once the line before it has
/// been read.
/// </summary>
public sealed record Said(string Text) : BattleEvent
{
    public List<BattleEvent> Shows { get; } = new();
    public List<BattleEvent> OnImpact { get; } = new();

    public Said With(BattleEvent happening)
    {
        Shows.Add(happening);
        return this;
    }

    public Said AtImpact(BattleEvent happening)
    {
        OnImpact.Add(happening);
        return this;
    }
}

/// <summary>The trainers standing across the field as the battle opens.</summary>
public sealed record TrainersStand(string First, string? Second) : BattleEvent;

/// <summary>A species has been met (it goes in the Pokédex as seen).</summary>
public sealed record Seen(PokemonSpecies Species) : BattleEvent;

/// <summary>A Pokémon takes a place: thrown out of a ball, or already there when a wild battle opens.</summary>
public sealed record Entered(Place Place, Pokemon Pokemon, bool FromBall) : BattleEvent;

/// <summary>A Pokémon is called back (the animation); <see cref="Left"/> is when it is gone.</summary>
public sealed record Recalled(Place Place) : BattleEvent;
public sealed record Left(Place Place) : BattleEvent;

/// <summary>A Pokémon starts its move.</summary>
public sealed record Lunged(Place Place, MoveCategory Category) : BattleEvent;

/// <summary>A move's effect travels from its user to one target, and how it came out there.</summary>
public sealed record MoveShown(string Move, PokemonType Type, MoveCategory Category, Place From, Place To,
    bool Missed, bool Blocked, bool Critical, bool SuperEffective) : BattleEvent;

/// <summary>A move's damage lands: the HP its target is left with.</summary>
public sealed record Struck(Place Place, int Hp, bool Hard) : BattleEvent;

/// <summary>The sound of the hits of one move landing.</summary>
public sealed record HitSounded(bool SuperEffective) : BattleEvent;

/// <summary>HP restored, or lost to something that isn't a move's hit (poison, recoil): the HP it is left with.</summary>
public sealed record HpChanged(Place Place, int Hp, bool Healed) : BattleEvent;

public sealed record StageChanged(Place Place, StatType Stat, int Stage, bool Rose) : BattleEvent;

/// <summary>A status condition given, or cured (<see cref="StatusCondition.None"/>).</summary>
public sealed record StatusChanged(Place Place, StatusCondition Status) : BattleEvent;

public sealed record Fainted(Place Place) : BattleEvent;

/// <summary>A Pokémon goes where it can't be seen (into the air, under the ground), and comes back.</summary>
public sealed record Vanished(Place Place) : BattleEvent;
public sealed record Reappeared(Place Place) : BattleEvent;

/// <summary>A Pokémon has taken another's shape (Transform): the screen shows it as the rules now have it.</summary>
public sealed record Reshaped(Place Place) : BattleEvent;

/// <summary>A Substitute is put up in a place, or is gone from it.</summary>
public sealed record SubstituteChanged(Place Place, bool Up) : BattleEvent;

/// <summary>The weather over the battle is now this (none: it cleared).</summary>
public sealed record WeatherChanged(BattleWeather Weather) : BattleEvent;
public sealed record ExpGained(Pokemon Pokemon, int Amount) : BattleEvent;
public sealed record LevelRose(Pokemon Pokemon, int Level) : BattleEvent;

/// <summary>A ball is thrown at the foe in a slot and shakes this many times (4: it holds).</summary>
public sealed record BallThrown(string Ball, int Slot, int Shakes) : BattleEvent;

/// <summary>The wild Pokémon is the player's: in the party, or sent to the PC.</summary>
public sealed record Caught(Pokemon Pokemon, string Ball, bool ToBox) : BattleEvent;

/// <summary>The other side has nobody left (the victory music starts before the last lines are read).</summary>
public sealed record Won : BattleEvent;

public sealed record Ended(BattleResult Result) : BattleEvent;
