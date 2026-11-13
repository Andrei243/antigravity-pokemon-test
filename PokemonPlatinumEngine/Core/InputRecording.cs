using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>A row of <see cref="StateHash"/> a recording keeps: taken after the frame of that number, once a second of the movie's time.</summary>
public sealed record MovieCheckpoint(int Frame, double Seconds, uint[] Hash);

/// <summary>
/// A whole session of play, recorded (plan 16 · T19): a header (the format's version, the build's
/// <see cref="GameVersion"/>, the data's <see cref="GameDataFiles.Version"/>, the seed handed to <see cref="Dice.Seed"/>,
/// the options in force, the time zone's offset, the region a new game begins in, and the files the title read: the
/// slots' saves, their backups, the autosave and the options, <c>titleSave</c>), then every frame
/// (<see cref="InputFrame"/>) and a row of <see cref="StateHash"/> each second. Written gzipped. Kept from the title,
/// not as a ring of the last minutes, because playing a ring back would need the game's whole state at its start, which
/// a save doesn't hold. GPU-free.
/// </summary>
public sealed class InputRecording
{
    /// <summary>The file's shape; a file of a newer one is refused.</summary>
    public const int FormatVersion = 5; // 2: each frame keeps the mouse's too (plan 12 · Q6); 3: the packs in use and the first run (plan 16 · T14, T24); 4: the stick's tilt (plan 30 · FM7); 5: whether the regions were built together

    private const string Magic = "PPMOVIE";

    /// <summary>The extension a recording is written with.</summary>
    public const string Extension = ".ppmovie";

    public int Format { get; init; } = FormatVersion;
    public string GameVersion { get; init; } = "";
    public string DataVersion { get; init; } = "";
    public int Seed { get; init; }
    /// <summary>The options in force as the session began (<see cref="GameSettings.InForce"/>).</summary>
    public string Settings { get; init; } = "";
    /// <summary>The time zone's offset from UTC as the session began: the local time each frame's reading is read by.</summary>
    public TimeSpan Offset { get; init; }
    /// <summary>The region a new game began in (<c>--region</c>), or null for the chain's first.</summary>
    public string? Region { get; init; }
    /// <summary>The files the title read, by name in the save folder (<see cref="SavePaths.OwnFiles"/> and the autosave's backup).</summary>
    public Dictionary<string, byte[]> Saves { get; init; } = new();
    /// <summary>
    /// The packs in use as the session began, in load order, each "name version" (plan 16 · T14): a pack's scripts,
    /// rooms and songs are no part of <see cref="GameDataFiles.Version"/>, so a recording names them and is refused
    /// where other packs are in use. Empty for none, and in a recording of an older shape.
    /// </summary>
    public List<string> Packs { get; init; } = new();
    /// <summary>Whether the session began on the game's first run (plan 16 · T24), which opens on the first start's page.</summary>
    public bool FirstRun { get; init; }
    /// <summary>
    /// Whether the regions' maps were built several at once as the session began (<see cref="MapDatabase.Initialize(bool)"/>):
    /// each region then drew from a generator of its own, so the same seed makes other trainers than a build one
    /// after another does, and a replay builds its maps the way its recording did. False in a recording of an older shape.
    /// </summary>
    public bool RegionsTogether { get; init; }

    /// <summary>The packs in use now, as a recording names them.</summary>
    public static List<string> PacksInUse(IReadOnlyList<DataLayer>? packs = null) =>
        (packs ?? DataLayers.Packs).Where(l => l.Pack != null).Select(l => $"{l.Pack!.Name} {l.Pack.Version}").ToList();

    public List<InputFrame> Frames { get; } = new();
    public List<MovieCheckpoint> Checkpoints { get; } = new();

    /// <summary>The movie's time: every frame's seconds added up.</summary>
    public double Seconds => Frames.Sum(f => (double)f.Dt);

    /// <summary>
    /// Why this recording can't be played by this build, or null when it can: a newer file than this build reads,
    /// or a recording of other data, which would be played wrong rather than not at all.
    /// </summary>
    public string? Refusal(string? dataVersion = null, IReadOnlyList<string>? packsInUse = null)
    {
        if (Format > FormatVersion) return $"The recording is of a newer shape ({Format}) than this build reads ({FormatVersion}).";
        string here = dataVersion ?? GameDataFiles.Version;
        if (DataVersion != here) return $"The recording was made with other data ({DataVersion}) than this build has ({here}): it would play otherwise, so it isn't played.";
        var packs = packsInUse ?? PacksInUse();
        if (!packs.SequenceEqual(Packs, StringComparer.Ordinal))
        {
            string Named(IReadOnlyList<string> list) => list.Count == 0 ? "none" : string.Join(", ", list);
            return $"The recording was made with the packs {Named(Packs)} and this game has {Named(packs)} in use: it would play otherwise, so it isn't played.";
        }
        return null;
    }

