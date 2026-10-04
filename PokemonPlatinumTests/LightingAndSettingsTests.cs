using System;
using System.IO;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>Time of day, the light rigs that follow it, and the options that are saved (plan 04 · G2).</summary>
public class LightingAndSettingsTests
{
    // pret/pokeplatinum, src/rtc.c: TimeOfDayForHour
    [Theory]
    [InlineData(0, TimeOfDay.LateNight)]
    [InlineData(3, TimeOfDay.LateNight)]
    [InlineData(4, TimeOfDay.Morning)]
    [InlineData(9, TimeOfDay.Morning)]
    [InlineData(10, TimeOfDay.Day)]
    [InlineData(16, TimeOfDay.Day)]
    [InlineData(17, TimeOfDay.Twilight)]
    [InlineData(19, TimeOfDay.Twilight)]
    [InlineData(20, TimeOfDay.Night)]
    [InlineData(23, TimeOfDay.Night)]
    public void TheClockFollowsPlatinumsTimesOfDay(int hour, TimeOfDay expected)
    {
        Assert.Equal(expected, GameClock.ForHour(hour));
    }

    [Fact]
    public void AFixedTimeOfDaySitsInsideItsPeriod()
    {
        try
        {
            foreach (var time in Enum.GetValues<TimeOfDay>())
            {
                GameClock.Fixed = time;
                Assert.Equal(time, GameClock.Now);
                Assert.Equal(time is TimeOfDay.Night or TimeOfDay.LateNight, GameClock.IsNight);
            }
        }
        finally
        {
            GameClock.Fixed = null;
        }
    }

    private static float Brightness(LightRig rig) =>
        Vector3.Dot(rig.Light.SunColor + rig.Light.SkyAmbient, Vector3.One) / 3f;

    [Fact]
    public void NightsAreDarkerAndLightTheWindows()
    {
        foreach (var rigFor in new Func<TimeOfDay, LightRig>[] { ArtLook.FieldRigFor, ArtLook.BattleRigFor })
        {
            float day = Brightness(rigFor(TimeOfDay.Day));
            // Night battles stay a little brighter than the night field, so the Pokémon read clearly
            Assert.True(Brightness(rigFor(TimeOfDay.Night)) < day * 0.7f);
            Assert.True(Brightness(rigFor(TimeOfDay.LateNight)) < Brightness(rigFor(TimeOfDay.Night)));
            Assert.True(Brightness(rigFor(TimeOfDay.Twilight)) < day);
        }

        // Street lamps and shops come on at twilight and burn all night; the windows of homes go dark late at night
        Assert.Equal(0f, ArtLook.FieldRigFor(TimeOfDay.Morning).LampGlow);
        Assert.Equal(0f, ArtLook.FieldRigFor(TimeOfDay.Day).LampGlow);
        Assert.Equal(0f, ArtLook.FieldRigFor(TimeOfDay.Day).HomeGlow);
        Assert.True(ArtLook.FieldRigFor(TimeOfDay.Twilight).LampGlow > 0f);
        Assert.True(ArtLook.FieldRigFor(TimeOfDay.Twilight).HomeGlow > 0f);
        Assert.Equal(1f, ArtLook.FieldRigFor(TimeOfDay.Night).LampGlow);
        Assert.Equal(1f, ArtLook.FieldRigFor(TimeOfDay.Night).HomeGlow);
        Assert.Equal(1f, ArtLook.FieldRigFor(TimeOfDay.LateNight).LampGlow);
        Assert.Equal(0f, ArtLook.FieldRigFor(TimeOfDay.LateNight).HomeGlow);
        Assert.Equal(ArtLook.Lamplight, ArtLook.FieldRigFor(TimeOfDay.Night).GlowColor);
        Assert.True(ArtLook.BattleRigFor(TimeOfDay.Night).Sky.Stars > 0f);
        Assert.Equal(0f, ArtLook.BattleRigFor(TimeOfDay.Day).Sky.Stars);
    }

    [Fact]
    public void LightBlendsSmoothlyAcrossEachChange()
    {
        // Away from a change the rig is exactly its period's; around it the light moves without a jump
        Assert.Equal(ArtLook.FieldRigFor(TimeOfDay.Day), ArtLook.FieldRig(13f, indoors: false));
        Assert.Equal(ArtLook.FieldRigFor(TimeOfDay.Night), ArtLook.FieldRig(22f, indoors: false));

        foreach (float change in new[] { 4f, 10f, 17f, 20f, 24f })
        {
            for (float h = change - 0.6f; h < change + 0.6f; h += 0.02f)
            {
                var a = ArtLook.FieldRig(h, indoors: false);
                var b = ArtLook.FieldRig(h + 0.02f, indoors: false);
                Assert.True(MathF.Abs(Brightness(a) - Brightness(b)) < 0.05f, $"light jumps at {h:F2}h");
            }
        }

        // Midnight wraps: just after 0:00 is still blending from night into late night
        float midnight = ArtLook.FieldRig(0.1f, indoors: false).HomeGlow;
        Assert.InRange(midnight, 0.01f, 0.99f);
    }

