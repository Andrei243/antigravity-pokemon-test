using System.Numerics;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The game's art direction. <see cref="Diorama"/> is the style chosen in plan 04 · G1 and described in
/// docs/art/style-guide.md; <see cref="Current"/> is the look from before the overhaul, kept only until the
/// sessions that rebuild each area replace it (and for before/after shots).
/// </summary>
public enum ArtDirection
{
    Current,

    /// <summary>
    /// "Sinnoh Diorama": an HD-2D overworld (pixel-art textures and character sprites in a lit 3D diorama),
    /// full 3D battles (smooth cel-shaded Pokémon on modelled stages) and a crisp vector interface that shows
    /// Pokémon as 2D pixel sprites.
    /// </summary>
    Diorama
}

/// <summary>Colour grading and screen effects applied when the 3D scene is composited.</summary>
internal readonly record struct PostSettings(
    float TiltShift,          // small blur radius (scene texels) at the top and bottom of the screen
    float Dof,                // 0..1 mix toward a wide blur at the top and bottom (depth of field)
    float FocusBand,          // half-height of the sharp band in the middle (0..1 of the screen)
    float BloomThreshold,     // luma above which pixels glow
    float BloomStrength,
    float Saturation,
    float Contrast,
    Vector3 ShadowTint,       // multiplied into the darks
    Vector3 HighlightTint,    // multiplied into the lights
    float Vignette);          // darkening in the corners (0..1)

/// <summary>The active art direction, which layer draws in which style, and the numbers that belong to it.</summary>
public static class ArtLook
{
    public static ArtDirection Direction { get; private set; } = ArtDirection.Diorama;

    /// <summary>Bumped on every change, so caches built for one look can tell they are stale.</summary>
    public static int Version { get; private set; }

    public static void Set(ArtDirection direction)
    {
        if (direction == Direction) return;
        Direction = direction;
        Version++;
    }

    private static bool Diorama => Direction == ArtDirection.Diorama;

    /// <summary>The overworld is HD-2D: pixel-art ground, clean pixel textures and characters as baked sprites.</summary>
    public static bool PixelField => Diorama;

    /// <summary>Battles are fully 3D: modelled stage, soft foliage, and the Pokémon as lit 3D models.</summary>
    public static bool ModelBattle => Diorama;

    /// <summary>Menus and HUDs use the vector interface kit (Nunito, anti-aliased panels).</summary>
    public static bool VectorUi => Diorama;

    // ------------------------------------------------------------------ lighting

    /// <summary>Daylight for outdoor maps.</summary>
    internal static SceneLighting FieldDay => PixelField
        // Low, golden late-morning sun with violet-blue shade, like a lit diorama
        ? new SceneLighting(Vector3.Normalize(new Vector3(-0.6f, 0.8f, 0.42f)), new Vector3(0.66f, 0.55f, 0.4f),
            new Vector3(0.5f, 0.55f, 0.74f), new Vector3(0.46f, 0.44f, 0.4f))
        : new SceneLighting(Vector3.Normalize(new Vector3(-0.42f, 1.0f, 0.3f)), new Vector3(0.46f, 0.44f, 0.39f),
            new Vector3(0.58f, 0.62f, 0.7f), new Vector3(0.5f, 0.48f, 0.42f));

    internal static SceneLighting Battle => ModelBattle
        // Warm sun from the upper left; sky-blue fill in the shadows and a green bounce from the meadow
        ? new SceneLighting(Vector3.Normalize(new Vector3(-0.5f, 0.9f, 0.5f)), new Vector3(0.47f, 0.43f, 0.35f),
            new Vector3(0.54f, 0.6f, 0.72f), new Vector3(0.5f, 0.5f, 0.4f))
        : new SceneLighting(Vector3.Normalize(new Vector3(-0.5f, 0.85f, 0.45f)), new Vector3(0.5f, 0.48f, 0.43f),
            new Vector3(0.6f, 0.64f, 0.72f), new Vector3(0.5f, 0.48f, 0.42f));

    /// <summary>
    /// How much battle scenery's diffuse light is banded like a cel-shaded model: 0 keeps smooth Lambert shading,
    /// 1 gives a soft two-tone ramp. The pixel-art field keeps Lambert light (its textures carry the shading).
    /// </summary>
    internal static float BattleRamp => ModelBattle ? 0.85f : 0f;

    /// <summary>Outline colour for 3D characters and Pokémon: a darker shade of the surface instead of near-black ink.</summary>
    internal static float OutlineInkAmount => ModelBattle ? 0.5f : 0.72f;

    // ------------------------------------------------------------------ post

    // HD-2D field: strong tilt-shift depth of field, warm highlights and violet shade; rooms are small and bright,
    // so they get a gentler blur and less glow
    private static readonly PostSettings DioramaOutdoors = new(6f, 0.95f, 0.3f, 0.86f, 0.38f, 1.1f, 1.08f,
        new Vector3(0.88f, 0.92f, 1.1f), new Vector3(1.07f, 1.0f, 0.9f), 0.26f);
    private static readonly PostSettings DioramaIndoors = DioramaOutdoors with
    {
        TiltShift = 4f, Dof = 0.6f, FocusBand = 0.36f, BloomThreshold = 0.93f, BloomStrength = 0.22f
    };

    internal static PostSettings FieldPost(bool indoors) => PixelField
        ? indoors ? DioramaIndoors : DioramaOutdoors
        : new PostSettings(indoors ? 3.5f : 6f, 0f, 0.5f, 1f, 0f, 1.1f, 1.04f, Vector3.One, Vector3.One, 0.16f);

    internal static PostSettings BattlePost => ModelBattle
        ? new PostSettings(2f, 0.15f, 0.5f, 0.86f, 0.28f, 1.05f, 1.05f,
            new Vector3(0.94f, 0.97f, 1.06f), new Vector3(1.04f, 1.01f, 0.95f), 0.1f)
        : new PostSettings(2.5f, 0f, 0.5f, 1f, 0f, 1.1f, 1.04f, Vector3.One, Vector3.One, 0.16f);
}
