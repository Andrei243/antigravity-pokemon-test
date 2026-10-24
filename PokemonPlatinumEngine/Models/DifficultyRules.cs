using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Challenge and Easy (plan 13 · V2; <c>docs/mechanics/rulings.md</c>, "Ways to play"): what a difficulty does to the
/// trainers the player meets, to the EXP a battle gives and to a shop's prices. GPU-free, and every number is our own
/// (Platinum has one difficulty). Normal changes nothing: <see cref="Prepare"/> hands back the very object it was given,
/// <see cref="ScaleExp"/> and <see cref="ShopPrice"/> the number.
///
/// <para>A trainer is changed on a <b>copy</b> (<see cref="Trainer.Copied"/>): the map's <see cref="Trainer"/> keeps the
/// team the import and the seeded <c>Dice</c> made, so the harness's shots and every test that loads a map are untouched.</para>
/// </summary>
public static class DifficultyRules
{
    /// <summary>How much a trainer matters to the story, which is how much a difficulty moves them.</summary>
    public enum Tier
    {
        /// <summary>Everyone else: a little.</summary>
        Everyone,

        /// <summary>The rival, a Commander, a Galactic Boss: next.</summary>
        Boss,

        /// <summary>A Gym Leader, one of the Elite Four, the Champion: most.</summary>
        Chief
    }

    /// <summary>The table by class: <see cref="Tier"/> of a trainer's class.</summary>
    public static Tier TierOf(string trainerClass) => trainerClass switch
    {
        "Leader" or "Elite Four" or "Champion" => Tier.Chief,
        "Commander" or "Galactic Boss" => Tier.Boss,
        _ when TrainerDatabase.IsRival(trainerClass) => Tier.Boss,
        _ => Tier.Everyone
    };

    /// <summary>The levels a tier is raised by under Challenge and lowered by under Easy.</summary>
    public static int LevelsOf(Tier tier) => tier switch
    {
        Tier.Chief => 4,
        Tier.Boss => 3,
        _ => 1
    };

    /// <summary>EXP under Easy is this much more (three halves), rounded down and never less than it was.</summary>
    public static int ScaleExp(int exp, Difficulty difficulty) =>
        difficulty == Difficulty.Easy ? Math.Max(exp, exp * 3 / 2) : exp;

    /// <summary>What a shop asks for one of an item: a quarter off under Easy (rounded down, never nothing for something that costs), as it is under the others.</summary>
    public static int ShopPrice(int price, Difficulty difficulty) =>
        difficulty == Difficulty.Easy && price > 0 ? Math.Max(1, price * 3 / 4) : price;

    /// <summary>Prize money under Challenge: half as much again.</summary>
    public static int PrizeMoney(int money, Difficulty difficulty) =>
        difficulty == Difficulty.Challenge ? money * 3 / 2 : money;

    /// <summary>Every routine of the trainer AI a trainer can think with (plan 06 · R9): not the tag battle's, which the battle adds itself, nor a wild Pokémon's.</summary>
    public const AiFlags EveryRoutine = AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert | AiFlags.SetupFirstTurn | AiFlags.Risky
        | AiFlags.PrioritizeExtremes | AiFlags.BatonPass | AiFlags.CheckHp | AiFlags.Weather | AiFlags.Harassment;

    /// <summary>What an important trainer's best Pokémon holds under Challenge when the data gave it nothing.</summary>
    public const string AceItem = "Sitrus Berry";

    /// <summary>What the Pokémon a Gym Leader, the Elite Four or the Champion gains under Challenge holds.</summary>
    public const string ExtraItem = "Leftovers";

    /// <summary>The level a species is held to be too young for its next stage when that stage needs no level (a stone, a trade, friendship).</summary>
    public const int UnleveledStage = 30;

    /// <summary>The same for an evolution by friendship, which comes early (Pichu, Togepi, Riolu).</summary>
    public const int FriendshipStage = 16;

