using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// <c>Data/audio/sound-map.json</c>: what music a trainer class brings, by our names for the classes. The eye
/// themes follow the original's table (<c>src/field_bgm.c</c>); the battle themes name the few opponents whose
/// battle has a theme of its own. Read once, with no audio device, so the director and the tests can ask it.
/// </summary>
public sealed class SoundMapFile
{
    public string Comment { get; set; } = "";

    /// <summary>Trainer class → the short name of an eye theme (<c>boy</c>, <c>girl</c>, <c>galactic</c>…).</summary>
    public Dictionary<string, string> EyeThemes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Trainer class → the short name of a battle theme (<c>gym</c>, <c>rival</c>, <c>galactic</c>…).</summary>
    public Dictionary<string, string> BattleThemes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class SoundMap
{
    public const string FileName = "audio/sound-map.json";

    private static readonly Lazy<SoundMapFile> file = new(() =>
    {
        var read = GameDataFiles.Load<SoundMapFile>(FileName);
        return new SoundMapFile
        {
            Comment = read.Comment,
            EyeThemes = new Dictionary<string, string>(read.EyeThemes, StringComparer.OrdinalIgnoreCase),
            BattleThemes = new Dictionary<string, string>(read.BattleThemes, StringComparer.OrdinalIgnoreCase)
        };
    });

    public static IReadOnlyDictionary<string, string> EyeThemes => file.Value.EyeThemes;
    public static IReadOnlyDictionary<string, string> BattleThemes => file.Value.BattleThemes;
}
