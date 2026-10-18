using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The last 23 abilities of Generations 5 to 9 (plan 06 · R27b), and the rules R27b's audit of R27 against Pokémon
/// Showdown's data/abilities.ts at the pinned commit set right, each by Showdown's rule. One rule to a test, under the
/// modern rules, the rolls fixed by kind (<see cref="CoreScenario.Calm"/>); a Pokémon made by <c>Mon</c> with no moves
/// knows only Idle, so its move 0 sits the turn out.
/// </summary>
public class LaterAbilities2Tests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, BattleWeather sky = BattleWeather.None, params Pokemon[] bench) =>
        Wild(mine, foe, rolls, Modern, sky, BattleTerrain.Plain, bench);

    private static BattleCore Pair(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null) => Doubles(mine, theirs, rolls, Modern);

    private static BattleCore Versus(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null) => Against(mine, theirs, rolls, Modern);

    /// <summary>A double battle's turn in which everyone uses its first move, aimed where the rules pick unless said.</summary>
    private static List<string> AllIdle(BattleCore core, params (Place Who, int Move, Place? At)[] but)
    {
        var actions = new List<(Place, int, Place?)> { (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null) };
        foreach (var b in but) actions[actions.FindIndex(a => a.Item1 == b.Who)] = b;
        return DoubleTurn(core, actions.ToArray());
    }

    private static int Stage(Pokemon p, StatType stat) => p.StatStages.GetValueOrDefault(stat);

    private static Pokemon AtHp(Pokemon p, int hp)
    {
        p.CurrentHP = hp;
        return p;
    }

    private static Pokemon InForm(Pokemon p, string form)
    {
        p.ChangeForm(form);
        return p;
    }

    /// <summary>What the foe lost to the player's first move in one turn, the foe sitting it out.</summary>
    private static int Took(Pokemon mine, Pokemon foe, BattleWeather sky = BattleWeather.None)
    {
        var core = Duel(mine, foe, sky: sky);
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        return hp - foe.CurrentHP;
    }

    private static void Near(double expected, int with, int without) =>
        Assert.InRange(with / (double)without, expected - 0.04, expected + 0.04);

    // ------------------------------------------------------------------ berries kept from the foes

    [Fact]
    public void UnnerveKeepsTheFoeFromEatingItsBerry()
    {
        Pokemon Low() { var p = With(Mon("Snorlax", 50), item: "Sitrus Berry"); return AtHp(p, p.MaxHP * 2 / 5); }
        var kept = Low();
        Duel(With(Mon("Jolteon", 50), "Unnerve"), kept);
        Assert.Equal("Sitrus Berry", kept.HeldItem?.Name);
        var eaten = Low();
        Duel(With(Mon("Jolteon", 50), "Run Away"), eaten);
        Assert.Null(eaten.HeldItem);
    }

    [Fact]
    public void UnnerveKeepsAResistBerryFromHalvingAHit()
    {
        var foe = With(Mon("Venusaur", 50), item: "Occa Berry");
        Duel(With(Mon("Charizard", 50, "Flamethrower"), "Unnerve"), foe);
        var core = Duel(With(Mon("Charizard", 50, "Flamethrower"), "Unnerve"), foe);
        Turn(core, 0, 0);
        Assert.Equal("Occa Berry", foe.HeldItem?.Name);
    }

    [Fact]
    public void AsOneSaysBothAbilitiesAsItComesIn()
    {
        var core = Duel(With(InForm(Mon("Calyrex", 60), "Calyrex-Ice"), "As One"), Mon("Snorlax", 50));
        var said = Lines(core.Log);
        Assert.Contains("Calyrex has two Abilities!", said);
        Assert.Contains(said, s => s.Contains("too nervous to eat Berries"));
    }

    [Fact]
    public void AsOneOfTheIceRiderRaisesItsAttackForAKnockOut()
    {
        var rider = With(InForm(Mon("Calyrex", 60, "Tackle"), "Calyrex-Ice"), "As One");
        var core = Versus(new[] { rider }, new[] { Mon("Magikarp", 2), Mon("Magikarp", 2) });
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(rider, StatType.Attack));
        Assert.Equal(0, Stage(rider, StatType.SpAttack));
    }

    [Fact]
    public void AsOneOfTheShadowRiderRaisesItsSpAttackForAKnockOut()
    {
        var rider = With(InForm(Mon("Calyrex", 60, "Tackle"), "Calyrex-Shadow"), "As One");
        var core = Versus(new[] { rider }, new[] { Mon("Magikarp", 2), Mon("Magikarp", 2) });
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(rider, StatType.SpAttack));
    }

    // ------------------------------------------------------------------ as it comes in

    [Fact]
    public void IllusionSendsItOutUnderTheNameOfTheLastOfItsParty()
    {
        var core = Versus(new[] { Mon("Snorlax", 50, "Tackle") }, new[] { With(Mon("Zoroark", 50), "Illusion"), Mon("Pidgey", 10) });
        var said = Lines(core.Log);
        Assert.Contains(said, s => s.Contains("sent out") && s.Contains("Pidgey"));
        Assert.DoesNotContain(said, s => s.Contains("Zoroark"));
    }

    [Fact]
    public void AHitBreaksTheIllusion()
    {
        var core = Versus(new[] { Mon("Snorlax", 50, "Tackle") }, new[] { With(Mon("Zoroark", 50), "Illusion"), Mon("Pidgey", 10) });
        Assert.Equal("Pidgey", core.At(Foe).Volatile.Illusion);
        var said = Turn(core, 0, 0);
        Assert.Contains("Foe Zoroark's illusion wore off!", said);
        Assert.Null(core.At(Foe).Volatile.Illusion);
    }

    [Fact]
    public void ImposterTransformsIntoTheFoeAsItComesIn()
    {
        var ditto = With(Mon("Ditto", 50), "Imposter");
        Duel(ditto, Mon("Snorlax", 50, "Tackle"));
        Assert.Equal("Snorlax", ditto.Species.Name);
        Assert.Equal("Tackle", ditto.Moves[0].Name);
    }

    [Fact]
    public void NeutralizingGasKeepsAnIntimidateFromActing()
    {
        var foe = With(Mon("Weezing", 50), "Neutralizing Gas");
        var core = Duel(With(Mon("Gyarados", 50), "Intimidate"), foe);
        Assert.Contains("Neutralizing gas filled the area!", Lines(core.Log));
        Assert.Equal(0, Stage(foe, StatType.Attack));
        Assert.Null(core.At(Mine).Ability);
        Assert.Equal("Neutralizing Gas", core.At(Foe).Ability?.Name);
    }

    [Fact]
    public void NeutralizingGasLeavesAnAbilityThatCantBeSuppressed()
    {
        var core = Duel(With(Mon("Mimikyu", 50), "Disguise"), With(Mon("Weezing", 50), "Neutralizing Gas"));
        Assert.Equal("Disguise", core.At(Mine).Ability?.Name);
    }

    [Fact]
    public void OnceTheGasHasGoneTheAbilitiesItHeldActAgain()
    {
        // Gastro Acid takes the gas away, and the Intimidate it held acts as if its holder had just come in
        var weezing = With(Mon("Weezing", 50), "Neutralizing Gas");
        var core = Duel(With(Mon("Gyarados", 50, "Gastro Acid"), "Intimidate"), weezing);
        Assert.Equal(0, Stage(weezing, StatType.Attack));
        var said = Turn(core, 0, 0);
        Assert.True(said.Contains("The effects of the neutralizing gas wore off!"), string.Join("\n", said));
        Assert.True(Stage(weezing, StatType.Attack) == -1, string.Join("\n", said));
        Assert.Equal(-1, Stage(weezing, StatType.Attack));
    }

    [Fact]
    public void SupremeOverlordCountsTheFallenAsItComesInAndNoLater()
    {
        var kingambit = With(Mon("Kingambit", 50), "Supreme Overlord");
        var ally = AtHp(Mon("Snorlax", 50), 1);
        var core = Pair(new[] { kingambit, ally }, new[] { Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50) });
        AllIdle(core, (Foe, 0, Mine2));
        Assert.True(ally.IsFainted);
        Assert.Equal(0, core.At(Mine).Volatile.Fallen);
    }

    // ------------------------------------------------------------------ the allies' veils

    [Fact]
    public void AromaVeilKeepsATauntOff()
    {
        var core = Duel(With(Mon("Aromatisse", 50), "Aroma Veil"), Mon("Sableye", 50, "Taunt"));
        var said = Turn(core, 0, 0);
        Assert.Equal(0, core.At(Mine).Volatile.TauntTurns);
        Assert.Contains(said, s => s.Contains("is protected by"));
    }

    [Fact]
    public void AromaVeilKeepsAnEncoreOffAnAlly()
    {
        var core = Pair(new[] { With(Mon("Aromatisse", 50), "Aroma Veil"), Mon("Snorlax", 50, "Tackle") }, new[] { Mon("Gengar", 50, "Encore"), Mon("Snorlax", 50) });
        AllIdle(core, (Mine2, 0, Foe2), (Foe, 0, Mine2));
        Assert.Null(core.At(Mine2).Volatile.Encored);
    }

    [Fact]
    public void FlowerVeilKeepsAGrassAllysStatsFromFalling()
    {
        var florges = With(Mon("Florges", 50), "Flower Veil");
        var venusaur = Mon("Venusaur", 50);
        var core = Pair(new[] { florges, venusaur }, new[] { Mon("Snorlax", 50, "Growl"), Mon("Snorlax", 50) });
        AllIdle(core);
        Assert.Equal(0, Stage(venusaur, StatType.Attack));
        Assert.Equal(-1, Stage(florges, StatType.Attack));
    }

    [Fact]
    public void FlowerVeilKeepsAConditionOffAGrassAlly()
    {
        var venusaur = Mon("Venusaur", 50);
        var core = Pair(new[] { With(Mon("Florges", 50), "Flower Veil"), venusaur }, new[] { Mon("Jolteon", 50, "Thunder Wave"), Mon("Snorlax", 50) });
        AllIdle(core, (Foe, 0, Mine2));
        Assert.Equal(StatusCondition.None, venusaur.Status);
    }

    [Fact]
    public void SweetVeilKeepsAnAllyAwake()
    {
        var snorlax = Mon("Snorlax", 50);
        var core = Pair(new[] { With(Mon("Swirlix", 50), "Sweet Veil"), snorlax }, new[] { Mon("Breloom", 50, "Spore"), Mon("Snorlax", 50) });
        AllIdle(core, (Foe, 0, Mine2));
        Assert.Equal(StatusCondition.None, snorlax.Status);
    }

    [Fact]
    public void PastelVeilCuresAnAllysPoisonAsItComesIn()
    {
        var ally = Mon("Snorlax", 50);
        ally.Status = StatusCondition.Poison;
        Pair(new[] { With(Mon("Ponyta", 50), "Pastel Veil"), ally }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        Assert.Equal(StatusCondition.None, ally.Status);
    }

    [Fact]
    public void PastelVeilKeepsPoisonOffAnAlly()
    {
        var ally = Mon("Snorlax", 50);
        var core = Pair(new[] { With(Mon("Ponyta", 50), "Pastel Veil"), ally }, new[] { Mon("Grimer", 50, "Toxic"), Mon("Snorlax", 50) });
        AllIdle(core, (Foe, 0, Mine2));
        Assert.Equal(StatusCondition.None, ally.Status);
    }

    // ------------------------------------------------------------------ a berry's own course

    [Fact]
    public void CheekPouchGivesAThirdBackWhenItEatsABerry()
    {
        var dedenne = With(Mon("Dedenne", 50), "Cheek Pouch", "Sitrus Berry");
        int start = dedenne.MaxHP * 2 / 5;
        AtHp(dedenne, start);
        Duel(dedenne, Mon("Snorlax", 50));
        Assert.Null(dedenne.HeldItem);
        Assert.Equal(System.Math.Min(dedenne.MaxHP, start + dedenne.MaxHP / 4 + dedenne.MaxHP / 3), dedenne.CurrentHP);
    }

    [Fact]
    public void RipenDoublesWhatABerryHeals()
    {
        var appletun = With(Mon("Appletun", 50), "Ripen", "Sitrus Berry");
        int start = appletun.MaxHP * 2 / 5;
        AtHp(appletun, start);
        var core = Duel(appletun, Mon("Snorlax", 50));
        Assert.Equal(System.Math.Min(appletun.MaxHP, start + 2 * (appletun.MaxHP / 4)), appletun.CurrentHP);
        Assert.Contains(Lines(core.Log), s => s.Contains("ripened the Berry"));
    }

    [Fact]
    public void RipenDoublesWhatABerryRaises()
    {
        var appletun = With(Mon("Appletun", 50), "Ripen", "Liechi Berry");
        AtHp(appletun, appletun.MaxHP / 5);
        Duel(appletun, Mon("Snorlax", 50));
        Assert.Equal(2, Stage(appletun, StatType.Attack));
    }

    [Fact]
    public void RipenMakesAResistBerryTakeThreeQuartersOfAHitAway()
    {
        int with = Took(Mon("Charizard", 50, "Flamethrower"), With(Mon("Venusaur", 50), "Ripen", "Occa Berry"));
        int without = Took(Mon("Charizard", 50, "Flamethrower"), With(Mon("Venusaur", 50), "Run Away", "Occa Berry"));
        Near(0.5, with, without);
    }

    [Fact]
    public void CudChewEatsTheBerryAgainAtTheEndOfTheNextTurn()
    {
        var farigiraf = With(Mon("Farigiraf", 50), "Cud Chew", "Sitrus Berry");
        AtHp(farigiraf, farigiraf.MaxHP * 2 / 5);
        var core = Duel(farigiraf, Mon("Snorlax", 50));
        int after = farigiraf.CurrentHP;
        Assert.DoesNotContain(Turn(core, 0, 0), s => s.Contains("once more"));
        var said = Turn(core, 0, 0);
        Assert.Contains(said, s => s.Contains("chewed its Sitrus Berry once more"));
        Assert.Equal(System.Math.Min(farigiraf.MaxHP, after + farigiraf.MaxHP / 4), farigiraf.CurrentHP);
    }

    [Fact]
    public void SymbiosisHandsItsItemToAnAllyThatUsedItsOwn()
    {
        var florges = With(Mon("Florges", 50), "Symbiosis", "Leftovers");
        var snorlax = With(Mon("Snorlax", 50), item: "Sitrus Berry");
        AtHp(snorlax, snorlax.MaxHP * 2 / 5);
        Pair(new[] { florges, snorlax }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        Assert.Equal("Leftovers", snorlax.HeldItem?.Name);
        Assert.Null(florges.HeldItem);
    }

    // ------------------------------------------------------------------ an ally's ability, and leaving

    [Fact]
    public void ReceiverTakesOnAFaintedAllysAbility()
    {
        var passimian = With(Mon("Passimian", 50), "Receiver");
        var core = Pair(new[] { passimian, AtHp(With(Mon("Gyarados", 50), "Intimidate"), 1) }, new[] { Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50) });
        AllIdle(core, (Foe, 0, Mine2));
        Assert.Equal("Intimidate", passimian.AbilityName);
    }

    [Fact]
    public void ReceiverLeavesAnAbilityThatCantBeTakenOn()
    {
        var passimian = With(Mon("Passimian", 50), "Receiver");
        var core = Pair(new[] { passimian, AtHp(With(Mon("Ditto", 50), "Imposter"), 1) }, new[] { Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50) });
        AllIdle(core, (Foe, 0, Mine2));
        Assert.Equal("Receiver", passimian.AbilityName);
    }

    [Fact]
    public void WimpOutSendsItBackWhenAHitTakesItToHalfItsHp()
    {
        var golisopod = With(Mon("Golisopod", 50), "Wimp Out");
        AtHp(golisopod, golisopod.MaxHP / 2 + 2);
        var core = Versus(new[] { golisopod, Mon("Snorlax", 50) }, new[] { Mon("Snorlax", 50, "Tackle") });
        var said = Turn(core, 0, 0);
        Assert.Contains(said, s => s.Contains("out of harm's way"));
        Assert.IsType<ReplacementRequest>(core.Request);
        SendIn(core, 1);
        Assert.Equal("Snorlax", core.At(Mine).Pokemon!.Species.Name);
    }

    [Fact]
    public void AWildPokemonWithEmergencyExitFleesTheBattle()
    {
        var golisopod = With(Mon("Golisopod", 50), "Emergency Exit");
        AtHp(golisopod, golisopod.MaxHP / 2 + 2);
        var core = Duel(Mon("Snorlax", 50, "Tackle"), golisopod);
        Turn(core, 0, 0);
        Assert.Equal(BattleResult.EnemyFled, core.Result);
    }

    [Fact]
    public void EmergencyExitDoesNothingForAHitThatLeavesItOverHalf()
    {
        var golisopod = With(Mon("Golisopod", 50), "Emergency Exit");
        var core = Duel(Mon("Pidgey", 5, "Tackle"), golisopod);
        Turn(core, 0, 0);
        Assert.Equal(BattleResult.None, core.Result);
    }

    // ------------------------------------------------------------------ copying a foe

    [Fact]
    public void DancerDancesAFoesDanceToo()
    {
        var oricorio = With(Mon("Oricorio", 50), "Dancer");
        var core = Duel(oricorio, Mon("Snorlax", 50, "Swords Dance"));
        var said = Turn(core, 0, 0);
        Assert.Equal(2, Stage(oricorio, StatType.Attack));
        Assert.Contains(said, s => s.Contains("joined in the dance"));
    }

    [Fact]
    public void OpportunistCopiesAFoesStatRise()
    {
        var espathra = With(Mon("Espathra", 50), "Opportunist");
        var core = Duel(espathra, Mon("Snorlax", 50, "Swords Dance"));
        Turn(core, 0, 0);
        Assert.Equal(2, Stage(espathra, StatType.Attack));
    }

    [Fact]
    public void OpportunistLeavesItsOwnSidesRisesAlone()
    {
        var espathra = With(Mon("Espathra", 50), "Opportunist");
        var core = Pair(new[] { espathra, Mon("Snorlax", 50, "Swords Dance") }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        AllIdle(core);
        Assert.Equal(0, Stage(espathra, StatType.Attack));
    }

    // ------------------------------------------------------------------ Commander

    [Fact]
    public void CommanderSendsTatsugiriIntoDondozosMouth()
    {
        var tatsugiri = With(Mon("Tatsugiri", 50), "Commander");
        var dondozo = Mon("Dondozo", 50, "Tackle");
        var core = Pair(new[] { tatsugiri, dondozo }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed })
            Assert.Equal(2, Stage(dondozo, stat));
        Assert.Equal(Elsewhere.Vanished, core.At(Mine).Volatile.Elsewhere);
        Assert.Contains(Everything(core.Log), e => e is Vanished { Place: var p } && p == Mine);
        Assert.False(core.CanSwitchOut(core.At(Mine2)));
        Assert.False(core.CanSwitchOut(core.At(Mine)));
    }

    [Fact]
    public void ATatsugiriInCommandIsOutOfEveryMovesReach()
    {
        var tatsugiri = With(Mon("Tatsugiri", 50), "Commander");
        var core = Pair(new[] { tatsugiri, Mon("Dondozo", 50) }, new[] { Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50) });
        AllIdle(core, (Foe, 0, Mine));
        Assert.Equal(tatsugiri.MaxHP, tatsugiri.CurrentHP);
    }

    [Fact]
    public void OrderUpRaisesTheStatOfTheTatsugirisForm()
    {
        var dondozo = Mon("Dondozo", 50, "Order Up");
        var core = Pair(new[] { With(InForm(Mon("Tatsugiri", 50), "Tatsugiri-Droopy"), "Commander"), dondozo }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        AllIdle(core, (Mine2, 0, Foe));
        Assert.Equal(3, Stage(dondozo, StatType.Defense));
        Assert.Equal(2, Stage(dondozo, StatType.Attack));
    }

    [Fact]
    public void OrderUpIsAPlainHitWithNobodyInCommand()
    {
        var dondozo = Mon("Dondozo", 50, "Order Up");
        var core = Duel(dondozo, Mon("Snorlax", 50));
        Turn(core, 0, 0);
        Assert.Equal(0, Stage(dondozo, StatType.Attack));
    }

    // ------------------------------------------------------------------ forms in battle

    [Fact]
    public void BattleBondRaisesThreeStatsForAKnockOutOnceABattle()
    {
        var greninja = With(InForm(Mon("Greninja", 60, "Surf"), "Greninja-Battle-Bond"), "Battle Bond");
        var core = Versus(new[] { greninja }, new[] { Mon("Magikarp", 2), Mon("Magikarp", 2), Mon("Magikarp", 2) });
        Turn(core, 0, 0);
        foreach (var stat in new[] { StatType.Attack, StatType.SpAttack, StatType.Speed }) Assert.Equal(1, Stage(greninja, stat));
        core.Submit(BattleChoice.Switch(Foe, 1));
        core.TakeLog();
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(greninja, StatType.SpAttack));
    }

    [Fact]
    public void PowerConstructCompletesZygardeAtHalfItsHp()
    {
        var zygarde = With(InForm(Mon("Zygarde", 60), "Zygarde-50-Power-Construct"), "Power Construct");
        int most = zygarde.MaxHP;
        AtHp(zygarde, most / 2);
        var core = Duel(zygarde, Mon("Snorlax", 50));
        core.TakeLog();
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        Assert.Equal("Zygarde-Complete", zygarde.Form);
        Assert.True(zygarde.MaxHP > most);
        Assert.Equal(most / 2 + zygarde.MaxHP - most, zygarde.CurrentHP);
        Assert.Contains(Everything(log), e => e is FormHpChanged { MaxHp: var m } && m == zygarde.MaxHP);
    }

    [Fact]
    public void RksSystemMakesSilvallyTheTypeOfItsMemory()
    {
        var core = Duel(With(Mon("Silvally", 50), "RKS System", "Fire Memory"), Mon("Snorlax", 50));
        Assert.Equal(new[] { PokemonType.Fire }, core.At(Mine).Types);
        var plain = Duel(With(Mon("Silvally", 50), "RKS System"), Mon("Snorlax", 50));
        Assert.Equal(new[] { PokemonType.Normal }, plain.At(Mine).Types);
    }

    // ------------------------------------------------------------------ Ball Fetch and Mega Sol

    [Fact]
    public void BallFetchPicksUpTheFirstBallThatFailed()
    {
        var yamper = With(Mon("Yamper", 20), "Ball Fetch");
        var core = Duel(yamper, Mon("Bidoof", 10), Calm().Force(RollKind.CatchShake, 65535));
        core.Submit(BattleChoice.UseItem(Mine, "Poké Ball"), BattleChoice.Fight(Foe, 0));
        Assert.Equal("Poké Ball", yamper.HeldItem?.Name);
        Assert.Contains(Lines(core.TakeLog()), s => s.Contains("fetched the Poké Ball"));
    }

    [Fact]
    public void MegaSolHitsAsUnderTheSun()
    {
        int with = Took(With(Mon("Meganium", 50, "Flamethrower"), "Mega Sol"), Mon("Snorlax", 50));
        int without = Took(With(Mon("Meganium", 50, "Flamethrower"), "Run Away"), Mon("Snorlax", 50));
        Near(1.5, with, without);
        int water = Took(With(Mon("Meganium", 50, "Surf"), "Mega Sol"), Mon("Snorlax", 50));
        int waterWithout = Took(With(Mon("Meganium", 50, "Surf"), "Run Away"), Mon("Snorlax", 50));
        Near(0.5, water, waterWithout);
    }

    [Fact]
    public void MegaSolTakesTheSunOverTheRain()
    {
        int with = Took(With(Mon("Meganium", 50, "Flamethrower"), "Mega Sol"), Mon("Snorlax", 50), BattleWeather.Rain);
        int without = Took(With(Mon("Meganium", 50, "Flamethrower"), "Run Away"), Mon("Snorlax", 50));
        Near(1.5, with, without);
    }

    // ------------------------------------------------------------------ R27's rules the audit set right

    [Fact]
    public void RattledRaisesItsSpeedWhenIntimidated()
    {
        var dunsparce = With(Mon("Dunsparce", 50), "Rattled");
        Duel(With(Mon("Gyarados", 50), "Intimidate"), dunsparce);
        Assert.Equal(-1, Stage(dunsparce, StatType.Attack));
        Assert.Equal(1, Stage(dunsparce, StatType.Speed));
    }

    [Fact]
    public void MirrorArmorTurnsNothingBackFromAStatAtItsLowest()
    {
        var foe = With(Mon("Corviknight", 50), "Mirror Armor");
        var mine = Mon("Raichu", 50, "Growl");
        var core = Duel(mine, foe);
        foe.StatStages[StatType.Attack] = -6;
        Turn(core, 0, 0);
        Assert.Equal(0, Stage(mine, StatType.Attack));
    }

    [Fact]
    public void WindRiderRaisesItsAttackWhenATailwindStarts()
    {
        var bramble = With(Mon("Brambleghast", 50, "Tailwind"), "Wind Rider");
        var core = Duel(bramble, Mon("Snorlax", 50));
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(bramble, StatType.Attack));
    }

    [Fact]
    public void WindPowerChargesWhenATailwindStarts()
    {
        var core = Duel(With(Mon("Kilowattrel", 50, "Tailwind"), "Wind Power"), Mon("Snorlax", 50));
        Turn(core, 0, 0);
        Assert.True(core.At(Mine).Volatile.ChargeTurns > 0);
    }

    [Fact]
    public void ProtosynthesisKeepsTheStatItTookHoldWith()
    {
        var tusk = With(Mon("Great Tusk", 50, "Iron Defense"), "Protosynthesis");
        var core = Duel(tusk, Mon("Snorlax", 50), sky: BattleWeather.Sun);
        Assert.Equal(StatType.Attack, LaterAbilities.ParadoxStat(core.At(Mine)));
        for (int i = 0; i < 3; i++) Turn(core, 0, 0);
        Assert.True(Formulas.Staged(tusk.Defense, Stage(tusk, StatType.Defense)) > tusk.Attack);
        Assert.Equal(StatType.Attack, LaterAbilities.ParadoxStat(core.At(Mine)));
    }

    [Fact]
    public void PoisonPuppeteerIsPecharuntsAlone()
    {
        var foe = Mon("Regirock", 50);
        var core = Duel(With(Mon("Grimer", 50, "Toxic"), "Poison Puppeteer"), foe);
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.Toxic, foe.Status);
        Assert.False(core.At(Foe).IsConfused);
    }

    [Fact]
    public void PoisonTouchDoesntGetPastShieldDust()
    {
        var foe = With(Mon("Snorlax", 50), "Shield Dust");
        var core = Duel(With(Mon("Grimer", 50, "Pound"), "Poison Touch"), foe, Calm().Force(RollKind.AbilityChance, 0));
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void MindsEyeTakesNoNoticeOfTheTargetsEvasion()
    {
        bool Hit(string ability)
        {
            var foe = Mon("Snorlax", 50);
            var core = Duel(With(Mon("Ursaluna", 50, "Tackle"), ability), foe, Calm().Force(RollKind.Accuracy, 50));
            foe.StatStages[StatType.Evasion] = 6;
            int hp = foe.CurrentHP;
            Turn(core, 0, 0);
            return foe.CurrentHP < hp;
        }
        Assert.True(Hit("Mind's Eye"));
        Assert.False(Hit("Run Away"));
    }

    [Fact]
    public void LightMetalHalvesTheWeightInWholeHectograms()
    {
        // Skarmory weighs 50.5 kg, 505 hg: halved and truncated, 252 hg
        var core = Duel(With(Mon("Skarmory", 50), "Light Metal"), Mon("Snorlax", 50));
        Assert.Equal(25.2f, BattleCore.WeightOf(core.At(Mine)), 3);
    }

    [Fact]
    public void SupremeOverlordsPowerIsShowdownsTableOfFallen()
    {
        // Three fallen: 5325/4096, where R27's 4096 + 410 a faint made 5326
        var down = Enumerable.Range(0, 3).Select(_ => AtHp(Mon("Bidoof", 5), 0)).ToArray();
        var core = Duel(With(Mon("Kingambit", 50, "Iron Head"), "Supreme Overlord"), Mon("Snorlax", 50), bench: down);
        Assert.Equal(3, core.At(Mine).Volatile.Fallen);
    }

    /// <summary>Every event of a log, those that ride on its lines too.</summary>
    private static IEnumerable<BattleEvent> Everything(IEnumerable<BattleEvent> log) =>
        log.SelectMany(e => e is Said s ? new[] { e }.Concat(s.Shows).Concat(s.OnImpact) : new[] { e });

    private static Pokemon Fast(Pokemon p)
    {
        p.Speed = 999;
        return p;
    }
}
