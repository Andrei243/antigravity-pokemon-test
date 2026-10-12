using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// What may be skipped of the story (plan 12 · Q20). A script marks a scene with <c>scene "Name"</c> and
/// <c>endscene</c>; a scene played to its <c>endscene</c> is seen, by the game it was played in
/// (<see cref="StoryState.ScenesSeen"/>, kept in the save) and by this machine (<c>GameSettings.ScenesSeen</c>, kept
/// with the options, so a second game knows it). The options' row says which scenes the player may skip: none, those
/// seen, or any. No drawing and no keys: the engine asks, and <see cref="SkipHold"/> counts the hold.
/// </summary>
public static class SceneSkip
{
    /// <summary>A scene's key: the file its script is written in and its own name, which no two scenes of a file share.</summary>
    public static string Key(string file, string name) => file + "." + name;

    /// <summary>
    /// Whether a scene may be skipped under the options' row: never with it off, always under <c>Any</c>, and under
    /// <c>Seen</c> once this game or this machine has played it to its end.
    /// </summary>
    public static bool Allowed(SkipScenes row, string key, StoryState story, IReadOnlyCollection<string> machine) => row switch
    {
        SkipScenes.Any => true,
        SkipScenes.Seen => story.HasSeenScene(key) || Contains(machine, key),
        _ => false
    };

    private static bool Contains(IReadOnlyCollection<string> keys, string key)
    {
        if (keys is ICollection<string> set) return set.Contains(key);
        foreach (string k in keys)
            if (string.Equals(k, key, StringComparison.Ordinal)) return true;
        return false;
    }
}

/// <summary>
/// The hold that skips a scene (plan 12 · Q20; style guide, "Story scenes"): the button kept down for
/// <see cref="Seconds"/> while a scene that may be skipped runs. Let go early it starts again from nothing; once it
/// has done its work the button has to be let go before it counts again, so one long press skips one stretch. It
/// only counts: whoever reads the key says whether it is down, and whoever draws the ring asks <see cref="Fill"/>.
/// </summary>
public sealed class SkipHold
{
    /// <summary>How long the button is held before the scene is skipped.</summary>
    public const float Seconds = 1f;

    /// <summary>A frame longer than this counts as this long, so a hitch doesn't finish the hold by itself.</summary>
    private const float LongestFrame = 0.1f;

    private float held;
    private bool spent;

    /// <summary>How far the hold has come, 0 to 1: the share of its ring that is filled.</summary>
    public float Fill => Math.Clamp(held / Seconds, 0f, 1f);

    /// <summary>True while the button is counted as held: the ring is drawn then and at no other time.</summary>
    public bool Showing => held > 0f;

    /// <summary>
    /// A frame of the hold. <paramref name="down"/> says the button is down and <paramref name="allowed"/> that a
    /// scene which may be skipped is running; true the one frame the hold is complete.
    /// </summary>
    public bool Update(bool down, bool allowed, float dt)
    {
        if (!down)
        {
            held = 0f;
            spent = false;
            return false;
        }
        if (!allowed || spent)
        {
            held = 0f;
            return false;
        }
        held += Math.Clamp(dt, 0f, LongestFrame);
        if (held < Seconds) return false;
        held = 0f;
        spent = true;
        return true;
    }

    /// <summary>Forgets the hold: a new game, a loaded one.</summary>
    public void Reset()
    {
        held = 0f;
        spent = false;
    }
}
