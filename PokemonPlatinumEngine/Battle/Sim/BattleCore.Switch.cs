using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Coming onto the field and leaving it: a switch (with Pursuit catching whoever leaves), what lies in wait for the
// one that comes in (Toxic Spikes, Spikes, Stealth Rock), what acts on entry, being dragged out by Roar, leaving by
// one's own move (U-turn, Baton Pass, Healing Wish), and the replacement of whoever fainted.
public sealed partial class BattleCore
{
    /// <summary>Puts a Pokémon in a place on the field.</summary>
    private void SendIn(Battler place, Pokemon pokemon)
    {
        place.Pokemon = pokemon;
        OnEntered(place);
    }

    private void OnEntered(Battler place)
    {
        place.ClearVolatile();
        place.Pokemon!.ResetStatStages();
        place.Pokemon.ToxicCounter = 0;
        place.Volatile.EnteredOnTurn = Turn;
        place.FoughtAgainst.Clear();
        foreach (var foe in EnemySlots.Where(b => b.IsActive))
            foreach (var mine in PlayerSlots.Where(b => b.IsActive)) foe.FoughtAgainst.Add(mine.Pokemon!);
    }

    /// <summary>
    /// What a Pokémon takes with it from the others when it leaves or faints (<c>BattleSystem_UpdateAfterSwitch</c>,
    /// <c>BattleSystem_CleanupFaintedMon</c>): whoever it bound is free and whoever loved it no longer does; and,
    /// unless it passes the baton, whoever it trapped or took aim at is let go.
    /// </summary>
    private void LetGoOf(Battler leaving, bool passesTheBaton = false)
    {
        foreach (var other in AllBattlers.Where(b => b != leaving))
        {
            var v = other.Volatile;
            if (v.BoundBy == leaving.Place)
            {
                v.BoundBy = null;
                v.BindTurns = 0;
            }
            if (v.InLoveWith == leaving.Place) v.InLoveWith = null;
            if (passesTheBaton) continue;
            if (v.TrappedBy == leaving.Place) v.TrappedBy = null;
            if (v.LockedOnBy == leaving.Place)
            {
                v.LockedOnBy = null;
                v.LockOnTurns = 0;
            }
        }
    }

    /// <summary>What Baton Pass carries over, taken from a place as its Pokémon leaves it.</summary>
    private sealed record Baton(Volatiles State, Dictionary<StatType, int> Stages, int ConfusionTurns);

    private Baton? Withdraw(Battler place, bool passesTheBaton = false)
    {
        foreach (var e in BattleEffects.Of(place)) e.OnWithdraw(place);
        LetGoOf(place, passesTheBaton);
        var baton = passesTheBaton
            ? new Baton(place.Volatile.Passed(), new Dictionary<StatType, int>(place.Pokemon!.StatStages), place.ConfusionTurns)
            : null;
        ComeBack(place);
        place.ClearVolatile();
        place.Pokemon!.ResetStatStages();
        place.Pokemon.ToxicCounter = 0;
        return baton;
    }

    private void TakeUp(Battler place, Baton baton)
    {
        place.Volatile = baton.State;
        place.Volatile.EnteredOnTurn = Turn;
        place.ConfusionTurns = baton.ConfusionTurns;
        foreach (var (stat, stage) in baton.Stages) place.Pokemon!.StatStages[stat] = stage;
        if (place.HasSubstitute) Emit(new SubstituteChanged(place.Place, Up: true));
    }

    /// <summary>Abilities that act on entry (Intimidate, Drizzle), fastest first.</summary>
    private void RunEntryEffects(List<Battler> entered)
    {
        foreach (var b in BySpeed().Where(entered.Contains))
        {
            if (!b.IsActive) continue;
            foreach (var e in BattleEffects.Of(b)) e.OnEntry(this, b);
            CheckConditionHooks(b, null);
        }
    }

