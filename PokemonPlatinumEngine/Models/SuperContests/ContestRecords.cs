using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.SuperContests;

/// <summary>
/// The photograph of a contest's winner (plan 06 · R17b; the original keeps one dressed-up picture for each of the five
/// contests, <c>ContestPhotoHasData</c>): the player's own Pokémon as it won, with the accessories it wore, hung in the
/// frame of its contest on the Contest Hall's wall until a later win of that contest replaces it.
/// </summary>
public sealed class ContestPhoto
{
    public ContestStat Category { get; set; }
    public ContestRank Rank { get; set; }

    /// <summary>The Pokémon: its species, form, whether it shone, and the name it went by.</summary>
    public string Species { get; set; } = "";
    public string? Form { get; set; }
    public bool Shiny { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Whose it was, and the day.</summary>
    public string Trainer { get; set; } = "";
    public DateTime Date { get; set; }

    /// <summary>The accessories it wore, by their names, in the order they were put on.</summary>
    public List<string> Accessories { get; set; } = new();
}

/// <summary>
/// What the game keeps of the player's Super Contests (plan 06 · R17b; the original's <c>RECORD_SUPER_CONTEST_PARTICIPATIONS</c>,
/// <c>RECORD_OFFICIAL_CONTEST_WINS</c> and <c>RECORD_RIBBONS_WON</c>, counted by <c>Contest_EndContest</c>, and the
/// winners' photographs): how many were entered and won, how many ribbons they brought, and the photograph in each
/// contest's frame. Saved. No drawing, no input.
/// </summary>
public sealed class ContestRecords
{
    /// <summary>The most a record counts to (the original's records of two bytes).</summary>
    public const int Most = 9999;

    public int Entered { get; set; }
    public int Won { get; set; }
    public int Ribbons { get; set; }

    /// <summary>The photograph in each contest's frame, by the contest's place among the five; null for an empty frame.</summary>
    public List<ContestPhoto?> Photos { get; set; } = new() { null, null, null, null, null };

    /// <summary>The photograph in a contest's frame (<c>ContestPhotoHasData</c>), or null.</summary>
    public ContestPhoto? PhotoOf(ContestStat category) => (int)category < Photos.Count ? Photos[(int)category] : null;

    /// <summary>
    /// Counts a contest that has ended (<c>Contest_EndContest</c>): one entered; a win is one won, a ribbon the Pokémon
    /// hadn't is one more ribbon, and the winner's photograph goes into its contest's frame. A practice round counts
    /// nothing.
    /// </summary>
    public void Count(ContestOutcome outcome, Pokemon pokemon, IEnumerable<int> worn, string trainer, DateTime today)
    {
        if (outcome.Mode != ContestMode.Official) return;
        Entered = Math.Min(Most, Entered + 1);
        if (!outcome.Won) return;
        Won = Math.Min(Most, Won + 1);
        if (outcome.NewRibbon != null) Ribbons = Math.Min(Most, Ribbons + 1);
        while (Photos.Count < 5) Photos.Add(null);
        Photos[(int)outcome.Category] = new ContestPhoto
        {
            Category = outcome.Category,
            Rank = outcome.Rank,
            Species = pokemon.Species.Name,
            Form = pokemon.Form,
            Shiny = pokemon.IsShiny,
            Name = pokemon.DisplayName,
            Trainer = trainer,
            Date = today.Date,
            Accessories = worn.Select(Data.SuperContestDatabase.AccessoryName).Where(n => n.Length > 0).ToList()
        };
    }

    /// <summary>Takes on what a save kept.</summary>
    public void Load(ContestRecords? saved)
    {
        Entered = saved?.Entered ?? 0;
        Won = saved?.Won ?? 0;
        Ribbons = saved?.Ribbons ?? 0;
        Photos = Enumerable.Range(0, 5).Select(i => saved != null && i < saved.Photos.Count ? saved.Photos[i] : null).ToList();
    }

    /// <summary>Whether there is anything to keep.</summary>
    public bool IsEmpty => Entered == 0 && Won == 0 && Ribbons == 0 && Photos.All(p => p == null);
}
