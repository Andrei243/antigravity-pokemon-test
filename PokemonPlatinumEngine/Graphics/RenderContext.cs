using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// GPU resources shared by the 3D renderers (field and battle): shaders, the sun's shadow map, the scene target
/// (supersampled on the high preset, with a depth texture the post passes can read), half-resolution blur,
/// bloom and ambient-occlusion buffers, and the materials used for characters. Only one 3D scene is rendered per
/// frame, so they can share everything.
/// </summary>
public sealed class RenderContext
{
    public int Width { get; }
    public int Height { get; }

    internal FieldShaders Shaders { get; } = new();
    internal ShadowMap Shadows { get; } = new();
    internal RenderTexture2D Target { get; private set; }
    internal Material Toon { get; private set; }
    internal Material Outline { get; private set; }
    internal Material Depth { get; private set; }

    /// <summary>Skinned characters (<see cref="SkinnedModel"/>): lit, outline and shadow-map depth.</summary>
    internal Material ToonSkinned { get; private set; }
    internal Material OutlineSkinned { get; private set; }
    internal Material DepthSkinned { get; private set; }

    /// <summary>What the current graphics preset turns on.</summary>
    public QualityProfile Quality { get; private set; } = QualityProfile.For(GraphicsQuality.High);

    // The scene at half resolution, which the blurred copy (depth of field) and the glow (bloom) are both made
    // from; their ping-pong pairs; the depth buffer at half resolution, and quarter-resolution ambient occlusion
    // made from that
    private RenderTexture2D half, blurA, blurB, bloomA, bloomB, depthHalf, aoA, aoB;
    private PostSettings post;
    private DepthRange depthRange = new(1f, 100f, 30f, 16f / 9f);
    private GraphicsQuality? pendingQuality;
    private bool targetsLoaded;

    /// <summary>The width of the window the screen is shown in, in pixels: what decides whether edges need smoothing.</summary>
    public int OutputWidth { get; set; } = 1920;

    /// <summary>
    /// Whether the composite must smooth edges itself (FXAA). A window that shows the scene at three quarters
    /// of its size or less already smooths it by scaling it down, and FXAA would cost half a millisecond for
    /// nothing: the 4K scene of the high preset needs it only in a window wider than 2880, the medium preset's
    /// only in one wider than 2160, the low preset's in all but the smallest.
    /// </summary>
    public static bool NeedsFxaa(QualityProfile quality, int sceneWidth, int windowWidth) =>
        quality.Fxaa && sceneWidth * 3 < windowWidth * 4;

    public bool Loaded { get; private set; }

