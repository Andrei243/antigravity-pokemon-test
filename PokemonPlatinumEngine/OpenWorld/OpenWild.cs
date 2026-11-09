using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>What a wild Pokémon of an open map is doing (plan 32 · OW7).</summary>
public enum OpenWildMood
{
    /// <summary>Wandering its zone, or standing between two legs of it.</summary>
    Roaming,

    /// <summary>Running from the player (a shy one).</summary>
    Fleeing,

    /// <summary>Coming over to look (a curious one).</summary>
    Coming,

    /// <summary>Standing a few metres off and watching the player (a curious one, come).</summary>
    Watching,

    /// <summary>Running at the player (an aggressive one).</summary>
    Charging
}

/// <summary>Why a wild Pokémon of an open map left the field (plan 32 · OW7): what the zone's wait before the next is chosen by.</summary>
public enum OpenWildEnd
{
    /// <summary>It was caught, or felled: the zone waits <see cref="OpenWildField.RespawnAfterBattle"/> before another comes.</summary>
    CaughtOrFelled,

    /// <summary>The battle ended otherwise (the player ran, or it did): <see cref="OpenWildField.RespawnAfterFlight"/>.</summary>
    Otherwise
}

/// <summary>
/// A wild Pokémon standing in an open map (plan 32 · OW7): what its zone's table drew (the row, the species and the
/// level, decided as it came out), the capsule it walks with (<see cref="Body"/>, the player's own rules, no grants:
/// a person of the open map's kind), its temperament and what it is doing. The Pokémon itself is made only as it is
/// met, through the game's own chance (<c>GameEngine.MakeWild</c>), so coming out draws nothing from the dice. GPU-free.
/// </summary>
public sealed class OpenWildPokemon
{
    /// <summary>Its number in its field, in the order they came out: what it is told apart by.</summary>
    public int Id { get; }

    /// <summary>The zone it came out in, and the row of the zone's table it is.</summary>
    public OpenZone Zone { get; }
    public OpenSlot Slot { get; }

    /// <summary>The species (or form) and the level it is met at, before the region's levels map it (<c>LevelHere</c>).</summary>
    public string Species => Slot.Species;
    public int Level { get; }

    public OpenWildKind Kind { get; }
    public OpenWildMood Mood { get; internal set; }

    public OpenBody Body { get; }
    public OpenPos Pos => Body.Pos;
    public int Yaw => Body.Yaw;

    /// <summary>Where it came out.</summary>
    public OpenPos Home { get; }

    /// <summary>What walks it now (a leg of its wandering, its flight, its coming over or its charge), or null.</summary>
    public OpenPathWalk? Walk { get; internal set; }

    // Ticks until it next thinks of a leg; ticks until it next asks its senses; ticks until a chase is laid afresh
    internal int Wait, SenseIn, Relay;

    // Its own generator, for its legs: seeded by its map, its zone and its number
    internal readonly SplitMix Draws;

    internal OpenWildPokemon(int id, OpenZone zone, OpenSlot slot, int level, OpenWildKind kind, OpenPos at, int yaw, uint seed)
    {
        Id = id;
        Zone = zone;
        Slot = slot;
        Level = level;
        Kind = kind;
        Home = at;
        Body = new OpenBody(at, yaw);
        Draws = new SplitMix(seed);
    }

    public override string ToString() => $"{Species} {Level} in {Zone.Name} at {Pos} ({Mood})";
}

/// <summary>
/// The wild Pokémon of an open map (plan 32 · OW7; plan 32, "Encounters"): every one is seen, standing in its zone,
/// as the originals have them, and none is met in the grass by a roll. After the player's every tick
/// (<see cref="OpenField.StepTheRest"/>) each zone near the player brings one out now and then, out of the player's
/// view, up to what its density and cap allow; each walks the navigation mesh by its temperament (it wanders its zone,
/// and once it has seen or smelt the player it runs, comes over to look or charges); one too far from the player is
/// gone; and one the player walks into, or that runs into the player, is met (<see cref="Touching"/>, which the engine
/// asks). Whatever it decides is drawn from generators of its own, seeded by the map's, the zone's names and the
/// field's own count of ticks (never <c>Dice</c>), so two runs of the same ticks place the same Pokémon in the same
/// places and no other draw of the game moves for them. GPU-free; ticks only.
/// </summary>
public sealed class OpenWildField
{
    /// <summary>Ticks between a zone's tries at bringing one out (a quarter of a second).</summary>
    public const int SpawnEvery = OpenClock.TicksPerSecond / 4;

