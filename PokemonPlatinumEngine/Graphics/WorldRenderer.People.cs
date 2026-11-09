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
        foreach (var actor in actors) figuresShown.Add(new FieldFigure(actor.Rig, actor.Feet, actor.Yaw, actor.Pose, actor.Over, actor.Who, actor.Sprite, actor.Seating));
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
