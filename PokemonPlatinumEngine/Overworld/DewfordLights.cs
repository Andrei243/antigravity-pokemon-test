using System;
using System.Linq;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The dark of Dewford Town's Gym (plan 21 · H3; Emerald's <c>DewfordTown_Gym_EventScript_SetFlashLevel</c>): the hall is
/// dark when the player first comes in, and every trainer of it beaten widens the circle the player sees round
/// themselves, until Brawly's defeat lights the whole hall. It is the Gym's own light, not a cave's: Flash does nothing
/// here (the header's weather is clear), so whatever Flash left in force doesn't light it. GPU-free; the engine keeps
/// <see cref="Map.IsDark"/> and <see cref="Map.DarkReach"/> to it, and the field draws the dark as a cave's.
/// </summary>
public static class DewfordLights
{
    /// <summary>The Gym's room, as <c>world.json</c> names it.</summary>
    public const string GymMap = "DewfordTown_Gym";

    /// <summary>The flag Brawly's battle sets as he is beaten, which lights the hall for good.</summary>
    public const string LeaderBeatenFlag = "FLAG_DEFEATED_DEWFORD_GYM";

    /// <summary>The six trainers of the hall whose defeats Emerald counts (<c>DewfordTown_Gym_EventScript_CountTrainersDefeated</c>).</summary>
    public static readonly string[] Trainers = { "hoenn_takao", "hoenn_jocelyn", "hoenn_laura", "hoenn_brenden", "hoenn_cristian", "hoenn_lilith" };

    /// <summary>
    /// The radius of the light at each of Emerald's flash levels, in the original's pixels (<c>sFlashLevelToRadius</c>):
    /// level 0 is the hall lit, 7 the darkest, which the player sees with nobody beaten.
    /// </summary>
    private static readonly int[] RadiusInPixels = { 200, 72, 64, 56, 48, 40, 32, 24 };

    /// <summary>The flash level the hall is at: 0 once Brawly is beaten, otherwise 7 less one for each trainer beaten.</summary>
    public static int LevelFor(StoryState story)
    {
        if (story.Has(LeaderBeatenFlag)) return 0;
        int beaten = Trainers.Count(story.HasDefeated);
        return 7 - beaten;
    }

    /// <summary>
    /// How far round the player the hall is seen at a level, in tiles to where nothing is seen: the original's radius
    /// in tiles of sixteen pixels and the dark's soft edge beyond it. Level 0 has no dark at all.
    /// </summary>
    public static float ReachFor(int level) =>
        level <= 0 ? float.PositiveInfinity : RadiusInPixels[Math.Clamp(level, 1, 7)] / 16f + Darkness.Soft;

    /// <summary>
    /// Lays the hall's dark over its map for the story as it stands, moving the light's reach toward the level's by
    /// <paramref name="dt"/> seconds (the original widens it over about half a second as a trainer falls); a
    /// <paramref name="dt"/> of nought sets it at once.
    /// </summary>
    public static void Keep(Map map, StoryState story, float dt)
    {
        int level = LevelFor(story);
        map.Lit = false;
        map.IsDark = level > 0;
        if (!map.IsDark) { map.DarkReach = Darkness.Reach; return; }
        float target = ReachFor(level);
        if (dt <= 0f || map.DarkReach >= 20f) { map.DarkReach = target; return; }
        float step = 3f * dt;
        map.DarkReach = map.DarkReach < target ? MathF.Min(target, map.DarkReach + step) : MathF.Max(target, map.DarkReach - step);
    }
}
