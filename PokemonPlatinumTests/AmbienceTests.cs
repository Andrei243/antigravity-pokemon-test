using System;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>Plan 05 · A7: the field's ambience, thunder, panned sounds, the handheld speakers, the loudness of each bus and the sound options.</summary>
public class AmbienceTests
{
    private static float[] Render(AudioMixer mixer, double seconds)
    {
        var buffer = new float[(int)(seconds * Synthesizer.SampleRate) * 2];
        for (int done = 0; done < buffer.Length; done += 2048)
            mixer.Render(buffer.AsSpan(done, Math.Min(2048, buffer.Length - done)));
        return buffer;
    }

    // ------------------------------------------------------------------ the beds

    [Fact]
    public void EveryBedLoopsWithoutASeamAtTheAmbienceLevel()
    {
        Assert.Equal(Enum.GetValues<AmbienceBed>().Length, Ambience.Entries.Count);
        foreach (var bed in Enum.GetValues<AmbienceBed>())
        {
            var s = Ambience.Get(bed).Samples;
            Assert.Same(Ambience.Get(bed), Ambience.Get(bed));
            Assert.Equal((int)(Ambience.LoopSeconds(bed) * Synthesizer.SampleRate), s.Length);
            Assert.All(s, x => Assert.True(float.IsFinite(x)));
            Assert.True(s.Max(MathF.Abs) < 0.9f, $"{bed} peaks too high");
            Assert.True(MathF.Abs(s.Average()) < 0.002f, $"{bed} has an offset");

            // The end runs on into the start: no bigger a step there than anywhere inside the bed
            float largest = 0f;
            for (int i = 1; i < s.Length; i++) largest = MathF.Max(largest, MathF.Abs(s[i] - s[i - 1]));
            Assert.True(MathF.Abs(s[0] - s[^1]) <= largest, $"{bed} has a seam");

            // Held at full gain it sounds at the ambience bus's level, well under the music
            var mixer = new AudioMixer();
            mixer.SetAmbience(new[] { new AmbienceLayer(bed, 1f, 0f) });
            var sound = Render(mixer, AudioMixer.AmbienceFade + 3);
            int settled = (int)((AudioMixer.AmbienceFade + 0.1) * Synthesizer.SampleRate) * 2;
            var (low, high) = Loudness.Target(AudioBus.Ambience);
            double loud = Loudness.Of(sound.AsSpan(settled), 2);
            Assert.True(loud >= low && loud <= high, $"{bed} measures {loud:0.0} dB, outside {low} to {high}");
            Assert.True(high < Loudness.Target(AudioBus.Music).Low);
        }
    }

    [Fact]
    public void ABedGlidesInLoopsAndFadesAway()
    {
        var mixer = new AudioMixer();
        mixer.SetAmbience(new[] { new AmbienceLayer(AmbienceBed.Rain, 0.5f, -0.8f) });
        Assert.Equal(0f, mixer.AmbienceGain(AmbienceBed.Rain));
        Render(mixer, AudioMixer.AmbienceFade / 4);
        float early = mixer.AmbienceGain(AmbienceBed.Rain);
        Assert.InRange(early, 0.05f, 0.45f);
        // Past the end of its loop, and still sounding: from the left
        var held = Render(mixer, Ambience.LoopSeconds(AmbienceBed.Rain) + 1);
        Assert.Equal(0.5f, mixer.AmbienceGain(AmbienceBed.Rain), 3);
        double left = 0, right = 0;
        for (int i = held.Length / 2; i < held.Length; i += 2)
        {
            left += held[i] * held[i];
            right += held[i + 1] * held[i + 1];
        }
        Assert.True(left > right * 4, "a bed panned left is heard from the left");

        // Another bed takes over: the rain goes and the wind comes, each over the fade
        mixer.SetAmbience(new[] { new AmbienceLayer(AmbienceBed.Wind, 1f, 0f) });
        Render(mixer, AudioMixer.AmbienceFade / 4);
        Assert.InRange(mixer.AmbienceGain(AmbienceBed.Rain), 0.01f, 0.49f);
        Render(mixer, AudioMixer.AmbienceFade);
        Assert.Equal(0f, mixer.AmbienceGain(AmbienceBed.Rain));
        Assert.Equal(1f, mixer.AmbienceGain(AmbienceBed.Wind), 3);

        // Nothing: silence once the fade is over, and the bus's volume holds a bed down
        mixer.SetAmbience(Array.Empty<AmbienceLayer>());
        Render(mixer, AudioMixer.AmbienceFade + 0.1);
        Assert.All(Render(mixer, 0.2), x => Assert.True(MathF.Abs(x) < 1e-6f));
        mixer.SetVolume(AudioBus.Ambience, 0f);
        mixer.SetAmbience(new[] { new AmbienceLayer(AmbienceBed.Waves, 1f, 0f) });
        Assert.All(Render(mixer, 2), x => Assert.True(MathF.Abs(x) < 1e-6f));
    }

