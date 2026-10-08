using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Pokémon in the field (plan 10 · F1; style guide, "Pokémon in the field"): a species' or a form's sprites baked
/// from its 3D model at the characters' density (<see cref="CharacterSprites.TexelsPerUnit"/>), so a Machop stands
/// shorter than the player and a Steelix taller. Four facings, each with two idle frames, on a square card of 32,
/// 48 or 64 texels (<see cref="ClassOf"/>) with the Pokémon's lowest point on its bottom row but one (the outline's).
/// Kept in <c>cache/sprites</c> beside the menu sprites, keyed by the same signature, and replaced by hand-drawn
/// frames in <c>overrides/sprites/pokemon/&lt;NAME&gt;/&lt;facing&gt;_idle_&lt;frame&gt;.png</c>. A facing not baked
/// yet is drawn as a Poké Ball by the caller, as the menus show their stand-in.
/// </summary>
internal static class FieldSprites
{
    public const int Small = 32, Medium = 48, Large = 64;

    /// <summary>Raise when the field's baking changes, so cached field sprites are baked again.</summary>
    public const int BakeVersion = 1;

    public const int IdleFrames = CharacterSprites.IdleFrames;

    /// <summary>Rows of the card under the Pokémon's lowest point: the outline's and the half texel the feet stand in.</summary>
    public const float FootRows = 1.5f;

    // Seen a little from above, as the people are
    private const float ViewPitchDeg = 24f;

    /// <summary>
    /// How tall a Pokémon stands in the field, in tiles, by its species' height in metres: the player is about 1.7
    /// tiles, so a Machop (0.8 m) comes to about half of that, and nothing is smaller than a fifth of a tile wide
    /// nor taller than the tallest card holds.
    /// </summary>
    public static float FieldHeight(float metres) => Math.Clamp(0.3f + 0.75f * metres, 0.4f, 1.9f);

    /// <summary>The card a Pokémon of that height in the field stands on: small under 0.7 tiles, big over 1.6.</summary>
    public static int ClassOf(float fieldHeight) => fieldHeight < 0.7f ? Small : fieldHeight <= 1.6f ? Medium : Large;

    /// <summary>A species' or a form's height in metres, from its data; half a metre for a name the data doesn't know.</summary>
    public static float MetresOf(string name)
    {
        if (PokemonDatabase.Get(name) is { } species) return species.Height;
        if (PokemonDatabase.SpeciesOfForm(name) is { } of) return of.Form(name)?.Height ?? of.Height;
        return 0.5f;
    }

    /// <summary>
    /// Makes whatever asks for a field sprite wait for the model instead of showing the stand-in: the harness sets
    /// it, so that two runs draw the same pictures.
    /// </summary>
    public static bool Patient { get; set; }

    public static string OverrideFolder { get; set; } = Path.Combine(AppContext.BaseDirectory, "overrides", "sprites", "pokemon");

    /// <summary>One facing's frames, and how many rows of its first frame the Pokémon fills above the card's foot.</summary>
    internal sealed record Strip(CharacterSprites.Card[] Frames, int Size, int Rows);

    private static readonly Dictionary<(string Name, int Facing), Strip> Baked = new();
    private static readonly Dictionary<int, RenderTexture2D> Targets = new();

    private static readonly SceneLighting StudioLight = new(Vector3.Normalize(new Vector3(-0.5f, 0.75f, 0.6f)),
        new Vector3(0.62f, 0.58f, 0.52f), new Vector3(0.6f, 0.62f, 0.7f), new Vector3(0.42f, 0.4f, 0.38f));

    /// <summary>A facing's frames if they are ready; null while they aren't (ask with <see cref="Prepare"/>).</summary>
    public static Strip? Get(string name, int facing) => Baked.GetValueOrDefault((name, facing));

    /// <summary>
    /// Makes a facing ready if it can be now: from an override or the cache, or baked once its model is built
    /// (asked for in the background meanwhile). Bakes only while <paramref name="bakes"/> is above nothing, and
    /// counts it down, so a frame bakes a facing or two at most. Call outside any texture mode.
    /// </summary>
    public static void Prepare(RenderContext context, string name, int facing, ref int bakes)
    {
        if (Baked.ContainsKey((name, facing))) return;
        if (TryOverride(context, name, facing) || TryLoad(context, name, facing)) return;
        if (bakes <= 0 && !Patient) return;
        PokeModel model;
        if (Patient) model = PokemonModels.Get(name);
        else if (!PokemonModels.TryGet(name, out model))
        {
            PokemonModels.Request(name);
            return;
        }
        bakes--;
        var sheet = Bake(context, model, name, facing);
        Keep(context, name, facing, sheet);
        if (!PokemonSprites.CacheEnabled) return;
        string file = FileOf(name, facing);
        string stem = Path.GetFileName(file)[..^(SignatureOf(name).Length + 4)];
        var png = PngWriter.Encode(sheet);
        CacheFolders.Write(file, stream => stream.Write(png), () => Directory.GetFiles(PokemonSprites.CacheFolder, stem + "*.png"), CacheFolders.Prunes);
    }

