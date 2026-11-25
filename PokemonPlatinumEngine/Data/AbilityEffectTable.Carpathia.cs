using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

// Carpathia's seven abilities (plan 46 · CP10), ours: Battle/Effects/CarpathiaAbilityEffects.cs and, for the three the core
// reads by name, BattleCore.Carpathia.cs.
public static partial class AbilityEffectTable
{
    private static void AddCarpathia()
    {
        Add("Forest Hush", new ForestHush());
        // Read by name in the core: a contact move that missed (Anvil), a sound move struck again the turn after (Echo Pouch),
        // a damaging move struck three times (Three Heads)
        foreach (var name in new[] { "Anvil", "Echo Pouch", "Three Heads" }) Add(name, new FlagEffect());
        Add("Noonday", new StrongWeatherBringer(BattleWeather.HarshSun));
        Add("Eclipse", new Eclipse());
        Add("War Cry", new WarCry());
    }
}
