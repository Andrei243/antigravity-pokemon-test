using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Sitting and lying (plan 11 · C7; style guide, "Characters", "Stances"): who takes a stance on which furniture, what
/// a file and a script say over it, and that a sitter keeps still and keeps the seat's facing.
/// </summary>
public class StanceTests
{
    private static (Map Map, NPC Npc) Room(PropType furniture, Direction facing = Direction.Down, string? movement = null)
    {
        var map = new Map(12, 12);
        map.Props.Add(new Prop { Type = furniture, X = 4, Y = 4, Width = furniture == PropType.Sofa ? 2 : 1, Depth = furniture == PropType.Bed ? 2 : 1 });
        var npc = new NPC { Name = "Sitter", GridX = 4, GridY = 4, Facing = facing };
        if (movement != null) npc.Movement = PersonMovement.Parse(movement, 1, 1, 4, 4, facing);
        map.NPCs.Add(npc);
        return (map, npc);
    }

    [Theory]
    [InlineData(PropType.Bench, Stance.Sit)]
    [InlineData(PropType.Chair, Stance.Sit)]
    [InlineData(PropType.Sofa, Stance.Sit)]
    [InlineData(PropType.Cushion, Stance.SitGround)]
    [InlineData(PropType.Bed, Stance.Lie)]
    [InlineData(PropType.Table, Stance.Stand)]
    public void SomeonePlacedOnFurnitureTakesItsStanceWithoutBeingTold(PropType furniture, Stance stance)
    {
        var (map, npc) = Room(furniture);
        Assert.Equal(stance, map.StanceOf(npc));
        // Off it, they stand; and on the other tile of a sofa or a bed they are still on it
        npc.GridX = 8;
        Assert.Equal(Stance.Stand, map.StanceOf(npc));
        if (furniture is PropType.Bed)
        {
            npc.GridX = 4;
            npc.GridY = 5;
            Assert.Equal(Stance.Lie, map.StanceOf(npc));
        }
    }

    [Fact]
    public void AScriptsPoseWinsOverTheFileWhichWinsOverTheFurniture()
    {
        var (map, npc) = Room(PropType.Bench);
        npc.Stance = Stance.Stand;
        Assert.Equal(Stance.Stand, map.StanceOf(npc));
        npc.Posed = Stance.Lie;
        Assert.Equal(Stance.Lie, map.StanceOf(npc));
        // Coming to the place again lets a script's pose go, and the file's stance stands
        map.ForgetForced();
        Assert.Null(npc.Posed);
        Assert.Equal(Stance.Stand, map.StanceOf(npc));
        // Someone in the middle of a step over a seat stands
        npc.Stance = null;
        npc.StepOffsetX = 0.5f;
        Assert.Equal(Stance.Stand, map.StanceOf(npc));
    }

    [Fact]
    public void ASitterKeepsStillAndKeepsTheSeatsFacing()
    {
        // Someone who would look about doesn't while seated; the same person standing does
        var (map, npc) = Room(PropType.Bench, Direction.Down, "look_around");
        var wandering = new Wandering();
        var rng = new Random(7);
        for (int frame = 0; frame < 30 * 60; frame++) wandering.Update(1f / 60f, map, (0, 0), (0, 0), rng);
        Assert.Equal((Direction.Down, 4, 4), (npc.Facing, npc.GridX, npc.GridY));

        npc.Stance = Stance.Stand;
        var seen = new System.Collections.Generic.HashSet<Direction>();
        for (int frame = 0; frame < 30 * 60; frame++)
        {
            wandering.Update(1f / 60f, map, (0, 0), (0, 0), rng);
            seen.Add(npc.Facing);
        }
        Assert.True(seen.Count > 1);
    }

    [Fact]
    public void TheFurnitureSaysHowHighItsSeatIs()
    {
        // The props' own texels, 32 to a tile (style guide, "Stances"; PropModels)
        Assert.Equal(14f / 32f, Seats.HeightOf(PropType.Chair));
        Assert.Equal(13f / 32f, Seats.HeightOf(PropType.Sofa));
        Assert.Equal(13f / 32f, Seats.HeightOf(PropType.Bench));
        Assert.Equal(5f / 32f, Seats.HeightOf(PropType.Cushion));
        Assert.Equal(14f / 32f, Seats.HeightOf(PropType.Bed));
        Assert.Null(Seats.StanceOn(PropType.Counter));
    }

    [Fact]
    public void AScriptPosesSomeoneAndThePlayer()
    {
        var map = new Map(12, 12) { Name = "Room" };
        var mom = new NPC { Name = "Mom", Key = "mom", GridX = 3, GridY = 3 };
        map.NPCs.Add(mom);
        var library = ScriptLibrary.FromSources(("test", "script S\n pose mom sitground\n pose player lie\n pose mom stand\n end"), ("common", ""));
        var host = new HeadlessScriptHost { Map = map };
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), null, null);
        runner.RunToEnd();
        Assert.Equal(new[] { "pose Mom SitGround", "pose player Lie", "pose Mom Stand" }, host.Log);
        Assert.Equal(Stance.Stand, mom.Posed);
        Assert.Equal(Stance.Lie, host.PlayerStance);
        // A stance it doesn't know is the parser's error
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script S\n pose mom kneel"));
    }
}
