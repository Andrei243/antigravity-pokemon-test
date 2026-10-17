using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The later generations' moves, batch 2 (plan 06 · R25): the rest of Generations 5 to 7. The rules are Pokémon
// Showdown's (the third pinned source, data/moves.ts at the commit in tools/DataImporter/Sources.cs); each entry
// names the move it was read from. A move that hits with other numbers (Freeze-Dry, Flying Press, Thousand Arrows)
// has an empty entry here and its rule in DamageCalculator, which reads the move's effect; Wonder Room's and Magic
// Room's rules are DamageCalculator.DefendingStat's and BattleEffects.ItemsAreOff's. They join the table in
// BattleCore.Effects.cs when the class is first used (the static constructor in BattleCore.Families.cs).
public sealed partial class BattleCore
{
    /// <summary>A "But it failed!" was said for the move being used (Stomping Tantrum's <c>moveThisTurnResult</c>).</summary>
    private bool failedNow;

    /// <summary>The turn each Pokémon's move last failed on: Stomping Tantrum is twice as strong the turn after.</summary>
    private readonly Dictionary<Pokemon, int> failedOnTurn = new();

    /// <summary>The Pokémon that have eaten a berry this battle (Showdown's <c>ateBerry</c>): only they can use Belch.</summary>
    private readonly HashSet<Pokemon> ateBerry = new();

    /// <summary>The last turn Fairy Lock holds everyone, and the turn Ion Deluge was used.</summary>
    private int fairyLockUntil = -1, ionDelugeTurn = -1;

    /// <summary>The Pokémon whose Round a Round brought forward, by turn: theirs is twice as strong.</summary>
    private readonly HashSet<(int Turn, Pokemon Who)> roundBrought = new();

    /// <summary>Happy Hour was used by the player's side: a trainer battle won pays twice.</summary>
    private bool happyHour;

    /// <summary>What a side has put up for the turn (Showdown's side conditions of one turn), by side.</summary>
    [Flags]
    private enum SideGuard { None = 0, WideGuard = 1, QuickGuard = 2, CraftyShield = 4, MatBlock = 8 }

    private readonly (int Turn, SideGuard Guards)[] guards = { (-1, SideGuard.None), (-1, SideGuard.None) };

    private SideGuard GuardsOf(BattleSide side) => guards[(int)side].Turn == Turn ? guards[(int)side].Guards : SideGuard.None;

    /// <summary>The moves Instruct can't make a Pokémon use again (Showdown's <c>failinstruct</c>, and what holds its user to it).</summary>
    private static readonly HashSet<string> NotInstructed = new()
    {
        "RechargeAfter", "Bide", "Uproar", "ContinueAndConfuseSelf", "Instruct", "HitLastWhiffIfHit", "BeakBlast", "ShellTrap",
        "KingsShield", "Rollout", "Transform", "Mimic", "Sketch", "Metronome", "Assist", "Copycat", "MeFirst", "MirrorMove", "SleepTalk"
    };

