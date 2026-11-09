using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Someone the grid's field draws this frame, as the renderer gathered them (plan 30 · FM3): the rig of their chibi look
/// (which their grown look is named from, and which stands in as a sprite where the grown one isn't ready), where their
/// feet are, the way they face, their pose, a bridge's deck over them, who they are (the person of the map or the player:
/// what keeps them live from one frame to the next) and whether they are drawn as their sprite whatever the kit
/// (<paramref name="Sprite"/>: a stance, a ride, a rig that is no person).
/// </summary>
internal readonly record struct FieldFigure(CharacterRig Rig, Vector3 Feet, float Yaw, CharacterPose Pose,
    (float X0, float X1, float Top)? Over, object? Who, bool Sprite);

/// <summary>
/// Which of a grown region's people are drawn live and which as impostors (plan 30 · FM3; style guide, "Live rigs in the
/// field"), and where a bubble stands over a grown head. GPU-free.
/// </summary>
internal static class FieldRigChoice
{
    /// <summary>How many people are drawn as live rigs at most: the plan's twelve.</summary>
    public const int MostLive = 12;

    /// <summary>
    /// How much nearer, in tiles, someone drawn live last frame counts: two people at about the same distance never trade
    /// places from frame to frame, and the swap comes as someone walks out to the crowd's edge.
    /// </summary>
    public const float Stickiness = 1.5f;

    /// <summary>How much higher a grown head stands than a chibi one, in tiles before the field's vertical stretch.</summary>
    public const float GrownHeadRise = 0.2f;

    /// <summary>
    /// The people drawn live (indices into the list, nearest first): the <see cref="MostLive"/> nearest the camera's
    /// target over the ground, someone live last frame (<paramref name="before"/>, by who they are) counted
    /// <see cref="Stickiness"/> nearer, ties going to the first in the list. The same places and the same last frame give
    /// the same choice.
    /// </summary>
    public static void Choose(IReadOnlyList<Vector3> feet, IReadOnlyList<object?> who, Vector3 focus, IReadOnlySet<object> before, List<int> live)
    {
        live.Clear();
        var near = new List<(float D, int I)>(feet.Count);
        for (int i = 0; i < feet.Count; i++)
        {
            float d = new Vector2(feet[i].X - focus.X, feet[i].Z - focus.Z).Length();
            if (who[i] is { } w && before.Contains(w)) d -= Stickiness;
            near.Add((d, i));
        }
        near.Sort((a, b) => a.D != b.D ? a.D.CompareTo(b.D) : a.I.CompareTo(b.I));
        for (int k = 0; k < near.Count && k < MostLive; k++) live.Add(near[k].I);
    }

    /// <summary>
    /// Where a bubble's foot stands over the ground under a person (the "!" and the rest): over a chibi head 1.95, as the
    /// field has always had it, and over a grown head that and the grown body's extra height stretched as upright things are.
    /// </summary>
    public static float BubbleLift(bool grown, float verticalScale) => grown ? 1.95f + GrownHeadRise * verticalScale : 1.95f;

    /// <summary>The nearest of eight facings to a way (radians, nought south), as an impostor is baked at.</summary>
    public static int Facing8(float yaw) => (((int)MathF.Round(yaw / (MathF.PI / 4f))) % 8 + 8) % 8;
}

/// <summary>
/// A grown region's people drawn as themselves (plan 30 · FM3, decision 1 (c); style guide, "Live rigs in the field"):
/// each person's look in the grown kit (<see cref="CharacterStyle.InKit"/>), the <see cref="FieldRigChoice.MostLive"/>
/// nearest live (skinned, outlined, with their painted face, in the field's light and shadow, straightened as the scenery
/// is), the rest as impostors of eight facings baked from the grown rig into pixel cards. A look not made yet, an impostor
/// not baked yet and anyone in a stance or on a ride are their chibi sprite. Never waits; every load through
/// <see cref="Gpu"/>, under <c>fieldrig</c>. Only the field's own renderer makes one, and draws with it only where the
/// region's kit is grown.
/// </summary>
internal sealed class FieldRigs
{
    /// <summary>An impostor's frame in texels, at the field's 32 to the tile: a grown body of 1.65 tiles with room round it.</summary>
    public const int ImpostorW = 48, ImpostorH = 64;

