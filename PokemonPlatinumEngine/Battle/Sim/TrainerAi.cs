using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// How the opponents choose, and the player's partner in a tag battle (plan 06 · R9): Platinum's trainer AI,
/// written from the decompilation's <c>trainer_ai.c</c> and AI script. A trainer's Pokémon first asks whether to
/// switch or to use one of its trainer's items (<c>TrainerAI_PickCommand</c>), and otherwise scores its moves with
/// the routines its trainer thinks with (<see cref="AiThinking"/>) and uses the best, a tie broken by chance. In a
/// double battle it scores them against each of the other three on the field and aims at the best (<c>TrainerAI_MainDoubles</c>).
/// A wild Pokémon picks one of the moves it can use at random, as in the original; a roaming one thinks with the
/// roaming routine, which runs. After a faint the next Pokémon is the one the original's
/// <c>BattleAI_PostKOSwitchIn</c> picks. Every roll is the battle's own, so a battle against the AI replays.
/// </summary>
public sealed class TrainerAi : IBattleController
{
    public static TrainerAi Instance { get; } = new();

    public BattleChoice ChooseAction(BattleCore battle, Battler mine)
    {
        var memory = battle.AiMemory;
        memory.CatchUp(battle);
        if (FlagsOf(battle, mine) is not { } flags) return Wild(battle, mine);
        // The catching lesson goes as the original's screens drive it: one move, then the ball, whatever the HP left
        // (Task_PlayerShowMoveSelectMenu sets BattleSystem_SetCatchingTutorialLowHP once a move is chosen)
        if (battle.Kind == BattleKind.CatchingLesson && mine.IsPlayerSide && mine.Volatile.LastMove != null) return Escape(battle, mine);

        // TrainerAI_PickCommand: a trainer's Pokémon (or the player's partner) may switch or use an item instead
        if (battle.IsTrainerBattle || mine.IsPlayerSide)
        {
            if (ShouldSwitch(battle, mine)) return BattleChoice.Switch(mine.Place, SwitchTarget(battle, mine));
            if (ShouldUseItem(battle, mine) is { } item) return BattleChoice.UseItem(mine.Place, item);
        }
        return battle.IsDouble ? Doubles(battle, mine, flags) : Singles(battle, mine, flags);
    }

    public int ChooseReplacement(BattleCore battle, Battler place)
    {
        battle.AiMemory.CatchUp(battle);
        // The slot a switch was decided on, if it can still come in; otherwise as after a faint
        if (battle.AiMemory.SwitchedTo.TryGetValue(place.Place, out int slot) && slot is >= 0 and < 6 && battle.CanSendIn(place, slot))
        {
            battle.AiMemory.SwitchedTo.Remove(place.Place);
            return slot;
        }
        battle.AiMemory.SwitchedTo.Remove(place.Place);
        int picked = PostKoSwitchIn(battle, place);
        if (picked < 0) picked = Enumerable.Range(0, place.Roster?.Count ?? 0).FirstOrDefault(i => battle.CanSendIn(place, i), -1);
        return picked;
    }

    /// <summary>
    /// What a Pokémon thinks with: its trainer's flags (the tag strategy added in a double battle), the roaming
    /// routine for a roamer, the catching lesson's for its helper; null for a wild Pokémon, which doesn't think.
    /// </summary>
    internal static AiFlags? FlagsOf(BattleCore battle, Battler mine)
    {
        AiFlags flags;
        if (battle.Kind == BattleKind.CatchingLesson && mine.IsPlayerSide) flags = AiFlags.CatchTutorial;
        else if (mine.Trainer != null) flags = mine.Trainer.Ai;
        else if (battle.Kind == BattleKind.Roamer && !mine.IsPlayerSide) return AiFlags.Roaming;
        else return null;
        if (battle.IsDouble) flags |= AiFlags.TagStrategy;
        return flags;
    }

    // ================================================================ moves

