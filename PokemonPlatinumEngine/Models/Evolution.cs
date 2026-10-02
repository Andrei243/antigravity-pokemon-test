using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What sets an evolution off. Each <see cref="EvolutionMethod"/> answers to exactly one.</summary>
public enum EvolutionTrigger
{
    /// <summary>The Pokémon gained a level (checked once the battle or the Rare Candy is over).</summary>
    LevelUp,
    /// <summary>An item was used on it from the bag.</summary>
    UseItem,
    /// <summary>It has just arrived by trade.</summary>
    Trade,
    /// <summary>The player turned a full circle in the field.</summary>
    Spin
}

/// <summary>What the rules need to know besides the Pokémon itself. Whoever asks fills in what it knows.</summary>
public sealed class EvolutionContext
{
    /// <summary>The party it is in (species or types that must be there; Shedinja's free place).</summary>
    public Party? Party { get; set; }
    public Inventory? Bag { get; set; }

    /// <summary>Night as Platinum's evolutions count it: 20:00 to 03:59 (<c>GameClock.IsNight</c>).</summary>
    public bool IsNight { get; set; }

    /// <summary>It is raining in the field.</summary>
    public bool IsRaining { get; set; }

    /// <summary>The special places of the map the player is on (<c>Map.EvolutionSites</c>).</summary>
    public IReadOnlyCollection<string> Sites { get; set; } = Array.Empty<string>();

    /// <summary>The item being used, for <see cref="EvolutionTrigger.UseItem"/>.</summary>
    public ItemData? Item { get; set; }

    /// <summary>The species given away for it, for <see cref="EvolutionTrigger.Trade"/> (null if nothing was).</summary>
    public PokemonSpecies? TradedFor { get; set; }
}

/// <summary>What an evolution did.</summary>
public sealed class EvolutionOutcome
{
    public required PokemonSpecies From { get; init; }
    public required PokemonSpecies Into { get; init; }

    /// <summary>The Shedinja left behind in the party when Nincada evolved, if there was room and a Poké Ball.</summary>
    public Pokemon? Shed { get; init; }

    /// <summary>Moves the new species learns on evolving or at this level, which the Pokémon doesn't know yet.</summary>
    public List<string> NewMoves { get; init; } = new();
}

/// <summary>
/// When a Pokémon evolves, and what evolving does. Platinum's rules are pret/pokeplatinum's
/// <c>Pokemon_GetEvolutionTargetSpecies</c> and <c>Evolution_ProcessEvolutionEffects</c>; the methods of later
/// games follow the game that introduced them, with a stand-in where that game leans on hardware or a mode this
/// one lacks (docs/mechanics/evolution.md has the whole list, docs/mechanics/rulings.md the choices).
///
/// Nothing here draws or reads input. <see cref="Find"/> answers "does it evolve now?" for one trigger and
/// <see cref="Evolve"/> carries it out; the field, the bag, battles and (later) trades all go through the two.
/// </summary>
public static class Evolution
{
    /// <summary>Stops level-up and trade evolutions while held.</summary>
    public const string EverstoneEffect = "NoEvolve";

    /// <summary>Used on a Pokémon, it counts as a trade (Legends: Arceus): a way to evolve without a partner.</summary>
    public const string LinkingCord = "Linking Cord";

    private const string PokeBall = "Poké Ball";

    // Keys of Pokemon.EvolutionProgress
    public const string StepsKey = "Steps";
    public static string MoveKey(string move) => "Move:" + move;
    public static string DefeatKey(string species) => "Defeated:" + species;

    /// <summary>The trigger a method waits for; null for one that never fires by itself.</summary>
    public static EvolutionTrigger? TriggerOf(EvolutionMethod method) => method switch
    {
        EvolutionMethod.UseItem or EvolutionMethod.UseItemMale or EvolutionMethod.UseItemFemale or EvolutionMethod.UseItemNight
            => EvolutionTrigger.UseItem,
        EvolutionMethod.Trade or EvolutionMethod.TradeHoldingItem or EvolutionMethod.TradeWithSpecies => EvolutionTrigger.Trade,
        EvolutionMethod.SpinHoldingItem => EvolutionTrigger.Spin,
        // Shedinja is what Nincada leaves behind, not something it turns into
        EvolutionMethod.LevelShedinja or EvolutionMethod.Other => null,
        _ => EvolutionTrigger.LevelUp
    };

