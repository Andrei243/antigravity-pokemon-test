using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What decides play, hashed (plan 16 · T19): the part of plan 16 · T17's <see cref="StateSnapshot"/> that a recorded
/// game must come to again when it is played back, in groups, each a 32-bit FNV-1a of its fields written out as text:
/// the <see cref="GameState"/>; where the player stands (the map, the tile and the facing, and in an open map the place to the millimetre, the yaw and the field's ticks); the team (species, form,
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
        // (and where the body stands between tiles in a free region, plan 30 · FM7: nought everywhere else, and left out then,
        // so the grid's rows are what they were)
        var offset = player?.Offset ?? (0, 0);
        string between = offset == (0, 0) ? "" : $" {Num(offset.X)} {Num(offset.Y)}";
        row[1] = map != null && player != null ? Fnv(string.Join(' ', map.Name, Num(player.GridX), Num(player.GridY), player.Facing.ToString()) + between) : 0;
        // In an open map (plan 32 · OW1) the place is the open one, to the millimetre, with the yaw and the field's ticks,
        // after the grid's place it was entered from; on the grid this is left out, so the grid's rows are what they were
        if (flow.Open is { } open)
        {
            var at = open.Player.Pos;
            // and its people and its follower where they stand (OW6), each who is there in the map's order
            var people = new StringBuilder();
            foreach (var p in open.Map.People)
                if (p.Present) people.Append(' ').Append(p.Key).Append(' ').Append(Num(p.Pos.X)).Append(' ').Append(Num(p.Pos.Y)).Append(' ').Append(Num(p.Pos.Z)).Append(' ').Append(Num(p.Yaw));
            // and the mount ridden with its stance (plan 30 · FM12; nothing on foot, so a walker's rows are what they were)
            if (open.Player.Body.Mounted) people.Append(" mounted ").Append(open.Player.Body.Stance.ToString());
            if (open.Follower is { } follower) people.Append(" follower ").Append(Num(follower.Pos.X)).Append(' ').Append(Num(follower.Pos.Y)).Append(' ').Append(Num(follower.Pos.Z));
            // and its wild Pokémon where they stand (OW7), in the order they came out
            foreach (var w in open.Wild.All)
                people.Append(" wild ").Append(w.Species).Append(' ').Append(Num(w.Level)).Append(' ').Append(Num(w.Pos.X)).Append(' ').Append(Num(w.Pos.Y)).Append(' ').Append(Num(w.Pos.Z)).Append(' ').Append(Num(w.Yaw));
            row[1] = Fnv(string.Join(' ', row[1].ToString(CultureInfo.InvariantCulture), "open", open.Map.Name, Num(at.X), Num(at.Y), Num(at.Z), Num(open.Player.Yaw),
                open.Clock.Tick.ToString(CultureInfo.InvariantCulture)) + people);
        }

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
