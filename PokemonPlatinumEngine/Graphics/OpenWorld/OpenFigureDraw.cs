using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// The open world's figures drawn (plan 32 · OW9; style guide, "Open world", "People and Pokémon"): the people as rigs
/// of the grown kit and the Pokémon as their models at their heights in metres, the twelve nearest within
/// <see cref="OpenFigures.LiveReach"/> live (posed walking by how far they moved, with their outline's shell within
/// <see cref="OpenFigures.OutlineReach"/>: a Pokémon's three levels are its model with its shell, without it, and its
/// impostor), the rest as impostors: cards turned to the camera by the impostor program, each showing the one of eight
/// views of its look (baked once a look, one look a frame, into a shared atlas of <see cref="MostLooks"/> rows) nearest
/// the camera's side of it, turned by the figure's own way. A trainer who has caught the player's eye has the "!" over
/// their head, popping up as the grid's does. A look whose rig or model isn't made yet is asked for and drawn once it
/// is (never waited for). Every load through <see cref="Gpu"/>, under <c>openfigure</c>.
/// </summary>
internal sealed class OpenFigureDraw
{
    /// <summary>
    /// An impostor's view, in texels (square: the people framed 2 m tall, a Pokémon a little over its height; a figure
    /// past the live reach stands under fifty pixels of the 1080p scene), the views across and the looks the atlas holds.
    /// </summary>
    public const int Cell = 96, Views = 8, MostLooks = 32;

    /// <summary>How tall a person's impostor is framed, in metres.</summary>
    public const float PersonFrame = 2.0f;

    /// <summary>How many impostors a frame draws at most.</summary>
    public const int MostImpostors = 128;

    private readonly Dictionary<string, (int Row, float Frame, double Used)> rows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (Vector3 Last, float Walk, float Speed, double Exclaimed)> motion = new();
    // Each person's grown motion (plan 30 · FM6): its gait, yaw, lean and turns from how fast its feet moved over the ground
    private readonly Dictionary<string, GrownMotion> grownMotion = new();
    private Texture2D atlas;
    private RenderTexture2D bakeTarget;
    private Mesh cards;
    private Material cardMaterial;
    private bool loaded;
    private double lastTime = double.NaN;
    private readonly List<int> live = new(), far = new();
    private IReadOnlyList<OpenFigure> figures = Array.Empty<OpenFigure>();

    // What the last frame drew, for the harness
    public int Live { get; private set; }
    public int Impostors { get; private set; }
    public int Waiting { get; private set; }
    public int Exclaiming { get; private set; }
    public int LooksBaked => rows.Count;

    /// <summary>The impostors' atlas as it stands, for the harness to look at (read back from the card).</summary>
    public Texture2D AtlasTexture => atlas;

    private void Ensure(OpenShaders shaders)
    {
        if (loaded) return;
        var blank = Raylib.GenImageColor(Cell * Views, Cell * MostLooks, new Color(0, 0, 0, 0));
        atlas = Gpu.TextureFromImage(blank, "openfigure");
        Raylib.UnloadImage(blank);
        Raylib.SetTextureFilter(atlas, TextureFilter.Bilinear);
        Raylib.SetTextureWrap(atlas, TextureWrap.Clamp);
        bakeTarget = Gpu.Target(Cell, Cell, "openfigure");
        var v = new OpenVertices(MostImpostors * 6);
        for (int i = 0; i < MostImpostors * 6; i++) v.Add(Vector3.Zero, Vector3.Zero, Vector2.Zero, Color.White);
        cards = v.Pack();
        Gpu.Upload(ref cards, "openfigure");
        cardMaterial = RenderContext.MaterialFor(shaders.Impostor, atlas);
        loaded = true;
    }

