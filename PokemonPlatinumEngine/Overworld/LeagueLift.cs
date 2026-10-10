using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Pokémon League's platform lifts (plan 01 · M9, part 2d), ported from the original's <c>src/platform_lift.c</c>:
/// a platform three tiles wide and two deep (<c>PLATFORM_LIFT_SIZE_X</c>, <c>_Y</c>) that stands at the foot of a shaft
/// and carries the player up it, once, never down again (<c>preventGoingDown</c>). Five stand in the passages between
/// the rooms of the Elite Four and the Champion's, and one in the Champion's own room, which only her script sets off.
/// <para>
/// It is the original's dynamic height plate: a floor over its six tiles at the platform's height, which
/// <see cref="FieldMovement"/> stands the walker on where it is nearer their own height than the room's ground under
/// it. The floor takes the top's height only once the platform has stopped (<c>DynamicTerrainHeightManager_SetHeight</c>
/// in the task's last state); while it rises the player is held on it by the game (<c>Player_SetYPos</c>).
/// </para>
/// </summary>
public sealed class LeagueLift : GymPuzzle
{
    /// <summary>A shaft of <c>sPerMapPlatformLiftConfiguration</c>: the platform's first tile and the heights of its two floors, in tiles.</summary>
    public sealed record Shaft(string Puzzle, int X, int Z, float Bottom, float Top);

    /// <summary>The platform's size in tiles (<c>PLATFORM_LIFT_SIZE_X</c>, <c>PLATFORM_LIFT_SIZE_Y</c>).</summary>
    public const int Width = 3, Depth = 2;

    /// <summary>
    /// How fast a platform of the League rises: two units of sixteen a frame at thirty frames a second
    /// (<c>heightIncrement = FX32_ONE * 2</c>; Iron Island's, the same code's other kind, rise at half that).
    /// </summary>
    public const float TilesPerSecond = 30f * 2f / 16f;

    public const string AaronName = "LeagueLiftAaron", BerthaName = "LeagueLiftBertha", FlintName = "LeagueLiftFlint",
        LucianName = "LeagueLiftLucian", ChampionName = "LeagueLiftChampion", ChampionRoomName = "LeagueLiftChampionRoom";

    /// <summary>The League's six shafts, by the name a map file gives its puzzle.</summary>
    public static readonly IReadOnlyList<Shaft> Shafts = new[]
    {
        new Shaft(AaronName, 3, 11, 0f, 10f),
        new Shaft(BerthaName, 3, 11, 0f, 5f),
        new Shaft(FlintName, 3, 11, 0f, 5f),
        new Shaft(LucianName, 3, 11, 0f, 5f),
        new Shaft(ChampionName, 3, 19, 0f, 5f),
        new Shaft(ChampionRoomName, 7, 8, 0f, 15f)
    };

    public static bool IsName(string name) => Find(name) != null;

    private static Shaft? Find(string name)
    {
        foreach (var shaft in Shafts)
            if (shaft.Puzzle == name) return shaft;
        return null;
    }

    public LeagueLift(string name) =>
        Where = Find(name) ?? throw new ArgumentException($"There is no League lift called '{name}'.");

    public override string Name => Where.Puzzle;

    /// <summary>The shaft this platform runs in.</summary>
    public Shaft Where { get; }

    /// <summary>Whether the platform is at the top, or on its way there (the original's <c>floorID</c>, turned over as it sets off).</summary>
    public bool AtTop { get; private set; }

    /// <summary>Where the platform is now, in tiles; between its floors while it rises.</summary>
    public float Height { get; private set; }

    /// <summary>The height its floor carries walkers at, which changes only once it has stopped.</summary>
    public float Floor { get; private set; }

    /// <summary>
    /// Whether the platform stood at the foot of the shaft as the player came in (<c>notUsedWhenEnteredMap</c>): the
    /// passage's own script asks, and its trigger sets the platform off only then.
    /// </summary>
    public bool NotUsedWhenEntered { get; private set; } = true;

    /// <summary>Whether the platform is rising.</summary>
    public bool Moving => Height != (AtTop ? Where.Top : Where.Bottom);

    public bool Covers(int x, int y) => x >= Where.X && x < Where.X + Width && y >= Where.Z && y < Where.Z + Depth;

    public override void Apply(Map map, StoryState story) { }

    /// <summary>
    /// The platform is laid out by where the player comes in (<c>PersistedMapFeatures_InitForPlatformLift</c>, by the
    /// warp's row: at the foot from the bottom warp, at the top from any other): here by the height of the ground
    /// they stand on, which is the same for both of the original's ways in.
    /// </summary>
    public override void ArriveAt(Map map, StoryState story, Random rng, int x, int y) => StandAt(map.HeightAt(x, y));

    /// <summary>
    /// Lays the platform out for someone standing at a height: at the top if that is nearer. The game gives the
    /// player's own height, which a save keeps, so a game saved on the platform at the top comes back there.
    /// </summary>
    public void StandAt(float height) => Settle(MathF.Abs(height - Where.Top) < MathF.Abs(height - Where.Bottom));

    /// <summary>The platform at one of its floors at once.</summary>
    public void Settle(bool top)
    {
        AtTop = top;
        Height = Floor = top ? Where.Top : Where.Bottom;
        NotUsedWhenEntered = !top;
    }

    /// <summary>
    /// Sets the platform off for the top (<c>PlatformLift_Trigger</c>). One at the top stays there: the League's
    /// never go down. Whether it set off.
    /// </summary>
    public bool Trigger()
    {
        if (AtTop) return false;
        AtTop = true;
        return true;
    }

    /// <summary>Moves the platform on; its floor takes the top's height once it stands still.</summary>
    public void Update(float dt)
    {
        if (!Moving) return;
        float target = AtTop ? Where.Top : Where.Bottom;
        float step = TilesPerSecond * dt;
        Height = MathF.Abs(target - Height) <= step ? target : Height + MathF.Sign(target - Height) * step;
        if (Height == target) Floor = target;
    }

    /// <summary>Takes the platform to where it is going at once (a game with no screen, a test, the harness).</summary>
    public void Finish() => Height = Floor = AtTop ? Where.Top : Where.Bottom;

    /// <summary>
    /// The floor over the platform's tiles: where the platform is now. Nobody takes a step while it rises (the game
    /// holds the field), so this is <see cref="Floor"/> for every step, and on the way up it is what whoever stands on
    /// the platform is drawn at.
    /// </summary>
    public override float? FloorAt(int x, int y, float near) => Covers(x, y) ? Height : null;
}
