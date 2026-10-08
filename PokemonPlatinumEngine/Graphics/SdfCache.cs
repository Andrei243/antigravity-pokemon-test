using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Keeps meshed SDF models on disk (<c>cache/models</c> next to the executable, or the shared folder
/// <see cref="CacheFolders"/> names), named after the model and a hash of everything that shapes it, so a model is
/// only meshed again after it changes. A missing or unreadable file just means meshing again.
/// </summary>
internal static class SdfCache
{
    /// <summary>Bump when the mesher's output changes for the same model.</summary>
    private const int Version = 1;

    private const uint Magic = 0x4D464453; // "SDFM"

    public static bool Enabled { get; set; } = true;

    public static string Folder { get; set; } = CacheFolders.Models;

    public static SdfMesh Get(SdfModel model, float cell, float iso = 0f)
    {
        string key = Key(model, cell, iso);
        string name = Safe(model.Name) + (iso != 0f ? "-shell" : "");
        string path = Path.Combine(Folder, $"{name}-{key}.mesh");
        if (Enabled && File.Exists(path))
        {
            try { return Read(path); }
            catch (Exception e) when (e is IOException or EndOfStreamException or InvalidDataException) { }
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

    public static void Write(Stream stream, SdfMesh m)
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

    public static SdfMesh Read(string path)
    {
        using var r = new BinaryReader(File.OpenRead(path));
        if (r.ReadUInt32() != Magic || r.ReadInt32() != Version) throw new InvalidDataException("not a mesh of this version");
        int count = r.ReadInt32(), indexCount = r.ReadInt32();
        if (count < 0 || indexCount < 0 || indexCount % 3 != 0) throw new InvalidDataException("bad counts");
        var m = new SdfMesh
        {
            Positions = new Vector3[count],
            Normals = new Vector3[count],
            Colors = new Color[count],
            Materials = new byte[count],
            BoneWeights = new float[count * 4],
            Indices = new int[indexCount]
        };
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
        for (int i = 0; i < indexCount; i++)
        {
            m.Indices[i] = r.ReadInt32();
            if ((uint)m.Indices[i] >= (uint)count) throw new InvalidDataException("index out of range");
        }
        return m;
    }
}
