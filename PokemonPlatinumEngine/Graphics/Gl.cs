using System.Runtime.InteropServices;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The few OpenGL calls the game needs that raylib has no way to make: waiting for the graphics card (the frame
/// profiler), and making a depth texture answer comparisons (the shadow map). They are looked up in the system's
/// own OpenGL library; where that can't be found, <see cref="Available"/> is false and the callers do without.
/// </summary>
internal static unsafe class Gl
{
    private const uint Texture2D = 0x0DE1;
    private const uint TextureMagFilter = 0x2800, TextureMinFilter = 0x2801;
    private const uint TextureCompareMode = 0x884C, TextureCompareFunc = 0x884D;
    private const int CompareRefToTexture = 0x884E, Lequal = 0x0203, Linear = 0x2601;

    private const uint RendererName = 0x1F01;

    private static delegate* unmanaged<void> finish;
    private static delegate* unmanaged<uint, byte*> getString;
    private static delegate* unmanaged<uint, uint, void> bindTexture;
    private static delegate* unmanaged<uint, uint, int, void> texParameteri;
    private static bool looked;

    private static void Look()
    {
        if (looked) return;
        looked = true;
        foreach (string name in new[] { "opengl32.dll", "libGL.so.1", "/System/Library/Frameworks/OpenGL.framework/OpenGL" })
        {
            if (!NativeLibrary.TryLoad(name, out var library)) continue;
            if (NativeLibrary.TryGetExport(library, "glFinish", out var f)) finish = (delegate* unmanaged<void>)f;
            if (NativeLibrary.TryGetExport(library, "glBindTexture", out var b)) bindTexture = (delegate* unmanaged<uint, uint, void>)b;
            if (NativeLibrary.TryGetExport(library, "glTexParameteri", out var t)) texParameteri = (delegate* unmanaged<uint, uint, int, void>)t;
            if (NativeLibrary.TryGetExport(library, "glGetString", out var g)) getString = (delegate* unmanaged<uint, byte*>)g;
            break;
        }
    }

    /// <summary>Whether the calls were found on this machine.</summary>
    public static bool Available
    {
        get
        {
            Look();
            return finish != null && bindTexture != null && texParameteri != null;
        }
    }

    /// <summary>Waits until the graphics card has done everything it has been given.</summary>
    public static void Finish()
    {
        Look();
        if (finish != null) finish();
    }

    /// <summary>
    /// The name the driver gives the renderer (a graphics card's, or "llvmpipe" for Mesa's software one), or null
    /// where it can't be asked. Only asked while a window is open.
    /// </summary>
    public static string? Renderer()
    {
        Look();
        if (getString == null) return null;
        byte* name = getString(RendererName);
        return name == null ? null : Marshal.PtrToStringUTF8((nint)name);
    }

    /// <summary>
    /// Makes a depth texture compare instead of return its depth: a shader that samples it as a
    /// <c>sampler2DShadow</c> gets back how much of the four texels round the point lies beyond a depth, filtered
    /// by the card. Returns false where it can't be done.
    /// </summary>
    public static bool CompareDepth(uint textureId)
    {
        if (!Available) return false;
        bindTexture(Texture2D, textureId);
        texParameteri(Texture2D, TextureCompareMode, CompareRefToTexture);
        texParameteri(Texture2D, TextureCompareFunc, Lequal);
        texParameteri(Texture2D, TextureMinFilter, Linear);
        texParameteri(Texture2D, TextureMagFilter, Linear);
        bindTexture(Texture2D, 0);
        return true;
    }
}
