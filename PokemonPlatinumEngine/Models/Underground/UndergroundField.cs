using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Underground;

/// <summary>Why a Sphere or a trap couldn't be put in the ground (the original's refusals, in its order).</summary>
public enum BuryResult
{
    Done,
    InTheBase,
    SomeoneThere,
    Wall,
    SomethingThere,
    NoRoom
}

/// <summary>What a pulse of the radar found round a tile: the walls that hold something, the traps and the buried Spheres, the nearest first and eight of each at most.</summary>
public sealed record RadarPulse(IReadOnlyList<Spot> Walls, IReadOnlyList<Spot> Traps, IReadOnlyList<Spot> Spheres);

/// <summary>
/// The tunnels' rules (plan 06 · R16a), on the saved state and on what the map says is rock: where the day's walls
/// and traps are spread (<c>MiningEnv_Init</c>, <c>Mining_SpawnMiningSpotsAndTraps</c>, <c>Traps_SpawnRandomTrap</c>),
/// Spheres buried and dug up (<c>src/underground/spheres.c</c>), traps set, disarmed and stepped on
/// (<c>src/underground/traps.c</c>), and the radar's pulse. No drawing or input.
/// </summary>
public sealed class UndergroundField
{
    /// <summary>The tunnels' own part of the map: the top two rows of rooms and the left column are the bases' (<c>UNDERGROUND_MAIN_AREA_START</c>, <c>UNDERGROUND_MAX</c>).</summary>
    public const int MainLeft = 32, MainTop = 64, MaxX = 479, MaxZ = 479;

    /// <summary>How far the radar's pulse reaches, in steps (<c>TOUCH_RADAR_RADIUS</c>), and how many of a kind it shows.</summary>
    public const int RadarReach = 6, RadarMost = 8;

    /// <summary>The traps the day spreads about, each as likely as the next (<c>Traps_SpawnRandomTrap</c>).</summary>
    public static readonly string[] SpreadTraps =
    {
        "TRAP_MOVE_UP", "TRAP_MOVE_RIGHT", "TRAP_MOVE_DOWN", "TRAP_MOVE_LEFT", "TRAP_HURL_UP", "TRAP_HURL_RIGHT", "TRAP_HURL_DOWN",
        "TRAP_HURL_LEFT", "TRAP_HOLE", "TRAP_PIT", "TRAP_REVERSE", "TRAP_CONFUSE", "TRAP_SMOKE", "TRAP_BIG_SMOKE", "TRAP_BUBBLE",
        "TRAP_FLOWER", "TRAP_ALERT_1", "TRAP_ALERT_2", "TRAP_ALERT_3", "TRAP_ALERT_4", "TRAP_EMBER", "TRAP_FIRE", "TRAP_ROCK",
        "TRAP_ROCKFALL", "TRAP_FOAM", "TRAP_LEAF"
    };

    private readonly UndergroundState state;
    private readonly Func<int, int, bool> rock;

    /// <param name="rock">Whether a tile of the map can't be walked: its walls (<c>TerrainCollisionManager_CheckCollision</c>).</param>
    public UndergroundField(UndergroundState state, Func<int, int, bool> rock)
    {
        this.state = state;
        this.rock = rock;
    }

    /// <summary><c>UndergroundMan_AreCoordinatesInSecretBase</c>: anywhere that isn't strictly inside the tunnels' own part.</summary>
    public static bool InABase(int x, int z) => !(x > MainLeft && z > MainTop && x < MaxX && z < MaxZ);

    /// <summary>
    /// The tile of the tunnels under a tile of the overworld (<c>MapChangeUndergroundContext_New</c>): the Underground
    /// is half the region's size, a chunk of it lying under four of the overworld's, and whoever goes down lands on
    /// one of four tiles of that chunk by the quarter they stood in.
    /// </summary>
    public static (int X, int Z) Below(int surfaceX, int surfaceY)
    {
        const int t = 32;
        int matrixX = Math.Max(0, surfaceX / t - 1), matrixZ = Math.Max(0, surfaceY / t - 6);
        int x = matrixX % 2 == 0 ? 8 : 23, z = matrixZ % 2 == 0 ? 8 : 23;
        return ((matrixX / 2 + 1) * t + x, (matrixZ / 2 + 3) * t + z);
    }

    // ------------------------------------------------------------------ the day's spread

    /// <summary><c>Mining_CanSpawnMiningSpotOnCoordinates</c>: rock of the tunnels with open floor on one side of it.</summary>
    public bool CanHoldTreasure(int x, int z) =>
        !InABase(x, z) && z <= MaxZ - 1 && x <= MaxX - 1 && rock(x, z) && (!rock(x, z + 1) || !rock(x, z - 1) || !rock(x + 1, z) || !rock(x - 1, z));

