using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>The cries (plan 05 · A4): a voice of its own for every species and form, the original's modes, and where they are heard.</summary>
public class CryTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static CryVoice Voice(string name) => CryVoice.Of(Species(name));

    /// <summary>What the game asks to hear while <paramref name="play"/> runs.</summary>
    private static List<string> Hear(Action play)
    {
        var heard = AudioManager.Listen();
        try
        {
            play();
            return heard.ToList();
        }
        finally
        {
            AudioManager.StopListening();
        }
    }

    [Fact]
    public void EverySpeciesAndFormCriesCleanlyInAVoiceOfItsOwn()
    {
        var seen = new Dictionary<string, string>();
        var (low, high) = Loudness.Target(AudioBus.Cry);
        int count = 0;
        foreach (var species in PokemonDatabase.GetAll().OrderBy(s => s.DexNumber))
            foreach (var form in new string?[] { null }.Concat(species.Forms?.Select(f => f.Name) ?? Enumerable.Empty<string>()))
            {
                var voice = CryVoice.Of(species, form);
                var s = Cries.Play(Cries.Make(voice), CryMode.Normal);
                string name = form ?? species.Name;
                Assert.InRange(s.Length / (double)Synthesizer.SampleRate, 0.3, 1.5);
                Assert.InRange(voice.Pitch, 65f, 1500f);
                Assert.InRange(voice.Syllables.Length, 1, 4);
                Assert.All(s, x => Assert.True(float.IsFinite(x)));
                // At its peak, or turned down under it to the ceiling
                float peak = s.Max(MathF.Abs);
                double made = Loudness.Of(s);
                Assert.True(peak <= Cries.Peak + 0.01f, $"{name} peaks at {peak:0.000}");
                Assert.True(peak >= Cries.Peak - 0.01f || Math.Abs(made - Loudness.CryCeiling) < 0.05, $"{name} is turned down to {made:0.0} dB");
                Assert.True(made <= Loudness.CryCeiling + 0.05, $"{name} is made at {made:0.0} dB");
                Assert.True(MathF.Abs(s[0]) < 0.02f && MathF.Abs(s[^1]) < 0.02f, $"{name} clicks");
                double rms = Math.Sqrt(s.Average(x => (double)x * x));
                Assert.True(rms > 0.03, $"{name} is nearly silent");

                // In its bus's window as the mixer plays it
                var mixer = new AudioMixer();
                mixer.PlaySound(new SoundSample("cry " + name, s), AudioBus.Cry);
                var heard = new float[(s.Length + Synthesizer.SampleRate / 10) * 2];
                for (int done = 0; done < heard.Length; done += 2048)
                    mixer.Render(heard.AsSpan(done, Math.Min(2048, heard.Length - done)));
                double loud = Loudness.Of(heard, 2);
                Assert.True(loud >= low && loud <= high, $"{name} cries at {loud:0.0} dB, outside {low} to {high}");

                // No two cries are the same
                string print = string.Join(",", Enumerable.Range(0, 64).Select(i => s[s.Length * i / 64].ToString("0.000")));
                Assert.False(seen.TryGetValue(print, out var twin), $"{name} cries just like {twin}");
                seen[print] = name;
                count++;
            }
        Assert.True(count > 1025);
    }

    [Fact]
    public void ACryIsTheSameEveryTimeItIsMade()
    {
        var a = Cries.Make(Voice("Shinx"));
        var b = Cries.Make(Voice("Shinx"));
        Assert.Equal(a, b);
        var (x, y) = (Voice("Shinx"), Voice("Shinx"));
        Assert.Equal((x.Pitch, x.Seconds, x.Timbre), (y.Pitch, y.Seconds, y.Timbre));
        Assert.Equal(x.Syllables, y.Syllables);
    }

    [Theory]
    [InlineData("Turtwig", "Grotle", "Torterra")]
    [InlineData("Chimchar", "Monferno", "Infernape")]
    [InlineData("Piplup", "Prinplup", "Empoleon")]
    [InlineData("Shinx", "Luxio", "Luxray")]
    [InlineData("Magikarp", "Gyarados", null)]
    public void ALineSoundsRelatedAndDeepensAsItGrows(string first, string second, string? third)
    {
        var line = new[] { first, second, third }.Where(n => n != null).Select(n => Voice(n!)).ToList();
        for (int i = 1; i < line.Count; i++)
        {
            Assert.True(line[i].Pitch < line[i - 1].Pitch, $"{line[i].Name} is higher than {line[i - 1].Name}");
            Assert.True(line[i].Seconds >= line[i - 1].Seconds - 0.1f, $"{line[i].Name} is much shorter than {line[i - 1].Name}");
        }
        // The family's timbre, but for where each one's size sets its vowels
        if (line.All(v => Species(v.Name).PrimaryType == Species(first).PrimaryType && Species(v.Name).SecondaryType == Species(first).SecondaryType))
            Assert.All(line, v => Assert.Equal(line[0].Timbre with { Formants = 1f }, v.Timbre with { Formants = 1f }));
    }

    [Fact]
    public void ANameGivesTheCryItsSyllables()
    {
        Assert.Equal(3, Voice("Pikachu").Syllables.Length);
        Assert.Equal(3, Voice("Bulbasaur").Syllables.Length);
        Assert.InRange(Voice("Mew").Syllables.Length, 1, 2);
        Assert.Equal(4, Voice("Kricketune").Syllables.Length);
        // Shares add up to the whole cry
        Assert.All(new[] { "Pikachu", "Mew", "Bidoof", "Giratina" }, n => Assert.Equal(1f, Voice(n).Syllables.Sum(s => s.Share), 3));
        // A form tunes its species' voice: same syllables, its own pitch
        var altered = CryVoice.Of(Species("Giratina"));
        var origin = CryVoice.Of(Species("Giratina"), "Giratina-Origin");
        Assert.Equal(altered.Syllables.Length, origin.Syllables.Length);
        Assert.NotEqual(altered.Pitch, origin.Pitch);
        Assert.True(altered.Grand && origin.Grand);
    }

    [Fact]
    public void TheModesPitchAndShortenTheCryAsTheOriginalDoes()
    {
        var raw = Cries.Make(Voice("Bidoof"));
        int Length(CryMode mode) => Cries.Play(raw, mode).Length;
        int normal = Length(CryMode.Normal);
        Assert.Equal(raw.Length, normal);
        // Lower by 3.5 and by 1.5 semitones: slower, so longer by as much
        Assert.Equal(normal * Math.Pow(2, 3.5 / 12), Length(CryMode.Faint), normal * 0.002);
        Assert.Equal(normal * Math.Pow(2, 1.5 / 12), Length(CryMode.Pinch), normal * 0.002);
        // Twenty frames of sixty
        Assert.Equal(Synthesizer.SampleRate / 3, Length(CryMode.Half));
        Assert.Equal(Synthesizer.SampleRate / 3, Length(CryMode.PinchHalf));
        // A semitone up with its echo a third of a semitone up beside it: the echo is the longer
        Assert.Equal(normal / Math.Pow(2, 0.3125 / 12), Length(CryMode.FieldEvent), normal * 0.002);
        Assert.Equal(normal, Length(CryMode.Pokedex));
        // The raw cry is left as it was
        Assert.Equal(Cries.Make(Voice("Bidoof")), raw);
    }

    [Fact]
    public void APokemonSentOutInAPinchCriesLower()
    {
        var p = Scenario.Mon("Bidoof", 30);
        Assert.Equal(CryMode.Normal, Cries.SendOutMode(p));
        p.CurrentHP = p.MaxHP * 6 / 10;
        Assert.Equal(CryMode.Normal, Cries.SendOutMode(p));
        // At half or below the bar is yellow (24 of its 48 pixels)
        p.CurrentHP = p.MaxHP / 2;
        Assert.Equal(CryMode.Pinch, Cries.SendOutMode(p));
        p.CurrentHP = p.MaxHP;
        p.Status = StatusCondition.Sleep;
        Assert.Equal(CryMode.Pinch, Cries.SendOutMode(p));
    }

    [Fact]
    public void OnlyAFewDozenCriesAreKept()
    {
        foreach (var s in PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Take(Cries.Kept + 10)) Cries.Get(s);
        Assert.True(Cries.KeptCount <= Cries.Kept);
        // The one asked for last is still there, the same one
        var last = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Skip(Cries.Kept + 9).First();
        Assert.Same(Cries.Get(last), Cries.Get(last));
    }

    // ------------------------------------------------------------------ where they are heard

    [Fact]
    public void ABattleIsCriedIntoAndOutOf()
    {
        BattleEngine? battle = null;
        var start = Hear(() => battle = Scenario.Battle(Scenario.Mon("Pikachu", 30, "Thunderbolt"), Scenario.Mon("Bidoof", 5)));
        // The wild Pokémon as it appears, the player's as its ball opens
        Assert.Contains("cry Bidoof", start);
        Assert.Contains("cry Pikachu", start);
        Assert.True(start.IndexOf("send_out") < start.IndexOf("cry Pikachu"));

        var turn = Hear(() => Scenario.Turn(battle!, 0));
        Assert.Contains("cry Bidoof Faint", turn);
        Assert.True(turn.IndexOf("cry Bidoof Faint") < turn.IndexOf("faint"));
    }

    [Fact]
    public void APokemonSentOutHurtCriesInAPinch()
    {
        var hurt = Scenario.Mon("Pikachu", 30, "Tackle");
        hurt.CurrentHP = hurt.MaxHP / 3;
        Assert.Contains("cry Pikachu Pinch", Hear(() => Scenario.Battle(hurt, Scenario.Mon("Bidoof", 5))));
    }

    [Fact]
    public void AScriptMakesAPokemonCryInTheField()
    {
        var library = ScriptLibrary.FromSources(("test", "script S\n cry \"Shinx\"\n cry \"Giratina-Origin\"\nscript Wrong\n cry \"Nobody\""));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.Name == "S"));
        runner.RunToEnd();
        Assert.Equal(new[] { "cry Shinx", "cry Giratina-Origin" }, host.Log.Where(l => l.StartsWith("cry ")));
        Assert.Single(library.Problems(), p => p.Contains("Nobody"));
    }
}
