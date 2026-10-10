using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The relief of a hand-made map (plan 01 · M9 1b): a room rebuilt to the original's plan with the original's
/// heights (a Gym's tiers, a pool), its stairs, the decks of its bridges and the tile behaviours that do more than
/// the tile's type says. Each is a layer of rows, as the map's tiles are (<c>docs/data-files.md</c>): a height is one
/// character in halves of a tile, a behaviour two hex digits. A map without them is flat and does what its types
/// imply, as every hand-made map did before.
/// </summary>
public static class MapRelief
{
    private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

    /// <summary>The character for a height in halves of a tile ('0' is 0, '8' is 4 tiles, 'z' is 17.5).</summary>
    public static char HeightCode(float tiles)
    {
        float halves = tiles * 2f;
        int whole = (int)MathF.Round(halves);
        if (MathF.Abs(halves - whole) > 0.01f || whole < 0 || whole >= Digits.Length)
            throw new ArgumentOutOfRangeException(nameof(tiles), tiles, "A map file's heights are whole halves of a tile, from 0 to 17.5.");
        return Digits[whole];
    }

    /// <summary>The height a character of <see cref="MapFile.Heights"/> or <see cref="MapFile.Decks"/> stands for, in tiles.</summary>
    public static float HeightOf(char code, string mapName)
    {
        int halves = Digits.IndexOf(code);
        if (halves < 0) throw new InvalidDataException($"Map {mapName}: '{code}' is no height (use '0'–'9' and 'a'–'z', halves of a tile).");
        return halves / 2f;
    }

    /// <summary>Stairs rise one tile across their tile: north ('n'), south ('s'), east ('e') or west ('w').</summary>
    private static (float X, float Z) SlopeOf(char code, string mapName) => code switch
    {
        '.' => (0f, 0f),
        'n' => (0f, -1f),
        's' => (0f, 1f),
        'e' => (1f, 0f),
        'w' => (-1f, 0f),
        _ => throw new InvalidDataException($"Map {mapName}: '{code}' is no slope (use '.', 'n', 's', 'e' or 'w').")
    };

    private static char SlopeCode((float X, float Z) slope) => slope switch
    {
        (0f, 0f) => '.',
        (0f, -1f) => 'n',
        (0f, 1f) => 's',
        (1f, 0f) => 'e',
        (-1f, 0f) => 'w',
        _ => throw new ArgumentOutOfRangeException(nameof(slope), slope, "A map file's stairs rise one tile across their tile, along x or z.")
    };

    /// <summary>Gives a map the relief and behaviours its file has.</summary>
    internal static void Read(MapFile file, Map map)
    {
        string name = file.Name;
        void Check(List<string> rows, string layer, int perTile)
        {
            if (rows.Count != file.Height)
                throw new InvalidDataException($"Map {name}: {layer} has {rows.Count} rows, expected {file.Height}.");
            for (int y = 0; y < rows.Count; y++)
                if (rows[y].Length != file.Width * perTile)
                    throw new InvalidDataException($"Map {name}: {layer} row {y} has {rows[y].Length} characters, expected {file.Width * perTile}.");
        }

        if (file.Behaviours is { } behaviours)
        {
            Check(behaviours, "behaviours", 2);
            for (int y = 0; y < file.Height; y++)
                for (int x = 0; x < file.Width; x++)
                {
                    string pair = behaviours[y].Substring(x * 2, 2);
                    if (pair == "..") continue;
                    if (!byte.TryParse(pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value))
                        throw new InvalidDataException($"Map {name}: behaviours row {y} has '{pair}' at column {x}; use two hex digits or '..'.");
                    map.SetBehaviour(x, y, (TileBehavior)value);
                }
        }

        if (file.Heights == null)
        {
            if (file.Slopes != null || file.Decks != null || file.GroundLevel != null)
                throw new InvalidDataException($"Map {name}: slopes, decks and a ground level need heights.");
            return;
        }
        Check(file.Heights, "heights", 1);
        if (file.Slopes != null) Check(file.Slopes, "slopes", 1);
        if (file.Decks != null) Check(file.Decks, "decks", 1);
        map.GroundLevel = file.GroundLevel ?? 0f;
        for (int y = 0; y < file.Height; y++)
            for (int x = 0; x < file.Width; x++)
            {
                var (sx, sz) = file.Slopes != null ? SlopeOf(file.Slopes[y][x], name) : (0f, 0f);
                map.SetHeight(x, y, HeightOf(file.Heights[y][x], name), sx, sz);
                if (file.Decks != null && file.Decks[y][x] != '.') map.SetDeck(x, y, HeightOf(file.Decks[y][x], name));
            }
    }

    /// <summary>Writes a map's relief and behaviours into its file; a flat map with none writes nothing.</summary>
    internal static void Write(Map map, MapFile file)
    {
        bool anyBehaviour = false;
        var behaviours = new List<string>();
        for (int y = 0; y < map.Height; y++)
        {
            var row = new StringBuilder(map.Width * 2);
            for (int x = 0; x < map.Width; x++)
            {
                if (map.OwnBehaviourAt(x, y) is { } own)
                {
                    anyBehaviour = true;
                    row.Append(((byte)own).ToString("X2", CultureInfo.InvariantCulture));
                }
                else row.Append("..");
            }
            behaviours.Add(row.ToString());
        }
        if (anyBehaviour) file.Behaviours = behaviours;

        if (!map.HasRelief) return;
        bool anySlope = false, anyDeck = false;
        var heights = new List<string>();
        var slopes = new List<string>();
        var decks = new List<string>();
        for (int y = 0; y < map.Height; y++)
        {
            var h = new StringBuilder(map.Width);
            var s = new StringBuilder(map.Width);
            var d = new StringBuilder(map.Width);
            for (int x = 0; x < map.Width; x++)
            {
                h.Append(HeightCode(map.HeightAt(x, y)));
                char slope = SlopeCode(map.SlopeAt(x, y));
                anySlope |= slope != '.';
                s.Append(slope);
                var deck = map.DeckAt(x, y);
                anyDeck |= deck != null;
                d.Append(deck is { } level ? HeightCode(level) : '.');
            }
            heights.Add(h.ToString());
            slopes.Add(s.ToString());
            decks.Add(d.ToString());
        }
        file.GroundLevel = map.GroundLevel != 0f ? map.GroundLevel : null;
        file.Heights = heights;
        if (anySlope) file.Slopes = slopes;
        if (anyDeck) file.Decks = decks;
    }
}
