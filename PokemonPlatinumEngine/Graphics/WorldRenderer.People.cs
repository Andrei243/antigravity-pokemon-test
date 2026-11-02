using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A tool's own way of drawing the people of the field (plan 30 · FM0's prototype, the harness's <c>freemove</c> mode):
/// never set by the game, so with it null the field draws its baked sprites as it always has.
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

    private readonly List<(CharacterRig Rig, Vector3 Feet, float Yaw, CharacterPose Pose)> peopleShown = new();

    private void PreparePeople(Camera3D camera)
    {
        if (People == null) return;
        peopleShown.Clear();
        foreach (var actor in actors) peopleShown.Add((actor.Rig, actor.Feet, actor.Yaw, actor.Pose));
        People.Prepare(context, peopleShown, camera, VerticalScale);
    }

    /// <summary>The people of the frame in a pass: their sprites, or what a tool draws in their place.</summary>
    private void DrawPeople(CharacterPass pass)
    {
        if (People != null) People.Draw(context, pass);
        else DrawActors(pass);
    }
}
