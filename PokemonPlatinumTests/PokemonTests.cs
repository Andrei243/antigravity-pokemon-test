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

        // A hand-made map says what its buildings are with roof, wall, door and sign tiles
        var twinleaf = MapStructures.FindBuildings(Fixtures.Map("TwinleafTown"));
        Assert.Equal(2, twinleaf.Count);
        var home = twinleaf.Single(b => b.X0 == 4);
        Assert.Equal((4, 4, 8, 7), (home.X0, home.Y0, home.X1, home.Y1));
        Assert.Equal(BuildingKind.House, home.Kind);
        Assert.Equal(TileType.RoofGreen, home.RoofTile);
        Assert.Equal(new[] { (6, (string?)"PlayerHouse") }, home.Doors);
        Assert.Equal(new[] { 5 }, home.Plaques);

        var sandgem = MapStructures.FindBuildings(Fixtures.Map("SandgemTown"));
        Assert.Equal(3, sandgem.Count);
        Assert.Contains(sandgem, b => b.Kind == BuildingKind.PokemonCenter && (b.X0, b.Y0, b.X1, b.Y1) == (4, 3, 8, 6));
        Assert.Contains(sandgem, b => b.Kind == BuildingKind.PokeMart && (b.X0, b.Y0, b.X1, b.Y1) == (20, 3, 24, 6));
        Assert.Contains(sandgem, b => b.Kind == BuildingKind.Lab && (b.X0, b.Y0, b.X1, b.Y1) == (4, 14, 10, 17));

        // Interior walls are room walls, not buildings
        Assert.Empty(MapStructures.FindBuildings(MapDatabase.Get("PokemonCenter")));
    }

    [Fact]
    public void TestRoute202RunsOnIntoJubilifeCity()
    {
        MapDatabase.Initialize();

        // The city is part of the map of Sinnoh (plan 01 · M5): the road goes on into it, with no warp on the way
        var sinnoh = MapDatabase.Get("Sinnoh");
        Assert.Equal("route_202", sinnoh.AreaAt(173, 800)!.Key);
        Assert.Null(sinnoh.GetWarpAt(173, 800));
        Assert.Equal("jubilife_city", sinnoh.AreaAt(174, 796)!.Key);
        Assert.True(sinnoh.IsWalkable(173, 800) && sinnoh.IsWalkable(174, 796));
        Assert.DoesNotContain("JubilifeCity", MapDatabase.MapNames);

        // Jubilife has its own Center and Mart, recognised as such in the field, and its two landmarks with rooms
        var buildings = MapStructures.BuildingsOf(sinnoh);
        Assert.Contains(buildings, b => b.Kind == BuildingKind.PokemonCenter && b.Doors.Contains((180, "JubilifePokemonCenter")));
        Assert.Contains(buildings, b => b.Kind == BuildingKind.PokeMart && b.Doors.Contains((179, "JubilifePokeMart")));
        Assert.Contains(buildings, b => b.Kind == BuildingKind.School && b.Doors.Contains((168, "TrainersSchool")));
        Assert.Contains(buildings, b => b.Kind == BuildingKind.Office && b.Doors.Contains((141, "PoketchCompany")) && b.Doors.Contains((145, "PoketchCompany")));

        // Leaving a room puts you down outside its own door, in the city
        foreach (var (room, doorX, doorY) in new[] { ("JubilifePokemonCenter", 180, 776), ("JubilifePokeMart", 179, 766), ("TrainersSchool", 168, 776), ("PoketchCompany", 141, 751) })
        {
            Assert.Equal(room, sinnoh.GetWarpAt(doorX, doorY)?.TargetMap);
            var exit = MapDatabase.Get(room).Warps.Single();
            Assert.Equal(("Sinnoh", doorX, doorY + 1), (exit.TargetMap, exit.TargetX, exit.TargetY));
            Assert.True(sinnoh.IsWalkable(exit.TargetX, exit.TargetY), $"{room} lets out onto a tile nobody can stand on");
        }
        Assert.Contains(MapDatabase.Get("JubilifePokeMart").NPCs, n => n.IsPokeMartClerk && n.DialogLines[0].Contains("Jubilife"));

        // The roads out are open: east to Route 203, north to Route 204, and through the gate house west to Route 218
        // (plan 01 · M7)
        Assert.True(sinnoh.IsWalkable(190, 758) && sinnoh.AreaAt(193, 758)!.Key == "route_203" && sinnoh.IsWalkable(193, 758));
        Assert.True(sinnoh.IsWalkable(175, 737) && sinnoh.AreaAt(175, 735)!.Key == "route_204_south" && sinnoh.IsWalkable(175, 735));
        var gate = sinnoh.GetWarpAt(128, 758)!;
        Assert.Equal("route_218", sinnoh.AreaAt(gate.TargetX, gate.TargetY)!.Key);
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
        bool leveledUp = turtwig.GainExp(expNeeded, out var newMoves);

        // The level comes first; it is still a Turtwig until the battle (or the Rare Candy) is over
        Assert.True(leveledUp);
        Assert.Equal(18, turtwig.Level);
        Assert.Equal("Turtwig", turtwig.Species.Name);

        var evolution = Evolution.Find(turtwig, EvolutionTrigger.LevelUp, new EvolutionContext());
        Assert.NotNull(evolution);
        var outcome = Evolution.Evolve(turtwig, evolution!, new EvolutionContext());
        Assert.Equal("Grotle", turtwig.Species.Name);
        Assert.Equal("Turtwig", outcome.From.Name);
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
        // A map of the imported world is many places, and not every one leads to every other: a cave lies between
        // the two halves of a route, rocks wait for Rock Smash. The test below walks the whole game instead.
        if (map.IsStreamed) return;

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
    public void TestEveryWarpCanBeReachedFromWhereTheGameStarts()
    {
        MapDatabase.Initialize();
        var reached = WorldWalk.FromEveryStart(MapDatabase.Get);

        foreach (string name in MapDatabase.MapNames)
        {
            var map = MapDatabase.Get(name);
            // Johto and the regions after it have no maps yet; every map there is must be one the game can come to
            Assert.True(reached.TryGetValue(map, out var tiles), $"{name} can't be reached from where its region starts");
            foreach (var warp in map.Warps)
                Assert.True(tiles!.Contains((warp.SourceX, warp.SourceY)),
                    $"{name}: the warp to {warp.TargetMap} at ({warp.SourceX},{warp.SourceY}) can't be reached from where the game starts");
        }
    }

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
                    // On the map of the imported world a house whose rooms aren't built yet keeps its door shut
                    // (WorldTests checks each is listed as locked); an open door always leads inside
                    if (map.IsStreamed && target == null && map.IsSolid(x, building.Y1)) continue;
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
        Assert.False(home.IsWalkable(8, 2), "the stairs should be solid");
        Assert.Equal("PlayerHouse2F", home.GetWarpAt(8, 3)?.TargetMap);
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

        // Platinum's own teams: Logan's Burmy knows nothing but Tackle
        var logan = trainers.Single(t => t.Name == "Logan").TrainerData!;
        Assert.Equal(new[] { "Burmy" }, logan.Party.Members.Select(p => p.Species.Name));
        Assert.Equal(5, logan.Party.Members[0].Level);
        Assert.Equal(new[] { "Tackle" }, logan.Party.Members[0].Moves.Select(m => m.Name));
        Assert.Equal(80, logan.PrizeMoney);

        // The twins of Route 204 are two people and one trainer, who battles two Pokémon at a time
        var twins = trainers.Where(t => t.TrainerData!.Id == "trainer_liv_and_liz").ToList();
        Assert.Equal(new[] { "Liv", "Liz" }, twins.Select(t => t.Name).OrderBy(n => n));
        Assert.All(twins, t => Assert.True(t.TrainerData!.DoubleBattle));
    }

    [Fact]
    public void TestBeatenTrainersAreRememberedInTheSave()
    {
        MapDatabase.Initialize();

        // The save names trainers by id, so each needs one of their own; two people share one only where they
        // battle together as one trainer
        var known = MapDatabase.MapNames.SelectMany(n => MapDatabase.Get(n).NPCs).Where(n => n.IsTrainer).Select(n => n.TrainerData!).ToList();
        Assert.DoesNotContain("", known.Select(t => t.Id));
        Assert.All(known.GroupBy(t => t.Id).Where(g => g.Count() > 1), pair => Assert.All(pair, t => Assert.True(t.DoubleBattle, $"{t.Id} is the id of two trainers")));

        Assert.Empty(MapDatabase.DefeatedTrainerIds());
        var sinnoh = MapDatabase.Get("Sinnoh");
        sinnoh.NPCs.Single(n => n.Id == "trainer_tristan").HasBattled = true;
        var save = new PokemonPlatinumEngine.Core.SaveData { DefeatedTrainers = MapDatabase.DefeatedTrainerIds() };
        Assert.Equal(new[] { "trainer_tristan" }, save.DefeatedTrainers);
        string json = System.Text.Json.JsonSerializer.Serialize(save);

        // The game starts again with everyone waiting, then the save is loaded
        MapDatabase.Initialize();
        // Tristan looks south down Route 202 from (166, 813)
        var route = MapDatabase.Get("Sinnoh");
        Assert.NotNull(TrainerApproach.FindSpotter(route, 166, 815));

        var loaded = System.Text.Json.JsonSerializer.Deserialize<PokemonPlatinumEngine.Core.SaveData>(json)!;
        MapDatabase.RestoreDefeatedTrainers(loaded.DefeatedTrainers);
        Assert.True(route.NPCs.Single(n => n.Id == "trainer_tristan").HasBattled);
        Assert.Null(TrainerApproach.FindSpotter(route, 166, 815));
        Assert.All(route.NPCs.Where(n => n.IsTrainer && n.Id != "trainer_tristan"), n => Assert.False(n.HasBattled));

        // A save from before trainers were recorded still loads: nobody has been beaten
        Assert.Empty(System.Text.Json.JsonSerializer.Deserialize<PokemonPlatinumEngine.Core.SaveData>("{}")!.DefeatedTrainers);

        MapDatabase.Initialize();
    }

    [Fact]
    public void TestRunningFromAWildBattleEndsItRightAway()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        // Getting away from something faster is a roll (FormulaTests has its numbers); here it comes out well
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = { new Pokemon(PokemonDatabase.Get("Starly")!, 3) },
            Random = new PokemonPlatinumEngine.Battle.Sim.BattleRandom(1).Force(PokemonPlatinumEngine.Battle.Sim.RollKind.Escape, 0)
        });
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
        var battle = new BattleEngine(party, foe, inventory, pokedex, null, new PcBoxes());
        SkipMessages(battle);

        battle.UseBagItem("Poké Ball");
        Assert.Equal("Lucas used one Poké Ball!", battle.CurrentMessage);
        battle.ConfirmMessage();
        Tick(battle, BattleAnimator.BallThrowTime(4) + 0.5f);

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

            battle.UseBagItem("Poké Ball");
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
    public void TestALevelAloneEvolvesOnlyTheSpeciesThatAskForNothingMore()
    {
        // Riolu evolves by friendship in the daytime: a new level doesn't do it for a Riolu fresh from the wild
        var riolu = new Pokemon(PokemonDatabase.Get("Riolu")!, 30, new Random(1));
        riolu.GainExp(riolu.ExpForNextLevel - riolu.CurrentExp, out _);
        Assert.Null(Evolution.Find(riolu, EvolutionTrigger.LevelUp, new EvolutionContext()));

        var starly = new Pokemon(PokemonDatabase.Get("Starly")!, 13, new Random(1));
        starly.GainExp(starly.ExpForNextLevel - starly.CurrentExp, out _);
        Assert.Equal("Staravia", Evolution.Find(starly, EvolutionTrigger.LevelUp, new EvolutionContext())!.TargetSpecies);
    }

    [Fact]
    public void TestEverySpeciesHasA3DModel()
    {
        // The species of the Sinnoh Pokédex have hand-built models (plan 04 · G7 and plan 03 · D6–D9), and so, one batch
        // at a time, do the popular ones from outside it (plan 03, decision 3); the rest are generated (plan 03 · D5).
        // This list may only grow.
        string[] handBuilt =
        {
            "Turtwig", "Grotle", "Torterra", "Chimchar", "Monferno", "Infernape", "Piplup", "Prinplup", "Empoleon",
            "Starly", "Staravia", "Staraptor", "Bidoof", "Bibarel", "Shinx", "Luxio", "Luxray", "Riolu", "Lucario",
            "Gible", "Gabite", "Garchomp", "Giratina", "Buneary",
            // Plan 03 · D6, the Sinnoh Pokédex's first batch
            "Kricketot", "Kricketune", "Abra", "Kadabra", "Alakazam", "Magikarp", "Gyarados", "Budew", "Roselia", "Roserade",
            "Zubat", "Golbat", "Crobat", "Geodude", "Graveler", "Golem", "Onix", "Steelix", "Cranidos", "Rampardos",
            "Shieldon", "Bastiodon", "Machop", "Machoke", "Machamp", "Psyduck", "Golduck", "Burmy", "Wormadam", "Mothim",
            "Wurmple", "Silcoon", "Beautifly", "Cascoon", "Dustox", "Combee", "Vespiquen", "Pachirisu", "Buizel", "Floatzel",
            "Cherubi", "Cherrim", "Shellos", "Gastrodon", "Heracross", "Aipom", "Ambipom", "Drifloon", "Drifblim", "Lopunny",
            // Plan 03 · D7, the second batch
            "Gastly", "Haunter", "Gengar", "Misdreavus", "Mismagius", "Murkrow", "Honchkrow", "Glameow", "Purugly", "Goldeen",
            "Seaking", "Barboach", "Whiscash", "Chingling", "Chimecho", "Stunky", "Skuntank", "Meditite", "Medicham", "Bronzor",
            "Bronzong", "Ponyta", "Rapidash", "Bonsly", "Sudowoodo", "Mime Jr.", "Mr. Mime", "Happiny", "Chansey", "Blissey",
            "Cleffa", "Clefairy", "Clefable", "Chatot", "Pichu", "Pikachu", "Raichu", "Hoothoot", "Noctowl", "Spiritomb",
            "Munchlax", "Snorlax", "Unown", "Wooper", "Quagsire", "Wingull", "Pelipper", "Girafarig", "Hippopotas", "Hippowdon",
            // Plan 03 · D8, the third batch
            "Azurill", "Marill", "Azumarill", "Skorupi", "Drapion", "Croagunk", "Toxicroak", "Carnivine", "Remoraid", "Octillery",
            "Finneon", "Lumineon", "Tentacool", "Tentacruel", "Feebas", "Milotic", "Mantyke", "Mantine", "Snover", "Abomasnow",
            "Sneasel", "Weavile", "Rotom", "Gligar", "Gliscor", "Nosepass", "Probopass", "Ralts", "Kirlia", "Gardevoir",
            "Gallade", "Lickitung", "Lickilicky", "Eevee", "Vaporeon", "Jolteon", "Flareon", "Espeon", "Umbreon", "Leafeon",
            "Glaceon", "Swablu", "Altaria", "Togepi", "Togetic", "Togekiss", "Houndour", "Houndoom", "Magnemite", "Magneton",
            "Magnezone",
            // Plan 03 · D9, the last batch
            "Uxie", "Mesprit", "Azelf", "Dialga", "Palkia", "Manaphy", "Tangela", "Tangrowth", "Yanma", "Yanmega",
            "Tropius", "Rhyhorn", "Rhydon", "Rhyperior", "Duskull", "Dusclops", "Dusknoir", "Porygon", "Porygon2", "Porygon-Z",
            "Scyther", "Scizor", "Elekid", "Electabuzz", "Electivire", "Magby", "Magmar", "Magmortar", "Swinub", "Piloswine",
            "Mamoswine", "Snorunt", "Glalie", "Froslass", "Absol",
            // Popular species from outside the Sinnoh Pokédex (plan 03, decision 3): Kanto's first, Bulbasaur to Vileplume
            "Bulbasaur", "Ivysaur", "Venusaur", "Charmander", "Charmeleon", "Charizard", "Squirtle", "Wartortle", "Blastoise",
            "Caterpie", "Metapod", "Butterfree", "Weedle", "Kakuna", "Beedrill", "Pidgey", "Pidgeotto", "Pidgeot", "Rattata",
            "Raticate", "Spearow", "Fearow", "Ekans", "Arbok", "Sandshrew", "Sandslash", "Nidoran♀", "Nidorina", "Nidoqueen",
            "Nidoran♂", "Nidorino", "Nidoking", "Vulpix", "Ninetales", "Jigglypuff", "Wigglytuff", "Oddish", "Gloom", "Vileplume",
            // Kanto's second batch, Paras to Kingler
            "Paras", "Parasect", "Venonat", "Venomoth", "Diglett", "Dugtrio", "Meowth", "Persian", "Mankey", "Primeape",
            "Growlithe", "Arcanine", "Poliwag", "Poliwhirl", "Poliwrath", "Bellsprout", "Weepinbell", "Victreebel",
            "Slowpoke", "Slowbro", "Farfetch'd", "Doduo", "Dodrio", "Seel", "Dewgong", "Grimer", "Muk", "Shellder", "Cloyster",
            "Drowzee", "Hypno", "Krabby", "Kingler",
            // Kanto's third batch, Voltorb to Mew
            "Voltorb", "Electrode", "Exeggcute", "Exeggutor", "Cubone", "Marowak", "Hitmonlee", "Hitmonchan", "Koffing", "Weezing",
            "Kangaskhan", "Horsea", "Seadra", "Staryu", "Starmie", "Jynx", "Pinsir", "Tauros", "Lapras", "Ditto",
            "Omanyte", "Omastar", "Kabuto", "Kabutops", "Aerodactyl", "Articuno", "Zapdos", "Moltres", "Dratini", "Dragonair",
            "Dragonite", "Mewtwo", "Mew",
            // Johto's first batch, Chikorita to Corsola
            "Chikorita", "Bayleef", "Meganium", "Cyndaquil", "Quilava", "Typhlosion", "Totodile", "Croconaw", "Feraligatr",
            "Sentret", "Furret", "Ledyba", "Ledian", "Spinarak", "Ariados", "Chinchou", "Lanturn", "Igglybuff", "Natu", "Xatu",
            "Mareep", "Flaaffy", "Ampharos", "Bellossom", "Politoed", "Hoppip", "Skiploom", "Jumpluff", "Sunkern", "Sunflora",
            "Slowking", "Wobbuffet", "Pineco", "Forretress", "Dunsparce", "Snubbull", "Granbull", "Qwilfish", "Shuckle",
            "Teddiursa", "Ursaring", "Slugma", "Magcargo", "Corsola",
            // Johto's second batch, Delibird to Celebi
            "Delibird", "Skarmory", "Kingdra", "Phanpy", "Donphan", "Stantler", "Smeargle", "Tyrogue", "Hitmontop", "Smoochum", "Miltank",
            "Raikou", "Entei", "Suicune", "Larvitar", "Pupitar", "Tyranitar", "Lugia", "Ho-Oh", "Celebi",
            // Hoenn's first batch, Treecko to Delcatty
            "Treecko", "Grovyle", "Sceptile", "Torchic", "Combusken", "Blaziken", "Mudkip", "Marshtomp", "Swampert", "Poochyena",
            "Mightyena", "Zigzagoon", "Linoone", "Lotad", "Lombre", "Ludicolo", "Seedot", "Nuzleaf", "Shiftry", "Taillow", "Swellow",
            "Surskit", "Masquerain", "Shroomish", "Breloom", "Slakoth", "Vigoroth", "Slaking", "Nincada", "Ninjask", "Shedinja", "Whismur",
            "Loudred", "Exploud", "Makuhita", "Hariyama", "Skitty", "Delcatty",
            // Hoenn's second batch, Sableye to Armaldo
            "Sableye", "Mawile", "Aron", "Lairon", "Aggron", "Electrike", "Manectric", "Plusle", "Minun", "Volbeat", "Illumise",
            "Gulpin", "Swalot", "Carvanha", "Sharpedo", "Wailmer", "Wailord", "Numel", "Camerupt", "Torkoal", "Spoink", "Grumpig",
            "Spinda", "Trapinch", "Vibrava", "Flygon", "Cacnea", "Cacturne", "Zangoose", "Seviper", "Lunatone", "Solrock", "Corphish",
            "Crawdaunt", "Baltoy", "Claydol", "Lileep", "Cradily", "Anorith", "Armaldo",
            // Hoenn's third batch, Castform to Deoxys
            "Castform", "Kecleon", "Shuppet", "Banette", "Wynaut", "Spheal", "Sealeo", "Walrein", "Clamperl", "Huntail", "Gorebyss",
            "Relicanth", "Luvdisc", "Bagon", "Shelgon", "Salamence", "Beldum", "Metang", "Metagross", "Regirock", "Regice", "Registeel",
            "Latias", "Latios", "Kyogre", "Groudon", "Rayquaza", "Jirachi", "Deoxys",
            // Unova's first batch, Victini to Audino
            "Victini", "Snivy", "Servine", "Serperior", "Tepig", "Pignite", "Emboar", "Oshawott", "Dewott", "Samurott", "Patrat",
            "Watchog", "Lillipup", "Herdier", "Stoutland", "Purrloin", "Liepard", "Pansage", "Simisage", "Pansear", "Simisear", "Panpour",
            "Simipour", "Munna", "Musharna", "Pidove", "Tranquill", "Unfezant", "Blitzle", "Zebstrika", "Roggenrola", "Boldore", "Gigalith",
            "Woobat", "Swoobat", "Drilbur", "Excadrill", "Audino",
            // Unova's second batch, Timburr to Zoroark
            "Timburr", "Gurdurr", "Conkeldurr", "Tympole", "Palpitoad", "Seismitoad", "Throh", "Sawk", "Sewaddle", "Swadloon", "Leavanny",
            "Venipede", "Whirlipede", "Scolipede", "Cottonee", "Whimsicott", "Petilil", "Lilligant", "Basculin", "Sandile", "Krokorok",
            "Krookodile", "Darumaka", "Darmanitan", "Maractus", "Dwebble", "Crustle", "Scraggy", "Scrafty", "Sigilyph", "Yamask", "Cofagrigus",
            "Tirtouga", "Carracosta", "Archen", "Archeops", "Trubbish", "Garbodor", "Zorua", "Zoroark",
            // Unova's third batch, Minccino to Chandelure
            "Minccino", "Cinccino", "Gothita", "Gothorita", "Gothitelle", "Solosis", "Duosion", "Reuniclus", "Ducklett", "Swanna", "Vanillite",
            "Vanillish", "Vanilluxe", "Deerling", "Sawsbuck", "Emolga", "Karrablast", "Escavalier", "Foongus", "Amoonguss", "Frillish", "Jellicent",
            "Alomomola", "Joltik", "Galvantula", "Ferroseed", "Ferrothorn", "Klink", "Klang", "Klinklang", "Tynamo", "Eelektrik", "Eelektross",
            "Elgyem", "Beheeyem", "Litwick", "Lampent", "Chandelure",
            // Unova's fourth batch, Axew to Genesect
            "Axew", "Fraxure", "Haxorus", "Cubchoo", "Beartic", "Cryogonal", "Shelmet", "Accelgor", "Stunfisk", "Mienfoo", "Mienshao",
            "Druddigon", "Golett", "Golurk", "Pawniard", "Bisharp", "Bouffalant", "Rufflet", "Braviary", "Vullaby", "Mandibuzz", "Heatmor",
            "Durant", "Deino", "Zweilous", "Hydreigon", "Larvesta", "Volcarona", "Cobalion", "Terrakion", "Virizion", "Tornadus", "Thundurus",
            "Reshiram", "Zekrom", "Landorus", "Kyurem", "Keldeo", "Meloetta", "Genesect",
            // Kalos's first batch, Chespin to Slurpuff
            "Chespin", "Quilladin", "Chesnaught", "Fennekin", "Braixen", "Delphox", "Froakie", "Frogadier", "Greninja", "Bunnelby", "Diggersby",
            "Fletchling", "Fletchinder", "Talonflame", "Scatterbug", "Spewpa", "Vivillon", "Litleo", "Pyroar", "Flabébé", "Floette", "Florges",
            "Skiddo", "Gogoat", "Pancham", "Pangoro", "Furfrou", "Espurr", "Meowstic", "Honedge", "Doublade", "Aegislash", "Spritzee",
            "Aromatisse", "Swirlix", "Slurpuff",
            // Kalos's second batch, Inkay to Volcanion
            "Inkay", "Malamar", "Binacle", "Barbaracle", "Skrelp", "Dragalge", "Clauncher", "Clawitzer", "Helioptile", "Heliolisk", "Tyrunt",
            "Tyrantrum", "Amaura", "Aurorus", "Sylveon", "Hawlucha", "Dedenne", "Carbink", "Goomy", "Sliggoo", "Goodra", "Klefki", "Phantump",
            "Trevenant", "Pumpkaboo", "Gourgeist", "Bergmite", "Avalugg", "Noibat", "Noivern", "Xerneas", "Yveltal", "Zygarde", "Diancie",
            "Hoopa", "Volcanion",
            // Alola's first batch, Rowlet to Bewear
            "Rowlet", "Dartrix", "Decidueye", "Litten", "Torracat", "Incineroar", "Popplio", "Brionne", "Primarina", "Pikipek", "Trumbeak",
            "Toucannon", "Yungoos", "Gumshoos", "Grubbin", "Charjabug", "Vikavolt", "Crabrawler", "Crabominable", "Oricorio", "Cutiefly",
            "Ribombee", "Rockruff", "Lycanroc", "Wishiwashi", "Mareanie", "Toxapex", "Mudbray", "Mudsdale", "Dewpider", "Araquanid",
            "Fomantis", "Lurantis", "Morelull", "Shiinotic", "Salandit", "Salazzle", "Stufful", "Bewear",
            // Alola's second batch, Bounsweet to Kommo-o
            "Bounsweet", "Steenee", "Tsareena", "Comfey", "Oranguru", "Passimian", "Wimpod", "Golisopod", "Sandygast", "Palossand",
            "Pyukumuku", "Type: Null", "Silvally", "Minior", "Komala", "Turtonator", "Togedemaru", "Mimikyu", "Bruxish", "Drampa", "Dhelmise",
            "Jangmo-o", "Hakamo-o", "Kommo-o",
            // Alola's third batch, Tapu Koko to Melmetal
            "Tapu Koko", "Tapu Lele", "Tapu Bulu", "Tapu Fini", "Cosmog", "Cosmoem", "Solgaleo", "Lunala", "Nihilego", "Buzzwole", "Pheromosa",
            "Xurkitree", "Celesteela", "Kartana", "Guzzlord", "Necrozma", "Magearna", "Marshadow", "Poipole", "Naganadel", "Stakataka",
            "Blacephalon", "Zeraora", "Meltan", "Melmetal",
            // Sinnoh's last seven, outside Platinum's Sinnoh Pokédex
            "Heatran", "Regigigas", "Cresselia", "Phione", "Darkrai", "Shaymin", "Arceus",
            // Galar's first batch, Grookey to Eldegoss
            "Grookey", "Thwackey", "Rillaboom", "Scorbunny", "Raboot", "Cinderace", "Sobble", "Drizzile", "Inteleon", "Skwovet", "Greedent",
            "Rookidee", "Corvisquire", "Corviknight", "Blipbug", "Dottler", "Orbeetle", "Nickit", "Thievul", "Gossifleur", "Eldegoss",
            // Galar's second batch, Wooloo to Polteageist
            "Wooloo", "Dubwool", "Chewtle", "Drednaw", "Yamper", "Boltund", "Rolycoly", "Carkol", "Coalossal", "Applin", "Flapple", "Appletun",
            "Silicobra", "Sandaconda", "Cramorant", "Arrokuda", "Barraskewda", "Toxel", "Toxtricity", "Sizzlipede", "Centiskorch", "Clobbopus",
            "Grapploct", "Sinistea", "Polteageist",
            // Galar's third batch, Hatenna to Morpeko
            "Hatenna", "Hattrem", "Hatterene", "Impidimp", "Morgrem", "Grimmsnarl", "Obstagoon", "Perrserker", "Cursola", "Sirfetch'd", "Mr. Rime",
            "Runerigus", "Milcery", "Alcremie", "Falinks", "Pincurchin", "Snom", "Frosmoth", "Stonjourner", "Eiscue", "Indeedee", "Morpeko",
            // Galar's last batch, Cufant to Calyrex
            "Cufant", "Copperajah", "Dracozolt", "Arctozolt", "Dracovish", "Arctovish", "Duraludon", "Dreepy", "Drakloak", "Dragapult", "Zacian",
            "Zamazenta", "Eternatus", "Kubfu", "Urshifu", "Zarude", "Regieleki", "Regidrago", "Glastrier", "Spectrier", "Calyrex",
            // The seven first met in Hisui, and Paldea's first batch, Sprigatito to Pawmot
            "Wyrdeer", "Kleavor", "Ursaluna", "Basculegion", "Sneasler", "Overqwil", "Enamorus",
            "Sprigatito", "Floragato", "Meowscarada", "Fuecoco", "Crocalor", "Skeledirge", "Quaxly", "Quaxwell", "Quaquaval", "Lechonk", "Oinkologne",
            "Tarountula", "Spidops", "Nymble", "Lokix", "Pawmi", "Pawmo", "Pawmot",
            // Paldea's second batch, Tandemaus to Klawf
            "Tandemaus", "Maushold", "Fidough", "Dachsbun", "Smoliv", "Dolliv", "Arboliva", "Squawkabilly", "Nacli", "Naclstack", "Garganacl",
            "Charcadet", "Armarouge", "Ceruledge", "Tadbulb", "Bellibolt", "Wattrel", "Kilowattrel", "Maschiff", "Mabosstiff", "Shroodle", "Grafaiai",
            "Bramblin", "Brambleghast", "Toedscool", "Toedscruel", "Klawf"
        };
        Assert.All(handBuilt, n => Assert.True(PokemonPlatinumEngine.Graphics.PokemonModels.HasModel(n), n));
        Assert.All(handBuilt, n => Assert.NotNull(PokemonDatabase.Get(n)));
        Assert.All(PokemonPlatinumEngine.Graphics.PokemonModels.Species, n => Assert.NotNull(PokemonDatabase.Get(n)));
    }

    [Fact]
    public void EverySpeciesOfTheSinnohPokedexIsHandBuilt()
    {
        // Plan 03 · D6–D9: Platinum's 210 Sinnoh species all have hand-built models; the generator is for the rest
        var sinnoh = PokemonDatabase.GetAll().Where(s => s.SinnohNumber != null).Select(s => s.Name).ToList();
        Assert.Equal(210, sinnoh.Count);
        Assert.All(sinnoh, n => Assert.True(PokemonPlatinumEngine.Graphics.PokemonModels.HasModel(n), n));
    }

    [Fact]
    public void EverySpeciesOfKantoIsHandBuilt()
    {
        // Plan 03, decision 3: the 105 Kanto species outside the Sinnoh Pokédex, in three batches, and the 46 inside it
        var kanto = PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 1 and <= 151).Select(s => s.Name).ToList();
        Assert.Equal(151, kanto.Count);
        Assert.All(kanto, n => Assert.True(PokemonPlatinumEngine.Graphics.PokemonModels.HasModel(n), n));
    }

    [Fact]
    public void EverySpeciesOfJohtoIsHandBuilt()
    {
        // Plan 03, decision 3: the 64 Johto species outside the Sinnoh Pokédex, in two batches, and the 36 inside it
        var johto = PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 152 and <= 251).Select(s => s.Name).ToList();
        Assert.Equal(100, johto.Count);
        Assert.All(johto, n => Assert.True(PokemonPlatinumEngine.Graphics.PokemonModels.HasModel(n), n));
    }

    [Fact]
    public void EverySpeciesOfHoennIsHandBuilt()
    {
        // Plan 03, decision 3: the 107 Hoenn species outside the Sinnoh Pokédex, in three batches, and the 28 inside it
        var hoenn = PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 252 and <= 386).Select(s => s.Name).ToList();
        Assert.Equal(135, hoenn.Count);
        Assert.All(hoenn, n => Assert.True(PokemonPlatinumEngine.Graphics.PokemonModels.HasModel(n), n));
    }

    [Fact]
    public void EverySpeciesOfSinnohIsHandBuilt()
    {
        // Plan 03, decision 3: the 100 Sinnoh species in Platinum's Sinnoh Pokédex (D6–D9) and the seven outside it
        var sinnoh = PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 387 and <= 493).Select(s => s.Name).ToList();
        Assert.Equal(107, sinnoh.Count);
        Assert.All(sinnoh, n => Assert.True(PokemonPlatinumEngine.Graphics.PokemonModels.HasModel(n), n));
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
