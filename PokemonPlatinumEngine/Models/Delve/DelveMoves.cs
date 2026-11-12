using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>Where a move reaches on a floor.</summary>
public enum DelveReach
{
    /// <summary>The tile ahead (a move at one Pokémon).</summary>
    Ahead,
    /// <summary>The eight tiles round (a move at every foe).</summary>
    Round,
    /// <summary>Every foe within <see cref="DelveMoves.RoomReach"/> (a move at everyone else on the field).</summary>
    Room,
    /// <summary>The user itself.</summary>
    Self,
    /// <summary>The user and its teammates round it.</summary>
    Team,
    /// <summary>A teammate ahead.</summary>
    Ally,
    /// <summary>Nothing on a floor: a side's, the foes' side's or the field's.</summary>
    None
}

/// <summary>
/// A move on a delve's floor (plan 44 · SD7; <c>docs/mechanics/rulings.md</c>, "The delve"): it reaches by its
/// <see cref="MoveTarget"/> (<see cref="ReachOf"/>), hits by <see cref="Formulas.HitRate"/>, damages by
/// <see cref="DamageCalculator"/> under the modern rules on the run's generator, with every ability left out and nobody
/// holding anything, and does what its data's fields say: a condition, confusion, a stat's stages, healing, draining,
/// recoil, a flinch as a lost action. A move whose doing is a battle's is refused (<see cref="WhyNot"/>). The plain strike
/// costs no PP and goes through <see cref="Formulas.ModernBaseDamage"/> and <see cref="Formulas.ModernFinalDamage"/> at
/// an effectiveness of one, so no type is immune to it. No drawing or input.
/// </summary>
public static class DelveMoves
{
    /// <summary>The plain strike's power.</summary>
    public const int StrikePower = 40;

    /// <summary>How far a move at the room reaches.</summary>
    public const int RoomReach = 3;

    /// <summary>Where a move reaches by its target.</summary>
    public static DelveReach ReachOf(MoveData move) => move.Target switch
    {
        MoveTarget.Selected or MoveTarget.RandomFoe => DelveReach.Ahead,
        MoveTarget.AllFoes => DelveReach.Round,
        MoveTarget.AllOthers => DelveReach.Room,
        MoveTarget.User or MoveTarget.UserOrAlly => DelveReach.Self,
        MoveTarget.UserAndAllies => DelveReach.Team,
        MoveTarget.Ally or MoveTarget.Allies => DelveReach.Ally,
        _ => DelveReach.None
    };

    /// <summary>
    /// Whether a move does anything on a floor: a damaging one with a power of its own, or a status move whose fields say
    /// what it does (a condition, confusion, a stat's stages, healing). Everything else is a battle's (a switch, a
    /// weather, the field, a power the battle works out, an effect of its own that only the battle's rules carry out).
    /// </summary>
    public static bool WorksOnAFloor(MoveData move)
    {
        if (ReachOf(move) == DelveReach.None) return false;
        if (move.Category != MoveCategory.Status) return move.Power > 0;
        return move.InflictStatus != StatusCondition.None || move.ConfuseChancePercent > 0 || move.HealPercent > 0
               || (move.TargetStatChange != null && move.StatStageAmount != 0);
    }

    /// <summary>Why a Pokémon may not use a move now, or null.</summary>
    public static DelveRefusal? WhyNot(DelveMon user, int move)
    {
        if (move < 0 || move >= user.Pokemon.Moves.Count) return DelveRefusal.NoMove;
        var m = user.Pokemon.Moves[move];
        if (m.CurrentPP <= 0) return DelveRefusal.NoPP;
        return WorksOnAFloor(m.Data) ? null : DelveRefusal.NeedsABattle;
    }

