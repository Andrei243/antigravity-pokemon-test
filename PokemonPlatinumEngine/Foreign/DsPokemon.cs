using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumEngine.Foreign.ForeignBytes;

namespace PokemonPlatinumEngine.Foreign;

/// <summary>
/// A Pokémon of the DS games' (plan 08 · P20), 136 bytes in a box (a party Pokémon's 236 add stats that nothing here
/// reads), from <c>src/pokemon.c</c> and <c>struct_defs/pokemon.h</c>: the personality, a checksum, and four blocks of
/// 32 bytes that lie in an order the personality chooses and are encrypted by a generator the checksum seeds.
/// </summary>
public static class DsPokemon
{
    public const int BoxSize = 136, PartySize = 236;
    private const int BlockSize = 32, Blocks = 4, DataStart = 8, DataSize = BlockSize * Blocks;

    // Where blocks A, B, C and D lie for the personality's bits 13 to 17 (the DATA_BLOCK_SHUFFLE_CASE rows; 24 to 31 are 0 to 7 again)
    private static readonly byte[][] Orders =
    {
        new byte[] { 0, 1, 2, 3 }, new byte[] { 0, 1, 3, 2 }, new byte[] { 0, 2, 1, 3 }, new byte[] { 0, 3, 1, 2 },
        new byte[] { 0, 2, 3, 1 }, new byte[] { 0, 3, 2, 1 }, new byte[] { 1, 0, 2, 3 }, new byte[] { 1, 0, 3, 2 },
        new byte[] { 2, 0, 1, 3 }, new byte[] { 3, 0, 1, 2 }, new byte[] { 2, 0, 3, 1 }, new byte[] { 3, 0, 2, 1 },
        new byte[] { 1, 2, 0, 3 }, new byte[] { 1, 3, 0, 2 }, new byte[] { 2, 1, 0, 3 }, new byte[] { 3, 1, 0, 2 },
        new byte[] { 2, 3, 0, 1 }, new byte[] { 3, 2, 0, 1 }, new byte[] { 1, 2, 3, 0 }, new byte[] { 1, 3, 2, 0 },
        new byte[] { 2, 1, 3, 0 }, new byte[] { 3, 1, 2, 0 }, new byte[] { 2, 3, 1, 0 }, new byte[] { 3, 2, 1, 0 }
    };

    internal static int Place(uint personality, int block) => Orders[(int)(((personality & 0x3E000) >> 13) % 24)][block];

    /// <summary>Whether the 128 bytes of blocks add up, as they lie, to the checksum the header holds: a <c>.pk4</c> is plain, a <c>.ek4</c> is not.</summary>
    public static bool IsPlain(ReadOnlySpan<byte> data) => data.Length >= BoxSize && WordSum(data.Slice(DataStart, DataSize)) == U16(data, 6);

    /// <summary>
    /// The encryption (<c>EncodeData</c> and <c>DecodeData</c> are one): each 16-bit word xored with the high half of
    /// the next number of the linear congruential generator, started at the checksum.
    /// </summary>
    internal static void Crypt(Span<byte> words, uint seed)
    {
        for (int i = 0; i + 1 < words.Length; i += 2)
        {
            seed = seed * 1103515245u + 24691u;
            ushort word = (ushort)(U16(words, i) ^ (seed >> 16));
            BitConverter.TryWriteBytes(words[i..], word);
        }
    }

    /// <summary>Reads a Pokémon as the game keeps it in a box (shuffled and encrypted), or a <c>.ek4</c>: null for a slot with no species, a bad Egg for a checksum that fails.</summary>
    public static ForeignPokemon? ReadEncrypted(ReadOnlySpan<byte> data)
    {
        if (data.Length < BoxSize) throw new ArgumentException("a Pokémon of the DS games' is 136 bytes or more");
        var blocks = data.Slice(DataStart, DataSize).ToArray();
        Crypt(blocks, U16(data, 6));
        return Read(data, blocks, shuffled: true);
    }

    /// <summary>Reads a Pokémon whose blocks are plain and in the order A, B, C, D (a <c>.pk4</c>).</summary>
    public static ForeignPokemon? ReadPlain(ReadOnlySpan<byte> data)
    {
        if (data.Length < BoxSize) throw new ArgumentException("a Pokémon of the DS games' is 136 bytes or more");
        return Read(data, data.Slice(DataStart, DataSize).ToArray(), shuffled: false);
    }

