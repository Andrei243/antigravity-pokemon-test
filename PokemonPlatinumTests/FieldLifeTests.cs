using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// What moves in the field (plan 04 · G9; style guide, "Life"): what a step leaves behind, rain landing, doors,
/// the weather over the picture and in the light, the bubbles, and the ways into a battle. All of it is rules
/// and pixel art that run without a window.
/// </summary>
public class FieldLifeTests
{
    private const float Frame = 1f / 60f;

    private static Map Lawn(int width = 9, int height = 9) => new(width, height) { Name = "Lab" };

    private static void Run(FieldLife life, float seconds)
    {
        for (float t = 0f; t < seconds - 0.0001f; t += Frame) life.Advance(Frame);
    }

    private static (List<LifeQuad> Flat, List<LifeQuad> Upright) Quads(FieldLife life)
    {
        var flat = new List<LifeQuad>();
        var upright = new List<LifeQuad>();
        life.Quads(flat, upright);
        return (flat, upright);
    }

    private static bool IsCell(LifeQuad quad, int first, int frames) => quad.Cell >= first && quad.Cell < first + frames;

    /// <summary>The numbers the style guide gives for all this ("Life"): change them there first.</summary>
    [Fact]
    public void TheNumbersAreTheStyleGuides()
    {
        // What a step leaves, and for how long
        Assert.Equal((3f, 3f), (FieldLife.PrintStays, FieldLife.PrintFades));
        Assert.Equal((0.36f, 0.5f, 0.7f), (FieldLife.PuffTime, FieldLife.LeafTime, FieldLife.RingTime));
        static (int, int, int) Rgb(Color c) => (c.R, c.G, c.B);
        Assert.Equal((214, 196, 150), Rgb(FieldLife.DustOf(TileType.Path)));
        Assert.Equal((232, 218, 170), Rgb(FieldLife.DustOf(TileType.Sand)));
        Assert.Equal((170, 134, 98), Rgb(FieldLife.DustOf(TileType.Dirt)));
        Assert.Equal((214, 226, 244), Rgb(FieldLife.DustOf(TileType.Snow)));
        Assert.Equal((200, 196, 190), Rgb(FieldLife.DustOf(TileType.Rock)));

        Color First(TileBehavior behaviour)
        {
            var map = Lawn();
            map.SetBehaviour(4, 4, behaviour);
            var life = new FieldLife();
            life.Footstep(map, 4, 4, Direction.Right, false, TravelMode.OnFoot);
            var (flat, upright) = Quads(life);
            return flat.Count > 0 ? flat[0].Tint : upright[0].Tint;
        }
        Assert.Equal((196, 170, 118), Rgb(First(TileBehavior.Sand)));
        Assert.Equal((140, 160, 206), Rgb(First(TileBehavior.ShallowSnow)));
        Assert.Equal((168, 226, 112), Rgb(First(TileBehavior.TallGrass)));
        Assert.Equal((210, 236, 255), Rgb(First(TileBehavior.ShallowWater)));

        // The weather: how much is in the air, how much lands, how much sun gets through
        var inTheAir = new Dictionary<FieldWeather, int>
        {
            [FieldWeather.Clear] = 0, [FieldWeather.Cloudy] = 0, [FieldWeather.Rain] = 220, [FieldWeather.HeavyRain] = 380,
            [FieldWeather.Thunderstorm] = 380, [FieldWeather.Snow] = 140, [FieldWeather.HeavySnow] = 320, [FieldWeather.Blizzard] = 460,
            [FieldWeather.Hail] = 180, [FieldWeather.Fog] = 0, [FieldWeather.Sandstorm] = 240, [FieldWeather.Ash] = 110
        };
        var sun = new Dictionary<FieldWeather, float>
        {
            [FieldWeather.Clear] = 1f, [FieldWeather.Cloudy] = 0.45f, [FieldWeather.Rain] = 0.3f, [FieldWeather.HeavyRain] = 0.22f,
            [FieldWeather.Thunderstorm] = 0.2f, [FieldWeather.Snow] = 0.6f, [FieldWeather.HeavySnow] = 0.35f, [FieldWeather.Blizzard] = 0.25f,
            [FieldWeather.Hail] = 0.4f, [FieldWeather.Fog] = 0.5f, [FieldWeather.Sandstorm] = 0.55f, [FieldWeather.Ash] = 0.75f
        };
        foreach (var weather in AllWeather)
        {
            Assert.Equal(inTheAir[weather], WeatherFx.CountOf(weather));
            Assert.Equal(sun[weather], ArtLook.SunThrough(weather));
        }
        Assert.Equal(360f, Weathers.LandsABeat(FieldWeather.Rain) * FieldLife.RainBeats);
        Assert.Equal(720f, Weathers.LandsABeat(FieldWeather.HeavyRain) * FieldLife.RainBeats);
        Assert.Equal(720f, Weathers.LandsABeat(FieldWeather.Thunderstorm) * FieldLife.RainBeats);
        Assert.Equal(180f, Weathers.LandsABeat(FieldWeather.Hail) * FieldLife.RainBeats);
        Assert.Equal((176, 200, 236), Rgb(Blocks(FieldWeather.Rain, 1f)[0].Color));
        Assert.Equal((224, 238, 255), Rgb(Blocks(FieldWeather.Hail, 1f)[0].Color));
        Assert.Equal((226, 198, 140), Rgb(Layers(FieldWeather.Sandstorm, 1f)[0].Tint));
        Assert.Equal((232, 236, 244), Rgb(Layers(FieldWeather.Fog, 1f)[0].Tint));

        // Doors, bubbles and what moves in place
        Assert.Equal((0.2f, 0.4f), (FieldLife.DoorOpens, FieldLife.DoorLingers));
        Assert.Equal((20, 22), (LifeArt.BubbleWidth, LifeArt.BubbleHeight));
        var npc = new NPC();
        npc.ShowBubble(EmoteBubble.Dots);
        Assert.Equal(0.9f, npc.BubbleTimer);
        Assert.Equal(4, Landmarks.MovingFrames);

        // Into battle
        Assert.Equal((0.25f, 0.9f, 0.5f), (SceneTransition.FlashSeconds, SceneTransition.CloseSeconds, SceneTransition.OpenSeconds));
        Assert.Equal((14, 14, 22), Rgb(SceneTransition.Dark));
    }

    // ------------------------------------------------------------------ what a step leaves

    [Theory]
    [InlineData(TileType.Sand)]
    [InlineData(TileType.Snow)]
    public void AStepInSandOrSnowLeavesAPrintThatStaysFadesAndGoes(TileType ground)
    {
        var map = Lawn();
        map.SetGroundTile(4, 4, ground);
        var life = new FieldLife();
        life.Footstep(map, 4, 4, Direction.Right, running: false, TravelMode.OnFoot);

        var (flat, upright) = Quads(life);
        Assert.Empty(upright);
        var print = Assert.Single(flat);
        Assert.Equal(FieldLife.Print, print.Cell);
        Assert.InRange(print.At.X, 4f, 5f);
        Assert.InRange(print.At.Z, 4f, 5f);
        byte fresh = print.Tint.A;

        // It is still there, as it was, just before its three seconds are up
        Run(life, FieldLife.PrintStays - 0.1f);
        Assert.Equal(fresh, Quads(life).Flat[0].Tint.A);

        // Then it fades in two steps, each fainter than the last, and is gone
        Run(life, 0.2f);
        byte first = Quads(life).Flat[0].Tint.A;
        Run(life, FieldLife.PrintFades / 2f);
        byte second = Quads(life).Flat[0].Tint.A;
        Assert.True(fresh > first && first > second && second > 0, $"{fresh} > {first} > {second}");
        Run(life, FieldLife.PrintFades / 2f);
        Assert.Equal(0, life.Count);
    }

