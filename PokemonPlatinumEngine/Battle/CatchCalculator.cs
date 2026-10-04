using System;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public static class CatchCalculator
{
    public struct CatchResult
    {
        public bool IsCaught;
        public int Shakes; // 0, 1, 2, 3, or 4 (4 = caught)
    }

    /// <param name="rng">The battle's own generator; left out, the rolls are the game's shared chance.</param>
    public static CatchResult AttemptCatch(Pokemon wildPokemon, ItemData ball, Random? rng = null)
    {
        rng ??= Core.Dice.Shared;
        // Master Ball always catches
        if (ball.EffectValue >= 9999)
        {
            return new CatchResult { IsCaught = true, Shakes = 4 };
        }

        float ballMultiplier = ball.EffectValue / 10.0f; // e.g. 1.0 for Poke Ball, 1.5 for Great, 2.0 for Ultra
        float statusMultiplier = wildPokemon.Status switch
        {
            StatusCondition.Sleep or StatusCondition.Freeze => 2.0f,
            StatusCondition.Poison or StatusCondition.Toxic or StatusCondition.Burn or StatusCondition.Paralyze => 1.5f,
            _ => 1.0f
        };

        // Gen 4 Catch Rate Formula:
        // a = ((3 * MaxHP - 2 * CurrentHP) * CatchRate * BallBonus) / (3 * MaxHP) * StatusBonus
        float maxHP = wildPokemon.MaxHP;
        float curHP = wildPokemon.CurrentHP;
        float catchRate = wildPokemon.Species.CatchRate;

        float a = ((3.0f * maxHP - 2.0f * curHP) * catchRate * ballMultiplier) / (3.0f * maxHP) * statusMultiplier;
        a = Math.Clamp(a, 1.0f, 255.0f);

        if (a >= 255.0f)
        {
            return new CatchResult { IsCaught = true, Shakes = 4 };
        }

        // Shake probability b = 65536 * (a / 255)^(1/4)
        double b = 65536.0 * Math.Pow(a / 255.0, 0.25);
        int bInt = (int)Math.Clamp(b, 0, 65535);

        int shakes = 0;
        for (int i = 0; i < 4; i++)
        {
            int check = rng.Roll(RollKind.CatchShake, 65536);
            if (check < bInt)
            {
                shakes++;
            }
            else
            {
                break;
            }
        }

        return new CatchResult
        {
            IsCaught = shakes == 4,
            Shakes = shakes
        };
    }
}