    private static string SignatureOf(string name) => $"{PokemonModels.Signature(name)}{PokemonSprites.BakeVersion}{BakeVersion}";

    private static string FileOf(string name, int facing)
    {
        var safe = new System.Text.StringBuilder();
        foreach (char c in name) safe.Append(char.IsLetterOrDigit(c) && c < 128 ? char.ToLowerInvariant(c) : '_');
        return Path.Combine(PokemonSprites.CacheFolder, $"{safe}-field-{facing}-{SignatureOf(name)}.png");
    }

    /// <summary>The name a hand-drawn frame is looked for under, in the species' folder: <c>down_idle_0.png</c>.</summary>
    public static string OverrideName(int facing, int frame) =>
        $"{(facing switch { 0 => "down", 1 => "right", 2 => "up", _ => "left" })}_idle_{frame}.png";

    private static bool TryOverride(RenderContext context, string name, int facing)
    {
        string folder = Path.Combine(OverrideFolder, name.ToUpperInvariant());
        if (!Directory.Exists(folder)) return false;
        PixelCanvas? sheet = null;
        for (int f = 0; f < IdleFrames; f++)
        {
            string path = Path.Combine(folder, OverrideName(facing, f));
            if (!File.Exists(path)) return false;
            var image = Raylib.LoadImage(path);
            try
            {
                if (image.Width != image.Height || image.Width is not (Small or Medium or Large)) return false;
                sheet ??= new PixelCanvas(image.Width * IdleFrames, image.Height);
                if (sheet.Height != image.Height) return false;
                sheet.Blit(PixelCanvas.FromImage(image), f * image.Width, 0);
            }
            finally
            {
                Raylib.UnloadImage(image);
            }
        }
        if (sheet == null) return false;
        Keep(context, name, facing, sheet);
        return true;
    }