    [Fact]
    public void FootprintsAreKeptByTheGroundThatKeepsThemInPlatinum()
    {
        var keeps = new[]
        {
            TileBehavior.Sand, TileBehavior.ShallowSnow, TileBehavior.ShadedSnow, TileBehavior.DeepSnow, TileBehavior.DeeperSnow, TileBehavior.DeepestSnow
        };
        foreach (var behaviour in Enum.GetValues<TileBehavior>())
        {
            var map = Lawn();
            map.SetBehaviour(4, 4, behaviour);
            var life = new FieldLife();
            life.Footstep(map, 4, 4, Direction.Right, running: false, TravelMode.OnFoot);
            bool printed = Quads(life).Flat.Any(q => q.Cell == FieldLife.Print);
            Assert.Equal(keeps.Contains(behaviour), printed);
            Assert.Equal(keeps.Contains(behaviour), TileBehaviors.KeepsFootprints(behaviour));
        }

        // Sand's prints are sand-coloured and snow's are blue shade, whatever the ground is painted as
        Color PrintOn(TileBehavior behaviour)
        {
            var map = Lawn();
            map.SetBehaviour(4, 4, behaviour);
            var life = new FieldLife();
            life.Footstep(map, 4, 4, Direction.Right, false, TravelMode.OnFoot);
            return Quads(life).Flat[0].Tint;
        }
        Assert.True(PrintOn(TileBehavior.Sand).R > PrintOn(TileBehavior.Sand).B);
        Assert.True(PrintOn(TileBehavior.ShadedSnow).B > PrintOn(TileBehavior.ShadedSnow).R);
        Assert.Equal(PrintOn(TileBehavior.ShadedSnow), PrintOn(TileBehavior.DeepSnow));
    }

    [Fact]
    public void PrintsFallLeftAndRightOfTheLineWalkedAndPointTheWayItWent()
    {
        var map = Lawn();
        for (int x = 0; x < 9; x++)
            for (int y = 0; y < 9; y++)
                map.SetGroundTile(x, y, TileType.Sand);
        var life = new FieldLife();
        life.Footstep(map, 3, 4, Direction.Right, false, TravelMode.OnFoot);
        life.Footstep(map, 4, 4, Direction.Right, false, TravelMode.OnFoot);
        var east = Quads(life).Flat;
        Assert.Equal(2, east.Count);
        Assert.All(east, p => Assert.Equal(1, p.Turn));
        // One north of the middle of the row, one south of it; neither off the line the other way
        Assert.True((east[0].At.Z - 4.5f) * (east[1].At.Z - 4.5f) < 0f);
        Assert.Equal(3.5f, east[0].At.X);
        Assert.Equal(4.5f, east[1].At.X);

        life.Clear();
        life.Footstep(map, 4, 3, Direction.Up, false, TravelMode.OnFoot);
        life.Footstep(map, 4, 2, Direction.Up, false, TravelMode.OnFoot);
        var north = Quads(life).Flat;
        Assert.All(north, p => Assert.Equal(0, p.Turn));
        Assert.True((north[0].At.X - 4.5f) * (north[1].At.X - 4.5f) < 0f);

        life.Clear();
        life.Footstep(map, 4, 5, Direction.Down, false, TravelMode.OnFoot);
        life.Footstep(map, 3, 5, Direction.Left, false, TravelMode.OnFoot);
        Assert.Equal(new[] { 2, 3 }, Quads(life).Flat.Select(p => p.Turn));
    }

    [Fact]
    public void ARunRaisesDustBehindTheStepAndAWalkRaisesNone()
    {
        var map = Lawn();
        map.SetGroundTile(4, 4, TileType.Path);
        var life = new FieldLife();
        life.Footstep(map, 4, 4, Direction.Right, running: false, TravelMode.OnFoot);
        Assert.Equal(0, life.Count);

        life.Footstep(map, 4, 4, Direction.Right, running: true, TravelMode.OnFoot);
        var (flat, upright) = Quads(life);
        Assert.Empty(flat);
        var puff = Assert.Single(upright);
        Assert.True(IsCell(puff, FieldLife.Puff, 3));
        Assert.True(puff.At.X < 4.5f, "the dust is behind whoever raised it");
        var dust = FieldLife.DustOf(TileType.Path);
        Assert.Equal((dust.R, dust.G, dust.B), (puff.Tint.R, puff.Tint.G, puff.Tint.B));

        // It swells through its three frames, rising a texel with each, and is gone in a third of a second
        Run(life, FieldLife.PuffTime * 0.5f);
        var later = Quads(life).Upright[0];
        Assert.True(later.Cell > puff.Cell && later.At.Y > puff.At.Y);
        Run(life, FieldLife.PuffTime * 0.6f);
        Assert.Equal(0, life.Count);
    }

    [Fact]
    public void OnlyBareGroundGivesOffDustAndEachKindItsOwnColour()
    {
        var map = Lawn();
        var life = new FieldLife();
        life.Footstep(map, 4, 4, Direction.Right, running: true, TravelMode.OnFoot);
        Assert.Equal(0, life.Count);   // a lawn

        var colours = new[] { TileType.Path, TileType.Sand, TileType.Dirt, TileType.Snow }.Select(FieldLife.DustOf).ToList();
        Assert.Equal(colours.Count, colours.Select(c => (c.R, c.G, c.B)).Distinct().Count());
    }

    [Fact]
    public void TallGrassThrowsUpFourLeavesThatFanOutAndComeDown()
    {
        var map = Lawn();
        map.SetGroundTile(4, 4, TileType.TallGrass);
        Assert.True(map.IsTallGrass(4, 4));
        var life = new FieldLife();
        // Running or not: grass gives leaves, never dust
        life.Footstep(map, 4, 4, Direction.Right, running: true, TravelMode.OnFoot);
        var (flat, leaves) = Quads(life);
        Assert.Empty(flat);
        Assert.Equal(4, leaves.Count);
        Assert.All(leaves, l => Assert.True(IsCell(l, FieldLife.Leaf, 2)));
        Assert.Equal(2, leaves.Count(l => l.At.X < 4.5f));
        Assert.Equal(2, leaves.Count(l => l.At.X > 4.5f));
        Assert.Equal(2, leaves.Select(l => (l.Tint.R, l.Tint.G, l.Tint.B)).Distinct().Count());

        // At the top of their arcs the inner pair are higher than the outer pair, and all have flown further out
        Run(life, FieldLife.LeafTime / 2f);
        var top = Quads(life).Upright.OrderBy(l => MathF.Abs(l.At.X - 4.5f)).ToList();
        Assert.True(top[0].At.Y > top[3].At.Y && top[1].At.Y > top[2].At.Y);
        var startSpread = leaves.Max(l => MathF.Abs(l.At.X - 4.5f));
        Assert.True(MathF.Abs(top[3].At.X - 4.5f) > startSpread);
        Assert.All(top, l => Assert.True(l.At.Y > leaves[0].At.Y));

        Run(life, FieldLife.LeafTime / 2f + 0.05f);
        Assert.Equal(0, life.Count);
    }

    /// <summary>A pond in the middle of a lawn: water at (3..5, 3..5).</summary>
    private static Map Pond()
    {
        var map = Lawn();
        for (int y = 3; y <= 5; y++)
            for (int x = 3; x <= 5; x++)
                map.SetGroundTile(x, y, TileType.Water);
        return map;
    }

    [Fact]
    public void ASwimmerLeavesARingOnTheWaterBehindThem()
    {
        var map = Pond();
        var life = new FieldLife();
        life.Footstep(map, 5, 4, Direction.Right, false, TravelMode.Surfing);
        var (flat, upright) = Quads(life);
        Assert.Empty(upright);
        var ring = Assert.Single(flat);
        Assert.Equal(FieldLife.Ring, ring.Cell);
        Assert.Equal((4.5f, 4.5f), (ring.At.X, ring.At.Z));

        // It widens through three frames as it fades, and is gone
        Run(life, FieldLife.RingTime * 0.8f);
        var wide = Quads(life).Flat[0];
        Assert.True(wide.Cell == FieldLife.Ring + 2 && wide.Width > ring.Width && wide.Tint.A < ring.Tint.A);
        Run(life, FieldLife.RingTime * 0.3f);
        Assert.Equal(0, life.Count);
    }

    [Fact]
    public void WaterIsThrownUpByAPuddleAndMoreByRidingOutOntoIt()
    {
        var map = Pond();
        map.SetBehaviour(1, 1, TileBehavior.Puddle);
        map.SetBehaviour(1, 2, TileBehavior.ShallowWater);
        map.SetBehaviour(2, 1, TileBehavior.StillPuddle);
        var life = new FieldLife();
        life.Footstep(map, 1, 1, Direction.Right, false, TravelMode.OnFoot);
        var (puddleRing, puddleDrops) = Quads(life);
        Assert.Single(puddleRing);
        Assert.Equal(2, puddleDrops.Count);
        Assert.All(puddleDrops, d => Assert.Equal(FieldLife.Drop, d.Cell));

        // As in Platinum: water ankle deep only ripples, and a still puddle is not disturbed at all
        life.Clear();
        life.Footstep(map, 1, 2, Direction.Right, false, TravelMode.OnFoot);
        Assert.Equal((1, 0), (Quads(life).Flat.Count, Quads(life).Upright.Count));
        life.Clear();
        life.Footstep(map, 2, 1, Direction.Right, false, TravelMode.OnFoot);
        Assert.Equal(0, life.Count);

        life.Clear();
        life.Splash(map, 3, 4);
        var (ring, drops) = Quads(life);
        Assert.Single(ring);
        Assert.Equal(4, drops.Count);
        Assert.True(ring[0].Width > puddleRing[0].Width);
        Assert.Equal(2, drops.Count(d => d.At.X < 3.5f));

        // The drops of the bigger splash fly higher
        life.Clear();
        life.Footstep(map, 1, 1, Direction.Right, false, TravelMode.OnFoot);
        life.Splash(map, 3, 4);
        Run(life, FieldLife.DropTime / 2f);
        var flying = Quads(life).Upright;
        Assert.True(flying.Where(d => d.At.X > 2.5f).Min(d => d.At.Y) > flying.Where(d => d.At.X < 2.5f).Max(d => d.At.Y));
    }

