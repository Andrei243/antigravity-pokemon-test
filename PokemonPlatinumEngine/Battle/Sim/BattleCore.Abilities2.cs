using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The last 23 later abilities that the core reads by name (plan 06 · R27b; Pokémon Showdown's data/abilities.ts and
// data/conditions.ts at the pinned commit), and the fixes R27b's audit found in R27's: a berry's own course (Cheek Pouch,
// Ripen, Cud Chew, Symbiosis: what ConsumeItem notes, Digest carries out once the berry's own line is said), the allies'
// veils (Aroma Veil, Flower Veil, and Sweet Veil's and Pastel Veil's reach to the allies), a switch in the middle of a
// turn (Wimp Out, Emergency Exit), a move danced again (Dancer), Commander, Opportunist, Receiver and Power of Alchemy,
// Neutralizing Gas's coming and going, Illusion's disguise, Imposter's Transform, Battle Bond, Power Construct, Ball Fetch.
public sealed partial class BattleCore
{
    // ---------------------------------------------------------------- Illusion

    /// <summary>
    /// Illusion (onBeforeSwitchIn): the name of the last of its party behind it that can still fight, which it looks
    /// like as it is sent out; none when nobody behind it can (its party's order is the place's roster's).
    /// </summary>
    private static string? IllusionFor(Battler place, Pokemon incoming)
    {
        if (incoming.AbilityName != "Illusion" || place.Roster is not { } roster) return null;
        int at = roster.Members.IndexOf(incoming);
        if (at < 0) return null;
        for (int i = roster.Members.Count - 1; i > at; i--)
            if (!roster.Members[i].IsFainted) return roster.Members[i].DisplayName;
        return null;
    }

    /// <summary>The name a Pokémon is sent out under: its disguise's under an Illusion.</summary>
    private static string ShownAs(Battler place, Pokemon incoming) => IllusionFor(place, incoming) ?? incoming.DisplayName;

    /// <summary>A Pokémon with Illusion takes its disguise as it comes in.</summary>
    private static void TakeTheIllusion(Battler place)
    {
        if (place.Pokemon is { } p) place.Volatile.Illusion = IllusionFor(place, p);
    }

    // ---------------------------------------------------------------- Imposter

    private static readonly MoveData TransformData = MoveDatabase.Get("Transform");

    /// <summary>Imposter: the foe across from it (in a double battle the one diagonally opposite), unless it can't be copied.</summary>
    internal void Impersonate(Battler self)
    {
        var foes = SlotsOf(Other(self.Side)).ToList();
        if (foes.Count == 0 || self.Volatile.Transformed || self.Volatile.Illusion != null) return;
        var t = foes[Math.Clamp(foes.Count - 1 - self.Slot, 0, foes.Count - 1)];
        if (!t.IsActive || t.IsElsewhere || t.Volatile.Transformed || t.HasSubstitute || t.Volatile.Illusion != null) return;
        var use = new MoveUse { User = self, Move = new Move(TransformData), Effect = EffectOf(TransformData) };
        Transform(use, t);
    }

    // ---------------------------------------------------------------- Neutralizing Gas

    /// <summary>The gas is on the field: when it goes, the abilities it held act as their holders came in.</summary>
    private bool gasOn;

    /// <summary>Neutralizing Gas comes in: it says so, and every disguise on the field falls away.</summary>
    internal void GasFills(Battler self)
    {
        gasOn = true;
        Say(Text.Of("battle.neutralizingGas"));
        foreach (var b in AllBattlers.Where(b => b != self && b.IsActive).ToList())
        {
            // What the others did coming in is to be done again once the gas goes (Showdown's onEnd runs each one's Start)
            b.Volatile.Announced &= EntryCheck.Item | EntryCheck.Shape;
            if (b.Volatile.Illusion == null) continue;
            b.Volatile.Illusion = null;
            Say(Text.Of("battle.illusionEnded", ("pokemon", b.Name)));
        }
    }

    /// <summary>
    /// The last gas has gone (its holder left, fainted or lost its ability): it says so, before the abilities it held
    /// act as their holders came in (their entry checks were cleared as it came, and none acted while it stood).
    /// </summary>
    private void GasGoes()
    {
        if (!gasOn || AllBattlers.Any(b => b.IsActive && b.Pokemon!.AbilityName == "Neutralizing Gas" && !b.Volatile.AbilitySuppressed && !b.Volatile.Transformed)) return;
        gasOn = false;
        Say(Text.Of("battle.neutralizingGasGone"));
    }

    // ---------------------------------------------------------------- a berry's own course

    /// <summary>The items used up since the last <see cref="Digest"/>, and whose: Cheek Pouch, Cud Chew and Symbiosis answer them.</summary>
    private readonly List<(Battler Holder, ItemData Item)> eaten = new();

    /// <summary>Ripen: the Pokémon whose berry is being eaten, so what the berry gives it is twice as much (Showdown's onTryHeal and onChangeBoost).</summary>
    private Battler? ripening;

    /// <summary>A berry is being chewed again: Cud Chew doesn't count it as eaten.</summary>
    private bool chewing;

    /// <summary>ConsumeItem's note of an item used up; a berry eaten with Ripen says so before the berry's own line.</summary>
    private void Ate(Battler holder, ItemData item)
    {
        eaten.Add((holder, item));
        if (item.Pocket != ItemPocket.Berries || !Has(holder, "Ripen")) return;
        ripening = holder;
        Say(Text.Of("battle.ripen", ("pokemon", holder.Name), ("ability", AbilityOf(holder))));
    }

    /// <summary>What a berry gives a Pokémon with Ripen, twice over: HP (a heal), stages (a boost of its own).</summary>
    private int Ripened(Battler target, int amount) => target == ripening ? amount * 2 : amount;

    /// <summary>
    /// After an item was used up and its line said: Cheek Pouch gives a third of its HP back for a berry (onEatItem), Cud
    /// Chew notes the berry to chew again (two turn ends off, one when it was eaten at a turn's end), and an ally with
    /// Symbiosis hands over its own item (onAllyAfterUseItem).
    /// </summary>
    private void Digest(bool turnEnd = false)
    {
        ripening = null;
        if (eaten.Count == 0) return;
        var list = eaten.ToList();
        eaten.Clear();
        foreach (var (holder, item) in list)
        {
            if (!holder.IsActive) continue;
            var p = holder.Pokemon!;
            if (item.Pocket == ItemPocket.Berries)
            {
                if (Has(holder, "Cheek Pouch") && p.CurrentHP < p.MaxHP && holder.Volatile.HealBlockTurns == 0)
                    RestoreHp(holder, Math.Max(1, p.MaxHP / 3), Text.Of("battle.restoredItsHp", ("pokemon", holder.Name), ("ability", AbilityOf(holder))));
                if (Has(holder, "Cud Chew") && !chewing)
                {
                    holder.Volatile.CudChewBerry = item;
                    holder.Volatile.CudChewTurns = turnEnd ? 1 : 2;
                }
            }
            Symbiosis(holder);
        }
    }

    /// <summary>Cud Chew at a turn's end: the berry noted is chewed once its count is up, and gives what it gives once more.</summary>
    internal void ChewAgain(Battler self)
    {
        var v = self.Volatile;
        if (v.CudChewBerry is not { } berry || --v.CudChewTurns > 0) return;
        v.CudChewBerry = null;
        if (!self.IsActive) return;
        chewing = true;
        Say(Text.Of("battle.cudChew", ("pokemon", self.Name), ("ability", AbilityOf(self)), ("item", ItemOf(self, berry))));
        ateBerry.Add(self.Pokemon!);
        if (berry.PluckEffect != null) EatBerry(self, berry, berry.PluckEffect);
        chewing = false;
    }

    /// <summary>Symbiosis: an ally that has just used up its item takes the holder's (not one that clings to either).</summary>
    private void Symbiosis(Battler taker)
    {
        if (!taker.IsActive || taker.Pokemon!.HeldItem != null) return;
        var giver = SlotsOf(taker.Side).FirstOrDefault(b => b != taker && b.IsActive && Has(b, "Symbiosis") && b.Pokemon!.HeldItem != null);
        if (giver?.Pokemon!.HeldItem is not { } item || ClingsTo(item, giver.Pokemon) || ClingsTo(item, taker.Pokemon)) return;
        giver.Pokemon.HeldItem = null;
        taker.Pokemon.HeldItem = item;
        _ = ItemOf(taker);
        Say(Text.Of("battle.symbiosis", ("pokemon", giver.Name), ("ability", AbilityOf(giver)), ("item", ItemOf(giver, item)), ("ally", taker.Name)));
        CheckConditionHooks(taker, null);
    }

    // ---------------------------------------------------------------- the allies' veils

    /// <summary>
    /// The veils over a Pokémon's side (onAllySetStatus): Sweet Veil keeps sleep off it and its allies, Pastel Veil
    /// poison, and Flower Veil any condition another gives a Grass type. Mold Breaker goes past all three. R27's two
    /// kept their holder alone.
    /// </summary>
    private bool VeiledFrom(Battler target, StatusCondition status, Battler? source, bool announce)
    {
        bool byAnother = source != null && source != target;
        if (byAnother && BattleEffects.Of(source!).Any(e => e.IgnoresTargetAbility)) return false;
        foreach (var ally in SlotsOf(target.Side).Where(b => b.IsActive))
        {
            bool veils = ally.Ability?.Name switch
            {
                "Sweet Veil" => status == StatusCondition.Sleep,
                "Pastel Veil" => status is StatusCondition.Poison or StatusCondition.Toxic,
                "Flower Veil" => byAnother && target.HasType(PokemonType.Grass),
                _ => false
            };
            if (!veils) continue;
            if (announce) Say(Text.Of("battle.veiled", ("target", target.Name), ("pokemon", ally.Name), ("ability", AbilityOf(ally))));
            return true;
        }
        return false;
    }

    /// <summary>Flower Veil (onAllyTryBoost): no other can lower a Grass type's stats on its side.</summary>
    private bool FlowerVeiled(Battler target, bool announce)
    {
        if (!target.HasType(PokemonType.Grass) || SlotsOf(target.Side).FirstOrDefault(b => b.IsActive && b.Ability?.Name == "Flower Veil") is not { } veil) return false;
        if (announce) Say(Text.Of("battle.veiled", ("target", target.Name), ("pokemon", veil.Name), ("ability", AbilityOf(veil))));
        return true;
    }

    /// <summary>The moves Aroma Veil keeps off its side (onAllyTryAddVolatile: attract, disable, encore, healblock, taunt, torment).</summary>
    private static readonly HashSet<string> AromaVeiled = new() { "Infatuate", "Disable", "Encore", "PreventHealing", "Taunt", "Torment" };

    /// <summary>Aroma Veil: a move that would do one of those to it or an ally is stopped before it lands; Mold Breaker goes past.</summary>
    private bool AromaVeils(MoveUse use, Battler target, MoveHit hit)
    {
        if (use.Data.Effect is not { } effect || !AromaVeiled.Contains(effect)) return false;
        if (SlotsOf(target.Side).FirstOrDefault(b => b.IsActive && b.Ability?.Name == "Aroma Veil") is not { } veil) return false;
        hit.Missed = true;
        hit.FailLine = Text.Of("battle.veiled", ("target", target.Name), ("pokemon", veil.Name), ("ability", AbilityOf(veil)));
        return true;
    }

    // ---------------------------------------------------------------- stats

    /// <summary>Rattled (onAfterBoost): an Intimidate that took a stage of its Attack raises its Speed one (R27 had the hits alone).</summary>
    private void RattledByIntimidate(Battler target, Battler? source, StatType stat, By how)
    {
        if (stat != StatType.Attack || how != By.Ability || source == null || !Has(source, "Intimidate") || !Has(target, "Rattled")) return;
        ChangeStat(target, StatType.Speed, 1, target);
    }

    /// <summary>Opportunist copies a foe's stat rise at once (not another Opportunist's copy).</summary>
    private bool copying;

    /// <summary>
    /// Opportunist (onFoeAfterBoost): a stage a foe gained, its holder gains too. Showdown gathers them and copies them
    /// once the action is done; here each is copied as it is made.
    /// </summary>
    private void Opportunism(Battler raised, StatType stat, int stages)
    {
        if (copying || stages <= 0) return;
        foreach (var o in SlotsOf(Other(raised.Side)).Where(b => b.IsActive && Has(b, "Opportunist")).ToList())
        {
            copying = true;
            ChangeStat(o, stat, stages, o);
            copying = false;
        }
    }

    /// <summary>A Tailwind starts behind a side: Wind Rider raises its Attack and Wind Power charges (onSideConditionStart).</summary>
    private void WindBlows(BattleSide side)
    {
        foreach (var b in SlotsOf(side).Where(b => b.IsActive).ToList())
        {
            if (Has(b, "Wind Rider")) ChangeStat(b, StatType.Attack, 1, b);
            else if (Has(b, "Wind Power"))
            {
                b.Volatile.ChargeTurns = 2;
                Say(Text.Of("battle.chargedWithPower", ("pokemon", b.Name), ("ability", AbilityOf(b))));
            }
        }
    }

    // ---------------------------------------------------------------- an ally's ability

    /// <summary>Receiver and Power of Alchemy (onAllyFaint): the holder takes on its fainted ally's ability, unless it is one of Showdown's <c>noreceiver</c>.</summary>
    private void Receives(Battler down)
    {
        if (down.Pokemon?.AbilityName is not { } ability || LaterAbilities.NoReceiver.Contains(ability)) return;
        foreach (var b in SlotsOf(down.Side).Where(b => b != down && b.IsActive && b.Ability?.Name is "Receiver" or "Power of Alchemy").ToList())
        {
            var p = b.Pokemon!;
            KeepAbility(Keep(b), p);
            p.AbilityName = ability;
            b.Volatile.Announced &= EntryCheck.Item | EntryCheck.Shape;
            Say(Text.Of("battle.receiver", ("pokemon", b.Name), ("ally", down.Name), ("ability", AbilityOf(b))));
        }
    }

    // ---------------------------------------------------------------- Wimp Out and Emergency Exit

    /// <summary>Those a move just took from over half their HP to half or under, with Wimp Out or Emergency Exit: they leave once the move is done.</summary>
    private readonly List<Battler> exits = new();

    /// <summary>A hit that took the target from over half its HP to half or under (not one Sheer Force made stronger).</summary>
    private void NoteTheExit(MoveUse use, MoveHit hit)
    {
        var t = hit.Target;
        var p = t.Pokemon!;
        if (t == use.User || !t.IsActive || hit.IntoSubstitute || use.User.Turn.SheerForced || exits.Contains(t)) return;
        if (!(Has(t, "Wimp Out") || Has(t, "Emergency Exit"))) return;
        if (p.CurrentHP * 2 <= p.MaxHP && (p.CurrentHP + hit.LastDealt) * 2 > p.MaxHP) exits.Add(t);
    }

    /// <summary>
    /// Whoever noted leaves, if someone of its party can come in (Showdown's onEmergencyExit: whatever holds it), and a
    /// U-turn of the same move leaves nobody; a wild Pokémon flees the battle. The replacement is picked there and then.
    /// </summary>
    private IEnumerable<BattleRequest> EmergencyExits(MoveUse? use)
    {
        var list = exits.ToList();
        exits.Clear();
        foreach (var t in list)
        {
            if (Result != BattleResult.None) yield break;
            if (!t.IsActive) continue;
            if (t.Roster == null)
            {
                if (IsTrainerBattle || IsDouble || t.IsPlayerSide) continue;
                Say(Text.Of("battle.emergencyExit", ("pokemon", t.Name), ("ability", AbilityOf(t))));
                Say(Text.Of("battle.ranAway", ("pokemon", t.Name))).With(new Left(t.Place, Ran: true));
                End(BattleResult.EnemyFled);
                yield break;
            }
            if (!HasReplacement(t)) continue;
            if (use != null) use.Leaves = Leaving.No;
            Say(Text.Of("battle.emergencyExit", ("pokemon", t.Name), ("ability", AbilityOf(t))));
            foreach (var request in PickReplacement(t)) yield return request;
            if (picked == null || !t.IsActive) continue;
            var incoming = picked;
            Withdraw(t);
            Emit(new Recalled(t.Place));
            Emit(new Left(t.Place));
            Arrive(t, incoming, SentOutLine(t, incoming));
        }
    }

    // ---------------------------------------------------------------- Dancer

    /// <summary>
    /// Dancer (Showdown's runMove): a dance that did something is danced again at once by everyone else with Dancer,
    /// the fastest first, at the dance's target when the dancer's ally aimed it at a foe and at the dancer otherwise.
    /// </summary>
    private void Dance(MoveUse use)
    {
        var data = use.Own ?? use.Data;
        if ((data.Flags & MoveFlags.Dance) == 0 || use.Called) return;
        var user = use.User;
        var first = use.Targets.FirstOrDefault();
        foreach (var dancer in BySpeed().Where(b => b != user && b.IsActive && !b.IsElsewhere && Has(b, "Dancer")).ToList())
        {
            if (Result != BattleResult.None || !dancer.IsActive) return;
            var aim = first != null && first.Side != dancer.Side && user.Side == dancer.Side ? first : user;
            Say(Text.Of("battle.dancer", ("pokemon", dancer.Name), ("ability", AbilityOf(dancer))));
            var inner = new MoveUse { User = dancer, Move = new Move(data), Effect = EffectOf(data), MoveBefore = dancer.Volatile.LastMove, Called = true };
            inner.Targets = ResolveTargets(dancer, inner.Move, aim);
            Perform(inner);
        }
    }

    // ---------------------------------------------------------------- Commander

    /// <summary>
    /// Commander (onUpdate, conditions.ts's commanding and commanded), in a double battle: a Tatsugiri beside a Dondozo
    /// goes into its mouth, out of every move's reach and doing nothing, and the Dondozo's five stats rise two stages;
    /// neither can leave or be made to. Once the Dondozo is gone the Tatsugiri comes out.
    /// </summary>
    private void Commanders()
    {
        if (!IsDouble || Result != BattleResult.None) return;
        foreach (var tatsugiri in AllBattlers.Where(b => b.IsActive && b.Pokemon!.Species.Name == "Tatsugiri").ToList())
        {
            var v = tatsugiri.Volatile;
            var ally = SlotsOf(tatsugiri.Side).FirstOrDefault(b => b != tatsugiri);
            if (v.Commanding != null)
            {
                if (ally is { IsActive: true } && ally.Volatile.CommandedBy == tatsugiri.Place) continue;
                v.Commanding = null;
                v.Elsewhere = Elsewhere.No;
                Emit(new Reappeared(tatsugiri.Place));
                continue;
            }
            if (!Has(tatsugiri, "Commander") || ally is not { IsActive: true } || ally.Pokemon!.Species.Name != "Dondozo" || ally.Volatile.CommandedBy != null) continue;
            v.Commanding = ally.Place;
            v.Elsewhere = Elsewhere.Vanished;
            ally.Volatile.CommandedBy = tatsugiri.Place;
            waiting.RemoveAll(a => a.User == tatsugiri);
            Say(Text.Of("battle.commander", ("pokemon", tatsugiri.Name), ("ability", AbilityOf(tatsugiri)), ("ally", ally.Name))).With(new Vanished(tatsugiri.Place));
            foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed })
                ChangeStat(ally, stat, 2, ally);
        }
    }

    /// <summary>A Tatsugiri in command, or the Dondozo it commands: neither can be called back or dragged out.</summary>
    private static bool InTheCommand(Battler b) => b.Volatile.Commanding != null || b.Volatile.CommandedBy != null;

    /// <summary>Order Up (onAfterMoveSecondarySelf): a Dondozo with a Tatsugiri in its mouth raises a stat by the Tatsugiri's form, Droopy's Defense, Stretchy's Speed, Curly's Attack.</summary>
    private void OrderUp(MoveUse use)
    {
        var user = use.User;
        if (!user.IsActive || user.Volatile.CommandedBy is not { } place || At(place).Pokemon is not { } tatsugiri) return;
        var stat = tatsugiri.Form switch
        {
            "Tatsugiri-Droopy" => StatType.Defense,
            "Tatsugiri-Stretchy" => StatType.Speed,
            _ => StatType.Attack
        };
        ChangeStat(user, stat, 1, user);
    }

    // ---------------------------------------------------------------- forms in battle

    /// <summary>Battle Bond (generation 9's onSourceAfterFaint): a Greninja of the bond, with foes still to come, once a battle.</summary>
    internal void BondWith(Battler self)
    {
        var p = self.Pokemon!;
        if (!self.IsActive || p.Form != "Greninja-Battle-Bond" || self.Volatile.Transformed || !FoesLeft(self) || !FirstTimeThisBattle(self)) return;
        foreach (var stat in new[] { StatType.Attack, StatType.SpAttack, StatType.Speed }) ChangeStat(self, stat, 1, self);
        Say(Text.Of("battle.battleBond", ("pokemon", self.Name)));
    }

    /// <summary>The other side has a Pokémon left that can fight, on the field or behind it.</summary>
    private bool FoesLeft(Battler self) =>
        SlotsOf(Other(self.Side)).Any(b => b.IsActive || b.Roster?.Members.Any(m => !m.IsFainted && !m.IsEgg) == true);

    /// <summary>
    /// Power Construct: a Zygarde of the cells at half its HP or less becomes its Complete Forme for the rest of the
    /// battle, its most HP raised and the difference given to it (Showdown's permanent formeChange); the screen takes
    /// the HP from the event.
    /// </summary>
    internal void Construct(Battler self)
    {
        var p = self.Pokemon!;
        if (!self.IsActive || p.Species.Name != "Zygarde" || p.Form == "Zygarde-Complete" || self.Volatile.Transformed || p.CurrentHP * 2 > p.MaxHP) return;
        Say(Text.Of("battle.powerConstruct"));
        TakeForm(self, "Zygarde-Complete");
        Say(Text.Of("battle.completeForme", ("pokemon", self.Name))).With(new Reshaped(self.Place)).With(new FormHpChanged(self.Place, p.MaxHP, p.CurrentHP));
    }

    // ---------------------------------------------------------------- Ball Fetch

    /// <summary>Ball Fetch has fetched this battle's ball.</summary>
    private bool ballFetched;

    /// <summary>Ball Fetch: the first ball of the battle that fails is picked up by the player's Pokémon that has it and holds nothing.</summary>
    private void FetchTheBall(ItemData ball)
    {
        if (ballFetched || Kind is BattleKind.Safari or BattleKind.PalPark) return;
        var fetcher = PlayerSlots.FirstOrDefault(b => b.IsActive && Has(b, "Ball Fetch") && b.Pokemon!.HeldItem == null);
        if (fetcher == null) return;
        ballFetched = true;
        fetcher.Pokemon!.HeldItem = ball;
        Say(Text.Of("battle.ballFetch", ("pokemon", fetcher.Name), ("item", ItemOf(fetcher))));
    }

    // ---------------------------------------------------------------- Protosynthesis and Quark Drive

    /// <summary>The sun came: Protosynthesis takes hold on everyone it can, its stat fixed now (R27b's audit).</summary>
    private void ParadoxesWake()
    {
        foreach (var b in AllBattlers.Where(b => b.IsActive)) LaterAbilities.ParadoxStat(b);
    }
}
