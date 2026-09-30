using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// GPU resources shared by the 3D renderers (field and battle): shaders, the sun's shadow map, a supersampled
/// scene target, half-resolution blur and bloom chains, and the materials used for characters. Only one 3D
/// scene is rendered per frame, so they can share everything.
/// </summary>
public sealed class RenderContext
{
    public const int SuperSample = 2;

    public int Width { get; }
    public int Height { get; }

    internal FieldShaders Shaders { get; } = new();
    internal ShadowMap Shadows { get; } = new();
    internal RenderTexture2D Target { get; private set; }
    internal Material Toon { get; private set; }
    internal Material Outline { get; private set; }
    internal Material Depth { get; private set; }

    // Half-resolution ping-pong pairs: a blurred copy of the scene (depth of field) and its glow (bloom)
    private RenderTexture2D blurA, blurB, bloomA, bloomB;
    private PostSettings post;

    public bool Loaded { get; private set; }

    public RenderContext(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void EnsureLoaded()
    {
        if (Loaded) return;
        Target = Raylib.LoadRenderTexture(Width * SuperSample, Height * SuperSample);
        Raylib.SetTextureFilter(Target.Texture, TextureFilter.Bilinear);
        blurA = HalfTarget();
        blurB = HalfTarget();
        bloomA = HalfTarget();
        bloomB = HalfTarget();
        Shaders.Load();
        Shadows.Load();

        Toon = MaterialFor(Shaders.Character, SceneTextures.White);
        Outline = MaterialFor(Shaders.Outline, SceneTextures.White);
        Depth = MaterialFor(Shaders.Depth, SceneTextures.White);
        Loaded = true;
    }

    private RenderTexture2D HalfTarget()
    {
        var rt = Raylib.LoadRenderTexture(Width / 2, Height / 2);
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

    /// <summary>Binds the shadow map for sampling by the field shaders.</summary>
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
    /// Chooses the grading for the next composite and, when it needs them, renders the blurred and glowing copies
    /// of the scene target. Call after the scene is rendered, outside any other texture mode.
    /// </summary>
    internal void PreparePost(PostSettings settings)
    {
        post = settings;
        if (!Loaded) return;
        if (settings.Dof > 0f)
        {
            Pass(Target.Texture, blurA, Shaders.Down, () => Shaders.SetDown(Texel(Target.Texture), 1f));
            BlurChain(blurA, blurB, 1.5f);
        }
        if (settings.BloomStrength > 0f)
        {
            Pass(Target.Texture, bloomA, Shaders.Down, () => Shaders.SetDown(Texel(Target.Texture), settings.BloomThreshold));
            BlurChain(bloomA, bloomB, 1.2f);
        }
    }

    private static Vector2 Texel(Texture2D t) => new(1f / t.Width, 1f / t.Height);

    /// <summary>Two rounds of separable blur, the second twice as wide, ending back in <paramref name="a"/>.</summary>
    private void BlurChain(RenderTexture2D a, RenderTexture2D b, float radius)
    {
        var texel = Texel(a.Texture);
        foreach (float r in new[] { radius, radius * 2.2f })
        {
            Pass(a.Texture, b, Shaders.Blur, () => Shaders.SetBlur(new Vector2(texel.X * r, 0)));
            Pass(b.Texture, a, Shaders.Blur, () => Shaders.SetBlur(new Vector2(0, texel.Y * r)));
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

    /// <summary>Draws the scene target into the current target, filtered down, with tilt-shift, bloom and colour grading.</summary>
    public void Composite(Rectangle destination)
    {
        if (!Loaded) return;
        Shaders.SetPost(Texel(Target.Texture), post);
        Raylib.BeginShaderMode(Shaders.Post);
        Shaders.BindPostTextures(blurA.Texture, bloomA.Texture);
        var src = new Rectangle(0, 0, Target.Texture.Width, -Target.Texture.Height);
        Raylib.DrawTexturePro(Target.Texture, src, destination, Vector2.Zero, 0f, Color.White);
        Raylib.EndShaderMode();
    }

    public void Unload()
    {
        if (!Loaded) return;
        Raylib.UnloadRenderTexture(Target);
        foreach (var rt in new[] { blurA, blurB, bloomA, bloomB }) Raylib.UnloadRenderTexture(rt);
        Shaders.Unload();
        Shadows.Unload();
        Loaded = false;
    }
}
