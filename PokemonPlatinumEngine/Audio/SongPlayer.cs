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

    private readonly Synthesizer synth;
    private readonly float[] block = new float[Block * 2];

    /// <summary>A player in the DS sound, as every song has played.</summary>
    public SongPlayer() : this(SynthSound.Ds) { }

    /// <summary>A player in the sound given (plan 31: the modern one renders at <see cref="Synthesizer.ModernRate"/>).</summary>
    public SongPlayer(SynthSound sound)
    {
        synth = new Synthesizer(sound);
        if (sound == SynthSound.Modern) compressor = new Compressor(synth.Rate);
    }

    /// <summary>The modern sound's compressor on the song's own mix (plan 31 · MU2); none in the DS sound.</summary>
    private readonly Compressor? compressor;

    /// <summary>How hard the compressor is working now, in dB (0 when it isn't, or in the DS sound).</summary>
    public float CompressionDb => compressor?.ReductionDb ?? 0f;

    /// <summary>The compressor's mean and most reduction since the player was made, in dB (both 0 in the DS sound).</summary>
    public (float Mean, float Most) Compression => compressor == null ? (0f, 0f) : (compressor.MeanDb, compressor.MostDb);

    /// <summary>The rate this player renders at.</summary>
    public int Rate => synth.Rate;

    public SynthSound Sound => synth.Sound;

    /// <summary>Notes that took a voice still sounding, since the player was made (plan 31: whether the voices suffice).</summary>
    public int VoicesStolen => synth.Stolen;

    /// <summary>The most voices sounding at once, since the player was made.</summary>
    public int PeakVoices => synth.PeakVoices;
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
            Retime();
        }
    }

    /// <summary>
    /// The giant arrangement (plan Y · YD5), switched while a Dynamaxed Pokémon stands on the field, the same way as
    /// <see cref="Agitated"/> and independently of it: its layer joins in or falls silent on the next notes and the
    /// song keeps its place. A song with no giant arrangement plays exactly as it did.
    /// </summary>
    public bool Giant
    {
        get => giant;
        set
        {
            if (giant == value) return;
            giant = value;
            Retime();
        }
    }
    private bool giant;

    private void Retime()
    {
        if (song != null) ticksPerSample = 1.0 / (song.SecondsPerTick(night, agitated, giant) * synth.Rate);
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
        night = atNight && s.HasNight;
        tick = 0;
        Loops = 0;
        ticksPerSample = 1.0 / (s.SecondsPerTick(night, agitated, giant) * synth.Rate);
        next = new int[s.Tracks.Count];
        synth.AllNotesOff(immediate: true);
        synth.ReverbLevel = s.Reverb + (night ? 0.08f : 0f);
        if (synth.Sound == SynthSound.Modern)
        {
            // The night's room is a little longer as well as wetter
            var room = s.Room ?? RoomShape.Default;
            synth.Room = night ? room with { Seconds = room.Seconds * 1.15f } : room;
            compressor!.Reset();
        }
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
            compressor?.Process(b);
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
                bool plays = track.When switch
                {
                    TrackWhen.LowHp => agitated, TrackWhen.NotLowHp => !agitated,
                    TrackWhen.Giant => giant, TrackWhen.NotGiant => !giant, _ => true
                };
                while (next[t] < notes.Count && notes[next[t]].Tick < end && notes[next[t]].Tick < s.EndTick)
                {
                    var note = notes[next[t]++];
                    if (note.Tick < tick || !plays) continue;
                    var inst = agitated && track.LowHpInstrument != null ? track.LowHpInstrument
                        : giant && track.GiantInstrument != null ? track.GiantInstrument
                        : night ? track.NightInstrument ?? track.Instrument : track.Instrument;
                    float vol = track.Volume * (night && track.IsDrums ? 0.6f : 1f);
                    synth.NoteOn(inst, note.Key, note.Velocity, (int)(note.Gate * samplesPerTick), track.Pan, vol, track.ReverbSend, track.Width);
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
