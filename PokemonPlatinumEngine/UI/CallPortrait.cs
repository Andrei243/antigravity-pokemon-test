using Raylib_cs;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The caller's face on a call of the travel phone (plan 43 · LF6; style guide, "Menu screens", "The travel phone"):
/// a bust of the contact's look, rendered as the bands' busts are (<see cref="CharacterStudio.RenderBust"/>), twice,
/// once with the mouth shut and once open, so the text box's voice can move it (<c>VoiceTalk.MouthOpen</c>). The
/// engine begins it as a call of the travel phone is placed, renders it in the offscreen phase of the frame (before the
/// virtual screen's texture mode: texture modes can't nest) and ends it as the call's script ends, which frees both
/// targets. It never waits for a model: until the rig is there the call shows no face.
/// </summary>
internal sealed class CallPortrait
{
    /// <summary>A face's side in pixels: drawn 300 units across, which is this at 4K.</summary>
    public const int Pixels = 600;

    /// <summary>How far the face is turned toward the middle of the picture, in radians: 15°.</summary>
    public const float Turn = 0.26f;

    /// <summary>The face a caller wears: a friendly one.</summary>
    public const Expression Face = Expression.Happy;

    private string? look;
    private RenderTexture2D shut, open;
    private bool loaded, rendered;

    /// <summary>True from <see cref="Begin"/> to <see cref="End"/>.</summary>
    public bool Active { get; private set; }

    /// <summary>The look asked for, and whether its two pictures are rendered.</summary>
    public string? Look => look;
    public bool Rendered => rendered;

    /// <summary>A call begins with the face of this look (null for a contact with none: no face is shown).</summary>
    public void Begin(string? callerLook)
    {
        End();
        if (string.IsNullOrEmpty(callerLook)) return;
        look = callerLook;
        Active = true;
    }

    /// <summary>Renders the two pictures once the look's rig is ready. Call outside any texture mode.</summary>
    public void Render(RenderContext context)
    {
        if (!Active || rendered || look == null) return;
        context.EnsureLoaded();
        // Never a wait: null while the rig is being built, and asked for again next frame
        var rig = CharacterModels.TryGet(look, context.Shaders);
        if (rig == null || rig.Kind != RigKind.Humanoid) return;
        if (!loaded)
        {
            shut = Gpu.Target(Pixels, Pixels, "ui");
            open = Gpu.Target(Pixels, Pixels, "ui");
            Raylib.SetTextureFilter(shut.Texture, TextureFilter.Bilinear);
            Raylib.SetTextureFilter(open.Texture, TextureFilter.Bilinear);
            loaded = true;
        }
        CharacterStudio.RenderBust(context, rig, shut, new CharacterPose { Expression = Face }, -Turn);
        CharacterStudio.RenderBust(context, rig, open, new CharacterPose { Expression = Face, MouthOpen = true }, -Turn);
        rendered = true;
    }

    /// <summary>The picture to draw now: the open mouth's while <paramref name="mouthOpen"/>; null while none is rendered.</summary>
    public Texture2D? Picture(bool mouthOpen) => Active && rendered ? (mouthOpen ? open.Texture : shut.Texture) : null;

    /// <summary>The call is over: the targets are freed.</summary>
    public void End()
    {
        if (loaded)
        {
            Gpu.Unload(shut);
            Gpu.Unload(open);
        }
        loaded = rendered = false;
        look = null;
        Active = false;
    }
}