    /// <summary>
    /// The evolution this Pokémon goes through now, or null. As in Platinum the first of the species' evolutions
    /// whose condition holds wins, an Everstone stops everything but an item used on the Pokémon, and Kadabra
    /// evolves whatever it holds.
    /// </summary>
    public static EvolutionData? Find(Pokemon p, EvolutionTrigger trigger, EvolutionContext context)
    {
        if (p.Species.Evolutions == null) return null;

        // A Linking Cord is a trade in all but name
        bool cord = trigger == EvolutionTrigger.UseItem && context.Item?.Name == LinkingCord;
        if (cord) trigger = EvolutionTrigger.Trade;

        if ((trigger != EvolutionTrigger.UseItem) && p.HeldItem?.HoldEffect == EverstoneEffect && p.Species.Name != "Kadabra")
            return null;

        return p.Species.Evolutions.FirstOrDefault(e =>
            TriggerOf(e.Method) == trigger && Meets(p, e, context, cord) && PokemonDatabase.Get(e.TargetSpecies) != null);
    }

    private static bool Meets(Pokemon p, EvolutionData e, EvolutionContext context, bool cord)
    {
        if (p.Level < e.Level) return false;
        if (e.NeedsFriendship && p.Friendship < FriendshipRules.EvolveAt) return false;

        bool Holds() => e.Item != null && p.HeldItem?.Name == e.Item;
        var others = context.Party?.Members.Where(m => m != p) ?? Enumerable.Empty<Pokemon>();

        switch (e.Method)
        {
            case EvolutionMethod.Level:
            case EvolutionMethod.LevelNinjask:
            // There is no console to hold upside down: a plain level here
            case EvolutionMethod.LevelUpsideDown:
            case EvolutionMethod.Trade:
                return true;

            case EvolutionMethod.Friendship:
            // Affection (Generation 6's Pokémon-Amie) was folded into friendship from Generation 8 on
            case EvolutionMethod.Affection:
                return p.Friendship >= FriendshipRules.EvolveAt;
            case EvolutionMethod.FriendshipDay: return !context.IsNight && p.Friendship >= FriendshipRules.EvolveAt;
            case EvolutionMethod.FriendshipNight: return context.IsNight && p.Friendship >= FriendshipRules.EvolveAt;

            case EvolutionMethod.LevelDay: return !context.IsNight;
            case EvolutionMethod.LevelNight: return context.IsNight;
            case EvolutionMethod.LevelMale: return p.Gender == Gender.Male;
            case EvolutionMethod.LevelFemale: return p.Gender == Gender.Female;

            case EvolutionMethod.LevelAttackHigher: return p.Attack > p.Defense;
            case EvolutionMethod.LevelAttackEqual: return p.Attack == p.Defense;
            case EvolutionMethod.LevelDefenseHigher: return p.Attack < p.Defense;

            case EvolutionMethod.LevelPersonalityLow: return (p.Personality >> 16) % 10 < 5;
            case EvolutionMethod.LevelPersonalityHigh: return (p.Personality >> 16) % 10 >= 5;

            case EvolutionMethod.LevelHoldingItem: return Holds();
            case EvolutionMethod.LevelHoldingItemDay: return Holds() && !context.IsNight;
            case EvolutionMethod.LevelHoldingItemNight: return Holds() && context.IsNight;

            case EvolutionMethod.LevelKnowsMove: return e.Move != null && p.Knows(e.Move);
            case EvolutionMethod.LevelKnowsMoveType: return e.Type != null && p.Moves.Any(m => m.Type == e.Type);

            case EvolutionMethod.LevelWithSpeciesInParty: return others.Any(m => m.Species.Name == e.Species);
            case EvolutionMethod.LevelWithTypeInParty:
                return others.Any(m => m.Species.PrimaryType == e.Type || m.Species.SecondaryType == e.Type);

            case EvolutionMethod.LevelAtLocation: return e.Location != null && context.Sites.Contains(e.Location);
            case EvolutionMethod.LevelInRain: return context.IsRaining;
            case EvolutionMethod.Beauty: return p.Beauty >= e.Value;

            case EvolutionMethod.LevelAfterSteps: return Progress(p, StepsKey) >= e.Value;
            case EvolutionMethod.LevelAfterMoveUses: return e.Move != null && Progress(p, MoveKey(e.Move)) >= e.Value;
            case EvolutionMethod.LevelAfterDefeating: return e.Species != null && Progress(p, DefeatKey(e.Species)) >= e.Value;
            case EvolutionMethod.LevelWithItemsInBag:
                return e.Item != null && ItemDatabase.Get(e.Item) is { } needed && context.Bag?.GetQuantity(needed) >= e.Value;

            case EvolutionMethod.UseItem: return context.Item?.Name == e.Item;
            case EvolutionMethod.UseItemMale: return context.Item?.Name == e.Item && p.Gender == Gender.Male;
            case EvolutionMethod.UseItemFemale: return context.Item?.Name == e.Item && p.Gender == Gender.Female;
            case EvolutionMethod.UseItemNight: return context.Item?.Name == e.Item && context.IsNight;

            case EvolutionMethod.TradeHoldingItem: return Holds();
            // Traded for its partner; with a Linking Cord the partner has to be in the party instead
            case EvolutionMethod.TradeWithSpecies:
                return cord ? others.Any(m => m.Species.Name == e.Species) : context.TradedFor?.Name == e.Species;

            case EvolutionMethod.SpinHoldingItem: return Holds();

            default: return false;
        }
    }

