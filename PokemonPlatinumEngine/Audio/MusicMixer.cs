using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Mixes the background music and fanfares, the way the DS games do: a new area's theme fades the old one out
/// first, and a fanfare (healing, an item, a level-up) silences the music, plays, and lets the music come back
/// where it left off. Rendering runs on the audio thread, commands on the game thread, so both take the lock.
/// </summary>
public sealed class MusicMixer
{
    private const float FadeOutTime = 0.45f;
    private const float FanfareDuckTime = 0.12f;
    private const float ResumeTime = 0.6f;
    private const float FanfareTail = 0.35f;

    private readonly object gate = new();
    private readonly SongPlayer music = new();
    private readonly SongPlayer fanfare = new();
    private Song? pending;
    private bool pendingNight;
    private float musicGain = 1f;
    private float fadeTarget = 1f;
    private float fadeRate;
    private bool fanfareActive;
    private float fanfareTail;
    private static readonly float HighPassCoef = 1f / (1f + MathF.Tau * 30f / Synthesizer.SampleRate);
    private readonly float[] highPass = new float[2];
    private readonly float[] lastInput = new float[2];

    /// <summary>Overall music volume (0 to 1).</summary>
    public float Volume { get; set; } = 0.8f;

    public bool Muted { get; set; }

    /// <summary>The song that is playing or about to, or null.</summary>
    public string? CurrentId
    {
        get { lock (gate) return (pending ?? music.Song)?.Id; }
    }

    public bool FanfarePlaying
    {
        get { lock (gate) return fanfareActive; }
    }

    /// <summary>
    /// Switches to <paramref name="song"/>. The same song keeps playing without a restart (unless the time of day
    /// changes its arrangement); a different one starts once the current one has faded out, or at once when
    /// <paramref name="immediate"/> (a battle starting), which also restarts the same song.
    /// </summary>
    public void Play(Song song, bool night, bool immediate = false)
    {
        lock (gate)
        {
            var current = pending ?? music.Song;
            bool samePending = pending != null && pending.Id == song.Id && pendingNight == night;
            if (!immediate && (samePending || (pending == null && current?.Id == song.Id && music.Night == night && music.Playing)))
                return;

            pending = song;
            pendingNight = night;
            if (immediate || music.Song == null || musicGain <= 0.001f)
            {
                StartPending();
            }
            else
            {
                fadeTarget = 0f;
                fadeRate = 1f / FadeOutTime;
            }
        }
    }

    /// <summary>Fades the music out and leaves silence.</summary>
    public void Stop()
    {
        lock (gate)
        {
            pending = null;
            fadeTarget = 0f;
            fadeRate = 1f / FadeOutTime;
        }
    }

    /// <summary>Pauses the music, plays <paramref name="jingle"/> once, then brings the music back.</summary>
    public void PlayFanfare(Song jingle)
    {
        lock (gate)
        {
            fanfare.Start(jingle, atNight: false);
            fanfareActive = true;
            fanfareTail = 0f;
            fadeTarget = 0f;
            fadeRate = 1f / FanfareDuckTime;
        }
    }

    private void StartPending()
    {
        music.Start(pending!, pendingNight);
        pending = null;
        musicGain = fanfareActive ? 0f : 1f;
        fadeTarget = musicGain;
    }

    /// <summary>Fills <paramref name="output"/> (interleaved stereo) with the next stretch of music.</summary>
    public void Render(Span<float> output)
    {
        output.Clear();
        lock (gate)
        {
            int frames = output.Length / 2;
            const int chunk = 256;
            for (int done = 0; done < frames; done += chunk)
            {
                int n = Math.Min(chunk, frames - done);
                var o = output.Slice(done * 2, n * 2);
                float dt = n / (float)Synthesizer.SampleRate;

                // Fade towards the target; a finished fade-out hands over to the next song
                if (musicGain < fadeTarget) musicGain = MathF.Min(fadeTarget, musicGain + fadeRate * dt);
                else if (musicGain > fadeTarget) musicGain = MathF.Max(fadeTarget, musicGain - fadeRate * dt);
                if (musicGain <= 0f && !fanfareActive && fadeTarget <= 0f)
                {
                    if (pending != null) StartPending();
                    else if (music.Song != null) music.Stop();
                }

                // The music holds its place while a fanfare plays
                if (music.Song != null && musicGain > 0f) music.Render(o, musicGain);
                if (fanfare.Song != null)
                {
                    fanfare.Render(o, 1f);
                    if (fanfareActive && !fanfare.Playing)
                    {
                        // The last note rings on for a moment before the music comes back under it
                        fanfareTail += dt;
                        if (fanfareTail >= FanfareTail || fanfare.Silent)
                        {
                            fanfareActive = false;
                            if (pending != null) StartPending();
                            musicGain = 0f;
                            fadeTarget = 1f;
                            fadeRate = 1f / ResumeTime;
                        }
                    }
                    if (!fanfareActive && fanfare.Silent) fanfare.Stop();
                }
            }

            float gain = Muted ? 0f : Volume;
            for (int i = 0; i < output.Length; i++)
            {
                // A high-pass at ~30 Hz keeps rumble the speakers can't play out of the mix
                int ch = i & 1;
                float input = output[i];
                highPass[ch] = HighPassCoef * (highPass[ch] + input - lastInput[ch]);
                lastInput[ch] = input;

                // Gentle limiter: transparent below ±0.8, rounding off peaks above it
                float x = highPass[ch] * gain;
                float ax = MathF.Abs(x);
                if (ax > 0.8f) x = MathF.Sign(x) * (0.8f + 0.2f * MathF.Tanh((ax - 0.8f) / 0.2f));
                output[i] = x;
            }
        }
    }
}
