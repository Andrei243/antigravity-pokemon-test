using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>What a honey tree is like when the player looks at it (<c>TREE_STATUS_*</c>).</summary>
public enum HoneyTreeStatus
{
    /// <summary>No honey on it, or its day is over.</summary>
    Bare = 1,
    /// <summary>Slathered, but six hours haven't passed.</summary>
    Slathered = 2,
    /// <summary>Six hours have passed: whatever the honey drew is waiting (or nothing came, which is found out by looking).</summary>
    Ready = 3
}

/// <summary>
/// The honey trees (plan 06 · R13; <c>src/overlay005/honey_tree.c</c>), with no drawing or input: one to each of 21
/// places, slathered with Honey from the south. A day's honey draws one group of Pokémon, chosen as it is slathered:
/// nothing one time in ten, the common table seven in ten and the uncommon two, or at one of the player's four
/// Munchlax trees nothing nine in a hundred, the common table twenty, the uncommon seventy and Munchlax one. Six
/// hours later a slot of the group's six is waiting (40, 20, 20, 10, 5 and 5 in a hundred) at a level from 5 to 15, and
/// the tree shakes as hard as the group says. Slathering the same tree twice running keeps the group nine times in
/// ten, drawing only a new slot and shake. The trees' clocks count minutes, a day from slathering.
/// </summary>
public static class HoneyTrees
{
    public const int Count = 21;

    /// <summary>A day's honey, in minutes; Pokémon come once <see cref="ReadyFrom"/> minutes are left (six hours have passed).</summary>
    public const int DayMinutes = 24 * 60;
    public const int ReadyFrom = 18 * 60;

    public const int MinLevel = 5, MaxLevel = 15;

    /// <summary>Each tree's place, by its number (<c>sHoneyTreeMapHeaderIDs</c>).</summary>
    public static readonly string[] Areas =
    {
        "route_205_south", "route_205_north", "route_206", "route_207", "route_208", "route_209", "route_210_south",
        "route_210_north", "route_211_east", "route_212_north", "route_212_south", "route_213", "route_214", "route_215",
        "route_218", "route_221", "route_222", "valley_windworks_outside", "eterna_forest_outside",
        "fuego_ironworks_outside", "floaroma_meadow"
    };

    /// <summary>The tree of a place, or null where there is none (<c>GetTreeIDFromMapHeaderID</c>).</summary>
    public static int? IdOf(string? area)
    {
        int id = area == null ? -1 : Array.IndexOf(Areas, area);
        return id >= 0 ? id : null;
    }

    /// <summary><c>HoneyTree_GetTreeSlatherStatus</c>.</summary>
    public static HoneyTreeStatus Status(HoneyTree tree) =>
        tree.MinutesLeft > 0 && tree.MinutesLeft <= ReadyFrom ? HoneyTreeStatus.Ready
        : tree.MinutesLeft != 0 ? HoneyTreeStatus.Slathered
        : HoneyTreeStatus.Bare;

    /// <summary>Whether the tree shakes when looked at: Pokémon have come, and its group gave it a shake (<c>GetShakingValue</c>).</summary>
    public static bool Shaking(HoneyTree tree) => Status(tree) == HoneyTreeStatus.Ready && tree.Shakes > 0;

    /// <summary>
    /// <c>HoneyTree_SlatherTree</c>: a day of honey on the tree, and what it will draw. The trainer's whole number
    /// (<paramref name="trainerId"/>, the card's number and its hidden half) decides the four Munchlax trees.
    /// </summary>
    public static void Slather(SpecialEncounters state, int id, uint trainerId, Random rng)
    {
        var tree = state.Trees[id];
        tree.MinutesLeft = DayMinutes;
        bool munchlax = IsMunchlaxTree(trainerId, id);
        if (state.LastSlathered == id && rng.Next(100) < 90)
        {
            tree.Slot = DrawSlot(rng);
            tree.Shakes = DrawShakes(tree.Group, rng);
            return;
        }
        tree.Group = DrawGroup(munchlax, rng);
        if (tree.Group != 0) tree.Slot = DrawSlot(rng);
        else
        {
            tree.Slot = 0;
            tree.MinutesLeft = 0;
        }
        tree.Shakes = DrawShakes(tree.Group, rng);
        state.LastSlathered = id;
    }

    /// <summary><c>GetTreeEncounterGroup</c>: 0 nothing, 1 the common table, 2 the uncommon, 3 Munchlax.</summary>
    public static int DrawGroup(bool munchlaxTree, Random rng)
    {
        int roll = rng.Next(100);
        if (munchlaxTree) return roll < 1 ? 3 : roll < 10 ? 0 : roll < 30 ? 1 : 2;
        return roll < 10 ? 0 : roll < 30 ? 2 : 1;
    }

