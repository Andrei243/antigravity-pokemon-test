using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Poffin Case (plan 06 · R14c; the original's <c>PoffinCase</c>, <c>src/poffin.c</c>): a hundred places, filled
/// from the first empty one, the rest closing up in order when one is taken out (<c>PoffinCase_ClearSlot</c> then
/// <c>PoffinCase_Compact</c>). Kept in the save. No drawing or input.
/// </summary>
public sealed class PoffinCase
{
    /// <summary>How many it holds (<c>MAX_POFFINS</c>).</summary>
    public const int Capacity = 100;

    private readonly List<Poffin> poffins = new();

    /// <summary>The Poffins in their places, the first first.</summary>
    public IReadOnlyList<Poffin> All => poffins;

    public int Count => poffins.Count;

    /// <summary>Whether there is no room for another (<c>PoffinCase_CountFilledSlots</c> ≥ 100).</summary>
    public bool IsFull => poffins.Count >= Capacity;

    /// <summary>The places still empty (<c>PoffinCase_CountEmptySlots</c>).</summary>
    public int Free => Capacity - poffins.Count;

    /// <summary>Puts a Poffin in the first empty place (<c>PoffinCase_AddPoffin</c>): false, with nothing changed, when the case is full.</summary>
    public bool Add(Poffin poffin)
    {
        if (IsFull) return false;
        poffins.Add(poffin);
        return true;
    }

    /// <summary>Takes out the Poffin at a place (eaten or thrown away); the ones after it move up a place each.</summary>
    public void RemoveAt(int index)
    {
        if (index >= 0 && index < poffins.Count) poffins.RemoveAt(index);
    }

    public void Clear() => poffins.Clear();

    /// <summary>The case as a save has it; anything past the hundredth place is left out.</summary>
    public void Restore(IEnumerable<Poffin>? saved)
    {
        poffins.Clear();
        if (saved != null) poffins.AddRange(saved.Take(Capacity));
    }

    /// <summary>
    /// The places of the Poffins the case lists under a flavour (<c>PoffinManager_FilterPoffins</c>: those with any of
    /// it; all of them for none), newest first, as the original links its list from the last place back.
    /// </summary>
    public IReadOnlyList<int> Listed(Flavor? filter)
    {
        var listed = new List<int>();
        for (int i = poffins.Count - 1; i >= 0; i--)
            if (filter is not { } flavor || poffins[i].Has(flavor)) listed.Add(i);
        return listed;
    }
}
