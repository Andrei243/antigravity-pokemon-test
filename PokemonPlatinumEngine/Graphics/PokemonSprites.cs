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

    /// <summary>
    /// Half the width of the model's silhouette as this view turns it, in model units (plan 09 · L2): how far from its
    /// middle the edge of its body is, for what is drawn beside it in battle.
    /// </summary>
    public float HalfWidth;
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

    /// <summary>
    /// How many species' sprites are held at most (plan 03 · D13): eight boxes' worth, about 34 MB of textures.
    /// Beyond that the ones longest unasked for are freed, and read from the cache again when next asked for.
    /// </summary>
    public const int Kept = 240;

    /// <summary>The frames a sprite is kept for after it was last asked for, whatever else is held: two seconds.</summary>
    public const int FreshFrames = 120;

    private static readonly SpriteLedger Ledger = new(Kept, FreshFrames);

    /// <summary>
    /// How many models are being built for the sprites' sake at a time. A screen can ask for thirty species' sprites
    /// in one frame (a box), and a model is built much faster than a sprite is baked from it: asked for all at
    /// once, they would all be in memory waiting their turn.
    /// </summary>
    public const int MostOnLoan = 16;

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
            WorldPerPixel = frame / size,
            HalfWidth = (maxU - minU) / 2f
        };
        return model.Framings.GetOrAdd((view, size), f);
    }

    /// <summary>Renders one frame of a Pokémon into <paramref name="target"/>. Call outside any other texture mode.</summary>
    /// <param name="flash">Blends the body toward a flat colour by this much (the white of an evolving Pokémon).</param>
    public static void Render(RenderContext context, PokeModel model, SpriteView view, PokePose pose, RenderTexture2D target, bool hullOutline = true,
        (Color Color, float Amount)? flash = null, int stripFrame = -1)
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
        // A frame of the strip (plan 09 · L5) is posed once, and both passes draw it as it stands
        bool posed = stripFrame >= 0;
        if (posed)
        {
            PokemonRenderer.EnsureUploaded(model, shaders);
            model.AnimateStrip(stripFrame, StripFrames, StripStart);
        }
        PokemonRenderer.Draw(context, model, pose, turn, CharacterPass.Color, posed);
        if (flash != null) shaders.SetFlash(default, 0f);
        if (hullOutline) PokemonRenderer.Draw(context, model, pose, turn, CharacterPass.Outline, posed);
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
        {
            if (!Baked.ContainsKey(Key(name, SpriteView.Front)) && !TryLoad(name))
            {
                // A model nobody had asked for is built for this baking alone, and let go after it
                bool passing = !PokemonModels.Holds(name);
                BakeAndKeep(context, name, PokemonModels.Get(name));
                if (passing) PokemonModels.Release(name);
            }
            // What the start makes is never let go: the stand-in must always be there to show
            Ledger.Keep(name, forGood: true);
        }
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
        // Between frames, when nothing drawn is still on its way to the screen: the sprites longest unasked for go
        Ledger.Tick();
        foreach (var name in Ledger.Overflow()) Forget(name);
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
                // A model built for these sprites alone has done its work (plan 03 · D13), unless its strip waits for it
                if (!QueuedStrips.Contains(name)) PokemonModels.Return(name);
                continue;
            }
            // (one already on its way, for this or for the game, is waited for; a new one is asked for while few are)
            if (!PokemonModels.Holds(name) && PokemonModels.OnLoan < MostOnLoan) PokemonModels.Borrow(name);
            Waiting.Enqueue(name);
        }
        // The strips (plan 09 · L5) after the stills, in what is left of the budget: a list's stills come first
        ServiceStrips(context, clock, budgetMs);
    }

    /// <summary>Waits until every strip asked for is ready (the harness, before it shoots a screen that plays one).</summary>
    public static void FlushStrips(RenderContext context)
    {
        while (WaitingStrips.Count > 0)
        {
            var name = WaitingStrips.Dequeue();
            QueuedStrips.Remove(name);
            if (Strips.ContainsKey(StripKey(name)) || TryLoadStrip(name)) continue;
            bool passing = !PokemonModels.Holds(name);
            BakeStrip(context, name, PokemonModels.Get(name));
            // (one borrowed for its sake while it waited in the game's queue is given back)
            if (passing) PokemonModels.Release(name);
            else PokemonModels.Return(name);
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

    /// <summary>How many species' menu sprites are held now (three textures each).</summary>
    public static int KeptCount => Baked.Count / Views.Length;

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
        Ledger.Keep(name);
    }

    private static PixelCanvas Bake(RenderContext context, PokeModel model, SpriteView view, RenderTexture2D target, bool hull)
    {
        Render(context, model, view, new PokePose { Time = StripStart }, target, hull);
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
        if (Baked.TryGetValue(Key(name, view), out var tex))
        {
            Ledger.Touch(name);
            return tex;
        }
        Request(name);
        return null;
    }

    /// <summary>Forgets a species' sprites (and frees them), so they are made again when next asked for.</summary>
    public static void Forget(string name)
    {
        foreach (var view in Views)
            if (Baked.Remove(Key(name, view), out var tex)) Raylib.UnloadTexture(tex);
        Ledger.Forget(name);
    }

    // ------------------------------------------------------------------ strips: the front sprite breathing (plan 09 · L5)

    /// <summary>The frames of a strip: one breath of the idle loop in eight (style guide, "Pokémon in menus").</summary>
    public const int StripFrames = 8;

    /// <summary>The moment of the idle loop the still is baked at, and the strip's first frame.</summary>
    public const float StripStart = 0.35f;

    /// <summary>
    /// Raise when a strip's baking changes. A strip has a version of its own beside <see cref="BakeVersion"/>, so that
    /// the strips came in without baking every still again (plan 09 · L5's decision).
    /// </summary>
    public const int StripVersion = 1;

    /// <summary>
    /// How many strips are held at once. A strip is eight stills' texture (512 KB), and only the one Pokémon looked at
    /// on a screen plays one: the Pokédex's entry, the summary, the chosen starter, with room for the one just left.
    /// </summary>
    public const int StripsKept = 4;

    /// <summary>A strip held: its texture (the frames side by side) and the seconds of the breath it plays over.</summary>
    public readonly record struct Strip(Texture2D Texture, float Period)
    {
        /// <summary>The frame shown at <paramref name="seconds"/> of <see cref="FrameClock"/>.</summary>
        public int FrameAt(double seconds) => StripFrameAt(seconds, Period);

        /// <summary>Where frame <paramref name="frame"/> lies on the texture.</summary>
        public Rectangle Source(int frame) => new(frame * Size, 0, Size, Size);
    }

    private static readonly Dictionary<string, Strip> Strips = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>The frames a strip is kept for after it was last asked for: half a second, since one plays only while its screen is open.</summary>
    public const int StripFreshFrames = 30;

    private static readonly SpriteLedger StripLedger = new(StripsKept, StripFreshFrames);
    private static readonly Queue<string> WaitingStrips = new();
    private static readonly HashSet<string> QueuedStrips = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// For a tool (the harness): every strip asked for in a frame is made ready by the next frame's
    /// <see cref="Service"/>, whatever it takes, so a screen breathes from the same frame every run, as
    /// <see cref="FieldSprites.Patient"/> does for the field's sprites. The game leaves it off.
    /// </summary>
    public static bool PatientStrips { get; set; }

    /// <summary>How many strips are held now.</summary>
    public static int StripCount => Strips.Count;

    /// <summary>The frame of a strip over a breath of <paramref name="period"/> seconds shown at <paramref name="seconds"/>. No GPU calls.</summary>
    public static int StripFrameAt(double seconds, float period)
    {
        double step = Math.Max(0.01, period) / StripFrames;
        long frame = (long)Math.Floor(seconds / step) % StripFrames;
        return (int)(frame < 0 ? frame + StripFrames : frame);
    }

    /// <summary>The key a strip is held under: the look's name and the frame count.</summary>
    public static string StripKey(string name) => $"{name}|strip{StripFrames}";

    /// <summary>
    /// The cache file of a strip: the look's name, the frame count, the signature and the breath's length in
    /// milliseconds (which a strip read back from the cache can't ask of a model it hasn't built).
    /// </summary>
    public static string StripFile(string name, string signature, float period) =>
        Path.Combine(CacheFolder, $"{StripStem(name)}{signature}-{(int)MathF.Round(period * 1000f)}.png");

    private static string StripStem(string name)
    {
        var safe = new System.Text.StringBuilder();
        foreach (char c in name) safe.Append(char.IsLetterOrDigit(c) && c < 128 ? char.ToLowerInvariant(c) : '_');
        return $"{safe}-strip{StripFrames}-";
    }

    private static string StripSignatureOf(string name) => $"{PokemonModels.Signature(name)}{BakeVersion}s{StripVersion}";

    /// <summary>
    /// A look's strip for the one Pokémon a screen looks at, or null while it isn't ready (asking starts it, as for
    /// <see cref="GetBaked"/>; the still is shown meanwhile). Never asked for by a list: <see cref="StripsKept"/> are held.
    /// </summary>
    public static Strip? GetStrip(string name)
    {
        if (name == Fallback) return null;
        if (Strips.TryGetValue(StripKey(name), out var strip))
        {
            StripLedger.Touch(name);
            return strip;
        }
        if (QueuedStrips.Add(name)) WaitingStrips.Enqueue(name);
        return null;
    }

    /// <summary>Loads or bakes the strips asked for, within what is left of <see cref="Service"/>'s budget.</summary>
    private static void ServiceStrips(RenderContext context, System.Diagnostics.Stopwatch clock, double budgetMs)
    {
        StripLedger.Tick();
        foreach (var name in StripLedger.Overflow()) ForgetStrip(name);
        if (PatientStrips)
        {
            FlushStrips(context);
            return;
        }
        int rounds = WaitingStrips.Count;
        for (int i = 0; i < rounds && clock.Elapsed.TotalMilliseconds < budgetMs; i++)
        {
            var name = WaitingStrips.Dequeue();
            if (Strips.ContainsKey(StripKey(name)) || TryLoadStrip(name))
            {
                QueuedStrips.Remove(name);
                continue;
            }
            if (PokemonModels.TryGet(name, out var model))
            {
                BakeStrip(context, name, model);
                QueuedStrips.Remove(name);
                PokemonModels.Return(name);
                continue;
            }
            if (!PokemonModels.Holds(name) && PokemonModels.OnLoan < MostOnLoan) PokemonModels.Borrow(name);
            WaitingStrips.Enqueue(name);
        }
    }

    private static bool TryLoadStrip(string name)
    {
        if (!CacheEnabled || !Directory.Exists(CacheFolder)) return false;
        string signature = StripSignatureOf(name), stem = StripStem(name) + signature + "-";
        var file = Directory.GetFiles(CacheFolder, stem + "*.png").FirstOrDefault();
        if (file == null) return false;
        if (!int.TryParse(Path.GetFileNameWithoutExtension(file)[stem.Length..], out int ms) || ms <= 0) return false;
        var tex = Raylib.LoadTexture(file);
        if (tex.Id == 0) return false;
        if (tex.Width != Size * StripFrames || tex.Height != Size)
        {
            Raylib.UnloadTexture(tex);
            return false;
        }
        Raylib.SetTextureFilter(tex, TextureFilter.Point);
        KeepStrip(name, new Strip(tex, ms / 1000f));
        return true;
    }

    /// <summary>Bakes a look's strip frame by frame as the stills are baked, keeps it and writes it to the cache.</summary>
    private static void BakeStrip(RenderContext context, string name, PokeModel model)
    {
        if (!targetsLoaded)
        {
            bakeBig = Raylib.LoadRenderTexture(Size, Size);
            bakeSmall = Raylib.LoadRenderTexture(IconSize, IconSize);
            targetsLoaded = true;
        }
        var strip = new PixelCanvas(Size * StripFrames, Size);
        for (int f = 0; f < StripFrames; f++)
        {
            Render(context, model, SpriteView.Front, new PokePose { Time = StripStart }, bakeBig, hullOutline: true, stripFrame: f);
            var image = Raylib.LoadImageFromTexture(bakeBig.Texture);
            Raylib.ImageFlipVertical(ref image);
            var canvas = PixelCanvas.FromImage(image);
            Raylib.UnloadImage(image);
            canvas.OutlinePass(innerSeams: false);
            // Copied texel for texel, as the still is kept: no blending of what isn't opaque
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++) strip.SetRaw(f * Size + x, y, canvas.Get(x, y));
        }
        float period = PokemonAnimation.BreathSeconds(model);
        KeepStrip(name, new Strip(strip.ToTexture(), period));
        if (!CacheEnabled) return;
        string file = StripFile(name, StripSignatureOf(name), period);
        var png = PngWriter.Encode(strip);
        CacheFolders.Write(file, stream => stream.Write(png), () => Directory.GetFiles(CacheFolder, StripStem(name) + "*.png"), CacheFolders.Prunes);
    }

    private static void KeepStrip(string name, Strip strip)
    {
        if (Strips.TryGetValue(StripKey(name), out var old)) Raylib.UnloadTexture(old.Texture);
        Strips[StripKey(name)] = strip;
        StripLedger.Keep(name);
    }

    /// <summary>Forgets a look's strip (and frees it).</summary>
    public static void ForgetStrip(string name)
    {
        if (Strips.Remove(StripKey(name), out var strip)) Raylib.UnloadTexture(strip.Texture);
        StripLedger.Forget(name);
    }
}

