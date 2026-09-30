using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// GPU resources shared by the 3D renderers (field and battle): shaders, the sun's shadow map, a supersampled
/// scene target and the materials used for characters. Only one 3D scene is rendered per frame, so they can
/// share everything.
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
        Shaders.Load();
        Shadows.Load();

        Toon = MaterialFor(Shaders.Character, SceneTextures.White);
        Outline = MaterialFor(Shaders.Outline, SceneTextures.White);
        Depth = MaterialFor(Shaders.Depth, SceneTextures.White);
        Loaded = true;
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

    /// <summary>Draws the scene target into the current target, filtered down, with tilt-shift and colour grading.</summary>
    public void Composite(Rectangle destination, float tiltShift)
    {
        if (!Loaded) return;
        Shaders.SetPost(new Vector2(1f / Target.Texture.Width, 1f / Target.Texture.Height), tiltShift);
        Raylib.BeginShaderMode(Shaders.Post);
        var src = new Rectangle(0, 0, Target.Texture.Width, -Target.Texture.Height);
        Raylib.DrawTexturePro(Target.Texture, src, destination, Vector2.Zero, 0f, Color.White);
        Raylib.EndShaderMode();
    }

    public void Unload()
    {
        if (!Loaded) return;
        Raylib.UnloadRenderTexture(Target);
        Shaders.Unload();
        Shadows.Unload();
        Loaded = false;
    }
}