    /// <summary>
    /// Carries an evolution out: uses up what the method uses up, changes the species, leaves Shedinja behind
    /// for Nincada, and says which moves the new species wants to learn. The item of
    /// <see cref="EvolutionTrigger.UseItem"/> is the bag's to remove.
    /// </summary>
    public static EvolutionOutcome Evolve(Pokemon p, EvolutionData e, EvolutionContext context)
    {
        var from = p.Species;
        var into = PokemonDatabase.Get(e.TargetSpecies)
            ?? throw new InvalidOperationException($"{from.Name} evolves into unknown species {e.TargetSpecies}");

        switch (e.Method)
        {
            case EvolutionMethod.TradeHoldingItem:
            case EvolutionMethod.LevelHoldingItem:
            case EvolutionMethod.LevelHoldingItemDay:
            case EvolutionMethod.LevelHoldingItemNight:
            case EvolutionMethod.SpinHoldingItem:
                p.HeldItem = null;
                break;
            case EvolutionMethod.LevelWithItemsInBag:
                if (e.Item != null && ItemDatabase.Get(e.Item) is { } spent) context.Bag?.RemoveItem(spent, e.Value);
                break;
        }

        // The shell is a copy of the Pokémon as it was, so it is made before anything else changes
        var shed = e.Method == EvolutionMethod.LevelNinjask ? Shed(p, from, context) : null;

        p.EvolveInto(into);
        p.EvolutionProgress.Clear();

        return new EvolutionOutcome { From = from, Into = into, Shed = shed, NewMoves = MovesOnEvolving(p) };
    }

    /// <summary>
    /// Nincada's empty shell: with a free place in the party and a Poké Ball in the bag, a Shedinja with the same
    /// level, nature, IVs, EVs and moves joins the party, and the ball is used up.
    /// </summary>
    private static Pokemon? Shed(Pokemon p, PokemonSpecies from, EvolutionContext context)
    {
        var shellData = from.Evolutions?.FirstOrDefault(x => x.Method == EvolutionMethod.LevelShedinja);
        var species = shellData != null ? PokemonDatabase.Get(shellData.TargetSpecies) : null;
        var ball = ItemDatabase.Get(PokeBall);
        if (species == null || ball == null || context.Party == null || context.Party.IsFull) return null;
        if (context.Bag == null || context.Bag.GetQuantity(ball) < 1) return null;

        var shell = new Pokemon(species, p.Level, Pokemon.RollGender(species, Random.Shared), p.Nature, p.IsShiny)
        {
            IvHP = p.IvHP, IvAttack = p.IvAttack, IvDefense = p.IvDefense,
            IvSpAttack = p.IvSpAttack, IvSpDefense = p.IvSpDefense, IvSpeed = p.IvSpeed,
            EvHP = p.EvHP, EvAttack = p.EvAttack, EvDefense = p.EvDefense,
            EvSpAttack = p.EvSpAttack, EvSpDefense = p.EvSpDefense, EvSpeed = p.EvSpeed,
            CurrentExp = p.CurrentExp,
            Personality = p.Personality,
            Friendship = p.Friendship,
            Ball = PokeBall
        };
        shell.Moves.Clear();
        foreach (var m in p.Moves) shell.Moves.Add(new Move(m.Data, m.CurrentPP));
        shell.RecalculateStats();
        shell.CurrentHP = shell.MaxHP;

        context.Bag.RemoveItem(ball, 1);
        context.Party.Add(shell);
        return shell;
    }

