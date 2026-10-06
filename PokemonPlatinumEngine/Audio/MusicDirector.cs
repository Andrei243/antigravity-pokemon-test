using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// The music that isn't tied to a place: the title, riding, a trainer's eyes meeting the player's, battles,
/// victories and fanfares. The eye and battle roles follow Platinum's own set of themes
/// (<c>SEQ_EYE_BOY</c> … <c>SEQ_EYE_CHAMP</c>, <c>SEQ_BA_*</c>); a role without a song of its own falls back to a
/// more general one (<see cref="MusicDirector.Fallback"/>).
/// </summary>
public enum MusicRole
{
    Title,
    /// <summary>The new-game introduction: the professor's welcome.</summary>
    Introduction,
    /// <summary>The rival's own theme, for the scenes where he bursts in.</summary>
    Rival,
    /// <summary>The evolution scene.</summary>
    Evolution,
    Surf,
    Bicycle,
    EyeBoy,
    EyeGirl,
    EyeKid,
    EyeLady,
    EyeRich,
    EyeMountain,
    EyeFighter,
    EyeSport,
    EyeFun,
    EyeMystery,
    EyeSailor,
    EyeGalactic,
    EyeAce,
    EyeEliteFour,
    EyeChampion,
    BattleWild,
    BattleTrainer,
    BattleGymLeader,
    BattleRival,
    BattleGalactic,
    BattleGalacticBoss,
    BattleEliteFour,
    BattleChampion,
    BattleLegendary,
    VictoryWild,
    VictoryTrainer,
    VictoryGymLeader,
    FanfareHeal,
    FanfareItem,
    FanfareLevelUp,
    FanfarePokemon,
    FanfareEvolution,
    FanfareBadge,
    FanfareTM,
    FanfareKeyItem
}

/// <summary>
/// Decides what plays, without touching the audio device. Areas name their theme in their map file; roles are
/// looked up first in the current region's folder, then in <c>common</c>, then through a more general role (a gym
/// leader without a theme of its own gets the trainer battle theme). What a trainer class brings (its eye theme,
/// and a battle theme of its own for the few that have one) is the sound map, <c>Data/audio/sound-map.json</c>.
/// </summary>
public static class MusicDirector
{
    public const string Common = "common";

    /// <summary>The eye themes by the short names the sound map uses.</summary>
    public static readonly IReadOnlyDictionary<string, MusicRole> EyeThemes = new Dictionary<string, MusicRole>(StringComparer.OrdinalIgnoreCase)
    {
        ["boy"] = MusicRole.EyeBoy,
        ["girl"] = MusicRole.EyeGirl,
        ["kid"] = MusicRole.EyeKid,
        ["lady"] = MusicRole.EyeLady,
        ["rich"] = MusicRole.EyeRich,
        ["mountain"] = MusicRole.EyeMountain,
        ["fighter"] = MusicRole.EyeFighter,
        ["sport"] = MusicRole.EyeSport,
        ["fun"] = MusicRole.EyeFun,
        ["mystery"] = MusicRole.EyeMystery,
        ["sailor"] = MusicRole.EyeSailor,
        ["galactic"] = MusicRole.EyeGalactic,
        ["ace"] = MusicRole.EyeAce,
        ["eliteFour"] = MusicRole.EyeEliteFour,
        ["champion"] = MusicRole.EyeChampion
    };

    /// <summary>The battle themes a trainer class can bring, by the short names the sound map uses.</summary>
    public static readonly IReadOnlyDictionary<string, MusicRole> BattleThemes = new Dictionary<string, MusicRole>(StringComparer.OrdinalIgnoreCase)
    {
        ["trainer"] = MusicRole.BattleTrainer,
        ["gym"] = MusicRole.BattleGymLeader,
        ["rival"] = MusicRole.BattleRival,
        ["galactic"] = MusicRole.BattleGalactic,
        ["galacticBoss"] = MusicRole.BattleGalacticBoss,
        ["eliteFour"] = MusicRole.BattleEliteFour,
        ["champion"] = MusicRole.BattleChampion
    };

