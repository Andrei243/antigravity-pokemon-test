using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Foreign;

namespace PokemonPlatinumEngine.Models;

/// <summary>Why a Pokémon of a source can't be chosen (<see cref="Migration.WhyNot"/>).</summary>
public enum MigrationRefusal
{
    None,
    /// <summary>An Egg (<c>GBA_MON_STATE_IS_EGG</c>); a bad Egg is one too.</summary>
    Egg,
    /// <summary>It knows one of the GBA's HM moves (<c>GBA_MON_STATE_HAS_HM</c>).</summary>
    KnowsAnHm,
    /// <summary>It holds one of the GBA's key items or HMs (<c>GBA_MON_STATE_HAS_INVALID_ITEM</c>).</summary>
    KeyItem,
    /// <summary>A species the games don't share (<c>GBA_MON_STATE_IS_INVALID_SPECIES</c>).</summary>
    NotARealSpecies,
    /// <summary>It has come over before (ours): its personality and its trainer's ID are in the ledger.</summary>
    CameBefore,
    /// <summary>The legality check refuses it (ours): see <see cref="MigrationVerdict.Detail"/>.</summary>
    Impossible
}

/// <summary>Why a source can't bring Pokémon now, whichever they are (<see cref="Migration.WhyNotFrom"/>).</summary>
public enum MigrationBlock
{
    None,
    /// <summary>The save hasn't the National Pokédex (Platinum's MIGRATE asks it).</summary>
    NeedsNationalPokedex,
    /// <summary>The source's boxes hold fewer than six Pokémon (<c>CANNOT_MIGRATE_LESS_THAN_6_IN_GBA_BOXES</c>).</summary>
    TooFew,
    /// <summary>The six that came before still wait for the show (<c>CANNOT_MIGRATE_ALREADY_STOCKED</c>).</summary>
    AlreadyStocked,
    /// <summary>This source gave in the last day (<c>CANNOT_MIGRATE_WAIT_FOR_FULL_DAY</c>).</summary>
    WaitForADay,
    /// <summary>Not six chosen, or one twice.</summary>
    NotSix,
    /// <summary>One of the six chosen can't come (<see cref="MigrationRefusal"/>).</summary>
    Refused,
    /// <summary>The receiving save could not be read or written, so nothing was brought.</summary>
    CouldNotSave
}

/// <summary>A Pokémon's verdict: whether it can come, and for a refusal that names something (a move), the thing.</summary>
public readonly record struct MigrationVerdict(MigrationRefusal Refusal, string? Detail = null)
{
    public bool Allowed => Refusal == MigrationRefusal.None;
}

/// <summary>
/// Pal Park's migration, as Platinum's (plan 08 · P21; <c>src/main_menu/gba_migrator.c</c> and
/// <c>pal_park_transfers.c</c>; <c>docs/mechanics/rulings.md</c>, "Pal Park's migration"): six Pokémon out of a source's
/// boxes — a GBA save, a Platinum save, a folder of Pokémon files — wait in the park for the Catching Show. The rules
/// here are GPU-free and take the time as an argument. What is Platinum's: only with the National Pokédex; six in the
/// source's boxes; none while the last six still wait; a source once in a day; no Egg, no HM move, no key item, only
/// real species. What is ours: none that came before, and none the legality check refuses. What is left out: the
/// console and the clock (<c>CANNOT_MIGRATE_DIFFERENT_CONSOLE</c>, <c>CANNOT_MIGRATE_INTERNAL_CLOCK_ALTERED</c>), for
/// which a computer has nothing to stand.
/// </summary>
public static class Migration
{
    /// <summary>Six come at a time (<c>CATCHING_SHOW_MONS</c>).</summary>
    public static int Size => Overworld.CatchingShow.Size;

    /// <summary>The source a folder of loose Pokémon files is remembered by: they count as one source.</summary>
    public const uint LooseFilesId = 0xFFFFFFFF;

    // What each species may know, by the legality check; kept, since a box of thirty asks it thirty times
    private static readonly Dictionary<string, HashSet<string>> Learnable = new(StringComparer.Ordinal);

    /// <summary>The identity a Pokémon is remembered by once it has come over.</summary>
    public static string IdentityOf(ForeignPokemon pokemon) => PalParkLedger.Identity(pokemon.Personality, pokemon.OtId);

