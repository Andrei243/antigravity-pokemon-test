using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The painted picture of each region's map as textures (plan 01 · M12): <see cref="RegionMapArt"/>'s canvas, a texel
/// a tile, drawn with its texels whole, and a copy at half its size, smoothed, for where the map is shown small (the
/// Pokédex's area page, the locator beside a zoomed map). A region's picture is painted the first time it is asked
/// for and again when the story has revealed another of its hidden places (<see cref="Show"/>), which the game tells
/// as it happens; everything that draws the region asks here, so they all show the same map.
/// </summary>
public static class RegionMapPictures
{
    private sealed class Entry
    {
        public HashSet<string> Revealed = new(StringComparer.OrdinalIgnoreCase);
        public string Wanted = "", Painted = "\0";
        public Texture2D Full, Small;
        public bool Loaded;
    }

    private static readonly Dictionary<string, Entry> entries = new(StringComparer.OrdinalIgnoreCase);

    private static Entry Of(RegionMap region)
    {
        if (!entries.TryGetValue(region.Region, out var entry)) entries[region.Region] = entry = new Entry();
        return entry;
    }

    /// <summary>Says which of a region's hidden places the story has revealed; the picture is painted again if that changed.</summary>
    public static void Show(RegionMap region, IEnumerable<string> revealed)
    {
        var entry = Of(region);
        entry.Revealed = revealed.ToHashSet(StringComparer.OrdinalIgnoreCase);
        entry.Wanted = string.Join(',', entry.Revealed.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A region's picture, painted now if it has not been or is out of date (about a tenth of a second): the whole
    /// picture a texel a tile, and its smoothed half. Null where the region's world or its habitats are not there.
    /// </summary>
    public static (Texture2D Full, Texture2D Small)? Get(RegionMap region)
    {
        var entry = Of(region);
        if (entry.Loaded && entry.Painted == entry.Wanted) return (entry.Full, entry.Small);
        if (!MapDatabase.MapNames.Contains(region.Map) || Habitats.For(region.Region) is not { } habitats) return null;

        var canvas = RegionMapArt.Paint(MapDatabase.Get(region.Map), region, (x, y) => habitats.LookAt(x, y) != ' ', entry.Revealed);
        if (entry.Loaded)
        {
            Raylib.UnloadTexture(entry.Full);
            Raylib.UnloadTexture(entry.Small);
        }
        entry.Full = canvas.ToTexture();
        Raylib.SetTextureFilter(entry.Full, TextureFilter.Point);
        entry.Small = RegionMapArt.Halved(canvas).ToTexture();
        Raylib.SetTextureFilter(entry.Small, TextureFilter.Bilinear);
        entry.Loaded = true;
        entry.Painted = entry.Wanted;
        return (entry.Full, entry.Small);
    }

    /// <summary>Lets every picture go (the game is closing, or its maps were made again).</summary>
    public static void Unload()
    {
        foreach (var entry in entries.Values)
            if (entry.Loaded)
            {
                Raylib.UnloadTexture(entry.Full);
                Raylib.UnloadTexture(entry.Small);
                entry.Loaded = false;
            }
    }
}
