using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Depth-only render target seen from the sun, used by the field shaders to decide what is in shadow.
/// </summary>
internal sealed class ShadowMap
{
    private RenderTexture2D target;
    private bool loaded;

    public int Resolution { get; private set; } = 2048;
    public RenderTexture2D Target => target;
    public uint DepthTextureId => target.Depth.Id;
    public float Texel => 1f / Resolution;

    public void Load(int resolution)
    {
        if (loaded) return;
        Resolution = resolution;
        target = new RenderTexture2D { Id = Rlgl.LoadFramebuffer() };
        target.Texture.Width = Resolution;
        target.Texture.Height = Resolution;

        Rlgl.EnableFramebuffer(target.Id);
        target.Depth = new Texture2D
        {
            Id = Rlgl.LoadTextureDepth(Resolution, Resolution, false),
            Width = Resolution,
            Height = Resolution,
            Format = (PixelFormat)19,
            Mipmaps = 1
        };
        Rlgl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);
        Rlgl.FramebufferComplete(target.Id);
        Rlgl.DisableFramebuffer();
        // The card compares and filters for the shaders, where it can be asked to (they are compiled to match)
        if (FieldShaders.HardwareShadows) Gl.CompareDepth(target.Depth.Id);
        loaded = true;
    }

    public void Unload()
    {
        if (!loaded) return;
        Rlgl.UnloadFramebuffer(target.Id);
        Rlgl.UnloadTexture(target.Depth.Id);
        loaded = false;
    }

    /// <summary>
    /// Orthographic camera looking along the sunlight, centred on <paramref name="focus"/> and covering
    /// <paramref name="extent"/> world units. The centre is snapped to whole shadow-map texels so shadow
    /// edges stay still while the view scrolls.
    /// </summary>
    public Camera3D LightCamera(Vector3 focus, Vector3 sunDirection, float extent)
    {
        var forward = -Vector3.Normalize(sunDirection);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var up = Vector3.Cross(right, forward);

        float texelWorld = extent / Resolution;
        float sx = Vector3.Dot(focus, right), sy = Vector3.Dot(focus, up);
        focus += right * (MathF.Floor(sx / texelWorld) * texelWorld - sx);
        focus += up * (MathF.Floor(sy / texelWorld) * texelWorld - sy);

        return new Camera3D(focus - forward * 80f, focus, Vector3.UnitY, extent, CameraProjection.Orthographic);
    }
}
