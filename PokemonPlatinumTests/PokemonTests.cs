using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

// MapDatabase is static, so the classes that rebuild it must not run in parallel
[Collection("MapDatabase")]
public class PokemonTests
{
    [Fact]
    public void TestBuildingsAreDetectedForThe3DField()
    {
        MapDatabase.Initialize();

        var twinleaf = MapStructures.FindBuildings(MapDatabase.Get("TwinleafTown"));
        Assert.Equal(2, twinleaf.Count);
        var home = twinleaf.Single(b => b.X0 == 4);
        Assert.Equal((4, 4, 8, 7), (home.X0, home.Y0, home.X1, home.Y1));
        Assert.Equal(BuildingKind.House, home.Kind);
        Assert.Equal(TileType.RoofGreen, home.RoofTile);
        Assert.Equal(new[] { (6, (string?)"PlayerHouse") }, home.Doors);
        Assert.Equal(new[] { 5 }, home.Plaques);

        var sandgem = MapStructures.FindBuildings(MapDatabase.Get("SandgemTown"));
        Assert.Equal(3, sandgem.Count);
        Assert.Contains(sandgem, b => b.Kind == BuildingKind.PokemonCenter && (b.X0, b.Y0, b.X1, b.Y1) == (4, 3, 8, 6));
        Assert.Contains(sandgem, b => b.Kind == BuildingKind.PokeMart && (b.X0, b.Y0, b.X1, b.Y1) == (20, 3, 24, 6));
        Assert.Contains(sandgem, b => b.Kind == BuildingKind.Lab && (b.X0, b.Y0, b.X1, b.Y1) == (4, 14, 10, 17));

        // Interior walls are room walls, not buildings
        Assert.Empty(MapStructures.FindBuildings(MapDatabase.Get("PokemonCenter")));
    }

    [Fact]
    public void TestRoute202LeadsToJubilifeCity()
    {
        MapDatabase.Initialize();

        var route = MapDatabase.Get("Route202");
        var north = route.GetWarpAt(14, 0);
        Assert.NotNull(north);
        Assert.Equal("JubilifeCity", north!.TargetMap);

        var city = MapDatabase.Get("JubilifeCity");
        var south = city.GetWarpAt(north.TargetX, north.TargetY + 1);
        Assert.NotNull(south);
        Assert.Equal(("Route202", 14, 1), (south!.TargetMap, south.TargetX, south.TargetY));

        // Jubilife has its own Center and Mart, recognised as such in the field
        var buildings = MapStructures.FindBuildings(city);
        Assert.Contains(buildings, b => b.Kind == BuildingKind.PokemonCenter && b.Doors.Contains((25, "JubilifePokemonCenter")));
        Assert.Contains(buildings, b => b.Kind == BuildingKind.PokeMart && b.Doors.Contains((32, "JubilifePokeMart")));
        Assert.Contains(buildings, b => b.Doors.Contains((7, "TrainersSchool")));
        Assert.Contains(buildings, b => b.Doors.Contains((26, "PoketchCompany")));

        // Leaving the Center puts you back in Jubilife, not Sandgem
        var exit = MapDatabase.Get("JubilifePokemonCenter").Warps.Single();
        Assert.Equal(("JubilifeCity", 25, 27), (exit.TargetMap, exit.TargetX, exit.TargetY));
        Assert.Contains(MapDatabase.Get("JubilifePokeMart").NPCs, n => n.IsPokeMartClerk && n.DialogLines[0].Contains("Jubilife"));

        // The routes beyond aren't built yet: every road out of the city is closed off short of the edge
        foreach (var (x, y) in new[] { (19, 0), (20, 0), (0, 16), (0, 17), (39, 16), (39, 17) })
        {
            Assert.Null(city.GetWarpAt(x, y));
        }
        Assert.False(city.IsWalkable(19, 1) || city.IsWalkable(20, 1), "the road to Route 204 should be closed");
        Assert.False(city.IsWalkable(1, 16) || city.IsWalkable(1, 17), "the gate to Route 218 should be closed");
        Assert.False(city.IsWalkable(38, 16) || city.IsWalkable(38, 17), "the road to Route 203 should be closed");
    }