    /// <summary>Who a move reaches, from its user facing a way.</summary>
    public static List<DelveMon> TargetsOf(DelveRun run, DelveMon user, MoveData move, Direction way)
    {
        var (ax, ay) = DelveTurns.Ahead(user.X, user.Y, way);
        bool Foe(DelveMon m) => m.OnTeam != user.OnTeam;
        var everyone = run.Team.Concat(run.Residents).Where(m => !m.Fainted && m != user).OrderBy(m => m.Order);
        return ReachOf(move) switch
        {
            DelveReach.Ahead => run.At(ax, ay) is { } t && Foe(t) ? new List<DelveMon> { t } : new List<DelveMon>(),
            DelveReach.Round => everyone.Where(m => Foe(m) && user.Reach(m.X, m.Y) == 1).ToList(),
            DelveReach.Room => everyone.Where(m => Foe(m) && user.Reach(m.X, m.Y) <= RoomReach).ToList(),
            DelveReach.Self => new List<DelveMon> { user },
            DelveReach.Team => new[] { user }.Concat(everyone.Where(m => !Foe(m) && user.Reach(m.X, m.Y) == 1)).ToList(),
            DelveReach.Ally => run.At(ax, ay) is { } a && !Foe(a) ? new List<DelveMon> { a } : new List<DelveMon>(),
            _ => new List<DelveMon>()
        };
    }

    /// <summary>A move used facing a way (its PP paid whether anyone is there or not).</summary>
    public static void Use(DelveRun run, DelveMon user, int index, Direction way)
    {
        var move = user.Pokemon.Moves[index];
        move.CurrentPP--;
        user.Facing = way;
        run.Say(DelveEventKind.UsedMove, user.Name, what: move.Data.Name);
        var targets = TargetsOf(run, user, move.Data, way);
        if (targets.Count == 0)
        {
            run.Say(DelveEventKind.Missed, user.Name, what: move.Data.Name);
            return;
        }
        foreach (var target in targets)
        {
            if (user.Fainted || run.Outcome != DelveOutcome.Going) return;
            if (target.Fainted) continue;
            Wake(run, target);
            bool foe = target.OnTeam != user.OnTeam;
            if (foe && !Hits(run, user, target, move.Data.Accuracy))
            {
                run.Say(DelveEventKind.Missed, user.Name, target.Name, move.Data.Name);
                continue;
            }
            if (move.Data.Category == MoveCategory.Status) Status(run, user, target, move.Data);
            else Damaging(run, user, target, move);
        }
    }

    /// <summary>Whether a move of that accuracy lands: always with none, else a draw of a hundred under the hit rate by the two's stages.</summary>
    private static bool Hits(DelveRun run, DelveMon user, DelveMon target, int accuracy)
    {
        if (accuracy <= 0) return true;
        int rate = Formulas.HitRate(accuracy, user.Pokemon.StatStages.GetValueOrDefault(StatType.Accuracy),
            target.Pokemon.StatStages.GetValueOrDefault(StatType.Evasion));
        return run.Rng.Next(100) < rate;
    }

    /// <summary>
    /// The damage of a move on a floor: <see cref="DamageCalculator.Calculate"/> under the modern rules between two
    /// battlers that stand on no field and whose abilities are left out, on the generator handed. On the same draws,
    /// two Pokémon whose abilities and items play no part come to what <see cref="DamageCalculator.CalculateDamage"/> gives.
    /// </summary>
    public static DamageCalculator.DamageResult Damage(Pokemon attacker, Pokemon defender, Move move, Random rng)
    {
        var a = new Battler(BattleSide.Player, 0) { Pokemon = attacker };
        var d = new Battler(BattleSide.Enemy, 0) { Pokemon = defender };
        a.Volatile.AbilitySuppressed = true;
        d.Volatile.AbilitySuppressed = true;
        return DamageCalculator.Calculate(a, d, move, rng, spread: false, rules: Ruleset.Modern);
    }