    /// <summary><c>GetTreeEncounterSlot</c>: 40, 20, 20, 10, 5 and 5 in a hundred.</summary>
    public static int DrawSlot(Random rng)
    {
        int roll = rng.Next(100);
        return roll < 5 ? 5 : roll < 10 ? 4 : roll < 20 ? 3 : roll < 40 ? 2 : roll < 60 ? 1 : 0;
    }

    /// <summary><c>GetShakesFromGroup</c>: how hard the tree shakes, by the group it drew.</summary>
    public static int DrawShakes(int group, Random rng)
    {
        int roll = rng.Next(100);
        return group switch
        {
            3 => roll < 5 ? 2 : roll < 6 ? 1 : roll < 7 ? 0 : 3,
            2 => roll < 75 ? 2 : roll < 95 ? 1 : roll < 96 ? 0 : 3,
            1 => roll < 19 ? 2 : roll < 79 ? 1 : roll < 99 ? 0 : 3,
            _ => roll < 1 ? 2 : roll < 19 ? 1 : roll < 99 ? 0 : 3
        };
    }

    /// <summary>
    /// <c>IsMunchlaxTree</c>: the four bytes of the trainer's number, each taken modulo 21, are the player's Munchlax
    /// trees; a number that is one of the earlier ones moves on to the next tree (round to the first), so there are four.
    /// </summary>
    public static bool IsMunchlaxTree(uint trainerId, int id) => MunchlaxTrees(trainerId).Contains(id);

    public static int[] MunchlaxTrees(uint trainerId)
    {
        var trees = new int[4];
        for (int i = 0; i < 4; i++) trees[i] = (int)((trainerId >> (24 - 8 * i)) & 0xff) % Count;
        for (int i = 1; i < 4; i++)
            for (int j = 0; j < i; j++)
                if (trees[j] == trees[i])
                {
                    trees[i]++;
                    if (trees[i] >= Count) trees[i] = 0;
                }
        return trees;
    }

    /// <summary>The species waiting at a tree (<c>HoneyTree_GetSpecies</c>): its slot of the table its group drew.</summary>
    public static string SpeciesAt(HoneyTree tree, HoneyTreeTables tables)
    {
        var table = tree.Group switch { 3 => tables.Rare, 2 => tables.Uncommon, _ => tables.Common };
        return table.Count == 0 ? "" : table[Math.Clamp(tree.Slot, 0, table.Count - 1)];
    }

    /// <summary>
    /// The Pokémon met at a tree (<c>CreateWildMon_HoneyTree</c>): its species at a level from 5 to 15, the highest one
    /// time in two with Hustle, Vital Spirit or Pressure at the head of the party; neither Keen Eye nor a Repel keeps
    /// it away. The honey is gone from the tree (<c>HoneyTree_Unslather</c>).
    /// </summary>
    public static WildEncounterEntry Meet(SpecialEncounters state, int id, HoneyTreeTables tables, WildLead? lead, Random rng)
    {
        var tree = state.Trees[id];
        string species = SpeciesAt(tree, tables);
        int level = MinLevel + rng.Next(MaxLevel - MinLevel + 1);
        if (lead?.Ability is "Hustle" or "Vital Spirit" or "Pressure" && rng.Next(2) != 0) level = MaxLevel;
        tree.MinutesLeft = 0;
        return new WildEncounterEntry
        {
            SpeciesName = species,
            MinLevel = level,
            MaxLevel = level,
            Gender = WildEncounterRules.GenderFor(PokemonDatabase.Get(species), lead, rng),
            Nature = WildEncounterRules.NatureFor(lead, rng)
        };
    }

    /// <summary>Minutes have passed (<c>SpecialEncounter_DecrementHoneyTreeTimers</c>): every tree's honey is that much older, none below nothing.</summary>
    public static void MinutesPass(SpecialEncounters state, int minutes)
    {
        foreach (var tree in state.Trees.Where(t => t.MinutesLeft != 0))
            tree.MinutesLeft = Math.Max(0, tree.MinutesLeft - minutes);
    }

    /// <summary>
    /// Whether the player faces a honey tree from the south (<c>HoneyTree_TryInteract</c>): facing north, with a
    /// honey tree's model on the tile ahead, in a place that has a tree.
    /// </summary>
    public static int? Faced(Map map, int x, int y, Direction facing)
    {
        if (facing != Direction.Up || !map.InBounds(x, y - 1)) return null;
        if (!map.Props.Any(p => p.Type == PropType.HoneyTree && p.Covers(x, y - 1))) return null;
        return IdOf(map.AreaAt(x, y)?.Key);
    }
}