    [Fact]
    public void ALandingRaisesDustToBothSidesButNoneOffWater()
    {
        var map = Pond();
        var life = new FieldLife();
        life.Landing(map, 1, 1);
        var (flat, puffs) = Quads(life);
        Assert.Empty(flat);
        Assert.Equal(2, puffs.Count);
        Assert.True(puffs.Any(p => p.At.X < 1.5f) && puffs.Any(p => p.At.X > 1.5f));

        life.Clear();
        life.Landing(map, 4, 4);
        Assert.Equal(0, life.Count);
    }

    [Fact]
    public void NothingIsLeftBehindInARoomOrOffTheMap()
    {
        var room = Lawn();
        room.Interior = InteriorStyle.House;
        for (int x = 0; x < 9; x++) room.SetGroundTile(x, 4, TileType.Sand);
        var life = new FieldLife();
        life.Footstep(room, 4, 4, Direction.Right, true, TravelMode.OnFoot);
        life.Landing(room, 4, 4);
        life.Splash(room, 4, 4);
        life.Advance(1f);
        life.Rainfall(room, 4.5f, 4.5f, 12);
        Assert.Equal(0, life.Count);

        var lawn = Lawn();
        life.Footstep(lawn, -1, 4, Direction.Right, true, TravelMode.OnFoot);
        life.Landing(lawn, 4, 20);
        life.Splash(lawn, 30, 4);
        Assert.Equal(0, life.Count);
    }

    [Fact]
    public void WhatIsDrawnStandsOnWholeTexels()
    {
        var map = Lawn();
        map.SetGroundTile(4, 4, TileType.TallGrass);
        map.SetGroundTile(2, 2, TileType.Path);
        var life = new FieldLife();
        life.Footstep(map, 4, 4, Direction.Right, false, TravelMode.OnFoot);
        life.Footstep(map, 2, 2, Direction.Up, true, TravelMode.OnFoot);
        life.Splash(map, 6, 6);
        for (int frame = 0; frame < 20; frame++)
        {
            life.Advance(Frame);
            // Heights move by whole texels of a tile's 32, whatever the frame
            foreach (var quad in Quads(life).Upright)
            {
                float texels = quad.At.Y * 32f;
                Assert.True(MathF.Abs(texels - MathF.Round(texels)) < 0.01f, $"{quad.At.Y} at frame {frame}");
            }
        }
    }

    // ------------------------------------------------------------------ rain landing

    private static Map Field(TileType ground, bool solid = false)
    {
        var map = Lawn(40, 30);
        for (int y = 0; y < 30; y++)
            for (int x = 0; x < 40; x++)
                map.SetGroundTile(x, y, ground, solid);
        return map;
    }

    private static void Rain(FieldLife life, Map map, float seconds, int drops, float step = Frame)
    {
        for (float t = 0f; t < seconds - 0.0001f; t += step)
        {
            life.Advance(step);
            life.Rainfall(map, 20f, 15f, drops);
        }
    }

    [Fact]
    public void RainLandsAtItsRateInViewOfTheSpotItFallsRound()
    {
        var life = new FieldLife();
        int drops = Weathers.LandsABeat(FieldWeather.Rain);
        Rain(life, Field(TileType.Grass), 1f, drops);
        var (flecks, upright) = Quads(life);
        Assert.Empty(upright);
        Assert.All(flecks, f => Assert.True(IsCell(f, FieldLife.Fleck, 2)));
        Assert.All(flecks, f => Assert.True(MathF.Abs(f.At.X - 20f) <= FieldLife.RainWidth / 2f && MathF.Abs(f.At.Z - 15f) <= FieldLife.RainDepth / 2f));

        // A fleck lasts three tenths of a second: what lies on the ground is what landed in that time
        float expected = FieldLife.RainBeats * drops * FieldLife.FleckTime;
        Assert.InRange(flecks.Count, expected - 2 * drops, expected + 2 * drops);
        // And both of its frames are to be seen at once: the rain doesn't land in step
        Assert.Contains(flecks, f => f.Cell == FieldLife.Fleck);
        Assert.Contains(flecks, f => f.Cell == FieldLife.Fleck + 1);

        // Heavy rain lands twice as thick; fair weather not at all
        var heavy = new FieldLife();
        Rain(heavy, Field(TileType.Grass), 1f, Weathers.LandsABeat(FieldWeather.HeavyRain));
        Assert.InRange(heavy.Count, 2 * expected - 4 * drops, 2 * expected + 4 * drops);
        var fair = new FieldLife();
        Rain(fair, Field(TileType.Grass), 1f, Weathers.LandsABeat(FieldWeather.Clear));
        Assert.Equal(0, fair.Count);
    }

    [Fact]
    public void OnWaterOneDropInThreeLeavesARingAndNothingShowsOnWhatIsBuiltOver()
    {
        var water = new FieldLife();
        int drops = Weathers.LandsABeat(FieldWeather.Rain);
        Rain(water, Field(TileType.Water), 1.5f, drops);
        var rings = Quads(water).Flat;
        Assert.All(rings, r => Assert.True(IsCell(r, FieldLife.Ring, 3)));
        float expected = FieldLife.RainBeats * (drops / 3) * FieldLife.RingTime;
        Assert.InRange(rings.Count, expected - drops, expected + drops);
        // Smaller than the ring a swimmer leaves
        var swimmer = new FieldLife();
        swimmer.Footstep(Field(TileType.Water), 20, 15, Direction.Right, false, TravelMode.Surfing);
        Assert.True(rings.Where(r => r.Cell == FieldLife.Ring).All(r => r.Width < Quads(swimmer).Flat[0].Width));

        var built = new FieldLife();
        Rain(built, Field(TileType.Grass, solid: true), 1f, drops);
        Assert.Equal(0, built.Count);
    }

    [Fact]
    public void RainIsTheSameRainHoweverTheFramesFall()
    {
        var map = Field(TileType.Grass);
        int drops = Weathers.LandsABeat(FieldWeather.Rain);
        var smooth = new FieldLife();
        Rain(smooth, map, 1f, drops);
        var jerky = new FieldLife();
        Rain(jerky, map, 1f, drops, step: 1f / 20f);
        // Both a little way past a beat, so neither is on the edge of one
        smooth.Advance(0.012f); smooth.Rainfall(map, 20f, 15f, drops);
        jerky.Advance(0.012f); jerky.Rainfall(map, 20f, 15f, drops);

        static List<(float, float)> Places(FieldLife life) => Quads(life).Flat.Select(f => (f.At.X, f.At.Z)).OrderBy(p => p).ToList();
        Assert.Equal(Places(smooth), Places(jerky));
    }

    [Fact]
    public void AfterAPauseOnlyTheLastFewBeatsOfRainAreMadeUp()
    {
        var map = Field(TileType.Grass);
        var life = new FieldLife();
        life.Advance(30f);   // a battle, a menu: no rain was asked for meanwhile
        life.Rainfall(map, 20f, 15f, 12);
        Assert.InRange(life.Count, 1, 4 * 12);
    }

    // ------------------------------------------------------------------ doors

    [Fact]
    public void ADoorOpensInTwoFramesAsSomeoneStepsUpToIt()
    {
        var map = Lawn();
        var other = Lawn();
        var life = new FieldLife();
        Assert.Equal(0, life.DoorFrame(map, 3, 2));
        Assert.Null(life.MovingDoor);

        life.OpenDoor(map, 3, 2);
        Assert.Equal(1, life.DoorFrame(map, 3, 2));
        Assert.Equal((map, 3, 2), life.MovingDoor);
        // Only that door, on that map
        Assert.Equal(0, life.DoorFrame(map, 4, 2));
        Assert.Equal(0, life.DoorFrame(other, 3, 2));

        Run(life, FieldLife.DoorOpens / 2f + Frame);
        Assert.Equal(2, life.DoorFrame(map, 3, 2));

        // Asking again while it stands open doesn't start it over, and it stays open as long as it takes
        life.OpenDoor(map, 3, 2);
        Assert.Equal(2, life.DoorFrame(map, 3, 2));
        Run(life, 3f);
        Assert.Equal(2, life.DoorFrame(map, 3, 2));
    }