    /// <summary>
    /// The frame's figures: which are live and which impostors (<see cref="OpenFigures.Choose"/>), their walks moved on
    /// by how far each came since the last frame, the looks not made yet asked for, and one impostor's look baked where
    /// one is wanted (outside any texture mode: the bake has a target of its own).
    /// </summary>
    public void Prepare(RenderContext context, OpenShaders shaders, IReadOnlyList<OpenFigure> list, Vector3 eye, double time)
    {
        Ensure(shaders);
        figures = list;
        float dt = double.IsNaN(lastTime) ? 0f : (float)Math.Clamp(time - lastTime, 0, 0.25);
        lastTime = time;
        OpenFigures.Choose(list, eye, live, far);
        Exclaiming = 0;
        var seen = new HashSet<string>();
        foreach (var f in list)
        {
            seen.Add(f.Key);
            if (!motion.TryGetValue(f.Key, out var m)) m = (f.Feet, 0f, 0f, f.Exclaiming ? time : double.NaN);
            float moved = new Vector2(f.Feet.X - m.Last.X, f.Feet.Z - m.Last.Z).Length();
            if (moved > 2f) moved = 0f;
            float speed = dt > 0 ? moved / dt : m.Speed;
            float stride = f.Kind == OpenFigureKind.Person ? 1.6f : MathF.Max(0.6f, f.Height * 1.4f);
            double exclaimed = f.Exclaiming ? (double.IsNaN(m.Exclaimed) ? time : m.Exclaimed) : double.NaN;
            motion[f.Key] = (f.Feet, (m.Walk + moved / stride) % 1f, m.Speed + (speed - m.Speed) * Math.Min(1f, dt * 10f), exclaimed);
            // A person's rig of the grown kit walks by its own motion, handed the velocity over the ground in metres a
            // second (an open figure's model unit is the rig's own, drawn unscaled)
            if (f.Kind == OpenFigureKind.Person && CharacterModels.TryGet(f.Look, context.Shaders) is { Style.Kit: Data.CharacterKit.Grown } rig)
            {
                if (!grownMotion.TryGetValue(f.Key, out var gm)) grownMotion[f.Key] = gm = GrownMotion.For(rig, f.Yaw);
                gm.Step(dt, Graphics.FieldRigChoice.VelocityOf(m.Last, f.Feet, dt, 1f), f.Yaw);
            }
            if (f.Exclaiming) Exclaiming++;
        }
        foreach (var key in new List<string>(motion.Keys)) if (!seen.Contains(key)) motion.Remove(key);
        foreach (var key in new List<string>(grownMotion.Keys)) if (!seen.Contains(key)) grownMotion.Remove(key);

        // The looks asked for ahead, and one impostor's look baked a frame
        Waiting = 0;
        Pending = 0;
        bool bakedOne = false;
        foreach (int i in live) if (!Ready(context, list[i])) Waiting++;
        foreach (int i in far)
        {
            var f = list[i];
            if (rows.ContainsKey(f.Look)) { rows[f.Look] = rows[f.Look] with { Used = time }; continue; }
            if (!Ready(context, f)) { Waiting++; continue; }
            if (bakedOne) { Pending++; continue; }
            Bake(context, f, time);
            bakedOne = true;
        }
    }

    /// <summary>How many far figures' looks were ready and still wait for their impostor's bake (one a frame).</summary>
    public int Pending { get; private set; }

    /// <summary>Whether a figure's rig or model is on the card (asking for it if not, never waiting).</summary>
    private static bool Ready(RenderContext context, OpenFigure f)
    {
        if (f.Kind == OpenFigureKind.Person) return CharacterModels.TryGet(f.Look, context.Shaders) != null;
        if (PokemonModels.TryGet(f.Look, out _)) return true;
        PokemonModels.Request(f.Look);
        return false;
    }

    /// <summary>The figure's pose and where it is drawn, by how it has been moving.</summary>
    private (CharacterPose Person, PokePose Pokemon) PoseOf(OpenFigure f, double time)
    {
        (Vector3 Last, float Walk, float Speed, double Exclaimed) m = motion.TryGetValue(f.Key, out var mm) ? mm : (f.Feet, 0f, 0f, double.NaN);
        bool moving = m.Speed > 0.25f;
        // Each figure breathes on its own time, by its key
        float offset = (Core.SplitMix.Fnv(f.Key) & 1023) / 1023f * 4f;
        var person = new CharacterPose { Time = (float)(time % 3600.0) + offset, Walk = m.Walk, WalkBlend = moving ? 1f : 0f, Running = m.Speed > 3f };
        // (a grown rig's gait from its own motion: plan 30 · FM6)
        if (f.Kind == OpenFigureKind.Person && grownMotion.TryGetValue(f.Key, out var gm)) gm.Fill(ref person);
        var pokemon = new PokePose { Time = (float)(time % 3600.0) + offset, Walk = m.Walk, Moving = moving ? 1f : 0f };
        return (person, pokemon);
    }

