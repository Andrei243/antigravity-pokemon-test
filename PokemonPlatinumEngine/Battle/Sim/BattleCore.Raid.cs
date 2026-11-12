using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// What a Max Raid den's battle is fought under (plan 33 · GL3; <c>docs/mechanics/rulings.md</c>, "Max Raid dens"): the
/// den's stars, the three Trainers who join the player (each with one Pokémon, thought for by the trainer AI as a
/// partner is), and whether the raid can't be lost (plan 33 · GL6's Eternatus). Plain values, so the Vs. Recorder's file
/// keeps them.
/// </summary>
public sealed record RaidTerms(int Stars, IReadOnlyList<Trainer> Allies, bool CannotFail = false)
{
    /// <summary>The fewest stars and the most.</summary>
    public const int FewestStars = 1, MostStars = 5;
}

/// <summary>
/// A raid's own numbers (plan 33 · GL3), ours on Sword and Shield's shape: how much a boss's HP is raised past its
/// Dynamax by its stars, how many hits its barrier takes and when it goes up, which turns it acts twice on, and how many
/// falls the raiders' side may take before the den's energy blows them out. GPU-free.
/// </summary>
public static class RaidRules
{
    /// <summary>The raiders' side is blown out of the den at its fourth fall (Sword's), unless the raid can't be lost.</summary>
    public const int MostFalls = 4;

    /// <summary>A boss's HP past its Dynamax, in halves: one star, as it Dynamaxed; then a half more a star.</summary>
    public static int HpHalves(int stars) => 2 + Math.Clamp(stars, RaidTerms.FewestStars, RaidTerms.MostStars) - 1;

    /// <summary>The hits its barrier takes once its HP has fallen to half: none for one or two stars, then one less than its stars.</summary>
    public static int Shields(int stars) => stars >= 3 ? stars - 1 : 0;

    /// <summary>A barrier lets through a quarter of a hit's damage, as it lets through a Max Move's.</summary>
    public const int ThroughTheBarrier = 4;

    /// <summary>A Max Move breaks two of a barrier's shields at once, any other hit one.</summary>
    public static int BreaksOf(bool maxMove) => maxMove ? 2 : 1;

    /// <summary>Whether the boss acts twice in a turn: every turn at five stars, every other turn (the even ones) at four, never fewer.</summary>
    public static bool ActsTwice(int stars, int turn) => stars >= 5 || stars == 4 && turn % 2 == 0;
}

