using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// One place on the field (a side and a slot: one per side in a single battle, two in a double) and the
/// Pokémon standing there, with the conditions that last only while it stays in (confusion, flinching, a
/// Choice item's lock). Stat stages live on <see cref="Pokemon"/> and are cleared when it leaves.
/// </summary>
public sealed class Battler
{
    public BattleSide Side { get; }
    public int Slot { get; }

    /// <summary>The Pokémon in this place; it stays here after fainting until a replacement comes in (null = never filled).</summary>
    public Pokemon? Pokemon { get; internal set; }

    /// <summary>The party that sends Pokémon to this place (null for a wild Pokémon).</summary>
    internal Party? Roster { get; set; }

    /// <summary>The trainer standing behind this place, for "sent out" messages (null for the player and wild Pokémon).</summary>
    internal Trainer? Trainer { get; set; }

    internal Battler(BattleSide side, int slot)
    {
        Side = side;
        Slot = slot;
    }

    public bool IsPlayerSide => Side == BattleSide.Player;

    /// <summary>A Pokémon is standing here and can still fight.</summary>
    public bool IsActive => Pokemon != null && !Pokemon.IsFainted;

    /// <summary>How the battle's messages name it: "Turtwig" for the player's, "Foe Starly" for the other side's.</summary>
    public string Name => Pokemon == null ? "" : IsPlayerSide ? Pokemon.DisplayName : $"Foe {Pokemon.DisplayName}";

    // ---- Conditions that end when the Pokémon leaves the field

    public int ConfusionTurns { get; internal set; }
    public bool IsConfused => ConfusionTurns > 0;
    internal bool Flinched;

    /// <summary>The move a Choice item holds it to, until it leaves the field.</summary>
    public Move? ChoiceLock { get; internal set; }

    /// <summary>Flash Fire has absorbed a Fire move: its own Fire moves are stronger.</summary>
    internal bool FlashFire;

    /// <summary>It has already moved this turn (Flinch only works on a Pokémon that hasn't).</summary>
    internal bool MovedThisTurn;

    /// <summary>It was hit this turn (by a critical hit, for Anger Point; by anything, for Revenge-style checks later).</summary>
    internal bool TookCriticalHit;

    /// <summary>The Pokémon of the other side that saw this one on the field, for sharing EXP.</summary>
    internal readonly HashSet<Pokemon> FoughtAgainst = new();

    /// <summary>Clears everything that only lasts while on the field (on switching out or in).</summary>
    internal void ClearVolatile()
    {
        ConfusionTurns = 0;
        Flinched = false;
        ChoiceLock = null;
        FlashFire = false;
        MovedThisTurn = false;
        TookCriticalHit = false;
    }

    /// <summary>The types the Pokémon has right now.</summary>
    public bool HasType(PokemonType type) =>
        Pokemon != null && (Pokemon.Species.PrimaryType == type || Pokemon.Species.SecondaryType == type);

    public override string ToString() => $"{Side} {Slot}: {Pokemon?.DisplayName ?? "empty"}";
}
