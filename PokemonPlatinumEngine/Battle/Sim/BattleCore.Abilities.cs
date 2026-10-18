using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The later abilities that the core reads by name (plan 06 · R27; Pokémon Showdown's data/abilities.ts at the pinned
// commit): a move's priority (Prankster, Gale Wings, Triage), its type and what it carries (Protean and Libero, the
// -ate abilities, Liquid Voice, Sheer Force, Long Reach, Parental Bond), a move stopped before it lands (a Dark type
// against a Prankster's move, Dazzling and its kin, Good as Gold, Telepathy, Magic Bounce, Unseen Fist past Protect),
// what answers a stat lowered by another (Defiant, Competitive, Mirror Armor), and the effects' own helpers. Each is
// a line or two in the course it changes, where Showdown's event has it.
public sealed partial class BattleCore
{
    /// <summary>Who has had an ability's once-a-battle effect (Intrepid Sword, Dauntless Shield, Supersweet Syrup; Showdown's generation 9 flags on the Pokémon).</summary>
    private readonly HashSet<(Pokemon, string)> onceABattle = new();

    /// <summary>Whether this is the first time the holder's ability acts in this battle; noted as asked.</summary>
    internal bool FirstTimeThisBattle(Battler b) => b.Pokemon is { } p && b.Ability is { } a && onceABattle.Add((p, a.Name));

    // ---------------------------------------------------------------- the turn's order

    /// <summary>
    /// What the user's ability adds to its move's priority (onModifyPriority): Prankster a step for a status move, Gale
    /// Wings a step for a Flying move at full HP (generation 7 on), Triage three for a move that heals.
    /// </summary>
    private static int AbilityPriority(Battler user, Move? move)
    {
        if (move == null || user.Pokemon is not { } p) return 0;
        return user.Ability?.Name switch
        {
            "Prankster" when move.Category == MoveCategory.Status => 1,
            "Gale Wings" when move.Type == PokemonType.Flying && p.CurrentHP >= p.MaxHP => 1,
            "Triage" when LaterAbilities.Healing.Contains(move.Data.Name) => 3,
            _ => 0
        };
    }

    /// <summary>
    /// Quick Draw (onFractionalPriority): three times in ten a damaging move goes first among moves of its priority,
    /// as a Quick Claw's; Mycelium Might: a status move goes last among them, as a Lagging Tail's.
    /// </summary>
    private void AbilitiesInTheOrder(Act act)
    {
        if (act.Choice.Kind != ChoiceKind.Fight || act.Move == null) return;
        bool status = act.Move.Category == MoveCategory.Status;
        if (Has(act.User, "Quick Draw") && !status && rng.Roll(RollKind.AbilityChance, 100) < 30) act.MovesFirst = true;
        if (Has(act.User, "Mycelium Might") && status) act.Lags = true;
    }

    // ---------------------------------------------------------------- the move as its user makes it

    /// <summary>Propeller Tail and Stalwart: its moves go where they were aimed, past Follow Me, Lightning Rod and Storm Drain (<c>tracksTarget</c>).</summary>
    private static bool KeepsItsAim(Battler user) => user.Ability?.Name is "Propeller Tail" or "Stalwart";

    /// <summary>
    /// The user's ability has its say on the move it is about to make (onModifyType, onPrepareHit, onModifyMove): the
    /// -ate abilities make a Normal move of their type (and a fifth stronger), Liquid Voice a sound move Water,
    /// Long Reach takes away the touch, Sheer Force the side effects (and adds 5325/4096 to the power), and Protean
    /// or Libero turn the user into the move's type, once a stay on the field.
    /// </summary>
    private void ShapedByAbility(MoveUse use)
    {
        var user = use.User;
        string? ability = user.Ability?.Name;
        if (use.Move.Data == StruggleData)
        {
            user.Turn.AteBoosted = user.Turn.SheerForced = user.Turn.ParentalChild = false;
            return;
        }
        var shaped = ShapedBy(user, use.Move.Data);
        if (shaped != use.Move.Data) use.Move = new Move(shaped, use.Move.CurrentPP);
        if (ability == null) return;

        // Protean and Libero: before the move, its user becomes its type (once a stay, from generation 9)
        if (ability is "Protean" or "Libero" && !user.Volatile.ProteanUsed && user.Pokemon!.Terastallized == null)
        {
            var type = use.Move.Type;
            if (user.Types.Count == 1 && user.Types[0] == type) return;
            user.Volatile.ProteanUsed = true;
            user.Volatile.Types = new List<PokemonType> { type };
            Say(Text.Of("battle.transformedIntoTheType", ("user", user.Name), ("pick", type)));
        }
    }

    /// <summary>
    /// The move as the user's ability makes it, its marks for the formula set on the user's turn (a -ate ability's fifth,
    /// Sheer Force's power): what <see cref="ShapedByAbility"/> hands the course, and what the oracle hands the formula
    /// (plan 06 · R27b), so both see the same move.
    /// </summary>
    internal static MoveData ShapedBy(Battler user, MoveData data)
    {
        user.Turn.AteBoosted = user.Turn.SheerForced = user.Turn.ParentalChild = false;
        string? ability = user.Ability?.Name;
        if (ability == null) return data;
        PokemonType? ate = ability switch
        {
            "Aerilate" => PokemonType.Flying,
            "Pixilate" => PokemonType.Fairy,
            "Refrigerate" => PokemonType.Ice,
            "Galvanize" => PokemonType.Electric,
            "Dragonize" => PokemonType.Dragon,
            _ => null
        };
        if (ate is { } to && data.Type == PokemonType.Normal && !TypedByItsOwnRule(data) && data.Kind == MoveKind.Standard)
        {
            user.Turn.AteBoosted = true;
            return data.OfType(to);
        }
        if (ability == "Liquid Voice" && (data.Flags & MoveFlags.Sound) != 0 && data.Type != PokemonType.Water) return data.OfType(PokemonType.Water);
        if (ability == "Long Reach" && data.MakesContact) return data.CopyWith(d => d.Flags &= ~MoveFlags.Contact);
        if (ability == "Sheer Force" && DamageCalculator.HasSecondaries(data))
        {
            user.Turn.SheerForced = true;
            return data.CopyWith(WithoutSecondaries);
        }
        return data;
    }

    /// <summary>The moves whose type is their own rule's, which a -ate ability leaves alone (Showdown's <c>noModifyType</c>).</summary>
    private static bool TypedByItsOwnRule(MoveData move) =>
        move.Name is "Judgment" or "Multi-Attack" or "Natural Gift" or "Revelation Dance" or "Techno Blast" or "Terrain Pulse" or "Weather Ball"
            or "Hidden Power" or "Tera Blast" or "Aura Wheel" or "Raging Bull" or "Ivy Cudgel";

    /// <summary>What Sheer Force takes away: the chances of a condition, a flinch, confusion and a stat change on the target, and a change of its user's that isn't sure.</summary>
    private static void WithoutSecondaries(MoveData d)
    {
        d.InflictStatus = StatusCondition.None;
        d.StatusChancePercent = 0;
        d.FlinchChancePercent = 0;
        d.ConfuseChancePercent = 0;
        d.EffectChance = 0;
        if (d.TargetStatChange != null && (!d.StatChangeTargetSelf || d.StatChangeChancePercent < 100 || d.StatStageAmount > 0))
        {
            d.TargetStatChange = null;
            d.StatChangeChancePercent = 0;
            d.AlsoChangesStats = null;
        }
    }

    /// <summary>
    /// Parental Bond (generation 7 on): a damaging move of one hit at one target hits twice, the second a quarter as
    /// strong (Showdown's <c>onPrepareHit</c>: not a move of several hits, a spread move, a charge or a move of fixed damage).
    /// </summary>
    private void ParentalBond(MoveUse use)
    {
        if (!Has(use.User, "Parental Bond") || use.StrikeCount != 1 || use.Targets.Count != 1 || use.Effect.Deals != null || use.Effect.SelfKo) return;
        if (use.Move.Category == MoveCategory.Status || use.Move.Data.Kind != MoveKind.Standard) return;
        use.StrikeCount = 2;
        parentalBond = true;
    }

    /// <summary>The move being struck is Parental Bond's two hits (its count is said as any move's of several hits).</summary>
    private bool parentalBond;

    /// <summary>The hit about to be struck is Parental Bond's second.</summary>
    private void StrikingAs(MoveUse use, int n)
    {
        use.User.Turn.ParentalChild = parentalBond && n == 2;
        if (n == use.StrikeCount && n > 1) parentalBond = false;
    }

    // ---------------------------------------------------------------- a move stopped before it lands

    /// <summary>
    /// The target's side stops the move before it lands (onTryHit, onFoeTryMove): a Dark type the move a Prankster made
    /// faster (generation 7 on), Dazzling, Queenly Majesty and Armor Tail against a move of priority from across the
    /// field, Good as Gold against another's status move, and Telepathy against its ally's damaging move. Mold Breaker
    /// goes past all but the Dark type.
    /// </summary>
    private bool StoppedByAbility(MoveUse use, Battler target, MoveHit hit)
    {
        var user = use.User;
        if (target == user) return false;
        bool foe = target.Side != user.Side;
        var move = use.Move;
        if (foe && move.Category == MoveCategory.Status && Has(user, "Prankster") && target.HasType(PokemonType.Dark) && use.Called == false)
        {
            hit.Missed = true;
            hit.FailLine = Text.Of("battle.itDoesntAffect", ("target", target.Name));
            return true;
        }
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility) || DamageCalculator.IgnoresAbilities(move.Data);
        if (breaks) return false;
        // Aroma Veil over the target's side (plan 06 · R27b)
        if (AromaVeils(use, target, hit)) return true;
        if (foe && move.Priority + AbilityPriority(user, use.Own == null ? move : new Move(use.Own)) > 0 && move.Data.Target is not (MoveTarget.FoeSide or MoveTarget.Field or MoveTarget.AllPokemon)
            && SlotsOf(target.Side).FirstOrDefault(b => b.IsActive && b.Ability?.Name is "Dazzling" or "Queenly Majesty" or "Armor Tail") is { } guard)
        {
            hit.Missed = true;
            hit.FailLine = Text.Of("battle.cannotUse", ("user", user.Name), ("move", move.DisplayName), ("pokemon", guard.Name), ("ability", AbilityOf(guard)));
            return true;
        }
        if (move.Category == MoveCategory.Status && Has(target, "Good as Gold"))
        {
            hit.Missed = true;
            hit.FailLine = Text.Of("battle.blocks", ("pokemon", target.Name), ("ability", AbilityOf(target)), ("move", move.DisplayName));
            return true;
        }
        if (!foe && move.Category != MoveCategory.Status && Has(target, "Telepathy"))
        {
            hit.Missed = true;
            hit.FailLine = Text.Of("battle.telepathy", ("target", target.Name), ("ability", AbilityOf(target)));
            return true;
        }
        return false;
    }

    /// <summary>Unseen Fist and Piercing Drill: a move that touches goes through Protect and its kin (not the side's guards).</summary>
    private static bool PiercesProtect(MoveUse use) => use.Data.MakesContact && use.User.Ability?.Name is "Unseen Fist" or "Piercing Drill";

    /// <summary>Infiltrator: its hits go past a Substitute (Showdown's <c>infiltrates</c>), as they go past the screens.</summary>
    private static bool Infiltrates(MoveUse use) => DamageCalculator.Infiltrates(use.User) && use.Move.Data.Name is not ("Transform");

    /// <summary>
    /// Magic Bounce (onTryHit): a status move that can be reflected, aimed at the holder or its side by another, goes
    /// back to whoever used it, as Magic Coat sends it; not one that was bounced already.
    /// </summary>
    private bool BouncedByAbility(MoveUse use)
    {
        var user = use.User;
        var data = use.Own ?? use.Data;
        if (use.Called || !data.Flags.HasFlag(MoveFlags.Reflectable)) return false;
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        if (breaks) return false;
        var bouncer = use.Targets.FirstOrDefault(t => t != user && t.IsActive && Has(t, "Magic Bounce"))
                      ?? (data.Target == MoveTarget.FoeSide ? SlotsOf(Other(user.Side)).FirstOrDefault(t => t.IsActive && Has(t, "Magic Bounce")) : null);
        if (bouncer == null) return false;
        UseAs(bouncer, data, user, Say(Text.Of("battle.bouncedBack", ("user", user.Name), ("move", use.Move.DisplayName), ("pokemon", bouncer.Name), ("ability", AbilityOf(bouncer)))));
        return true;
    }

    /// <summary>
    /// The later abilities' say in the accuracy roll (onModifyAccuracy, onSourceModifyAccuracy): Victory Star a tenth
    /// more for its holder and its allies (4506/4096), and Wonder Skin holds a status move at the target to 50.
    /// </summary>
    private int LaterAccuracy(MoveUse use, Battler target, int rate)
    {
        var user = use.User;
        if (SlotsOf(user.Side).Any(b => b.IsActive && Has(b, "Victory Star"))) rate = rate * 4506 / 4096;
        bool breaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        if (!breaks && use.Move.Category == MoveCategory.Status && use.Move.Accuracy > 50 && Has(target, "Wonder Skin") && target != user)
            rate = rate * 50 / use.Move.Accuracy;
        return rate;
    }

    // ---------------------------------------------------------------- a stat lowered by another

    /// <summary>
    /// Mirror Armor (onTryBoost): a stage another would take from the holder is taken from the one who tried, and the
    /// holder's is left alone. Not back at another with Mirror Armor.
    /// </summary>
    private bool MirrorsTheDrop(Battler target, Battler source, StatType stat, int amount, bool announceFailure, By how)
    {
        // (Not a stage already at its lowest: plan 06 · R27b's audit, Showdown's mirrorarmor onTryBoost)
        if (!Has(target, "Mirror Armor") || !source.IsActive || Has(source, "Mirror Armor") || target.Pokemon!.StatStages.GetValueOrDefault(stat) <= -6) return false;
        Say(Text.Of("battle.mirrorArmor", ("pokemon", target.Name), ("ability", AbilityOf(target))));
        ChangeStat(source, stat, amount, target, announceFailure, how);
        return true;
    }

    /// <summary>Defiant and Competitive (onAfterEachBoost): a stage taken by a foe raises the holder's Attack or Sp. Atk two.</summary>
    private void AnswersTheDrop(Battler target, Battler source)
    {
        if (!target.IsActive || source.Side == target.Side) return;
        if (Has(target, "Defiant")) ChangeStat(target, StatType.Attack, 2, target);
        else if (Has(target, "Competitive")) ChangeStat(target, StatType.SpAttack, 2, target);
    }

    /// <summary>Guard Dog (onDragOut, onTryBoost): an Intimidate raises its Attack instead (and Roar and its kin can't move it: <c>GuardDog.HoldsItsGround</c>).</summary>
    private bool GuardDogStands(Battler target, Battler source, StatType stat, By how)
    {
        if (!Has(target, "Guard Dog") || stat != StatType.Attack || how != By.Ability || !Has(source, "Intimidate")) return false;
        ChangeStat(target, StatType.Attack, 1, target);
        return true;
    }

    /// <summary>Soul-Heart (onAnyFaint): every Pokémon that faints raises its holder's Sp. Atk a stage.</summary>
    private void SoulHeart(Battler down)
    {
        foreach (var b in AllBattlers.Where(b => b != down && b.IsActive && Has(b, "Soul-Heart")).ToList())
            ChangeStat(b, StatType.SpAttack, 1, b);
    }

    /// <summary>Corrosion: its own moves and abilities can poison a Steel or a Poison type.</summary>
    private static bool Corrodes(Battler? source) => source?.Ability?.Name == "Corrosion";

    /// <summary>Heavy Metal doubles the holder's weight and Light Metal halves it (onModifyWeight).</summary>
    internal static float AbilityWeight(Battler b, float kg) => b.Ability?.Name switch
    {
        "Heavy Metal" => kg * 2f,
        // (halved in whole hectograms, as Showdown truncates it: plan 06 · R27b's audit)
        "Light Metal" => Math.Max(1, (int)Math.Round(kg * 10f) / 2) / 10f,
        _ => kg
    };

    // ---------------------------------------------------------------- the effects' helpers

    /// <summary>
    /// Mummy and Lingering Aroma give whoever touches the holder their ability, Wandering Spirit swaps the two
    /// (Showdown's <c>cantsuppress</c> abilities, and for a swap <c>failskillswap</c>, stay); both put back as they leave.
    /// </summary>
    internal void CatchAbility(Battler holder, Battler toucher, bool swaps)
    {
        var mine = holder.Pokemon!;
        var theirs = toucher.Pokemon!;
        string? own = theirs.AbilityName;
        if (own == null || own == mine.AbilityName || CantBeReplaced.Contains(own) || (swaps && (NotHandedOn.Contains(own) || own == "Wonder Guard"))) return;
        string given = mine.AbilityName!;
        KeepAbility(Keep(toucher), theirs);
        theirs.AbilityName = given;
        if (swaps)
        {
            KeepAbility(Keep(holder), mine);
            mine.AbilityName = own;
            _ = AbilityOf(holder);
            Say(Text.Of("battle.swappedAbilities", ("pokemon", holder.Name), ("target", toucher.Name)));
            return;
        }
        Say(Text.Of("battle.abilityCaught", ("target", toucher.Name), ("ability", AbilityOf(toucher))));
    }

    /// <summary>Pickpocket and Magician: the taker, holding nothing, takes the other's item, unless it clings or Sticky Hold keeps it. True when it did.</summary>
    internal bool TakeItem(Battler taker, Battler from)
    {
        var p = taker.Pokemon!;
        var q = from.Pokemon!;
        if (p.HeldItem != null || !CanTake(from) || knockedOff.ContainsKey(p)) return false;
        if (q.AbilityName == "Multitype" || q.HeldItem?.Name == "Griseous Orb" || ClingsTo(q.HeldItem, q) || ClingsTo(q.HeldItem, p)) return false;
        if (Has(from, "Sticky Hold")) return false;
        var item = q.HeldItem!;
        q.HeldItem = null;
        p.HeldItem = item;
        _ = ItemOf(taker);
        Say(Text.Of("battle.stole", ("user", taker.Name), ("target", from.Name), ("item", ItemOf(from, item))));
        return true;
    }

    /// <summary>Toxic Debris: a layer of Toxic Spikes round the attacker's side, two at most, with the event its look follows.</summary>
    internal void ScatterToxicSpikes(Battler holder, BattleSide side)
    {
        var state = Field.Side(side);
        if (state.ToxicSpikes >= 2) return;
        state.ToxicSpikes++;
        Say(Text.Of("battle.toxicDebris", ("pokemon", holder.Name), ("ability", AbilityOf(holder)))).With(Now(side, SideCondition.ToxicSpikes));
    }

    /// <summary>Screen Cleaner: every Reflect, Light Screen and Aurora Veil on both sides is gone, each with its event.</summary>
    internal void CleanTheScreens(Battler holder)
    {
        var said = new List<SideConditionChanged>();
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
        {
            var state = Field.Side(side);
            if (state.Reflect)
            {
                state.ReflectTurns = 0;
                said.Add(Now(side, SideCondition.Reflect));
            }
            if (state.LightScreen)
            {
                state.LightScreenTurns = 0;
                said.Add(Now(side, SideCondition.LightScreen));
            }
            if (state.AuroraVeil)
            {
                state.AuroraVeilTurns = 0;
                said.Add(Now(side, SideCondition.AuroraVeil));
            }
        }
        if (said.Count == 0) return;
        var line = Say(Text.Of("battle.screenCleaner", ("pokemon", holder.Name), ("ability", AbilityOf(holder))));
        foreach (var e in said) line.With(e);
    }

    /// <summary>Curious Medicine: a Pokémon's stat changes are gone.</summary>
    internal void ClearStages(Battler b, string line)
    {
        var p = b.Pokemon!;
        var said = Say(line);
        foreach (var stat in p.StatStages.Keys.ToList())
        {
            if (p.StatStages[stat] == 0) continue;
            p.StatStages[stat] = 0;
            said.With(new StageChanged(b.Place, stat, 0, Rose: false));
        }
    }

    /// <summary>Costar: the holder takes its ally's stat changes and its critical-hit stages.</summary>
    internal void CopyStages(Battler holder, Battler ally, string line)
    {
        var p = holder.Pokemon!;
        var said = Say(line);
        foreach (var (stat, stage) in ally.Pokemon!.StatStages.ToList())
        {
            if (p.StatStages.GetValueOrDefault(stat) == stage) continue;
            bool rose = stage > p.StatStages.GetValueOrDefault(stat);
            p.StatStages[stat] = stage;
            said.With(new StageChanged(holder.Place, stat, stage, rose));
        }
        holder.Volatile.FocusEnergy = ally.Volatile.FocusEnergy;
        holder.Volatile.DragonCheer = ally.Volatile.DragonCheer;
    }
}
