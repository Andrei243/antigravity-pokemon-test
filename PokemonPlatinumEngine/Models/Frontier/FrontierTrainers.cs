using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// One of the Frontier's trainers with the team made for one battle: who they are, the sets drawn and the Pokémon
/// built from them (the original's <c>FrontierOpponent</c>).
/// </summary>
public sealed class FrontierOpponent
{
    /// <summary>The trainer's number among the Frontier's trainers.</summary>
    public int Number { get; init; }
    public FrontierTrainerRecord Record => FrontierDatabase.Trainer(Number);
    public List<int> Sets { get; init; } = new();
    public List<Pokemon> Pokemon { get; init; } = new();

    /// <summary>The number its Pokémon carry as their first trainer's, and each one's personality: a partner's are kept so the same team comes back after a pause.</summary>
    public uint OtId { get; init; }
    public List<uint> Personalities { get; init; } = new();

    /// <summary>The draw couldn't keep the items apart, and the team was handed the four reserved ones instead (<c>giveReservedItem</c>).</summary>
    public bool ReservedItems { get; init; }

    /// <summary>
    /// The trainer as a battle takes one (<c>FieldBattleDTO_InitFrontierTrainer</c>): no prize money, no items of
    /// their own, and the three routines every Frontier trainer thinks with (<c>FieldBattleDTO_NewBattleTower</c>:
    /// <c>AI_FLAG_BASIC | AI_FLAG_EVAL_ATTACK | AI_FLAG_EXPERT</c>), or with <paramref name="mind"/> where a facility has its
    /// trainers think otherwise (the Battle Factory's by the round).
    /// </summary>
    public Trainer AsTrainer(AiFlags? mind = null)
    {
        var trainer = new Trainer
        {
            Id = "frontier_" + Record.Id,
            Name = Record.Name,
            TrainerClass = Record.Class,
            PrizeMoney = 0,
            Ai = mind ?? FrontierTrainers.Mind,
            Look = FrontierTrainers.LookOf(Record)
        };
        foreach (var p in Pokemon) trainer.Party.Add(p);
        return trainer;
    }
}

/// <summary>
/// How the Battle Frontier makes its trainers' Pokémon (plan 06 · R18a), ported from
/// <c>src/overlay104/frontier_opponents.c</c>: how good a trainer's Pokémon are by the trainer's number, a set
/// turned into a Pokémon, and the draw of a team's sets that every facility but the Tower shares
/// (<see cref="SelectSets"/>, <see cref="ForTrainers"/>; the Tower draws its own way with its own generator,
/// <see cref="BattleTowerChallenge"/>). A draw of sixteen bits is a function the caller hands over (the original's
/// <c>LCRNG_Next</c>). No drawing or input.
/// </summary>
public static class FrontierTrainers
{
    /// <summary>The three routines of the trainer AI every Frontier trainer thinks with.</summary>
    public const AiFlags Mind = AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert;

    /// <summary>The items handed out, by the Pokémon's place in the team, when a draw can't keep the sets' own apart (<c>sFrontierPokemonDefaultItems</c>).</summary>
    public static readonly string[] ReservedItems = { "Bright Powder", "Lum Berry", "Leftovers", "Quick Claw" };

    /// <summary>
    /// The IV of all six stats of a trainer's Pokémon, by the trainer's number (<c>GetIVsForTrainer</c>,
    /// <c>BattleTower_GetIVsFromTrainerID</c>): 3 under trainer 100, three more for each twenty after, and 31 from
    /// trainer 220 on.
    /// </summary>
    public static int IvsFor(int trainer) => trainer switch
    {
        < 100 => 3,
        < 120 => 6,
        < 140 => 9,
        < 160 => 12,
        < 180 => 15,
        < 200 => 18,
        < 220 => 21,
        _ => 31
    };

    /// <summary>
    /// A personality that gives a set its nature and isn't shiny for the trainer (<c>FrontierPokemon_Init</c>): pairs
    /// of draws, the first the low half, until one does.
    /// </summary>
    public static uint PersonalityFor(Nature nature, uint otId, Func<int> next16)
    {
        while (true)
        {
            uint low = (uint)next16() & 0xFFFF, high = (uint)next16() & 0xFFFF;
            uint personality = low | high << 16;
            if (Personality.NatureOf(personality) == nature && !Personality.IsShiny(otId, personality, 8192)) return personality;
        }
    }

    /// <summary>
    /// A set as a Pokémon (<c>FrontierPokemon_Init</c>, <c>FrontierPokemon_InitPokemon</c>): every IV the same, its
    /// effort shared evenly between the stats the set names (510 between them, 255 at most each), its ability by its
    /// personality's lowest bit, its nature and gender the personality's, full friendship unless it knows
    /// Frustration, never shiny. <paramref name="item"/> stands in for the set's own when the draw says so.
    /// </summary>
    public static Pokemon Build(FrontierSetRecord set, uint personality, int ivs, int level = FrontierRules.Level, string? item = null)
    {
        var species = PokemonDatabase.Get(set.Species) ?? throw new InvalidOperationException($"No species called {set.Species}");
        var pokemon = new Pokemon(species, level, personality, new Random(0))
        {
            IsShiny = false,
            IvHP = ivs, IvAttack = ivs, IvDefense = ivs, IvSpeed = ivs, IvSpAttack = ivs, IvSpDefense = ivs
        };
        if (set.Form != null) pokemon.ChangeForm(set.Form);
        int stats = System.Numerics.BitOperations.PopCount((uint)set.EvFlags & 0x3F);
        int effort = stats == 0 ? 0 : Math.Min(255, 510 / stats);
        if ((set.EvFlags & 1) != 0) pokemon.EvHP = effort;
        if ((set.EvFlags & 2) != 0) pokemon.EvAttack = effort;
        if ((set.EvFlags & 4) != 0) pokemon.EvDefense = effort;
        if ((set.EvFlags & 8) != 0) pokemon.EvSpeed = effort;
        if ((set.EvFlags & 16) != 0) pokemon.EvSpAttack = effort;
        if ((set.EvFlags & 32) != 0) pokemon.EvSpDefense = effort;
        pokemon.AbilityName = Personality.AbilityOf(pokemon.Abilities, personality);
        pokemon.HeldItem = ItemDatabase.Get(item ?? set.Item ?? "");
        pokemon.Moves.Clear();
        foreach (string move in set.Moves) pokemon.Moves.Add(MoveDatabase.Create(move));
        pokemon.Friendship = set.Moves.Contains("Frustration") ? 0 : 255;
        pokemon.RecalculateStats();
        pokemon.CurrentHP = pokemon.MaxHP;
        return pokemon;
    }