    private void ExecuteSwitch(Battler place, int partyIndex)
    {
        if (!CanSendIn(partyIndex)) return;
        var outgoing = place.Pokemon!;
        var incoming = PlayerParty.Members[partyIndex];
        var back = Say($"Come back, {outgoing.DisplayName}!");

        // A foe that chose Pursuit catches it on its way out
        if (!Pursue(place)) back.With(new Recalled(place.Place));
        else if (Result != BattleResult.None) return;
        else if (place.IsActive) Emit(new Recalled(place.Place));

        // Knocked out before it could leave: the Pokémon chosen comes in all the same
        if (!place.IsActive) ResolveFaints();
        if (Result != BattleResult.None) return;
        if (place.Pokemon == outgoing && place.IsActive) Withdraw(place);
        Emit(new Left(place.Place));
        Arrive(place, incoming, $"Go! {incoming.DisplayName}!");
    }

    /// <summary>A Pokémon takes a place in the middle of things: its line, what lies in wait for it, then what it does on entry.</summary>
    private void Arrive(Battler place, Pokemon incoming, string line, Baton? baton = null, Action? beforeEntryEffects = null)
    {
        SendIn(place, incoming);
        if (baton != null) TakeUp(place, baton);
        var said = Say(line);
        if (!place.IsPlayerSide) said.With(new Seen(incoming.Species));
        said.With(new Entered(place.Place, incoming, FromBall: true));
        Hazards(place);
        if (place.IsActive) beforeEntryEffects?.Invoke();
        ResolveFaints();
        if (Result == BattleResult.None) RunEntryEffects(new List<Battler> { place });
    }

    // ---------------------------------------------------------------- Pursuit

    /// <summary>
    /// <c>BtlCmd_TryPursuit</c>: every foe still to act this turn that chose Pursuit (and isn't asleep or frozen)
    /// uses it now, at double power, on the Pokémon that is leaving. It costs its PP and is that foe's action for
    /// the turn. Returns whether anyone did.
    /// </summary>
    private bool Pursue(Battler leaving)
    {
        bool any = false;
        foreach (var act in waiting.ToList())
        {
            if (Result != BattleResult.None || !leaving.IsActive) break;
            var foe = act.User;
            if (act.Choice.Kind != ChoiceKind.Fight || foe.Side == leaving.Side || !foe.IsActive || foe.Pokemon != act.Actor) continue;
            if (foe.Pokemon!.Status is StatusCondition.Sleep or StatusCondition.Freeze) continue;

            var move = act.Move!;
            if (foe.Volatile.Encored != null && foe.Pokemon.Moves.FirstOrDefault(m => m.Data == foe.Volatile.Encored) is { } encored) move = encored;
            if (move.Data.Effect != "HitBeforeSwitch" || move.CurrentPP <= 0) continue;

            waiting.Remove(act);
            any = true;
            move.CurrentPP -= Math.Min(move.CurrentPP, 1 + BattleEffects.Of(leaving).Sum(e => e.ExtraPpUsed));
            foe.MovedThisTurn = true;
            foe.Volatile.LastMove = move.Data;
            if (BattleEffects.Of(foe).Any(e => e.LocksMoveChoice)) foe.ChoiceLock = move;

            var use = new MoveUse { User = foe, Move = move, Effect = EffectOf(move.Data), Boost = 20 };
            use.Targets.Add(leaving);
            use.Line = Say($"{foe.Name} used {move.Name}!");
            leaving.Turn.SubstituteHit = false;
            Strike(use);
        }
        return any;
    }

    // ---------------------------------------------------------------- what lies in wait

