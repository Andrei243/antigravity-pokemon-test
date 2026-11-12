using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Delve;

namespace PokemonPlatinumTests;

/// <summary>
/// The delve's rules (plan 44 · SD7; <c>docs/mechanics/rulings.md</c>, "The delve"): a hollow's floors from their seeds,
/// the turns, the moves on a floor, hunger, recruits, a fallen leader and a whole run played without a window. The
/// Pyramid's floors, which share <see cref="FloorLayout"/>, are held by <c>GoldenPyramidTests</c>.
/// </summary>
public class DelveTests
{
    private static readonly DelveHollow Hollow = DelveHollow.Test;

    private static Pokemon Strong(string species, int level = 30) => new(PokemonDatabase.Get(species)!, level, new DelveRandom(3));

    /// <summary>A run on the test hollow's first floor with nobody else on it, the leader on an open tile with open tiles all round.</summary>
    private static DelveRun Quiet(Pokemon? leader = null, Pokemon? partner = null, uint seed = 11)
    {
        var run = DelveRun.Begin(Hollow, leader ?? Strong("Machop"), partner ?? Strong("Bidoof"), seed);
        run.Residents.Clear();
        run.Finds.Clear();
        run.Snares.Clear();
        var open = OpenSpot(run);
        (run.Leader.X, run.Leader.Y) = open;
        // The partner well away, so it acts on nobody in a test that doesn't want it to
        var far = run.Floor.Grid.Reachable(open, new HashSet<(int, int)>()).OrderByDescending(t => System.Math.Abs(t.X - open.X) + System.Math.Abs(t.Y - open.Y)).ThenBy(t => t.Y).ThenBy(t => t.X).First();
        (run.Team[1].X, run.Team[1].Y) = far;
        run.Events.Clear();
        return run;
    }

    private static (int X, int Y) OpenSpot(DelveRun run)
    {
        for (int y = 0; y < DelveFloor.Height; y++)
            for (int x = 0; x < DelveFloor.Width; x++)
                if (Enumerable.Range(-1, 3).All(dy => Enumerable.Range(-1, 3).All(dx => run.Floor.TileAt(x + dx, y + dy) == DelveTile.Floor))
                    && (x, y) != run.Floor.Stairs && System.Math.Abs(x - run.Floor.Stairs.X) + System.Math.Abs(y - run.Floor.Stairs.Y) > 3)
                    return (x, y);
        throw new System.InvalidOperationException("No open spot.");
    }

    private static DelveMon Place(DelveRun run, Pokemon p, int x, int y, bool onTeam = false)
    {
        var m = new DelveMon(p, onTeam, 100 + run.Residents.Count + run.Team.Count) { X = x, Y = y };
        if (onTeam) run.Team.Add(m);
        else run.Residents.Add(m);
        return m;
    }

    [Fact]
    public void EveryPieceIsOpenAtItsJointsAndOneFloor()
    {
        foreach (var piece in DelveFloor.Pieces)
        {
            Assert.Equal(8, piece.Length);
            Assert.All(piece, row => Assert.Equal(8, row.Length));
            foreach (int i in new[] { 3, 4 })
            {
                Assert.Equal('.', piece[0][i]);
                Assert.Equal('.', piece[7][i]);
                Assert.Equal('.', piece[i][0]);
                Assert.Equal('.', piece[i][7]);
            }
            var open = new HashSet<(int, int)>();
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if (piece[y][x] == '.') open.Add((x, y));
            var seen = new HashSet<(int, int)> { (3, 0) };
            var queue = new Queue<(int X, int Y)>(seen);
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                foreach (var n in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                    if (open.Contains(n) && seen.Add(n)) queue.Enqueue(n);
            }
            Assert.Equal(open.Count, seen.Count);
        }
    }

