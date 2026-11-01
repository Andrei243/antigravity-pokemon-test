using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>Why a trade or a move to or from the Home box can't be made (plan 12 · Q28, Q29); None when it can.</summary>
public enum HomeRefusal
{
    None,
    /// <summary>A save that can't be read whole, or a slot with nothing in it.</summary>
    Unreadable,
    /// <summary>A save a newer game wrote (its <see cref="SaveData.SaveVersion"/> is past today's).</summary>
    NewerSave,
    /// <summary>The Home box's file was written under other data (<see cref="GameDataFiles.Version"/>).</summary>
    OtherData,
    /// <summary>The Home box's file and its backup are both damaged.</summary>
    HomeDamaged,
    /// <summary>A battle is suspended beside the slot (plan 12 · Q25): its Pokémon wait for it.</summary>
    Suspended,
    SameSlot,
    Egg,
    /// <summary>A Pokémon with no id of its own (plan 07 · O2), which nothing could keep from being copied.</summary>
    NoId,
    /// <summary>The other side holds a Pokémon of this id already: it would be a copy.</summary>
    AlreadyThere,
    /// <summary>A Nuzlocke or a challenge's restrictions under way (plan 13): nothing leaves the run and nothing comes in.</summary>
    RunUnderWay,
    /// <summary>The team would be left with nobody able to battle.</summary>
    LastOnTeam,
    /// <summary>No room in the team or the boxes, or in the Home box.</summary>
    NoRoom,
    /// <summary>The save's region may not take it (plan 18's <see cref="Crossing.CanTake"/>).</summary>
    CannotCross,
    /// <summary>One of the files could not be written; nothing was moved.</summary>
    CouldNotSave,
    NotFound
}

/// <summary>
/// Trades between this game's saves and moves to and from the Home box (plan 12 · Q28, Q29; <c>docs/mechanics/rulings.md</c>,
/// "Trades between saves and the Home box"). Neither loads a game: the slots' files are read with
/// <see cref="SaveSlots.Read(SavePaths, int)"/> and written with Q1's writer, and the Home box's with <see cref="HomeBank.Save"/>.
/// Each is four writes with a <see cref="MoveReceipt"/> in both files between the first and the fourth, as plan 08 · P22's
/// moves of six are (<see cref="SlotMoves"/>):
/// <list type="bullet">
/// <item>a trade: the first save with its Pokémon swapped and the receipt, the second the same, then the receipt closed in
/// the first and last in the second;</item>
/// <item>a move to or from the Home box: the side that takes it, with it and the receipt; the side that gives it, without
/// it and with the receipt; then the receipt closed in the taker and last in the giver.</item>
/// </list>
/// <see cref="Resolve"/>, run as the game starts beside <see cref="SlotMoves.Resolve"/>, finishes any of them that a crash
/// cut short, so that each Pokémon is in exactly one place whenever a game is loaded. A Pokémon is found by its own id
/// (<see cref="Pokemon.Id"/>). No drawing and no input.
/// </summary>
public static class HomeMoves
{
    /// <summary>The number a Home box receipt gives the Home box in place of a slot.</summary>
    public const int HomeSide = 0;

    // ------------------------------------------------------------------ the rules

    /// <summary>
    /// Whether a save's run holds its Pokémon (plan 13): a Nuzlocke or a challenge's restrictions under way, which neither
    /// lets a Pokémon out nor takes one in. Ended (a Nuzlocke's whiteout) or finished (its Hall of Fame entered), it holds none.
    /// </summary>
    public static bool RunHolds(SaveData save) =>
        save.Run is { } run && (run.Nuzlocke || BattleRetry.Restricts(run.BattleTerms()))
        && save.RunState?.Ended != true && save.HallOfFame.Count == 0;

    /// <summary>Whether a save's run randomises (plan 13 · V6): its Pokémon carry the mark into the Home box.</summary>
    public static bool Randomised(SaveData save) => save.Run?.Random != null;