    /// <summary>
    /// <c>TrainerAI_MainSingles</c>: the move of the highest score against the Pokémon across the field, a tie
    /// broken by chance; running if the script said so.
    /// </summary>
    private static BattleChoice Singles(BattleCore battle, Battler mine, AiFlags flags)
    {
        var defender = battle.ActiveFoes(mine).FirstOrDefault() ?? battle.SlotsOf(Other(mine.Side))[0];
        battle.AiMemory.RecordLastMove(defender);
        var look = new AiThinking(battle, battle.AiMemory, mine, defender, flags);
        look.Think();
        if (look.Escaped) return Escape(battle, mine);

        var moves = mine.Pokemon!.Moves;
        var best = new List<int> { 0 };
        for (int i = 1; i < moves.Count; i++)
        {
            if (look.Scores[i] == look.Scores[best[0]]) best.Add(i);
            else if (look.Scores[i] > look.Scores[best[0]]) best = new List<int> { i };
        }
        int slot = best[battle.Random.Roll(RollKind.AiChoice, best.Count)];
        return Usable(battle, mine, slot, look.Scores, defender.Place);
    }

    /// <summary>
    /// <c>TrainerAI_MainDoubles</c>: the moves scored against each of the other three on the field (a move on its
    /// partner counts only if it scored 100 or more), the best move against each, then the best target, ties
    /// broken by chance. Acupressure aimed at the player's side and a Curse that isn't a Ghost's are used on itself.
    /// </summary>
    private static BattleChoice Doubles(BattleCore battle, Battler mine, AiFlags flags)
    {
        var others = battle.AllBattlers.OrderBy(b => b.Place.Number).ToList();
        var bestOf = new Dictionary<Battler, (int Slot, int Score)>();
        int[]? lastScores = null;
        foreach (var target in others)
        {
            if (target == mine || target.Pokemon == null || target.Pokemon.CurrentHP == 0)
            {
                bestOf[target] = (-1, -1);
                continue;
            }
            if (target.Side != mine.Side) battle.AiMemory.RecordLastMove(target);
            var look = new AiThinking(battle, battle.AiMemory, mine, target, flags);
            look.Think();
            if (look.Escaped) return Escape(battle, mine);

            var moves = mine.Pokemon!.Moves;
            var best = new List<int> { 0 };
            for (int i = 1; i < moves.Count; i++)
            {
                if (look.Scores[i] == look.Scores[best[0]]) best.Add(i);
                else if (look.Scores[i] > look.Scores[best[0]]) best = new List<int> { i };
            }
            int slot = best[battle.Random.Roll(RollKind.AiChoice, best.Count)];
            int score = look.Scores[best[0]];
            // A move on its own partner is only worth it scored at 100 or more
            if (target.Side == mine.Side && score < 100) score = -1;
            bestOf[target] = (slot, score);
            lastScores = look.Scores;
        }

        int top = bestOf.Values.Max(v => v.Score);
        var tied = others.Where(b => bestOf[b].Score == top).ToList();
        var chosen = tied[battle.Random.Roll(RollKind.AiChoice, tied.Count)];
        int moveSlot = bestOf[chosen].Slot;
        if (moveSlot < 0) return BattleChoice.Fight(mine.Place, -1);

        var move = mine.Pokemon!.Moves[moveSlot].Data;
        var aim = chosen.Place;
        if (move.Target == MoveTarget.UserOrAlly && chosen.Side == BattleSide.Player) aim = mine.Place;
        if (move.Name == "Curse" && !mine.HasType(PokemonType.Ghost)) aim = mine.Place;
        return Usable(battle, mine, moveSlot, lastScores ?? new int[4], aim);
    }

    /// <summary>
    /// The chosen move as a choice. The original never refuses its own pick; this one can't hand the rules a move
    /// they forbid, so a move that can't be used gives way to the best that can, or to Struggle.
    /// </summary>
    private static BattleChoice Usable(BattleCore battle, Battler mine, int slot, int[] scores, Place? aim)
    {
        var moves = mine.Pokemon!.Moves;
        var usable = battle.UsableMoves(mine);
        if (usable.Count == 0) return BattleChoice.Fight(mine.Place, -1);
        if (slot >= moves.Count || !usable.Contains(moves[slot]))
            slot = Enumerable.Range(0, moves.Count).Where(i => usable.Contains(moves[i])).OrderByDescending(i => scores[i]).First();
        var target = moves[slot].Target == MoveTarget.Selected || moves[slot].Target == MoveTarget.UserOrAlly ? aim : null;
        return BattleChoice.Fight(mine.Place, slot, target);
    }

