namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The effects a story's scene shows in the field (plan 09 · L11; style guide, "Life", "Scene effects"): a script's
/// <c>effect</c> lights one on a tile, and the field draws it as pixel art like the rest of its life.
/// </summary>
public enum SceneEffect
{
    /// <summary>Light thrown out of a point in three frames of rays, with sparks and a white flash over the picture.</summary>
    Burst,
    /// <summary>A column of light standing on a tile, out of the sky or out of a machine, its edges stepping a texel at a time.</summary>
    Beam,
    /// <summary>A tear standing in the air, its rim crawling, dark inside.</summary>
    Rift,
    /// <summary>A dark shape crossing the ground the way it is sent, flat on it like a blob shadow.</summary>
    Shadow,
    /// <summary>The picture's rows pushed sideways by whole texels on a sine: the screen wavering.</summary>
    Ripple,
    /// <summary>Points of light rising round a tile and blinking out.</summary>
    Sparkles
}

/// <summary>
/// What a scene effect is besides its picture: how long it lasts unless it is told, how long it takes to go once it
/// is stopped, the moments a tool looks at it, and its sound. GPU-free, so the script's reader and the headless host
/// share it with the field.
/// </summary>
public static class SceneEffects
{
    /// <summary>How long an effect plays when the script gives no time, in seconds (the style guide's lengths).</summary>
    public static float Length(SceneEffect kind) => kind switch
    {
        SceneEffect.Burst => 0.6f,
        SceneEffect.Beam => 1.5f,
        SceneEffect.Rift => 3f,
        SceneEffect.Shadow => 2f,
        SceneEffect.Ripple => 1.2f,
        _ => 1.5f
    };

    /// <summary>
    /// How long an effect takes to go: the last stretch of its length, and what is left of it once a held one is
    /// stopped. A burst is over by itself.
    /// </summary>
    public static float Out(SceneEffect kind) => kind switch
    {
        SceneEffect.Burst => 0.3f,
        SceneEffect.Beam => 0.2f,
        SceneEffect.Rift => 0.25f,
        SceneEffect.Shadow => 0.25f,
        SceneEffect.Ripple => 0.3f,
        _ => 0.5f
    };

    /// <summary>The moment an effect of its own length is at its height: what the harness shoots.</summary>
    public static float Height(SceneEffect kind) => kind switch
    {
        SceneEffect.Burst => 0.1f,
        SceneEffect.Shadow => Length(kind) / 2f,
        SceneEffect.Ripple => 0.6f,
        SceneEffect.Sparkles => 0.5f,
        _ => 1f
    };

    /// <summary>A moment while an effect of its own length is going: halfway through its last stretch.</summary>
    public static float Fading(SceneEffect kind) => Length(kind) - Out(kind) / 2f;

    /// <summary>Whether the effect is sent a way (only a shadow crosses the ground; the rest stand where they are lit).</summary>
    public static bool Moves(SceneEffect kind) => kind == SceneEffect.Shadow;

    /// <summary>The sound an effect starts with (<c>docs/sound-effects.md</c>).</summary>
    public static string SoundOf(SceneEffect kind) => kind switch
    {
        SceneEffect.Burst => "effect_burst",
        SceneEffect.Beam => "effect_beam",
        SceneEffect.Rift => "effect_rift",
        SceneEffect.Shadow => "effect_shadow",
        SceneEffect.Ripple => "effect_ripple",
        _ => "effect_sparkles"
    };
}
