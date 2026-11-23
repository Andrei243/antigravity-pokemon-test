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
public sealed record RaidTerms(int Stars, IReadOnlyList<Trainer> Allies, bool CannotFail = false, bool MegaMaxDen = false, TeraType? Tera = null)
{
    /// <summary>The fewest stars and the most of a Max Raid den.</summary>
    public const int FewestStars = 1, MostStars = 5;

    /// <summary>The most stars of a Tera Raid crystal (plan 34 · PD7; the black crystals' six and seven are PD9's).</summary>
    public const int TeraMostStars = 7;

    /// <summary>A Tera Raid (plan 34 · PD7): the boss Terastallized into <see cref="Tera"/> rather than Dynamaxed.</summary>
    public bool IsTera => Tera != null;
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

    /// <summary>A boss's HP past its Dynamax, in halves: as it Dynamaxed at one star, then once more for every star past it (five stars, five times).</summary>
    public static int HpHalves(int stars) => 2 * Math.Clamp(stars, RaidTerms.FewestStars, RaidTerms.MostStars);

    /// <summary>The hits its barrier takes once its HP has fallen to half: none for one or two stars, then one less than its stars.</summary>
    public static int Shields(int stars) => stars >= 3 ? stars - 1 : 0;

    /// <summary>A barrier lets through a quarter of a hit's damage, as it lets through a Max Move's.</summary>
    public const int ThroughTheBarrier = 4;

    /// <summary>A Max Move breaks two of a barrier's shields at once, any other hit one.</summary>
    public static int BreaksOf(bool maxMove) => maxMove ? 2 : 1;

    /// <summary>Whether the boss acts twice in a turn: every turn at five stars, every other turn (the even ones) at four, never fewer.</summary>
    public static bool ActsTwice(int stars, int turn) => stars >= 5 || stars == 4 && turn % 2 == 0;

    // ---------------------------------------------------------------- Tera Raids (plan 34 · PD7), ours on Scarlet's shape

    /// <summary>How many times its own HP a Tera Raid's boss has, by its stars: three at one star up to twenty at seven.</summary>
    public static int TeraHpTimes(int stars) => Math.Clamp(stars, RaidTerms.FewestStars, RaidTerms.TeraMostStars) switch
    {
        1 => 3,
        2 => 4,
        3 => 6,
        4 => 8,
        5 => 12,
        6 => 16,
        _ => 20
    };

    /// <summary>The share of its HP, in hundredths, at which a Tera boss's crystal shield goes up: none to three stars, half at four, three quarters from five.</summary>
    public static int TeraShieldAt(int stars) => stars >= 5 ? 75 : stars == 4 ? 50 : 0;

    /// <summary>The hits a Tera boss's crystal shield takes: as many as its stars (none where it raises none).</summary>
    public static int TeraShields(int stars) => TeraShieldAt(stars) > 0 ? stars : 0;

    /// <summary>A crystal shield lets a fifth of a hit's damage through.</summary>
    public const int ThroughTheCrystal = 5;

    /// <summary>A hit from a Terastallized raider breaks three of the crystal's shields at once, any other hit one.</summary>
    public static int CrystalBreaksOf(bool terastallized) => terastallized ? 3 : 1;

    /// <summary>The turns a Tera Raid's clock holds, by its stars: ten to two stars, twelve to four, fifteen at five, eighteen at six and seven.</summary>
    public static int TeraTurns(int stars) => stars >= 6 ? 18 : stars == 5 ? 15 : stars >= 3 ? 12 : 10;

    /// <summary>The turns a raider's fall takes off the clock.</summary>
    public const int FallCost = 2;

    /// <summary>The moves a raider's Pokémon uses before its Tera Orb is charged and it may Terastallize.</summary>
    public const int OrbCharge = 3;
}

