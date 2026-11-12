using System.Numerics;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 30 · FM1: a region's word on movement (<see cref="RegionMovement"/>, its file's <c>movement</c>), a place's laid
/// over it, the options' MOVEMENT row, and the eight-way <see cref="Heading"/> the keys and the stick hold. Nothing here
/// walks a diagonal (FM2): every region before Kalos says nothing and stays on the grid's four ways.
/// </summary>
public class RegionMovementTests
{
    // ----------------------------------------------------------------------------- the region's word

    [Fact]
    public void EveryRegionBeforeKalosSaysNothingAndIsTheGrid()
    {
        foreach (var region in RegionDatabase.All.Where(r => r.Generation < 6))
        {
            Assert.Null(region.File?.Movement);
            Assert.True(region.Movement.IsGrid, region.Id);
            Assert.Equal(MovementDirections.Four, region.Movement.Directions);
            Assert.Equal(CharacterKit.Chibi, region.Kit);
        }
    }

    [Fact]
    public void KalosMovesFreelyOnItsSkatesInTheGrownKit()
    {
        // The gate of plan 30's decision 10, opened by the user on 2026-11-12: free at the stick's tilt, the skates kept,
        // the grown kit
        var kalos = RegionDatabase.Get("Kalos")!;
        Assert.Equal(MovementDirections.Free, kalos.Movement.Directions);
        Assert.Equal(MovementSpeed.Analog, kalos.Movement.Speed);
        Assert.True(kalos.Movement.Offers(RegionMode.Skates));
        Assert.Equal(CharacterKit.Grown, kalos.Kit);
        // Alola keeps eight ways on the grid and the chibi kit for now
        var alola = RegionDatabase.Get("Alola")!;
        Assert.Equal(MovementDirections.Eight, alola.Movement.Directions);
        Assert.Equal(CharacterKit.Chibi, alola.Kit);
    }

    [Fact]
    public void OnlyTheLaterRegionsSayAnythingAndEachModeIsOneRegions()
    {
        foreach (var region in RegionDatabase.All)
            if (!region.Movement.IsGrid || region.Kit != CharacterKit.Chibi)
                Assert.True(region.Generation >= 6, $"{region.Id} says something of movement before generation 6");

        foreach (var mode in Enum.GetValues<RegionMode>())
            Assert.True(RegionDatabase.All.Count(r => r.Movement.Offers(mode)) <= 1, $"{mode} is listed by more than one region");

        // A region whose people skate lists the skates among its modes, and only such a region does
        foreach (var region in RegionDatabase.All)
            Assert.Equal(region.Skates, region.Movement.Offers(RegionMode.Skates));
    }

    [Fact]
    public void ARecordReadsFromItsFileWithEveryFieldLeftOutTheGrids()
    {
        var empty = JsonSerializer.Deserialize<RegionMovement>("{}", GameDataFiles.Json)!;
        Assert.True(empty.IsGrid);
        Assert.Null(empty.Modes);

        var free = JsonSerializer.Deserialize<RegionMovement>("""{ "directions": "Free", "speed": "Analog", "modes": [ "Mount" ] }""", GameDataFiles.Json)!;
        Assert.Equal(MovementDirections.Free, free.Directions);
        Assert.Equal(MovementSpeed.Analog, free.Speed);
        Assert.True(free.Offers(RegionMode.Mount));
        Assert.False(free.Offers(RegionMode.Skates));

        var file = JsonSerializer.Deserialize<RegionFile>("""{ "id": "Test", "kit": "Grown", "movement": { "directions": "Eight" } }""", GameDataFiles.Json)!;
        Assert.Equal(MovementDirections.Eight, file.Movement!.Directions);
        Assert.Equal(CharacterKit.Grown, file.Kit);
    }

    [Fact]
    public void APlacesRecordWinsForThePlace()
    {
        var region = new RegionMovement { Directions = MovementDirections.Free, Speed = MovementSpeed.Analog, Modes = new() { RegionMode.Skates } };

        Assert.Same(region, region.Under(null));

        // A Gym puzzle kept on the grid: four ways, the region's modes kept (the place lists none)
        var gym = JsonSerializer.Deserialize<WorldMapEntry>("""{ "name": "Gym", "movement": { "directions": "Four" } }""", GameDataFiles.Json)!;
        var here = region.Under(gym.Movement);
        Assert.Equal(MovementDirections.Four, here.Directions);
        Assert.Equal(MovementSpeed.Paces, here.Speed);
        Assert.True(here.Offers(RegionMode.Skates));

        // A room where the modes are barred: an empty list
        var room = region.Under(new RegionMovement { Directions = MovementDirections.Free, Speed = MovementSpeed.Analog, Modes = new() });
        Assert.Equal(MovementDirections.Free, room.Directions);
        Assert.False(room.Offers(RegionMode.Skates));
    }

    [Fact]
    public void NoPlaceOfTheWorldsSaysAnythingYet()
    {
        foreach (var folder in RegionDatabase.All.Select(r => r.Folder).Distinct())
        {
            string path = $"world/{folder}/world.json";
            if (!File.Exists(GameDataFiles.PathOf(path))) continue;
            var index = GameDataFiles.Load<WorldIndexFile>(path);
            Assert.All(index.Maps, m => Assert.Null(m.Movement));
        }
    }

    // ----------------------------------------------------------------------------- the options' row

