using System;
using Raylib_cs;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The portraits of the band a battle opens on (plan 09 · L15; style guide, "Into battle", "Bands"): whose band it
/// is, the looks to draw, and a picture of each, rendered once into a target of its own as soon as the look's rig
/// is there. The engine begins it as it starts the transition, renders it in the offscreen phase of each of the
/// closing's frames (before the virtual screen's texture mode: texture modes can't nest), hands what there is to
/// <see cref="SceneTransition.Draw"/> and ends it at the midpoint, which frees the targets.
///
/// It never waits for a model: a rig still being built is asked for again the next frame
/// (<see cref="CharacterModels.TryGet"/>), and the band shows its plain bust meanwhile. Nothing is rendered twice:
/// a portrait is one picture, which the band draws as its shape alone while it is a silhouette and whole once lit.
/// </summary>
internal sealed class BandPortraits
{
    /// <summary>A portrait's side in pixels: the band draws it 440 units across, which is this at 4K.</summary>
    public const int Pixels = 880;

    /// <summary>How far each is turned toward the middle of the picture, in radians: 20°.</summary>
    public const float Turn = 0.35f;

    private BattleBand band;
    private string? theirLook, ownLook;
    private RenderTexture2D theirs, own;
    private bool theirsLoaded, ownLoaded, theirsRendered, ownRendered;

    /// <summary>True from <see cref="Begin"/> to <see cref="End"/>.</summary>
    public bool Active { get; private set; }

    /// <summary>The looks asked for: theirs, and the player's in the League's band (null in a Leader's).</summary>
    public string? TheirLook => theirLook;
    public string? OwnLook => ownLook;

    /// <summary>How many of the portraits asked for are rendered.</summary>
    public int Rendered => (theirsRendered ? 1 : 0) + (ownRendered ? 1 : 0);

    /// <summary>
    /// A band begins: whose it is, the look their portrait is of (the one the battle itself draws them in) and the
    /// player's own, when the band shows it. No picture is made here: that is <see cref="Render"/>'s, in a frame.
    /// </summary>
    public void Begin(BattleBand whose, string theirLook, string? ownLook)
    {
        End();
        band = whose;
        this.theirLook = theirLook;
        this.ownLook = ownLook;
        Active = true;
    }

    /// <summary>
    /// Renders whichever portrait is still to come and whose rig is ready, each once. Call outside any texture mode.
    /// </summary>
    public void Render(RenderContext context)
    {
        if (!Active) return;
        SceneTransition.WarmBands();
        // Theirs faces the middle from the right, the player's from the left
        if (!theirsRendered && theirLook != null) theirsRendered = RenderOne(context, theirLook, ref theirs, ref theirsLoaded, -Turn);
        if (!ownRendered && ownLook != null) ownRendered = RenderOne(context, ownLook, ref own, ref ownLoaded, Turn);
    }

    private static bool RenderOne(RenderContext context, string look, ref RenderTexture2D target, ref bool loaded, float yaw)
    {
        context.EnsureLoaded();
        // Never a wait: null while the rig is being built, and the build is under way from this call on if it wasn't
        var rig = CharacterModels.TryGet(look, context.Shaders);
        if (rig == null || rig.Kind != RigKind.Humanoid) return false;
        if (!loaded)
        {
            target = Gpu.Target(Pixels, Pixels, "ui");
            Raylib.SetTextureFilter(target.Texture, TextureFilter.Bilinear);
            loaded = true;
        }
        // A set face of the battle's kind, eyes open, standing still: the same picture every time
        CharacterStudio.RenderBust(context, rig, target, new CharacterPose { Expression = Face }, yaw);
        return true;
    }

    /// <summary>The face a portrait wears.</summary>
    public const Expression Face = Expression.Angry;

    /// <summary>What the band is drawn with now: whose it is and the portraits rendered so far; null when no band is on.</summary>
    public BandPicture? Picture => Active
        ? new BandPicture(band, theirsRendered ? theirs.Texture : null, ownRendered ? own.Texture : null)
        : null;

    /// <summary>The band is over: its targets are freed.</summary>
    public void End()
    {
        if (theirsLoaded) Gpu.Unload(theirs);
        if (ownLoaded) Gpu.Unload(own);
        theirsLoaded = ownLoaded = theirsRendered = ownRendered = false;
        theirLook = ownLook = null;
        Active = false;
    }
}
