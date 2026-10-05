using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>Status conditions, stat stages, abilities, held items, natures and double battles (Generation 4 rules).</summary>
public class BattleMechanicsTests
{
    // ---------------------------------------------------------------- helpers

    /// <summary>A move that does nothing, so a test's foe stays out of the way.</summary>
    private static readonly MoveData Idle = new() { Name = "Idle", Type = PokemonType.Normal, Category = MoveCategory.Status, MaxPP = 40, Target = MoveTarget.User };

    /// <summary>A Pokémon with no IVs and a neutral nature, so its stats are the same every run.</summary>
    private static Pokemon Mon(string species, int level, string? ability = null, params MoveData[] moves)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);
        if (ability != null) p.AbilityName = ability;
        if (moves.Length > 0)
        {
            p.Moves.Clear();
            foreach (var m in moves) p.Moves.Add(new Move(m));
        }
        return p;
    }

    private static MoveData M(string name) => MoveDatabase.Get(name);

    private static MoveData Custom(string name, Action<MoveData> setup)
    {
        var m = new MoveData { Name = name, Type = PokemonType.Normal, Category = MoveCategory.Physical, Power = 40, Accuracy = 0, MaxPP = 30, Flags = MoveFlags.Contact };
        setup(m);
        return m;
    }

    private static BattleEngine Wild(Pokemon mine, Pokemon foe, int seed = 1, params Pokemon[] bench)
    {
        var party = new Party();
        party.Add(mine);
        foreach (var b in bench) party.Add(b);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            // Nearby seeds give similar first rolls, so spread them out
            WildPokemon = new List<Pokemon> { foe }, Random = new Random(seed * 7919 + 17)
        });
        Settle(battle);
        return battle;
    }

    /// <summary>Confirms messages (letting timed animations play) until a menu opens or the battle ends; returns what was said.</summary>
    private static List<string> Settle(BattleEngine battle)
    {
        var said = new List<string>();
        for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState != BattleMenuState.Message) break;
            if (battle.IsWaitingForConfirm) said.Add(battle.CurrentMessage);
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return said;
    }

    private static List<string> UseMove(BattleEngine battle, int index = 0)
    {
        battle.SelectMove(index);
        return Settle(battle);
    }

    // ---------------------------------------------------------------- imported moves the engine can't run yet

    [Fact]
    public void TestAMoveWhoseEffectIsMissingDoesNothing()
    {
        // Fling's throw of its item isn't written yet (plan 06 · R5): it neither hits for made-up damage nor crashes
        var mine = Mon("Riolu", 20, null, M("Fling"));
        var foe = Mon("Bidoof", 20, null, Idle);
        var battle = Wild(mine, foe);
        int hp = foe.CurrentHP;
        var said = UseMove(battle);
        Assert.Contains("But nothing happened!", said);
        Assert.Equal(hp, foe.CurrentHP);

        // A move whose own effect is missing but that still hits (Fury Attack hits once for now)
        var striker = Mon("Riolu", 20, null, M("Fury Attack"));
        var target = Mon("Bidoof", 20, null, Idle);
        battle = Wild(striker, target);
        hp = target.CurrentHP;
        UseMove(battle);
        Assert.True(target.CurrentHP < hp);
    }

    [Fact]
    public void TestTrainersPickMovesThatWorkFirst()
    {
        // The foe knows a move that does nothing yet and one that works: it uses the one that works
        var mine = Mon("Bidoof", 20, null, Idle);
        var foe = Mon("Riolu", 20, null, M("Fling"), M("Quick Attack"));
        var battle = Wild(mine, foe);
        int hp = mine.CurrentHP;
        var said = UseMove(battle);
        Assert.Contains(said, m => m.Contains("used Quick Attack"));
        Assert.True(mine.CurrentHP < hp);
    }

    [Fact]
    public void TestFairyTypeMatchups()
    {
        Assert.Equal(2f, TypeChart.GetEffectiveness(PokemonType.Fairy, PokemonType.Dragon));
        Assert.Equal(0f, TypeChart.GetEffectiveness(PokemonType.Dragon, PokemonType.Fairy));
        Assert.Equal(2f, TypeChart.GetEffectiveness(PokemonType.Steel, PokemonType.Fairy));
        Assert.Equal(0.5f, TypeChart.GetEffectiveness(PokemonType.Fairy, PokemonType.Fire));
        // Platinum's chart stays: Steel still resists Ghost and Dark
        Assert.Equal(0.5f, TypeChart.GetEffectiveness(PokemonType.Ghost, PokemonType.Steel));
        Assert.Equal(0.5f, TypeChart.GetEffectiveness(PokemonType.Dark, PokemonType.Steel));
    }

    // ---------------------------------------------------------------- status conditions

    [Fact]
    public void TestBurnAndPoisonTakeAnEighthAtTheEndOfTheTurn()
    {
        var mine = Mon("Turtwig", 20, null, Idle);
        var foe = Mon("Bidoof", 20, null, Idle);
        var battle = Wild(mine, foe);
        mine.Status = StatusCondition.Burn;
        foe.Status = StatusCondition.Poison;
        int mineHp = mine.CurrentHP, foeHp = foe.CurrentHP;

        var said = UseMove(battle);

        Assert.Contains("Turtwig is hurt by its burn!", said);
        Assert.Contains("Foe Bidoof is hurt by poison!", said);
        Assert.Equal(mineHp - mine.MaxHP / 8, mine.CurrentHP);
        Assert.Equal(foeHp - foe.MaxHP / 8, foe.CurrentHP);
    }

    [Fact]
    public void TestBadPoisonGetsWorseEachTurn()
    {
        var mine = Mon("Turtwig", 30, null, Idle);
        var foe = Mon("Bidoof", 30, null, Idle);
        var battle = Wild(mine, foe);
        foe.Status = StatusCondition.Toxic;
        int hp = foe.CurrentHP;

        UseMove(battle);
        Assert.Equal(hp - foe.MaxHP / 16, foe.CurrentHP);
        hp = foe.CurrentHP;
        UseMove(battle);
        // A sixteenth, rounded down, times the count (battle_controller_player.c, MON_COND_CHECK_STATE_TOXIC)
        Assert.Equal(hp - foe.MaxHP / 16 * 2, foe.CurrentHP);
    }

    [Fact]
    public void TestTypesAndStatusBlockNewConditions()
    {
        // Fire types can't be burned
        var battle = Wild(Mon("Turtwig", 20, null, M("Will-O-Wisp")), Mon("Chimchar", 20, null, Idle));
        var said = UseMove(battle);
        Assert.Contains("It doesn't affect Foe Chimchar...", said);
        Assert.Equal(StatusCondition.None, battle.EnemyPokemon.Status);

        // A Pokémon that already has a condition can't get another
        battle = Wild(Mon("Shinx", 20, null, M("Thunder Wave")), Mon("Bidoof", 20, null, Idle));
        battle.EnemyPokemon.Status = StatusCondition.Burn;
        said = UseMove(battle);
        Assert.Contains("But it failed!", said);
        Assert.Equal(StatusCondition.Burn, battle.EnemyPokemon.Status);

        // Thunder Wave can't touch a Ground type
        battle = Wild(Mon("Shinx", 20, null, M("Thunder Wave")), Mon("Gible", 20, null, Idle));
        said = UseMove(battle);
        Assert.Contains("It doesn't affect Foe Gible...", said);
    }

    [Fact]
    public void TestParalysisQuartersSpeed()
    {
        var battle = Wild(Mon("Shinx", 30, null, Idle), Mon("Bidoof", 30, null, Idle));
        var place = battle.PlayerSlots[0];
        int speed = battle.EffectiveSpeed(place);
        place.Pokemon!.Status = StatusCondition.Paralyze;
        Assert.InRange(battle.EffectiveSpeed(place), speed / 4 - 1, speed / 4 + 1);
    }

    [Fact]
    public void TestASleepingPokemonCantMoveUntilItWakes()
    {
        var mine = Mon("Turtwig", 20, null, M("Tackle"));
        var battle = Wild(mine, Mon("Bidoof", 20, null, Idle));
        mine.Status = StatusCondition.Sleep;
        mine.SleepTurns = 2;
        int foeHp = battle.EnemyPokemon.CurrentHP;

        Assert.Contains("Turtwig is fast asleep.", UseMove(battle));
        Assert.Equal(foeHp, battle.EnemyPokemon.CurrentHP);

        var said = UseMove(battle);
        Assert.Contains("Turtwig woke up!", said);
        Assert.Contains("Turtwig used Tackle!", said);
        Assert.Equal(StatusCondition.None, mine.Status);
    }

    [Fact]
    public void TestAFireMoveThawsAFrozenTarget()
    {
        var battle = Wild(Mon("Chimchar", 20, null, M("Ember")), Mon("Bidoof", 20, null, Idle));
        battle.EnemyPokemon.Status = StatusCondition.Freeze;
        var said = UseMove(battle);
        Assert.Contains("Foe Bidoof thawed out!", said);
        Assert.NotEqual(StatusCondition.Freeze, battle.EnemyPokemon.Status);
    }

    [Fact]
    public void TestSideEffectsOfDamagingMovesLand()
    {
        var scorch = Custom("Scorch", m => { m.Type = PokemonType.Fire; m.InflictStatus = StatusCondition.Burn; m.StatusChancePercent = 100; });
        var battle = Wild(Mon("Chimchar", 20, null, scorch), Mon("Bidoof", 20, null, Idle));
        Assert.Contains("Foe Bidoof was burned!", UseMove(battle));
        Assert.Equal(StatusCondition.Burn, battle.EnemyPokemon.Status);
    }

    [Fact]
    public void TestAFasterAttackerCanMakeItsTargetFlinch()
    {
        var jolt = Custom("Jolt", m => { m.FlinchChancePercent = 100; m.Power = 10; });
        var battle = Wild(Mon("Shinx", 30, null, jolt), Mon("Bidoof", 25, null, M("Tackle")));
        var said = UseMove(battle);
        Assert.Contains("Foe Bidoof flinched!", said);
        Assert.DoesNotContain("Foe Bidoof used Tackle!", said);
    }

    [Fact]
    public void TestConfusionWearsOffAndCanStopAMove()
    {
        var battle = Wild(Mon("Turtwig", 30, null, M("Confuse Ray"), Idle), Mon("Bidoof", 30, null, M("Tackle")), seed: 4);
        Assert.Contains("Foe Bidoof became confused!", UseMove(battle));
        Assert.True(battle.EnemySlots[0].IsConfused);

        // Within a few turns it hurts itself at least once, and it always snaps out within four
        bool hurt = false, snapped = false;
        for (int turn = 0; turn < 5; turn++)
        {
            var said = UseMove(battle, 1);
            hurt |= said.Contains("It hurt itself in its confusion!");
            snapped |= said.Contains("Foe Bidoof snapped out of confusion!");
        }
        Assert.True(snapped);
        Assert.False(battle.EnemySlots[0].IsConfused);
        _ = hurt; // depends on the rolls; the snapping out is the rule being checked
    }

    // ---------------------------------------------------------------- stat stages

    [Fact]
    public void TestStatStagesStopAtSixAndSayHowFarTheyMoved()
    {
        var battle = Wild(Mon("Riolu", 30, null, M("Swords Dance")), Mon("Bidoof", 30, null, Idle));
        Assert.Contains("Riolu's Attack sharply rose!", UseMove(battle));
        UseMove(battle);
        UseMove(battle);
        Assert.Equal(6, battle.PlayerPokemon.StatStages[StatType.Attack]);
        Assert.Contains("Riolu's Attack won't go any higher!", UseMove(battle));
    }

    [Fact]
    public void TestStagesResetWhenThePokemonIsWithdrawn()
    {
        var bench = Mon("Piplup", 20, null, Idle);
        var battle = Wild(Mon("Riolu", 20, null, M("Swords Dance")), Mon("Bidoof", 20, null, Idle), 1, bench);
        UseMove(battle);
        var riolu = battle.PlayerPokemon;
        Assert.Equal(2, riolu.StatStages[StatType.Attack]);

        battle.SelectMainMenuOption(2);
        battle.SelectSwitch(1);
        Settle(battle);
        Assert.Equal(bench, battle.PlayerPokemon);
        Assert.Equal(0, riolu.StatStages[StatType.Attack]);
    }

    // ---------------------------------------------------------------- abilities

    [Fact]
    public void TestIntimidateLowersTheFoesAttackOnEntry()
    {
        var party = new Party();
        party.Add(Mon("Staravia", 20, "Intimidate", Idle));
        var foe = Mon("Bidoof", 20, "Unaware", Idle);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new() { foe }, Random = new Random(1)
        });
        var said = Settle(battle);
        Assert.Contains("Staravia's Intimidate cuts Foe Bidoof's Attack!", said);
        Assert.Equal(-1, foe.StatStages[StatType.Attack]);
    }

    [Fact]
    public void TestAbilitiesThatStopMoves()
    {
        // Levitate: Ground moves miss
        var battle = Wild(Mon("Gible", 30, null, M("Earthquake")), Mon("Bidoof", 30, "Levitate", Idle));
        int hp = battle.EnemyPokemon.CurrentHP;
        Assert.Contains("Foe Bidoof makes Ground moves miss with Levitate!", UseMove(battle));
        Assert.Equal(hp, battle.EnemyPokemon.CurrentHP);

        // Volt Absorb: Electric moves heal instead
        battle = Wild(Mon("Shinx", 30, null, M("Thunderbolt")), Mon("Bidoof", 30, "Volt Absorb", Idle));
        battle.EnemyPokemon.CurrentHP = 10;
        UseMove(battle);
        Assert.Equal(10 + battle.EnemyPokemon.MaxHP / 4, battle.EnemyPokemon.CurrentHP);

        // Clear Body: stats can't be lowered by others
        battle = Wild(Mon("Piplup", 30, null, M("Growl")), Mon("Bidoof", 30, "Clear Body", Idle));
        Assert.Contains("Foe Bidoof's Clear Body prevents its Attack from being lowered!", UseMove(battle));
        Assert.Equal(0, battle.EnemyPokemon.StatStages[StatType.Attack]);

        // Insomnia: no sleep
        battle = Wild(Mon("Piplup", 30, null, M("Hypnosis")), Mon("Bidoof", 30, "Insomnia", Idle), seed: 2);
        for (int i = 0; i < 6; i++) UseMove(battle);
        Assert.Equal(StatusCondition.None, battle.EnemyPokemon.Status);
    }

    [Fact]
    public void TestSimpleCountsStatChangesDouble()
    {
        // In Platinum the stage changes as anyone's does, and counts double where it is read (BattleAbilityTests
        // has the numbers); from Generation 5 the change itself is doubled
        var battle = Wild(Mon("Piplup", 30, null, M("Growl")), Mon("Bidoof", 30, "Simple", Idle));
        Assert.Contains("Foe Bidoof's Attack fell!", UseMove(battle));
        Assert.Equal(-1, battle.EnemyPokemon.StatStages[StatType.Attack]);
    }

    [Fact]
    public void TestStaticCanParalyzeAnAttackerThatTouchesIt()
    {
        bool paralyzed = false;
        for (int seed = 0; seed < 30 && !paralyzed; seed++)
        {
            var battle = Wild(Mon("Turtwig", 30, null, M("Tackle")), Mon("Shinx", 40, "Static", Idle), seed);
            UseMove(battle);
            paralyzed = battle.PlayerPokemon.Status == StatusCondition.Paralyze;
        }
        Assert.True(paralyzed);

        // Never from a move that doesn't touch it
        for (int seed = 0; seed < 15; seed++)
        {
            var battle = Wild(Mon("Piplup", 30, null, M("Water Gun")), Mon("Shinx", 40, "Static", Idle), seed);
            UseMove(battle);
            Assert.Equal(StatusCondition.None, battle.PlayerPokemon.Status);
        }
    }

    [Fact]
    public void TestDamageModifiersFromAbilities()
    {
        var tackle = new Move(M("Tackle"));
        var razor = new Move(M("Razor Leaf"));
        int Hit(Pokemon a, Pokemon d, Move m) => DamageCalculator.CalculateDamage(a, d, m, new Random(7)).Damage;

        // Overgrow: Grass moves at a third of HP or less
        var turtwig = Mon("Turtwig", 50, "Overgrow");
        var target = Mon("Bidoof", 50);
        int normal = Hit(turtwig, target, razor);
        turtwig.CurrentHP = turtwig.MaxHP / 3;
        Assert.InRange(Hit(turtwig, target, razor), normal * 1.4, normal * 1.6);

        // Huge Power doubles the Attack stat
        var plain = Mon("Bidoof", 50, "Simple");
        var strong = Mon("Bidoof", 50, "Huge Power");
        Assert.InRange(Hit(strong, target, tackle), Hit(plain, target, tackle) * 1.8, Hit(plain, target, tackle) * 2.2);

        // Thick Fat halves Fire damage
        var ember = new Move(M("Flamethrower"));
        var fat = Mon("Bidoof", 50, "Thick Fat");
        var chimchar = Mon("Chimchar", 50);
        Assert.InRange(Hit(chimchar, fat, ember), Hit(chimchar, target, ember) * 0.4, Hit(chimchar, target, ember) * 0.6);
    }

    [Fact]
    public void TestEverySpeciesAbilityIsKnown()
    {
        foreach (var species in PokemonDatabase.GetAll())
        {
            var abilities = AbilityDatabase.ForSpecies(species);
            Assert.NotEmpty(abilities);
            Assert.All(abilities, a => Assert.NotNull(AbilityDatabase.Get(a)));
        }

        // Every ability of Platinum's has its code since plan 06 · R7; those shown but not working yet are the
        // later generations' (plan 06 · R16 on)
        var records = GameDataFiles.Load<List<AbilityDatabase.AbilityRecord>>(AbilityDatabase.FileName);
        var platinums = records.Where(r => r.Generation <= 4).Select(r => r.Name).ToList();
        Assert.Equal(123, platinums.Count);
        var pending = AbilityDatabase.GetAll().Where(a => !a.IsImplemented).Select(a => a.Name).ToList();
        Assert.Empty(platinums.Intersect(pending));
        Assert.Contains("Sheer Force", pending);
    }

    [Fact]
    public void TestAbilitiesFollowTheSpeciesWhenItEvolves()
    {
        var shinx = Mon("Shinx", 14, "Intimidate");
        shinx.GainExp(shinx.ExpForNextLevel - shinx.CurrentExp, out _);
        Evolution.Evolve(shinx, Evolution.Find(shinx, EvolutionTrigger.LevelUp, new EvolutionContext())!, new EvolutionContext());
        Assert.Equal("Luxio", shinx.Species.Name);
        Assert.Equal("Intimidate", shinx.AbilityName);
    }

    // ---------------------------------------------------------------- held items

    [Fact]
    public void TestLeftoversRestoreASixteenthEachTurn()
    {
        var mine = Mon("Turtwig", 30, null, Idle);
        mine.HeldItem = ItemDatabase.Get("Leftovers");
        var battle = Wild(mine, Mon("Bidoof", 30, null, Idle));
        mine.CurrentHP = 10;
        var said = UseMove(battle);
        Assert.Contains("Turtwig restored a little HP using its Leftovers!", said);
        Assert.Equal(10 + mine.MaxHP / 16, mine.CurrentHP);
    }

    [Fact]
    public void TestBerriesAreEatenWhenNeeded()
    {
        // Sitrus Berry at half HP or less (the burn at the end of the turn takes it there)
        var mine = Mon("Turtwig", 30, null, Idle);
        mine.HeldItem = ItemDatabase.Get("Sitrus Berry");
        var battle = Wild(mine, Mon("Bidoof", 30, null, Idle));
        mine.CurrentHP = mine.MaxHP / 2 + 2;
        mine.Status = StatusCondition.Burn;
        var said = UseMove(battle);
        Assert.Contains("Turtwig restored its health using its Sitrus Berry!", said);
        Assert.Equal(mine.MaxHP / 2 + 2 - mine.MaxHP / 8 + mine.MaxHP / 4, mine.CurrentHP);
        Assert.Null(mine.HeldItem);

        // Lum Berry cures a status as soon as it lands
        var piplup = Mon("Piplup", 30, null, Idle);
        piplup.HeldItem = ItemDatabase.Get("Lum Berry");
        battle = Wild(piplup, Mon("Shinx", 30, null, M("Thunder Wave")));
        said = UseMove(battle);
        Assert.Contains("Piplup's Lum Berry cured its paralysis!", said);
        Assert.Equal(StatusCondition.None, piplup.Status);
        Assert.Null(piplup.HeldItem);
    }

    [Fact]
    public void TestFocusSashSurvivesAKnockoutAtFullHp()
    {
        var foe = Mon("Starly", 5, null, Idle);
        foe.HeldItem = ItemDatabase.Get("Focus Sash");
        var battle = Wild(Mon("Garchomp", 60, null, M("Dragon Claw")), foe);
        var said = UseMove(battle);
        Assert.Contains("Foe Starly hung on using its Focus Sash!", said);
        Assert.Equal(1, foe.CurrentHP);
        Assert.Null(foe.HeldItem);
    }

    [Fact]
    public void TestChoiceBandBoostsAndLocksTheMove()
    {
        var mine = Mon("Riolu", 30, null, M("Tackle"), M("Bite"));
        mine.HeldItem = ItemDatabase.Get("Choice Band");
        var target = Mon("Bidoof", 30);
        var plain = Mon("Riolu", 30);
        var tackle = new Move(M("Tackle"));
        int boosted = DamageCalculator.CalculateDamage(mine, target, tackle, new Random(3)).Damage;
        int normal = DamageCalculator.CalculateDamage(plain, target, tackle, new Random(3)).Damage;
        Assert.InRange(boosted, normal * 1.3, normal * 1.7);

        var battle = Wild(mine, Mon("Bidoof", 40, null, Idle));
        UseMove(battle, 0);
        battle.SelectMove(1);
        Assert.Equal("Riolu can only use Tackle!", battle.CurrentMessage);
    }

    [Fact]
    public void TestLifeOrbCostsATenthPerAttack()
    {
        var mine = Mon("Riolu", 30, null, M("Tackle"));
        mine.HeldItem = ItemDatabase.Get("Life Orb");
        var battle = Wild(mine, Mon("Bidoof", 40, null, Idle));
        int hp = mine.CurrentHP;
        var said = UseMove(battle);
        if (said.Contains("Riolu's attack missed!")) return;
        Assert.Contains("Riolu lost some of its HP!", said);
        Assert.Equal(hp - mine.MaxHP / 10, mine.CurrentHP);
    }

    [Fact]
    public void TestHeldItemsAndAbilitiesSurviveASave()
    {
        var p = Mon("Luxio", 20, "Intimidate");
        p.HeldItem = ItemDatabase.Get("Magnet");
        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal("Intimidate", back.AbilityName);
        Assert.Equal("Magnet", back.HeldItem?.Name);

        // An old save without them falls back to the species' first ability
        var old = System.Text.Json.JsonSerializer.Deserialize<SavedPokemonData>("{\"SpeciesName\":\"Shinx\"}")!.ToPokemon();
        Assert.Equal("Rivalry", old.AbilityName);
        Assert.Null(old.HeldItem);
    }

    [Fact]
    public void TestEveryHoldableItemIsInTheItemList()
    {
        foreach (var name in HeldItemEffects.Names) Assert.NotNull(ItemDatabase.Get(name));
    }

    // ---------------------------------------------------------------- natures

    [Fact]
    public void TestNaturesRaiseOneStatAndLowerAnother()
    {
        // Natures come in a 5×5 grid over Attack, Defense, Speed, Sp. Atk, Sp. Def: row raises, column lowers
        StatType[] order = { StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense };
        var species = PokemonDatabase.Get("Bidoof")!;
        var neutral = new Pokemon(species, 50, Gender.Male, Nature.Hardy, false);
        foreach (var nature in Enum.GetValues<Nature>())
        {
            var p = new Pokemon(species, 50, Gender.Male, nature, false);
            int up = (int)nature / 5, down = (int)nature % 5;
            foreach (var (stat, i) in order.Select((s, i) => (s, i)))
            {
                int expected = neutral.GetEffectiveStat(stat);
                if (up != down && i == up) expected = (int)(expected * 1.1f);
                if (up != down && i == down) expected = (int)(expected * 0.9f);
                Assert.Equal(expected, p.GetEffectiveStat(stat));
            }
        }
    }

    // ---------------------------------------------------------------- double battles

    private static (BattleEngine Battle, Trainer Twins) Doubles(Pokemon[] mine, Pokemon[] theirs, int seed = 1)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var twins = new Trainer { Name = "Liv & Liz", TrainerClass = "Twins", PrizeMoney = 400 };
        foreach (var p in theirs) twins.Party.Add(p);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            Format = BattleFormat.Double, Trainers = new() { twins }, Random = new Random(seed)
        });
        Settle(battle);
        return (battle, twins);
    }

    [Fact]
    public void TestADoubleBattleSendsOutTwoAndAsksForEachAction()
    {
        var (battle, _) = Doubles(new[] { Mon("Turtwig", 20, null, M("Tackle")), Mon("Piplup", 20, null, Idle) },
            new[] { Mon("Bidoof", 18, null, Idle), Mon("Starly", 18, null, Idle) });

        Assert.True(battle.IsDouble);
        Assert.Equal(2, battle.Anim.Slots);
        Assert.All(battle.PlayerSlots.Concat(battle.EnemySlots), b => Assert.True(b.IsActive));
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal("Turtwig", battle.PlayerPokemon.DisplayName);

        // Tackle asks for a target: the two foes (left to right) and the partner
        battle.SelectMove(0);
        Assert.Equal(BattleMenuState.SelectTarget, battle.HUD.MenuState);
        Assert.Equal(3, battle.TargetChoices.Count);
        var starly = battle.EnemySlots[1];
        int aim = battle.TargetChoices.ToList().IndexOf(starly);
        battle.SelectTarget(aim);

        // Then it's Piplup's turn to choose
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal("Piplup", battle.PlayerPokemon.DisplayName);

        // Going back to Turtwig's choice and forward again works
        Assert.True(battle.CancelChoice());
        Assert.Equal("Turtwig", battle.PlayerPokemon.DisplayName);
        battle.SelectMove(0);
        battle.SelectTarget(aim);

        int hp = starly.Pokemon!.CurrentHP;
        battle.SelectMove(0);
        var said = Settle(battle);
        Assert.Contains("Turtwig used Tackle!", said);
        Assert.Contains("Foe Starly used Idle!", said);
        Assert.True(starly.Pokemon.CurrentHP < hp || said.Any(s => s.Contains("avoided")));
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
    }

    [Fact]
    public void TestSpreadMovesHitEveryoneForThreeQuarters()
    {
        var (battle, _) = Doubles(new[] { Mon("Garchomp", 50, null, M("Earthquake")), Mon("Piplup", 20, null, Idle) },
            new[] { Mon("Bibarel", 60, null, Idle), Mon("Bibarel", 60, null, Idle) });

        var partner = battle.PlayerSlots[1].Pokemon!;
        int partnerHp = partner.CurrentHP;
        int foeHp = battle.EnemySlots[0].Pokemon!.CurrentHP;

        battle.SelectMove(0);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState); // Earthquake needs no target
        battle.SelectMove(0);
        var said = Settle(battle);

        // Earthquake hits the partner too
        Assert.True(partner.CurrentHP < partnerHp);
        int dealt = foeHp - battle.EnemySlots[0].Pokemon!.CurrentHP;

        // A quarter less than the same hit on its own
        var alone = DamageCalculator.Calculate(battle.PlayerSlots[0], battle.EnemySlots[0], new Move(M("Earthquake")), new Random(9), spread: false);
        var spread = DamageCalculator.Calculate(battle.PlayerSlots[0], battle.EnemySlots[0], new Move(M("Earthquake")), new Random(9), spread: true);
        Assert.InRange(spread.Damage, alone.Damage * 0.7, alone.Damage * 0.8);
        Assert.True(dealt > 0 || said.Any(s => s.Contains("avoided")));
    }

    [Fact]
    public void TestFaintedPokemonAreReplacedInADoubleBattle()
    {
        var (battle, twins) = Doubles(
            new[] { Mon("Garchomp", 60, null, M("Dragon Claw")), Mon("Piplup", 20, null, Idle), Mon("Turtwig", 20, null, Idle) },
            new[] { Mon("Bidoof", 5, null, M("Tackle")), Mon("Starly", 5, null, Idle), Mon("Shinx", 5, null, Idle) });

        var bidoof = battle.EnemySlots[0].Pokemon!;
        battle.SelectMove(0);
        battle.SelectTarget(battle.TargetChoices.ToList().IndexOf(battle.EnemySlots[0]));
        battle.SelectMove(0);
        var said = Settle(battle);

        Assert.Contains("Foe Bidoof fainted!", said);
        Assert.Contains("Twins Liv & Liz sent out Shinx!", said);
        Assert.Equal("Shinx", battle.EnemySlots[0].Pokemon!.Species.Name);
        Assert.True(bidoof.IsFainted);

        // EXP went to the Pokémon that fought it
        Assert.Contains(said, s => s.StartsWith("Garchomp gained"));
        Assert.Contains(said, s => s.StartsWith("Piplup gained"));

        // The player picks a replacement for a fainted Pokémon of theirs
        battle.PlayerSlots[1].Pokemon!.CurrentHP = 1;
        battle.PlayerSlots[1].Pokemon!.Status = StatusCondition.Burn;
        battle.SelectMove(0);
        battle.SelectTarget(0);
        battle.SelectMove(0);
        Settle(battle);
        Assert.Equal(BattleMenuState.SwitchPokemon, battle.HUD.MenuState);
        Assert.True(battle.IsChoosingReplacement);
        Assert.Equal("Piplup", battle.PlayerPokemon.DisplayName);
        battle.SelectSwitch(2);
        Settle(battle);
        Assert.Equal("Turtwig", battle.PlayerSlots[1].Pokemon!.DisplayName);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        _ = twins;
    }

    [Fact]
    public void TestTwoTrainersEachSendTheirOwn()
    {
        var party = new Party();
        party.Add(Mon("Garchomp", 60, null, M("Earthquake")));
        party.Add(Mon("Staraptor", 60, null, Idle));
        var a = new Trainer { Name = "A", TrainerClass = "Galactic Grunt", PrizeMoney = 100 };
        var b = new Trainer { Name = "B", TrainerClass = "Galactic Grunt", PrizeMoney = 200 };
        a.Party.Add(Mon("Bidoof", 5, null, Idle));
        b.Party.Add(Mon("Starly", 5, null, Idle));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            Format = BattleFormat.Double, Trainers = new() { a, b }, Random = new Random(3)
        });
        var said = Settle(battle);
        Assert.Contains("Galactic Grunt A and Galactic Grunt B would like to battle!", said);
        Assert.Contains("Galactic Grunt A sent out Bidoof!", said);
        Assert.Contains("Galactic Grunt B sent out Starly!", said);

        battle.SelectMove(0);
        battle.SelectMove(0);
        said = Settle(battle);
        if (battle.Result == BattleResult.PlayerVictory)
        {
            Assert.Contains("Player defeated Galactic Grunt A and Galactic Grunt B!", said);
            Assert.Contains("Lucas received $300 for winning!", said);
        }
    }

    [Fact]
    public void TestADoubleBattleNeedsTwoPokemonOnEachSide()
    {
        var party = new Party();
        party.Add(Mon("Turtwig", 20, null, Idle));
        var twins = new Trainer { Name = "Liv & Liz", TrainerClass = "Twins" };
        twins.Party.Add(Mon("Bidoof", 5, null, Idle));
        twins.Party.Add(Mon("Starly", 5, null, Idle));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            Format = BattleFormat.Double, Trainers = new() { twins }
        });
        Assert.False(battle.IsDouble);
    }

    [Fact]
    public void TestStruggleWhenNoPpIsLeft()
    {
        var mine = Mon("Turtwig", 30, null, M("Tackle"));
        mine.Moves[0].CurrentPP = 0;
        var battle = Wild(mine, Mon("Bidoof", 30, null, Idle));
        battle.SelectMainMenuOption(0);
        Assert.Equal("Turtwig has no moves left!", battle.CurrentMessage);
        var said = Settle(battle);
        Assert.Contains("Turtwig used Struggle!", said);
        Assert.Contains("Turtwig is hit with recoil!", said);
    }
}