    [Fact]
    public void RoomsFollowTheClockThroughTheirWindows()
    {
        var day = ArtLook.FieldRig(13f, indoors: true);
        var dusk = ArtLook.FieldRig(18.5f, indoors: true);
        var night = ArtLook.FieldRig(2f, indoors: true);

        // By day the sun throws light on the floor and the glass keeps its painted sky
        Assert.Equal(1f, day.LampGlow);
        Assert.Equal(0f, day.HomeGlow);

        // At twilight the glass turns orange; at night it is dark blue and no daylight falls in
        Assert.True(dusk.HomeGlow > 0.5f);
        Assert.True(dusk.GlowColor.X > dusk.GlowColor.Z);
        Assert.Equal(0f, night.LampGlow);
        Assert.Equal(1f, night.HomeGlow);
        Assert.True(night.GlowColor.Z > night.GlowColor.X);
        Assert.True(night.GlowColor.Z < 0.4f);

        // A room at night is lit by its lamps: dimmer and warmer than by day, but far brighter than the night outside
        Assert.True(Brightness(night) < Brightness(day));
        Assert.True(Brightness(night) > Brightness(ArtLook.FieldRig(2f, indoors: false)) * 1.5f);
        Assert.True(night.Light.SunColor.X > night.Light.SunColor.Z * 1.5f);
        Assert.Equal(ArtLook.FieldRig(22f, indoors: true), night);
    }

    [Fact]
    public void QualityPresetsTradeSharpnessForSpeed()
    {
        var low = QualityProfile.For(GraphicsQuality.Low);
        var medium = QualityProfile.For(GraphicsQuality.Medium);
        var high = QualityProfile.For(GraphicsQuality.High);

        Assert.Equal(2f, high.SceneScale);
        Assert.True(high.Fxaa);
        Assert.True(medium.SceneScale < high.SceneScale && low.SceneScale < medium.SceneScale);
        Assert.True(medium.Fxaa && medium.AmbientOcclusion);
        Assert.False(low.AmbientOcclusion || low.DepthOfField);
        Assert.True(low.ShadowTaps < medium.ShadowTaps && medium.ShadowTaps < high.ShadowTaps);
    }

    [Fact]
    public void SettingsSurviveSavingAndLoading()
    {
        string path = Path.Combine(Path.GetTempPath(), $"platinum-settings-{Guid.NewGuid():N}.json");
        try
        {
            var saved = new GameSettings
            {
                Quality = GraphicsQuality.Medium, WindowWidth = 1600, WindowHeight = 900, Fullscreen = true,
                VSync = false, Muted = true, TimeOfDay = TimeOfDay.Twilight
            };
            saved.Save(path);
            var loaded = GameSettings.Load(path);

            Assert.Equal(GraphicsQuality.Medium, loaded.Quality);
            Assert.Equal((1600, 900), (loaded.WindowWidth, loaded.WindowHeight));
            Assert.True(loaded.Fullscreen);
            Assert.False(loaded.VSync);
            Assert.True(loaded.Muted);
            Assert.Equal(TimeOfDay.Twilight, loaded.TimeOfDay);

            // A damaged file falls back to the defaults instead of stopping the game
            File.WriteAllText(path, "{ not json");
            var fallback = GameSettings.Load(path);
            Assert.Equal(GraphicsQuality.High, fallback.Quality);
            Assert.Null(fallback.TimeOfDay);
        }
        finally
        {
            File.Delete(path);
        }

        Assert.Equal(GraphicsQuality.High, GameSettings.Load(path).Quality);
    }

    [Fact]
    public void OptionsCycleThroughTheirValues()
    {
        var s = new GameSettings();
        const OptionRow quality = OptionRow.Quality, windowSize = OptionRow.WindowSize, fullscreen = OptionRow.Fullscreen, timeOfDay = OptionRow.TimeOfDay;

        OptionsScreen.Change(s, quality, 1);
        Assert.Equal(GraphicsQuality.Low, s.Quality);
        OptionsScreen.Change(s, quality, -1);
        Assert.Equal(GraphicsQuality.High, s.Quality);

        OptionsScreen.Change(s, windowSize, 1);
        Assert.Equal((2560, 1440), (s.WindowWidth, s.WindowHeight));
        OptionsScreen.Change(s, windowSize, 1);
        Assert.Equal((3840, 2160), (s.WindowWidth, s.WindowHeight));
        OptionsScreen.Change(s, windowSize, 1);
        Assert.Equal((1280, 720), (s.WindowWidth, s.WindowHeight));

        OptionsScreen.Change(s, fullscreen, 1);
        Assert.True(s.Fullscreen);

        // Clock first, then the five times in the order of the day, and round again
        var seen = new TimeOfDay?[7];
        for (int i = 0; i < 7; i++)
        {
            seen[i] = s.TimeOfDay;
            OptionsScreen.Change(s, timeOfDay, 1);
        }
        Assert.Equal(new TimeOfDay?[] { null, TimeOfDay.Morning, TimeOfDay.Day, TimeOfDay.Twilight, TimeOfDay.Night, TimeOfDay.LateNight, null }, seen);
    }
}
