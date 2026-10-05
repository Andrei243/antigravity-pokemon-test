using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Plays one <see cref="Song"/> through its own <see cref="Synthesizer"/>: starts each note on time, loops at the
/// song's loop point, and applies the night arrangement (softer instruments, a little slower, quieter drums, more
/// reverb). Notes start on a 1 ms grid, which is finer than anyone can hear.
/// </summary>
public sealed class SongPlayer
{
    private const int Block = 32;

    private readonly Synthesizer synth = new();
    private readonly float[] block = new float[Block * 2];
    private Song? song;
    private bool night;
    private bool agitated;
    private double tick;
    private double ticksPerSample;
    private int[] next = Array.Empty<int>();

    public Song? Song => song;
    public bool Night => night;

    /// <summary>
    /// The low-HP arrangement, switched on and off in the middle of the song: the notes already sounding ring on,
    /// the next ones start on the arrangement's instruments, its own tracks join in or fall silent, and the tempo
    /// changes from here, so the song keeps its place.
    /// </summary>
    public bool Agitated
    {
        get => agitated;
        set
        {
            if (agitated == value) return;
            agitated = value;
            if (song != null) ticksPerSample = 1.0 / (song.SecondsPerTick(night, agitated) * Synthesizer.SampleRate);
        }
    }

    /// <summary>True while notes are still being started (a one-shot song that has reached its end is not).</summary>
    public bool Playing { get; private set; }

    /// <summary>True once nothing more will sound: the song has ended and its last notes have died away.</summary>
    public bool Silent => !Playing && synth.ActiveVoices == 0;

    /// <summary>How many times the song has wrapped round to its loop point.</summary>
    public int Loops { get; private set; }

    /// <summary>The position in the song, in seconds at its own tempo (the low-HP arrangement plays it faster but keeps its place).</summary>
    public double Position => song == null ? 0 : tick * song.SecondsPerTick(night);

    public void Start(Song s, bool atNight)
    {
        song = s;
        night = atNight;
        tick = 0;
        Loops = 0;
        ticksPerSample = 1.0 / (s.SecondsPerTick(atNight, agitated) * Synthesizer.SampleRate);
        next = new int[s.Tracks.Count];
        synth.AllNotesOff(immediate: true);
        synth.ReverbLevel = s.Reverb + (atNight ? 0.08f : 0f);
        Playing = true;
    }

    public void Stop()
    {
        Playing = false;
        song = null;
        synth.AllNotesOff(immediate: true);
    }

    /// <summary>Lets the sounding notes ring out and starts no new ones.</summary>
    public void Release()
    {
        Playing = false;
        synth.AllNotesOff(immediate: false);
    }

    /// <summary>Renders interleaved stereo, adding it into <paramref name="output"/> at <paramref name="gain"/>.</summary>
    public void Render(Span<float> output, float gain)
    {
        int frames = output.Length / 2;
        for (int done = 0; done < frames; done += Block)
        {
            int n = Math.Min(Block, frames - done);
            if (Playing) Advance(n);
            var b = block.AsSpan(0, n * 2);
            synth.Render(b);
            var o = output.Slice(done * 2, n * 2);
            for (int i = 0; i < b.Length; i++) o[i] += b[i] * gain;
        }
    }

    /// <summary>Starts the notes that fall within the next <paramref name="samples"/> samples.</summary>
    private void Advance(int samples)
    {
        var s = song!;
        double end = tick + samples * ticksPerSample;
        double samplesPerTick = 1.0 / ticksPerSample;

        while (true)
        {
            for (int t = 0; t < s.Tracks.Count; t++)
            {
                var track = s.Tracks[t];
                var notes = track.Notes;
                bool plays = track.When switch { TrackWhen.LowHp => agitated, TrackWhen.NotLowHp => !agitated, _ => true };
                while (next[t] < notes.Count && notes[next[t]].Tick < end && notes[next[t]].Tick < s.EndTick)
                {
                    var note = notes[next[t]++];
                    if (note.Tick < tick || !plays) continue;
                    var inst = agitated && track.LowHpInstrument != null ? track.LowHpInstrument
                        : night ? track.NightInstrument ?? track.Instrument : track.Instrument;
                    float vol = track.Volume * (night && track.IsDrums ? 0.6f : 1f);
                    synth.NoteOn(inst, note.Key, note.Velocity, (int)(note.Gate * samplesPerTick), track.Pan, vol, track.ReverbSend);
                }
            }

            if (end < s.EndTick) break;

            // Reached the end: wrap to the loop point, or stop starting notes
            if (!s.Loops)
            {
                Playing = false;
                return;
            }
            Loops++;
            double over = end - s.EndTick;
            tick = s.LoopTick;
            end = s.LoopTick + over;
            for (int t = 0; t < s.Tracks.Count; t++)
            {
                var notes = s.Tracks[t].Notes;
                int i = 0;
                while (i < notes.Count && notes[i].Tick < s.LoopTick) i++;
                next[t] = i;
            }
        }
        tick = end;
    }
}
