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

    // ------------------------------------------------------------------ swimming and fishing (plan 11 · C8)

    private static Map Sea()
    {
        var map = new Map(12, 12) { Name = "Sea" };
        for (int x = 0; x < 12; x++)
            for (int y = 6; y < 12; y++) map.SetGroundTile(x, y, TileType.Water);
        return map;
    }

    [Fact]
    public void SomeoneOnOpenDeepWaterSwimsWithoutBeingTold()
    {
        var map = Sea();
        Assert.True(map.IsDeepWater(4, 8));
        var swimmer = new NPC { Name = "Swimmer", GridX = 4, GridY = 8 };
        map.NPCs.Add(swimmer);
        Assert.Equal(Stance.Swim, map.StanceOf(swimmer));
        // Stepping, too
        swimmer.StepOffsetY = 0.5f;
        Assert.Equal(Stance.Swim, map.StanceOf(swimmer));
        swimmer.StepOffsetY = 0f;
        // Not on a deck over it, not wading in a pool, not on land, and not a thing in the water
        Assert.Equal(Stance.Stand, map.StanceOf(new NPC { Name = "Deck", GridX = 4, GridY = 8, Level = 1f }));
        Assert.Equal(Stance.Stand, map.StanceOf(new NPC { Name = "Pool", GridX = 4, GridY = 8, Wading = 6 }));
        Assert.Equal(Stance.Stand, map.StanceOf(new NPC { Name = "Shore", GridX = 4, GridY = 3 }));
        // A swimmer still moves about and turns, as the original's swimmers do
        swimmer.Movement = PersonMovement.Parse("look_around", 0, 0, 4, 8, Direction.Down);
        var wandering = new Wandering();
        var seen = new System.Collections.Generic.HashSet<Direction>();
        var rng = new Random(7);
        for (int frame = 0; frame < 30 * 60; frame++)
        {
            wandering.Update(1f / 60f, map, (0, 0), (0, 0), rng);
            seen.Add(swimmer.Facing);
        }
        Assert.True(seen.Count > 1);
    }

    [Fact]
    public void ARingSpreadsBehindEveryStrokeAtTheSameTimesHoweverTheFramesFall()
    {
        var map = Sea();
        var once = new PokemonPlatinumEngine.Graphics.FieldLife();
        var sliced = new PokemonPlatinumEngine.Graphics.FieldLife();
        // Five seconds of strokes asked for in one go, and in a frame's slices of the same span
        once.Strokes(map, 4.5f, 8.5f, Direction.Down, 0, 5, 0.3f);
        for (int i = 0; i < 300; i++) sliced.Strokes(map, 4.5f, 8.5f, Direction.Down, i / 60.0, (i + 1) / 60.0, 0.3f);
        // One a stroke, each born at its moment: 0.3 + k × 1.2 up to 5 is four
        Assert.Equal(4, once.Count);
        Assert.Equal(once.Count, sliced.Count);
        var a = new System.Collections.Generic.List<PokemonPlatinumEngine.Graphics.LifeQuad>();
        var b = new System.Collections.Generic.List<PokemonPlatinumEngine.Graphics.LifeQuad>();
        once.Advance(4.8f);
        sliced.Advance(4.8f);
        once.Quads(a, new());
        sliced.Quads(b, new());
        Assert.Equal(a, b);
        // Behind the swimmer: north of them, facing south
        Assert.All(a, q => Assert.True(q.At.Z < 8.5f));
        // Indoors, nothing
        var pool = new PokemonPlatinumEngine.Graphics.FieldLife();
        var room = Sea();
        room.Interior = InteriorStyle.House;
        pool.Strokes(room, 4.5f, 8.5f, Direction.Down, 0, 5, 0.3f);
        Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void TheSwimmingAndFishingClipsDoWhatTheStyleGuideSays()
    {
        var player = PokemonPlatinumEngine.Graphics.CharacterModels.Build("PLAYER");
        float shoulders = PokemonPlatinumEngine.Graphics.CharacterSprites.WaterLineHeight(player);
        // The water comes to the shoulders: above the hips, under the eyes
        Assert.True(shoulders > player.Skeleton[PokemonPlatinumEngine.Graphics.HumanBones.Hips].Joint.Y + 0.1f);
        Assert.True(shoulders < player.Face.Anchor.Y);
        // In the frame: a row between the face's and the frame's bottom
        int line = PokemonPlatinumEngine.Graphics.CharacterSprites.WaterLineRow(player);
        Assert.InRange(line, 15, PokemonPlatinumEngine.Graphics.CharacterSprites.SpriteH - 10);

        // A swimmer's arms go round with the stroke: two moments half a stroke apart differ
        System.Numerics.Vector3 HandAt(float t, Stance stance)
        {
            player.Animate(new PokemonPlatinumEngine.Graphics.CharacterPose { Time = t, Stance = stance });
            return player.Skin[PokemonPlatinumEngine.Graphics.HumanBones.HandL].Translation;
        }
        Assert.NotEqual(HandAt(0.1f, Stance.Swim), HandAt(0.1f + Seats.StrokeSeconds / 2f, Stance.Swim));
        // The tug's three frames are three poses, the first the rod held out
        var pulls = Enumerable.Range(0, 3).Select(f => HandAt((f + 0.5f) / 3f * Seats.TugSeconds, Stance.Tug)).ToList();
        Assert.Equal(3, pulls.Distinct().Count());
        Assert.Equal(HandAt(0.5f / 3f * Seats.TugSeconds, Stance.Fish), pulls[0]);

        // The player fishing is their own look holding a rod
        var fishing = PokemonPlatinumEngine.Graphics.CharacterStyle.For(PokemonPlatinumEngine.Graphics.CharacterStyle.Holding("PLAYER", PokemonPlatinumEngine.Graphics.HeldProp.Rod));
        var plain = PokemonPlatinumEngine.Graphics.CharacterStyle.For("PLAYER");
        Assert.Equal(PokemonPlatinumEngine.Graphics.HeldProp.Rod, fishing.Holds);
        Assert.Equal((plain.Top, plain.Hat, plain.Hair), (fishing.Top, fishing.Hat, fishing.Hair));
    }
}
