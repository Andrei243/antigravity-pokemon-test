using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>One thing a grotto may hold: a Pokémon of a species at a level between two, or an item; <see cref="Weight"/> says how often against the others.</summary>
public sealed class GrottoEntry
{
    public string? Species { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public string? Item { get; set; }
    public int Weight { get; set; } = 1;

    public bool IsPokemon => Species != null;
}

/// <summary>
/// A Hidden Grotto (plan 22 · U11): its number, the area its mouth is in, the mouth's tile on its map (a thing of the map,
/// <see cref="Overworld.NPC.GrottoHollowType"/>; the player stands on the tile south of it) and what it may hold.
/// </summary>
public sealed class Grotto
{
    public int Id { get; set; }
    public string Area { get; set; } = "";
    public string Map { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public List<GrottoEntry> Table { get; set; } = new();
}

/// <summary>The file of Unova's grottoes, <c>Data/world/unova/grottoes.json</c>, written by hand.</summary>
public sealed class GrottoFile
{
    public List<Grotto> Grottoes { get; set; } = new();
}

/// <summary>
/// Black 2's Hidden Grottoes (plan 22 · U11; <c>docs/mechanics/rulings.md</c>, "The Hidden Grottoes"), GPU-free: a hollow in
/// a tree wall that holds one thing at a time, a Pokémon with its hidden ability or an item. A grotto is full the first time
/// it is come to; once what it held is taken (a Pokémon met, whatever came of the battle, or an item picked up) it is empty,
/// and every <see cref="StepsPerRoll"/> steps the player walks after that it has a <see cref="RefillChance"/> in a hundred
/// of filling again, rolled as the grotto is next come to (so the steps walked anywhere count, as the original counts
/// them). What a grotto holds is drawn from its table by weight. Nothing of it is saved but the story's own variables
/// (<see cref="Vars"/>: what each holds and the step count it was emptied at), as the Battle Subway's run is, so a save
/// needs nothing new; the steps are the game's own record (<see cref="Pastimes.Steps"/>). Every draw is on the
/// <see cref="Random"/> the caller hands over (the game's <c>Dice</c>), so the same seed rolls the same.
/// </summary>
public static class HiddenGrottoes
{
    /// <summary>The steps between one roll for a refill and the next.</summary>
    public const int StepsPerRoll = 256;

    /// <summary>The chance in a hundred that an empty grotto fills at each roll.</summary>
    public const int RefillChance = 5;

    /// <summary>The most rolls made at one coming: past it a grotto is as good as sure to have filled.</summary>
    public const int MostRolls = 200;

    /// <summary>What a grotto's content variable holds once it is empty.</summary>
    public const int Empty = 999;

    /// <summary>The story's variables the grottoes keep.</summary>
    public static class Vars
    {
        /// <summary>The grotto the player is in or last went into, by its number.</summary>
        public const string Current = "VAR_UNOVA_GROTTO";
        /// <summary>What a grotto holds: 0 before it was ever come to, an entry of its table plus one, or <see cref="Empty"/>.</summary>
        public static string Content(int id) => $"VAR_UNOVA_GROTTO_{id}";
        /// <summary>The player's steps (<see cref="Pastimes.Steps"/>) when a grotto was emptied.</summary>
        public static string EmptiedAt(int id) => $"VAR_UNOVA_GROTTO_{id}_STEPS";
    }

    private static IReadOnlyList<Grotto>? all;

    /// <summary>Unova's grottoes, from the hand-written file.</summary>
    public static IReadOnlyList<Grotto> All => all ??= GameDataFiles.Load<GrottoFile>("world/unova/grottoes.json").Grottoes;

    /// <summary>The grotto of that number, or null.</summary>
    public static Grotto? Get(int id) => All.FirstOrDefault(g => g.Id == id);

    /// <summary>The grotto whose mouth stands on that tile of that map, or null.</summary>
    public static Grotto? MouthAt(string map, int x, int y) => All.FirstOrDefault(g => g.Map == map && g.X == x && g.Y == y);

