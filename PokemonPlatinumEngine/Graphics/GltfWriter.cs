using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Writes a sculpted Pokémon model to a binary glTF file (plan 03 · D5): the mesh with its vertex colours and
/// materials' colours, the eyes and markings as a textured patch over it, the skeleton as a skin, and every clip of
/// its body plan baked as keyframes (idle, physical, special, status, hit, faint, entry). A generated or hand-built
/// model can then be refined in a 3D editor and put back in <c>overrides/models</c>, where the game reads it with
/// <see cref="ImportedModels"/>. No GPU calls.
/// </summary>
internal static class GltfWriter
{
    /// <summary>Keyframes per second of the baked clips.</summary>
    private const float Rate = 30f;

    private sealed class Bin
    {
        public readonly MemoryStream Data = new();
        public readonly JsonArray Views = new(), Accessors = new();

        private int View(byte[] bytes, int? target = null)
        {
            while (Data.Length % 4 != 0) Data.WriteByte(0);
            var view = new JsonObject { ["buffer"] = 0, ["byteOffset"] = Data.Length, ["byteLength"] = bytes.Length };
            if (target is { } t) view["target"] = t;
            Data.Write(bytes);
            Views.Add(view);
            return Views.Count - 1;
        }

        private int Accessor(int view, int componentType, int count, string type, bool normalized = false, float[]? min = null, float[]? max = null)
        {
            var a = new JsonObject { ["bufferView"] = view, ["componentType"] = componentType, ["count"] = count, ["type"] = type };
            if (normalized) a["normalized"] = true;
            if (min != null) a["min"] = Array(min);
            if (max != null) a["max"] = Array(max);
            Accessors.Add(a);
            return Accessors.Count - 1;
        }

        public int Floats(float[] values, string type, int width, int? target = null, bool bounds = false)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            float[]? min = null, max = null;
            if (bounds)
            {
                min = new float[width];
                max = new float[width];
                System.Array.Fill(min, float.MaxValue);
                System.Array.Fill(max, float.MinValue);
                for (int i = 0; i < values.Length; i++)
                {
                    min[i % width] = Math.Min(min[i % width], values[i]);
                    max[i % width] = Math.Max(max[i % width], values[i]);
                }
            }
            return Accessor(View(bytes, target), 5126, values.Length / width, type, min: min, max: max);
        }

        public int Bytes(byte[] values, string type, int width, bool normalized) => Accessor(View(values, 34962), 5121, values.Length / width, type, normalized);

        public int Indices(int[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return Accessor(View(bytes, 34963), 5125, values.Length, "SCALAR");
        }

        public int Image(byte[] png) => View(png);

        private static JsonArray Array(float[] v)
        {
            var a = new JsonArray();
            foreach (float f in v) a.Add(f);
            return a;
        }
    }

    public static void Save(PokeModel m, string path) => File.WriteAllBytes(path, Write(m));