    /// <summary>The files the title would read in a folder now, for a recording's header.</summary>
    public static Dictionary<string, byte[]> SavesIn(string folder)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (string name in SavePaths.OwnFiles().Append(SaveManager.BackupOf(SavePaths.AutosaveName)).Distinct())
        {
            string path = Path.Combine(folder, name);
            try
            {
                if (File.Exists(path)) files[name] = File.ReadAllBytes(path);
            }
            catch (IOException) { }
        }
        return files;
    }

    /// <summary>Lays the title's files down in a folder, as they were when the session began.</summary>
    public void LaySaves(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var (name, bytes) in Saves) File.WriteAllBytes(Path.Combine(folder, name), bytes);
    }

    // ------------------------------------------------------------------ the file

    public void Write(Stream stream)
    {
        using var zip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
        using var w = new BinaryWriter(zip, Encoding.UTF8);
        w.Write(Magic);
        w.Write(Format);
        w.Write(GameVersion);
        w.Write(DataVersion);
        w.Write(Seed);
        w.Write(Settings);
        w.Write(Offset.Ticks);
        w.Write(Region ?? "");
        w.Write(Saves.Count);
        foreach (var (name, bytes) in Saves.OrderBy(s => s.Key, StringComparer.Ordinal))
        {
            w.Write(name);
            w.Write(bytes.Length);
            w.Write(bytes);
        }
        if (Format >= 3)
        {
            w.Write(Packs.Count);
            foreach (string pack in Packs) w.Write(pack);
            w.Write(FirstRun);
        }
        if (Format >= 5)
        {
            w.Write(RegionsTogether);
        }
        w.Write(Frames.Count);
        foreach (var f in Frames)
        {
            w.Write(f.Dt);
            w.Write(f.UtcTicks);
            w.Write(f.Actions.Down);
            w.Write(f.Actions.Pressed);
            w.Write((byte)f.Window);
            if (Format >= 2)
            {
                w.Write(f.Pointer.X);
                w.Write(f.Pointer.Y);
                w.Write((byte)f.Pointer.Buttons);
                w.Write(f.Pointer.Wheel);
            }
            if (Format >= 4)
            {
                w.Write(f.Actions.MoveX);
                w.Write(f.Actions.MoveY);
            }
        }
        w.Write(Checkpoints.Count);
        foreach (var c in Checkpoints)
        {
            w.Write(c.Frame);
            w.Write(c.Seconds);
            w.Write(c.Hash.Length);
            foreach (uint h in c.Hash) w.Write(h);
        }
    }

    public static InputRecording Read(Stream stream)
    {
        using var zip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
        using var r = new BinaryReader(zip, Encoding.UTF8);
        if (r.ReadString() != Magic) throw new InvalidDataException("This is no recording of the game.");
        int format = r.ReadInt32();
        if (format > FormatVersion) return new InputRecording { Format = format };
        var saves = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        string gameVersion = r.ReadString(), dataVersion = r.ReadString();
        int seed = r.ReadInt32();
        string settings = r.ReadString();
        var offset = TimeSpan.FromTicks(r.ReadInt64());
        string? region = r.ReadString() is { Length: > 0 } named ? named : null;
        int files = r.ReadInt32();
        for (int i = 0; i < files; i++)
        {
            string name = r.ReadString();
            saves[name] = r.ReadBytes(r.ReadInt32());
        }
        var packs = new List<string>();
        bool firstRun = false;
        if (format >= 3)
        {
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++) packs.Add(r.ReadString());
            firstRun = r.ReadBoolean();
        }
        bool together = false;
        if (format >= 5)
        {
            together = r.ReadBoolean();
        }
        var recording = new InputRecording
        {
            Format = format,
            GameVersion = gameVersion,
            DataVersion = dataVersion,
            Seed = seed,
            Settings = settings,
            Offset = offset,
            Region = region,
            Saves = saves,
            Packs = packs,
            FirstRun = firstRun,
            RegionsTogether = together
        };
        int frames = r.ReadInt32();
        recording.Frames.Capacity = frames;
        for (int i = 0; i < frames; i++)
        {
            var frame = new InputFrame(r.ReadSingle(), r.ReadInt64(), new ActionFrame(r.ReadUInt32(), r.ReadUInt32()), (WindowEvents)r.ReadByte());
            if (format >= 2) frame = frame with { Pointer = new PointerFrame(r.ReadSingle(), r.ReadSingle(), (PointerButtons)r.ReadByte(), r.ReadSingle()) };
            // (an older shape has no tilt: its stick is read from the direction bits, as the keyboard's, InputManager.StickOf)
            if (format >= 4) frame = frame with { Actions = frame.Actions with { MoveX = r.ReadSByte(), MoveY = r.ReadSByte() } };
            recording.Frames.Add(frame);
        }
        int checkpoints = r.ReadInt32();
        for (int i = 0; i < checkpoints; i++)
        {
            int frame = r.ReadInt32();
            double seconds = r.ReadDouble();
            var hash = new uint[r.ReadInt32()];
            for (int k = 0; k < hash.Length; k++) hash[k] = r.ReadUInt32();
            recording.Checkpoints.Add(new MovieCheckpoint(frame, seconds, hash));
        }
        return recording;
    }

    public void WriteTo(string path)
    {
        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
        using var file = File.Create(path);
        Write(file);
    }

    public static InputRecording ReadFrom(string path)
    {
        using var file = File.OpenRead(path);
        return Read(file);
    }
}