    /// <summary>Whether a source's Pokémon may be chosen, and if not why.</summary>
    public static MigrationVerdict WhyNot(ForeignPokemon pokemon, PalParkLedger ledger, IReadOnlySet<Guid>? heldByTarget = null)
    {
        // A Pokémon of one of this game's own saves (plan 08 · P22) is moved, not brought: it is told by its id
        if (pokemon.Own != null) return WhyNotOwn(pokemon.Own, heldByTarget ?? new HashSet<Guid>());
        if (pokemon.IsEgg || pokemon.IsBadEgg) return new(MigrationRefusal.Egg);
        // The GBA's own checks, in the order the migrator asks them
        if (pokemon.Console == ForeignConsole.Gba)
        {
            if (pokemon.KnowsAnHm) return new(MigrationRefusal.KnowsAnHm);
            if (pokemon.HoldsAnInvalidItem) return new(MigrationRefusal.KeyItem);
        }
        if (!pokemon.HasARealSpecies) return new(MigrationRefusal.NotARealSpecies);
        if (ledger.HasCome(IdentityOf(pokemon))) return new(MigrationRefusal.CameBefore);
        return Impossible(pokemon) is { } why ? new(MigrationRefusal.Impossible, why) : new(MigrationRefusal.None);
    }

    /// <summary>
    /// Whether a Pokémon of one of this game's saves may be moved to another (plan 08 · P22): not an Egg, one with an id
    /// (a save not played since ids were given has none yet), not one the receiving save already holds, and one the legality check passes.
    /// </summary>
    public static MigrationVerdict WhyNotOwn(SavedPokemonData pokemon, IReadOnlySet<Guid> heldByTarget)
    {
        if (pokemon.IsEgg) return new(MigrationRefusal.Egg);
        if (pokemon.Id is not { } id) return new(MigrationRefusal.Impossible, Text.Of("migrate.impossible.noId"));
        if (heldByTarget.Contains(id)) return new(MigrationRefusal.CameBefore);
        return ImpossibleSaved(pokemon) is { } why ? new(MigrationRefusal.Impossible, why) : new(MigrationRefusal.None);
    }

    /// <summary>The legality check of a Pokémon a save keeps: <see cref="Impossible"/> without the foreign file's numbers.</summary>
    public static string? ImpossibleSaved(SavedPokemonData saved)
    {
        var species = PokemonDatabase.Get(saved.SpeciesName) ?? PokemonDatabase.GetByDex(saved.DexNumber);
        if (species == null) return Text.Of("migrate.impossible.species");
        // A save of this game's may hold a level past 100 (plan 18 · W4): the highest any rules allow is the bound
        if (saved.CurrentExp < 0 || saved.CurrentExp > Pokemon.GetExpForLevel(Ruleset.HighestLevel, species.GrowthRate)) return Text.Of("migrate.impossible.exp");
        int[] evs = { saved.EvHP, saved.EvAttack, saved.EvDefense, saved.EvSpeed, saved.EvSpAttack, saved.EvSpDefense };
        if (evs.Any(e => e is < 0 or > 255) || evs.Sum() > 510) return Text.Of("migrate.impossible.effort");
        int[] ivs = { saved.IvHP, saved.IvAttack, saved.IvDefense, saved.IvSpeed, saved.IvSpAttack, saved.IvSpDefense };
        if (ivs.Any(i => i is < 0 or > 31)) return Text.Of("migrate.impossible.ivs");
        return UnlearnableMove(saved.ToPokemon());
    }

    private static string? UnlearnableMove(Pokemon made)
    {
        HashSet<string> may;
        lock (Learnable)
        {
            if (!Learnable.TryGetValue(made.Species.Name, out may!)) Learnable[made.Species.Name] = may = Legality.Learnable(made.Species);
        }
        foreach (var move in made.Learnset) may.Add(move.MoveName);
        foreach (var move in made.Moves)
            if (!may.Contains(move.Name)) return Text.Of("migrate.impossible.move", ("move", LocalNames.Move(move.Name)));
        return null;
    }

    /// <summary>
    /// The legality check (plan 07 · O5's, written here): the species is in the data; EXP no higher than a level 100 has;
    /// effort in bounds (255 a stat, 510 in all); an item this game has; and moves its data can teach it. Returns what is
    /// wrong in a few words, or null. Nature, gender and ability follow from the personality, so they can't be wrong.
    /// </summary>
    public static string? Impossible(ForeignPokemon pokemon)
    {
        if (pokemon.SpeciesData is not { } species) return Text.Of("migrate.impossible.species");
        if (pokemon.Experience < 0 || pokemon.Experience > Pokemon.GetExpForLevel(100, species.GrowthRate)) return Text.Of("migrate.impossible.exp");
        if (pokemon.Evs.Any(e => e is < 0 or > 255) || pokemon.Evs.Sum() > 510) return Text.Of("migrate.impossible.effort");
        if (pokemon.Ivs.Any(i => i is < 0 or > 31)) return Text.Of("migrate.impossible.ivs");
        if (pokemon.HeldItemId != 0 && pokemon.Console == ForeignConsole.Ds && pokemon.HeldItemName == null) return Text.Of("migrate.impossible.item");

        PokemonPlatinumEngine.Core.SavedPokemonData saved;
        try { saved = pokemon.ToSaved(); }
        catch (InvalidOperationException) { return Text.Of("migrate.impossible.species"); }
        return UnlearnableMove(saved.ToPokemon());
    }

