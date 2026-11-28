using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

// The abilities of Carpathia's starters' last stages and four legendaries (plan 46 · CP10; docs/mechanics/rulings.md,
// "Carpathia's signature moves and abilities"). Ours, so there is no original to read: each is the proposal's sentence
// (docs/plans/decisions/CP-proposals-starters-legendaries.md) carried out where the rules already bite. Anvil, Echo Pouch
// and Three Heads are read by name in the core (BattleCore.Carpathia.cs), as Parental Bond is; Noonday is the strong
// sun's holder (StrongWeatherBringer).

/// <summary>Forest Hush: a move that makes sound fails against it while it is at full HP (Soundproof's rule, and a wound lets the sound in).</summary>
internal sealed class ForestHush : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if ((move.Data.Flags & MoveFlags.Sound) == 0 || attacker == self) return false;
        var p = self.Pokemon!;
        if (p.CurrentHP < p.MaxHP) return false;
        ctx.Announce(Text.Of("battle.blocks", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self)), ("move", move.DisplayName)));
        return true;
    }
}

/// <summary>Eclipse: as it comes in the Vârcolac's tale darkens the field for the tale's five turns (accuracy down, Dark and Ghost up).</summary>
internal sealed class Eclipse : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self) =>
        ctx.TellTaleFromAbility(self, "varcolac", Text.Of("battle.eclipseFalls", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
}

/// <summary>War Cry: at the end of each turn it is on the field its team's Attack and Sp. Atk rise a stage, three turns at most.</summary>
internal sealed class WarCry : BattleEffect
{
    public const int MostRallies = 3;

    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        var v = self.Volatile;
        if (v.WarCries >= MostRallies) return;
        v.WarCries++;
        ctx.Announce(Text.Of("battle.warCry", ("pokemon", self.Name), ("ability", ctx.AbilityOf(self))));
        foreach (var ally in ctx.Everyone.Where(b => b.Side == self.Side && b.IsActive).ToList())
        {
            ctx.ChangeStat(ally, StatType.Attack, 1, self);
            ctx.ChangeStat(ally, StatType.SpAttack, 1, self);
        }
    }
}
