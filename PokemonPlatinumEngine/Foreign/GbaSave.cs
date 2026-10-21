using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Foreign.ForeignBytes;

namespace PokemonPlatinumEngine.Foreign;

/// <summary>
/// A save of Ruby, Sapphire, Emerald, FireRed or LeafGreen (plan 08 · P20), from <c>src/main_menu/ov97_02235D18.c</c> and
/// <c>include/main_menu/gba_save.h</c>: two slots of fourteen 4 KiB sectors, each with its place among the fourteen,
/// a checksum, a signature and a counter in its last twelve bytes; the newer whole slot is the save. Of it only the
/// trainer's name and ID (the second block, sector 0) and the boxes (sectors 5 to 13, the Pokémon storage) are read.
/// </summary>
public static class GbaSave
{
    public const int SectorSize = 4096, SectorDataSize = 3968, SectorsPerSlot = 14, SlotSize = SectorSize * SectorsPerSlot;

    /// <summary>The file's size: the flash's 128 KiB, and a few bytes more where an emulator adds a clock.</summary>
    public const int FileSize = 0x20000;

    private const uint Signature = 0x08012025;
    private const int StorageFirstSector = 5, StorageSize = 33744, Boxes = 14, BoxSlots = 30, BoxNameLength = 9;

    // The sizes of sector 0 (the second save block) and sector 4 (the end of the first) by game. Ruby's and Sapphire's are
    // the ones Platinum's decompilation sets for them (saveBlock2Size 2192, saveBlock1Size 15040); the others' are not in it
    private static readonly int[] FirstSectorSizes = { 2192, 3884, 3876 };
    private static readonly int[] FifthSectorSizes = { 3136, 3816 };

    /// <summary>What a slot of a file is: whole, partly there, or not a save's.</summary>
    private enum SlotState { Empty, Partial, Whole }

    private sealed class Slot
    {
        public SlotState State;
        public uint Counter;
        public GbaGame Game = GbaGame.Unknown;
        public byte[]? Storage, Trainer;
    }

    /// <summary>Whether a file's size is a GBA save's.</summary>
    public static bool IsSizeOfASave(int length) => length is >= FileSize and <= FileSize + 0x40;

    /// <summary>
    /// Reads a save's boxes and its trainer, or says why it is refused whole. The bytes are only read.
    /// </summary>
    public static (ForeignSave? Save, ForeignRefusal Refusal) Read(ReadOnlySpan<byte> file)
    {
        if (!IsSizeOfASave(file.Length)) return (null, ForeignRefusal.UnknownSize);

        var a = ReadSlot(file[..SlotSize]);
        var b = ReadSlot(file.Slice(SlotSize, SlotSize));
        Slot? chosen;
        if (a.State == SlotState.Whole && b.State == SlotState.Whole) chosen = Newer(a.Counter, b.Counter) ? b : a;
        else if (a.State == SlotState.Whole) chosen = a;
        else if (b.State == SlotState.Whole) chosen = b;
        else return (null, ForeignRefusal.DamagedSave);

        var game = chosen.Game;
        var boxes = new List<(string Name, ForeignPokemon?[] Slots)>();
        var storage = chosen.Storage!;
        for (int box = 0; box < Boxes; box++)
        {
            var slots = new ForeignPokemon?[BoxSlots];
            for (int slot = 0; slot < BoxSlots; slot++)
                slots[slot] = GbaPokemon.ReadEncrypted(storage.AsSpan(4 + (box * BoxSlots + slot) * GbaPokemon.BoxSize, GbaPokemon.BoxSize), game);
            boxes.Add(("", slots));
        }

        // The language the texts of the file are in: the games keep none of their own, so it is the Pokémon's
        int language = boxes.SelectMany(x => x.Slots).OfType<ForeignPokemon>().Where(p => !p.IsBadEgg).GroupBy(p => p.Language)
            .OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault(MigrationDatabase.English);
        var named = new List<ForeignBox>();
        for (int box = 0; box < Boxes; box++)
        {
            var nameBytes = storage.AsSpan(4 + Boxes * BoxSlots * GbaPokemon.BoxSize + box * BoxNameLength, BoxNameLength);
            string name = MigrationDatabase.TextFromGba(nameBytes, language);
            named.Add(new ForeignBox { Name = name.Length > 0 ? name : $"BOX {box + 1}", Slots = boxes[box].Slots });
        }

        var trainer = chosen.Trainer!;
        return (new ForeignSave
        {
            Kind = ForeignKind.GbaSave,
            Game = game,
            TrainerName = MigrationDatabase.TextFromGba(trainer.AsSpan(0, 8), language),
            TrainerId = U32(trainer, 0xA),
            Boxes = named
        }, ForeignRefusal.None);
    }

    /// <summary>Whether the second counter is the newer, as <c>ov97_02235FFC</c> compares them (a counter that wrapped from the top to nought is the newer).</summary>
    private static bool Newer(uint first, uint second)
    {
        if (first == 0xFFFFFFFF && second == 0) return true;
        if (first == 0 && second == 0xFFFFFFFF) return false;
        return first < second;
    }

    private static int Size(int id, int[] known, ReadOnlySpan<byte> data, ushort stored)
    {
        foreach (int size in known)
            if (GbaSectorChecksum(data, size) == stored) return size;
        // A game whose size the decompilation doesn't give: any size a sector's data can be
        for (int size = SectorDataSize; size >= 4; size -= 4)
            if (GbaSectorChecksum(data, size) == stored) return size;
        return -1;
    }

    private static Slot ReadSlot(ReadOnlySpan<byte> slot)
    {
        var result = new Slot();
        var data = new byte[SectorsPerSlot][];
        int seen = 0, signed = 0;
        for (int i = 0; i < SectorsPerSlot; i++)
        {
            var sector = slot.Slice(i * SectorSize, SectorSize);
            if (U32(sector, 0xFF8) != Signature) continue;
            signed++;
            int id = U16(sector, 0xFF4);
            if (id >= SectorsPerSlot || data[id] != null) continue;
            ushort stored = U16(sector, 0xFF6);
            int size = id switch
            {
                0 => Size(id, FirstSectorSizes, sector, stored),
                4 => Size(id, FifthSectorSizes, sector, stored),
                13 => 2000,
                _ => SectorDataSize
            };
            if (size < 0 || GbaSectorChecksum(sector, size) != stored) continue;
            data[id] = sector[..SectorDataSize].ToArray();
            seen++;
            result.Counter = U32(sector, 0xFFC);
            if (id == 0)
                result.Game = size switch { 2192 => GbaGame.RubySapphire, 3884 => GbaGame.Emerald, 3876 => GbaGame.FireRedLeafGreen, _ => GbaGame.Unknown };
        }
        if (signed == 0) return result;
        if (seen != SectorsPerSlot) { result.State = SlotState.Partial; return result; }

        result.State = SlotState.Whole;
        result.Trainer = data[0]!;
        var storage = new byte[StorageSize];
        int copied = 0;
        for (int id = StorageFirstSector; id < SectorsPerSlot; id++)
        {
            int length = Math.Min(SectorDataSize, StorageSize - copied);
            Array.Copy(data[id]!, 0, storage, copied, length);
            copied += length;
        }
        result.Storage = storage;
        return result;
    }
}
