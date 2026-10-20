using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Planks that give under a step (plan 21 · H6, part c; style guide, "Life", "Planks that give"): Emerald's
/// <c>FortreeBridgePerStepCallback</c> lowers the metatile of the plank the player stands on and bounces the one they
/// leave. The plank's picture is baked into its chunk and does not move here, so a walker standing on a plank of
/// Hoenn carries the dip alone: their sprite is lowered by whole rows, for as long as they stand there, with a little
/// shiver as their feet come onto a new plank. GPU-free; the clock is the field's own.
/// </summary>
internal sealed partial class FieldLife
{
    /// <summary>How long after the feet come onto a plank it drops (rows <see cref="PlankDrop"/>), springs back and drops again, in seconds.</summary>
    public const float PlankDropEnds = 0.07f, PlankSpringEnds = 0.13f, PlankShiverEnds = 0.20f;

    /// <summary>The rows a walker is lowered while they stand on a plank, and the rows of the drop and the shiver.</summary>
    public const int PlankRest = 1, PlankDrop = 2;

    private int plankX = int.MinValue, plankY = int.MinValue;
    private double plankSince;
    private string? plankMap;

    /// <summary>Whether a tile of a map is one of Hoenn's plank bridges: the deck Emerald's maps call <c>MB_FORTREE_BRIDGE</c>, which Hoenn's maps read as <c>Bridge</c>. Sinnoh's decks give nothing.</summary>
    public static bool IsPlank(Map map, int x, int y) =>
        map.InBounds(x, y) && map.BehaviourAt(x, y) == TileBehavior.Bridge && InHoenn(map.Name);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> hoennMaps = new(StringComparer.Ordinal);

    private static bool InHoenn(string mapName) =>
        hoennMaps.GetOrAdd(mapName, name => RegionDatabase.RegionOfMap(name)?.Id == RegionDatabase.Hoenn);

    /// <summary>The rows a walker on a plank is lowered by, as a function of how long ago their feet came onto it.</summary>
    public static int PlankDipAt(float age) =>
        age < PlankDropEnds ? PlankDrop
        : age < PlankSpringEnds ? PlankRest
        : age < PlankShiverEnds ? PlankDrop
        : PlankRest;

    /// <summary>
    /// How many rows the walker at this place (a point in tiles) is lowered by this frame: 0 off the planks. Asked
    /// once a frame for the player; the plank the walker's tile is remembered, and the time it was come onto, so
    /// a walker who steps from one plank to the next drops again on each.
    /// </summary>
    public int PlankRows(Map map, float x, float z)
    {
        int tx = (int)MathF.Floor(x), ty = (int)MathF.Floor(z);
        if (!IsPlank(map, tx, ty))
        {
            ForgetPlank();
            return 0;
        }
        if (tx != plankX || ty != plankY || !string.Equals(map.Name, plankMap, StringComparison.Ordinal))
        {
            plankX = tx;
            plankY = ty;
            plankMap = map.Name;
            plankSince = Now;
        }
        return PlankDipAt((float)(Now - plankSince));
    }

    private void ForgetPlank()
    {
        plankX = plankY = int.MinValue;
        plankMap = null;
    }
}
