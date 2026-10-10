using System;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Pastoria Gym's water (plan 01 · M9), ported from the original's <c>gym_features.c</c>. A floor floats on the
/// water over most of the room (the original's dynamic height plate, <c>DynamicTerrainHeightManager_SetPlate(0, 1,
/// 2, 25, 38)</c>), at nought, two or four tiles: stepping on a button of each colour sets it (<c>PastoriaGym_PressButton</c>:
/// the blue raises it to four, the green to two, the orange lowers it to nought), and it is laid out with the green
/// pressed as the player comes in (<c>PastoriaGym_DynamicMapFeaturesInit</c>, by the room's own script).
/// <para>
/// Where it carries someone is the original's rule for heights (<c>TerrainCollisionManager</c>'s, the floor against
/// the room's own plates): the floor is stood on only where it is above the plate under the tile and nearer to the
/// walker's height, the plate winning a tie; nobody is carried onto a tile of the floor's own collision behaviour
/// (<see cref="TileBehavior.MovingFloor"/>, <c>TILE_BEHAVIOR_DYNAMIC_HEIGHT_COLLISION</c>: deep water). And a tile of
/// the high, middle or low ground is stepped onto only from the height its name says
/// (<c>PastoriaGym_DynamicMapFeaturesCheckCollision</c>): the high ground from nought, the middle from two, the low
/// from four, whatever the water does.
/// </para>
/// </summary>
public sealed class PastoriaWater : GymPuzzle
{
    public const string PuzzleName = "PastoriaWater";
    public override string Name => PuzzleName;

    /// <summary>The original's three buttons; its scripts call the orange one yellow.</summary>
    public enum Button { Blue, Green, Orange }

    /// <summary>The tiles the floor covers (<c>DynamicTerrainHeightManager_SetPlate</c>'s x, z, width and depth).</summary>
    public const int Left = 1, Top = 2, Width = 25, Depth = 38;

    /// <summary>
    /// Where the buttons are (the room's coordinate events, each running its colour's script while the variable it
    /// goes by is nought): the blue at (3, 6) and (9, 24), the green at four places, the orange at four.
    /// </summary>
    public static readonly (int X, int Y, Button Colour)[] Buttons =
    {
        (3, 6, Button.Blue), (9, 24, Button.Blue),
        (17, 9, Button.Green), (19, 13, Button.Green), (19, 24, Button.Green), (10, 30, Button.Green),
        (9, 14, Button.Orange), (19, 22, Button.Orange), (23, 31, Button.Orange), (3, 34, Button.Orange)
    };

    /// <summary>
    /// How fast the water moves: one unit of sixteen a frame at thirty frames a second (the press tasks'
    /// <c>FX32_ONE</c>), after the buttons' own press has played (the tasks wait for their animations' loop).
    /// </summary>
    public const float TilesPerSecond = 30f / 16f, PressSeconds = 0.4f;

    /// <summary>The floor's height for a button: <c>PASTORIA_WATER_HEIGHT_LOW</c>, <c>MIDDLE</c> and <c>HIGH</c>.</summary>
    public static float LevelOf(Button button) => button switch
    {
        Button.Orange => 0f,
        Button.Green => 2f,
        _ => 4f
    };

    /// <summary>The button pressed last (the original's persisted <c>pressedButton</c>).</summary>
    public Button Pressed { get; private set; } = Button.Green;

    /// <summary>Where the water and the floor on it stand now, in tiles; between levels while they move.</summary>
    public float Level { get; private set; } = LevelOf(Button.Green);

    /// <summary>
    /// The height the floor carries walkers at (the original's <c>DynamicTerrainHeightManager_SetHeight</c>), which
    /// changes only once the water has stopped.
    /// </summary>
    public float Floor { get; private set; } = LevelOf(Button.Green);

    // How long the press has been playing before the water moves
    private float pressing;

    /// <summary>Whether the water is on its way to the level of the button pressed last.</summary>
    public bool Moving => Level != LevelOf(Pressed);

    public static bool Covers(int x, int y) => x >= Left && x < Left + Width && y >= Top && y < Top + Depth;

    /// <summary>The colour of the button on a tile, or null where there is none.</summary>
    public static Button? ButtonAt(int x, int y)
    {
        foreach (var (bx, by, colour) in Buttons)
            if (bx == x && by == y) return colour;
        return null;
    }

    public override void Apply(Map map, StoryState story) { }

    /// <summary>The room's own script lays it out with the green pressed, the water at two (<c>PastoriaGym_Init</c>).</summary>
    public override void Arrive(Map map, StoryState story, Random rng) => Settle(Button.Green);

    /// <summary>The water and its floor at a button's level at once.</summary>
    public void Settle(Button button)
    {
        Pressed = button;
        Level = Floor = LevelOf(button);
        pressing = 0f;
    }

    /// <summary>
    /// A button is stepped on (<c>PastoriaGym_PressBlue</c>, <c>Green</c>, <c>OrangeButton</c>): the water starts for
    /// its level. Pressing the button already down does nothing more.
    /// </summary>
    public void Press(Button button)
    {
        Pressed = button;
        pressing = 0f;
    }

    /// <summary>Moves the water on; the floor takes its height once the water stands still.</summary>
    public void Update(float dt)
    {
        if (!Moving) return;
        pressing += dt;
        if (pressing < PressSeconds) return;
        float target = LevelOf(Pressed);
        float step = TilesPerSecond * dt;
        Level = MathF.Abs(target - Level) <= step ? target : Level + MathF.Sign(target - Level) * step;
        if (Level == target) Floor = target;
    }

    public override float? FloorAt(int x, int y) => Covers(x, y) ? Floor : null;

    public override bool Refuses(Map map, int x, int y, float from, bool afloat)
    {
        var behaviour = map.BehaviourAt(x, y);
        float? needs = behaviour switch
        {
            TileBehavior.PastoriaGymHigh => 0f,
            TileBehavior.PastoriaGymMiddle => 2f,
            TileBehavior.PastoriaGymLow => 4f,
            _ => null
        };
        if (needs is { } height) return MathF.Abs(from - height) > 0.01f;
        return afloat && behaviour == TileBehavior.MovingFloor;
    }
}