    // ------------------------------------------------------------------ what the field sounds like

    [Theory]
    [InlineData(FieldWeather.Rain, AmbienceBed.Rain)]
    [InlineData(FieldWeather.HeavyRain, AmbienceBed.HeavyRain)]
    [InlineData(FieldWeather.Thunderstorm, AmbienceBed.HeavyRain)]
    [InlineData(FieldWeather.Hail, AmbienceBed.Hail)]
    [InlineData(FieldWeather.Snow, AmbienceBed.Wind)]
    [InlineData(FieldWeather.HeavySnow, AmbienceBed.Wind)]
    [InlineData(FieldWeather.Blizzard, AmbienceBed.Blizzard)]
    [InlineData(FieldWeather.Sandstorm, AmbienceBed.Sandstorm)]
    public void TheWeatherIsHeardWhereItFalls(FieldWeather weather, AmbienceBed bed)
    {
        var map = new Map(12, 12) { Weather = weather };
        var layers = FieldAmbience.Around(map, 6, 6);
        Assert.Equal(bed, Assert.Single(layers).Bed);
        Assert.Equal(0f, layers[0].Pan);
        // Heavier weather is heard harder
        Assert.True(FieldAmbience.OfWeather(FieldWeather.HeavySnow)!.Value.Gain > FieldAmbience.OfWeather(FieldWeather.Snow)!.Value.Gain);

        // Under a roof nothing falls, and in a cave only the cave is heard
        Assert.Empty(FieldAmbience.Around(new Map(12, 12) { Weather = weather, Interior = InteriorStyle.House }, 6, 6));
        var cave = new Map(12, 12) { Weather = weather, Setting = MapSetting.Cave };
        Assert.Equal(AmbienceBed.Cave, Assert.Single(FieldAmbience.Around(cave, 6, 6)).Bed);
    }

    [Theory]
    [InlineData(FieldWeather.Clear)]
    [InlineData(FieldWeather.Cloudy)]
    [InlineData(FieldWeather.Fog)]
    [InlineData(FieldWeather.Ash)]
    public void QuietWeatherAndDryLandMakeNoSound(FieldWeather weather)
    {
        Assert.Null(FieldAmbience.OfWeather(weather));
        Assert.Empty(FieldAmbience.Around(new Map(12, 12) { Weather = weather }, 6, 6));
    }

    [Fact]
    public void AWaterfallGrowsAsItIsWalkedUpToAndIsHeardFromItsSide()
    {
        var map = new Map(40, 20);
        for (int y = 4; y < 10; y++)
            for (int x = 30; x < 33; x++) map.SetBehaviour(x, y, TileBehavior.Waterfall);

        float Gain(int x, int y) => FieldAmbience.Around(map, x, y).FirstOrDefault(l => l.Bed == AmbienceBed.Waterfall).Gain;
        Assert.Equal(0f, Gain(5, 7));                       // out of reach
        float far = Gain(23, 7), near = Gain(28, 7);
        Assert.True(far > 0f && near > far, $"far {far}, near {near}");
        Assert.Equal(1f, Gain(31, 10));                     // at its foot
        // From the right while it is to the right, from the middle under it, and never wholly from one side
        var right = FieldAmbience.Around(map, 25, 7).First(l => l.Bed == AmbienceBed.Waterfall);
        Assert.InRange(right.Pan, 0.4f, FieldAmbience.PanReach);
        Assert.InRange(FieldAmbience.Around(map, 31, 11).First(l => l.Bed == AmbienceBed.Waterfall).Pan, -0.05f, 0.05f);
    }

