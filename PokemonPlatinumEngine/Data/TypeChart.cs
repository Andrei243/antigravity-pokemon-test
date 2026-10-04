using System.Collections.Generic;

namespace PokemonPlatinumEngine.Data;

public static class TypeChart
{
    private static readonly Dictionary<(PokemonType Atk, PokemonType Def), float> Multipliers = new()
    {
        // Normal
        { (PokemonType.Normal, PokemonType.Rock), 0.5f },
        { (PokemonType.Normal, PokemonType.Ghost), 0.0f },
        { (PokemonType.Normal, PokemonType.Steel), 0.5f },

        // Fire
        { (PokemonType.Fire, PokemonType.Fire), 0.5f },
        { (PokemonType.Fire, PokemonType.Water), 0.5f },
        { (PokemonType.Fire, PokemonType.Grass), 2.0f },
        { (PokemonType.Fire, PokemonType.Ice), 2.0f },
        { (PokemonType.Fire, PokemonType.Bug), 2.0f },
        { (PokemonType.Fire, PokemonType.Rock), 0.5f },
        { (PokemonType.Fire, PokemonType.Dragon), 0.5f },
        { (PokemonType.Fire, PokemonType.Steel), 2.0f },

        // Water
        { (PokemonType.Water, PokemonType.Fire), 2.0f },
        { (PokemonType.Water, PokemonType.Water), 0.5f },
        { (PokemonType.Water, PokemonType.Grass), 0.5f },
        { (PokemonType.Water, PokemonType.Ground), 2.0f },
        { (PokemonType.Water, PokemonType.Rock), 2.0f },
        { (PokemonType.Water, PokemonType.Dragon), 0.5f },

        // Grass
        { (PokemonType.Grass, PokemonType.Fire), 0.5f },
        { (PokemonType.Grass, PokemonType.Water), 2.0f },
        { (PokemonType.Grass, PokemonType.Grass), 0.5f },
        { (PokemonType.Grass, PokemonType.Poison), 0.5f },
        { (PokemonType.Grass, PokemonType.Ground), 2.0f },
        { (PokemonType.Grass, PokemonType.Flying), 0.5f },
        { (PokemonType.Grass, PokemonType.Bug), 0.5f },
        { (PokemonType.Grass, PokemonType.Rock), 2.0f },
        { (PokemonType.Grass, PokemonType.Dragon), 0.5f },
        { (PokemonType.Grass, PokemonType.Steel), 0.5f },

        // Electric
        { (PokemonType.Electric, PokemonType.Water), 2.0f },
        { (PokemonType.Electric, PokemonType.Grass), 0.5f },
        { (PokemonType.Electric, PokemonType.Electric), 0.5f },
        { (PokemonType.Electric, PokemonType.Ground), 0.0f },
        { (PokemonType.Electric, PokemonType.Flying), 2.0f },
        { (PokemonType.Electric, PokemonType.Dragon), 0.5f },

        // Ice
        { (PokemonType.Ice, PokemonType.Fire), 0.5f },
        { (PokemonType.Ice, PokemonType.Water), 0.5f },
        { (PokemonType.Ice, PokemonType.Grass), 2.0f },
        { (PokemonType.Ice, PokemonType.Ice), 0.5f },
        { (PokemonType.Ice, PokemonType.Ground), 2.0f },
        { (PokemonType.Ice, PokemonType.Flying), 2.0f },
        { (PokemonType.Ice, PokemonType.Dragon), 2.0f },
        { (PokemonType.Ice, PokemonType.Steel), 0.5f },

        // Fighting
        { (PokemonType.Fighting, PokemonType.Normal), 2.0f },
        { (PokemonType.Fighting, PokemonType.Ice), 2.0f },
        { (PokemonType.Fighting, PokemonType.Poison), 0.5f },
        { (PokemonType.Fighting, PokemonType.Flying), 0.5f },
        { (PokemonType.Fighting, PokemonType.Psychic), 0.5f },
        { (PokemonType.Fighting, PokemonType.Bug), 0.5f },
        { (PokemonType.Fighting, PokemonType.Rock), 2.0f },
        { (PokemonType.Fighting, PokemonType.Ghost), 0.0f },
        { (PokemonType.Fighting, PokemonType.Dark), 2.0f },
        { (PokemonType.Fighting, PokemonType.Steel), 2.0f },

        // Poison
        { (PokemonType.Poison, PokemonType.Grass), 2.0f },
        { (PokemonType.Poison, PokemonType.Poison), 0.5f },
        { (PokemonType.Poison, PokemonType.Ground), 0.5f },
        { (PokemonType.Poison, PokemonType.Rock), 0.5f },
        { (PokemonType.Poison, PokemonType.Ghost), 0.5f },
        { (PokemonType.Poison, PokemonType.Steel), 0.0f },

        // Ground
        { (PokemonType.Ground, PokemonType.Fire), 2.0f },
        { (PokemonType.Ground, PokemonType.Electric), 2.0f },
        { (PokemonType.Ground, PokemonType.Grass), 0.5f },
        { (PokemonType.Ground, PokemonType.Poison), 2.0f },
        { (PokemonType.Ground, PokemonType.Flying), 0.0f },
        { (PokemonType.Ground, PokemonType.Bug), 0.5f },
        { (PokemonType.Ground, PokemonType.Rock), 2.0f },
        { (PokemonType.Ground, PokemonType.Steel), 2.0f },

        // Flying
        { (PokemonType.Flying, PokemonType.Electric), 0.5f },
        { (PokemonType.Flying, PokemonType.Grass), 2.0f },
        { (PokemonType.Flying, PokemonType.Fighting), 2.0f },
        { (PokemonType.Flying, PokemonType.Bug), 2.0f },
        { (PokemonType.Flying, PokemonType.Rock), 0.5f },
        { (PokemonType.Flying, PokemonType.Steel), 0.5f },

        // Psychic
        { (PokemonType.Psychic, PokemonType.Fighting), 2.0f },
        { (PokemonType.Psychic, PokemonType.Poison), 2.0f },
        { (PokemonType.Psychic, PokemonType.Psychic), 0.5f },
        { (PokemonType.Psychic, PokemonType.Dark), 0.0f },
        { (PokemonType.Psychic, PokemonType.Steel), 0.5f },

        // Bug
        { (PokemonType.Bug, PokemonType.Fire), 0.5f },
        { (PokemonType.Bug, PokemonType.Grass), 2.0f },
        { (PokemonType.Bug, PokemonType.Fighting), 0.5f },
        { (PokemonType.Bug, PokemonType.Poison), 0.5f },
        { (PokemonType.Bug, PokemonType.Flying), 0.5f },
        { (PokemonType.Bug, PokemonType.Psychic), 2.0f },
        { (PokemonType.Bug, PokemonType.Ghost), 0.5f },
        { (PokemonType.Bug, PokemonType.Dark), 2.0f },
        { (PokemonType.Bug, PokemonType.Steel), 0.5f },

        // Rock
        { (PokemonType.Rock, PokemonType.Fire), 2.0f },
        { (PokemonType.Rock, PokemonType.Ice), 2.0f },
        { (PokemonType.Rock, PokemonType.Fighting), 0.5f },
        { (PokemonType.Rock, PokemonType.Ground), 0.5f },
        { (PokemonType.Rock, PokemonType.Flying), 2.0f },
        { (PokemonType.Rock, PokemonType.Bug), 2.0f },
        { (PokemonType.Rock, PokemonType.Steel), 0.5f },

        // Ghost
        { (PokemonType.Ghost, PokemonType.Normal), 0.0f },
        { (PokemonType.Ghost, PokemonType.Psychic), 2.0f },
        { (PokemonType.Ghost, PokemonType.Ghost), 2.0f },
        { (PokemonType.Ghost, PokemonType.Dark), 0.5f },
        { (PokemonType.Ghost, PokemonType.Steel), 0.5f },

        // Dragon
        { (PokemonType.Dragon, PokemonType.Dragon), 2.0f },
        { (PokemonType.Dragon, PokemonType.Steel), 0.5f },

        // Steel
        { (PokemonType.Steel, PokemonType.Fire), 0.5f },
        { (PokemonType.Steel, PokemonType.Water), 0.5f },
        { (PokemonType.Steel, PokemonType.Electric), 0.5f },
        { (PokemonType.Steel, PokemonType.Ice), 2.0f },
        { (PokemonType.Steel, PokemonType.Rock), 2.0f },
        { (PokemonType.Steel, PokemonType.Steel), 0.5f },

        // Dark
        { (PokemonType.Dark, PokemonType.Fighting), 0.5f },
        { (PokemonType.Dark, PokemonType.Psychic), 2.0f },
        { (PokemonType.Dark, PokemonType.Ghost), 2.0f },
        { (PokemonType.Dark, PokemonType.Dark), 0.5f },
        { (PokemonType.Dark, PokemonType.Steel), 0.5f },

        // Fairy (Generation 6). Only later species and moves have the type: Platinum's species keep their
        // Generation 4 typings and the rest of the chart stays Platinum's (plan 03, decision 2).
        { (PokemonType.Fairy, PokemonType.Fire), 0.5f },
        { (PokemonType.Fairy, PokemonType.Fighting), 2.0f },
        { (PokemonType.Fairy, PokemonType.Poison), 0.5f },
        { (PokemonType.Fairy, PokemonType.Dragon), 2.0f },
        { (PokemonType.Fairy, PokemonType.Dark), 2.0f },
        { (PokemonType.Fairy, PokemonType.Steel), 0.5f },
        { (PokemonType.Fighting, PokemonType.Fairy), 0.5f },
        { (PokemonType.Poison, PokemonType.Fairy), 2.0f },
        { (PokemonType.Bug, PokemonType.Fairy), 0.5f },
        { (PokemonType.Dragon, PokemonType.Fairy), 0.0f },
        { (PokemonType.Dark, PokemonType.Fairy), 0.5f },
        { (PokemonType.Steel, PokemonType.Fairy), 2.0f }
    };

    /// <param name="rules">The rules whose chart this is; left out, those of the game in progress.</param>
    public static float GetEffectiveness(PokemonType attackType, PokemonType defenderType1, PokemonType? defenderType2 = null, Ruleset? rules = null)
    {
        rules ??= Ruleset.Current;
        float mult1 = Against(attackType, defenderType1, rules);
        float mult2 = 1.0f;

        if (defenderType2.HasValue && defenderType2.Value != defenderType1)
        {
            mult2 = Against(attackType, defenderType2.Value, rules);
        }

        return mult1 * mult2;
    }

    private static float Against(PokemonType attack, PokemonType defender, Ruleset rules)
    {
        // Generation 6 took Steel's resistance to Ghost and Dark away
        if (!rules.SteelResistsGhostAndDark && defender == PokemonType.Steel && attack is PokemonType.Ghost or PokemonType.Dark) return 1.0f;
        return Multipliers.TryGetValue((attack, defender), out float m) ? m : 1.0f;
    }
}
