using System.Collections.Generic;

namespace PokemonPlatinumEngine.Audio;

/// <summary>One note of a track, in ticks (<see cref="Song.TicksPerWhole"/> to a whole note).</summary>
public readonly record struct NoteEvent(int Tick, int Length, int Gate, int Key, float Velocity);

/// <summary>Which arrangements a track belongs to.</summary>
public enum TrackWhen { Always, LowHp, NotLowHp }

public sealed class SongTrack
{
    public required string Name { get; init; }
    public required Instrument Instrument { get; init; }

    /// <summary>The instrument the night arrangement uses instead; the same one when null.</summary>
    public Instrument? NightInstrument { get; init; }

    /// <summary>The instrument the low-HP arrangement uses instead (over the night one); the same one when null.</summary>
    public Instrument? LowHpInstrument { get; init; }

    /// <summary>When the track plays: always, only in the low-HP arrangement (an alarm figure), or only out of it.</summary>
    public TrackWhen When { get; init; } = TrackWhen.Always;

    public float Volume { get; init; } = 0.8f;
    public float Pan { get; init; }
    public float ReverbSend { get; init; } = 0.3f;

    /// <summary>Notes in the order they start.</summary>
    public List<NoteEvent> Notes { get; } = new();

    public bool IsDrums => Instrument.IsKit;
}

/// <summary>
/// A piece of music: tracks of notes, a tempo, and where it loops back to. Fanfares and jingles have no loop and
/// play once. Read from the <c>.mml</c> files in <c>Data/music</c> by <see cref="Mml"/>.
/// </summary>
public sealed class Song
{
    public const int TicksPerWhole = 192;
    public const int TicksPerQuarter = TicksPerWhole / 4;

    /// <summary>The path under Data/music without the extension, for example <c>sinnoh/twinleaf</c>.</summary>
    public required string Id { get; init; }

    public string Title { get; init; } = "";

    /// <summary>Quarter notes per minute.</summary>
    public double Tempo { get; init; } = 120;

    /// <summary>The night arrangement plays this much slower or faster.</summary>
    public double NightTempo { get; init; } = 0.95;

    /// <summary>
    /// False for a song that plays the same by day and by night (the header <c>night none</c>): the original's caves and
    /// dungeons have one theme, the same by day and by night in their map headers.
    /// </summary>
    public bool HasNight { get; init; } = true;

    /// <summary>The low-HP arrangement of a battle theme plays this much faster (Black and White's way: the music itself turns agitated).</summary>
    public double LowHpTempo { get; init; } = 1.0;

    public int TicksPerBar { get; init; } = TicksPerWhole;

    /// <summary>Tick the song jumps back to when it reaches the end; -1 to play once.</summary>
    public int LoopTick { get; init; } = -1;

    /// <summary>The tick where the song ends (and loops from).</summary>
    public int EndTick { get; init; }

    public float Reverb { get; init; } = 0.3f;

    public List<SongTrack> Tracks { get; } = new();

    public bool Loops => LoopTick >= 0;

    public double SecondsPerTick(bool night, bool lowHp = false) => 60.0 / (Tempo * (night && HasNight ? NightTempo : 1.0) * (lowHp ? LowHpTempo : 1.0) * TicksPerQuarter);

    /// <summary>Length of one pass through the song, in seconds.</summary>
    public double Duration(bool night = false) => EndTick * SecondsPerTick(night);

    /// <summary>Length of the looping part, in seconds.</summary>
    public double LoopDuration(bool night = false) => Loops ? (EndTick - LoopTick) * SecondsPerTick(night) : 0;
}
