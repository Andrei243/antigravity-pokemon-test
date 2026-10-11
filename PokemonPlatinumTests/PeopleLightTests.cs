using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The light people stand in where a place's own is strongly coloured (plan 01 · M12; style guide, "Climates"):
/// inside a volcano a pale uniform lit by the room's amber came out the colour of a face.
/// </summary>
public class PeopleLightTests
{
    private static float Brightness(Vector3 c) => 0.299f * c.X + 0.587f * c.Y + 0.114f * c.Z;

    [Fact]
    public void InsideAVolcanoPeopleAreLitByAGreyAsBrightAsTheRoomsLight()
    {
        var cave = ArtLook.FieldRigFor(TimeOfDay.Day);
        var hot = ArtLook.InClimate(cave, AreaClimate.Volcanic, cave: true);
        Assert.Equal(1f, ArtLook.VolcanicPeopleWash);
        Assert.Equal(ArtLook.VolcanicPeopleWash, hot.PeopleWash);

        var people = ArtLook.ForPeople(hot.Light, hot.PeopleWash);
        foreach (var (room, theirs) in new[] { (hot.Light.SunColor, people.SunColor), (hot.Light.SkyAmbient, people.SkyAmbient), (hot.Light.GroundAmbient, people.GroundAmbient) })
        {
            // No colour of its own, and as bright as the room's
            Assert.Equal(theirs.X, theirs.Y, 5);
            Assert.Equal(theirs.Y, theirs.Z, 5);
            Assert.Equal(Brightness(room), Brightness(theirs), 4);
        }
        // The sun stands where it stood: only the colours change
        Assert.Equal(hot.Light.SunDirection, people.SunDirection);

        // What it is for: a pale uniform (#d0d4de) keeps more of its blue beside its red than a face does of its own,
        // where under the room's amber it had less than a face has in daylight
        var top = new Vector3(0xd0, 0xd4, 0xde) / 255f;
        Vector3 Lit(Vector3 colour, SceneLighting light) => colour * (light.SunColor + 0.5f * (light.SkyAmbient + light.GroundAmbient));
        var amber = Lit(top, hot.Light);
        var washed = Lit(top, people);
        Assert.True(amber.Z / amber.X < 0.62f, $"under the room's light the uniform's blue is {amber.Z / amber.X:0.00} of its red");
        Assert.True(washed.Z / washed.X > 1f, $"under the people's light the uniform's blue is {washed.Z / washed.X:0.00} of its red");
    }

    [Fact]
    public void EverywhereElsePeopleAreLitAsTheSceneryIs()
    {
        var day = ArtLook.FieldRigFor(TimeOfDay.Day);
        Assert.Equal(0f, day.PeopleWash);
        Assert.Equal(0f, ArtLook.InClimate(day, AreaClimate.Volcanic, cave: false).PeopleWash);
        Assert.Equal(0f, ArtLook.InClimate(day, AreaClimate.Frozen, cave: true).PeopleWash);
        Assert.Equal(day.Light, ArtLook.ForPeople(day.Light, 0f));
    }

    [Fact]
    public void ThePeoplesLightEasesInWithTheClimate()
    {
        var cave = ArtLook.FieldRigFor(TimeOfDay.Day);
        var hot = ArtLook.InClimate(cave, AreaClimate.Volcanic, cave: true);
        Assert.Equal(0.25f, LightRig.Lerp(cave, hot, 0.25f).PeopleWash, 5);
        // Part of the way there, the light has part of its colour left
        var half = ArtLook.ForPeople(hot.Light, 0.5f);
        Assert.True(half.SunColor.X > half.SunColor.Z && half.SunColor.X - half.SunColor.Z < hot.Light.SunColor.X - hot.Light.SunColor.Z);
    }
}
