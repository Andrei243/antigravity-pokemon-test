using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A tool's own way of drawing the people of the field (plan 30 · FM0's prototype, the harness's <c>freemove</c> mode):
/// never set by the game, so with it null the field draws its baked sprites as it always has (or, where the region's
/// people are grown, its live rigs: <see cref="WorldRenderer.GrownField"/>).
/// </summary>
internal interface IFieldPeople
{
    /// <summary>
    /// Called once a frame, outside any texture mode, after the people of the frame are gathered and before any pass:
    /// the people the field would have drawn (<paramref name="people"/>), the camera it looks through and the stretch it
    /// gives upright things.
    /// </summary>
    void Prepare(RenderContext context, IReadOnlyList<(CharacterRig Rig, Vector3 Feet, float Yaw, CharacterPose Pose)> people, Camera3D camera, float verticalScale);

    /// <summary>Draws them in a pass (the shadow map's depth, the scene's colour), in place of their sprites.</summary>
    void Draw(RenderContext context, CharacterPass pass);
}

public sealed partial class WorldRenderer
{
    /// <summary>Who draws the people in place of their baked sprites (plan 30 · FM0): only a tool sets it; null in the game.</summary>
    internal IFieldPeople? People { get; set; }

    /// <summary>
    /// Whether the region's people are grown (plan 30 · FM3; style guide, "Live rigs in the field"): the engine sets it
    /// every frame from <c>GameEngine.KitHere</c>. Where it is false the field draws exactly what it always has.
    /// </summary>
    internal bool GrownField { get; set; }

    /// <summary>
    /// Whether a talk is under way (a box of text or a script has the field): the engine sets it every frame. In a grown
    /// region the player who faces north into someone on the next tile stands back (<see cref="TalkStepBack"/>).
    /// </summary>
    internal bool InTalk { get; set; }

    /// <summary>
    /// How far a grown player is drawn south of their tile while they talk to someone on the tile north of them, in tiles
    /// (plan 30, the gate's owed; style guide, "Scale in a grown region", "Talking"): a grown body 1.5 tiles tall a tile
    /// south of a person covered them to the chest from any of the field's cameras, since a tile of ground shows only
    /// sin(pitch) of a tile on the picture. Half a tile more keeps the face and shoulders of whoever speaks clear. Only the
    /// picture moves: the player's tile, and who they speak to, never do.
    /// </summary>
    public const float TalkStepBack = 0.5f;

    /// <summary>How long the step back takes, and the step forward again, in seconds.</summary>
    public const float TalkStepSeconds = 0.25f;

    // How far through the step back the player is (0 standing on their tile, 1 stood back) and when it was last moved on
    private float talkBack;

    /// <summary>How far through the step back the player is this frame (0 to 1), for a tool to print.</summary>
    internal float TalkBackShare => talkBack;
    private double talkBackAt = double.NaN;

    /// <summary>
    /// The player's step back this frame, in tiles south (nought in a chibi region, out of a talk, or with nobody on the
    /// tile they face north into): eased in and out on the camera's clock.
    /// </summary>
    private float TalkBackOf(Overworld.Map map, Overworld.Player player)
    {
        if (!GrownField) { talkBack = 0f; talkBackAt = double.NaN; return 0f; }
        bool wanted = InTalk && player.Facing == Data.Direction.Up && player.Mount == null
            && map.NPCs.At(player.GridX, player.GridY - 1) is { } ahead && !ahead.IsThing;
        // (on the camera's clock, which runs while a box of text is up: the field's own stands still in a talk)
        double now = cameraClock;
        // A clock gone back (a new place) or never read: whatever is wanted holds at once
        bool fresh = double.IsNaN(talkBackAt) || now < talkBackAt;
        float dt = fresh ? 0f : (float)Math.Min(now - talkBackAt, 0.25);
        talkBackAt = now;
        float step = dt / TalkStepSeconds;
        talkBack = fresh ? (wanted ? 1f : 0f) : wanted ? Math.Min(1f, talkBack + step) : Math.Max(0f, talkBack - step);
        float t = talkBack * talkBack * (3f - 2f * talkBack);
        return TalkStepBack * t;
    }

    /// <summary>A grown region's people drawn as live rigs and impostors (plan 30 · FM3): the harness reads its counts.</summary>
    internal FieldRigs LiveRigs => liveRigs;

    private readonly FieldRigs liveRigs = new();

    // Whether this frame draws the live rigs: a grown region's field, with no tool's people in their place
    private bool LivePeople => People == null && GrownField;

    private readonly List<(CharacterRig Rig, Vector3 Feet, float Yaw, CharacterPose Pose)> peopleShown = new();
    private readonly List<FieldFigure> figuresShown = new();

    private void PreparePeople(Camera3D camera, float pitchDeg)
    {
        if (People != null)
        {
            peopleShown.Clear();
            foreach (var actor in actors) peopleShown.Add((actor.Rig, actor.Feet, actor.Yaw, actor.Pose));
            People.Prepare(context, peopleShown, camera, VerticalScale);
            return;
        }
        if (!GrownField) return;
        figuresShown.Clear();
        foreach (var actor in actors) figuresShown.Add(new FieldFigure(actor.Rig, actor.Feet, actor.Yaw, actor.Pose, actor.Over, actor.Who, actor.Sprite, actor.Seating, actor.Drawn));
        liveRigs.Prepare(context, figuresShown, camera.Target, VerticalScale, pitchDeg, Life.Now);
    }

    /// <summary>
    /// The people of the frame in a pass: their sprites, a grown region's live rigs (timed as the profiler's
    /// <see cref="FrameSection.Characters"/>, the pass's own <paramref name="section"/> lapped before them), or what a tool
    /// draws in their place.
    /// </summary>
    private void DrawPeople(CharacterPass pass, FrameSection section)
    {
        if (People != null) People.Draw(context, pass);
        else if (GrownField)
        {
            FrameProfiler.Lap(section);
            liveRigs.Draw(pass);
            DrawMount(pass);
            FrameProfiler.Lap(FrameSection.Characters);
        }
        else DrawActors(pass);
    }
}