    private static void Damaging(DelveRun run, DelveMon user, DelveMon target, Move move)
    {
        var data = move.Data;
        var result = Damage(user.Pokemon, target.Pokemon, move, run.Rng);
        if (result.IsImmune)
        {
            run.Say(DelveEventKind.Missed, user.Name, target.Name, data.Name);
            return;
        }
        int dealt = Hurt(run, user, target, result.Damage, data.Name);
        if (data.DrainPercent > 0 && dealt > 0 && !user.Fainted)
        {
            int back = Heal(user, Math.Max(1, dealt * data.DrainPercent / 100));
            run.Say(DelveEventKind.Drained, user.Name, target.Name, data.Name, back);
        }
        if (data.RecoilPercent > 0 && dealt > 0 && !user.Fainted)
        {
            int recoil = Math.Max(1, dealt * data.RecoilPercent / 100);
            user.Pokemon.CurrentHP = Math.Max(0, user.Pokemon.CurrentHP - recoil);
            run.Say(DelveEventKind.Recoil, user.Name, what: data.Name, amount: recoil);
            if (user.Fainted) Faint(run, null, user);
        }
        if (target.Fainted) return;
        // What its fields say it may also do, each by its own chance
        if (data.InflictStatus != StatusCondition.None && data.StatusChancePercent > 0 && run.Rng.Next(100) < data.StatusChancePercent)
            GiveStatus(run, user, target, data.InflictStatus);
        if (data.ConfuseChancePercent > 0 && run.Rng.Next(100) < data.ConfuseChancePercent) Confuse(run, target);
        if (data.FlinchChancePercent > 0 && run.Rng.Next(100) < data.FlinchChancePercent)
        {
            target.Flinched = true;
            run.Say(DelveEventKind.Flinched, target.Name);
        }
        if (data.TargetStatChange != null && data.StatStageAmount != 0 && data.StatChangeChancePercent > 0
            && run.Rng.Next(100) < data.StatChangeChancePercent)
            ChangeStats(run, data.StatChangeTargetSelf ? user : target, data);
    }

    private static void Status(DelveRun run, DelveMon user, DelveMon target, MoveData data)
    {
        bool did = false;
        if (data.InflictStatus != StatusCondition.None) did |= GiveStatus(run, user, target, data.InflictStatus);
        if (data.ConfuseChancePercent > 0) { Confuse(run, target); did = true; }
        if (data.HealPercent > 0)
        {
            int healed = Heal(target, Math.Max(1, target.Pokemon.MaxHP * data.HealPercent / 100));
            run.Say(DelveEventKind.Healed, target.Name, what: data.Name, amount: healed);
            did = true;
        }
        if (data.TargetStatChange != null && data.StatStageAmount != 0)
        {
            ChangeStats(run, data.StatChangeTargetSelf && target.OnTeam != user.OnTeam ? user : target, data);
            did = true;
        }
        if (!did) run.Say(DelveEventKind.StatusFailed, user.Name, target.Name, data.Name);
    }

    /// <summary>The plain strike at the tile ahead: no PP, a power of <see cref="StrikePower"/>, an effectiveness of one.</summary>
    public static void Strike(DelveRun run, DelveMon user, Direction way)
    {
        var (x, y) = DelveTurns.Ahead(user.X, user.Y, way);
        run.Say(DelveEventKind.Struck, user.Name);
        if (run.At(x, y) is not { } target || target.OnTeam == user.OnTeam)
        {
            run.Say(DelveEventKind.Missed, user.Name);
            return;
        }
        Wake(run, target);
        if (!Hits(run, user, target, 100))
        {
            run.Say(DelveEventKind.Missed, user.Name, target.Name);
            return;
        }
        Hurt(run, user, target, StrikeDamage(user.Pokemon, target.Pokemon, run.Rng.Next(16)), null);
    }

    /// <summary>The plain strike's damage for a roll (0 the strongest to 15): the user's Attack against the target's Defense, both by their stages.</summary>
    public static int StrikeDamage(Pokemon user, Pokemon target, int roll)
    {
        int attack = Formulas.Staged(user.Attack, user.StatStages.GetValueOrDefault(StatType.Attack));
        int defense = Formulas.Staged(target.Defense, target.StatStages.GetValueOrDefault(StatType.Defense));
        int baseDamage = Formulas.ModernBaseDamage(user.Level, StrikePower, attack, defense);
        return Formulas.ModernFinalDamage(baseDamage, roll, 4096, 1f, user.Status == StatusCondition.Burn, 4096);
    }

