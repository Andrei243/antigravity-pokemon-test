using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>The buses everything the game hears goes through, each with its own volume.</summary>
public enum AudioBus { Music, Fanfare, Sound, Cry, Ambience }

/// <summary>
/// Mixes everything the game hears, the way the DS games do: a new area's theme fades the old one out first, a
/// fanfare (healing, an item, a level-up) silences the music, plays, and lets the music come back where it left
/// off, sound effects play over the music, and the music dips under a cry. Rendering runs on the audio thread,
/// commands on the game thread, so both take the lock.
/// </summary>
public sealed class AudioMixer
{
    private const float FadeOutTime = 0.45f;
    private const float FanfareDuckTime = 0.12f;
    private const float ResumeTime = 0.6f;
    private const float FanfareTail = 0.35f;

    /// <summary>How far the music dips under a cry, and how quickly it dips and comes back.</summary>
    public const float CryDuckLevel = 0.45f;
    private const float DuckTime = 0.08f;
    private const float UnduckTime = 0.4f;

    /// <summary>Sound effects that can play at once; a further one takes the place of the one nearest its end.</summary>
    public const int MaxSounds = 8;

    private readonly object gate = new();
    private readonly SongPlayer music = new();
    private readonly SongPlayer fanfare = new();
    private readonly float[] busVolume = { 1f, 1f, 1f, 1f, 1f };
    private readonly SoundVoice[] sounds = new SoundVoice[MaxSounds];
    private Song? pending;
    private bool pendingNight;
    private float musicGain = 1f;
    private float fadeTarget = 1f;
    private float fadeRate;
    private float duck = 1f;
    private bool fanfareActive;
    private float fanfareTail;
    private static readonly float HighPassCoef = 1f / (1f + MathF.Tau * 30f / Synthesizer.SampleRate);
    private readonly float[] highPass = new float[2];
    private readonly float[] lastInput = new float[2];

    private struct SoundVoice
    {
        public SoundSample? Sample;
        public int Position;
        public float Left, Right;
        public AudioBus Bus;
    }

    /// <summary>Overall volume (0 to 1) before the limiter: the trim that keeps the mix out of it.</summary>
    public float Volume { get; set; } = 0.8f;

    public bool Muted { get; set; }

    /// <summary>
    /// The music's low-HP arrangement (a battle theme turning agitated while the player's Pokémon is in the red).
    /// It holds across songs: the game sets it every frame of a battle and clears it when the battle ends.
    /// </summary>
    public bool LowHp
    {
        get { lock (gate) return music.Agitated; }
        set { lock (gate) music.Agitated = value; }
    }

    /// <summary>A bus's volume, 0 to 1: what the options set.</summary>
    public float VolumeOf(AudioBus bus)
    {
        lock (gate) return busVolume[(int)bus];
    }

    public void SetVolume(AudioBus bus, float volume)
    {
        lock (gate) busVolume[(int)bus] = Math.Clamp(volume, 0f, 1f);
    }

    /// <summary>The song that is playing or about to, or null.</summary>
    public string? CurrentId
    {
        get { lock (gate) return (pending ?? music.Song)?.Id; }
    }

    public bool FanfarePlaying
    {
        get { lock (gate) return fanfareActive; }
    }

    /// <summary>How many sound effects (and cries) are sounding now.</summary>
    public int SoundsPlaying
    {
        get
        {
            lock (gate)
            {
                int n = 0;
                foreach (var v in sounds) if (v.Sample != null) n++;
                return n;
            }
        }
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

    /// <summary>
    /// Plays a sound effect over whatever else is sounding, on its bus (a cry on <see cref="AudioBus.Cry"/> dips
    /// the music while it lasts). <paramref name="pan"/> is -1 for the left, 1 for the right.
    /// </summary>
    public void PlaySound(SoundSample sample, AudioBus bus = AudioBus.Sound, float volume = 1f, float pan = 0f)
    {
        lock (gate)
        {
            int slot = -1, nearestEnd = int.MaxValue;
            for (int i = 0; i < sounds.Length; i++)
            {
                if (sounds[i].Sample == null) { slot = i; break; }
                int left = sounds[i].Sample!.Samples.Length - sounds[i].Position;
                if (left < nearestEnd) { nearestEnd = left; slot = i; }
            }
            float angle = (Math.Clamp(pan, -1f, 1f) + 1f) * MathF.PI / 4f;
            sounds[slot] = new SoundVoice
            {
                Sample = sample, Position = 0, Bus = bus,
                Left = MathF.Cos(angle) * volume, Right = MathF.Sin(angle) * volume
            };
        }
    }

    /// <summary>Stops every sound effect and cry at once (a scene change).</summary>
    public void StopSounds()
    {
        lock (gate) Array.Clear(sounds);
    }

    private void StartPending()
    {
        music.Start(pending!, pendingNight);
        pending = null;
        musicGain = fanfareActive ? 0f : 1f;
        fadeTarget = musicGain;
    }

    /// <summary>Fills <paramref name="output"/> (interleaved stereo) with the next stretch of sound.</summary>
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

                // The music dips while a cry sounds and comes back once it is over
                bool crying = false;
                foreach (var v in sounds) if (v.Sample != null && v.Bus == AudioBus.Cry) crying = true;
                float duckTarget = crying ? CryDuckLevel : 1f;
                if (duck > duckTarget) duck = MathF.Max(duckTarget, duck - dt / DuckTime);
                else if (duck < duckTarget) duck = MathF.Min(duckTarget, duck + dt / UnduckTime);

                // The music holds its place while a fanfare plays
                if (music.Song != null && musicGain > 0f) music.Render(o, musicGain * duck * busVolume[(int)AudioBus.Music]);
                if (fanfare.Song != null)
                {
                    fanfare.Render(o, duck * busVolume[(int)AudioBus.Fanfare]);
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

                RenderSounds(o);
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

    /// <summary>Adds the sound effects that are playing into <paramref name="o"/> and drops the ones that have ended.</summary>
    private void RenderSounds(Span<float> o)
    {
        int n = o.Length / 2;
        for (int s = 0; s < sounds.Length; s++)
        {
            ref var v = ref sounds[s];
            if (v.Sample == null) continue;
            var data = v.Sample.Samples;
            float bus = busVolume[(int)v.Bus];
            float l = v.Left * bus, r = v.Right * bus;
            int count = Math.Min(n, data.Length - v.Position);
            for (int i = 0; i < count; i++)
            {
                float x = data[v.Position + i];
                o[i * 2] += x * l;
                o[i * 2 + 1] += x * r;
            }
            v.Position += count;
            if (v.Position >= data.Length) v.Sample = null;
        }
    }
}
