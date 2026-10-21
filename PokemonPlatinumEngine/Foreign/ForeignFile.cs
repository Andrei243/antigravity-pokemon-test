using System;
using System.IO;

namespace PokemonPlatinumEngine.Foreign;

/// <summary>
/// The one door to a player's own files (plan 08 · P20): a file is told by its size, opened for reading only, and read by the
/// reader of what it is, or refused whole. Nothing of it is written, renamed, moved or copied; a file read is the same
/// bytes afterwards, and what comes back is plain data (<see cref="ForeignSave"/>) that points back at nothing.
/// </summary>
public static class ForeignFile
{
    /// <summary>The sizes a Pokémon file has: the GBA's box and party Pokémon, the DS games' box and party Pokémon.</summary>
    public static bool IsSizeOfAPokemon(int length) => length is GbaPokemon.BoxSize or GbaPokemon.PartySize or DsPokemon.BoxSize or DsPokemon.PartySize;

    /// <summary>The largest file any reader takes: a Platinum save's 512 KiB.</summary>
    public const int Largest = PlatinumSave.FileSize;

    /// <summary>Opens a file by its path. Its size is asked before a byte is read, so a file of no known size is never loaded.</summary>
    public static (ForeignSave? Save, ForeignRefusal Refusal) Open(string path)
    {
        try
        {
            long length = new FileInfo(path).Length;
            if (length > Largest) return (null, ForeignRefusal.UnknownSize);
            if (!GbaSave.IsSizeOfASave((int)length) && !PlatinumSave.IsSizeOfASave((int)length) && !IsSizeOfAPokemon((int)length))
                return (null, ForeignRefusal.UnknownSize);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var bytes = new byte[length];
            stream.ReadExactly(bytes);
            return Read(bytes);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return (null, ForeignRefusal.Unreadable);
        }
    }

    /// <summary>Reads the bytes of a file already in memory, by their size.</summary>
    public static (ForeignSave? Save, ForeignRefusal Refusal) Read(ReadOnlySpan<byte> file)
    {
        if (GbaSave.IsSizeOfASave(file.Length)) return GbaSave.Read(file);
        if (PlatinumSave.IsSizeOfASave(file.Length)) return PlatinumSave.Read(file);
        if (file.Length is GbaPokemon.BoxSize or GbaPokemon.PartySize) return Single(GbaFile(file), ForeignKind.GbaPokemon);
        if (file.Length is DsPokemon.BoxSize or DsPokemon.PartySize) return Single(DsFile(file), ForeignKind.DsPokemon);
        return (null, ForeignRefusal.UnknownSize);
    }

    // A plain file (.pk3, .pk4) is told from an encrypted one (.ek3, .ek4) by its checksum, which only one of the two ways of reading it can satisfy
    private static ForeignPokemon? GbaFile(ReadOnlySpan<byte> file) =>
        GbaPokemon.IsPlain(file) ? GbaPokemon.ReadPlain(file, GbaGame.Unknown) : GbaPokemon.ReadEncrypted(file, GbaGame.Unknown);

    private static ForeignPokemon? DsFile(ReadOnlySpan<byte> file) =>
        DsPokemon.IsPlain(file) ? DsPokemon.ReadPlain(file) : DsPokemon.ReadEncrypted(file);

    private static (ForeignSave? Save, ForeignRefusal Refusal) Single(ForeignPokemon? pokemon, ForeignKind kind)
    {
        // A file that holds no Pokémon, or one whose checksum fits neither reading, is no file of one
        if (pokemon == null || pokemon.IsBadEgg) return (null, ForeignRefusal.DamagedPokemon);
        return (new ForeignSave
        {
            Kind = kind,
            TrainerName = pokemon.OtName,
            TrainerId = pokemon.OtId,
            Boxes = new[] { new ForeignBox { Name = "FILE", Slots = new[] { pokemon } } }
        }, ForeignRefusal.None);
    }
}
