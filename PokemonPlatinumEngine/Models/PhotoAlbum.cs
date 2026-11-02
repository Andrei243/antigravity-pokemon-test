using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// One of Cameron's photographs (plan 20, part q): where it was taken (the area's key, its name told as the language
/// names it where it is shown), the Pokémon that led the team then (its look, <see cref="Pokemon.LookName"/>: a form's,
/// a female's and a shiny one's own; null for none), the player's look and the day. Plain values, saved as they are.
/// </summary>
public sealed class Photo
{
    public string Place { get; set; } = "";
    public string? Pokemon { get; set; }
    public string Player { get; set; } = "";

    /// <summary>The day it was taken, as <c>yyyy-MM-dd</c>.</summary>
    public string Day { get; set; } = "";

    /// <summary>The day as a date, or null where it doesn't read as one.</summary>
    public DateTime? Date() => DateTime.TryParseExact(Day, DayFormat, System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.None, out var when) ? when : null;

    /// <summary>How <see cref="Day"/> is written.</summary>
    public const string DayFormat = "yyyy-MM-dd";
}

/// <summary>
/// The photo album Cameron's father keeps in Cianwood City's studio (plan 20, part q; HeartGold's photo album, in our
/// own numbers): up to <see cref="Capacity"/> photographs in the order they were taken, one to a page. A photograph
/// is taken by Cameron at his places (<c>photo take</c>) and the album looked through at the studio (<c>photo album</c>);
/// one can be thrown away from its page. No drawing or input.
/// </summary>
public sealed class PhotoAlbum
{
    /// <summary>How many photographs the album holds (ours: HeartGold's album isn't read).</summary>
    public const int Capacity = 36;

    private readonly List<Photo> photos = new();

    public IReadOnlyList<Photo> Photos => photos;
    public int Count => photos.Count;
    public bool IsFull => photos.Count >= Capacity;

    /// <summary>Puts a photograph at the album's end; false when the album is full.</summary>
    public bool Add(Photo photo)
    {
        if (IsFull) return false;
        photos.Add(photo);
        return true;
    }

    /// <summary>A photograph taken now: the place, the team's first Pokémon that is no Egg, the player's look and the day.</summary>
    public static Photo Of(string place, Models.Pokemon? lead, string look, DateTime day) => new()
    {
        Place = place,
        Pokemon = lead?.LookName,
        Player = look,
        Day = day.ToString(Photo.DayFormat, System.Globalization.CultureInfo.InvariantCulture)
    };

    /// <summary>Throws a photograph away; the ones after it move up a page.</summary>
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= photos.Count) return false;
        photos.RemoveAt(index);
        return true;
    }

    public void Clear() => photos.Clear();

    /// <summary>The album as the save keeps it: null while empty, so an older save needs nothing.</summary>
    public List<Photo>? ToSave() => photos.Count == 0 ? null : photos.Select(p => new Photo
    {
        Place = p.Place, Pokemon = p.Pokemon, Player = p.Player, Day = p.Day
    }).ToList();

    public void Load(IEnumerable<Photo>? saved)
    {
        photos.Clear();
        foreach (var p in saved ?? Enumerable.Empty<Photo>())
            if (photos.Count < Capacity) photos.Add(p);
    }
}
