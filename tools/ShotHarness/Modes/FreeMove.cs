using System.Numerics;
using PokemonPlatinumEngine.Graphics;

/// <summary>How the prototype draws a person in the field (plan 30, decision 1).</summary>
enum PeopleLook
{
    /// <summary>(a) the rig baked into pixel sprites at eight facings, each kept once baked.</summary>
    Baked8,
    /// <summary>(b) the rig rendered every frame into its own target at 32 texels to the unit, at its true yaw, point-scaled and outlined.</summary>
    LivePixels,
    /// <summary>(c) the rig itself, smooth and outlined, at the screen's resolution in the field's light and shadow.</summary>
    Smooth
}

/// <summary>
/// The prototype's people (plan 30 · FM0; nothing of it reaches the game): whoever the field would draw, and the
/// harness's own figures, drawn in one of the three looks in place of the field's four-facing sprites. A figure may
/// be "grown": the chibi rig's own skin stretched (longer legs, a longer body and arms, a head shrunk to three fifths)
/// and scaled to a height in tiles, a stand-in for the grown kit FM4 is to build.
/// </summary>
sealed class FreeMovePeople : IFieldPeople
{
    public PeopleLook Look = PeopleLook.Baked8;

    /// <summary>A figure of the harness's own: a look, its feet, its yaw, its pose and, if grown, its height in tiles.</summary>
    public sealed record Figure(string Type, Vector3 Feet, float Yaw, CharacterPose Pose, float Grown = 0f);

    public readonly List<Figure> Extra = new();

    // The doorway beside the grown figures: a wall three tiles wide with a door a tile wide and 1.75 tall, as a card
    public (Vector3 Foot, CharacterSprites.Card? Card) Doorway;

    readonly List<(CharacterRig Rig, Vector3 Feet, float Yaw, CharacterPose Pose, float Grown)> drawn = new();
    float vs = 1f;

    // What the last frame drew, for the printout
    public int Rigs, Triangles, ShadowTriangles, Bones, Cards, SpriteRenders;

    // The sprite frame: wider and taller than the field's 40 by 58, so a grown figure of 1.6 tiles fits
    const int W = 48, H = 72;
    const float FrameW = W / 32f, FrameH = H / 32f;
    RenderTexture2D target;
    bool loaded;
    Mesh quad;

    sealed record Sprite(Texture2D Texture, Material Color, Material Depth);
    readonly Dictionary<(CharacterRig, int, float, SpriteAnim, int, bool), Sprite> baked = new();
    readonly List<Sprite> live = new();
    readonly List<Sprite?> perDraw = new();

    void EnsureLoaded()
    {
        if (loaded) return;
        target = Gpu.Target(W, H, "freemove");
        var b = new MeshBuilder();
        b.Quad(new(-0.5f, 0, 0), new(0.5f, 0, 0), new(0.5f, 1, 0), new(-0.5f, 1, 0), new(0, 1), new(1, 1), new(1, 0), new(0, 0),
            Color.White, Vector3.UnitZ);
        quad = b.Upload("freemove");
        loaded = true;
    }

    public void Prepare(RenderContext context, IReadOnlyList<(CharacterRig Rig, Vector3 Feet, float Yaw, CharacterPose Pose)> people, Camera3D camera, float verticalScale)
    {
        EnsureLoaded();
        vs = verticalScale;
        drawn.Clear();
        foreach (var p in people) drawn.Add((p.Rig, p.Feet, p.Yaw, p.Pose, 0f));
        foreach (var f in Extra) drawn.Add((CharacterModels.Get(f.Type, context.Shaders), f.Feet, f.Yaw, f.Pose, f.Grown));

        perDraw.Clear();
        SpriteRenders = 0;
        if (Look == PeopleLook.Smooth) return;
        int liveIndex = 0;
        foreach (var (rig, _, yaw, pose, grown) in drawn)
        {
            if (Look == PeopleLook.Baked8)
            {
                // One of eight facings, and the frame of the strip the pose is in, baked once
                int f8 = Facing8(yaw);
                var key = CharacterSprites.KeyOf(rig, pose, 0f);
                var at = (rig, f8, grown, key.Anim, key.Frame, key.Blink);
                if (!baked.TryGetValue(at, out var sprite))
                {
                    var canvas = Render(context, rig, CharacterSprites.BakePose(key), f8 * MathF.PI / 4f, grown);
                    baked[at] = sprite = Upload(context, canvas);
                }
                perDraw.Add(sprite);
            }
            else
            {
                // Rendered afresh at the true yaw and pose, into a texture of its own kept from frame to frame
                var canvas = Render(context, rig, pose, yaw, grown);
                if (liveIndex >= live.Count) live.Add(Upload(context, canvas));
                else unsafe
                {
                    var image = canvas.ToImage(1);
                    Raylib.UpdateTexture(live[liveIndex].Texture, image.Data);
                    Raylib.UnloadImage(image);
                }
                perDraw.Add(live[liveIndex++]);
            }
        }
    }

