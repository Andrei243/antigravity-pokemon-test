using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 10 · F12's rustle on screen and F10's alpha in the field and in battle, GPU-free (style guide, "Life", "A rustle";
/// "Pokémon (version 2, G7)", "Alphas" and the eyes' order): what the field's life gives for a rustle and for a find
/// scared off, the two shiver marks of the atlas, the held fierce eyes and an alpha's size on its field body.
/// </summary>
public class CompanionRustleTests
{
    private static Map Grass()
    {
        var map = new Map(12, 12) { Name = "Lab" };
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++) map.SetGroundTile(x, y, TileType.TallGrass);
        return map;
    }

    [Fact]
    public void ARustleThrowsUpTwoLeavesAndShowsTwoShiverMarksEitherSideOfItsTile()
    {
        var life = new FieldLife();
        var map = Grass();
        life.Hiding(map, 5, 5);
        var flat = new List<LifeQuad>();
        var upright = new List<LifeQuad>();
        life.Advance(0.02f);
        life.Quads(flat, upright);
        Assert.Empty(flat);
        var shivers = upright.Where(q => q.Cell is FieldLife.ShiverLeft or FieldLife.ShiverRight).ToList();
        var leaves = upright.Where(q => q.Cell is FieldLife.Leaf or FieldLife.Leaf + 1).ToList();
        Assert.Equal(2, shivers.Count);
        Assert.Equal(2, leaves.Count);
        // The left mark west of the tile's middle and the right one east, half a tile out, whole at first
        var left = shivers.Single(q => q.Cell == FieldLife.ShiverLeft);
        var right = shivers.Single(q => q.Cell == FieldLife.ShiverRight);
        Assert.Equal(5.5f - FieldLife.ShiverOut, left.At.X, 3);
        Assert.Equal(5.5f + FieldLife.ShiverOut, right.At.X, 3);
        Assert.Equal(255, left.Tint.A);

        // In its second half a texel further out and fainter; gone after its time
        life.Advance(FieldLife.ShiverTime / 2f);
        flat.Clear(); upright.Clear();
        life.Quads(flat, upright);
        var later = upright.Single(q => q.Cell == FieldLife.ShiverLeft);
        Assert.Equal(150, later.Tint.A);
        Assert.Equal(left.At.X - 1f / 32f, later.At.X, 4);
        life.Advance(FieldLife.ShiverTime);
        flat.Clear(); upright.Clear();
        life.Quads(flat, upright);
        Assert.DoesNotContain(upright, q => q.Cell is FieldLife.ShiverLeft or FieldLife.ShiverRight);
    }

    [Fact]
    public void AFindScaredOffBurstsOutInSixLeavesAndAPuffAndNothingIsLeftInARoom()
    {
        var life = new FieldLife();
        life.Fled(Grass(), 4, 4);
        life.Advance(0.02f);
        var flat = new List<LifeQuad>();
        var upright = new List<LifeQuad>();
        life.Quads(flat, upright);
        Assert.Equal(6, upright.Count(q => q.Cell is FieldLife.Leaf or FieldLife.Leaf + 1));
        Assert.Contains(upright, q => q.Cell >= FieldLife.Puff && q.Cell <= FieldLife.Puff + 2);

        var room = new Map(8, 8) { Name = "Lab", Interior = InteriorStyle.House };
        var inside = new FieldLife();
        inside.Hiding(room, 3, 3);
        inside.Fled(room, 3, 3);
        Assert.Equal(0, inside.Count);
    }

    [Fact]
    public void TheShiverMarksAreCellsOfTheirOwnInGreysClearOfTheirEdgesAndNoMarksNorWater()
    {
        var atlas = LifeArt.Atlas();
        foreach (int cell in new[] { FieldLife.ShiverLeft, FieldLife.ShiverRight })
        {
            Assert.False(FieldLife.IsMark(cell));
            Assert.False(FieldLife.IsWet(cell));
            int ox = cell % FieldLife.Columns * FieldLife.Cell, oy = cell / FieldLife.Columns * FieldLife.Cell, painted = 0;
            for (int y = 0; y < FieldLife.Cell; y++)
                for (int x = 0; x < FieldLife.Cell; x++)
                {
                    var texel = atlas.Get(ox + x, oy + y);
                    if (texel.A == 0) continue;
                    painted++;
                    Assert.True(x > 0 && y > 0 && x < FieldLife.Cell - 1 && y < FieldLife.Cell - 1, $"cell {cell} touches its edge at ({x}, {y})");
                    Assert.True(Math.Max(texel.R, Math.Max(texel.G, texel.B)) - Math.Min(texel.R, Math.Min(texel.G, texel.B)) <= 10, $"cell {cell} is coloured");
                }
            Assert.True(painted >= 11, $"cell {cell} has {painted} texels");
        }
        // The two cells lie in no mark's block of two by two
        foreach (int mark in new[] { FieldLife.PressedPlank, FieldLife.SaltHeap, FieldLife.SaltLittle, FieldLife.CaveMouth, FieldLife.DeepWater })
            foreach (int taken in new[] { mark, mark + 1, mark + FieldLife.Columns, mark + FieldLife.Columns + 1 })
                Assert.NotEqual(taken, FieldLife.ShiverLeft);
        // Water thrown up is what it always was
        foreach (int cell in Enumerable.Range(0, FieldLife.PressedPlank))
            Assert.Equal(cell >= FieldLife.Ring, FieldLife.IsWet(cell));
    }

    [Fact]
    public void AnAlphasEyesAreHeldFierceUnderAFaintSleepAndAHitAndOverAStatsChange()
    {
        Assert.Equal(EyeState.Open, PokemonAnimation.EyesOf(new PokePose { Time = 0.4f }));
        Assert.Equal(EyeState.Fierce, PokemonAnimation.EyesOf(new PokePose { Time = 0.4f }, fierce: true));
        // Held fierce through a blink, a cheer, tiredness and a stat falling (5 comes before 6 to 11)
        Assert.Equal(EyeState.Fierce, PokemonAnimation.EyesOf(new PokePose { Blink = 1f }, true));
        Assert.Equal(EyeState.Fierce, PokemonAnimation.EyesOf(new PokePose { Stat = -1f }, true));
        Assert.Equal(EyeState.Fierce, PokemonAnimation.EyesOf(new PokePose { Cheer = 0.5f }, true));
        // A faint, sleep and a hit come first (1 to 3)
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Faint = 0.5f }, true));
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Asleep = true }, true));
        Assert.Equal(EyeState.Squeeze, PokemonAnimation.EyesOf(new PokePose { Hurt = 0.3f }, true));
    }

    [Fact]
    public void AnAlphaStandsInTheFieldAtItsOwnSizeAndAPlainOneAtOne()
    {
        var plain = new Pokemon(PokemonDatabase.Get("Bidoof")!, 50, new Random(3));
        var alpha = new Pokemon(PokemonDatabase.Get("Bidoof")!, 50, new Random(3));
        Alphas.Apply(alpha, new Random(5));
        var a = new NPC { Key = "a" };
        var b = new NPC { Key = "b" };
        Follower.Dress(a, plain);
        Follower.Dress(b, alpha);
        Assert.Equal(1f, a.Scale);
        Assert.Equal(Alphas.Scale, b.Scale);
        // The sprite is the species' own: the same look name
        Assert.Equal(a.Species, b.Species);
    }
}
