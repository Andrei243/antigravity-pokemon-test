using System.Buffers.Binary;
using System.Text;
using System.Text.Json.Nodes;

/// <summary>
/// Tripo's retarget answers one GLB per animation, each carrying the whole rigged model. The game wants one file whose
/// clips are all in it, so the animations of the others are copied into the first: their samplers' accessors and buffer
/// views are appended to its binary chunk and renumbered. The skeleton is the same in every file (the same rig task made
/// it), so a channel's node index means the same node; a file whose nodes do not match is left out and reported.
/// </summary>
static class GlbMerge
{
    const uint Magic = 0x46546C67, JsonChunk = 0x4E4F534A, BinChunk = 0x004E4942;

    sealed class Parsed
    {
        public JsonObject Json = new();
        public byte[] Bin = Array.Empty<byte>();
    }

    /// <summary>One file from the first, then the clips of the rest, each clip named as <paramref name="names"/> says.</summary>
    public static byte[] Merge(IReadOnlyList<byte[]> files, IReadOnlyList<string> names, List<string> notes)
    {
        var baseFile = Read(files[0]);
        var bin = new List<byte>(baseFile.Bin);
        var animations = baseFile.Json["animations"] as JsonArray ?? new JsonArray();
        baseFile.Json["animations"] = animations;

        // The first file's own clips take the first name (or numbered, if it carries several).
        for (int i = 0; i < animations.Count; i++)
            animations[i]!["name"] = animations.Count == 1 ? names[0] : $"{names[0]}_{i + 1}";
        if (animations.Count == 0) notes.Add($"{names[0]}: the first file holds no animation");

        for (int f = 1; f < files.Count; f++)
        {
            var other = Read(files[f]);
            if (!SameSkeleton(baseFile.Json, other.Json)) { notes.Add($"{names[f]}: its nodes are not the first file's, left out"); continue; }
            var clips = other.Json["animations"] as JsonArray;
            if (clips == null || clips.Count == 0) { notes.Add($"{names[f]}: no animation in its file"); continue; }

            var accessorMap = new Dictionary<int, int>();
            for (int i = 0; i < clips.Count; i++)
            {
                var clip = (JsonObject)clips[i]!.DeepClone();
                foreach (var sampler in (clip["samplers"] as JsonArray)!.OfType<JsonObject>())
                {
                    foreach (string key in new[] { "input", "output" })
                    {
                        int old = sampler[key]!.GetValue<int>();
                        sampler[key] = CopyAccessor(old, baseFile, other, bin, accessorMap);
                    }
                }
                clip["name"] = clips.Count == 1 ? names[f] : $"{names[f]}_{i + 1}";
                animations.Add(clip);
            }
        }

        baseFile.Bin = bin.ToArray();
        var buffers = baseFile.Json["buffers"] as JsonArray ?? new JsonArray();
        baseFile.Json["buffers"] = buffers;
        if (buffers.Count == 0) buffers.Add(new JsonObject());
        buffers[0]!["byteLength"] = baseFile.Bin.Length;
        return Write(baseFile);
    }

    public static List<string> AnimationNames(byte[] glb) =>
        (Read(glb).Json["animations"] as JsonArray ?? new JsonArray()).Select(a => a?["name"]?.GetValue<string>() ?? "").ToList();

    static bool SameSkeleton(JsonObject a, JsonObject b)
    {
        var na = a["nodes"] as JsonArray;
        var nb = b["nodes"] as JsonArray;
        if (na == null || nb == null || na.Count != nb.Count) return false;
        for (int i = 0; i < na.Count; i++)
            if (na[i]?["name"]?.GetValue<string>() != nb[i]?["name"]?.GetValue<string>()) return false;
        return true;
    }

    // An accessor of the other file, with the bytes of its buffer view, set at the end of the first file's binary chunk.
    static int CopyAccessor(int index, Parsed to, Parsed from, List<byte> bin, Dictionary<int, int> done)
    {
        if (done.TryGetValue(index, out int seen)) return seen;
        var accessor = (JsonObject)(from.Json["accessors"] as JsonArray)![index]!.DeepClone();
        if (accessor["bufferView"] is JsonNode viewNode)
        {
            var view = (JsonObject)(from.Json["bufferViews"] as JsonArray)![viewNode.GetValue<int>()]!.DeepClone();
            int offset = view["byteOffset"]?.GetValue<int>() ?? 0;
            int length = view["byteLength"]!.GetValue<int>();
            while (bin.Count % 4 != 0) bin.Add(0);
            int start = bin.Count;
            bin.AddRange(from.Bin.AsSpan(offset, length).ToArray());
            view["buffer"] = 0;
            view["byteOffset"] = start;
            var views = to.Json["bufferViews"] as JsonArray ?? new JsonArray();
            to.Json["bufferViews"] = views;
            views.Add(view);
            accessor["bufferView"] = views.Count - 1;
        }
        var accessors = to.Json["accessors"] as JsonArray ?? new JsonArray();
        to.Json["accessors"] = accessors;
        accessors.Add(accessor);
        done[index] = accessors.Count - 1;
        return accessors.Count - 1;
    }

