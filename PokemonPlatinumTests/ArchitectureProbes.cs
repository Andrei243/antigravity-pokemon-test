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

namespace PokemonPlatinumTests.Probes.Core.Diagnostics
{
    internal static class Report
    {
        public static bool Window() => Raylib_cs.Raylib.IsWindowReady();
    }
}

namespace PokemonPlatinumTests.Probes.Core
{
    internal static class CommandLine
    {
        public static bool InFront() => Raylib_cs.Raylib.IsWindowFocused();
    }

    // Where the saves live needs no window (plan 12 · Q2)
    internal static class SavePaths
    {
        public static bool Hidden() => Raylib_cs.Raylib.IsWindowHidden();
    }

    // Only the game's own start asks the real machine where its saves are; this is never run
    internal static class Elsewhere
    {
        public static string? OwnFolder() => PokemonPlatinumEngine.Core.SaveEnvironment.Here().PerUserFolder;
    }
}

namespace PokemonPlatinumTests.Probes.Foreign
{
    // The readers of a player's own files need no window and write nothing (plan 08 · P20)
    internal static class Reader
    {
        public static bool Window() => Raylib_cs.Raylib.IsWindowReady();
        public static void Writes(string path) => System.IO.File.WriteAllBytes(path, new byte[1]);
    }
}

namespace PokemonPlatinumTests.Probes.OpenWorld
{
    // The open world's rules need no window and keep no time but their ticks (plan 32 · OW1)
    internal static class Field
    {
        public static bool Window() => Raylib_cs.Raylib.IsWindowReady();
        public static System.DateTime Now() => PokemonPlatinumEngine.Core.WallClock.UtcNow;
        public static void Aside() => System.Threading.Tasks.Parallel.For(0, 2, _ => { });

        // A navigation mesh built at run time (plan 32 · OW4: only the file's reader makes one)
        public static object Navigation() => new PokemonPlatinumEngine.OpenWorld.OpenNavMesh(
            System.Array.Empty<PokemonPlatinumEngine.OpenWorld.OpenPos>(), System.Array.Empty<PokemonPlatinumEngine.OpenWorld.OpenNavPolygon>(),
            System.Array.Empty<PokemonPlatinumEngine.OpenWorld.OpenNavLink>());
    }
}

namespace PokemonPlatinumTests.Probes.Baker
{
    // The baker's rules (plan 32 · OW2): a window, a clock, unseeded chance and a second thread, each where it may not be
    internal static class Mesher
    {
        public static bool Window() => Raylib_cs.Raylib.IsWindowReady();
        public static long Clock() => System.Diagnostics.Stopwatch.GetTimestamp() + System.DateTime.UtcNow.Ticks + new Random().Next();
        public static void Aside() => System.Threading.Tasks.Parallel.For(0, 2, _ => { });
    }

    // And where they may be
    internal static class Pictures
    {
        public static bool Window() => Raylib_cs.Raylib.IsWindowReady();
    }

    internal static class RegionBake
    {
        public static void Aside() => System.Threading.Tasks.Parallel.For(0, 2, _ => { });
    }
}