    private static int Hurt(DelveRun run, DelveMon user, DelveMon target, int damage, string? move)
    {
        int dealt = Math.Min(target.Pokemon.CurrentHP, Math.Max(0, damage));
        target.Pokemon.CurrentHP -= dealt;
        run.Say(DelveEventKind.Damaged, user.Name, target.Name, move, dealt);
        if (target.Fainted) Faint(run, user, target);
        return dealt;
    }

    private static int Heal(DelveMon m, int amount)
    {
        int before = m.Pokemon.CurrentHP;
        m.Pokemon.CurrentHP = Math.Min(m.Pokemon.MaxHP, before + amount);
        return m.Pokemon.CurrentHP - before;
    }

    private static void Wake(DelveRun run, DelveMon target)
    {
        if (!target.Dormant) return;
        target.Dormant = false;
        run.Say(DelveEventKind.Noticed, target.Name);
    }

    /// <summary>A Pokémon knocked out: off the floor, and a resident a team member knocked out may ask to join.</summary>
    internal static void Faint(DelveRun run, DelveMon? by, DelveMon m)
    {
        m.Pokemon.CurrentHP = 0;
        run.Say(DelveEventKind.Fainted, m.Name, by?.Name);
        if (m.OnTeam)
        {
            // The leader stays for the bag's Revive; anyone else of the team is gone from the run
            if (run.Team.IndexOf(m) > 0) run.Team.Remove(m);
            else run.LeaderFalls();
            return;
        }
        if (by is { OnTeam: true }) Recruits.MayAsk(run, m);
    }

    /// <summary>A condition given, unless one is held already or the target's type keeps it off.</summary>
    internal static bool GiveStatus(DelveRun run, DelveMon user, DelveMon target, StatusCondition status, int sleepTurns = 0)
    {
        var p = target.Pokemon;
        bool kept = p.Status != StatusCondition.None || status switch
        {
            StatusCondition.Poison or StatusCondition.Toxic => p.HasType(PokemonType.Poison) || p.HasType(PokemonType.Steel),
            StatusCondition.Burn => p.HasType(PokemonType.Fire),
            StatusCondition.Paralyze => p.HasType(PokemonType.Electric),
            StatusCondition.Freeze => p.HasType(PokemonType.Ice),
            _ => false
        };
        if (kept)
        {
            run.Say(DelveEventKind.StatusFailed, user.Name, target.Name, status.ToString());
            return false;
        }
        p.Status = status;
        if (status == StatusCondition.Sleep) p.SleepTurns = sleepTurns > 0 ? sleepTurns : run.Rng.Next(2, 5);
        run.Say(DelveEventKind.StatusGiven, target.Name, what: status.ToString());
        return true;
    }

    private static void Confuse(DelveRun run, DelveMon target)
    {
        if (target.ConfusedTurns > 0) return;
        target.ConfusedTurns = run.Rng.Next(2, 6);
        run.Say(DelveEventKind.Confused, target.Name);
    }

    private static void ChangeStats(DelveRun run, DelveMon who, MoveData data)
    {
        var stats = new List<StatType> { data.TargetStatChange!.Value };
        if (data.AlsoChangesStats != null) stats.AddRange(data.AlsoChangesStats);
        foreach (var stat in stats.Distinct())
        {
            int before = who.Pokemon.StatStages.GetValueOrDefault(stat);
            int after = Math.Clamp(before + data.StatStageAmount, -6, 6);
            who.Pokemon.StatStages[stat] = after;
            run.Say(DelveEventKind.StatChanged, who.Name, what: stat.ToString(), amount: after - before);
        }
    }
}