    [Fact]
    public void TheSeaIsLouderTheMoreOfItThereIsAndARiverRuns()
    {
        var map = new Map(60, 30);
        for (int y = 0; y < 30; y++)
            for (int x = 30; x < 60; x++) map.SetBehaviour(x, y, TileBehavior.Sea);
        for (int y = 0; y < 30; y++) map.SetBehaviour(5, y, TileBehavior.River);

        float Waves(int x) => FieldAmbience.Around(map, x, 15).FirstOrDefault(l => l.Bed == AmbienceBed.Waves).Gain;
        Assert.Equal(0f, Waves(15));
        float shore = Waves(29), out_ = Waves(45);
        Assert.True(shore > 0.3f && out_ > shore, $"shore {shore}, out at sea {out_}");
        Assert.Equal(1f, out_);
        Assert.True(FieldAmbience.Around(map, 29, 15).First(l => l.Bed == AmbienceBed.Waves).Pan > 0.3f, "the sea is to the right of the shore");

        var river = FieldAmbience.Around(map, 7, 15).First(l => l.Bed == AmbienceBed.River);
        Assert.True(river.Gain > 0.5f && river.Pan < 0f, $"{river}");
    }

    [Fact]
    public void ASoundOfTheFieldIsHeardFromItsSideOfTheScreen()
    {
        Assert.Equal(0f, FieldAmbience.PanOf(0));
        Assert.Equal(FieldAmbience.PanReach, FieldAmbience.PanOf(FieldAmbience.HalfView));
        Assert.Equal(-FieldAmbience.PanReach, FieldAmbience.PanOf(-30));
        Assert.True(FieldAmbience.PanOf(3) is > 0f and < 0.5f);
    }

    [Fact]
    public void ThunderFollowsEachFlashOfAStorm()
    {
        // A storm's eighty seconds: ten strikes, each heard once, after its flash, two of three of them close
        var heard = Enumerable.Range(0, 800).SelectMany(i => WeatherFx.Thunder(FieldWeather.Thunderstorm, i * 0.1, (i + 1) * 0.1)
            .Select(name => (Name: name, At: (i + 1) * 0.1))).ToList();
        Assert.Equal(10, heard.Count);
        Assert.Contains(heard, h => h.Name == "thunder");
        Assert.Contains(heard, h => h.Name == "thunder_rumble");
        foreach (var (name, at) in heard)
        {
            double flash = Enumerable.Range(0, 30).Select(k => at - k * 0.05).First(t => WeatherFx.Lightning(FieldWeather.Thunderstorm, t) >= 0.8f);
            Assert.InRange(at - flash, 0.1, name == "thunder" ? 0.4 : 1.2);
            Assert.True(SoundBank.Exists(name));
        }
        // However the time is cut up, the same thunder
        Assert.Equal(heard.Select(h => h.Name), WeatherFx.Thunder(FieldWeather.Thunderstorm, 0, 80));
        Assert.Empty(WeatherFx.Thunder(FieldWeather.Rain, 0, 80));
    }

    // ------------------------------------------------------------------ the handheld speakers

    [Fact]
    public void TheHandheldSpeakersLoseTheBassNarrowTheSidesAndRoundToTenBits()
    {
        var song = MusicLibrary.Get("sinnoh/route201")!;
        float[] Play(bool handheld)
        {
            var mixer = new AudioMixer { Handheld = handheld };
            mixer.Play(song, night: false);
            mixer.PlaySound(SoundBank.Get("exclaim")!, pan: -1f);
            return Render(mixer, 3);
        }
        var stereo = Play(false);
        var handheld = Play(true);

        // Ten bits: every sample a whole number of steps
        Assert.All(handheld, x => Assert.Equal(0f, x * 512 - MathF.Round(x * 512), 3));
        Assert.Contains(stereo, x => MathF.Abs(x * 512 - MathF.Round(x * 512)) > 0.01f);

        // Less of the bass: a slow average follows it
        static double Bass(float[] s)
        {
            double low = 0, sum = 0;
            for (int i = 0; i < s.Length; i += 2)
            {
                low += 0.02 * (s[i] - low);
                sum += low * low;
            }
            return sum;
        }
        Assert.True(Bass(handheld) < Bass(stereo) * 0.5, $"the handheld keeps the bass: {Bass(handheld)} against {Bass(stereo)}");

        // A sound panned hard left alone: the handheld's speakers are close together, so the right side hears it too
        static double Side(float[] s, int ch) { double e = 0; for (int i = ch; i < s.Length; i += 2) e += s[i] * s[i]; return e; }
        float[] Alone(bool handheld)
        {
            var mixer = new AudioMixer { Handheld = handheld };
            mixer.PlaySound(SoundBank.Get("exclaim")!, pan: -1f);
            return Render(mixer, 0.3);
        }
        Assert.True(Side(Alone(false), 1) < 1e-9);
        var both = Alone(true);
        Assert.InRange(Side(both, 1) / Side(both, 0), 0.2, 0.8);

        // Muted is silent all the same
        var muted = new AudioMixer { Handheld = true, Muted = true };
        muted.Play(song, night: false);
        Assert.All(Render(muted, 0.5), x => Assert.Equal(0f, x));
    }

