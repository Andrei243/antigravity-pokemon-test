using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The busts of the cards in view (plan 43 · LF2; style guide, "The cards of the people beaten"), rendered as the
/// bands' are (<see cref="CharacterStudio.RenderBust"/>, the same face and light, turned as theirs): each once, into a
/// small target of its own, in the offscreen phase of the first frame that finds its rig ready (before the virtual
/// screen's texture mode: texture modes can't nest). Whatever is no longer in view is freed, and all of them as the
/// page is left. It never waits for a model: a card shows a plain bust until its rig is there.
/// </summary>
internal sealed class CardPortraits
{
    /// <summary>A small card's bust and an opened card's, in pixels: what each is drawn at on the 4K screen.</summary>
    public const int Pixels = (int)ModernUi.AlbumBustSize * 2, LargePixels = (int)ModernUi.AlbumLargeBustSize * 2;

    private sealed class Bust
    {
        public RenderTexture2D Target;
        public bool Loaded, Rendered;
    }

    private readonly Dictionary<(string Look, bool Large), Bust> busts = new();

    /// <summary>How many busts are asked for now, and how many of them are rendered.</summary>
    public int Wanted => busts.Count;
    public int Rendered => busts.Values.Count(b => b.Rendered);

    /// <summary>The picture of a look's bust, or null while there is none.</summary>
    public Texture2D? Picture(string look, bool large) =>
        busts.TryGetValue((look, large), out var bust) && bust.Rendered ? bust.Target.Texture : null;

    /// <summary>
    /// The looks in view this frame: busts no longer among them are freed, and whichever of them is still to come and
    /// whose rig is ready is rendered. Call outside any texture mode.
    /// </summary>
    public void Show(RenderContext context, IReadOnlyCollection<(string Look, bool Large)> inView)
    {
        foreach (var gone in busts.Keys.Where(k => !inView.Contains(k)).ToList()) Free(gone);
        foreach (var key in inView)
        {
            if (!busts.TryGetValue(key, out var bust)) busts[key] = bust = new Bust();
            if (bust.Rendered) continue;
            context.EnsureLoaded();
            // Never a wait: null while the rig is being built, and the build is under way from this call on
            var rig = CharacterModels.TryGet(key.Look, context.Shaders);
            if (rig == null || rig.Kind != RigKind.Humanoid) continue;
            if (!bust.Loaded)
            {
                int side = key.Large ? LargePixels : Pixels;
                bust.Target = Gpu.Target(side, side, "ui");
                Raylib.SetTextureFilter(bust.Target.Texture, TextureFilter.Bilinear);
                bust.Loaded = true;
            }
            CharacterStudio.RenderBust(context, rig, bust.Target, new CharacterPose { Expression = BandPortraits.Face }, -BandPortraits.Turn,
                CallPortrait.HeadroomOf(rig));
            bust.Rendered = true;
        }
    }

    /// <summary>The page is left: every target is freed.</summary>
    public void End()
    {
        if (busts.Count == 0) return;
        foreach (var key in busts.Keys.ToList()) Free(key);
    }

    private void Free((string, bool) key)
    {
        if (busts[key].Loaded) Gpu.Unload(busts[key].Target);
        busts.Remove(key);
    }
}