    private static readonly Dictionary<string, MoveEffect> LaterEffects2 = new()
    {
        // ---- Generation 5
        ["WideGuard"] = new() { Does = (b, use) => b.RaiseGuard(use, SideGuard.WideGuard) },
        ["QuickGuard"] = new() { Does = (b, use) => b.RaiseGuard(use, SideGuard.QuickGuard) },
        ["WonderRoom"] = new() { Does = (b, use) => b.OpenRoom(use, FieldCondition.WonderRoom) },
        ["MagicRoom"] = new() { Does = (b, use) => b.OpenRoom(use, FieldCondition.MagicRoom) },
        ["Telekinesis"] = new() { Does = (b, use) => b.OnEach(use, t => b.Telekinesis(t)) },
        ["AfterYou"] = new() { Does = (b, use) => b.OnEach(use, t => b.Reorder(t, first: true)) },
        ["Quash"] = new() { Does = (b, use) => b.OnEach(use, t => b.Reorder(t, first: false)) },
        ["Round"] = new()
        {
            // The first Round still to come goes next, and is twice as strong for being brought forward by a Round; it
            // brings the next in its turn (onTry's prioritizeAction, basePowerCallback's sourceEffect)
            Start = (b, use) =>
            {
                use.Scratch = !use.Called && b.roundBrought.Remove((b.Turn, use.User.Pokemon!));
                var next = b.waiting.FirstOrDefault(a => a.Choice.Kind == ChoiceKind.Fight && a.Move?.Data.Effect == "Round" && a.User.IsActive && a.User.Pokemon == a.Actor);
                if (next != null)
                {
                    b.waiting.Remove(next);
                    b.waiting.Insert(0, next);
                    b.roundBrought.Add((b.Turn, next.Actor!));
                }
                return true;
            },
            BasePower = (b, use, t) => use.Scratch is true ? use.Data.Power * 2 : use.Data.Power
        },

        // ---- Generation 6
        ["FlyingPress"] = new(), // the Flying type's matchup too: DamageCalculator's Matchups
        ["FreezeDry"] = new(),   // twice as strong against Water: DamageCalculator's Matchups
        ["MatBlock"] = new() { Does = (b, use) => b.RaiseGuard(use, SideGuard.MatBlock) },
        ["CraftyShield"] = new() { Does = (b, use) => b.RaiseGuard(use, SideGuard.CraftyShield) },
        ["KingsShield"] = new() { Does = (b, use) => b.Brace(use, endure: false, Shield.KingsShield) },
        ["SpikyShield"] = new() { Does = (b, use) => b.Brace(use, endure: false, Shield.SpikyShield) },
        ["BanefulBunker"] = new() { Does = (b, use) => b.Brace(use, endure: false, Shield.BanefulBunker) },
        ["Belch"] = new() { Start = (b, use) => b.ateBerry.Contains(use.User.Pokemon!) || b.Fails() },
        ["Rototiller"] = new() { Does = (b, use) => b.Rototiller(use) },
        ["FlowerShield"] = new() { Does = (b, use) => b.FlowerShield(use) },
        ["FellStinger"] = new()
        {
            // Three stages of Attack once it has knocked its target out (onAfterMoveSecondarySelf)
            Hit = (b, use, hit) =>
            {
                if (hit.Landed && !hit.IntoSubstitute && hit.Target.Pokemon!.CurrentHP == 0 && use.User.IsActive)
                    b.ChangeStat(use.User, StatType.Attack, 3, use.User, false, By.Move);
            }
        },
        ["TrickOrTreat"] = new() { Does = (b, use) => b.OnEach(use, t => b.AddType(t, PokemonType.Ghost)) },
        ["ForestsCurse"] = new() { Does = (b, use) => b.OnEach(use, t => b.AddType(t, PokemonType.Grass)) },
        ["PartingShot"] = new() { Does = (b, use) => b.OnEach(use, t => b.PartingShot(use, t)) },
        ["TopsyTurvy"] = new() { Does = (b, use) => b.OnEach(use, t => b.TopsyTurvy(t)) },
        ["FairyLock"] = new()
        {
            Does = (b, use) =>
            {
                use.Line.With(Shown(use.User, use.User, use.Move, null));
                if (b.fairyLockUntil >= b.Turn)
                {
                    b.Fails();
                    return;
                }
                b.fairyLockUntil = b.Turn + 1;
                b.Say(Text.Of("battle.noOneCanRunAway"));
            }
        },
        ["HyperspaceHole"] = new()
        {
            // Through Protect and every guard, which it breaks; Hyperspace Fury only from Hoopa Unbound (breaksProtect, onTry)
            Protectable = _ => false,
            Start = (b, use) => use.Data.Name != "Hyperspace Fury" || use.User.Pokemon!.Form == "Hoopa-Unbound" || b.Fails(),
            Hit = (b, use, hit) =>
            {
                if (hit.Landed) b.BreakGuards(hit.Target);
            }
        },
        ["WaterShuriken"] = new()
        {
            // Ash-Greninja's (with Battle Bond, not transformed) hits three times, 20 strong (onModifyMove, basePowerCallback)
            Strikes = (b, use) => AshGreninja(use.User) ? 3 : b.MultiHitCount(use.User),
            BasePower = (b, use, t) => AshGreninja(use.User) ? use.Data.Power + 5 : use.Data.Power
        },
        ["AromaticMist"] = new() { Does = (b, use) => b.OnEach(use, t => b.ChangeStat(t, StatType.SpDefense, 1, use.User, true, By.Move)) },
        ["VenomDrench"] = new() { Does = (b, use) => b.OnEach(use, t => b.VenomDrench(use, t)) },
        ["Powder"] = new() { Does = (b, use) => b.OnEach(use, t => b.Powder(t)) },
        ["Geomancy"] = new()
        {
            Charge = new("battle.isAbsorbingPower"),
            Does = (b, use) => b.Raise(use, (StatType.SpAttack, 2), (StatType.SpDefense, 2), (StatType.Speed, 2))
        },
        ["MagneticFlux"] = new() { Does = (b, use) => b.PlusAndMinus(use, StatType.Defense, StatType.SpDefense) },
        ["HappyHour"] = new()
        {
            Does = (b, use) =>
            {
                use.Line.With(Shown(use.User, use.User, use.Move, null));
                if (use.User.IsPlayerSide) b.happyHour = true;
                b.Say(Text.Of("battle.happyAtmosphere"));
            }
        },
        ["Celebrate"] = new()
        {
            Does = (b, use) =>
            {
                use.Line.With(Shown(use.User, use.User, use.Move, null));
                b.Say(Text.Of("battle.congratulations", ("trainer", use.User.Trainer?.Name ?? b.playerName)));
            }
        },
        ["HoldHands"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                b.Say(Text.Of("battle.heldHands", ("user", use.User.Name), ("target", t.Name)));
                return true;
            })
        },
        ["ThousandArrows"] = new() { Hit = (b, use, hit) => b.SmackDown(hit) }, // and reaches what floats: DamageCalculator.HitsWhatFloats
        ["ThousandWaves"] = new() { Hit = (b, use, hit) => b.Shackle(use, hit) },

        // ---- Generation 7
        ["ShoreUp"] = new() { Does = (b, use) => b.ShoreUp(use) },
        ["FirstImpression"] = new() { Start = (b, use) => b.Turn == use.User.Volatile.FirstTurn || b.Fails() },
        ["SpiritShackle"] = new() { Hit = (b, use, hit) => b.Shackle(use, hit) },
        ["SparklingAria"] = new() { Hit = (b, use, hit) => b.SparklingAria(use, hit) },
        ["FloralHealing"] = new() { Does = (b, use) => b.OnEach(use, t => b.FloralHealing(t)) },
        ["StrengthSap"] = new() { Does = (b, use) => b.OnEach(use, t => b.StrengthSap(use, t)) },
        ["LaserFocus"] = new()
        {
            Does = (b, use) =>
            {
                use.Line.With(Shown(use.User, use.User, use.Move, null));
                use.User.Volatile.LaserFocusUntil = b.Turn + 1;
                b.Say(Text.Of("battle.concentratedIntensely", ("user", use.User.Name)));
            }
        },
        ["GearUp"] = new() { Does = (b, use) => b.PlusAndMinus(use, StatType.Attack, StatType.SpAttack) },
        ["ThroatChop"] = new()
        {
            // No sound move for the rest of the turn and the next (its condition of two turns)
            Hit = (b, use, hit) =>
            {
                var t = hit.Target;
                if (hit.Touched && t.IsActive && t.Volatile.ThroatChopUntil < b.Turn) t.Volatile.ThroatChopUntil = b.Turn + 1;
            }
        },
        ["PollenPuff"] = new() { Start = (b, use) => b.PollenPuff(use) },
        ["BurnUp"] = new()
        {
            // Only from a Fire type, which it then isn't (onTryMove, self.onHit)
            Start = (b, use) => use.User.HasType(PokemonType.Fire) || b.Fails(),
            Then = (b, use) =>
            {
                use.User.Volatile.Types = use.User.Types.Where(t => t != PokemonType.Fire).ToList();
                b.Say(Text.Of("battle.burnedItselfOut", ("user", use.User.Name)));
            }
        },
        ["SpeedSwap"] = new() { Does = (b, use) => b.OnEach(use, t => b.SpeedSwap(use.User, t)) },
        ["Purify"] = new() { Does = (b, use) => b.OnEach(use, t => b.Purify(use, t)) },
        ["RevelationDance"] = new()
        {
            // The type of its user's first type (onModifyType); none left after a Burn Up, and it keeps its own
            Start = (b, use) =>
            {
                if (use.User.Types.Count > 0) Retype(use, use.User.Types[0]);
                return true;
            }
        },
        ["CoreEnforcer"] = new() { Hit = (b, use, hit) => b.CoreEnforcer(hit) },
        ["Instruct"] = new() { Does = (b, use) => b.OnEach(use, t => b.Instruct(use, t)) },
        ["BeakBlast"] = new()
        {
            Start = (b, use) =>
            {
                use.User.Turn.HeatingBeak = false;
                return true;
            }
        },
        ["ShellTrap"] = new()
        {
            Start = (b, use) =>
            {
                use.User.Turn.ShellTrap = false;
                if (use.User.Turn.ShellTrapSprung) return true;
                b.failedNow = true;
                b.Say(Text.Of("battle.shellTrapDidntWork", ("user", use.User.Name)));
                return false;
            }
        },
        ["StompingTantrum"] = new()
        {
            // Twice as strong when its user's move failed the turn before (moveLastTurnResult)
            BasePower = (b, use, t) => b.failedOnTurn.TryGetValue(use.User.Pokemon!, out int turn) && turn == b.Turn - 1 ? use.Data.Power * 2 : use.Data.Power
        },
        ["SpectralThief"] = new() { Reach = (b, use, t) => b.StealBoosts(use.User, t) },
        ["MindBlown"] = new()
        {
            // Half its user's HP, rounded up, whatever came of it (mindBlownRecoil, onMoveFail)
            Then = (b, use) => b.BlowMind(use),
            Miss = (b, use, hit) => b.BlowMind(use)
        },
        ["PlasmaFists"] = new() { Then = (b, use) => b.IonDeluge() },
        ["IonDeluge"] = new()
        {
            Does = (b, use) =>
            {
                use.Line.With(Shown(use.User, use.User, use.Move, null));
                b.IonDeluge();
            }
        },
        ["Electrify"] = new() { Does = (b, use) => b.OnEach(use, t => b.Electrify(t)) },
        ["Spotlight"] = new()
        {
            Does = (b, use) => b.OnEach(use, t =>
            {
                b.Field.Side(t.Side).FollowMe = t.Place;
                b.Say(Text.Of("battle.becameTheCenterOf", ("user", t.Name)));
                return true;
            })
        }
    };

    /// <summary>Greninja in its Ash form with Battle Bond, its own and not a Transform's.</summary>
    private static bool AshGreninja(Battler user) =>
        user.Pokemon!.Form == "Greninja-Ash" && user.Ability?.Name == "Battle Bond" && !user.Volatile.Transformed;

    // ---------------------------------------------------------------- protection of the later games

    /// <summary>The moves that count toward Protect's chain (Showdown's <c>stall</c>): Protect's kin, and Wide Guard and Quick Guard.</summary>
    private static bool Stalls(MoveData? move) =>
        move?.Effect is "Protect" or "SurviveWith1Hp" or "KingsShield" or "SpikyShield" or "BanefulBunker" or "WideGuard" or "QuickGuard" or "MaxGuard" or "Obstruct" or "SilkTrap" or "BurningBulwark";

    /// <summary>
    /// Whether Protect, a shield or the target's side's guard stops a move at its target (each guard's
    /// <c>onTryHit</c>): King's Shield lets a status move through; Wide Guard stops a move at more than one foe,
    /// Quick Guard one that goes first, Mat Block one that hurts, Crafty Shield a status move aimed at another.
    /// A side's guard says whose it was (<see cref="MoveHit.FailLine"/>).
    /// </summary>
    private bool Guarded(MoveUse use, Battler target, MoveHit hit)
    {
        var user = use.User;
        // Max Guard, and what a Dynamaxed target can't be made to do (plan 06 · R22)
        if (DynamaxGuards(use, target, hit) is { } decided) return decided;
        if (target == user || !use.Effect.StoppedByProtect(user)) return false;
        bool protectable = use.Data.Flags.HasFlag(MoveFlags.Protect);
        bool status = use.Move.Category == MoveCategory.Status;
        // King's Shield and Obstruct (plan 06 · R26) let a status move through
        if (target.Turn.Protecting && protectable && !(target.Turn.Shield is Shield.KingsShield or Shield.Obstruct or Shield.SilkTrap or Shield.BurningBulwark && status)) return true;

        var up = GuardsOf(target.Side);
        if (up == SideGuard.None) return false;
        string? which = null;
        if (up.HasFlag(SideGuard.WideGuard) && protectable && use.Data.Target is MoveTarget.AllFoes or MoveTarget.AllOthers) which = "Wide Guard";
        else if (up.HasFlag(SideGuard.QuickGuard) && protectable && use.Move.Priority > 0) which = "Quick Guard";
        else if (up.HasFlag(SideGuard.MatBlock) && protectable && !status) which = "Mat Block";
        else if (up.HasFlag(SideGuard.CraftyShield) && status) which = "Crafty Shield";
        if (which == null) return false;
        hit.FailLine = Text.Of("battle.guardProtected", ("move", MoveDatabase.Get(which).DisplayName), ("target", target.Name));
        return true;
    }

    /// <summary>
    /// A shield answers whoever it stopped, if the move touched it: King's Shield takes a stage of Attack, Spiky
    /// Shield an eighth of their HP, Baneful Bunker poisons them (each condition's <c>onTryHit</c>).
    /// </summary>
    private void ShieldBites(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (hit.FailLine != null || !user.IsActive || !use.Data.Flags.HasFlag(MoveFlags.Contact)) return;
        switch (t.Turn.Shield)
        {
            case Shield.KingsShield:
                ChangeStat(user, StatType.Attack, -1, t, false, By.Other);
                break;
            case Shield.SpikyShield:
                LoseHp(user, Math.Max(1, user.Pokemon!.MaxHP / 8), Text.Of("battle.wasHurtByTheSpikes", ("user", user.Name), ("target", t.Name)));
                break;
            case Shield.BanefulBunker:
                TryInflictStatus(user, StatusCondition.Poison, t, false, By.Other);
                break;
            case Shield.Obstruct:
                // Two stages of Defense (plan 06 · R26; Obstruct's onTryHit)
                ChangeStat(user, StatType.Defense, -2, t, false, By.Other);
                break;
            default:
                // Silk Trap and Burning Bulwark (plan 06 · R26b)
                LaterShieldBites(use, t);
                break;
        }
    }

    /// <summary>
    /// Wide Guard, Quick Guard, Crafty Shield and Mat Block: the user's side for the rest of the turn, while anyone
    /// is still to act. Mat Block is for its user's first turn alone; Wide Guard and Quick Guard go on Protect's
    /// chain without being held back by it (Showdown's <c>stall</c> volatile, added on success).
    /// </summary>
    private void RaiseGuard(MoveUse use, SideGuard guard)
    {
        var user = use.User;
        var v = user.Volatile;
        use.Line.With(Shown(user, user, use.Move, null));
        bool othersToAct = waiting.Any(a => a.User.IsActive && a.User.Pokemon == a.Actor);
        if (!othersToAct || (guard == SideGuard.MatBlock && Turn != v.FirstTurn))
        {
            Fails();
            return;
        }
        if (guard is SideGuard.WideGuard or SideGuard.QuickGuard)
        {
            if (!Stalls(use.MoveBefore)) v.ProtectChain = 0;
            if (v.ProtectChain < Rules.ProtectRates.Count - 1) v.ProtectChain++;
        }
        int side = (int)user.Side;
        guards[side] = (Turn, GuardsOf(user.Side) | guard);
        Say(Text.Of("battle.guardUp", ("move", use.Move.DisplayName), ("user", user.Name)));
    }

    /// <summary>Hyperspace Hole and Fury break Protect, a shield and the side's guards (<c>breaksProtect</c>).</summary>
    private void BreakGuards(Battler t)
    {
        bool broke = t.Turn.Protecting || GuardsOf(t.Side) != SideGuard.None;
        t.Turn.Protecting = false;
        t.Turn.Shield = Shield.None;
        guards[(int)t.Side] = (-1, SideGuard.None);
        if (broke) Say(Text.Of("battle.itBrokeThroughProtection", ("target", t.Name)));
    }

    // ---------------------------------------------------------------- the rooms and the field's moments

    /// <summary>
    /// Wonder Room and Magic Room: five turns; used again while it stands, it ends (each room's <c>onFieldRestart</c>).
    /// Each says its line with its event, as Trick Room does.
    /// </summary>
    private void OpenRoom(MoveUse use, FieldCondition room)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        bool wonder = room == FieldCondition.WonderRoom;
        if (Field.Has(room))
        {
            CloseRoom(room);
            return;
        }
        if (wonder) Field.WonderRoomTurns = 5;
        else Field.MagicRoomTurns = 5;
        Say(Text.Of(wonder ? "battle.wonderRoomOn" : "battle.magicRoomOn")).With(new FieldConditionChanged(room, Up: true));
    }

    private void CloseRoom(FieldCondition room)
    {
        bool wonder = room == FieldCondition.WonderRoom;
        if (wonder) Field.WonderRoomTurns = 0;
        else Field.MagicRoomTurns = 0;
        Say(Text.Of(wonder ? "battle.wonderRoomOff" : "battle.magicRoomOff")).With(new FieldConditionChanged(room, Up: false));
    }

    /// <summary>The rooms run down at the turn's end, with Trick Room (Showdown's residual order 27).</summary>
    private void RoomsRunDown()
    {
        if (Field.WonderRoomTurns > 0 && --Field.WonderRoomTurns == 0) CloseRoom(FieldCondition.WonderRoom);
        if (Field.MagicRoomTurns > 0 && --Field.MagicRoomTurns == 0) CloseRoom(FieldCondition.MagicRoom);
    }

    /// <summary>Ion Deluge (and Plasma Fists): every Normal move is Electric for the rest of the turn.</summary>
    private void IonDeluge()
    {
        ionDelugeTurn = Turn;
        Say(Text.Of("battle.ionDeluge"));
    }

    /// <summary>A move as Ion Deluge and Electrify leave it: Electric.</summary>
    private Move Electrified(Battler user, Move move)
    {
        bool electric = user.Turn.Electrified || (ionDelugeTurn == Turn && move.Type == PokemonType.Normal);
        return electric && move.Type != PokemonType.Electric ? new Move(move.Data.OfType(PokemonType.Electric), move.CurrentPP) : move;
    }

    /// <summary>
    /// Powder: a Fire move of its target's explodes in its user's face before anything happens, for a quarter of
    /// its HP (rounded, at least 1), and the move fails (the condition's <c>onTryMove</c>).
    /// </summary>
    private bool Explodes(MoveUse use)
    {
        var user = use.User;
        if (!user.Turn.Powdered || use.Move.Type != PokemonType.Fire) return false;
        failedNow = true;
        Unlock(user);
        LoseHp(user, Math.Max(1, (user.Pokemon!.MaxHP + 2) / 4), Text.Of("battle.powderExploded", ("user", user.Name)));
        return true;
    }

    /// <summary>Throat Chop holds: the move is a sound move.</summary>
    private bool ThroatChopped(Battler b, MoveData move) => b.Volatile.ThroatChopUntil >= Turn && move.Flags.HasFlag(MoveFlags.Sound);

    /// <summary>Beak Blast heats up and Shell Trap is set as the turn begins, before anyone moves.</summary>
    private void GetReady()
    {
        foreach (var act in waiting.Where(a => a.Choice.Kind == ChoiceKind.Fight && a.User.IsActive && a.User.Pokemon == a.Actor))
        {
            switch (act.Move?.Data.Effect)
            {
                case "BeakBlast":
                    act.User.Turn.HeatingBeak = true;
                    Say(Text.Of("battle.heatingUpBeak", ("user", act.User.Name)));
                    break;
                case "ShellTrap":
                    act.User.Turn.ShellTrap = true;
                    Say(Text.Of("battle.setAShellTrap", ("user", act.User.Name)));
                    break;
            }
        }
    }

    /// <summary>
    /// What a hit sets off in its target this turn: a beak heating up burns whoever touched it, and a shell trap a
    /// foe's physical move struck goes off next (the conditions' <c>onHit</c>).
    /// </summary>
    private void HitAnswers(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (!hit.Touched || t == user) return;
        if (t.Turn.HeatingBeak && use.Data.Flags.HasFlag(MoveFlags.Contact) && user.IsActive)
            TryInflictStatus(user, StatusCondition.Burn, t, false, By.Other);
        if (t.Turn.ShellTrap && !t.Turn.ShellTrapSprung && t.IsActive && t.Side != user.Side && use.Move.Category == MoveCategory.Physical)
        {
            t.Turn.ShellTrapSprung = true;
            var act = waiting.FirstOrDefault(a => a.User == t && a.Actor == t.Pokemon);
            if (act != null)
            {
                waiting.Remove(act);
                waiting.Insert(0, act);
            }
        }
    }

    // ---------------------------------------------------------------- moves on another Pokémon

    /// <summary>
    /// Telekinesis: three turns in the air, where every move but a one-hit knockout finds it and no Ground move
    /// does; not under Gravity, nor for one rooted or brought down, nor for the Diglett and Sandygast lines (onTry, onStart).
    /// </summary>
    private bool Telekinesis(Battler t)
    {
        var v = t.Volatile;
        if (Field.Gravity || v.TelekinesisTurns > 0 || v.SmackedDown || v.Ingrained || t.Pokemon!.Form == "Gengar-Mega" ||
            t.Pokemon.Species.Name is "Diglett" or "Dugtrio" or "Sandygast" or "Palossand") return Fails();
        v.TelekinesisTurns = 3;
        Say(Text.Of("battle.wasHurledIntoTheAir", ("target", t.Name)));
        return true;
    }

    /// <summary>After You and Quash: the target's action comes next, or last of the turn; in a single battle neither works.</summary>
    private bool Reorder(Battler t, bool first)
    {
        var act = IsDouble ? waiting.FirstOrDefault(a => a.User == t && a.Actor == t.Pokemon) : null;
        if (act == null) return Fails();
        waiting.Remove(act);
        if (first) waiting.Insert(0, act);
        else waiting.Add(act);
        Say(first ? Text.Of("battle.tookTheKindOffer", ("target", t.Name)) : Text.Of("battle.moveWasPostponed", ("target", t.Name)));
        return true;
    }

    /// <summary>Trick-or-Treat and Forest's Curse: a type added to the target's own, in place of any added before; not one it has.</summary>
    private bool AddType(Battler t, PokemonType type)
    {
        if (t.HasType(type) || t.HasSubstitute) return Fails();
        var v = t.Volatile;
        var types = t.Types.Where(x => v.AddedType != x).ToList();
        types.Add(type);
        v.Types = types;
        v.AddedType = type;
        Say(Text.Of("battle.typeWasAdded", ("pick", type), ("target", t.Name)));
        return true;
    }

    /// <summary>Parting Shot: a stage off the target's Attack and Sp. Atk; if either fell, its user goes back.</summary>
    private bool PartingShot(MoveUse use, Battler t)
    {
        bool attack = ChangeStat(t, StatType.Attack, -1, use.User, true, By.Move);
        bool special = ChangeStat(t, StatType.SpAttack, -1, use.User, true, By.Move);
        if (attack || special) use.Leaves = Leaving.Returns;
        return attack || special;
    }

    /// <summary>Topsy-Turvy: every stage the target has, turned round; it fails on one with none (or behind a Substitute).</summary>
    private bool TopsyTurvy(Battler t)
    {
        var stages = t.Pokemon!.StatStages;
        if (t.HasSubstitute || stages.Values.All(s => s == 0)) return Fails();
        foreach (var stat in stages.Keys.ToList()) stages[stat] = -stages[stat];
        Say(Text.Of("battle.statChangesWereReversed", ("target", t.Name)));
        return true;
    }

    /// <summary>Venom Drench: a stage off a poisoned foe's Attack, Sp. Atk and Speed; nothing to one that isn't.</summary>
    private bool VenomDrench(MoveUse use, Battler t)
    {
        if (t.Pokemon!.Status is not (StatusCondition.Poison or StatusCondition.Toxic)) return Fails();
        bool any = false;
        foreach (var stat in new[] { StatType.Attack, StatType.SpAttack, StatType.Speed }) any |= ChangeStat(t, stat, -1, use.User, true, By.Move);
        return any;
    }

    /// <summary>Powder: the target is covered for the turn; a Grass type is not.</summary>
    private bool Powder(Battler t)
    {
        if (t.HasType(PokemonType.Grass))
        {
            Say(Text.Of("battle.itDoesntAffect", ("target", t.Name)));
            return false;
        }
        if (t.Turn.Powdered) return Fails();
        t.Turn.Powdered = true;
        Say(Text.Of("battle.isCoveredInPowder", ("target", t.Name)));
        return true;
    }

    /// <summary>Electrify: the target's move this turn is Electric; it fails on one that has moved.</summary>
    private bool Electrify(Battler t)
    {
        if (t.MovedThisTurn) return Fails();
        t.Turn.Electrified = true;
        Say(Text.Of("battle.wasElectrified", ("target", t.Name)));
        return true;
    }

    /// <summary>Spirit Shackle, Anchor Shot and Thousand Waves: a target the hit reached can't leave while its user stays.</summary>
    private void Shackle(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive || !use.User.IsActive || t.Volatile.TrappedBy != null) return;
        t.Volatile.TrappedBy = use.User.Place;
        Say(Text.Of("battle.canNoLongerEscape", ("target", t.Name)));
    }

    /// <summary>Sparkling Aria: a burned target the hit reached is cured of it.</summary>
    private void SparklingAria(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        if (!hit.Landed || t == use.User || !t.IsActive || t.Pokemon!.Status != StatusCondition.Burn) return;
        CureStatus(t, Text.Of("battle.burnWasHealed", ("target", t.Name)));
    }

    /// <summary>Floral Healing: half the target's HP, rounded up, or two thirds on Grassy Terrain; not to one whose HP is full.</summary>
    private bool FloralHealing(Battler t)
    {
        var p = t.Pokemon!;
        if (t.HasSubstitute || p.CurrentHP >= p.MaxHP) return Fails();
        int amount = Field.Has(FieldCondition.GrassyTerrain) ? Formulas.Modify(p.MaxHP, 2732) : (p.MaxHP + 1) / 2;
        RestoreHp(t, amount, Text.Of("battle.regainedHealth", ("user", t.Name)));
        return true;
    }

    /// <summary>
    /// Strength Sap: its user gains HP as great as the target's Attack at its stage, and the target loses a stage of
    /// it; not when its Attack is as low as it goes. Liquid Ooze turns the gain into a loss.
    /// </summary>
    private bool StrengthSap(MoveUse use, Battler t)
    {
        var user = use.User;
        var q = t.Pokemon!;
        int stage = q.StatStages.GetValueOrDefault(StatType.Attack);
        if (t.HasSubstitute || stage <= -6) return Fails();
        int attack = Formulas.Staged(q.Attack, stage);
        bool fell = ChangeStat(t, StatType.Attack, -1, user, false, By.Move);
        if (!user.IsActive) return fell;
        if (BattleEffects.Of(t).Any(e => e.HurtsDrainers)) LoseHp(user, attack, Text.Of("battle.suckedUpTheLiquid", ("user", user.Name)));
        else if (user.Pokemon!.CurrentHP < user.Pokemon.MaxHP && user.Volatile.HealBlockTurns == 0)
            RestoreHp(user, attack, Text.Of("battle.regainedHealth", ("user", user.Name)));
        return true;
    }

    /// <summary>Speed Swap: the user's Speed and the target's change places for as long as each stays in.</summary>
    private bool SpeedSwap(Battler user, Battler t)
    {
        var p = user.Pokemon!;
        var q = t.Pokemon!;
        KeepStats(Keep(user), p);
        KeepStats(Keep(t), q);
        (p.Speed, q.Speed) = (q.Speed, p.Speed);
        Say(Text.Of("battle.switchedSpeed", ("user", user.Name)));
        orderChanged = true;
        return true;
    }

    /// <summary>Purify: the target is cured of its condition and the user regains half its HP, rounded up; nothing to a target with none.</summary>
    private bool Purify(MoveUse use, Battler t)
    {
        var status = t.Pokemon!.Status;
        if (t.HasSubstitute || status is StatusCondition.None or StatusCondition.Faint) return Fails();
        CureStatus(t, Text.Of("battle.wasCuredOf", ("pokemon", t.Name), ("status", BattleText.StatusName(status))));
        var p = use.User.Pokemon!;
        if (use.User.IsActive && p.CurrentHP < p.MaxHP) RestoreHp(use.User, (p.MaxHP + 1) / 2, Text.Of("battle.regainedHealth", ("user", use.User.Name)));
        return true;
    }

    /// <summary>Core Enforcer: the ability of a target the hit reached that has already moved this turn is suppressed while it stays in.</summary>
    private void CoreEnforcer(MoveHit hit)
    {
        var t = hit.Target;
        if (!hit.Touched || !t.IsActive || !t.MovedThisTurn || t.Volatile.AbilitySuppressed) return;
        if (t.Pokemon!.AbilityName is { } ability && CantBeReplaced.Contains(ability) && ability != "Truant") return;
        t.Volatile.AbilitySuppressed = true;
        Say(Text.Of("battle.abilityWasSuppressed", ("target", t.Name)));
    }

    /// <summary>
    /// Instruct: the target uses the move it last used again at once, at the cost of its PP; not a move that holds
    /// its user, charges, calls another or can't be told again (Showdown's <c>failinstruct</c>).
    /// </summary>
    private bool Instruct(MoveUse use, Battler t)
    {
        var data = t.Volatile.LastMove;
        var known = data == null ? null : t.Pokemon!.Moves.FirstOrDefault(m => m.Data == data);
        if (known == null || known.CurrentPP <= 0 || t.IsHeldToItsMove || t.Volatile.Recharging || EffectOf(data!).Charge != null ||
            (data!.Effect is { } effect && NotInstructed.Contains(effect)))
            return Fails();
        Say(Text.Of("battle.followedInstructions", ("target", t.Name), ("user", use.User.Name)));
        known.CurrentPP--;
        var inner = new MoveUse { User = t, Move = known, Effect = EffectOf(data), MoveBefore = t.Volatile.LastMove };
        // At the place it last aimed at (lastMoveTargetLoc), or wherever a move goes when that place has nobody
        inner.Targets = ResolveTargets(t, known, t.Volatile.LastMoveTarget is { } aimed ? At(aimed) : null);
        Perform(inner);
        return true;
    }

    /// <summary>Spectral Thief: the target's raised stages are its user's before the hit lands, and the target's are gone.</summary>
    private void StealBoosts(Battler user, Battler t)
    {
        var raised = t.Pokemon!.StatStages.Where(s => s.Value > 0).ToList();
        if (raised.Count == 0 || !user.IsActive) return;
        foreach (var (stat, _) in raised) t.Pokemon.StatStages[stat] = 0;
        Say(Text.Of("battle.stoleBoosts", ("user", user.Name)));
        foreach (var (stat, by) in raised) ChangeStat(user, stat, by, user, false, By.Move);
    }

    /// <summary>Mind Blown: once, whatever came of it, half its user's HP rounded up (Magic Guard keeps it off).</summary>
    private void BlowMind(MoveUse use)
    {
        if (use.Scratch is true || !use.User.IsActive) return;
        use.Scratch = true;
        LoseHp(use.User, (use.User.Pokemon!.MaxHP + 1) / 2, Text.Of("battle.hurtByItsOwnBlast", ("user", use.User.Name)));
    }

    /// <summary>Pollen Puff on an ally heals it by half its HP, rounded down, and hurts it not at all (onTryHit, onHit).</summary>
    private bool PollenPuff(MoveUse use)
    {
        var user = use.User;
        var t = use.Targets.FirstOrDefault();
        if (t == null || t.Side != user.Side || t == user) return true;
        use.Line.With(Shown(user, t, use.Move, null));
        var p = t.Pokemon!;
        if (user.Volatile.HealBlockTurns > 0 || p.CurrentHP >= p.MaxHP) return Fails();
        RestoreHp(t, p.MaxHP / 2, Text.Of("battle.regainedHealth", ("user", t.Name)));
        return false;
    }

    // ---------------------------------------------------------------- moves on its own side

    /// <summary>Shore Up: half its HP, or two thirds in a sandstorm (<c>this.modify</c>'s 4096ths, rounded half down).</summary>
    private void ShoreUp(MoveUse use)
    {
        var user = use.User;
        var p = user.Pokemon!;
        use.Line.With(Shown(user, user, use.Move, null));
        if (p.CurrentHP >= p.MaxHP)
        {
            Say(Text.Of("battle.hpIsFull", ("user", user.Name)));
            return;
        }
        int amount = Formulas.Modify(p.MaxHP, Field.WeatherInEffect == BattleWeather.Sandstorm ? 2732 : 2048);
        RestoreHp(user, amount, Text.Of("battle.regainedHealth", ("user", user.Name)));
    }

    /// <summary>
    /// Rototiller: a stage of Attack and Sp. Atk for every Grass type on the ground; it fails when there is none
    /// and nobody is in the air (onHitField).
    /// </summary>
    private void Rototiller(MoveUse use)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        var everyone = BySpeed().Where(b => b.IsActive).ToList();
        var tilled = everyone.Where(b => DamageCalculator.IsGrounded(b) && b.HasType(PokemonType.Grass)).ToList();
        if (tilled.Count == 0)
        {
            Fails();
            return;
        }
        foreach (var b in tilled)
        {
            ChangeStat(b, StatType.Attack, 1, use.User, false, By.Move);
            ChangeStat(b, StatType.SpAttack, 1, use.User, false, By.Move);
        }
    }

    /// <summary>Flower Shield: a stage of Defense for every Grass type on the field; it fails when none rose.</summary>
    private void FlowerShield(MoveUse use)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        bool any = false;
        foreach (var b in BySpeed().Where(b => b.IsActive && b.HasType(PokemonType.Grass)).ToList())
            any |= ChangeStat(b, StatType.Defense, 1, use.User, false, By.Move);
        if (!any) Fails();
    }

    /// <summary>Magnetic Flux and Gear Up: a stage of two stats for the user and its ally where they have Plus or Minus.</summary>
    private void PlusAndMinus(MoveUse use, StatType first, StatType second)
    {
        use.Line.With(Shown(use.User, use.User, use.Move, null));
        var charged = use.Targets.Where(b => b.IsActive && b.Ability?.Name is "Plus" or "Minus").ToList();
        bool any = false;
        foreach (var b in charged)
        {
            any |= ChangeStat(b, first, 1, use.User, false, By.Move);
            any |= ChangeStat(b, second, 1, use.User, false, By.Move);
        }
        if (!any) Fails();
    }
}
