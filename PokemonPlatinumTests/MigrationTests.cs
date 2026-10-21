using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Foreign;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 08 · P20: the readers of the player's own files. No save or Pokémon file of the games is in the repository
/// (they are the player's, and not ours to keep): every file here is built byte by byte from the layouts the
/// decompilation's code reads, and read back.
/// </summary>
public class MigrationTests
{
    // ------------------------------------------------------------------ the bytes of files

    private static byte[] GbaText(string text, int length, int language = MigrationDatabase.English)
    {
        var table = MigrationDatabase.Data.GbaChars[language];
        var bytes = new byte[length];
        Array.Fill(bytes, (byte)0xFF);
        for (int i = 0; i < text.Length; i++) bytes[i] = (byte)table.IndexOf(text[i].ToString());
        return bytes;
    }

    private static void DsText(byte[] into, int at, string text, int length)
    {
        var inverse = new Dictionary<string, int>();
        foreach (var (code, character) in MigrationDatabase.Data.DsChars) inverse.TryAdd(character, code);
        for (int i = 0; i < length; i++)
            BitConverter.TryWriteBytes(into.AsSpan(at + i * 2), (ushort)(i < text.Length ? inverse[text[i].ToString()] : 0xFFFF));
    }

    private static void Put16(byte[] data, int at, int value) => BitConverter.TryWriteBytes(data.AsSpan(at), (ushort)value);
    private static void Put32(byte[] data, int at, uint value) => BitConverter.TryWriteBytes(data.AsSpan(at), value);

    private static readonly byte[][] Orders =
    {
        new byte[] { 0, 1, 2, 3 }, new byte[] { 0, 1, 3, 2 }, new byte[] { 0, 2, 1, 3 }, new byte[] { 0, 3, 1, 2 },
        new byte[] { 0, 2, 3, 1 }, new byte[] { 0, 3, 2, 1 }, new byte[] { 1, 0, 2, 3 }, new byte[] { 1, 0, 3, 2 },
        new byte[] { 2, 0, 1, 3 }, new byte[] { 3, 0, 1, 2 }, new byte[] { 2, 0, 3, 1 }, new byte[] { 3, 0, 2, 1 },
        new byte[] { 1, 2, 0, 3 }, new byte[] { 1, 3, 0, 2 }, new byte[] { 2, 1, 0, 3 }, new byte[] { 3, 1, 0, 2 },
        new byte[] { 2, 3, 0, 1 }, new byte[] { 3, 2, 0, 1 }, new byte[] { 1, 2, 3, 0 }, new byte[] { 1, 3, 2, 0 },
        new byte[] { 2, 1, 3, 0 }, new byte[] { 3, 1, 2, 0 }, new byte[] { 2, 3, 1, 0 }, new byte[] { 3, 2, 1, 0 }
    };

    /// <summary>What a test says of a GBA Pokémon before it is laid into 80 bytes.</summary>
    private sealed record GbaSpec(
        uint Personality = 0x12345678, uint OtId = 0x0A0B0C0D, int Species = 277, int Item = 0, int Exp = 0, int Friendship = 200,
        int[]? Moves = null, string Nickname = "", string Ot = "RED", int Language = MigrationDatabase.English,
        int AbilityBit = 0, bool Egg = false, int Ball = 4, int MetLevel = 5, int MetGame = 3, int OtGirl = 0, int Markings = 0,
        int Ppbonus = 0, uint Ivs = 0x1F | 0x1E << 5 | 0x1D << 10 | 0x1C << 15 | 0x1B << 20 | 0x1A << 25, int[]? Evs = null);

    private static byte[] BuildGba(GbaSpec s, bool encrypt = true, bool breakChecksum = false)
    {
        var data = new byte[80];
        Put32(data, 0, s.Personality);
        Put32(data, 4, s.OtId);
        GbaText(s.Nickname.Length > 0 ? s.Nickname : "", 10, s.Language).CopyTo(data, 8);
        data[18] = (byte)s.Language;
        data[19] = (byte)(2 | (s.Egg ? 4 : 0));
        GbaText(s.Ot, 7, s.Language).CopyTo(data, 20);
        data[27] = (byte)s.Markings;

        var growth = new byte[12];
        var moves = new byte[12];
        var effort = new byte[12];
        var misc = new byte[12];
        Put16(growth, 0, s.Species);
        Put16(growth, 2, s.Item);
        Put32(growth, 4, (uint)s.Exp);
        growth[8] = (byte)s.Ppbonus;
        growth[9] = (byte)s.Friendship;
        var mv = s.Moves ?? new[] { 33, 45, 0, 0 };
        for (int i = 0; i < 4; i++) { Put16(moves, i * 2, mv[i]); moves[8 + i] = (byte)(mv[i] == 0 ? 0 : 35); }
        var evs = s.Evs ?? new[] { 10, 20, 30, 40, 50, 60 };
        for (int i = 0; i < 6; i++) effort[i] = (byte)evs[i];
        for (int i = 0; i < 6; i++) effort[6 + i] = (byte)(100 + i);
        misc[0] = 1; // Pokérus
        uint origins = (uint)(s.MetLevel | s.MetGame << 7 | s.Ball << 11 | s.OtGirl << 15);
        Put32(misc, 0, misc[0] | origins << 16);
        Put32(misc, 4, s.Ivs | (s.Egg ? 1u << 30 : 0) | (uint)s.AbilityBit << 31);

        var kinds = new[] { growth, moves, effort, misc };
        var secure = new byte[48];
        for (int kind = 0; kind < 4; kind++) kinds[kind].CopyTo(secure, (encrypt ? Orders[s.Personality % 24][kind] : kind) * 12);
        ushort sum = 0;
        for (int i = 0; i < 48; i += 2) sum += BitConverter.ToUInt16(secure, i);
        Put16(data, 28, sum + (breakChecksum ? 1 : 0));
        if (encrypt)
            for (int i = 0; i < 12; i++) Put32(secure, i * 4, BitConverter.ToUInt32(secure, i * 4) ^ s.OtId ^ s.Personality);
        secure.CopyTo(data, 32);
        return data;
    }

