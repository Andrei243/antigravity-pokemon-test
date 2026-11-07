using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 40 · GE5: <c>charm</c>, which any region's designer says. <c>due</c> sets <c>RESULT</c> to how many
/// charms the player's Pokédex has earned and the bag doesn't hold (<see cref="Charms.Due"/>), and <c>{item}</c> to the
/// first of them; <c>give</c> hands that first one over as <c>give</c> hands an item (its fanfare, its two lines, the Key
/// Items pocket), <c>RESULT</c> 1, or does and says nothing with nothing due, <c>RESULT</c> 0. A designer who has more to
/// give asks <c>due</c> again after each. A host with no Pokédex (<see cref="IScriptHost.Pokedex"/>) has nothing due.
/// </summary>
public sealed partial class ScriptRunner
{
    private void CharmCommand(Instruction i)
    {
        IReadOnlyList<string> due = host.Pokedex is { } pokedex ? Charms.Due(pokedex, host.Bag) : Array.Empty<string>();
        switch (i.Name)
        {
            case "due":
                Result = due.Count;
                if (due.Count > 0) lastItem = due[0];
                break;
            case "give":
            {
                if (due.Count == 0)
                {
                    Result = 0;
                    break;
                }
                // A charm is a key item, which a randomised run never maps to another (Randomizer keeps the key items), so
                // it is handed over as it is: a charm swapped for something else would be due for ever
                var item = ItemDatabase.Get(due[0]) ?? throw Wrong(i, $"there is no item '{due[0]}'");
                host.Bag.AddItem(item, 1);
                lastItem = item.Name;
                host.Fanfare(FanfareFor(item));
                host.Say(null, new List<string>
                {
                    Text.Of("found.received", ("n", 1), ("item", item.DisplayName)),
                    Text.Of("found.putAway", ("n", 1), ("pocket", PocketName(item.Pocket)))
                });
                Result = 1;
                break;
            }
        }
    }
}
