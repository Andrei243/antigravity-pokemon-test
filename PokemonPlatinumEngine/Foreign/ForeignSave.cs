using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Foreign;

/// <summary>What kind of file a foreign file is (<see cref="ForeignFile.Open"/>).</summary>
public enum ForeignKind
{
    GbaSave, PlatinumSave, GbaPokemon, DsPokemon,
    /// <summary>A folder of loose Pokémon files taken as one source (<c>Models.Migration.Combine</c>, plan 08 · P21).</summary>
    Files
}

/// <summary>Why a foreign file was refused whole: nothing of it was kept.</summary>
public enum ForeignRefusal
{
    None,
    /// <summary>The file's size is none a save or a Pokémon file has.</summary>
    UnknownSize,
    /// <summary>The size is a save's, and no slot of it is whole: a sector's checksum, signature or number fails.</summary>
    DamagedSave,
    /// <summary>A Pokémon file whose checksum is neither a plain file's nor an encrypted one's.</summary>
    DamagedPokemon,
    /// <summary>The file could not be read.</summary>
    Unreadable
}

/// <summary>One box of a source: its name and its thirty slots, a slot with no Pokémon null.</summary>
public sealed class ForeignBox
{
    public required string Name { get; init; }
    public required IReadOnlyList<ForeignPokemon?> Slots { get; init; }
    public int Count => Slots.Count(s => s != null);
}

/// <summary>
/// What a foreign file gave (plan 08 · P20): the trainer's name and ID where the file has them, and the boxes, which a
/// save gives as it keeps them and a Pokémon file as one box of one. Plain data: nothing in it points back at the file.
/// </summary>
public sealed class ForeignSave
{
    public required ForeignKind Kind { get; init; }
    public GbaGame Game { get; init; } = GbaGame.Unknown;
    public string TrainerName { get; init; } = "";

    /// <summary>The trainer's ID and secret ID, the ID the low half: what a source is remembered by for the day a batch must wait.</summary>
    public uint TrainerId { get; init; }
    public required IReadOnlyList<ForeignBox> Boxes { get; init; }

    /// <summary>Every Pokémon of every box, in order.</summary>
    public IEnumerable<ForeignPokemon> All => Boxes.SelectMany(b => b.Slots).OfType<ForeignPokemon>();

    public int Count => Boxes.Sum(b => b.Count);
}
