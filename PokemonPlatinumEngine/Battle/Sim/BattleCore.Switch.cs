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

    /// <param name="opening">It is one of the first out, so the battle's first turn is its first: afterwards a Pokémon's first turn is the one after it came in.</param>
    private void OnEntered(Battler place, bool opening = false)
    {
        place.ClearVolatile();
        place.Pokemon!.ResetStatStages();
        place.Pokemon.ToxicCounter = 0;
        CountFrom(place, opening);
        place.FoughtAgainst.Clear();
        foreach (var foe in EnemySlots.Where(b => b.IsActive))
            foreach (var mine in PlayerSlots.Where(b => b.IsActive)) foe.FoughtAgainst.Add(mine.Pokemon!);
        // The player's own that took part, for what a battle leaves on them (Burmy's cloak)
        if (place.IsPlayerSide && place.Trainer == null) sentOut.Add(place.Pokemon);
    }

    /// <summary>
    /// The turns a Pokémon's time on the field counts from (the original's <c>fakeOutTurnNumber</c>,
    /// <c>slowStartTurnNumber</c> and <c>truant</c>, set as it comes in): Fake Out works on its first turn alone,
    /// Slow Start counts five from it and Truant acts on it; and whether it came in holding an item, for Unburden.
    /// </summary>
    private void CountFrom(Battler place, bool opening = false)
    {
        var v = place.Volatile;
        v.EnteredOnTurn = Turn;
        v.FirstTurn = opening ? Turn : Turn + 1;
        v.SlowStartTurn = v.FirstTurn;
        v.TruantParity = v.FirstTurn & 1;
        v.CanUnburden = place.Pokemon?.HeldItem != null;
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
        Revert(place);
        place.ClearVolatile();
        place.Pokemon!.ResetStatStages();
        place.Pokemon.ToxicCounter = 0;
        return baton;
    }

    /// <summary>
    /// Puts back what a move changed for the battle (plan 06 · R5), as the original does by rebuilding a Pokémon's
    /// battle data from its party data whenever it comes in: its species and shape (Transform), its stats (Power
    /// Trick, Transform), its ability (Role Play, Skill Swap, Worry Seed, Transform) and its moves (Mimic,
    /// Transform). Sketch is for good and is left alone; a knocked-off item comes back only with the battle's end.
    /// </summary>
    private static void Revert(Battler place)
    {
        if (place.Volatile.Original is not { } o || place.Pokemon is not { } p) return;
        if (o.Species != null)
        {
            p.Species = o.Species;
            p.Form = o.Form;
        }
        if (o.NicknameGiven) p.Nickname = o.Nickname!;
        if (o.StatsKept)
        {
            p.Attack = o.Attack;
            p.Defense = o.Defense;
            p.SpAttack = o.SpAttack;
            p.SpDefense = o.SpDefense;
            p.Speed = o.Speed;
        }
        if (o.AbilityKept) p.AbilityName = o.AbilityName;
        if (o.Moves != null)
        {
            p.Moves.Clear();
            foreach (var (data, pp) in o.Moves) p.Moves.Add(new Move(data, pp));
        }
        else if (o.MoveSlots != null)
        {
            foreach (var (slot, (data, pp)) in o.MoveSlots)
                if (slot < p.Moves.Count) p.Moves[slot] = new Move(data, pp);
        }
        place.Volatile.Original = null;
    }

    /// <summary>What a move is about to change for the battle is kept the first time, so <see cref="Revert"/> can put it back.</summary>
    private static Original Keep(Battler place) => place.Volatile.Original ??= new Original();

    private void TakeUp(Battler place, Baton baton)
    {
        place.Volatile = baton.State;
        CountFrom(place);
        place.ConfusionTurns = baton.ConfusionTurns;
        foreach (var (stat, stage) in baton.Stages) place.Pokemon!.StatStages[stat] = stage;
        if (place.HasSubstitute) Emit(new SubstituteChanged(place.Place, Up: true));
    }

    /// <summary>The phases of the original's switch-in check, in its order, and the abilities each is for.</summary>
    private static readonly (EntryCheck Flag, string[] Abilities)[] EntryPhases =
    {
        (EntryCheck.Trace, new[] { "Trace" }),
        (EntryCheck.WeatherAbility, new[] { "Drizzle", "Sand Stream", "Drought", "Snow Warning" }),
        (EntryCheck.Intimidate, new[] { "Intimidate" }),
        (EntryCheck.Download, new[] { "Download" }),
        (EntryCheck.Anticipation, new[] { "Anticipation" }),
        (EntryCheck.Forewarn, new[] { "Forewarn" }),
        (EntryCheck.Frisk, new[] { "Frisk" }),
        (EntryCheck.SlowStart, new[] { "Slow Start" }),
        (EntryCheck.MoldBreaker, new[] { "Mold Breaker" }),
        (EntryCheck.Pressure, new[] { "Pressure" })
    };

    /// <summary>
    /// <c>BattleSystem_TriggerEffectOnSwitch</c>: what acts on entry, phase by phase (Trace, the weather
    /// abilities, Intimidate, Download, Anticipation, Forewarn, Frisk, Slow Start, Mold Breaker, Pressure, the
    /// shape the weather or an item gives, the held item), over everyone from the fastest, each once per stay on
    /// the field (<see cref="Volatiles.Announced"/>). The original runs it as the Pokémon come in, after every
    /// move and at the start of every turn, so an ability gained by a Transform acts then, and Slow Start's five
    /// turns end at a turn's start.
    /// </summary>
    private void SwitchInChecks()
    {
        if (Result != BattleResult.None) return;
        // In the Great Marsh and Pal Park nobody is sent out: no ability has anyone to act on
        if (Kind is BattleKind.Safari or BattleKind.PalPark) return;
        var order = BySpeed();
        foreach (var (flag, abilities) in EntryPhases)
        {
            foreach (var b in order)
            {
                if (!b.IsActive || b.Ability is not { } ability || !abilities.Contains(ability.Name)) continue;
                var v = b.Volatile;
                if (flag == EntryCheck.SlowStart)
                {
                    // Said once as it comes in; and once more in the turn its five turns are up
                    if ((v.Announced & flag) == 0 && Turn <= v.SlowStartTurn)
                    {
                        v.Announced |= flag;
                        ability.Effect?.OnEntry(this, b);
                    }
                    if (!v.SlowStartEnded && Turn - v.SlowStartTurn == 5)
                    {
                        v.SlowStartEnded = true;
                        Say($"{b.Name} finally got going!");
                    }
                    continue;
                }
                if ((v.Announced & flag) != 0) continue;
                // Trace waits for a foe it can take an ability from
                if (flag == EntryCheck.Trace && (b.Pokemon!.HeldItem?.Name == "Griseous Orb" || Trace.Candidates(this, b).Count == 0)) continue;
                v.Announced |= flag;
                ability.Effect?.OnEntry(this, b);
                if (Result != BattleResult.None) return;
            }
        }

        // An Amulet Coin or Luck Incense on the field doubles the prize (SWITCH_IN_CHECK_STATE_AMULET_COIN)
        CheckAmuletCoin();
        CheckShapes(order);

        foreach (var b in order)
        {
            if (!b.IsActive) continue;
            var v = b.Volatile;
            // An ability of a later generation that acts on entry has no phase of its own and goes here
            if ((v.Announced & EntryCheck.Other) == 0 && b.Ability is { } ability && !EntryPhases.Any(p => p.Abilities.Contains(ability.Name)))
            {
                v.Announced |= EntryCheck.Other;
                ability.Effect?.OnEntry(this, b);
            }
            if ((v.Announced & EntryCheck.Item) == 0)
            {
                v.Announced |= EntryCheck.Item;
                BattleEffects.ItemOf(b)?.OnEntry(this, b);
                CheckConditionHooks(b, null);
            }
            if (Result != BattleResult.None) return;
        }
    }

    /// <summary>
    /// <c>BattleSystem_TriggerFormChange</c>: Castform (with Forecast), Cherrim (whatever its ability) and Arceus
    /// (with Multitype) take the shape the weather or their item gives them, fastest first.
    /// </summary>
    private void CheckShapes(IEnumerable<Battler> order)
    {
        foreach (var b in order)
        {
            if (!b.IsActive) continue;
            string? line = b.Pokemon!.Species.Name == "Cherrim" ? Shapes.Cherrim(this, b) : b.Ability?.Effect?.ChangeShape(this, b);
            if (line != null) Say(line).With(new Reshaped(b.Place));
        }
    }

    private void ExecuteSwitch(Battler place, int partyIndex)
    {
        if (!CanSendIn(place, partyIndex)) return;
        var outgoing = place.Pokemon!;
        var incoming = place.Roster!.Members[partyIndex];
        var back = Say(place.Trainer == null ? $"Come back, {outgoing.DisplayName}!" : $"{place.Trainer.FullTitle} withdrew {outgoing.DisplayName}!");

        // A foe that chose Pursuit catches it on its way out
        if (!Pursue(place)) back.With(new Recalled(place.Place));
        else if (Result != BattleResult.None) return;
        else if (place.IsActive) Emit(new Recalled(place.Place));

        // Knocked out before it could leave: the Pokémon chosen comes in all the same
        if (!place.IsActive) ResolveFaints();
        if (Result != BattleResult.None) return;
        if (place.Pokemon == outgoing && place.IsActive) Withdraw(place);
        Emit(new Left(place.Place));
        Arrive(place, incoming, SentOutLine(place, incoming));
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
        SwitchInChecks();
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
            if (foe.Pokemon!.Status is StatusCondition.Sleep or StatusCondition.Freeze || Loafs(foe)) continue;

            var move = act.Move!;
            if (foe.Volatile.Encored != null && foe.Pokemon.Moves.FirstOrDefault(m => m.Data == foe.Volatile.Encored) is { } encored) move = encored;
            if (move.Data.Effect != "HitBeforeSwitch" || move.CurrentPP <= 0) continue;

            waiting.Remove(act);
            any = true;
            move.CurrentPP -= Math.Min(move.CurrentPP, 1 + BattleEffects.Of(leaving).Sum(e => e.ExtraPpUsed));
            foe.MovedThisTurn = true;
            foe.Volatile.LastMove = move.Data;
            if (BattleEffects.Of(foe).Any(e => e.LocksMoveChoice)) foe.ChoiceLock = move;

            var use = new MoveUse { User = foe, Move = Normalized(foe, move), Own = move.Data, Effect = EffectOf(move.Data), Boost = 20 };
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
    private bool HasReplacement(Battler place) => place.Roster != null && Enumerable.Range(0, place.Roster.Count).Any(i => CanSendIn(place, i));

    /// <summary>
    /// Roar and Whirlwind's doing in a trainer battle (<c>BtlCmd_TryWhirlwind</c>): one of the target's party that
    /// can fight and isn't out, picked by chance, takes its place.
    /// </summary>
    private void DragOut(Battler target)
    {
        var bench = target.Roster == null ? new List<Pokemon>()
            : Enumerable.Range(0, target.Roster.Count).Where(i => CanSendIn(target, i)).Select(i => target.Roster.Members[i]).ToList();
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
        if (!HasReplacement(place)) yield break;

        int index;
        if (ControllerOf(place) is { } controller) index = controller.ChooseReplacement(this, place);
        else
        {
            yield return new ReplacementRequest(place.Place);
            index = answer[0].SwitchTo;
        }
        if (CanSendIn(place, index)) picked = place.Roster!.Members[index];
        // Whoever chooses must name someone who can come: the next able one otherwise
        else picked = NextFromRoster(place, SlotsOf(place.Side).Select(b => b.Pokemon));
    }

    private string SentOutLine(Battler place, Pokemon incoming) =>
        place.Trainer == null ? $"Go! {incoming.DisplayName}!" : $"{place.Trainer.FullTitle} sent out {incoming.DisplayName}!";

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

            // The foes first, each trainer's Pokémon picked as the AI picks after a faint
            foreach (var place in EnemySlots.Where(b => !b.IsActive && b.Roster != null))
            {
                foreach (var request in PickReplacement(place)) yield return request;
                if (picked == null) continue;
                var next = picked;
                entered.Add(place);
                SendIn(place, next);
                Say(SentOutLine(place, next)).With(new Seen(next.Species)).With(new Entered(place.Place, next, FromBall: true));
            }

            // Then each empty place of the player's side (the partner's too), while there is anyone left to send
            foreach (var place in PlayerSlots.Where(b => !b.IsActive))
            {
                foreach (var request in PickReplacement(place)) yield return request;
                if (picked == null) continue;
                var chosen = picked;
                entered.Add(place);
                SendIn(place, chosen);
                Say(SentOutLine(place, chosen)).With(new Entered(place.Place, chosen, FromBall: true));
            }

            if (entered.Count == 0) yield break;
            speedOrder = BySpeed();
            foreach (var place in speedOrder.Where(entered.Contains)) Hazards(place);
            ResolveFaints();
            if (Result != BattleResult.None) yield break;
            SwitchInChecks();
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