    /// <summary>
    /// Whether a source can bring Pokémon now, whichever they are (<c>GetCanMigrateStatus</c>, with the National Pokédex
    /// first): a source that gave in the last day waits, fewer than six in its boxes is too few, and six still waiting
    /// in the park are in the way.
    /// </summary>
    public static MigrationBlock WhyNotFrom(ForeignSave source, bool nationalPokedex, PalParkState park, DateTime now)
    {
        if (!nationalPokedex) return MigrationBlock.NeedsNationalPokedex;
        // (this game's own saves take no turns: a move between them is told by the receipt, not by the day)
        if (source.Kind != ForeignKind.OwnSave && park.Ledger.WaitLeft(SourceId(source), now) > TimeSpan.Zero) return MigrationBlock.WaitForADay;
        if (source.Count < Size) return MigrationBlock.TooFew;
        if (park.Waiting.Count > 0) return MigrationBlock.AlreadyStocked;
        return MigrationBlock.None;
    }

    /// <summary>The ID a source is remembered by: its trainer's, or one for a folder of Pokémon files.</summary>
    public static uint SourceId(ForeignSave source) => source.Kind == ForeignKind.Files ? LooseFilesId : source.TrainerId;

    /// <summary>How much longer a source must wait, and 0 for one that may bring Pokémon.</summary>
    public static TimeSpan WaitLeft(ForeignSave source, PalParkState park, DateTime now) => park.Ledger.WaitLeft(SourceId(source), now);

    /// <summary>
    /// Brings six Pokémon over: they are converted as <c>BoxMonGBAToBoxMon</c> converts them and wait in the park, their
    /// identities and their source's time are written in the ledger. Returns what stopped it (<see cref="MigrationBlock.None"/>
    /// when it was done); nothing is changed unless it was.
    /// </summary>
    public static MigrationBlock Bring(PalParkState park, ForeignSave source, IReadOnlyList<ForeignPokemon> six, bool nationalPokedex, DateTime now, Ruleset? rules = null)
    {
        var block = WhyNotFrom(source, nationalPokedex, park, now);
        if (block != MigrationBlock.None) return block;
        if (six.Count != Size || six.Select(IdentityOf).Distinct().Count() != Size) return MigrationBlock.NotSix;
        if (six.Any(p => !WhyNot(p, park.Ledger).Allowed)) return MigrationBlock.Refused;

        var made = six.Select(p => p.ToSaved(rules).ToPokemon(rules)).ToList();
        if (!park.Stock(made)) return MigrationBlock.AlreadyStocked;
        park.Ledger.Remember(SourceId(source), now);
        foreach (var pokemon in six) park.Ledger.MarkCame(IdentityOf(pokemon));
        return MigrationBlock.None;
    }

    /// <summary>
    /// The same on a save that isn't loaded (the title's MIGRATE): the save's own park is read, the six brought, and the
    /// park written back into it. Nothing is changed unless they came.
    /// </summary>
    public static MigrationBlock Bring(SaveData save, ForeignSave source, IReadOnlyList<ForeignPokemon> six, DateTime now, Ruleset? rules = null)
    {
        var pastimes = new Pastimes();
        pastimes.Load(save.Pastimes);
        var block = Bring(pastimes.PalPark, source, six, save.NationalPokedex, now, rules);
        if (block == MigrationBlock.None) save.Pastimes = pastimes.ToSave();
        return block;
    }