    /// <summary>
    /// The trainer a battle is fought with under <paramref name="rules"/>: <paramref name="trainer"/> itself when the
    /// difficulty touches nothing (Normal, or no rules), otherwise a copy. Challenge raises every Pokémon by the table,
    /// gives a leader, one of the League and the Champion one more from the team the original gives them for a rematch,
    /// gives important trainers a held item, thinks with every routine of the AI and pays half as much again; Easy
    /// lowers the levels by the same table and changes nothing else of the trainer.
    /// </summary>
    public static Trainer Prepare(Trainer trainer, RunRules? rules)
    {
        var difficulty = rules?.Difficulty ?? Difficulty.Normal;
        if (difficulty == Difficulty.Normal) return trainer;

        var copy = trainer.Copied();
        var tier = TierOf(trainer.TrainerClass);
        int by = LevelsOf(tier);
        int cap = Ruleset.Current.LevelCap;
        foreach (var p in copy.Party.Members)
        {
            int level = difficulty == Difficulty.Challenge
                ? Math.Min(p.Level + by, Math.Max(p.Level, cap))
                : Math.Max(Math.Min(p.Level, 2), p.Level - by);
            p.Relevel(level);
        }
        if (difficulty == Difficulty.Easy) return copy;

        var extra = tier == Tier.Chief ? AddFromTheRematch(copy, trainer.Id) : null;
        if (tier != Tier.Everyone) GiveItems(copy, extra);
        copy.Ai |= EveryRoutine;
        copy.PrizeMoney = PrizeMoney(copy.PrizeMoney, difficulty);
        return copy;
    }

    /// <summary>
    /// One more Pokémon, from the species the original's rematch team of the same trainer (<c>leader_roark_rematch</c>)
    /// has and the team doesn't: the first of them whose line isn't on the team already, at the level of the team's best
    /// and at the stage that level allows (a Golem is a Geodude at 14), knowing what that level taught it.
    /// </summary>
    private static Pokemon? AddFromTheRematch(Trainer trainer, string id)
    {
        if (trainer.Party.Count == 0 || trainer.Party.Count >= Party.MaxSize) return null;
        if (TrainerDatabase.Get(id + "_rematch") is not { } rematch) return null;
        var ace = trainer.Party.Members.OrderBy(p => p.Level).Last();
        var lines = trainer.Party.Members.Select(p => RootOf(p.Species.Name)).ToHashSet();
        foreach (var m in rematch.Party)
        {
            string species = StageAt(m.Species, ace.Level);
            if (!lines.Add(RootOf(species))) continue;
            int iv = ace.IvHP;
            var record = new TrainerPokemonRecord
            {
                Species = species,
                Form = species == m.Species ? m.Form : null,
                Level = ace.Level,
                IvScale = (iv * 255 + 30) / 31,
                Personality = m.Personality
            };
            // Before the team's last, which stays the one that finishes the battle
            var added = TrainerDatabase.Build(record);
            trainer.Party.Insert(trainer.Party.Count > 1 ? trainer.Party.Count - 1 : trainer.Party.Count, added);
            return added;
        }
        return null;
    }

    /// <summary>The held items of the important: the best Pokémon a berry that heals it once, the Pokémon a leader or the League gained leftovers; nothing taken from one that has an item.</summary>
    private static void GiveItems(Trainer trainer, Pokemon? extra)
    {
        var ace = trainer.Party.Members.OrderBy(p => p.Level).Last();
        if (ace.HeldItem == null) ace.HeldItem = ItemDatabase.Get(AceItem);
        if (extra is { HeldItem: null }) extra.HeldItem = ItemDatabase.Get(ExtraItem);
    }

    // ---- evolution lines

    private static readonly Lazy<Dictionary<string, (string From, EvolutionData Way)>> Before = new(() =>
    {
        var before = new Dictionary<string, (string, EvolutionData)>(StringComparer.OrdinalIgnoreCase);
        foreach (var species in PokemonDatabase.GetAll().OrderBy(s => s.DexNumber))
            foreach (var way in species.Evolutions ?? new List<EvolutionData>())
                before.TryAdd(way.TargetSpecies, (species.Name, way));
        return before;
    });

    /// <summary>The first stage of a species' line.</summary>
    public static string RootOf(string species)
    {
        for (int guard = 0; guard < 4 && Before.Value.TryGetValue(species, out var step); guard++) species = step.From;
        return species;
    }

    /// <summary>
    /// The species of a line a Pokémon of this level is: <paramref name="species"/> itself unless the way to it asks for
    /// a higher level (or, with no level to ask, <see cref="FriendshipStage"/> for friendship and <see cref="UnleveledStage"/> for the rest), then the stage before, and so on.
    /// </summary>
    public static string StageAt(string species, int level)
    {
        for (int guard = 0; guard < 4 && Before.Value.TryGetValue(species, out var step); guard++)
        {
            int needs = step.Way.Level > 0 ? step.Way.Level
                : step.Way.Method is EvolutionMethod.Friendship or EvolutionMethod.FriendshipDay or EvolutionMethod.FriendshipNight ? FriendshipStage : UnleveledStage;
            if (needs <= level) break;
            species = step.From;
        }
        return species;
    }
}
