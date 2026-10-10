using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The Pokémon History (<c>pokemon_history/main.c</c> and <c>graphics.c</c>): the last twelve Pokémon caught,
/// hatched or given, four to a row in three rows, read like a page: the oldest at the top left and the newest last
/// (<c>Poketch_PokemonHistoryEnqueue</c> adds at the end and drops the first). A Pokémon touched cries
/// (<c>State_UpdateApp</c>).
/// </summary>
public sealed class PokemonHistoryApp : PoketchAppState
{
    public const int PerRow = 4, RowsOfIcons = 3;

    private readonly Dictionary<string, Pokemon> criers = new();

    public override PoketchApp App => PoketchApp.PokemonHistory;

    /// <summary>The place of the history's <paramref name="i"/>th Pokémon, in blocks: a cell of 10 by 11, four to a row.</summary>
    public static PoketchButton Cell(int i) => new(i, 3 + (i % PerRow) * 10, 2 + (i / PerRow) * 11, 9, 11);

    /// <summary>The Pokémon shown, oldest first, by their model's name.</summary>
    public static IReadOnlyList<string> Entries(PoketchContext context) => context.Poketch.History;

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context)
    {
        int count = Math.Min(Poketch.HistoryLength, context.Poketch.History.Count);
        var list = new List<PoketchButton>(count);
        for (int i = 0; i < count; i++) list.Add(Cell(i));
        return list;
    }

    public override void Press(int button, PoketchContext context)
    {
        var history = context.Poketch.History;
        if (button < 0 || button >= history.Count) return;
        if (Crier(history[button], context) is { } pokemon) context.Cry(pokemon);
    }

    /// <summary>
    /// A Pokémon of the history's species and form to cry with: the history keeps only the model's name, as the
    /// original keeps the species and form, so one is made the first time it is touched.
    /// </summary>
    private Pokemon? Crier(string model, PoketchContext context)
    {
        if (criers.TryGetValue(model, out var known)) return known;
        var species = PokemonDatabase.Get(model) ?? PokemonDatabase.SpeciesOfForm(model);
        if (species == null) return null;
        var pokemon = new Pokemon(species, 1, context.Rng);
        pokemon.Form = species.Form(model) != null ? model : null;
        criers[model] = pokemon;
        return pokemon;
    }
}
