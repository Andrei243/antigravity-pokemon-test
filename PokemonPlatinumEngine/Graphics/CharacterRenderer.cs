using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Which pass a character is being drawn in.</summary>
internal enum CharacterPass { Depth, Color, Outline }

/// <summary>Draws a posed <see cref="CharacterRig"/>: the skinned body, its face, its outline and its shadow.</summary>
internal static class CharacterRenderer
{
    /// <param name="root">Places the feet and turns the model (System.Numerics row-vector convention).</param>
    /// <param name="drawFace">
    /// Lay the smooth face over the head (3D characters in battle); sprite bakes leave it off and stamp a pixel
    /// face instead.
    /// </param>
    public static void Draw(RenderContext context, CharacterRig rig, CharacterPose pose, Matrix4x4 root, CharacterPass pass, bool drawFace = true)
    {
        if (!rig.Uploaded || rig.Body == null) return;
        rig.Animate(pose);
        var model = Matrix4x4.CreateScale(rig.Scale) * root;
        switch (pass)
        {
            case CharacterPass.Depth:
                rig.Body.Draw(context.DepthSkinned, rig.Skin, model);
                break;
            case CharacterPass.Color:
                rig.Body.Draw(context.ToonSkinned, rig.Skin, model);
                if (drawFace && rig.FaceModel != null && rig.FaceMaterials.Length > 0)
                    rig.FaceModel.Draw(rig.FaceMaterial(CharacterAnimation.ExpressionOf(pose), pose.Blink), rig.Skin, model);
                break;
            case CharacterPass.Outline:
                // A shell round the body, showing only its inside: a rim round the silhouette
                if (rig.Outline == null) break;
                context.Shaders.SetOutlineWidth(0f);
                Rlgl.DrawRenderBatchActive();
                Rlgl.EnableBackfaceCulling();
                Rlgl.SetCullFace(0);
                rig.Outline.Draw(context.OutlineSkinned, rig.Skin, model);
                Rlgl.SetCullFace(1);
                break;
        }
    }
}