    [Fact]
    public void TestTypeEffectivenessMatrix()
    {
        // Water -> Fire = 2.0x
        Assert.Equal(2.0f, TypeChart.GetEffectiveness(PokemonType.Water, PokemonType.Fire));

        // Fire -> Water = 0.5x
        Assert.Equal(0.5f, TypeChart.GetEffectiveness(PokemonType.Fire, PokemonType.Water));

        // Electric -> Ground = 0.0x (Immunity)
        Assert.Equal(0.0f, TypeChart.GetEffectiveness(PokemonType.Electric, PokemonType.Ground));

        // Normal -> Ghost = 0.0x
        Assert.Equal(0.0f, TypeChart.GetEffectiveness(PokemonType.Normal, PokemonType.Ghost));

        // Grass -> Water / Ground dual type = 2.0 * 2.0 = 4.0x (Quadruple weakness)
        Assert.Equal(4.0f, TypeChart.GetEffectiveness(PokemonType.Grass, PokemonType.Water, PokemonType.Ground));

        // Fighting -> Steel = 2.0x
        Assert.Equal(2.0f, TypeChart.GetEffectiveness(PokemonType.Fighting, PokemonType.Steel));
    }

    [Fact]
    public void TestDamageCalculationAndSplit()
    {
        var attackerSpecies = PokemonDatabase.Get("Chimchar")!;
        var defenderSpecies = PokemonDatabase.Get("Turtwig")!;

        var chimchar = new Pokemon(attackerSpecies, 10);
        var turtwig = new Pokemon(defenderSpecies, 10);

        // Special Fire Move (Ember) against Grass type: Super effective with STAB!
        var ember = MoveDatabase.Create("Ember");
        var result = DamageCalculator.CalculateDamage(chimchar, turtwig, ember);

        Assert.True(result.IsSuperEffective);
        Assert.True(result.IsSTAB);
        Assert.True(result.Damage > 0);

        // Physical Normal Move (Scratch) against Grass type: Neutral
        var scratch = MoveDatabase.Create("Scratch");
        var neutralResult = DamageCalculator.CalculateDamage(chimchar, turtwig, scratch);
        Assert.Equal(1.0f, neutralResult.TypeMultiplier);
    }

    [Fact]
    public void TestCatchRateFormula()
    {
        var starlySpecies = PokemonDatabase.Get("Starly")!;
        var wildStarly = new Pokemon(starlySpecies, 3);

        // Master Ball always succeeds
        var masterBall = ItemDatabase.Get("Master Ball")!;
        var masterCatch = CatchCalculator.AttemptCatch(wildStarly, masterBall);
        Assert.True(masterCatch.IsCaught);
        Assert.Equal(4, masterCatch.Shakes);

        // Poke Ball on low HP Starly
        wildStarly.CurrentHP = 1;
        var pokeBall = ItemDatabase.Get("Poké Ball")!;
        var catchAttempt = CatchCalculator.AttemptCatch(wildStarly, pokeBall);
        Assert.True(catchAttempt.Shakes >= 0 && catchAttempt.Shakes <= 4);
    }

    [Fact]
    public void TestLevelUpAndEvolution()
    {
        var turtwigSpecies = PokemonDatabase.Get("Turtwig")!;
        var turtwig = new Pokemon(turtwigSpecies, 17);

        Assert.Equal("Turtwig", turtwig.Species.Name);
        Assert.Equal(17, turtwig.Level);

        // Give enough EXP to level up to 18 (Turtwig evolution level)
        int expNeeded = turtwig.ExpForNextLevel - turtwig.CurrentExp + 10;
        bool leveledUp = turtwig.GainExp(expNeeded, out var newMoves, out bool evolved, out string oldName);

        Assert.True(leveledUp);
        Assert.Equal(18, turtwig.Level);
        Assert.True(evolved);
        Assert.Equal("Grotle", turtwig.Species.Name);
        Assert.Equal("Turtwig", oldName);
    }

