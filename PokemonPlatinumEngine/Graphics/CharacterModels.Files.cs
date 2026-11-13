using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Looks assembled from a build's file (plan 47, route b; style guide, "Characters from files"): the engine's own
/// skeleton for the build, the pieces the look wears in its colours as the rig's three meshes with their outline
/// shells, and the place of the face for the field's pixel faces. The frames the files are made on are written out
/// for Blender's scripts by <see cref="FramesJson"/>.
/// </summary>
internal static partial class CharacterModels
{
    /// <summary>
    /// Makes <paramref name="rig"/> from its build's file where there is one holding every piece the look wears;
    /// false leaves the rig untouched for the kits to sculpt.
    /// </summary>
    /// <summary>The engine's skeleton for a look's build: what a file's pieces are skinned to.</summary>
    internal static Skeleton SkeletonOf(CharacterStyle s)
    {
        var skeleton = new Skeleton();
        if (s.Kit == Data.CharacterKit.Grown) GrownSkeleton(skeleton, GrownFrameOf(s.Build, s.CurvedFigure()));
        else ChibiSkeleton(skeleton, FrameOf(s.Build));
        return skeleton;
    }

    private static bool BuildFromFile(CharacterRig rig)
    {
        if (!CharacterFiles.Enabled) return false;
        var s = rig.Style;
        bool grown = s.Kit == Data.CharacterKit.Grown;
        var skeleton = SkeletonOf(s);
        GrownFrame? gf = grown ? GrownFrameOf(s.Build, s.CurvedFigure()) : null;
        Frame? cf = grown ? null : FrameOf(s.Build);
        var file = CharacterFiles.For(s.Build, grown && s.CurvedFigure(), skeleton);
        if (file == null) return false;
        if (CharacterFiles.Missing(file, s) is { Count: > 0 } missing)
        {
            Core.Diagnostics.Log.Info($"{rig.Type} is sculpted by the kit: {file.Path} lacks {string.Join(", ", missing)}.");
            return false;
        }

        float outline = grown ? GrownOutlineThickness : OutlineThickness;
        var (body, clothes, held) = CharacterFiles.Wanted(s);
        var hidden = CharacterFiles.HiddenBy(file, clothes);
        // What grows on the face is drawn with the body but has no rim of its own
        var rimmed = body.Where(n => !n.StartsWith("face.", StringComparison.OrdinalIgnoreCase)).ToList();
        SdfMesh? mesh, shell, wear, wearShell, holds, holdsShell;
        try
        {
            mesh = CharacterFiles.Assemble(file, body, s, hidden);
            shell = CharacterFiles.Assemble(file, rimmed, s, hidden, outline);
            wear = CharacterFiles.Assemble(file, clothes, s);
            wearShell = CharacterFiles.Assemble(file, clothes.Where(n => !n.StartsWith("face.", StringComparison.OrdinalIgnoreCase)), s, lift: outline);
            holds = CharacterFiles.Assemble(file, held, s);
            holdsShell = CharacterFiles.Assemble(file, held, s, lift: outline);
        }
        catch (System.IO.InvalidDataException e)
        {
            // A look whose pieces together are more than a mesh holds is sculpted as before; nothing of the rig was touched
            Core.Diagnostics.Log.Warn($"{rig.Type} is sculpted, not made from {file.Path}: {e.Message}");
            return false;
        }
        if (grown) GrownSkeleton(rig.Skeleton, gf!);
        else ChibiSkeleton(rig.Skeleton, cf!);
        rig.Scale = s.Height;
        rig.File = file;
        (rig.Mesh, rig.Shell) = (mesh ?? new SdfMesh(), shell ?? new SdfMesh());
        (rig.Clothes, rig.ClothesShell, rig.Held, rig.HeldShell) = (wear, wearShell, holds, holdsShell);

        // The face's place, for the field's pixel faces: the head's surface between the eyes
        var head = file.Pieces["body.head"].Mesh;
        if (grown)
        {
            float R = gf!.HeadR;
            var anchor = CharacterFiles.Hit(head, new Vector3(0, gf.HeadC.Y, gf.HeadC.Z + 1f), -Vector3.UnitZ) ?? gf.HeadC + new Vector3(0, 0, R);
            rig.Face = new FaceLayout(anchor, GrownFaces.EyeHalfSpacing * R, new Vector3(0, gf.HeadC.Y - 0.2f * R, anchor.Z), 0.62f * R);
        }
        else
        {
            float eyeY = cf!.HeadC.Y - cf.EyeDrop;
            var anchor = CharacterFiles.Hit(head, new Vector3(0, eyeY, cf.HeadC.Z + 1f), -Vector3.UnitZ) ?? cf.HeadC + new Vector3(0, -cf.EyeDrop, cf.HeadR);
            rig.Face = new FaceLayout(anchor, cf.EyeHalf, new Vector3(0, eyeY - 0.03f, anchor.Z), cf.HeadR * 0.82f);
        }
        rig.PrepareScratch();
        return true;
    }

    /// <summary>
    /// Every build's frame and skeleton as JSON, in the game's axes (x to the character's left, y up, z to the front):
    /// what <c>tools/BlenderAnim/blender/people_frames.json</c> holds, which Blender's scripts build the files on. A
    /// test keeps that file equal to this.
    /// </summary>
    internal static string FramesJson()
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        var entries = new List<string>();
        foreach (var build in new[] { BodyBuild.Kid, BodyBuild.Adult, BodyBuild.Little, BodyBuild.Elder })
        {
            var f = FrameOf(build);
            var sk = new Skeleton();
            ChibiSkeleton(sk, f);
            entries.Add(Entry(CharacterFiles.NameOf(build, false), "Chibi", f, sk));
        }
        foreach (var build in new[] { BodyBuild.Teen, BodyBuild.Grown, BodyBuild.GrownBroad, BodyBuild.Child, BodyBuild.GrownElder })
            foreach (bool curved in new[] { false, true })
            {
                if (curved && build == BodyBuild.Child) continue;
                var f = GrownFrameOf(build, curved);
                var sk = new Skeleton();
                GrownSkeleton(sk, f);
                entries.Add(Entry(CharacterFiles.NameOf(build, curved), "Grown", f, sk));
            }
        sb.Append(string.Join(",\n", entries));
        sb.Append("\n}\n");
        return sb.ToString();
    }

    private static string Entry(string name, string kit, object frame, Skeleton sk)
    {
        static string N(float v) => MathF.Round(v, 5).ToString("0.#####", CultureInfo.InvariantCulture);
        static string V(Vector3 v) => $"[{N(v.X)}, {N(v.Y)}, {N(v.Z)}]";
        var fields = new List<string>();
        foreach (var p in frame.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            object? value = p.GetValue(frame);
            string? text = value switch
            {
                float x => N(x),
                Vector3 v => V(v),
                bool b => b ? "true" : "false",
                Enum e => $"\"{e}\"",
                _ => null
            };
            if (text != null) fields.Add($"\"{p.Name}\": {text}");
        }
        var bones = sk.Bones.Select(b => $"[\"{b.Name}\", {(b.Parent < 0 ? "null" : $"\"{sk[b.Parent].Name}\"")}, {V(b.Joint)}]");
        return $"  \"{name}\": {{\n    \"kit\": \"{kit}\",\n    \"frame\": {{{string.Join(", ", fields)}}},\n    \"bones\": [{string.Join(", ", bones)}]\n  }}";
    }
}