    [Fact]
    public void TheRowFollowsTheRegionUnlessChosenAndStandsBeforeControls()
    {
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(Array.IndexOf(rows, OptionRow.WildPokemon) + 1, Array.IndexOf(rows, OptionRow.Movement));
        Assert.Equal(Array.IndexOf(rows, OptionRow.Movement) + 1, Array.IndexOf(rows, OptionRow.Voices));
        Assert.DoesNotContain(nameof(OptionRow.Movement), FirstStartScreen.Wanted);

        var s = new GameSettings();
        Assert.Equal(MovementChoice.AsTheRegion, s.Movement);
        OptionsScreen.Change(s, OptionRow.Movement, 1);
        Assert.Equal(MovementChoice.FourWays, s.Movement);
        OptionsScreen.Change(s, OptionRow.Movement, -1);
        Assert.Equal(MovementChoice.AsTheRegion, s.Movement);

        s.Movement = MovementChoice.FourWays;
        s.ResetToDefaults();
        Assert.Equal(MovementChoice.AsTheRegion, s.Movement);
    }

    [Fact]
    public void FourWaysMakesEveryRegionTheGridsAndKeepsItsModes()
    {
        var kalos = RegionDatabase.Get("Kalos")!.Movement;
        Assert.Same(kalos, kalos.AsChosen(MovementChoice.AsTheRegion));
        var four = kalos.AsChosen(MovementChoice.FourWays);
        Assert.Equal(MovementDirections.Four, four.Directions);
        Assert.Equal(MovementSpeed.Paces, four.Speed);
        Assert.True(four.Offers(RegionMode.Skates));

        foreach (var region in RegionDatabase.All.Where(r => r.Generation < 6))
            foreach (var choice in Enum.GetValues<MovementChoice>())
                Assert.Equal(MovementDirections.Four, region.Movement.AsChosen(choice).Directions);
    }

    // ----------------------------------------------------------------------------- the heading

    [Fact]
    public void AHeadingsStepPartsAndAngleAgreeWithTheGrid()
    {
        foreach (var direction in Enum.GetValues<Direction>())
        {
            var heading = Headings.Of(direction);
            Assert.False(heading.IsDiagonal());
            Assert.Equal(direction, heading.Cardinal());
            var (dx, dy) = FieldMovement.Delta(direction);
            Assert.Equal((dx, dy), heading.Delta());
        }

        Assert.Equal(MathF.PI, Heading.N.Yaw(), 4);
        Assert.Equal(0f, Heading.S.Yaw(), 4);
        Assert.Equal(MathF.PI / 2f, Heading.E.Yaw(), 4);
        Assert.Equal(-MathF.PI / 2f, Heading.W.Yaw(), 4);
        Assert.Equal(MathF.PI / 4f, Heading.SE.Yaw(), 4);

        foreach (var heading in Enum.GetValues<Heading>().Where(h => h.IsDiagonal()))
        {
            Assert.Null(heading.Cardinal());
            var (vertical, horizontal) = heading.Parts();
            var (vx, vy) = FieldMovement.Delta(vertical);
            var (hx, hy) = FieldMovement.Delta(horizontal);
            Assert.Equal((vx + hx, vy + hy), heading.Delta());
        }
    }

    [Fact]
    public void TheFourWayAnswerIsTheOneThePlayerHasAlwaysWalked()
    {
        // The player's steering before FM1: the stronger axis, a tie the vertical one
        static Direction? Old(Vector2 dir) => dir == Vector2.Zero ? null
            : Math.Abs(dir.X) > Math.Abs(dir.Y) ? (dir.X > 0 ? Direction.Right : Direction.Left) : (dir.Y > 0 ? Direction.Down : Direction.Up);

        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                var v = new Vector2(x, y);
                Assert.Equal(Old(v), Headings.FourWay(v));
                Assert.Equal(x == 0 && y == 0, Headings.FromVector(v) == null);
            }
        Assert.Equal(Direction.Up, Headings.FourWay(new Vector2(1, -1)));
        Assert.Equal(Heading.NE, Headings.FromVector(new Vector2(1, -1)));
    }

    [Fact]
    public void TheStickAndTheKeysHoldEightWaysInTheBitsARecordingKeeps()
    {
        var stick = new StickReader();
        Assert.Equal(Heading.NE, Headings.FromStick(stick.Update(0.7f, -0.7f, 0.35f, out _)));
        Assert.Equal(Heading.E, Headings.FromStick(stick.Update(0.9f, -0.2f, 0.35f, out _)));
        Assert.Equal(Heading.SW, Headings.FromStick(stick.Update(-0.6f, 0.5f, 0.35f, out _)));
        Assert.Null(Headings.FromStick(stick.Update(0.05f, 0.05f, 0.35f, out _)));
        Assert.Null(Headings.FromStick(StickDirs.Left | StickDirs.Right));

        // The stick through the binding set: its diagonal is two direction bits, which is all a movie keeps
        var bindings = InputBindings.For(ButtonMode.Normal);
        var held = new StickReader().Update(-0.7f, -0.7f, 0.35f, out var pressed);
        var frame = bindings.Resolve(_ => false, _ => false, PadSnapshot.Pushed(-0.7f, -0.7f), held, pressed);
        Assert.Equal(Heading.NW, Headings.Of(frame));

        // The keyboard's two keys
        var keys = new HashSet<KeyboardKey> { KeyboardKey.Down, KeyboardKey.D };
        Assert.Equal(Heading.SE, Headings.Of(bindings.Resolve(keys.Contains, _ => false, PadSnapshot.None, StickDirs.None, StickDirs.None)));

        // And through the game's own reads, fed as a recording feeds them
        InputManager.Feed(ActionFrame.Press(GameAction.Up, GameAction.Left));
        try
        {
            Assert.Equal(Heading.NW, InputManager.GetHeading());
            Assert.Equal(Direction.Up, Headings.FourWay(InputManager.GetMovementVector()));
        }
        finally { InputManager.Feed(null); }
    }
}
