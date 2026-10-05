using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>The music: the song files, the director's choices, and the synthesiser's output, all without an audio device.</summary>
[Collection("MapDatabase")]
public class MusicTests
{
    private static float[] Render(Action<AudioMixer> start, double seconds)
    {
        var mixer = new AudioMixer();
        start(mixer);
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

    [Fact]
    public void TestEverySongFileParses()
    {
        Assert.NotEmpty(MusicLibrary.Ids);
        foreach (var song in MusicLibrary.All)
        {
            Assert.NotEmpty(song.Tracks);
            Assert.True(song.EndTick > 0, song.Id);
            Assert.True(song.EndTick % song.TicksPerBar == 0, $"{song.Id} ends mid-bar");
        }
    }

    [Fact]
    public void TestEveryMapPlaysASongThatExists()
    {
        MapDatabase.Initialize();
        foreach (var (place, track) in Themes())
            Assert.True(MusicLibrary.Exists(track), $"{place} plays '{track}', which is not in Data/music");
    }

    /// <summary>Every place with a theme of its own: each small map, and each open area of a map of the imported world.</summary>
    private static IEnumerable<(string Place, string Track)> Themes()
    {
        foreach (var name in MapDatabase.MapNames)
        {
            var map = MapDatabase.Get(name);
            if (!map.IsStreamed) yield return (name, map.BgmTrack);
            else
                foreach (var area in map.Areas.Where(a => a.Open)) yield return ($"{name}/{area.Key}", area.BgmTrack);
        }
    }

    [Fact]
    public void TestAreaThemesLoopAndFanfaresPlayOnce()
    {
        MapDatabase.Initialize();
        foreach (var id in Themes().Select(t => t.Track).Distinct())
        {
            var song = MusicLibrary.Get(id)!;
            Assert.True(song.Loops, $"{id} should loop");
            Assert.True(song.LoopDuration() >= 25, $"{id} loops after only {song.LoopDuration():0.0} s");
        }
        foreach (var role in new[] { MusicRole.FanfareHeal, MusicRole.FanfareItem, MusicRole.FanfareLevelUp, MusicRole.FanfarePokemon })
        {
            var song = MusicLibrary.Get(MusicDirector.Resolve(role, null, MusicLibrary.Exists)!)!;
            Assert.False(song.Loops, $"{song.Id} should play once");
            Assert.InRange(song.Duration(), 0.5, 8);
        }
    }

    [Fact]
    public void TestEveryRoleHasASongInEveryRegion()
    {
        foreach (var region in RegionDatabase.All.Select(r => r.Id).Append(null))
        {
            foreach (var role in Enum.GetValues<MusicRole>())
            {
                Assert.NotNull(MusicDirector.Resolve(role, region, MusicLibrary.Exists));
            }
        }
    }

    [Fact]
    public void TestRegionsUseTheirOwnThemesBeforeTheSharedOnes()
    {
        Assert.Equal("kanto/battle_wild", MusicDirector.Resolve(MusicRole.BattleWild, RegionDatabase.Kanto, MusicLibrary.Exists));
        Assert.Equal("common/battle_wild", MusicDirector.Resolve(MusicRole.BattleWild, RegionDatabase.Sinnoh, MusicLibrary.Exists));
        Assert.Equal("common/battle_wild", MusicDirector.Resolve(MusicRole.BattleWild, RegionDatabase.Johto, MusicLibrary.Exists));
    }

    [Fact]
    public void TestMoreSpecificRolesFallBackToGeneralOnes()
    {
        static bool OnlyShared(string id) => id is "common/battle_trainer" or "common/victory_trainer";

        Assert.Equal("common/battle_trainer", MusicDirector.Resolve(MusicRole.BattleRival, RegionDatabase.Sinnoh, OnlyShared));
        Assert.Equal("common/battle_trainer", MusicDirector.Resolve(MusicRole.BattleGymLeader, null, OnlyShared));
        Assert.Equal("common/victory_trainer", MusicDirector.Resolve(MusicRole.VictoryGymLeader, null, OnlyShared));
        Assert.Null(MusicDirector.Resolve(MusicRole.BattleWild, null, OnlyShared));
    }

    [Fact]
    public void TestBattleThemesFollowTheOpponent()
    {
        Assert.Equal(MusicRole.BattleWild, MusicDirector.BattleRole(Array.Empty<string>()));
        Assert.Equal(MusicRole.BattleTrainer, MusicDirector.BattleRole(new[] { "Youngster" }));
        Assert.Equal(MusicRole.BattleGymLeader, MusicDirector.BattleRole(new[] { "Lass", "Gym Leader" }));
        Assert.Equal(MusicRole.BattleRival, MusicDirector.BattleRole(new[] { "Rival" }));

        Assert.Equal(MusicRole.VictoryWild, MusicDirector.VictoryRole(MusicRole.BattleWild));
        Assert.Equal(MusicRole.VictoryTrainer, MusicDirector.VictoryRole(MusicRole.BattleRival));
        Assert.Equal(MusicRole.VictoryGymLeader, MusicDirector.VictoryRole(MusicRole.BattleGymLeader));
    }

    [Fact]
    public void TestNightArrangementsPlayAtNightOnly()
    {
        Assert.True(MusicDirector.IsNightArrangement(TimeOfDay.Night));
        Assert.True(MusicDirector.IsNightArrangement(TimeOfDay.LateNight));
        Assert.False(MusicDirector.IsNightArrangement(TimeOfDay.Morning));
        Assert.False(MusicDirector.IsNightArrangement(TimeOfDay.Day));
        Assert.False(MusicDirector.IsNightArrangement(TimeOfDay.Twilight));

        var twinleaf = MusicLibrary.Get("sinnoh/twinleaf")!;
        Assert.True(twinleaf.Duration(night: true) > twinleaf.Duration());
        Assert.Contains(twinleaf.Tracks, t => t.NightInstrument != null && t.NightInstrument != t.Instrument);
    }

    [Fact]
    public void TestEverySongRendersCleanly()
    {
        foreach (var song in MusicLibrary.All)
        {
            // The first stretch of each song: no NaNs, no clipping, and actually audible
            double seconds = Math.Min(song.Duration(), 6);
            var samples = Render(m =>
            {
                if (song.Loops) m.Play(song, night: false);
                else m.PlayFanfare(song);
            }, seconds);

            Assert.All(samples, s => Assert.True(float.IsFinite(s)));
            Assert.True(samples.Max(MathF.Abs) < 1f, $"{song.Id} clips");
            Assert.True(Rms(samples) > 0.01, $"{song.Id} is nearly silent");
        }
    }

    [Fact]
    public void TestSongsLoopBackToTheirLoopPoint()
    {
        var song = MusicLibrary.Get("common/battle_wild")!;
        Assert.True(song.LoopTick > 0, "the battle theme has an intro before its loop");

        var player = new SongPlayer();
        player.Start(song, atNight: false);
        int frames = (int)((song.Duration() + 1.0) * Synthesizer.SampleRate);
        var buffer = new float[2048];
        for (int done = 0; done < frames; done += 1024) player.Render(buffer, 1f);

        Assert.Equal(1, player.Loops);
        Assert.InRange(player.Position, song.LoopTick * song.SecondsPerTick(false) + 0.9, song.LoopTick * song.SecondsPerTick(false) + 1.1);
        Assert.True(player.Playing);
    }

    [Fact]
    public void TestTheSameSongCarriesOnAndADifferentOneFadesIn()
    {
        var twinleaf = MusicLibrary.Get("sinnoh/twinleaf")!;
        var route = MusicLibrary.Get("sinnoh/route201")!;
        var mixer = new AudioMixer();
        var buffer = new float[2048];
        void Run(double seconds)
        {
            for (int done = 0; done < seconds * Synthesizer.SampleRate; done += 1024) mixer.Render(buffer);
        }

        mixer.Play(twinleaf, night: false);
        Run(1);
        mixer.Play(twinleaf, night: false);
        Assert.Equal("sinnoh/twinleaf", mixer.CurrentId);

        mixer.Play(route, night: false);
        Assert.Equal("sinnoh/route201", mixer.CurrentId);
        Run(1);
        Assert.Equal("sinnoh/route201", mixer.CurrentId);
    }

    [Fact]
    public void TestAFanfarePausesTheMusicAndLetsItResume()
    {
        var twinleaf = MusicLibrary.Get("sinnoh/twinleaf")!;
        var heal = MusicLibrary.Get("common/fanfare_heal")!;
        var mixer = new AudioMixer();
        var player = (SongPlayer)typeof(AudioMixer).GetField("music", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(mixer)!;
        var buffer = new float[2048];
        void Run(double seconds)
        {
            for (int done = 0; done < seconds * Synthesizer.SampleRate; done += 1024) mixer.Render(buffer);
        }

        mixer.Play(twinleaf, night: false);
        Run(3);
        mixer.PlayFanfare(heal);
        Run(0.3);
        double held = player.Position;
        Run(1.5);
        Assert.True(mixer.FanfarePlaying);
        Assert.Equal(held, player.Position, 3);

        Run(heal.Duration() + 0.5);
        Assert.False(mixer.FanfarePlaying);
        Assert.True(player.Position > held, "the music carries on from where it stopped");
        Assert.Equal("sinnoh/twinleaf", mixer.CurrentId);
    }

    [Fact]
    public void TestMutedMusicIsSilent()
    {
        var samples = Render(m =>
        {
            m.Muted = true;
            m.Play(MusicLibrary.Get("sinnoh/twinleaf")!, night: false);
        }, 2);
        Assert.All(samples, s => Assert.Equal(0f, s));
    }

    [Fact]
    public void TestMmlNotesLengthsTiesChordsAndRepeats()
    {
        var song = Mml.Parse("test", """
            tempo 120
            track a flute
              o4 l8 c d4 e4. f^16 g16 | (c e g)2 [a b]2 |
            """);
        var notes = song.Tracks[0].Notes;

        Assert.Equal(new[] { 60, 62, 64, 65, 67, 60, 64, 67, 69, 71, 69, 71 }, notes.Select(n => n.Key));
        Assert.Equal(new[] { 24, 48, 72, 36, 12 }, notes.Take(5).Select(n => n.Length));
        Assert.Equal(new[] { 0, 24, 72, 144, 180 }, notes.Take(5).Select(n => n.Tick));
        Assert.Equal(3, notes.Count(n => n.Tick == 192));
        Assert.Equal(384, song.EndTick);
        Assert.False(song.Loops);
    }

    [Fact]
    public void TestMmlCatchesMisalignedBarsAndUnevenTracks()
    {
        var bar = Assert.Throws<FormatException>(() => Mml.Parse("test", """
            track a flute
              c4 d4 e4 | f4 |
            """));
        Assert.Contains("bar", bar.Message);

        var uneven = Assert.Throws<FormatException>(() => Mml.Parse("test", """
            track a flute
              c1 |
            track b bass
              c1 | c1 |
            """));
        Assert.Contains("bars long", uneven.Message);

        Assert.Throws<FormatException>(() => Mml.Parse("test", """
            track a kazoo
              c1 |
            """));
    }

    [Fact]
    public void TestMmlLoopPointAndDrums()
    {
        var song = Mml.Parse("test", """
            meter 3/4
            track lead flute
              c2. | L d2. |
            track drums kit
              b4 s4 h4 | L (b c)4 x4 k4 |
            """);

        Assert.Equal(144, song.TicksPerBar);
        Assert.Equal(144, song.LoopTick);
        Assert.Equal(288, song.EndTick);
        Assert.Equal(new[] { Drum.Kick, Drum.Snare, Drum.HiHat, Drum.Kick, Drum.Crash, Drum.Clap, Drum.Shaker },
            song.Tracks[1].Notes.Select(n => (Drum)n.Key));
    }

    // ------------------------------------------------------------------ plan 05 · A2: the switching rules

    [Fact]
    public void TestLowHpArrangementSwitchesMidSongAndKeepsItsPlace()
    {
        var song = Mml.Parse("test", """
            tempo 120
            lowhptempo 1.5
            track lead flute lowhp=sawlead
              L [ c4 d4 e4 f4 | ]4
            track pad strings unless=lowhp
              L [ (c e g)1 | ]4
            track alarm pulse only=lowhp
              L [ c8 r8 c8 r8 c8 r8 c8 r8 | ]4
            """);
        Assert.Equal(1.5, song.LowHpTempo);
        Assert.Same(InstrumentBank.Get("sawlead"), song.Tracks[0].LowHpInstrument);
        Assert.Equal(new[] { TrackWhen.Always, TrackWhen.NotLowHp, TrackWhen.LowHp }, song.Tracks.Select(t => t.When));
        Assert.Throws<FormatException>(() => Mml.Parse("test", "track a flute only=night\n  c1 |\n"));

        var player = new SongPlayer();
        var buffer = new float[2048];
        void Run(double seconds)
        {
            for (int done = 0; done < seconds * Synthesizer.SampleRate; done += 1024) player.Render(buffer, 1f);
        }

        // Switched on in the middle: the song keeps its place and goes on half as fast again
        player.Start(song, atNight: false);
        Run(1.0);
        double at = player.Position;
        Assert.InRange(at, 0.95, 1.05);
        player.Agitated = true;
        Assert.Equal(at, player.Position, 6);
        Run(1.0);
        Assert.InRange(player.Position - at, 1.45, 1.55);
        player.Agitated = false;
        Run(1.0);
        Assert.InRange(player.Position - at, 2.45, 2.55);
    }

    [Fact]
    public void TestTheArrangementsTracksPlayOnlyInTheirArrangement()
    {
        static double Loudness(string track, bool agitated)
        {
            var song = Mml.Parse("test", "tempo 120\n" + track + "\n  L c4 e4 g4 >c4 |\n");
            var player = new SongPlayer { Agitated = agitated };
            player.Start(song, atNight: false);
            var buffer = new float[Synthesizer.SampleRate * 2];
            player.Render(buffer, 1f);
            return buffer.Sum(x => Math.Abs(x));
        }

        Assert.Equal(0, Loudness("track alarm pulse only=lowhp", agitated: false));
        Assert.True(Loudness("track alarm pulse only=lowhp", agitated: true) > 1);
        Assert.True(Loudness("track pad strings unless=lowhp", agitated: false) > 1);
        Assert.Equal(0, Loudness("track pad strings unless=lowhp", agitated: true));
        Assert.True(Loudness("track lead flute", agitated: true) > 1);
    }

    [Fact]
    public void TestEveryBattleThemeHasALowHpArrangement()
    {
        foreach (var role in new[] { MusicRole.BattleWild, MusicRole.BattleTrainer, MusicRole.BattleGymLeader })
        {
            foreach (var region in RegionDatabase.All.Select(r => r.Id))
            {
                var song = MusicLibrary.Get(MusicDirector.Resolve(role, region, MusicLibrary.Exists)!)!;
                Assert.True(song.LowHpTempo > 1.0, $"{song.Id} plays no faster in the red");
                Assert.Contains(song.Tracks, t => t.When == TrackWhen.LowHp);
                Assert.Contains(song.Tracks, t => t.LowHpInstrument != null);
            }
        }
    }

    [Fact]
    public void TestTheMixerPassesLowHpToTheMusicAndKeepsItAcrossSongs()
    {
        var mixer = new AudioMixer();
        var player = (SongPlayer)typeof(AudioMixer).GetField("music", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(mixer)!;
        mixer.Play(MusicLibrary.Get("common/battle_wild")!, night: false, immediate: true);
        Assert.False(player.Agitated);
        mixer.LowHp = true;
        Assert.True(player.Agitated);
        mixer.Play(MusicLibrary.Get("common/battle_trainer")!, night: false, immediate: true);
        Assert.True(mixer.LowHp);
        mixer.LowHp = false;
        Assert.False(player.Agitated);
    }

    [Fact]
    public void TestEyeThemesFollowTheSoundMap()
    {
        Assert.Equal(MusicRole.EyeBoy, MusicDirector.EyeRole("Youngster"));
        Assert.Equal(MusicRole.EyeGirl, MusicDirector.EyeRole("Lass"));
        Assert.Equal(MusicRole.EyeMountain, MusicDirector.EyeRole("Hiker"));
        Assert.Equal(MusicRole.EyeGalactic, MusicDirector.EyeRole("Galactic Grunt"));
        Assert.Equal(MusicRole.EyeChampion, MusicDirector.EyeRole("Champion"));
        Assert.Equal(MusicRole.EyeBoy, MusicDirector.EyeRole("Nobody"));
        Assert.Equal(MusicRole.EyeBoy, MusicDirector.EyeRole(null));

        // Every value the map gives is a theme the director knows, and every eye theme resolves to a song
        foreach (var (cls, theme) in SoundMap.EyeThemes) Assert.True(MusicDirector.EyeThemes.ContainsKey(theme), $"{cls}: '{theme}'");
        foreach (var (cls, theme) in SoundMap.BattleThemes) Assert.True(MusicDirector.BattleThemes.ContainsKey(theme), $"{cls}: '{theme}'");
        foreach (var role in Enum.GetValues<MusicRole>().Where(r => r.ToString().StartsWith("Eye")))
            Assert.NotNull(MusicDirector.Resolve(role, null, MusicLibrary.Exists));
        Assert.Equal("common/eye_girl", MusicDirector.Resolve(MusicRole.EyeLady, null, MusicLibrary.Exists));
        Assert.Equal("common/eye_boy", MusicDirector.Resolve(MusicRole.EyeChampion, null, MusicLibrary.Exists));

        // Every trainer class the game's maps use is in the map
        MapDatabase.Initialize();
        var classes = MapDatabase.MapNames.SelectMany(n => MapDatabase.Get(n).Everyone)
            .Select(n => n.TrainerData?.TrainerClass).Where(c => c != null && c != "Rival").Distinct().ToList();
        Assert.NotEmpty(classes);
        foreach (var cls in classes) Assert.True(SoundMap.EyeThemes.ContainsKey(cls!), $"the sound map has no eye theme for '{cls}'");
    }

    [Fact]
    public void TestBattleThemesFollowTheSoundMapAndTheMostImportantOpponent()
    {
        Assert.Equal(MusicRole.BattleGalactic, MusicDirector.BattleRole(new[] { "Galactic Grunt" }));
        Assert.Equal(MusicRole.BattleChampion, MusicDirector.BattleRole(new[] { "Youngster", "Champion" }));
        Assert.Equal(MusicRole.BattleGymLeader, MusicDirector.BattleRole(new[] { "Galactic Grunt", "Gym Leader" }));
        Assert.Equal(MusicRole.BattleTrainer, MusicDirector.BattleRole(new[] { "Nobody" }));

        // Roles without a song of their own fall back to the nearest that has one
        Assert.Equal("common/battle_trainer", MusicDirector.Resolve(MusicRole.BattleGalacticBoss, null, MusicLibrary.Exists));
        Assert.Equal("common/battle_gym", MusicDirector.Resolve(MusicRole.BattleChampion, null, MusicLibrary.Exists));
        Assert.Equal("common/battle_wild", MusicDirector.Resolve(MusicRole.BattleLegendary, null, MusicLibrary.Exists));
        Assert.Equal(MusicRole.VictoryGymLeader, MusicDirector.VictoryRole(MusicRole.BattleChampion));
        Assert.Equal(MusicRole.VictoryTrainer, MusicDirector.VictoryRole(MusicRole.BattleGalactic));
        Assert.Equal(MusicRole.VictoryWild, MusicDirector.VictoryRole(MusicRole.BattleLegendary));
    }

    [Fact]
    public void TestRidingThemesExistAndLoop()
    {
        foreach (var role in new[] { MusicRole.Surf, MusicRole.Bicycle, MusicRole.EyeBoy, MusicRole.EyeGirl })
        {
            var song = MusicLibrary.Get(MusicDirector.Resolve(role, null, MusicLibrary.Exists)!)!;
            Assert.True(song.Loops, $"{song.Id} should loop");
            Assert.True(song.LoopDuration() >= 8, $"{song.Id} loops after only {song.LoopDuration():0.0} s");
        }
    }
}