    private static Matrix4x4 RootOf(OpenFigure f) => Matrix4x4.CreateRotationY(f.Yaw) * Matrix4x4.CreateTranslation(f.Feet);

    /// <summary>The scale a Pokémon's model is drawn at for its height in metres.</summary>
    private static float PokemonScale(PokeModel model, OpenFigure f) => OpenFigures.PokemonHeight(f.Height) / MathF.Max(0.05f, model.Height);

    /// <summary>The live figures in a pass: the depth (a shadow map, the nearest within a reach of a sphere), the colour or the outline.</summary>
    public int DrawLive(RenderContext context, CharacterPass pass, double time, Vector3? centre = null, float radius = 0f)
    {
        int drawn = 0;
        foreach (int i in live)
        {
            var f = figures[i];
            if (centre is { } c && Vector3.Distance(f.Feet, c) > radius + 3f) continue;
            if (pass == CharacterPass.Outline && !WithinOutline(f)) continue;
            var (person, pokemon) = PoseOf(f, time);
            if (f.Kind == OpenFigureKind.Person)
            {
                if (CharacterModels.TryGet(f.Look, context.Shaders) is not { } rig) continue;
                // (turned the way its grown motion has it, which eases and step-turns: plan 30 · FM6)
                var root = grownMotion.TryGetValue(f.Key, out var gm) ? Matrix4x4.CreateRotationY(gm.Yaw) * Matrix4x4.CreateTranslation(f.Feet) : RootOf(f);
                CharacterRenderer.Draw(context, rig, person, root, pass);
            }
            else
            {
                if (!PokemonModels.TryGet(f.Look, out var model)) continue;
                var root = Matrix4x4.CreateScale(PokemonScale(model, f)) * RootOf(f);
                PokemonRenderer.Draw(context, model, pokemon, root, pass);
            }
            drawn++;
        }
        if (pass == CharacterPass.Color) Live = drawn;
        return drawn;
    }

    // The eye of the frame, for the outline's reach
    private Vector3 eye;

    /// <summary>Tells where the eye is this frame (the outlines' reach is measured from it).</summary>
    public void SetEye(Vector3 at) => eye = at;

    private bool WithinOutline(OpenFigure f) => Vector3.Distance(f.Feet, eye) <= OpenFigures.OutlineReach;

    /// <summary>The far figures as impostors (the impostor program's uniforms of the frame set; its reach nought).</summary>
    public void DrawImpostors(OpenShaders shaders, Vector3 eyeAt)
    {
        Impostors = 0;
        if (!loaded) return;
        int n = 0;
        var positions = new Vector3[MostImpostors * 6];
        var corners = new Vector2[MostImpostors * 6];
        var sizes = new Vector3[MostImpostors * 6];
        var colours = new Color[MostImpostors * 6];
        Vector2[] corner = { new(0, 0), new(1, 0), new(1, 1), new(0, 0), new(1, 1), new(0, 1) };
        foreach (int i in far)
        {
            if (n == MostImpostors) break;
            var f = figures[i];
            if (!rows.TryGetValue(f.Look, out var row)) continue;
            // The figure's way as the colour's alpha, a whole turn in 255ths (a tree's 255 is as good as nought)
            float turns = f.Yaw / MathF.Tau;
            byte way = (byte)((int)MathF.Round((turns - MathF.Floor(turns)) * 255f) % 255);
            for (int k = 0; k < 6; k++)
            {
                positions[n * 6 + k] = f.Feet;
                corners[n * 6 + k] = corner[k];
                sizes[n * 6 + k] = new Vector3(row.Frame, row.Frame, row.Row);
                colours[n * 6 + k] = new Color((byte)255, (byte)255, (byte)255, way);
            }
            n++;
        }
        if (n == 0) return;
        Raylib.UpdateMeshBuffer<Vector3>(cards, 0, positions, 0);
        Raylib.UpdateMeshBuffer<Vector2>(cards, 1, corners, 0);
        Raylib.UpdateMeshBuffer<Vector3>(cards, 2, sizes, 0);
        Raylib.UpdateMeshBuffer<Color>(cards, 3, colours, 0);
        shaders.SetImpostors(eyeAt, 0f, Views, MostLooks);
        var drawn = cards;
        drawn.VertexCount = n * 6;
        drawn.TriangleCount = n * 2;
        Raylib.DrawMesh(drawn, cardMaterial, Matrix4x4.Identity);
        FrameProfiler.Count(n * 2);
        Impostors = n;
    }

