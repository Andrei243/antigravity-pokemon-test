using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// What may be chosen (the original's BattleSystem_CheckInvalidMoves, BattleSystem_CanUseMove, Battler_IsTrapped and
// Battler_IsTrappedMsg). A menu asks WhyNot before it hands a choice over; Submit refuses what it would refuse.
public sealed partial class BattleCore
{
    /// <summary>The move a choice names: one of its own by position, Struggle, or the move it is held to.</summary>
    private Move MoveFor(Battler user, int index)
    {
        var moves = user.Pokemon!.Moves;
        if (index == BattleChoice.HeldMove)
        {
            var held = user.Volatile.LockedMove ?? StruggleData;
            return moves.FirstOrDefault(m => m.Data == held) ?? new Move(held);
        }
        return index >= 0 && index < moves.Count ? moves[index] : new Move(StruggleData);
    }

    /// <summary>
    /// The moves a Pokémon can pick this turn. Whoever asks may hand over the screen's battler for a place: the
    /// answer is about the Pokémon the rules have there. None left means Struggle.
    /// </summary>
    public List<Move> UsableMoves(Battler b)
    {
        var mine = At(b.Place);
        return mine.Pokemon!.Moves.Where(m => WhyNotMove(mine, m) == null).ToList();
    }

    /// <summary>
    /// Why a move can't be picked, in the original's order of reasons: disabled, the same move twice under a
    /// Torment, a taunt, sealed, Gravity, kept from healing, an Encore, a Choice item, no PP. Null when it can.
    /// </summary>
    public string? WhyNotMove(Battler b, Move move, bool ignorePp = false)
    {
        var mine = At(b.Place);
        var v = mine.Volatile;
        var data = move.Data;
        if (v.Disabled == data) return Text.Of("battle.isDisabled", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (v.Tormented && v.LastMove == data) return Text.Of("battle.cantUseTheSame", ("pokemon", mine.Name));
        // The original keeps back the moves of power 0 in its table: the status moves (a move of variable power is 1 there)
        if (v.TauntTurns > 0 && move.Category == MoveCategory.Status) return Text.Of("battle.cantUseAfterThe", ("pokemon", mine.Name), ("move", move.DisplayName));
        // The later moves that keep a move back (plan 06 · R25): Throat Chop a sound move, and Belch until a berry is eaten
        if (ThroatChopped(mine, data)) return Text.Of("battle.cantUseBecauseOfThroatChop", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (data.Effect == "Belch" && !ateBerry.Contains(mine.Pokemon!)) return Text.Of("battle.isDisabled", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (IsSealed(mine, data)) return Text.Of("battle.cantUseTheSealed", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (Field.Gravity && FailsUnderGravity(data)) return Text.Of("battle.cantUseBecauseOf", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (v.HealBlockTurns > 0 && IsHealingMove(data)) return Text.Of("battle.cantUseWhileIt", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (v.Encored != null && v.Encored != data && mine.Pokemon!.Moves.Any(m => m.Data == v.Encored))
            return Text.Of("battle.canOnlyUseAfter", ("pokemon", mine.Name), ("move", v.Encored.DisplayName));
        if (mine.ChoiceLock != null && mine.ChoiceLock != move && mine.Pokemon!.Moves.Contains(mine.ChoiceLock) && BattleEffects.Of(mine).Any(e => e.LocksMoveChoice))
            return Text.Of("battle.canOnlyUse", ("pokemon", mine.Name), ("move", mine.ChoiceLock.DisplayName));
        if (!ignorePp && move.CurrentPP <= 0) return Text.Of("battle.thereNoPpLeft");
        return null;
    }

    /// <summary>Why a choice can't be made; null when it can. A Pokémon held to a move isn't asked, so it has no choices to refuse.</summary>
    public string? WhyNot(BattleChoice choice)
    {
        var b = At(choice.Who);
        if (!b.IsActive) return Text.Of("battle.thereIsNoPokemon");
        switch (choice.Kind)
        {
            case ChoiceKind.Fight:
                if (Kind is BattleKind.Safari or BattleKind.PalPark) return Text.Of("battle.thereNoPokemonOf");
                if (choice.Move == BattleChoice.HeldMove) return b.IsHeldToItsMove ? null : Text.Of("battle.isntInTheMiddle", ("pokemon", b.Name));
                var moves = b.Pokemon!.Moves;
                if (choice.Move < 0 || choice.Move >= moves.Count)
                    return UsableMoves(b).Count == 0 ? null : Text.Of("battle.stillHasMovesIt", ("pokemon", b.Name));
                return WhyNotMove(b, moves[choice.Move]);

            case ChoiceKind.Switch:
                if (Kind is BattleKind.Safari or BattleKind.PalPark) return Text.Of("battle.thereNoPokemonOfYours");
                if (!CanSendIn(b, choice.SwitchTo)) return Text.Of("battle.thatPokemonCantBe");
                // At Battle Style Shift's offer nothing holds it: the original opens the team with nothing barred
                // (BtlCmd_ChoosePokemonMenu), and whatever bound it fell with the foe
                if (Request is SwitchOfferRequest offered && offered.Place == choice.Who) return null;
                return TrapOn(b, forRunning: false);

            case ChoiceKind.Stay:
                return Request is SwitchOfferRequest offer && offer.Place == choice.Who ? null : Text.Of("battle.nothingIsBeingOffered");

            case ChoiceKind.Run:
                // In the Battle Frontier RUN gives the match up, whatever holds the Pokémon
                if (Frontier) return null;
                if (CannotFlee && b.IsPlayerSide) return Text.Of("battle.thereNoRunningFrom");
                if (Kind is BattleKind.Safari or BattleKind.PalPark) return null;
                return IsTrainerBattle ? null : TrapOn(b, forRunning: true);

            case ChoiceKind.Bait or ChoiceKind.Mud:
                return Kind == BattleKind.Safari ? null : Text.Of("battle.thereNothingToThrow");

            case ChoiceKind.Item:
                return WhyNotItem(b, choice);
        }
        return null;
    }

    /// <summary>
    /// What keeps a Pokémon from leaving, as a line; null when nothing does. A Shed Shell lets its holder switch
    /// whatever holds it; a Smoke Ball and Run Away let theirs run. Otherwise: a binding move, Mean Look and its
    /// like, its own roots, and a foe's Shadow Tag, Arena Trap (for those on the ground) or Magnet Pull (for
    /// Steel types).
    /// </summary>
    private string? TrapOn(Battler b, bool forRunning)
    {
        var effects = BattleEffects.Of(b).ToList();
        if (forRunning ? effects.Any(e => e.AlwaysEscapes) : effects.Any(e => e.SlipsAway)) return null;
        if (Rules.GhostTypesCantBeTrapped && b.HasType(PokemonType.Ghost)) return null;

        bool onTheGround = IsOnTheGround(b);
        foreach (var foe in ActiveFoes(b))
        {
            if (foe.Ability?.Effect is { } trap && trap.Traps(foe, b, onTheGround))
                return Text.Of("battle.preventsEscapeWith", ("foe", foe.Name), ("ability", foe.Ability.DisplayName));
        }
        var v = b.Volatile;
        // Fairy Lock holds everyone for the turn after it (plan 06 · R25)
        if (v.BindTurns > 0 || v.TrappedBy != null || v.Ingrained || Turn <= fairyLockUntil)
            return forRunning ? Text.Of("battle.cantEscape") : Text.Of("battle.cantBeCalledBack", ("pokemon", b.Name));
        return null;
    }

    /// <summary>Whether a Pokémon may be switched out by choice this turn.</summary>
    public bool CanSwitchOut(Battler b) => TrapOn(At(b.Place), forRunning: false) == null;

    /// <summary>
    /// On the ground, as Spikes, Toxic Spikes and Arena Trap ask (<c>BattlerIsGrounded</c>): not a Flying type,
    /// not floating by Levitate or Magnet Rise; or weighed down by an Iron Ball or by Gravity all the same.
    /// </summary>
    private static bool IsOnTheGround(Battler b) => DamageCalculator.IsGrounded(b);
}
