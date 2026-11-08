using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// The open world's shadows (plan 32 · OW8; style guide, "Open world", "Shadows"): three maps from the sun, each fitted
/// round its slice of the camera's view and snapped to its own texels, the first twice the preset's size across. The
/// grid keeps its one map (<see cref="ShadowMap"/>); these are the open renderer's own, made when it first draws. The
/// fitting (<see cref="Slice"/>, <see cref="Fit"/>) is GPU-free.
/// </summary>
internal sealed class OpenCascades
{
    /// <summary>How far along the view each map's slice reaches, in metres: past the last nothing casts.</summary>
    public static readonly float[] Ends = { 18f, 60f, 220f };

    /// <summary>The share of each slice at its far end that blends into the next.</summary>
    public const float Blend = 0.15f;

    /// <summary>How far behind its box, toward the sun, each map takes what casts into it.</summary>
    public const float Behind = 200f;

    public const int Count = 3;

    /// <summary>The texture slots the three are bound to: the first is the field's own slot, which the characters' programs read.</summary>
    public static readonly int[] Slots = { FieldShaders.ShadowMapSlot, FieldShaders.ShadowMapSlot + 1, FieldShaders.ShadowMapSlot + 2 };

    private readonly RenderTexture2D[] targets = new RenderTexture2D[Count];
    private readonly int[] sizes = new int[Count];
    private bool loaded;

    /// <summary>Each map's side in texels.</summary>
    public int SizeOf(int cascade) => sizes[cascade];

    public RenderTexture2D Target(int cascade) => targets[cascade];

    public uint DepthOf(int cascade) => targets[cascade].Depth.Id;

    /// <summary>The maps for a preset's shadow map: the first twice its size, the others its size; made again if the preset changed.</summary>
    public void Ensure(int presetSize)
    {
        int first = presetSize * 2;
        if (loaded && sizes[0] == first && sizes[1] == presetSize) return;
        Unload();
        for (int i = 0; i < Count; i++)
        {
            sizes[i] = i == 0 ? first : presetSize;
            var target = new RenderTexture2D { Id = Gpu.Framebuffer("openshadow") };
            target.Texture.Width = sizes[i];
            target.Texture.Height = sizes[i];
            Rlgl.EnableFramebuffer(target.Id);
            target.Depth = new Texture2D
            {
                Id = Gpu.DepthTexture(sizes[i], sizes[i], "openshadow"),
                Width = sizes[i],
                Height = sizes[i],
                Format = (PixelFormat)19,
                Mipmaps = 1
            };
            Rlgl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);
            Rlgl.FramebufferComplete(target.Id);
            Rlgl.DisableFramebuffer();
            if (FieldShaders.HardwareShadows) Gl.CompareDepth(target.Depth.Id);
            targets[i] = target;
        }
        loaded = true;
    }

    public void Bind()
    {
        if (!loaded) return;
        for (int i = 0; i < Count; i++)
        {
            Rlgl.ActiveTextureSlot(Slots[i]);
            Rlgl.EnableTexture(targets[i].Depth.Id);
        }
        Rlgl.ActiveTextureSlot(0);
    }

    public void Unbind()
    {
        for (int i = 0; i < Count; i++)
        {
            Rlgl.ActiveTextureSlot(Slots[i]);
            Rlgl.DisableTexture();
        }
        Rlgl.ActiveTextureSlot(0);
    }

    public void Unload()
    {
        if (!loaded) return;
        for (int i = 0; i < Count; i++)
        {
            Gpu.UnloadFramebuffer(targets[i].Id);
            Gpu.UnloadRawTexture(targets[i].Depth.Id);
            targets[i] = default;
        }
        loaded = false;
    }

    // ------------------------------------------------------------------ fitting (GPU-free)

    /// <summary>
    /// The smallest sphere round the slice of a camera's view between two distances along it (the eight corners of
    /// the slice of its frustum): its centre on the view's axis and its radius. A sphere keeps its size however the camera
    /// turns, so the map's texels stay the same size and its edges don't swim.
    /// </summary>
    public static (Vector3 Centre, float Radius) Slice(Vector3 eye, Vector3 forward, float fovYDegrees, float aspect, float near, float far)
    {
        float tanHalf = MathF.Tan(fovYDegrees * MathF.PI / 360f);
        float k2 = (1f + aspect * aspect) * tanHalf * tanHalf;
        float centre, radius;
        if (k2 >= (far - near) / (far + near))
        {
            centre = far;
            radius = far * MathF.Sqrt(k2);
        }
        else
        {
            centre = 0.5f * (far + near) * (1f + k2);
            radius = 0.5f * MathF.Sqrt((far - near) * (far - near) + 2f * (far * far + near * near) * k2 + (far + near) * (far + near) * k2 * k2);
        }
        return (eye + Vector3.Normalize(forward) * centre, radius);
    }

    /// <summary>
    /// The sun's camera for a map: looking along the sunlight at the sphere's centre, snapped to whole texels of a map
    /// <paramref name="size"/> across on the plane facing the sun, and the clip planes that hold the sphere and what
    /// lies <see cref="Behind"/> it toward the sun. The camera's view height is the sphere's diameter.
    /// </summary>
    public static (Camera3D Camera, double Near, double Far) Fit(Vector3 centre, float radius, Vector3 sunDirection, int size)
    {
        var forward = -Vector3.Normalize(sunDirection);
        var reference = MathF.Abs(forward.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
        var right = Vector3.Normalize(Vector3.Cross(forward, reference));
        var up = Vector3.Cross(right, forward);
        float extent = radius * 2f;
        float texel = extent / size;
        float sx = Vector3.Dot(centre, right), sy = Vector3.Dot(centre, up);
        centre += right * (MathF.Floor(sx / texel) * texel - sx);
        centre += up * (MathF.Floor(sy / texel) * texel - sy);
        float back = radius + Behind;
        var camera = new Camera3D(centre - forward * back, centre, reference, extent, CameraProjection.Orthographic);
        return (camera, 1.0, back + radius + 1.0);
    }
}