    /// <summary>The file name a role's song has in a region's folder.</summary>
    public static string FileName(MusicRole role) => role switch
    {
        MusicRole.Title => "title",
        MusicRole.Introduction => "intro",
        MusicRole.Rival => "rival",
        MusicRole.Evolution => "evolution",
        MusicRole.Surf => "surf",
        MusicRole.Bicycle => "bicycle",
        MusicRole.EyeBoy => "eye_boy",
        MusicRole.EyeGirl => "eye_girl",
        MusicRole.EyeKid => "eye_kid",
        MusicRole.EyeLady => "eye_lady",
        MusicRole.EyeRich => "eye_rich",
        MusicRole.EyeMountain => "eye_mountain",
        MusicRole.EyeFighter => "eye_fighter",
        MusicRole.EyeSport => "eye_sport",
        MusicRole.EyeFun => "eye_fun",
        MusicRole.EyeMystery => "eye_mystery",
        MusicRole.EyeSailor => "eye_sailor",
        MusicRole.EyeGalactic => "eye_galactic",
        MusicRole.EyeAce => "eye_ace",
        MusicRole.EyeEliteFour => "eye_elite_four",
        MusicRole.EyeChampion => "eye_champion",
        MusicRole.BattleWild => "battle_wild",
        MusicRole.BattleTrainer => "battle_trainer",
        MusicRole.BattleGymLeader => "battle_gym",
        MusicRole.BattleRival => "battle_rival",
        MusicRole.BattleGalactic => "battle_galactic",
        MusicRole.BattleGalacticBoss => "battle_galactic_boss",
        MusicRole.BattleEliteFour => "battle_elite_four",
        MusicRole.BattleChampion => "battle_champion",
        MusicRole.BattleLegendary => "battle_legendary",
        MusicRole.VictoryWild => "victory_wild",
        MusicRole.VictoryTrainer => "victory_trainer",
        MusicRole.VictoryGymLeader => "victory_gym",
        MusicRole.FanfareHeal => "fanfare_heal",
        MusicRole.FanfareItem => "fanfare_item",
        MusicRole.FanfareLevelUp => "fanfare_levelup",
        MusicRole.FanfarePokemon => "fanfare_pokemon",
        MusicRole.FanfareEvolution => "fanfare_evolution",
        MusicRole.FanfareBadge => "fanfare_badge",
        MusicRole.FanfareTM => "fanfare_tm",
        MusicRole.FanfareKeyItem => "fanfare_keyitem",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    /// <summary>The role to use when there is no song for this one anywhere.</summary>
    public static MusicRole? Fallback(MusicRole role) => role switch
    {
        // Riding: the place's own theme carries on (the engine plays it when the role resolves to nothing)
        MusicRole.EyeGirl or MusicRole.EyeKid or MusicRole.EyeMountain or MusicRole.EyeFighter or MusicRole.EyeSport
            or MusicRole.EyeFun or MusicRole.EyeMystery or MusicRole.EyeAce => MusicRole.EyeBoy,
        MusicRole.EyeLady or MusicRole.EyeRich => MusicRole.EyeGirl,
        MusicRole.EyeSailor => MusicRole.EyeMountain,
        MusicRole.EyeGalactic => MusicRole.EyeFighter,
        MusicRole.EyeEliteFour => MusicRole.EyeAce,
        MusicRole.EyeChampion => MusicRole.EyeEliteFour,
        MusicRole.BattleGymLeader or MusicRole.BattleRival or MusicRole.BattleGalactic => MusicRole.BattleTrainer,
        MusicRole.BattleGalacticBoss => MusicRole.BattleGalactic,
        MusicRole.BattleEliteFour => MusicRole.BattleGymLeader,
        MusicRole.BattleChampion => MusicRole.BattleEliteFour,
        MusicRole.BattleLegendary => MusicRole.BattleWild,
        MusicRole.VictoryGymLeader => MusicRole.VictoryTrainer,
        MusicRole.FanfareEvolution => MusicRole.FanfarePokemon,
        MusicRole.FanfareBadge or MusicRole.FanfareTM or MusicRole.FanfareKeyItem => MusicRole.FanfareItem,
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

    /// <summary>The theme that plays when a trainer of this class spots the player: the sound map's, or the boy's.</summary>
    public static MusicRole EyeRole(string? trainerClass)
    {
        if (trainerClass != null && SoundMap.EyeThemes.TryGetValue(trainerClass, out var theme) && EyeThemes.TryGetValue(theme, out var role))
            return role;
        return MusicRole.EyeBoy;
    }

    /// <summary>The battle theme one trainer class brings: the sound map's, or the trainer battle theme.</summary>
    public static MusicRole BattleRoleOf(string? trainerClass)
    {
        if (trainerClass != null && SoundMap.BattleThemes.TryGetValue(trainerClass, out var theme) && BattleThemes.TryGetValue(theme, out var role))
            return role;
        return MusicRole.BattleTrainer;
    }

    /// <summary>The battle theme for a fight: wild Pokémon, or the most important of the opposing trainers.</summary>
    public static MusicRole BattleRole(IEnumerable<string> trainerClasses)
    {
        var classes = trainerClasses.ToList();
        if (classes.Count == 0) return MusicRole.BattleWild;
        return classes.Select(BattleRoleOf).MaxBy(Rank);
    }

    /// <summary>How much a battle theme matters when two trainers bring different ones: the champion's over a grunt's.</summary>
    private static int Rank(MusicRole role) => role switch
    {
        MusicRole.BattleChampion => 6,
        MusicRole.BattleEliteFour => 5,
        MusicRole.BattleGalacticBoss => 4,
        MusicRole.BattleGymLeader => 3,
        MusicRole.BattleRival => 2,
        MusicRole.BattleGalactic => 1,
        _ => 0
    };

    /// <summary>The victory theme that follows a battle theme.</summary>
    public static MusicRole VictoryRole(MusicRole battle) => battle switch
    {
        MusicRole.BattleWild or MusicRole.BattleLegendary => MusicRole.VictoryWild,
        MusicRole.BattleGymLeader or MusicRole.BattleEliteFour or MusicRole.BattleChampion => MusicRole.VictoryGymLeader,
        _ => MusicRole.VictoryTrainer
    };

    /// <summary>Platinum switches to the night arrangements from evening's end until morning.</summary>
    public static bool IsNightArrangement(TimeOfDay time) => time is TimeOfDay.Night or TimeOfDay.LateNight;
}
