using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Z-Moves and Ultra Burst (plan 06 · R21; docs/mechanics/rulings.md, "Z-Moves and Ultra Burst"), the second of the
// four later mechanics, on R20's pattern: a flag on the move's choice (BattleChoice.ZMove, BattleChoice.UltraBurst),
// refused by WhyNot where it can't be; the change carried out at its moment of the turn, with its line and an event
// of its own on the line (ZPowerUsed, UltraBursted); the trainer AI's use of it (TrainerAi.WithTheLaterMechanics);
// all of it behind the Ruleset (Ruleset.ZMoves), so a battle of Platinum's rules never comes here. The rules are
// Pokémon Showdown's at the pinned commit: sim/battle-actions.ts (getZMove, getActiveZMove, canZMove, runZPower,
// canUltraBurst, runMegaEvo for the Ultra Burst), sim/side.ts (one Z-Move and one Ultra Burst a battle for each
// trainer), data/conditions.ts and data/moves.ts (a Z-Move through Protect and its kin does a quarter: zBrokeProtect,
// healreplacement, the signature Z-Moves) and data/items.ts (no Z-Crystal is ever taken: onTakeItem false).
public sealed partial class BattleCore
{
    /// <summary>The item in the bag that lets the player's Pokémon use a Z-Move, by name.</summary>
    public const string ZRingItem = "Z-Ring";

    /// <summary>The held item that lets a Necrozma fused with Solgaleo or Lunala Ultra Burst.</summary>
    public const string UltranecroziumZ = "Ultranecrozium Z";

    /// <summary>The player has a Z-Ring (<see cref="CoreSetup.ZRing"/>).</summary>
    public bool ZRing { get; }

    /// <summary>Whoever has used a Z-Move this battle, and whoever has Ultra Burst: a trainer, or the player for their own (<see cref="MegaKey"/>).</summary>
    private readonly HashSet<object> zUsed = new(), burstUsed = new();

    /// <summary>The Z-Moves (true) and Ultra Bursts (false) chosen for the turn being chosen, by trainer.</summary>
    private readonly HashSet<(object Who, bool Z)> zChosen = new();

    /// <summary>The uses of moves that are Z-Moves: what goes through a protection at a quarter.</summary>
    private readonly HashSet<MoveUse> zUses = new();

    /// <summary>Memento's and Parting Shot's Z-Power: the next Pokémon to come into the place is healed whole (Showdown's <c>healreplacement</c>), and who it was set by.</summary>
    private readonly Dictionary<Place, Pokemon> healReplacement = new();

    /// <summary>Each type's Z-Move (Showdown's <c>Z_MOVES</c>).</summary>
    private static readonly Dictionary<PokemonType, string> TypeZMoves = new()
    {
        [PokemonType.Normal] = "Breakneck Blitz", [PokemonType.Fighting] = "All-Out Pummeling", [PokemonType.Flying] = "Supersonic Skystrike",
        [PokemonType.Poison] = "Acid Downpour", [PokemonType.Ground] = "Tectonic Rage", [PokemonType.Rock] = "Continental Crush",
        [PokemonType.Bug] = "Savage Spin-Out", [PokemonType.Ghost] = "Never-Ending Nightmare", [PokemonType.Steel] = "Corkscrew Crash",
        [PokemonType.Fire] = "Inferno Overdrive", [PokemonType.Water] = "Hydro Vortex", [PokemonType.Grass] = "Bloom Doom",
        [PokemonType.Electric] = "Gigavolt Havoc", [PokemonType.Psychic] = "Shattered Psyche", [PokemonType.Ice] = "Subzero Slammer",
        [PokemonType.Dragon] = "Devastating Drake", [PokemonType.Dark] = "Black Hole Eclipse", [PokemonType.Fairy] = "Twinkle Tackle"
    };

    /// <summary>The forms Showdown's crystals name by other names than the data's (the fused Necrozmas).</summary>
    private static readonly Dictionary<string, string> ZUserNames = new()
    {
        ["Necrozma-Dusk-Mane"] = "Necrozma-Dusk", ["Necrozma-Dawn-Wings"] = "Necrozma-Dawn"
    };