    /// <summary>Places drawn at each try before the zone gives up till the next.</summary>
    public const int TriesPerSpawn = 4;

    /// <summary>
    /// How near the player a place may be for one to come out there, in millimetres: no nearer than
    /// <see cref="SpawnNear"/>, no farther than <see cref="SpawnFar"/>, and never within <see cref="AheadHalfAngle"/> of
    /// the way the player faces (where the camera, behind them, looks): out of view. The rules never read the camera.
    /// </summary>
    public const int SpawnNear = 12_000, SpawnFar = 56_000, AheadHalfAngle = OpenYaw.Turn * 75 / 360;

    /// <summary>One farther than this from the player, in millimetres, is gone.</summary>
    public const int DespawnReach = 72_000;

    /// <summary>The waits before a zone brings one out again, in ticks: after one caught or felled (thirty seconds), and after one run from (ten).</summary>
    public const int RespawnAfterBattle = 30 * OpenClock.TicksPerSecond, RespawnAfterFlight = 10 * OpenClock.TicksPerSecond;

    /// <summary>
    /// Ticks after a battle (two seconds) in which nothing senses the player and nothing meets them, so the player back
    /// from a battle is not walked straight into the next.
    /// </summary>
    public const int Grace = 2 * OpenClock.TicksPerSecond;

    /// <summary>How near on the ground, in millimetres, a wild Pokémon and the player meet: their capsules' radii and a tenth of a metre.</summary>
    public const int TouchReach = (int)(OpenBody.Radius * 2 * OpenPos.PerMetre) + 100;

    /// <summary>How far apart in height, in millimetres, the two may stand and still meet (not one on a ledge over the other).</summary>
    public const int TouchHeight = 1200;

    /// <summary>Ticks between asks of a wild Pokémon's senses (a tenth of a second), and between a chase's paths laid afresh (a quarter).</summary>
    public const int SenseEvery = OpenClock.TicksPerSecond / 10, RelayEvery = OpenClock.TicksPerSecond / 4;

    /// <summary>How far a leg of wandering goes at most, in millimetres, and how far a flight runs.</summary>
    public const int LegReach = 8_000, FleeReach = 12_000;

    /// <summary>A pursuer gives up once the player is this many times its sight away (or its scent, where it has no sight).</summary>
    public const int GiveUpFactor = 2;

    private readonly List<OpenWildPokemon> all = new();
    private readonly Dictionary<string, int> waits = new(StringComparer.Ordinal);
    private int made, grace;

    /// <summary>Those standing in the map now, in the order they came out.</summary>
    public IReadOnlyList<OpenWildPokemon> All => all;

    /// <summary>The field's own count of ticks (every tick the field's rules run, never reset while the field lasts): what the zones' draws are seeded by.</summary>
    public long Tick { get; private set; }

    /// <summary>The time of day the zones' tables are read at (the engine's, from the game's clock; day unless told).</summary>
    public TimeOfDay Now { get; set; } = TimeOfDay.Day;

    /// <summary>The temperaments (the game's file unless told: a test's own).</summary>
    public OpenWildKinds? Kinds { get; set; }

    /// <summary>Ticks of grace left (<see cref="Grace"/>).</summary>
    public int GraceLeft => grace;