/// <summary>
/// Max Raid battles (plan 33 · GL3; <c>BattleFormat.Raid</c>, for plan 34's Tera Raids too): the player and three
/// Trainers on the player's side's four places, each with one Pokémon, against one wild Pokémon Dynamaxed as the battle
/// opens and kept so for the whole battle, its HP raised by its stars. Nobody switches, nothing is used from the bag,
/// and a raider who falls is back on its feet at the turn's end; the raiders' fourth fall blows them out of the den and
/// the raid is lost. Past half its HP the boss puts up a barrier that takes a hit to break a shield (two for a Max Move)
/// and lets a quarter of the damage through, never its last HP; at four and five stars it acts twice in a turn. Felled, it is the
/// raiders' victory, and the den's catch is the script's (<see cref="Models.Raids.CatchChance"/>). Every draw is on the
/// battle's own generator.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>A Max Raid battle (<see cref="BattleFormat.Raid"/>).</summary>
    public bool IsRaid => Format == BattleFormat.Raid;

    /// <summary>A Tera Raid (plan 34 · PD7): a raid whose boss Terastallizes rather than Dynamaxes, fought against a clock of turns.</summary>
    public bool IsTeraRaid => IsRaid && Raid is { IsTera: true };

    /// <summary>The moves each raider's Pokémon has used toward its Tera Orb's charge, by place (plan 34 · PD7).</summary>
    private readonly int[] orbCharge = new int[4];

    /// <summary>How charged a raider's Tera Orb is: the moves its Pokémon has used, held to <see cref="RaidRules.OrbCharge"/>.</summary>
    public int OrbChargeOf(Battler b) => b.IsPlayerSide && b.Slot is >= 0 and < 4 ? Math.Min(orbCharge[b.Slot], RaidRules.OrbCharge) : 0;

    /// <summary>The turns a Tera Raid's clock has left (the clock's turns less those played to their end and two for every fall); nought in any other battle.</summary>
    public int TeraTurnsLeft => IsTeraRaid ? Math.Max(0, RaidRules.TeraTurns(Raid!.Stars) - Turn - RaidRules.FallCost * RaidFalls) : 0;

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
        if (Raid.Tera is { } tera)
        {
            BeginTheTeraRaid(boss, tera);
            return;
        }
        Say(Text.Of("battle.raidBegins", ("pokemon", boss.Name)));
        // A Mega Max den's boss holding its stone Mega Evolves first, its stone reacting to the den's light, and so grows
        // into its Mega Dynamax form (plan 33 · GL7, part b; plan 06 · R20c's question 14)
        if (MegaMaxDen && Rules.MegaEvolution && Rules.MegaDynamax && !LaterMechanicsBarred && MegaFormOf(boss.Pokemon!) != null) MegaEvolve(boss);
        Dynamax(boss);
        var p = boss.Pokemon!;
        int halves = RaidRules.HpHalves(Raid.Stars);
        if (halves > 2) p.TakeHp(p.MaxHP * halves / 2, p.CurrentHP * halves / 2);
        boss.Volatile.MaxHpDynamaxed = p.MaxHP;
        Emit(new HpChanged(boss.Place, p.CurrentHP, Healed: true));
    }

    /// <summary>
    /// A Tera Raid opens (plan 34 · PD7): the crystal's light, the boss Terastallized into the crystal's type for the
    /// whole battle (whatever the rules' Tera Orb says: the crystal is its orb), its HP raised by its stars, and the clock
    /// told. Nothing is drawn.
    /// </summary>
    private void BeginTheTeraRaid(Battler boss, TeraType tera)
    {
        var p = boss.Pokemon!;
        Say(Text.Of("battle.teraRaidBegins", ("pokemon", boss.Name)));
        p.TeraType = tera;
        if (Rules.Terastallization && !LaterMechanicsBarred) Terastallize(boss);
        int times = RaidRules.TeraHpTimes(Raid!.Stars);
        if (times > 1) p.TakeHp(p.MaxHP * times, p.CurrentHP * times);
        Emit(new HpChanged(boss.Place, p.CurrentHP, Healed: true));
        Say(Text.Of("battle.teraRaidClock", ("n", RaidRules.TeraTurns(Raid.Stars))));
    }

    /// <summary>A raider's Pokémon uses a move in a Tera Raid: its Tera Orb charges toward <see cref="RaidRules.OrbCharge"/>, told once charged.</summary>
    private void ChargeTheOrb(Battler user)
    {
        if (!IsTeraRaid || !user.IsPlayerSide || user.Slot is < 0 or >= 4 || orbCharge[user.Slot] >= RaidRules.OrbCharge) return;
        if (++orbCharge[user.Slot] == RaidRules.OrbCharge && user.Pokemon?.Terastallized == null)
            Say(Text.Of("battle.teraOrbCharged", ("pokemon", user.Name)));
    }

    /// <summary>
    /// The clock at a Tera Raid's turn end (plan 34 · PD7): once its turns are spent (two more for every raider's fall),
    /// the crystal's light swallows the raiders and the raid is lost, unless it can't be. True when it ended here.
    /// </summary>
    private bool TheTeraClockRanOut()
    {
        if (!IsTeraRaid || Result != BattleResult.None || Raid is { CannotFail: true }) return false;
        // (the turn under way counts as played)
        if (RaidRules.TeraTurns(Raid!.Stars) - (Turn + 1) - RaidRules.FallCost * RaidFalls > 0) return false;
        Say(Text.Of("battle.teraRaidTimeUp"));
        End(BattleResult.PlayerDefeat);
        return true;
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
        if (Raid.IsTera) return ThroughTheCrystal(use, t, p, damage);
        bool maxMove = use.Data.Kind is MoveKind.MaxMove or MoveKind.GMaxMove;
        if (RaidShields > 0)
        {
            RaidShields = Math.Max(0, RaidShields - RaidRules.BreaksOf(maxMove));
            // (and never the last of its HP: the barrier must fall before the boss can)
            int through = Math.Min(Math.Max(1, damage / RaidRules.ThroughTheBarrier), p.CurrentHP - 1);
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

    /// <summary>
    /// A hit on a Tera boss as its crystal shield has it (plan 34 · PD7): at four stars and more the shield goes up once as
    /// its HP falls to <see cref="RaidRules.TeraShieldAt"/>, the hit that takes it there stopping at that share; while it
    /// stands a fifth of a hit gets through (never its last HP) and the hit breaks a shield, three from a Terastallized
    /// raider. The same <see cref="RaidBarrier"/> event as a den's barrier, so the screen's pips count it.
    /// </summary>
    private int ThroughTheCrystal(MoveUse use, Battler t, Pokemon p, int damage)
    {
        if (RaidShields > 0)
        {
            bool tera = use.User.Pokemon?.Terastallized != null;
            RaidShields = Math.Max(0, RaidShields - RaidRules.CrystalBreaksOf(tera));
            int through = Math.Min(Math.Max(1, damage / RaidRules.ThroughTheCrystal), p.CurrentHP - 1);
            var line = RaidShields > 0
                ? Say(Text.Of("battle.teraShieldBroke", ("pokemon", t.Name), ("n", RaidShields)))
                : Say(Text.Of("battle.teraShieldDown", ("pokemon", t.Name)));
            line.With(new RaidBarrier(t.Place, RaidShields));
            return through;
        }
        int at = RaidRules.TeraShieldAt(Raid!.Stars);
        int threshold = (int)((long)p.MaxHP * at / 100);
        if (!raidBarrierRaised && at > 0 && p.CurrentHP > threshold && p.CurrentHP - damage <= threshold)
        {
            raidBarrierRaised = true;
            RaidShields = RaidRules.TeraShields(Raid.Stars);
            Say(Text.Of("battle.teraShieldUp", ("pokemon", t.Name))).With(new RaidBarrier(t.Place, RaidShields));
            return Math.Max(0, p.CurrentHP - threshold);
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
        // A Tera Raid has no count of falls: its clock is what loses it (TheTeraClockRanOut)
        if (!IsTeraRaid && RaidFalls >= RaidRules.MostFalls && Raid is { CannotFail: false })
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
        // (a Tera Raid's boss acts once a turn, as Scarlet's: its clock is the pressure)
        if (!IsRaid || Raid == null || Raid.IsTera || Result != BattleResult.None || RaidBoss is not { IsActive: true } boss) yield break;
        if (!RaidRules.ActsTwice(Raid.Stars, Turn)) yield break;
        var act = ToAct(TrainerAi.WildChoice(this, boss));
        Say(Text.Of("battle.raidActsAgain", ("pokemon", boss.Name)));
        foreach (var request in ExecuteMove(act)) yield return request;
        if (Result == BattleResult.None) ResolveFaints();
    }
}