    /// <summary>
    /// The Z-Move effects the data names (<c>moves.json</c>'s <c>effect</c> for the Z-Moves; the importer marks a
    /// Z-Move run by them). A type's Z-Move and most species' are plain hits of their power (<c>ZMove</c>); the
    /// others are Showdown's own.
    /// </summary>
    private static readonly Dictionary<string, MoveEffect> ZEffects = new()
    {
        ["ZMove"] = new(),
        // Extreme Evoboost: two stages of every stat but accuracy and evasion
        ["ZExtremeEvoboost"] = new() { Does = (b, use) => b.RaiseAll(use.User, 2) },
        // Genesis Supernova: Psychic Terrain once it has hit (its secondary's self onHit)
        ["ZGenesisSupernova"] = new()
        {
            Then = (b, use) =>
            {
                if (use.Hits.Any(h => h.Landed) && use.User.IsActive) b.SetTerrain(FieldCondition.PsychicTerrain, use.User);
            }
        },
        // Splintered Stormshards: whatever terrain there is ends where it hits (onHit, onAfterSubDamage)
        ["ZSplinteredStormshards"] = new()
        {
            Hit = (b, use, hit) =>
            {
                if (hit.Landed && b.Field.Terrain is { } terrain) b.EndTerrain(terrain);
            }
        },
        // Clangorous Soulblaze: a stage of every stat once it has hit (its selfBoost)
        ["ZClangorousSoulblaze"] = new()
        {
            Then = (b, use) =>
            {
                if (use.Hits.Any(h => h.Landed) && use.User.IsActive) b.RaiseAll(use.User, 1);
            }
        },
        // Guardian of Alola: three quarters of the target's HP, a quarter of that through a protection (its damageCallback)
        ["ZGuardianOfAlola"] = new()
        {
            Deals = (b, use, t) =>
            {
                int hp75 = t.Pokemon!.CurrentHP * 3 / 4;
                if (!b.ZBrokeProtect(use, t)) return Math.Max(1, hp75);
                b.Say(Text.Of("battle.zBrokeProtect", ("target", t.Name)));
                return Math.Max(1, (int)Math.Ceiling(hp75 / 4.0 - 0.5));
            }
        }
    };

    /// <summary>The trainer of the place has a Z-Ring: the player's own (<see cref="ZRing"/>), a trainer's or a partner's.</summary>
    private bool HasZRing(Battler b) => b.Trainer != null ? b.Trainer.ZRing : b.IsPlayerSide && ZRing;

    /// <summary>A name a crystal's list of users gives is the Pokémon's: its form's, or its species', a Pikachu's cap's.</summary>
    private static bool IsZUser(Pokemon p, string user)
    {
        string name = p.Form ?? p.Species.Name;
        string ours = ZUserNames.GetValueOrDefault(user, user);
        return name == ours || name == ours + "-Cap";
    }

    /// <summary>
    /// The Z-Move a Pokémon would make of one of its moves through the crystal it holds, whatever the battle; null
    /// for none (Showdown's <c>getZMove</c>): a species' crystal turns its own move into its own Z-Move, in the
    /// hands of the species it names; a type's crystal turns a damaging move of its type into the type's Z-Move and
    /// a status move into its Z-Power version, which is the move itself. A move out of PP has none, unless
    /// <paramref name="pastPp"/> (the move has just been paid for).
    /// </summary>
    public static MoveData? ZMoveOf(Pokemon p, Move move, bool pastPp = false)
    {
        if (p.IsEgg || p.HeldItem?.ZCrystal is not { } crystal || move.Data == StruggleData || (!pastPp && move.CurrentPP <= 0)) return null;
        if (crystal.Users is { Count: > 0 } users && !users.Any(u => IsZUser(p, u))) return null;
        if (crystal.From != null) return crystal.Move != null && move.Name == crystal.From ? MoveDatabase.Get(crystal.Move) : null;
        if (crystal.Type != move.Data.Type) return null;
        if (move.Data.Category == MoveCategory.Status) return move.Data;
        return move.Data.ZPower > 0 ? MoveDatabase.Get(TypeZMoves[move.Data.Type]) : null;
    }