    /// <summary>
    /// The "!" over each trainer who has caught the player's eye (the grid's bubble: half size, four fifths, whole in
    /// three frames of a twelfth of a second), a card facing the camera 0.45 m over the head, dimmed as paper is after dark.
    /// </summary>
    public void DrawBubbles(Camera3D camera, float lit, double time)
    {
        var art = SceneTextures.Bubble(EmoteBubble.Exclaim);
        var paper = new Color((byte)Math.Clamp(255 * lit, 0, 255), (byte)Math.Clamp(255 * lit, 0, 255), (byte)Math.Clamp(255 * lit, 0, 255), (byte)255);
        var forward = Vector3.Normalize(camera.Target - camera.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        bool any = false;
        foreach (var f in figures)
        {
            if (!f.Exclaiming || !motion.TryGetValue(f.Key, out var m) || double.IsNaN(m.Exclaimed)) continue;
            float age = (float)(time - m.Exclaimed);
            float size = age < 0.04f ? 0.5f : age < 0.08f ? 0.8f : 1f;
            float w = LifeArt.BubbleWidth / 32f * OpenBuildingKit.MetresPerTile * size, h = LifeArt.BubbleHeight / 32f * OpenBuildingKit.MetresPerTile * size;
            float head = f.Kind == OpenFigureKind.Person ? 1.62f : OpenFigures.PokemonHeight(f.Height);
            var bottom = f.Feet + new Vector3(0, head + 0.15f, 0);
            if (!any)
            {
                Rlgl.DrawRenderBatchActive();
                Rlgl.DisableDepthMask();
                any = true;
            }
            Rlgl.SetTexture(art.Id);
            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Color4ub(paper.R, paper.G, paper.B, paper.A);
            var l = bottom - right * (w / 2f);
            var r = bottom + right * (w / 2f);
            Rlgl.TexCoord2f(0, 1); Rlgl.Vertex3f(l.X, l.Y, l.Z);
            Rlgl.TexCoord2f(1, 1); Rlgl.Vertex3f(r.X, r.Y, r.Z);
            Rlgl.TexCoord2f(1, 0); Rlgl.Vertex3f(r.X, r.Y + h, r.Z);
            Rlgl.TexCoord2f(0, 0); Rlgl.Vertex3f(l.X, l.Y + h, l.Z);
            Rlgl.End();
        }
        if (!any) return;
        Rlgl.DrawRenderBatchActive();
        Rlgl.SetTexture(Rlgl.GetTextureIdDefault());
        Rlgl.EnableDepthMask();
    }

    /// <summary>
    /// A look's eight views baked into its row of the atlas (the least lately used row when the atlas is full): the figure
    /// standing facing south, the camera round it an eighth of a turn at a time, lit from above and a little toward the
    /// camera, as the trees' impostors are baked.
    /// </summary>
    private void Bake(RenderContext context, OpenFigure f, double time)
    {
        var fs = context.Shaders;
        int row = rows.Count;
        if (row >= MostLooks)
        {
            string oldest = "";
            double when = double.MaxValue;
            foreach (var (look, r) in rows) if (r.Used < when) { when = r.Used; oldest = look; }
            // Every row shown this very frame: the look waits (drawn as nothing) rather than another's be taken from under it
            if (when >= time) return;
            row = rows[oldest].Row;
            rows.Remove(oldest);
        }
        CharacterRig? rig = f.Kind == OpenFigureKind.Person ? CharacterModels.TryGet(f.Look, fs) : null;
        PokeModel? model = null;
        if (f.Kind == OpenFigureKind.Pokemon) PokemonModels.TryGet(f.Look, out model);
        if (rig == null && model == null) return;
        float frame = f.Kind == OpenFigureKind.Person ? PersonFrame : OpenFigures.PokemonHeight(f.Height) * 1.25f;
        var still = new OpenFigure(f.Kind, f.Key, f.Look, Vector3.Zero, 0f, false, f.Height);
        var canvas = new PixelCanvas(Cell * Views, Cell);
        for (int k = 0; k < Views; k++)
        {
            float a = k * MathF.PI / 4f;
            var look = new Vector3(0, frame / 2f, 0);
            var camera = new Camera3D(look + new Vector3(MathF.Sin(a), 0.14f, MathF.Cos(a)) * 40f, look, Vector3.UnitY, frame, CameraProjection.Orthographic);
            var light = new SceneLighting(Vector3.Normalize(new Vector3(MathF.Sin(a) * 0.35f, 1f, MathF.Cos(a) * 0.35f)),
                new Vector3(0.45f, 0.45f, 0.42f), new Vector3(0.72f, 0.74f, 0.76f), new Vector3(0.6f, 0.6f, 0.58f));
            Raylib.BeginTextureMode(context.Shadows.Target);
            Raylib.ClearBackground(Color.White);
            Raylib.EndTextureMode();
            Raylib.BeginTextureMode(bakeTarget);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            Rlgl.SetClipPlanes(1.0, 100.0);
            Raylib.BeginMode3D(camera);
            fs.SetStudio();
            fs.SetLighting(Matrix4x4.Identity, light, camera.Position, 1f);
            fs.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.2f);
            fs.SetFog(Vector3.One, 0f, 1000f, 2000f);
            fs.SetFlash(default, 0f);
            context.BindShadowMap();
            if (rig != null)
            {
                CharacterRenderer.Draw(context, rig, new CharacterPose { Time = 0.4f }, RootOf(still), CharacterPass.Color);
                CharacterRenderer.Draw(context, rig, new CharacterPose { Time = 0.4f }, RootOf(still), CharacterPass.Outline);
            }
            else if (model != null)
            {
                var root = Matrix4x4.CreateScale(PokemonScale(model, still)) * RootOf(still);
                PokemonRenderer.Draw(context, model, new PokePose { Time = 0.4f }, root, CharacterPass.Color);
                PokemonRenderer.Draw(context, model, new PokePose { Time = 0.4f }, root, CharacterPass.Outline);
            }
            Raylib.EndMode3D();
            Raylib.EndTextureMode();
            context.UnbindShadowMap();
            Rlgl.SetClipPlanes(0.01, 1000.0);
            var image = Raylib.LoadImageFromTexture(bakeTarget.Texture);
            Raylib.ImageFlipVertical(ref image);
            var view = PixelCanvas.FromImage(image);
            Raylib.UnloadImage(image);
            canvas.Blit(view, k * Cell, 0);
        }
        var rowImage = canvas.ToImage();
        unsafe
        {
            Raylib.UpdateTextureRec(atlas, new Rectangle(0, row * Cell, Cell * Views, Cell), rowImage.Data);
        }
        Raylib.UnloadImage(rowImage);
        rows[f.Look] = (row, frame, time);
    }

    public unsafe void Unload()
    {
        if (!loaded) return;
        Gpu.Unload(atlas);
        Gpu.Unload(bakeTarget);
        Gpu.Unload(cards);
        Raylib.MemFree(cardMaterial.Maps);
        rows.Clear();
        motion.Clear();
        loaded = false;
        lastTime = double.NaN;
    }
}
