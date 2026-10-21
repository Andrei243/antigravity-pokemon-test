using PokemonPlatinumEngine.Data;

/// <summary>
/// A save of the GBA's built byte by byte, for the title's MIGRATE shots (plan 08 · P21): no file of the games is kept
/// anywhere, and the harness writes this one into its out dir's <c>migrate</c> folder (the layouts are the ones the
/// readers take from the decompilation; <c>MigrationTests</c> builds the same).
/// </summary>
static class MigrateFixtures
{
    const int Sector = 4096, Data = 3968;

    static readonly byte[][] Orders =
    {
        new byte[] { 0, 1, 2, 3 }, new byte[] { 0, 1, 3, 2 }, new byte[] { 0, 2, 1, 3 }, new byte[] { 0, 3, 1, 2 },
        new byte[] { 0, 2, 3, 1 }, new byte[] { 0, 3, 2, 1 }, new byte[] { 1, 0, 2, 3 }, new byte[] { 1, 0, 3, 2 },
        new byte[] { 2, 0, 1, 3 }, new byte[] { 3, 0, 1, 2 }, new byte[] { 2, 0, 3, 1 }, new byte[] { 3, 0, 2, 1 },
        new byte[] { 1, 2, 0, 3 }, new byte[] { 1, 3, 0, 2 }, new byte[] { 2, 1, 0, 3 }, new byte[] { 3, 1, 0, 2 },
        new byte[] { 2, 3, 0, 1 }, new byte[] { 3, 2, 0, 1 }, new byte[] { 1, 2, 3, 0 }, new byte[] { 1, 3, 2, 0 },
        new byte[] { 2, 1, 3, 0 }, new byte[] { 3, 1, 2, 0 }, new byte[] { 2, 3, 1, 0 }, new byte[] { 3, 2, 1, 0 }
    };

    static byte[] Text(string text, int length)
    {
        var table = MigrationDatabase.Data.GbaChars[MigrationDatabase.English];
        var bytes = new byte[length];
        Array.Fill(bytes, (byte)0xFF);
        for (int i = 0; i < text.Length && i < length; i++) bytes[i] = (byte)Math.Max(0, table.IndexOf(text[i].ToString()));
        return bytes;
    }

    static void Put16(byte[] data, int at, int value) => BitConverter.TryWriteBytes(data.AsSpan(at), (ushort)value);
    static void Put32(byte[] data, int at, uint value) => BitConverter.TryWriteBytes(data.AsSpan(at), value);

    /// <summary>A Pokémon of the GBA's, 80 bytes as a box keeps it: encrypted, its four parts in the order its personality chooses.</summary>
    public static byte[] Pokemon(uint personality, int species, int exp, int[] moves, string nickname = "", string trainer = "RED",
        uint otId = 0x0001_0002, int item = 0, bool egg = false)
    {
        var data = new byte[80];
        Put32(data, 0, personality);
        Put32(data, 4, otId);
        Text(nickname, 10).CopyTo(data, 8);
        data[18] = MigrationDatabase.English;
        data[19] = (byte)(2 | (egg ? 4 : 0));
        Text(trainer, 7).CopyTo(data, 20);
        var growth = new byte[12];
        var moveBlock = new byte[12];
        var effort = new byte[12];
        var misc = new byte[12];
        Put16(growth, 0, species);
        Put16(growth, 2, item);
        Put32(growth, 4, (uint)exp);
        growth[9] = 200;
        for (int i = 0; i < 4; i++) { Put16(moveBlock, i * 2, moves[i]); moveBlock[8 + i] = (byte)(moves[i] == 0 ? 0 : 35); }
        Put32(misc, 0, (uint)(5 | 3 << 7 | 4 << 11) << 16);
        Put32(misc, 4, 0x1F | 0x1E << 5 | 0x1D << 10 | 0x1C << 15 | 0x1B << 20 | 0x1A << 25 | (egg ? 1u << 30 : 0));
        var kinds = new[] { growth, moveBlock, effort, misc };
        var secure = new byte[48];
        for (int kind = 0; kind < 4; kind++) kinds[kind].CopyTo(secure, Orders[personality % 24][kind] * 12);
        ushort sum = 0;
        for (int i = 0; i < 48; i += 2) sum += BitConverter.ToUInt16(secure, i);
        Put16(data, 28, sum);
        for (int i = 0; i < 12; i++) Put32(secure, i * 4, BitConverter.ToUInt32(secure, i * 4) ^ otId ^ personality);
        secure.CopyTo(data, 32);
        return data;
    }

    static ushort Checksum(byte[] sector, int size)
    {
        uint sum = 0;
        for (int i = 0; i < size / 4; i++) sum += BitConverter.ToUInt32(sector, i * 4);
        return (ushort)((sum >> 16) + (sum & 0xFFFF));
    }

    /// <summary>An Emerald save of 128 KiB with the boxes given (box, place) to Pokémon, a name for the first box and a trainer.</summary>
    public static byte[] Save(Dictionary<(int Box, int Slot), byte[]> mons, string firstBox, string trainer, uint trainerId, uint counter)
    {
        var storage = new byte[33744];
        foreach (var ((box, slot), mon) in mons) mon.CopyTo(storage, 4 + (box * 30 + slot) * 80);
        for (int box = 0; box < 14; box++)
            Text(box == 0 ? firstBox : $"BOX{box + 1}", 9).CopyTo(storage, 4 + 14 * 30 * 80 + box * 9);

        var slotBytes = new byte[14 * Sector];
        for (int id = 0; id < 14; id++)
        {
            var sector = new byte[Sector];
            if (id == 0)
            {
                for (int i = 0x20; i < Data; i++) sector[i] = (byte)(1 + i * 13 % 250);
                Text(trainer, 8).CopyTo(sector, 0);
                Put32(sector, 0xA, trainerId);
            }
            else if (id >= 5)
            {
                int from = (id - 5) * Data;
                Array.Copy(storage, from, sector, 0, Math.Min(Data, storage.Length - from));
            }
            else for (int i = 0; i < Data; i++) sector[i] = (byte)(id * 7 + i % 251);
            int size = id switch { 0 => 3884, 4 => 3816, 13 => 2000, _ => Data };
            Put16(sector, 0xFF4, id);
            Put16(sector, 0xFF6, Checksum(sector, size));
            Put32(sector, 0xFF8, 0x08012025);
            Put32(sector, 0xFFC, counter);
            sector.CopyTo(slotBytes, id * Sector);
        }
        var file = new byte[0x20000];
        slotBytes.CopyTo(file, 0);
        slotBytes.CopyTo(file, 14 * Sector);
        return file;
    }
}