    [Fact]
    public void ADoorStandsOpenBehindWhoeverComesOutThenShuts()
    {
        var map = Lawn();
        var life = new FieldLife();
        Run(life, 1f);
        life.LeaveDoor(map, 3, 2);
        Assert.Equal(2, life.DoorFrame(map, 3, 2));
        Run(life, FieldLife.DoorLingers - 2 * Frame);
        Assert.Equal(2, life.DoorFrame(map, 3, 2));
        Run(life, 3 * Frame);
        Assert.Equal(1, life.DoorFrame(map, 3, 2));
        Run(life, FieldLife.DoorOpens / 2f + Frame);
        Assert.Equal(0, life.DoorFrame(map, 3, 2));
        Run(life, FieldLife.DoorOpens);
        Assert.Null(life.MovingDoor);
    }

    [Fact]
    public void ADoorSaysOnceWhenItBeginsToOpenAndWhenItShuts()
    {
        var map = Lawn();
        var life = new FieldLife();
        Assert.True(life.OpenDoor(map, 3, 2));
        Assert.False(life.OpenDoor(map, 3, 2));
        // A door that is opening never shuts by itself
        Run(life, 2f);
        Assert.Null(life.TakeShutting());

        life.LeaveDoor(map, 3, 2);
        Assert.Null(life.TakeShutting());
        Run(life, FieldLife.DoorLingers + Frame);
        Assert.Equal((map, 3, 2), life.TakeShutting());
        Assert.Null(life.TakeShutting());
    }

    [Fact]
    public void GoingSomewhereElseForgetsEverything()
    {
        var map = Lawn();
        map.SetGroundTile(4, 4, TileType.Sand);
        var life = new FieldLife();
        life.Footstep(map, 4, 4, Direction.Right, true, TravelMode.OnFoot);
        life.OpenDoor(map, 3, 2);
        Assert.True(life.Count > 0);
        life.Clear();
        Assert.Equal(0, life.Count);
        Assert.Null(life.MovingDoor);
    }

    [Fact]
    public void AnOpenDoorIsTheSizeOfTheShutOneAndShowsTheRoomBehindIt()
    {
        foreach (var (glass, pair, width, height) in new[] { (true, false, 36, 40), (false, true, 40, 39), (false, false, 22, 39) })
        {
            int Doorway(PixelCanvas art)
            {
                // The room behind is marked to light up after dark, like a window
                int marked = 0;
                for (int y = 0; y < art.Height; y++)
                    for (int x = 0; x < art.Width; x++)
                    {
                        var texel = art.Get(x, y);
                        if (texel.A == ArtSheet.HomeLight && texel.R < 90 && texel.B < 90) marked++;
                    }
                return marked;
            }

            var ajar = BuildingArt.OpenDoor(glass, pair, 1, ArtSheet.HomeLight);
            var open = BuildingArt.OpenDoor(glass, pair, 2, ArtSheet.HomeLight);
            Assert.Equal((width, height), (ajar.Width, ajar.Height));
            Assert.Equal((width, height), (open.Width, open.Height));
            Assert.True(Doorway(ajar) > 0 && Doorway(open) > Doorway(ajar), $"glass {glass} pair {pair}: {Doorway(ajar)} then {Doorway(open)}");
            // Nothing of it is cut out: it covers the shut door whole
            for (int y = 0; y < open.Height; y++)
                for (int x = 0; x < open.Width; x++)
                    Assert.True(open.Get(x, y).A >= 128 && ajar.Get(x, y).A >= 128);
        }
    }

    [Theory]
    [InlineData("TwinleafTown")]
    [InlineData("SandgemTown")]
    public void EveryDoorThatLeadsSomewhereHasAPlaceOnItsWall(string name)
    {
        var map = Fixtures.Map(name);
        var buildings = MapStructures.BuildingsOf(map);
        int found = 0;
        foreach (var warp in map.Warps)
        {
            if (WorldRenderer.DoorPlace(map, warp.SourceX, warp.SourceY) is not { } place) continue;
            found++;
            // Over the warp's own tile, on the front of the building that has it, a texel proud of the wall
            Assert.Equal(warp.SourceX + 0.5f, place.Foot.X);
            var building = buildings.Single(b => !b.Annex && warp.SourceX >= b.X0 && warp.SourceX <= b.X1 && (warp.SourceY == b.Y1 || warp.SourceY == b.Y1 + 1));
            Assert.InRange(place.Foot.Z, building.Y1 + 1f, building.Y1 + 2.1f);
            Assert.False(place.Glass && place.Pair);
            Assert.True(place.Light is ArtSheet.HomeLight or ArtSheet.PublicLight);
        }
        Assert.True(found >= 2, $"{name}: {found} doors placed");

        // Where no door is, there is none to open
        Assert.Null(WorldRenderer.DoorPlace(map, 0, 0));
    }

    // ------------------------------------------------------------------ the art

    [Fact]
    public void TheAtlasHasEveryCellInItsOwnSquareInShadesToBeTinted()
    {
        var atlas = LifeArt.Atlas();
        Assert.Equal((FieldLife.Cell * FieldLife.Columns, FieldLife.Cell * 2), (atlas.Width, atlas.Height));
        var used = new[]
        {
            FieldLife.Print, FieldLife.Puff, FieldLife.Puff + 1, FieldLife.Puff + 2, FieldLife.Leaf, FieldLife.Leaf + 1,
            FieldLife.Ring, FieldLife.Ring + 1, FieldLife.Ring + 2, FieldLife.Drop, FieldLife.Fleck, FieldLife.Fleck + 1
        };
        Assert.Equal(used.Length, used.Distinct().Count());
        for (int cell = 0; cell < FieldLife.Columns * 2; cell++)
        {
            int ox = cell % FieldLife.Columns * FieldLife.Cell, oy = cell / FieldLife.Columns * FieldLife.Cell, painted = 0;
            for (int y = 0; y < FieldLife.Cell; y++)
                for (int x = 0; x < FieldLife.Cell; x++)
                {
                    var texel = atlas.Get(ox + x, oy + y);
                    if (texel.A == 0) continue;
                    painted++;
                    // Clear of the cell's edge, so no neighbour shows at a quad's rim
                    Assert.True(x > 0 && y > 0 && x < FieldLife.Cell - 1 && y < FieldLife.Cell - 1, $"cell {cell} touches its edge at ({x}, {y})");
                    // White and greys only: the colour comes from the tint
                    Assert.True(Math.Max(texel.R, Math.Max(texel.G, texel.B)) - Math.Min(texel.R, Math.Min(texel.G, texel.B)) <= 10, $"cell {cell} is coloured");
                }
            if (used.Contains(cell)) Assert.True(painted >= 4, $"cell {cell} is empty");
            else Assert.Equal(0, painted);
        }
    }

