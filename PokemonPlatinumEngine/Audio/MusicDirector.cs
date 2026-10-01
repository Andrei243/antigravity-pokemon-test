using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Audio;

/// <summary>The music that isn't tied to a place: battles, victories, fanfares and the title screen.</summary>
public enum MusicRole
{
    Title,
    BattleWild,
    BattleTrainer,
    BattleGymLeader,
    BattleRival,
    VictoryWild,
    VictoryTrainer,
    VictoryGymLeader,
    FanfareHeal,
    FanfareItem,
    FanfareLevelUp,
    FanfarePokemon
}

/// <summary>
/// Decides what plays, without touching the audio device. Areas name their theme in their map file; roles are
/// looked up first in the current region's folder, then in <c>common</c>, then through a more general role (a gym
/// leader without a theme of its own gets the trainer battle theme).
/// </summary>
public static class MusicDirector
{
    public const string Common = "common";

    /// <summary>The file name a role's song has in a region's folder.</summary>
    public static string FileName(MusicRole role) => role switch
    {
        MusicRole.Title => "title",
        MusicRole.BattleWild => "battle_wild",
        MusicRole.BattleTrainer => "battle_trainer",
        MusicRole.BattleGymLeader => "battle_gym",
        MusicRole.BattleRival => "battle_rival",
        MusicRole.VictoryWild => "victory_wild",
        MusicRole.VictoryTrainer => "victory_trainer",
        MusicRole.VictoryGymLeader => "victory_gym",
        MusicRole.FanfareHeal => "fanfare_heal",
        MusicRole.FanfareItem => "fanfare_item",
        MusicRole.FanfareLevelUp => "fanfare_levelup",
        MusicRole.FanfarePokemon => "fanfare_pokemon",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    /// <summary>The role to use when there is no song for this one anywhere.</summary>
    public static MusicRole? Fallback(MusicRole role) => role switch
    {
        MusicRole.BattleGymLeader or MusicRole.BattleRival => MusicRole.BattleTrainer,
        MusicRole.VictoryGymLeader => MusicRole.VictoryTrainer,
        _ => null
    };

    /// <summary>The song id for a role in a region, or null if nothing fits.</summary>
    public static string? Resolve(MusicRole role, string? region, Func<string, bool> exists)
    {
        for (MusicRole? r = role; r != null; r = Fallback(r.Value))
        {
            string file = FileName(r.Value);
            if (!string.IsNullOrEmpty(region))
            {
                string own = $"{region.ToLowerInvariant()}/{file}";
                if (exists(own)) return own;
            }
            string shared = $"{Common}/{file}";
            if (exists(shared)) return shared;
        }
        return null;
    }

    /// <summary>The battle theme for a fight: wild Pokémon, or the most important of the opposing trainers.</summary>
    public static MusicRole BattleRole(IEnumerable<string> trainerClasses)
    {
        var classes = trainerClasses.ToList();
        if (classes.Count == 0) return MusicRole.BattleWild;
        if (classes.Any(c => c.Contains("Leader", StringComparison.OrdinalIgnoreCase))) return MusicRole.BattleGymLeader;
        if (classes.Any(c => c.Contains("Rival", StringComparison.OrdinalIgnoreCase))) return MusicRole.BattleRival;
        return MusicRole.BattleTrainer;
    }

    /// <summary>The victory theme that follows a battle theme.</summary>
    public static MusicRole VictoryRole(MusicRole battle) => battle switch
    {
        MusicRole.BattleWild => MusicRole.VictoryWild,
        MusicRole.BattleGymLeader => MusicRole.VictoryGymLeader,
        _ => MusicRole.VictoryTrainer
    };

    /// <summary>Platinum switches to the night arrangements from evening's end until morning.</summary>
    public static bool IsNightArrangement(TimeOfDay time) => time is TimeOfDay.Night or TimeOfDay.LateNight;
}
