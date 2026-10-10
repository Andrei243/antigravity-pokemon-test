using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>The sound effects and the mixer's buses, all without an audio device.</summary>
public class SoundTests
{
    private static float[] Render(AudioMixer mixer, double seconds)
    {
        var buffer = new float[(int)(seconds * Synthesizer.SampleRate) * 2];
        for (int done = 0; done < buffer.Length; done += 2048)
            mixer.Render(buffer.AsSpan(done, Math.Min(2048, buffer.Length - done)));
        return buffer;
    }

    private static double Rms(float[] samples, int from = 0, int to = -1)
    {
        if (to < 0) to = samples.Length;
        double sum = 0;
        for (int i = from; i < to; i++) sum += samples[i] * samples[i];
        return Math.Sqrt(sum / Math.Max(1, to - from));
    }

    private static int Frames(double seconds) => (int)(seconds * Synthesizer.SampleRate) * 2;

    [Fact]
    public void EverySoundRendersCleanlyAndNeverClicks()
    {
        Assert.NotEmpty(SoundBank.Names);
        Assert.Equal(SoundBank.Names, AudioManager.SoundNames);
        Assert.Null(SoundBank.Get("kazoo"));
        foreach (var name in SoundBank.Names)
        {
            var sound = SoundBank.Get(name);
            Assert.NotNull(sound);
            Assert.Same(sound, SoundBank.Get(name.ToUpperInvariant()));
            var s = sound!.Samples;
            Assert.All(s, x => Assert.True(float.IsFinite(x)));
            // Thunder alone rolls on for seconds
            Assert.InRange(sound.Duration, 0.02, name.StartsWith("thunder") ? 3.5 : 1.0);
            float peak = s.Max(MathF.Abs);
            Assert.True(peak is >= 0.3f and <= 0.6f, $"{name} peaks at {peak}");
            Assert.True(Rms(s) > 0.02, $"{name} is nearly silent");
            // A sound's ends are rounded off, so it starts and stops without a click
            Assert.True(MathF.Abs(s[0]) < 0.02f && MathF.Abs(s[^1]) < 0.02f, $"{name} clicks");
            Assert.True(MathF.Abs(s.Average()) < 0.05f, $"{name} has a DC offset");

            // As loud as the sound bus's window, as the mixer plays it (plan 05 · A7)
            var mixer = new AudioMixer();
            mixer.PlaySound(sound, AudioBus.Sound);
            var (low, high) = Loudness.Target(AudioBus.Sound);
            double loud = Loudness.Of(Render(mixer, sound.Duration + 0.1), 2);
            Assert.True(loud >= low && loud <= high, $"{name} measures {loud:0.0} dB, outside {low} to {high}");
        }
    }

    [Fact]
    public void ASoundPlaysOverTheMusicAndTheMusicGoesOn()
    {
        var twinleaf = MusicLibrary.Get("sinnoh/twinleaf")!;
        var select = SoundBank.Get("select")!;

        var alone = new AudioMixer();
        alone.Play(twinleaf, night: false);
        var musicOnly = Render(alone, 1.5);

        var mixer = new AudioMixer();
        mixer.Play(twinleaf, night: false);
        Render(mixer, 1.0);
        mixer.PlaySound(select);
        Assert.Equal(1, mixer.SoundsPlaying);
        var withSound = Render(mixer, 0.5);

        // The same music underneath, with the sound added on top for as long as it lasts
        int soundEnd = Frames(select.Duration);
        Assert.Equal("sinnoh/twinleaf", mixer.CurrentId);
        Assert.Equal(0, mixer.SoundsPlaying);
        Assert.NotEqual(musicOnly.Skip(Frames(1.0)).Take(soundEnd), withSound.Take(soundEnd));
        // (the high-pass filter remembers the sound for a few milliseconds, so compare a little later, nearly)
        var later = musicOnly.Skip(Frames(1.0) + soundEnd + 1024).Take(2048).Zip(withSound.Skip(soundEnd + 1024).Take(2048), (a, b) => MathF.Abs(a - b));
        Assert.True(later.Max() < 1e-4f);
    }

    [Fact]
    public void ASoundAloneEndsInSilence()
    {
        var mixer = new AudioMixer();
        var bump = SoundBank.Get("bump")!;
        mixer.PlaySound(bump);
        var samples = Render(mixer, 0.5);
        int end = Frames(bump.Duration);
        Assert.True(Rms(samples, 0, end) > 0.02);
        Assert.True(Rms(samples, end + Frames(0.05)) < 0.0005);
    }

