using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumTests;

/// <summary>
/// Sprites that move (plan 09 · L5; style guide, "Pokémon in menus"): the strip of eight frames of the idle loop over
/// one breath, which the Pokédex's entry, the summary and the chosen starter play and no list asks for.
/// </summary>
public class MenuStripTests
{
    /// <summary>One of each body plan's way of idling: standing, hovering, flying, a serpent, a fish, flames.</summary>
    public static IEnumerable<object[]> Breathers => new[] { "Turtwig", "Chimchar", "Gastly", "Starly", "Dratini", "Magikarp", "Pikachu" }
        .Select(s => new object[] { s });

    private static SkeletonPose Frame(PokeModel m, int frame)
    {
        var into = new SkeletonPose(m.Skeleton.Count);
        PokemonAnimation.StripPose(m, frame, PokemonSprites.StripFrames, PokemonSprites.StripStart, into, new SkeletonPose(m.Skeleton.Count));
        return into;
    }

    /// <summary>The largest difference between two poses, over every bone's turn, offset and scale.</summary>
    private static float Apart(SkeletonPose a, SkeletonPose b)
    {
        float most = 0f;
        for (int i = 0; i < a.Count; i++)
        {
            var q = a.Rotation[i] - b.Rotation[i];
            var r = a.Rotation[i] + b.Rotation[i];
            // A turn and its negation are the same turn
            most = MathF.Max(most, MathF.Min(MathF.Abs(q.X) + MathF.Abs(q.Y) + MathF.Abs(q.Z) + MathF.Abs(q.W),
                MathF.Abs(r.X) + MathF.Abs(r.Y) + MathF.Abs(r.Z) + MathF.Abs(r.W)));
            most = MathF.Max(most, Vector3.Distance(a.Offset[i], b.Offset[i]));
            most = MathF.Max(most, Vector3.Distance(a.Scale[i], b.Scale[i]));
        }
        return most;
    }

    [Theory]
    [MemberData(nameof(Breathers))]
    public void TheStripsFramesDifferAndItsLastLeadsBackToItsFirst(string species)
    {
        var m = PokemonModels.Get(species);
        var frames = Enumerable.Range(0, PokemonSprites.StripFrames + 1).Select(f => Frame(m, f)).ToList();

        // The first frame is the still: the idle loop at the still's moment, unblended
        var still = new SkeletonPose(m.Skeleton.Count);
        PokemonAnimation.Apply(m, new PokePose { Time = PokemonSprites.StripStart }, still);
        Assert.True(Apart(frames[0], still) < 1e-6f, $"{species}: the strip's first frame is not the still");

        // Each frame moves on from the one before
        for (int f = 0; f < PokemonSprites.StripFrames; f++)
            Assert.True(Apart(frames[f], frames[f + 1]) > 1e-4f, $"{species}: frames {f} and {(f + 1) % PokemonSprites.StripFrames} are the same pose");

        // The frame after the last is the first, exactly: the loop has no jump
        Assert.True(Apart(frames[PokemonSprites.StripFrames], frames[0]) < 1e-5f, $"{species}: the strip's last frame doesn't lead back to its first");

        // And the step from the last back to the first is a step like the others, not a jump
        var steps = Enumerable.Range(0, PokemonSprites.StripFrames).Select(f => Apart(frames[f], frames[f + 1])).ToList();
        float others = steps.Take(PokemonSprites.StripFrames - 1).Max();
        Assert.True(steps[^1] <= 3f * others, $"{species}: the way back to the first frame is {steps[^1]:F4}, where the others are at most {others:F4}");
    }

    [Fact]
    public void AStripIsOneBreathOfEightFrames()
    {
        Assert.Equal(8, PokemonSprites.StripFrames);
        var m = PokemonModels.Get("Turtwig");
        Assert.Equal(1.7f / MathF.Max(0.1f, m.Tempo), PokemonAnimation.BreathSeconds(m), 4);

        // Played by the clock, a frame every eighth of the breath, round and round
        const float period = 1.6f;
        for (int f = 0; f < 8; f++) Assert.Equal(f, PokemonSprites.StripFrameAt(period * (f + 0.5) / 8, period));
        Assert.Equal(0, PokemonSprites.StripFrameAt(period, period));
        Assert.Equal(3, PokemonSprites.StripFrameAt(10 * period + period * 3.5 / 8, period));
        Assert.Equal(7, PokemonSprites.StripFrameAt(-period * 0.5 / 8, period));
    }

    [Fact]
    public void AStripsKeyCarriesItsFrameCount()
    {
        Assert.Equal("Turtwig|strip8", PokemonSprites.StripKey("Turtwig"));
        Assert.NotEqual(PokemonSprites.StripKey("Gyarados"), PokemonSprites.StripKey("Gyarados+s"));
        string file = Path.GetFileName(PokemonSprites.StripFile("Gyarados+s", "abc1s1", 1.7f));
        Assert.Equal("gyarados_s-strip8-abc1s1-1700.png", file);
        // Its name can't be taken for a still's: those are named -front-, -back- and -icon-
        Assert.DoesNotContain("-front-", file);
    }

    [Fact]
    public void OnlyTheFewOnScreenHoldAStrip()
    {
        // A strip is eight stills' texture: the game holds a handful, against the stills' hundreds (plan 03 · D13)
        Assert.InRange(PokemonSprites.StripsKept, 1, 4);
        Assert.True(PokemonSprites.StripsKept * PokemonSprites.StripFrames < PokemonSprites.Kept / 4);
    }

    /// <summary>The three places a strip plays, each a method of the interface's drawing.</summary>
    private static readonly string[] PlaysIn = { "DexInfo", "DrawSummary", "DrawStarters" };

    [Fact]
    public void ListsNeverAskForAStrip()
    {
        string engine = Path.Combine(Repository.Root, "PokemonPlatinumEngine");
        var asks = new Regex(@"PixelArtGenerator\.StripOf\(|PokemonSprites\.GetStrip\(|\bBreathing\(");
        var method = new Regex(@"static\s+[\w<>\[\]?,. ]+\s+(\w+)\s*\(");
        var found = new List<string>();
        foreach (var file in Directory.GetFiles(engine, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            string name = Path.GetFileName(file);
            // Where a strip is made and where it is handed out
            if (name is "PokemonSprites.cs" or "PixelArtGenerator.cs") continue;
            string? inMethod = null;
            foreach (var line in File.ReadLines(file))
            {
                var m = method.Match(line);
                if (m.Success) inMethod = m.Groups[1].Value;
                if (!asks.IsMatch(line) || inMethod == "Breathing") continue;
                found.Add($"{name}:{inMethod}");
                Assert.True(PlaysIn.Contains(inMethod), $"{name} asks for a strip in {inMethod}, which is no screen of one Pokémon");
            }
        }
        foreach (var place in PlaysIn) Assert.Contains(found, f => f.EndsWith(":" + place, StringComparison.Ordinal));
    }
}