    /// <summary>The script said to run: a Pokémon of the other side flees; the catching lesson's helper stops attacking and throws its ball.</summary>
    private static BattleChoice Escape(BattleCore battle, Battler mine) =>
        mine.IsPlayerSide && battle.Kind == BattleKind.CatchingLesson ? BattleChoice.UseItem(mine.Place, "Poké Ball") : BattleChoice.Run(mine.Place);

    /// <summary>
    /// A wild Pokémon (<c>Task_TrainerShowMoveSelectMenu</c> outside a trainer battle): one of the moves it can use,
    /// each as likely, at a foe picked as the move's target would be.
    /// </summary>
    private static BattleChoice Wild(BattleCore battle, Battler mine)
    {
        var moves = mine.Pokemon!.Moves;
        var usable = battle.UsableMoves(mine);
        if (usable.Count == 0) return BattleChoice.Fight(mine.Place, -1);
        var move = usable[battle.Random.Roll(RollKind.AiChoice, usable.Count)];
        int slot = moves.IndexOf(move);
        Place? target = null;
        if (move.Target == MoveTarget.Selected && battle.IsDouble)
        {
            var foes = battle.ActiveFoes(mine).ToList();
            if (foes.Count > 0) target = foes[battle.Random.Roll(RollKind.AiChoice, foes.Count)].Place;
        }
        return BattleChoice.Fight(mine.Place, slot, target);
    }

    // ================================================================ switching (TrainerAI_ShouldSwitch)

    /// <summary>
    /// <c>TrainerAI_ShouldSwitch</c>: never while held in (bound, Mean Look, rooted, a foe's Shadow Tag or Arena
    /// Trap, a Steel type facing Magnet Pull) or with nobody to switch to; otherwise to get away from a doomed perish
    /// count, from a foe's Wonder Guard it can't touch, from having only moves that don't affect the foes, to a party
    /// member that absorbs the move that hit it, to cure sleep with Natural Cure, and, failing a super-effective move
    /// of its own or a heavy boost, to a party member immune or resistant to the move that hit it that has a
    /// super-effective move.
    /// </summary>
    private static bool ShouldSwitch(BattleCore battle, Battler mine)
    {
        var v = mine.Volatile;
        if (v.BindTurns > 0 || v.TrappedBy != null || v.Ingrained) return false;
        var foes = battle.SlotsOf(Other(mine.Side)).Where(b => b.Pokemon != null && !b.Pokemon.IsFainted).ToList();
        if (foes.Any(f => f.Ability?.Name is "Shadow Tag" or "Arena Trap")) return false;
        if (mine.HasType(PokemonType.Steel) && battle.AllBattlers.Any(b => b != mine && b.IsActive && b.Ability?.Name == "Magnet Pull")) return false;

        if (!Bench(battle, mine).Any()) return false;
        if (PerishSongKo(battle, mine)) return true;
        if (CannotDamageWonderGuard(battle, mine)) return true;
        if (OnlyIneffectiveMoves(battle, mine)) return true;
        if (HasAbsorbAbilityInParty(battle, mine)) return true;
        if (IsAsleepWithNaturalCure(battle, mine)) return true;
        if (HasSuperEffectiveMove(battle, mine, always: false, battle.Random)) return false;
        if (IsHeavilyStatBoosted(mine)) return false;
        if (PartyMemberWithSuperEffectiveMove(battle, mine, immune: true, odds: 2)) return true;
        if (PartyMemberWithSuperEffectiveMove(battle, mine, immune: false, odds: 3)) return true;
        return false;
    }

    /// <summary>The party slots a switch may go to: able to fight, not out, and not already picked by its partner.</summary>
    private static IEnumerable<int> Bench(BattleCore battle, Battler mine)
    {
        if (mine.Roster == null) return Enumerable.Empty<int>();
        var partner = battle.IsDouble ? battle.SlotsOf(mine.Side)[1 - mine.Slot] : null;
        int taken = partner != null && partner.Roster == mine.Roster && battle.AiMemory.SwitchedTo.TryGetValue(partner.Place, out int s) ? s : -1;
        return Enumerable.Range(0, mine.Roster.Count).Where(i => i != taken && battle.CanSendIn(mine, i));
    }