    /// <summary>
    /// Why the Pokémon in a place can't use its move in that position as a Z-Move this turn; null when it can. Under
    /// rules without them, with no Z-Ring, with no Z-Move of that move, a Mega, primal or Ultra form that has
    /// transformed, or once its trainer has used one.
    /// </summary>
    public string? WhyNotZMove(Battler b, int move)
    {
        var mine = At(b.Place);
        string no = Text.Of("battle.cantZMove", ("pokemon", mine.Name));
        if (!Rules.ZMoves || mine.Pokemon is not { } p || !HasZRing(mine) || move < 0 || move >= p.Moves.Count) return no;
        if (zUsed.Contains(MegaKey(mine))) return Text.Of("battle.zOnlyOnce");
        if (mine.Volatile.Transformed && (p.FormData?.Kind is FormKind.Mega or FormKind.Primal || p.Form == UltraForm)) return no;
        return ZMoveOf(p, p.Moves[move]) == null ? no : null;
    }

    /// <summary>Whether the Pokémon in a place may use its move in that position as a Z-Move (and nobody of its trainer's has chosen to this turn).</summary>
    public bool CanUseZMove(Battler b, int move) => WhyNotZMove(b, move) == null && !zChosen.Contains((MegaKey(At(b.Place)), true));

    /// <summary>Ultra Burst's form.</summary>
    private const string UltraForm = "Necrozma-Ultra";

    /// <summary>The form a Pokémon would Ultra Burst into: a Necrozma fused with Solgaleo or Lunala holding Ultranecrozium Z (Showdown's <c>canUltraBurst</c>); null for anyone else.</summary>
    public static string? UltraBurstFormOf(Pokemon p) =>
        !p.IsEgg && p.Form is "Necrozma-Dusk" or "Necrozma-Dawn" && p.HeldItem?.Name == UltranecroziumZ && p.Species.Form(UltraForm) != null ? UltraForm : null;

    /// <summary>Why the Pokémon in a place can't Ultra Burst this turn; null when it can.</summary>
    public string? WhyNotUltraBurst(Battler b)
    {
        var mine = At(b.Place);
        string no = Text.Of("battle.cantUltraBurst", ("pokemon", mine.Name));
        if (!Rules.ZMoves || mine.Pokemon is not { } p || !HasZRing(mine) || mine.Volatile.Transformed || UltraBurstFormOf(p) == null) return no;
        return burstUsed.Contains(MegaKey(mine)) ? Text.Of("battle.ultraBurstOnlyOnce") : null;
    }

    /// <summary>Whether the Pokémon in a place may Ultra Burst with the move it chooses now.</summary>
    public bool CanUltraBurst(Battler b) => WhyNotUltraBurst(b) == null && !zChosen.Contains((MegaKey(At(b.Place)), false));

    /// <summary>What a choice of the move's may not have: a Z-Move or an Ultra Burst the rules don't let it make (a chooser inside asks this).</summary>
    private BattleChoice AsTheZRulesLet(Battler b, BattleChoice choice)
    {
        if (choice.ZMove && (choice.Kind != ChoiceKind.Fight || !CanUseZMove(b, choice.Move))) choice = choice with { ZMove = false };
        if (choice.UltraBurst && (choice.Kind != ChoiceKind.Fight || !CanUltraBurst(b))) choice = choice with { UltraBurst = false };
        return choice;
    }

    /// <summary>The refusal of a Z-Move or an Ultra Burst in a choice (<see cref="WhyNot"/> asks it); null when there is none.</summary>
    private string? WhyNotZ(BattleChoice choice, Battler b)
    {
        if (choice.ZMove && (choice.Kind != ChoiceKind.Fight || choice.Move < 0)) return Text.Of("battle.cantZMove", ("pokemon", b.Name));
        if (choice.ZMove && WhyNotZMove(b, choice.Move) is { } noZ) return noZ;
        if (choice.UltraBurst && (choice.Kind != ChoiceKind.Fight || choice.Move == BattleChoice.HeldMove)) return Text.Of("battle.cantUltraBurst", ("pokemon", b.Name));
        return choice.UltraBurst ? WhyNotUltraBurst(b) : null;
    }

    /// <summary>Two Z-Moves, or two Ultra Bursts, of one trainer in one answer are refused.</summary>
    private void RefuseTwoZ(IReadOnlyList<BattleChoice> choices)
    {
        if (choices.Where(c => c.ZMove).GroupBy(c => MegaKey(At(c.Who))).Any(g => g.Count() > 1)) throw new ArgumentException(Text.Of("battle.zOnlyOnce"));
        if (choices.Where(c => c.UltraBurst).GroupBy(c => MegaKey(At(c.Who))).Any(g => g.Count() > 1)) throw new ArgumentException(Text.Of("battle.ultraBurstOnlyOnce"));
    }