    [Fact]
    public void AFountainPlaysAndATurbineTurnsInFramesTheSizeOfTheirFace()
    {
        var map = Lawn(22, 14);
        map.Props.Add(new Prop { Type = PropType.Fountain, X = 5, Y = 5, Width = 4, Depth = 3 });
        map.Props.Add(new Prop { Type = PropType.WindTurbine, X = 13, Y = 6, Width = 2, Depth = 2, Height = 5f });
        map.Props.Add(new Prop { Type = PropType.Statue, X = 2, Y = 2, Width = 1, Depth = 1 });
        var kit = new KitBuilder(new ArtSheet(), MapScene.VerticalScaleOf(map));
        var lamplight = new MeshBuilder();
        foreach (var prop in map.Props)
        {
            kit.Origin = new Vector3(prop.X, 0f, prop.Y);
            Assert.True(Landmarks.Add(kit, map, prop, lamplight));
        }

        // The fountain and the turbine move; a statue stands still
        var moving = kit.Sheet.Moving;
        Assert.Equal(2, moving.Count);
        Assert.Contains(moving, m => m.Key.StartsWith("fountain."));
        Assert.Contains(moving, m => m.Key.StartsWith("turbine."));
        var sheet = kit.Sheet.ToCanvas();
        foreach (var (key, region, frames, rate) in moving)
        {
            Assert.Equal(Landmarks.MovingFrames, frames.Length);
            Assert.Equal(6f, rate);
            var seen = new HashSet<string>();
            foreach (var frame in frames)
            {
                // Each frame fills the face's rectangle exactly, and no two are the same picture
                Assert.Equal((region.Width, region.Height), (frame.Width, frame.Height));
                var text = new System.Text.StringBuilder();
                int painted = 0;
                for (int y = 0; y < frame.Height; y++)
                    for (int x = 0; x < frame.Width; x++)
                    {
                        var t = frame.Get(x, y);
                        text.Append(t.A == 0 ? 0 : t.R * 65536 + t.G * 256 + t.B).Append(';');
                        if (t.A > 0) painted++;
                        // Cut out or solid, never in between: a moving face has no glass
                        Assert.True(t.A is 0 or 255, $"{key}: a texel of alpha {t.A}");
                    }
                Assert.True(seen.Add(text.ToString()), $"{key}: two frames alike");
                Assert.True(painted > region.Width * region.Height / 12, $"{key}: a frame is nearly empty");
            }
            // The sheet holds the first frame
            for (int y = 0; y < region.Height; y++)
                for (int x = 0; x < region.Width; x++)
                    Assert.Equal(frames[0].Get(x, y), sheet.Get(region.X + x, region.Y + y));
        }

        // The turbine's tower stands still while its blades turn: below the hub every frame is the same
        var turbine = moving.Single(m => m.Key.StartsWith("turbine."));
        for (int y = 100; y < turbine.Region.Height; y++)
            for (int x = 0; x < turbine.Region.Width; x++)
                Assert.Equal(turbine.Frames[0].Get(x, y), turbine.Frames[2].Get(x, y));

        // Asking for the same face again paints nothing new
        kit.Origin = new Vector3(16, 0f, 6);
        Assert.True(Landmarks.Add(kit, map, map.Props[1], lamplight));
        Assert.Equal(2, kit.Sheet.Moving.Count);
    }

    [Fact]
    public void EveryBubbleIsTheSameBubbleWithItsOwnSign()
    {
        var kinds = Enum.GetValues<EmoteBubble>().Where(k => k != EmoteBubble.None).ToList();
        Assert.Equal(7, kinds.Count);
        var blank = LifeArt.Bubble(EmoteBubble.None);
        Assert.Equal((LifeArt.BubbleWidth, LifeArt.BubbleHeight), (blank.Width, blank.Height));

        static string Key(PixelCanvas c)
        {
            var text = new System.Text.StringBuilder();
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                {
                    var t = c.Get(x, y);
                    text.Append(t.R).Append(',').Append(t.G).Append(',').Append(t.B).Append(',').Append(t.A).Append(';');
                }
            return text.ToString();
        }

        var seen = new HashSet<string> { Key(blank) };
        foreach (var kind in kinds)
        {
            var art = LifeArt.Bubble(kind);
            Assert.True(seen.Add(Key(art)), $"{kind} looks like another");
            // The sign is inside the bubble: its outline is the blank one's
            for (int y = 0; y < art.Height; y++)
                for (int x = 0; x < art.Width; x++)
                    Assert.Equal(blank.Get(x, y).A, art.Get(x, y).A);
            // And nothing of it is pure white, which would glow
            for (int y = 0; y < art.Height; y++)
                for (int x = 0; x < art.Width; x++)
                    Assert.False(art.Get(x, y) is { R: 255, G: 255, B: 255, A: 255 });
        }
    }

    [Fact]
    public void MistRepeatsAtItsEdgesAndHasClearAirInIt()
    {
        var haze = LifeArt.Haze();
        Assert.Equal((LifeArt.HazeSize, LifeArt.HazeSize), (haze.Width, haze.Height));
        int clear = 0, thick = 0;
        float seam = 0f, inside = 0f;
        for (int i = 0; i < LifeArt.HazeSize; i++)
        {
            // Across the edge the cloud changes no faster than it does anywhere else
            seam += Math.Abs(haze.Get(0, i).A - haze.Get(LifeArt.HazeSize - 1, i).A) + Math.Abs(haze.Get(i, 0).A - haze.Get(i, LifeArt.HazeSize - 1).A);
            inside += Math.Abs(haze.Get(60, i).A - haze.Get(61, i).A) + Math.Abs(haze.Get(i, 60).A - haze.Get(i, 61).A);
            for (int j = 0; j < LifeArt.HazeSize; j++)
            {
                var texel = haze.Get(i, j);
                Assert.Equal((255, 255, 255), (texel.R, texel.G, texel.B));
                if (texel.A == 0) clear++;
                if (texel.A > 200) thick++;
            }
        }
        Assert.True(seam <= inside * 3f + 2f * LifeArt.HazeSize, $"seam {seam}, inside {inside}");
        int all = LifeArt.HazeSize * LifeArt.HazeSize;
        Assert.InRange(clear, all / 50, all * 3 / 4);
        Assert.InRange(thick, all / 50, all * 3 / 4);
    }

    // ------------------------------------------------------------------ weather over the picture

    private static readonly FieldWeather[] AllWeather = Enum.GetValues<FieldWeather>();

    private static List<WeatherBlock> Blocks(FieldWeather weather, float time)
    {
        var blocks = new List<WeatherBlock>();
        WeatherFx.Build(weather, time, 1920f, 1080f, blocks);
        return blocks;
    }

    private static List<HazeLayer> Layers(FieldWeather weather, float time)
    {
        var layers = new List<HazeLayer>();
        WeatherFx.Haze(weather, time, layers);
        return layers;
    }

    [Fact]
    public void WeatherIsAFunctionOfItsKindAndTheTime()
    {
        foreach (var weather in AllWeather)
        {
            Assert.Equal(Blocks(weather, 3.7f), Blocks(weather, 3.7f));
            Assert.Equal(Layers(weather, 3.7f), Layers(weather, 3.7f));
            if (WeatherFx.CountOf(weather) > 0) Assert.NotEqual(Blocks(weather, 3.7f), Blocks(weather, 3.9f));
            else Assert.Empty(Blocks(weather, 3.7f));
        }
        Assert.Empty(Blocks(FieldWeather.Clear, 1f));
        Assert.Empty(Blocks(FieldWeather.Cloudy, 1f));
        Assert.Empty(Blocks(FieldWeather.Fog, 1f));
    }

    [Fact]
    public void WhatFallsIsDrawnInThePicturesOwnPixelsAndInItsNumbers()
    {
        foreach (var weather in AllWeather)
        {
            int count = WeatherFx.CountOf(weather);
            foreach (float time in new[] { 0f, 0.31f, 12.6f, 300f })
            {
                var blocks = Blocks(weather, time);
                // Each piece is one to four blocks
                Assert.InRange(blocks.Count, count, count * 4);
                foreach (var block in blocks)
                {
                    foreach (float v in new[] { block.X, block.Y, block.Width, block.Height })
                        Assert.True(MathF.Abs(v / 3f - MathF.Round(v / 3f)) < 0.001f, $"{weather}: {v} is not on the three-pixel grid");
                    Assert.True(block.Width > 0f && block.Height > 0f && block.Color.A > 0);
                    // Nothing is drawn far outside the picture
                    Assert.InRange(block.X, -700f, 2400f);
                    Assert.InRange(block.Y, -120f, 1200f);
                }
            }
        }
        // The heavier kinds have more in the air than the lighter
        Assert.True(WeatherFx.CountOf(FieldWeather.HeavyRain) > WeatherFx.CountOf(FieldWeather.Rain));
        Assert.Equal(WeatherFx.CountOf(FieldWeather.HeavyRain), WeatherFx.CountOf(FieldWeather.Thunderstorm));
        Assert.True(WeatherFx.CountOf(FieldWeather.Blizzard) > WeatherFx.CountOf(FieldWeather.HeavySnow));
        Assert.True(WeatherFx.CountOf(FieldWeather.HeavySnow) > WeatherFx.CountOf(FieldWeather.Snow));
    }

