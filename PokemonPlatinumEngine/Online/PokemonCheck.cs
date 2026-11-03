using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Online;

/// <summary>
/// "Is this Pokémon possible?" (plan 07's design, Validation on the server): what the server refuses of a Pokémon
/// offered in a trade or brought to a battle, a line to each fault. It can't tell a Pokémon caught fairly from one made
/// in a save editor; it holds that nothing impossible goes from one game to another. The game may ask it too before it
/// offers, so a player is told at the counter rather than by the server. GPU-free.
/// <para>
/// What it checks: an id; a species and form of the data; a level from 1 to <see cref="Ruleset.HighestLevel"/> with
/// EXP inside that level for its growth rate (not for an Egg); IVs of 0 to 31, effort of at most 252 a stat and 510
/// in all; a gender the species can be, an ability it can have and a nature that is one; one to four moves, each of the
/// data, none twice, each one the species (or a forebear) can know by <see cref="Legality"/> or by either rules'
/// level-up list, PP Ups of 0 to 3 and PP no more than the move holds under either rules; a held item of the data; a
/// friendship of 0 to 255; HP no more than its stats give. Stats are never sent: they are worked out again.
/// </para>
/// </summary>
public static class PokemonCheck
{
    public const int MostEffortPerStat = 252, MostEffort = 510, MostIv = 31, MostFriendship = 255;

    /// <summary>
    /// What can't be of the Pokémon; empty when it can be. A trade needs its id (<paramref name="needsId"/>); a team
    /// brought to a battle is fought as copies and needs none.
    /// </summary>
    public static List<string> Faults(SavedPokemonData saved, bool needsId = true)
    {
        var faults = new List<string>();
        if (needsId && (saved.Id is not { } id || id == Guid.Empty)) faults.Add("it has no id");
        var species = PokemonDatabase.Get(saved.SpeciesName);
        if (species == null)
        {
            faults.Add($"no species is called {saved.SpeciesName}");
            return faults;
        }
        if (saved.DexNumber != 0 && saved.DexNumber != species.DexNumber) faults.Add($"{saved.SpeciesName} is not number {saved.DexNumber}");
        if (saved.Form != null && species.Form(saved.Form) == null) faults.Add($"{saved.SpeciesName} has no form {saved.Form}");

        if (saved.Level < 1 || saved.Level > Ruleset.HighestLevel) faults.Add($"level {saved.Level} is out of 1 to {Ruleset.HighestLevel}");
        else if (!saved.IsEgg)
        {
            int low = Pokemon.GetExpForLevel(saved.Level, species.GrowthRate);
            int high = saved.Level >= Ruleset.HighestLevel ? int.MaxValue : Pokemon.GetExpForLevel(saved.Level + 1, species.GrowthRate);
            if (saved.CurrentExp < low || saved.CurrentExp >= high) faults.Add($"{saved.CurrentExp} EXP is not level {saved.Level}");
        }

        int[] ivs = { saved.IvHP, saved.IvAttack, saved.IvDefense, saved.IvSpAttack, saved.IvSpDefense, saved.IvSpeed };
        if (ivs.Any(v => v < 0 || v > MostIv)) faults.Add($"an IV is out of 0 to {MostIv}");
        int[] evs = { saved.EvHP, saved.EvAttack, saved.EvDefense, saved.EvSpAttack, saved.EvSpDefense, saved.EvSpeed };
        if (evs.Any(v => v < 0 || v > MostEffortPerStat)) faults.Add($"an effort value is out of 0 to {MostEffortPerStat}");
        if (evs.Sum() > MostEffort) faults.Add($"its effort comes to {evs.Sum()}, past {MostEffort}");
        if (!Enum.IsDefined(saved.Nature)) faults.Add("its nature is none");
        if (saved.MintNature is { } mint && !Enum.IsDefined(mint)) faults.Add("its mint's nature is none");
        if (saved.Friendship is { } f && (f < 0 || f > MostFriendship)) faults.Add($"friendship {f} is out of 0 to {MostFriendship}");
        if (saved.HeldItem != null && ItemDatabase.Get(saved.HeldItem) == null) faults.Add($"no item is called {saved.HeldItem}");

        Pokemon made;
        try
        {
            made = saved.ToPokemon(Ruleset.Modern);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            faults.Add($"it can't be made: {e.Message}");
            return faults;
        }

        var ratio = species.GenderRatio;
        bool genderOk = saved.Gender switch
        {
            Gender.Genderless => ratio < 0,
            Gender.Male => ratio is >= 0 and < 8,
            Gender.Female => ratio > 0,
            _ => false
        };
        if (!genderOk) faults.Add($"{saved.SpeciesName} can't be {saved.Gender}");

        if (saved.Ability != null)
        {
            bool known = made.Abilities.Any(a => string.Equals(a, saved.Ability, StringComparison.OrdinalIgnoreCase))
                || string.Equals(made.HiddenAbilityName, saved.Ability, StringComparison.OrdinalIgnoreCase);
            if (!known) faults.Add($"{saved.SpeciesName} can't have {saved.Ability}");
        }

        if (saved.Moves.Count is < 1 or > 4) faults.Add($"it knows {saved.Moves.Count} moves");
        if (saved.Moves.Select(m => m.MoveName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != saved.Moves.Count) faults.Add("it knows a move twice");
        var learnable = Learnable(made);
        foreach (var move in saved.Moves)
        {
            if (!MoveDatabase.Has(move.MoveName))
            {
                faults.Add($"no move is called {move.MoveName}");
                continue;
            }
            if (!learnable.Contains(move.MoveName)) faults.Add($"{saved.SpeciesName} can't know {move.MoveName}");
            if (move.PPUps is < 0 or > 3) faults.Add($"{move.MoveName} has {move.PPUps} PP Ups");
            var data = MoveDatabase.Get(move.MoveName);
            int most = Math.Max(Move.MaxPPWith(data, move.PPUps), Move.MaxPPWith(data.Under(Ruleset.Modern), move.PPUps));
            if (move.CurrentPP < 0 || move.CurrentPP > most) faults.Add($"{move.MoveName} has {move.CurrentPP} PP, past {most}");
        }

        if (saved.CurrentHP < 0 || saved.CurrentHP > made.MaxHP) faults.Add($"{saved.CurrentHP} HP is past its {made.MaxHP}");
        return faults;
    }

    /// <summary>Every move the Pokémon can know: <see cref="Legality"/>'s, its form's list, and both rules' level-up lists.</summary>
    public static HashSet<string> Learnable(Pokemon pokemon)
    {
        var moves = Legality.Learnable(pokemon.Species);
        foreach (var m in pokemon.Learnset) moves.Add(m.MoveName);
        foreach (var m in pokemon.Species.Modern?.Learnset ?? new()) moves.Add(m.MoveName);
        foreach (var m in pokemon.FormData?.Modern?.Learnset ?? new()) moves.Add(m.MoveName);
        foreach (var m in pokemon.Species.Under(Ruleset.Modern).Learnset) moves.Add(m.MoveName);
        return moves;
    }
}
