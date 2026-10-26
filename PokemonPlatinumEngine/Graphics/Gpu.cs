using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Every load onto the graphics card and every unload from it, each told to <see cref="GpuLedger.Game"/> under its
/// owner (plan 16 · T21). Nothing else in the engine calls raylib's or rlgl's loads and unloads
/// (<c>GpuLedgerTests</c> reads the sources): a new kind of load is a method here. The owners are short words
/// (<c>sprites</c>, <c>chunk</c>, <c>rig</c>, <c>pokemon</c>, <c>post</c>, <c>shadow</c>, <c>ui</c>…) that the soak
/// run counts by.
/// </summary>
public static unsafe class Gpu
{
    private static GpuLedger Ledger => GpuLedger.Game;

    // Textures

    public static Texture2D TextureFromImage(Image image, string owner)
    {
        var texture = Raylib.LoadTextureFromImage(image);
        Ledger.Add(GpuKind.Texture, texture.Id, owner, GpuLedger.PictureBytes(texture.Width, texture.Height));
        return texture;
    }

    public static Texture2D TextureFromFile(string file, string owner)
    {
        var texture = Raylib.LoadTexture(file);
        Ledger.Add(GpuKind.Texture, texture.Id, owner, GpuLedger.PictureBytes(texture.Width, texture.Height));
        return texture;
    }

    public static void Unload(Texture2D texture)
    {
        Ledger.Remove(GpuKind.Texture, texture.Id);
        Raylib.UnloadTexture(texture);
    }

    /// <summary>A texture of rlgl's own, with nothing in it yet.</summary>
    public static uint RawTexture(int width, int height, PixelFormat format, string owner)
    {
        uint id = Rlgl.LoadTexture(null, width, height, format, 1);
        // Both formats the game asks for (RGBA8 and R32) are four bytes a texel
        Ledger.Add(GpuKind.Texture, id, owner, GpuLedger.PictureBytes(width, height));
        return id;
    }

    public static uint DepthTexture(int width, int height, string owner)
    {
        uint id = Rlgl.LoadTextureDepth(width, height, false);
        Ledger.Add(GpuKind.Texture, id, owner, GpuLedger.PictureBytes(width, height));
        return id;
    }

    public static void UnloadRawTexture(uint id)
    {
        Ledger.Remove(GpuKind.Texture, id);
        Rlgl.UnloadTexture(id);
    }

    // Render targets and framebuffers

    /// <summary>raylib's render texture: a colour texture and a depth buffer on a framebuffer.</summary>
    public static RenderTexture2D Target(int width, int height, string owner)
    {
        var target = Raylib.LoadRenderTexture(width, height);
        Ledger.Add(GpuKind.Target, target.Id, owner, GpuLedger.PictureBytes(width, height) * 2);
        return target;
    }

    public static void Unload(RenderTexture2D target)
    {
        Ledger.Remove(GpuKind.Target, target.Id);
        Raylib.UnloadRenderTexture(target);
    }

    public static uint Framebuffer(string owner)
    {
        uint id = Rlgl.LoadFramebuffer();
        Ledger.Add(GpuKind.Framebuffer, id, owner, 0);
        return id;
    }

    public static void UnloadFramebuffer(uint id)
    {
        Ledger.Remove(GpuKind.Framebuffer, id);
        Rlgl.UnloadFramebuffer(id);
    }

    // Meshes and buffers

    public static void Upload(ref Mesh mesh, string owner)
    {
        Raylib.UploadMesh(ref mesh, false);
        Ledger.Add(GpuKind.Mesh, mesh.VaoId, owner, BytesOf(mesh));
    }

    public static void Unload(Mesh mesh)
    {
        Ledger.Remove(GpuKind.Mesh, mesh.VaoId);
        Raylib.UnloadMesh(mesh);
    }

    public static uint VertexBuffer(void* data, int size, string owner)
    {
        uint id = Rlgl.LoadVertexBuffer(data, size, false);
        Ledger.Add(GpuKind.Buffer, id, owner, size);
        return id;
    }

    public static void UnloadVertexBuffer(uint id)
    {
        Ledger.Remove(GpuKind.Buffer, id);
        Rlgl.UnloadVertexBuffer(id);
    }

    /// <summary>A mesh's buffers as raylib uploads them.</summary>
    public static long BytesOf(Mesh mesh)
    {
        long n = mesh.VertexCount, per = 0;
        if (mesh.Vertices != null) per += 12;
        if (mesh.Normals != null) per += 12;
        if (mesh.TexCoords != null) per += 8;
        if (mesh.TexCoords2 != null) per += 8;
        if (mesh.Colors != null) per += 4;
        if (mesh.Tangents != null) per += 16;
        long indices = mesh.Indices != null ? (long)mesh.TriangleCount * 3 * sizeof(ushort) : 0;
        return n * per + indices;
    }

    // Shaders and fonts

    public static Shader ShaderFrom(string? vertex, string? fragment, string owner)
    {
        var shader = Raylib.LoadShaderFromMemory(vertex, fragment);
        Ledger.Add(GpuKind.Shader, shader.Id, owner, 0);
        return shader;
    }

    public static void Unload(Shader shader)
    {
        Ledger.Remove(GpuKind.Shader, shader.Id);
        Raylib.UnloadShader(shader);
    }

    public static Font FontFrom(string path, int size, int[] codepoints, string owner)
    {
        var font = Raylib.LoadFontEx(path, size, codepoints, codepoints.Length);
        Ledger.Add(GpuKind.Font, font.Texture.Id, owner, GpuLedger.PictureBytes(font.Texture.Width, font.Texture.Height));
        return font;
    }
}