    /// <summary>How many looks' impostors are kept at most (the least lately used let go).</summary>
    public const int MostLooks = 48;

    private const float FrameW = ImpostorW / (float)CharacterSprites.TexelsPerUnit, FrameH = ImpostorH / (float)CharacterSprites.TexelsPerUnit;

    // How far under the frame's bottom row the feet stand, as a sprite's (CharacterSprites.DrawBillboard)
    private const float Margin = 0.05f;

    private static readonly SceneLighting StudioLight = new(Vector3.Normalize(new Vector3(-0.5f, 0.75f, 0.6f)),
        new Vector3(0.62f, 0.58f, 0.52f), new Vector3(0.6f, 0.62f, 0.7f), new Vector3(0.42f, 0.4f, 0.38f));

    private readonly Dictionary<string, string> grownNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (CharacterSprites.Card[] Views, double Used)> impostors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FieldFigure> figures = new();
    private readonly List<CharacterRig?> grown = new();
    private readonly List<int> live = new();
    private readonly HashSet<object> liveBefore = new(), liveNow = new();
    private readonly List<Vector3> feet = new();
    private readonly List<object?> who = new();
    private RenderTexture2D bakeTarget;
    private bool loaded;
    private float vs = 1f, pitchDeg;
    private RenderContext? context;

    // What the last frame drew, for the harness and the profile's printout
    public int Live { get; private set; }
    public int Impostors { get; private set; }
    public int Sprites { get; private set; }
    public int Waiting { get; private set; }
    public long Triangles { get; private set; }
    public long ShadowTriangles { get; private set; }
    public int LooksBaked => impostors.Count;

    /// <summary>Where the water's plane is while the mirror pass draws the people (plan 09 · L10), and null otherwise.</summary>
    public float? MirrorPlane { get; set; }

    /// <summary>The name of a look in the grown kit (kept, as working it out reads the table).</summary>
    public string GrownNameOf(string look)
    {
        if (!grownNames.TryGetValue(look, out var name))
            grownNames[look] = name = CharacterStyle.InKit(look, Data.CharacterKit.Grown);
        return name;
    }

    /// <summary>
    /// The frame's people: their grown rigs asked for (never waited for), who is live (<see cref="FieldRigChoice.Choose"/>
    /// round the camera's target), and one look's impostors baked where one is wanted. Outside any texture mode.
    /// </summary>
    public void Prepare(RenderContext context, IReadOnlyList<FieldFigure> people, Vector3 focus, float verticalScale, float pitch, double time)
    {
        this.context = context;
        vs = verticalScale;
        pitchDeg = pitch;
        figures.Clear();
        grown.Clear();
        feet.Clear();
        who.Clear();
        Waiting = 0;
        foreach (var f in people)
        {
            figures.Add(f);
            CharacterRig? rig = null;
            if (!f.Sprite)
            {
                rig = CharacterModels.TryGet(GrownNameOf(f.Rig.Type), context.Shaders);
                if (rig == null) Waiting++;
            }
            grown.Add(rig);
        }
        // Only those with a grown rig ready are chosen among; the rest are their sprites whatever their distance
        var index = new List<int>();
        for (int i = 0; i < figures.Count; i++)
        {
            if (grown[i] == null) continue;
            index.Add(i);
            feet.Add(figures[i].Feet);
            who.Add(figures[i].Who);
        }
        var chosen = new List<int>();
        FieldRigChoice.Choose(feet, who, focus, liveBefore, chosen);
        live.Clear();
        foreach (int k in chosen) live.Add(index[k]);
        liveNow.Clear();
        foreach (int i in live) if (figures[i].Who is { } w) liveNow.Add(w);
        liveBefore.Clear();
        liveBefore.UnionWith(liveNow);

        // One look's eight impostors baked a frame, for the first far person whose look has none
        bool bakedOne = false;
        foreach (int i in index)
        {
            if (live.Contains(i)) continue;
            string name = grown[i]!.Type;
            if (impostors.TryGetValue(name, out var have)) { impostors[name] = have with { Used = time }; continue; }
            if (bakedOne) continue;
            Bake(context, grown[i]!, time);
            bakedOne = true;
        }
    }

