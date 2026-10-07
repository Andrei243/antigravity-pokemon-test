using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

public class Party
{
    public const int MaxSize = 6;
    public List<Pokemon> Members { get; } = new();

    public int Count => Members.Count;
    public bool IsFull => Members.Count >= MaxSize;
    public bool HasUsablePokemon => Members.Any(p => !p.IsFainted);

    public Pokemon? FirstUsable => Members.FirstOrDefault(p => !p.IsFainted);

    public bool Add(Pokemon pokemon)
    {
        if (IsFull) return false;
        Members.Add(pokemon);
        return true;
    }

    public void Remove(Pokemon pokemon)
    {
        Members.Remove(pokemon);
    }

    public void RemoveAt(int index)
    {
        if (index >= 0 && index < Members.Count)
        {
            Members.RemoveAt(index);
        }
    }

    public void Clear()
    {
        Members.Clear();
    }

    /// <summary>Puts a Pokémon in at a place of the team (the PC putting one down between two others); false when full.</summary>
    public bool Insert(int index, Pokemon pokemon)
    {
        if (IsFull) return false;
        Members.Insert(Math.Clamp(index, 0, Members.Count), pokemon);
        return true;
    }

    public void Swap(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= Members.Count || indexB < 0 || indexB >= Members.Count) return;
        (Members[indexA], Members[indexB]) = (Members[indexB], Members[indexA]);
    }

    public void HealAll()
    {
        foreach (var p in Members)
        {
            p.HealFull();
        }
    }
}