    private static bool TryLoad(RenderContext context, string name, int facing)
    {
        if (!PokemonSprites.CacheEnabled) return false;
        string file = FileOf(name, facing);
        if (!File.Exists(file)) return false;
        var image = Raylib.LoadImage(file);
        try
        {
            if (image.Width != image.Height * IdleFrames || image.Height is not (Small or Medium or Large)) return false;
            Keep(context, name, facing, PixelCanvas.FromImage(image));
            return true;
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }

    /// <summary>Cuts a sheet of a facing's frames into cards and keeps them.</summary>
    private static void Keep(RenderContext context, string name, int facing, PixelCanvas sheet)
    {
        int size = sheet.Height;
        var frames = new CharacterSprites.Card[IdleFrames];
        for (int f = 0; f < IdleFrames; f++) frames[f] = CharacterSprites.MakeCard(context, sheet.Crop(f * size, 0, size, size));
        Baked[(name, facing)] = new Strip(frames, size, RowsFilled(sheet.Crop(0, 0, size, size)));
    }

    /// <summary>How many rows above the foot the art reaches (its topmost opaque row), for whoever sits on it.</summary>
    internal static int RowsFilled(PixelCanvas card)
    {
        for (int y = 0; y < card.Height; y++)
            for (int x = 0; x < card.Width; x++)
                if (card.Get(x, y).A > 0) return Math.Max(1, (int)MathF.Round(card.Height - FootRows - y));
        return 1;
    }

    /// <summary>The idle frame a moment shows: two breaths, as the people's.</summary>
    public static int FrameAt(float time) => MathF.Sin(time * PokemonAnimation.Beat) > 0f ? 1 : 0;

    /// <summary>
    /// How a model is placed on a card of a facing: its scale, so it stands <see cref="FieldHeight"/> tall but fits
    /// the card in every facing, and how far above its rest point its lowest point is seen (no GPU calls).
    /// </summary>
    internal static (int Size, float Scale) Fit(PokeModel model, string name)
    {
        float maxY = float.MinValue, minY = float.MaxValue;
        foreach (var p in model.Mesh.Positions)
        {
            maxY = MathF.Max(maxY, p.Y);
            minY = MathF.Min(minY, p.Y);
        }
        if (maxY <= minY) return (Small, 1f);
        float height = FieldHeight(MetresOf(name));
        int size = ClassOf(height);
        float scale = height / (maxY - MathF.Min(0f, minY));

        // Shrunk until it fits the card from every side: half the card less the outline across, and the card less
        // its foot and outline from the lowest point up
        float pitch = ViewPitchDeg * MathF.PI / 180f;
        float halfWidth = (size / 2f - 1f) / CharacterSprites.TexelsPerUnit, tall = (size - FootRows - 1f) / CharacterSprites.TexelsPerUnit;
        for (int facing = 0; facing < 4; facing++)
        {
            var turn = Matrix4x4.CreateRotationY(facing * MathF.PI / 2f);
            float across = 0f, low = float.MaxValue, high = float.MinValue;
            foreach (var p in model.Mesh.Positions)
            {
                var q = Vector3.Transform(p, turn) * scale;
                float v = q.Y * MathF.Cos(pitch) - q.Z * MathF.Sin(pitch);
                across = MathF.Max(across, MathF.Abs(q.X));
                low = MathF.Min(low, v);
                high = MathF.Max(high, v);
            }
            float shrink = MathF.Min(across > halfWidth ? halfWidth / across : 1f, high - MathF.Min(low, 0f) > tall ? tall / (high - MathF.Min(low, 0f)) : 1f);
            scale *= shrink;
        }
        return (size, scale);
    }

    /// <summary>Renders a facing's idle frames side by side, outlined. Call outside any texture mode.</summary>
    internal static PixelCanvas Bake(RenderContext context, PokeModel model, string name, int facing)
    {
        context.EnsureLoaded();
        var (size, scale) = Fit(model, name);
        if (!Targets.TryGetValue(size, out var target)) Targets[size] = target = Raylib.LoadRenderTexture(size, size);

        float yaw = facing * MathF.PI / 2f;
        var root = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(yaw);
        float pitch = ViewPitchDeg * MathF.PI / 180f;

        // The lowest point seen sits on the foot row: the rest point (the ground) unless something reaches lower
        float low = 0f;
        foreach (var p in model.Mesh.Positions)
        {
            var q = Vector3.Transform(p, root);
            low = MathF.Min(low, q.Y * MathF.Cos(pitch) - q.Z * MathF.Sin(pitch));
        }
        float units = size / (float)CharacterSprites.TexelsPerUnit;
        float centre = units / 2f - FootRows / CharacterSprites.TexelsPerUnit + low;
        var up = new Vector3(0, MathF.Cos(pitch), -MathF.Sin(pitch));
        var look = up * centre;
        var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 20f, look, Vector3.UnitY, units, CameraProjection.Orthographic);

        var sheet = new PixelCanvas(size * IdleFrames, size);
        var shaders = context.Shaders;
        for (int f = 0; f < IdleFrames; f++)
        {
            // Breathed out, then in: a quarter and three quarters of the idle's beat
            var pose = new PokePose { Time = (f == 0 ? 0.25f : 0.75f) * MathF.Tau / PokemonAnimation.Beat / MathF.Max(0.1f, model.Tempo) };
            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            Rlgl.SetClipPlanes(1.0, 60.0);
            Raylib.BeginMode3D(camera);
            shaders.SetLighting(Matrix4x4.Identity, StudioLight, camera.Position, 1f);
            shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.2f);
            shaders.SetStudio();
            Rlgl.DisableBackfaceCulling();
            PokemonRenderer.Draw(context, model, pose, root, CharacterPass.Color);
            Rlgl.EnableBackfaceCulling();
            Raylib.EndMode3D();
            Raylib.EndTextureMode();
            Rlgl.SetClipPlanes(0.01, 1000.0);

            var image = Raylib.LoadImageFromTexture(target.Texture);
            Raylib.ImageFlipVertical(ref image);
            var canvas = PixelCanvas.FromImage(image);
            Raylib.UnloadImage(image);
            canvas.OutlinePass(innerSeams: false);
            sheet.Blit(canvas, f * size, 0);
        }
        return sheet;
    }

    /// <summary>
    /// Every facing's frames of a species side by side, enlarged without filtering (the harness's boards). Bakes
    /// what isn't baked; call outside any texture mode.
    /// </summary>
    internal static Image Sheet(RenderContext context, string name, int scale)
    {
        var model = PokemonModels.Get(name);
        PixelCanvas? all = null;
        for (int facing = 0; facing < 4; facing++)
        {
            var sheet = Bake(context, model, name, facing);
            all ??= new PixelCanvas(sheet.Width * 4, sheet.Height);
            all.Blit(sheet, facing * sheet.Width, 0);
        }
        return all!.ToImage(scale);
    }
}