    static int Facing8(float yaw) => (((int)MathF.Round(yaw / (MathF.PI / 4f))) % 8 + 8) % 8;

    Sprite Upload(RenderContext context, PixelCanvas canvas)
    {
        var tex = canvas.ToTexture(1, "freemove");
        Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
        return new Sprite(tex, RenderContext.MaterialFor(context.Shaders.Sprite, tex), RenderContext.MaterialFor(context.Shaders.Depth, tex));
    }

    // The field's sprites are seen from 24° above, as the baked ones are
    const float SpritePitch = 24f;

    /// <summary>The rig at a yaw into a pixel frame, its pixel face stamped and outlined (CharacterSprites.Render at any yaw).</summary>
    PixelCanvas Render(RenderContext context, CharacterRig rig, CharacterPose pose, float yaw, float grown)
    {
        SpriteRenders++;
        var shaders = context.Shaders;
        float pitch = SpritePitch * MathF.PI / 180f;
        var look = new Vector3(0, FrameH / 2f - 0.05f, 0);
        var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 20f, look, Vector3.UnitY, FrameH, CameraProjection.Orthographic);
        var root = Matrix4x4.CreateRotationY(yaw);
        var light = new SceneLighting(Vector3.Normalize(new Vector3(-0.5f, 0.75f, 0.6f)),
            new Vector3(0.62f, 0.58f, 0.52f), new Vector3(0.6f, 0.62f, 0.7f), new Vector3(0.42f, 0.4f, 0.38f));

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(1.0, 60.0);
        Raylib.BeginMode3D(camera);
        shaders.SetLighting(Matrix4x4.Identity, light, camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.2f);
        shaders.SetStudio();
        var model = DrawRig(context, rig, pose, root, CharacterPass.Color, face: false, grown);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);

        var image = Raylib.LoadImageFromTexture(target.Texture);
        Raylib.ImageFlipVertical(ref image);
        var canvas = PixelCanvas.FromImage(image);
        Raylib.UnloadImage(image);

        if (rig.Kind == RigKind.Humanoid && model is { } m)
        {
            // The point between the eyes, as the rig was just posed (and stretched)
            var anchor = Vector3.Transform(Skeleton.Transform(rig.Face.Anchor, HumanBones.Head, rig.Skin), m);
            var d = anchor - look;
            var up = new Vector3(0, MathF.Cos(pitch), -MathF.Sin(pitch));
            var px = new Vector2(W / 2f + d.X * 32f, H / 2f - Vector3.Dot(d, up) * 32f);
            int f8 = Facing8(yaw);
            int facing = f8 switch { 7 or 0 or 1 => 0, 2 => 1, 6 => 3, _ => 2 };
            CharacterFaces.Stamp(canvas, px, facing, rig.Style, CharacterAnimation.ExpressionOf(pose), pose.Blink);
        }
        canvas.OutlinePass(innerSeams: false);
        return canvas;
    }

    // ------------------------------------------------------------------ the rig, grown or not

    static readonly Dictionary<CharacterRig, float> tops = new();

    /// <summary>How tall the rig's own sculpt is, in its units (before its scale).</summary>
    static float TopOf(CharacterRig rig)
    {
        if (tops.TryGetValue(rig, out var top)) return top;
        top = rig.Mesh.Positions.Length > 0 ? rig.Mesh.Positions.Max(p => p.Y) : rig.Skeleton[HumanBones.Head].Joint.Y + 0.55f;
        return tops[rig] = top;
    }

    // The stand-in's proportions: legs, the body and the arms lengthened, the head shrunk
    const float Legs = 1.75f, Body = 1.3f, Head = 0.6f, Arms = 1.45f, Slim = 0.82f;

    /// <summary>
    /// Stretches a posed chibi rig's skin into a grown body (a stand-in for FM4's kit) and gives the factor of the rig's
    /// own scale that makes it <paramref name="height"/> tiles tall: the legs lengthened from the ground, the body from
    /// the hips over them, each arm from its shoulder, the head shrunk about the neck and carried up with it.
    /// </summary>
    static float Grow(CharacterRig rig, float height)
    {
        var sk = rig.Skeleton;
        var skin = rig.Skin;
        float hips = sk[HumanBones.ThighL].Joint.Y;
        Vector3 Torso(Vector3 p) => new(p.X, Body * p.Y + hips * (Legs - Body), p.Z);
        var torso = Matrix4x4.CreateScale(1f, Body, 1f) * Matrix4x4.CreateTranslation(0, hips * (Legs - Body), 0);
        Matrix4x4 About(Vector3 at, Vector3 scale, Vector3 to) =>
            Matrix4x4.CreateTranslation(-at) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(to);

        var neck = Vector3.Transform(sk[HumanBones.Head].Joint, skin[HumanBones.Head]);
        var head = About(neck, new Vector3(Head), Torso(neck));
        var shoulderL = Vector3.Transform(sk[HumanBones.UpperArmL].Joint, skin[HumanBones.UpperArmL]);
        var shoulderR = Vector3.Transform(sk[HumanBones.UpperArmR].Joint, skin[HumanBones.UpperArmR]);
        var armL = About(shoulderL, new Vector3(1f, Arms, 1f), Torso(shoulderL));
        var armR = About(shoulderR, new Vector3(1f, Arms, 1f), Torso(shoulderR));
        var hipL = Vector3.Transform(sk[HumanBones.ThighL].Joint, skin[HumanBones.ThighL]) with { Y = 0f };
        var hipR = Vector3.Transform(sk[HumanBones.ThighR].Joint, skin[HumanBones.ThighR]) with { Y = 0f };
        var legL = About(hipL, new Vector3(Slim, Legs, Slim), hipL);
        var legR = About(hipR, new Vector3(Slim, Legs, Slim), hipR);

        for (int b = 0; b < skin.Length; b++)
        {
            var g = b switch
            {
                HumanBones.Head or HumanBones.Hair => head,
                HumanBones.UpperArmL or HumanBones.ForeArmL or HumanBones.HandL => armL,
                HumanBones.UpperArmR or HumanBones.ForeArmR or HumanBones.HandR => armR,
                HumanBones.ThighL or HumanBones.ShinL or HumanBones.FootL => legL,
                HumanBones.ThighR or HumanBones.ShinR or HumanBones.FootR => legR,
                _ => torso
            };
            skin[b] *= g;
        }
        // The top of the head, carried as the head is
        float restNeck = sk[HumanBones.Head].Joint.Y;
        float top = Torso(new Vector3(0, restNeck, 0)).Y + (TopOf(rig) - restNeck) * Head;
        return height / (top * rig.Scale);
    }

    /// <summary>Plain chibi height in tiles, for the printout.</summary>
    public static float HeightOf(CharacterRig rig) => TopOf(rig) * rig.Scale;

    /// <summary>Poses the rig (grown if asked) and draws one pass of it; gives the model matrix it drew with.</summary>
    Matrix4x4? DrawRig(RenderContext context, CharacterRig rig, CharacterPose pose, Matrix4x4 root, CharacterPass pass, bool face, float grown)
    {
        if (!rig.Uploaded || rig.Body == null) return null;
        rig.Animate(pose);
        float scale = rig.Scale * (grown > 0f ? Grow(rig, grown) : 1f);
        var model = Matrix4x4.CreateScale(scale) * root;
        switch (pass)
        {
            case CharacterPass.Depth:
                rig.Body.Draw(context.DepthSkinned, rig.Skin, model);
                rig.ClothesBody?.Draw(context.DepthSkinned, rig.Skin, model);
                ShadowTriangles += rig.Body.Mesh.TriangleCount + (rig.ClothesBody?.Mesh.TriangleCount ?? 0);
                break;
            case CharacterPass.Color:
                rig.Body.Draw(context.ToonSkinned, rig.Skin, model);
                rig.ClothesBody?.Draw(context.ToonSkinned, rig.Skin, model);
                if (face && rig.FaceModel != null && rig.FaceMaterials.Length > 0)
                    rig.FaceModel.Draw(rig.FaceMaterial(CharacterAnimation.ExpressionOf(pose), pose.Blink), rig.Skin, model);
                Triangles += rig.Body.Mesh.TriangleCount + (rig.ClothesBody?.Mesh.TriangleCount ?? 0) + (face ? rig.FaceModel?.Mesh.TriangleCount ?? 0 : 0);
                break;
            case CharacterPass.Outline:
                if (rig.Outline == null) break;
                context.Shaders.SetOutlineWidth(0f);
                Rlgl.DrawRenderBatchActive();
                Rlgl.EnableBackfaceCulling();
                Rlgl.SetCullFace(0);
                rig.Outline.Draw(context.OutlineSkinned, rig.Skin, model);
                rig.ClothesOutline?.Draw(context.OutlineSkinned, rig.Skin, model);
                Rlgl.SetCullFace(1);
                Triangles += rig.Outline.Mesh.TriangleCount + (rig.ClothesOutline?.Mesh.TriangleCount ?? 0);
                break;
        }
        return model;
    }

    public void Draw(RenderContext context, CharacterPass pass)
    {
        if (pass == CharacterPass.Depth) { ShadowTriangles = 0; }
        else { Triangles = 0; Rigs = 0; Bones = 0; Cards = 0; }

        if (Doorway.Card is { } door) CharacterSprites.DrawCard(door, Doorway.Foot, vs, pass);

        for (int i = 0; i < drawn.Count; i++)
        {
            var (rig, feet, yaw, pose, grown) = drawn[i];
            if (Look == PeopleLook.Smooth)
            {
                // Stood up as every upright thing of the field is (stretched by the pitch), at the feet, turned to its yaw
                var root = Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateScale(1f, vs, 1f) * Matrix4x4.CreateTranslation(feet);
                if (pass == CharacterPass.Depth)
                {
                    // The shadow is cast by the body as it stands, not stretched for the camera
                    DrawRig(context, rig, pose, Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(feet), pass, false, grown);
                    continue;
                }
                DrawRig(context, rig, pose, root, CharacterPass.Color, face: true, grown);
                DrawRig(context, rig, pose, root, CharacterPass.Outline, face: false, grown);
                Rlgl.DisableBackfaceCulling();
                Rigs++;
                Bones += rig.Skeleton.Count;
                continue;
            }
            if (perDraw.Count <= i || perDraw[i] is not { } sprite) continue;
            var at = new Vector3(WorldRenderer.SnapToTexel(feet.X), feet.Y, WorldRenderer.SnapToTexel(feet.Z));
            var m = Matrix4x4.CreateScale(FrameW, FrameH * vs, 1f) * Matrix4x4.CreateTranslation(at + new Vector3(0, -0.05f * vs, 0.02f));
            Raylib.DrawMesh(quad, pass == CharacterPass.Depth ? sprite.Depth : sprite.Color, Matrix4x4.Transpose(m));
            if (pass != CharacterPass.Depth) { Cards++; Triangles += 2; }
        }
    }

    /// <summary>The wall and its door: three tiles of wall 2.5 tiles tall, a door a tile wide and 1.75 tall in its middle.</summary>
    public static PixelCanvas PaintDoorway()
    {
        const int w = 96, h = 80, doorW = 32, doorH = 56;
        var c = new PixelCanvas(w, h);
        var wall = new Color(226, 214, 196, 255);
        var line = new Color(196, 182, 162, 255);
        var frame = new Color(92, 70, 56, 255);
        var wood = new Color(150, 98, 64, 255);
        var dark = new Color(120, 76, 50, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                c.SetRaw(x, y, y % 8 == 7 ? line : wall);
        int x0 = (w - doorW) / 2, y0 = h - doorH;
        for (int y = y0 - 2; y < h; y++)
            for (int x = x0 - 2; x < x0 + doorW + 2; x++)
                c.SetRaw(x, y, frame);
        for (int y = y0; y < h; y++)
            for (int x = x0; x < x0 + doorW; x++)
                c.SetRaw(x, y, (x - x0) % 8 == 0 ? dark : wood);
        for (int y = y0 + 26; y < y0 + 30; y++) c.SetRaw(x0 + doorW - 5, y, new Color(232, 196, 90, 255));
        // A ruler up the wall's left edge: a mark every tile and every quarter
        for (int y = h - 1, n = 0; y >= 0; y -= 8, n++)
            for (int x = 0; x < (n % 4 == 0 ? 8 : 3); x++) c.SetRaw(x, y, new Color(200, 60, 60, 255));
        return c;
    }
}