    [Fact]
    public void BusVolumesApplyToTheirOwnBusOnly()
    {
        var twinleaf = MusicLibrary.Get("sinnoh/twinleaf")!;
        var heal = SoundBank.Get("heal")!;

        var reference = new AudioMixer();
        reference.Play(twinleaf, night: false);
        var musicOnly = Render(reference, 1.0);

        // The sound effects' bus turned down leaves the music exactly as it was
        var quietSounds = new AudioMixer();
        quietSounds.SetVolume(AudioBus.Sound, 0f);
        quietSounds.Play(twinleaf, night: false);
        quietSounds.PlaySound(heal);
        Assert.Equal(musicOnly, Render(quietSounds, 1.0));

        // The music's bus turned down leaves the sound alone
        var soundOnly = new AudioMixer();
        soundOnly.PlaySound(heal);
        var expected = Render(soundOnly, 1.0);
        var quietMusic = new AudioMixer();
        quietMusic.SetVolume(AudioBus.Music, 0f);
        quietMusic.Play(twinleaf, night: false);
        quietMusic.PlaySound(heal);
        Assert.Equal(expected, Render(quietMusic, 1.0));

        // Half volume is half the amplitude, before the limiter
        var half = new AudioMixer();
        half.SetVolume(AudioBus.Sound, 0.5f);
        half.PlaySound(heal);
        var halved = Render(half, 0.2);
        var loud = new AudioMixer();
        loud.PlaySound(heal);
        var full = Render(loud, 0.2);
        Assert.InRange(Rms(halved) / Rms(full), 0.49, 0.51);
        Assert.Equal(0.5f, half.VolumeOf(AudioBus.Sound));
    }

    [Fact]
    public void ACryDucksTheMusicAndItComesBack()
    {
        var twinleaf = MusicLibrary.Get("sinnoh/twinleaf")!;
        var reference = new AudioMixer();
        reference.Play(twinleaf, night: false);
        var plain = Render(reference, 4.0);

        // A silent "cry" a second long: what it does to the music is all that is heard
        var silentCry = new SoundSample("silence", new float[Synthesizer.SampleRate]);
        var mixer = new AudioMixer();
        mixer.Play(twinleaf, night: false);
        Render(mixer, 2.0);
        mixer.PlaySound(silentCry, AudioBus.Cry);
        var ducked = Render(mixer, 2.0);

        int from = Frames(2.0);
        double during = Rms(ducked, Frames(0.3), Frames(0.9)) / Rms(plain, from + Frames(0.3), from + Frames(0.9));
        double after = Rms(ducked, Frames(1.7), Frames(2.0)) / Rms(plain, from + Frames(1.7), from + Frames(2.0));
        Assert.InRange(during, AudioMixer.CryDuckLevel - 0.08, AudioMixer.CryDuckLevel + 0.08);
        Assert.InRange(after, 0.9, 1.1);

        // An effect on the sound bus doesn't dip the music
        var effects = new AudioMixer();
        effects.Play(twinleaf, night: false);
        Render(effects, 2.0);
        effects.PlaySound(silentCry, AudioBus.Sound);
        var level = Render(effects, 1.0);
        Assert.Equal(plain.Skip(from).Take(level.Length), level);
    }

    [Fact]
    public void EverySoundAtOnceStaysUnderTheCeiling()
    {
        var mixer = new AudioMixer();
        foreach (var name in SoundBank.Names) mixer.PlaySound(SoundBank.Get(name)!);
        Assert.Equal(Math.Min(AudioMixer.MaxSounds, SoundBank.Names.Length), mixer.SoundsPlaying);
        var samples = Render(mixer, SoundBank.Names.Max(n => SoundBank.Get(n)!.Duration) + 0.1);
        Assert.All(samples, s => Assert.True(float.IsFinite(s)));
        Assert.True(samples.Max(MathF.Abs) < 1f);
        Assert.Equal(0, mixer.SoundsPlaying);
    }

    [Fact]
    public void AFullBankTakesTheSoundNearestItsEnd()
    {
        var mixer = new AudioMixer();
        var faint = SoundBank.Get("faint")!;
        var cursor = SoundBank.Get("cursor")!;
        for (int i = 0; i < AudioMixer.MaxSounds - 1; i++) mixer.PlaySound(faint);
        mixer.PlaySound(cursor);
        Render(mixer, 0.02);
        mixer.PlaySound(faint);
        Assert.Equal(AudioMixer.MaxSounds, mixer.SoundsPlaying);
        // The cursor blip (40 ms) was the one nearest its end, so it was replaced; the faints all play on
        Render(mixer, 0.03);
        Assert.Equal(AudioMixer.MaxSounds, mixer.SoundsPlaying);
        mixer.StopSounds();
        Assert.Equal(0, mixer.SoundsPlaying);
    }

    [Fact]
    public void PanningSendsASoundToOneSide()
    {
        var mixer = new AudioMixer();
        mixer.PlaySound(SoundBank.Get("levelup")!, pan: -1f);
        var left = Render(mixer, 0.4);
        double l = 0, r = 0;
        for (int i = 0; i < left.Length; i += 2) { l += left[i] * left[i]; r += left[i + 1] * left[i + 1]; }
        Assert.True(l > 0 && r < l * 1e-6);
    }

