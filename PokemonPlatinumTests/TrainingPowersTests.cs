using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 36's last three sessions: TR5, the Tera Shards and the Tera keeper (<see cref="TeraChange"/>, <c>train tera</c>);
/// TR7, the powers' rule and its readers (<see cref="PowerState"/>, <see cref="PowerSet"/>: EXP, effort, money, catching,
/// prices, the charge, today's free power, NO POWERS); TR8, the Power Watch's panel (<see cref="PowersScreen"/>), its
/// chips over the field and the teachers (<c>power learn</c>). The numbers are <c>Data/powers.json</c>'s and worked out by
/// hand in the comments.
/// </summary>
[Collection("Shared clocks and dice")]
public class TrainingPowersTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    /// <summary>A hall's map of the test's own, so a script that turns its people changes nobody else's.</summary>
    private static Map OwnHall(string name) => GameDataFiles.Load<MapFile>(System.IO.Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Inventory BagWith(params (string Item, int Count)[] items)
    {
        var bag = new Inventory();
        foreach (var (item, count) in items) bag.AddItem(ItemDatabase.Get(item)!, count);
        return bag;
    }

    // ================================================================ TR5 · Tera Shards and the Tera keeper

    [Fact]
    public void TheEighteenShardsAndTheStellarOneAreItemsOfTheData()
    {
        Assert.Equal(19, TeraChange.Types.Count);
        foreach (var type in TeraChange.Types)
        {
            var shard = ItemDatabase.Get(TeraChange.ShardOf(type));
            Assert.NotNull(shard);
            Assert.Equal(ItemPocket.Items, shard!.Pocket);
            Assert.True(shard.Price > 0);
        }
        Assert.Equal(9700, ItemDatabase.Get("Stellar Tera Shard")!.Id);
    }

    [Fact]
    public void FiftyShardsAreTakenAndTheTypeIsSet()
    {
        var bidoof = Mon("Bidoof", 30);
        var bag = BagWith(("Fire Tera Shard", 63));
        Assert.Equal(TeraType.Of(PokemonType.Normal), bidoof.TeraType);
        Assert.True(TeraChange.Change(bidoof, TeraType.Of(PokemonType.Fire), bag));
        Assert.Equal(TeraType.Of(PokemonType.Fire), bidoof.TeraType);
        Assert.Equal(13, TeraChange.Held(bag, TeraType.Of(PokemonType.Fire)));
        // Thirteen are too few for another change, and Fire is its type already
        Assert.Equal(TeraRefusal.Already, TeraChange.WhyNot(bidoof, TeraType.Of(PokemonType.Fire), bag));
        Assert.Equal(TeraRefusal.TooFewShards, TeraChange.WhyNot(bidoof, TeraType.Of(PokemonType.Water), bag));
        Assert.False(TeraChange.Change(bidoof, TeraType.Of(PokemonType.Water), bag));
        // The Stellar shards turn it Stellar
        bag.AddItem(ItemDatabase.Get("Stellar Tera Shard")!, 50);
        Assert.True(TeraChange.Change(bidoof, TeraType.Stellar, bag));
        Assert.True(bidoof.TeraType.IsStellar);
    }

    [Fact]
    public void OgerponAndTerapagosAndAnEggAreRefused()
    {
        var bag = BagWith(("Water Tera Shard", 99));
        Assert.Equal(TeraRefusal.Forced, TeraChange.WhyNot(Mon("Ogerpon", 70), TeraType.Of(PokemonType.Water), bag));
        Assert.Equal(TeraRefusal.Forced, TeraChange.WhyNotAny(Mon("Terapagos", 70), bag));
        var egg = Mon("Bidoof", 1);
        egg.IsEgg = true;
        Assert.Equal(TeraRefusal.Nobody, TeraChange.WhyNotAny(egg, bag));
        Assert.Equal(99, TeraChange.Held(bag, TeraType.Of(PokemonType.Water)));
    }

    [Fact]
    public void ASaveKeepsTheChangedType()
    {
        var bidoof = Mon("Bidoof", 30);
        TeraChange.Change(bidoof, TeraType.Of(PokemonType.Ghost), BagWith(("Ghost Tera Shard", 50)));
        var back = SavedPokemonData.FromPokemon(bidoof).ToPokemon(Ruleset.Modern);
        Assert.Equal(TeraType.Of(PokemonType.Ghost), back.TeraType);
    }

    [Fact]
    public void ABattleAfterwardsTerastallizesIntoTheNewType()
    {
        var bidoof = Mon("Bidoof", 50, "Tackle");
        TeraChange.Change(bidoof, TeraType.Of(PokemonType.Steel), BagWith(("Steel Tera Shard", 50)));
        var party = new Party();
        party.Add(bidoof);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Starly", 50, "Tackle") }, Random = Calm(), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", TeraOrb = true
        });
        core.Start();
        core.TakeLog();
        core.Submit(BattleChoice.Fight(Mine, 0) with { Terastallize = true }, BattleChoice.Fight(Foe, 0));
        Assert.Contains("Bidoof terastallized into the Steel type!", Lines(core.TakeLog()).Select(l => l.Replace("Terastallized", "terastallized")));
    }

    private static HeadlessScriptHost Keeper(Inventory bag, params Pokemon[] team)
    {
        var party = new Party();
        foreach (var p in team) party.Add(p);
        var host = new HeadlessScriptHost(new StoryState(), party, bag) { PokemonChoice = 0 };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("TeraKeeper", GameEngine.SunyshoreTrainingHall)!, OwnHall(GameEngine.SunyshoreTrainingHall).Everyone.Single(n => n.Script == "TeraKeeper"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheKeeperChangesATypeForATrainerWithAnOrbAndOnlySpeaksWithoutOne()
    {
        // Without the orb she tells what she does, and changes nothing
        var bidoof = Mon("Bidoof", 30);
        var none = Keeper(BagWith(("Fire Tera Shard", 50)), bidoof);
        Assert.Empty(none.Asked);
        Assert.Equal(TeraType.Of(PokemonType.Normal), bidoof.TeraType);

        // With it: yes, the Bidoof, and the first type open to it (Fire, the only one with fifty shards)
        var bag = BagWith(("Tera Orb", 1), ("Fire Tera Shard", 50), ("Water Tera Shard", 49));
        var host = Keeper(bag, bidoof);
        Assert.Contains(host.Asked, q => q.Question.Contains("Which Tera Type shall it take?"));
        Assert.Equal(TeraType.Of(PokemonType.Fire), bidoof.TeraType);
        Assert.Equal(0, TeraChange.Held(bag, TeraType.Of(PokemonType.Fire)));
        Assert.Contains(host.Transcript, t => t.Text.Contains("Fire type"));

        // Terapagos's is its own
        var forced = Keeper(BagWith(("Tera Orb", 1), ("Fire Tera Shard", 50)), Mon("Terapagos", 70));
        Assert.Contains(forced.Transcript, t => t.Text.Contains("part of what it is"));
    }

    [Fact]
    public void WithAnOrbEveryFifthGoldEarnsAShardOfTheTraineesFirstType()
    {
        var records = new TrainingRecords();
        for (int gold = 1; gold <= 10; gold++)
            records.Record(StatType.Attack, 100, DrillGrade.Gold, TeraChange.ShardOf(PokemonType.Normal));
        // The tenth gold also owes a Bottle Cap for a Champion; the two shards come after it, in the order earned
        var owed = records.Owed(hallOfFame: true);
        Assert.Equal(2, owed.Count(o => o == "Normal Tera Shard"));
        // Without the orb a gold earns none
        var plain = new TrainingRecords();
        for (int gold = 1; gold <= 10; gold++) plain.Record(StatType.Attack, 100, DrillGrade.Gold);
        Assert.DoesNotContain(plain.Owed(hallOfFame: true), o => o.EndsWith("Tera Shard"));
        // What is owed outlasts a save
        var back = new TrainingRecords();
        back.Load(System.Text.Json.JsonSerializer.Deserialize<TrainingSave>(System.Text.Json.JsonSerializer.Serialize(records.ToSave())));
        Assert.Equal(owed, back.Owed(hallOfFame: true));
    }

    [Fact]
    public void TheClubsClerkTradesShardsForBattlePointsOnlyWithAnOrb()
    {
        Assert.Equal(19, FrontierExchange.Stock(FrontierExchange.Tera).Count);
        Assert.All(FrontierExchange.Stock(FrontierExchange.Tera), p => Assert.EndsWith("Tera Shard", p.Item));
        var script = Scripts.Find("TrainingExchange", GameEngine.SunyshoreTrainingHall)!;
        Assert.Contains(script.Code, i => i.Op == Op.Frontier && i.Name == "prize" && i.Other == FrontierExchange.Tera);
        // Each region can have the shards by its clerk
        foreach (string region in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Unova })
            Assert.Contains(ItemSources.In(region).Of("Stellar Tera Shard"), s => s.Kind == ItemSourceKind.Prize);
    }

    public static readonly TheoryData<string, bool> Halls = new()
    {
        { GameEngine.SunyshoreTrainingHall, true },
        { "CeladonCity_DepartmentStore_1F", true },
        { "Goldenrod_DeptStore_1F", true },
        { "LilycoveCity_DepartmentStore_1F", true },
        { "ShoppingMallNine_1F", true },
        { "LaverreCity_TrainingHall", false }
    };

    [Theory]
    [MemberData(nameof(Halls))]
    public void EveryHallHasItsKeeperAndTheFiveTheirTeacherOnOpenFloor(string mapName, bool teacher)
    {
        var map = MapDatabase.Get(mapName);
        Assert.Equal(mapName, map.Name);
        var scripts = teacher ? new[] { "TeraKeeper", "TrainingTeacher" } : new[] { "TeraKeeper" };
        foreach (string script in scripts)
        {
            var who = map.Everyone.Single(n => n.Script == script);
            Assert.False(map.IsSolid(who.GridX, who.GridY), $"{script} of {mapName} stands in a wall");
            var (dx, dy) = who.Facing switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            Assert.True(map.IsWalkable(who.GridX + dx, who.GridY + dy), $"{script} of {mapName} can't be spoken to");
            Assert.DoesNotContain(map.Everyone, n => n != who && n.GridX == who.GridX && n.GridY == who.GridY);
        }
        Assert.Equal(teacher, map.Everyone.Any(n => n.Script == "TrainingTeacher"));
    }

    // ================================================================ TR7 · the rule and its readers

    private static PowerState Learned(params (PowerKind Kind, int Level)[] kinds)
    {
        var powers = new PowerState();
        foreach (var (kind, level) in kinds) powers.Learn(kind, level);
        return powers;
    }

    [Fact]
    public void EveryKindHasARowAndOneReader()
    {
        foreach (var kind in Enum.GetValues<PowerKind>())
        {
            Assert.NotNull(PowerTable.Row(kind));
            Assert.Contains(kind, PowerTable.Read);
        }
        // The old games' first numbers: EXP ×1.2, ×1.5, ×2 and prices 90%, 75%, 50%
        Assert.Equal(new[] { 20, 50, 100 }, new[] { 1, 2, 3 }.Select(l => PowerTable.Strength(PowerKind.Exp, l)));
        Assert.Equal(new[] { 10, 25, 50 }, new[] { 1, 2, 3 }.Select(l => PowerTable.Strength(PowerKind.Bargain, l)));
        Assert.Equal(new[] { PowerKind.Exp, PowerKind.Money, PowerKind.Encounter, PowerKind.Stealth }, PowerTable.TaughtIn(RegionDatabase.Sinnoh));
        Assert.Equal((0, 1, 2, 2, 3), (PowerTable.TeachingLevel(0), PowerTable.TeachingLevel(1), PowerTable.TeachingLevel(2), PowerTable.TeachingLevel(3), PowerTable.TeachingLevel(4)));
    }

    /// <summary>A wild Bidoof of level 10 knocked out at 1 HP by a Machamp of 50, under the modern rules, with the powers given.</summary>
    private static BattleCore KnockOut(Pokemon mine, PowerSet? powers)
    {
        var foe = Mon("Bidoof", 10);
        foe.CurrentHP = 1;
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = Ruleset.Modern,
            Conditions = new BattleConditions { Powers = powers }, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        core.TakeLog();
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        return core;
    }

    [Fact]
    public void TheExpPowerAddsItsShareAfterAffectionAndNothingWithout()
    {
        // The Bidoof gives a Machamp of 50 14 EXP (CatchExpTests); level 2 adds half: 14 + 7 = 21; level 3 doubles it
        int Gained(PowerSet? p) => KnockOut(Mon("Machamp", 50, "Strength"), p).Log.OfType<ExpGained>().Sum(e => e.Amount);
        Assert.Equal(14, Gained(null));
        Assert.Equal(14, Gained(new PowerSet()));
        Assert.Equal(21, Gained(new PowerSet { Exp = PowerTable.Strength(PowerKind.Exp, 2) }));
        Assert.Equal(28, Gained(new PowerSet { Exp = PowerTable.Strength(PowerKind.Exp, 3) }));
    }

    [Fact]
    public void TheEffortPowerAddsItsShareOfEachYield()
    {
        // A Bidoof yields one HP point; with the Effort Power's level 2 (+100) it is two, at level 3 (+200) three
        int Hp(PowerSet? p)
        {
            var machamp = Mon("Machamp", 50, "Strength");
            KnockOut(machamp, p);
            return machamp.EvHP;
        }
        Assert.Equal(1, Hp(null));
        Assert.Equal(2, Hp(new PowerSet { Effort = 100 }));
        Assert.Equal(3, Hp(new PowerSet { Effort = 200 }));
    }

    [Fact]
    public void ThePrizePowerAddsItsShareOfTheMoney()
    {
        BattleCore Won(PowerSet? powers)
        {
            var trainer = new Trainer { Id = "youngster", Name = "Joey", TrainerClass = "Youngster", PrizeMoney = 160 };
            var foe = Mon("Bidoof", 10);
            foe.CurrentHP = 1;
            trainer.Party.Add(foe);
            var party = new Party();
            party.Add(Mon("Machamp", 50, "Strength"));
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = Ruleset.Modern,
                Conditions = new BattleConditions { Powers = powers }, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
            });
            core.Start();
            return core;
        }
        int plain = Won(null).PrizeMoney;
        Assert.True(plain > 0);
        Assert.Equal(plain + plain / 2, Won(new PowerSet { Money = 50 }).PrizeMoney);
        Assert.Equal(plain * 3, Won(new PowerSet { Money = 200 }).PrizeMoney);
    }

    [Fact]
    public void TheCatchingPowerRaisesTheSpeciesRateToTwoFiftyFiveAtMost()
    {
        // Machamp is caught at a rate of 45: room for the power to raise it
        var wild = Mon("Machamp", 30);
        var ball = ItemDatabase.Get("Poké Ball")!;
        // A roll of shakes that holds under the raised rate and not under the plain one: over many seeds the raised one holds more
        int held = 0, heldWith = 0;
        for (int seed = 1; seed <= 2000; seed++)
        {
            if (CatchCalculator.Shakes(wild, ball, new Random(seed), 0, new BattleConditions()) == 4) held++;
            if (CatchCalculator.Shakes(wild, ball, new Random(seed), 0, new BattleConditions { Powers = new PowerSet { Catch = 50 } }) == 4) heldWith++;
        }
        Assert.True(heldWith > held, $"{held} plain, {heldWith} with the power");
        // Nothing without one: the same seeds, the same shakes
        for (int seed = 1; seed <= 50; seed++)
            Assert.Equal(CatchCalculator.Shakes(wild, ball, new Random(seed), 0, new BattleConditions()),
                CatchCalculator.Shakes(wild, ball, new Random(seed), 0, new BattleConditions { Powers = new PowerSet() }));
    }

    [Fact]
    public void TheBargainPowerTakesItsShareOffWhatACounterAsks()
    {
        Assert.Equal(200, ShopScreen.BargainOf(200, 0));
        Assert.Equal(180, ShopScreen.BargainOf(200, 10));
        Assert.Equal(150, ShopScreen.BargainOf(200, 25));
        Assert.Equal(100, ShopScreen.BargainOf(200, 50));
        Assert.Equal(1, ShopScreen.BargainOf(1, 50));
        var shop = new ShopScreen { Bargain = 25 };
        Assert.Equal(150, shop.PriceOf(ItemDatabase.Get("Poké Ball")!));
    }

    [Fact]
    public void ABattleKeepsItsPowersInItsFileAndAReplayGivesTheSameLog()
    {
        var party = new Party();
        party.Add(Mon("Machamp", 50, "Strength"));
        var foe = Mon("Bidoof", 10);
        foe.CurrentHP = 1;
        BattleSetup Setup(PowerSet? powers) => new()
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { foe },
            Rules = Ruleset.Modern, Random = new BattleRandom(36), Conditions = new BattleConditions { Powers = powers }
        };
        var battle = new BattleEngine(Setup(new PowerSet { Exp = 50, Effort = 100 }));
        Assert.NotNull(battle.Opening);
        Assert.Equal(50, battle.Opening!.Conditions.Powers!.Exp);
        var back = battle.Opening.ToSetup();
        Assert.Equal(100, back.Conditions.Powers!.Effort);
        // Without powers the file keeps none
        var plain = new BattleEngine(Setup(null));
        Assert.Null(plain.Opening!.Conditions.Powers);
    }

    [Fact]
    public void APowerCountsDownOnPlayTimeAndWearsOff()
    {
        var powers = new PowerState();
        Assert.True(powers.Grant(PowerKind.Exp, 2, PowerTable.Seconds));
        Assert.Empty(powers.Tick(599f));
        Assert.Equal(50, powers.Strength(PowerKind.Exp));
        Assert.Equal(new[] { PowerKind.Exp }, powers.Tick(1.5f));
        Assert.False(powers.Has(PowerKind.Exp));
        Assert.True(powers.ForBattle().IsEmpty);
    }

    [Fact]
    public void WalkingFillsTheChargeAPointEveryTwoHundredStepsToTen()
    {
        var powers = new PowerState();
        for (int step = 0; step < 199; step++) Assert.False(powers.Walk());
        Assert.True(powers.Walk());
        Assert.Equal(1, powers.Charge);
        for (int step = 0; step < 200 * 20; step++) powers.Walk();
        Assert.Equal(PowerTable.ChargeMost, powers.Charge);
    }

    [Fact]
    public void UsingAPowerPaysItsLevelAndIsRefusedForWhatIsntThere()
    {
        var powers = Learned((PowerKind.Exp, 2));
        Assert.Equal(PowerRefusal.NotLearned, powers.WhyNot(PowerKind.Money, 1));
        Assert.Equal(PowerRefusal.NotLearned, powers.WhyNot(PowerKind.Exp, 3));
        Assert.Equal(PowerRefusal.TooLittleCharge, powers.WhyNot(PowerKind.Exp, 1));
        powers.AddCharge(3);
        Assert.Equal(PowerRefusal.NoPowers, powers.WhyNot(PowerKind.Exp, 1, noPowers: true));
        Assert.False(powers.Use(PowerKind.Exp, 2, noPowers: true));
        Assert.True(powers.Use(PowerKind.Exp, 2));
        Assert.Equal(1, powers.Charge);
        Assert.Equal(2, powers.LevelOf(PowerKind.Exp));
        Assert.Equal(PowerTable.Seconds, powers.SecondsLeft(PowerKind.Exp));
        // Only plan 36's eight are learned; a lower level never takes the place of a higher one
        Assert.False(powers.Learn(PowerKind.Materials, 1));
        Assert.False(powers.Learn(PowerKind.Exp, 1));
    }

    [Fact]
    public void TodaysPowerIsTheSameAllDayIsTakenOnceAndDrawsNothing()
    {
        var powers = Learned((PowerKind.Exp, 3), (PowerKind.Money, 1), (PowerKind.Stealth, 2));
        const uint day = 0x1234ABCDu;
        var free = powers.FreeToday(day);
        Assert.NotNull(free);
        Assert.Equal(free, powers.FreeToday(day));
        Assert.InRange(free!.Value.Level, 1, Math.Min(2, powers.Learned(free.Value.Kind)));
        // Other days give the others too
        var kinds = Enumerable.Range(0, 60).Select(d => powers.FreeToday((uint)d * 7919u)!.Value.Kind).Distinct().ToList();
        Assert.Equal(3, kinds.Count);
        // Taken, it is in force for nothing, and gone until the day moves on; NO POWERS takes none
        Assert.False(powers.TakeFree(day, noPowers: true));
        Assert.True(powers.TakeFree(day));
        Assert.True(powers.Has(free.Value.Kind));
        Assert.Equal(0, powers.Charge);
        Assert.Null(powers.FreeToday(day));
        Assert.NotNull(powers.FreeToday(day + 1));
        // Nothing learned, nothing free
        Assert.Null(new PowerState().FreeToday(day));
    }

    [Fact]
    public void TheWatchsStateOutlastsASave()
    {
        var powers = Learned((PowerKind.Exp, 2), (PowerKind.Catch, 1));
        powers.AddCharge(4);
        for (int i = 0; i < 37; i++) powers.Walk();
        powers.TakeFree(99u);
        var json = System.Text.Json.JsonSerializer.Serialize(powers.ToSave());
        var back = new PowerState();
        back.Load(System.Text.Json.JsonSerializer.Deserialize<PowersSave>(json));
        Assert.Equal((2, 1, 4, 37), (back.Learned(PowerKind.Exp), back.Learned(PowerKind.Catch), back.Charge, back.Steps));
        Assert.Null(back.FreeToday(99u));
        Assert.Equal(powers.InForce, back.InForce);
        // A game with nothing of it keeps nothing
        Assert.Null(new PowerState().ToSave());
    }

    [Fact]
    public void NoPowersIsARunRowAndAScriptWord()
    {
        var panel = new PokemonPlatinumEngine.UI.Kit.RunPanel();
        Assert.Contains(PokemonPlatinumEngine.UI.Kit.RunRow.NoPowers, panel.Rows);
        Assert.Contains("nopowers", RunRules.RuleNames);
        Assert.True((RunRules.Normal with { NoPowers = true }).Has("nopowers"));
        Assert.False(RunRules.Normal.Has("nopowers"));
    }

    // ================================================================ TR8 · the watch, the panel and the teachers

    [Fact]
    public void ThePanelUsesTakesTodaysPowerAndSaysWhyNot()
    {
        var powers = Learned((PowerKind.Exp, 2), (PowerKind.Money, 1));
        var screen = new PowersScreen();
        screen.Open(powers, 5u, noPowers: false);
        // Today's power first, then the two learned
        Assert.True(screen.OnFree);
        Assert.Equal(3, screen.RowCount);
        var free = screen.Free!.Value;
        screen.Confirm();
        Assert.True(powers.Has(free.Kind));
        Assert.False(screen.Refused);
        Assert.Equal(2, screen.RowCount);

        // EXP at its highest level, with no charge: refused
        Assert.Equal(PowerKind.Exp, screen.KindAt(screen.Cursor));
        Assert.Equal(2, screen.LevelOf(PowerKind.Exp));
        screen.Confirm();
        Assert.True(screen.Refused);
        Assert.Equal(Text.Of("powerWatch.why.charge"), screen.Message);
        // Down a level with Left, charge enough: used
        powers.AddCharge(1);
        screen.Change(-1);
        Assert.Equal(1, screen.LevelOf(PowerKind.Exp));
        screen.Confirm();
        Assert.False(screen.Refused);
        Assert.Equal(1, powers.LevelOf(PowerKind.Exp));
        Assert.Equal(0, powers.Charge);

        // Under NO POWERS nothing is offered free and every use is refused
        screen.Open(powers, 6u, noPowers: true);
        Assert.Null(screen.Free);
        powers.AddCharge(5);
        screen.Confirm();
        Assert.Equal(Text.Of("powerWatch.why.run"), screen.Message);
    }

    [Fact]
    public void TheChipsStandClearOfTheSafariCountTheTimerAndTheAutosaveDisc()
    {
        const int sw = GameEngine.VirtualWidth, sh = GameEngine.VirtualHeight;
        var safari = new Raylib_cs.Rectangle(sw - 32 - 300, 32, 300, 96);
        var timer = new Raylib_cs.Rectangle(sw - 32 - ModernUi.TimerCornerWidth, ModernUi.TimerCornerTop, ModernUi.TimerCornerWidth, ModernUi.TimerCornerLow + ModernUi.TimerCornerHeight);
        var disc = new Raylib_cs.Rectangle(ModernUi.AutosaveDiscMargin - ModernUi.AutosaveDiscRadius - 4, sh - ModernUi.AutosaveDiscMargin - ModernUi.AutosaveDiscRadius - 4,
            ModernUi.AutosaveDiscRadius * 2 + 8, ModernUi.AutosaveDiscRadius * 2 + 8);
        static bool Overlap(Raylib_cs.Rectangle a, Raylib_cs.Rectangle b) =>
            a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
        // Every kind in force at once
        for (int i = 0; i < Enum.GetValues<PowerKind>().Length; i++)
        {
            var chip = ModernUi.PowerChipRect(i, new ActivePower(PowerKind.Exp, 1, 600));
            Assert.False(Overlap(chip, safari) || Overlap(chip, timer) || Overlap(chip, disc), $"chip {i} overlaps another corner");
            Assert.True(chip.Y + chip.Height < sh - 200, $"chip {i} reaches the text box's height");
        }
    }

    private static HeadlessScriptHost Teacher(StoryState story, PowerState powers, Inventory? bag = null, string place = GameEngine.SunyshoreTrainingHall)
    {
        var host = new HeadlessScriptHost(story, new Party(), bag ?? new Inventory()) { Powers = powers };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("TrainingTeacher", place)!, OwnHall(place).Everyone.Single(n => n.Script == "TrainingTeacher"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheTeacherTeachesHisRegionsPowersByTheHallsOfFameAndGivesTheWatchOnce()
    {
        // Before any Hall of Fame he only talks
        var powers = new PowerState();
        var bag = new Inventory();
        Teacher(new StoryState(), powers, bag);
        Assert.Empty(powers.LearnedKinds);

        // With Sinnoh's: its four at level 1, and the watch
        var story = new StoryState();
        story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        var first = Teacher(story, powers, bag);
        Assert.Equal(PowerTable.TaughtIn(RegionDatabase.Sinnoh), powers.LearnedKinds);
        Assert.All(powers.LearnedKinds, k => Assert.Equal(1, powers.Learned(k)));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(GameEngine.PowerWatchItem)!));
        Assert.Contains(first.Transcript, t => t.Text.Contains("EXP Power"));

        // Again: nothing more, nor a second watch
        var again = Teacher(story, powers, bag);
        Assert.Contains(again.Transcript, t => t.Text.Contains("You know all I can teach you"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(GameEngine.PowerWatchItem)!));

        // A second Hall of Fame raises them to level 2
        story.Set(RegionDatabase.Get(RegionDatabase.Kanto)!.StoryCompleteFlag);
        Teacher(story, powers, bag);
        Assert.All(powers.LearnedKinds, k => Assert.Equal(2, powers.Learned(k)));
    }

    [Fact]
    public void TheWatchIsAKeyItemUsedInTheFieldAndRegistered()
    {
        var watch = ItemDatabase.Get(GameEngine.PowerWatchItem)!;
        Assert.Equal(ItemPocket.KeyItems, watch.Pocket);
        Assert.Equal(9710, watch.Id);
        Assert.True(watch.CanBeRegistered);
        Assert.True(BagScreen.UsedInField(watch));
        Assert.Contains(ItemSources.In(RegionDatabase.Sinnoh).Of(GameEngine.PowerWatchItem), s => s.Kind == ItemSourceKind.Given);
    }
}