    private static ForeignPokemon? Read(ReadOnlySpan<byte> data, byte[] blocks, bool shuffled)
    {
        uint personality = U32(data, 0);
        ReadOnlySpan<byte> Block(int index) => blocks.AsSpan((shuffled ? Place(personality, index) : index) * BlockSize, BlockSize);
        bool bad = WordSum(blocks) != U16(data, 6);
        var a = Block(0);
        int species = U16(a, 0);
        if (!bad && species == 0) return null;
        if (bad) return new ForeignPokemon { Console = ForeignConsole.Ds, Personality = personality, IsBadEgg = true, IsEgg = true, Species = 0 };

        var b = Block(1);
        var c = Block(2);
        var d = Block(3);
        uint ivWord = U32(b, 0x10);
        int form = b[0x18] >> 3;
        var moves = new int[4];
        var pp = new int[4];
        var ups = new int[4];
        for (int i = 0; i < 4; i++)
        {
            moves[i] = U16(b, i * 2);
            pp[i] = b[8 + i];
            ups[i] = b[0xC + i];
        }
        int language = a[0xF];
        DateTime? met = d[0x13] == 0 && d[0x14] == 0 && d[0x15] == 0 ? null : SafeDate(2000 + d[0x13], d[0x14], d[0x15]);
        var nickname = new ushort[11];
        var otName = new ushort[8];
        for (int i = 0; i < nickname.Length; i++) nickname[i] = U16(c, i * 2);
        for (int i = 0; i < otName.Length; i++) otName[i] = U16(d, i * 2);

        return new ForeignPokemon
        {
            Console = ForeignConsole.Ds,
            Personality = personality,
            OtId = U32(a, 4),
            Species = species,
            IsEgg = Bits(ivWord, 30, 1) == 1,
            HasNickname = Bits(ivWord, 31, 1) == 1,
            Nickname = MigrationDatabase.TextFromDs(nickname),
            OtName = MigrationDatabase.TextFromDs(otName),
            OtGender = d[0x1C] >> 7,
            Language = language == 0 ? MigrationDatabase.English : language,
            HeldItemId = U16(a, 2),
            Experience = (int)U32(a, 8),
            Friendship = a[0xC],
            AbilityNumber = a[0xD],
            Markings = a[0xE],
            Moves = moves,
            Pp = pp,
            PpUps = ups,
            Ivs = new[] { Bits(ivWord, 0, 5), Bits(ivWord, 5, 5), Bits(ivWord, 10, 5), Bits(ivWord, 15, 5), Bits(ivWord, 20, 5), Bits(ivWord, 25, 5) },
            Evs = new int[] { a[0x10], a[0x11], a[0x12], a[0x13], a[0x14], a[0x15] },
            Cool = a[0x16], Beauty = a[0x17], Cute = a[0x18], Smart = a[0x19], Tough = a[0x1A], Sheen = a[0x1B],
            Pokerus = d[0x1A],
            Ball = d[0x1B],
            MetLevel = d[0x1C] & 0x7F,
            MetGame = c[0x17],
            FormNumber = form,
            MetDate = met
        };
    }

    // ------------------------------------------------------------------ the way out (plan 08 · P22)

    /// <summary>The highest species, item, ability and move numbers Platinum's records can hold (a later one is written as none).</summary>
    public const int HighestSpecies = 493, HighestItem = 536, HighestAbility = 123, HighestMove = 467;

