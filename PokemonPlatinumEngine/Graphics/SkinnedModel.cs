using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// How the character shader lights each <see cref="SurfaceMaterial"/> (style guide, "Materials"): the width of the
/// soft terminator, a crisp highlight band (strength and sharpness), light of its own, and the tint of its shade.
/// Material 0 is the plain look every model had before materials, so models without ids are unchanged.
/// </summary>
internal static class SurfaceMaterials
{
    public const int Count = 12;

    /// <summary>Terminator width, highlight strength, highlight sharpness (exponent), own light; by material.</summary>
    public static readonly Vector4[] LightTable =
    {
        new(0.12f, 0f, 1f, 0f),     // Default
        new(0.22f, 0f, 1f, 0f),     // Skin: a softer edge to the shade
        new(0.16f, 0f, 1f, 0f),     // Cloth
        new(0.10f, 0.14f, 10f, 0f), // Hair: a soft sheen
        new(0.08f, 0.40f, 36f, 0f), // Leather: shoes, bags, belts
        new(0.10f, 0.35f, 48f, 0f), // Plastic: buttons, the briefcase's clasps
        new(0.06f, 0.60f, 64f, 0f), // Metal
        new(0.12f, 0f, 1f, 0.85f),  // Glow: lit from within
        new(0.20f, 0f, 1f, 0f),     // Fur and feathers: soft and matt
        new(0.12f, 0.12f, 26f, 0f), // Scales and smooth hide: a small sheen
        new(0.08f, 0.36f, 30f, 0f), // Shell, horn, claws, beaks and teeth: hard and glossy
        new(0.22f, 0.06f, 8f, 0f)   // Leaves
    };

    /// <summary>Shade tint (multiplies the sky light where the sun doesn't reach) and rim strength; by material.</summary>
    public static readonly Vector4[] ShadeTable =
    {
        new(1f, 1f, 1f, 1f),
        new(1.10f, 0.96f, 0.94f, 1f),  // warm shade on skin, never grey
        new(1f, 1f, 1f, 0.9f),
        new(1f, 1f, 1.02f, 1f),
        new(1f, 1f, 1f, 1f),
        new(1f, 1f, 1f, 1f),
        new(0.95f, 0.97f, 1.05f, 1f),
        new(1f, 1f, 1f, 0.5f),
        new(1.03f, 1f, 0.98f, 1.15f),  // fur catches a little more rim light
        new(1f, 1f, 1.02f, 1f),
        new(1f, 1f, 1f, 0.9f),
        new(0.94f, 1.06f, 0.94f, 1.25f) // leaves: green bounce in the shade, a bright sunlit edge
    };
}

/// <summary>
/// An <see cref="SdfMesh"/> on the GPU, skinned by the skinned character programs (<see cref="FieldShaders"/>):
/// bone indices and weights per vertex, the surface material in the second texture coordinate. One upload serves
/// every pose: the bone matrices are set for each draw.
/// </summary>
internal sealed class SkinnedModel
{
    /// <summary>Bones a model may have (the shaders' uniform array holds three rows for each).</summary>
    public const int MaxBones = 32;

    public Mesh Mesh;
    public int BoneCount { get; private init; }
    private readonly Vector4[] rows = new Vector4[MaxBones * 3];

    private static readonly System.Collections.Generic.Dictionary<uint, int> BoneLocations = new();

