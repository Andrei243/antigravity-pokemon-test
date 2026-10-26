using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The field's rules drawn as layers (plan 16 · T18): what each layer builds over a window of a map, worked out with no
/// window, held to the rules it draws.
/// </summary>
public class DebugLayerTests
{
    private static TileWindow Whole(Map map) => new(0, 0, map.Width, map.Height);

    [Fact]
    public void TheSolidLayerIsExactlyTheSolidTiles()
    {
        var map = Fixtures.Map("TwinleafTown");
        var picture = DebugLayers.Build(map, Whole(map), DebugLayer.Solid);
        var drawn = picture.Quads.Select(q => (q.X, q.Y)).ToHashSet();
        var solid = new HashSet<(int, int)>();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.IsSolid(x, y)) solid.Add((x, y));
        Assert.NotEmpty(solid);
        Assert.Equal(solid, drawn);
        Assert.Equal(solid.Count, picture.Quads.Count);
        Assert.All(picture.Quads, q => Assert.Equal(DebugLayer.Solid, q.Layer));
    }

    [Fact]
    public void ATrainersSightIsExactlyTheTilesItSees()
    {
        var map = Fixtures.Map("Route202");
        var trainers = map.NPCs.Where(n => n.IsTrainer && n.TrainerData != null).ToList();
        Assert.NotEmpty(trainers);
        var picture = DebugLayers.Build(map, Whole(map), DebugLayer.Sight);
        var drawn = picture.Quads.Select(q => (q.X, q.Y)).ToHashSet();

        var seen = new HashSet<(int, int)>();
        foreach (var trainer in trainers)
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (TrainerApproach.CanSee(map, trainer, x, y)) seen.Add((x, y));
        Assert.NotEmpty(seen);
        Assert.Equal(seen, drawn);
        // ... and each trainer is named with the range it looks
        foreach (var trainer in trainers)
            Assert.Contains(picture.Labels, l => l.Text == $"{trainer.TrainerData!.Id} sight {trainer.TrainerData.SightRange}");
    }

    [Fact]
    public void ATriggersLabelNamesItsVariableAndValue()
    {
        var map = Fixtures.Map("TwinleafTown");
        map.Triggers.Add(new StepTrigger { X = 4, Y = 5, Width = 2, Depth = 1, Script = "RivalWaits", Variable = "VAR_TWINLEAF_TOWN_STATE", Value = 3 });
        var picture = DebugLayers.Build(map, Whole(map), DebugLayer.Triggers);
        var label = Assert.Single(picture.Labels, l => l.Text.Contains("RivalWaits"));
        Assert.Contains("VAR_TWINLEAF_TOWN_STATE = 3", label.Text);
        Assert.Equal((4.5f, 5.5f), (label.X, label.Z));
        Assert.Contains(picture.Quads, q => (q.X, q.Y) == (4, 5));
        Assert.Contains(picture.Quads, q => (q.X, q.Y) == (5, 5));
    }

    [Fact]
    public void AWarpsLabelSaysWhereItLeadsOrThatItIsShut()
    {
        var map = Fixtures.Map("TwinleafTown");
        map.Warps.Clear();
        map.Warps.Add(new Warp { SourceX = 3, SourceY = 3, TargetMap = "PlayerHouse", TargetX = 4, TargetY = 7 });
        map.Warps.Add(new Warp { SourceX = 6, SourceY = 3, TargetMap = "LakeVerity", TargetX = 1, TargetY = 1, Until = "FLAG_X", Shut = true });
        var picture = DebugLayers.Build(map, Whole(map), DebugLayer.Warps);
        Assert.Contains(picture.Labels, l => l.Text == "-> PlayerHouse 4,7");
        Assert.Contains(picture.Labels, l => l.Text == "shut (until FLAG_X)");
        Assert.Equal(2, picture.Quads.Count);
    }

    [Fact]
    public void NoLayerOnBuildsNothing()
    {
        var map = Fixtures.Map("TwinleafTown");
        Assert.True(DebugLayers.Build(map, Whole(map), DebugLayer.None).IsEmpty);
    }

    [Fact]
    public void TheLayersNamesReadBack()
    {
        Assert.Equal(DebugLayer.Solid | DebugLayer.Sight, DebugLayers.Parse("solid,sight", out _));
        Assert.Equal(DebugLayer.Behaviour, DebugLayers.Parse("behavior", out _));
        Assert.Equal(DebugLayer.Triggers, DebugLayers.Parse("trigger", out _));
        Assert.Equal(DebugLayer.All, DebugLayers.Parse("all", out _));
        Assert.Equal(DebugLayer.None, DebugLayers.Parse("off", out _));
        Assert.Null(DebugLayers.Parse("solid,fog", out var unknown));
        Assert.Equal("fog", unknown);
        foreach (var (name, layer) in DebugLayers.Names)
            Assert.Equal(layer, DebugLayers.Parse(DebugLayers.Describe(layer), out _));
        // The console's and the scenarios' command shares the names
        Assert.Null(DevCommands.Check("layers", new[] { "solid", "warps" }));
        Assert.StartsWith("'fog' is no layer", DevCommands.Check("layers", new[] { "fog" }));
    }

    [Fact]
    public void OnAWindowOfSinnohTheChunkLinesFallOnMultiplesOf32AndNothingIsBuiltOutsideIt()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        Assert.True(sinnoh.IsStreamed);
        var window = new TileWindow(100, 850, 32, 32);
        var picture = DebugLayers.Build(sinnoh, window, DebugLayer.All, (x, y) => x % 2 == 0 ? ChunkState.Ready : ChunkState.Preparing);

        var chunkLines = picture.Lines.Where(l => l.Layer == DebugLayer.Chunks).ToList();
        Assert.NotEmpty(chunkLines);
        foreach (var line in chunkLines)
        {
            if (line.X0 == line.X1) Assert.Equal(0f, line.X0 % 32f);
            else Assert.Equal(0f, line.Z0 % 32f);
        }
        // (the window spans chunks 3 and 4 across and 26 and 27 down: a line at x 128 and one at z 864)
        Assert.Contains(chunkLines, l => l.X0 == 128f && l.X1 == 128f);
        Assert.Contains(chunkLines, l => l.Z0 == 864f && l.Z1 == 864f);
        Assert.Equal(4, picture.Labels.Count(l => l.Layer == DebugLayer.Chunks));

        Assert.All(picture.Quads, q => Assert.True(window.Contains(q.X, q.Y), $"{q.Layer} at {q.X},{q.Y}"));
        Assert.All(picture.Lines, l =>
        {
            Assert.InRange(Math.Min(l.X0, l.X1), window.X, window.Right);
            Assert.InRange(Math.Max(l.X0, l.X1), window.X, window.Right);
            Assert.InRange(Math.Min(l.Z0, l.Z1), window.Y, window.Bottom);
            Assert.InRange(Math.Max(l.Z0, l.Z1), window.Y, window.Bottom);
        });
        Assert.All(picture.Labels, l => Assert.True(window.Contains((int)MathF.Floor(l.X), (int)MathF.Floor(l.Z)), $"{l.Layer} '{l.Text}'"));
    }

    [Fact]
    public void AWindowWiderThanTheLayersDrawIsCutRoundItsMiddle()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        var cut = DebugLayers.Clamp(sinnoh, new TileWindow(0, 0, sinnoh.Width, sinnoh.Height));
        Assert.Equal(DebugLayers.MostTiles, cut.Width);
        Assert.Equal(DebugLayers.MostTiles, cut.Height);
        Assert.Equal((sinnoh.Width - DebugLayers.MostTiles) / 2, cut.X);
    }
}
