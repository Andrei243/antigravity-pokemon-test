using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Johto's pieces that wanted looking at (plan 20, part r): Chuck's waterfall, the Gyms' statues by their doors and the
/// Department Store basement's crates, which its Machoke shift about by the day of the week.
/// </summary>
public partial class JohtoStoryTests
{
    [Fact]
    public void ChucksWaterfallFallsUntilTheWinchIsTurnedForTheVisit()
    {
        var game = new Game();
        game.Arrive("Cianwood_Gym", 13, 18);
        var falls = game.Present("cianwood_gym_waterfall");
        Assert.NotNull(falls);
        Assert.True(falls!.IsGymWaterfall);
        Assert.True(falls.IsThing);
        // On the pool behind Chuck, under a flag local to the room, so it falls again the next time the player comes in
        Assert.Equal((13, 7), (falls.GridX, falls.GridY));
        Assert.StartsWith("FLAG_MAP_LOCAL_", falls.HiddenBy);
        game.Read(10, 2);
        Assert.True(game.Story.Has(falls.HiddenBy!));
        game.Map.ApplyPresence(game.Story.Has);
        Assert.Null(game.Present("cianwood_gym_waterfall"));
    }

    [Theory]
    [InlineData("Cianwood_Gym")]
    [InlineData("Mahogany_Gym")]
    public void AJohtoGymsStatuesStandByTheDoorFacingEachOther(string gym)
    {
        var game = new Game();
        game.Arrive(gym, 4, 17);
        var map = game.Map;
        var statues = map.Props.Where(p => p.Type == PropType.Statue && p.Model == "object01").OrderBy(p => p.X).ToList();
        Assert.Equal(2, statues.Count);
        Assert.Equal(statues[0].Y, statues[1].Y);
        // The west one looks east as the figure is drawn, the east one is turned to look west
        Assert.False(Landmarks.GymStatueFacesWest(map, statues[0]));
        Assert.True(Landmarks.GymStatueFacesWest(map, statues[1]));
        // No block of wall stands behind either: the blocked tile the original's statue stands over is floor
        foreach (var s in statues)
        {
            Assert.True(map.IsSolid(s.X, s.Y - 1));
            Assert.NotEqual(TileType.Wall, map.GetGroundTile(s.X, s.Y - 1));
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, false)]
    [InlineData(DayOfWeek.Tuesday, true)]
    [InlineData(DayOfWeek.Wednesday, false)]
    [InlineData(DayOfWeek.Thursday, true)]
    [InlineData(DayOfWeek.Saturday, true)]
    [InlineData(DayOfWeek.Sunday, false)]
    public void TheBasementsMachokeShiftItsCratesByTheDayAndTheWestAndSouthAreOpenOnlyThen(DayOfWeek day, bool shifted)
    {
        var game = new Game { Weekday = day };
        game.Arrive("Goldenrod_DeptStore_Basement", 18, 7);
        var map = game.Map;
        var crates = map.NPCs.Where(n => n.NpcType == NPC.CrateType).ToList();
        Assert.Equal(12, crates.Count);
        Assert.All(crates, c => Assert.Equal(PropType.CratePile, c.GymThing));
        // Each square pushed the way its Machoke faces, or where HeartGold stands them
        var west = game.Present("d37r0103_babyboy1_5_2")!;
        var southEast = game.Present("d37r0103_babyboy1_5_3")!;
        var south = game.Present("d37r0103_babyboy1_5")!;
        Assert.Equal(shifted ? (2, 14) : (3, 14), (west.GridX, west.GridY));
        Assert.Equal(shifted ? (19, 17) : (19, 16), (southEast.GridX, southEast.GridY));
        Assert.Equal(shifted ? (8, 18) : (6, 18), (south.GridX, south.GridY));
        // No two things on one tile, and nothing on a blocked tile
        Assert.Equal(map.NPCs.Count, map.NPCs.Select(n => (n.GridX, n.GridY)).Distinct().Count());
        Assert.All(crates, c => Assert.False(map.IsSolid(c.GridX, c.GridY), $"{c.Key} at {c.GridX},{c.GridY}"));

        // From the lift: every item's ball and the shutter can be come to
        var reached = new HashSet<(int, int)> { (18, 7) };
        var next = new Queue<(int X, int Y)>(reached);
        while (next.Count > 0)
        {
            var (x, y) = next.Dequeue();
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (!map.InBounds(nx, ny) || map.IsSolid(nx, ny) || map.GetNpcAt(nx, ny) != null || !reached.Add((nx, ny))) continue;
                next.Enqueue((nx, ny));
            }
        }
        bool Beside(int x, int y) => reached.Contains((x + 1, y)) || reached.Contains((x - 1, y)) || reached.Contains((x, y + 1)) || reached.Contains((x, y - 1));
        // The shutter every day; the west room's Burn Heal, the south passage's Ether and the east passage's Ultra Ball
        // only once the crates have been shifted out of their ways (where HeartGold's objects stand, the three squares
        // shut the floor's middle off from all three)
        foreach (var ball in map.NPCs.Where(n => n.IsItemBall && n.GridX < 27))
            Assert.True(Beside(ball.GridX, ball.GridY) == shifted, $"{ball.Item} at {ball.GridX},{ball.GridY} on a {day}");
        Assert.True(Beside(26, 8), $"the shutter on a {day}");
        // A pile of crates says what it is
        Assert.NotEmpty(game.Talk(west.Key!).Transcript);
    }
}