    public static unsafe SkinnedModel Upload(SdfMesh source, int boneCount, Vector2[]? uvs = null, Func<Color, Color>? recolor = null)
    {
        if (source.VertexCount > ushort.MaxValue) throw new ArgumentException($"{source.VertexCount} vertices is more than one mesh can index");
        if (boneCount > MaxBones) throw new ArgumentException($"{boneCount} bones; at most {MaxBones}");

        int n = source.VertexCount;
        var mesh = new Mesh(n, source.TriangleCount);
        mesh.AllocVertices();
        mesh.AllocNormals();
        mesh.AllocColors();
        mesh.AllocTexCoords();
        mesh.AllocTexCoords2();
        mesh.AllocIndices();
        mesh.BoneCount = boneCount;
        mesh.BoneIndices = (byte*)Raylib.MemAlloc((uint)(n * 4));
        mesh.BoneWeights = (float*)Raylib.MemAlloc((uint)(n * 4 * sizeof(float)));

        var pos = mesh.VerticesAs<Vector3>();
        var nor = mesh.NormalsAs<Vector3>();
        var col = mesh.ColorsAs<Color>();
        var uv = mesh.TexCoordsAs<Vector2>();
        var uv2 = mesh.TexCoords2As<Vector2>();
        var idx = mesh.IndicesAs<ushort>();
        for (int i = 0; i < n; i++)
        {
            pos[i] = source.Positions[i];
            nor[i] = source.Normals[i];
            col[i] = recolor != null ? recolor(source.Colors[i]) : source.Colors[i];
            uv[i] = uvs != null ? uvs[i] : Vector2.Zero;
            uv2[i] = new Vector2(source.Materials[i], 0f);
        }
        for (int i = 0; i < n * 4; i++)
        {
            mesh.BoneIndices[i] = source.BoneIndices[i];
            mesh.BoneWeights[i] = source.BoneWeights[i];
        }
        for (int i = 0; i < source.Indices.Length; i++) idx[i] = (ushort)source.Indices[i];

        Raylib.UploadMesh(ref mesh, false);
        AttachBones(ref mesh, n);
        return new SkinnedModel { Mesh = mesh, BoneCount = boneCount };
    }

    // Where raylib binds the vertexBoneIndices and vertexBoneWeights attributes in every shader it loads
    private const uint BoneIndexLocation = 7, BoneWeightLocation = 8;
    private const int GlUnsignedByte = 0x1401, GlFloat = 0x1406;

    /// <summary>
    /// Puts the bone indices and weights in buffers on the mesh's vertex array. The raylib this game ships with
    /// binds those attributes in its shaders but doesn't upload them in UploadMesh, so without this every vertex
    /// reads the default values and follows a single bone. UnloadMesh frees the buffers with the rest.
    /// </summary>
    private static unsafe void AttachBones(ref Mesh mesh, int vertexCount)
    {
        if (mesh.VboId[BoneIndexLocation] != 0 && mesh.VboId[BoneWeightLocation] != 0) return;
        Rlgl.EnableVertexArray(mesh.VaoId);
        uint indices = Rlgl.LoadVertexBuffer(mesh.BoneIndices, vertexCount * 4, false);
        Rlgl.SetVertexAttribute(BoneIndexLocation, 4, GlUnsignedByte, false, 0, 0);
        Rlgl.EnableVertexAttribute(BoneIndexLocation);
        uint weights = Rlgl.LoadVertexBuffer(mesh.BoneWeights, vertexCount * 4 * sizeof(float), false);
        Rlgl.SetVertexAttribute(BoneWeightLocation, 4, GlFloat, false, 0, 0);
        Rlgl.EnableVertexAttribute(BoneWeightLocation);
        Rlgl.DisableVertexArray();
        mesh.VboId[BoneIndexLocation] = indices;
        mesh.VboId[BoneWeightLocation] = weights;
    }

    /// <summary>
    /// Draws the model posed by <paramref name="skin"/> (from <see cref="Skeleton.Evaluate"/>) and placed by
    /// <paramref name="model"/> (System.Numerics row-vector convention), with a skinned program's material.
    /// </summary>
    public void Draw(Material material, ReadOnlySpan<Matrix4x4> skin, Matrix4x4 model)
    {
        int count = Math.Min(BoneCount, skin.Length);
        for (int b = 0; b < count; b++)
        {
            // Rows of the column-vector matrix the shader multiplies with: the columns of the row-vector one
            var m = skin[b];
            rows[b * 3] = new Vector4(m.M11, m.M21, m.M31, m.M41);
            rows[b * 3 + 1] = new Vector4(m.M12, m.M22, m.M32, m.M42);
            rows[b * 3 + 2] = new Vector4(m.M13, m.M23, m.M33, m.M43);
        }

        var shader = material.Shader;
        if (!BoneLocations.TryGetValue(shader.Id, out int location))
        {
            location = Raylib.GetShaderLocation(shader, "bones");
            BoneLocations[shader.Id] = location;
        }
        if (location >= 0) Raylib.SetShaderValueV(shader, location, rows, ShaderUniformDataType.Vec4, count * 3);
        Raylib.DrawMesh(Mesh, material, Matrix4x4.Transpose(model));
        FrameProfiler.Count(Mesh.TriangleCount);
    }

    public void Unload() => Raylib.UnloadMesh(Mesh);
}
