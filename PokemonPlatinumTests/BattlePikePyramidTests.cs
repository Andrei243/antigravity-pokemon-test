using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Pike and the Battle Pyramid (plan 14 · B10; rulings, "The Battle Pike" and "The Battle Pyramid"): the Pike's
/// rooms come in the original's odds over a thousand draws, its strange powers and rests as Emerald's, its round of
/// fourteen rooms; every Pyramid floor walked from its way in to its stairs, a seed giving the same floor twice, its dark
/// that Flash doesn't lift and that every battle won widens, its round of seven floors to the King; and the desks, the
/// doors, the rooms and the floors played by <see cref="HeadlessScriptHost"/>.
/// </summary>
[Collection("MapDatabase")]
public class BattlePikePyramidTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Func<int> Draws(int seed)
    {
        var random = new Random(seed);
        return () => random.Next(0x10000);
    }

    private static Party Three()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 60, new Random(60)));
        party.Add(new Pokemon(PokemonDatabase.Get("Lucario")!, 55, new Random(55)));
        party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 52, new Random(52)));
        return party;
    }

    private static Party Copies() => FrontierRules.TeamFor(Three(), new[] { 0, 1, 2 });

    // ------------------------------------------------------------------ the Pike's rooms of chance

    [Fact]
    public void ThePikesRoomsComeInTheOriginalsOddsOverAThousandDraws()
    {
        var team = Copies();
        var draw = Draws(1);
        var doors = new int[3];
        var hinted = new Dictionary<PikeRoom, int>();
        for (int i = 0; i < 1000; i++)
        {
            var (door, room) = PikeRooms.Hint(queen: false, healingOff: false, team, 1, draw);
            doors[door]++;
            hinted[room] = hinted.GetValueOrDefault(room) + 1;
        }
        // SetHintedRoom: one door of three, and one room of the eight (no Brain), evenly
        Assert.All(doors, n => Assert.InRange(n, 280, 390));
        Assert.Equal(8, hinted.Count);
        Assert.DoesNotContain(PikeRoom.Brain, hinted.Keys);
        Assert.All(hinted.Values, n => Assert.InRange(n, 90, 165));

        // GetNextRoomType: behind the other two doors one of the six whose hint isn't the hinted room's, evenly
        var behind = new Dictionary<PikeRoom, int>();
        for (int i = 0; i < 1000; i++)
        {
            var room = PikeRooms.Next(door: 1, hintedDoor: 0, PikeRoom.SingleBattle, healingOff: false, team, 1, draw);
            behind[room] = behind.GetValueOrDefault(room) + 1;
        }
        Assert.Equal(6, behind.Count);
        Assert.DoesNotContain(PikeRoom.SingleBattle, behind.Keys);
        Assert.DoesNotContain(PikeRoom.HealFull, behind.Keys);
        Assert.All(behind.Values, n => Assert.InRange(n, 120, 215));
        // And the hinted door holds what the hint said
        Assert.Equal(PikeRoom.WildPokemon, PikeRooms.Next(2, 2, PikeRoom.WildPokemon, false, team, 1, draw));
    }

    [Fact]
    public void ARoomTheTeamCouldNotFeelIsLeftOut()
    {
        var team = Copies();
        var draw = Draws(2);
        // The healing rooms off: neither drawn, as a hint or behind another door
        for (int i = 0; i < 300; i++)
        {
            Assert.DoesNotContain(PikeRooms.Hint(false, true, team, 1, draw).Room, new[] { PikeRoom.HealFull, PikeRoom.HealPart });
            Assert.DoesNotContain(PikeRooms.Next(0, 1, PikeRoom.Visitor, true, team, 1, draw), new[] { PikeRoom.HealFull, PikeRoom.HealPart });
        }
        // Two of the three down: no double battle; nobody well: no strange power
        team.Members[0].CurrentHP = 0;
        team.Members[1].CurrentHP = 0;
        team.Members[2].Status = StatusCondition.Burn;
        Assert.False(PikeRooms.AtLeastTwoAlive(team));
        Assert.False(PikeRooms.AtLeastOneHealthy(team, 1));
        for (int i = 0; i < 300; i++)
            Assert.DoesNotContain(PikeRooms.Next(0, 1, PikeRoom.SingleBattle, false, team, 1, draw), new[] { PikeRoom.DoubleBattle, PikeRoom.Status });
    }

    [Fact]
    public void AStrangePowerGivesItsConditionToAsManyAsTheRoomSays()
    {
        // The first four rooms touch one, to the ninth two, then three; a freeze is the Dusclops's
        Assert.Equal((1, 1, 2, 2, 3), (PikeRooms.CountFor(1), PikeRooms.CountFor(4), PikeRooms.CountFor(5), PikeRooms.CountFor(9), PikeRooms.CountFor(10)));
        for (int seed = 0; seed < 60; seed++)
        {
            var team = Copies();
            var done = PikeRooms.InflictStatus(team, 12, Draws(seed));
            Assert.NotNull(done);
            var (status, giver, given) = done!.Value;
            if (status == StatusCondition.Freeze) Assert.Equal("Dusclops", giver);
            if (status is StatusCondition.Toxic or StatusCondition.Paralyze or StatusCondition.Sleep) Assert.Equal("Kirlia", giver);
            Assert.Equal(given, team.Members.Count(p => p.Status == status));
            // A Garchomp is Ground and is never paralysed; a Lucario is Steel and is never badly poisoned
            Assert.False(status == StatusCondition.Paralyze && team.Members[0].Status == StatusCondition.Paralyze);
            Assert.False(status == StatusCondition.Toxic && team.Members[1].Status == StatusCondition.Toxic);
        }
        // Nobody standing well: nothing is given
        var down = Copies();
        foreach (var p in down.Members) p.CurrentHP = 0;
        Assert.Null(PikeRooms.InflictStatus(down, 1, Draws(9)));
    }

    [Fact]
    public void ARestHealsOneOrTwoAndAFullRestAllThree()
    {
        var team = Copies();
        foreach (var p in team.Members) { p.CurrentHP = 1; p.Status = StatusCondition.Burn; }
        int healed = PikeRooms.Heal(team, 2, Draws(4));
        Assert.Equal(2, healed);
        Assert.Equal(2, team.Members.Count(p => p.CurrentHP == p.MaxHP && p.Status == StatusCondition.None));
        // One already whole isn't counted
        var whole = Copies();
        Assert.Equal(0, PikeRooms.Heal(whole, 2, Draws(5)));
        Assert.True(PikeRooms.FullHealth(whole));
    }

    [Fact]
    public void ThePikeQueenComesAtThe28thRoomAndThe140thAndEvery56thAfter()
    {
        Assert.Equal(1, BattlePikeChallenge.QueenFor(28));
        Assert.Equal(2, BattlePikeChallenge.QueenFor(140));
        Assert.Equal(2, BattlePikeChallenge.QueenFor(196));
        Assert.Equal(0, BattlePikeChallenge.QueenFor(27));
        Assert.Equal(0, BattlePikeChallenge.QueenFor(141));

        // With her due every door leads to her, and the door decides how many are healed first (sNumMonsToHealBeforePikeQueen)
        var records = new FrontierRecords();
        records.Pike.Streak = 27;
        var round = BattlePikeChallenge.Begin(records, Three(), new[] { 0, 1, 2 });
        Assert.Equal(PikeHint.Brain, round.Hint(records, Draws(6)));
        Assert.Equal(PikeRoom.Brain, round.Enter(records, 2, Draws(7)));
        var queen = round.Queen(records);
        Assert.Equal(("Pike Queen", 3), (queen.TrainerClass, queen.Party.Count));
        Assert.Equal(50, queen.Party.Members[0].Level);
    }

    [Fact]
    public void ARoundOfFourteenRoomsIsWonAndALossEndsTheStreak()
    {
        var records = new FrontierRecords();
        var round = BattlePikeChallenge.Begin(records, Three(), new[] { 0, 1, 2 });
        Assert.True(records.Pike.InProgress);
        var draw = Draws(8);
        var met = new HashSet<int>();
        for (int room = 1; room <= BattlePikeChallenge.RoundLength; room++)
        {
            Assert.Equal(room, round.Next);
            round.Hint(records, draw);
            var behind = round.Enter(records, room % 3, draw);
            Assert.Equal(behind, round.Current);
            if (behind is PikeRoom.SingleBattle or PikeRoom.HardBattle) Assert.Single(round.Foes);
            if (behind == PikeRoom.DoubleBattle) Assert.Equal(2, round.Foes.Count);
            if (behind == PikeRoom.WildPokemon) Assert.InRange(round.Wild!.Level, 45, 46);
            // Nobody met twice in a round
            foreach (var foe in round.Foes) Assert.True(met.Add(foe.Number));
            Assert.Equal(room == BattlePikeChallenge.RoundLength, round.Passed(records));
        }
        round.Finish(records, won: true);
        Assert.Equal((14, 14, 1, false), (records.Pike.Streak, records.Pike.Best, records.Pike.Rounds, records.Pike.InProgress));
        Assert.Equal(3, round.PointsEarned);

        // A round walked out on ends the streak when the next is begun, and a loss ends it at once
        var walked = BattlePikeChallenge.Begin(records, Three(), new[] { 0, 1, 2 });
        walked.Hint(records, draw);
        walked.Enter(records, 0, draw);
        walked.Passed(records);
        Assert.Equal(15, records.Pike.Streak);
        BattlePikeChallenge.Begin(records, Three(), new[] { 0, 1, 2 }).Finish(records, won: false);
        Assert.Equal((0, 15), (records.Pike.Streak, records.Pike.Best));
    }

    [Fact]
    public void TheThreeStayAsEachRoomLeavesThem()
    {
        var party = Three();
        var round = BattlePikeChallenge.Begin(new FrontierRecords(), party, new[] { 2, 0, 1 });
        // Copies at 50, in the order entered, the party's own untouched
        Assert.Equal(new[] { "Staraptor", "Garchomp", "Lucario" }, round.Team.Members.Select(p => p.Species.Name));
        Assert.All(round.Team.Members, p => Assert.Equal(50, p.Level));
        round.Team.Members[0].CurrentHP = 3;
        Assert.Equal(3, round.Team.Members[0].CurrentHP);
        Assert.Equal(party.Members[2].MaxHP, party.Members[2].CurrentHP);
    }

    // ------------------------------------------------------------------ the Pyramid's floors

    [Fact]
    public void EveryPieceIsOpenAtItsGatesAndOneAreaOfFloor()
    {
        foreach (var piece in PyramidFloor.Pieces)
        {
            Assert.Equal(PyramidFloor.Square, piece.Length);
            Assert.All(piece, row => Assert.Equal(PyramidFloor.Square, row.Length));
            foreach (int g in new[] { 3, 4 })
                Assert.True(piece[0][g] == '.' && piece[7][g] == '.' && piece[g][0] == '.' && piece[g][7] == '.');
            var open = Enumerable.Range(0, 64).Where(i => piece[i / 8][i % 8] == '.').Select(i => (i % 8, i / 8)).ToHashSet();
            var seen = new HashSet<(int, int)> { (3, 0) };
            var queue = new Queue<(int X, int Y)>(seen);
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                foreach (var next in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                    if (open.Contains(next) && seen.Add(next)) queue.Enqueue(next);
            }
            Assert.Equal(open.Count, seen.Count);
        }
    }

    [Fact]
    public void ASeedGivesTheSameFloorTwice()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            var a = PyramidFloor.Generate(seed * 7919, seed % 7);
            var b = PyramidFloor.Generate(seed * 7919, seed % 7);
            Assert.Equal(a.Layout, b.Layout);
            Assert.Equal((a.Entrance, a.Exit, a.Template), (b.Entrance, b.Exit, b.Template));
            Assert.Equal(a.Trainers, b.Trainers);
            Assert.Equal(a.Finds, b.Finds);
            Assert.NotEqual(a.EntranceSquare, a.ExitSquare);
        }
        Assert.NotEqual(PyramidFloor.Generate(1, 0).Layout, PyramidFloor.Generate(2, 0).Layout);

        // And a round's floor made twice is the same map with the same trainers
        var records = new FrontierRecords();
        var round = BattlePyramidChallenge.Begin(records, Three(), new[] { 0, 1, 2 }, Draws(11));
        var first = round.MakeFloor();
        var foes = round.FloorFoes.Select(f => (f.Number, string.Join("/", f.Pokemon.Select(p => p.Species.Name)))).ToList();
        var again = round.MakeFloor();
        Assert.Equal(foes, round.FloorFoes.Select(f => (f.Number, string.Join("/", f.Pokemon.Select(p => p.Species.Name)))));
        for (int y = 0; y < first.Height; y++)
            for (int x = 0; x < first.Width; x++)
                Assert.Equal((first.GetGroundTile(x, y), first.IsSolid(x, y)), (again.GetGroundTile(x, y), again.IsSolid(x, y)));
        Assert.Equal(first.NPCs.Select(n => (n.GridX, n.GridY, n.Facing)), again.NPCs.Select(n => (n.GridX, n.GridY, n.Facing)));
    }

    [Fact]
    public void EveryPyramidFloorIsWalkedFromItsWayInToItsStairs()
    {
        // WorldWalk's way: steps by the field's own rules, with the floor's people standing in the way
        var records = new FrontierRecords();
        for (int r = 0; r < 25; r++)
        {
            var round = BattlePyramidChallenge.Begin(records, Three(), new[] { 0, 1, 2 }, Draws(100 + r));
            for (int floor = 1; floor <= BattlePyramidChallenge.Floors; floor++)
            {
                var map = round.MakeFloor();
                var (sx, sy) = round.Layout!.Entrance;
                var exit = round.Layout.Exit;
                Assert.True(map.IsWalkable(sx, sy), $"floor {floor} of round {r}: the way in is no floor");
                var seen = new HashSet<(int, int)> { (sx, sy) };
                var queue = new Queue<(int X, int Y)>(seen);
                while (queue.Count > 0)
                {
                    var (x, y) = queue.Dequeue();
                    foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
                    {
                        var step = FieldMovement.Step(map, x, y, dir, new Walker());
                        if (!step.Moves || map.GetNpcAt(step.X, step.Y) != null) continue;
                        if (seen.Add((step.X, step.Y))) queue.Enqueue((step.X, step.Y));
                    }
                }
                Assert.True(seen.Contains(exit), $"floor {floor} of round {r}: the stairs at {exit} can't be walked to");
                // The stairs are the floor's trigger, its trainers its people, its finds hidden in it, its wild Pokémon the round's
                Assert.Contains(map.Triggers, t => (t.X, t.Y) == exit && t.Script == "Stairs");
                Assert.Equal(round.Layout.Trainers.Count, map.NPCs.Count(n => n.IsTrainer));
                Assert.Equal(round.Layout.Finds.Count, map.HiddenItems.Count);
                Assert.NotEmpty(map.WildEncounters);
                round.Climb(records);
            }
        }
    }

    [Fact]
    public void AFloorTakesEmeraldsTemplateForItsTrainersAndFinds()
    {
        Assert.Equal(0, PyramidFloor.TemplateFor(0, 39));
        Assert.Equal(3, PyramidFloor.TemplateFor(0, 99));
        Assert.Equal(15, PyramidFloor.TemplateFor(6, 95));
        for (int seed = 0; seed < 30; seed++)
        {
            var floor = PyramidFloor.Generate(seed, seed % 7);
            var (items, trainers) = PyramidFloor.Templates[floor.Template];
            Assert.Equal((items, trainers), (floor.Finds.Count, floor.Trainers.Count));
            // Nobody stands where they'd see the player arrive
            Assert.All(floor.Trainers, t => Assert.True(Math.Max(Math.Abs(t.X - floor.Entrance.X), Math.Abs(t.Y - floor.Entrance.Y)) > PyramidFloor.ClearOfTheWayIn));
        }
    }

    [Fact]
    public void TheDarkIsTheFloorsOwnAndEveryBattleWonWidensIt()
    {
        var round = BattlePyramidChallenge.Begin(new FrontierRecords(), Three(), new[] { 0, 1, 2 }, Draws(12));
        var map = round.MakeFloor();
        // Flash lit nothing here: the dark covers the floor whatever it left in force, and isn't offered
        map.Lit = true;
        Assert.True(Darkness.Covers(map));
        Assert.False(FieldMoveRules.SpotOf(map, round.Layout!.Entrance.X, round.Layout.Entrance.Y, Direction.Up, new Walker()).Dark);
        // Emerald's 32 pixels at the start, four more for each battle won, never past 120
        Assert.Equal(BattlePyramidChallenge.StartLight, round.Light);
        Assert.Equal(BattlePyramidChallenge.ReachOf(32), map.DarkReach, 3);
        round.Brighten();
        Assert.Equal(36, round.Light);
        for (int i = 0; i < 40; i++) round.Brighten();
        Assert.Equal(BattlePyramidChallenge.MostLight, round.Light);
        // A new floor begins dark again
        round.Climb(new FrontierRecords());
        round.MakeFloor();
        Assert.Equal(BattlePyramidChallenge.StartLight, round.Light);
    }

    [Fact]
    public void TheWildPokemonAreOfTheRoundsHabitat()
    {
        for (int r = 0; r < BattlePyramidChallenge.Habitats.Count; r++)
        {
            var table = BattlePyramidChallenge.WildTable(r);
            Assert.Equal(8, table.Count);
            Assert.All(table, e => Assert.Equal(BattlePyramidChallenge.HabitatOf(r), DexEntries.HabitatOf(e.SpeciesName)));
            Assert.All(table, e => Assert.False(PokemonDatabase.Get(e.SpeciesName)!.Legendary));
            Assert.Equal(table.Select(e => e.SpeciesName), BattlePyramidChallenge.WildTable(r).Select(e => e.SpeciesName));
        }
    }

    [Fact]
    public void AFindIsUsedOnWhoeverNeedsIt()
    {
        var round = BattlePyramidChallenge.Begin(new FrontierRecords(), Three(), new[] { 0, 1, 2 }, Draws(13));
        Assert.Null(round.UseFind("Potion"));
        round.Team.Members[1].CurrentHP = 10;
        Assert.Same(round.Team.Members[1], round.UseFind("Potion"));
        Assert.Equal(30, round.Team.Members[1].CurrentHP);
        round.Team.Members[2].CurrentHP = 0;
        Assert.Same(round.Team.Members[2], round.UseFind("Revive"));
        Assert.Equal(round.Team.Members[2].MaxHP / 2, round.Team.Members[2].CurrentHP);
        round.Team.Members[0].Status = StatusCondition.Burn;
        Assert.Same(round.Team.Members[0], round.UseFind("Full Heal"));
    }

    [Fact]
    public void SevenFloorsWinTheRoundAndTheKingWaitsAtTheTwentyFirst()
    {
        Assert.Equal((1, 2, 2, 0), (BattlePyramidChallenge.KingFor(21), BattlePyramidChallenge.KingFor(70), BattlePyramidChallenge.KingFor(105), BattlePyramidChallenge.KingFor(22)));
        var records = new FrontierRecords();
        var round = BattlePyramidChallenge.Begin(records, Three(), new[] { 0, 1, 2 }, Draws(14));
        for (int floor = 1; floor <= 7; floor++)
            Assert.Equal(floor == 7, round.Climb(records));
        round.Finish(records, won: true);
        Assert.Equal((7, 1, false, 3), (records.Pyramid.Streak, records.Pyramid.Rounds, records.Pyramid.InProgress, round.PointsEarned));
        var king = BattlePyramidChallenge.King(1);
        Assert.Equal(("Pyramid King", "Regirock"), (king.TrainerClass, king.Party.Members[0].Species.Name));
    }

    // ------------------------------------------------------------------ the core: a wild Pokémon of the Frontier is run from

    [Fact]
    public void AWildPokemonOfTheFrontierIsRunFromAndATrainerIsForfeited()
    {
        BattleCore Wild()
        {
            var party = new Party();
            party.Add(Of(Gender.Male, "Jolteon", 50, "Thunderbolt"));
            return new BattleCore(new CoreSetup
            {
                PlayerParty = party, WildPokemon = new List<Pokemon> { Of(Gender.Male, "Magikarp", 5, "Splash") }, Random = Calm(),
                Rules = Ruleset.Platinum, PlayerController = null, PlayerName = "Lucas", Frontier = true
            });
        }
        var core = Wild();
        core.Start();
        // Nothing comes out of the bag, so nothing is caught
        Assert.NotNull(core.WhyNot(BattleChoice.UseItem(core.PlayerSlots[0].Place, "Poké Ball")));
        Assert.Null(core.WhyNot(BattleChoice.Run(core.PlayerSlots[0].Place)));
        core.Submit(BattleChoice.Run(core.PlayerSlots[0].Place));
        Assert.Equal(BattleResult.PlayerRan, core.Result);
    }

    // ------------------------------------------------------------------ the desks, the doors, the rooms and the floors

    [Fact]
    public void ThePikesDeskDoorsAndRoomsPlayARoundToItsEnd()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleFrontierAnnex");
        host.PlayerTile = (2, 7);
        runner.Start(ScriptLibrary.Default.Find("PikeDesk", "BattleFrontierAnnex")!, host.Map.Everyone.First(n => n.Key == "pike_receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.NotNull(host.Pike);
        Assert.Contains(host.Log, l => l.StartsWith("warp BattlePike "));

        var hall = Room("BattlePike");
        var room = Room("BattlePikeRoom");
        string[] doors = { "LeftDoor", "MiddleDoor", "RightDoor" };
        for (int n = 0; n < BattlePikeChallenge.RoundLength && host.Pike != null; n++)
        {
            host.Map = hall;
            runner.Start(ScriptLibrary.Default.Find("OnEnter", "BattlePike")!, null);
            runner.RunToEnd();
            runner.Start(ScriptLibrary.Default.Find(doors[n % 3], "BattlePike")!, null);
            runner.RunToEnd();
            host.Map = room;
            runner.Start(ScriptLibrary.Default.Find("OnEnter", "BattlePikeRoom")!, null);
            runner.RunToEnd(20000);
            runner.Start(ScriptLibrary.Default.Find("Exit", "BattlePikeRoom")!, null);
            runner.RunToEnd(20000);
            Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        }
        // Every battle won: fourteen rooms, the round and its Battle Points, and the way back to the lobby
        Assert.Null(host.Pike);
        Assert.Equal((14, 1, false), (host.Frontier.Pike.Streak, host.Frontier.Pike.Rounds, host.Frontier.Pike.InProgress));
        Assert.Equal(3, host.Frontier.BattlePoints);
        Assert.Contains(host.Log, l => l.StartsWith("warp BattleFrontierAnnex"));
    }

    [Fact]
    public void AllThreeDownInAPikeRoomEndsTheRound()
    {
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Lost };
        foreach (var p in Three().Members) host.Party.Add(p);
        var records = host.Frontier;
        host.Pike = BattlePikeChallenge.Begin(records, host.Party, new[] { 0, 1, 2 });
        // A room with a battle behind every door: the hint of a trainer, met behind the hinted door
        var draw = Draws(15);
        for (int guard = 0; guard < 200; guard++)
        {
            host.Pike = BattlePikeChallenge.Begin(records, host.Party, new[] { 0, 1, 2 });
            host.Pike.Hint(records, draw);
            if (host.Pike.HintedRoom == PikeRoom.SingleBattle) break;
        }
        host.Pike!.Enter(records, host.Pike.HintedDoor, draw);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattlePikeRoom");
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "BattlePikeRoom")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Single(host.FrontierBattles);
        Assert.Null(host.Pike);
        Assert.Equal((0, 0, false), (records.Pike.Streak, records.BattlePoints, records.Pike.InProgress));
    }

    [Fact]
    public void ThePyramidsDeskAndStairsClimbSevenFloors()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleFrontierAnnex");
        host.PlayerTile = (12, 7);
        // A warp to the floor goes to the floor the round has laid, as GameEngine.MapNamed has it
        host.MapNamed = name => name == PyramidFloor.MapName && host.Pyramid?.FloorMap is { } floor ? floor : Room(name);
        runner.Start(ScriptLibrary.Default.Find("PyramidDesk", "BattleFrontierAnnex")!, host.Map.Everyone.First(n => n.Key == "pyramid_receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        var round = host.Pyramid!;
        Assert.NotNull(round.FloorMap);
        // The player stands on the floor's way in, not where the warp put them
        Assert.Equal(round.Layout!.Entrance, host.PlayerTile);

        var seen = new List<(int, int)>();
        for (int floor = 1; floor <= BattlePyramidChallenge.Floors; floor++)
        {
            host.Map = round.FloorMap!;
            runner.Start(ScriptLibrary.Default.Find("OnEnter", PyramidFloor.MapName)!, null);
            runner.RunToEnd();
            // A trainer of the floor spoken to battles, and its win widens the light
            var trainer = host.Map.NPCs.FirstOrDefault(n => n.IsTrainer);
            if (trainer != null)
            {
                runner.Start(ScriptLibrary.Default.Find("Trainer", PyramidFloor.MapName)!, trainer);
                runner.RunToEnd();
                Assert.True(trainer.HasBattled);
                Assert.Equal(BattlePyramidChallenge.StartLight + BattlePyramidChallenge.LightStep, round.Light);
            }
            seen.Add(round.Layout!.Exit);
            runner.Start(ScriptLibrary.Default.Find("Stairs", PyramidFloor.MapName)!, null);
            runner.RunToEnd();
            Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        }
        Assert.Null(host.Pyramid);
        Assert.Equal((7, 1, 3), (host.Frontier.Pyramid.Streak, host.Frontier.Pyramid.Rounds, host.Frontier.BattlePoints));
        Assert.Contains(host.Log, l => l.StartsWith("warp BattleFrontierAnnex"));
    }

    [Fact]
    public void TheKingBattlesAtTheTopOfTheTwentyFirstFloor()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        host.Frontier.Pyramid.Streak = 14;
        host.Pyramid = BattlePyramidChallenge.Begin(host.Frontier, host.Party, new[] { 0, 1, 2 }, Draws(16));
        for (int i = 0; i < 6; i++) host.Pyramid.Climb(host.Frontier);
        host.Pyramid.MakeFloor();
        host.Map = host.Pyramid.FloorMap!;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Stairs", PyramidFloor.MapName)!, null);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        var king = Assert.Single(host.FrontierBattles);
        Assert.Equal(BattlePyramidChallenge.KingId, king.Foes[0].Id);
        // The streak of 14 before was two rounds: the third won earns five Battle Points
        Assert.Equal((21, 1, 5), (host.Frontier.Pyramid.Streak, host.Frontier.Pyramid.Rounds, host.Frontier.BattlePoints));
    }

    [Fact]
    public void AWildPokemonMetOnAFloorIsBattledWithTheThree()
    {
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Fled };
        foreach (var p in Three().Members) host.Party.Add(p);
        host.Pyramid = BattlePyramidChallenge.Begin(host.Frontier, host.Party, new[] { 0, 1, 2 }, Draws(17));
        host.Pyramid.MakeFloor();
        host.Pyramid.PendingWild = BattlePyramidChallenge.WildOf(host.Pyramid.FloorMap!.WildEncounters[0], new Random(1));
        host.Map = host.Pyramid.FloorMap;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Wild", PyramidFloor.MapName)!, null);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        var battle = Assert.Single(host.FrontierBattles);
        Assert.Same(host.Pyramid.Team, battle.Team);
        Assert.Empty(battle.Foes);
        Assert.NotNull(host.FrontierFields[0]!.Wild);
        // Run from, the light stays as it was and the round goes on
        Assert.Equal(BattlePyramidChallenge.StartLight, host.Pyramid.Light);
        Assert.Null(host.Pyramid.PendingWild);
    }
}
