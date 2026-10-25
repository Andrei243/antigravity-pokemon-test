using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// The formats Platinum never had (plan 14; <c>docs/mechanics/rulings.md</c>, "Formats of the later games"), each a
/// flag or a shape the battle is handed as it is made and that nothing changes while it runs. B1: an inverse battle.
/// B3: a horde, one against five.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>What an inverse battle says as it opens: a line of our own, since the later games' say nothing.</summary>
    public static string InverseLine => Text.Of("battle.everyTypeMatchupIs");

    /// <summary>
    /// An inverse battle (<see cref="BattleConditions.Inverse"/>): the type chart is turned round from the first
    /// turn to the last. The damage formula, the lines about how a hit did, the immunities of types, Stealth Rock,
    /// Conversion 2, the berries against a type, Anticipation, Wonder Guard and the trainer AI all ask the same
    /// chart (<see cref="Data.TypeChart.GetEffectiveness"/>, through <see cref="FieldState.Inverse"/>), so none has
    /// a rule of its own for it. What an ability or a move's own magnetism stops is still stopped: Levitate and
    /// Magnet Rise keep Ground moves off, as in X and Y.
    /// </summary>
    public bool Inverse => Conditions.Inverse;

    /// <summary>The battle says what format it is once everyone is out, before the weather of the place.</summary>
    private void AnnounceFormat()
    {
        if (Inverse) Say(InverseLine);
    }

    /// <summary>
    /// A horde (plan 14 · B3; <see cref="BattleFormat.Horde"/>): the player's one Pokémon against up to five wild ones,
    /// as X and Y have it, under rules that have hordes (<see cref="Data.Ruleset.Hordes"/>). Each wild Pokémon chooses
    /// for itself (<see cref="TrainerAi"/>: a move at random, at a foe drawn for it), a move that hits every foe hits
    /// all five and each takes three quarters (the spread hit every format has), a ball can't be aimed until one is left,
    /// each foe leaves its EXP as it faints, and running is measured against the fastest of them. What a wild double
    /// battle refuses (Roar, Whirlwind, Teleport, Dragon Tail and Circle Throw driving one off, Emergency Exit's flight) a
    /// horde refuses too: none of them ends a battle with others still on the field.
    /// </summary>
    public bool IsHorde => Format == BattleFormat.Horde;

    /// <summary>What a horde says as it comes out: the species when all of them are one, no names when they aren't.</summary>
    private static string HordeAppears(IReadOnlyList<Battler> foes) =>
        foes.Select(b => b.Pokemon!.Species.Name).Distinct().Count() == 1
            ? Text.Of("battle.aWildHorde", ("pokemon", foes[0].Pokemon!.DisplayName))
            : Text.Of("battle.aMixedHorde");

    /// <summary>
    /// Who a run from a wild battle is measured against: in a horde the fastest of the wild Pokémon still standing (plan
    /// 14 · B3; the first of them where two are as fast), anywhere else the first of them, as Platinum has it.
    /// </summary>
    private Battler RunAgainst()
    {
        var standing = EnemySlots.Where(b => b.IsActive).ToList();
        if (IsHorde && standing.Count > 1) return standing.OrderByDescending(b => b.Pokemon!.Speed).ThenBy(b => b.Slot).First();
        return standing.FirstOrDefault() ?? EnemySlots[0];
    }

    /// <summary>Why a ball can't be aimed while more than one wild Pokémon stands: two of them, or a horde's more.</summary>
    private static string NoAim(int standing) => standing > 2 ? Text.Of("battle.thereAreTooManyPokemon") : Text.Of("battle.thereAreTwoPokemon");

    // ---------------------------------------------------------------- triple battles (plan 14 · B6)

    /// <summary>
    /// A triple battle (plan 14 · B6; <see cref="BattleFormat.Triple"/>): three places a side, as Black and White have it,
    /// under rules that have it (<see cref="Data.Ruleset.Triples"/>). A move aimed at one Pokémon, a random foe's and a
    /// spread move reach only the places in reach of their user (<see cref="InReach(Battler, Battler)"/>), unless the move
    /// reaches anywhere (<see cref="MoveFlags.Distance"/>); a Pokémon at an end may spend its turn moving to the centre
    /// (<see cref="ChoiceKind.Shift"/>), and when each side is down to one and the two are out of each other's reach, both
    /// are moved to the centre at the turn's end (<see cref="MoveToTheCentre"/>).
    /// </summary>
    public bool IsTriple => Format == BattleFormat.Triple;

    /// <summary>The middle place of a triple battle's side, which every place of the other side can reach.</summary>
    public const int CentreSlot = 1;

    /// <summary>
    /// Whether a Pokémon in <paramref name="user"/>'s place reaches the one in <paramref name="target"/>'s with a move that
    /// reaches only those near it: the one place where reach is decided (plan 14 · B6). In a triple battle a place of the
    /// other side is in reach when it is across from the user's (slot 2 − the user's) or beside that one, and a place of
    /// the user's own side when it is beside the user's; in every other format everything is in reach.
    /// </summary>
    public static bool InReach(BattleFormat format, Place user, Place target)
    {
        if (format != BattleFormat.Triple) return true;
        if (user.Side == target.Side) return Math.Abs(user.Slot - target.Slot) <= 1;
        return Math.Abs(target.Slot - (2 - user.Slot)) <= 1;
    }

    /// <summary>Whether <paramref name="user"/> reaches <paramref name="target"/> in this battle (<see cref="InReach(BattleFormat, Place, Place)"/>).</summary>
    public bool InReach(Battler user, Battler target) => InReach(Format, user.Place, target.Place);

    /// <summary>Whether <paramref name="move"/>, used from <paramref name="user"/>'s place, gets to <paramref name="target"/>'s: always for a move that reaches anywhere.</summary>
    public bool GetsTo(Battler user, Battler target, MoveData move) => move.Flags.HasFlag(MoveFlags.Distance) || InReach(user, target);

    /// <summary>Whether a Pokémon in this place may shift to the centre: one at an end of its side in a triple battle.</summary>
    public bool CanShift(Battler b) => IsTriple && b.Slot != CentreSlot && b.IsActive;

    /// <summary>
    /// A shift (Black and White's, decision 4 of plan 14): the Pokémon at an end trades places with whoever stands in the
    /// centre, or moves into it when it is empty, as Ally Switch trades them (<see cref="Exchange"/>): everything that
    /// lasts on each goes with it, and a move still to come this turn aimed at a place finds whoever stands there now.
    /// </summary>
    private void ShiftToTheCentre(Battler user)
    {
        var centre = SlotsOf(user.Side)[CentreSlot];
        string name = user.Name;
        var (from, to) = (user.Place, centre.Place);
        Exchange(user, centre, AllBattlers);
        foreach (var act in waiting)
        {
            if (act.User == user) act.User = centre;
            else if (act.User == centre) act.User = user;
        }
        var side = Field.Side(user.Side);
        if (side.FollowMe == from) side.FollowMe = to;
        else if (side.FollowMe == to) side.FollowMe = from;
        Say(Text.Of("battle.shiftedToTheCentre", ("pokemon", name))).With(new PlacesSwapped(from, to));
    }

    /// <summary>
    /// When each side of a triple battle has one Pokémon left able to fight and the two can't reach each other, both
    /// move to the centre (Black and White's rule, as Pokémon Showdown's <c>checkFainted</c> in triples has it), so a
    /// battle can't stall out of reach.
    /// </summary>
    private void MoveToTheCentre()
    {
        if (!IsTriple || Result != BattleResult.None) return;
        if (AbleLeft(BattleSide.Player) != 1 || AbleLeft(BattleSide.Enemy) != 1) return;
        var mine = PlayerSlots.FirstOrDefault(b => b.IsActive);
        var theirs = EnemySlots.FirstOrDefault(b => b.IsActive);
        if (mine == null || theirs == null || InReach(mine, theirs)) return;
        var line = Say(Text.Of("battle.movedToTheCentre"));
        foreach (var b in new[] { mine, theirs })
        {
            if (b.Slot == CentreSlot) continue;
            var centre = SlotsOf(b.Side)[CentreSlot];
            var from = b.Place;
            Exchange(b, centre, AllBattlers);
            line.With(new PlacesSwapped(from, centre.Place));
        }
    }

    /// <summary>The Pokémon a side has left able to fight, on the field and in reserve (Showdown's <c>pokemonLeft</c>).</summary>
    private int AbleLeft(BattleSide side) =>
        SlotsOf(side).Where(b => b.Pokemon is { IsFainted: false }).Select(b => b.Pokemon!)
            .Concat(SlotsOf(side).Where(b => b.Roster != null).SelectMany(b => b.Roster!.Members).Where(p => !p.IsFainted))
            .Concat(side == BattleSide.Enemy && !IsTrainerBattle ? WildPokemon.Where(p => !p.IsFainted) : Enumerable.Empty<Pokemon>())
            .Distinct().Count();
}
