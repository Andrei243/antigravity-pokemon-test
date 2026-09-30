using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Which pass a character is being drawn in.</summary>
internal enum CharacterPass { Depth, Color, Outline }

/// <summary>Draws a posed <see cref="CharacterRig"/> with the shared character materials.</summary>
internal static class CharacterRenderer
{
    private static readonly Matrix4x4[] PartMatrices = new Matrix4x4[6];

    /// <param name="root">Places the feet and turns the model (System.Numerics row-vector convention).</param>
    /// <param name="trueProportions">Skip the field's vertical stretch (shadow pass, and the level battle camera).</param>
    public static void Draw(RenderContext context, CharacterRig rig, CharacterPose pose, Matrix4x4 root, CharacterPass pass, bool trueProportions)
    {
        rig.PartTransforms(pose, root, PartMatrices, trueProportions);
        int i = 0;
        foreach (var part in rig.Parts)
        {
            // raylib expects column-vector matrices; System.Numerics builds row-vector ones
            var m = Matrix4x4.Transpose(PartMatrices[i++]);
            if (!part.Uploaded) continue;
            switch (pass)
            {
                case CharacterPass.Depth:
                    Raylib.DrawMesh(part.Body, context.Depth, m);
                    if (part.HasFace) Raylib.DrawMesh(part.Face, context.Depth, m);
                    break;
                case CharacterPass.Color:
                    Raylib.DrawMesh(part.Body, context.Toon, m);
                    if (part.HasFace) Raylib.DrawMesh(part.Face, pose.Blink ? rig.FaceBlink : rig.FaceOpen, m);
                    break;
                case CharacterPass.Outline:
                    Raylib.DrawMesh(part.Outline, context.Outline, m);
                    break;
            }
        }
    }
}
