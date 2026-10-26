using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A Pokémon as the text Pokémon Showdown imports and exports (plan 08 · P22, the way out of the migration): its name,
/// gender and item on a line, then its ability, level, shininess, effort, nature, IVs and moves. The format is
/// Showdown's own and so is English, whatever language the game is in: its names are the ones the data keeps (the
/// species' and forms' as Showdown names them). No drawing and no input.
/// </summary>
public static class ShowdownText
{
    private static readonly string[] Stats = { "HP", "Atk", "Def", "SpA", "SpD", "Spe" };

    /// <summary>The text of one Pokémon; lines end in a line feed and the last has none after it.</summary>
    public static string Of(Pokemon pokemon)
    {
        var ours = pokemon.Form ?? pokemon.Species.Name;
        // The few the data names otherwise go by Showdown's names (plan 06 · R29b)
        var species = ShowdownNames.ToShowdown(ours);
        var line = new StringBuilder();
        if (pokemon.Nickname.Length > 0 && pokemon.Nickname != pokemon.Species.Name && pokemon.Nickname != ours)
            line.Append(pokemon.Nickname).Append(" (").Append(species).Append(')');
        else line.Append(species);
        if (pokemon.Gender == Gender.Male) line.Append(" (M)");
        else if (pokemon.Gender == Gender.Female) line.Append(" (F)");
        if (pokemon.HeldItem != null) line.Append(" @ ").Append(pokemon.HeldItem.Name);

        var lines = new List<string> { line.ToString() };
        if (pokemon.AbilityName != null) lines.Add($"Ability: {pokemon.AbilityName}");
        lines.Add($"Level: {pokemon.Level}");
        if (pokemon.IsShiny) lines.Add("Shiny: Yes");
        if (pokemon.Friendship < 255) lines.Add($"Happiness: {pokemon.Friendship}");

        // Effort and IVs in Showdown's order of stats: HP, Atk, Def, SpA, SpD, Spe (the game's own is Speed before the special stats)
        int[] evs = { pokemon.EvHP, pokemon.EvAttack, pokemon.EvDefense, pokemon.EvSpAttack, pokemon.EvSpDefense, pokemon.EvSpeed };
        int[] ivs = { pokemon.IvHP, pokemon.IvAttack, pokemon.IvDefense, pokemon.IvSpAttack, pokemon.IvSpDefense, pokemon.IvSpeed };
        string effort = string.Join(" / ", Enumerable.Range(0, 6).Where(i => evs[i] > 0).Select(i => $"{evs[i]} {Stats[i]}"));
        if (effort.Length > 0) lines.Add($"EVs: {effort}");
        lines.Add($"{pokemon.Nature} Nature");
        // Showdown assumes 31 where an IV isn't written
        string individual = string.Join(" / ", Enumerable.Range(0, 6).Where(i => ivs[i] != 31).Select(i => $"{ivs[i]} {Stats[i]}"));
        if (individual.Length > 0) lines.Add($"IVs: {individual}");
        foreach (var move in pokemon.Moves) lines.Add($"- {move.Name}");
        return string.Join("\n", lines);
    }
}