    [Theory]
    [InlineData(FieldWeather.Rain)]
    [InlineData(FieldWeather.HeavyRain)]
    [InlineData(FieldWeather.Thunderstorm)]
    [InlineData(FieldWeather.Hail)]
    [InlineData(FieldWeather.HeavySnow)]
    [InlineData(FieldWeather.Blizzard)]
    [InlineData(FieldWeather.Sandstorm)]
    public void WhatTheWindDrivesGoesEastAndDown(FieldWeather weather)
    {
        var before = Blocks(weather, 5f);
        var after = Blocks(weather, 5.02f);
        Assert.Equal(before.Count, after.Count);
        int east = 0, down = 0, moved = 0;
        for (int i = 0; i < before.Count; i++)
        {
            float dx = after[i].X - before[i].X, dy = after[i].Y - before[i].Y;
            // One that has left the picture comes back in at the other side
            if (dx < -100f || dy < -100f) continue;
            moved++;
            Assert.True(dx >= 0f, $"{weather}: a piece went west by {-dx}");
            Assert.True(dy >= 0f, $"{weather}: a piece went up by {-dy}");
            if (dx > 0f) east++;
            if (dy > 0f) down++;
        }
        Assert.True(east > moved / 2 && down > moved / 2, $"{weather}: {east} east and {down} down of {moved}");
    }

    [Fact]
    public void TheWeatherIsAsSmoothAfterADaysPlayAsAtItsStart()
    {
        // A day in: a sixtieth of a second still moves the rain by its speed, not by whatever a float has left
        const double day = 86400.0, frame = 1.0 / 60.0;
        foreach (var weather in new[] { FieldWeather.Rain, FieldWeather.Sandstorm, FieldWeather.Blizzard })
        {
            var before = new List<WeatherBlock>();
            var after = new List<WeatherBlock>();
            WeatherFx.Build(weather, day, 1920f, 1080f, before);
            WeatherFx.Build(weather, day + frame, 1920f, 1080f, after);
            int moved = 0, still = 0;
            for (int i = 0; i < before.Count; i++)
            {
                float dx = after[i].X - before[i].X, dy = after[i].Y - before[i].Y;
                if (dx < -100f || dy < -100f) continue;
                // Never a jump: at most what its speed covers in a frame, to the block
                Assert.InRange(dx, 0f, 45f);
                Assert.InRange(dy, 0f, 45f);
                if (dx > 0f || dy > 0f) moved++; else still++;
            }
            Assert.True(moved > still, $"{weather}: {moved} moved, {still} stood still");
        }

        var life = new FieldLife();
        life.Advance(86400f);
        var map = Lawn();
        map.SetGroundTile(4, 4, TileType.Path);
        life.Footstep(map, 4, 4, Direction.Right, true, TravelMode.OnFoot);
        int first = Quads(life).Upright[0].Cell;
        // The puff still goes through its three frames in its third of a second
        var cells = new HashSet<int> { first };
        for (int i = 0; i < 30 && life.Count > 0; i++)
        {
            life.Advance(Frame);
            if (life.Count > 0) cells.Add(Quads(life).Upright[0].Cell);
        }
        Assert.Equal(3, cells.Count);
        Assert.Equal(0, life.Count);
    }

    [Fact]
    public void OnlyMistIsSoftAndItDriftsTheWayTheWindGoes()
    {
        var misty = new Dictionary<FieldWeather, int>
        {
            [FieldWeather.Fog] = 2, [FieldWeather.Sandstorm] = 2, [FieldWeather.Blizzard] = 2, [FieldWeather.HeavySnow] = 1
        };
        foreach (var weather in AllWeather)
        {
            var layers = Layers(weather, 1f);
            Assert.Equal(misty.GetValueOrDefault(weather), layers.Count);
            var later = Layers(weather, 1.5f);
            for (int i = 0; i < layers.Count; i++)
            {
                Assert.True(later[i].OffsetX > layers[i].OffsetX && later[i].OffsetY >= layers[i].OffsetY);
                Assert.InRange(layers[i].Tint.A, 40, 140);
                Assert.True(layers[i].Scale >= 10f);
            }
            // Layers of one weather move at different paces, or they would be one layer
            if (layers.Count == 2)
                Assert.NotEqual(later[0].OffsetX - layers[0].OffsetX, later[1].OffsetX - layers[1].OffsetX);
        }
        // A layer's offset stays small however long the mist has drifted: it wraps where its cloud repeats,
        // which leaves the picture as it would have been
        foreach (var weather in misty.Keys)
            foreach (var layer in Layers(weather, 400000f))
            {
                Assert.InRange(layer.OffsetX, 0f, LifeArt.HazeSize * layer.Scale);
                Assert.InRange(layer.OffsetY, 0f, LifeArt.HazeSize * layer.Scale);
            }

        // Fog is pale and a sandstorm's haze is sand
        Assert.True(Layers(FieldWeather.Fog, 0f).All(l => l.Tint.B > l.Tint.R));
        Assert.True(Layers(FieldWeather.Sandstorm, 0f).All(l => l.Tint.R > l.Tint.B + 40));
    }

    [Fact]
    public void LightningComesOnlyInAThunderstormTwoFlashesAtATime()
    {
        int strikes = 0, lit = 0;
        float last = 0f, lastStrike = -100f;
        var gaps = new List<float>();
        const float step = 1f / 240f;
        for (float t = 0f; t < 160f; t += step)
        {
            float now = WeatherFx.Lightning(FieldWeather.Thunderstorm, t);
            Assert.InRange(now, 0f, 0.8f);
            if (now > 0f) lit++;
            if (now >= 0.7f && last < 0.7f)
            {
                strikes++;
                if (lastStrike >= 0f) gaps.Add(t - lastStrike);
                lastStrike = t;

                // The first flash, a dark beat, the second and fainter flash, and then the dark again
                Assert.Equal(0f, WeatherFx.Lightning(FieldWeather.Thunderstorm, t + 0.1f));
                Assert.InRange(WeatherFx.Lightning(FieldWeather.Thunderstorm, t + 0.17f), 0.3f, 0.7f);
                Assert.Equal(0f, WeatherFx.Lightning(FieldWeather.Thunderstorm, t + 0.5f));
            }
            last = now;
        }
        Assert.Equal(20, strikes);   // one in each eight seconds
        // One to six seconds into each eight: three to thirteen seconds apart
        Assert.All(gaps, gap => Assert.InRange(gap, 2.9f, 13.1f));
        Assert.True(gaps.Distinct().Count() > 10, "the strikes come at a steady beat");
        // It is dark far longer than it is lit
        Assert.True(lit * step < 160f * 0.06f);

        foreach (var weather in AllWeather.Where(w => w != FieldWeather.Thunderstorm))
            for (float t = 0f; t < 20f; t += 0.01f)
                Assert.Equal(0f, WeatherFx.Lightning(weather, t));
    }

    // ------------------------------------------------------------------ weather by name, and in the light

    [Fact]
    public void AnAreasWeatherIsReadFromTheNameItsHeaderGivesIt()
    {
        var expected = new Dictionary<string, FieldWeather>
        {
            ["Clear"] = FieldWeather.Clear, ["Cloudy"] = FieldWeather.Cloudy, ["Raining"] = FieldWeather.Rain,
            ["HeavyRain"] = FieldWeather.HeavyRain, ["Thunderstorm"] = FieldWeather.Thunderstorm, ["Snowing"] = FieldWeather.Snow,
            ["HeavySnow"] = FieldWeather.HeavySnow, ["Blizzard"] = FieldWeather.Blizzard, ["SlowAshfall"] = FieldWeather.Ash,
            ["Sandstorm"] = FieldWeather.Sandstorm, ["Hailing"] = FieldWeather.Hail, ["Fog"] = FieldWeather.Fog, ["DeepFog"] = FieldWeather.Fog,
            // The five places of Platinum's calendar, on most of its days
            ["Route212South"] = FieldWeather.Rain, ["Route213"] = FieldWeather.Clear, ["Route216"] = FieldWeather.HeavySnow,
            ["AcuityLakefront"] = FieldWeather.HeavySnow, ["SnowpointCity"] = FieldWeather.Snow,
            // Moods of caves and rooms, which put nothing in the air
            ["DarkFlash"] = FieldWeather.Clear, ["Clear8"] = FieldWeather.Clear, ["Clear13"] = FieldWeather.Clear, ["Spirits"] = FieldWeather.Clear,
            ["23"] = FieldWeather.Clear, ["29"] = FieldWeather.Clear, [""] = FieldWeather.Clear
        };
        foreach (var (name, weather) in expected) Assert.Equal(weather, Weathers.Of(name));
        Assert.Equal(FieldWeather.Clear, Weathers.Of(null));
        // Every kind there is can be named
        Assert.Equal(AllWeather.OrderBy(w => w), expected.Values.Distinct().OrderBy(w => w));
    }

