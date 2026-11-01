using System.Collections.Generic;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a receipt is the receipt of (plan 12 · Q28, Q29): a move of six into a park, a trade, or a move to or from the Home box.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReceiptKind
{
    /// <summary>Plan 08 · P22's move of six into another slot's park (<see cref="SlotMoves"/>).</summary>
    Move,
    /// <summary>A trade between two slots (plan 12 · Q28, <see cref="HomeMoves"/>): <see cref="MoveReceipt.Pokemon"/> go from <see cref="MoveReceipt.From"/> to <see cref="MoveReceipt.To"/>, <see cref="MoveReceipt.Back"/> the other way.</summary>
    Trade,
    /// <summary>A move to or from the Home box (plan 12 · Q29): slot 0 (<see cref="HomeMoves.HomeSide"/>) is the Home box.</summary>
    Home
}

/// <summary>
/// The receipt of a move of Pokémon from one of this game's saves to another (plan 08 · P22, <see cref="SlotMoves"/>):
/// kept in both saves from the moment the first is written until the move is over, it names the move, the two slots, the
/// source's checksum as it was before and the Pokémon themselves, whole, so that a move cut short at any step can be
/// finished, and one whose other save is gone undone, with each Pokémon in exactly one save. A trade between two saves
/// and a move to or from the Home box (plan 12 · Q28, Q29) keep the same receipts, told apart by <see cref="Kind"/>.
/// </summary>
public sealed class MoveReceipt
{
    /// <summary>What names the move: a hash of the Pokémon's ids and the slots, so the same move has the same receipt in both saves.</summary>
    public string Id { get; set; } = "";

    /// <summary>What it is the receipt of; a move of six (P22's) where a receipt doesn't say.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ReceiptKind Kind { get; set; }

    /// <summary>The slot the Pokémon leave and the slot they go to.</summary>
    public int From { get; set; }
    public int To { get; set; }

    /// <summary>The checksum the source's file had before the move (<see cref="SaveManager.ChecksumOfFile"/>), for the record.</summary>
    public string? SourceChecksum { get; set; }

    /// <summary>The Pokémon moved, as they were in the source.</summary>
    public List<SavedPokemonData> Pokemon { get; set; } = new();

    /// <summary>A trade's other half: the Pokémon that go from <see cref="To"/> to <see cref="From"/>, as they were there. Null for a move.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SavedPokemonData>? Back { get; set; }
}