    /// <summary>
    /// A save made ready to take part: one a newer game wrote is refused, an older one brought up to today's records in
    /// memory (its Pokémon given their ids, plan 07 · O2), which the move then writes.
    /// </summary>
    public static HomeRefusal Ready(SaveData? save)
    {
        if (save == null) return HomeRefusal.Unreadable;
        if (save.SaveVersion > SaveData.CurrentSave) return HomeRefusal.NewerSave;
        SaveMigration.Upgrade(save);
        return HomeRefusal.None;
    }

    /// <summary>Every Pokémon a save keeps where the Home screen shows them: the team, then the boxes box by box, place by place.</summary>
    public static IEnumerable<SavedPokemonData> Shown(SaveData save)
    {
        foreach (var p in save.Party) yield return p;
        if (save.Boxes != null)
            foreach (var box in save.Boxes.Boxes)
                foreach (var boxed in box.Pokemon.OrderBy(b => b.Slot)) yield return boxed.Pokemon;
    }

    /// <summary>Whether a Pokémon is on the save's team (not in a box).</summary>
    public static bool OnTeam(SaveData save, SavedPokemonData pokemon) => save.Party.Contains(pokemon);

    private static SavedPokemonData? Find(SaveData save, Guid id) => Shown(save).FirstOrDefault(p => p.Id == id);

    private static bool Able(SavedPokemonData p) => !p.IsEgg && p.CurrentHP > 0;

    /// <summary>Whether the team is left with somebody able to battle once these leave and that one comes in.</summary>
    private static bool TeamStands(SaveData save, Guid leaving, SavedPokemonData? coming) =>
        save.Party.Any(p => p.Id != leaving && Able(p)) || (coming != null && Able(coming) && HasRoomOnTeam(save, leaving));

    private static bool HasRoomOnTeam(SaveData save, Guid? leaving) =>
        save.Party.Count(p => leaving == null || p.Id != leaving) < (save.Run?.PartyLimit ?? Party.MaxSize);

    private static bool HasBoxRoom(SaveData save) =>
        save.Boxes != null && save.Boxes.Boxes.Any(b => b.Pokemon.Count < PcBoxes.BoxSize);

    /// <summary>Why a Pokémon of a save may not leave it (for a trade or the Home box), or None.</summary>
    public static HomeRefusal WhyNotLeave(SaveData save, SavedPokemonData pokemon, bool trade)
    {
        if (pokemon.IsEgg && trade) return HomeRefusal.Egg;
        if (pokemon.Id is not { } id) return HomeRefusal.NoId;
        if (RunHolds(save)) return HomeRefusal.RunUnderWay;
        if (!trade && OnTeam(save, pokemon) && !TeamStands(save, id, null)) return HomeRefusal.LastOnTeam;
        return HomeRefusal.None;
    }

    /// <summary>Why a Pokémon may not come into a save (after <paramref name="leaving"/> has left it, in a trade), or None.</summary>
    public static HomeRefusal WhyNotArrive(SaveData save, SavedPokemonData pokemon, Guid? leaving = null)
    {
        if (pokemon.Id is not { } id) return HomeRefusal.NoId;
        if (RunHolds(save)) return HomeRefusal.RunUnderWay;
        if (SlotMoves.Held(save).Contains(id) && id != leaving) return HomeRefusal.AlreadyThere;
        if (!HasRoomOnTeam(save, leaving) && !HasBoxRoom(save)) return HomeRefusal.NoRoom;
        // A species of a region the save hasn't reached comes as plan 18 rules: under option B every one may
        var made = pokemon.ToPokemon();
        var region = RegionDatabase.Get(save.RegionId) ?? RegionDatabase.First;
        if (!Crossing.CanTake(made, region, new StoryState())) return HomeRefusal.CannotCross;
        return HomeRefusal.None;
    }

