using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Pastoria Gym's water (plan 01 · M9 1b), ported from the original's <c>gym_features.c</c>: a pool whose water
/// stands at one of three heights, set by the button of each colour last stepped on (<c>PastoriaGym_PressButton</c>):
/// blue raises it to four tiles over the pool's floor, green brings it to two, orange lets it all out. The water is
/// one plate over the pool (<c>DynamicTerrainHeightManager_SetPlate(0, 1, 2, 25, 38, ...)</c>: x 1 to 25, z 2 to
/// 39), and the floats on it are stood on wherever the water lies over the floor and nearer than it (the original's
/// height rule, <see cref="Map.StandAt"/>); where nothing floats (the tiles of behaviour 0x59) the water can't be
/// stood on. Three behaviours are walked onto only by someone standing at one height
/// (<c>PastoriaGym_DynamicMapFeaturesCheckCollision</c>): 0x56 from the bottom, 0x57 from two tiles up, 0x58 from
/// four. The green button is pressed as the player comes in (<c>PersistedMapFeatures_InitForPastoriaGym</c>), and
/// the water stays where it was put through a battle, as the original's persisted feature does.
/// </summary>
public sealed class PastoriaWater : GymPuzzle
{
    public const string PuzzleName = "PastoriaWater";
    public override string Name => PuzzleName;

    /// <summary>The water's three heights over the pool's floor (<c>PASTORIA_WATER_HEIGHT_*</c>: 0, two and four tiles).</summary>
    public const float Low = 0f, Middle = 2f, High = 4f;

    /// <summary>The plate of water (<c>PastoriaGym_DynamicMapFeaturesInit</c>): from (1, 2), 25 tiles wide and 38 deep.</summary>
    public const int PlateX = 1, PlateZ = 2, PlateWidth = 25, PlateDepth = 38;

    /// <summary>The original runs the field at thirty frames a second.</summary>
    public const float FrameSeconds = 1f / 30f;

    /// <summary>How far the water moves in a frame: one unit (<c>FX32_ONE</c>) of sixteen to the tile.</summary>
    public const float TilesPerFrame = 1f / 16f;

    /// <summary>
    /// Frames the buttons take to go down and come up before the water moves (the original waits for their
    /// animations, <c>MapPropAnimation_IsLoopFinished</c>, whose length is the model's; this length is ours).
    /// </summary>
    public const int ButtonFrames = 8;

    /// <summary>The three colours of button (<c>pastoria_gym_*_button</c>).</summary>
    public enum Button
    {
        Blue,
        Green,
        Orange
    }

    /// <summary>The height a button sets the water to: blue the highest, green the middle, orange the bottom.</summary>
    public static float LevelOf(Button button) => button switch
    {
        Button.Blue => High,
        Button.Green => Middle,
        _ => Low
    };

    /// <summary>
    /// The variable of the room's script that says a button's colour was pressed last (<c>scripts_pastoria_city_gym.s</c>):
    /// a button's step trigger runs while its own is 0, so the colour pressed last does nothing until another is.
    /// </summary>
    public static string VarOf(Button button) => button switch
    {
        Button.Blue => "VAR_MAP_LOCAL_0x01",
        Button.Green => "VAR_MAP_LOCAL_0x02",
        _ => "VAR_MAP_LOCAL_0x03"
    };

    /// <summary>
    /// The original's buttons, each on its tile (the land data's <c>pastoria_gym_*_button</c> props, under the room
    /// script's coordinate events): two blue, four green and four orange.
    /// </summary>
    public static readonly (int X, int Y, Button Button)[] Buttons =
    {
        (3, 6, Button.Blue), (9, 24, Button.Blue),
        (17, 9, Button.Green), (19, 13, Button.Green), (19, 24, Button.Green), (10, 30, Button.Green),
        (9, 14, Button.Orange), (19, 22, Button.Orange), (23, 31, Button.Orange), (3, 34, Button.Orange)
    };

