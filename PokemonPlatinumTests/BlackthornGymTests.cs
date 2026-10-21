using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Blackthorn Gym's slabs on the lava (plan 20 · J11, part c; <see cref="BlackthornPlatforms"/>, HeartGold's
/// <c>src/field/gymmick/blackthorn.c</c>): on a room of lava made here, a slab turned about its middle button, sent its
/// width along each way, bumped back by a wall, refused a turn where its side touches one, the lava refusing a step
/// off the slabs' floors, and everything back where the room begins as the player comes in again.
/// </summary>
public class BlackthornGymTests
{
    /// <summary>A room of lava as wide as the Gym, walled round by plain floor, with whatever else is asked made floor too.</summary>
    private static Map Lava(params (int X, int Y)[] floor)
    {
        var map = new Map(30, 90);
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                bool edge = x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1;
                map.SetBehaviour(x, y, edge ? TileBehavior.None : TileBehavior.Magma);
            }
        foreach (var (x, y) in floor) map.SetBehaviour(x, y, TileBehavior.None);
        return map;
    }

    private static void Play(BlackthornPlatforms slabs)
    {
        for (int i = 0; i < 200 && slabs.Moving != null; i++) slabs.Update(1f / 30f);
        Assert.Null(slabs.Moving);
    }

    [Fact]
    public void TheSlabsBeginWhereTheRoomLaysThemOut()
    {
        var slabs = new BlackthornPlatforms();
        Assert.Equal(BlackthornPlatforms.Layout, slabs.Slabs);
        Assert.IsType<BlackthornPlatforms>(GymPuzzle.Create(BlackthornPlatforms.PuzzleName));
        // Three buttons to a slab: the middle one turns it, the two beside it send it along
        Assert.Equal((BlackthornPlatforms.Action.Rotate, 0), slabs.ActionAt(13, 75));
        Assert.Equal((BlackthornPlatforms.Action.MoveRight, 0), slabs.ActionAt(14, 75));
        Assert.Equal((BlackthornPlatforms.Action.MoveLeft, 0), slabs.ActionAt(12, 75));
        Assert.Equal((BlackthornPlatforms.Action.None, -1), slabs.ActionAt(13, 76));
        // The narrow second slab lies turned a quarter: its buttons run north and south
        Assert.Equal((BlackthornPlatforms.Action.MoveRight, 1), slabs.ActionAt(9, 59));
        Assert.Equal((BlackthornPlatforms.Action.MoveLeft, 1), slabs.ActionAt(9, 57));
    }

    [Fact]
    public void OnlyASlabsFloorCanBeWalkedOverTheLava()
    {
        var map = Lava();
        var slabs = new BlackthornPlatforms();
        // Three wide, from a row before the middle button to four after it
        foreach (var (x, y) in BlackthornPlatforms.FloorOf(0, 13, 75, 0))
            Assert.False(slabs.Refuses(map, x, y, 0f, false), $"{x},{y}");
        Assert.Equal(18, BlackthornPlatforms.FloorOf(0, 13, 75, 0).Count());
        Assert.True(slabs.Refuses(map, 13, 73, 0f, false));
        Assert.True(slabs.Refuses(map, 15, 75, 0f, false));
        Assert.True(slabs.Refuses(map, 13, 81, 0f, false));
        // The floor round the lava is no slab's business
        Assert.False(slabs.Refuses(map, 0, 0, 0f, false));
    }

    [Fact]
    public void ASlabGoesItsWholeWidthAlongAndCarriesItsFloorWithIt()
    {
        var map = Lava();
        var slabs = new BlackthornPlatforms();
        Assert.True(slabs.Press(map, 14, 75));
        var motion = slabs.Moving!;
        Assert.False(motion.Bumped);
        Assert.Equal(5, motion.Tiles);
        Assert.Equal(5 * BlackthornPlatforms.SecondsPerTile, motion.Length, 4);
        // Nothing else can be pressed while it moves
        Assert.False(slabs.Press(map, 9, 58));
        Play(slabs);
        Assert.Equal(new BlackthornPlatforms.Slab(18, 75, 0), slabs.Slabs[0]);
        Assert.False(slabs.Refuses(map, 18, 75, 0f, false));
        Assert.True(slabs.Refuses(map, 13, 75, 0f, false));
        // And back the other way
        Assert.True(slabs.Press(map, 17, 75));
        Play(slabs);
        Assert.Equal(BlackthornPlatforms.Layout[0], slabs.Slabs[0]);
    }

    [Fact]
    public void ASlabThatMeetsSomethingOnItsWayGoesBackToWhereItBegan()
    {
        // A wall three tiles east of the first slab's edge: it gets three tiles along, bumps and goes back
        var map = Lava(Enumerable.Range(70, 12).Select(y => (19, y)).ToArray());
        var slabs = new BlackthornPlatforms();
        Assert.True(slabs.Press(map, 14, 75));
        var motion = slabs.Moving!;
        Assert.True(motion.Bumped);
        Assert.Equal(3, motion.Tiles);
        Assert.False(motion.Refused);
        Assert.True(motion.BumpsAt(0f, motion.Length));
        // Half way through the bump it is back where the wall stopped it, and at the end where it began
        motion.Time = 3 * BlackthornPlatforms.SecondsPerTile;
        Assert.Equal(3f, motion.Along, 3);
        Play(slabs);
        Assert.Equal(BlackthornPlatforms.Layout[0], slabs.Slabs[0]);

        // Right against the wall it goes nowhere at all
        var against = Lava(Enumerable.Range(70, 12).Select(y => (16, y)).ToArray());
        slabs.Reset();
        Assert.True(slabs.Press(against, 14, 75));
        Assert.True(slabs.Moving!.Refused);
        Play(slabs);
        Assert.Equal(BlackthornPlatforms.Layout[0], slabs.Slabs[0]);
    }

    [Fact]
    public void ASlabTurnsAQuarterAboutItsMiddleButtonOrIsRefusedWhereItsSideTouchesSomething()
    {
        var map = Lava();
        var slabs = new BlackthornPlatforms();
        Assert.True(slabs.Press(map, 13, 75));
        Assert.False(slabs.Moving!.Bumped);
        Assert.Equal(BlackthornPlatforms.SecondsPerQuarter, slabs.Moving.Length, 4);
        Play(slabs);
        Assert.Equal(new BlackthornPlatforms.Slab(13, 75, 1), slabs.Slabs[0]);
        // Turned, its floor runs west from the button, and its side buttons are north and south of it
        Assert.False(slabs.Refuses(map, 10, 75, 0f, false));
        Assert.True(slabs.Refuses(map, 13, 78, 0f, false));
        Assert.Equal((BlackthornPlatforms.Action.MoveRight, 0), slabs.ActionAt(13, 76));
        // Four quarters bring it round again
        for (int i = 0; i < 3; i++)
        {
            Assert.True(slabs.Press(map, 13, 75));
            Play(slabs);
        }
        Assert.Equal(BlackthornPlatforms.Layout[0], slabs.Slabs[0]);

        // Floor touching the slab's bottom side refuses the turn outright
        var touching = Lava((13, 80));
        slabs.Reset();
        Assert.True(slabs.Press(touching, 13, 75));
        Assert.True(slabs.Moving!.Refused);
        Assert.Equal(0, slabs.Moving.Reach);
        Play(slabs);
        Assert.Equal(BlackthornPlatforms.Layout[0], slabs.Slabs[0]);
    }

    [Fact]
    public void ComingInAgainLaysTheSlabsOutAfresh()
    {
        var map = Lava();
        var slabs = new BlackthornPlatforms();
        slabs.Press(map, 14, 75);
        Play(slabs);
        slabs.Press(map, 9, 58);
        Play(slabs);
        Assert.NotEqual(BlackthornPlatforms.Layout, slabs.Slabs);
        slabs.Arrive(map, new StoryState(), new Random(0));
        Assert.Equal(BlackthornPlatforms.Layout, slabs.Slabs);
    }
}