    [Fact]
    public void AHundredSeedsGiveFloorsWalkedFromTheWayInToTheStairs()
    {
        for (uint seed = 1; seed <= 100; seed++)
            for (int index = 0; index < Hollow.Floors; index++)
            {
                var f = DelveFloor.Generate(Hollow, index, seed * 7919);
                Assert.NotEqual(f.Entrance, f.Stairs);
                var reach = f.Grid.Reachable(f.Entrance, new HashSet<(int, int)>());
                Assert.Contains(f.Stairs, reach);
                // Everything stands on ground a walker reaches, never on the way in, the stairs or another
                var placed = f.Residents.Select(r => (r.X, r.Y)).Concat(f.Finds.Select(t => (t.X, t.Y))).Concat(f.Snares.Select(t => (t.X, t.Y))).ToList();
                Assert.All(placed, t => Assert.Contains(t, reach));
                Assert.Equal(placed.Count, placed.Distinct().Count());
                Assert.DoesNotContain(f.Entrance, placed);
                Assert.DoesNotContain(f.Stairs, placed);
                Assert.Equal(Hollow.ResidentsPerFloor, f.Residents.Count);
                // No pool lies on the walked ground, and a sea cave has some
                Assert.All(reach, t => Assert.Equal(DelveTile.Floor, f.TileAt(t.X, t.Y)));
                if (index == Hollow.Floors - 1) Assert.DoesNotContain(f.Snares, s => s.Kind == SnareKind.Drop);
            }
        Assert.Contains(Enumerable.Range(1, 30), s =>
        {
            var f = DelveFloor.Generate(Hollow, 0, (uint)s);
            return Enumerable.Range(0, DelveFloor.Height).Any(y => Enumerable.Range(0, DelveFloor.Width).Any(x => f.TileAt(x, y) == DelveTile.Pool));
        });
    }

    [Fact]
    public void AFloorIsItsSeedAndWritesAMapFile()
    {
        var a = DelveFloor.Generate(Hollow, 1, 4242).ToFile();
        var b = DelveFloor.Generate(Hollow, 1, 4242).ToFile();
        Assert.Equal(a.Ground, b.Ground);
        Assert.Equal(a.Solid, b.Solid);
        Assert.Equal(DelveFloor.MapName, a.Name);
        Assert.NotEqual(a.Ground, DelveFloor.Generate(Hollow, 1, 4243).ToFile().Ground);
        // The stairs are the run's own rule (SD9), so no script stands on them
        Assert.True(a.Triggers == null || a.Triggers.Count == 0);
        var stairs = DelveFloor.Generate(Hollow, 1, 4242).Stairs;
        Assert.Equal('s', a.Ground[stairs.Y][stairs.X]);
    }

    [Fact]
    public void AnEggALockedHeartAndTheSamePokemonTwiceStayAtTheCamp()
    {
        var egg = Strong("Bidoof");
        egg.IsEgg = true;
        Assert.Equal(DelveRefusal.Egg, DelveRun.WhyNot(egg));
        var locked = Strong("Gible");
        locked.Lock = HeartLock.Locked("lock_test");
        Assert.Equal(DelveRefusal.Locked, DelveRun.WhyNot(locked));
        var one = Strong("Machop");
        Assert.Equal(DelveRefusal.Same, DelveRun.WhyNot(one, one));
        Assert.Null(DelveRun.WhyNot(one, Strong("Bidoof")));
    }

    [Fact]
    public void TheTeamGoesDownAsCopiesAtTheHollowsLevelHoldingNothing()
    {
        var leader = Strong("Machop", 40);
        leader.HeldItem = ItemDatabase.Get("Leftovers");
        var run = DelveRun.Begin(Hollow, leader, Strong("Bidoof"), 5);
        Assert.Equal(Hollow.Level, run.Leader.Pokemon.Level);
        Assert.Null(run.Leader.Pokemon.HeldItem);
        Assert.NotSame(leader, run.Leader.Pokemon);
        Assert.Equal(40, leader.Level);
        Assert.NotNull(leader.HeldItem);
    }

    [Fact]
    public void EveryoneElseActsInSpeedOrder()
    {
        var run = Quiet();
        var (x, y) = (run.Leader.X, run.Leader.Y);
        // Three residents round the leader, of three speeds
        var slow = Place(run, Scenario.Mon("Shuckle", 8), x - 1, y);
        var mid = Place(run, Scenario.Mon("Bidoof", 8), x + 1, y);
        var fast = Place(run, Scenario.Mon("Jolteon", 8), x, y + 1);
        Assert.True(fast.EffectiveSpeed > mid.EffectiveSpeed && mid.EffectiveSpeed > slow.EffectiveSpeed);
        Assert.Null(run.Act(DelveAction.Wait));
        var order = run.Events.Where(e => e.Kind is DelveEventKind.Struck or DelveEventKind.UsedMove).Select(e => e.Who).ToList();
        Assert.Equal(new[] { "Jolteon", "Bidoof", "Shuckle" }, order);
    }

