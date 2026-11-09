using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW10: an open map's rules drawn as layers (<see cref="OpenDebugLayers"/>), GPU-free, on the open test
/// world: nothing built with none on, each layer's lines its own, nothing beyond the reach round the place, and what
/// each tells of (the cavern's trigger and what it waits for, the house's door and its room, a trainer's cone, a
/// zone and its Pokémon, the chunks' levels, the markers, the navigation mesh's steps and drops and the collision surface).
/// </summary>
public class OpenDebugLayerTests
{
    static Vector3 At(OpenMap map, string marker)
    {
        var p = map.Marker(marker)!.Pos;
        return new Vector3(p.X / 1000f, p.Y / 1000f, p.Z / 1000f);
    }

    [Fact]
    public void NoLayerBuildsNothing()
    {
        var map = OpenWorldPeopleTests.LabWithPeople();
        Assert.True(OpenDebugLayers.Build(map, null, At(map, "cavern_middle"), DebugLayer.None).IsEmpty);
        // The grid's own layers tell nothing of an open map
        Assert.True(OpenDebugLayers.Build(map, null, At(map, "cavern_middle"), DebugLayer.Behaviour | DebugLayer.Height).IsEmpty);
    }

    [Fact]
    public void EachLayerIsItsOwnAndNothingIsBuiltBeyondTheReach()
    {
        var map = OpenWorldPeopleTests.LabWithPeople();
        var field = new OpenField(map, map.Marker("east_mouth")!.Pos, OpenYaw.West);
        for (int i = 0; i < 4 * OpenClock.TicksPerSecond; i++) { field.Player.Step(map, OpenInput.None, field.Grants); field.StepTheRest(); }
        var centre = At(map, "east_mouth");
        foreach (var (name, layer) in DebugLayers.Names)
        {
            var picture = OpenDebugLayers.Build(map, field, centre, layer, (x, z) => 0);
            Assert.All(picture.Lines, l => Assert.Equal(layer, l.Layer));
            Assert.All(picture.Labels, l => Assert.Equal(layer, l.Layer));
        }
        var all = OpenDebugLayers.Build(map, field, centre, DebugLayer.All, (x, z) => (x + z) % 3);
        Assert.False(all.IsEmpty);
        // Lines near the place: the reach, and a trainer's cone or a chunk's border's length past it at most
        float most = (float)OpenDebugLayers.Reach + OpenSight.Longest / 1000f + 32f;
        Assert.All(all.Lines, l => Assert.True(Vector2.Distance(new(l.A.X, l.A.Z), new(centre.X, centre.Z)) <= most, $"{l.Layer} at {l.A}"));
        Assert.All(all.Labels, l => Assert.True(Vector2.Distance(new(l.At.X, l.At.Z), new(centre.X, centre.Z)) <= most, $"{l.Layer} '{l.Text}'"));
        // Every layer of the open world drew something here but the grid's own two
        foreach (var layer in new[] { DebugLayer.Solid, DebugLayer.Nav, DebugLayer.Triggers, DebugLayer.Areas, DebugLayer.Encounters, DebugLayer.Chunks, DebugLayer.Markers })
            Assert.True(all.LinesOf(layer) > 0, $"{layer} drew nothing at the east mouth");
    }

