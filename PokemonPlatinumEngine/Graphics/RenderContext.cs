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

    // Ping-pong pairs: half-resolution blurred copy of the scene (depth of field) and its glow (bloom), and
    // quarter-resolution ambient occlusion
    private RenderTexture2D blurA, blurB, bloomA, bloomB, aoA, aoB;
    private PostSettings post;
    private DepthRange depthRange = new(1f, 100f, 30f, 16f / 9f);
    private GraphicsQuality? pendingQuality;
    private bool targetsLoaded;

    /// <summary>
    /// True when the window shows the 4K screen at (close to) full size. A smaller window scales the screen
    /// down, which already smooths edges, so a full-resolution scene then skips FXAA.
    /// </summary>
    public bool OutputIsNative { get; set; }

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

        if (post.Dof > 0f)
        {
            Pass(Target.Texture, blurA, Shaders.Down, () => Shaders.SetDown(Texel(Target.Texture), 1f));
            BlurChain(blurA, blurB, 1.5f, rounds: 2);
        }
        if (post.BloomStrength > 0f)
        {
            Pass(Target.Texture, bloomA, Shaders.Down, () => Shaders.SetDown(Texel(Target.Texture), post.BloomThreshold));
            BlurChain(bloomA, bloomB, 1.2f, rounds: 2);
        }
        if (post.AoStrength > 0f)
        {
            Pass(Target.Depth, aoA, Shaders.Ssao, () => Shaders.SetSsao(Texel(Target.Depth), depth, aoRadius));
            BlurChain(aoA, aoB, 0.6f, rounds: 1);
        }
    }

    private static Vector2 Texel(Texture2D t) => new(1f / t.Width, 1f / t.Height);

    /// <summary>Rounds of separable blur, each twice as wide as the last, ending back in <paramref name="a"/>.</summary>
    private void BlurChain(RenderTexture2D a, RenderTexture2D b, float radius, int rounds)
    {
        var texel = Texel(a.Texture);
        float r = radius;
        for (int i = 0; i < rounds; i++, r *= 2.2f)
        {
            float rr = r;
            Pass(a.Texture, b, Shaders.Blur, () => Shaders.SetBlur(new Vector2(texel.X * rr, 0)));
            Pass(b.Texture, a, Shaders.Blur, () => Shaders.SetBlur(new Vector2(0, texel.Y * rr)));
        }
    }

    /// <summary>Draws <paramref name="source"/> over all of <paramref name="destination"/> through a shader, keeping its orientation.</summary>
    private static void Pass(Texture2D source, RenderTexture2D destination, Shader shader, System.Action setUniforms)
    {
        setUniforms();
        Raylib.BeginTextureMode(destination);
        Raylib.ClearBackground(Color.Black);
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
        bool fxaa = Quality.Fxaa && (OutputIsNative || Quality.SceneScale < 2f);
        Shaders.SetPost(Texel(Target.Texture), post, depthRange, fxaa, Quality.SceneScale);
        Raylib.BeginShaderMode(Shaders.Post);
        Shaders.BindPostTextures(blurA.Texture, bloomA.Texture, aoA.Texture, Target.Depth);
        var src = new Rectangle(0, 0, Target.Texture.Width, -Target.Texture.Height);
        Raylib.DrawTexturePro(Target.Texture, src, destination, Vector2.Zero, 0f, Color.White);
        Raylib.EndShaderMode();
    }

    private void UnloadTargets()
    {
        if (!targetsLoaded) return;
        Rlgl.UnloadFramebuffer(Target.Id);
        Rlgl.UnloadTexture(Target.Texture.Id);
        Rlgl.UnloadTexture(Target.Depth.Id);
        foreach (var rt in new[] { blurA, blurB, bloomA, bloomB, aoA, aoB }) Raylib.UnloadRenderTexture(rt);
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