    /// <summary>
    /// <c>SelectPokemonForTrainer</c>: draws <paramref name="count"/> of a trainer's sets, no two of one species or
    /// holding the same item, and none sharing either with a partner's team already drawn
    /// (<paramref name="partnerSets"/>) for the first fifty refusals of that kind, after which the partner is no
    /// longer looked at (true comes back then).
    /// </summary>
    public static List<int> SelectSets(FrontierTrainerRecord trainer, IReadOnlyList<int> partnerSets, int count, Func<int> next16, out bool gaveUp)
    {
        if (count > trainer.Sets.Count) throw new InvalidOperationException($"{trainer.Id} has only {trainer.Sets.Count} sets to draw {count} from");
        var chosen = new List<int>();
        int conflicts = 0;
        while (chosen.Count < count)
        {
            int number = trainer.Sets[(next16() & 0xFFFF) % trainer.Sets.Count];
            var set = FrontierDatabase.Set(number);
            // The original compares the items as they are, so two sets holding nothing count as holding the same
            if (chosen.Select(FrontierDatabase.Set).Any(c => c.Species == set.Species || c.Item == set.Item)) continue;
            if (conflicts < 50 && partnerSets.Select(FrontierDatabase.Set).Any(p => p.Species == set.Species || p.Item == set.Item))
            {
                conflicts++;
                continue;
            }
            chosen.Add(number);
        }
        gaveUp = conflicts >= 50;
        return chosen;
    }

    /// <summary>
    /// <c>BattleFrontier_GetPokemonForTrainers</c>: the opposing team of a battle, <paramref name="count"/> Pokémon
    /// of one trainer, or in a challenge with a partner half of them from each of two trainers, the second's kept
    /// apart from the first's. Each Pokémon's IVs go by its own trainer's number, its first trainer's number is two
    /// draws and its personality as many pairs as it takes.
    /// </summary>
    public static List<FrontierOpponent> ForTrainers(int count, int trainer, int trainer2, bool withPartner, Func<int> next16, int level = FrontierRules.Level)
    {
        if (!withPartner)
        {
            var sets = SelectSets(FrontierDatabase.Trainer(trainer), Array.Empty<int>(), count, next16, out _);
            return new List<FrontierOpponent> { Make(trainer, sets, next16, level) };
        }
        var first = SelectSets(FrontierDatabase.Trainer(trainer), Array.Empty<int>(), count / 2, next16, out _);
        var second = SelectSets(FrontierDatabase.Trainer(trainer2), first, count / 2, next16, out _);
        return new List<FrontierOpponent> { Make(trainer, first, next16, level), Make(trainer2, second, next16, level) };
    }

    // BattleFrontier_LoadFrontierPokemon over BattleFrontier_LoadOpponentMonData: a first trainer's number of its own for each Pokémon
    private static FrontierOpponent Make(int trainer, List<int> sets, Func<int> next16, int level)
    {
        int ivs = IvsFor(trainer);
        var pokemon = new List<Pokemon>();
        var personalities = new List<uint>();
        uint otId = 0;
        foreach (int number in sets)
        {
            var set = FrontierDatabase.Set(number);
            otId = ((uint)next16() & 0xFFFF) | ((uint)next16() & 0xFFFF) << 16;
            uint personality = PersonalityFor(set.Nature, otId, next16);
            personalities.Add(personality);
            pokemon.Add(Build(set, personality, ivs, level));
        }
        return new FrontierOpponent { Number = trainer, Sets = sets, Pokemon = pokemon, OtId = otId, Personalities = personalities };
    }

    /// <summary>
    /// The look a Frontier trainer walks into a battle room with: the class's own of the original
    /// (<c>sTrainerClassToObjectID</c>) as one of this game's (<see cref="CharacterStyles.CharacterFor"/>). The Poké
    /// Kid's is the original's Pikachu, which here is a youngster in no costume.
    /// </summary>
    public static string LookOf(FrontierTrainerRecord trainer) => trainer.Class switch
    {
        // The Frontier Brains are their own looks (plan 11 · C6), whatever object the data gives them (a schoolboy's)
        "Tower Tycoon" => "palmer",
        "Factory Head" => "thorton",
        "Castle Valet" => "darach",
        "Hall Matron" => "argenta",
        "Arcade Star" => "dahlia",
        // The partners of the Battle Salon (BattleTowerChallenge.PartnerLooks) and everyone else are their object's own
        // look, or what it stands as
        _ => trainer.Look == "pikachu" ? CharacterStyles.Default.CharacterFor("youngster") : CharacterStyles.Default.CharacterFor(trainer.Look)
    };
}
