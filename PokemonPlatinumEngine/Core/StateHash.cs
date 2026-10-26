using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What decides play, hashed (plan 16 · T19): the part of plan 16 · T17's <see cref="StateSnapshot"/> that a recorded
/// game must come to again when it is played back, in groups, each a 32-bit FNV-1a of its fields written out as text:
/// the <see cref="GameState"/>; where the player stands (the map, the tile and the facing); the team (species, form,
/// level, HP, status and each move's PP); the bag and its counts; the money; the story's revision; and in a battle,
/// the core's turn and every battler's HP. Nothing drawn and no cursor is in it, so a chunk or a sprite baked later on
/// a slower machine never parts a replay. A movie keeps one row of it each second of its own time, and playback
/// names the groups that part (<see cref="Parted"/>). GPU-free.
/// </summary>
public static class StateHash
{
    /// <summary>The groups' names, in the order of a row: what a parting names.</summary>
    public static readonly IReadOnlyList<string> Groups = new[] { "state", "place", "party", "bag", "money", "story", "battle" };

    /// <summary>A row of the hash: one number a group, in the order of <see cref="Groups"/>.</summary>
    public static uint[] Of(GameFlow flow)
    {
        var row = new uint[Groups.Count];
        row[0] = Fnv(flow.State.ToString());

        var map = flow.Map;
        var player = flow.Player;
        row[1] = map != null && player != null ? Fnv(string.Join(' ', map.Name, Num(player.GridX), Num(player.GridY), player.Facing.ToString())) : 0;

        var team = new StringBuilder();
        foreach (var p in flow.Party.Members)
        {
            team.Append(p.Species.Name).Append('/').Append(p.Form).Append(' ').Append(Num(p.Level)).Append(' ')
                .Append(Num(p.CurrentHP)).Append(' ').Append(p.Status.ToString()).Append(p.IsEgg ? " egg" : "");
            foreach (var m in p.Moves) team.Append(' ').Append(m.Data.Name).Append(':').Append(Num(m.CurrentPP));
            team.Append(';');
        }
        row[2] = Fnv(team.ToString());

        var bag = new StringBuilder();
        foreach (var stack in flow.Bag.AllItems) bag.Append(stack.Name).Append(' ').Append(Num(stack.Quantity)).Append(';');
        row[3] = Fnv(bag.ToString());

        row[4] = Fnv(Num(flow.Money));
        row[5] = Fnv(Num(flow.Story.Revision));

        if (flow.Battle?.Core is { } core)
        {
            var fight = new StringBuilder(Num(core.Turn));
            foreach (var b in core.AllBattlers) fight.Append(' ').Append(b.Pokemon == null ? "-" : Num(b.Pokemon.CurrentHP));
            row[6] = Fnv(fight.ToString());
        }
        return row;
    }

    /// <summary>The groups whose numbers differ between two rows (a row cut short counts its missing groups as differing).</summary>
    public static IEnumerable<string> Parted(IReadOnlyList<uint> expected, IReadOnlyList<uint> found)
    {
        for (int i = 0; i < Groups.Count; i++)
        {
            uint? a = i < expected.Count ? expected[i] : null, b = i < found.Count ? found[i] : null;
            if (a != b) yield return Groups[i];
        }
    }

    private static string Num(int n) => n.ToString(CultureInfo.InvariantCulture);

    /// <summary>FNV-1a of 32 bits over the text's UTF-8 bytes.</summary>
    public static uint Fnv(string text)
    {
        uint hash = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(text))
        {
            hash ^= b;
            hash *= 16777619;
        }
        return hash;
    }
}