    /// <summary>The people in a pass, in place of their sprites: live, as impostors, or as their sprite.</summary>
    public void Draw(CharacterPass pass)
    {
        if (context == null) return;
        var shaders = context.Shaders;
        if (pass == CharacterPass.Depth) ShadowTriangles = 0;
        else { Triangles = 0; Live = 0; Impostors = 0; Sprites = 0; }

        // The live rigs, straightened as the scenery is (and about the water's plane in the mirror), put back after
        if (pass == CharacterPass.Color) shaders.SetSkinnedStraightening(ArtLook.FieldUpright, pitchDeg, MirrorPlane);
        foreach (int i in live)
        {
            var f = figures[i];
            var rig = grown[i]!;
            if (pass == CharacterPass.Depth)
            {
                // The sun sees the body as it stands, not stretched for the camera
                CharacterRenderer.Draw(context, rig, f.Pose, Matrix4x4.CreateRotationY(f.Yaw) * Matrix4x4.CreateTranslation(f.Feet), CharacterPass.Depth);
                ShadowTriangles += TrianglesOf(rig, outline: false);
                continue;
            }
            var root = Matrix4x4.CreateRotationY(f.Yaw) * Matrix4x4.CreateScale(1f, vs, 1f) * Matrix4x4.CreateTranslation(f.Feet);
            CharacterRenderer.Draw(context, rig, f.Pose, root, CharacterPass.Color);
            CharacterRenderer.Draw(context, rig, f.Pose, root, CharacterPass.Outline);
            // (the outline leaves culling on; the field draws with it off)
            Rlgl.DisableBackfaceCulling();
            Triangles += TrianglesOf(rig, outline: true);
            Live++;
        }
        if (pass == CharacterPass.Color)
        {
            Rlgl.DrawRenderBatchActive();
            shaders.ClearSkinnedStraightening();
        }

        for (int i = 0; i < figures.Count; i++)
        {
            if (live.Contains(i)) continue;
            var f = figures[i];
            if (f.Over != null && pass == CharacterPass.Color) shaders.SetDeckOver(f.Over);
            if (grown[i] is { } rig && impostors.TryGetValue(rig.Type, out var views))
            {
                // An impostor: the facing of eight nearest the way they face, its foot as a sprite's is
                var card = views.Views[FieldRigChoice.Facing8(f.Yaw)];
                CharacterSprites.DrawCard(card, new Vector3(f.Feet.X, f.Feet.Y - Margin * vs, f.Feet.Z + 0.02f), vs, pass);
                if (pass == CharacterPass.Color) { Impostors++; Triangles += 2; }
            }
            else
            {
                CharacterSprites.DrawBillboard(context, f.Rig, f.Pose, f.Yaw, f.Feet, vs, pass);
                if (pass == CharacterPass.Color) { Sprites++; Triangles += 2; }
            }
            if (f.Over != null && pass == CharacterPass.Color) shaders.SetDeckOver(null);
        }
    }

    /// <summary>
    /// The texture of a person's impostor facing the way they face, for a puddle to mirror (style guide, "Puddles"), with
    /// the frame's size in tiles; null where they are no grown figure with an impostor baked.
    /// </summary>
    public (Texture2D Texture, float Width, float Height)? ReflectionOf(int figure)
    {
        if (figure < 0 || figure >= figures.Count || grown[figure] is not { } rig || !impostors.TryGetValue(rig.Type, out var views)) return null;
        var card = views.Views[FieldRigChoice.Facing8(figures[figure].Yaw)];
        return (card.Texture, card.Width, card.Height);
    }