    [Fact]
    public void MutedSoundsAreSilent()
    {
        var mixer = new AudioMixer { Muted = true };
        mixer.PlaySound(SoundBank.Get("select")!);
        Assert.All(Render(mixer, 0.2), s => Assert.Equal(0f, s));
    }

    [Fact]
    public void TheOptionsVolumesStopAtTheirEnds()
    {
        var s = new GameSettings();
        Assert.Equal((100, 100), (s.MusicVolume, s.SoundVolume));
        OptionsScreen.Change(s, OptionRow.MusicVolume, 1);
        Assert.Equal(100, s.MusicVolume);
        OptionsScreen.Change(s, OptionRow.MusicVolume, -1);
        Assert.Equal(90, s.MusicVolume);
        for (int i = 0; i < 12; i++) OptionsScreen.Change(s, OptionRow.SoundVolume, -1);
        Assert.Equal(0, s.SoundVolume);
        Assert.Equal(90, s.MusicVolume);
        OptionsScreen.Change(s, OptionRow.Sound, 1);
        Assert.True(s.Muted);

        // A settings file from before the volumes existed plays everything at full volume
        var old = System.Text.Json.JsonSerializer.Deserialize<GameSettings>("""{ "Muted": false, "VSync": true }""")!;
        Assert.Equal((100, 100), (old.MusicVolume, old.SoundVolume));
    }

    // ------------------------------------------------------------------ the set (plan 05 · A3)

    [Fact]
    public void TheBankHasTheWholeSetThePlanAsksFor()
    {
        Assert.Equal(SoundBank.Names.Length, SoundBank.Names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var group in Enum.GetValues<SoundGroup>()) Assert.Contains(SoundBank.Entries, e => e.Group == group);
        Assert.All(SoundBank.Entries, e =>
        {
            Assert.Matches("^[a-z_]+$", e.Name);
            Assert.False(string.IsNullOrWhiteSpace(e.PlayedWhen), e.Name);
            // Only the original's names are kept, never its sounds
            // The decompilation names most of its sounds SEQ_SE_DP_..., and a few by the sequence itself (SE_DP_..._sseq)
            if (e.Original != null) Assert.Matches("^(SEQ_SE_(DP|PL)_[A-Z0-9_]+|SE_(DP|PL)_[A-Z0-9_]+_sseq)$", e.Original);
        });

        // A sound for every type's moves and for every status condition a Pokémon can be given
        foreach (var type in Enum.GetValues<PokemonType>()) Assert.True(SoundBank.Exists(SoundBank.MoveSound(type)), type.ToString());
        foreach (var status in Enum.GetValues<StatusCondition>().Where(s => s is not StatusCondition.None and not StatusCondition.Faint))
            Assert.True(SoundBank.StatusSound(status) is { } name && SoundBank.Exists(name), status.ToString());
        Assert.Null(SoundBank.StatusSound(StatusCondition.None));

        // The checklist of the plan, by group
        foreach (var name in new[] { "cursor", "select", "cancel", "error", "menu_open", "menu_close", "page", "text",
                     "door_open", "door_close", "stairs", "warp", "bump", "ledge", "grass", "bike_bell", "gear", "surf", "step_puddle",
                     "boulder", "rock_smash", "cut", "fish_cast", "fish_bite", "fish_reel", "poketch", "pc_on", "pc_off", "save", "exclaim",
                     "send_out", "recall", "ball_throw", "ball_shake", "ball_click", "ball_break", "hit_normal", "hit_super", "hit_weak",
                     "stat_up", "stat_down", "faint", "exp", "run_away" })
            Assert.True(SoundBank.Exists(name), name);
    }

    [Fact]
    public void EverySoundTheCodeAsksForIsInTheBank()
    {
        string repo = Repo();
        var asked = new Regex(@"(?:PlaySound|Sound)\(\s*""([a-z_]+)""");
        var unknown = new List<string>();
        int found = 0;
        foreach (var file in Directory.EnumerateFiles(Path.Combine(repo, "PokemonPlatinumEngine"), "*.cs", SearchOption.AllDirectories))
            foreach (Match m in asked.Matches(File.ReadAllText(file)))
            {
                found++;
                if (!SoundBank.Exists(m.Groups[1].Value)) unknown.Add($"{Path.GetFileName(file)}: {m.Groups[1].Value}");
            }
        Assert.True(found > 50);
        Assert.Empty(unknown);
    }

    [Fact]
    public void TheListOfSoundsNamesEverySound()
    {
        string doc = File.ReadAllText(Path.Combine(Repo(), "docs", "sound-effects.md"));
        Assert.All(SoundBank.Names, name => Assert.Contains($"`{name}`", doc));
    }