    private sealed record DsSpec(
        uint Personality = 0x00ABCDEF, uint OtId = 0x01020304, int Species = 25, int Item = 0, int Exp = 0, int Friendship = 120, int Ability = 9,
        int[]? Moves = null, string Nickname = "", string Ot = "DAWN", int Language = MigrationDatabase.English, int Form = 0, bool Egg = false,
        bool HasNickname = false, int Ball = 4, int MetLevel = 7, int MetGame = 12, int Year = 8, int Month = 3, int Day = 14, int Pokerus = 0x22,
        uint Ivs = 0x1F | 0x1E << 5 | 0x1D << 10 | 0x1C << 15 | 0x1B << 20 | 0x1A << 25);

    private static byte[] BuildDs(DsSpec s, bool encrypt = true, bool breakChecksum = false)
    {
        var data = new byte[136];
        Put32(data, 0, s.Personality);
        var a = new byte[32];
        var b = new byte[32];
        var c = new byte[32];
        var d = new byte[32];
        Put16(a, 0, s.Species);
        Put16(a, 2, s.Item);
        Put32(a, 4, s.OtId);
        Put32(a, 8, (uint)s.Exp);
        a[0xC] = (byte)s.Friendship;
        a[0xD] = (byte)s.Ability;
        a[0xF] = (byte)s.Language;
        for (int i = 0; i < 6; i++) a[0x10 + i] = (byte)(i + 1);
        for (int i = 0; i < 6; i++) a[0x16 + i] = (byte)(50 + i);
        var mv = s.Moves ?? new[] { 33, 45, 0, 0 };
        for (int i = 0; i < 4; i++) { Put16(b, i * 2, mv[i]); b[8 + i] = (byte)(mv[i] == 0 ? 0 : 35); }
        Put32(b, 0x10, s.Ivs | (s.Egg ? 1u << 30 : 0) | (s.HasNickname ? 1u << 31 : 0));
        b[0x18] = (byte)(s.Form << 3);
        DsText(c, 0, s.Nickname.Length > 0 ? s.Nickname : "", 11);
        c[0x17] = (byte)s.MetGame;
        DsText(d, 0, s.Ot, 8);
        d[0x13] = (byte)s.Year; d[0x14] = (byte)s.Month; d[0x15] = (byte)s.Day;
        d[0x1A] = (byte)s.Pokerus;
        d[0x1B] = (byte)s.Ball;
        d[0x1C] = (byte)s.MetLevel;

        var blocks = new[] { a, b, c, d };
        var shuffled = new byte[128];
        ushort sum = 0;
        for (int i = 0; i < 4; i++)
        {
            blocks[i].CopyTo(shuffled, (encrypt ? Orders[(int)(((s.Personality & 0x3E000) >> 13) % 24)][i] : i) * 32);
            for (int j = 0; j < 32; j += 2) sum += BitConverter.ToUInt16(blocks[i], j);
        }
        Put16(data, 6, sum + (breakChecksum ? 1 : 0));
        if (encrypt)
        {
            uint seed = sum;
            for (int i = 0; i < 128; i += 2)
            {
                seed = seed * 1103515245u + 24691u;
                Put16(shuffled, i, BitConverter.ToUInt16(shuffled, i) ^ (int)(seed >> 16));
            }
        }
        shuffled.CopyTo(data, 8);
        return data;
    }

    private static byte[] EmptyDs()
    {
        var data = new byte[136];
        uint seed = 0;
        for (int i = 8; i < 136; i += 2)
        {
            seed = seed * 1103515245u + 24691u;
            Put16(data, i, (int)(seed >> 16));
        }
        return data;
    }

    // ------------------------------------------------------------------ saves

    private const int Sector = 4096, SectorData = 3968;

    private static ushort SectorChecksum(byte[] sector, int size)
    {
        uint sum = 0;
        for (int i = 0; i < size / 4; i++) sum += BitConverter.ToUInt32(sector, i * 4);
        return (ushort)((sum >> 16) + (sum & 0xFFFF));
    }

    /// <summary>One slot of a GBA save: the fourteen sectors, the first at the place <paramref name="shift"/> turns them to.</summary>
    private static byte[] GbaSlot(GbaGame game, byte[] storage, string trainer, uint trainerId, uint counter, int shift = 0)
    {
        var slot = new byte[14 * Sector];
        for (int id = 0; id < 14; id++)
        {
            var sector = new byte[Sector];
            for (int i = 0; i < SectorData; i++) sector[i] = (byte)(id * 7 + i % 251);
            if (id == 0)
            {
                // Filler after the trainer, so that the sector's checksum tells the games' sizes of it apart
                for (int i = 0x20; i < SectorData; i++) sector[i] = (byte)(1 + i * 13 % 250);
                Array.Clear(sector, 0, 0x20);
                GbaText(trainer, 8).CopyTo(sector, 0);
                Put32(sector, 0xA, trainerId);
            }
            else if (id >= 5)
            {
                Array.Clear(sector, 0, SectorData);
                int from = (id - 5) * SectorData;
                int length = Math.Min(SectorData, storage.Length - from);
                Array.Copy(storage, from, sector, 0, length);
            }
            int size = id switch
            {
                0 => game switch { GbaGame.RubySapphire => 2192, GbaGame.Emerald => 3884, _ => 3876 },
                4 => game == GbaGame.RubySapphire ? 3136 : 3816,
                13 => 2000,
                _ => SectorData
            };
            Put16(sector, 0xFF4, id);
            Put16(sector, 0xFF6, SectorChecksum(sector, size));
            Put32(sector, 0xFF8, 0x08012025);
            Put32(sector, 0xFFC, counter);
            sector.CopyTo(slot, ((id + shift) % 14) * Sector);
        }
        return slot;
    }

