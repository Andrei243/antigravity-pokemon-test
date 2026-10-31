using System.Numerics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Photo mode's filters (plan 12 · Q16; style guide, "Light, shadow and grading (day)", "Photo mode's filters"): laid
/// over a light rig's grade last, after the weather, the climate and the season. GPU-free.
/// </summary>
internal static class PhotoFilters
{
    /// <summary>A rig with a filter laid over its grade; the rig itself under <see cref="PhotoFilter.None"/>.</summary>
    public static LightRig Filtered(LightRig rig, PhotoFilter filter) => filter == PhotoFilter.None ? rig : rig with { Post = Graded(rig.Post, filter) };

    /// <summary>A grade with a filter laid over it.</summary>
    public static PostSettings Graded(PostSettings post, PhotoFilter filter) => filter switch
    {
        PhotoFilter.Vivid => post with { Saturation = post.Saturation * 1.25f, Contrast = post.Contrast * 1.06f },
        PhotoFilter.Mono => post with { Saturation = 0f, Contrast = post.Contrast * 1.10f, ShadowTint = Vector3.One, HighlightTint = Vector3.One },
        // The composite tints before it takes the colour out, so sepia keeps a little colour for its tints to show in
        PhotoFilter.Sepia => post with { Saturation = 0.3f, ShadowTint = new Vector3(1.14f, 0.94f, 0.68f), HighlightTint = new Vector3(1.24f, 1.00f, 0.68f) },
        PhotoFilter.Soft => post with
        {
            BloomStrength = post.BloomStrength * 1.6f,
            BloomThreshold = post.BloomThreshold - 0.08f,
            Contrast = post.Contrast * 0.94f,
            Vignette = post.Vignette + 0.12f
        },
        _ => post
    };
}
