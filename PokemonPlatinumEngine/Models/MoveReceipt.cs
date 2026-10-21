using System.Collections.Generic;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The receipt of a move of Pokémon from one of this game's saves to another (plan 08 · P22, <see cref="SlotMoves"/>):
/// kept in both saves from the moment the first is written until the move is over, it names the move, the two slots, the
/// source's checksum as it was before and the Pokémon themselves, whole, so that a move cut short at any step can be
/// finished, and one whose other save is gone undone, with each Pokémon in exactly one save.
/// </summary>
public sealed class MoveReceipt
{
    /// <summary>What names the move: a hash of the Pokémon's ids and the slots, so the same move has the same receipt in both saves.</summary>
    public string Id { get; set; } = "";

    /// <summary>The slot the Pokémon leave and the slot they go to.</summary>
    public int From { get; set; }
    public int To { get; set; }

    /// <summary>The checksum the source's file had before the move (<see cref="SaveManager.ChecksumOfFile"/>), for the record.</summary>
    public string? SourceChecksum { get; set; }

    /// <summary>The Pokémon moved, as they were in the source.</summary>
    public List<SavedPokemonData> Pokemon { get; set; } = new();
}
