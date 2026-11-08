using System;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// How the people of an open map move by themselves (plan 32 · OW6, the grid's <c>Wandering</c> at metre scale): a
/// wanderer waits a while and then walks to a place drawn within their bounds, over the navigation mesh by polygons
/// whose hub is within the bounds, and walks no drop and swims no water, so they never leave the bounds nor anywhere
/// they couldn't walk back from; someone who looks about turns a quarter turn now and then; someone still stands.
/// Only people within <see cref="Reach"/> of the player move, and nobody starts a move while a script or a walk up
/// has the field (the field says so). Their choices are drawn from a generator of each person's own, seeded by the
/// map's and their names (never <c>Dice</c>, so no other draw of the game moves for them, and two runs of the same
/// ticks move them the same). GPU-free.
/// </summary>
public static class OpenWandering
{
    /// <summary>How near the player someone must be, in millimetres, to move by themselves (the grid's 64 tiles, as metres).</summary>
    public const int Reach = 64_000;

    /// <summary>The wait between two moves, in ticks: from one second to four.</summary>
    public const int ShortestWait = OpenClock.TicksPerSecond, LongestWait = 4 * OpenClock.TicksPerSecond;

    /// <summary>The fewest millimetres a leg of wandering goes: a place drawn nearer is drawn again.</summary>
    public const int ShortestLeg = 1000;

    /// <summary>
    /// A tick of someone who isn't walked by anything: their capsule stepped at rest, and, where <paramref name="mayStart"/>,
    /// the wait counted down and the next move begun.
    /// </summary>
    public static void Step(OpenMap map, OpenPerson p, bool mayStart)
    {
        p.Body.Step(map.Ground, OpenInput.None);
        if (!mayStart || p.Held || p.Moves == OpenPersonMoves.Still) return;
        if (p.Wait > 0)
        {
            p.Wait--;
            return;
        }
        var draws = p.Draws ??= new Core.SplitMix(Seed(map.Name, p.Key));
        p.Wait = ShortestWait + draws.Int(LongestWait - ShortestWait + 1);
        if (p.Moves == OpenPersonMoves.LookAbout)
        {
            p.Body.Face(OpenYaw.Normalize(p.Yaw + (draws.Int(3) + 1) * (OpenYaw.Turn / 4)));
            return;
        }
        // A wanderer's next place: drawn within the bounds' box until one is within them and a leg away
        int minX = int.MaxValue, minZ = int.MaxValue, maxX = int.MinValue, maxZ = int.MinValue;
        foreach (var (x, z) in p.Bounds) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minZ = Math.Min(minZ, z); maxZ = Math.Max(maxZ, z); }
        var at = p.Pos;
        for (int tries = 0; tries < 6; tries++)
        {
            int x = minX + (int)(draws.Next() % (uint)Math.Max(1, maxX - minX));
            int z = minZ + (int)(draws.Next() % (uint)Math.Max(1, maxZ - minZ));
            var to = new OpenPos(x, at.Y, z);
            if (!OpenPolygons.Holds(p.Bounds, x, z) || at.GroundDistanceSquared(to) < (long)ShortestLeg * ShortestLeg) continue;
            // The ground there, within a few metres of the feet: a place on another layer (the cave under the hill) is no place to wander to
            if (map.Ground.Footing(x / 1000.0, z / 1000.0, at.MetresY + 3, at.MetresY - 3, walkableOnly: true) is not { } ground) continue;
            to = new OpenPos(x, OpenPos.Millimetres(ground.Y), z);
            var mesh = map.Land?.Nav;
            var walk = OpenPathWalk.Over(map, at, to, OpenWalkKind.Wander, allowed: OpenNavFlags.Walk,
                barred: mesh == null ? null : polygon => !p.InBounds(mesh.Polygons[polygon].Hub), straight: mesh == null);
            if (walk == null) continue;
            p.Walk = walk;
            return;
        }
    }

    /// <summary>A person's own seed: the FNV-1a of the map's name and theirs (a string's own hash changes from run to run).</summary>
    public static uint Seed(string map, string key)
    {
        uint h = 2166136261;
        foreach (char c in map + "/" + key) { h ^= c; h *= 16777619; }
        return h;
    }
}