partial class Harness
{
    /// <summary>
    /// Plan 30 · FM0's prototype (not part of <c>all</c>): Aquacorde Town's promenade in Kalos with the same people
    /// drawn three ways on the same frame (<c>fm_00_look_*</c>, side by side on <c>fm_01_looks</c>, close up at the
    /// screen's own resolution on <c>fm_02_looks_close</c>): (a) baked sprites of eight facings, (b) the rigs rendered
    /// every frame into pixel frames at their true yaw, (c) the smooth rigs in the field's light and shadow. Two rows of
    /// the chibi cast turn through the eight facings (the second 20° past each, walking), and beside a wall with a door
    /// a tile wide and 1.75 tall stand a "grown" stand-in (the chibi rig stretched, FM4's kit not built) at 1.6 tiles and
    /// at today's 1.25, a chibi of the same look and the chibi player. Then Platinum's camera beside a Kalos one of 50°
    /// with a nearer lens (<c>fm_03_camera_*</c>, <c>fm_04_cameras</c>) and the heights (<c>fm_05_heights</c>). It prints
    /// each look's rigs, bones, triangles and sprite renders a frame: no timings, which mean nothing on a shared card.
    /// </summary>
    public void FreeMoveMode()
    {
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        const int px = 261, pz = 661;
        At("Kalos", px, pz, Direction.Down);
        Frames(20);

        var people = new FreeMovePeople();
        var map = game.Map;
        Vector3 Feet(float x, float z) => new(x + 0.5f, Relief.At(map, x + 0.5f, z + 0.5f), z + 0.5f);
        CharacterPose Idle(float seed) => new() { Time = 0.4f + seed };
        string[] cast = { "player", "dawn", "rival", "lass", "youngster", "nurse", "cynthia", "roark" };
        for (int i = 0; i < 8; i++)
        {
            people.Extra.Add(new(cast[i], Feet(px - 9 + i, pz - 1), i * MathF.PI / 4f, Idle(i * 0.37f)));
            var walking = new CharacterPose { Time = 0.4f + i * 0.21f, Walk = 0.12f + i * 0.11f, WalkBlend = 1f };
            people.Extra.Add(new(cast[(i + 3) % 8], Feet(px - 9 + i, pz + 2), i * MathF.PI / 4f + 20f * MathF.PI / 180f, walking));
        }
        // The grown stand-ins beside the doorway (east of the player), and a chibi of the same look
        people.Doorway = (Feet(px + 3, pz - 1) with { Z = pz - 1 + 0.1f }, CharacterSprites.MakeCard(game.RenderContext, FreeMovePeople.PaintDoorway()));
        people.Extra.Add(new("cynthia", Feet(px + 2, pz), 0f, Idle(0.1f), Grown: 1.6f));
        people.Extra.Add(new("rival", Feet(px + 3, pz), 0f, Idle(0.2f), Grown: 1.5f));
        people.Extra.Add(new("cynthia", Feet(px + 4, pz), 0f, Idle(0.3f), Grown: 1.25f));
        people.Extra.Add(new("cynthia", Feet(px + 1, pz), 0f, Idle(0.4f)));
        game.World.People = people;

        foreach (var look in new[] { "player", "cynthia", "rival", "dawn", "youngster", "lass" })
            Console.WriteLine($"  chibi {look}: {FreeMovePeople.HeightOf(CharacterModels.Get(look, game.RenderContext.Shaders)):0.00} tiles tall, scale {CharacterModels.Get(look, game.RenderContext.Shaders).Scale:0.00}");

        (PeopleLook Look, string Name, string Label)[] looks =
        {
            (PeopleLook.Baked8, "a_baked8", "(a) baked sprites, 8 facings"),
            (PeopleLook.LivePixels, "b_livepixels", "(b) live pixel layer"),
            (PeopleLook.Smooth, "c_smooth", "(c) smooth live rigs")
        };
        // The cast's part of the picture, and the doorway's close up
        var castCrop = new Rectangle(140, 330, 1180, 470);
        var doorCrop = new Rectangle(900, 330, 480, 360);
        var rowsCrop = new Rectangle(120, 330, 640, 430);
        var rows = new List<(string, string)>();
        var whole = new List<(string, string)>();
        var close = new List<(string, string)>();
        void ShootLooks(string prefix, string suffix)
        {
            foreach (var (look, name, label) in looks)
            {
                people.Look = look;
                Frames(3);
                string shot = $"{prefix}_look_{name}{suffix}";
                Shot(shot);
                ShotCrop(shot + "_close", (int)doorCrop.X, (int)doorCrop.Y, (int)doorCrop.Width, (int)doorCrop.Height, 2);
                whole.Add((shot, label + suffix.Replace("_", " ")));
                close.Add((shot + "_close", label + suffix.Replace("_", " ")));
                ShotCrop(shot + "_rows", (int)rowsCrop.X, (int)rowsCrop.Y, (int)rowsCrop.Width, (int)rowsCrop.Height, 2);
                rows.Add((shot + "_rows", label + suffix.Replace("_", " ")));
                Console.WriteLine($"  {shot}: {people.Rigs} live rigs ({people.Bones} bones posed), {people.Cards} cards, "
                    + $"{people.Triangles} triangles in the colour pass, {people.ShadowTriangles} in the shadow pass, {people.SpriteRenders} sprite renders this frame");
            }
        }

        ShootLooks("fm_00", "");
        Board("fm_01_looks", whole, castCrop, 1);
        Board("fm_02_looks_close", close, new Rectangle(0, 0, doorCrop.Width * 2, doorCrop.Height * 2), 1);
        Board("fm_06_facings_close", rows, new Rectangle(0, 0, rowsCrop.Width * 2, rowsCrop.Height * 2), 1);

        // A Kalos camera: pitched 50°, a wider lens a little nearer (the ground across about 0.94 of Platinum's)
        whole.Clear(); close.Clear();
        MapScene.OutdoorOverride = new FieldView(50f, 20f, 520f / 16f);
        game.World.ForgetAll();
        Frames(60);
        ShootLooks("fm_03", "_kalos50");
        var camWhole = new List<(string, string)> { ("fm_00_look_a_baked8", "59° (Platinum) (a)"), ("fm_00_look_c_smooth", "59° (Platinum) (c)") };
        camWhole.AddRange(whole.Where(w => !w.Item1.Contains("b_live")).Select(w => (w.Item1, "50° Kalos " + (w.Item1.Contains("a_baked") ? "(a)" : "(c)"))));
        Board("fm_04_cameras", camWhole, new Rectangle(0, 0, 1920, 1080), 2);
        Board("fm_05_heights", new List<(string, string)>
        {
            ("fm_00_look_a_baked8_close", "(a) 59°"), ("fm_00_look_c_smooth_close", "(c) 59°"),
            ("fm_03_look_a_baked8_kalos50_close", "(a) 50°"), ("fm_03_look_c_smooth_kalos50_close", "(c) 50°")
        }, new Rectangle(0, 0, doorCrop.Width * 2, doorCrop.Height * 2), 1);

        MapScene.OutdoorOverride = null;
        game.World.ForgetAll();
        game.World.People = null;
        Frames(30);
        game.DeleteSave();
    }

    // Shots laid side by side, a crop of each, a label over each: the boards the user compares
    void Board(string name, List<(string Shot, string Label)> shots, Rectangle crop, int columnsDown)
    {
        int per = (int)Math.Ceiling(shots.Count / (double)columnsDown);
        int cw = (int)crop.Width, ch = (int)crop.Height;
        var board = Raylib.GenImageColor(per * (cw + 10) + 10, columnsDown * (ch + 60) + 10, new Color(24, 26, 34, 255));
        for (int i = 0; i < shots.Count; i++)
        {
            string path = Path.Combine(outDir, shots[i].Shot + ".png");
            if (!File.Exists(path)) continue;
            var img = Raylib.LoadImage(path);
            int x = 10 + (i % per) * (cw + 10), y = 10 + (i / per) * (ch + 60);
            Raylib.ImageDraw(ref board, img, crop, new Rectangle(x, y + 50, cw, ch), Color.White);
            Raylib.ImageDrawText(ref board, shots[i].Label, x + 4, y + 8, 36, Color.White);
            Raylib.UnloadImage(img);
        }
        if (columnsDown == 2) Raylib.ImageResize(ref board, board.Width / 2, board.Height / 2);
        Save(board, name);
    }
}
