using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>How a photograph of the book was taken (plan 43 · LF4).</summary>
public enum PhotoTaken { PhotoMode, Spot, Studio }

/// <summary>
/// A photograph of the book (plan 43 · LF4): its picture's file in <c>photos/</c> (its small copy beside it), where and
/// when it was taken, the lead's look, the photo spot's id where it was taken at one, and how. The picture is the
/// player's own file; a record whose file is gone is drawn as Cameron's prints are.
/// </summary>
public sealed record PhotoPrint
{
    public string File { get; init; } = "";
    public string Place { get; init; } = "";
    public string Region { get; init; } = "";
    public DateTime Day { get; init; }
    public string? Lead { get; init; }
    public string? Spot { get; init; }
    public PhotoTaken How { get; init; }

    /// <summary>The composition of a studio photograph (plan 43 · LF5); null for any other.</summary>
    public StudioShot? Studio { get; init; }

    /// <summary>The small copy's file: the picture's with <c>-small</c> before its extension.</summary>
    public string SmallFile => SmallOf(File);

    public static string SmallOf(string file) =>
        file.Length == 0 ? "" : System.IO.Path.ChangeExtension(file, null) + "-small" + System.IO.Path.GetExtension(file);
}

/// <summary>
/// The photo book (plan 43 · LF4, decision 5): a hundred photographs at most, newest last, saved as
/// <c>SaveData.PhotoBook</c> (null while empty, so an older save needs nothing). GPU-free: the pictures are the
/// engine's to write and to read; the book keeps the records and says which spots of a region are taken.
/// </summary>
public sealed class PhotoBook
{
    /// <summary>How many photographs the book holds (decision 5).</summary>
    public const int Capacity = 100;

    /// <summary>The picture's size in the book, and its small copy's for a page.</summary>
    public const int Width = 1920, Height = 1080, SmallWidth = 480, SmallHeight = 270;

    private readonly List<PhotoPrint> prints = new();

    public IReadOnlyList<PhotoPrint> Prints => prints;
    public int Count => prints.Count;
    public bool IsFull => prints.Count >= Capacity;

    /// <summary>A photograph kept; false once the book is full.</summary>
    public bool Add(PhotoPrint print)
    {
        if (IsFull) return false;
        prints.Add(print);
        return true;
    }

    /// <summary>A photograph thrown away (the record; its two files are the engine's to delete).</summary>
    public bool Remove(PhotoPrint print) => prints.Remove(print);

    public void Clear() => prints.Clear();

    /// <summary>The spots of a region with a photograph in the book.</summary>
    public IEnumerable<string> SpotsTakenIn(string region) =>
        prints.Where(p => p.Spot != null && string.Equals(p.Region, region, StringComparison.OrdinalIgnoreCase)).Select(p => p.Spot!).Distinct();

    /// <summary>The regions whose every photo spot has a photograph in the book (what unlocks a card's background).</summary>
    public IEnumerable<string> RegionsWhole() =>
        PhotoSpots.All.GroupBy(s => s.Region, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.All(s => prints.Any(p => p.Spot == s.Id)))
            .Select(g => g.Key);

    /// <summary>The regions with a photograph, in the order they first appear: the book's tabs after ALL.</summary>
    public IReadOnlyList<string> Regions => prints.Select(p => p.Region).Where(r => r.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public List<PhotoPrint>? ToSave() => prints.Count == 0 ? null : prints.ToList();

    public void Load(IEnumerable<PhotoPrint>? saved)
    {
        prints.Clear();
        if (saved != null) prints.AddRange(saved.Take(Capacity));
    }

    /// <summary>A picture's name: the moment to the millisecond and its count, as the screenshot key's are, so they sort by time.</summary>
    public static string NameFor(DateTime time, int n) =>
        string.Format(CultureInfo.InvariantCulture, "photo-{0:yyyyMMdd-HHmmss-fff}-{1:00}.png", time, n);

    /// <summary>The first name of a moment not taken in <paramref name="folder"/>; never a file written over.</summary>
    public static string PathFor(string folder, DateTime time, Func<string, bool> exists)
    {
        for (int n = 0; n < 99; n++)
        {
            string path = System.IO.Path.Combine(folder, NameFor(time, n));
            if (!exists(path) && !exists(PhotoPrint.SmallOf(path))) return path;
        }
        return System.IO.Path.Combine(folder, NameFor(time, 99));
    }
}

/// <summary>A photo spot (plan 43 · LF4): where its plate stands, the way the player faces reading it, the point the camera looks at and its zoom.</summary>
public sealed record PhotoSpot
{
    [DataRequired] public string Id { get; init; } = "";
    [DataRequired] public string Region { get; init; } = "";
    [DataRequired] public string Map { get; init; } = "";

    /// <summary>The plate's tile.</summary>
    [DataRequired] public int X { get; init; }
    [DataRequired] public int Z { get; init; }

    /// <summary>The tile the camera looks at, and the lens's factor.</summary>
    [DataRequired] public int LookX { get; init; }
    [DataRequired] public int LookZ { get; init; }
    public float Zoom { get; init; } = 1f;
}

/// <summary>What <c>Data/photo-spots.json</c> holds (ours, written by hand, read strictly).</summary>
public sealed record PhotoSpotsFile
{
    public List<PhotoSpot> Spots { get; init; } = new();
}

/// <summary>The photo spots of every built region (plan 43 · LF4): five of ours to a region, each named in <c>ui.json</c>'s <c>photoBook.spot</c>.</summary>
public static class PhotoSpots
{
    public const string FileName = "photo-spots.json";

    private static readonly Lazy<PhotoSpotsFile> file = new(() => GameDataFiles.Load<PhotoSpotsFile>(FileName, strict: true));

    public static IReadOnlyList<PhotoSpot> All => file.Value.Spots;

    public static PhotoSpot? Get(string? id) => All.FirstOrDefault(s => s.Id == id);

    /// <summary>The spot whose plate stands on a tile of a map; null for none.</summary>
    public static PhotoSpot? At(string map, int x, int z) => All.FirstOrDefault(s => s.Map == map && s.X == x && s.Z == z);
}
