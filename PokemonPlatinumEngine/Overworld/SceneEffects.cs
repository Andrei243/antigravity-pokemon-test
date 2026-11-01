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
    Sparkles,
    /// <summary>A column of water heaving up out of the tile and falling back as spray (plan 29 · LS10: Kyogre rising).</summary>
    Geyser,
    /// <summary>The geyser's shape in magma, throwing embers (plan 29 · LS10: Groudon rising).</summary>
    Eruption,
    /// <summary>A stair of light climbing away north and up from the tile, a step at a time (plan 29 · LS10: the Azure Flute).</summary>
    Stair,
    /// <summary>Petals blown up and round a tile, fluttering (plan 29 · LS10: Shaymin).</summary>
    Petals,
    /// <summary>A dark mist lying low round a tile and creeping out (plan 29 · LS10: Darkrai's dream).</summary>
    Mist
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
        SceneEffect.Geyser or SceneEffect.Eruption => 1.6f,
        SceneEffect.Stair => 3f,
        SceneEffect.Mist => 3f,
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
        SceneEffect.Geyser or SceneEffect.Eruption => 0.3f,
        SceneEffect.Stair => 0.4f,
        SceneEffect.Petals => 0.4f,
        _ => 0.5f
    };

    /// <summary>The moment an effect of its own length is at its height: what the harness shoots.</summary>
    public static float Height(SceneEffect kind) => kind switch
    {
        SceneEffect.Burst => 0.1f,
        SceneEffect.Shadow => Length(kind) / 2f,
        SceneEffect.Ripple => 0.6f,
        SceneEffect.Sparkles => 0.5f,
        SceneEffect.Geyser or SceneEffect.Eruption => 0.7f,
        SceneEffect.Stair => 1.5f,
        SceneEffect.Petals => 0.6f,
        SceneEffect.Mist => 1.5f,
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
        SceneEffect.Geyser => "effect_geyser",
        SceneEffect.Eruption => "effect_eruption",
        SceneEffect.Stair => "effect_stair",
        SceneEffect.Petals => "effect_petals",
        SceneEffect.Mist => "effect_mist",
        _ => "effect_sparkles"
    };
}
