using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Keeps meshed SDF models on disk (<c>cache/models</c> next to the executable, or the shared folder
/// <see cref="CacheFolders"/> names), named after the model and a hash of everything that shapes it, so a model is
/// only meshed again after it changes. A missing or unreadable file just means meshing again. A file is kept packed
/// (plan 03 · D13: every species and form has a sculpt, and their meshes as first written came to gigabytes): the
/// same numbers, a byte of each at a time and deflated, a third of the size and bit for bit what was meshed.
/// </summary>
internal static class SdfCache
{
    /// <summary>Bump when the mesher's output changes for the same model.</summary>
    private const int Version = 1;

    /// <summary>"SDFM": a mesh as it was first kept, every number written out as it is. Still read.</summary>
    private const uint Magic = 0x4D464453;

    /// <summary>"SDFZ": a packed mesh (<see cref="Write(Stream, SdfMesh)"/>).</summary>
    private const uint Packed = 0x5A464453;

    /// <summary>More than any model has by far: a header that claims more is a damaged file, not a mesh to make room for.</summary>
    private const int MostVertices = 1 << 23, MostIndices = 1 << 26;

    public static bool Enabled { get; set; } = true;

    /// <summary>The folder meshes are written to, and read from first.</summary>
    public static string Folder { get; set; } = CacheFolders.Models;

    /// <summary>
    /// Folders read after <see cref="Folder"/> and never written to (plan 16 · T2): the meshes a release ships beside its
    /// executable, which a build published for players reads while it writes its own beside the saves
    /// (<see cref="CacheFolders.ForShippedBuild"/>). Empty for every other build.
    /// </summary>
    public static IReadOnlyList<string> ShippedFolders { get; set; } = Array.Empty<string>();

    public static SdfMesh Get(SdfModel model, float cell, float iso = 0f)
    {
        string key = Key(model, cell, iso);
        string name = Safe(model.Name) + (iso != 0f ? "-shell" : "");
        string file = $"{name}-{key}.mesh";
        string path = Path.Combine(Folder, file);
        if (Enabled && File.Exists(path))
        {
            try
            {
                var kept = Read(path, out bool packed);
                // A mesh kept as it was first written is packed as it is read, unless another tree shares the folder
                // (one at an older commit couldn't read it, and would mesh the model again)
                if (!packed && CacheFolders.Prunes)
                    CacheFolders.Write(path, stream => Write(stream, kept), () => Array.Empty<string>(), prune: false);
                return kept;
            }
            catch (Exception e) when (e is IOException or EndOfStreamException or InvalidDataException) { }
        }
        // The cache the build was shipped with, read as it is
        if (Enabled)
            foreach (string folder in ShippedFolders)
            {
                string shipped = Path.Combine(folder, file);
                if (!File.Exists(shipped)) continue;
                try
                {
                    return Read(shipped, out _);
                }
                catch (Exception e) when (e is IOException or EndOfStreamException or InvalidDataException or UnauthorizedAccessException) { }
            }

        var mesh = SdfMesher.Mesh(model, cell, iso);
        // Older versions of the same model are of no more use, unless another tree shares the folder
        if (Enabled)
            CacheFolders.Write(path, stream => Write(stream, mesh),
                () => Array.FindAll(Directory.GetFiles(Folder, $"{name}-*.mesh"), old => iso != 0f || !old.Contains("-shell-")),
                CacheFolders.Prunes);
        return mesh;
    }

    public static string Key(SdfModel model, float cell, float iso = 0f)
    {
        var text = $"{Version}|{cell:R}|{iso:R}|{model.Describe()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    private static string Safe(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in name) sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
        return sb.ToString();
    }

    public static void Write(string path, SdfMesh m)
    {
        using var stream = File.Create(path);
        Write(stream, m);
    }

    /// <summary>
    /// Writes a mesh packed: the counts, then everything else deflated, each array turned into planes first (the
    /// first byte of every number, then the second…), which is what makes numbers that differ little from one
    /// vertex to the next deflate well, and each index as its step from the one before. Nothing is rounded.
    /// </summary>
    public static void Write(Stream stream, SdfMesh m)
    {
        // The planes are the numbers' own bytes, which are the file's (little-endian) only on a machine that keeps them so
        if (!BitConverter.IsLittleEndian)
        {
            WriteUnpacked(stream, m);
            return;
        }
        Span<byte> header = stackalloc byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(header, Packed);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..], Version);
        BinaryPrimitives.WriteInt32LittleEndian(header[8..], m.VertexCount);
        BinaryPrimitives.WriteInt32LittleEndian(header[12..], m.Indices.Length);
        stream.Write(header);

        using var z = new ZLibStream(stream, CompressionLevel.Optimal, leaveOpen: true);
        WritePlanes(z, MemoryMarshal.AsBytes(m.Positions.AsSpan()), 12);
        WritePlanes(z, MemoryMarshal.AsBytes(m.Normals.AsSpan()), 12);
        WritePlanes(z, MemoryMarshal.AsBytes(m.Colors.AsSpan()), 4);
        WritePlanes(z, m.Materials, 1);
        WritePlanes(z, m.BoneIndices, 4);
        WritePlanes(z, MemoryMarshal.AsBytes(m.BoneWeights.AsSpan()), 4);
        var steps = new int[m.Indices.Length];
        for (int i = 0, before = 0; i < steps.Length; before = m.Indices[i], i++) steps[i] = unchecked(m.Indices[i] - before);
        WritePlanes(z, MemoryMarshal.AsBytes(steps.AsSpan()), 4);
    }

    /// <summary>A mesh as it was first kept: every number written out as it is. What <see cref="Read(Stream, out bool)"/> still reads.</summary>
    internal static void WriteUnpacked(Stream stream, SdfMesh m)
    {
        using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(Version);
        w.Write(m.VertexCount);
        w.Write(m.Indices.Length);
        for (int i = 0; i < m.VertexCount; i++)
        {
            var p = m.Positions[i];
            w.Write(p.X); w.Write(p.Y); w.Write(p.Z);
            var n = m.Normals[i];
            w.Write(n.X); w.Write(n.Y); w.Write(n.Z);
            var c = m.Colors[i];
            w.Write(c.R); w.Write(c.G); w.Write(c.B); w.Write(c.A);
            w.Write(m.Materials[i]);
        }
        w.Write(m.BoneIndices);
        foreach (float f in m.BoneWeights) w.Write(f);
        foreach (int i in m.Indices) w.Write(i);
    }

    public static SdfMesh Read(string path) => Read(path, out _);

    /// <summary>Reads a mesh from the cache, and says whether it was kept packed.</summary>
    public static SdfMesh Read(string path, out bool packed)
    {
        using var stream = File.OpenRead(path);
        return Read(stream, out packed);
    }

    public static SdfMesh Read(Stream stream, out bool packed)
    {
        Span<byte> header = stackalloc byte[16];
        stream.ReadExactly(header);
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if ((magic != Magic && magic != Packed) || BinaryPrimitives.ReadInt32LittleEndian(header[4..]) != Version)
            throw new InvalidDataException("not a mesh of this version");
        int count = BinaryPrimitives.ReadInt32LittleEndian(header[8..]), indexCount = BinaryPrimitives.ReadInt32LittleEndian(header[12..]);
        if (count < 0 || indexCount < 0 || indexCount % 3 != 0 || count > MostVertices || indexCount > MostIndices) throw new InvalidDataException("bad counts");
        packed = magic == Packed;
        if (packed && !BitConverter.IsLittleEndian) throw new InvalidDataException("a packed mesh of another kind of machine");
        var m = new SdfMesh
        {
            Positions = new Vector3[count],
            Normals = new Vector3[count],
            Colors = new Color[count],
            Materials = new byte[count],
            BoneIndices = new byte[count * 4],
            BoneWeights = new float[count * 4],
            Indices = new int[indexCount]
        };
        if (packed)
        {
            using var z = new ZLibStream(stream, CompressionMode.Decompress, leaveOpen: true);
            ReadPlanes(z, MemoryMarshal.AsBytes(m.Positions.AsSpan()), 12);
            ReadPlanes(z, MemoryMarshal.AsBytes(m.Normals.AsSpan()), 12);
            ReadPlanes(z, MemoryMarshal.AsBytes(m.Colors.AsSpan()), 4);
            ReadPlanes(z, m.Materials, 1);
            ReadPlanes(z, m.BoneIndices, 4);
            ReadPlanes(z, MemoryMarshal.AsBytes(m.BoneWeights.AsSpan()), 4);
            ReadPlanes(z, MemoryMarshal.AsBytes(m.Indices.AsSpan()), 4);
            for (int i = 0, before = 0; i < indexCount; before = m.Indices[i], i++) m.Indices[i] = unchecked(m.Indices[i] + before);
        }
        else
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            for (int i = 0; i < count; i++)
            {
                m.Positions[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                m.Normals[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                m.Colors[i] = new Color(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte());
                m.Materials[i] = r.ReadByte();
            }
            m.BoneIndices = r.ReadBytes(count * 4);
            if (m.BoneIndices.Length != count * 4) throw new EndOfStreamException();
            for (int i = 0; i < m.BoneWeights.Length; i++) m.BoneWeights[i] = r.ReadSingle();
            for (int i = 0; i < indexCount; i++) m.Indices[i] = r.ReadInt32();
        }
        foreach (int index in m.Indices)
            if ((uint)index >= (uint)count) throw new InvalidDataException("index out of range");
        return m;
    }

    /// <summary>Writes numbers of <paramref name="width"/> bytes a plane at a time: every number's first byte, then every second…</summary>
    private static void WritePlanes(Stream stream, ReadOnlySpan<byte> numbers, int width)
    {
        if (width == 1)
        {
            stream.Write(numbers);
            return;
        }
        int count = numbers.Length / width;
        var planes = new byte[numbers.Length];
        for (int plane = 0, at = 0; plane < width; plane++)
            for (int i = 0; i < count; i++) planes[at++] = numbers[i * width + plane];
        stream.Write(planes);
    }

    private static void ReadPlanes(Stream stream, Span<byte> numbers, int width)
    {
        if (width == 1)
        {
            stream.ReadExactly(numbers);
            return;
        }
        int count = numbers.Length / width;
        var planes = new byte[numbers.Length];
        stream.ReadExactly(planes);
        for (int plane = 0, at = 0; plane < width; plane++)
            for (int i = 0; i < count; i++) numbers[i * width + plane] = planes[at++];
    }
}
