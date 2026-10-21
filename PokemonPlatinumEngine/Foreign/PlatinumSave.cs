using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Foreign.ForeignBytes;

namespace PokemonPlatinumEngine.Foreign;

/// <summary>
/// A save of Pokémon Platinum, Diamond or Pearl (plan 08 · P20), from <c>src/savedata.c</c>: a normal block and a boxes
/// block, each ending in a 20-byte footer of two counters, its own size, a signature, its number and a CRC-16, kept
/// twice (a primary copy and a backup), of which the game loads the newer whole one. The tables in a block are the
/// decompilation's thirty-odd structures one after another; of them only the trainer (the normal block) and the
/// <c>PCBoxes</c> (the boxes block's only one, so at its start) are read, and a block is found by its footer, which
/// says its own size, rather than by adding the structures up.
/// </summary>
public static class PlatinumSave
{
    public const int FileSize = 0x80000, SlotSize = 0x40000, FooterSize = 20;
    private const uint Signature = 0x20060623;
    private const int NormalBlock = 0, BoxesBlock = 1;
    private const int TrainerNameAt = 0x68, TrainerIdAt = 0x78, TrainerNameLength = 8;
    private const int Boxes = 18, BoxSlots = 30, BoxNameLength = 20;

    /// <summary>One block of one copy: where it begins, and what its footer says.</summary>
    private readonly record struct Block(bool Valid, int Start, int Size, uint GlobalCounter, uint BlockCounter);

    public static bool IsSizeOfASave(int length) => length == FileSize;

    public static (ForeignSave? Save, ForeignRefusal Refusal) Read(ReadOnlySpan<byte> file)
    {
        if (!IsSizeOfASave(file.Length)) return (null, ForeignRefusal.UnknownSize);

        var normal = new Block[2];
        var boxes = new Block[2];
        for (int copy = 0; copy < 2; copy++)
        {
            var slot = file.Slice(copy * SlotSize, SlotSize);
            normal[copy] = Find(slot, NormalBlock, 0);
            boxes[copy] = Find(slot, BoxesBlock, normal[copy].Valid ? normal[copy].Size : -1);
        }

        var (normalResult, currentNormal, staleNormal) = Compare(normal[0], normal[1]);
        var (boxesResult, currentBoxes, staleBoxes) = Compare(boxes[0], boxes[1]);
        if (normalResult == Result.Invalid || boxesResult == Result.Invalid) return (null, ForeignRefusal.DamagedSave);

        // What the game does with the pair it finds, as SaveData_LoadCheck has it: a block of the newer save and its counterpart
        // of the same save, or, where the two disagree (a save cut short), the older normal block and the newer boxes
        int useNormal = currentNormal, useBoxes = currentBoxes;
        if (normalResult == Result.Whole && boxesResult == Result.Whole
            && normal[currentNormal].GlobalCounter != boxes[currentBoxes].GlobalCounter)
            useNormal = staleNormal;
        else if (normalResult == Result.Partial && boxesResult == Result.Whole
            && normal[currentNormal].GlobalCounter != boxes[currentBoxes].GlobalCounter)
        {
            if (normal[currentNormal].GlobalCounter != boxes[staleBoxes].GlobalCounter) return (null, ForeignRefusal.DamagedSave);
            useBoxes = staleBoxes;
        }
        else if (normalResult == Result.Whole && boxesResult == Result.Partial
            && normal[currentNormal].GlobalCounter != boxes[currentBoxes].GlobalCounter)
            useNormal = staleNormal;
        else if (normalResult == Result.Partial && boxesResult == Result.Partial && currentNormal != currentBoxes)
            return (null, ForeignRefusal.DamagedSave);

        var normalData = file.Slice(useNormal * SlotSize + normal[useNormal].Start, normal[useNormal].Size);
        var boxData = file.Slice(useBoxes * SlotSize + boxes[useBoxes].Start, boxes[useBoxes].Size);
        if (boxData.Length < 4 + Boxes * BoxSlots * DsPokemon.BoxSize + Boxes * BoxNameLength * 2 + FooterSize
            || normalData.Length < TrainerIdAt + 4 + FooterSize)
            return (null, ForeignRefusal.DamagedSave);

        var named = new List<ForeignBox>();
        for (int box = 0; box < Boxes; box++)
        {
            var slots = new ForeignPokemon?[BoxSlots];
            for (int slot = 0; slot < BoxSlots; slot++)
                slots[slot] = DsPokemon.ReadEncrypted(boxData.Slice(4 + (box * BoxSlots + slot) * DsPokemon.BoxSize, DsPokemon.BoxSize));
            var name = new ushort[BoxNameLength];
            int nameAt = 4 + Boxes * BoxSlots * DsPokemon.BoxSize + box * BoxNameLength * 2;
            for (int i = 0; i < name.Length; i++) name[i] = U16(boxData, nameAt + i * 2);
            string text = MigrationDatabase.TextFromDs(name);
            named.Add(new ForeignBox { Name = text.Length > 0 ? text : $"BOX {box + 1}", Slots = slots });
        }

        var trainerName = new ushort[TrainerNameLength];
        for (int i = 0; i < trainerName.Length; i++) trainerName[i] = U16(normalData, TrainerNameAt + i * 2);
        return (new ForeignSave
        {
            Kind = ForeignKind.PlatinumSave,
            TrainerName = MigrationDatabase.TextFromDs(trainerName),
            TrainerId = U32(normalData, TrainerIdAt),
            Boxes = named
        }, ForeignRefusal.None);
    }