    /// <summary>Notes the Z-Moves and Ultra Bursts an answer chooses, refusing a second one of the same trainer's in a turn.</summary>
    private void ChooseZ(IEnumerable<BattleChoice> chosen)
    {
        foreach (var choice in chosen)
        {
            if (choice.ZMove && !zChosen.Add((MegaKey(At(choice.Who)), true))) throw new ArgumentException(Text.Of("battle.zOnlyOnce"));
            if (choice.UltraBurst && !zChosen.Add((MegaKey(At(choice.Who)), false))) throw new ArgumentException(Text.Of("battle.ultraBurstOnlyOnce"));
        }
    }

    /// <summary>
    /// The Ultra Burst, with the turn's Mega Evolutions (Showdown's <c>runMegaEvo</c> for <c>canUltraBurst</c>): the
    /// light's line, the form taken with its ability, stats and types, the Ultra Burst spent for the trainer, and
    /// Neuroforce acting as an ability does on entry.
    /// </summary>
    private void UltraBurstNow(Battler b)
    {
        string form = UltraBurstFormOf(b.Pokemon!)!;
        Say(Text.Of("battle.ultraBurstLight", ("pokemon", b.Name)));
        TakeForm(b, form);
        burstUsed.Add(MegaKey(b));
        Say(Text.Of("battle.ultraBurst", ("pokemon", b.Name))).With(new UltraBursted(b.Place, form));
        b.Volatile.Announced &= EntryCheck.Item | EntryCheck.Shape;
        SwitchInChecks();
    }

    /// <summary>
    /// The move chosen as a Z-Move becomes it, once its user can move and has paid its PP (Showdown's <c>runMove</c>
    /// with <c>zMove</c>, then <c>useMoveInner</c> and <c>runZPower</c>): the Z-Power's line and the Z-Move spent for
    /// the trainer; a damaging move turns into its Z-Move, of the move's power as a Z-Move, its category and its
    /// priority, and is unleashed; a status move is used as its Z-Power version, the move itself with its bonus
    /// first, and like every Z-Move it goes through a protection. Nothing happens where the move has no Z-Move after
    /// all (an Encore chose another).
    /// </summary>
    private void BecomeZMove(MoveUse use, Battler? target)
    {
        var user = use.User;
        var p = user.Pokemon!;
        if (!Rules.ZMoves || !HasZRing(user) || zUsed.Contains(MegaKey(user)) || ZMoveOf(p, use.Move, pastPp: true) is not { } z) return;
        var own = use.Move.Data;
        zUsed.Add(MegaKey(user));
        zUses.Add(use);
        Say(Text.Of("battle.zPower", ("pokemon", user.Name))).With(new ZPowerUsed(user.Place, z.Name, own.Name));
        if (z == own)
        {
            // A status move's Z-Power version: "Z-" before its name, its bonus after its line (runZPower), Protect no bar
            use.Move = new Move(own.CopyWith(d => d.Flags &= ~MoveFlags.Protect), use.Move.CurrentPP);
            use.Own = own;
            use.Line = Say(Text.Of("battle.used", ("user", user.Name), ("move", Text.Of("battle.zMoveName", ("move", own.DisplayName)))));
            ZPower(user, own);
        }
        else
        {
            Say(Text.Of("battle.zUnleash", ("pokemon", user.Name)));
            // A type's Z-Move takes its power from the move, its category and its priority (getActiveZMove)
            var data = TypeZMoves.ContainsValue(z.Name)
                ? z.CopyWith(d =>
                {
                    d.Power = own.ZPower;
                    d.Category = own.Category;
                    d.Priority = own.Priority;
                })
                : z;
            use.Move = new Move(data, use.Move.CurrentPP);
            use.Own = data;
            use.Effect = EffectOf(data);
        }
        if (!user.IsActive) return;
        use.Targets = ResolveTargets(user, use.Move, target);
    }

