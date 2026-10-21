using System;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Foreign.ForeignBytes;

namespace PokemonPlatinumEngine.Foreign;

/// <summary>
/// A Pokémon of the GBA's games (plan 08 · P20), 80 bytes in a box, from <c>src/main_menu/gba_pokemon.c</c>: a header
/// of the personality, the trainer's ID, the nickname, the language, three flags, the trainer's name, the markings and
/// a checksum, then 48 bytes of four substructures in an order the personality chooses, each word of them xored with
/// the trainer's ID and the personality. A party Pokémon's 100 bytes add stats that nothing here reads.
/// </summary>
public static class GbaPokemon
{
    public const int BoxSize = 80, PartySize = 100;

    // For a personality's remainder of 24: where the growth, moves, effort and miscellaneous substructures lie (the SUBSTRUCT_CASE rows)
    private static readonly byte[][] Orders =
    {
        new byte[] { 0, 1, 2, 3 }, new byte[] { 0, 1, 3, 2 }, new byte[] { 0, 2, 1, 3 }, new byte[] { 0, 3, 1, 2 },
        new byte[] { 0, 2, 3, 1 }, new byte[] { 0, 3, 2, 1 }, new byte[] { 1, 0, 2, 3 }, new byte[] { 1, 0, 3, 2 },
        new byte[] { 2, 0, 1, 3 }, new byte[] { 3, 0, 1, 2 }, new byte[] { 2, 0, 3, 1 }, new byte[] { 3, 0, 2, 1 },
        new byte[] { 1, 2, 0, 3 }, new byte[] { 1, 3, 0, 2 }, new byte[] { 2, 1, 0, 3 }, new byte[] { 3, 1, 0, 2 },
        new byte[] { 2, 3, 0, 1 }, new byte[] { 3, 2, 0, 1 }, new byte[] { 1, 2, 3, 0 }, new byte[] { 1, 3, 2, 0 },
        new byte[] { 2, 1, 3, 0 }, new byte[] { 3, 1, 2, 0 }, new byte[] { 2, 3, 1, 0 }, new byte[] { 3, 2, 1, 0 }
    };

    /// <summary>The place in the 48 bytes of a personality's substructure of a kind (0 growth, 1 moves, 2 effort, 3 miscellaneous).</summary>
    internal static int Place(uint personality, int kind) => Orders[personality % 24][kind];

    /// <summary>
    /// Reads a Pokémon whose substructures are encrypted and in the game's order (a box's slot, a <c>.ek3</c>): null for a
    /// slot with no species, a bad Egg for one whose checksum fails, which is what the game itself would show.
    /// </summary>
    public static ForeignPokemon? ReadEncrypted(ReadOnlySpan<byte> data, GbaGame game)
    {
        if (data.Length < BoxSize) throw new ArgumentException("a Pokémon of the GBA's is 80 bytes or more");
        uint personality = U32(data, 0), otId = U32(data, 4);
        var secure = new byte[48];
        data.Slice(32, 48).CopyTo(secure);
        for (int i = 0; i < 12; i++)
        {
            uint word = U32(secure, i * 4) ^ otId ^ personality;
            BitConverter.TryWriteBytes(secure.AsSpan(i * 4), word);
        }
        return Read(data, secure, game, shuffled: true);
    }

    /// <summary>Reads a Pokémon whose substructures are plain and in the fixed order (a <c>.pk3</c>); the checksum is still the check.</summary>
    public static ForeignPokemon? ReadPlain(ReadOnlySpan<byte> data, GbaGame game)
    {
        if (data.Length < BoxSize) throw new ArgumentException("a Pokémon of the GBA's is 80 bytes or more");
        return Read(data, data.Slice(32, 48).ToArray(), game, shuffled: false);
    }

    /// <summary>Whether the plain 48 bytes add up to the checksum the header holds: how a <c>.pk3</c> is told from a <c>.ek3</c>.</summary>
    public static bool IsPlain(ReadOnlySpan<byte> data) => data.Length >= BoxSize && WordSum(data.Slice(32, 48)) == U16(data, 28);

    private static ForeignPokemon? Read(ReadOnlySpan<byte> data, byte[] secure, GbaGame game, bool shuffled)
    {
        uint personality = U32(data, 0), otId = U32(data, 4);
        bool hasSpecies = (data[19] & 2) != 0, badEgg = (data[19] & 1) != 0;
        int language = data[18];

        // The checksum is the sum of the whole 48 bytes, in whatever order they lie
        bool bad = WordSum(secure) != U16(data, 28);
        ReadOnlySpan<byte> Sub(int kind) => secure.AsSpan((shuffled ? Place(personality, kind) : kind) * 12, 12);
        var growth = Sub(0);
        int species = U16(growth, 0);
        if (!bad && species == 0 && !hasSpecies) return null;
        if (bad || badEgg)
            return new ForeignPokemon { Console = ForeignConsole.Gba, Game = game, Personality = personality, OtId = otId, IsBadEgg = true, IsEgg = true, GbaSpecies = 412, Species = 412, Language = language };

        var moves = Sub(1);
        var effort = Sub(2);
        var misc = Sub(3);
        uint word0 = U32(misc, 0), word1 = U32(misc, 4);
        int[] moveIds = new int[4], pp = new int[4], bonus = new int[4];
        int ppBonuses = growth[8];
        for (int i = 0; i < 4; i++)
        {
            moveIds[i] = U16(moves, i * 2);
            pp[i] = moves[8 + i];
            bonus[i] = (ppBonuses >> (i * 2)) & 3;
        }
        var ivs = new[] { Bits(word1, 0, 5), Bits(word1, 5, 5), Bits(word1, 10, 5), Bits(word1, 15, 5), Bits(word1, 20, 5), Bits(word1, 25, 5) };
        var evs = new int[] { effort[0], effort[1], effort[2], effort[3], effort[4], effort[5] };

        int language2 = language == 0 ? MigrationDatabase.English : language;
        var otName = MigrationDatabase.TextFromGba(data.Slice(20, 7), language2);
        var nickname = MigrationDatabase.TextFromGba(data.Slice(8, 10), language2);
        return new ForeignPokemon
        {
            Console = ForeignConsole.Gba,
            Game = game,
            Personality = personality,
            OtId = otId,
            GbaSpecies = species,
            Species = MigrationDatabase.SpeciesFromGba(species),
            IsEgg = Bits(word1, 30, 1) == 1 || (data[19] & 4) != 0,
            Nickname = nickname,
            // The games keep the species' name as the nickname of one that was never named: ToSaved takes that for none
            HasNickname = true,
            OtName = otName,
            OtGender = Bits(word0, 31, 1),
            Language = language2,
            HeldItemId = U16(growth, 2),
            Experience = (int)U32(growth, 4),
            Friendship = growth[9],
            AbilityNumber = Bits(word1, 31, 1),
            Moves = moveIds,
            Pp = pp,
            PpUps = bonus,
            Ivs = ivs,
            Evs = evs,
            Cool = effort[6], Beauty = effort[7], Cute = effort[8], Smart = effort[9], Tough = effort[10], Sheen = effort[11],
            Pokerus = Bits(word0, 0, 8),
            Markings = data[27],
            Ball = Bits(word0, 27, 4),
            MetLevel = Bits(word0, 16, 7),
            MetGame = Bits(word0, 23, 4)
        };
    }
}