    /// <summary>
    /// Whether a tile carries a float: one under the water's plate, open, whose floor lies under the water's top height
    /// and that the water may be stood on over (any behaviour but 0x59, and the gates walked onto from the bottom or the
    /// middle): the float rides on the water and rests on the floor once it is let out.
    /// </summary>
    public static bool IsFloat(Map map, int x, int y) =>
        InPlate(x, y) && !map.IsSolid(x, y) && map.HeightAt(x, y) < High - 0.01f
        && map.BehaviourAt(x, y) is not (TileBehavior.MovingFloor or TileBehavior.PastoriaGymHigh or TileBehavior.PastoriaGymMiddle);

    /// <summary>The button pressed last.</summary>
    public Button Pressed { get; private set; } = Button.Green;

    /// <summary>The water's height as the field walks it: changed only once the water has come to rest (the original's plate).</summary>
    public float Height { get; private set; } = Middle;

    /// <summary>The height the water is drawn at: moving from the old height to the new while it rises or falls.</summary>
    public float Level { get; private set; } = Middle;

    /// <summary>The water on its way to a button's height; null at rest.</summary>
    public Rise? Rising { get; private set; }

    /// <summary>Whether a tile lies under the plate of water.</summary>
    public static bool InPlate(int x, int y) => x >= PlateX && x < PlateX + PlateWidth && y >= PlateZ && y < PlateZ + PlateDepth;

    public override float? WaterAt(int x, int y) => InPlate(x, y) ? Height : null;

    public override bool? Collides(Map map, int x, int y, float from) => map.BehaviourAt(x, y) switch
    {
        TileBehavior.PastoriaGymHigh => !Near(from, Low),
        TileBehavior.PastoriaGymMiddle => !Near(from, Middle),
        TileBehavior.PastoriaGymLow => !Near(from, High),
        _ => null
    };

    private static bool Near(float a, float b) => MathF.Abs(a - b) < 0.01f;

    // Nothing of the room is closed by it: its gates are asked as each step is taken
    public override void Apply(Map map, StoryState story) { }

    /// <summary>The player has come in by the door: the green button is pressed and the water stands at its middle height.</summary>
    public override void Arrive(Map map, StoryState story, Random rng)
    {
        Pressed = Button.Green;
        Height = Level = Middle;
        Rising = null;
    }

    /// <summary>
    /// A button stepped on (<c>PastoriaGym_PressButton</c>): it goes down and the others come up, then the water
    /// moves to its height. Green moves the water whichever way it has to; nothing moves if it is there already.
    /// </summary>
    public Rise Press(Button button)
    {
        Pressed = button;
        return Rising = new Rise(this, LevelOf(button));
    }

    /// <summary>Plays the water's movement out at once: for tests and for anything that doesn't watch it.</summary>
    public void Settle()
    {
        while (Rising is { IsDone: false } rise) rise.Update(1f);
    }

    /// <summary>
    /// The water on its way (the original's <c>PastoriaGym_Press*Button</c> tasks): the buttons' animations, then the
    /// water moving a sixteenth of a tile a frame until it reaches the height, which the field then walks.
    /// </summary>
    public sealed class Rise
    {
        private readonly PastoriaWater water;
        private float clock;
        private int frames;

        internal Rise(PastoriaWater water, float to)
        {
            this.water = water;
            To = to;
        }

        /// <summary>The height the water is going to.</summary>
        public float To { get; }

        /// <summary>Whether the buttons have finished and the water is moving (for the sound of it).</summary>
        public bool Flowing { get; private set; }

        public bool IsDone { get; private set; }

        public void Update(float dt)
        {
            if (IsDone) return;
            clock += dt;
            while (clock >= FrameSeconds && !IsDone)
            {
                clock -= FrameSeconds;
                Frame();
            }
        }

        private void Frame()
        {
            if (++frames <= ButtonFrames) return;
            float level = water.Level;
            if (Near(level, To))
            {
                // At its height: the field walks the plate there from now on
                water.Level = water.Height = To;
                Flowing = false;
                IsDone = true;
                water.Rising = null;
                return;
            }
            Flowing = true;
            water.Level = level < To ? MathF.Min(To, level + TilesPerFrame) : MathF.Max(To, level - TilesPerFrame);
        }
    }
}