    /// <summary>
    /// A new day's walls and traps, drawn from the day's number, if the day asks for them (<c>MiningEnv_Init</c>): a
    /// wall near every buried Sphere, then fourteen clusters of six to eleven walls and up to five traps each.
    /// </summary>
    public bool SpreadIfDue()
    {
        if (!state.SpreadAnew) return false;
        var rand = new Rand16(state.Seed);
        state.MiningSpots.Clear();
        state.SpawnedTraps.Clear();

        void AddSpot(int x, int z)
        {
            if (state.MiningSpots.Count < UndergroundState.MaxMiningSpots) state.MiningSpots.Add(new Spot(x, z));
        }

        // Spheres_SpawnMiningSpotsNearBuriedSpheres
        foreach (var sphere in state.Buried)
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = rand.Next(20) + sphere.X - 10, z = rand.Next(20) + sphere.Z - 10;
                if (!CanHoldTreasure(x, z)) continue;
                AddSpot(x, z);
                break;
            }

        // 25 less a tenth of the most Spheres that can be buried, less one (the original means the Spheres buried and counts the room for them)
        int clusters = 25 - UndergroundState.MaxBuriedSpheres / 10 - 1;
        const int range = (15 - 2) * 32;
        for (int cluster = 0; cluster < clusters; cluster++)
        {
            int centreX, centreZ;
            do
            {
                centreX = rand.Next(range) + MainLeft;
                centreZ = rand.Next(range) + MainTop;
            } while (!CanHoldTreasure(centreX, centreZ));

            int count = rand.Next(6) + 6;
            for (int i = 0; i < count; i++)
            {
                bool found = false;
                int x = 0, z = 0;
                for (int attempt = 0; attempt < 100 && !found; attempt++)
                {
                    x = rand.Next(20) + centreX - 10;
                    z = rand.Next(20) + centreZ - 10;
                    found = CanHoldTreasure(x, z);
                }
                if (!found) continue;
                if (state.MiningSpots.Count >= UndergroundState.MaxMiningSpots) break;
                AddSpot(x, z);
            }

            count = rand.Next(6);
            for (int i = 0; i < count; i++)
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    int x = rand.Next(20) + centreX - 10, z = rand.Next(20) + centreZ - 10;
                    if (rock(x, z) || state.SpawnedTraps.Count >= UndergroundState.MaxSpawnedTraps) continue;
                    int trap = UndergroundDatabase.TrapId(SpreadTraps[rand.Next(SpreadTraps.Length)]);
                    if (TrapAt(x, z) != null) continue;
                    state.SpawnedTraps.Add(new BuriedTrap { Trap = trap, X = x, Z = z });
                    break;
                }
        }
        state.SpreadAnew = false;
        return true;
    }

    // ------------------------------------------------------------------ what lies where

    public Spot? MiningSpotAt(int x, int z) => state.MiningSpots.FirstOrDefault(s => s.X == x && s.Z == z);
    public BuriedSphere? SphereAt(int x, int z) => state.Buried.FirstOrDefault(s => s.X == x && s.Z == z);
    public BuriedTrap? TrapAt(int x, int z) =>
        state.PlacedTraps.FirstOrDefault(t => t.X == x && t.Z == z) ?? state.SpawnedTraps.FirstOrDefault(t => t.X == x && t.Z == z);

    /// <summary>A wall that holds something is dug at once: it is gone from the map as the game begins (<c>Mining_ProcessConfirmStartMiningResult</c>).</summary>
    public bool StartMining(int x, int z)
    {
        var spot = MiningSpotAt(x, z);
        return spot != null && state.MiningSpots.Remove(spot);
    }

    // ------------------------------------------------------------------ Spheres

    /// <summary>
    /// <c>Spheres_TryBurySphere</c>: the Sphere at a place of the bag goes into the floor ahead. Onto a Sphere of its
    /// own kind it joins it: the larger of the two, a fifth of the smaller and one more. The hundred-and-first
    /// Sphere takes the oldest one's place.
    /// </summary>
    public BuryResult Bury(int slot, int x, int z, bool someoneThere)
    {
        if (slot < 0 || slot >= state.Spheres.Count) return BuryResult.NoRoom;
        var sphere = state.Spheres[slot];
        if (InABase(x, z)) return BuryResult.InTheBase;
        if (someoneThere) return BuryResult.SomeoneThere;
        if (rock(x, z)) return BuryResult.Wall;
        if (SphereAt(x, z) is { } there)
        {
            if (there.Kind != sphere.Kind) return BuryResult.SomethingThere;
            there.Size = Math.Min(UndergroundState.MaxSphereSize,
                there.Size > sphere.Size ? there.Size + sphere.Size / 5 + 1 : sphere.Size + there.Size / 5 + 1);
        }
        else
        {
            if (state.Buried.Count >= UndergroundState.MaxBuriedSpheres) state.Buried.RemoveAt(0);
            state.Buried.Add(new BuriedSphere { Kind = sphere.Kind, X = x, Z = z, Size = sphere.Size });
        }
        state.Spheres.RemoveAt(slot);
        return BuryResult.Done;
    }

    /// <summary>A buried Sphere dug up into the bag, grown (<c>Spheres_ProcessRetrieveBuriedSphereRequest</c>); null where none lies or the bag has no room.</summary>
    public BuriedSphere? DigUp(int x, int z)
    {
        if (SphereAt(x, z) is not { } sphere || !state.TryAddSphere(sphere.Kind, sphere.Worth)) return null;
        state.Buried.Remove(sphere);
        return sphere;
    }

    // ------------------------------------------------------------------ traps

    /// <summary><c>Traps_TryPlaceTrap</c>: the trap at a place of the bag is set under the floor ahead; the player's own traps number sixteen at most.</summary>
    public BuryResult SetTrap(int slot, int x, int z, bool someoneThere)
    {
        if (slot < 0 || slot >= state.Traps.Count) return BuryResult.NoRoom;
        if (someoneThere) return BuryResult.SomeoneThere;
        if (InABase(x, z)) return BuryResult.InTheBase;
        if (rock(x, z)) return BuryResult.Wall;
        if (TrapAt(x, z) != null) return BuryResult.SomethingThere;
        if (state.PlacedTraps.Count >= UndergroundState.MaxPlacedTraps) return BuryResult.NoRoom;
        state.PlacedTraps.Add(new BuriedTrap { Trap = state.Traps[slot], X = x, Z = z });
        state.Traps.RemoveAt(slot);
        return BuryResult.Done;
    }

    /// <summary>
    /// A trap seen in the floor ahead is taken up into the bag (<c>Traps_TryDisengageTrap</c>): the trap's number, 0
    /// where none lies, or -1 where the bag has no room and it stays where it is.
    /// </summary>
    public int Disarm(int x, int z)
    {
        if (TrapAt(x, z) is not { } trap) return 0;
        if (state.Traps.Count >= UndergroundState.MaxTraps) return -1;
        Lift(trap);
        state.Traps.Add(trap.Trap);
        return trap.Trap;
    }

    /// <summary>
    /// A step onto a tile (<c>CheckPlayerSteppedOnTrap</c>): the trap under it goes off and is gone. Stepping on a
    /// trap counts in the records, on a trap of one's own setting too.
    /// </summary>
    public int StepOn(int x, int z)
    {
        if (TrapAt(x, z) is not { } trap) return 0;
        Lift(trap);
        state.Records.TrapsTriggered = UndergroundRecords.Add(state.Records.TrapsTriggered, 1);
        return trap.Trap;
    }

    private void Lift(BuriedTrap trap)
    {
        if (!state.PlacedTraps.Remove(trap)) state.SpawnedTraps.Remove(trap);
    }

    // ------------------------------------------------------------------ the radar

    /// <summary>
    /// A pulse round a tile (<c>UndergroundMan_TouchRadarSearch</c>): what lies within six steps, ring by ring from
    /// the middle, eight of a kind at most.
    /// </summary>
    public RadarPulse Pulse(int x, int z)
    {
        List<Spot> Find(Func<int, int, bool> at)
        {
            var found = new List<Spot>();
            for (int ring = 0; ring <= RadarReach && found.Count < RadarMost; ring++)
                for (int dz = -ring; dz <= ring && found.Count < RadarMost; dz++)
                {
                    int dx = ring - Math.Abs(dz);
                    if (at(x + dx, z + dz)) found.Add(new Spot(x + dx, z + dz));
                    if (dx != 0 && found.Count < RadarMost && at(x - dx, z + dz)) found.Add(new Spot(x - dx, z + dz));
                }
            return found;
        }
        return new RadarPulse(Find((a, b) => MiningSpotAt(a, b) != null), Find((a, b) => TrapAt(a, b) != null), Find((a, b) => SphereAt(a, b) != null));
    }
}
