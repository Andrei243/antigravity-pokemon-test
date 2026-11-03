using System.Collections.Generic;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What the story lays on a tile for the field to show (plan 21 · part r): Shoal Cave's piles of salt by what is left of
/// each, and under Hoenn's abnormal weather the caves' mouths it opens and the deep water it lays. The field's life is
/// told whenever the story or the map changes.
/// </summary>
public partial class GameEngine
{
    // What the field's life was last told: the story's revision and the map
    private int marksRevision = -1;
    private Map? marksMap;

    /// <summary>Every frame of the field: once the story or the map has changed, the field's life is told the map's marks.</summary>
    private void KeepTheSaltInView()
    {
        if (flow.Story.Revision == marksRevision && marksMap == flow.Map) return;
        marksRevision = flow.Story.Revision;
        marksMap = flow.Map;
        world.Life.SetMarks(flow.Map, StoryMarksOn(flow.Map, flow.Story.Has));
    }

    /// <summary>The marks the story has laid on a map (no GPU): its piles of salt, the abnormal weather's caves' mouths opened and its deep water.</summary>
    internal static IEnumerable<StoryMark> StoryMarksOn(Map map, System.Func<string, bool> flagSet)
    {
        foreach (var (x, y, left) in SaltPiles.On(map, flagSet))
            if (left != SaltLeft.None) yield return new StoryMark(x, y, left == SaltLeft.Whole ? StoryMarkKind.SaltHeap : StoryMarkKind.SaltLittle);
        foreach (var warp in map.Warps)
            if (warp.OpenedBy is { } flag && flag.StartsWith(AbnormalWeatherFlags, System.StringComparison.Ordinal) && flagSet(flag))
                yield return new StoryMark(warp.SourceX, warp.SourceY, StoryMarkKind.CaveMouth);
        foreach (var (tile, (flag, _)) in map.FlaggedDarkWater)
            if (flagSet(flag)) yield return new StoryMark(tile.X, tile.Y, StoryMarkKind.DeepWater);
    }

    /// <summary>The stem of the flags of Hoenn's abnormal weather, one to a place (plan 21 · H12, part e).</summary>
    internal const string AbnormalWeatherFlags = "FLAG_HOENN_ABNORMAL_WEATHER_";
}
