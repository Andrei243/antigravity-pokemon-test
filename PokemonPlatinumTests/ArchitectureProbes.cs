using System;

// The architecture tests' probes (ArchitectureTests.EachRuleFindsItsOwnBreak): each breaks one rule, under a root of their own
namespace PokemonPlatinumTests.Probes.Battle.Sim
{
    internal static class Breaks
    {
        public static int Chance() => new Random(1).Next() + PokemonPlatinumEngine.Core.Dice.Shared.Next();
        public static float Frame() => Raylib_cs.Raylib.GetFrameTime();
        public static void Sound() => PokemonPlatinumEngine.Core.AudioManager.PlaySound("cursor");
    }

    internal sealed class BattleCore
    {
        // The one place a battle may read Dice
        public BattleCore() => _ = PokemonPlatinumEngine.Core.Dice.Shared.Next();
    }
}

namespace PokemonPlatinumTests.Probes.Battle
{
    internal static class Reads
    {
        public static object? Ability(PokemonPlatinumEngine.Models.Pokemon p) => p.Ability;
    }

    internal sealed class Battler
    {
        public PokemonPlatinumEngine.Models.Pokemon? Pokemon;
        public object? Ability => Pokemon?.Ability;
    }
}

namespace PokemonPlatinumTests.Probes.Overworld
{
    internal static class Field
    {
        public static double Clock() => Raylib_cs.Raylib.GetFrameTime();
    }

    internal static class DialogueManager
    {
        public static double Clock() => Raylib_cs.Raylib.GetFrameTime();
    }
}

namespace PokemonPlatinumTests.Probes.Graphics
{
    internal static class PokeBuilder
    {
        public static int Sculpt() => new Random(5).Next();
    }

    internal static class Placing
    {
        public static float Height(PokemonPlatinumEngine.Overworld.Map map) => map.HeightAt(0, 0);
    }

    internal static class Relief
    {
        public static float Height(PokemonPlatinumEngine.Overworld.Map map) => map.HeightAt(0, 0);
    }
}
