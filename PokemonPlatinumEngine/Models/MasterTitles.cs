using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The titles of the Masters' Hall (plan 14 · B15; <c>docs/mechanics/rulings.md</c>, "Master Trainers"): a title is a
/// Master Trainer beaten, which the story already keeps (<see cref="StoryState.DefeatedTrainers"/>, the ids that begin
/// with <c>master_</c>), so no field of the save is new. Which was won last is the story's variable
/// <see cref="LatestVariable"/>, the species' National Pokédex number, set by the script's <c>master</c> on a win.
/// GPU-free.
/// </summary>
public static class MasterTitles
{
    /// <summary>The story's variable of the title won last: the National Pokédex number of its species; nought for none.</summary>
    public const string LatestVariable = "VAR_MASTER_TITLE_LATEST";

    /// <summary>How many titles there are: a master for every species of the games (1025).</summary>
    public static int Total => TrainerDatabase.Masters.Count;

    // Each master's id to its species and National Pokédex number, built once
    private static readonly Lazy<Dictionary<string, (string Species, int Number)>> Ids = new(() =>
    {
        var ids = new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase);
        foreach (var master in TrainerDatabase.Masters)
        {
            if (master.Party.Count == 0 || PokemonDatabase.Get(master.Party[0].Species) is not { } species) continue;
            ids[master.Id] = (species.Name, species.DexNumber);
        }
        return ids;
    });

    /// <summary>A species' title as the chosen language names the master's class ("Bidoof Master": <see cref="Trainer.ClassName"/>).</summary>
    public static string Title(string species) => Trainer.ClassName(species + MasterClassEnding);

    // How the masters' file writes every master's class after its species' name
    private const string MasterClassEnding = " Master";

    /// <summary>Whether an id is a master's.</summary>
    public static bool IsMaster(string trainerId) => Ids.Value.ContainsKey(trainerId);

    /// <summary>The species of the titles won, by National Pokédex number.</summary>
    public static IReadOnlyList<(string Species, int Number)> Won(StoryState story) =>
        story.DefeatedTrainers.Where(IsMaster).Select(id => Ids.Value[id]).OrderBy(t => t.Number).ToList();

    /// <summary>How many titles are won.</summary>
    public static int Count(StoryState story) => story.DefeatedTrainers.Count(IsMaster);

    /// <summary>Whether the master of a species is beaten.</summary>
    public static bool Holds(StoryState story, string species) => story.HasDefeated(TrainerDatabase.MasterId(species));

    /// <summary>The species of the title won last, or null for none.</summary>
    public static string? Latest(StoryState story)
    {
        int number = story.Var(LatestVariable);
        return number <= 0 ? null : PokemonDatabase.GetByDex(number)?.Name;
    }

    /// <summary>A title won: the master beaten, and the latest the one of this species.</summary>
    public static void Win(StoryState story, PokemonSpecies species)
    {
        story.Defeat(TrainerDatabase.MasterId(species.Name));
        story.SetVar(LatestVariable, species.DexNumber);
    }
}