    /// <summary>Why two Pokémon of two saves may not be traded for each other, or None.</summary>
    public static HomeRefusal WhyNotTrade(SaveData first, SavedPokemonData mine, SaveData second, SavedPokemonData theirs)
    {
        var why = WhyNotLeave(first, mine, trade: true);
        if (why == HomeRefusal.None) why = WhyNotLeave(second, theirs, trade: true);
        if (why == HomeRefusal.None) why = WhyNotArrive(second, mine, theirs.Id);
        if (why == HomeRefusal.None) why = WhyNotArrive(first, theirs, mine.Id);
        if (why == HomeRefusal.None && (!TeamStands(first, mine.Id!.Value, theirs) || !TeamStands(second, theirs.Id!.Value, mine))) why = HomeRefusal.LastOnTeam;
        return why;
    }

    /// <summary>Why a Pokémon of a save may not go into the Home box, or None.</summary>
    public static HomeRefusal WhyNotDeposit(SaveData save, SavedPokemonData pokemon, HomeBank bank)
    {
        var why = WhyNotLeave(save, pokemon, trade: false);
        if (why != HomeRefusal.None) return why;
        if (bank.Holds(pokemon.Id!.Value)) return HomeRefusal.AlreadyThere;
        if (bank.IsFull) return HomeRefusal.NoRoom;
        return HomeRefusal.None;
    }

    /// <summary>Why a Pokémon of the Home box may not come into a save, or None.</summary>
    public static HomeRefusal WhyNotWithdraw(SaveData save, BankedPokemon banked) => WhyNotArrive(save, banked.Pokemon);

    // ------------------------------------------------------------------ a save changed

    private static bool Remove(SaveData save, Guid id)
    {
        int gone = save.Party.RemoveAll(p => p.Id == id);
        if (save.Boxes != null)
            foreach (var box in save.Boxes.Boxes) gone += box.Pokemon.RemoveAll(b => b.Pokemon.Id == id);
        return gone > 0;
    }

