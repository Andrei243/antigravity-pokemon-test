using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 35 · BN6, the jobs at the pasture: the nursery hatches an Egg left overnight and brings one back early nearer
/// hatching, takes an Egg and nothing else; the cave's finds are its region's table's and none gives a species; six away
/// at once and a seventh refused; a save of seven is never cut; the orchard's treat; each job's place in the meadow.
/// </summary>
public class PastureJobsTests
{
    private static readonly DateTime Evening = new(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc);

    private static Pokemon Mon(string species, int level = 10) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    private static Pokemon Egg(int cycles)
    {
        var egg = Mon("Togepi", 1);
        egg.IsEgg = true;
        egg.EggCycles = cycles;
        return egg;
    }

    [Fact]
    public void AnEggLeftOvernightComesBackHatchingAndOneBroughtBackEarlyIsNearer()
    {
        var board = new JobBoard();
        var night = board.Post(Egg(40), JobKind.Nursery, 8, Evening)!;
        var early = board.Post(Egg(40), JobKind.Nursery, 8, Evening)!;

        var back = board.Collect(night, Evening.AddHours(8), new Random(1), out var refusal)!;
        Assert.Equal(JobRefusal.None, refusal);
        Assert.True(back.Hatches);
        Assert.True(back.Pokemon.IsEgg);
        Assert.Equal(0, back.Pokemon.EggCycles);
        Assert.Equal(0, back.Exp);
        Assert.Equal(0, back.Effort);

        var soon = board.Collect(early, Evening.AddHours(3), new Random(1), out _)!;
        Assert.False(soon.Hatches);
        Assert.True(soon.Pokemon.IsEgg);
        Assert.Equal(40 - 3 * JobBoard.CyclesPerHour, soon.Pokemon.EggCycles);
        Assert.Equal(3 * JobBoard.CyclesPerHour, soon.EggCycles);
    }

    [Fact]
    public void TheNurseryTakesAnEggAndNothingElseAndNoOtherJobTakesAnEgg()
    {
        var board = new JobBoard();
        Assert.Equal(JobRefusal.NotAnEgg, board.WhyNot(Mon("Bidoof"), 4, JobKind.Nursery));
        Assert.Equal(JobRefusal.None, board.WhyNot(Egg(10), 4, JobKind.Nursery));
        foreach (var kind in new[] { JobKind.Errand, JobKind.Orchard, JobKind.HotSpring, JobKind.Cave })
            Assert.Equal(JobRefusal.Egg, board.WhyNot(Egg(10), 4, kind));
        Assert.Equal(new[] { JobKind.Nursery }, JobsScreen.KindsFor(Egg(10)));
        Assert.DoesNotContain(JobKind.Nursery, JobsScreen.KindsFor(Mon("Bidoof")));
    }

    [Fact]
    public void TheCavesFindsAreItsRegionsTableAndNoneGivesASpecies()
    {
        foreach (string region in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Unova })
        {
            var table = JobBoard.CaveTable(region);
            foreach (string item in table)
            {
                var data = ItemDatabase.Get(item);
                Assert.NotNull(data);
                Assert.DoesNotContain("Fossil", item);
                Assert.DoesNotContain("Old Amber", item);
            }
            var board = new JobBoard();
            var job = board.Post(Mon("Bidoof"), JobKind.Cave, 12, Evening)!;
            var back = board.Collect(job, Evening.AddHours(12), new Random(4), region, out _)!;
            Assert.Equal(12 / JobBoard.HoursPerFind, back.Finds.Sum(f => f.Count));
            Assert.All(back.Finds, f => Assert.Contains(f.Item, table));
        }
        // A Rock, Ground or Steel type finds a little more
        Assert.Equal(12 / JobBoard.HoursPerFind + 12 / JobBoard.HoursPerExtraFind, JobBoard.CaveFinds(Mon("Geodude"), 12));
    }

    [Fact]
    public void SixAreAwayAtOnceASeventhIsRefusedAndASaveOfMoreIsNeverCut()
    {
        var board = new JobBoard();
        for (int i = 0; i < 6; i++) Assert.NotNull(board.Post(Mon("Bidoof"), JobKind.Errand, 2, Evening));
        Assert.Equal(JobRefusal.Full, board.WhyNot(Mon("Starly"), 2, JobKind.Errand));
        var saved = board.ToSaved()!;
        saved.Add(saved[0]);
        var loaded = new JobBoard();
        loaded.Restore(saved);
        Assert.Equal(7, loaded.Jobs.Count);
    }

    [Fact]
    public void TheOrchardBringsATreatEveryEightHoursBesidesItsBerries()
    {
        var board = new JobBoard();
        var job = board.Post(Mon("Bidoof"), JobKind.Orchard, 16, Evening)!;
        var back = board.Collect(job, Evening.AddHours(16), new Random(9), out _)!;
        Assert.Equal(16 / JobBoard.HoursPerBerry(JobKind.Orchard), back.Berries.Sum(b => b.Count));
        Assert.Equal(2, back.Finds.Sum(f => f.Count));
        Assert.All(back.Finds, f => Assert.Contains(f.Item, JobBoard.OrchardTreats));
        Assert.All(JobBoard.OrchardTreats, t => Assert.NotNull(ItemDatabase.Get(t)?.Treat));
    }

    [Fact]
    public void TheOrchardsBerriesAreWhatTheyWereForTheSameSeed()
    {
        // The treat is drawn after the berries, so a harvest under eight hours draws what it always drew
        var board = new JobBoard();
        var job = board.Post(Mon("Bidoof"), JobKind.Orchard, 4, Evening)!;
        var back = board.Collect(job, Evening.AddHours(5), new Random(1), out _)!;
        Assert.Empty(back.Finds);
        Assert.Equal(2, back.Berries.Sum(b => b.Count));
    }

    [Fact]
    public void EachFieldJobHasItsPlaceInTheMeadowAndTheRestAreOutOfSight()
    {
        var map = MapDatabase.Get("SinnohPasture");
        for (int n = 0; n < 6; n++)
        {
            var orchard = Pasture.WorkerPlace(JobKind.Orchard, n)!.Value;
            Assert.Equal(Pasture.OrchardRow, orchard.Y);
            Assert.False(map.IsSolid(orchard.X, orchard.Y));
            Assert.DoesNotContain(orchard, Pasture.OpenTiles(map));
            var bath = Pasture.WorkerPlace(JobKind.HotSpring, n)!.Value;
            Assert.Equal(TileType.Water, map.GetGroundTile(bath.X, bath.Y));
        }
        Assert.Null(Pasture.WorkerPlace(JobKind.Errand, 0));
        Assert.Null(Pasture.WorkerPlace(JobKind.Nursery, 0));
        Assert.Null(Pasture.WorkerPlace(JobKind.Cave, 0));
    }
}
