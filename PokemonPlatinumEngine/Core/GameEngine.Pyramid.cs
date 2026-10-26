using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Battle Pike and the Battle Pyramid in the game (plan 14 · B10): where their rules (<see cref="BattlePikeChallenge"/>,
/// <see cref="BattlePyramidChallenge"/>) meet the field. The Pike is its rooms and their scripts alone. A floor of the
/// Pyramid is a map made from its seed (<see cref="PyramidFloor"/>) that a script's warp to <c>BattlePyramidFloor</c>
/// reaches while a round is under way (<see cref="MapNamed"/>), its dark widened as battles are won
/// (<see cref="KeepThePyramidsLight"/>) and its wild Pokémon met in a battle of the Frontier's on the round's three
/// (<see cref="MeetInThePyramid"/>, the floor's <c>Wild</c> script).
/// </summary>
public partial class GameEngine
{
    public const string PikeMap = "BattlePike", PikeRoomMap = "BattlePikeRoom";

    /// <summary>The scripts of the Pyramid's floor the game itself starts, or a floor's people run: a wild Pokémon met, a trainer, a find.</summary>
    public static IEnumerable<string> PyramidScripts => new[] { "Wild", "Trainer", "HiddenItem" }.Select(name => $"{PyramidFloor.MapName}.{name}");

    /// <summary>The map a script's warp goes to: the Pyramid's floor of the round under way, or the map of that name.</summary>
    private Map MapNamed(string name) => name == PyramidFloor.MapName && flow.Pyramid?.FloorMap is { } floor ? floor : MapDatabase.Get(name);

    /// <summary>
    /// A wild Pokémon met on a floor of the Pyramid (Emerald's <c>GenerateBattlePyramidWildMon</c>): not a battle of the
    /// player's own team, but of the round's three, which the floor's script starts. False anywhere else.
    /// </summary>
    private bool MeetInThePyramid(WildEncounterEntry entry)
    {
        if (flow.Pyramid is not { FloorMap: { } floor } pyramid || flow.Map != floor) return false;
        pyramid.PendingWild = BattlePyramidChallenge.WildOf(entry, fieldRandom);
        StartScript("Wild", file: PyramidFloor.MapName);
        return true;
    }

    /// <summary>The floor's dark kept to the round's light, widening toward it as a battle is won (Emerald widens it a pixel a frame).</summary>
    private void KeepThePyramidsLight(float dt)
    {
        if (flow.Pyramid is not { FloorMap: { } floor } pyramid || flow.Map != floor) return;
        floor.IsDark = true;
        float target = BattlePyramidChallenge.ReachOf(pyramid.Light);
        float step = 3f * dt;
        floor.DarkReach = dt <= 0f ? target : floor.DarkReach < target ? MathF.Min(target, floor.DarkReach + step) : MathF.Max(target, floor.DarkReach - step);
    }

    private sealed partial class FieldHost
    {
        public BattlePikeChallenge? Pike { get => game.flow.Pike; set => game.flow.Pike = value; }
        public BattlePyramidChallenge? Pyramid { get => game.flow.Pyramid; set => game.flow.Pyramid = value; }
    }
}