    /// <summary>
    /// Finds a copy's block by its footer: a footer is where the signature lies four bytes in from the end of its fourth word, the
    /// block it closes begins its size before the footer's end, and its CRC-16 is that of the block without the footer.
    /// </summary>
    private static Block Find(ReadOnlySpan<byte> slot, int blockId, int mustStartAt)
    {
        for (int at = 0; at + FooterSize <= slot.Length; at += 4)
        {
            if (U32(slot, at + 12) != Signature || slot[at + 16] != blockId) continue;
            uint size = U32(slot, at + 8);
            if (size < FooterSize || size > SlotSize || at + FooterSize < size) continue;
            int start = at + FooterSize - (int)size;
            if (mustStartAt >= 0 && start != mustStartAt) continue;
            if (!Crc16Matches(slot.Slice(start, (int)size - FooterSize), U16(slot, at + 18))) continue;
            return new Block(true, start, (int)size, U32(slot, at), U32(slot, at + 4));
        }
        return default;
    }

    private enum Result { Invalid, Partial, Whole }

    /// <summary><c>SaveCheckInfo_CompareCounters</c>: 1 where the first is the newer, -1 where the second, a counter that wrapped from the top to nought counting as the newer.</summary>
    private static int Compare(uint first, uint second)
    {
        if (first == 0xFFFFFFFF && second == 0) return -1;
        if (first == 0 && second == 0xFFFFFFFF) return 1;
        return first > second ? 1 : first < second ? -1 : 0;
    }

    /// <summary><c>SaveCheckInfo_CompareSectors</c>: which copy (0 the primary, 1 the backup) is the current and which the stale.</summary>
    private static (Result Result, int Current, int Stale) Compare(Block primary, Block backup)
    {
        if (primary.Valid && backup.Valid)
        {
            int global = Compare(primary.GlobalCounter, backup.GlobalCounter);
            int block = Compare(primary.BlockCounter, backup.BlockCounter);
            int primaryNewer = global != 0 ? global : block;
            return primaryNewer < 0 ? (Result.Whole, 1, 0) : (Result.Whole, 0, 1);
        }
        if (primary.Valid) return (Result.Partial, 0, -1);
        if (backup.Valid) return (Result.Partial, 1, -1);
        return (Result.Invalid, -1, -1);
    }
}