    private static readonly Lazy<Dictionary<string, int>> DsCodes = new(() =>
    {
        var codes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (code, character) in MigrationDatabase.Data.DsChars) codes.TryAdd(character, code);
        return codes;
    });

    private static void PutText(Span<byte> into, string text, int length)
    {
        int at = 0;
        foreach (char c in text)
        {
            if (at >= length || !DsCodes.Value.TryGetValue(c.ToString(), out int code)) continue;
            BitConverter.TryWriteBytes(into[(at * 2)..], (ushort)code);
            at++;
        }
        // A string ends in 0xFFFF and the rest is that too, as the game fills a name it clears
        for (; at < length; at++) BitConverter.TryWriteBytes(into[(at * 2)..], (ushort)0xFFFF);
    }

    /// <summary>
    /// A Pokémon as a <c>.pk4</c> (136 bytes: what a box keeps, plain and in the order A, B, C, D, as the tools that read
    /// <c>.pk4</c> files read them; <see cref="ReadPlain"/> reads it back), for the way out of the migration (plan 08 ·
    /// P22). The original trainer is the Pokémon's own, or the save's player's where it has none. Null for a species Platinum's
    /// records don't have (<see cref="HighestSpecies"/>); a held item, ability or move of a later game is written as none.
    /// </summary>
    public static byte[]? Write(Pokemon pokemon, uint ownTrainerId, string ownTrainerName, bool ownTrainerIsGirl)
    {
        if (pokemon.Species.DexNumber is < 1 or > HighestSpecies) return null;
        var data = new byte[BoxSize];
        var a = new byte[BlockSize];
        var b = new byte[BlockSize];
        var c = new byte[BlockSize];
        var d = new byte[BlockSize];

        var trainer = pokemon.OriginalTrainer;
        uint otId = trainer != null ? unchecked((uint)trainer.Id) : ownTrainerId;
        string otName = trainer?.Name ?? ownTrainerName;
        bool otGirl = trainer != null ? trainer.Look == Core.PlayerLook.Girl : ownTrainerIsGirl;

        BitConverter.TryWriteBytes(a.AsSpan(0), (ushort)pokemon.Species.DexNumber);
        int item = pokemon.HeldItem?.Id ?? 0;
        BitConverter.TryWriteBytes(a.AsSpan(2), (ushort)(item is > 0 and <= HighestItem ? item : 0));
        BitConverter.TryWriteBytes(a.AsSpan(4), otId);
        BitConverter.TryWriteBytes(a.AsSpan(8), (uint)Math.Max(0, pokemon.CurrentExp));
        a[0xC] = (byte)Math.Clamp(pokemon.Friendship, 0, 255);
        int ability = AbilityDatabase.IdOf(pokemon.AbilityName);
        a[0xD] = (byte)(ability is > 0 and <= HighestAbility ? ability : 0);
        a[0xE] = (byte)(pokemon.Marks & 0x3F);
        a[0xF] = (byte)LanguageNumber(pokemon.Language);
        int[] evs = { pokemon.EvHP, pokemon.EvAttack, pokemon.EvDefense, pokemon.EvSpeed, pokemon.EvSpAttack, pokemon.EvSpDefense };
        for (int i = 0; i < 6; i++) a[0x10 + i] = (byte)Math.Clamp(evs[i], 0, 255);
        int[] contest = { pokemon.Cool, pokemon.Beauty, pokemon.Cute, pokemon.Smart, pokemon.Tough, pokemon.Sheen };
        for (int i = 0; i < 6; i++) a[0x16 + i] = (byte)Math.Clamp(contest[i], 0, 255);

        for (int i = 0; i < 4; i++)
        {
            if (i >= pokemon.Moves.Count) break;
            var move = pokemon.Moves[i];
            int id = move.Data.Id;
            BitConverter.TryWriteBytes(b.AsSpan(i * 2), (ushort)(id is > 0 and <= HighestMove ? id : 0));
            b[8 + i] = (byte)Math.Clamp(move.CurrentPP, 0, 255);
            b[0xC + i] = (byte)Math.Clamp(move.PPUps, 0, 3);
        }
        uint ivs = (uint)(pokemon.IvHP & 31 | (pokemon.IvAttack & 31) << 5 | (pokemon.IvDefense & 31) << 10 | (pokemon.IvSpeed & 31) << 15
            | (pokemon.IvSpAttack & 31) << 20 | (pokemon.IvSpDefense & 31) << 25);
        if (pokemon.IsEgg) ivs |= 1u << 30;
        if (pokemon.Nickname.Length > 0) ivs |= 1u << 31;
        BitConverter.TryWriteBytes(b.AsSpan(0x10), ivs);
        int form = pokemon.Form != null ? DsForms.NumberOf(pokemon.Species.Name, pokemon.Form) : 0;
        b[0x18] = (byte)((pokemon.Gender == Gender.Female ? 2 : pokemon.Gender == Gender.Genderless ? 4 : 0) | form << 3);

        PutText(c, pokemon.Nickname, 11);
        c[0x17] = 12; // Platinum
        PutText(d, otName, 8);
        if (pokemon.MetDate is { } met && met.Year >= 2000)
        {
            d[0x13] = (byte)(met.Year - 2000);
            d[0x14] = (byte)met.Month;
            d[0x15] = (byte)met.Day;
        }
        d[0x1A] = (byte)pokemon.Pokerus;
        int ball = Array.IndexOf(BallNames, pokemon.Ball ?? "");
        d[0x1B] = (byte)Math.Max(0, ball);
        d[0x1C] = (byte)(Math.Clamp(pokemon.MetLevel, 0, 127) | (otGirl ? 0x80 : 0));

        BitConverter.TryWriteBytes(data.AsSpan(0), pokemon.Personality);
        a.CopyTo(data, DataStart);
        b.CopyTo(data, DataStart + BlockSize);
        c.CopyTo(data, DataStart + BlockSize * 2);
        d.CopyTo(data, DataStart + BlockSize * 3);
        BitConverter.TryWriteBytes(data.AsSpan(6), WordSum(data.AsSpan(DataStart, DataSize)));
        return data;
    }

    private static readonly string[] BallNames =
    {
        "", "Master Ball", "Ultra Ball", "Great Ball", "Poké Ball", "Safari Ball", "Net Ball", "Dive Ball", "Nest Ball",
        "Repeat Ball", "Timer Ball", "Luxury Ball", "Premier Ball", "Dusk Ball", "Heal Ball", "Quick Ball", "Cherish Ball"
    };

    /// <summary>A language's number in the DS games' records (the GBA's: 1 Japanese, 2 English, 3 French, 4 Italian, 5 German, 7 Spanish); English for none.</summary>
    private static int LanguageNumber(string? language) => language switch
    {
        "Japanese" => MigrationDatabase.Japanese,
        "French" => MigrationDatabase.French,
        "Italian" => MigrationDatabase.Italian,
        "German" => MigrationDatabase.German,
        "Spanish" => MigrationDatabase.Spanish,
        _ => MigrationDatabase.English
    };

    private static DateTime? SafeDate(int year, int month, int day)
    {
        try { return new DateTime(year, month, day); }
        catch (ArgumentOutOfRangeException) { return null; }
    }
}