/// <summary>
/// Max Raid battles (plan 33 · GL3; <c>BattleFormat.Raid</c>, for plan 34's Tera Raids too): the player and three
/// Trainers on the player's side's four places, each with one Pokémon, against one wild Pokémon Dynamaxed as the battle
/// opens and kept so for the whole battle, its HP raised by its stars. Nobody switches, nothing is used from the bag,
/// and a raider who falls is back on its feet at the turn's end; the raiders' fourth fall blows them out of the den and
/// the raid is lost. Past half its HP the boss puts up a barrier that takes a hit to break a shield (two for a Max Move)
/// and lets a quarter of the damage through; at four and five stars it acts twice in a turn. Felled, it is the
/// raiders' victory, and the den's catch is the script's (<see cref="Models.Raids.CatchChance"/>). Every draw is on the
/// battle's own generator.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>A Max Raid battle (<see cref="BattleFormat.Raid"/>).</summary>
    public bool IsRaid => Format == BattleFormat.Raid;

    /// <summary>The raid's terms, or null in any other battle.</summary>
    public RaidTerms? Raid { get; private set; }

    /// <summary>The raiders' falls so far.</summary>
    public int RaidFalls { get; private set; }

    /// <summary>The boss's barrier: the shields standing now, nought while it is down.</summary>
    public int RaidShields { get; private set; }

    // Whether the boss has put its barrier up (it goes up once), and the Pokémon each raider fights with, by place
    private bool raidBarrierRaised;
    private readonly Pokemon?[] raidOwn = new Pokemon?[4];

    /// <summary>The boss's place: the wild side's first.</summary>
    private Battler? RaidBoss => IsRaid ? EnemySlots.FirstOrDefault() : null;

    /// <summary>What a switch meets in a raid.</summary>
    public static string RaidSwitchRefusal => Text.Of("battle.raidNoSwitch");

    /// <summary>The raid's places filled as the battle is made: each ally in a place of their own after the player's.</summary>
    private void SetUpTheRaid(RaidTerms? terms)
    {
        if (!IsRaid || terms == null) return;
        Raid = terms;
        for (int i = 1; i < PlayerSlots.Count && i - 1 < terms.Allies.Count; i++)
        {
            var ally = terms.Allies[i - 1];
            PlayerSlots[i].Trainer = ally;
            PlayerSlots[i].Roster = ally.Party;
            PlayerSlots[i].Pokemon = ally.Party.Members.FirstOrDefault(p => !p.IsFainted);
        }
        for (int i = 0; i < PlayerSlots.Count; i++) raidOwn[i] = PlayerSlots[i].Pokemon;
    }

    /// <summary>As the raid opens, once everyone is out: the boss Dynamaxes and its HP is raised by its stars.</summary>
    private void BeginTheRaid()
    {
        if (RaidBoss is not { IsActive: true } boss || Raid == null) return;
        Say(Text.Of("battle.raidBegins", ("pokemon", boss.Name)));
        Dynamax(boss);
        var p = boss.Pokemon!;
        int halves = RaidRules.HpHalves(Raid.Stars);
        if (halves > 2) p.TakeHp(p.MaxHP * halves / 2, p.CurrentHP * halves / 2);
        boss.Volatile.MaxHpDynamaxed = p.MaxHP;
        Emit(new HpChanged(boss.Place, p.CurrentHP, Healed: true));
    }

    /// <summary>Whether a place's Dynamax never runs down: the raid's boss.</summary>
    private bool KeepsItsDynamax(Battler b) => IsRaid && b == RaidBoss;

    /// <summary>
    /// A hit on the boss as its barrier has it: past half its HP the barrier goes up once, the hit that takes it there
    /// stopping at half; while shields stand a quarter of the hit gets through and the hit breaks one shield (two for a Max
    /// Move). Any other target's damage is as it was.
    /// </summary>
    private int ThroughTheRaidBarrier(MoveUse use, Battler t, int damage)
    {
        if (!IsRaid || Raid == null || t != RaidBoss || t.Pokemon is not { } p || damage <= 0) return damage;
        bool maxMove = use.Data.Kind is MoveKind.MaxMove or MoveKind.GMaxMove;
        if (RaidShields > 0)
        {
            RaidShields = Math.Max(0, RaidShields - RaidRules.BreaksOf(maxMove));
            int through = Math.Max(1, damage / RaidRules.ThroughTheBarrier);
            var line = RaidShields > 0
                ? Say(Text.Of("battle.raidShieldBroke", ("pokemon", t.Name), ("n", RaidShields)))
                : Say(Text.Of("battle.raidShieldDown", ("pokemon", t.Name)));
            line.With(new RaidBarrier(t.Place, RaidShields));
            return through;
        }
        int half = p.MaxHP / 2;
        if (!raidBarrierRaised && RaidRules.Shields(Raid.Stars) > 0 && p.CurrentHP > half && p.CurrentHP - damage <= half)
        {
            raidBarrierRaised = true;
            RaidShields = RaidRules.Shields(Raid.Stars);
            Say(Text.Of("battle.raidShieldUp", ("pokemon", t.Name))).With(new RaidBarrier(t.Place, RaidShields));
            return Math.Max(0, p.CurrentHP - half);
        }
        return damage;
    }

    /// <summary>A raider falls: the fall is counted.</summary>
    private void CountTheRaidFall(Battler down)
    {
        if (IsRaid && down.IsPlayerSide) RaidFalls++;
    }

    /// <summary>
    /// Whether the raid is over, as a faint leaves it (asked first by the battle's end): the raiders' fourth fall blows
    /// them out of the den (but where the raid can't be lost); the boss down is the raiders' victory, which the battle's
    /// usual end tells. True when the end was decided here (or nothing ends yet), false to let the usual end go on.
    /// </summary>
    private bool EndTheRaid()
    {
        if (!IsRaid) return false;
        if (RaidFalls >= RaidRules.MostFalls && Raid is { CannotFail: false })
        {
            Say(Text.Of("battle.raidBlownOut"));
            End(BattleResult.PlayerDefeat);
            return true;
        }
        // The boss down goes on to the usual victory; anything else waits for the raiders to stand again
        return !SideDefeated(EnemySlots);
    }

    /// <summary>The raiders who fell this turn are back on their feet as it ends, whole, each with the Pokémon they came with.</summary>
    private void TheRaidersStandAgain(List<Battler> entered)
    {
        if (!IsRaid) return;
        foreach (var place in PlayerSlots)
        {
            if (place.IsActive || raidOwn[place.Slot] is not { } own) continue;
            own.Status = Data.StatusCondition.None;
            own.CurrentHP = own.MaxHP;
            entered.Add(place);
            SendIn(place, own);
            Bringing(Say(Text.Of("battle.raidBackOnItsFeet", ("pokemon", own.DisplayName))).With(new Entered(place.Place, own, FromBall: true)), place);
        }
    }

    /// <summary>
    /// The boss's second action of a turn, at four and five stars (<see cref="RaidRules.ActsTwice"/>): a move it can use
    /// at one of the raiders, chosen as a wild Pokémon chooses, before the turn's end.
    /// </summary>
    private IEnumerable<BattleRequest> TheBossActsAgain()
    {
        if (!IsRaid || Raid == null || Result != BattleResult.None || RaidBoss is not { IsActive: true } boss) yield break;
        if (!RaidRules.ActsTwice(Raid.Stars, Turn)) yield break;
        var act = ToAct(TrainerAi.WildChoice(this, boss));
        Say(Text.Of("battle.raidActsAgain", ("pokemon", boss.Name)));
        foreach (var request in ExecuteMove(act)) yield return request;
        if (Result == BattleResult.None) ResolveFaints();
    }
}