    [Fact]
    public void TheLayersTellWhatTheyShow()
    {
        var map = OpenWorldPeopleTests.LabWithPeople();
        // The cavern's trigger and what it waits for
        var cave = OpenDebugLayers.Build(map, null, At(map, "cavern_middle"), DebugLayer.Triggers);
        Assert.Contains(cave.Labels, l => l.Text == "cavern: Cavern (VAR_LAB_CAVE == 0)");
        Assert.True(cave.Lines.Count >= 16 * 2 + 4);
        // The house's door and the room it leads into
        var house = OpenDebugLayers.Build(map, null, At(map, "house_step"), DebugLayer.Warps | DebugLayer.Solid);
        Assert.Contains(house.Labels, l => l.Text == "house_door -> OpenLabHouse 3,6");
        Assert.Contains(house.Labels, l => l.Text == "the house" && l.Layer == DebugLayer.Solid);
        // Both trainers' cones on the north meadow, and nobody else's
        var twins = OpenDebugLayers.Build(map, null, At(map, "twins_gap"), DebugLayer.Sight);
        Assert.Equal(new[] { "camper_west: 16 m, 40°", "picnicker_east: 16 m, 40°" }, twins.Labels.Select(l => l.Text).Order());
        Assert.Equal(2 * (2 + 12), twins.Lines.Count);
        // The south meadow's zone, its Pokémon over its target, and one standing there with its kind and mood
        var field = new OpenField(map, map.Marker("east_mouth")!.Pos, OpenYaw.West);
        var zone = map.Zones.Single(z => z.Name == "south_meadow");
        field.Wild.Put(map, zone, zone.Slots.Single(s => s.Species == "Bidoof"), 3, OpenPos.FromMetres(24, 0.2, 22), OpenYaw.East);
        var meadow = OpenDebugLayers.Build(map, field, new Vector3(24, 0, 22), DebugLayer.Encounters);
        Assert.Contains(meadow.Labels, l => l.Text.StartsWith("south_meadow: 1 of 2; Starly (Day), Hoothoot (Night), Bidoof"));
        Assert.Contains(meadow.Labels, l => l.Text == "Bidoof 3 calm, roaming");
        // The chunks round the place with the levels they are drawn at
        var chunks = OpenDebugLayers.Build(map, null, new Vector3(0, 0, 0), DebugLayer.Chunks, (x, z) => x == 0 && z == 0 ? 0 : x == -1 ? 1 : null);
        Assert.Contains(chunks.Labels, l => l.Text == "0,0 level 0");
        Assert.Contains(chunks.Labels, l => l.Text == "-1,0 level 1");
        Assert.Contains(chunks.Labels, l => l.Text == "1,1 not drawn");
        Assert.DoesNotContain(chunks.Labels, l => l.Text.StartsWith("2,"));   // the lab's chunks are -2 to 1 each way
        // The markers by name, each a cross, a tick and an upright
        var markers = OpenDebugLayers.Build(map, null, At(map, "house_step"), DebugLayer.Markers);
        Assert.Contains(markers.Labels, l => l.Text == "house_step");
        Assert.Contains(markers.Labels, l => l.Text == "house_path");
        Assert.Equal(4 * markers.Labels.Count, markers.Lines.Count);
    }

    [Fact]
    public void TheNavigationMeshsLayerShowsItsLedgeAsADropAndTheRiverAsSwum()
    {
        var map = OpenWorldPeopleTests.LabWithPeople();
        var nav = map.Land!.Nav!;
        var ledge = OpenDebugLayers.Build(map, null, new Vector3(-48, 0, -40), DebugLayer.Nav);
        Assert.Contains(ledge.Lines, l => l.Colour.Equals(OpenDebugLayers.DropColour));
        var river = OpenDebugLayers.Build(map, null, new Vector3(0, 0, 44), DebugLayer.Nav);
        Assert.Contains(river.Lines, l => l.Colour.Equals(OpenDebugLayers.SwimColour));
        Assert.Contains(river.Lines, l => l.Colour.Equals(OpenDebugLayers.NavColour));
        // The collision surface near the place: walked faces on the meadow, steep ones on the hill's flank
        var flank = OpenDebugLayers.Build(map, null, new Vector3(0, 6, -16), DebugLayer.Solid);
        Assert.Contains(flank.Lines, l => l.Colour.Equals(OpenDebugLayers.WalkedColour));
        Assert.All(flank.Lines.Where(l => !l.Colour.Equals(OpenDebugLayers.SolidColour)),
            l => Assert.True(Vector2.Distance(new(l.A.X, l.A.Z), new(0, -16)) <= OpenDebugLayers.SurfaceReach + 1));
        Assert.NotEmpty(nav.Polygons);
    }
}
