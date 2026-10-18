using PokemonPlatinumEngine.Core;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

// The last 23 abilities of Generations 5 to 9 (plan 06 · R27b), each by Pokémon Showdown's rule (data/abilities.ts and
// data/conditions.ts at the pinned commit). What these classes can't reach from their hooks is the core's
// (BattleCore.Abilities2.cs): a berry's own course (Cheek Pouch, Ripen, Cud Chew, Symbiosis), an ally's veil, a switch in
// the middle of a turn (Wimp Out, Emergency Exit), Dancer, Commander, Opportunist, Receiver and Ball Fetch.

/// <summary>Unnerve: as it comes in it says so, and while it stands its foes eat no berry (<see cref="LaterAbilities.Unnerved"/>).</summary>
internal sealed class Unnerve : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self) =>
        ctx.Announce(Text.Of("battle.unnerve", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
}

/// <summary>
/// As One: Unnerve, and Chilling Neigh for the Ice Rider or Grim Neigh for the Shadow Rider (Showdown's asoneglastrier
/// and asonespectrier, one ability in the data): both said as it comes in.
/// </summary>
internal sealed class AsOne : BattleEffect
{
    private readonly KnockOutRaises ice = new(StatType.Attack), shadow = new(StatType.SpAttack);

    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        ctx.Announce(Text.Of("battle.asOne", ("pokemon", self.Name)));
        ctx.Announce(Text.Of("battle.unnerve", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
    }

    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits) =>
        (self.Pokemon?.Form == "Calyrex-Shadow" ? shadow : ice).AfterAttacking(ctx, self, move, hits);
}

/// <summary>Illusion: the disguise is taken before it is sent out (<see cref="BattleCore"/>'s <c>IllusionFor</c>), and a damaging hit breaks it.</summary>
internal sealed class Illusion : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (self.Volatile.Illusion == null) return;
        self.Volatile.Illusion = null;
        ctx.Announce(Text.Of("battle.illusionEnded", ("pokemon", self.Name)));
    }
}

/// <summary>Imposter: as it comes in it transforms into the foe across from it (Showdown's imposter onSwitchIn).</summary>
internal sealed class Imposter : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (ctx is BattleCore core) core.Impersonate(self);
    }
}

/// <summary>Neutralizing Gas: as it comes in it fills the field, and every other ability but those that can't be suppressed does nothing while it stands (<see cref="LaterAbilities.Neutralized"/>).</summary>
internal sealed class NeutralizingGas : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (ctx is BattleCore core) core.GasFills(self);
    }
}

/// <summary>
/// Supreme Overlord: as it comes in, it counts the fallen of its party, five at most (Showdown's onStart, which counts
/// <c>side.totalFainted</c>: here the party's fainted, so one that fell before the battle counts as in the games); the
/// power's bonus is the damage formula's.
/// </summary>
internal sealed class SupremeOverlord : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        var p = self.Pokemon!;
        self.Volatile.Fallen = System.Math.Min(5, self.Roster?.Members.Count(m => m != p && m.IsFainted) ?? 0);
    }
}

/// <summary>Cud Chew: a berry it ate is chewed again at the end of the next turn (Showdown's onResidual, order 28.2).</summary>
internal sealed class CudChew : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        if (ctx is BattleCore core) core.ChewAgain(self);
    }
}

/// <summary>Battle Bond (generation 9): a Greninja of the bond that knocks out a foe, with foes still to come, raises its Attack, Sp. Atk and Speed a stage, once a battle.</summary>
internal sealed class BattleBond : BattleEffect
{
    public override void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits)
    {
        if (ctx is not BattleCore core || !hits.Any(h => h.Target != self && h.Target.Pokemon is { IsFainted: true })) return;
        core.BondWith(self);
    }
}

/// <summary>Power Construct: at the end of a turn a Zygarde at half its HP or less becomes its Complete Forme, for the rest of the battle (Showdown's onResidual, order 29).</summary>
internal sealed class PowerConstruct : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        if (ctx is BattleCore core) core.Construct(self);
    }
}

