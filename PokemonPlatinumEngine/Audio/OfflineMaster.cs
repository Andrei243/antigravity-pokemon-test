using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Plan 31 · MU0: a song rendered in either sound with no mixer, for <c>tools/MusicRender</c> and the tests to compare
/// the DS sound with the modern one before the modern sound reaches the game (MU2). It plays the song through a
/// <see cref="SongPlayer"/> of that sound at the music bus's full volume and ends as the mixer ends: the same 30 Hz
/// high-pass and the same soft limiter, at the player's own rate, so a song measures the same here in the DS sound as
/// through <see cref="AudioMixer"/> with nothing else playing.
/// </summary>
public static class OfflineMaster
{
    /// <summary>
    /// The song's first <paramref name="seconds"/> as interleaved stereo at the sound's rate, the arrangement chosen
    /// (night, low HP, giant), at the mixer's <paramref name="volume"/>.
    /// </summary>
    public static float[] Render(Song song, SynthSound sound, double seconds, bool night = false, bool lowHp = false, bool giant = false, float volume = 0.8f) =>
        Render(song, sound, seconds, out _, night, lowHp, giant, volume);

    /// <summary>As <see cref="Render(Song, SynthSound, double, bool, bool, bool, float)"/>, with the player afterwards, to read how many voices it needed.</summary>
    public static float[] Render(Song song, SynthSound sound, double seconds, out SongPlayer played, bool night = false, bool lowHp = false, bool giant = false, float volume = 0.8f)
    {
        var player = new SongPlayer(sound) { Agitated = lowHp, Giant = giant };
        played = player;
        player.Start(song, night);
        int rate = player.Rate;
        int frames = (int)(seconds * rate);
        var samples = new float[frames * 2];
        const int chunk = 256;
        for (int done = 0; done < frames; done += chunk)
        {
            int n = Math.Min(chunk, frames - done);
            player.Render(samples.AsSpan(done * 2, n * 2), 1f);
        }
        Finish(samples, rate, volume);
        return samples;
    }

    /// <summary>The mixer's last stage at <paramref name="rate"/>: a high-pass at about 30 Hz, the volume, and the limiter that rounds off peaks over ±0.8.</summary>
    public static void Finish(Span<float> samples, int rate, float volume)
    {
        float coef = 1f / (1f + MathF.Tau * 30f / rate);
        Span<float> highPass = stackalloc float[2];
        Span<float> lastInput = stackalloc float[2];
        for (int i = 0; i < samples.Length; i++)
        {
            int ch = i & 1;
            float input = samples[i];
            highPass[ch] = coef * (highPass[ch] + input - lastInput[ch]);
            lastInput[ch] = input;
            float x = highPass[ch] * volume;
            float ax = MathF.Abs(x);
            if (ax > 0.8f) x = MathF.Sign(x) * (0.8f + 0.2f * MathF.Tanh((ax - 0.8f) / 0.2f));
            samples[i] = x;
        }
    }
}
