using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

public static partial class MapDatabase
{
    private static IEnumerable<NPC> Trainers
    {
        get
        {
            // Those a flag keeps off their map for now are counted too
            return Maps.Values.SelectMany(m => m.Everyone).Where(n => n.IsTrainer && n.TrainerData != null);
        }
    }

    /// <summary>Ids of the trainers the player has beaten, for the save file.</summary>
    public static List<string> DefeatedTrainerIds() =>
        Trainers.Where(n => n.HasBattled).Select(n => n.TrainerData!.Id).ToList();

    /// <summary>
    /// Puts everyone of every map where the story has them: on it, or off it while a flag hides them (plan 02 ·
    /// S1). With <paramref name="forget"/>, whoever a script showed or hid by itself goes by the flags again.
    /// </summary>
    /// <summary>Hides or reveals every map's hidden places as the story says (<see cref="Map.ApplyHiddenPlaces"/>); what changed.</summary>
    public static List<(Map Map, Map.HiddenPlace Place)> ApplyHiddenPlaces(System.Func<string, int> variable) =>
        Maps.Values.SelectMany(map => map.ApplyHiddenPlaces(variable).Select(place => (map, place))).ToList();

    public static void ApplyPresence(System.Func<string, bool> flagSet, bool forget = false)
    {
        foreach (var map in Maps.Values)
        {
            if (forget) map.ForgetForced();
            map.ApplyPresence(flagSet);
        }
    }

    /// <summary>Marks the trainers a loaded save says were beaten; all the others are waiting for a battle.</summary>
    public static void RestoreDefeatedTrainers(IEnumerable<string> ids)
    {
        var beaten = new HashSet<string>(ids);
        foreach (var npc in Trainers)
        {
            npc.HasBattled = beaten.Contains(npc.TrainerData!.Id);
        }
    }
}
