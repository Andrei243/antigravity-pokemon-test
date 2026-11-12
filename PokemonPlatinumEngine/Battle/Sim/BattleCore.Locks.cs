using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// Locked hearts in battle (plan 44 · SD1; <see cref="HeartLock"/>; <c>docs/mechanics/rulings.md</c>, "Locked hearts").
/// A trainer of Lockwork's sends out Pokémon with locks; with the Reclaimer (<see cref="CoreSetup.Reclaimer"/>) a ball
/// may be thrown at one, and caught it leaves the trainer's team (<see cref="Reclaimed"/>), the trainer sending the
/// next, reclaiming the last winning the battle. A locked Pokémon of the player's holds its EXP, has its sealed moves
/// refused, and may break loose at its action, heeding one order in two and losing a sixteenth of its HP at the turn's
/// end until it is calmed (<see cref="ChoiceKind.Calm"/>). Everything here asks for a lock first and rolls only where
/// there is one, under rules with locked hearts (<see cref="Ruleset.LockedHearts"/>): a battle with no lock draws and
/// says exactly what it did.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>The player carries the Reclaimer (<see cref="CoreSetup.Reclaimer"/>).</summary>
    public bool Reclaimer { get; }

    private readonly List<Pokemon> reclaimed = new();

    /// <summary>The trainers' locked Pokémon caught this battle, the rules' own copies, in the order they were caught.</summary>
    public IReadOnlyList<Pokemon> Reclaimed => reclaimed;

    /// <summary>Locked hearts are part of these rules.</summary>
    private bool LocksInPlay => Rules.LockedHearts;

    /// <summary>A Pokémon of the player's own (never a partner's) whose lock the rules read.</summary>
    private bool LockedOfMine(Battler b) =>
        LocksInPlay && b.IsPlayerSide && b.Trainer == null && b.Pokemon?.Lock != null && !Frontier;

    /// <summary>
    /// Whether a ball may be thrown at a trainer's Pokémon: only with the Reclaimer, only at a locked one, only where it
    /// has one target. Null when it may, the line otherwise.
    /// </summary>
    private string? WhyNotReclaim()
    {
        if (!LocksInPlay || !Reclaimer || Frontier || Link || IsRoyal || !IsSingle) return Text.Of("battle.theTrainerBlockedThe");
        var target = EnemySlots.FirstOrDefault(b => b.IsActive);
        if (target?.Pokemon?.Lock == null) return Text.Of("battle.theTrainerBlockedThe");
        return null;
    }

    /// <summary>
    /// A trainer's locked Pokémon caught (plan 44 · SD1): "Gotcha!", the EXP a faint would have given, and it leaves the
    /// trainer's team and the field. The trainer sends the next as after a faint, at the turn's end; reclaiming the last
    /// wins the battle and its prize. It joins neither the team nor the PC here: the game takes it in once the battle is
    /// over, so the rules' team and the screen's stay the same all battle.
    /// </summary>
    private void Reclaim(Battler target, ItemData ball)
    {
        var foe = target.Pokemon!;
        foe.ResetStatStages();
        foe.Ball = ball.Name;
        CatchCalculator.AfterTheCatch(foe, ball);
        Emit(new Caught(foe, ball.Name, ToBox: false));
        Say(Text.Of("battle.reclaimed", ("foe", foe.DisplayName)));
        ExpForTheCatch(target);
        Withdraw(target);
        Emit(new Left(target.Place));
        target.Roster?.Remove(foe);
        target.Pokemon = null;
        reclaimed.Add(foe);
        // The trainer's last: the battle is won, prize and all
        CheckBattleEnd();
    }

    /// <summary>
    /// What a locked Pokémon of the player's does at its action, after obedience: it may break loose (likelier on the
    /// turn after a critical or super-effective hit), and a loose one heeds the order one time in two. True when the
    /// action is lost, as <see cref="Disobeys"/>'s.
    /// </summary>
    private bool Unruly(Battler user, ref Move move, ref Battler? target)
    {
        if (!LockedOfMine(user)) return false;
        var v = user.Volatile;
        if (!v.Loose)
        {
            bool stirred = v.StirredOnTurn > 0 && v.StirredOnTurn == Turn;
            int chance = stirred ? LooseChanceStirred : LooseChance;
            if (rng.Roll(RollKind.Lock, 100) >= chance) return false;
            v.Loose = true;
            Say(Text.Of("battle.brokeLoose", ("user", user.Name))).With(new BrokeLoose(user.Place));
        }
        // Loose: one order in two
        if (rng.Roll(RollKind.Lock, 2) == 0) return false;
        Say(Text.Of("battle.looseIgnores", ("user", user.Name)));
        return true;
    }

    /// <summary>A locked Pokémon's chance of breaking loose at its action, out of 100.</summary>
    public const int LooseChance = 6;

    /// <summary>The same on the turn after a critical or super-effective hit.</summary>
    public const int LooseChanceStirred = 30;

    /// <summary>A locked Pokémon of the player's struck critically or super-effectively is likelier to break loose next turn. Rolls nothing.</summary>
    private void Stir(Battler t, bool critical, bool superEffective)
    {
        if (!LockedOfMine(t) || !(critical || superEffective)) return;
        t.Volatile.StirredOnTurn = Turn + 1;
    }

    /// <summary>CALM (plan 44 · SD1): a loose Pokémon is itself again, and its lock is lowered by the calm.</summary>
    private void CalmDown(Battler b)
    {
        var p = b.Pokemon!;
        b.Volatile.Loose = false;
        if (p.Lock is { } locked) p.Lock = locked.Lower(HeartLock.Fall.Calm);
        Say(Text.Of("battle.calmedDown", ("pokemon", b.Name))).With(new Calmed(b.Place, p.Lock?.BarsLeft ?? 0));
    }

    /// <summary>Why CALM can't be chosen: nobody here is loose.</summary>
    private string? WhyNotCalm(Battler b) =>
        LockedOfMine(b) && b.Volatile.Loose ? null : Text.Of("battle.nothingToCalm", ("pokemon", b.Name));

    /// <summary>At the turn's end a loose Pokémon loses a sixteenth of its HP (no roll; nothing where nobody is loose).</summary>
    private void LooseBites()
    {
        foreach (var b in speedOrder)
        {
            if (!b.IsActive || !b.Volatile.Loose || !LockedOfMine(b)) continue;
            LoseHp(b, Formulas.Divide(b.Pokemon!.MaxHP, 16), Text.Of("battle.looseHurts", ("pokemon", b.Name)), direct: true);
        }
        if (Result == BattleResult.None) ResolveFaints();
    }

    /// <summary>
    /// A locked Pokémon of the player's holds what it earns (plan 44 · SD1): true, with the line and the
    /// <see cref="ExpHeld"/>, where its lock takes the EXP; false for any other.
    /// </summary>
    private bool HoldsTheExp(Pokemon p, int exp)
    {
        if (!LocksInPlay || p.Lock is not { } locked || Frontier) return false;
        if (!PlayerParty.Members.Contains(p)) return false;
        p.Lock = locked.Hold(exp);
        Say(Text.Of("battle.expHeld", ("pokemon", p.DisplayName), ("exp", exp))).With(new ExpHeld(p, exp));
        return true;
    }

    private readonly Dictionary<Pokemon, int> lockKnockOuts = new();

    /// <summary>The foes each locked Pokémon of the player's was facing when they fell, for its gauge (plan 44 · SD1).</summary>
    public IReadOnlyDictionary<Pokemon, int> LockKnockOuts => lockKnockOuts;

    /// <summary>A foe fell: each locked Pokémon of the player's facing it counts it. Rolls nothing.</summary>
    private void CountLockKnockOut(Battler down)
    {
        if (!LocksInPlay || down.IsPlayerSide) return;
        foreach (var mine in PlayerSlots.Where(b => b.IsActive && LockedOfMine(b)))
            lockKnockOuts[mine.Pokemon!] = lockKnockOuts.GetValueOrDefault(mine.Pokemon!) + 1;
    }

    /// <summary>Whether a move of a locked Pokémon of the player's is still sealed: the line, or null.</summary>
    private string? SealedByTheLock(Battler b, Move move)
    {
        if (!LockedOfMine(b)) return null;
        int index = b.Pokemon!.Moves.IndexOf(move);
        return index >= 0 && b.Pokemon.Lock!.Seals(index) ? Text.Of("battle.moveSealedByLock", ("pokemon", b.Name), ("move", move.DisplayName)) : null;
    }
}