    public static byte[] Write(PokeModel m)
    {
        if (m.Imported != null) throw new InvalidOperationException("only sculpted models can be written");
        var bin = new Bin();
        var mesh = m.Mesh;
        int n = mesh.VertexCount, bones = m.Skeleton.Count;

        // The body: positions, normals, colours, and four bones per vertex
        var primitives = new JsonArray { Primitive(bin, mesh, null, 0) };
        var materials = new JsonArray { new JsonObject { ["name"] = "body", ["pbrMetallicRoughness"] = new JsonObject { ["metallicFactor"] = 0f, ["roughnessFactor"] = 1f } } };
        var images = new JsonArray();
        var textures = new JsonArray();

        // The eyes and markings: their patch of triangles with the decal atlas, eyes open
        if (m.DecalPatch is { TriangleCount: > 0 } patch && m.DecalUVs != null)
        {
            images.Add(new JsonObject { ["bufferView"] = bin.Image(PngWriter.Encode(PokemonDecals.Paint(m, EyeState.Open))), ["mimeType"] = "image/png", ["name"] = "eyes" });
            textures.Add(new JsonObject { ["source"] = 0 });
            materials.Add(new JsonObject
            {
                ["name"] = "eyes", ["alphaMode"] = "MASK", ["alphaCutoff"] = 0.5f,
                ["pbrMetallicRoughness"] = new JsonObject { ["baseColorTexture"] = new JsonObject { ["index"] = 0 }, ["metallicFactor"] = 0f, ["roughnessFactor"] = 1f }
            });
            primitives.Add(Primitive(bin, patch, m.DecalUVs, 1));
        }

        // The skeleton: a node per bone, placed at its joint relative to its parent, bound where it was sculpted
        var nodes = new JsonArray { new JsonObject { ["name"] = m.Species, ["mesh"] = 0, ["skin"] = 0 } };
        var inverseBind = new float[bones * 16];
        var joints = new JsonArray();
        for (int b = 0; b < bones; b++)
        {
            var bone = m.Skeleton[b];
            var t = bone.Joint - (bone.Parent >= 0 ? m.Skeleton[bone.Parent].Joint : Vector3.Zero);
            var node = new JsonObject { ["name"] = bone.Name, ["translation"] = new JsonArray(t.X, t.Y, t.Z) };
            var children = new JsonArray();
            for (int c = 0; c < bones; c++)
                if (m.Skeleton[c].Parent == b) children.Add(1 + c);
            if (children.Count > 0) node["children"] = children;
            nodes.Add(node);
            joints.Add(1 + b);
            var ibm = Matrix4x4.CreateTranslation(-bone.Joint);
            Store(ibm, inverseBind, b * 16);
        }
        var sceneNodes = new JsonArray { 0 };
        for (int b = 0; b < bones; b++)
            if (m.Skeleton[b].Parent < 0) sceneNodes.Add(1 + b);

        var skins = new JsonArray { new JsonObject { ["joints"] = joints, ["inverseBindMatrices"] = bin.Floats(inverseBind, "MAT4", 16) } };

        var animations = new JsonArray();
        foreach (var (name, length, pose) in Clips())
            animations.Add(Clip(bin, m, name, length, pose));

        var buffer = bin.Data.ToArray();
        var gltf = new JsonObject
        {
            ["asset"] = new JsonObject { ["version"] = "2.0", ["generator"] = "Pokémon Platinum remake (GltfWriter)" },
            ["scene"] = 0,
            ["scenes"] = new JsonArray { new JsonObject { ["nodes"] = sceneNodes } },
            ["nodes"] = nodes,
            ["meshes"] = new JsonArray { new JsonObject { ["name"] = m.Species, ["primitives"] = primitives } },
            ["materials"] = materials,
            ["skins"] = skins,
            ["animations"] = animations,
            ["buffers"] = new JsonArray { new JsonObject { ["byteLength"] = buffer.Length } },
            ["bufferViews"] = bin.Views,
            ["accessors"] = bin.Accessors
        };
        if (images.Count > 0)
        {
            gltf["images"] = images;
            gltf["textures"] = textures;
        }
        return Glb(Encoding.UTF8.GetBytes(gltf.ToJsonString()), buffer);
    }

    private static JsonObject Primitive(Bin bin, SdfMesh mesh, Vector2[]? uvs, int material)
    {
        int n = mesh.VertexCount;
        var pos = new float[n * 3];
        var nor = new float[n * 3];
        var col = new byte[n * 4];
        for (int v = 0; v < n; v++)
        {
            pos[v * 3] = mesh.Positions[v].X;
            pos[v * 3 + 1] = mesh.Positions[v].Y;
            pos[v * 3 + 2] = mesh.Positions[v].Z;
            nor[v * 3] = mesh.Normals[v].X;
            nor[v * 3 + 1] = mesh.Normals[v].Y;
            nor[v * 3 + 2] = mesh.Normals[v].Z;
            var c = mesh.Colors[v];
            // glTF vertex colours are linear
            col[v * 4] = Linear(c.R);
            col[v * 4 + 1] = Linear(c.G);
            col[v * 4 + 2] = Linear(c.B);
            col[v * 4 + 3] = 255;
        }
        var attributes = new JsonObject
        {
            ["POSITION"] = bin.Floats(pos, "VEC3", 3, 34962, bounds: true),
            ["NORMAL"] = bin.Floats(nor, "VEC3", 3, 34962),
            ["COLOR_0"] = bin.Bytes(col, "VEC4", 4, normalized: true),
            ["JOINTS_0"] = bin.Bytes((byte[])mesh.BoneIndices.Clone(), "VEC4", 4, normalized: false),
            ["WEIGHTS_0"] = bin.Floats((float[])mesh.BoneWeights.Clone(), "VEC4", 4, 34962)
        };
        if (uvs != null)
        {
            var uv = new float[n * 2];
            for (int v = 0; v < n; v++)
            {
                uv[v * 2] = uvs[v].X;
                uv[v * 2 + 1] = uvs[v].Y;
            }
            attributes["TEXCOORD_0"] = bin.Floats(uv, "VEC2", 2, 34962);
        }
        return new JsonObject { ["attributes"] = attributes, ["indices"] = bin.Indices(mesh.Indices), ["material"] = material };
    }

    private static byte Linear(byte srgb) => (byte)MathF.Round(MathF.Pow(srgb / 255f, 2.2f) * 255f);