    private static byte[] GbaStorage(Dictionary<(int Box, int Slot), byte[]> mons, string? boxName = null)
    {
        var storage = new byte[33744];
        foreach (var ((box, slot), mon) in mons) mon.CopyTo(storage, 4 + (box * 30 + slot) * 80);
        for (int box = 0; box < 14; box++)
            GbaText(box == 0 && boxName != null ? boxName : $"BOX{box + 1}", 9).CopyTo(storage, 4 + 14 * 30 * 80 + box * 9);
        return storage;
    }

    private static byte[] GbaFile(byte[] first, byte[]? second = null)
    {
        var file = new byte[0x20000];
        first.CopyTo(file, 0);
        (second ?? first).CopyTo(file, 14 * Sector);
        return file;
    }

    // The blocks' sizes are the multiples of four the game pads its tables to (PCBoxes: a box number, 18 x 30 Pokémon, 18 names, 18 wallpapers)
    private const int PlatinumNormal = 49416, PlatinumBoxes = 74184 + 20;

    private static byte[] PlatinumBlock(int id, int size, uint global, uint block, byte[] body, bool reflected = false)
    {
        var data = new byte[size];
        body.CopyTo(data, 0);
        var span = data.AsSpan(0, size - 20);
        ushort crc = reflected ? Reflected(span) : Ccitt(span);
        Put32(data, size - 20, global);
        Put32(data, size - 16, block);
        Put32(data, size - 12, (uint)size);
        Put32(data, size - 8, 0x20060623);
        data[size - 4] = (byte)id;
        Put16(data, size - 2, crc);
        return data;
    }

    private static ushort Ccitt(ReadOnlySpan<byte> bytes)
    {
        int crc = 0xFFFF;
        foreach (byte b in bytes)
        {
            crc ^= b << 8;
            for (int bit = 0; bit < 8; bit++) crc = (crc & 0x8000) != 0 ? crc << 1 ^ 0x1021 : crc << 1;
        }
        return (ushort)crc;
    }