    static Parsed Read(byte[] b)
    {
        var p = new Parsed();
        if (b.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(b) != Magic) throw new InvalidDataException("not a binary glTF");
        int pos = 12;
        while (pos + 8 <= b.Length)
        {
            int len = (int)BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(pos));
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(pos + 4));
            if (type == JsonChunk) p.Json = (JsonObject)JsonNode.Parse(b.AsMemory(pos + 8, len).Span)!;
            else if (type == BinChunk) p.Bin = b.AsSpan(pos + 8, len).ToArray();
            pos += 8 + ((len + 3) & ~3);
        }
        return p;
    }

    static byte[] Write(Parsed p)
    {
        byte[] json = Encoding.UTF8.GetBytes(p.Json.ToJsonString());
        int jsonLen = (json.Length + 3) & ~3;
        int binLen = (p.Bin.Length + 3) & ~3;
        int total = 12 + 8 + jsonLen + (binLen > 0 ? 8 + binLen : 0);
        var o = new byte[total];
        BinaryPrimitives.WriteUInt32LittleEndian(o, Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(o.AsSpan(4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(o.AsSpan(8), (uint)total);
        BinaryPrimitives.WriteUInt32LittleEndian(o.AsSpan(12), (uint)jsonLen);
        BinaryPrimitives.WriteUInt32LittleEndian(o.AsSpan(16), JsonChunk);
        json.CopyTo(o, 20);
        for (int i = 20 + json.Length; i < 20 + jsonLen; i++) o[i] = (byte)' ';
        if (binLen > 0)
        {
            int at = 20 + jsonLen;
            BinaryPrimitives.WriteUInt32LittleEndian(o.AsSpan(at), (uint)binLen);
            BinaryPrimitives.WriteUInt32LittleEndian(o.AsSpan(at + 4), BinChunk);
            p.Bin.CopyTo(o, at + 8);
        }
        return o;
    }

    /// <summary>A rigged GLB of one root node and one clip of two keys, for the self-test's fake server.</summary>
    public static byte[] Fake(string clip, float lift)
    {
        var bin = new byte[8 + 24];
        BitConverter.GetBytes(0f).CopyTo(bin, 0);
        BitConverter.GetBytes(1f).CopyTo(bin, 4);
        // translation keys: (0,0,0) and (0,lift,0)
        float[] t = { 0, 0, 0, 0, lift, 0 };
        for (int i = 0; i < 6; i++) BitConverter.GetBytes(t[i]).CopyTo(bin, 8 + i * 4);
        var json = new JsonObject
        {
            ["asset"] = new JsonObject { ["version"] = "2.0" },
            ["scene"] = 0,
            ["scenes"] = new JsonArray(new JsonObject { ["nodes"] = new JsonArray(0) }),
            ["nodes"] = new JsonArray(new JsonObject { ["name"] = "root" }),
            ["buffers"] = new JsonArray(new JsonObject { ["byteLength"] = bin.Length }),
            ["bufferViews"] = new JsonArray(
                new JsonObject { ["buffer"] = 0, ["byteOffset"] = 0, ["byteLength"] = 8 },
                new JsonObject { ["buffer"] = 0, ["byteOffset"] = 8, ["byteLength"] = 24 }),
            ["accessors"] = new JsonArray(
                new JsonObject { ["bufferView"] = 0, ["componentType"] = 5126, ["count"] = 2, ["type"] = "SCALAR", ["min"] = new JsonArray(0f), ["max"] = new JsonArray(1f) },
                new JsonObject { ["bufferView"] = 1, ["componentType"] = 5126, ["count"] = 2, ["type"] = "VEC3" }),
            ["animations"] = new JsonArray(new JsonObject
            {
                ["name"] = clip,
                ["samplers"] = new JsonArray(new JsonObject { ["input"] = 0, ["output"] = 1, ["interpolation"] = "LINEAR" }),
                ["channels"] = new JsonArray(new JsonObject { ["sampler"] = 0, ["target"] = new JsonObject { ["node"] = 0, ["path"] = "translation" } }),
            }),
        };
        return Write(new Parsed { Json = json, Bin = bin });
    }
}