    /// <summary>How many a zone wants standing at once: its area in square metres by its density to a thousand, at least one and at most its cap.</summary>
    public static int TargetOf(OpenZone zone)
    {
        long twice = 0;
        var p = zone.Polygon;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) twice += (long)p[j].X * p[i].Z - (long)p[i].X * p[j].Z;
        // Square millimetres to square metres, by the density to a thousand of them, rounded half up
        long squareMetres = Math.Abs(twice) / 2 / 1_000_000;
        long want = (squareMetres * zone.Density + 500) / 1000;
        return (int)Math.Clamp(want, 1, zone.Cap);
    }

    /// <summary>Whether a row of a table is met at a time of day: one with no time always; morning only in the morning; day by day and at twilight; night at night and late at night.</summary>
    public static bool MetAt(OpenSlot slot, TimeOfDay now) => slot.Time switch
    {
        OpenSlotTime.Morning => now == TimeOfDay.Morning,
        OpenSlotTime.Day => now is TimeOfDay.Day or TimeOfDay.Twilight,
        OpenSlotTime.Night => now is TimeOfDay.Night or TimeOfDay.LateNight,
        _ => true
    };

    /// <summary>A zone's own seed: the FNV-1a of the map's name and the zone's (a string's own hash changes from run to run).</summary>
    public static uint Seed(string map, string zone) => SplitMix.Fnv(map + "/" + zone);

    /// <summary>
    /// A tick of the field's wild Pokémon, after the player's and the people's: the far ones gone, each zone near the
    /// player perhaps bringing one out, and each stepped by what walks it and, where <paramref name="mayAct"/>, by its
    /// temperament. Nothing comes out and nobody starts a move while a script or a trainer's walk up has the field.
    /// </summary>
    public void Step(OpenField field, bool mayAct)
    {
        Tick++;
        var map = field.Map;
        if (all.Count == 0 && map.Zones.Count == 0) return;
        var player = field.Player;
        var at = player.Pos;
        if (grace > 0) grace--;

        for (int i = all.Count - 1; i >= 0; i--)
            if (all[i].Pos.GroundDistanceSquared(at) > (long)DespawnReach * DespawnReach)
                all.RemoveAt(i);

        if (mayAct)
            foreach (var zone in map.Zones) Spawn(map, zone, player);

        // Whether the player runs this tick: farther than a walk's stride between their last two ticks
        long stride = (long)(OpenBody.WalkSpeed * 1.2 * OpenPos.PerMetre / OpenClock.TicksPerSecond);
        bool running = player.Before.GroundDistanceSquared(at) > stride * stride;
        foreach (var w in all)
        {
            if (w.Walk is { } walk)
            {
                w.Body.Step(map.Ground, walk.InputFor(w.Body));
                if (walk.After(w.Body) && ReferenceEquals(w.Walk, walk)) w.Walk = null;
            }
            else w.Body.Step(map.Ground, OpenInput.None);
            if (mayAct) Think(map, w, at, running);
        }
    }

    /// <summary>
    /// The wild Pokémon the player touches now (the first in the order they came out), or null: within
    /// <see cref="TouchReach"/> on the ground and <see cref="TouchHeight"/> in height, out of the grace after a battle.
    /// </summary>
    public OpenWildPokemon? Touching(OpenPos player)
    {
        if (grace > 0) return null;
        foreach (var w in all)
            if (w.Pos.GroundDistanceSquared(player) <= (long)TouchReach * TouchReach && Math.Abs(w.Pos.Y - player.Y) <= TouchHeight)
                return w;
        return null;
    }

    /// <summary>
    /// Whether the player meets one from behind: facing within an eighth of a turn of the way it faces (the grid's
    /// back strike, <c>FieldPokemon.FromBehind</c>, at metre scale), which is a back strike under the later rules.
    /// </summary>
    public static bool FromBehind(int playerYaw, int wildYaw) => Math.Abs(OpenYaw.Between(playerYaw, wildYaw)) <= OpenYaw.Turn / 8;

    /// <summary>
    /// One met leaves the field (the engine's, as the battle begins), and its zone waits before it brings another out:
    /// <see cref="RespawnAfterBattle"/> or <see cref="RespawnAfterFlight"/> as the battle ended (<see cref="Ended"/>),
    /// and until then as for one caught.
    /// </summary>
    public void Met(OpenWildPokemon wild)
    {
        all.Remove(wild);
        waits[wild.Zone.Name] = Math.Max(waits.GetValueOrDefault(wild.Zone.Name), RespawnAfterBattle);
    }

    /// <summary>How the battle with one met ended: its zone's wait set by it, and the grace begun.</summary>
    public void Ended(OpenZone zone, OpenWildEnd end)
    {
        waits[zone.Name] = end == OpenWildEnd.CaughtOrFelled ? RespawnAfterBattle : RespawnAfterFlight;
        grace = Grace;
    }

    /// <summary>The grace begun without a battle (the player put down, a scene's end): nothing senses or meets them for a while.</summary>
    public void Calm() => grace = Grace;

    /// <summary>Every one gone and every zone's wait forgotten (another map come to, the field begun afresh).</summary>
    public void Clear()
    {
        all.Clear();
        waits.Clear();
        grace = 0;
    }

    /// <summary>
    /// One put down by hand (a test's, a tool's): the zone's row and level as given, at a place, facing a way. It is a
    /// wild Pokémon of the field like any that came out.
    /// </summary>
    public OpenWildPokemon Put(OpenMap map, OpenZone zone, OpenSlot slot, int level, OpenPos at, int yaw)
    {
        var w = new OpenWildPokemon(made, zone, slot, level, (Kinds ?? OpenWildKinds.Game).For(slot.Species), at, yaw,
            SplitMix.Mix(Seed(map.Name, zone.Name), (uint)made, 0x5EED));
        made++;
        // It stands a while before its first leg
        w.Wait = 2 * OpenClock.TicksPerSecond;
        all.Add(w);
        return w;
    }

    /// <summary>The zone's wait in ticks before it may bring another out (nought when it may).</summary>
    public int WaitOf(OpenZone zone) => waits.GetValueOrDefault(zone.Name);

    // ------------------------------------------------------------------ coming out

    private void Spawn(OpenMap map, OpenZone zone, OpenPlayer player)
    {
        int wait = waits.GetValueOrDefault(zone.Name);
        if (wait > 0)
        {
            waits[zone.Name] = wait - 1;
            return;
        }
        if (Tick % SpawnEvery != 0) return;
        int standing = 0;
        foreach (var w in all) if (w.Zone == zone) standing++;
        if (standing >= TargetOf(zone)) return;
        // The rows met at this time of day, and their weights
        int total = 0;
        foreach (var slot in zone.Slots) if (MetAt(slot, Now)) total += slot.Weight;
        if (total == 0) return;

        int minX = int.MaxValue, minZ = int.MaxValue, maxX = int.MinValue, maxZ = int.MinValue;
        foreach (var (x, z) in zone.Polygon) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minZ = Math.Min(minZ, z); maxZ = Math.Max(maxZ, z); }
        var at = player.Pos;
        // Not worth a try where every corner of the zone's box is past the farthest a Pokémon comes out
        long nearestX = Math.Max(0, Math.Max(minX - (long)at.X, (long)at.X - maxX)), nearestZ = Math.Max(0, Math.Max(minZ - (long)at.Z, (long)at.Z - maxZ));
        if (nearestX * nearestX + nearestZ * nearestZ > (long)SpawnFar * SpawnFar) return;

        uint seed = Seed(map.Name, zone.Name);
        for (int attempt = 0; attempt < TriesPerSpawn; attempt++)
        {
            var draws = new SplitMix(SplitMix.Mix(seed, (uint)Tick, (uint)attempt));
            int x = minX + (int)(draws.Next() % (uint)Math.Max(1, maxX - minX));
            int z = minZ + (int)(draws.Next() % (uint)Math.Max(1, maxZ - minZ));
            if (!OpenPolygons.Holds(zone.Polygon, x, z)) continue;
            long dx = x - (long)at.X, dz = z - (long)at.Z, d2 = dx * dx + dz * dz;
            if (d2 < (long)SpawnNear * SpawnNear || d2 > (long)SpawnFar * SpawnFar) continue;
            if (Math.Abs(OpenYaw.Between(player.Yaw, OpenTriangles.YawOf(dx, dz))) <= AheadHalfAngle) continue;
            // Walkable ground between the zone's heights, on the navigation mesh where the map has one (never swum water)
            if (map.Ground.Footing(x / 1000.0, z / 1000.0, zone.MaxY / 1000.0, zone.MinY / 1000.0, walkableOnly: true) is not { } ground) continue;
            var pos = new OpenPos(x, OpenPos.Millimetres(ground.Y), z);
            if (!zone.Contains(pos)) continue;
            if (map.Land?.Nav is { } mesh && (mesh.PolygonAt(pos) is not { } polygon || mesh.Polygons[polygon].Swim)) continue;
            if (map.Ground.WaterAt(pos.MetresX, pos.MetresZ) is { } water && water > ground.Y + 0.1) continue;
            // The row, by weight among those met now, and the level within its range
            int pick = draws.Int(total);
            OpenSlot? row = null;
            foreach (var slot in zone.Slots)
            {
                if (!MetAt(slot, Now)) continue;
                if ((pick -= slot.Weight) < 0) { row = slot; break; }
            }
            if (row == null) return;
            int level = row.MinLevel + draws.Int(row.MaxLevel - row.MinLevel + 1);
            int yaw = (int)(draws.Next() & (OpenYaw.Turn - 1));
            var w = new OpenWildPokemon(made, zone, row, level, (Kinds ?? OpenWildKinds.Game).For(row.Species), pos, yaw, SplitMix.Mix(seed, (uint)made, 0x5EED));
            made++;
            // Its first leg after a short wait of its own
            w.Wait = OpenClock.TicksPerSecond / 2 + w.Draws.Int(OpenClock.TicksPerSecond * 2);
            w.SenseIn = w.Draws.Int(SenseEvery);
            all.Add(w);
            return;
        }
    }

    // ------------------------------------------------------------------ what each does

    private void Think(OpenMap map, OpenWildPokemon w, OpenPos player, bool running)
    {
        var kind = w.Kind;
        // The senses, every tenth of a second, out of the grace
        if (grace == 0 && kind.Notices && --w.SenseIn <= 0)
        {
            w.SenseIn = SenseEvery;
            long d2 = w.Pos.GroundDistanceSquared(player);
            bool sensed = Senses(map, w, player, running, d2);
            int give = Math.Max(kind.Sight, kind.Scent) * GiveUpFactor;
            switch (w.Mood)
            {
                case OpenWildMood.Roaming when sensed:
                    Notice(map, w, player);
                    return;
                case OpenWildMood.Fleeing when w.Walk == null:
                    // Run on while it still senses the player; rest otherwise
                    if (sensed) Flee(map, w, player);
                    else w.Mood = OpenWildMood.Roaming;
                    return;
                case OpenWildMood.Coming or OpenWildMood.Watching or OpenWildMood.Charging when d2 > (long)give * give:
                    w.Mood = OpenWildMood.Roaming;
                    w.Walk = null;
                    return;
            }
        }
        switch (w.Mood)
        {
            case OpenWildMood.Charging:
                if (--w.Relay <= 0 || w.Walk == null) Chase(map, w, player, run: true, stopShort: OpenPathWalk.Within);
                return;
            case OpenWildMood.Coming or OpenWildMood.Watching:
            {
                long d2 = w.Pos.GroundDistanceSquared(player);
                long keep = kind.Keep + 500;
                if (d2 <= keep * keep)
                {
                    // Come: it stands and watches, facing the player
                    w.Mood = OpenWildMood.Watching;
                    w.Walk = null;
                    if (w.Pos.X != player.X || w.Pos.Z != player.Z) w.Body.Face(OpenTriangles.YawOf(player.X - w.Pos.X, player.Z - w.Pos.Z));
                }
                else if (--w.Relay <= 0 || w.Walk == null)
                {
                    w.Mood = OpenWildMood.Coming;
                    Chase(map, w, player, run: false, stopShort: kind.Keep);
                }
                return;
            }
            case OpenWildMood.Fleeing:
                return;
        }
        // Roaming: a wait, then a leg within its zone
        if (w.Walk != null) return;
        if (w.Wait > 0)
        {
            w.Wait--;
            return;
        }
        w.Wait = OpenClock.TicksPerSecond + w.Draws.Int(OpenClock.TicksPerSecond * 3);
        Wander(map, w);
    }

    /// <summary>Whether a wild Pokémon senses the player: smelt within its scent (twice as far for one who runs), or seen within its cone with nothing of the ground between.</summary>
    private static bool Senses(OpenMap map, OpenWildPokemon w, OpenPos player, bool running, long d2)
    {
        var kind = w.Kind;
        if (Math.Abs(w.Pos.Y - player.Y) > 3000) return false;
        long scent = (long)kind.Scent * (running ? 2 : 1);
        if (scent > 0 && d2 <= scent * scent) return true;
        if (kind.Sight <= 0) return false;
        var cone = new OpenSight(kind.Sight, kind.HalfAngle);
        return cone.InCone(w.Pos, w.Yaw, player) && OpenSight.Clear(map.Ground, w.Pos, player);
    }

    private static void Notice(OpenMap map, OpenWildPokemon w, OpenPos player)
    {
        switch (w.Kind.Temperament)
        {
            case OpenTemperament.Shy:
                Flee(map, w, player);
                break;
            case OpenTemperament.Curious:
                w.Mood = OpenWildMood.Coming;
                Chase(map, w, player, run: false, stopShort: w.Kind.Keep);
                break;
            case OpenTemperament.Aggressive:
                w.Mood = OpenWildMood.Charging;
                Chase(map, w, player, run: true, stopShort: OpenPathWalk.Within);
                break;
        }
    }

    /// <summary>A walk at the player over the mesh, laid afresh every <see cref="RelayEvery"/> ticks as the player moves.</summary>
    private static void Chase(OpenMap map, OpenWildPokemon w, OpenPos player, bool run, int stopShort)
    {
        w.Relay = RelayEvery;
        w.Walk = OpenPathWalk.Over(map, w.Pos, player, OpenWalkKind.Wander, run, stopShort: stopShort, allowed: OpenNavFlags.Walk | OpenNavFlags.Drop,
            straight: map.Land?.Nav == null);
    }

    /// <summary>
    /// A run away from the player: to the place <see cref="FleeReach"/> on, straight away from them, or the first of
    /// the ways a quarter turn either side of it the mesh has a path to.
    /// </summary>
    private static void Flee(OpenMap map, OpenWildPokemon w, OpenPos player)
    {
        w.Mood = OpenWildMood.Fleeing;
        w.Walk = null;
        double dx = w.Pos.X - player.X, dz = w.Pos.Z - player.Z;
        double length = Math.Sqrt(dx * dx + dz * dz);
        if (length < 1) (dx, dz, length) = (0, 1, 1);
        // Straight away, then an eighth and a quarter turn either side (turned by √½ and by swapping: the square root only)
        double ux = dx / length, uz = dz / length, h = Math.Sqrt(0.5);
        var ways = new (double X, double Z)[]
        {
            (ux, uz), (h * (ux + uz), h * (uz - ux)), (h * (ux - uz), h * (uz + ux)), (uz, -ux), (-uz, ux)
        };
        foreach (var (wx, wz) in ways)
        {
            int x = w.Pos.X + (int)Math.Round(wx * FleeReach, MidpointRounding.AwayFromZero), z = w.Pos.Z + (int)Math.Round(wz * FleeReach, MidpointRounding.AwayFromZero);
            if (map.Ground.Footing(x / 1000.0, z / 1000.0, w.Pos.MetresY + 4, w.Pos.MetresY - 4, walkableOnly: true) is not { } ground) continue;
            var to = new OpenPos(x, OpenPos.Millimetres(ground.Y), z);
            if (OpenPathWalk.Over(map, w.Pos, to, OpenWalkKind.Wander, run: true, allowed: OpenNavFlags.Walk | OpenNavFlags.Drop, straight: map.Land?.Nav == null) is { } walk)
            {
                w.Walk = walk;
                return;
            }
        }
    }

    /// <summary>A leg of wandering: a place drawn within its zone and <see cref="LegReach"/> of its feet, over polygons whose hub is in the zone, walking only.</summary>
    private static void Wander(OpenMap map, OpenWildPokemon w)
    {
        var zone = w.Zone;
        var at = w.Pos;
        for (int tries = 0; tries < 4; tries++)
        {
            int x = at.X - LegReach + w.Draws.Int(LegReach * 2 + 1), z = at.Z - LegReach + w.Draws.Int(LegReach * 2 + 1);
            if (!OpenPolygons.Holds(zone.Polygon, x, z) || at.GroundDistanceSquared(new OpenPos(x, at.Y, z)) < (long)OpenWandering.ShortestLeg * OpenWandering.ShortestLeg) continue;
            if (map.Ground.Footing(x / 1000.0, z / 1000.0, at.MetresY + 3, at.MetresY - 3, walkableOnly: true) is not { } ground) continue;
            var to = new OpenPos(x, OpenPos.Millimetres(ground.Y), z);
            if (!zone.Contains(to)) continue;
            var mesh = map.Land?.Nav;
            var walk = OpenPathWalk.Over(map, at, to, OpenWalkKind.Wander, allowed: OpenNavFlags.Walk,
                barred: mesh == null ? null : polygon => !zone.Contains(mesh.Polygons[polygon].Hub), straight: mesh == null);
            if (walk == null) continue;
            w.Walk = walk;
            return;
        }
    }
}