    /// <summary>Puts a Pokémon into a save: its team where there is room (and the run lets it carry it), its first free box place otherwise.</summary>
    private static bool PutIn(SaveData save, SavedPokemonData pokemon)
    {
        if (HasRoomOnTeam(save, null) && (save.Run == null || save.Run.MayCarry(pokemon.ToPokemon(), null)))
        {
            save.Party.Add(pokemon);
            return true;
        }
        if (save.Boxes == null) return false;
        foreach (var box in save.Boxes.Boxes)
        {
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

    /// <summary>The player of a save, as a Pokémon's original trainer names them.</summary>
    public static TrainerMark PlayerOf(SaveData save) => new(save.PlayerName, save.TrainerId, save.Look);

    /// <summary>
    /// A Pokémon as it arrives by trade from <paramref name="giver"/> in <paramref name="taker"/>: a copy, whose original
    /// trainer is the giving save's player if it had none, and none again where that is the taker's own (traded back,
    /// it is its first player's own: <see cref="TrainerMark.Is"/>).
    /// </summary>
    public static SavedPokemonData Arriving(SavedPokemonData pokemon, SaveData giver, SaveData taker)
    {
        var copy = Copy(pokemon);
        copy.OriginalTrainer ??= PlayerOf(giver);
        var me = PlayerOf(taker);
        if (copy.OriginalTrainer is { } ot && ot.Is(me.Name, me.Id, me.Look)) copy.OriginalTrainer = null;
        return copy;
    }

    private static SavedPokemonData Copy(SavedPokemonData p) =>
        System.Text.Json.JsonSerializer.Deserialize<SavedPokemonData>(System.Text.Json.JsonSerializer.Serialize(p))!;

    private static bool Has(List<MoveReceipt>? receipts, string id) => receipts?.Any(r => r.Id == id) == true;

    private static void Close(SaveData save, string id)
    {
        save.Receipts?.RemoveAll(r => r.Id == id);
        if (save.Receipts is { Count: 0 }) save.Receipts = null;
    }

    private static void Close(HomeBank bank, string id)
    {
        bank.Receipts?.RemoveAll(r => r.Id == id);
        if (bank.Receipts is { Count: 0 }) bank.Receipts = null;
    }

    /// <summary>The name of a move or a trade: the same in both files, from its kind, its sides and its Pokémon.</summary>
    public static string ReceiptId(ReceiptKind kind, int from, int to, IEnumerable<Guid> ids)
    {
        string text = $"{kind}:{from}>{to}:" + string.Join(",", ids.Select(i => i.ToString("N")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..32];
    }

    // A save's side of a trade: the given one out, the one that arrives in, listed to be taken up as the save is continued
    private static void Swap(SaveData save, Guid giving, SavedPokemonData arriving, string? forSpecies)
    {
        Remove(save, giving);
        PutIn(save, arriving);
        (save.ArrivedByTrade ??= new()).RemoveAll(a => a.Id == arriving.Id);
        save.ArrivedByTrade.Add(new TradeArrival { Id = arriving.Id!.Value, For = forSpecies });
    }

    // A trade undone on the save that had made its side: its own Pokémon back where the other was
    private static void Unswap(SaveData save, SavedPokemonData own, Guid arrived)
    {
        Remove(save, arrived);
        save.ArrivedByTrade?.RemoveAll(a => a.Id == arrived);
        if (save.ArrivedByTrade is { Count: 0 }) save.ArrivedByTrade = null;
        if (own.Id is { } id && !SlotMoves.Held(save).Contains(id)) PutIn(save, own);
    }

    // ------------------------------------------------------------------ a trade

    /// <summary>
    /// Trades a Pokémon of slot <paramref name="first"/> for one of slot <paramref name="second"/>. Returns what stopped
    /// it, <see cref="HomeRefusal.None"/> when it was done; nothing is changed unless the first write was made, and from
    /// there a trade cut short is finished by the next start (<see cref="Resolve"/>). <paramref name="stopAfter"/> is the
    /// tests': asked after each write (1 to 4) whether the game "crashes" there.
    /// </summary>
    public static HomeRefusal Trade(SavePaths paths, int first, Guid mine, int second, Guid theirs, Func<int, bool>? stopAfter = null)
    {
        Resolve(paths);
        if (first == second) return HomeRefusal.SameSlot;
        if (BattleSuspension.Exists(paths, first) || BattleSuspension.Exists(paths, second)) return HomeRefusal.Suspended;
        var a = SaveSlots.Read(paths, first);
        var b = SaveSlots.Read(paths, second);
        if (a.Outcome != SaveLoad.Ok || b.Outcome != SaveLoad.Ok) return HomeRefusal.Unreadable;
        var why = Ready(a.Save);
        if (why == HomeRefusal.None) why = Ready(b.Save);
        if (why != HomeRefusal.None) return why;
        var (sa, sb) = (a.Save!, b.Save!);
        var pa = Find(sa, mine);
        var pb = Find(sb, theirs);
        if (pa == null || pb == null) return HomeRefusal.NotFound;
        why = WhyNotTrade(sa, pa, sb, pb);
        if (why != HomeRefusal.None) return why;

        var receipt = new MoveReceipt
        {
            Id = ReceiptId(ReceiptKind.Trade, first, second, new[] { mine, theirs }), Kind = ReceiptKind.Trade,
            From = first, To = second, SourceChecksum = SaveManager.ChecksumOfFile(paths.Slot(first)),
            Pokemon = new() { Copy(pa) }, Back = new() { Copy(pb) }
        };

        // 1: the first save with its Pokémon swapped, and the receipt
        Swap(sa, mine, Arriving(pb, sb, sa), pa.SpeciesName);
        (sa.Receipts ??= new()).Add(receipt);
        if (!SaveSlots.Write(paths, first, sa)) return HomeRefusal.CouldNotSave;
        AutosaveFile.ClearFor(paths, first);
        if (stopAfter?.Invoke(1) == true) return HomeRefusal.CouldNotSave;

        // 2: the second the same
        Swap(sb, theirs, Arriving(pa, sa, sb), pb.SpeciesName);
        (sb.Receipts ??= new()).Add(receipt);
        if (!SaveSlots.Write(paths, second, sb)) return HomeRefusal.CouldNotSave;
        AutosaveFile.ClearFor(paths, second);
        if (stopAfter?.Invoke(2) == true) return HomeRefusal.CouldNotSave;

        // 3 and 4: the receipt closed in the first and then in the second
        Close(sa, receipt.Id);
        if (!SaveSlots.Write(paths, first, sa)) return HomeRefusal.CouldNotSave;
        if (stopAfter?.Invoke(3) == true) return HomeRefusal.CouldNotSave;
        Close(sb, receipt.Id);
        if (!SaveSlots.Write(paths, second, sb)) return HomeRefusal.CouldNotSave;
        Log.Info($"A {pa.SpeciesName} of slot {first} was traded for a {pb.SpeciesName} of slot {second}.");
        return HomeRefusal.None;
    }

    // ------------------------------------------------------------------ the Home box

    /// <summary>Reads the Home box and says why nothing may be moved, if so.</summary>
    public static (HomeRefusal Why, HomeBank? Bank) ReadBank(SavePaths paths)
    {
        var (outcome, bank) = HomeBank.Load(paths.HomeBox);
        return outcome switch
        {
            BankLoad.OtherData => (HomeRefusal.OtherData, null),
            BankLoad.Damaged => (HomeRefusal.HomeDamaged, null),
            _ => (HomeRefusal.None, bank)
        };
    }

    /// <summary>
    /// Puts a Pokémon of slot <paramref name="slot"/> into the Home box: the Home box written with it and the receipt, the
    /// save without it and with the receipt, then the receipt closed in the Home box and last in the save.
    /// </summary>
    public static HomeRefusal Deposit(SavePaths paths, int slot, Guid id, Func<int, bool>? stopAfter = null)
    {
        Resolve(paths);
        if (BattleSuspension.Exists(paths, slot)) return HomeRefusal.Suspended;
        var read = SaveSlots.Read(paths, slot);
        if (read.Outcome != SaveLoad.Ok) return HomeRefusal.Unreadable;
        var why = Ready(read.Save);
        if (why != HomeRefusal.None) return why;
        var (bankWhy, bank) = ReadBank(paths);
        if (bankWhy != HomeRefusal.None) return bankWhy;
        var save = read.Save!;
        if (Find(save, id) is not { } pokemon) return HomeRefusal.NotFound;
        why = WhyNotDeposit(save, pokemon, bank!);
        if (why != HomeRefusal.None) return why;

        var receipt = new MoveReceipt
        {
            Id = ReceiptId(ReceiptKind.Home, slot, HomeSide, new[] { id }), Kind = ReceiptKind.Home,
            From = slot, To = HomeSide, SourceChecksum = SaveManager.ChecksumOfFile(paths.Slot(slot)), Pokemon = new() { Copy(pokemon) }
        };
        // 1: the Home box takes it, with the receipt
        bank!.Put(Copy(pokemon), Randomised(save), save.PlayerName);
        (bank.Receipts ??= new()).Add(receipt);
        if (!HomeBank.Save(bank, paths.HomeBox)) return HomeRefusal.CouldNotSave;
        if (stopAfter?.Invoke(1) == true) return HomeRefusal.CouldNotSave;
        // 2: the save lets it go, with the receipt
        Remove(save, id);
        (save.Receipts ??= new()).Add(receipt);
        if (!SaveSlots.Write(paths, slot, save)) return HomeRefusal.CouldNotSave;
        AutosaveFile.ClearFor(paths, slot);
        if (stopAfter?.Invoke(2) == true) return HomeRefusal.CouldNotSave;
        // 3 and 4: closed in the Home box, then in the save
        Close(bank, receipt.Id);
        if (!HomeBank.Save(bank, paths.HomeBox)) return HomeRefusal.CouldNotSave;
        if (stopAfter?.Invoke(3) == true) return HomeRefusal.CouldNotSave;
        Close(save, receipt.Id);
        if (!SaveSlots.Write(paths, slot, save)) return HomeRefusal.CouldNotSave;
        Log.Info($"A {pokemon.SpeciesName} of slot {slot} was put in the Home box.");
        return HomeRefusal.None;
    }

    /// <summary>
    /// Takes a Pokémon of the Home box into slot <paramref name="slot"/>: the save written with it and the receipt, the Home
    /// box without it and with the receipt, then the receipt closed in the save and last in the Home box.
    /// </summary>
    public static HomeRefusal Withdraw(SavePaths paths, int slot, Guid id, Func<int, bool>? stopAfter = null)
    {
        Resolve(paths);
        if (BattleSuspension.Exists(paths, slot)) return HomeRefusal.Suspended;
        var read = SaveSlots.Read(paths, slot);
        if (read.Outcome != SaveLoad.Ok) return HomeRefusal.Unreadable;
        var why = Ready(read.Save);
        if (why != HomeRefusal.None) return why;
        var (bankWhy, bank) = ReadBank(paths);
        if (bankWhy != HomeRefusal.None) return bankWhy;
        var save = read.Save!;
        var banked = bank!.Pokemon.FirstOrDefault(p => p.Pokemon.Id == id);
        if (banked == null) return HomeRefusal.NotFound;
        why = WhyNotWithdraw(save, banked);
        if (why != HomeRefusal.None) return why;

        var receipt = new MoveReceipt
        {
            Id = ReceiptId(ReceiptKind.Home, HomeSide, slot, new[] { id }), Kind = ReceiptKind.Home,
            From = HomeSide, To = slot, Pokemon = new() { Copy(banked.Pokemon) }
        };
        // 1: the save takes it, with the receipt
        PutIn(save, Copy(banked.Pokemon));
        (save.Receipts ??= new()).Add(receipt);
        if (!SaveSlots.Write(paths, slot, save)) return HomeRefusal.CouldNotSave;
        AutosaveFile.ClearFor(paths, slot);
        if (stopAfter?.Invoke(1) == true) return HomeRefusal.CouldNotSave;
        // 2: the Home box lets it go, with the receipt
        bank.Take(id);
        (bank.Receipts ??= new()).Add(receipt);
        if (!HomeBank.Save(bank, paths.HomeBox)) return HomeRefusal.CouldNotSave;
        if (stopAfter?.Invoke(2) == true) return HomeRefusal.CouldNotSave;
        // 3 and 4: closed in the save, then in the Home box
        Close(save, receipt.Id);
        if (!SaveSlots.Write(paths, slot, save)) return HomeRefusal.CouldNotSave;
        if (stopAfter?.Invoke(3) == true) return HomeRefusal.CouldNotSave;
        Close(bank, receipt.Id);
        if (!HomeBank.Save(bank, paths.HomeBox)) return HomeRefusal.CouldNotSave;
        Log.Info($"A {banked.Pokemon.SpeciesName} of the Home box was taken into slot {slot}.");
        return HomeRefusal.None;
    }

    // ------------------------------------------------------------------ a crash's move finished

    /// <summary>
    /// Finishes every trade and every move to or from the Home box that a crash cut short, as the game starts (before the
    /// slots are read for the title, beside <see cref="SlotMoves.Resolve"/>), and before any new one. A trade whose
    /// second save is gone or is another game is undone in the first; a move whose taker is gone gives the Pokémon back
    /// to the giver. A file that can't be read whole leaves its receipt open until it can. Returns how many were finished.
    /// </summary>
    public static int Resolve(SavePaths paths)
    {
        var open = new Dictionary<string, MoveReceipt>();
        for (int slot = 1; slot <= SavePaths.Slots; slot++)
            if (SaveSlots.Read(paths, slot).Save?.Receipts is { } receipts)
                foreach (var r in receipts.Where(r => r.Kind != ReceiptKind.Move)) open.TryAdd(r.Id, r);
        var (_, bank) = HomeBank.Load(paths.HomeBox);
        foreach (var r in bank?.Receipts ?? new()) open.TryAdd(r.Id, r);

        int finished = 0;
        foreach (var receipt in open.Values)
            if (receipt.Kind == ReceiptKind.Trade ? FinishTrade(paths, receipt) : FinishHome(paths, receipt)) finished++;
        if (finished > 0) Log.Info($"{finished} trade(s) or move(s) to or from the Home box, cut short by the last run, were finished or undone.");
        return finished;
    }

    private static bool FinishTrade(SavePaths paths, MoveReceipt receipt)
    {
        var a = SaveSlots.Read(paths, receipt.From);
        var b = SaveSlots.Read(paths, receipt.To);
        if (a.Save == null || receipt.Pokemon.Count != 1 || receipt.Back is not { Count: 1 }) return false;
        var (pa, pb) = (receipt.Pokemon[0], receipt.Back[0]);
        var (ida, idb) = (pa.Id!.Value, pb.Id!.Value);
        bool aHas = Has(a.Save.Receipts, receipt.Id), bHas = b.Save != null && Has(b.Save.Receipts, receipt.Id);

        if (aHas && !bHas)
        {
            // Only the first save was written: the second still holds its own, or is gone or another game
            if (b.Save != null && SlotMoves.Held(b.Save).Contains(idb) && !SlotMoves.Held(b.Save).Contains(ida))
            {
                Swap(b.Save, idb, Arriving(pa, a.Save, b.Save), pb.SpeciesName);
                (b.Save.Receipts ??= new()).Add(receipt);
                if (!SaveSlots.Write(paths, receipt.To, b.Save)) return false;
                AutosaveFile.ClearFor(paths, receipt.To);
                bHas = true;
            }
            else if (b.Outcome == SaveLoad.Damaged) return false;
            else
            {
                Unswap(a.Save, pa, idb);
                Close(a.Save, receipt.Id);
                if (!SaveSlots.Write(paths, receipt.From, a.Save)) return false;
                Log.Info($"A trade with slot {receipt.To} was undone: that save is gone or is another game.");
                return true;
            }
        }
        if (aHas)
        {
            Close(a.Save, receipt.Id);
            if (!SaveSlots.Write(paths, receipt.From, a.Save)) return false;
        }
        if (bHas && b.Save != null)
        {
            Close(b.Save, receipt.Id);
            if (!SaveSlots.Write(paths, receipt.To, b.Save)) return false;
        }
        Log.Info($"A trade between slots {receipt.From} and {receipt.To} that was cut short was finished.");
        return true;
    }

    // One side of a move to or from the Home box, a slot's save or the Home box itself
    private sealed class Side
    {
        public SaveData? Save;
        public HomeBank? Bank;
        public bool Gone;
        public bool Damaged;
        public int Slot;

        public bool Has(string id) => Save != null ? HomeMoves.Has(Save.Receipts, id) : Bank != null && HomeMoves.Has(Bank.Receipts, id);
        public bool Holds(Guid id) => Save != null ? SlotMoves.Held(Save).Contains(id) : Bank != null && Bank.Holds(id);
        public void Close(string id) { if (Save != null) HomeMoves.Close(Save, id); else if (Bank != null) HomeMoves.Close(Bank, id); }
        public void TakeOut(Guid id) { if (Save != null) Remove(Save, id); else Bank?.Take(id); }
        public bool PutBack(SavedPokemonData p) => Save != null ? PutIn(Save, Copy(p)) : Bank != null && Bank.Put(Copy(p), false, null);
        public void Add(MoveReceipt r) { if (Save != null) (Save.Receipts ??= new()).Add(r); else if (Bank != null) (Bank.Receipts ??= new()).Add(r); }
        public bool Write(SavePaths paths) => Save != null ? SaveSlots.Write(paths, Slot, Save) : Bank != null && HomeBank.Save(Bank, paths.HomeBox);
    }

    private static Side SideOf(SavePaths paths, int slot)
    {
        if (slot == HomeSide)
        {
            var (outcome, bank) = HomeBank.Load(paths.HomeBox);
            return new Side { Bank = bank, Slot = slot, Gone = outcome == BankLoad.None, Damaged = bank == null };
        }
        var read = SaveSlots.Read(paths, slot);
        return new Side { Save = read.Save, Slot = slot, Gone = read.Outcome == SaveLoad.None, Damaged = read.Save == null && read.Outcome != SaveLoad.None };
    }

    private static bool FinishHome(SavePaths paths, MoveReceipt receipt)
    {
        var giver = SideOf(paths, receipt.From);
        var taker = SideOf(paths, receipt.To);
        var ids = receipt.Pokemon.Where(p => p.Id != null).Select(p => p.Id!.Value).ToList();
        // The giver is gone altogether (its slot deleted): the taker keeps what it took, and its receipt is closed
        if (giver.Gone && !taker.Damaged && (taker.Save != null || taker.Bank != null))
        {
            taker.Close(receipt.Id);
            return taker.Write(paths);
        }
        if (giver.Damaged || (giver.Save == null && giver.Bank == null)) return false;

        // The taker is gone, or another game took its slot (neither the receipt nor the Pokémon): back to the giver
        bool takerGone = taker.Gone || (!taker.Damaged && !taker.Has(receipt.Id) && !ids.Any(taker.Holds));
        if (takerGone)
        {
            foreach (var p in receipt.Pokemon)
                if (p.Id is { } id && !giver.Holds(id)) giver.PutBack(p);
            giver.Close(receipt.Id);
            giver.Write(paths);
            Log.Info($"A move to {(receipt.To == HomeSide ? "the Home box" : $"slot {receipt.To}")} was undone: it is gone or is another game.");
            return true;
        }
        if (taker.Damaged) return false;

        // The giver lets go of them (the second write), then the taker's receipt closed and the giver's last
        if (ids.Any(giver.Holds) && taker.Has(receipt.Id))
        {
            foreach (var id in ids) giver.TakeOut(id);
            if (!giver.Has(receipt.Id)) giver.Add(receipt);
            if (!giver.Write(paths)) return false;
        }
        if (taker.Has(receipt.Id))
        {
            taker.Close(receipt.Id);
            if (!taker.Write(paths)) return false;
        }
        if (giver.Has(receipt.Id))
        {
            giver.Close(receipt.Id);
            if (!giver.Write(paths)) return false;
        }
        return true;
    }

    // ------------------------------------------------------------------ words

    /// <summary>What the screen says of a refusal.</summary>
    public static string Words(HomeRefusal why) => why switch
    {
        HomeRefusal.Unreadable => Text.Of("homeBox.why.unreadable"),
        HomeRefusal.NewerSave => Text.Of("homeBox.why.newer"),
        HomeRefusal.OtherData => Text.Of("homeBox.why.otherData"),
        HomeRefusal.HomeDamaged => Text.Of("homeBox.why.damaged"),
        HomeRefusal.Suspended => Text.Of("homeBox.why.suspended"),
        HomeRefusal.SameSlot => Text.Of("homeBox.why.sameSlot"),
        HomeRefusal.Egg => Text.Of("homeBox.why.egg"),
        HomeRefusal.NoId => Text.Of("homeBox.why.noId"),
        HomeRefusal.AlreadyThere => Text.Of("homeBox.why.already"),
        HomeRefusal.RunUnderWay => Text.Of("homeBox.why.run"),
        HomeRefusal.LastOnTeam => Text.Of("homeBox.why.last"),
        HomeRefusal.NoRoom => Text.Of("homeBox.why.room"),
        HomeRefusal.CannotCross => Text.Of("homeBox.why.cross"),
        HomeRefusal.CouldNotSave => Text.Of("homeBox.why.save"),
        HomeRefusal.NotFound => Text.Of("homeBox.why.notFound"),
        _ => ""
    };
}
