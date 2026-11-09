using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Every song in <c>Data/music</c>, read once. A song's id is its path there without the extension:
/// <c>common/battle_wild</c>, <c>kanto/pallet</c>, <c>sinnoh/twinleaf</c>. Each region has a folder for its own
/// themes; <c>common</c> holds the ones every region shares until it gets its own.
/// </summary>
public static class MusicLibrary
{
    private static Dictionary<string, Song>? songs;
    private static readonly object loadLock = new();

    public static string Directory => GameDataFiles.PathOf("music");

    private static Dictionary<string, Song> Songs
    {
        get
        {
            lock (loadLock)
            {
                return songs ??= Load();
            }
        }
    }

    public static IReadOnlyCollection<string> Ids => Songs.Keys;

    public static IEnumerable<Song> All => Songs.Values;

    public static bool Exists(string id) => Songs.ContainsKey(id);

    public static Song? Get(string id) => Songs.TryGetValue(id, out var s) ? s : null;

    private static Dictionary<string, string>? recordingFiles;
    private static Song? lastRecording;

    /// <summary>
    /// The recorded songs (2026-11-09: each region's credits song, generated with Lyria 3.5), by id: an Ogg Vorbis file
    /// <c>Data/music/&lt;id&gt;.ogg</c>, a pack's over the game's. They are not among <see cref="All"/>, which is the songs
    /// of notes every check of the music holds, and a recording of an id wins over a song of notes of the same id.
    /// </summary>
    private static Dictionary<string, string> RecordingFiles
    {
        get
        {
            lock (loadLock)
            {
                if (recordingFiles != null) return recordingFiles;
                var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (key, path) in DataLayers.FilesIn("music", "*.ogg", recursive: true))
                    files[key[..^".ogg".Length]] = path;
                return recordingFiles = files;
            }
        }
    }

    /// <summary>The ids that have a recording.</summary>
    public static IReadOnlyCollection<string> RecordingIds => RecordingFiles.Keys;

    public static bool HasRecording(string id) => RecordingFiles.ContainsKey(id);

    /// <summary>
    /// A recorded song by id, its file read (about two megabytes; the last one asked for is kept) and its length taken
    /// from the file's header; null where there is none or the file isn't a readable Ogg Vorbis file.
    /// </summary>
    public static Song? GetRecording(string id)
    {
        if (!RecordingFiles.TryGetValue(id, out var path)) return null;
        lock (loadLock)
        {
            if (lastRecording is { } kept && string.Equals(kept.Id, id, StringComparison.OrdinalIgnoreCase)) return kept;
        }
        try
        {
            var bytes = File.ReadAllBytes(path);
            using var reader = new NVorbis.VorbisReader(new MemoryStream(bytes), true);
            var song = new Song
            {
                Id = id, Title = Path.GetFileNameWithoutExtension(path), Recording = bytes,
                RecordedSeconds = reader.TotalTime.TotalSeconds, HasNight = false, Sound = SynthSound.Modern,
            };
            lock (loadLock) lastRecording = song;
            return song;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            Core.Diagnostics.Log.Warn($"AUDIO: the recording '{id}' can't be read: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Reads every song again while the game runs (plan 16 · T6, the developer's live reload): the new set takes the
    /// old one's place only when every file reads; otherwise what was wrong is handed back and the songs stay as they were.
    /// </summary>
    public static List<string> Reload()
    {
        try
        {
            var fresh = Load();
            lock (loadLock) { songs = fresh; recordingFiles = null; lastRecording = null; }
            return new List<string>();
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return new List<string> { e.Message };
        }
    }

    private static Dictionary<string, Song> Load()
    {
        var result = new Dictionary<string, Song>(StringComparer.OrdinalIgnoreCase);
        var regions = RegionSounds(ReadRegionFiles());
        // The game's own songs and every pack's, a pack's song replacing the game's of the same id (plan 16 · T14)
        foreach (var (key, path) in DataLayers.FilesIn("music", "*.mml", recursive: true))
        {
            string id = key[..^".mml".Length];
            result[id] = Mml.Parse(id, File.ReadAllText(path), SoundOfFolder(id, regions));
        }
        return result;
    }

    /// <summary>Every region file of the data, as the regions' sounds are read from them.</summary>
    private static IEnumerable<RegionFile> ReadRegionFiles()
    {
        foreach (var (key, _) in DataLayers.FilesIn("regions", "*.json"))
            if (GameDataFiles.Load<RegionFile>("regions/" + key) is { } file) yield return file;
    }

    /// <summary>
    /// The sound each region's music folder plays in (plan 31 · MU2), by the folder's name: only the regions whose files
    /// name a sound. A region's folder is its file's <c>folder</c>, or its id in lower case.
    /// </summary>
    public static Dictionary<string, SynthSound> RegionSounds(IEnumerable<RegionFile> files)
    {
        var sounds = new Dictionary<string, SynthSound>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
            if (file.MusicSound is { } sound)
                sounds[file.Folder is { Length: > 0 } f ? f : file.Id.ToLowerInvariant()] = sound;
        return sounds;
    }

    /// <summary>The sound a song's folder (the first part of its id, <c>kalos/vaniville</c>) plays in, or null: <c>common</c> and a region that names none.</summary>
    public static SynthSound? SoundOfFolder(string id, IReadOnlyDictionary<string, SynthSound> regions)
    {
        int slash = id.IndexOf('/');
        return slash > 0 && regions.TryGetValue(id[..slash], out var sound) ? sound : null;
    }
}
