using System;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>Where someone stands in an open map: the map's name, the place in millimetres and the yaw (plan 32 · OW1). What the save keeps.</summary>
public sealed record OpenPlace(string Map, OpenPos Pos, int Yaw)
{
    public int Yaw { get; init; } = OpenYaw.Normalize(Yaw);

    public override string ToString() => $"{Map} {Pos} yaw {Yaw}";
}

/// <summary>
/// The player in an open map (plan 32 · OW1, OW3): the capsule (<see cref="OpenBody"/>) that walks, runs, steps,
/// slides, falls and swims a tick at a time, and where it stands as the save keeps it (<see cref="Pos"/>, whole
/// millimetres, and <see cref="Yaw"/>). The body keeps micrometres between ticks (decision 11); a place put down or
/// loaded from the save starts it afresh on that millimetre. GPU-free.
/// </summary>
public sealed class OpenPlayer
{
    /// <summary>The capsule.</summary>
    public OpenBody Body { get; private set; }

    /// <summary>Where the feet are, to the millimetre.</summary>
    public OpenPos Pos => Body.Pos;

    /// <summary>The way the player faces, 0 to 65,535 (<see cref="OpenYaw"/>).</summary>
    public int Yaw => Body.Yaw;

    public OpenPlayer(OpenPos pos, int yaw) => Body = new OpenBody(pos, yaw);

    /// <summary>Puts the player down at a place, facing a way: the capsule begun afresh there, settled on the ground at its next tick.</summary>
    public void Place(OpenPos pos, int yaw) => Body = new OpenBody(pos, yaw);

    /// <summary>A tick of the field: the capsule stepped on the map's ground by the stick.</summary>
    public void Step(OpenMap map, OpenInput input, MovementGrants grants) => Body.Step(map.Ground, input, grants);
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

    /// <summary>What the region lets the player do beyond walking (swimming, plan 32 · OW3), as the engine has it from the story.</summary>
    public MovementGrants Grants { get; set; }

    public OpenField(OpenMap map, OpenPos pos, int yaw, OpenClock? clock = null)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Player = new OpenPlayer(pos, yaw);
        Clock = clock ?? new OpenClock();
    }

    /// <summary>Where the player stands, as the save keeps it.</summary>
    public OpenPlace Place => new(Map.Name, Player.Pos, Player.Yaw);

    /// <summary>
    /// A frame of the field: the ticks the frame brings, each running the field's rules (OW3: the player's capsule,
    /// steered the same way every tick of the frame by <paramref name="input"/>) and then <paramref name="tick"/>, if
    /// given. Returns the ticks run.
    /// </summary>
    public int Advance(double seconds, OpenInput input, Action<OpenField>? tick = null)
    {
        int ticks = Clock.Advance(seconds);
        for (int i = 0; i < ticks; i++)
        {
            Player.Step(Map, input, Grants);
            tick?.Invoke(this);
        }
        return ticks;
    }

    /// <summary>A frame with the stick at rest (the player stands, or falls and slides as the ground has it).</summary>
    public int Advance(double seconds, Action<OpenField>? tick = null) => Advance(seconds, OpenInput.None, tick);
}