/// <summary>Pastel Veil: no poison takes hold of it or its allies (the allies' is the core's <c>VeiledFrom</c>), and as it comes in it cures theirs.</summary>
internal sealed class PastelVeil : BattleEffect
{
    public override bool BlocksStatus(Battler self, StatusCondition status) => status is StatusCondition.Poison or StatusCondition.Toxic;

    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        foreach (var ally in ctx.Everyone.Where(b => b.Side == self.Side).ToList())
            if (ally.Pokemon!.Status is StatusCondition.Poison or StatusCondition.Toxic)
                ctx.CureStatus(ally, Text.Of("battle.curedIts", ("pokemon", ally.Name), ("item", ctx.AbilityOf(self)), ("condition", BattleText.StatusName(ally.Pokemon.Status))));
    }
}

/// <summary>
/// Wind Rider: a wind move is taken in and raises its Attack, and so does a Tailwind blowing behind it, as it comes in
/// under one and as one starts (Showdown's windrider onStart and onSideConditionStart: R27 had the move alone).
/// </summary>
internal sealed class WindRider : BattleEffect
{
    private readonly AbsorbRaises wind = new(m => LaterAbilities.IsWind(m.Data) && m.Data.Target is not (MoveTarget.User or MoveTarget.UserSide), StatType.Attack, 1);

    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness) =>
        wind.AbsorbsMove(ctx, self, attacker, move, effectiveness);

    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        if (ctx.Field.Side(self.Side).Tailwind) ctx.ChangeStat(self, StatType.Attack, 1, self);
    }
}

internal static partial class LaterAbilities
{
    /// <summary>The abilities nothing suppresses (Showdown's <c>cantsuppress</c>), as the data names them: Neutralizing Gas passes them by.</summary>
    public static readonly HashSet<string> CantSuppress = new()
    {
        "As One", "Battle Bond", "Comatose", "Disguise", "Gulp Missile", "Ice Face", "Multitype", "Power Construct", "RKS System",
        "Schooling", "Shields Down", "Stance Change", "Tera Shift", "Zen Mode", "Zero to Hero"
    };

    /// <summary>The abilities Receiver and Power of Alchemy can't take on (Showdown's <c>noreceiver</c>), as the data names them.</summary>
    public static readonly HashSet<string> NoReceiver = new()
    {
        "As One", "Battle Bond", "Comatose", "Commander", "Disguise", "Embody Aspect", "Flower Gift", "Forecast", "Hunger Switch", "Ice Face",
        "Illusion", "Imposter", "Multitype", "Neutralizing Gas", "Poison Puppeteer", "Power Construct", "Power of Alchemy", "Protosynthesis",
        "Quark Drive", "Receiver", "RKS System", "Schooling", "Shields Down", "Stance Change", "Teraform Zero", "Tera Shell", "Tera Shift",
        "Trace", "Wonder Guard", "Zen Mode", "Zero to Hero"
    };

    /// <summary>
    /// Neutralizing Gas is on the field and this one's ability does nothing (Showdown's <c>ignoringAbility</c>): not one
    /// that can't be suppressed, not the gas itself, not behind an Ability Shield, not a Tatsugiri in command; the gas of
    /// a holder that has transformed or whose own is suppressed fills nothing.
    /// </summary>
    public static bool Neutralized(Battler b)
    {
        if (b.Pokemon?.AbilityName is not { } own || own == "Neutralizing Gas" || CantSuppress.Contains(own)) return false;
        if (b.Pokemon.HeldItem?.Name == "Ability Shield" || b.Volatile.Commanding != null) return false;
        foreach (var o in b.Field!.Battlers())
            if (o != b && o.IsActive && o.Pokemon!.AbilityName == "Neutralizing Gas" && !o.Volatile.AbilitySuppressed && !o.Volatile.Transformed) return true;
        return false;
    }

    /// <summary>A foe with Unnerve or As One stands across from it: it eats no berry (Showdown's <c>onFoeTryEatItem</c>).</summary>
    public static bool Unnerved(Battler b)
    {
        if (b.Field == null) return false;
        foreach (var o in b.Field.Battlers())
            if (o.Side != b.Side && o.IsActive && o.Ability?.Name is "Unnerve" or "As One") return true;
        return false;
    }
}
