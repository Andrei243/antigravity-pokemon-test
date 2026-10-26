using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>What a leg of an <see cref="Itinerary"/> asks of the bot (plan 16 · T13).</summary>
public enum LegKind
{
    /// <summary>Step onto a trigger of a place, found by its script's name, and see its scene through.</summary>
    Trigger,
    /// <summary>Speak to someone of a place, found by their key, and see their script through.</summary>
    Talk,
    /// <summary>Come onto a map by its ways, and see whatever the arrival starts through.</summary>
    Enter,
    /// <summary>Stand on a tile of a map.</summary>
    Reach,
    /// <summary>Walk to and fro over a stretch of ground until the lead is of a level, seen to by a nurse when low.</summary>
    Train,
    /// <summary>Have the team seen to by the nurse of a Pokémon Center's map.</summary>
    Heal
}

/// <summary>
/// A leg of the way: what to do (<see cref="Kind"/>), on which map, who or what (<see cref="What"/>: a trigger's
/// script, a person's key), of which place's file (<see cref="Place"/>: the map of Sinnoh has many people of one key),
/// a tile (<see cref="X"/>, <see cref="Y"/>: a tile to reach, one end of a stretch to train on, whose other end is
/// <see cref="X2"/>, <see cref="Y2"/>), a level to train to and the Pokémon Center's map a trainer heals at.
/// </summary>
public sealed record Leg(LegKind Kind, string Map, string What = "", string? Place = null, int X = 0, int Y = 0, int X2 = 0, int Y2 = 0,
    int Level = 0, string? Center = null)
{
    /// <summary>
    /// What the leg is for, when seeing its scene through isn't enough (a Gym Leader's battle can be lost): the leg is
    /// tried again until the story holds it, and given up after <see cref="Itinerary.MostTries"/>.
    /// </summary>
    public Func<Story.StoryState, bool>? Until { get; init; }

    public static Leg Trigger(string map, string script, string? place = null) => new(LegKind.Trigger, map, script, place);
    public static Leg Talk(string map, string key, string? place = null) => new(LegKind.Talk, map, key, place);
    public static Leg Enter(string map) => new(LegKind.Enter, map);
    public static Leg Reach(string map, int x, int y) => new(LegKind.Reach, map, X: x, Y: y);
    public static Leg Heal(string center) => new(LegKind.Heal, center);
    public static Leg Train(int level, string map, int x, int y, int x2, int y2, string center) =>
        new(LegKind.Train, map, X: x, Y: y, X2: x2, Y2: y2, Level: level, Center: center);

    public override string ToString() => Kind switch
    {
        LegKind.Trigger or LegKind.Talk => $"{Kind} {What} on {Map}{(Place != null ? $" ({Place})" : "")}",
        LegKind.Reach => $"Reach {Map} {X},{Y}",
        LegKind.Train => $"Train to {Level} on {Map} {X},{Y}..{X2},{Y2}",
        _ => $"{Kind} {Map}"
    };
}

/// <summary>
/// The way through a stretch of the story for the playthrough bot (plan 16 · T13), GPU-free: legs in order, each a
/// thing a player does (a trigger stepped on, someone spoken to, a map come to, a level trained to), which the bot
/// goes for in place of its own goals while any are left (<see cref="Autoplayer.Itinerary"/>). The legs say only what
/// and where; the bot finds the way there by the field's own rules, through the game's own maps and ways, and plays
/// every scene, battle and question as it comes. A leg the bot can't complete within its tries is given up and named.
/// </summary>
public sealed class Itinerary
{
    /// <summary>How many times a leg is tried before it is given up.</summary>
    public const int MostTries = 4;

    private readonly List<Leg> legs;

    public Itinerary(IEnumerable<Leg> legs) => this.legs = legs.ToList();

    public IReadOnlyList<Leg> Legs => legs;

    /// <summary>The leg being gone for (<see cref="Legs"/>' index).</summary>
    public int Index { get; private set; }

    public Leg? Current => Index < legs.Count ? legs[Index] : null;
    public bool Finished => Index >= legs.Count;

    /// <summary>The current leg's thing has been done (the trigger stepped onto, the person spoken to); its end is awaited.</summary>
    public bool Taken { get; set; }

    /// <summary>A script has run since the current leg was taken.</summary>
    public bool SawScript { get; set; }

    /// <summary>How often the current leg has failed.</summary>
    public int Tries { get; set; }

    /// <summary>The legs done, in order, and those given up.</summary>
    public List<string> Done { get; } = new();
    public List<string> GivenUp { get; } = new();

    /// <summary>On to the next leg, the current one done (or given up).</summary>
    public void Advance(bool givenUp = false)
    {
        if (Current is { } leg) (givenUp ? GivenUp : Done).Add(leg.ToString());
        Index++;
        Taken = false;
        SawScript = false;
        Tries = 0;
    }

    /// <summary>
    /// The way from one map to another by the maps' own warps, the first map after <paramref name="from"/> on it, or
    /// null where none leads there (or the two are the same).
    /// </summary>
    public static string? NextMap(string from, string to, Func<string, Overworld.Map?> maps)
    {
        if (from == to) return null;
        var cameFrom = new Dictionary<string, string> { [from] = from };
        var queue = new Queue<string>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            string here = queue.Dequeue();
            var map = maps(here);
            if (map == null) continue;
            foreach (var warp in map.Warps)
            {
                string there = warp.TargetMap;
                if (warp.Shut || string.IsNullOrEmpty(there) || cameFrom.ContainsKey(there)) continue;
                cameFrom[there] = here;
                if (there == to)
                {
                    while (cameFrom[there] != from) there = cameFrom[there];
                    return there;
                }
                queue.Enqueue(there);
            }
        }
        return null;
    }
}
