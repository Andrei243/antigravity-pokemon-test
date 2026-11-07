using System;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>Where someone stands in an open map: the map's name, the place in millimetres and the yaw (plan 32 · OW1). What the save keeps.</summary>
public sealed record OpenPlace(string Map, OpenPos Pos, int Yaw)
{
    public int Yaw { get; init; } = OpenYaw.Normalize(Yaw);

    public override string ToString() => $"{Map} {Pos} yaw {Yaw}";
}

/// <summary>
/// The player in an open map (plan 32 · OW1): a place and a yaw. As OW1 builds it the player only stands; the capsule
/// that walks, runs, steps, slides and falls is plan 32 · OW3's (<c>OpenMovement</c>), which moves this place a tick at
/// a time. GPU-free.
/// </summary>
public sealed class OpenPlayer
{
    public OpenPos Pos { get; private set; }

    /// <summary>The way the player faces, 0 to 65,535 (<see cref="OpenYaw"/>).</summary>
    public int Yaw { get; private set; }

    public OpenPlayer(OpenPos pos, int yaw) => Place(pos, yaw);

    /// <summary>Puts the player down at a place, facing a way.</summary>
    public void Place(OpenPos pos, int yaw)
    {
        Pos = pos;
        Yaw = OpenYaw.Normalize(yaw);
    }
}

/// <summary>
/// The open field in force (plan 32 · OW1): the map, the player in it and the field's own clock. The game keeps one
/// field at a time (<c>GameFlow.Open</c>): while this is there the player is in an open map, and the grid's map and
/// player beside it are where the player last stood on the grid (a room's door, the region's arrival), which nothing
/// draws or walks meanwhile. GPU-free.
/// </summary>
public sealed class OpenField
{
    public OpenMap Map { get; }
    public OpenPlayer Player { get; }
    public OpenClock Clock { get; }

    public OpenField(OpenMap map, OpenPos pos, int yaw, OpenClock? clock = null)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Player = new OpenPlayer(pos, yaw);
        Clock = clock ?? new OpenClock();
    }

    /// <summary>Where the player stands, as the save keeps it.</summary>
    public OpenPlace Place => new(Map.Name, Player.Pos, Player.Yaw);

    /// <summary>
    /// A frame of the field: the ticks the frame brings, each run by <paramref name="tick"/> (the field's rules, which OW1
    /// has none of yet: the player stands). Returns the ticks run.
    /// </summary>
    public int Advance(double seconds, Action<OpenField>? tick = null)
    {
        int ticks = Clock.Advance(seconds);
        if (tick != null) for (int i = 0; i < ticks; i++) tick(this);
        return ticks;
    }
}
