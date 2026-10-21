using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Pokémon moved from one of this game's saves to another (plan 08 · P22; <c>docs/mechanics/rulings.md</c>, "Between this
/// game's saves"). A move is four writes, each of which <see cref="SaveManager.SaveGame"/> makes whole or not at all, with a
/// <see cref="MoveReceipt"/> in both saves between the first and the fourth:
/// <list type="number">
/// <item>the receiving save, with the six waiting in its park and the receipt;</item>
/// <item>the source, without them and with the receipt;</item>
/// <item>the receiving save, the receipt closed;</item>
/// <item>the source, the receipt closed.</item>
/// </list>
/// The game starts by <see cref="Resolve"/>, which finishes any move a crash cut short, so each Pokémon is in exactly one
/// save whenever a game is loaded: after the first write both saves hold it and the start takes it out of the source;
/// after the second the source has let it go and the start closes the receipts; and so on. A receiving save that is gone
/// gives the Pokémon back to the source. A Pokémon is found by its own id (<see cref="Pokemon.Id"/>). Only slots take part:
/// a save in the <c>migrate</c> folder is never written (it is a source that gives copies, kept to one coming by the
/// receiving park's ledger). No drawing and no input.
/// </summary>
public static class SlotMoves
{
    /// <summary>Every Pokémon in a save's boxes, with the box and place it is in.</summary>
    public static IEnumerable<(int Box, SavedBoxedPokemon Place)> Boxed(SaveData save) =>
        save.Boxes == null ? Enumerable.Empty<(int, SavedBoxedPokemon)>()
            : save.Boxes.Boxes.SelectMany((box, i) => box.Pokemon.Select(p => (i, p)));

    /// <summary>The ids of every Pokémon a save has, wherever it keeps them: the team, the boxes, the park, the Day Care.</summary>
    public static HashSet<Guid> Held(SaveData save)
    {
        var ids = new HashSet<Guid>();
        void Add(SavedPokemonData? p) { if (p?.Id is { } id) ids.Add(id); }
        foreach (var p in save.Party) Add(p);
        foreach (var (_, place) in Boxed(save)) Add(place.Pokemon);
        foreach (var p in save.Pastimes?.Waiting ?? new()) Add(p);
        foreach (var p in save.BoxStorage) Add(p);
        if (save.DayCare != null)
            foreach (var place in save.DayCare.Places) Add(place?.Pokemon);
        return ids;
    }

