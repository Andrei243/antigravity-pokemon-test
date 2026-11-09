using System.Numerics;
using System.Reflection;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 30 · FM3: a grown region's people as live rigs and impostors (style guide, "Live rigs in the field"): who is
/// live, the bubble over a grown head, the eight facings an impostor is baked at, the straightening the skinned programs
/// carry, and that nothing of it is reached where the region's kit is chibi.
/// </summary>
public class FieldRigTests
{
    private static (List<Vector3> Feet, List<object?> Who) Crowd(int count)
    {
        var feet = new List<Vector3>();
        var who = new List<object?>();
        for (int i = 0; i < count; i++)
        {
            // A ring of rings round the middle: person i stands i / 3 tiles out, three to a ring
            float r = 1f + i / 3, a = i * 2.1f;
            feet.Add(new Vector3(10f + r * MathF.Cos(a), 0f, 10f + r * MathF.Sin(a)));
            who.Add(new object());
        }
        return (feet, who);
    }

    [Fact]
    public void TheTwelveNearestAreLiveNearestFirst()
    {
        var (feet, who) = Crowd(30);
        var live = new List<int>();
        FieldRigChoice.Choose(feet, who, new Vector3(10, 0, 10), new HashSet<object>(), live);
        Assert.Equal(FieldRigChoice.MostLive, live.Count);
        float D(int i) => new Vector2(feet[i].X - 10, feet[i].Z - 10).Length();
        for (int k = 1; k < live.Count; k++) Assert.True(D(live[k - 1]) <= D(live[k]));
        float farthestLive = live.Max(D);
        for (int i = 0; i < feet.Count; i++)
            if (!live.Contains(i)) Assert.True(D(i) >= farthestLive);
    }

    [Fact]
    public void FewerThanTwelveAreAllLive()
    {
        var (feet, who) = Crowd(5);
        var live = new List<int>();
        FieldRigChoice.Choose(feet, who, Vector3.Zero, new HashSet<object>(), live);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }.OrderBy(i => i), live.OrderBy(i => i));
    }

    [Fact]
    public void HeightDoesNotCountOnlyTheGround()
    {
        var feet = new List<Vector3> { new(0, 40, 3), new(0, 0, 4) };
        var live = new List<int>();
        FieldRigChoice.Choose(feet, new List<object?> { null, null }, Vector3.Zero, new HashSet<object>(), live);
        Assert.Equal(0, live[0]);
    }

    [Fact]
    public void SomeoneLiveLastFrameStaysLiveAgainstANeighbourAlmostAsNear()
    {
        // Thirteen people: the twelfth and thirteenth at almost the same distance, the thirteenth a hair nearer
        var feet = new List<Vector3>();
        var who = new List<object?>();
        for (int i = 0; i < 11; i++) { feet.Add(new Vector3(i * 0.1f, 0, 1)); who.Add(new object()); }
        var twelfth = new object();
        var thirteenth = new object();
        feet.Add(new Vector3(6.0f, 0, 0)); who.Add(twelfth);
        feet.Add(new Vector3(5.6f, 0, 0)); who.Add(thirteenth);
        var live = new List<int>();
        FieldRigChoice.Choose(feet, who, Vector3.Zero, new HashSet<object>(), live);
        Assert.Contains(12, live);
        Assert.DoesNotContain(11, live);
        // Had the twelfth been live last frame, it keeps its place
        FieldRigChoice.Choose(feet, who, Vector3.Zero, new HashSet<object> { twelfth }, live);
        Assert.Contains(11, live);
        Assert.DoesNotContain(12, live);
        // ... but not once the other is nearer by more than the stickiness
        feet[12] = new Vector3(6.0f - FieldRigChoice.Stickiness - 0.1f, 0, 0);
        FieldRigChoice.Choose(feet, who, Vector3.Zero, new HashSet<object> { twelfth }, live);
        Assert.Contains(12, live);
    }

    [Fact]
    public void TheSamePlacesGiveTheSameChoice()
    {
        var (feet, who) = Crowd(30);
        var a = new List<int>();
        var b = new List<int>();
        var before = new HashSet<object> { who[20]!, who[25]! };
        FieldRigChoice.Choose(feet, who, new Vector3(9, 0, 11), before, a);
        FieldRigChoice.Choose(feet, who, new Vector3(9, 0, 11), before, b);
        Assert.Equal(a, b);
    }

    [Fact]
    public void ABubbleOverAChibiHeadIsWhereItAlwaysWas()
    {
        // Bit for bit the field's own number, so no older shot moves
        Assert.Equal(1.95f, FieldRigChoice.BubbleLift(false, 1.94f));
        Assert.Equal(1.95f, FieldRigChoice.BubbleLift(false, 1f));
        Assert.True(FieldRigChoice.BubbleLift(true, 1.94f) > 1.95f + 0.3f);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.78f, 1)]
    [InlineData(1.5708f, 2)]
    [InlineData(3.1416f, 4)]
    [InlineData(-1.5708f, 6)]
    [InlineData(6.2832f, 0)]
    [InlineData(5.5f, 7)]
    public void AWayIsTheNearestOfEightFacings(float yaw, int facing) => Assert.Equal(facing, FieldRigChoice.Facing8(yaw));

    [Fact]
    public void ThePeopleOfEveryRegionBuiltOnTheGridAreChibiSoTheirFieldNeverMeetsALiveRig()
    {
        foreach (var region in RegionDatabase.All.Where(r => r.Generation < 6))
            Assert.Equal(CharacterKit.Chibi, region.Kit);
    }

    [Theory]
    [InlineData("YOUNGSTER")]
    [InlineData("PLAYER")]
    [InlineData("CLERK")]
    [InlineData("OLD_MAN")]
    public void APersonIsDrawnInTheGrownKitByTheirLooksGrownName(string look)
    {
        string grown = CharacterStyle.InKit(look, CharacterKit.Grown);
        Assert.Equal(CharacterKit.Grown, CharacterStyle.For(grown).Kit);
        // and the chibi name back again is the look itself
        Assert.Equal(look, CharacterStyle.InKit(grown, CharacterKit.Chibi), ignoreCase: true);
    }

    private static string Source(string field) =>
        (string)typeof(FieldShaders).GetField(field, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    [Fact]
    public void TheSkinnedProgramsStraightenOnlyWhileAskedTo()
    {
        foreach (var field in new[] { "SkinnedVertex", "SkinnedOutlineVertex" })
        {
            string text = Source(field);
            Assert.Contains("uniform float upright;", text);
            Assert.Contains("if (upright > 0.0)", text);
            Assert.Contains("gl_Position = Straightened(", text);
        }
        // The outline is cut off under the water's plane in the mirror pass, as the body is
        Assert.Contains("MirrorClip(fragWorld);", Source("SkinnedOutlineFragment"));
    }
}
