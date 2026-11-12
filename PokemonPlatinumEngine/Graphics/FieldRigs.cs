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
/// (<paramref name="Sprite"/>: a stance the grown kit isn't placed in, a ride, a rig that is no person). Someone sitting
/// (plan 30 · FM6) carries <paramref name="Seating"/>: where they would stand, the way they face, the seat under them and
/// whether they stand at a height of their own, which a grown rig is seated from (<see cref="FieldRigChoice.GrownSeated"/>);
/// <paramref name="Feet"/> is then where their chibi sprite sits.
/// </summary>
internal readonly record struct FieldFigure(CharacterRig Rig, Vector3 Feet, float Yaw, CharacterPose Pose,
    (float X0, float X1, float Top)? Over, object? Who, bool Sprite, FieldSeating? Seating = null, Vector3 Drawn = default);

/// <summary>Where a sitter of the field would stand, the way they face, the seat under them and whether they stand at a height of their own (plan 30 · FM6).</summary>
internal readonly record struct FieldSeating(Vector3 Stood, Data.Direction Facing, Overworld.PropType? Seat, bool Raised);

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

    /// <summary>
    /// How far a grown sitter's feet stand from their tile's middle along the way they face, in tiles (plan 30 · FM6;
    /// style guide, "Scale in a grown region"): the grown sit puts the hip joints over the feet's root and the feet
    /// ahead, so the root stands a little past the seat's middle (a sofa's seat is deeper).
    /// </summary>
    public static float GrownForwardOn(Overworld.PropType? type) => type switch
    {
        Overworld.PropType.Sofa => 0.18f,
        Overworld.PropType.Cushion => 0f,
        _ => 0.08f
    };

    /// <summary>
    /// Where a grown sitter's feet stand (plan 30 · FM6): sitting, on the floor in front of the seat, lifted only where
    /// the seat stands higher (<see cref="Overworld.Seats.GrownHeightOf"/>) than the grown sit's own seat
    /// (<paramref name="seatHeight"/>, <see cref="GrownAnimation.SeatOf"/> times the rig's scale), so a grown region's
    /// chairs, benches and sofas take a grown body with its feet on the floor; on the ground, on the cushion's top. Whoever
    /// stands at a height of their own (<paramref name="raised"/>) is lifted no more.
    /// </summary>
    public static Vector3 GrownSeated(Vector3 stood, Overworld.Stance stance, Data.Direction facing, Overworld.PropType? seat, bool raised, float seatHeight)
    {
        var (dx, dz) = Overworld.FieldMovement.Delta(facing);
        float height = raised || seat == null ? 0f : Overworld.Seats.GrownHeightOf(seat.Value);
        float forward = GrownForwardOn(seat);
        float lift = stance == Overworld.Stance.Sit ? MathF.Max(0f, height - seatHeight) : height;
        return stood + new Vector3(dx * forward, lift, dz * forward);
    }

    /// <summary>
    /// Whether the grown kit draws someone in a stance live (plan 30 · FM6): standing, sitting on a seat or on the ground,
    /// and (the gate's owed) treading water, lying on a bed and fishing (a rod in their hands).
    /// </summary>
    public static bool LiveIn(Overworld.Stance stance) => stance is Overworld.Stance.Stand or Overworld.Stance.Sit or Overworld.Stance.SitGround
        or Overworld.Stance.Swim or Overworld.Stance.Lie or Overworld.Stance.Fish or Overworld.Stance.Tug;

    /// <summary>How far behind a lier's tile's middle their crown lies, in tiles: the pillow, where the chibi card stands.</summary>
    public const float PillowBehind = 0.3f;

    /// <summary>
    /// Where a grown lier's root (their feet) lies (plan 30, the gate's owed; style guide, "Live rigs in the field"): the
    /// lying clip lays the body on its back from the feet toward the way behind them, so the feet are put a body's length
    /// (<paramref name="crown"/>, in tiles) less the pillow's share ahead of the tile's middle along the way they face, and
    /// the crown comes to the pillow.
    /// </summary>
    public static Vector3 LieAlong(Vector3 lying, Data.Direction facing, float crown)
    {
        var (dx, dz) = Overworld.FieldMovement.Delta(facing);
        float ahead = crown - PillowBehind;
        return lying + new Vector3(dx * ahead, 0f, dz * ahead);
    }

    /// <summary>
    /// How fast someone moved over the ground between two frames, in a grown rig's model units a second (plan 30 · FM6,
    /// what <see cref="GrownMotion"/> is handed): the feet's way over the ground over the frame's time and the rig's
    /// scale, nought for a frame of no time and for a jump of more than <see cref="Teleport"/> tiles (a warp, a place
    /// put down), which starts the motion again.
    /// </summary>
    public static Vector2 VelocityOf(Vector3 from, Vector3 to, float dt, float scale)
    {
        var way = new Vector2(to.X - from.X, to.Z - from.Z);
        if (dt <= 0f || way.Length() > Teleport) return Vector2.Zero;
        return way / dt / MathF.Max(0.05f, scale);
    }

    /// <summary>A move over the ground longer than this in one frame, in tiles, is a jump, not a walk.</summary>
    public const float Teleport = 2f;

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
    // Each person's grown motion (plan 30 · FM6), the feet it last saw and the frame's pose, yaw and where they are drawn
    private readonly Dictionary<object, (GrownMotion Motion, Vector3 Feet, CharacterRig Rig)> motions = new();
    private readonly List<(CharacterPose Pose, float Yaw, Vector3 Feet)> posed = new();
    private readonly HashSet<object> moved = new();
    private double lastTime = double.NaN;
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

    /// <summary>
    /// A tool's switch (the harness's, as <see cref="FieldSprites.Patient"/>): a grown look not made yet is made on the
    /// spot, and every far look's impostors are baked in the frame that wants them, so a shot of a grown place is the
    /// same picture every run, never the chibi stand-ins of a look still being meshed. False in the game.
    /// </summary>
    public static bool Patient { get; set; }
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
        posed.Clear();
        feet.Clear();
        who.Clear();
        Waiting = 0;
        // The frame's time on the field's clock: nought on the first frame and while it stands still, and a clock gone
        // back (a new place) starts every motion again
        float dt = double.IsNaN(lastTime) || time < lastTime ? 0f : (float)Math.Min(time - lastTime, 0.25);
        if (!double.IsNaN(lastTime) && time < lastTime) motions.Clear();
        lastTime = time;
        moved.Clear();
        foreach (var f in people)
        {
            figures.Add(f);
            CharacterRig? rig = null;
            if (!f.Sprite)
            {
                rig = Patient ? CharacterModels.Get(GrownNameOf(f.Rig.Type), context.Shaders) : CharacterModels.TryGet(GrownNameOf(f.Rig.Type), context.Shaders);
                if (rig == null) Waiting++;
            }
            grown.Add(rig);
            posed.Add(rig == null ? (f.Pose, f.Yaw, f.Feet) : Posed(f, rig, dt));
        }
        foreach (var key in new List<object>(motions.Keys)) if (!moved.Contains(key)) motions.Remove(key);
        // Only those with a grown rig ready are chosen among; the rest are their sprites whatever their distance
        var index = new List<int>();
        for (int i = 0; i < figures.Count; i++)
        {
            if (grown[i] == null) continue;
            index.Add(i);
            feet.Add(posed[i].Feet);
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
            var stance = figures[i].Pose.Stance;
            if (!HasImpostor(stance)) continue;
            string name = ImpostorKey(grown[i]!.Type, stance);
            if (impostors.TryGetValue(name, out var have)) { impostors[name] = have with { Used = time }; continue; }
            if (bakedOne && !Patient) continue;
            Bake(context, grown[i]!, time, stance);
            bakedOne = true;
        }
    }

    /// <summary>
    /// Whether someone far off in a stance is an impostor (plan 30, the gate's owed: a crowd's sitters past the live twelve
    /// were chibi sprites among the grown): standing, sitting on a seat or on the ground. Anyone else is their sprite.
    /// </summary>
    public static bool HasImpostor(Overworld.Stance stance) => stance is Overworld.Stance.Stand or Overworld.Stance.Sit or Overworld.Stance.SitGround;

    /// <summary>The name a look's impostors are kept under in a stance: the look's own name standing, as it always was.</summary>
    public static string ImpostorKey(string look, Overworld.Stance stance) => stance == Overworld.Stance.Stand ? look : $"{look}#{stance}";

    /// <summary>
    /// A grown person's pose, way and feet this frame (plan 30 · FM6): their gait, yaw, lean, step-turn, start and stop
    /// from their own <see cref="GrownMotion"/>, handed how fast their feet moved over the ground (standing, the way the
    /// grid has them face is where they turn to); a sitter's feet where a grown body sits
    /// (<see cref="FieldRigChoice.GrownSeated"/>). Whoever is nobody (a tool's figure) keeps the pose they came with.
    /// </summary>
    private (CharacterPose Pose, float Yaw, Vector3 Feet) Posed(FieldFigure f, CharacterRig rig, float dt)
    {
        var at = f.Feet;
        // A grown swimmer treads water with the water at its shoulders: its feet sunk under the surface its sprite was set
        // at by that much of it, stretched as it is drawn (plan 30, the gate's owed)
        if (f.Pose.Stance == Overworld.Stance.Swim && f.Seating is { } water)
            at = water.Stood - new Vector3(0f, CharacterSprites.WaterLineHeight(rig) * vs, 0f);
        // (lying anywhere but a bed, as a script may pose someone, the body stays on its tile, as the chibi card does)
        else if (f.Seating is { Seat: Overworld.PropType.Bed } seating && f.Pose.Stance == Overworld.Stance.Lie)
        {
            // Lying along the bed on the grown mattress, the crown on the pillow (the gate's owed): the mattress stands
            // stretched as every upright thing is, so its top is its height by the vertical scale
            float mattress = seating.Raised || seating.Seat is not { } bed ? 0f : Overworld.Seats.GrownHeightOf(bed) * vs;
            float crown = CharacterModels.GrownFrameOf(rig.Style.Build, rig.Style.CurvedFigure()).Crown * rig.Scale;
            at = FieldRigChoice.LieAlong(seating.Stood + new Vector3(0f, mattress, 0f), seating.Facing, crown);
        }
        else if (f.Seating is { } seating2 && f.Pose.Stance != Overworld.Stance.Stand)
            at = FieldRigChoice.GrownSeated(seating2.Stood, f.Pose.Stance, seating2.Facing, seating2.Seat, seating2.Raised,
                GrownAnimation.SeatOf(rig.Skeleton) * rig.Scale);
        // A grown rider on a ride on land sits their saddle on the seat a chibi rider's feet are set at (the gate's owed)
        else if (f.Pose.Riding == Riding.Mount)
            at -= new Vector3(0f, GrownAnimation.SaddleOf(rig.Skeleton) * rig.Scale * vs, 0f);
        if (f.Who is not { } key) return (f.Pose, f.Yaw, at + f.Drawn);
        var ground = f.Seating?.Stood ?? f.Feet;
        if (!motions.TryGetValue(key, out var m) || !ReferenceEquals(m.Rig, rig)) m = (GrownMotion.For(rig, f.Yaw), ground, rig);
        var velocity = FieldRigChoice.VelocityOf(m.Feet, ground, dt, rig.Scale);
        // A jump (a warp, someone put down) starts the motion again where they are, facing their way
        if (dt > 0f && new Vector2(ground.X - m.Feet.X, ground.Z - m.Feet.Z).Length() > FieldRigChoice.Teleport) m = (GrownMotion.For(rig, f.Yaw), ground, rig);
        // A sitter turns to the way they sit at once and never walks
        if (f.Pose.Stance != Overworld.Stance.Stand) { m = (GrownMotion.For(rig, f.Yaw), ground, rig); velocity = Vector2.Zero; }
        m.Motion.Step(dt, velocity, f.Yaw);
        motions[key] = m with { Feet = ground };
        moved.Add(key);
        var pose = f.Pose;
        m.Motion.Fill(ref pose);
        // (drawn off the feet the motion follows: the player's step back in a talk, which no step of theirs is)
        return (pose, m.Motion.Yaw, at + f.Drawn);
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
            var (pose, yaw, at) = posed[i];
            var rig = grown[i]!;
            if (pass == CharacterPass.Depth)
            {
                // The sun sees the body as it stands, not stretched for the camera
                CharacterRenderer.Draw(context, rig, pose, Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(at), CharacterPass.Depth);
                ShadowTriangles += TrianglesOf(rig, outline: false);
                continue;
            }
            var root = Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateScale(1f, vs, 1f) * Matrix4x4.CreateTranslation(at);
            // Under a bridge's deck the boards hide them as they hide a sprite (plan 30, the gate's owed)
            var over = figures[i].Over;
            if (over != null) shaders.SetLiveDeckOver(over);
            CharacterRenderer.Draw(context, rig, pose, root, CharacterPass.Color);
            CharacterRenderer.Draw(context, rig, pose, root, CharacterPass.Outline);
            if (over != null) { Rlgl.DrawRenderBatchActive(); shaders.SetLiveDeckOver(null); }
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
            // (a sitter beyond the live twelve is a seated impostor where the grown body sits)
            if (grown[i] is { } rig && HasImpostor(f.Pose.Stance) && impostors.TryGetValue(ImpostorKey(rig.Type, f.Pose.Stance), out var views))
            {
                // An impostor: the facing of eight nearest the way they face, its foot as a sprite's is
                var (_, yaw, at) = posed[i];
                var card = views.Views[FieldRigChoice.Facing8(yaw)];
                CharacterSprites.DrawCard(card, new Vector3(at.X, at.Y - Margin * vs, at.Z + 0.02f), vs, pass);
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
    private unsafe void Bake(RenderContext context, CharacterRig rig, double time, Overworld.Stance stance = Overworld.Stance.Stand)
    {
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
        var views = new CharacterSprites.Card[8];
        for (int k = 0; k < 8; k++)
        {
            var texture = View(context, rig, k, stance);
            views[k] = new CharacterSprites.Card(texture, RenderContext.MaterialFor(shaders.Sprite, texture), RenderContext.MaterialFor(shaders.Depth, texture), FrameW, FrameH);
        }
        impostors[ImpostorKey(rig.Type, stance)] = (views, time);
    }

    /// <summary>
    /// One of a grown look's eight views (<paramref name="k"/> eighths of a turn from facing the eye), as an impostor is
    /// baked: standing still, seen from the sprites' 24°, its face drawn small, outlined in ink. Outside any texture mode.
    /// </summary>
    private Texture2D View(RenderContext context, CharacterRig rig, int k, Overworld.Stance stance = Overworld.Stance.Stand)
    {
        if (!loaded)
        {
            bakeTarget = Gpu.Target(ImpostorW, ImpostorH, "fieldrig");
            loaded = true;
        }
        var shaders = context.Shaders;
        float pitch = CharacterSprites.ViewPitchDeg * MathF.PI / 180f;
        var look = new Vector3(0, FrameH / 2f - Margin, 0);
        var camera = new Camera3D(look + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 20f, look, Vector3.UnitY, FrameH, CameraProjection.Orthographic);
        Raylib.BeginTextureMode(bakeTarget);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(1.0, 60.0);
        Raylib.BeginMode3D(camera);
        shaders.SetLighting(Matrix4x4.Identity, StudioLight, camera.Position, 1f);
        shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0.2f);
        shaders.SetStudio();
        CharacterRenderer.Draw(context, rig, new CharacterPose { Time = 0.4f, Stance = stance }, Matrix4x4.CreateRotationY(k * MathF.PI / 4f), CharacterPass.Color);
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
        return texture;
    }

    private readonly Dictionary<string, Texture2D> portraits = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A grown look's portrait for the menus (plan 30, the gate's owed: the Trainer Card's plate, the Pokédex's size page),
    /// in place of the chibi field sprite: its impostor facing the eye, the 48 by 64 texels the field shows it as far off.
    /// Kept for the look; made outside any texture mode.
    /// </summary>
    public Texture2D Portrait(RenderContext context, CharacterRig rig)
    {
        if (!portraits.TryGetValue(rig.Type, out var texture)) portraits[rig.Type] = texture = View(context, rig, 0);
        return texture;
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
        foreach (var (_, texture) in portraits) Gpu.Unload(texture);
        portraits.Clear();
        if (loaded) Gpu.Unload(bakeTarget);
        loaded = false;
        liveBefore.Clear();
        motions.Clear();
        lastTime = double.NaN;
    }
}
