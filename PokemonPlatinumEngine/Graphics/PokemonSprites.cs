using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Which way a Pokémon sprite looks: the opponent faces the camera, the player's Pokémon shows its back.</summary>
internal enum SpriteView { Front, Back, Icon }

/// <summary>Camera placement that frames one model for one view.</summary>
internal sealed class SpriteFraming
{
    public Camera3D Camera;
    public float Yaw;

    /// <summary>Row of the sprite (0 top .. 1 bottom) where the model's lowest point sits at rest.</summary>
    public float FeetRow;

    /// <summary>World units covered by one sprite pixel.</summary>
    public float WorldPerPixel;
}

/// <summary>
/// Renders the 3D Pokémon models into low-resolution pixel-art sprites for the menus (2D sprites, as in the main
/// games), baked once at start-up, and frames each model the way its sprite does (battles size the 3D models by
/// that frame).
/// </summary>
internal static class PokemonSprites
{
    public const int Size = 128;
    public const int IconSize = 48;

    private static readonly Dictionary<string, Texture2D> Baked = new(StringComparer.OrdinalIgnoreCase);

    // A soft studio light from the upper left, independent of the field's sun
    private static readonly SceneLighting Light = new(Vector3.Normalize(new Vector3(-0.55f, 0.7f, 0.55f)),
        new Vector3(0.6f, 0.58f, 0.53f), new Vector3(0.6f, 0.62f, 0.7f), new Vector3(0.42f, 0.4f, 0.38f));

    /// <summary>How a view of the model fills a sprite of <paramref name="size"/> pixels (no GPU calls).</summary>
    public static SpriteFraming Framing(PokeModel model, SpriteView view, int size)
    {
        if (model.Framings.TryGetValue((view, size), out var f)) return f;

        float yaw = view == SpriteView.Back ? 2.55f : -0.5f;
        float pitch = (view == SpriteView.Back ? 20f : 10f) * MathF.PI / 180f;
        var up = new Vector3(0, MathF.Cos(pitch), -MathF.Sin(pitch));
        var forward = new Vector3(0, -MathF.Sin(pitch), -MathF.Cos(pitch));

        // Project the sculpted (rest) pose to find the silhouette's extent in this view
        var turn = Matrix4x4.CreateRotationY(yaw);
        float minU = float.MaxValue, maxU = float.MinValue, minV = float.MaxValue, maxV = float.MinValue;
        foreach (var p in model.Mesh.Positions)
        {
            var q = Vector3.Transform(p, turn);
            float u = q.X, v = Vector3.Dot(q, up);
            minU = MathF.Min(minU, u); maxU = MathF.Max(maxU, u);
            minV = MathF.Min(minV, v); maxV = MathF.Max(maxV, v);
        }

        float fill = view == SpriteView.Icon ? 0.86f : model.Fill;
        float margin = model.Hovers ? 0.14f : 0.07f;

        // Leave head-room above tall models for crests, flames and the idle bounce
        float frame = MathF.Max((maxU - minU) / fill, (maxV - minV) / MathF.Min(fill, 0.86f - margin));
        float bottom = minV - margin * frame;
        float centerV = view == SpriteView.Icon ? (minV + maxV) / 2f : bottom + frame / 2f;
        var target = new Vector3((minU + maxU) / 2f, 0, 0) + up * centerV;

        f = new SpriteFraming
        {
            Camera = new Camera3D(target - forward * 40f, target, up, frame, CameraProjection.Orthographic),
            Yaw = yaw,
            FeetRow = view == SpriteView.Icon ? 0.5f + (centerV - minV) / frame : 1f - margin,
            WorldPerPixel = frame / size
        };
        return model.Framings.GetOrAdd((view, size), f);
    }