    [Fact]
    public void EachReachFindsItsOwn()
    {
        var run = Quiet();
        var (x, y) = (run.Leader.X, run.Leader.Y);
        var ahead = Place(run, Scenario.Mon("Bidoof", 12), x, y - 1);
        var corner = Place(run, Scenario.Mon("Starly", 12), x + 1, y + 1);
        var room = Place(run, Scenario.Mon("Zubat", 12), x, y + 3 <= DelveFloor.Height ? y + 3 : y - 3);
        var mate = Place(run, Scenario.Mon("Geodude", 12), x - 1, y, onTeam: true);
        var leader = run.Leader;
        List<DelveMon> Of(string move, Direction way) => DelveMoves.TargetsOf(run, leader, MoveDatabase.Get(move), way);

        Assert.Equal(DelveReach.Ahead, DelveMoves.ReachOf(MoveDatabase.Get("Tackle")));
        Assert.Equal(new[] { ahead }, Of("Tackle", Direction.Up));
        Assert.Empty(Of("Tackle", Direction.Right));
        Assert.Equal(DelveReach.Round, DelveMoves.ReachOf(MoveDatabase.Get("Growl")));
        Assert.Equal(new[] { ahead, corner }, Of("Growl", Direction.Up));
        Assert.Equal(DelveReach.Room, DelveMoves.ReachOf(MoveDatabase.Get("Earthquake")));
        Assert.Equal(room.Reach(x, y) <= DelveMoves.RoomReach ? 3 : 2, Of("Earthquake", Direction.Up).Count);
        Assert.Equal(new[] { leader }, Of("Swords Dance", Direction.Up));
        Assert.Equal(DelveReach.None, DelveMoves.ReachOf(MoveDatabase.Get("Rain Dance")));
        Assert.Equal(DelveReach.None, DelveMoves.ReachOf(MoveDatabase.Get("Spikes")));
        Assert.Equal(DelveReach.Ally, DelveMoves.ReachOf(MoveDatabase.Get("Helping Hand")));
        Assert.Equal(new[] { mate }, Of("Helping Hand", Direction.Left));
    }

    [Fact]
    public void AMovesDamageIsTheBattlesOnTheSameDraws()
    {
        var a = Scenario.Mon("Machop", 30, "Karate Chop");
        var d = Scenario.Mon("Bidoof", 30);
        for (uint seed = 1; seed <= 20; seed++)
        {
            var floor = DelveMoves.Damage(a, d, a.Moves[0], new DelveRandom(seed));
            var battle = DamageCalculator.CalculateDamage(a, d, a.Moves[0], new DelveRandom(seed), Ruleset.Modern);
            Assert.Equal(battle.Damage, floor.Damage);
            Assert.Equal(battle.IsCritical, floor.IsCritical);
        }
    }

    [Fact]
    public void ThePlainStrikeLandsOnAGhost()
    {
        var run = Quiet(Scenario.Mon("Machop", 30), Scenario.Mon("Bidoof", 30));
        var ghost = Place(run, Scenario.Mon("Gastly", 30), run.Leader.X, run.Leader.Y - 1);
        int before = ghost.Pokemon.CurrentHP;
        // A Normal move does nothing to it; the plain strike does (unless it misses, so strike until it lands)
        Assert.True(DelveMoves.Damage(run.Leader.Pokemon, ghost.Pokemon, new Move(MoveDatabase.Get("Tackle")), new DelveRandom(1)).IsImmune);
        for (int i = 0; i < 5 && ghost.Pokemon.CurrentHP == before; i++) run.Act(DelveAction.Strike(Direction.Up));
        Assert.True(ghost.Pokemon.CurrentHP < before);
        // And it is the formula's own number: level 12's base damage of power 40 at an effectiveness of one
        var p = run.Leader.Pokemon;
        int baseDamage = PokemonPlatinumEngine.Battle.Sim.Formulas.ModernBaseDamage(p.Level, DelveMoves.StrikePower, p.Attack, ghost.Pokemon.Defense);
        Assert.Equal(PokemonPlatinumEngine.Battle.Sim.Formulas.ModernFinalDamage(baseDamage, 0, 4096, 1f, false, 4096), DelveMoves.StrikeDamage(p, ghost.Pokemon, 0));
    }

