using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Pokémon's own ids (plan 07 · O2; <see cref="Pokemon.Id"/>). An id is a GUID made of the number of the trainer
/// who first named it (the card's number and its hidden half, <c>GameEngine.TrainerNumber</c>) and a count the save keeps
/// (<c>SaveData.PokemonIdsIssued</c>), in the layout of RFC 9562's version 8: <c>nnnnnnnn-504b-8001-8000-0000ssssssss</c>.
/// Nothing is drawn for it, so giving one moves no seeded draw, no battle and no shot, and the same game gives the same
/// ids every time. Two players' ids part by their numbers; one player's by the count. GPU-free.
/// </summary>
public static class PokemonIds
{
    /// <summary>The id of a trainer's <paramref name="serial"/>th Pokémon.</summary>
    public static Guid Make(uint trainer, uint serial) => new(
        unchecked((int)trainer), 0x504B, unchecked((short)0x8001),
        0x80, 0x00, 0x00, 0x00,
        (byte)(serial >> 24), (byte)(serial >> 16), (byte)(serial >> 8), (byte)serial);

    /// <summary>The trainer an id was made for, and its count; null for an id this game didn't make (or none).</summary>
    public static (uint Trainer, uint Serial)? Parts(Guid id)
    {
        Span<byte> b = stackalloc byte[16];
        id.TryWriteBytes(b);
        // Guid's own layout: the first three fields little-endian, the last eight bytes as they stand
        if (b[4] != 0x4B || b[5] != 0x50 || b[6] != 0x01 || b[7] != 0x80 || b[8] != 0x80 || b[9] != 0 || b[10] != 0 || b[11] != 0) return null;
        uint trainer = (uint)(b[0] | b[1] << 8 | b[2] << 16 | b[3] << 24);
        uint serial = (uint)(b[12] << 24 | b[13] << 16 | b[14] << 8 | b[15]);
        return (trainer, serial);
    }

    /// <summary>
    /// Gives every Pokémon of <paramref name="owned"/> that has none an id, and a new one to each that has the id of
    /// one before it in the list (a copy that should never have been made, a save edited by hand): the first to hold an
    /// id keeps it. The ids are <paramref name="trainer"/>'s, counted on from <paramref name="next"/>, passing over any
    /// already held; <paramref name="next"/> comes back past the last given. Returns how many were given.
    /// </summary>
    public static int Give<T>(IEnumerable<T> owned, Func<T, Guid> get, Action<T, Guid> set, uint trainer, ref uint next)
    {
        var all = new List<T>(owned);
        var held = new HashSet<Guid>();
        var wanting = new List<T>();
        foreach (var p in all)
        {
            var id = get(p);
            if (id == Guid.Empty || !held.Add(id)) wanting.Add(p);
        }
        foreach (var p in wanting)
        {
            Guid id;
            do id = Make(trainer, next++);
            while (!held.Add(id));
            set(p, id);
        }
        return wanting.Count;
    }

    /// <summary><see cref="Give{T}"/> for the game's own Pokémon.</summary>
    public static int Give(IEnumerable<Pokemon> owned, uint trainer, ref uint next) =>
        Give(owned, p => p.Id, (p, id) => p.Id = id, trainer, ref next);
}
