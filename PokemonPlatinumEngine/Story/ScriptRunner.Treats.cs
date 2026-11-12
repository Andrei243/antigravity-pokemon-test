using System;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 35 · BN3: <c>treat</c>, the bakers'. <c>treat can</c> asks whether a bake could be made (<c>RESULT</c> 1
/// or 0): <c>treat can</c> or <c>treat can most</c> with three berries of the flavour the bag has most of, <c>treat can spicy</c>
/// and the other four with three of that flavour, <c>treat can mixed</c> with one of each of the five. <c>treat bake</c> (and the
/// same words) takes the berries and puts the treat in the bag (<c>RESULT</c> 1, <c>{item}</c> naming it), or 0 and nothing taken.
/// </summary>
public sealed partial class ScriptRunner
{
    private void TreatCommand(Instruction i)
    {
        bool mixed = i.Other == "mixed";
        Flavor? flavour = mixed ? null : i.Other is "most" or "" or null ? Treats.MostOf(host.Bag)
            : Enum.Parse<Flavor>(i.Other, ignoreCase: true);
        if (i.Name == "can")
        {
            Result = Treats.CanBake(host.Bag, flavour, mixed) ? 1 : 0;
            return;
        }
        var made = Treats.Bake(host.Bag, flavour, mixed);
        Result = made != null ? 1 : 0;
        if (made != null) lastItem = made.Name;
    }
}