    /// <summary>
    /// <c>subscript_hazards_check</c>: Toxic Spikes and Spikes for a Pokémon on the ground, then Stealth Rock for
    /// anyone. Magic Guard walks through all three. A Poison type on the ground takes the Toxic Spikes away.
    /// </summary>
    private void Hazards(Battler b)
    {
        if (!b.IsActive) return;
        var side = Field.Side(b.Side);
        if (BattleEffects.Of(b).Any(e => e.PreventsIndirectDamage)) return;

        if (IsOnTheGround(b))
        {
            if (side.ToxicSpikes > 0)
            {
                if (b.HasType(PokemonType.Poison))
                {
                    side.ToxicSpikes = 0;
                    Say($"The poison spikes disappeared from around {TeamOf(b.Side)}'s feet!");
                }
                else if (!side.Safeguard)
                {
                    TryInflictStatus(b, side.ToxicSpikes >= 2 ? StatusCondition.Toxic : StatusCondition.Poison, null, false, By.Other);
                }
            }
            if (side.Spikes > 0 && b.IsActive)
                LoseHp(b, Formulas.Divide(b.Pokemon!.MaxHP, (5 - side.Spikes) * 2), $"{b.Name} is hurt by the spikes!");
        }

        if (side.StealthRock && b.IsActive)
        {
            // By how the Rock type does against its types, with nothing else counted: 1/8 at an even matchup
            var mon = b.Pokemon!;
            float matchup = TypeChart.GetEffectiveness(PokemonType.Rock, mon.PrimaryType, mon.SecondaryType, Rules);
            int divisor = matchup switch { >= 4f => 2, >= 2f => 4, >= 1f => 8, >= 0.5f => 16, > 0f => 32, _ => 0 };
            if (divisor > 0) LoseHp(b, Formulas.Divide(mon.MaxHP, divisor), $"Pointed stones dug into {b.Name}!");
        }
    }

    // ---------------------------------------------------------------- made to leave, and leaving by a move

    /// <summary>Someone of the place's party could come in for the Pokémon standing there.</summary>
    private bool HasReplacement(Battler place) =>
        place.IsPlayerSide ? Enumerable.Range(0, PlayerParty.Count).Any(CanSendIn) : NextFromRoster(place, EnemySlots.Select(b => b.Pokemon)) != null;

    /// <summary>
    /// Roar and Whirlwind's doing in a trainer battle (<c>BtlCmd_TryWhirlwind</c>): one of the target's party that
    /// can fight and isn't out, picked by chance, takes its place.
    /// </summary>
    private void DragOut(Battler target)
    {
        var bench = target.IsPlayerSide
            ? Enumerable.Range(0, PlayerParty.Count).Where(CanSendIn).Select(i => PlayerParty.Members[i]).ToList()
            : target.Roster!.Members.Where(p => !p.IsFainted && EnemySlots.All(b => b.Pokemon != p)).ToList();
        if (bench.Count == 0) return;
        var dragged = bench[rng.Roll(RollKind.DraggedOut, bench.Count)];

        Withdraw(target);
        Emit(new Recalled(target.Place));
        Emit(new Left(target.Place));
        Arrive(target, dragged, $"{(target.IsPlayerSide ? dragged.DisplayName : "Foe " + dragged.DisplayName)} was dragged out!");
    }

    /// <summary>The party member that takes a place, asked of whoever chooses for its side; null when nobody can.</summary>
    private Pokemon? picked;

    private IEnumerable<BattleRequest> PickReplacement(Battler place)
    {
        picked = null;
        if (!place.IsPlayerSide)
        {
            picked = NextFromRoster(place, EnemySlots.Select(b => b.Pokemon));
            yield break;
        }
        if (!Enumerable.Range(0, PlayerParty.Count).Any(CanSendIn)) yield break;

        int index;
        if (controllers[(int)BattleSide.Player] is { } controller) index = controller.ChooseReplacement(this, place);
        else
        {
            yield return new ReplacementRequest(place.Place);
            index = answer[0].SwitchTo;
        }
        if (CanSendIn(index)) picked = PlayerParty.Members[index];
    }

    private string SentOutLine(Battler place, Pokemon incoming) =>
        place.IsPlayerSide ? $"Go! {incoming.DisplayName}!" : $"{place.Trainer!.FullTitle} sent out {incoming.DisplayName}!";