    /// <summary>An entry of a table drawn by weight.</summary>
    public static int Draw(Grotto grotto, Random rng)
    {
        int total = grotto.Table.Sum(e => Math.Max(1, e.Weight));
        int roll = rng.Next(total);
        for (int n = 0; n < grotto.Table.Count; n++)
        {
            roll -= Math.Max(1, grotto.Table[n].Weight);
            if (roll < 0) return n;
        }
        return grotto.Table.Count - 1;
    }

    /// <summary>
    /// A grotto as it is come to: full the first time, and when empty filled again by a roll for every
    /// <see cref="StepsPerRoll"/> steps walked since it was emptied (the first that comes up). Returns what it holds now,
    /// or null when it is empty.
    /// </summary>
    public static GrottoEntry? Visit(StoryState story, Grotto grotto, int steps, Random rng)
    {
        int content = story.Var(Vars.Content(grotto.Id));
        if (content == 0 || content > grotto.Table.Count && content != Empty)
        {
            story.SetVar(Vars.Content(grotto.Id), Draw(grotto, rng) + 1);
        }
        else if (content == Empty)
        {
            int rolls = Math.Min(MostRolls, Math.Max(0, steps - story.Var(Vars.EmptiedAt(grotto.Id))) / StepsPerRoll);
            for (int n = 0; n < rolls; n++)
            {
                if (rng.Next(100) >= RefillChance) continue;
                story.SetVar(Vars.Content(grotto.Id), Draw(grotto, rng) + 1);
                break;
            }
        }
        return Holds(story, grotto);
    }

    // ------------------------------------------------------------------ the rustle (plan 22 · U11)

    /// <summary>How near the player, in tiles each way, a grotto's rustle is heard and its leaves seen to stir.</summary>
    public const int RustleReach = 5;

    /// <summary>The seconds of the field's clock between one rustle and the next.</summary>
    public const double RustleEvery = 2.4;

    /// <summary>
    /// Whether a grotto rustles: one never come to (it is full), one that holds something, and an empty one once enough
    /// steps have been walked since it was emptied that a roll could have filled it (a rustle says it may hold something,
    /// not that it does: the roll is still made as it is come to, so nothing is drawn here).
    /// </summary>
    public static bool Rustles(StoryState story, Grotto grotto, int steps)
    {
        int content = story.Var(Vars.Content(grotto.Id));
        if (content != Empty) return true;
        return steps - story.Var(Vars.EmptiedAt(grotto.Id)) >= StepsPerRoll;
    }

    /// <summary>The nearest grotto of a map within <see cref="RustleReach"/> of a tile that rustles, or null (the lowest number among equals).</summary>
    public static Grotto? RustlingNear(StoryState story, string map, int x, int y, int steps) =>
        All.Where(g => g.Map == map && Math.Abs(g.X - x) <= RustleReach && Math.Abs(g.Y - y) <= RustleReach && Rustles(story, g, steps))
            .OrderBy(g => Math.Max(Math.Abs(g.X - x), Math.Abs(g.Y - y))).ThenBy(g => g.Id).FirstOrDefault();

    /// <summary>What a grotto holds as the story stands, without a roll: null when it is empty or never come to.</summary>
    public static GrottoEntry? Holds(StoryState story, Grotto grotto)
    {
        int content = story.Var(Vars.Content(grotto.Id));
        return content >= 1 && content <= grotto.Table.Count ? grotto.Table[content - 1] : null;
    }

    /// <summary>Empties a grotto, at the step count the player has walked.</summary>
    public static void Take(StoryState story, Grotto grotto, int steps)
    {
        story.SetVar(Vars.Content(grotto.Id), Empty);
        story.SetVar(Vars.EmptiedAt(grotto.Id), steps);
    }

    /// <summary>
    /// A grotto's Pokémon: its level drawn between the entry's two, and its species' hidden ability where the species has
    /// one (the form's, where a form has its own).
    /// </summary>
    public static Pokemon Make(PokemonSpecies species, GrottoEntry entry, Random rng)
    {
        int level = entry.MinLevel + rng.Next(Math.Max(0, entry.MaxLevel - entry.MinLevel) + 1);
        var pokemon = new Pokemon(species, Math.Clamp(level, 1, 100), rng);
        if (pokemon.HiddenAbilityName is { Length: > 0 } hidden) pokemon.AbilityName = hidden;
        return pokemon;
    }
}
