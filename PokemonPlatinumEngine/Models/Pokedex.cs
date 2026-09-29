using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

public class Pokedex
{
    public HashSet<int> SeenSpecies { get; } = new();
    public HashSet<int> CaughtSpecies { get; } = new();

    public int SeenCount => SeenSpecies.Count;
    public int CaughtCount => CaughtSpecies.Count;

    public void RegisterSeen(int dexNumber)
    {
        SeenSpecies.Add(dexNumber);
    }

    public void RegisterCaught(int dexNumber)
    {
        SeenSpecies.Add(dexNumber);
        CaughtSpecies.Add(dexNumber);
    }

    public void Clear()
    {
        SeenSpecies.Clear();
        CaughtSpecies.Clear();
    }

    public bool IsSeen(int dexNumber) => SeenSpecies.Contains(dexNumber);
    public bool IsCaught(int dexNumber) => CaughtSpecies.Contains(dexNumber);
}