    // ------------------------------------------------------------------ the mixing pass

    [Fact]
    public void CriesSitInTheirBusesWindow()
    {
        var (low, high) = Loudness.Target(AudioBus.Cry);
        foreach (var name in new[] { "Turtwig", "Torterra", "Pikachu", "Bidoof", "Shinx", "Luxray", "Magikarp", "Gyarados", "Unown", "Happiny", "Snorlax", "Wailord", "Dialga", "Giratina" })
        {
            var mixer = new AudioMixer();
            var cry = Cries.Get(PokemonDatabase.Get(name)!);
            mixer.PlaySound(cry, AudioBus.Cry);
            double loud = Loudness.Of(Render(mixer, cry.Duration + 0.1), 2);
            Assert.True(loud >= low && loud <= high, $"{name} cries at {loud:0.0} dB, outside {low} to {high}");
        }
    }

    [Fact]
    public void TheLoudnessMeasureLeavesOutSilence()
    {
        // Ten blocks of a tone, then ten of silence
        int block = Synthesizer.SampleRate / 10;
        var tone = new float[block * 20];
        for (int i = 0; i < block * 10; i++) tone[i] = 0.5f * MathF.Sin(MathF.Tau * 440f * i / Synthesizer.SampleRate);
        // A sine of amplitude 0.5 is about -9 dB; the second of silence after it doesn't pull that down
        Assert.Equal(Loudness.Db(0.5 / Math.Sqrt(2)), Loudness.Of(tone), 1);
        Assert.Equal(-120, Loudness.Of(new float[4096]));
    }

    // ------------------------------------------------------------------ the options

    [Fact]
    public void TheSoundOptionsSetEachBusAndTheSpeakers()
    {
        var s = new GameSettings();
        Assert.Equal((100, 100, SpeakerMode.Stereo), (s.CryVolume, s.AmbienceVolume, s.Speakers));
        OptionsScreen.Change(s, OptionRow.CryVolume, -1);
        for (int i = 0; i < 12; i++) OptionsScreen.Change(s, OptionRow.AmbienceVolume, -1);
        OptionsScreen.Change(s, OptionRow.Speakers, 1);
        Assert.Equal((90, 0, SpeakerMode.Handheld), (s.CryVolume, s.AmbienceVolume, s.Speakers));
        OptionsScreen.Change(s, OptionRow.Speakers, -1);
        Assert.Equal(SpeakerMode.Stereo, s.Speakers);

        var options = new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        s.Speakers = SpeakerMode.Handheld;
        var read = JsonSerializer.Deserialize<GameSettings>(JsonSerializer.Serialize(s, options), options)!;
        Assert.Equal((90, 0, SpeakerMode.Handheld), (read.CryVolume, read.AmbienceVolume, read.Speakers));

        // Each bus has its own volume, the fanfares following the music's
        AudioManager.SetVolumes(0.5f, 0.6f, 0.7f, 0.2f);
        Assert.Equal((0.5f, 0.5f, 0.6f, 0.7f, 0.2f), (AudioManager.VolumeOf(AudioBus.Music), AudioManager.VolumeOf(AudioBus.Fanfare),
            AudioManager.VolumeOf(AudioBus.Sound), AudioManager.VolumeOf(AudioBus.Cry), AudioManager.VolumeOf(AudioBus.Ambience)));
        AudioManager.SetVolumes(1f, 1f, 1f, 1f);
    }

    [Fact]
    public void TheOptionsScrollToShowTheRowTheCursorIsOn()
    {
        var screen = new OptionsScreen();
        screen.Open();
        int rows = Enum.GetValues<OptionRow>().Length;
        Assert.True(rows > OptionsScreen.VisibleRows);
        for (int i = 0; i < rows - 1; i++)
        {
            screen.Move(1);
            Assert.InRange(screen.SelectedIndex, screen.FirstRow, screen.FirstRow + OptionsScreen.VisibleRows - 1);
        }
        Assert.Equal(OptionRow.Help, Enum.GetValues<OptionRow>()[screen.SelectedIndex]);
        Assert.Equal(rows - OptionsScreen.VisibleRows, screen.FirstRow);
        // Round to the top again
        screen.Move(1);
        Assert.Equal((0, 0), (screen.SelectedIndex, screen.FirstRow));
    }
}