    /// <summary>The clips written: a few seconds of idle, then each action over its time in battle.</summary>
    private static IEnumerable<(string Name, float Length, Func<float, PokePose> Pose)> Clips()
    {
        yield return ("idle", 1.7f * 5f, t => new PokePose { Time = t });
        yield return ("physical", BattleAnimator.AttackTime, t => new PokePose { Time = 0.4f, Attack = Math.Clamp(t / BattleAnimator.AttackTime, 0.001f, 0.999f), Kind = MoveCategory.Physical });
        yield return ("special", BattleAnimator.AttackTime, t => new PokePose { Time = 0.4f, Attack = Math.Clamp(t / BattleAnimator.AttackTime, 0.001f, 0.999f), Kind = MoveCategory.Special });
        yield return ("status", BattleAnimator.AttackTime, t => new PokePose { Time = 0.4f, Attack = Math.Clamp(t / BattleAnimator.AttackTime, 0.001f, 0.999f), Kind = MoveCategory.Status });
        yield return ("hit", BattleAnimator.HitTime, t => new PokePose { Time = 0.4f, Hurt = Math.Clamp(t / BattleAnimator.HitTime, 0.001f, 0.999f) });
        yield return ("faint", BattleAnimator.FaintTime, t => new PokePose { Time = 0.4f, Faint = t / BattleAnimator.FaintTime });
        yield return ("entry", 0.8f, t => new PokePose { Time = 0.4f, Entry = Math.Clamp(t / 0.8f, 0.001f, 0.999f) });
    }

    /// <summary>One clip baked from the body plan's motion: a translation, rotation and scale key for every bone at every frame.</summary>
    private static JsonObject Clip(Bin bin, PokeModel m, string name, float length, Func<float, PokePose> poseAt)
    {
        int bones = m.Skeleton.Count;
        int frames = Math.Max(2, (int)MathF.Ceiling(length * Rate) + 1);
        var times = new float[frames];
        var t = new float[bones][];
        var r = new float[bones][];
        var s = new float[bones][];
        for (int b = 0; b < bones; b++)
        {
            t[b] = new float[frames * 3];
            r[b] = new float[frames * 4];
            s[b] = new float[frames * 3];
        }
        var pose = new SkeletonPose(bones);
        for (int f = 0; f < frames; f++)
        {
            times[f] = length * f / (frames - 1);
            PokemonAnimation.Apply(m, poseAt(times[f]), pose);
            for (int b = 0; b < bones; b++)
            {
                var bone = m.Skeleton[b];
                var at = bone.Joint - (bone.Parent >= 0 ? m.Skeleton[bone.Parent].Joint : Vector3.Zero) + pose.Offset[b];
                var q = pose.Rotation[b];
                t[b][f * 3] = at.X; t[b][f * 3 + 1] = at.Y; t[b][f * 3 + 2] = at.Z;
                r[b][f * 4] = q.X; r[b][f * 4 + 1] = q.Y; r[b][f * 4 + 2] = q.Z; r[b][f * 4 + 3] = q.W;
                s[b][f * 3] = pose.Scale[b].X; s[b][f * 3 + 1] = pose.Scale[b].Y; s[b][f * 3 + 2] = pose.Scale[b].Z;
            }
        }
        int input = bin.Floats(times, "SCALAR", 1, bounds: true);
        var samplers = new JsonArray();
        var channels = new JsonArray();
        for (int b = 0; b < bones; b++)
        {
            foreach (var (path, values, type, width) in new[] { ("translation", t[b], "VEC3", 3), ("rotation", r[b], "VEC4", 4), ("scale", s[b], "VEC3", 3) })
            {
                samplers.Add(new JsonObject { ["input"] = input, ["output"] = bin.Floats(values, type, width), ["interpolation"] = "LINEAR" });
                channels.Add(new JsonObject { ["sampler"] = samplers.Count - 1, ["target"] = new JsonObject { ["node"] = 1 + b, ["path"] = path } });
            }
        }
        return new JsonObject { ["name"] = name, ["samplers"] = samplers, ["channels"] = channels };
    }

    private static void Store(Matrix4x4 m, float[] into, int at)
    {
        // The row-vector matrix's rows in order are the column-major layout glTF wants
        float[] values = { m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44 };
        System.Array.Copy(values, 0, into, at, 16);
    }

    /// <summary>A .glb: the header, the JSON chunk padded with spaces, the binary chunk padded with zeros.</summary>
    private static byte[] Glb(byte[] json, byte[] bin)
    {
        int jsonLength = (json.Length + 3) & ~3, binLength = (bin.Length + 3) & ~3;
        using var s = new MemoryStream();
        using var w = new BinaryWriter(s);
        w.Write(0x46546C67u);
        w.Write(2u);
        w.Write((uint)(12 + 8 + jsonLength + 8 + binLength));
        w.Write((uint)jsonLength);
        w.Write(0x4E4F534Au);
        w.Write(json);
        for (int i = json.Length; i < jsonLength; i++) w.Write((byte)' ');
        w.Write((uint)binLength);
        w.Write(0x004E4942u);
        w.Write(bin);
        for (int i = bin.Length; i < binLength; i++) w.Write((byte)0);
        w.Flush();
        return s.ToArray();
    }
}