    private static ushort Reflected(ReadOnlySpan<byte> bytes)
    {
        int crc = 0xFFFF;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? crc >> 1 ^ 0x8408 : crc >> 1;
        }
        return (ushort)crc;
    }

    private static byte[] PlatinumBody(string trainer, uint id, Dictionary<(int Box, int Slot), byte[]> mons, out byte[] boxBody)
    {
        var normal = new byte[PlatinumNormal - 20];
        DsText(normal, 0x68, trainer, 8);
        Put32(normal, 0x78, id);
        boxBody = new byte[PlatinumBoxes - 20];
        // An empty slot is not zeroes: BoxPokemon_Init clears it and then encrypts it, with the checksum, nought, as the seed
        for (int box = 0; box < 18; box++)
            for (int slot = 0; slot < 30; slot++) EmptyDs().CopyTo(boxBody, 4 + (box * 30 + slot) * 136);
        foreach (var ((box, slot), mon) in mons) mon.CopyTo(boxBody, 4 + (box * 30 + slot) * 136);
        for (int box = 0; box < 18; box++) DsText(boxBody, 4 + 18 * 30 * 136 + box * 40, $"BOX{box + 1}", 20);
        return normal;
    }

    /// <summary>A Platinum save: each copy a normal block and a boxes block, the counters given for the primary and the backup.</summary>
    private static byte[] PlatinumFile(byte[] normalBody, byte[] boxBody, uint primary, uint backup, bool reflected = false, uint? backupBlock = null)
    {
        var file = new byte[0x80000];
        PlatinumBlock(0, PlatinumNormal, primary, primary, normalBody, reflected).CopyTo(file, 0);
        PlatinumBlock(1, PlatinumBoxes, primary, primary, boxBody, reflected).CopyTo(file, PlatinumNormal);
        PlatinumBlock(0, PlatinumNormal, backup, backupBlock ?? backup, normalBody, reflected).CopyTo(file, 0x40000);
        PlatinumBlock(1, PlatinumBoxes, backup, backupBlock ?? backup, boxBody, reflected).CopyTo(file, 0x40000 + PlatinumNormal);
        return file;
    }

    // ------------------------------------------------------------------ the tables

    [Fact]
    public void TheMigrationTablesAreWhatTheImporterReadFromTheDecompilation()
    {
        var data = MigrationDatabase.Data;
        Assert.Equal(144, data.GbaSpecies.Count);
        Assert.Equal(8, data.HmMoves.Count);
        Assert.Equal(145, data.InvalidItems.Count);
        Assert.Equal(94, data.NewAbilities.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 7 }, data.GbaChars.Keys.OrderBy(k => k));
        // Codes 0x00 to 0xF6; 0xFF ends a string and the seven between are none of any table's
        Assert.All(data.GbaChars.Values, table => Assert.Equal(247, table.Count));

        // Hoenn's first starter is the GBA's 277 and the National Pokédex's 252; Deoxys is 410 and 386; Chimecho 411 and 358
        Assert.Equal(252, MigrationDatabase.SpeciesFromGba(277));
        Assert.Equal(386, MigrationDatabase.SpeciesFromGba(410));
        Assert.Equal(358, MigrationDatabase.SpeciesFromGba(411));
        Assert.Equal(25, MigrationDatabase.SpeciesFromGba(25));
        Assert.True(MigrationDatabase.IsGbaSpeciesReal(251) && MigrationDatabase.IsGbaSpeciesReal(277) && MigrationDatabase.IsGbaSpeciesReal(411));
        Assert.False(MigrationDatabase.IsGbaSpeciesReal(252));
        Assert.False(MigrationDatabase.IsGbaSpeciesReal(412));
        Assert.False(MigrationDatabase.IsGbaSpeciesReal(0));
    }

    [Fact]
    public void EveryLanguagesGbaCharactersMakeTheTextTheyWereWrittenAs()
    {
        foreach (int language in MigrationDatabase.Data.GbaChars.Keys)
        {
            // The Japanese games' letters are their own (full-width) ones
            string name = language == MigrationDatabase.Japanese ? "あいう" : "ASH";
            var bytes = GbaText(name, 8, language);
            Assert.Equal(name, MigrationDatabase.TextFromGba(bytes, language));
        }
        var ds = new byte[40];
        DsText(ds, 0, "MAY", 20);
        var codes = new ushort[20];
        for (int i = 0; i < 20; i++) codes[i] = BitConverter.ToUInt16(ds, i * 2);
        Assert.Equal("MAY", MigrationDatabase.TextFromDs(codes));
    }

    // ------------------------------------------------------------------ a Pokémon of the GBA's

    [Fact]
    public void AGbaPokemonIsReadWhicheverOrderItsPersonalityLaysItsPartsIn()
    {
        // The 24 orders: the personality's remainder of 24 chooses where the four substructures lie
        for (uint remainder = 0; remainder < 24; remainder++)
        {
            uint personality = 0x1000 * 24 + remainder;
            var spec = new GbaSpec(Personality: personality, Species: 277, Exp: 1234, Item: 13, Nickname: "BLAZE", Ot: "BRENDAN", OtGirl: 0, Markings: 3);
            var mon = GbaPokemon.ReadEncrypted(BuildGba(spec), GbaGame.Emerald)!;
            Assert.NotNull(mon);
            Assert.Equal(277, mon.GbaSpecies);
            Assert.Equal(252, mon.Species);
            Assert.Equal(1234, mon.Experience);
            Assert.Equal(13, mon.HeldItemId);
            Assert.Equal("BLAZE", mon.Nickname);
            Assert.Equal("BRENDAN", mon.OtName);
            Assert.Equal(new[] { 33, 45, 0, 0 }, mon.Moves);
            Assert.Equal(new[] { 31, 30, 29, 28, 27, 26 }, mon.Ivs);
            Assert.Equal(new[] { 10, 20, 30, 40, 50, 60 }, mon.Evs);
            Assert.Equal(3, mon.Markings);
            Assert.Equal(personality, mon.Personality);
            Assert.Equal(0x0A0B0C0Du, mon.OtId);
            Assert.False(mon.IsEgg);
            Assert.False(mon.IsBadEgg);
        }
    }

    [Fact]
    public void AGbaPokemonReadsTheMetFieldsAndTheContestStatsToo()
    {
        var spec = new GbaSpec(Ball: 11, MetLevel: 42, MetGame: 5, OtGirl: 1, Language: MigrationDatabase.French, Ot: "LEA", Nickname: "FLAMME");
        var mon = GbaPokemon.ReadEncrypted(BuildGba(spec), GbaGame.FireRedLeafGreen)!;
        Assert.Equal(11, mon.Ball);
        Assert.Equal(42, mon.MetLevel);
        Assert.Equal(5, mon.MetGame);
        Assert.Equal(1, mon.OtGender);
        Assert.Equal(MigrationDatabase.French, mon.Language);
        Assert.Equal("LEA", mon.OtName);
        Assert.Equal(1, mon.Pokerus);
        Assert.Equal((100, 101, 102, 103, 104, 105), (mon.Cool, mon.Beauty, mon.Cute, mon.Smart, mon.Tough, mon.Sheen));
    }

    [Fact]
    public void APlainGbaFileIsToldFromAnEncryptedOne()
    {
        var spec = new GbaSpec(Species: 25, Exp: 500);
        var plain = BuildGba(spec, encrypt: false);
        var encrypted = BuildGba(spec);
        Assert.True(GbaPokemon.IsPlain(plain));
        Assert.False(GbaPokemon.IsPlain(encrypted));
        // A plain Pokémon keeps its parts in the fixed order, an encrypted one in the personality's: both give the same Pokémon
        var fromPlain = GbaPokemon.ReadPlain(plain, GbaGame.Unknown)!;
        var fromEncrypted = GbaPokemon.ReadEncrypted(encrypted, GbaGame.Unknown)!;
        Assert.Equal(25, fromPlain.GbaSpecies);
        Assert.Equal(500, fromPlain.Experience);
        Assert.Equal(fromEncrypted.Experience, fromPlain.Experience);
        Assert.Equal(fromEncrypted.Moves, fromPlain.Moves);
        Assert.Equal(fromEncrypted.Ivs, fromPlain.Ivs);
        Assert.Equal(25, ForeignFile.Read(plain).Save!.All.Single().GbaSpecies);
        Assert.Equal(25, ForeignFile.Read(encrypted).Save!.All.Single().GbaSpecies);
    }

    [Fact]
    public void AGbaPokemonWhoseChecksumFailsIsABadEggAndAnEmptySlotIsNothing()
    {
        var bad = GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(), breakChecksum: true), GbaGame.Emerald)!;
        Assert.True(bad.IsBadEgg);
        Assert.True(bad.IsEgg);
        Assert.Null(GbaPokemon.ReadEncrypted(new byte[80], GbaGame.Emerald));
    }

    [Fact]
    public void AGbaPokemonIsMadeWhatPlatinumMakesOfIt()
    {
        var treecko = PokemonDatabase.GetByDex(252)!;
        int exp = Pokemon.GetExpForLevel(16, treecko.GrowthRate);
        var read = GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Species: 277, Exp: exp, Friendship: 255, Nickname: "TREECKO", Moves: new[] { 33, 45, 98, 0 }, Ppbonus: 0b10)), GbaGame.Emerald)!;
        var saved = read.ToSaved(Ruleset.Modern);

        Assert.Equal("Treecko", saved.SpeciesName);
        Assert.Equal(252, saved.DexNumber);
        Assert.Equal(16, saved.Level);
        Assert.Equal(exp, saved.CurrentExp);
        // The games keep the species' name as the name of a Pokémon never named, which Platinum takes for none
        Assert.True(string.IsNullOrEmpty(saved.Nickname));
        // Friendship goes back to the base value on the way over
        Assert.Equal(ForeignPokemon.BaseFriendship, saved.Friendship);
        Assert.Equal(new[] { 31, 30, 29, 28, 27, 26 }, new[] { saved.IvHP, saved.IvAttack, saved.IvDefense, saved.IvSpeed, saved.IvSpAttack, saved.IvSpDefense });
        Assert.Equal(new[] { 10, 20, 30, 40, 50, 60 }, new[] { saved.EvHP, saved.EvAttack, saved.EvDefense, saved.EvSpeed, saved.EvSpAttack, saved.EvSpDefense });
        Assert.Equal(new[] { ForeignPokemon.MoveName(33), ForeignPokemon.MoveName(45), ForeignPokemon.MoveName(98) }, saved.Moves.Select(m => m.MoveName).ToArray());
        Assert.Equal("Poké Ball", saved.Ball);
        Assert.Equal(0x12345678u, saved.Personality);
        Assert.Equal("RED", saved.OriginalTrainer!.Name);
        Assert.Equal(PokemonPlatinumEngine.Models.Personality.NatureOf(0x12345678), saved.Nature);
    }

    [Fact]
    public void ANicknameStaysAndTheSecondAbilityComesOnlyWhereTheSpeciesHasOneAndNeverGainedOne()
    {
        var species = PokemonDatabase.GetAll().First(s => s.DexNumber is >= 1 and <= 251 && s.Abilities.Count >= 2 && !MigrationDatabase.GainedAnAbility(s.DexNumber));
        int gba = species.DexNumber;
        var first = GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Species: gba, AbilityBit: 0, Nickname: "BUDDY")), GbaGame.Emerald)!.ToSaved(Ruleset.Modern);
        var second = GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Species: gba, AbilityBit: 1)), GbaGame.Emerald)!.ToSaved(Ruleset.Modern);
        Assert.Equal("BUDDY", first.Nickname);
        Assert.Equal(species.Abilities[0], first.Ability);
        Assert.Equal(species.Abilities[1], second.Ability);

        var gained = PokemonDatabase.GetAll().First(s => s.DexNumber is >= 1 and <= 251 && s.Abilities.Count >= 2 && MigrationDatabase.GainedAnAbility(s.DexNumber));
        var taken = GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Species: gained.DexNumber, AbilityBit: 1)), GbaGame.Emerald)!.ToSaved(Ruleset.Modern);
        Assert.Equal(gained.Abilities[0], taken.Ability);
    }

    [Fact]
    public void UnownsLetterAndDeoxyssFormeAreWorkedOutAsTheMigrationDoesIt()
    {
        // Unown: two bits of each byte of the personality, modulo 28 (0 is A, which is the species' own)
        Assert.Null(GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Personality: 0, Species: 201)), GbaGame.Emerald)!.ToSaved(Ruleset.Modern).Form);
        Assert.Equal("Unown-B", GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Personality: 1, Species: 201)), GbaGame.Emerald)!.ToSaved(Ruleset.Modern).Form);
        Assert.Equal("Unown-D", GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Personality: 3, Species: 201)), GbaGame.Emerald)!.ToSaved(Ruleset.Modern).Form);

        // Deoxys: Emerald's is Speed, FireRed's Attack and LeafGreen's Defense (the Pokémon's own game of origin), Ruby's and Sapphire's the normal forme
        string? Forme(GbaGame game, int metGame) =>
            GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Species: 410, MetGame: metGame)), game)!.ToSaved(Ruleset.Modern).Form;
        Assert.Equal("Deoxys-Speed", Forme(GbaGame.Emerald, 3));
        Assert.Equal("Deoxys-Attack", Forme(GbaGame.FireRedLeafGreen, 4));
        Assert.Equal("Deoxys-Defense", Forme(GbaGame.FireRedLeafGreen, 5));
        Assert.Null(Forme(GbaGame.RubySapphire, 1));
    }

    [Fact]
    public void WhatPlatinumRefusesAPokemonForIsAskedOfTheReadOne()
    {
        var cut = GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Moves: new[] { 15, 0, 0, 0 })), GbaGame.Emerald)!;
        Assert.True(cut.KnowsAnHm);
        var plain = GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Moves: new[] { 33, 0, 0, 0 })), GbaGame.Emerald)!;
        Assert.False(plain.KnowsAnHm);
        int invalid = MigrationDatabase.Data.InvalidItems[0];
        Assert.True(GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Item: invalid)), GbaGame.Emerald)!.HoldsAnInvalidItem);
        int allowed = MigrationDatabase.Data.GbaItems.Keys.First(k => !MigrationDatabase.IsInvalidGbaItem(k));
        Assert.False(GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Item: allowed)), GbaGame.Emerald)!.HoldsAnInvalidItem);
        Assert.False(GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Species: 252)), GbaGame.Emerald)!.HasARealSpecies);
        Assert.True(GbaPokemon.ReadEncrypted(BuildGba(new GbaSpec(Species: 277)), GbaGame.Emerald)!.HasARealSpecies);
    }

    // ------------------------------------------------------------------ a Pokémon of the DS games'

    [Fact]
    public void ADsPokemonIsReadWhicheverOrderItsPersonalityShufflesItsBlocksIn()
    {
        for (uint order = 0; order < 24; order++)
        {
            uint personality = (order << 13) | 0x1234;
            var spec = new DsSpec(Personality: personality, Species: 25, Exp: 4321, Nickname: "SPARKY", HasNickname: true, Ot: "DAWN", Form: 0);
            var mon = DsPokemon.ReadEncrypted(BuildDs(spec))!;
            Assert.NotNull(mon);
            Assert.Equal(25, mon.Species);
            Assert.Equal(4321, mon.Experience);
            Assert.Equal("SPARKY", mon.Nickname);
            Assert.True(mon.HasNickname);
            Assert.Equal("DAWN", mon.OtName);
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, mon.Evs);
            Assert.Equal(new[] { 31, 30, 29, 28, 27, 26 }, mon.Ivs);
            Assert.Equal(new DateTime(2008, 3, 14), mon.MetDate);
            Assert.Equal(0x22, mon.Pokerus);
            Assert.Equal(12, mon.MetGame);
            Assert.Equal(120, mon.Friendship);
        }
    }

    [Fact]
    public void ADsPokemonKeepsItsFriendshipAndItsAbilityAndItsFormWhenItIsMadeASavedPokemon()
    {
        var rotom = PokemonDatabase.GetByDex(479)!;
        int exp = Pokemon.GetExpForLevel(30, rotom.GrowthRate);
        var spec = new DsSpec(Species: 479, Exp: exp, Form: 2, Friendship: 33, Ability: 26);
        var saved = DsPokemon.ReadEncrypted(BuildDs(spec))!.ToSaved(Ruleset.Modern);
        Assert.Equal("Rotom", saved.SpeciesName);
        Assert.Equal("Rotom-Wash", saved.Form);
        Assert.Equal(33, saved.Friendship);
        Assert.Equal(30, saved.Level);
        Assert.Equal(AbilityDatabase.NameOf(26), saved.Ability);
        Assert.Equal("Poké Ball", saved.Ball);
        Assert.Equal(new DateTime(2008, 3, 14), saved.MetDate);
    }

    [Fact]
    public void APlainDsFileAndAnEncryptedOneAreToldApart()
    {
        var spec = new DsSpec();
        Assert.True(DsPokemon.IsPlain(BuildDs(spec, encrypt: false)));
        Assert.False(DsPokemon.IsPlain(BuildDs(spec)));
        Assert.Equal(25, ForeignFile.Read(BuildDs(spec, encrypt: false)).Save!.All.Single().Species);
        Assert.Equal(25, ForeignFile.Read(BuildDs(spec)).Save!.All.Single().Species);
    }

    [Fact]
    public void ADsPokemonWhoseChecksumFailsIsABadEggAndANothingIsNothing()
    {
        Assert.True(DsPokemon.ReadEncrypted(BuildDs(new DsSpec(), breakChecksum: true))!.IsBadEgg);
        Assert.Null(DsPokemon.ReadEncrypted(EmptyDs()));
        // Bytes that were never an encoded slot at all fail their checksum, as the game itself finds
        Assert.True(DsPokemon.ReadEncrypted(new byte[136])!.IsBadEgg);
    }

    // ------------------------------------------------------------------ the GBA's saves

    private static Dictionary<(int, int), byte[]> TwoGbaMons() => new()
    {
        [(0, 0)] = BuildGba(new GbaSpec(Species: 277, Exp: 1000, Nickname: "ONE")),
        [(13, 29)] = BuildGba(new GbaSpec(Personality: 99, Species: 25, Exp: 2000, Nickname: "LAST"))
    };

    [Fact]
    public void AGbaSaveGivesItsBoxesItsTrainerAndTheGameItCameFrom()
    {
        foreach (var game in new[] { GbaGame.RubySapphire, GbaGame.Emerald, GbaGame.FireRedLeafGreen })
        {
            var file = GbaFile(GbaSlot(game, GbaStorage(TwoGbaMons(), "MY BOX"), "BRENDAN", 0x7B01_1234u, 7));
            var (save, refusal) = ForeignFile.Read(file);
            Assert.Equal(ForeignRefusal.None, refusal);
            Assert.NotNull(save);
            Assert.Equal(ForeignKind.GbaSave, save!.Kind);
            Assert.Equal(game, save.Game);
            Assert.Equal("BRENDAN", save.TrainerName);
            Assert.Equal(0x7B01_1234u, save.TrainerId);
            Assert.Equal(14, save.Boxes.Count);
            Assert.Equal("MY BOX", save.Boxes[0].Name);
            Assert.Equal(2, save.Count);
            Assert.Equal("ONE", save.Boxes[0].Slots[0]!.Nickname);
            Assert.Equal("LAST", save.Boxes[13].Slots[29]!.Nickname);
            Assert.Equal(252, save.Boxes[0].Slots[0]!.Species);
            Assert.Equal(game, save.All.First().Game);
        }
    }

    [Fact]
    public void AGbaSavesSectorsMayLieInAnyTurnOfTheFourteen()
    {
        for (int shift = 0; shift < 14; shift += 3)
        {
            var file = GbaFile(GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "MAY", 5, 3, shift));
            var (save, _) = ForeignFile.Read(file);
            Assert.Equal(2, save!.Count);
            Assert.Equal("MAY", save.TrainerName);
        }
    }

    [Fact]
    public void TheNewerOfAGbaSavesTwoSlotsIsTheOneRead()
    {
        var older = TwoGbaMons();
        var newer = new Dictionary<(int, int), byte[]>(older) { [(5, 5)] = BuildGba(new GbaSpec(Species: 25, Nickname: "NEWEST")) };
        var olderSlot = GbaSlot(GbaGame.Emerald, GbaStorage(older), "OLD", 1, 10);
        var newerSlot = GbaSlot(GbaGame.Emerald, GbaStorage(newer), "NEW", 1, 11);

        Assert.Equal("NEW", ForeignFile.Read(GbaFile(olderSlot, newerSlot)).Save!.TrainerName);
        Assert.Equal("NEW", ForeignFile.Read(GbaFile(newerSlot, olderSlot)).Save!.TrainerName);
        Assert.Equal(3, ForeignFile.Read(GbaFile(newerSlot, olderSlot)).Save!.Count);
    }

    [Fact]
    public void ACounterThatWrappedFromTheTopToNothingIsTheNewer()
    {
        var wrapped = GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "WRAPPED", 1, 0);
        var top = GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "TOP", 1, 0xFFFFFFFF);
        Assert.Equal("WRAPPED", ForeignFile.Read(GbaFile(top, wrapped)).Save!.TrainerName);
        Assert.Equal("WRAPPED", ForeignFile.Read(GbaFile(wrapped, top)).Save!.TrainerName);
    }

    [Fact]
    public void ADamagedNewerSlotLeavesTheOlderToBeRead()
    {
        var older = GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "OLD", 1, 10);
        var newer = GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "NEW", 1, 11);
        newer[7 * Sector + 100] ^= 0xFF;
        var (save, refusal) = ForeignFile.Read(GbaFile(older, newer));
        Assert.Equal(ForeignRefusal.None, refusal);
        Assert.Equal("OLD", save!.TrainerName);
    }

    [Fact]
    public void AGbaSaveWithNoWholeSlotIsRefusedWholeAndSoIsOneOfTheWrongSize()
    {
        var a = GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "A", 1, 10);
        var b = GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "B", 1, 11);
        a[8 * Sector + 5] ^= 1;
        b[2 * Sector + 5] ^= 1;
        Assert.Equal(ForeignRefusal.DamagedSave, ForeignFile.Read(GbaFile(a, b)).Refusal);
        Assert.Equal(ForeignRefusal.DamagedSave, ForeignFile.Read(new byte[0x20000]).Refusal);
        Assert.Equal(ForeignRefusal.UnknownSize, ForeignFile.Read(new byte[0x20000 - 1]).Refusal);
        Assert.Equal(ForeignRefusal.UnknownSize, ForeignFile.Read(new byte[1234]).Refusal);
        Assert.Null(ForeignFile.Read(new byte[1234]).Save);
    }

    // ------------------------------------------------------------------ Platinum's saves

    private static Dictionary<(int, int), byte[]> TwoDsMons() => new()
    {
        [(0, 0)] = BuildDs(new DsSpec(Species: 25, Exp: 1000, Nickname: "FIRST", HasNickname: true)),
        [(17, 29)] = BuildDs(new DsSpec(Personality: 0x5A5A5A, Species: 479, Form: 1, Exp: 2000))
    };

    [Fact]
    public void APlatinumSaveGivesItsEighteenBoxesAndItsTrainer()
    {
        var normal = PlatinumBody("DAWN", 0x00AB_1234u, TwoDsMons(), out var boxes);
        var (save, refusal) = ForeignFile.Read(PlatinumFile(normal, boxes, 5, 4));
        Assert.Equal(ForeignRefusal.None, refusal);
        Assert.Equal(ForeignKind.PlatinumSave, save!.Kind);
        Assert.Equal("DAWN", save.TrainerName);
        Assert.Equal(0x00AB_1234u, save.TrainerId);
        Assert.Equal(18, save.Boxes.Count);
        Assert.Equal("BOX1", save.Boxes[0].Name);
        Assert.Equal(2, save.Count);
        Assert.Equal("FIRST", save.Boxes[0].Slots[0]!.Nickname);
        Assert.Equal(479, save.Boxes[17].Slots[29]!.Species);
        Assert.Equal(1, save.Boxes[17].Slots[29]!.FormNumber);
    }

    [Fact]
    public void APlatinumSaveIsReadWhicheverWayRoundTheCrcIsTaken()
    {
        var normal = PlatinumBody("DAWN", 1, TwoDsMons(), out var boxes);
        Assert.Equal(2, ForeignFile.Read(PlatinumFile(normal, boxes, 5, 4, reflected: true)).Save!.Count);
        Assert.Equal(2, ForeignFile.Read(PlatinumFile(normal, boxes, 5, 4, reflected: false)).Save!.Count);
    }

    [Fact]
    public void TheBackupOfAPlatinumSaveIsReadWhenThePrimaryIsDamaged()
    {
        var normal = PlatinumBody("DAWN", 1, TwoDsMons(), out var boxes);
        var file = PlatinumFile(normal, boxes, 9, 8);
        file[PlatinumNormal + 5000] ^= 0xFF;
        var (save, refusal) = ForeignFile.Read(file);
        Assert.Equal(ForeignRefusal.None, refusal);
        Assert.Equal(2, save!.Count);

        // And the other way round, with the backup the newer
        var backupNewer = PlatinumFile(normal, boxes, 8, 9);
        backupNewer[0x40000 + 100] ^= 0xFF;
        Assert.Equal(ForeignRefusal.None, ForeignFile.Read(backupNewer).Refusal);
    }

    [Fact]
    public void APlatinumSaveWithNoWholeBlockInEitherCopyIsRefusedWhole()
    {
        var normal = PlatinumBody("DAWN", 1, TwoDsMons(), out var boxes);
        var file = PlatinumFile(normal, boxes, 9, 8);
        file[PlatinumNormal + 5000] ^= 0xFF;
        file[0x40000 + PlatinumNormal + 5000] ^= 0xFF;
        Assert.Equal(ForeignRefusal.DamagedSave, ForeignFile.Read(file).Refusal);
        Assert.Equal(ForeignRefusal.DamagedSave, ForeignFile.Read(new byte[0x80000]).Refusal);
        Assert.Equal(ForeignRefusal.UnknownSize, ForeignFile.Read(new byte[0x80000 + 1]).Refusal);
    }

    // ------------------------------------------------------------------ the door

    private static string Scratch(string name, byte[] bytes)
    {
        string folder = Path.Combine(Path.GetTempPath(), "migration-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact]
    public void AFileOpenedIsLeftExactlyAsItWas()
    {
        var files = new[]
        {
            Scratch("emerald.sav", GbaFile(GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "BRENDAN", 1, 3))),
            Scratch("platinum.sav", PlatinumFile(PlatinumBody("DAWN", 1, TwoDsMons(), out var boxes), boxes, 5, 4)),
            Scratch("mon.pk3", BuildGba(new GbaSpec(), encrypt: false)),
            Scratch("mon.ek3", BuildGba(new GbaSpec())),
            Scratch("mon.pk4", BuildDs(new DsSpec(), encrypt: false)),
            Scratch("mon.ek4", BuildDs(new DsSpec()))
        };
        try
        {
            foreach (string path in files)
            {
                var bytes = File.ReadAllBytes(path);
                var written = File.GetLastWriteTimeUtc(path);
                var folder = Directory.GetFileSystemEntries(Path.GetDirectoryName(path)!).Order().ToArray();

                var (save, refusal) = ForeignFile.Open(path);
                Assert.Equal(ForeignRefusal.None, refusal);
                Assert.NotNull(save);
                Assert.True(save!.Count >= 1);

                Assert.Equal(bytes, File.ReadAllBytes(path));
                Assert.Equal(written, File.GetLastWriteTimeUtc(path));
                Assert.Equal(folder, Directory.GetFileSystemEntries(Path.GetDirectoryName(path)!).Order().ToArray());
            }
        }
        finally
        {
            foreach (string path in files) Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void AReadOnlyFileOpensAndAFileThatIsNotThereOrIsTheWrongSizeIsRefusedAtOnce()
    {
        string path = Scratch("locked.ek4", BuildDs(new DsSpec()));
        try
        {
            File.SetAttributes(path, FileAttributes.ReadOnly);
            Assert.Equal(ForeignRefusal.None, ForeignFile.Open(path).Refusal);
            File.SetAttributes(path, FileAttributes.Normal);

            Assert.Equal(ForeignRefusal.Unreadable, ForeignFile.Open(path + ".nothing").Refusal);
            string odd = Scratch("odd.bin", new byte[999]);
            Assert.Equal(ForeignRefusal.UnknownSize, ForeignFile.Open(odd).Refusal);
            Directory.Delete(Path.GetDirectoryName(odd)!, true);
            string huge = Scratch("huge.bin", new byte[0x80001]);
            Assert.Equal(ForeignRefusal.UnknownSize, ForeignFile.Open(huge).Refusal);
            Directory.Delete(Path.GetDirectoryName(huge)!, true);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void APokemonFileWithNoPokemonOrABadChecksumIsRefusedWhole()
    {
        Assert.Equal(ForeignRefusal.DamagedPokemon, ForeignFile.Read(BuildGba(new GbaSpec(), breakChecksum: true)).Refusal);
        Assert.Equal(ForeignRefusal.DamagedPokemon, ForeignFile.Read(BuildDs(new DsSpec(), breakChecksum: true)).Refusal);
        Assert.Equal(ForeignRefusal.DamagedPokemon, ForeignFile.Read(new byte[80]).Refusal);
        Assert.Equal(ForeignRefusal.DamagedPokemon, ForeignFile.Read(new byte[136]).Refusal);
    }

    [Fact]
    public void APartyPokemonsFilesReadAsABoxPokemonsDo()
    {
        var gba = BuildGba(new GbaSpec(Species: 25, Exp: 99));
        var party = new byte[100];
        gba.CopyTo(party, 0);
        Assert.Equal(99, ForeignFile.Read(party).Save!.All.Single().Experience);
        var ds = BuildDs(new DsSpec());
        var dsParty = new byte[236];
        ds.CopyTo(dsParty, 0);
        Assert.Equal(25, ForeignFile.Read(dsParty).Save!.All.Single().Species);
    }

    [Fact]
    public void ConvertedPokemonHaveTheShapeASavedPokemonNeeds()
    {
        // Everything a migrated Pokémon holds becomes a SavedPokemonData of species and moves this game has
        var saved = ForeignFile.Read(GbaFile(GbaSlot(GbaGame.Emerald, GbaStorage(TwoGbaMons()), "MAY", 1, 1))).Save!.All.Select(p => p.ToSaved(Ruleset.Modern)).ToList();
        Assert.Equal(2, saved.Count);
        Assert.All(saved, p =>
        {
            Assert.NotNull(PokemonDatabase.Get(p.SpeciesName));
            Assert.All(p.Moves, m => Assert.NotNull(MoveDatabase.Get(m.MoveName)));
            Assert.InRange(p.Level, 1, 100);
        });
    }
}
