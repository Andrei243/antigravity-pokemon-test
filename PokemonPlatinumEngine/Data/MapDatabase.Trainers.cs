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
            return Maps.Values.SelectMany(m => m.NPCs).Where(n => n.IsTrainer && n.TrainerData != null);
        }
    }

    /// <summary>Ids of the trainers the player has beaten, for the save file.</summary>
    public static List<string> DefeatedTrainerIds() =>
        Trainers.Where(n => n.HasBattled).Select(n => n.TrainerData!.Id).ToList();

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
