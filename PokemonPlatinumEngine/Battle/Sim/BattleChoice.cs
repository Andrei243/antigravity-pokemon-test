using PokemonPlatinumEngine.Models;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// What a choice is: a move, a switch, an item, running; in the Great Marsh also bait or mud thrown at the Pokémon;
/// and staying in when Battle Style Shift offers a switch (plan 08 · P1); in a triple battle, a Pokémon at an end moving
/// to the centre (plan 14 · B6).
/// </summary>
public enum ChoiceKind { Fight, Switch, Item, Run, Bait, Mud, Stay, Shift }

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

    /// <summary>
    /// With a move: the Pokémon Mega Evolves first (plan 06 · R20; Showdown's <c>move 1 mega</c>), before the turn's
    /// moves. Refused where it can't (<see cref="BattleCore.WhyNotMega"/>).
    /// </summary>
    public bool Mega { get; init; }

    /// <summary>
    /// With a move: it is used as its Z-Move (plan 06 · R21; Showdown's <c>move 1 zmove</c>), through the Z-Crystal its
    /// user holds. Refused where it can't be (<see cref="BattleCore.WhyNotZMove"/>).
    /// </summary>
    public bool ZMove { get; init; }

    /// <summary>
    /// With a move: a Necrozma fused with Solgaleo or Lunala Ultra Bursts first (plan 06 · R21; Showdown's
    /// <c>move 1 ultra</c>), when the turn's Mega Evolutions come. Refused where it can't (<see cref="BattleCore.WhyNotUltraBurst"/>).
    /// </summary>
    public bool UltraBurst { get; init; }

    /// <summary>
    /// With a move: the Pokémon Dynamaxes first (plan 06 · R22; Showdown's <c>move 1 dynamax</c>), after the turn's
    /// Mega Evolutions, and the move is used as its Max Move. Refused where it can't (<see cref="BattleCore.WhyNotDynamax"/>).
    /// </summary>
    public bool Dynamax { get; init; }

    /// <summary>
    /// With a move: the Pokémon Terastallizes first (plan 06 · R23; Showdown's <c>move 1 terastallize</c>), after the
    /// turn's Mega Evolutions and Dynamaxes. Refused where it can't (<see cref="BattleCore.WhyNotTera"/>).
    /// </summary>
    public bool Terastallize { get; init; }

    /// <summary>
    /// With a move, in a rotation battle (plan 14 · B7): the turntable turns this way first, +1 bringing the Pokémon of
    /// the seat after the front's to the front and −1 the one before it, and <see cref="Move"/> is that Pokémon's move.
    /// 0 fights with the one in front. Refused where it can't (<see cref="BattleCore.WhyNotRotation"/>).
    /// </summary>
    public int Rotation { get; init; }

    /// <summary>
    /// With a move, in the Battle Palace (plan 14 · B9): the Pokémon chose it by its nature but can't bring itself to use
    /// it (Emerald's <c>palaceUnableToUseMove</c>), and spends its turn hesitating. Only <see cref="PalaceController"/>
    /// makes one; refused anywhere but the Palace (<see cref="BattleCore.WhyNot"/>).
    /// </summary>
    public bool Falters { get; init; }

    /// <summary>In place of a move's position: the move the Pokémon is in the middle of or held to.</summary>
    public const int HeldMove = -2;

    public static BattleChoice Fight(Place who, int move, Place? target = null) => new(who, ChoiceKind.Fight) { Move = move, Target = target };

    /// <summary>What the core makes for a Pokémon that has nothing to choose: it goes on with the move it is held to.</summary>
    public static BattleChoice GoOn(Place who) => new(who, ChoiceKind.Fight) { Move = HeldMove };
    public static BattleChoice Switch(Place who, int partyIndex) => new(who, ChoiceKind.Switch) { SwitchTo = partyIndex };
    /// <summary>An item from the bag: on the Pokémon of this place, or on the party member at <paramref name="onPartyMember"/> (a Revive on the bench), for the move at <paramref name="onMove"/> (an Ether).</summary>
    public static BattleChoice UseItem(Place who, string item, int onPartyMember = -1, int onMove = -1) =>
        new(who, ChoiceKind.Item) { Item = item, SwitchTo = onPartyMember, Move = onMove };
    public static BattleChoice Run(Place who) => new(who, ChoiceKind.Run);

    /// <summary>The Great Marsh: bait thrown to the Pokémon (easier to catch, likelier to run).</summary>
    public static BattleChoice Bait(Place who) => new(who, ChoiceKind.Bait);

    /// <summary>The Great Marsh: mud thrown at the Pokémon (likelier to stay, harder to catch).</summary>
    public static BattleChoice Mud(Place who) => new(who, ChoiceKind.Mud);

    /// <summary>
    /// The answer to a <see cref="SwitchOfferRequest"/> that keeps the Pokémon in (plan 08 · P1); a
    /// <see cref="Switch"/> is the other answer. (<see cref="GoOn"/> is the core's own answer for a Pokémon held to its move.)
    /// </summary>
    public static BattleChoice Stay(Place who) => new(who, ChoiceKind.Stay);

    /// <summary>
    /// A triple battle (plan 14 · B6): the Pokémon at an end trades places with the centre, and that is its turn
    /// (Black and White's rule). Refused anywhere else (<see cref="BattleCore.WhyNot"/>).
    /// </summary>
    public static BattleChoice Shift(Place who) => new(who, ChoiceKind.Shift);
}

/// <summary>
/// A battle as it can be kept or sent: the number its chance began from (a <see cref="BattleRandom"/>'s seed) and
/// every answer it was given from outside, in the order they were given. Played again between the same teams
/// (<see cref="BattleCore.Replay"/>) it gives the same log. An inverse battle (plan 14 · B1) says so, since the
/// same seed and answers under the other chart are another battle: a record written before the flag reads as the
/// plain chart's. So does a battle that offered Battle Style Shift's switch (plan 08 · P1), whose answers include
/// each offer's: a record written before the flag reads as a battle that offered none. The core's record says the
/// format the battle was fought in (plan 14 · B3: a horde's places draw numbers a single's don't); a record that
/// doesn't say (one written before, or a Vs. Recorder file's, whose setup names the format itself) isn't held to one.
/// </summary>
public sealed record BattleRecord(uint Seed, IReadOnlyList<IReadOnlyList<BattleChoice>> Answers, bool Inverse = false, bool OffersSwitch = false, Difficulty Difficulty = Difficulty.Normal, int RunLevelCap = 0,
    BattleFormat? Format = null);

/// <summary>What the core is waiting for before it can go on.</summary>
public abstract record BattleRequest;

/// <summary>A choice for the turn from each of these Pokémon (the ones nobody inside the core chooses for).</summary>
public sealed record ActionRequest(IReadOnlyList<Place> Places) : BattleRequest;

/// <summary>A Pokémon to send into a place whose own has fainted: a <see cref="BattleChoice.Switch"/>.</summary>
public sealed record ReplacementRequest(Place Place) : BattleRequest;

/// <summary>
/// Battle Style Shift (plan 08 · P1; the original's <c>subscript_replace_fainted</c>): the trainer across the field is
/// about to send <paramref name="Upcoming"/> in for the one that fainted, and the player may switch the Pokémon in
/// <paramref name="Place"/> first. Answered by <see cref="BattleChoice.Stay"/> or a <see cref="BattleChoice.Switch"/>.
/// Asked only when <see cref="CoreSetup.OffersSwitch"/> is set, in a single battle against a trainer.
/// </summary>
public sealed record SwitchOfferRequest(Place Place, Models.Pokemon Upcoming) : BattleRequest;

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