    /// <summary>Renders one frame of a Pokémon into <paramref name="target"/>. Call outside any other texture mode.</summary>
    /// <param name="flash">Blends the body toward a flat colour by this much (the white of an evolving Pokémon).</param>
    public static void Render(RenderContext context, PokeModel model, SpriteView view, PokePose pose, RenderTexture2D target, bool hullOutline = true,
        (Color Color, float Amount)? flash = null)
    {
        context.EnsureLoaded();
        int size = target.Texture.Width;
        var framing = Framing(model, view, size);
        var shaders = context.Shaders;

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(1.0, 100.0);
        Raylib.BeginMode3D(framing.Camera);

        shaders.SetLighting(Matrix4x4.Identity, Light, framing.Camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.32f);
        shaders.SetStudio();
        if (flash is { } f) shaders.SetFlash(f.Color, f.Amount);

        var turn = Matrix4x4.CreateRotationY(framing.Yaw);
        Rlgl.DisableBackfaceCulling();
        PokemonRenderer.Draw(context, model, pose, turn, CharacterPass.Color);
        if (flash != null) shaders.SetFlash(default, 0f);
        if (hullOutline) PokemonRenderer.Draw(context, model, pose, turn, CharacterPass.Outline);
        Rlgl.EnableBackfaceCulling();

        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }

    // ------------------------------------------------------------------ static sprites for menus

    /// <summary>Name under which the generic stand-in model is baked, for species whose sprite isn't ready yet.</summary>
    public const string Fallback = "?";

    /// <summary>Raise when baking changes (lighting, framing, the outline pass, the mesher), so cached sprites are baked again.</summary>
    public const int BakeVersion = 1;

    /// <summary>
    /// Where baked sprites are kept between runs (plan 03 · D5): three PNGs per species, named after it and a
    /// signature of its model, so a sprite is only baked again after its model changes. A missing or unreadable file
    /// just means baking again. Beside the executable, or in the shared folder <see cref="CacheFolders"/> names.
    /// </summary>
    public static string CacheFolder { get; set; } = CacheFolders.Sprites;

    public static bool CacheEnabled { get; set; } = true;

    private static readonly Queue<string> Waiting = new();
    private static readonly HashSet<string> Queued = new(StringComparer.OrdinalIgnoreCase);
    private static RenderTexture2D bakeBig, bakeSmall;
    private static bool targetsLoaded;

    private static readonly SpriteView[] Views = { SpriteView.Front, SpriteView.Back, SpriteView.Icon };

    /// <summary>
    /// Makes the sprites of these species ready now (from the cache, or baked, waiting for their models), and the
    /// stand-in's. Start-up uses it for the species the story shows first. Call outside any texture mode.
    /// </summary>
    public static void BakeAll(RenderContext context, IEnumerable<string> species)
    {
        foreach (var name in new List<string>(species) { Fallback })
            if (!Baked.ContainsKey(Key(name, SpriteView.Front)) && !TryLoad(name))
                BakeAndKeep(context, name, PokemonModels.Get(name));
    }

    /// <summary>Asks for a species' sprites: they are loaded or baked over the next frames by <see cref="Service"/>.</summary>
    public static void Request(string species)
    {
        if (species == Fallback || Baked.ContainsKey(Key(species, SpriteView.Front)) || !Queued.Add(species)) return;
        Waiting.Enqueue(species);
    }

    /// <summary>Whether any sprite is still waiting to be loaded or baked.</summary>
    public static bool Busy => Waiting.Count > 0;

    /// <summary>
    /// Loads or bakes the sprites asked for, within about <paramref name="budgetMs"/> a frame: from the cache when it
    /// has them, else from the model once it is built (it is requested in the background meanwhile). Call once a
    /// frame, outside any texture mode.
    /// </summary>
    public static void Service(RenderContext context, double budgetMs = 4.0)
    {
        if (Waiting.Count == 0) return;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        int rounds = Waiting.Count;
        for (int i = 0; i < rounds && clock.Elapsed.TotalMilliseconds < budgetMs; i++)
        {
            var name = Waiting.Dequeue();
            if (Baked.ContainsKey(Key(name, SpriteView.Front)) || TryLoad(name))
            {
                Queued.Remove(name);
                continue;
            }
            if (PokemonModels.TryGet(name, out var model))
            {
                BakeAndKeep(context, name, model);
                Queued.Remove(name);
                continue;
            }
            PokemonModels.Request(name);
            Waiting.Enqueue(name);
        }
    }