    private static string Repo()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("The tests don't run from inside the repository");
    }

    [Fact]
    public void AStepSoundsAsTheOriginalsDo()
    {
        Assert.Equal("grass", SoundBank.StepSound(TileBehavior.VeryTallGrass));
        Assert.Null(SoundBank.StepSound(TileBehavior.TallGrass));
        Assert.Equal("step_snow", SoundBank.StepSound(TileBehavior.ShallowSnow));
        Assert.Equal("step_snow", SoundBank.StepSound(TileBehavior.DeepSnow));
        Assert.Equal("step_puddle", SoundBank.StepSound(TileBehavior.Puddle));
        Assert.Null(SoundBank.StepSound(TileBehavior.StillPuddle));
        Assert.Equal("step_shallows", SoundBank.StepSound(TileBehavior.ShallowWater));
        Assert.Equal("step_mud", SoundBank.StepSound(TileBehavior.Mud));
        // Deep mud holds the feet fast: the original plays nothing as one sinks in, nor on sand
        Assert.Null(SoundBank.StepSound(TileBehavior.DeepMud));
        Assert.Null(SoundBank.StepSound(TileBehavior.Sand));
    }

    // ------------------------------------------------------------------ a battle, heard

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
    public void AMoveIsHeardAsItSetsOffAndAsItLands()
    {
        var battle = Scenario.Battle(Scenario.Mon("Pikachu", 30, "Thunder Wave", "Growl", "Supersonic", "Tackle"), Scenario.Mon("Bidoof", 30));
        // Each move's sound as it sets off, then what it did (the foe's own idle move is heard too)
        void Heard(int move, string launch, string then)
        {
            var heard = Hear(() => Scenario.Turn(battle, move));
            Assert.Contains(launch, heard);
            Assert.Contains(then, heard);
            Assert.True(heard.IndexOf(launch) < heard.IndexOf(then), string.Join(", ", heard));
        }
        Heard(0, "move_electric", "status_paralysis");
        Heard(1, "move_normal", "stat_down");
        Heard(2, "move_normal", "status_confusion");
        var tackle = Hear(() => Scenario.Turn(battle, 3));
        Assert.Equal(new[] { "move_normal", "hit_normal" }, tackle.Where(s => s.StartsWith("move_") || s.StartsWith("hit_")));
    }

    [Theory]
    [InlineData("Squirtle", "Water Gun", "Charmander", "hit_super")]
    [InlineData("Charmander", "Ember", "Squirtle", "hit_weak")]
    [InlineData("Squirtle", "Tackle", "Charmander", "hit_normal")]
    public void AHitSoundsAsHardAsItLands(string mine, string move, string foe, string expected)
    {
        var battle = Scenario.Battle(Scenario.Mon(mine, 20, move), Scenario.Mon(foe, 20));
        var heard = Hear(() => Scenario.Turn(battle, 0));
        Assert.Equal(new[] { expected }, heard.Where(s => s.StartsWith("hit_")));
    }

    [Theory]
    [InlineData("Master Ball", 4)]
    public void AThrownBallIsHeardWobbleByWobble(string ball, int shakes)
    {
        var party = new Party();
        party.Add(Scenario.Mon("Pikachu", 30, "Tackle"));
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(ball)!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Scenario.Mon("Bidoof", 5) }, Random = Scenario.Steady(), Rules = Ruleset.Platinum
        });
        Scenario.Settle(battle);

        var heard = Hear(() =>
        {
            battle.UseItem(ItemDatabase.Get(ball)!);
            for (int i = 0; i < 600 && !battle.IsBattleOver; i++)
            {
                if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
                battle.Update(1f / 60f);
            }
        });
        var ballSounds = heard.Where(s => s.StartsWith("ball_")).ToList();
        Assert.Equal(new[] { "ball_throw" }.Concat(Enumerable.Repeat("ball_shake", Math.Min(shakes, 3))).Append(shakes >= 4 ? "ball_click" : "ball_break"), ballSounds);
    }

    [Fact]
    public void RunningAwayIsHeard()
    {
        var battle = Scenario.Battle(Scenario.Mon("Pikachu", 30, "Tackle"), Scenario.Mon("Bidoof", 5));
        var heard = Hear(() =>
        {
            battle.SelectMainMenuOption(3);
            Scenario.Settle(battle);
        });
        Assert.Contains("run_away", heard);
    }

    [Fact]
    public void TheSendOutIsHeardWhenTheBallOpens()
    {
        Assert.Contains("send_out", Hear(() => Scenario.Battle(Scenario.Mon("Pikachu", 30, "Tackle"), Scenario.Mon("Bidoof", 5))));
    }
}