    /// <summary>
    /// <c>runZPower</c> for a status move: its stats raised, HP restored, lowered stats put back, a critical-hit
    /// boost, the centre of attention, the heal waiting for whoever comes in next, or Curse's two ways.
    /// </summary>
    private void ZPower(Battler user, MoveData move)
    {
        var p = user.Pokemon!;
        if (move.ZBonus is not { } bonus || !user.IsActive) return;
        if (bonus.Stats != null)
        {
            foreach (var stat in bonus.Stats) ChangeStat(user, stat, bonus.Stages, user, false, By.Move);
            return;
        }
        switch (bonus.Effect)
        {
            case "Heal":
                HealWhole(user);
                break;
            case "HealReplacement":
                healReplacement[user.Place] = p;
                break;
            case "ClearNegativeBoost":
            {
                var lowered = p.StatStages.Where(s => s.Value < 0).Select(s => s.Key).ToList();
                if (lowered.Count == 0) break;
                foreach (var stat in lowered) p.StatStages[stat] = 0;
                var line = Say(Text.Of("battle.zClearedNegative", ("pokemon", user.Name)));
                foreach (var stat in lowered) line.With(new StageChanged(user.Place, stat, 0, Rose: true));
                break;
            }
            case "Crit2":
                if (user.Volatile.FocusEnergy) break;
                user.Volatile.FocusEnergy = true;
                Say(Text.Of("battle.zCritBoost", ("pokemon", user.Name)));
                break;
            case "Redirect":
                Field.Side(user.Side).FollowMe = user.Place;
                Say(Text.Of("battle.becameTheCenterOf", ("user", user.Name)));
                break;
            case "Curse":
                if (user.HasType(PokemonType.Ghost)) HealWhole(user);
                else ChangeStat(user, StatType.Attack, 1, user, false, By.Move);
                break;
        }
    }

    /// <summary>The Z-Power's whole heal, when there is HP to restore.</summary>
    private void HealWhole(Battler b)
    {
        var p = b.Pokemon!;
        if (p.CurrentHP < p.MaxHP) RestoreHp(b, p.MaxHP - p.CurrentHP, Text.Of("battle.zRestoredHp", ("pokemon", b.Name)));
    }

    /// <summary>Memento's and Parting Shot's Z-Power: whoever has come into the place since is healed whole (healreplacement's onSwitchIn).</summary>
    private void HealReplacements()
    {
        if (healReplacement.Count == 0) return;
        foreach (var (place, setter) in healReplacement.ToList())
        {
            var b = At(place);
            if (!b.IsActive || b.Pokemon == setter) continue;
            healReplacement.Remove(place);
            var p = b.Pokemon!;
            if (p.CurrentHP < p.MaxHP) RestoreHp(b, p.MaxHP - p.CurrentHP, Text.Of("battle.zHealReplacement", ("pokemon", b.Name)));
        }
    }

    /// <summary>Every stat but accuracy and evasion raised by so many stages, one line each (Extreme Evoboost, Clangorous Soulblaze).</summary>
    private void RaiseAll(Battler b, int stages)
    {
        foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed })
            ChangeStat(b, stat, stages, b, false, By.Move);
    }

    /// <summary>
    /// A Z-Move has come through a protection of its target (Protect and its kin's <c>onTryHit</c>, Mat Block's, a
    /// Quick Guard against a move that goes first, a Wide Guard against one at more than one foe: <c>zBrokeProtect</c>).
    /// </summary>
    private bool ZBrokeProtect(MoveUse use, Battler t)
    {
        if (!zUses.Contains(use) || t == use.User) return false;
        if (t.Turn.Protecting) return true;
        var guards = GuardsOf(t.Side);
        return guards.HasFlag(SideGuard.MatBlock) && use.Move.Category != MoveCategory.Status
            || guards.HasFlag(SideGuard.QuickGuard) && use.Move.Priority > 0
            || guards.HasFlag(SideGuard.WideGuard) && use.Data.Target is MoveTarget.AllFoes or MoveTarget.AllOthers;
    }

    /// <summary>A Z-Move's damage through a protection is a quarter (<c>modifyDamage</c>'s last step, at least 1), and the line says so.</summary>
    private DamageCalculator.DamageResult ThroughProtection(MoveUse use, Battler t, DamageCalculator.DamageResult result)
    {
        if (!ZBrokeProtect(use, t)) return result;
        Say(Text.Of("battle.zBrokeProtect", ("target", t.Name)));
        result.Damage = Math.Max(1, Formulas.Modify(result.Damage, 1024));
        return result;
    }
}