    /// <summary>Waits until every sprite asked for is ready (the harness, before it draws sheets of them).</summary>
    public static void Flush(RenderContext context)
    {
        while (Waiting.Count > 0)
        {
            var name = Waiting.Dequeue();
            Queued.Remove(name);
            if (!Baked.ContainsKey(Key(name, SpriteView.Front)) && !TryLoad(name)) BakeAndKeep(context, name, PokemonModels.Get(name));
        }
    }

    private static string Key(string name, SpriteView view) => $"{name}|{view}";

    private static string FileOf(string name, SpriteView view, string signature)
    {
        var safe = new System.Text.StringBuilder();
        foreach (char c in name) safe.Append(char.IsLetterOrDigit(c) && c < 128 ? char.ToLowerInvariant(c) : '_');
        return Path.Combine(CacheFolder, $"{safe}-{view.ToString().ToLowerInvariant()}-{signature}.png");
    }

    private static string SignatureOf(string name) => $"{PokemonModels.Signature(name)}{BakeVersion}";

    /// <summary>Takes a species' sprites from the cache if all three are there.</summary>
    private static bool TryLoad(string name)
    {
        if (!CacheEnabled) return false;
        string signature = SignatureOf(name);
        var files = Views.Select(v => FileOf(name, v, signature)).ToArray();
        if (!files.All(File.Exists)) return false;
        var textures = new List<Texture2D>();
        foreach (var file in files)
        {
            var tex = Raylib.LoadTexture(file);
            if (tex.Id == 0)
            {
                foreach (var t in textures) Raylib.UnloadTexture(t);
                return false;
            }
            Raylib.SetTextureFilter(tex, TextureFilter.Point);
            textures.Add(tex);
        }
        for (int i = 0; i < Views.Length; i++) Keep(name, Views[i], textures[i]);
        return true;
    }

    /// <summary>Bakes a species' three sprites, keeps them and writes them to the cache.</summary>
    private static void BakeAndKeep(RenderContext context, string name, PokeModel model)
    {
        if (!targetsLoaded)
        {
            bakeBig = Raylib.LoadRenderTexture(Size, Size);
            bakeSmall = Raylib.LoadRenderTexture(IconSize, IconSize);
            targetsLoaded = true;
        }
        string signature = SignatureOf(name);
        foreach (var view in Views)
        {
            var canvas = Bake(context, model, view, view == SpriteView.Icon ? bakeSmall : bakeBig, hull: view != SpriteView.Icon);
            Keep(name, view, canvas.ToTexture());
            if (!CacheEnabled) continue;
            string file = FileOf(name, view, signature);
            // Older bakes of the same sprite are of no more use, unless another tree shares the folder
            string stem = Path.GetFileName(file)[..^(signature.Length + 4)];
            var png = PngWriter.Encode(canvas);
            CacheFolders.Write(file, stream => stream.Write(png), () => Directory.GetFiles(CacheFolder, stem + "*.png"), CacheFolders.Prunes);
        }
    }

    private static void Keep(string name, SpriteView view, Texture2D texture)
    {
        if (Baked.TryGetValue(Key(name, view), out var old)) Raylib.UnloadTexture(old);
        Baked[Key(name, view)] = texture;
    }

    private static PixelCanvas Bake(RenderContext context, PokeModel model, SpriteView view, RenderTexture2D target, bool hull)
    {
        Render(context, model, view, new PokePose { Time = 0.35f }, target, hull);
        var image = Raylib.LoadImageFromTexture(target.Texture);
        Raylib.ImageFlipVertical(ref image);
        var canvas = PixelCanvas.FromImage(image);
        Raylib.UnloadImage(image);
        canvas.OutlinePass(innerSeams: false);
        return canvas;
    }

    /// <summary>
    /// A species' sprite for the menus, or null while it isn't ready: asking for one that isn't starts loading or
    /// baking it (see <see cref="Service"/>), and callers show the stand-in meanwhile.
    /// </summary>
    public static Texture2D? GetBaked(string name, SpriteView view)
    {
        if (Baked.TryGetValue(Key(name, view), out var tex)) return tex;
        Request(name);
        return null;
    }

    /// <summary>Forgets a species' sprites (and frees them), so they are made again when next asked for.</summary>
    public static void Forget(string name)
    {
        foreach (var view in Views)
            if (Baked.Remove(Key(name, view), out var tex)) Raylib.UnloadTexture(tex);
    }
}

