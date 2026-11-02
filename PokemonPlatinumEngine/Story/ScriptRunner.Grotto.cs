using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 22 · U11: <c>grotto</c>, a Hidden Grotto (<see cref="HiddenGrottoes"/>). Its state is the story's own
/// variables, so nothing is kept by a host and nothing new is saved.
/// </summary>
public sealed partial class ScriptRunner
{
    private Grotto? GrottoHere => HiddenGrottoes.Get(host.Story.Var(HiddenGrottoes.Vars.Current));

    /// <summary>
    /// <c>mouth</c>: the grotto whose mouth the script's person is becomes the one gone into (RESULT 1, or 0 for a mouth no
    /// grotto has). <c>which</c>: RESULT the number of the grotto gone into (0 for none). <c>visit</c>: the grotto gone into as it is come to, refilled by the steps walked if it was empty
    /// (RESULT 0 empty, 1 a Pokémon, 2 an item; <c>{foe}</c> the Pokémon's species, <c>{item}</c> the item), and the
    /// person <c>grotto_mon</c> of the place stood as that species. <c>battle</c>: its Pokémon met with its hidden ability,
    /// and the grotto empty whatever came of it (RESULT as a <c>wildbattle</c>'s). <c>take</c>: its item handed over, as
    /// <c>find</c> hands one over, and the grotto empty.
    /// </summary>
    private void GrottoCommand(Instruction i)
    {
        var story = host.Story;
        switch (i.Name)
        {
            case "mouth":
            {
                var mouth = Subject is { } who
                    ? HiddenGrottoes.All.FirstOrDefault(g => g.X == who.GridX && g.Y == who.GridY && g.Area == who.LinesPlace)
                      ?? HiddenGrottoes.All.FirstOrDefault(g => g.X == who.GridX && g.Y == who.GridY)
                    : null;
                story.SetVar(HiddenGrottoes.Vars.Current, mouth?.Id ?? 0);
                Result = mouth == null ? 0 : 1;
                break;
            }
            case "which":
                // The grotto gone into, by its number (0 for none): the way out asks it to put the player before its mouth
                Result = story.Var(HiddenGrottoes.Vars.Current);
                break;
            case "visit":
            {
                Result = 0;
                if (GrottoHere is not { } grotto) break;
                var holds = HiddenGrottoes.Visit(story, grotto, host.Pastimes.Steps, host.Chance);
                if (holds == null) break;
                if (holds.IsPokemon)
                {
                    string species = SpeciesInGrotto(holds);
                    lastFoe = NameOfSpecies(species);
                    // Whoever stands for it in the grotto takes its look
                    if (host.FindNpc("grotto_mon", place.Length > 0 ? place : null) is { } stand) stand.Species = species;
                    Result = 1;
                }
                else
                {
                    lastItem = holds.Item ?? "";
                    Result = 2;
                }
                break;
            }
            case "battle":
            {
                if (GrottoHere is not { } grotto || HiddenGrottoes.Holds(story, grotto) is not { IsPokemon: true } holds) { Result = 0; break; }
                if (NoPokemonToBattle()) break;
                var species = PokemonDatabase.Get(SpeciesInGrotto(holds)) ?? throw Wrong(i, $"there is no species '{holds.Species}'");
                var wild = HiddenGrottoes.Make(species, holds, host.Chance);
                // Met is met: the grotto is empty however the battle ends
                HiddenGrottoes.Take(story, grotto, host.Pastimes.Steps);
                host.WildBattle(wild, BattleKind.Normal, cannotFlee: false);
                afterBusy = () => AfterBattle(mayLose: false);
                break;
            }
            case "take":
            {
                if (GrottoHere is not { } grotto || HiddenGrottoes.Holds(story, grotto) is not { IsPokemon: false, Item: { } name }) { Result = 0; break; }
                var (item, count) = Swapped(ItemDatabase.Get(name) ?? throw Wrong(i, $"there is no item '{name}'"), 1, i);
                HiddenGrottoes.Take(story, grotto, host.Pastimes.Steps);
                host.Bag.AddItem(item, count);
                lastItem = item.Name;
                host.Fanfare(FanfareFor(item));
                host.Say(null, new List<string>
                {
                    Text.Of("found.found", ("n", count), ("item", item.DisplayName)),
                    Text.Of("found.putAway", ("n", count), ("pocket", PocketName(item.Pocket)))
                });
                Result = 1;
                break;
            }
        }
    }

    /// <summary>A grotto's species, the one a randomised run maps it to (plan 13 · V6: a new place that gives a Pokémon asks).</summary>
    private string SpeciesInGrotto(GrottoEntry holds)
    {
        string species = holds.Species ?? "";
        return Randomizer.For(host.Rules) is { MapsSpecies: true } random ? random.Given(species) : species;
    }
}