    private static long TrianglesOf(CharacterRig rig, bool outline)
    {
        long t = rig.Mesh.TriangleCount + (rig.Clothes?.TriangleCount ?? 0) + (rig.Held?.TriangleCount ?? 0);
        if (!outline) return t;
        return t + rig.Shell.TriangleCount + (rig.ClothesShell?.TriangleCount ?? 0) + (rig.HeldShell?.TriangleCount ?? 0) + (rig.FacePatch?.TriangleCount ?? 0);
    }

    /// <summary>
    /// A grown look's eight impostors: the rig standing still, turned to each eighth of a turn, seen from 24° above as the
    /// field's sprites are, its painted face drawn small, outlined in ink; into the least lately used place when full.
    /// </summary>
    private unsafe void Bake(RenderContext context, CharacterRig rig, double time)
    {
        if (!loaded)
        {
            bakeTarget = Gpu.Target(ImpostorW, ImpostorH, "fieldrig");
            loaded = true;
        }
        if (impostors.Count >= MostLooks)
        {
            string oldest = "";
            double when = double.MaxValue;
            foreach (var (name, have) in impostors) if (have.Used < when) { when = have.Used; oldest = name; }
            // Every look shown this very frame: this one waits as its sprite rather than take another's from under it
            if (when >= time) return;
            Free(impostors[oldest].Views);
            impostors.Remove(oldest);
        }
        var shaders = context.Shaders;
        float pitch = CharacterSprites.ViewPitchDeg * MathF.PI / 180f;
        var look = new Vector3(0, FrameH / 2f - Margin, 0);
        var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 20f, look, Vector3.UnitY, FrameH, CameraProjection.Orthographic);
        var views = new CharacterSprites.Card[8];
        for (int k = 0; k < 8; k++)
        {
            Raylib.BeginTextureMode(bakeTarget);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            Rlgl.SetClipPlanes(1.0, 60.0);
            Raylib.BeginMode3D(camera);
            shaders.SetLighting(Matrix4x4.Identity, StudioLight, camera.Position, 1f);
            shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.2f);
            shaders.SetStudio();
            CharacterRenderer.Draw(context, rig, new CharacterPose { Time = 0.4f }, Matrix4x4.CreateRotationY(k * MathF.PI / 4f), CharacterPass.Color);
            Raylib.EndMode3D();
            Raylib.EndTextureMode();
            Rlgl.SetClipPlanes(0.01, 1000.0);

            var image = Raylib.LoadImageFromTexture(bakeTarget.Texture);
            Raylib.ImageFlipVertical(ref image);
            var canvas = PixelCanvas.FromImage(image);
            Raylib.UnloadImage(image);
            canvas.OutlinePass(innerSeams: false);
            var texture = canvas.ToTexture(1, "fieldrig");
            Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
            views[k] = new CharacterSprites.Card(texture, RenderContext.MaterialFor(shaders.Sprite, texture), RenderContext.MaterialFor(shaders.Depth, texture), FrameW, FrameH);
        }
        impostors[rig.Type] = (views, time);
    }

    private static unsafe void Free(CharacterSprites.Card[] views)
    {
        foreach (var card in views)
        {
            Gpu.Unload(card.Texture);
            // Never UnloadMaterial: it would free the shared shaders the materials point at
            Raylib.MemFree(card.Color.Maps);
            Raylib.MemFree(card.Depth.Maps);
        }
    }

    /// <summary>Lets go of every impostor and the bake's target.</summary>
    public void Unload()
    {
        foreach (var (_, have) in impostors) Free(have.Views);
        impostors.Clear();
        if (loaded) Gpu.Unload(bakeTarget);
        loaded = false;
        liveBefore.Clear();
    }
}