    /// <summary>The name of a move: the same in both saves, whoever asks, from the slots and the Pokémon.</summary>
    public static string ReceiptId(int from, int to, IEnumerable<Guid> ids)
    {
        string text = $"{from}>{to}:" + string.Join(",", ids.OrderBy(i => i).Select(i => i.ToString("N")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..32];
    }

    private static bool Has(SaveData save, string receipt) => save.Receipts?.Any(r => r.Id == receipt) == true;

    private static void Close(SaveData save, string receipt)
    {
        save.Receipts?.RemoveAll(r => r.Id == receipt);
        if (save.Receipts is { Count: 0 }) save.Receipts = null;
    }

    private static void TakeOut(SaveData save, IEnumerable<Guid> ids)
    {
        if (save.Boxes == null) return;
        var set = ids.ToHashSet();
        foreach (var box in save.Boxes.Boxes) box.Pokemon.RemoveAll(p => p.Pokemon.Id is { } id && set.Contains(id));
    }

    /// <summary>Puts a Pokémon back in the first free place of a save's boxes; false where there is none.</summary>
    private static bool PutBack(SaveData save, SavedPokemonData pokemon)
    {
        if (save.Boxes == null) return false;
        for (int b = 0; b < save.Boxes.Boxes.Count; b++)
        {
            var box = save.Boxes.Boxes[b];
            var taken = box.Pokemon.Select(p => p.Slot).ToHashSet();
            for (int slot = 0; slot < PcBoxes.BoxSize; slot++)
                if (!taken.Contains(slot))
                {
                    box.Pokemon.Add(new SavedBoxedPokemon { Slot = slot, Pokemon = pokemon });
                    return true;
                }
        }
        return false;
    }

    // ------------------------------------------------------------------ a move

    /// <summary>
    /// Moves six Pokémon of the boxes of slot <paramref name="from"/> into the park of slot <paramref name="to"/>. Returns
    /// what stopped it, <see cref="MigrationBlock.None"/> when it was done; nothing is changed unless the first write was
    /// made, and from there a move cut short is finished by the next start. <paramref name="stopAfter"/> is for the tests:
    /// asked after each write (1 to 4) whether the game "crashes" there.
    /// </summary>
    public static MigrationBlock Move(SavePaths paths, int from, int to, IReadOnlyList<Guid> ids, Func<int, bool>? stopAfter = null)
    {
        Resolve(paths);
        var source = SaveSlots.Read(paths, from);
        var target = SaveSlots.Read(paths, to);
        if (from == to || source.Outcome != SaveLoad.Ok || target.Outcome != SaveLoad.Ok || source.Save == null || target.Save == null)
            return MigrationBlock.CouldNotSave;
        var (s, t) = (source.Save, target.Save);

        if (!t.NationalPokedex) return MigrationBlock.NeedsNationalPokedex;
        if ((t.Pastimes?.Waiting?.Count ?? 0) > 0) return MigrationBlock.AlreadyStocked;
        if (ids.Count != Migration.Size || ids.Distinct().Count() != Migration.Size) return MigrationBlock.NotSix;
        var boxed = Boxed(s).Select(b => b.Place.Pokemon).ToList();
        var six = ids.Select(id => boxed.FirstOrDefault(p => p.Id == id)).ToList();
        var heldByTarget = Held(t);
        if (six.Any(p => p == null || !Migration.WhyNotOwn(p, heldByTarget).Allowed)) return MigrationBlock.Refused;

        var receipt = new MoveReceipt
        {
            Id = ReceiptId(from, to, ids), From = from, To = to,
            SourceChecksum = SaveManager.ChecksumOfFile(paths.Slot(from)),
            Pokemon = six.Select(p => p!).ToList()
        };

        // 1: the receiving save has them, and the receipt
        t.Pastimes ??= new PastimesSave();
        t.Pastimes.Waiting = receipt.Pokemon.ToList();
        (t.Receipts ??= new()).Add(receipt);
        if (!SaveSlots.Write(paths, to, t)) return MigrationBlock.CouldNotSave;
        if (stopAfter?.Invoke(1) == true) return MigrationBlock.CouldNotSave;

        // 2: the source lets them go, with the receipt
        TakeOut(s, ids);
        (s.Receipts ??= new()).Add(receipt);
        if (!SaveSlots.Write(paths, from, s)) return MigrationBlock.CouldNotSave;
        if (stopAfter?.Invoke(2) == true) return MigrationBlock.CouldNotSave;

        // 3 and 4: the receipt closed in the receiving save and then in the source
        Close(t, receipt.Id);
        if (!SaveSlots.Write(paths, to, t)) return MigrationBlock.CouldNotSave;
        if (stopAfter?.Invoke(3) == true) return MigrationBlock.CouldNotSave;
        Close(s, receipt.Id);
        if (!SaveSlots.Write(paths, from, s)) return MigrationBlock.CouldNotSave;
        return MigrationBlock.None;
    }

    // ------------------------------------------------------------------ a move finished

    /// <summary>
    /// Finishes every move that was cut short, as the game starts (before it reads the slots for the title): for each open
    /// receipt in any slot, the source lets go of the Pokémon if it still holds them, and the receipts are closed, the
    /// receiving save's first. With the receiving save gone altogether (its slot deleted) the Pokémon go back to the source.
    /// Returns how many moves it finished.
    /// </summary>
    public static int Resolve(SavePaths paths)
    {
        var open = new Dictionary<string, MoveReceipt>();
        for (int slot = 1; slot <= SavePaths.Slots; slot++)
            if (SaveSlots.Read(paths, slot).Save?.Receipts is { } receipts)
                foreach (var r in receipts) open.TryAdd(r.Id, r);
        int finished = 0;
        foreach (var receipt in open.Values)
            if (Finish(paths, receipt)) finished++;
        return finished;
    }

    private static bool Finish(SavePaths paths, MoveReceipt receipt)
    {
        var from = SaveSlots.Read(paths, receipt.From);
        var to = SaveSlots.Read(paths, receipt.To);
        var ids = receipt.Pokemon.Where(p => p.Id != null).Select(p => p.Id!.Value).ToList();

        // The receiving save is gone, or is another game that took its slot (it has neither the receipt nor a single one of the
        // Pokémon): nothing was left to take them, so they go back where they were
        bool targetGone = to.Outcome == SaveLoad.None
            || (to.Save != null && !Has(to.Save, receipt.Id) && !ids.Any(Held(to.Save).Contains));
        if (targetGone && from.Save != null)
        {
            foreach (var pokemon in receipt.Pokemon)
                if (pokemon.Id is { } id && !Held(from.Save).Contains(id) && !PutBack(from.Save, pokemon))
                    Log.Warn($"A move of Pokémon was undone but slot {receipt.From} has no room for {pokemon.SpeciesName}.");
            Close(from.Save, receipt.Id);
            SaveSlots.Write(paths, receipt.From, from.Save);
            Log.Info($"A move of Pokémon to slot {receipt.To} was undone: that save is gone or is another game.");
            return true;
        }
        // Either save damaged: leave the receipt open until it can be read
        if (to.Save == null) return false;

        // The source lets go of them, with the receipt (the second write)
        if (from.Save != null)
        {
            bool changed = false;
            if (Boxed(from.Save).Any(b => b.Place.Pokemon.Id is { } id && ids.Contains(id))) { TakeOut(from.Save, ids); changed = true; }
            if (Has(to.Save, receipt.Id) && !Has(from.Save, receipt.Id)) { (from.Save.Receipts ??= new()).Add(receipt); changed = true; }
            if (changed && !SaveSlots.Write(paths, receipt.From, from.Save)) return false;
        }
        // The receiving save's receipt closed, and then the source's
        if (Has(to.Save, receipt.Id))
        {
            Close(to.Save, receipt.Id);
            if (!SaveSlots.Write(paths, receipt.To, to.Save)) return false;
        }
        if (from.Save != null && Has(from.Save, receipt.Id))
        {
            Close(from.Save, receipt.Id);
            if (!SaveSlots.Write(paths, receipt.From, from.Save)) return false;
        }
        Log.Info($"A move of Pokémon from slot {receipt.From} to slot {receipt.To} that was cut short was finished.");
        return true;
    }
}