    /// <summary>
    /// A Pokémon leaves by its own move once the move is done: U-turn brings it back (Pursuit can catch it), Baton
    /// Pass hands its state on, and Healing Wish and Lunar Dance, having cost it everything, make whoever comes
    /// next whole. The replacement is picked there and then, in the middle of the turn.
    /// </summary>
    private IEnumerable<BattleRequest> Leave(MoveUse use)
    {
        if (use.Leaves == Leaving.No || Result != BattleResult.None) yield break;
        var place = use.User;
        bool wished = use.Leaves is Leaving.MakesAWish or Leaving.DancesAway;
        if (!wished && (!place.IsActive || !HasReplacement(place))) yield break;

        if (use.Leaves == Leaving.Returns)
        {
            Say($"{place.Name} went back to {(place.IsPlayerSide ? playerName : place.Trainer!.Name)}!");
            if (Pursue(place))
            {
                if (Result != BattleResult.None) yield break;
                if (!place.IsActive)
                {
                    ResolveFaints();
                    yield break;
                }
            }
        }

        foreach (var request in PickReplacement(place)) yield return request;
        if (picked == null) yield break;
        var incoming = picked;

        Baton? baton = null;
        if (place.IsActive)
        {
            baton = Withdraw(place, use.Leaves == Leaving.PassesTheBaton);
            Emit(new Recalled(place.Place));
            Emit(new Left(place.Place));
        }

        Arrive(place, incoming, SentOutLine(place, incoming), baton, beforeEntryEffects: !wished ? null : () =>
        {
            var p = place.Pokemon!;
            p.Status = StatusCondition.None;
            p.SleepTurns = 0;
            p.ToxicCounter = 0;
            if (use.Leaves == Leaving.DancesAway) foreach (var m in p.Moves) m.RestorePP();
            Emit(new StatusChanged(place.Place, StatusCondition.None));
            if (p.CurrentHP < p.MaxHP)
            {
                p.CurrentHP = p.MaxHP;
                Say(use.Leaves == Leaving.DancesAway ? $"{place.Name} became cloaked in mystical moonlight!" : $"The healing wish came true for {place.Name}!")
                    .With(new HpChanged(place.Place, p.CurrentHP, Healed: true));
            }
            else Say(use.Leaves == Leaving.DancesAway ? $"{place.Name} became cloaked in mystical moonlight!" : $"The healing wish came true for {place.Name}!");
        });
    }

    // ---------------------------------------------------------------- after a faint

    /// <summary>Fills the places of fainted Pokémon from the bench: trainers send theirs, the player picks. Whoever comes in meets what lies in wait, so this goes round until nobody is missing.</summary>
    private IEnumerable<BattleRequest> ReplaceFainted()
    {
        for (int round = 0; round < 12 && Result == BattleResult.None; round++)
        {
            var entered = new List<Battler>();

            // The foes first
            foreach (var place in EnemySlots.Where(b => !b.IsActive && b.Roster != null))
            {
                var next = NextFromRoster(place, EnemySlots.Select(b => b.Pokemon));
                if (next == null) continue;
                entered.Add(place);
                SendIn(place, next);
                Say($"{place.Trainer!.FullTitle} sent out {next.DisplayName}!")
                    .With(new Seen(next.Species)).With(new Entered(place.Place, next, FromBall: true));
            }

            // Then each empty place of the player's, while there is anyone left to send
            foreach (var place in PlayerSlots.Where(b => !b.IsActive))
            {
                foreach (var request in PickReplacement(place)) yield return request;
                if (picked == null) break;
                var chosen = picked;
                entered.Add(place);
                SendIn(place, chosen);
                Say($"Go! {chosen.DisplayName}!").With(new Entered(place.Place, chosen, FromBall: true));
            }

            if (entered.Count == 0) yield break;
            speedOrder = BySpeed();
            foreach (var place in speedOrder.Where(entered.Contains)) Hazards(place);
            ResolveFaints();
            if (Result != BattleResult.None) yield break;
            RunEntryEffects(entered);
            ResolveFaints();
        }
    }

    /// <summary>The next Pokémon of a place's roster that can fight and isn't already out.</summary>
    private static Pokemon? NextFromRoster(Battler place, IEnumerable<Pokemon?> exclude)
    {
        var used = exclude.Where(p => p != null).ToHashSet();
        return place.Roster?.Members.FirstOrDefault(p => !p.IsFainted && !used.Contains(p));
    }
}