    /// <summary>The slot a decided switch goes to (6 in the original: as after a faint, else the first that can come in).</summary>
    private static int SwitchTarget(BattleCore battle, Battler mine)
    {
        int slot = battle.AiMemory.SwitchedTo.GetValueOrDefault(mine.Place, 6);
        if (slot == 6)
        {
            slot = PostKoSwitchIn(battle, mine);
            if (slot < 0) slot = Bench(battle, mine).FirstOrDefault(-1);
            battle.AiMemory.SwitchedTo[mine.Place] = slot;
        }
        return slot;
    }

    private static void Decide(BattleCore battle, Battler mine, int slot) => battle.AiMemory.SwitchedTo[mine.Place] = slot;

    /// <summary>
    /// <c>AI_PerishSongKO</c>: the perish count is at its last. (The count goes down at the turn's end, so the
    /// original never sees it at nothing and this never switches: kept so.)
    /// </summary>
    private static bool PerishSongKo(BattleCore battle, Battler mine)
    {
        if (mine.Volatile.PerishCount != 0) return false;
        Decide(battle, mine, 6);
        return true;
    }

    /// <summary><c>AI_CannotDamageWonderGuard</c> (single battles): no super-effective move against Wonder Guard, but a party member with one: two times in three.</summary>
    private static bool CannotDamageWonderGuard(BattleCore battle, Battler mine)
    {
        if (battle.IsDouble) return false;
        var foe = battle.SlotsOf(Other(mine.Side))[0];
        if (foe.Pokemon == null || foe.Pokemon.AbilityName != "Wonder Guard") return false;
        if (mine.Pokemon!.Moves.Any(m => SuperEffective(battle, mine, foe, m.Data))) return false;
        foreach (int i in Bench(battle, mine))
        {
            var p = mine.Roster!.Members[i];
            foreach (var m in p.Moves)
            {
                if (SuperEffectiveFrom(battle, p, m.Data, foe) && battle.Random.Roll(RollKind.AiChoice, 3) < 2)
                {
                    Decide(battle, mine, i);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// <c>AI_OnlyIneffectiveMoves</c>: every damaging move it has misses both foes for their types (and it has at
    /// least two): then a party member with a super-effective move against either, two times in three for each such
    /// move, or failing that one with a move that hits normally, half the time.
    /// </summary>
    private static bool OnlyIneffectiveMoves(BattleCore battle, Battler mine)
    {
        var foeSide = battle.SlotsOf(Other(mine.Side));
        var first = foeSide[0];
        var second = battle.IsDouble ? foeSide[1] : foeSide[0];
        int damaging = 0;
        foreach (var m in mine.Pokemon!.Moves)
        {
            if (AiThinking.PowerOf(m.Data) == 0) continue;
            damaging++;
            if (!Ineffective(battle, mine, first, m.Data)) return false;
            if (!Ineffective(battle, mine, second, m.Data)) return false;
        }
        if (damaging < 2) return false;

        foreach (int i in Bench(battle, mine))
        {
            var p = mine.Roster!.Members[i];
            foreach (var m in p.Moves.Where(m => AiThinking.PowerOf(m.Data) != 0))
            {
                if (first.Pokemon is { CurrentHP: > 0 } && SuperEffectiveFrom(battle, p, m.Data, first) && battle.Random.Roll(RollKind.AiChoice, 3) < 2)
                {
                    Decide(battle, mine, i);
                    return true;
                }
                if (second.Pokemon is { CurrentHP: > 0 } && SuperEffectiveFrom(battle, p, m.Data, second) && battle.Random.Roll(RollKind.AiChoice, 3) < 2)
                {
                    Decide(battle, mine, i);
                    return true;
                }
            }
        }
        foreach (int i in Bench(battle, mine))
        {
            var p = mine.Roster!.Members[i];
            foreach (var m in p.Moves.Where(m => AiThinking.PowerOf(m.Data) != 0))
            {
                if ((first.Pokemon is not { CurrentHP: > 0 } || PlainFrom(battle, p, m.Data, first)) && battle.Random.Roll(RollKind.AiChoice, 2) == 0)
                {
                    Decide(battle, mine, i);
                    return true;
                }
                if ((second.Pokemon is not { CurrentHP: > 0 } || PlainFrom(battle, p, m.Data, second)) && battle.Random.Roll(RollKind.AiChoice, 2) == 0)
                {
                    Decide(battle, mine, i);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// <c>AI_HasSuperEffectiveMove</c>: one of its moves hits the Pokémon across from it (and in a double battle that
    /// one's partner) super effectively, nine times in ten for each such move unless <paramref name="always"/>.
    /// </summary>
    internal static bool HasSuperEffectiveMove(BattleCore battle, Battler mine, bool always, Random rng)
    {
        var foeSide = battle.SlotsOf(Other(mine.Side));
        var across = foeSide[battle.IsDouble ? mine.Slot : 0];
        foreach (var target in battle.IsDouble ? new[] { across, foeSide[1 - mine.Slot] } : new[] { across })
        {
            if (target.Pokemon == null || target.Pokemon.IsFainted) continue;
            foreach (var m in mine.Pokemon!.Moves)
                if (SuperEffective(battle, mine, target, m.Data) && (always || rng.Roll(RollKind.AiChoice, 10) != 0)) return true;
        }
        return false;
    }

    /// <summary>
    /// <c>AI_HasAbsorbAbilityInParty</c>: hit by a Fire, Water or Electric move it doesn't absorb itself, a party
    /// member with Flash Fire, Water Absorb or Volt Absorb comes in half the time (each looked at in turn); not two
    /// times in three when it has a super-effective move of its own.
    /// </summary>
    private static bool HasAbsorbAbilityInParty(BattleCore battle, Battler mine)
    {
        if (HasSuperEffectiveMove(battle, mine, always: true, battle.Random) && battle.Random.Roll(RollKind.AiChoice, 3) != 0) return false;
        if (mine.Volatile.LastHitMove is not { } hit || AiThinking.PowerOf(hit) == 0) return false;
        string? absorbs = hit.Type switch { PokemonType.Fire => "Flash Fire", PokemonType.Water => "Water Absorb", PokemonType.Electric => "Volt Absorb", _ => null };
        if (absorbs == null || mine.Ability?.Name == absorbs) return false;
        foreach (int i in Bench(battle, mine))
        {
            if (mine.Roster!.Members[i].AbilityName == absorbs && battle.Random.Roll(RollKind.AiChoice, 2) == 1)
            {
                Decide(battle, mine, i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// <c>AI_IsAsleepWithNaturalCure</c>: asleep with Natural Cure and at half its HP or more: switching to wake up,
    /// half the time when nothing has hit it yet or a status move did, then to a party member immune to or resisting
    /// what hit it that has a super-effective move, then half the time anyway.
    /// </summary>
    private static bool IsAsleepWithNaturalCure(BattleCore battle, Battler mine)
    {
        var p = mine.Pokemon!;
        if (p.Status != StatusCondition.Sleep || mine.Ability?.Name != "Natural Cure" || p.CurrentHP < p.MaxHP / 2) return false;
        var hit = mine.Volatile.LastHitMove;
        if (hit == null && battle.Random.Roll(RollKind.AiChoice, 2) == 1)
        {
            Decide(battle, mine, 6);
            return true;
        }
        if (AiThinking.PowerOf(hit) == 0 && battle.Random.Roll(RollKind.AiChoice, 2) == 1)
        {
            Decide(battle, mine, 6);
            return true;
        }
        if (PartyMemberWithSuperEffectiveMove(battle, mine, immune: true, odds: 1)) return true;
        if (PartyMemberWithSuperEffectiveMove(battle, mine, immune: false, odds: 1)) return true;
        if (battle.Random.Roll(RollKind.AiChoice, 2) == 1)
        {
            Decide(battle, mine, 6);
            return true;
        }
        return false;
    }

    /// <summary><c>AI_IsHeavilyStatBoosted</c>: four or more stages raised, all told.</summary>
    private static bool IsHeavilyStatBoosted(Battler mine) => mine.Pokemon!.StatStages.Values.Where(s => s > 0).Sum() >= 4;

    /// <summary>
    /// <c>AI_HasPartyMemberWithSuperEffectiveMove</c>: a party member immune to (or, the second time, resisting) the
    /// last move that hit it, with a move super effective against whoever used it: one chance in <paramref name="odds"/>
    /// for each such move.
    /// </summary>
    private static bool PartyMemberWithSuperEffectiveMove(BattleCore battle, Battler mine, bool immune, int odds)
    {
        if (mine.Volatile.LastHitMove is not { } hit || mine.Volatile.LastHitMoveBy is not { } from) return false;
        if (AiThinking.PowerOf(hit) == 0) return false;
        var attacker = battle.At(from);
        if (attacker.Pokemon == null) return false;
        foreach (int i in Bench(battle, mine))
        {
            var p = mine.Roster!.Members[i];
            float against = Data.TypeChart.GetEffectiveness(hit.Type, p.PrimaryType, p.SecondaryType, battle.Rules);
            bool fits = immune ? against == 0f : against > 0f && against < 1f;
            if (!fits) continue;
            foreach (var m in p.Moves)
            {
                if (SuperEffectiveFrom(battle, p, m.Data, attacker) && battle.Random.Roll(RollKind.AiChoice, odds) == 0)
                {
                    Decide(battle, mine, i);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>A move of a Pokémon on the field against another, by the type chart the AI reads (<c>BattleSystem_ApplyTypeChart</c>).</summary>
    private static bool SuperEffective(BattleCore battle, Battler user, Battler target, MoveData move)
    {
        var look = new AiThinking(battle, battle.AiMemory, user, target, AiFlags.None, initialScores: false);
        look.TypeChart(user, target, move, look.AiMoveType(user, move), Eff.Neutral, out bool immune, out bool super, out _);
        return super && !immune;
    }

    /// <summary>A move doesn't affect a Pokémon on the field (an empty place counts as not affected).</summary>
    private static bool Ineffective(BattleCore battle, Battler user, Battler target, MoveData move)
    {
        if (target.Pokemon is not { CurrentHP: > 0 }) return true;
        var look = new AiThinking(battle, battle.AiMemory, user, target, AiFlags.None, initialScores: false);
        look.TypeChart(user, target, move, look.AiMoveType(user, move), Eff.Neutral, out bool immune, out _, out _);
        return immune;
    }

    /// <summary>A party member's move against a Pokémon on the field (<c>BattleSystem_CalcEffectiveness</c>): super effective.</summary>
    private static bool SuperEffectiveFrom(BattleCore battle, Pokemon p, MoveData move, Battler target) =>
        BenchEffect(battle, p, move, target) is var (immune, super, _) && super && !immune;

    /// <summary>A party member's move hits a Pokémon on the field neither better nor worse than normally.</summary>
    private static bool PlainFrom(BattleCore battle, Pokemon p, MoveData move, Battler target) =>
        BenchEffect(battle, p, move, target) is var (immune, super, weak) && !immune && !super && !weak;

    private static (bool Immune, bool Super, bool Weak) BenchEffect(BattleCore battle, Pokemon p, MoveData move, Battler target)
    {
        if (target.Pokemon == null || move == BattleCore.StruggleData) return (false, false, false);
        var stand = new Battler(Other(target.Side), 0) { Pokemon = p, Field = target.Field };
        var look = new AiThinking(battle, battle.AiMemory, stand, target, AiFlags.None, initialScores: false);
        // BattleSystem_CalcEffectiveness: a Pokémon on the bench has no STAB or items to count, only the matchup
        string? ability = p.AbilityName;
        string? theirs = ability == "Mold Breaker" ? null : target.Ability?.Name;
        var type = ability == "Normalize" ? PokemonType.Normal : look.AiMoveType(stand, move);
        if (theirs == "Levitate" && type == PokemonType.Ground && !battle.Field.Gravity && BattleEffects.HoldEffectOf(target) != "SpeedDownGrounded")
            return (true, false, false);
        bool super = false, weak = false, immune = false;
        foreach (var t in target.Types.Distinct())
        {
            float multiplier = Data.TypeChart.GetEffectiveness(type, t, null, battle.Rules);
            if (multiplier == 0f)
            {
                if (t == PokemonType.Flying && type == PokemonType.Ground && (battle.Field.Gravity || BattleEffects.HoldEffectOf(target) == "SpeedDownGrounded")) continue;
                if (t == PokemonType.Ghost && ability == "Scrappy") continue;
                immune = true;
            }
            else if (multiplier > 1f) { if (weak) weak = false; else super = true; }
            else if (multiplier < 1f) { if (super) super = false; else weak = true; }
        }
        if (theirs == "Wonder Guard" && !super) immune = true;
        return (immune, super, weak);
    }

    // ================================================================ after a faint (BattleAI_PostKOSwitchIn)

    /// <summary>
    /// <c>BattleAI_PostKOSwitchIn</c>: against a foe picked as the original picks one, the party member whose types
    /// hit the foe's best (counting a single type twice) that also has a super-effective move; if none has, the one
    /// whose best move would hit hardest. −1 when nobody can come in. The original keeps its scores in bytes, so
    /// a high one wraps round: kept so.
    /// </summary>
    internal static int PostKoSwitchIn(BattleCore battle, Battler place)
    {
        if (place.Roster == null) return -1;
        var foes = battle.SlotsOf(Other(place.Side));
        var defender = battle.IsDouble ? RandomOpponent(battle, place) : foes[0];
        if (defender.Pokemon == null) return -1;
        var party = place.Roster.Members;
        var candidates = Enumerable.Range(0, party.Count).Where(i => battle.CanSendIn(place, i) && Taken(battle, place) != i).ToList();
        var disregarded = new HashSet<int>();

        while (true)
        {
            int maxScore = 0, picked = -1;
            foreach (int i in candidates.Where(i => !disregarded.Contains(i)))
            {
                var p = party[i];
                var d1 = defender.Types[0];
                var d2 = defender.Types[^1];
                byte score = (byte)(Matchup(p.PrimaryType, d1, d2, battle.Rules) + Matchup(p.SecondaryType ?? p.PrimaryType, d1, d2, battle.Rules));
                if (maxScore < score)
                {
                    maxScore = score;
                    picked = i;
                }
            }
            if (picked < 0) break;
            if (party[picked].Moves.Any(m => SuperEffectiveFrom(battle, party[picked], m.Data, defender))) return picked;
            disregarded.Add(picked);
        }

        // By the damage its moves would do, as if used by the one that fainted
        int best = 0, pick = -1;
        foreach (int i in candidates)
        {
            var p = party[i];
            var stand = new Battler(place.Side, place.Slot) { Pokemon = p, Field = place.Field };
            var look = new AiThinking(battle, battle.AiMemory, place.Pokemon == null ? stand : place, defender, AiFlags.None, initialScores: false);
            foreach (var m in p.Moves)
            {
                byte score = 0;
                if (AiThinking.PowerOf(m.Data) != 1) score = (byte)look.DamageOf(place.Pokemon == null ? stand : place, m.Data, 100);
                if (best < score)
                {
                    best = score;
                    pick = i;
                }
            }
        }
        return pick;
    }

    /// <summary><c>BattleSystem_TypeMatchupMultiplier</c>: 40 put through the chart for one attacking type against two.</summary>
    private static int Matchup(PokemonType attacking, PokemonType d1, PokemonType d2, Ruleset rules)
    {
        int mul = 40;
        mul = mul * (int)MathF.Round(Data.TypeChart.GetEffectiveness(attacking, d1, null, rules) * 10f) / 10;
        if (d2 != d1) mul = mul * (int)MathF.Round(Data.TypeChart.GetEffectiveness(attacking, d2, null, rules) * 10f) / 10;
        return mul;
    }

    private static int Taken(BattleCore battle, Battler place)
    {
        var partner = battle.IsDouble ? battle.SlotsOf(place.Side)[1 - place.Slot] : null;
        return partner != null && partner.Roster == place.Roster && battle.AiMemory.SwitchedTo.TryGetValue(partner.Place, out int s) ? s : -1;
    }

    /// <summary><c>BattleSystem_RandomOpponent</c>: in a double battle one of the two foes by a coin, the other if that one is down.</summary>
    internal static Battler RandomOpponent(BattleCore battle, Battler attacker)
    {
        var foes = battle.SlotsOf(Other(attacker.Side));
        if (!battle.IsDouble) return foes[0];
        // The original's right-hand and left-hand foe, seen from the attacker's side
        var options = attacker.IsPlayerSide ? new[] { foes[1], foes[0] } : new[] { foes[0], foes[1] };
        int coin = battle.Random.Roll(RollKind.AiChoice, 2);
        var chosen = options[coin];
        return chosen.Pokemon is { CurrentHP: > 0 } ? chosen : options[coin ^ 1];
    }

    // ================================================================ items (TrainerAI_ShouldUseItem)

    /// <summary>
    /// <c>TrainerAI_ShouldUseItem</c>: the trainer's items looked at in order (the second only when two or fewer of
    /// its Pokémon are left, and so on): a Full Restore below a quarter of its HP, a medicine that heals HP below a
    /// quarter or when its whole amount would be used, one that cures a condition the Pokémon has, and after its
    /// first turn an X item or a Guard Spec. Never under an Embargo, and never the player's partner against a
    /// trainer. As in the original, once one item is picked every later item that is looked at goes too (the loop
    /// doesn't stop), and the last of them is the one used: kept so.
    /// </summary>
    private static string? ShouldUseItem(BattleCore battle, Battler mine)
    {
        if (mine.IsPlayerSide && battle.IsTrainerBattle) return null;
        if (mine.Volatile.EmbargoTurns > 0) return null;
        if (mine.Trainer == null) return null;
        var items = battle.AiMemory.ItemsOf(mine.Trainer);
        int alive = mine.Roster?.Members.Count(p => !p.IsFainted) ?? 1;
        int count = mine.Trainer.Items.Count;
        var p = mine.Pokemon!;
        string? used = null;
        bool result = false;

        for (int i = 0; i < items.Count; i++)
        {
            if (i != 0 && alive > count - i + 1) continue;
            if (items[i] is not { } name || ItemDatabase.Get(name) is not { } item) continue;
            int Use(string key) => item.Use?.GetValueOrDefault(key) ?? 0;

            if (name == "Full Restore")
            {
                if (p.CurrentHP < p.MaxHP / 4 && p.CurrentHP > 0) result = true;
            }
            else if (item.Use?.ContainsKey("hpRestored") == true)
            {
                // The amount is a byte in the original's table: "all" and "half" read as 255 and 254
                int amount = (byte)Use("hpRestored");
                if (amount != 0 && p.CurrentHP > 0 && (p.CurrentHP < p.MaxHP / 4 || p.MaxHP - p.CurrentHP > amount)) result = true;
            }
            else if (Use("healSleep") != 0) { if (p.Status == StatusCondition.Sleep) result = true; }
            else if (Use("healPoison") != 0) { if (p.Status is StatusCondition.Poison or StatusCondition.Toxic) result = true; }
            else if (Use("healBurn") != 0) { if (p.Status == StatusCondition.Burn) result = true; }
            else if (Use("healFreeze") != 0) { if (p.Status == StatusCondition.Freeze) result = true; }
            else if (Use("healParalysis") != 0) { if (p.Status == StatusCondition.Paralyze) result = true; }
            else if (Use("healConfusion") != 0) { if (mine.IsConfused) result = true; }
            else if (mine.Volatile.FirstTurn - battle.Turn >= 0)
            {
                // Only on its first turn out: an X item, or a Guard Spec. with no Mist up (Dire Hit is none of these)
                if (Use("atkStages") != 0 || Use("defStages") != 0 || Use("spAtkStages") != 0 || Use("spDefStages") != 0 || Use("speedStages") != 0 || Use("accStages") != 0)
                    result = true;
                else if (Use("guardSpec") != 0 && !battle.Field.Side(mine.Side).Mist) result = true;
            }

            if (result)
            {
                used = name;
                items[i] = null;
            }
        }
        return used;
    }

    private static BattleSide Other(BattleSide side) => side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
}