    /// <summary>
    /// A save of this game as a source (plan 08 · P22): its boxes, each Pokémon as it is kept (<see cref="ForeignPokemon.Own"/>),
    /// so that what is moved is the Pokémon itself and not a conversion of it.
    /// </summary>
    public static ForeignSave FromSave(SaveData save, bool withParty = false)
    {
        var boxes = new List<ForeignBox>();
        // The way out looks at the team too: a box of its own before the first of the PC's
        if (withParty)
        {
            var team = new ForeignPokemon?[PcBoxes.BoxSize];
            for (int i = 0; i < Math.Min(save.Party.Count, team.Length); i++) team[i] = Of(save.Party[i], save);
            boxes.Add(new ForeignBox { Name = Text.Of("migrate.party"), Slots = team });
        }
        foreach (var (box, i) in (save.Boxes?.Boxes ?? new()).Select((b, i) => (b, i)))
        {
            var slots = new ForeignPokemon?[PcBoxes.BoxSize];
            foreach (var placed in box.Pokemon)
                if (placed.Slot is >= 0 and < PcBoxes.BoxSize) slots[placed.Slot] = Of(placed.Pokemon, save);
            boxes.Add(new ForeignBox { Name = string.IsNullOrEmpty(box.Name) ? Text.Of("migrate.box", ("n", i + 1)) : box.Name, Slots = slots });
        }
        return new ForeignSave { Kind = ForeignKind.OwnSave, TrainerName = save.PlayerName, TrainerId = unchecked((uint)save.TrainerId), Boxes = boxes };
    }

    /// <summary>A Pokémon of a save as the migration's screen draws and asks about it, holding the saved Pokémon whole.</summary>
    public static ForeignPokemon Of(SavedPokemonData saved, SaveData save)
    {
        var species = PokemonDatabase.Get(saved.SpeciesName) ?? PokemonDatabase.GetByDex(saved.DexNumber);
        var trainer = saved.OriginalTrainer;
        return new ForeignPokemon
        {
            Console = ForeignConsole.Ds,
            Own = saved,
            Personality = saved.Personality ?? 0,
            OtId = trainer != null ? unchecked((uint)trainer.Id) : unchecked((uint)save.TrainerId),
            Species = species?.DexNumber ?? 0,
            IsEgg = saved.IsEgg,
            Nickname = saved.Nickname,
            HasNickname = saved.Nickname.Length > 0,
            OtName = trainer?.Name ?? save.PlayerName,
            Experience = saved.CurrentExp
        };
    }

    /// <summary>A loose Pokémon file is not a source of its own: the folder's files are one (<see cref="ForeignKind.Files"/>), thirty to a box.</summary>
    public static ForeignSave Combine(IEnumerable<ForeignSave> files)
    {
        var all = files.SelectMany(f => f.All).ToList();
        var boxes = new List<ForeignBox>();
        for (int at = 0, n = 1; at < all.Count || boxes.Count == 0; at += 30, n++)
        {
            var slots = all.Skip(at).Take(30).Cast<ForeignPokemon?>().ToList();
            while (slots.Count < 30) slots.Add(null);
            boxes.Add(new ForeignBox { Name = Text.Of("migrate.files.box", ("n", n)), Slots = slots });
        }
        return new ForeignSave { Kind = ForeignKind.Files, Boxes = boxes };
    }

    // ------------------------------------------------------------------ in words

    /// <summary>Why a Pokémon can't be chosen, as the screen says it.</summary>
    public static string Words(MigrationVerdict verdict) => verdict.Refusal switch
    {
        MigrationRefusal.Egg => Text.Of("migrate.why.egg"),
        MigrationRefusal.KnowsAnHm => Text.Of("migrate.why.hm"),
        MigrationRefusal.KeyItem => Text.Of("migrate.why.item"),
        MigrationRefusal.NotARealSpecies => Text.Of("migrate.why.species"),
        MigrationRefusal.CameBefore => Text.Of("migrate.why.before"),
        MigrationRefusal.Impossible => Text.Of("migrate.why.impossible", ("why", verdict.Detail ?? "")),
        _ => ""
    };

    /// <summary>Why a source can't bring Pokémon now, as the screen says it (the wait in hours).</summary>
    public static string Words(MigrationBlock block, TimeSpan waitLeft = default) => block switch
    {
        MigrationBlock.NeedsNationalPokedex => Text.Of("migrate.block.national"),
        MigrationBlock.TooFew => Text.Of("migrate.block.tooFew", ("n", Size)),
        MigrationBlock.AlreadyStocked => Text.Of("migrate.block.stocked"),
        MigrationBlock.WaitForADay => Text.Of("migrate.block.wait", ("hours", Math.Max(1, (int)Math.Ceiling(waitLeft.TotalHours)))),
        MigrationBlock.NotSix => Text.Of("migrate.block.notSix", ("n", Size)),
        MigrationBlock.Refused => Text.Of("migrate.block.refused"),
        MigrationBlock.CouldNotSave => Text.Of("migrate.block.notSaved"),
        _ => ""
    };
}
