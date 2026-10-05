using System.Collections.Generic;

namespace PokemonPlatinumEngine.Battle.Sim;

public enum ChoiceKind { Fight, Switch, Item, Run }

/// <summary>
/// What one Pokémon's trainer decides for a turn, as plain data: a seed and the choices made are a whole battle
/// (a replay, a link battle's traffic).
/// </summary>
public sealed record BattleChoice(Place Who, ChoiceKind Kind)
{
    /// <summary>Which of its moves, by position; −1 is Struggle, for a Pokémon with nothing left to use.</summary>
    public int Move { get; init; } = -1;

    /// <summary>Where a single-target move is aimed (null: the core picks a foe).</summary>
    public Place? Target { get; init; }

    /// <summary>The party member to send in, by position.</summary>
    public int SwitchTo { get; init; } = -1;

    /// <summary>The item used, by name.</summary>
    public string? Item { get; init; }

    /// <summary>In place of a move's position: the move the Pokémon is in the middle of or held to.</summary>
    public const int HeldMove = -2;

    public static BattleChoice Fight(Place who, int move, Place? target = null) => new(who, ChoiceKind.Fight) { Move = move, Target = target };

    /// <summary>What the core makes for a Pokémon that has nothing to choose: it goes on with the move it is held to.</summary>
    public static BattleChoice GoOn(Place who) => new(who, ChoiceKind.Fight) { Move = HeldMove };
    public static BattleChoice Switch(Place who, int partyIndex) => new(who, ChoiceKind.Switch) { SwitchTo = partyIndex };
    public static BattleChoice UseItem(Place who, string item) => new(who, ChoiceKind.Item) { Item = item };
    public static BattleChoice Run(Place who) => new(who, ChoiceKind.Run);
}

/// <summary>
/// A battle as it can be kept or sent: the number its chance began from (a <see cref="BattleRandom"/>'s seed) and
/// every answer it was given from outside, in the order they were given. Played again between the same teams
/// (<see cref="BattleCore.Replay"/>) it gives the same log.
/// </summary>
public sealed record BattleRecord(uint Seed, IReadOnlyList<IReadOnlyList<BattleChoice>> Answers);

/// <summary>What the core is waiting for before it can go on.</summary>
public abstract record BattleRequest;

/// <summary>A choice for the turn from each of these Pokémon (the ones nobody inside the core chooses for).</summary>
public sealed record ActionRequest(IReadOnlyList<Place> Places) : BattleRequest;

/// <summary>A Pokémon to send into a place whose own has fainted: a <see cref="BattleChoice.Switch"/>.</summary>
public sealed record ReplacementRequest(Place Place) : BattleRequest;

/// <summary>
/// Chooses for one side from inside the core, with the battle's own random numbers, so that a seed and the other
/// side's choices still replay the battle. The opponents of the story are one (<see cref="TrainerAi"/>); a player
/// on the other end of a link battle is not: their choices come in from outside like the local player's.
/// </summary>
public interface IBattleController
{
    BattleChoice ChooseAction(BattleCore battle, Battler mine);

    /// <summary>The party member to send into an empty place, by position; −1 for nobody.</summary>
    int ChooseReplacement(BattleCore battle, Battler place);
}