    [Fact]
    public void AMoveThatNeedsABattleIsRefusedAndOneOutOfPPToo()
    {
        var run = Quiet(Scenario.Mon("Machop", 30, "Rain Dance", "Karate Chop", "Seismic Toss"), Scenario.Mon("Bidoof", 30));
        Assert.Equal(DelveRefusal.NeedsABattle, run.WhyNot(DelveAction.UseMove(0, Direction.Up)));
        Assert.Null(run.WhyNot(DelveAction.UseMove(1, Direction.Up)));
        Assert.Equal(DelveRefusal.NeedsABattle, run.WhyNot(DelveAction.UseMove(2, Direction.Up)));
        run.Leader.Pokemon.Moves[1].CurrentPP = 0;
        Assert.Equal(DelveRefusal.NoPP, run.WhyNot(DelveAction.UseMove(1, Direction.Up)));
        int turn = run.Turn;
        Assert.Equal(DelveRefusal.NoPP, run.Act(DelveAction.UseMove(1, Direction.Up)));
        Assert.Equal(turn, run.Turn);
    }

    [Fact]
    public void EveryMoveIsRefusedOrDoesSomething()
    {
        var did = new[] { DelveEventKind.Damaged, DelveEventKind.StatusGiven, DelveEventKind.Confused, DelveEventKind.StatChanged, DelveEventKind.Healed };
        foreach (var data in MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.Standard && m.MaxPP > 0).OrderBy(m => m.Id))
        {
            if (!DelveMoves.WorksOnAFloor(data))
            {
                Assert.Equal(DelveRefusal.NeedsABattle, DelveMoves.WhyNot(new DelveMon(Scenario.Mon("Smeargle", 50, data.Name), true, 0), 0));
                continue;
            }
            bool happened = false;
            for (uint seed = 1; seed <= 30 && !happened; seed++)
            {
                var run = Quiet(Scenario.Mon("Smeargle", 50, data.Name), Scenario.Mon("Bidoof", 30), seed);
                var reach = DelveMoves.ReachOf(data);
                // A foe it can touch (a Ghost's moves at a Psychic type, everything else at a Normal one), or a mate
                var target = data.Type == PokemonType.Ghost ? "Mew" : data.Type == PokemonType.Ground ? "Chansey" : "Chansey";
                if (reach == DelveReach.Ally) Place(run, Scenario.Mon("Chansey", 50), run.Leader.X, run.Leader.Y - 1, onTeam: true);
                else Place(run, Scenario.Mon(target, 50), run.Leader.X, run.Leader.Y - 1);
                run.Leader.Pokemon.CurrentHP = run.Leader.Pokemon.MaxHP / 2;
                run.Act(DelveAction.UseMove(0, Direction.Up));
                happened = run.Events.Any(e => did.Contains(e.Kind));
            }
            Assert.True(happened, $"{data.Name} works on a floor and did nothing.");
        }
    }

    [Fact]
    public void HungerFallsEveryTenTurnsAndBitesTheLeaderAtNought()
    {
        var run = Quiet();
        for (int i = 0; i < 10; i++) run.Act(DelveAction.Wait);
        Assert.Equal(DelveRun.FullHunger - 1, run.Hunger);
        run.Hunger = 0;
        int hp = run.Leader.Pokemon.CurrentHP;
        run.Act(DelveAction.Wait);
        Assert.Equal(hp - 1, run.Leader.Pokemon.CurrentHP);
        Assert.Contains(run.Events, e => e.Kind == DelveEventKind.Starving);
        // A berry gives hunger back
        run.Bag.Add("Oran Berry");
        Assert.Null(run.Act(DelveAction.Use("Oran Berry")));
        Assert.Equal(DelveItems.BerryHunger, run.Hunger);
        Assert.Empty(run.Bag);
    }

    [Fact]
    public void AResidentKnockedOutAsksToJoinWithTheRollFixed()
    {
        var run = Quiet();
        var weak = Place(run, Scenario.Mon("Bidoof", 12), run.Leader.X, run.Leader.Y - 1);
        weak.Pokemon.CurrentHP = 1;
        run.FixedRecruitRoll = 0;
        for (int i = 0; i < 5 && run.Asking == null; i++) run.Act(DelveAction.Strike(Direction.Up));
        Assert.Same(weak, run.Asking);
        // Nothing but the answer may be done meanwhile, and the answer is no turn
        Assert.Equal(DelveRefusal.NotNow, run.WhyNot(DelveAction.Wait));
        int turn = run.Turn;
        Assert.Null(run.Act(DelveAction.Answer(true)));
        Assert.Equal(turn, run.Turn);
        Assert.Contains(weak, run.Team);
        Assert.True(weak.Recruit && weak.OnTeam);
        Assert.Equal(weak.Pokemon.MaxHP / 2, weak.Pokemon.CurrentHP);
        // A draw over the chance asks nobody
        Assert.Equal(0, Recruits.ChanceOf(Strong("Mewtwo"), 100));
        run.FixedRecruitRoll = 99;
        var other = Place(run, Scenario.Mon("Starly", 12), run.Leader.X + 1, run.Leader.Y);
        other.Pokemon.CurrentHP = 1;
        for (int i = 0; i < 5 && !other.Fainted; i++) run.Act(DelveAction.Strike(Direction.Right));
        Assert.True(other.Fainted);
        Assert.Null(run.Asking);
    }

    [Fact]
    public void AFallenLeadersRunKeepsNothingUnlessARevivesInTheBag()
    {
        var run = Quiet();
        run.Bag.AddRange(new[] { "Potion", "Revive" });
        run.Hunger = 0;
        run.Leader.Pokemon.CurrentHP = 1;
        run.Act(DelveAction.Wait);
        Assert.Equal(DelveOutcome.Going, run.Outcome);
        Assert.Contains(run.Events, e => e.Kind == DelveEventKind.Revived);
        Assert.Equal(new[] { "Potion" }, run.Bag);
        run.Leader.Pokemon.CurrentHP = 1;
        run.Act(DelveAction.Wait);
        Assert.Equal(DelveOutcome.Fallen, run.Outcome);
        Assert.Empty(run.Bag);
        Assert.Equal(DelveRefusal.NotNow, run.WhyNot(DelveAction.Wait));
    }

    [Fact]
    public void AnEscapeRopeLeavesWithTheBag()
    {
        var run = Quiet();
        run.Bag.AddRange(new[] { "Escape Rope", "Potion" });
        Assert.Null(run.Act(DelveAction.Use("Escape Rope")));
        Assert.Equal(DelveOutcome.Escaped, run.Outcome);
        Assert.Equal(new[] { "Potion" }, run.Bag);
    }

    private static DelveRun PlayToTheEnd(uint seed)
    {
        var run = DelveRun.Begin(Hollow, Strong("Machop"), Strong("Bidoof"), seed);
        for (int guard = 0; guard < 3000 && run.Outcome == DelveOutcome.Going; guard++)
        {
            var action = DelvePolicy.Next(run);
            if (run.Act(action) != null) run.Act(DelveAction.Wait);
        }
        return run;
    }

    [Fact]
    public void ARunIsPlayedToTheLastFloorTheSameTwiceForASeed()
    {
        var cleared = Enumerable.Range(1, 12).Select(s => PlayToTheEnd((uint)s)).ToList();
        Assert.Contains(cleared, r => r.Outcome == DelveOutcome.Cleared);
        var run = cleared.First(r => r.Outcome == DelveOutcome.Cleared);
        Assert.Equal(Hollow.Floors - 1, run.Floor.Index);
        var again = PlayToTheEnd(run.Seed);
        Assert.Equal(run.Outcome, again.Outcome);
        Assert.Equal(run.Turn, again.Turn);
        Assert.Equal(run.Events, again.Events);
        Assert.Equal(run.Bag, again.Bag);
        // Another seed is another run
        Assert.NotEqual(run.Events, PlayToTheEnd(run.Seed + 1000).Events);
    }
}