    public RenderContext(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Requests a preset; it takes effect before the next frame is rendered.</summary>
    public void SetQuality(GraphicsQuality quality) => pendingQuality = quality;

    public void EnsureLoaded()
    {
        if (pendingQuality.HasValue)
        {
            var profile = QualityProfile.For(pendingQuality.Value);
            pendingQuality = null;
            if (profile != Quality)
            {
                // Only the targets depend on the preset; shaders stay, since cached materials refer to them
                UnloadTargets();
                Quality = profile;
            }
        }
        if (!Loaded)
        {
            Shaders.Load();
            Toon = MaterialFor(Shaders.Character, SceneTextures.White);
            Outline = MaterialFor(Shaders.Outline, SceneTextures.White);
            Depth = MaterialFor(Shaders.Depth, SceneTextures.White);
            ToonSkinned = MaterialFor(Shaders.CharacterSkinned, SceneTextures.White);
            OutlineSkinned = MaterialFor(Shaders.OutlineSkinned, SceneTextures.White);
            DepthSkinned = MaterialFor(Shaders.DepthSkinned, SceneTextures.White);
            Loaded = true;
        }
        if (targetsLoaded) return;

        Target = LoadSceneTarget((int)(Width * Quality.SceneScale), (int)(Height * Quality.SceneScale));
        half = HalfTarget();
        depthHalf = LoadDepthCopy(Width / 2, Height / 2);
        blurA = HalfTarget();
        blurB = HalfTarget();
        bloomA = HalfTarget();
        bloomB = HalfTarget();
        aoA = HalfTarget(4);
        aoB = HalfTarget(4);
        Shadows.Load(Quality.ShadowMapSize);
        Shaders.SetShadowQuality(Quality.ShadowTaps, 1.8f);
        targetsLoaded = true;
    }

    /// <summary>A colour target whose depth is a texture (raylib's own render textures keep depth in a renderbuffer).</summary>
    private static unsafe RenderTexture2D LoadSceneTarget(int width, int height)
    {
        var rt = new RenderTexture2D { Id = Rlgl.LoadFramebuffer() };
        Rlgl.EnableFramebuffer(rt.Id);
        rt.Texture = new Texture2D
        {
            Id = Rlgl.LoadTexture(null, width, height, PixelFormat.UncompressedR8G8B8A8, 1),
            Width = width,
            Height = height,
            Format = PixelFormat.UncompressedR8G8B8A8,
            Mipmaps = 1
        };
        rt.Depth = new Texture2D
        {
            Id = Rlgl.LoadTextureDepth(width, height, false),
            Width = width,
            Height = height,
            Format = (PixelFormat)19,
            Mipmaps = 1
        };
        Rlgl.FramebufferAttach(rt.Id, rt.Texture.Id, FramebufferAttachType.ColorChannel0, FramebufferAttachTextureType.Texture2D, 0);
        Rlgl.FramebufferAttach(rt.Id, rt.Depth.Id, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);
        Rlgl.FramebufferComplete(rt.Id);
        Rlgl.DisableFramebuffer();
        Raylib.SetTextureFilter(rt.Texture, TextureFilter.Bilinear);
        Raylib.SetTextureWrap(rt.Texture, TextureWrap.Clamp);
        return rt;
    }

    /// <summary>
    /// A target of one 32-bit float to a pixel, without a depth buffer of its own: the scene's depth is copied
    /// into it small, because the occlusion pass reads depth at scattered places and the 4K buffer is slow to read so.
    /// </summary>
    private static unsafe RenderTexture2D LoadDepthCopy(int width, int height)
    {
        var rt = new RenderTexture2D { Id = Rlgl.LoadFramebuffer() };
        Rlgl.EnableFramebuffer(rt.Id);
        rt.Texture = new Texture2D
        {
            Id = Rlgl.LoadTexture(null, width, height, PixelFormat.UncompressedR32, 1),
            Width = width,
            Height = height,
            Format = PixelFormat.UncompressedR32,
            Mipmaps = 1
        };
        Rlgl.FramebufferAttach(rt.Id, rt.Texture.Id, FramebufferAttachType.ColorChannel0, FramebufferAttachTextureType.Texture2D, 0);
        Rlgl.FramebufferComplete(rt.Id);
        Rlgl.DisableFramebuffer();
        Raylib.SetTextureFilter(rt.Texture, TextureFilter.Point);
        Raylib.SetTextureWrap(rt.Texture, TextureWrap.Clamp);
        return rt;
    }

    /// <summary>A filtered target a fraction of the virtual screen (half for blur and bloom, a quarter for occlusion).</summary>
    private RenderTexture2D HalfTarget(int divisor = 2)
    {
        var rt = Raylib.LoadRenderTexture(Width / divisor, Height / divisor);
        Raylib.SetTextureFilter(rt.Texture, TextureFilter.Bilinear);
        Raylib.SetTextureWrap(rt.Texture, TextureWrap.Clamp);
        return rt;
    }

    internal static Material MaterialFor(Shader shader, Texture2D texture)
    {
        var material = Raylib.LoadMaterialDefault();
        material.Shader = shader;
        Raylib.SetMaterialTexture(ref material, MaterialMapIndex.Albedo, texture);
        return material;
    }

    /// <summary>Binds the shadow map for sampling by the scene shaders.</summary>

    internal void BindShadowMap()
    {
        Rlgl.ActiveTextureSlot(FieldShaders.ShadowMapSlot);
        Rlgl.EnableTexture(Shadows.DepthTextureId);
        Rlgl.ActiveTextureSlot(0);
    }

    internal void UnbindShadowMap()
    {
        Rlgl.ActiveTextureSlot(FieldShaders.ShadowMapSlot);
        Rlgl.DisableTexture();
        Rlgl.ActiveTextureSlot(0);
    }

    /// <summary>
    /// Chooses the grading for the next composite and renders what it needs from the scene target: the blurred
    /// and glowing copies, and ambient occlusion from the depth buffer (<paramref name="depth"/> describes the
    /// camera that drew it). Call after the scene is rendered, outside any other texture mode.
    /// </summary>
    internal void PreparePost(PostSettings settings, DepthRange depth, float aoRadius = 0.6f)
    {
        post = settings;
        depthRange = depth;

        if (!Quality.DepthOfField) post = post with { Dof = 0f };
        if (!Quality.AmbientOcclusion) post = post with { AoStrength = 0f };
        if (!targetsLoaded) return;

        // The 4K picture is read once, into its half-size copy; the wide blur and the glow both start from that
        // (the glow keeps only what is bright as it takes its first step)
        if (post.Dof > 0f || post.BloomStrength > 0f)
            Pass(Target.Texture, half, Shaders.Down, () => Shaders.SetDown(Texel(Target.Texture)));
        if (post.Dof > 0f) BlurChain(half.Texture, blurA, blurB, 1.5f, rounds: 2, threshold: 1f);
        FrameProfiler.Lap(FrameSection.Blur);
        if (post.BloomStrength > 0f) BlurChain(half.Texture, bloomA, bloomB, 1.2f, rounds: 2, post.BloomThreshold);
        FrameProfiler.Lap(FrameSection.Bloom);
        if (post.AoStrength > 0f)
        {
            Pass(Target.Depth, depthHalf, Shaders.DepthCopy, () => { });
            // (The pass steps three of its texels to either side to find the surface's slope: one texel of the copy)
            Pass(depthHalf.Texture, aoA, Shaders.Ssao, () => Shaders.SetSsao(Texel(depthHalf.Texture) / 3f, depth, aoRadius));
            BlurChain(aoA.Texture, aoA, aoB, 0.6f, rounds: 1, threshold: 1f);
        }
        FrameProfiler.Lap(FrameSection.Occlusion);
    }

    private static Vector2 Texel(Texture2D t) => new(1f / t.Width, 1f / t.Height);

    /// <summary>
    /// Rounds of separable blur of <paramref name="source"/>, each twice as wide as the last, ending in
    /// <paramref name="a"/>. The first step keeps only what is brighter than <paramref name="threshold"/>
    /// (1 keeps everything).
    /// </summary>
    private void BlurChain(Texture2D source, RenderTexture2D a, RenderTexture2D b, float radius, int rounds, float threshold)
    {
        var texel = Texel(a.Texture);
        float r = radius;
        for (int i = 0; i < rounds; i++, r *= 2.2f)
        {
            float rr = r;
            bool first = i == 0;
            Pass(first ? source : a.Texture, b, Shaders.Blur, () => Shaders.SetBlur(new Vector2(texel.X * rr, 0), first ? threshold : 1f));
            Pass(b.Texture, a, Shaders.Blur, () => Shaders.SetBlur(new Vector2(0, texel.Y * rr), 1f));
        }
    }

    /// <summary>
    /// Draws <paramref name="source"/> over all of <paramref name="destination"/> through a shader, keeping its
    /// orientation. Nothing is cleared first: every pixel is written.
    /// </summary>
    private static void Pass(Texture2D source, RenderTexture2D destination, Shader shader, System.Action setUniforms)
    {
        setUniforms();
        Raylib.BeginTextureMode(destination);
        Raylib.BeginShaderMode(shader);
        Raylib.DrawTexturePro(source, new Rectangle(0, 0, source.Width, -source.Height),
            new Rectangle(0, 0, destination.Texture.Width, destination.Texture.Height), Vector2.Zero, 0f, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }

    /// <summary>Draws the scene target into the current target with the post effects and colour grading.</summary>
    public void Composite(Rectangle destination)
    {
        if (!targetsLoaded) return;
        bool fxaa = NeedsFxaa(Quality, Target.Texture.Width, OutputWidth);
        Shaders.SetPost(Texel(Target.Texture), post, depthRange, fxaa, Quality.SceneScale);
        Raylib.BeginShaderMode(Shaders.Post);
        Shaders.BindPostTextures(blurA.Texture, bloomA.Texture, aoA.Texture, Target.Depth);
        var src = new Rectangle(0, 0, Target.Texture.Width, -Target.Texture.Height);
        Raylib.DrawTexturePro(Target.Texture, src, destination, Vector2.Zero, 0f, Color.White);
        Raylib.EndShaderMode();
        FrameProfiler.Lap(FrameSection.Composite);
    }

    private void UnloadTargets()
    {
        if (!targetsLoaded) return;
        Rlgl.UnloadFramebuffer(Target.Id);
        Rlgl.UnloadTexture(Target.Texture.Id);
        Rlgl.UnloadTexture(Target.Depth.Id);
        Rlgl.UnloadFramebuffer(depthHalf.Id);
        Rlgl.UnloadTexture(depthHalf.Texture.Id);
        foreach (var rt in new[] { half, blurA, blurB, bloomA, bloomB, aoA, aoB }) Raylib.UnloadRenderTexture(rt);
        Shadows.Unload();
        targetsLoaded = false;
    }

    public void Unload()
    {
        UnloadTargets();
        if (!Loaded) return;
        Shaders.Unload();
        Loaded = false;
    }
}
