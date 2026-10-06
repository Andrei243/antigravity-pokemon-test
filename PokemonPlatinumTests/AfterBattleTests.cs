using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 06 · R10, what a battle leaves behind and what the team carries from one to the next: effort values (from
/// battles, vitamins and berries), Pokérus, a traded Pokémon's obedience, the money a lost battle costs, the
/// forms Platinum's own Pokémon take outside battle, the question of a move to learn, and the modern rules' EXP.
/// The numbers are worked out from the decompilation's code in a comment beside each.
/// </summary>
public class AfterBattleTests
{
    /// <summary>The original trainer of a Pokémon from someone else, and the player who isn't them.</summary>
    private static readonly TrainerMark Stranger = new("Kaz", 54321, PlayerLook.Boy);
    private static readonly TrainerMark Player = new("Lucas", 12345, PlayerLook.Boy);

    private static Pokemon Traded(Pokemon p)
    {
        p.OriginalTrainer = Stranger;
        return p;
    }

    private static Pokemon Weak(Pokemon p)
    {
        p.CurrentHP = 1;
        return p;
    }

    /// <summary>A wild battle on the rules alone, with the world outside it as given.</summary>
    private static BattleCore WildIn(BattleConditions conditions, Pokemon mine, Pokemon foe, BattleRandom? rolls = null, Ruleset? rules = null)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = rolls ?? Calm(), Rules = rules ?? Ruleset.Platinum,
            Conditions = conditions, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    private static BattleConditions Badges(int badges, int money = 0) => new() { Badges = badges, Money = money, Player = Player };

    // ================================================================== effort values

    [Fact]
    public void EffortIsGainedStatByStatInTheOriginalsOrderUpToItsLimits()
    {
        // BattleScript_CalcEffortValues: HP, Attack, Defense, Speed, Sp. Atk, Sp. Def; once the total is 510 the
        // rest is skipped. With 505 already, a yield of 3 HP and 3 Speed gives 3 HP and only 2 Speed.
        var p = Mon("Machop", 30);
        p.EvAttack = 252;
        p.EvDefense = 253;
        EffortRules.Gain(p, new StatSpread { HP = 3, Speed = 3 }, Ruleset.Platinum);
        Assert.Equal(3, p.EvHP);
        Assert.Equal(2, p.EvSpeed);
        Assert.Equal(EffortRules.MaxTotal, EffortRules.Total(p));

        // And no stat past 255
        var q = Mon("Machop", 30);
        q.EvAttack = 254;
        EffortRules.Gain(q, new StatSpread { Attack = 2 }, Ruleset.Platinum);
        Assert.Equal(255, q.EvAttack);
    }

    [Fact]
    public void APowerItemAddsItsNumberAndPokerusAndTheMachoBraceDouble()
    {
        // The Power Weight's 4 is added to HP's yield before the doublings: (0 + 4) × 2 for Pokérus × 2 for... it
        // holds one item only, so the Macho Brace is a second Pokémon: (1 × 2) × 2 = 4 Defense
        var weight = With(Mon("Machop", 30), item: "Power Weight");
        weight.Pokerus = 0x14;
        EffortRules.Gain(weight, new StatSpread { Defense = 1 }, Ruleset.Platinum);
        Assert.Equal(8, weight.EvHP);
        Assert.Equal(2, weight.EvDefense);

        var brace = With(Mon("Machop", 30), item: "Macho Brace");
        brace.Pokerus = 0x10;   // cured still doubles
        EffortRules.Gain(brace, new StatSpread { Defense = 1 }, Ruleset.Platinum);
        Assert.Equal(4, brace.EvDefense);
    }

    [Fact]
    public void BeatingAFoeGivesItsEffortToWhoFoughtAndShowsAtTheNextLevel()
    {
        // Geodude yields 1 Defense. The effort is counted before the level, so a level reached by the same EXP
        // works its stats out with it; a Pokémon that didn't level keeps the stats it had
        var machop = Mon("Machop", 30, "Karate Chop");
        int defense = machop.Defense;
        Turn(Wild(machop, Weak(Mon("Geodude", 5))));
        Assert.Equal(1, machop.EvDefense);
        Assert.Equal(defense, machop.Defense);
    }

    [Fact]
    public void ALevel100PokemonGainsNoEffortByPlatinumsRulesButDoesByTheModernOnes()
    {
        var platinum = Mon("Machop", 100, "Karate Chop");
        Turn(Wild(platinum, Weak(Mon("Geodude", 5))));
        Assert.Equal(0, platinum.EvDefense);

        var modern = Mon("Machop", 100, "Karate Chop");
        Turn(Wild(modern, Weak(Mon("Geodude", 5)), rules: Ruleset.Modern));
        Assert.Equal(1, modern.EvDefense);
    }

    [Fact]
    public void AVitaminRaisesAStatOnlyUpToOneHundred()
    {
        // CalculateEVUpdate: +10 stops at 100; at 100 it does nothing, and nothing is used up
        var p = Mon("Machop", 30);
        p.EvHP = 95;
        int hp = p.MaxHP;
        Assert.True(EffortRules.UseItem(ItemDatabase.Get("HP Up")!, p, Ruleset.Platinum));
        Assert.Equal(100, p.EvHP);
        Assert.False(EffortRules.WouldHelp(ItemDatabase.Get("HP Up")!, p, Ruleset.Platinum));
        Assert.False(EffortRules.UseItem(ItemDatabase.Get("HP Up")!, p, Ruleset.Platinum));
        // 100 / 4 × 30 / 100 = 7 more HP, worked out at once
        Assert.Equal(hp + 7, p.MaxHP);

        // The modern rules let it go on to 252
        Assert.True(EffortRules.UseItem(ItemDatabase.Get("HP Up")!, p, Ruleset.Modern));
        Assert.Equal(110, p.EvHP);
    }

    [Fact]
    public void ABerryBringsAStatOverOneHundredDownToItAndStillPleasesAtNothing()
    {
        var p = Mon("Machop", 30);
        p.EvHP = 150;
        p.Friendship = 70;
        Assert.True(EffortRules.UseItem(ItemDatabase.Get("Pomeg Berry")!, p, Ruleset.Platinum));
        Assert.Equal(100, p.EvHP);
        Assert.Equal(80, p.Friendship);   // +10 below 100

        // At nothing it still raises friendship, until that is full
        p.EvHP = 0;
        Assert.True(EffortRules.UseItem(ItemDatabase.Get("Pomeg Berry")!, p, Ruleset.Platinum));
        Assert.Equal(90, p.Friendship);
        p.Friendship = FriendshipRules.Max;
        Assert.False(EffortRules.WouldHelp(ItemDatabase.Get("Pomeg Berry")!, p, Ruleset.Platinum));
    }

    [Fact]
    public void AnItemsFriendshipIsRaisedByTheSootheBellThenTheLuxuryBall()
    {
        // UpdatePokemonFriendship: × 150 / 100 for the Soothe Bell, then + 1 for a Luxury Ball: 5 → 7 → 8
        var p = With(Mon("Machop", 30), item: "Soothe Bell");
        p.Ball = FriendshipRules.LuxuryBall;
        p.Friendship = 70;
        EffortRules.UseItem(ItemDatabase.Get("Protein")!, p, Ruleset.Platinum);
        Assert.Equal(78, p.Friendship);
    }

    // ================================================================== Pokérus

    /// <summary>A generator that gives the draws it was handed, in order.</summary>
    private sealed class Draws : Random
    {
        private readonly Queue<int> values;
        public Draws(params int[] values) => this.values = new Queue<int>(values);
        public override int Next(int maxValue) => values.Dequeue() % maxValue;
    }

    [Fact]
    public void PokerusIsCaughtOnThreeDrawsInSixtyFiveThousand()
    {
        // Pokemon_ApplyPokerus: 16384 catches it; the second Pokémon is drawn (1 % 2); its strain from 0x23: & 7 is
        // 3, the high bits are cleared, 0x33 & 0xF3 = 0x33, + 1 = 0x34 (strain 3, four days)
        var party = new Party();
        party.Add(Mon("Bidoof", 5));
        party.Add(Mon("Starly", 5));
        PokerusRules.TryInfect(party, new Draws(16384, 1, 0x23));
        Assert.Equal(0, party.Members[0].Pokerus);
        Assert.Equal(0x34, party.Members[1].Pokerus);

        // Any other draw passes
        var clean = new Party();
        clean.Add(Mon("Bidoof", 5));
        PokerusRules.TryInfect(clean, new Draws(16383));
        Assert.Equal(0, clean.Members[0].Pokerus);
    }

    [Fact]
    public void PokerusSpreadsToItsNeighboursOneTimeInThree()
    {
        // Pokemon_ValidatePokerus: on a draw that is 0 modulo 3, a carrier passes its byte to the one before and the
        // one after it that never had it; the one after is then passed over
        var party = new Party();
        foreach (var name in new[] { "Bidoof", "Starly", "Shinx", "Kricketot" }) party.Add(Mon(name, 5));
        party.Members[1].Pokerus = 0x34;
        party.Members[3].Pokerus = 0x10;   // over it: never catches it again
        PokerusRules.Spread(party, new Draws(1));
        Assert.Equal(0, party.Members[0].Pokerus);

        PokerusRules.Spread(party, new Draws(3));
        Assert.Equal(0x34, party.Members[0].Pokerus);
        Assert.Equal(0x34, party.Members[2].Pokerus);
        Assert.Equal(0x10, party.Members[3].Pokerus);
    }

    [Fact]
    public void PokerusRunsItsDaysAndLeavesItsMark()
    {
        // Party_UpdatePokerusStatus: a day off each day; more days than are left, or more than four, cure it
        var party = new Party();
        party.Add(Mon("Bidoof", 5));
        party.Add(Mon("Starly", 5));
        party.Members[0].Pokerus = 0x34;
        party.Members[1].Pokerus = 0x21;
        PokerusRules.DaysPass(party, 1);
        Assert.Equal(0x33, party.Members[0].Pokerus);
        Assert.True(PokerusRules.Cured(party.Members[1]));
        Assert.Equal("PKRS", PokerusRules.Tag(party.Members[0]));
        Assert.Null(PokerusRules.Tag(party.Members[1]));

        PokerusRules.DaysPass(party, 5);
        Assert.Equal(0x30, party.Members[0].Pokerus);
        Assert.False(PokerusRules.AnyInfected(party));
    }

    // ================================================================== obedience

    [Fact]
    public void TheBadgesSetTheLevelATradedPokemonObeysTo()
    {
        Assert.Equal(10, Obedience.LevelCap(0));
        Assert.Equal(10, Obedience.LevelCap(1));
        Assert.Equal(30, Obedience.LevelCap(2));
        Assert.Equal(50, Obedience.LevelCap(5));
        Assert.Equal(70, Obedience.LevelCap(7));
        Assert.Null(Obedience.LevelCap(8));

        // Caught by the player (no mark), or marked with the player's own name, ID and look
        Assert.False(Obedience.IsOutsider(Mon("Machop", 30), Player));
        var own = Mon("Machop", 30);
        own.OriginalTrainer = Player;
        Assert.False(Obedience.IsOutsider(own, Player));
        Assert.True(Obedience.IsOutsider(Traded(Mon("Machop", 30)), Player));
    }

    [Fact]
    public void ATradedPokemonOverTheCapCanDoNothing()
    {
        // Level 20 against a cap of 10: (255 × 30) >> 8 = 29, not under 10, so it disobeys; the second test fails
        // the same way; 255 is neither under 10 (a nap) nor is 255 - 10 (hurting itself); it does nothing
        var foe = Mon("Bidoof", 20);
        int hp = foe.CurrentHP;
        var said = Turn(WildIn(Badges(0), Traded(Mon("Machop", 20, "Karate Chop", "Leer")), foe, Calm().Force(RollKind.Obedience, 255)));
        Assert.Contains("Machop acted as if it hadn't heard!", said);
        Assert.DoesNotContain("Machop used Karate Chop!", said);
        Assert.Equal(hp, foe.CurrentHP);
    }

    [Fact]
    public void ATradedPokemonCanUseAnotherMoveOfItsOwn()
    {
        // The second test passes ((0 × 30) >> 8 = 0, under 10): it uses another move, drawn from its four places
        var said = Turn(WildIn(Badges(0), Traded(Mon("Machop", 20, "Karate Chop", "Leer")), Mon("Bidoof", 20),
            Calm().Force(RollKind.Obedience, 255, 0, 1)));
        InOrder(said, "Machop ignored the order and did as it liked!", "Machop used Leer!");
    }

    [Fact]
    public void ATradedPokemonCanNapOrHurtItself()
    {
        // The third roll against the 10 levels over the cap: 5 is under them, a nap
        var napper = Traded(Mon("Machop", 20, "Karate Chop"));
        var said = Turn(WildIn(Badges(0), napper, Mon("Bidoof", 20), Calm().Force(RollKind.Obedience, 255, 255, 5)));
        Assert.Contains("Machop lay down for a nap!", said);
        Assert.Equal(StatusCondition.Sleep, napper.Status);

        // 15 isn't, but 15 - 10 is: a typeless hit of 40 on itself
        var hurt = Traded(Mon("Machop", 20, "Karate Chop"));
        int hp = hurt.CurrentHP;
        said = Turn(WildIn(Badges(0), hurt, Mon("Bidoof", 20), Calm().Force(RollKind.Obedience, 255, 255, 15)));
        InOrder(said, "Machop won't do as it's told!", "It hurt itself in its confusion!");
        Assert.True(hurt.CurrentHP < hp);
    }

    [Fact]
    public void ThePlayersOwnPokemonAndTheEightBadgesAlwaysObey()
    {
        var rolls = Calm().Force(RollKind.Obedience, 255);
        Assert.Contains("Machop used Karate Chop!", Turn(WildIn(Badges(0), Mon("Machop", 90, "Karate Chop"), Mon("Bidoof", 20), rolls)));
        Assert.Contains("Machop used Karate Chop!", Turn(WildIn(Badges(8), Traded(Mon("Machop", 90, "Karate Chop")), Mon("Bidoof", 20), rolls)));
        Assert.Contains("Machop used Karate Chop!", Turn(WildIn(Badges(2), Traded(Mon("Machop", 30, "Karate Chop")), Mon("Bidoof", 20), rolls)));
    }

    [Fact]
    public void ATradedPokemonGainsBoostedExp()
    {
        // Platinum: half as much again for a Pokémon from another trainer (BattleSystem_CalcExp... × 150 / 100)
        Assert.Equal(150, Formulas.ExpFor(100, luckyEgg: false, trainerBattle: false, traded: true));
        Assert.Equal(337, Formulas.ExpFor(100, luckyEgg: true, trainerBattle: true, traded: true));
        var said = Turn(WildIn(Badges(8), Traded(Mon("Machop", 30, "Karate Chop")), Weak(Mon("Geodude", 5))));
        Assert.Contains(said, line => line.StartsWith("Machop gained a boosted ", StringComparison.Ordinal));
    }

    // ================================================================== losing

    [Fact]
    public void LosingCostsMoneyByTheHighestLevelAndTheBadges()
    {
        // BattleSystem_CalcMoneyPenalty: highest level × 4 × the badges' step (2, 4, 6, 9, 12, 16, 20, 25, 30)
        Assert.Equal(160, Formulas.MoneyPenalty(20, 0, 10_000));
        Assert.Equal(20 * 4 * 12, Formulas.MoneyPenalty(20, 4, 10_000));
        Assert.Equal(3000, Formulas.MoneyPenalty(25, 8, 10_000));
        Assert.Equal(100, Formulas.MoneyPenalty(20, 0, 100));
    }

    [Fact]
    public void ALostBattleSaysWhatItCostAndWhitesOut()
    {
        // A level-5 Magikarp on 1 HP falls to a Tackle: 5 × 4 × 2 = 40 dropped before a wild Pokémon
        var core = WildIn(Badges(0, 1000), Weak(Mon("Magikarp", 5)), Mon("Bidoof", 30, "Tackle"));
        var said = Turn(core);
        InOrder(said, "Lucas has no Pokémon left that can fight!", "Lucas dropped $40 in the panic!", "...  ...  ...", "Lucas whited out!");
        Assert.Equal(40, core.MoneyLost);
        Assert.Equal(BattleResult.PlayerDefeat, core.Result);

        // Nothing to lose, nothing said of it
        core = WildIn(Badges(0, 0), Weak(Mon("Magikarp", 5)), Mon("Bidoof", 30, "Tackle"));
        said = Turn(core);
        Assert.Equal(0, core.MoneyLost);
        Assert.DoesNotContain(said, line => line.Contains("panic", StringComparison.Ordinal));
    }

    // ================================================================== forms

    [Fact]
    public void BurmyTakesTheCloakOfTheGroundItFoughtOn()
    {
        // BattleSystem_SetBurmyForm: sand and open ground the sandy cloak, a building the trash cloak, grass the plant
        var sandy = Mon("Burmy", 20, "Tackle");
        Turn(Wild(sandy, Weak(Mon("Magikarp", 2)), ground: BattleTerrain.Sand));
        Assert.Equal("Burmy-Sandy", sandy.Form);

        var trash = Mon("Burmy", 20, "Tackle");
        Turn(Wild(trash, Weak(Mon("Magikarp", 2)), ground: BattleTerrain.Building));
        Assert.Equal("Burmy-Trash", trash.Form);

        Turn(Wild(trash, Weak(Mon("Magikarp", 2)), ground: BattleTerrain.Grass));
        Assert.Null(trash.Form);

        // One left on the bench keeps its cloak
        var benched = Mon("Burmy", 20);
        Turn(Wild(Mon("Machop", 30, "Karate Chop"), Weak(Mon("Magikarp", 2)), ground: BattleTerrain.Sand, bench: benched));
        Assert.Null(benched.Form);
    }

    [Fact]
    public void ShellosAndUnownAreMetInTheirPlacesForms()
    {
        var shellos = PokemonDatabase.Get("Shellos")!;
        Assert.Equal("Shellos-East", FormRules.WildForm(shellos, eastSea: true, 0, new Random(1)));
        Assert.Null(FormRules.WildForm(shellos, eastSea: false, 0, new Random(1)));
        Assert.Equal("Gastrodon-East", FormRules.WildForm(PokemonDatabase.Get("Gastrodon")!, eastSea: true, 0, new Random(1)));

        // The way through the Solaceon Ruins spells FRIEND (tables 2 to 7), the dead ends have most letters, and
        // the room past Maniac Tunnel the two marks
        var unown = PokemonDatabase.Get("Unown")!;
        Assert.Equal("Unown-F", FormRules.WildForm(unown, false, 2, new Random(1)));
        Assert.Equal("Unown-N", FormRules.WildForm(unown, false, 5, new Random(1)));
        Assert.Equal("Unown-E", FormRules.WildForm(unown, false, 6, new Random(1)));
        Assert.Equal("Unown-D", FormRules.WildForm(unown, false, 7, new Random(1)));
        Assert.Contains(FormRules.WildForm(unown, false, 8, new Random(1)), new[] { "Unown-Exclamation", "Unown-Question" });
        Assert.All(Enumerable.Range(0, 40), seed =>
        {
            string? letter = FormRules.WildForm(unown, false, 1, new Random(seed));
            Assert.True(letter == null || (unown.Form(letter) != null && letter is not ("Unown-F" or "Unown-R" or "Unown-I" or "Unown-E" or "Unown-N" or "Unown-D")));
        });
    }

    [Fact]
    public void GiratinaAndArceusFollowWhatTheyHold()
    {
        var giratina = With(Mon("Giratina", 50), item: FormRules.GriseousOrb);
        Assert.True(FormRules.ByHeldItem(giratina));
        Assert.Equal("Giratina-Origin", giratina.Form);
        giratina.HeldItem = null;
        Assert.True(FormRules.ByHeldItem(giratina));
        Assert.Null(giratina.Form);

        var arceus = With(Mon("Arceus", 80), ability: "Multitype", item: "Flame Plate");
        Assert.True(FormRules.ByHeldItem(arceus));
        Assert.Equal("Arceus-Fire", arceus.Form);
        Assert.False(FormRules.ByHeldItem(arceus));
    }

    [Fact]
    public void ShayminTakesToTheSkyByDayAndComesBackWhenFrozenOrAtNight()
    {
        var shaymin = Mon("Shaymin", 30);
        Assert.True(FormRules.CanTakeToTheSky(shaymin, 12));
        Assert.False(FormRules.CanTakeToTheSky(shaymin, 21));
        Assert.True(FormRules.ShayminNight(21) && FormRules.ShayminNight(3) && !FormRules.ShayminNight(4));
        shaymin.Status = StatusCondition.Freeze;
        Assert.False(FormRules.CanTakeToTheSky(shaymin, 12));
        shaymin.Status = StatusCondition.None;

        // Frozen in battle, it is in its Land Forme again (subscript_check_shaymin_form)
        shaymin.ChangeForm("Shaymin-Sky");
        var said = Turn(Wild(shaymin, Mon("Glaceon", 10, "Ice Beam"), Calm().Force(RollKind.SideEffect, 0)));
        InOrder(said, "Foe Glaceon used Ice Beam!", "Shaymin was frozen solid!", "Shaymin changed back into its Land Forme!");
        Assert.Null(shaymin.Form);
    }

    // ================================================================== a move to learn

    /// <summary>A Turtwig a point short of level 9, where it learns Absorb, already knowing four moves.</summary>
    private static Pokemon FullTurtwig()
    {
        var turtwig = Mon("Turtwig", 8, "Tackle", "Withdraw", "Growl", "Leer");
        turtwig.CurrentExp = turtwig.ExpForNextLevel - 1;
        return turtwig;
    }

    /// <summary>Reads the battle on until its question about a move to learn opens; returns what was said.</summary>
    private static List<string> UntilAsked(BattleEngine battle)
    {
        var said = new List<string>();
        for (int i = 0; i < 400 && !battle.IsLearningMove && !battle.IsBattleOver; i++)
        {
            if (battle.IsWaitingForConfirm) said.Add(battle.CurrentMessage);
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return said;
    }

    [Fact]
    public void AMoveThatDoesntFitIsAskedAboutAndForgettingOneLearnsIt()
    {
        var battle = Battle(FullTurtwig(), Weak(Mon("Magikarp", 5)));
        battle.SelectMove(0);
        var said = UntilAsked(battle);
        InOrder(said, "Turtwig grew to Lv. 9!", "Turtwig is trying to learn Absorb.", "But Turtwig already knows four moves.");
        Assert.Equal(LearnStep.Forget, battle.LearnStep);
        Assert.Equal("Absorb", battle.MoveToLearn);

        battle.ChooseLearn(0);
        UntilAsked(battle);
        Assert.Equal(LearnStep.Choose, battle.LearnStep);
        battle.ChooseLearn(1);
        said = Settle(battle);
        InOrder(said, "Turtwig has forgotten Withdraw.", "Turtwig learned Absorb!");
        Assert.True(battle.IsBattleOver);

        // The game's Turtwig and the rules' copy of it both know it
        Assert.Equal(new[] { "Tackle", "Absorb", "Growl", "Leer" }, battle.PlayerPokemon.Moves.Select(m => m.Name));
        Assert.Equal(new[] { "Tackle", "Absorb", "Growl", "Leer" }, battle.Core.PlayerParty.Members[0].Moves.Select(m => m.Name));
    }

    [Fact]
    public void ReadingTheQuestionOnKeepsEveryMove()
    {
        // No to forgetting, then yes to giving up: what a battle read on with ConfirmMessage answers
        var battle = Battle(FullTurtwig(), Weak(Mon("Magikarp", 5)));
        var said = Turn(battle);
        InOrder(said, "Give up on learning Absorb?", "Turtwig did not learn Absorb.");
        Assert.True(battle.IsBattleOver);
        Assert.Equal(new[] { "Tackle", "Withdraw", "Growl", "Leer" }, battle.PlayerPokemon.Moves.Select(m => m.Name));
    }

    [Fact]
    public void NotGivingUpAsksAgain()
    {
        var battle = Battle(FullTurtwig(), Weak(Mon("Magikarp", 5)));
        battle.SelectMove(0);
        UntilAsked(battle);
        battle.ChooseLearn(1);   // no, keep them
        UntilAsked(battle);
        Assert.Equal(LearnStep.GiveUp, battle.LearnStep);
        battle.ChooseLearn(1);   // no, don't give up
        var said = UntilAsked(battle);
        Assert.Contains("Turtwig is trying to learn Absorb.", said);
        Assert.Equal(LearnStep.Forget, battle.LearnStep);
    }

    // ================================================================== the modern rules' EXP

    [Fact]
    public void TheModernRulesScaleExpByTheLevels()
    {
        // base × foe level / 5 × ((2 × foe + 10) / (foe + level + 10))^2.5, + 1: at the same level the scale is 1,
        // 64 × 10 / 5 = 128 + 1 = 129; five levels under, 128 × 1.2^2.5 = 201.9 → 201 + 1 = 202; not fighting, half
        Assert.Equal(129, Formulas.ScaledExp(64, 10, 10, fought: true, traded: false, luckyEgg: false, pastEvolution: false));
        Assert.Equal(202, Formulas.ScaledExp(64, 10, 5, fought: true, traded: false, luckyEgg: false, pastEvolution: false));
        Assert.Equal(101, Formulas.ScaledExp(64, 10, 5, fought: false, traded: false, luckyEgg: false, pastEvolution: false));
        // 129 × 1.5 = 193, × 1.5 = 289, × 1.2 = 346
        Assert.Equal(346, Formulas.ScaledExp(64, 10, 10, fought: true, traded: true, luckyEgg: true, pastEvolution: true));
    }

    [Fact]
    public void TheModernRulesGiveExpToTheWholeTeam()
    {
        // The modern Exp. Share is always on: the one on the bench gains half
        var bench = Mon("Bidoof", 10);
        int exp = bench.CurrentExp;
        Turn(Wild(Mon("Machop", 10, "Karate Chop"), Weak(Mon("Geodude", 10)), rules: Ruleset.Modern, bench: bench));
        Assert.True(bench.CurrentExp > exp);

        var platinumBench = Mon("Bidoof", 10);
        exp = platinumBench.CurrentExp;
        Turn(Wild(Mon("Machop", 10, "Karate Chop"), Weak(Mon("Geodude", 10)), bench: platinumBench));
        Assert.Equal(exp, platinumBench.CurrentExp);
    }

    // ================================================================== saved

    [Fact]
    public void EffortTheOriginalTrainerAndPokerusAreSaved()
    {
        var p = Traded(Mon("Machop", 30, "Karate Chop"));
        p.EvHP = 12; p.EvAttack = 34; p.EvDefense = 56; p.EvSpAttack = 78; p.EvSpDefense = 90; p.EvSpeed = 100;
        p.Pokerus = 0x33;
        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal((12, 34, 56, 78, 90, 100), (back.EvHP, back.EvAttack, back.EvDefense, back.EvSpAttack, back.EvSpDefense, back.EvSpeed));
        Assert.Equal(0x33, back.Pokerus);
        Assert.Equal(Stranger, back.OriginalTrainer);
        Assert.Equal(p.MaxHP, back.MaxHP);
    }
}