    [Fact]
    public void TestPartyAndInventoryManagement()
    {
        var party = new Party();
        var turtwig = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5);
        var chimchar = new Pokemon(PokemonDatabase.Get("Chimchar")!, 5);

        Assert.True(party.Add(turtwig));
        Assert.True(party.Add(chimchar));
        Assert.Equal(2, party.Count);

        // Lead Pokemon is Turtwig
        Assert.Equal("Turtwig", party.Members[0].Species.Name);

        // Swap lead to Chimchar
        party.Swap(0, 1);
        Assert.Equal("Chimchar", party.Members[0].Species.Name);

        // Inventory
        var inventory = new Inventory();
        var potion = ItemDatabase.Get("Potion")!;
        inventory.AddItem(potion, 3);
        Assert.Equal(3, inventory.GetQuantity(potion));

        inventory.RemoveItem(potion, 1);
        Assert.Equal(2, inventory.GetQuantity(potion));
    }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void TestEveryWarpIsReachableFromEveryArrivalPoint(string mapName)
    {
        MapDatabase.Initialize();
        var map = MapDatabase.Get(mapName);
        Assert.Equal(mapName, map.Name);
        Assert.NotEmpty(map.Warps);

        // Every tile another map warps the player onto in this map
        var arrivals = MapDatabase.MapNames
            .SelectMany(n => MapDatabase.Get(n).Warps)
            .Where(w => w.TargetMap == mapName)
            .Select(w => (w.TargetX, w.TargetY))
            .ToList();
        Assert.NotEmpty(arrivals);

        foreach (var (startX, startY) in arrivals)
        {
            Assert.True(map.IsWalkable(startX, startY), $"{mapName}: arrival tile ({startX},{startY}) is not walkable");

            // Flood fill; stepping onto a warp tile triggers it, so don't expand past one
            var reached = new HashSet<(int, int)> { (startX, startY) };
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue((startX, startY));
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if ((x, y) != (startX, startY) && map.GetWarpAt(x, y) != null) continue;

                foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                {
                    if (map.IsWalkable(nx, ny) && reached.Add((nx, ny)))
                    {
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            foreach (var warp in map.Warps)
            {
                Assert.True(reached.Contains((warp.SourceX, warp.SourceY)),
                    $"{mapName}: warp to {warp.TargetMap} at ({warp.SourceX},{warp.SourceY}) is unreachable from arrival ({startX},{startY})");
            }
        }
    }

    public static IEnumerable<object[]> AllMaps => MapDatabase.MapNames.Select(n => new object[] { n }).ToList();

    [Fact]
    public void TestEveryDoorOnAHouseLeadsInside()
    {
        MapDatabase.Initialize();
        foreach (var name in MapDatabase.MapNames)
        {
            var map = MapDatabase.Get(name);
            foreach (var building in MapStructures.FindBuildings(map))
            {
                foreach (var (x, target) in building.Doors)
                {
                    Assert.False(target == null, $"{name}: the door at ({x},{building.Y1}) doesn't lead anywhere");
                    Assert.Contains(target, MapDatabase.MapNames);
                }
            }
        }
    }

    [Fact]
    public void TestFurnitureBlocksMovementButRugsDoNot()
    {
        MapDatabase.Initialize();
        var home = MapDatabase.Get("PlayerHouse");

        Assert.False(home.IsWalkable(4, 4), "the dining table should be solid");
        Assert.False(home.IsWalkable(8, 3), "the stairs should be solid");
        Assert.True(home.IsWalkable(4, 6), "the rug by the door should be walkable");
        Assert.True(home.IsWalkable(4, 5), "the white-out spot must stay free");

        // Nurse Joy stands behind the counter and is talked to across it
        var center = MapDatabase.Get("PokemonCenter");
        Assert.True(center.IsCounter(5, 3));
        Assert.False(center.IsWalkable(5, 3));
        Assert.NotNull(center.GetNpcAt(5, 2));
    }

    [Fact]
    public void TestEveryTrainerBringsTheirOwnTeam()
    {
        MapDatabase.Initialize();
        var trainers = MapDatabase.MapNames.SelectMany(n => MapDatabase.Get(n).NPCs).Where(n => n.IsTrainer).ToList();

        Assert.NotEmpty(trainers);
        Assert.All(trainers, t => Assert.True(t.TrainerData!.Party.Count > 0, $"{t.Name} has no Pokémon"));

        var logan = trainers.Single(t => t.Name == "Logan").TrainerData!;
        Assert.Equal(new[] { "Bidoof", "Starly" }, logan.Party.Members.Select(p => p.Species.Name));
    }

    [Fact]
    public void TestBeatenTrainersAreRememberedInTheSave()
    {
        MapDatabase.Initialize();

        // The save names trainers by id, so each needs one of their own
        var ids = MapDatabase.MapNames.SelectMany(n => MapDatabase.Get(n).NPCs).Where(n => n.IsTrainer).Select(n => n.TrainerData!.Id).ToList();
        Assert.DoesNotContain("", ids);
        Assert.Equal(ids.Count, ids.Distinct().Count());

        Assert.Empty(MapDatabase.DefeatedTrainerIds());
        MapDatabase.Get("Route201").NPCs.First(n => n.IsTrainer).HasBattled = true;
        var save = new PokemonPlatinumEngine.Core.SaveData { DefeatedTrainers = MapDatabase.DefeatedTrainerIds() };
        Assert.Equal(new[] { "trainer_tristan" }, save.DefeatedTrainers);
        string json = System.Text.Json.JsonSerializer.Serialize(save);

        // The game starts again with everyone waiting, then the save is loaded
        MapDatabase.Initialize();
        var route = MapDatabase.Get("Route201");
        Assert.NotNull(TrainerApproach.FindSpotter(route, 24, 9));

        var loaded = System.Text.Json.JsonSerializer.Deserialize<PokemonPlatinumEngine.Core.SaveData>(json)!;
        MapDatabase.RestoreDefeatedTrainers(loaded.DefeatedTrainers);
        Assert.True(route.NPCs.First(n => n.IsTrainer).HasBattled);
        Assert.Null(TrainerApproach.FindSpotter(route, 24, 9));
        Assert.All(MapDatabase.Get("Route202").NPCs.Where(n => n.IsTrainer), n => Assert.False(n.HasBattled));

        // A save from before trainers were recorded still loads: nobody has been beaten
        Assert.Empty(System.Text.Json.JsonSerializer.Deserialize<PokemonPlatinumEngine.Core.SaveData>("{}")!.DefeatedTrainers);

        MapDatabase.Initialize();
    }

    [Fact]
    public void TestRunningFromAWildBattleEndsItRightAway()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        var battle = new BattleEngine(party, new Pokemon(PokemonDatabase.Get("Starly")!, 3), new Inventory(), new Pokedex());
        SkipMessages(battle);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);

        battle.SelectMainMenuOption(3);

        Assert.Equal(BattleMenuState.Message, battle.HUD.MenuState);
        Assert.Equal("Got away safely!", battle.CurrentMessage);
        battle.ConfirmMessage();
        Assert.Equal(BattleResult.PlayerRan, battle.Result);
        Assert.True(battle.IsBattleOver);
    }

    [Fact]
    public void TestRunningFromATrainerIsRefusedRightAway()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        var trainer = new Trainer { Name = "Tristan", TrainerClass = "Youngster" };
        trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 4));
        var battle = new BattleEngine(party, trainer.Party.Members[0], new Inventory(), new Pokedex(), trainer);
        SkipMessages(battle);

        battle.SelectMainMenuOption(3);

        Assert.Equal("No! There's no running from a Trainer battle!", battle.CurrentMessage);
        battle.ConfirmMessage();
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.False(battle.IsBattleOver);
    }

    [Fact]
    public void TestTheHitLandsAfterTheAttackersLungeAndTheBarDrains()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 12, new Random(1)));
        var foe = new Pokemon(PokemonDatabase.Get("Bidoof")!, 8, new Random(2));
        var battle = new BattleEngine(party, foe, new Inventory(), new Pokedex());
        SkipMessages(battle);
        int hp = foe.CurrentHP;

        // Piplup is much faster, so its move comes first
        battle.SelectMove(party.Members[0].Moves.FindIndex(m => m.Category != MoveCategory.Status));
        Assert.StartsWith("Piplup used", battle.CurrentMessage);
        Assert.True(battle.Anim.Player.AttackAge >= 0f);

        // The lunge plays first; the hit lands a moment later and the bar drains toward the new HP
        Tick(battle, 0.3f);
        Assert.Equal(hp, foe.CurrentHP);
        Tick(battle, 0.1f);
        bool hit = foe.CurrentHP < hp;
        if (hit)
        {
            Assert.True(battle.Anim.Enemy.HitAge >= 0f);
            Assert.True(battle.Anim.Enemy.DisplayedHp > foe.CurrentHP);
            Tick(battle, 2f);
            Assert.Equal(foe.CurrentHP, battle.Anim.Enemy.DisplayedHp, 3);
        }

        battle.ConfirmMessage();
        if (!hit) Assert.Contains("missed", battle.CurrentMessage);
    }

    [Theory]
    [InlineData(MoveCategory.Physical)]
    [InlineData(MoveCategory.Special)]
    [InlineData(MoveCategory.Status)]
    public void TestTheAttackAnimationKnowsTheKindOfMove(MoveCategory category)
    {
        // A physical move strikes, a special one is cast and a status move is a hop (plan 04 · G7): the animator
        // is told which kind is being used
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 12, new Random(1)));
        var foe = new Pokemon(PokemonDatabase.Get("Bidoof")!, 8, new Random(2));
        var battle = new BattleEngine(party, foe, new Inventory(), new Pokedex());
        SkipMessages(battle);

        int move = party.Members[0].Moves.FindIndex(m => m.Category == category);
        Assert.True(move >= 0, $"Piplup knows no {category} move at level 12");
        battle.SelectMove(move);
        Assert.True(battle.Anim.Player.AttackAge >= 0f);
        Assert.Equal(category, battle.Anim.Player.AttackCategory);
    }

    [Fact]
    public void TestAFaintedPokemonMustBeReplaced()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5, new Random(3)));
        party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 5, new Random(4)));
        var battle = new BattleEngine(party, new Pokemon(PokemonDatabase.Get("Starly")!, 10, new Random(5)), new Inventory(), new Pokedex());
        SkipMessages(battle);

        // One HP and a burn: fainted by the foe's attack or by the burn at the end of the turn
        battle.PlayerPokemon.CurrentHP = 1;
        battle.PlayerPokemon.Status = StatusCondition.Burn;
        battle.SelectMove(0);
        SkipMessages(battle);

        Assert.True(battle.PlayerPokemon.IsFainted);
        Assert.Equal(BattleMenuState.SwitchPokemon, battle.HUD.MenuState);
    }

    [Fact]
    public void TestTheTrainerStepsAsideWhenSendingOutAPokemon()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        var trainer = new Trainer { Name = "Tristan", TrainerClass = "Youngster" };
        trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 4));
        var battle = new BattleEngine(party, trainer.Party.Members[0], new Inventory(), new Pokedex(), trainer);

        // Both trainers stand on their platforms until the Pokémon come out
        Assert.Equal("Youngster", battle.Anim.EnemyTrainer);
        Assert.False(battle.Anim.Enemy.Present);

        battle.ConfirmMessage();
        Assert.Contains("sent out", battle.CurrentMessage);
        Assert.True(battle.Anim.Enemy.Present);
        Tick(battle, 1f);
        Assert.Null(battle.Anim.EnemyTrainer);
        Assert.Equal("PLAYER", battle.Anim.PlayerTrainer);

        battle.ConfirmMessage();
        Assert.StartsWith("Go!", battle.CurrentMessage);
        Tick(battle, 1f);
        Assert.Null(battle.Anim.PlayerTrainer);
        Assert.True(battle.Anim.Player.Present);
    }

    [Fact]
    public void TestACapturedPokemonLeavesTheFieldUntilItBreaksFree()
    {
        var anim = new BattleAnimator();
        var mine = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5);
        var foe = new Pokemon(PokemonDatabase.Get("Starly")!, 3);
        anim.Appear(BattleSide.Enemy, foe);

        // Drawn into the ball once it arrives
        anim.Capture(0.8f);
        for (int i = 0; i < 30; i++) anim.Update(1f / 60f, mine, foe);
        Assert.True(anim.Enemy.Present);
        for (int i = 0; i < 60; i++) anim.Update(1f / 60f, mine, foe);
        Assert.False(anim.Enemy.Present);

        anim.BreakFree();
        Assert.True(anim.Enemy.Present);
        Assert.True(anim.Enemy.SendOutAge >= 0f);
    }

    [Fact]
    public void TestACaughtPokemonJoinsThePartyAndEndsTheBattle()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        var inventory = new Inventory();
        inventory.AddItem(ItemDatabase.Get("Poké Ball")!, 1);
        var pokedex = new Pokedex();

        // Asleep on 1 HP: a Poké Ball can't miss
        var foe = new Pokemon(PokemonDatabase.Get("Starly")!, 3) { CurrentHP = 1, Status = StatusCondition.Sleep };
        var battle = new BattleEngine(party, foe, inventory, pokedex, null, new List<Pokemon>());
        SkipMessages(battle);

        battle.SelectBagItem(0);
        Assert.Equal("Lucas used one Poké Ball!", battle.CurrentMessage);
        battle.ConfirmMessage();
        Tick(battle, BattleVFX.BallThrowTime(4) + 0.5f);

        Assert.Equal("Gotcha! Starly was caught!", battle.CurrentMessage);
        Assert.Contains(foe, party.Members);
        Assert.True(pokedex.IsCaught(foe.Species.DexNumber));
        battle.ConfirmMessage();
        Assert.Equal(BattleResult.EnemyCaught, battle.Result);
        Assert.True(battle.IsBattleOver);
    }

    [Fact]
    public void TestAPokemonThatBreaksFreeIsBackOnTheFieldBeforeTheMessage()
    {
        // Giratina at full HP almost always breaks out of a Poké Ball; try again on the rare catch
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var party = new Party();
            party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
            var inventory = new Inventory();
            inventory.AddItem(ItemDatabase.Get("Poké Ball")!, 1);
            var battle = new BattleEngine(party, new Pokemon(PokemonDatabase.Get("Giratina")!, 70), inventory, new Pokedex());
            SkipMessages(battle);

            battle.SelectBagItem(0);
            string thrown = battle.CurrentMessage;
            battle.ConfirmMessage();

            bool vanished = false;
            for (float t = 0f; t < 8f && battle.CurrentMessage == thrown; t += 1f / 60f)
            {
                battle.Update(1f / 60f);
                vanished |= !battle.Anim.Enemy.Present;
            }
            if (battle.CurrentMessage.StartsWith("Gotcha")) continue;

            // Drawn into the ball, then fully burst back out before the message says how close it was
            Assert.True(vanished);
            Assert.True(battle.Anim.Enemy.Present);
            Assert.True(battle.Anim.Enemy.SendOutAge < 0f);
            Assert.Single(party.Members);
            return;
        }
        Assert.Fail("Giratina was caught every time");
    }

    [Fact]
    public void TestGenderFollowsTheSpeciesRatio()
    {
        var rng = new Random(3);
        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(Gender.Genderless, new Pokemon(PokemonDatabase.Get("Magnemite")!, 5, rng).Gender);
            Assert.Equal(Gender.Female, new Pokemon(PokemonDatabase.Get("Nidoran♀")!, 5, rng).Gender);
            Assert.Equal(Gender.Male, new Pokemon(PokemonDatabase.Get("Tauros")!, 5, rng).Gender);
        }
        // Starters are seven males to one female
        int females = Enumerable.Range(0, 800).Count(_ => new Pokemon(PokemonDatabase.Get("Piplup")!, 5, rng).Gender == Gender.Female);
        Assert.InRange(females, 60, 140);
    }

    [Fact]
    public void TestEveryGrowthRateReachesItsLevel100Total()
    {
        Assert.Equal(600_000, Pokemon.GetExpForLevel(100, GrowthRate.Erratic));
        Assert.Equal(800_000, Pokemon.GetExpForLevel(100, GrowthRate.Fast));
        Assert.Equal(1_000_000, Pokemon.GetExpForLevel(100, GrowthRate.MediumFast));
        Assert.Equal(1_059_860, Pokemon.GetExpForLevel(100, GrowthRate.MediumSlow));
        Assert.Equal(1_250_000, Pokemon.GetExpForLevel(100, GrowthRate.Slow));
        Assert.Equal(1_640_000, Pokemon.GetExpForLevel(100, GrowthRate.Fluctuating));
        foreach (var rate in Enum.GetValues<GrowthRate>())
            for (int level = 2; level <= 100; level++)
                Assert.True(Pokemon.GetExpForLevel(level, rate) > Pokemon.GetExpForLevel(level - 1, rate), $"{rate} {level}");
    }

    [Fact]
    public void TestOnlyPlainLevelEvolutionsHappenOnLevelUp()
    {
        // Riolu evolves by friendship in the daytime, which the engine doesn't track yet: it stays a Riolu
        var riolu = new Pokemon(PokemonDatabase.Get("Riolu")!, 30, new Random(1));
        riolu.GainExp(riolu.ExpForNextLevel - riolu.CurrentExp, out _, out bool evolved, out _);
        Assert.False(evolved);
        Assert.Equal("Riolu", riolu.Species.Name);

        var starly = new Pokemon(PokemonDatabase.Get("Starly")!, 13, new Random(1));
        starly.GainExp(starly.ExpForNextLevel - starly.CurrentExp, out _, out evolved, out _);
        Assert.True(evolved);
        Assert.Equal("Staravia", starly.Species.Name);
    }

    [Fact]
    public void TestEverySpeciesHasA3DModel()
    {
        // The species the story shows so far have hand-built models; the rest use the generic stand-in until the
        // model generator (plan 03 · D5). This list may only grow.
        string[] handBuilt =
        {
            "Turtwig", "Grotle", "Torterra", "Chimchar", "Monferno", "Infernape", "Piplup", "Prinplup", "Empoleon",
            "Starly", "Staravia", "Staraptor", "Bidoof", "Bibarel", "Shinx", "Luxio", "Luxray", "Riolu", "Lucario",
            "Gible", "Gabite", "Garchomp", "Giratina"
        };
        Assert.All(handBuilt, n => Assert.True(PokemonPlatinumEngine.Graphics.PokemonModels.HasModel(n), n));
        Assert.All(handBuilt, n => Assert.NotNull(PokemonDatabase.Get(n)));
        Assert.All(PokemonPlatinumEngine.Graphics.PokemonModels.Species, n => Assert.NotNull(PokemonDatabase.Get(n)));
    }

    private static void Tick(BattleEngine battle, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) battle.Update(1f / 60f);
    }

    private static void SkipMessages(BattleEngine battle)
    {
        for (int i = 0; i < 10 && battle.HUD.MenuState == BattleMenuState.Message; i++)
        {
            battle.ConfirmMessage();
        }
    }
}
