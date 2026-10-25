using System.Buffers.Binary;
using System.Text.Json;

/// <summary>What a downloaded file is, read from the binary glTF's header and its JSON chunk alone.</summary>
sealed record GlbInfo(string? Error, List<string> Warnings, int Meshes, int Materials, int Images, int Skins, int Animations)
{
    const uint Magic = 0x46546C67;      // "glTF"
    const uint JsonChunk = 0x4E4F534A;  // "JSON"

    // What the game's reader (docs/model-files.md, "What can be read") does not take.
    static readonly string[] Unreadable = { "KHR_draco_mesh_compression", "EXT_meshopt_compression", "KHR_texture_basisu" };

    public static GlbInfo Read(byte[] b)
    {
        var warnings = new List<string>();
        if (b.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(b) != Magic)
            return new GlbInfo("not a binary glTF (no glTF header)", warnings, 0, 0, 0, 0, 0);
        uint version = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(4));
        if (version != 2) return new GlbInfo($"glTF version {version}, not 2", warnings, 0, 0, 0, 0, 0);
        uint total = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(8));
        if (total > b.Length) return new GlbInfo($"cut short: the header says {total} bytes, the file has {b.Length}", warnings, 0, 0, 0, 0, 0);
        uint len = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(12));
        if (BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(16)) != JsonChunk || 20 + (long)len > b.Length)
            return new GlbInfo("the first chunk is not the JSON one", warnings, 0, 0, 0, 0, 0);

        JsonElement root;
        try { root = JsonDocument.Parse(b.AsMemory(20, (int)len)).RootElement; }
        catch (JsonException e) { return new GlbInfo("the JSON chunk does not parse: " + e.Message, warnings, 0, 0, 0, 0, 0); }

        foreach (string list in new[] { "extensionsRequired", "extensionsUsed" })
            if (root.TryGetProperty(list, out var ext) && ext.ValueKind == JsonValueKind.Array)
                foreach (var e in ext.EnumerateArray())
                    if (Unreadable.Contains(e.GetString()))
                        warnings.Add($"uses {e.GetString()}, which the game cannot read: open it in Blender and export it again with compression off");

        if (root.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
            foreach (var im in images.EnumerateArray())
                if (im.TryGetProperty("mimeType", out var m) && m.GetString() is "image/webp" or "image/ktx2")
                    warnings.Add($"has a {m.GetString()} texture, which the game cannot read: export it again with PNG or JPEG images");

        return new GlbInfo(null, warnings, Count(root, "meshes"), Count(root, "materials"), Count(root, "images"), Count(root, "skins"), Count(root, "animations"));
    }

    static int Count(JsonElement root, string name) =>
        root.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array ? a.GetArrayLength() : 0;

    /// <summary>The smallest file that passes <see cref="Read"/>, for the self-test's fake server.</summary>
    public static byte[] Minimal()
    {
        byte[] json = System.Text.Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"},\"scenes\":[{}],\"scene\":0,\"meshes\":[{}]}");
        int padded = (json.Length + 3) & ~3;
        var file = new byte[20 + padded];
        BinaryPrimitives.WriteUInt32LittleEndian(file, Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)file.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)padded);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(16), JsonChunk);
        json.CopyTo(file, 20);
        for (int i = 20 + json.Length; i < file.Length; i++) file[i] = (byte)' ';
        return file;
    }
}
