using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Pokémon that wear the season (plan 22 · U4; <c>docs/mechanics/rulings.md</c>, "Seasons in Unova"): Deerling
/// and Sawsbuck, in spring as their species and in the other three in the coat of the season
/// (<c>Deerling-Summer</c>, <c>Sawsbuck-Winter</c>…). Black 2 puts every one in the party in the season's coat as the
/// game is taken up and as the season turns, and one in a box as it comes out; the engine asks this only where the
/// region has seasons, so a coat taken out of Unova stays what it is. An evolution carries a coat over by the data's
/// <c>fromForm</c> and <c>targetForm</c>. No drawing or input.
/// </summary>
public static class SeasonalForms
{
    /// <summary>Whether a species changes its coat with the season.</summary>
    public static bool Changes(string species) => species is "Deerling" or "Sawsbuck";

    /// <summary>The form a species wears in a season: null for spring (the species itself) and for a species with no coats.</summary>
    public static string? FormFor(string species, Season season) =>
        !Changes(species) || season == Season.Spring ? null : $"{species}-{season}";

    /// <summary>
    /// Puts a Pokémon in the season's coat if its species has coats; an Egg is left as it is, and so is one already in
    /// it. True when the coat changed.
    /// </summary>
    public static bool Wear(Pokemon pokemon, Season season)
    {
        if (pokemon.IsEgg || !Changes(pokemon.Species.Name)) return false;
        string? form = FormFor(pokemon.Species.Name, season);
        if (form != null && pokemon.Species.Form(form) == null) return false;
        if (pokemon.Form == (form == null ? null : pokemon.Species.Form(form)!.Name)) return false;
        pokemon.ChangeForm(form);
        return true;
    }
}