    [Fact]
    public void EveryAreaOfTheWorldHasAWeatherAndTheOpenOnesAreFair()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        // Twinleaf Town and its routes are fair in Platinum; a room has no weather whatever its map says
        Assert.Equal(FieldWeather.Clear, sinnoh.WeatherAt(112, 880));
        var room = Lawn();
        room.Weather = FieldWeather.Rain;
        Assert.Equal(FieldWeather.Rain, room.WeatherAt(4, 4));
        room.Interior = InteriorStyle.House;
        Assert.Equal(FieldWeather.Clear, room.WeatherAt(4, 4));
    }

    [Fact]
    public void RainIsWhatWetsTheGroundAndWhatAnEvolutionWaitsFor()
    {
        var rain = new[] { FieldWeather.Rain, FieldWeather.HeavyRain, FieldWeather.Thunderstorm };
        foreach (var weather in AllWeather)
        {
            Assert.Equal(rain.Contains(weather), Weathers.IsRain(weather));
            Assert.Equal(rain.Contains(weather) || weather == FieldWeather.Hail, Weathers.LandsABeat(weather) > 0);
            Assert.InRange(Weathers.Wind(weather), 0f, 1f);
        }
        Assert.Equal(0f, Weathers.Wind(FieldWeather.Clear));
        Assert.True(Weathers.Wind(FieldWeather.Thunderstorm) > Weathers.Wind(FieldWeather.HeavyRain));
        Assert.True(Weathers.Wind(FieldWeather.HeavyRain) > Weathers.Wind(FieldWeather.Rain));
        Assert.Equal(1f, Weathers.Wind(FieldWeather.Blizzard));
    }

    [Fact]
    public void WeatherTakesTheSunAndGivesPartOfItBackFromTheSky()
    {
        foreach (var time in Enum.GetValues<TimeOfDay>())
        {
            var fair = ArtLook.FieldRigFor(time);
            Assert.Equal(fair, ArtLook.Weathered(fair, FieldWeather.Clear));

            foreach (var weather in AllWeather.Where(w => w != FieldWeather.Clear))
            {
                var rig = ArtLook.Weathered(fair, weather);
                float through = ArtLook.SunThrough(weather);
                Assert.InRange(through, 0.15f, 0.8f);
                Assert.Equal(fair.Light.SunColor * through, rig.Light.SunColor);
                Assert.Equal(fair.Light.SunDirection, rig.Light.SunDirection);

                // The sky makes up half of what the sun lost: shade is lighter, lit ground darker, the two nearer each other
                static float Sum(Vector3 v) => v.X + v.Y + v.Z;
                float up = fair.Light.SunDirection.Y;
                float litBefore = Sum(fair.Light.SunColor) * up + Sum(fair.Light.SkyAmbient), shadeBefore = Sum(fair.Light.SkyAmbient);
                float litAfter = Sum(rig.Light.SunColor) * up + Sum(rig.Light.SkyAmbient), shadeAfter = Sum(rig.Light.SkyAmbient);
                Assert.True(shadeAfter > shadeBefore, $"{weather} at {time}: shade grew darker");
                Assert.True(litAfter < litBefore, $"{weather} at {time}: lit ground grew lighter");
                Assert.True(litAfter > litBefore * 0.6f, $"{weather} at {time}: the picture went dark");
                Assert.True(shadeAfter / litAfter > shadeBefore / litBefore, $"{weather} at {time}: shadows grew harder");

                // More fog, never a wall of it; no cloud shadows drifting under cloud
                Assert.True(rig.FogAmount >= fair.FogAmount && rig.FogAmount <= 0.9f);
                Assert.True(rig.FogNear <= fair.FogNear && rig.FogFar <= fair.FogFar);
                Assert.Equal(0f, rig.CloudShade);
                // Only rain takes colour out of things
                Assert.Equal(Weathers.IsRain(weather), rig.Post.Saturation < fair.Post.Saturation);
                // Lamps and windows burn as they did
                Assert.Equal((fair.LampGlow, fair.HomeGlow), (rig.LampGlow, rig.HomeGlow));
            }
        }

        // Each of the heavier kinds lets less sun through than the lighter
        Assert.True(ArtLook.SunThrough(FieldWeather.Thunderstorm) < ArtLook.SunThrough(FieldWeather.HeavyRain));
        Assert.True(ArtLook.SunThrough(FieldWeather.HeavyRain) < ArtLook.SunThrough(FieldWeather.Rain));
        Assert.True(ArtLook.SunThrough(FieldWeather.Rain) < ArtLook.SunThrough(FieldWeather.Cloudy));
        Assert.True(ArtLook.SunThrough(FieldWeather.Blizzard) < ArtLook.SunThrough(FieldWeather.HeavySnow));
        Assert.True(ArtLook.SunThrough(FieldWeather.HeavySnow) < ArtLook.SunThrough(FieldWeather.Snow));
        Assert.Equal(1f, ArtLook.SunThrough(FieldWeather.Clear));

        // What hides things ten tiles off: fog, a sandstorm, heavy snow, a blizzard
        var day = ArtLook.FieldRigFor(TimeOfDay.Day);
        foreach (var weather in AllWeather)
        {
            bool thick = weather is FieldWeather.Fog or FieldWeather.Sandstorm or FieldWeather.HeavySnow or FieldWeather.Blizzard;
            Assert.Equal(thick, ArtLook.Weathered(day, weather).FogNear < day.FogNear);
        }
        var sand = ArtLook.Weathered(day, FieldWeather.Sandstorm).FogColor;
        Assert.True(sand.X > sand.Z + 0.1f, "a sandstorm's fog is sand-coloured");
    }

    // ------------------------------------------------------------------ bubbles, hops and riding out

    [Fact]
    public void ABubbleShowsForItsTimeOverAnyoneAndCountsHowLongItHas()
    {
        var npc = new NPC();
        var player = new Player(2, 2);
        Assert.Equal(EmoteBubble.None, npc.Bubble);
        npc.ShowBubble(EmoteBubble.Question);
        player.ShowBubble(EmoteBubble.Heart, 0.5f);
        Assert.Equal((EmoteBubble.Question, 0.9f, 0f), (npc.Bubble, npc.BubbleTimer, npc.BubbleAge));

        for (int i = 0; i < 24; i++) { npc.TickBubble(Frame); player.TickBubble(Frame); }
        Assert.True(npc.BubbleTimer > 0f && MathF.Abs(npc.BubbleAge - 0.4f) < 0.001f);
        Assert.True(player.BubbleTimer > 0f);
        for (int i = 0; i < 12; i++) { npc.TickBubble(Frame); player.TickBubble(Frame); }
        Assert.True(npc.BubbleTimer > 0f);
        Assert.True(player.BubbleTimer <= 0f);

        // Its clock stops with it, and another bubble starts from the beginning
        float stopped = player.BubbleAge;
        player.TickBubble(Frame);
        Assert.Equal(stopped, player.BubbleAge);
        player.ShowBubble(EmoteBubble.Exclaim);
        Assert.Equal((EmoteBubble.Exclaim, 0f), (player.Bubble, player.BubbleAge));
    }

    /// <summary>Holds a direction until the player has stopped on a tile again.</summary>
    private static void Walk(Player player, Map map, Direction dir)
    {
        player.Facing = dir;
        bool started = false;
        for (int frame = 0; frame < 600; frame++)
        {
            player.Advance(Frame, map, started ? null : dir, false, _ => { }, _ => { });
            started |= player.IsMoving;
            if (started && !player.IsMoving && !player.IsSliding) return;
            if (!started && frame > 2) return;
        }
    }

    [Fact]
    public void AHopEndsInALandingAndAPlainStepDoesNot()
    {
        var map = Lawn();
        map.SetGroundTile(4, 5, TileType.LedgeDown);
        var player = new Player(4, 3);
        Walk(player, map, Direction.Down);
        Assert.Equal((4, 4), (player.GridX, player.GridY));
        Assert.False(player.JustLanded);

        // While the step is on, its heading is where it ends
        player.Advance(Frame, map, Direction.Down, false, _ => { }, _ => { });
        player.Advance(Frame, map, Direction.Down, false, _ => { }, _ => { });
        Assert.True(player.IsMoving);
        Assert.Equal((4, 6), player.Heading);

        Walk(player, map, Direction.Down);
        Assert.Equal((4, 6), (player.GridX, player.GridY));
        Assert.True(player.JustLanded);
        Assert.Equal((4, 6), player.Heading);

        Walk(player, map, Direction.Down);
        Assert.Equal((4, 7), (player.GridX, player.GridY));
        Assert.False(player.JustLanded);

        // Being put somewhere is not landing there
        Walk(player, map, Direction.Up);
        player.SetPosition(4, 3, Direction.Down);
        Walk(player, map, Direction.Down);
        Walk(player, map, Direction.Down);
        Assert.True(player.JustLanded);
        player.SetPosition(1, 1, Direction.Down);
        Assert.False(player.JustLanded);
    }

    [Fact]
    public void RidingOutOntoWaterIsToldApartFromSwimmingOn()
    {
        var map = Pond();
        var player = new Player(2, 4) { Moves = FieldMoves.Surf };
        player.Facing = Direction.Right;
        Assert.True(player.StartSurf(map));
        for (int frame = 0; frame < 600 && (player.IsMoving || player.Mode != TravelMode.Surfing); frame++)
            player.Advance(Frame, map, null, false, _ => { }, _ => { });
        Assert.Equal((3, 4, TravelMode.Surfing), (player.GridX, player.GridY, player.Mode));
        Assert.True(player.JustRodeOut);

        Walk(player, map, Direction.Right);
        Assert.Equal((4, 4), (player.GridX, player.GridY));
        Assert.False(player.JustRodeOut);
    }

    // ------------------------------------------------------------------ into battle

    [Fact]
    public void TheWayIntoABattleFollowsWhoIsMetAndWhetherTheyAreStronger()
    {
        // As in Platinum: the first Pokémon they send out against the player's first
        Assert.Equal(TransitionKind.Wild, SceneTransition.ForBattle(trainer: false, leader: false, theirLevel: 4, ownLevel: 5));
        Assert.Equal(TransitionKind.Wild, SceneTransition.ForBattle(false, false, 5, 5));
        Assert.Equal(TransitionKind.WildStrong, SceneTransition.ForBattle(false, false, 6, 5));
        Assert.Equal(TransitionKind.Trainer, SceneTransition.ForBattle(true, false, 5, 5));
        Assert.Equal(TransitionKind.TrainerStrong, SceneTransition.ForBattle(true, false, 9, 5));
        // A Gym Leader has their own, however strong
        Assert.Equal(TransitionKind.Leader, SceneTransition.ForBattle(true, true, 3, 50));
        Assert.Equal(TransitionKind.Leader, SceneTransition.ForBattle(true, true, 60, 5));
    }

    public static IEnumerable<object[]> BattleKinds() =>
        Enum.GetValues<TransitionKind>().Where(k => k != TransitionKind.Fade).Select(k => new object[] { k });

    private static float Covered(TransitionKind kind, bool closing, float closed)
    {
        int covered = 0, all = 0;
        for (float y = 7f; y < 1080f; y += 21f)
            for (float x = 9f; x < 1920f; x += 23f)
            {
                all++;
                if (SceneTransition.Covers(kind, closing, closed, x, y, 1920f, 1080f)) covered++;
            }
        return covered / (float)all;
    }

    [Theory]
    [MemberData(nameof(BattleKinds))]
    public void TheShapesStartOpenCloseSteadilyAndEndShut(TransitionKind kind)
    {
        foreach (bool closing in new[] { true, false })
        {
            Assert.Equal(0f, Covered(kind, closing, 0f));
            Assert.Equal(1f, Covered(kind, closing, 1f));
            float last = 0f;
            for (float closed = 0.05f; closed < 1f; closed += 0.05f)
            {
                float now = Covered(kind, closing, closed);
                Assert.True(now >= last - 0.02f, $"{kind}: less is covered at {closed} ({now}) than before ({last})");
                last = now;
            }
            // Half way, some of the picture is still to be seen and some is gone
            Assert.InRange(Covered(kind, closing, 0.5f), 0.1f, 0.9f);
        }
    }

    [Fact]
    public void AWildBattleOpensThroughAnIrisAndTheOthersThroughTheirOwnShapes()
    {
        foreach (var kind in new[] { TransitionKind.Wild, TransitionKind.WildStrong })
        {
            // The middle is seen first, the corners last
            Assert.False(SceneTransition.Covers(kind, closing: false, 0.6f, 960f, 540f, 1920f, 1080f));
            Assert.True(SceneTransition.Covers(kind, closing: false, 0.6f, 20f, 20f, 1920f, 1080f));
            Assert.True(SceneTransition.Covers(kind, closing: false, 0.6f, 1900f, 1060f, 1920f, 1080f));
        }
        foreach (var kind in new[] { TransitionKind.Trainer, TransitionKind.TrainerStrong, TransitionKind.Leader })
            for (float closed = 0.1f; closed < 1f; closed += 0.2f)
                Assert.Equal(Covered(kind, true, closed), Covered(kind, false, closed));

        // A stronger trainer's shutters cover whatever a trainer's do, and more
        for (float y = 30f; y < 1080f; y += 90f)
            for (float x = 30f; x < 1920f; x += 120f)
                if (SceneTransition.Covers(TransitionKind.Trainer, true, 0.4f, x, y, 1920f, 1080f))
                    Assert.True(SceneTransition.Covers(TransitionKind.TrainerStrong, true, 0.4f, x, y, 1920f, 1080f));
        Assert.True(Covered(TransitionKind.TrainerStrong, true, 0.4f) > Covered(TransitionKind.Trainer, true, 0.4f));

        // The shards break out from the middle: a third of the way closed, much of the middle is gone and none of the rim
        int middle = 0, middleCovered = 0, rimCovered = 0;
        for (float y = 5f; y < 1080f; y += 15f)
            for (float x = 5f; x < 1920f; x += 15f)
            {
                float far = MathF.Sqrt((x - 960f) * (x - 960f) + (y - 540f) * (y - 540f)) / MathF.Sqrt(960f * 960f + 540f * 540f);
                bool covered = SceneTransition.Covers(TransitionKind.WildStrong, true, 0.35f, x, y, 1920f, 1080f);
                if (far < 0.2f) { middle++; if (covered) middleCovered++; }
                else if (far > 0.9f && covered) rimCovered++;
            }
        Assert.True(middleCovered > middle / 3, $"{middleCovered} of {middle} in the middle");
        Assert.Equal(0, rimCovered);
    }

    [Theory]
    [MemberData(nameof(BattleKinds))]
    public void TwoFlashesComeFirstThenTheShapesClose(TransitionKind kind)
    {
        // Lit, dark, lit, dark within the quarter second, and never blinding
        Assert.True(SceneTransition.Flash(kind, 0.01f) > 0f);
        Assert.Equal(0f, SceneTransition.Flash(kind, 0.09f));
        Assert.True(SceneTransition.Flash(kind, 0.14f) > 0f);
        Assert.Equal(0f, SceneTransition.Flash(kind, 0.22f));
        for (float t = -0.1f; t < 1.2f; t += 0.005f) Assert.InRange(SceneTransition.Flash(kind, t), 0f, 0.75f);
        Assert.Equal(0f, SceneTransition.Flash(kind, SceneTransition.FlashSeconds));

        // Nothing closes while it flashes; then the shapes close with an ease and are shut on time
        Assert.Equal(0f, SceneTransition.Closed(kind, closing: true, SceneTransition.FlashSeconds));
        float last = 0f;
        for (float t = SceneTransition.FlashSeconds; t <= SceneTransition.CloseSeconds; t += 0.01f)
        {
            float now = SceneTransition.Closed(kind, true, t);
            Assert.True(now >= last);
            last = now;
        }
        Assert.Equal(1f, SceneTransition.Closed(kind, true, SceneTransition.CloseSeconds));
        Assert.Equal(SceneTransition.CloseSeconds, SceneTransition.OutSeconds(kind));

        // And they open at once on the other side, in half a second
        Assert.Equal(1f, SceneTransition.Closed(kind, closing: false, 0f));
        Assert.Equal(0f, SceneTransition.Closed(kind, false, SceneTransition.OpenSeconds));
        Assert.Equal(SceneTransition.OpenSeconds, SceneTransition.InSeconds(kind));
    }

    [Fact]
    public void APlainFadeNeitherFlashesNorDraws()
    {
        Assert.Equal(0f, SceneTransition.Flash(TransitionKind.Fade, 0.01f));
        Assert.Equal(SceneTransition.FadeSeconds, SceneTransition.OutSeconds(TransitionKind.Fade));
        Assert.Equal(SceneTransition.FadeSeconds, SceneTransition.InSeconds(TransitionKind.Fade));
        Assert.Equal(0.5f, SceneTransition.Closed(TransitionKind.Fade, true, SceneTransition.FadeSeconds / 2f), 3);
        Assert.Equal(0.5f, SceneTransition.Closed(TransitionKind.Fade, false, SceneTransition.FadeSeconds / 2f), 3);
        // The dark it and the others close to is never pure black
        Assert.True(SceneTransition.Dark.R > 0 && SceneTransition.Dark.B > SceneTransition.Dark.R);
    }
}