    /// <summary>
    /// The moves a freshly evolved Pokémon may learn: the new species' moves for this very level (Platinum) and
    /// its evolution moves (level 0 in the learnsets of later games), leaving out what it already knows.
    /// </summary>
    public static List<string> MovesOnEvolving(Pokemon p) =>
        p.Species.Learnset.Where(m => m.Level == 0 || m.Level == p.Level)
            .Select(m => m.MoveName).Distinct().Where(m => !p.Knows(m)).ToList();

    // ---------------------------------------------------------------- items

    private static readonly Lazy<HashSet<string>> UsedItems = new(() => ItemsOf(m => TriggerOf(m) == EvolutionTrigger.UseItem));

    private static readonly Lazy<HashSet<string>> HeldItems = new(() => ItemsOf(m => m is EvolutionMethod.TradeHoldingItem
        or EvolutionMethod.LevelHoldingItem or EvolutionMethod.LevelHoldingItemDay or EvolutionMethod.LevelHoldingItemNight
        or EvolutionMethod.SpinHoldingItem));

    private static HashSet<string> ItemsOf(Func<EvolutionMethod, bool> methods) =>
        PokemonDatabase.GetAll().SelectMany(s => s.Evolutions ?? new()).Where(e => e.Item != null && methods(e.Method))
            .Select(e => e.Item!).ToHashSet();

    /// <summary>An item that evolves some Pokémon when used on it: the stones and their like, and the Linking Cord.</summary>
    public static bool IsUsedToEvolve(ItemData item) => item.Name == LinkingCord || UsedItems.Value.Contains(item.Name);

    /// <summary>An item a Pokémon holds for its evolution's sake: one some species must hold to evolve, or the Everstone.</summary>
    public static bool IsHeldForEvolution(ItemData item) => item.HoldEffect == EverstoneEffect || HeldItems.Value.Contains(item.Name);

    // ---------------------------------------------------------------- things that are counted

    public static int Progress(Pokemon p, string key) => p.EvolutionProgress.GetValueOrDefault(key);

    /// <summary>A step walked at the head of the party.</summary>
    public static void CountStep(Pokemon p) => Count(p, EvolutionMethod.LevelAfterSteps, _ => StepsKey);

    /// <summary>A move used in battle.</summary>
    public static void CountMoveUse(Pokemon p, string move) =>
        Count(p, EvolutionMethod.LevelAfterMoveUses, e => e.Move == move ? MoveKey(move) : null);

    /// <summary>A foe it knocked out.</summary>
    public static void CountDefeat(Pokemon p, PokemonSpecies foe) =>
        Count(p, EvolutionMethod.LevelAfterDefeating, e => e.Species == foe.Name ? DefeatKey(foe.Name) : null);

    /// <summary>Only what one of the species' evolutions asks for is counted, and no further than it asks.</summary>
    private static void Count(Pokemon p, EvolutionMethod method, Func<EvolutionData, string?> keyOf)
    {
        foreach (var e in p.Species.Evolutions ?? Enumerable.Empty<EvolutionData>())
        {
            if (e.Method != method || keyOf(e) is not { } key) continue;
            p.EvolutionProgress[key] = Math.Min(e.Value, Progress(p, key) + 1);
            return;
        }
    }
}
