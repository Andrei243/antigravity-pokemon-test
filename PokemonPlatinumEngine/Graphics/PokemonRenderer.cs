using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Draws a posed <see cref="PokeModel"/> (plan 04 · G7): the skinned body with the character shader's materials,
/// its eyes and markings on top, and its outline shell. Used by battles, the title and the menu sprite bakes.
/// </summary>
internal static class PokemonRenderer
{
    /// <summary>Puts the model on the GPU the first time it is drawn.</summary>
    public static void EnsureUploaded(PokeModel m, FieldShaders shaders)
    {
        if (m.Uploaded) return;
        if (m.Imported != null)
        {
            ImportedModels.Upload(m, shaders);
            m.Uploaded = true;
            return;
        }
        int bones = m.Skeleton.Count;
        m.Body = SkinnedModel.Upload(m.Mesh, bones);
        m.Outline = SkinnedModel.Upload(m.Shell, bones);
        if (m.DecalPatch != null && m.DecalUVs != null && m.DecalPatch.TriangleCount > 0)
        {
            m.DecalModel = SkinnedModel.Upload(m.DecalPatch, bones, m.DecalUVs, _ => Color.White);
            m.DecalMaterials = PokemonDecals.Materials(m, shaders);
        }
        m.Uploaded = true;
    }

    /// <summary>Frees what <see cref="EnsureUploaded"/> put on the GPU (the shaders stay: they are shared).</summary>
    public static unsafe void Unload(PokeModel m)
    {
        if (!m.Uploaded) return;
        if (m.Imported != null)
        {
            ImportedModels.Unload(m);
            m.Uploaded = false;
            return;
        }
        m.Body?.Unload();
        m.Outline?.Unload();
        m.DecalModel?.Unload();
        foreach (var material in m.DecalMaterials)
        {
            Raylib.UnloadTexture(material.Maps[(int)MaterialMapIndex.Albedo].Texture);
            Raylib.MemFree(material.Maps);
        }
        m.Body = m.Outline = m.DecalModel = null;
        m.DecalMaterials = System.Array.Empty<Material>();
        m.Uploaded = false;
    }

    /// <param name="root">Places the feet and turns the model (System.Numerics row-vector convention).</param>
    public static void Draw(RenderContext context, PokeModel m, PokePose pose, Matrix4x4 root, CharacterPass pass)
    {
        EnsureUploaded(m, context.Shaders);
        if (m.Imported != null)
        {
            m.Animate(pose);
            if (pass == CharacterPass.Outline)
            {
                context.Shaders.SetOutlineWidth(0f);
                Rlgl.DrawRenderBatchActive();
                Rlgl.EnableBackfaceCulling();
                Rlgl.SetCullFace(0);
                ImportedModels.Draw(context, m, root, pass);
                Rlgl.SetCullFace(1);
            }
            else ImportedModels.Draw(context, m, root, pass);
            return;
        }
        if (m.Body == null) return;
        m.Animate(pose);
        switch (pass)
        {
            case CharacterPass.Depth:
                m.Body.Draw(context.DepthSkinned, m.Skin, root);
                break;
            case CharacterPass.Color:
                m.Body.Draw(context.ToonSkinned, m.Skin, root);
                if (m.DecalModel != null && m.DecalMaterials.Length > 0)
                    m.DecalModel.Draw(m.DecalMaterials[(int)PokemonAnimation.EyesOf(pose)], m.Skin, root);
                break;
            case CharacterPass.Outline:
                // A shell round the body, showing only its inside: a rim round the silhouette
                if (m.Outline == null) break;
                context.Shaders.SetOutlineWidth(0f);
                Rlgl.DrawRenderBatchActive();
                Rlgl.EnableBackfaceCulling();
                Rlgl.SetCullFace(0);
                m.Outline.Draw(context.OutlineSkinned, m.Skin, root);
                Rlgl.SetCullFace(1);
                break;
        }
    }
}
